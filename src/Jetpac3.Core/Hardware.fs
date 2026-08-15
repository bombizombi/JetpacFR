namespace Jetpac3.Core

/// Shared Core-facing aliases for the copied machine boundary. The copied
/// Jetpac2 machine remains the implementation; Jetpac3 owns the contract used
/// by traces, diagnostics, and later lifted dispatch.
type HardwareEvent = Jetpac2.Core.HardwareEvent
type MemoryWriteEvent = Jetpac2.Core.MemoryWriteEvent

type HardwareTrace =
  { PortEvents: HardwareEvent list
    MemoryWrites: MemoryWriteEvent list }

module HardwareTrace =
  let empty = { PortEvents = []; MemoryWrites = [] }
  let merge left right =
    { PortEvents = left.PortEvents @ right.PortEvents
      MemoryWrites = left.MemoryWrites @ right.MemoryWrites }

/// Immutable frame payload for clients. Pixels are BGRA and events retain
/// absolute emulated timestamps; render cadence is not part of the contract.
type FrameRender =
  { Frame: int
    StartTState: int64
    EndTState: int64
    Pixels: byte[]
    Events: HardwareEvent list }

type TraceDifference =
  { Index: int
    Expected: HardwareEvent option
    Actual: HardwareEvent option }

module TimedEvents =
  let firstDifference (expected: HardwareEvent list) (actual: HardwareEvent list) =
    let rec loop index left right =
      match left, right with
      | [], [] -> None
      | e :: es, a :: ass when e = a -> loop (index + 1) es ass
      | e :: _, a :: _ -> Some { Index = index; Expected = Some e; Actual = Some a }
      | e :: _, [] -> Some { Index = index; Expected = Some e; Actual = None }
      | [], a :: _ -> Some { Index = index; Expected = None; Actual = Some a }
    loop 0 expected actual
