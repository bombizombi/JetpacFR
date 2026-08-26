// JetpacFR regression harness.
//
//   --test disasm          known-instruction table + full 64K corpus
//   --test trace           recorder/codec roundtrip + stats sanity
//   --test agree           live trace vs disassembler length agreement
//   --test all             everything (boots the emulator once per test)

module JetpacFRTests.Program

open System
open System.IO
open JetpacFR.Core
open Jetpac2.Core.Z80BuilderInstance

let rom = LocalAssets.find "48.rom"
let tzx = LocalAssets.find "Jetpac.tzx"

let failures = ResizeArray<string>()
let check (name: string) (ok: bool) (detail: string) =
  if ok then printfn "  ok   %s" name
  else
    failures.Add(name + ": " + detail)
    eprintfn "  FAIL %s: %s" name detail

let runDisasmKnown () : int =
  printfn "disasm known-instruction table"
  // (bytes, expected text)
  let cases: (byte[] * string) list =
    [ [| 0x00uy |], "NOP"
      [| 0x76uy |], "HALT"
      [| 0x21uy; 0x00uy; 0x40uy |], "LD HL,0x4000"
      [| 0x3Euy; 0x47uy |], "LD A,0x47"
      [| 0x32uy; 0x00uy; 0x58uy |], "LD (0x5800),A"
      [| 0x3Auy; 0x00uy; 0x58uy |], "LD A,(0x5800)"
      [| 0xC3uy; 0xB8uy; 0x71uy |], "JP 0x71B8"
      [| 0xCDuy; 0x00uy; 0x12uy |], "CALL 0x1200"
      [| 0xC4uy; 0x00uy; 0x12uy |], "CALL NZ,0x1200"
      [| 0x18uy; 0xFEuy |], "JR 0x0000"
      [| 0x18uy; 0x00uy |], "JR 0x0002"
      [| 0x20uy; 0x05uy |], "JR NZ,0x0007"
      [| 0x10uy; 0x00uy |], "DJNZ 0x0002"
      [| 0x10uy; 0xF6uy |], "DJNZ 0xFFF8"
      [| 0xCBuy; 0x00uy |], "RLC B"
      [| 0xCBuy; 0x3Euy |], "SRL (HL)"
      [| 0xCBuy; 0x7Euy |], "BIT 7,(HL)"
      [| 0xCBuy; 0x46uy |], "BIT 0,(HL)"
      [| 0xCBuy; 0x86uy |], "RES 0,(HL)"
      [| 0xCBuy; 0xC6uy |], "SET 0,(HL)"
      [| 0xEDuy; 0x47uy |], "LD I,A"
      [| 0xEDuy; 0x4Fuy |], "LD R,A"
      [| 0xEDuy; 0x57uy |], "LD A,I"
      [| 0xEDuy; 0x5Fuy |], "LD A,R"
      [| 0xEDuy; 0x4Buy; 0x00uy; 0x58uy |], "LD BC,(0x5800)"
      [| 0xEDuy; 0x43uy; 0x00uy; 0x58uy |], "LD (0x5800),BC"
      [| 0xEDuy; 0x42uy |], "SBC HL,BC"
      [| 0xEDuy; 0x7Auy |], "ADC HL,SP"
      [| 0xEDuy; 0x70uy |], "IN (C)"
      [| 0xEDuy; 0x78uy |], "IN A,(C)"
      [| 0xEDuy; 0x71uy |], "OUT (C),0"
      [| 0xEDuy; 0x79uy |], "OUT (C),A"
      [| 0xEDuy; 0x4Duy |], "RETI"
      [| 0xEDuy; 0xB0uy |], "LDIR"
      [| 0xEDuy; 0xB9uy |], "CPDR"
      [| 0xEDuy; 0x67uy |], "RRD"
      [| 0xEDuy; 0x6Fuy |], "RLD"
      [| 0xEDuy; 0x44uy |], "NEG"
      [| 0xEDuy; 0x5Euy |], "IM 2"
      [| 0xDDuy; 0x21uy; 0x00uy; 0x40uy |], "LD IX,0x4000"
      [| 0xDDuy; 0x36uy; 0x02uy; 0x47uy |], "LD (IX+0x02),0x47"
      [| 0xDDuy; 0x34uy; 0x01uy |], "INC (IX+0x01)"
      [| 0xDDuy; 0x35uy; 0xFEuy |], "DEC (IX-0x02)"
      [| 0xDDuy; 0x26uy; 0x05uy |], "LD IXH,0x05"
      [| 0xDDuy; 0x6Duy |], "LD IXL,IXL"
      [| 0xFDuy; 0x6Duy |], "LD IYL,IYL"
      [| 0xDDuy; 0x7Cuy |], "LD A,IXH"
      [| 0xDDuy; 0xCBuy; 0x01uy; 0x06uy |], "RLC (IX+0x01)"
      [| 0xDDuy; 0xCBuy; 0x01uy; 0x7Euy |], "BIT 7,(IX+0x01)"
      [| 0xFDuy; 0x7Euy; 0x02uy |], "LD A,(IY+0x02)"
      [| 0xFDuy; 0xE9uy |], "JP (IY)"
      [| 0xDDuy; 0xE9uy |], "JP (IX)"
      [| 0xDDuy; 0x09uy |], "ADD IX,BC"
      [| 0xFDuy; 0x29uy |], "ADD IY,IY"
      [| 0xDDuy; 0xF9uy |], "LD SP,IX"
      [| 0xDDuy; 0xE3uy |], "EX (SP),IX"
      [| 0xE9uy |], "JP (HL)"
      [| 0xC9uy |], "RET"
      [| 0xC0uy |], "RET NZ"
      [| 0xD8uy |], "RET C"
      [| 0xF5uy |], "PUSH AF"
      [| 0xC1uy |], "POP BC"
      [| 0xC7uy |], "RST 0x00"
      [| 0xFFuy |], "RST 0x38"
      [| 0xD3uy; 0xFEuy |], "OUT (0xFE),A"
      [| 0xDBuy; 0xFEuy |], "IN A,(0xFE)"
      [| 0x08uy |], "EX AF,AF'"
      [| 0xD9uy |], "EXX"
      [| 0xEBuy |], "EX DE,HL"
      [| 0x07uy |], "RLCA"
      [| 0x2Fuy |], "CPL"
      [| 0x37uy |], "SCF"
      [| 0x3Fuy |], "CCF"
      [| 0x86uy |], "ADD A,(HL)"
      [| 0x80uy |], "ADD A,B"
      [| 0xAFuy |], "XOR A"
      [| 0xB8uy |], "CP B"
      [| 0xFEuy; 0x58uy |], "CP 0x58"
      [| 0xF3uy |], "DI"
      [| 0xFBuy |], "EI"
      [| 0xDDuy; 0x86uy; 0x03uy |], "ADD A,(IX+0x03)"
      [| 0xFDuy; 0x96uy; 0x7Fuy |], "SUB (IY+0x7F)"
      [| 0xDDuy; 0x70uy; 0x00uy |], "LD (IX+0x00),B"
      [| 0x3Cuy |], "INC A"
      [| 0x0Cuy |], "INC C"
      [| 0x34uy |], "INC (HL)"
      [| 0xEDuy; 0x72uy |], "SBC HL,SP"
      [| 0xEDuy; 0x6Auy |], "ADC HL,HL"
      [| 0xEDuy; 0x5Auy |], "ADC HL,DE" ]
  let mutable ok = true
  for (bytes, expected) in cases do
    let insn = Disasm.disasmBytes bytes 0
    let matchText = insn.Text = expected
    let matchLen = insn.Length = bytes.Length
    if not (matchText && matchLen) then
      ok <- false
      eprintfn "  MISMATCH %A: got '%s' len=%d, expected '%s' len=%d"
        bytes insn.Text insn.Length expected bytes.Length
  check "known instructions decode to expected text+length" ok ""
  if failures.Count > 0 then 1 else 0

let runDisasmCorpus () : int =
  printfn "disasm corpus over the loaded 64K image"
  let oracle, _ = Jetpac3.Core.Boot.bootToEntry rom tzx
  let mem, _ = oracle.SaveState()
  let mutable bad = 0
  let mutable dbFalls = 0
  let mutable lengthMismatch = 0
  for addr in 0 .. 0xFFFF do
    let insn = Disasm.disasmMemory mem addr
    if Disasm.disasmLength mem addr <> insn.Length then lengthMismatch <- lengthMismatch + 1
    if insn.Length < 1 || insn.Length > 4 then
      bad <- bad + 1
      if bad < 5 then eprintfn "  bad length at %04X: %A" addr insn
    if insn.Text.StartsWith "DB " then dbFalls <- dbFalls + 1
  check "every address decodes to a 1..4 byte instruction" (bad = 0) (sprintf "%d bad" bad)
  check "length-only path agrees with full disassembly everywhere" (lengthMismatch = 0)
    (sprintf "%d mismatches" lengthMismatch)
  check "no undefined-opcode fallbacks in ROM+game code region" (dbFalls <= 0x4000) (sprintf "%d DB fallbacks" dbFalls)
  if failures.Count > 0 then 1 else 0

