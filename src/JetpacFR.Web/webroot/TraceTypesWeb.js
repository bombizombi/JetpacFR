
import { Record } from "./fable_modules/fable-library-js.5.17.0/Types.js";
import { class_type, bool_type, int32_type, array_type, record_type, uint32_type, uint8_type, uint16_type } from "./fable_modules/fable-library-js.5.17.0/Reflection.js";
import { copy, setItem, getSubArray, copyTo, item, fill } from "./fable_modules/fable-library-js.5.17.0/Array.js";
import { max } from "./fable_modules/fable-library-js.5.17.0/Double.js";
import { copyToArray, Exception } from "./fable_modules/fable-library-js.5.17.0/Util.js";

/**
 * One executed instruction in the trace window. Bytes are the bytes the
 * machine actually executed (self-modifying code included); Tick is the
 * absolute machine cycle before the instruction ran.
 */
export class TraceEntry extends Record {
    constructor(Pc, B0, B1, B2, B3, Target, Tick, Length, Cycles, FlagsBefore, FlagsAfter, Taken) {
        super();
        this.Pc = Pc;
        this.B0 = B0;
        this.B1 = B1;
        this.B2 = B2;
        this.B3 = B3;
        this.Target = Target;
        this.Tick = Tick;
        this.Length = Length;
        this.Cycles = Cycles;
        this.FlagsBefore = FlagsBefore;
        this.FlagsAfter = FlagsAfter;
        this.Taken = Taken;
    }
}

export function TraceEntry_$reflection() {
    return record_type("JetpacFR.Core.TraceEntry", [], TraceEntry, () => [["Pc", uint16_type], ["B0", uint8_type], ["B1", uint8_type], ["B2", uint8_type], ["B3", uint8_type], ["Target", uint16_type], ["Tick", uint32_type], ["Length", uint8_type], ["Cycles", uint8_type], ["FlagsBefore", uint8_type], ["FlagsAfter", uint8_type], ["Taken", uint8_type]]);
}

/**
 * Periodic full register snapshot (every 64 instructions by default),
 * time-ordered so the cinema can binary-search the state nearest any entry.
 */
export class RegSnapshot extends Record {
    constructor(Tick, Af, Bc, De, Hl, Af2, Bc2, De2, Hl2, Ix, Iy, Sp, Pc, I, R) {
        super();
        this.Tick = Tick;
        this.Af = Af;
        this.Bc = Bc;
        this.De = De;
        this.Hl = Hl;
        this.Af2 = Af2;
        this.Bc2 = Bc2;
        this.De2 = De2;
        this.Hl2 = Hl2;
        this.Ix = Ix;
        this.Iy = Iy;
        this.Sp = Sp;
        this.Pc = Pc;
        this.I = I;
        this.R = R;
    }
}

export function RegSnapshot_$reflection() {
    return record_type("JetpacFR.Core.RegSnapshot", [], RegSnapshot, () => [["Tick", uint32_type], ["Af", uint16_type], ["Bc", uint16_type], ["De", uint16_type], ["Hl", uint16_type], ["Af2", uint16_type], ["Bc2", uint16_type], ["De2", uint16_type], ["Hl2", uint16_type], ["Ix", uint16_type], ["Iy", uint16_type], ["Sp", uint16_type], ["Pc", uint16_type], ["I", uint8_type], ["R", uint8_type]]);
}

/**
 * A memory byte change (fires only when the value actually changes).
 */
export class MemWriteEvent extends Record {
    constructor(Tick, Address, OldValue, NewValue) {
        super();
        this.Tick = Tick;
        this.Address = Address;
        this.OldValue = OldValue;
        this.NewValue = NewValue;
    }
}

export function MemWriteEvent_$reflection() {
    return record_type("JetpacFR.Core.MemWriteEvent", [], MemWriteEvent, () => [["Tick", uint32_type], ["Address", uint16_type], ["OldValue", uint8_type], ["NewValue", uint8_type]]);
}

/**
 * A port OUT (beeper, border, tape, ...).
 */
export class PortEvent extends Record {
    constructor(Tick, Port, Value, Border) {
        super();
        this.Tick = Tick;
        this.Port = Port;
        this.Value = Value;
        this.Border = Border;
    }
}

