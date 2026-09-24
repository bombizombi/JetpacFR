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

    let isRst (b0: byte) = b0 &&& 0xC7uy = 0xC7uy

    let isRet (b0: byte) (b1: byte) =
        match b0 with
        | 0xC9uy
        | 0xC0uy
        | 0xC8uy
        | 0xD0uy
        | 0xD8uy
        | 0xE0uy
        | 0xE8uy
        | 0xF0uy
        | 0xF8uy -> true
        | 0xEDuy -> b1 = 0x45uy || b1 = 0x4Duy // RETN / RETI
        | _ -> false

/// How a flame-graph frame was entered. Enum, not union: FlameRect is a
/// struct and stays blittable-lean.
type FlameKind =
    | FlameCall = 0
    | FlameRst = 1
    | FlameInterrupt = 2
    | FlameRoot = 3

/// One stack level of a per-frame flame summary: the function that occupied
/// that depth for most of the frame and the T-states it covered there. A
/// level whose function changed mid-frame contributes only its dominant
/// occupant - enough to paint zoomed-out flame blocks without emulation.
/// (Lives here so every consumer - the sparse timeline, the live tracker,
/// the builder and the web subset, which compiles this file without Trace.fs -
/// speaks the same shape.)
[<Struct>]
type FrameSummaryLevel =
    { Entry: uint16
      Coverage: uint32 }

/// Per-frame flame summary, captured live for EVERY recorded frame (dense),
/// while the full 64K states are captured sparsely (see TimelineCadence).
/// ~10-50 bytes per frame compressed, it is what makes the zoomed-out flame
/// graph paint with zero emulation: one dominant rectangle per stack level,
/// per frame, plus the instruction count that lets clicks map pixels to
/// instruction-index ranges.
type FrameSummary =
    {
        /// Trace-entry count of the frame (interrupt markers included - the
        /// same currency the flame window's instruction tier speaks).
        InstrCount: uint16
        /// Stack depth at frame end: calls still open here continue into the
        /// next frame's summary (the walker's cross-frame open frames).
        EndDepth: uint8
        /// Levels[0] is the synthetic root (depth 0); length = max depth reached.
        Levels: FrameSummaryLevel[] }

module FrameSummary =

    /// Rough on-disk size estimate (the exact V3 encoding is 5 + 6*levels
    /// bytes, pre-compression): the number the recording stats display reports.
    let byteSize (s: FrameSummary) = int64 (8 + 6 * s.Levels.Length)

/// One flame-graph rectangle: a single function invocation (not an aggregate)
/// spanning [StartTick, EndTick) of ticks relative to the window's first
/// instruction. Depth 0 is the synthetic root; every taken CALL/RST and every
/// interrupt marker pushed one frame below it. StartIndex/EndIndex delimit the
/// window's instruction entries executed inside the invocation (EndIndex
/// exclusive) - the instruction-level drill-down reads them back when the
/// window kept its detail tier.
[<Struct>]
type FlameRect =
    {
        Entry: uint16
        /// The CALL/RST site that opened the frame (0 for root/interrupts).
        CallPc: uint16
        StartTick: int64
        EndTick: int64
        Depth: int
        Kind: FlameKind
        StartIndex: int
        EndIndex: int
    }

/// The walked result over one execution window. Ticks are int64 relative to
/// the window start (the trace's uint32 machine-cycle counter wraps after
/// ~20 minutes; unwrapping happens once here, never in the UI).
type FlameWindow =
    {
        /// Sorted by StartTick; query via `stab`.
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
        EntryTicks: int64[] option
        /// Per-frame flame summaries parallel to FrameTicks (sparse recordings
        /// capture them live; see FrameSummary). None when unknown.
        FrameSummaries: FrameSummary[] option
        /// Prefix instruction counts: InstrBase[j] = window entries executed
        /// before frame (FirstFrame + j); length FrameTicks.Length + 1. The
        /// pixel -> instruction-range mapping for click requests.
        InstrBase: int64[] option
    }

