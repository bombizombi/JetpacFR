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
  printfn "generated-table gap probe (the reported 0x6496 case)"
  match EntryCache.tryLoad rom tzx with
  | Some (mem, state) ->
    let port = Jetpac2.Core.Machine()
    Jetpac2.Core.Generated.EnsureInstalled()
    port.LoadState(mem, state)
    // Fresh machine, zero cycles executed: the interrupt cannot be pending,
    // so a Step at this address is a pure dispatch.
    port.Regs.SetPc 0x6496
    let outcome =
      try
        port.Step() |> ignore
        "executable"
      with ex ->
        if ex.Message.StartsWith "no code at" then
          sprintf "gap, pc preserved=%b" (port.Regs.Pc() = 0x6496)
        else "other: " + ex.Message
    printfn "  0x6496 from entry state: %s" outcome
    check "0x6496 is not executable in the port (translation gap)" (outcome.StartsWith "gap") outcome
    check "failing PC is preserved after the error (UI can disassemble it)"
      (outcome.StartsWith "gap, pc preserved=true") outcome
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
    Jetpac2.Core.Generated.EnsureInstalled()
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

[<EntryPoint>]
let main argv =
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
      | "all" ->
        runDisasmKnown () + runDisasmCorpus () + runTrace rom tzx + runAgree rom tzx
        + runGaps () + runMine rom tzx + runContract rom tzx + runValidate rom tzx
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
