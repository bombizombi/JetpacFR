
import { Union, Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { class_type, union_type, record_type, int32_type, array_type, uint8_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { forAll2, item } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { equals, Exception } from "../fable_modules/fable-library-js.5.13.0/Util.js";
import { trimEnd, toText, printf, toFail } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { item as item_1, length, collect, empty, singleton, ofSeq } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { get_UTF8 } from "../fable_modules/fable-library-js.5.13.0/Encoding.js";
import { min, max } from "../fable_modules/fable-library-js.5.13.0/Double.js";

export const TapeModule_PilotCycles = 2168;

export const TapeModule_Sync1Cycles = 667;

export const TapeModule_Sync2Cycles = 735;

export const TapeModule_Data0Cycles = 855;

export const TapeModule_Data1Cycles = 1710;

export const TapeModule_PilotDataEdges = 3223;

export const TapeModule_PilotHeaderEdges = 8063;

/**
 * One playable frame: flag byte + payload + checksum, exactly as recorded.
 */
export class TapeModule_FrameBlock extends Record {
    constructor(Data, PauseCycles, Bit0Cycles, Bit1Cycles, PilotEdges) {
        super();
        this.Data = Data;
        this.PauseCycles = (PauseCycles | 0);
        this.Bit0Cycles = (Bit0Cycles | 0);
        this.Bit1Cycles = (Bit1Cycles | 0);
        this.PilotEdges = (PilotEdges | 0);
    }
}

export function TapeModule_FrameBlock_$reflection() {
    return record_type("Jetpac.Core.TapeModule.FrameBlock", [], TapeModule_FrameBlock, () => [["Data", array_type(uint8_type)], ["PauseCycles", int32_type], ["Bit0Cycles", int32_type], ["Bit1Cycles", int32_type], ["PilotEdges", int32_type]]);
}

export class TapeModule_TzxBlock extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["StandardData", "TurboData", "Skipped"];
    }
}

export function TapeModule_TzxBlock_$reflection() {
    return union_type("Jetpac.Core.TapeModule.TzxBlock", [], TapeModule_TzxBlock, () => [[["pauseMs", int32_type], ["payload", array_type(uint8_type)]], [["pauseMs", int32_type], ["pilotMs", int32_type], ["bit0", int32_type], ["bit1", int32_type], ["payload", array_type(uint8_type)]], [["id", uint8_type]]]);
}

export function TapeModule_u16(b, i) {
    return (~~item(i, b) | (~~item(i + 1, b) << 8)) | 0;
}

const TapeModule_zxTapeSig = new Uint8Array([90, 88, 84, 97, 112, 101, 33]);

/**
 * Parse a whole TZX file into a block list (ported from TzxScreen/Program.fs).
 */