module FlameWindow =

    let empty: FlameWindow =
        { Rects = [||]
          PrefixMaxEnd = [||]
          FrameTicks = [||]
          FirstFrame = -1
          BaseTick = 0L
          EndTick = 0L
          MaxDepth = 1
          Entries = None
          EntryTicks = None
          FrameSummaries = None
          InstrBase = None }

    /// Drop the detail tier (per-instruction entries) - the rect tier is tiny
    /// and always kept.
    let trim (w: FlameWindow) : FlameWindow =
        { w with
            Entries = None
            EntryTicks = None }

    /// Entries executed before window-frame index j (InstrBase lookup; 0 when
    /// the window carries no instruction addressing).
    let instrBaseAt (w: FlameWindow) (j: int) : int64 =
        match w.InstrBase with
        | Some arr when j >= 0 && j < arr.Length -> arr[j]
        | _ -> 0L

    /// The window-frame index containing absolute entry index `i` (the last
    /// j with InstrBase[j] <= i). 0 when no instruction addressing exists.
    let frameIndexOfInstr (w: FlameWindow) (i: int64) : int =
        match w.InstrBase with
        | Some arr when arr.Length > 1 ->
            let rec search lo hi =
                if lo >= hi then
                    lo
                else
                    let mid = (lo + hi + 1) >>> 1

                    if arr[mid] <= i then search mid hi else search lo (mid - 1)

            search 0 (arr.Length - 2)
        | _ -> 0

    /// Index of the first rectangle that can overlap `tick`: PrefixMaxEnd is
    /// sorted, so the first entry exceeding the tick bounds every later one.
    let stab (w: FlameWindow) (tick: int64) : int =
        let a = w.PrefixMaxEnd

        let rec search lo hi =
            if lo > hi then
                lo
            else
                let mid = (lo + hi) >>> 1

                if a[mid] > tick then
                    search lo (mid - 1)
                else
                    search (mid + 1) hi

        search 0 (a.Length - 1)

    /// Last entry index with EntryTicks <= tick (-1 when none).
    let entryAtTick (w: FlameWindow) (tick: int64) : int =
        match w.EntryTicks with
        | Some ticks when ticks.Length > 0 ->
            let rec search lo hi =
                if lo > hi then
                    hi
                else
                    let mid = (lo + hi) >>> 1

                    if ticks[mid] <= tick then
                        search (mid + 1) hi
                    else
                        search lo (mid - 1)

            search 0 (ticks.Length - 1)
        | _ -> -1

    /// The tick frame `frame` completed at (relative ticks). Requires
    /// FirstFrame >= 0 and the frame inside the window.
    let endTickOfFrame (w: FlameWindow) (frame: int) : int64 =
        if
            w.FirstFrame < 0
            || frame < w.FirstFrame
            || frame - w.FirstFrame >= w.FrameTicks.Length
        then
            invalidOp (
                sprintf
                    "frame %d outside the flame window (first %d, %d boundaries)"
                    frame
                    w.FirstFrame
                    w.FrameTicks.Length
            )

        w.FrameTicks[frame - w.FirstFrame]

    /// The frame whose span contains `tick`, clamped into the window
    /// (-1 when the window has no frame mapping).
    let frameAtTick (w: FlameWindow) (tick: int64) : int =
        if w.FirstFrame < 0 || w.FrameTicks.Length = 0 then
            -1
        else
            let rec search lo hi =
                if lo > hi then
                    lo - 1
                else
                    let mid = (lo + hi) >>> 1

                    if w.FrameTicks[mid] <= tick then
                        search (mid + 1) hi
                    else
                        search lo (mid - 1)

            let idx = search 0 (w.FrameTicks.Length - 1)
            w.FirstFrame + (max 0 idx)

    /// The instruction-index range covered by window frames [f0, f1Exclusive)
    /// (clamped into the window).
    let instrRangeOfFrames (w: FlameWindow) (f0: int) (f1Exclusive: int) : int64 * int64 =
        let frameCount = max 0 w.FrameTicks.Length
        let a = max 0 f0
        let b = max a (min frameCount f1Exclusive)
        instrBaseAt w a, instrBaseAt w b

    /// The instruction-index range covered by the tick range [t0, t1]
    /// (frame-granular: a pixel's tick span becomes its frames' entry span -
    /// the address every seek/materialize request speaks).
    let instrRangeOfTicks (w: FlameWindow) (t0: int64) (t1: int64) : int64 * int64 =
        if w.FirstFrame < 0 || w.FrameTicks.Length = 0 then
            0L, 0L
        else
            let f0 = frameAtTick w t0
            let f1 = frameAtTick w (max t0 t1) + 1
            instrRangeOfFrames w f0 f1

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

        if n = 0 then
            { FlameWindow.empty with
                FirstFrame = firstFrame }
        else
            // Unwrap the uint32 cycle counter: ticks are non-decreasing except at
            // the wrap, where the raw value jumps backwards by (2^32 - progress).
            let ticks = Array.zeroCreate<int64> n
            let mutable offset = 0L
            let mutable prev = Int64.MinValue

            for i in 0 .. n - 1 do
                let raw = int64 entries[i].Tick

                if raw + offset < prev then
                    offset <- offset + 4294967296L

                let t = raw + offset
                ticks[i] <- t
                prev <- t

            let baseTick = ticks[0]

            for i in 0 .. n - 1 do
                ticks[i] <- ticks[i] - baseTick

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

                    if raw + off < prevF then
                        off <- off + 4294967296L

                    let t = raw + off
                    arr[j] <- max 0L (t - int64 entries[0].Tick)
                    prevF <- t

                arr

            let windowEnd =
                let lastEntryEnd = ticks[n - 1] + int64 entries[n - 1].Cycles

                if frameTicks64.Length > 0 then
                    max lastEntryEnd frameTicks64[frameTicks64.Length - 1]
                else
                    lastEntryEnd

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

                    if endTick > top.EndTick then
                        top.EndTick <- endTick

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

                        if f.EndTick > parent.EndTick then
                            parent.EndTick <- f.EndTick

                        if f.EndIndex > parent.EndIndex then
                            parent.EndIndex <- f.EndIndex

            // Frames still open at the window end (JP-out exits, window cut
            // mid-call) close there - the dashed-right-edge case.
            while stack.Count > 1 do
                let f = stack[stack.Count - 1]
                stack.RemoveAt(stack.Count - 1)

                if f.EndTick < windowEnd then
                    f.EndTick <- windowEnd

                if f.EndIndex < n then
                    f.EndIndex <- n

                finished.Add f

            let rects =
                // The label set matches the private Frame record above, so the
                // constructor must be qualified or F# binds the wrong record.
                [| for f in finished ->
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
                         EndIndex = f.EndIndex } |]

            Array.sortInPlaceBy (fun (r: FlameRect) -> r.StartTick, r.Depth) rects
            let prefixMax = Array.zeroCreate<int64> rects.Length
            let mutable runningMax = 0L

            for i in 0 .. rects.Length - 1 do
                if rects[i].EndTick > runningMax then
                    runningMax <- rects[i].EndTick

                prefixMax[i] <- runningMax

            let mutable depth = 1

            for r in rects do
                if r.Depth + 1 > depth then
                    depth <- r.Depth + 1

            // Instruction addressing: entries executed before each window frame
            // (merge-scan of entries against the boundary table).
            let instrBase =
                if frameTicks64.Length = 0 then
                    None
                else
                    let arr = Array.zeroCreate<int64> (frameTicks64.Length + 1)
                    let mutable e = 0

                    for j in 0 .. frameTicks64.Length - 1 do
                        let boundary = frameTicks64[j]

                        while e < n && ticks[e] < boundary do
                            e <- e + 1

                        arr[j + 1] <- int64 e

                    Some arr

            { Rects = rects
              PrefixMaxEnd = prefixMax
              FrameTicks = frameTicks64
              FirstFrame = firstFrame
              BaseTick = baseTick
              EndTick = max windowEnd 1L
              MaxDepth = depth
              Entries = Some entries
              EntryTicks = Some ticks
              FrameSummaries = None
              InstrBase = instrBase }

