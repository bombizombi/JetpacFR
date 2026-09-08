namespace Jetpac2.Core

/// Port of JetpacFSharp (Jetpac.Core) Flags — specbolt's flag register.
/// Stored as an int; 8-bit operators mask to 0xFF. A struct so ALU operations
/// allocate nothing.
[<Struct>]
type Flags =
    val Value: int
    new(value: int) = { Value = value &&& 0xFF }

    member this.ToU8() = this.Value

    static member (&&&)(lhs: Flags, rhs: Flags) = Flags(lhs.Value &&& rhs.Value)
    static member (|||)(lhs: Flags, rhs: Flags) = Flags(lhs.Value ||| rhs.Value)
    static member (^^^)(lhs: Flags, rhs: Flags) = Flags(lhs.Value ^^^ rhs.Value)
    static member (~~~)(f: Flags) = Flags(~~~f.Value &&& 0xFF)

    static member Carry() = Flags(0x01)
    static member Subtract() = Flags(0x02)
    static member Parity() = Flags(0x04)
    static member Overflow() = Flags(0x04)
    static member Flag3() = Flags(0x08)
    static member HalfCarry() = Flags(0x10)
    static member Flag5() = Flags(0x20)
    static member Zero() = Flags(0x40)
    static member Sign() = Flags(0x80)

    member this.carry = this.Value &&& 0x01 <> 0
    member this.subtract = this.Value &&& 0x02 <> 0
    member this.parity = this.Value &&& 0x04 <> 0
    member this.overflow = this.Value &&& 0x04 <> 0
    member this.half_carry = this.Value &&& 0x10 <> 0
    member this.zero = this.Value &&& 0x40 <> 0
    member this.sign = this.Value &&& 0x80 <> 0

