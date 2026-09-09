namespace JetpacFR.Desktop

open System
open System.Globalization
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Documents
open System.Windows.Input
open System.Windows.Media
open System.Windows.Threading

/// One of the two selection brushes.
type BrushId =
    | A
    | B

/// Half-open selection range [Start, End) in timeline units.
[<Struct>]
type BrushRange =
    { Start: int64
      End: int64 }

    member this.Length = this.End - this.Start

/// Ruler x-scale helpers.
module TimelineRuler =
    /// Tick step for the x scale: the smallest "nice" step
    /// (1 / 2 / 2.5 / 5 x 10^k) that keeps 3-4 tick marks in [0, length],
    /// so the labels read as round numbers (100 200 300, 1000 2000 3000, ...)
    /// regardless of the range size.
    let niceTickStep (length: int64) : int64 =
        let nice = [| 1.0; 2.0; 2.5; 5.0 |]

        let niceCeil (v: float) =
            let mutable base10 = Math.Pow(10.0, floor (Math.Log10 v))
            let mutable m = v / base10
            let mutable s = 0.0

            while s = 0.0 do
                match nice |> Array.tryFind (fun n -> n >= m - 1e-9) with
                | Some n -> s <- n * base10
                | None ->
                    m <- m / 10.0
                    base10 <- base10 * 10.0

            s

        let l = float length
        let mutable step = niceCeil (l / 4.0)

        while floor (l / step) + 1.0 > 4.0 do
            step <- niceCeil (step * 1.01)

        while floor (l / step) + 1.0 < 3.0 && step > 1.0 do
            step <- niceCeil (step / 2.02)

        max 1L (int64 (Math.Ceiling step))

