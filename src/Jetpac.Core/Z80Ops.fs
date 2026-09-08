namespace Jetpac.Core

/// Port of specbolt's v2 instruction set (z80/v2/Z80Impl.hpp + Z80Impl.cpp).
/// The C++ generates a constexpr table of 256 function pointers per prefix;
/// here the generator is replaced by concrete dispatch functions. Timing and
/// flag semantics are ported 1:1 from the C++.
module Z80Ops =

    type HlSet =
        | Base
        | Ix
        | Iy

    let private isIndexed (ctx: HlSet) = ctx <> Base

    let private hlHighLow (ctx: HlSet) : R16 =
        match ctx with
        | Base -> R16.HL
        | Ix -> R16.IX
        | Iy -> R16.IY

    let private hlHigh (ctx: HlSet) : R8 =
        match ctx with
        | Base -> R8.H
        | Ix -> R8.IXH
        | Iy -> R8.IYH

    let private hlLow (ctx: HlSet) : R8 =
        match ctx with
        | Base -> R8.L
        | Ix -> R8.IXL
        | Iy -> R8.IYL

    /// TableRp highlow: BC, DE, hl, SP
    let private rpHighLow (ctx: HlSet) (p: int) : R16 =
        match p with
        | 0 -> R16.BC
        | 1 -> R16.DE
        | 2 -> hlHighLow ctx
        | 3 -> R16.SP
        | _ -> failwith "bad rp"

    let private rpHigh (ctx: HlSet) (p: int) : R8 =
        match p with
        | 0 -> R8.B
        | 1 -> R8.D
        | 2 -> hlHigh ctx
        | 3 -> R8.SPH
        | _ -> failwith "bad rp"

    let private rpLow (ctx: HlSet) (p: int) : R8 =
        match p with
        | 0 -> R8.C
        | 1 -> R8.E
        | 2 -> hlLow ctx
        | 3 -> R8.SPL
        | _ -> failwith "bad rp"

    /// TableRp2 highlow: BC, DE, hl, AF
    let private rp2HighLow (ctx: HlSet) (p: int) : R16 =
        match p with
        | 0 -> R16.BC
        | 1 -> R16.DE
        | 2 -> hlHighLow ctx
        | 3 -> R16.AF
        | _ -> failwith "bad rp2"

    /// TableR register for index 0..5,7. Index 6 (indirect) and 8 ($nn) are
    /// handled by getR/setR directly.
    let private tableR (ctx: HlSet) (noRemap: bool) (idx: int) : R8 =
        match idx with
        | 0 -> R8.B
        | 1 -> R8.C
        | 2 -> R8.D
        | 3 -> R8.E
        | 4 -> if noRemap then R8.H else hlHigh ctx
        | 5 -> if noRemap then R8.L else hlLow ctx
        | 7 -> R8.A
        | _ -> failwith "bad tableR index"

    let private getR (z80: Z80) (ctx: HlSet) (idx: int) (noRemap: bool) : int =
        match idx with
        | 6 -> z80.Read(z80.Regs.Wz())
        | 8 -> z80.ReadImmediate()
        | _ -> z80.Regs.Get(tableR ctx noRemap idx)

    let private setR (z80: Z80) (ctx: HlSet) (idx: int) (noRemap: bool) (value: int) =
        match idx with
        | 6 -> z80.Write(z80.Regs.Wz(), value)
        | 8 -> failwith "cannot set immediate value"
        | _ -> z80.Regs.Set(tableR ctx noRemap idx, value)

    let private signedImm (v: int) = if v >= 0x80 then v - 0x100 else v

    let private ccCheck (cc: int) (flags: Flags) : bool =
        match cc with
        | 0 -> not flags.zero
        | 1 -> flags.zero
        | 2 -> not flags.carry
        | 3 -> flags.carry
        | 4 -> not flags.parity
        | 5 -> flags.parity
        | 6 -> not flags.sign
        | 7 -> flags.sign
        | _ -> failwith "bad cc"

    let private imTable = [| 0; 0; 1; 2; 0; 0; 1; 2 |]

    /// 8 ALU variants on (lhs, rhs, flags): add/adc/sub/sbc/and/xor/or/cp.
    let private aluOp (y: int) (lhs: int) (rhs: int) (flags: Flags) : struct (int * Flags) =
        match y with
        | 0 -> Alu.add8 lhs rhs false
        | 1 -> Alu.add8 lhs rhs flags.carry
        | 2 -> Alu.sub8 lhs rhs false
        | 3 -> Alu.sub8 lhs rhs flags.carry
        | 4 -> Alu.and8 lhs rhs
        | 5 -> Alu.xor8 lhs rhs
        | 6 -> Alu.or8 lhs rhs
        | 7 -> Alu.cmp8 lhs rhs
        | _ -> failwith "bad alu y"

    // ------------------------------------------------------------------------
    // ED block instructions
    // ------------------------------------------------------------------------

    let private blockLoad (z80: Z80) (increment: bool) (repeat: bool) =
        let add = if increment then 1 else 0xFFFF
        let hl = z80.Regs.Get R16.HL
        z80.Regs.Set(R16.HL, (hl + add) &&& 0xFFFF)
        let byte = z80.Read hl
        let de = z80.Regs.Get R16.DE
        z80.Regs.Set(R16.DE, (de + add) &&& 0xFFFF)
        z80.Write(de, byte)
        z80.PassTime 2
        // bits 3 and 5 come from the weird value of "byte read + A", where bit 3
        // goes to flag 5, and bit 1 to flag 3.
        let flagBits = byte + z80.Regs.Get R8.A
        let newBc = (z80.Regs.Get R16.BC - 1) &&& 0xFFFF
        z80.Regs.Set(R16.BC, newBc)

        let preservedFlags =
            z80.Flags() &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Carry())

        let flagsFromBits =
            (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags())
            ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())

        let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()
        z80.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc)

        if repeat && newBc <> 0 then
            z80.Regs.SetWz((z80.Regs.Pc() - 1) &&& 0xFFFF)
            z80.Regs.SetPc((z80.Regs.Pc() - 2) &&& 0xFFFF)
            z80.PassTime 5

    let private blockCompare (z80: Z80) (increment: bool) (repeat: bool) =
        let add = if increment then 1 else 0xFFFF
        let hl = z80.Regs.Get R16.HL
        z80.Regs.Set(R16.HL, (hl + add) &&& 0xFFFF)
        let byte = z80.Read hl
        let struct (result, subtractFlags) = Alu.sub8 (z80.Regs.Get R8.A) byte false
        z80.PassTime 5
        // bits 3 and 5 come from the result, where bit 3 goes to flag 5, and bit 1
        // to flag 3... and where HF is set we use res-1.
        let flagBits = if subtractFlags.half_carry then result - 1 else result
        let newBc = (z80.Regs.Get R16.BC - 1) &&& 0xFFFF
        z80.Regs.Set(R16.BC, newBc)

        let fromSubtractMask =
            Flags.HalfCarry() ||| Flags.Zero() ||| Flags.Sign() ||| Flags.Subtract()

        let preservedFlags =
            z80.Flags()
            &&& ~~~(Flags.Flag3() ||| Flags.Flag5() ||| fromSubtractMask ||| Flags.Overflow())

        let flagsFromBits =
            (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags())
            ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())

        let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()

        z80.SetFlags(
            preservedFlags
            ||| flagsFromBits
            ||| flagsFromBc
            ||| (fromSubtractMask &&& subtractFlags)
        )

        if repeat && newBc <> 0 && not subtractFlags.zero then
            z80.Regs.SetWz((z80.Regs.Pc() - 1) &&& 0xFFFF)
            z80.Regs.SetPc((z80.Regs.Pc() - 2) &&& 0xFFFF)
            z80.PassTime 5

    // ------------------------------------------------------------------------
    // CB table
    // ------------------------------------------------------------------------

    let private cbOp (z80: Z80) (opcode: int) (ctx: HlSet) =
        let x = opcode >>> 6
        let y = (opcode >>> 3) &&& 7
        let z = opcode &&& 7

        match x with
        | 0 ->
            // Rotate/shift group.
            let lhs = getR z80 ctx z false

            if z = 6 then
                z80.PassTime 1

            let struct (result, flags) =
                match y with
                | 0 -> Alu.rotateCircular8 lhs Alu.Left
                | 1 -> Alu.rotateCircular8 lhs Alu.Right
                | 2 -> Alu.rotate8 lhs Alu.Left (z80.Flags().carry)
                | 3 -> Alu.rotate8 lhs Alu.Right (z80.Flags().carry)
                | 4 -> Alu.shiftArithmetic8 lhs Alu.Left
                | 5 -> Alu.shiftArithmetic8 lhs Alu.Right
                | 6 -> Alu.shiftLogical8 lhs Alu.Left
                | 7 -> Alu.shiftLogical8 lhs Alu.Right
                | _ -> failwith "bad cb y"

            setR z80 ctx z false result
            z80.SetFlags flags
        | 1 ->
            // BIT y, r
            let lhs = getR z80 ctx z false

            if z = 6 then
                z80.PassTime 1

            let busNoise = if z = 6 then (z80.Regs.Wz() >>> 8) &&& 0xFF else lhs
            z80.SetFlags(Alu.bit lhs (1 <<< y) (z80.Flags()) busNoise)
        | 2
        | 3 ->
            // RES (x=2) / SET (x=3) y, r
            let lhs = getR z80 ctx z false

            if z = 6 then
                z80.PassTime 1

            let bit = 1 <<< y
            let result = if x = 2 then lhs &&& ~~~bit else lhs ||| bit
            setR z80 ctx z false result
        | _ -> failwith "bad cb x"

    // ------------------------------------------------------------------------
    // ED table
    // ------------------------------------------------------------------------

    let private edOp (z80: Z80) (opcode: int) =
        let x = opcode >>> 6
        let y = (opcode >>> 3) &&& 7
        let z = opcode &&& 7
        let p = y >>> 1
        let q = y &&& 1

        match x with
        | 0
        | 3 -> () // InvalidInstruction
        | 1 ->
            match z with
            | 0 ->
                z80.PassTime 4 // IN time
                let port = z80.Regs.Get R16.BC
                let result = z80.In port
                z80.SetFlags((z80.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor result)

                if y <> 6 then
                    setR z80 Base y false result
            | 1 ->
                z80.PassTime 4 // OUT time

                if y = 6 then
                    z80.Out(z80.Regs.Get R16.BC, 0)
                else
                    z80.Out(z80.Regs.Get R16.BC, getR z80 Base y false)
            | 2 ->
                let rhs = z80.Regs.Get(rpHighLow Base p)
                let lhs = z80.Regs.Get R16.HL

                let struct (result, flags) =
                    if q = 0 then
                        Alu.sbc16 lhs rhs (z80.Flags().carry)
                    else
                        Alu.adc16 lhs rhs (z80.Flags().carry)

                z80.Regs.Set(R16.HL, result)
                z80.SetFlags flags
                z80.PassTime 7
            | 3 ->
                let addr = z80.ReadImmediate16()

                if q = 0 then
                    z80.Write(addr, z80.Regs.Get(rpLow Base p))
                    z80.Write((addr + 1) &&& 0xFFFF, z80.Regs.Get(rpHigh Base p))
                else
                    z80.Regs.Set(rpLow Base p, z80.Read addr)
                    z80.Regs.Set(rpHigh Base p, z80.Read((addr + 1) &&& 0xFFFF))
            | 4 ->
                // NEG
                let struct (result, flags) = Alu.sub8 0 (z80.Regs.Get R8.A) false
                z80.Regs.Set(R8.A, result)
                z80.SetFlags flags
            | 5 ->
                if y <> 1 then
                    z80.Iff1 <- z80.Iff2 // retn

                let returnAddress = z80.Pop16()
                z80.Regs.SetPc returnAddress
            | 6 -> z80.IrqMode <- imTable[y]
            | 7 ->
                match y with
                | 0 ->
                    z80.PassTime 1
                    z80.Regs.SetI(z80.Regs.Get R8.A)
                | 1 ->
                    z80.PassTime 1
                    z80.Regs.SetR(z80.Regs.Get R8.A)
                | 2 ->
                    let result = z80.Regs.I()
                    z80.PassTime 1
                    z80.SetFlags(Alu.iff2FlagsFor result (z80.Flags()) z80.Iff2)
                    z80.Regs.Set(R8.A, result)
                | 3 ->
                    let result = z80.Regs.R()
                    z80.PassTime 1
                    z80.SetFlags(Alu.iff2FlagsFor result (z80.Flags()) z80.Iff2)
                    z80.Regs.Set(R8.A, result)
                | 4 ->
                    // RRD
                    let address = z80.Regs.Get R16.HL
                    let indHl = z80.Read address
                    let prevA = z80.Regs.Get R8.A
                    let newA = (prevA &&& 0xF0) ||| (indHl &&& 0xF)
                    z80.Regs.Set(R8.A, newA)
                    z80.PassTime 4
                    z80.Write(address, ((indHl >>> 4) ||| ((prevA &&& 0xF) <<< 4)) &&& 0xFF)
                    z80.SetFlags((z80.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor newA)
                | 5 ->
                    // RLD
                    let address = z80.Regs.Get R16.HL
                    let indHl = z80.Read address
                    let prevA = z80.Regs.Get R8.A
                    let newA = (prevA &&& 0xF0) ||| ((indHl >>> 4) &&& 0xF)
                    z80.Regs.Set(R8.A, newA)
                    z80.PassTime 4
                    z80.Write(address, ((indHl <<< 4) ||| (prevA &&& 0xF)) &&& 0xFF)
                    z80.SetFlags((z80.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor newA)
                | _ -> () // InvalidInstruction
            | _ -> failwith "bad ed z"
        | 2 ->
            match z with
            | 0 -> blockLoad z80 (y &&& 1 = 0) (y &&& 2 <> 0)
            | 1 -> blockCompare z80 (y &&& 1 = 0) (y &&& 2 <> 0)
            | _ -> () // InvalidInstruction (TODO otid/inir)
        | _ -> failwith "bad ed x"

    // ------------------------------------------------------------------------
    // Main table (shared between Base, IX and IY)
    // ------------------------------------------------------------------------

    /// True when the opcode's only operand is (hl) / (ix+d) / (iy+d), which
    /// makes the indirect wrapper (wz setup / displacement read) apply.
    let private isIndirectMain (opcode: int) =
        let x = opcode >>> 6
        let y = (opcode >>> 3) &&& 7
        let z = opcode &&& 7

        (x = 0 && (z = 4 || z = 5) && y = 6)
        || (x = 0 && z = 6 && y = 6)
        || (x = 1 && not (y = 6 && z = 6) && (y = 6 || z = 6))
        || (x = 2 && z = 6)

    let rec private mainOp (z80: Z80) (opcode: int) (ctx: HlSet) =
        let x = opcode >>> 6
        let y = (opcode >>> 3) &&& 7
        let z = opcode &&& 7
        let p = y >>> 1
        let q = y &&& 1

        match x with
        | 0 ->
            match z with
            | 0 ->
                match y with
                | 0 -> () // nop
                | 1 -> z80.Regs.Ex(R16.AF, R16.AF_) // ex af, af'
                | 2 -> // djnz $d
                    z80.PassTime 1
                    let offset = signedImm (z80.ReadImmediate())
                    let newB = z80.Regs.Get R8.B - 1
                    z80.Regs.Set(R8.B, newB)

                    if newB <> 0 then
                        z80.PassTime 5
                        z80.Branch offset
                | 3 -> // jr $d
                    let offset = signedImm (z80.ReadImmediate())
                    z80.PassTime 5
                    z80.Branch offset
                | _ -> // jr cc, $d (y = 4..7)
                    let offset = signedImm (z80.ReadImmediate())

                    if ccCheck (y - 4) (z80.Flags()) then
                        z80.PassTime 5
                        z80.Branch offset
            | 1 ->
                if q = 0 then
                    // ld rp, $nnnn
                    z80.Regs.Set(rpLow ctx p, z80.ReadImmediate())
                    z80.Regs.Set(rpHigh ctx p, z80.ReadImmediate())
                else
                    // add hl, rp
                    let rhs = z80.Regs.Get(rpHighLow ctx p)

                    let struct (result, flags) =
                        Alu.add16 (z80.Regs.Get(hlHighLow ctx)) rhs (z80.Flags())

                    z80.Regs.Set(hlHighLow ctx, result)
                    z80.SetFlags flags
                    z80.PassTime 7
            | 2 ->
                if q = 0 then
                    match p with
                    | 0 -> z80.Write(z80.Regs.Get R16.BC, z80.Regs.Get R8.A) // ld (bc), a
                    | 1 -> z80.Write(z80.Regs.Get R16.DE, z80.Regs.Get R8.A) // ld (de), a
                    | 2 -> // ld ($nnnn), hl
                        let address = z80.ReadImmediate16()
                        z80.Write(address, z80.Regs.Get(hlLow ctx))
                        z80.Write((address + 1) &&& 0xFFFF, z80.Regs.Get(hlHigh ctx))
                    | 3 -> // ld ($nnnn), a
                        let address = z80.ReadImmediate16()
                        z80.Write(address, z80.Regs.Get R8.A)
                    | _ -> failwith "bad p"
                else
                    match p with
                    | 0 -> z80.Regs.Set(R8.A, z80.Read(z80.Regs.Get R16.BC)) // ld a, (bc)
                    | 1 -> z80.Regs.Set(R8.A, z80.Read(z80.Regs.Get R16.DE)) // ld a, (de)
                    | 2 -> // ld hl, ($nnnn)
                        let address = z80.ReadImmediate16()
                        z80.Regs.Set(hlLow ctx, z80.Read address)
                        z80.Regs.Set(hlHigh ctx, z80.Read((address + 1) &&& 0xFFFF))
                    | 3 -> // ld a, ($nnnn)
                        let address = z80.ReadImmediate16()
                        z80.Regs.Set(R8.A, z80.Read address)
                    | _ -> failwith "bad p"
            | 3 ->
                if q = 0 then
                    z80.Regs.Set(rpHighLow ctx p, (z80.Regs.Get(rpHighLow ctx p) + 1) &&& 0xFFFF) // inc rp
                    z80.PassTime 2
                else
                    z80.Regs.Set(rpHighLow ctx p, (z80.Regs.Get(rpHighLow ctx p) - 1) &&& 0xFFFF) // dec rp
                    z80.PassTime 2
            | 4 -> // inc r
                let rhs = getR z80 ctx y false

                if y = 6 then
                    z80.PassTime 1

                let struct (result, flags) = Alu.inc8 rhs (z80.Flags())
                setR z80 ctx y false result
                z80.SetFlags flags
            | 5 -> // dec r
                let rhs = getR z80 ctx y false

                if y = 6 then
                    z80.PassTime 1

                let struct (result, flags) = Alu.dec8 rhs (z80.Flags())
                setR z80 ctx y false result
                z80.SetFlags flags
            | 6 -> // ld r, $nn
                setR z80 ctx y false (z80.ReadImmediate())
            | 7 -> // fast ALU ops on A
                let a = z80.Regs.Get R8.A
                let flags = z80.Flags()

                match y with
                | 0 ->
                    let struct (r, f) = Alu.fastRotateCircular8 a Alu.Left flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 1 ->
                    let struct (r, f) = Alu.fastRotateCircular8 a Alu.Right flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 2 ->
                    let struct (r, f) = Alu.fastRotate8 a Alu.Left flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 3 ->
                    let struct (r, f) = Alu.fastRotate8 a Alu.Right flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 4 ->
                    let struct (r, f) = Alu.daa a flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 5 ->
                    let struct (r, f) = Alu.cpl a flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 6 ->
                    let struct (r, f) = Alu.scf a flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | 7 ->
                    let struct (r, f) = Alu.ccf a flags
                    z80.Regs.Set(R8.A, r)
                    z80.SetFlags f
                | _ -> failwith "bad fast alu y"
            | _ -> failwith "bad z"
        | 1 ->
            if y = 6 && z = 6 then
                z80.Halt()
            else
                // ld y, z — noRemap for the partner operand when one is (hl)/(ix+d).
                setR z80 ctx y (z = 6) (getR z80 ctx z (y = 6))
        | 2 ->
            // ALU a, r / a, (hl)
            let a = z80.Regs.Get R8.A
            let rhs = getR z80 ctx z false
            let struct (result, flags) = aluOp y a rhs (z80.Flags())
            z80.Regs.Set(R8.A, result)
            z80.SetFlags flags
        | 3 ->
            match z with
            | 0 -> // ret cc
                z80.PassTime 1

                if ccCheck y (z80.Flags()) then
                    let returnAddress = z80.Pop16()
                    z80.Regs.SetPc returnAddress
            | 1 ->
                if q = 0 then
                    // pop rp2
                    let result = z80.Pop16()
                    z80.Regs.Set(rp2HighLow ctx p, result)
                else
                    match p with
                    | 0 -> // ret
                        let returnAddress = z80.Pop16()
                        z80.Regs.SetPc returnAddress
                    | 1 -> z80.Regs.Exx()
                    | 2 -> // jp (hl)
                        z80.Regs.SetPc(z80.Regs.Get(hlHighLow ctx))
                    | 3 -> // ld sp, hl
                        z80.PassTime 2
                        z80.Regs.SetSp(z80.Regs.Get(hlHighLow ctx))
                    | _ -> failwith "bad p"
            | 2 -> // jp cc, $nnnn
                let jumpAddress = z80.ReadImmediate16()

                if ccCheck y (z80.Flags()) then
                    z80.Regs.SetPc jumpAddress
            | 3 ->
                match y with
                | 0 -> // jp $nnnn
                    let jumpAddress = z80.ReadImmediate16()
                    z80.Regs.SetPc jumpAddress
                | 1 -> // CB prefix
                    match ctx with
                    | Base -> decodeAndRunCB z80
                    | Ix -> decodeAndRunDDCB z80
                    | Iy -> decodeAndRunFDCB z80
                | 2 -> // out ($nn), a
                    let a = z80.Regs.Get R8.A
                    let port = (z80.ReadImmediate() ||| (a <<< 8)) &&& 0xFFFF
                    z80.PassTime 4 // OUT time
                    z80.Out(port, a)
                | 3 -> // in a, ($nn)
                    let port = (z80.ReadImmediate() ||| ((z80.Regs.Get R8.A) <<< 8)) &&& 0xFFFF
                    z80.PassTime 4 // IN time
                    z80.Regs.Set(R8.A, z80.In port)
                | 4 -> // ex (sp), hl
                    let sp = z80.Regs.Sp()
                    let spOldLow = z80.Read sp
                    z80.PassTime 1
                    let spOldHigh = z80.Read((sp + 1) &&& 0xFFFF)
                    z80.Write(sp, z80.Regs.Get(hlLow ctx))
                    z80.PassTime 2
                    z80.Write((sp + 1) &&& 0xFFFF, z80.Regs.Get(hlHigh ctx))
                    z80.Regs.Set(hlHighLow ctx, (spOldLow ||| (spOldHigh <<< 8)) &&& 0xFFFF)
                | 5 -> z80.Regs.Ex(R16.DE, hlHighLow ctx) // ex de, hl
                | 6 -> // di
                    z80.Iff1 <- false
                    z80.Iff2 <- false
                | 7 -> // ei
                    z80.Iff1 <- true
                    z80.Iff2 <- true
                | _ -> failwith "bad y"
            | 4 -> // call cc, $nnnn
                let jumpAddress = z80.ReadImmediate16()

                if ccCheck y (z80.Flags()) then
                    z80.PassTime 1
                    z80.Push16(z80.Regs.Pc())
                    z80.Regs.SetPc jumpAddress
            | 5 ->
                if q = 0 then
                    // push rp2
                    z80.PassTime 1
                    z80.Push16(z80.Regs.Get(rp2HighLow ctx p))
                else
                    match p with
                    | 0 -> // call $nnnn
                        let jumpAddress = z80.ReadImmediate16()
                        z80.PassTime 1
                        z80.Push16(z80.Regs.Pc())
                        z80.Regs.SetPc jumpAddress
                    | 1 -> decodeAndRunDD z80
                    | 2 -> decodeAndRunED z80
                    | 3 -> decodeAndRunFD z80
                    | _ -> failwith "bad p"
            | 6 ->
                // ALU a, $nn
                let a = z80.Regs.Get R8.A
                let rhs = z80.ReadImmediate()
                let struct (result, flags) = aluOp y a rhs (z80.Flags())
                z80.Regs.Set(R8.A, result)
                z80.SetFlags flags
            | 7 -> // rst
                z80.PassTime 1
                z80.Push16(z80.Regs.Pc())
                z80.Regs.SetPc(y * 8)
            | _ -> failwith "bad z"
        | _ -> failwith "bad x"

    // ------------------------------------------------------------------------
    // Prefix handlers (mutually recursive with mainOp)
    // ------------------------------------------------------------------------

    and decodeAndRunCB (z80: Z80) =
        let opcode = z80.ReadOpcode()

        if opcode &&& 7 = 6 then
            z80.Regs.SetWz(z80.Regs.Get R16.HL)

        cbOp z80 opcode Base

    and decodeAndRunED (z80: Z80) =
        let opcode = z80.ReadOpcode()
        edOp z80 opcode

    and decodeAndRunDD (z80: Z80) =
        let opcode = z80.ReadOpcode()

        if isIndirectMain opcode then
            let offset = signedImm (z80.ReadImmediate())
            // ld (ix+d), $nn fetches its operand differently; timing handled by the
            // C++ heuristic.
            let isImmediate = opcode = 0x36
            z80.PassTime(if isImmediate then 2 else 5)
            z80.Regs.SetWz((z80.Regs.Ix() + offset) &&& 0xFFFF)

        mainOp z80 opcode Ix

    and decodeAndRunFD (z80: Z80) =
        let opcode = z80.ReadOpcode()

        if isIndirectMain opcode then
            let offset = signedImm (z80.ReadImmediate())
            let isImmediate = opcode = 0x36
            z80.PassTime(if isImmediate then 2 else 5)
            z80.Regs.SetWz((z80.Regs.Iy() + offset) &&& 0xFFFF)

        mainOp z80 opcode Iy

    and decodeAndRunDDCB (z80: Z80) =
        let offset = signedImm (z80.ReadImmediate())
        z80.PassTime 1
        z80.Regs.SetWz((z80.Regs.Ix() + offset) &&& 0xFFFF)
        let opcode = z80.ReadOpcode()
        cbOp z80 opcode Ix

    and decodeAndRunFDCB (z80: Z80) =
        let offset = signedImm (z80.ReadImmediate())
        z80.PassTime 1
        z80.Regs.SetWz((z80.Regs.Iy() + offset) &&& 0xFFFF)
        let opcode = z80.ReadOpcode()
        cbOp z80 opcode Iy

    /// Top-level dispatch for the base table (build_execute_hl).
    let executeOpcode (z80: Z80) (opcode: int) =
        if isIndirectMain opcode then
            z80.Regs.SetWz(z80.Regs.Get R16.HL)

        mainOp z80 opcode Base

    let private install () = Z80Dispatch.Run <- executeOpcode

    /// Ensure the opcode dispatcher is installed. Call before any ExecuteOne.
    let EnsureInstalled () = install ()

    do install ()
