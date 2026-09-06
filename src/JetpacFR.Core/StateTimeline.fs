namespace JetpacFR.Core

open System
open System.IO
open System.Text

/// The per-frame full-state timeline: after every completed frame the whole
/// machine state (64K memory + register/timing text + keyboard matrix) is
/// kept, so any recorded frame - past or future - can be restored instantly
/// without executing anything. Uncompressed in memory (~66 KB per frame,
/// ~3.3 MB/s at 50 fps); the file codec reserves a flags bit for the later
/// delta-compressed mode.
///
/// The state indexed by frame N is the machine state *after* frame N
/// completed. Frames run from `startFrame` (0 for a recording loaded or
/// captured from boot; the current frame after ResetTimeline) and captures
/// must be sequential, mirroring FrameHistory.Capture.
type StateTimeline(startFrame: int) =

  let memories = ResizeArray<byte[]>()
  let texts = ResizeArray<string>()
  let keys = ResizeArray<byte[]>()
  let mutable bytesUsed = 0L

  let frameBytes (text: string) =
    int64 (0x10000 + 8 + 2 + Encoding.UTF8.GetByteCount text)

  member _.StartFrame = startFrame
  member _.Count = memories.Count

  /// The last frame with a stored state; startFrame - 1 when empty.
  member _.EndFrame = startFrame + memories.Count - 1

  member _.CoversFrame(frame: int) = frame >= startFrame && frame < startFrame + memories.Count

  /// Bytes the timeline would occupy on disk in the current (uncompressed)
  /// format: the number the recording stats display reports.
  member _.BytesUsed = bytesUsed

  /// Record the state after `frame` completed. `memory` and `keys` are
  /// copied (they are the live machine images); `stateText` is immutable.
  member _.Capture(frame: int, memory: byte[], stateText: string, keyBytes: byte[]) =
    if frame <> startFrame + memories.Count then
      invalidOp (sprintf "timeline frames must be sequential: expected %d, got %d" (startFrame + memories.Count) frame)
    memories.Add(Array.copy memory)
    texts.Add stateText
    keys.Add(Array.copy keyBytes)
    bytesUsed <- bytesUsed + frameBytes stateText

  /// Restore the memory image at `frame` into `memory`; returns the state
  /// text and a copy of the keyboard matrix captured at that exact frame.
  member this.RestoreInto(frame: int, memory: byte[]) : string * byte[] =
    if not (this.CoversFrame frame) then
      invalidOp (sprintf "no timeline state at frame %d (covers %d..%d)" frame startFrame (startFrame + memories.Count - 1))
    let idx = frame - startFrame
    Array.blit memories.[idx] 0 memory 0 0x10000
    texts.[idx], Array.copy keys.[idx]

  /// Drop every state after `frame` (the "Go" branch point); new captures
  /// continue sequentially from frame + 1.
  member _.Truncate(frame: int) =
    let keep = frame - startFrame + 1
    if keep < memories.Count then
      let firstDropped = max 0 keep
      for i in firstDropped .. memories.Count - 1 do
        bytesUsed <- bytesUsed - frameBytes texts.[i]
      let removed = memories.Count - firstDropped
      if removed > 0 then
        memories.RemoveRange(firstDropped, removed)
        texts.RemoveRange(firstDropped, removed)
        keys.RemoveRange(firstDropped, removed)

  /// (state text, keyboard bytes, memory image) at storage index `idx` for
  /// the codec. The arrays are the stored ones - read-only by convention.
  member _.FrameData(idx: int) : string * byte[] * byte[] =
    if idx < 0 || idx >= memories.Count then
      invalidArg (nameof idx) (sprintf "no timeline state at index %d" idx)
    texts.[idx], keys.[idx], memories.[idx]

/// Identity a timeline file is pinned to: a recording is never loaded
/// against a different game or ROM/tape/program image. (Mirrors
/// ReplayFingerprint plus the game id, kept local to avoid a compile-order
/// dependency on Replay.fs.)
type TimelineFingerprint =
  { GameId: string
    RomSha256: string
    TzxSha256: string
    ProgramSha256: string
    ProgramAddress: int option }

type TimelineLoad =
  | TimelineMissing
  | TimelineLoaded of StateTimeline * KeyEvent list
  | TimelineIgnored of string

