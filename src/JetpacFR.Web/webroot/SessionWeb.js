
import { item } from "./fable_modules/fable-library-js.5.13.0/Array.js";
import { toBase64String, fromBase64String, printf, toText, format } from "./fable_modules/fable-library-js.5.13.0/String.js";
import { bootToEntry, AssetProvider } from "./BootWeb.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.13.0/FSharp.Core.js";
import { RegisterFile__R, RegisterFile__Sp, RegisterFile__Iy, RegisterFile__Ix, R16, RegisterFile__Get_Z61FD1070, Machine__LoadState_5EF83E14, Machine__set_FrameEnd_Z524259C1, Machine__Step, RegisterFile__I, Machine__get_IrqMode, Flags__ToU8, Machine__Flags, RegisterFile__Pc, Machine__get_Iff1, Machine__get_IrqPending, Machine__get_FrameEnd, Machine__get_BeeperTrace, Machine__get_Video, Machine__SetKey_289F56A, Machine__get_Memory, Machine__get_Regs, Machine__CycleCount, Machine__AddOutHandler_Z5F57DC44, Machine__AddMemoryWriteHandler_Z3CB4FF01, Machine_$ctor } from "./Jetpac2.Core/Machine.js";
import { RegSnapshot, TraceRecorder__RecordFrameBoundary_Z6EF827D7, TraceEntry, TraceRecorder__Record_A4DCE76, TraceRecorder__RecordSnapshot_7114161F, PortEvent, TraceRecorder__RecordPort_Z54D84C2A, MemWriteEvent, TraceRecorder__RecordWrite_Z30129C29, TraceRecorder_$ctor_Z37302880 } from "./TraceTypesWeb.js";
import { EnsureInstalled } from "./Jetpac2.Core/Z80Table.js";
import { op_Addition, toInt32_unchecked, compare, op_Subtraction, toInt64_unchecked, toUInt32_unchecked } from "./fable_modules/fable-library-js.5.13.0/BigInt.js";
import { class_type } from "./fable_modules/fable-library-js.5.13.0/Reflection.js";
import { VideoScreen__BlitTo } from "./Jetpac2.Core/Screen.js";
import { toList } from "./fable_modules/fable-library-js.5.13.0/Seq.js";
import { clear } from "./fable_modules/fable-library-js.5.13.0/Util.js";
import { ToSamples } from "./Jetpac2.Core/Beeper.js";
import { disasmMemory } from "./JetpacFR.Core/Disasm.js";
import { min } from "./fable_modules/fable-library-js.5.13.0/Double.js";
import { Spectrum48__SaveState } from "./Jetpac.Core/Spectrum.js";

function AssetHash_ofBytes(b) {
    let h = -2128831035;
    for (let i = 0; i <= (b.length - 1); i++) {
        h = (((h ^ ~~item(i, b)) * 16777619) | 0);
        h = ((h & -1) | 0);
    }
    return format('{0:' + "x8" + '}', h);
}

const EntryCache_storage = window.localStorage;

const EntryCache_memKey = "jetpacfr.entry.v1.mem";

const EntryCache_stateKey = "jetpacfr.entry.v1.state";

const EntryCache_metaKey = "jetpacfr.entry.v1.meta";

function EntryCache_marker(romPath, tzxPath) {
    const arg = AssetHash_ofBytes(AssetProvider()(romPath));
    const arg_1 = AssetHash_ofBytes(AssetProvider()(tzxPath));
    return toText(printf("rom=%s\ntzx=%s"))(arg)(arg_1);
}

function EntryCache_getItem(key) {
    const v = EntryCache_storage.getItem(key);
    if (Operators_IsNull(v)) {
        return undefined;
    }
    else {
        return v;
    }
}

/**
 * Some(mem, state) when localStorage holds a cache matching the current
 * assets and it decodes to a full 64K image; None otherwise (cold boot).
 */
export function EntryCache_tryLoad(romPath, tzxPath) {
    try {
        const matchValue = EntryCache_getItem(EntryCache_metaKey);
        let matchResult, m_1;
        if (matchValue != null) {
            if (matchValue === EntryCache_marker(romPath, tzxPath)) {
                matchResult = 0;
                m_1 = matchValue;
            }
            else {
                matchResult = 1;
            }
        }
        else {
            matchResult = 1;
        }
        switch (matchResult) {
            case 0: {
                const matchValue_1 = EntryCache_getItem(EntryCache_memKey);
                const matchValue_2 = EntryCache_getItem(EntryCache_stateKey);
                let matchResult_1, memB64, state;
                if (matchValue_1 != null) {
                    if (matchValue_2 != null) {
                        matchResult_1 = 0;
                        memB64 = matchValue_1;
                        state = matchValue_2;
                    }
                    else {
                        matchResult_1 = 1;
                    }
                }
                else {
                    matchResult_1 = 1;
                }
                switch (matchResult_1) {
                    case 0: {
                        const mem = fromBase64String(memB64);
                        return (mem.length === 65536) ? [mem, state] : undefined;
                    }
                    default:
                        return undefined;
                }
            }
            default:
                return undefined;
        }
    }
    catch (matchValue_4) {
        return undefined;
    }
}

