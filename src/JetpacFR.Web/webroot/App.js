
import { Exception, int32ToString, disposeSafe, getEnumerator, comparePrimitives, defaultOf, createAtom } from "./fable_modules/fable-library-js.5.13.0/Util.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.13.0/FSharp.Core.js";
import { max as max_1, fill, initialize, setItem, item } from "./fable_modules/fable-library-js.5.13.0/Array.js";
import { tryFind, isEmpty, truncate, map as map_1, length, iterate, ofArray, singleton, empty } from "./fable_modules/fable-library-js.5.13.0/List.js";
import { add, remove, FSharpSet__get_Count, toList as toList_1, FSharpSet__Contains, empty as empty_1 } from "./fable_modules/fable-library-js.5.13.0/Set.js";
import { TraceSession_$ctor_28C3603C, TraceSession__get_CycleCount, TraceSession__get_Frame, EntryCache_save, EntryCache_tryLoad, TraceSession__get_ReplayEventCount, TraceSession__get_WarmStart, TraceSession__DrainBeeperSamples_Z524259C1, TraceSession__RunFrame, TraceSession__get_Regs, TraceSession__get_Memory, TraceSession__get_ScreenBuffer, TraceSession__get_Recorder, TraceSession__SetKey_289F56A } from "./SessionWeb.js";
import { TraceRecorder__set_RecordEnabled_Z1FBCCD16, TraceRecorder__get_SelfModCount, TraceRecorder__get_PerPcCount, TraceRecorder__get_Capacity, TraceRecorder__get_RecordEnabled, TraceRecorder__get_SegmentCounts, TraceQuery_nearestSnapshotBefore, TraceRecorder__get_SelfModified, TraceRecorder__Build, TraceRecorder__get_EntryCount } from "./TraceTypesWeb.js";
import { min, max } from "./fable_modules/fable-library-js.5.13.0/Double.js";
import { disasmBytes, disasmMemory } from "./JetpacFR.Core/Disasm.js";
import { toFail, substring, printf, toText, join } from "./fable_modules/fable-library-js.5.13.0/String.js";
import { fold, iterateIndexed, map, delay, toList } from "./fable_modules/fable-library-js.5.13.0/Seq.js";
import { rangeDouble } from "./fable_modules/fable-library-js.5.13.0/Range.js";
import { RegisterFile__Pc } from "./Jetpac2.Core/Machine.js";
import { getItemFromDict, tryGetValue } from "./fable_modules/fable-library-js.5.13.0/MapUtil.js";
import { toString, FSharpRef } from "./fable_modules/fable-library-js.5.13.0/Types.js";
import { parse } from "./fable_modules/fable-library-js.5.13.0/Int32.js";
import { toFloat64, fromUInt8, op_Addition, fromInt32, op_Multiply, op_Division, toInt64_unchecked, toInt32_unchecked } from "./fable_modules/fable-library-js.5.13.0/BigInt.js";
import { value as value_17 } from "./fable_modules/fable-library-js.5.13.0/Option.js";
import { extract } from "./JetpacFR.Core/Contract.js";
import { generate } from "./JetpacFR.Core/Prompt.js";
import { mine } from "./JetpacFR.Core/Miner.js";
import { decode, encode } from "./TraceCodecWeb.js";
import { AssetProvider } from "./BootWeb.js";
import { WarmState, WarmMemoryB64, TzxB64, RomB64, decode as decode_1 } from "./Embedded.js";
import { LiftedRoutines_registryHook } from "./Jetpac3.Core/LiftedRoutines.js";
import { defaultSession } from "./ScriptWeb.js";
import { run } from "./JetpacFR.Core/Validation.js";
import { createCancellationToken } from "./fable_modules/fable-library-js.5.13.0/Async.js";

export const Dom_document = document;

export const Dom_window = window;

export function Dom_byId(id) {
    return document.getElementById(id);
}

export function Dom_el(tag) {
    return document.createElement(tag);
}

export function Dom_append(parent, child) {
    return parent.appendChild(child);
}

export function Dom_clear(node) {
    node.textContent = "";
}

export function Dom_setTimeout(f, ms) {
    return setTimeout(f, ms);
}

export function Dom_setInterval(f, ms) {
    return setInterval(f, ms);
}

export let Audio_Enabled = createAtom(false);

const Audio_ring = new Float32Array(88200);

let Audio_wpos = 0;

let Audio_rpos = 0;

let Audio_ctx = defaultOf();

let Audio_node = defaultOf();

function Audio_ensure() {
    if (Operators_IsNull(Audio_ctx)) {
        Audio_ctx = (((window.AudioContext || window.webkitAudioContext) ? new (window.AudioContext || window.webkitAudioContext)() : null));
        if (!Operators_IsNull(Audio_ctx)) {
            Audio_node = (Audio_ctx.createScriptProcessor(4096, 0, 1));
            Audio_node.onaudioprocess = ((e) => {
                const out = e.outputBuffer.getChannelData(0);
                const n = out.length | 0;
                for (let i = 0; i <= (n - 1); i++) {
                    if (Audio_rpos !== Audio_wpos) {
                        setItem(out, i, item(Audio_rpos, Audio_ring));
                        Audio_rpos = (((Audio_rpos + 1) % Audio_ring.length) | 0);
                    }
                    else {
                        setItem(out, i, 0);
                    }
                }
            });
            Audio_node.connect(Audio_ctx.destination);
        }
    }
}

export function Audio_Play(samples) {
    if (Audio_Enabled()) {
        Audio_ensure();
        if (!Operators_IsNull(Audio_ctx)) {
            for (let idx = 0; idx <= (samples.length - 1); idx++) {
                const s = item(idx, samples);
                setItem(Audio_ring, Audio_wpos, s * 0.20000000298023224);
                Audio_wpos = (((Audio_wpos + 1) % Audio_ring.length) | 0);
            }
        }
    }
}

export function Audio_SetEnabled(on) {
    Audio_Enabled(on);
    if (on) {
        Audio_ensure();
        if (!Operators_IsNull(Audio_ctx)) {
            Audio_ctx.resume();
        }
    }
    Audio_rpos = (Audio_wpos | 0);
}

const App_bg = "#101016";

const App_panel = "#181C24";

const App_normal = "#C8C8CE";

const App_dim = "#8A8A92";

const App_green = "#4EE060";

const App_cyan = "#4ED0E0";

const App_yellow = "#E6D04E";

const App_red = "#E85454";

const App_orange = "#E88A2E";

const App_hiBg = "#2E3444";

const App_mono = "Consolas, monospace";

const App_heatLut = ["#08080C", "#0A1E4A", "#0A3A5E", "#0A5C6A", "#12865E", "#3AA83C", "#96B82C", "#D8A022", "#E86618", "#F03828"];

let App_statusText = defaultOf();

let App_disasmBox = defaultOf();

let App_cursorLabel = defaultOf();

let App_slider = defaultOf();

let App_pcCell = defaultOf();

let App_routineList = defaultOf();

let App_routineCountLabel = defaultOf();

let App_routineDetail = defaultOf();

let App_tray = defaultOf();

let App_trayCountLabel = defaultOf();

let App_contractText = defaultOf();

let App_contractLabel = defaultOf();

let App_theaterText = defaultOf();

let App_pasteBox = defaultOf();

let App_playBtn = defaultOf();

let App_chkRec = defaultOf();

let App_chkMute = defaultOf();

let App_gameCtx = defaultOf();

let App_gameImg = defaultOf();

let App_heatCtx = defaultOf();

let App_heatImg = defaultOf();

let App_stripCtx = defaultOf();

let App_stripImg = defaultOf();

let App_graphCtx = defaultOf();

let App_graphCanvas = defaultOf();

let App_frameTimerId = defaultOf();

let App_cinemaTimerId = defaultOf();

let App_session = undefined;

let App_running = false;

let App_cursor = 0;

let App_built = undefined;

