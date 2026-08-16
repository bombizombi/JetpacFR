Convert the Z80 routine at 0x6254 (span 0x6254-0x625A) in the 16K Spectrum game Jetpac into idiomatic F#, replacing the generated instruction-table implementation. The converted code runs inside Jetpac2.Core.Machine and MUST be instruction-granular (one match arm per resume PC) so an interrupt can land mid-routine at the exact T-state.

## Exact disassembly (with execution counts from the trace window)
6254  SET 7,(HL)                    x82       ~15 tstates
6256  INC HL                        x82       ~6 tstates
6257  RES 7,(HL)                    x82       ~15 tstates
6259  INC HL                        x82       ~6 tstates
625A  RET                           x82       ~10 tstates

## Behavioural contract (observed, not guessed)
- call count: 82
- inclusive T-states: 4264 (avg 52 per call)
- memory writes (lo..hi -> value x count; these are memory CHANGES, old<>new, not every store):
  5CEE..5CEE -> 47 (x82)
  6290..6290 -> C7 (x1)
  6292..6292 -> C7 (x1)
- registers changed (input vs exit of the first call):
  (none observed)
- self-modifying: false
- resume addresses (PC after each call site): 6240, 6247

## Trace evidence
- input register samples at entry (max 5):
  AF=0054 BC=00E2 DE=0095 HL=6290 IX=5C7A IY=5C3A SP=5CF0 I=3F R=7B

## Machine API surface this routine touches
- Alu.rotate8 / bit / res / set
- Fetch/ReadImm (immediates), Regs.Get/Set (R16/R8)
- Pop16 + Regs.SetPc (return)

## Jetpac2.Core.Machine API (exact members)
- Fetch(): 4 T-states, advances PC and R
- ReadImm() / ReadImm16(): operand bytes, advance PC
- Write(addr, value): 3 T-states, fires memory-write handlers
- Read(addr): 3 T-states
- Regs.Get / Regs.Set (R8.A|F|B|C|D|E|H|L | R16.AF|BC|DE|HL|IX|IY|SP)
- Alu.add8/sub8/cmp8/inc8/dec8/and8/or8/xor8/...: struct(result, flags)
- Flags() / SetFlags(flags)
- Branch(offset): relative PC move; Regs.SetPc(value): absolute
- Push16/Pop16, PassTime(tstates)
- In(port) / Out(port, value)

## Conventions (Jetpac3.Core/LiftedRoutines.fs)
type LiftedRoutine = { Name: string; EntryAddresses: int list; Execute: Jetpac2.Core.Machine -> unit; ImplementationMode = Lifted }
- registryHook: address -> Some execute | None; add the routine to the registry list.
- Instruction granular: match m.Regs.Pc() with | 0x6254 -> ... ; the fetches must mirror the original byte-for-byte (m.Fetch(); m.ReadImm(); m.PassTime(n)) so the R register and T-states match exactly.
- Implement shared loops once and thin entry stubs for each entry address.
- For any resume PC inside the span you do not handle: failwithf "unhandled resume at %04X".
- Idiomatic F#; no .[ ] array syntax; no comments that restate the code.

## Acceptance
- Differential test (oracle vs port) over 300 frames of the default script: 0 diffs.
- Per-instruction T-states identical (R-register fidelity).
- Reply with ONLY the F# code in a single ```fsharp block: the routine(s) and the updated registry list.
