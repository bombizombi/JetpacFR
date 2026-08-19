
import { Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { option_type, bool_type, record_type, string_type, int32_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { EntryCache_save, EntryCache_tryLoad } from "../SessionWeb.js";
import { Spectrum48__DrainBeeperTrace, Spectrum48__get_Border, Spectrum48__RunGameFrame, Spectrum48_$ctor, Spectrum48__get_Memory, Spectrum48__get_DebugZ80, Spectrum48__SetKey_289F56A, Spectrum48__LoadState_5EF83E14, Spectrum48__SaveState } from "../Jetpac.Core/Spectrum.js";
import { bootToEntry } from "../BootWeb.js";
import { Machine__get_Halted, Machine__get_IrqMode, Machine__get_Iff2, Machine__get_Iff1, RegisterFile__R, RegisterFile__I, Machine__get_BeeperTrace, Machine__get_Border, Machine__set_FrameEnd_Z524259C1, Machine__set_Override_393C3003, Machine_$ctor, Machine__get_Memory, Machine__Step, Machine__get_Regs, RegisterFile__Pc, Machine__CycleCount, Machine__get_FrameEnd, Machine__SetKey_289F56A, Machine__LoadState_5EF83E14, RegisterFile__Sp, RegisterFile__Iy, RegisterFile__Ix, R16, RegisterFile__Get_Z61FD1070 } from "../Jetpac2.Core/Machine.js";
import { printf, toText } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { RegisterFile__R as RegisterFile__R_1, RegisterFile__I as RegisterFile__I_1, RegisterFile__Pc as RegisterFile__Pc_1, RegisterFile__Sp as RegisterFile__Sp_1, RegisterFile__Iy as RegisterFile__Iy_1, RegisterFile__Ix as RegisterFile__Ix_1, R16 as R16_1, RegisterFile__Get_6F21F62 } from "../Jetpac.Core/Registers.js";
import { equals as equals_1, clear, disposeSafe, getEnumerator } from "../fable_modules/fable-library-js.5.13.0/Util.js";
import { op_Addition, fromUInt64, toInt64_unchecked, equals, compare } from "../fable_modules/fable-library-js.5.13.0/BigInt.js";
import { Z80__get_Halted, Z80__get_IrqMode, Z80__get_Iff2, Z80__get_Iff1, Z80__CycleCount, Z80__ExecuteOne, Z80__get_Regs } from "../Jetpac.Core/Z80.js";
import { disasmMemory } from "./Disasm.js";
import { EnsureInstalled } from "../Jetpac2.Core/Z80Table.js";
import { isCancellationRequested } from "../fable_modules/fable-library-js.5.13.0/Async.js";
import { item } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { toList } from "../fable_modules/fable-library-js.5.13.0/Seq.js";
import { max } from "../fable_modules/fable-library-js.5.13.0/Double.js";

export class Divergence extends Record {
    constructor(Frame, Kind, Address, PortPc, OraclePc, PortExecuted, OracleExecuted, PortRegs, OracleRegs) {
        super();
        this.Frame = (Frame | 0);
        this.Kind = Kind;
        this.Address = (Address | 0);
        this.PortPc = (PortPc | 0);
        this.OraclePc = (OraclePc | 0);
        this.PortExecuted = PortExecuted;
        this.OracleExecuted = OracleExecuted;
        this.PortRegs = PortRegs;
        this.OracleRegs = OracleRegs;
    }
}

export function Divergence_$reflection() {
    return record_type("JetpacFR.Core.Validation.Divergence", [], Divergence, () => [["Frame", int32_type], ["Kind", string_type], ["Address", int32_type], ["PortPc", int32_type], ["OraclePc", int32_type], ["PortExecuted", string_type], ["OracleExecuted", string_type], ["PortRegs", string_type], ["OracleRegs", string_type]]);
}

export class Report extends Record {
    constructor(Frames, Passed, FirstDivergence) {
        super();
        this.Frames = (Frames | 0);
        this.Passed = Passed;
        this.FirstDivergence = FirstDivergence;
    }
}

export function Report_$reflection() {
    return record_type("JetpacFR.Core.Validation.Report", [], Report, () => [["Frames", int32_type], ["Passed", bool_type], ["FirstDivergence", option_type(Divergence_$reflection())]]);
}

function entryState(romPath, tzxPath) {
    const matchValue = EntryCache_tryLoad(romPath, tzxPath);
    if (matchValue == null) {
        const patternInput_1 = Spectrum48__SaveState(bootToEntry(romPath, tzxPath)[0]);
        const state = patternInput_1[1];
        const mem = patternInput_1[0];
        EntryCache_save(romPath, tzxPath, mem, state);
        return [mem, state];
    }
    else {
        return matchValue;
    }
}

function portRegs(r) {
    const arg = RegisterFile__Get_Z61FD1070(r, R16.AF) | 0;
    const arg_1 = RegisterFile__Get_Z61FD1070(r, R16.BC) | 0;
    const arg_2 = RegisterFile__Get_Z61FD1070(r, R16.DE) | 0;
    const arg_3 = RegisterFile__Get_Z61FD1070(r, R16.HL) | 0;
    const arg_4 = RegisterFile__Ix(r) | 0;
    const arg_5 = RegisterFile__Iy(r) | 0;
    const arg_6 = RegisterFile__Sp(r) | 0;
    return toText(printf("AF=%04X BC=%04X DE=%04X HL=%04X IX=%04X IY=%04X SP=%04X"))(arg)(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6);
}

function oracleRegs(r) {
    const arg = RegisterFile__Get_6F21F62(r, R16_1.AF) | 0;
    const arg_1 = RegisterFile__Get_6F21F62(r, R16_1.BC) | 0;
    const arg_2 = RegisterFile__Get_6F21F62(r, R16_1.DE) | 0;
    const arg_3 = RegisterFile__Get_6F21F62(r, R16_1.HL) | 0;
    const arg_4 = RegisterFile__Ix_1(r) | 0;
    const arg_5 = RegisterFile__Iy_1(r) | 0;
    const arg_6 = RegisterFile__Sp_1(r) | 0;
    return toText(printf("AF=%04X BC=%04X DE=%04X HL=%04X IX=%04X IY=%04X SP=%04X"))(arg)(arg_1)(arg_2)(arg_3)(arg_4)(arg_5)(arg_6);
}

function replayFrame(oracle, port, startMem, startState, frame, script, fallbackKind, fallbackAddress) {
    Spectrum48__LoadState_5EF83E14(oracle, startMem, startState);
    Machine__LoadState_5EF83E14(port, startMem, startState);
    const enumerator = getEnumerator(script);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const forLoopVar = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const row = forLoopVar[1] | 0;
            const pressed = forLoopVar[3];
            const bit = forLoopVar[2] | 0;
            if (forLoopVar[0] < frame) {
                Spectrum48__SetKey_289F56A(oracle, row, bit, pressed);
                Machine__SetKey_289F56A(port, row, bit, pressed);
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const z80 = Spectrum48__get_DebugZ80(oracle);
    const frameEnd = Machine__get_FrameEnd(port);
    let steps = 0;
    let found = undefined;
    while (((found == null) && (compare(Machine__CycleCount(port), frameEnd) < 0)) && (steps < 100000)) {
        const pcBefore = RegisterFile__Pc(Machine__get_Regs(port)) | 0;
        const oPcBefore = RegisterFile__Pc_1(Z80__get_Regs(z80)) | 0;
        Z80__ExecuteOne(z80);
        Machine__Step(port);
        steps = ((steps + 1) | 0);
        if ((RegisterFile__Pc(Machine__get_Regs(port)) !== RegisterFile__Pc_1(Z80__get_Regs(z80))) ? true : !equals(Machine__CycleCount(port), toInt64_unchecked(fromUInt64(Z80__CycleCount(z80))))) {
            found = (new Divergence(frame, "instruction", pcBefore, pcBefore, oPcBefore, disasmMemory(Machine__get_Memory(port), pcBefore).Text, disasmMemory(Spectrum48__get_Memory(oracle), oPcBefore).Text, portRegs(Machine__get_Regs(port)), oracleRegs(Z80__get_Regs(z80))));
        }
    }
    if (found == null) {
        return new Divergence(frame, fallbackKind, fallbackAddress, RegisterFile__Pc(Machine__get_Regs(port)), RegisterFile__Pc_1(Z80__get_Regs(z80)), "", "", portRegs(Machine__get_Regs(port)), oracleRegs(Z80__get_Regs(z80)));
    }
    else {
        return found;
    }
}

/**
 * Run `framesN` lockstep frames: oracle reference vs port with
 * `overrideHook` (normally LiftedRoutines.registryHook). Returns the first
 * divergence, or Passed when every frame matches.
 */
export function run(romPath, tzxPath, framesN, script, overrideHook, cancel) {
    const patternInput = entryState(romPath, tzxPath);
    const state = patternInput[1];
    const mem = patternInput[0];
    const oracle = Spectrum48_$ctor();
    Spectrum48__LoadState_5EF83E14(oracle, mem, state);
    const port = Machine_$ctor();
    EnsureInstalled();
    Machine__set_Override_393C3003(port, overrideHook);
    Machine__LoadState_5EF83E14(port, mem, state);
    let frame = 0;
    let divergence = undefined;
    let frameStartMem = mem;
    let frameStartState = state;
    while (((frame < framesN) && (divergence == null)) && !isCancellationRequested(cancel)) {
        const patternInput_1 = Spectrum48__SaveState(oracle);
        frameStartMem = patternInput_1[0];
        frameStartState = patternInput_1[1];
        Spectrum48__RunGameFrame(oracle);
        const portEnd = Machine__get_FrameEnd(port);
        while (compare(Machine__CycleCount(port), portEnd) < 0) {
            Machine__Step(port);
        }
        Machine__set_FrameEnd_Z524259C1(port, toInt64_unchecked(op_Addition(Machine__get_FrameEnd(port), 69888n)));
        const enumerator = getEnumerator(script);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                const forLoopVar = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const row = forLoopVar[1] | 0;
                const pressed = forLoopVar[3];
                const bit = forLoopVar[2] | 0;
                if (forLoopVar[0] === frame) {
                    Spectrum48__SetKey_289F56A(oracle, row, bit, pressed);
                    Machine__SetKey_289F56A(port, row, bit, pressed);
                }
            }
        }
        finally {
            disposeSafe(enumerator);
        }
        const z80 = Spectrum48__get_DebugZ80(oracle);
        let memDiff = -1;
        let i = 16384;
        while ((memDiff < 0) && (i < 65536)) {
            if (item(i, Machine__get_Memory(port)) !== item(i, Spectrum48__get_Memory(oracle))) {
                memDiff = (i | 0);
            }
            i = ((i + 1) | 0);
        }
        const borderDiff = Machine__get_Border(port) !== Spectrum48__get_Border(oracle);
        const portSlice = toList(Machine__get_BeeperTrace(port));
        clear(Machine__get_BeeperTrace(port));
        const traceDiff = !equals_1(portSlice, Spectrum48__DrainBeeperTrace(oracle));
        const regsOk = (((((((((((((((((RegisterFile__Pc(Machine__get_Regs(port)) === RegisterFile__Pc_1(Z80__get_Regs(z80))) && (RegisterFile__Sp(Machine__get_Regs(port)) === RegisterFile__Sp_1(Z80__get_Regs(z80)))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.AF) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.AF))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.BC) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.BC))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.DE) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.DE))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.HL) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.HL))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.AF_) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.AF_))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.BC_) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.BC_))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.DE_) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.DE_))) && (RegisterFile__Get_Z61FD1070(Machine__get_Regs(port), R16.HL_) === RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16_1.HL_))) && (RegisterFile__Ix(Machine__get_Regs(port)) === RegisterFile__Ix_1(Z80__get_Regs(z80)))) && (RegisterFile__Iy(Machine__get_Regs(port)) === RegisterFile__Iy_1(Z80__get_Regs(z80)))) && (RegisterFile__I(Machine__get_Regs(port)) === RegisterFile__I_1(Z80__get_Regs(z80)))) && (RegisterFile__R(Machine__get_Regs(port)) === RegisterFile__R_1(Z80__get_Regs(z80)))) && (Machine__get_Iff1(port) === Z80__get_Iff1(z80))) && (Machine__get_Iff2(port) === Z80__get_Iff2(z80))) && (Machine__get_IrqMode(port) === Z80__get_IrqMode(z80))) && (Machine__get_Halted(port) === Z80__get_Halted(z80));
        let kind;
        if (!regsOk) {
            kind = "registers";
        }
        else if (memDiff >= 0) {
            const arg = memDiff | 0;
            kind = toText(printf("memory@%04X"))(arg);
        }
        else {
            kind = (traceDiff ? "beeper trace" : "border");
        }
        if ((((memDiff >= 0) ? true : borderDiff) ? true : traceDiff) ? true : !regsOk) {
            divergence = replayFrame(oracle, port, frameStartMem, frameStartState, frame, script, kind, max(0, memDiff));
        }
        frame = ((frame + 1) | 0);
    }
    return new Report(framesN, divergence == null, divergence);
}