/// Zoom-independent column model for the flame graph (the LOD pyramid). The
/// window is bucketed into one cell per frame (level 0); each cell holds its
/// dominant function (by covered T-states) and how much of the cell that
/// function covers. Coarser levels halve the cell count. Painting at any
/// zoom then costs O(visible cells) instead of O(rectangles) - a
/// multi-hour trace paints exactly like a two-second one. Pure arrays, no
/// WPF types: the web client renders the same runs on a canvas.
module FlameLod =

    /// One pyramid level: granularity 2^Shift frames per cell, row-major over
    /// depths (index = depth * Cols + cell). Parallel arrays, Fable-friendly.
    type Level =
        {
            Shift: int
            Cols: int
            /// Dominant entry address per cell (-1 = uncovered).
            Funcs: int[]
            /// T-states the dominant function covers inside each cell.
            Dominated: int64[]
            /// T-states covered by any function inside each cell (0 = idle).
            Covered: int64[]
        }

    type Lod =
        { Levels: Level[] // levels[0] = one cell per frame
          FrameTicks: int64[]
          EndTick: int64 }

    let empty: Lod =
        { Levels = [||]
          FrameTicks = [||]
          EndTick = 1L }

    /// Frame cell a tick falls into: how many boundaries are <= tick.
    let private cellOf (frameTicks: int64[]) (t: int64) : int =
        let rec search lo hi =
            if lo > hi then
                lo
            else
                let mid = (lo + hi) >>> 1

                if frameTicks[mid] <= t then
                    search (mid + 1) hi
                else
                    search lo (mid - 1)

        min frameTicks.Length (search 0 (frameTicks.Length - 1) + 1)

    /// The tick range covered by one cell of `level`.
    let cellTickRange (lod: Lod) (level: Level) (cell: int) : int64 * int64 =
        let first = cell <<< level.Shift
        let lastExcl = min lod.FrameTicks.Length ((cell + 1) <<< level.Shift)
        let startT = if first <= 0 then 0L else lod.FrameTicks[first - 1]

        let endT =
            if lastExcl >= lod.FrameTicks.Length then
                lod.EndTick
            else
                lod.FrameTicks[lastExcl - 1]

        startT, max startT endT

    /// The level-`shift` cell containing `tick`.
    let cellAt (lod: Lod) (shift: int) (t: int64) : int = (cellOf lod.FrameTicks t) >>> shift

    /// Finest level whose cells are at least `minPx` wide at `ppTick`.
    let pickLevel (lod: Lod) (ppTick: float) (minPx: float) : int =
        let rec go k =
            if k >= lod.Levels.Length - 1 then
                k
            else
                let a, b = cellTickRange lod lod.Levels[k] 0
                if float (b - a) * ppTick >= minPx then k else go (k + 1)

        go 0

    /// Build the pyramid from a walked window: O(rects * overlapped cells)
    /// once for level 0, then linear per coarser level. Rects arrive sorted by
    /// start and same-depth invocations are sequential (one stack), so the
    /// streaming per-cell max yields the true dominant function.
    let build (w: FlameWindow) : Lod =
        if w.FrameTicks.Length = 0 || w.MaxDepth = 0 then
            empty
        else
            let cols0 = w.FrameTicks.Length
            let depthMax = w.MaxDepth
            let size = cols0 * depthMax
            let funcs = Array.create size -1
            let dom = Array.zeroCreate<int64> size
            let cov = Array.zeroCreate<int64> size

            for r in w.Rects do
                let len = r.EndTick - r.StartTick

                if len > 0L && r.Depth < depthMax then
                    let mutable c = cellOf w.FrameTicks r.StartTick
                    let cEnd = min (cols0 - 1) (cellOf w.FrameTicks (max r.StartTick (r.EndTick - 1L)))

                    while c <= cEnd do
                        let cStart = if c = 0 then 0L else w.FrameTicks[c - 1]
                        let cStop = w.FrameTicks[c]
                        let o = min r.EndTick cStop - max r.StartTick cStart

                        if o > 0L then
                            let idx = r.Depth * cols0 + c
                            cov[idx] <- cov[idx] + o

                            if funcs[idx] = int r.Entry then
                                dom[idx] <- dom[idx] + o
                            elif o > dom[idx] then
                                dom[idx] <- o
                                funcs[idx] <- int r.Entry

                        c <- c + 1
            // Coarser levels: pair cells up; the dominant side carries over (same
            // -function time sums, otherwise the larger side - an approximation
            // that only mis-ranks cells where three functions interleave evenly).
            let levels = ResizeArray<Level>()

            let rec addLevel (shift: int) (cols: int) (f: int[]) (d: int64[]) (cv: int64[]) =
                levels.Add
                    { Shift = shift
                      Cols = cols
                      Funcs = f
                      Dominated = d
                      Covered = cv }

                if cols > 1 then
                    let nc = (cols + 1) / 2
                    let nf = Array.create (nc * depthMax) -1
                    let nd = Array.zeroCreate<int64> (nc * depthMax)
                    let ncv = Array.zeroCreate<int64> (nc * depthMax)

                    for depth in 0 .. depthMax - 1 do
                        for j in 0 .. nc - 1 do
                            let dst = depth * nc + j
                            let baseIdx = depth * cols
                            let i0 = baseIdx + (j * 2)
                            // An odd cell count leaves the last pair short one cell:
                            // treat the missing half as uncovered (-1 / 0).
                            let hasSecond = (j * 2 + 1) < cols
                            let f0, d0, c0 = f[i0], d[i0], cv[i0]

                            let f1, d1, c1 =
                                if hasSecond then
                                    f[baseIdx + (j * 2 + 1)], d[baseIdx + (j * 2 + 1)], cv[baseIdx + (j * 2 + 1)]
                                else
                                    -1, 0L, 0L

                            let t = c0 + c1
                            ncv[dst] <- t

                            if t = 0L then
                                ()
                            elif f0 = f1 then
                                nf[dst] <- f0
                                nd[dst] <- d0 + d1
                            elif f1 < 0 then
                                nf[dst] <- f0
                                nd[dst] <- d0
                            elif f0 < 0 then
                                nf[dst] <- f1
                                nd[dst] <- d1
                            elif d0 >= d1 then
                                nf[dst] <- f0
                                nd[dst] <- d0
                            else
                                nf[dst] <- f1
                                nd[dst] <- d1

                    addLevel (shift + 1) nc nf nd ncv

            addLevel 0 cols0 funcs dom cov

            { Levels = levels.ToArray()
              FrameTicks = w.FrameTicks
              EndTick = w.EndTick }

    /// Coverage bucket for painting: -1 idle, else 0-3 by the dominant
    /// function's share of the cell (3 = >= 75%, full color).
    let inline bucket (level: Level) (idx: int) : int =
        let c = level.Covered[idx]

        if c <= 0L then
            -1
        else
            let share = float level.Dominated[idx] / float c

            if share >= 0.75 then 3
            elif share >= 0.5 then 2
            elif share >= 0.25 then 1
            else 0

    /// Run-length encode the visible cells of one depth row at one level:
    /// adjacent cells merge while function and coverage bucket match. Returns
    /// parallel arrays (funcs, buckets, cellStarts, cellLens) - the painter
    /// turns each run into one rectangle, so drawn geometry is bounded by
    /// pixels, not by the number of invocations in the trace.
    let runs (lod: Lod) (level: Level) (depth: int) (cellFrom: int) (cellTo: int) : int[] * int[] * int[] * int[] =
        let cellTo = min cellTo level.Cols
        let funcs = ResizeArray<int>()
        let buckets = ResizeArray<int>()
        let starts = ResizeArray<int>()
        let lens = ResizeArray<int>()
        let mutable runFunc = -2
        let mutable runBucket = -2
        let mutable runStart = cellFrom
        let mutable c = cellFrom

        while c < cellTo do
            let idx = depth * level.Cols + c
            let f = level.Funcs[idx]
            let b = bucket level idx

            if f <> runFunc || b <> runBucket then
                if runFunc <> -2 then
                    funcs.Add runFunc
                    buckets.Add runBucket
                    starts.Add runStart
                    lens.Add(c - runStart)

                runFunc <- f
                runBucket <- b
                runStart <- c

            c <- c + 1

        if runFunc <> -2 && cellTo > runStart then
            funcs.Add runFunc
            buckets.Add runBucket
            starts.Add runStart
            lens.Add(cellTo - runStart)

        funcs.ToArray(), buckets.ToArray(), starts.ToArray(), lens.ToArray()

