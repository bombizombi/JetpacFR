
import { fill, item, setItem } from "./fable_modules/fable-library-js.5.17.0/Array.js";
import { Exception, copyToArray } from "./fable_modules/fable-library-js.5.17.0/Util.js";
import { printf, toFail } from "./fable_modules/fable-library-js.5.17.0/String.js";
import { Trace, PortEvent, MemWriteEvent, RegSnapshot, TraceEntry } from "./TraceTypesWeb.js";

const magic = new Uint8Array(["J".charCodeAt(0) & 0xFF, "P".charCodeAt(0) & 0xFF, "T".charCodeAt(0) & 0xFF, "R".charCodeAt(0) & 0xFF]);

function w8(b, o, v) {
    setItem(b, o, (v & 255) & 0xFF);
}

function w16(b, o, v) {
    setItem(b, o, (v & 255) & 0xFF);
    setItem(b, o + 1, ((v >> 8) & 255) & 0xFF);
}

function w32(b, o, v) {
    setItem(b, o, (v & 255) & 0xFF);
    setItem(b, o + 1, ((v >> 8) & 255) & 0xFF);
    setItem(b, o + 2, ((v >> 16) & 255) & 0xFF);
    setItem(b, o + 3, ((v >> 24) & 255) & 0xFF);
}

function r8(b, o) {
    return ~~item(o, b) | 0;
}

function r16(b, o) {
    return (~~item(o, b) | (~~item(o + 1, b) << 8)) | 0;
}

function r32(b, o) {
    return (((~~item(o, b) | (~~item(o + 1, b) << 8)) | (~~item(o + 2, b) << 16)) | (~~item(o + 3, b) << 24)) | 0;
}

/**
 * Serialize a trace to the .jpt byte stream (identical layout to Core's writer).
 */