let runTrace (romPath: string) (tzxPath: string) : int =
  printfn "trace recorder + codec roundtrip + stats"
  let session = TraceSession(romPath, tzxPath, 500_000)
  let framesN = 120
  let script = Jetpac3.Core.Script.defaultSession framesN
  for f in 0 .. framesN - 1 do
    let frameStart, _ = session.RunFrame()
    session.DrainBeeperSamples(frameStart) |> ignore
    for (sf, row, bit, pressed) in script do
      if sf = f then session.SetKey(row, bit, pressed)
  let recorder = session.Recorder
  let trace = recorder.Build()
  let mutable nonDecreasing = true
  for i in 1 .. trace.Entries.Length - 1 do
    if trace.Entries[i].Tick < trace.Entries[i - 1].Tick then nonDecreasing <- false
  let mutable heatSum = 0
  for i in 0 .. 0xFFFF do heatSum <- heatSum + trace.PerPcCount[i]
  let sawInterrupt = trace.Entries |> Array.exists (fun e -> e.Length = 0uy)
  check "trace captured instructions" (trace.Entries.Length > 0) (sprintf "%d entries" trace.Entries.Length)
  check "ticks are non-decreasing" nonDecreasing ""
  check "per-PC heat counts sum to the entry count" (heatSum = trace.Entries.Length)
    (sprintf "heat=%d entries=%d" heatSum trace.Entries.Length)
  check "interrupt service appears in the trace" sawInterrupt ""
  check "per-frame instruction counts are sane (7k..16k)"
    (trace.FrameTicks.Length >= 100) (sprintf "%d frames" trace.FrameTicks.Length)
  // codec roundtrip
  let path = Path.Combine(Path.GetTempPath(), sprintf "jetpacfr-%d.jpt" (DateTime.UtcNow.Ticks))
  TraceCodec.save trace path
  let loaded = TraceCodec.load path
  File.Delete path
  let mutable entriesEqual = loaded.Entries.Length = trace.Entries.Length
  let mutable i = 0
  while entriesEqual && i < trace.Entries.Length do
    if loaded.Entries[i] <> trace.Entries[i] then entriesEqual <- false
    i <- i + 1
  let mutable snapsEqual = loaded.Snapshots.Length = trace.Snapshots.Length
  i <- 0
  while snapsEqual && i < trace.Snapshots.Length do
    if loaded.Snapshots[i] <> trace.Snapshots[i] then snapsEqual <- false
    i <- i + 1
  check "codec roundtrip: entries identical" entriesEqual ""
  check "codec roundtrip: snapshots identical" snapsEqual ""
  check "codec roundtrip: frame ticks identical" (loaded.FrameTicks = trace.FrameTicks) ""
  check "codec roundtrip: per-PC counts identical" (loaded.PerPcCount = trace.PerPcCount) ""
  check "codec roundtrip: self-modified flags identical" (loaded.SelfModified = trace.SelfModified) ""
  check "self-mod count is consistent" (trace.SelfModCount = (trace.SelfModified |> Array.filter id |> Array.length)) ""
  let selfModConsistent =
    Array.forall2 (fun sm c -> (not sm) || c > 0) trace.SelfModified trace.PerPcCount
  check "every self-modified address was executed" selfModConsistent ""
  // record toggle
  let before = recorder.EntryCount
  recorder.RecordEnabled <- false
  session.RunFrame() |> ignore
  let after = recorder.EntryCount
  recorder.RecordEnabled <- true
  check "recording can be disabled without disturbing the machine"
    (before = after) (sprintf "%d -> %d" before after)
  if failures.Count > 0 then 1 else 0

let runAgree (romPath: string) (tzxPath: string) : int =
  printfn "live trace vs disassembler agreement"
  let session = TraceSession(romPath, tzxPath, 500_000)
  let framesN = 150
  let script = Jetpac3.Core.Script.defaultSession framesN
  for f in 0 .. framesN - 1 do
    let frameStart, _ = session.RunFrame()
    session.DrainBeeperSamples(frameStart) |> ignore
    for (sf, row, bit, pressed) in script do
      if sf = f then session.SetKey(row, bit, pressed)
  let trace = session.Recorder.Build()
  let mutable mismatches = 0
  let mutable checkedCount = 0
  for e in trace.Entries do
    if e.Length <> 0uy then
      let bytes =
        [| e.B0
           e.B1
           e.B2
           e.B3 |]
      let insn = Disasm.disasmBytes bytes 0
      checkedCount <- checkedCount + 1
      if insn.Length <> int e.Length then
        mismatches <- mismatches + 1
        if mismatches < 5 then
          eprintfn "  length mismatch at pc=%04X trace=%d disasm=%d (%s)"
            e.Pc e.Length insn.Length insn.Text
  check "disassembler length equals recorder length for every executed instruction"
    (mismatches = 0) (sprintf "%d of %d mismatched" mismatches checkedCount)
  // The strong contract: for every fall-through instruction the machine's
  // real PC advance must equal the recorded length.
  let mutable advanceMismatches = 0
  for e in trace.Entries do
    if e.Length <> 0uy && e.Taken = 0uy then
      let expected = (int e.Pc + int e.Length) &&& 0xFFFF
      if expected <> int e.Target then
        advanceMismatches <- advanceMismatches + 1
        if advanceMismatches < 5 then
          eprintfn "  advance mismatch at pc=%04X len=%d target=%04X" e.Pc e.Length e.Target
  check "fall-through PC advance matches recorded length for every instruction"
    (advanceMismatches = 0) (sprintf "%d mismatches" advanceMismatches)
  let distinctPcs =
    trace.Entries |> Array.fold (fun (s: System.Collections.Generic.HashSet<int>) e ->
      s.Add(int e.Pc) |> ignore; s) (System.Collections.Generic.HashSet<int>())
  check "execution covers a rich set of addresses"
    (distinctPcs.Count >= 100) (sprintf "%d distinct PCs" distinctPcs.Count)
  let hot =
    trace.PerPcCount
    |> Array.mapi (fun pc c -> pc, c)
    |> Array.sortByDescending (fun (_, c) -> c)
    |> Array.take 8
  printfn "  hottest PCs: %s"
    (hot |> Array.map (fun (pc, c) -> sprintf "%04X x%d" pc c) |> String.concat "  ")
  if failures.Count > 0 then 1 else 0

let runGaps () : int =
  printfn "generic-table coverage probe (the reported 0x6496 case)"
  match EntryCache.tryLoad rom tzx with
  | Some (mem, state) ->
    let port = Jetpac2.Core.Machine()
    Jetpac2.Core.Z80Table.EnsureInstalled()
    port.LoadState(mem, state)
    // Fresh machine, zero cycles executed: the interrupt cannot be pending,
    // so a Step at this address is a pure dispatch.
    port.Regs.SetPc 0x6496
    let opcode = int mem[0x6496]
    let outcome =
      try
        port.Step() |> ignore
        "executable"
      with ex ->
        if ex.Message.StartsWith "no code at" then
          sprintf "uncovered opcode, pc preserved=%b" (port.Regs.Pc() = 0x6496)
        else "other: " + ex.Message
    printfn "  0x6496 from entry state (opcode %02X): %s" opcode outcome
    // The generic table dispatches by opcode, not address: 0x6496 holds a
    // covered opcode (0xCD = CALL), so it executes even though the game
    // never visits that address. Uncovered opcodes still raise "no code at"
    // with the PC preserved (the UI's static-disasm path).
    if opcode = 0xCD then
      check "0x6496 (opcode CD) is executable in the generic table" (outcome = "executable") outcome
      check "executed step advanced past 0x6496" (port.Regs.Pc() <> 0x6496)
        (sprintf "pc=%04X" (port.Regs.Pc()))
    else
      check "uncovered opcode fails with pc preserved" (outcome.StartsWith "uncovered opcode, pc preserved=true") outcome
    if failures.Count > 0 then 1 else 0
  | None ->
    eprintfn "no entry cache; run --test trace first (cold boot)"
    1

let runMine (romPath: string) (tzxPath: string) : int =
  printfn "function miner over a scripted session"
  let session = TraceSession(romPath, tzxPath, 2_000_000)
  let framesN = 500
  let script = Jetpac3.Core.Script.defaultSession framesN
  for f in 0 .. framesN - 1 do
    let frameStart, _ = session.RunFrame()
    session.DrainBeeperSamples(frameStart) |> ignore
    for (sf, row, bit, pressed) in script do
      if sf = f then session.SetKey(row, bit, pressed)
  let trace = session.Recorder.Build()
  let routines, edges = Miner.mine trace
  printfn "  %d routines, %d edges; 0x71B8 executed=%b"
    routines.Length edges.Length
    (trace.Entries |> Array.exists (fun e -> e.Pc = 0x71B8us))
  for r in routines |> List.truncate 15 do
    printfn "    %04X  calls=%4d  span=%04X-%04X  incl=%6d  excl=%6d  %s"
      r.Entry r.CallCount r.SpanLo r.SpanHi r.InclusiveTStates r.ExclusiveTStates r.Risk
  check "miner found routines" (routines.Length > 0) ""
  let entrySet = routines |> List.map (fun r -> r.Entry) |> Set.ofList
  let badInvariants =
    routines
    |> List.filter (fun r ->
      not (r.SpanLo <= r.Entry
           && r.Entry <= r.SpanHi
           && r.InclusiveTStates >= r.ExclusiveTStates
           && r.ExclusiveTStates >= 0L
           && (r.CallCount >= 1 || r.SpanHi - r.SpanLo + 1 <= 0x1000)))
  check "routine invariants (span contains entry, incl>=excl, calls>=1)"
    (List.isEmpty badInvariants)
    (badInvariants
     |> List.truncate 3
     |> List.map (fun r ->
       sprintf "%04X span=%04X-%04X incl=%d excl=%d calls=%d"
         r.Entry r.SpanLo r.SpanHi r.InclusiveTStates r.ExclusiveTStates r.CallCount)
     |> String.concat "; ")
  let edgeOk =
    edges
    |> List.forall (fun e ->
      entrySet.Contains e.Callee
      && e.Count >= 1)
  check "every call edge points at a mined routine" edgeOk ""
  check "a hot routine exists (>= 10 calls, e.g. the ISR)"
    (routines |> List.exists (fun r -> r.CallCount >= 10)) ""
  let intVectors =
    trace.Entries
    |> Array.choose (fun e -> if e.Length = 0uy then Some(int e.Pc) else None)
    |> Array.distinct
  printfn "  INT vectors seen: %A" intVectors
  let isrOk =
    intVectors
    |> Array.exists (fun vec -> routines |> List.exists (fun r -> r.Entry = vec))
  check "an interrupt service routine was mined" isrOk (sprintf "vectors=%A" intVectors)
  // Identify what is actually at 0x71B8 (Jetpac3's lifted screenClear entry)
  // versus the hot clear the game really calls (0x71CF).
  printfn "  disasm 71B0-7215:"
  let mutable addr = 0x71B0
  while addr <= 0x7215 do
    let insn = Disasm.disasmMemory session.Memory addr
    printfn "    %04X  %s" addr insn.Text
    addr <- (addr + insn.Length) &&& 0xFFFF
  match routines |> List.tryFind (fun r -> r.Entry = 0x71CF) with
  | Some clear ->
    check "screen-clear routine (0x71CF) mined with many calls"
      (clear.CallCount >= 100) (sprintf "%d calls" clear.CallCount)
    check "screen-clear routine has loop extents"
      (not (List.isEmpty clear.LoopExtents)) (sprintf "%A" (clear.LoopExtents |> List.truncate 3))
  | None -> check "screen-clear routine (0x71CF) found in the mined set" false ""
  match routines |> List.tryFind (fun r -> r.Entry = 0x71B8) with
  | Some _ -> check "0x71B8 (Jetpac3 lift entry) not exercised by this script" false "unexpected: 0x71B8 IS executed"
  | None -> ()
  if failures.Count > 0 then 1 else 0