/// Live per-frame flame summaries: the same synthetic call stack the
/// FlameWalker walks, maintained incrementally by the recording session so
/// every completed frame can append one FrameSummary to the timeline. Shares
/// the CallOps classifier (and Miner's-policy quirks: an RST always pushes,
/// a taken RET never pops the synthetic root) so tracked summaries agree
/// with walked windows over the same frames - the summary parity test pins
/// this. Cost per instruction is O(open stack depth), typically 2-4.
type CallTracker() =

    let maxDepth = FlameWalker.maxDepth

    // Current function per stack level; level 0 is the synthetic root. Levels
    // stay occupied across frame boundaries (a CALL open at the boundary
    // resumes in the next frame's summary).
    let levelEntry = Array.create maxDepth 0
    let levelCov = Array.zeroCreate<int64> maxDepth
    let mutable openLevels = 1
    let mutable maxSeen = 1
    let mutable instrCount = 0

    // Flushed (depth, entry, coverage) spans of the current frame: a level
    // whose function changed mid-frame contributes one span per occupant.
    let spansDepth = ResizeArray<int>()
    let spansEntry = ResizeArray<int>()
    let spansCov = ResizeArray<int64>()

    let flush (level: int) =
        if levelCov[level] > 0L then
            spansDepth.Add level
            spansEntry.Add levelEntry[level]
            spansCov.Add levelCov[level]
            levelCov[level] <- 0L

    let push (entry: int) =
        if openLevels < maxDepth then
            levelEntry[openLevels] <- entry
            openLevels <- openLevels + 1

            if openLevels > maxSeen then
                maxSeen <- openLevels

    /// Reset the per-frame spans (stack levels persist across frames).
    member _.BeginFrame() =
        spansDepth.Clear()
        spansEntry.Clear()
        spansCov.Clear()
        instrCount <- 0
        maxSeen <- openLevels

    /// Feed one recorded instruction (interrupt markers included) with the
    /// T-states it consumed - the same entries the FlameWalker would walk.
    member _.Feed(e: TraceEntry, cycles: int) =
        instrCount <- instrCount + 1
        let c = int64 cycles

        for lv in 0 .. openLevels - 1 do
            levelCov[lv] <- levelCov[lv] + c

        let b0 = e.B0

        if e.Length = 0uy then
            push (int e.Pc) // interrupt marker: the ISR frame opens here
        elif CallOps.isCall b0 && e.Taken = 1uy then
            push (int e.Target)
        elif CallOps.isRst b0 then
            push (int e.Target)
        elif CallOps.isRet b0 e.B1 && e.Taken = 1uy && openLevels > 1 then
            openLevels <- openLevels - 1
            flush openLevels

    /// Close the frame: flush every open level's span and aggregate one
    /// dominant entry per depth. The tracker stays primed for the next frame.
    member _.EndFrame() : FrameSummary =
        for lv in 0 .. openLevels - 1 do
            flush lv

        let depthMax = max 1 maxSeen
        let levels = Array.zeroCreate<FrameSummaryLevel> depthMax
        // Per-depth dominant sweep: entries at one depth are few, so a linear
        // rescan over a small span list beats a dictionary per depth.
        let work = Array.zeroCreate<int64> 64
        let workEntry = Array.zeroCreate<int> 64
        let mutable workN = 0

        for d in 0 .. depthMax - 1 do
            workN <- 0

            for s in 0 .. spansDepth.Count - 1 do
                if spansDepth[s] = d then
                    let entry = spansEntry[s]

                    let mutable i = 0

                    while i < workN && workEntry[i] <> entry do
                        i <- i + 1

                    if i = workN then
                        if workN < work.Length then
                            workEntry[workN] <- entry
                            work[workN] <- spansCov[s]
                            workN <- workN + 1
                        else
                            // >64 distinct functions at one depth in a frame is
                            // pathological; fold into the first slot's coverage.
                            work[0] <- work[0] + spansCov[s]
                    else
                        work[i] <- work[i] + spansCov[s]

            if workN > 0 then
                let mutable best = 1L
                let mutable bi = 0

                for i in 0 .. workN - 1 do
                    if work[i] > best then
                        best <- work[i]
                        bi <- i

                levels[d] <-
                    { Entry = uint16 workEntry[bi]
                      Coverage = uint32 (min best (int64 UInt32.MaxValue)) }

        let instr =
            if instrCount > 65535 then 65535us else uint16 instrCount

        let endDepth =
            if openLevels > 255 then 255uy else uint8 openLevels

        { InstrCount = instr
          EndDepth = endDepth
          Levels = levels }

