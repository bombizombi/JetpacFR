namespace JetpacFR.Desktop

open System
open System.Globalization
open System.Windows
open System.Windows.Input
open System.Windows.Media
open JetpacFR.Core

/// Full-height vertical map of the address space driven by the in-memory
/// ControlFile (see plan_ctrl_map). Renders block kinds color-coded,
/// execution heat blended underneath, comment/self-mod ticks, and zooms
/// from the whole 64K span down to instruction-level snippets.
///
/// The control is dumb: it owns only view state (zoom/pan) and paints
/// whatever CtrlMapModel produces from the data properties set by the
/// host. Navigation and editing behavior live in MainWindow via the
/// AddressClicked / MenuRequested events.
type ControlMap() as self =
  inherit FrameworkElement()

  let addressClicked = Event<int>()
  let menuRequested = Event<int>()

  // ---- palette -----------------------------------------------------------
  let unmappedCol = Color.FromRgb(0x0Auy, 0x0Auy, 0x10uy)
  let codeCol = Color.FromRgb(0x2Euy, 0x7Duy, 0xD1uy)
  let dataCol = Color.FromRgb(0xC8uy, 0x8Auy, 0x2Euy)
  let gapCol = Color.FromRgb(0x3Auy, 0x3Fuy, 0x4Cuy)
  let mixedCol = Color.FromRgb(0x55uy, 0x51uy, 0x6Buy)
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
  let selfModPen = Pen(SolidColorBrush(Color.FromRgb(0xF0uy, 0x30uy, 0xF0uy)), 2.0)
  let commentPen = Pen(SolidColorBrush(Color.FromRgb(0x4Euy, 0xE0uy, 0x60uy)), 2.0)
  let rangeBrush = SolidColorBrush(Color.FromArgb(0x30uy, 0x4Euy, 0xE0uy, 0x60uy))
  let cursorPen = Pen(SolidColorBrush(Color.FromRgb(0x4Euy, 0xD0uy, 0xE0uy)), 1.0)
  let thumbPen = Pen(SolidColorBrush(Color.FromArgb(0x80uy, 0xFFuy, 0xFFuy, 0xFFuy)), 1.0)
  let labelFg = SolidColorBrush(Color.FromRgb(0xE8uy, 0xE8uy, 0xECuy))
  let snipFg = SolidColorBrush(Color.FromRgb(0xC8uy, 0xC8uy, 0xCEuy))
  let cmtFg = SolidColorBrush(Color.FromRgb(0x4Euy, 0xE0uy, 0x60uy))
  let rulerFg = SolidColorBrush(Color.FromRgb(0x6Auy, 0x6Auy, 0x74uy))
  let typeface =
    Typeface(FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal)

  let blend (a: Color) (b: Color) (t: float) =
    let t = max 0.0 (min 1.0 t)
    let mix (x: byte) (y: byte) = byte (Math.Round(float x + (float y - float x) * t))
    Color.FromRgb(mix a.R b.R, mix a.G b.G, mix a.B b.B)

  /// Row-fill color: block kind base with heat blended on top.
  let rowColor (kind: CtrlMapModel.RowKind) (heat: int) =
    let baseC =
      match kind with
      | CtrlMapModel.CodeR -> codeCol
      | CtrlMapModel.DataR -> dataCol
      | CtrlMapModel.GapR -> gapCol
      | CtrlMapModel.MixedR -> mixedCol
      | CtrlMapModel.UnmappedR -> unmappedCol
    if heat <= 0 then baseC
    else blend baseC heatLut[min 9 heat] (0.15 + 0.65 * float heat / 9.0)

  // ---- host-supplied data -------------------------------------------------
  let mutable controlData: ControlFile option = None
  let mutable execCounts = Array.empty<int>
  let mutable selfModFlags = Array.empty<bool>
  let mutable instrStarts: bool[] option = None
  let mutable memImage: byte[] option = None
  let mutable cursorAddr = -1
  let mutable disasmLo = -1
  let mutable disasmHi = -1

  // ---- view state ---------------------------------------------------------
  let mutable viewStart = 0
  let mutable viewEnd = 0x10000
  let mutable scrubbing = false
  let mutable panning = false
  let mutable panStartY = 0.0
  let mutable panStartView = 0

  let minSpan () = max 32 (int (self.ActualHeight / 32.0))

  let clampView () =
    let span = max (minSpan ()) (viewEnd - viewStart)
    viewStart <- max 0 (min (0x10000 - span) viewStart)
    viewEnd <- viewStart + span

  let clampF (lo: float) (hi: float) (v: float) = max lo (min hi v)

  let addrAt (y: float) : int =
    let h = max 1.0 self.ActualHeight
    viewStart + int (clampF 0.0 (float (viewEnd - viewStart - 1)) (y / h * float (viewEnd - viewStart)))

  do
    self.Focusable <- true
    self.Cursor <- Cursors.Cross

  [<CLIEvent>]
  member _.AddressClicked = addressClicked.Publish

  [<CLIEvent>]
  member _.MenuRequested = menuRequested.Publish

  member _.ControlData
    with get () = controlData
    and set v =
      controlData <- v
      self.InvalidateVisual()

  member _.ExecCounts
    with get () = execCounts
    and set v =
      execCounts <- v
      self.InvalidateVisual()

  member _.SelfModified
    with get () = selfModFlags
    and set v =
      selfModFlags <- v
      self.InvalidateVisual()

  member _.InstrStarts
    with get () = instrStarts
    and set v =
      instrStarts <- v
      self.InvalidateVisual()

  member _.MemoryImage
    with get () = memImage
    and set v =
      memImage <- v
      self.InvalidateVisual()

  /// Highlighted address (-1 hides the hairline).
  member _.CursorAddress
    with get () = cursorAddr
    and set v =
      cursorAddr <- v
      self.InvalidateVisual()

  /// Disassembly viewport extent (-1,-1 hides the thumb).
  member _.DisasmViewport
    with get () = (disasmLo, disasmHi)
    and set (lo, hi) =
      disasmLo <- lo
      disasmHi <- hi
      self.InvalidateVisual()

  /// Currently visible address range.
  member _.ViewRange = (viewStart, viewEnd)

  /// Center the viewport on an address (auto-follow while running).
  member this.CenterOn(addr: int) =
    let span = viewEnd - viewStart
    viewStart <- addr - span / 2
    clampView ()
    self.InvalidateVisual()

  /// True when addr is outside the visible range.
  member this.IsOutside(addr: int) = addr < viewStart || addr >= viewEnd

  member private this.Model () : CtrlMapModel.MapRender =
    let rowCount = max 1 (int (Math.Ceiling self.ActualHeight))
    match controlData with
    | Some c ->
      CtrlMapModel.render c.Blocks c.Comments instrStarts memImage execCounts selfModFlags
        viewStart viewEnd rowCount
    | None ->
      { ViewStart = viewStart; ViewEnd = viewEnd
        Rows = Array.empty; Labels = Array.empty; Ranges = Array.empty; Snippets = Array.empty }

  override this.OnRender(dc: DrawingContext) =
    let w = self.ActualWidth
    let h = self.ActualHeight
    if w <= 0.0 || h <= 0.0 then ()
    else
      dc.DrawRectangle(SolidColorBrush(unmappedCol), null, Rect(0.0, 0.0, w, h))
      let m = this.Model ()
      let dpi = VisualTreeHelper.GetDpi this
      let ft (text: string) (size: float) (fg: Brush) =
        let t = FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                              typeface, size, fg, dpi.PixelsPerDip)
        t.Trimming <- TextTrimming.CharacterEllipsis
        t
      if m.Rows.Length > 0 then
        let rowH = h / float m.Rows.Length
        // Coalesce runs of identical colors into one rect per run.
        let mutable y = 0
        while y < m.Rows.Length do
          let r = m.Rows[y]
          let col = rowColor r.Kind r.Heat
          let mutable y2 = y + 1
          while y2 < m.Rows.Length do
            let n = m.Rows[y2]
            if n.Kind = r.Kind && n.Heat = r.Heat then y2 <- y2 + 1 else y2 <- m.Rows.Length
          let brush = SolidColorBrush(col)
          brush.Freeze()
          dc.DrawRectangle(brush, null, Rect(0.0, float y * rowH, w, float (y2 - y) * rowH + 0.5))
          // left-edge ticks: self-mod magenta at x0, comment green at x2
          for yy in y .. y2 - 1 do
            let ry = float yy * rowH
            if m.Rows[yy].SelfMod then dc.DrawLine(selfModPen, Point(0.0, ry), Point(2.0, ry))
            if m.Rows[yy].Comment then dc.DrawLine(commentPen, Point(2.0, ry), Point(4.0, ry))
          y <- y2

        // range comments as translucent bands
        for (a, b) in m.Ranges do
          let y0 = CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length a
          let y1 = CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length b
          dc.DrawRectangle(rangeBrush, null, Rect(0.0, y0 * rowH, w, (y1 - y0) * rowH))
        // block-name labels where the band is tall enough
        for lbl in m.Labels do
          let y = CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length lbl.Addr
          dc.DrawText(ft lbl.Text 9.0 labelFg, Point(5.0, y * rowH))

        // instruction-level snippets
        if m.Snippets.Length > 0 then
          let size = min 11.0 (rowH * 0.72)
          for s in m.Snippets do
            let y = CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length s.Addr
            let line = sprintf "%04X  %-11s" s.Addr s.Bytes
            dc.DrawText(ft line size snipFg, Point(5.0, y * rowH))
            dc.DrawText(ft s.Text size snipFg, Point(58.0, y * rowH))
            match s.Comment with
            | Some c when c.Length > 0 -> dc.DrawText(ft ("; " + c) size cmtFg, Point(w * 0.62, y * rowH))
            | _ -> ()

      // disassembly viewport thumb
      if disasmLo >= 0 && disasmHi > disasmLo then
        let y0 = CtrlMapModel.addrY viewStart viewEnd (max 1 (int h)) disasmLo
        let y1 = CtrlMapModel.addrY viewStart viewEnd (max 1 (int h)) disasmHi
        dc.DrawRectangle(null, thumbPen, Rect(1.0, y0, w - 2.0, max 3.0 (y1 - y0)))

      // cursor hairline
      if cursorAddr >= 0 && cursorAddr >= viewStart && cursorAddr < viewEnd then
        let y = CtrlMapModel.addrY viewStart viewEnd (max 1 (int h)) cursorAddr
        dc.DrawLine(cursorPen, Point(0.0, y), Point(w, y))

      // faint $-ruler when zoomed out far enough that labels don't collide
      if (viewEnd - viewStart) > 0x2000 then
        for a in 0 .. 0x40 .. 0xFFFF - 1 do
          if a % 0x4000 = 0 then
            let y = CtrlMapModel.addrY viewStart viewEnd (max 1 (int h)) a
            dc.DrawText(ft (sprintf "%04X" a) 8.0 rulerFg, Point(w - 26.0, y + 1.0))

      // live view-range readout (dev aid: confirms zoom/pan took effect)
      dc.DrawText(ft (sprintf "%04X-%04X" viewStart viewEnd) 8.0 rulerFg, Point(2.0, 1.0))

  override this.OnMouseWheel(e: MouseWheelEventArgs) =
    let delta = float e.Delta
    if Keyboard.Modifiers &&& ModifierKeys.Shift = ModifierKeys.Shift then
      // pan
      let span = viewEnd - viewStart
      viewStart <- viewStart + int (-delta * float span / 600.0)
      viewEnd <- viewStart + span
      clampView ()
    else
      let pos = e.GetPosition this
      let frac = clampF 0.0 1.0 (pos.Y / max 1.0 self.ActualHeight)
      // zoom anchored under the cursor
      let oldSpan = viewEnd - viewStart
      let anchor = viewStart + int (frac * float oldSpan)
      let factor = Math.Pow(1.15, delta / 120.0)
      let newSpan = max (minSpan ()) (min 0x10000 (int (float oldSpan / factor)))
      viewStart <- anchor - int (frac * float newSpan)
      viewEnd <- viewStart + newSpan
      clampView ()
    self.InvalidateVisual()
    e.Handled <- true

  override this.OnMouseDown(e: MouseButtonEventArgs) =
    self.Focus() |> ignore
    let pos = e.GetPosition this
    if e.ChangedButton = MouseButton.Middle then
      panning <- true
      panStartY <- pos.Y
      panStartView <- viewStart
      self.CaptureMouse() |> ignore
      e.Handled <- true
    elif e.RightButton = MouseButtonState.Pressed then
      menuRequested.Trigger(addrAt pos.Y)
    elif e.LeftButton = MouseButtonState.Pressed then
      scrubbing <- true
      self.CaptureMouse() |> ignore
      addressClicked.Trigger(addrAt pos.Y)
    base.OnMouseDown e

  override this.OnMouseMove(e: MouseEventArgs) =
    let pos = e.GetPosition this
    if scrubbing then addressClicked.Trigger(addrAt pos.Y)
    elif panning then
      let dy = pos.Y - panStartY
      let span = viewEnd - viewStart
      viewStart <- panStartView - int (dy * float span / max 1.0 self.ActualHeight)
      viewEnd <- viewStart + span
      clampView ()
      self.InvalidateVisual()
    else
      let a = addrAt pos.Y
      let blockTxt =
        match controlData with
        | Some c ->
          ControlFile.blockAt c a
          |> Option.map (fun b -> sprintf " %s (%s)" b.Name (string b.Kind))
          |> Option.defaultValue " <unmapped>"
        | None -> ""
      let count = if a < execCounts.Length then execCounts[a] else 0
      self.ToolTip <- sprintf "0x%04X%s  (%d executions)" a blockTxt count
    base.OnMouseMove e

  override this.OnMouseUp(e: MouseButtonEventArgs) =
    if e.ChangedButton = MouseButton.Middle && panning then
      panning <- false
      self.ReleaseMouseCapture()
      e.Handled <- true
    elif e.ChangedButton = MouseButton.Left && scrubbing then
      scrubbing <- false
      self.ReleaseMouseCapture()
    base.OnMouseUp e

  override this.OnLostMouseCapture(e: MouseEventArgs) =
    scrubbing <- false
    panning <- false
    base.OnLostMouseCapture e
