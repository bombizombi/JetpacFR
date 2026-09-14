namespace JetpacFR.Core

open System
open System.IO
open System.IO.Compression
open System.Text
open System.Globalization

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

    member _.CoversFrame(frame: int) =
        frame >= startFrame && frame < startFrame + memories.Count

    /// Bytes the timeline would occupy on disk in the current (uncompressed)
    /// format: the number the recording stats display reports.
    member _.BytesUsed = bytesUsed

    /// Record the state after `frame` completed. `memory` and `keys` are
    /// copied (they are the live machine images); `stateText` is immutable.
    member _.Capture(frame: int, memory: byte[], stateText: string, keyBytes: byte[]) =
        if frame <> startFrame + memories.Count then
            invalidOp (
                sprintf "timeline frames must be sequential: expected %d, got %d" (startFrame + memories.Count) frame
            )

        memories.Add(Array.copy memory)
        texts.Add stateText
        keys.Add(Array.copy keyBytes)
        bytesUsed <- bytesUsed + frameBytes stateText

    /// Restore the memory image at `frame` into `memory`; returns the state
    /// text and a copy of the keyboard matrix captured at that exact frame.
    member this.RestoreInto(frame: int, memory: byte[]) : string * byte[] =
        if not (this.CoversFrame frame) then
            invalidOp (
                sprintf
                    "no timeline state at frame %d (covers %d..%d)"
                    frame
                    startFrame
                    (startFrame + memories.Count - 1)
            )

        let idx = frame - startFrame
        Array.blit memories[idx] 0 memory 0 0x10000
        texts[idx], Array.copy keys[idx]

    /// Drop every state after `frame` (the "Go" branch point); new captures
    /// continue sequentially from frame + 1.
    member _.Truncate(frame: int) =
        let keep = frame - startFrame + 1

        if keep < memories.Count then
            let firstDropped = max 0 keep

            for i in firstDropped .. memories.Count - 1 do
                bytesUsed <- bytesUsed - frameBytes texts[i]

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

        texts[idx], keys[idx], memories[idx]

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

