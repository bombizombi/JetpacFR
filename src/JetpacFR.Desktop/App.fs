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
/// when enabled the beeper waveform is scaled down (volume, below) before the
/// 16-bit PCM conversion.
module Audio =
  let private waveOut = new WaveOutEvent()
  let private provider = new BufferedWaveProvider(WaveFormat(44100, 16, 1))

  do
    provider.DiscardOnBufferOverflow <- true
    waveOut.Init provider
    waveOut.Play()

  /// Muted by default; the user explicitly enables sound.
  let mutable Enabled = false
  /// Volume: the beeper waveform (+-0.8) is scaled down before the 16-bit
  /// PCM conversion. Tuned to 0.2 after listening tests (0.1 was too quiet);
  /// the tooltip stays in sync with this value.
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
    /// Optional row background: the active brush tint when the address is
    /// inside the selected code. Null = transparent.
    Tint: Brush
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
  /// When the current bootTask was launched - drives the status-bar progress.
  let mutable bootStartedAt = System.DateTime.UtcNow
  let mutable manualBoot: ManualBoot option = None
  /// Latest frame snapshot from a running background boot: (screen, frame).
  let mutable bootScreen: byte[] option = None
  let mutable bootFrameNo = 0
  /// Background-boot hook: keep the latest screen snapshot so the status
  /// view can render the tape loading live.
  let recordBootFrame (screen: byte[], frameNo: int) =
    bootScreen <- Some screen
    bootFrameNo <- frameNo
  let mutable ceGame: CEGame option = None
  let mutable engineCE = false
  let mutable currentGame: GameManifest option = None
  let mutable gamesList: GameManifest list = []
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
  let gfxBmp = WriteableBitmap(256, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
  let heatPixels = Array.zeroCreate<byte> (256 * 256 * 4)
  let stripPixels = Array.zeroCreate<byte> (512 * 24 * 4)
  let gfxPixels = Array.zeroCreate<byte> (256 * 256 * 4)

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
  let statsTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 500.0)

  // ---- controls ----------------------------------------------------------
  let gameImage = Image()
  let heatImage = Image()
  let stripImage = Image()
  let gfxImage = Image()
  /// Graphics view: base address of the 8 KB window being rendered.
  let mutable gfxBase = 0x4000
  /// Graphics view: row width in pixels. Memory is read byte for byte;
  /// width/8 consecutive bytes are drawn side by side on one bitmap row,
  /// then drawing steps down a row; at the bitmap bottom it wraps to the
  /// top of the next row-width column.
  let mutable gfxRowWidth = 8
  let gfxSlider =
    Slider(
      Minimum = 0.0, Maximum = 65535.0,
      SmallChange = 8.0, LargeChange = 128.0,
      IsMoveToPointEnabled = true)
  let gfxAddrLabel = TextBlock()
  let gfxWidthBox = TextBox(Width = 44.0)
  let gfxWidthDown = Button(Content = "-8", Width = 32.0)
  let gfxWidthUp = Button(Content = "+8", Width = 32.0)
  let disasmList = ListBox()
  let slider = Slider()
  let cursorLabel = TextBlock()
  /// The address the code window is centered on (memCursor or trace PC).
  let disasmTarget = TextBlock()
  let statusText = TextBlock()
  let timelineFingerprint (game: GameManifest) : TimelineFingerprint =
    let fp = ReplayStore.fingerprint game
    { GameId = game.GameId
      RomSha256 = fp.RomSha256
      TzxSha256 = fp.TzxSha256
      ProgramSha256 = fp.ProgramSha256
      ProgramAddress = fp.ProgramAddress }

  let timelinePath (game: GameManifest) =
    Path.Combine(game.GameDirectory, StateTimelineStore.FileName)

  /// Persist states + key log. Writes ONLY when the timeline actually
  /// changed since the last save (captures, branch truncation, key events);
  /// `force` (the Save button) rewrites even an unchanged recording. Called
  /// on window close and game switch - a recording grows by megabytes per
  /// second, so there is deliberately no autosave on pause or scrub. A
  /// trivial timeline (fresh boot or just cleared) is never written.
  let saveTimeline (force: bool) =
    match currentGame, session with
    | Some game, Some s
      when (force || s.TimelineDirty) && s.StateTimeline.Count > 1 ->
      match StateTimelineStore.save (timelinePath game) (timelineFingerprint game) s.StateTimeline s.KeyLog.Events with
      | Ok () ->
        s.ClearTimelineDirty ()
        statusText.Text <-
          sprintf "timeline saved: %d frames, %.1f MB" s.StateTimeline.Count (float s.StateTimeline.BytesUsed / (1024.0 * 1024.0))
      | Error error -> statusText.Text <- sprintf "timeline save failed for %s: %s" game.Name error
    | _ -> ()

  /// Install a saved recording before a single frame runs: the key script
  /// drives autoplay and every frame - future included - is seekable.
  let loadTimeline (game: GameManifest) (s: TraceSession) : bool =
    match StateTimelineStore.tryLoad (timelinePath game) (timelineFingerprint game) with
    | TimelineLoaded (timeline, events) ->
      s.LoadStateTimeline(timeline, events)
      if not (List.isEmpty events) then
        statusText.Text <- sprintf "%s: loaded %d frames, %d key events" game.Name timeline.Count events.Length
      timeline.Count > 1
    | TimelineMissing -> false
    | TimelineIgnored reason ->
      statusText.Text <- sprintf "%s: saved timeline ignored (%s)" game.Name reason
      false


  /// Start a game from its manifest: warm cache -> fast port session; cold
  /// auto -> oracle boot (slow, first run); cold manual -> the user runs the
  /// loader in the oracle and marks the entry point.
  let launchGame (m: GameManifest) =
    saveTimeline false
    bootStartedAt <- System.DateTime.UtcNow
    bootScreen <- None
    bootFrameNo <- 0
    manualBoot <- None
    manualTimer.Stop()
    currentGame <- Some m
    bootGameId <- m.GameId
    match EntryCache.tryLoad m.Rom m.Tzx with
    | Some _ ->
      bootTask <- Some(System.Threading.Tasks.Task.Run(fun () ->
        TraceSession(m.Rom, m.Tzx, 4_000_000, onFrame = recordBootFrame)))
      statusText.Text <- sprintf "booting %s (cached entry state)..." m.Name
    | None when m.Boot = "auto" ->
      bootTask <- Some(System.Threading.Tasks.Task.Run(fun () ->
        TraceSession(m.Rom, m.Tzx, 4_000_000, onFrame = recordBootFrame)))
      statusText.Text <- sprintf "booting %s to game entry (first run, slow)..." m.Name
    | None when m.Boot = "program" ->
      // A raw Z80 image: seed the entry cache from the bin, then boot the
      // port session warm (the cache is keyed on the image file).
      match m.ProgramBin, m.ProgramAddress with
      | Some bin, Some addr ->
        try
          ProgramEntry.seed bin addr
          bootTask <- Some(System.Threading.Tasks.Task.Run(fun () ->
            TraceSession(bin, bin, 4_000_000, onFrame = recordBootFrame)))
          statusText.Text <- sprintf "booting %s (program image at 0x%04X)..." m.Name addr
        with ex ->
          statusText.Text <- sprintf "cannot load %s's program image: %s" m.Name ex.Message
      | _ -> statusText.Text <- sprintf "%s: manifest is missing program.bin/address" m.Name
    | None ->
      bootTask <- None
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
  /// Recording stats (timeline size, rates), refreshed twice a second.
  let recStatsLabel = TextBlock(Foreground = dim, VerticalAlignment = VerticalAlignment.Center)
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
  /// Alternate IDA-style block/graph rendering of the memory view.
  let mutable disasmModeGraph = false
  /// Graph-view expand toggle: show ALL blocks instead of the active
  /// brush selection filter.
  let mutable disasmGraphAll = false
  let mutable memCursor = 0x8000
  /// Why the cursor sits at its address (the last jump site that set it) -
  /// reported alongside a code-DESYNC warning, where the linear sweep can't
  /// reach the cursor as an instruction start.
  let mutable memCursorReason = "session start"
  /// Top address of the memory-view window. None (or out of range) means
  /// "recenter on memCursor". The wheel moves this anchor, so the
  /// highlighted cursor instruction stays on its address and scrolls with
  /// the content instead of being pinned to the middle row.
  let mutable memViewTop: int option = None
  let gotoMemCursor (addr: int) (reason: string) =
    memCursor <- addr
    memCursorReason <- reason
    memViewTop <- None // jumps recenter the window on the new cursor
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
  // RadioButtons (one group) instead of ToggleButtons: clicking the active
  // mode keeps it checked, so the view can never fall into a ghost mode with
  // no button highlighted.
  let memModeBtn = RadioButton(Content = "Memory", Width = 68.0, GroupName = "codeview", Foreground = normal)
  let execModeBtn = RadioButton(Content = "Execution", Width = 76.0, GroupName = "codeview", IsChecked = Nullable<bool>(true), Foreground = normal)
  let graphModeBtn = RadioButton(Content = "Graph", Width = 60.0, GroupName = "codeview", Foreground = normal)
  let graphAllBtn = ToggleButton(Content = "All", Width = 44.0, Foreground = normal, ToolTip = "Graph view: ignore the brush selection and show all blocks")
  /// Code graph view surfaces (alternate rendering of the memory view).
  /// Separate from the call graph's graphCanvas: a canvas can only be
  /// hosted by one parent and each view Clears its own surface.
  let graphScroll = ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
  let codeGraphCanvas = Canvas(Background = SolidColorBrush(Color.FromRgb(0x18uy, 0x18uy, 0x20uy)))
  /// Call graph surface (Functions tab, drawGraph).
  let graphCanvas = Canvas(Background = SolidColorBrush(Color.FromRgb(0x18uy, 0x18uy, 0x20uy)))
  /// Shared comment text for all mass-comment buttons.
  let commentBox = TextBox(Width = 420.0, Height = 22.0)
  let idxCmtBtn = Button(Content = "idx -> cmt", ToolTip = "Comment the current instruction index (execution trace)")
  let lineCmtBtn = Button(Content = "addr -> cmt", ToolTip = "Comment the current address: the cursor in memory/graph view, or the current instruction's PC in execution view (text from the comment box)")
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

  /// When set, the next switch to memory view centers on this address
  /// instead of the execution view's current PC ("sync to this instruction").
  let mutable pendingMemCursor: int option = None

  /// Right-click menu for a code row (list or graph surface): add a Line
  /// comment at the address (text from the shared comment box), edit or
  /// clear it, add an exec comment for the log occurrence, copy the address,
  /// or switch to memory view synced to this instruction. `refresh`
  /// re-renders whichever code surface is showing. `target` anchors the
  /// popup - without a placement target an IsOpen menu never displays.
  let openRowMenu (target: FrameworkElement) (addr: int) (instrIdx: int) (refresh: unit -> unit) =
    let hasComment =
      match control with
      | Some c -> ControlFile.commentAt c addr |> Option.isSome
      | None -> false
    let menu = ContextMenu()
    let mk header act =
      let mi = MenuItem(Header = header)
      mi.Click.Add(fun _ -> act ())
      menu.Items.Add mi |> ignore
    let setText (m: ControlComment) =
      match control with
      | Some c ->
        control <-
          (if m.Kind = Line then Some (ControlFile.addLine c m.Addr m.Text)
           else Some (ControlFile.upsert c m))
        refresh ()
        statusText.Text <- sprintf "comment at %04X" addr
      | None -> statusText.Text <- "no control file - click New ctrl first"
    mk (sprintf "add comment at %04X" addr) (fun _ ->
      setText { Kind = Line; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = "" })
    if instrIdx >= 0 then
      mk (sprintf "exec comment at instruction #%d" instrIdx) (fun _ ->
        setText { Kind = Exec; Addr = 0; EndExcl = 0; InstrIndex = instrIdx; Text = "" })
    if hasComment then
      mk (sprintf "edit comment at %04X (loads it into the comment box)" addr) (fun _ ->
        match control with
        | Some c ->
          commentBox.Text <- ControlFile.commentAt c addr |> Option.defaultValue ""
          commentBox.Focus () |> ignore
          commentBox.SelectAll ()
          statusText.Text <- sprintf "editing %04X - re-apply with 'add comment' when done" addr
        | None -> ())
      mk (sprintf "clear comment at %04X" addr) (fun _ ->
        match control with
        | Some c ->
          control <- Some(ControlFile.upsert c { Kind = Line; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = "" })
          refresh ()
          statusText.Text <- sprintf "comment cleared at %04X" addr
        | None -> ())
    if not disasmModeMemory then
      mk "switch to memory view synced to this instruction" (fun _ ->
        pendingMemCursor <- Some addr
        if memModeBtn.IsChecked <> Nullable<bool>(true) then memModeBtn.IsChecked <- Nullable<bool>(true)
        else
          (match pendingMemCursor with Some a -> memCursor <- a | None -> ())
          pendingMemCursor <- None
          refresh ())
    mk (sprintf "copy address %04X" addr) (fun _ ->
      System.Windows.Clipboard.SetText(sprintf "0x%04X" addr)
      statusText.Text <- sprintf "copied %04X" addr)
    menu.PlacementTarget <- target
    menu.Placement <- PlacementMode.MousePoint
    menu.IsOpen <- true

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
    | Some s -> max 1L (int64 (max s.ReplayEndFrame s.TimelineExtent))
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
        // New control files cover all of RAM; the block map starts empty
        // and is filled in on the map. (The old timeline-derived default
        // produced bogus 50-byte spans like [100,150).)
        control <- Some(ControlFile.loadOrCreate dir (0x4000, 0x10000))
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
        rows.Add { Tag = tag; Brush = (if isErr then red else normal); Tint = null; IsCurrent = isErr; Addr = addr; InstrIdx = -1 }
        addr <- (addr + insn.Length) &&& 0xFFFF
      disasmList.ItemsSource <- rows
      if disasmModeGraph then
        // the fault listing is a memory view; show the list pane over the
        // graph surface until the next mode toggle rebuilds
        graphScroll.Visibility <- Visibility.Collapsed
        disasmList.Visibility <- Visibility.Visible
      disasmTarget.Text <- sprintf "target: 0x%04X (fault site)" pc
    | None -> ()
  /// MM:SS of play time at 50 fps (frames / 50), shown as current / total:
  /// the total includes frames loaded from the saved recording, which are
  /// seekable before they execute.
  let updateTimeLabel (frames: int) =
    let curSeconds = frames / 50
    let totalSeconds = int (timelineExtent ()) / 50
    timeLabel.Text <-
      sprintf "%02d:%02d / %02d:%02d"
        (curSeconds / 60) (curSeconds % 60) (totalSeconds / 60) (totalSeconds % 60)

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
        Tint = null
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
        Tint = null
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

  /// Active-brush tints for the code view (match the timeline chip colors).
  let tintA = SolidColorBrush(Color.FromArgb(0x30uy, 0x38uy, 0xBDuy, 0xF8uy))
  let tintB = SolidColorBrush(Color.FromArgb(0x30uy, 0xFBuy, 0xBFuy, 0x24uy))

  /// The address set of the ACTIVE brush: PCs executed inside the brush's
  /// frame range. None when there is no trace yet or the range is empty.
  /// Cached against (brush, range, trace): pcsInFrames scans EVERY trace
  /// entry, so recomputing it per row made the graph view freeze the UI
  /// thread (rows x trace-entries iterations).
  let mutable activeSelCache: (BrushId * int * int * Trace * (BrushId * Set<int>) option) option = None
  let activeSelection () : (BrushId * Set<int>) option =
    match currentTrace () with
    | Some t when t.Entries.Length > 0 ->
      let which = timeline.ActiveBrush
      let fA, fB = rangeOf which
      match activeSelCache with
      | Some (w, a, b, t2, v) when w = which && a = fA && b = fB && obj.ReferenceEquals(t2, t) -> v
      | _ ->
        let pcs = ControlFile.pcsInFrames t fA fB
        let v = if List.isEmpty pcs then None else Some (which, Set.ofList pcs)
        activeSelCache <- Some (which, fA, fB, t, v)
        v
    | _ -> None

  /// Basic blocks for the code/graph views. The span is the bounding box
  /// of the executed region and the brush selection, plus any control-file
  /// code blocks that overlap them. (A control file whose code blocks do
  /// not overlap the executed region is a stale template and is ignored -
  /// the whole chosen span decodes as code; the splitter's resync handles
  /// embedded data.) Cached: a decode sweep is a few ms and the pane
  /// refreshes at 10 Hz.
  let mutable flowCache: Z80Flow.Block list option = None
  let mutable flowCacheAt = DateTime.MinValue
  let flowBlocks () : Z80Flow.Block list =
    if flowCache.IsNone || (DateTime.Now - flowCacheAt).TotalMilliseconds > 1000.0 then
      match currentMemory () with
      | None -> flowCache <- Some []
      | Some mem ->
        let counts = currentCounts ()
        let executed = [ for a in 0 .. 0xFFFF do if counts[a] > 0 then a ]
        let execBox =
          if List.isEmpty executed then None
          else Some (List.min executed, List.max executed + 1)
        let selBox =
          match activeSelection () with
          | Some (_, s) when not (Set.isEmpty s) -> Some (Set.minElement s, Set.maxElement s + 1)
          | _ -> None
        let controlBox =
          match control with
          | Some c ->
            let codes = c.Blocks |> List.filter (fun b -> b.Kind = Code)
            if codes.IsEmpty then None
            else Some ((codes |> List.minBy (fun b -> b.Start)).Start,
                       (codes |> List.maxBy (fun b -> b.EndExcl)).EndExcl)
          | None -> None
        let overlaps (a1, b1) (a2, b2) = a1 < b2 && a2 < b1
        let useControl =
          match controlBox with
          | None -> false
          | Some cb ->
            (match execBox with Some eb -> overlaps cb eb | None -> false)
            || (match selBox with Some sb -> overlaps cb sb | None -> false)
        let present =
          [ execBox; selBox; (if useControl then controlBox else None) ]
          |> List.choose id
        let start, endExcl =
          if present.IsEmpty then 0x4000, 0x10000
          else
            max 0x4000 ((present |> List.map fst |> List.min) - 0x200),
            min 0x10000 ((present |> List.map snd |> List.max) + 0x200)
        let start, endExcl = if start < endExcl then start, endExcl else 0x4000, 0x10000
        flowCache <- Some (Z80Flow.splitBlocks mem start endExcl (fun _ -> true))
      flowCacheAt <- DateTime.Now
    flowCache |> Option.defaultValue []

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
            Tint = null
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
  /// Foreground for the cursor row: hiBg (the IsCurrent row background) is
  /// nearly black, so the row needs its own bright text color to stay legible.
  let bright = SolidColorBrush(Color.FromRgb(0xECuy, 0xEFuy, 0xF8uy))

  /// One row of the linear memory sweep. The active-brush selection is
  /// passed in by the caller (computed once per refresh, never per row).
  let memRowFor (mem: byte[]) (addr: int) (sel: (BrushId * Set<int>) option) : DisasmRow =
    let insn = Disasm.disasmMemory mem addr
    let hex =
      [ for i in 0 .. insn.Length - 1 -> sprintf "%02X" mem[(addr + i) &&& 0xFFFF] ]
      |> String.concat " "
    let comment =
      match control with
      | Some c -> ControlFile.commentAt c addr |> Option.defaultValue ""
      | None -> ""
    let cm = if comment <> "" then sprintf "  ; %s" comment else ""
    let tint =
      match sel with
      | Some (which, s) when s.Contains addr ->
        (match which with A -> tintA :> Brush | B -> tintB :> Brush)
      | _ -> null
    { Tag = sprintf "  %04X  %-11s  %s%s" addr hex insn.Text cm
      Brush =
        if addr = memCursor then bright
        elif (currentCounts())[addr] > 0 then cyan
        else normal
      Tint = tint
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
  /// Graph view (plan_code_graph): blocks as boxes in address order,
  /// arrows routed on right rails (taken = green, jump = cyan, call =
  /// orange), back edges marked by an upward arrowhead. No virtualization:
  /// every block decodes its rows into plain TextBlocks on a Canvas.
  /// Skipped entirely when nothing it depends on changed: refreshDisasm
  /// also runs off timers (cinema at 30 ms), and re-creating thousands of
  /// TextBlocks per tick would wedge the UI thread.
  let mutable lastGraphMem: byte[] option = None
  let mutable lastGraphSel: (BrushId * Set<int>) option option = None
  let mutable lastGraphAll = false
  let mutable lastGraphControl: ControlFile option = None
  let mutable lastGraphEpoch = (-1, -1)
  /// `onRowMenu` opens the row context menu for an address (injected by
  /// refreshDisasm, which owns the refresh path).
  let buildGraph (onRowMenu: int -> unit) =
    let sel = if disasmGraphAll then None else activeSelection ()
    let mem = currentMemory ()
    // mem is the same array instance for a whole session, so instance
    // equality alone never sees content changes (self-modifying code, tape
    // loads) and the graph went permanently stale. Pair it with a cheap
    // session epoch; loaded traces are immutable, so they stay instance-keyed.
    let epoch =
      match loaded, session with
      | Some _, _ -> (-1, -1)
      | None, Some s -> (s.Frame, s.Recorder.SelfModCount)
      | None, None -> (-1, -1)
    if obj.ReferenceEquals(mem, lastGraphMem)
       && lastGraphSel = Some sel
       && lastGraphAll = disasmGraphAll
       && obj.ReferenceEquals(control, lastGraphControl)
       && epoch = lastGraphEpoch then
      ()
    else
      lastGraphMem <- mem
      lastGraphSel <- Some sel
      lastGraphAll <- disasmGraphAll
      lastGraphControl <- control
      lastGraphEpoch <- epoch
      codeGraphCanvas.Children.Clear()
      Mouse.OverrideCursor <- Cursors.Wait
      try
        match mem with
        | None -> disasmTarget.Text <- "target: ----"
        | Some mem ->
          let blocks =
            match sel with
            | Some (_, s) -> flowBlocks () |> List.filter (fun b -> Set.exists (fun a -> a >= b.Start && a < b.EndExcl) s)
            | None -> flowBlocks ()
          let blockW, headerH, rowH, gap = 560.0, 18.0, 15.0, 24.0
          let leftX = 8.0
          let railRight = leftX + blockW
          let tops = ResizeArray<float>()
          let bottoms = ResizeArray<float>()
          let blockIndex = System.Collections.Generic.Dictionary<int, int>()
          blocks |> List.iteri (fun i b -> blockIndex[b.Start] <- i)
          let mutable y = 8.0
          for b in blocks do
            tops.Add y
            let mutable a = b.Start
            let mutable n = 0
            while a < b.EndExcl do
              n <- n + 1
              a <- a + max 1 (Disasm.disasmMemory mem a).Length
            y <- y + headerH + float n * rowH + 4.0
            bottoms.Add y
            y <- y + gap
          codeGraphCanvas.Height <- max y 60.0
          codeGraphCanvas.Width <- railRight + 130.0
          // blocks
          for i in 0 .. blocks.Length - 1 do
            let b = blocks[i]
            let name =
              match control with
              | Some c -> ControlFile.blockAt c b.Start |> Option.map (fun bl -> bl.Name) |> Option.defaultValue ""
              | None -> ""
            let p = StackPanel()
            let header =
              TextBlock(
                Text = sprintf "  %04X  %s" b.Start name,
                FontFamily = mono, FontSize = 11.5, Foreground = cyan,
                Background = hiBgBrush, Height = headerH - 2.0)
            p.Children.Add header |> ignore
            let mutable a = b.Start
            while a < b.EndExcl do
              let row = memRowFor mem a sel
              let tb =
                TextBlock(
                  Text = row.Tag, FontFamily = mono, FontSize = 11.5,
                  Foreground = row.Brush, Background = row.Tint, Height = rowH)
              let addr = a
              tb.MouseLeftButtonDown.Add(fun _ ->
                gotoMemCursor addr "graph view row"
                disasmTarget.Text <- sprintf "target: 0x%04X (graph view)" addr)
              tb.MouseRightButtonDown.Add(fun e ->
                e.Handled <- true
                onRowMenu addr)
              p.Children.Add tb |> ignore
              a <- a + max 1 (Disasm.disasmMemory mem a).Length
            let border = Border(BorderBrush = dim, BorderThickness = Thickness(1.0), Child = p)
            Canvas.SetLeft(border, leftX)
            Canvas.SetTop(border, tops[i])
            codeGraphCanvas.Children.Add border |> ignore
          // edges
          let edgeColor =
            function
            | Z80Flow.Branch _ -> green
            | Z80Flow.Jump _ -> cyan
            | Z80Flow.Call _ -> orange
            | _ -> dim
          let addElbow (x1: float) (y1: float) (x2: float) (y2: float) (rail: float) (color: Brush) =
            let pl = Polyline(Stroke = color, StrokeThickness = 1.2)
            for (px, py) in [ x1, y1; rail, y1; rail, y2; x2, y2 ] do
              pl.Points.Add (Point(px, py))
            codeGraphCanvas.Children.Add pl |> ignore
            let head = Polygon(Fill = color, Points = PointCollection())
            let d = if y2 >= y1 then 5.0 else -5.0
            head.Points.Add (Point(x2 - 3.5, y2 - d))
            head.Points.Add (Point(x2 + 3.5, y2 - d))
            head.Points.Add (Point(x2, y2))
            codeGraphCanvas.Children.Add head |> ignore
          let mutable railSlot = 0
          for i in 0 .. blocks.Length - 1 do
            let b = blocks[i]
            match b.Ends with
            | Z80Flow.Linear -> ()
            | Z80Flow.Return -> ()
            | Z80Flow.Jump None | Z80Flow.Branch None -> ()
            | kind ->
              let target =
                match kind with
                | Z80Flow.Jump (Some t) | Z80Flow.Branch (Some t) | Z80Flow.Call (Some t) -> t
                | _ -> -1
              if target >= 0 && blockIndex.ContainsKey target then
                let j = blockIndex[target]
                let color = edgeColor kind
                let fromY = bottoms[i]
                let toY = tops[j] + 9.0
                if j = i + 1 then
                  // adjacent fall-through: short straight arrow
                  let l = System.Windows.Shapes.Line(X1 = leftX + blockW / 2.0, Y1 = fromY, X2 = leftX + blockW / 2.0, Y2 = toY, Stroke = color, StrokeThickness = 1.2)
                  codeGraphCanvas.Children.Add l |> ignore
                  addElbow (leftX + blockW / 2.0) fromY (leftX + blockW / 2.0) toY (leftX + blockW / 2.0) color
                else
                  let rail = railRight + 12.0 + 14.0 * float (railSlot % 6)
                  railSlot <- railSlot + 1
                  addElbow railRight fromY leftX toY rail color
          disasmTarget.Text <-
            match sel with
            | Some (which, _) -> sprintf "graph view: %d blocks (brush %s)" blocks.Length (match which with A -> "A" | B -> "B")
            | None -> sprintf "graph view: %d blocks" blocks.Length
      finally
        Mouse.OverrideCursor <- null

  // ---- code-window scrolling ----------------------------------------------
  // The list's internal ScrollViewer is discovered lazily from the visual
  // tree (ListBox.ScrollHost is protected). It is null until the window has
  // been laid out once; each setRows call retries.
  let mutable disasmScroll: ScrollViewer option = None
  let findScroll () =
    match disasmScroll with
    | Some s -> Some s
    | None ->
      let rec walk (d: DependencyObject) : ScrollViewer option =
        match d with
        | :? ScrollViewer as sv -> Some sv
        | _ ->
          let n = VisualTreeHelper.GetChildrenCount d
          let mutable found = None
          let mutable i = 0
          while found.IsNone && i < n do
            found <- walk (VisualTreeHelper.GetChild(d, i))
            i <- i + 1
          found
      let sv = walk disasmList
      disasmScroll <- sv
      sv

  /// Swap the code window's rows while keeping the scroll offset stable.
  /// Assigning a fresh ItemsSource regenerates every container and resets the
  /// ScrollViewer to the top; refreshDisasm runs off the 30 ms cinema timer
  /// and every scrub interaction, which made any scroll attempt snap straight
  /// back. Same row count => same window shape, so re-apply the old offset
  /// once the new containers are laid out (clamped by the ScrollViewer).
  let setRows (rows: ResizeArray<DisasmRow>) =
    let sv = findScroll ()
    let oldOffset = sv |> Option.map (fun s -> s.VerticalOffset) |> Option.defaultValue 0.0
    let oldCount = disasmList.Items.Count
    disasmList.ItemsSource <- rows
    if oldCount = rows.Count && oldOffset > 0.0 then
      sv |> Option.iter (fun s ->
        disasmList.Dispatcher.BeginInvoke(
          DispatcherPriority.Loaded,
          Action(fun () -> s.ScrollToVerticalOffset oldOffset))
        |> ignore)

  let applyViewMode () =
    disasmList.Visibility <- if disasmModeGraph then Visibility.Collapsed else Visibility.Visible
    graphScroll.Visibility <- if disasmModeGraph then Visibility.Visible else Visibility.Collapsed

  /// Frame number (1-based) of the trace entry at `idx`, derived from the
  /// recorded frame-boundary ticks: boundaries are the end-of-frame cycle
  /// counts, so an entry belongs to the frame after the last boundary at or
  /// before its start tick. 0 when the trace carries no boundaries.
  let frameOfEntry (t: Trace) (idx: int) : int =
    if t.FrameTicks.Length = 0 then 0
    else
      let tick = t.Entries[idx].Tick
      let mutable f = 0
      while f < t.FrameTicks.Length && t.FrameTicks[f] <= tick do f <- f + 1
      f + 1

  let rec refreshDisasm () =
    if disasmModeGraph then
      // the graph build can own the thread for minutes on wide spans; show
      // the busy cursor for its duration
      let prevCursor = Mouse.OverrideCursor
      Mouse.OverrideCursor <- Cursors.Wait
      // pump one Render-priority pass so the busy cursor actually shows
      // before the build wedges the thread; queued input sits at a lower
      // priority, so nothing re-enters mid-build
      Dispatcher.CurrentDispatcher.Invoke(
        DispatcherPriority.Render, System.Action(fun () -> ())) |> ignore
      try
        buildGraph (fun addr -> openRowMenu codeGraphCanvas addr -1 refreshDisasm)
      finally
        Mouse.OverrideCursor <- prevCursor
    elif disasmModeMemory then
      match currentMemory () with
      | None ->
        disasmList.ItemsSource <- null
        disasmTarget.Text <- "target: ----"
      | Some mem ->
        match activeSelection () with
        | Some (which, sel) ->
          // Selected blocks: every block touching the selection, decoded
          // in full, tinted where the address is selected. Scrollable.
          let blocks =
            flowBlocks ()
            |> List.filter (fun b -> Set.exists (fun a -> a >= b.Start && a < b.EndExcl) sel)
          let rows = ResizeArray<DisasmRow>()
          for b in blocks do
            let mutable a = b.Start
            while a < b.EndExcl do
              rows.Add(memRowFor mem a (Some (which, sel)))
              a <- a + max 1 (Disasm.disasmMemory mem a).Length
          setRows rows
          let lo = blocks |> List.map (fun b -> b.Start) |> function [] -> 0x10000 | xs -> List.min xs
          let hi = blocks |> List.map (fun b -> b.EndExcl) |> function [] -> 0 | xs -> List.max xs
          disasmTarget.Text <-
            sprintf "target: 0x%04X (memory view, brush %s: %d blocks 0x%04X-0x%04X)" memCursor
              (match which with A -> "A" | B -> "B") blocks.Length lo hi
        | None ->
          let rows = ResizeArray<DisasmRow>()
          // Recentre (only when the anchor is unset, i.e. after a jump):
          // decode forward from a floor below the cursor until a boundary
          // sequence lands exactly on it, then start the window 20
          // boundaries back - the cursor row is a true instruction start
          // and the block above it is flush. If no alignment reaches the
          // cursor, it sits mid-instruction (a jump into the middle, or
          // bytes shifted by self-modification): a code DESYNC - the window
          // parks on the cursor and the target line says so.
          let memTop =
            match memViewTop with
            | Some t when t >= 0 && t < 0x10000 -> t
            | _ ->
              let floor = max 0 (memCursor - 0xA0)
              let starts =
                [ floor - 4; floor - 3; floor - 2; floor - 1; floor ]
                |> List.filter (fun s -> s >= 0)
              let candidate =
                starts |> List.tryPick (fun start ->
                  let bounds = ResizeArray<int>()
                  let mutable a = start
                  let mutable aligned = false
                  while a < memCursor do
                    let next = a + max 1 (Disasm.disasmMemory mem a).Length
                    if next = memCursor then aligned <- true
                    if next <= memCursor then bounds.Add a
                    a <- next
                  if aligned && bounds.Count >= 20 then Some bounds.[bounds.Count - 20]
                  elif aligned && bounds.Count > 0 then Some bounds.[0]
                  elif aligned then Some memCursor
                  else None)
              match candidate with
              | Some t -> t
              | None -> memCursor
          memViewTop <- Some memTop
          let mutable cur = memTop
          while rows.Count < 41 do
            rows.Add(memRowFor mem cur None)
            cur <- (cur + (Disasm.disasmMemory mem cur).Length) &&& 0xFFFF
          setRows rows
          // DESYNC probe, independent of the window: is the cursor itself an
          // instruction start in the linear decode around it?
          let alignedAt (a: int) =
            if a <= 0 then true
            else
              let floor = max 0 (a - 0x44)
              [ floor - 4; floor - 3; floor - 2; floor - 1; floor ]
              |> List.filter (fun s -> s >= 0)
              |> List.exists (fun start ->
                let mutable p = start
                let mutable hit = false
                while p < a do
                  let next = p + max 1 (Disasm.disasmMemory mem p).Length
                  if next = a then hit <- true
                  p <- next
                hit)
          let desync = memCursor > 0 && not (alignedAt memCursor)
          disasmTarget.Text <-
            if desync then
              sprintf
                "target: 0x%04X (memory view) - code DESYNC detected: cursor is mid-instruction in this sweep (cursor set by: %s)"
                memCursor memCursorReason
            else
              sprintf "target: 0x%04X (memory view)" memCursor
    else
      match currentTrace () with
      | None ->
        disasmList.ItemsSource <- null
        disasmTarget.Text <- "target: ----"
      | Some t ->
        if t.Entries.Length = 0 then
          disasmList.ItemsSource <- null
          disasmTarget.Text <- "target: ----"
        else
          let c = max 0 (min cursor (t.Entries.Length - 1))
          let rows = ResizeArray<DisasmRow>()
          let mutable lastIdx = -1
          for j in -20 .. 20 do
            let idx = c + j
            if idx >= 0 && idx < t.Entries.Length then
              // Timeline jumps leave tick gaps in the log; mark the seam so
              // the discontinuity reads as a landmark, not corrupted order.
              if idx > 0 && int t.Entries[idx].Tick - int t.Entries[idx - 1].Tick > 2 * 69888 then
                rows.Add
                  { Tag = "  ---------- jump ----------"
                    Brush = red
                    Tint = null
                    IsCurrent = false
                    Addr = -1
                    InstrIdx = -1 }
              rows.Add(rowFor t t.Entries[idx] idx (idx = c))
              lastIdx <- idx
          // When the window reaches the log's end, make the boundary
          // explicit: seeks into the recorded future pin here because
          // nothing has executed past this point yet.
          if lastIdx = t.Entries.Length - 1 then
            let extent =
              if t.FrameTicks.Length > 0 then sprintf "frame %d" t.FrameTicks.Length
              else sprintf "%d entries" t.Entries.Length
            rows.Add
              { Tag = sprintf "  ---------- end of executed log (%s) ----------" extent
                Brush = dim
                Tint = null
                IsCurrent = false
                Addr = -1
                InstrIdx = -1 }
          setRows rows
          let fr = frameOfEntry t c
          disasmTarget.Text <-
            if fr = 0 then sprintf "target: 0x%04X (instruction #%d)" (int t.Entries[c].Pc) c
            else sprintf "target: 0x%04X (instruction #%d frame %d)" (int t.Entries[c].Pc) c fr
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

  /// Show the live machine registers - the restored state after a frame
  /// seek. The trace-snapshot path in refreshRegs cannot reflect a future
  /// jump: no instruction has executed there, so no snapshot exists.
  let showLiveRegs (s: TraceSession) : int =
    let r = s.Regs
    setReg "AF" (int (r.Get Jetpac2.Core.R16.AF))
    setReg "BC" (int (r.Get Jetpac2.Core.R16.BC))
    setReg "DE" (int (r.Get Jetpac2.Core.R16.DE))
    setReg "HL" (int (r.Get Jetpac2.Core.R16.HL))
    setReg "AF'" (int (r.Get Jetpac2.Core.R16.AF_))
    setReg "BC'" (int (r.Get Jetpac2.Core.R16.BC_))
    setReg "DE'" (int (r.Get Jetpac2.Core.R16.DE_))
    setReg "HL'" (int (r.Get Jetpac2.Core.R16.HL_))
    setReg "IX" (int (r.Ix ()))
    setReg "IY" (int (r.Iy ()))
    setReg "SP" (int (r.Sp ()))
    setReg "I" (int (r.I ()))
    setReg "R" (int (r.R ()))
    let pc = (int (r.Pc ())) &&& 0xFFFF
    pcCell.Text <- sprintf "%04X" pc
    updateFlags (int (r.Get Jetpac2.Core.R16.AF))
    markerPc <- pc
    pc

  /// After a frame seek (slider drag / timeline scrub / preview): point the
  /// code window at the seeked state even though nothing executed. Memory
  /// mode centers on the restored PC; execution mode follows to the first
  /// logged entry at-or-after the frame, clamped to the trace end (a future
  /// frame has no entries of its own yet).
  let seekCodeView (s: TraceSession) (frame: int) =
    let pc = showLiveRegs s
    gotoMemCursor pc "frame seek"
    if not disasmModeMemory && not disasmModeGraph then
      match currentTrace () with
      | Some t when t.Entries.Length > 0 && t.FrameTicks.Length > 0 ->
        cursor <- entryAtFrame t (min frame (t.FrameTicks.Length - 1))
      | _ -> ()
    refreshDisasm ()
    timeline.Playhead <- int64 s.Frame

  let refreshHeatmap () =
    let counts = currentCounts ()
    let selfMod = currentSelfModified ()
    let maxC = max 1 (Array.max counts)
    let logMax = log10 (float maxC)
    let mutable i = 0
    for y in 0 .. 255 do
      for x in 0 .. 255 do
        let c = counts[i]
        // logMax is 0 when every executed PC ran exactly once; treat that
        // flat case as full heat instead of feeding NaN into the LUT index.
        let li =
          if c <= 0 then 0
          elif logMax <= 0.0 then 9
          else min 9 (int (9.0 * (log10 (float c) / logMax)))
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
    // Bucket by entry index, not Tick: Tick is absolute machine cycles
    // (~20 per instruction), so tick/n would blow past 511 a few percent
    // into the trace and pile everything into the last bucket.
    let mutable i = 0
    for e in t.Entries do
      segs[min 511 (i / n)] <- segs[min 511 (i / n)] + 1
      i <- i + 1
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

  // ---- graphics view --------------------------------------------------------
  // Sprite finder: draws an 8 KB window of memory starting at gfxBase,
  // read byte for byte. One byte = 8 horizontal pixels (MSB leftmost):
  // the first width/8 bytes sit side by side on bitmap row 0, the next
  // width/8 on row 1, and so on; at the bitmap bottom drawing wraps to
  // the top of the next row-width column. With the default 8 the bytes
  // simply march down the bitmap and step 8px right every 256 bytes, so
  // consecutive bytes form an unbroken vertical run and sprite data
  // reads as contiguous streaks instead of scatter. Black and white
  // only: 1-bits white, 0-bits black.
  let refreshGfx () =
    let baseAddr = gfxBase &&& 0xFFFF
    let w = max 8 (min 256 gfxRowWidth)
    let bytesPerRow = w / 8
    let blocks = 256 / w
    for p in 0 .. 4 .. gfxPixels.Length - 4 do
      gfxPixels[p] <- 0uy
      gfxPixels[p + 1] <- 0uy
      gfxPixels[p + 2] <- 0uy
      gfxPixels[p + 3] <- 255uy
    match currentMemory () with
    | Some mem ->
      for block in 0 .. blocks - 1 do
        for row in 0 .. 255 do
          for byteInRow in 0 .. bytesPerRow - 1 do
            let i = (block * 256 + row) * bytesPerRow + byteInRow
            let bits = mem[(baseAddr + i) &&& 0xFFFF]
            let px = block * w + byteInRow * 8
            for col in 0 .. 7 do
              if bits &&& (0x80uy >>> col) <> 0uy then
                let p = ((row * 256) + (px + col)) * 4
                gfxPixels[p] <- 0xFFuy
                gfxPixels[p + 1] <- 0xFFuy
                gfxPixels[p + 2] <- 0xFFuy
    | None -> ()
    gfxBmp.WritePixels(Int32Rect(0, 0, 256, 256), gfxPixels, 256 * 4, 0)
    let visible = blocks * bytesPerRow * 256
    let hi = (baseAddr + visible - 1) &&& 0xFFFF
    gfxAddrLabel.Text <- sprintf "%04Xh - %04Xh (%d bytes)" baseAddr hi visible

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
    factory.SetValue(TextBlock.BackgroundProperty, Binding("Tint"))
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

    // 3. engine radios + disasm mode toggles above the code window.
    // Two rows: packing both groups into one horizontal StackPanel made
    // the trailing code-view buttons clip out of view on narrower windows.
    let modeRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 2.0))
    let engLabel = TextBlock(Text = "engine:", Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(0.0, 0.0, 4.0, 0.0))
    modeRow.Children.Add engLabel |> ignore
    for r in [ oracleRadio :> FrameworkElement; ceRadio :> FrameworkElement; diffRadio :> FrameworkElement ] do
      r.Margin <- Thickness(0.0, 0.0, 10.0, 0.0)
      r.VerticalAlignment <- VerticalAlignment.Center
      modeRow.Children.Add r |> ignore
    DockPanel.SetDock(modeRow, Dock.Top)
    center.Children.Add modeRow |> ignore

    let viewRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 2.0, 0.0, 4.0))
    let modeLabel = TextBlock(Text = "code view:", Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(0.0, 0.0, 4.0, 0.0))
    viewRow.Children.Add modeLabel |> ignore
    memModeBtn.VerticalAlignment <- VerticalAlignment.Center
    execModeBtn.VerticalAlignment <- VerticalAlignment.Center
    for b in [ memModeBtn :> FrameworkElement; execModeBtn :> FrameworkElement; graphModeBtn :> FrameworkElement; graphAllBtn :> FrameworkElement ] do
      b.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
      viewRow.Children.Add b |> ignore
    DockPanel.SetDock(viewRow, Dock.Top)
    center.Children.Add viewRow |> ignore

    // 4. bottom: shared comment box + mass-comment buttons, then the
    //    disassembly target line (docked BEFORE the code window so the
    //    code fills the rest of the panel width)
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
    for b in [ idxCmtBtn :> FrameworkElement; lineCmtBtn :> FrameworkElement; nameBeforeBtn :> FrameworkElement; nameABtn :> FrameworkElement;
               nameBBtn :> FrameworkElement; cmtABtn :> FrameworkElement; cmtBBtn :> FrameworkElement;
               cmtBNotA :> FrameworkElement; cmtANotB :> FrameworkElement ] do
      b.Margin <- Thickness(0.0, 0.0, 6.0, 3.0)
      cmtBtnRow.Children.Add b |> ignore
    cmtBar.Children.Add cmtBtnRow |> ignore
    DockPanel.SetDock(cmtBar, Dock.Bottom)
    center.Children.Add cmtBar |> ignore
    disasmTarget.Foreground <- cyan
    disasmTarget.FontFamily <- mono
    disasmTarget.FontSize <- 12.0
    disasmTarget.Margin <- Thickness(0.0, 4.0, 0.0, 4.0)
    DockPanel.SetDock(disasmTarget, Dock.Bottom)
    center.Children.Add disasmTarget |> ignore

    // Comment edit pane (below the code pane): every comment attached to the
    // current line, most specific first. Line comments get editable boxes;
    // block (range/name) comments are shown read-only and marked. Enter on a
    // code line hands focus here.
    let cmtPaneBorder = Border(BorderBrush = dim, BorderThickness = Thickness(1.0), Margin = Thickness(0.0, 2.0, 0.0, 2.0))
    let cmtPaneInner = StackPanel(Margin = Thickness(4.0))
    let cmtPaneTitle = TextBlock(Text = "comments (click a code line or use arrows)", Foreground = dim, FontSize = 11.0, Margin = Thickness(0.0, 0.0, 0.0, 2.0))
    let cmtPaneRows = StackPanel()
    let cmtPaneAddBox = TextBox(Width = 300.0, Height = 20.0)
    let cmtPaneAddBtn = Button(Content = "add line comment", Width = 116.0)
    cmtPaneBorder.Child <- cmtPaneInner
    cmtPaneInner.Children.Add cmtPaneTitle |> ignore
    cmtPaneInner.Children.Add cmtPaneRows |> ignore
    let cmtPaneAddRow = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 2.0, 0.0, 0.0))
    cmtPaneAddRow.Children.Add cmtPaneAddBox |> ignore
    cmtPaneAddRow.Children.Add cmtPaneAddBtn |> ignore
    cmtPaneInner.Children.Add cmtPaneAddRow |> ignore
    DockPanel.SetDock(cmtPaneBorder, Dock.Bottom)
    center.Children.Add cmtPaneBorder |> ignore

    let mutable paneAddr: int option = None
    let mutable paneControl: obj = null
    let rec rebuildCommentPane (force: bool) =
      let controlRef = control |> Option.map box |> Option.defaultValue null
      if force || paneAddr <> Some memCursor || not (Object.ReferenceEquals(paneControl, controlRef)) then
        paneAddr <- Some memCursor
        paneControl <- controlRef
        cmtPaneRows.Children.Clear()
        cmtPaneTitle.Text <- sprintf "comments at %04X" memCursor
        match control with
        | None ->
          cmtPaneRows.Children.Add(TextBlock(Text = "no control file - click New ctrl first", Foreground = dim, FontSize = 11.0)) |> ignore
        | Some c ->
          let mkRow (label: string) (labelColor: Brush) (text: string) (readOnly: bool) (original: ControlComment option) =
            let row = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 1.0, 0.0, 1.0))
            let tag = TextBlock(Text = label, Foreground = labelColor, FontSize = 11.0, VerticalAlignment = VerticalAlignment.Center, MinWidth = 150.0)
            row.Children.Add tag |> ignore
            let box = TextBox(Text = text, Width = 300.0, Height = 20.0, IsReadOnly = readOnly)
            row.Children.Add box |> ignore
            match original with
            | Some m ->
              let commit () =
                control <- Some(ControlFile.replaceComment c m box.Text)
                refreshDisasm ()
                rebuildCommentPane true
              let apply = Button(Content = "apply", Width = 48.0, Margin = Thickness(4.0, 0.0, 0.0, 0.0))
              apply.Click.Add(fun _ -> commit ())
              let del = Button(Content = "del", Width = 38.0, Margin = Thickness(2.0, 0.0, 0.0, 0.0))
              del.Click.Add(fun _ ->
                control <- Some(ControlFile.removeComment c m)
                refreshDisasm ()
                rebuildCommentPane true)
              box.KeyDown.Add(fun e -> if e.Key = Key.Enter then commit ())
              row.Children.Add apply |> ignore
              row.Children.Add del |> ignore
            | None -> ()
            cmtPaneRows.Children.Add row |> ignore
          let lines = c.Comments |> List.filter (fun m -> m.Kind = Line && m.Addr = memCursor)
          let covering kind =
            c.Comments
            |> List.filter (fun m -> m.Kind = kind && m.Addr <= memCursor && memCursor < m.EndExcl)
            |> List.sortBy (fun m -> m.EndExcl - m.Addr)
          if List.isEmpty lines && List.isEmpty (covering Range) && List.isEmpty (covering Name) then
            cmtPaneRows.Children.Add(TextBlock(Text = "none - add one below", Foreground = dim, FontSize = 11.0)) |> ignore
          for m in lines do
            mkRow "line" normal m.Text false (Some m)
          for m in covering Range do
            mkRow (sprintf "[block range %04X-%04X]" m.Addr m.EndExcl) cyan m.Text true None
          for m in covering Name do
            mkRow (sprintf "[block name %04X-%04X]" m.Addr m.EndExcl) cyan m.Text true None
    cmtPaneAddBtn.Click.Add(fun _ ->
      match control with
      | Some c ->
        control <- Some(ControlFile.addLine c memCursor cmtPaneAddBox.Text)
        cmtPaneAddBox.Text <- ""
        refreshDisasm ()
        rebuildCommentPane true
      | None -> statusText.Text <- "no control file - click New ctrl first")
    // pane lights up while it owns the keyboard
    cmtPaneInner.GotKeyboardFocus.Add(fun _ -> cmtPaneBorder.BorderBrush <- cyan)
    cmtPaneInner.LostKeyboardFocus.Add(fun _ -> cmtPaneBorder.BorderBrush <- dim)
    // clicking a code row (or arrows in the focused list) moves the bright
    // cursor to that row without recentering, and feeds the pane
    disasmList.SelectionChanged.Add(fun _ ->
      match disasmList.SelectedItem with
      | :? DisasmRow as row when row.Addr >= 0 && memCursor <> row.Addr ->
        memCursor <- row.Addr
        memCursorReason <- "row click"
        refreshDisasm ()
        rebuildCommentPane true
      | _ -> ())
    disasmList.KeyDown.Add(fun e ->
      if e.Key = Key.Enter then
        e.Handled <- true
        cmtPaneBorder.BorderBrush <- cyan
        cmtPaneAddBox.Focus () |> ignore)

    // 5. the code window: list + graph surfaces stacked, one visible
    let codeHost = Grid()
    codeHost.Children.Add disasmList |> ignore
    graphScroll.Content <- codeGraphCanvas
    graphScroll.Visibility <- Visibility.Collapsed
    codeHost.Children.Add graphScroll |> ignore
    center.Children.Add codeHost |> ignore

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

    // graphics view: byte rows (8 KB window) + row width + zoom + address
    // slider
    let gfxTab = TabItem(Header = "Graphics")
    let gfxPanel = DockPanel()
    let gfxTop = StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))
    let gfxHeader =
      TextBlock(
        Text = "memory as 8px rows (sprite finder)",
        Foreground = dim, FontSize = 12.0,
        VerticalAlignment = VerticalAlignment.Center)
    gfxTop.Children.Add gfxHeader |> ignore
    // row width: pixels per row; width/8 consecutive bytes are drawn side
    // by side before stepping down. Type a value 8..256 and press Enter,
    // or step by 8 with the buttons
    let gfxWidthLabel =
      TextBlock(
        Text = "row width:", Foreground = dim, FontSize = 12.0,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = Thickness(14.0, 0.0, 4.0, 0.0))
    gfxTop.Children.Add gfxWidthLabel |> ignore
    gfxWidthBox.Text <- string gfxRowWidth
    gfxWidthBox.ToolTip <- "pixels per row (bytes drawn side by side); press Enter to apply"
    gfxWidthBox.Background <- panel
    gfxWidthBox.Foreground <- normal
    gfxTop.Children.Add gfxWidthBox |> ignore
    let applyGfxWidth (v: int) =
      gfxRowWidth <- max 8 (min 256 v)
      gfxWidthBox.Text <- string gfxRowWidth
      refreshGfx ()
    let commitGfxWidth () =
      match Int32.TryParse(gfxWidthBox.Text) with
      | true, v -> applyGfxWidth v
      | _ -> gfxWidthBox.Text <- string gfxRowWidth
    gfxWidthBox.KeyDown.Add(fun e ->
      if e.Key = Key.Enter then commitGfxWidth ())
    gfxWidthBox.LostFocus.Add(fun _ -> commitGfxWidth ())
    gfxWidthDown.ToolTip <- "narrower rows (-8 pixels)"
    gfxWidthDown.Margin <- Thickness(4.0, 0.0, 0.0, 0.0)
    gfxWidthDown.Click.Add(fun _ -> applyGfxWidth (gfxRowWidth - 8))
    gfxTop.Children.Add gfxWidthDown |> ignore
    gfxWidthUp.ToolTip <- "wider rows (+8 pixels)"
    gfxWidthUp.Margin <- Thickness(4.0, 0.0, 0.0, 0.0)
    gfxWidthUp.Click.Add(fun _ -> applyGfxWidth (gfxRowWidth + 8))
    gfxTop.Children.Add gfxWidthUp |> ignore
    // zoom: stretch the 256x256 bitmap by an integer factor with nearest
    // neighbour sampling; the ScrollViewer below takes over once the
    // enlarged image outgrows the tab
    for z in [ 1; 2; 3; 4; 8 ] do
      let isDflt = (z = 2)
      let radio =
        RadioButton(
          Content = sprintf "%dx" z, GroupName = "gfxZoom",
          IsChecked = Nullable<bool>(isDflt), Foreground = normal,
          VerticalAlignment = VerticalAlignment.Center,
          Margin = Thickness(8.0, 0.0, 0.0, 0.0))
      radio.Checked.Add(fun _ ->
        gfxImage.Width <- 256.0 * float z
        gfxImage.Height <- 256.0 * float z)
      gfxTop.Children.Add radio |> ignore
    DockPanel.SetDock(gfxTop, Dock.Top)
    gfxPanel.Children.Add gfxTop |> ignore
    // slider row: 0000h [========] FFFFh (docked bottom-most)
    let gfxSliderRow = Grid()
    gfxSliderRow.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
    gfxSliderRow.ColumnDefinitions.Add(ColumnDefinition())
    gfxSliderRow.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
    gfxSliderRow.Margin <- Thickness(0.0, 2.0, 0.0, 0.0)
    let gfxLoLabel =
      TextBlock(
        Text = "0000h", Foreground = dim, FontFamily = mono, FontSize = 11.0,
        VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(0.0, 0.0, 6.0, 0.0))
    Grid.SetColumn(gfxLoLabel, 0)
    gfxSliderRow.Children.Add gfxLoLabel |> ignore
    gfxSlider.VerticalAlignment <- VerticalAlignment.Center
    Grid.SetColumn(gfxSlider, 1)
    gfxSliderRow.Children.Add gfxSlider |> ignore
    let gfxHiLabel =
      TextBlock(
        Text = "FFFFh", Foreground = dim, FontFamily = mono, FontSize = 11.0,
        VerticalAlignment = VerticalAlignment.Center, Margin = Thickness(6.0, 0.0, 0.0, 0.0))
    Grid.SetColumn(gfxHiLabel, 2)
    gfxSliderRow.Children.Add gfxHiLabel |> ignore
    DockPanel.SetDock(gfxSliderRow, Dock.Bottom)
    gfxPanel.Children.Add gfxSliderRow |> ignore
    gfxAddrLabel.Foreground <- dim
    gfxAddrLabel.FontFamily <- mono
    gfxAddrLabel.FontSize <- 12.0
    gfxAddrLabel.Margin <- Thickness(0.0, 6.0, 0.0, 0.0)
    DockPanel.SetDock(gfxAddrLabel, Dock.Bottom)
    gfxPanel.Children.Add gfxAddrLabel |> ignore
    gfxImage.Source <- gfxBmp
    // 512 = the default 2x zoom radio; the Checked handler above sets this
    // too, but runs before the image gets its source
    gfxImage.Width <- 256.0 * 2.0
    gfxImage.Height <- 256.0 * 2.0
    gfxImage.Stretch <- Stretch.Uniform
    RenderOptions.SetBitmapScalingMode(gfxImage, BitmapScalingMode.NearestNeighbor)
    let gfxScroll =
      ScrollViewer(
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
    gfxScroll.Content <- gfxImage
    gfxPanel.Children.Add gfxScroll |> ignore
    gfxTab.Content <- gfxPanel
    gfxSlider.Value <- float gfxBase
    gfxSlider.ValueChanged.Add(fun _ ->
      gfxBase <- int gfxSlider.Value
      refreshGfx ())
    refreshGfx ()

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
    let callGraphScroll = ScrollViewer(HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
    callGraphScroll.Content <- graphCanvas
    graphTab.Content <- callGraphScroll

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
    right.Items.Add gfxTab |> ignore
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
    let saveTimelineBtn = Button(Content = "Save timeline", ToolTip = "save the per-frame state timeline + keys to games/<id>/timeline.jst")
    let clearTimelineBtn = Button(Content = "Clear timeline", ToolTip = "delete the recorded gameplay: wipes the states and key script in memory and deletes timeline.jst from the game folder (the project is untouched)")
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
    saveTimelineBtn.Click.Add(fun _ -> saveTimeline true)
    clearTimelineBtn.Click.Add(fun _ ->
      match session with
      | Some s ->
        pauseGame ()
        replaying <- false
        s.StopReplay ()
        s.ResetTimeline ()
        match currentGame with
        | Some game ->
          try File.Delete (timelinePath game) with _ -> ()
          statusText.Text <- sprintf "%s: recording cleared" game.Name
        | None -> ()
        syncSlider ()
        refreshAll ()
      | None -> statusText.Text <- "no session")
    loadBtn.Click.Add(fun _ -> loadTrace ())
    // Seek: dragging the frame slider pauses the game and jumps to ANY
    // recorded frame - past from history, future straight from the loaded
    // timeline - and stops a running replay there. The session stays parked
    // on the previewed frame until Run (live from here), Go (branch), or
    // Replay (script from here) is pressed.
    rewindSlider.PreviewMouseDown.Add(fun _ ->
      if session.IsSome then pauseGame ()
      rewinding <- true)
    rewindSlider.PreviewMouseUp.Add(fun _ -> rewinding <- false)
    rewindSlider.ValueChanged.Add(fun args ->
      match session with
      | Some s ->
        let target = int args.NewValue
        if rewinding && target <= s.TimelineExtent then
          if replaying then
            s.StopReplay ()
            replaying <- false
          s.JumpTo(target)
          presentGame ()
          updateTimeLabel s.Frame
          seekCodeView s target
      | None -> ())
    goBtn.Click.Add(fun _ ->
      match session with
      | Some s ->
        pauseGame ()
        s.BranchAt(s.Frame)
        replaying <- false
        running <- true
        frameTimer.Start()
        statusText.Text <- sprintf "branched at frame %d - new future starts here" s.Frame
      | None -> ())
    replayBtn.Click.Add(fun _ ->
      match session with
      | Some s ->
        pauseGame ()
        replayExtent <- max 1 s.TimelineExtent // seekable horizon before truncation
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
    soundToggle.ToolTip <- "beeper audio is muted by default; uncheck for low-volume audio"
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
              | Some s -> max 1L (int64 (max s.ReplayEndFrame s.TimelineExtent))
              | None ->
                match currentTrace () with
                | Some t when t.FrameTicks.Length > 0 -> int64 t.FrameTicks.Length
                | _ -> 300L)
          w.Owner <- self
          timelineWin <- Some w
          w.Closed.Add(fun _ -> timelineWin <- None)
          w.Show()
      with ex -> statusText.Text <- sprintf "timeline demo failed: %s" ex.Message)

    for c in [ gameCombo :> FrameworkElement; engineCombo :> FrameworkElement; parityLabel :> FrameworkElement; setEntryBtn :> FrameworkElement; exportScriptBtn :> FrameworkElement; timelineBtn :> FrameworkElement; runBtn :> FrameworkElement; pauseBtn :> FrameworkElement; stepFrameBtn :> FrameworkElement; recordToggle :> FrameworkElement; soundToggle :> FrameworkElement; saveBtn :> FrameworkElement; saveTimelineBtn :> FrameworkElement; clearTimelineBtn :> FrameworkElement; loadBtn :> FrameworkElement ] do
      c.Margin <- Thickness(4.0, 0.0, 4.0, 0.0)
      toolbar.Children.Add c |> ignore
    toolbar.Children.Add sep |> ignore
    // rewind + replay group
    for c in [ rewindSlider :> FrameworkElement; timeLabel :> FrameworkElement; recStatsLabel :> FrameworkElement; goBtn :> FrameworkElement; replayBtn :> FrameworkElement ] do
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
        // Seek to any recorded frame, future included, and park there.
        let target = max 0 (min (int u) s.TimelineExtent)
        s.JumpTo(target)
        presentGame ()
        updateTimeLabel s.Frame
        seekCodeView s target
      | None -> ()

    previewRangeImpl <- fun which ->
      let fStart, fEnd = rangeOf which
      let label = if which = A then "A" else "B"
      match session with
      | Some s when fEnd - 1 <= s.TimelineExtent && fEnd - 1 >= 0 ->
        if running || replaying then pauseGame ()
        if replaying then replaying <- false // JumpTo stops the movie core-side
        previewTimer.Stop()
        previewFrom <- max 0 fStart
        previewSpan <- max 0 (fEnd - 1 - previewFrom)
        previewTicks <- 0
        s.JumpTo previewFrom
        presentGame ()
        updateTimeLabel s.Frame
        seekCodeView s previewFrom
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
        s.JumpTo(max 0 (min (previewFrom + previewSpan * frac / 50) s.TimelineExtent))
        presentGame ()
        updateTimeLabel s.Frame
        timeline.Playhead <- int64 s.Frame
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
    // focus the code view on an address: memory mode re-centers the memory
    // disassembly on it, execution mode jumps to its first execution
    let focusCodeView (reason: string) (addr: int) =
      if disasmModeMemory then
        gotoMemCursor (addr &&& 0xFFFF) reason
        refreshDisasm ()
      else
        match currentTrace (), session with
        | Some t, _ when t.Entries.Length > 0 && addr < t.FirstIndexAtPc.Length && t.FirstIndexAtPc[addr] >= 0 ->
          cursor <- t.FirstIndexAtPc[addr]
          refreshAll ()
          syncSlider ()
        | _ -> ()

    // click/drag scrubs the code views to the address under the cursor
    controlMap.AddressClicked.Add(fun addr -> focusCodeView "memory map click" addr)

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
      control <- Some(ControlFile.empty 0x4000 0x10000)
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
      disasmModeGraph <- false
      execModeBtn.IsChecked <- Nullable<bool>(false)
      graphModeBtn.IsChecked <- Nullable<bool>(false)
      // Carry the execution view's position: the memory sweep always shows
      // the cursor row exactly (linear decode can be misaligned elsewhere),
      // so landing on it makes the instruction the exec view was showing
      // appear in memory view. A row right-click overrides the target.
      match pendingMemCursor with
      | Some a -> gotoMemCursor a "row context menu sync"; pendingMemCursor <- None
      | None ->
        match currentTrace () with
        | Some t when t.Entries.Length > 0 ->
          gotoMemCursor (int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc) "execution view switch"
        | _ -> ()
      applyViewMode ()
      refreshDisasm ())
    execModeBtn.Checked.Add(fun _ ->
      disasmModeMemory <- false
      disasmModeGraph <- false
      memModeBtn.IsChecked <- Nullable<bool>(false)
      graphModeBtn.IsChecked <- Nullable<bool>(false)
      applyViewMode ()
      refreshDisasm ())
    graphModeBtn.Checked.Add(fun _ ->
      disasmModeGraph <- true
      memModeBtn.IsChecked <- Nullable<bool>(false)
      execModeBtn.IsChecked <- Nullable<bool>(false)
      applyViewMode ()
      refreshDisasm ())
    graphAllBtn.Checked.Add(fun _ ->
      disasmGraphAll <- true
      if disasmModeGraph then refreshDisasm ())
    graphAllBtn.Unchecked.Add(fun _ ->
      disasmGraphAll <- false
      if disasmModeGraph then refreshDisasm ())

    // mass-comment buttons: all read commentBox.Text
    let text () : string = commentBox.Text
    let addComment (m: ControlComment) =
      match control with
      | Some c ->
        // Line comments append (an address can carry several); keyed kinds
        // (exec/name/range) still replace their key.
        control <-
          (if m.Kind = Line then Some (ControlFile.addLine c m.Addr m.Text)
           else Some (ControlFile.upsert c m))
        statusText.Text <- sprintf "comment added (%s)" (ControlFile.kindToString m.Kind)
        refreshDisasm ()
      | None -> statusText.Text <- "no control file - click New ctrl first"
    idxCmtBtn.Click.Add(fun _ ->
      match currentTrace () with
      | Some t when t.Entries.Length > 0 ->
        addComment { Kind = Exec; Addr = 0; EndExcl = 0; InstrIndex = cursor; Text = text () }
      | _ -> statusText.Text <- "no trace - cannot index-comment")
    lineCmtBtn.Click.Add(fun _ ->
      let addr =
        if disasmModeMemory || disasmModeGraph then memCursor
        else
          match currentTrace () with
          | Some t when t.Entries.Length > 0 -> int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc
          | _ -> memCursor
      addComment { Kind = Line; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = text () })
    // right-click a code row: comment / clear / exec comment / copy address.
    // Preview + a walk up the visual tree, so the row is found no matter
    // which template part sits under the mouse.
    let rec findRow (d: obj) : DisasmRow option =
      match d with
      | :? FrameworkElement as fe ->
        match fe.DataContext with
        | :? DisasmRow as row -> Some row
        | _ ->
          match VisualTreeHelper.GetParent fe with
          | null -> None
          | parent -> findRow parent
      | _ -> None
    disasmList.PreviewMouseRightButtonDown.Add(fun e ->
      match findRow e.OriginalSource with
      | Some row when row.Addr >= 0 ->
        e.Handled <- true
        openRowMenu disasmList row.Addr row.InstrIdx refreshDisasm
      | _ -> ())
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
              cc <- ControlFile.addLine cc pc (text ())
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
        // n is the 16-bit word at the address in word mode, the single byte
        // in byte mode; each mode compares against exactly that width.
        applyScanFilter (fun _ o n ->
          if scanWidth16.IsChecked.GetValueOrDefault()
          then n = (value &&& 0xFFFF)
          else n = (value &&& 0xFF))
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
          gotoMemCursor row.Addr "scan list jump"
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
            gotoMemCursor row.Addr "scan list jump"
            memModeBtn.IsChecked <- Nullable<bool>(true)
            statusText.Text <- sprintf "no trace yet - showing %04X in memory view" row.Addr
          | _ -> ()
      | _ -> ())
    scanCmtBtn.Click.Add(fun _ ->
      match scanList.SelectedItem with
      | :? DisasmRow as row when row.Addr >= 0 ->
        match control with
        | Some c ->
          control <- Some(ControlFile.addLine c row.Addr commentBox.Text)
          statusText.Text <- sprintf "comment added at %04X" row.Addr
          refreshDisasm ()
        | None -> statusText.Text <- "no control file - click New ctrl first"
      | _ -> statusText.Text <- "select an address in the scan list first")

    // keep the timeline extent fresh each UI tick
    uiTimer.Tick.Add(fun _ ->
      timeline.Length <- timelineExtent ())
    // keep the graphics tile view in step with live memory
    uiTimer.Tick.Add(fun _ -> refreshGfx ())
    // keep the control map live: cursor hairline, heat/self-mod overlays,
    // and a thumb showing which address window the code view displays.
    uiTimer.Tick.Add(fun _ ->
      // While the machine is running the recorder window keeps growing, so
      // ensureBuilt() here would linearize up to 4M entries at every tick;
      // follow the live PC and reuse the last built trace instead.
      let live =
        match session with
        | Some s when running || replaying -> Some s
        | _ -> None
      let curAddr =
        if disasmModeMemory then Some memCursor
        else
          match live with
          | Some s -> Some(s.Regs.Pc())
          | None ->
            match currentTrace () with
            | Some t when t.Entries.Length > 0 ->
              Some(int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc)
            | _ -> None
      match curAddr with
      | Some a ->
        controlMap.CursorAddress <- a
        if controlMap.IsOutside a then controlMap.CenterOn a
        let lo, hi =
          if disasmModeMemory then
            (max 0 (memCursor - 0x40), min 0x10000 (memCursor + 0xC0))
          else
            let t = match live with Some _ -> built | None -> currentTrace ()
            match t with
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

    // Mouse wheel over the code window travels through the code. The fixed
    // 41-row windows have no real scroll extent, so each notch moves the
    // anchor itself (one instruction per line) and setRows keeps the offset
    // stable, which reads as the code sliding under the cursor. The long
    // brush-selection list (memory mode) and the graph surface have genuine
    // overflow and keep their native scrolling.
    disasmList.PreviewMouseWheel.Add(fun e ->
      let native = disasmModeGraph || (disasmModeMemory && activeSelection().IsSome)
      if not native then
        let notches = float e.Delta / 120.0
        // negated so wheel up = earlier content: lower addresses in memory
        // mode, earlier instructions in execution mode
        let lines = -int (float SystemParameters.WheelScrollLines * notches)
        if lines <> 0 then
          e.Handled <- true
          if disasmModeMemory then
            match currentMemory () with
            | None -> ()
            | Some mem ->
              // The wheel scrolls the window anchor, not the cursor: the
              // highlighted instruction stays on its address and moves with
              // the content (out of view if you keep scrolling the same way).
              let top =
                match memViewTop with
                | Some t when t >= 0 && t < 0x10000 -> t
                | _ -> memCursor
              if lines > 0 then
                let mutable a = top
                for _ in 1 .. lines do
                  a <- (a + max 1 (Disasm.disasmMemory mem a).Length) &&& 0xFFFF
                memViewTop <- Some a
              else
                // Back up to the previous instruction start; starts[0] is
                // always set, so the walk terminates at worst at address 0.
                match getInstrStarts () with
                | None -> memViewTop <- Some ((top + lines) &&& 0xFFFF)
                | Some starts ->
                  let mutable a = top
                  for _ in 1 .. -lines do
                    a <- (a - 1) &&& 0xFFFF
                    while not starts[a] do a <- (a - 1) &&& 0xFFFF
                  memViewTop <- Some a
              refreshDisasm ()
          else
            let n = currentEntryCount ()
            if n > 0 then
              cursor <- max 0 (min (cursor + lines) (n - 1))
              refreshAll ()
              syncSlider ())

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

    // double-click on the graphics view focuses the code view on the
    // address under the pixel: undo the zoom scaling and the row-width
    // layout to get back from bitmap coordinates to a memory address
    gfxImage.MouseLeftButtonDown.Add(fun e ->
      if e.ClickCount = 2 then
        let scale = gfxImage.ActualWidth / 256.0
        if scale > 0.0 then
          let pos = e.GetPosition gfxImage
          let px = int (pos.X / scale)
          let py = int (pos.Y / scale)
          if px >= 0 && px < 256 && py >= 0 && py < 256 then
            let w = max 8 (min 256 gfxRowWidth)
            let i = ((px / w) * 256 + py) * (w / 8) + (px % w) / 8
            let addr = (gfxBase + i) &&& 0xFFFF
            focusCodeView "graphics pane double-click" addr
            statusText.Text <- sprintf "graphics click: 0x%04X (pixel %d, %d)" addr px py)

    // wheel over the graphics view pages memory, matching the code pane's
    // wheel-anchored scrolling: each notch moves the window by one pixel
    // row (bytesPerRow bytes at the current row width), handled before the
    // ScrollViewer can turn it into viewport scrolling; the slider follows
    gfxImage.PreviewMouseWheel.Add(fun e ->
      let notches = float e.Delta / 120.0
      // negated so wheel up moves toward lower addresses, matching the
      // code pane
      let lines = -int (float SystemParameters.WheelScrollLines * notches)
      if lines <> 0 then
        e.Handled <- true
        let w = max 8 (min 256 gfxRowWidth)
        gfxSlider.Value <- float ((gfxBase + lines * (w / 8)) &&& 0xFFFF))

    // Emulator keys ride the window's KeyDown/KeyUp. Text-entry controls
    // (comment box, paste box, scan value) must not leak keystrokes into the
    // emulated keyboard - and with recording on, into the replay key log.
    let isTextEntry (e: KeyEventArgs) =
      e.OriginalSource :? System.Windows.Controls.Primitives.TextBoxBase
      || e.OriginalSource :? ComboBox
    self.KeyDown.Add(fun e ->
      if isTextEntry e then ()
      else
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
      if isTextEntry e then ()
      else
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
          if cursor >= n - 1 then
            // End of the trace: stop stepping so the timer doesn't keep
            // rebuilding the views at the last instruction forever.
            cinemaPlaying <- false
            cinemaTimer.Stop()
            playBtn.Content <- "Play"
            statusText.Text <- "cinema reached the end of the trace"
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
              bootTask <- None
              session <- Some loadedSession
              let hasTimeline =
                match currentGame with
                | Some game when game.GameId = bootGameId -> loadTimeline game loadedSession
                | _ -> false
              if hasTimeline then
                // Autoplay with the whole recorded timeline seekable from
                // frame 0: states are restored, not re-executed.
                replayExtent <- max 1 loadedSession.TimelineExtent
                loadedSession.StartReplay()
                replaying <- true
              else
                replaying <- false
              startGame ()
              if hasTimeline then
                statusText.Text <-
                  sprintf "%s: timeline loaded (%d frames) - autoplay, seek anywhere" currentGame.Value.Name loadedSession.StateTimeline.Count
            with ex ->
              statusText.Text <- "boot failed: " + ex.Message
          | Some t ->
            match bootScreen with
            | Some buf -> gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), buf, 320 * 4, 0)
            | None -> ()
            let secs = int (System.DateTime.UtcNow - bootStartedAt).TotalSeconds
            let gameName = match currentGame with Some g -> g.Name | None -> "game"
            statusText.Text <-
              sprintf "booting %s to game entry... %ds, tape frame %d" gameName secs bootFrameNo
          | None -> ()
      | Some s ->
        if not rewinding then
          rewindSlider.Maximum <- float (if replaying then replayExtent else max 0 s.TimelineExtent)
          rewindSlider.Value <- float s.Frame
        timeline.Playhead <- int64 s.Frame
        updateTimeLabel s.Frame
        if running then
          statusText.Text <-
            sprintf "frame=%d  tick=%.2fM  instr=%d/%d  distinct-pc=%d  selfmod=%d  rec=%s"
              s.Frame
              (float s.CycleCount / 1_000_000.0)
              s.Recorder.EntryCount
              s.Recorder.Capacity
              (s.Recorder.PerPcCount |> Array.fold (fun n c -> if c > 0 then n + 1 else n) 0)
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
    // Recording stats, twice a second: this session's captured frames as
    // time, the timeline's size, and the live bytes/s and bytes/min rates.
    statsTimer.Tick.Add(fun _ ->
      match session with
      | Some s when s.LiveCapturedFrames > 0 ->
        let seconds = float s.LiveCapturedFrames / 50.0
        let mb = float s.TimelineSessionBytes / (1024.0 * 1024.0)
        let mbPerSec = if seconds >= 2.0 then mb / seconds else 0.0
        recStatsLabel.Text <-
          sprintf "rec %02d:%02d  %.0f MB  %.1f MB/s  %.0f MB/min"
            (int seconds / 60) (int seconds % 60) mb mbPerSec (mbPerSec * 60.0)
      | _ -> recStatsLabel.Text <- "")
    // comment pane follows the cursor and control-file changes
    uiTimer.Tick.Add(fun _ -> rebuildCommentPane false)
    uiTimer.Start()
    manualTimer.Start()
    statsTimer.Start()

    // Launch the manifest-selected default game.
    launchGame startupGame
    loadControlForGame ()

    self.Closed.Add(fun _ ->
      running <- false
      saveTimeline false // only when the recording actually changed
      frameTimer.Stop()
      cinemaTimer.Stop()
      manualTimer.Stop()
      uiTimer.Stop()
      previewTimer.Stop()
      statsTimer.Stop()
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