let App_builtAtCount = -1;

let App_loaded = undefined;

let App_syncingSlider = false;

let App_syncingRoutines = false;

let App_cinemaPlaying = false;

let App_cinemaSpeed = 10;

let App_markerPc = 0;

let App_minedRoutines = empty();

let App_minedEdges = empty();

let App_selectedEntries = empty_1({
    Compare: (x, y) => (comparePrimitives(x, y) | 0),
});

let App_activeRoutine = undefined;

let App_pcCyclesCache = undefined;

let App_pcCyclesForTrace = defaultOf();

const App_graphNodes = [];

const App_regCells = new Map([]);

const App_flagCells = initialize(8, (_arg) => defaultOf());

const App_capacity = 1000000;

function App_keyMap(key) {
    switch (key) {
        case "a":
            return singleton([1, 0]);
        case "s":
            return singleton([1, 1]);
        case "d":
            return singleton([1, 2]);
        case "f":
            return singleton([1, 3]);
        case "g":
            return singleton([1, 4]);
        case "q":
            return singleton([2, 0]);
        case "w":
            return singleton([2, 1]);
        case "e":
            return singleton([2, 2]);
        case "r":
            return singleton([2, 3]);
        case "t":
            return singleton([2, 4]);
        case "p":
            return singleton([5, 0]);
        case "o":
            return singleton([5, 1]);
        case "i":
            return singleton([5, 2]);
        case "u":
            return singleton([5, 3]);
        case "y":
            return singleton([5, 4]);
        case "l":
            return singleton([6, 1]);
        case "k":
            return singleton([6, 2]);
        case "j":
            return singleton([6, 3]);
        case "h":
            return singleton([6, 4]);
        case "m":
            return singleton([7, 2]);
        case "n":
            return singleton([7, 3]);
        case "b":
            return singleton([7, 4]);
        case "z":
            return singleton([0, 1]);
        case "x":
            return singleton([0, 2]);
        case "c":
            return singleton([0, 3]);
        case "v":
            return singleton([0, 4]);
        case "1":
            return singleton([3, 0]);
        case "2":
            return singleton([3, 1]);
        case "3":
            return singleton([3, 2]);
        case "4":
            return singleton([3, 3]);
        case "5":
            return singleton([3, 4]);
        case "0":
            return singleton([4, 0]);
        case "9":
            return singleton([4, 1]);
        case "8":
            return singleton([4, 2]);
        case "7":
            return singleton([4, 3]);
        case "6":
            return singleton([4, 4]);
        case " ":
            return singleton([7, 0]);
        case "Enter":
            return singleton([6, 0]);
        case "Shift":
            return singleton([0, 0]);
        case "Control":
            return singleton([7, 1]);
        case "ArrowLeft":
            return ofArray([[0, 0], [3, 4]]);
        case "ArrowRight":
            return ofArray([[0, 0], [4, 2]]);
        case "ArrowUp":
            return ofArray([[0, 0], [4, 3]]);
        case "ArrowDown":
            return ofArray([[0, 0], [4, 4]]);
        default:
            return empty();
    }
}

function App_setKeyFor(key, pressed) {
    if (App_session == null) {
    }
    else {
        const s = App_session;
        iterate((tupledArg) => {
            TraceSession__SetKey_289F56A(s, tupledArg[0], tupledArg[1], pressed);
        }, App_keyMap(key));
    }
}

function App_isCall(b) {
    switch (b) {
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

function App_isRet(b) {
    switch (b) {
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
        default:
            return false;
    }
}

function App_isBranch(b) {
    if (App_isCall(b) ? true : App_isRet(b)) {
        return true;
    }
    else {
        switch (b) {
            case 16:
            case 24:
            case 32:
            case 40:
            case 48:
            case 56:
            case 194:
            case 195:
            case 202:
            case 210:
            case 218:
            case 226:
            case 233:
            case 234:
            case 242:
            case 250:
                return true;
            default:
                return false;
        }
    }
}

function App_ensureBuilt() {
    if (App_session == null) {
    }
    else {
        const recorder = TraceSession__get_Recorder(App_session);
        if (App_builtAtCount !== TraceRecorder__get_EntryCount(recorder)) {
            App_built = TraceRecorder__Build(recorder);
            App_builtAtCount = (TraceRecorder__get_EntryCount(recorder) | 0);
        }
    }
}

function App_currentTrace() {
    if (App_loaded == null) {
        App_ensureBuilt();
        return App_built;
    }
    else {
        return App_loaded;
    }
}

function App_currentEntryCount() {
    const matchValue = App_currentTrace();
    if (matchValue == null) {
        return 0;
    }
    else {
        const t = matchValue;
        return t.Entries.length | 0;
    }
}

function App_clampCursor() {
    const n = App_currentEntryCount() | 0;
    if (n === 0) {
        App_cursor = 0;
    }
    else {
        App_cursor = (max(0, min(App_cursor, n - 1)) | 0);
    }
}

function App_buildTraceNow() {
    App_ensureBuilt();
}

function App_paintGame() {
    if (App_session == null) {
    }
    else {
        const src = TraceSession__get_ScreenBuffer(App_session);
        const buf = App_gameImg.data;
        for (let i = 0; i <= ((320 * 256) - 1); i++) {
            const o = (i * 4) | 0;
            buf[o]=item(o + 2, src);
            buf[(o + 1)]=item(o + 1, src);
            buf[(o + 2)]=item(o, src);
            buf[(o + 3)]=item(o + 3, src);
        }
        App_gameCtx.putImageData(App_gameImg, 0, 0);
    }
}

function App_showErrorDisasm(pc) {
    if (App_session == null) {
    }
    else {
        const mem = TraceSession__get_Memory(App_session);
        const start = (((pc - 64) + 65536) & 65528) | 0;
        const rows = [];
        let addr = start;
        while ((rows.length < 48) && (addr <= (start + 128))) {
            const insn = disasmMemory(mem, addr);
            const hex = join(" ", toList(delay(() => map((i) => {
                const arg = item((addr + i) & 65535, mem);
                return toText(printf("%02X"))(arg);
            }, rangeDouble(0, 1, insn.Length - 1)))));
            let tag;
            if (addr === pc) {
                const arg_1 = addr | 0;
                tag = toText(printf("-> %04X  %-11s  %s   <<< cannot execute here"))(arg_1)(hex)(insn.Text);
            }
            else {
                const arg_4 = addr | 0;
                tag = toText(printf("   %04X  %-11s  %s"))(arg_4)(hex)(insn.Text);
            }
            void (rows.push(tag));
            addr = (((addr + insn.Length) & 65535) | 0);
        }
        Dom_clear(App_disasmBox);
        let enumerator = getEnumerator(rows);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                const r = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const d = Dom_el("div");
                d.textContent = r;
                d.className = "disasm-row";
                Dom_append(App_disasmBox, d);
            }
        }
        finally {
            disposeSafe(enumerator);
        }
        App_cursorLabel.textContent = toText(printf("emulator stopped - static disassembly around 0x%04X"))(pc);
    }
}

function App_renderFrame() {
    let arg_1;
    if (App_session == null) {
    }
    else {
        const s = App_session;
        try {
            const patternInput = TraceSession__RunFrame(s);
            App_paintGame();
            Audio_Play(TraceSession__DrainBeeperSamples_Z524259C1(s, patternInput[0]));
        }
        catch (ex) {
            App_running = false;
            const pc = RegisterFile__Pc(TraceSession__get_Regs(s)) | 0;
            App_statusText.textContent = ((arg_1 = ex.message, toText(printf("emulator error at 0x%04X: %s"))(pc)(arg_1)));
            App_showErrorDisasm(pc);
        }
    }
}

function App_currentSelfModified() {
    if (App_loaded == null) {
        if (App_session == null) {
            return fill(new Array(65536), 0, 65536, false);
        }
        else {
            return TraceRecorder__get_SelfModified(TraceSession__get_Recorder(App_session));
        }
    }
    else {
        return App_loaded.SelfModified;
    }
}