export function PortEvent_$reflection() {
    return record_type("JetpacFR.Core.PortEvent", [], PortEvent, () => [["Tick", uint32_type], ["Port", uint16_type], ["Value", uint8_type], ["Border", uint8_type]]);
}

/**
 * Linearized trace window: entries in execution order plus side streams.
 */
export class Trace extends Record {
    constructor(Entries, Snapshots, Writes, Ports, FrameTicks, PerPcCount, SelfModified, SelfModCount, FirstIndexAtPc, StartTick, EndTick) {
        super();
        this.Entries = Entries;
        this.Snapshots = Snapshots;
        this.Writes = Writes;
        this.Ports = Ports;
        this.FrameTicks = FrameTicks;
        this.PerPcCount = PerPcCount;
        this.SelfModified = SelfModified;
        this.SelfModCount = (SelfModCount | 0);
        this.FirstIndexAtPc = FirstIndexAtPc;
        this.StartTick = StartTick;
        this.EndTick = EndTick;
    }
}

export function Trace_$reflection() {
    return record_type("JetpacFR.Core.Trace", [], Trace, () => [["Entries", array_type(TraceEntry_$reflection())], ["Snapshots", array_type(RegSnapshot_$reflection())], ["Writes", array_type(MemWriteEvent_$reflection())], ["Ports", array_type(PortEvent_$reflection())], ["FrameTicks", array_type(uint32_type)], ["PerPcCount", array_type(int32_type)], ["SelfModified", array_type(bool_type)], ["SelfModCount", int32_type], ["FirstIndexAtPc", array_type(int32_type)], ["StartTick", uint32_type], ["EndTick", uint32_type]]);
}

/**
 * Circular-buffer recorder. Zero allocation per instruction: entries land in
 * preallocated arrays; only write/port side streams (sparse) use a
 * ResizeArray. When the ring is full the oldest entry is evicted and the
 * per-PC and per-segment counters are decremented, so the heatmap and the
 * density strip always describe the current window.
 */
