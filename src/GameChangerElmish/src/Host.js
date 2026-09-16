
import { equals, compare, compareArrays, disposeSafe, getEnumerator, defaultOf, createAtom } from "../fable_modules/fable-library-js.5.17.2/Util.js";
import { Operators_IsNull } from "../fable_modules/fable-library-js.5.17.2/FSharp.Core.js";
import { setItem, item } from "../fable_modules/fable-library-js.5.17.2/Array.js";
import { toString, Record, Union } from "../fable_modules/fable-library-js.5.17.2/Types.js";
import { record_type, list_type, array_type, uint8_type, int32_type, string_type, union_type } from "../fable_modules/fable-library-js.5.17.2/Reflection.js";
import { isEmpty, head, tryFind, tryHead, singleton, ofArray } from "../fable_modules/fable-library-js.5.17.2/List.js";
import { Z80Op_$reflection } from "../Jetpac2.Core/Z80Asm.js";
import { entryState, program, image, memory } from "../games/uridium/WebImage.js";
import { GameImage, GameRegistry_register } from "../JetpacFR.Core/GameRegistry.js";
import { remove, add, FSharpSet__Contains, ofList, union, fold, empty } from "../fable_modules/fable-library-js.5.17.2/Set.js";
import { substring, printf, toText } from "../fable_modules/fable-library-js.5.17.2/String.js";
import { CEGame_$ctor_20C827FE, CEParity_check, CEGame__SetKey_289F56A, CEGame__DrainBeeperSamples_Z524259C1, CEGame__get_ScreenBuffer, CEGame__RunFrame } from "../JetpacFR.Core/CEGame.js";

export const Dom_window = window;

export const Dom_document = document;

export function Dom_byId(id) {
    return document.getElementById(id);
}

export function Dom_setTimeout(f, ms) {
    return setTimeout(f, ms);
}

export function Dom_setInterval(f, ms) {
    return setInterval(f, ms);
}

export function Dom_clearInterval(id) {
    clearInterval(id);
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

export class KeyMap_Dir extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
    }
    static N = new KeyMap_Dir(0, []);
    static NE = new KeyMap_Dir(1, []);
    static E = new KeyMap_Dir(2, []);
    static SE = new KeyMap_Dir(3, []);
    static S = new KeyMap_Dir(4, []);
    static SW = new KeyMap_Dir(5, []);
    static W = new KeyMap_Dir(6, []);
    static NW = new KeyMap_Dir(7, []);
}

export function KeyMap_Dir_$reflection() {
    return union_type("GameChangerElmish.Host.KeyMap.Dir", [], KeyMap_Dir, () => [[], [], [], [], [], [], [], []]);
}

export const KeyMap_all = ofArray([KeyMap_Dir.N, KeyMap_Dir.NE, KeyMap_Dir.E, KeyMap_Dir.SE, KeyMap_Dir.S, KeyMap_Dir.SW, KeyMap_Dir.W, KeyMap_Dir.NW]);

/**
 * Matrix cells (row, bit) a direction holds down. Diagonals resolve to
 * their vertical/horizontal component.
 */
export function KeyMap_dirCells(d) {
    switch (d.tag) {
        case 4:
        case 3:
        case 5:
            return ofArray([[0, 0], [4, 4]]);
        case 6:
            return ofArray([[0, 0], [3, 4]]);
        case 2:
            return ofArray([[0, 0], [4, 2]]);
        default:
            return ofArray([[0, 0], [4, 3]]);
    }
}

export const KeyMap_fireCells = singleton([4, 0]);

export class Games_GameDef extends Record {
    constructor(Id, Name, BaseAddress, Memory, Image, Program, EntryState) {
        super();
        this.Id = Id;
        this.Name = Name;
        this.BaseAddress = (BaseAddress | 0);
        this.Memory = Memory;
        this.Image = Image;
        this.Program = Program;
        this.EntryState = EntryState;
    }
}

