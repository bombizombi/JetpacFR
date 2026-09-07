namespace JetpacFR.Core

open System
open System.Collections.Generic

/// CALL/RET/RST byte classification shared by the trace miners and the flame
/// walker. `isCall`/`isRet` are Miner's historical predicates (RST excluded),
/// so mining behavior is unchanged; the flame walker additionally treats RST
/// as a call - it pushes a return address exactly like CALL.
module CallOps =

  let isCall (b0: byte) =
    match b0 with
    | 0xCDuy | 0xC4uy | 0xCCuy | 0xD4uy | 0xDCuy | 0xE4uy | 0xECuy | 0xF4uy | 0xFCuy -> true
    | _ -> false

  let isRst (b0: byte) = b0 &&& 0xC7uy = 0xC7uy

  let isRet (b0: byte) (b1: byte) =
    match b0 with
    | 0xC9uy | 0xC0uy | 0xC8uy | 0xD0uy | 0xD8uy | 0xE0uy | 0xE8uy | 0xF0uy | 0xF8uy -> true
    | 0xEDuy -> b1 = 0x45uy || b1 = 0x4Duy // RETN / RETI
    | _ -> false

/// How a flame-graph frame was entered. Enum, not union: FlameRect is a
/// struct and stays blittable-lean.
type FlameKind =
  | FlameCall = 0
  | FlameRst = 1
  | FlameInterrupt = 2
  | FlameRoot = 3

/// One flame-graph rectangle: a single function invocation (not an aggregate)
/// spanning [StartTick, EndTick) of ticks relative to the window's first
/// instruction. Depth 0 is the synthetic root; every taken CALL/RST and every
/// interrupt marker pushed one frame below it. StartIndex/EndIndex delimit the
/// window's instruction entries executed inside the invocation (EndIndex
/// exclusive) - the instruction-level drill-down reads them back when the
/// window kept its detail tier.
[<Struct>]
type FlameRect =
  { Entry: uint16
    /// The CALL/RST site that opened the frame (0 for root/interrupts).
    CallPc: uint16
    StartTick: int64
    EndTick: int64
    Depth: int
    Kind: FlameKind
    StartIndex: int
    EndIndex: int }

/// The walked result over one execution window. Ticks are int64 relative to
/// the window start (the trace's uint32 machine-cycle counter wraps after
/// ~20 minutes; unwrapping happens once here, never in the UI).
type FlameWindow =
  { /// Sorted by StartTick; query via `stab`.
    Rects: FlameRect[]
    /// rects[i].EndTick maximum over [0..i]: non-decreasing, so the first
    /// rectangle that can overlap a query tick is binary-searchable.
    PrefixMaxEnd: int64[]
    /// Raw frame-boundary ticks (Trace.FrameTicks passthrough), unwrapped and
    /// relative like the rect ticks. Boundary j is the tick frame
    /// (FirstFrame + j) completed at.
    FrameTicks: int64[]
    /// The first executed frame of the window (-1 when unknown, e.g. a live
    /// trace-ring walk); the host uses it to map frames <-> boundary indexes.
    FirstFrame: int
    BaseTick: int64
    EndTick: int64
    MaxDepth: int
    /// Detail tier: the window's instruction entries with unwrapped ticks.
    /// The cache evicts these for large windows (FlameWindow.trim).
    Entries: TraceEntry[] option
    EntryTicks: int64[] option }