export function encode(trace) {
    const total = (((((((34 + (trace.Entries.length * 20)) + (trace.Snapshots.length * 32)) + (trace.Writes.length * 8)) + (trace.Ports.length * 8)) + (trace.FrameTicks.length * 4)) + (65536 * 4)) + 65536) | 0;
    const b = new Uint8Array(total);
    let o = 0;
    copyToArray(magic, 0, b, 0, 4);
    o = ((o + 4) | 0);
    w16(b, o, 1);
    o = ((o + 2) | 0);
    w32(b, o, ~~trace.StartTick);
    o = ((o + 4) | 0);
    w32(b, o, ~~trace.EndTick);
    o = ((o + 4) | 0);
    w32(b, o, trace.Entries.length);
    o = ((o + 4) | 0);
    w32(b, o, trace.Snapshots.length);
    o = ((o + 4) | 0);
    w32(b, o, trace.Writes.length);
    o = ((o + 4) | 0);
    w32(b, o, trace.Ports.length);
    o = ((o + 4) | 0);
    w32(b, o, trace.FrameTicks.length);
    o = ((o + 4) | 0);
    const arr = trace.Entries;
    for (let idx = 0; idx <= (arr.length - 1); idx++) {
        const e = item(idx, arr);
        w16(b, o, ~~e.Pc);
        w8(b, o + 2, ~~e.B0);
        w8(b, o + 3, ~~e.B1);
        w8(b, o + 4, ~~e.B2);
        w8(b, o + 5, ~~e.B3);
        w16(b, o + 6, ~~e.Target);
        w32(b, o + 8, ~~e.Tick);
        w8(b, o + 12, ~~e.Length);
        w8(b, o + 13, ~~e.Cycles);
        w8(b, o + 14, ~~e.FlagsBefore);
        w8(b, o + 15, ~~e.FlagsAfter);
        w8(b, o + 16, ~~e.Taken);
        o = ((o + 20) | 0);
    }
    const arr_1 = trace.Snapshots;
    for (let idx_1 = 0; idx_1 <= (arr_1.length - 1); idx_1++) {
        const s = item(idx_1, arr_1);
        w32(b, o, ~~s.Tick);
        w16(b, o + 4, ~~s.Af);
        w16(b, o + 6, ~~s.Bc);
        w16(b, o + 8, ~~s.De);
        w16(b, o + 10, ~~s.Hl);
        w16(b, o + 12, ~~s.Af2);
        w16(b, o + 14, ~~s.Bc2);
        w16(b, o + 16, ~~s.De2);
        w16(b, o + 18, ~~s.Hl2);
        w16(b, o + 20, ~~s.Ix);
        w16(b, o + 22, ~~s.Iy);
        w16(b, o + 24, ~~s.Sp);
        w16(b, o + 26, ~~s.Pc);
        w8(b, o + 28, ~~s.I);
        w8(b, o + 29, ~~s.R);
        o = ((o + 32) | 0);
    }
    const arr_2 = trace.Writes;
    for (let idx_2 = 0; idx_2 <= (arr_2.length - 1); idx_2++) {
        const w = item(idx_2, arr_2);
        w32(b, o, ~~w.Tick);
        w16(b, o + 4, ~~w.Address);
        w8(b, o + 6, ~~w.OldValue);
        w8(b, o + 7, ~~w.NewValue);
        o = ((o + 8) | 0);
    }
    const arr_3 = trace.Ports;
    for (let idx_3 = 0; idx_3 <= (arr_3.length - 1); idx_3++) {
        const p = item(idx_3, arr_3);
        w32(b, o, ~~p.Tick);
        w16(b, o + 4, ~~p.Port);
        w8(b, o + 6, ~~p.Value);
        w8(b, o + 7, ~~p.Border);
        o = ((o + 8) | 0);
    }
    const arr_4 = trace.FrameTicks;
    for (let idx_4 = 0; idx_4 <= (arr_4.length - 1); idx_4++) {
        const t = item(idx_4, arr_4);
        w32(b, o, ~~t);
        o = ((o + 4) | 0);
    }
    for (let i = 0; i <= 65535; i++) {
        w32(b, o, item(i, trace.PerPcCount));
        o = ((o + 4) | 0);
    }
    for (let i_1 = 0; i_1 <= 65535; i_1++) {
        w8(b, o, item(i_1, trace.SelfModified) ? 1 : 0);
        o = ((o + 1) | 0);
    }
    if (o !== total) {
        const arg = o | 0;
        toFail(printf("TraceCodecWeb: wrote %d bytes, expected %d"))(arg)(total);
    }
    return b;
}

/**
 * Parse a .jpt byte stream into a Trace (tolerates the omitted self-mod array).
 */
