namespace JetpacFR.Core

/// Behavioural contract for a mined routine, extracted from the trace window
/// plus a memory image: aligned disassembly with execution counts, write
/// ranges (from the miner's write attribution), register deltas (first input
/// sample vs first exit sample), timing, resume addresses, and the Machine
/// API surface the routine touches.
module Contract =

  type ContractInsn =
    { Address: int
      Text: string
      Length: int
      Executions: int
      AvgCycles: int }

  type RoutineContract =
    { Entry: int
      SpanLo: int
      SpanHi: int
      CallCount: int
      Disassembly: ContractInsn list
      /// (lo, hi, lastValue, writeCount) from the miner.
      WriteRanges: (int * int * int * int) list
      /// (register name, before, after) of the first observed call.
      RegisterDelta: (string * int * int) list
      InclusiveTStates: int64
      Callsites: int list
      ResumeAddresses: int list
      InputSamples: RegSnapshot list
      ExitSamples: RegSnapshot list
      SelfModifying: bool
      ApiHints: string list }

  /// Coarse Machine-API surface hint for an opcode's first byte.
  let private apiHint (b0: byte) : string =
    let op = int b0
    match op with
    | 0xCB -> "Alu.rotate8 / bit / res / set"
    | 0xED -> "Alu block ops / RETI-RETN (IFF flags)"
    | 0xDD | 0xFD -> "Regs.Get/Set (IX/IY, displacement via ReadImm)"
    | 0xD3 | 0xDB -> "Out / In (port I/O)"
    | op when op >= 0xC0 && op <= 0xFF ->
      let low = op &&& 0x0F
      if op = 0xC9 || low = 0 || low = 8 then "Pop16 + Regs.SetPc (return)"
      elif low = 4 || low = 0xC then "Push16 (return addr) + Regs.SetPc (call)"
      elif op = 0xC3 || low = 2 || low = 0xA then "Regs.SetPc (jump)"
      elif low = 1 then "Pop16"
      elif low = 5 then "Push16"
      elif low = 6 || low = 0xE then "Alu.* (immediate) + SetFlags"
      elif low = 7 || low = 0xF then "Push16 (RST return addr) + Regs.SetPc"
      else "Regs.SetPc / Push16 / Pop16"
    | op when op >= 0x80 && op <= 0xBF -> "Alu.add8/sub8/cmp8/and8/or8/xor8 + SetFlags"
    | op when op >= 0x40 && op <= 0x7F ->
      if op = 0x76 then "HALT (PassTime)"
      elif ((op >>> 3) &&& 7) = 6 || (op &&& 7) = 6 then "Read/Write (memory via (HL))"
      else "Regs.Get/Set (R8 pairs)"
    | 0x34 | 0x35 -> "Alu.inc8/dec8 (memory via (HL))"
    | 0x36 -> "Write (LD (HL),n)"
    | 0x02 | 0x0A | 0x12 | 0x1A | 0x22 | 0x2A | 0x32 | 0x3A -> "Read/Write (memory via (nn))"
    | 0x10 | 0x18 | 0x20 | 0x28 | 0x30 | 0x38 -> "Branch (relative)"
    | _ -> "Fetch/ReadImm (immediates), Regs.Get/Set (R16/R8)"

  let private regNames =
    [ "AF"; "BC"; "DE"; "HL"; "AF'"; "BC'"; "DE'"; "HL'"; "IX"; "IY"; "SP"; "I"; "R" ]

  let private regOf (s: RegSnapshot) (name: string) : int =
    match name with
    | "AF" -> int s.Af
    | "BC" -> int s.Bc
    | "DE" -> int s.De
    | "HL" -> int s.Hl
    | "AF'" -> int s.Af2
    | "BC'" -> int s.Bc2
    | "DE'" -> int s.De2
    | "HL'" -> int s.Hl2
    | "IX" -> int s.Ix
    | "IY" -> int s.Iy
    | "SP" -> int s.Sp
    | "I" -> int s.I
    | "R" -> int s.R
    | _ -> 0

  /// Extract the contract. `memory` is the memory image to disassemble the
  /// span from (normally the port's current memory; for self-modifying code
  /// it may differ from what executed). `pcCycles` is the per-PC total cycle
  /// count over the trace window (int64[65536]) used for averages.
  let extract (memory: byte[]) (trace: Trace) (r: Miner.Routine) (pcCycles: int64[]) : RoutineContract =
    // Aligned linear disassembly of the span.
    let disasm = ResizeArray<ContractInsn>()
    let mutable addr = r.SpanLo
    while addr <= r.SpanHi && disasm.Count < 512 do
      let insn = Disasm.disasmMemory memory addr
      let executions =
        if addr < trace.PerPcCount.Length then trace.PerPcCount[addr] else 0
      let avgCycles =
        if addr < pcCycles.Length && executions > 0 then
          int (pcCycles[addr] / int64 executions)
        else 0
      disasm.Add
        { Address = addr
          Text = insn.Text
          Length = insn.Length
          Executions = executions
          AvgCycles = avgCycles }
      addr <- (addr + insn.Length) &&& 0xFFFF

    let registerDelta =
      match r.InputSamples, r.ExitSamples with
      | input :: _, exit :: _ ->
        [ for name in regNames do
            let before = regOf input name
            let after = regOf exit name
            if after <> before then yield (name, before, after) ]
      | _ -> []

    let apiHints =
      disasm
      |> Seq.map (fun i -> apiHint memory[i.Address &&& 0xFFFF])
      |> Seq.distinct
      |> Seq.toList

    { Entry = r.Entry
      SpanLo = r.SpanLo
      SpanHi = r.SpanHi
      CallCount = r.CallCount
      Disassembly = disasm |> Seq.toList
      WriteRanges = r.WriteRanges
      RegisterDelta = registerDelta
      InclusiveTStates = r.InclusiveTStates
      Callsites = r.CallSites |> List.map fst |> List.distinct
      ResumeAddresses = r.CallSites |> List.map snd |> List.distinct
      InputSamples = r.InputSamples
      ExitSamples = r.ExitSamples
      SelfModifying = r.SelfModifying
      ApiHints = apiHints }
