namespace JetpacFR.Desktop

open System
open System.Globalization
open System.Windows
open System.Windows.Input
open System.Windows.Media
open JetpacFR.Core

module FlameGraphInternal =
    /// HSL (h in 0..360, s/l in 0..1) to a WPF color - the flame palette needs
    /// hue hashing, which RGB cannot express directly.
    let hslToColor (h: float) (s: float) (l: float) : Color =
        let hue2rgb (p: float) (q: float) (t0: float) =
            let mutable t = t0

            if t < 0.0 then
                t <- t + 1.0

            if t > 1.0 then
                t <- t - 1.0

            if t < 1.0 / 6.0 then p + (q - p) * 6.0 * t
            elif t < 0.5 then q
            elif t < 2.0 / 3.0 then p + (q - p) * (2.0 / 3.0 - t) * 6.0
            else p

        if s <= 0.0 then
            let v = byte (l * 255.0 + 0.5)
            Color.FromRgb(v, v, v)
        else
            let q = if l < 0.5 then l * (1.0 + s) else l + s - l * s
            let p = 2.0 * l - q
            let hn = (h % 360.0) / 360.0

            Color.FromRgb(
                byte ((hue2rgb p q (hn + 1.0 / 3.0)) * 255.0 + 0.5),
                byte ((hue2rgb p q hn) * 255.0 + 0.5),
                byte ((hue2rgb p q (hn - 1.0 / 3.0)) * 255.0 + 0.5)
            )

    /// T-state counts on the x axis, compact ("5.0k", "1.2M").
    let formatTicks (t: int64) : string =
        let v = float t

        if abs v >= 1_000_000.0 then
            sprintf "%.1fM" (v / 1_000_000.0)
        elif abs v >= 1_000.0 then
            sprintf "%.1fk" (v / 1_000.0)
        else
            string t

    /// Ellipsize s to at most n characters with a trailing ellipsis character.
    let elide (s: string) (n: int) : string =
        if s.Length <= n then s
        elif n <= 1 then "\u2026"
        else s.Substring(0, n - 1) + "\u2026"

    /// Mnemonic text for traced instruction bytes (tooltip / instruction-level
    /// drill-down). Matches Z80Decode rows the same way Z80CE.decode does, but
    /// from the four recorded bytes instead of a memory image.
    let mnemonicOfBytes (b0: uint8) (b1: uint8) (b2: uint8) (b3: uint8) : string =
        let bytes = [| b0; b1; b2; b3 |]

        let rec go (rows: Jetpac2.Core.Z80Decode.Row list) =
            match rows with
            | [] -> sprintf "DEFB $%02X" b0
            | r :: rest ->
                let len = r.Prefix.Length
                let mutable ok = true
                let mutable i = 0

                while ok && i < len do
                    if r.Mask[i] <> 0uy && bytes[i] <> r.Prefix[i] then
                        ok <- false

                    i <- i + 1

                if ok then r.Format bytes 0 else go rest

        go Jetpac2.Core.Z80Decode.rows


open FlameGraphInternal

