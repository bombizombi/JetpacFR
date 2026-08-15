namespace Jetpac3.Core

open System

/// Identity fields required to reproduce a checkpoint/timeline result.
type DiagnosticIdentity =
  { RomSha256: string
    TzxSha256: string
    EmulatorVersion: string
    LiftRegistryVersion: string
    CheckpointSchemaVersion: int
    ExecutionMode: ExecutionMode }

type DiagnosticArtifact =
  { Identity: DiagnosticIdentity
    CheckpointId: string
    Timeline: InputTimeline option
    StateHash: CanonicalStateHash option
    HardwareTrace: HardwareTrace
    Notes: string list }

module Diagnostics =
  let emptyIdentity mode =
    { RomSha256 = ""
      TzxSha256 = ""
      EmulatorVersion = "Jetpac4"
      LiftRegistryVersion = "screen-clear-1"
      CheckpointSchemaVersion = CheckpointSchema.CurrentVersion
      ExecutionMode = mode }

  let summarize artifact =
    let timeline =
      match artifact.Timeline with
      | Some value -> sprintf "timeline-events=%d" value.Events.Length
      | None -> "timeline-events=0"
    let hash =
      match artifact.StateHash with
      | Some value -> "state-hash=" + value.Full
      | None -> "state-hash=none"
    String.concat "\n"
      [ "checkpoint=" + artifact.CheckpointId
        "rom-sha256=" + artifact.Identity.RomSha256
        "tzx-sha256=" + artifact.Identity.TzxSha256
        "emulator=" + artifact.Identity.EmulatorVersion
        "lift-registry=" + artifact.Identity.LiftRegistryVersion
        sprintf "checkpoint-schema=%d" artifact.Identity.CheckpointSchemaVersion
        sprintf "execution-mode=%A" artifact.Identity.ExecutionMode
        timeline
        hash
        sprintf "hardware-events=%d" artifact.HardwareTrace.PortEvents.Length
        sprintf "memory-writes=%d" artifact.HardwareTrace.MemoryWrites.Length
        yield! artifact.Notes ]
