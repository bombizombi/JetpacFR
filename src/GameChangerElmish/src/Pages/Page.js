
import { Union, Record } from "../../fable_modules/fable-library-js.5.17.2/Types.js";
import { KeyMap_Dir, Audio_SetEnabled, Emulator_setRunning, Emulator_applyKeys, Emulator_start, Dom_setTimeout, KeyMap_Dir_$reflection } from "../Host.js";
import { union_type, record_type, bool_type, class_type } from "../../fable_modules/fable-library-js.5.17.2/Reflection.js";
import { Msg_$reflection as Msg_$reflection_1 } from "./Layout.js";
import { FSharpSet__Contains, singleton, empty } from "../../fable_modules/fable-library-js.5.17.2/Set.js";
import { compare } from "../../fable_modules/fable-library-js.5.17.2/Util.js";
import { Command_none } from "../../.elmish-land/Base/Command.js";
import { min, max } from "../../fable_modules/fable-library-js.5.17.2/Double.js";
import { HtmlHelper_createElement } from "../../fable_modules/Feliz.3.3.3/Html.fs.js";
import { empty as empty_1, singleton as singleton_1, append, delay, toList } from "../../fable_modules/fable-library-js.5.17.2/Seq.js";
import { printf, toText, join } from "../../fable_modules/fable-library-js.5.17.2/String.js";
import { value as value_37 } from "../../fable_modules/fable-library-js.5.17.2/Option.js";
import { ofArray, mapIndexed, collect, singleton as singleton_2 } from "../../fable_modules/fable-library-js.5.17.2/List.js";
import { Page_from } from "../../.elmish-land/Base/Page.js";

export class Model extends Record {
    constructor(Dirs, Fire, Running, Muted) {
        super();
        this.Dirs = Dirs;
        this.Fire = Fire;
        this.Running = Running;
        this.Muted = Muted;
    }
}

export function Model_$reflection() {
    return record_type("GameChangerElmish.Pages.Page.Model", [], Model, () => [["Dirs", class_type("Microsoft.FSharp.Collections.FSharpSet`1", [KeyMap_Dir_$reflection()])], ["Fire", bool_type], ["Running", bool_type], ["Muted", bool_type]]);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["LayoutMsg", "DirsChange", "FireChange", "ToggleRun", "ToggleMute"];
    }
    static ToggleRun = new Msg(3, []);
    static ToggleMute = new Msg(4, []);
}

export function Msg_$reflection() {
    return union_type("GameChangerElmish.Pages.Page.Msg", [], Msg, () => [[["Item", Msg_$reflection_1()]], [["Item", class_type("Microsoft.FSharp.Collections.FSharpSet`1", [KeyMap_Dir_$reflection()])]], [["Item", bool_type]], [], []]);
}

export function init() {
    Dom_setTimeout(() => {
        Emulator_start("gameScreen");
    }, 30);
    return [new Model(empty({
        Compare: (x, y) => (compare(x, y) | 0),
    }), false, true, true), Command_none()];
}

export function update(msg, model) {
    switch (msg.tag) {
        case 1: {
            const dirs = msg.fields[0];
            Emulator_applyKeys(dirs, model.Fire);
            return [new Model(dirs, model.Fire, model.Running, model.Muted), Command_none()];
        }
        case 2: {
            const fire = msg.fields[0];
            Emulator_applyKeys(model.Dirs, fire);
            return [new Model(model.Dirs, fire, model.Running, model.Muted), Command_none()];
        }
        case 3: {
            Emulator_setRunning(!model.Running);
            return [new Model(model.Dirs, model.Fire, !model.Running, model.Muted), Command_none()];
        }
        case 4: {
            Audio_SetEnabled(model.Muted);
            return [new Model(model.Dirs, model.Fire, model.Running, !model.Muted), Command_none()];
        }
        default:
            return [model, Command_none()];
    }
}

let keypadPid = undefined;

let firePid = undefined;

function pointerId(e) {
    return e.pointerId | 0;
}

function capture(e) {
    try {
        e.currentTarget.setPointerCapture(e.pointerId);
    }
    catch (matchValue) {
    }
}

function samePointer(pid, e) {
    if (pid == null) {
        return false;
    }
    else {
        return pid === pointerId(e);
    }
}

function dirAt(e) {
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    const w = rect.width;
    const h = rect.height;
    const clamp = (v) => (max(0, min(2, v)) | 0);
    const col = clamp(~~((x / w) * 3)) | 0;
    const row = clamp(~~((y / h) * 3)) | 0;
    let matchResult;
    switch (row) {
        case 0: {
            switch (col) {
                case 0: {
                    matchResult = 0;
                    break;
                }
                case 1: {
                    matchResult = 1;
                    break;
                }
                case 2: {
                    matchResult = 2;
                    break;
                }
                default:
                    matchResult = 9;
            }
            break;
        }
        case 1: {
            switch (col) {
                case 0: {
                    matchResult = 3;
                    break;
                }
                case 1: {
                    matchResult = 4;
                    break;
                }
                case 2: {
                    matchResult = 5;
                    break;
                }
                default:
                    matchResult = 9;
            }
            break;
        }
        case 2: {
            switch (col) {
                case 0: {
                    matchResult = 6;
                    break;
                }
                case 1: {
                    matchResult = 7;
                    break;
                }
                case 2: {
                    matchResult = 8;
                    break;
                }
                default:
                    matchResult = 9;
            }
            break;
        }
        default:
            matchResult = 9;
    }
    switch (matchResult) {
        case 0:
            return KeyMap_Dir.NW;
        case 1:
            return KeyMap_Dir.N;
        case 2:
            return KeyMap_Dir.NE;
        case 3:
            return KeyMap_Dir.W;
        case 4:
            return undefined;
        case 5:
            return KeyMap_Dir.E;
        case 6:
            return KeyMap_Dir.SW;
        case 7:
            return KeyMap_Dir.S;
        case 8:
            return KeyMap_Dir.SE;
        default:
            return undefined;
    }
}