/// Zoomable flame graph over one execution window: a series of rectangles
/// spanning the time line, growing downward as the call stack deepens. The
/// x domain is unwrapped T-state ticks relative to the window start; the
/// host maps frames <-> ticks through the window's boundary table.
///
/// Rendering is a single OnRender pass, culled by tick-range stabbing.
/// Interaction mirrors TimelineSelector's conventions:
///   click (no drag)   -> seek to the tick/frame under the cursor
///   drag              -> pan (horizontal time, vertical depth)
///   wheel             -> zoom around the cursor
///   shift+wheel       -> scroll depth
///   right-click       -> RectMenu (the host offers "name function" etc.)
type FlameGraph() as self =
    inherit FrameworkElement()

    let seekRequested = Event<int64 * int>() // (tick, frame; frame -1 when unknown)
    let viewportChanged = Event<unit>()
    let rectMenu = Event<FlameRect * int * int>() // (rect, frame, entry index)

    // Per-mode canvas chrome. Hue-based flame boxes + amber cursor + tints are
    // identical in both modes; only the canvas, lines and lane fills change.
    let chromeBg () =
        if Theme.isLight () then
            Color.FromRgb(0xF8uy, 0xFAuy, 0xFCuy)
        else
            Color.FromRgb(0x0Euy, 0x0Euy, 0x14uy)

    let chromeBorder () =
        if Theme.isLight () then
            Color.FromRgb(0xCBuy, 0xD5uy, 0xE1uy)
        else
            Color.FromRgb(0x3Auy, 0x3Fuy, 0x4Cuy)

    let chromePlay () =
        if Theme.isLight () then
            Color.FromRgb(0x0Fuy, 0x17uy, 0x2Auy)
        else
            Color.FromRgb(0xE8uy, 0xE8uy, 0xE8uy)

    let chromeLane () =
        if Theme.isLight () then
            Color.FromRgb(0xEEuy, 0xF2uy, 0xF7uy)
        else
            Color.FromRgb(0x26uy, 0x2Auy, 0x34uy)

    let chromeLanePen () =
        if Theme.isLight () then
            Color.FromRgb(0xCBuy, 0xD5uy, 0xE1uy)
        else
            Color.FromRgb(0x4Auy, 0x50uy, 0x60uy)

    let chromeLaneEdge () =
        if Theme.isLight () then
            Color.FromArgb(0xB0uy, 0x0Fuy, 0x17uy, 0x2Auy)
        else
            Color.FromArgb(0xB0uy, 0xE8uy, 0xE8uy, 0xE8uy)

    let chromeMixed () =
        if Theme.isLight () then
            Color.FromRgb(0xCBuy, 0xD5uy, 0xE1uy)
        else
            Color.FromRgb(0x3Auy, 0x3Euy, 0x4Auy)

    let bgBrush = SolidColorBrush(chromeBg ())
    let borderPen = Pen(SolidColorBrush(chromeBorder ()), 1.0)

    let gridPen =
        Pen(SolidColorBrush(Color.FromArgb(0x40uy, 0x80uy, 0x80uy, 0x90uy)), 1.0)

    let framePen =
        Pen(SolidColorBrush(Color.FromArgb(0x55uy, 0x40uy, 0xC4uy, 0xFFuy)), 1.0)

    let playPen = Pen(SolidColorBrush(chromePlay ()), 1.5)
    /// Execution-view cursor: dashed amber so it reads apart from the solid
    /// frame playhead.
    let cursorBrush = SolidColorBrush(Color.FromRgb(0xFBuy, 0xBFuy, 0x24uy))
    let cursorPen = Pen(cursorBrush, 1.5)
    do cursorPen.DashStyle <- DashStyles.Dash
    // Shared Theme instance: label text follows the day/night switch untouched.
    let labelFg = Theme.dim

    let typeface =
        Typeface(FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal)

    let boldTypeface =
        Typeface(FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal)

    let skyTint = SolidColorBrush(Color.FromArgb(0x1Euy, 0x38uy, 0xBDuy, 0xF8uy))
    let amberTint = SolidColorBrush(Color.FromArgb(0x1Euy, 0xFBuy, 0xBFuy, 0x24uy))
    // Mass-comment lanes: deliberately muted next to the code boxes.
    let laneBrush = SolidColorBrush(chromeLane ())
    let lanePen = Pen(SolidColorBrush(chromeLanePen ()), 1.0)

    let laneEdgePen = Pen(SolidColorBrush(chromeLaneEdge ()), 2.0)

    /// Warm flame palette: hue hashed per entry address so a function keeps
    /// its color across the whole view. Mid luminance keeps black text legible.
    /// Memoized and frozen: the paint path must not allocate brushes.
    let brushMemo = System.Collections.Generic.Dictionary<int, Brush>()

    let funcColor (entry: int) : Color =
        hslToColor (float ((entry * 47 + 13) % 360)) 0.62 0.62

    let brushFor (entry: int) : Brush =
        match brushMemo.TryGetValue entry with
        | true, b -> b
        | _ ->
            let brush = SolidColorBrush(funcColor entry)
            brush.Freeze()
            brushMemo[entry] <- brush
            brush

    let mutable mixedBrush: Brush =
        let b = SolidColorBrush(chromeMixed ())
        b.Freeze()
        b :> Brush

    let covMemo = System.Collections.Generic.Dictionary<int * int, Brush>()

    let coverageBrush (entry: int) (bucket: int) : Brush =
        if bucket >= 3 then
            brushFor entry
        elif bucket < 0 then
            mixedBrush
        else
            let key = (entry, bucket)

            match covMemo.TryGetValue key with
            | true, b -> b
            | _ ->
                let f = funcColor entry
                let bg = chromeBg ()
                let t = 0.35 + 0.2 * float bucket

                let lerp (a: byte) (b: byte) =
                    byte (Math.Round(float a + (float b - float a) * t))

                let brush =
                    SolidColorBrush(Color.FromRgb(lerp bg.R f.R, lerp bg.G f.G, lerp bg.B f.B))

                brush.Freeze()
                covMemo[key] <- brush
                brush

    // ---- state ---------------------------------------------------------------
    let mutable window: FlameWindow option = None
    /// LOD pyramid for the current window (cleared by SetWindow, built lazily
    /// on the first zoomed-out paint).
    let mutable lod: FlameLod.Lod option = None
    let mutable origin = 0L // tick at the left edge
    let mutable ppTick = 0.01 // pixels per tick
    let mutable firstDepth = 0
    let mutable playheadFrame = -1
    let mutable cursorTick = -1L
    let mutable rangeA: (int64 * int64) option = None
    let mutable rangeB: (int64 * int64) option = None
    let mutable labelFor = fun (addr: int) -> sprintf "$%04X" addr
    /// Mass-comment lanes: (startTick, endTick, text) in window ticks, pulled
    /// per render - the host resolves the control file's Frames comments.
    let mutable frameRangesFor = fun () -> []: (int64 * int64 * string) list
    let mutable hoverPt: Point option = None
    let mutable hoverRect: FlameRect option = None // redraw only when it changes
    let mutable dragging = false
    let mutable dragStartX = 0.0
    let mutable dragStartY = 0.0
    let mutable dragOrigin = 0L
    let mutable dragDepth = 0
    let mutable dragMoved = false

    do
        self.Focusable <- false
        self.ClipToBounds <- true
        self.Cursor <- Cursors.Cross

    [<CLIEvent>]
    member _.SeekRequested = seekRequested.Publish

    [<CLIEvent>]
    member _.ViewportChanged = viewportChanged.Publish

    [<CLIEvent>]
    member _.RectMenu = rectMenu.Publish

    /// Day/night switch: re-tint the canvas chrome, drop the background-blended
    /// memo (hue-only boxes survive), repaint. Call after Theme.apply.
    member this.RefreshTheme() =
        let setBrush (b: Brush) (c: Color) = (b :?> SolidColorBrush).Color <- c
        let setPen (p: Pen) (c: Color) = setBrush p.Brush c
        bgBrush.Color <- chromeBg ()
        setPen borderPen (chromeBorder ())
        setPen playPen (chromePlay ())
        laneBrush.Color <- chromeLane ()
        setPen lanePen (chromeLanePen ())
        setPen laneEdgePen (chromeLaneEdge ())
        let b = SolidColorBrush(chromeMixed ())
        b.Freeze()
        mixedBrush <- b :> Brush
        covMemo.Clear()
        this.InvalidateVisual()

    /// Resolves an entry address into a display name (symbols > blocks > $XXXX).
    member _.LabelFor
        with get () = labelFor
        and set v = labelFor <- v

    /// Pull delegate for mass-comment frame ranges (window ticks + text).
    member _.FrameRangesFor
        with get () = frameRangesFor
        and set v = frameRangesFor <- v

    member _.Window = window

    /// Install a window and reset the viewport to show all of it.
    member this.SetWindow(w: FlameWindow) =
        window <- Some w
        firstDepth <- 0
        cursorTick <- -1L // the old window's tick is meaningless in the new one
        lod <- None
        this.ZoomToFit()

    /// Drop the installed window: the canvas falls back to the empty hint.
    /// Session switches call this - a live play session has no trace at all,
    /// and a new recording's window arrives via SetWindow once its build lands.
    member this.Clear() =
        window <- None
        lod <- None
        firstDepth <- 0
        cursorTick <- -1L
        playheadFrame <- -1
        this.InvalidateVisual()

    member this.PlayheadFrame
        with get () = playheadFrame
        and set v =
            if playheadFrame <> v then
                playheadFrame <- v
                this.InvalidateVisual()

    /// Execution-view cursor position in window ticks (-1 = hidden): the exact
    /// instruction the code pane is on. The host computes it; hidden whenever
    /// the pane's cursor lies outside this window or the pane shows no trace.
    member this.CursorTick
        with get () = cursorTick
        and set v =
            if cursorTick <> v then
                cursorTick <- v
                this.InvalidateVisual()

    /// Brush overlays in tick coordinates (None = unset).
    member this.RangeA
        with get () = rangeA
        and set v =
            rangeA <- v
            this.InvalidateVisual()

    member this.RangeB
        with get () = rangeB
        and set v =
            rangeB <- v
            this.InvalidateVisual()

    // ---- coordinate helpers ----------------------------------------------------

    member private this.VisibleTicks = float (max 1.0 this.ActualWidth) / ppTick

    member private this.ClampOrigin(w: FlameWindow) =
        let endTick = w.EndTick
        let visible = int64 (Math.Round this.VisibleTicks)
        let maxOrigin = max 0L (endTick - visible)
        origin <- min origin maxOrigin |> max 0L

    member private this.TickAt(x: float) = origin + int64 (Math.Round(x / ppTick))

    member private this.XOf(tick: int64) = float (tick - origin) * ppTick

    member private this.RowOf(depth: int) =
        float (depth - firstDepth) * FlameGraph.RowHeight

    member private this.ClampDepth(w: FlameWindow) =
        let visibleRows = int (this.ActualHeight / FlameGraph.RowHeight)
        firstDepth <- max 0 (min firstDepth (w.MaxDepth - visibleRows))

    /// The visible [start, end) tick range, for view synchronization with the
    /// brush timeline.
    member this.VisibleRange: int64 * int64 =
        origin, origin + int64 (Math.Round this.VisibleTicks)

    /// Fit the whole window into the current width.
    member this.ZoomToFit() =
        match window with
        | Some w ->
            ppTick <- float (max 1.0 this.ActualWidth) / float w.EndTick
            origin <- 0L
            this.ClampDepth w
            this.InvalidateVisual()
            viewportChanged.Trigger()
        | None -> ()

    /// Zoom so that [tickFrom, tickTo] spans the full width.
    member this.ZoomToRange(tickFrom: int64, tickTo: int64) =
        match window with
        | Some w ->
            let span = max 1L (tickTo - tickFrom)
            ppTick <- float (max 1.0 this.ActualWidth) / float span
            origin <- tickFrom
            this.ClampOrigin w
            this.InvalidateVisual()
            viewportChanged.Trigger()
        | None -> ()

    member private this.ApplyZoom(factor: float, anchorX: float) =
        match window with
        | Some w ->
            let anchor = this.TickAt anchorX

            let newPpt =
                let v = ppTick * factor
                let minPpt = float (max 1.0 this.ActualWidth) / float w.EndTick
                max (minPpt * 0.999) (min 20.0 v)

            ppTick <- newPpt
            origin <- anchor - int64 (Math.Round(anchorX / ppTick))
            this.ClampOrigin w
            this.InvalidateVisual()
            viewportChanged.Trigger()
        | None -> ()

    // ---- interaction -------------------------------------------------------------

    override this.OnMouseWheel(e: MouseWheelEventArgs) =
        match window with
        | Some w ->
            if Keyboard.Modifiers &&& ModifierKeys.Shift = ModifierKeys.Shift then
                let delta = if e.Delta > 0 then 3 else -3
                firstDepth <- firstDepth + delta
                this.ClampDepth w
                this.InvalidateVisual()
            else
                let factor = if e.Delta > 0 then 1.2 else 1.0 / 1.2
                this.ApplyZoom(factor, e.GetPosition(this).X)

            e.Handled <- true
        | None -> base.OnMouseWheel e

    override this.OnMouseDown(e: MouseButtonEventArgs) =
        let p = e.GetPosition(this)

        if e.ChangedButton = MouseButton.Left then
            dragging <- true
            dragStartX <- p.X
            dragStartY <- p.Y
            dragOrigin <- origin
            dragDepth <- firstDepth
            dragMoved <- false
            self.CaptureMouse() |> ignore

        e.Handled <- true

    override this.OnMouseMove(e: MouseEventArgs) =
        let p = e.GetPosition(this)

        if dragging then
            let dx = p.X - dragStartX
            let dy = p.Y - dragStartY

            if abs dx > 3.0 || abs dy > 3.0 then
                dragMoved <- true

            if dragMoved then
                match window with
                | Some w ->
                    origin <- dragOrigin - int64 (Math.Round(dx / ppTick))
                    this.ClampOrigin w
                    firstDepth <- dragDepth - int (Math.Round(dy / FlameGraph.RowHeight))
                    this.ClampDepth w
                    this.InvalidateVisual()
                | None -> ()
        else
            hoverPt <- Some p
            let r = this.RectAt p

            if hoverRect <> r then
                hoverRect <- r
                this.InvalidateVisual()

        base.OnMouseMove e

    override this.OnMouseUp(e: MouseButtonEventArgs) =
        let p = e.GetPosition(this)

        if e.ChangedButton = MouseButton.Left && dragging then
            dragging <- false
            self.ReleaseMouseCapture()

            if not dragMoved then
                match window with
                | Some w ->
                    let tick = max 0L (min (this.TickAt p.X) w.EndTick)
                    let frame = FlameWindow.frameAtTick w tick
                    seekRequested.Trigger(tick, frame)
                | None -> ()
        elif e.ChangedButton = MouseButton.Right then
            match window, this.RectAt p with
            | Some w, Some r ->
                let frame = FlameWindow.frameAtTick w ((r.StartTick + r.EndTick) / 2L)

                let entryIdx =
                    if r.EndIndex > r.StartIndex then
                        r.EndIndex - 1
                    else
                        r.StartIndex

                rectMenu.Trigger(r, frame, entryIdx)
                e.Handled <- true
            | _ -> ()

        base.OnMouseUp e

    /// The deepest rectangle under a point, if any.
    member private this.RectAt(p: Point) : FlameRect option =
        match window with
        | Some w when p.X >= 0.0 && p.X <= this.ActualWidth ->
            let tick = this.TickAt p.X
            let depth = firstDepth + int (p.Y / FlameGraph.RowHeight)
            let mutable best: FlameRect option = None
            let mutable i = FlameWindow.stab w tick

            while i < w.Rects.Length && w.Rects[i].StartTick <= tick do
                let r = w.Rects[i]

                if r.EndTick > tick && r.Depth = depth then
                    best <- Some r

                i <- i + 1

            best
        | _ -> None

    override this.OnMouseLeave(e: MouseEventArgs) =
        hoverPt <- None
        hoverRect <- None
        this.InvalidateVisual()
        base.OnMouseLeave e

    // ---- rendering ----------------------------------------------------------------

    static member RowHeight = 18.0
    member private this.RowH = FlameGraph.RowHeight

    override this.OnRender(dc: DrawingContext) =
        let w = this.ActualWidth
        let h = this.ActualHeight

        if w <= 0.0 || h <= 0.0 then
            ()
        else
            let dpi = VisualTreeHelper.GetDpi this

            let ft (text: string) (size: float) (fg: Brush) (bold: bool) =
                FormattedText(
                    text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    (if bold then boldTypeface else typeface),
                    size,
                    fg,
                    dpi.PixelsPerDip
                )

            dc.DrawRectangle(bgBrush, borderPen, Rect(0.0, 0.0, w, h))

            match window with
            | None ->
                let hint =
                    ft
                        "flame graph: select a range and press Flame A / Flame B (or let auto-build run)"
                        11.0
                        labelFg
                        false

                dc.DrawText(hint, Point(8.0, 8.0))
            | Some win ->
                // A/B brush tints under everything else.
                let drawTint (r: (int64 * int64) option) (brush: Brush) =
                    match r with
                    | Some(a, b) ->
                        let x0 = this.XOf(max 0L a)
                        let x1 = this.XOf b

                        if x1 > 0.0 && x0 < w then
                            dc.DrawRectangle(brush, null, Rect(max 0.0 x0, 0.0, min w x1 - max 0.0 x0, h))
                    | None -> ()

                drawTint rangeA skyTint
                drawTint rangeB amberTint

                // Grid: nice tick steps over the visible span.
                let visibleSpan = int64 (Math.Round this.VisibleTicks)
                let step = TimelineRuler.niceTickStep (max 1L visibleSpan)
                let firstTick = origin / step * step
                let mutable t = firstTick

                while t <= origin + visibleSpan do
                    if t >= 0L then
                        let x = this.XOf t
                        dc.DrawLine(gridPen, Point(x, 0.0), Point(x, h))
                        let label = ft (formatTicks t) 9.0 labelFg false
                        dc.DrawText(label, Point(min (max 2.0 (x + 2.0)) (w - label.Width - 2.0), h - 13.0))

                    t <- t + step

                // Frame boundaries when they are far enough apart.
                if win.FrameTicks.Length > 0 then
                    let framePx = 1.0 / ppTick

                    if framePx >= 6.0 then
                        let lo = max 0L origin
                        let hi = origin + visibleSpan

                        for j in 0 .. win.FrameTicks.Length - 1 do
                            let bt = win.FrameTicks[j]

                            if bt >= lo && bt <= hi then
                                let x = this.XOf bt
                                dc.DrawLine(framePen, Point(x, 0.0), Point(x, h))

                                if framePx >= 40.0 then
                                    let label = ft (string (win.FirstFrame + j)) 8.0 labelFg false
                                    dc.DrawText(label, Point(x + 2.0, 1.0))

                // Rectangles. Two regimes: once a frame is narrower than the
                // fidelity threshold, paint LOD pyramid runs - geometry is bounded
                // by viewport pixels x visible depth rows, so paint cost does not
                // depend on how many invocations the window holds. Below the
                // threshold, draw raw rectangles with per-invocation fidelity.
                let leftTick = origin
                let rightTick = origin + visibleSpan
                let visibleRows = int (h / this.RowH) + 1

                let blackBrush =
                    let b = SolidColorBrush(Colors.Black)
                    b.Freeze()
                    b

                let lodData =
                    match lod with
                    | Some l -> Some l
                    | None ->
                        let l = FlameLod.build win
                        lod <- Some l
                        Some l

                let framePx =
                    match lodData with
                    | Some l when l.Levels.Length > 0 ->
                        let a, b = FlameLod.cellTickRange l l.Levels[0] 0
                        float (b - a) * ppTick
                    | _ -> 0.0

                if framePx >= 1.0 && framePx < 10.0 then
                    let level = lodData.Value.Levels[FlameLod.pickLevel lodData.Value ppTick 1.0]
                    let depthLast = min win.MaxDepth (firstDepth + visibleRows)

                    for depth in firstDepth .. depthLast - 1 do
                        let y = this.RowOf depth

                        if y > -this.RowH && y < h then
                            let cellFrom = max 0 (FlameLod.cellAt lodData.Value level.Shift leftTick)

                            let cellTo =
                                min level.Cols ((FlameLod.cellAt lodData.Value level.Shift rightTick) + 1)

                            let rFuncs, rBuckets, rStarts, rLens =
                                FlameLod.runs lodData.Value level depth cellFrom cellTo

                            for r in 0 .. rFuncs.Length - 1 do
                                let func = rFuncs[r]
                                let bucket = rBuckets[r]

                                if func >= 0 && bucket >= 0 then
                                    let a, _ = FlameLod.cellTickRange lodData.Value level rStarts[r]
                                    let _, b = FlameLod.cellTickRange lodData.Value level (rStarts[r] + rLens[r] - 1)
                                    let x0 = max -1.0 (this.XOf a)
                                    let x1 = min (w + 1.0) (this.XOf b)
                                    let rw = x1 - x0

                                    if rw >= 1.0 && x1 > 0.0 then
                                        dc.DrawRectangle(
                                            coverageBrush func bucket,
                                            null,
                                            Rect(x0, y, rw, this.RowH - 1.0)
                                        )

                                        if this.RowH >= 12.0 && rw >= 40.0 then
                                            let text = if depth = 0 then "(window root)" else labelFor func

                                            let fg =
                                                if bucket >= 2 then
                                                    blackBrush :> Brush
                                                else
                                                    labelFg :> Brush

                                            let mutable ftText = ft text 10.5 fg true

                                            if ftText.Width > rw - 5.0 then
                                                let t2 = elide text (int (rw / 6.0))
                                                ftText <- ft t2 10.5 fg true

                                            if ftText.Width <= rw - 5.0 then
                                                dc.DrawText(
                                                    ftText,
                                                    Point(x0 + 3.0, y + (this.RowH - 1.0 - ftText.Height) / 2.0)
                                                )
                else
                    let mutable i = FlameWindow.stab win leftTick

                    while i < win.Rects.Length && win.Rects[i].StartTick <= rightTick do
                        let r = win.Rects[i]
                        i <- i + 1
                        let y = this.RowOf r.Depth

                        if y > -this.RowH && y < h && r.EndTick > leftTick then
                            let x0 = max -1.0 (this.XOf r.StartTick)
                            let x1 = min (w + 1.0) (this.XOf r.EndTick)
                            let rw = max 1.0 (x1 - x0)

                            if x1 > 0.0 then
                                let brush = brushFor (int r.Entry)
                                dc.DrawRectangle(brush, null, Rect(x0, y, rw, this.RowH - 1.0))
                                let minWidth = if r.Depth = 0 then 54.0 else 26.0

                                if rw >= minWidth && this.RowH >= 12.0 then
                                    let mutable text = if r.Depth = 0 then "(window root)" else labelFor (int r.Entry)
                                    let mutable ftText = ft text 10.5 blackBrush true

                                    if ftText.Width > rw - 5.0 then
                                        text <- elide text (int (rw / 6.0))
                                        ftText <- ft text 10.5 blackBrush true

                                    if ftText.Width <= rw - 5.0 then
                                        dc.DrawText(
                                            ftText,
                                            Point(x0 + 3.0, y + (this.RowH - 1.0 - ftText.Height) / 2.0)
                                        )
                                // Instruction-level separators when the window kept its
                                // detail tier and instructions are wide enough to matter.
                                match win.EntryTicks with
                                | Some ticks when ppTick >= 3.0 && r.EndIndex - r.StartIndex <= 512 ->
                                    let pen = Pen(SolidColorBrush(Color.FromArgb(0x60uy, 0x00uy, 0x00uy, 0x00uy)), 1.0)
                                    let mutable e = r.StartIndex + 1
                                    let stop = min (r.EndIndex) ticks.Length

                                    while e < stop do
                                        let x = this.XOf ticks[e]

                                        if x >= 0.0 && x <= w then
                                            dc.DrawLine(pen, Point(x, y), Point(x, y + this.RowH - 1.0))

                                        e <- e + 1
                                | _ -> ()

                // Mass-comment lanes: one muted row per frame-range comment that
                // intersects the viewport, one gap row below the deepest box. When
                // the tree is deeper than the pane, lanes pin to the bottom edge
                // (order kept) so the annotations stay readable at any depth. Edges
                // inside the viewport get accent ticks; a range spanning the whole
                // viewport is just the long muted box with its text.
                let ranges =
                    frameRangesFor ()
                    |> List.sortBy (fun (a, _, _) -> a)
                    |> List.filter (fun (a, b, _) -> b > leftTick && a < rightTick)

                let laneCount = ranges.Length

                ranges
                |> List.iteri (fun i (a, b, text) ->
                    let yTree = this.RowOf(win.MaxDepth + 1 + i)
                    let y = min yTree (h - float (laneCount - i) * this.RowH)
                    let x0 = max 0.0 (this.XOf a)
                    let x1 = min w (this.XOf b)

                    if x1 > x0 then
                        dc.DrawRectangle(laneBrush, lanePen, Rect(x0, y, x1 - x0, this.RowH - 1.0))

                        if a >= leftTick then
                            dc.DrawLine(laneEdgePen, Point(x0, y), Point(x0, y + this.RowH - 1.0))

                        if b <= rightTick then
                            dc.DrawLine(laneEdgePen, Point(x1, y), Point(x1, y + this.RowH - 1.0))

                        if this.RowH >= 12.0 && x1 - x0 >= 30.0 then
                            let mutable txt = text
                            let mutable ftText = ft txt 10.5 labelFg false

                            if ftText.Width > x1 - x0 - 8.0 then
                                txt <- elide text (int ((x1 - x0) / 6.0))
                                ftText <- ft txt 10.5 labelFg false

                            if ftText.Width <= x1 - x0 - 8.0 then
                                dc.DrawText(ftText, Point(x0 + 4.0, y + (this.RowH - 1.0 - ftText.Height) / 2.0)))

                // Playhead: the frame the machine is parked on / executing.
                if playheadFrame >= 0 && win.FirstFrame >= 0 then
                    let tick =
                        if playheadFrame < win.FirstFrame then
                            0L
                        elif playheadFrame - win.FirstFrame >= win.FrameTicks.Length then
                            win.EndTick
                        else
                            win.FrameTicks[playheadFrame - win.FirstFrame]

                    let x = this.XOf tick
                    dc.DrawLine(playPen, Point(x, 0.0), Point(x, h))

                // Execution-view cursor: the exact instruction the code pane is on.
                if cursorTick >= 0L then
                    let x = this.XOf cursorTick

                    if x >= -1.0 && x <= w + 1.0 then
                        dc.DrawLine(cursorPen, Point(x, 0.0), Point(x, h))
                        dc.DrawRectangle(cursorBrush, null, Rect(x - 1.5, 0.0, 3.0, 5.0))

                // Tooltip for the rectangle under the cursor.
                match hoverPt, hoverRect with
                | Some p, Some r ->
                    let dur = r.EndTick - r.StartTick

                    let kindName =
                        match r.Kind with
                        | FlameKind.FlameCall -> "call"
                        | FlameKind.FlameRst -> "rst"
                        | FlameKind.FlameInterrupt -> "interrupt"
                        | _ -> "root"

                    let ms = float dur / 3500.0

                    let lines =
                        [ labelFor (int r.Entry) + sprintf " ($%04X)" (int r.Entry)
                          sprintf "%s from $%04X   depth %d" kindName (int r.CallPc) r.Depth
                          sprintf "%dT (%.2f ms)   %d instrs" dur ms (r.EndIndex - r.StartIndex)
                          match win.Entries with
                          | Some entries when r.EndIndex > r.StartIndex ->
                              let e = entries[min (r.EndIndex - 1) (entries.Length - 1)]
                              "last: " + mnemonicOfBytes e.B0 e.B1 e.B2 e.B3
                          | _ -> "" ]

                    let widest =
                        lines |> List.map (fun s -> (ft s 11.0 Brushes.White false).Width) |> List.max

                    let boxW = widest + 12.0
                    let boxH = float lines.Length * 15.0 + 8.0
                    let bx = min (p.X + 14.0) (w - boxW - 2.0) |> max 2.0
                    let by = min (p.Y + 14.0) (h - boxH - 2.0) |> max 2.0
                    let tooltipBg = SolidColorBrush(Color.FromArgb(0xE0uy, 0x10uy, 0x14uy, 0x1Euy))
                    dc.DrawRoundedRectangle(tooltipBg, borderPen, Rect(bx, by, boxW, boxH), 3.0, 3.0)
                    let mutable ly = by + 4.0

                    for idx, line in lines |> List.indexed do
                        let color = if idx = 0 then Brushes.White else labelFg
                        dc.DrawText((ft line 11.0 color (idx = 0)), Point(bx + 6.0, ly))
                        ly <- ly + 15.0
                | _ -> ()
