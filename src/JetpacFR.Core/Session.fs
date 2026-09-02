namespace JetpacFR.Core

open System
open System.IO

/// Locate assets by walking up from the app base directory to the first
/// directory containing an assets folder with the requested file. The Jetpac3
/// resolver only searches for a "Jetpac4" ancestor, which this project (in
/// Jetpac_FreeRange) never has.
module LocalAssets =
  let find (name: string) : string =
    let rec walk (dir: DirectoryInfo) =
      let candidate = Path.Combine(dir.FullName, "assets", name)
      if File.Exists candidate then candidate
      else
        match dir.Parent with
        | null -> failwithf "asset %s not found; searched up from %s" name AppContext.BaseDirectory
        | parent -> walk parent
    walk (DirectoryInfo AppContext.BaseDirectory)

/// Persistent game-entry state. The first launch boots the oracle to the game
/// entry (the slow part) and saves memory.bin + state.txt next to the app;
/// subsequent launches load that state straight into the port machine, which
/// is exact because the entry state is a full 64K memory + register snapshot.
/// The cache is keyed by the ROM/TZX hashes so a changed game invalidates it.
module EntryCache =

  let private sha256 (bytes: byte[]) =
    use sha = System.Security.Cryptography.SHA256.Create()
    sha.ComputeHash bytes |> Array.map (fun b -> b.ToString("x2")) |> String.concat ""

  /// One cache slot per game, keyed by the ROM hash: switching games must
  /// not wipe another game's booted entry state.
  let private dir (romPath: string) =
    let h = sha256 (File.ReadAllBytes romPath)
    Path.Combine(AppContext.BaseDirectory, "entry-cache", h.Substring(0, 12))

  let private memoryPath romPath = Path.Combine(dir romPath, "memory.bin")
  let private statePath romPath = Path.Combine(dir romPath, "state.txt")
  let private metaPath romPath = Path.Combine(dir romPath, "meta.txt")

  let private marker (romPath: string) (tzxPath: string) =
    sprintf "rom=%s\ntzx=%s\n" (sha256 (File.ReadAllBytes romPath)) (sha256 (File.ReadAllBytes tzxPath))

  /// Some(mem, state) when a cache exists, matches the current assets, and is
  /// structurally intact; None otherwise (cold boot needed).
  let tryLoad (romPath: string) (tzxPath: string) : (byte[] * string) option =
    try
      let mp = memoryPath romPath
      if File.Exists mp && File.Exists (statePath romPath) && File.Exists (metaPath romPath) then
        let current = marker romPath tzxPath
        let stored = File.ReadAllText (metaPath romPath)
        if stored = current then
          let mem = File.ReadAllBytes mp
          if mem.Length = 0x10000 then Some(mem, File.ReadAllText (statePath romPath))
          else None
        else None
      else None
    with _ -> None

  let save (romPath: string) (tzxPath: string) (mem: byte[]) (state: string) =
    try
      Directory.CreateDirectory (dir romPath) |> ignore
      File.WriteAllBytes(memoryPath romPath, mem)
      File.WriteAllText(statePath romPath, state)
      File.WriteAllText(metaPath romPath, marker romPath tzxPath)
    with _ -> () // the cache is an optimization; a failed write must not break startup