/// Port of JetpacFSharp Alu (specbolt z80/common/Alu.cpp).
module Alu =

    type Direction =
        | Left
        | Right

    let private popcount (v: int) =
        let v = v - ((v >>> 1) &&& 0x55555555)
        let v = (v &&& 0x33333333) + ((v >>> 2) &&& 0x33333333)
        (((v + (v >>> 4)) &&& 0x0F0F0F0F) * 0x01010101) >>> 24

    let private sz53_8 (value: int) =
        (Flags(value) &&& (Flags.Sign() ||| Flags.Flag3() ||| Flags.Flag5()))
        ||| (if value = 0 then Flags.Zero() else Flags(0))

    let private sz53_parity (value: int) =
        (Flags(value) &&& (Flags.Sign() ||| Flags.Flag3() ||| Flags.Flag5()))
        ||| (if value = 0 then Flags.Zero() else Flags(0))
        ||| (if popcount value % 2 = 0 then Flags.Parity() else Flags(0))

    let private mask53 = Flags.Flag5() ||| Flags.Flag3()

    let add8 (lhs: int) (rhs: int) (carryIn: bool) : struct (int * Flags) =
        let intermediate = (lhs &&& 0xFF) + (rhs &&& 0xFF) + (if carryIn then 1 else 0)
        let carry = if intermediate > 0xFF then Flags.Carry() else Flags()
        let result = intermediate &&& 0xFF

        let halfCarry =
            if ((lhs &&& 0xF) + (rhs &&& 0xF) + (if carryIn then 1 else 0)) > 0xF then
                Flags.HalfCarry()
            else
                Flags()

        let overflow =
            if ((lhs ^^^ result) &&& (rhs ^^^ result) &&& 0x80) <> 0 then
                Flags.Overflow()
            else
                Flags()

        struct (result, sz53_8 result ||| carry ||| halfCarry ||| overflow)

    let sub8 (lhs: int) (rhs: int) (carryIn: bool) : struct (int * Flags) =
        let struct (result, flags) = add8 (lhs &&& 0xFF) (~~~rhs &&& 0xFF) (not carryIn)
        struct (result, (flags ||| Flags.Subtract()) ^^^ Flags.Carry() ^^^ Flags.HalfCarry())

    let inc8 (lhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let result = (lhs + 1) &&& 0xFF

        let flags =
            sz53_8 result
            ||| (Flags(result) &&& mask53)
            ||| (Flags(result ^^^ lhs) &&& Flags.HalfCarry())
            ||| (if result = 0x80 then Flags.Overflow() else Flags(0))
            ||| (currentFlags &&& Flags.Carry())

        struct (result, flags)

    let dec8 (lhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let result = (lhs - 1) &&& 0xFF

        let flags =
            Flags.Subtract()
            ||| sz53_8 result
            ||| (Flags(result) &&& mask53)
            ||| (if result = 0x7F then Flags.Overflow() else Flags(0))
            ||| (Flags(result ^^^ lhs) &&& Flags.HalfCarry())
            ||| (currentFlags &&& Flags.Carry())

        struct (result, flags)

    let cmp8 (lhs: int) (rhs: int) : struct (int * Flags) =
        let struct (_, flags) = sub8 (lhs &&& 0xFF) (rhs &&& 0xFF) false
        (lhs &&& 0xFF, (flags &&& ~~~mask53) ||| (Flags(rhs &&& 0xFF) &&& mask53))

    let add16 (lhs: int) (rhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let intermediate = (lhs &&& 0xFFFF) + (rhs &&& 0xFFFF)
        let carry = if intermediate > 0xFFFF then Flags.Carry() else Flags()
        let result = intermediate &&& 0xFFFF

        let halfCarry =
            if ((lhs &&& 0xFFF) + (rhs &&& 0xFFF)) > 0xFFF then
                Flags.HalfCarry()
            else
                Flags()

        let flags35 = Flags((result >>> 8) &&& 0xFF) &&& (Flags.Flag5() ||| Flags.Flag3())

        struct (result,
                (currentFlags &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Parity()))
                ||| carry
                ||| halfCarry
                ||| flags35)

    let sub16 (lhs: int) (rhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let struct (result, flags) = add16 (lhs &&& 0xFFFF) (~~~rhs &&& 0xFFFF) currentFlags
        struct (result, (flags ||| Flags.Subtract()) ^^^ Flags.Carry() ^^^ Flags.HalfCarry())

    let adc16 (lhs: int) (rhs: int) (carryIn: bool) : struct (int * Flags) =
        let intermediate = (lhs &&& 0xFFFF) + (rhs &&& 0xFFFF) + (if carryIn then 1 else 0)
        let carry = if intermediate > 0xFFFF then Flags.Carry() else Flags()
        let result = intermediate &&& 0xFFFF

        let halfCarry =
            if ((lhs &&& 0xFFF) + (rhs &&& 0xFFF)) > 0xFFF then
                Flags.HalfCarry()
            else
                Flags()

        let flags35 = Flags((result >>> 8) &&& 0xFF) &&& (Flags.Flag5() ||| Flags.Flag3())
        let negative = if result &&& 0x8000 <> 0 then Flags.Sign() else Flags()
        let zero = if result = 0 then Flags.Zero() else Flags()

        let overflow =
            if ((lhs ^^^ result) &&& (rhs ^^^ result) &&& 0x8000) <> 0 then
                Flags.Overflow()
            else
                Flags()

        struct (result, carry ||| halfCarry ||| flags35 ||| negative ||| zero ||| overflow)

    let sbc16 (lhs: int) (rhs: int) (carryIn: bool) : struct (int * Flags) =
        let struct (result, flags) =
            adc16 (lhs &&& 0xFFFF) (~~~rhs &&& 0xFFFF) (not carryIn)

        struct (result, (flags ||| Flags.Subtract()) ^^^ Flags.Carry() ^^^ Flags.HalfCarry())

    let xor8 (lhs: int) (rhs: int) : struct (int * Flags) =
        let result = (lhs ^^^ rhs) &&& 0xFF
        struct (result, sz53_parity result)

    let or8 (lhs: int) (rhs: int) : struct (int * Flags) =
        let result = (lhs ||| rhs) &&& 0xFF
        struct (result, sz53_parity result)

    let and8 (lhs: int) (rhs: int) : struct (int * Flags) =
        let result = (lhs &&& rhs) &&& 0xFF
        struct (result, sz53_parity result ||| Flags.HalfCarry())

    let daa (lhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let signAdjust = if currentFlags.subtract then -1 else 1
        let lowNibbleCarry = (lhs &&& 0xF) > 0x09 || currentFlags.half_carry
        let highNibbleCarry = lhs > 0x99 || currentFlags.carry

        let result =
            (lhs
             + (if lowNibbleCarry then 0x06 * signAdjust else 0)
             + (if highNibbleCarry then 0x60 * signAdjust else 0))
            &&& 0xFF

        let carry = if lhs > 0x99 then Flags.Carry() else Flags()

        let halfCarry =
            if (lhs ^^^ result) &&& 0x10 <> 0 then
                Flags.HalfCarry()
            else
                Flags()

        let preservedFlags = currentFlags &&& (Flags.Carry() ||| Flags.Subtract())
        struct (result, sz53_parity result ||| carry ||| halfCarry ||| preservedFlags)

    let cpl (lhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let result = lhs ^^^ 0xFF

        let preservedFlags =
            currentFlags
            &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Parity() ||| Flags.Carry())

        let setFlags = Flags.HalfCarry() ||| Flags.Subtract()
        let flagsFromResult = Flags(result) &&& (Flags.Flag3() ||| Flags.Flag5())
        struct (result, preservedFlags ||| setFlags ||| flagsFromResult)

    let scf (lhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let preserved = currentFlags &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Parity())
        let flags53 = Flags(lhs) &&& (Flags.Flag3() ||| Flags.Flag5())
        (lhs &&& 0xFF, preserved ||| flags53 ||| Flags.Carry())

    let ccf (lhs: int) (currentFlags: Flags) : struct (int * Flags) =
        let preserved =
            currentFlags
            &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Parity() ||| Flags.Carry())

        let flags53 = Flags(lhs) &&& (Flags.Flag3() ||| Flags.Flag5())
        let otherFlag = if currentFlags.carry then Flags.HalfCarry() else Flags()
        (lhs &&& 0xFF, (preserved ||| flags53 ||| otherFlag) ^^^ Flags.Carry())

    let bit (lhs: int) (rhs: int) (currentFlags: Flags) (busNoise: int) : Flags =
        let flagsPersisted = currentFlags &&& Flags.Carry()
        let flagsFromValue = Flags(busNoise &&& 0xFF) &&& (Flags.Flag3() ||| Flags.Flag5())

        let flagsFromSign =
            if (rhs &&& lhs &&& 0x80) <> 0 then
                Flags.Sign()
            else
                Flags()

        let flagsFromBit =
            if lhs &&& rhs <> 0 then
                Flags(0)
            else
                (Flags.Zero() ||| Flags.Parity())

        flagsPersisted
        ||| flagsFromValue
        ||| flagsFromSign
        ||| flagsFromBit
        ||| Flags.HalfCarry()

    let parityFlagsFor (value: int) : Flags = sz53_parity (value &&& 0xFF)

    let iff2FlagsFor (value: int) (currentFlags: Flags) (iff2: bool) : Flags =
        let flagsPersisted = currentFlags &&& Flags.Carry()
        let flagsFromValue = Flags(value &&& 0xFF) &&& (Flags.Flag3() ||| Flags.Flag5())
        let flagsFromIff = if iff2 then Flags.Parity() else Flags()
        sz53_8 (value &&& 0xFF) ||| flagsPersisted ||| flagsFromValue ||| flagsFromIff

    let rotate8 (lhs: int) (direction: Direction) (carryIn: bool) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                ((lhs <<< 1) ||| (if carryIn then 1 else 0)) &&& 0xFF
            else
                ((lhs >>> 1) ||| (if carryIn then 0x80 else 0)) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

    let rotateCircular8 (lhs: int) (direction: Direction) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                ((lhs <<< 1) ||| (if carryOut <> 0 then 0x01 else 0)) &&& 0xFF
            else
                ((lhs >>> 1) ||| (if carryOut <> 0 then 0x80 else 0)) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

    let fastRotate8 (lhs: int) (direction: Direction) (flags: Flags) : struct (int * Flags) =
        let struct (result, resultFlags) = rotate8 lhs direction flags.carry
        let preservedOriginal = Flags.Sign() ||| Flags.Zero() ||| Flags.Parity()
        let preservedResult = Flags.Carry() ||| Flags.Flag3() ||| Flags.Flag5()
        struct (result, (flags &&& preservedOriginal) ||| (resultFlags &&& preservedResult))

    let fastRotateCircular8 (lhs: int) (direction: Direction) (flags: Flags) : struct (int * Flags) =
        let struct (result, resultFlags) = rotateCircular8 lhs direction
        let preservedOriginal = Flags.Sign() ||| Flags.Zero() ||| Flags.Parity()
        let preservedResult = Flags.Carry() ||| Flags.Flag3() ||| Flags.Flag5()
        struct (result, (flags &&& preservedOriginal) ||| (resultFlags &&& preservedResult))

    let shiftLogical8 (lhs: int) (direction: Direction) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                ((lhs <<< 1) ||| 1) &&& 0xFF
            else
                (lhs >>> 1) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

    let shiftArithmetic8 (lhs: int) (direction: Direction) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                (lhs <<< 1) &&& 0xFF
            else
                ((lhs >>> 1) ||| (lhs &&& 0x80)) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

/// 8-bit register selector (port of JetpacFSharp Registers.fs).
type R8 =
    | A
    | F
    | B
    | C
    | D
    | E
    | H
    | L
    | A_
    | F_
    | B_
    | C_
    | D_
    | E_
    | H_
    | L_
    | SPH
    | SPL
    | IXH
    | IXL
    | IYH
    | IYL

/// 16-bit register pair selector.
type R16 =
    | AF
    | BC
    | DE
    | HL
    | AF_
    | BC_
    | DE_
    | HL_
    | SP
    | IX
    | IY

/// A high/low byte pair. All values are `int`; 8-bit values are masked to
/// 0xFF and 16-bit values to 0xFFFF at the point of use.
type RegPair() =
    let mutable low = 0xFF
    let mutable high = 0xFF

    member this.High() = high
    member this.SetHigh(v: int) = high <- v &&& 0xFF
    member this.Low() = low
    member this.SetLow(v: int) = low <- v &&& 0xFF
    member this.HighLow() = ((high <<< 8) ||| low) &&& 0xFFFF

    member this.SetHighLow(v: int) =
        high <- (v >>> 8) &&& 0xFF
        low <- v &&& 0xFF

/// Port of specbolt's RegisterFile.
type RegisterFile() =
    let regs = Array.init 11 (fun _ -> RegPair())
    let mutable wz_ = 0xFFFF
    let mutable pc_ = 0
    let mutable r_ = 0
    let mutable i_ = 0

    static member private PairIndex(r8: R8) =
        match r8 with
        | A
        | F -> 0
        | B
        | C -> 1
        | D
        | E -> 2
        | H
        | L -> 3
        | A_
        | F_ -> 4
        | B_
        | C_ -> 5
        | D_
        | E_ -> 6
        | H_
        | L_ -> 7
        | SPH
        | SPL -> 8
        | IXH
        | IXL -> 9
        | IYH
        | IYL -> 10

    static member private IsHigh(r8: R8) =
        match r8 with
        | A
        | B
        | D
        | H
        | A_
        | B_
        | D_
        | H_
        | SPH
        | IXH
        | IYH -> true
        | _ -> false

    static member private PairIndex16(r16: R16) =
        match r16 with
        | AF -> 0
        | BC -> 1
        | DE -> 2
        | HL -> 3
        | AF_ -> 4
        | BC_ -> 5
        | DE_ -> 6
        | HL_ -> 7
        | SP -> 8
        | IX -> 9
        | IY -> 10

    member private this.RegFor(r8: R8) = regs[RegisterFile.PairIndex r8]
    member private this.RegFor(r16: R16) = regs[RegisterFile.PairIndex16 r16]

    member this.Get(r8: R8) : int =
        let pair = this.RegFor r8
        if RegisterFile.IsHigh r8 then pair.High() else pair.Low()

    member this.Set(r8: R8, value: int) =
        let pair = this.RegFor r8

        if RegisterFile.IsHigh r8 then
            pair.SetHigh value
        else
            pair.SetLow value

    member this.Get(r16: R16) : int = this.RegFor(r16).HighLow()
    member this.Set(r16: R16, value: int) = this.RegFor(r16).SetHighLow value

    member this.Ix() = this.Get R16.IX
    member this.Iy() = this.Get R16.IY
    member this.Sp() = this.Get R16.SP
    member this.SetSp(v: int) = this.Set(R16.SP, v)
    member this.Pc() = pc_
    member this.SetPc(v: int) = pc_ <- v &&& 0xFFFF
    member this.R() = r_
    member this.SetR(v: int) = r_ <- v &&& 0xFF
    member this.I() = i_
    member this.SetI(v: int) = i_ <- v &&& 0xFF
    member this.Wz() = wz_
    member this.SetWz(v: int) = wz_ <- v &&& 0xFFFF

    member this.Ex(lhs: R16, rhs: R16) =
        let l = this.RegFor lhs
        let r = this.RegFor rhs
        let tmp = l.HighLow()
        l.SetHighLow(r.HighLow())
        r.SetHighLow tmp

    member this.Exx() =
        this.Ex(R16.BC, R16.BC_)
        this.Ex(R16.DE, R16.DE_)
        this.Ex(R16.HL, R16.HL_)

#nowarn "40" // intentional self-referential scheduler task (videoTask)

/// Port of JetpacFSharp (Jetpac.Core) Z80: the fetch/IO/interrupt model and
/// the per-instruction step driver. Instructions come from the Generated
/// page table (or, for converted routines, the Override hook), so there is
/// no opcode decoder here.
/// Timestamped externally visible port effect from the copied machine.
type HardwareEvent =
    { Tick: int64
      Port: int
      Value: int
      Border: int
      Beeper: bool }

/// Invalidation input for decoded code/cache layers.
type MemoryWriteEvent =
    { Tick: int64
      Address: int
      OldValue: byte
      NewValue: byte }

type Machine() as self =
    let memory = Array.zeroCreate<byte> 0x10000
    let keyboard = Keyboard()
    let scheduler = Scheduler()
    let video = VideoScreen(memory)
    let regs = RegisterFile()
    let inHandlers = System.Collections.Generic.List<int -> int option>()
    let outHandlers = System.Collections.Generic.List<int -> int -> unit>()
    let hardwareEvents = System.Collections.Generic.List<HardwareEvent>()

    let memoryWriteHandlers =
        System.Collections.Generic.List<MemoryWriteEvent -> unit>()

    let beeperTrace = System.Collections.Generic.List<int64 * bool>()
    let mutable beeperLevel = false
    let mutable earLevel = false
    let mutable border = 0
    let mutable halted_ = false
    let mutable irqPending_ = false
    let mutable iff1_ = false
    let mutable iff2_ = false
    let mutable irqMode_ = 0
    let mutable overrideHook: int -> (Machine -> unit) option = fun _ -> None
    let mutable frameEnd = 0L

    // Video task: every 224 cycles a scanline passes; the 312-line wrap sets
    // the interrupt pending flag (port of Spectrum48 wiring + Video.Poll).
    let rec videoTask =
        SchedulerTask(fun _ ->
            if video.NextScanLine() then
                irqPending_ <- true

            scheduler.Schedule(videoTask, VideoConstants.CyclesPerScanLine))

    do
        scheduler.Schedule(videoTask, 0)

        outHandlers.Add(fun port value ->
            if port &&& 0xFF = 0xFE then
                video.SetBorder(value &&& 7)
                border <- value &&& 7
                let lvl = value &&& 0x10 <> 0

                if lvl <> beeperLevel then
                    beeperLevel <- lvl
                    beeperTrace.Add(scheduler.Cycles, lvl))

        inHandlers.Add(fun port -> keyboard.In port)

        inHandlers.Add(fun port ->
            if port &&& 1 <> 0 then
                None
            else
                Some(0xBF ||| (if earLevel then 0x40 else 0)))

    member this.Memory = memory
    member this.Regs = regs
    member this.Video = video
    member this.Keyboard = keyboard
    member this.Border = border
    member this.BeeperLevel = beeperLevel
    member this.BeeperTrace = beeperTrace
    member this.HardwareEvents = hardwareEvents
    member this.Halted = halted_
    member this.IrqPending = irqPending_

    member this.Iff1
        with get () = iff1_
        and set v = iff1_ <- v

    member this.Iff2
        with get () = iff2_
        and set v = iff2_ <- v

    member this.IrqMode
        with get () = irqMode_
        and set v = irqMode_ <- v

    /// Address -> semantic-instruction hook for converted (M5) routines.
    member this.Override
        with get () = overrideHook
        and set v = overrideHook <- v

    /// Absolute cycle at which the current frame ends (next scanline wrap).
    member this.FrameEnd
        with get () = frameEnd
        and set v = frameEnd <- v

    member this.CycleCount() = scheduler.Cycles
    member this.PassTime(tstates: int) = scheduler.Tick(int64 tstates)

    member this.Interrupt() = irqPending_ <- true
    member this.AddInHandler(handler: int -> int option) = inHandlers.Add handler
    member this.AddOutHandler(handler: int -> int -> unit) = outHandlers.Add handler
    member this.AddMemoryWriteHandler(handler: MemoryWriteEvent -> unit) = memoryWriteHandlers.Add handler

    member this.In(port: int) : int =
        let mutable combined = 0xFF

        for handler in inHandlers do
            match handler port with
            | Some v -> combined <- combined &&& v
            | None -> ()

        combined

    member this.Out(port: int, value: int) =
        for handler in outHandlers do
            handler port value

        hardwareEvents.Add
            { Tick = scheduler.Cycles
              Port = port &&& 0xFFFF
              Value = value &&& 0xFF
              Border = border
              Beeper = beeperLevel }

    member this.DrainHardwareEvents() : HardwareEvent list =
        let items = hardwareEvents |> Seq.toList
        hardwareEvents.Clear()
        items


    member this.Write(address: int, value: int) =
        this.PassTime 3
        let addr = address &&& 0xFFFF
        let oldValue = memory[addr]
        let newValue = byte (value &&& 0xFF)
        memory[addr] <- newValue

        if oldValue <> newValue then
            let event =
                { Tick = scheduler.Cycles
                  Address = addr
                  OldValue = oldValue
                  NewValue = newValue }

            for handler in memoryWriteHandlers do
                handler event

    member this.SetKey(row: int, bit: int, pressed: bool) = keyboard.SetKey(row, bit, pressed)

    member this.Halt() =
        halted_ <- true
        regs.SetPc((regs.Pc() - 1) &&& 0xFFFF)

    member this.Branch(offset: int) =
        regs.SetPc((regs.Pc() + offset) &&& 0xFFFF)

    /// One opcode/prefix byte: refresh register + 4 T-states + PC advance
    /// (mirrors Z80.ReadOpcode; the instruction bytes are decoded statically,
    /// so the memory read itself is skipped — the generated guard checks the
    /// opcode byte still matches).
    member this.Fetch() =
        this.PassTime 3
        regs.SetPc((regs.Pc() + 1) &&& 0xFFFF)
        regs.SetR((regs.R() &&& 0x80) ||| ((regs.R() + 1) &&& 0x7F))
        this.PassTime 1

    member this.ReadImm() : int =
        this.PassTime 3
        let addr = regs.Pc()
        regs.SetPc((addr + 1) &&& 0xFFFF)
        int memory[addr]

    member this.ReadImm16() : int =
        let low = this.ReadImm()
        let high = this.ReadImm()
        ((high <<< 8) ||| low) &&& 0xFFFF


    member this.Read(address: int) : int =
        this.PassTime 3
        int memory[address &&& 0xFFFF]

    member this.Pop8() : int =
        let value = this.Read(regs.Sp())
        regs.SetSp((regs.Sp() + 1) &&& 0xFFFF)
        value

    member this.Pop16() : int =
        let low = this.Pop8()
        let high = this.Pop8()
        ((high <<< 8) ||| low) &&& 0xFFFF

    member this.Push8(value: int) =
        regs.SetSp((regs.Sp() - 1) &&& 0xFFFF)
        this.Write(regs.Sp(), value &&& 0xFF)

    member this.Push16(value: int) =
        this.Push8((value >>> 8) &&& 0xFF)
        this.Push8(value &&& 0xFF)

    member this.Flags() = Flags(regs.Get R8.F)
    member this.SetFlags(flags: Flags) = regs.Set(R8.F, flags.ToU8())

    member private this.HandleInterrupt() =
        irqPending_ <- false

        if not iff1_ then
            ()
        else
            if halted_ then
                halted_ <- false
                regs.SetPc((regs.Pc() + 1) &&& 0xFFFF)

            iff1_ <- false
            iff2_ <- false
            this.PassTime 7
            regs.SetSp((regs.Sp() - 2) &&& 0xFFFF)
            memory[regs.Sp()] <- byte (regs.Pc() &&& 0xFF)
            memory[(regs.Sp() + 1) &&& 0xFFFF] <- byte ((regs.Pc() >>> 8) &&& 0xFF)

            match irqMode_ with
            | 0
            | 1 -> regs.SetPc 0x38
            | 2 ->
                let addr = 0xFF ||| ((regs.I() <<< 8) &&& 0xFF00)
                let vector = (int memory[addr]) ||| ((int memory[(addr + 1) &&& 0xFFFF]) <<< 8)
                regs.SetPc(vector &&& 0xFFFF)
            | _ -> failwith "Inconceivable interrupt mode"

    /// Execute one instruction (mirrors Z80.ExecuteOne).
    member this.Step() =
        if irqPending_ then
            this.HandleInterrupt()

        if halted_ then
            this.PassTime 1
        else
            match overrideHook (regs.Pc()) with
            | Some f -> f this
            | None -> Machine.GeneratedStep this

    /// Installed by Generated.fs (same pattern as Z80Ops/Z80Dispatch).
    static member val GeneratedStep: Machine -> unit =
        (fun _ -> failwith "Generated instruction table not installed") with get, set

    /// Reset the scheduler clock and schedule the video task for the next fire
    /// after the given absolute cycle (used by LoadState).
    member this.ResetClock(cycles: int64, videoNextTime: int64) =
        scheduler.Reset cycles
        scheduler.Schedule(videoTask, videoNextTime - cycles)

    member this.LoadState(bytes: byte[], regsText: string) =
        Array.blit bytes 0 memory 0 (min bytes.Length 0x10000)
        let kv = System.Collections.Generic.Dictionary<string, string>()

        for line in regsText.Split('\n') do
            let line = line.Trim()

            if line.Length > 0 then
                match line.IndexOf('=') with
                | -1 -> ()
                | i -> kv[line.Substring(0, i).Trim()] <- line.Substring(i + 1).Trim()

        let hex (k: string) (def: int) =
            match kv.TryGetValue k with
            | true, v -> System.Convert.ToInt32(v.Replace("0x", ""), 16)
            | _ -> def

        let boolv (k: string) (def: bool) =
            match kv.TryGetValue k with
            | true, v -> v = "true" || v = "1"
            | _ -> def

        regs.Set(R16.AF, hex "af" 0)
        regs.Set(R16.BC, hex "bc" 0)
        regs.Set(R16.DE, hex "de" 0)
        regs.Set(R16.HL, hex "hl" 0)
        regs.Set(R16.AF_, hex "af2" 0)
        regs.Set(R16.BC_, hex "bc2" 0)
        regs.Set(R16.DE_, hex "de2" 0)
        regs.Set(R16.HL_, hex "hl2" 0)
        regs.Set(R16.IX, hex "ix" 0)
        regs.Set(R16.IY, hex "iy" 0)
        regs.SetSp(hex "sp" 0)
        regs.SetPc(hex "pc" 0)
        regs.SetI(hex "i" 0)
        regs.SetR(hex "r" 0)
        regs.SetWz 0xFFFF
        iff1_ <- boolv "iff1" false
        iff2_ <- boolv "iff2" false
        irqMode_ <- hex "im" 0
        halted_ <- boolv "halted" false
        irqPending_ <- false
        border <- hex "border" 0
        video.SetBorder border
        beeperLevel <- boolv "beeper" false
        earLevel <- boolv "tapeEar" false
        let cycles = System.Int64.Parse(kv["cycles"])
        let videoNextTime = System.Int64.Parse(kv["videoNextTime"])
        self.ResetClock(cycles, videoNextTime)
        // Scanline + flash state at capture, derived from the video schedule.
        let f = videoNextTime / 224L
        let scanline = int (f % 312L)
        let wraps = f / 312L
        video.SetState(scanline, int (wraps % 16L), (wraps / 16L) % 2L = 1L)
        beeperTrace.Clear()
        frameEnd <- (System.Int64.Parse(kv["nextWrap"]))
