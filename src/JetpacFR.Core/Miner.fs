namespace JetpacFR.Core

open System.Collections.Generic

/// Dynamic function mining from a trace: CALL/RET back-chaining over a single
/// interrupt-aware call stack. Approximations, stated:
/// - A function is a CALL target (or interrupt vector). Its span is the union
///   of all observed call extents; entries inside another routine's span are
///   flagged as overlapping.
/// - JP-out exits (functions that leave without RET) desync a single stack;
///   the depth is capped so a runaway push cannot blow up, and whatever
///   frames remain open at the end of the trace are finalized anyway.
/// - Self-modifying = a recorded write landed on an address inside the
///   routine's span after that address had executed (the trace's
///   SelfModified array).
module Miner =

  type Routine =
    { Entry: int
      SpanLo: int
      SpanHi: int
      CallCount: int
      InclusiveTStates: int64
      ExclusiveTStates: int64
      /// (callsite pc, return pc), capped at 64 entries for display.
      CallSites: (int * int) list
      /// (backedge pc, loop target, iteration count), hot first.
      LoopExtents: (int * int * int) list
      /// (lo, hi, lastValue, writeCount) coalesced over consecutive addresses
      /// written while this routine's frame was on the stack.
      WriteRanges: (int * int * int * int) list
      /// Input register states at entry, one per distinct call site (max 5).
      InputSamples: RegSnapshot list
      /// Register states near each return (max 5), paired with InputSamples
      /// for register-delta extraction.
      ExitSamples: RegSnapshot list
      SelfModifying: bool
      Overlapping: bool
      Score: int
      Risk: string
      Reasons: string list }

  type CallEdge =
    { Caller: int
      Callee: int
      Count: int }

  let private isCallOp = CallOps.isCall

  let private isRetOp = CallOps.isRet

  type private Frame =
    { Entry: int
      mutable SpanLo: int
      mutable SpanHi: int
      mutable Exclusive: int64
      mutable Inclusive: int64 }

  type private Accum =
    { Entry: int
      mutable SpanLo: int
      mutable SpanHi: int
      mutable CallCount: int
      mutable Inclusive: int64
      mutable Exclusive: int64
      CallSites: ResizeArray<int * int>
      DistinctSites: HashSet<int>
      Loops: Dictionary<int * int, int>
      Samples: ResizeArray<RegSnapshot>
      SampleTicks: HashSet<uint32>
      Exits: ResizeArray<RegSnapshot>
      ExitTicks: HashSet<uint32>
      /// address -> (write count, last value)
      Writes: Dictionary<int, int * int> }

  /// Mine the trace window. Returns (routines, call edges) sorted by lift
  /// score, hot first. A caller of -1 means "called with an empty stack"
  /// (top-level / initial execution).
  let mine (trace: Trace) : Routine list * CallEdge list =
    let accums = Dictionary<int, Accum>()
    let edges = Dictionary<int * int, int>()
    let stack = ResizeArray<Frame>()
    let maxDepth = 128

    let accumOf (entry: int) =
      match accums.TryGetValue entry with
      | true, a -> a
      | _ ->
        let a =
          { Entry = entry
            SpanLo = entry
            SpanHi = entry
            CallCount = 0
            Inclusive = 0L
            Exclusive = 0L
            CallSites = ResizeArray()
            DistinctSites = HashSet<int>()
            Loops = Dictionary<int * int, int>()
            Samples = ResizeArray()
            SampleTicks = HashSet<uint32>()
            Exits = ResizeArray()
            ExitTicks = HashSet<uint32>()
            Writes = Dictionary<int, int * int>() }
        accums[entry] <- a
        a

    let entries = trace.Entries
    let mutable w = 0
    let mutable i = 0
    while i < entries.Length do
      let e = entries[i]
      // Attribute memory writes since the previous entry to the frame that
      // executed them: the current top. CALL/RET instructions never write
      // memory, so this entry's own stack change cannot misattribute them.
      while w < trace.Writes.Length && trace.Writes[w].Tick < e.Tick do
        if stack.Count > 0 then
          let a = accumOf stack[stack.Count - 1].Entry
          let addr = int trace.Writes[w].Address
          let value = int trace.Writes[w].NewValue
          let count =
            match a.Writes.TryGetValue addr with
            | true, (c, _) -> c
            | _ -> 0
          a.Writes[addr] <- (count + 1, value)
        w <- w + 1
      if e.Length = 0uy then
        // Interrupt marker: the ISR becomes its own frame (its RET pops it).
        if stack.Count < maxDepth then
          let entry = int e.Pc
          stack.Add
            { Entry = entry
              SpanLo = entry
              SpanHi = entry
              Exclusive = 0L
              Inclusive = 0L }
      else
        if stack.Count > 0 then
          let top = stack[stack.Count - 1]
          top.SpanLo <- min top.SpanLo (int e.Pc)
          top.SpanHi <- max top.SpanHi (int e.Pc + int e.Length - 1)
          top.Exclusive <- top.Exclusive + int64 e.Cycles
        let b0 = e.B0
        if isCallOp b0 && e.Taken = 1uy then
          let callee = int e.Target
          let returnPc = (int e.Pc + int e.Length) &&& 0xFFFF
          let caller = if stack.Count > 0 then stack[stack.Count - 1].Entry else -1
          let key = caller, callee
          edges[key] <- (match edges.TryGetValue key with | true, v -> v | _ -> 0) + 1
          let a = accumOf callee
          a.CallCount <- a.CallCount + 1
          if a.CallSites.Count < 64 then a.CallSites.Add(int e.Pc, returnPc)
          if a.DistinctSites.Add(int e.Pc) && a.Samples.Count < 5 then
            let si = TraceQuery.nearestSnapshotBefore trace.Snapshots e.Tick
            if si >= 0 && a.SampleTicks.Add trace.Snapshots[si].Tick then
              a.Samples.Add trace.Snapshots[si]
          if stack.Count < maxDepth then
            stack.Add
              { Entry = callee
                SpanLo = callee
                SpanHi = callee
                Exclusive = 0L
                Inclusive = 0L }
        elif isRetOp b0 e.B1 && e.Taken = 1uy then
          if stack.Count > 0 then
            let f = stack[stack.Count - 1]
            stack.RemoveAt(stack.Count - 1)
            let a = accumOf f.Entry
            a.SpanLo <- min a.SpanLo f.SpanLo
            a.SpanHi <- max a.SpanHi f.SpanHi
            a.Exclusive <- a.Exclusive + f.Exclusive
            let childIncl = f.Inclusive + f.Exclusive
            a.Inclusive <- a.Inclusive + childIncl
            if stack.Count > 0 then
              stack[stack.Count - 1].Inclusive <-
                stack[stack.Count - 1].Inclusive + childIncl
            let si = TraceQuery.nearestSnapshotBefore trace.Snapshots e.Tick
            if si >= 0 && a.ExitTicks.Add trace.Snapshots[si].Tick && a.Exits.Count < 5 then
              a.Exits.Add trace.Snapshots[si]
        elif e.Taken = 1uy && int e.Target < int e.Pc && stack.Count > 0 then
          // Taken backward branch: one more loop iteration.
          let f = stack[stack.Count - 1]
          let a = accumOf f.Entry
          let key = int e.Pc, int e.Target
          a.Loops[key] <- (match a.Loops.TryGetValue key with | true, v -> v | _ -> 0) + 1
      i <- i + 1

    // Finalize frames still open (non-RET exits).
    while stack.Count > 0 do
      let f = stack[stack.Count - 1]
      stack.RemoveAt(stack.Count - 1)
      let a = accumOf f.Entry
      a.SpanLo <- min a.SpanLo f.SpanLo
      a.SpanHi <- max a.SpanHi f.SpanHi
      a.Exclusive <- a.Exclusive + f.Exclusive
      a.Inclusive <- a.Inclusive + f.Inclusive + f.Exclusive

    // Writes after the last entry.
    while w < trace.Writes.Length do
      if stack.Count > 0 then
        let a = accumOf stack[stack.Count - 1].Entry
        let addr = int trace.Writes[w].Address
        let value = int trace.Writes[w].NewValue
        let count =
          match a.Writes.TryGetValue addr with
          | true, (c, _) -> c
          | _ -> 0
        a.Writes[addr] <- (count + 1, value)
      w <- w + 1

    /// (lo, hi, lastValue, writeCount) coalesced over consecutive addresses.
    let writeRangesOf (a: Accum) : (int * int * int * int) list =
      a.Writes
      |> Seq.sortBy (fun kv -> kv.Key)
      |> Seq.fold (fun acc kv ->
        let addr = kv.Key
        let count, value = kv.Value
        match acc with
        | (lo, hi, v, c) :: rest when hi + 1 = addr && v = value -> (lo, addr, v, c + count) :: rest
        | _ -> (addr, addr, value, count) :: acc) []
      |> List.rev

    let riskNames = [| "green"; "yellow"; "orange"; "red" |]
    let routines =
      accums.Values
      |> Seq.filter (fun a ->
        // Stack-desync artifact: a frame entered by an interrupt whose handler
        // exits without a RET at the simulated top can swallow the rest of
        // the trace. Those have no CALL sites and an implausibly huge span.
        a.CallCount > 0 || a.SpanHi - a.SpanLo + 1 <= 0x1000)
      |> Seq.map (fun a ->
        let mutable score = min 100 (a.CallCount * 10)
        let reasons = ResizeArray<string>()
        let mutable risk = 0
        let selfMod =
          let mutable found = false
          let mutable addr = a.SpanLo
          while addr <= a.SpanHi && not found do
            if trace.SelfModified[addr &&& 0xFFFF] then found <- true
            addr <- addr + 1
          found
        if selfMod then
          risk <- max risk 3
          score <- score - 80
          reasons.Add "self-modifying"
        let overlapping =
          accums.Values
          |> Seq.exists (fun other -> other.Entry <> a.Entry && other.Entry >= a.SpanLo && other.Entry <= a.SpanHi)
        if overlapping then
          risk <- max risk 2
          score <- score - 30
          reasons.Add "overlapping entry"
        let span = a.SpanHi - a.SpanLo + 1
        if span > 0x400 then
          risk <- max risk 1
          score <- score - 10
          reasons.Add(sprintf "large span %04X-%04X" a.SpanLo a.SpanHi)
        { Entry = a.Entry
          SpanLo = a.SpanLo
          SpanHi = a.SpanHi
          CallCount = a.CallCount
          InclusiveTStates = a.Inclusive
          ExclusiveTStates = a.Exclusive
          CallSites = a.CallSites |> Seq.toList
          LoopExtents =
            [ for kv in a.Loops -> (fst kv.Key, snd kv.Key, kv.Value) ]
            |> List.sortByDescending (fun (_, _, c) -> c)
          WriteRanges = writeRangesOf a
          InputSamples = a.Samples |> Seq.toList
          ExitSamples = a.Exits |> Seq.toList
          SelfModifying = selfMod
          Overlapping = overlapping
          Score = max 0 score
          Risk = riskNames[risk]
          Reasons = reasons |> Seq.toList })
      |> Seq.sortByDescending (fun r -> r.Score, r.CallCount)
      |> Seq.toList

    let edgeList =
      let entriesSet = routines |> List.map (fun r -> r.Entry) |> Set.ofList
      [ for kv in edges -> { Caller = fst kv.Key; Callee = snd kv.Key; Count = kv.Value } ]
      |> List.filter (fun e -> entriesSet.Contains e.Callee)
      |> List.sortByDescending (fun e -> e.Count)

    routines, edgeList
