namespace Jetpac3.Core

open System
open System.Collections.Generic

/// Runtime instruction description. Bytes are copied at decode time so later
/// self-modifying writes cannot mutate an already-decoded record.
type DecodedInstruction =
    { Address: int
      Bytes: byte[]
      Length: int
      Mnemonic: string
      Successors: int list
      Cycles: int
      Dependencies: int list }

module DynamicDecoder =
    let private byteAt (memory: byte[]) address =
        int (Array.item (address &&& 0xFFFF) memory)

    let private wordAt (memory: byte[]) address =
        let low = byteAt memory address
        let high = byteAt memory (address + 1)
        (high <<< 8) ||| low

    let private signed value =
        if value >= 0x80 then value - 0x100 else value

    let private baseLength opcode =
        match opcode with
        | 0x01
        | 0x11
        | 0x21
        | 0x31
        | 0x22
        | 0x2A
        | 0x32
        | 0x3A
        | 0xC3
        | 0xCD
        | 0xC2
        | 0xC4
        | 0xCA
        | 0xCC
        | 0xD2
        | 0xD4
        | 0xDA
        | 0xDC
        | 0xE2
        | 0xE4
        | 0xEA
        | 0xEC
        | 0xF2
        | 0xF4
        | 0xFA
        | 0xFC -> 3
        | 0x10
        | 0x18
        | 0x20
        | 0x28
        | 0x30
        | 0x38
        | 0x06
        | 0x0E
        | 0x16
        | 0x1E
        | 0x26
        | 0x2E
        | 0x36
        | 0x3E
        | 0xC6
        | 0xCE
        | 0xD6
        | 0xDE
        | 0xD3
        | 0xDB
        | 0xE6
        | 0xEE
        | 0xF6
        | 0xFE -> 2
        | 0xCB -> 2
        | 0xED -> 2
        | 0xDD
        | 0xFD -> 2
        | _ -> 1

    let private instructionLength (memory: byte[]) address opcode =
        match opcode with
        | 0xDD
        | 0xFD ->
            let inner = byteAt memory (address + 1)
            if inner = 0xCB then 4 else 1 + baseLength inner
        | _ -> baseLength opcode

    let private targetFor (memory: byte[]) address length = wordAt memory (address + length - 2)

    let private relativeTarget (memory: byte[]) address length =
        (address + length + signed (byteAt memory (address + length - 1))) &&& 0xFFFF

    let private successors (memory: byte[]) address opcode length =
        let next = (address + length) &&& 0xFFFF

        let absoluteJumps =
            [ 0xC3
              0xC2
              0xC4
              0xCA
              0xCC
              0xD2
              0xD4
              0xDA
              0xDC
              0xE2
              0xE4
              0xEA
              0xEC
              0xF2
              0xF4
              0xFA
              0xFC ]

        let conditionalReturns = [ 0xC0; 0xC8; 0xC9; 0xD0; 0xD8; 0xE0; 0xE8; 0xF0; 0xF8 ]
        let relativeJumps = [ 0x10; 0x18; 0x20; 0x28; 0x30; 0x38 ]

        if List.contains opcode absoluteJumps then
            let target = targetFor memory address length
            if opcode = 0xC3 then [ target ] else [ next; target ]
        elif List.contains opcode relativeJumps then
            let target = relativeTarget memory address length
            if opcode = 0x18 then [ target ] else [ next; target ]
        elif List.contains opcode conditionalReturns then
            if opcode = 0xC9 then [] else [ next ]
        elif opcode = 0xCD then
            [ next; targetFor memory address length ]
        elif opcode = 0xE9 then
            [] // JP (HL)/(IX)/(IY): concrete target is runtime-only.
        else
            [ next ]

    let private cycleCount opcode =
        match opcode with
        | 0x00
        | 0x76 -> 4
        | 0x3E
        | 0x06
        | 0x0E
        | 0x16
        | 0x1E
        | 0x26
        | 0x2E -> 7
        | 0xC3
        | 0xCD -> 10
        | 0xC9 -> 10
        | 0x18 -> 12
        | 0x10 -> 13
        | _ -> 0

    let decode (memory: byte[]) (address: int) : DecodedInstruction =
        if isNull memory || memory.Length <> 0x10000 then
            invalidArg (nameof memory) "Decoder requires 65536 bytes"

        let entry = address &&& 0xFFFF
        let opcode = byteAt memory entry
        let length = instructionLength memory entry opcode |> min 4
        let bytes = Array.sub memory entry length

        { Address = entry
          Bytes = bytes
          Length = length
          Mnemonic = sprintf "DB %02X" opcode
          Successors = successors memory entry opcode length
          Cycles = cycleCount opcode
          Dependencies = [ for offset in 0 .. length - 1 -> (entry + offset) &&& 0xFFFF ] }

