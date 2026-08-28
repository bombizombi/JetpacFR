namespace JetpacFR.Desktop

open System
open System.IO
open System.Collections.Generic
open JetpacFR.Core
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Data
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Shapes
open System.Windows.Threading
open NAudio.Wave

// Disambiguate ControlFile comment kinds from WPF shapes (WPF's Line
// class would otherwise swallow these identifiers).
module CmtKinds =
  let Line = CommentKind.Line
  let Name = CommentKind.Name
  let Range = CommentKind.Range
  let Exec = CommentKind.Exec
open CmtKinds

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

/// One row of the disassembly lists (execution + memory modes).
type DisasmRow =
  { Tag: string
    Brush: SolidColorBrush
    IsCurrent: bool
    /// Row address (PC for execution rows, address for memory rows); -1
    /// for synthetic rows. Drives comment lookups and jumps.
    Addr: int
    /// Instruction index in the trace (-1 in memory mode).
    InstrIdx: int }

/// One row of the mined-routine list.
type RoutineRow =
  { Tag: string
    Brush: SolidColorBrush
    IsSelected: bool
    Entry: int }

type MainWindow() as self =
  inherit Window()

  let mutable session: TraceSession option = None
  let mutable bootTask: System.Threading.Tasks.Task<TraceSession> option = None
  let mutable bootGameId = ""
  let mutable manualBoot: ManualBoot option = None
  let mutable ceGame: CEGame option = None
  let mutable engineCE = false
  let mutable currentGame: GameManifest option = None
  let mutable gamesList: GameManifest list = []
  let mutable replaySavedRevision = -1
  let manualTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 20.0)

  /// Walk up from the app base directory to the first folder containing a
  /// games/ directory (the manifests live at the repository root).
  let findGamesDir () =
    let rec walk (d: System.IO.DirectoryInfo) =
      let candidate = System.IO.Path.Combine(d.FullName, "games")
      if System.IO.Directory.Exists candidate then candidate
      else
        match d.Parent with
        | null -> ""
        | parent -> walk parent
    walk (System.IO.DirectoryInfo AppContext.BaseDirectory)

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
  let sky = Color.FromRgb(0x38uy, 0xBDuy, 0xF8uy)
  let amber = Color.FromRgb(0xFBuy, 0xBFuy, 0x24uy)
  let darkFg = SolidColorBrush(Color.FromRgb(0x02uy, 0x06uy, 0x17uy))
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
  let saveReplay (force: bool) =
    match currentGame, session with
    | Some game, Some s when force || s.KeyLog.Revision <> replaySavedRevision ->
      match ReplayStore.save game s.KeyLog.Events with
      | Ok () -> replaySavedRevision <- s.KeyLog.Revision
      | Error error -> statusText.Text <- sprintf "replay save failed for %s: %s" game.Name error
    | _ -> ()

  let loadReplay (game: GameManifest) (s: TraceSession) : bool =
    match ReplayStore.tryLoad game with
    | ReplayLoaded events ->
      s.KeyLog.Replace events
      replaySavedRevision <- s.KeyLog.Revision
      if not (List.isEmpty events) then
        statusText.Text <- sprintf "%s: loaded %d saved key events" game.Name events.Length
      not (List.isEmpty events)
    | ReplayMissing ->
      replaySavedRevision <- s.KeyLog.Revision
      false
    | ReplayIgnored reason ->
      replaySavedRevision <- s.KeyLog.Revision
      statusText.Text <- sprintf "%s: saved replay ignored (%s)" game.Name reason
      false


  /// Start a game from its manifest: warm cache -> fast port session; cold
  /// auto -> oracle boot (slow, first run); cold manual -> the user runs the
  /// loader in the oracle and marks the entry point.
  let launchGame (m: GameManifest) =
    saveReplay true
    manualBoot <- None
    manualTimer.Stop()
    currentGame <- Some m
    bootGameId <- m.GameId
    replaySavedRevision <- -1
    match EntryCache.tryLoad m.Rom m.Tzx with
    | Some _ ->
      bootTask <- Some(System.Threading.Tasks.Task.Run(fun () ->
        TraceSession(m.Rom, m.Tzx, 4_000_000)))
      statusText.Text <- sprintf "booting %s (cached entry state)..." m.Name
    | None when m.Boot = "auto" ->
      bootTask <- Some(System.Threading.Tasks.Task.Run(fun () ->
        TraceSession(m.Rom, m.Tzx, 4_000_000)))
      statusText.Text <- sprintf "booting %s to game entry (first run, slow)..." m.Name
    | None when m.Boot = "program" ->
      // A raw Z80 image: seed the entry cache from the bin, then boot the
      // port session warm (the cache is keyed on the image file).
      match m.ProgramBin, m.ProgramAddress with
      | Some bin, Some addr ->
        try
          ProgramEntry.seed bin addr
          bootTask <- Some(System.Threading.Tasks.Task.Run(fun () ->
            TraceSession(bin, bin, 4_000_000)))
          statusText.Text <- sprintf "booting %s (program image at 0x%04X)..." m.Name addr
        with ex ->
          statusText.Text <- sprintf "cannot load %s's program image: %s" m.Name ex.Message
      | _ -> statusText.Text <- sprintf "%s: manifest is missing program.bin/address" m.Name
    | None ->
      manualBoot <- Some(ManualBoot(m.Rom, m.Tzx))
      manualTimer.Start()
      statusText.Text <-
        sprintf "%s: run the game loader here; press 'Set as game entry' when it is ready" m.Name
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
  let rewindSlider = Slider(Minimum = 0.0, Maximum = 0.0, Value = 0.0, Width = 260.0)
  let gameCombo = ComboBox(Width = 130.0, VerticalAlignment = VerticalAlignment.Center)
  let engineCombo = ComboBox(Width = 92.0, VerticalAlignment = VerticalAlignment.Center)
  let parityLabel = TextBlock(Text = "", VerticalAlignment = VerticalAlignment.Center, Foreground = normal)
  let setEntryBtn = Button(Content = "Set as game entry")
  let exportScriptBtn = Button(Content = "Export script")
  let timelineBtn = Button(Content = "Timeline")
  let timeLabel = TextBlock(Text = "00:00", VerticalAlignment = VerticalAlignment.Center)
  let goBtn = Button(Content = "Go")
  let replayBtn = Button(Content = "Replay")
  let mutable rewinding = false // slider drag in progress
  let mutable replaying = false // scripted replay driving the frame timer
  /// Timeline extent frozen when Replay starts: the replay regrows history
  /// in lockstep with the replaying frame, so LastFrame alone would pin the
  /// thumb to the right edge instead of showing replay progress.
  let mutable replayExtent = 0

  // ---- control-file GUI state --------------------------------------------
  let mutable control: ControlFile option = None
  let mutable controlGameDir = ""
  /// Disassembly window mode: linear memory sweep vs execution trace.
  let mutable disasmModeMemory = false
  let mutable memCursor = 0x8000
  /// Memory scan state: baseline snapshot + surviving addresses.
  let mutable scanBaseline: byte[] option = None
  let mutable scanBaselineFrame = -1
  let mutable scanResults: int list = []
  let mutable lastPreviewFrame = -1

  // ---- control-file GUI widgets -------------------------------------------
  let timeline = TimelineSelector(Height = 56.0, UnitName = "f")
  let controlMap = ControlMap(Width = 56.0)
  let brushABtn = Button(Content = "Brush A", Width = 64.0)
  let brushBBtn = Button(Content = "Brush B", Width = 64.0)
  let previewABtn = Button(Content = "Preview A", Width = 72.0)
  let previewBBtn = Button(Content = "Preview B", Width = 72.0)

  // 1-second selection movie: 50 ticks x 20 ms
  let previewTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 20.0)
  let mutable previewFrom = 0
  let mutable previewSpan = 0
  let mutable previewTicks = 0
  let saveCtrlBtn = Button(Content = "Save ctrl", Width = 72.0)
  let importCtrlBtn = Button(Content = "Import ctrl...", Width = 96.0)
  let newCtrlBtn = Button(Content = "New ctrl", Width = 64.0)
  let exportCtrlBtn = Button(Content = "Export skool...", Width = 100.0)
  let memModeBtn = ToggleButton(Content = "Memory", Width = 68.0)
  let execModeBtn = ToggleButton(Content = "Execution", Width = 76.0, IsChecked = Nullable<bool>(true))
  /// Shared comment text for all mass-comment buttons.
  let commentBox = TextBox(Width = 420.0, Height = 22.0)
  let idxCmtBtn = Button(Content = "idx -> cmt", ToolTip = "Comment the current instruction index (execution trace)")
  let nameBeforeBtn = Button(Content = "Name region before cursor", ToolTip = "Name the address region ending at the current instruction")
  let nameABtn = Button(Content = "Name A", ToolTip = "Name the code region executed in selector range A")
  let nameBBtn = Button(Content = "Name B", ToolTip = "Name the code region executed in selector range B")
  let cmtABtn = Button(Content = "Comment A", ToolTip = "Add the comment text to all addresses executed in A")
  let cmtBBtn = Button(Content = "Comment B", ToolTip = "Add the comment text to all addresses executed in B")
  let cmtBNotA = Button(Content = "Comment B-only", ToolTip = "Comment addresses executed in B but NOT in A")
  let cmtANotB = Button(Content = "Comment A-only", ToolTip = "Comment addresses executed in A but NOT in B")
  // engine radio group
  let oracleRadio = RadioButton(Content = "Oracle", GroupName = "engine", IsChecked = Nullable<bool>(true), Foreground = normal)
  let ceRadio = RadioButton(Content = "game.fs (CE)", GroupName = "engine", Foreground = normal)
  let diffRadio = RadioButton(Content = "Both + compare", GroupName = "engine", Foreground = normal)
  // scan tab
  let scanStartBtn = Button(Content = "Start memory scan")
  let scanChangedBtn = Button(Content = "= changed") 
  let scanSameBtn = Button(Content = "= unchanged")
  let scanExactBox = TextBox(Width = 70.0)
  let scanExactBtn = Button(Content = "= value")
  let scanSmallerBtn = Button(Content = "< smaller")
  let scanLargerBtn = Button(Content = "> larger")
  let scanWrittenBtn = Button(Content = "written")
  let scanExecutedBtn = Button(Content = "executed")
  let scanList = ListBox(Height = 320.0)
  let scanCountLabel = TextBlock(Foreground = dim, FontSize = 11.0)
  let scanWidth8 = RadioButton(Content = "byte 8", GroupName = "scanwidth", IsChecked = Nullable<bool>(true), Foreground = dim)
  let scanWidth16 = RadioButton(Content = "word 16", GroupName = "scanwidth", Foreground = dim)
  let scanRomCheck = CheckBox(Content = "include ROM", IsChecked = Nullable<bool>(false), Foreground = dim)
  let scanScreenCheck = CheckBox(Content = "include screen", IsChecked = Nullable<bool>(true), Foreground = dim)
  let scanCodeOnly = ComboBox(Width = 110.0)

  let mutable timelineWin: TimelineDemoWindow option = None
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

  // ---- control-file GUI logic ---------------------------------------------
  /// Current timeline extent in frames (replay extent when replaying).
  let timelineExtent () : int64 =
    match session with
    | Some s -> max 1L (int64 (max s.ReplayEndFrame s.History.LastFrame))
    | None ->
      match currentTrace () with
      | Some t when t.FrameTicks.Length > 0 -> int64 t.FrameTicks.Length
      | _ -> max 1L (int64 replayExtent)

  /// Entry index of the first instruction at-or-after frame f.
  let entryAtFrame (t: Trace) (f: int) : int =
    if f <= 0 || t.FrameTicks.Length = 0 then 0
    else
      let tick = t.FrameTicks[min (t.FrameTicks.Length - 1) f]
      // Entries are time-ordered; binary search for Tick >= tick.
      let rec search lo hi =
        if lo >= hi then lo
        else
          let mid = (lo + hi) / 2
          if t.Entries[mid].Tick < tick then search (mid + 1) hi else search lo mid
      min (t.Entries.Length - 1) (search 0 t.Entries.Length)

  /// The game's control dir (games/<id>), empty when unknown.
  let controlDir () : string =
    match currentGame with
    | Some g -> g.GameDirectory
    | None -> ""

  let loadControlForGame () : unit =
    let dir = controlDir ()
    if dir <> "" then
      try
        let span = int (timelineExtent ()) * 50 + 50, int (timelineExtent ()) * 50 + 100
        control <- Some(ControlFile.loadOrCreate dir (fst span, snd span))
        controlGameDir <- dir
        statusText.Text <- sprintf "control file loaded (%d comments)" control.Value.Comments.Length
      with ex -> statusText.Text <- sprintf "control load failed: %s" ex.Message

  let saveControlNow () : unit =
    match control, controlGameDir with
    | Some c, dir when dir <> "" && c.Dirty ->
      try
        control <- Some(ControlFile.save dir c)
        statusText.Text <- sprintf "control saved: %s" (Path.Combine(dir, "control.json"))
      with ex -> statusText.Text <- sprintf "control save failed: %s" ex.Message
    | Some c, _ -> statusText.Text <- sprintf "control unchanged (%d comments)" c.Comments.Length
    | None, _ -> statusText.Text <- "no control file for this game yet - use New ctrl"

  let mutable previewRangeImpl: (BrushId -> unit) = fun _ -> ()
  let previewRange which = previewRangeImpl which

  /// Timeline range A/B as frame pair.
  let rangeOf (which: BrushId) : int * int =
    let r = if which = A then timeline.RangeA else timeline.RangeB
    int r.Start, int r.End

  // ---- view refresh ------------------------------------------------------
  let presentGame () =
    match ceGame with
    | Some c ->
      gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), c.ScreenBuffer, 320 * 4, 0)
    | None ->
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
        rows.Add { Tag = tag; Brush = (if isErr then red else normal); IsCurrent = isErr; Addr = addr; InstrIdx = -1 }
        addr <- (addr + insn.Length) &&& 0xFFFF
  /// MM:SS of accumulated play time at 50 fps (frames / 50).
  let updateTimeLabel (frames: int) =
    let totalSeconds = frames / 50
    timeLabel.Text <- sprintf "%02d:%02d" (totalSeconds / 60) (totalSeconds % 60)

  let renderFrame () =
    match ceGame with
    | Some c ->
      try
        let frameStart, _ = c.RunFrame()
        presentGame ()
        Audio.Play(c.DrainBeeperSamples(frameStart))
      with ex ->
        running <- false
        frameTimer.Stop()
        statusText.Text <- sprintf "CE engine error: %s" ex.Message
    | None ->
      match session with
      | Some s ->
        try
          let frameStart, _ = s.RunFrame()
          presentGame ()
          Audio.Play(s.DrainBeeperSamples(frameStart))
          if s.ReplayFinished then
            // Replay reached the recording's end: stop and hand to live Run.
            replaying <- false
            running <- false
            frameTimer.Stop()
            statusText.Text <- "replay finished - press Run to take over"
        with ex ->
          running <- false
          replaying <- false
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
    let comment =
      match control with
      | Some c -> ControlFile.commentAt c (int e.Pc) |> Option.defaultValue ""
      | None -> ""
    if e.Length = 0uy then
      { Tag = sprintf "%07d  ----  INT -> %04X   (%d tstates)" idx e.Target e.Cycles
        Brush = red
        IsCurrent = isCurrent
        Addr = -1
        InstrIdx = idx }
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
      let cm = if comment <> "" then sprintf "  ; %s" comment else ""
      { Tag = sprintf "%07d  %04X  %-11s  %s%s%s%s%s" idx e.Pc hex insn.Text tail sm lifted cm
        Brush = brush
        IsCurrent = isCurrent
        Addr = int e.Pc
        InstrIdx = idx }

  // ---- memory scan ---------------------------------------------------------
  let currentMemory () : byte[] option =
    match session with
    | Some s -> Some s.Memory
    | None ->
      match currentTrace () with
      // A loaded trace has no memory image; scans need a live session.
      | _ -> None

  let codeSetFromControl () : Set<int> =
    match control with
    | Some c ->
      c.Blocks
      |> List.filter (fun b -> b.Kind = Code)
      |> List.collect (fun b -> [ b.Start .. b.EndExcl - 1 ])
      |> Set.ofList
    | None -> Set.empty

  /// The address universe per scan options.
  let scanUniverse () : int[] =
    match currentMemory () with
    | None -> [||]
    | Some mem ->
      let lo = if scanRomCheck.IsChecked.GetValueOrDefault() then 0 else 0x4000
      let screenHi = 0x5B00
      seq {
        for a in lo .. (mem.Length - 1) do
          let inScreen = a >= 0x4000 && a < screenHi
          if inScreen && not (scanScreenCheck.IsChecked.GetValueOrDefault()) then ()
          else yield a
      }
      |> Seq.toArray

  let refreshScanList () : unit =
    match currentMemory () with
    | None -> scanCountLabel.Text <- "no live session - cannot scan"
    | Some mem ->
      let rows =
        scanResults
        |> List.truncate 2000
        |> List.map (fun a ->
          let prev = match scanBaseline with Some b when a < b.Length -> b[a] | _ -> 0uy
          { Tag = sprintf "%04X  %02X%s" a mem[a] (if prev <> mem[a] then sprintf "  (was %02X)" prev else "")
            Brush = normal
            IsCurrent = false
            Addr = a
            InstrIdx = -1 })
      scanList.ItemsSource <- rows
      scanCountLabel.Text <-
        sprintf "%d addresses%s" scanResults.Length (if scanResults.Length > 2000 then " (showing first 2000)" else "")

  let startScan () : unit =
    match currentMemory () with
    | None -> statusText.Text <- "start a game first - scans need live memory"
    | Some mem ->
      scanBaseline <- Some (Array.copy mem)
      scanBaselineFrame <-
        match session with Some s -> s.Frame | None -> -1
      scanResults <- scanUniverse () |> Array.toList
      for b in [ scanChangedBtn; scanSameBtn; scanExactBtn; scanSmallerBtn; scanLargerBtn; scanWrittenBtn; scanExecutedBtn ] do
        b.IsEnabled <- true
      refreshScanList ()
      statusText.Text <- sprintf "scan started: %d candidate addresses" scanResults.Length

  /// Apply a byte filter (addr, oldValue, curValue -> keep). In word mode
  /// the 16-bit values at addr are compared and the filter sees the low
  /// bytes; changed/unchanged still work through value inequality.
  let applyScanFilter (keep: int -> int -> int -> bool) : unit =
    match currentMemory (), scanBaseline with
    | Some mem, Some baselineArr ->
      let word = scanWidth16.IsChecked.GetValueOrDefault()
      let codeOnly, nonCodeOnly =
        match scanCodeOnly.SelectedItem with
        | :? string as s when s.StartsWith("code") -> true, false
        | :? string as s when s.StartsWith("non-code") -> false, true
        | _ -> false, false
      let codes = codeSetFromControl ()
      let wordAt (arr: byte[]) (a: int) =
        (int arr[(a + 1) &&& 0xFFFF] <<< 8) ||| int arr[a]
      let passes a =
        if codeOnly && not (codes.Contains a) then false
        elif nonCodeOnly && codes.Contains a then false
        elif word then keep a (wordAt baselineArr a) (wordAt mem a)
        else keep a (int baselineArr[a]) (int mem[a])
      scanResults <- scanResults |> List.filter passes
      refreshScanList ()
    | _ -> statusText.Text <- "run Start memory scan first"

  let writtenSinceBaseline () : Set<int> =
    match currentTrace () with
    | Some t when scanBaselineFrame >= 0 ->
      // Approximation: writes recorded in the current trace window above
      // the frame boundary tick of the baseline frame.
      let tick =
        if t.FrameTicks.Length > scanBaselineFrame then t.FrameTicks[scanBaselineFrame]
        else 0u
      t.Writes |> Array.filter (fun w -> w.Tick >= tick) |> Seq.map (fun w -> int w.Address) |> Set.ofSeq
    | _ -> Set.empty

  /// hiBg as a brush for the memory-mode cursor row.
  let hiBgBrush = SolidColorBrush(Color.FromRgb(0x2Euy, 0x34uy, 0x44uy))

  /// One row of the linear memory sweep.
  let memRowFor (mem: byte[]) (addr: int) : DisasmRow =
    let insn = Disasm.disasmMemory mem addr
    let hex =
      [ for i in 0 .. insn.Length - 1 -> sprintf "%02X" mem[(addr + i) &&& 0xFFFF] ]
      |> String.concat " "
    let comment =
      match control with
      | Some c -> ControlFile.commentAt c addr |> Option.defaultValue ""
      | None -> ""
    let cm = if comment <> "" then sprintf "  ; %s" comment else ""
    { Tag = sprintf "  %04X  %-11s  %s%s" addr hex insn.Text cm
      Brush =
        if addr = memCursor then hiBgBrush
        elif (currentCounts())[addr] > 0 then cyan
        else normal
      IsCurrent = addr = memCursor
      Addr = addr
      InstrIdx = -1 }
  /// Cached "byte is an instruction start" bitmap for the control map,
  /// rebuilt lazily but at most once per second (self-modifying code
  /// shifts boundaries; a 64K decode sweep is a few ms).
  let mutable instrStartsCache: bool[] option = None
  let mutable instrStartsBuiltAt = DateTime.MinValue
  let getInstrStarts () : bool[] option =
    match currentMemory () with
    | None -> None
    | Some mem ->
      if instrStartsCache.IsNone || (DateTime.Now - instrStartsBuiltAt).TotalMilliseconds > 1000.0 then
        let starts = Array.create 0x10000 false
        let mutable a = 0
        while a < 0x10000 do
          starts[a] <- true
          let len = (Disasm.disasmMemory mem a).Length
          a <- a + (if len < 1 then 1 else min 8 len)
        instrStartsCache <- Some starts
        instrStartsBuiltAt <- DateTime.Now
      instrStartsCache

  /// Push the in-memory control file + overlays into the map panel.
  let syncMapData () =
    controlMap.ControlData <- control
    controlMap.InstrStarts <- getInstrStarts ()
    controlMap.MemoryImage <- currentMemory ()

  let refreshDisasm () =
    if disasmModeMemory then
      match currentMemory () with
      | None -> disasmList.ItemsSource <- null
      | Some mem ->
        let rows = ResizeArray<DisasmRow>()
        let mutable a = max 0 ((memCursor - 0x40) &&& 0xFFFF)
        // Align to an instruction start below the cursor.
        while rows.Count < 20 && a < memCursor do
          let next = (a + (Disasm.disasmMemory mem a).Length) &&& 0xFFFF
          if next > memCursor then (rows.Clear(); rows.Add(memRowFor mem a); a <- memCursor)
          else
            if next <= memCursor then rows.Add(memRowFor mem a)
            a <- next
        // From cursor: fill the rest of the window.
        let mutable cur = memCursor
        while rows.Count < 41 do
          rows.Add(memRowFor mem cur)
          cur <- (cur + (Disasm.disasmMemory mem cur).Length) &&& 0xFFFF
        disasmList.ItemsSource <- rows
    else
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
    syncMapData ()

  let updateFlags (af: int) =
    let f = af &&& 0xFF
    let bits = [| 0x80; 0x40; 0x20; 0x10; 0x08; 0x04; 0x02; 0x01 |]
    for i in 0 .. 7 do
      flagCells[i].Text <- if f &&& bits[i] <> 0 then "1" else "0"
      flagCells[i].Foreground <- if f &&& bits[i] <> 0 then green else dim

  let setReg (name: string) (v: int) =
    match regCells.TryGetValue name with
    | true, tb -> tb.Text <- sprintf "%04X" v
    | _ -> ()

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
          let line = System.Windows.Shapes.Line(X1 = p1.X + 104.0, Y1 = p1.Y + 14.0, X2 = p2.X, Y2 = p2.Y + 14.0)
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
    previewTimer.Stop()
    saveReplay true
    playBtn.Content <- "Play"
    buildTraceNow ()
    clampCursor ()
    syncSlider ()
    refreshAll ()

  /// Does the game have a CE program (a compiled F# version) yet?
  let hasCE (m: GameManifest) : bool =
    GameRegistry.tryFind m.GameId |> Option.isSome

  /// Build the CE engine for a game; updates the parity display.
  let startCE (m: GameManifest) : CEGame option =
    match GameRegistry.tryFind m.GameId with
    | Some bundle ->
      let matching, total, divergences = GameRegistry.parity bundle
      let status =
        if divergences.IsEmpty then sprintf "CE parity: %d/%d bytes match" matching total
        else sprintf "CE parity: %d/%d - diverges at %A" matching total divergences
      parityLabel.Text <- status
      Some(CEGame(bundle.Program, bundle.Memory, bundle.EntryState))
    | None ->
      parityLabel.Text <- "no CE program for this game yet"
      None

  /// Switch the engine (Interpreter <-> CE); returns true when the switch
  /// happened.
  let switchEngine (toCE: bool) : bool =
    if toCE then
      match currentGame with
      | Some g when hasCE g ->
        if session.IsSome then pauseGame ()
        match startCE g with
        | Some ce ->
          session <- None
          ceGame <- Some ce
          engineCE <- true
          running <- true
          frameTimer.Start()
          statusText.Text <- sprintf "%s running on the CE engine (compiled F#)" g.Name
          true
        | None -> false
      | _ -> false
    else if engineCE then
      ceGame <- None
      engineCE <- false
      pauseGame ()
      match currentGame with
      | Some g -> launchGame g; loadControlForGame ()
      | None -> ()
      true
    else true


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
        sprintf "emulator ready (%s) - frame 0, recording %s, saved input %d events"
          (if s.WarmStart then "cached entry state" else "booted to game entry")
          (if s.Recorder.RecordEnabled then "ON" else "OFF") s.KeyLog.Count
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

    // center: timeline selector, mode buttons, disassembly, comment toolbar
    let center = DockPanel(Margin = Thickness(8.0))

    // 1. top: timeline selector with the button row underneath it
    let tlColumn = StackPanel()
    tlColumn.Children.Add timeline |> ignore
    let tlBtns = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(2.0, 4.0, 0.0, 0.0))
    for b in [ brushABtn :> FrameworkElement; brushBBtn :> FrameworkElement; previewABtn :> FrameworkElement; previewBBtn :> FrameworkElement; saveCtrlBtn :> FrameworkElement; importCtrlBtn :> FrameworkElement; exportCtrlBtn :> FrameworkElement; newCtrlBtn :> FrameworkElement ] do
      b.Margin <- Thickness(2.0)
      tlBtns.Children.Add b |> ignore
    tlColumn.Children.Add tlBtns |> ignore
    DockPanel.SetDock(tlColumn, Dock.Top)
    center.Children.Add tlColumn |> ignore

    // 2. strip + slider
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

    // 3. engine radios + disasm mode toggles above the code window
    let modeRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 4.0))
    let engLabel = TextBlock(Text = "engine:", Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(0.0, 0.0, 4.0, 0.0))
    modeRow.Children.Add engLabel |> ignore
    for r in [ oracleRadio :> FrameworkElement; ceRadio :> FrameworkElement; diffRadio :> FrameworkElement ] do
      r.Margin <- Thickness(0.0, 0.0, 10.0, 0.0)
      r.VerticalAlignment <- VerticalAlignment.Center
      modeRow.Children.Add r |> ignore
    let sep1 = Border(Width = 1.0, Height = 18.0, Background = dim, Margin = Thickness(4.0, 0.0, 8.0, 0.0), VerticalAlignment = VerticalAlignment.Center)
    modeRow.Children.Add sep1 |> ignore
    let modeLabel = TextBlock(Text = "code view:", Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(0.0, 0.0, 4.0, 0.0))
    modeRow.Children.Add modeLabel |> ignore
    memModeBtn.VerticalAlignment <- VerticalAlignment.Center
    execModeBtn.VerticalAlignment <- VerticalAlignment.Center
    modeRow.Children.Add memModeBtn |> ignore
    modeRow.Children.Add execModeBtn |> ignore
    DockPanel.SetDock(modeRow, Dock.Top)
    center.Children.Add modeRow |> ignore

    // 4. the code window (fills the rest)
    center.Children.Add(disasmList) |> ignore

    // 5. bottom: shared comment box + mass-comment buttons
    let cmtBar = StackPanel(Margin = Thickness(0.0, 4.0, 0.0, 0.0))
    let cmtLabel =
      TextBlock(
        Text = "comment text (shared):",
        Foreground = dim,
        FontSize = 11.0,
        Margin = Thickness(0.0, 0.0, 6.0, 2.0))
    cmtBar.Children.Add cmtLabel |> ignore
    cmtBar.Children.Add commentBox |> ignore
    let cmtBtnRow = WrapPanel(Margin = Thickness(0.0, 4.0, 0.0, 0.0))
    for b in [ idxCmtBtn :> FrameworkElement; nameBeforeBtn :> FrameworkElement; nameABtn :> FrameworkElement;
               nameBBtn :> FrameworkElement; cmtABtn :> FrameworkElement; cmtBBtn :> FrameworkElement;
               cmtBNotA :> FrameworkElement; cmtANotB :> FrameworkElement ] do
      b.Margin <- Thickness(0.0, 0.0, 6.0, 3.0)
      cmtBtnRow.Children.Add b |> ignore
    cmtBar.Children.Add cmtBtnRow |> ignore
    DockPanel.SetDock(cmtBar, Dock.Bottom)
    center.Children.Add cmtBar |> ignore

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
    let scanTab = TabItem(Header = "Scan")
    let scanPanel = DockPanel(Margin = Thickness(4.0))
    let scanTop = WrapPanel()
    scanStartBtn.Margin <- Thickness(0.0, 0.0, 8.0, 3.0)
    scanTop.Children.Add scanStartBtn |> ignore
    for b in [ scanChangedBtn :> FrameworkElement; scanSameBtn :> FrameworkElement; scanSmallerBtn :> FrameworkElement; scanLargerBtn :> FrameworkElement; scanWrittenBtn :> FrameworkElement; scanExecutedBtn :> FrameworkElement ] do
      b.Margin <- Thickness(0.0, 0.0, 6.0, 3.0)
      b.IsEnabled <- false
      scanTop.Children.Add b |> ignore
    scanExactBtn.IsEnabled <- false
    let exactRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 6.0, 3.0))
    exactRow.Children.Add(scanExactBox) |> ignore
    exactRow.Children.Add(scanExactBtn) |> ignore
    scanTop.Children.Add exactRow |> ignore
    DockPanel.SetDock(scanTop, Dock.Top)
    scanPanel.Children.Add scanTop |> ignore

    let scanOpts = StackPanel(Margin = Thickness(0.0, 4.0, 0.0, 0.0))
    let widthRow = StackPanel(Orientation = Orientation.Horizontal)
    widthRow.Children.Add(scanWidth8) |> ignore
    widthRow.Children.Add(scanWidth16) |> ignore
    scanOpts.Children.Add widthRow |> ignore
    let inclRow = StackPanel(Orientation = Orientation.Horizontal)
    for c in [ scanRomCheck :> FrameworkElement; scanScreenCheck :> FrameworkElement ] do
      c.Margin <- Thickness(0.0, 0.0, 10.0, 0.0)
      inclRow.Children.Add c |> ignore
    scanCodeOnly.Items.Add("all memory") |> ignore
    scanCodeOnly.Items.Add("code only") |> ignore
    scanCodeOnly.Items.Add("non-code only") |> ignore
    scanCodeOnly.SelectedIndex <- 0
    inclRow.Children.Add scanCodeOnly |> ignore
    scanOpts.Children.Add inclRow |> ignore
    scanCountLabel.Margin <- Thickness(0.0, 4.0, 0.0, 0.0)
    scanOpts.Children.Add scanCountLabel |> ignore
    let jumpRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 0.0))
    let scanJumpBtn = Button(Content = "Jump to selected", Width = 120.0, Margin = Thickness(0.0, 0.0, 6.0, 0.0))
    let scanCmtBtn = Button(Content = "Comment selected", Width = 130.0)
    jumpRow.Children.Add scanJumpBtn |> ignore
    jumpRow.Children.Add scanCmtBtn |> ignore
    scanOpts.Children.Add jumpRow |> ignore
    DockPanel.SetDock(scanOpts, Dock.Bottom)
    scanPanel.Children.Add scanOpts |> ignore
    scanPanel.Children.Add scanList |> ignore
    scanTab.Content <- scanPanel

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
    right.Items.Add scanTab |> ignore
    right.Items.Add contractTab |> ignore
    right.Items.Add theaterTab |> ignore
    // toolbar
    let toolbar = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(8.0))
    let runBtn = Button(Content = "Run")
    let pauseBtn = Button(Content = "Pause")
    let stepFrameBtn = Button(Content = "Step frame")
    let saveBtn = Button(Content = "Save trace")
    let loadBtn = Button(Content = "Load trace")
    runBtn.Click.Add(fun _ ->
      pauseGame ()
      match session with
      | Some s when replaying || s.ReplayFinished -> s.StopReplay(); replaying <- false
      | _ -> ()
      running <- true
      frameTimer.Start())
    pauseBtn.Click.Add(fun _ ->
      pauseGame ()
      if replaying then replaying <- false)
    stepFrameBtn.Click.Add(fun _ -> pauseGame (); renderFrame ())
    saveBtn.Click.Add(fun _ -> saveTrace ())
    loadBtn.Click.Add(fun _ -> loadTrace ())
    // Rewind: dragging pauses the game and previews the snapshot at the
    // slider's frame; Go commits the branch (new future starts here);
    // Replay re-runs the recorded keys from here and stops at the end.
    rewindSlider.PreviewMouseDown.Add(fun _ ->
      if session.IsSome then pauseGame ()
      rewinding <- true
      saveReplay true)
    rewindSlider.PreviewMouseUp.Add(fun _ ->
      rewinding <- false
      saveReplay true)
    rewindSlider.ValueChanged.Add(fun args ->
      match session with
      | Some s ->
        let target = int args.NewValue
        if rewinding && target <= s.History.LastFrame then
          if running || replaying then pauseGame ()
          if replaying then
            s.StopReplay ()
            replaying <- false // drag aborts the script; preview takes over
          s.RewindTo(target)
          presentGame ()
          updateTimeLabel s.Frame
      | None -> ())
    goBtn.Click.Add(fun _ ->
      match session with
      | Some s ->
        pauseGame ()
        s.BranchAt(s.Frame)
        saveReplay true
        replaying <- false
        running <- true
        frameTimer.Start()
        statusText.Text <- sprintf "branched at frame %d - new future starts here" s.Frame
      | None -> ())
    replayBtn.Click.Add(fun _ ->
      match session with
      | Some s ->
        pauseGame ()
        replayExtent <- max 1 s.History.LastFrame // timeline before truncation
        s.StartReplay()
        replaying <- true
        running <- true
        frameTimer.Start()
        statusText.Text <- sprintf "replaying keys until frame %d" s.ReplayEndFrame
      | None -> ())
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

    // game selection + manual boot + script export
    // CE registry: one registration per game project the desktop shell
    // references. New game = new games/<id> fsproj + a line here.
    GameRegistry.register
      { GameId = "minimal"
        Name = "Minimal"
        Memory = fst (MinimalGame.Game.entryState MinimalGame.Image.binary)
        BaseAddress = 0x8000
        Program = MinimalGame.Image.program
        EntryState = snd (MinimalGame.Game.entryState MinimalGame.Image.binary) }
    gamesList <- Manifest.discover (findGamesDir ())
    if List.isEmpty gamesList then
      // Fallback: no games/ folder reachable from the app; the explicit
      // Jetpac fallback keeps the app usable without manifests.
      gamesList <-
        [ { GameId = "jetpac"
            ManifestPath = ""
            GameDirectory = Directory.GetCurrentDirectory()
            Name = "Jetpac"
            Default = true
            Rom = LocalAssets.find "48.rom"
            Tzx = LocalAssets.find "Jetpac.tzx"
            Boot = "auto"
            Script = None
            ProgramBin = None
            ProgramAddress = None } ]
    gameCombo.ItemsSource <- gamesList
    gameCombo.DisplayMemberPath <- "Name"
    let startupGame =
      match gamesList |> List.tryFind (fun g -> g.Default) with
      | Some g -> g
      | None ->
        match gamesList |> List.tryFind (fun g -> g.GameId = "jetpac") with
        | Some g -> g
        | None -> List.head gamesList
    let startupIndex = gamesList |> List.findIndex (fun g -> g.GameId = startupGame.GameId)
    engineCombo.Items.Add("Interpreter") |> ignore
    engineCombo.Items.Add("CE") |> ignore
    engineCombo.SelectedIndex <- 0
    engineCombo.SelectionChanged.Add(fun _ ->
      match engineCombo.SelectedIndex with
      | 1 ->
        if not (switchEngine true) then engineCombo.SelectedIndex <- 0
      | _ -> switchEngine false |> ignore)
    gameCombo.SelectedIndex <- startupIndex
    gameCombo.SelectionChanged.Add(fun _ ->
      match gameCombo.SelectedItem with
      | :? GameManifest as g ->
        ceGame <- None
        engineCE <- false
        engineCombo.SelectedIndex <- 0
        parityLabel.Text <- ""
        if session.IsSome then pauseGame ()
        session <- None
        loaded <- None
        built <- None
        engineCombo.IsEnabled <- hasCE g
        launchGame g
        loadControlForGame ()
      | _ -> ())
    setEntryBtn.Click.Add(fun _ ->
      match manualBoot with
      | Some mb ->
        mb.CaptureEntry()
        manualBoot <- None
        manualTimer.Stop()
        statusText.Text <- "game entry captured - starting the port session..."
        match currentGame with
        | Some g -> launchGame g; loadControlForGame ()
        | None -> ()
      | None -> statusText.Text <- "no manual boot running (the game has a cached entry or boots automatically)")
    exportScriptBtn.Click.Add(fun _ ->
      match session with
      | Some s ->
        let dlg = Microsoft.Win32.SaveFileDialog(Filter = "JSON (*.json)|*.json", FileName = "script.json")
        if dlg.ShowDialog() = Nullable<bool>(true) then
          let body =
            s.KeyLog.Events
            |> Seq.map (fun e ->
              sprintf "  { \"frame\": %d, \"row\": %d, \"bit\": %d, \"pressed\": %b }" e.Frame e.Row e.Bit e.Pressed)
            |> String.concat ",\n"
          System.IO.File.WriteAllText(dlg.FileName, "[\n" + body + "\n]\n")
          statusText.Text <- sprintf "exported %d key events to %s" s.KeyLog.Events.Count dlg.FileName
      | None -> statusText.Text <- "no game running - play first, then export")
    timelineBtn.Click.Add(fun _ ->
      try
        match timelineWin with
        | Some w when w.IsLoaded -> w.Activate() |> ignore
        | _ ->
          // Timeline length: a running replay knows its full extent up
          // front (ReplayEndFrame), so the selector shows the whole range
          // immediately instead of growing with the playhead; recording
          // beyond the replay extends the max naturally.
          let w =
            TimelineDemoWindow(fun () ->
              match session with
              | Some s -> max 1L (int64 (max s.ReplayEndFrame s.Frame))
              | None ->
                match currentTrace () with
                | Some t when t.FrameTicks.Length > 0 -> int64 t.FrameTicks.Length
                | _ -> 300L)
          w.Owner <- self
          timelineWin <- Some w
          w.Closed.Add(fun _ -> timelineWin <- None)
          w.Show()
      with ex -> statusText.Text <- sprintf "timeline demo failed: %s" ex.Message)

    for c in [ gameCombo :> FrameworkElement; engineCombo :> FrameworkElement; parityLabel :> FrameworkElement; setEntryBtn :> FrameworkElement; exportScriptBtn :> FrameworkElement; timelineBtn :> FrameworkElement; runBtn :> FrameworkElement; pauseBtn :> FrameworkElement; stepFrameBtn :> FrameworkElement; recordToggle :> FrameworkElement; soundToggle :> FrameworkElement; saveBtn :> FrameworkElement; loadBtn :> FrameworkElement ] do
      c.Margin <- Thickness(4.0, 0.0, 4.0, 0.0)
      toolbar.Children.Add c |> ignore
    toolbar.Children.Add sep |> ignore
    // rewind + replay group
    for c in [ rewindSlider :> FrameworkElement; timeLabel :> FrameworkElement; goBtn :> FrameworkElement; replayBtn :> FrameworkElement ] do
      c.Margin <- Thickness(4.0, 0.0, 4.0, 0.0)
      toolbar.Children.Add c |> ignore
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

    // far-right full-height control map: spans every row (toolbar, main,
    // lift tray, status). Splitter is a direct Grid child so dragging it
    // actually resizes columns: col1 shrinks main, col2 grows the map.
    root.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
    root.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
    root.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
    let mapSplitter =
      GridSplitter(
        Width = 4.0,
        VerticalAlignment = VerticalAlignment.Stretch,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Background = SolidColorBrush(Color.FromRgb(0x22uy, 0x26uy, 0x30uy)),
        ResizeBehavior = GridResizeBehavior.PreviousAndNext)
    Grid.SetColumn(mapSplitter, 1)
    Grid.SetRowSpan(mapSplitter, 4)
    root.Children.Add mapSplitter |> ignore
    controlMap.MinWidth <- 40.0
    Grid.SetColumn(controlMap, 2)
    Grid.SetRowSpan(controlMap, 4)
    root.Children.Add controlMap |> ignore
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

    // ---- control GUI wiring ------------------------------------------------
    // Preview = replay the selection as a movie lasting exactly one second
    // (50 ticks x 20 ms), leaving the session parked on the last frame.
    let scrubToFrame (u: int64) =
      match session with
      | Some s ->
        if running || replaying then pauseGame ()
        if replaying then
          s.StopReplay ()
          replaying <- false
        previewTimer.Stop()
        s.RewindTo(max 0 (min (int u) s.History.LastFrame))
        presentGame ()
        updateTimeLabel s.Frame
      | None -> ()

    previewRangeImpl <- fun which ->
      let fStart, fEnd = rangeOf which
      let label = if which = A then "A" else "B"
      match session with
      | Some s when fEnd - 1 <= s.History.LastFrame && fEnd - 1 >= 0 ->
        if running || replaying then pauseGame ()
        if replaying then
          s.StopReplay ()
          replaying <- false
        previewTimer.Stop()
        previewFrom <- max 0 fStart
        previewSpan <- max 0 (fEnd - 1 - previewFrom)
        previewTicks <- 0
        s.RewindTo previewFrom
        presentGame ()
        updateTimeLabel s.Frame
        statusText.Text <- sprintf "preview %s: frames %d..%d over 1s" label fStart fEnd
        previewTimer.Start()
      | _ ->
        match currentTrace () with
        | Some t when t.Entries.Length > 0 ->
          cursor <- entryAtFrame t (fEnd - 1)
          refreshAll ()
          syncSlider ()
          statusText.Text <- sprintf "preview %s: jumped to instr %d" label cursor
        | _ -> statusText.Text <- "nothing to preview yet - play or load a trace"

    previewTimer.Tick.Add(fun _ ->
      previewTicks <- previewTicks + 1
      match session with
      | Some s ->
        let frac = min previewTicks 50
        s.RewindTo(max 0 (min (previewFrom + previewSpan * frac / 50) s.History.LastFrame))
        presentGame ()
        updateTimeLabel s.Frame
        if previewTicks >= 50 then previewTimer.Stop()
      | None -> previewTimer.Stop())

    // dragging anywhere on the selector (track or A/B chips) scrubs the
    // emulator screen to the frame under the cursor, both directions.
    timeline.Scrubbed.Add(fun u ->
      match session with
      | Some _ -> scrubToFrame u
      | None ->
        match currentTrace () with
        | Some t when t.Entries.Length > 0 && int u < t.Entries.Length ->
          cursor <- entryAtFrame t (int u)
          refreshAll ()
          syncSlider ()
        | _ -> ())

    // brush toggle buttons: selected one fills with its brush color
    let styleBrushBtn (btn: Button) (color: Color) (on: bool) =
      btn.Background <- if on then SolidColorBrush color else panel
      btn.Foreground <- if on then darkFg else dim
      btn.BorderBrush <- if on then SolidColorBrush color else dim
      btn.FontWeight <- if on then FontWeights.Bold else FontWeights.Normal
    let setActiveBrush (b: BrushId) =
      timeline.ActiveBrush <- b
      styleBrushBtn brushABtn sky (b = A)
      styleBrushBtn brushBBtn amber (b = B)
    brushABtn.Click.Add(fun _ -> setActiveBrush A)
    brushBBtn.Click.Add(fun _ -> setActiveBrush B)
    setActiveBrush timeline.ActiveBrush

    // ---- control map wiring -------------------------------------------------
    // click/drag scrubs the code views to the address under the cursor
    controlMap.AddressClicked.Add(fun addr ->
      if disasmModeMemory then
        memCursor <- addr &&& 0xFFFF
        refreshDisasm ()
      else
        match currentTrace (), session with
        | Some t, _ when t.Entries.Length > 0 && addr < t.FirstIndexAtPc.Length && t.FirstIndexAtPc[addr] >= 0 ->
          cursor <- t.FirstIndexAtPc[addr]
          refreshAll ()
          syncSlider ()
        | _ -> ())

    // right-click editing: items that need text take it from the shared
    // comment box, matching the mass-comment toolbar convention.
    controlMap.MenuRequested.Add(fun addr ->
      match control with
      | None -> statusText.Text <- "no control file - click New ctrl first"
      | Some _ ->
        let menu = ContextMenu()
        let mkItem (txt: string) (act: ControlFile -> ControlFile) =
          let mi = MenuItem(Header = txt)
          mi.Click.Add(fun _ ->
            match control with
            | Some c ->
              control <- Some(act c)
              refreshDisasm ()
              statusText.Text <- sprintf "%s @ %04X" txt addr
            | None -> ())
          menu.Items.Add mi |> ignore
        mkItem "kind: code" (fun c -> ControlFile.setKindAt c addr Code)
        mkItem "kind: data" (fun c -> ControlFile.setKindAt c addr Data)
        mkItem "kind: gap" (fun c -> ControlFile.setKindAt c addr Gap)
        menu.Items.Add(Separator()) |> ignore
        let split = MenuItem(Header = "split block here")
        split.Click.Add(fun _ ->
          match control with
          | Some c ->
            let canSplit =
              match instrStartsCache with
              | Some s -> addr < s.Length && s[addr]
              | None -> true
            if canSplit then
              control <- Some(ControlFile.splitBlockAt c addr)
              refreshDisasm ()
              statusText.Text <- sprintf "block split at %04X" addr
            else statusText.Text <- sprintf "%04X is not an instruction start - split lands on one" addr
          | None -> ())
        menu.Items.Add split |> ignore
        mkItem "merge into next block" (fun c -> ControlFile.mergeWithNext c addr)
        mkItem "rename block <- comment box" (fun c ->
          if String.IsNullOrWhiteSpace commentBox.Text then c
          else ControlFile.renameBlockAt c addr (commentBox.Text.Trim()))
        mkItem "line comment <- comment box" (fun c ->
          ControlFile.upsert c { Kind = Line; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = commentBox.Text })
        mkItem "range over block <- comment box" (fun c ->
          match ControlFile.blockAt c addr with
          | Some b -> ControlFile.upsert c { Kind = Range; Addr = b.Start; EndExcl = b.EndExcl; InstrIndex = -1; Text = commentBox.Text }
          | None -> c)
        menu.PlacementTarget <- controlMap
        menu.Placement <- PlacementMode.MousePoint
        menu.IsOpen <- true)


    previewABtn.Click.Add(fun _ -> previewRange A)
    previewBBtn.Click.Add(fun _ -> previewRange B)
    saveCtrlBtn.Click.Add(fun _ -> saveControlNow ())

    importCtrlBtn.Click.Add(fun _ ->
      let dlg = Microsoft.Win32.OpenFileDialog(Filter = "Control files (*.json;*.ctl)|*.json;*.ctl|All files (*.*)|*.*")
      if dlg.ShowDialog() = Nullable<bool>(true) then
        try
          let text = File.ReadAllText dlg.FileName
          let cf =
            if dlg.FileName.EndsWith(".ctl", StringComparison.OrdinalIgnoreCase) then SkoolCtl.import text
            else ControlFile.fromJson text
          control <- Some cf
          controlGameDir <- controlDir ()
          statusText.Text <- sprintf "imported %d blocks, %d comments from %s" cf.Blocks.Length cf.Comments.Length dlg.FileName
        with ex -> statusText.Text <- sprintf "import failed: %s" ex.Message)

    exportCtrlBtn.Click.Add(fun _ ->
      match control with
      | Some c ->
        let suggested = currentGame |> Option.map (fun g -> g.GameId + ".ctl") |> Option.defaultValue "game.ctl"
        let dlg = Microsoft.Win32.SaveFileDialog(Filter = "SkoolKit ctrl (*.ctl)|*.ctl", FileName = suggested)
        if dlg.ShowDialog() = Nullable<bool>(true) then
          File.WriteAllText(dlg.FileName, SkoolCtl.export c)
          statusText.Text <- sprintf "exported skool ctrl to %s" dlg.FileName
      | None -> statusText.Text <- "no control file loaded")

    newCtrlBtn.Click.Add(fun _ ->
      let ext = timelineExtent () |> int
      control <- Some(ControlFile.empty (if ext > 0 then 0x4000 else 0x4000) 0x10000)
      controlGameDir <- controlDir ()
      statusText.Text <- sprintf "empty control file created over $4000-$FFFF")

    // engine radios reuse switchEngine for oracle/CE; differential runs a
    // short lockstep validation and reports in statusText.
    oracleRadio.Checked.Add(fun _ ->
      if ceGame.IsSome then switchEngine false |> ignore)
    ceRadio.Checked.Add(fun _ ->
      if ceGame.IsNone then
        if not (switchEngine true) then oracleRadio.IsChecked <- Nullable<bool>(true))
    diffRadio.Checked.Add(fun _ ->
      match currentGame, session with
      | Some g, Some _ when hasCE g ->
        let framesN = max 60 ((fst (rangeOf A)) - (snd (rangeOf A)) |> abs |> max 300)
        statusText.Text <- sprintf "differential run (%s vs CE) starting..." g.Name
        let rom, tzx = g.Rom, (if g.Tzx <> "" then g.Tzx else g.Rom)
        let task =
          System.Threading.Tasks.Task.Run(fun () ->
            Validation.run rom tzx framesN (Jetpac3.Core.Script.defaultSession framesN)
              Jetpac3.Core.LiftedRoutines.registryHook
              System.Threading.CancellationToken.None)
        task.ContinueWith(fun (t: System.Threading.Tasks.Task<Validation.Report>) ->
          self.Dispatcher.Invoke(System.Action(fun () ->
            if t.IsFaulted then statusText.Text <- "differential failed: " + t.Exception.Message
            else
              let rep = t.Result
              if rep.Passed then statusText.Text <- sprintf "DIFFERENTIAL OK over %d frames" rep.Frames
              else
                match rep.FirstDivergence with
                | Some d -> statusText.Text <- sprintf "DIFFERENTIAL FAILED at frame %d (%s): port=%04X oracle=%04X" d.Frame d.Kind d.PortPc d.OraclePc
                | None -> statusText.Text <- "DIFFERENTIAL FAILED (no details)"))) |> ignore
      | _ ->
        statusText.Text <- "differential needs a game with a compiled CE program"
        oracleRadio.IsChecked <- Nullable<bool>(true))

    // disassembly mode toggles (mutually exclusive)
    memModeBtn.Checked.Add(fun _ ->
      disasmModeMemory <- true
      execModeBtn.IsChecked <- Nullable<bool>(false)
      refreshDisasm ())
    execModeBtn.Checked.Add(fun _ ->
      disasmModeMemory <- false
      memModeBtn.IsChecked <- Nullable<bool>(false)
      refreshDisasm ())

    // mass-comment buttons: all read commentBox.Text
    let text () : string = commentBox.Text
    let addComment (m: ControlComment) =
      match control with
      | Some c ->
        control <- Some(ControlFile.upsert c m)
        statusText.Text <- sprintf "comment added (%s)" (ControlFile.kindToString m.Kind)
        refreshDisasm ()
      | None -> statusText.Text <- "no control file - click New ctrl first"
    idxCmtBtn.Click.Add(fun _ ->
      match currentTrace () with
      | Some t when t.Entries.Length > 0 ->
        addComment { Kind = Exec; Addr = 0; EndExcl = 0; InstrIndex = cursor; Text = text () }
      | _ -> statusText.Text <- "no trace - cannot index-comment")
    nameBeforeBtn.Click.Add(fun _ ->
      match currentTrace (), control with
      | Some t, Some c when t.Entries.Length > 0 ->
        let pcHere = int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc
        let start =
          c.Blocks
          |> List.filter (fun b -> b.Start <= pcHere)
          |> List.map (fun b -> b.Start)
          |> fun xs -> if List.isEmpty xs then 0x4000 else List.max xs
        addComment { Kind = Name; Addr = start; EndExcl = pcHere; InstrIndex = -1; Text = text () }
      | _ -> statusText.Text <- "no trace/control")
    let regionComments (which: BrushId) (kind: CommentKind) =
      match currentTrace (), control with
      | Some t, Some c when t.Entries.Length > 0 ->
        let fA, fB = rangeOf which
        let pcs = ControlFile.pcsInFrames t fA fB
        if List.isEmpty pcs then statusText.Text <- sprintf "no instructions executed in range %s" (if which = A then "A" else "B")
        else
          let mutable cc = c
          match kind with
          | k when k = Name ->
            addComment { Kind = Name; Addr = List.head pcs; EndExcl = (List.last pcs) + 8; InstrIndex = -1; Text = text () }
          | _ ->
            for pc in pcs do
              cc <- ControlFile.upsert cc { Kind = Line; Addr = pc; EndExcl = 0; InstrIndex = -1; Text = text () }
            control <- Some { cc with Dirty = true }
            statusText.Text <- sprintf "%d line comments added" pcs.Length
            refreshDisasm ()
      | _ -> statusText.Text <- "no trace/control"
    nameABtn.Click.Add(fun _ -> regionComments A Name)
    nameBBtn.Click.Add(fun _ -> regionComments B Name)
    cmtABtn.Click.Add(fun _ -> regionComments A Line)
    cmtBBtn.Click.Add(fun _ -> regionComments B Line)
    cmtBNotA.Click.Add(fun _ ->
      match currentTrace () with
      | Some t ->
        let a1, a2 = rangeOf A
        let b1, b2 = rangeOf B
        let onlyA, onlyB = ControlFile.diffPcs (ControlFile.pcsInFrames t a1 a2) (ControlFile.pcsInFrames t b1 b2)
        match control with
        | Some c ->
          let mutable cc = c
          for pc in onlyB do
            cc <- ControlFile.upsert cc { Kind = Line; Addr = pc; EndExcl = 0; InstrIndex = -1; Text = text () }
          control <- Some { cc with Dirty = true }
          statusText.Text <- sprintf "%d B-only comments added" onlyB.Length
          refreshDisasm ()
        | None -> statusText.Text <- "no control file"
      | None -> ())
    cmtANotB.Click.Add(fun _ ->
      match currentTrace () with
      | Some t ->
        let a1, a2 = rangeOf A
        let b1, b2 = rangeOf B
        let onlyA, onlyB = ControlFile.diffPcs (ControlFile.pcsInFrames t a1 a2) (ControlFile.pcsInFrames t b1 b2)
        match control with
        | Some c ->
          let mutable cc = c
          for pc in onlyA do
            cc <- ControlFile.upsert cc { Kind = Line; Addr = pc; EndExcl = 0; InstrIndex = -1; Text = text () }
          control <- Some { cc with Dirty = true }
          statusText.Text <- sprintf "%d A-only comments added" onlyA.Length
          refreshDisasm ()
        | None -> statusText.Text <- "no control file"
      | None -> ())

    // scan buttons
    scanStartBtn.Click.Add(fun _ -> startScan ())
    scanChangedBtn.Click.Add(fun _ -> applyScanFilter (fun _ o n -> n <> o))
    scanSameBtn.Click.Add(fun _ -> applyScanFilter (fun _ o n -> n = o))
    scanExactBtn.Click.Add(fun _ ->
      let v = scanExactBox.Text.Trim()
      let parsed =
        if v.StartsWith "$" then Int32.TryParse(v.Substring(1), Globalization.NumberStyles.HexNumber, null)
        elif v.StartsWith "0x" then Int32.TryParse(v.Substring(2), Globalization.NumberStyles.HexNumber, null)
        else Int32.TryParse v
      match parsed with
      | true, value ->
        applyScanFilter (fun _ o n -> if scanWidth16.IsChecked.GetValueOrDefault() then n = (value &&& 0xFFFF) || (((n <<< 8) ||| n) &&& 0xFFFF) = (value &&& 0xFF) else n = (value &&& 0xFF))
      | _ -> statusText.Text <- "enter a number ($hex or decimal)")
    scanSmallerBtn.Click.Add(fun _ -> applyScanFilter (fun _ o n -> n < o))
    scanLargerBtn.Click.Add(fun _ -> applyScanFilter (fun _ o n -> n > o))
    scanWrittenBtn.Click.Add(fun _ ->
      let writtenSet = writtenSinceBaseline ()
      scanResults <- scanResults |> List.filter writtenSet.Contains
      refreshScanList ())
    scanExecutedBtn.Click.Add(fun _ ->
      let counts = currentCounts ()
      scanResults <- scanResults |> List.filter (fun a -> counts[a] > 0)
      refreshScanList ())

    // scan result interactions
    scanJumpBtn.Click.Add(fun _ ->
      match scanList.SelectedItem with
      | :? DisasmRow as row when row.Addr >= 0 ->
        if disasmModeMemory then
          memCursor <- row.Addr
          refreshDisasm ()
        else
          match currentTrace (), session with
          | Some t, _ when t.FirstIndexAtPc[row.Addr] >= 0 ->
            cursor <- t.FirstIndexAtPc[row.Addr]
            refreshAll ()
            syncSlider ()
          | _, Some s ->
            // No trace yet: at least show it in memory mode.
            disasmModeMemory <- true
            memCursor <- row.Addr
            memModeBtn.IsChecked <- Nullable<bool>(true)
            statusText.Text <- sprintf "no trace yet - showing %04X in memory view" row.Addr
          | _ -> ()
      | _ -> ())
    scanCmtBtn.Click.Add(fun _ ->
      match scanList.SelectedItem with
      | :? DisasmRow as row when row.Addr >= 0 ->
        match control with
        | Some c ->
          control <- Some(ControlFile.upsert c { Kind = Line; Addr = row.Addr; EndExcl = 0; InstrIndex = -1; Text = commentBox.Text })
          statusText.Text <- sprintf "comment added at %04X" row.Addr
          refreshDisasm ()
        | None -> statusText.Text <- "no control file - click New ctrl first"
      | _ -> statusText.Text <- "select an address in the scan list first")

    // keep the timeline extent fresh each UI tick
    uiTimer.Tick.Add(fun _ ->
      timeline.Length <- timelineExtent ())
    // keep the control map live: cursor hairline, heat/self-mod overlays,
    // and a thumb showing which address window the code view displays.
    uiTimer.Tick.Add(fun _ ->
      let curAddr =
        if disasmModeMemory then Some memCursor
        else
          match currentTrace () with
          | Some t when t.Entries.Length > 0 ->
            Some(int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc)
          | _ -> None
      match curAddr with
      | Some a ->
        controlMap.CursorAddress <- a
        if controlMap.IsOutside a then controlMap.CenterOn a
        let lo, hi =
          if disasmModeMemory then (memCursor - 0x40, memCursor + 0xC0)
          else
            match currentTrace () with
            | Some t when t.Entries.Length > 0 ->
              let c = max 0 (min cursor (t.Entries.Length - 1))
              let pcs =
                [ for j in -20 .. 20 do
                    let idx = c + j
                    if idx >= 0 && idx < t.Entries.Length then int t.Entries[idx].Pc ]
              if pcs.IsEmpty then (-1, -1) else (List.min pcs, List.max pcs + 1)
            | _ -> (-1, -1)
        controlMap.DisasmViewport <- (lo, hi)
      | None -> controlMap.CursorAddress <- -1
      // pick up control-file edits made outside refreshDisasm's path
      let stale =
        match controlMap.ControlData, control with
        | Some old, Some newC -> not (Object.ReferenceEquals(old, newC))
        | None, None -> false
        | _ -> true
      if stale then controlMap.ControlData <- control
      controlMap.InstrStarts <- getInstrStarts ()
      controlMap.MemoryImage <- currentMemory ()
      controlMap.ExecCounts <- currentCounts ()
      controlMap.SelfModified <- currentSelfModified ())

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
      match ceGame with
      | Some c -> List.iter (fun (r, b) -> c.SetKey(r, b, true)) (keyMap e.Key)
      | None ->
        match session with
        | Some s -> List.iter (fun (r, b) -> s.SetKey(r, b, true)) (keyMap e.Key)
        | None ->
          match manualBoot with
          | Some mb -> List.iter (fun (r, b) -> mb.SetKey(r, b, true)) (keyMap e.Key)
          | None -> ())
    self.KeyUp.Add(fun e ->
      match ceGame with
      | Some c -> List.iter (fun (r, b) -> c.SetKey(r, b, false)) (keyMap e.Key)
      | None ->
        match session with
        | Some s -> List.iter (fun (r, b) -> s.SetKey(r, b, false)) (keyMap e.Key)
        | None ->
          match manualBoot with
          | Some mb -> List.iter (fun (r, b) -> mb.SetKey(r, b, false)) (keyMap e.Key)
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
    manualTimer.Tick.Add(fun _ ->
      match manualBoot with
      | Some mb ->
        mb.RunFrame()
        gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), mb.ScreenBuffer, 320 * 4, 0)
        statusText.Text <-
          sprintf "manual boot: frame %d, tape %s - press 'Set as game entry' when the game is ready"
            mb.FrameCount (if mb.TapePlaying then "playing" else "stopped")
      | None -> ())
    uiTimer.Tick.Add(fun _ ->
      match session with
      | None ->
        match ceGame with
        | Some c ->
          if not rewinding then
            rewindSlider.Maximum <- 0.0
            rewindSlider.Value <- 0.0
          updateTimeLabel c.Frame
          statusText.Text <- sprintf "CE engine: frame=%d tick=%.2fM" c.Frame (float c.CycleCount / 1_000_000.0)
        | None ->
          match bootTask with
          | Some t when t.IsCompleted ->
            try
              let loadedSession = t.Result
              session <- Some loadedSession
              let hasSavedReplay =
                match currentGame with
                | Some game when game.GameId = bootGameId -> loadReplay game loadedSession
                | _ -> false
              if hasSavedReplay then
                replayExtent <- max 1 loadedSession.KeyLog.EndFrame
                loadedSession.StartReplay()
                replaying <- true
              else
                replaying <- false
              startGame ()
              if hasSavedReplay then
                statusText.Text <- sprintf "%s: replaying %d saved key events from frame 0" currentGame.Value.Name loadedSession.KeyLog.Count
            with ex ->
              statusText.Text <- "boot failed: " + ex.Message
          | _ -> ()
      | Some s ->
        saveReplay false
        if not rewinding then
          rewindSlider.Maximum <- float (if replaying then replayExtent else max 0 s.History.LastFrame)
          rewindSlider.Value <- float s.Frame
        updateTimeLabel s.Frame
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
    manualTimer.Start()

    // Launch the manifest-selected default game.
    launchGame startupGame
    loadControlForGame ()

    self.Closed.Add(fun _ ->
      running <- false
      saveReplay true
      frameTimer.Stop()
      cinemaTimer.Stop()
      manualTimer.Stop()
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