/// Zoomed-out painting straight from FrameSummaries: frames aggregate into
/// fixed-size blocks (8 px wide at full zoom-out is the default), and each
/// block shows one dominant function per stack depth - no emulation, no
/// caching: one pass over the visible frames per repaint, bounded by the
/// viewport's block count x frames-per-block.
module SummaryBlocks =

    /// Frames per block so a block paints ~`targetPx` wide at `framePx`.
    let blockWidth (framePx: float) (targetPx: float) =
        if framePx <= 0.0 then 1 else max 1 (int (Math.Round(targetPx / framePx)))

    /// Dominant (entry, its coverage, total coverage) per depth for the frame
    /// slice [f0, f0+count). Returns one struct per depth (0..depthMax-1);
    /// entry -1 marks an uncovered depth.
    let blockDominant (summaries: FrameSummary[]) (f0: int) (count: int) : struct (int * int64 * int64)[] =
        // Reusable accumulator: (depth, entry) -> coverage.
        let acc = Dictionary<int * int, int64>()
        let hi = min summaries.Length (f0 + count)

        for f in f0 .. hi - 1 do
            let levels = summaries[f].Levels

            for d in 0 .. levels.Length - 1 do
                if levels[d].Coverage > 0u then
                    let key = (d, int levels[d].Entry)

                    acc[key] <-
                        match acc.TryGetValue key with
                        | true, v -> v + int64 levels[d].Coverage
                        | _ -> int64 levels[d].Coverage

        let mutable depthMax = 1

        for kv in acc do
            if (fst kv.Key) + 1 > depthMax then
                depthMax <- (fst kv.Key) + 1

        let zero = struct (-1, 0L, 0L)
        let result = Array.create depthMax zero
        let totals = Array.zeroCreate<int64> depthMax
        let bests = Array.create depthMax -1L
        let bestEntries = Array.create depthMax -1

        for kv in acc do
            let d, entry = kv.Key
            totals[d] <- totals[d] + kv.Value

            if kv.Value > bests[d] then
                bests[d] <- kv.Value
                bestEntries[d] <- entry

        for d in 0 .. depthMax - 1 do
            result[d] <- struct (bestEntries[d], bests[d], totals[d])

        result

    /// Coverage bucket of a dominant cell (FlameLod.bucket thresholds).
    let bucket (dominated: int64) (covered: int64) : int =
        if covered <= 0L then -1
        else
            let share = float dominated / float covered

            if share >= 0.75 then 3
            elif share >= 0.5 then 2
            elif share >= 0.25 then 1
            else 0

    /// One paint pass over the visible block range [blockFrom, blockTo):
    /// per depth, RLE runs of (entry, bucket) over blocks - parallel arrays
    /// shaped exactly like FlameLod.runs, so the painter treats both regimes
    /// uniformly. Geometry is bounded by viewport pixels x depth rows; the
    /// dominant sweep runs ONCE per block and is shared across depths, so a
    /// paint costs O(sum of block frame counts) dictionary work total.
    let runsAllDepths
        (summaries: FrameSummary[])
        (blockFrames: int)
        (depthMax: int)
        (blockFrom: int)
        (blockTo: int)
        : (int[] * int[] * int[] * int[])[]
        =
        let bf = max 1 blockFrames
        let blocks = max 0 (blockTo - blockFrom)
        let depths = max 1 depthMax

        let outs =
            Array.init depths (fun _ ->
                (ResizeArray<int>(), ResizeArray<int>(), ResizeArray<int>(), ResizeArray<int>()))

        let runFunc = Array.create depths -2
        let runBucket = Array.create depths -2
        let runStart = Array.create depths blockFrom

        for b in blockFrom .. blockFrom + blocks - 1 do
            let dominant = blockDominant summaries (b * bf) bf

            for d in 0 .. depths - 1 do
                let struct (entry, dominated, covered) =
                    if d < dominant.Length then dominant[d] else struct (-1, 0L, 0L)

                let bk = bucket dominated covered

                if entry <> runFunc[d] || bk <> runBucket[d] then
                    if runFunc[d] <> -2 then
                        let fs, bs, ss, ls = outs[d]
                        fs.Add runFunc[d]
                        bs.Add runBucket[d]
                        ss.Add runStart[d]
                        ls.Add(b - runStart[d])

                    runFunc[d] <- entry
                    runBucket[d] <- bk
                    runStart[d] <- b

        Array.init depths (fun d ->
            if runFunc[d] <> -2 && blockFrom + blocks > runStart[d] then
                let fs, bs, ss, ls = outs[d]
                fs.Add runFunc[d]
                bs.Add runBucket[d]
                ss.Add runStart[d]
                ls.Add(blockFrom + blocks - runStart[d])

            let fs, bs, ss, ls = outs[d]
            fs.ToArray(), bs.ToArray(), ss.ToArray(), ls.ToArray())
