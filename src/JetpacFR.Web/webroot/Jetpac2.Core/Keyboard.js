
import { setItem, item, fill, initialize } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { class_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { empty, singleton, collect, delay, toList } from "../fable_modules/fable-library-js.5.13.0/Seq.js";
import { rangeDouble } from "../fable_modules/fable-library-js.5.13.0/Range.js";

/**
 * Port of JetpacFSharp (Jetpac.Core) Keyboard — specbolt's keyboard matrix
 * (peripherals/Keyboard.cpp). Only the matrix and its port decode; the
 * keycode table belongs to the shells.
 */
export class Keyboard {
    constructor() {
        this.keys = initialize(8, (_arg) => fill(new Array(5), 0, 5, false));
    }
}

export function Keyboard_$reflection() {
    return class_type("Jetpac2.Core.Keyboard", undefined, Keyboard);
}

export function Keyboard_$ctor() {
    return new Keyboard();
}

export function Keyboard__SetKey_289F56A(this$, row, bit, pressed) {
    item(row, this$.keys)[bit] = pressed;
}

export function Keyboard__GetKey_Z37302880(this$, row, bit) {
    return item(bit, item(row, this$.keys));
}

/**
 * Currently pressed cells, e.g. for a controlled teardown (replay end).
 */
export function Keyboard__PressedCells(this$) {
    return toList(delay(() => collect((row) => collect((bit) => (item(bit, item(row, this$.keys)) ? singleton([row, bit]) : empty()), rangeDouble(0, 1, 4)), rangeDouble(0, 1, 7))));
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

/**
 * The 8 half-row bytes (pressed bits inverted: 0 = pressed), the IN form.
 */
export function Keyboard__ToBytes(this$) {
    const k = new Uint8Array(8);
    for (let row = 0; row <= 7; row++) {
        let bits = 0;
        for (let bit = 0; bit <= 4; bit++) {
            if (item(bit, item(row, this$.keys))) {
                bits = ((bits | (1 << bit)) | 0);
            }
        }
        setItem(k, row, (255 & ~bits) & 0xFF);
    }
    return k;
}

/**
 * Apply half-row bytes captured by `ToBytes`.
 */
export function Keyboard__Load_Z3F6BC7B1(this$, bytes) {
    for (let row = 0; row <= 7; row++) {
        for (let bit = 0; bit <= 4; bit++) {
            Keyboard__SetKey_289F56A(this$, row, bit, ((~~item(row, bytes) >> bit) & 1) === 0);
        }
    }
}