function keypadDown(dispatch, e) {
    let _arg;
    e.preventDefault();
    capture(e);
    keypadPid = pointerId(e);
    dispatch(new Msg(/* DirsChange */ 1, [(_arg = dirAt(e), (_arg == null) ? empty({
        Compare: (x_1, y_1) => (compare(x_1, y_1) | 0),
    }) : singleton(_arg, {
        Compare: (x, y) => (compare(x, y) | 0),
    }))]));
}

function keypadMove(dispatch, e) {
    let _arg;
    if (samePointer(keypadPid, e)) {
        e.preventDefault();
        dispatch(new Msg(/* DirsChange */ 1, [(_arg = dirAt(e), (_arg == null) ? empty({
            Compare: (x_1, y_1) => (compare(x_1, y_1) | 0),
        }) : singleton(_arg, {
            Compare: (x, y) => (compare(x, y) | 0),
        }))]));
    }
}

function keypadUp(dispatch, e) {
    if (samePointer(keypadPid, e)) {
        keypadPid = undefined;
        dispatch(new Msg(/* DirsChange */ 1, [empty({
            Compare: (x, y) => (compare(x, y) | 0),
        })]));
    }
}

function fireDown(dispatch, e) {
    e.preventDefault();
    capture(e);
    firePid = pointerId(e);
    dispatch(new Msg(/* FireChange */ 2, [true]));
}

function fireUp(dispatch, e) {
    if (samePointer(firePid, e)) {
        firePid = undefined;
        dispatch(new Msg(/* FireChange */ 2, [false]));
    }
}

function dirName(d) {
    switch (d.tag) {
        case 1:
            return "↗";
        case 2:
            return "→";
        case 3:
            return "↘";
        case 4:
            return "↓";
        case 5:
            return "↙";
        case 6:
            return "←";
        case 7:
            return "↖";
        default:
            return "↑";
    }
}

function cell(model, row, col, dir) {
    const active = (dir == null) ? false : FSharpSet__Contains(model.Dirs, dir);
    return HtmlHelper_createElement("div", toList(delay(() => append(singleton_1(["className", join(" ", toList(delay(() => append(singleton_1("cell"), delay(() => append(active ? singleton_1("on") : empty_1(), delay(() => ((dir == null) ? singleton_1("center") : empty_1()))))))))]), delay(() => append(singleton_1(["key", toText(printf("cell-%d-%d"))(row)(col)]), delay(() => ((dir != null) ? singleton_1(["children", singleton_2(dirName(value_37(dir)))]) : empty_1()))))))));
}

function keypad(model, dispatch) {
    return HtmlHelper_createElement("div", ofArray([["className", "keypad"], ["onPointerDown", (e) => {
        keypadDown(dispatch, e);
    }], ["onPointerMove", (e_1) => {
        keypadMove(dispatch, e_1);
    }], ["onPointerUp", (e_2) => {
        keypadUp(dispatch, e_2);
    }], ["onPointerCancel", (e_3) => {
        keypadUp(dispatch, e_3);
    }], ["children", collect((x) => x, mapIndexed((r, row) => mapIndexed((c, dir) => cell(model, r, c, dir), row), ofArray([ofArray([KeyMap_Dir.NW, KeyMap_Dir.N, KeyMap_Dir.NE]), ofArray([KeyMap_Dir.W, undefined, KeyMap_Dir.E]), ofArray([KeyMap_Dir.SW, KeyMap_Dir.S, KeyMap_Dir.SE])])))]]));
}

function fireButton(model, dispatch) {
    return HtmlHelper_createElement("div", ofArray([["className", join(" ", toList(delay(() => append(singleton_1("fire"), delay(() => (model.Fire ? singleton_1("on") : empty_1()))))))], ["children", singleton_2("FIRE")], ["onPointerDown", (e) => {
        fireDown(dispatch, e);
    }], ["onPointerUp", (e_1) => {
        fireUp(dispatch, e_1);
    }], ["onPointerCancel", (e_2) => {
        fireUp(dispatch, e_2);
    }]]));
}

export function view(model, dispatch) {
    return HtmlHelper_createElement("div", ofArray([["className", "app"], ["children", [HtmlHelper_createElement("div", ofArray([["className", "topbar"], ["children", [HtmlHelper_createElement("span", ofArray([["className", "title"], ["children", singleton_2("GAME CHANGER")]])), HtmlHelper_createElement("button", ofArray([["className", "tbtn"], ["children", singleton_2(model.Running ? "Pause" : "Run")], ["onClick", (_arg) => {
        dispatch(Msg.ToggleRun);
    }]])), HtmlHelper_createElement("button", ofArray([["className", "tbtn"], ["children", singleton_2(model.Muted ? "Sound off" : "Sound on")], ["onClick", (_arg_1) => {
        dispatch(Msg.ToggleMute);
    }]]))]]])), HtmlHelper_createElement("canvas", ofArray([["id", "gameScreen"], ["className", "screen"], ["width", 320], ["height", 256]])), HtmlHelper_createElement("div", ofArray([["className", "statusbar"], ["id", "statusLine"], ["children", singleton_2("booting emulator...")]])), HtmlHelper_createElement("div", ofArray([["className", "controls"], ["children", [fireButton(model, dispatch), keypad(model, dispatch)]]]))]]]));
}

export function page(_shared, _route) {
    return Page_from(init, update, view, undefined, (Item) => (new Msg(/* LayoutMsg */ 0, [Item])));
}

