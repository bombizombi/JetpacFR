namespace JetpacFR.Core

open System
open System.IO
open System.IO.Compression
open System.Text
open System.Globalization

/// Capture cadence for the sparse timeline: full 64K states are stored for
/// every frame of a configurable dense head (the first `DenseSeconds` of the
/// recording), then as a PAIR of states at every whole second afterwards -
/// the second-boundary frame and the frame right after it. Gaps therefore
/// alternate long (period - 2 frames) and short (1-frame), so the worst
/// materialization run is half a long gap and the short state gives an
/// immediate neighbor for single-frame stepping. Summaries are captured for
/// every frame regardless of this rule.
module TimelineCadence =

    type Cadence = { DenseSeconds: int; SparseEverySeconds: int; Fps: int }

    /// 10 s dense head, then a state pair every second. The defaults surface
    /// as GuiConfig tunables in the Desktop shell; Core tests use tighter ones.
    let deflt = { DenseSeconds = 10; SparseEverySeconds = 1; Fps = 50 }

    let denseFrames (c: Cadence) = c.DenseSeconds * c.Fps

    let sparsePeriod (c: Cadence) = max 1 (c.SparseEverySeconds * c.Fps)

    /// Store a full state at `frame`? `startFrame` is the recording's first
    /// frame (the anchor; always stored).
    let shouldCapture (c: Cadence) (startFrame: int) (frame: int) =
        let rel = frame - startFrame

        if rel <= 0 then true
        elif rel < denseFrames c then true
        else
            let r = rel % sparsePeriod c
            r = 0 || r = 1