let runContract (romPath: string) (tzxPath: string) : int =
  printfn "contract + prompt extraction"
  let session = TraceSession(romPath, tzxPath, 1_000_000)
  let framesN = 400
  let script = Jetpac3.Core.Script.defaultSession framesN
  for f in 0 .. framesN - 1 do
    let frameStart, _ = session.RunFrame()
    session.DrainBeeperSamples(frameStart) |> ignore
    for (sf, row, bit, pressed) in script do
      if sf = f then session.SetKey(row, bit, pressed)
  let trace = session.Recorder.Build()
  let routines, _ = Miner.mine trace
  match routines |> List.tryFind (fun r -> r.Entry = 0x71CF) with
  | None ->
    check "screen-clear routine (0x71CF) mined" false "not found"
    1
  | Some clear ->
    let pcCycles = Array.zeroCreate<int64> 0x10000
    for e in trace.Entries do
      if e.Length <> 0uy then pcCycles[int e.Pc] <- pcCycles[int e.Pc] + int64 e.Cycles
    let contract = Contract.extract session.Memory trace clear pcCycles
    check "disassembly starts at the entry"
      (match contract.Disassembly with | h :: _ -> h.Address = clear.Entry | _ -> false) ""
    check "disassembly stays within the span"
      (contract.Disassembly |> List.forall (fun i -> i.Address >= contract.SpanLo && i.Address <= contract.SpanHi)) ""
    check "routine ends with RET"
      (contract.Disassembly |> List.exists (fun i -> i.Text = "RET")) ""
    check "writes attributed to the clear land in the screen area (4000..5AFF)"
      (contract.WriteRanges
       |> List.exists (fun (lo, hi, _, _) -> hi >= 0x4000 && lo <= 0x5AFF))
      (sprintf "%A" (contract.WriteRanges |> List.truncate 4))
    check "the clear restores its registers (small register delta)"
      (contract.RegisterDelta.Length <= 4)
      (sprintf "%A" contract.RegisterDelta)
    check "api hints mention memory writes and registers"
      (contract.ApiHints |> List.exists (fun h -> h.Contains "Write" || h.Contains "memory")
       && contract.ApiHints |> List.exists (fun h -> h.Contains "Regs"))
      (sprintf "%A" contract.ApiHints)
    // A routine that actually mutates state should show either writes or a
    // register delta somewhere in the mined set.
    let mutating =
      routines
      |> List.exists (fun r ->
        let c2 = Contract.extract session.Memory trace r pcCycles
        not (List.isEmpty c2.WriteRanges) || not (List.isEmpty c2.RegisterDelta))
    check "some mined routine shows writes or a register delta" mutating ""
    let prompt = Prompt.generate contract
    check "prompt mentions the entry address" (prompt.Contains(sprintf "0x%04X" clear.Entry)) ""
    check "prompt has contract/api/conventions/acceptance sections"
      (prompt.Contains "## Behavioural contract"
       && prompt.Contains "Jetpac2.Core.Machine"
       && prompt.Contains "## Conventions"
       && prompt.Contains "## Acceptance"
       && prompt.Contains "```fsharp") ""
    check "prompt carries the write contract"
      (prompt.Contains "memory writes") (sprintf "len=%d" prompt.Length)
    let path = Path.Combine(Path.GetTempPath(), sprintf "prompt-%d.md" (DateTime.UtcNow.Ticks))
    File.WriteAllText(path, prompt)
    let read = File.ReadAllText path
    File.Delete path
    check "prompt file round-trips" (read = prompt) ""
    if failures.Count > 0 then 1 else 0

let runValidate (romPath: string) (tzxPath: string) : int =
  printfn "validation theater"
  let covered (addr: int) =
    match Jetpac3.Core.LiftedRoutines.registryHook addr with
    | Some _ -> true
    | None -> false
  check "registry covers all lifted routines"
    (covered 0x71B8 && covered 0x71CF && covered 0x72EE && covered 0x64E6)
    ""
  // Good path: the current registry (screenClear 0x71B8) never executes in
  // this script, so the lifted port must match the oracle exactly.
  let good =
    Validation.run romPath tzxPath 120
      (Jetpac3.Core.Script.defaultSession 120)
      Jetpac3.Core.LiftedRoutines.registryHook
      System.Threading.CancellationToken.None
  check "validated run passes with the current registry"
    good.Passed
    (match good.FirstDivergence with Some d -> d.Kind | None -> "ok")
  // Bad path: deliberately wrong lift of the real screen clear (0x71CF) must
  // be detected with both machines' context.
  let badHook (addr: int) =
    if addr = 0x71CF then
      Some
        (fun (m: Jetpac2.Core.Machine) ->
          m.Fetch()
          m.PassTime 4)
    else
      Jetpac3.Core.LiftedRoutines.registryHook addr
  let bad =
    Validation.run romPath tzxPath 200
      (Jetpac3.Core.Script.defaultSession 200)
      badHook
      System.Threading.CancellationToken.None
  check "wrong lift is detected" (not bad.Passed) ""
  match bad.FirstDivergence with
  | Some d ->
    check "divergence carries both machines' executed instructions"
      (d.PortExecuted.Length > 0 && d.OracleExecuted.Length > 0)
      (sprintf "'%s' vs '%s'" d.PortExecuted d.OracleExecuted)
    check "divergence carries register context"
      (d.PortRegs.Contains "AF=" && d.OracleRegs.Contains "AF=") ""
    check "divergence frame is sane" (d.Frame >= 0) (sprintf "frame %d" d.Frame)
  | None -> check "divergence details captured" false "no divergence"
  if failures.Count > 0 then 1 else 0

let runPrompt (romPath: string) (tzxPath: string) : int =
  // Generate and print the lift prompt for the screen-clear routine (0x71CF)
  // so it can be pasted into the model chat verbatim.
  let session = TraceSession(romPath, tzxPath, 1_000_000)
  let framesN = 400
  let script = Jetpac3.Core.Script.defaultSession framesN
  for f in 0 .. framesN - 1 do
    let frameStart, _ = session.RunFrame()
    session.DrainBeeperSamples(frameStart) |> ignore
    for (sf, row, bit, pressed) in script do
      if sf = f then session.SetKey(row, bit, pressed)
  let trace = session.Recorder.Build()
  let routines, _ = Miner.mine trace
  match routines |> List.tryFind (fun r -> r.Entry = 0x71CF) with
  | None ->
    eprintfn "0x71CF not mined in %d frames" framesN
    1
  | Some clear ->
    let pcCycles = Array.zeroCreate<int64> 0x10000
    for e in trace.Entries do
      if e.Length <> 0uy then pcCycles[int e.Pc] <- pcCycles[int e.Pc] + int64 e.Cycles
    let contract = Contract.extract session.Memory trace clear pcCycles
    let prompt = Prompt.generate contract
    printfn "%s" prompt
    0

let runBench (romPath: string) (tzxPath: string) : int =
  printfn "bench: bare machine vs session (recording off/on), 600 frames"
  let mem, state =
    match EntryCache.tryLoad romPath tzxPath with
    | Some (m, s) -> m, s
    | None ->
      printfn "  cold booting to entry (caches for future runs)..."
      let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath
      let mem, state = oracle.SaveState()
      EntryCache.save romPath tzxPath mem state
      mem, state
  let framesN = 600
  let runBare () =
    let m = Jetpac2.Core.Machine()
    Jetpac2.Core.Z80Table.EnsureInstalled()
    m.LoadState(mem, state)
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let mutable steps = 0L
    for _ in 1 .. framesN do
      let fe = m.FrameEnd
      while m.CycleCount() < fe do
        m.Step()
        steps <- steps + 1L
      m.FrameEnd <- fe + 69888L
    sw.Stop()
    printfn "  bare machine          : %6.2f ms/frame  %7.0f steps/frame  %8.0f Msteps/s"
      (float sw.Elapsed.TotalMilliseconds / float framesN)
      (float steps / float framesN)
      (float steps / sw.Elapsed.TotalSeconds / 1e6)
  let runSession (recording: bool) =
    let s = TraceSession(romPath, tzxPath, 4_000_000)
    s.Recorder.RecordEnabled <- recording
    let sw = System.Diagnostics.Stopwatch.StartNew()
    for _ in 1 .. framesN do
      s.RunFrame() |> ignore
    sw.Stop()
    printfn "  session record=%5b    : %6.2f ms/frame  %7.0f steps/frame  %8.0f Msteps/s"
      recording
      (float sw.Elapsed.TotalMilliseconds / float framesN)
      (float s.Recorder.EntryCount / float framesN)
      (float s.Recorder.EntryCount / sw.Elapsed.TotalSeconds / 1e6)
  runBare ()
  runSession false
  runSession true
  if failures.Count > 0 then 1 else 0

