
import { join, printf, toText } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { StringBuilder__AppendLine_Z721C83C5, StringBuilder_$ctor } from "../fable_modules/fable-library-js.5.13.0/System.Text.js";
import { disposeSafe, getEnumerator } from "../fable_modules/fable-library-js.5.13.0/Util.js";
import { fromInt32, max, op_Division, toInt64_unchecked, toInt32_unchecked } from "../fable_modules/fable-library-js.5.13.0/BigInt.js";
import { map, isEmpty } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { toString } from "../fable_modules/fable-library-js.5.13.0/Types.js";

function regLine(s) {
    return toText(printf("AF=%04X BC=%04X DE=%04X HL=%04X IX=%04X IY=%04X SP=%04X I=%02X R=%02X"))(s.Af)(s.Bc)(s.De)(s.Hl)(s.Ix)(s.Iy)(s.Sp)(s.I)(s.R);
}

export function generate(c) {
    let arg_9, arg_18, clo_19;
    const sb = StringBuilder_$ctor();
    const append = (s) => {
        StringBuilder__AppendLine_Z721C83C5(sb, s);
    };
    append(toText(printf("Convert the Z80 routine at 0x%04X (span 0x%04X-0x%04X) in the 16K Spectrum game Jetpac into idiomatic F#, replacing the generated instruction-table implementation. The converted code runs inside Jetpac2.Core.Machine and MUST be instruction-granular (one match arm per resume PC) so an interrupt can land mid-routine at the exact T-state."))(c.Entry)(c.SpanLo)(c.SpanHi));
    append("");
    append("## Exact disassembly (with execution counts from the trace window)");
    const enumerator = getEnumerator(c.Disassembly);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const insn = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            append(toText(printf("%04X  %-28s  x%-7d  ~%d tstates"))(insn.Address)(insn.Text)(insn.Executions)(insn.AvgCycles));
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    append("");
    append("## Behavioural contract (observed, not guessed)");
    append(toText(printf("- call count: %d"))(c.CallCount));
    append((arg_9 = (~~toInt32_unchecked(toInt64_unchecked(op_Division(c.InclusiveTStates, max(1n, toInt64_unchecked(fromInt32(c.CallCount)))))) | 0), toText(printf("- inclusive T-states: %d (avg %d per call)"))(c.InclusiveTStates)(arg_9)));
    append("- memory writes (lo..hi -> value x count; these are memory CHANGES, old<>new, not every store):");
    if (isEmpty(c.WriteRanges)) {
        append("  (none observed)");
    }
    else {
        const enumerator_1 = getEnumerator(c.WriteRanges);
        try {
            while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
                const forLoopVar = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]();
                append(toText(printf("  %04X..%04X -> %02X (x%d)"))(forLoopVar[0])(forLoopVar[1])(forLoopVar[2])(forLoopVar[3]));
            }
        }
        finally {
            disposeSafe(enumerator_1);
        }
    }
    append("- registers changed (input vs exit of the first call):");
    if (isEmpty(c.RegisterDelta)) {
        append("  (none observed)");
    }
    else {
        const enumerator_2 = getEnumerator(c.RegisterDelta);
        try {
            while (enumerator_2["System.Collections.IEnumerator.MoveNext"]()) {
                const forLoopVar_1 = enumerator_2["System.Collections.Generic.IEnumerator`1.get_Current"]();
                append(toText(printf("  %s: %04X -> %04X"))(forLoopVar_1[0])(forLoopVar_1[1])(forLoopVar_1[2]));
            }
        }
        finally {
            disposeSafe(enumerator_2);
        }
    }
    append(toText(printf("- self-modifying: %b"))(c.SelfModifying));
    append((arg_18 = (isEmpty(c.ResumeAddresses) ? "-" : join(", ", map((clo_19 = toText(printf("%04X")), clo_19), c.ResumeAddresses))), toText(printf("- resume addresses (PC after each call site): %s"))(arg_18)));
    append("");
    append("## Trace evidence");
    append("- input register samples at entry (max 5):");
    const enumerator_3 = getEnumerator(c.InputSamples);
    try {
        while (enumerator_3["System.Collections.IEnumerator.MoveNext"]()) {
            append("  " + regLine(enumerator_3["System.Collections.Generic.IEnumerator`1.get_Current"]()));
        }
    }
    finally {
        disposeSafe(enumerator_3);
    }
    append("");
    append("## Machine API surface this routine touches");
    const enumerator_4 = getEnumerator(c.ApiHints);
    try {
        while (enumerator_4["System.Collections.IEnumerator.MoveNext"]()) {
            append("- " + enumerator_4["System.Collections.Generic.IEnumerator`1.get_Current"]());
        }
    }
    finally {
        disposeSafe(enumerator_4);
    }
    append("");
    append("## Jetpac2.Core.Machine API (exact members)");
    append("- Fetch(): 4 T-states, advances PC and R");
    append("- ReadImm() / ReadImm16(): operand bytes, advance PC");
    append("- Write(addr, value): 3 T-states, fires memory-write handlers");
    append("- Read(addr): 3 T-states");
    append("- Regs.Get / Regs.Set (R8.A|F|B|C|D|E|H|L | R16.AF|BC|DE|HL|IX|IY|SP)");
    append("- Alu.add8/sub8/cmp8/inc8/dec8/and8/or8/xor8/...: struct(result, flags)");
    append("- Flags() / SetFlags(flags)");
    append("- Branch(offset): relative PC move; Regs.SetPc(value): absolute");
    append("- Push16/Pop16, PassTime(tstates)");
    append("- In(port) / Out(port, value)");
    append("");
    append("## Conventions (Jetpac3.Core/LiftedRoutines.fs)");
    append("type LiftedRoutine = { Name: string; EntryAddresses: int list; Execute: Jetpac2.Core.Machine -> unit; ImplementationMode = Lifted }");
    append("- registryHook: address -> Some execute | None; add the routine to the registry list.");
    append(toText(printf("- Instruction granular: match m.Regs.Pc() with | 0x%04X -> ... ; the fetches must mirror the original byte-for-byte (m.Fetch(); m.ReadImm(); m.PassTime(n)) so the R register and T-states match exactly."))(c.Entry));
    append("- Implement shared loops once and thin entry stubs for each entry address.");
    append("- For any resume PC inside the span you do not handle: failwithf \"unhandled resume at %04X\".");
    append("- Idiomatic F#; no .[ ] array syntax; no comments that restate the code.");
    append("");
    append("## Acceptance");
    append("- Differential test (oracle vs port) over 300 frames of the default script: 0 diffs.");
    append("- Per-instruction T-states identical (R-register fidelity).");
    append("- Reply with ONLY the F# code in a single ```fsharp block: the routine(s) and the updated registry list.");
    return toString(sb);
}