/// Binary codec for the per-frame timeline ("JPST"). Uncompressed layout:
///   magic "JPST" | version u16 | flags u16 (bit 0 = compressed, future)
///   gameId / romSha / tzxSha / programSha as u8-length-prefixed strings
///   programAddress: tag u8 (0 = none) + i32
///   keyEventCount u32 + events (frame i32, row i32, bit i32, pressed u8)
///   startFrame i32 (the first stored frame; 0 for recordings from boot)
///   frameCount u32 + per frame: textLen u16 + UTF8 text, keys[8], memory[65536]
///
/// The per-frame block is the compression seam: the later compressed mode
/// sets flags bit 0 and writes each frame's memory as a delta from the
/// previous frame followed by a simple coder; the loader dispatches on the
/// flag and old files stay readable.
module StateTimelineStore =

  let Version = 1us
  let FlagsCompressed = 1us
  let FileName = "timeline.jst"

  let private magic = [| byte 'J'; byte 'P'; byte 'S'; byte 'T' |]

  let private writeLenString (w: BinaryWriter) (value: string) =
    let bytes = Encoding.UTF8.GetBytes(if isNull value then "" else value)
    if bytes.Length > 255 then invalidOp "timeline header string too long"
    w.Write (byte bytes.Length)
    w.Write bytes

  let private readLenString (r: BinaryReader) =
    let len = int (r.ReadByte())
    Encoding.UTF8.GetString(r.ReadBytes len)

  /// Write the whole timeline to an already-opened file (closed by the
  /// caller's `use` before the atomic rename happens).
  let private writeStream (w: BinaryWriter) (fp: TimelineFingerprint) (timeline: StateTimeline) (events: KeyEvent seq) =
    w.Write magic
    w.Write Version
    w.Write 0us // flags: uncompressed
    writeLenString w fp.GameId
    writeLenString w fp.RomSha256
    writeLenString w fp.TzxSha256
    writeLenString w fp.ProgramSha256
    match fp.ProgramAddress with
    | Some address -> w.Write 1uy; w.Write address
    | None -> w.Write 0uy
    let eventArray = Array.ofSeq events
    w.Write (uint32 eventArray.Length)
    for e in eventArray do
      w.Write e.Frame
      w.Write e.Row
      w.Write e.Bit
      w.Write e.Pressed
    w.Write timeline.StartFrame
    w.Write (uint32 timeline.Count)
    for i in 0 .. timeline.Count - 1 do
      let text, keyBytes, memory = timeline.FrameData i
      let textBytes = Encoding.UTF8.GetBytes text
      if textBytes.Length > 0xFFFF then invalidOp "state text too long for the timeline format"
      w.Write (uint16 textBytes.Length)
      w.Write textBytes
      w.Write keyBytes
      w.Write memory
    w.Flush()

  let save (path: string) (fp: TimelineFingerprint) (timeline: StateTimeline) (events: KeyEvent seq) : Result<unit, string> =
    try
      let directory = Path.GetDirectoryName path
      if not (String.IsNullOrEmpty directory) then Directory.CreateDirectory directory |> ignore
      let temporary = path + ".tmp"
      do
        use fs = File.Create temporary
        use w = new BinaryWriter(fs)
        writeStream w fp timeline events
      File.Move(temporary, path, true)
      Ok ()
    with ex ->
      try File.Delete(path + ".tmp") with _ -> ()
      Error ex.Message

  let tryLoad (path: string) (fp: TimelineFingerprint) : TimelineLoad =
    if not (File.Exists path) then TimelineMissing
    else
      try
        use fs = File.OpenRead path
        use r = new BinaryReader(fs)
        if r.ReadBytes 4 <> magic then TimelineIgnored "not a Jetpac timeline file"
        else
          let version = r.ReadUInt16()
          if version <> Version then TimelineIgnored(sprintf "unsupported timeline version %d" version)
          else
            let flags = r.ReadUInt16()
            if flags &&& FlagsCompressed <> 0us then TimelineIgnored "compressed timelines are not supported yet"
            else
              let storedGameId = readLenString r
              let storedRom = readLenString r
              let storedTzx = readLenString r
              let storedProgram = readLenString r
              if storedGameId <> fp.GameId then TimelineIgnored "timeline belongs to another game"
              elif storedRom <> fp.RomSha256 || storedTzx <> fp.TzxSha256 || storedProgram <> fp.ProgramSha256 then
                TimelineIgnored "timeline assets do not match the selected game"
              else
                let programAddress =
                  match r.ReadByte() with
                  | 1uy -> Some (r.ReadInt32())
                  | _ -> None
                if programAddress <> fp.ProgramAddress then
                  TimelineIgnored "timeline assets do not match the selected game"
                else
                  let eventCount = int (r.ReadUInt32())
                  let events = ResizeArray<KeyEvent> eventCount
                  let mutable valid = true
                  let mutable i = 0
                  while valid && i < eventCount do
                    let frame = r.ReadInt32()
                    let row = r.ReadInt32()
                    let bit = r.ReadInt32()
                    let pressed = r.ReadByte() <> 0uy
                    if frame < 0 || row < 0 || row >= 8 || bit < 0 || bit >= 5 then valid <- false
                    else events.Add { Frame = frame; Row = row; Bit = bit; Pressed = pressed }
                    i <- i + 1
                  if not valid then TimelineIgnored "timeline contains an invalid key event"
                  else
                    let startFrame = r.ReadInt32()
                    if startFrame < 0 then TimelineIgnored "timeline has an invalid start frame"
                    else
                      let frameCount = int (r.ReadUInt32())
                      let timeline = StateTimeline startFrame
                      let mutable error = None
                      let mutable f = startFrame
                      while error.IsNone && f < startFrame + frameCount do
                        let textLen = int (r.ReadUInt16())
                        let text = Encoding.UTF8.GetString(r.ReadBytes textLen)
                        let keyBytes = r.ReadBytes 8
                        let memory = r.ReadBytes 0x10000
                        if keyBytes.Length <> 8 || memory.Length <> 0x10000 then
                          error <- Some "timeline file is truncated"
                        else
                          timeline.Capture(f, memory, text, keyBytes)
                          f <- f + 1
                      match error with
                      | Some message -> TimelineIgnored message
                      | None -> TimelineLoaded (timeline, Seq.toList events)
      with ex -> TimelineIgnored ex.Message
