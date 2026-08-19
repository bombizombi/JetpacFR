
import { Z80_runFrame, Z80_ExecutionIndex__Attach_3EE36980, Z80_makeIndexAt, Z80_assemble, Z80BuilderInstance_z80, Z80Builder__YieldFrom_Z3F6BC7B1, Z80Builder__Delay_535E0280 } from "../../Jetpac2.Core/Z80Asm.js";
import { copyTo } from "../../fable_modules/fable-library-js.5.13.0/Array.js";
import { min } from "../../fable_modules/fable-library-js.5.13.0/Double.js";

export const binary = new Uint8Array([62, 5, 211, 254, 243, 49, 255, 255, 205, 90, 128, 205, 104, 128, 62, 223, 219, 254, 203, 71, 40, 6, 203, 79, 40, 7, 24, 242, 205, 53, 128, 24, 237, 1, 24, 2, 205, 41, 128, 24, 229, 237, 67, 45, 128, 118, 0, 0, 0, 205, 72, 128, 201, 205, 90, 128, 42, 139, 128, 125, 254, 31, 40, 4, 44, 34, 139, 128, 205, 104, 128, 201, 205, 90, 128, 42, 139, 128, 125, 183, 40, 4, 45, 34, 139, 128, 205, 104, 128, 201, 33, 0, 64, 17, 1, 64, 1, 255, 23, 175, 119, 237, 176, 201, 42, 139, 128, 62, 60, 119, 36, 62, 126, 119, 36, 62, 255, 119, 36, 62, 219, 119, 36, 62, 255, 119, 36, 62, 126, 119, 36, 62, 60, 119, 36, 62, 0, 119, 201, 15, 72, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

export const program = Z80Builder__Delay_535E0280(Z80BuilderInstance_z80, () => Z80Builder__YieldFrom_Z3F6BC7B1(Z80BuilderInstance_z80, binary));

export const assembled = Z80_assemble(program);

export const index = Z80_makeIndexAt(32768, program);

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