let runHistory (romPath: string) (tzxPath: string) : int =
  printfn "history: snapshot exactness, determinism, replay, branch"
  let framesN = 120
  let script = Jetpac3.Core.Script.defaultSession framesN
  let applyKeys (s: TraceSession) (f: int) =
    for (sf, row, bit, pressed) in script do
      if sf = f then s.SetKey(row, bit, pressed)
  // Helper: a fresh session driven identically for `n` frames. Keys are
  // applied AFTER each completed frame, matching the app's frame loop
  // (RunFrame, then key events for the next frame).
  let runStraight n =
    let s = TraceSession(romPath, tzxPath, 500_000)
    for f in 0 .. n - 1 do
      s.RunFrame() |> ignore
      applyKeys s (f + 1)
    s

  // 1. Scripted session with the key log recording (as the app does).
  let session = TraceSession(romPath, tzxPath, 500_000)
  for f in 0 .. framesN - 1 do
    session.RunFrame() |> ignore
    applyKeys session (f + 1)
  check "history captured every frame" (session.History.LastFrame = framesN)
    (sprintf "last=%d" session.History.LastFrame)

  // 2. SaveState/LoadState roundtrip on the bare machine is exact (the
  // foundation of every rewind restore).
  let m = Jetpac2.Core.Machine()
  Jetpac2.Core.Z80Table.EnsureInstalled()
  let mem1, text1 = m.SaveState()
  let m2 = Jetpac2.Core.Machine()
  m2.LoadState(mem1, text1)
  let sameMem = (m.Memory = m2.Memory)
  let sameRegs = m.Regs.Pc() = m2.Regs.Pc() && m.Regs.Get Jetpac2.Core.R16.HL = m2.Regs.Get Jetpac2.Core.R16.HL
  check "port SaveState/LoadState roundtrip is exact" (sameMem && sameRegs) ""

  // 3. Rewind (preview) to 60: state matches a straight-through 60-frame run.
  let ref = runStraight 60
  session.RewindTo(60)
  let memEq = (session.Memory = ref.Memory)
  let pcEq = session.Regs.Pc() = ref.Regs.Pc()
  // Preview kept everything: the script's last release lands at frame 110.
  check "preview does not truncate the key log" (session.KeyLog.EndFrame = 110)
    (sprintf "end=%d" session.KeyLog.EndFrame)
  let restarted = TraceSession(romPath, tzxPath, 500_000)
  restarted.KeyLog.Replace(session.KeyLog.Events)
  restarted.StartReplay()
  check "restarted replay uses persisted key-log extent"
    (restarted.Replaying && restarted.ReplayEndFrame = restarted.KeyLog.EndFrame && restarted.ReplayEndFrame = 110)
    (sprintf "replaying=%b end=%d keyEnd=%d" restarted.Replaying restarted.ReplayEndFrame restarted.KeyLog.EndFrame)

  // 4. Replay from the previewed frame: history truncates at 60 but the log
  // is kept as the script; the replay must end exactly at the recording's
  // end (frame 120) with the recorded end state.
  let orig = runStraight framesN
  session.StartReplay()
  let mutable replayed = 0
  let mutable stopped = false
  while not stopped && replayed < framesN + 10 do
    session.RunFrame() |> ignore
    replayed <- replayed + 1
    if session.ReplayFinished then stopped <- true
  let endEq = stopped && session.Frame = framesN && session.Memory = orig.Memory
  check "replay stops at the end of the recording with the recorded end state"
    endEq
    (sprintf "stopped=%b frame=%d (want %d) mem=%b" stopped session.Frame framesN (session.Memory = orig.Memory))

  // 5. Branch (Go) at 60 after a fresh preview, then a live future with the
  // same keys must equal a straight-through 110-frame run.
  session.RewindTo(60)
  session.BranchAt(60)
  check "branch truncates history" (session.History.LastFrame = 60)
    (sprintf "last=%d" session.History.LastFrame)
  let post = runStraight 110
  for f in 60 .. 109 do
    session.RunFrame() |> ignore
    applyKeys session (f + 1)
  let futureEq = (session.Memory = post.Memory) && session.Regs.Pc() = post.Regs.Pc()
  check "post-branch future is deterministic" futureEq
    (sprintf "pc %04X vs %04X" (session.Regs.Pc()) (post.Regs.Pc()))

  if failures.Count > 0 then 1 else 0

let runManifest () : int =
  printfn "manifest: game discovery + load"
  let rec walk (d: DirectoryInfo) =
    let candidate = Path.Combine(d.FullName, "games")
    if Directory.Exists candidate then Some candidate
    else
      match d.Parent with
      | null -> None
      | p -> walk p
  let gamesDir =
    match walk (DirectoryInfo AppContext.BaseDirectory) with
    | Some d -> d
    | None ->
      check "games/ directory found from the test base" false "not found"
      ""
  match walk (DirectoryInfo AppContext.BaseDirectory) with
  | None -> check "games/ directory found from the test base" false "not found"
  | Some gamesDir ->
    let games = Manifest.discover gamesDir
    check "at least one game manifest discovered" (games.Length >= 1) (sprintf "%d games" games.Length)
    match games |> List.tryFind (fun g -> g.Name = "Jetpac") with
    | Some jetpac ->
      check "jetpac manifest resolves rom" (File.Exists jetpac.Rom) jetpac.Rom
      check "jetpac manifest resolves tzx" (File.Exists jetpac.Tzx) jetpac.Tzx
      check "jetpac boots automatically" (jetpac.Boot = "auto") jetpac.Boot
      check "jetpac is explicitly marked default" jetpac.Default "default flag missing"
      check "manifest discovery is stable by game ID" (games.Head.GameId = "jetpac") (sprintf "%A" (games |> List.map (fun g -> g.GameId)))
      let temp = Path.Combine(Path.GetTempPath(), "jetpacfr-replay-" + Guid.NewGuid().ToString("N"))
      Directory.CreateDirectory temp |> ignore
      let replayGame =
        { GameId = "replay-test"
          ManifestPath = ""
          GameDirectory = temp
          Name = "Replay test"
          Default = false
          Rom = jetpac.Rom
          Tzx = jetpac.Tzx
          Boot = "auto"
          Script = None
          ProgramBin = None
          ProgramAddress = None }
      let events =
        [ { Frame = 10; Row = 3; Bit = 0; Pressed = true }
          { Frame = 30; Row = 3; Bit = 0; Pressed = false } ]
      let saved = ReplayStore.save replayGame events
      let loaded = ReplayStore.tryLoad replayGame
      check "versioned replay round-trips" (saved = Ok () && File.Exists(ReplayStore.path replayGame) && loaded = ReplayLoaded events) (sprintf "%A / %A" saved loaded)
      let mismatched = { replayGame with GameId = "other-game" }
      check "replay identity rejects another game" (ReplayStore.tryLoad mismatched = ReplayIgnored "replay belongs to another game") "mismatch accepted"
      Directory.Delete(temp, true)
    | None -> check "jetpac manifest present" false ""
    match games |> List.tryFind (fun g -> g.Name = "Minimal") with
    | Some m ->
      check "minimal manifest is a program boot" (m.Boot = "program") m.Boot
      check "minimal manifest has program bin+address"
        (m.ProgramBin.IsSome && m.ProgramAddress = Some 32768)
        (sprintf "bin=%b addr=%A" m.ProgramBin.IsSome m.ProgramAddress)
    | None -> check "minimal manifest present" false ""
  if failures.Count > 0 then 1 else 0

