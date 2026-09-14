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
    {
        Tag: string
        Brush: SolidColorBrush
        /// Optional row background: the active brush tint when the address is
        /// inside the selected code. Null = transparent.
        Tint: Brush
        IsCurrent: bool
        /// Row address (PC for execution rows, address for memory rows); -1
        /// for synthetic rows. Drives comment lookups and jumps.
        Addr: int
        /// Instruction index in the trace (-1 in memory mode).
        InstrIdx: int
    }

/// One row of the mined-routine list.
type RoutineRow =
    { Tag: string
      Brush: SolidColorBrush
      IsSelected: bool
      Entry: int }

/// How a fresh boot enters the emulator: autoplay a slot, arm a fresh
/// recording under a new slot name, or run live with no trace at all.
type SessionStart =
    | AutoplaySlot of string
    | RecordFresh of string
    | LiveFresh

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
    /// games/ directory (the manifests live at the repository root). Shared
    /// with the launcher window (Projects.findGamesDir).
    let findGamesDir () = Projects.findGamesDir ()

    // ---- palette -----------------------------------------------------------
    // Shared Theme instances (day/night switch mutates them in place; every
    // alias below follows). Same names as before, so no use-site churn.
    let bg = Theme.bg
    let panel = Theme.panel
    let normal = Theme.normal
    let dim = Theme.dim
    let green = Theme.green
    let cyan = Theme.cyan
    let yellow = Theme.yellow
    let red = Theme.red
    let orange = Theme.orange
    let sky = Theme.sky
    let amber = Theme.amber
    let darkFg = Theme.darkFg
    let hiBg = Theme.hiBg
    let mono = FontFamily("Consolas")
    let heatLut = Theme.heatLut
    // ---- bitmaps -----------------------------------------------------------
    let gameBitmap = WriteableBitmap(320, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
    let heatBmp = WriteableBitmap(256, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
    let stripBmp = WriteableBitmap(512, 24, 96.0, 96.0, PixelFormats.Bgra32, null)
    let gfxBmp = WriteableBitmap(256, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
    let heatPixels = Array.zeroCreate<byte> (256 * 256 * 4)
    let stripPixels = Array.zeroCreate<byte> (512 * 24 * 4)
    let gfxPixels = Array.zeroCreate<byte> (256 * 256 * 4)

    // ---- state -------------------------------------------------------------
    let mutable running = false
    let mutable cursor = 0
    /// Execution-view window bookkeeping: `execViewCenter` is the trace index
    /// the 41-row window was last centered on, `execViewHold` makes the next
    /// render reuse it. A row click holds the window so the bright row moves
    /// without the content sliding; any other cursor move (keys, wheel, slider,
    /// seeks, jumps) renders without the hold and recenters on the cursor.
    let mutable execViewCenter = 0
    let mutable execViewHold = false
    let mutable built: Trace option = None
    let mutable builtAtCount = -1
    let mutable loaded: Trace option = None
    /// Execution trace synthesized from the active flame window: a completed
    /// flame build installs its instruction tier as the browsable trace, so
    /// the code view, slider and heatmap describe exactly the built range -
    /// the live ring usually does not cover recording-only frames. Cleared by
    /// any live frame execution (renderFrame) and on session switches.
    let mutable flameTrace: Trace option = None
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

    /// Mode bar under the screen: says whether the frames on screen come from
    /// a scripted replay of a saved trace or from live play being recorded
    /// (red) - after boot it is otherwise hard to tell the two apart.
    let modeLabel =
        TextBlock(FontFamily = mono, FontSize = 12.0, Margin = Thickness(0.0, 6.0, 0.0, 0.0))

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
        Slider(Minimum = 0.0, Maximum = 65535.0, SmallChange = 8.0, LargeChange = 128.0, IsMoveToPointEnabled = true)

    let gfxAddrLabel = TextBlock()
    let gfxWidthBox = TextBox(Width = 44.0)
    let gfxWidthDown = Button(Content = "-8", Width = 32.0)
    let gfxWidthUp = Button(Content = "+8", Width = 32.0)
    let disasmList = ListBox()
    let slider = Slider()
    let cursorLabel = TextBlock()

    /// T-state range of the current trace window: the first and last t-count
    /// the trace slider can scroll, their span in raw T-states and in ms at
    /// 3.5 MHz, and the entry/frame coverage. Updated by refreshStrip.
    let traceRangeLabel =
        TextBlock(Foreground = dim, FontFamily = mono, FontSize = 11.0)

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

    /// Active recording slot ("default" = the legacy timeline.jst). Autosave
    /// and Clear both route to it; the picker and the toolbar combo switch it.
    let mutable currentSlot = TimelineSlots.defaultName

    let slotPath (game: GameManifest) (name: string) =
        TimelineSlots.pathFor game.GameDirectory name

    let mutable pendingStart: SessionStart option = None

    /// Boot parked while the menu was up and the slot picker never ran:
    /// the uiTimer shows it on re-entry instead.
    let mutable pickQueued = false

    /// No-questions entry: the single slot autoplays, nothing recorded yet
    /// starts a fresh named recording; picker dismissal falls back to the
    /// newest slot.
    let defaultChoice (slots: TimelineSlots.SlotInfo list) : SessionStart =
        match slots with
        | [] -> RecordFresh(TimelineSlots.suggestName ())
        | first :: _ -> AutoplaySlot first.Name

    /// Persist states + key log. Writes ONLY when the timeline actually
    /// changed since the last save (captures, branch truncation, key events);
    /// `force` (the Save button) rewrites even an unchanged recording. Called
    /// on window close and game switch - a recording grows by megabytes per
    /// Assigned once the flame wiring exists: (firstFrame, lastFrameExclusive,
    /// zoom). Lets early code (timeline load) kick off flame builds.
    let mutable flameBuildImpl: (int * int * bool -> unit) = fun _ -> ()

    /// second, so there is deliberately no autosave on pause or scrub. A
    /// trivial timeline (fresh boot or just cleared) is never written.
    let saveTimeline (force: bool) =
        match currentGame, session with
        | Some game, Some s when (force || s.TimelineDirty) && s.StateTimeline.Count > 1 ->
            match
                StateTimelineStore.save
                    (slotPath game currentSlot)
                    (timelineFingerprint game)
                    s.StateTimeline
                    s.KeyLog.Events
            with
            | Ok() ->
                s.ClearTimelineDirty()

                statusText.Text <-
                    sprintf
                        "timeline saved: %d frames, %.1f MB"
                        s.StateTimeline.Count
                        (float s.StateTimeline.BytesUsed / (1024.0 * 1024.0))
            | Error error -> statusText.Text <- sprintf "timeline save failed for %s: %s" game.Name error
        | _ -> ()

    /// Install a saved recording before a single frame runs: the key script
    /// drives autoplay and every frame - future included - is seekable.
    let loadSlotTimeline (game: GameManifest) (s: TraceSession) (path: string) : bool =
        match StateTimelineStore.tryLoad path (timelineFingerprint game) with
        | TimelineLoaded(timeline, events) ->
            s.LoadStateTimeline(timeline, events)
            // Big picture immediately: build a flame window for the recording's
            // head (capped - the rest follows on demand via brushes/auto).
            flameBuildImpl (timeline.StartFrame + 1, min (timeline.EndFrame + 1) (timeline.StartFrame + 801), true)

            if not (List.isEmpty events) then
                statusText.Text <- sprintf "%s: loaded %d frames, %d key events" game.Name timeline.Count events.Length

            timeline.Count > 1
        | TimelineMissing -> false
        | TimelineIgnored reason ->
            statusText.Text <- sprintf "%s: saved timeline ignored (%s)" game.Name reason
            false

    let loadTimeline (game: GameManifest) (s: TraceSession) : bool =
        loadSlotTimeline game s (slotPath game currentSlot)


    /// One-shot: the launcher's "Slow tape boot" button forces the next
    /// launch to emulate the real tape (the oracle boot) instead of the
    /// flash loader. Consumed by the next launchGame.
    let mutable forceSlowBoot = false

    /// Start a game from its manifest: warm cache -> fast port session; cold
    /// auto -> flash load (FastBoot) or the emulated-tape oracle boot when
    /// forced slow; cold manual -> the user runs the
    let launchGame (m: GameManifest, preset: SessionStart option) =
        saveTimeline false
        let slowBoot = forceSlowBoot // one-shot: consumed by this launch
        forceSlowBoot <- false
        bootStartedAt <- System.DateTime.UtcNow
        bootScreen <- None
        bootFrameNo <- 0
        manualBoot <- None
        manualTimer.Stop()
        currentGame <- Some m
        bootGameId <- m.GameId
        pickQueued <- false

        // Entry choice: explicit (slot switch / live / record-new from the
        // toolbar) wins; menu entry with 0-1 slots preinstalls the obvious
        // default; 2+ slots get the picker once boot lands. Manual boots
        // keep their own path and ignore all of this.
        match preset with
        | Some st -> pendingStart <- Some st
        | None when m.Boot <> "manual" ->
            let slots = TimelineSlots.list m.GameDirectory
            pendingStart <- if slots.Length < 2 then Some(defaultChoice slots) else None
        | None -> pendingStart <- None

        // Warm either way; the fast slot only counts when it may be used.
        // (A warm-but-wrong-mode slot makes the TraceSession rebuild the
        // other one in seconds - both boots are cheap once cached.)
        let warmFast = EntryCache.tryLoadMode m.Rom m.Tzx true |> Option.isSome
        let warmLegacy = EntryCache.tryLoad m.Rom m.Tzx |> Option.isSome
        let warm = if slowBoot then warmLegacy else warmFast || warmLegacy

        if warm then
            bootTask <-
                Some(
                    System.Threading.Tasks.Task.Run(fun () ->
                        TraceSession(m.Rom, m.Tzx, 4_000_000, onFrame = recordBootFrame, fastBoot = not slowBoot))
                )

            statusText.Text <- sprintf "booting %s (cached entry state)..." m.Name
        elif m.Boot = "auto" then
            bootTask <-
                Some(
                    System.Threading.Tasks.Task.Run(fun () ->
                        TraceSession(m.Rom, m.Tzx, 4_000_000, onFrame = recordBootFrame, fastBoot = not slowBoot))
                )

            if slowBoot then
                statusText.Text <- sprintf "booting %s to game entry (emulating the tape, first run is slow)..." m.Name
            else
                statusText.Text <- sprintf "flash-loading %s..." m.Name
        elif m.Boot = "program" then
            // A raw Z80 image: seed the entry cache from the bin, then boot the
            // port session warm (the cache is keyed on the image file).
            match m.ProgramBin, m.ProgramAddress with
            | Some bin, Some addr ->
                try
                    ProgramEntry.seed bin addr

                    bootTask <-
                        Some(
                            System.Threading.Tasks.Task.Run(fun () ->
                                TraceSession(bin, bin, 4_000_000, onFrame = recordBootFrame))
                        )

                    statusText.Text <- sprintf "booting %s (program image at 0x%04X)..." m.Name addr
                with ex ->
                    statusText.Text <- sprintf "cannot load %s's program image: %s" m.Name ex.Message
            | _ -> statusText.Text <- sprintf "%s: manifest is missing program.bin/address" m.Name
        else
            bootTask <- None
            manualBoot <- Some(ManualBoot(m.Rom, m.Tzx))
            manualTimer.Start()
            statusText.Text <- sprintf "%s: run the game loader here; press 'Set as game entry' when it is ready" m.Name

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

    let gameCombo =
        ComboBox(Width = 130.0, VerticalAlignment = VerticalAlignment.Center)

    let engineCombo =
        ComboBox(Width = 92.0, VerticalAlignment = VerticalAlignment.Center)

    let parityLabel =
        TextBlock(Text = "", VerticalAlignment = VerticalAlignment.Center, Foreground = normal)

    let setEntryBtn = Button(Content = "Set as game entry")
    let exportScriptBtn = Button(Content = "Export script")
    let timelineBtn = Button(Content = "Timeline")

    let timeLabel =
        TextBlock(Text = "00:00", VerticalAlignment = VerticalAlignment.Center)

    /// Recording stats (timeline size, rates), refreshed twice a second.
    let recStatsLabel =
        TextBlock(Foreground = dim, VerticalAlignment = VerticalAlignment.Center)

    let goBtn = Button(Content = "Go")
    let replayBtn = Button(Content = "Replay")
    let mutable rewinding = false // slider drag in progress
    let mutable replaying = false // scripted replay driving the frame timer
    /// Timeline extent frozen when Replay starts: the replay regrows history
    /// in lockstep with the replaying frame, so LastFrame alone would pin the
    /// thumb to the right edge instead of showing replay progress.
    let mutable replayExtent = 0

    /// Recompute the mode bar under the screen from the live session state.
    /// Cheap and idempotent; called on the UI timer so every state change
    /// (Run/Pause/Replay/rec toggle/boot) shows up within ~100 ms without
    /// having to instrument each individual handler.
    let updateModeLabel () =
        let set text brush bold =
            modeLabel.Text <- text
            modeLabel.Foreground <- brush
            modeLabel.FontWeight <- if bold then FontWeights.Bold else FontWeights.Normal

        match ceGame, session with
        | Some _, _ -> set "compiled F# port - no trace recorded" dim false
        | None, Some s when s.Replaying || replaying ->
            set "\u25B6 REPLAYING saved trace (keys scripted, nothing new recorded)" orange true
        | None, Some s when s.LiveMode -> set "▶ LIVE play - no trace, nothing recorded" green true
        | None, Some s ->
            let recOn = s.Recorder.RecordEnabled

            if running then
                if recOn then
                    set "\u25CF RECORDING - live play, new trace" red true
                else
                    set "running live - not recording" dim false
            elif s.ReplayFinished then
                set "replay finished - parked at recording end" orange false
            elif recOn then
                set "paused - recording armed, press Run to capture" red false
            else
                set "paused" dim false
        | None, None -> set "booting..." dim false

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
    /// Memory-mode jump flag: gotoMemCursor recentres the window AND asks the
    /// next memory render to scroll the cursor row to 2/5 of the viewport
    /// (wheel moves and row clicks keep the viewport still instead).
    let mutable pendingMemJumpScroll = false
    /// The label row clicked in memory mode (addr, current name): Some puts
    /// the comments pane into label-editor mode and keeps the instruction
    /// row unhighlighted while the label is selected.
    let mutable labelEditor: (int * string) option = None
    /// Names of the label rows currently rendered in the memory window
    /// (addr -> name). A clicked row is a label row when its address is
    /// registered here; the map is rebuilt on every memory render.
    let labelNames = Dictionary<int, string>()

    let gotoMemCursor (addr: int) (reason: string) =
        memCursor <- addr
        memCursorReason <- reason
        memViewTop <- None // jumps recenter the window on the new cursor
        pendingMemJumpScroll <- true

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
    // flame graph pane + controls (built from the saved recording's states)
    let flameCtrl = FlameGraph(MinHeight = 70.0)

    let flameABtn =
        Button(Content = "Flame A", Width = 64.0, ToolTip = "build the flame graph for selector range A")

    let flameBBtn =
        Button(Content = "Flame B", Width = 64.0, ToolTip = "build the flame graph for selector range B")

    let flameFitBtn =
        Button(Content = "Flame fit", Width = 64.0, ToolTip = "zoom the flame graph to the built window")

    let flameAutoBtn =
        CheckBox(
            Content = "flame auto",
            IsChecked = Nullable<bool>(true),
            Foreground = normal,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "keep a flame window for the brush selection built automatically"
        )

    let syncViewsBtn =
        CheckBox(
            Content = "sync with main timeline",
            IsChecked = Nullable<bool>(false),
            Foreground = dim,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip =
                "keep the brush timeline and the flame graph at the same position and zoom (timeline wheel zooms, shift+wheel pans)"
        )

    /// Built flame windows (LRU, detail tier evicted for large windows).
    let flameCache = FlameCache()
    /// Generation counter + in-flight flag for the async flame builds. Bumping
    /// the generation makes running workers cancel and stale completions be
    /// ignored - the game switch uses it to invalidate a build started for the
    /// previous game. Declared at class level so the switch handler can reach
    /// them (buildFlameRange's wiring sits later in the file).
    let mutable flameGeneration = 0
    let mutable flameBuilding = false

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
    let memModeBtn =
        RadioButton(Content = "Memory", Width = 68.0, GroupName = "codeview", Foreground = normal)

    let execModeBtn =
        RadioButton(
            Content = "Execution",
            Width = 76.0,
            GroupName = "codeview",
            IsChecked = Nullable<bool>(true),
            Foreground = normal
        )

    let graphModeBtn =
        RadioButton(Content = "Graph", Width = 60.0, GroupName = "codeview", Foreground = normal)

    let graphAllBtn =
        ToggleButton(
            Content = "All",
            Width = 44.0,
            Foreground = normal,
            ToolTip = "Graph view: ignore the brush selection and show all blocks"
        )

    /// Code graph view surfaces (alternate rendering of the memory view).
    /// Separate from the call graph's graphCanvas: a canvas can only be
    /// hosted by one parent and each view Clears its own surface.
    let graphScroll =
        ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto)

    let codeGraphCanvas = Canvas(Background = Theme.panel)

    /// Call graph surface (Functions tab, drawGraph).
    let graphCanvas = Canvas(Background = Theme.panel)
    /// Shared comment text for all mass-comment buttons.
    let commentBox = TextBox(Width = 420.0, Height = 22.0)

    let idxCmtBtn =
        Button(Content = "idx -> cmt", ToolTip = "Comment the current instruction index (execution trace)")

    let lineCmtBtn =
        Button(
            Content = "addr -> cmt",
            ToolTip =
                "Comment the current address: the cursor in memory/graph view, or the current instruction's PC in execution view (text from the comment box)"
        )

    let nameBeforeBtn =
        Button(
            Content = "Name region before cursor",
            ToolTip = "Name the address region ending at the current instruction"
        )

    let nameABtn =
        Button(Content = "Name A", ToolTip = "Name the code region executed in selector range A")

    let nameBBtn =
        Button(Content = "Name B", ToolTip = "Name the code region executed in selector range B")

    let cmtABtn =
        Button(Content = "Comment A", ToolTip = "Add the comment text to all addresses executed in A")

    let cmtBBtn =
        Button(Content = "Comment B", ToolTip = "Add the comment text to all addresses executed in B")

    let cmtBNotA =
        Button(Content = "Comment B-only", ToolTip = "Comment addresses executed in B but NOT in A")

    let cmtANotB =
        Button(Content = "Comment A-only", ToolTip = "Comment addresses executed in A but NOT in B")

    let framesABtn =
        Button(
            Content = "Frames A",
            ToolTip =
                "Name brush A's frame range (trace states) with the comment-box text; renders as a lane under the flame graph"
        )

    let framesBBtn =
        Button(
            Content = "Frames B",
            ToolTip =
                "Name brush B's frame range (trace states) with the comment-box text; renders as a lane under the flame graph"
        )
    // engine radio group
    let oracleRadio =
        RadioButton(Content = "Oracle", GroupName = "engine", IsChecked = Nullable<bool>(true), Foreground = normal)

    let ceRadio =
        RadioButton(Content = "game.fs (CE)", GroupName = "engine", Foreground = normal)

    let diffRadio =
        RadioButton(Content = "Both + compare", GroupName = "engine", Foreground = normal)

    /// When set, the next switch to memory view centers on this address
    /// instead of the execution view's current PC ("sync to this instruction").
    let mutable pendingMemCursor: int option = None

    /// Right-click menu for a code row (list or graph surface): add a Line
    /// comment at the address (text from the shared comment box), edit or
    /// clear it, add an exec comment for the log occurrence, copy the address,
    /// or switch to memory view synced to this instruction. `refresh`
    /// re-renders whichever code surface is showing. `target` anchors the
    /// popup - without a placement target an IsOpen menu never displays.
    let openRowMenu
        (target: FrameworkElement)
        (addr: int)
        (instrIdx: int)
        (refresh: unit -> unit)
        (jumpToPrevExec: int -> unit)
        =
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
                    (if m.Kind = Line then
                         Some(ControlFile.addLine c m.Addr m.Text)
                     else
                         Some(ControlFile.upsert c m))

                refresh ()
                statusText.Text <- sprintf "comment at %04X" addr
            | None -> statusText.Text <- "no control file - click New ctrl first"

        mk (sprintf "add comment at %04X" addr) (fun _ ->
            setText
                { Kind = Line
                  Addr = addr
                  EndExcl = 0
                  InstrIndex = -1
                  Text = "" })

        if instrIdx >= 0 then
            mk (sprintf "exec comment at instruction #%d" instrIdx) (fun _ ->
                setText
                    { Kind = Exec
                      Addr = 0
                      EndExcl = 0
                      InstrIndex = instrIdx
                      Text = "" })

            mk (sprintf "jump to previous execution of %04X in trace" addr) (fun _ -> jumpToPrevExec instrIdx)

        if hasComment then
            mk (sprintf "edit comment at %04X (loads it into the comment box)" addr) (fun _ ->
                match control with
                | Some c ->
                    commentBox.Text <- ControlFile.commentAt c addr |> Option.defaultValue ""
                    commentBox.Focus() |> ignore
                    commentBox.SelectAll()
                    statusText.Text <- sprintf "editing %04X - re-apply with 'add comment' when done" addr
                | None -> ())

            mk (sprintf "clear comment at %04X" addr) (fun _ ->
                match control with
                | Some c ->
                    control <-
                        Some(
                            ControlFile.upsert
                                c
                                { Kind = Line
                                  Addr = addr
                                  EndExcl = 0
                                  InstrIndex = -1
                                  Text = "" }
                        )

                    refresh ()
                    statusText.Text <- sprintf "comment cleared at %04X" addr
                | None -> ())

        if not disasmModeMemory then
            mk "switch to memory view synced to this instruction" (fun _ ->
                pendingMemCursor <- Some addr

                if memModeBtn.IsChecked <> Nullable<bool>(true) then
                    memModeBtn.IsChecked <- Nullable<bool>(true)
                else
                    (match pendingMemCursor with
                     | Some a -> memCursor <- a
                     | None -> ())

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

    let scanWidth8 =
        RadioButton(Content = "byte 8", GroupName = "scanwidth", IsChecked = Nullable<bool>(true), Foreground = dim)

    let scanWidth16 =
        RadioButton(Content = "word 16", GroupName = "scanwidth", Foreground = dim)

    let scanRomCheck =
        CheckBox(Content = "include ROM", IsChecked = Nullable<bool>(false), Foreground = dim)

    let scanScreenCheck =
        CheckBox(Content = "include screen", IsChecked = Nullable<bool>(true), Foreground = dim)

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
        | Key.LeftShift
        | Key.RightShift -> [ (0, 0) ]
        | Key.LeftCtrl
        | Key.RightCtrl -> [ (7, 1) ]
        | Key.Left -> [ (0, 0); (3, 4) ]
        | Key.Right -> [ (0, 0); (4, 2) ]
        | Key.Up -> [ (0, 0); (4, 3) ]
        | Key.Down -> [ (0, 0); (4, 4) ]
        | _ -> []

    let isCall (b: byte) =
        match b with
        | 0xCDuy
        | 0xC4uy
        | 0xCCuy
        | 0xD4uy
        | 0xDCuy
        | 0xE4uy
        | 0xECuy
        | 0xF4uy
        | 0xFCuy -> true
        | _ -> false

    /// Signed displacement byte (JR/DJNZ operands).
    let s8 (b: byte) = int (sbyte b)

    /// Relative-jump target of the instruction at `addr`, if it is one
    /// (JR / JR cc / DJNZ). Absolute jumps and calls are not relative.
    let relJumpTarget (addr: int) (mem: byte[]) : int option =
        match mem[addr &&& 0xFFFF] with
        | 0x18uy
        | 0x20uy
        | 0x28uy
        | 0x30uy
        | 0x38uy
        | 0x10uy -> Some((addr + 2 + s8 mem[(addr + 1) &&& 0xFFFF]) &&& 0xFFFF)
        | _ -> None

    let isRet (b: byte) =
        match b with
        | 0xC9uy
        | 0xC0uy
        | 0xC8uy
        | 0xD0uy
        | 0xD8uy
        | 0xE0uy
        | 0xE8uy
        | 0xF0uy
        | 0xF8uy -> true
        | _ -> false

    let isBranch (b: byte) =
        isCall b
        || isRet b
        || match b with
           | 0xC3uy
           | 0xC2uy
           | 0xCAuy
           | 0xD2uy
           | 0xDAuy
           | 0xE2uy
           | 0xEAuy
           | 0xF2uy
           | 0xFAuy
           | 0x10uy
           | 0x18uy
           | 0x20uy
           | 0x28uy
           | 0x30uy
           | 0x38uy
           | 0xE9uy -> true
           | _ -> false

    // ---- trace plumbing ----------------------------------------------------
    let ensureBuilt () =
        match session with
        | Some s ->
            let recorder = s.Recorder

            // Build() linearizes the whole recorder ring - O(window) work its
            // docs scope to pause/save. While the machine runs the ring refills
            // as fast as it drains, so rebuilding per EntryCount change here
            // re-linearized millions of entries on every UI tick and starved
            // the 20 ms frame timer. Build once per session (so views always
            // have a trace) and otherwise only while parked; pauseGame runs
            // buildTraceNow, so taking over refreshes the window.
            if builtAtCount = -1 || not (running || replaying) then
                if builtAtCount <> recorder.EntryCount then
                    built <- Some(recorder.Build())
                    builtAtCount <- recorder.EntryCount
        | None -> ()

    let currentTrace () : Trace option =
        match flameTrace with
        | Some t -> Some t
        | None ->
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

        if n = 0 then
            cursor <- 0
        else
            cursor <- max 0 (min cursor (n - 1))

    let buildTraceNow () = ensureBuilt ()

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
        if f <= 0 || t.FrameTicks.Length = 0 then
            0
        else
            let tick = t.FrameTicks[min (t.FrameTicks.Length - 1) f]
            // Entries are time-ordered; binary search for Tick >= tick.
            let rec search lo hi =
                if lo >= hi then
                    lo
                else
                    let mid = (lo + hi) / 2

                    if t.Entries[mid].Tick < tick then
                        search (mid + 1) hi
                    else
                        search lo mid

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
            with ex ->
                // A failed load must not keep the previous game's control file: its
                // annotations would render onto this game, and saving would write
                // them into the other game's control.json.
                control <- None
                controlGameDir <- dir
                statusText.Text <- sprintf "control load failed: %s" ex.Message

    let saveControlNow () : unit =
        match control, controlGameDir with
        | Some c, dir when dir <> "" && c.Dirty ->
            try
                control <- Some(ControlFile.save dir c)
                statusText.Text <- sprintf "control saved: %s" (Path.Combine(dir, "control.json"))
            with ex ->
                statusText.Text <- sprintf "control save failed: %s" ex.Message
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
        | Some c -> gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), c.ScreenBuffer, 320 * 4, 0)
        | None ->
            match session with
            | Some s -> gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), s.ScreenBuffer, 320 * 4, 0)
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

                rows.Add
                    { Tag = tag
                      Brush = (if isErr then red else normal)
                      Tint = null
                      IsCurrent = isErr
                      Addr = addr
                      InstrIdx = -1 }

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
            sprintf "%02d:%02d / %02d:%02d" (curSeconds / 60) (curSeconds % 60) (totalSeconds / 60) (totalSeconds % 60)

    let renderFrame () =
        // Live execution leaves the parked preview: the flame window's trace no
        // longer describes what is running, so the view falls back to the ring.
        flameTrace <- None

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

    // ---- frame pacing (render-loop stepper) ---------------------------------
    // The old 20 ms DispatcherTimer sat at Background priority, and vsync-
    // paced render passes monopolize the dispatcher above it - the timer only
    // got serviced ~15x/s, so a 4 ms frame ran at 15-20 fps. Step frames from
    // the render loop instead (the classic game loop): every Rendering event
    // accumulates real time and runs one 20 ms tick per due frame, capped at
    // 4 frames so a long stall never fast-forwards the replay. `running`
    // remains the single run gate (Run/Pause/startGame/CE switch all toggle
    // it); the leftover frameTimer Start/Stop calls are inert - its Tick
    // handler is gone.
    let mutable stepAcc = 0L // Stopwatch ticks owed a 20 ms frame
    let mutable stepLast = 0L
    let stepTickTicks = int64 (0.020 * float System.Diagnostics.Stopwatch.Frequency)

    do
        System.Windows.Media.CompositionTarget.Rendering.Add(fun _ ->
            if running then
                let now = System.Diagnostics.Stopwatch.GetTimestamp()

                if stepLast <> 0L then
                    stepAcc <- stepAcc + now - stepLast
                    let mutable steps = 0

                    while stepAcc >= stepTickTicks && steps < 4 do
                        stepAcc <- stepAcc - stepTickTicks
                        steps <- steps + 1
                        renderFrame ()

                    if steps = 4 then stepAcc <- 0L // drop the backlog after a stall

                stepLast <- now
            else
                stepLast <- 0L)

    let currentSelfModified () : bool[] =
        match flameTrace with
        | Some t -> t.SelfModified
        | None ->
            match loaded with
            | Some t -> t.SelfModified
            | None ->
                match session with
                | Some s -> s.Recorder.SelfModified
                | None -> Array.zeroCreate<bool> 0x10000

    /// Live per-PC counts: the flame window's when installed, the recorder's
    /// window while playing, the loaded trace's otherwise. Never triggers a
    /// trace build (the heatmap refreshes at 10 Hz during recording;
    /// rebuilding 68 MB per tick would be ~680 MB/s).
    let currentCounts () : int[] =
        match flameTrace with
        | Some t -> t.PerPcCount
        | None ->
            match loaded with
            | Some t -> t.PerPcCount
            | None ->
                match session with
                | Some s -> s.Recorder.PerPcCount
                | None -> Array.zeroCreate<int> 0x10000


    /// Code-window branch annotation: when the instruction is a call or jump
    /// with an address operand and the control file names the target, the row
    /// text gains a bracketed symbol - `CALL 0x7308 [name]`, `JP 0x4000
    /// [start]`, `JR -5 [loop]`. Absolute targets (CD / C4-class calls,
    /// C3 / C2-class jumps) are the little-endian bytes 1-2; relative targets
    /// (JR / DJNZ, one signed byte) resolve against the instruction's own
    /// address. Register-indirect jumps (JP (HL)) have no operand and gain
    /// nothing.
    let branchAnnotation (addr: int) (b0: byte) (b1: byte) (b2: byte) : string =
        let s8 (b: byte) = int (sbyte b)
        let imm16 = (int b1) ||| ((int b2) <<< 8)

        let target =
            match b0 with
            | 0xC3uy -> Some imm16 // JP nn
            | 0xCDuy -> Some imm16 // CALL nn
            | 0x18uy
            | 0x20uy
            | 0x28uy
            | 0x30uy
            | 0x38uy -> // JR [cc], DJNZ
                Some((addr + 2 + s8 b1) &&& 0xFFFF)
            | _ ->
                if b0 &&& 0xC7uy = 0xC4uy then Some imm16 // CALL cc,nn
                elif b0 &&& 0xC7uy = 0xC2uy then Some imm16 // JP cc,nn
                else None

        match target with
        | Some t ->
            match control with
            | Some c ->
                match ControlFile.symbolAt c t with
                | Some n -> sprintf " [%s]" n
                | None -> ""
            | None -> ""
        | None -> ""

    /// The user-visible name exactly at addr (user symbol first, then a
    /// named code block starting there) with its kind. Generated jump-target
    /// labels do not count - they are window-local, not named.
    let nameAtAddr (addr: int) : (string * string) option =
        match control with
        | Some c ->
            match ControlFile.symbolAt c addr with
            | Some n -> Some(n, "user symbol")
            | None ->
                c.Blocks
                |> List.tryFind (fun b -> b.Kind = Code && b.Start = addr && b.Name <> sprintf "block_%04X" b.Start)
                |> Option.map (fun b -> b.Name, "block name")
        | None -> None

    let rowFor (t: Trace) (e: TraceEntry) (idx: int) (isCurrent: bool) : DisasmRow =
        let comment =
            match control with
            | Some c -> ControlFile.commentAt c (int e.Pc) |> Option.defaultValue ""
            | None -> ""

        // execution mode has no label rows: a named address shows its name
        // as a bracketed prefix on the instruction line instead
        let named = nameAtAddr (int e.Pc)

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
                if (currentSelfModified ())[int e.Pc] then
                    "   [self-mod]"
                else
                    ""

            let lifted =
                match Jetpac3.Core.LiftedRoutines.registryHook (int e.Pc) with
                | Some _ -> "   [lift]"
                | None -> ""
            // IDA-style merged text: a lifted step can run several instructions
            // inside one recorded step (only the entry's first-instruction bytes
            // were fetched into the row), so show the whole folded sequence.
            let text =
                match Jetpac3.Core.LiftedRoutines.folded (int e.Pc) with
                | Some insns -> String.concat "; " insns
                | None -> insn.Text

            let text = text + branchAnnotation (int e.Pc) e.B0 e.B1 e.B2

            let brush =
                if isCall e.B0 then cyan
                elif isRet e.B0 then yellow
                elif isBranch e.B0 then green
                else normal

            let cm = if comment <> "" then sprintf "  ; %s" comment else ""

            // a named function start shows its name at the end of the line,
            // after the comment
            let nm =
                named |> Option.map (fun (n, _) -> sprintf "  [%s]" n) |> Option.defaultValue ""

            { Tag = sprintf "%07d  %04X  %-11s %3dt  %s%s%s%s%s%s" idx e.Pc hex (int e.Cycles) text tail sm lifted cm nm
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
        | Some c when c.Blocks |> List.exists (fun b -> b.Kind = Code) ->
            (Set.empty, c.Blocks)
            ||> List.fold (fun acc b ->
                if b.Kind = Code then
                    let mutable s = acc
                    for a = b.Start to b.EndExcl - 1 do
                        s <- Set.add a s
                    s
                else
                    acc)
        | _ -> Set.empty

    /// Active-brush tints for the code view (match the timeline chip colors).
    let tintA = SolidColorBrush(Color.FromArgb(0x30uy, 0x38uy, 0xBDuy, 0xF8uy))
    let tintB = SolidColorBrush(Color.FromArgb(0x30uy, 0xFBuy, 0xBFuy, 0x24uy))

    /// The address set of the ACTIVE brush: PCs executed inside the brush's
    /// frame range. None when there is no trace yet or the range is empty.
    /// Cached against (brush, range, trace): pcsInFrames scans EVERY trace
    /// entry, so recomputing it per row made the graph view freeze the UI
    /// thread (rows x trace-entries iterations).
    let mutable activeSelCache: (BrushId * int * int * Trace * (BrushId * Set<int>) option) option =
        None

    let activeSelection () : (BrushId * Set<int>) option =
        match currentTrace () with
        | Some t when t.Entries.Length > 0 ->
            let which = timeline.ActiveBrush
            let fA, fB = rangeOf which

            match activeSelCache with
            | Some(w, a, b, t2, v) when w = which && a = fA && b = fB && obj.ReferenceEquals(t2, t) -> v
            | _ ->
                let pcs = ControlFile.pcsInFrames t fA fB

                let v =
                    if List.isEmpty pcs then
                        None
                    else
                        Some(which, Set.ofList pcs)

                activeSelCache <- Some(which, fA, fB, t, v)
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

                let executed =
                    [ for a in 0..0xFFFF do
                          if counts[a] > 0 then
                              a ]

                let execBox =
                    if List.isEmpty executed then
                        None
                    else
                        Some(List.min executed, List.max executed + 1)

                let selBox =
                    match activeSelection () with
                    | Some(_, s) when not (Set.isEmpty s) -> Some(Set.minElement s, Set.maxElement s + 1)
                    | _ -> None

                let controlBox =
                    match control with
                    | Some c when c.Blocks |> List.exists (fun b -> b.Kind = Code) ->
                        let lo, hi =
                            ((Int32.MaxValue, Int32.MinValue), c.Blocks)
                            ||> List.fold (fun (lo, hi) b ->
                                if b.Kind = Code then
                                    (min lo b.Start, max hi b.EndExcl)
                                else
                                    (lo, hi))

                        Some(lo, hi)
                    | _ -> None

                let overlaps (a1, b1) (a2, b2) = a1 < b2 && a2 < b1

                let useControl =
                    match controlBox with
                    | None -> false
                    | Some cb ->
                        (match execBox with
                         | Some eb -> overlaps cb eb
                         | None -> false)
                        || (match selBox with
                            | Some sb -> overlaps cb sb
                            | None -> false)

                let present =
                    [ execBox; selBox; (if useControl then controlBox else None) ] |> List.choose id

                let start, endExcl =
                    if present.IsEmpty then
                        0x4000, 0x10000
                    else
                        max 0x4000 ((present |> List.map fst |> List.min) - 0x200),
                        min 0x10000 ((present |> List.map snd |> List.max) + 0x200)

                let start, endExcl = if start < endExcl then start, endExcl else 0x4000, 0x10000
                flowCache <- Some(Z80Flow.splitBlocks mem start endExcl (fun _ -> true))

            flowCacheAt <- DateTime.Now

        flowCache |> Option.defaultValue []

    /// The address universe per scan options.
    let scanUniverse () : int[] =
        match currentMemory () with
        | None -> [||]
        | Some mem ->
            let lo =
                if scanRomCheck.IsChecked.GetValueOrDefault() then
                    0
                else
                    0x4000

            let screenHi = 0x5B00

            seq {
                for a in lo .. (mem.Length - 1) do
                    let inScreen = a >= 0x4000 && a < screenHi

                    if inScreen && not (scanScreenCheck.IsChecked.GetValueOrDefault()) then
                        ()
                    else
                        yield a
            }
            |> Seq.toArray

    let refreshScanList () : unit =
        match currentMemory () with
        | None -> scanCountLabel.Text <- "no live session - cannot scan"
        | Some _ when List.isEmpty scanResults ->
            scanList.ItemsSource <- ([] : DisasmRow list)
            scanCountLabel.Text <- "0 addresses"
        | Some mem ->
            let rows =
                scanResults
                |> List.truncate 2000
                |> List.map (fun a ->
                    let prev =
                        match scanBaseline with
                        | Some b when a < b.Length -> b[a]
                        | _ -> 0uy

                    { Tag =
                        sprintf "%04X  %02X%s" a mem[a] (if prev <> mem[a] then sprintf "  (was %02X)" prev else "")
                      Brush = normal
                      Tint = null
                      IsCurrent = false
                      Addr = a
                      InstrIdx = -1 })

            scanList.ItemsSource <- rows

            scanCountLabel.Text <-
                sprintf
                    "%d addresses%s"
                    scanResults.Length
                    (if scanResults.Length > 2000 then
                         " (showing first 2000)"
                     else
                         "")

    let startScan () : unit =
        match currentMemory () with
        | None -> statusText.Text <- "start a game first - scans need live memory"
        | Some mem ->
            scanBaseline <- Some(Array.copy mem)

            scanBaselineFrame <-
                match session with
                | Some s -> s.Frame
                | None -> -1

            scanResults <- scanUniverse () |> Array.toList

            for b in
                [ scanChangedBtn
                  scanSameBtn
                  scanExactBtn
                  scanSmallerBtn
                  scanLargerBtn
                  scanWrittenBtn
                  scanExecutedBtn ] do
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
                if t.FrameTicks.Length > scanBaselineFrame then
                    t.FrameTicks[scanBaselineFrame]
                else
                    0u

            t.Writes
            |> Array.filter (fun w -> w.Tick >= tick)
            |> Seq.map (fun w -> int w.Address)
            |> Set.ofSeq
        | _ -> Set.empty

    /// hiBg as a brush for the memory-mode cursor row (shared Theme instance).
    let hiBgBrush = Theme.hiBg
    /// Foreground for the cursor row: contrasts HiBg in both modes.
    let bright = Theme.bright

    /// One row of the linear memory sweep. The active-brush selection is
    /// passed in by the caller (computed once per refresh, never per row).
    /// `labelFor` resolves jump-target labels for the window being rendered:
    /// a relative jump whose target has a label gains ` [label_71B0]` (or the
    /// user symbol's name).
    let memRowFor
        (mem: byte[])
        (addr: int)
        (sel: (BrushId * Set<int>) option)
        (labelFor: int -> string option)
        : DisasmRow =
        let insn = Disasm.disasmMemory mem addr

        let hex =
            [ for i in 0 .. insn.Length - 1 -> sprintf "%02X" mem[(addr + i) &&& 0xFFFF] ]
            |> String.concat " "

        let comment =
            match control with
            | Some c -> ControlFile.commentAt c addr |> Option.defaultValue ""
            | None -> ""

        let cm = if comment <> "" then sprintf "  ; %s" comment else ""

        let ann =
            branchAnnotation addr mem[addr] mem[(addr + 1) &&& 0xFFFF] mem[(addr + 2) &&& 0xFFFF]

        let jr =
            relJumpTarget addr mem
            |> Option.bind labelFor
            |> Option.map (sprintf " [%s]")
            |> Option.defaultValue ""

        let tint =
            match sel with
            | Some(which, s) when s.Contains addr ->
                (match which with
                 | A -> tintA :> Brush
                 | B -> tintB :> Brush)
            | _ -> null

        { Tag = sprintf "  %04X  %-11s  %s%s%s%s" addr hex insn.Text jr ann cm
          Brush =
            if addr = memCursor then bright
            elif (currentCounts ())[addr] > 0 then cyan
            else normal
          Tint = tint
          // while a label is selected, the instruction row stays dim: the
          // label row is the highlighted one
          IsCurrent = addr = memCursor && labelEditor.IsNone
          Addr = addr
          InstrIdx = -1 }

    /// A label line placed before the row it names: function starts render as
    /// `fun_73B2:` (or the user symbol), jump targets as `label_71B0:`. The
    /// label being edited in the comments pane is the highlighted row.
    let labelRowFor (a: int) (name: string) : DisasmRow =
        let isEdited =
            match labelEditor with
            | Some(ea, _) -> ea = a
            | None -> false

        { Tag = sprintf "  %04X         %s:" a name
          Brush = (if name.StartsWith "label_" then dim else cyan)
          Tint = null
          IsCurrent = isEdited
          Addr = a
          InstrIdx = -1 }

    /// Labels for one memory-mode window: user symbols and code-block starts
    /// become function labels (`fun_73B2` unless named), and every relative
    /// jump target found in the window becomes `label_XXXX`. Registration
    /// order gives precedence: symbol > block start > jump target.
    let windowLabels (addrs: ResizeArray<int>) (targets: int list) : (int -> string option) =
        let dict = System.Collections.Generic.Dictionary<int, string>()

        let register a name =
            if dict.ContainsKey a |> not then
                dict[a] <- name

        match control with
        | Some c ->
            for (a, n) in c.Symbols do
                register a n

            for b in c.Blocks do
                if b.Kind = Code then
                    let n =
                        match ControlFile.symbolAt c b.Start with
                        | Some s -> s
                        | None ->
                            if b.Name.StartsWith "block_" then
                                sprintf "fun_%04X" b.Start
                            else
                                b.Name

                    register b.Start n
        | None -> ()

        for t in targets do
            register t (sprintf "label_%04X" t)

        fun a ->
            match dict.TryGetValue a with
            | true, n -> Some n
            | _ -> None

    /// Cached "byte is an instruction start" bitmap for the control map,
    /// rebuilt lazily but at most once per second (self-modifying code
    /// shifts boundaries; a 64K decode sweep is a few ms).
    let mutable instrStartsCache: bool[] option = None
    let mutable instrStartsBuiltAt = DateTime.MinValue

    let getInstrStarts () : bool[] option =
        match currentMemory () with
        | None -> None
        | Some mem ->
            if
                instrStartsCache.IsNone
                || (DateTime.Now - instrStartsBuiltAt).TotalMilliseconds > 1000.0
            then
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

        if
            obj.ReferenceEquals(mem, lastGraphMem)
            && lastGraphSel = Some sel
            && lastGraphAll = disasmGraphAll
            && obj.ReferenceEquals(control, lastGraphControl)
            && epoch = lastGraphEpoch
        then
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
                        | Some(_, s) ->
                            flowBlocks ()
                            |> List.filter (fun b -> Set.exists (fun a -> a >= b.Start && a < b.EndExcl) s)
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
                            | Some c ->
                                // Only a control block starting EXACTLY at the node names
                                // it - a containing span would title every node with its
                                // own name (the old whole-span behaviour).
                                c.Blocks
                                |> List.tryFind (fun bl ->
                                    bl.Start = b.Start && bl.Name <> sprintf "block_%04X" bl.Start)
                                |> Option.map (fun bl -> bl.Name)
                                |> Option.defaultValue ""
                            | None -> ""

                        let p = StackPanel()

                        let header =
                            TextBlock(
                                Text = sprintf "  %04X  %s" b.Start name,
                                FontFamily = mono,
                                FontSize = 11.5,
                                Foreground = cyan,
                                Background = hiBgBrush,
                                Height = headerH - 2.0
                            )

                        p.Children.Add header |> ignore
                        let mutable a = b.Start

                        while a < b.EndExcl do
                            let row = memRowFor mem a sel (fun _ -> None)

                            let tb =
                                TextBlock(
                                    Text = row.Tag,
                                    FontFamily = mono,
                                    FontSize = 11.5,
                                    Foreground = row.Brush,
                                    Background = row.Tint,
                                    Height = rowH
                                )

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
                            pl.Points.Add(Point(px, py))

                        codeGraphCanvas.Children.Add pl |> ignore
                        let head = Polygon(Fill = color, Points = PointCollection())
                        let d = if y2 >= y1 then 5.0 else -5.0
                        head.Points.Add(Point(x2 - 3.5, y2 - d))
                        head.Points.Add(Point(x2 + 3.5, y2 - d))
                        head.Points.Add(Point(x2, y2))
                        codeGraphCanvas.Children.Add head |> ignore

                    let mutable railSlot = 0

                    for i in 0 .. blocks.Length - 1 do
                        let b = blocks[i]

                        match b.Ends with
                        | Z80Flow.Linear -> ()
                        | Z80Flow.Return -> ()
                        | Z80Flow.Jump None
                        | Z80Flow.Branch None -> ()
                        | kind ->
                            let target =
                                match kind with
                                | Z80Flow.Jump(Some t)
                                | Z80Flow.Branch(Some t)
                                | Z80Flow.Call(Some t) -> t
                                | _ -> -1

                            if target >= 0 && blockIndex.ContainsKey target then
                                let j = blockIndex[target]
                                let color = edgeColor kind
                                let fromY = bottoms[i]
                                let toY = tops[j] + 9.0

                                if j = i + 1 then
                                    // adjacent fall-through: short straight arrow
                                    let l =
                                        System.Windows.Shapes.Line(
                                            X1 = leftX + blockW / 2.0,
                                            Y1 = fromY,
                                            X2 = leftX + blockW / 2.0,
                                            Y2 = toY,
                                            Stroke = color,
                                            StrokeThickness = 1.2
                                        )

                                    codeGraphCanvas.Children.Add l |> ignore

                                    addElbow
                                        (leftX + blockW / 2.0)
                                        fromY
                                        (leftX + blockW / 2.0)
                                        toY
                                        (leftX + blockW / 2.0)
                                        color
                                else
                                    let rail = railRight + 12.0 + 14.0 * float (railSlot % 6)
                                    railSlot <- railSlot + 1
                                    addElbow railRight fromY leftX toY rail color

                    disasmTarget.Text <-
                        match sel with
                        | Some(which, _) ->
                            sprintf
                                "graph view: %d blocks (brush %s)"
                                blocks.Length
                                (match which with
                                 | A -> "A"
                                 | B -> "B")
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

    /// When set, the next setRows call scrolls this row index to the given
    /// fraction of the viewport (0 = top, 1 = bottom) instead of restoring the
    /// previous offset. Consumed by that call - jumps request it, wheel and
    /// row-click renders do not. The ListBox scrolls by items, so both the row
    /// index and ViewportHeight are in rows.
    let mutable pendingScrollToRow: (int * float) option = None

    /// Swap the code window's rows while keeping the scroll offset stable.
    /// Assigning a fresh ItemsSource regenerates every container and resets the
    /// ScrollViewer to the top; refreshDisasm runs off the 30 ms cinema timer
    /// and every scrub interaction, which made any scroll attempt snap straight
    /// back. Same row count => same window shape, so re-apply the old offset
    /// once the new containers are laid out (clamped by the ScrollViewer) -
    /// unless a jump asked for the current row to be placed instead.
    let setRows (rows: ResizeArray<DisasmRow>) =
        let sv = findScroll ()

        let oldOffset =
            sv |> Option.map (fun s -> s.VerticalOffset) |> Option.defaultValue 0.0

        let oldCount = disasmList.Items.Count
        disasmList.ItemsSource <- rows

        match pendingScrollToRow with
        | Some(rowIdx, frac) ->
            pendingScrollToRow <- None

            sv
            |> Option.iter (fun s ->
                disasmList.Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    Action(fun () ->
                        let target = float rowIdx - frac * s.ViewportHeight
                        s.ScrollToVerticalOffset(max 0.0 (min s.ScrollableHeight target)))
                )
                |> ignore)
        | None ->
            if oldCount = rows.Count && oldOffset > 0.0 then
                sv
                |> Option.iter (fun s ->
                    disasmList.Dispatcher.BeginInvoke(
                        DispatcherPriority.Loaded,
                        Action(fun () -> s.ScrollToVerticalOffset oldOffset)
                    )
                    |> ignore)

    let applyViewMode () =
        disasmList.Visibility <-
            if disasmModeGraph then
                Visibility.Collapsed
            else
                Visibility.Visible

        graphScroll.Visibility <-
            if disasmModeGraph then
                Visibility.Visible
            else
                Visibility.Collapsed

    /// Frame number (1-based) of the trace entry at `idx`, derived from the
    /// recorded frame-boundary ticks: boundaries are the end-of-frame cycle
    /// counts, so an entry belongs to the frame after the last boundary at or
    /// before its start tick. 0 when the trace carries no boundaries.
    let frameOfEntry (t: Trace) (idx: int) : int =
        if t.FrameTicks.Length = 0 then
            0
        else
            let tick = t.Entries[idx].Tick
            let mutable f = 0

            while f < t.FrameTicks.Length && t.FrameTicks[f] <= tick do
                f <- f + 1

            f + 1

    /// Sync the flame graph's execution cursor to the code pane's cursor. Exact
    /// when the pane browses the flame window's own entries (they share the
    /// window's index space; EntryTicks are window-relative), frame-mapped for
    /// the live/loaded traces, and hidden whenever the cursor's frame lies
    /// outside the built window. Cheap: the setter only invalidates on change.
    let updateFlameCursor () =
        if disasmModeMemory || disasmModeGraph then
            flameCtrl.CursorTick <- -1L
        else
            match flameCtrl.Window, currentTrace () with
            | Some w, Some t when t.Entries.Length > 0 && w.FirstFrame >= 0 ->
                let c = max 0 (min cursor (t.Entries.Length - 1))

                let exact =
                    match flameTrace, w.EntryTicks with
                    | Some ft, Some ticks when System.Object.ReferenceEquals(ft, t) && c < ticks.Length -> Some ticks[c]
                    | _ -> None

                match exact with
                | Some tick -> flameCtrl.CursorTick <- tick
                | None ->
                    let f = frameOfEntry t c

                    if f >= w.FirstFrame && f - w.FirstFrame < w.FrameTicks.Length then
                        // Window tick 0 is the start of the window's first frame; frame
                        // f starts where frame f - 1 ends. The entry's offset inside its
                        // frame comes from the trace's own boundary table (mod 2^32, so
                        // a wrapped uint32 cycle counter still yields a forward delta).
                        let frameStart =
                            if f = w.FirstFrame then
                                0L
                            else
                                w.FrameTicks[f - 1 - w.FirstFrame]

                        let prevBoundary =
                            if f >= 2 && f - 2 < t.FrameTicks.Length then
                                int64 t.FrameTicks[f - 2]
                            else
                                int64 t.StartTick

                        let offset = (int64 t.Entries[c].Tick - prevBoundary) &&& 0xFFFFFFFFL
                        flameCtrl.CursorTick <- min w.EndTick (frameStart + offset)
                    else
                        flameCtrl.CursorTick <- -1L
            | _ -> flameCtrl.CursorTick <- -1L

    let rec refreshDisasm () =
        // Consume a pending memory jump once per render: only the memory branches
        // below use it (a gotoMemCursor between renders must not leak into a
        // later render after a mode switch).
        let memJump = pendingMemJumpScroll
        pendingMemJumpScroll <- false

        if disasmModeGraph then
            // the graph build can own the thread for minutes on wide spans; show
            // the busy cursor for its duration
            let prevCursor = Mouse.OverrideCursor
            Mouse.OverrideCursor <- Cursors.Wait
            // pump one Render-priority pass so the busy cursor actually shows
            // before the build wedges the thread; queued input sits at a lower
            // priority, so nothing re-enters mid-build
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Render, System.Action(fun () -> ()))
            |> ignore

            try
                buildGraph (fun addr -> openRowMenu codeGraphCanvas addr -1 refreshDisasm ignore)
            finally
                Mouse.OverrideCursor <- prevCursor
        elif disasmModeMemory then
            match currentMemory () with
            | None ->
                disasmList.ItemsSource <- null
                disasmTarget.Text <- "target: ----"
            | Some mem ->
                match activeSelection () with
                | Some(which, sel) ->
                    // Selected blocks: every block touching the selection, decoded
                    // in full, tinted where the address is selected. Scrollable.
                    let blocks =
                        flowBlocks ()
                        |> List.filter (fun b -> Set.exists (fun a -> a >= b.Start && a < b.EndExcl) sel)

                    let rows = ResizeArray<DisasmRow>()

                    for b in blocks do
                        let mutable a = b.Start

                        while a < b.EndExcl do
                            rows.Add(memRowFor mem a (Some(which, sel)) (fun _ -> None))
                            a <- a + max 1 (Disasm.disasmMemory mem a).Length
                    // A memory jump places the cursor row at 2/5 of the viewport (the
                    // selection list is long; without this the target can sit outside
                    // it entirely).
                    pendingScrollToRow <-
                        if memJump then
                            rows |> Seq.tryFindIndex (fun r -> r.IsCurrent) |> Option.map (fun i -> i, 0.4)
                        else
                            None

                    setRows rows

                    let lo =
                        blocks
                        |> List.map (fun b -> b.Start)
                        |> function
                            | [] -> 0x10000
                            | xs -> List.min xs

                    let hi =
                        blocks
                        |> List.map (fun b -> b.EndExcl)
                        |> function
                            | [] -> 0
                            | xs -> List.max xs

                    disasmTarget.Text <-
                        sprintf
                            "target: 0x%04X (memory view, brush %s: %d blocks 0x%04X-0x%04X)"
                            memCursor
                            (match which with
                             | A -> "A"
                             | B -> "B")
                            blocks.Length
                            lo
                            hi
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
                                starts
                                |> List.tryPick (fun start ->
                                    let bounds = ResizeArray<int>()
                                    let mutable a = start
                                    let mutable aligned = false

                                    while a < memCursor do
                                        let next = a + max 1 (Disasm.disasmMemory mem a).Length

                                        if next = memCursor then
                                            aligned <- true

                                        if next <= memCursor then
                                            bounds.Add a

                                        a <- next

                                    if aligned && bounds.Count >= 20 then
                                        Some bounds[bounds.Count - 20]
                                    elif aligned && bounds.Count > 0 then
                                        Some bounds[0]
                                    elif aligned then
                                        Some memCursor
                                    else
                                        None)

                            match candidate with
                            | Some t -> t
                            | None -> memCursor

                    memViewTop <- Some memTop
                    // Pass 1: collect the window's instruction addresses plus every
                    // relative-jump target - those become label_XXXX labels. Pass 2
                    // emits label lines before the rows they name.
                    let addrs = ResizeArray<int>()
                    let targets = ResizeArray<int>()
                    let mutable cur = memTop

                    while addrs.Count < 41 do
                        addrs.Add cur

                        (match mem[cur] with
                         | 0x18uy
                         | 0x20uy
                         | 0x28uy
                         | 0x30uy
                         | 0x38uy
                         | 0x10uy -> targets.Add(relJumpTarget cur mem |> Option.defaultValue cur)
                         | _ -> ())

                        cur <- (cur + max 1 (Disasm.disasmMemory mem cur).Length) &&& 0xFFFF

                    let labelFor = windowLabels addrs (List.ofSeq targets)
                    labelNames.Clear()
                    let mutable emitted = 0
                    let mutable i = 0

                    while emitted < 41 && i < addrs.Count do
                        let a = addrs[i]
                        i <- i + 1

                        match labelFor a with
                        | Some name ->
                            labelNames[a] <- name
                            rows.Add(labelRowFor a name)
                            emitted <- emitted + 1
                        | None -> ()

                        rows.Add(memRowFor mem a None labelFor)
                        emitted <- emitted + 1
                    // A memory jump places the cursor row at 2/5 of the viewport;
                    // wheel moves and row clicks keep the viewport still.
                    pendingScrollToRow <-
                        if memJump then
                            rows |> Seq.tryFindIndex (fun r -> r.IsCurrent) |> Option.map (fun i -> i, 0.4)
                        else
                            None

                    setRows rows
                    // DESYNC probe, independent of the window: is the cursor itself an
                    // instruction start in the linear decode around it?
                    let alignedAt (a: int) =
                        if a <= 0 then
                            true
                        else
                            let floor = max 0 (a - 0x44)

                            [ floor - 4; floor - 3; floor - 2; floor - 1; floor ]
                            |> List.filter (fun s -> s >= 0)
                            |> List.exists (fun start ->
                                let mutable p = start
                                let mutable hit = false

                                while p < a do
                                    let next = p + max 1 (Disasm.disasmMemory mem p).Length

                                    if next = a then
                                        hit <- true

                                    p <- next

                                hit)

                    let desync = memCursor > 0 && not (alignedAt memCursor)

                    disasmTarget.Text <-
                        if desync then
                            sprintf
                                "target: 0x%04X (memory view) - code DESYNC detected: cursor is mid-instruction in this sweep (cursor set by: %s)"
                                memCursor
                                memCursorReason
                        else
                            sprintf "target: 0x%04X (memory view)" memCursor
        else
            match currentTrace () with
            | None ->
                disasmList.ItemsSource <- null
                disasmTarget.Text <- "target: ----"
            | Some t ->
                if t.Entries.Length = 0 then
                    execViewHold <- false
                    disasmList.ItemsSource <- null
                    disasmTarget.Text <- "target: ----"
                else
                    let c = max 0 (min cursor (t.Entries.Length - 1))
                    // Window center: after a row click the held center keeps the
                    // content still while only the bright row moves; any other cursor
                    // move renders without the hold and recenters on the cursor.
                    let held = execViewHold

                    let winCenter =
                        if held then
                            max 0 (min execViewCenter (t.Entries.Length - 1))
                        else
                            c

                    execViewHold <- false
                    execViewCenter <- winCenter
                    let rows = ResizeArray<DisasmRow>()
                    let mutable lastIdx = -1
                    // Repeated instructions (LDIR blocks, HALT/idle loops) can run for
                    // thousands of trace entries and push everything else out of the
                    // window. Runs of 3+ identical entries (same PC, same bytes)
                    // collapse into one row - "repeated N times" with the iterations'
                    // total t-states - while the run's LAST iteration always renders
                    // normally, so its own t-count stays readable.
                    let sameRun (a: TraceEntry) (b: TraceEntry) =
                        a.Pc = b.Pc
                        && a.Length = b.Length
                        && a.B0 = b.B0
                        && a.B1 = b.B1
                        && a.B2 = b.B2
                        && a.B3 = b.B3

                    let mutable j = -20

                    while j <= 20 do
                        let idx = winCenter + j

                        if idx < 0 || idx >= t.Entries.Length then
                            j <- j + 1
                        else
                            let e = t.Entries[idx]
                            let mutable s = idx

                            while s > 0 && sameRun t.Entries[s - 1] e do
                                s <- s - 1

                            let mutable e2 = idx

                            while e2 < t.Entries.Length - 1 && sameRun t.Entries[e2 + 1] e do
                                e2 <- e2 + 1

                            if e2 - s + 1 >= 3 && idx < e2 then
                                let count = e2 - s
                                let mutable totalT = 0L

                                for k in s .. e2 - 1 do
                                    totalT <- totalT + int64 t.Entries[k].Cycles

                                let anchor = rowFor t e s (c >= s && c < e2)

                                rows.Add
                                    { anchor with
                                        Tag = anchor.Tag + sprintf "  <<< repeated %d times, %dT total >>>" count totalT
                                        Brush = orange }

                                lastIdx <- s
                                // Resume at the run's last entry: the next iteration renders
                                // it as a normal row (and when the run extends past the
                                // window edge, the collapsed row already covered the rest).
                                j <- e2 - winCenter
                            else
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

                                rows.Add(rowFor t e idx (idx = c))
                                lastIdx <- idx

                            j <- j + 1
                    // When the window reaches the log's end, make the boundary
                    // explicit: seeks into the recorded future pin here because
                    // nothing has executed past this point yet.
                    if lastIdx = t.Entries.Length - 1 then
                        let extent =
                            if t.FrameTicks.Length > 0 then
                                // frameOfEntry reads session frames even from the padded
                                // flame-trace boundary table
                                sprintf "frame %d" (frameOfEntry t (t.Entries.Length - 1))
                            else
                                sprintf "%d entries" t.Entries.Length

                        rows.Add
                            { Tag = sprintf "  ---------- end of executed log (%s) ----------" extent
                              Brush = dim
                              Tint = null
                              IsCurrent = false
                              Addr = -1
                              InstrIdx = -1 }
                    // Jumps (any recentering render - slider, keys, seeks, flame/heatmap
                    // navigation) place the current row at 2/5 of the viewport: two
                    // fifths of the visible rows above the target, three fifths below,
                    // so more of the following code stays readable. Held renders (row
                    // clicks, wheel) keep the viewport still instead.
                    pendingScrollToRow <-
                        if held then
                            None
                        else
                            rows |> Seq.tryFindIndex (fun r -> r.IsCurrent) |> Option.map (fun i -> i, 0.4)

                    setRows rows
                    let fr = frameOfEntry t c

                    disasmTarget.Text <-
                        if fr = 0 then
                            sprintf "target: 0x%04X (instruction #%d)" (int t.Entries[c].Pc) c
                        else
                            sprintf "target: 0x%04X (instruction #%d frame %d)" (int t.Entries[c].Pc) c fr

        syncMapData ()
        updateFlameCursor ()

    let updateFlags (af: int) =
        let f = af &&& 0xFF
        let bits = [| 0x80; 0x40; 0x20; 0x10; 0x08; 0x04; 0x02; 0x01 |]

        for i in 0..7 do
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
        setReg "IX" (int (r.Ix()))
        setReg "IY" (int (r.Iy()))
        setReg "SP" (int (r.Sp()))
        setReg "I" (int (r.I()))
        setReg "R" (int (r.R()))
        let pc = (int (r.Pc())) &&& 0xFFFF
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
        flameCtrl.PlayheadFrame <- s.Frame

    let refreshHeatmap () =
        let counts = currentCounts ()
        let selfMod = currentSelfModified ()
        let maxC = max 1 (Array.max counts)
        let logMax = log10 (float maxC)
        let mutable i = 0

        for y in 0..255 do
            for x in 0..255 do
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
            match flameTrace with
            | Some t -> segmentsOf t
            | None ->
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

                for sy in 0..23 do
                    let p = (sy * 512 + sx) * 4
                    stripPixels[p] <- b
                    stripPixels[p + 1] <- g
                    stripPixels[p + 2] <- r
                    stripPixels[p + 3] <- 255uy

            let n = currentEntryCount ()

            if n > 1 then
                // Clamp before projecting: cursor can legitimately be stale (a game
                // switch keeps it while the new session's trace is still tiny), and
                // an unclamped cx indexes far outside the 512-wide bitmap.
                let c = max 0 (min cursor (n - 1))
                let cx = int (int64 c * 511L / int64 (n - 1))

                for sy in 0..23 do
                    let p = (sy * 512 + cx) * 4
                    stripPixels[p] <- 255uy
                    stripPixels[p + 1] <- 255uy
                    stripPixels[p + 2] <- 255uy
                    stripPixels[p + 3] <- 255uy

            stripBmp.WritePixels(Int32Rect(0, 0, 512, 24), stripPixels, 512 * 4, 0)
        // The T-range label needs a built trace, but currentTrace() can
        // linearize the recorder's whole ring - while the machine runs that
        // ring refills as fast as it drains, so a per-tick rebuild here cost
        // hundreds of ms and starved the frame timer. While live, describe
        // the last built window instead (pauseGame rebuilds on taking over).
        let stripTrace = if running || replaying then built else currentTrace ()

        match stripTrace with
        | Some t when t.Entries.Length > 0 ->
            let firstT = int64 t.Entries[0].Tick
            let lastE = t.Entries[t.Entries.Length - 1]
            let lastT = int64 lastE.Tick + int64 lastE.Cycles
            let span = max 0L (lastT - firstT)

            let frames =
                if t.FrameTicks.Length > 0 then
                    sprintf ", %d frames" t.FrameTicks.Length
                else
                    ""

            traceRangeLabel.Text <-
                sprintf
                    "T %s → %s  (%sT = %.1f ms)  %d instrs%s"
                    (firstT.ToString "N0")
                    (lastT.ToString "N0")
                    (span.ToString "N0")
                    (float span / 3500.0)
                    t.Entries.Length
                    frames
        | _ -> traceRangeLabel.Text <- "T ----"

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

        for p in 0..4 .. gfxPixels.Length - 4 do
            gfxPixels[p] <- 0uy
            gfxPixels[p + 1] <- 0uy
            gfxPixels[p + 2] <- 0uy
            gfxPixels[p + 3] <- 255uy

        match currentMemory () with
        | Some mem ->
            for block in 0 .. blocks - 1 do
                for row in 0..255 do
                    for byteInRow in 0 .. bytesPerRow - 1 do
                        let i = (block * 256 + row) * bytesPerRow + byteInRow
                        let bits = mem[(baseAddr + i) &&& 0xFFFF]
                        let px = block * w + byteInRow * 8

                        for col in 0..7 do
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

        if n = 0 then
            cursorLabel.Text <- "no trace yet - play the game"
        else
            let c = max 0 (min cursor (n - 1))
            cursorLabel.Text <- sprintf "instr %d / %d   pc=%04X" c (n - 1) (int currentTrace().Value.Entries[c].Pc)

    let refreshAll () =
        refreshDisasm ()
        refreshRegs ()
        refreshHeatmap ()
        refreshStrip ()
        refreshCursorLabel ()

    /// Day/night switch for the whole window: shared brushes, custom canvases,
    /// data repaint, immediate gui.cfg persist. Both the menu toggle and the
    /// emulator toolbar toggle route through here.
    let applyTheme (m: Theme.Mode) =
        Theme.apply m
        flameCtrl.RefreshTheme()
        timeline.RefreshTheme()
        controlMap.RefreshTheme()
        refreshAll ()
        GuiConfig.save ()

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
                    sprintf
                        "%04X  calls=%5d  span=%04X-%04X  incl=%7d  excl=%7d  %s"
                        r.Entry
                        r.CallCount
                        r.SpanLo
                        r.SpanHi
                        r.InclusiveTStates
                        r.ExclusiveTStates
                        r.Risk
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
            sprintf
                "%04X  (%s)\ncalls=%d  span=%04X-%04X\nincl=%d  excl=%d tstates\nself-modifying=%b  overlapping=%b\n%s%s\ninput samples: %d"
                r.Entry
                r.Risk
                r.CallCount
                r.SpanLo
                r.SpanHi
                r.InclusiveTStates
                r.ExclusiveTStates
                r.SelfModifying
                r.Overlapping
                (r.Reasons |> String.concat "; ")
                (if List.isEmpty loops then
                     ""
                 else
                     "\nloops: " + String.concat ", " loops)
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
                    if e.Length <> 0uy then
                        cyc[int e.Pc] <- cyc[int e.Pc] + int64 e.Cycles

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
                    sprintf
                        "contract for 0x%04X  span=%04X-%04X  calls=%d  writes=%d"
                        r.Entry
                        r.SpanLo
                        r.SpanHi
                        r.CallCount
                        contract.WriteRanges.Length
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
            graphCanvas.Children.Add(TextBlock(Text = "mine a trace first (Functions tab)", Foreground = dim))
            |> ignore
        else
            // BFS-ish depth assignment from the synthetic root (-1).
            let depth = Dictionary<int, int>()
            depth[-1] <- 0

            for r in minedRoutines do
                depth[r.Entry] <- 100

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
                kv.Value
                |> Seq.iteri (fun row entry ->
                    pos[entry] <- Point(20.0 + float kv.Key * colWidth, 20.0 + float row * rowHeight))

            pos[-1] <- Point(20.0, 20.0)
            // edges under nodes
            for e in minedEdges do
                match pos.TryGetValue e.Caller, pos.TryGetValue e.Callee with
                | (true, p1), (true, p2) ->
                    let line =
                        System.Windows.Shapes.Line(X1 = p1.X + 104.0, Y1 = p1.Y + 14.0, X2 = p2.X, Y2 = p2.Y + 14.0)

                    line.Stroke <- dim
                    line.StrokeThickness <- 1.0
                    graphCanvas.Children.Add line |> ignore

                    let label =
                        TextBlock(Text = string e.Count, Foreground = dim, FontSize = 10.0, FontFamily = mono)

                    Canvas.SetLeft(label, (p1.X + p2.X) / 2.0)
                    Canvas.SetTop(label, (p1.Y + p2.Y) / 2.0)
                    graphCanvas.Children.Add label |> ignore
                | _ -> ()

            let nodeBox (entry: int) (title: string) (sub: string) (brush: SolidColorBrush) =
                let border =
                    Border(
                        Width = 104.0,
                        Height = 28.0,
                        BorderBrush = brush,
                        BorderThickness = Thickness(2.0),
                        Background = panel
                    )

                let stack = StackPanel()

                stack.Children.Add(
                    TextBlock(
                        Text = title,
                        Foreground = normal,
                        FontFamily = mono,
                        FontSize = 11.0,
                        HorizontalAlignment = HorizontalAlignment.Center
                    )
                )
                |> ignore

                stack.Children.Add(
                    TextBlock(
                        Text = sub,
                        Foreground = dim,
                        FontFamily = mono,
                        FontSize = 9.0,
                        HorizontalAlignment = HorizontalAlignment.Center
                    )
                )
                |> ignore

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
            if wasSelected then
                Set.remove entry selectedEntries
            else
                Set.add entry selectedEntries

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

            statusText.Text <-
                sprintf
                    "mined %d routines, %d call edges (self-mod addresses: %d)"
                    routines.Length
                    edges.Length
                    t.SelfModCount
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
                if divergences.IsEmpty then
                    sprintf "CE parity: %d/%d bytes match" matching total
                else
                    sprintf "CE parity: %d/%d - diverges at %A" matching total divergences

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
                if session.IsSome then
                    pauseGame ()

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
            | Some g ->
                launchGame (g, None)
                loadControlForGame ()
            | None -> ()

            true
        else
            true


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

    /// Park the session machine exactly on the trace entry under the cursor:
    /// restore the state after frame - 1 and re-execute the frame prefix with
    /// recording suppressed - memory, registers and the screen become the
    /// exact state of that moment (the same mechanism the flame seek uses).
    /// Best effort: when the frame is outside restorable history (deep live
    /// frames, a loaded trace with no stored states) the step stays
    /// display-only and says so.
    let syncToCursor () =
        match session, currentTrace () with
        | Some s, Some t when t.Entries.Length > 0 && t.FrameTicks.Length > 0 ->
            let c = max 0 (min cursor (t.Entries.Length - 1))
            let frameNo = frameOfEntry t c
            // First entry of that frame: boundary [frame - 2] ends frame - 1
            // (0-padded window leads read as the window's first entry).
            let firstInFrame =
                if frameNo < 2 || frameNo - 2 >= t.FrameTicks.Length then
                    0
                else
                    let tick' = t.FrameTicks[frameNo - 2]

                    let rec s2 (lo: int) (hi: int) =
                        if lo >= hi then
                            lo
                        else
                            let mid = (lo + hi) / 2

                            if t.Entries[mid].Tick < tick' then
                                s2 (mid + 1) hi
                            else
                                s2 lo mid

                    s2 0 t.Entries.Length
            // Steps count real instructions: an interrupt marker shares its Step
            // with the vector instruction.
            let mutable steps = 0

            for k in firstInFrame..c do
                if t.Entries[k].Length > 0uy then
                    steps <- steps + 1

            if steps > 0 then
                try
                    s.ParkAtInstruction(frameNo, steps)
                    presentGame ()
                    showLiveRegs s |> ignore
                with ex ->
                    statusText.Text <- sprintf "no state for frame %d (%s) - step is display-only" frameNo ex.Message
        | _ -> ()

    /// After any step: point the editors at the live machine. Memory/graph
    /// view centers on PC - a full jump, so a wheeled-away viewport recenters
    /// and the cursor row lands in view; the comment pane is memCursor-bound,
    /// so the 10 Hz pane poll picks it up everywhere within a tick.
    let followLivePc (reason: string) =
        match session with
        | Some s ->
            labelEditor <- None
            gotoMemCursor (int (s.Regs.Pc()) &&& 0xFFFF) reason
            refreshDisasm ()
        | None -> ()

    /// Debugger-style stepping: move the trace cursor, then park the machine
    /// on the new entry so registers, memory and the screen reflect the exact
    /// state of that moment (recording stays suppressed; Run resumes live
    /// execution from the parked position). Step into = the next executed
    /// entry. Step over = when the cursor sits on a call, land on its return
    /// site: the next executed entry at the call's fall-through address (a
    /// not-taken conditional call therefore just advances one). If the routine
    /// never returns inside the recorded trace, the cursor parks on the last
    let stepInto () =
        match session with
        | Some s when s.LiveMode ->
            // No trace: advance the live machine one instruction and show it.
            pauseGame ()
            s.StepLiveInstructions(1)
            presentGame ()
            showLiveRegs s |> ignore
        | _ ->
            pauseGame ()
            seek 1
            syncToCursor ()

        followLivePc "step into"

    let stepBack () =
        pauseGame ()
        seek -1
        syncToCursor ()
        followLivePc "step back"

    let stepOver () =
        match session with
        | Some _s when _s.LiveMode -> statusText.Text <- "step over needs a trace - use step into (F11) in live play"
        | _ ->
            pauseGame ()

            match currentTrace () with
            | Some t when t.Entries.Length > 0 ->
                let c = max 0 (min cursor (t.Entries.Length - 1))
                let e = t.Entries[c]

                if e.Length > 0uy && isCall e.B0 && c < t.Entries.Length - 1 then
                    let fallThrough = (int e.Pc + int e.Length) &&& 0xFFFF
                    let mutable k = c + 1

                    while k < t.Entries.Length && int t.Entries[k].Pc <> fallThrough do
                        k <- k + 1

                    seek (min (t.Entries.Length - 1) k - c)
                else
                    seek 1
            | _ -> ()

            syncToCursor ()
            followLivePc "step over"

    /// "Jump to previous execution of this instruction" (code row menu): scan
    /// the browsed trace backwards from the clicked entry for the same PC, and
    /// report execution counts in the status line - total, before, and after
    /// are computed in one forward pass (a 4M-entry sweep is a few
    /// milliseconds, fine for a menu click). The machine is parked on the
    /// landed entry, so state is exact like the step buttons.
    let jumpToPrevExecFromIndex (idx: int) =
        match currentTrace () with
        | Some t when t.Entries.Length > 0 && idx >= 0 && idx < t.Entries.Length ->
            let pc = int t.Entries[idx].Pc
            let mutable total = 0
            let mutable before = 0

            for i in 0 .. t.Entries.Length - 1 do
                if int t.Entries[i].Pc = pc then
                    total <- total + 1

                    if i < idx then
                        before <- before + 1

            let after = total - before
            let mutable k = idx - 1

            while k >= 0 && int t.Entries[k].Pc <> pc do
                k <- k - 1

            if k >= 0 then
                pauseGame ()
                cursor <- k
                refreshAll ()
                syncSlider ()
                syncToCursor ()

                statusText.Text <-
                    sprintf
                        "prev exec of %04X: instr #%d (was #%d) - total %d, %d before, %d after"
                        pc
                        k
                        idx
                        total
                        before
                        after
            else
                statusText.Text <-
                    sprintf
                        "no previous execution of %04X before instr #%d - total %d, %d before, %d after"
                        pc
                        idx
                        total
                        before
                        after
        | _ -> statusText.Text <- "trace changed - reopen the menu"

    let toggleCinema () =
        buildTraceNow ()

        if cinemaPlaying then
            cinemaPlaying <- false
            cinemaTimer.Stop()
            playBtn.Content <- "Play"
        else if currentEntryCount () > 0 then
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

            if t.Entries.Length = 0 then
                failwith "trace is empty"

            let dlg =
                Microsoft.Win32.SaveFileDialog(Filter = "Jetpac trace (*.jpt)|*.jpt", FileName = "trace.jpt")

            if dlg.ShowDialog() = Nullable<bool>(true) then
                TraceCodec.save t dlg.FileName
                statusText.Text <- sprintf "saved %d instructions to %s" t.Entries.Length dlg.FileName
        with ex ->
            statusText.Text <- "save failed: " + ex.Message

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

                statusText.Text <-
                    sprintf
                        "loaded %d instructions from %s; mined %d routines"
                        t.Entries.Length
                        dlg.FileName
                        routines.Length
            with ex ->
                statusText.Text <- "load failed: " + ex.Message

    let startGame () =
        match session with
        | Some s ->
            recordToggle.IsChecked <- Nullable<bool>(s.Recorder.RecordEnabled)
            running <- true
            frameTimer.Start()

            statusText.Text <-
                sprintf
                    "emulator ready (%s) - frame 0, recording %s, saved input %d events"
                    (if s.WarmStart then
                         "cached entry state"
                     else
                         "booted to game entry")
                    (if s.Recorder.RecordEnabled then "ON" else "OFF")
                    s.KeyLog.Count
        | None -> ()

    /// Install a booted session per the entry choice: autoplay a slot (falls
    /// back to recording into it when the file is missing), arm a fresh
    /// recording, or run live with no trace. Callers repaint + gate after.
    let installSession (game: GameManifest) (s: TraceSession) (start: SessionStart) =
        session <- Some s
        flameTrace <- None
        flameCache.Clear()
        // The window on screen belongs to the previous session (or none): a
        // live play session has no trace to show, and a recording's window is
        // reinstalled by its build. The bump also cancels a build in flight
        // for the previous session.
        flameGeneration <- flameGeneration + 1
        flameBuilding <- false
        flameCtrl.Clear()
        cursor <- 0
        execViewHold <- false

        match start with
        | AutoplaySlot name ->
            currentSlot <- name

            if loadSlotTimeline game s (slotPath game name) then
                replayExtent <- max 1 s.TimelineExtent
                s.StartReplay()
                replaying <- true
                startGame ()

                statusText.Text <-
                    sprintf
                        "%s: slot '%s' loaded (%d frames) - autoplay, seek anywhere"
                        game.Name
                        name
                        s.StateTimeline.Count
            else
                replaying <- false
                s.Recorder.RecordEnabled <- true
                startGame ()
                statusText.Text <- sprintf "%s: slot '%s' is empty - recording" game.Name name
        | RecordFresh name ->
            currentSlot <- name
            replaying <- false
            s.Recorder.RecordEnabled <- true
            startGame ()
            statusText.Text <- sprintf "%s: recording into new slot '%s'" game.Name name
        | LiveFresh ->
            currentSlot <- ""
            s.EnterLive()
            replaying <- false
            recordToggle.IsChecked <- Nullable<bool>(false)
            running <- true
            frameTimer.Start()
            statusText.Text <- sprintf "%s: live play - no trace, nothing recorded (Pause breaks)" game.Name

        syncSlider ()
        refreshAll ()

    // ---- single-window phase state -----------------------------------------
    // Both contents of the one window plus the picker's refresh hook: the
    // picker do block fills these, BuildEmulator and the back button swap
    // the window between them.
    let mutable pickerRoot: FrameworkElement = null
    let mutable emulatorRoot: FrameworkElement option = None
    /// Rebuilds the picker rows from disk + live session state. The picker
    /// do block installs the real implementation; the back button calls it
    /// just before swapping the menu in.
    let mutable refreshPicker: unit -> unit = fun () -> ()
    /// True while the picker is on screen (the emulator UI stays built
    /// underneath): background boots check it and stay parked instead of
    /// autoplaying into an invisible window.
    let mutable menuOpen = false
    /// Refreshes the control-file-driven lists (names tab, mass comments).
    /// The names tab block installs the real implementation; every game
    /// switch / phase swap calls it so a list never shows a previous
    /// project's rows.
    let mutable refreshControlLists: unit -> unit = fun () -> ()
    /// The cheat-engine view (CheatEngineView.fs) once built; it is
    /// embedded in this window as a phase - there is no second window.
    let mutable cheatView: CheatEngineView option = None
    /// Which view the launcher's buttons open: "emulator" (full UI), "game"
    /// (bare game screen with reverse playback) or "cheat" (cheat-engine
    /// view) - no emulator chrome for the latter two.
    let mutable launchPhase = "emulator"
    /// The bare game view (GameOnlyView.fs) once it has been built; it is
    /// embedded in this window as a phase - there is no second window.
    let mutable gameOnlyView: GameOnlyView option = None

    /// Leaves the current phase back to the launcher: pause, persist an
    /// unsaved recording, swap the launcher back in.
    let returnToMenu () =
        pauseGame ()

        if replaying then
            replaying <- false

        match session with
        | Some s -> s.StopReplay()
        | None -> ()

        saveTimeline false
        menuOpen <- true
        self.Title <- "ZX Spectrum Game Changer"
        refreshPicker ()
        self.Content <- pickerRoot

    // ---- startup picker (the window's first content) -----------------------
    // The window opens as "ZX Spectrum Game Changer": the LauncherView
    // (LauncherView.fs) lists projects and offers Emulator / Cheat Engine /
    // Just the Game. The emulator UI below is built on demand; the bare
    // game view (GameOnlyView.fs) swaps in for the Just-the-Game phase.
    // REPLACES this window's content with the emulator UI below - the same
    // window is reused, no second window is ever created. "<- back to main
    // menu" in the emulator toolbar returns here; the picker then reads the
    // LIVE session state (loaded files, unsaved changes) for the open
    // project instead of just the on-disk files.
    do
        GameImages.register () // CE availability for the status chips
        self.Title <- "ZX Spectrum Game Changer"
        // Window bounds come from gui.cfg when a previous run saved them;
        // otherwise the emulator layout's default size, centered. Either way
        // the phases never resize: only the user does.
        match GuiConfig.bounds () with
        | Some(w, h, x, y) ->
            self.Width <- w
            self.Height <- h
            self.Left <- x
            self.Top <- y
            self.WindowStartupLocation <- WindowStartupLocation.Manual
        | None ->
            self.Width <- 1560.0
            self.Height <- 900.0
            self.WindowStartupLocation <- WindowStartupLocation.CenterScreen

        self.Background <- bg

        // Open a project per the chosen phase: "emulator" swaps the full
        // emulator UI in, "game" swaps in the bare game view, "cheat" swaps
        // in the cheat-engine view - the emulator chrome never shows for
        // the latter two.
        let openProject (g: GameManifest) =
            menuOpen <- false

            match emulatorRoot with
            | None -> self.BuildEmulator g
            | Some r ->
                (match currentGame with
                 | Some cur when cur.GameId <> g.GameId -> gameCombo.SelectedItem <- g
                 | _ -> ())

                if launchPhase = "game" then
                    self.ShowGameOnly()
                elif launchPhase = "cheat" then
                    self.ShowCheatEngine()
                else
                    self.Title <- "JetpacFR - Game Changer"
                    self.Content <- r

                refreshControlLists ()

        let ctx: LauncherView.LauncherContext =
            { OpenEmulator =
                fun g ->
                    launchPhase <- "emulator"
                    openProject g
              OpenCheatEngine =
                fun g ->
                    launchPhase <- "cheat"
                    openProject g
              OpenJustGame =
                fun g ->
                    launchPhase <- "game"
                    openProject g
              BootSlow =
                fun g ->
                    // Relaunch through the emulated-tape boot even when the
                    // game is already running: the point is the other loader.
                    // BuildEmulator's own auto-launch (first open) runs first;
                    // the slow launch below replaces its boot task.
                    launchPhase <- "emulator"

                    match emulatorRoot with
                    | None -> self.BuildEmulator g
                    | Some r ->
                        self.Title <- "JetpacFR - Game Changer"
                        self.Content <- r

                    forceSlowBoot <- true
                    launchGame (g, None)
                    loadControlForGame ()
                    refreshControlLists ()
              SetTheme = applyTheme
              IsLight = fun () -> Theme.isLight () }

        let root, refreshLauncher = LauncherView.build ctx
        pickerRoot <- root
        refreshPicker <- refreshLauncher
        self.Content <- root

        // Unimportant GUI state outlives the process here: theme is already
        // synced on every toggle, bounds snapshot from RestoreBounds so a
        // maximized close still restores a sane placement next launch.
        self.Closing.Add(fun _ ->
            let b = self.RestoreBounds
            GuiConfig.setBounds (b.Width, b.Height, b.X, b.Y)
            GuiConfig.save ())

    // ---- construction ------------------------------------------------------
    /// Build the emulator UI into this window (replacing the picker content)
    /// and boot `game`: boot to entry, load its saved trace, autoplay.
    member self.BuildEmulator(game: GameManifest) : unit =
        menuOpen <- false
        self.Title <- "JetpacFR - Game Changer"
        // No size changes on any phase switch: the window keeps whatever size
        // it has (set once at startup, resizable only by the user).
        self.Background <- bg

        gameImage.Source <- gameBitmap
        gameImage.Stretch <- Stretch.Uniform
        gameImage.Width <- 320.0
        gameImage.Height <- 256.0
        RenderOptions.SetBitmapScalingMode(gameImage, BitmapScalingMode.NearestNeighbor)
        // Clicking the screen takes keyboard focus: emulator keys are read at
        // window level, but a focused Button eats Space/Enter (Spectrum fire and
        // Enter) for its own click, and text boxes are filtered out entirely.
        // Focusable + no focus visual = an explicit "control the game" gesture.
        gameImage.Focusable <- true
        gameImage.FocusVisualStyle <- null
        gameImage.MouseLeftButtonDown.Add(fun _ -> gameImage.Focus() |> ignore)

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
        let regNames =
            [ "AF"
              "BC"
              "DE"
              "HL"
              "AF'"
              "BC'"
              "DE'"
              "HL'"
              "IX"
              "IY"
              "SP"
              "I"
              "R"
              "PC" ]

        let regGrid = Grid()
        regGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(52.0)))
        regGrid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(64.0)))

        for (i, name) in List.indexed regNames do
            regGrid.RowDefinitions.Add(RowDefinition(Height = GridLength(20.0)))

            let nameTb =
                TextBlock(
                    Text = name,
                    Foreground = dim,
                    FontFamily = mono,
                    FontSize = 12.0,
                    VerticalAlignment = VerticalAlignment.Center
                )

            let valTb =
                TextBlock(
                    Text = "----",
                    Foreground = normal,
                    FontFamily = mono,
                    FontSize = 12.0,
                    VerticalAlignment = VerticalAlignment.Center
                )

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

        let flagsPanel =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 0.0))

        for i in 0..7 do
            let label =
                TextBlock(Text = flagLabels[i] + "=", Foreground = dim, FontFamily = mono, FontSize = 12.0)

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
        left.Children.Add(modeLabel) |> ignore

        let regHeader =
            TextBlock(
                Text = "registers (nearest snapshot)",
                Foreground = dim,
                FontSize = 12.0,
                Margin = Thickness(0.0, 8.0, 0.0, 2.0)
            )

        left.Children.Add(regHeader) |> ignore
        left.Children.Add(regGrid) |> ignore
        left.Children.Add(flagsPanel) |> ignore

        // ---- project files + trace autoplay timing (below the registers) -----
        // control.json: created (on disk) and modified (unsaved edits). CE
        // file: the game's compiled-port source (Game.fs, or the materialized
        // CeProgram.fs) - created vs missing, and whether it changed on disk
        // since this game was loaded. Autoplay timing: the UI timer marks the
        // real clock when a trace replay starts (the app-entry autoplay or
        // the Replay button) and when it ends or is interrupted, then reports
        // elapsed real time and achieved fps against the 50 Hz frame target
        // (one frame per 20 ms timer tick). Refreshed from the UI timer like
        // updateModeLabel, so no individual handler needs instrumenting.
        let filesHeader =
            TextBlock(
                Text = "project files",
                Foreground = dim,
                FontSize = 12.0,
                Margin = Thickness(0.0, 10.0, 0.0, 2.0)
            )

        let controlFileLabel =
            TextBlock(Foreground = dim, FontFamily = mono, FontSize = 11.5, TextWrapping = TextWrapping.Wrap)

        let ceFileLabel =
            TextBlock(
                Foreground = dim,
                FontFamily = mono,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = Thickness(0.0, 2.0, 0.0, 0.0)
            )

        let replayHeader =
            TextBlock(
                Text = "trace autoplay timing (target 50 fps)",
                Foreground = dim,
                FontSize = 12.0,
                Margin = Thickness(0.0, 10.0, 0.0, 2.0)
            )

        let replayStartLabel =
            TextBlock(
                Text = "no autoplay yet",
                Foreground = dim,
                FontFamily = mono,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            )

        let replayEndLabel =
            TextBlock(
                Foreground = dim,
                FontFamily = mono,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = Thickness(0.0, 2.0, 0.0, 0.0)
            )

        // Trace regeneration counter: the recorder stamps each Build() with a
        // monotonically increasing number (not persisted). Views that redraw
        // from the trace can skip rebuilding their tree while the number
        // stands still - the counter makes the redraw need visible.
        let traceRegenHeader =
            TextBlock(
                Text = "trace rebuilds (regenerations)",
                Foreground = dim,
                FontSize = 12.0,
                Margin = Thickness(0.0, 10.0, 0.0, 2.0)
            )

        let traceRegenLabel =
            TextBlock(
                Text = "trace regenerations: -",
                Foreground = dim,
                FontFamily = mono,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            )

        // CE file watched for the current game: its path and stamp when the
        // game was loaded; "modified" = the on-disk stamp differs now.
        let mutable ceWatchGameId = ""
        let mutable ceWatchPath = ""
        let mutable ceWatchStamp = DateTime.MinValue

        let mutable replayTimingActive = false
        let mutable replayTimingStart = DateTime.MinValue
        let mutable replayTimingStartFrame = 0
        let mutable replayTimingLastFrame = 0
        let mutable replayTimingSession: TraceSession option = None

        let updateFileStatusPanel () =
            match control with
            | Some c when controlGameDir <> "" ->
                let created = File.Exists(Path.Combine(controlGameDir, "control.json"))

                controlFileLabel.Text <-
                    sprintf
                        "control.json: %s, %s (%d comments)"
                        (if created then "created" else "not created")
                        (if c.Dirty then "modified - use Save ctrl" else "saved")
                        c.Comments.Length

                controlFileLabel.Foreground <- (if c.Dirty then orange else dim)
            | None when controlGameDir <> "" ->
                controlFileLabel.Text <- "control.json: load failed"
                controlFileLabel.Foreground <- red
            | _ -> controlFileLabel.Text <- "control.json: -"

            let gameId =
                match currentGame with
                | Some g -> g.GameId
                | None -> ""

            if gameId <> ceWatchGameId then
                ceWatchGameId <- gameId
                ceWatchPath <- ""
                ceWatchStamp <- DateTime.MinValue
                let dir = controlDir ()

                if dir <> "" then
                    match
                        [ "Game.fs"; "CeProgram.fs" ]
                        |> List.map (fun n -> Path.Combine(dir, n))
                        |> List.tryFind File.Exists
                    with
                    | Some p ->
                        ceWatchPath <- p
                        ceWatchStamp <- File.GetLastWriteTime p
                    | None -> ()

            if gameId = "" then
                ceFileLabel.Text <- "CE file: -"
                ceFileLabel.Foreground <- dim
            elif ceWatchPath = "" then
                ceFileLabel.Text <- "CE file: missing (interpreter only)"
                ceFileLabel.Foreground <- dim
            else
                let modified = File.GetLastWriteTime ceWatchPath <> ceWatchStamp

                ceFileLabel.Text <-
                    sprintf
                        "CE file %s: created %s, %s"
                        (Path.GetFileName ceWatchPath)
                        (ceWatchStamp.ToString("yyyy-MM-dd HH:mm"))
                        (if modified then
                             "modified on disk since load"
                         else
                             "unchanged since load")

                ceFileLabel.Foreground <- (if modified then orange else dim)

            // the regeneration counter rides the same UI-tick update: views
            // watch this number to skip rebuilds of their display trees
            match currentTrace () with
            | Some t ->
                traceRegenLabel.Text <-
                    sprintf "trace regenerations: %d (window: %d entries)" t.Regenerations t.Entries.Length

                traceRegenLabel.Foreground <- dim
            | None -> traceRegenLabel.Text <- "trace regenerations: -"

        let updateReplayTiming () =
            match session with
            | Some s when s.Replaying || replaying ->
                let isNewReplay =
                    (not replayTimingActive)
                    || (match replayTimingSession with
                        | Some prev -> not (Object.ReferenceEquals(prev, s))
                        | None -> true)

                if isNewReplay then
                    replayTimingActive <- true
                    replayTimingSession <- Some s
                    replayTimingStart <- DateTime.Now
                    replayTimingStartFrame <- s.Frame
                    replayTimingLastFrame <- s.Frame

                    replayStartLabel.Text <-
                        sprintf "started %s at frame %d" (replayTimingStart.ToString("HH:mm:ss")) s.Frame

                    replayStartLabel.Foreground <- green
                else
                    replayTimingLastFrame <- s.Frame

                    let elapsed = (DateTime.Now - replayTimingStart).TotalSeconds
                    let frames = replayTimingLastFrame - replayTimingStartFrame
                    let fps = if elapsed > 0.5 then float frames / elapsed else 0.0

                    replayEndLabel.Text <- sprintf "running: %.1f s real, %d frames, %.1f fps" elapsed frames fps

                    replayEndLabel.Foreground <- green
            | _ ->
                if replayTimingActive then
                    replayTimingActive <- false

                    let ended = DateTime.Now
                    let elapsed = (ended - replayTimingStart).TotalSeconds
                    let frames = max 0 (replayTimingLastFrame - replayTimingStartFrame)
                    let fps = if elapsed > 0.5 then float frames / elapsed else 0.0

                    let sameSession =
                        match session, replayTimingSession with
                        | Some cur, Some s -> Object.ReferenceEquals(cur, s)
                        | _ -> false

                    let replayDone =
                        match session with
                        | Some s -> s.ReplayFinished
                        | None -> false

                    let reason =
                        if not sameSession then "session switched"
                        elif replayDone then "reached recording end"
                        else "interrupted"

                    replayEndLabel.Text <-
                        sprintf
                            "ended %s - %s after %.1f s real: %d frames, %.1f fps"
                            (ended.ToString("HH:mm:ss"))
                            reason
                            elapsed
                            frames
                            fps

                    replayEndLabel.Foreground <- (if reason = "reached recording end" then green else orange)

        left.Children.Add filesHeader |> ignore
        left.Children.Add controlFileLabel |> ignore
        left.Children.Add ceFileLabel |> ignore
        left.Children.Add replayHeader |> ignore
        left.Children.Add replayStartLabel |> ignore
        left.Children.Add replayEndLabel |> ignore
        left.Children.Add traceRegenHeader |> ignore
        left.Children.Add traceRegenLabel |> ignore
        updateFileStatusPanel ()

        // center: timeline selector, mode buttons, disassembly, comment toolbar
        let center = DockPanel(Margin = Thickness(8.0))

        // 1. top: timeline selector with the button row underneath it
        let tlColumn = StackPanel()
        tlColumn.Children.Add timeline |> ignore

        let tlBtns =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(2.0, 4.0, 0.0, 0.0))

        for b in
            [ brushABtn :> FrameworkElement
              brushBBtn :> FrameworkElement
              previewABtn :> FrameworkElement
              previewBBtn :> FrameworkElement
              saveCtrlBtn :> FrameworkElement
              importCtrlBtn :> FrameworkElement
              exportCtrlBtn :> FrameworkElement
              newCtrlBtn :> FrameworkElement
              flameABtn :> FrameworkElement
              flameBBtn :> FrameworkElement
              flameFitBtn :> FrameworkElement
              flameAutoBtn :> FrameworkElement
              syncViewsBtn :> FrameworkElement ] do
            b.Margin <- Thickness(2.0)
            tlBtns.Children.Add b |> ignore

        tlColumn.Children.Add tlBtns |> ignore
        DockPanel.SetDock(tlColumn, Dock.Top)
        center.Children.Add tlColumn |> ignore

        // 2. strip + slider
        let stripRow = StackPanel()
        stripRow.Children.Add(stripImage) |> ignore
        stripRow.Children.Add(slider) |> ignore
        traceRangeLabel.Margin <- Thickness(0.0, 1.0, 0.0, 2.0)
        stripRow.Children.Add(traceRangeLabel) |> ignore
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
        let modeRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 2.0))

        let engLabel =
            TextBlock(
                Text = "engine:",
                Foreground = dim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(0.0, 0.0, 4.0, 0.0)
            )

        modeRow.Children.Add engLabel |> ignore

        for r in
            [ oracleRadio :> FrameworkElement
              ceRadio :> FrameworkElement
              diffRadio :> FrameworkElement ] do
            r.Margin <- Thickness(0.0, 0.0, 10.0, 0.0)
            r.VerticalAlignment <- VerticalAlignment.Center
            modeRow.Children.Add r |> ignore

        DockPanel.SetDock(modeRow, Dock.Top)
        center.Children.Add modeRow |> ignore

        let viewRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 2.0, 0.0, 4.0))

        let modeLabel =
            TextBlock(
                Text = "code view:",
                Foreground = dim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(0.0, 0.0, 4.0, 0.0)
            )

        viewRow.Children.Add modeLabel |> ignore
        memModeBtn.VerticalAlignment <- VerticalAlignment.Center
        execModeBtn.VerticalAlignment <- VerticalAlignment.Center

        for b in
            [ memModeBtn :> FrameworkElement
              execModeBtn :> FrameworkElement
              graphModeBtn :> FrameworkElement
              graphAllBtn :> FrameworkElement ] do
            b.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
            viewRow.Children.Add b |> ignore

        DockPanel.SetDock(viewRow, Dock.Top)
        center.Children.Add viewRow |> ignore

        // 4. the comment EDIT pane moves right of the code pane (they share
        //    codeCmtHost below the flame graph, with a splitter); the shared
        //    comment box + mass-comment buttons and the target line stay
        //    docked to the bottom of the center column, where they had room.
        let codeCmtHost = Grid()
        codeCmtHost.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
        codeCmtHost.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
        codeCmtHost.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(320.0)))

        let cmtSplitter =
            GridSplitter(
                Width = 4.0,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Theme.hiBg,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            )

        Grid.SetColumn(cmtSplitter, 1)
        codeCmtHost.Children.Add cmtSplitter |> ignore
        let cmtHost = DockPanel(Margin = Thickness(4.0, 0.0, 0.0, 0.0))
        cmtHost.MinWidth <- 220.0
        Grid.SetColumn(cmtHost, 2)
        codeCmtHost.Children.Add cmtHost |> ignore
        let cmtBar = StackPanel(Margin = Thickness(0.0, 4.0, 0.0, 0.0))

        let cmtLabel =
            TextBlock(
                Text = "comment text (shared):",
                Foreground = dim,
                FontSize = 11.0,
                Margin = Thickness(0.0, 0.0, 6.0, 2.0)
            )

        cmtBar.Children.Add cmtLabel |> ignore
        cmtBar.Children.Add commentBox |> ignore
        let cmtBtnRow = WrapPanel(Margin = Thickness(0.0, 4.0, 0.0, 0.0))

        for b in
            [ idxCmtBtn :> FrameworkElement
              lineCmtBtn :> FrameworkElement
              nameBeforeBtn :> FrameworkElement
              nameABtn :> FrameworkElement
              nameBBtn :> FrameworkElement
              cmtABtn :> FrameworkElement
              cmtBBtn :> FrameworkElement
              cmtBNotA :> FrameworkElement
              cmtANotB :> FrameworkElement
              framesABtn :> FrameworkElement
              framesBBtn :> FrameworkElement ] do
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

        // Comment edit pane (right of the code pane): every comment attached
        // to the current line, most specific first. Line comments get
        // editable boxes; block (range/name) comments are shown read-only
        // and marked. Enter on a code line hands focus here. It is the only
        // element inside cmtHost, so it fills the side pane.
        let cmtPaneBorder =
            Border(BorderBrush = dim, BorderThickness = Thickness(1.0), Margin = Thickness(0.0, 2.0, 0.0, 2.0))

        let cmtPaneInner = StackPanel(Margin = Thickness(4.0))

        let cmtPaneTitle =
            TextBlock(
                Text = "comments (click a code line or use arrows)",
                Foreground = dim,
                FontSize = 11.0,
                Margin = Thickness(0.0, 0.0, 0.0, 2.0)
            )

        let cmtPaneRows = StackPanel()
        // Capped + scrollable: comment rows must never push the shared box
        // and buttons out of the side pane (the cap keeps the pane's own
        // scrollbar doing the work).
        let cmtPaneRowsScroll =
            ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 480.0)

        cmtPaneRowsScroll.Content <- cmtPaneRows
        let cmtPaneAddBox = TextBox(Width = 300.0, Height = 20.0)
        let cmtPaneAddBtn = Button(Content = "add line comment", Width = 132.0)
        cmtPaneBorder.Child <- cmtPaneInner
        cmtPaneInner.Children.Add cmtPaneTitle |> ignore
        cmtPaneInner.Children.Add cmtPaneRowsScroll |> ignore

        let cmtPaneAddRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 2.0, 0.0, 0.0))

        cmtPaneAddRow.Children.Add cmtPaneAddBox |> ignore
        cmtPaneAddRow.Children.Add cmtPaneAddBtn |> ignore
        cmtPaneInner.Children.Add cmtPaneAddRow |> ignore
        DockPanel.SetDock(cmtPaneBorder, Dock.Bottom)
        cmtHost.Children.Add cmtPaneBorder |> ignore // last child: fills the pane

        let mutable paneAddr: int option = None
        let mutable paneControl: obj = null
        /// The line comment selected for editing in the pane's single text box
        /// (None = the box adds a new comment at the cursor).
        let mutable editingComment: ControlComment option = None

        let rec rebuildCommentPane (force: bool) =
            // Label-editor mode: clicking a label row swaps the pane from the
            // comment list to a label editor - static kind + address text,
            // the name in an editable box, Rename/Cancel.
            match labelEditor with
            | Some(addr, name) ->
                if force || paneAddr <> Some addr then
                    paneAddr <- Some addr

                    let kind =
                        match control with
                        | Some c when c.Symbols |> List.exists (fun (a, _) -> a = addr) -> "user symbol"
                        | Some c when
                            c.Blocks
                            |> List.exists (fun b ->
                                b.Kind = Code && b.Start = addr && b.Name <> sprintf "block_%04X" b.Start)
                            ->
                            "block name"
                        | _ -> "generated label"

                    cmtPaneTitle.Text <- sprintf "label editor (was: %s)" name
                    cmtPaneRows.Children.Clear()

                    cmtPaneRows.Children.Add(
                        TextBlock(
                            Text = sprintf "kind: %s    address: $%04X" kind addr,
                            Foreground = dim,
                            FontSize = 11.0,
                            Margin = Thickness(0.0, 0.0, 0.0, 6.0)
                        )
                    )
                    |> ignore

                    let box = TextBox(Text = name, FontFamily = mono, FontSize = 12.0)
                    cmtPaneRows.Children.Add box |> ignore

                    let btnRow =
                        StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 6.0, 0.0, 0.0))

                    let renameBtn =
                        Button(Content = "Rename", Width = 90.0, Margin = Thickness(0.0, 0.0, 8.0, 0.0))

                    let apply () =
                        match control with
                        | Some c ->
                            control <- Some(ControlFile.renameSymbol c addr box.Text)
                            labelEditor <- Some(addr, box.Text.Trim())
                            refreshControlLists ()
                            refreshDisasm ()
                            rebuildCommentPane true
                        | None -> ()

                    renameBtn.Click.Add(fun _ -> apply ())
                    btnRow.Children.Add renameBtn |> ignore

                    let cancelBtn = Button(Content = "Cancel", Width = 90.0)

                    cancelBtn.Click.Add(fun _ ->
                        labelEditor <- None
                        refreshDisasm ()
                        rebuildCommentPane true)

                    btnRow.Children.Add cancelBtn |> ignore
                    cmtPaneRows.Children.Add btnRow |> ignore

                    box.KeyDown.Add(fun e ->
                        if e.Key = Key.Enter then
                            e.Handled <- true
                            apply ()
                        elif e.Key = Key.Escape then
                            e.Handled <- true
                            labelEditor <- None
                            refreshDisasm ()
                            rebuildCommentPane true)
            | None -> rebuildCommentRows force

        and rebuildCommentRows (force: bool) =
            let controlRef = control |> Option.map box |> Option.defaultValue null

            if
                force
                || paneAddr <> Some memCursor
                || not (Object.ReferenceEquals(paneControl, controlRef))
            then
                // The editing selection is address-bound: moving the cursor away
                // leaves edit mode (the drafted text stays in the box).
                match editingComment with
                | Some m when m.Addr <> memCursor ->
                    editingComment <- None
                    cmtPaneAddBtn.Content <- "add line comment"
                | _ -> ()

                paneAddr <- Some memCursor
                paneControl <- controlRef
                cmtPaneRows.Children.Clear()
                cmtPaneTitle.Text <- sprintf "comments at %04X" memCursor

                match control with
                | None ->
                    cmtPaneRows.Children.Add(
                        TextBlock(Text = "no control file - click New ctrl first", Foreground = dim, FontSize = 11.0)
                    )
                    |> ignore
                | Some c ->
                    // Name/label section: in execution mode there are no
                    // label rows, so a named address shows its name here
                    // for editing (and an unnamed one offers a box to add
                    // one). renameSymbol upserts, empty text removes.
                    let named = nameAtAddr memCursor

                    let kindText, boxText, btnText =
                        match named with
                        | Some(n, k) -> k, n, "Rename"
                        | None -> "no name", "", "Add name"

                    cmtPaneRows.Children.Add(
                        TextBlock(
                            Text = sprintf "name: %s  ($%04X)" kindText memCursor,
                            Foreground = dim,
                            FontSize = 11.0,
                            Margin = Thickness(0.0, 0.0, 0.0, 3.0)
                        )
                    )
                    |> ignore

                    let nameRow =
                        StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 8.0))

                    let nameBox =
                        TextBox(Text = boxText, Width = 190.0, FontFamily = mono, FontSize = 11.5)

                    let applyName () =
                        if not (String.IsNullOrWhiteSpace nameBox.Text) then
                            control <- Some(ControlFile.renameSymbol c memCursor (nameBox.Text.Trim()))
                            refreshControlLists ()
                            refreshDisasm ()
                            rebuildCommentPane true
                        elif named.IsSome then
                            // empty text removes the name
                            control <- Some(ControlFile.renameSymbol c memCursor "")
                            refreshControlLists ()
                            refreshDisasm ()
                            rebuildCommentPane true

                    let nameBtn =
                        Button(Content = btnText, MinWidth = 80.0, Margin = Thickness(6.0, 0.0, 0.0, 0.0))

                    nameBtn.Click.Add(fun _ -> applyName ())

                    nameBox.KeyDown.Add(fun e ->
                        if e.Key = Key.Enter then
                            e.Handled <- true
                            applyName ())

                    nameRow.Children.Add nameBox |> ignore
                    nameRow.Children.Add nameBtn |> ignore
                    cmtPaneRows.Children.Add nameRow |> ignore

                    // Comment rows are read-only one-liners: clicking a line comment
                    // loads it into the single editor below (whose button turns into
                    // "update comment"), so rows never carry text boxes and the pane
                    // height stays bounded.
                    let mkRow (label: string) (labelColor: Brush) (text: string) (original: ControlComment option) =
                        let row =
                            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 1.0, 0.0, 1.0))

                        let tag =
                            TextBlock(
                                Text = label,
                                Foreground = labelColor,
                                FontSize = 11.0,
                                VerticalAlignment = VerticalAlignment.Center,
                                MinWidth = 150.0
                            )

                        row.Children.Add tag |> ignore

                        let body =
                            TextBlock(
                                Text = text,
                                Foreground = normal,
                                FontSize = 11.0,
                                VerticalAlignment = VerticalAlignment.Center
                            )

                        row.Children.Add body |> ignore

                        match original with
                        | Some m ->
                            row.Cursor <- Cursors.Hand

                            row.MouseLeftButtonDown.Add(fun _ ->
                                Seq.cast<Panel> cmtPaneRows.Children
                                |> Seq.iter (fun el -> el.Background <- null)

                                row.Background <- hiBgBrush
                                editingComment <- Some m
                                cmtPaneAddBox.Text <- m.Text
                                cmtPaneAddBtn.Content <- "update comment")

                            let del =
                                Button(Content = "del", Width = 38.0, Margin = Thickness(6.0, 0.0, 0.0, 0.0))

                            del.Click.Add(fun _ ->
                                if editingComment = Some m then
                                    editingComment <- None
                                    cmtPaneAddBox.Text <- ""
                                    cmtPaneAddBtn.Content <- "add line comment"

                                control <- Some(ControlFile.removeComment c m)
                                refreshDisasm ()
                                rebuildCommentPane true)

                            row.Children.Add del |> ignore
                        | None -> ()

                        cmtPaneRows.Children.Add row |> ignore

                    let lines = c.Comments |> List.filter (fun m -> m.Kind = Line && m.Addr = memCursor)

                    let covering kind =
                        c.Comments
                        |> List.filter (fun m -> m.Kind = kind && m.Addr <= memCursor && memCursor < m.EndExcl)
                        |> List.sortBy (fun m -> m.EndExcl - m.Addr)

                    if
                        List.isEmpty lines
                        && List.isEmpty (covering Range)
                        && List.isEmpty (covering Name)
                    then
                        cmtPaneRows.Children.Add(
                            TextBlock(Text = "none - add one below", Foreground = dim, FontSize = 11.0)
                        )
                        |> ignore

                    for m in lines do
                        mkRow "line" normal m.Text (Some m)

                    for m in covering Range do
                        mkRow (sprintf "[block range %04X-%04X]" m.Addr m.EndExcl) cyan m.Text None

                    for m in covering Name do
                        mkRow (sprintf "[block name %04X-%04X]" m.Addr m.EndExcl) cyan m.Text None

        let commitComment () =
            match control with
            | Some c ->
                match editingComment with
                | Some m ->
                    control <- Some(ControlFile.replaceComment c m cmtPaneAddBox.Text)
                    editingComment <- None
                    cmtPaneAddBox.Text <- ""
                    cmtPaneAddBtn.Content <- "add line comment"
                | None ->
                    control <- Some(ControlFile.addLine c memCursor cmtPaneAddBox.Text)
                    cmtPaneAddBox.Text <- ""

                refreshDisasm ()
                rebuildCommentPane true
            | None -> statusText.Text <- "no control file - click New ctrl first"

        cmtPaneAddBtn.Click.Add(fun _ -> commitComment ())

        cmtPaneAddBox.KeyDown.Add(fun e ->
            if e.Key = Key.Enter then
                commitComment ())
        // pane lights up while it owns the keyboard
        cmtPaneInner.GotKeyboardFocus.Add(fun _ -> cmtPaneBorder.BorderBrush <- cyan)
        cmtPaneInner.LostKeyboardFocus.Add(fun _ -> cmtPaneBorder.BorderBrush <- dim)
        // clicking a code row (or arrows in the focused list) moves the bright
        // cursor to that row without recentering, and feeds the pane. Per mode:
        // the trace cursor in execution mode (else the ListBox's selection
        // highlight and the IsCurrent row diverge into two highlighted rows),
        // the memory cursor in memory mode. The refresh swaps ItemsSource, which
        // also clears the WPF selection, so exactly one row stays highlighted.
        disasmList.SelectionChanged.Add(fun _ ->
            match disasmList.SelectedItem with
            | :? DisasmRow as row when row.Addr >= 0 ->
                if disasmModeMemory then
                    // A label row (its tag ends with the ':' of `name:`) opens
                    // the label editor in the comments pane; the instruction
                    // row keeps unhighlighted and the bright cursor does not
                    // move. Any other row exits label-editor mode.
                    let tag = string row.Tag
                    let isLabelRow = tag.EndsWith ":" && labelNames.ContainsKey row.Addr

                    if isLabelRow then
                        labelEditor <- Some(row.Addr, labelNames[row.Addr])
                        refreshDisasm ()
                        rebuildCommentPane true
                    else
                        let wasEditing = labelEditor.IsSome
                        labelEditor <- None

                        if memCursor <> row.Addr || wasEditing then
                            memCursor <- row.Addr
                            memCursorReason <- "row click"
                            refreshDisasm ()
                            rebuildCommentPane true
                elif row.InstrIdx >= 0 then
                    cursor <- row.InstrIdx
                    memCursor <- row.Addr
                    execViewHold <- true // keep the window put: only the bright row moves
                    refreshAll ()
                    syncSlider ()
                    rebuildCommentPane true
            | _ -> ())

        disasmList.KeyDown.Add(fun e ->
            if e.Key = Key.Enter then
                e.Handled <- true
                cmtPaneBorder.BorderBrush <- cyan
                cmtPaneAddBox.Focus() |> ignore)

        // 5. the code window: list + graph surfaces stacked, one visible.
        // Above it sits the flame graph in a resizable row (GridSplitter): both
        // share the horizontal time domain with the timeline selector, so the
        // flame is the zoomable big-picture view of the same timeline.
        // Stepping toolbar over the code pane: moves the trace cursor (F10/F11
        // keys mirror the buttons; execution mode only). Memory mode has no
        // trace cursor, so the buttons no-op there for now.
        let breakBtn =
            Button(
                Content = "Break",
                Width = 64.0,
                ToolTip = "break into the debugger: pause the emulator where it stands (same as Pause)"
            )

        breakBtn.IsEnabled <- false

        let traceStepBackBtn =
            Button(
                Content = "Step back",
                Width = 76.0,
                ToolTip = "one instruction back; parks the machine there (registers, memory, screen)"
            )

        let stepIntoBtn =
            Button(
                Content = "Step into (F11)",
                Width = 104.0,
                ToolTip = "advance to the next executed instruction; parks the machine there"
            )

        let stepOverBtn =
            Button(
                Content = "Step over (F10)",
                Width = 104.0,
                ToolTip = "when the cursor is on a call, advance to its return site; parks the machine there"
            )

        breakBtn.Click.Add(fun _ ->
            pauseGame ()

            // Live play has no trace cursor to keep: the machine's PC is the
            // truth after a break, so the registers and the code window follow
            // it. With a trace, break stays on the cursor - the recording is
            // the working context there.
            match session with
            | Some s when s.LiveMode ->
                presentGame ()
                showLiveRegs s |> ignore
                followLivePc "break"
            | _ -> ())

        traceStepBackBtn.Click.Add(fun _ -> stepBack ())
        stepIntoBtn.Click.Add(fun _ -> stepInto ())
        stepOverBtn.Click.Add(fun _ -> stepOver ())
        let stepRow = StackPanel(Orientation = Orientation.Horizontal)

        for b in
            [ breakBtn :> FrameworkElement
              stepIntoBtn :> FrameworkElement
              stepOverBtn :> FrameworkElement
              traceStepBackBtn :> FrameworkElement ] do
            b.Margin <- Thickness(2.0)
            stepRow.Children.Add b |> ignore
        // jump helpers (handlers wired later - they need focusCodeView)
        let jumpPcBtn =
            Button(
                Content = "Jump To PC",
                ToolTip =
                    "execution mode: re-center the view on the current instruction; memory mode: jump to the machine's live PC"
            )

        let jumpAddrLabel =
            TextBlock(
                Text = "Jump To Address:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(10.0, 0.0, 4.0, 0.0),
                Foreground = normal
            )

        let jumpAddrBox =
            TextBox(
                Width = 64.0,
                FontFamily = mono,
                Background = panel,
                Foreground = normal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(0.0, 2.0, 2.0, 2.0),
                ToolTip = "hex address, e.g. 7309 or 0x7309 - Enter or Go jumps"
            )

        let jumpGoBtn =
            Button(
                Content = "Go",
                Width = 40.0,
                ToolTip = "execution mode: first execution of the address in the trace; memory mode: the address itself"
            )

        jumpPcBtn.Margin <- Thickness(2.0)
        jumpGoBtn.Margin <- Thickness(2.0, 2.0, 2.0, 2.0)
        stepRow.Children.Add jumpPcBtn |> ignore
        stepRow.Children.Add jumpAddrLabel |> ignore
        stepRow.Children.Add jumpAddrBox |> ignore
        stepRow.Children.Add jumpGoBtn |> ignore

        let codeHost = Grid()
        codeHost.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
        codeHost.RowDefinitions.Add(RowDefinition(Height = GridLength(1.0, GridUnitType.Star)))
        Grid.SetRow(stepRow, 0)
        codeHost.Children.Add stepRow |> ignore
        Grid.SetRow(disasmList, 1)
        codeHost.Children.Add disasmList |> ignore
        graphScroll.Content <- codeGraphCanvas
        graphScroll.Visibility <- Visibility.Collapsed
        Grid.SetRow(graphScroll, 1)
        codeHost.Children.Add graphScroll |> ignore
        let lowerHost = Grid()
        lowerHost.RowDefinitions.Add(RowDefinition(Height = GridLength(1.0, GridUnitType.Star), MinHeight = 70.0))
        lowerHost.RowDefinitions.Add(RowDefinition(Height = GridLength.Auto))
        lowerHost.RowDefinitions.Add(RowDefinition(Height = GridLength(1.2, GridUnitType.Star)))
        let flameHost = Grid()
        flameHost.Children.Add flameCtrl |> ignore
        flameHost.Children.Add flameCtrl.Overlay |> ignore
        Grid.SetRow(flameHost, 0)

        let flameSplitter =
            GridSplitter(
                Height = 4.0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Theme.hiBg,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            )

        Grid.SetRow(flameSplitter, 1)
        // the code pane (left) and the comment pane (right) share the row
        // below the flame graph via codeCmtHost, so the flame graph spans
        // BOTH at full width
        codeHost.MinWidth <- 300.0
        Grid.SetColumn(codeHost, 0)
        codeCmtHost.Children.Add codeHost |> ignore
        Grid.SetRow(codeCmtHost, 2)
        lowerHost.Children.Add flameHost |> ignore
        lowerHost.Children.Add flameSplitter |> ignore
        lowerHost.Children.Add codeCmtHost |> ignore
        center.Children.Add lowerHost |> ignore

        // right column: heatmap / functions / call graph
        let right = TabControl(Margin = Thickness(8.0), Background = panel)

        let heatTab = TabItem(Header = "Heatmap")
        let heatPanel = StackPanel()

        let heatHeader =
            TextBlock(Text = "64K execution heatmap (click to jump)", Foreground = dim, FontSize = 12.0)

        heatPanel.Children.Add heatHeader |> ignore
        heatPanel.Children.Add heatImage |> ignore

        let heatHint =
            TextBlock(
                Text = "black = never executed  magenta = executed then written",
                Foreground = dim,
                FontSize = 11.0
            )

        heatPanel.Children.Add heatHint |> ignore
        heatTab.Content <- heatPanel

        // graphics view: byte rows (8 KB window) + row width + zoom + address
        // slider
        let gfxTab = TabItem(Header = "Graphics")
        let gfxPanel = DockPanel()

        let gfxTop =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        let gfxHeader =
            TextBlock(
                Text = "memory as 8px rows (sprite finder)",
                Foreground = dim,
                FontSize = 12.0,
                VerticalAlignment = VerticalAlignment.Center
            )

        gfxTop.Children.Add gfxHeader |> ignore
        // row width: pixels per row; width/8 consecutive bytes are drawn side
        // by side before stepping down. Type a value 8..256 and press Enter,
        // or step by 8 with the buttons
        let gfxWidthLabel =
            TextBlock(
                Text = "row width:",
                Foreground = dim,
                FontSize = 12.0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(14.0, 0.0, 4.0, 0.0)
            )

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
            if e.Key = Key.Enter then
                commitGfxWidth ())

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
                    Content = sprintf "%dx" z,
                    GroupName = "gfxZoom",
                    IsChecked = Nullable<bool>(isDflt),
                    Foreground = normal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = Thickness(8.0, 0.0, 0.0, 0.0)
                )

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
                Text = "0000h",
                Foreground = dim,
                FontFamily = mono,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(0.0, 0.0, 6.0, 0.0)
            )

        Grid.SetColumn(gfxLoLabel, 0)
        gfxSliderRow.Children.Add gfxLoLabel |> ignore
        gfxSlider.VerticalAlignment <- VerticalAlignment.Center
        Grid.SetColumn(gfxSlider, 1)
        gfxSliderRow.Children.Add gfxSlider |> ignore

        let gfxHiLabel =
            TextBlock(
                Text = "FFFFh",
                Foreground = dim,
                FontFamily = mono,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(6.0, 0.0, 0.0, 0.0)
            )

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
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            )

        gfxScroll.Content <- gfxImage
        gfxPanel.Children.Add gfxScroll |> ignore
        gfxTab.Content <- gfxPanel
        gfxSlider.Value <- float gfxBase

        gfxSlider.ValueChanged.Add(fun _ ->
            gfxBase <- int gfxSlider.Value
            refreshGfx ())

        refreshGfx ()

        let funcTab = TabItem(Header = "Functions")
        let funcPanel = DockPanel(Margin = Thickness(4.0))

        let funcTop =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        mineBtn.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
        funcTop.Children.Add mineBtn |> ignore
        routineCountLabel.Foreground <- dim
        routineCountLabel.VerticalAlignment <- VerticalAlignment.Center
        funcTop.Children.Add routineCountLabel |> ignore
        // The tab's content was never assembled (unlike Scan/Contract/Theater,
        // which all set .Content) - the Mine button and the routine list were
        // built but never parented, so the tab rendered empty.
        DockPanel.SetDock(funcTop, Dock.Top)
        funcPanel.Children.Add funcTop |> ignore
        routineDetail.Foreground <- dim
        routineDetail.FontFamily <- mono
        routineDetail.FontSize <- 11.0
        routineDetail.TextWrapping <- TextWrapping.Wrap
        DockPanel.SetDock(routineDetail, Dock.Bottom)
        funcPanel.Children.Add routineDetail |> ignore
        funcPanel.Children.Add routineList |> ignore // last child fills the pane
        funcTab.Content <- funcPanel

        // Mass comments: a mass-comment batch (Comment A / B / A-only / B-only)
        // stamps the SAME text onto many addresses, so groups are recovered by
        // grouping Line comments on their text. Selecting a group and jumping
        // finds the earliest executed instruction of the region - the moment the
        // compared ranges first diverge.
        let massTab = TabItem(Header = "Mass comments")
        let massPanel = DockPanel(Margin = Thickness(4.0))
        let massList = ListBox()
        let massRefreshBtn = Button(Content = "Refresh list", Width = 100.0)

        let massFindBtn =
            Button(
                Content = "Find first execution in trace",
                Width = 200.0,
                ToolTip =
                    "jump the trace cursor (and park the machine) on the earliest executed instruction whose address carries this comment group"
            )

        let massHint =
            TextBlock(
                Text = "select a group, then find its first execution",
                Foreground = dim,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center
            )

        let mutable massGroupsCache: (string * Set<int>) list = []

        let refreshMassList () =
            let groups =
                match control with
                | None -> []
                | Some c ->
                    c.Comments
                    |> List.filter (fun m -> m.Kind = CommentKind.Line && not (String.IsNullOrWhiteSpace m.Text))
                    |> List.groupBy (fun m -> m.Text.Trim())
                    |> List.map (fun (txt, ms) -> txt, (ms |> List.map (fun m -> m.Addr) |> Set.ofList))
                    |> List.sortByDescending (fun (_, a) -> Set.count a)

            massGroupsCache <- groups
            let rows = ResizeArray<DisasmRow>()

            for txt, addrs in groups do
                rows.Add
                    { Tag =
                        sprintf
                            "%s   -   %d addrs, trace-defined (span %04X..%04X)"
                            txt
                            (Set.count addrs)
                            (Set.minElement addrs)
                            (Set.maxElement addrs)
                      Brush = cyan
                      Tint = null
                      IsCurrent = false
                      Addr = -1
                      InstrIdx = -1 }

            massList.ItemsSource <- rows

        /// Comment operations call this: the list only refreshes while its tab
        /// is the visible one (it re-reads on tab selection otherwise).
        let refreshMassIfVisible () =
            if right.SelectedItem = massTab then
                refreshMassList ()

        let findFirstInRegion () =
            match massList.SelectedIndex with
            | i when i >= 0 && i < List.length massGroupsCache ->
                let _, addrs = List.item i massGroupsCache

                match currentTrace () with
                | Some t when t.Entries.Length > 0 ->
                    // Entries are time-ordered, so the smallest entry index over the
                    // region's first-execution indices IS the first execution.
                    let best =
                        (Int32.MaxValue, addrs)
                        ||> Set.fold (fun best a ->
                            if a < t.FirstIndexAtPc.Length && t.FirstIndexAtPc[a] >= 0 then
                                min best t.FirstIndexAtPc[a]
                            else
                                best)

                    if best = Int32.MaxValue then
                        statusText.Text <- "none of the region's addresses executed in this trace"
                    else
                        pauseGame ()
                        cursor <- best
                        refreshAll ()
                        syncSlider ()
                        syncToCursor ()

                        statusText.Text <-
                            sprintf
                                "first execution of region: instr #%d, frame %d, pc=%04X"
                                best
                                (frameOfEntry t best)
                                (int t.Entries[best].Pc)
                | _ -> statusText.Text <- "no trace - play the game first"
            | _ -> statusText.Text <- "select a comment group first"

        let massTemplate = DataTemplate()
        let massFactory = FrameworkElementFactory(typeof<TextBlock>)
        massFactory.SetValue(TextBlock.TextProperty, Binding("Tag"))
        massFactory.SetValue(TextBlock.FontFamilyProperty, mono)
        massFactory.SetValue(TextBlock.FontSizeProperty, 11.5)
        massFactory.SetValue(TextBlock.ForegroundProperty, Binding("Brush"))
        massTemplate.VisualTree <- massFactory
        massList.ItemTemplate <- massTemplate
        massList.Background <- panel
        massList.BorderThickness <- Thickness(0.0)
        massList.Foreground <- normal
        massRefreshBtn.Click.Add(fun _ -> refreshMassList ())
        massFindBtn.Click.Add(fun _ -> findFirstInRegion ())

        let massTop =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        massRefreshBtn.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
        massTop.Children.Add massRefreshBtn |> ignore
        massTop.Children.Add massHint |> ignore
        DockPanel.SetDock(massTop, Dock.Top)
        massPanel.Children.Add massTop |> ignore

        let massBottom =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 0.0))

        massBottom.Children.Add massFindBtn |> ignore
        DockPanel.SetDock(massBottom, Dock.Bottom)
        massPanel.Children.Add massBottom |> ignore
        massPanel.Children.Add massList |> ignore // last child fills the pane
        massTab.Content <- massPanel
        let scanTab = TabItem(Header = "Scan")
        let scanPanel = DockPanel(Margin = Thickness(4.0))
        let scanTop = WrapPanel()
        scanStartBtn.Margin <- Thickness(0.0, 0.0, 8.0, 3.0)
        scanTop.Children.Add scanStartBtn |> ignore

        for b in
            [ scanChangedBtn :> FrameworkElement
              scanSameBtn :> FrameworkElement
              scanSmallerBtn :> FrameworkElement
              scanLargerBtn :> FrameworkElement
              scanWrittenBtn :> FrameworkElement
              scanExecutedBtn :> FrameworkElement ] do
            b.Margin <- Thickness(0.0, 0.0, 6.0, 3.0)
            b.IsEnabled <- false
            scanTop.Children.Add b |> ignore

        scanExactBtn.IsEnabled <- false

        let exactRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 6.0, 3.0))

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

        let jumpRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 4.0, 0.0, 0.0))

        let scanJumpBtn =
            Button(Content = "Jump to selected", Width = 120.0, Margin = Thickness(0.0, 0.0, 6.0, 0.0))

        let scanCmtBtn = Button(Content = "Comment selected", Width = 130.0)
        jumpRow.Children.Add scanJumpBtn |> ignore
        jumpRow.Children.Add scanCmtBtn |> ignore
        scanOpts.Children.Add jumpRow |> ignore
        DockPanel.SetDock(scanOpts, Dock.Bottom)
        scanPanel.Children.Add scanOpts |> ignore
        scanPanel.Children.Add scanList |> ignore
        scanTab.Content <- scanPanel

        let graphTab = TabItem(Header = "Call graph")

        let callGraphScroll =
            ScrollViewer(
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            )

        callGraphScroll.Content <- graphCanvas
        graphTab.Content <- callGraphScroll

        let contractTab = TabItem(Header = "Contract/Prompt")
        let contractPanel = DockPanel()

        let contractTop =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))

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

        let contractScroll =
            ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto)

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

        let pasteLabel =
            TextBlock(
                Text = "paste the model's F# reply here, then Stage to Lifted/:",
                Foreground = dim,
                FontSize = 11.0
            )

        pasteSection.Children.Add pasteLabel |> ignore
        pasteSection.Children.Add pasteBox |> ignore
        DockPanel.SetDock(pasteSection, Dock.Bottom)
        theaterPanel.Children.Add pasteSection |> ignore
        theaterText.Foreground <- normal
        theaterText.FontFamily <- mono
        theaterText.FontSize <- 11.0
        theaterText.TextWrapping <- TextWrapping.Wrap

        let theaterScroll =
            ScrollViewer(VerticalScrollBarVisibility = ScrollBarVisibility.Auto)

        theaterScroll.Content <- theaterText
        theaterPanel.Children.Add theaterScroll |> ignore
        theaterTab.Content <- theaterPanel

        // Analyze tab: the recursive code mapper (bench codemap's GUI twin).
        // Runs on a background task over a defensive copy of memory, shows a
        // readable text report, and saves it into the game folder.
        let anaTab = TabItem(Header = "Analyze")

        let anaPanel = DockPanel(Margin = Thickness(4.0))
        let anaTop = WrapPanel(Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        let anaEntryBox = TextBox(Width = 64.0, Background = panel, Foreground = normal)

        anaEntryBox.ToolTip <-
            "entry address ($F500 / 0xF500 / decimal); empty = control entryPc, else the current PC"

        let anaAllCheck =
            CheckBox(
                Content = "map whole image",
                IsChecked = Nullable<bool>(true),
                Margin = Thickness(8.0, 0.0, 8.0, 0.0),
                Foreground = normal
            )

        anaAllCheck.ToolTip <-
            "after the entry-reachable map, seed every remaining CALL target in the image so the whole program maps"

        let anaRomCheck =
            CheckBox(
                Content = "follow ROM calls",
                IsChecked = Nullable<bool>(false),
                Margin = Thickness(0.0, 0.0, 8.0, 0.0),
                Foreground = normal
            )

        anaRomCheck.ToolTip <-
            "walk into the 48K ROM when RAM code CALLs/RSTs there; off keeps the map and the report RAM-only (ROM targets still show as call sites)"

        let anaRunBtn = Button(Content = "Map code", Width = 90.0, Margin = Thickness(0.0, 0.0, 8.0, 0.0))
        let anaSaveBtn = Button(Content = "Save report", Width = 100.0, IsEnabled = false)

        let anaProgress =
            TextBlock(Text = "", Foreground = dim, VerticalAlignment = VerticalAlignment.Center)

        anaTop.Children.Add anaEntryBox |> ignore
        anaTop.Children.Add anaAllCheck |> ignore
        anaTop.Children.Add anaRomCheck |> ignore
        anaTop.Children.Add anaRunBtn |> ignore
        anaTop.Children.Add anaSaveBtn |> ignore
        anaTop.Children.Add anaProgress |> ignore
        DockPanel.SetDock(anaTop, Dock.Top)
        anaPanel.Children.Add anaTop |> ignore

        let anaText =
            TextBox(
                IsReadOnly = true,
                FontFamily = mono,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = panel,
                Foreground = normal
            )

        anaPanel.Children.Add anaText |> ignore
        anaTab.Content <- anaPanel

        let mutable anaRunning = false
        let mutable anaLines: string list = []

        let anaDefaultEntry () =
            match control with
            | Some c when c.EntryPc >= 0x4000 && c.EntryPc < 0x10000 -> c.EntryPc
            | _ ->
                match session with
                | Some s -> (int (s.Regs.Pc())) &&& 0xFFFF
                | None -> 0x4000

        anaRunBtn.Click.Add(fun _ ->
            if anaRunning then
                ()
            else
                match session with
                | None -> anaProgress.Text <- "load a game first"
                | Some s ->
                    let entry =
                        let t = anaEntryBox.Text.Trim()

                        if t.Length > 0 then
                            GameProject.parseAddr t
                        else
                            let d = anaDefaultEntry ()
                            anaEntryBox.Text <- sprintf "$%04X" d
                            d

                    if entry < 0 || entry > 0xFFFF then
                        anaProgress.Text <- "entry out of range"
                    else
                        let seedAll = anaAllCheck.IsChecked = Nullable<bool>(true)
                        let followRom = anaRomCheck.IsChecked = Nullable<bool>(true)
                        // defensive copy: the walker reads while the machine may run
                        let mem = Array.copy s.Memory
                        let names = (match control with Some c -> c.Symbols | None -> [])
                        anaRunning <- true
                        anaRunBtn.IsEnabled <- false
                        anaSaveBtn.IsEnabled <- false
                        anaProgress.Text <- "mapping..."
                        let ui = anaProgress.Dispatcher

                        System.Threading.Tasks.Task.Run(fun () ->
                            try
                                let result =
                                    CodeMap.map mem entry seedAll followRom (fun (f, b, byts) ->
                                        ui.BeginInvoke(
                                            DispatcherPriority.Background,
                                            System.Action(fun () ->
                                                anaProgress.Text <-
                                                    sprintf "functions: %d, blocks: %d, code bytes: %d" f b byts)
                                        )
                                        |> ignore)

                                let lines = CodeMap.renderReport mem result names
                                let codeTotal = result.IsCode |> Array.filter id |> Array.length

                                ui.BeginInvoke(
                                    DispatcherPriority.Normal,
                                    System.Action(fun () ->
                                        anaLines <- lines
                                        anaText.Text <- String.concat "\n" lines

                                        anaProgress.Text <-
                                            sprintf
                                                "done: %d functions, %d blocks, %d code bytes"
                                                result.Functions.Length
                                                result.Blocks.Length
                                                codeTotal

                                        anaRunBtn.IsEnabled <- true
                                        anaSaveBtn.IsEnabled <- true
                                        anaRunning <- false)
                                )
                                |> ignore
                            with ex ->
                                ui.BeginInvoke(
                                    DispatcherPriority.Normal,
                                    System.Action(fun () ->
                                        anaProgress.Text <- sprintf "analysis failed: %s" ex.Message
                                        anaRunBtn.IsEnabled <- true
                                        anaRunning <- false)
                                )
                                |> ignore)

                        |> ignore

                    ())

        anaSaveBtn.Click.Add(fun _ ->
            let dir = controlDir ()

            if dir = "" then
                anaProgress.Text <- "no game folder - load a game first"
            else
                try
                    let path = Path.Combine(dir, "analysis_report.txt")
                    File.WriteAllLines(path, anaLines)
                    anaProgress.Text <- sprintf "report saved: %s" path
                with ex ->
                    anaProgress.Text <- sprintf "save failed: %s" ex.Message)

        // CE tab: build + display the game's CE program from the live control
        // file (labels + function names as label cells). Labels and
        // disassembly switch independently; both off yields the null CE -
        // raw bytes only - for runner testing. The parity gate is absolute:
        // a failed parity shows in the status and Save stays disabled.
        let ceTab = TabItem(Header = "CE")

        let cePanel = DockPanel(Margin = Thickness(4.0))
        let ceTop = WrapPanel(Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        let ceLabelsCheck =
            CheckBox(
                Content = "labels",
                IsChecked = Nullable<bool>(true),
                Margin = Thickness(0.0, 0.0, 8.0, 0.0),
                Foreground = normal
            )

        ceLabelsCheck.ToolTip <-
            "control-file symbols become named label cells; off emits purely numeric jumps"

        let ceDisasmCheck =
            CheckBox(
                Content = "disassembly",
                IsChecked = Nullable<bool>(true),
                Margin = Thickness(0.0, 0.0, 8.0, 0.0),
                Foreground = normal
            )

        ceDisasmCheck.ToolTip <-
            "decode code blocks to vocab ops; off leaves every byte raw - with labels off this is the null CE"

        let ceGenBtn = Button(Content = "Generate", Width = 100.0, Margin = Thickness(0.0, 0.0, 8.0, 0.0))

        let ceSaveBtn =
            Button(
                Content = "Save CE",
                Width = 100.0,
                Margin = Thickness(0.0, 0.0, 8.0, 0.0),
                IsEnabled = false,
                ToolTip = "write versions/vNNN_<mode>.fs and materialize games/<id>/CeProgram.fs"
            )

        let ceProgress =
            TextBlock(Text = "", Foreground = dim, VerticalAlignment = VerticalAlignment.Center)

        ceTop.Children.Add ceLabelsCheck |> ignore
        ceTop.Children.Add ceDisasmCheck |> ignore
        ceTop.Children.Add ceGenBtn |> ignore
        ceTop.Children.Add ceSaveBtn |> ignore
        ceTop.Children.Add ceProgress |> ignore
        DockPanel.SetDock(ceTop, Dock.Top)
        cePanel.Children.Add ceTop |> ignore

        let ceText =
            TextBox(
                IsReadOnly = true,
                FontFamily = mono,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = panel,
                Foreground = normal
            )

        cePanel.Children.Add ceText |> ignore
        ceTab.Content <- cePanel

        let mutable ceBuilding = false
        let mutable ceBuild: CeBuild.BuildResult option = None

        ceGenBtn.Click.Add(fun _ ->
            if ceBuilding then
                ()
            else
                match control, controlDir () with
                | Some c, dir when dir <> "" ->
                    ceBuilding <- true
                    ceGenBtn.IsEnabled <- false
                    ceSaveBtn.IsEnabled <- false
                    ceProgress.Text <- "building..."
                    let ui = ceProgress.Dispatcher

                    let opts =
                        { CeBuild.Labels = ceLabelsCheck.IsChecked = Nullable<bool>(true)
                          CeBuild.Disassembly = ceDisasmCheck.IsChecked = Nullable<bool>(true) }

                    // the image comes from the control file's own original
                    // bytes on disk - not the live machine, which may have
                    // mutated RAM since boot
                    System.Threading.Tasks.Task.Run(fun () ->
                        try
                            let mem = CeBuild.imageOf dir c
                            let result = CeBuild.build mem c opts

                            ui.BeginInvoke(
                                DispatcherPriority.Normal,
                                System.Action(fun () ->
                                    ceBuild <- Some result
                                    ceText.Text <- result.Body

                                    ceProgress.Text <-
                                        if result.ParityOk then
                                            sprintf
                                                "PARITY OK: %d ops, %d labels, %.1f%% raw, %d lines"
                                                result.OpCount
                                                result.LabelCount
                                                result.RawPct
                                                result.Lines
                                        else
                                            let rb, eb = result.ParityLengths

                                            sprintf
                                                "PARITY FAILED at $%04X (%d vs %d bytes) - Save disabled"
                                                result.ParityDiffAt
                                                rb
                                                eb

                                    ceSaveBtn.IsEnabled <- result.ParityOk
                                    ceBuilding <- false)
                            )
                            |> ignore
                        with ex ->
                            ui.BeginInvoke(
                                DispatcherPriority.Normal,
                                System.Action(fun () ->
                                    ceProgress.Text <- sprintf "build failed: %s" ex.Message
                                    ceGenBtn.IsEnabled <- true
                                    ceBuilding <- false)
                            )
                            |> ignore)
                    |> ignore

                    ceGenBtn.IsEnabled <- false
                | _ -> ceProgress.Text <- "no control file for this game yet - use New ctrl first")

        ceSaveBtn.Click.Add(fun _ ->
            match ceBuild, controlDir () with
            | Some b, dir when dir <> "" && b.ParityOk ->
                try
                    let vd = Path.Combine(dir, "versions")
                    Directory.CreateDirectory vd |> ignore

                    let nextIdx =
                        if Directory.Exists vd then
                            (Directory.GetFiles(vd, "v*.fs")
                             |> Array.choose (fun f ->
                                 let name = Path.GetFileName f

                                 if
                                     name.Length >= 4
                                     && (name.Substring(1, 3) |> Seq.forall System.Char.IsDigit)
                                 then
                                     Some(int (name.Substring(1, 3)))
                                 else
                                     None)
                             |> fun a ->
                                 if a.Length = 0 then
                                     -1
                                 else
                                     Array.max a)
                            + 1
                        else
                            0

                    let labelsOn = ceLabelsCheck.IsChecked = Nullable<bool>(true)
                    let disasmOn = ceDisasmCheck.IsChecked = Nullable<bool>(true)

                    let tag =
                        if not disasmOn then "null_ce" elif not labelsOn then "nolabels" else "ce_gui"

                    let text = CeBuild.fileText { CeBuild.Labels = labelsOn; CeBuild.Disassembly = disasmOn } b.Body
                    let name = sprintf "v%03d_%s.fs" nextIdx tag
                    File.WriteAllText(Path.Combine(vd, name), text)
                    File.WriteAllText(Path.Combine(dir, "CeProgram.fs"), text)
                    ceProgress.Text <- sprintf "saved: %s + CeProgram.fs (control.json untouched)" name
                with ex ->
                    ceProgress.Text <- sprintf "save failed: %s" ex.Message
            | _ -> ())

        right.Items.Add heatTab |> ignore
        right.Items.Add gfxTab |> ignore
        right.Items.Add funcTab |> ignore
        right.Items.Add massTab |> ignore
        right.Items.Add graphTab |> ignore
        right.Items.Add scanTab |> ignore
        right.Items.Add anaTab |> ignore
        right.Items.Add ceTab |> ignore
        right.Items.Add contractTab |> ignore
        right.Items.Add theaterTab |> ignore
        // The mass-comment list reads the control file on demand: refresh when
        // the tab becomes visible (and via its Refresh button).
        right.SelectionChanged.Add(fun e ->
            if Object.ReferenceEquals(e.OriginalSource, right) && right.SelectedItem = massTab then
                refreshMassList ())
        // toolbar
        let toolbar =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(8.0))

        let runBtn = Button(Content = "Run")
        let pauseBtn = Button(Content = "Pause")
        let stepFrameBtn = Button(Content = "Step frame")

        let stepBackBtn =
            Button(
                Content = "Step <- instr",
                ToolTip =
                    "step one instruction backwards: re-executes the current frame from its start (recording suppressed) and parks the machine right after the previous instruction"
            )

        let saveBtn = Button(Content = "Save trace")

        let saveTimelineBtn =
            Button(
                Content = "Save timeline",
                ToolTip = "save the per-frame state timeline + keys to the active slot file"
            )

        let clearTimelineBtn =
            Button(
                Content = "Clear timeline",
                ToolTip =
                    "delete the active slot: wipes the states and key script in memory and deletes its file from the game folder (the project is untouched)"
            )

        let loadBtn = Button(Content = "Load trace")

        runBtn.Click.Add(fun _ ->
            pauseGame ()

            match session with
            | Some s when replaying || s.ReplayFinished ->
                s.StopReplay()
                replaying <- false
            | _ -> ()

            running <- true
            frameTimer.Start())

        pauseBtn.Click.Add(fun _ ->
            pauseGame ()

            if replaying then
                replaying <- false)

        stepFrameBtn.Click.Add(fun _ ->
            pauseGame ()
            renderFrame ())

        stepBackBtn.Click.Add(fun _ ->
            match session, currentTrace () with
            | Some s, Some t when cursor > 0 && t.Entries.Length > 0 && t.FrameTicks.Length > 0 ->
                let target = cursor - 1
                let e = t.Entries[target]
                // The session frame containing the target entry. The entry state IS
                // frame 0 and boundary j is recorded when session frame j+1
                // completes, so the frame is (boundaries at-or-before the tick) + 1
                // and its entries sit between boundaries [frame - 2] and [frame - 1].
                let rec search (lo: int) (hi: int) =
                    if lo >= hi then
                        lo
                    else
                        let mid = (lo + hi) >>> 1

                        if int64 t.FrameTicks[mid] <= int64 e.Tick then
                            search (mid + 1) hi
                        else
                            search lo mid

                let frame = search 0 (t.FrameTicks.Length - 1) + 1

                let firstInFrame =
                    if frame = 1 then
                        0
                    else
                        let tick = t.FrameTicks[frame - 2]

                        let rec s2 (lo: int) (hi: int) =
                            if lo >= hi then
                                lo
                            else
                                let mid = (lo + hi) / 2

                                if t.Entries[mid].Tick < tick then
                                    s2 (mid + 1) hi
                                else
                                    s2 lo mid

                        s2 0 t.Entries.Length

                let steps = target - firstInFrame + 1

                if steps <= 0 then
                    statusText.Text <- "cursor already at the frame start"
                else
                    pauseGame ()
                    s.ParkAtInstruction(frame, steps)
                    cursor <- target
                    presentGame ()
                    showLiveRegs s |> ignore
                    refreshAll ()
                    syncSlider ()
                    timeline.Playhead <- int64 s.Frame
                    flameCtrl.PlayheadFrame <- s.Frame
                    statusText.Text <- sprintf "stepped back to instr %d ($%04X, frame %d)" target (int e.Pc) frame
            | _ -> statusText.Text <- "step back needs a live session with a trace cursor")

        saveBtn.Click.Add(fun _ -> saveTrace ())
        saveTimelineBtn.Click.Add(fun _ -> saveTimeline true)

        clearTimelineBtn.Click.Add(fun _ ->
            match session with
            | Some s ->
                pauseGame ()
                replaying <- false
                s.StopReplay()
                s.ResetTimeline()
                flameTrace <- None
                flameCache.Clear()

                match currentGame with
                | Some game ->
                    try
                        File.Delete(slotPath game currentSlot)
                    with _ ->
                        ()

                    statusText.Text <- sprintf "%s: slot '%s' cleared" game.Name currentSlot
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
            if session.IsSome then
                pauseGame ()

            rewinding <- true)

        rewindSlider.PreviewMouseUp.Add(fun _ -> rewinding <- false)

        rewindSlider.ValueChanged.Add(fun args ->
            match session with
            | Some s ->
                let target = int args.NewValue

                if rewinding && target <= s.TimelineExtent then
                    if replaying then
                        s.StopReplay()
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


        recordToggle.ToolTip <- "record every executed instruction into the ring buffer"
        recordToggle.Foreground <- normal
        recordToggle.VerticalAlignment <- VerticalAlignment.Center

        /// Day/night switch from inside the emulator: shared coordinator plus
        /// the comment-pane rebuild the menu path has no use for.
        let setTheme (m: Theme.Mode) =
            applyTheme m
            rebuildCommentPane true

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
                let dlg =
                    Microsoft.Win32.SaveFileDialog(
                        Filter = "Markdown (*.md)|*.md",
                        FileName = sprintf "fn%04X.md" r.Entry
                    )

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
                        sprintf
                            "// Staged lift for 0x%04X (span 0x%04X-0x%04X)\n// Add this file to Jetpac3.Core in Visual Studio, register the routine in\n// LiftedRoutines.registry, rebuild, then validate from the Theater tab.\n\n"
                            r.Entry
                            r.SpanLo
                            r.SpanHi

                    File.WriteAllText(path, header + pasteBox.Text)
                    statusText.Text <- "staged to " + path
                else
                    statusText.Text <- "paste the model's F# first"
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
                            Validation.run
                                rom
                                tzx
                                framesN
                                script
                                Jetpac3.Core.LiftedRoutines.registryHook
                                System.Threading.CancellationToken.None)

                    task.ContinueWith(fun (t: System.Threading.Tasks.Task<Validation.Report>) ->
                        self.Dispatcher.Invoke(
                            System.Action(fun () ->
                                if t.IsFaulted then
                                    theaterText.Text <- "validation crashed: " + t.Exception.Message
                                else
                                    let rep = t.Result

                                    if rep.Passed then
                                        theaterText.Text <-
                                            sprintf
                                                "VALIDATION PASSED: %d frames, 0 diffs (lifted 0x%04X == oracle)"
                                                rep.Frames
                                                r.Entry
                                    else
                                        match rep.FirstDivergence with
                                        | Some d ->
                                            theaterText.Text <-
                                                sprintf
                                                    "VALIDATION FAILED at frame %d (%s)\n\nport   executed %04X: %s\noracle executed %04X: %s\n\nport   regs: %s\noracle regs: %s"
                                                    d.Frame
                                                    d.Kind
                                                    d.PortPc
                                                    d.PortExecuted
                                                    d.OraclePc
                                                    d.OracleExecuted
                                                    d.PortRegs
                                                    d.OracleRegs
                                        | None -> theaterText.Text <- "VALIDATION FAILED: no divergence details")
                        ))
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
                        sprintf "saved %d prompts to %s:\n%s" saved.Count dir (String.concat ", " saved)
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
                        sprintf
                            "validating queue (%d of %d queued covered by the registry) over %d frames..."
                            covered.Length
                            queued.Length
                            framesN

                    let task =
                        System.Threading.Tasks.Task.Run(fun () ->
                            Validation.run
                                rom
                                tzx
                                framesN
                                script
                                Jetpac3.Core.LiftedRoutines.registryHook
                                System.Threading.CancellationToken.None)

                    task.ContinueWith(fun (t: System.Threading.Tasks.Task<Validation.Report>) ->
                        self.Dispatcher.Invoke(
                            System.Action(fun () ->
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
                                                sb.AppendLine(sprintf "%04X  NOT LIFTED (not in registry)" e)
                                                |> ignore
                                            elif rep.Passed then
                                                sb.AppendLine(sprintf "%04X  PASSED" e) |> ignore
                                            else
                                                match rep.FirstDivergence with
                                                | Some d when d.Address >= r.SpanLo && d.Address <= r.SpanHi ->
                                                    sb.AppendLine(
                                                        sprintf
                                                            "%04X  FAILED: divergence inside its span at frame %d (%s)"
                                                            e
                                                            d.Frame
                                                            d.Kind
                                                    )
                                                    |> ignore
                                                | Some d ->
                                                    sb.AppendLine(
                                                        sprintf
                                                            "%04X  PASSED (divergence is outside its span: %04X %s)"
                                                            e
                                                            d.Address
                                                            d.Kind
                                                    )
                                                    |> ignore
                                                | None ->
                                                    sb.AppendLine(sprintf "%04X  FAILED (no divergence details)" e)
                                                    |> ignore

                                    theaterText.Text <- sb.ToString())
                        ))
                    |> ignore)

        let sep =
            Border(Width = 1.0, Height = 24.0, Background = dim, Margin = Thickness(8.0, 0.0, 8.0, 0.0))

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

        // Recording slots: switch timelines, start live play, or arm a fresh
        // recording. Switching reboots warm into the choice (entry cache, no
        // oracle wait) and reuses the boot-completion path.
        let liveChoice = "▶ Live play"
        let recordChoice = "● Record new…"

        let slotCombo =
            ComboBox(
                Width = 150.0,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "recording slot: switch timelines, play live, or record new"
            )

        let mutable syncingSlots = false

        let refreshSlotCombo () =
            syncingSlots <- true

            try
                slotCombo.Items.Clear()

                match currentGame with
                | Some game ->
                    for s in TimelineSlots.list game.GameDirectory do
                        slotCombo.Items.Add s.Name |> ignore
                | None -> ()

                slotCombo.Items.Add liveChoice |> ignore
                slotCombo.Items.Add recordChoice |> ignore

                let current =
                    match session with
                    | Some s when s.LiveMode -> liveChoice
                    | _ -> if currentSlot = "" then liveChoice else currentSlot

                slotCombo.SelectedItem <- current
            finally
                syncingSlots <- false

        let switchChoice (text: string) =
            match currentGame with
            | None -> ()
            | Some game ->
                pauseGame ()
                saveTimeline false

                let preset =
                    if text = liveChoice then
                        LiveFresh
                    elif text = recordChoice then
                        RecordFresh(TimelineSlots.suggestName ())
                    else
                        AutoplaySlot text

                match bootTask with
                | Some _ ->
                    // Boot already in flight: queue the choice onto it instead of
                    // starting a second boot that would race the first.
                    pendingStart <- Some preset
                    statusText.Text <- "boot in flight - choice applies on arrival"
                | None -> launchGame (game, Some preset)

        slotCombo.SelectionChanged.Add(fun _ ->
            if not syncingSlots then
                match slotCombo.SelectedItem with
                | :? string as text ->
                    let alreadyThere =
                        match session with
                        | Some s when s.LiveMode -> text = liveChoice
                        | _ -> text <> liveChoice && text = currentSlot

                    if not alreadyThere then
                        switchChoice text
                | _ -> ())

        /// Live play has no trace: execution view, trace stepping, replay and
        /// seeking have nothing to work on. Memory/graph views, live stepping
        /// (into), scan and comments keep working.
        let applyLiveGating () =
            let live =
                match session with
                | Some s -> s.LiveMode
                | None -> false

            execModeBtn.IsEnabled <- not live
            stepOverBtn.IsEnabled <- not live
            traceStepBackBtn.IsEnabled <- not live
            stepBackBtn.IsEnabled <- not live
            b100.IsEnabled <- not live
            b10.IsEnabled <- not live
            b1.IsEnabled <- not live
            f1.IsEnabled <- not live
            f10.IsEnabled <- not live
            f100.IsEnabled <- not live
            playBtn.IsEnabled <- not live
            rewindSlider.IsEnabled <- not live
            replayBtn.IsEnabled <- not live
            goBtn.IsEnabled <- not live
            recordToggle.IsEnabled <- true

            if live && not disasmModeMemory && not disasmModeGraph then
                memModeBtn.IsChecked <- Nullable<bool>(true)

        recordToggle.Checked.Add(fun _ ->
            match session with
            | Some s when s.LiveMode ->
                // Leaving live play: reseed the timeline at the live frame and
                // capture from here (the skipped frames cannot join any chain).
                s.ArmRecording()
                currentSlot <- TimelineSlots.suggestName ()
                applyLiveGating ()
                refreshSlotCombo ()
                refreshAll ()
                rebuildCommentPane true
                statusText.Text <- sprintf "recording armed from live play into slot '%s'" currentSlot
            | Some s -> s.Recorder.RecordEnabled <- true
            | None -> ())

        recordToggle.Unchecked.Add(fun _ ->
            match session with
            | Some s -> s.Recorder.RecordEnabled <- false
            | None -> ())

        // game selection + manual boot + script export
        // CE registry moved to GameImages.register (the launcher needs it
        // before this window is constructed).
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
        // The project picked in the startup picker (this method is only
        // reached through the picker, so there is no fallback choice).
        let startupGame = game

        let startupIndex =
            gamesList |> List.findIndex (fun g -> g.GameId = startupGame.GameId)

        engineCombo.Items.Add("Interpreter") |> ignore
        engineCombo.Items.Add("CE") |> ignore
        engineCombo.SelectedIndex <- 0

        engineCombo.SelectionChanged.Add(fun _ ->
            match engineCombo.SelectedIndex with
            | 1 ->
                if not (switchEngine true) then
                    engineCombo.SelectedIndex <- 0
            | _ -> switchEngine false |> ignore)

        gameCombo.SelectedIndex <- startupIndex

        gameCombo.SelectionChanged.Add(fun _ ->
            match gameCombo.SelectedItem with
            | :? GameManifest as g ->
                ceGame <- None
                engineCE <- false
                engineCombo.SelectedIndex <- 0
                parityLabel.Text <- ""
                // Pause unconditionally: with the CE engine active `session` is None,
                // and the old conditional skip left the frame/cinema timers running
                // into the new game.
                pauseGame ()
                session <- None
                loaded <- None
                built <- None
                builtAtCount <- -1 // a stale count could suppress the new trace build
                flameTrace <- None
                flameCache.Clear()
                // Invalidate a flame build running for the previous game: its worker
                // sees the new generation, cancels, and a stale completion is ignored
                // instead of installing the old game's window.
                flameGeneration <- flameGeneration + 1
                flameBuilding <- false
                flameCtrl.Clear() // the old game's window must not show through the boot
                cursor <- 0 // the old cursor indexed the previous game's trace
                execViewCenter <- 0
                execViewHold <- false
                // Brush ranges are frame numbers of the PREVIOUS game's timeline:
                // stale brushes drive auto-build/jump/preview into ranges the new
                // session cannot serve.
                timeline.RangeA <- { Start = 0L; End = 0L }
                timeline.RangeB <- { Start = 0L; End = 0L }
                memCursor <- 0x8000
                memCursorReason <- "game switch"
                memViewTop <- None
                pendingMemJumpScroll <- false
                previewFrom <- 0
                previewSpan <- 0
                previewTicks <- 0
                // Mining, lift queue, and scan results describe the previous game.
                minedRoutines <- []
                minedEdges <- []
                selectedEntries <- Set.empty
                activeRoutine <- None
                refreshRoutines ()
                routineDetail.Text <- ""
                refreshTray ()
                drawGraph ()
                contractText.Text <- ""
                contractLabel.Text <- ""
                scanBaseline <- None
                scanBaselineFrame <- -1
                scanResults <- []
                scanList.ItemsSource <- null
                scanCountLabel.Text <- ""
                engineCombo.IsEnabled <- hasCE g
                saveControlNow () // persist the PREVIOUS project's dirty labels/comments first
                launchGame (g, None)
                loadControlForGame ()
                refreshControlLists () // names/mass lists must not keep the previous game's rows
            | _ -> ())

        setEntryBtn.Click.Add(fun _ ->
            match manualBoot with
            | Some mb ->
                mb.CaptureEntry()
                manualBoot <- None
                manualTimer.Stop()
                statusText.Text <- "game entry captured - starting the port session..."

                match currentGame with
                | Some g ->
                    launchGame (g, None)
                    loadControlForGame ()
                | None -> ()
            | None -> statusText.Text <- "no manual boot running (the game has a cached entry or boots automatically)")

        exportScriptBtn.Click.Add(fun _ ->
            match session with
            | Some s ->
                let dlg =
                    Microsoft.Win32.SaveFileDialog(Filter = "JSON (*.json)|*.json", FileName = "script.json")

                if dlg.ShowDialog() = Nullable<bool>(true) then
                    let body =
                        s.KeyLog.Events
                        |> Seq.map (fun e ->
                            sprintf
                                "  { \"frame\": %d, \"row\": %d, \"bit\": %d, \"pressed\": %b }"
                                e.Frame
                                e.Row
                                e.Bit
                                e.Pressed)
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
            with ex ->
                statusText.Text <- sprintf "timeline demo failed: %s" ex.Message)

        // Back to the project picker: pause, persist an unsaved recording,
        // then swap this window's content back to the menu. The session stays
        // alive in memory - reopening the same project resumes it.
        let backBtn =
            Button(
                Content = "<- back to main menu",
                ToolTip =
                    "pause and return to the project picker (an unsaved recording is saved first; the session stays in memory)"
            )

        backBtn.Click.Add(fun _ -> returnToMenu ())

        for c in
            [ backBtn :> FrameworkElement
              gameCombo :> FrameworkElement
              engineCombo :> FrameworkElement
              parityLabel :> FrameworkElement
              setEntryBtn :> FrameworkElement
              exportScriptBtn :> FrameworkElement
              timelineBtn :> FrameworkElement
              runBtn :> FrameworkElement
              pauseBtn :> FrameworkElement
              stepFrameBtn :> FrameworkElement
              stepBackBtn :> FrameworkElement
              recordToggle :> FrameworkElement
              soundToggle :> FrameworkElement
              saveBtn :> FrameworkElement
              saveTimelineBtn :> FrameworkElement
              clearTimelineBtn :> FrameworkElement
              loadBtn :> FrameworkElement
              slotCombo :> FrameworkElement ] do
            c.Margin <- Thickness(4.0, 0.0, 4.0, 0.0)
            toolbar.Children.Add c |> ignore

        toolbar.Children.Add sep |> ignore
        // rewind + replay group
        for c in
            [ rewindSlider :> FrameworkElement
              timeLabel :> FrameworkElement
              recStatsLabel :> FrameworkElement
              goBtn :> FrameworkElement
              replayBtn :> FrameworkElement ] do
            c.Margin <- Thickness(4.0, 0.0, 4.0, 0.0)
            toolbar.Children.Add c |> ignore

        for c in
            [ b100 :> Control
              b10 :> Control
              b1 :> Control
              playBtn :> Control
              f1 :> Control
              f10 :> Control
              f100 :> Control ] do
            c.Margin <- Thickness(2.0, 0.0, 2.0, 0.0)
            toolbar.Children.Add c |> ignore

        toolbar.Children.Add(
            TextBlock(
                Text = "speed",
                Foreground = dim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(6.0, 0.0, 2.0, 0.0)
            )
        )
        |> ignore

        speedBox.VerticalAlignment <- VerticalAlignment.Center
        toolbar.Children.Add speedBox |> ignore

        // status bar
        statusText.Foreground <- normal
        statusText.Margin <- Thickness(8.0)
        statusText.TextWrapping <- TextWrapping.Wrap

        // lift-queue tray
        let trayRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(8.0, 0.0, 8.0, 4.0))

        let trayTitle =
            TextBlock(
                Text = "Lift queue:",
                Foreground = dim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(0.0, 0.0, 4.0, 0.0)
            )

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
                Background = Theme.hiBg,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            )

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
        main.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))
        Grid.SetColumn(left, 0)
        Grid.SetColumn(center, 1)
        // Draggable border between the code pane (center) and the tab pane
        // (names / heatmap / ...): dragging widens one at the other's cost.
        // Same pattern as the control-map splitter above.
        let tabsSplitter =
            GridSplitter(
                Width = 4.0,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Theme.hiBg,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            )

        Grid.SetColumn(tabsSplitter, 2)
        right.MinWidth <- 260.0
        Grid.SetColumn(right, 3)
        main.Children.Add left |> ignore
        main.Children.Add center |> ignore
        main.Children.Add tabsSplitter |> ignore
        main.Children.Add right |> ignore
        Grid.SetRow(main, 1)
        root.Children.Add main |> ignore
        emulatorRoot <- Some root

        // Just-the-Game / Cheat phases: the emulator machinery is built
        // (timers, wiring, boot) but stays invisible - only the chosen
        // bare view shows.
        if launchPhase = "game" then self.ShowGameOnly()
        elif launchPhase = "cheat" then self.ShowCheatEngine()
        else self.Content <- root

        // ---- control GUI wiring ------------------------------------------------
        // Preview = replay the selection as a movie lasting exactly one second
        // (50 ticks x 20 ms), leaving the session parked on the last frame.
        let scrubToFrame (u: int64) =
            match session with
            | Some s ->
                if running || replaying then
                    pauseGame ()

                if replaying then
                    s.StopReplay()
                    replaying <- false

                previewTimer.Stop()
                // Seek to any recorded frame, future included, and park there.
                let target = max 0 (min (int u) s.TimelineExtent)
                s.JumpTo(target)
                presentGame ()
                updateTimeLabel s.Frame
                seekCodeView s target
            | None -> ()

        previewRangeImpl <-
            fun which ->
                let fStart, fEnd = rangeOf which
                let label = if which = A then "A" else "B"

                match session with
                | Some s when fEnd - 1 <= s.TimelineExtent && fEnd - 1 >= 0 ->
                    if running || replaying then
                        pauseGame ()

                    if replaying then
                        replaying <- false // JumpTo stops the movie core-side

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
                flameCtrl.PlayheadFrame <- s.Frame

                if previewTicks >= 50 then
                    previewTimer.Stop()
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
                labelEditor <- None
                gotoMemCursor (addr &&& 0xFFFF) reason
                refreshDisasm ()
                rebuildCommentPane true
            else
                match currentTrace (), session with
                | Some t, _ when
                    t.Entries.Length > 0
                    && addr < t.FirstIndexAtPc.Length
                    && t.FirstIndexAtPc[addr] >= 0
                    ->
                    cursor <- t.FirstIndexAtPc[addr]
                    memCursor <- addr &&& 0xFFFF
                    // drop the wheel/click window anchor (like jumpToPc): a
                    // held anchor re-renders centered on the OLD rows, so the
                    // jump lands outside the visible window and looks dead
                    execViewHold <- false
                    refreshAll ()
                    syncSlider ()
                    rebuildCommentPane true
                | Some t, _ ->
                    statusText.Text <-
                        sprintf
                            "%s: $%04X is not in the current trace window (%d entries) - rebuild the flame over frames containing it"
                            reason
                            addr
                            t.Entries.Length
                | _ -> statusText.Text <- sprintf "%s: no trace - play the game first" reason

        // Hex address jump (code pane toolbar). Bare numbers are hex here.
        let parseHexAddr (s: string) : int option =
            let t = s.Trim().ToLowerInvariant()

            let t =
                if t.StartsWith("0x") then t.Substring 2
                elif t.StartsWith("$") then t.Substring 1
                else t

            match
                Int32.TryParse(t, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture)
            with
            | true, v when v >= 0 && v <= 0xFFFF -> Some v
            | _ -> None

        let jumpToPc () =
            if disasmModeMemory then
                match session with
                | Some s ->
                    gotoMemCursor ((int (s.Regs.Pc()) &&& 0xFFFF)) "pc jump"
                    refreshDisasm ()
                | None -> statusText.Text <- "no live machine - PC jump needs a running session"
            else
                match currentTrace () with
                | Some t when t.Entries.Length > 0 ->
                    execViewHold <- false // drop the wheel/click anchor: recenter on the cursor
                    refreshDisasm ()
                | _ -> statusText.Text <- "no trace - play the game first"

        let jumpToAddress () =
            match parseHexAddr jumpAddrBox.Text with
            | None -> statusText.Text <- "enter a hex address (e.g. 7309 or 0x7309)"
            | Some raw ->
                let addr = raw &&& 0xFFFF

                if disasmModeMemory then
                    focusCodeView "address jump" addr
                else
                    match currentTrace () with
                    | Some t when addr < t.FirstIndexAtPc.Length && t.FirstIndexAtPc[addr] >= 0 ->
                        focusCodeView "address jump" addr
                    | _ -> statusText.Text <- sprintf "no execution recorded at 0x%04X in this trace" addr

        jumpPcBtn.Click.Add(fun _ -> jumpToPc ())
        jumpGoBtn.Click.Add(fun _ -> jumpToAddress ())

        jumpAddrBox.KeyDown.Add(fun e ->
            if e.Key = Key.Enter then
                jumpToAddress ())

        // names tab (inserted into the strip right after Heatmap): the whole
        // control file at a glance - named symbols, the block map (the default
        // "whole_span" block lives here until regions get named), and every
        // comment (line/name/range/exec/frames). Double-click jumps to the
        // row's address in the code view.
        let namesTab = TabItem(Header = "Names")
        let namesPanel = DockPanel(Margin = Thickness(4.0))
        let namesList = ListBox()
        let namesRefreshBtn = Button(Content = "Refresh", Width = 90.0)

        let namesCountLabel =
            TextBlock(
                Foreground = dim,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(8.0, 0.0, 0.0, 0.0)
            )

        let addRow (text: string) (color: Brush) (addr: int) (menuItems: (string * (unit -> unit)) list) =
            let tb =
                TextBlock(
                    Text = text,
                    Foreground = color,
                    FontFamily = mono,
                    FontSize = 11.5,
                    Margin = Thickness(6.0, 1.0, 6.0, 1.0)
                )

            let item = ListBoxItem(Content = tb, Tag = box addr)

            if not (List.isEmpty menuItems) then
                let menu = ContextMenu()

                for (label, act) in menuItems do
                    let mi = MenuItem(Header = label)
                    mi.Click.Add(fun _ -> act ())
                    menu.Items.Add mi |> ignore

                item.ContextMenu <- menu

            namesList.Items.Add item |> ignore

        let refreshNamesList () =
            namesList.Items.Clear()
            namesCountLabel.Text <- ""

            let refreshAfterEdit (msg: string) =
                refreshControlLists ()
                refreshDisasm ()
                rebuildCommentPane true
                statusText.Text <- msg

            match control with
            | None -> addRow "no control file loaded - use 'New ctrl' or 'Import ctrl...' first" dim -1 []
            | Some c ->
                // named function entry points (feed the flame graph labels);
                // right-click deletes the symbol
                for (a, n) in c.Symbols do
                    addRow
                        (sprintf "symbol   %04X            %s" a n)
                        green
                        a
                        [ (sprintf "delete symbol '%s' at $%04X" n a,
                           fun () ->
                               control <- Some(ControlFile.renameSymbol c a "")
                               refreshAfterEdit (sprintf "symbol at $%04X deleted" a)) ]

                // the block map (structural - no delete here)
                for b in c.Blocks do
                    let kind =
                        match b.Kind with
                        | Code -> "code"
                        | Data -> "data"
                        | Gap -> "gap"

                    addRow (sprintf "block    %04X..%04X  %s  (%s)" b.Start b.EndExcl b.Name kind) cyan b.Start []

                // every comment in the file; right-click deletes it
                for m in c.Comments do
                    let span =
                        if m.EndExcl > m.Addr then
                            sprintf "%04X..%04X" m.Addr m.EndExcl
                        else
                            sprintf "%04X" m.Addr

                    let color =
                        match m.Kind with
                        | CommentKind.Name -> yellow
                        | CommentKind.Exec -> orange
                        | _ -> normal

                    let kindText = ControlFile.kindToString m.Kind

                    addRow
                        (sprintf "%-8s %s   %s" kindText span m.Text)
                        color
                        m.Addr
                        [ (sprintf "delete this %s comment" kindText,
                           fun () ->
                               control <- Some(ControlFile.upsert c { m with Text = "" })
                               refreshAfterEdit "comment deleted") ]

                namesCountLabel.Text <-
                    sprintf "%d symbols, %d blocks, %d comments" c.Symbols.Length c.Blocks.Length c.Comments.Length

        let refreshNamesIfVisible () =
            if right.SelectedItem = namesTab then
                refreshNamesList ()

        namesList.MouseDoubleClick.Add(fun _ ->
            match namesList.SelectedItem with
            | :? ListBoxItem as it ->
                match it.Tag with
                | :? int as a when a >= 0 ->
                    let addr = a &&& 0xFFFF
                    // Land on the row's line whatever it takes: execution view when the
                    // address executed in the trace, otherwise switch to the memory
                    // sweep and center on the address there.
                    if disasmModeMemory then
                        focusCodeView "names tab" addr
                    else
                        match currentTrace () with
                        | Some t when
                            t.Entries.Length > 0
                            && addr < t.FirstIndexAtPc.Length
                            && t.FirstIndexAtPc[addr] >= 0
                            ->
                            focusCodeView "names tab" addr
                        | _ ->
                            pendingMemCursor <- Some addr
                            memModeBtn.IsChecked <- Nullable<bool>(true)
                | _ -> ()
            | _ -> ())

        namesList.Background <- panel
        namesList.BorderThickness <- Thickness(0.0)
        namesRefreshBtn.Click.Add(fun _ -> refreshNamesList ())

        let namesTop =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        namesTop.Children.Add namesRefreshBtn |> ignore
        namesTop.Children.Add namesCountLabel |> ignore

        namesTop.Children.Add(
            TextBlock(
                Text = "double-click a row to jump - right-click a symbol or comment row to delete it",
                Foreground = dim,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(8.0, 0.0, 0.0, 0.0)
            )
        )
        |> ignore

        DockPanel.SetDock(namesTop, Dock.Top)
        namesPanel.Children.Add namesTop |> ignore
        namesPanel.Children.Add namesList |> ignore // last child fills the pane
        namesTab.Content <- namesPanel
        // First tab in the strip, and the one open by default. Selecting it
        // here also fires the refresh above, populating the list at startup.
        right.Items.Insert(0, namesTab)
        right.SelectedIndex <- 0

        right.SelectionChanged.Add(fun e ->
            if Object.ReferenceEquals(e.OriginalSource, right) && right.SelectedItem = namesTab then
                refreshNamesList ())
        // Phase switches and game switches route through this hook (declared
        // at class level): the names list AND the mass-comment list re-read
        // the freshly loaded control file instead of keeping the previous
        // project's rows.
        refreshControlLists <-
            fun () ->
                refreshNamesList ()
                refreshMassIfVisible ()


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
                        else
                            statusText.Text <- sprintf "%04X is not an instruction start - split lands on one" addr
                    | None -> ())

                menu.Items.Add split |> ignore
                mkItem "merge into next block" (fun c -> ControlFile.mergeWithNext c addr)

                mkItem "rename block <- comment box" (fun c ->
                    if String.IsNullOrWhiteSpace commentBox.Text then
                        c
                    else
                        ControlFile.renameBlockAt c addr (commentBox.Text.Trim()))

                mkItem "line comment <- comment box" (fun c ->
                    ControlFile.upsert
                        c
                        { Kind = Line
                          Addr = addr
                          EndExcl = 0
                          InstrIndex = -1
                          Text = commentBox.Text })

                mkItem "range over block <- comment box" (fun c ->
                    match ControlFile.blockAt c addr with
                    | Some b ->
                        ControlFile.upsert
                            c
                            { Kind = Range
                              Addr = b.Start
                              EndExcl = b.EndExcl
                              InstrIndex = -1
                              Text = commentBox.Text }
                    | None -> c)

                menu.PlacementTarget <- controlMap
                menu.Placement <- PlacementMode.MousePoint
                menu.IsOpen <- true)

        // ---- flame graph wiring -------------------------------------------------
        // The flame shares the timeline's frame domain: brushes overlay it,
        // scrubbing moves its playhead, a click seeks the machine (and every
        // synced pane) exactly like the selector does. Windows are built
        // off-UI by FlameBuilder from the saved recording's states + key
        // script, then walked into rectangles by FlameWalker.
        flameCtrl.LabelFor <-
            fun addr ->
                match control with
                | Some c -> ControlFile.nameFor c addr
                | None -> sprintf "$%04X" addr

        /// A browsable Trace built from a flame window's instruction tier: raw
        /// uint32 ticks restored from the window's unwrapped ones (a window
        /// spans far under 2^32, so the mask round-trips), and the frame
        /// boundary table padded to session-frame indexes - boundary j ends
        /// session frame j + 1 and the window starts at FirstFrame, so
        /// entryAtFrame / frameOfEntry / pcsInFrames keep speaking session
        /// frames. No snapshots/writes: the builder does not record them.
        let traceOfWindow (w: FlameWindow) : Trace option =
            match w.Entries, w.EntryTicks with
            | Some entries, Some _ when entries.Length > 0 ->
                let raw (t: int64) =
                    uint32 ((w.BaseTick + t) &&& 0xFFFFFFFFL)

                let perPc = Array.zeroCreate<int> 0x10000
                let firstAt = Array.create 0x10000 -1

                for i in 0 .. entries.Length - 1 do
                    let pc = int entries[i].Pc

                    if firstAt[pc] < 0 then
                        firstAt[pc] <- i

                    perPc[pc] <- perPc[pc] + 1

                let bounds =
                    Array.concat [ Array.create (max 0 (w.FirstFrame - 1)) 0u; w.FrameTicks |> Array.map raw ]

                Some
                    { Entries = Array.copy entries
                      Snapshots = [||]
                      Writes = [||]
                      Ports = [||]
                      FrameTicks = bounds
                      PerPcCount = perPc
                      SelfModified = Array.zeroCreate<bool> 0x10000
                      SelfModCount = 0
                      FirstIndexAtPc = firstAt
                      StartTick = raw 0L
                      EndTick = raw (w.EndTick - 1L)
                      Regenerations = 0 }
            | _ -> None

        /// [startTick, endTick] covered by frames [f0, f1Exclusive) in window
        /// ticks, None when the window does not overlap the range.
        let tickRangeOfFrames (w: FlameWindow) (f0: int) (f1Exclusive: int) : (int64 * int64) option =
            if w.FirstFrame < 0 || w.FrameTicks.Length = 0 then
                None
            else
                let lastCovered = w.FirstFrame + w.FrameTicks.Length - 1

                if f1Exclusive - 1 < w.FirstFrame || f0 > lastCovered then
                    None
                else
                    let startTick =
                        if f0 <= w.FirstFrame then
                            0L
                        else
                            w.FrameTicks[min (w.FrameTicks.Length - 1) (f0 - 1 - w.FirstFrame)]

                    let endIdx = f1Exclusive - 1 - w.FirstFrame

                    let endTick =
                        if endIdx >= w.FrameTicks.Length then
                            w.EndTick
                        else
                            w.FrameTicks[max 0 endIdx]

                    Some(startTick, endTick)

        /// Project the selector brushes onto the current flame window.
        let syncFlameRanges () =
            let toTicks which =
                let a, b = rangeOf which

                match flameCtrl.Window with
                | Some w -> tickRangeOfFrames w a b
                | None -> None

            flameCtrl.RangeA <- toTicks A
            flameCtrl.RangeB <- toTicks B

        /// Mass-comment lanes: Frames-kind comments (trace-state ranges) become
        /// muted rows under the tree. Pulled per render by the control, so
        /// renames and new comments show up without any sync hooks.
        flameCtrl.FrameRangesFor <-
            fun () ->
                match flameCtrl.Window, control with
                | Some w, Some c ->
                    c.Comments
                    |> List.filter (fun m -> m.Kind = CommentKind.Frames)
                    |> List.sortBy (fun m -> m.Addr)
                    |> List.choose (fun m ->
                        tickRangeOfFrames w m.Addr m.EndExcl
                        |> Option.map (fun (a, b) -> (a, b, m.Text)))
                | _ -> []

        /// Build (or reuse) the flame window for frames [f0, f1Exclusive).
        /// Heavy build runs on a worker; the result lands via the dispatcher.
        /// A cache hit whose detail tier was trimmed is treated as a miss and
        /// rebuilt: without entries there is no browsable trace and no exact
        /// flame-cursor mapping (clicking a code row could not move the cursor
        /// line onto the timeline). The auto-build coverage check still counts
        /// trimmed windows as covered, so background rebuilds never loop.
        let rec buildFlameRange (f0: int) (f1Exclusive: int) (zoom: bool) =
            match session with
            | Some s when f1Exclusive - 1 >= f0 ->
                let hit =
                    flameCache.TryGet(f0, f1Exclusive - 1)
                    |> Option.filter (fun w -> w.Entries.IsSome)

                match hit with
                | Some w ->
                    // Keep the browsable trace in step with the shown window - also
                    // keeps the flame cursor mapping exact.
                    flameTrace <- traceOfWindow w
                    clampCursor ()
                    flameCtrl.SetWindow w

                    if zoom then
                        flameCtrl.ZoomToFit()
                | None when not flameBuilding ->
                    flameBuilding <- true
                    flameGeneration <- flameGeneration + 1
                    let gen = flameGeneration
                    statusText.Text <- sprintf "building flame graph: frames %d..%d ..." f0 (f1Exclusive - 1)

                    System.Threading.Tasks.Task.Run(fun () ->
                        let result =
                            FlameBuilder.buildFrames s.StateTimeline s.KeyLog f0 (f1Exclusive - 1) ignore (fun () ->
                                flameGeneration <> gen)

                        self.Dispatcher.Invoke(
                            Action(fun () ->
                                flameBuilding <- false

                                if flameGeneration = gen then
                                    match result with
                                    | Some w ->
                                        let stored = flameCache.Add w
                                        // The full-detail window (before the cache trims it)
                                        // becomes the browsable execution trace.
                                        flameTrace <- traceOfWindow w
                                        clampCursor ()
                                        flameCtrl.SetWindow stored

                                        if zoom then
                                            flameCtrl.ZoomToFit()

                                        syncFlameRanges ()

                                        statusText.Text <-
                                            sprintf
                                                "flame graph: frames %d..%d - %d rectangles; code view now shows the built range"
                                                f0
                                                (f1Exclusive - 1)
                                                stored.Rects.Length
                                    | None -> statusText.Text <- "flame graph build cancelled")
                        ))
                    |> ignore
            | _ -> ()

        flameBuildImpl <- fun (a, b, zoom) -> buildFlameRange a b zoom

        timeline.RangeChanged.Add(fun _ -> syncFlameRanges ())

        // Optional view sync: the brush timeline and the flame graph share one
        // viewport (position + zoom). The timeline's domain is frames, the
        // flame's is window-relative ticks; tickRangeOfFrames / frameAtTick
        // bridge them. `syncingViews` stops the two events from feeding each
        // other, and `tickRangeOfFrames` returning None (timeline view outside
        // the built window) leaves the flame graph untouched.
        let mutable syncingViews = false

        let pushTimelineToFlame () =
            if
                syncViewsBtn.IsChecked.HasValue
                && syncViewsBtn.IsChecked.Value
                && not syncingViews
            then
                match flameCtrl.Window with
                | Some w ->
                    let f0, f1 = timeline.Viewport
                    syncingViews <- true

                    match tickRangeOfFrames w (int f0) (int f1 + 1) with
                    | Some(a, b) -> flameCtrl.ZoomToRange(a, b)
                    | None -> ()

                    syncingViews <- false
                | None -> ()

        let pushFlameToTimeline () =
            if
                syncViewsBtn.IsChecked.HasValue
                && syncViewsBtn.IsChecked.Value
                && not syncingViews
            then
                match flameCtrl.Window with
                | Some w ->
                    let o, e = flameCtrl.VisibleRange
                    let f0 = FlameWindow.frameAtTick w o
                    let f1 = FlameWindow.frameAtTick w (max o e)
                    syncingViews <- true
                    timeline.SetViewport(int64 f0, int64 (max 1 (f1 - f0)))
                    syncingViews <- false
                | None -> ()

        timeline.ViewportChanged.Add(fun _ -> pushTimelineToFlame ())
        flameCtrl.ViewportChanged.Add(fun _ -> pushFlameToTimeline ())
        // Toggling on adopts the flame graph's current view immediately.
        syncViewsBtn.Checked.Add(fun _ -> pushFlameToTimeline ())
        flameFitBtn.Click.Add(fun _ -> flameCtrl.ZoomToFit())

        let flameBrushBuild which =
            let a, b = rangeOf which

            if b > a then
                buildFlameRange a b true
            else
                statusText.Text <-
                    sprintf "brush %s is empty - drag a range on the timeline first" (if which = A then "A" else "B")

        flameABtn.Click.Add(fun _ -> flameBrushBuild A)
        flameBBtn.Click.Add(fun _ -> flameBrushBuild B)

        // click-to-seek: park the machine EXACTLY on the clicked instruction by
        // re-executing the frame prefix from the recording (ParkAtInstruction),
        // with the code cursor on the exact entry of the flame trace. Falls
        // back to a frame-level scrub when the window has no instruction tier.
        // Frame-level fallback for flame clicks: clamps the frame into the
        // recording and parks there. Every caller passes the reason the
        // instruction-level seek could not run, and it lands in the status
        // bar - flame clicks are never silent.
        let frameSeek (reason: string) (frame: int) =
            match session with
            | None ->
                statusText.Text <-
                    sprintf "flame click: %s - but there is no live session (CE engine active), nothing to seek" reason
            | Some s ->
                let target = max 0 (min frame s.TimelineExtent)

                let clampNote =
                    if frame > s.TimelineExtent then
                        " - beyond the recording, clamped"
                    else
                        ""

                if target = s.Frame && not running && not replaying then
                    statusText.Text <- sprintf "flame click: %s - already parked at frame %d%s" reason target clampNote
                else
                    statusText.Text <- sprintf "flame click: %s - frame seek to %d%s" reason target clampNote
                    scrubToFrame (int64 frame)

        flameCtrl.SeekRequested.Add(fun (tick, frame) ->
            match session, flameCtrl.Window, flameTrace with
            | Some s, Some w, Some ft when frame >= 0 ->
                let idx = FlameWindow.entryAtTick w tick

                if idx < 0 || idx >= ft.Entries.Length then
                    frameSeek "the clicked tick has no instruction in the flame trace" frame
                else
                    let frameNo = frameOfEntry ft idx
                    // First entry of that frame: boundary [frame - 2] ends frame - 1
                    // (0-padded window leads read as the window's first entry).
                    let firstInFrame =
                        if frameNo < 2 || frameNo - 2 >= ft.FrameTicks.Length then
                            0
                        else
                            let tick' = ft.FrameTicks[frameNo - 2]

                            let rec s2 (lo: int) (hi: int) =
                                if lo >= hi then
                                    lo
                                else
                                    let mid = (lo + hi) / 2

                                    if ft.Entries[mid].Tick < tick' then
                                        s2 (mid + 1) hi
                                    else
                                        s2 lo mid

                            s2 0 ft.Entries.Length
                    // Steps count real instructions: an interrupt marker shares its
                    // Step with the vector instruction.
                    let mutable steps = 0

                    for k in firstInFrame..idx do
                        if ft.Entries[k].Length > 0uy then
                            steps <- steps + 1

                    if steps <= 0 || frameNo < 1 || frameNo - 1 > s.TimelineExtent then
                        let reason =
                            if frameNo - 1 > s.TimelineExtent then
                                "the clicked frame is beyond the recording"
                            else
                                "the tick precedes any instruction of its frame"

                        frameSeek reason frame
                    else
                        pauseGame ()

                        if replaying then
                            s.StopReplay()
                            replaying <- false

                        try
                            s.ParkAtInstruction(frameNo, steps)
                            cursor <- idx
                            presentGame ()
                            showLiveRegs s |> ignore
                            refreshAll ()
                            syncSlider ()
                            updateTimeLabel s.Frame
                            timeline.Playhead <- int64 s.Frame
                            flameCtrl.PlayheadFrame <- s.Frame

                            statusText.Text <-
                                sprintf
                                    "flame seek: instr #%d ($%04X) of frame %d"
                                    idx
                                    (int ft.Entries[idx].Pc)
                                    frameNo
                        with ex ->
                            // The recording branched away under us: fall back to the
                            // frame-level seek, which clamps into the timeline's extent.
                            scrubToFrame (int64 frame)
                            statusText.Text <- sprintf "flame seek fell back to frame %d (%s)" frame ex.Message
            | _ when frame >= 0 ->
                let reason =
                    match session, flameTrace, flameCtrl.Window with
                    | None, _, _ -> "no live session (CE engine active)"
                    | _, None, _ -> "the game is running - instruction data is dropped while playing"
                    | _, _, None -> "no flame window loaded"
                    | _ -> "unmapped click"

                frameSeek reason frame
            | _ -> statusText.Text <- "flame click: this window has no frame mapping - click ignored")

        // right-click: jump to the function, or name it from the comment box
        // (control.json symbols feed the labels here AND the regenerated CE).
        flameCtrl.RectMenu.Add(fun (r, _frame, _entryIdx) ->
            match control with
            | None -> statusText.Text <- "no control file - click New ctrl first"
            | Some _ ->
                let menu = ContextMenu()
                let jump = MenuItem(Header = sprintf "go to $%04X in code view" (int r.Entry))
                jump.Click.Add(fun _ -> focusCodeView "flame jump" (int r.Entry))
                menu.Items.Add jump |> ignore

                let name =
                    MenuItem(Header = sprintf "name function $%04X <- comment box" (int r.Entry))

                name.Click.Add(fun _ ->
                    match control with
                    | Some c when not (String.IsNullOrWhiteSpace commentBox.Text) ->
                        control <- Some(ControlFile.renameSymbol c (int r.Entry) commentBox.Text)
                        saveControlNow ()
                        refreshDisasm ()
                        statusText.Text <- sprintf "symbol %s = $%04X" (commentBox.Text.Trim()) (int r.Entry)
                    | Some _ -> statusText.Text <- "type a name into the comment box first"
                    | None -> ())

                menu.Items.Add name |> ignore
                menu.PlacementTarget <- flameCtrl
                menu.Placement <- PlacementMode.MousePoint
                menu.IsOpen <- true)

        // auto-build: keep a window covering the brush selection (debounced by
        // the timer; the cache makes repeats free).
        let flameAutoTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 700.0)

        flameAutoTimer.Tick.Add(fun _ ->
            if
                flameAutoBtn.IsChecked.HasValue
                && flameAutoBtn.IsChecked.Value
                && session.IsSome
                && not flameBuilding
            then
                let a0, a1 = rangeOf A
                let b0, b1 = rangeOf B

                let target =
                    match (if a1 > a0 then Some(a0, a1) else None), (if b1 > b0 then Some(b0, b1) else None) with
                    | Some x, Some y -> Some(min (fst x) (fst y), max (snd x) (snd y))
                    | Some x, None
                    | None, Some x -> Some x
                    | None, None -> None

                match target with
                | Some(f0, f1) ->
                    let covered =
                        match flameCtrl.Window with
                        | Some w -> tickRangeOfFrames w f0 f1 |> Option.isSome
                        | None -> false

                    if not covered then
                        buildFlameRange f0 f1 false
                | None -> ())

        flameAutoTimer.Start()

        previewABtn.Click.Add(fun _ -> previewRange A)
        previewBBtn.Click.Add(fun _ -> previewRange B)
        saveCtrlBtn.Click.Add(fun _ -> saveControlNow ())

        importCtrlBtn.Click.Add(fun _ ->
            let dlg =
                Microsoft.Win32.OpenFileDialog(
                    Filter = "Control files (*.json;*.ctl)|*.json;*.ctl|All files (*.*)|*.*"
                )

            if dlg.ShowDialog() = Nullable<bool>(true) then
                try
                    let text = File.ReadAllText dlg.FileName

                    let cf =
                        if dlg.FileName.EndsWith(".ctl", StringComparison.OrdinalIgnoreCase) then
                            SkoolCtl.import text
                        else
                            ControlFile.fromJson text

                    control <- Some cf
                    controlGameDir <- controlDir ()

                    statusText.Text <-
                        sprintf
                            "imported %d blocks, %d comments from %s"
                            cf.Blocks.Length
                            cf.Comments.Length
                            dlg.FileName
                with ex ->
                    statusText.Text <- sprintf "import failed: %s" ex.Message)

        exportCtrlBtn.Click.Add(fun _ ->
            match control with
            | Some c ->
                let suggested =
                    currentGame
                    |> Option.map (fun g -> g.GameId + ".ctl")
                    |> Option.defaultValue "game.ctl"

                let dlg =
                    Microsoft.Win32.SaveFileDialog(Filter = "SkoolKit ctrl (*.ctl)|*.ctl", FileName = suggested)

                if dlg.ShowDialog() = Nullable<bool>(true) then
                    File.WriteAllText(dlg.FileName, SkoolCtl.export c)
                    statusText.Text <- sprintf "exported skool ctrl to %s" dlg.FileName
            | None -> statusText.Text <- "no control file loaded")

        newCtrlBtn.Click.Add(fun _ ->
            // Prefer a generated map: the live machine's memory, else the
            // game's cached entry snapshot; the bare skeleton only when no
            // snapshot is reachable. Dirty so "Save ctrl" persists it.
            let generated =
                match session with
                | Some s ->
                    let start, endExcl = CtlGen.ramSpan s.Memory
                    let entryPc = (int (s.Regs.Pc())) &&& 0xFFFF

                    Some(
                        CtlGen.toControlFile
                            (CtlGen.analyze s.Memory start endExcl CtlGen.defaultConfig)
                            entryPc
                            start
                            endExcl
                    )
                | None ->
                    match currentGame with
                    | Some game ->
                        match EntryCache.tryLoad game.Rom game.Tzx with
                        | Some(mem, _state) ->
                            let start, endExcl = CtlGen.ramSpan mem

                            Some(
                                CtlGen.toControlFile
                                    (CtlGen.analyze mem start endExcl CtlGen.defaultConfig)
                                    0
                                    start
                                    endExcl
                            )
                        | None -> None
                    | None -> None

            match generated with
            | Some cf ->
                control <- Some { cf with Dirty = true }
                statusText.Text <- sprintf "control map generated: %d blocks - Save ctrl writes it" cf.Blocks.Length
            | None ->
                control <-
                    Some
                        { ControlFile.empty 0x4000 0x10000 with
                            Dirty = true }

                statusText.Text <- "empty control file created over $4000-$FFFF (no snapshot available)"

            controlGameDir <- controlDir ()
            syncMapData ()
            refreshControlLists ())

        // engine radios reuse switchEngine for oracle/CE; differential runs a
        // short lockstep validation and reports in statusText.
        oracleRadio.Checked.Add(fun _ ->
            if ceGame.IsSome then
                switchEngine false |> ignore)

        ceRadio.Checked.Add(fun _ ->
            if ceGame.IsNone then
                if not (switchEngine true) then
                    oracleRadio.IsChecked <- Nullable<bool>(true))

        diffRadio.Checked.Add(fun _ ->
            match currentGame, session with
            | Some g, Some _ when hasCE g ->
                let framesN = max 60 ((fst (rangeOf A)) - (snd (rangeOf A)) |> abs |> max 300)
                statusText.Text <- sprintf "differential run (%s vs CE) starting..." g.Name
                let rom, tzx = g.Rom, (if g.Tzx <> "" then g.Tzx else g.Rom)

                let task =
                    System.Threading.Tasks.Task.Run(fun () ->
                        Validation.run
                            rom
                            tzx
                            framesN
                            (Jetpac3.Core.Script.defaultSession framesN)
                            Jetpac3.Core.LiftedRoutines.registryHook
                            System.Threading.CancellationToken.None)

                task.ContinueWith(fun (t: System.Threading.Tasks.Task<Validation.Report>) ->
                    self.Dispatcher.Invoke(
                        System.Action(fun () ->
                            if t.IsFaulted then
                                statusText.Text <- "differential failed: " + t.Exception.Message
                            else
                                let rep = t.Result

                                if rep.Passed then
                                    statusText.Text <- sprintf "DIFFERENTIAL OK over %d frames" rep.Frames
                                else
                                    match rep.FirstDivergence with
                                    | Some d ->
                                        statusText.Text <-
                                            sprintf
                                                "DIFFERENTIAL FAILED at frame %d (%s): port=%04X oracle=%04X"
                                                d.Frame
                                                d.Kind
                                                d.PortPc
                                                d.OraclePc
                                    | None -> statusText.Text <- "DIFFERENTIAL FAILED (no details)")
                    ))
                |> ignore
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
            | Some a ->
                gotoMemCursor a "row context menu sync"
                pendingMemCursor <- None
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

            if disasmModeGraph then
                refreshDisasm ())

        graphAllBtn.Unchecked.Add(fun _ ->
            disasmGraphAll <- false

            if disasmModeGraph then
                refreshDisasm ())

        // mass-comment buttons: all read commentBox.Text
        let text () : string = commentBox.Text

        let addComment (m: ControlComment) =
            match control with
            | Some c ->
                // Line comments append (an address can carry several); keyed kinds
                // (exec/name/range) still replace their key.
                control <-
                    (if m.Kind = Line then
                         Some(ControlFile.addLine c m.Addr m.Text)
                     else
                         Some(ControlFile.upsert c m))

                statusText.Text <- sprintf "comment added (%s)" (ControlFile.kindToString m.Kind)
                refreshDisasm ()
                refreshMassIfVisible ()
                refreshNamesIfVisible ()
            | None -> statusText.Text <- "no control file - click New ctrl first"

        idxCmtBtn.Click.Add(fun _ ->
            match currentTrace () with
            | Some t when t.Entries.Length > 0 ->
                addComment
                    { Kind = Exec
                      Addr = 0
                      EndExcl = 0
                      InstrIndex = cursor
                      Text = text () }
            | _ -> statusText.Text <- "no trace - cannot index-comment")

        lineCmtBtn.Click.Add(fun _ ->
            let addr =
                if disasmModeMemory || disasmModeGraph then
                    memCursor
                else
                    match currentTrace () with
                    | Some t when t.Entries.Length > 0 -> int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc
                    | _ -> memCursor

            addComment
                { Kind = Line
                  Addr = addr
                  EndExcl = 0
                  InstrIndex = -1
                  Text = text () })
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
                openRowMenu disasmList row.Addr row.InstrIdx refreshDisasm jumpToPrevExecFromIndex
            | _ -> ())

        nameBeforeBtn.Click.Add(fun _ ->
            match currentTrace (), control with
            | Some t, Some c when t.Entries.Length > 0 ->
                let pcHere = int t.Entries[max 0 (min cursor (t.Entries.Length - 1))].Pc

                let start =
                    let best =
                        (None, c.Blocks)
                        ||> List.fold (fun acc b ->
                            if b.Start <= pcHere then
                                match acc with
                                | None -> Some b.Start
                                | Some m -> Some(max m b.Start)
                            else
                                acc)

                    match best with
                    | None -> 0x4000
                    | Some m -> m

                addComment
                    { Kind = Name
                      Addr = start
                      EndExcl = pcHere
                      InstrIndex = -1
                      Text = text () }
            | _ -> statusText.Text <- "no trace/control")

        let regionComments (which: BrushId) (kind: CommentKind) =
            match currentTrace (), control with
            | Some t, Some c when t.Entries.Length > 0 ->
                let fA, fB = rangeOf which
                let pcs = ControlFile.pcsInFrames t fA fB

                if List.isEmpty pcs then
                    statusText.Text <- sprintf "no instructions executed in range %s" (if which = A then "A" else "B")
                else
                    let mutable cc = c

                    match kind with
                    | k when k = Name ->
                        addComment
                            { Kind = Name
                              Addr = List.head pcs
                              EndExcl = (List.last pcs) + 8
                              InstrIndex = -1
                              Text = text () }
                    | _ ->
                        for pc in pcs do
                            cc <- ControlFile.addLine cc pc (text ())

                        control <- Some { cc with Dirty = true }
                        statusText.Text <- sprintf "%d line comments added" pcs.Length
                        refreshDisasm ()
                        refreshMassIfVisible ()
                        refreshNamesIfVisible ()
            | _ -> statusText.Text <- "no trace/control"

        nameABtn.Click.Add(fun _ -> regionComments A Name)
        nameBBtn.Click.Add(fun _ -> regionComments B Name)
        // Frames A/B: name the brush's TRACE-STATE range (not the memory extent
        // the other mass comments use) - rendered as a lane under the flame
        // graph, since brushes select time, not addresses.
        let framesRange (which: BrushId) =
            match control with
            | None -> statusText.Text <- "no control file - click New ctrl first"
            | Some _ ->
                let fA, fB = rangeOf which

                if fB - fA < 1 then
                    statusText.Text <-
                        sprintf
                            "brush %s is empty - drag a range on the timeline first"
                            (if which = A then "A" else "B")
                elif String.IsNullOrWhiteSpace(text ()) then
                    statusText.Text <- "type the range name into the comment box first"
                else
                    addComment
                        { Kind = Frames
                          Addr = min fA fB
                          EndExcl = max fA fB
                          InstrIndex = -1
                          Text = text () }

        framesABtn.Click.Add(fun _ -> framesRange A)
        framesBBtn.Click.Add(fun _ -> framesRange B)
        cmtABtn.Click.Add(fun _ -> regionComments A Line)
        cmtBBtn.Click.Add(fun _ -> regionComments B Line)

        cmtBNotA.Click.Add(fun _ ->
            match currentTrace () with
            | Some t ->
                let a1, a2 = rangeOf A
                let b1, b2 = rangeOf B

                let onlyA, onlyB =
                    ControlFile.diffPcs (ControlFile.pcsInFrames t a1 a2) (ControlFile.pcsInFrames t b1 b2)

                match control with
                | Some c ->
                    let mutable cc = c

                    for pc in onlyB do
                        cc <-
                            ControlFile.upsert
                                cc
                                { Kind = Line
                                  Addr = pc
                                  EndExcl = 0
                                  InstrIndex = -1
                                  Text = text () }

                    control <- Some { cc with Dirty = true }
                    statusText.Text <- sprintf "%d B-only comments added" onlyB.Length
                    refreshDisasm ()
                    refreshMassIfVisible ()
                    refreshNamesIfVisible ()
                | None -> statusText.Text <- "no control file"
            | None -> ())

        cmtANotB.Click.Add(fun _ ->
            match currentTrace () with
            | Some t ->
                let a1, a2 = rangeOf A
                let b1, b2 = rangeOf B

                let onlyA, onlyB =
                    ControlFile.diffPcs (ControlFile.pcsInFrames t a1 a2) (ControlFile.pcsInFrames t b1 b2)

                match control with
                | Some c ->
                    let mutable cc = c

                    for pc in onlyA do
                        cc <-
                            ControlFile.upsert
                                cc
                                { Kind = Line
                                  Addr = pc
                                  EndExcl = 0
                                  InstrIndex = -1
                                  Text = text () }

                    control <- Some { cc with Dirty = true }
                    statusText.Text <- sprintf "%d A-only comments added" onlyA.Length
                    refreshDisasm ()
                    refreshMassIfVisible ()
                    refreshNamesIfVisible ()
                | None -> statusText.Text <- "no control file"
            | None -> ())

        // scan buttons
        scanStartBtn.Click.Add(fun _ -> startScan ())
        scanChangedBtn.Click.Add(fun _ -> applyScanFilter (fun _ o n -> n <> o))
        scanSameBtn.Click.Add(fun _ -> applyScanFilter (fun _ o n -> n = o))

        scanExactBtn.Click.Add(fun _ ->
            let v = scanExactBox.Text.Trim()

            let parsed =
                if v.StartsWith "$" then
                    Int32.TryParse(v.Substring(1), Globalization.NumberStyles.HexNumber, null)
                elif v.StartsWith "0x" then
                    Int32.TryParse(v.Substring(2), Globalization.NumberStyles.HexNumber, null)
                else
                    Int32.TryParse v

            match parsed with
            | true, value ->
                // n is the 16-bit word at the address in word mode, the single byte
                // in byte mode; each mode compares against exactly that width.
                applyScanFilter (fun _ o n ->
                    if scanWidth16.IsChecked.GetValueOrDefault() then
                        n = (value &&& 0xFFFF)
                    else
                        n = (value &&& 0xFF))
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
        uiTimer.Tick.Add(fun _ -> timeline.Length <- timelineExtent ())
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
                if disasmModeMemory then
                    Some memCursor
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

                if controlMap.IsOutside a then
                    controlMap.CenterOn a

                let lo, hi =
                    if disasmModeMemory then
                        (max 0 (memCursor - 0x40), min 0x10000 (memCursor + 0xC0))
                    else
                        let t =
                            match live with
                            | Some _ -> built
                            | None -> currentTrace ()

                        match t with
                        | Some t when t.Entries.Length > 0 ->
                            let c = max 0 (min cursor (t.Entries.Length - 1))

                            let pcs =
                                [ for j in -20 .. 20 do
                                      let idx = c + j

                                      if idx >= 0 && idx < t.Entries.Length then
                                          int t.Entries[idx].Pc ]

                            if pcs.IsEmpty then
                                (-1, -1)
                            else
                                (List.min pcs, List.max pcs + 1)
                        | _ -> (-1, -1)

                controlMap.DisasmViewport <- (lo, hi)
            | None -> controlMap.CursorAddress <- -1
            // pick up control-file edits made outside refreshDisasm's path
            let stale =
                match controlMap.ControlData, control with
                | Some old, Some newC -> not (Object.ReferenceEquals(old, newC))
                | None, None -> false
                | _ -> true

            if stale then
                controlMap.ControlData <- control

            controlMap.InstrStarts <- getInstrStarts ()
            controlMap.MemoryImage <- currentMemory ()
            controlMap.ExecCounts <- currentCounts ()
            controlMap.SelfModified <- currentSelfModified ()
            )

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
                let lines = -int(float SystemParameters.WheelScrollLines * notches)

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

                                for _ in 1..lines do
                                    a <- (a + max 1 (Disasm.disasmMemory mem a).Length) &&& 0xFFFF

                                memViewTop <- Some a
                            else
                                // Back up to the previous instruction start; starts[0] is
                                // always set, so the walk terminates at worst at address 0.
                                match getInstrStarts () with
                                | None -> memViewTop <- Some((top + lines) &&& 0xFFFF)
                                | Some starts ->
                                    let mutable a = top

                                    for _ in 1 .. -lines do
                                        a <- (a - 1) &&& 0xFFFF

                                        while not starts[a] do
                                            a <- (a - 1) &&& 0xFFFF

                                    memViewTop <- Some a

                            refreshDisasm ()
                    else
                        let n = currentEntryCount ()

                        if n > 0 then
                            // The wheel scrolls the window, not the cursor: the
                            // highlighted instruction stays highlighted and moves with the
                            // content (out of view if you keep scrolling), matching what
                            // the list's own scrollbar does to the selection.
                            execViewCenter <- max 0 (min (execViewCenter + lines) (n - 1))
                            execViewHold <- true
                            refreshDisasm ())

        heatImage.MouseLeftButtonDown.Add(fun e ->
            match currentTrace () with
            | Some t when t.Entries.Length > 0 ->
                let pos = e.GetPosition heatImage
                let x = int pos.X
                let y = int pos.Y

                if x >= 0 && x < 256 && y >= 0 && y < 256 then
                    let pc = (y <<< 8) ||| x

                    let idx =
                        if pc < t.FirstIndexAtPc.Length then
                            t.FirstIndexAtPc[pc]
                        else
                            -1

                    if idx >= 0 then
                        cursor <- idx
                        refreshAll ()
                        syncSlider ()

                        statusText.Text <-
                            sprintf
                                "jumped to 0x%04X (first of %d executions)"
                                pc
                                (if pc < t.PerPcCount.Length then t.PerPcCount[pc] else 0)
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
            let lines = -int(float SystemParameters.WheelScrollLines * notches)

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
            if isTextEntry e then
                ()
            // Debugger stepping: F10 over, F11 into - execution mode only (the
            // memory view has no trace cursor to move).
            elif e.Key = Key.F10 && not disasmModeMemory && not disasmModeGraph then
                e.Handled <- true
                stepOver ()
            elif e.Key = Key.F11 && not disasmModeMemory && not disasmModeGraph then
                e.Handled <- true
                stepInto ()
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
            if isTextEntry e then
                ()
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
        // frameTimer no longer renders: frame pacing moved to the render-loop
        // stepper above (the Background-priority timer starved to ~15 fps
        // behind vsync-paced render passes). Its Start/Stop calls elsewhere
        // are inert now, kept to mark the old run/pause intents.
        frameTimer.Tick.Add(fun _ -> ())
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
                else
                    cinemaPlaying <- false)

        manualTimer.Tick.Add(fun _ ->
            match manualBoot with
            | Some mb ->
                mb.RunFrame()
                gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), mb.ScreenBuffer, 320 * 4, 0)

                statusText.Text <-
                    sprintf
                        "manual boot: frame %d, tape %s - press 'Set as game entry' when the game is ready"
                        mb.FrameCount
                        (if mb.TapePlaying then "playing" else "stopped")
            | None -> ())

        /// Entry picker: 2+ recording slots -> modal choice of slot, fresh
        /// recording, or live play. Dismissal (X) falls back to the newest
        /// slot that is still on disk (deletions shrink the fallback pool).
        let showStartPicker (game: GameManifest) (slots: TimelineSlots.SlotInfo list) : SessionStart =
            let committed = ref false
            let remaining = ref slots
            let choice = ref (defaultChoice slots)

            let win =
                Window(
                    Title = sprintf "Choose trace - %s" game.Name,
                    Width = 560.0,
                    Height = 440.0,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = self,
                    Background = bg
                )

            let body = StackPanel(Margin = Thickness(12.0))

            body.Children.Add(
                TextBlock(
                    Text = "Play a recording, record a new one, or play live.",
                    Foreground = dim,
                    FontSize = 12.0,
                    Margin = Thickness(0.0, 0.0, 0.0, 8.0)
                )
            )
            |> ignore

            let list =
                ListBox(
                    Background = panel,
                    BorderThickness = Thickness(0.0),
                    FontFamily = mono,
                    FontSize = 12.0,
                    MinHeight = 200.0
                )

            for s in slots do
                let frames =
                    match s.Frames with
                    | Some f -> sprintf "%d frames" f
                    | None -> "unreadable"

                let item =
                    ListBoxItem(
                        Content =
                            TextBlock(
                                Text =
                                    sprintf
                                        "%-24s %12s %8.1f MB  %s"
                                        s.Name
                                        frames
                                        (float s.Bytes / (1024.0 * 1024.0))
                                        (s.Modified.ToLocalTime().ToString("yyyy-MM-dd HH:mm")),
                                Foreground = normal,
                                FontFamily = mono,
                                FontSize = 12.0,
                                Margin = Thickness(6.0, 2.0, 6.0, 2.0)
                            ),
                        Tag = box s.Name
                    )

                list.Items.Add item |> ignore

            if list.Items.Count > 0 then
                list.SelectedIndex <- 0

            let playSelected () =
                // no selection (e.g. the last row was just deleted): a no-op,
                // never a commit of the stale pre-fallback choice
                let mutable picked: string option = None

                match list.SelectedItem with
                | :? ListBoxItem as it ->
                    match it.Tag with
                    | :? string as name -> picked <- Some name
                    | _ -> ()
                | _ -> ()

                match picked with
                | Some name ->
                    choice := AutoplaySlot name
                    committed := true
                    win.Close()
                | None -> ()

            // Remove the selected recording from disk (after confirmation) and
            // from the list. The session is not installed yet, so nothing else
            // holds the file open.
            let deleteSelected () =
                match list.SelectedItem with
                | :? ListBoxItem as it ->
                    match it.Tag with
                    | :? string as name ->
                        match !remaining |> List.tryFind (fun s -> s.Name = name) with
                        | None -> ()
                        | Some s ->
                            let frames =
                                match s.Frames with
                                | Some f -> sprintf "%d frames" f
                                | None -> "unreadable length"

                            let answer =
                                MessageBox.Show(
                                    win,
                                    sprintf
                                        "Delete recording '%s'?\n\n%s, %.1f MB, modified %s\n\nThe file is removed from disk - this cannot be undone."
                                        s.Name
                                        frames
                                        (float s.Bytes / (1024.0 * 1024.0))
                                        (s.Modified.ToLocalTime().ToString("yyyy-MM-dd HH:mm")),
                                    "Delete recording",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning,
                                    MessageBoxResult.No
                                )

                            if answer = MessageBoxResult.Yes then
                                try
                                    File.Delete s.Path
                                    remaining := (!remaining |> List.filter (fun x -> x.Name <> s.Name))

                                    match list.SelectedItem with
                                    | :? ListBoxItem -> list.Items.Remove it |> ignore
                                    | _ -> ()
                                with ex ->
                                    MessageBox.Show(
                                        win,
                                        sprintf "delete failed: %s" ex.Message,
                                        "Delete recording",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error
                                    )
                                    |> ignore
                    | _ -> ()
                | _ -> ()

            list.MouseDoubleClick.Add(fun _ -> playSelected ())

            list.KeyDown.Add(fun e ->
                if e.Key = Key.Delete then
                    deleteSelected ()
                    e.Handled <- true)

            body.Children.Add list |> ignore

            let row =
                StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 10.0, 0.0, 0.0))

            let playBtn =
                Button(Content = "Play", Width = 110.0, Margin = Thickness(0.0, 0.0, 8.0, 0.0))

            let delBtn =
                Button(
                    Content = "Delete",
                    Width = 110.0,
                    Margin = Thickness(0.0, 0.0, 8.0, 0.0),
                    ToolTip = "delete the selected recording file (or press Del in the list)"
                )

            let newBtn =
                Button(Content = "Record new", Width = 110.0, Margin = Thickness(0.0, 0.0, 8.0, 0.0))

            let liveBtn = Button(Content = "Live play", Width = 110.0)
            playBtn.Click.Add(fun _ -> playSelected ())
            delBtn.Click.Add(fun _ -> deleteSelected ())

            newBtn.Click.Add(fun _ ->
                choice := RecordFresh(TimelineSlots.suggestName ())
                committed := true
                win.Close())

            liveBtn.Click.Add(fun _ ->
                choice := LiveFresh
                committed := true
                win.Close())

            row.Children.Add playBtn |> ignore
            row.Children.Add delBtn |> ignore
            row.Children.Add newBtn |> ignore
            row.Children.Add liveBtn |> ignore
            body.Children.Add row |> ignore
            win.Content <- body
            win.ShowDialog() |> ignore

            // Dismissal (X) after deletions: the precomputed fallback may name
            // a deleted slot - recompute over whatever is still on disk.
            if not !committed then
                choice := defaultChoice !remaining

            !choice

        let finishInstall () =
            applyLiveGating ()
            refreshSlotCombo ()
            rebuildCommentPane true

        /// 2+ slots and no preset: pause, ask once, install the answer. False
        /// when there is nothing to ask (0-1 slots keep the default path).
        let maybePickStart (game: GameManifest) : bool =
            match session with
            | None -> false
            | Some s ->
                let slots = TimelineSlots.list game.GameDirectory

                if slots.Length < 2 then
                    false
                else
                    pauseGame ()
                    let choice = showStartPicker game slots
                    installSession game s choice
                    finishInstall ()
                    true

        /// First boot of a project without a control.json: run the static
        /// analyzer (CtlGen) over the game-entry snapshot and persist a
        /// default block map. Every boot completion converges here (presets,
        /// legacy single-slot, parked-in-menu; a manual boot reaches it once
        /// "Set as game entry" has relaunched warm), so a new project picks
        /// up a code/data/text map without hand-drawing blocks first. An
        /// in-memory import is never stomped: generation runs only while no
        /// file exists on disk AND the loaded control is still the pristine
        /// empty stub.
        let maybeGenerateDefaultControl (game: GameManifest) (s: TraceSession) =
            if not (File.Exists(Path.Combine(game.GameDirectory, "control.json"))) then
                let pristine =
                    match control with
                    | Some c -> c.Comments.IsEmpty && c.Symbols.IsEmpty && c.Blocks.Length <= 1
                    | None -> true

                if pristine then
                    try
                        let start, endExcl = CtlGen.ramSpan s.Memory
                        let entryPc = (int (s.Regs.Pc())) &&& 0xFFFF

                        let generated =
                            CtlGen.toControlFile
                                (CtlGen.analyze s.Memory start endExcl CtlGen.defaultConfig)
                                entryPc
                                start
                                endExcl

                        control <- Some(ControlFile.save game.GameDirectory generated)
                        controlGameDir <- game.GameDirectory

                        let count kind =
                            generated.Blocks |> List.filter (fun b -> b.Kind = kind) |> List.length

                        statusText.Text <-
                            sprintf
                                "control file created: %d blocks (%d code / %d data / %d gap)"
                                generated.Blocks.Length
                                (count Code)
                                (count Data)
                                (count Gap)

                        syncMapData ()
                        refreshControlLists ()
                    with ex ->
                        statusText.Text <- sprintf "control file generation failed: %s" ex.Message

        /// A finished boot replaces whatever session is installed: the queued
        /// preset installs, or (2+ slots, no preset) the picker runs once, or
        /// the legacy single-slot autoplay takes over. Shared by both uiTimer
        /// branches - a slot switch / live play / record-new choice arrives
        /// while an old session is up, and the finished boot must land there
        /// too (consuming it only when no session existed kept the old machine
        /// alive and silently dropped the choice).
        let consumeFinishedBoot (t: System.Threading.Tasks.Task<TraceSession>) =
            try
                let loadedSession = t.Result
                bootTask <- None
                session <- Some loadedSession
                flameTrace <- None
                flameCache.Clear()
                // Drop the previous session's flame window and cancel its build:
                // the paths that do not run installSession (parked-in-menu,
                // legacy single-slot autoplay) must not keep it on screen.
                flameGeneration <- flameGeneration + 1
                flameBuilding <- false
                flameCtrl.Clear()

                match currentGame with
                | Some game when game.GameId = bootGameId ->
                    maybeGenerateDefaultControl game loadedSession

                    if menuOpen then
                        // The picker is on screen: install the session parked.
                        // A queued picker (2+ slots, no preset) waits for
                        // re-entry; a preset installs on the next tick.
                        replaying <- false

                        if pendingStart.IsNone && TimelineSlots.list game.GameDirectory |> List.length >= 2 then
                            pickQueued <- true

                        statusText.Text <- sprintf "%s: boot finished (parked in main menu)" game.Name
                    else
                        match pendingStart with
                        | Some st ->
                            pendingStart <- None
                            installSession game loadedSession st
                            finishInstall ()
                        | None ->
                            if not (maybePickStart game) then
                                // Single slot or nothing recorded: legacy path.
                                let hasTimeline = loadTimeline game loadedSession

                                if hasTimeline then
                                    replayExtent <- max 1 loadedSession.TimelineExtent
                                    loadedSession.StartReplay()
                                    replaying <- true
                                else
                                    replaying <- false

                                startGame ()

                                if hasTimeline then
                                    statusText.Text <-
                                        sprintf
                                            "%s: timeline loaded (%d frames) - autoplay, seek anywhere"
                                            game.Name
                                            loadedSession.StateTimeline.Count

                                finishInstall ()
                | _ ->
                    pendingStart <- None // stale choice must not leak into an unrelated boot
                    replaying <- false
                    startGame ()
            with ex ->
                statusText.Text <- "boot failed: " + ex.Message

        uiTimer.Tick.Add(fun _ ->
            match session with
            | None ->
                match ceGame with
                | Some c ->
                    if not rewinding then
                        rewindSlider.Maximum <- 0.0
                        rewindSlider.Value <- 0.0

                    updateTimeLabel c.Frame

                    statusText.Text <-
                        sprintf "CE engine: frame=%d tick=%.2fM" c.Frame (float c.CycleCount / 1_000_000.0)
                | None ->
                    match bootTask with
                    | Some t when t.IsCompleted -> consumeFinishedBoot t
                    | Some t ->
                        match bootScreen with
                        | Some buf -> gameBitmap.WritePixels(Int32Rect(0, 0, 320, 256), buf, 320 * 4, 0)
                        | None -> ()

                        let secs = int (System.DateTime.UtcNow - bootStartedAt).TotalSeconds

                        let gameName =
                            match currentGame with
                            | Some g -> g.Name
                            | None -> "game"

                        statusText.Text <-
                            sprintf "booting %s to game entry... %ds, tape frame %d" gameName secs bootFrameNo
                    | None -> ()
            | Some s ->
                // A queued slot switch / live play / record-new choice finishes
                // its boot HERE: the completed task replaces the installed
                // session. This tick's updates below still describe the old
                // machine for one tick; installSession already refreshed the
                // views for the new one.
                match bootTask with
                | Some t when t.IsCompleted -> consumeFinishedBoot t
                | _ -> ()

                breakBtn.IsEnabled <- running

                // Deferred entry: boot parked while the menu was up. A queued
                // preset installs within one tick of re-entry; a queued picker
                // shows once and settles the same way.
                if not menuOpen && bootTask.IsNone then
                    match pendingStart, currentGame with
                    | Some st, Some game ->
                        pendingStart <- None
                        installSession game s st
                        finishInstall ()
                    | None, Some game when pickQueued ->
                        pickQueued <- false
                        ignore (maybePickStart game)
                    | _ -> ()

                if not rewinding then
                    rewindSlider.Maximum <- float (if replaying then replayExtent else max 0 s.TimelineExtent)
                    rewindSlider.Value <- float s.Frame

                timeline.Playhead <- int64 s.Frame
                // Playhead lives on the hit-test-invisible cursor overlay: two
                // lines per tick, the graph itself never repaints for this.
                flameCtrl.PlayheadFrame <- s.Frame
                updateTimeLabel s.Frame


                if running then
                    statusText.Text <-
                        sprintf
                            "frame=%d  tick=%.2fM  instr=%d/%d  distinct-pc=%d  selfmod=%d  rec=%s"
                            s.Frame
                            (float s.CycleCount / 1_000_000.0)
                            s.Recorder.EntryCount
                            s.Recorder.Capacity
                            (s.Recorder.PerPcCount |> Array.fold (fun n c -> if c > 0 then n + 1 else n) 0)
                            s.Recorder.SelfModCount
                            (if s.Recorder.RecordEnabled then "ON" else "OFF")

                if not userDragging && not running then
                    refreshHeatmap ()
                    refreshStrip ()

                if not userDragging && running then
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
                    sprintf
                        "rec %02d:%02d  %.0f MB  %.1f MB/s  %.0f MB/min"
                        (int seconds / 60)
                        (int seconds % 60)
                        mb
                        mbPerSec
                        (mbPerSec * 60.0)
            | _ -> recStatsLabel.Text <- "")
        // comment pane follows the cursor and control-file changes
        uiTimer.Tick.Add(fun _ ->
            rebuildCommentPane false
            updateModeLabel ()
            updateFileStatusPanel ()
            updateReplayTiming ())

        uiTimer.Start()
        manualTimer.Start()
        statsTimer.Start()

        // Launch the manifest-selected default game.
        launchGame (startupGame, None)
        loadControlForGame ()
        refreshControlLists ()

        self.Closed.Add(fun _ ->
            running <- false
            saveTimeline false // only when the recording actually changed
            saveControlNow () // labels/comments: written only when dirty
            frameTimer.Stop()
            cinemaTimer.Stop()
            manualTimer.Stop()
            uiTimer.Stop()
            previewTimer.Stop()
            statsTimer.Stop()
            Audio.Stop())

        statusText.Text <- "booting emulator to game entry..."
        presentGame ()
        refreshAll ()
        syncSlider ()
        setTheme Theme.mode

    /// Open (or focus) the cheat-engine window. It shares the machine: the
    /// accessors hand out the live session's own screen buffer and memory,
    /// so pokes hit the running game and the picture follows project
    /// switches automatically.
    /// Swap the window to the cheat-engine view (CheatEngineView.fs): the
    /// Spectrum screen plus the memory scanner, over the shared live
    /// session. Pokes hit the running game and the picture follows project
    /// switches automatically. Escape or its main-menu button returns to
    /// the launcher.
    member self.ShowCheatEngine() : unit =
        self.Title <-
            sprintf
                "Cheat Engine - %s"
                (match currentGame with
                 | Some g -> g.Name
                 | None -> "no project")

        match cheatView with
        | Some v ->
            self.Content <- v
            v.Focus() |> ignore
        | None ->
            let getScreen () =
                match session with
                | Some s -> Some s.ScreenBuffer
                | None -> None

            let getMemory () =
                match session with
                | Some s -> Some s.Memory
                | None -> None

            let v = CheatEngineView(getScreen, getMemory, returnToMenu)
            cheatView <- Some v
            self.Content <- v
            v.Focus() |> ignore

    /// Swap the window to the bare game view (GameOnlyView.fs): only the
    /// Spectrum screen plus hold-to-reverse playback, no emulator chrome.
    /// The view talks to the shared session through accessors; Escape or
    /// its main-menu button returns to the launcher.
    member self.ShowGameOnly() : unit =
        self.Title <-
            sprintf
                "Just the Game - %s"
                (match currentGame with
                 | Some g -> g.Name
                 | None -> "no project")

        match gameOnlyView with
        | Some v ->
            self.Content <- v
            v.Focus() |> ignore
        | None ->
            let getScreen () =
                match session with
                | Some s -> Some s.ScreenBuffer
                | None -> None

            let getFrame () =
                match session with
                | Some s -> s.Frame
                | None -> 0

            // Reverse can only reach frames that have recorded state: a
            // recorded session covers from its start, but live play records
            // nothing, so there the floor is the current frame - reverse
            // degrades to the "earliest recorded frame" message instead of
            // JumpTo throwing on an unrecorded frame.
            let minFrame () =
                match session with
                | Some s when s.LiveMode || s.TimelineExtent <= 1 -> s.Frame
                | _ -> 1

            let seekFrame (f: int) =
                match session with
                | Some s ->
                    if replaying then
                        s.StopReplay()
                        replaying <- false

                    s.JumpTo(f)
                    presentGame ()
                | None -> ()

            let v =
                GameOnlyView(getScreen, getFrame, minFrame, seekFrame, pauseGame, returnToMenu)

            gameOnlyView <- Some v
            self.Content <- v
            v.Focus() |> ignore

module Program =
    [<EntryPoint>]
    [<STAThread>]
    let main _ =
        let logCrash (label: string) (e: exn) =
            try
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log"),
                    sprintf "%s: %s\n%s" label e.Message e.StackTrace
                )
            with _ ->
                ()

        let app = Application()
        // Log but do not swallow: after the log is written the crash proceeds
        // exactly as it would without the handler.
        app.DispatcherUnhandledException.Add(fun args -> logCrash "dispatcher" args.Exception)

        AppDomain.CurrentDomain.UnhandledException.Add(fun args ->
            match args.ExceptionObject with
            | :? exn as e -> logCrash "appdomain" e
            | _ -> ())
        // One window for the whole app: it opens as the "ZX Spectrum Game
        // Changer" picker and swaps its own content for the emulator when a
        // project opens. Closing it ends the app (default OnLastWindowClose).
        app.Run(MainWindow())
