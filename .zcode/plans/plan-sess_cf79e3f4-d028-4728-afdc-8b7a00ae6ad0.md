Integrate the pasted SkoolKit static-analysis code into control-file creation, as a new Core module + hooks at the points where a default control file gets created. Decisions (user-confirmed): text blocks become Data + a Range comment holding the decoded string; scope is the static path only (no code-map/trace seeding for now).

## 1. New module `src/JetpacFR.Core/CtlGen.fs` (~250 lines)

A port of `generateCtlsWithoutCodeMap` from the pasted code, adapted to this repo's types. Public API:

- `CtlGen.analyze : memory:byte[] -> start:int -> endExcl:int -> Result` where `Result = { Blocks: Block list; TextNotes: (int * int * string) list }` (Block from GameProject.fs).
- `CtlGen.ramSpan : memory:byte[] -> int * int` — the default analysis span (0x4000 to last non-zero byte + 1, mirroring loadOrCreate's original.bin rule).
- `CtlGen.toControlFile : Result -> entryPc:int -> start:int -> endExcl:int -> ControlFile` — assembles a complete ControlFile (ImageFile="original.bin", ActiveVersion=-1, no symbols, TextNotes as `Range` comments `text: <decoded string>`), for the caller to save.

Ported pieces and their replacements for the stubbed SkoolKit `External` API:

- **Decode loop**: iterate with `Disasm.disasmLength`; per instruction capture address/size/op-bytes.
- **END (terminal) detection**: `Z80Flow.classify` matching `Jump | Branch | Return` (Call and Linear are not terminal). Drive-by fix included: add the missing RET cc opcodes 0xE0/0xE8/0xF0/0xF8 to Z80Flow.fs:68-72 (CallOps.isRet already covers them).
- **`catchData`**: same logic; op identity = normalized (prefix-group, opcode) pair with DD/FD collapsed and condition bits masked; `MaxCount` = config knob (default 4) with the original's harmless-pair exception (count=2 with first byte 0x66/0x6E).
- **NOP-prefix stripping, all-zero → zero block, adjacent data/zero joining**: direct ports. Kind mapping to the repo's three kinds: 'c'→Code, 'b'/'t'→Data, 's'→Gap.
- **`getTextBlocks`/`isAllowedText`**: direct port with a `Config` record (printable-ASCII TextChars, TextMinLengthCode=3, TextMinLengthData=2, Words=[]). Text ranges emit Data blocks + the decoded string as a Range comment.
- Blocks emitted sorted, non-overlapping, covering [start, endExcl), auto-names (`block_%04X`) — satisfying `GameProject.loadControl`'s validation invariants.

**Not ported** (stated in the module doc): `readMap` textual/binary map formats, the with-code-map algorithm + referrer promotion (deferred until the trace-seeded refinement), RST sub-ctls, comment generator, `writeCtl` textual output (SkoolKit .ctl export already exists as `SkoolCtl.export`).

## 2. App.fs wiring — `maybeGenerateDefaultControl`

- New helper called from `consumeFinishedBoot` right after `session <- Some loadedSession` — all boot paths converge there (autoplay/record/live presets, legacy single-slot, parked-in-menu; manual boot reaches it after "Set as game entry" relaunches warm).
- Guard: only when `control.json` is absent on disk AND the in-memory `control` is still the pristine empty stub (no comments/symbols, single ≤16-byte block), so an in-memory import is never stomped.
- Inputs: `loadedSession.Memory` (game-entry 64K snapshot, guaranteed valid once the boot completed), span from `CtlGen.ramSpan`, EntryPc = `loadedSession.Regs.Pc()`.
- Persist immediately via `ControlFile.save dir generated` — the point is a default file on disk for new projects (`loadOrCreate` never writes; this is why the launcher's "control:" column stays "no" today). Status: `control file created: N blocks (x code / y data / z gap)`, then `refreshControlLists()` + `syncMapData()`.

## 3. "New ctrl" button (App.fs)

Replace `ControlFile.empty 0x4000 0x10000` with: generate from the current session's `Memory` when one is installed; else `EntryCache.tryLoad` of the current game's rom/tzx; else fall back to `empty`. Result marked `Dirty = true` so a plain "Save ctrl" persists it.

## 4. Tests (tests/JetpacFR.Core.Tests)

Following the existing runner patterns in Program.fs — synthetic 64K snapshots:
- code ending in RET → one Code block ending after the RET; NOP prefix → Gap + Code resplit;
- repeated-op run past MaxRepeat → Data block (catchData), including the 0x66/0x6E harmless-pair exception;
- ASCII run ≥ TextMinLengthData → Data block + text Range comment with decoded content;
- all-zero region → Gap; adjacent Data/Gap join;
- invariant: analyze → toControlFile → toJson/fromJson round-trip → `GameProject.loadControl`-style validation (sorted, non-overlapping, in span).

## 5. Housekeeping

- Add `CtlGen.fs` to JetpacFR.Core.fsproj after `ControlFile.fs` (compile order verified: Disasm, Z80Flow, GameProject, ControlFile all precede).
- `dotnet fantomas` on every touched file; build Core + Desktop; run the Core test project.

Files touched: NEW src/JetpacFR.Core/CtlGen.fs; src/JetpacFR.Core/JetpacFR.Core.fsproj; src/JetpacFR.Core/Z80Flow.fs (RET cc fix); src/JetpacFR.Desktop/App.fs; tests/JetpacFR.Core.Tests/Program.fs.

Deferred (explicitly, per your choices): trace-seeded with-code-map refinement (later, as an explicit re-analyze action), a dedicated Text BlockKind, the web app's control storage, RST sub-directives.