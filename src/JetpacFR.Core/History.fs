namespace JetpacFR.Core

open System

/// One recorded keyboard event: the (row, bit) matrix position and whether it
/// was pressed or released, stamped with the frame number in which it landed.
[<Struct>]
type KeyEvent =
  { Frame: int
    Row: int
    Bit: int
    Pressed: bool }

/// The frame-snapshotted history store behind rewind + replay.
///
/// Storage per frame:
///   - scalars: the state text (registers/timing) + keyboard matrix, ~130 B;
///   - memory: a full 64K anchor every `anchorInterval` frames, exact byte
///     deltas between (Jetpac writes a few hundred bytes per frame, so
///     deltas run ~30-60 MB/min; full snapshots would be 190 MB/s).
///
/// The frame indexed N is the machine state *after* frame N completed. Frame
/// 0 is the game entry state (the slider's minimum, always reachable).
///
/// Rewind (Restore) is a pure lookup: history is NOT truncated, so the user
/// can drag a slider back and forth freely. Truncation is an explicit
/// operation (branch: the moment "Go" is pressed and the old future is
/// abandoned).
type FrameHistory(anchorInterval: int, budgetBytes: int64) =

  /// Full 64K memory image at an anchor frame.
  let anchors = ResizeArray<struct (int * byte[])>()
  /// Byte diffs for non-anchor frames: addr<<16 | old<<8 | new.
  let deltas = ResizeArray<struct (int * int[])>()
  /// Per-frame scalars: state text + keyboard half-row bytes.
  let scalars = ResizeArray<struct (int * string * byte[])>()
  let mutable totalDeltaBytes = 0L
  let lastMemory = Array.zeroCreate<byte> 0x10000
  let mutable lastFrame = -1

  do
    if anchorInterval < 1 then invalidArg (nameof anchorInterval) "anchor interval must be positive"

  member _.LastFrame = lastFrame
  member _.AnchorCount = anchors.Count
  member _.DeltasCaptured = deltas.Count

  /// Record the state after `frame` completed. `memory` is read in place
  /// (not retained) unless this frame is an anchor.
  member _.Capture(frame: int, memory: byte[], stateText: string, keys: byte[]) =
    if frame <> lastFrame + 1 then
      invalidOp (sprintf "history frames must be sequential: got %d after %d" frame lastFrame)
    scalars.Add(struct (frame, stateText, keys))
    let isAnchor = anchors.Count = 0 || frame % anchorInterval = 0
    if isAnchor then
      anchors.Add(struct (frame, Array.copy memory))
    else
      let packed = ResizeArray<int>()
      for i in 0 .. 0xFFFF do
        if memory.[i] <> lastMemory.[i] then
          packed.Add((i <<< 16) ||| (int lastMemory.[i] <<< 8) ||| int memory.[i])
      if packed.Count > 0 then
        deltas.Add(struct (frame, packed.ToArray()))
        totalDeltaBytes <- totalDeltaBytes + int64 (packed.Count * 4)
    Array.blit memory 0 lastMemory 0 0x10000
    lastFrame <- frame
    // Eviction: drop the oldest anchor (and its deltas) when over budget.
    // Frame 0's anchor is never dropped.
    while totalDeltaBytes > budgetBytes && anchors.Count > 1 do
      let struct (af, _) = anchors.[0]
      deltas.RemoveAll(fun struct (f, _) -> f <= af) |> ignore
      anchors.RemoveAt(0)
      totalDeltaBytes <- deltas |> Seq.sumBy (fun struct (_, d) -> int64 d.Length * 4L)

  /// The state text + keyboard matrix at `frame` (exact scalars exist for
  /// every captured frame).
  member private this.ScalarsAt(frame: int) : string * byte[] =
    let mutable i = scalars.Count - 1
    while i > 0 && (let struct (f, _, _) = scalars.[i] in f > frame) do i <- i - 1
    let struct (_, text, keys) = scalars.[i]
    text, keys

  /// Restore the memory image at `frame` into `memory`; returns the state
  /// text and keyboard matrix captured at that exact frame.
  member this.Restore(frame: int, memory: byte[]) : string * byte[] =
    if frame < 0 || frame > lastFrame then
      invalidOp (sprintf "no history at frame %d (last=%d)" frame lastFrame)
    let mutable ai = anchors.Count - 1
    while ai > 0 && (let struct (af, _) = anchors.[ai] in af > frame) do ai <- ai - 1
    let struct (af, mem) = anchors.[ai]
    Array.blit mem 0 memory 0 0x10000
    for i = 0 to deltas.Count - 1 do
      let struct (df, d) = deltas.[i]
      if df > af && df <= frame then
        for p in d do
          memory.[(p >>> 16) &&& 0xFFFF] <- byte (p &&& 0xFF)
    let text, keys = this.ScalarsAt frame
    text, Array.copy keys

  /// Drop everything captured after `frame` (the "Go" branch point); new
  /// captures continue sequentially from the restored state. `memory` must
  /// be the restored image (the delta baseline restarts from it).
  member _.Truncate(frame: int, memory: byte[]) =
    if frame < 0 || frame > lastFrame then
      invalidOp (sprintf "no history at frame %d (last=%d)" frame lastFrame)
    deltas.RemoveAll(fun struct (f, _) -> f > frame) |> ignore
    scalars.RemoveAll(fun struct (f, _, _) -> f > frame) |> ignore
    while anchors.Count > 1 && (let struct (af, _) = anchors.[anchors.Count - 1] in af > frame) do
      anchors.RemoveAt(anchors.Count - 1)
    lastFrame <- frame
    Array.blit memory 0 lastMemory 0 0x10000
    totalDeltaBytes <- deltas |> Seq.sumBy (fun struct (_, d) -> int64 d.Length * 4L)

/// Recorded keyboard events, appended in live mode and replayed by frame.
type KeyLog() =
  let events = ResizeArray<KeyEvent>()

  member _.Events = events
  member _.Count = events.Count

  member _.Add(e: KeyEvent) = events.Add e

  /// Remove events at frames strictly after `frame` (branch point).
  member _.Truncate(frame: int) =
    let mutable keep = events.Count
    while keep > 0 && events.[keep - 1].Frame > frame do keep <- keep - 1
    if keep < events.Count then events.RemoveRange(keep, events.Count - keep)

  /// Events to apply for the given frame during replay (in order).
  member _.ForFrame(frame: int) : KeyEvent list =
    events |> Seq.filter (fun e -> e.Frame = frame) |> Seq.toList

  /// The last frame the log covers; -1 when empty.
  member _.EndFrame = if events.Count = 0 then -1 else events.[events.Count - 1].Frame