function App_rowFor(t, e, idx, isCurrent) {
    if (e.Length === 0) {
        return [toText(printf("%07d  ----  INT -> %04X   (%d tstates)"))(idx)(e.Target)(e.Cycles), App_red];
    }
    else {
        const bytes = new Uint8Array([e.B0, e.B1, e.B2, e.B3]);
        const insn = disasmBytes(bytes, 0);
        const hex = join(" ", toList(delay(() => map((i) => {
            const arg_3 = item(i, bytes);
            return toText(printf("%02X"))(arg_3);
        }, rangeDouble(0, 1, ~~e.Length - 1)))));
        const tail = (e.Taken === 1) ? toText(printf("   -> %04X"))(e.Target) : (App_isBranch(e.B0) ? "   (not taken)" : "");
        const sm = item(~~e.Pc, App_currentSelfModified()) ? "   [self-mod]" : "";
        const brush = App_isCall(e.B0) ? App_cyan : (App_isRet(e.B0) ? App_yellow : (App_isBranch(e.B0) ? App_green : App_normal));
        return [toText(printf("%07d  %04X  %-11s  %s%s%s"))(idx)(e.Pc)(hex)(insn.Text)(tail)(sm), brush];
    }
}

function App_refreshDisasm() {
    Dom_clear(App_disasmBox);
    const matchValue = App_currentTrace();
    if (matchValue != null) {
        const t = matchValue;
        if (t.Entries.length > 0) {
            const c = max(0, min(App_cursor, t.Entries.length - 1)) | 0;
            for (let j = -20; j <= 20; j++) {
                const idx = (c + j) | 0;
                if ((idx >= 0) && (idx < t.Entries.length)) {
                    const patternInput = App_rowFor(t, item(idx, t.Entries), idx, idx === c);
                    const d = Dom_el("div");
                    d.textContent = patternInput[0];
                    d.className = ("disasm-row" + ((idx === c) ? " current" : ""));
                    d.style.color = patternInput[1];
                    Dom_append(App_disasmBox, d);
                }
            }
        }
    }
}

function App_setReg(name, v) {
    let matchValue;
    let outArg = defaultOf();
    matchValue = [tryGetValue(App_regCells, name, new FSharpRef(() => outArg, (v_1) => {
        outArg = v_1;
    })), outArg];
    if (matchValue[0]) {
        matchValue[1].textContent = toText(printf("%04X"))(v);
    }
}

function App_updateFlags(af) {
    const f = (af & 255) | 0;
    const bits = new Int32Array([128, 64, 32, 16, 8, 4, 2, 1]);
    for (let i = 0; i <= 7; i++) {
        item(i, App_flagCells).textContent = (((f & item(i, bits)) !== 0) ? "1" : "0");
        item(i, App_flagCells).style.color = (((f & item(i, bits)) !== 0) ? App_green : App_dim);
    }
}

function App_refreshRegs() {
    let t;
    const matchValue = App_currentTrace();
    let matchResult, t_1;
    if (matchValue != null) {
        if ((t = matchValue, t.Entries.length > 0)) {
            matchResult = 0;
            t_1 = matchValue;
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
            const e = item(max(0, min(App_cursor, t_1.Entries.length - 1)), t_1.Entries);
            const snapIdx = TraceQuery_nearestSnapshotBefore(t_1.Snapshots, e.Tick) | 0;
            if (snapIdx >= 0) {
                const s = item(snapIdx, t_1.Snapshots);
                App_setReg("AF", ~~s.Af);
                App_setReg("BC", ~~s.Bc);
                App_setReg("DE", ~~s.De);
                App_setReg("HL", ~~s.Hl);
                App_setReg("AF\'", ~~s.Af2);
                App_setReg("BC\'", ~~s.Bc2);
                App_setReg("DE\'", ~~s.De2);
                App_setReg("HL\'", ~~s.Hl2);
                App_setReg("IX", ~~s.Ix);
                App_setReg("IY", ~~s.Iy);
                App_setReg("SP", ~~s.Sp);
                App_setReg("I", ~~s.I);
                App_setReg("R", ~~s.R);
                App_pcCell.textContent = toText(printf("%04X"))(e.Pc);
                App_updateFlags(~~s.Af);
            }
            App_markerPc = (~~e.Pc | 0);
            break;
        }
        case 1: {
            break;
        }
    }
}

function App_riskColor(risk) {
    switch (risk) {
        case "green":
            return App_green;
        case "yellow":
            return App_yellow;
        case "orange":
            return App_orange;
        case "red":
            return App_red;
        default:
            return App_normal;
    }
}

function App_refreshHeatmap() {
    let counts;
    const matchValue = App_currentTrace();
    counts = ((matchValue == null) ? (new Int32Array(65536)) : matchValue.PerPcCount);
    const selfMod = App_currentSelfModified();
    const maxC = max(1, max_1(counts, {
        Compare: (x, y) => (comparePrimitives(x, y) | 0),
    })) | 0;
    const logMax = Math.log10(maxC);
    const buf = App_heatImg.data;
    let i = 0;
    for (let y_1 = 0; y_1 <= 255; y_1++) {
        for (let x_1 = 0; x_1 <= 255; x_1++) {
            const c = item(i, counts) | 0;
            const col = item((c <= 0) ? 0 : min(9, ~~(9 * (Math.log10(c) / logMax))), App_heatLut);
            const p = (i * 4) | 0;
            const r = parse(substring(col, 1, 2), 511, false, 32, 16) | 0;
            const g = parse(substring(col, 3, 2), 511, false, 32, 16) | 0;
            const b = parse(substring(col, 5, 2), 511, false, 32, 16) | 0;
            if (item(i, selfMod)) {
                buf[p]=(~~((b + 192) / 2) & 0xFF);
                buf[(p + 1)]=(~~((g + 48) / 2) & 0xFF);
                buf[(p + 2)]=(~~((r + 240) / 2) & 0xFF);
            }
            else {
                buf[p]=(b & 0xFF);
                buf[(p + 1)]=(g & 0xFF);
                buf[(p + 2)]=(r & 0xFF);
            }
            buf[(p + 3)]=255;
            i = ((i + 1) | 0);
        }
    }
    const pc = App_markerPc | 0;
    const x_2 = (pc & 255) | 0;
    const y_2 = (pc >> 8) | 0;
    for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
            const yy = (y_2 + dy) | 0;
            if ((yy >= 0) && (yy < 256)) {
                const p_1 = (((yy * 256) + ((x_2 + dx) & 255)) * 4) | 0;
                buf[p_1]=255;
                buf[(p_1 + 1)]=255;
                buf[(p_1 + 2)]=255;
                buf[(p_1 + 3)]=255;
            }
        }
    }
    return App_heatCtx.putImageData(App_heatImg, 0, 0);
}

function App_segmentsOf(t) {
    const n = max(1, ~~(t.Entries.length / 512)) | 0;
    const segs = new Int32Array(512);
    const arr = t.Entries;
    for (let idx = 0; idx <= (arr.length - 1); idx++) {
        const e = item(idx, arr);
        setItem(segs, min(511, ~~(~~e.Tick / n)), (item(min(511, ~~(~~e.Tick / n)), segs) + 1) | 0);
    }
    return segs;
}