export function EntryCache_save(romPath, tzxPath, mem, state) {
    try {
        EntryCache_storage.setItem(EntryCache_metaKey, EntryCache_marker(romPath, tzxPath));
        EntryCache_storage.setItem(EntryCache_memKey, toBase64String(mem));
        EntryCache_storage.setItem(EntryCache_stateKey, state);
    }
    catch (matchValue) {
    }
}

export class TraceSession {
    constructor(romBytes, tzxBytes, capacity) {
        this.romPath = "rom";
        this.tzxPath = "tzx";
        this.port = Machine_$ctor();
        this.recorder = TraceRecorder_$ctor_Z37302880(capacity, 512);
        this.frame = 0;
        this.warmStart = false;
        this.snapshotInterval = 64;
        EnsureInstalled();
        Machine__AddMemoryWriteHandler_Z3CB4FF01(this.port, (e) => {
            TraceRecorder__RecordWrite_Z30129C29(this.recorder, new MemWriteEvent(toUInt32_unchecked(e.Tick) >>> 0, e.Address & 0xFFFF, e.OldValue, e.NewValue));
        });
        Machine__AddOutHandler_Z5F57DC44(this.port, (p, v) => {
            TraceRecorder__RecordPort_Z54D84C2A(this.recorder, new PortEvent(toUInt32_unchecked(Machine__CycleCount(this.port)) >>> 0, p & 0xFFFF, (v & 255) & 0xFF, (v & 7) & 0xFF));
        });
        TraceSession__loadEntryState(this);
        TraceRecorder__RecordSnapshot_7114161F(this.recorder, TraceSession__snapshotOf(this));
    }
}

export function TraceSession_$reflection() {
    return class_type("JetpacFR.Core.TraceSession", undefined, TraceSession);
}

export function TraceSession_$ctor_28C3603C(romBytes, tzxBytes, capacity) {
    return new TraceSession(romBytes, tzxBytes, capacity);
}

/**
 * True when the entry state came from the cache (no oracle boot needed).
 */
export function TraceSession__get_WarmStart(this$) {
    return this$.warmStart;
}

export function TraceSession__get_Frame(this$) {
    return this$.frame | 0;
}

export function TraceSession__get_Recorder(this$) {
    return this$.recorder;
}

export function TraceSession__get_CycleCount(this$) {
    return Machine__CycleCount(this$.port);
}

export function TraceSession__get_Regs(this$) {
    return Machine__get_Regs(this$.port);
}

export function TraceSession__get_Memory(this$) {
    return Machine__get_Memory(this$.port);
}

export function TraceSession__SetKey_289F56A(this$, row, bit, pressed) {
    Machine__SetKey_289F56A(this$.port, row, bit, pressed);
}

/**
 * Rendered frame (BGRA 320x256) from the port's video state.
 */
export function TraceSession__get_ScreenBuffer(this$) {
    return VideoScreen__BlitTo(Machine__get_Video(this$.port));
}

/**
 * Drain the frame's beeper transitions and synthesize the 882 samples for
 * the just-executed frame.
 */
export function TraceSession__DrainBeeperSamples_Z524259C1(this$, frameStart) {
    const trace = toList(Machine__get_BeeperTrace(this$.port));
    clear(Machine__get_BeeperTrace(this$.port));
    return ToSamples(trace, frameStart, toInt64_unchecked(op_Subtraction(Machine__CycleCount(this$.port), frameStart)));
}

/**
 * Execute one frame (69888 T-states plus overshoot) on the port machine,
 * recording every instruction. Returns (frameStart, frameEnd) for the
 * audio call.
 */
