
import { item, fill, initialize } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { class_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";

/**
 * Port of specbolt's keyboard matrix (peripherals/Keyboard.cpp). Only the
 * matrix and its port decode; the SDL keycode table belongs to the shells.
 */
export class Keyboard {
    constructor() {
        this.keys = initialize(8, (_arg) => fill(new Array(5), 0, 5, false));
    }
}

export function Keyboard_$reflection() {
    return class_type("Jetpac.Core.Keyboard", undefined, Keyboard);
}

export function Keyboard_$ctor() {
    return new Keyboard();
}

export function Keyboard__SetKey_289F56A(this$, row, bit, pressed) {
    item(row, this$.keys)[bit] = pressed;
}

/**
 * Keyboard responds to any even address.
 */
export function Keyboard__In_Z524259A4(this$, address) {
    if ((address & 1) === 1) {
        return undefined;
    }
    else {
        let value = 255;
        const res = ((address >> 8) & 255) | 0;
        for (let row = 0; row <= 7; row++) {
            if ((res & (1 << row)) === 0) {
                for (let bit = 0; bit <= 4; bit++) {
                    if (item(bit, item(row, this$.keys))) {
                        value = ((value & ~(1 << bit)) | 0);
                    }
                }
            }
        }
        return value;
    }
}

