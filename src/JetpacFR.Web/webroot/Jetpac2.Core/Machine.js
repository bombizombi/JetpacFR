
import { FSharpRef, Union, Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { uint8_type, string_type, record_type, bool_type, int32_type, int64_type, union_type, class_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { clear, curry2, disposeSafe, getEnumerator, Exception, Lazy, defaultOf, equals } from "../fable_modules/fable-library-js.5.17.0/Util.js";
import { copy, copyTo, item, initialize } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { Keyboard__SetKey_289F56A, Keyboard__In_Z524259A4, Keyboard_$ctor } from "./Keyboard.js";
import { SchedulerTask_$ctor_43314A15, Scheduler__Reset_Z524259C1, Scheduler__Tick_Z524259C1, Scheduler__get_Cycles, Scheduler__Schedule_79A1460, Scheduler_$ctor } from "./Timing.js";
import { VideoConstants_CyclesPerScanLine, VideoScreen__NextScanLine, VideoScreen__SetState_289F56A, VideoScreen__SetBorder_Z524259A4, VideoScreen_$ctor_Z3F6BC7B1 } from "./Screen.js";
import { op_Addition, op_Multiply, equals as equals_1, op_Modulus, toInt32_unchecked, op_Division, op_Subtraction, fromInt32, toInt64_unchecked } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { getItemFromDict, tryGetValue } from "../fable_modules/fable-library-js.5.17.0/MapUtil.js";
import { toList } from "../fable_modules/fable-library-js.5.17.0/Seq.js";
import { min } from "../fable_modules/fable-library-js.5.17.0/Double.js";
import { printf, toText, replace, substring } from "../fable_modules/fable-library-js.5.17.0/String.js";
import { parse } from "../fable_modules/fable-library-js.5.17.0/Int32.js";
import { parse as parse_1 } from "../fable_modules/fable-library-js.5.17.0/Long.js";

/**
 * Port of JetpacFSharp (Jetpac.Core) Flags — specbolt's flag register.
 * Stored as an int; 8-bit operators mask to 0xFF. A struct so ALU operations
 * allocate nothing.
 */
export class Flags extends Record {
    constructor(Value) {
        super();
        this.Value = (Value | 0);
    }
}

export function Flags_$reflection() {
    return class_type("Jetpac2.Core.Flags", undefined, Flags, class_type("System.ValueType"));
}

export function Flags_$ctor_Z524259A4(value) {
    return new Flags(value & 255);
}

export function Flags__ToU8(this$) {
    return this$.Value | 0;
}

export function Flags_op_BitwiseAnd_603E7D40(lhs, rhs) {
    return Flags_$ctor_Z524259A4(lhs.Value & rhs.Value);
}

export function Flags_op_BitwiseOr_603E7D40(lhs, rhs) {
    return Flags_$ctor_Z524259A4(lhs.Value | rhs.Value);
}

export function Flags_op_ExclusiveOr_603E7D40(lhs, rhs) {
    return Flags_$ctor_Z524259A4(lhs.Value ^ rhs.Value);
}

export function Flags_op_LogicalNot_2901ED1A(f) {
    return Flags_$ctor_Z524259A4(~f.Value & 255);
}

export function Flags_Carry() {
    return Flags_$ctor_Z524259A4(1);
}

export function Flags_Subtract() {
    return Flags_$ctor_Z524259A4(2);
}

export function Flags_Parity() {
    return Flags_$ctor_Z524259A4(4);
}

export function Flags_Overflow() {
    return Flags_$ctor_Z524259A4(4);
}

export function Flags_Flag3() {
    return Flags_$ctor_Z524259A4(8);
}

export function Flags_HalfCarry() {
    return Flags_$ctor_Z524259A4(16);
}

export function Flags_Flag5() {
    return Flags_$ctor_Z524259A4(32);
}

export function Flags_Zero() {
    return Flags_$ctor_Z524259A4(64);
}

export function Flags_Sign() {
    return Flags_$ctor_Z524259A4(128);
}

export function Flags__get_carry(this$) {
    return (this$.Value & 1) !== 0;
}

export function Flags__get_subtract(this$) {
    return (this$.Value & 2) !== 0;
}

export function Flags__get_parity(this$) {
    return (this$.Value & 4) !== 0;
}

export function Flags__get_overflow(this$) {
    return (this$.Value & 4) !== 0;
}

export function Flags__get_half_carry(this$) {
    return (this$.Value & 16) !== 0;
}

export function Flags__get_zero(this$) {
    return (this$.Value & 64) !== 0;
}

export function Flags__get_sign(this$) {
    return (this$.Value & 128) !== 0;
}

export class Alu_Direction extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Left", "Right"];
    }
    static Left = new Alu_Direction(0, []);
    static Right = new Alu_Direction(1, []);
}

export function Alu_Direction_$reflection() {
    return union_type("Jetpac2.Core.Alu.Direction", [], Alu_Direction, () => [[], []]);
}

function Alu_popcount(v) {
    const v_1 = (v - ((v >> 1) & 1431655765)) | 0;
    const v_2 = ((v_1 & 858993459) + ((v_1 >> 2) & 858993459)) | 0;
    return ((((v_2 + (v_2 >> 4)) & 252645135) * 16843009) >> 24) | 0;
}

function Alu_sz53_8(value) {
    return Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(value), Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Flag3()), Flags_Flag5())), (value === 0) ? Flags_Zero() : Flags_$ctor_Z524259A4(0));
}

function Alu_sz53_parity(value) {
    return Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(value), Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Flag3()), Flags_Flag5())), (value === 0) ? Flags_Zero() : Flags_$ctor_Z524259A4(0)), ((Alu_popcount(value) % 2) === 0) ? Flags_Parity() : Flags_$ctor_Z524259A4(0));
}

const Alu_mask53 = Flags_op_BitwiseOr_603E7D40(Flags_Flag5(), Flags_Flag3());

export function Alu_add8(lhs, rhs, carryIn) {
    const intermediate = (((lhs & 255) + (rhs & 255)) + (carryIn ? 1 : 0)) | 0;
    const carry = (intermediate > 255) ? Flags_Carry() : (new Flags(0));
    const result = (intermediate & 255) | 0;
    const halfCarry = ((((lhs & 15) + (rhs & 15)) + (carryIn ? 1 : 0)) > 15) ? Flags_HalfCarry() : (new Flags(0));
    const overflow = ((((lhs ^ result) & (rhs ^ result)) & 128) !== 0) ? Flags_Overflow() : (new Flags(0));
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Alu_sz53_8(result), carry), halfCarry), overflow)];
}

