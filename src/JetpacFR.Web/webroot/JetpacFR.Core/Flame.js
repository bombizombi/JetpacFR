
import { Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { option_type, array_type, record_type, enum_type, int32_type, int64_type, uint16_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { TraceEntry_$reflection } from "../TraceTypesWeb.js";
import { equals, min as min_1, toFloat64, fromUInt8, max as max_1, op_Subtraction, op_Addition, fromUInt32, toInt64_unchecked, compare } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { fill, sortInPlaceBy, setItem, item } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { compareArrays, Exception } from "../fable_modules/fable-library-js.5.17.0/Util.js";
import { printf, toText } from "../fable_modules/fable-library-js.5.17.0/String.js";
import { min, max } from "../fable_modules/fable-library-js.5.17.0/Double.js";
import { map, append, delay, toArray } from "../fable_modules/fable-library-js.5.17.0/Seq.js";

export function CallOps_isCall(b0) {
    switch (b0) {
        case 196:
        case 204:
        case 205:
        case 212:
        case 220:
        case 228:
        case 236:
        case 244:
        case 252:
            return true;
        default:
            return false;
    }
}

export function CallOps_isRst(b0) {
    return (b0 & 199) === 199;
}

export function CallOps_isRet(b0, b1) {
    switch (b0) {
        case 192:
        case 200:
        case 201:
        case 208:
        case 216:
        case 224:
        case 232:
        case 240:
        case 248:
            return true;
        case 237:
            if (b1 === 69) {
                return true;
            }
            else {
                return b1 === 77;
            }
        default:
            return false;
    }
}

/**
 * One flame-graph rectangle: a single function invocation (not an aggregate)
 * spanning [StartTick, EndTick) of ticks relative to the window's first
 * instruction. Depth 0 is the synthetic root; every taken CALL/RST and every
 * interrupt marker pushed one frame below it. StartIndex/EndIndex delimit the
 * window's instruction entries executed inside the invocation (EndIndex
 * exclusive) - the instruction-level drill-down reads them back when the
 * window kept its detail tier.
 */
export class FlameRect extends Record {
    constructor(Entry, CallPc, StartTick, EndTick, Depth, Kind, StartIndex, EndIndex) {
        super();
        this.Entry = Entry;
        this.CallPc = CallPc;
        this.StartTick = StartTick;
        this.EndTick = EndTick;
        this.Depth = (Depth | 0);
        this.Kind = Kind;
        this.StartIndex = (StartIndex | 0);
        this.EndIndex = (EndIndex | 0);
    }
}

export function FlameRect_$reflection() {
    return record_type("JetpacFR.Core.FlameRect", [], FlameRect, () => [["Entry", uint16_type], ["CallPc", uint16_type], ["StartTick", int64_type], ["EndTick", int64_type], ["Depth", int32_type], ["Kind", enum_type("JetpacFR.Core.FlameKind", int32_type, [["FlameCall", 0], ["FlameRst", 1], ["FlameInterrupt", 2], ["FlameRoot", 3]])], ["StartIndex", int32_type], ["EndIndex", int32_type]]);
}

/**
 * The walked result over one execution window. Ticks are int64 relative to
 * the window start (the trace's uint32 machine-cycle counter wraps after
 * ~20 minutes; unwrapping happens once here, never in the UI).
 */
export class FlameWindow extends Record {
    constructor(Rects, PrefixMaxEnd, FrameTicks, FirstFrame, BaseTick, EndTick, MaxDepth, Entries, EntryTicks) {
        super();
        this.Rects = Rects;
        this.PrefixMaxEnd = PrefixMaxEnd;
        this.FrameTicks = FrameTicks;
        this.FirstFrame = (FirstFrame | 0);
        this.BaseTick = BaseTick;
        this.EndTick = EndTick;
        this.MaxDepth = (MaxDepth | 0);
        this.Entries = Entries;
        this.EntryTicks = EntryTicks;
    }
}

export function FlameWindow_$reflection() {
    return record_type("JetpacFR.Core.FlameWindow", [], FlameWindow, () => [["Rects", array_type(FlameRect_$reflection())], ["PrefixMaxEnd", array_type(int64_type)], ["FrameTicks", array_type(int64_type)], ["FirstFrame", int32_type], ["BaseTick", int64_type], ["EndTick", int64_type], ["MaxDepth", int32_type], ["Entries", option_type(array_type(TraceEntry_$reflection()))], ["EntryTicks", option_type(array_type(int64_type))]]);
}

export const FlameWindowModule_empty = new FlameWindow([], new BigInt64Array([]), new BigInt64Array([]), -1, 0n, 0n, 1, undefined, undefined);

/**
 * Drop the detail tier (per-instruction entries) - the rect tier is tiny
 * and always kept.
 */
export function FlameWindowModule_trim(w) {
    return new FlameWindow(w.Rects, w.PrefixMaxEnd, w.FrameTicks, w.FirstFrame, w.BaseTick, w.EndTick, w.MaxDepth, undefined, undefined);
}

/**
 * Index of the first rectangle that can overlap `tick`: PrefixMaxEnd is
 * sorted, so the first entry exceeding the tick bounds every later one.
 */
export function FlameWindowModule_stab(w, tick) {
    const a = w.PrefixMaxEnd;
    const search = (lo_mut, hi_mut) => {
        search:
        while (true) {
            const lo = lo_mut, hi = hi_mut;
            if (lo > hi) {
                return lo | 0;
            }
            else {
                const mid = ((lo + hi) >> 1) | 0;
                if (compare(item(mid, a), tick) > 0) {
                    lo_mut = lo;
                    hi_mut = (mid - 1);
                    continue search;
                }
                else {
                    lo_mut = (mid + 1);
                    hi_mut = hi;
                    continue search;
                }
            }
            break;
        }
    };
    return search(0, a.length - 1) | 0;
}

/**
 * Last entry index with EntryTicks <= tick (-1 when none).
 */
export function FlameWindowModule_entryAtTick(w, tick) {
    let ticks;
    const matchValue = w.EntryTicks;
    let matchResult, ticks_1;
    if (matchValue != null) {
        if ((ticks = matchValue, ticks.length > 0)) {
            matchResult = 0;
            ticks_1 = matchValue;
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
            const search = (lo_mut, hi_mut) => {
                search:
                while (true) {
                    const lo = lo_mut, hi = hi_mut;
                    if (lo > hi) {
                        return hi | 0;
                    }
                    else {
                        const mid = ((lo + hi) >> 1) | 0;
                        if (compare(item(mid, ticks_1), tick) <= 0) {
                            lo_mut = (mid + 1);
                            hi_mut = hi;
                            continue search;
                        }
                        else {
                            lo_mut = lo;
                            hi_mut = (mid - 1);
                            continue search;
                        }
                    }
                    break;
                }
            };
            return search(0, ticks_1.length - 1) | 0;
        }
        default:
            return -1;
    }
}

/**
 * The tick frame `frame` completed at (relative ticks). Requires
 * FirstFrame >= 0 and the frame inside the window.
 */
export function FlameWindowModule_endTickOfFrame(w, frame) {
    let arg_2;
    if (((w.FirstFrame < 0) ? true : (frame < w.FirstFrame)) ? true : ((frame - w.FirstFrame) >= w.FrameTicks.length)) {
        throw new Exception((arg_2 = (w.FrameTicks.length | 0), toText(printf("frame %d outside the flame window (first %d, %d boundaries)"))(frame)(w.FirstFrame)(arg_2)));
    }
    return item(frame - w.FirstFrame, w.FrameTicks);
}

/**
 * The frame whose span contains `tick`, clamped into the window
 * (-1 when the window has no frame mapping).
 */
export function FlameWindowModule_frameAtTick(w, tick) {
    if ((w.FirstFrame < 0) ? true : (w.FrameTicks.length === 0)) {
        return -1;
    }
    else {
        const search = (lo_mut, hi_mut) => {
            search:
            while (true) {
                const lo = lo_mut, hi = hi_mut;
                if (lo > hi) {
                    return (lo - 1) | 0;
                }
                else {
                    const mid = ((lo + hi) >> 1) | 0;
                    if (compare(item(mid, w.FrameTicks), tick) <= 0) {
                        lo_mut = (mid + 1);
                        hi_mut = hi;
                        continue search;
                    }
                    else {
                        lo_mut = lo;
                        hi_mut = (mid - 1);
                        continue search;
                    }
                }
                break;
            }
        };
        return (w.FirstFrame + max(0, search(0, w.FrameTicks.length - 1))) | 0;
    }
}

class FlameWalker_Frame extends Record {
    constructor(Entry, CallPc, Kind, Depth, StartTick, EndTick, StartIndex, EndIndex) {
        super();
        this.Entry = (Entry | 0);
        this.CallPc = (CallPc | 0);
        this.Kind = Kind;
        this.Depth = (Depth | 0);
        this.StartTick = StartTick;
        this.EndTick = EndTick;
        this.StartIndex = (StartIndex | 0);
        this.EndIndex = (EndIndex | 0);
    }
}

function FlameWalker_Frame_$reflection() {
    return record_type("JetpacFR.Core.FlameWalker.Frame", [], FlameWalker_Frame, () => [["Entry", int32_type], ["CallPc", int32_type], ["Kind", enum_type("JetpacFR.Core.FlameKind", int32_type, [["FlameCall", 0], ["FlameRst", 1], ["FlameInterrupt", 2], ["FlameRoot", 3]])], ["Depth", int32_type], ["StartTick", int64_type], ["EndTick", int64_type], ["StartIndex", int32_type], ["EndIndex", int32_type]]);
}

export const FlameWalker_maxDepth = 256;

/**
 * Walk `entries` (execution order, monotonic ticks) plus the frame
 * boundary ticks into a FlameWindow. `firstFrame` tags the window for
 * frame <-> tick mapping (see FlameWindow.frameAtTick).
 */
export function FlameWalker_walk(firstFrame, frameTicks, entries) {
    const n = entries.length | 0;
    if (n === 0) {
        const bind$0040 = FlameWindowModule_empty;
        return new FlameWindow(bind$0040.Rects, bind$0040.PrefixMaxEnd, bind$0040.FrameTicks, firstFrame, bind$0040.BaseTick, bind$0040.EndTick, bind$0040.MaxDepth, bind$0040.Entries, bind$0040.EntryTicks);
    }
    else {
        const ticks = new BigInt64Array(n);
        let offset = 0n;
        let prev = -9223372036854775808n;
        for (let i = 0; i <= (n - 1); i++) {
            const raw = toInt64_unchecked(fromUInt32(item(i, entries).Tick));
            if (compare(toInt64_unchecked(op_Addition(raw, offset)), prev) < 0) {
                offset = toInt64_unchecked(op_Addition(offset, 4294967296n));
            }
            const t = toInt64_unchecked(op_Addition(raw, offset));
            setItem(ticks, i, t);
            prev = t;
        }
        const baseTick = item(0, ticks);
        for (let i_1 = 0; i_1 <= (n - 1); i_1++) {
            setItem(ticks, i_1, toInt64_unchecked(op_Subtraction(item(i_1, ticks), baseTick)));
        }
        let frameTicks64;
        const arr = new BigInt64Array(frameTicks.length);
        let off = 0n;
        let prevF = -9223372036854775808n;
        for (let j = 0; j <= (frameTicks.length - 1); j++) {
            const raw_1 = toInt64_unchecked(fromUInt32(item(j, frameTicks)));
            if (compare(toInt64_unchecked(op_Addition(raw_1, off)), prevF) < 0) {
                off = toInt64_unchecked(op_Addition(off, 4294967296n));
            }
            const t_1 = toInt64_unchecked(op_Addition(raw_1, off));
            setItem(arr, j, max_1(0n, toInt64_unchecked(op_Subtraction(t_1, toInt64_unchecked(fromUInt32(item(0, entries).Tick))))));
            prevF = t_1;
        }
        frameTicks64 = arr;
        let windowEnd;
        const lastEntryEnd = toInt64_unchecked(op_Addition(item(n - 1, ticks), toInt64_unchecked(fromUInt8(item(n - 1, entries).Cycles))));
        windowEnd = ((frameTicks64.length > 0) ? max_1(lastEntryEnd, item(frameTicks64.length - 1, frameTicks64)) : lastEntryEnd);
        const stack = [];
        const finished = [];
        void (stack.push(new FlameWalker_Frame(0, 0, 3, 0, 0n, windowEnd, 0, n)));
        for (let i_2 = 0; i_2 <= (n - 1); i_2++) {
            const e = item(i_2, entries);
            if (e.Length === 0) {
                if (stack.length < FlameWalker_maxDepth) {
                    void (stack.push(new FlameWalker_Frame(~~e.Pc, 0, 2, stack.length, item(i_2, ticks), item(i_2, ticks), i_2, i_2)));
                }
            }
            else {
                const top = item(stack.length - 1, stack);
                const endTick = toInt64_unchecked(op_Addition(item(i_2, ticks), toInt64_unchecked(fromUInt8(e.Cycles))));
                if (compare(endTick, top.EndTick) > 0) {
                    top.EndTick = endTick;
                }
                top.EndIndex = ((i_2 + 1) | 0);
                const b0 = e.B0;
                if ((CallOps_isCall(b0) && (e.Taken === 1)) && (stack.length < FlameWalker_maxDepth)) {
                    void (stack.push(new FlameWalker_Frame(~~e.Target, ~~e.Pc, 0, stack.length, item(i_2, ticks), item(i_2, ticks), i_2, i_2)));
                }
                else if (CallOps_isRst(b0) && (stack.length < FlameWalker_maxDepth)) {
                    void (stack.push(new FlameWalker_Frame(~~e.Target, ~~e.Pc, 1, stack.length, item(i_2, ticks), item(i_2, ticks), i_2, i_2)));
                }
                else if ((CallOps_isRet(b0, e.B1) && (e.Taken === 1)) && (stack.length > 1)) {
                    const f = item(stack.length - 1, stack);
                    stack.splice(stack.length - 1, 1);
                    void (finished.push(f));
                    const parent = item(stack.length - 1, stack);
                    if (compare(f.EndTick, parent.EndTick) > 0) {
                        parent.EndTick = f.EndTick;
                    }
                    if (f.EndIndex > parent.EndIndex) {
                        parent.EndIndex = (f.EndIndex | 0);
                    }
                }
            }
        }
        while (stack.length > 1) {
            const f_1 = item(stack.length - 1, stack);
            stack.splice(stack.length - 1, 1);
            if (compare(f_1.EndTick, windowEnd) < 0) {
                f_1.EndTick = windowEnd;
            }
            if (f_1.EndIndex < n) {
                f_1.EndIndex = (n | 0);
            }
            void (finished.push(f_1));
        }
        const rects = toArray(delay(() => append(map((f_2) => (new FlameRect(f_2.Entry & 0xFFFF, f_2.CallPc & 0xFFFF, f_2.StartTick, max_1(f_2.EndTick, f_2.StartTick), f_2.Depth, f_2.Kind, f_2.StartIndex, f_2.EndIndex)), finished), delay(() => map((f_3) => (new FlameRect(f_3.Entry & 0xFFFF, f_3.CallPc & 0xFFFF, f_3.StartTick, max_1(f_3.EndTick, f_3.StartTick), f_3.Depth, f_3.Kind, f_3.StartIndex, f_3.EndIndex)), stack)))));
        sortInPlaceBy((r) => [r.StartTick, r.Depth], rects, {
            Compare: (x, y) => (compareArrays(x, y) | 0),
        });
        const prefixMax = new BigInt64Array(rects.length);
        let runningMax = 0n;
        for (let i_3 = 0; i_3 <= (rects.length - 1); i_3++) {
            if (compare(item(i_3, rects).EndTick, runningMax) > 0) {
                runningMax = item(i_3, rects).EndTick;
            }
            setItem(prefixMax, i_3, runningMax);
        }
        let depth = 1;
        for (let idx = 0; idx <= (rects.length - 1); idx++) {
            const r_1 = item(idx, rects);
            if ((r_1.Depth + 1) > depth) {
                depth = ((r_1.Depth + 1) | 0);
            }
        }
        return new FlameWindow(rects, prefixMax, frameTicks64, firstFrame, baseTick, max_1(windowEnd, 1n), depth, entries, ticks);
    }
}

/**
 * One pyramid level: granularity 2^Shift frames per cell, row-major over
 * depths (index = depth * Cols + cell). Parallel arrays, Fable-friendly.
 */
export class FlameLod_Level extends Record {
    constructor(Shift, Cols, Funcs, Dominated, Covered) {
        super();
        this.Shift = (Shift | 0);
        this.Cols = (Cols | 0);
        this.Funcs = Funcs;
        this.Dominated = Dominated;
        this.Covered = Covered;
    }
}

export function FlameLod_Level_$reflection() {
    return record_type("JetpacFR.Core.FlameLod.Level", [], FlameLod_Level, () => [["Shift", int32_type], ["Cols", int32_type], ["Funcs", array_type(int32_type)], ["Dominated", array_type(int64_type)], ["Covered", array_type(int64_type)]]);
}

export class FlameLod_Lod extends Record {
    constructor(Levels, FrameTicks, EndTick) {
        super();
        this.Levels = Levels;
        this.FrameTicks = FrameTicks;
        this.EndTick = EndTick;
    }
}

export function FlameLod_Lod_$reflection() {
    return record_type("JetpacFR.Core.FlameLod.Lod", [], FlameLod_Lod, () => [["Levels", array_type(FlameLod_Level_$reflection())], ["FrameTicks", array_type(int64_type)], ["EndTick", int64_type]]);
}

export const FlameLod_empty = new FlameLod_Lod([], new BigInt64Array([]), 1n);

function FlameLod_cellOf(frameTicks, t) {
    const search = (lo_mut, hi_mut) => {
        search:
        while (true) {
            const lo = lo_mut, hi = hi_mut;
            if (lo > hi) {
                return lo | 0;
            }
            else {
                const mid = ((lo + hi) >> 1) | 0;
                if (compare(item(mid, frameTicks), t) <= 0) {
                    lo_mut = (mid + 1);
                    hi_mut = hi;
                    continue search;
                }
                else {
                    lo_mut = lo;
                    hi_mut = (mid - 1);
                    continue search;
                }
            }
            break;
        }
    };
    return min(frameTicks.length, search(0, frameTicks.length - 1) + 1) | 0;
}

/**
 * The tick range covered by one cell of `level`.
 */
export function FlameLod_cellTickRange(lod, level, cell) {
    const first = (cell << level.Shift) | 0;
    const lastExcl = min(lod.FrameTicks.length, (cell + 1) << level.Shift) | 0;
    const startT = (first <= 0) ? (0n) : item(first - 1, lod.FrameTicks);
    return [startT, max_1(startT, (lastExcl >= lod.FrameTicks.length) ? lod.EndTick : item(lastExcl - 1, lod.FrameTicks))];
}

/**
 * The level-`shift` cell containing `tick`.
 */
export function FlameLod_cellAt(lod, shift, t) {
    return (FlameLod_cellOf(lod.FrameTicks, t) >> shift) | 0;
}

/**
 * Finest level whose cells are at least `minPx` wide at `ppTick`.
 */
export function FlameLod_pickLevel(lod, ppTick, minPx) {
    const go = (k_mut) => {
        go:
        while (true) {
            const k = k_mut;
            if (k >= (lod.Levels.length - 1)) {
                return k | 0;
            }
            else {
                const patternInput = FlameLod_cellTickRange(lod, item(k, lod.Levels), 0);
                if ((toFloat64(toInt64_unchecked(op_Subtraction(patternInput[1], patternInput[0]))) * ppTick) >= minPx) {
                    return k | 0;
                }
                else {
                    k_mut = (k + 1);
                    continue go;
                }
            }
            break;
        }
    };
    return go(0) | 0;
}

/**
 * Build the pyramid from a walked window: O(rects * overlapped cells)
 * once for level 0, then linear per coarser level. Rects arrive sorted by
 * start and same-depth invocations are sequential (one stack), so the
 * streaming per-cell max yields the true dominant function.
 */
export function FlameLod_build(w) {
    if ((w.FrameTicks.length === 0) ? true : (w.MaxDepth === 0)) {
        return FlameLod_empty;
    }
    else {
        const cols0 = w.FrameTicks.length | 0;
        const depthMax = w.MaxDepth | 0;
        const size = (cols0 * depthMax) | 0;
        const funcs = fill(new Int32Array(size), 0, size, -1);
        const dom = new BigInt64Array(size);
        const cov = new BigInt64Array(size);
        const arr = w.Rects;
        for (let idx = 0; idx <= (arr.length - 1); idx++) {
            const r = item(idx, arr);
            if ((compare(toInt64_unchecked(op_Subtraction(r.EndTick, r.StartTick)), 0n) > 0) && (r.Depth < depthMax)) {
                let c = FlameLod_cellOf(w.FrameTicks, r.StartTick);
                const cEnd = min(cols0 - 1, FlameLod_cellOf(w.FrameTicks, max_1(r.StartTick, toInt64_unchecked(op_Subtraction(r.EndTick, 1n))))) | 0;
                while (c <= cEnd) {
                    const cStart = (c === 0) ? (0n) : item(c - 1, w.FrameTicks);
                    const o = toInt64_unchecked(op_Subtraction(min_1(r.EndTick, item(c, w.FrameTicks)), max_1(r.StartTick, cStart)));
                    if (compare(o, 0n) > 0) {
                        const idx_1 = ((r.Depth * cols0) + c) | 0;
                        setItem(cov, idx_1, toInt64_unchecked(op_Addition(item(idx_1, cov), o)));
                        if (item(idx_1, funcs) === ~~r.Entry) {
                            setItem(dom, idx_1, toInt64_unchecked(op_Addition(item(idx_1, dom), o)));
                        }
                        else if (compare(o, item(idx_1, dom)) > 0) {
                            setItem(dom, idx_1, o);
                            setItem(funcs, idx_1, ~~r.Entry | 0);
                        }
                    }
                    c = ((c + 1) | 0);
                }
            }
        }
        const levels = [];
        const addLevel = (shift_mut, cols_mut, f_mut, d_mut, cv_mut) => {
            addLevel:
            while (true) {
                const shift = shift_mut, cols = cols_mut, f = f_mut, d = d_mut, cv = cv_mut;
                void (levels.push(new FlameLod_Level(shift, cols, f, d, cv)));
                if (cols > 1) {
                    const nc = ~~((cols + 1) / 2) | 0;
                    const nf = fill(new Int32Array(nc * depthMax), 0, nc * depthMax, -1);
                    const nd = new BigInt64Array(nc * depthMax);
                    const ncv = new BigInt64Array(nc * depthMax);
                    for (let depth = 0; depth <= (depthMax - 1); depth++) {
                        for (let j = 0; j <= (nc - 1); j++) {
                            const dst = ((depth * nc) + j) | 0;
                            const baseIdx = (depth * cols) | 0;
                            const i0 = (baseIdx + (j * 2)) | 0;
                            const hasSecond = ((j * 2) + 1) < cols;
                            const matchValue = item(i0, f) | 0;
                            const matchValue_1 = item(i0, d);
                            const matchValue_2 = item(i0, cv);
                            const f0 = matchValue | 0;
                            const d0 = matchValue_1;
                            const patternInput_1 = hasSecond ? [item(baseIdx + ((j * 2) + 1), f), item(baseIdx + ((j * 2) + 1), d), item(baseIdx + ((j * 2) + 1), cv)] : [-1, 0n, 0n];
                            const f1 = patternInput_1[0] | 0;
                            const d1 = patternInput_1[1];
                            const t = toInt64_unchecked(op_Addition(matchValue_2, patternInput_1[2]));
                            setItem(ncv, dst, t);
                            if (equals(t, 0n)) {
                            }
                            else if (f0 === f1) {
                                setItem(nf, dst, f0 | 0);
                                setItem(nd, dst, toInt64_unchecked(op_Addition(d0, d1)));
                            }
                            else if (f1 < 0) {
                                setItem(nf, dst, f0 | 0);
                                setItem(nd, dst, d0);
                            }
                            else if (f0 < 0) {
                                setItem(nf, dst, f1 | 0);
                                setItem(nd, dst, d1);
                            }
                            else if (compare(d0, d1) >= 0) {
                                setItem(nf, dst, f0 | 0);
                                setItem(nd, dst, d0);
                            }
                            else {
                                setItem(nf, dst, f1 | 0);
                                setItem(nd, dst, d1);
                            }
                        }
                    }
                    shift_mut = (shift + 1);
                    cols_mut = nc;
                    f_mut = nf;
                    d_mut = nd;
                    cv_mut = ncv;
                    continue addLevel;
                }
                break;
            }
        };
        addLevel(0, cols0, funcs, dom, cov);
        return new FlameLod_Lod(levels.slice(), w.FrameTicks, w.EndTick);
    }
}

/**
 * Run-length encode the visible cells of one depth row at one level:
 * adjacent cells merge while function and coverage bucket match. Returns
 * parallel arrays (funcs, buckets, cellStarts, cellLens) - the painter
 * turns each run into one rectangle, so drawn geometry is bounded by
 * pixels, not by the number of invocations in the trace.
 */
export function FlameLod_runs(lod, level, depth, cellFrom, cellTo) {
    const cellTo_1 = min(cellTo, level.Cols) | 0;
    const funcs = [];
    const buckets = [];
    const starts = [];
    const lens = [];
    let runFunc = -2;
    let runBucket = -2;
    let runStart = cellFrom;
    let c = cellFrom;
    while (c < cellTo_1) {
        const idx = ((depth * level.Cols) + c) | 0;
        const f = item(idx, level.Funcs) | 0;
        let b;
        const level_1 = level;
        const idx_1 = idx | 0;
        const c_1 = item(idx_1, level_1.Covered);
        if (compare(c_1, 0n) <= 0) {
            b = -1;
        }
        else {
            const share = toFloat64(item(idx_1, level_1.Dominated)) / toFloat64(c_1);
            b = ((share >= 0.75) ? 3 : ((share >= 0.5) ? 2 : ((share >= 0.25) ? 1 : 0)));
        }
        if ((f !== runFunc) ? true : (b !== runBucket)) {
            if (runFunc !== -2) {
                void (funcs.push(runFunc));
                void (buckets.push(runBucket));
                void (starts.push(runStart));
                void (lens.push(c - runStart));
            }
            runFunc = (f | 0);
            runBucket = (b | 0);
            runStart = (c | 0);
        }
        c = ((c + 1) | 0);
    }
    if ((runFunc !== -2) && (cellTo_1 > runStart)) {
        void (funcs.push(runFunc));
        void (buckets.push(runBucket));
        void (starts.push(runStart));
        void (lens.push(cellTo_1 - runStart));
    }
    return [funcs.slice(), buckets.slice(), starts.slice(), lens.slice()];
}

