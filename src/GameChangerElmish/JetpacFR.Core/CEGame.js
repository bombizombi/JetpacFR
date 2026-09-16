
import { Machine__get_BeeperTrace, Machine__CycleCount, Machine__get_Memory, Machine__get_Video, Machine__SetKey_289F56A, Machine__DrainEffects, Machine__RemoveCoHook_Z524259A4, Machine__AddCoHook_6363443A, Machine__get_Regs, RegisterFile__Pc, Machine__LoadState_5EF83E14, Machine_$ctor } from "../Jetpac2.Core/Machine.js";
import { EnsureInstalled } from "../Jetpac2.Core/Z80Table.js";
import { Z80_assemble, Z80_runFrame, Z80_ExecutionIndex__Attach_3EE36980, Z80_makeIndexAt } from "../Jetpac2.Core/Z80Asm.js";
import { class_type } from "../fable_modules/fable-library-js.5.17.2/Reflection.js";
import { VideoScreen__BlitTo } from "../Jetpac2.Core/Screen.js";
import { toList } from "../fable_modules/fable-library-js.5.17.2/Seq.js";
import { clear } from "../fable_modules/fable-library-js.5.17.2/Util.js";
import { ToSamples } from "../Jetpac2.Core/Beeper.js";
import { op_Subtraction, toInt64_unchecked } from "../fable_modules/fable-library-js.5.17.2/BigInt.js";
import { max, min } from "../fable_modules/fable-library-js.5.17.2/Double.js";
import { item } from "../fable_modules/fable-library-js.5.17.2/Array.js";
import { ofSeq } from "../fable_modules/fable-library-js.5.17.2/List.js";

/**
 * The CE execution engine (Phase 5): drives a port Machine with a CE
 * program's per-frame driver (`Z80.runFrame`). CE ops dispatch by PC from
 * the program index and run directly; raw blocks and anything outside the
 * CE layout fall through to the interpreter. Same Machine underneath, so
 * screen, audio, and keys behave exactly like the interpreter path — the
 * "both versions of the game" toggle.
 */
export class CEGame {
    constructor(program, entryMem, entryState) {
        this.port = Machine_$ctor();
        this.frame = 0;
        this.index = ((EnsureInstalled(), (Machine__LoadState_5EF83E14(this.port, entryMem, entryState), Z80_makeIndexAt(RegisterFile__Pc(Machine__get_Regs(this.port)), program))));
        Z80_ExecutionIndex__Attach_3EE36980(this.index, this.port);
    }
}

export function CEGame_$reflection() {
    return class_type("JetpacFR.Core.CEGame", undefined, CEGame);
}

export function CEGame_$ctor_20C827FE(program, entryMem, entryState) {
    return new CEGame(program, entryMem, entryState);
}

export function CEGame__RunFrame(_) {
    const r = Z80_runFrame(_.index, _.port);
    _.frame = ((_.frame + 1) | 0);
    return r;
}

export function CEGame__AddCoHook_6363443A(_, address, hook) {
    return Machine__AddCoHook_6363443A(_.port, address, hook) | 0;
}

export function CEGame__RemoveCoHook_Z524259A4(_, token) {
    Machine__RemoveCoHook_Z524259A4(_.port, token);
}

export function CEGame__DrainEffects(_) {
    return Machine__DrainEffects(_.port);
}

export function CEGame__SetKey_289F56A(_, row, bit, pressed) {
    Machine__SetKey_289F56A(_.port, row, bit, pressed);
}

export function CEGame__get_ScreenBuffer(_) {
    return VideoScreen__BlitTo(Machine__get_Video(_.port));
}

export function CEGame__get_Regs(_) {
    return Machine__get_Regs(_.port);
}

export function CEGame__get_Memory(_) {
    return Machine__get_Memory(_.port);
}

export function CEGame__get_CycleCount(_) {
    return Machine__CycleCount(_.port);
}

export function CEGame__get_Frame(_) {
    return _.frame | 0;
}

export function CEGame__DrainBeeperSamples_Z524259C1(_, frameStart) {
    const trace = toList(Machine__get_BeeperTrace(_.port));
    clear(Machine__get_BeeperTrace(_.port));
    return ToSamples(trace, frameStart, toInt64_unchecked(op_Subtraction(Machine__CycleCount(_.port), frameStart)));
}

/**
 * (matching bytes, total compared, first diverging offsets; -1 marks a
 * length difference).
 */
export function CEParity_check(program, image) {
    const assembled = Z80_assemble(program);
    const n = min(assembled.length, image.length) | 0;
    let matching = 0;
    const mismatches = [];
    for (let i = 0; i <= (n - 1); i++) {
        if (item(i, assembled) === item(i, image)) {
            matching = ((matching + 1) | 0);
        }
        else if (mismatches.length < 8) {
            void (mismatches.push(i));
        }
    }
    const total = max(assembled.length, image.length) | 0;
    if ((assembled.length !== image.length) && (mismatches.length < 8)) {
        void (mismatches.push(-1));
    }
    return [matching, total, ofSeq(mismatches)];
}

