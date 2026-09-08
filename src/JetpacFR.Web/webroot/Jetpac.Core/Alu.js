
import { Union } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { union_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { Flags__get_carry, Flags__get_half_carry, Flags__get_subtract, Flags_op_LogicalNot_Z7353D318, Flags_Subtract, Flags_op_ExclusiveOr_Z51621B00, Flags_Overflow, Flags_HalfCarry, Flags, Flags_Carry, Flags_Parity, Flags_Zero, Flags_Flag5, Flags_Flag3, Flags_Sign, Flags_$ctor_Z524259A4, Flags_op_BitwiseAnd_Z51621B00, Flags_op_BitwiseOr_Z51621B00 } from "./Flags.js";
import { equals } from "../fable_modules/fable-library-js.5.17.0/Util.js";

export class Direction extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Left", "Right"];
    }
    static Left = new Direction(0, []);
    static Right = new Direction(1, []);
}

export function Direction_$reflection() {
    return union_type("Jetpac.Core.Alu.Direction", [], Direction, () => [[], []]);
}

function popcount(v) {
    const v_1 = (v - ((v >> 1) & 1431655765)) | 0;
    const v_2 = ((v_1 & 858993459) + ((v_1 >> 2) & 858993459)) | 0;
    return ((((v_2 + (v_2 >> 4)) & 252645135) * 16843009) >> 24) | 0;
}

function sz53_8(value) {
    return Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(value), Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Flag3()), Flags_Flag5())), (value === 0) ? Flags_Zero() : Flags_$ctor_Z524259A4(0));
}

function sz53_parity(value) {
    return Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(value), Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Flag3()), Flags_Flag5())), (value === 0) ? Flags_Zero() : Flags_$ctor_Z524259A4(0)), ((popcount(value) % 2) === 0) ? Flags_Parity() : Flags_$ctor_Z524259A4(0));
}

const mask53 = Flags_op_BitwiseOr_Z51621B00(Flags_Flag5(), Flags_Flag3());

export function add8(lhs, rhs, carryIn) {
    const intermediate = (((lhs & 255) + (rhs & 255)) + (carryIn ? 1 : 0)) | 0;
    const carry = (intermediate > 255) ? Flags_Carry() : (new Flags(0));
    const result = (intermediate & 255) | 0;
    const halfCarry = ((((lhs & 15) + (rhs & 15)) + (carryIn ? 1 : 0)) > 15) ? Flags_HalfCarry() : (new Flags(0));
    const overflow = ((((lhs ^ result) & (rhs ^ result)) & 128) !== 0) ? Flags_Overflow() : (new Flags(0));
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(sz53_8(result), carry), halfCarry), overflow)];
}

export function sub8(lhs, rhs, carryIn) {
    const patternInput = add8(lhs & 255, ~rhs & 255, !carryIn);
    return [patternInput[0], Flags_op_ExclusiveOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(patternInput[1], Flags_Subtract()), Flags_op_ExclusiveOr_Z51621B00(Flags_Carry(), Flags_HalfCarry()))];
}

export function inc8(lhs, currentFlags) {
    const result = ((lhs + 1) & 255) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(sz53_8(result), Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(result), mask53)), Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(result ^ lhs), Flags_HalfCarry())), (result === 128) ? Flags_Overflow() : Flags_$ctor_Z524259A4(0)), Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_Carry()))];
}

export function dec8(lhs, currentFlags) {
    const result = ((lhs - 1) & 255) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Subtract(), sz53_8(result)), Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(result), mask53)), (result === 127) ? Flags_Overflow() : Flags_$ctor_Z524259A4(0)), Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(result ^ lhs), Flags_HalfCarry())), Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_Carry()))];
}

export function cmp8(lhs, rhs) {
    return [lhs & 255, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(sub8(lhs & 255, rhs & 255, false)[1], Flags_op_LogicalNot_Z7353D318(mask53)), Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(rhs & 255), mask53))];
}

export function add16(lhs, rhs, currentFlags) {
    const intermediate = ((lhs & 65535) + (rhs & 65535)) | 0;
    const carry = (intermediate > 65535) ? Flags_Carry() : (new Flags(0));
    const result = (intermediate & 65535) | 0;
    const halfCarry = (((lhs & 4095) + (rhs & 4095)) > 4095) ? Flags_HalfCarry() : (new Flags(0));
    const flags35 = Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4((result >> 8) & 255), Flags_op_BitwiseOr_Z51621B00(Flags_Flag5(), Flags_Flag3()));
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Parity())), carry), halfCarry), flags35)];
}

