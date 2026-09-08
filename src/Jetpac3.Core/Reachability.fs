namespace Jetpac3.Core

open System
open System.Collections.Generic
open System.Threading

type DecisionKind =
    | ConditionalBranch
    | ExternalInput
    | IndirectTarget
    | Return
    | FrameBoundary
    | ConfiguredAddress

type ExplorationPolicy =
    { MaxInstructions: int
      MaxTStates: int64
      ConfiguredAddresses: Set<int> }

module ExplorationPolicy =
    let safe =
        { MaxInstructions = 100_000
          MaxTStates = 10_000_000L
          ConfiguredAddresses = Set.empty }

type ExplorationNode =
    { Id: string
      Pc: int
      TState: int64
      InstructionCount: int
      Status: string }

type ExplorationEdge =
    { From: string
      ToPc: int
      TState: int64
      Kind: DecisionKind
      ExternalInput: bool }

type ExplorationGraph =
    { Nodes: ExplorationNode list
      Edges: ExplorationEdge list
      Failure: string option }

module ExplorationGraph =
    let merge left right =
        { Nodes = (left.Nodes @ right.Nodes) |> List.distinctBy (fun node -> node.Id)
          Edges = (left.Edges @ right.Edges) |> List.distinct
          Failure =
            match left.Failure, right.Failure with
            | Some failure, _ -> Some failure
            | None, failure -> failure }


module ConcreteExplorer =
    let private classify (instruction: DecodedInstruction) =
        match instruction.Bytes |> Array.toList with
        | 0xDBuy :: _ -> ExternalInput
        | 0xE9uy :: _ -> IndirectTarget
        | 0xC9uy :: _ -> Return
        | _ when instruction.Successors.Length > 1 -> ConditionalBranch
        | _ -> ConfiguredAddress

    let explore
        (machine: Jetpac2.Core.Machine)
        (cache: CodeCache)
        (policy: ExplorationPolicy)
        (cancel: CancellationToken)
        : ExplorationGraph =
        let nodes = ResizeArray<ExplorationNode>()
        let edges = ResizeArray<ExplorationEdge>()
        let visited = HashSet<string>()
        let startTState = machine.CycleCount()
        let mutable failure: string option = None
        let mutable count = 0

        while failure.IsNone
              && count < policy.MaxInstructions
              && machine.CycleCount() - startTState < policy.MaxTStates
              && not cancel.IsCancellationRequested do
            let pc = machine.Regs.Pc()
            let tstate = machine.CycleCount()
            let instruction = cache.Decode machine.Memory pc
            let id = sprintf "%04X:%d" pc tstate

            if visited.Add id then
                nodes.Add
                    { Id = id
                      Pc = pc
                      TState = tstate
                      InstructionCount = count
                      Status = "visited" }

            for successor in instruction.Successors do
                if
                    successor <> ((pc + instruction.Length) &&& 0xFFFF)
                    || policy.ConfiguredAddresses.Contains successor
                then
                    let kind = classify instruction

                    edges.Add
                        { From = id
                          ToPc = successor
                          TState = tstate
                          Kind = kind
                          ExternalInput = kind = ExternalInput }

            try
                machine.Step()
            with ex ->
                failure <- Some ex.Message

            count <- count + 1

        let failure =
            if cancel.IsCancellationRequested then
                Some "exploration cancelled"
            elif failure.IsSome then
                failure
            elif count >= policy.MaxInstructions then
                Some "instruction limit reached"
            elif machine.CycleCount() - startTState >= policy.MaxTStates then
                Some "T-state limit reached"
            else
                None

        { Nodes = nodes |> Seq.toList
          Edges = edges |> Seq.toList
          Failure = failure }
