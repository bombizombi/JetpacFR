
import { FSharpRef, Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { class_type, record_type, list_type, uint8_type, int32_type, int64_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { Memory__SetPageTable_4F10E657, Memory__SetRomFlags_5907F3E1, Memory__LoadBytes_6BA4C033, Memory__Read_Z524259A4, Memory__get_AddressSpace, Memory__AddWriteHandler_48E2F4A1, Memory_$ctor_Z524259A4, MemoryWrite_$reflection } from "./Memory.js";
import { clear, Lazy, defaultOf } from "../fable_modules/fable-library-js.5.17.0/Util.js";
import { VideoConstants_CyclesPerScanLine, Video__NextScanLine, Video__SetState_289F56A, Video__BlitTo, Video__SetBorder_Z524259A4, Video_$ctor_44195B56 } from "./Video.js";
import { Keyboard__SetKey_289F56A, Keyboard__In_Z524259A4, Keyboard_$ctor } from "./Keyboard.js";
import { SchedulerTask_$ctor_5D8E2020, Scheduler__Reset_Z6EF827B6, Scheduler__Schedule_Z9FDE619, Scheduler_$ctor } from "./Scheduler.js";
import { Z80__set_Halted_Z1FBCCD16, Z80__Interrupt, Z80__set_IrqMode_Z524259A4, Z80__set_Iff2_Z1FBCCD16, Z80__set_Iff1_Z1FBCCD16, Z80__ResetInterruptState, Z80__get_IrqPending, Z80__get_Halted, Z80__get_IrqMode, Z80__get_Iff2, Z80__get_Iff1, Z80__get_Regs, Z80__ExecuteOne, Z80__AddInHandler_16BB63F5, Z80__CycleCount, Z80__AddOutHandler_Z5F57DC44, Z80_$ctor_Z31E0EE2A } from "./Z80.js";
import { Tape__PassTime_Z524259A4, Tape__NextTransition, Tape__Play, Tape__InsertTzx_Z3F6BC7B1, Tape__SetLevel_Z1FBCCD16, Tape__Stop, Tape__Playing, Tape__Level, Tape_$ctor } from "./Tape.js";
import { EnsureInstalled } from "./Z80Ops.js";
import { fromInt32, compare, equals, op_Modulus, toInt32_unchecked, toUInt64_unchecked, op_Subtraction, op_Division, op_Addition, op_Multiply, fromUInt64, toInt64_unchecked } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { toList } from "../fable_modules/fable-library-js.5.17.0/Seq.js";
import { R8, RegisterFile__Get_2EC184DD, RegisterFile__SetWz_Z524259A4, RegisterFile__SetR_Z524259A4, RegisterFile__SetI_Z524259A4, RegisterFile__SetPc_Z524259A4, RegisterFile__SetSp_Z524259A4, RegisterFile__Set_488BADFE, RegisterFile__Wz, RegisterFile__R, RegisterFile__I, RegisterFile__Sp, RegisterFile__Iy, RegisterFile__Ix, R16, RegisterFile__Get_6F21F62, RegisterFile__Pc } from "./Registers.js";
import { getItemFromDict, tryGetValue, addToSet } from "../fable_modules/fable-library-js.5.17.0/MapUtil.js";
import { toFail, replace, substring, printf, toText } from "../fable_modules/fable-library-js.5.17.0/String.js";
import { item, copyTo, copy } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { min } from "../fable_modules/fable-library-js.5.17.0/Double.js";
import { parse } from "../fable_modules/fable-library-js.5.17.0/Int32.js";
import { parse as parse_1 } from "../fable_modules/fable-library-js.5.17.0/Long.js";

/**
 * Port of specbolt's Spectrum 48K wiring (spectrum/include/spectrum/Spectrum.hpp,
 * 48K paths only). Runs the Z80 against the ULA/keyboard/tape peripherals;
 * a frame-based boot macro types `LOAD ""` so the ROM loader starts the tape.
 */
export class InstructionTraceEvent extends Record {
    constructor(TState, Address, Opcode, NextPc, Writes) {
        super();
        this.TState = TState;
        this.Address = (Address | 0);
        this.Opcode = Opcode;
        this.NextPc = (NextPc | 0);
        this.Writes = Writes;
    }
}

export function InstructionTraceEvent_$reflection() {
    return record_type("Jetpac.Core.InstructionTraceEvent", [], InstructionTraceEvent, () => [["TState", int64_type], ["Address", int32_type], ["Opcode", uint8_type], ["NextPc", int32_type], ["Writes", list_type(MemoryWrite_$reflection())]]);
}

export class Spectrum48 {
    constructor() {
        this.self = (new FSharpRef(defaultOf()));
        this.self;
        this.self.contents = this;
        this.memory = Memory_$ctor_Z524259A4(4);
        this.video = Video_$ctor_44195B56(this.memory);
        this.keyboard = Keyboard_$ctor();
        this.scheduler = Scheduler_$ctor();
        this.z80 = Z80_$ctor_Z31E0EE2A(this.scheduler, this.memory);
        this.tape = Tape_$ctor();
        this.beeper = false;
        this.border = 0;
        this.frameCount = 0;
        this.tapeLastTime = (0n);
        this.booted = false;
        this.frameEnd = (0n);
        this.beeperTrace = [];
        this.traceEnabled = false;
        this.traceEvents = [];
        this.executedAddresses = (new Set([]));
        this.pendingWrites = [];
        this.lastDetect = (0n);
        this.lastBRead = 0;
        this.readsInARow = 0;
        this["videoTask@41"] = (new Lazy(() => Spectrum48__videoTask(this)));
        this["tapeTask@48"] = (new Lazy(() => Spectrum48__tapeTask(this)));
        this["videoTask@41-1"] = this["videoTask@41"].Value;
        this["tapeTask@48-1"] = this["tapeTask@48"].Value;
        this["init@15"] = 1;
        EnsureInstalled();
        Memory__AddWriteHandler_48E2F4A1(this.memory, (event) => {
            if (this.traceEnabled) {
                void (this.pendingWrites.push(event));
            }
        });
        Z80__AddOutHandler_Z5F57DC44(this.z80, (port, value) => {
            if ((port & 255) === 254) {
                Video__SetBorder_Z524259A4(this.video, value & 7);
                this.border = ((value & 7) | 0);
                const lvl = (value & 16) !== 0;
                if (lvl !== this.beeper) {
                    this.beeper = lvl;
                    void (this.beeperTrace.push([toInt64_unchecked(fromUInt64(Z80__CycleCount(this.z80))), lvl]));
                }
            }
        });
        Z80__AddInHandler_16BB63F5(this.z80, (port_1) => Keyboard__In_Z524259A4(this.keyboard, port_1));
        Z80__AddInHandler_16BB63F5(this.z80, (port_2) => {
            if ((port_2 & 1) !== 0) {
                return undefined;
            }
            else {
                Spectrum48__MaybeDetectLoading(this.self.contents);
                return 191 | (Tape__Level(this.tape) ? 64 : 0);
            }
        });
        Scheduler__Schedule_Z9FDE619(this.scheduler, this["videoTask@41-1"], 0n);
    }
}

export function Spectrum48_$reflection() {
    return class_type("Jetpac.Core.Spectrum48", undefined, Spectrum48);
}

export function Spectrum48_$ctor() {
    return new Spectrum48();
}

export function Spectrum48__get_Memory(this$) {
    return Memory__get_AddressSpace(this$.memory);
}

export function Spectrum48__get_ScreenBuffer(this$) {
    return Video__BlitTo(this$.video);
}

export function Spectrum48__get_FrameCount(this$) {
    return this$.frameCount | 0;
}

export function Spectrum48__get_Beeper(this$) {
    return this$.beeper;
}

export function Spectrum48__get_Border(this$) {
    return this$.border | 0;
}

export function Spectrum48__BeginExecutionTrace(this$) {
    clear(this$.traceEvents);
    this$.executedAddresses.clear();
    clear(this$.pendingWrites);
    this$.traceEnabled = true;
}

export function Spectrum48__EndExecutionTrace(this$) {
    this$.traceEnabled = false;
}

export function Spectrum48__get_ExecutedAddressCount(this$) {
    return this$.executedAddresses.size | 0;
}

export function Spectrum48__DrainInstructionTrace(this$) {
    const items = toList(this$.traceEvents);
    clear(this$.traceEvents);
    return items;
}

function Spectrum48__ExecuteOneTraced(this$) {
    if (!this$.traceEnabled) {
        Z80__ExecuteOne(this$.z80);
    }
    else {
        const address = RegisterFile__Pc(Z80__get_Regs(this$.z80)) | 0;
        const opcode = Memory__Read_Z524259A4(this$.memory, address) & 0xFF;
        const tstate = toInt64_unchecked(fromUInt64(Z80__CycleCount(this$.z80)));
        clear(this$.pendingWrites);
        Z80__ExecuteOne(this$.z80);
        addToSet(address, this$.executedAddresses);
        void (this$.traceEvents.push(new InstructionTraceEvent(tstate, address, opcode, RegisterFile__Pc(Z80__get_Regs(this$.z80)), toList(this$.pendingWrites))));
    }
}

export function Spectrum48__ExecuteInstruction(this$) {
    Spectrum48__ExecuteOneTraced(this$.self.contents);
}

export function Spectrum48__get_DebugZ80(this$) {
    return this$.z80;
}

export function Spectrum48__get_DebugTapePlaying(this$) {
    return Tape__Playing(this$.tape);
}

export function Spectrum48__get_BeeperTrace(this$) {
    return toList(this$.beeperTrace);
}

/**
 * Consume the beeper transitions since the last call (per-frame slices).
 */
export function Spectrum48__DrainBeeperTrace(this$) {
    const items = toList(this$.beeperTrace);
    clear(this$.beeperTrace);
    return items;
}

/**
 * Capture the full machine state (64K memory + key=value register/timing
 * text, the same schema the Jetpac2 fixture uses, plus `wz`).
 */
export function Spectrum48__SaveState(this$) {
    const regs = Z80__get_Regs(this$.z80);
    const cycles = toInt64_unchecked(fromUInt64(Z80__CycleCount(this$.z80)));
    const videoNextTime = toInt64_unchecked(op_Multiply(toInt64_unchecked(op_Addition(toInt64_unchecked(op_Division(cycles, 224n)), 1n)), 224n));
    const nextWrap = toInt64_unchecked(op_Multiply(toInt64_unchecked(op_Subtraction(toInt64_unchecked(op_Multiply(toInt64_unchecked(op_Division(toInt64_unchecked(op_Addition(toInt64_unchecked(op_Division(videoNextTime, 224n)), 312n)), 312n)), 312n)), 1n)), 224n));
    let regsText;
    const arg = RegisterFile__Get_6F21F62(regs, R16.AF) | 0;
    const arg_1 = RegisterFile__Get_6F21F62(regs, R16.BC) | 0;
    const arg_2 = RegisterFile__Get_6F21F62(regs, R16.DE) | 0;
    const arg_3 = RegisterFile__Get_6F21F62(regs, R16.HL) | 0;
    const arg_4 = RegisterFile__Get_6F21F62(regs, R16.AF_) | 0;
    const arg_5 = RegisterFile__Get_6F21F62(regs, R16.BC_) | 0;
    const arg_6 = RegisterFile__Get_6F21F62(regs, R16.DE_) | 0;
    const arg_7 = RegisterFile__Get_6F21F62(regs, R16.HL_) | 0;
    const arg_8 = RegisterFile__Ix(regs) | 0;
    const arg_9 = RegisterFile__Iy(regs) | 0;
    const arg_10 = RegisterFile__Sp(regs) | 0;
    const arg_11 = RegisterFile__Pc(regs) | 0;
    const arg_12 = RegisterFile__I(regs) | 0;
    const arg_13 = RegisterFile__R(regs) | 0;
    const arg_14 = RegisterFile__Wz(regs) | 0;
    const arg_15 = Z80__get_Iff1(this$.z80);
    const arg_16 = Z80__get_Iff2(this$.z80);
    const arg_17 = Z80__get_IrqMode(this$.z80) | 0;
    const arg_18 = Z80__get_Halted(this$.z80);
    const arg_19 = Z80__get_IrqPending(this$.z80);
    const arg_20 = this$.border | 0;
    const arg_21 = this$.beeper;
    const arg_22 = Tape__Level(this$.tape);
    regsText = toText(printf("af=%X\nbc=%X\nde=%X\nhl=%X\naf2=%X\nbc2=%X\nde2=%X\nhl2=%X\nix=%X\niy=%X\nsp=%X\npc=%X\ni=%X\nr=%X\nwz=%X\niff1=%b\niff2=%b\nim=%d\nhalted=%b\nirq=%b\nborder=%d\nbeeper=%b\ntapeEar=%b\ncycles=%d\nvideoNextTime=%d\nnextWrap=%d\n"))(arg)(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6)(arg_7)(arg_8)(arg_9)(arg_10)(arg_11)(arg_12)(arg_13)(arg_14)(arg_15)(arg_16)(arg_17)(arg_18)(arg_19)(arg_20)(arg_21)(arg_22)(cycles)(videoNextTime)(nextWrap);
    return [copy(Memory__get_AddressSpace(this$.memory)), regsText];
}

/**
 * Restore a captured state (mirrors Jetpac2 Machine.LoadState). The tape is
 * stopped (checkpoints are taken at/beyond game entry) and its ear level is
 * restored; loading-detection state is reset.
 */
export function Spectrum48__LoadState_5EF83E14(this$, bytes, regsText) {
    copyTo(bytes, 0, Memory__get_AddressSpace(this$.memory), 0, min(bytes.length, 65536));
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
    const regs = Z80__get_Regs(this$.z80);
    RegisterFile__Set_488BADFE(regs, R16.AF, hex("af", 0));
    RegisterFile__Set_488BADFE(regs, R16.BC, hex("bc", 0));
    RegisterFile__Set_488BADFE(regs, R16.DE, hex("de", 0));
    RegisterFile__Set_488BADFE(regs, R16.HL, hex("hl", 0));
    RegisterFile__Set_488BADFE(regs, R16.AF_, hex("af2", 0));
    RegisterFile__Set_488BADFE(regs, R16.BC_, hex("bc2", 0));
    RegisterFile__Set_488BADFE(regs, R16.DE_, hex("de2", 0));
    RegisterFile__Set_488BADFE(regs, R16.HL_, hex("hl2", 0));
    RegisterFile__Set_488BADFE(regs, R16.IX, hex("ix", 0));
    RegisterFile__Set_488BADFE(regs, R16.IY, hex("iy", 0));
    RegisterFile__SetSp_Z524259A4(regs, hex("sp", 0));
    RegisterFile__SetPc_Z524259A4(regs, hex("pc", 0));
    RegisterFile__SetI_Z524259A4(regs, hex("i", 0));
    RegisterFile__SetR_Z524259A4(regs, hex("r", 0));
    RegisterFile__SetWz_Z524259A4(regs, hex("wz", 65535));
    Z80__ResetInterruptState(this$.z80);
    Z80__set_Iff1_Z1FBCCD16(this$.z80, boolv("iff1", false));
    Z80__set_Iff2_Z1FBCCD16(this$.z80, boolv("iff2", false));
    Z80__set_IrqMode_Z524259A4(this$.z80, hex("im", 0));
    if (boolv("irq", false)) {
        Z80__Interrupt(this$.z80);
    }
    Z80__set_Halted_Z1FBCCD16(this$.z80, boolv("halted", false));
    this$.border = (hex("border", 0) | 0);
    Video__SetBorder_Z524259A4(this$.video, this$.border);
    this$.beeper = boolv("beeper", false);
    const ear = boolv("tapeEar", false);
    Tape__Stop(this$.tape);
    Tape__SetLevel_Z1FBCCD16(this$.tape, ear);
    const cycles = toUInt64_unchecked(parse_1(getItemFromDict(kv, "cycles"), 511, true, 64));
    const videoNextTime = toUInt64_unchecked(parse_1(getItemFromDict(kv, "videoNextTime"), 511, true, 64));
    const nextWrap = toInt64_unchecked(parse_1(getItemFromDict(kv, "nextWrap"), 511, false, 64));
    Scheduler__Reset_Z6EF827B6(this$.scheduler, cycles);
    Scheduler__Schedule_Z9FDE619(this$.scheduler, this$["videoTask@41-1"], toUInt64_unchecked(op_Subtraction(videoNextTime, cycles)));
    const f = toUInt64_unchecked(op_Division(videoNextTime, 224n));
    const scanline = ~~toInt32_unchecked(toUInt64_unchecked(op_Modulus(f, 312n))) | 0;
    const wraps = toUInt64_unchecked(op_Division(f, 312n));
    Video__SetState_289F56A(this$.video, scanline, ~~toInt32_unchecked(toUInt64_unchecked(op_Modulus(wraps, 16n))), equals(toUInt64_unchecked(op_Modulus(toUInt64_unchecked(op_Division(wraps, 16n)), 2n)), 1n));
    clear(this$.beeperTrace);
    this$.frameEnd = nextWrap;
    this$.frameCount = 0;
    this$.tapeLastTime = (0n);
    this$.lastDetect = (0n);
    this$.lastBRead = 0;
    this$.readsInARow = 0;
    this$.booted = true;
}

/**
 * Absolute cycle at which the current frame ends.
 */
export function Spectrum48__get_FrameEnd(this$) {
    return this$.frameEnd;
}

/**
 * Execute until an absolute cycle without changing the frame boundary.
 */
export function Spectrum48__RunUntil_Z524259C1(this$, targetCycle) {
    while (compare(toInt64_unchecked(fromUInt64(Z80__CycleCount(this$.z80))), targetCycle) < 0) {
        Spectrum48__ExecuteOneTraced(this$.self.contents);
    }
}

export function Spectrum48__AdvanceFrameBoundary(this$) {
    this$.frameEnd = toInt64_unchecked(op_Addition(this$.frameEnd, 69888n));
    this$.frameCount = ((this$.frameCount + 1) | 0);
}

/**
 * Run to the next 50Hz interrupt boundary (69888 T-states), without the
 * boot macro (used after a checkpoint restore).
 */
export function Spectrum48__RunGameFrame(this$) {
    Spectrum48__RunUntil_Z524259C1(this$, this$.frameEnd);
    Spectrum48__AdvanceFrameBoundary(this$);
}

export function Spectrum48__LoadRom_Z3F6BC7B1(this$, bytes) {
    if (bytes.length !== 16384) {
        const arg = bytes.length | 0;
        toFail(printf("Bad ROM size: %d (expected 16384)"))(arg);
    }
    Memory__LoadBytes_6BA4C033(this$.memory, bytes, 0, 0, 16384);
    Memory__SetRomFlags_5907F3E1(this$.memory, [true, false, false, false]);
    Memory__SetPageTable_4F10E657(this$.memory, new Int32Array([0, 1, 2, 3]));
}

export function Spectrum48__InsertTape_Z3F6BC7B1(this$, bytes) {
    Tape__InsertTzx_Z3F6BC7B1(this$.tape, bytes);
}

export function Spectrum48__SetKey_289F56A(this$, row, bit, pressed) {
    Keyboard__SetKey_289F56A(this$.keyboard, row, bit, pressed);
}

function Spectrum48__MaybeDetectLoading(this$) {
    const sinceLast = toUInt64_unchecked(op_Subtraction(Z80__CycleCount(this$.z80), this$.lastDetect));
    const bDiff = ((RegisterFile__Get_2EC184DD(Z80__get_Regs(this$.z80), R8.B) - this$.lastBRead) & 255) | 0;
    this$.lastDetect = Z80__CycleCount(this$.z80);
    this$.lastBRead = (RegisterFile__Get_2EC184DD(Z80__get_Regs(this$.z80), R8.B) | 0);
    if (Tape__Playing(this$.tape)) {
        if ((compare(sinceLast, 1000n) > 0) ? true : (((bDiff !== 1) && (bDiff !== 0)) && (bDiff !== 255))) {
            this$.readsInARow = ((this$.readsInARow + 1) | 0);
            if (this$.readsInARow >= 2) {
                Tape__Stop(this$.tape);
            }
        }
        else {
            this$.readsInARow = 0;
        }
    }
    else if ((compare(sinceLast, 500n) <= 0) && ((bDiff === 1) ? true : (bDiff === 255))) {
        this$.readsInARow = ((this$.readsInARow + 1) | 0);
        if (this$.readsInARow >= 10) {
            Spectrum48__Play(this$);
        }
    }
    else {
        this$.readsInARow = 0;
    }
}

function Spectrum48__Play(this$) {
    Tape__Play(this$.tape);
    if (Tape__NextTransition(this$.tape) !== 0) {
        this$.tapeLastTime = Z80__CycleCount(this$.z80);
        Scheduler__Schedule_Z9FDE619(this$.scheduler, this$["tapeTask@48-1"], toUInt64_unchecked(fromInt32(Tape__NextTransition(this$.tape))));
    }
}

function Spectrum48__RunBootMacro(this$) {
    switch (this$.frameCount) {
        case 150: {
            Keyboard__SetKey_289F56A(this$.keyboard, 6, 3, true);
            break;
        }
        case 158: {
            Keyboard__SetKey_289F56A(this$.keyboard, 6, 3, false);
            break;
        }
        case 162: {
            Keyboard__SetKey_289F56A(this$.keyboard, 7, 1, true);
            Keyboard__SetKey_289F56A(this$.keyboard, 5, 0, true);
            break;
        }
        case 170: {
            Keyboard__SetKey_289F56A(this$.keyboard, 7, 1, false);
            Keyboard__SetKey_289F56A(this$.keyboard, 5, 0, false);
            break;
        }
        case 174: {
            Keyboard__SetKey_289F56A(this$.keyboard, 7, 1, true);
            Keyboard__SetKey_289F56A(this$.keyboard, 5, 0, true);
            break;
        }
        case 182: {
            Keyboard__SetKey_289F56A(this$.keyboard, 7, 1, false);
            Keyboard__SetKey_289F56A(this$.keyboard, 5, 0, false);
            break;
        }
        case 186: {
            Keyboard__SetKey_289F56A(this$.keyboard, 6, 0, true);
            break;
        }
        case 194: {
            Keyboard__SetKey_289F56A(this$.keyboard, 6, 0, false);
            break;
        }
        default:
            undefined;
    }
}

export function Spectrum48__RunFrame(this$) {
    if (!this$.booted) {
        Spectrum48__RunBootMacro(this$);
    }
    const endCycles = toUInt64_unchecked(op_Addition(Z80__CycleCount(this$.z80), 70000n));
    while (compare(Z80__CycleCount(this$.z80), endCycles) < 0) {
        Spectrum48__ExecuteOneTraced(this$.self.contents);
    }
    Video__BlitTo(this$.video);
    this$.frameCount = ((this$.frameCount + 1) | 0);
}

export function Spectrum48__Reset(this$) {
    RegisterFile__SetPc_Z524259A4(Z80__get_Regs(this$.z80), 0);
    Z80__ResetInterruptState(this$.z80);
    Tape__Stop(this$.tape);
    this$.frameCount = 0;
    this$.tapeLastTime = (0n);
    this$.lastDetect = (0n);
    this$.lastBRead = 0;
    this$.readsInARow = 0;
    for (let row = 0; row <= 7; row++) {
        for (let bit = 0; bit <= 4; bit++) {
            Keyboard__SetKey_289F56A(this$.keyboard, row, bit, false);
        }
    }
}

export function Spectrum48__videoTask(this$) {
    return SchedulerTask_$ctor_5D8E2020((_arg) => {
        if (Video__NextScanLine(this$.video)) {
            Z80__Interrupt(this$.z80);
        }
        Scheduler__Schedule_Z9FDE619(this$.scheduler, this$["videoTask@41"].Value, toUInt64_unchecked(fromInt32(VideoConstants_CyclesPerScanLine)));
    });
}

export function Spectrum48__tapeTask(this$) {
    return SchedulerTask_$ctor_5D8E2020((cycle) => {
        Tape__PassTime_Z524259A4(this$.tape, ~~toInt32_unchecked(toUInt64_unchecked(op_Subtraction(cycle, this$.tapeLastTime))));
        this$.tapeLastTime = cycle;
        if (Tape__NextTransition(this$.tape) !== 0) {
            Scheduler__Schedule_Z9FDE619(this$.scheduler, this$["tapeTask@48"].Value, toUInt64_unchecked(fromInt32(Tape__NextTransition(this$.tape))));
        }
    });
}

