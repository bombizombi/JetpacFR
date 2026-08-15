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

  let private dir = Path.Combine(AppContext.BaseDirectory, "entry-cache")
  let private memoryPath = Path.Combine(dir, "memory.bin")
  let private statePath = Path.Combine(dir, "state.txt")
  let private metaPath = Path.Combine(dir, "meta.txt")

  let private sha256 (bytes: byte[]) =
    use sha = System.Security.Cryptography.SHA256.Create()
    sha.ComputeHash bytes |> Array.map (fun b -> b.ToString("x2")) |> String.concat ""

  let private marker (romPath: string) (tzxPath: string) =
    sprintf "rom=%s\ntzx=%s\n" (sha256 (File.ReadAllBytes romPath)) (sha256 (File.ReadAllBytes tzxPath))

  /// Some(mem, state) when a cache exists, matches the current assets, and is
  /// structurally intact; None otherwise (cold boot needed).
  let tryLoad (romPath: string) (tzxPath: string) : (byte[] * string) option =
    try
      if File.Exists memoryPath && File.Exists statePath && File.Exists metaPath then
        let current = marker romPath tzxPath
        let stored = File.ReadAllText metaPath
        if stored = current then
          let mem = File.ReadAllBytes memoryPath
          if mem.Length = 0x10000 then Some(mem, File.ReadAllText statePath)
          else None
        else None
      else None
    with _ -> None

  let save (romPath: string) (tzxPath: string) (mem: byte[]) (state: string) =
    try
      Directory.CreateDirectory dir |> ignore
      File.WriteAllBytes(memoryPath, mem)
      File.WriteAllText(statePath, state)
      File.WriteAllText(metaPath, marker romPath tzxPath)
    with _ -> () // the cache is an optimization; a failed write must not break startup

/// The engine: the Jetpac2 port machine driven instruction-by-instruction,
/// recording every step into a TraceRecorder. The JetpacFSharp oracle is only
/// involved on a cold start (boot to the game entry, then cache the state);
/// after that the port runs alone, so the screen, the sound and the trace all
/// describe the same execution.
type TraceSession(romPath: string, tzxPath: string, capacity: int) =

  let port = Jetpac2.Core.Machine()
  let recorder = TraceRecorder(capacity, 512)
  let mutable frame = 0
  let mutable warmStart = false
  let snapshotInterval = 64

  /// Put the port into the game-entry state. Warm: load the cached snapshot.
  /// Cold: boot the oracle to the entry and cache its state for next time.
  let loadEntryState () =
    match EntryCache.tryLoad romPath tzxPath with
    | Some (mem, state) ->
      try
        port.LoadState(mem, state)
        warmStart <- true
      with _ ->
        let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath
        let mem, state = oracle.SaveState()
        EntryCache.save romPath tzxPath mem state
        port.LoadState(mem, state)
        warmStart <- false
    | None ->
      let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath
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
    Jetpac2.Core.Generated.EnsureInstalled()
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
    recorder.RecordSnapshot(snapshotOf ())

  /// True when the entry state came from the cache (no oracle boot needed).
  member this.WarmStart = warmStart
  member this.Frame = frame
  member this.Recorder = recorder
  member this.CycleCount = port.CycleCount()
  member this.Regs = port.Regs
  member this.Memory = port.Memory

  member this.SetKey(row: int, bit: int, pressed: bool) = port.SetKey(row, bit, pressed)

  /// Rendered frame (BGRA 320x256) from the port's video state.
  member this.ScreenBuffer = port.Video.BlitTo()

  /// Drain the frame's beeper transitions and synthesize the 882 samples for
  /// the just-executed frame.
  member this.DrainBeeperSamples(frameStart: int64) : float32[] =
    let trace = port.BeeperTrace |> Seq.toList
    port.BeeperTrace.Clear()
    Jetpac2.Core.Beeper.ToSamples trace frameStart (port.CycleCount() - frameStart)

  /// Execute one frame (69888 T-states plus overshoot) on the port machine,
  /// recording every instruction. Returns (frameStart, frameEnd) for the
  /// audio call.
  member this.RunFrame() : int64 * int64 =
    let frameStart = port.CycleCount()
    let frameEnd = port.FrameEnd
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
        let vinsn = Disasm.disasmMemory port.Memory vector
        port.Step()
        let after = port.Regs.Pc()
        let cycles = int (port.CycleCount() - cyclesBefore)
        let next = (vector + vinsn.Length) &&& 0xFFFF
        let m = port.Memory
        recorder.Record
          { Pc = uint16 vector
            B0 = m[vector &&& 0xFFFF]
            B1 = m[(vector + 1) &&& 0xFFFF]
            B2 = m[(vector + 2) &&& 0xFFFF]
            B3 = m[(vector + 3) &&& 0xFFFF]
            Target = uint16 after
            Tick = uint32 cyclesBefore
            Length = uint8 vinsn.Length
            Cycles = uint8 (min 255 cycles)
            FlagsBefore = uint8 flagsBefore
            FlagsAfter = uint8 (port.Flags().ToU8())
            Taken = (if after <> next then 1uy else 0uy) }
      else
        let insn = Disasm.disasmMemory port.Memory pc
        port.Step()
        let after = port.Regs.Pc()
        let cycles = int (port.CycleCount() - cyclesBefore)
        let next = (pc + insn.Length) &&& 0xFFFF
        let m = port.Memory
        recorder.Record
          { Pc = uint16 pc
            B0 = m[pc &&& 0xFFFF]
            B1 = m[(pc + 1) &&& 0xFFFF]
            B2 = m[(pc + 2) &&& 0xFFFF]
            B3 = m[(pc + 3) &&& 0xFFFF]
            Target = uint16 after
            Tick = uint32 cyclesBefore
            Length = uint8 insn.Length
            Cycles = uint8 (min 255 cycles)
            FlagsBefore = uint8 flagsBefore
            FlagsAfter = uint8 (port.Flags().ToU8())
            Taken = (if after <> next then 1uy else 0uy) }
      if step % snapshotInterval = 0 then recorder.RecordSnapshot(snapshotOf ())
      step <- step + 1
    port.FrameEnd <- frameEnd + 69888L
    recorder.RecordFrameBoundary(uint32 (port.CycleCount()))
    frame <- frame + 1
    frameStart, port.CycleCount()
