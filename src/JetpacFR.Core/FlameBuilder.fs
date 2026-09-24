namespace JetpacFR.Core

open System
open System.Collections.Generic
open Jetpac2.Core
open Jetpac3.Core

/// One warm gap chain: the anchor's stored frame, a private machine parked at
/// the end of the emulated run, and the states re-derived so far
/// (States[i] = the state after frame Anchor + 1 + i).
type private MaterializerChain =
    { Anchor: int
      Port: Machine
      States: ResizeArray<string * byte[] * byte[]>
      mutable LastUse: int64 }

/// Re-derives gap frames of a sparse timeline: the nearest stored state at or
/// before the target, then forward emulation (applying each frame's logged
/// key events - the replay convention) on a cached private machine. Per-gap
/// chains stay warm, so stepping frame by frame through a gap after the first
/// touch is a lookup, not an emulation. UI-thread only (no locking): the
/// flame builders run their own continuous machines.
type TimelineMaterializer(timeline: unit -> StateTimeline, keyLog: unit -> KeyLog, ?capacity: int) =

    let capacity = max 1 (defaultArg capacity 6)
    let buffer = Array.zeroCreate<byte> 0x10000

    let chains = Dictionary<int, MaterializerChain>()
    let mutable uses = 0L

    do Jetpac2.Core.Z80Table.EnsureInstalled()

    let ensureChain (anchor: int) =
        match chains.TryGetValue anchor with
        | true, c ->
            c
        | _ ->
            let port = Machine()
            // Parity with the live session: lifted routines run here too, so
            // re-derived states match what was recorded.
            port.Override <- LiftedRoutines.registryHook

            let text, keys = (timeline ()).RestoreInto(anchor, buffer)
            port.LoadState(buffer, text)
            port.Keyboard.Load keys

            let chain =
                { Anchor = anchor
                  Port = port
                  States = ResizeArray()
                  LastUse = uses }

            chains[anchor] <- chain

            while chains.Count > capacity do
                let mutable oldestKey = -1
                let mutable oldestUse = Int64.MaxValue

                for kv in chains do
                    if kv.Value.LastUse < oldestUse then
                        oldestUse <- kv.Value.LastUse
                        oldestKey <- kv.Key

                // Never evict the chain being built (it may be the only one).
                if oldestKey >= 0 && oldestKey <> anchor then
                    chains.Remove oldestKey |> ignore
                else
                    ()

            chain

    /// Drop every chain (the timeline was reloaded, branched or cleared).
    member _.Invalidate() = chains.Clear()

    /// (state text, memory copy, keyboard copy) of the machine state AFTER
    /// `frame` - the stored state when the timeline has one, re-derived from
    /// the nearest anchor otherwise. Throws when `frame` lies outside the
    /// recording.
    member _.StateAfter(frame: int) : string * byte[] * byte[] =
        let tl = timeline ()

        if frame < tl.StartFrame || frame > tl.EndFrame then
            invalidOp (sprintf "frame %d outside the recording (%d..%d)" frame tl.StartFrame tl.EndFrame)
        elif tl.HasState frame then
            let mem = Array.zeroCreate<byte> 0x10000
            let text, keys = tl.RestoreInto(frame, mem)
            text, mem, keys
        else
            let anchor =
                match tl.NearestStateAtOrBefore frame with
                | Some a -> a
                | None -> invalidOp (sprintf "no stored state at or before frame %d" frame)

            uses <- uses + 1L
            let chain = ensureChain anchor
            chain.LastUse <- uses
            let port = chain.Port

            // Extend the chain forward to the requested frame: each frame's
            // logged keys apply before it runs (replay convention).
            while chain.States.Count < frame - anchor do
                let f = anchor + chain.States.Count + 1

                for e in (keyLog ()).ForFrame f do
                    port.SetKey(e.Row, e.Bit, e.Pressed)

                let frameEnd = port.FrameEnd

                while port.CycleCount() < frameEnd do
                    port.Step()

                port.FrameEnd <- frameEnd + 69888L

                let mem, text = port.SaveState()
                chain.States.Add(text, Array.copy mem, port.Keyboard.ToBytes())

            let text, mem, keys = chain.States[frame - anchor - 1]
            text, Array.copy mem, Array.copy keys

