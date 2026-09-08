
import { RegisterFile__I, RegisterFile__SetSp_Z524259A4, RegisterFile__Sp, RegisterFile__R, RegisterFile__SetR_Z524259A4, RegisterFile__Pc, RegisterFile__SetPc_Z524259A4, RegisterFile__Set_Z54B079DF, R8, RegisterFile__Get_2EC184DD, RegisterFile_$ctor } from "./Registers.js";
import { class_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { Flags__ToU8, Flags_$ctor_Z524259A4 } from "./Flags.js";
import { Scheduler__Tick_Z6EF827B6, Scheduler__get_Cycles } from "./Scheduler.js";
import { fromInt32, toUInt64_unchecked } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { Exception, disposeSafe, getEnumerator, curry2 } from "../fable_modules/fable-library-js.5.17.0/Util.js";
import { Memory__Read16_Z524259A4, Memory__Write16_Z37302880, Memory__Write_Z37302880, Memory__Read_Z524259A4 } from "./Memory.js";

/**
 * Port of specbolt's v2 Z80 core (z80/common/Z80Base.* + z80/v2/Z80.*).
 * All registers are ints; 8/16-bit values are masked at the point of use.
 * The instruction set lives in Z80Ops.fs; this class delegates opcode
 * execution through `Z80Dispatch.Run`, installed by Z80Ops.
 */
export class Z80 {
    constructor(scheduler, memory) {
        this.scheduler = scheduler;
        this.memory = memory;
        this.halted_ = false;
        this.irqPending_ = false;
        this.iff1_ = false;
        this.iff2_ = false;
        this.irqMode_ = 0;
        this.inHandlers_ = [];
        this.outHandlers_ = [];
        this.regs_ = RegisterFile_$ctor();
    }
}

export function Z80_$reflection() {
    return class_type("Jetpac.Core.Z80", undefined, Z80);
}

export function Z80_$ctor_Z31E0EE2A(scheduler, memory) {
    return new Z80(scheduler, memory);
}

/**
 * Opcode execution dispatcher. Z80Ops.fs installs its table here; calling
 * execute_one() before installation fails loudly.
 */
export class Z80Dispatch {
    constructor() {
    }
}

export function Z80Dispatch_$reflection() {
    return class_type("Jetpac.Core.Z80Dispatch", undefined, Z80Dispatch);
}

export function Z80__get_Regs(this$) {
    return this$.regs_;
}

export function Z80__get_Memory(this$) {
    return this$.memory;
}

export function Z80__get_Iff1(this$) {
    return this$.iff1_;
}

export function Z80__set_Iff1_Z1FBCCD16(this$, v) {
    this$.iff1_ = v;
}

export function Z80__get_Iff2(this$) {
    return this$.iff2_;
}

export function Z80__set_Iff2_Z1FBCCD16(this$, v) {
    this$.iff2_ = v;
}

export function Z80__get_Halted(this$) {
    return this$.halted_;
}

export function Z80__set_Halted_Z1FBCCD16(this$, v) {
    this$.halted_ = v;
}

export function Z80__get_IrqMode(this$) {
    return this$.irqMode_ | 0;
}

export function Z80__set_IrqMode_Z524259A4(this$, v) {
    this$.irqMode_ = (v | 0);
}

export function Z80__Flags(this$) {
    return Flags_$ctor_Z524259A4(RegisterFile__Get_2EC184DD(this$.regs_, R8.F));
}

export function Z80__SetFlags_Z7353D318(this$, flags) {
    RegisterFile__Set_Z54B079DF(this$.regs_, R8.F, Flags__ToU8(flags));
}

export function Z80__CycleCount(this$) {
    return Scheduler__get_Cycles(this$.scheduler);
}

export function Z80__PassTime_Z524259A4(this$, tstates) {
    Scheduler__Tick_Z6EF827B6(this$.scheduler, toUInt64_unchecked(fromInt32(tstates)));
}

export function Z80__Interrupt(this$) {
    this$.irqPending_ = true;
}

/**
 * True when an interrupt is pending but not yet accepted (save-state).
 */
export function Z80__get_IrqPending(this$) {
    return this$.irqPending_;
}

export function Z80__AddInHandler_16BB63F5(this$, handler) {
    void (this$.inHandlers_.push(handler));
}

export function Z80__AddOutHandler_Z5F57DC44(this$, handler) {
    void (this$.outHandlers_.push(curry2(handler)));
}

/**
 * Clear interrupt/iff/halt state (used by Spectrum48.Reset).
 */
export function Z80__ResetInterruptState(this$) {
    this$.halted_ = false;
    this$.irqPending_ = false;
    this$.iff1_ = false;
    this$.iff2_ = false;
    this$.irqMode_ = 0;
}

export function Z80__In_Z524259A4(this$, port) {
    let combined = 255;
    let enumerator = getEnumerator(this$.inHandlers_);
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

export function Z80__Out_Z37302880(this$, port, value) {
    let enumerator = getEnumerator(this$.outHandlers_);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()(port)(value);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

export function Z80__Halt(this$) {
    this$.halted_ = true;
    RegisterFile__SetPc_Z524259A4(this$.regs_, (RegisterFile__Pc(this$.regs_) - 1) & 65535);
}

export function Z80__Branch_Z524259A4(this$, offset) {
    RegisterFile__SetPc_Z524259A4(this$.regs_, (RegisterFile__Pc(this$.regs_) + offset) & 65535);
}

export function Z80__ReadOpcode(this$) {
    const opcode = Z80__ReadImmediate(this$) | 0;
    RegisterFile__SetR_Z524259A4(this$.regs_, (RegisterFile__R(this$.regs_) & 128) | ((RegisterFile__R(this$.regs_) + 1) & 127));
    Z80__PassTime_Z524259A4(this$, 1);
    return opcode | 0;
}

export function Z80__ReadImmediate(this$) {
    Z80__PassTime_Z524259A4(this$, 3);
    const addr = RegisterFile__Pc(this$.regs_) | 0;
    RegisterFile__SetPc_Z524259A4(this$.regs_, (addr + 1) & 65535);
    return Memory__Read_Z524259A4(this$.memory, addr) | 0;
}

export function Z80__ReadImmediate16(this$) {
    const low = Z80__ReadImmediate(this$) | 0;
    return (((Z80__ReadImmediate(this$) << 8) | low) & 65535) | 0;
}

export function Z80__Write_Z37302880(this$, address, value) {
    Z80__PassTime_Z524259A4(this$, 3);
    Memory__Write_Z37302880(this$.memory, address & 65535, value & 255);
}

export function Z80__Read_Z524259A4(this$, address) {
    Z80__PassTime_Z524259A4(this$, 3);
    return Memory__Read_Z524259A4(this$.memory, address & 65535) | 0;
}

export function Z80__Pop8(this$) {
    const value = Z80__Read_Z524259A4(this$, RegisterFile__Sp(this$.regs_)) | 0;
    RegisterFile__SetSp_Z524259A4(this$.regs_, (RegisterFile__Sp(this$.regs_) + 1) & 65535);
    return value | 0;
}

export function Z80__Pop16(this$) {
    const low = Z80__Pop8(this$) | 0;
    return (((Z80__Pop8(this$) << 8) | low) & 65535) | 0;
}

export function Z80__Push8_Z524259A4(this$, value) {
    RegisterFile__SetSp_Z524259A4(this$.regs_, (RegisterFile__Sp(this$.regs_) - 1) & 65535);
    Z80__Write_Z37302880(this$, RegisterFile__Sp(this$.regs_), value & 255);
}

export function Z80__Push16_Z524259A4(this$, value) {
    Z80__Push8_Z524259A4(this$, (value >> 8) & 255);
    Z80__Push8_Z524259A4(this$, value & 255);
}

function Z80__HandleInterrupt(this$) {
    this$.irqPending_ = false;
    if (!this$.iff1_) {
    }
    else {
        if (this$.halted_) {
            this$.halted_ = false;
            RegisterFile__SetPc_Z524259A4(this$.regs_, (RegisterFile__Pc(this$.regs_) + 1) & 65535);
        }
        this$.iff1_ = false;
        this$.iff2_ = false;
        Z80__PassTime_Z524259A4(this$, 7);
        RegisterFile__SetSp_Z524259A4(this$.regs_, (RegisterFile__Sp(this$.regs_) - 2) & 65535);
        Memory__Write16_Z37302880(this$.memory, RegisterFile__Sp(this$.regs_), RegisterFile__Pc(this$.regs_));
        Z80__PassTime_Z524259A4(this$, 6);
        const matchValue = this$.irqMode_ | 0;
        switch (matchValue) {
            case 0:
            case 1: {
                RegisterFile__SetPc_Z524259A4(this$.regs_, 56);
                break;
            }
            case 2: {
                const addr = (255 | ((RegisterFile__I(this$.regs_) << 8) & 65280)) | 0;
                Z80__PassTime_Z524259A4(this$, 6);
                RegisterFile__SetPc_Z524259A4(this$.regs_, Memory__Read16_Z524259A4(this$.memory, addr));
                break;
            }
            default:
                throw new Exception("Inconceivable interrupt mode");
        }
    }
}

export function Z80__ExecuteOne(this$) {
    if (this$.irqPending_) {
        Z80__HandleInterrupt(this$);
    }
    if (this$.halted_) {
        Z80__PassTime_Z524259A4(this$, 1);
    }
    else {
        const opcode = Z80__ReadOpcode(this$) | 0;
        Z80Dispatch_get_Run()(this$)(opcode);
    }
}

(() => {
    Z80Dispatch["Run@"] = ((_arg, _arg_1) => {
        throw new Exception("Z80 instruction dispatcher not installed (Z80Ops.fs not initialized)");
    });
})();

export function Z80Dispatch_get_Run() {
    return curry2(Z80Dispatch["Run@"]);
}

export function Z80Dispatch_set_Run_3708BEA5(v) {
    Z80Dispatch["Run@"] = v;
}

