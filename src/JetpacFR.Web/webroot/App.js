
import { equals, round, Exception, int32ToString, clear, disposeSafe, getEnumerator, comparePrimitives, defaultOf, createAtom } from "./fable_modules/fable-library-js.5.17.0/Util.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.17.0/FSharp.Core.js";
import { tryFind as tryFind_1, fold as fold_1, max as max_1, map as map_1, fill, initialize, setItem, item as item_1 } from "./fable_modules/fable-library-js.5.17.0/Array.js";
import { tryFind, isEmpty, truncate, map as map_2, length as length_1, iterate, ofArray, singleton, empty } from "./fable_modules/fable-library-js.5.17.0/List.js";
import { add, remove, FSharpSet__get_Count, toList as toList_1, FSharpSet__Contains, empty as empty_1 } from "./fable_modules/fable-library-js.5.17.0/Set.js";
import { TraceSession_$ctor_28C3603C, TraceSession__get_CycleCount, TraceSession__get_Frame, TraceSession__FlushReplayCache, EntryCache_save, EntryCache_tryLoad, TraceSession__get_ReplayEventCount, TraceSession__get_WarmStart, TraceSession__DrainBeeperSamples_Z524259C1, TraceSession__RunFrame, TraceSession__get_Regs, TraceSession__get_Memory, TraceSession__get_ScreenBuffer, TraceSession__get_Recorder, TraceSession__SetKey_289F56A } from "./SessionWeb.js";
import { TraceRecorder__set_RecordEnabled_Z1FBCCD16, TraceRecorder__get_SelfModCount, TraceRecorder__get_PerPcCount, TraceRecorder__get_Capacity, TraceRecorder__get_RecordEnabled, TraceRecorder__get_SegmentCounts, TraceQuery_nearestSnapshotBefore, TraceRecorder__get_SelfModified, TraceRecorder__Build, TraceRecorder__get_EntryCount } from "./TraceTypesWeb.js";
import { min, max } from "./fable_modules/fable-library-js.5.17.0/Double.js";
import { disasmBytes, disasmMemory } from "./JetpacFR.Core/Disasm.js";
import { toFail, substring, printf, toText, join } from "./fable_modules/fable-library-js.5.17.0/String.js";
import { fold, iterateIndexed, map, delay, toList } from "./fable_modules/fable-library-js.5.17.0/Seq.js";
import { rangeDouble } from "./fable_modules/fable-library-js.5.17.0/Range.js";
import { RegisterFile__Pc } from "./Jetpac2.Core/Machine.js";
import { getItemFromDict, tryGetValue } from "./fable_modules/fable-library-js.5.17.0/MapUtil.js";
import { toString, FSharpRef } from "./fable_modules/fable-library-js.5.17.0/Types.js";
import { parse } from "./fable_modules/fable-library-js.5.17.0/Int32.js";
import { toFloat64, fromUInt8, op_Addition, fromInt32, op_Multiply, op_Division, toInt64_unchecked, toInt32_unchecked } from "./fable_modules/fable-library-js.5.17.0/BigInt.js";
import { defaultArg, value as value_36 } from "./fable_modules/fable-library-js.5.17.0/Option.js";
import { extract } from "./JetpacFR.Core/Contract.js";
import { generate } from "./JetpacFR.Core/Prompt.js";
import { mine } from "./JetpacFR.Core/Miner.js";
import { decode, encode } from "./TraceCodecWeb.js";
import { AssetProvider } from "./BootWeb.js";
import { WarmState, WarmMemoryB64, TzxB64, RomB64, decode as decode_1 } from "./Embedded.js";
import { LiftedRoutines_registryHook } from "./Jetpac3.Core/LiftedRoutines.js";
import { defaultSession } from "./ScriptWeb.js";
import { run } from "./JetpacFR.Core/Validation.js";
import { createCancellationToken } from "./fable_modules/fable-library-js.5.17.0/Async.js";
import { ControlFileModule_empty, ControlComment, CommentKind, ControlFileModule_upsert, ControlFileModule_renameBlockAt, ControlFileModule_mergeWithNext, ControlFileModule_splitBlockAt, BlockKind, ControlFileModule_setKindAt, ControlFileModule_blockAt, ControlFileModule_fromJson, ControlFileModule_kindToString, ControlFile, ControlFileModule_toJson } from "./ControlTypesWeb.js";
import { addrY, render } from "./JetpacFR.Core/CtrlMapModel.js";

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
                        setItem(out, i, item_1(Audio_rpos, Audio_ring));
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
                const s = item_1(idx, samples);
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