/// Binary codec for the per-frame timeline. Two on-disk formats, same header:
///   magic | version u16 | flags u16
///   gameId / romSha / tzxSha / programSha as u8-length-prefixed strings
///   programAddress: tag u8 (0 = none) + i32
///   keyEventCount u32 + events (frame i32, row i32, bit i32, pressed u8)
///   startFrame i32 (the first stored frame; 0 for recordings from boot)
///   frameCount u32 + per-frame payload (format-dependent)
///
/// V1 ("JPST", .jst): per frame textLen u16 + UTF8 text, keys[8], memory[65536]
/// raw. Old files stay readable by the loader below.
///
/// V2 ("JS2T", .js2): per frame textLen u16 + UTF8 text (raw - small and
/// variable-length, so it cannot join the fixed-width stream), then
/// payloadLen u32 + DEFLATE(XOR-stream). The XOR stream is constant 65544
/// bytes for every frame - memory[65536] ++ keys[8] - XORed against the
/// previous frame's stream (zeros for the first frame) before compression.
/// Typical game frames touch only hundreds of bytes, so the XOR is ~all
/// zeros and deflates to almost nothing.
module StateTimelineStore =

    let Version = 1us
    let FlagsCompressed = 1us
    let FileName = "timeline.jst"
    /// Second-format sibling: the same recording, delta-compressed.
    let FileNameV2 = "timeline.js2"

    /// The .js2 sibling of a .jst slot path (ChangeExtension keeps directory
    /// and stem, so every slot resolves with either file present).
    let toV2Path (path: string) : string = Path.ChangeExtension(path, ".js2")

    let private magic = [| byte 'J'; byte 'P'; byte 'S'; byte 'T' |]
    let private magic2 = [| byte 'J'; byte 'S'; byte '2'; byte 'T' |]

    let private writeLenString (w: BinaryWriter) (value: string) =
        let bytes = Encoding.UTF8.GetBytes(if isNull value then "" else value)

        if bytes.Length > 255 then
            invalidOp "timeline header string too long"

        w.Write(byte bytes.Length)
        w.Write bytes

    let private readLenString (r: BinaryReader) =
        let len = int (r.ReadByte())
        Encoding.UTF8.GetString(r.ReadBytes len)

    /// Write the whole timeline to an already-opened file (closed by the
    /// caller's `use` before the atomic rename happens).
    let private writeStream
        (w: BinaryWriter)
        (fp: TimelineFingerprint)
        (timeline: StateTimeline)
        (events: KeyEvent seq)
        =
        w.Write magic
        w.Write Version
        w.Write 0us // flags: uncompressed
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
        w.Write(uint32 timeline.Count)

        for i in 0 .. timeline.Count - 1 do
            let text, keyBytes, memory = timeline.FrameData i
            let textBytes = Encoding.UTF8.GetBytes text

            if textBytes.Length > 0xFFFF then
                invalidOp "state text too long for the timeline format"

            w.Write(uint16 textBytes.Length)
            w.Write textBytes
            w.Write keyBytes
            w.Write memory

    /// Fixed-width frame stream for the v2 codec: memory[65536] ++ keys[8],
    /// XORed against the previous frame (zeros for frame 0) before DEFLATE.
    [<Literal>]
    let private streamLen = 0x10000 + 8

    let private buildStream (memory: byte[]) (keyBytes: byte[]) (dst: byte[]) =
        Array.blit memory 0 dst 0 0x10000
        Array.blit keyBytes 0 dst 0x10000 8

    let private xorInto (cur: byte[]) (prev: byte[]) =
        for i in 0 .. streamLen - 1 do
            cur[i] <- cur[i] ^^^ prev[i]

    let private writeText (w: BinaryWriter) (text: string) =
        let textBytes = Encoding.UTF8.GetBytes text

        if textBytes.Length > 0xFFFF then
            invalidOp "state text too long for the timeline format"

        w.Write(uint16 textBytes.Length)
        w.Write textBytes

    /// V2 twin of writeStream: same header (JS2T magic, FlagsCompressed), then
    /// textLen + text raw per frame plus one length-prefixed DEFLATE blob of
    /// the XORed fixed-width stream.
    let private writeStreamV2
        (w: BinaryWriter)
        (fp: TimelineFingerprint)
        (timeline: StateTimeline)
        (events: KeyEvent seq)
        =
        w.Write magic2
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
        w.Write(uint32 timeline.Count)
        let cur = Array.zeroCreate<byte> streamLen
        let prev = Array.zeroCreate<byte> streamLen

        for i in 0 .. timeline.Count - 1 do
            let text, keyBytes, memory = timeline.FrameData i
            writeText w text
            buildStream memory keyBytes cur
            xorInto cur prev
            use ms = new MemoryStream()
            use dz = new DeflateStream(ms, CompressionLevel.Optimal, true)
            dz.Write(cur, 0, streamLen)
            dz.Close()
            let blob = ms.ToArray()
            w.Write(uint32 blob.Length)
            w.Write blob
            // Chain on the PLAIN stream: cur currently holds cur ^ prev, so
            // XOR once more to restore plain cur before it becomes prev.
            xorInto cur prev
            Array.blit cur 0 prev 0 streamLen

        w.Flush()


    /// Load one timeline file (either magic); shared by tryLoad and the
    /// dual-format verifier so both paths decode identically.
    let private loadOne (path: string) : Result<TimelineFingerprint * int * StateTimeline * KeyEvent list, string> =
        try
            use fs = File.OpenRead path
            use r = new BinaryReader(fs)
            let head = r.ReadBytes 4
            let compressed = head = magic2

            if head <> magic && not compressed then
                Error "not a Jetpac timeline file"
            else
                let version = r.ReadUInt16()

                if version <> Version then
                    Error(sprintf "unsupported timeline version %d" version)
                else
                    let flags = r.ReadUInt16()
                    let expectCompressed = compressed && flags &&& FlagsCompressed <> 0us

                    if compressed <> expectCompressed && not (not compressed && flags = 0us) then
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
                        let events = ResizeArray<KeyEvent> eventCount
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
                                let frameCount = int (r.ReadUInt32())
                                let timeline = StateTimeline startFrame
                                let mutable error = None
                                let mutable f = startFrame

                                if not compressed then
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
                                else
                                    // Chained XOR undo: keep the previous frame's
                                    // decoded stream; the blob holds cur ^ prev.
                                    let stream = Array.zeroCreate<byte> streamLen
                                    let prev = Array.zeroCreate<byte> streamLen
                                    let memory = Array.zeroCreate<byte> 0x10000
                                    let keyBytes = Array.zeroCreate<byte> 8

                                    while error.IsNone && f < startFrame + frameCount do
                                        let textLen = int (r.ReadUInt16())
                                        let text = Encoding.UTF8.GetString(r.ReadBytes textLen)
                                        let blobLen = int (r.ReadUInt32())
                                        let blob = r.ReadBytes blobLen

                                        if blob.Length <> blobLen then
                                            error <- Some "timeline file is truncated"
                                        else
                                            use ms = new MemoryStream(blob, false)
                                            use dz = new DeflateStream(ms, CompressionMode.Decompress)
                                            let mutable got = 0
                                            let mutable short = false

                                            while not short && got < streamLen do
                                                let n = dz.Read(stream, got, streamLen - got)

                                                if n = 0 then
                                                    short <- true
                                                else
                                                    got <- got + n

                                            if short || got <> streamLen then
                                                error <- Some "timeline delta payload is corrupt"
                                            else
                                                xorInto stream prev
                                                Array.blit stream 0 prev 0 streamLen
                                                Array.blit stream 0 memory 0 0x10000
                                                Array.blit stream 0x10000 keyBytes 0 8
                                                timeline.Capture(f, memory, text, keyBytes)

                                        f <- f + 1

                                match error with
                                | Some message -> Error message
                                | None -> Ok(storedFp, startFrame, timeline, Seq.toList events)
        with ex ->
            Error ex.Message

    /// Byte-compare two decoded timelines (header identity, key script, every
    /// frame's memory/text/keys). Used by the dual-format save verifier.
    let private timelinesEqual
        (fpA: TimelineFingerprint)
        (startA: int)
        (a: StateTimeline)
        (eventsA: KeyEvent list)
        (fpB: TimelineFingerprint)
        (startB: int)
        (b: StateTimeline)
        (eventsB: KeyEvent list)
        : string option =
        if fpA <> fpB then Some "header fingerprint mismatch between .jst and .js2"
        elif startA <> startB || a.Count <> b.Count then
            Some(sprintf "frame count mismatch between .jst and .js2 (%d vs %d)" a.Count b.Count)
        elif eventsA <> eventsB then Some "key script mismatch between .jst and .js2"
        else
            let mutable diff = None
            let mutable i = 0

            while diff.IsNone && i < a.Count do
                let t1, k1, m1 = a.FrameData i
                let t2, k2, m2 = b.FrameData i

                if t1 <> t2 then diff <- Some(sprintf "state text differs at frame index %d" i)
                elif k1 <> k2 then diff <- Some(sprintf "keyboard bytes differ at frame index %d" i)
                elif m1 <> m2 then diff <- Some(sprintf "memory differs at frame index %d" i)

                i <- i + 1

            diff

    /// Decode both freshly-written files and require byte-identical results.
    /// REMOVE-ME (uncompressed): when V1 retires, shrink this to a .js2-only
    /// smoke decode (or drop it) instead of a pair comparison.
    let private verifyPair (jstPath: string) (js2Path: string) (fp: TimelineFingerprint) : Result<unit, string> =
        let mismatch (stored: TimelineFingerprint) = stored <> fp
