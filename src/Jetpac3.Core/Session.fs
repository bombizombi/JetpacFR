namespace Jetpac3.Core

open System
open System.IO

/// Resolve assets from the copied Jetpac4 tree only. Tests/tools may bypass
/// this resolver by passing explicit ROM/TZX paths on their command line.
module Paths =
  let private rootCandidates (start: string) : string list =
    let rec walk (dir: DirectoryInfo) (depth: int) (acc: string list) =
      if depth > 12 then acc
      else
        let path = dir.FullName
        let next =
          if String.Equals(dir.Name, "Jetpac4", StringComparison.OrdinalIgnoreCase) then
            path :: acc
          else
            acc
        match dir.Parent with
        | null -> next
        | parent -> walk parent (depth + 1) next
    walk (DirectoryInfo(Path.GetFullPath start)) 0 []

  let private candidates (name: string) : string list =
    [ AppContext.BaseDirectory; Directory.GetCurrentDirectory() ]
    |> List.collect (fun start -> rootCandidates start)
    |> List.distinct
    |> List.map (fun root -> Path.Combine(root, "JetpacFSharp", "assets", name))

  let resolveAsset (name: string) : string =
    let searched = candidates name
    match searched |> List.tryFind File.Exists with
    | Some path -> path
    | None ->
      let paths = String.Join(Environment.NewLine + "  ", searched)
      failwithf "asset not found: %s; searched:\n  %s" name paths


/// Scripted key sessions (frame, row, bit, pressed), shared by the tests and
/// the GUI's differential runner. Mirrors Jetpac2's `default` scenario.
module Script =
  let defaultSession (framesN: int) : (int * int * int * bool) list =
    let script =
      [ (10, 30, (3, 0))   // press "1" (start 1-player game)
        (35, 50, (3, 4))   // hold 5 (right)
        (90, 110, (3, 4))  // 5 again
        (120, 135, (4, 2)) // hold 8 (left)
        (160, 165, (7, 0)) // fire (space)
        (200, 220, (7, 0)) // fire
        (260, 275, (0, 0)) // up (CAPS SHIFT + 7)
        (300, 315, (0, 0)) // up
        (350, 365, (4, 4)) // down (6)
        (400, 420, (7, 0)) ] // fire
    [ for (press, release, (row, bit)) in script do
        if release <= framesN then
          yield (press, row, bit, true)
          yield (release, row, bit, false) ]