export function TraceSession__RunFrame(this$) {
    const frameStart = Machine__CycleCount(this$.port);
    const frameEnd = Machine__get_FrameEnd(this$.port);
    let step = 0;
    while (compare(Machine__CycleCount(this$.port), frameEnd) < 0) {
        let copyOfStruct_1, copyOfStruct_2;
        const irq = Machine__get_IrqPending(this$.port) && Machine__get_Iff1(this$.port);
        const pc = RegisterFile__Pc(Machine__get_Regs(this$.port)) | 0;
        let flagsBefore;
        let copyOfStruct = Machine__Flags(this$.port);
        flagsBefore = Flags__ToU8(copyOfStruct);
        const cyclesBefore = Machine__CycleCount(this$.port);
        if (irq) {
            let vector;
            if (Machine__get_IrqMode(this$.port) === 2) {
                const addr = (255 | ((RegisterFile__I(Machine__get_Regs(this$.port)) << 8) & 65280)) | 0;
                vector = ((~~item(addr & 65535, Machine__get_Memory(this$.port)) | (~~item((addr + 1) & 65535, Machine__get_Memory(this$.port)) << 8)) & 65535);
            }
            else {
                vector = 56;
            }
            TraceRecorder__Record_A4DCE76(this$.recorder, new TraceEntry(vector & 0xFFFF, 0, 0, 0, 0, vector & 0xFFFF, toUInt32_unchecked(cyclesBefore) >>> 0, 0, 7, flagsBefore & 0xFF, flagsBefore & 0xFF, 1));
            const vinsn = disasmMemory(Machine__get_Memory(this$.port), vector);
            Machine__Step(this$.port);
            const after = RegisterFile__Pc(Machine__get_Regs(this$.port)) | 0;
            const cycles = ~~toInt32_unchecked(toInt64_unchecked(op_Subtraction(Machine__CycleCount(this$.port), cyclesBefore))) | 0;
            const next = ((vector + vinsn.Length) & 65535) | 0;
            const m = Machine__get_Memory(this$.port);
            TraceRecorder__Record_A4DCE76(this$.recorder, new TraceEntry(vector & 0xFFFF, item(vector & 65535, m), item((vector + 1) & 65535, m), item((vector + 2) & 65535, m), item((vector + 3) & 65535, m), after & 0xFFFF, toUInt32_unchecked(cyclesBefore) >>> 0, vinsn.Length & 0xFF, min(255, cycles) & 0xFF, flagsBefore & 0xFF, ((copyOfStruct_1 = Machine__Flags(this$.port), Flags__ToU8(copyOfStruct_1))) & 0xFF, (after !== next) ? 1 : 0));
        }
        else {
            const insn = disasmMemory(Machine__get_Memory(this$.port), pc);
            Machine__Step(this$.port);
            const after_1 = RegisterFile__Pc(Machine__get_Regs(this$.port)) | 0;
            const cycles_1 = ~~toInt32_unchecked(toInt64_unchecked(op_Subtraction(Machine__CycleCount(this$.port), cyclesBefore))) | 0;
            const next_1 = ((pc + insn.Length) & 65535) | 0;
            const m_1 = Machine__get_Memory(this$.port);
            TraceRecorder__Record_A4DCE76(this$.recorder, new TraceEntry(pc & 0xFFFF, item(pc & 65535, m_1), item((pc + 1) & 65535, m_1), item((pc + 2) & 65535, m_1), item((pc + 3) & 65535, m_1), after_1 & 0xFFFF, toUInt32_unchecked(cyclesBefore) >>> 0, insn.Length & 0xFF, min(255, cycles_1) & 0xFF, flagsBefore & 0xFF, ((copyOfStruct_2 = Machine__Flags(this$.port), Flags__ToU8(copyOfStruct_2))) & 0xFF, (after_1 !== next_1) ? 1 : 0));
        }
        if ((step % this$.snapshotInterval) === 0) {
            TraceRecorder__RecordSnapshot_7114161F(this$.recorder, TraceSession__snapshotOf(this$));
        }
        step = ((step + 1) | 0);
    }
    Machine__set_FrameEnd_Z524259C1(this$.port, toInt64_unchecked(op_Addition(frameEnd, 69888n)));
    TraceRecorder__RecordFrameBoundary_Z6EF827D7(this$.recorder, toUInt32_unchecked(Machine__CycleCount(this$.port)) >>> 0);
    this$.frame = ((this$.frame + 1) | 0);
    return [frameStart, Machine__CycleCount(this$.port)];
}

export function TraceSession__loadEntryState(this$) {
    const matchValue = EntryCache_tryLoad(this$.romPath, this$.tzxPath);
    if (matchValue == null) {
        const patternInput_3 = Spectrum48__SaveState(bootToEntry(this$.romPath, this$.tzxPath)[0]);
        const state_2 = patternInput_3[1];
        const mem_2 = patternInput_3[0];
        EntryCache_save(this$.romPath, this$.tzxPath, mem_2, state_2);
        Machine__LoadState_5EF83E14(this$.port, mem_2, state_2);
        this$.warmStart = false;
    }
    else {
        const state = matchValue[1];
        const mem = matchValue[0];
        try {
            Machine__LoadState_5EF83E14(this$.port, mem, state);
            this$.warmStart = true;
        }
        catch (matchValue_1) {
            const patternInput_1 = Spectrum48__SaveState(bootToEntry(this$.romPath, this$.tzxPath)[0]);
            const state_1 = patternInput_1[1];
            const mem_1 = patternInput_1[0];
            EntryCache_save(this$.romPath, this$.tzxPath, mem_1, state_1);
            Machine__LoadState_5EF83E14(this$.port, mem_1, state_1);
            this$.warmStart = false;
        }
    }
}

export function TraceSession__snapshotOf(this$) {
    const r = Machine__get_Regs(this$.port);
    return new RegSnapshot(toUInt32_unchecked(Machine__CycleCount(this$.port)) >>> 0, RegisterFile__Get_Z61FD1070(r, R16.AF) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.BC) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.DE) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.HL) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.AF_) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.BC_) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.DE_) & 0xFFFF, RegisterFile__Get_Z61FD1070(r, R16.HL_) & 0xFFFF, RegisterFile__Ix(r) & 0xFFFF, RegisterFile__Iy(r) & 0xFFFF, RegisterFile__Sp(r) & 0xFFFF, RegisterFile__Pc(r) & 0xFFFF, RegisterFile__I(r) & 0xFF, RegisterFile__R(r) & 0xFF);
}