/// Headless flame-graph builder: re-executes a frame range of a saved
/// recording (the sparse per-frame timeline + key script) on a private port
/// machine, recording every instruction exactly like the live session does,
/// then walks the entries into a FlameWindow.
///
/// Determinism: the builder mirrors TraceSession.RunFrame's traced path -
/// same LiftedRoutines override hook, same key application (the replay
/// convention: a frame's logged key events apply before the frame runs),
/// same interrupt marker recording - so re-executed entries match what the
/// live session recorded for the same frames, byte for byte.
///
/// Gap-aware since the sparse cadence: the window executes CONTINUOUSLY from
/// the nearest stored state at-or-before `firstFrame - 1` - gap frames are
/// re-derived by simply running on (the materializer parity test pins the
/// re-derived states to the stored ones byte for byte).
///
/// Frames run from `firstFrame` (>= StartFrame + 1: executing frame f needs
/// the state after f - 1; frame 0's predecessor state does not exist in a
/// recording) through `lastFrame`.
module FlameBuilder =

    /// Run one frame on `port`, appending one TraceEntry per executed
    /// instruction (interrupt markers included) - the traced path of the live
    /// session. Advances FrameEnd past the completed frame.
    let private runTracedFrame (port: Machine) (entries: ResizeArray<TraceEntry>) =
        let frameEnd = port.FrameEnd

        while port.CycleCount() < frameEnd do
            let irq = port.IrqPending && port.Iff1
            let cyclesBefore = port.CycleCount()

            if irq then
                // One Step services the interrupt AND runs the vector
                // instruction. Record a synthetic marker (Length = 0) plus the
                // vector instruction under its own pc - exactly like the live
                // session's recorder path.
                let vector =
                    match port.IrqMode with
                    | 2 ->
                        let addr = 0xFF ||| ((port.Regs.I() <<< 8) &&& 0xFF00)
                        let lo = int port.Memory[addr &&& 0xFFFF]
                        let hi = int port.Memory[(addr + 1) &&& 0xFFFF]
                        (lo ||| (hi <<< 8)) &&& 0xFFFF
                    | _ -> 0x38

                entries.Add
                    { Pc = uint16 vector
                      B0 = 0uy
                      B1 = 0uy
                      B2 = 0uy
                      B3 = 0uy
                      Target = uint16 vector
                      Tick = uint32 cyclesBefore
                      Length = 0uy
                      Cycles = 7uy
                      FlagsBefore = 0uy
                      FlagsAfter = 0uy
                      Taken = 1uy }

                let vlen = Disasm.disasmLength port.Memory vector
                let m = port.Memory
                let vb0 = m[vector &&& 0xFFFF]
                let vb1 = m[(vector + 1) &&& 0xFFFF]
                let vb2 = m[(vector + 2) &&& 0xFFFF]
                let vb3 = m[(vector + 3) &&& 0xFFFF]
                port.Step()
                let after = port.Regs.Pc()
                let cycles = int (port.CycleCount() - cyclesBefore)

                entries.Add
                    { Pc = uint16 vector
                      B0 = vb0
                      B1 = vb1
                      B2 = vb2
                      B3 = vb3
                      Target = uint16 after
                      Tick = uint32 cyclesBefore
                      Length = uint8 vlen
                      Cycles = uint8 (min 255 cycles)
                      FlagsBefore = 0uy
                      FlagsAfter = 0uy
                      Taken = (if after <> ((vector + vlen) &&& 0xFFFF) then 1uy else 0uy) }
            else
                let pc = port.Regs.Pc()
                let len = Disasm.disasmLength port.Memory pc
                let m = port.Memory
                let b0 = m[pc &&& 0xFFFF]
                let b1 = m[(pc + 1) &&& 0xFFFF]
                let b2 = m[(pc + 2) &&& 0xFFFF]
                let b3 = m[(pc + 3) &&& 0xFFFF]
                port.Step()
                let after = port.Regs.Pc()
                let cycles = int (port.CycleCount() - cyclesBefore)

                entries.Add
                    { Pc = uint16 pc
                      B0 = b0
                      B1 = b1
                      B2 = b2
                      B3 = b3
                      Target = uint16 after
                      Tick = uint32 cyclesBefore
                      Length = uint8 len
                      Cycles = uint8 (min 255 cycles)
                      FlagsBefore = 0uy
                      FlagsAfter = 0uy
                      Taken = (if after <> ((pc + len) &&& 0xFFFF) then 1uy else 0uy) }

        port.FrameEnd <- frameEnd + 69888L

    /// Run one frame without recording (the live bare loop): warm-up frames
    /// between a materializer anchor and the window's first frame.
    let private runBareFrame (port: Machine) =
        let frameEnd = port.FrameEnd

        while port.CycleCount() < frameEnd do
            port.Step()

        port.FrameEnd <- frameEnd + 69888L

    /// Build the flame window for frames [firstFrame .. lastFrame]. Returns
    /// None when the range is not covered by the timeline or the build was
    /// cancelled. `progress` reports frames completed (on the caller's
    /// thread - run this on a worker and marshal if needed).
    let buildFrames
        (timeline: StateTimeline)
        (keyLog: KeyLog)
        (firstFrame: int)
        (lastFrame: int)
        (progress: int -> unit)
        (cancel: unit -> bool)
        : FlameWindow option =
        if lastFrame < firstFrame then
            Some
                { FlameWindow.empty with
                    FirstFrame = firstFrame }
        elif firstFrame <= timeline.StartFrame || lastFrame > timeline.EndFrame then
            None
        elif cancel () then
            None
        else
            Jetpac2.Core.Z80Table.EnsureInstalled()
            let port = Machine()
            // Parity with the live session: lifted routines run here too, so
            // the re-executed instruction stream is the one that was recorded.
            port.Override <- LiftedRoutines.registryHook
            let entries = ResizeArray<TraceEntry>(1 <<< 16)
            let frameBounds = ResizeArray<uint32>(max 1 (lastFrame - firstFrame + 1))
            let summaries = ResizeArray<FrameSummary>(max 1 (lastFrame - firstFrame + 1))
            let tracker = CallTracker()
            let buffer = Array.zeroCreate<byte> 0x10000
            let mutable cancelled = false

            // Anchor: the nearest stored state at-or-before firstFrame - 1
            // (StartFrame is always stored, so this always resolves), then run
            // forward CONTINUOUSLY - the warm-up frames before firstFrame
            // execute bare, gap frames need no per-frame restore.
            let anchorFrame =
                match timeline.NearestStateAtOrBefore(firstFrame - 1) with
                | Some a -> a
                | None -> invalidOp "recording has no stored state at or before the window start"

            let text, keys = timeline.RestoreInto(anchorFrame, buffer)
            port.LoadState(buffer, text)
            port.Keyboard.Load keys

            let mutable f = anchorFrame + 1

            while not cancelled && f <= lastFrame do
                for e in keyLog.ForFrame f do
                    port.SetKey(e.Row, e.Bit, e.Pressed)

                if f >= firstFrame then
                    tracker.BeginFrame()
                    let before = entries.Count
                    runTracedFrame port entries

                    for i in before .. entries.Count - 1 do
                        tracker.Feed(entries[i], int entries[i].Cycles)

                    frameBounds.Add(uint32 (port.CycleCount()))
                    summaries.Add(tracker.EndFrame())
                    progress (f - firstFrame + 1)
                else
                    runBareFrame port

                f <- f + 1

                if cancel () then
                    cancelled <- true

            if cancelled then
                None
            else
                let w = FlameWalker.walk firstFrame (frameBounds.ToArray()) (entries.ToArray())

                Some
                    { w with
                        FrameSummaries = Some(summaries.ToArray()) }

    /// One-frame flame window (a "bundle") for on-demand detail: materialize
    /// the state before `frame` (gap-aware, chain-cached), execute the frame
    /// traced, walk it. Returns None when `frame` is outside the recording.
    let buildFrameBundle
        (timeline: StateTimeline)
        (mat: TimelineMaterializer)
        (keyLog: KeyLog)
        (frame: int)
        : FlameWindow option =
        if frame <= timeline.StartFrame || frame > timeline.EndFrame then
            None
        else
            Jetpac2.Core.Z80Table.EnsureInstalled()
            let port = Machine()
            port.Override <- LiftedRoutines.registryHook
            let text, mem, keys = mat.StateAfter(frame - 1)
            port.LoadState(mem, text)
            port.Keyboard.Load keys

            for e in keyLog.ForFrame frame do
                port.SetKey(e.Row, e.Bit, e.Pressed)

            let entries = ResizeArray<TraceEntry>(1 <<< 12)
            let tracker = CallTracker()
            tracker.BeginFrame()
            runTracedFrame port entries

            for i in 0 .. entries.Count - 1 do
                tracker.Feed(entries[i], int entries[i].Cycles)

            let bounds = [| uint32 (port.CycleCount()) |]
            let w = FlameWalker.walk frame bounds (entries.ToArray())

            Some
                { w with
                    FrameSummaries = Some [| tracker.EndFrame() |] }

    /// A display window over the WHOLE recording built purely from its
    /// summaries - zero emulation: synthetic 69888-tick frame boundaries (a
    /// few ticks off the real ones; frame-granular mapping is unaffected),
    /// the summary tier for the zoomed-out regime, and InstrBase prefix sums
    /// for the pixel -> instruction-range mapping. None for recordings
    /// without summaries (legacy loads keep the probe-build path).
    let overviewWindow (timeline: StateTimeline) : FlameWindow option =
        if timeline.FrameCount = 0 || timeline.SummaryCount <> timeline.FrameCount then
            None
        else
            let n = timeline.FrameCount
            let summaries = Array.init timeline.SummaryCount timeline.SummaryData
            let bounds = Array.init n (fun j -> int64 (j + 1) * 69888L)
            let instrBase = Array.zeroCreate<int64> (n + 1)

            for j in 0 .. n - 1 do
                instrBase[j + 1] <- instrBase[j] + int64 summaries[j].InstrCount

            let mutable depth = 1

            for s in summaries do
                if s.Levels.Length > depth then
                    depth <- s.Levels.Length

            Some
                { FlameWindow.empty with
                    FirstFrame = timeline.StartFrame
                    FrameTicks = bounds
                    EndTick = max 1L (int64 n * 69888L)
                    MaxDepth = depth
                    FrameSummaries = Some summaries
                    InstrBase = Some instrBase }

