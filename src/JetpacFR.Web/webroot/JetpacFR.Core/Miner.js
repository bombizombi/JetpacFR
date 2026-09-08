
import { FSharpRef, Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { uint32_type, class_type, array_type, record_type, string_type, bool_type, list_type, tuple_type, int64_type, int32_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { TraceQuery_nearestSnapshotBefore, RegSnapshot_$reflection } from "../TraceTypesWeb.js";
import { CallOps_isRet, CallOps_isCall } from "./Flame.js";
import { Dictionary } from "../fable_modules/fable-library-js.5.17.0/MutableMap.js";
import { compareArrays, comparePrimitives, defaultOf, arrayHash, equalArrays } from "../fable_modules/fable-library-js.5.17.0/Util.js";
import { addToSet, tryGetValue } from "../fable_modules/fable-library-js.5.17.0/MapUtil.js";
import { item } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { max, min } from "../fable_modules/fable-library-js.5.17.0/Double.js";
import { fromUInt8, op_Addition, toInt64_unchecked } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { filter, sortBy, fold, delay, exists, map, sortByDescending, toList } from "../fable_modules/fable-library-js.5.17.0/Seq.js";
import { printf, toText } from "../fable_modules/fable-library-js.5.17.0/String.js";
import { filter as filter_1, map as map_1, empty, tail, head, isEmpty, cons, reverse, sortByDescending as sortByDescending_1 } from "../fable_modules/fable-library-js.5.17.0/List.js";
import { FSharpSet__Contains, ofList } from "../fable_modules/fable-library-js.5.17.0/Set.js";

export class Routine extends Record {
    constructor(Entry, SpanLo, SpanHi, CallCount, InclusiveTStates, ExclusiveTStates, CallSites, LoopExtents, WriteRanges, InputSamples, ExitSamples, SelfModifying, Overlapping, Score, Risk, Reasons) {
        super();
        this.Entry = (Entry | 0);
        this.SpanLo = (SpanLo | 0);
        this.SpanHi = (SpanHi | 0);
        this.CallCount = (CallCount | 0);
        this.InclusiveTStates = InclusiveTStates;
        this.ExclusiveTStates = ExclusiveTStates;
        this.CallSites = CallSites;
        this.LoopExtents = LoopExtents;
        this.WriteRanges = WriteRanges;
        this.InputSamples = InputSamples;
        this.ExitSamples = ExitSamples;
        this.SelfModifying = SelfModifying;
        this.Overlapping = Overlapping;
        this.Score = (Score | 0);
        this.Risk = Risk;
        this.Reasons = Reasons;
    }
}

export function Routine_$reflection() {
    return record_type("JetpacFR.Core.Miner.Routine", [], Routine, () => [["Entry", int32_type], ["SpanLo", int32_type], ["SpanHi", int32_type], ["CallCount", int32_type], ["InclusiveTStates", int64_type], ["ExclusiveTStates", int64_type], ["CallSites", list_type(tuple_type(int32_type, int32_type))], ["LoopExtents", list_type(tuple_type(int32_type, int32_type, int32_type))], ["WriteRanges", list_type(tuple_type(int32_type, int32_type, int32_type, int32_type))], ["InputSamples", list_type(RegSnapshot_$reflection())], ["ExitSamples", list_type(RegSnapshot_$reflection())], ["SelfModifying", bool_type], ["Overlapping", bool_type], ["Score", int32_type], ["Risk", string_type], ["Reasons", list_type(string_type)]]);
}

export class CallEdge extends Record {
    constructor(Caller, Callee, Count) {
        super();
        this.Caller = (Caller | 0);
        this.Callee = (Callee | 0);
        this.Count = (Count | 0);
    }
}

export function CallEdge_$reflection() {
    return record_type("JetpacFR.Core.Miner.CallEdge", [], CallEdge, () => [["Caller", int32_type], ["Callee", int32_type], ["Count", int32_type]]);
}

const isCallOp = CallOps_isCall;

const isRetOp = (b) => ((b_1) => CallOps_isRet(b, b_1));

class Frame extends Record {
    constructor(Entry, SpanLo, SpanHi, Exclusive, Inclusive) {
        super();
        this.Entry = (Entry | 0);
        this.SpanLo = (SpanLo | 0);
        this.SpanHi = (SpanHi | 0);
        this.Exclusive = Exclusive;
        this.Inclusive = Inclusive;
    }
}

function Frame_$reflection() {
    return record_type("JetpacFR.Core.Miner.Frame", [], Frame, () => [["Entry", int32_type], ["SpanLo", int32_type], ["SpanHi", int32_type], ["Exclusive", int64_type], ["Inclusive", int64_type]]);
}

class Accum extends Record {
    constructor(Entry, SpanLo, SpanHi, CallCount, Inclusive, Exclusive, CallSites, DistinctSites, Loops, Samples, SampleTicks, Exits, ExitTicks, Writes) {
        super();
        this.Entry = (Entry | 0);
        this.SpanLo = (SpanLo | 0);
        this.SpanHi = (SpanHi | 0);
        this.CallCount = (CallCount | 0);
        this.Inclusive = Inclusive;
        this.Exclusive = Exclusive;
        this.CallSites = CallSites;
        this.DistinctSites = DistinctSites;
        this.Loops = Loops;
        this.Samples = Samples;
        this.SampleTicks = SampleTicks;
        this.Exits = Exits;
        this.ExitTicks = ExitTicks;
        this.Writes = Writes;
    }
}

function Accum_$reflection() {
    return record_type("JetpacFR.Core.Miner.Accum", [], Accum, () => [["Entry", int32_type], ["SpanLo", int32_type], ["SpanHi", int32_type], ["CallCount", int32_type], ["Inclusive", int64_type], ["Exclusive", int64_type], ["CallSites", array_type(tuple_type(int32_type, int32_type))], ["DistinctSites", class_type("System.Collections.Generic.HashSet`1", [int32_type])], ["Loops", class_type("System.Collections.Generic.Dictionary`2", [tuple_type(int32_type, int32_type), int32_type])], ["Samples", array_type(RegSnapshot_$reflection())], ["SampleTicks", class_type("System.Collections.Generic.HashSet`1", [uint32_type])], ["Exits", array_type(RegSnapshot_$reflection())], ["ExitTicks", class_type("System.Collections.Generic.HashSet`1", [uint32_type])], ["Writes", class_type("System.Collections.Generic.Dictionary`2", [int32_type, tuple_type(int32_type, int32_type)])]]);
}

/**
 * Mine the trace window. Returns (routines, call edges) sorted by lift
 * score, hot first. A caller of -1 means "called with an empty stack"
 * (top-level / initial execution).
 */
export function mine(trace) {
    let entriesSet;
    const accums = new Map([]);
    const edges = new Dictionary([], {
        Equals: equalArrays,
        GetHashCode: (x) => (arrayHash(x) | 0),
    });
    const stack = [];
    const accumOf = (entry) => {
        let matchValue;
        let outArg = defaultOf();
        matchValue = [tryGetValue(accums, entry, new FSharpRef(() => outArg, (v) => {
            outArg = v;
        })), outArg];
        if (matchValue[0]) {
            return matchValue[1];
        }
        else {
            const a_1 = new Accum(entry, entry, entry, 0, 0n, 0n, [], new Set([]), new Dictionary([], {
                Equals: equalArrays,
                GetHashCode: (x_1) => (arrayHash(x_1) | 0),
            }), [], new Set([]), [], new Set([]), new Map([]));
            accums.set(entry, a_1);
            return a_1;
        }
    };
    const entries = trace.Entries;
    let w = 0;
    let i = 0;
    while (i < entries.length) {
        let matchValue_2, outArg_2, matchValue_3, outArg_3;
        const e = item(i, entries);
        while ((w < trace.Writes.length) && (item(w, trace.Writes).Tick < e.Tick)) {
            if (stack.length > 0) {
                const a_2 = accumOf(item(stack.length - 1, stack).Entry);
                const addr = ~~item(w, trace.Writes).Address | 0;
                const value = ~~item(w, trace.Writes).NewValue | 0;
                let count;
                let matchValue_1;
                let outArg_1 = defaultOf();
                matchValue_1 = [tryGetValue(a_2.Writes, addr, new FSharpRef(() => outArg_1, (v_1) => {
                    outArg_1 = v_1;
                })), outArg_1];
                count = (matchValue_1[0] ? matchValue_1[1][0] : 0);
                a_2.Writes.set(addr, [count + 1, value]);
            }
            w = ((w + 1) | 0);
        }
        if (e.Length === 0) {
            if (stack.length < 128) {
                const entry_1 = ~~e.Pc | 0;
                void (stack.push(new Frame(entry_1, entry_1, entry_1, 0n, 0n)));
            }
        }
        else {
            if (stack.length > 0) {
                const top = item(stack.length - 1, stack);
                top.SpanLo = (min(top.SpanLo, ~~e.Pc) | 0);
                top.SpanHi = (max(top.SpanHi, (~~e.Pc + ~~e.Length) - 1) | 0);
                top.Exclusive = toInt64_unchecked(op_Addition(top.Exclusive, toInt64_unchecked(fromUInt8(e.Cycles))));
            }
            const b0 = e.B0;
            if (isCallOp(b0) && (e.Taken === 1)) {
                const callee = ~~e.Target | 0;
                const returnPc = ((~~e.Pc + ~~e.Length) & 65535) | 0;
                const key = [(stack.length > 0) ? item(stack.length - 1, stack).Entry : -1, callee];
                edges.set(key, ((matchValue_2 = ((outArg_2 = 0, [tryGetValue(edges, key, new FSharpRef(() => (outArg_2 | 0), (v_2) => {
                    outArg_2 = (v_2 | 0);
                })), outArg_2])), matchValue_2[0] ? matchValue_2[1] : 0)) + 1);
                const a_3 = accumOf(callee);
                a_3.CallCount = ((a_3.CallCount + 1) | 0);
                if (a_3.CallSites.length < 64) {
                    void (a_3.CallSites.push([~~e.Pc, returnPc]));
                }
                if (addToSet(~~e.Pc, a_3.DistinctSites) && (a_3.Samples.length < 5)) {
                    const si = TraceQuery_nearestSnapshotBefore(trace.Snapshots, e.Tick) | 0;
                    if ((si >= 0) && addToSet(item(si, trace.Snapshots).Tick, a_3.SampleTicks)) {
                        void (a_3.Samples.push(item(si, trace.Snapshots)));
                    }
                }
                if (stack.length < 128) {
                    void (stack.push(new Frame(callee, callee, callee, 0n, 0n)));
                }
            }
            else if (isRetOp(b0)(e.B1) && (e.Taken === 1)) {
                if (stack.length > 0) {
                    const f = item(stack.length - 1, stack);
                    stack.splice(stack.length - 1, 1);
                    const a_4 = accumOf(f.Entry);
                    a_4.SpanLo = (min(a_4.SpanLo, f.SpanLo) | 0);
                    a_4.SpanHi = (max(a_4.SpanHi, f.SpanHi) | 0);
                    a_4.Exclusive = toInt64_unchecked(op_Addition(a_4.Exclusive, f.Exclusive));
                    const childIncl = toInt64_unchecked(op_Addition(f.Inclusive, f.Exclusive));
                    a_4.Inclusive = toInt64_unchecked(op_Addition(a_4.Inclusive, childIncl));
                    if (stack.length > 0) {
                        item(stack.length - 1, stack).Inclusive = toInt64_unchecked(op_Addition(item(stack.length - 1, stack).Inclusive, childIncl));
                    }
                    const si_1 = TraceQuery_nearestSnapshotBefore(trace.Snapshots, e.Tick) | 0;
                    if (((si_1 >= 0) && addToSet(item(si_1, trace.Snapshots).Tick, a_4.ExitTicks)) && (a_4.Exits.length < 5)) {
                        void (a_4.Exits.push(item(si_1, trace.Snapshots)));
                    }
                }
            }
            else if (((e.Taken === 1) && (~~e.Target < ~~e.Pc)) && (stack.length > 0)) {
                const a_5 = accumOf(item(stack.length - 1, stack).Entry);
                const key_1 = [~~e.Pc, ~~e.Target];
                a_5.Loops.set(key_1, ((matchValue_3 = ((outArg_3 = 0, [tryGetValue(a_5.Loops, key_1, new FSharpRef(() => (outArg_3 | 0), (v_4) => {
                    outArg_3 = (v_4 | 0);
                })), outArg_3])), matchValue_3[0] ? matchValue_3[1] : 0)) + 1);
            }
        }
        i = ((i + 1) | 0);
    }
    while (stack.length > 0) {
        const f_2 = item(stack.length - 1, stack);
        stack.splice(stack.length - 1, 1);
        const a_6 = accumOf(f_2.Entry);
        a_6.SpanLo = (min(a_6.SpanLo, f_2.SpanLo) | 0);
        a_6.SpanHi = (max(a_6.SpanHi, f_2.SpanHi) | 0);
        a_6.Exclusive = toInt64_unchecked(op_Addition(a_6.Exclusive, f_2.Exclusive));
        a_6.Inclusive = toInt64_unchecked(op_Addition(toInt64_unchecked(op_Addition(a_6.Inclusive, f_2.Inclusive)), f_2.Exclusive));
    }
    while (w < trace.Writes.length) {
        if (stack.length > 0) {
            const a_7 = accumOf(item(stack.length - 1, stack).Entry);
            const addr_1 = ~~item(w, trace.Writes).Address | 0;
            const value_1 = ~~item(w, trace.Writes).NewValue | 0;
            let count_1;
            let matchValue_4;
            let outArg_4 = defaultOf();
            matchValue_4 = [tryGetValue(a_7.Writes, addr_1, new FSharpRef(() => outArg_4, (v_6) => {
                outArg_4 = v_6;
            })), outArg_4];
            count_1 = (matchValue_4[0] ? matchValue_4[1][0] : 0);
            a_7.Writes.set(addr_1, [count_1 + 1, value_1]);
        }
        w = ((w + 1) | 0);
    }
    const riskNames = ["green", "yellow", "orange", "red"];
    const routines = toList(sortByDescending((r) => [r.Score, r.CallCount], map((a_10) => {
        let arg, arg_1;
        let score = min(100, a_10.CallCount * 10);
        const reasons = [];
        let risk = 0;
        let selfMod;
        let found = false;
        let addr_3 = a_10.SpanLo;
        while ((addr_3 <= a_10.SpanHi) && !found) {
            if (item(addr_3 & 65535, trace.SelfModified)) {
                found = true;
            }
            addr_3 = ((addr_3 + 1) | 0);
        }
        selfMod = found;
        if (selfMod) {
            risk = (max(risk, 3) | 0);
            score = ((score - 80) | 0);
            void (reasons.push("self-modifying"));
        }
        const overlapping = exists((other) => {
            if ((other.Entry !== a_10.Entry) && (other.Entry >= a_10.SpanLo)) {
                return other.Entry <= a_10.SpanHi;
            }
            else {
                return false;
            }
        }, accums.values());
        if (overlapping) {
            risk = (max(risk, 2) | 0);
            score = ((score - 30) | 0);
            void (reasons.push("overlapping entry"));
        }
        if (((a_10.SpanHi - a_10.SpanLo) + 1) > 1024) {
            risk = (max(risk, 1) | 0);
            score = ((score - 10) | 0);
            void (reasons.push((arg = (a_10.SpanLo | 0), (arg_1 = (a_10.SpanHi | 0), toText(printf("large span %04X-%04X"))(arg)(arg_1)))));
        }
        return new Routine(a_10.Entry, a_10.SpanLo, a_10.SpanHi, a_10.CallCount, a_10.Inclusive, a_10.Exclusive, toList(a_10.CallSites), sortByDescending_1((tupledArg) => (tupledArg[2] | 0), toList(delay(() => map((kv_2) => [kv_2[0][0], kv_2[0][1], kv_2[1]], a_10.Loops))), {
            Compare: (x_3, y_3) => (comparePrimitives(x_3, y_3) | 0),
        }), reverse(fold((acc, kv_1) => {
            const addr_2 = kv_1[0] | 0;
            const patternInput = kv_1[1];
            const value_2 = patternInput[1] | 0;
            const count_2 = patternInput[0] | 0;
            let matchResult, c_3, hi_1, lo_1, rest_1, v_8;
            if (!isEmpty(acc)) {
                if (((head(acc)[1] + 1) === addr_2) && (head(acc)[2] === value_2)) {
                    matchResult = 0;
                    c_3 = head(acc)[3];
                    hi_1 = head(acc)[1];
                    lo_1 = head(acc)[0];
                    rest_1 = tail(acc);
                    v_8 = head(acc)[2];
                }
                else {
                    matchResult = 1;
                }
            }
            else {
                matchResult = 1;
            }
            switch (matchResult) {
                case 0:
                    return cons([lo_1, addr_2, v_8, c_3 + count_2], rest_1);
                default:
                    return cons([addr_2, addr_2, value_2, count_2], acc);
            }
        }, empty(), sortBy((kv) => (kv[0] | 0), a_10.Writes, {
            Compare: (x_2, y_2) => (comparePrimitives(x_2, y_2) | 0),
        }))), toList(a_10.Samples), toList(a_10.Exits), selfMod, overlapping, max(0, score), item(risk, riskNames), toList(reasons));
    }, filter((a_9) => {
        if (a_9.CallCount > 0) {
            return true;
        }
        else {
            return ((a_9.SpanHi - a_9.SpanLo) + 1) <= 4096;
        }
    }, accums.values())), {
        Compare: (x_4, y_4) => (compareArrays(x_4, y_4) | 0),
    }));
    return [routines, (entriesSet = ofList(map_1((r_1) => (r_1.Entry | 0), routines), {
        Compare: (x_5, y_5) => (comparePrimitives(x_5, y_5) | 0),
    }), sortByDescending_1((e_2) => (e_2.Count | 0), filter_1((e_1) => FSharpSet__Contains(entriesSet, e_1.Callee), toList(delay(() => map((kv_3) => (new CallEdge(kv_3[0][0], kv_3[0][1], kv_3[1])), edges)))), {
        Compare: (x_6, y_6) => (comparePrimitives(x_6, y_6) | 0),
    }))];
}

