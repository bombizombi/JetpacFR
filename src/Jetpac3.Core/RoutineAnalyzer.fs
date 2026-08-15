namespace Jetpac3.Core

/// Behavioural contract extracted by tracing the oracle through one routine.
type RoutineReport =
  { Entries: int list
    TStates: int64
    /// Coalesced equal-value write ranges: (startAddr, endAddr, value).
    WriteRanges: (int * int * int) list
    /// 16-bit registers that changed: (name, before, after).
    RegisterDelta: (string * int * int) list
    /// True if the routine rewrote its own instruction bytes during the trace.
    SelfModifying: bool }
/// Machine-readable contract consumed by lifted dispatch and review tools.
type RoutineContract =
  { EntryAddresses: int list
    ResumeAddresses: int list
    ExitBehavior: string
    Preconditions: string list
    WriteRanges: (int * int * int) list
    RegisterDelta: (string * int * int) list
    TimingTStates: int64
    EventOffsets: (int64 * string) list
    SelfModifying: bool
    ImplementationMode: ExecutionMode }

/// Trace-based routine analysis: no static disassembler. Restores the entry
/// checkpoint, single-steps until PC enters a target address, snapshots, steps
/// until the routine returns (PC leaves [rangeLo, rangeHi]), then diffs.
module RoutineAnalyzer =

  let private registerSnapshot (z80: Jetpac.Core.Z80) : (string * int) list =
    let r = z80.Regs
    [ "AF", r.Get Jetpac.Core.R16.AF
      "BC", r.Get Jetpac.Core.R16.BC
      "DE", r.Get Jetpac.Core.R16.DE
      "HL", r.Get Jetpac.Core.R16.HL
      "IX", r.Ix()
      "IY", r.Iy()
      "SP", r.Sp()
      "PC", r.Pc()
      "I", r.I()
      "R", r.R() ]

  let private diffRegisters (before: (string * int) list) (after: (string * int) list) =
    let afterMap = Map.ofList after
    [ for (name, v0) in before do
        match Map.tryFind name afterMap with
        | Some v1 when v1 <> v0 -> yield (name, v0, v1)
        | _ -> () ]

  let private coalesceWrites (before: byte[]) (after: byte[]) : (int * int * int) list =
    let mutable ranges = []
    let mutable i = 0
    while i < before.Length do
      if before.[i] <> after.[i] then
        let v = after.[i]
        let start = i
        let mutable j = i
        while j < before.Length && before.[j] <> after.[j] && after.[j] = v do
          j <- j + 1
        ranges <- (start, j - 1, int v) :: ranges
        i <- j
      else
        i <- i + 1
    ranges |> List.rev

  /// `entries`: the PC addresses that begin the routine; `rangeLo`/`rangeHi`:
  /// the full instruction-address extent (including internal loop addresses).
  let analyze (oracle: Jetpac.Core.Spectrum48) (entries: int list) (rangeLo: int) (rangeHi: int) : RoutineReport =
    let z80 = oracle.DebugZ80
    let mutable entered = -1
    let mutable guard = 0
    while entered < 0 && guard < 5000000 do
      z80.ExecuteOne()
      guard <- guard + 1
      if List.contains (z80.Regs.Pc()) entries then entered <- z80.Regs.Pc()
    if entered < 0 then failwith "analyze: never reached a target entry address"

    let memBefore = Array.copy oracle.Memory
    let regsBefore = registerSnapshot z80
    let cyclesBefore = int64 (z80.CycleCount())

    let mutable inside = true
    let mutable steps = 0
    while inside && steps < 500000 do
      z80.ExecuteOne()
      steps <- steps + 1
      let pc = z80.Regs.Pc()
      inside <- pc >= rangeLo && pc <= rangeHi

    let memAfter = Array.copy oracle.Memory
    let regsAfter = registerSnapshot z80
    let cyclesAfter = int64 (z80.CycleCount())

    let selfModifying =
      let mutable sm = false
      let mutable a = rangeLo
      while a <= rangeHi && not sm do
        if memBefore.[a] <> memAfter.[a] then sm <- true
        a <- a + 1
      sm

    { Entries = entries
      TStates = cyclesAfter - cyclesBefore
      WriteRanges = coalesceWrites memBefore memAfter
      RegisterDelta = diffRegisters regsBefore regsAfter
      SelfModifying = selfModifying }
  let toContract (report: RoutineReport) : RoutineContract =
    { EntryAddresses = report.Entries
      ResumeAddresses = report.Entries
      ExitBehavior = "return or PC leaving analyzed range"
      Preconditions = []
      WriteRanges = report.WriteRanges
      RegisterDelta = report.RegisterDelta
      TimingTStates = report.TStates
      EventOffsets = []
      SelfModifying = report.SelfModifying
      ImplementationMode = OracleGenerated }