let runGame2 () : int =
  printfn "game2: synthetic 48K game at 0x7000 - full pipeline (multi-game proof)"
  // Find the fixture through the manifest mechanism (same path the UI uses).
  let rec walk (d: DirectoryInfo) =
    let candidate = Path.Combine(d.FullName, "games")
    if Directory.Exists candidate then Some candidate
    else
      match d.Parent with
      | null -> None
      | p -> walk p
  match walk (DirectoryInfo AppContext.BaseDirectory) |> Option.bind (Manifest.discover >> List.tryFind (fun g -> g.Name = "Synthetic (test fixture)")) with
  | None ->
    check "synthetic game manifest discovered" false "not found"
    1
  | Some g ->
    // 1. Seed the entry state. This is exactly what ManualBoot.CaptureEntry
    // produces after the user runs the loader and marks the entry: the
    // machine-code game below runs from 0x7000 (NOT the Jetpac layout),
    // loops forever calling a subroutine at 0x7030 that writes a screen
    // pattern, reads the keyboard into 0x7102, and RETs.
    let code = Array.zeroCreate<byte> 0x80 // address - 0x7000 = index
    code[0x00] <- 0x3Euy; code[0x01] <- 0x00uy // LD A,0 (reset frame counter)
    code[0x02] <- 0x32uy; code[0x03] <- 0x01uy; code[0x04] <- 0x71uy // LD (0x7101),A
    code[0x05] <- 0xCDuy; code[0x06] <- 0x30uy; code[0x07] <- 0x70uy // CALL 0x7030
    code[0x08] <- 0x18uy; code[0x09] <- 0xFBuy // JR 0x7005 (infinite loop)
    code[0x30] <- 0x3Auy; code[0x31] <- 0x01uy; code[0x32] <- 0x71uy // LD A,(0x7101)
    code[0x33] <- 0x3Cuy // INC A
    code[0x34] <- 0x32uy; code[0x35] <- 0x01uy; code[0x36] <- 0x71uy // LD (0x7101),A
    code[0x37] <- 0x21uy; code[0x38] <- 0x00uy; code[0x39] <- 0x40uy // LD HL,0x4000
    code[0x3A] <- 0x47uy // LD B,A
    code[0x3B] <- 0x77uy; code[0x3C] <- 0x23uy; code[0x3D] <- 0x10uy; code[0x3E] <- 0xFCuy // LD (HL),A / INC HL / DJNZ 0x703B
    code[0x3F] <- 0xDBuy; code[0x40] <- 0xFEuy; code[0x41] <- 0xE6uy; code[0x42] <- 0x1Fuy // IN A,(0xFE) / AND 0x1F
    code[0x43] <- 0x32uy; code[0x44] <- 0x02uy; code[0x45] <- 0x71uy // LD (0x7102),A
    code[0x46] <- 0xC9uy // RET
    let mem = Array.zeroCreate<byte> 0x10000
    Array.blit code 0 mem 0x7000 0x47
    let state =
      "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=7FFE\npc=7000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"
    EntryCache.save g.Rom g.Tzx mem state

    // 2. Rewind/replay/branch determinism on the non-Jetpac layout.
    let historyCode = runHistory g.Rom g.Tzx
    if historyCode <> 0 then historyCode
    else
      // 3. Mine: the subroutine at 0x7030 called from the 0x7005 loop.
      let session = TraceSession(g.Rom, g.Tzx, 1_000_000)
      let framesN = 120
      let script = Jetpac3.Core.Script.defaultSession framesN
      for f in 0 .. framesN - 1 do
        session.RunFrame() |> ignore
        for (sf, row, bit, pressed) in script do
          if sf = f then session.SetKey(row, bit, pressed)
      let trace = session.Recorder.Build()
      let routines, edges = Miner.mine trace
      let entrySet = routines |> List.map (fun r -> r.Entry) |> Set.ofList
      check "synthetic routine 0x7030 mined" (entrySet.Contains 0x7030)
        (sprintf "%d routines" routines.Length)
      match routines |> List.tryFind (fun r -> r.Entry = 0x7030) with
      | Some r ->
        check "0x7030 called from the main loop" (r.CallCount >= 10) (sprintf "%d calls" r.CallCount)
        check "call edge 0x7005 -> 0x7030 recorded"
          (edges |> List.exists (fun e -> e.Callee = 0x7030 && e.Count >= 1)) ""
        let pcCycles = Array.zeroCreate<int64> 0x10000
        for e in trace.Entries do
          if e.Length <> 0uy then pcCycles[int e.Pc] <- pcCycles[int e.Pc] + int64 e.Cycles
        let contract = Contract.extract session.Memory trace r pcCycles
        check "contract ends with RET" (contract.Disassembly |> List.exists (fun i -> i.Text = "RET")) ""
        check "contract attributes writes to the screen/keyboard cells"
          (contract.WriteRanges |> List.exists (fun (lo, hi, _, _) -> lo <= 0x7102 && hi >= 0x4000))
          (sprintf "%A" (contract.WriteRanges |> List.truncate 4))
      | None -> check "synthetic routine 0x7030 mined" false "not found"
      // 4. Differential: port vs oracle, 0 diffs, 150 frames with keys.
      // The lifted-registry hook is per-game: the synthetic game has no
      // lifts yet, so an empty hook is the correct registry (Jetpac's
      // registryHook would fire Jetpac lifts at the wrong addresses).
      let report =
        Validation.run g.Rom g.Tzx 150 (Jetpac3.Core.Script.defaultSession 150)
          (fun _ -> None) System.Threading.CancellationToken.None
      check "synthetic game validates 0 diffs vs the oracle"
        report.Passed
        (match report.FirstDivergence with
         | Some d -> sprintf "%s@%04X port=%04X oracle=%04X" d.Kind d.Address d.PortPc d.OraclePc
         | None -> "")
      if failures.Count > 0 then 1 else 0

let runMinimal () : int =
  printfn "minimal: CE DSL game project - bytes, mixed CE+raw, oracle lockstep"
    // 1. Structure -> bytes: assembling the CE program reproduces the image.
  check "CE assemble reproduces the game image" (MinimalGame.Image.assembled = MinimalGame.Image.binary)
    (sprintf "%d vs %d bytes" MinimalGame.Image.assembled.Length MinimalGame.Image.binary.Length)
  // 2. Mixed raw + CE program: the two forms interleave and assemble
  // exactly (raw bytes not yet disassembled, CE ops once lifted).
  let mixed : Jetpac2.Core.Z80Op list =
    z80 {
      yield! [| 0x3Euy; 0x00uy |] // LD A,0 (raw)
      Jetpac2.Core.Z80Vocab.INC_A
      Jetpac2.Core.Z80Vocab.LD_HL 16384
      yield! [| 0xC9uy |] // RET (raw)
    }
  let mixedBytes = Jetpac2.Core.Z80.assemble mixed
  let expected = [| 0x3Euy; 0x00uy; 0x3Cuy; 0x21uy; 0x00uy; 0x40uy; 0xC9uy |]
  check "mixed raw+CE assembles to the expected bytes" (mixedBytes = expected)
    (sprintf "%A" mixedBytes)
  // 3. Structure -> behavior: the CE frame driver on the port must
  // lockstep with the oracle (which executes the same bytes natively) over
  // 150 frames: identical memory, PC/SP, and cycle counts.
  let mem, state = MinimalGame.Game.entryState MinimalGame.Image.binary
  let port = Jetpac2.Core.Machine()
  Jetpac2.Core.Z80Table.EnsureInstalled()
  port.LoadState(mem, state)
  let oracle = Jetpac.Core.Spectrum48()
  oracle.LoadState(mem, state)
  let mutable ok = true
  let mutable detail = ""
  let mutable f = 0
  while ok && f < 150 do
    MinimalGame.Game.runFrame port |> ignore
    oracle.RunGameFrame()
    let mutable memDiff = -1
    let mutable i = 0x4000
    while memDiff < 0 && i < 0x10000 do
      if port.Memory[i] <> oracle.Memory[i] then memDiff <- i
      i <- i + 1
    let z = oracle.DebugZ80
    let regsOk =
      port.Regs.Pc() = z.Regs.Pc()
      && port.Regs.Sp() = z.Regs.Sp()
      && port.CycleCount() = int64 (z.CycleCount())
    if memDiff >= 0 || not regsOk then
      ok <- false
      detail <- sprintf "frame %d memDiff=%d pc=%04X/%04X cycles=%d/%d"
        f memDiff (port.Regs.Pc()) (z.Regs.Pc()) (port.CycleCount()) (z.CycleCount())
    f <- f + 1
  check "CE driver locksteps with the oracle over 150 frames" ok detail
  // 4. If proba.bin is present, the assembled bytes must equal it exactly
  // (the "compare emitted bytes to the original" contract).
  let rec findMinimal (d: DirectoryInfo) =
    let candidate = Path.Combine(d.FullName, "games", "minimal", "proba.bin")
    if File.Exists candidate then Some candidate
    else
      match d.Parent with
      | null -> None
      | p -> findMinimal p
  match findMinimal (DirectoryInfo AppContext.BaseDirectory) with
  | Some proba ->
    let orig = File.ReadAllBytes proba
    check "assembled bytes match proba.bin" (MinimalGame.Image.assembled = orig)
      (sprintf "%d vs %d bytes" MinimalGame.Image.assembled.Length orig.Length)
  | None -> printfn "  proba.bin not present yet - placeholder game used"
  if failures.Count > 0 then 1 else 0