#if REMOVE_V1_TIMELINE
        match loadOne js2Path with
        | Error message -> Error(sprintf ".js2 verify failed: %s" message)
        | Ok(storedFp, _, _, _) ->
            if mismatch storedFp then Error ".js2 verify failed: header fingerprint mismatch"
            else Ok()
#else
        match loadOne jstPath, loadOne js2Path with
        | Error message, _ -> Error(sprintf ".jst verify failed: %s" message)
        | _, Error message -> Error(sprintf ".js2 verify failed: %s" message)
        | Ok(fpA, startA, a, eventsA), Ok(fpB, startB, b, eventsB) ->
            if mismatch fpA || mismatch fpB then
                Error ".jst/.js2 verify failed: header fingerprint mismatch"
            else
                match timelinesEqual fpA startA a eventsA fpB startB b eventsB with
                | Some diff -> Error(sprintf ".jst/.js2 verify failed: %s" diff)
                | None -> Ok()
#endif
    /// Dual-format save (transition clutch): writes BOTH the .jst (V1 raw)
    /// and its .js2 sibling (V2 delta-compressed), then loads both back and
    /// checks the decoded states are byte-identical before reporting Ok.
    /// REMOVE-ME (uncompressed): once the .js2 path has proven itself, delete
    /// the V1 write below (and the #else branch of LOAD_BOTH below) and keep
    /// only the .js2 write here.
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

            let writeAtomic (target: string) (writer: BinaryWriter -> unit) =
                let temporary = target + ".tmp"
                use fs = File.Create temporary
                use w = new BinaryWriter(fs)
                writer w
                w.Flush()
                fs.Dispose()
                File.Move(temporary, target, true)

            let v2Path = toV2Path path