export function TapeModule_parseTzx(data) {
    if ((data.length < 10) ? true : !forAll2((x, y) => (x === y), TapeModule_zxTapeSig, data.slice(0, 6 + 1))) {
        throw new Exception("Not a TZX file (missing \'ZXTape!\' signature).");
    }
    let pos = 7;
    if (item(pos, data) !== 26) {
        throw new Exception("Malformed TZX header (missing 0x1A).");
    }
    pos = ((pos + 1) | 0);
    if (item(pos, data) === 26) {
        pos = ((pos + 1) | 0);
    }
    pos = ((pos + 2) | 0);
    const blocks = [];
    while (pos < data.length) {
        const id = item(pos, data);
        pos = ((pos + 1) | 0);
        switch (id) {
            case 16: {
                const len = TapeModule_u16(data, pos + 2) | 0;
                void (blocks.push(new TapeModule_TzxBlock(/* StandardData */ 0, [TapeModule_u16(data, pos), data.slice(pos + 4, (((pos + 4) + len) - 1) + 1)])));
                pos = (((pos + 4) + len) | 0);
                break;
            }
            case 17: {
                const len_1 = TapeModule_u16(data, pos + 17) | 0;
                void (blocks.push(new TapeModule_TzxBlock(/* TurboData */ 1, [TapeModule_u16(data, pos), TapeModule_u16(data, pos + 2), TapeModule_u16(data, pos + 8), TapeModule_u16(data, pos + 10), data.slice(pos + 19, (((pos + 19) + len_1) - 1) + 1)])));
                pos = (((pos + 19) + len_1) | 0);
                break;
            }
            case 18: {
                pos = ((pos + 4) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 19: {
                const n = ~~item(pos, data) | 0;
                pos = (((pos + 1) + (n * 2)) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 20: {
                const len_2 = TapeModule_u16(data, pos + 15) | 0;
                pos = (((pos + 17) + len_2) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 21: {
                const len_3 = ((~~item(pos + 6, data) | (~~item(pos + 7, data) << 8)) | (~~item(pos + 8, data) << 16)) | 0;
                pos = (((pos + 9) + len_3) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 22: {
                pos = ((pos + 3) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 23: {
                const len_4 = TapeModule_u16(data, pos + 13) | 0;
                pos = (((pos + 15) + len_4) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 24: {
                const len_5 = TapeModule_u16(data, pos + 2) | 0;
                pos = (((pos + 16) + len_5) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 25: {
                const pilotLen = TapeModule_u16(data, pos) | 0;
                const d2 = ((pos + 2) + pilotLen) | 0;
                const dataLen = TapeModule_u16(data, d2) | 0;
                const symCount = TapeModule_u16(data, d2 + 2) | 0;
                let p = d2 + 4;
                for (let forLoopVar = 1; forLoopVar <= symCount; forLoopVar++) {
                    const pulseLen = TapeModule_u16(data, p + 1) | 0;
                    p = (((p + 3) + pulseLen) | 0);
                }
                const maxSym = TapeModule_u16(data, p) | 0;
                const dataSym = TapeModule_u16(data, p + 2) | 0;
                p = ((p + 4) | 0);
                for (let forLoopVar_1 = 1; forLoopVar_1 <= dataSym; forLoopVar_1++) {
                    p = (((p + 2) + maxSym) | 0);
                }
                p = ((p + dataLen) | 0);
                pos = (p | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 32: {
                pos = ((pos + 2) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 33:
            case 40:
            case 48: {
                const n_1 = ~~item(pos, data) | 0;
                pos = (((pos + 1) + n_1) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 34:
            case 37: {
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 35:
            case 36: {
                pos = ((pos + 2) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 38: {
                pos = ((pos + 4) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 39: {
                pos = ((pos + 1) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 49: {
                const n_2 = ~~item(pos + 1, data) | 0;
                pos = (((pos + 2) + n_2) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 50: {
                const count = TapeModule_u16(data, pos) | 0;
                let p_1 = pos + 2;
                for (let forLoopVar_2 = 1; forLoopVar_2 <= count; forLoopVar_2++) {
                    const n_3 = TapeModule_u16(data, p_1 + 2) | 0;
                    p_1 = (((p_1 + 4) + n_3) | 0);
                }
                pos = (p_1 | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 51: {
                const n_4 = ~~item(pos, data) | 0;
                pos = (((pos + 1) + (n_4 * 3)) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 52: {
                const descLen = TapeModule_u16(data, pos) | 0;
                const n_5 = ~~item((pos + 2) + descLen, data) | 0;
                pos = ((((pos + 3) + descLen) + n_5) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            case 53: {
                pos = ((pos + 4) | 0);
                void (blocks.push(new TapeModule_TzxBlock(/* Skipped */ 2, [id])));
                break;
            }
            default: {
                const arg_1 = pos | 0;
                toFail(printf("Unknown TZX block 0x%02X at offset %d"))(id)(arg_1);
            }
        }
    }
    return ofSeq(blocks);
}

/**
 * Payload of a tape frame with the flag byte and (when present) checksum
 * removed (ported from TzxScreen/Program.fs).
 */
export function TapeModule_stripFrame(frame) {
    const n = frame.length | 0;
    if ((n >= 2) && ((item(0, frame) === 255) ? true : (item(0, frame) === 0))) {
        if (n >= 3) {
            let x = 0;
            for (let i = 0; i <= (n - 2); i++) {
                x = (x ^ item(i, frame));
            }
            if (x === item(n - 1, frame)) {
                return frame.slice(1, (n - 2) + 1);
            }
            else {
                return frame.slice(1, frame.length);
            }
        }
        else {
            return frame.slice(1, frame.length);
        }
    }
    else {
        return frame;
    }
}

/**
 * Decode a 17-byte tape header frame. Returns (type, name, length, param1, param2).
 */
export function TapeModule_headerInfo(frame) {
    let matchValue;
    const p = TapeModule_stripFrame(frame);
    if ((p.length === 17) && (item(0, frame) === 0)) {
        return [(matchValue = item(0, p), (matchValue === 0) ? "PROGRAM" : ((matchValue === 1) ? "NUM ARRAY" : ((matchValue === 2) ? "CHAR ARRAY" : ((matchValue === 3) ? "CODE" : toText(printf("TYPE %d"))(matchValue))))), trimEnd(get_UTF8().getString(p, 1, 10)), TapeModule_u16(p, 11), TapeModule_u16(p, 13), TapeModule_u16(p, 15)];
    }
    else {
        return undefined;
    }
}

/**
 * Convert a TZX data block (0x10/0x11) into playable frames. The payload
 * is the raw tape frame (flag + payload + checksum); 0x10 blocks use the
 * standard timings, 0x11 blocks carry their own per-block timings.
 */
export function TapeModule_framesFor(block) {
    switch (block.tag) {
        case 1:
            return singleton(new TapeModule_FrameBlock(block.fields[4], block.fields[0] * 3500, block.fields[2], block.fields[3], max(1, ~~((block.fields[1] * 3500) / TapeModule_PilotCycles))));
        case 2:
            return empty();
        default: {
            const frame = block.fields[1];
            return singleton(new TapeModule_FrameBlock(frame, block.fields[0] * 3500, TapeModule_Data0Cycles, TapeModule_Data1Cycles, ((item(0, frame) & 128) !== 0) ? TapeModule_PilotDataEdges : TapeModule_PilotHeaderEdges));
        }
    }
}

export class TapeModule_State extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Idle", "Pilot", "Sync1", "Sync2", "Data1", "Data2", "Pause"];
    }
    static Idle = new TapeModule_State(0, []);
    static Pilot = new TapeModule_State(1, []);
    static Sync1 = new TapeModule_State(2, []);
    static Sync2 = new TapeModule_State(3, []);
    static Data1 = new TapeModule_State(4, []);
    static Data2 = new TapeModule_State(5, []);
    static Pause = new TapeModule_State(6, []);
}

export function TapeModule_State_$reflection() {
    return union_type("Jetpac.Core.TapeModule.State", [], TapeModule_State, () => [[], [], [], [], [], [], []]);
}

/**
 * Pulse playback state machine (Tape.cpp). InsertTzx parses the TZX; play()
 * starts from the current block.
 */
export class Tape {
    constructor() {
        this.numEdges = 0;
        this.nextTransition = 0;
        this.bitCycles = 0;
        this.level = false;
        this.state = TapeModule_State.Idle;
        this.currentBlockIndex = 0;
        this.bitOffset = 0;
        this.blocks = empty();
    }
}

export function Tape_$reflection() {
    return class_type("Jetpac.Core.Tape", undefined, Tape);
}

export function Tape_$ctor() {
    return new Tape();
}

export function Tape__InsertTzx_Z3F6BC7B1(this$, bytes) {
    const frames = collect(TapeModule_framesFor, TapeModule_parseTzx(bytes));
    this$.blocks = frames;
    this$.currentBlockIndex = 0;
    this$.bitOffset = 0;
    this$.state = TapeModule_State.Idle;
    this$.nextTransition = 0;
    this$.level = false;
}

function Tape__CurrentBlock(this$) {
    if (this$.currentBlockIndex < length(this$.blocks)) {
        return item_1(this$.currentBlockIndex, this$.blocks);
    }
    else {
        return undefined;
    }
}

export function Tape__NextTransition(this$) {
    return this$.nextTransition | 0;
}

export function Tape__Level(this$) {
    return this$.level;
}

export function Tape__Playing(this$) {
    return !equals(this$.state, TapeModule_State.Idle);
}

/**
 * Restore the ear level after a checkpoint load (used by Spectrum48.LoadState).
 */
export function Tape__SetLevel_Z1FBCCD16(this$, v) {
    this$.level = v;
}

export function Tape__PassTime_Z524259A4(this$, cycles) {
    this$.nextTransition = ((this$.nextTransition - min(cycles, this$.nextTransition)) | 0);
    if (this$.nextTransition === 0) {
        Tape__Next(this$);
    }
}

export function Tape__Play(this$) {
    const matchValue = Tape__CurrentBlock(this$);
    const matchValue_1 = this$.state;
    let matchResult, block;
    if (matchValue != null) {
        if (matchValue_1.tag === 0) {
            matchResult = 0;
            block = matchValue;
        }
        else {
            matchResult = 1;
        }
    }
    else {
        matchResult = 1;
    }
    switch (matchResult) {
        case 0: {
            this$.state = TapeModule_State.Pilot;
            this$.nextTransition = 1;
            this$.numEdges = (block.PilotEdges | 0);
            break;
        }
        case 1: {
            break;
        }
    }
}

export function Tape__Stop(this$) {
    this$.state = TapeModule_State.Idle;
    this$.nextTransition = 0;
}

function Tape__Next(this$) {
    this$.level = !this$.level;
    const matchValue = this$.state;
    switch (matchValue.tag) {
        case 2: {
            this$.nextTransition = (TapeModule_Sync1Cycles | 0);
            this$.state = TapeModule_State.Sync2;
            break;
        }
        case 3: {
            this$.nextTransition = (TapeModule_Sync2Cycles | 0);
            this$.state = Tape__NextBit(this$);
            break;
        }
        case 4: {
            this$.nextTransition = (this$.bitCycles | 0);
            this$.state = TapeModule_State.Data2;
            break;
        }
        case 5: {
            this$.nextTransition = (this$.bitCycles | 0);
            this$.state = Tape__NextBit(this$);
            break;
        }
        case 6: {
            this$.nextTransition = (this$.bitCycles | 0);
            const matchValue_1 = Tape__CurrentBlock(this$);
            if (matchValue_1 == null) {
            }
            else {
                const block = matchValue_1;
                this$.numEdges = (block.PilotEdges | 0);
                this$.state = TapeModule_State.Pilot;
            }
            break;
        }
        case 0: {
            this$.nextTransition = 0;
            break;
        }
        default: {
            this$.nextTransition = (TapeModule_PilotCycles | 0);
            this$.numEdges = ((this$.numEdges - 1) | 0);
            if (this$.numEdges === 0) {
                this$.state = TapeModule_State.Sync1;
            }
        }
    }
}

function Tape__NextBit(this$) {
    const matchValue = Tape__CurrentBlock(this$);
    if (matchValue != null) {
        const block = matchValue;
        const byteOffset = ~~(this$.bitOffset / 8) | 0;
        if (byteOffset >= block.Data.length) {
            this$.bitCycles = (block.PauseCycles | 0);
            this$.bitOffset = 0;
            this$.currentBlockIndex = ((this$.currentBlockIndex + 1) | 0);
            return TapeModule_State.Pause;
        }
        else {
            this$.bitCycles = ((((item(byteOffset, block.Data) & (1 << (7 - (this$.bitOffset % 8)))) !== 0) ? block.Bit1Cycles : block.Bit0Cycles) | 0);
            this$.bitOffset = ((this$.bitOffset + 1) | 0);
            return TapeModule_State.Data1;
        }
    }
    else {
        return TapeModule_State.Idle;
    }
}