/// LRU-ish cache of built flame windows with the detail tier kept only for
/// small windows. Keyed by frame range; a query hit requires a window fully
/// covering [firstFrame .. lastFrame].
type FlameCache(?detailFrameBudget: int) =

    let detailBudget = defaultArg detailFrameBudget 8
    let windows = Dictionary<int * int, FlameWindow>()
    let order = ResizeArray<int * int>()
    let capacity = 8

    member _.Clear() =
        windows.Clear()
        order.Clear()

    member _.TryGet(firstFrame: int, lastFrame: int) : FlameWindow option =
        let mutable hit = None

        for kv in windows do
            let (a, b) = kv.Key

            if a <= firstFrame && lastFrame <= b then
                hit <- Some kv.Value

        match hit with
        | Some w ->
            // Refresh LRU position.
            let key = (w.FirstFrame, w.FirstFrame + w.FrameTicks.Length - 1)
            order.Remove key |> ignore
            order.Add key
            Some w
        | None -> None

    member _.Add(window: FlameWindow) : FlameWindow =
        if window.FirstFrame < 0 then
            window
        else
            let key = (window.FirstFrame, window.FirstFrame + window.FrameTicks.Length - 1)

            let stored =
                if window.FrameTicks.Length <= detailBudget then
                    window
                else
                    FlameWindow.trim window

            if not (windows.ContainsKey key) then
                order.Add key

            windows[key] <- stored

            while order.Count > capacity do
                let oldest = order[0]
                order.RemoveAt 0
                windows.Remove oldest |> ignore

            stored

/// LRU cache of per-frame bundle windows (single-frame FlameWindows from
/// buildFrameBundle): the on-demand detail the deep-zoom and click paths
/// materialize. Keyed by frame number, so bundles stay valid across zoom and
/// pan - unlike the range-keyed FlameCache, the viewport never invalidates
/// them. The host clears it on session switches.
type BundleCache(?capacity: int) =

    let capacity = max 1 (defaultArg capacity 256)
    let bundles = Dictionary<int, FlameWindow>()
    let order = ResizeArray<int>()

    member _.Clear() =
        bundles.Clear()
        order.Clear()

    member _.TryGet(frame: int) : FlameWindow option =
        match bundles.TryGetValue frame with
        | true, w ->
            order.Remove frame |> ignore
            order.Add frame
            Some w
        | _ -> None

    member _.Add(frame: int, window: FlameWindow) : FlameWindow =
        if window.FirstFrame < 0 then
            window
        else
            if not (bundles.ContainsKey frame) then
                order.Add frame

            bundles[frame] <- window

            while order.Count > capacity do
                let oldest = order[0]
                order.RemoveAt 0
                bundles.Remove oldest |> ignore

            window
