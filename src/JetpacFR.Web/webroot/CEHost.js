
import { equals, defaultOf } from "./fable_modules/fable-library-js.5.17.0/Util.js";
import { iterate, isEmpty, tryHead, empty, ofArray, singleton } from "./fable_modules/fable-library-js.5.17.0/List.js";
import { CEGame__SetKey_289F56A, CEGame__get_Frame, CEGame__DrainBeeperSamples_Z524259C1, CEGame__RunFrame, CEGame_$ctor_20C827FE, CEGame__get_ScreenBuffer } from "./JetpacFR.Core/CEGame.js";
import { item } from "./fable_modules/fable-library-js.5.17.0/Array.js";
import { Audio_Play, Dom_setInterval, Dom_window, Dom_byId } from "./App.js";
import { Operators_IsNull } from "./fable_modules/fable-library-js.5.17.0/FSharp.Core.js";
import { entryState } from "./games/minimal/Game.js";
import { program, binary } from "./games/minimal/Image.js";
import { GameRegistry_parity, GameRegistry_tryFind, GameRegistry_all, GameImage, GameRegistry_register } from "./JetpacFR.Core/GameRegistry.js";
import { toString } from "./fable_modules/fable-library-js.5.17.0/Types.js";
import { printf, toText, substring } from "./fable_modules/fable-library-js.5.17.0/String.js";

let game = undefined;

let running = false;

let timerId = defaultOf();

let ctx = defaultOf();

let img = defaultOf();

function keyCells(key) {
    switch (key) {
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
        case "6":
            return singleton([4, 4]);
        case "7":
            return singleton([4, 3]);
        case "8":
            return singleton([4, 2]);
        case "9":
            return singleton([4, 1]);
        case "0":
            return singleton([4, 0]);
        case " ":
            return singleton([7, 0]);
        case "Shift":
            return singleton([0, 0]);
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

function paint() {
    if (game == null) {
    }
    else {
        const src = CEGame__get_ScreenBuffer(game);
        const buf = img.data;
        for (let i = 0; i <= ((320 * 256) - 1); i++) {
            const o = (i * 4) | 0;
            buf[o]=item(o + 2, src);
            buf[(o + 1)]=item(o + 1, src);
            buf[(o + 2)]=item(o, src);
            buf[(o + 3)]=item(o + 3, src);
        }
        ctx.putImageData(img, 0, 0);
    }
}

export function start() {
    const canvas = Dom_byId("ceScreen");
    if (Operators_IsNull(canvas)) {
    }
    else {
        ctx = (canvas.getContext("2d"));
        img = (new ImageData(320, 256));
        const btn = Dom_byId("ceBtn");
        const status = Dom_byId("ceStatus");
        const patternInput = entryState(binary);
        const state = patternInput[1];
        const mem = patternInput[0];
        GameRegistry_register(new GameImage("minimal", "Minimal", mem, 32768, program, state));
        let search;
        const matchValue = Dom_window.location.search;
        search = (equals(matchValue, defaultOf()) ? "" : toString(matchValue));
        const wanted = search.startsWith("?game=") ? substring(search, 6).toLowerCase() : "";
        let selected;
        const option_1 = (wanted === "") ? tryHead(GameRegistry_all()) : GameRegistry_tryFind(wanted);
        selected = ((option_1 != null) ? option_1 : (new GameImage("minimal", "Minimal", mem, 32768, program, state)));
        const patternInput_1 = GameRegistry_parity(selected);
        const total = patternInput_1[1] | 0;
        const matching = patternInput_1[0] | 0;
        const divergences = patternInput_1[2];
        const parity = isEmpty(divergences) ? toText(printf("CE parity: %d/%d bytes match"))(matching)(total) : toText(printf("CE parity: %d/%d - diverges at %A"))(matching)(total)(divergences);
        game = CEGame_$ctor_20C827FE(selected.Program, selected.Memory, selected.EntryState);
        status.textContent = toText(printf("CE engine - frame 0, %s"))(parity);
        btn.onclick = ((_arg) => {
            running = !running;
            if (running) {
                btn.textContent = "Pause CE";
                timerId = Dom_setInterval(() => {
                    let arg_6;
                    if (game == null) {
                    }
                    else {
                        const g = game;
                        const patternInput_2 = CEGame__RunFrame(g);
                        paint();
                        Audio_Play(CEGame__DrainBeeperSamples_Z524259C1(g, patternInput_2[0]));
                        status.textContent = ((arg_6 = (CEGame__get_Frame(g) | 0), toText(printf("CE engine - frame %d, %s"))(arg_6)(parity)));
                    }
                }, 20);
            }
            else {
                btn.textContent = "Run CE";
                Dom_window.clearInterval(timerId);
            }
        });
        const isEditableTarget = (e) => {
            const t = e.target;
            const tag = toString(t.tagName);
            if (((tag === "INPUT") ? true : (tag === "TEXTAREA")) ? true : (tag === "SELECT")) {
                return true;
            }
            else {
                return toString(t.isContentEditable) === "true";
            }
        };
        Dom_window.addEventListener("keydown", ((e_1) => {
            const key = e_1.key;
            if (isEditableTarget(e_1)) {
            }
            else if (game == null) {
            }
            else {
                const g_1 = game;
                iterate((tupledArg) => {
                    CEGame__SetKey_289F56A(g_1, tupledArg[0], tupledArg[1], true);
                }, keyCells(key));
            }
        }));
        Dom_window.addEventListener("keyup", ((e_2) => {
            if (isEditableTarget(e_2)) {
            }
            else {
                const key_1 = e_2.key;
                if (game == null) {
                }
                else {
                    const g_2 = game;
                    iterate((tupledArg_1) => {
                        CEGame__SetKey_289F56A(g_2, tupledArg_1[0], tupledArg_1[1], false);
                    }, keyCells(key_1));
                }
            }
        }));
    }
}