export function decode(b) {
    let array_1;
    let o = 0;
    if (b.length < 34) {
        const arg = b.length | 0;
        toFail(printf("trace file too short (%d bytes)"))(arg);
    }
    for (let i = 0; i <= 3; i++) {
        if (item(i, b) !== item(i, magic)) {
            throw new Exception("not a Jetpac trace file (bad magic)");
        }
    }
    o = ((o + 4) | 0);
    const v = r16(b, o) | 0;
    o = ((o + 2) | 0);
    if (v !== 1) {
        toFail(printf("unsupported trace version %d"))(v);
    }
    const startTick = r32(b, o) >>> 0;
    o = ((o + 4) | 0);
    const endTick = r32(b, o) >>> 0;
    o = ((o + 4) | 0);
    const entryCount = r32(b, o) | 0;
    o = ((o + 4) | 0);
    const snapCount = r32(b, o) | 0;
    o = ((o + 4) | 0);
    const writeCount = r32(b, o) | 0;
    o = ((o + 4) | 0);
    const portCount = r32(b, o) | 0;
    o = ((o + 4) | 0);
    const frameCount = r32(b, o) | 0;
    o = ((o + 4) | 0);
    const need = ((((((entryCount * 20) + (snapCount * 32)) + (writeCount * 8)) + (portCount * 8)) + (frameCount * 4)) + (65536 * 4)) | 0;
    if ((o + need) > b.length) {
        const arg_3 = (b.length - o) | 0;
        toFail(printf("trace file truncated: header claims %d more bytes but only %d remain"))(need)(arg_3);
    }
    const entries = fill(new Array(entryCount), 0, entryCount, new TraceEntry(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    for (let i_1 = 0; i_1 <= (entryCount - 1); i_1++) {
        setItem(entries, i_1, new TraceEntry(r16(b, o) & 0xFFFF, r8(b, o + 2) & 0xFF, r8(b, o + 3) & 0xFF, r8(b, o + 4) & 0xFF, r8(b, o + 5) & 0xFF, r16(b, o + 6) & 0xFFFF, r32(b, o + 8) >>> 0, r8(b, o + 12) & 0xFF, r8(b, o + 13) & 0xFF, r8(b, o + 14) & 0xFF, r8(b, o + 15) & 0xFF, r8(b, o + 16) & 0xFF));
        o = ((o + 20) | 0);
    }
    const snaps = fill(new Array(snapCount), 0, snapCount, new RegSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    for (let i_2 = 0; i_2 <= (snapCount - 1); i_2++) {
        setItem(snaps, i_2, new RegSnapshot(r32(b, o) >>> 0, r16(b, o + 4) & 0xFFFF, r16(b, o + 6) & 0xFFFF, r16(b, o + 8) & 0xFFFF, r16(b, o + 10) & 0xFFFF, r16(b, o + 12) & 0xFFFF, r16(b, o + 14) & 0xFFFF, r16(b, o + 16) & 0xFFFF, r16(b, o + 18) & 0xFFFF, r16(b, o + 20) & 0xFFFF, r16(b, o + 22) & 0xFFFF, r16(b, o + 24) & 0xFFFF, r16(b, o + 26) & 0xFFFF, r8(b, o + 28) & 0xFF, r8(b, o + 29) & 0xFF));
        o = ((o + 32) | 0);
    }
    const writes = fill(new Array(writeCount), 0, writeCount, new MemWriteEvent(0, 0, 0, 0));
    for (let i_3 = 0; i_3 <= (writeCount - 1); i_3++) {
        setItem(writes, i_3, new MemWriteEvent(r32(b, o) >>> 0, r16(b, o + 4) & 0xFFFF, r8(b, o + 6) & 0xFF, r8(b, o + 7) & 0xFF));
        o = ((o + 8) | 0);
    }
    const ports = fill(new Array(portCount), 0, portCount, new PortEvent(0, 0, 0, 0));
    for (let i_4 = 0; i_4 <= (portCount - 1); i_4++) {
        setItem(ports, i_4, new PortEvent(r32(b, o) >>> 0, r16(b, o + 4) & 0xFFFF, r8(b, o + 6) & 0xFF, r8(b, o + 7) & 0xFF));
        o = ((o + 8) | 0);
    }
    const frameTicks = new Uint32Array(frameCount);
    for (let i_5 = 0; i_5 <= (frameCount - 1); i_5++) {
        setItem(frameTicks, i_5, r32(b, o) >>> 0);
        o = ((o + 4) | 0);
    }
    const perPc = new Int32Array(65536);
    for (let i_6 = 0; i_6 <= 65535; i_6++) {
        setItem(perPc, i_6, r32(b, o) | 0);
        o = ((o + 4) | 0);
    }
    let selfMod;
    if ((o + 65536) <= b.length) {
        const a = fill(new Array(65536), 0, 65536, false);
        for (let i_7 = 0; i_7 <= 65535; i_7++) {
            setItem(a, i_7, item(o + i_7, b) !== 0);
        }
        selfMod = a;
    }
    else {
        selfMod = fill(new Array(65536), 0, 65536, false);
    }
    const firstAt = fill(new Int32Array(65536), 0, 65536, -1);
    for (let i_8 = 0; i_8 <= (entries.length - 1); i_8++) {
        const pc = ~~item(i_8, entries).Pc | 0;
        if (item(pc, firstAt) < 0) {
            setItem(firstAt, pc, i_8 | 0);
        }
    }
    return new Trace(entries, snaps, writes, ports, frameTicks, perPc, selfMod, (array_1 = selfMod.filter((x) => x), array_1.length), firstAt, startTick, endTick);
}

