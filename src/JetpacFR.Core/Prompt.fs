namespace JetpacFR.Core

open System.Text

/// Generates the self-contained follow-up prompt for a mined routine: exact
/// disassembly with execution counts, behavioural contract, trace evidence,
/// the Machine API reference and the LiftedRoutines conventions, ending with
/// the acceptance criteria. Designed to be pasted into the model chat
/// verbatim so the model can write the lifted F#.
module Prompt =

    let private regLine (s: RegSnapshot) =
        sprintf
            "AF=%04X BC=%04X DE=%04X HL=%04X IX=%04X IY=%04X SP=%04X I=%02X R=%02X"
            s.Af
            s.Bc
            s.De
            s.Hl
            s.Ix
            s.Iy
            s.Sp
            s.I
            s.R

    let generate (c: Contract.RoutineContract) : string =
        let sb = StringBuilder()
        let append (s: string) = sb.AppendLine s |> ignore

        append (
            sprintf
                "Convert the Z80 routine at 0x%04X (span 0x%04X-0x%04X) in the 16K Spectrum game Jetpac into idiomatic F#, replacing the generated instruction-table implementation. The converted code runs inside Jetpac2.Core.Machine and MUST be instruction-granular (one match arm per resume PC) so an interrupt can land mid-routine at the exact T-state."
                c.Entry
                c.SpanLo
                c.SpanHi
        )

        append ""
        append "## Exact disassembly (with execution counts from the trace window)"

        for insn in c.Disassembly do
            append (sprintf "%04X  %-28s  x%-7d  ~%d tstates" insn.Address insn.Text insn.Executions insn.AvgCycles)

        append ""
        append "## Behavioural contract (observed, not guessed)"
        append (sprintf "- call count: %d" c.CallCount)

        append (
            sprintf
                "- inclusive T-states: %d (avg %d per call)"
                c.InclusiveTStates
                (int (c.InclusiveTStates / max 1L (int64 c.CallCount)))
        )

        append "- memory writes (lo..hi -> value x count; these are memory CHANGES, old<>new, not every store):"

        if List.isEmpty c.WriteRanges then
            append "  (none observed)"
        else
            for lo, hi, value, count in c.WriteRanges do
                append (sprintf "  %04X..%04X -> %02X (x%d)" lo hi value count)

        append "- registers changed (input vs exit of the first call):"

        if List.isEmpty c.RegisterDelta then
            append "  (none observed)"
        else
            for name, before, after in c.RegisterDelta do
                append (sprintf "  %s: %04X -> %04X" name before after)

        append (sprintf "- self-modifying: %b" c.SelfModifying)

        append (
            sprintf
                "- resume addresses (PC after each call site): %s"
                (if List.isEmpty c.ResumeAddresses then
                     "-"
                 else
                     c.ResumeAddresses |> List.map (sprintf "%04X") |> String.concat ", ")
        )

        append ""
        append "## Trace evidence"
        append "- input register samples at entry (max 5):"

        for s in c.InputSamples do
            append ("  " + regLine s)

        append ""
        append "## Machine API surface this routine touches"

        for hint in c.ApiHints do
            append ("- " + hint)

        append ""
        append "## Jetpac2.Core.Machine API (exact members)"
        append "- Fetch(): 4 T-states, advances PC and R"
        append "- ReadImm() / ReadImm16(): operand bytes, advance PC"
        append "- Write(addr, value): 3 T-states, fires memory-write handlers"
        append "- Read(addr): 3 T-states"
        append "- Regs.Get / Regs.Set (R8.A|F|B|C|D|E|H|L | R16.AF|BC|DE|HL|IX|IY|SP)"
        append "- Alu.add8/sub8/cmp8/inc8/dec8/and8/or8/xor8/...: struct(result, flags)"
        append "- Flags() / SetFlags(flags)"
        append "- Branch(offset): relative PC move; Regs.SetPc(value): absolute"
        append "- Push16/Pop16, PassTime(tstates)"
        append "- In(port) / Out(port, value)"
        append ""
        append "## Conventions (Jetpac3.Core/LiftedRoutines.fs)"

        append
            "type LiftedRoutine = { Name: string; EntryAddresses: int list; Execute: Jetpac2.Core.Machine -> unit; ImplementationMode = Lifted }"

        append "- registryHook: address -> Some execute | None; add the routine to the registry list."

        append (
            sprintf
                "- Instruction granular: match m.Regs.Pc() with | 0x%04X -> ... ; the fetches must mirror the original byte-for-byte (m.Fetch(); m.ReadImm(); m.PassTime(n)) so the R register and T-states match exactly."
                c.Entry
        )

        append "- Implement shared loops once and thin entry stubs for each entry address."
        append "- For any resume PC inside the span you do not handle: failwithf \"unhandled resume at %04X\"."
        append "- Idiomatic F#; no .[ ] array syntax; no comments that restate the code."
        append ""
        append "## Acceptance"
        append "- Differential test (oracle vs port) over 300 frames of the default script: 0 diffs."
        append "- Per-instruction T-states identical (R-register fidelity)."

        append
            "- Reply with ONLY the F# code in a single ```fsharp block: the routine(s) and the updated registry list."

        sb.ToString()