#if REMOVE_V1_TIMELINE
            // Uncompressed V1 retired: .js2 is the only on-disk format.
            writeAtomic v2Path (fun w -> writeStreamV2 w fp timeline eventList)
#else
            writeAtomic path (fun w -> writeStream w fp timeline eventList)
            writeAtomic v2Path (fun w -> writeStreamV2 w fp timeline eventList)
#endif
            // Verify-before-return: decode both files fresh and compare every
            // frame (memory, text, keys) plus the key script.
            verifyPair path v2Path fp
        with ex ->
            try
                File.Delete(path + ".tmp")
            with _ ->
                ()

            try
                File.Delete(toV2Path path + ".tmp")
            with _ ->
                ()

            Error ex.Message

    /// Clutch: build the missing .js2 twin straight from a lone .jst, without
    /// touching the recording. Used by tryLoad so every load still compares
    /// both formats; safe to delete once every slot on disk has its twin.
    let duplicateJstToJs2 (jstPath: string) : Result<unit, string> =
        match loadOne jstPath with
        | Error message -> Error message
        | Ok(fp, _, timeline, events) ->
            try
                let target = toV2Path jstPath
                let temporary = target + ".tmp"
                use fs = File.Create temporary
                use w = new BinaryWriter(fs)
                writeStreamV2 w fp timeline events
                w.Flush()
                fs.Dispose()
                File.Move(temporary, target, true)
                Ok()
            with ex ->
                try
                    File.Delete(toV2Path jstPath + ".tmp")
                with _ ->
                    ()

                Error ex.Message

    /// Dual-format load (transition clutch): decodes the .jst AND its .js2
    /// sibling and requires byte-identical results (memory, text, keys, key
    /// script, header identity) before installing either. A lone .jst is
    /// rebuilt into its .js2 twin first (duplicateJstToJs2 above), so the
    /// pair comparison still runs everywhere.
    /// REMOVE-ME (uncompressed): when V1 retires, load the .js2 directly and
    /// drop the pair comparison (keep duplicateJstToJs2 for legacy files).
    let tryLoad (path: string) (fp: TimelineFingerprint) : TimelineLoad =
        if not (File.Exists path) then
            TimelineMissing
        else
            let v2Path = toV2Path path

            if File.Exists v2Path then
                ()
            elif Path.GetExtension(path).ToLowerInvariant() = ".jst" then
                // Clutch: .js2 missing - decode the .jst once; if it is valid,
                // materialize the twin so the pair check below still applies.
                match loadOne path with
                | Ok _ ->
                    match duplicateJstToJs2 path with
                    | Ok() -> ()
                    | Error _ -> ()
                | Error _ -> ()

            let checkIdentity (stored: TimelineFingerprint) : TimelineLoad option =
                if stored.GameId <> fp.GameId then
                    Some(TimelineIgnored "timeline belongs to another game")
                elif
                    stored.RomSha256 <> fp.RomSha256
                    || stored.TzxSha256 <> fp.TzxSha256
                    || stored.ProgramSha256 <> fp.ProgramSha256
                then
                    Some(TimelineIgnored "timeline assets do not match the selected game")
                elif stored.ProgramAddress <> fp.ProgramAddress then
                    Some(TimelineIgnored "timeline assets do not match the selected game")
                else
                    None