export function Alu_sub8(lhs, rhs, carryIn) {
    const patternInput = Alu_add8(lhs & 255, ~rhs & 255, !carryIn);
    return [patternInput[0], Flags_op_ExclusiveOr_603E7D40(Flags_op_BitwiseOr_603E7D40(patternInput[1], Flags_Subtract()), Flags_op_ExclusiveOr_603E7D40(Flags_Carry(), Flags_HalfCarry()))];
}

export function Alu_inc8(lhs, currentFlags) {
    const result = ((lhs + 1) & 255) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Alu_sz53_8(result), Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(result), Alu_mask53)), Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(result ^ lhs), Flags_HalfCarry())), (result === 128) ? Flags_Overflow() : Flags_$ctor_Z524259A4(0)), Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_Carry()))];
}

export function Alu_dec8(lhs, currentFlags) {
    const result = ((lhs - 1) & 255) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Subtract(), Alu_sz53_8(result)), Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(result), Alu_mask53)), (result === 127) ? Flags_Overflow() : Flags_$ctor_Z524259A4(0)), Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(result ^ lhs), Flags_HalfCarry())), Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_Carry()))];
}

export function Alu_cmp8(lhs, rhs) {
    return [lhs & 255, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Alu_sub8(lhs & 255, rhs & 255, false)[1], Flags_op_LogicalNot_2901ED1A(Alu_mask53)), Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(rhs & 255), Alu_mask53))];
}

export function Alu_add16(lhs, rhs, currentFlags) {
    const intermediate = ((lhs & 65535) + (rhs & 65535)) | 0;
    const carry = (intermediate > 65535) ? Flags_Carry() : (new Flags(0));
    const result = (intermediate & 65535) | 0;
    const halfCarry = (((lhs & 4095) + (rhs & 4095)) > 4095) ? Flags_HalfCarry() : (new Flags(0));
    const flags35 = Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4((result >> 8) & 255), Flags_op_BitwiseOr_603E7D40(Flags_Flag5(), Flags_Flag3()));
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Parity())), carry), halfCarry), flags35)];
}

export function Alu_sub16(lhs, rhs, currentFlags) {
    const patternInput = Alu_add16(lhs & 65535, ~rhs & 65535, currentFlags);
    return [patternInput[0], Flags_op_ExclusiveOr_603E7D40(Flags_op_BitwiseOr_603E7D40(patternInput[1], Flags_Subtract()), Flags_op_ExclusiveOr_603E7D40(Flags_Carry(), Flags_HalfCarry()))];
}

export function Alu_adc16(lhs, rhs, carryIn) {
    const intermediate = (((lhs & 65535) + (rhs & 65535)) + (carryIn ? 1 : 0)) | 0;
    const carry = (intermediate > 65535) ? Flags_Carry() : (new Flags(0));
    const result = (intermediate & 65535) | 0;
    const halfCarry = ((((lhs & 4095) + (rhs & 4095)) + (carryIn ? 1 : 0)) > 4095) ? Flags_HalfCarry() : (new Flags(0));
    const flags35 = Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4((result >> 8) & 255), Flags_op_BitwiseOr_603E7D40(Flags_Flag5(), Flags_Flag3()));
    const negative = ((result & 32768) !== 0) ? Flags_Sign() : (new Flags(0));
    const zero = (result === 0) ? Flags_Zero() : (new Flags(0));
    const overflow = ((((lhs ^ result) & (rhs ^ result)) & 32768) !== 0) ? Flags_Overflow() : (new Flags(0));
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(carry, halfCarry), flags35), negative), zero), overflow)];
}

export function Alu_sbc16(lhs, rhs, carryIn) {
    const patternInput = Alu_adc16(lhs & 65535, ~rhs & 65535, !carryIn);
    return [patternInput[0], Flags_op_ExclusiveOr_603E7D40(Flags_op_BitwiseOr_603E7D40(patternInput[1], Flags_Subtract()), Flags_op_ExclusiveOr_603E7D40(Flags_Carry(), Flags_HalfCarry()))];
}

export function Alu_xor8(lhs, rhs) {
    const result = ((lhs ^ rhs) & 255) | 0;
    return [result, Alu_sz53_parity(result)];
}

export function Alu_or8(lhs, rhs) {
    const result = ((lhs | rhs) & 255) | 0;
    return [result, Alu_sz53_parity(result)];
}

export function Alu_and8(lhs, rhs) {
    const result = ((lhs & rhs) & 255) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Alu_sz53_parity(result), Flags_HalfCarry())];
}

export function Alu_daa(lhs, currentFlags) {
    const signAdjust = (Flags__get_subtract(currentFlags) ? -1 : 1) | 0;
    const lowNibbleCarry = ((lhs & 15) > 9) ? true : Flags__get_half_carry(currentFlags);
    const highNibbleCarry = (lhs > 153) ? true : Flags__get_carry(currentFlags);
    const result = (((lhs + (lowNibbleCarry ? (6 * signAdjust) : 0)) + (highNibbleCarry ? (96 * signAdjust) : 0)) & 255) | 0;
    const carry = (lhs > 153) ? Flags_Carry() : (new Flags(0));
    const halfCarry = (((lhs ^ result) & 16) !== 0) ? Flags_HalfCarry() : (new Flags(0));
    const preservedFlags = Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_op_BitwiseOr_603E7D40(Flags_Carry(), Flags_Subtract()));
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Alu_sz53_parity(result), carry), halfCarry), preservedFlags)];
}

export function Alu_cpl(lhs, currentFlags) {
    const result = (lhs ^ 255) | 0;
    const preservedFlags = Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Parity()), Flags_Carry()));
    const setFlags = Flags_op_BitwiseOr_603E7D40(Flags_HalfCarry(), Flags_Subtract());
    const flagsFromResult = Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(result), Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()));
    return [result, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags, setFlags), flagsFromResult)];
}

export function Alu_scf(lhs, currentFlags) {
    return [lhs & 255, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Parity())), Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(lhs), Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()))), Flags_Carry())];
}

export function Alu_ccf(lhs, currentFlags) {
    const preserved = Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Parity()), Flags_Carry()));
    const flags53 = Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(lhs), Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()));
    const otherFlag = Flags__get_carry(currentFlags) ? Flags_HalfCarry() : (new Flags(0));
    return [lhs & 255, Flags_op_ExclusiveOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preserved, flags53), otherFlag), Flags_Carry())];
}

