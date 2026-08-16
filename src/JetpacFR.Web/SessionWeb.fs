namespace JetpacFR.Core

/// WEB SHELL SHIM (JetpacFR.Web): browser equivalents of the file-IO parts of
/// JetpacFR.Core/Session.fs. EntryCache persists the game-entry snapshot in
/// browser localStorage (the desktop writes entry-cache/ files); TraceSession
/// is the Core engine verbatim, constructed from byte[] assets instead of paths.
open System
open Fable.Core
open Fable.Core.JsInterop

/// Stable 32-bit FNV-1a over the asset bytes, hex (marker stability only; the
/// desktop uses real sha256 but the marker is never cross-checked between the
/// two shells).
module private AssetHash =
  let ofBytes (b: byte[]) : string =
    let mutable h = 0x811C9DC5
    for i in 0 .. b.Length - 1 do
      h <- (h ^^^ int b[i]) * 0x01000193
      h <- h &&& 0xFFFFFFFF
    h.ToString("x8")

module EntryCache =

  let private storage: obj = emitJsExpr () "window.localStorage"
  let private memKey = "jetpacfr.entry.v1.mem"
  let private stateKey = "jetpacfr.entry.v1.state"
  let private metaKey = "jetpacfr.entry.v1.meta"

  let private marker (romPath: string) (tzxPath: string) =
    sprintf "rom=%s\ntzx=%s" (AssetHash.ofBytes (Jetpac3.Core.Boot.AssetProvider romPath))
      (AssetHash.ofBytes (Jetpac3.Core.Boot.AssetProvider tzxPath))

  let private getItem (key: string) : string option =
    let v: obj = storage?getItem(key)
    if isNull v then None else Some (unbox<string> v)

  /// Some(mem, state) when localStorage holds a cache matching the current
  /// assets and it decodes to a full 64K image; None otherwise (cold boot).
  let tryLoad (romPath: string) (tzxPath: string) : (byte[] * string) option =
    try
      match getItem metaKey with
      | Some m when m = marker romPath tzxPath ->
        match getItem memKey, getItem stateKey with
        | Some memB64, Some state ->
          let mem = System.Convert.FromBase64String memB64
          if mem.Length = 0x10000 then Some (mem, state) else None
        | _ -> None
      | _ -> None
    with _ -> None

  let save (romPath: string) (tzxPath: string) (mem: byte[]) (state: string) =
    try
      storage?setItem(metaKey, marker romPath tzxPath)
      storage?setItem(memKey, System.Convert.ToBase64String mem)
      storage?setItem(stateKey, state)
    with _ -> () // the cache is an optimization; quota failures must not break startup

type TraceSession(romBytes: byte[], tzxBytes: byte[], capacity: int) =
  // Web: assets arrive as byte arrays (embedded base64). EntryCache/Boot use the
  // fixed asset keys "rom"/"tzx", resolved by Boot.AssetProvider.
  let romPath = "rom"
  let tzxPath = "tzx"

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