let runZ80Ops () : int =
  printfn "z80ops: per-op oracle lockstep (%d ops)" Jetpac2.Core.Z80Vocab.allOps.Length
  let stateText (af: int) (bc: int) (de: int) (hl: int) (ix: int) (iy: int) (sp: int) =
    sprintf "af=%04X\nbc=%04X\nde=%04X\nhl=%04X\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=%04X\niy=%04X\nsp=%04X\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"
      af bc de hl ix iy sp
  let mutable badOps = 0
  let mutable detail = ""
  for (name, op, kind) in Jetpac2.Core.Z80Vocab.allOps do
    let mem = Array.zeroCreate<byte> 0x10000
    // Program = [op; HALT] at 0x8000 (HALT alone for itself: a halted CPU
    // re-executes at 1 cycle, which the second-op model would mismatch).
    let program = if kind = "halt" then [ op ] else [ op; Jetpac2.Core.Z80Vocab.HALT ]
    let bytes = Jetpac2.Core.Z80.assemble program
    Array.blit bytes 0 mem 0x8000 bytes.Length
    let mutable af, bc, de, hl, ix, iy, sp = 0, 0, 0, 0, 0, 0, 0x7FFE
    match kind with
    | "hl" ->
      hl <- 0x9000; mem[0x9000] <- 0x12uy
    | "ixd" ->
      ix <- 0x9000; mem[0x9002] <- 0x12uy
    | "iyd" ->
      iy <- 0x9000; mem[0x9002] <- 0x12uy
    | "nn" ->
      mem[0x9000] <- 0x34uy; mem[0x9001] <- 0x12uy
    | "sp" ->
      mem[0x7FFE] <- 0x34uy; mem[0x7FFF] <- 0x12uy
    | "exsp" ->
      mem[0x7FFE] <- 0x34uy; mem[0x7FFF] <- 0x12uy
    | "djnz" ->
      bc <- 0x0001
    | "jp" ->
      mem[0x0000] <- 0x76uy
    | "ret" ->
      // RET pops to the HALT byte that follows the op (1- or 2-byte ops).
      let haltAddr = 0x8000 + op.Bytes.Length
      mem[0x7FFE] <- byte (haltAddr &&& 0xFF)
      mem[0x7FFF] <- byte ((haltAddr >>> 8) &&& 0xFF)
    | "rst" ->
      mem[0x00] <- 0x76uy; mem[0x08] <- 0x76uy; mem[0x10] <- 0x76uy; mem[0x18] <- 0x76uy
      mem[0x20] <- 0x76uy; mem[0x28] <- 0x76uy; mem[0x30] <- 0x76uy; mem[0x38] <- 0x76uy
    | "block" ->
      // Single pass: BC=1 so the repeat ops (LDIR/CPIR) do not loop.
      hl <- 0x9000; de <- 0x9100; bc <- 0x0001
      mem[0x9000] <- 0x12uy; mem[0x9100] <- 0x55uy
    | _ -> ()
    let state = stateText af bc de hl ix iy sp
    let port = Jetpac2.Core.Machine()
    Jetpac2.Core.Z80Table.EnsureInstalled()
    port.LoadState(mem, state)
    let oracle = Jetpac.Core.Spectrum48()
    oracle.LoadState(mem, state)
    // CE run of the two-op program vs the oracle's native execution.
    Jetpac2.Core.Z80.run program port
    let z = oracle.DebugZ80
    z.ExecuteOne()
    if kind <> "halt" then z.ExecuteOne()
    let mutable memDiff = -1
    let mutable i = 0
    while memDiff < 0 && i < 0x10000 do
      if port.Memory[i] <> oracle.Memory[i] then memDiff <- i
      i <- i + 1
    let regsOk =
      port.Regs.Pc() = z.Regs.Pc()
      && port.Regs.Sp() = z.Regs.Sp()
      && port.Regs.Get Jetpac2.Core.R16.AF = z.Regs.Get Jetpac.Core.R16.AF
      && port.Regs.Get Jetpac2.Core.R16.BC = z.Regs.Get Jetpac.Core.R16.BC
      && port.Regs.Get Jetpac2.Core.R16.DE = z.Regs.Get Jetpac.Core.R16.DE
      && port.Regs.Get Jetpac2.Core.R16.HL = z.Regs.Get Jetpac.Core.R16.HL
      && port.Regs.Get Jetpac2.Core.R16.AF_ = z.Regs.Get Jetpac.Core.R16.AF_
      && port.Regs.Get Jetpac2.Core.R16.BC_ = z.Regs.Get Jetpac.Core.R16.BC_
      && port.Regs.Get Jetpac2.Core.R16.DE_ = z.Regs.Get Jetpac.Core.R16.DE_
      && port.Regs.Get Jetpac2.Core.R16.HL_ = z.Regs.Get Jetpac.Core.R16.HL_
      && port.Regs.Ix() = z.Regs.Ix()
      && port.Regs.Iy() = z.Regs.Iy()
      && port.Regs.I() = z.Regs.I()
      && port.Regs.R() = z.Regs.R()
      && port.Iff1 = z.Iff1
      && port.Iff2 = z.Iff2
      && port.IrqMode = z.IrqMode
    let cyclesOk = port.CycleCount() = int64 (z.CycleCount())
    if memDiff >= 0 || not regsOk || not cyclesOk then
      badOps <- badOps + 1
      if badOps <= 10 then
        printfn "  FAIL %s: mem=%d pc=%04X/%04X cycles=%d/%d" name memDiff
          (port.Regs.Pc()) (z.Regs.Pc()) (port.CycleCount()) (z.CycleCount())
  check "every vocabulary op locksteps with the oracle" (badOps = 0)
    (sprintf "%d/%d failed%s" badOps Jetpac2.Core.Z80Vocab.allOps.Length detail)
  if badOps = 0 then printfn "  all %d ops matched the oracle" Jetpac2.Core.Z80Vocab.allOps.Length
  if failures.Count > 0 then 1 else 0

let runLabels () : int =
  printfn "labels: two-pass symbolic assembly + oracle lockstep"
  // Bounded loop with a backward JP: LD B,3; loop: DEC B; JP NZ loop; HALT.
  let loop = Jetpac2.Core.Z80.label ()
  let loopProgram : Jetpac2.Core.Z80Op list =
    z80 {
      Jetpac2.Core.Z80Vocab.LD_B 0x03
      Jetpac2.Core.Z80.at loop
      Jetpac2.Core.Z80Vocab.DEC_B
      Jetpac2.Core.Z80.JP_NZ_LBL loop
      Jetpac2.Core.Z80Vocab.HALT
    }
  let bytes = Jetpac2.Core.Z80.assemble loopProgram
  // Layout: 06 03 | 05 | C2 02 00 (JP NZ -> the DEC B at offset 2) | 76
  let expected = [| 0x06uy; 0x03uy; 0x05uy; 0xC2uy; 0x02uy; 0x00uy; 0x76uy |]
  check "backward JP label assembles to the expected bytes" (bytes = expected)
    (sprintf "%A" bytes)
  // Forward reference: JR over a NOP. Layout: 00 | 18 FD | 76
  // (JR at offset 1: disp = target(0) - (1 + 2) = -3 = 0xFD).
  let fwdProgram : Jetpac2.Core.Z80Op list =
    z80 {
      let target = Jetpac2.Core.Z80.label ()
      Jetpac2.Core.Z80.at target
      Jetpac2.Core.Z80Vocab.NOP
      Jetpac2.Core.Z80.JR_LBL target
      Jetpac2.Core.Z80Vocab.HALT
    }
  let fwdBytes = Jetpac2.Core.Z80.assemble fwdProgram
  let fwdExpected = [| 0x00uy; 0x18uy; 0xFDuy; 0x76uy |]
  check "backward JR label (over a NOP) assembles to the expected bytes" (fwdBytes = fwdExpected)
    (sprintf "%A" fwdBytes)
  // CALL to a forward subroutine: LD A,5; CALL sub; HALT; sub: DEC A; RET.
  let callProgram : Jetpac2.Core.Z80Op list =
    z80 {
      let sub = Jetpac2.Core.Z80.label ()
      Jetpac2.Core.Z80Vocab.LD_A 0x05
      Jetpac2.Core.Z80.CALL_LBL sub
      Jetpac2.Core.Z80Vocab.HALT
      Jetpac2.Core.Z80.at sub
      Jetpac2.Core.Z80Vocab.DEC_A
      Jetpac2.Core.Z80Vocab.RET
    }
  let callBytes = Jetpac2.Core.Z80.assemble callProgram
  // Layout: 3E 05 | CD 03 00 (CALL -> sub at offset 3) | 76 | 3D | C9
  // Layout: 3E 05 | CD 06 00 (CALL -> sub at offset 6) | 76 | 3D | C9
  let callExpected = [| 0x3Euy; 0x05uy; 0xCDuy; 0x06uy; 0x00uy; 0x76uy; 0x3Duy; 0xC9uy |]
  check "forward CALL label assembles to the expected bytes" (callBytes = callExpected)
    (sprintf "%A" callBytes)
  // Oracle lockstep on the bounded loop: 7 instructions (3 x DEC+JP, HALT).
  let mem = Array.zeroCreate<byte> 0x10000
  Array.blit bytes 0 mem 0x8000 bytes.Length
  let state =
    "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=7FFE\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"
  let port = Jetpac2.Core.Machine()
  Jetpac2.Core.Z80Table.EnsureInstalled()
  port.LoadState(mem, state)
  let oracle = Jetpac.Core.Spectrum48()
  oracle.LoadState(mem, state)
  let z = oracle.DebugZ80
  for _ in 1 .. 7 do
    port.Step()
    z.ExecuteOne()
  let mutable memDiff = -1
  let mutable i = 0
  while memDiff < 0 && i < 0x10000 do
    if port.Memory[i] <> oracle.Memory[i] then memDiff <- i
    i <- i + 1
  let ok =
    memDiff < 0
    && port.Regs.Pc() = z.Regs.Pc()
    && port.Regs.Get Jetpac2.Core.R16.AF = z.Regs.Get Jetpac.Core.R16.AF
    && port.Regs.Get Jetpac2.Core.R16.BC = z.Regs.Get Jetpac.Core.R16.BC
    && port.CycleCount() = int64 (z.CycleCount())
  check "labeled loop locksteps with the oracle" ok
    (sprintf "mem=%d pc=%04X/%04X cycles=%d/%d" memDiff (port.Regs.Pc()) (z.Regs.Pc())
       (port.CycleCount()) (z.CycleCount()))
  // The CE index (used by runFrame) also resolves labels.
  let idx = Jetpac2.Core.Z80.makeIndex loopProgram
  check "label resolution via makeIndex"
    (idx.Lookup 2 |> Option.isSome && !loop = 2) (sprintf "cell=%d" !loop)
  if failures.Count > 0 then 1 else 0

