
import { Z80_runFrame, Z80_ExecutionIndex__Attach_3EE36980 } from "../../Jetpac2.Core/Z80Asm.js";
import { index } from "./Image.js";
import { copyTo } from "../../fable_modules/fable-library-js.5.17.0/Array.js";
import { min } from "../../fable_modules/fable-library-js.5.17.0/Double.js";

/**
 * The per-frame CE driver (69888 T-states). CE ops dispatch by PC from the
 * program index; raw blocks and anything outside the CE layout fall through
 * to the interpreter. Returns (frameStart, frameEnd) for audio.
 */
export function runFrame(m) {
    Z80_ExecutionIndex__Attach_3EE36980(index, m);
    return Z80_runFrame(index, m);
}

/**
 * Entry state: the image at 0x8000, PC=0x8000, SP=0xFFFE, interrupts
 * off. Pure (no I/O), so shells and tests share it.
 */
export function entryState(image) {
    const mem = new Uint8Array(65536);
    copyTo(image, 0, mem, 32768, min(image.length, 65536 - 32768));
    const state = "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=FFFE\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n";
    return [mem, state];
}