/// Timeline selection strip with two brushes (A = sky, B = amber).
///
/// Positions are plain int64 timeline units - frames today, instruction
/// indices later. The control maps unit <-> pixel purely through `Length`
/// and formats units via `FormatUnit`, so selection precision is a property
/// of the data fed in, not of this control.
///
/// Interaction (ported from the opus HTML tape):
///   drag on the track -> range [anchor, current] for the active brush
///   clean click       -> point range (both markers coincide)
///   drag the "A" chip -> move RangeA.Start; "B" chip -> move RangeB.End
///   A / B keys        -> switch active brush
type TimelineSelector() as self =
    inherit FrameworkElement()

    let rangeChanged = Event<BrushId * BrushRange>()
    let activeChanged = Event<BrushId>()
    let scrubbed = Event<int64>()
    let viewportChanged = Event<unit>()

    // sky/amber accents are identical in both modes; canvas + labels follow it.
    let sky = Theme.sky
    let amber = Theme.amber
    let skyBrush = SolidColorBrush(sky)
    let amberBrush = SolidColorBrush(amber)
    let skyPen = Pen(skyBrush, 2.0)
    let amberPen = Pen(amberBrush, 2.0)
    let skyTint = SolidColorBrush(Color.FromArgb(0x26uy, sky.R, sky.G, sky.B))
    let amberTint = SolidColorBrush(Color.FromArgb(0x26uy, amber.R, amber.G, amber.B))
    let chipFg = SolidColorBrush(Color.FromRgb(0x02uy, 0x06uy, 0x17uy))
    // Shared Theme instance: labels follow the day/night switch untouched.
    let labelFg = Theme.dim
    let labelPen = Pen(labelFg, 1.0)
    let chromeBg () =
        if Theme.isLight () then Color.FromRgb(0xF8uy, 0xFAuy, 0xFCuy)
        else Color.FromRgb(0x10uy, 0x10uy, 0x16uy)
    let chromeBorder () =
        if Theme.isLight () then Color.FromRgb(0xCBuy, 0xD5uy, 0xE1uy)
        else Color.FromRgb(0x3Auy, 0x3Fuy, 0x4Cuy)
    let chromePlay () =
        if Theme.isLight () then Color.FromRgb(0x0Fuy, 0x17uy, 0x2Auy)
        else Color.FromRgb(0xE8uy, 0xE8uy, 0xE8uy)
    let bgBrush = SolidColorBrush(chromeBg ())
    let borderPen = Pen(SolidColorBrush(chromeBorder ()), 1.0)

    let typeface =
        Typeface(FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal)

    let mutable length = 1L
    /// Viewport: first visible unit + pixels per unit. ppUnit <= 0.0 means
    /// "fit the whole length" (recomputed from the actual width on demand).
    let mutable viewOrigin = 0L
    let mutable ppUnit = 0.0
    let mutable rangeA = { Start = 0L; End = 0L }
    let mutable rangeB = { Start = 0L; End = 0L }
    let mutable active = A
    let mutable unitName = "f"
    let mutable formatUnit = fun (n: int64) -> string n
    /// The machine's current frame; -1 hides the playhead marker.
    let mutable playhead = -1L

    let mutable dragging = false
    let mutable dragStartX = 0.0
    let mutable dragAnchor = 0L
    let mutable dragMoved = false
    let mutable chipRectA = Rect()
    let mutable chipRectB = Rect()
    let mutable chipDrag: BrushId option = None
    let mutable chipStartX = 0.0

    do
        self.Focusable <- true
        self.Cursor <- Cursors.Cross

    [<CLIEvent>]
    member _.RangeChanged = rangeChanged.Publish

    [<CLIEvent>]
    member _.ActiveBrushChanged = activeChanged.Publish

    [<CLIEvent>]
    member _.Scrubbed = scrubbed.Publish

    [<CLIEvent>]
    member _.ViewportChanged = viewportChanged.Publish
    /// Day/night switch: re-tint the canvas + border, repaint. Labels are a
    /// shared Theme brush and follow untouched. Call after Theme.apply.
    member this.RefreshTheme() =
        bgBrush.Color <- chromeBg ()
        (borderPen.Brush :?> SolidColorBrush).Color <- chromeBorder ()
        this.InvalidateVisual()

    /// Pixels per visible unit; fit mode computed from the actual width.
    member private this.PpUnit =
        if ppUnit <= 0.0 then
            float (max 1.0 this.ActualWidth) / float length
        else
            ppUnit

    /// The visible unit span.
    member private this.VisibleSpan =
        int64 (Math.Round(float (max 1.0 this.ActualWidth) / this.PpUnit))

    /// Keep the origin inside [0, Length - visibleSpan].
    member private this.ClampViewport() =
        viewOrigin <- max 0L (min viewOrigin (max 0L (length - this.VisibleSpan)))

    /// Show [originUnit, originUnit + spanUnits), clamped into [0, Length].
    /// The view-sync entry point (and shift+wheel pan).
    member this.SetViewport(originUnit: int64, spanUnits: int64) =
        let span = max 1L (min spanUnits length)
        let newPp = float (max 1.0 this.ActualWidth) / float span
        let newOrigin = max 0L (min originUnit (length - span))

        if viewOrigin <> newOrigin || abs (newPp - ppUnit) > 1e-9 then
            viewOrigin <- newOrigin
            ppUnit <- newPp
            this.InvalidateVisual()
            viewportChanged.Trigger()

    /// The visible [start, end) unit range.
    member this.Viewport: int64 * int64 =
        viewOrigin, min length (viewOrigin + this.VisibleSpan)

    /// Show the whole length again.
    member this.ZoomToFit() =
        viewOrigin <- 0L
        ppUnit <- 0.0
        this.InvalidateVisual()
        viewportChanged.Trigger()

    member this.Length
        with get () = length
        and set v =
            length <- max 1L v
            // keep a zoomed viewport usable when the recording grows or shrinks
            if ppUnit > 0.0 then
                this.ClampViewport()

            this.InvalidateVisual()

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

    member this.ActiveBrush
        with get () = active
        and set v =
            active <- v
            this.InvalidateVisual()

    member this.UnitName
        with get () = unitName
        and set v =
            unitName <- v
            this.InvalidateVisual()

    member this.FormatUnit
        with get () = formatUnit
        and set v =
            formatUnit <- v
            this.InvalidateVisual()

    /// Playhead: the frame the machine is currently parked on / executing.
    member this.Playhead
        with get () = playhead
        and set v =
            if playhead <> v then
                playhead <- v
                this.InvalidateVisual()

    /// Timeline unit at a client x coordinate, clamped to the viewport.
    member private this.UnitAt(x: float) =
        let clamped = min (max x 0.0) (max 1.0 this.ActualWidth)
        let u = viewOrigin + int64 (Math.Round(clamped / this.PpUnit))
        max 0L (min u length)

    /// Set a brush range and notify subscribers.
    member private this.SetRange(which: BrushId, r: BrushRange) =
        match which with
        | A -> this.RangeA <- r
        | B -> this.RangeB <- r

        rangeChanged.Trigger(which, r)

    member private this.StartDrag(x: float) =
        dragging <- true
        dragStartX <- x
        dragAnchor <- this.UnitAt x
        dragMoved <- false
        scrubbed.Trigger(dragAnchor)
        this.CaptureMouse() |> ignore

    member private this.UpdateDrag(x: float) =
        if dragging then
            let u = this.UnitAt x
            // scrub fires on every move, before the range guard below: the host
            // shows the frame under the cursor even before the >3px travel turns
            // the press into a real drag.
            scrubbed.Trigger u

            if Math.Abs(x - dragStartX) > 3.0 then
                dragMoved <- true

                this.SetRange(
                    active,
                    { Start = min dragAnchor u
                      End = max dragAnchor u }
                )

    member private this.EndDrag(x: float) =
        if dragging then
            dragging <- false
            this.ReleaseMouseCapture()
            let u = this.UnitAt x

            if not dragMoved then
                // clean click: point range, start and end markers coincide
                this.SetRange(active, { Start = u; End = u })

    /// Pressing on the "A"/"B" label chip starts an endpoint drag instead of
    /// a track drag: A's chip moves RangeA.Start, B's chip moves RangeB.End.
    member private this.TryStartChipDrag(p: Point) : bool =
        let hit (r: Rect) =
            let r = Rect(r.X - 3.0, r.Y - 3.0, r.Width + 6.0, r.Height + 6.0)
            r.Contains p

        let which =
            if hit chipRectA then Some A
            elif hit chipRectB then Some B
            else None

        match which with
        | Some b ->
            chipDrag <- Some b
            chipStartX <- p.X
            dragging <- false
            scrubbed.Trigger(this.UnitAt p.X)
            this.CaptureMouse() |> ignore
            true
        | None -> false

    /// Move the dragged chip's endpoint to the unit under the cursor,
    /// clamped so the range never inverts (Start <= End). A >3px guard keeps
    /// a plain click on the chip (and the cursor-at-press synthesized move)
    /// from touching the range.
    member private this.UpdateChipDrag(x: float) =
        let u = this.UnitAt x
        scrubbed.Trigger u

        if Math.Abs(x - chipStartX) > 3.0 then
            match chipDrag with
            | Some A ->
                let r = rangeA

                this.SetRange(
                    A,
                    { Start = max 0L (min u r.End)
                      End = r.End }
                )
            | Some B ->
                let r = rangeB

                this.SetRange(
                    B,
                    { Start = r.Start
                      End = max r.Start (min u length) }
                )
            | None -> ()

    member private this.EndChipDrag() =
        chipDrag <- None
        this.ReleaseMouseCapture()

    /// wheel      -> zoom around the cursor (out stops at the full length)
    /// shift+wheel-> pan by 10% of the visible span per notch
    override this.OnMouseWheel(e: MouseWheelEventArgs) =
        let w = max 1.0 this.ActualWidth

        if Keyboard.Modifiers &&& ModifierKeys.Shift = ModifierKeys.Shift then
            let span = this.VisibleSpan
            let d = (max 1L (span / 10L)) * (if e.Delta > 0 then 1L else -1L)
            this.SetViewport(viewOrigin + d, span)
        else
            let anchor = this.UnitAt(e.GetPosition(this).X)
            let oldPp = this.PpUnit
            let minPp = float w / float length
            let newPp = max minPp (min 200.0 (oldPp * (if e.Delta > 0 then 1.2 else 1.0 / 1.2)))
            // keep the unit under the cursor anchored at the same pixel
            let anchorX = float (anchor - viewOrigin) * oldPp
            viewOrigin <- max 0L (anchor - int64 (Math.Round(anchorX / newPp)))
            ppUnit <- newPp
            this.ClampViewport()
            this.InvalidateVisual()
            viewportChanged.Trigger()

        e.Handled <- true
        base.OnMouseWheel e

    override this.OnMouseDown(e: MouseButtonEventArgs) =
        if e.LeftButton = MouseButtonState.Pressed then
            let p = e.GetPosition(this)

            if not (this.TryStartChipDrag p) then
                this.StartDrag p.X

        base.OnMouseDown e

    override this.OnMouseMove(e: MouseEventArgs) =
        let p = e.GetPosition(this)

        if dragging then
            this.UpdateDrag p.X
        elif chipDrag.IsSome then
            this.UpdateChipDrag p.X
        else
            // hover feedback: resize grip over a chip handle, crosshair elsewhere
            let overChip = chipRectA.Contains p || chipRectB.Contains p
            this.Cursor <- if overChip then Cursors.SizeWE else Cursors.Cross

        base.OnMouseMove e

    override this.OnMouseUp(e: MouseButtonEventArgs) =
        if chipDrag.IsSome then
            this.EndChipDrag()

        this.EndDrag(e.GetPosition(this).X)
        base.OnMouseUp e

    override this.OnLostMouseCapture(e: MouseEventArgs) =
        dragging <- false
        chipDrag <- None
        base.OnLostMouseCapture e

    override this.OnKeyDown(e: KeyEventArgs) =
        match e.Key with
        | Key.A
        | Key.B ->
            // A/B switch brushes while the timeline has focus; consume them either
            // way: otherwise they bubble to the main window, which maps A/B onto
            // Spectrum matrix cells and logs them into the replay key log.
            let b = if e.Key = Key.A then A else B

            if active <> b then
                active <- b
                activeChanged.Trigger b

            e.Handled <- true
        | _ -> ()

        base.OnKeyDown e

    override this.OnRender(dc: DrawingContext) =
        let w = this.ActualWidth
        let h = this.ActualHeight

        if w <= 0.0 || h <= 0.0 then
            ()
        else
            let dpi = VisualTreeHelper.GetDpi this

            let ft (text: string) (size: float) (fg: Brush) =
                FormattedText(
                    text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    size,
                    fg,
                    dpi.PixelsPerDip
                )

            // rounded track background; bottom 13px is the ruler label zone
            dc.DrawRoundedRectangle(bgBrush, borderPen, Rect(0.5, 0.5, w - 1.0, h - 1.0), 3.0, 3.0)
            let labelZone = 13.0
            let top = 2.0
            let bottom = h - labelZone - 2.0
            let trackH = max 1.0 (bottom - top)

            // viewport x mapping: unit -> client pixel
            let xOf (u: int64) = float (u - viewOrigin) * this.PpUnit

            let drawBrush (which: BrushId) (r: BrushRange) =
                let color, tint, pen =
                    match which with
                    | A -> sky, skyTint, skyPen
                    | B -> amber, amberTint, amberPen

                let x0raw = xOf r.Start
                let x1raw = xOf r.End

                if x1raw > 0.0 && x0raw < w then
                    let x0 = min w (max 0.0 x0raw)
                    let x1 = min w (max 0.0 x1raw)
                    // tinted range
                    dc.DrawRectangle(tint, null, Rect(min x0 x1, top, max 2.0 (abs (x1 - x0)), trackH))
                    // start/end marker lines
                    dc.DrawLine(pen, Point(x0, top), Point(x0, bottom))
                    dc.DrawLine(pen, Point(x1, top), Point(x1, bottom))
                    // label chip: "A" bottom-left of the range, "B" bottom-right (HTML)
                    let label = ft (string which) 9.0 chipFg
                    let chipW = label.Width + 8.0
                    let chipH = 12.0

                    let chipX =
                        if which = A then
                            min x0 (max 0.0 (w - chipW))
                        else
                            max 0.0 (min (x1 - chipW) (w - chipW))

                    let chipY = bottom - chipH
                    let chipRect = Rect(chipX, chipY, chipW, chipH)

                    if which = A then
                        chipRectA <- chipRect
                    else
                        chipRectB <- chipRect

                    dc.DrawRectangle(SolidColorBrush(color), null, chipRect)
                    dc.DrawText(label, Point(chipX + 4.0, chipY + (chipH - label.Height) / 2.0))

            drawBrush A rangeA
            drawBrush B rangeB

            // playhead: bright line + top notch marking the current execution
            // state, drawn over the tints so it reads on both. Hidden while it
            // lies outside the visible viewport.
            if playhead >= 0L then
                let x = xOf playhead

                if x >= -1.0 && x <= w + 1.0 then
                    let playPen = Pen(SolidColorBrush(chromePlay ()), 1.5)
                    dc.DrawLine(playPen, Point(x, top), Point(x, bottom))

                    dc.DrawRectangle(SolidColorBrush(chromePlay ()), null, Rect(x - 2.5, top, 5.0, 5.0))

            // x scale: round tick marks over the visible span, 3-4 visible,
            // labels at round units
            let step = TimelineRuler.niceTickStep (this.VisibleSpan)
            let mutable t = viewOrigin / step * step

            while t <= viewOrigin + this.VisibleSpan do
                if t >= 0L && t <= length then
                    let x = xOf t
                    dc.DrawLine(labelPen, Point(x, bottom - 3.0), Point(x, bottom + 1.0))
                    let label = ft (formatUnit t) 8.0 labelFg
                    let lx = min (max 2.0 (x - label.Width / 2.0)) (w - label.Width - 2.0)
                    dc.DrawText(label, Point(lx, h - 12.0))

                t <- t + step

