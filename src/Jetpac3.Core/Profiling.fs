namespace Jetpac3.Core

/// Persistable observation for candidate ranking. Missing observations remain
/// explicit rather than being inferred from static code.
type RoutineProfile =
  { EntryAddress: int
    CallCount: int
    InclusiveTStates: int64
    ExclusiveTStates: int64
    ReturnSites: int list
    RegisterDelta: (string * int * int) list
    ReadRanges: (int * int) list
    WriteRanges: (int * int * int) list
    HardwareEvents: HardwareEvent list
    Interrupts: int
    SelfModifying: bool
    OverlappingEntry: bool }

type CandidateRisk =
  | Green
  | Yellow
  | Orange
  | Red

type LiftCandidate =
  { EntryAddress: int
    Score: int
    Risk: CandidateRisk
    Reasons: string list
    Profile: RoutineProfile }

module RoutineProfiler =
  let fromReport entry (report: RoutineReport) =
    { EntryAddress = entry
      CallCount = 1
      InclusiveTStates = report.TStates
      ExclusiveTStates = report.TStates
      ReturnSites = []
      RegisterDelta = report.RegisterDelta
      ReadRanges = []
      WriteRanges = report.WriteRanges
      HardwareEvents = []
      Interrupts = 0
      SelfModifying = report.SelfModifying
      OverlappingEntry = report.Entries.Length > 1 }

  let rank (profiles: RoutineProfile list) : LiftCandidate list =
    profiles
    |> List.map (fun profile ->
      let mutable score = min 100 (profile.CallCount * 10)
      let reasons = ResizeArray<string>()
      let mutable risk = Green
      if profile.SelfModifying then
        risk <- Red
        reasons.Add "self-modifying"
        score <- score - 80
      if profile.OverlappingEntry then
        risk <- max risk Orange
        reasons.Add "overlapping entry"
        score <- score - 30
      if profile.HardwareEvents.Length > 0 then
        risk <- max risk Yellow
        reasons.Add "hardware timing"
        score <- score - 15
      { EntryAddress = profile.EntryAddress
        Score = max 0 score
        Risk = risk
        Reasons = reasons |> Seq.toList
        Profile = profile })
    |> List.sortByDescending (fun candidate -> candidate.Score)
