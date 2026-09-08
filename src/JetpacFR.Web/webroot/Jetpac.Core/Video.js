
import { Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { class_type, record_type, array_type, int32_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { setItem, item, initialize } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { fromInt32, compare, op_Addition, toUInt64_unchecked } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { Memory__RawRead_Z37302880 } from "./Memory.js";

export const VideoConstants_XBorder = 32;

export const VideoConstants_YBorder = 32;

export const VideoConstants_ScreenWidth = 256;

export const VideoConstants_ScreenHeight = 192;

export const VideoConstants_VisibleWidth = 320;

export const VideoConstants_VisibleHeight = 256;

export const VideoConstants_ColumnCount = 32;

export const VideoConstants_CyclesPerScanLine = 224;

export const VideoConstants_PalTotalLines = 312;

export const VideoConstants_VSyncLines = 56;

export const VideoConstants_FramesPerFlash = 16;

export const VideoConstants_AttributeDataOffset = 6144;

export const VideoConstants_Palette = new Int32Array([-16777216, -16777011, -3342336, -3342131, -16724736, -16724531, -3289856, -3289651, -16777216, -16776961, -65536, -65281, -16711936, -16711681, -256, -1]);

class VideoLine extends Record {
    constructor(Border, Pixels, Attrs) {
        super();
        this.Border = (Border | 0);
        this.Pixels = Pixels;
        this.Attrs = Attrs;
    }
}

function VideoLine_$reflection() {
    return record_type("Jetpac.Core.VideoLine", [], VideoLine, () => [["Border", int32_type], ["Pixels", array_type(int32_type)], ["Attrs", array_type(int32_type)]]);
}

/**
 * Port of specbolt's ULA video (peripherals/Video.cpp). The screen is
 * rendered one scanline at a time into a 256-line buffer (only the visible
 * lines need storing); BlitTo turns it into a 320x256 ARGB (B,G,R,A byte
 * order) buffer.
 */
export class Video {
    constructor(memory) {
        this.memory = memory;
        this.border = 3;
        this.page = 1;
        this.totalCycles = (0n);
        this.nextLineCycles = (0n);
        this.currentLine = 0;
        this.flashCounter = 0;
        this.flashOn = false;
        this.lines = initialize(VideoConstants_VisibleHeight, (_arg) => (new VideoLine(3, new Int32Array(VideoConstants_ColumnCount), new Int32Array(VideoConstants_ColumnCount))));
        this.screenBuffer = (new Uint8Array((VideoConstants_VisibleWidth * VideoConstants_VisibleHeight) * 4));
    }
}

export function Video_$reflection() {
    return class_type("Jetpac.Core.Video", undefined, Video);
}

export function Video_$ctor_44195B56(memory) {
    return new Video(memory);
}

export function Video__SetBorder_Z524259A4(this$, v) {
    this$.border = ((v & 7) | 0);
}

export function Video__SetPage_Z524259A4(this$, v) {
    this$.page = (v | 0);
}

/**
 * Reconstruct the scanline/flash phase (used by Spectrum48.LoadState).
 */
export function Video__SetState_289F56A(this$, scanline, flash, flashState) {
    this$.currentLine = (scanline | 0);
    this$.flashCounter = (flash | 0);
    this$.flashOn = flashState;
}

export function Video__Poll_Z6EF827B6(this$, numCycles) {
    let irq = false;
    this$.totalCycles = toUInt64_unchecked(op_Addition(this$.totalCycles, numCycles));
    while (compare(this$.totalCycles, this$.nextLineCycles) > 0) {
        if (Video__NextScanLine(this$)) {
            irq = true;
        }
        this$.nextLineCycles = toUInt64_unchecked(op_Addition(this$.nextLineCycles, toUInt64_unchecked(fromInt32(VideoConstants_CyclesPerScanLine))));
    }
    return irq;
}

export function Video__NextScanLine(this$) {
    Video__RenderLine_Z524259A4(this$, this$.currentLine);
    this$.currentLine = (((this$.currentLine + 1) % VideoConstants_PalTotalLines) | 0);
    if (this$.currentLine === 0) {
        this$.flashCounter = ((this$.flashCounter + 1) | 0);
        if (this$.flashCounter === VideoConstants_FramesPerFlash) {
            this$.flashCounter = 0;
            this$.flashOn = !this$.flashOn;
        }
        return true;
    }
    else {
        return false;
    }
}

function Video__RenderLine_Z524259A4(this$, displayLine) {
    if (displayLine < VideoConstants_VSyncLines) {
    }
    else {
        const line = item(displayLine - VideoConstants_VSyncLines, this$.lines);
        line.Border = (this$.border | 0);
        const y = ((displayLine - VideoConstants_YBorder) - VideoConstants_VSyncLines) | 0;
        if ((y >= VideoConstants_ScreenHeight) ? true : (y < 0)) {
        }
        else {
            const screenOffset = (((((y >> 6) & 3) << 11) + (((y >> 3) & 7) << 5)) + ((y & 7) << 8)) | 0;
            const charRow = ~~(y / 8) | 0;
            for (let x = 0; x <= (VideoConstants_ColumnCount - 1); x++) {
                line.Pixels[x] = (Memory__RawRead_Z37302880(this$.memory, this$.page, screenOffset + x) | 0);
                line.Attrs[x] = (Memory__RawRead_Z37302880(this$.memory, this$.page, (VideoConstants_AttributeDataOffset + (charRow * VideoConstants_ColumnCount)) + x) | 0);
            }
        }
    }
}

/**
 * Fill the reused screen buffer (B,G,R,A byte order) and return it.
 */
export function Video__BlitTo(this$) {
    const pal = VideoConstants_Palette;
    const buf = this$.screenBuffer;
    for (let y = 0; y <= (VideoConstants_VisibleHeight - 1); y++) {
        const line = item(y, this$.lines);
        const fillColor = (color, startX, count) => {
            let i = ((y * VideoConstants_VisibleWidth) + startX) * 4;
            const b = (color & 255) | 0;
            const g = ((color >> 8) & 255) | 0;
            const r = ((color >> 16) & 255) | 0;
            const a = ((color >> 24) & 255) | 0;
            for (let forLoopVar = 0; forLoopVar <= (count - 1); forLoopVar++) {
                setItem(buf, i, b & 0xFF);
                setItem(buf, i + 1, g & 0xFF);
                setItem(buf, i + 2, r & 0xFF);
                setItem(buf, i + 3, a & 0xFF);
                i = ((i + 4) | 0);
            }
        };
        if ((y < VideoConstants_YBorder) ? true : (y >= (VideoConstants_YBorder + VideoConstants_ScreenHeight))) {
            fillColor(item(line.Border, pal), 0, VideoConstants_VisibleWidth);
        }
        else {
            fillColor(item(line.Border, pal), 0, VideoConstants_XBorder);
            fillColor(item(line.Border, pal), VideoConstants_XBorder + VideoConstants_ScreenWidth, VideoConstants_XBorder);
            for (let x = 0; x <= (VideoConstants_ColumnCount - 1); x++) {
                const pixel = item(x, line.Pixels) | 0;
                const attr = item(x, line.Attrs) | 0;
                const invert = ((attr & 128) !== 0) && this$.flashOn;
                const brightness = (((attr & 64) !== 0) ? 8 : 0) | 0;
                const index1 = (((attr >> 3) & 7) + brightness) | 0;
                const index2 = ((attr & 7) + brightness) | 0;
                const paperColor = (invert ? item(index1, pal) : item(index2, pal)) | 0;
                const penColor = (invert ? item(index2, pal) : item(index1, pal)) | 0;
                let i_1 = (((y * VideoConstants_VisibleWidth) + VideoConstants_XBorder) + (x * 8)) * 4;
                for (let bit = 0; bit <= 7; bit++) {
                    const color_1 = (((pixel & (1 << (7 - bit))) !== 0) ? paperColor : penColor) | 0;
                    setItem(buf, i_1, (color_1 & 255) & 0xFF);
                    setItem(buf, i_1 + 1, ((color_1 >> 8) & 255) & 0xFF);
                    setItem(buf, i_1 + 2, ((color_1 >> 16) & 255) & 0xFF);
                    setItem(buf, i_1 + 3, ((color_1 >> 24) & 255) & 0xFF);
                    i_1 = ((i_1 + 4) | 0);
                }
            }
        }
    }
    return buf;
}

