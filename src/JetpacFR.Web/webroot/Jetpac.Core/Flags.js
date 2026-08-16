
import { printf, toText } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { class_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";

/**
 * The Z80 flag register, ported from specbolt (z80/common/include/z80/common/Flags.hpp).
 * Stored as an int; 8-bit operators mask to 0xFF. A struct so ALU operations
 * allocate nothing (critical for emulator throughput).
 */
export class Flags extends Record {
    constructor(Value) {
        super();
        this.Value = (Value | 0);
    }
    toString() {
        const this$ = this;
        return toText(printf("Flags(0x%02x)"))(this$.Value);
    }
}

export function Flags_$reflection() {
    return class_type("Jetpac.Core.Flags", undefined, Flags, class_type("System.ValueType"));
}

export function Flags_$ctor_Z524259A4(value) {
    return new Flags(value & 255);
}

export function Flags__ToU8(this$) {
    return this$.Value | 0;
}

export function Flags_op_BitwiseAnd_Z51621B00(lhs, rhs) {
    return Flags_$ctor_Z524259A4(lhs.Value & rhs.Value);
}

export function Flags_op_BitwiseOr_Z51621B00(lhs, rhs) {
    return Flags_$ctor_Z524259A4(lhs.Value | rhs.Value);
}

export function Flags_op_ExclusiveOr_Z51621B00(lhs, rhs) {
    return Flags_$ctor_Z524259A4(lhs.Value ^ rhs.Value);
}

export function Flags_op_LogicalNot_Z7353D318(f) {
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

