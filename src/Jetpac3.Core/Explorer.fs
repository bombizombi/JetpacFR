namespace Jetpac3.Core

open System

/// Stable identifier for a checkpoint in the explorer graph.
type CheckpointId = CheckpointId of string

module CheckpointId =
    let value (CheckpointId id) = id

    let create (id: string) =
        if String.IsNullOrWhiteSpace id then
            invalidArg (nameof id) "Checkpoint ID cannot be empty"

        CheckpointId id

/// Human-readable label attached to a branch edge.
type BranchLabel = BranchLabel of string

module BranchLabel =
    let value (BranchLabel label) = label

    let create (label: string) =
        BranchLabel(if isNull label then "" else label)

/// Timestamped physical/emulated input transition.
type InputEvent =
    { TState: int64
      Frame: int
      Row: int
      Bit: int
      Pressed: bool }

/// Replayable input sequence. T-state is authoritative; Frame is a display/index aid.
type InputTimeline =
    { Version: int
      RomSha256: string
      TzxSha256: string
      Events: InputEvent list }

module InputTimeline =
    let empty romSha256 tzxSha256 =
        { Version = 1
          RomSha256 = romSha256
          TzxSha256 = tzxSha256
          Events = [] }

    let append event timeline =
        { timeline with
            Events = timeline.Events @ [ event ] }

module InputTimelineCodec =
    let serialize timeline =
        let header =
            [ sprintf "version=%d" timeline.Version
              "rom=" + timeline.RomSha256
              "tzx=" + timeline.TzxSha256 ]

        let events =
            timeline.Events
            |> List.sortBy (fun event -> event.TState)
            |> List.map (fun event ->
                sprintf "event|%d|%d|%d|%d|%b" event.TState event.Frame event.Row event.Bit event.Pressed)

        String.concat "\n" (header @ events) + "\n"

    let parse (text: string) : InputTimeline =
        if isNull text then
            invalidArg (nameof text) "Timeline text cannot be null"

        let lines =
            text.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList

        let value key =
            lines
            |> List.tryPick (fun line ->
                let prefix = key + "="

                if line.StartsWith(prefix, StringComparison.Ordinal) then
                    Some(line.Substring(prefix.Length))
                else
                    None)

        let version = value "version" |> Option.map Int32.Parse |> Option.defaultValue 0

        if version <> 1 then
            failwithf "unsupported input timeline version %d" version

        let rom = value "rom" |> Option.defaultValue ""
        let tzx = value "tzx" |> Option.defaultValue ""

        let events =
            lines
            |> List.choose (fun line ->
                match line.Split('|') |> Array.toList with
                | [ "event"; t; frame; row; bit; pressed ] ->
                    Some
                        { TState = Int64.Parse t
                          Frame = Int32.Parse frame
                          Row = Int32.Parse row
                          Bit = Int32.Parse bit
                          Pressed = Boolean.Parse pressed }
                | _ -> None)

        { Version = version
          RomSha256 = rom
          TzxSha256 = tzx
          Events = events }

/// Which implementation owns execution for the current run.
type ExecutionMode =
    | OracleGenerated
    | Lifted
    | Differential

/// Boundary used by run/step commands.
type RunTarget =
    | OneFrame
    | Frames of int
    | UntilNextDecision
    | UntilReturn

/// Optional automatic checkpoint policy. Decision-point capture is supplied by
/// the concrete explorer; frame intervals are handled directly by Session.
type CheckpointCaptureOptions =
    { FrameInterval: int option
      CaptureDecisionPoints: bool }

/// First-start policy: the game remains playable while the oracle trace
/// collects new executable addresses until the configured target is reached.
type StartupExplorationPolicy =
    { NewExecutableAddresses: int
      MaxFrames: int }

/// Mutable-session presentation state. Machine state remains in the Core machine.
type ExplorerState =
    { SelectedCheckpoint: CheckpointId option
      CurrentCheckpoint: CheckpointId option
      Running: bool
      CurrentFrame: int
      CurrentTState: int64
      PendingInput: InputEvent list
      ActiveImplementation: ExecutionMode
      RunTarget: RunTarget option
      BranchLabel: BranchLabel option }

/// Graph metadata independent of WPF controls or rendered thumbnails.
type CheckpointMetadata =
    { Id: CheckpointId
      ParentId: CheckpointId option
      Children: CheckpointId list
      Branch: BranchLabel option
      Frame: int
      TState: int64
      Pc: int
      InputSummary: string
      Status: ExecutionMode }
