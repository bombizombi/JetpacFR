namespace JetpacFR.Desktop

open System
open System.IO
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Shapes
open System.Windows.Threading
open JetpacFR.Core
open NAudio.Wave

/// NAudio playback: 44100 Hz mono 16-bit; per-frame beeper samples appended to
/// a buffered provider (same plumbing as Jetpac3.Desktop). Muted by default;
/// when enabled the beeper waveform is scaled to 10% volume before the 16-bit
/// PCM conversion.
module Audio =
  let private waveOut = new WaveOutEvent()
  let private provider = new BufferedWaveProvider(WaveFormat(44100, 16, 1))

  do
    provider.DiscardOnBufferOverflow <- true
    waveOut.Init provider
    waveOut.Play()

  /// Muted by default; the user explicitly enables sound.
  let mutable Enabled = false
  /// 10% volume: the beeper waveform (+-0.8) is scaled down before the 16-bit
  /// PCM conversion so output is a tenth of full volume.
  let volume = 0.2f

  let Play (samples: float32[]) =
    if Enabled then
      let bytes = Array.zeroCreate<byte> (samples.Length * 2)
      for i in 0 .. samples.Length - 1 do
        let v = max -1.0f (min 1.0f (samples[i] * volume))
        let pcm = int (v * 32767.0f)
        bytes[i * 2] <- byte (pcm &&& 0xFF)
        bytes[i * 2 + 1] <- byte ((pcm >>> 8) &&& 0xFF)
      provider.AddSamples(bytes, 0, bytes.Length)

  let SetEnabled (on: bool) =
    Enabled <- on
    provider.ClearBuffer()

  let Stop () =
    waveOut.Stop()
    waveOut.Dispose()

/// One row of the cinema disassembly list.
type DisasmRow =
  { Tag: string
    Brush: SolidColorBrush
    IsCurrent: bool }

/// One row of the mined-routine list.
type RoutineRow =
  { Tag: string
    Brush: SolidColorBrush
    IsSelected: bool
    Entry: int }

