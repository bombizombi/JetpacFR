Instant tape boot via ROM-trap flash loading (user-confirmed mechanism), with the old slow boot kept as a launcher option. 48K Spectrum only. The pasted SkoolKit conversion's tables/config machinery are NOT needed with this approach — the real ROM loader sets every system variable itself; we only accelerate the byte transfer. (Its CLI parts — SimLoadConfig, arg parsing, filenames — have no in-process role either way.)

## 1. New module `src/Jetpac3.Core/FastBoot.fs`

`FastBoot.bootToEntryFast (romPath) (tzxPath) (onFrame) : Spectrum48 * int64` — same contract as `Boot.bootToEntry`, reached in ~1-2s instead of ~40s:

- Own `Spectrum48` oracle with ROM loaded and tape inserted but **never played**.
- Block list: `Jetpac.Core.Tape.parseTzx` → data payloads (flag + data + checksum split) in tape order, tracked by index.
- Drive frames instruction-by-instruction via the public `spec.DebugZ80.ExecuteOne()` (the same API Boot.fs's tail already uses), managing 70000-cycle frame boundaries from `z80.CycleCount()`. Replicate the boot macro's key presses via public `spec.SetKey` (frame indices copied from `Spectrum48.RunBootMacro`, Spectrum.fs:314-334) so `LOAD ""` gets typed without touching private members.
- **The trap**: when PC reaches LD-BYTES (0x0556, and the 0x056C variant) with carry set (load): write the next block's data bytes directly to the destination span, set the documented LD-BYTES success exit state (registers/flags, RET semantics — exact register contract from the 48K ROM disassembly during implementation), advance the block index, continue. Carry clear (verify) is not trapped. Because the real ROM performs all header/data dispatch, BASIC chaining, and sysvar bookkeeping, the resulting entry state is byte-identical to the slow boot (verified by test). Cycles/beeper/tapeEar fields will differ (tape never played) — harmless, sessions are cycle-relative.
- Termination: same as Boot.fs — run until PC ≥ 0x4000 (first RAM instruction), then `SaveState()`.
- **Automatic fallback**: if no trap fires within a bounded window after the loader starts (custom bit-banging loaders), abandon and delegate to the existing `Boot.bootToEntry` (old speed, still correct). Fast boot can never be worse than the status quo.
- Progress: call `onFrame (screen, frame)` per frame like the slow boot (the boot screen renders).
- Add to Jetpac3.Core.fsproj after Boot.fs.

## 2. EntryCache mode separation (src/JetpacFR.Core/Session.fs)

The cache key currently hashes only `rom=..\ntzx=..\n` — fast and oracle boots must not share a slot:
- `marker` gains an optional third line: fast slot = `rom=..\ntzx=..\nboot=fast\n`; the oracle slot keeps the exact legacy marker, so the 5 existing warm caches stay valid for the slow path (no mass re-boot).
- `EntryCache.tryLoad`/`save` gain `?fast: bool` (default false = legacy behavior; all existing callers unchanged).

## 3. TraceSession seam (src/JetpacFR.Core/Session.fs)

- Constructor gains `?fastBoot: bool` (default false). `loadEntryState()`: `EntryCache.tryLoad rom tzx fastBoot`; on cold + fastBoot → `FastBoot.bootToEntryFast` (which internally falls back to the oracle) → `EntryCache.save ... fast=true`. Minimal seam; the emulator construction stays in MainWindow per AGENTS.md.

## 4. App.fs + LauncherView wiring

- `let mutable forceSlowBoot = false` (one-shot). `launchGame` reads-and-clears it and passes `fastBoot = not slow` to the `Task.Run` TraceSession sites (App.fs:349, 357; the program-image site at 372 and manual boot stay untouched). The warm-check at App.fs:344 tries both slots (fast slot first when not forced slow). Cold-status text: "flash-loading tape...". `boot:"auto"` projects default to the fast path; "manual"/"program" unchanged.
- LauncherView: `LauncherContext` gains `BootSlow: GameManifest -> unit`; a small "Slow tape boot" button (Rescan-style: Width≈120, Height≈24) in the bottom-left `leftBox` next to Rescan (LauncherView.fs:317-334), acting on the selected game, with a tooltip explaining it emulates the real tape (~40s first time).
- App.fs picker wires it: `forceSlowBoot <- true; launchPhase <- "emulator"` then relaunch the selected game through `launchGame` directly (always relaunching, even when it is the current game, so the button is never a no-op) and swap the emulator content in (mirroring `openProject`'s Some-branch, App.fs:3479-3497).
- Web shell untouched.

## 5. Tests (`--test fastboot`, wired into the harness + `all`)

- **Equivalence gate**: `FastBoot.bootToEntryFast` vs `Boot.bootToEntry` on Jetpac — 64K memory byte-equal and entry PC equal (the slow side can come from the existing warm oracle cache when present).
- **Timing**: fast boot completes < ~5s.
- **Cache separation**: fast and oracle slots are distinct; a legacy cache still loads via the default flag.
- **Block-blit unit test**: the trap's write helper lands block bytes at the destination, strips flag/checksum, honors the length.
- Fantomas all touched files; build Core, Jetpac3.Core, Desktop; run `--test fastboot` plus `--test boot` and `--test flow` as regression neighbors.

Files touched: NEW src/Jetpac3.Core/FastBoot.fs; src/Jetpac3.Core/Jetpac3.Core.fsproj; src/JetpacFR.Core/Session.fs; src/JetpacFR.Desktop/App.fs; src/JetpacFR.Desktop/LauncherView.fs; tests/JetpacFR.Core.Tests/Program.fs.

Explicitly out of scope: other machines (128K etc.), the pasted SkoolKit sysvar/patch tables, Python.NET, TAP/PZX input (TZX only, matching the current tape stack), and the web shell.