/// The per-frame state timeline, SPARSE since the cadence change: full 64K
/// machine states (memory + register/timing text + keyboard matrix) are kept
/// only at cadence-selected frames, while a dense stream of tiny per-frame
/// FrameSummaries covers every recorded frame for the flame graph. Stored
/// frames are explicit-numbered and strictly increasing, not contiguous; gap
/// frames are re-derived on demand by TimelineMaterializer (nearest stored
/// state + forward emulation, proven byte-exact by the parity tests).
///
/// Frames run from `startFrame` (0 for a recording loaded or captured from
/// boot; the current frame after ResetTimeline). Summaries are appended
/// sequentially per completed frame; states land per the cadence.
type StateTimeline(startFrame: int) =

    let frames = ResizeArray<int>()
    let memories = ResizeArray<byte[]>()
    let texts = ResizeArray<string>()
    let keys = ResizeArray<byte[]>()
    let summaryFrames = ResizeArray<int>()
    let summaries = ResizeArray<FrameSummary>()
    let mutable bytesUsed = 0L

    let frameBytes (text: string) =
        int64 (0x10000 + 8 + 2 + Encoding.UTF8.GetByteCount text)

    member _.StartFrame = startFrame

    /// Number of STORED full states (sparse - not the recording's frame span).
    member _.Count = memories.Count

    /// Number of frames the recording covers (the dense summary stream; the
    /// frame span the flame graph and the seek extent speak in).
    member _.FrameCount = summaryFrames.Count

    /// The last frame with recorded content (summary or state) - the
    /// recording's seekable horizon. startFrame - 1 when empty.
    member _.EndFrame =
        let lastSummary =
            if summaryFrames.Count = 0 then startFrame - 1 else summaryFrames[summaryFrames.Count - 1]

        let lastStored = if frames.Count = 0 then startFrame - 1 else frames[frames.Count - 1]
        max lastSummary lastStored

    /// True when an exact full state is stored at `frame` (no emulation).
    member _.HasState(frame: int) =
        frame >= startFrame
        && frames.Count > 0
        && frame >= frames[0]
        && frame <= frames[frames.Count - 1]
        && (let mutable found = false
            let mutable lo = 0
            let mutable hi = frames.Count - 1

            while not found && lo <= hi do
                let mid = (lo + hi) >>> 1

                if frames[mid] = frame then found <- true
                elif frames[mid] < frame then lo <- mid + 1
                else hi <- mid - 1

            found)

    /// The latest stored frame <= `frame` (the materializer's anchor), if any.
    member _.NearestStateAtOrBefore(frame: int) : int option =
        if frames.Count = 0 || frame < frames[0] then
            None
        else
            let mutable lo = 0
            let mutable hi = frames.Count - 1

            while lo < hi do
                let mid = (lo + hi + 1) >>> 1

                if frames[mid] <= frame then lo <- mid else hi <- mid - 1

            Some frames[lo]

    member this.CoversFrame(frame: int) = this.HasState frame

    /// Bytes the timeline would occupy on disk (stored states + summaries):
    /// the number the recording stats display reports.
    member _.BytesUsed = bytesUsed

    /// Record the full state after `frame` completed (cadence-selected).
    /// `memory` and `keys` are copied (they are the live machine images);
    /// `stateText` is immutable. Stored frames must strictly increase.
    member _.Capture(frame: int, memory: byte[], stateText: string, keyBytes: byte[]) =
        if frames.Count > 0 && frame <= frames[frames.Count - 1] then
            invalidOp (sprintf "timeline state frames must increase: got %d after %d" frame frames[frames.Count - 1])

        frames.Add frame
        memories.Add(Array.copy memory)
        texts.Add stateText
        keys.Add(Array.copy keyBytes)
        bytesUsed <- bytesUsed + frameBytes stateText

    /// Append the flame summary of the NEXT frame (live capture is dense and
    /// sequential; the V3 loader installs summaries in frame order too).
    member _.CaptureSummary(frame: int, summary: FrameSummary) =
        if summaryFrames.Count > 0 && frame <= summaryFrames[summaryFrames.Count - 1] then
            invalidOp (
                sprintf
                    "timeline summary frames must increase: got %d after %d"
                    frame
                    summaryFrames[summaryFrames.Count - 1]
            )

        summaryFrames.Add frame
        summaries.Add summary
        bytesUsed <- bytesUsed + FrameSummary.byteSize summary

    /// Restore the stored memory image at `frame` into `memory`; returns the
    /// state text and a copy of the keyboard matrix captured at that exact
    /// frame. Gap frames are rejected - restore via TimelineMaterializer.
    member this.RestoreInto(frame: int, memory: byte[]) : string * byte[] =
        if not (this.HasState frame) then
            invalidOp (
                sprintf
                    "no stored timeline state at frame %d (stored %d states from %d, extent %d)"
                    frame
                    frames.Count
                    startFrame
                    this.EndFrame
            )

        let mutable lo = 0
        let mutable hi = frames.Count - 1

        while lo < hi do
            let mid = (lo + hi) >>> 1

            if frames[mid] < frame then lo <- mid + 1 else hi <- mid

        let idx = lo
        Array.blit memories[idx] 0 memory 0 0x10000
        texts[idx], Array.copy keys[idx]

    /// Drop everything after `frame` (the "Go" branch point): stored states
    /// and summaries alike; new captures continue from frame + 1.
    member _.Truncate(frame: int) =
        // Remove-from-tail helper shared by the two streams; returns the kept
        // count so the parallel arrays and the byte accounting stay in step.
        let dropAfter (arr: ResizeArray<'a>) (frameOf: 'a -> int) =
            let mutable keep = arr.Count

            while keep > 0 && frameOf arr[keep - 1] > frame do
                keep <- keep - 1

            if keep < arr.Count then arr.RemoveRange(keep, arr.Count - keep)

            keep

        if frames.Count > 0 then
            let keepStates = dropAfter frames id

            for i in keepStates .. memories.Count - 1 do
                bytesUsed <- bytesUsed - frameBytes texts[i]

            memories.RemoveRange(keepStates, memories.Count - keepStates)
            texts.RemoveRange(keepStates, texts.Count - keepStates)
            keys.RemoveRange(keepStates, keys.Count - keepStates)

        if summaryFrames.Count > 0 then
            let keepSummaries = dropAfter summaryFrames id

            for i in keepSummaries .. summaries.Count - 1 do
                bytesUsed <- bytesUsed - FrameSummary.byteSize summaries[i]

            summaries.RemoveRange(keepSummaries, summaries.Count - keepSummaries)

    /// (state text, keyboard bytes, memory image) at storage index `idx` for
    /// the codec. The arrays are the stored ones - read-only by convention.
    member _.FrameData(idx: int) : string * byte[] * byte[] =
        if idx < 0 || idx >= memories.Count then
            invalidArg (nameof idx) (sprintf "no timeline state at index %d" idx)

        texts[idx], keys[idx], memories[idx]

    /// Storage-index accessors for the codecs.
    member _.StoredCount = frames.Count
    member _.StoredFrameAt(idx: int) = frames[idx]
    member _.SummaryCount = summaries.Count
    member _.SummaryFrameAt(idx: int) = summaryFrames[idx]

    member _.SummaryData(idx: int) : FrameSummary =
        if idx < 0 || idx >= summaries.Count then
            invalidArg (nameof idx) (sprintf "no timeline summary at index %d" idx)

        summaries[idx]

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