type MainWindow() as self =
  inherit Window()

  let mutable session: TraceSession option = None
  let bootTask =
    System.Threading.Tasks.Task.Run(fun () ->
      TraceSession(LocalAssets.find "48.rom", LocalAssets.find "Jetpac.tzx", 4_000_000))

  // ---- palette -----------------------------------------------------------
  let bg = SolidColorBrush(Color.FromRgb(0x10uy, 0x10uy, 0x16uy))
  let panel = SolidColorBrush(Color.FromRgb(0x18uy, 0x1Cuy, 0x24uy))
  let normal = SolidColorBrush(Color.FromRgb(0xC8uy, 0xC8uy, 0xCEuy))
  let dim = SolidColorBrush(Color.FromRgb(0x8Auy, 0x8Auy, 0x92uy))
  let green = SolidColorBrush(Color.FromRgb(0x4Euy, 0xE0uy, 0x60uy))
  let cyan = SolidColorBrush(Color.FromRgb(0x4Euy, 0xD0uy, 0xE0uy))
  let yellow = SolidColorBrush(Color.FromRgb(0xE6uy, 0xD0uy, 0x4Euy))
  let red = SolidColorBrush(Color.FromRgb(0xE8uy, 0x54uy, 0x54uy))
  let orange = SolidColorBrush(Color.FromRgb(0xE8uy, 0x8Auy, 0x2Euy))
  let hiBg = SolidColorBrush(Color.FromRgb(0x2Euy, 0x34uy, 0x44uy))
  let mono = FontFamily("Consolas")

  // ---- bitmaps -----------------------------------------------------------
  let gameBitmap = WriteableBitmap(320, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
  let heatBmp = WriteableBitmap(256, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
  let stripBmp = WriteableBitmap(512, 24, 96.0, 96.0, PixelFormats.Bgra32, null)
  let heatPixels = Array.zeroCreate<byte> (256 * 256 * 4)
  let stripPixels = Array.zeroCreate<byte> (512 * 24 * 4)

  let heatLut =
    [| Color.FromRgb(0x08uy, 0x08uy, 0x0Cuy)
       Color.FromRgb(0x0Auy, 0x1Euy, 0x4Auy)
       Color.FromRgb(0x0Auy, 0x3Auy, 0x5Euy)
       Color.FromRgb(0x0Auy, 0x5Cuy, 0x6Auy)
       Color.FromRgb(0x12uy, 0x86uy, 0x5Euy)
       Color.FromRgb(0x3Auy, 0xA8uy, 0x3Cuy)
       Color.FromRgb(0x96uy, 0xB8uy, 0x2Cuy)
       Color.FromRgb(0xD8uy, 0xA0uy, 0x22uy)
       Color.FromRgb(0xE8uy, 0x66uy, 0x18uy)
       Color.FromRgb(0xF0uy, 0x38uy, 0x28uy) |]

  // ---- state -------------------------------------------------------------
  let mutable running = false
  let mutable cursor = 0
  let mutable built: Trace option = None
  let mutable builtAtCount = -1
  let mutable loaded: Trace option = None
  let mutable syncingSlider = false
  let mutable cinemaPlaying = false
  let mutable cinemaSpeed = 10
  let mutable userDragging = false
  let mutable markerPc = 0
  let mutable minedRoutines: Miner.Routine list = []
  let mutable minedEdges: Miner.CallEdge list = []
  let mutable selectedEntries: Set<int> = Set.empty
  let mutable syncingRoutines = false
  let mutable activeRoutine: Miner.Routine option = None
  let mutable pcCyclesCache: int64[] option = None
  let mutable pcCyclesForTrace: Trace = Unchecked.defaultof<Trace>

  let frameTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 20.0)
  let uiTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 100.0)
  let cinemaTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 30.0)

  // ---- controls ----------------------------------------------------------
  let gameImage = Image()
  let heatImage = Image()
  let stripImage = Image()
  let disasmList = ListBox()
  let slider = Slider()
  let cursorLabel = TextBlock()
  let statusText = TextBlock()
  let recordToggle = CheckBox(Content = "Rec trace", IsChecked = Nullable<bool>(true))
  let playBtn = Button(Content = "Play")
  let regCells = Dictionary<string, TextBlock>()
  let flagCells = Array.init 8 (fun _ -> TextBlock())
  let mutable pcCell = TextBlock()
  let routineList = ListBox()
  let routineDetail = TextBlock()
  let routineCountLabel = TextBlock()
  let graphCanvas = Canvas()
  let trayWrap = WrapPanel()
  let trayCountLabel = TextBlock()
  let clearTrayBtn = Button(Content = "Clear")
  let mineBtn = Button(Content = "Mine routines")
  let contractText = TextBlock()
  let contractLabel = TextBlock()
  let savePromptBtn = Button(Content = "Save .md")
  let copyPromptBtn = Button(Content = "Copy prompt")
  let pasteBox = TextBox()
  let theaterText = TextBlock()
  let validateBtn = Button(Content = "Validate selected")
  let stageBtn = Button(Content = "Stage to Lifted/")
  let queuePromptsBtn = Button(Content = "Prompts for queue")
  let validateQueueBtn = Button(Content = "Validate queue")

  let keyMap (key: Key) : (int * int) list =
    match key with
    | Key.A -> [ (1, 0) ]
    | Key.S -> [ (1, 1) ]
    | Key.D -> [ (1, 2) ]
    | Key.F -> [ (1, 3) ]
    | Key.G -> [ (1, 4) ]
    | Key.Q -> [ (2, 0) ]
    | Key.W -> [ (2, 1) ]
    | Key.E -> [ (2, 2) ]
    | Key.R -> [ (2, 3) ]
    | Key.T -> [ (2, 4) ]
    | Key.P -> [ (5, 0) ]
    | Key.O -> [ (5, 1) ]
    | Key.I -> [ (5, 2) ]
    | Key.U -> [ (5, 3) ]
    | Key.Y -> [ (5, 4) ]
    | Key.L -> [ (6, 1) ]
    | Key.K -> [ (6, 2) ]
    | Key.J -> [ (6, 3) ]
    | Key.H -> [ (6, 4) ]
    | Key.M -> [ (7, 2) ]
    | Key.N -> [ (7, 3) ]
    | Key.B -> [ (7, 4) ]
    | Key.Z -> [ (0, 1) ]
    | Key.X -> [ (0, 2) ]
    | Key.C -> [ (0, 3) ]
    | Key.V -> [ (0, 4) ]
    | Key.D1 -> [ (3, 0) ]
    | Key.D2 -> [ (3, 1) ]
    | Key.D3 -> [ (3, 2) ]
    | Key.D4 -> [ (3, 3) ]
    | Key.D5 -> [ (3, 4) ]
    | Key.D0 -> [ (4, 0) ]
    | Key.D9 -> [ (4, 1) ]
    | Key.D8 -> [ (4, 2) ]
    | Key.D7 -> [ (4, 3) ]
    | Key.D6 -> [ (4, 4) ]
    | Key.Space -> [ (7, 0) ]
    | Key.Enter -> [ (6, 0) ]
    | Key.LeftShift | Key.RightShift -> [ (0, 0) ]
    | Key.LeftCtrl | Key.RightCtrl -> [ (7, 1) ]
    | Key.Left -> [ (0, 0); (3, 4) ]
    | Key.Right -> [ (0, 0); (4, 2) ]
    | Key.Up -> [ (0, 0); (4, 3) ]
    | Key.Down -> [ (0, 0); (4, 4) ]
    | _ -> []

  let isCall (b: byte) =
    match b with
    | 0xCDuy | 0xC4uy | 0xCCuy | 0xD4uy | 0xDCuy | 0xE4uy | 0xECuy | 0xF4uy | 0xFCuy -> true
    | _ -> false

  let isRet (b: byte) =
    match b with
    | 0xC9uy | 0xC0uy | 0xC8uy | 0xD0uy | 0xD8uy | 0xE0uy | 0xE8uy | 0xF0uy | 0xF8uy -> true
    | _ -> false

  let isBranch (b: byte) =
    isCall b || isRet b
    || match b with
       | 0xC3uy | 0xC2uy | 0xCAuy | 0xD2uy | 0xDAuy | 0xE2uy | 0xEAuy | 0xF2uy | 0xFAuy
       | 0x10uy | 0x18uy | 0x20uy | 0x28uy | 0x30uy | 0x38uy | 0xE9uy -> true
       | _ -> false

  // ---- trace plumbing ----------------------------------------------------
  let ensureBuilt () =
    match session with
    | Some s ->
      let recorder = s.Recorder
      if builtAtCount <> recorder.EntryCount then
        built <- Some(recorder.Build())
        builtAtCount <- recorder.EntryCount
    | None -> ()

  let currentTrace () : Trace option =
    match loaded with
    | Some t -> Some t
    | None ->
      ensureBuilt ()
      built

  let currentEntryCount () =
    match currentTrace () with
    | Some t -> t.Entries.Length
    | None -> 0

  let clampCursor () =
    let n = currentEntryCount ()
    if n = 0 then cursor <- 0
    else cursor <- max 0 (min cursor (n - 1))

  let buildTraceNow () =
    ensureBuilt ()

  // ---- view refresh ------------------------------------------------------
  let presentGame () =
    match session with
    | Some s ->
      gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), s.ScreenBuffer, 320 * 4, 0)
    | None -> ()

  /// Static linear disassembly of port memory around `pc`, shown when the
  /// emulator stops (e.g. "no code at 0xNNNN"). The row at the error address
  /// is exact; rows before it may be misaligned if execution entered mid-
  /// instruction (linear sweep from an 8-aligned start below the address).
  let showErrorDisasm (pc: int) =
    match session with
    | Some s ->
      let mem = s.Memory
      let start = ((pc - 0x40) + 0x10000) &&& 0xFFF8
      let rows = ResizeArray<DisasmRow>()
      let mutable addr = start
      while rows.Count < 48 && addr <= start + 0x80 do
        let insn = Disasm.disasmMemory mem addr
        let hex =
          [ for i in 0 .. insn.Length - 1 -> sprintf "%02X" mem[(addr + i) &&& 0xFFFF] ]
          |> String.concat " "
        let isErr = addr = pc
        let tag =
          if isErr then
            sprintf "-> %04X  %-11s  %s   <<< cannot execute here" addr hex insn.Text
          else
            sprintf "   %04X  %-11s  %s" addr hex insn.Text
        rows.Add { Tag = tag; Brush = (if isErr then red else normal); IsCurrent = isErr }
        addr <- (addr + insn.Length) &&& 0xFFFF
      disasmList.ItemsSource <- rows
      cursorLabel.Text <- sprintf "emulator stopped - static disassembly around 0x%04X" pc
    | None -> ()

  let renderFrame () =
    match session with
    | Some s ->
      try
        let frameStart, _ = s.RunFrame()
        presentGame ()
        Audio.Play(s.DrainBeeperSamples(frameStart))
      with ex ->
        running <- false
        frameTimer.Stop()
        let pc = s.Regs.Pc()
        statusText.Text <- sprintf "emulator error at 0x%04X: %s" pc ex.Message
        showErrorDisasm pc
    | None -> ()

  let currentSelfModified () : bool[] =
    match loaded with
    | Some t -> t.SelfModified
    | None ->
      match session with
      | Some s -> s.Recorder.SelfModified
      | None -> Array.zeroCreate<bool> 0x10000

  /// Live per-PC counts: the recorder's window while playing, the loaded
  /// trace's otherwise. Never triggers a trace build (the heatmap refreshes
  /// at 10 Hz during recording; rebuilding 68 MB per tick would be ~680 MB/s).
  let currentCounts () : int[] =
    match loaded with
    | Some t -> t.PerPcCount
    | None ->
      match session with
      | Some s -> s.Recorder.PerPcCount
      | None -> Array.zeroCreate<int> 0x10000

  let rowFor (t: Trace) (e: TraceEntry) (idx: int) (isCurrent: bool) : DisasmRow =
    if e.Length = 0uy then
      { Tag = sprintf "%07d  ----  INT -> %04X   (%d tstates)" idx e.Target e.Cycles
        Brush = red
        IsCurrent = isCurrent }
    else
      let bytes = [| e.B0; e.B1; e.B2; e.B3 |]
      let insn = Disasm.disasmBytes bytes 0
      let hex =
        [ for i in 0 .. int e.Length - 1 -> sprintf "%02X" bytes[i] ]
        |> String.concat " "
      let tail =
        if e.Taken = 1uy then sprintf "   -> %04X" e.Target
        elif isBranch e.B0 then "   (not taken)"
        else ""
      let sm =
        if (currentSelfModified ())[int e.Pc] then "   [self-mod]" else ""
      let lifted =
        match Jetpac3.Core.LiftedRoutines.registryHook (int e.Pc) with
        | Some _ -> "   [lift]"
        | None -> ""
      let brush =
        if isCall e.B0 then cyan
        elif isRet e.B0 then yellow
        elif isBranch e.B0 then green
        else normal
      { Tag = sprintf "%07d  %04X  %-11s  %s%s%s%s" idx e.Pc hex insn.Text tail sm lifted
        Brush = brush
        IsCurrent = isCurrent }

  let refreshDisasm () =
    match currentTrace () with
    | None -> disasmList.ItemsSource <- null
    | Some t ->
      if t.Entries.Length = 0 then disasmList.ItemsSource <- null
      else
        let c = max 0 (min cursor (t.Entries.Length - 1))
        let rows = ResizeArray<DisasmRow>()
        for j in -20 .. 20 do
          let idx = c + j
          if idx >= 0 && idx < t.Entries.Length then
            rows.Add(rowFor t t.Entries[idx] idx (idx = c))
        disasmList.ItemsSource <- rows

  let setReg (name: string) (v: int) =
    match regCells.TryGetValue name with
    | true, tb -> tb.Text <- sprintf "%04X" v
    | _ -> ()

  let updateFlags (af: int) =
    let f = af &&& 0xFF
    let bits = [| 0x80; 0x40; 0x20; 0x10; 0x08; 0x04; 0x02; 0x01 |]
    for i in 0 .. 7 do
      flagCells[i].Text <- if f &&& bits[i] <> 0 then "1" else "0"
      flagCells[i].Foreground <- if f &&& bits[i] <> 0 then green else dim

  let refreshRegs () =
    match currentTrace () with
    | Some t when t.Entries.Length > 0 ->
      let c = max 0 (min cursor (t.Entries.Length - 1))
      let e = t.Entries[c]
      let snapIdx = TraceQuery.nearestSnapshotBefore t.Snapshots e.Tick
      if snapIdx >= 0 then
        let s = t.Snapshots[snapIdx]
        setReg "AF" (int s.Af)
        setReg "BC" (int s.Bc)
        setReg "DE" (int s.De)
        setReg "HL" (int s.Hl)
        setReg "AF'" (int s.Af2)
        setReg "BC'" (int s.Bc2)
        setReg "DE'" (int s.De2)
        setReg "HL'" (int s.Hl2)
        setReg "IX" (int s.Ix)
        setReg "IY" (int s.Iy)
        setReg "SP" (int s.Sp)
        setReg "I" (int s.I)
        setReg "R" (int s.R)
        pcCell.Text <- sprintf "%04X" e.Pc
        updateFlags (int s.Af)
      markerPc <- int e.Pc
    | _ -> ()

  let riskBrush (risk: string) =
    match risk with
    | "green" -> green
    | "yellow" -> yellow
    | "orange" -> orange
    | "red" -> red
    | _ -> normal

  let refreshHeatmap () =
    let counts = currentCounts ()
    let selfMod = currentSelfModified ()
    let maxC = max 1 (Array.max counts)
    let logMax = log10 (float maxC)
    let mutable i = 0
    for y in 0 .. 255 do
      for x in 0 .. 255 do
        let c = counts[i]
        let li = if c <= 0 then 0 else min 9 (int (9.0 * (log10 (float c) / logMax)))
        let col = heatLut[li]
        let p = i * 4
        if selfMod[i] then
          // executed-then-written: blend toward magenta so self-modified
          // code stands out from the heat colors.
          heatPixels[p] <- byte ((int col.B + 0xC0) / 2)
          heatPixels[p + 1] <- byte ((int col.G + 0x30) / 2)
          heatPixels[p + 2] <- byte ((int col.R + 0xF0) / 2)
        else
          heatPixels[p] <- col.B
          heatPixels[p + 1] <- col.G
          heatPixels[p + 2] <- col.R
        heatPixels[p + 3] <- 255uy
        i <- i + 1
    let pc = markerPc
    let x = pc &&& 0xFF
    let y = pc >>> 8
    for dy in -1 .. 1 do
      for dx in -1 .. 1 do
        let yy = y + dy
        if yy >= 0 && yy < 256 then
          let p = ((yy * 256) + ((x + dx) &&& 0xFF)) * 4
          heatPixels[p] <- 255uy
          heatPixels[p + 1] <- 255uy
          heatPixels[p + 2] <- 255uy
          heatPixels[p + 3] <- 255uy
    heatBmp.WritePixels(Int32Rect(0, 0, 256, 256), heatPixels, 256 * 4, 0)

  let segmentsOf (t: Trace) : int[] =
    let n = max 1 (t.Entries.Length / 512)
    let segs = Array.zeroCreate<int> 512
    for e in t.Entries do
      segs[min 511 (int e.Tick / n)] <- segs[min 511 (int e.Tick / n)] + 1
    segs

  let refreshStrip () =
    let segs =
      match loaded with
      | Some t -> segmentsOf t
      | None ->
        match session with
        | Some s -> s.Recorder.SegmentCounts
        | None -> Array.empty
    if segs.Length > 0 then
      let maxS = max 1 (Array.max segs)
      let logMax = log10 (float (maxS + 1))
      for sx in 0 .. segs.Length - 1 do
        let v = log10 (float (segs[sx] + 1)) / logMax
        let r = byte (60 + int (v * 160.0))
        let g = byte (120 + int (v * 100.0))
        let b = byte (60 + int (v * 60.0))
        for sy in 0 .. 23 do
          let p = (sy * 512 + sx) * 4
          stripPixels[p] <- b
          stripPixels[p + 1] <- g
          stripPixels[p + 2] <- r
          stripPixels[p + 3] <- 255uy
      let n = currentEntryCount ()
      if n > 1 then
        let cx = int (int64 cursor * 511L / int64 (n - 1))
        for sy in 0 .. 23 do
          let p = (sy * 512 + cx) * 4
          stripPixels[p] <- 255uy
          stripPixels[p + 1] <- 255uy
          stripPixels[p + 2] <- 255uy
          stripPixels[p + 3] <- 255uy
      stripBmp.WritePixels(Int32Rect(0, 0, 512, 24), stripPixels, 512 * 4, 0)

  let refreshCursorLabel () =
    let n = currentEntryCount ()
    if n = 0 then cursorLabel.Text <- "no trace yet - play the game"
    else
      let c = max 0 (min cursor (n - 1))
      cursorLabel.Text <- sprintf "instr %d / %d   pc=%04X" c (n - 1) (int currentTrace().Value.Entries[c].Pc)

  let refreshAll () =
    refreshDisasm ()
    refreshRegs ()
    refreshHeatmap ()
    refreshStrip ()
    refreshCursorLabel ()

  let syncSlider () =
    let n = currentEntryCount ()
    syncingSlider <- true
    if n > 1 then
      slider.Minimum <- 0.0
      slider.Maximum <- float (n - 1)
      slider.Value <- float (max 0 (min cursor (n - 1)))
      slider.IsEnabled <- true
    else
      slider.IsEnabled <- false
      slider.Value <- 0.0
    syncingSlider <- false
  // ---- phase 2: mined routines, call graph, selection --------------------
  let refreshRoutines () =
    syncingRoutines <- true
    routineList.ItemsSource <-
      minedRoutines
      |> List.map (fun r ->
        { Tag =
            sprintf "%04X  calls=%5d  span=%04X-%04X  incl=%7d  excl=%7d  %s"
              r.Entry r.CallCount r.SpanLo r.SpanHi r.InclusiveTStates r.ExclusiveTStates r.Risk
          Brush = riskBrush r.Risk
          IsSelected = selectedEntries.Contains r.Entry
          Entry = r.Entry })
    routineCountLabel.Text <- sprintf "%d routines" minedRoutines.Length
    syncingRoutines <- false

  let showRoutineDetail (r: Miner.Routine) =
    let loops =
      r.LoopExtents
      |> List.truncate 5
      |> List.map (fun (back, tgt, iters) -> sprintf "%04X->%04X x%d" back tgt iters)
    routineDetail.Text <-
      sprintf "%04X  (%s)\ncalls=%d  span=%04X-%04X\nincl=%d  excl=%d tstates\nself-modifying=%b  overlapping=%b\n%s%s\ninput samples: %d"
        r.Entry r.Risk r.CallCount r.SpanLo r.SpanHi r.InclusiveTStates r.ExclusiveTStates
        r.SelfModifying r.Overlapping
        (r.Reasons |> String.concat "; ")
        (if List.isEmpty loops then "" else "\nloops: " + String.concat ", " loops)
        r.InputSamples.Length

  /// Per-PC total cycle counts over the current trace, cached per trace
  /// instance (reference identity), for contract averages.
  let getPcCycles () : int64[] =
    match currentTrace () with
    | None -> Array.zeroCreate<int64> 0x10000
    | Some t ->
      if not (System.Object.ReferenceEquals(pcCyclesForTrace, t)) then
        let cyc = Array.zeroCreate<int64> 0x10000
        for e in t.Entries do
          if e.Length <> 0uy then cyc[int e.Pc] <- cyc[int e.Pc] + int64 e.Cycles
        pcCyclesCache <- Some cyc
        pcCyclesForTrace <- t
      match pcCyclesCache with
      | Some c -> c
      | None -> Array.zeroCreate<int64> 0x10000

  let generateContract () =
    match activeRoutine with
    | None -> contractText.Text <- "select a mined routine to generate its contract + prompt"
    | Some r ->
      match currentTrace (), session with
      | Some t, Some s ->
        let contract = Contract.extract s.Memory t r (getPcCycles ())
        let prompt = Prompt.generate contract
        contractText.Text <- prompt
        contractLabel.Text <-
          sprintf "contract for 0x%04X  span=%04X-%04X  calls=%d  writes=%d"
            r.Entry r.SpanLo r.SpanHi r.CallCount contract.WriteRanges.Length
      | _ -> contractText.Text <- "no trace to extract from"

  let rec refreshTray () =
    trayWrap.Children.Clear()
    for entry in selectedEntries |> Set.toList do
      match minedRoutines |> List.tryFind (fun r -> r.Entry = entry) with
      | Some r ->
        let chip = Button(Content = sprintf "%04X x%d" r.Entry r.CallCount)
        chip.Margin <- Thickness(2.0)
        chip.Click.Add(fun _ -> selectRoutine r.Entry false)
        trayWrap.Children.Add chip |> ignore
      | None -> ()
    trayCountLabel.Text <- sprintf "%d in lift queue" selectedEntries.Count

  and drawGraph () =
    graphCanvas.Children.Clear()
    if List.isEmpty minedRoutines then
      graphCanvas.Children.Add(TextBlock(Text = "mine a trace first (Functions tab)", Foreground = dim)) |> ignore
    else
      // BFS-ish depth assignment from the synthetic root (-1).
      let depth = Dictionary<int, int>()
      depth[-1] <- 0
      for r in minedRoutines do depth[r.Entry] <- 100
      let mutable changed = true
      let mutable guard = 0
      while changed && guard < 20 do
        changed <- false
        guard <- guard + 1
        for e in minedEdges do
          let dFrom =
            match depth.TryGetValue e.Caller with
            | true, v -> v
            | _ -> 100
          if dFrom < 100 && depth[e.Callee] > dFrom + 1 then
            depth[e.Callee] <- dFrom + 1
            changed <- true
      let colWidth = 150.0
      let rowHeight = 44.0
      let columns = Dictionary<int, ResizeArray<int>>()
      for r in minedRoutines do
        let d =
          match depth.TryGetValue r.Entry with
          | true, v -> min v 9
          | _ -> 1
        match columns.TryGetValue d with
        | true, list -> list.Add r.Entry
        | _ ->
          let list = ResizeArray<int>()
          list.Add r.Entry
          columns[d] <- list
      let pos = Dictionary<int, Point>()
      for kv in columns do
        kv.Value |> Seq.iteri (fun row entry ->
          pos[entry] <- Point(20.0 + float kv.Key * colWidth, 20.0 + float row * rowHeight))
      pos[-1] <- Point(20.0, 20.0)
      // edges under nodes
      for e in minedEdges do
        match pos.TryGetValue e.Caller, pos.TryGetValue e.Callee with
        | (true, p1), (true, p2) ->
          let line = Line(X1 = p1.X + 104.0, Y1 = p1.Y + 14.0, X2 = p2.X, Y2 = p2.Y + 14.0)
          line.Stroke <- dim
          line.StrokeThickness <- 1.0
          graphCanvas.Children.Add line |> ignore
          let label = TextBlock(Text = string e.Count, Foreground = dim, FontSize = 10.0, FontFamily = mono)
          Canvas.SetLeft(label, (p1.X + p2.X) / 2.0)
          Canvas.SetTop(label, (p1.Y + p2.Y) / 2.0)
          graphCanvas.Children.Add label |> ignore
        | _ -> ()
      let nodeBox (entry: int) (title: string) (sub: string) (brush: SolidColorBrush) =
        let border = Border(Width = 104.0, Height = 28.0, BorderBrush = brush, BorderThickness = Thickness(2.0), Background = panel)
        let stack = StackPanel()
        stack.Children.Add(TextBlock(Text = title, Foreground = normal, FontFamily = mono, FontSize = 11.0, HorizontalAlignment = HorizontalAlignment.Center)) |> ignore
        stack.Children.Add(TextBlock(Text = sub, Foreground = dim, FontFamily = mono, FontSize = 9.0, HorizontalAlignment = HorizontalAlignment.Center)) |> ignore
        border.Child <- stack
        border.Tag <- box entry
        border.MouseLeftButtonDown.Add(fun _ -> selectRoutine entry true)
        if selectedEntries.Contains entry then
          border.Background <- hiBg
        graphCanvas.Children.Add border |> ignore
        Canvas.SetLeft(border, pos[entry].X)
        Canvas.SetTop(border, pos[entry].Y)
      nodeBox -1 "TOP" "" normal
      for r in minedRoutines do
        nodeBox r.Entry (sprintf "%04X" r.Entry) (sprintf "x%d" r.CallCount) (riskBrush r.Risk)
      let maxDepth = columns.Keys |> Seq.fold max 0
      let maxRows = columns.Values |> Seq.map (fun l -> l.Count) |> Seq.fold max 0
      graphCanvas.Width <- 40.0 + (float (maxDepth + 1)) * colWidth
      graphCanvas.Height <- 40.0 + float maxRows * rowHeight
  and selectRoutine (entry: int) (jump: bool) =
    let wasSelected = selectedEntries.Contains entry
    selectedEntries <-
      if wasSelected then Set.remove entry selectedEntries
      else Set.add entry selectedEntries
    match minedRoutines |> List.tryFind (fun r -> r.Entry = entry) with
    | Some r ->
      activeRoutine <- Some r
      showRoutineDetail r
    | None -> ()
    refreshRoutines ()
    refreshTray ()
    drawGraph ()
    generateContract ()
    if (not wasSelected) && jump then
      match currentTrace () with
      | Some t when entry < t.FirstIndexAtPc.Length && t.FirstIndexAtPc[entry] >= 0 ->
        cursor <- t.FirstIndexAtPc[entry]
        refreshAll ()
        syncSlider ()
      | _ -> ()

  let mineNow () =
    buildTraceNow ()
    match currentTrace () with
    | Some t when t.Entries.Length > 0 ->
      let routines, edges = Miner.mine t
      minedRoutines <- routines
      minedEdges <- edges
      selectedEntries <- Set.empty
      refreshRoutines ()
      refreshTray ()
      drawGraph ()
      statusText.Text <- sprintf "mined %d routines, %d call edges (self-mod addresses: %d)"
        routines.Length edges.Length t.SelfModCount
    | _ -> statusText.Text <- "no trace to mine - play the game first"



  // ---- actions -----------------------------------------------------------
  let pauseGame () =
    running <- false
    frameTimer.Stop()
    cinemaPlaying <- false
    cinemaTimer.Stop()
    playBtn.Content <- "Play"
    buildTraceNow ()
    clampCursor ()
    syncSlider ()
    refreshAll ()

  let seek (delta: int) =
    cinemaPlaying <- false
    cinemaTimer.Stop()
    playBtn.Content <- "Play"
    buildTraceNow ()
    clampCursor ()
    cursor <- max 0 (min (cursor + delta) (max 0 (currentEntryCount () - 1)))
    refreshAll ()
    syncSlider ()

  let stepBack100 () = seek -100
  let stepBack10 () = seek -10
  let stepBack1 () = seek -1
  let stepFwd1 () = seek 1
  let stepFwd10 () = seek 10
  let stepFwd100 () = seek 100

  let toggleCinema () =
    buildTraceNow ()
    if cinemaPlaying then
      cinemaPlaying <- false
      cinemaTimer.Stop()
      playBtn.Content <- "Play"
    else
      if currentEntryCount () > 0 then
        cinemaPlaying <- true
        cinemaTimer.Start()
        playBtn.Content <- "Pause"

  let saveTrace () =
    try
      let t =
        match loaded with
        | Some t -> t
        | None ->
          buildTraceNow ()
          match built with
          | Some t -> t
          | None -> failwith "no trace recorded yet"
      if t.Entries.Length = 0 then failwith "trace is empty"
      let dlg = Microsoft.Win32.SaveFileDialog(Filter = "Jetpac trace (*.jpt)|*.jpt", FileName = "trace.jpt")
      if dlg.ShowDialog() = Nullable<bool>(true) then
        TraceCodec.save t dlg.FileName
        statusText.Text <- sprintf "saved %d instructions to %s" t.Entries.Length dlg.FileName
    with ex -> statusText.Text <- "save failed: " + ex.Message

  let loadTrace () =
    let dlg = Microsoft.Win32.OpenFileDialog(Filter = "Jetpac trace (*.jpt)|*.jpt")
    if dlg.ShowDialog() = Nullable<bool>(true) then
      try
        let t = TraceCodec.load dlg.FileName
        loaded <- Some t
        cursor <- 0
        markerPc <- int t.Entries[0].Pc
        syncSlider ()
        refreshAll ()
        let routines, edges = Miner.mine t
        minedRoutines <- routines
        minedEdges <- edges
        selectedEntries <- Set.empty
        refreshRoutines ()
        refreshTray ()
        drawGraph ()
        statusText.Text <- sprintf "loaded %d instructions from %s; mined %d routines"
          t.Entries.Length dlg.FileName routines.Length
      with ex -> statusText.Text <- "load failed: " + ex.Message

  let startGame () =
    match session with
    | Some s ->
      recordToggle.IsChecked <- Nullable<bool>(s.Recorder.RecordEnabled)
      running <- true
      frameTimer.Start()
      statusText.Text <-
        sprintf "emulator ready (%s) - frame 0, recording %s"
          (if s.WarmStart then "cached entry state" else "booted to game entry")
          (if s.Recorder.RecordEnabled then "ON" else "OFF")
    | None -> ()

  // ---- construction ------------------------------------------------------
  do
    self.Title <- "JetpacFR - reversing lab"
    self.Width <- 1560.0
    self.Height <- 900.0
    self.Background <- bg

    gameImage.Source <- gameBitmap
    gameImage.Stretch <- Stretch.Uniform
    gameImage.Width <- 320.0
    gameImage.Height <- 256.0
    RenderOptions.SetBitmapScalingMode(gameImage, BitmapScalingMode.NearestNeighbor)

    heatImage.Source <- heatBmp
    heatImage.Width <- 256.0
    heatImage.Height <- 256.0
    heatImage.Stretch <- Stretch.None

    stripImage.Source <- stripBmp
    stripImage.Width <- 512.0
    stripImage.Height <- 24.0
    stripImage.Stretch <- Stretch.None

    // disassembly list template
    let itemTemplate = DataTemplate()
    let factory = FrameworkElementFactory(typeof<TextBlock>)
    factory.SetValue(TextBlock.TextProperty, Binding("Tag"))
    factory.SetValue(TextBlock.FontFamilyProperty, mono)
    factory.SetValue(TextBlock.FontSizeProperty, 13.0)
    factory.SetValue(TextBlock.ForegroundProperty, Binding("Brush"))
    itemTemplate.VisualTree <- factory
    disasmList.ItemTemplate <- itemTemplate
    let containerStyle = Style(typeof<ListBoxItem>)
    containerStyle.Setters.Add(Setter(ListBoxItem.BackgroundProperty, Brushes.Transparent))
    containerStyle.Setters.Add(Setter(ListBoxItem.PaddingProperty, Thickness(2.0, 0.0, 2.0, 0.0)))
    let trigger = DataTrigger(Binding = Binding("IsCurrent"), Value = box true)
    trigger.Setters.Add(Setter(ListBoxItem.BackgroundProperty, hiBg))
    containerStyle.Triggers.Add trigger
    disasmList.ItemContainerStyle <- containerStyle
    disasmList.Background <- panel
    disasmList.BorderThickness <- Thickness(0.0)
    disasmList.Foreground <- normal
    disasmList.HorizontalContentAlignment <- HorizontalAlignment.Left

    // mined-routine list template
    let ritemTemplate = DataTemplate()
    let rfactory = FrameworkElementFactory(typeof<TextBlock>)
    rfactory.SetValue(TextBlock.TextProperty, Binding("Tag"))
    rfactory.SetValue(TextBlock.FontFamilyProperty, mono)
    rfactory.SetValue(TextBlock.FontSizeProperty, 11.5)
    rfactory.SetValue(TextBlock.ForegroundProperty, Binding("Brush"))
    ritemTemplate.VisualTree <- rfactory
    routineList.ItemTemplate <- ritemTemplate
    let rstyle = Style(typeof<ListBoxItem>)
    rstyle.Setters.Add(Setter(ListBoxItem.BackgroundProperty, Brushes.Transparent))
    rstyle.Setters.Add(Setter(ListBoxItem.PaddingProperty, Thickness(2.0, 0.0, 2.0, 0.0)))
    let rtrigger = DataTrigger(Binding = Binding("IsSelected"), Value = box true)
    rtrigger.Setters.Add(Setter(ListBoxItem.BackgroundProperty, hiBg))
    rstyle.Triggers.Add rtrigger
    routineList.ItemContainerStyle <- rstyle
    routineList.Background <- panel
    routineList.BorderThickness <- Thickness(0.0)
    routineList.Foreground <- normal
    routineList.SelectionChanged.Add(fun _ ->
      if not syncingRoutines then
        match routineList.SelectedItem with
        | :? RoutineRow as row -> selectRoutine row.Entry true
        | _ -> ())

    // register board
    let regNames = [ "AF"; "BC"; "DE"; "HL"; "AF'"; "BC'"; "DE'"; "HL'"; "IX"; "IY"; "SP"; "I"; "R"; "PC" ]
    let regGrid = Grid()
    regGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(52.0)))
    regGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(64.0)))
    for (i, name) in List.indexed regNames do
      regGrid.RowDefinitions.Add(RowDefinition(Height = GridLength(20.0)))
      let nameTb = TextBlock(Text = name, Foreground = dim, FontFamily = mono, FontSize = 12.0, VerticalAlignment = VerticalAlignment.Center)
      let valTb = TextBlock(Text = "----", Foreground = normal, FontFamily = mono, FontSize = 12.0, VerticalAlignment = VerticalAlignment.Center)
      Grid.SetColumn(nameTb, 0)
      Grid.SetColumn(valTb, 1)
      Grid.SetRow(nameTb, i)
      Grid.SetRow(valTb, i)
      regGrid.Children.Add(nameTb) |> ignore
      regGrid.Children.Add(valTb) |> ignore
      regCells[name] <- valTb
    pcCell <- regCells["PC"]

    // flags row: S Z 5 H 3 V N C
    let flagLabels = [| "S"; "Z"; "5"; "H"; "3"; "V"; "N"; "C" |]
    let flagsPanel = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 0.0))
    for i in 0 .. 7 do
      let label = TextBlock(Text = flagLabels[i] + "=", Foreground = dim, FontFamily = mono, FontSize = 12.0)
      flagCells[i].Text <- "0"
      flagCells[i].Foreground <- dim
      flagCells[i].FontFamily <- mono
      flagCells[i].FontSize <- 12.0
      flagCells[i].Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
      flagsPanel.Children.Add(label) |> ignore
      flagsPanel.Children.Add(flagCells[i]) |> ignore

    // left column: game + registers
    let left = StackPanel(Margin = Thickness(8.0))
    left.Children.Add(gameImage) |> ignore
    let regHeader = TextBlock(Text = "registers (nearest snapshot)", Foreground = dim, FontSize = 12.0, Margin = Thickness(0.0, 8.0, 0.0, 2.0))
    left.Children.Add(regHeader) |> ignore
    left.Children.Add(regGrid) |> ignore
    left.Children.Add(flagsPanel) |> ignore

    // center: cinema
    let center = DockPanel(Margin = Thickness(8.0))
    let stripRow = StackPanel()
    stripRow.Children.Add(stripImage) |> ignore
    stripRow.Children.Add(slider) |> ignore
    DockPanel.SetDock(stripRow, Dock.Top)
    center.Children.Add(stripRow) |> ignore
    cursorLabel.Foreground <- dim
    cursorLabel.FontFamily <- mono
    cursorLabel.Margin <- Thickness(0.0, 2.0, 0.0, 2.0)
    DockPanel.SetDock(cursorLabel, Dock.Top)
    center.Children.Add(cursorLabel) |> ignore
    center.Children.Add(disasmList) |> ignore

    // right column: heatmap / functions / call graph
    let right = TabControl(Margin = Thickness(8.0), Background = panel)

    let heatTab = TabItem(Header = "Heatmap")
    let heatPanel = StackPanel()
    let heatHeader = TextBlock(Text = "64K execution heatmap (click to jump)", Foreground = dim, FontSize = 12.0)
    heatPanel.Children.Add heatHeader |> ignore
    heatPanel.Children.Add heatImage |> ignore
    let heatHint = TextBlock(Text = "black = never executed  magenta = executed then written", Foreground = dim, FontSize = 11.0)
    heatPanel.Children.Add heatHint |> ignore
    heatTab.Content <- heatPanel

    let funcTab = TabItem(Header = "Functions")
    let funcPanel = DockPanel()
    let funcTop = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))
    mineBtn.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
    funcTop.Children.Add mineBtn |> ignore
    routineCountLabel.Foreground <- dim
    routineCountLabel.VerticalAlignment <- VerticalAlignment.Center
    funcTop.Children.Add routineCountLabel |> ignore
    DockPanel.SetDock(funcTop, Dock.Top)
    funcPanel.Children.Add funcTop |> ignore
    routineDetail.Foreground <- normal
    routineDetail.FontFamily <- mono
    routineDetail.FontSize <- 11.0
    routineDetail.TextWrapping <- TextWrapping.Wrap
    routineDetail.Margin <- Thickness(0.0, 4.0, 0.0, 0.0)
    DockPanel.SetDock(routineDetail, Dock.Bottom)
    funcPanel.Children.Add routineDetail |> ignore
    funcPanel.Children.Add routineList |> ignore
    funcTab.Content <- funcPanel

    let graphTab = TabItem(Header = "Call graph")
    let graphScroll = ScrollViewer(HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
    graphScroll.Content <- graphCanvas
    graphTab.Content <- graphScroll

    let contractTab = TabItem(Header = "Contract/Prompt")
    let contractPanel = DockPanel()
    let contractTop = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))
    contractLabel.Foreground <- dim
    contractLabel.VerticalAlignment <- VerticalAlignment.Center
    contractLabel.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
    contractTop.Children.Add contractLabel |> ignore
    savePromptBtn.Margin <- Thickness(0.0, 0.0, 4.0, 0.0)
    contractTop.Children.Add savePromptBtn |> ignore
    contractTop.Children.Add copyPromptBtn |> ignore
    DockPanel.SetDock(contractTop, Dock.Top)
    contractPanel.Children.Add contractTop |> ignore
    contractText.Foreground <- normal
    contractText.FontFamily <- mono
    contractText.FontSize <- 11.0
    contractText.TextWrapping <- TextWrapping.Wrap
    let contractScroll = ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
    contractScroll.Content <- contractText
    contractPanel.Children.Add contractScroll |> ignore
    contractTab.Content <- contractPanel

    let theaterTab = TabItem(Header = "Theater")
    let theaterPanel = DockPanel()
    let theaterTop = WrapPanel(Margin = Thickness(0.0, 0.0, 0.0, 4.0))
    validateBtn.Margin <- Thickness(0.0, 0.0, 4.0, 2.0)
    theaterTop.Children.Add validateBtn |> ignore
    stageBtn.Margin <- Thickness(0.0, 0.0, 4.0, 2.0)
    theaterTop.Children.Add stageBtn |> ignore
    queuePromptsBtn.Margin <- Thickness(0.0, 0.0, 4.0, 2.0)
    theaterTop.Children.Add queuePromptsBtn |> ignore
    validateQueueBtn.Margin <- Thickness(0.0, 0.0, 4.0, 2.0)
    theaterTop.Children.Add validateQueueBtn |> ignore
    DockPanel.SetDock(theaterTop, Dock.Top)
    theaterPanel.Children.Add theaterTop |> ignore
    pasteBox.AcceptsReturn <- true
    pasteBox.TextWrapping <- TextWrapping.Wrap
    pasteBox.VerticalScrollBarVisibility <- ScrollBarVisibility.Auto
    pasteBox.MinHeight <- 90.0
    pasteBox.FontFamily <- mono
    pasteBox.FontSize <- 11.0
    pasteBox.Background <- panel
    pasteBox.Foreground <- normal
    let pasteSection = StackPanel(Margin = Thickness(0.0, 4.0, 0.0, 0.0))
    let pasteLabel = TextBlock(Text = "paste the model's F# reply here, then Stage to Lifted/:", Foreground = dim, FontSize = 11.0)
    pasteSection.Children.Add pasteLabel |> ignore
    pasteSection.Children.Add pasteBox |> ignore
    DockPanel.SetDock(pasteSection, Dock.Bottom)
    theaterPanel.Children.Add pasteSection |> ignore
    theaterText.Foreground <- normal
    theaterText.FontFamily <- mono
    theaterText.FontSize <- 11.0
    theaterText.TextWrapping <- TextWrapping.Wrap
    let theaterScroll = ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
    theaterScroll.Content <- theaterText
    theaterPanel.Children.Add theaterScroll |> ignore
    theaterTab.Content <- theaterPanel

    right.Items.Add heatTab |> ignore
    right.Items.Add funcTab |> ignore
    right.Items.Add graphTab |> ignore
    right.Items.Add contractTab |> ignore
    right.Items.Add theaterTab |> ignore

    // toolbar
    let toolbar = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(8.0))
    let runBtn = Button(Content = "Run")
    let pauseBtn = Button(Content = "Pause")
    let stepFrameBtn = Button(Content = "Step frame")
    let saveBtn = Button(Content = "Save trace")
    let loadBtn = Button(Content = "Load trace")
    runBtn.Click.Add(fun _ -> pauseGame(); running <- true; frameTimer.Start())
    pauseBtn.Click.Add(fun _ -> pauseGame())
    stepFrameBtn.Click.Add(fun _ -> pauseGame(); renderFrame ())
    saveBtn.Click.Add(fun _ -> saveTrace ())
    loadBtn.Click.Add(fun _ -> loadTrace ())
    recordToggle.Checked.Add(fun _ -> match session with Some s -> s.Recorder.RecordEnabled <- true | None -> ())
    recordToggle.Unchecked.Add(fun _ -> match session with Some s -> s.Recorder.RecordEnabled <- false | None -> ())
    recordToggle.ToolTip <- "record every executed instruction into the ring buffer"
    recordToggle.Foreground <- normal
    recordToggle.VerticalAlignment <- VerticalAlignment.Center

    let soundToggle = CheckBox(Content = "Mute", IsChecked = Nullable<bool>(true))
    soundToggle.ToolTip <- "beeper audio is muted by default; uncheck for 10% volume"
    soundToggle.Foreground <- normal
    soundToggle.VerticalAlignment <- VerticalAlignment.Center
    soundToggle.Checked.Add(fun _ -> Audio.SetEnabled false)
    soundToggle.Unchecked.Add(fun _ -> Audio.SetEnabled true)

    mineBtn.Click.Add(fun _ -> mineNow ())
    clearTrayBtn.Click.Add(fun _ ->
      selectedEntries <- Set.empty
      refreshRoutines ()
      refreshTray ()
      drawGraph ())
    savePromptBtn.Click.Add(fun _ ->
      match activeRoutine with
      | Some r ->
        let dlg = Microsoft.Win32.SaveFileDialog(Filter = "Markdown (*.md)|*.md", FileName = sprintf "fn%04X.md" r.Entry)
        if dlg.ShowDialog() = Nullable<bool>(true) then
          File.WriteAllText(dlg.FileName, contractText.Text)
          statusText.Text <- "prompt saved to " + dlg.FileName
      | None -> statusText.Text <- "select a routine first")
    copyPromptBtn.Click.Add(fun _ ->
      if contractText.Text.Length > 0 then
        System.Windows.Clipboard.SetText contractText.Text
        statusText.Text <- "prompt copied to clipboard")

    stageBtn.Click.Add(fun _ ->
      match activeRoutine with
      | Some r ->
        if pasteBox.Text.Trim().Length > 0 then
          let dir = Path.Combine(Directory.GetCurrentDirectory(), "Lifted")
          Directory.CreateDirectory dir |> ignore
          let path = Path.Combine(dir, sprintf "fn%04X.fs" r.Entry)
          let header =
            sprintf "// Staged lift for 0x%04X (span 0x%04X-0x%04X)\n// Add this file to Jetpac3.Core in Visual Studio, register the routine in\n// LiftedRoutines.registry, rebuild, then validate from the Theater tab.\n\n"
              r.Entry r.SpanLo r.SpanHi
          File.WriteAllText(path, header + pasteBox.Text)
          statusText.Text <- "staged to " + path
        else statusText.Text <- "paste the model's F# first"
      | None -> statusText.Text <- "select a routine first")

    validateBtn.Click.Add(fun _ ->
      match activeRoutine, session with
      | Some r, Some _ ->
        let registered =
          match Jetpac3.Core.LiftedRoutines.registryHook r.Entry with
          | Some _ -> true
          | None -> false
        if not registered then
          theaterText.Text <-
            sprintf
              "0x%04X is not covered by the lifted registry yet.\n\nStage the pasted F# (below) to Lifted/, add it to Jetpac3.Core in Visual Studio, rebuild, then validate again."
              r.Entry
        else
          let framesN = 300
          let script = Jetpac3.Core.Script.defaultSession framesN
          let rom = LocalAssets.find "48.rom"
          let tzx = LocalAssets.find "Jetpac.tzx"
          theaterText.Text <- sprintf "validating 0x%04X over %d frames..." r.Entry framesN
          let task =
            System.Threading.Tasks.Task.Run(fun () ->
              Validation.run rom tzx framesN script
                Jetpac3.Core.LiftedRoutines.registryHook
                System.Threading.CancellationToken.None)
          task.ContinueWith(fun (t: System.Threading.Tasks.Task<Validation.Report>) ->
            self.Dispatcher.Invoke(System.Action(fun () ->
              if t.IsFaulted then
                theaterText.Text <- "validation crashed: " + t.Exception.Message
              else
                let rep = t.Result
                if rep.Passed then
                  theaterText.Text <-
                    sprintf "VALIDATION PASSED: %d frames, 0 diffs (lifted 0x%04X == oracle)" rep.Frames r.Entry
                else
                  match rep.FirstDivergence with
                  | Some d ->
                    theaterText.Text <-
                      sprintf "VALIDATION FAILED at frame %d (%s)\n\nport   executed %04X: %s\noracle executed %04X: %s\n\nport   regs: %s\noracle regs: %s"
                        d.Frame d.Kind d.PortPc d.PortExecuted d.OraclePc d.OracleExecuted d.PortRegs d.OracleRegs
                  | None -> theaterText.Text <- "VALIDATION FAILED: no divergence details")))
          |> ignore
      | None, _ -> theaterText.Text <- "select a mined routine to validate"
      | _ -> theaterText.Text <- "emulator not ready")

    queuePromptsBtn.Click.Add(fun _ ->
      if Set.isEmpty selectedEntries then
        theaterText.Text <- "fill the lift queue first (click routines in the Functions tab)"
      else
        match session, currentTrace () with
        | Some s, Some t when t.Entries.Length > 0 ->
          let dir = Path.Combine(Directory.GetCurrentDirectory(), "prompts")
          Directory.CreateDirectory dir |> ignore
          let pcCycles = getPcCycles ()
          let saved = ResizeArray<string>()
          for entry in selectedEntries |> Set.toList do
            match minedRoutines |> List.tryFind (fun r -> r.Entry = entry) with
            | Some r ->
              let contract = Contract.extract s.Memory t r pcCycles
              let prompt = Prompt.generate contract
              let path = Path.Combine(dir, sprintf "fn%04X.md" entry)
              File.WriteAllText(path, prompt)
              saved.Add(sprintf "%04X" entry)
            | None -> ()
          statusText.Text <- sprintf "saved %d prompts to %s" saved.Count dir
          theaterText.Text <-
            sprintf "saved %d prompts to %s:\n%s"
              saved.Count dir (String.concat ", " saved)
        | _ -> theaterText.Text <- "no trace to extract from")

    validateQueueBtn.Click.Add(fun _ ->
      if Set.isEmpty selectedEntries then
        theaterText.Text <- "fill the lift queue first (click routines in the Functions tab)"
      else
        let queued = selectedEntries |> Set.toList
        let covered =
          queued
          |> List.filter (fun e ->
            match Jetpac3.Core.LiftedRoutines.registryHook e with
            | Some _ -> true
            | None -> false)
        if List.isEmpty covered then
          theaterText.Text <-
            "none of the queued routines are in the lifted registry yet: "
            + (queued |> List.map (sprintf "%04X") |> String.concat ", ")
            + "\n\nLift them first (prompt -> stage -> wire into Jetpac3.Core -> rebuild)."
        else
          let framesN = 300
          let script = Jetpac3.Core.Script.defaultSession framesN
          let rom = LocalAssets.find "48.rom"
          let tzx = LocalAssets.find "Jetpac.tzx"
          theaterText.Text <-
            sprintf "validating queue (%d of %d queued covered by the registry) over %d frames..."
              covered.Length queued.Length framesN
          let task =
            System.Threading.Tasks.Task.Run(fun () ->
              Validation.run rom tzx framesN script
                Jetpac3.Core.LiftedRoutines.registryHook
                System.Threading.CancellationToken.None)
          task.ContinueWith(fun (t: System.Threading.Tasks.Task<Validation.Report>) ->
            self.Dispatcher.Invoke(System.Action(fun () ->
              if t.IsFaulted then
                theaterText.Text <- "queue validation crashed: " + t.Exception.Message
              else
                let rep = t.Result
                let sb = System.Text.StringBuilder()
                sb.AppendLine(sprintf "QUEUE VALIDATION: %d frames" rep.Frames) |> ignore
                for e in queued do
                  match minedRoutines |> List.tryFind (fun r -> r.Entry = e) with
                  | None -> sb.AppendLine(sprintf "%04X  skipped (not mined)" e) |> ignore
                  | Some r ->
                    let inRegistry =
                      match Jetpac3.Core.LiftedRoutines.registryHook e with
                      | Some _ -> true
                      | None -> false
                    if not inRegistry then
                      sb.AppendLine(sprintf "%04X  NOT LIFTED (not in registry)" e) |> ignore
                    elif rep.Passed then
                      sb.AppendLine(sprintf "%04X  PASSED" e) |> ignore
                    else
                      match rep.FirstDivergence with
                      | Some d when d.Address >= r.SpanLo && d.Address <= r.SpanHi ->
                        sb.AppendLine(sprintf "%04X  FAILED: divergence inside its span at frame %d (%s)" e d.Frame d.Kind) |> ignore
                      | Some d ->
                        sb.AppendLine(sprintf "%04X  PASSED (divergence is outside its span: %04X %s)" e d.Address d.Kind) |> ignore
                      | None -> sb.AppendLine(sprintf "%04X  FAILED (no divergence details)" e) |> ignore
                theaterText.Text <- sb.ToString())))
          |> ignore)

    let sep = Border(Width = 1.0, Height = 24.0, Background = dim, Margin = Thickness(8.0, 0.0, 8.0, 0.0))

    let b100 = Button(Content = "<<100")
    let b10 = Button(Content = "<<10")
    let b1 = Button(Content = "<<1")
    let f1 = Button(Content = "1>>")
    let f10 = Button(Content = "10>>")
    let f100 = Button(Content = "100>>")
    b100.Click.Add(fun _ -> stepBack100 ())
    b10.Click.Add(fun _ -> stepBack10 ())
    b1.Click.Add(fun _ -> stepBack1 ())
    f1.Click.Add(fun _ -> stepFwd1 ())
    f10.Click.Add(fun _ -> stepFwd10 ())
    f100.Click.Add(fun _ -> stepFwd100 ())
    playBtn.Click.Add(fun _ -> toggleCinema ())

    let speedBox = ComboBox(Width = 64.0)
    for s in [ 1; 10; 100; 1000 ] do
      speedBox.Items.Add(string s) |> ignore
    speedBox.SelectedIndex <- 1
    speedBox.SelectionChanged.Add(fun _ ->
      match speedBox.SelectedItem with
      | :? string as s -> cinemaSpeed <- Int32.Parse s
      | _ -> ())

    for c in [ runBtn :> Control; pauseBtn :> Control; stepFrameBtn :> Control; recordToggle :> Control; soundToggle :> Control; saveBtn :> Control; loadBtn :> Control ] do
      c.Margin <- Thickness(4.0, 0.0, 4.0, 0.0)
      toolbar.Children.Add c |> ignore
    toolbar.Children.Add sep |> ignore
    for c in [ b100 :> Control; b10 :> Control; b1 :> Control; playBtn :> Control; f1 :> Control; f10 :> Control; f100 :> Control ] do
      c.Margin <- Thickness(2.0, 0.0, 2.0, 0.0)
      toolbar.Children.Add c |> ignore
    toolbar.Children.Add(TextBlock(Text = "speed", Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(6.0, 0.0, 2.0, 0.0))) |> ignore
    speedBox.VerticalAlignment <- VerticalAlignment.Center
    toolbar.Children.Add speedBox |> ignore

    // status bar
    statusText.Foreground <- normal
    statusText.Margin <- Thickness(8.0)
    statusText.TextWrapping <- TextWrapping.Wrap

    // lift-queue tray
    let trayRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(8.0, 0.0, 8.0, 4.0))
    let trayTitle = TextBlock(Text = "Lift queue:", Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(0.0, 0.0, 4.0, 0.0))
    trayRow.Children.Add trayTitle |> ignore
    trayWrap.VerticalAlignment <- VerticalAlignment.Center
    trayRow.Children.Add trayWrap |> ignore
    trayCountLabel.Foreground <- dim
    trayCountLabel.VerticalAlignment <- VerticalAlignment.Center
    trayCountLabel.Margin <- Thickness(6.0, 0.0, 6.0, 0.0)
    trayRow.Children.Add trayCountLabel |> ignore
    clearTrayBtn.VerticalAlignment <- VerticalAlignment.Center
    trayRow.Children.Add clearTrayBtn |> ignore

    let root = Grid()
    root.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
    root.RowDefinitions.Add(RowDefinition(Height = GridLength(1.0, GridUnitType.Star)))
    root.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
    root.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
    Grid.SetRow(toolbar, 0)
    Grid.SetRow(trayRow, 2)
    Grid.SetRow(statusText, 3)
    root.Children.Add toolbar |> ignore
    root.Children.Add trayRow |> ignore
    root.Children.Add statusText |> ignore

    let main = Grid()
    main.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
    main.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
    main.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
    Grid.SetColumn(left, 0)
    Grid.SetColumn(center, 1)
    Grid.SetColumn(right, 2)
    main.Children.Add left |> ignore
    main.Children.Add center |> ignore
    main.Children.Add right |> ignore
    Grid.SetRow(main, 1)
    root.Children.Add main |> ignore
    self.Content <- root

    // interactions
    slider.ValueChanged.Add(fun _ ->
      if not syncingSlider && currentEntryCount () > 0 then
        cursor <- int slider.Value
        refreshDisasm ()
        refreshRegs ()
        refreshHeatmap ()
        refreshStrip ()
        refreshCursorLabel ())
    slider.PreviewMouseDown.Add(fun _ -> userDragging <- true)
    slider.PreviewMouseUp.Add(fun _ -> userDragging <- false)

    heatImage.MouseLeftButtonDown.Add(fun e ->
      match currentTrace () with
      | Some t when t.Entries.Length > 0 ->
        let pos = e.GetPosition heatImage
        let x = int pos.X
        let y = int pos.Y
        if x >= 0 && x < 256 && y >= 0 && y < 256 then
          let pc = (y <<< 8) ||| x
          let idx = if pc < t.FirstIndexAtPc.Length then t.FirstIndexAtPc[pc] else -1
          if idx >= 0 then
            cursor <- idx
            refreshAll ()
            syncSlider ()
            statusText.Text <- sprintf "jumped to 0x%04X (first of %d executions)" pc (if pc < t.PerPcCount.Length then t.PerPcCount[pc] else 0)
      | _ -> ())
    heatImage.MouseMove.Add(fun e ->
      let pos = e.GetPosition heatImage
      let x = int pos.X
      let y = int pos.Y
      if x >= 0 && x < 256 && y >= 0 && y < 256 then
        let pc = (y <<< 8) ||| x
        let count =
          match currentTrace () with
          | Some t when pc < t.PerPcCount.Length -> t.PerPcCount[pc]
          | _ -> 0
        heatImage.ToolTip <- sprintf "0x%04X  (%d executions)" pc count)

    self.KeyDown.Add(fun e ->
      match session with
      | Some s -> List.iter (fun (r, b) -> s.SetKey(r, b, true)) (keyMap e.Key)
      | None -> ())
    self.KeyUp.Add(fun e ->
      match session with
      | Some s -> List.iter (fun (r, b) -> s.SetKey(r, b, false)) (keyMap e.Key)
      | None -> ())

    // timers
    frameTimer.Tick.Add(fun _ -> if running then renderFrame ())
    cinemaTimer.Tick.Add(fun _ ->
      if cinemaPlaying then
        let n = currentEntryCount ()
        if n > 0 then
          cursor <- min (n - 1) (cursor + cinemaSpeed)
          refreshDisasm ()
          refreshRegs ()
          refreshHeatmap ()
          refreshStrip ()
          refreshCursorLabel ()
          syncingSlider <- true
          slider.Value <- float cursor
          syncingSlider <- false
        else cinemaPlaying <- false)
    uiTimer.Tick.Add(fun _ ->
      match session with
      | None ->
        if bootTask.IsCompleted then
          try
            session <- Some bootTask.Result
            startGame ()
          with ex ->
            statusText.Text <- "boot failed: " + ex.Message
      | Some s ->
        if running then
          statusText.Text <-
            sprintf "frame=%d  tick=%.2fM  instr=%d/%d  distinct-pc=%d  selfmod=%d  rec=%s"
              s.Frame
              (float s.CycleCount / 1_000_000.0)
              s.Recorder.EntryCount
              s.Recorder.Capacity
              (s.Recorder.PerPcCount |> Array.filter (fun c -> c > 0) |> Array.length)
              s.Recorder.SelfModCount
              (if s.Recorder.RecordEnabled then "ON" else "OFF")
        if not userDragging then
          refreshHeatmap ()
          refreshStrip ()
          if running then
            syncingSlider <- true
            let n = s.Recorder.EntryCount
            if n > 1 then
              slider.Maximum <- float (n - 1)
              slider.IsEnabled <- true
            syncingSlider <- false)
    uiTimer.Start()

    self.Closed.Add(fun _ ->
      running <- false
      frameTimer.Stop()
      cinemaTimer.Stop()
      uiTimer.Stop()
      Audio.Stop ())

    statusText.Text <- "booting emulator to game entry..."
    presentGame ()
    refreshAll ()
    syncSlider ()

module Program =
  [<EntryPoint>]
  [<STAThread>]
  let main _ =
    let app = Application()
    app.Run(MainWindow())
