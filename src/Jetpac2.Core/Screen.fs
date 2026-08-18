namespace Jetpac2.Core

/// Port of JetpacFSharp (Jetpac.Core) VideoConstants — specbolt ULA constants.
module VideoConstants =
  let XBorder = 32
  let YBorder = 32
  let ScreenWidth = 256
  let ScreenHeight = 192
  let VisibleWidth = 320
  let VisibleHeight = 256
  let ColumnCount = 32
  let CyclesPerScanLine = 224
  let PalTotalLines = 312
  let VSyncLines = 56
  let FramesPerFlash = 16
  let AttributeDataOffset = 0x1800

  /// 16 ARGB colours: normal + bright variants.
  let Palette =
    [| 0xFF000000; 0xFF0000CD; 0xFFCD0000; 0xFFCD00CD; 0xFF00CD00; 0xFF00CDCD; 0xFFCDCD00; 0xFFCDCDCD
       0xFF000000; 0xFF0000FF; 0xFFFF0000; 0xFFFF00FF; 0xFF00FF00; 0xFF00FFFF; 0xFFFFFF00; 0xFFFFFFFF |]

/// One rendered visible line: border colour plus 32 pixel/attribute columns.
type private VideoLine =
  { mutable Border: int
    mutable Pixels: int[]
    mutable Attrs: int[] }

/// Port of JetpacFSharp (Jetpac.Core) Video — the scanline renderer, reading
/// the game's flat 64K memory (page 1 at 0x4000) instead of a paged Memory.
/// The line buffer is snapshotted as each scanline passes, exactly like the
/// ULA, so mid-frame writes produce the same tearing the emulator shows.
type VideoScreen(memory: byte[]) =
  let mutable border = 3
  let mutable currentLine = 0
  let mutable flashCounter = 0
  let mutable flashOn = false
  let lines =
    Array.init VideoConstants.VisibleHeight (fun _ ->
      { Border = 3
        Pixels = Array.zeroCreate VideoConstants.ColumnCount
        Attrs = Array.zeroCreate VideoConstants.ColumnCount })
  let screenBuffer = Array.zeroCreate<byte> (VideoConstants.VisibleWidth * VideoConstants.VisibleHeight * 4)

  member this.SetBorder(v: int) = border <- v &&& 7

  /// Restore the scanline/flash phase captured in the fixture.
  member this.SetState(scanline: int, flashCounter_: int, flashOn_: bool) =
    currentLine <- scanline % VideoConstants.PalTotalLines
    flashCounter <- flashCounter_
    flashOn <- flashOn_


  /// Re-render every visible line from current memory (rewind present):
  /// `lines` holds scanline-pass snapshots, so after a state restore the
  /// blit buffer would otherwise show the pre-restore frame until a new
  /// frame executes. Border-only lines keep their captured border color.
  member this.RenderAll() =
    for y in 0 .. VideoConstants.VisibleHeight - 1 do
      this.RenderLine(y + VideoConstants.VSyncLines)

  /// Advance one scanline; returns true when the 312-line counter wraps
  /// (the interrupt point).
  member this.NextScanLine() : bool =
    this.RenderLine currentLine
    currentLine <- (currentLine + 1) % VideoConstants.PalTotalLines
    if currentLine = 0 then
      flashCounter <- flashCounter + 1
      if flashCounter = VideoConstants.FramesPerFlash then
        flashCounter <- 0
        flashOn <- not flashOn
      true
    else
      false

  member private this.RenderLine(displayLine: int) =
    if displayLine < VideoConstants.VSyncLines then
      ()
    else
      let line = lines.[displayLine - VideoConstants.VSyncLines]
      line.Border <- border
      let y = displayLine - VideoConstants.YBorder - VideoConstants.VSyncLines
      if y >= VideoConstants.ScreenHeight || y < 0 then
        () // border-only line
      else
        let y76 = (y >>> 6) &&& 3
        let y543 = (y >>> 3) &&& 7
        let y210 = y &&& 7
        let screenOffset = (y76 <<< 11) + (y543 <<< 5) + (y210 <<< 8)
        let charRow = y / 8
        for x in 0 .. VideoConstants.ColumnCount - 1 do
          line.Pixels.[x] <- int memory.[0x4000 + screenOffset + x]
          line.Attrs.[x] <- int memory.[0x5800 + charRow * VideoConstants.ColumnCount + x]

  /// Fill the reused screen buffer (B,G,R,A byte order) and return it.
  member this.BlitTo() : byte[] =
    let pal = VideoConstants.Palette
    let buf = screenBuffer
    for y in 0 .. VideoConstants.VisibleHeight - 1 do
      let line = lines.[y]
      let fillColor (color: int) (startX: int) (count: int) =
        let mutable i = (y * VideoConstants.VisibleWidth + startX) * 4
        let b = color &&& 0xFF
        let g = (color >>> 8) &&& 0xFF
        let r = (color >>> 16) &&& 0xFF
        let a = (color >>> 24) &&& 0xFF
        for _ in 0 .. count - 1 do
          buf.[i] <- byte b
          buf.[i + 1] <- byte g
          buf.[i + 2] <- byte r
          buf.[i + 3] <- byte a
          i <- i + 4
      if y < VideoConstants.YBorder || y >= VideoConstants.YBorder + VideoConstants.ScreenHeight then
        fillColor pal.[line.Border] 0 VideoConstants.VisibleWidth
      else
        fillColor pal.[line.Border] 0 VideoConstants.XBorder
        fillColor pal.[line.Border] (VideoConstants.XBorder + VideoConstants.ScreenWidth) VideoConstants.XBorder
        for x in 0 .. VideoConstants.ColumnCount - 1 do
          let pixel = line.Pixels.[x]
          let attr = line.Attrs.[x]
          let invert = attr &&& 0x80 <> 0 && flashOn
          let brightness = if attr &&& 0x40 <> 0 then 8 else 0
          let index1 = ((attr >>> 3) &&& 7) + brightness
          let index2 = (attr &&& 7) + brightness
          let paperColor = if invert then pal.[index1] else pal.[index2]
          let penColor = if invert then pal.[index2] else pal.[index1]
          let mutable i = (y * VideoConstants.VisibleWidth + VideoConstants.XBorder + x * 8) * 4
          for bit in 0 .. 7 do
            let color = if pixel &&& (1 <<< (7 - bit)) <> 0 then paperColor else penColor
            buf.[i] <- byte (color &&& 0xFF)
            buf.[i + 1] <- byte ((color >>> 8) &&& 0xFF)
            buf.[i + 2] <- byte ((color >>> 16) &&& 0xFF)
            buf.[i + 3] <- byte ((color >>> 24) &&& 0xFF)
            i <- i + 4
    buf