export function Alu_bit(lhs, rhs, currentFlags, busNoise) {
    const flagsPersisted = Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_Carry());
    const flagsFromValue = Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(busNoise & 255), Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()));
    const flagsFromSign = (((rhs & lhs) & 128) !== 0) ? Flags_Sign() : (new Flags(0));
    const flagsFromBit = ((lhs & rhs) !== 0) ? Flags_$ctor_Z524259A4(0) : Flags_op_BitwiseOr_603E7D40(Flags_Zero(), Flags_Parity());
    return Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(flagsPersisted, flagsFromValue), flagsFromSign), flagsFromBit), Flags_HalfCarry());
}

export function Alu_parityFlagsFor(value) {
    return Alu_sz53_parity(value & 255);
}

export function Alu_iff2FlagsFor(value, currentFlags, iff2) {
    const flagsPersisted = Flags_op_BitwiseAnd_603E7D40(currentFlags, Flags_Carry());
    const flagsFromValue = Flags_op_BitwiseAnd_603E7D40(Flags_$ctor_Z524259A4(value & 255), Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()));
    const flagsFromIff = iff2 ? Flags_Parity() : (new Flags(0));
    return Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Alu_sz53_8(value & 255), flagsPersisted), flagsFromValue), flagsFromIff);
}

export function Alu_rotate8(lhs, direction, carryIn) {
    const carryOut = (equals(direction, Alu_Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Alu_Direction.Left) ? (((lhs << 1) | (carryIn ? 1 : 0)) & 255) : (((lhs >> 1) | (carryIn ? 128 : 0)) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Alu_sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

export function Alu_rotateCircular8(lhs, direction) {
    const carryOut = (equals(direction, Alu_Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Alu_Direction.Left) ? (((lhs << 1) | ((carryOut !== 0) ? 1 : 0)) & 255) : (((lhs >> 1) | ((carryOut !== 0) ? 128 : 0)) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Alu_sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

export function Alu_fastRotate8(lhs, direction, flags) {
    const patternInput = Alu_rotate8(lhs, direction, Flags__get_carry(flags));
    const preservedOriginal = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Parity());
    const preservedResult = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Carry(), Flags_Flag3()), Flags_Flag5());
    return [patternInput[0], Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(flags, preservedOriginal), Flags_op_BitwiseAnd_603E7D40(patternInput[1], preservedResult))];
}

export function Alu_fastRotateCircular8(lhs, direction, flags) {
    const patternInput = Alu_rotateCircular8(lhs, direction);
    const preservedOriginal = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Parity());
    const preservedResult = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Carry(), Flags_Flag3()), Flags_Flag5());
    return [patternInput[0], Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(flags, preservedOriginal), Flags_op_BitwiseAnd_603E7D40(patternInput[1], preservedResult))];
}

