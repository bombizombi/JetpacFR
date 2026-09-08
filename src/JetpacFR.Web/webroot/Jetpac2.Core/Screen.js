
import { Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { class_type, record_type, array_type, int32_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { setItem, item, initialize } from "../fable_modules/fable-library-js.5.17.0/Array.js";

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
    return record_type("Jetpac2.Core.VideoLine", [], VideoLine, () => [["Border", int32_type], ["Pixels", array_type(int32_type)], ["Attrs", array_type(int32_type)]]);
}

/**
 * Port of JetpacFSharp (Jetpac.Core) Video — the scanline renderer, reading
 * the game's flat 64K memory (page 1 at 0x4000) instead of a paged Memory.
 * The line buffer is snapshotted as each scanline passes, exactly like the
 * ULA, so mid-frame writes produce the same tearing the emulator shows.
 */
export class VideoScreen {
    constructor(memory) {
        this.memory = memory;
        this.border = 3;
        this.currentLine = 0;
        this.flashCounter = 0;
        this.flashOn = false;
        this.lines = initialize(VideoConstants_VisibleHeight, (_arg) => (new VideoLine(3, new Int32Array(VideoConstants_ColumnCount), new Int32Array(VideoConstants_ColumnCount))));
        this.screenBuffer = (new Uint8Array((VideoConstants_VisibleWidth * VideoConstants_VisibleHeight) * 4));
    }
}

export function VideoScreen_$reflection() {
    return class_type("Jetpac2.Core.VideoScreen", undefined, VideoScreen);
}

export function VideoScreen_$ctor_Z3F6BC7B1(memory) {
    return new VideoScreen(memory);
}

export function VideoScreen__SetBorder_Z524259A4(this$, v) {
    this$.border = ((v & 7) | 0);
}

/**
 * Restore the scanline/flash phase captured in the fixture.
 */
export function VideoScreen__SetState_289F56A(this$, scanline, flashCounter_, flashOn_) {
    this$.currentLine = ((scanline % VideoConstants_PalTotalLines) | 0);
    this$.flashCounter = (flashCounter_ | 0);
    this$.flashOn = flashOn_;
}

/**
 * Re-render every visible line from current memory (rewind present):
 * `lines` holds scanline-pass snapshots, so after a state restore the
 * blit buffer would otherwise show the pre-restore frame until a new
 * frame executes. Border-only lines keep their captured border color.
 */
export function VideoScreen__RenderAll(this$) {
    for (let y = 0; y <= (VideoConstants_VisibleHeight - 1); y++) {
        VideoScreen__RenderLine_Z524259A4(this$, y + VideoConstants_VSyncLines);
    }
}

/**
 * Advance one scanline; returns true when the 312-line counter wraps
 * (the interrupt point).
 */
export function VideoScreen__NextScanLine(this$) {
    VideoScreen__RenderLine_Z524259A4(this$, this$.currentLine);
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

function VideoScreen__RenderLine_Z524259A4(this$, displayLine) {
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
                line.Pixels[x] = (~~item((16384 + screenOffset) + x, this$.memory) | 0);
                line.Attrs[x] = (~~item((22528 + (charRow * VideoConstants_ColumnCount)) + x, this$.memory) | 0);
            }
        }
    }
}

/**
 * Fill the reused screen buffer (B,G,R,A byte order) and return it.
 */
export function VideoScreen__BlitTo(this$) {
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

