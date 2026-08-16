
import { empty, singleton, append, collect, delay, toList } from "./fable_modules/fable-library-js.5.13.0/Seq.js";

export function defaultSession(framesN) {
    return toList(delay(() => collect((matchValue) => {
        const row = matchValue[2][0] | 0;
        const release = matchValue[1] | 0;
        const bit = matchValue[2][1] | 0;
        return (release <= framesN) ? append(singleton([matchValue[0], row, bit, true]), delay(() => singleton([release, row, bit, false]))) : empty();
    }, [[10, 30, [3, 0]], [35, 50, [3, 4]], [90, 110, [3, 4]], [120, 135, [4, 2]], [160, 165, [7, 0]], [200, 220, [7, 0]], [260, 275, [0, 0]], [300, 315, [0, 0]], [350, 365, [4, 4]], [400, 420, [7, 0]]])));
}