export function Games_GameDef_$reflection() {
    return record_type("GameChangerElmish.Host.Games.GameDef", [], Games_GameDef, () => [["Id", string_type], ["Name", string_type], ["BaseAddress", int32_type], ["Memory", array_type(uint8_type)], ["Image", array_type(uint8_type)], ["Program", list_type(Z80Op_$reflection())], ["EntryState", string_type]]);
}

export const Games_uridium = new Games_GameDef("uridium", "Uridium", 16384, memory, image, program, entryState);

export const Games_all = singleton(Games_uridium);

export function Games_registerAll() {
    const enumerator = getEnumerator(Games_all);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const g = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            GameRegistry_register(new GameImage(g.Id, g.Name, g.Memory, g.BaseAddress, g.Program, g.EntryState));
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

let Emulator_game = undefined;

let Emulator_timerId = defaultOf();

let Emulator_ctx = defaultOf();

let Emulator_img = defaultOf();

let Emulator_running = false;

let Emulator_statusText = "";

let Emulator_held = empty({
    Compare: (x, y) => (compareArrays(x, y) | 0),
});

let Emulator_frames = 0;

function Emulator_log(msg) {
    ((window.__log = window.__log || []).push(msg));
}

/**
 * Frames run since boot (liveness probe).
 */
export function Emulator_Frames() {
    return Emulator_frames | 0;
}

/**
 * Start (or stop) the 20 ms frame loop (RunFrame + paint + beeper).
 */
export function Emulator_setRunning(on) {
    let arg_1;
    Emulator_log((arg_1 = Emulator_running, toText(printf("setRunning %b (was %b)"))(on)(arg_1)));
    if (on && !Emulator_running) {
        Emulator_running = true;
        Emulator_timerId = Dom_setInterval(() => {
            if (Emulator_game == null) {
            }
            else {
                const g = Emulator_game;
                Emulator_frames = ((Emulator_frames + 1) | 0);
                const patternInput = CEGame__RunFrame(g);
                if (!Operators_IsNull(Emulator_ctx)) {
                    const src = CEGame__get_ScreenBuffer(g);
                    const buf = Emulator_img.data;
                    for (let i = 0; i <= ((320 * 256) - 1); i++) {
                        const o = (i * 4) | 0;
                        buf[o]=item(o + 2, src);
                        buf[(o + 1)]=item(o + 1, src);
                        buf[(o + 2)]=item(o, src);
                        buf[(o + 3)]=item(o + 3, src);
                    }
                    Emulator_ctx.putImageData(Emulator_img, 0, 0);
                }
                Audio_Play(CEGame__DrainBeeperSamples_Z524259C1(g, patternInput[0]));
            }
        }, 20);
    }
    else if (!on && Emulator_running) {
        Emulator_running = false;
        Dom_clearInterval(Emulator_timerId);
    }
}

export const Emulator_Running = Emulator_running;

/**
 * Wanted key state from the controls; diff against what is held.
 */
export function Emulator_applyKeys(dirs, fire) {
    let wanted;
    const acc_1 = fold((acc, d) => union(acc, ofList(KeyMap_dirCells(d), {
        Compare: (x_1, y_1) => (compareArrays(x_1, y_1) | 0),
    })), empty({
        Compare: (x, y) => (compareArrays(x, y) | 0),
    }), dirs);
    wanted = (fire ? union(acc_1, ofList(KeyMap_fireCells, {
        Compare: (x_2, y_2) => (compareArrays(x_2, y_2) | 0),
    })) : acc_1);
    if (Emulator_game != null) {
        const g = Emulator_game;
        const enumerator = getEnumerator(Emulator_held);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                const c = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
                if (!FSharpSet__Contains(wanted, c)) {
                    CEGame__SetKey_289F56A(g, c[0], c[1], false);
                }
            }
        }
        finally {
            disposeSafe(enumerator);
        }
        const enumerator_1 = getEnumerator(wanted);
        try {
            while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
                const c_1 = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]();
                if (!FSharpSet__Contains(Emulator_held, c_1)) {
                    CEGame__SetKey_289F56A(g, c_1[0], c_1[1], true);
                }
            }
        }
        finally {
            disposeSafe(enumerator_1);
        }
        Emulator_held = wanted;
    }
}