#if REMOVE_V1_TIMELINE
            // Uncompressed V1 retired: the .js2 is the recording.
            match loadOne v2Path with
            | Error message -> TimelineIgnored message
            | Ok(storedFp, _, timeline, events) ->
                match checkIdentity storedFp with
                | Some rejection -> rejection
                | None -> TimelineLoaded(timeline, events)
#else
            match loadOne path, (if File.Exists v2Path then Some(loadOne v2Path) else None) with
            | Error message, _ -> TimelineIgnored message
            | _, Some(Error message) -> TimelineIgnored(sprintf ".js2 twin unreadable: %s" message)
            | Ok(storedA, _, timelineA, eventsA), None ->
                // No twin and no clutch rebuild (corrupt .jst or odd extension):
                // fall back to the lone file after the identity check.
                match checkIdentity storedA with
                | Some rejection -> rejection
                | None -> TimelineLoaded(timelineA, eventsA)
            | Ok(storedA, startA, timelineA, eventsA), Some(Ok(storedB, startB, timelineB, eventsB)) ->
                match checkIdentity storedA with
                | Some rejection -> rejection
                | None ->
                    match checkIdentity storedB with
                    | Some rejection -> rejection
                    | None ->
                        match timelinesEqual storedA startA timelineA eventsA storedB startB timelineB eventsB with
                        | Some diff -> TimelineIgnored(sprintf ".jst/.js2 twins differ: %s" diff)
                        | None -> TimelineLoaded(timelineA, eventsA)
#endif


/// Named recording slots: the legacy games/<id>/timeline.jst is the "default"
/// slot, further recordings live as games/<id>/timelines/<name>.jst in the
/// same uncompressed format, each with a .js2 delta-compressed twin written
/// alongside by the dual-format save. Listing peeks headers only (no frame
/// data) and accepts either magic, so slots stay visible while twins land.
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
    /// the per-frame states. Accepts either magic (.jst or .js2 twin).
    let tryPeek (path: string) : (int * int * int) option =
        try
            use fs = File.OpenRead path
            use r = new BinaryReader(fs)
            let head = r.ReadBytes 4
            let magicJst = [| byte 'J'; byte 'P'; byte 'S'; byte 'T' |]
            let magicJs2 = [| byte 'J'; byte 'S'; byte '2'; byte 'T' |]

            if (head <> magicJst && head <> magicJs2) || r.ReadUInt16() <> StateTimelineStore.Version then
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

    /// All slots, newest first. Empty when nothing was ever recorded.
    let list (gameDir: string) : SlotInfo list =
        let legacy = info defaultName (Path.Combine(gameDir, StateTimelineStore.FileName)) |> Option.toList

        let named =
            try
                let dir = dirFor gameDir

                if Directory.Exists dir then
                    Directory.GetFiles(dir, "*.jst")
                    |> Array.sort
                    |> Array.choose (fun p -> info (Path.GetFileNameWithoutExtension p) p)
                    |> Array.toList
                else
                    []
            with _ ->
                []

        (legacy @ named) |> List.sortByDescending (fun s -> s.Modified)

    let suggestName () =
        "timeline-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
