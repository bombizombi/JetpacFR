namespace JetpacFR.Core

open System.IO

/// Oracle-driven boot for games whose entry cannot be auto-detected
/// (manifest boot mode "manual"): the user watches the loader run in the
/// oracle and presses "Set as game entry" when the game is ready; the
/// state is captured into the entry cache and the port session takes over
/// on the next launch (warm start). Works for ANY tape game.
type ManualBoot(romPath: string, tzxPath: string) =
  let spec = Jetpac.Core.Spectrum48()

  do
    spec.LoadRom(File.ReadAllBytes romPath)
    spec.InsertTape(File.ReadAllBytes tzxPath)

  member _.RunFrame() = spec.RunFrame()
  member _.SetKey(row: int, bit: int, pressed: bool) = spec.SetKey(row, bit, pressed)
  member _.ScreenBuffer = spec.ScreenBuffer
  member _.TapePlaying = spec.DebugTapePlaying
  member _.FrameCount = spec.FrameCount

  /// Snapshot the current oracle state as the game entry point; subsequent
  /// TraceSession constructions find it via the warm-start cache.
  member this.CaptureEntry() =
    let mem, state = spec.SaveState()
    EntryCache.save romPath tzxPath mem state