type DecodedBlock =
    { Entry: int
      Instructions: DecodedInstruction list
      Successors: int list
      TotalCycles: int
      Dependencies: int list
      Verified: bool }

module DynamicBlocks =
    let private ends (instruction: DecodedInstruction) =
        match instruction.Successors with
        | [ successor ] when successor = ((instruction.Address + instruction.Length) &&& 0xFFFF) -> false
        | _ -> true

    let decode (memory: byte[]) (entry: int) : DecodedBlock =
        let rec loop address instructions dependencies totalCycles steps =
            let instruction = DynamicDecoder.decode memory address
            let nextInstructions = instructions @ [ instruction ]
            let nextDependencies = dependencies @ instruction.Dependencies
            let nextCycles = totalCycles + instruction.Cycles

            if ends instruction || steps >= 255 then
                { Entry = entry &&& 0xFFFF
                  Instructions = nextInstructions
                  Successors = instruction.Successors
                  TotalCycles = nextCycles
                  Dependencies = nextDependencies |> List.distinct
                  Verified = nextCycles > 0 && nextInstructions |> List.forall (fun item -> item.Cycles > 0) }
            else
                loop
                    ((instruction.Address + instruction.Length) &&& 0xFFFF)
                    nextInstructions
                    nextDependencies
                    nextCycles
                    (steps + 1)

        loop (entry &&& 0xFFFF) [] [] 0 0

type BlockExecutionResult =
    { StartPc: int
      EndPc: int
      ActualTStates: int64
      ExpectedTStates: int
      EquivalentEntryFlow: bool }

module DynamicBlocksExecution =
    let execute (machine: Jetpac2.Core.Machine) (block: DecodedBlock) : BlockExecutionResult =
        let startPc = machine.Regs.Pc()
        let startTState = machine.CycleCount()
        let mutable equivalent = startPc = block.Entry

        for instruction in block.Instructions do
            if machine.Regs.Pc() <> instruction.Address then
                equivalent <- false

            if equivalent then
                machine.Step()

        { StartPc = startPc
          EndPc = machine.Regs.Pc()
          ActualTStates = machine.CycleCount() - startTState
          ExpectedTStates = block.TotalCycles
          EquivalentEntryFlow = equivalent }

/// Dependency-aware cache for lazily decoded runtime instructions.
type CodeCache() as self =
    let instructions = Dictionary<int, DecodedInstruction>()
    let dependents = Dictionary<int, HashSet<int>>()

    member this.Decode (memory: byte[]) (address: int) =
        let entry = address &&& 0xFFFF

        match instructions.TryGetValue entry with
        | true, instruction -> instruction
        | _ ->
            let instruction = DynamicDecoder.decode memory entry
            instructions[entry] <- instruction

            for dependency in instruction.Dependencies do
                let entries =
                    match dependents.TryGetValue dependency with
                    | true, existing -> existing
                    | _ ->
                        let created = HashSet<int>()
                        dependents[dependency] <- created
                        created

                entries.Add entry |> ignore

            instruction

    member this.Invalidate(address: int) =
        let address = address &&& 0xFFFF

        match dependents.TryGetValue address with
        | true, entries ->
            for entry in entries do
                instructions.Remove entry |> ignore

            dependents.Remove address |> ignore
        | _ -> ()

    member this.Clear() =
        instructions.Clear()
        dependents.Clear()

    member this.Count = instructions.Count

    member this.Attach(machine: Jetpac2.Core.Machine) =
        machine.AddMemoryWriteHandler(fun event -> self.Invalidate event.Address)
        machine.AddMemoryResetHandler(fun () -> self.Clear())