let Emulator_dirsFromKeys = empty({
    Compare: (x, y) => (compare(x, y) | 0),
});

let Emulator_fireFromKeys = false;

function Emulator_syncKeys() {
    Emulator_applyKeys(Emulator_dirsFromKeys, Emulator_fireFromKeys);
}

function Emulator_keyDir(key) {
    switch (key) {
        case "ArrowUp":
            return KeyMap_Dir.N;
        case "ArrowDown":
            return KeyMap_Dir.S;
        case "ArrowLeft":
            return KeyMap_Dir.W;
        case "ArrowRight":
            return KeyMap_Dir.E;
        default:
            return undefined;
    }
}

function Emulator_isEditableTarget(e) {
    const t = e.target;
    const tag = toString(t.tagName);
    if (((tag === "INPUT") ? true : (tag === "TEXTAREA")) ? true : (tag === "SELECT")) {
        return true;
    }
    else {
        return toString(t.isContentEditable) === "true";
    }
}

function Emulator_installKeyboardFallback() {
    Dom_window.addEventListener("keydown", ((e) => {
        if (Emulator_isEditableTarget(e)) {
        }
        else {
            const key = e.key;
            const matchValue = Emulator_keyDir(key);
            if (matchValue == null) {
                if (key === " ") {
                    Emulator_fireFromKeys = true;
                    Emulator_syncKeys();
                }
            }
            else {
                const d = matchValue;
                Emulator_dirsFromKeys = add(d, Emulator_dirsFromKeys);
                Emulator_syncKeys();
            }
        }
    }));
    return Dom_window.addEventListener("keyup", ((e_1) => {
        if (Emulator_isEditableTarget(e_1)) {
        }
        else {
            const key_1 = e_1.key;
            const matchValue_1 = Emulator_keyDir(key_1);
            if (matchValue_1 == null) {
                if (key_1 === " ") {
                    Emulator_fireFromKeys = false;
                    Emulator_syncKeys();
                }
            }
            else {
                const d_1 = matchValue_1;
                Emulator_dirsFromKeys = remove(d_1, Emulator_dirsFromKeys);
                Emulator_syncKeys();
            }
        }
    }));
}

/**
 * Boot the emulator host against a canvas already in the DOM. `?game=<id>`
 * selects a registered game; default: the first one. Returns a status
 * line with the game name and its CE parity result.
 */
export function Emulator_start(canvasId) {
    Games_registerAll();
    Emulator_installKeyboardFallback();
    let search;
    const matchValue = Dom_window.location.search;
    search = (equals(matchValue, defaultOf()) ? "" : toString(matchValue));
    const wanted = search.startsWith("?game=") ? substring(search, 6).toLowerCase() : "";
    let selected;
    const option_1 = (wanted === "") ? tryHead(Games_all) : tryFind((g) => (g.Id === wanted), Games_all);
    selected = ((option_1 != null) ? option_1 : head(Games_all));
    const canvas = Dom_byId(canvasId);
    Emulator_ctx = (canvas.getContext("2d"));
    Emulator_img = (new ImageData(320, 256));
    const patternInput = CEParity_check(selected.Program, selected.Image);
    const total = patternInput[1] | 0;
    const matching = patternInput[0] | 0;
    const divergences = patternInput[2];
    Emulator_statusText = (isEmpty(divergences) ? toText(printf("%s - CE parity: %d/%d bytes match"))(selected.Name)(matching)(total) : toText(printf("%s - CE parity: %d/%d, diverges at %A"))(selected.Name)(matching)(total)(divergences));
    Emulator_game = CEGame_$ctor_20C827FE(selected.Program, selected.Memory, selected.EntryState);
    Emulator_setRunning(true);
    Emulator_log(toText(printf("start: %s (parity %d/%d)"))(selected.Name)(matching)(total));
    const status = Dom_byId("statusLine");
    if (!Operators_IsNull(status)) {
        status.textContent = Emulator_statusText;
    }
    return Emulator_statusText;
}