module FlameWindow =

  let empty : FlameWindow =
    { Rects = [||]
      PrefixMaxEnd = [||]
      FrameTicks = [||]
      FirstFrame = -1
      BaseTick = 0L
      EndTick = 0L
      MaxDepth = 1
      Entries = None
      EntryTicks = None }

  /// Drop the detail tier (per-instruction entries) - the rect tier is tiny
  /// and always kept.
  let trim (w: FlameWindow) : FlameWindow =
    { w with Entries = None; EntryTicks = None }

  /// Index of the first rectangle that can overlap `tick`: PrefixMaxEnd is
  /// sorted, so the first entry exceeding the tick bounds every later one.
  let stab (w: FlameWindow) (tick: int64) : int =
    let a = w.PrefixMaxEnd
    let rec search lo hi =
      if lo > hi then lo
      else
        let mid = (lo + hi) >>> 1
        if a[mid] > tick then search lo (mid - 1)
        else search (mid + 1) hi
    search 0 (a.Length - 1)

  /// Last entry index with EntryTicks <= tick (-1 when none).
  let entryAtTick (w: FlameWindow) (tick: int64) : int =
    match w.EntryTicks with
    | Some ticks when ticks.Length > 0 ->
      let rec search lo hi =
        if lo > hi then hi
        else
          let mid = (lo + hi) >>> 1
          if ticks[mid] <= tick then search (mid + 1) hi
          else search lo (mid - 1)
      search 0 (ticks.Length - 1)
    | _ -> -1

  /// The tick frame `frame` completed at (relative ticks). Requires
  /// FirstFrame >= 0 and the frame inside the window.
  let endTickOfFrame (w: FlameWindow) (frame: int) : int64 =
    if w.FirstFrame < 0 || frame < w.FirstFrame || frame - w.FirstFrame >= w.FrameTicks.Length then
      invalidOp (sprintf "frame %d outside the flame window (first %d, %d boundaries)" frame w.FirstFrame w.FrameTicks.Length)
    w.FrameTicks[frame - w.FirstFrame]

  /// The frame whose span contains `tick`, clamped into the window
  /// (-1 when the window has no frame mapping).
  let frameAtTick (w: FlameWindow) (tick: int64) : int =
    if w.FirstFrame < 0 || w.FrameTicks.Length = 0 then -1
    else
      let rec search lo hi =
        if lo > hi then lo - 1
        else
          let mid = (lo + hi) >>> 1
          if w.FrameTicks[mid] <= tick then search (mid + 1) hi
          else search lo (mid - 1)
      let idx = search 0 (w.FrameTicks.Length - 1)
      w.FirstFrame + (max 0 idx)