function App_refreshStrip() {
    const segs = (App_loaded == null) ? ((App_session == null) ? (new Int32Array(0)) : TraceRecorder__get_SegmentCounts(TraceSession__get_Recorder(App_session))) : App_segmentsOf(App_loaded);
    if (segs.length > 0) {
        const maxS = max(1, max_1(segs, {
            Compare: (x, y) => (comparePrimitives(x, y) | 0),
        })) | 0;
        const logMax = Math.log10(maxS + 1);
        const buf = App_stripImg.data;
        for (let sx = 0; sx <= (segs.length - 1); sx++) {
            const v = Math.log10(item(sx, segs) + 1) / logMax;
            const r = (60 + ~~(v * 160)) | 0;
            const g = (120 + ~~(v * 100)) | 0;
            const b = (60 + ~~(v * 60)) | 0;
            for (let sy = 0; sy <= 23; sy++) {
                const p = (((sy * 512) + sx) * 4) | 0;
                buf[p]=(b & 0xFF);
                buf[(p + 1)]=(g & 0xFF);
                buf[(p + 2)]=(r & 0xFF);
                buf[(p + 3)]=255;
            }
        }
        const n = App_currentEntryCount() | 0;
        if (n > 1) {
            const cx = ~~toInt32_unchecked(toInt64_unchecked(op_Division(toInt64_unchecked(op_Multiply(toInt64_unchecked(fromInt32(App_cursor)), 511n)), toInt64_unchecked(fromInt32(n - 1))))) | 0;
            for (let sy_1 = 0; sy_1 <= 23; sy_1++) {
                const p_1 = (((sy_1 * 512) + cx) * 4) | 0;
                buf[p_1]=255;
                buf[(p_1 + 1)]=255;
                buf[(p_1 + 2)]=255;
                buf[(p_1 + 3)]=255;
            }
        }
        App_stripCtx.putImageData(App_stripImg, 0, 0);
    }
}

function App_refreshCursorLabel() {
    let arg_1, arg_2;
    const n = App_currentEntryCount() | 0;
    if (n === 0) {
        App_cursorLabel.textContent = "no trace yet - play the game";
    }
    else {
        const c = max(0, min(App_cursor, n - 1)) | 0;
        App_cursorLabel.textContent = ((arg_1 = ((n - 1) | 0), (arg_2 = (~~item(c, value_17(App_currentTrace()).Entries).Pc | 0), toText(printf("instr %d / %d   pc=%04X"))(c)(arg_1)(arg_2))));
    }
}

function App_refreshAll() {
    App_refreshDisasm();
    App_refreshRegs();
    App_refreshHeatmap();
    App_refreshStrip();
    App_refreshCursorLabel();
}

function App_syncSlider() {
    const n = App_currentEntryCount() | 0;
    App_syncingSlider = true;
    if (n > 1) {
        App_slider.min = 0;
        App_slider.max = (n - 1);
        App_slider.value = max(0, min(App_cursor, n - 1));
        App_slider.disabled = false;
    }
    else {
        App_slider.disabled = true;
        App_slider.value = 0;
    }
    App_syncingSlider = false;
}

function App_getPcCycles() {
    const matchValue = App_currentTrace();
    if (matchValue != null) {
        const t = matchValue;
        if (!(App_pcCyclesForTrace === t)) {
            const cyc = new BigInt64Array(65536);
            const arr = t.Entries;
            for (let idx = 0; idx <= (arr.length - 1); idx++) {
                const e = item(idx, arr);
                if (e.Length !== 0) {
                    setItem(cyc, ~~e.Pc, toInt64_unchecked(op_Addition(item(~~e.Pc, cyc), toInt64_unchecked(fromUInt8(e.Cycles)))));
                }
            }
            App_pcCyclesCache = cyc;
            App_pcCyclesForTrace = t;
        }
        if (App_pcCyclesCache == null) {
            return new BigInt64Array(65536);
        }
        else {
            return App_pcCyclesCache;
        }
    }
    else {
        return new BigInt64Array(65536);
    }
}

function App_generateContract() {
    let arg_4;
    if (App_activeRoutine != null) {
        const r = App_activeRoutine;
        const matchValue = App_currentTrace();
        const App_session_1 = App_session;
        let matchResult, s, t;
        if (matchValue != null) {
            if (App_session_1 != null) {
                matchResult = 0;
                s = App_session_1;
                t = matchValue;
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
                const contract = extract(TraceSession__get_Memory(s), t, r, App_getPcCycles());
                const prompt = generate(contract);
                App_contractText.value = prompt;
                App_contractLabel.textContent = ((arg_4 = (length(contract.WriteRanges) | 0), toText(printf("contract for 0x%04X  span=%04X-%04X  calls=%d  writes=%d"))(r.Entry)(r.SpanLo)(r.SpanHi)(r.CallCount)(arg_4)));
                break;
            }
            case 1: {
                App_contractText.value = "no trace to extract from";
                break;
            }
        }
    }
    else {
        App_contractText.value = "select a mined routine to generate its contract + prompt";
    }
}

function App_maxDepth(columns) {
    let m = 0;
    let enumerator = getEnumerator(columns);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const kv = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            m = (max(m, kv[0]) | 0);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    return m | 0;
}

function App_showRoutineDetail(r) {
    let arg_12, arg_13, arg_14;
    const loops = map_1((tupledArg) => toText(printf("%04X->%04X x%d"))(tupledArg[0])(tupledArg[1])(tupledArg[2]), truncate(5, r.LoopExtents));
    App_routineDetail.textContent = ((arg_12 = join("; ", r.Reasons), (arg_13 = (isEmpty(loops) ? "" : ("\nloops: " + join(", ", loops))), (arg_14 = (length(r.InputSamples) | 0), toText(printf("%04X  (%s)\ncalls=%d  span=%04X-%04X\nincl=%d  excl=%d tstates\nself-modifying=%b  overlapping=%b\n%s%s\ninput samples: %d"))(r.Entry)(r.Risk)(r.CallCount)(r.SpanLo)(r.SpanHi)(r.InclusiveTStates)(r.ExclusiveTStates)(r.SelfModifying)(r.Overlapping)(arg_12)(arg_13)(arg_14)))));
}

function App_refreshRoutines() {
    let arg_7;
    App_syncingRoutines = true;
    Dom_clear(App_routineList);
    const enumerator = getEnumerator(App_minedRoutines);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const r = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const row = Dom_el("div");
            row.textContent = toText(printf("%04X  calls=%5d  span=%04X-%04X  incl=%7d  excl=%7d  %s"))(r.Entry)(r.CallCount)(r.SpanLo)(r.SpanHi)(r.InclusiveTStates)(r.ExclusiveTStates)(r.Risk);
            row.className = ("rout-row" + (FSharpSet__Contains(App_selectedEntries, r.Entry) ? " selected" : ""));
            row.style.color = App_riskColor(r.Risk);
            row.addEventListener("click", ((_arg) => {
                App_selectRoutine(r.Entry, true);
            }));
            Dom_append(App_routineList, row);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    App_routineCountLabel.textContent = ((arg_7 = (length(App_minedRoutines) | 0), toText(printf("%d routines"))(arg_7)));
    App_syncingRoutines = false;
}

function App_refreshTray() {
    let arg_2;
    Dom_clear(App_tray);
    const enumerator = getEnumerator(toList_1(App_selectedEntries));
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const entry = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]() | 0;
            const matchValue = tryFind((r) => (r.Entry === entry), App_minedRoutines);
            if (matchValue == null) {
            }
            else {
                const r_1 = matchValue;
                const chip = Dom_el("button");
                chip.textContent = toText(printf("%04X x%d"))(r_1.Entry)(r_1.CallCount);
                chip.className = "chip";
                chip.addEventListener("click", ((_arg) => {
                    App_selectRoutine(r_1.Entry, false);
                }));
                Dom_append(App_tray, chip);
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    App_trayCountLabel.textContent = ((arg_2 = (FSharpSet__get_Count(App_selectedEntries) | 0), toText(printf("%d in lift queue"))(arg_2)));
}