/// The engine: the Jetpac2 port machine driven instruction-by-instruction,
/// recording every step into a TraceRecorder. The JetpacFSharp oracle is only
/// involved on a cold start (boot to the game entry, then cache the state);
/// after that the port runs alone, so the screen, the sound and the trace all
/// describe the same execution.
type TraceSession(romPath: string, tzxPath: string, capacity: int, ?onFrame: byte[] * int -> unit) =

  let port = Jetpac2.Core.Machine()
  let recorder = TraceRecorder(capacity, 512)
  let mutable frame = 0
  let mutable warmStart = false
  let snapshotInterval = 64
  let history = FrameHistory(300, 1_500_000_000L)
  let keyLog = KeyLog()
  let mutable replayMode = false
  let mutable replayEndFrame = -1
  let mutable replayFinished = false
  let mutable pendingReplayKeys: KeyEvent list = []

  /// Capture the port state + keyboard matrix into the history store.
  let captureState () =
    let mem, text = port.SaveState()
    history.Capture(frame, mem, text, port.Keyboard.ToBytes())

  /// Release every still-down matrix cell. A recording can end (or be
  /// aborted) mid-hold, and a script-driven press without a matching
  /// release must never leak into live control.
  let releaseAllKeys () =
    for row, bit in port.Keyboard.PressedCells () do
      port.SetKey(row, bit, false)

  /// Put the port into the game-entry state. Warm: load the cached snapshot.
  /// Cold: boot the oracle to the entry and cache its state for next time.
  let loadEntryState () =
    match EntryCache.tryLoad romPath tzxPath with
    | Some (mem, state) ->
      try
        port.LoadState(mem, state)
        warmStart <- true
      with _ ->
        let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath onFrame
        let mem, state = oracle.SaveState()
        EntryCache.save romPath tzxPath mem state
        port.LoadState(mem, state)
        warmStart <- false
    | None ->
      let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath onFrame
      let mem, state = oracle.SaveState()
      EntryCache.save romPath tzxPath mem state
      port.LoadState(mem, state)
      warmStart <- false

  let snapshotOf () =
    let r = port.Regs
    { Tick = uint32 (port.CycleCount())
      Af = uint16 (r.Get Jetpac2.Core.R16.AF)
      Bc = uint16 (r.Get Jetpac2.Core.R16.BC)
      De = uint16 (r.Get Jetpac2.Core.R16.DE)
      Hl = uint16 (r.Get Jetpac2.Core.R16.HL)
      Af2 = uint16 (r.Get Jetpac2.Core.R16.AF_)
      Bc2 = uint16 (r.Get Jetpac2.Core.R16.BC_)
      De2 = uint16 (r.Get Jetpac2.Core.R16.DE_)
      Hl2 = uint16 (r.Get Jetpac2.Core.R16.HL_)
      Ix = uint16 (r.Ix())
      Iy = uint16 (r.Iy())
      Sp = uint16 (r.Sp())
      Pc = uint16 (r.Pc())
      I = uint8 (r.I())
      R = uint8 (r.R()) }

  do
    Jetpac2.Core.Z80Table.EnsureInstalled()
    // Run lifted routines in the live session too: per-address dispatch, so
    // each Step still executes one logical instruction and the trace records
    // exactly what ran (same bytes/lengths as the generated layer).
    port.Override <- Jetpac3.Core.LiftedRoutines.registryHook
    port.AddMemoryWriteHandler(fun e ->
      recorder.RecordWrite
        { Tick = uint32 e.Tick
          Address = uint16 e.Address
          OldValue = e.OldValue
          NewValue = e.NewValue })
    port.AddOutHandler(fun p v ->
      recorder.RecordPort
        { Tick = uint32 (port.CycleCount())
          Port = uint16 p
          Value = uint8 (v &&& 0xFF)
          Border = uint8 (v &&& 7) })
    loadEntryState ()
    captureState () // frame 0 anchor: the slider's always-reachable minimum
    recorder.RecordSnapshot(snapshotOf ())


  /// True when the entry state came from the cache (no oracle boot needed).
  member this.WarmStart = warmStart
  member this.Frame = frame
  member this.Recorder = recorder
  member this.CycleCount = port.CycleCount()
  member this.Regs = port.Regs
  member this.Memory = port.Memory

  /// The live matrix, e.g. for shells/tests asserting a clean handoff.
  member this.Keyboard = port.Keyboard

  /// Rendered frame (BGRA 320x256) from the port's video state.
  member this.ScreenBuffer = port.Video.BlitTo()

  /// Drain the frame's beeper transitions and synthesize the 882 samples for
  /// the just-executed frame.
  member this.DrainBeeperSamples(frameStart: int64) : float32[] =
    let trace = port.BeeperTrace |> Seq.toList
    port.BeeperTrace.Clear()
    Jetpac2.Core.Beeper.ToSamples trace frameStart (port.CycleCount() - frameStart)

  member this.SetKey(row: int, bit: int, pressed: bool) =
    if replayMode then () // live keys ignored during scripted replay
    else
      keyLog.Add { Frame = frame; Row = row; Bit = bit; Pressed = pressed }
      port.SetKey(row, bit, pressed)

  member this.Replaying = replayMode
  member this.ReplayEndFrame = replayEndFrame
  member this.History = history
  member this.KeyLog = keyLog

  /// True when a replay reached the end of the recording in the last
  /// RunFrame; the UI pauses the frame timer and hands control back.
  member this.ReplayFinished = replayFinished

  /// Rewind PREVIEW: restore the machine (registers, memory, keyboard) to
  /// the state after `frameNumber` completed, without destroying anything.
  /// History and the key log stay intact, so the slider can be dragged on
  /// and "Replay" still has the whole script. Truncation happens only when
  /// a branch is committed (Go) or a replay starts.
  member this.RewindTo(frameNumber: int) =
    if frameNumber < 0 || frameNumber > history.LastFrame then
      invalidOp (sprintf "no history at frame %d (last=%d)" frameNumber history.LastFrame)
    let buffer = Array.zeroCreate<byte> 0x10000
    let text, keys = history.Restore(frameNumber, buffer)
    port.LoadState(buffer, text)
    port.Keyboard.Load(keys)
    replayMode <- false
    pendingReplayKeys <- []
    frame <- frameNumber
    port.Video.RenderAll()

  /// Branch COMMIT: having previewed `frameNumber` (or reached it during a
  /// replay), abandon the old future. History and the key log are truncated
  /// so new captures continue from here; the trace recorder window resets
  /// (the machine's cycle counter jumped, stale entries would break the
  /// recorder's tick invariants).
  member this.BranchAt(frameNumber: int) =
    if frameNumber < 0 || frameNumber > history.LastFrame then
      invalidOp (sprintf "no history at frame %d (last=%d)" frameNumber history.LastFrame)
    history.Truncate(frameNumber, port.Memory)
    keyLog.Truncate(frameNumber)
    recorder.Reset()
    replayMode <- false
    pendingReplayKeys <- []
    frame <- frameNumber

  /// Begin scripted replay from the current (rewound) frame: history is
  /// truncated at the branch point but the key log is KEPT as the script
  /// (events after the point are replayed). Live keys are ignored while
  /// replaying; RunFrame reports ReplayFinished at the recording's end.
  member this.StartReplay() =
    releaseAllKeys () // user-held keys would stay latched through the whole script
    replayMode <- true
    // After a process restart, history contains only the frame-0 entry
    // anchor, while the persisted KeyLog contains the recording extent.
    replayEndFrame <- max history.LastFrame keyLog.EndFrame
    replayFinished <- false
    history.Truncate(frame, port.Memory)
    recorder.Reset()
    pendingReplayKeys <- keyLog.ForFrame(frame)

  /// Abort a running replay and return to live control immediately (the
  /// frame timer's Run button mid-replay). Clears ReplayFinished too: a
  /// replay that ran to the end leaves the flag set, and live frames must
  /// not be cut short by the stale end-of-replay check.
  member this.StopReplay() =
    releaseAllKeys ()
    replayMode <- false
    replayFinished <- false
    pendingReplayKeys <- []
  /// Execute one frame (69888 T-states plus overshoot) on the port machine,
  /// recording every instruction. In replay mode the frame's logged key
  /// events are applied before the machine runs.
  member this.RunFrame() : int64 * int64 =
    let frameStart = port.CycleCount()
    let frameEnd = port.FrameEnd
    if replayMode then
      for e in pendingReplayKeys do
        port.SetKey(e.Row, e.Bit, e.Pressed)
    if recorder.RecordEnabled then
      let mutable step = 0
      while port.CycleCount() < frameEnd do
        let irq = port.IrqPending && port.Iff1
        let pc = port.Regs.Pc()
        let flagsBefore = port.Flags().ToU8()
        let cyclesBefore = port.CycleCount()
        if irq then
          // One Step services the interrupt AND runs the vector instruction.
          // Record a synthetic marker (Length = 0) plus the vector instruction
          // under its own pc; the interrupted instruction resumes after the
          // ISR returns and gets its own entry then.
          let vector =
            match port.IrqMode with
            | 2 ->
              let addr = 0xFF ||| ((port.Regs.I() <<< 8) &&& 0xFF00)
              let lo = int port.Memory[addr &&& 0xFFFF]
              let hi = int port.Memory[(addr + 1) &&& 0xFFFF]
              (lo ||| (hi <<< 8)) &&& 0xFFFF
            | _ -> 0x38
          recorder.Record
            { Pc = uint16 vector
              B0 = 0uy
              B1 = 0uy
              B2 = 0uy
              B3 = 0uy
              Target = uint16 vector
              Tick = uint32 cyclesBefore
              Length = 0uy
              Cycles = 7uy
              FlagsBefore = uint8 flagsBefore
              FlagsAfter = uint8 flagsBefore
              Taken = 1uy }
          let vinsnLen = Disasm.disasmLength port.Memory vector
          port.Step()
          let after = port.Regs.Pc()
          let cycles = int (port.CycleCount() - cyclesBefore)
          let next = (vector + vinsnLen) &&& 0xFFFF
          let m = port.Memory
          recorder.Record
            { Pc = uint16 vector
              B0 = m[vector &&& 0xFFFF]
              B1 = m[(vector + 1) &&& 0xFFFF]
              B2 = m[(vector + 2) &&& 0xFFFF]
              B3 = m[(vector + 3) &&& 0xFFFF]
              Target = uint16 after
              Tick = uint32 cyclesBefore
              Length = uint8 vinsnLen
              Cycles = uint8 (min 255 cycles)
              FlagsBefore = uint8 flagsBefore
              FlagsAfter = uint8 (port.Flags().ToU8())
              Taken = (if after <> next then 1uy else 0uy) }
        else
          let insnLen = Disasm.disasmLength port.Memory pc
          port.Step()
          let after = port.Regs.Pc()
          let cycles = int (port.CycleCount() - cyclesBefore)
          let next = (pc + insnLen) &&& 0xFFFF
          let m = port.Memory
          recorder.Record
            { Pc = uint16 pc
              B0 = m[pc &&& 0xFFFF]
              B1 = m[(pc + 1) &&& 0xFFFF]
              B2 = m[(pc + 2) &&& 0xFFFF]
              B3 = m[(pc + 3) &&& 0xFFFF]
              Target = uint16 after
              Tick = uint32 cyclesBefore
              Length = uint8 insnLen
              Cycles = uint8 (min 255 cycles)
              FlagsBefore = uint8 flagsBefore
              FlagsAfter = uint8 (port.Flags().ToU8())
              Taken = (if after <> next then 1uy else 0uy) }
        if step % snapshotInterval = 0 then recorder.RecordSnapshot(snapshotOf ())
        step <- step + 1
    else
      // Bare loop: the machine services interrupts inside Step; nothing is
      // captured. RecordWrite/RecordPort handlers early-return on the flag.
      while port.CycleCount() < frameEnd do
        port.Step()
    port.FrameEnd <- frameEnd + 69888L
    recorder.RecordFrameBoundary(uint32 (port.CycleCount()))
    // Rewind bookkeeping: the completed frame is `frame + 1`; stamp it into
    // the history store (anchor or delta) under its own number, then advance
    // the scripted replay if one is running. The replay's last executed
    // frame is exactly replayEndFrame: the state after it matches the
    // recording's end.
    frame <- frame + 1
    captureState ()
    if replayMode then
      if frame >= replayEndFrame then
        // The script ended mid-hold (or its final events latched a key);
        // hand live control an all-up keyboard.
        releaseAllKeys ()
        replayMode <- false
        pendingReplayKeys <- []
        replayFinished <- true
      else
        pendingReplayKeys <- keyLog.ForFrame(frame)
    frameStart, port.CycleCount()