export function Alu_shiftLogical8(lhs, direction) {
    const carryOut = (equals(direction, Alu_Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Alu_Direction.Left) ? (((lhs << 1) | 1) & 255) : ((lhs >> 1) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Alu_sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

export function Alu_shiftArithmetic8(lhs, direction) {
    const carryOut = (equals(direction, Alu_Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Alu_Direction.Left) ? ((lhs << 1) & 255) : (((lhs >> 1) | (lhs & 128)) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_603E7D40(Alu_sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

/**
 * 8-bit register selector (port of JetpacFSharp Registers.fs).
 */
export class R8 extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["A", "F", "B", "C", "D", "E", "H", "L", "A_", "F_", "B_", "C_", "D_", "E_", "H_", "L_", "SPH", "SPL", "IXH", "IXL", "IYH", "IYL"];
    }
    static A = new R8(0, []);
    static F = new R8(1, []);
    static B = new R8(2, []);
    static C = new R8(3, []);
    static D = new R8(4, []);
    static E = new R8(5, []);
    static H = new R8(6, []);
    static L = new R8(7, []);
    static A_ = new R8(8, []);
    static F_ = new R8(9, []);
    static B_ = new R8(10, []);
    static C_ = new R8(11, []);
    static D_ = new R8(12, []);
    static E_ = new R8(13, []);
    static H_ = new R8(14, []);
    static L_ = new R8(15, []);
    static SPH = new R8(16, []);
    static SPL = new R8(17, []);
    static IXH = new R8(18, []);
    static IXL = new R8(19, []);
    static IYH = new R8(20, []);
    static IYL = new R8(21, []);
}

export function R8_$reflection() {
    return union_type("Jetpac2.Core.R8", [], R8, () => [[], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []]);
}

/**
 * 16-bit register pair selector.
 */
export class R16 extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["AF", "BC", "DE", "HL", "AF_", "BC_", "DE_", "HL_", "SP", "IX", "IY"];
    }
    static AF = new R16(0, []);
    static BC = new R16(1, []);
    static DE = new R16(2, []);
    static HL = new R16(3, []);
    static AF_ = new R16(4, []);
    static BC_ = new R16(5, []);
    static DE_ = new R16(6, []);
    static HL_ = new R16(7, []);
    static SP = new R16(8, []);
    static IX = new R16(9, []);
    static IY = new R16(10, []);
}

export function R16_$reflection() {
    return union_type("Jetpac2.Core.R16", [], R16, () => [[], [], [], [], [], [], [], [], [], [], []]);
}

/**
 * A high/low byte pair. All values are `int`; 8-bit values are masked to
 * 0xFF and 16-bit values to 0xFFFF at the point of use.
 */
export class RegPair {
    constructor() {
        this.low = 255;
        this.high = 255;
    }
}

export function RegPair_$reflection() {
    return class_type("Jetpac2.Core.RegPair", undefined, RegPair);
}

export function RegPair_$ctor() {
    return new RegPair();
}

export function RegPair__High(this$) {
    return this$.high | 0;
}

export function RegPair__SetHigh_Z524259A4(this$, v) {
    this$.high = ((v & 255) | 0);
}

export function RegPair__Low(this$) {
    return this$.low | 0;
}

export function RegPair__SetLow_Z524259A4(this$, v) {
    this$.low = ((v & 255) | 0);
}

export function RegPair__HighLow(this$) {
    return (((this$.high << 8) | this$.low) & 65535) | 0;
}

export function RegPair__SetHighLow_Z524259A4(this$, v) {
    this$.high = (((v >> 8) & 255) | 0);
    this$.low = ((v & 255) | 0);
}

/**
 * Port of specbolt's RegisterFile.
 */
export class RegisterFile {
    constructor() {
        this.regs = initialize(11, (_arg) => RegPair_$ctor());
        this.wz_ = 65535;
        this.pc_ = 0;
        this.r_ = 0;
        this.i_ = 0;
    }
}

export function RegisterFile_$reflection() {
    return class_type("Jetpac2.Core.RegisterFile", undefined, RegisterFile);
}

export function RegisterFile_$ctor() {
    return new RegisterFile();
}

function RegisterFile_PairIndex_Z600F6D11(r8) {
    switch (r8.tag) {
        case 2:
        case 3:
            return 1;
        case 4:
        case 5:
            return 2;
        case 6:
        case 7:
            return 3;
        case 8:
        case 9:
            return 4;
        case 10:
        case 11:
            return 5;
        case 12:
        case 13:
            return 6;
        case 14:
        case 15:
            return 7;
        case 16:
        case 17:
            return 8;
        case 18:
        case 19:
            return 9;
        case 20:
        case 21:
            return 10;
        default:
            return 0;
    }
}

function RegisterFile_IsHigh_Z600F6D11(r8) {
    switch (r8.tag) {
        case 0:
        case 2:
        case 4:
        case 6:
        case 8:
        case 10:
        case 12:
        case 14:
        case 16:
        case 18:
        case 20:
            return true;
        default:
            return false;
    }
}

function RegisterFile_PairIndex16_Z61FD1070(r16) {
    switch (r16.tag) {
        case 1:
            return 1;
        case 2:
            return 2;
        case 3:
            return 3;
        case 4:
            return 4;
        case 5:
            return 5;
        case 6:
            return 6;
        case 7:
            return 7;
        case 8:
            return 8;
        case 9:
            return 9;
        case 10:
            return 10;
        default:
            return 0;
    }
}

function RegisterFile__RegFor_Z600F6D11(this$, r8) {
    return item(RegisterFile_PairIndex_Z600F6D11(r8), this$.regs);
}

function RegisterFile__RegFor_Z61FD1070(this$, r16) {
    return item(RegisterFile_PairIndex16_Z61FD1070(r16), this$.regs);
}

export function RegisterFile__Get_Z600F6D11(this$, r8) {
    const pair = RegisterFile__RegFor_Z600F6D11(this$, r8);
    if (RegisterFile_IsHigh_Z600F6D11(r8)) {
        return RegPair__High(pair) | 0;
    }
    else {
        return RegPair__Low(pair) | 0;
    }
}

export function RegisterFile__Set_33BF5693(this$, r8, value) {
    const pair = RegisterFile__RegFor_Z600F6D11(this$, r8);
    if (RegisterFile_IsHigh_Z600F6D11(r8)) {
        RegPair__SetHigh_Z524259A4(pair, value);
    }
    else {
        RegPair__SetLow_Z524259A4(pair, value);
    }
}

export function RegisterFile__Get_Z61FD1070(this$, r16) {
    return RegPair__HighLow(RegisterFile__RegFor_Z61FD1070(this$, r16)) | 0;
}

export function RegisterFile__Set_ZC22B834(this$, r16, value) {
    RegPair__SetHighLow_Z524259A4(RegisterFile__RegFor_Z61FD1070(this$, r16), value);
}

export function RegisterFile__Ix(this$) {
    return RegisterFile__Get_Z61FD1070(this$, R16.IX) | 0;
}

export function RegisterFile__Iy(this$) {
    return RegisterFile__Get_Z61FD1070(this$, R16.IY) | 0;
}

export function RegisterFile__Sp(this$) {
    return RegisterFile__Get_Z61FD1070(this$, R16.SP) | 0;
}

export function RegisterFile__SetSp_Z524259A4(this$, v) {
    RegisterFile__Set_ZC22B834(this$, R16.SP, v);
}

export function RegisterFile__Pc(this$) {
    return this$.pc_ | 0;
}

export function RegisterFile__SetPc_Z524259A4(this$, v) {
    this$.pc_ = ((v & 65535) | 0);
}

export function RegisterFile__R(this$) {
    return this$.r_ | 0;
}

export function RegisterFile__SetR_Z524259A4(this$, v) {
    this$.r_ = ((v & 255) | 0);
}

export function RegisterFile__I(this$) {
    return this$.i_ | 0;
}

export function RegisterFile__SetI_Z524259A4(this$, v) {
    this$.i_ = ((v & 255) | 0);
}

export function RegisterFile__Wz(this$) {
    return this$.wz_ | 0;
}

export function RegisterFile__SetWz_Z524259A4(this$, v) {
    this$.wz_ = ((v & 65535) | 0);
}

export function RegisterFile__Ex_Z3F9DF200(this$, lhs, rhs) {
    const l = RegisterFile__RegFor_Z61FD1070(this$, lhs);
    const r = RegisterFile__RegFor_Z61FD1070(this$, rhs);
    const tmp = RegPair__HighLow(l) | 0;
    RegPair__SetHighLow_Z524259A4(l, RegPair__HighLow(r));
    RegPair__SetHighLow_Z524259A4(r, tmp);
}

export function RegisterFile__Exx(this$) {
    RegisterFile__Ex_Z3F9DF200(this$, R16.BC, R16.BC_);
    RegisterFile__Ex_Z3F9DF200(this$, R16.DE, R16.DE_);
    RegisterFile__Ex_Z3F9DF200(this$, R16.HL, R16.HL_);
}

/**
 * Port of JetpacFSharp (Jetpac.Core) Z80: the fetch/IO/interrupt model and
 * the per-instruction step driver. Instructions come from the Generated
 * page table (or, for converted routines, the Override hook), so there is
 * no opcode decoder here.
 * Timestamped externally visible port effect from the copied machine.
 */
export class HardwareEvent extends Record {
    constructor(Tick, Port, Value, Border, Beeper) {
        super();
        this.Tick = Tick;
        this.Port = (Port | 0);
        this.Value = (Value | 0);
        this.Border = (Border | 0);
        this.Beeper = Beeper;
    }
}

export function HardwareEvent_$reflection() {
    return record_type("Jetpac2.Core.HardwareEvent", [], HardwareEvent, () => [["Tick", int64_type], ["Port", int32_type], ["Value", int32_type], ["Border", int32_type], ["Beeper", bool_type]]);
}

/**
 * Co-execution effect emitted by an address hook. `Tick` is intentionally
 * behavior-changing: it advances the same scheduler used by the CPU and is
 * therefore for explicit instrumentation experiments, not normal parity runs.
 */
export class Effect extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Tick", "Say"];
    }
}

export function Effect_$reflection() {
    return union_type("Jetpac2.Core.Effect", [], Effect, () => [[["Item", int32_type]], [["Item", string_type]]]);
}

/**
 * Address-attached hooks run before the resolved instruction and do not
 * replace it. The returned effects are applied immediately, in list order.
 * `Machine -> Effect list` keeps the hook Fable-clean and lets hooks inspect
 * or deliberately mutate the shared machine state.
 * Invalidation input for decoded code/cache layers.
 */
export class MemoryWriteEvent extends Record {
    constructor(Tick, Address, OldValue, NewValue) {
        super();
        this.Tick = Tick;
        this.Address = (Address | 0);
        this.OldValue = OldValue;
        this.NewValue = NewValue;
    }
}

export function MemoryWriteEvent_$reflection() {
    return record_type("Jetpac2.Core.MemoryWriteEvent", [], MemoryWriteEvent, () => [["Tick", int64_type], ["Address", int32_type], ["OldValue", uint8_type], ["NewValue", uint8_type]]);
}

export class Machine {
    constructor() {
        this.self = (new FSharpRef(defaultOf()));
        this.self;
        this.self.contents = this;
        this.memory = (new Uint8Array(65536));
        this.keyboard = Keyboard_$ctor();
        this.scheduler = Scheduler_$ctor();
        this.video = VideoScreen_$ctor_Z3F6BC7B1(this.memory);
        this.regs = RegisterFile_$ctor();
        this.inHandlers = [];
        this.outHandlers = [];
        this.hardwareEvents = [];
        this.memoryWriteHandlers = [];
        this.memoryResetHandlers = [];
        this.instrumentationEffects = [];
        this.coHooks = initialize(65536, (_arg) => []);
        this.coHookLocations = (new Map([]));
        this.nextCoHookToken = 0;
        this.beeperTrace = [];
        this.beeperLevel = false;
        this.earLevel = false;
        this.border = 0;
        this.halted_ = false;
        this.irqPending_ = false;
        this.iff1_ = false;
        this.iff2_ = false;
        this.irqMode_ = 0;
        this.overrideHook = ((_arg_1) => undefined);
        this.frameEnd = (0n);
        this["videoTask@535"] = (new Lazy(() => Machine__videoTask(this)));
        this["videoTask@535-1"] = this["videoTask@535"].Value;
        Scheduler__Schedule_79A1460(this.scheduler, this["videoTask@535-1"], toInt64_unchecked(fromInt32(0)));
        void (this.outHandlers.push((port) => ((value) => {
            if ((port & 255) === 254) {
                VideoScreen__SetBorder_Z524259A4(this.video, value & 7);
                this.border = ((value & 7) | 0);
                const lvl = (value & 16) !== 0;
                if (lvl !== this.beeperLevel) {
                    this.beeperLevel = lvl;
                    void (this.beeperTrace.push([Scheduler__get_Cycles(this.scheduler), lvl]));
                }
            }
        })));
        void (this.inHandlers.push((port_1) => Keyboard__In_Z524259A4(this.keyboard, port_1)));
        void (this.inHandlers.push((port_2) => (((port_2 & 1) !== 0) ? undefined : (191 | (this.earLevel ? 64 : 0)))));
        this["init@500"] = 1;
    }
}

export function Machine_$reflection() {
    return class_type("Jetpac2.Core.Machine", undefined, Machine);
}

export function Machine_$ctor() {
    return new Machine();
}

(() => {
    Machine["GeneratedStep@"] = ((_arg) => {
        throw new Exception("Generated instruction table not installed");
    });
})();

function Machine__ApplyEffect_3D049D32(this$, effect) {
    if (effect.tag === 1) {
        void (this$.instrumentationEffects.push(effect));
    }
    else {
        const n = effect.fields[0] | 0;
        if (n < 0) {
            throw new Exception("Tick cannot be negative (Parameter \'effect\')");
        }
        Machine__PassTime_Z524259A4(this$, n);
    }
}

function Machine__RunCoHooks(this$) {
    const hooks = item(RegisterFile__Pc(this$.regs) & 65535, this$.coHooks);
    const count = hooks.length | 0;
    let i = 0;
    while ((i < count) && (i < hooks.length)) {
        const enumerator = getEnumerator(item(i, hooks)[1](this$));
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                Machine__ApplyEffect_3D049D32(this$, enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]());
            }
        }
        finally {
            disposeSafe(enumerator);
        }
        i = ((i + 1) | 0);
    }
}

