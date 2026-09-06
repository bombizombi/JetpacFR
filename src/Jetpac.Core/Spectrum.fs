namespace Jetpac.Core

#nowarn "40" // intentional self-referential scheduler tasks (delayed reference is safe: tasks run only via scheduler.Schedule after construction)

/// Port of specbolt's Spectrum 48K wiring (spectrum/include/spectrum/Spectrum.hpp,
/// 48K paths only). Runs the Z80 against the ULA/keyboard/tape peripherals;
/// a frame-based boot macro types `LOAD ""` so the ROM loader starts the tape.
type InstructionTraceEvent =
  { TState: int64
    Address: int
    Opcode: byte
    NextPc: int
    Writes: MemoryWrite list }

type Spectrum48() as self =
  let memory = Memory(4)
  let video = Video(memory)
  let keyboard = Keyboard()
  let scheduler = Scheduler()
  let z80 = Z80(scheduler, memory)
  let tape = Tape()
  let mutable beeper = false
  let mutable border = 0
  let mutable frameCount = 0
  let mutable tapeLastTime = 0UL

  // Checkpoint support: boot guard, game-frame boundary and beeper trace.
  let mutable booted = false
  let mutable frameEnd = 0L
  let beeperTrace = System.Collections.Generic.List<int64 * bool>()
  let mutable traceEnabled = false
  let traceEvents = System.Collections.Generic.List<InstructionTraceEvent>()
  let executedAddresses = System.Collections.Generic.HashSet<int>()
  let pendingWrites = System.Collections.Generic.List<MemoryWrite>()

  // Loading detection heuristic state (Fuse).
  let mutable lastDetect : uint64 = 0UL
  let mutable lastBRead = 0
  let mutable readsInARow = 0

  let rec videoTask =
    SchedulerTask(fun _ ->
      if video.NextScanLine() then
        z80.Interrupt()
      scheduler.Schedule(videoTask, uint64 VideoConstants.CyclesPerScanLine))

  and tapeTask =
    SchedulerTask(fun cycle ->
      tape.PassTime(int (cycle - tapeLastTime))
      tapeLastTime <- cycle
      if tape.NextTransition() <> 0 then
        scheduler.Schedule(tapeTask, uint64 (tape.NextTransition())))

  do
    Z80Ops.EnsureInstalled()
    memory.AddWriteHandler(fun event -> if traceEnabled then pendingWrites.Add event)
    z80.AddOutHandler(fun port value ->
      if port &&& 0xFF = 0xFE then
        video.SetBorder(value &&& 7)
        border <- value &&& 7
        let lvl = value &&& 0x10 <> 0
        if lvl <> beeper then
          beeper <- lvl
          beeperTrace.Add(int64 (z80.CycleCount()), lvl))
    z80.AddInHandler(fun port -> keyboard.In port)
    z80.AddInHandler(fun port ->
      if port &&& 1 <> 0 then
        None
      else
        self.MaybeDetectLoading()
        Some(0xBF ||| (if tape.Level() then 0x40 else 0)))
    scheduler.Schedule(videoTask, 0UL)

  member this.Memory: byte[] = memory.AddressSpace
  member this.ScreenBuffer: byte[] = video.BlitTo()
  member this.FrameCount = frameCount
  member this.Beeper = beeper
  member this.Border = border
  member this.BeginExecutionTrace() =
    traceEvents.Clear()
    executedAddresses.Clear()
    pendingWrites.Clear()
    traceEnabled <- true

  member this.EndExecutionTrace() = traceEnabled <- false
  member this.ExecutedAddressCount = executedAddresses.Count
  member this.DrainInstructionTrace() : InstructionTraceEvent list =
    let items = traceEvents |> Seq.toList
    traceEvents.Clear()
    items

  member private this.ExecuteOneTraced() =
    if not traceEnabled then
      z80.ExecuteOne()
    else
      let address = z80.Regs.Pc()
      let opcode = byte (memory.Read address)
      let tstate = int64 (z80.CycleCount())
      pendingWrites.Clear()
      z80.ExecuteOne()
      executedAddresses.Add address |> ignore
      traceEvents.Add
        { TState = tstate
          Address = address
          Opcode = opcode
          NextPc = z80.Regs.Pc()
          Writes = pendingWrites |> Seq.toList }
  member this.ExecuteInstruction() = self.ExecuteOneTraced()
  member this.DebugZ80 = z80
  member this.DebugTapePlaying = tape.Playing()

  member this.BeeperTrace: (int64 * bool) list = beeperTrace |> Seq.toList

  /// Consume the beeper transitions since the last call (per-frame slices).
  member this.DrainBeeperTrace() : (int64 * bool) list =
    let items = beeperTrace |> Seq.toList
    beeperTrace.Clear()
    items

  /// Capture the full machine state (64K memory + key=value register/timing
  /// text, the same schema the Jetpac2 fixture uses, plus `wz`).
  member this.SaveState() : byte[] * string =
    let regs = z80.Regs
    let cycles = int64 (z80.CycleCount())
    let videoNextTime = (cycles / 224L + 1L) * 224L
    let f = videoNextTime / 224L
    let nextWrap = (((f + 312L) / 312L) * 312L - 1L) * 224L
    let regsText =
      sprintf
        "af=%X\nbc=%X\nde=%X\nhl=%X\naf2=%X\nbc2=%X\nde2=%X\nhl2=%X\nix=%X\niy=%X\nsp=%X\npc=%X\ni=%X\nr=%X\nwz=%X\niff1=%b\niff2=%b\nim=%d\nhalted=%b\nirq=%b\nborder=%d\nbeeper=%b\ntapeEar=%b\ncycles=%d\nvideoNextTime=%d\nnextWrap=%d\n"
        (regs.Get R16.AF) (regs.Get R16.BC) (regs.Get R16.DE) (regs.Get R16.HL)
        (regs.Get R16.AF_) (regs.Get R16.BC_) (regs.Get R16.DE_) (regs.Get R16.HL_)
        (regs.Ix()) (regs.Iy()) (regs.Sp()) (regs.Pc()) (regs.I()) (regs.R())
        (regs.Wz())
        z80.Iff1 z80.Iff2 z80.IrqMode z80.Halted z80.IrqPending border beeper (tape.Level())
        cycles videoNextTime nextWrap
    Array.copy memory.AddressSpace, regsText

  /// Restore a captured state (mirrors Jetpac2 Machine.LoadState). The tape is
  /// stopped (checkpoints are taken at/beyond game entry) and its ear level is
  /// restored; loading-detection state is reset.
  member this.LoadState(bytes: byte[], regsText: string) =
    Array.blit bytes 0 memory.AddressSpace 0 (min bytes.Length 0x10000)
    let kv = System.Collections.Generic.Dictionary<string, string>()
    for line in regsText.Split('\n') do
      let line = line.Trim()
      if line.Length > 0 then
        match line.IndexOf '=' with
        | -1 -> ()
        | i -> kv.[line.Substring(0, i).Trim()] <- line.Substring(i + 1).Trim()
    let hex (k: string) (def: int) =
      match kv.TryGetValue k with
      | true, v -> System.Convert.ToInt32(v.Replace("0x", ""), 16)
      | _ -> def
    let boolv (k: string) (def: bool) =
      match kv.TryGetValue k with
      | true, v -> v = "true" || v = "1"
      | _ -> def
    let regs = z80.Regs
    regs.Set(R16.AF, hex "af" 0)
    regs.Set(R16.BC, hex "bc" 0)
    regs.Set(R16.DE, hex "de" 0)
    regs.Set(R16.HL, hex "hl" 0)
    regs.Set(R16.AF_, hex "af2" 0)
    regs.Set(R16.BC_, hex "bc2" 0)
    regs.Set(R16.DE_, hex "de2" 0)
    regs.Set(R16.HL_, hex "hl2" 0)
    regs.Set(R16.IX, hex "ix" 0)
    regs.Set(R16.IY, hex "iy" 0)
    regs.SetSp(hex "sp" 0)
    regs.SetPc(hex "pc" 0)
    regs.SetI(hex "i" 0)
    regs.SetR(hex "r" 0)
    regs.SetWz(hex "wz" 0xFFFF)
    z80.ResetInterruptState()
    z80.Iff1 <- boolv "iff1" false
    z80.Iff2 <- boolv "iff2" false
    z80.IrqMode <- hex "im" 0
    // The wrap fire lands exactly at frameEnd, so a frame-boundary
    // checkpoint usually has an interrupt pending; dropping it made the
    // restored continuation run one ISR-less frame.
    if boolv "irq" false then z80.Interrupt()
    z80.Halted <- boolv "halted" false
    border <- hex "border" 0
    video.SetBorder border
    beeper <- boolv "beeper" false
    let ear = boolv "tapeEar" false
    tape.Stop()
    tape.SetLevel ear
    let cycles = System.UInt64.Parse(kv.["cycles"])
    let videoNextTime = System.UInt64.Parse(kv.["videoNextTime"])
    let nextWrap = System.Int64.Parse(kv.["nextWrap"])
    scheduler.Reset( cycles)
    scheduler.Schedule(videoTask,  (videoNextTime - cycles))
    let f = videoNextTime / 224UL
    let scanline = int (f % 312UL)
    let wraps = f / 312UL
    video.SetState(scanline, int (wraps % 16UL), (wraps / 16UL) % 2UL = 1UL)
    beeperTrace.Clear()
    frameEnd <- nextWrap
    frameCount <- 0
    tapeLastTime <- 0UL
    lastDetect <- 0UL
    lastBRead <- 0
    readsInARow <- 0
    booted <- true

  /// Absolute cycle at which the current frame ends.
  member this.FrameEnd = frameEnd

  /// Execute until an absolute cycle without changing the frame boundary.
  member this.RunUntil(targetCycle: int64) =
    while int64 (z80.CycleCount()) < targetCycle do
      self.ExecuteOneTraced()

  member this.AdvanceFrameBoundary() =
    frameEnd <- frameEnd + 69888L
    frameCount <- frameCount + 1

  /// Run to the next 50Hz interrupt boundary (69888 T-states), without the
  /// boot macro (used after a checkpoint restore).
  member this.RunGameFrame() =
    this.RunUntil frameEnd
    this.AdvanceFrameBoundary()

  member this.LoadRom(bytes: byte[]) =
    if bytes.Length <> 0x4000 then
      failwithf "Bad ROM size: %d (expected 16384)" bytes.Length
    memory.LoadBytes(bytes, 0, 0, 0x4000)
    memory.SetRomFlags([| true; false; false; false |])
    memory.SetPageTable([| 0; 1; 2; 3 |])

  member this.InsertTape(bytes: byte[]) = tape.InsertTzx bytes

  member this.SetKey(row: int, bit: int, pressed: bool) = keyboard.SetKey(row, bit, pressed)

  /// The Fuse loading-detection heuristic (Spectrum.hpp maybe_detect_loading).
  member private this.MaybeDetectLoading() =
    let sinceLast = z80.CycleCount() - lastDetect
    let bDiff = (z80.Regs.Get R8.B - lastBRead) &&& 0xFF
    lastDetect <- z80.CycleCount()
    lastBRead <- z80.Regs.Get R8.B
    if tape.Playing() then
      if sinceLast > 1000UL || (bDiff <> 1 && bDiff <> 0 && bDiff <> 0xFF) then
        readsInARow <- readsInARow + 1
        if readsInARow >= 2 then
          tape.Stop()
      else
        readsInARow <- 0
    else
      if sinceLast <= 500UL && (bDiff = 1 || bDiff = 0xFF) then
        readsInARow <- readsInARow + 1
        if readsInARow >= 10 then
          this.Play()
      else
        readsInARow <- 0

  member private this.Play() =
    tape.Play()
    if tape.NextTransition() <> 0 then
      tapeLastTime <- z80.CycleCount()
      scheduler.Schedule(tapeTask, uint64 (tape.NextTransition()))

  /// Frame-based boot macro: types LOAD "" (J, SYM SHIFT+P twice for the two
  /// quotes, ENTER) using the documented 48K keyboard layout. The quote on the
  /// ZX Spectrum is SYM SHIFT+P (the shifted digits are the edit/cursor keys).
  member private this.RunBootMacro() =
    if frameCount = 150 then
      keyboard.SetKey(6, 3, true) // J = LOAD keyword
    elif frameCount = 158 then
      keyboard.SetKey(6, 3, false)
    elif frameCount = 162 then
      keyboard.SetKey(7, 1, true) // SYM SHIFT
      keyboard.SetKey(5, 0, true) // P => "
    elif frameCount = 170 then
      keyboard.SetKey(7, 1, false)
      keyboard.SetKey(5, 0, false)
    elif frameCount = 174 then
      keyboard.SetKey(7, 1, true) // second quote
      keyboard.SetKey(5, 0, true)
    elif frameCount = 182 then
      keyboard.SetKey(7, 1, false)
      keyboard.SetKey(5, 0, false)
    elif frameCount = 186 then
      keyboard.SetKey(6, 0, true) // ENTER
    elif frameCount = 194 then
      keyboard.SetKey(6, 0, false)

  member this.RunFrame() =
    if not booted then
      this.RunBootMacro()
    let endCycles = z80.CycleCount() + 70000UL
    while z80.CycleCount() < endCycles do
      self.ExecuteOneTraced()
    video.BlitTo() |> ignore
    frameCount <- frameCount + 1

  member this.Reset() =
    z80.Regs.SetPc 0
    z80.ResetInterruptState()
    tape.Stop()
    frameCount <- 0
    tapeLastTime <- 0UL
    lastDetect <- 0UL
    lastBRead <- 0
    readsInARow <- 0
    for row in 0 .. 7 do
      for bit in 0 .. 4 do
        keyboard.SetKey(row, bit, false)