/// Binary codec for the per-frame timeline. Three on-disk generations, one
/// header shape (magic | version u16 | flags u16, then gameId / romSha /
/// tzxSha / programSha as u8-length-prefixed strings, programAddress tag u8 +
/// i32, keyEventCount u32 + events (frame i32, row i32, bit i32, pressed u8),
/// startFrame i32):
///   V1 ("JPST", .jst): dense - per frame textLen u16 + UTF8 text, keys[8],
///     memory[65536] raw. Read-only legacy.
///   V2 ("JS2T", .js2): dense - per frame textLen + text raw, then
///     payloadLen u32 + DEFLATE(XOR stream vs the previous frame). Read-only
///     legacy.
///   V3 ("JS3T", .js3): the SPARSE cadence format and the only one written:
///     frameCount u32 (the recording's frame extent; the dense summary
///     stream covers every frame), summaryBlobLen u32 + DEFLATE(summaries:
///     per frame u8 levelCount, u16 instrCount, u8 endDepth, then
///     levelCount x (u16 entry, u32 coverage)), storedCount u32, then per
///     stored state its EXPLICIT frame number i32, textLen u16 + text, and
///     payloadLen u32 + DEFLATE(XOR stream - memory[65536] ++ keys[8] - vs
///     the PREVIOUS STORED state, zeros for the first).
/// Legacy files stay readable: dense states are the degenerate sparse case
/// (every frame stored) with no summaries - the flame graph falls back to
/// probe emulation for such recordings.
module StateTimelineStore =

    let Version = 1us
    let FlagsCompressed = 1us
    let FileName = "timeline.jst"
    /// Third-format sparse sibling: the only format the save path writes.
    let FileNameV3 = "timeline.js3"

    /// The .js3 sibling of a slot path (ChangeExtension keeps directory and
    /// stem, so every slot resolves with either generation present).
    let toV3Path (path: string) : string = Path.ChangeExtension(path, ".js3")
    /// The .js2 twin of a .jst path (legacy recordings may carry one).
    let toV2Path (path: string) : string = Path.ChangeExtension(path, ".js2")

    let private magic = [| byte 'J'; byte 'P'; byte 'S'; byte 'T' |]
    let private magic2 = [| byte 'J'; byte 'S'; byte '2'; byte 'T' |]
    let private magic3 = [| byte 'J'; byte 'S'; byte '3'; byte 'T' |]

    let private writeLenString (w: BinaryWriter) (value: string) =
        let bytes = Encoding.UTF8.GetBytes(if isNull value then "" else value)

        if bytes.Length > 255 then
            invalidOp "timeline header string too long"

        w.Write(byte bytes.Length)
        w.Write bytes

    let private readLenString (r: BinaryReader) =
        let len = int (r.ReadByte())
        Encoding.UTF8.GetString(r.ReadBytes len)

    /// DEFLATE at optimal level; the XOR-delta streams are ~all zeros, so the
    /// blobs run hundreds of bytes per stored state.
    let private deflate (payload: byte[]) : byte[] =
        use ms = new MemoryStream()
        use dz = new DeflateStream(ms, CompressionLevel.Optimal, true)
        dz.Write(payload, 0, payload.Length)
        dz.Close()
        ms.ToArray()

    /// Inflate exactly `expected` bytes or fail - truncation must not decode
    /// into a short, silently wrong stream.
    let private inflate (blob: byte[]) (expected: int) : byte[] =
        use ms = new MemoryStream(blob, false)
        use dz = new DeflateStream(ms, CompressionMode.Decompress)
        let out = Array.zeroCreate<byte> expected
        let mutable got = 0

        while got < expected do
            let n = dz.Read(out, got, expected - got)

            if n = 0 then
                invalidOp "timeline delta payload is corrupt"

            got <- got + n

        out

    /// Inflate a blob of unknown decoded length (the summary stream).
    let private inflateAll (blob: byte[]) : byte[] =
        use ms = new MemoryStream(blob, false)
        use dz = new DeflateStream(ms, CompressionMode.Decompress)
        use out = new MemoryStream()
        dz.CopyTo(out)
        out.ToArray()

    /// Fixed-width state stream: memory[65536] ++ keys[8].
    [<Literal>]
    let private streamLen = 0x10000 + 8

    let private xorInto (cur: byte[]) (prev: byte[]) =
        for i in 0 .. streamLen - 1 do
            cur[i] <- cur[i] ^^^ prev[i]

    let private writeText (w: BinaryWriter) (text: string) =
        let textBytes = Encoding.UTF8.GetBytes text

        if textBytes.Length > 0xFFFF then
            invalidOp "state text too long for the timeline format"

        w.Write(uint16 textBytes.Length)
        w.Write textBytes

    /// Serialize the per-frame summary stream (frame order assumed).
    let private encodeSummaries (summaries: FrameSummary[]) : byte[] =
        use ms = new MemoryStream()
        use w = new BinaryWriter(ms)

        for s in summaries do
            if s.Levels.Length > 255 then
                invalidOp "frame summary too deep for the timeline format"

            w.Write(byte s.Levels.Length)
            w.Write s.InstrCount
            w.Write s.EndDepth

            for lv in s.Levels do
                w.Write lv.Entry
                w.Write lv.Coverage

        w.Flush()
        ms.ToArray()

    let private decodeSummaries (bytes: byte[]) (count: int) : FrameSummary[] =
        use ms = new MemoryStream(bytes)
        use r = new BinaryReader(ms)

        Array.init count (fun _ ->
            let levelCount = int (r.ReadByte())
            let instrCount = r.ReadUInt16()
            let endDepth = r.ReadByte()

            let levels =
                Array.init levelCount (fun _ ->
                    { Entry = r.ReadUInt16()
                      Coverage = r.ReadUInt32() })

            { InstrCount = instrCount
              EndDepth = endDepth
              Levels = levels })

    /// V3 writer: same header as the legacy generations, then the dense
    /// summary blob and the sparse stored states with explicit frame numbers,
    /// each XOR-delta-deflated against the previous STORED state.
    let private writeStreamV3
        (w: BinaryWriter)
        (fp: TimelineFingerprint)
        (timeline: StateTimeline)
        (events: KeyEvent seq)
        =
        w.Write magic3
        w.Write Version
        w.Write FlagsCompressed
        writeLenString w fp.GameId
        writeLenString w fp.RomSha256
        writeLenString w fp.TzxSha256
        writeLenString w fp.ProgramSha256

        match fp.ProgramAddress with
        | Some address ->
            w.Write 1uy
            w.Write address
        | None -> w.Write 0uy

        let eventArray = Array.ofSeq events
        w.Write(uint32 eventArray.Length)

        for e in eventArray do
            w.Write e.Frame
            w.Write e.Row
            w.Write e.Bit
            w.Write e.Pressed

        w.Write timeline.StartFrame
        w.Write(uint32 timeline.FrameCount)

        let summaries = Array.init timeline.SummaryCount timeline.SummaryData
        let summaryBlob = deflate (encodeSummaries summaries)
        w.Write(uint32 summaryBlob.Length)
        w.Write summaryBlob

        w.Write(uint32 timeline.StoredCount)
        let cur = Array.zeroCreate<byte> streamLen
        let prev = Array.zeroCreate<byte> streamLen

        for i in 0 .. timeline.StoredCount - 1 do
            let frame = timeline.StoredFrameAt i
            let text, keyBytes, memory = timeline.FrameData i
            w.Write frame
            writeText w text
            Array.blit memory 0 cur 0 0x10000
            Array.blit keyBytes 0 cur 0x10000 8
            xorInto cur prev
            let blob = deflate cur
            w.Write(uint32 blob.Length)
            w.Write blob
            // Chain on the PLAIN stream: cur currently holds cur ^ prev, so
            // XOR once more to restore plain cur before it becomes prev.
            xorInto cur prev
            Array.blit cur 0 prev 0 streamLen

        w.Flush()

    /// Load one timeline file (any of the three magics); shared by tryLoad
    /// and the save path's decode-back verifier.
    let private loadOne (path: string) : Result<TimelineFingerprint * int * StateTimeline * KeyEvent list, string> =
        try
            use fs = File.OpenRead path
            use r = new BinaryReader(fs)
            let head = r.ReadBytes 4
            let isV3 = head = magic3
            let isV2 = head = magic2
            let isV1 = head = magic

            if not (isV3 || isV2 || isV1) then
                Error "not a Jetpac timeline file"
            else
                let version = r.ReadUInt16()

                if version <> Version then
                    Error(sprintf "unsupported timeline version %d" version)
                else
                    let flags = r.ReadUInt16()
                    // V1 declared itself uncompressed (flags 0); V2/V3 carry
                    // FlagsCompressed. A mismatch means the file is not what
                    // its magic claims.
                    let flagsOk =
                        if isV1 then flags = 0us
                        else flags &&& FlagsCompressed <> 0us

                    if not flagsOk then
                        Error "timeline flags do not match the file magic"
                    else
                        let storedFp =
                            { GameId = readLenString r
                              RomSha256 = readLenString r
                              TzxSha256 = readLenString r
                              ProgramSha256 = readLenString r
                              ProgramAddress =
                                match r.ReadByte() with
                                | 1uy -> Some(r.ReadInt32())
                                | _ -> None }

                        let eventCount = int (r.ReadUInt32())
                        let events = ResizeArray<KeyEvent>(eventCount)
                        let mutable valid = true
                        let mutable i = 0

                        while valid && i < eventCount do
                            let frame = r.ReadInt32()
                            let row = r.ReadInt32()
                            let bit = r.ReadInt32()
                            let pressed = r.ReadByte() <> 0uy

                            if frame < 0 || row < 0 || row >= 8 || bit < 0 || bit >= 5 then
                                valid <- false
                            else
                                events.Add
                                    { Frame = frame
                                      Row = row
                                      Bit = bit
                                      Pressed = pressed }

                            i <- i + 1

                        if not valid then
                            Error "timeline contains an invalid key event"
                        else
                            let startFrame = r.ReadInt32()

                            if startFrame < 0 then
                                Error "timeline has an invalid start frame"
                            else
                                let timeline = StateTimeline startFrame
                                let mutable error = None

                                if isV3 then
                                    let frameCount = int (r.ReadUInt32())
                                    let blobLen = int (r.ReadUInt32())
                                    let blob = r.ReadBytes blobLen

                                    if blob.Length <> blobLen then
                                        error <- Some "timeline file is truncated"
                                    else
                                        try
                                            let raw = inflateAll blob
                                            let summaries = decodeSummaries raw frameCount

                                            for j in 0 .. frameCount - 1 do
                                                timeline.CaptureSummary(
                                                    startFrame + j,
                                                    summaries[j]
                                                )
                                        with _ ->
                                            error <- Some "timeline summary payload is corrupt"

                                    if error.IsNone then
                                        let storedCount = int (r.ReadUInt32())
                                        let prev = Array.zeroCreate<byte> streamLen
                                        let mutable s = 0

                                        while error.IsNone && s < storedCount do
                                            let frame = r.ReadInt32()
                                            let textLen = int (r.ReadUInt16())
                                            let text = Encoding.UTF8.GetString(r.ReadBytes textLen)
                                            let blobLen = int (r.ReadUInt32())
                                            let blob = r.ReadBytes blobLen

                                            if frame < 0 || blob.Length <> blobLen then
                                                error <- Some "timeline file is truncated"
                                            else
                                                try
                                                    let payload = inflate blob streamLen
                                                    xorInto payload prev
                                                    Array.blit payload 0 prev 0 streamLen
                                                    let mem = Array.zeroCreate<byte> 0x10000
                                                    Array.blit payload 0 mem 0 0x10000
                                                    let keys = Array.zeroCreate<byte> 8
                                                    Array.blit payload 0x10000 keys 0 8
                                                    timeline.Capture(frame, mem, text, keys)
                                                with _ ->
                                                    error <- Some "timeline delta payload is corrupt"

                                            s <- s + 1
                                else
                                    // Legacy dense generations: every frame is stored.
                                    let frameCount = int (r.ReadUInt32())
                                    let prev = Array.zeroCreate<byte> streamLen
                                    let mutable f = startFrame

                                    while error.IsNone && f < startFrame + frameCount do
                                        let textLen = int (r.ReadUInt16())
                                        let text = Encoding.UTF8.GetString(r.ReadBytes textLen)

                                        if isV1 then
                                            let keyBytes = r.ReadBytes 8
                                            let memory = r.ReadBytes 0x10000

                                            if keyBytes.Length <> 8 || memory.Length <> 0x10000 then
                                                error <- Some "timeline file is truncated"
                                            else
                                                timeline.Capture(f, memory, text, keyBytes)
                                                f <- f + 1
                                        else
                                            // V2: textLen+text raw, then one DEFLATE blob of
                                            // the stream XORed against the previous frame.
                                            let blobLen = int (r.ReadUInt32())
                                            let blob = r.ReadBytes blobLen

                                            if blob.Length <> blobLen then
                                                error <- Some "timeline file is truncated"
                                            else
                                                try
                                                    let payload = inflate blob streamLen
                                                    xorInto payload prev
                                                    Array.blit payload 0 prev 0 streamLen
                                                    let mem = Array.zeroCreate<byte> 0x10000
                                                    Array.blit payload 0 mem 0 0x10000
                                                    let keys = Array.zeroCreate<byte> 8
                                                    Array.blit payload 0x10000 keys 0 8
                                                    timeline.Capture(f, mem, text, keys)
                                                    f <- f + 1
                                                with _ ->
                                                    error <- Some "timeline delta payload is corrupt"

                                match error with
                                | Some message -> Error message
                                | None -> Ok(storedFp, startFrame, timeline, Seq.toList events)
        with ex ->
            Error ex.Message

    /// Byte-compare a freshly-written V3 file against the in-memory recording
    /// (header identity, key script, every stored state and summary). The
    /// save path runs it before reporting Ok - a codec bug must never eat a
    /// recording silently.
    let private verifyV3
        (path: string)
        (fp: TimelineFingerprint)
        (timeline: StateTimeline)
        (events: KeyEvent list)
        : Result<unit, string> =
        match loadOne path with
        | Error message -> Error(sprintf ".js3 verify failed: %s" message)
        | Ok(storedFp, start, loaded, loadedEvents) ->
            if storedFp <> fp then
                Error ".js3 verify failed: header fingerprint mismatch"
            elif
                start <> timeline.StartFrame
                || loaded.StoredCount <> timeline.StoredCount
                || loaded.FrameCount <> timeline.FrameCount
            then
                Error(
                    sprintf
                        ".js3 verify failed: frame counts differ (%d/%d vs %d/%d)"
                        loaded.StoredCount
                        loaded.FrameCount
                        timeline.StoredCount
                        timeline.FrameCount
                )
            elif loadedEvents <> events then
                Error ".js3 verify failed: key script differs"
            else
                let mutable diff = None
                let mutable i = 0

                while diff.IsNone && i < timeline.StoredCount do
                    if timeline.StoredFrameAt i <> loaded.StoredFrameAt i then
                        diff <- Some(sprintf "stored frame number differs at index %d" i)
                    else
                        let t1, k1, m1 = timeline.FrameData i
                        let t2, k2, m2 = loaded.FrameData i

                        if t1 <> t2 then diff <- Some(sprintf "state text differs at stored index %d" i)
                        elif k1 <> k2 then diff <- Some(sprintf "keyboard bytes differ at stored index %d" i)
                        elif m1 <> m2 then diff <- Some(sprintf "memory differs at stored index %d" i)

                    i <- i + 1

                let mutable j = 0

                while diff.IsNone && j < timeline.SummaryCount do
                    if timeline.SummaryFrameAt j <> loaded.SummaryFrameAt j then
                        diff <- Some(sprintf "summary frame differs at index %d" j)
                    elif timeline.SummaryData j <> loaded.SummaryData j then
                        diff <- Some(sprintf "summary differs at frame index %d" j)

                    j <- j + 1

                match diff with
                | Some d -> Error(sprintf ".js3 verify failed: %s" d)
                | None -> Ok()

    /// Write the recording as V3 (.js3 next to the slot path), atomically,
    /// then decode it back and require an identical recording before Ok.
    /// Legacy generations are never written anymore.
    let save
        (path: string)
        (fp: TimelineFingerprint)
        (timeline: StateTimeline)
        (events: KeyEvent seq)
        : Result<unit, string> =
        try
            let directory = Path.GetDirectoryName path

            if not (String.IsNullOrEmpty directory) then
                Directory.CreateDirectory directory |> ignore

            let eventList = List.ofSeq events
            let v3Path = toV3Path path

            let writeAtomic (target: string) (writer: BinaryWriter -> unit) =
                let temporary = target + ".tmp"
                use fs = File.Create temporary
                use w = new BinaryWriter(fs)
                writer w
                w.Flush()
                fs.Dispose()
                File.Move(temporary, target, true)

            writeAtomic v3Path (fun w -> writeStreamV3 w fp timeline eventList)
            verifyV3 v3Path fp timeline eventList
        with ex ->
            try
                File.Delete(toV3Path path + ".tmp")
            with _ ->
                ()

            Error ex.Message

    /// True when the stored fingerprint matches the expected one (game id and
    /// every asset hash / program address).
    let private identityError (stored: TimelineFingerprint) (fp: TimelineFingerprint) : string option =
        if stored.GameId <> fp.GameId then
            Some "timeline belongs to another game"
        elif
            stored.RomSha256 <> fp.RomSha256
            || stored.TzxSha256 <> fp.TzxSha256
            || stored.ProgramSha256 <> fp.ProgramSha256
        then
            Some "timeline assets do not match the selected game"
        elif stored.ProgramAddress <> fp.ProgramAddress then
            Some "timeline assets do not match the selected game"
        else
            None

    /// Load a slot: the V3 sparse file when present, else its legacy .jst /
    /// .js2 dense file (read-only compatibility). Identity is checked against
    /// the expected fingerprint either way.
    let tryLoad (path: string) (fp: TimelineFingerprint) : TimelineLoad =
        let v3Path = toV3Path path

        let actualPath =
            if File.Exists v3Path then Some v3Path
            elif File.Exists path then Some path
            else None

        match actualPath with
        | None -> TimelineMissing
        | Some file ->
            match loadOne file with
            | Error message -> TimelineIgnored message
            | Ok(storedFp, _, timeline, events) ->
                match identityError storedFp fp with
                | Some reason -> TimelineIgnored reason
                | None -> TimelineLoaded(timeline, events)