function App_drawGraph() {
    Dom_clear(App_graphNodes);
    if (isEmpty(App_minedRoutines)) {
        App_graphCanvas.width = 400;
        App_graphCanvas.height = 60;
        App_graphCtx.fillStyle = App_dim;
        App_graphCtx.font = ("12px " + App_mono);
        App_graphCtx.textAlign = "left";
        App_graphCtx.fillText("mine a trace first (Functions tab)", 8, 30);
    }
    else {
        const depth = new Map([]);
        depth.set(-1, 0);
        const enumerator = getEnumerator(App_minedRoutines);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                const r = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
                depth.set(r.Entry, 100);
            }
        }
        finally {
            disposeSafe(enumerator);
        }
        let changed = true;
        let guard = 0;
        while (changed && (guard < 20)) {
            changed = false;
            guard = ((guard + 1) | 0);
            const enumerator_1 = getEnumerator(App_minedEdges);
            try {
                while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
                    const e = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]();
                    let dFrom;
                    let matchValue;
                    let outArg = 0;
                    matchValue = [tryGetValue(depth, e.Caller, new FSharpRef(() => (outArg | 0), (v) => {
                        outArg = (v | 0);
                    })), outArg];
                    dFrom = (matchValue[0] ? matchValue[1] : 100);
                    if ((dFrom < 100) && (getItemFromDict(depth, e.Callee) > (dFrom + 1))) {
                        depth.set(e.Callee, dFrom + 1);
                        changed = true;
                    }
                }
            }
            finally {
                disposeSafe(enumerator_1);
            }
        }
        const columns = new Map([]);
        const enumerator_2 = getEnumerator(App_minedRoutines);
        try {
            while (enumerator_2["System.Collections.IEnumerator.MoveNext"]()) {
                const r_1 = enumerator_2["System.Collections.Generic.IEnumerator`1.get_Current"]();
                let d;
                let matchValue_1;
                let outArg_1 = 0;
                matchValue_1 = [tryGetValue(depth, r_1.Entry, new FSharpRef(() => (outArg_1 | 0), (v_2) => {
                    outArg_1 = (v_2 | 0);
                })), outArg_1];
                d = (matchValue_1[0] ? min(matchValue_1[1], 9) : 1);
                let matchValue_2;
                let outArg_2 = defaultOf();
                matchValue_2 = [tryGetValue(columns, d, new FSharpRef(() => outArg_2, (v_4) => {
                    outArg_2 = v_4;
                })), outArg_2];
                if (matchValue_2[0]) {
                    void (matchValue_2[1].push(r_1.Entry));
                }
                else {
                    const list_1 = [];
                    void (list_1.push(r_1.Entry));
                    columns.set(d, list_1);
                }
            }
        }
        finally {
            disposeSafe(enumerator_2);
        }
        const pos = new Map([]);
        let enumerator_3 = getEnumerator(columns);
        try {
            while (enumerator_3["System.Collections.IEnumerator.MoveNext"]()) {
                const kv = enumerator_3["System.Collections.Generic.IEnumerator`1.get_Current"]();
                iterateIndexed((row, entry) => {
                    pos.set(entry, [20 + (kv[0] * 150), 20 + (row * 44)]);
                }, kv[1]);
            }
        }
        finally {
            disposeSafe(enumerator_3);
        }
        pos.set(-1, [20, 20]);
        App_graphCanvas.width = (40 + ((App_maxDepth(columns) + 1) * 150));
        App_graphCanvas.height = (40 + (fold((e_1, e_2) => (max(e_1, e_2) | 0), 0, map((l) => (l.length | 0), columns.values())) * 44));
        App_graphCtx.strokeStyle = App_dim;
        App_graphCtx.fillStyle = App_dim;
        App_graphCtx.font = ("10px " + App_mono);
        App_graphCtx.textAlign = "center";
        const enumerator_4 = getEnumerator(App_minedEdges);
        try {
            while (enumerator_4["System.Collections.IEnumerator.MoveNext"]()) {
                const e_3 = enumerator_4["System.Collections.Generic.IEnumerator`1.get_Current"]();
                let matchValue_3;
                let outArg_3 = defaultOf();
                matchValue_3 = [tryGetValue(pos, e_3.Caller, new FSharpRef(() => outArg_3, (v_5) => {
                    outArg_3 = v_5;
                })), outArg_3];
                let matchValue_4;
                let outArg_4 = defaultOf();
                matchValue_4 = [tryGetValue(pos, e_3.Callee, new FSharpRef(() => outArg_4, (v_6) => {
                    outArg_4 = v_6;
                })), outArg_4];
                let matchResult;
                if (matchValue_3[0]) {
                    if (matchValue_4[0]) {
                        matchResult = 0;
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
                        const y2 = matchValue_4[1][1];
                        const y1 = matchValue_3[1][1];
                        const x2 = matchValue_4[1][0];
                        const x1 = matchValue_3[1][0];
                        App_graphCtx.beginPath();
                        App_graphCtx.moveTo((x1 + 104), (y1 + 14));
                        App_graphCtx.lineTo(x2, (y2 + 14));
                        App_graphCtx.stroke();
                        App_graphCtx.fillText(int32ToString(e_3.Count), ((x1 + x2) / 2), ((y1 + y2) / 2));
                        break;
                    }
                }
            }
        }
        finally {
            disposeSafe(enumerator_4);
        }
        const nodeBox = (entry_1, title, sub, color) => {
            const patternInput = getItemFromDict(pos, entry_1);
            const y = patternInput[1];
            const x = patternInput[0];
            App_graphCtx.fillStyle = (FSharpSet__Contains(App_selectedEntries, entry_1) ? App_hiBg : App_panel);
            App_graphCtx.strokeStyle = color;
            App_graphCtx.lineWidth = 2;
            App_graphCtx.fillRect(x, y, 104, 28);
            App_graphCtx.strokeRect(x, y, 104, 28);
            App_graphCtx.fillStyle = App_normal;
            App_graphCtx.font = ("11px " + App_mono);
            App_graphCtx.textAlign = "center";
            App_graphCtx.fillText(title, (x + 52), (y + 12));
            App_graphCtx.fillStyle = App_dim;
            App_graphCtx.font = ("9px " + App_mono);
            App_graphCtx.fillText(sub, (x + 52), (y + 24));
            void (App_graphNodes.push([entry_1, x, y]));
            App_graphCtx.lineWidth = 1;
        };
        nodeBox(-1, "TOP", "", App_normal);
        const enumerator_5 = getEnumerator(App_minedRoutines);
        try {
            while (enumerator_5["System.Collections.IEnumerator.MoveNext"]()) {
                const r_2 = enumerator_5["System.Collections.Generic.IEnumerator`1.get_Current"]();
                nodeBox(r_2.Entry, toText(printf("%04X"))(r_2.Entry), toText(printf("x%d"))(r_2.CallCount), App_riskColor(r_2.Risk));
            }
        }
        finally {
            disposeSafe(enumerator_5);
        }
    }
}

function App_selectRoutine(entry, jump) {
    let t;
    const wasSelected = FSharpSet__Contains(App_selectedEntries, entry);
    App_selectedEntries = (wasSelected ? remove(entry, App_selectedEntries) : add(entry, App_selectedEntries));
    const matchValue = tryFind((r) => (r.Entry === entry), App_minedRoutines);
    if (matchValue == null) {
    }
    else {
        const r_1 = matchValue;
        App_activeRoutine = r_1;
        App_showRoutineDetail(r_1);
    }
    App_refreshRoutines();
    App_refreshTray();
    App_drawGraph();
    App_generateContract();
    if (!wasSelected && jump) {
        const matchValue_1 = App_currentTrace();
        let matchResult, t_1;
        if (matchValue_1 != null) {
            if ((t = matchValue_1, (entry < t.FirstIndexAtPc.length) && (item(entry, t.FirstIndexAtPc) >= 0))) {
                matchResult = 0;
                t_1 = matchValue_1;
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
                App_cursor = (item(entry, t_1.FirstIndexAtPc) | 0);
                App_refreshAll();
                App_syncSlider();
                break;
            }
            case 1: {
                break;
            }
        }
    }
}

