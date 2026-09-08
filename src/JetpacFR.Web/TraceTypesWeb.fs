/// WEB SHELL SHIM (JetpacFR.Web): verbatim copy of the Fable-clean part of
/// JetpacFR.Core/Trace.fs (trace types, ring-buffer recorder, snapshot query).
/// The Core file is not compiled by the web project because its TraceCodec
/// module uses System.IO + Marshal/fixed, which Fable cannot compile; the
/// shell provides a byte-compatible codec in TraceCodecWeb.fs instead.
/// KEEP IN SYNC with JetpacFR.Core/Trace.fs (Core is authoritative).

namespace JetpacFR.Core

open System

/// One executed instruction in the trace window. Bytes are the bytes the
/// machine actually executed (self-modifying code included); Tick is the
/// absolute machine cycle before the instruction ran.
[<Struct>]
type TraceEntry =
    { Pc: uint16
      B0: uint8
      B1: uint8
      B2: uint8
      B3: uint8
      Target: uint16
      Tick: uint32
      Length: uint8
      Cycles: uint8
      FlagsBefore: uint8
      FlagsAfter: uint8
      Taken: uint8 }

/// Periodic full register snapshot (every 64 instructions by default),
/// time-ordered so the cinema can binary-search the state nearest any entry.
[<Struct>]
type RegSnapshot =
    { Tick: uint32
      Af: uint16
      Bc: uint16
      De: uint16
      Hl: uint16
      Af2: uint16
      Bc2: uint16
      De2: uint16
      Hl2: uint16
      Ix: uint16
      Iy: uint16
      Sp: uint16
      Pc: uint16
      I: uint8
      R: uint8 }

/// A memory byte change (fires only when the value actually changes).
[<Struct>]
type MemWriteEvent =
    { Tick: uint32
      Address: uint16
      OldValue: uint8
      NewValue: uint8 }

/// A port OUT (beeper, border, tape, ...).
[<Struct>]
type PortEvent =
    { Tick: uint32
      Port: uint16
      Value: uint8
      Border: uint8 }

/// Linearized trace window: entries in execution order plus side streams.
type Trace =
    { Entries: TraceEntry[]
      Snapshots: RegSnapshot[]
      Writes: MemWriteEvent[]
      Ports: PortEvent[]
      FrameTicks: uint32[]
      PerPcCount: int[]
      SelfModified: bool[]
      SelfModCount: int
      FirstIndexAtPc: int[]
      StartTick: uint32
      EndTick: uint32 }