/// The Jetpac3 engine: an oracle emulator plus a lifted port, driven from
/// checkpoints, with differential execution.
type Session(romPath: string, tzxPath: string) as self =
  let store = CheckpointStore(Path.Combine(Directory.GetCurrentDirectory(), "checkpoints"))
  let oracle, _ = Boot.bootToEntry romPath tzxPath None
  let mutable port = Jetpac2.Core.Machine()
  let mutable currentId = "entry"
  let mutable frame = 0
  let mutable pressedKeys : Set<int * int> = Set.empty
  let mutable captureOptions =
    { FrameInterval = None
      CaptureDecisionPoints = false }
  /// The 1-bit sound log: one (absolute cycle, speaker state) entry per OUT
  /// to the beeper port (0xFE), growing with each OUT. The oracle's own
  /// beeper trace only records level *changes*; this one keeps every OUT
  /// (repeated states included) so the WAV renderer can re-integrate the
  /// waveform exactly.
  let beeperEvents = System.Collections.Generic.List<BeeperEvent>()
  let memoryWrites = System.Collections.Generic.List<MemoryWriteEvent>()
  let codeCache = CodeCache()
  let timelineEvents = ResizeArray<InputEvent>()
  let mutable recordingInput = false
  let mutable traceWriter: ExecutionTraceWriter option = None
  let mutable tracePath = ""
  let mutable startupPolicy: StartupExplorationPolicy option = None
  let mutable explorationComplete = true
  let attachPort (machine: Jetpac2.Core.Machine) =
    machine.Override <- LiftedRoutines.registryHook
    machine.AddMemoryWriteHandler(fun event -> memoryWrites.Add event)
    codeCache.Attach machine
  let stateWithKeys (baseState: string) =
    let keyLines =
      pressedKeys
      |> Seq.map (fun (row, bit) -> sprintf "key%d_%d=true\n" row bit)
      |> String.concat ""
    baseState + keyLines

  let applyKeys (setKey: (int * int * bool) -> unit) (stateText: string) =
    let kv = StateText.parse stateText
    for row in 0 .. 7 do
      for bit in 0 .. 4 do
        setKey(row, bit, false)
    for row in 0 .. 7 do
      for bit in 0 .. 4 do
        let key = sprintf "key%d_%d" row bit
        match kv.TryGetValue key with
        | true, value when value = "true" || value = "1" -> setKey(row, bit, true)
        | _ -> ()

  let synchronizePort () =
    let mem, baseState = oracle.SaveState()
    let state = stateWithKeys baseState
    port.LoadState(mem, state)
    applyKeys port.SetKey state
    port.HardwareEvents.Clear()
    memoryWrites.Clear()

  let stopTrace () =
    oracle.EndExecutionTrace()
    match traceWriter with
    | Some writer -> writer.Dispose()
    | None -> ()
    traceWriter <- None
    explorationComplete <- true

  let flushTraceBatch () =
    let events = oracle.DrainInstructionTrace()
    match traceWriter with
    | Some writer -> writer.Append events
    | None -> ()
    match startupPolicy with
    | Some policy when oracle.ExecutedAddressCount >= policy.NewExecutableAddresses || frame >= policy.MaxFrames ->
      match traceWriter with
      | Some writer -> writer.Flush()
      | None -> ()
      stopTrace ()
    | _ -> ()

  do
    Jetpac2.Core.Z80Table.EnsureInstalled()
    attachPort port
    // OUT (n),A / OUT (C),r is the only path a 48K Spectrum can drive the
    // beeper through; hook the oracle's OUT dispatch to log each write.
    oracle.DebugZ80.AddOutHandler(fun p value ->
      if p &&& 0xFF = 0xFE then
        beeperEvents.Add
          { Tick = int64 (oracle.DebugZ80.CycleCount())
            State = value &&& 0x10 <> 0 })
    // Establish the frame boundary + boot guard on the freshly-booted oracle,
    // and load the same entry state into the port.
    let mem, state = oracle.SaveState()
    oracle.LoadState(mem, state)
    port.LoadState(mem, state)
    self.CaptureCheckpoint("entry", None, "game entry") |> ignore

  member this.Oracle = oracle
  member this.Store = store
  member this.CurrentId = currentId
  member this.Frame = frame
  member this.BeginStartupExploration(policy: StartupExplorationPolicy, path: string) =
    if policy.NewExecutableAddresses <= 0 then
      explorationComplete <- true
    else
      tracePath <- path
      startupPolicy <- Some policy
      explorationComplete <- false
      oracle.BeginExecutionTrace()
      traceWriter <- Some(ExecutionTraceWriter path)

  member this.ExplorationComplete = explorationComplete
  member this.ExploredAddressCount = oracle.ExecutedAddressCount
  member this.TracePath = tracePath

  member this.FlushExecutionTrace() =
    match traceWriter with
    | Some writer -> writer.Flush()
    | None -> ()

  member this.BeginInputRecording() =
    timelineEvents.Clear()
    recordingInput <- true

  member this.StopInputRecording() = recordingInput <- false

  member this.InputTimeline =
    { Version = 1
      RomSha256 = ""
      TzxSha256 = ""
      Events = timelineEvents |> Seq.toList }

  member this.SaveInputTimeline(path: string) =
    File.WriteAllText(path, InputTimelineCodec.serialize this.InputTimeline)

  member this.ReplayRecordedTimeline() =
    let timeline = this.InputTimeline
    if not (List.isEmpty timeline.Events) then
      let frames = (timeline.Events |> List.maxBy (fun event -> event.Frame)).Frame + 1
      let wasRecording = recordingInput
      recordingInput <- false
      self.RestoreCheckpoint("entry")
      self.RunTimeline(frames, timeline)
      recordingInput <- wasRecording
  member this.ScreenBuffer: byte[] = oracle.ScreenBuffer
  member this.Border = oracle.Border
  member this.Beeper = oracle.Beeper
  member this.DrainHardwareEvents() : HardwareEvent list = port.DrainHardwareEvents()
  member this.DrainMemoryWrites() : MemoryWriteEvent list =
    let items = memoryWrites |> Seq.toList
    memoryWrites.Clear()
    items
  member this.DecodeAt(address: int) = codeCache.Decode(port.Memory) address
  member this.DecodedInstructionCount = codeCache.Count
  member this.Explore(policy: ExplorationPolicy, cancel: System.Threading.CancellationToken) =
    ConcreteExplorer.explore port codeCache policy cancel
  member this.BuildDiagnostics(id: string, timeline: InputTimeline option, mode: ExecutionMode) : DiagnosticArtifact =
    let checkpoint = store.Load id
    { Identity = Diagnostics.emptyIdentity mode
      CheckpointId = id
      Timeline = timeline
      StateHash = Some(CanonicalState.checkpoint checkpoint)
      HardwareTrace =
        { PortEvents = this.DrainHardwareEvents()
          MemoryWrites = this.DrainMemoryWrites() }
      Notes = [ sprintf "frame=%d" checkpoint.Frame; sprintf "pc=%04X" checkpoint.Pc ] }
  /// Absolute CPU cycle count of the oracle (an int underneath, so it wraps
  /// every ~10 min; only differences within a <10 s batch are ever used).
  member this.CycleCount: int64 = int64 (oracle.DebugZ80.CycleCount())
  member this.DrainBeeperTrace() = oracle.DrainBeeperTrace()

  /// Consume the beeper OUT events since the last call (absolute cycle,
  /// level). Unlike DrainBeeperTrace this keeps repeated states, one entry
  /// per OUT, in the order they were executed.
  member this.DrainBeeperEvents() : BeeperEvent list =
    let items = beeperEvents |> Seq.toList
    beeperEvents.Clear()
    items

  member this.CaptureCheckpoint(id: string, parent: string option, inputSummary: string) : Checkpoint =
    let mem, baseState = oracle.SaveState()
    let stateText = stateWithKeys baseState
    let kv = StateText.parse stateText
    let pc = Convert.ToInt32(kv.["pc"].Replace("0x", ""), 16)
    let cycles = Int64.Parse kv.["cycles"]
    let c =
      { Id = id; ParentId = parent; Frame = frame; Pc = pc; Cycles = cycles
        InputSummary = inputSummary; CreatedAt = DateTime.UtcNow
        Memory = mem; StateText = stateText }
    store.Save c
    c

  member this.CreateBranch(id: string, inputSummary: string) : Checkpoint =
    let c = self.CaptureCheckpoint(id, Some currentId, inputSummary)
    currentId <- id
    c

  member this.RestoreCheckpoint(id: string) =
    // CheckpointStore.Load validates every file before either machine changes.
    let c = store.Load id
    let oldMemory, oldState = oracle.SaveState()
    let oldKeys = pressedKeys
    let applyKeys (setKey: (int * int * bool) -> unit) (stateText: string) =
      let kv = StateText.parse stateText
      for row in 0 .. 7 do
        for bit in 0 .. 4 do
          setKey(row, bit, false)
      for row in 0 .. 7 do
        for bit in 0 .. 4 do
          let key = sprintf "key%d_%d" row bit
          match kv.TryGetValue key with
          | true, value when value = "true" || value = "1" -> setKey(row, bit, true)
          | _ -> ()
    // Preflight the port restore on a fresh machine. This makes the eventual
    // swap atomic from Session's point of view and avoids mutating the live
    // port before the oracle restore has succeeded.
    let candidate = Jetpac2.Core.Machine()
    attachPort candidate
    candidate.LoadState(c.Memory, c.StateText)
    applyKeys candidate.SetKey c.StateText
    try
      oracle.LoadState(c.Memory, c.StateText)
      applyKeys oracle.SetKey c.StateText
      port <- candidate
      pressedKeys <-
        [ for row in 0 .. 7 do
            for bit in 0 .. 4 do
              let key = sprintf "key%d_%d" row bit
              let kv = StateText.parse c.StateText
              match kv.TryGetValue key with
              | true, value when value = "true" || value = "1" -> yield (row, bit)
              | _ -> () ]
        |> Set.ofList
      beeperEvents.Clear()
      currentId <- id
      frame <- c.Frame
    with _ ->
      oracle.LoadState(oldMemory, oldState)
      for row, bit in oldKeys do oracle.SetKey(row, bit, true)
      pressedKeys <- oldKeys
      reraise()

  member this.CaptureOptions
    with get () = captureOptions
    and set value = captureOptions <- value

  member private this.AutoCaptureIfDue() =
    match captureOptions.FrameInterval with
    | Some interval when interval > 0 && frame > 0 && frame % interval = 0 ->
      let id = sprintf "auto-%d" frame
      self.CaptureCheckpoint(id, Some currentId, sprintf "automatic frame %d" frame) |> ignore
    | _ -> ()

  member this.RunFrame() =
    match startupPolicy, traceWriter with
    | Some policy, Some _ when not explorationComplete ->
      let frameEnd = oracle.FrameEnd
      while int64 (oracle.DebugZ80.CycleCount()) < frameEnd
            && oracle.ExecutedAddressCount < policy.NewExecutableAddresses
            && frame < policy.MaxFrames do
        oracle.ExecuteInstruction()
      if int64 (oracle.DebugZ80.CycleCount()) >= frameEnd && not explorationComplete then
        oracle.AdvanceFrameBoundary()
        frame <- frame + 1
    | _ ->
      oracle.RunGameFrame()
      frame <- frame + 1
    flushTraceBatch ()
    self.AutoCaptureIfDue()
  /// Replay both machines using absolute emulated T-state input events. Events
  /// are applied before the next instruction at or after their timestamp.
  member this.RunTimeline(framesN: int, timeline: InputTimeline) =
    if framesN < 0 then invalidArg (nameof framesN) "Frame count cannot be negative"
    let mutable pending = timeline.Events |> List.sortBy (fun event -> event.TState)
    for _ in 1 .. framesN do
      let frameEnd = oracle.FrameEnd
      while not (List.isEmpty pending) && (List.head pending).TState < frameEnd do
        let event = List.head pending
        let current = int64 (oracle.DebugZ80.CycleCount())
        let target = max current event.TState
        oracle.RunUntil target
        while port.CycleCount() < target do port.Step()
        self.SetKey(event.Row, event.Bit, event.Pressed)
        pending <- List.tail pending
      oracle.RunUntil frameEnd
      while port.CycleCount() < frameEnd do port.Step()
      oracle.AdvanceFrameBoundary()
      port.FrameEnd <- port.FrameEnd + 69888L
      frame <- frame + 1
      flushTraceBatch ()
      self.AutoCaptureIfDue()


  member this.SetKey(row: int, bit: int, pressed: bool) =
    let wasPressed = pressedKeys.Contains(row, bit)
    oracle.SetKey(row, bit, pressed)
    port.SetKey(row, bit, pressed)
    pressedKeys <-
      if pressed then Set.add (row, bit) pressedKeys
      else Set.remove (row, bit) pressedKeys
    if recordingInput && wasPressed <> pressed then
      timelineEvents.Add
        { TState = this.CycleCount
          Frame = frame
          Row = row
          Bit = bit
          Pressed = pressed }

  member this.RunDiff(framesN: int, script: (int * int * int * bool) list) : DiffReport =
    synchronizePort ()
    let report = Differential.runDiff oracle port framesN script
    // The diff drives the oracle on a separate timeline; drop its OUTs so
    // they never pollute the WAV capture.
    beeperEvents.Clear()
    report
