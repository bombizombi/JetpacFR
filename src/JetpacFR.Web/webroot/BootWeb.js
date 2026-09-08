
import { Exception, disposeSafe, getEnumerator, createAtom } from "./fable_modules/fable-library-js.5.17.0/Util.js";
import { printf, toFail } from "./fable_modules/fable-library-js.5.17.0/String.js";
import { TapeModule_stripFrame, TapeModule_headerInfo, TapeModule_parseTzx } from "./Jetpac.Core/Tape.js";
import { item as item_1, length, isEmpty, ofSeq } from "./fable_modules/fable-library-js.5.17.0/List.js";
import { Spectrum48__get_DebugZ80, Spectrum48__get_Memory, Spectrum48__get_FrameCount, Spectrum48__get_ScreenBuffer, Spectrum48__RunFrame, Spectrum48__InsertTape_Z3F6BC7B1, Spectrum48__LoadRom_Z3F6BC7B1, Spectrum48_$ctor } from "./Jetpac.Core/Spectrum.js";
import { setItem, item, fill } from "./fable_modules/fable-library-js.5.17.0/Array.js";
import { RegisterFile__Pc } from "./Jetpac.Core/Registers.js";
import { Z80__CycleCount, Z80__ExecuteOne, Z80__get_Regs } from "./Jetpac.Core/Z80.js";
import { fromUInt64, toInt64_unchecked } from "./fable_modules/fable-library-js.5.17.0/BigInt.js";

export let AssetProvider = createAtom((key) => toFail(printf("Boot.AssetProvider not set for %s"))(key));

/**
 * Expected memory spans from the tape's CODE blocks - see Core Boot.fs.
 */
export function findCodeSpans(tzxBytes) {
    const spans = [];
    let pending = undefined;
    const enumerator = getEnumerator(TapeModule_parseTzx(tzxBytes));
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            let payload;
            const b = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            payload = ((b.tag === 1) ? b.fields[4] : ((b.tag === 2) ? (new Uint8Array([])) : b.fields[1]));
            if (payload.length > 0) {
                const matchValue = TapeModule_headerInfo(payload);
                let matchResult, len_1, start_1;
                if (matchValue == null) {
                    matchResult = 2;
                }
                else if (matchValue[0] === "CODE") {
                    if ((matchValue[3], matchValue[2] > 0)) {
                        matchResult = 0;
                        len_1 = matchValue[2];
                        start_1 = matchValue[3];
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
                        pending = [start_1, len_1];
                        break;
                    }
                    case 1: {
                        pending = undefined;
                        break;
                    }
                    case 2: {
                        if (pending == null) {
                        }
                        else {
                            const start_2 = pending[0] | 0;
                            const len_2 = pending[1] | 0;
                            const data = TapeModule_stripFrame(payload);
                            void (spans.push([start_2, data.slice(0, (len_2 - 1) + 1)]));
                            pending = undefined;
                        }
                        break;
                    }
                }
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    return ofSeq(spans);
}

/**
 * Boot to the first instruction the game's own code executes after the
 * tape finishes loading - see Core Boot.fs. Keys are resolved through
 * AssetProvider (browser: embedded assets). The onFrame option, when
 * set, receives (screen buffer, frame count) after every tape frame.
 */
export function bootToEntry(romKey, tzxKey, onFrame) {
    const rom = AssetProvider()(romKey);
    const tzx = AssetProvider()(tzxKey);
    const spans = findCodeSpans(tzx);
    if (isEmpty(spans)) {
        throw new Exception("TZX contains no CODE blocks - cannot auto-boot");
    }
    const spec = Spectrum48_$ctor();
    Spectrum48__LoadRom_Z3F6BC7B1(spec, rom);
    Spectrum48__InsertTape_Z3F6BC7B1(spec, tzx);
    const matched = fill(new Array(length(spans)), 0, length(spans), false);
    let pendingCount = length(spans);
    let frames = 0;
    while ((frames < 20000) && (pendingCount > 0)) {
        Spectrum48__RunFrame(spec);
        frames = ((frames + 1) | 0);
        if (onFrame == null) {
        }
        else {
            onFrame([Spectrum48__get_ScreenBuffer(spec), Spectrum48__get_FrameCount(spec)]);
        }
        for (let j = 0; j <= (length(spans) - 1); j++) {
            let expected, i, ok;
            if (!item(j, matched)) {
                const patternInput = item_1(j, spans);
                if ((expected = patternInput[1], (i = 0, (ok = true, ((() => {
                    while (ok && (i < expected.length)) {
                        if (item(patternInput[0] + i, Spectrum48__get_Memory(spec)) !== item(i, expected)) {
                            ok = false;
                        }
                        i = ((i + 1) | 0);
                    }
                })(), ok))))) {
                    setItem(matched, j, true);
                    pendingCount = ((pendingCount - 1) | 0);
                }
            }
        }
    }
    if (pendingCount > 0) {
        const arg = frames | 0;
        toFail(printf("Tape CODE blocks never matched after %d frames"))(arg);
    }
    const z80 = Spectrum48__get_DebugZ80(spec);
    let g = 0;
    while ((RegisterFile__Pc(Z80__get_Regs(z80)) < 16384) && (g < 300000)) {
        Z80__ExecuteOne(z80);
        g = ((g + 1) | 0);
    }
    if (RegisterFile__Pc(Z80__get_Regs(z80)) < 16384) {
        const arg_1 = RegisterFile__Pc(Z80__get_Regs(z80)) | 0;
        toFail(printf("Never reached a RAM instruction after loading (pc=%04X)"))(arg_1);
    }
    return [spec, toInt64_unchecked(fromUInt64(Z80__CycleCount(z80)))];
}