/// Named recording slots: the legacy games/<id>/timeline.jst is the "default"
/// slot, further recordings live as games/<id>/timelines/<name>.jst in the
/// same base naming; since the sparse cadence every save writes the .js3
/// sibling (StateTimelineStore.save), and that file is the slot's content.
/// Listing peeks headers only (no frame data) and accepts every generation,
/// preferring a stem's .js3 over its legacy file.
module TimelineSlots =

    let defaultName = "default"

    let dirFor (gameDir: string) =
        Path.Combine(gameDir, "timelines")

    let pathFor (gameDir: string) (name: string) =
        if name = defaultName then
            Path.Combine(gameDir, StateTimelineStore.FileName)
        else
            Path.Combine(dirFor gameDir, name + ".jst")

    type SlotInfo =
        { Name: string
          Path: string
          Bytes: int64
          Modified: DateTime
          StartFrame: int option
          Frames: int option }

    /// Header-only read: (startFrame, frameCount, eventCount) without touching
    /// the per-frame states. Accepts every generation's magic.
    let tryPeek (path: string) : (int * int * int) option =
        try
            use fs = File.OpenRead path
            use r = new BinaryReader(fs)
            let head = r.ReadBytes 4
            let magicJst = [| byte 'J'; byte 'P'; byte 'S'; byte 'T' |]
            let magicJs2 = [| byte 'J'; byte 'S'; byte '2'; byte 'T' |]
            let magicJs3 = [| byte 'J'; byte 'S'; byte '3'; byte 'T' |]

            if
                (head <> magicJst && head <> magicJs2 && head <> magicJs3)
                || r.ReadUInt16() <> StateTimelineStore.Version
            then
                None
            else
                r.ReadUInt16() |> ignore // flags
                for _ in 1..4 do
                    let len = int (r.ReadByte())
                    r.ReadBytes len |> ignore
                if r.ReadByte() = 1uy then r.ReadInt32() |> ignore
                let eventCount = int (r.ReadUInt32())
                // frame i32 + row i32 + bit i32 + pressed u8 per event
                fs.Seek(int64 eventCount * 13L, SeekOrigin.Current) |> ignore
                let startFrame = r.ReadInt32()
                let frameCount = int (r.ReadUInt32())

                if startFrame < 0 || frameCount < 0 then None
                else Some(startFrame, frameCount, eventCount)
        with _ ->
            None

    let private info (name: string) (path: string) : SlotInfo option =
        try
            let fi = FileInfo path

            if not fi.Exists then
                None
            else
                let peeked = tryPeek path

                Some
                    { Name = name
                      Path = path
                      Bytes = fi.Length
                      Modified = fi.LastWriteTimeUtc
                      StartFrame = peeked |> Option.map (fun (s, _, _) -> s)
                      Frames = peeked |> Option.map (fun (_, f, _) -> f) }
        with _ ->
            None

    /// All slots, newest first. A stem carrying both a legacy file and its
    /// .js3 lists once (the .js3 wins - it is what the save path maintains).
    /// Empty when nothing was ever recorded.
    let list (gameDir: string) : SlotInfo list =
        let slotFiles =
            try
                let dir = dirFor gameDir

                if Directory.Exists dir then
                    [ yield! Directory.GetFiles(dir, "*.jst")
                      yield! Directory.GetFiles(dir, "*.js3") ]
                    |> Array.ofList
                else
                    [||]
            with _ ->
                [||]

        let legacy = info defaultName (Path.Combine(gameDir, StateTimelineStore.FileName)) |> Option.toList

        let named =
            slotFiles
            |> Array.map (fun p -> Path.GetFileNameWithoutExtension p, p)
            |> Array.groupBy fst
            |> Array.choose (fun (name, files) ->
                let preferred =
                    files
                    |> Array.map snd
                    |> Array.sortBy (fun p ->
                        // .js3 before .jst/.js2: same stem, current format first.
                        if Path.GetExtension(p).ToLowerInvariant() = ".js3" then 0 else 1)
                    |> Array.head

                info name preferred)

        (legacy @ List.ofArray named) |> List.sortByDescending (fun s -> s.Modified)

    let suggestName () =
        "timeline-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