export function Machine__get_Memory(this$) {
    return this$.memory;
}

export function Machine__get_Regs(this$) {
    return this$.regs;
}

export function Machine__get_Video(this$) {
    return this$.video;
}

export function Machine__get_Keyboard(this$) {
    return this$.keyboard;
}

export function Machine__get_Border(this$) {
    return this$.border | 0;
}

export function Machine__get_BeeperLevel(this$) {
    return this$.beeperLevel;
}

export function Machine__get_BeeperTrace(this$) {
    return this$.beeperTrace;
}

export function Machine__get_HardwareEvents(this$) {
    return this$.hardwareEvents;
}

export function Machine__get_Effects(this$) {
    return this$.instrumentationEffects;
}

export function Machine__get_Halted(this$) {
    return this$.halted_;
}

export function Machine__set_Halted_Z1FBCCD16(this$, v) {
    this$.halted_ = v;
}

export function Machine__get_IrqPending(this$) {
    return this$.irqPending_;
}

export function Machine__get_Iff1(this$) {
    return this$.iff1_;
}

export function Machine__set_Iff1_Z1FBCCD16(this$, v) {
    this$.iff1_ = v;
}

export function Machine__get_Iff2(this$) {
    return this$.iff2_;
}

export function Machine__set_Iff2_Z1FBCCD16(this$, v) {
    this$.iff2_ = v;
}

export function Machine__get_IrqMode(this$) {
    return this$.irqMode_ | 0;
}

export function Machine__set_IrqMode_Z524259A4(this$, v) {
    this$.irqMode_ = (v | 0);
}

/**
 * Address -> semantic-instruction hook for converted (M5) routines.
 */
export function Machine__get_Override(this$) {
    return this$.overrideHook;
}

/**
 * Address -> semantic-instruction hook for converted (M5) routines.
 */
