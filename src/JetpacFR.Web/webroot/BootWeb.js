
import { Exception, disposeSafe, getEnumerator, createAtom } from "./fable_modules/fable-library-js.5.13.0/Util.js";
import { printf, toFail } from "./fable_modules/fable-library-js.5.13.0/String.js";
import { TapeModule_stripFrame, TapeModule_headerInfo, TapeModule_parseTzx } from "./Jetpac.Core/Tape.js";
import { Spectrum48__get_DebugZ80, Spectrum48__get_Memory, Spectrum48__RunFrame, Spectrum48__InsertTape_Z3F6BC7B1, Spectrum48__LoadRom_Z3F6BC7B1, Spectrum48_$ctor } from "./Jetpac.Core/Spectrum.js";
import { item } from "./fable_modules/fable-library-js.5.13.0/Array.js";
import { RegisterFile__Pc } from "./Jetpac.Core/Registers.js";
import { Z80__CycleCount, Z80__ExecuteOne, Z80__get_Regs } from "./Jetpac.Core/Z80.js";
import { fromUInt64, toInt64_unchecked } from "./fable_modules/fable-library-js.5.13.0/BigInt.js";

export let AssetProvider = createAtom((key) => toFail(printf("Boot.AssetProvider not set for %s"))(key));

export function findCodeBlocks(tzxBytes) {
    let pendingHeader = undefined;
    let screen = undefined;
    let gameCode = undefined;
    const enumerator = getEnumerator(TapeModule_parseTzx(tzxBytes));
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            let payload;
            const b = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            payload = ((b.tag === 1) ? b.fields[4] : ((b.tag === 2) ? (new Uint8Array([])) : b.fields[1]));
            if (payload.length > 0) {
                const matchValue = TapeModule_headerInfo(payload);
                if (matchValue == null) {
                    const s = TapeModule_stripFrame(payload);
                    let matchResult;
                    if (pendingHeader != null) {
                        if (pendingHeader[0] === "CODE") {
                            switch (pendingHeader[1]) {
                                case 6912: {
                                    if (screen == null) {
                                        matchResult = 0;
                                    }
                                    else {
                                        matchResult = 2;
                                    }
                                    break;
                                }
                                case 8192: {
                                    if (gameCode == null) {
                                        matchResult = 1;
                                    }
                                    else {
                                        matchResult = 2;
                                    }
                                    break;
                                }
                                default:
                                    matchResult = 2;
                            }
                        }
                        else {
                            matchResult = 2;
                        }
                    }
                    else {
                        matchResult = 2;
                    }
                    switch (matchResult) {
                        case 0: {
                            screen = s.slice(0, 6911 + 1);
                            break;
                        }
                        case 1: {
                            gameCode = s.slice(0, 8191 + 1);
                            break;
                        }
                    }
                }
                else {
                    const t = matchValue[0];
                    const len = matchValue[2] | 0;
                    pendingHeader = [t, len];
                }
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const screen_1 = screen;
    const gameCode_1 = gameCode;
    if (screen_1 == null) {
        throw new Exception("Could not find a 6912-byte CODE block in the TZX");
    }
    else if (gameCode_1 == null) {
        throw new Exception("Could not find an 8192-byte CODE block in the TZX");
    }
    else {
        const g = gameCode_1;
        const s_1 = screen_1;
        return [s_1, g];
    }
}

/**
 * Boot to the game entry instruction; returns (spec, entryCycles).
 * Keys are resolved through AssetProvider (browser: embedded assets).
 */
export function bootToEntry(romKey, tzxKey) {
    const rom = AssetProvider()(romKey);
    const tzx = AssetProvider()(tzxKey);
    const patternInput = findCodeBlocks(tzx);
    const expectedGameCode = patternInput[1];
    const spec = Spectrum48_$ctor();
    Spectrum48__LoadRom_Z3F6BC7B1(spec, rom);
    Spectrum48__InsertTape_Z3F6BC7B1(spec, tzx);
    let frames = 0;
    let loaded = false;
    while ((frames < 8000) && !loaded) {
        Spectrum48__RunFrame(spec);
        frames = ((frames + 1) | 0);
        let matchCount = 0;
        let i = 16384;
        while ((i <= 23295) && (matchCount >= 0)) {
            if (item(i, Spectrum48__get_Memory(spec)) === item(i - 16384, patternInput[0])) {
                matchCount = ((matchCount + 1) | 0);
            }
            else {
                matchCount = -1;
            }
            i = ((i + 1) | 0);
        }
        if (matchCount === 6912) {
            loaded = true;
        }
    }
    if (!loaded) {
        const arg = frames | 0;
        toFail(printf("Loading screen never matched after %d frames"))(arg);
    }
    let codeLoaded = false;
    let codeFrames = frames;
    while ((codeFrames < 8000) && !codeLoaded) {
        Spectrum48__RunFrame(spec);
        codeFrames = ((codeFrames + 1) | 0);
        let m = 0;
        let ok = true;
        while ((m < 8192) && ok) {
            if (item(24576 + m, Spectrum48__get_Memory(spec)) !== item(m, expectedGameCode)) {
                ok = false;
            }
            m = ((m + 1) | 0);
        }
        if (ok) {
            codeLoaded = true;
        }
    }
    if (!codeLoaded) {
        const arg_1 = codeFrames | 0;
        toFail(printf("Game code at 0x6000 never matched by frame %d"))(arg_1);
    }
    let moverStarted = false;
    let g = 0;
    while (!moverStarted && (g < 2000)) {
        Spectrum48__RunFrame(spec);
        g = ((g + 1) | 0);
        if (item(24576, Spectrum48__get_Memory(spec)) !== item(0, expectedGameCode)) {
            moverStarted = true;
        }
    }
    if (!moverStarted) {
        const arg_2 = g | 0;
        toFail(printf("Game loader never started after %d frames"))(arg_2);
    }
    const z80 = Spectrum48__get_DebugZ80(spec);
    g = 0;
    while (((RegisterFile__Pc(Z80__get_Regs(z80)) < 24576) ? true : (RegisterFile__Pc(Z80__get_Regs(z80)) > 32767)) && (g < 300000)) {
        Z80__ExecuteOne(z80);
        g = ((g + 1) | 0);
    }
    if ((RegisterFile__Pc(Z80__get_Regs(z80)) < 24576) ? true : (RegisterFile__Pc(Z80__get_Regs(z80)) > 32767)) {
        const arg_3 = RegisterFile__Pc(Z80__get_Regs(z80)) | 0;
        toFail(printf("Never reached game entry (pc=%04X)"))(arg_3);
    }
    return [spec, toInt64_unchecked(fromUInt64(Z80__CycleCount(z80)))];
}