/// Standalone host exercising TimelineSelector: two toggle buttons, a live
/// debug readout of both ranges, and a length fed from the recording's
/// frame count (live while recording, 300 default when nothing is running).
type TimelineDemoWindow(getFrames: unit -> int64) as self =
    inherit Window()

    let sky = Theme.sky
    let amber = Theme.amber
    let bg = Theme.bg
    let panel = Theme.panel
    let normal = Theme.normal
    let dim = Theme.dim
    let darkFg = Theme.darkFg
    let mono = FontFamily("Consolas")

    let mutable len = max 1L (getFrames ())

    let selector =
        TimelineSelector(Length = len, UnitName = "f", Height = 96.0, Margin = Thickness(8.0, 4.0, 8.0, 8.0))

    do
        selector.RangeA <- { Start = 0L; End = len / 2L }
        selector.RangeB <- { Start = len / 2L; End = len }

    let aBtn =
        RadioButton(Content = "brush A", GroupName = "brush", IsChecked = Nullable<bool>(true))

    let bBtn =
        RadioButton(Content = "brush B", GroupName = "brush", IsChecked = Nullable<bool>(false))

    let styleBtn (btn: RadioButton) (color: Color) (on: bool) =
        // selected: solid brush-color fill + dark text (mirrors the HTML
        // bg-sky-400 / bg-amber-400 text-slate-950 toggle), so the active
        // brush reads at a glance
        btn.Background <- if on then SolidColorBrush(color) else panel
        btn.Foreground <- if on then darkFg else dim
        btn.BorderBrush <- SolidColorBrush(color)
        btn.BorderThickness <- Thickness(1.0)
        btn.FontWeight <- if on then FontWeights.Bold else FontWeights.Normal
        btn.Padding <- Thickness(8.0, 3.0, 8.0, 3.0)

    /// Default RadioButton template paints `Background` only on the bullet
    /// and `Foreground` on the label, so a full-button fill never shows.
    /// Replace it: a rounded Border whose whole surface is the Background,
    /// with the label centered on top. The bullet is dropped - these act as
    /// exclusive toggle buttons (GroupName keeps exclusivity).
    let toggleTemplate =
        let t = ControlTemplate(typeof<RadioButton>)
        let border = FrameworkElementFactory(typeof<Border>)
        border.SetValue(Border.CornerRadiusProperty, CornerRadius(3.0))
        let templated = RelativeSource(RelativeSourceMode.TemplatedParent)

        let bind (dp: DependencyProperty) (path: string) =
            border.SetBinding(dp, Binding(path, RelativeSource = templated))

        bind Border.BackgroundProperty "Background"
        bind Border.BorderBrushProperty "BorderBrush"
        bind Border.BorderThicknessProperty "BorderThickness"
        bind Border.PaddingProperty "Padding"
        let cp = FrameworkElementFactory(typeof<ContentPresenter>)
        cp.SetBinding(ContentPresenter.ContentProperty, Binding("Content", RelativeSource = templated))
        cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center)
        cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center)
        cp.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true)
        border.AppendChild cp
        t.VisualTree <- border
        t

    let debugText =
        TextBlock(FontFamily = mono, FontSize = 12.0, Margin = Thickness(8.0, 6.0, 8.0, 0.0))

    let refreshDebug () =
        debugText.Inlines.Clear()

        let addRange (which: BrushId) (color: Color) =
            let r = if which = A then selector.RangeA else selector.RangeB
            let head = Run(if which = A then "A " else "B ")
            head.Foreground <- SolidColorBrush(color)
            head.FontWeight <- FontWeights.Bold
            debugText.Inlines.Add head

            let body =
                Run(sprintf "%d\u2013%d (%d %s)   " r.Start r.End r.Length selector.UnitName)

            body.Foreground <- normal
            debugText.Inlines.Add body

        addRange A sky
        addRange B amber

    let lenLabel =
        TextBlock(FontFamily = mono, FontSize = 10.0, Foreground = dim, Margin = Thickness(8.0, 0.0, 8.0, 4.0))

    let refreshLen () =
        lenLabel.Text <- sprintf "length = %d %s (from recording)" selector.Length selector.UnitName

    let setActive (b: BrushId) =
        selector.ActiveBrush <- b
        aBtn.IsChecked <- Nullable<bool>((b = A))
        bBtn.IsChecked <- Nullable<bool>((b = B))

    do
        aBtn.Checked.Add(fun _ ->
            styleBtn aBtn sky true
            setActive A)

        bBtn.Checked.Add(fun _ ->
            styleBtn bBtn amber true
            setActive B)

        aBtn.Unchecked.Add(fun _ -> styleBtn aBtn sky false)
        bBtn.Unchecked.Add(fun _ -> styleBtn bBtn amber false)
        selector.RangeChanged.Add(fun _ -> refreshDebug ())
        selector.ActiveBrushChanged.Add(fun b -> setActive b)
        aBtn.Template <- toggleTemplate
        bBtn.Template <- toggleTemplate
        // initial states: A active, both styled
        styleBtn aBtn sky true
        styleBtn bBtn amber false

        // layout
        let root = DockPanel()

        let topRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(8.0, 10.0, 8.0, 0.0))

        aBtn.Margin <- Thickness(0.0, 0.0, 8.0, 0.0)
        bBtn.Margin <- Thickness(0.0, 0.0, 12.0, 0.0)
        topRow.Children.Add aBtn |> ignore
        topRow.Children.Add bBtn |> ignore

        let hint =
            TextBlock(
                Text =
                    "drag on the tape to select a range for the active brush \u00B7 click = point range \u00B7 A/B keys switch brush",
                Foreground = dim,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center
            )

        topRow.Children.Add hint |> ignore
        DockPanel.SetDock(topRow, Dock.Top)
        DockPanel.SetDock(debugText, Dock.Top)
        DockPanel.SetDock(lenLabel, Dock.Bottom)
        root.Children.Add topRow |> ignore
        root.Children.Add debugText |> ignore
        root.Children.Add lenLabel |> ignore
        root.Children.Add selector |> ignore
        self.Content <- root

        // live length refresh from the recording
        let refreshTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 300.0)

        refreshTimer.Tick.Add(fun _ ->
            let n = max 1L (getFrames ())

            if n <> selector.Length then
                selector.Length <- n
                refreshLen ())

        refreshTimer.Start()

        self.Title <- "Timeline selector (WPF prototype)"
        self.Width <- 900.0
        self.Height <- 250.0
        self.Background <- bg
        refreshDebug ()
        refreshLen ()
        self.Closed.Add(fun _ -> refreshTimer.Stop())
