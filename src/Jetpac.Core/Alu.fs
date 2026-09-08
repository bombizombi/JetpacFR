namespace Jetpac.Core

/// Port of specbolt's Alu (z80/common/Alu.cpp). All functions return
/// (result, flags) with int values masked to 8/16 bits at the point of use.
module Alu =

    type Direction =
        | Left
        | Right

    /// Population count, portable (no BitOperations dependency for Fable).
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
        // F5/F3 are copied from the operand, not the result
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
            if ((lhs &&& 0xFFF) + (rhs &&& 0xFFF) + (if carryIn then 1 else 0)) > 0xFFF then
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

    /// Rotate through carry.
    let rotate8 (lhs: int) (direction: Direction) (carryIn: bool) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                ((lhs <<< 1) ||| (if carryIn then 1 else 0)) &&& 0xFF
            else
                ((lhs >>> 1) ||| (if carryIn then 0x80 else 0)) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

    /// Rotate circularly (through the carry flag).
    let rotateCircular8 (lhs: int) (direction: Direction) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                ((lhs <<< 1) ||| (if carryOut <> 0 then 0x01 else 0)) &&& 0xFF
            else
                ((lhs >>> 1) ||| (if carryOut <> 0 then 0x80 else 0)) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

    /// Fast rotate: flags preserved as in RLCA/RRCA.
    let fastRotate8 (lhs: int) (direction: Direction) (flags: Flags) : struct (int * Flags) =
        let struct (result, resultFlags) = rotate8 lhs direction flags.carry
        let preservedOriginal = Flags.Sign() ||| Flags.Zero() ||| Flags.Parity()
        let preservedResult = Flags.Carry() ||| Flags.Flag3() ||| Flags.Flag5()
        struct (result, (flags &&& preservedOriginal) ||| (resultFlags &&& preservedResult))

    /// Fast circular rotate: flags preserved as in RLCA/RRCA.
    let fastRotateCircular8 (lhs: int) (direction: Direction) (flags: Flags) : struct (int * Flags) =
        let struct (result, resultFlags) = rotateCircular8 lhs direction
        let preservedOriginal = Flags.Sign() ||| Flags.Zero() ||| Flags.Parity()
        let preservedResult = Flags.Carry() ||| Flags.Flag3() ||| Flags.Flag5()
        struct (result, (flags &&& preservedOriginal) ||| (resultFlags &&& preservedResult))

    /// SLL (Left) / SRL (Right).
    let shiftLogical8 (lhs: int) (direction: Direction) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                ((lhs <<< 1) ||| 1) &&& 0xFF
            else
                (lhs >>> 1) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))

    /// SLA (Left) / SRA (Right).
    let shiftArithmetic8 (lhs: int) (direction: Direction) : struct (int * Flags) =
        let carryOut = if direction = Left then lhs &&& 0x80 else lhs &&& 1

        let result =
            if direction = Left then
                (lhs <<< 1) &&& 0xFF
            else
                ((lhs >>> 1) ||| (lhs &&& 0x80)) &&& 0xFF

        struct (result, sz53_parity result ||| (if carryOut <> 0 then Flags.Carry() else Flags(0)))
