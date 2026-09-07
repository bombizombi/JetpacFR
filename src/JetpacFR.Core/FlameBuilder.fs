namespace JetpacFR.Core

open System
open System.Collections.Generic
open Jetpac2.Core
open Jetpac3.Core

/// Headless flame-graph builder: re-executes a frame range of a saved
/// recording (the .jst per-frame timeline + key script) on a private port
/// machine, recording every instruction exactly like the live session does,
/// then walks the entries into a FlameWindow.
///
/// Determinism: the builder mirrors TraceSession.RunFrame's traced path -
/// same LiftedRoutines override hook, same key application (the replay
/// convention: a frame's logged key events apply before the frame runs),
/// same interrupt marker recording - so re-executed entries match what the
/// live session recorded for the same frames, byte for byte.
///
/// Frames run from `firstFrame` (>= StartFrame + 1: executing frame f needs
/// the stored state after f - 1; frame 0's predecessor state does not exist
/// in a recording) through `lastFrame`.
module FlameBuilder =

  /// Build the flame window for frames [firstFrame .. lastFrame]. Returns
  /// None when the range is not covered by the timeline or the build was
  /// cancelled. `progress` reports frames completed (on the caller's
  /// thread - run this on a worker and marshal if needed).
  let buildFrames
    (timeline: StateTimeline)
    (keyLog: KeyLog)
    (firstFrame: int)
    (lastFrame: int)
    (progress: int -> unit)
    (cancel: unit -> bool)
    : FlameWindow option =
    if lastFrame < firstFrame then Some { FlameWindow.empty with FirstFrame = firstFrame }
    elif firstFrame <= timeline.StartFrame || lastFrame > timeline.EndFrame then None
    elif cancel () then None
    else
      Jetpac2.Core.Z80Table.EnsureInstalled ()
      let port = Jetpac2.Core.Machine ()
      // Parity with the live session: lifted routines run here too, so the
      // re-executed instruction stream is the one that was recorded.
      port.Override <- LiftedRoutines.registryHook
      let entries = ResizeArray<TraceEntry> (1 <<< 16)
      let frameBounds = ResizeArray<uint32> (max 1 (lastFrame - firstFrame + 1))
      let buffer = Array.zeroCreate<byte> 0x10000
      let mutable cancelled = false
      let mutable f = firstFrame
      while not cancelled && f <= lastFrame do
        let text, keys = timeline.RestoreInto(f - 1, buffer)
        port.LoadState(buffer, text)
        port.Keyboard.Load keys
        for e in keyLog.ForFrame f do
          port.SetKey(e.Row, e.Bit, e.Pressed)
        let frameEnd = port.FrameEnd
        let mutable step = 0
        while port.CycleCount() < frameEnd do
          let irq = port.IrqPending && port.Iff1
          let pc = port.Regs.Pc ()
          let cyclesBefore = port.CycleCount ()
          if irq then
            let vector =
              match port.IrqMode with
              | 2 ->
                let addr = 0xFF ||| ((port.Regs.I () <<< 8) &&& 0xFF00)
                let lo = int port.Memory[addr &&& 0xFFFF]
                let hi = int port.Memory[(addr + 1) &&& 0xFFFF]
                (lo ||| (hi <<< 8)) &&& 0xFFFF
              | _ -> 0x38
            entries.Add
              { Pc = uint16 vector
                B0 = 0uy; B1 = 0uy; B2 = 0uy; B3 = 0uy
                Target = uint16 vector
                Tick = uint32 cyclesBefore
                Length = 0uy
                Cycles = 7uy
                FlagsBefore = 0uy
                FlagsAfter = 0uy
                Taken = 1uy }
            let vlen = Disasm.disasmLength port.Memory vector
            let m = port.Memory
            let vb0 = m[vector &&& 0xFFFF]
            let vb1 = m[(vector + 1) &&& 0xFFFF]
            let vb2 = m[(vector + 2) &&& 0xFFFF]
            let vb3 = m[(vector + 3) &&& 0xFFFF]
            port.Step ()
            let after = port.Regs.Pc ()
            let cycles = int (port.CycleCount () - cyclesBefore)
            entries.Add
              { Pc = uint16 vector
                B0 = vb0; B1 = vb1; B2 = vb2; B3 = vb3
                Target = uint16 after
                Tick = uint32 cyclesBefore
                Length = uint8 vlen
                Cycles = uint8 (min 255 cycles)
                FlagsBefore = 0uy
                FlagsAfter = 0uy
                Taken = (if after <> ((vector + vlen) &&& 0xFFFF) then 1uy else 0uy) }
          else
            let len = Disasm.disasmLength port.Memory pc
            let m = port.Memory
            let b0 = m[pc &&& 0xFFFF]
            let b1 = m[(pc + 1) &&& 0xFFFF]
            let b2 = m[(pc + 2) &&& 0xFFFF]
            let b3 = m[(pc + 3) &&& 0xFFFF]
            port.Step ()
            let after = port.Regs.Pc ()
            let cycles = int (port.CycleCount () - cyclesBefore)
            entries.Add
              { Pc = uint16 pc
                B0 = b0; B1 = b1; B2 = b2; B3 = b3
                Target = uint16 after
                Tick = uint32 cyclesBefore
                Length = uint8 len
                Cycles = uint8 (min 255 cycles)
                FlagsBefore = 0uy
                FlagsAfter = 0uy
                Taken = (if after <> ((pc + len) &&& 0xFFFF) then 1uy else 0uy) }
          step <- step + 1
        port.FrameEnd <- frameEnd + 69888L
        frameBounds.Add(uint32 (port.CycleCount ()))
        progress (f - firstFrame + 1)
        f <- f + 1
        if cancel () then cancelled <- true
      if cancelled then None
      else
        Some(FlameWalker.walk firstFrame (frameBounds.ToArray()) (entries.ToArray()))

/// LRU-ish cache of built flame windows with the detail tier kept only for
/// small windows. Keyed by frame range; a query hit requires a window fully
/// covering [firstFrame .. lastFrame].
type FlameCache(?detailFrameBudget: int) =

  let detailBudget = defaultArg detailFrameBudget 8
  let windows = Dictionary<int * int, FlameWindow>()
  let order = ResizeArray<int * int>()
  let capacity = 8

  member _.Clear () =
    windows.Clear ()
    order.Clear ()

  member _.TryGet(firstFrame: int, lastFrame: int) : FlameWindow option =
    let mutable hit = None
    for kv in windows do
      let (a, b) = kv.Key
      if a <= firstFrame && lastFrame <= b then hit <- Some kv.Value
    match hit with
    | Some w ->
      // Refresh LRU position.
      let key = (w.FirstFrame, w.FirstFrame + w.FrameTicks.Length - 1)
      order.Remove key |> ignore
      order.Add key
      Some w
    | None -> None

  member _.Add(window: FlameWindow) : FlameWindow =
    if window.FirstFrame < 0 then window
    else
      let key = (window.FirstFrame, window.FirstFrame + window.FrameTicks.Length - 1)
      let stored = if window.FrameTicks.Length <= detailBudget then window else FlameWindow.trim window
      if not (windows.ContainsKey key) then
        order.Add key
      windows[key] <- stored
      while order.Count > capacity do
        let oldest = order[0]
        order.RemoveAt 0
        windows.Remove oldest |> ignore
      stored