/// Circular-buffer recorder. Zero allocation per instruction: entries land in
/// preallocated arrays; only write/port side streams (sparse) use a
/// ResizeArray. When the ring is full the oldest entry is evicted and the
/// per-PC and per-segment counters are decremented, so the heatmap and the
/// density strip always describe the current window.
type TraceRecorder(capacity: int, segmentCount: int) =

    let entries = Array.zeroCreate<TraceEntry> capacity
    let snapshots = Array.zeroCreate<RegSnapshot> (capacity / 64 + 1)
    let writes = ResizeArray<MemWriteEvent>(min capacity 65536)
    let ports = ResizeArray<PortEvent>(min capacity 65536)
    let frameTicks = ResizeArray<uint32>(4096)
    let perPc = Array.zeroCreate<int> 0x10000
    let segments = Array.zeroCreate<int> (max 1 segmentCount)

    let segmentSize =
        max 1 ((capacity + (max 1 segmentCount) - 1) / (max 1 segmentCount))

    let selfModified = Array.zeroCreate<bool> 0x10000
    let mutable selfModCount = 0
    let mutable head = 0
    let mutable count = 0
    let mutable snapshotCount = 0
    let mutable recordEnabled = true
    let mutable startTick = 0u
    let mutable endTick = 0u

    do
        if capacity < 1 then
            invalidArg (nameof capacity) "capacity must be positive"

    member this.EntryCount = count
    member this.Capacity = capacity
    member this.PerPcCount = perPc
    member this.SegmentCounts = segments
    member this.SegmentCount = segments.Length
    member this.SelfModified = selfModified
    member this.SelfModCount = selfModCount
    member this.StartTick = startTick
    member this.EndTick = endTick

    member this.RecordEnabled
        with get () = recordEnabled
        and set v = recordEnabled <- v

    member this.Record(entry: TraceEntry) =
        if recordEnabled then
            if count = capacity then
                let old = entries[head]
                perPc[int old.Pc] <- perPc[int old.Pc] - 1
                segments[head / segmentSize] <- segments[head / segmentSize] - 1
            else
                count <- count + 1

            if count = 1 then
                startTick <- entry.Tick

            entries[head] <- entry
            perPc[int entry.Pc] <- perPc[int entry.Pc] + 1
            segments[head / segmentSize] <- segments[head / segmentSize] + 1
            endTick <- entry.Tick
            head <- (head + 1) % capacity
            // Once the ring is full the oldest entry (now at head) defines the
            // window's start; without this refresh StartTick stays pinned to the
            // first tick ever recorded. (Sync: JetpacFR.Core/Trace.fs)
            if count = capacity then
                startTick <- entries[head].Tick

    member this.RecordSnapshot(s: RegSnapshot) =
        if recordEnabled then
            // One snapshot per 64 instructions keeps arriving long after the slot
            // budget is gone, so when full, drop the oldest half: snapshots stay
            // time-ordered and the nearest-before search stays valid. (Sync:
            // JetpacFR.Core/Trace.fs)
            if snapshotCount = snapshots.Length then
                let keep = snapshots.Length / 2
                Array.blit snapshots (snapshots.Length - keep) snapshots 0 keep
                snapshotCount <- keep

            snapshots[snapshotCount] <- s
            snapshotCount <- snapshotCount + 1

    member this.RecordWrite(w: MemWriteEvent) =
        if recordEnabled then
            writes.Add w
            let addr = int w.Address
            // A write into an address that has already executed is a
            // self-modification (the executed-then-written marking the cache
            // invalidation used to provide).
            if perPc[addr] > 0 && not selfModified[addr] then
                selfModified[addr] <- true
                selfModCount <- selfModCount + 1

    member this.RecordPort(p: PortEvent) =
        if recordEnabled then
            ports.Add p

    member this.RecordFrameBoundary(tick: uint32) =
        if recordEnabled then
            frameTicks.Add tick

    /// Linearize the ring into a fresh Trace. O(window) copy; call on pause or
    /// before save, not per cursor move.
    member this.Build() : Trace =
        let n = count
        let linear = Array.zeroCreate<TraceEntry> n

        if n > 0 then
            if n < capacity then
                Array.Copy(entries, 0, linear, 0, n)
            else
                Array.Copy(entries, head, linear, 0, capacity - head)
                Array.Copy(entries, 0, linear, capacity - head, head)

        let snaps = Array.sub snapshots 0 snapshotCount
        let firstAt = Array.create 0x10000 -1

        for i in 0 .. n - 1 do
            let pc = int linear[i].Pc

            if firstAt[pc] < 0 then
                firstAt[pc] <- i

        { Entries = linear
          Snapshots = snaps
          Writes = writes.ToArray()
          Ports = ports.ToArray()
          FrameTicks = frameTicks.ToArray()
          PerPcCount = Array.copy perPc
          SelfModified = Array.copy selfModified
          SelfModCount = selfModCount
          FirstIndexAtPc = firstAt
          StartTick = startTick
          EndTick = endTick }

module TraceQuery =

    /// Nearest snapshot index with Tick <= target (snapshots are time-ordered).
    let nearestSnapshotBefore (snapshots: RegSnapshot[]) (target: uint32) : int =
        let rec search lo hi =
            if lo > hi then
                hi
            else
                let mid = (lo + hi) >>> 1

                if snapshots[mid].Tick <= target then
                    search (mid + 1) hi
                else
                    search lo (mid - 1)

        search 0 (snapshots.Length - 1)