export function sub16(lhs, rhs, currentFlags) {
    const patternInput = add16(lhs & 65535, ~rhs & 65535, currentFlags);
    return [patternInput[0], Flags_op_ExclusiveOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(patternInput[1], Flags_Subtract()), Flags_op_ExclusiveOr_Z51621B00(Flags_Carry(), Flags_HalfCarry()))];
}

export function adc16(lhs, rhs, carryIn) {
    const intermediate = (((lhs & 65535) + (rhs & 65535)) + (carryIn ? 1 : 0)) | 0;
    const carry = (intermediate > 65535) ? Flags_Carry() : (new Flags(0));
    const result = (intermediate & 65535) | 0;
    const halfCarry = ((((lhs & 4095) + (rhs & 4095)) + (carryIn ? 1 : 0)) > 4095) ? Flags_HalfCarry() : (new Flags(0));
    const flags35 = Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4((result >> 8) & 255), Flags_op_BitwiseOr_Z51621B00(Flags_Flag5(), Flags_Flag3()));
    const negative = ((result & 32768) !== 0) ? Flags_Sign() : (new Flags(0));
    const zero = (result === 0) ? Flags_Zero() : (new Flags(0));
    const overflow = ((((lhs ^ result) & (rhs ^ result)) & 32768) !== 0) ? Flags_Overflow() : (new Flags(0));
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(carry, halfCarry), flags35), negative), zero), overflow)];
}

export function sbc16(lhs, rhs, carryIn) {
    const patternInput = adc16(lhs & 65535, ~rhs & 65535, !carryIn);
    return [patternInput[0], Flags_op_ExclusiveOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(patternInput[1], Flags_Subtract()), Flags_op_ExclusiveOr_Z51621B00(Flags_Carry(), Flags_HalfCarry()))];
}

export function xor8(lhs, rhs) {
    const result = ((lhs ^ rhs) & 255) | 0;
    return [result, sz53_parity(result)];
}

export function or8(lhs, rhs) {
    const result = ((lhs | rhs) & 255) | 0;
    return [result, sz53_parity(result)];
}

export function and8(lhs, rhs) {
    const result = ((lhs & rhs) & 255) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(sz53_parity(result), Flags_HalfCarry())];
}

export function daa(lhs, currentFlags) {
    const signAdjust = (Flags__get_subtract(currentFlags) ? -1 : 1) | 0;
    const lowNibbleCarry = ((lhs & 15) > 9) ? true : Flags__get_half_carry(currentFlags);
    const highNibbleCarry = (lhs > 153) ? true : Flags__get_carry(currentFlags);
    const result = (((lhs + (lowNibbleCarry ? (6 * signAdjust) : 0)) + (highNibbleCarry ? (96 * signAdjust) : 0)) & 255) | 0;
    const carry = (lhs > 153) ? Flags_Carry() : (new Flags(0));
    const halfCarry = (((lhs ^ result) & 16) !== 0) ? Flags_HalfCarry() : (new Flags(0));
    const preservedFlags = Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_op_BitwiseOr_Z51621B00(Flags_Carry(), Flags_Subtract()));
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(sz53_parity(result), carry), halfCarry), preservedFlags)];
}

export function cpl(lhs, currentFlags) {
    const result = (lhs ^ 255) | 0;
    const preservedFlags = Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Parity()), Flags_Carry()));
    const setFlags = Flags_op_BitwiseOr_Z51621B00(Flags_HalfCarry(), Flags_Subtract());
    const flagsFromResult = Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(result), Flags_op_BitwiseOr_Z51621B00(Flags_Flag3(), Flags_Flag5()));
    return [result, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(preservedFlags, setFlags), flagsFromResult)];
}

export function scf(lhs, currentFlags) {
    return [lhs & 255, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Parity())), Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(lhs), Flags_op_BitwiseOr_Z51621B00(Flags_Flag3(), Flags_Flag5()))), Flags_Carry())];
}