function App_mineNow() {
    let arg, arg_1, t;
    App_buildTraceNow();
    const matchValue = App_currentTrace();
    let matchResult, t_1;
    if (matchValue != null) {
        if ((t = matchValue, t.Entries.length > 0)) {
            matchResult = 0;
            t_1 = matchValue;
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
            const patternInput = mine(t_1);
            const routines = patternInput[0];
            const edges = patternInput[1];
            App_minedRoutines = routines;
            App_minedEdges = edges;
            App_selectedEntries = empty_1({
                Compare: (x, y) => (comparePrimitives(x, y) | 0),
            });
            App_refreshRoutines();
            App_refreshTray();
            App_drawGraph();
            App_statusText.textContent = ((arg = (length(routines) | 0), (arg_1 = (length(edges) | 0), toText(printf("mined %d routines, %d call edges (self-mod addresses: %d)"))(arg)(arg_1)(t_1.SelfModCount))));
            break;
        }
        case 1: {
            App_statusText.textContent = "no trace to mine - play the game first";
            break;
        }
    }
}

function App_pauseGame() {
    App_running = false;
    App_cinemaPlaying = false;
    App_playBtn.textContent = "Play";
    App_buildTraceNow();
    App_clampCursor();
    App_syncSlider();
    App_refreshAll();
}

function App_seek(delta) {
    App_cinemaPlaying = false;
    App_playBtn.textContent = "Play";
    App_buildTraceNow();
    App_clampCursor();
    App_cursor = (max(0, min(App_cursor + delta, max(0, App_currentEntryCount() - 1))) | 0);
    App_refreshAll();
    App_syncSlider();
}

function App_stepBack100() {
    App_seek(-100);
}

function App_stepBack10() {
    App_seek(-10);
}

function App_stepBack1() {
    App_seek(-1);
}

function App_stepFwd1() {
    App_seek(1);
}

function App_stepFwd10() {
    App_seek(10);
}

function App_stepFwd100() {
    App_seek(100);
}

function App_toggleCinema() {
    App_buildTraceNow();
    if (App_cinemaPlaying) {
        App_cinemaPlaying = false;
        App_playBtn.textContent = "Play";
    }
    else if (App_currentEntryCount() > 0) {
        App_cinemaPlaying = true;
        App_playBtn.textContent = "Pause";
    }
}