export let App_currentProjectId = createAtom(undefined);

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
            buf[o]=item_1(o + 2, src);
            buf[(o + 1)]=item_1(o + 1, src);
            buf[(o + 2)]=item_1(o, src);
            buf[(o + 3)]=item_1(o + 3, src);
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
                const arg = item_1((addr + i) & 65535, mem);
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
            const arg_3 = item_1(i, bytes);
            return toText(printf("%02X"))(arg_3);
        }, rangeDouble(0, 1, ~~e.Length - 1)))));
        const tail = (e.Taken === 1) ? toText(printf("   -> %04X"))(e.Target) : (App_isBranch(e.B0) ? "   (not taken)" : "");
        const sm = item_1(~~e.Pc, App_currentSelfModified()) ? "   [self-mod]" : "";
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
                    const patternInput = App_rowFor(t, item_1(idx, t.Entries), idx, idx === c);
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
        item_1(i, App_flagCells).textContent = (((f & item_1(i, bits)) !== 0) ? "1" : "0");
        item_1(i, App_flagCells).style.color = (((f & item_1(i, bits)) !== 0) ? App_green : App_dim);
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
            const e = item_1(max(0, min(App_cursor, t_1.Entries.length - 1)), t_1.Entries);
            const snapIdx = TraceQuery_nearestSnapshotBefore(t_1.Snapshots, e.Tick) | 0;
            if (snapIdx >= 0) {
                const s = item_1(snapIdx, t_1.Snapshots);
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

export const App_heatLutR = map_1((c) => (parse(substring(c, 1, 2), 511, false, 32, 16) & 0xFF), App_heatLut, Uint8Array);

export const App_heatLutG = map_1((c) => (parse(substring(c, 3, 2), 511, false, 32, 16) & 0xFF), App_heatLut, Uint8Array);

export const App_heatLutB = map_1((c) => (parse(substring(c, 5, 2), 511, false, 32, 16) & 0xFF), App_heatLut, Uint8Array);

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
            const c = item_1(i, counts) | 0;
            const li = ((c <= 0) ? 0 : ((logMax <= 0) ? 9 : min(9, ~~(9 * (Math.log10(c) / logMax))))) | 0;
            const p = (i * 4) | 0;
            if (item_1(i, selfMod)) {
                buf[p]=(~~((~~item_1(li, App_heatLutR) + 240) / 2) & 0xFF);
                buf[(p + 1)]=(~~((~~item_1(li, App_heatLutG) + 48) / 2) & 0xFF);
                buf[(p + 2)]=(~~((~~item_1(li, App_heatLutB) + 192) / 2) & 0xFF);
            }
            else {
                buf[p]=item_1(li, App_heatLutR);
                buf[(p + 1)]=item_1(li, App_heatLutG);
                buf[(p + 2)]=item_1(li, App_heatLutB);
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
    let i = 0;
    const arr = t.Entries;
    for (let idx = 0; idx <= (arr.length - 1); idx++) {
        item_1(idx, arr);
        setItem(segs, min(511, ~~(i / n)), (item_1(min(511, ~~(i / n)), segs) + 1) | 0);
        i = ((i + 1) | 0);
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
            const v = Math.log10(item_1(sx, segs) + 1) / logMax;
            const r = (60 + ~~(v * 160)) | 0;
            const g = (120 + ~~(v * 100)) | 0;
            const b = (60 + ~~(v * 60)) | 0;
            for (let sy = 0; sy <= 23; sy++) {
                const p = (((sy * 512) + sx) * 4) | 0;
                buf[p]=(r & 0xFF);
                buf[(p + 1)]=(g & 0xFF);
                buf[(p + 2)]=(b & 0xFF);
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
        App_cursorLabel.textContent = ((arg_1 = ((n - 1) | 0), (arg_2 = (~~item_1(c, value_36(App_currentTrace()).Entries).Pc | 0), toText(printf("instr %d / %d   pc=%04X"))(c)(arg_1)(arg_2))));
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
                const e = item_1(idx, arr);
                if (e.Length !== 0) {
                    setItem(cyc, ~~e.Pc, toInt64_unchecked(op_Addition(item_1(~~e.Pc, cyc), toInt64_unchecked(fromUInt8(e.Cycles)))));
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
                App_contractLabel.textContent = ((arg_4 = (length_1(contract.WriteRanges) | 0), toText(printf("contract for 0x%04X  span=%04X-%04X  calls=%d  writes=%d"))(r.Entry)(r.SpanLo)(r.SpanHi)(r.CallCount)(arg_4)));
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
    const loops = map_2((tupledArg) => toText(printf("%04X->%04X x%d"))(tupledArg[0])(tupledArg[1])(tupledArg[2]), truncate(5, r.LoopExtents));
    App_routineDetail.textContent = ((arg_12 = join("; ", r.Reasons), (arg_13 = (isEmpty(loops) ? "" : ("\nloops: " + join(", ", loops))), (arg_14 = (length_1(r.InputSamples) | 0), toText(printf("%04X  (%s)\ncalls=%d  span=%04X-%04X\nincl=%d  excl=%d tstates\nself-modifying=%b  overlapping=%b\n%s%s\ninput samples: %d"))(r.Entry)(r.Risk)(r.CallCount)(r.SpanLo)(r.SpanHi)(r.InclusiveTStates)(r.ExclusiveTStates)(r.SelfModifying)(r.Overlapping)(arg_12)(arg_13)(arg_14)))));
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
    App_routineCountLabel.textContent = ((arg_7 = (length_1(App_minedRoutines) | 0), toText(printf("%d routines"))(arg_7)));
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
    clear(App_graphNodes);
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
            if ((t = matchValue_1, (entry < t.FirstIndexAtPc.length) && (item_1(entry, t.FirstIndexAtPc) >= 0))) {
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
                App_cursor = (item_1(entry, t_1.FirstIndexAtPc) | 0);
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
            App_activeRoutine = undefined;
            App_routineDetail.textContent = "";
            App_refreshRoutines();
            App_refreshTray();
            App_drawGraph();
            App_statusText.textContent = ((arg = (length_1(routines) | 0), (arg_1 = (length_1(edges) | 0), toText(printf("mined %d routines, %d call edges (self-mod addresses: %d)"))(arg)(arg_1)(t_1.SelfModCount))));
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
                const bytes = new Uint8Array(buf);
                try {
                    const t = decode(bytes);
                    if (t.Entries.length === 0) {
                        App_statusText.textContent = "load failed: trace has no instructions";
                    }
                    else {
                        const patternInput = mine(t);
                        const routines = patternInput[0];
                        App_loaded = t;
                        App_minedRoutines = routines;
                        App_minedEdges = patternInput[1];
                        App_selectedEntries = empty_1({
                            Compare: (x, y) => (comparePrimitives(x, y) | 0),
                        });
                        App_activeRoutine = undefined;
                        App_routineDetail.textContent = "";
                        App_cursor = 0;
                        App_markerPc = (~~item_1(0, t.Entries).Pc | 0);
                        App_refreshRoutines();
                        App_refreshTray();
                        App_drawGraph();
                        App_syncSlider();
                        App_refreshAll();
                        App_statusText.textContent = ((arg = (t.Entries.length | 0), (arg_1 = (length_1(routines) | 0), toText(printf("loaded %d instructions; mined %d routines"))(arg)(arg_1))));
                    }
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
    App_currentProjectId("minimal");
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
        nameSpan_1.textContent = item_1(i, flagNames);
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
        let arg_1, arg_2, arg_3, arg_4, arg_5, arg_6, arg_7;
        if (App_session != null) {
            const s = App_session;
            TraceSession__FlushReplayCache(s);
            if (App_running) {
                App_statusText.textContent = ((arg_1 = (TraceSession__get_Frame(s) | 0), (arg_2 = (toFloat64(TraceSession__get_CycleCount(s)) / 1000000), (arg_3 = (TraceRecorder__get_EntryCount(TraceSession__get_Recorder(s)) | 0), (arg_4 = (TraceRecorder__get_Capacity(TraceSession__get_Recorder(s)) | 0), (arg_5 = (fold_1((n_1, c) => {
                    if (c > 0) {
                        return (n_1 + 1) | 0;
                    }
                    else {
                        return n_1 | 0;
                    }
                }, 0, TraceRecorder__get_PerPcCount(TraceSession__get_Recorder(s))) | 0), (arg_6 = (TraceRecorder__get_SelfModCount(TraceSession__get_Recorder(s)) | 0), (arg_7 = (TraceRecorder__get_RecordEnabled(TraceSession__get_Recorder(s)) ? "ON" : "OFF"), toText(printf("frame=%d  tick=%.2fM  instr=%d/%d  distinct-pc=%d  selfmod=%d  rec=%s"))(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6)(arg_7)))))))));
            }
            App_refreshHeatmap();
            App_refreshStrip();
            if (App_running) {
                App_syncingSlider = true;
                const n_2 = TraceRecorder__get_EntryCount(TraceSession__get_Recorder(s)) | 0;
                if (n_2 > 1) {
                    App_slider.max = (n_2 - 1);
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
                    const idx = ((pc < t_1.FirstIndexAtPc.length) ? item_1(pc, t_1.FirstIndexAtPc) : -1) | 0;
                    if (idx >= 0) {
                        App_cursor = (idx | 0);
                        App_refreshAll();
                        App_syncSlider();
                        App_statusText.textContent = ((arg_9 = (((pc < t_1.PerPcCount.length) ? item_1(pc, t_1.PerPcCount) : 0) | 0), toText(printf("jumped to 0x%04X (first of %d executions)"))(pc)(arg_9)));
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
                    count = item_1(pc_1, t_3.PerPcCount);
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
    const isEditableTarget = (e_4) => {
        const t_4 = e_4.target;
        const tag = toString(t_4.tagName);
        if (((tag === "INPUT") ? true : (tag === "TEXTAREA")) ? true : (tag === "SELECT")) {
            return true;
        }
        else {
            return toString(t_4.isContentEditable) === "true";
        }
    };
    Dom_window.addEventListener("keydown", ((e_5) => {
        const key_1 = e_5.key;
        if (isEditableTarget(e_5)) {
        }
        else {
            App_setKeyFor(key_1, true);
            if ((key_1 === " ") ? true : key_1.startsWith("Arrow")) {
                e_5.preventDefault();
            }
        }
    }));
    Dom_window.addEventListener("keyup", ((e_6) => {
        if (isEditableTarget(e_6)) {
        }
        else {
            App_setKeyFor(toString(e_6.key), false);
        }
    }));
    const controlKey = () => ("jetpacfr.control." + defaultArg(App_currentProjectId(), "minimal"));
    let control = undefined;
    const saveControlLocal = () => {
        if (control == null) {
        }
        else {
            const c_1 = control;
            const local = Dom_window.localStorage;
            local.setItem(controlKey(), ControlFileModule_toJson(c_1));
            control = (new ControlFile(c_1.ImageFile, c_1.Start, c_1.EndExcl, c_1.EntryPc, c_1.ActiveVersion, c_1.Blocks, c_1.Comments, false));
        }
    };
    const tlCanvas = Dom_byId("canvasTimeline");
    const tlCtx = tlCanvas.getContext("2d");
    let tlLen = 300;
    let rangeA = [0, 150];
    let rangeB = [150, 300];
    let brushA = true;
    let tlDragAnchor = undefined;
    let tlChipDrag = undefined;
    let tlStartX = 0;
    let tlMoved = false;
    let chipRectA = [0, 0, 0, 0];
    let chipRectB = [0, 0, 0, 0];
    const tlFrameCount = () => {
        let t_5;
        const matchValue_6 = App_currentTrace();
        let matchResult_2, t_6;
        if (matchValue_6 != null) {
            if ((t_5 = matchValue_6, t_5.FrameTicks.length > 0)) {
                matchResult_2 = 0;
                t_6 = matchValue_6;
            }
            else {
                matchResult_2 = 1;
            }
        }
        else {
            matchResult_2 = 1;
        }
        switch (matchResult_2) {
            case 0:
                return t_6.FrameTicks.length | 0;
            default:
                return 300;
        }
    };
    const entryAtFrame = (f) => {
        let t_7;
        const matchValue_7 = App_currentTrace();
        let matchResult_3, t_8;
        if (matchValue_7 != null) {
            if ((t_7 = matchValue_7, (t_7.Entries.length > 0) && (t_7.FrameTicks.length > 0))) {
                matchResult_3 = 0;
                t_8 = matchValue_7;
            }
            else {
                matchResult_3 = 1;
            }
        }
        else {
            matchResult_3 = 1;
        }
        switch (matchResult_3) {
            case 0: {
                const last = item_1(t_8.FrameTicks.length - 1, t_8.FrameTicks);
                const tick = (f <= 0) ? 0 : ((f >= t_8.FrameTicks.length) ? last : item_1(f, t_8.FrameTicks));
                let lo = 0;
                let hi = t_8.Entries.length - 1;
                let res = t_8.Entries.length - 1;
                while (lo <= hi) {
                    const mid = ~~((lo + hi) / 2) | 0;
                    if (item_1(mid, t_8.Entries).Tick >= tick) {
                        res = (mid | 0);
                        hi = ((mid - 1) | 0);
                    }
                    else {
                        lo = ((mid + 1) | 0);
                    }
                }
                return res | 0;
            }
            default:
                return 0;
        }
    };
    const tlRender = () => {
        const w = tlCanvas.width;
        const h = tlCanvas.height;
        tlCtx.fillStyle = "#101016";
        tlCtx.fillRect(0, 0, w, h);
        tlCtx.strokeStyle = "#3A3F4C";
        tlCtx.strokeRect(0.5, 0.5, (w - 1), (h - 1));
        const bottom = (h - 13) - 2;
        const len = max(1, tlLen) | 0;
        const drawBrush = (which, _arg_22) => {
            const color = which ? "#38BDF8" : "#FBBF24";
            const x0 = min(w, max(0, (_arg_22[0] / len) * w));
            const x1 = min(w, max(0, (_arg_22[1] / len) * w));
            tlCtx.fillStyle = (which ? "rgba(56,189,248,0.15)" : "rgba(251,191,36,0.15)");
            tlCtx.fillRect(min(x0, x1), 2, max(2, Math.abs(x1 - x0)), (bottom - 2));
            tlCtx.strokeStyle = color;
            tlCtx.lineWidth = 2;
            tlCtx.beginPath();
            tlCtx.moveTo(x0, 2);
            tlCtx.lineTo(x0, bottom);
            tlCtx.moveTo(x1, 2);
            tlCtx.lineTo(x1, bottom);
            tlCtx.stroke();
            const label = which ? "A" : "B";
            const chipX = which ? min(x0, max(0, w - 14)) : max(0, min(x1 - 14, w - 14));
            const chipY = bottom - 12;
            tlCtx.fillStyle = color;
            tlCtx.fillRect(chipX, chipY, 14, 12);
            tlCtx.fillStyle = "#020617";
            tlCtx.font = "bold 9px Consolas";
            tlCtx.fillText(label, (chipX + 4), (chipY + 9.5));
            if (which) {
                chipRectA = [chipX, chipY, 14, 12];
            }
            else {
                chipRectB = [chipX, chipY, 14, 12];
            }
        };
        drawBrush(true, rangeA);
        drawBrush(false, rangeB);
        let step_1;
        const nice = new Float64Array([1, 2, 2.5, 5]);
        const niceCeil = (v_mut, base10_mut) => {
            niceCeil:
            while (true) {
                const v = v_mut, base10 = base10_mut;
                const m = v / base10;
                const matchValue_8 = tryFind_1((n_3) => (n_3 >= (m - 1E-09)), nice);
                if (matchValue_8 == null) {
                    if (base10 >= 1000000000) {
                        return v;
                    }
                    else {
                        v_mut = v;
                        base10_mut = (base10 * 10);
                        continue niceCeil;
                    }
                }
                else {
                    return matchValue_8 * base10;
                }
                break;
            }
        };
        const l = len;
        let step = niceCeil(l / 4, 1);
        while ((Math.floor(l / step) + 1) > 4) {
            step = niceCeil(step * 1.01, 1);
        }
        while (((Math.floor(l / step) + 1) < 3) && (step > 1)) {
            step = niceCeil(step / 2.02, 1);
        }
        step_1 = max(1, ~~Math.ceil(step));
        tlCtx.strokeStyle = "#8A8A92";
        tlCtx.lineWidth = 1;
        tlCtx.fillStyle = "#8A8A92";
        tlCtx.font = "8px Consolas";
        let t_9 = 0;
        while (t_9 <= len) {
            const x_5 = (t_9 / len) * w;
            tlCtx.beginPath();
            tlCtx.moveTo(x_5, (bottom - 3));
            tlCtx.lineTo(x_5, (bottom + 1));
            tlCtx.stroke();
            tlCtx.fillText(int32ToString(t_9), min(max(2, x_5 - 8), w - 20), (h - 4));
            t_9 = ((t_9 + step_1) | 0);
        }
    };
    const tlSetRange = (which_1, r_2) => {
        if (which_1) {
            rangeA = r_2;
        }
        else {
            rangeB = r_2;
        }
        tlRender();
    };
    const tlUnitAt = (x_6) => {
        const w_1 = tlCanvas.width;
        return ~~round((min(max(x_6, 0), w_1) / w_1) * max(1, tlLen)) | 0;
    };
    const tlX = (e_7) => {
        const rect = tlCanvas.getBoundingClientRect();
        return (e_7.clientX - rect.left) * (tlCanvas.width / rect.width);
    };
    const inChip = (r_3, x_7, y_5) => {
        const ry = r_3[1];
        const rx = r_3[0];
        if (((x_7 >= (rx - 3)) && (x_7 <= ((rx + r_3[2]) + 3))) && (y_5 >= (ry - 3))) {
            return y_5 <= ((ry + r_3[3]) + 3);
        }
        else {
            return false;
        }
    };
    tlCanvas.addEventListener("mousedown", ((e_9) => {
        const x_8 = tlX(e_9);
        let y_6;
        const rect_1 = tlCanvas.getBoundingClientRect();
        y_6 = (e_9.clientY - rect_1.top);
        tlStartX = x_8;
        tlMoved = false;
        if (inChip(chipRectA, x_8, y_6)) {
            tlChipDrag = true;
        }
        else if (inChip(chipRectB, x_8, y_6)) {
            tlChipDrag = false;
        }
        else {
            tlChipDrag = undefined;
            tlDragAnchor = tlUnitAt(x_8);
            App_cursor = (entryAtFrame(tlUnitAt(x_8)) | 0);
            App_refreshAll();
            App_syncSlider();
        }
    }));
    tlCanvas.addEventListener("mousemove", ((e_10) => {
        const x_9 = tlX(e_10);
        const tlChipDrag_1 = tlChipDrag;
        const tlDragAnchor_1 = tlDragAnchor;
        if (tlChipDrag_1 == null) {
            if (tlDragAnchor_1 != null) {
                const anchor = tlDragAnchor_1 | 0;
                if (Math.abs(x_9 - tlStartX) > 3) {
                    tlMoved = true;
                    const u_1 = tlUnitAt(x_9) | 0;
                    tlSetRange(brushA, [min(anchor, u_1), max(anchor, u_1)]);
                }
                App_cursor = (entryAtFrame(tlUnitAt(x_9)) | 0);
                App_refreshAll();
                App_syncSlider();
            }
        }
        else {
            const which_2 = tlChipDrag_1;
            if (Math.abs(x_9 - tlStartX) > 3) {
                const u = tlUnitAt(x_9) | 0;
                if (which_2) {
                    rangeA[0];
                    const re_1 = rangeA[1] | 0;
                    tlSetRange(true, [min(u, re_1), re_1]);
                }
                else {
                    const rs_2 = rangeB[0] | 0;
                    rangeB[1];
                    tlSetRange(false, [rs_2, max(rs_2, u)]);
                }
            }
        }
    }));
    Dom_window.addEventListener("mouseup", ((_arg_23) => {
        let matchResult_4, anchor_2;
        if (tlDragAnchor != null) {
            if (!tlMoved) {
                matchResult_4 = 0;
                anchor_2 = tlDragAnchor;
            }
            else {
                matchResult_4 = 1;
            }
        }
        else {
            matchResult_4 = 1;
        }
        switch (matchResult_4) {
            case 0: {
                tlSetRange(brushA, [anchor_2, anchor_2]);
                break;
            }
        }
        tlDragAnchor = undefined;
        tlChipDrag = undefined;
    }));
    const brushStyle = () => {
        const a_2 = Dom_byId("btnBrushA");
        const b = Dom_byId("btnBrushB");
        a_2.className = (brushA ? "brush-btn active-brush-a" : "brush-btn");
        b.className = (brushA ? "brush-btn" : "brush-btn active-brush-b");
    };
    Dom_byId("btnBrushA").addEventListener("click", ((_arg_24) => {
        brushA = true;
        brushStyle();
    }));
    Dom_byId("btnBrushB").addEventListener("click", ((_arg_25) => {
        brushA = false;
        brushStyle();
    }));
    let previewFrom = 0;
    let previewSpan = 0;
    let previewTicks = 0;
    let previewActive = false;
    const previewRange = (which_4) => {
        const patternInput = which_4 ? rangeA : rangeB;
        const fStart = patternInput[0] | 0;
        const fEnd = patternInput[1] | 0;
        const label_1 = which_4 ? "A" : "B";
        const n_5 = tlFrameCount() | 0;
        if (((fEnd - 1) < n_5) && ((fEnd - 1) >= 0)) {
            App_pauseGame();
            previewFrom = (max(0, fStart) | 0);
            previewSpan = (max(0, (fEnd - 1) - previewFrom) | 0);
            previewTicks = 0;
            previewActive = true;
            App_cursor = (entryAtFrame(previewFrom) | 0);
            App_refreshAll();
            App_syncSlider();
            App_statusText.textContent = toText(printf("preview %s: frames %d..%d over 1s (cursor sweep)"))(label_1)(fStart)(fEnd);
        }
        else {
            App_statusText.textContent = toText(printf("preview %s: range outside the recording (%d frames)"))(label_1)(n_5);
        }
    };
    Dom_byId("btnPreviewA").addEventListener("click", ((_arg_26) => {
        previewRange(true);
    }));
    Dom_byId("btnPreviewB").addEventListener("click", ((_arg_27) => {
        previewRange(false);
    }));
    Dom_setInterval(() => {
        if (previewActive) {
            previewTicks = ((previewTicks + 1) | 0);
            const frac = min(previewTicks, 50) | 0;
            App_cursor = (entryAtFrame(previewFrom + ~~((previewSpan * frac) / 50)) | 0);
            App_refreshAll();
            App_syncSlider();
            if (previewTicks >= 50) {
                previewActive = false;
            }
        }
    }, 20);
    const cmCanvas = Dom_byId("canvasCtrlMap");
    const cmCtx = cmCanvas.getContext("2d");
    let cmViewStart = 0;
    let cmViewEnd = 65536;
    let instrStartsCache = undefined;
    let instrStartsAt = 0;
    let cmCursorAddr = -1;
    let cmDragAddr = undefined;
    let cmDisasmLo = -1;
    let cmDisasmHi = -1;
    const cmMinSpan = () => (max(32, ~~(cmCanvas.height / 32)) | 0);
    const cmCounts = () => {
        if (App_loaded == null) {
            if (App_session == null) {
                return new Int32Array(65536);
            }
            else {
                return TraceRecorder__get_PerPcCount(TraceSession__get_Recorder(App_session));
            }
        }
        else {
            return App_loaded.PerPcCount;
        }
    };
    const cmAddrY = (addr) => ((cmCanvas.height * (addr - cmViewStart)) / max(1, cmViewEnd - cmViewStart));
    const cmRender = () => {
        let s_2, now, st, a_3, option_1, arg_42, arg_43;
        const w_2 = cmCanvas.width;
        const h_1 = cmCanvas.height;
        const cw = cmCanvas.clientWidth;
        const ch = cmCanvas.clientHeight;
        if ((cw > 0) && (~~cw !== ~~w_2)) {
            cmCanvas.width = ~~cw;
        }
        if ((ch > 0) && (~~ch !== ~~h_1)) {
            cmCanvas.height = ~~ch;
        }
        const w_3 = cmCanvas.width;
        const h_2 = cmCanvas.height;
        cmCtx.fillStyle = "#0A0A10";
        cmCtx.fillRect(0, 0, w_3, h_2);
        const blocks = (control == null) ? empty() : control.Blocks;
        const comments = (control == null) ? empty() : control.Comments;
        const rowCount = max(1, ~~Math.ceil(h_2)) | 0;
        const m_1 = render(blocks, comments, (App_session != null) ? ((s_2 = App_session, (now = (Date.now()), (((instrStartsCache == null) ? true : ((now - instrStartsAt) > 1000)) ? ((st = fill(new Array(65536), 0, 65536, false), (a_3 = 0, ((() => {
            while (a_3 < 65536) {
                setItem(st, a_3, true);
                const len_1 = disasmMemory(TraceSession__get_Memory(s_2), a_3).Length | 0;
                a_3 = ((a_3 + ((len_1 < 1) ? 1 : min(8, len_1))) | 0);
            }
        })(), (instrStartsCache = st, instrStartsAt = now))))) : undefined, instrStartsCache)))) : undefined, (option_1 = App_session, (option_1 != null) ? TraceSession__get_Memory(option_1) : undefined), cmCounts(), (App_loaded == null) ? ((App_session == null) ? fill(new Array(65536), 0, 65536, false) : TraceRecorder__get_SelfModified(TraceSession__get_Recorder(App_session))) : App_loaded.SelfModified, cmViewStart, cmViewEnd, rowCount);
        const heatLutI = [[8, 8, 12], [10, 30, 74], [10, 58, 94], [10, 92, 106], [18, 134, 94], [58, 168, 60], [150, 184, 44], [216, 160, 34], [232, 102, 24], [240, 56, 40]];
        if (m_1.Rows.length > 0) {
            const rowH = h_2 / m_1.Rows.length;
            let y_8 = 0;
            while (y_8 < m_1.Rows.length) {
                const r_4 = item_1(y_8, m_1.Rows);
                let col;
                const heat = r_4.Heat | 0;
                let patternInput_1;
                const _arg_28 = r_4.Kind;
                patternInput_1 = ((_arg_28.tag === 2) ? [200, 138, 46] : ((_arg_28.tag === 3) ? [58, 63, 76] : ((_arg_28.tag === 4) ? [85, 81, 107] : ((_arg_28.tag === 0) ? [10, 10, 16] : [46, 125, 209]))));
                const br = patternInput_1[0] | 0;
                const bgc = patternInput_1[1] | 0;
                const bb = patternInput_1[2] | 0;
                if (heat <= 0) {
                    col = toText(printf("rgb(%d,%d,%d)"))(br)(bgc)(bb);
                }
                else {
                    const patternInput_2 = item_1(min(9, heat), heatLutI);
                    const t_12 = 0.15 + ((0.65 * heat) / 9);
                    const mix = (x_10, y_7) => (~~round(x_10 + ((y_7 - x_10) * t_12)) | 0);
                    const arg_39 = mix(br, patternInput_2[0]) | 0;
                    const arg_40 = mix(bgc, patternInput_2[1]) | 0;
                    const arg_41 = mix(bb, patternInput_2[2]) | 0;
                    col = toText(printf("rgb(%d,%d,%d)"))(arg_39)(arg_40)(arg_41);
                }
                let y2 = y_8 + 1;
                while (((y2 < m_1.Rows.length) && equals(item_1(y2, m_1.Rows).Kind, r_4.Kind)) && (item_1(y2, m_1.Rows).Heat === r_4.Heat)) {
                    y2 = ((y2 + 1) | 0);
                }
                cmCtx.fillStyle = col;
                cmCtx.fillRect(0, (y_8 * rowH), w_3, (((y2 - y_8) * rowH) + 0.5));
                for (let yy = y_8; yy <= (y2 - 1); yy++) {
                    const ry_1 = yy * rowH;
                    if (item_1(yy, m_1.Rows).SelfMod) {
                        cmCtx.fillStyle = "#F030F0";
                        cmCtx.fillRect(0, ry_1, 2, 1);
                    }
                    if (item_1(yy, m_1.Rows).Comment) {
                        cmCtx.fillStyle = "#4EE060";
                        cmCtx.fillRect(2, ry_1, 2, 1);
                    }
                }
                y_8 = (y2 | 0);
            }
            const arr = m_1.Ranges;
            for (let idx_1 = 0; idx_1 <= (arr.length - 1); idx_1++) {
                const forLoopVar_2 = item_1(idx_1, arr);
                const y0 = addrY(m_1.ViewStart, m_1.ViewEnd, m_1.Rows.length, forLoopVar_2[0]);
                const y1 = addrY(m_1.ViewStart, m_1.ViewEnd, m_1.Rows.length, forLoopVar_2[1]);
                cmCtx.fillStyle = "rgba(78,224,96,0.19)";
                cmCtx.fillRect(0, (y0 * rowH), w_3, ((y1 - y0) * rowH));
            }
            cmCtx.fillStyle = "#E8E8EC";
            cmCtx.font = "9px Consolas";
            const arr_1 = m_1.Labels;
            for (let idx_2 = 0; idx_2 <= (arr_1.length - 1); idx_2++) {
                const lbl = item_1(idx_2, arr_1);
                const y_9 = addrY(m_1.ViewStart, m_1.ViewEnd, m_1.Rows.length, lbl.Addr);
                cmCtx.fillText(lbl.Text, 5, ((y_9 * rowH) + 9));
            }
        }
        if ((cmDisasmLo >= 0) && (cmDisasmHi > cmDisasmLo)) {
            cmCtx.strokeStyle = "rgba(255,255,255,0.5)";
            cmCtx.lineWidth = 1;
            cmCtx.strokeRect(1, cmAddrY(cmDisasmLo), (w_3 - 2), max(3, cmAddrY(cmDisasmHi) - cmAddrY(cmDisasmLo)));
        }
        if (((cmCursorAddr >= 0) && (cmCursorAddr >= cmViewStart)) && (cmCursorAddr < cmViewEnd)) {
            cmCtx.strokeStyle = "#4ED0E0";
            cmCtx.beginPath();
            cmCtx.moveTo(0, cmAddrY(cmCursorAddr));
            cmCtx.lineTo(w_3, cmAddrY(cmCursorAddr));
            cmCtx.stroke();
        }
        cmCtx.fillStyle = "#6A6A74";
        cmCtx.font = "8px Consolas";
        return cmCtx.fillText(((arg_42 = (cmViewStart | 0), (arg_43 = (cmViewEnd | 0), toText(printf("%04X-%04X"))(arg_42)(arg_43)))), 2, 8);
    };
    const cmXy = (e_11) => {
        const rect_2 = cmCanvas.getBoundingClientRect();
        return [e_11.clientX - rect_2.left, e_11.clientY - rect_2.top];
    };
    const cmAddrAt = (y_10) => {
        const h_3 = cmCanvas.height;
        return (cmViewStart + ~~min((y_10 / h_3) * (cmViewEnd - cmViewStart), (cmViewEnd - cmViewStart) - 1)) | 0;
    };
    const cmNavigate = (addr_2) => {
        let t_13;
        const matchValue_10 = App_currentTrace();
        let matchResult_5, t_14;
        if (matchValue_10 != null) {
            if ((t_13 = matchValue_10, ((t_13.Entries.length > 0) && (addr_2 < t_13.FirstIndexAtPc.length)) && (item_1(addr_2, t_13.FirstIndexAtPc) >= 0))) {
                matchResult_5 = 0;
                t_14 = matchValue_10;
            }
            else {
                matchResult_5 = 1;
            }
        }
        else {
            matchResult_5 = 1;
        }
        switch (matchResult_5) {
            case 0: {
                App_cursor = (item_1(addr_2, t_14.FirstIndexAtPc) | 0);
                App_refreshAll();
                App_syncSlider();
                break;
            }
            case 1: {
                break;
            }
        }
    };
    let projects = defaultOf();
    let selectedIdx = -1;
    const projAt = (i_1) => (projects[i_1]);
    let openSelected = () => {
    };
    const renderLauncher = () => {
        const list = Dom_byId("launcherList");
        list.innerHTML = "";
        if (Operators_IsNull(projects)) {
            const row_2 = Dom_el("div");
            row_2.className = "hint";
            row_2.style.padding = "10px";
            row_2.textContent = "games/index.json not found - the launcher needs a projects index";
            Dom_append(list, row_2);
        }
        else {
            const n_6 = projects.length | 0;
            for (let i_2 = 0; i_2 <= (n_6 - 1); i_2++) {
                let arg_46, arg_47;
                const g = projAt(i_2);
                const id = toString(g.id);
                const name_1 = toString(g.name);
                const isWeb = g.web;
                const isDef = g.default;
                const row_3 = Dom_el("div");
                row_3.className = ((i_2 === selectedIdx) ? "proj-row sel" : "proj-row");
                const title = Dom_el("div");
                title.className = "proj-title";
                title.textContent = ((arg_46 = (isDef ? "  - auto-load default" : ""), toText(printf("%s (%s)%s"))(name_1)(id)(arg_46)));
                Dom_append(row_3, title);
                const chips = Dom_el("div");
                chips.className = "proj-chips";
                const chip = (cls, text) => {
                    const s_6 = Dom_el("span");
                    s_6.className = cls;
                    s_6.textContent = text;
                    Dom_append(chips, s_6);
                };
                if (isWeb) {
                    chip("chip-on", "deployed to web");
                }
                else {
                    chip("chip-off", "not deployed to web");
                }
                chip(g.trace ? "chip-on" : "chip-off", g.trace ? "trace: yes" : "trace: none");
                chip(g.keys ? "chip-on" : "chip-off", g.keys ? "key script: yes" : "key script: no");
                chip(g.control ? "chip-on" : "chip-off", g.control ? "control file: yes" : "control file: no");
                chip(g.ce ? "chip-on" : "chip-off", g.ce ? "CE program: yes" : "CE program: no");
                chip("chip-on", (arg_47 = toString(g.boot), toText(printf("boot: %s"))(arg_47)));
                Dom_append(row_3, chips);
                row_3.addEventListener("click", ((_arg_29) => {
                    selectedIdx = (i_2 | 0);
                    renderLauncher();
                }));
                row_3.addEventListener("dblclick", ((_arg_30) => {
                    openSelected();
                }));
                Dom_append(list, row_3);
            }
        }
    };
    const refreshNamesList = () => {
        let arg_59, arg_60;
        const list_1 = Dom_byId("namesList");
        const count_1 = Dom_byId("namesCount");
        list_1.innerHTML = "";
        const mkRow = (text_1, color_1, addr_3) => {
            const row_4 = Dom_el("div");
            row_4.className = "name-row";
            row_4.textContent = text_1;
            row_4.style.color = color_1;
            row_4.addEventListener("click", ((_arg_31) => {
                let t_15;
                if (addr_3 < 0) {
                }
                else {
                    const matchValue_11 = App_currentTrace();
                    let matchResult_6, t_16;
                    if (matchValue_11 != null) {
                        if ((t_15 = matchValue_11, ((t_15.Entries.length > 0) && (addr_3 < t_15.FirstIndexAtPc.length)) && (item_1(addr_3, t_15.FirstIndexAtPc) >= 0))) {
                            matchResult_6 = 0;
                            t_16 = matchValue_11;
                        }
                        else {
                            matchResult_6 = 1;
                        }
                    }
                    else {
                        matchResult_6 = 1;
                    }
                    switch (matchResult_6) {
                        case 0: {
                            App_cursor = (item_1(addr_3, t_16.FirstIndexAtPc) | 0);
                            App_refreshAll();
                            App_syncSlider();
                            App_statusText.textContent = toText(printf("jumped to 0x%04X"))(addr_3);
                            break;
                        }
                        case 1: {
                            App_statusText.textContent = toText(printf("0x%04X has not executed in this trace"))(addr_3);
                            break;
                        }
                    }
                }
            }));
            Dom_append(list_1, row_4);
        };
        if (control != null) {
            const c_4 = control;
            const enumerator_3 = getEnumerator(c_4.Blocks);
            try {
                while (enumerator_3["System.Collections.IEnumerator.MoveNext"]()) {
                    const b_2 = enumerator_3["System.Collections.Generic.IEnumerator`1.get_Current"]();
                    mkRow(toText(printf("block    %04X..%04X  %s"))(b_2.Start)(b_2.EndExcl)(b_2.Name), App_cyan, b_2.Start);
                }
            }
            finally {
                disposeSafe(enumerator_3);
            }
            const enumerator_4 = getEnumerator(c_4.Comments);
            try {
                while (enumerator_4["System.Collections.IEnumerator.MoveNext"]()) {
                    let arg_56;
                    const m_2 = enumerator_4["System.Collections.Generic.IEnumerator`1.get_Current"]();
                    const span_2 = (m_2.EndExcl > m_2.Addr) ? toText(printf("%04X..%04X"))(m_2.Addr)(m_2.EndExcl) : toText(printf("%04X"))(m_2.Addr);
                    let color_2;
                    const matchValue_12 = m_2.Kind;
                    color_2 = ((matchValue_12.tag === 1) ? App_yellow : ((matchValue_12.tag === 3) ? App_orange : App_normal));
                    mkRow((arg_56 = ControlFileModule_kindToString(m_2.Kind), toText(printf("%-7s %s  %s"))(arg_56)(span_2)(m_2.Text)), color_2, m_2.Addr);
                }
            }
            finally {
                disposeSafe(enumerator_4);
            }
            count_1.textContent = ((arg_59 = (length_1(c_4.Blocks) | 0), (arg_60 = (length_1(c_4.Comments) | 0), toText(printf("%d blocks, %d comments"))(arg_59)(arg_60))));
        }
        else {
            mkRow("no control file loaded - use New / Imp in the control map", App_dim, -1);
            count_1.textContent = "";
        }
    };
    openSelected = (() => {
        if (selectedIdx >= 0) {
            const g_1 = projAt(selectedIdx);
            const id_1 = toString(g_1.id);
            const name_2 = toString(g_1.name);
            if (!g_1.web) {
                App_statusText.textContent = toText(printf("%s is not deployed to the web build yet - copy its web assets into webroot/games/%s and regenerate games/index.json"))(name_2)(id_1);
            }
            else {
                const switch$ = (App_currentProjectId() == null) ? true : (App_currentProjectId() !== id_1);
                App_currentProjectId(id_1);
                Dom_byId("launcher").style.display = "none";
                if (App_currentProjectId() != null) {
                    Dom_byId("btnMenu").style.display = "inline-block";
                }
                if (switch$) {
                    control = undefined;
                    const local_1 = Dom_window.localStorage;
                    let matchValue_14;
                    try {
                        matchValue_14 = (local_1.getItem(controlKey()));
                    }
                    catch (matchValue_13) {
                        matchValue_14 = defaultOf();
                    }
                    if (equals(matchValue_14, defaultOf())) {
                        const path = toText(printf("games/%s/control.json"))(id_1);
                        const p_1 = fetch(path).then(function (r) { return r.ok ? r.text() : null }).catch(function () { return null });
                        p_1.then((txt) => {
                            if (!Operators_IsNull(txt)) {
                                try {
                                    control = ControlFileModule_fromJson(toString(txt));
                                    cmRender();
                                    refreshNamesList();
                                    App_statusText.textContent = toText(printf("control file loaded from %s"))(path);
                                }
                                catch (ex_1) {
                                    App_statusText.textContent = ("stored control.json ignored: " + ex_1.message);
                                }
                            }
                        });
                    }
                    else {
                        try {
                            control = ControlFileModule_fromJson(toString(matchValue_14));
                            cmRender();
                            refreshNamesList();
                        }
                        catch (ex_2) {
                            App_statusText.textContent = ("stored control ignored: " + ex_2.message);
                        }
                    }
                    App_statusText.textContent = toText(printf("%s opened"))(name_2);
                }
            }
        }
    });
    const showLauncher = () => {
        Dom_byId("launcher").style.display = "flex";
        Dom_byId("btnMenu").style.display = "none";
        App_statusText.textContent = "main menu";
        const p = fetch('games/index.json').then(function (r) { return r.ok ? r.json() : null }).catch(function () { return null });
        p.then((list_2) => {
            projects = list_2;
            selectedIdx = -1;
            if (!Operators_IsNull(projects)) {
                const n_7 = projects.length | 0;
                let defIdx = -1;
                let webIdx = -1;
                for (let i_3 = 0; i_3 <= (n_7 - 1); i_3++) {
                    const g_2 = projAt(i_3);
                    if (g_2.default) {
                        defIdx = (i_3 | 0);
                    }
                    if ((webIdx === -1) && g_2.web) {
                        webIdx = (i_3 | 0);
                    }
                }
                selectedIdx = ((((defIdx >= 0) && projAt(defIdx).web) ? defIdx : ((webIdx >= 0) ? webIdx : ((defIdx >= 0) ? defIdx : ((n_7 > 0) ? 0 : -1)))) | 0);
            }
            renderLauncher();
        });
    };
    Dom_byId("btnLauncherOpen").addEventListener("click", ((_arg_32) => {
        openSelected();
    }));
    Dom_byId("btnLauncherRescan").addEventListener("click", ((_arg_33) => {
        showLauncher();
    }));
    Dom_byId("btnMenu").addEventListener("click", ((_arg_34) => {
        App_pauseGame();
        showLauncher();
    }));
    Dom_byId("btnNamesRefresh").addEventListener("click", ((_arg_35) => {
        refreshNamesList();
    }));
    Dom_byId("tabbar").addEventListener("click", ((_arg_36) => {
        refreshNamesList();
    }));
    cmCanvas.addEventListener("mousedown", ((e_12) => {
        const addr_4 = cmAddrAt(cmXy(e_12)[1]) | 0;
        cmDragAddr = addr_4;
        cmNavigate(addr_4);
    }));
    cmCanvas.addEventListener("mousemove", ((e_13) => {
        let option_3, b_3;
        const y_12 = cmXy(e_13)[1];
        if (cmDragAddr == null) {
            const addr_6 = cmAddrAt(y_12) | 0;
            const blockTxt = (control == null) ? "" : defaultArg((option_3 = ControlFileModule_blockAt(control, addr_6), (option_3 != null) ? ((b_3 = option_3, toText(printf(" %s (%A)"))(b_3.Name)(b_3.Kind))) : undefined), " <unmapped>");
            const counts = cmCounts();
            const n_8 = ((addr_6 < counts.length) ? item_1(addr_6, counts) : 0) | 0;
            cmCanvas.title = toText(printf("0x%04X%s (%d executions)"))(addr_6)(blockTxt)(n_8);
        }
        else {
            const addr_5 = cmAddrAt(y_12) | 0;
            cmDragAddr = addr_5;
            cmNavigate(addr_5);
        }
    }));
    Dom_window.addEventListener("mouseup", ((_arg_37) => {
        cmDragAddr = undefined;
    }));
    cmCanvas.addEventListener("wheel", ((e_14) => {
        e_14.preventDefault();
        const delta = e_14.deltaY;
        const frac_1 = min(1, max(0, cmXy(e_14)[1] / max(1, cmCanvas.height)));
        const oldSpan = (cmViewEnd - cmViewStart) | 0;
        const anchor_3 = (cmViewStart + ~~(frac_1 * oldSpan)) | 0;
        const factor = Math.pow(1.15, -delta / 100);
        const newSpan = max(cmMinSpan(), min(65536, ~~(oldSpan / factor))) | 0;
        cmViewStart = ((anchor_3 - ~~(frac_1 * newSpan)) | 0);
        cmViewEnd = ((cmViewStart + newSpan) | 0);
        const span = max(cmMinSpan(), cmViewEnd - cmViewStart) | 0;
        cmViewStart = (max(0, min(65536 - span, cmViewStart)) | 0);
        cmViewEnd = ((cmViewStart + span) | 0);
        return cmRender();
    }));
    cmCanvas.addEventListener("contextmenu", ((e_15) => {
        let arg_76, arg_77;
        e_15.preventDefault();
        const addr_7 = cmAddrAt(cmXy(e_15)[1]) | 0;
        if (control != null) {
            const menu = Dom_byId("ctrlMenu");
            Dom_clear(menu);
            const addItem = (txt_1, act) => {
                const item = Dom_el("div");
                item.textContent = txt_1;
                item.addEventListener("click", ((_arg_38) => {
                    if (control == null) {
                    }
                    else {
                        const c_7 = control;
                        control = act(c_7);
                        saveControlLocal();
                        App_refreshDisasm();
                        cmRender();
                        App_statusText.textContent = toText(printf("%s @ %04X"))(txt_1)(addr_7);
                    }
                    menu.style.display = "none";
                }));
                Dom_append(menu, item);
            };
            addItem("kind: code", (c_8) => ControlFileModule_setKindAt(c_8, addr_7, BlockKind.Code));
            addItem("kind: data", (c_9) => ControlFileModule_setKindAt(c_9, addr_7, BlockKind.Data));
            addItem("kind: gap", (c_10) => ControlFileModule_setKindAt(c_10, addr_7, BlockKind.Gap));
            Dom_append(menu, Dom_el("hr"));
            const split = Dom_el("div");
            split.textContent = "split block here";
            split.addEventListener("click", ((_arg_39) => {
                let s_7;
                if (control == null) {
                }
                else {
                    const c_11 = control;
                    if ((instrStartsCache == null) ? true : ((s_7 = instrStartsCache, (addr_7 < s_7.length) && item_1(addr_7, s_7)))) {
                        control = ControlFileModule_splitBlockAt(c_11, addr_7);
                        saveControlLocal();
                        cmRender();
                        App_statusText.textContent = toText(printf("block split at %04X"))(addr_7);
                    }
                    else {
                        App_statusText.textContent = toText(printf("%04X is not an instruction start"))(addr_7);
                    }
                }
                menu.style.display = "none";
            }));
            Dom_append(menu, split);
            addItem("merge into next block", (c_12) => ControlFileModule_mergeWithNext(c_12, addr_7));
            addItem("rename block...", (c_13) => {
                const name_3 = Dom_window.prompt("block name", "");
                return (Operators_IsNull(name_3) ? true : (name_3.trim().length === 0)) ? c_13 : ControlFileModule_renameBlockAt(c_13, addr_7, name_3.trim());
            });
            addItem("line comment...", (c_14) => {
                const txt_2 = Dom_window.prompt(toText(printf("comment at %04X"))(addr_7), "");
                return Operators_IsNull(txt_2) ? c_14 : ControlFileModule_upsert(c_14, new ControlComment(CommentKind.Line, addr_7, 0, -1, txt_2));
            });
            addItem("range over block...", (c_15) => {
                const txt_3 = Dom_window.prompt("comment text for the whole block", "");
                if (Operators_IsNull(txt_3)) {
                    return c_15;
                }
                else {
                    const matchValue_15 = ControlFileModule_blockAt(c_15, addr_7);
                    if (matchValue_15 == null) {
                        return c_15;
                    }
                    else {
                        const b_4 = matchValue_15;
                        return ControlFileModule_upsert(c_15, new ControlComment(CommentKind.Range$, b_4.Start, b_4.EndExcl, -1, txt_3));
                    }
                }
            });
            menu.style.left = ((arg_76 = (~~e_15.clientX | 0), toText(printf("%dpx"))(arg_76)));
            menu.style.top = ((arg_77 = (~~e_15.clientY | 0), toText(printf("%dpx"))(arg_77)));
            menu.style.display = "block";
        }
        else {
            App_statusText.textContent = "no control file - click New first";
        }
    }));
    Dom_window.addEventListener("mousedown", ((e_16) => {
        const menu_1 = Dom_byId("ctrlMenu");
        if (((menu_1.style.display === "block") && !Operators_IsNull(e_16.target)) && !(e_16.target === menu_1)) {
            if (!(e_16.target.closest && e_16.target.closest('#ctrlMenu') != null)) {
                menu_1.style.display = "none";
            }
        }
    }));
    App_statusText.textContent = "booting emulator to game entry...";
    Dom_setTimeout(() => {
        try {
            App_session = TraceSession_$ctor_28C3603C(decode_1(RomB64), decode_1(TzxB64), App_capacity);
            App_startGame();
            App_refreshAll();
            App_syncSlider();
        }
        catch (ex_3) {
            App_statusText.textContent = ("boot failed: " + ex_3.message);
        }
    }, 30);
    Dom_setInterval(() => {
        const bar = Dom_byId("modeBar");
        if (App_session == null) {
            bar.textContent = "booting...";
            bar.style.color = App_dim;
        }
        else {
            const recOn = App_chkRec.checked;
            if (App_running) {
                if (recOn) {
                    bar.textContent = "● RECORDING - live play, new trace";
                    bar.style.color = App_red;
                }
                else {
                    bar.textContent = "running live - not recording";
                    bar.style.color = App_dim;
                }
            }
            else if (recOn) {
                bar.textContent = "paused - recording armed, press Run to capture";
                bar.style.color = App_red;
            }
            else {
                bar.textContent = "paused";
                bar.style.color = App_dim;
            }
        }
        const n_9 = tlFrameCount() | 0;
        if (n_9 !== tlLen) {
            tlLen = (n_9 | 0);
            tlRender();
        }
    }, 100);
    showLauncher();
    Dom_byId("btnCtrlNew").addEventListener("click", ((_arg_40) => {
        control = ControlFileModule_empty(16384, 65536);
        cmRender();
        App_statusText.textContent = "empty control file created over $4000-$FFFF";
    }));
    Dom_byId("btnCtrlSave").addEventListener("click", ((_arg_41) => {
        if (control == null) {
            App_statusText.textContent = "no control file - click New first";
        }
        else if (control.Dirty) {
            const c_17 = control;
            saveControlLocal();
            App_statusText.textContent = "control saved to browser storage";
        }
        else {
            App_statusText.textContent = "control unchanged";
        }
    }));
    Dom_byId("btnCtrlDl").addEventListener("click", ((_arg_42) => {
        if (control == null) {
            App_statusText.textContent = "no control file - click New first";
        }
        else {
            const json = ControlFileModule_toJson(control);
            const blob_2 = new Blob([json], { type: 'application/json' });
            const url_2 = URL.createObjectURL(blob_2);
            const a_5 = Dom_el("a");
            a_5.href = url_2;
            a_5.download = "control.json";
            a_5.click();
            App_statusText.textContent = "control.json downloaded";
        }
    }));
    Dom_byId("btnCtrlImport").addEventListener("click", ((_arg_43) => {
        document.getElementById('ctrlFileInput').click();
    }));
    Dom_byId("ctrlFileInput").addEventListener("change", ((e_17) => {
        const file = e_17.target.files[0];
        if (!Operators_IsNull(file)) {
            const reader = new FileReader();
            reader.onload = ((_arg_44) => {
                const text_2 = reader.result;
                try {
                    control = ControlFileModule_fromJson(text_2);
                    cmRender();
                    App_statusText.textContent = "control file imported";
                }
                catch (ex_4) {
                    App_statusText.textContent = ("import failed: " + ex_4.message);
                }
            });
            reader.readAsText(file);
        }
    }));
    const local_2 = Dom_window.localStorage;
    let matchValue_17;
    try {
        matchValue_17 = (local_2.getItem(controlKey()));
    }
    catch (matchValue_16) {
        matchValue_17 = defaultOf();
    }
    if (equals(matchValue_17, defaultOf())) {
        let path_1;
        const arg_79 = defaultArg(App_currentProjectId(), "minimal");
        path_1 = toText(printf("games/%s/control.json"))(arg_79);
        const p_2 = fetch(path_1).then(function (r) { return r.ok ? r.text() : null }).catch(function () { return null });
        p_2.then((txt_4) => {
            let arg_78;
            if (!Operators_IsNull(txt_4) && (control == null)) {
                try {
                    control = ControlFileModule_fromJson(toString(txt_4));
                    cmRender();
                    App_statusText.textContent = ((arg_78 = defaultArg(App_currentProjectId(), "minimal"), toText(printf("control file loaded from games/%s/control.json"))(arg_78)));
                }
                catch (ex_5) {
                    App_statusText.textContent = ("stored control.json ignored: " + ex_5.message);
                }
            }
        });
    }
    else {
        try {
            control = ControlFileModule_fromJson(toString(matchValue_17));
            cmRender();
        }
        catch (ex_6) {
            App_statusText.textContent = ("stored control ignored: " + ex_6.message);
        }
    }
}