/// Walks an instruction trace into a flame-graph window: a synthetic
/// interrupt-aware call stack driven by taken CALL/RST/RET classification
/// (Miner's policy, see module docs there - JP-out exits desync the stack;
/// frames still open at the window end are closed there).
///
/// Tick handling: the trace's Tick is the machine's uint32 cycle counter,
/// which wraps every ~20 minutes. Entries in a window are monotonic except
/// for that wrap, unwrapped once into int64 and rebased so the window starts
/// at tick 0.
module FlameWalker =

  type private Frame =
    { Entry: int
      CallPc: int
      Kind: FlameKind
      Depth: int
      StartTick: int64
      mutable EndTick: int64
      StartIndex: int
      mutable EndIndex: int }

  let maxDepth = 256

  /// Walk `entries` (execution order, monotonic ticks) plus the frame
  /// boundary ticks into a FlameWindow. `firstFrame` tags the window for
  /// frame <-> tick mapping (see FlameWindow.frameAtTick).
  let walk (firstFrame: int) (frameTicks: uint32[]) (entries: TraceEntry[]) : FlameWindow =
    let n = entries.Length
    if n = 0 then { FlameWindow.empty with FirstFrame = firstFrame }
    else
      // Unwrap the uint32 cycle counter: ticks are non-decreasing except at
      // the wrap, where the raw value jumps backwards by (2^32 - progress).
      let ticks = Array.zeroCreate<int64> n
      let mutable offset = 0L
      let mutable prev = Int64.MinValue
      for i in 0 .. n - 1 do
        let raw = int64 entries[i].Tick
        if raw + offset < prev then offset <- offset + 4294967296L
        let t = raw + offset
        ticks[i] <- t
        prev <- t
      let baseTick = ticks[0]
      for i in 0 .. n - 1 do ticks[i] <- ticks[i] - baseTick

      // Frame boundaries share the machine's counter, so they unwrap from
      // the same absolute origin and rebase on the first entry's raw tick.
      // A boundary can precede the window's first entry (evicted ring) -
      // clamp those to 0 so the array stays sorted for binary search.
      let frameTicks64 =
        let arr = Array.zeroCreate<int64> frameTicks.Length
        let mutable off = 0L
        let mutable prevF = Int64.MinValue
        for j in 0 .. frameTicks.Length - 1 do
          let raw = int64 frameTicks[j]
          if raw + off < prevF then off <- off + 4294967296L
          let t = raw + off
          arr[j] <- max 0L (t - int64 entries[0].Tick)
          prevF <- t
        arr

      let windowEnd =
        let lastEntryEnd = ticks[n - 1] + int64 entries[n - 1].Cycles
        if frameTicks64.Length > 0 then max lastEntryEnd frameTicks64[frameTicks64.Length - 1]
        else lastEntryEnd

      let stack = ResizeArray<Frame>(64)
      // Completed invocations (popped by a RET) - they leave the stack and
      // must not be lost: only the root and frames still open at the window
      // end remain on the stack when the walk finishes.
      let finished = ResizeArray<Frame>(64)
      // Synthetic root: everything not inside a CALL/RST/ISR frame. It spans
      // the whole window and is never popped (a RET with only the root on
      // the stack means the window opened mid-function - it is ignored).
      stack.Add
        { Entry = 0x0000
          CallPc = 0
          Kind = FlameKind.FlameRoot
          Depth = 0
          StartTick = 0L
          EndTick = windowEnd
          StartIndex = 0
          EndIndex = n }

      for i in 0 .. n - 1 do
        let e = entries[i]
        if e.Length = 0uy then
          // Interrupt marker: the ISR becomes its own frame (its RET pops it).
          if stack.Count < maxDepth then
            stack.Add
              { Entry = int e.Pc
                CallPc = 0
                Kind = FlameKind.FlameInterrupt
                Depth = stack.Count
                StartTick = ticks[i]
                EndTick = ticks[i]
                StartIndex = i
                EndIndex = i }
        else
          let top = stack[stack.Count - 1]
          let endTick = ticks[i] + int64 e.Cycles
          if endTick > top.EndTick then top.EndTick <- endTick
          top.EndIndex <- i + 1
          let b0 = e.B0
          if CallOps.isCall b0 && e.Taken = 1uy && stack.Count < maxDepth then
            stack.Add
              { Entry = int e.Target
                CallPc = int e.Pc
                Kind = FlameKind.FlameCall
                Depth = stack.Count
                StartTick = ticks[i]
                EndTick = ticks[i]
                StartIndex = i
                EndIndex = i }
          elif CallOps.isRst b0 && stack.Count < maxDepth then
            stack.Add
              { Entry = int e.Target
                CallPc = int e.Pc
                Kind = FlameKind.FlameRst
                Depth = stack.Count
                StartTick = ticks[i]
                EndTick = ticks[i]
                StartIndex = i
                EndIndex = i }
          elif CallOps.isRet b0 e.B1 && e.Taken = 1uy && stack.Count > 1 then
            // The RET instruction itself was attributed to the frame above;
            // merge it into its parent, whose own entries may end earlier.
            let f = stack[stack.Count - 1]
            stack.RemoveAt(stack.Count - 1)
            finished.Add f
            let parent = stack[stack.Count - 1]
            if f.EndTick > parent.EndTick then parent.EndTick <- f.EndTick
            if f.EndIndex > parent.EndIndex then parent.EndIndex <- f.EndIndex

      // Frames still open at the window end (JP-out exits, window cut
      // mid-call) close there - the dashed-right-edge case.
      while stack.Count > 1 do
        let f = stack[stack.Count - 1]
        stack.RemoveAt(stack.Count - 1)
        if f.EndTick < windowEnd then f.EndTick <- windowEnd
        if f.EndIndex < n then f.EndIndex <- n
        finished.Add f

      let rects =
        // The label set matches the private Frame record above, so the
        // constructor must be qualified or F# binds the wrong record.
        [|
          for f in finished ->
            { FlameRect.Entry = uint16 f.Entry
              CallPc = uint16 f.CallPc
              StartTick = f.StartTick
              EndTick = max f.EndTick f.StartTick
              Depth = f.Depth
              Kind = f.Kind
              StartIndex = f.StartIndex
              EndIndex = f.EndIndex }
          for f in stack ->
            { FlameRect.Entry = uint16 f.Entry
              CallPc = uint16 f.CallPc
              StartTick = f.StartTick
              EndTick = max f.EndTick f.StartTick
              Depth = f.Depth
              Kind = f.Kind
              StartIndex = f.StartIndex
              EndIndex = f.EndIndex }
        |]
      Array.sortInPlaceBy (fun (r: FlameRect) -> r.StartTick, r.Depth) rects
      let prefixMax = Array.zeroCreate<int64> rects.Length
      let mutable runningMax = 0L
      for i in 0 .. rects.Length - 1 do
        if rects[i].EndTick > runningMax then runningMax <- rects[i].EndTick
        prefixMax[i] <- runningMax
      let mutable depth = 1
      for r in rects do
        if r.Depth + 1 > depth then depth <- r.Depth + 1
      { Rects = rects
        PrefixMaxEnd = prefixMax
        FrameTicks = frameTicks64
        FirstFrame = firstFrame
        BaseTick = baseTick
        EndTick = max windowEnd 1L
        MaxDepth = depth
        Entries = Some entries
        EntryTicks = Some ticks }