export class TraceRecorder {
    constructor(capacity, segmentCount) {
        this.capacity = (capacity | 0);
        this.entries = fill(new Array(this.capacity), 0, this.capacity, new TraceEntry(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        this.snapshots = fill(new Array(~~(this.capacity / 64) + 1), 0, ~~(this.capacity / 64) + 1, new RegSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        this.writes = [];
        this.ports = [];
        this.frameTicks = [];
        this.perPc = (new Int32Array(65536));
        this.segments = (new Int32Array(max(1, segmentCount)));
        this.segmentSize = (max(1, ~~(((this.capacity + max(1, segmentCount)) - 1) / max(1, segmentCount))) | 0);
        this.selfModified = fill(new Array(65536), 0, 65536, false);
        this.selfModCount = 0;
        this.head = 0;
        this.count = 0;
        this.snapshotCount = 0;
        this.recordEnabled = true;
        this.startTick = 0;
        this.endTick = 0;
        if (this.capacity < 1) {
            throw new Exception("capacity must be positive (Parameter \'capacity\')");
        }
    }
}

export function TraceRecorder_$reflection() {
    return class_type("JetpacFR.Core.TraceRecorder", undefined, TraceRecorder);
}

export function TraceRecorder_$ctor_Z37302880(capacity, segmentCount) {
    return new TraceRecorder(capacity, segmentCount);
}

export function TraceRecorder__get_EntryCount(this$) {
    return this$.count | 0;
}

export function TraceRecorder__get_Capacity(this$) {
    return this$.capacity | 0;
}

export function TraceRecorder__get_PerPcCount(this$) {
    return this$.perPc;
}

export function TraceRecorder__get_SegmentCounts(this$) {
    return this$.segments;
}

export function TraceRecorder__get_SegmentCount(this$) {
    return this$.segments.length | 0;
}

export function TraceRecorder__get_SelfModified(this$) {
    return this$.selfModified;
}

export function TraceRecorder__get_SelfModCount(this$) {
    return this$.selfModCount | 0;
}

export function TraceRecorder__get_StartTick(this$) {
    return this$.startTick;
}

export function TraceRecorder__get_EndTick(this$) {
    return this$.endTick;
}

export function TraceRecorder__get_RecordEnabled(this$) {
    return this$.recordEnabled;
}

export function TraceRecorder__set_RecordEnabled_Z1FBCCD16(this$, v) {
    this$.recordEnabled = v;
}

export function TraceRecorder__Record_A4DCE76(this$, entry) {
    if (this$.recordEnabled) {
        if (this$.count === this$.capacity) {
            const old = item(this$.head, this$.entries);
            this$.perPc[~~old.Pc] = ((item(~~old.Pc, this$.perPc) - 1) | 0);
            this$.segments[~~(this$.head / this$.segmentSize)] = ((item(~~(this$.head / this$.segmentSize), this$.segments) - 1) | 0);
        }
        else {
            this$.count = ((this$.count + 1) | 0);
        }
        if (this$.count === 1) {
            this$.startTick = entry.Tick;
        }
        this$.entries[this$.head] = entry;
        this$.perPc[~~entry.Pc] = ((item(~~entry.Pc, this$.perPc) + 1) | 0);
        this$.segments[~~(this$.head / this$.segmentSize)] = ((item(~~(this$.head / this$.segmentSize), this$.segments) + 1) | 0);
        this$.endTick = entry.Tick;
        this$.head = (((this$.head + 1) % this$.capacity) | 0);
        if (this$.count === this$.capacity) {
            this$.startTick = item(this$.head, this$.entries).Tick;
        }
    }
}

export function TraceRecorder__RecordSnapshot_7114161F(this$, s) {
    if (this$.recordEnabled) {
        if (this$.snapshotCount === this$.snapshots.length) {
            const keep = ~~(this$.snapshots.length / 2) | 0;
            copyTo(this$.snapshots, this$.snapshots.length - keep, this$.snapshots, 0, keep);
            this$.snapshotCount = (keep | 0);
        }
        this$.snapshots[this$.snapshotCount] = s;
        this$.snapshotCount = ((this$.snapshotCount + 1) | 0);
    }
}

export function TraceRecorder__RecordWrite_Z30129C29(this$, w) {
    if (this$.recordEnabled) {
        void (this$.writes.push(w));
        const addr = ~~w.Address | 0;
        if ((item(addr, this$.perPc) > 0) && !item(addr, this$.selfModified)) {
            this$.selfModified[addr] = true;
            this$.selfModCount = ((this$.selfModCount + 1) | 0);
        }
    }
}

export function TraceRecorder__RecordPort_Z54D84C2A(this$, p) {
    if (this$.recordEnabled) {
        void (this$.ports.push(p));
    }
}

export function TraceRecorder__RecordFrameBoundary_Z6EF827D7(this$, tick) {
    if (this$.recordEnabled) {
        void (this$.frameTicks.push(tick));
    }
}

/**
 * Linearize the ring into a fresh Trace. O(window) copy; call on pause or
 * before save, not per cursor move.
 */
export function TraceRecorder__Build(this$) {
    const n = this$.count | 0;
    const linear = fill(new Array(n), 0, n, new TraceEntry(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    if (n > 0) {
        if (n < this$.capacity) {
            copyToArray(this$.entries, 0, linear, 0, n);
        }
        else {
            copyToArray(this$.entries, this$.head, linear, 0, this$.capacity - this$.head);
            copyToArray(this$.entries, 0, linear, this$.capacity - this$.head, this$.head);
        }
    }
    const snaps = getSubArray(this$.snapshots, 0, this$.snapshotCount);
    const firstAt = fill(new Int32Array(65536), 0, 65536, -1);
    for (let i = 0; i <= (n - 1); i++) {
        const pc = ~~item(i, linear).Pc | 0;
        if (item(pc, firstAt) < 0) {
            setItem(firstAt, pc, i | 0);
        }
    }
    return new Trace(linear, snaps, this$.writes.slice(), this$.ports.slice(), this$.frameTicks.slice(), copy(this$.perPc), copy(this$.selfModified), this$.selfModCount, firstAt, this$.startTick, this$.endTick);
}

/**
 * Nearest snapshot index with Tick <= target (snapshots are time-ordered).
 */
export function TraceQuery_nearestSnapshotBefore(snapshots, target) {
    const search = (lo_mut, hi_mut) => {
        search:
        while (true) {
            const lo = lo_mut, hi = hi_mut;
            if (lo > hi) {
                return hi | 0;
            }
            else {
                const mid = ((lo + hi) >> 1) | 0;
                if (item(mid, snapshots).Tick <= target) {
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
    return search(0, snapshots.length - 1) | 0;
}