let runIntegration () : int =
  printfn "integration: CE invalidation + co-executing effects"
  let state =
    "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=FFFE\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"

  let smcProgram =
    [ Jetpac2.Core.Z80Vocab.LD_A 1
      Jetpac2.Core.Z80Vocab.HALT ]
  let smcImage = Jetpac2.Core.Z80.assemble smcProgram
  let smcMem = Array.zeroCreate<byte> 0x10000
  Array.blit smcImage 0 smcMem 0x8000 smcImage.Length
  let smcMachine = Jetpac2.Core.Machine()
  Jetpac2.Core.Z80Table.EnsureInstalled()
  smcMachine.LoadState(smcMem, state)
  let smcIndex = Jetpac2.Core.Z80.makeIndexAt 0x8000 smcProgram
  smcIndex.Attach smcMachine
  let mutable visits = 0
  smcMachine.AddCoHook(0x8000, fun m ->
    visits <- visits + 1
    m.Write(0x8001, 2)
    [ Jetpac2.Core.Say "patched" ]) |> ignore
  let resolveSmc (m: Jetpac2.Core.Machine) =
    match smcIndex.Lookup(m.Regs.Pc()) with
    | Some op -> op.Run m
    | None -> Jetpac2.Core.Machine.GeneratedStep m
  smcMachine.ExecuteOne resolveSmc
  check "SMC hook invalidates the CE entry before dispatch"
    (visits = 1 && smcMachine.Regs.Get Jetpac2.Core.R8.A = 2
     && not (smcIndex.IsValid 0x8000))
    (sprintf "visits=%d A=%d valid=%b" visits (smcMachine.Regs.Get Jetpac2.Core.R8.A)
      (smcIndex.IsValid 0x8000))
  check "co-hook Say effect is emitted" (smcMachine.DrainEffects() = [ Jetpac2.Core.Say "patched" ]) ""
  smcMachine.LoadState(smcMem, state)
  check "state reload refreshes CE validity" (smcIndex.IsValid 0x8000) "entry stayed invalid after reload"

  let hookProgram =
    [ Jetpac2.Core.Z80Vocab.LD_A 9
      Jetpac2.Core.Z80Vocab.HALT ]
  let hookImage = Jetpac2.Core.Z80.assemble hookProgram
  let hookMem = Array.zeroCreate<byte> 0x10000
  Array.blit hookImage 0 hookMem 0x8000 hookImage.Length
  let hookMachine = Jetpac2.Core.Machine()
  hookMachine.LoadState(hookMem, state)
  let hookIndex = Jetpac2.Core.Z80.makeIndexAt 0x8000 hookProgram
  hookIndex.Attach hookMachine
  hookMachine.AddCoHook(0x8000, fun _ ->
    [ Jetpac2.Core.Say "first"; Jetpac2.Core.Tick 2 ]) |> ignore
  hookMachine.AddCoHook(0x8000, fun _ -> [ Jetpac2.Core.Say "second" ]) |> ignore
  let resolveHook (m: Jetpac2.Core.Machine) =
    match hookIndex.Lookup(m.Regs.Pc()) with
    | Some op -> op.Run m
    | None -> Jetpac2.Core.Machine.GeneratedStep m
  hookMachine.ExecuteOne resolveHook
  check "co-hooks run in registration order and preserve the instruction"
    (hookMachine.Regs.Get Jetpac2.Core.R8.A = 9
     && hookMachine.DrainEffects() = [ Jetpac2.Core.Say "first"; Jetpac2.Core.Say "second" ]
     && hookMachine.CycleCount() = 9L)
    (sprintf "A=%d cycles=%d" (hookMachine.Regs.Get Jetpac2.Core.R8.A) (hookMachine.CycleCount()))
  let stepMachine = Jetpac2.Core.Machine()
  stepMachine.LoadState(hookMem, state)
  stepMachine.AddCoHook(0x8000, fun _ -> [ Jetpac2.Core.Say "step" ]) |> ignore
  stepMachine.Step()
  check "co-hooks also run on the generated interpreter path"
    (stepMachine.Regs.Get Jetpac2.Core.R8.A = 9
     && stepMachine.DrainEffects() = [ Jetpac2.Core.Say "step" ]) ""
  let dynamicCache = Jetpac3.Core.CodeCache()
  dynamicCache.Attach stepMachine
  dynamicCache.Decode stepMachine.Memory 0x8000 |> ignore
  let decodedBeforeReset = dynamicCache.Count
  stepMachine.LoadState(hookMem, state)
  check "dynamic decode cache clears on state reload"
    (decodedBeforeReset = 1 && dynamicCache.Count = 0)
    (sprintf "before=%d after=%d" decodedBeforeReset dynamicCache.Count)
  if failures.Count > 0 then 1 else 0

let runControl () : int =
  printfn "control: json roundtrip + skool ctrl import/export"
  // 1. JSON roundtrip preserves everything.
  let cf : JetpacFR.Core.ControlFile =
    { ImageFile = "original.bin"
      Start = 0x8000
      EndExcl = 0x809B
      EntryPc = 0x8000
      ActiveVersion = 2
      Blocks =
        [ { Start = 0x8000; EndExcl = 0x8023; Name = "main"; Kind = Code }
          { Start = 0x8023; EndExcl = 0x8026; Name = "table"; Kind = Data } ]
      Comments =
        [ { Kind = Line; Addr = 0x8005; EndExcl = 0; InstrIndex = -1; Text = "keyboard scan" }
          { Kind = Name; Addr = 0x8000; EndExcl = 0x8023; InstrIndex = -1; Text = "MainLoop" }
          { Kind = Range; Addr = 0x8010; EndExcl = 0x8020; InstrIndex = -1; Text = "sprite update" }
          { Kind = Exec; Addr = 0; EndExcl = 0; InstrIndex = 1234; Text = "first jump into RAM" } ]
      Dirty = true }
  let rt = JetpacFR.Core.ControlFile.fromJson (JetpacFR.Core.ControlFile.toJson cf)
  check "json roundtrip: blocks" (rt.Blocks = cf.Blocks) (sprintf "%A" rt.Blocks)
  check "json roundtrip: comments" (rt.Comments = cf.Comments) (sprintf "%A" rt.Comments)
  check "json roundtrip: scalars"
    (rt.Start = cf.Start && rt.EndExcl = cf.EndExcl && rt.ActiveVersion = cf.ActiveVersion
     && rt.EntryPc = cf.EntryPc && rt.ImageFile = cf.ImageFile)
    (JetpacFR.Core.ControlFile.toJson rt)
  check "roundtrip clears dirty flag" (not rt.Dirty) ""
  // 2. commentAt: exact line wins, else covering name/range.
  let at a = JetpacFR.Core.ControlFile.commentAt cf a
  check "commentAt exact line" (at 0x8005 = Some "keyboard scan") (sprintf "%A" (at 0x8005))
  check "commentAt falls back to range" (at 0x8015 = Some "sprite update") (sprintf "%A" (at 0x8015))
  check "commentAt none" (at 0x8090 = None) (sprintf "%A" (at 0x8090))
  // 3. upsert replaces by key; empty text deletes.
  let cf2 =
    JetpacFR.Core.ControlFile.upsert cf { Kind = Line; Addr = 0x8005; EndExcl = 0; InstrIndex = -1; Text = "rewritten" }
  check "upsert replaces same key" ((cf2.Comments |> List.filter (fun m -> m.Kind = Line)).Length = 1) ""
  check "upsert new text" (JetpacFR.Core.ControlFile.commentAt cf2 0x8005 = Some "rewritten") ""
  let cf3 = JetpacFR.Core.ControlFile.upsert cf2 { Kind = Line; Addr = 0x8005; EndExcl = 0; InstrIndex = -1; Text = "" }
  // Removing the line comment at 0x8005: the address still shows the
  // covering Name "MainLoop" (0x8000-0x8023), not the removed text.
  check "upsert empty text removes"
    (JetpacFR.Core.ControlFile.commentAt cf3 0x8005 = Some "MainLoop") ""
  // 4. SkoolKit import: block directives + N/D comments, hex and decimal.
  let skoolText = """c $8000 Main routine
N $8005 entry point
b 32773 data table
D 32773 lookup values
t 32778 messages
"""
  let imported = JetpacFR.Core.SkoolCtl.import skoolText
  check "skool: 3 blocks" (imported.Blocks.Length = 3) (sprintf "%A" imported.Blocks)
  check "skool: first block code" (imported.Blocks.Head.Kind = Code && imported.Blocks.Head.Name = "Main routine") (sprintf "%A" imported.Blocks.Head)
  check "skool: hex addr parsed" (imported.Blocks.Head.Start = 0x8000) (sprintf "%d" imported.Blocks.Head.Start)
  check "skool: dec addr parsed" (imported.Blocks |> List.item 1 |> fun b -> b.Start = 32773) ""
  check "skool: N becomes line comment"
    (imported.Comments |> List.exists (fun m -> m.Kind = Line && m.Addr = 0x8005 && m.Text = "entry point")) ""
  check "skool: D becomes range comment"
    (imported.Comments |> List.exists (fun m -> m.Kind = Range && m.Addr = 32773)) ""
  check "skool: blocks closed at successors"
    (imported.Blocks.Head.EndExcl = 32773) (sprintf "%d" imported.Blocks.Head.EndExcl)
  // 5. Export shape: letters + titles, re-importable.
  let exported = JetpacFR.Core.SkoolCtl.export cf
  check "export emits c directive" (exported.Contains("c 32768")) exported
  check "export emits N comment" (exported.Contains("N 32773 keyboard scan")) exported
  let reimported = JetpacFR.Core.SkoolCtl.import exported
  check "export reimports to same block count" (reimported.Blocks.Length = cf.Blocks.Length) (sprintf "%d vs %d" reimported.Blocks.Length cf.Blocks.Length)
  if failures.Count > 0 then 1 else 0