export function Machine__set_Override_393C3003(this$, v) {
    this$.overrideHook = v;
}

/**
 * Absolute cycle at which the current frame ends (next scanline wrap).
 */
export function Machine__get_FrameEnd(this$) {
    return this$.frameEnd;
}

/**
 * Absolute cycle at which the current frame ends (next scanline wrap).
 */
export function Machine__set_FrameEnd_Z524259C1(this$, v) {
    this$.frameEnd = v;
}

export function Machine__CycleCount(this$) {
    return Scheduler__get_Cycles(this$.scheduler);
}

export function Machine__PassTime_Z524259A4(this$, tstates) {
    Scheduler__Tick_Z524259C1(this$.scheduler, toInt64_unchecked(fromInt32(tstates)));
}

export function Machine__Interrupt(this$) {
    this$.irqPending_ = true;
}

export function Machine__AddInHandler_16BB63F5(this$, handler) {
    void (this$.inHandlers.push(handler));
}

export function Machine__AddOutHandler_Z5F57DC44(this$, handler) {
    void (this$.outHandlers.push(curry2(handler)));
}

export function Machine__AddMemoryWriteHandler_Z3CB4FF01(this$, handler) {
    void (this$.memoryWriteHandlers.push(handler));
}

export function Machine__AddMemoryResetHandler_3A5B6456(this$, handler) {
    void (this$.memoryResetHandlers.push(handler));
}

/**
 * Register a co-executing hook. Hooks at one address execute in registration
 * order. The token is stable for the lifetime of this machine.
 */
export function Machine__AddCoHook_6363443A(this$, address, hook) {
    const addr = (address & 65535) | 0;
    const token = this$.nextCoHookToken | 0;
    this$.nextCoHookToken = ((this$.nextCoHookToken + 1) | 0);
    void (item(addr, this$.coHooks).push([token, hook]));
    this$.coHookLocations.set(token, addr);
    return token | 0;
}

export function Machine__RemoveCoHook_Z524259A4(this$, token) {
    let matchValue;
    let outArg = 0;
    matchValue = [tryGetValue(this$.coHookLocations, token, new FSharpRef(() => (outArg | 0), (v) => {
        outArg = (v | 0);
    })), outArg];
    if (matchValue[0]) {
        const hooks = item(matchValue[1], this$.coHooks);
        let i = 0;
        while (i < hooks.length) {
            if (item(i, hooks)[0] === token) {
                hooks.splice(i, 1);
                i = (hooks.length | 0);
            }
            else {
                i = ((i + 1) | 0);
            }
        }
        this$.coHookLocations.delete(token);
    }
}

export function Machine__DrainEffects(this$) {
    const items = toList(this$.instrumentationEffects);
    clear(this$.instrumentationEffects);
    return items;
}