export function ccf(lhs, currentFlags) {
    const preserved = Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Parity()), Flags_Carry()));
    const flags53 = Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(lhs), Flags_op_BitwiseOr_Z51621B00(Flags_Flag3(), Flags_Flag5()));
    const otherFlag = Flags__get_carry(currentFlags) ? Flags_HalfCarry() : (new Flags(0));
    return [lhs & 255, Flags_op_ExclusiveOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(preserved, flags53), otherFlag), Flags_Carry())];
}

export function bit(lhs, rhs, currentFlags, busNoise) {
    const flagsPersisted = Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_Carry());
    const flagsFromValue = Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(busNoise & 255), Flags_op_BitwiseOr_Z51621B00(Flags_Flag3(), Flags_Flag5()));
    const flagsFromSign = (((rhs & lhs) & 128) !== 0) ? Flags_Sign() : (new Flags(0));
    const flagsFromBit = ((lhs & rhs) !== 0) ? Flags_$ctor_Z524259A4(0) : Flags_op_BitwiseOr_Z51621B00(Flags_Zero(), Flags_Parity());
    return Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(flagsPersisted, flagsFromValue), flagsFromSign), flagsFromBit), Flags_HalfCarry());
}

export function parityFlagsFor(value) {
    return sz53_parity(value & 255);
}

export function iff2FlagsFor(value, currentFlags, iff2) {
    const flagsPersisted = Flags_op_BitwiseAnd_Z51621B00(currentFlags, Flags_Carry());
    const flagsFromValue = Flags_op_BitwiseAnd_Z51621B00(Flags_$ctor_Z524259A4(value & 255), Flags_op_BitwiseOr_Z51621B00(Flags_Flag3(), Flags_Flag5()));
    const flagsFromIff = iff2 ? Flags_Parity() : (new Flags(0));
    return Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(sz53_8(value & 255), flagsPersisted), flagsFromValue), flagsFromIff);
}

/**
 * Rotate through carry.
 */
export function rotate8(lhs, direction, carryIn) {
    const carryOut = (equals(direction, Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Direction.Left) ? (((lhs << 1) | (carryIn ? 1 : 0)) & 255) : (((lhs >> 1) | (carryIn ? 128 : 0)) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

/**
 * Rotate circularly (through the carry flag).
 */
export function rotateCircular8(lhs, direction) {
    const carryOut = (equals(direction, Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Direction.Left) ? (((lhs << 1) | ((carryOut !== 0) ? 1 : 0)) & 255) : (((lhs >> 1) | ((carryOut !== 0) ? 128 : 0)) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

/**
 * Fast rotate: flags preserved as in RLCA/RRCA.
 */
export function fastRotate8(lhs, direction, flags) {
    const patternInput = rotate8(lhs, direction, Flags__get_carry(flags));
    const preservedOriginal = Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Parity());
    const preservedResult = Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Carry(), Flags_Flag3()), Flags_Flag5());
    return [patternInput[0], Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(flags, preservedOriginal), Flags_op_BitwiseAnd_Z51621B00(patternInput[1], preservedResult))];
}

/**
 * Fast circular rotate: flags preserved as in RLCA/RRCA.
 */
export function fastRotateCircular8(lhs, direction, flags) {
    const patternInput = rotateCircular8(lhs, direction);
    const preservedOriginal = Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Parity());
    const preservedResult = Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Carry(), Flags_Flag3()), Flags_Flag5());
    return [patternInput[0], Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(flags, preservedOriginal), Flags_op_BitwiseAnd_Z51621B00(patternInput[1], preservedResult))];
}

/**
 * SLL (Left) / SRL (Right).
 */
export function shiftLogical8(lhs, direction) {
    const carryOut = (equals(direction, Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Direction.Left) ? (((lhs << 1) | 1) & 255) : ((lhs >> 1) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

/**
 * SLA (Left) / SRA (Right).
 */
export function shiftArithmetic8(lhs, direction) {
    const carryOut = (equals(direction, Direction.Left) ? (lhs & 128) : (lhs & 1)) | 0;
    const result = (equals(direction, Direction.Left) ? ((lhs << 1) & 255) : (((lhs >> 1) | (lhs & 128)) & 255)) | 0;
    return [result, Flags_op_BitwiseOr_Z51621B00(sz53_parity(result), (carryOut !== 0) ? Flags_Carry() : Flags_$ctor_Z524259A4(0))];
}