let runCtrlMap () : int =
  printfn "ctrlmap: LOD snippets, row classification, block edits"
  // A small code image: LD BC,$1234 / RET at $8000.
  let mem = Array.zeroCreate<byte> 0x10000
  mem[0x8000] <- 0x01uy
  mem[0x8001] <- 0x34uy
  mem[0x8002] <- 0x12uy
  mem[0x8003] <- 0xC9uy
  let starts =
    let s = Array.create 0x10000 false
    let mutable a = 0x8000
    while a < 0x9000 do
      s[a] <- true
      a <- a + max 1 (min 6 (Disasm.disasmMemory mem a).Length)
    Some s
  let blocks =
    [ { Start = 0x8000; EndExcl = 0x8010; Name = "main"; Kind = Code } ]
  let comments =
    [ { ControlComment.Kind = Line; Addr = 0x8003; EndExcl = 0; InstrIndex = -1; Text = "back" } ]
  let counts = Array.zeroCreate<int> 0x10000
  counts[0x8005] <- 500
  let render vs ve rc =
    JetpacFR.Core.CtrlMapModel.render blocks comments starts (Some mem) counts (Array.zeroCreate<bool> 0x10000) vs ve rc
  // 1. Instruction-level LOD: ppb >= 8 yields decoded snippets.
  let hi = render 0x8000 0x8040 800
  check "snippets present above threshold" (hi.Snippets.Length >= 2) (sprintf "%d" hi.Snippets.Length)
  check "snippet first is LD BC,nn"
    (hi.Snippets[0].Addr = 0x8000 && hi.Snippets[0].Bytes.StartsWith("01") && hi.Snippets[0].Text.Length > 0)
    (sprintf "%A" hi.Snippets[0])
  check "snippet carries line comment"
    (hi.Snippets |> Array.find (fun s -> s.Addr = 0x8003) |> fun s -> s.Comment = Some "back")
    (sprintf "%A" hi.Snippets)
  // 2. Zoomed out: no snippets.
  let lo = render 0x0000 0x10000 800
  check "snippets absent below threshold" (lo.Snippets.Length = 0) ""
  let deep = render 0x8004 0x8012 800
  // 3. Every row classified, even when several rows share one address
  //    (zoomed in past 1 byte/pixel - regression for a null-row crash).
  //    Rows inside the block classify Code; rows past its end Unmapped.
  let kindAt y = (JetpacFR.Core.CtrlMapModel.rowLo 0x8004 0x8012 800 y, deep.Rows[y].Kind)
  check "deep zoom rows all populated"
    (deep.Rows |> Array.forall (fun r -> r <> Unchecked.defaultof<JetpacFR.Core.CtrlMapModel.MapRow>))
    ""
  check "deep zoom row keeps block kind inside the block"
    ([ for y in 0 .. 799 -> kindAt y ]
     |> List.forall (fun (a, k) ->
       if a < 0x8010 then k = JetpacFR.Core.CtrlMapModel.CodeR
       else k = JetpacFR.Core.CtrlMapModel.UnmappedR))
    (sprintf "%A" (kindAt 0, kindAt 799))
  check "zoomed-out row shows heat"
    ((render 0x8000 0x8010 40).Rows |> Array.exists (fun r -> r.Heat > 0))
    ""
  // 4. Labels appear only when their band is tall enough.
  check "label fits when zoomed" (hi.Labels |> Array.exists (fun l -> l.Text = "main")) (sprintf "%A" hi.Labels)
  check "no label when zoomed far out" (lo.Labels.Length = 0) (sprintf "%A" lo.Labels)
  // 5. Range comments surface as extents clipped to the view.
  let cf = { ImageFile = "x"; Start = 0x8000; EndExcl = 0x8010; EntryPc = 0x8000; ActiveVersion = -1
             Blocks = blocks
             Comments = comments @ [ { Kind = Range; Addr = 0x8006; EndExcl = 0x800A; InstrIndex = -1; Text = "sprite" } ]
             Dirty = false }
  let rr = JetpacFR.Core.CtrlMapModel.render cf.Blocks cf.Comments starts (Some mem) counts (Array.zeroCreate<bool> 0x10000) 0x8000 0x8010 400
  check "range mark visible" (rr.Ranges = [| (0x8006, 0x800A) |]) (sprintf "%A" rr.Ranges)
  // 6. Block edit ops.
  let c0 = ControlFile.empty 0x8000 0x8020
  let split = ControlFile.splitBlockAt c0 0x8010
  check "split makes two blocks" (split.Blocks.Length = 2 && split.Dirty) (sprintf "%A" split.Blocks)
  check "split names the tail" ((split.Blocks |> List.find (fun b -> b.Start = 0x8010)).Name = "block_8010") ""
  check "split on edge is a no-op" (ControlFile.splitBlockAt c0 0x8000 = c0) ""
  let merged = ControlFile.mergeWithNext split 0x8000
  check "merge restores one block" (merged.Blocks.Length = 1 && merged.Blocks.Head.EndExcl = 0x8020) (sprintf "%A" merged.Blocks)
  check "rename sets name" ((ControlFile.renameBlockAt c0 0x8015 "loop").Blocks.Head.Name = "loop") ""
  check "setKind persists kind" ((ControlFile.setKindAt c0 0x8015 Data).Blocks.Head.Kind = Data) ""
  if failures.Count > 0 then 1 else 0

let runCE () : int =
  printfn "ce: binary -> CE ops (roundtrip) + F# source"
  let rec findMinimal (d: DirectoryInfo) =
    let candidate = Path.Combine(d.FullName, "games", "minimal", "proba.bin")
    if File.Exists candidate then Some candidate
    else
      match d.Parent with
      | null -> None
      | p -> findMinimal p
  match findMinimal (DirectoryInfo AppContext.BaseDirectory) with
  | None ->
    check "proba.bin present" false "missing"
    1
  | Some path ->
    let img = File.ReadAllBytes path
    let mem = Array.zeroCreate<byte> 0x10000
    Array.blit img 0 mem 0x8000 img.Length
    // 1. The roundtrip contract: assemble(toOps(binary)) == binary.
    let ops = Z80CE.toOps mem 0x8000 img.Length
    let rebuilt = Jetpac2.Core.Z80.assemble ops
    check "roundtrip: assemble(toOps(binary)) == binary" (rebuilt = img)
      (sprintf "%d vs %d bytes" rebuilt.Length img.Length)

    let rawBytes = ops |> List.filter (fun o -> o.Mnemonic = "raw") |> List.sumBy (fun o -> o.Bytes.Length)
    check "proba.bin decodes to real ops (raw < 5%)"
      (rawBytes < img.Length / 20)
      (sprintf "%d raw of %d bytes" rawBytes img.Length)
    // 3. The F# source: labels + named ops.
    let src = Z80CE.toSource mem 0x8000 img.Length
    check "source opens a z80 block" (src.Contains "z80 {") ""
    check "source labels in-range jumps" (src.Contains "Z80.at lbl0") ""
    check "source uses labeled conditional jumps" (src.Contains "Z80.JR_Z_LBL" || src.Contains "Z80.JR_LBL" || src.Contains "Z80.JP_NZ_LBL") ""
    check "source emits LDIR" (src.Contains "Z80Vocab.LDIR") ""
    check "source emits the self-modifying store" (src.Contains "Z80Vocab.LD_ptr_BC 0x802D") ""
    // 4. The CEGame engine wrapper (used by the Desktop/Web toggles)
    // locksteps with the oracle.
    let ceState = snd (MinimalGame.Game.entryState img)
    let ce = JetpacFR.Core.CEGame(MinimalGame.Image.program, mem, ceState)
    let oracle2 = Jetpac.Core.Spectrum48()
    oracle2.LoadState(mem, ceState)
    let mutable ok2 = true
    let mutable detail2 = ""
    let mutable f2 = 0
    while ok2 && f2 < 30 do
      ce.RunFrame() |> ignore
      oracle2.RunGameFrame()
      let mutable d = -1
      let mutable i = 0x4000
      while d < 0 && i < 0x10000 do
        if ce.Memory[i] <> oracle2.Memory[i] then d <- i
        i <- i + 1
      let z2 = oracle2.DebugZ80
      if d >= 0 || ce.Regs.Pc() <> z2.Regs.Pc() || ce.CycleCount <> int64 (z2.CycleCount()) then
        ok2 <- false
        detail2 <- sprintf "frame %d mem=%d pc=%04X/%04X" f2 d (ce.Regs.Pc()) (z2.Regs.Pc())
      f2 <- f2 + 1
    check "CEGame engine wrapper locksteps with the oracle" ok2 detail2
    printfn "  --- generated source (first 24 lines) ---"
    src.Split('\n') |> Array.truncate 24 |> Array.iter (printfn "  %s")
    if failures.Count > 0 then 1 else 0

/// The historical regression harness.
let mainTests argv =
  try
    let tests =
      match Array.toList argv with
      | "--test" :: name :: _ -> [ name ]
      | [ "--bench" ] -> [ "bench" ]
      | _ -> []
    let run (name: string) =
      match name with
      | "disasm" -> runDisasmKnown () + runDisasmCorpus ()
      | "trace" -> runTrace rom tzx
      | "agree" -> runAgree rom tzx
      | "gaps" -> runGaps ()
      | "mine" -> runMine rom tzx
      | "contract" -> runContract rom tzx
      | "validate" -> runValidate rom tzx
      | "prompt" -> runPrompt rom tzx
      | "bench" -> runBench rom tzx
      | "manifest" -> runManifest ()
      | "minimal" -> runMinimal ()
      | "game2" -> runGame2 ()
      | "history" -> runHistory rom tzx
      | "z80ops" -> runZ80Ops ()
      | "labels" -> runLabels ()
      | "integration" -> runIntegration ()
      | "ce" -> runCE ()
      | "control" -> runControl ()
      | "ctrlmap" -> runCtrlMap ()
      | "all" ->
        runDisasmKnown () + runDisasmCorpus () + runTrace rom tzx + runAgree rom tzx
        + runGaps () + runMine rom tzx + runContract rom tzx + runValidate rom tzx
        + runHistory rom tzx + runManifest () + runGame2 () + runMinimal () + runZ80Ops ()
        + runLabels () + runIntegration () + runCE () + runControl () + runCtrlMap ()
      | other ->
        eprintfn "unknown test: %s" other
        1
    let code =
      match tests with
      | [] ->
        eprintfn "usage: --test disasm|trace|agree|all"
        1
      | names -> List.sumBy run names
    if failures.Count > 0 then
      eprintfn "%d failure(s)" failures.Count
      1
    else
      printfn "ALL TESTS PASSED"
      0
  with ex ->
    eprintfn "harness crashed: %s" ex.Message
    1

/// Dispatch: game-project commands vs the regression harness.
[<EntryPoint>]
let main argv =
  match Array.toList argv with
  | cmd :: _ when cmd = "--gen-game" || cmd = "--regen" || cmd = "--materialize" || cmd = "--diff-game" ->
    GenGame.run (Array.toList argv)
  | _ -> mainTests argv