export function Machine__In_Z524259A4(this$, port) {
    let combined = 255;
    let enumerator = getEnumerator(this$.inHandlers);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const matchValue = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()(port);
            if (matchValue == null) {
            }
            else {
                const v = matchValue | 0;
                combined = ((combined & v) | 0);
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    return combined | 0;
}

export function Machine__Out_Z37302880(this$, port, value) {
    let enumerator = getEnumerator(this$.outHandlers);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()(port)(value);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    if (this$.hardwareEvents.length < 65536) {
        void (this$.hardwareEvents.push(new HardwareEvent(Scheduler__get_Cycles(this$.scheduler), port & 65535, value & 255, this$.border, this$.beeperLevel)));
    }
}

export function Machine__DrainHardwareEvents(this$) {
    const items = toList(this$.hardwareEvents);
    clear(this$.hardwareEvents);
    return items;
}

export function Machine__Write_Z37302880(this$, address, value) {
    Machine__PassTime_Z524259A4(this$, 3);
    Machine__storeRamByte(this$, address, value);
}

export function Machine__SetKey_289F56A(this$, row, bit, pressed) {
    Keyboard__SetKey_289F56A(this$.keyboard, row, bit, pressed);
}

export function Machine__Halt(this$) {
    this$.halted_ = true;
    RegisterFile__SetPc_Z524259A4(this$.regs, (RegisterFile__Pc(this$.regs) - 1) & 65535);
}

export function Machine__Branch_Z524259A4(this$, offset) {
    RegisterFile__SetPc_Z524259A4(this$.regs, (RegisterFile__Pc(this$.regs) + offset) & 65535);
}

/**
 * One opcode/prefix byte: refresh register + 4 T-states + PC advance
 * (mirrors Z80.ReadOpcode; the instruction bytes are decoded statically,
 * so the memory read itself is skipped).
 */
export function Machine__Fetch(this$) {
    Machine__PassTime_Z524259A4(this$, 3);
    RegisterFile__SetPc_Z524259A4(this$.regs, (RegisterFile__Pc(this$.regs) + 1) & 65535);
    RegisterFile__SetR_Z524259A4(this$.regs, (RegisterFile__R(this$.regs) & 128) | ((RegisterFile__R(this$.regs) + 1) & 127));
    Machine__PassTime_Z524259A4(this$, 1);
}

export function Machine__ReadImm(this$) {
    Machine__PassTime_Z524259A4(this$, 3);
    const addr = RegisterFile__Pc(this$.regs) | 0;
    RegisterFile__SetPc_Z524259A4(this$.regs, (addr + 1) & 65535);
    return ~~item(addr, this$.memory) | 0;
}

export function Machine__ReadImm16(this$) {
    const low = Machine__ReadImm(this$) | 0;
    return (((Machine__ReadImm(this$) << 8) | low) & 65535) | 0;
}

export function Machine__Read_Z524259A4(this$, address) {
    Machine__PassTime_Z524259A4(this$, 3);
    return ~~item(address & 65535, this$.memory) | 0;
}

export function Machine__Pop8(this$) {
    const value = Machine__Read_Z524259A4(this$, RegisterFile__Sp(this$.regs)) | 0;
    RegisterFile__SetSp_Z524259A4(this$.regs, (RegisterFile__Sp(this$.regs) + 1) & 65535);
    return value | 0;
}

export function Machine__Pop16(this$) {
    const low = Machine__Pop8(this$) | 0;
    return (((Machine__Pop8(this$) << 8) | low) & 65535) | 0;
}

export function Machine__Push8_Z524259A4(this$, value) {
    RegisterFile__SetSp_Z524259A4(this$.regs, (RegisterFile__Sp(this$.regs) - 1) & 65535);
    Machine__Write_Z37302880(this$, RegisterFile__Sp(this$.regs), value & 255);
}

export function Machine__Push16_Z524259A4(this$, value) {
    Machine__Push8_Z524259A4(this$, (value >> 8) & 255);
    Machine__Push8_Z524259A4(this$, value & 255);
}

export function Machine__Flags(this$) {
    return Flags_$ctor_Z524259A4(RegisterFile__Get_Z600F6D11(this$.regs, R8.F));
}

export function Machine__SetFlags_2901ED1A(this$, flags) {
    RegisterFile__Set_33BF5693(this$.regs, R8.F, Flags__ToU8(flags));
}

function Machine__HandleInterrupt(this$) {
    this$.irqPending_ = false;
    if (!this$.iff1_) {
    }
    else {
        if (this$.halted_) {
            this$.halted_ = false;
            RegisterFile__SetPc_Z524259A4(this$.regs, (RegisterFile__Pc(this$.regs) + 1) & 65535);
        }
        this$.iff1_ = false;
        this$.iff2_ = false;
        Machine__PassTime_Z524259A4(this$, 7);
        RegisterFile__SetSp_Z524259A4(this$.regs, (RegisterFile__Sp(this$.regs) - 2) & 65535);
        Machine__storeRamByte(this$, RegisterFile__Sp(this$.regs), RegisterFile__Pc(this$.regs) & 255);
        Machine__storeRamByte(this$, (RegisterFile__Sp(this$.regs) + 1) & 65535, (RegisterFile__Pc(this$.regs) >> 8) & 255);
        Machine__PassTime_Z524259A4(this$, 6);
        const matchValue = this$.irqMode_ | 0;
        switch (matchValue) {
            case 0:
            case 1: {
                RegisterFile__SetPc_Z524259A4(this$.regs, 56);
                break;
            }
            case 2: {
                const addr = (255 | ((RegisterFile__I(this$.regs) << 8) & 65280)) | 0;
                Machine__PassTime_Z524259A4(this$, 6);
                RegisterFile__SetPc_Z524259A4(this$.regs, (~~item(addr, this$.memory) | (~~item((addr + 1) & 65535, this$.memory) << 8)) & 65535);
                break;
            }
            default:
                throw new Exception("Inconceivable interrupt mode");
        }
    }
}

/**
 * Execute one instruction using a caller-provided base resolver. The
 * resolver runs after co-hooks, so a hook that modifies the current opcode
 * cannot leave a stale CE operation selected before the hook ran.
 */
export function Machine__ExecuteOne_5007B66A(this$, resolve) {
    if (this$.irqPending_) {
        Machine__HandleInterrupt(this$);
    }
    if (this$.halted_) {
        Machine__PassTime_Z524259A4(this$, 1);
    }
    else {
        Machine__RunCoHooks(this$);
        const matchValue = this$.overrideHook(RegisterFile__Pc(this$.regs));
        if (matchValue == null) {
            resolve(this$);
        }
        else {
            matchValue(this$);
        }
    }
}

/**
 * Execute one generated-table instruction (mirrors Z80.ExecuteOne).
 */
export function Machine__Step(this$) {
    Machine__ExecuteOne_5007B66A(this$, (m) => {
        Machine_get_GeneratedStep()(m);
    });
}

/**
 * Installed by Generated.fs (same pattern as Z80Ops/Z80Dispatch).
 */
export function Machine_get_GeneratedStep() {
    return Machine["GeneratedStep@"];
}

/**
 * Installed by Generated.fs (same pattern as Z80Ops/Z80Dispatch).
 */
export function Machine_set_GeneratedStep_5007B66A(v) {
    Machine["GeneratedStep@"] = v;
}

/**
 * Reset the scheduler clock and schedule the video task for the next fire
 * after the given absolute cycle (used by LoadState).
 */
export function Machine__ResetClock_Z373037E0(this$, cycles, videoNextTime) {
    Scheduler__Reset_Z524259C1(this$.scheduler, cycles);
    Scheduler__Schedule_79A1460(this$.scheduler, this$["videoTask@535-1"], toInt64_unchecked(op_Subtraction(videoNextTime, cycles)));
}

export function Machine__LoadState_5EF83E14(this$, bytes, regsText) {
    copyTo(bytes, 0, this$.memory, 0, min(bytes.length, 65536));
    let enumerator = getEnumerator(this$.memoryResetHandlers);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()();
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const kv = new Map([]);
    const arr = regsText.split("\n");
    for (let idx = 0; idx <= (arr.length - 1); idx++) {
        const line = item(idx, arr);
        const line_1 = line.trim();
        if (line_1.length > 0) {
            const matchValue = line_1.indexOf("=") | 0;
            if (matchValue === -1) {
            }
            else {
                const i = matchValue | 0;
                kv.set(substring(line_1, 0, i).trim(), substring(line_1, i + 1).trim());
            }
        }
    }
    const hex = (k, def) => {
        let matchValue_1;
        let outArg = defaultOf();
        matchValue_1 = [tryGetValue(kv, k, new FSharpRef(() => outArg, (v) => {
            outArg = v;
        })), outArg];
        if (matchValue_1[0]) {
            return parse(replace(matchValue_1[1], "0x", ""), 511, false, 32, 16) | 0;
        }
        else {
            return def | 0;
        }
    };
    const boolv = (k_1, def_1) => {
        let matchValue_2;
        let outArg_1 = defaultOf();
        matchValue_2 = [tryGetValue(kv, k_1, new FSharpRef(() => outArg_1, (v_2) => {
            outArg_1 = v_2;
        })), outArg_1];
        if (matchValue_2[0]) {
            const v_3 = matchValue_2[1];
            if (v_3 === "true") {
                return true;
            }
            else {
                return v_3 === "1";
            }
        }
        else {
            return def_1;
        }
    };
    RegisterFile__Set_ZC22B834(this$.regs, R16.AF, hex("af", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.BC, hex("bc", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.DE, hex("de", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.HL, hex("hl", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.AF_, hex("af2", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.BC_, hex("bc2", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.DE_, hex("de2", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.HL_, hex("hl2", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.IX, hex("ix", 0));
    RegisterFile__Set_ZC22B834(this$.regs, R16.IY, hex("iy", 0));
    RegisterFile__SetSp_Z524259A4(this$.regs, hex("sp", 0));
    RegisterFile__SetPc_Z524259A4(this$.regs, hex("pc", 0));
    RegisterFile__SetI_Z524259A4(this$.regs, hex("i", 0));
    RegisterFile__SetR_Z524259A4(this$.regs, hex("r", 0));
    RegisterFile__SetWz_Z524259A4(this$.regs, hex("wz", 65535));
    this$.iff1_ = boolv("iff1", false);
    this$.iff2_ = boolv("iff2", false);
    this$.irqMode_ = (hex("im", 0) | 0);
    this$.halted_ = boolv("halted", false);
    this$.irqPending_ = boolv("irq", false);
    this$.border = (hex("border", 0) | 0);
    VideoScreen__SetBorder_Z524259A4(this$.video, this$.border);
    this$.beeperLevel = boolv("beeper", false);
    this$.earLevel = boolv("tapeEar", false);
    const cycles = toInt64_unchecked(parse_1(getItemFromDict(kv, "cycles"), 511, false, 64));
    const videoNextTime = toInt64_unchecked(parse_1(getItemFromDict(kv, "videoNextTime"), 511, false, 64));
    Machine__ResetClock_Z373037E0(this$.self.contents, cycles, videoNextTime);
    const f = toInt64_unchecked(op_Division(videoNextTime, 224n));
    const scanline = ~~toInt32_unchecked(toInt64_unchecked(op_Modulus(f, 312n))) | 0;
    const wraps = toInt64_unchecked(op_Division(f, 312n));
    VideoScreen__SetState_289F56A(this$.video, scanline, ~~toInt32_unchecked(toInt64_unchecked(op_Modulus(wraps, 16n))), equals_1(toInt64_unchecked(op_Modulus(toInt64_unchecked(op_Division(wraps, 16n)), 2n)), 1n));
    clear(this$.beeperTrace);
    this$.frameEnd = toInt64_unchecked(parse_1(getItemFromDict(kv, "nextWrap"), 511, false, 64));
}

/**
 * Capture the full machine state (64K memory + key=value state text, the
 * same schema the oracle uses, including `wz` which LoadState restores).
 * The video task reschedules +224 inside its own run, so the next fire is
 * always the next multiple of 224 — no schedule walk needed.
 */
export function Machine__SaveState(this$) {
    const cycles = Scheduler__get_Cycles(this$.scheduler);
    const videoNextTime = toInt64_unchecked(op_Multiply(toInt64_unchecked(op_Addition(toInt64_unchecked(op_Division(cycles, 224n)), 1n)), 224n));
    const nextWrap = toInt64_unchecked(op_Multiply(toInt64_unchecked(op_Subtraction(toInt64_unchecked(op_Multiply(toInt64_unchecked(op_Division(toInt64_unchecked(op_Addition(toInt64_unchecked(op_Division(videoNextTime, 224n)), 312n)), 312n)), 312n)), 1n)), 224n));
    let regsText;
    const arg = RegisterFile__Get_Z61FD1070(this$.regs, R16.AF) | 0;
    const arg_1 = RegisterFile__Get_Z61FD1070(this$.regs, R16.BC) | 0;
    const arg_2 = RegisterFile__Get_Z61FD1070(this$.regs, R16.DE) | 0;
    const arg_3 = RegisterFile__Get_Z61FD1070(this$.regs, R16.HL) | 0;
    const arg_4 = RegisterFile__Get_Z61FD1070(this$.regs, R16.AF_) | 0;
    const arg_5 = RegisterFile__Get_Z61FD1070(this$.regs, R16.BC_) | 0;
    const arg_6 = RegisterFile__Get_Z61FD1070(this$.regs, R16.DE_) | 0;
    const arg_7 = RegisterFile__Get_Z61FD1070(this$.regs, R16.HL_) | 0;
    const arg_8 = RegisterFile__Ix(this$.regs) | 0;
    const arg_9 = RegisterFile__Iy(this$.regs) | 0;
    const arg_10 = RegisterFile__Sp(this$.regs) | 0;
    const arg_11 = RegisterFile__Pc(this$.regs) | 0;
    const arg_12 = RegisterFile__I(this$.regs) | 0;
    const arg_13 = RegisterFile__R(this$.regs) | 0;
    const arg_14 = RegisterFile__Wz(this$.regs) | 0;
    const arg_15 = this$.iff1_;
    const arg_16 = this$.iff2_;
    const arg_17 = this$.irqMode_ | 0;
    const arg_18 = this$.halted_;
    const arg_19 = this$.border | 0;
    const arg_20 = this$.beeperLevel;
    const arg_21 = this$.earLevel;
    const arg_25 = this$.irqPending_;
    regsText = toText(printf("af=%X\nbc=%X\nde=%X\nhl=%X\naf2=%X\nbc2=%X\nde2=%X\nhl2=%X\nix=%X\niy=%X\nsp=%X\npc=%X\ni=%X\nr=%X\nwz=%X\niff1=%b\niff2=%b\nim=%d\nhalted=%b\nborder=%d\nbeeper=%b\ntapeEar=%b\ncycles=%d\nvideoNextTime=%d\nnextWrap=%d\nirq=%b\n"))(arg)(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6)(arg_7)(arg_8)(arg_9)(arg_10)(arg_11)(arg_12)(arg_13)(arg_14)(arg_15)(arg_16)(arg_17)(arg_18)(arg_19)(arg_20)(arg_21)(cycles)(videoNextTime)(nextWrap)(arg_25);
    return [copy(this$.memory), regsText];
}

export function Machine__videoTask(this$) {
    return SchedulerTask_$ctor_43314A15((_arg) => {
        if (VideoScreen__NextScanLine(this$.video)) {
            this$.irqPending_ = true;
        }
        Scheduler__Schedule_79A1460(this$.scheduler, this$["videoTask@535"].Value, toInt64_unchecked(fromInt32(VideoConstants_CyclesPerScanLine)));
    });
}

export function Machine__storeRamByte(this$, address, value) {
    const addr = (address & 65535) | 0;
    if (addr >= 16384) {
        const oldValue = item(addr, this$.memory);
        const newValue = (value & 255) & 0xFF;
        this$.memory[addr] = newValue;
        if (oldValue !== newValue) {
            const event = new MemoryWriteEvent(Scheduler__get_Cycles(this$.scheduler), addr, oldValue, newValue);
            let enumerator = getEnumerator(this$.memoryWriteHandlers);
            try {
                while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                    enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()(event);
                }
            }
            finally {
                disposeSafe(enumerator);
            }
        }
    }
}