function App_saveTrace() {
    let arg;
    try {
        let t_2;
        if (App_loaded == null) {
            App_buildTraceNow();
            if (App_built == null) {
                throw new Exception("no trace recorded yet");
            }
            else {
                t_2 = App_built;
            }
        }
        else {
            t_2 = App_loaded;
        }
        if (t_2.Entries.length === 0) {
            throw new Exception("trace is empty");
        }
        const bytes = encode(t_2);
        const blob = new Blob([new Uint8Array(bytes)], { type: 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const a = Dom_el("a");
        a.href = url;
        a.download = "trace.jpt";
        Dom_append(Dom_document.body, a);
        a.click();
        URL.revokeObjectURL(url);
        App_statusText.textContent = ((arg = (t_2.Entries.length | 0), toText(printf("saved %d instructions to trace.jpt"))(arg)));
    }
    catch (ex) {
        App_statusText.textContent = ("save failed: " + ex.message);
    }
}

function App_loadTrace() {
    const input = Dom_el("input");
    input.type = "file";
    input.accept = ".jpt";
    input.addEventListener("change", ((_arg) => {
        const f = input.files[0];
        if (!Operators_IsNull(f)) {
            const reader = new FileReader();
            reader.onload = (() => {
                let arg, arg_1;
                const buf = reader.result;
                const bytes = Array.from(new Uint8Array(buf));
                try {
                    const t = decode(bytes);
                    App_loaded = t;
                    App_cursor = 0;
                    App_markerPc = (~~item(0, t.Entries).Pc | 0);
                    App_syncSlider();
                    App_refreshAll();
                    const patternInput = mine(t);
                    const routines = patternInput[0];
                    App_minedRoutines = routines;
                    App_minedEdges = patternInput[1];
                    App_selectedEntries = empty_1({
                        Compare: (x, y) => (comparePrimitives(x, y) | 0),
                    });
                    App_refreshRoutines();
                    App_refreshTray();
                    App_drawGraph();
                    App_statusText.textContent = ((arg = (t.Entries.length | 0), (arg_1 = (length(routines) | 0), toText(printf("loaded %d instructions; mined %d routines"))(arg)(arg_1))));
                }
                catch (ex) {
                    App_statusText.textContent = ("load failed: " + ex.message);
                }
            });
            reader.readAsArrayBuffer(f);
        }
    }));
    input.click();
}

function App_startGame() {
    let arg, arg_1, arg_2;
    if (App_session == null) {
    }
    else {
        const s = App_session;
        App_chkRec.checked = TraceRecorder__get_RecordEnabled(TraceSession__get_Recorder(s));
        App_running = true;
        App_statusText.textContent = ((arg = (TraceSession__get_WarmStart(s) ? "cached entry state" : "booted to game entry"), (arg_1 = (TraceRecorder__get_RecordEnabled(TraceSession__get_Recorder(s)) ? "ON" : "OFF"), (arg_2 = (TraceSession__get_ReplayEventCount(s) | 0), toText(printf("emulator ready (%s) - frame 0, recording %s, saved input %d events"))(arg)(arg_1)(arg_2)))));
    }
}

export function App_start() {
    AssetProvider((key) => ((key === "rom") ? decode_1(RomB64) : ((key === "tzx") ? decode_1(TzxB64) : toFail(printf("unknown asset key %s"))(key))));
    const matchValue = EntryCache_tryLoad("rom", "tzx");
    if (matchValue == null) {
        const mem = decode_1(WarmMemoryB64);
        if (mem.length === 65536) {
            EntryCache_save("rom", "tzx", mem, WarmState);
        }
    }
    App_statusText = Dom_byId("status");
    App_disasmBox = Dom_byId("disasmBox");
    App_cursorLabel = Dom_byId("cursorLabel");
    App_slider = Dom_byId("slider");
    App_pcCell = Dom_byId("pcCell");
    App_routineList = Dom_byId("routineList");
    App_routineCountLabel = Dom_byId("routineCount");
    App_routineDetail = Dom_byId("routineDetail");
    App_tray = Dom_byId("tray");
    App_trayCountLabel = Dom_byId("trayCount");
    App_contractText = Dom_byId("contractText");
    App_contractLabel = Dom_byId("contractLabel");
    App_theaterText = Dom_byId("theaterText");
    App_pasteBox = Dom_byId("pasteBox");
    App_playBtn = Dom_byId("btnPlay");
    App_chkRec = Dom_byId("chkRec");
    App_chkMute = Dom_byId("chkMute");
    const gameCanvas = Dom_byId("canvasScreen");
    App_gameCtx = (gameCanvas.getContext("2d"));
    App_gameImg = (new ImageData(320, 256));
    const heatCanvas = Dom_byId("canvasHeat");
    App_heatCtx = (heatCanvas.getContext("2d"));
    App_heatImg = (new ImageData(256, 256));
    const stripCanvas = Dom_byId("canvasStrip");
    App_stripCtx = (stripCanvas.getContext("2d"));
    App_stripImg = (new ImageData(512, 24));
    App_graphCanvas = Dom_byId("canvasGraph");
    App_graphCtx = (App_graphCanvas.getContext("2d"));
    const regsBox = Dom_byId("regsBox");
    App_regCells.clear();
    const enumerator = getEnumerator(["AF", "BC", "DE", "HL", "AF\'", "BC\'", "DE\'", "HL\'", "IX", "IY", "SP", "I", "R"]);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const name = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const row = Dom_el("div");
            row.className = "reg-row";
            const nameSpan = Dom_el("span");
            nameSpan.textContent = name;
            nameSpan.className = "reg-name";
            const valSpan = Dom_el("span");
            valSpan.textContent = "----";
            valSpan.className = "reg-val";
            Dom_append(row, nameSpan);
            Dom_append(row, valSpan);
            Dom_append(regsBox, row);
            App_regCells.set(name, valSpan);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const pcRow = Dom_el("div");
    pcRow.className = "reg-row";
    const pcName = Dom_el("span");
    pcName.textContent = "PC";
    pcName.className = "reg-name";
    const pcVal = Dom_el("span");
    pcVal.textContent = "----";
    pcVal.className = "reg-val";
    Dom_append(pcRow, pcName);
    Dom_append(pcRow, pcVal);
    Dom_append(regsBox, pcRow);
    App_regCells.set("PC", pcVal);
    App_pcCell = pcVal;
    const flagsBox = Dom_byId("flagsBox");
    Dom_clear(flagsBox);
    const flagNames = ["S", "Z", "5", "H", "3", "P", "N", "C"];
    for (let i = 0; i <= 7; i++) {
        const row_1 = Dom_el("div");
        row_1.className = "flag-row";
        const nameSpan_1 = Dom_el("span");
        nameSpan_1.textContent = item(i, flagNames);
        nameSpan_1.className = "reg-name";
        const valSpan_1 = Dom_el("span");
        valSpan_1.textContent = "0";
        valSpan_1.className = "reg-val";
        Dom_append(row_1, nameSpan_1);
        Dom_append(row_1, valSpan_1);
        Dom_append(flagsBox, row_1);
        setItem(App_flagCells, i, valSpan_1);
    }
    App_frameTimerId = Dom_setInterval(() => {
        if (App_running) {
            App_renderFrame();
        }
    }, 20);
    App_cinemaTimerId = Dom_setInterval(() => {
        if (App_cinemaPlaying) {
            const n = App_currentEntryCount() | 0;
            if (n > 0) {
                App_cursor = (min(n - 1, App_cursor + App_cinemaSpeed) | 0);
                App_refreshDisasm();
                App_refreshRegs();
                App_refreshHeatmap();
                App_refreshStrip();
                App_refreshCursorLabel();
                App_syncingSlider = true;
                App_slider.value = App_cursor;
                App_syncingSlider = false;
            }
            else {
                App_cinemaPlaying = false;
            }
        }
    }, 30);
    Dom_setInterval(() => {
        let arg_1, arg_2, arg_3, arg_4, arg_5, array_1, array, arg_6, arg_7;
        if (App_session != null) {
            const s = App_session;
            if (App_running) {
                App_statusText.textContent = ((arg_1 = (TraceSession__get_Frame(s) | 0), (arg_2 = (toFloat64(TraceSession__get_CycleCount(s)) / 1000000), (arg_3 = (TraceRecorder__get_EntryCount(TraceSession__get_Recorder(s)) | 0), (arg_4 = (TraceRecorder__get_Capacity(TraceSession__get_Recorder(s)) | 0), (arg_5 = (((array_1 = ((array = TraceRecorder__get_PerPcCount(TraceSession__get_Recorder(s)), array.filter((c) => (c > 0)))), array_1.length)) | 0), (arg_6 = (TraceRecorder__get_SelfModCount(TraceSession__get_Recorder(s)) | 0), (arg_7 = (TraceRecorder__get_RecordEnabled(TraceSession__get_Recorder(s)) ? "ON" : "OFF"), toText(printf("frame=%d  tick=%.2fM  instr=%d/%d  distinct-pc=%d  selfmod=%d  rec=%s"))(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6)(arg_7)))))))));
            }
            App_refreshHeatmap();
            App_refreshStrip();
            if (App_running) {
                App_syncingSlider = true;
                const n_1 = TraceRecorder__get_EntryCount(TraceSession__get_Recorder(s)) | 0;
                if (n_1 > 1) {
                    App_slider.max = (n_1 - 1);
                    App_slider.disabled = false;
                }
                App_syncingSlider = false;
            }
        }
    }, 100);
    Dom_byId("btnRun").addEventListener("click", ((_arg) => {
        App_pauseGame();
        App_running = true;
    }));
    Dom_byId("btnPause").addEventListener("click", ((_arg_1) => {
        App_pauseGame();
    }));
    Dom_byId("btnStepFrame").addEventListener("click", ((_arg_2) => {
        App_pauseGame();
        App_renderFrame();
    }));
    Dom_byId("btnSaveTrace").addEventListener("click", ((_arg_3) => {
        App_saveTrace();
    }));
    Dom_byId("btnLoadTrace").addEventListener("click", ((_arg_4) => {
        App_loadTrace();
    }));
    App_chkRec.addEventListener("change", ((_arg_5) => {
        if (App_session == null) {
        }
        else {
            TraceRecorder__set_RecordEnabled_Z1FBCCD16(TraceSession__get_Recorder(App_session), App_chkRec.checked);
        }
    }));
    App_chkMute.addEventListener("change", ((_arg_6) => {
        Audio_SetEnabled(!App_chkMute.checked);
    }));
    Dom_byId("btnB100").addEventListener("click", ((_arg_7) => {
        App_stepBack100();
    }));
    Dom_byId("btnB10").addEventListener("click", ((_arg_8) => {
        App_stepBack10();
    }));
    Dom_byId("btnB1").addEventListener("click", ((_arg_9) => {
        App_stepBack1();
    }));
    Dom_byId("btnF1").addEventListener("click", ((_arg_10) => {
        App_stepFwd1();
    }));
    Dom_byId("btnF10").addEventListener("click", ((_arg_11) => {
        App_stepFwd10();
    }));
    Dom_byId("btnF100").addEventListener("click", ((_arg_12) => {
        App_stepFwd100();
    }));
    App_playBtn.addEventListener("click", ((_arg_13) => {
        App_toggleCinema();
    }));
    const selSpeed = Dom_byId("selSpeed");
    selSpeed.addEventListener("change", ((_arg_14) => {
        App_cinemaSpeed = (parse(toString(selSpeed.value), 511, false, 32) | 0);
    }));
    App_slider.addEventListener("input", ((_arg_15) => {
        if (!App_syncingSlider && (App_currentEntryCount() > 0)) {
            App_cursor = (parse(toString(App_slider.value), 511, false, 32) | 0);
            App_refreshDisasm();
            App_refreshRegs();
            App_refreshHeatmap();
            App_refreshStrip();
            App_refreshCursorLabel();
        }
    }));
    heatCanvas.addEventListener("click", ((e) => {
        let arg_9, t;
        const matchValue_1 = App_currentTrace();
        let matchResult, t_1;
        if (matchValue_1 != null) {
            if ((t = matchValue_1, t.Entries.length > 0)) {
                matchResult = 0;
                t_1 = matchValue_1;
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
                const x = e.offsetX | 0;
                const y = e.offsetY | 0;
                if ((((x >= 0) && (x < 256)) && (y >= 0)) && (y < 256)) {
                    const pc = ((y << 8) | x) | 0;
                    const idx = ((pc < t_1.FirstIndexAtPc.length) ? item(pc, t_1.FirstIndexAtPc) : -1) | 0;
                    if (idx >= 0) {
                        App_cursor = (idx | 0);
                        App_refreshAll();
                        App_syncSlider();
                        App_statusText.textContent = ((arg_9 = (((pc < t_1.PerPcCount.length) ? item(pc, t_1.PerPcCount) : 0) | 0), toText(printf("jumped to 0x%04X (first of %d executions)"))(pc)(arg_9)));
                    }
                }
                break;
            }
            case 1: {
                break;
            }
        }
    }));
    heatCanvas.addEventListener("mousemove", ((e_1) => {
        let t_2;
        const x_1 = e_1.offsetX | 0;
        const y_1 = e_1.offsetY | 0;
        if ((((x_1 >= 0) && (x_1 < 256)) && (y_1 >= 0)) && (y_1 < 256)) {
            const pc_1 = ((y_1 << 8) | x_1) | 0;
            let count;
            const matchValue_2 = App_currentTrace();
            let matchResult_1, t_3;
            if (matchValue_2 != null) {
                if ((t_2 = matchValue_2, pc_1 < t_2.PerPcCount.length)) {
                    matchResult_1 = 0;
                    t_3 = matchValue_2;
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
                    count = item(pc_1, t_3.PerPcCount);
                    break;
                }
                default:
                    count = 0;
            }
            heatCanvas.title = toText(printf("0x%04X  (%d executions)"))(pc_1)(count);
        }
    }));
    Dom_byId("btnMine").addEventListener("click", ((_arg_16) => {
        App_mineNow();
    }));
    App_graphCanvas.addEventListener("click", ((e_2) => {
        const x_2 = e_2.offsetX;
        const y_2 = e_2.offsetY;
        let hit = -1;
        let enumerator_1 = getEnumerator(App_graphNodes);
        try {
            while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
                const forLoopVar = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const ny = forLoopVar[2];
                const nx = forLoopVar[1];
                if (((((hit < 0) && (x_2 >= nx)) && (x_2 <= (nx + 104))) && (y_2 >= ny)) && (y_2 <= (ny + 28))) {
                    hit = (forLoopVar[0] | 0);
                }
            }
        }
        finally {
            disposeSafe(enumerator_1);
        }
        if (hit >= 0) {
            App_selectRoutine(hit, true);
        }
    }));
    App_graphCanvas.addEventListener("mousemove", ((e_3) => {
        let arg_12;
        const x_3 = e_3.offsetX;
        const y_3 = e_3.offsetY;
        let hit_1 = -1;
        let enumerator_2 = getEnumerator(App_graphNodes);
        try {
            while (enumerator_2["System.Collections.IEnumerator.MoveNext"]()) {
                const forLoopVar_1 = enumerator_2["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const ny_1 = forLoopVar_1[2];
                const nx_1 = forLoopVar_1[1];
                if (((((hit_1 < 0) && (x_3 >= nx_1)) && (x_3 <= (nx_1 + 104))) && (y_3 >= ny_1)) && (y_3 <= (ny_1 + 28))) {
                    hit_1 = (forLoopVar_1[0] | 0);
                }
            }
        }
        finally {
            disposeSafe(enumerator_2);
        }
        if (hit_1 >= 0) {
            App_graphCanvas.title = ((arg_12 = (hit_1 | 0), toText(printf("0x%04X"))(arg_12)));
        }
        else {
            App_graphCanvas.title = "";
        }
    }));
    Dom_byId("btnSaveMd").addEventListener("click", ((_arg_17) => {
        if (toString(App_contractText.value).length > 0) {
            const blob = new Blob([App_contractText.value], { type: 'text/markdown' });
            const url = URL.createObjectURL(blob);
            const a = Dom_el("a");
            a.href = url;
            a.download = "prompt.md";
            Dom_append(Dom_document.body, a);
            a.click();
            URL.revokeObjectURL(url);
            App_statusText.textContent = "prompt saved to prompt.md";
        }
        else {
            App_statusText.textContent = "select a routine first";
        }
    }));
    Dom_byId("btnCopy").addEventListener("click", ((_arg_18) => {
        if (toString(App_contractText.value).length > 0) {
            navigator.clipboard.writeText(App_contractText.value);
            App_statusText.textContent = "prompt copied to clipboard";
        }
        else {
            App_statusText.textContent = "select a routine first";
        }
    }));
    Dom_byId("btnStage").addEventListener("click", ((_arg_19) => {
        if (App_activeRoutine == null) {
            App_statusText.textContent = "select a routine first";
        }
        else {
            const r = App_activeRoutine;
            if (toString(App_pasteBox.value).trim().length > 0) {
                const content = toText(printf("// Staged lift for 0x%04X (span 0x%04X-0x%04X)\n// Add this file to Jetpac3.Core in Visual Studio, register the routine in\n// LiftedRoutines.registry, rebuild, then validate from the Theater tab.\n\n"))(r.Entry)(r.SpanLo)(r.SpanHi) + toString(App_pasteBox.value);
                const blob_1 = new Blob([content], { type: 'text/plain' });
                const url_1 = URL.createObjectURL(blob_1);
                const a_1 = Dom_el("a");
                a_1.href = url_1;
                a_1.download = toText(printf("fn%04X.fs"))(r.Entry);
                Dom_append(Dom_document.body, a_1);
                a_1.click();
                URL.revokeObjectURL(url_1);
                App_statusText.textContent = toText(printf("staged fn%04X.fs downloaded"))(r.Entry);
            }
            else {
                App_statusText.textContent = "paste the model\'s F# first";
            }
        }
    }));
    Dom_byId("btnValidate").addEventListener("click", ((_arg_20) => {
        let matchValue_4;
        const App_activeRoutine_1 = App_activeRoutine;
        const App_session_1 = App_session;
        if (App_activeRoutine_1 == null) {
            App_theaterText.textContent = "select a mined routine to validate";
        }
        else if (App_session_1 != null) {
            const r_1 = App_activeRoutine_1;
            if (!((matchValue_4 = LiftedRoutines_registryHook(r_1.Entry), !(matchValue_4 == null)))) {
                App_theaterText.textContent = toText(printf("0x%04X is not covered by the lifted registry yet.\n\nStage the pasted F# (below), add it to Jetpac3.Core in Visual Studio, rebuild, then validate again."))(r_1.Entry);
            }
            else {
                const script = defaultSession(300);
                App_theaterText.textContent = toText(printf("validating 0x%04X over %d frames..."))(r_1.Entry)(300);
                try {
                    const rep = run("rom", "tzx", 300, script, LiftedRoutines_registryHook, createCancellationToken());
                    if (rep.Passed) {
                        App_theaterText.textContent = toText(printf("VALIDATION PASSED: %d frames, 0 diffs (lifted 0x%04X == oracle)"))(rep.Frames)(r_1.Entry);
                    }
                    else {
                        const matchValue_5 = rep.FirstDivergence;
                        if (matchValue_5 == null) {
                            App_theaterText.textContent = "VALIDATION FAILED: no divergence details";
                        }
                        else {
                            const d = matchValue_5;
                            App_theaterText.textContent = toText(printf("VALIDATION FAILED at frame %d (%s)\n\nport   executed %04X: %s\noracle executed %04X: %s\n\nport   regs: %s\noracle regs: %s"))(d.Frame)(d.Kind)(d.PortPc)(d.PortExecuted)(d.OraclePc)(d.OracleExecuted)(d.PortRegs)(d.OracleRegs);
                        }
                    }
                }
                catch (ex) {
                    App_theaterText.textContent = ("validation crashed: " + ex.message);
                }
            }
        }
        else {
            App_theaterText.textContent = "emulator not ready";
        }
    }));
    Dom_byId("btnClearTray").addEventListener("click", ((_arg_21) => {
        App_selectedEntries = empty_1({
            Compare: (x_4, y_4) => (comparePrimitives(x_4, y_4) | 0),
        });
        App_refreshRoutines();
        App_refreshTray();
        App_drawGraph();
    }));
    Dom_window.addEventListener("keydown", ((e_4) => {
        const key_1 = e_4.key;
        App_setKeyFor(key_1, true);
        if ((key_1 === " ") ? true : key_1.startsWith("Arrow")) {
            e_4.preventDefault();
        }
    }));
    Dom_window.addEventListener("keyup", ((e_5) => {
        App_setKeyFor(toString(e_5.key), false);
    }));
    App_statusText.textContent = "booting emulator to game entry...";
    return Dom_setTimeout(() => {
        try {
            App_session = TraceSession_$ctor_28C3603C(decode_1(RomB64), decode_1(TzxB64), App_capacity);
            App_startGame();
            App_refreshAll();
            App_syncSlider();
        }
        catch (ex_1) {
            App_statusText.textContent = ("boot failed: " + ex_1.message);
        }
    }, 30);
}

