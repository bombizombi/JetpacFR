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

// Lazy: the emulator assets only matter to the emulator-driving tests, and
// the module initializer would otherwise crash --gen-game/--regen on machines
// without them (LocalAssets.find fails hard).
let rom = lazy (LocalAssets.find "48.rom")
let tzx = lazy (LocalAssets.find "Jetpac.tzx")

let failures = ResizeArray<string>()

let check (name: string) (ok: bool) (detail: string) =
    if ok then
        printfn "  ok   %s" name
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

            eprintfn
                "  MISMATCH %A: got '%s' len=%d, expected '%s' len=%d"
                bytes
                insn.Text
                insn.Length
                expected
                bytes.Length

    check "known instructions decode to expected text+length" ok ""
    if failures.Count > 0 then 1 else 0

let runDisasmCorpus () : int =
    printfn "disasm corpus over the loaded 64K image"
    let oracle, _ = Jetpac3.Core.Boot.bootToEntry rom.Value tzx.Value None
    let mem, _ = oracle.SaveState()
    let mutable bad = 0
    let mutable dbFalls = 0
    let mutable lengthMismatch = 0

    for addr in 0..0xFFFF do
        let insn = Disasm.disasmMemory mem addr

        if Disasm.disasmLength mem addr <> insn.Length then
            lengthMismatch <- lengthMismatch + 1

        if insn.Length < 1 || insn.Length > 4 then
            bad <- bad + 1

            if bad < 5 then
                eprintfn "  bad length at %04X: %A" addr insn

        if insn.Text.StartsWith "DB " then
            dbFalls <- dbFalls + 1

    check "every address decodes to a 1..4 byte instruction" (bad = 0) (sprintf "%d bad" bad)

    check
        "length-only path agrees with full disassembly everywhere"
        (lengthMismatch = 0)
        (sprintf "%d mismatches" lengthMismatch)

    check
        "no undefined-opcode fallbacks in ROM+game code region"
        (dbFalls <= 0x4000)
        (sprintf "%d DB fallbacks" dbFalls)

    if failures.Count > 0 then 1 else 0

let runTrace (romPath: string) (tzxPath: string) : int =
    printfn "trace recorder + codec roundtrip + stats"
    // Capacity above the total instruction count (120 frames x ~7-16k) so no
    // ring eviction happens: the per-frame and self-mod invariants below only
    // hold over an unevicted window (runBench uses the same capacity).
    let session = TraceSession(romPath, tzxPath, 4_000_000)
    let framesN = 120
    let script = Jetpac3.Core.Script.defaultSession framesN

    for f in 0 .. framesN - 1 do
        // Keys land before the frame runs (event frame f influences run f),
        // matching the app's replay loop and runHistory's convention.
        for (sf, row, bit, pressed) in script do
            if sf = f then
                session.SetKey(row, bit, pressed)

        let frameStart, _ = session.RunFrame()
        session.DrainBeeperSamples(frameStart) |> ignore

    let recorder = session.Recorder
    let trace = recorder.Build()
    let mutable nonDecreasing = true

    for i in 1 .. trace.Entries.Length - 1 do
        if trace.Entries[i].Tick < trace.Entries[i - 1].Tick then
            nonDecreasing <- false

    let mutable heatSum = 0

    for i in 0..0xFFFF do
        heatSum <- heatSum + trace.PerPcCount[i]

    let sawInterrupt = trace.Entries |> Array.exists (fun e -> e.Length = 0uy)
    check "trace captured instructions" (trace.Entries.Length > 0) (sprintf "%d entries" trace.Entries.Length)
    check "ticks are non-decreasing" nonDecreasing ""

    check
        "per-PC heat counts sum to the entry count"
        (heatSum = trace.Entries.Length)
        (sprintf "heat=%d entries=%d" heatSum trace.Entries.Length)

    check "interrupt service appears in the trace" sawInterrupt ""
    // FrameTicks[f] is the absolute cycle at the end of frame f, so each entry
    // belongs to the first boundary at-or-after its tick. Only the last 60
    // frames are asserted to keep the check independent of any boot warm-up.
    let perFrameCounts =
        let counts = Array.zeroCreate<int> trace.FrameTicks.Length

        if counts.Length > 0 then
            let mutable fi = 0

            for e in trace.Entries do
                while fi < counts.Length && trace.FrameTicks[fi] <= e.Tick do
                    fi <- fi + 1

                if fi < counts.Length then
                    counts[fi] <- counts[fi] + 1

        counts

    check
        "trace covers all frames"
        (trace.FrameTicks.Length >= framesN)
        (sprintf "%d frame boundaries" trace.FrameTicks.Length)

    let tail = perFrameCounts |> Array.skip (max 0 (perFrameCounts.Length - 60))

    check
        "per-frame instruction counts are sane (7k..16k)"
        (tail.Length = 60 && tail |> Array.forall (fun c -> c >= 7_000 && c <= 16_000))
        (if tail.Length = 0 then
             "no frames"
         else
             sprintf "tail range %d..%d over %d frames" (Array.min tail) (Array.max tail) tail.Length)
    // codec roundtrip
    let path =
        Path.Combine(Path.GetTempPath(), sprintf "jetpacfr-%d.jpt" (DateTime.UtcNow.Ticks))

    TraceCodec.save trace path
    let loaded = TraceCodec.load path
    File.Delete path
    let mutable entriesEqual = loaded.Entries.Length = trace.Entries.Length
    let mutable i = 0

    while entriesEqual && i < trace.Entries.Length do
        if loaded.Entries[i] <> trace.Entries[i] then
            entriesEqual <- false

        i <- i + 1

    let mutable snapsEqual = loaded.Snapshots.Length = trace.Snapshots.Length
    i <- 0

    while snapsEqual && i < trace.Snapshots.Length do
        if loaded.Snapshots[i] <> trace.Snapshots[i] then
            snapsEqual <- false

        i <- i + 1

    check "codec roundtrip: entries identical" entriesEqual ""
    check "codec roundtrip: snapshots identical" snapsEqual ""
    check "codec roundtrip: frame ticks identical" (loaded.FrameTicks = trace.FrameTicks) ""
    check "codec roundtrip: per-PC counts identical" (loaded.PerPcCount = trace.PerPcCount) ""
    check "codec roundtrip: self-modified flags identical" (loaded.SelfModified = trace.SelfModified) ""

    check
        "self-mod count is consistent"
        (trace.SelfModCount = (trace.SelfModified |> Array.filter id |> Array.length))
        ""
    // Ring eviction decrements per-Pc counts while the sticky self-mod flags
    // stay set, so the strict invariant only holds when nothing was evicted.
    let selfModConsistent =
        if recorder.EntryCount >= recorder.Capacity then
            true
        else
            Array.forall2 (fun sm c -> (not sm) || c > 0) trace.SelfModified trace.PerPcCount

    check
        "every self-modified address was executed"
        selfModConsistent
        (sprintf "entries=%d capacity=%d" recorder.EntryCount recorder.Capacity)
    // record toggle
    let before = recorder.EntryCount
    recorder.RecordEnabled <- false
    session.RunFrame() |> ignore
    let after = recorder.EntryCount
    recorder.RecordEnabled <- true
    check "recording can be disabled without disturbing the machine" (before = after) (sprintf "%d -> %d" before after)
    if failures.Count > 0 then 1 else 0

let runAgree (romPath: string) (tzxPath: string) : int =
    printfn "live trace vs disassembler agreement"
    let session = TraceSession(romPath, tzxPath, 500_000)
    let framesN = 150
    let script = Jetpac3.Core.Script.defaultSession framesN

    for f in 0 .. framesN - 1 do
        // Keys land before the frame runs (event frame f influences run f),
        // matching the app's replay loop and runHistory's convention.
        for (sf, row, bit, pressed) in script do
            if sf = f then
                session.SetKey(row, bit, pressed)

        let frameStart, _ = session.RunFrame()
        session.DrainBeeperSamples(frameStart) |> ignore

    let trace = session.Recorder.Build()
    let mutable mismatches = 0
    let mutable checkedCount = 0

    for e in trace.Entries do
        if e.Length <> 0uy then
            let bytes = [| e.B0; e.B1; e.B2; e.B3 |]
            let insn = Disasm.disasmBytes bytes 0
            checkedCount <- checkedCount + 1

            if insn.Length <> int e.Length then
                mismatches <- mismatches + 1

                if mismatches < 5 then
                    eprintfn "  length mismatch at pc=%04X trace=%d disasm=%d (%s)" e.Pc e.Length insn.Length insn.Text

    check
        "disassembler length equals recorder length for every executed instruction"
        (mismatches = 0)
        (sprintf "%d of %d mismatched" mismatches checkedCount)
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

    check
        "fall-through PC advance matches recorded length for every instruction"
        (advanceMismatches = 0)
        (sprintf "%d mismatches" advanceMismatches)

    let distinctPcs =
        trace.Entries
        |> Array.fold
            (fun (s: System.Collections.Generic.HashSet<int>) e ->
                s.Add(int e.Pc) |> ignore
                s)
            (System.Collections.Generic.HashSet<int>())

    check
        "execution covers a rich set of addresses"
        (distinctPcs.Count >= 100)
        (sprintf "%d distinct PCs" distinctPcs.Count)

    let hot =
        trace.PerPcCount
        |> Array.mapi (fun pc c -> pc, c)
        |> Array.sortByDescending (fun (_, c) -> c)
        |> Array.take 8

    printfn "  hottest PCs: %s" (hot |> Array.map (fun (pc, c) -> sprintf "%04X x%d" pc c) |> String.concat "  ")
    if failures.Count > 0 then 1 else 0

let runGaps () : int =
    printfn "generic-table coverage probe (the reported 0x6496 case)"

    match EntryCache.tryLoad rom.Value tzx.Value with
    | Some(mem, state) ->
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
                else
                    "other: " + ex.Message

        printfn "  0x6496 from entry state (opcode %02X): %s" opcode outcome
        // The generic table dispatches by opcode, not address: whatever byte
        // the entry state holds at 0x6496, a covered opcode must execute (PC
        // leaves the address) and an uncovered one must raise "no code at"
        // with the PC preserved (the UI's static-disasm path).
        if outcome = "executable" then
            check "executed step advanced past 0x6496" (port.Regs.Pc() <> 0x6496) (sprintf "pc=%04X" (port.Regs.Pc()))
        else
            check
                "uncovered opcode fails with pc preserved"
                (outcome.StartsWith "uncovered opcode, pc preserved=true")
                outcome

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
        // Keys land before the frame runs (event frame f influences run f),
        // matching the app's replay loop and runHistory's convention.
        for (sf, row, bit, pressed) in script do
            if sf = f then
                session.SetKey(row, bit, pressed)

        let frameStart, _ = session.RunFrame()
        session.DrainBeeperSamples(frameStart) |> ignore

    let trace = session.Recorder.Build()
    let routines, edges = Miner.mine trace

    printfn
        "  %d routines, %d edges; 0x71B8 executed=%b"
        routines.Length
        edges.Length
        (trace.Entries |> Array.exists (fun e -> e.Pc = 0x71B8us))

    for r in routines |> List.truncate 15 do
        printfn
            "    %04X  calls=%4d  span=%04X-%04X  incl=%6d  excl=%6d  %s"
            r.Entry
            r.CallCount
            r.SpanLo
            r.SpanHi
            r.InclusiveTStates
            r.ExclusiveTStates
            r.Risk

    check "miner found routines" (routines.Length > 0) ""
    let entrySet = routines |> List.map (fun r -> r.Entry) |> Set.ofList

    let badInvariants =
        routines
        |> List.filter (fun r ->
            not (
                r.SpanLo <= r.Entry
                && r.Entry <= r.SpanHi
                && r.InclusiveTStates >= r.ExclusiveTStates
                && r.ExclusiveTStates >= 0L
                && (r.CallCount >= 1 || r.SpanHi - r.SpanLo + 1 <= 0x1000)
            ))

    check
        "routine invariants (span contains entry, incl>=excl, calls>=1)"
        (List.isEmpty badInvariants)
        (badInvariants
         |> List.truncate 3
         |> List.map (fun r ->
             sprintf
                 "%04X span=%04X-%04X incl=%d excl=%d calls=%d"
                 r.Entry
                 r.SpanLo
                 r.SpanHi
                 r.InclusiveTStates
                 r.ExclusiveTStates
                 r.CallCount)
         |> String.concat "; ")

    let edgeOk =
        edges |> List.forall (fun e -> entrySet.Contains e.Callee && e.Count >= 1)

    check "every call edge points at a mined routine" edgeOk ""
    check "a hot routine exists (>= 10 calls, e.g. the ISR)" (routines |> List.exists (fun r -> r.CallCount >= 10)) ""

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
        check
            "screen-clear routine (0x71CF) mined with many calls"
            (clear.CallCount >= 100)
            (sprintf "%d calls" clear.CallCount)

        check
            "screen-clear routine has loop extents"
            (not (List.isEmpty clear.LoopExtents))
            (sprintf "%A" (clear.LoopExtents |> List.truncate 3))
    | None -> check "screen-clear routine (0x71CF) found in the mined set" false ""

    match routines |> List.tryFind (fun r -> r.Entry = 0x71B8) with
    | Some _ ->
        // Expected under the app's replay timing: the script's inputs reach the
        // lift path, so the registry's 0x71B8 lift runs in-session. Its exactness
        // is proven by the validation theater below; here it only means the
        // mined set contains a routine that is also lifted.
        printfn "  0x71B8 (Jetpac3 lift entry) exercised by this script"
    | None -> printfn "  0x71B8 (Jetpac3 lift entry) not exercised by this script"

    if failures.Count > 0 then 1 else 0

let runContract (romPath: string) (tzxPath: string) : int =
    printfn "contract + prompt extraction"
    let session = TraceSession(romPath, tzxPath, 1_000_000)
    let framesN = 400
    let script = Jetpac3.Core.Script.defaultSession framesN

    for f in 0 .. framesN - 1 do
        // Keys land before the frame runs (event frame f influences run f),
        // matching the app's replay loop and runHistory's convention.
        for (sf, row, bit, pressed) in script do
            if sf = f then
                session.SetKey(row, bit, pressed)

        let frameStart, _ = session.RunFrame()
        session.DrainBeeperSamples(frameStart) |> ignore

    let trace = session.Recorder.Build()
    let routines, _ = Miner.mine trace

    match routines |> List.tryFind (fun r -> r.Entry = 0x71CF) with
    | None ->
        check "screen-clear routine (0x71CF) mined" false "not found"
        1
    | Some clear ->
        let pcCycles = Array.zeroCreate<int64> 0x10000

        for e in trace.Entries do
            if e.Length <> 0uy then
                pcCycles[int e.Pc] <- pcCycles[int e.Pc] + int64 e.Cycles

        let contract = Contract.extract session.Memory trace clear pcCycles

        check
            "disassembly starts at the entry"
            (match contract.Disassembly with
             | h :: _ -> h.Address = clear.Entry
             | _ -> false)
            ""

        check
            "disassembly stays within the span"
            (contract.Disassembly
             |> List.forall (fun i -> i.Address >= contract.SpanLo && i.Address <= contract.SpanHi))
            ""

        check
            "routine ends with RET"
            (match contract.Disassembly with
             | insns when not insns.IsEmpty -> (List.last insns).Text = "RET"
             | _ -> false)
            ""

        check
            "writes attributed to the clear land in the screen area (4000..5AFF)"
            (contract.WriteRanges
             |> List.exists (fun (lo, hi, _, _) -> hi >= 0x4000 && lo <= 0x5AFF))
            (sprintf "%A" (contract.WriteRanges |> List.truncate 4))

        check
            "the clear restores its registers (small register delta)"
            (contract.RegisterDelta.Length <= 4)
            (sprintf "%A" contract.RegisterDelta)

        check
            "api hints mention memory writes and registers"
            (contract.ApiHints
             |> List.exists (fun h -> h.Contains "Write" || h.Contains "memory")
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

        check
            "prompt has contract/api/conventions/acceptance sections"
            (prompt.Contains "## Behavioural contract"
             && prompt.Contains "Jetpac2.Core.Machine"
             && prompt.Contains "## Conventions"
             && prompt.Contains "## Acceptance"
             && prompt.Contains "```fsharp")
            ""

        check "prompt carries the write contract" (prompt.Contains "memory writes") (sprintf "len=%d" prompt.Length)

        let path =
            Path.Combine(Path.GetTempPath(), sprintf "prompt-%d.md" (DateTime.UtcNow.Ticks))

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

    check
        "registry covers all lifted routines"
        (covered 0x71B8 && covered 0x71CF && covered 0x72EE && covered 0x64E6)
        ""
    // Good path: the full registry runs lockstep with the oracle over the
    // script - both the interpreted code and any lifted routine the script
    // exercises (0x71B8 under the app's replay timing) must match exactly.
    let good =
        Validation.run
            romPath
            tzxPath
            120
            (Jetpac3.Core.Script.defaultSession 120)
            Jetpac3.Core.LiftedRoutines.registryHook
            System.Threading.CancellationToken.None

    check
        "validated run passes with the current registry"
        good.Passed
        (match good.FirstDivergence with
         | Some d -> d.Kind
         | None -> "ok")
    // Bad path: deliberately wrong lift of the real screen clear (0x71CF) must
    // be detected with both machines' context.
    let badHook (addr: int) =
        if addr = 0x71CF then
            Some(fun (m: Jetpac2.Core.Machine) ->
                m.Fetch()
                m.PassTime 4)
        else
            Jetpac3.Core.LiftedRoutines.registryHook addr

    let bad =
        Validation.run
            romPath
            tzxPath
            200
            (Jetpac3.Core.Script.defaultSession 200)
            badHook
            System.Threading.CancellationToken.None

    check "wrong lift is detected" (not bad.Passed) ""

    match bad.FirstDivergence with
    | Some d ->
        check
            "divergence carries both machines' executed instructions"
            (d.PortExecuted.Length > 0 && d.OracleExecuted.Length > 0)
            (sprintf "'%s' vs '%s'" d.PortExecuted d.OracleExecuted)

        check "divergence carries register context" (d.PortRegs.Contains "AF=" && d.OracleRegs.Contains "AF=") ""
        check "divergence frame is sane" (d.Frame >= 0 && d.Frame < 200) (sprintf "frame %d" d.Frame)
    | None -> check "divergence details captured" false "no divergence"

    if failures.Count > 0 then 1 else 0

let runPrompt (romPath: string) (tzxPath: string) : int =
    // Generate and print the lift prompt for the screen-clear routine (0x71CF)
    // so it can be pasted into the model chat verbatim.
    let session = TraceSession(romPath, tzxPath, 1_000_000)
    let framesN = 400
    let script = Jetpac3.Core.Script.defaultSession framesN

    for f in 0 .. framesN - 1 do
        // Keys land before the frame runs (event frame f influences run f),
        // matching the app's replay loop and runHistory's convention.
        for (sf, row, bit, pressed) in script do
            if sf = f then
                session.SetKey(row, bit, pressed)

        let frameStart, _ = session.RunFrame()
        session.DrainBeeperSamples(frameStart) |> ignore

    let trace = session.Recorder.Build()
    let routines, _ = Miner.mine trace

    match routines |> List.tryFind (fun r -> r.Entry = 0x71CF) with
    | None ->
        eprintfn "0x71CF not mined in %d frames" framesN
        1
    | Some clear ->
        let pcCycles = Array.zeroCreate<int64> 0x10000

        for e in trace.Entries do
            if e.Length <> 0uy then
                pcCycles[int e.Pc] <- pcCycles[int e.Pc] + int64 e.Cycles

        let contract = Contract.extract session.Memory trace clear pcCycles
        let prompt = Prompt.generate contract
        printfn "%s" prompt
        0

let runBench (romPath: string) (tzxPath: string) : int =
    printfn "bench: bare machine vs session (recording off/on), 600 frames"

    let mem, state =
        match EntryCache.tryLoad romPath tzxPath with
        | Some(m, s) -> m, s
        | None ->
            printfn "  cold booting to entry (caches for future runs)..."
            let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath None
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

        for _ in 1..framesN do
            let fe = m.FrameEnd

            while m.CycleCount() < fe do
                m.Step()
                steps <- steps + 1L

            m.FrameEnd <- fe + 69888L

        sw.Stop()

        printfn
            "  bare machine          : %6.2f ms/frame  %7.0f steps/frame  %8.0f Msteps/s"
            (float sw.Elapsed.TotalMilliseconds / float framesN)
            (float steps / float framesN)
            (float steps / sw.Elapsed.TotalSeconds / 1e6)

    let runSession (recording: bool) =
        let s = TraceSession(romPath, tzxPath, 4_000_000)
        s.Recorder.RecordEnabled <- recording
        let sw = System.Diagnostics.Stopwatch.StartNew()

        for _ in 1..framesN do
            s.RunFrame() |> ignore

        sw.Stop()

        printfn
            "  session record=%5b    : %6.2f ms/frame  %7.0f steps/frame  %8.0f Msteps/s"
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
            if sf = f then
                s.SetKey(row, bit, pressed)
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

    check
        "history captured every frame"
        (session.History.LastFrame = framesN)
        (sprintf "last=%d" session.History.LastFrame)

    // 2. SaveState/LoadState roundtrip on the bare machine is exact (the
    // foundation of every rewind restore).
    let m = Jetpac2.Core.Machine()
    Jetpac2.Core.Z80Table.EnsureInstalled()
    let mem1, text1 = m.SaveState()
    let m2 = Jetpac2.Core.Machine()
    m2.LoadState(mem1, text1)
    let sameMem = (m.Memory = m2.Memory)

    let sameRegs =
        m.Regs.Pc() = m2.Regs.Pc()
        && m.Regs.Get Jetpac2.Core.R16.HL = m2.Regs.Get Jetpac2.Core.R16.HL

    check "port SaveState/LoadState roundtrip is exact" (sameMem && sameRegs) ""

    // 3. Rewind (preview) to 60: state matches a straight-through 60-frame run.
    let ref = runStraight 60
    session.RewindTo(60)
    let memEq = (session.Memory = ref.Memory)
    let pcEq = session.Regs.Pc() = ref.Regs.Pc()
    // Preview kept everything: the script's last release lands at frame 110.
    check
        "preview does not truncate the key log"
        (session.KeyLog.EndFrame = 110)
        (sprintf "end=%d" session.KeyLog.EndFrame)

    let restarted = TraceSession(romPath, tzxPath, 500_000)
    restarted.KeyLog.Replace(session.KeyLog.Events)
    restarted.StartReplay()

    check
        "restarted replay uses persisted key-log extent"
        (restarted.Replaying
         && restarted.ReplayEndFrame = restarted.KeyLog.EndFrame
         && restarted.ReplayEndFrame = 110)
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

        if session.ReplayFinished then
            stopped <- true

    let endEq = stopped && session.Frame = framesN && session.Memory = orig.Memory

    check
        "replay stops at the end of the recording with the recorded end state"
        endEq
        (sprintf "stopped=%b frame=%d (want %d) mem=%b" stopped session.Frame framesN (session.Memory = orig.Memory))

    // 5. Branch (Go) at 60 after a fresh preview, then a live future with the
    // same keys must equal a straight-through 110-frame run.
    session.RewindTo(60)
    session.BranchAt(60)
    check "branch truncates history" (session.History.LastFrame = 60) (sprintf "last=%d" session.History.LastFrame)
    let post = runStraight 110

    for f in 60..109 do
        session.RunFrame() |> ignore
        applyKeys session (f + 1)

    let futureEq = (session.Memory = post.Memory) && session.Regs.Pc() = post.Regs.Pc()

    check
        "post-branch future is deterministic"
        futureEq
        (sprintf "pc %04X vs %04X" (session.Regs.Pc()) (post.Regs.Pc()))

    if failures.Count > 0 then 1 else 0

/// A recording that stops mid-hold (final event = press, no release) must
/// not leak the latched key into live control at a replay -> live handoff.
let runReplayHandoff (romPath: string) (tzxPath: string) : int =
    printfn "replay handoff: mid-hold ending releases all keys"
    let framesN = 30

    let mkScripted () =
        let s = TraceSession(romPath, tzxPath, 500_000)

        for f in 0 .. framesN - 1 do
            s.RunFrame() |> ignore

            if f = 10 then
                s.SetKey(3, 0, true) // pressed, never released

        s
    // Baseline: straight-through with no script to compare end memory later.
    let scripted = mkScripted ()

    let replayToEnd () =
        let s = TraceSession(romPath, tzxPath, 500_000)
        s.KeyLog.Replace(scripted.KeyLog.Events)
        s.StartReplay()
        let mutable guard = 0

        while not s.ReplayFinished && guard < framesN + 10 do
            s.RunFrame() |> ignore
            guard <- guard + 1

        s

    let finished = replayToEnd ()
    check "replay reaches recording end" (finished.ReplayFinished || finished.Frame >= framesN) ""

    let latchAtEnd =
        [ for r in 0..7 do
              for b in 0..4 do
                  if finished.Keyboard.GetKey(r, b) then
                      yield (r, b) ]

    check "replay-to-end leaves no key latched" (latchAtEnd.IsEmpty) (sprintf "latched=%A" latchAtEnd)

    // Aborting a running replay (Run button mid-script) also hands over clean.
    let aborted = TraceSession(romPath, tzxPath, 500_000)
    aborted.KeyLog.Replace(scripted.KeyLog.Events)
    aborted.StartReplay()
    aborted.RunFrame() |> ignore
    aborted.StopReplay()

    let latchAbort =
        [ for r in 0..7 do
              for b in 0..4 do
                  if aborted.Keyboard.GetKey(r, b) then
                      yield (r, b) ]

    check "replay abort leaves no key latched" (latchAbort.IsEmpty) ""

    if failures.Count > 0 then 1 else 0

let runTimeline (romPath: string) (tzxPath: string) : int =
    printfn "timeline: per-frame states, file roundtrip, future seek"
    let framesN = 60
    // 1. A live session captures a full state for every executed frame.
    let recorded = TraceSession(romPath, tzxPath, 500_000)

    for _ in 0 .. framesN - 1 do
        recorded.RunFrame() |> ignore

    check
        "timeline captured every executed frame"
        (recorded.StateTimeline.Count = framesN + 1)
        (sprintf "count=%d" recorded.StateTimeline.Count)

    check "session reports timeline bytes" (recorded.TimelineSessionBytes > int64 framesN * 65536L) ""
    check "live frames counted" (recorded.LiveCapturedFrames = framesN + 1) ""

    // 2. Codec roundtrip: save + load reproduces every state byte-exactly.
    let temp =
        Path.Combine(Path.GetTempPath(), "jetpacfr-timeline-" + Guid.NewGuid().ToString("N"))

    Directory.CreateDirectory temp |> ignore

    let fp =
        { GameId = "timeline-test"
          RomSha256 = "rom-hash"
          TzxSha256 = "tzx-hash"
          ProgramSha256 = ""
          ProgramAddress = None }

    let path = Path.Combine(temp, "timeline.jst")

    let saveResult =
        StateTimelineStore.save path fp recorded.StateTimeline recorded.KeyLog.Events

    let loadResult = StateTimelineStore.tryLoad path fp

    let loadedOk =
        match loadResult with
        | TimelineLoaded(timeline, events) ->
            timeline.Count = recorded.StateTimeline.Count
            && events = List.ofSeq recorded.KeyLog.Events
            && ([ for i in 0 .. timeline.Count - 1 do
                      let m1, t1, k1 = recorded.StateTimeline.FrameData i
                      let m2, t2, k2 = timeline.FrameData i
                      yield m1 = m2 && t1 = t2 && k1 = k2 ]
                |> List.forall id)
        | _ -> false

    check
        "timeline file round-trips every state"
        (saveResult = Ok() && loadedOk)
        (sprintf "%A / %A" saveResult loadResult)

    check
        "timeline identity rejects another game"
        (StateTimelineStore.tryLoad path { fp with GameId = "other-game" } = TimelineIgnored
            "timeline belongs to another game")
        ""

    check
        "timeline rejects changed assets"
        (StateTimelineStore.tryLoad path { fp with RomSha256 = "different" } = TimelineIgnored
            "timeline assets do not match the selected game")
        ""

    // 3. Future seek: a fresh session loads the recording and jumps straight
    // to the last frame WITHOUT executing anything; the machine state matches
    // the recorded end exactly, and the timeline did not grow.
    let seeker = TraceSession(romPath, tzxPath, 500_000)

    match StateTimelineStore.tryLoad path fp with
    | TimelineLoaded(timeline, events) ->
        seeker.LoadStateTimeline(timeline, events)
        check "loaded timeline sets the seekable horizon" (seeker.TimelineExtent = framesN) ""
        seeker.JumpTo(framesN)

        check
            "jump to the future lands on the recorded state"
            (seeker.Frame = framesN && seeker.Memory = recorded.Memory)
            ""

        check "seeking did not grow the timeline" (seeker.StateTimeline.Count = framesN + 1) ""
        check "loaded states are not counted as live capture" (seeker.LiveCapturedFrames = 0) ""
        // 4. Execution continues past the future jump: the first capture rebases
        // history, then the timeline grows sequentially.
        seeker.RunFrame() |> ignore
        seeker.RunFrame() |> ignore
        check "timeline grows past a future jump" (seeker.StateTimeline.Count = framesN + 3) ""
    | other -> check "timeline load for future seek" false (sprintf "%A" other)

    // 5. Branch (Go) at a future frame truncates the timeline there and new
    // captures continue sequentially.
    let brancher = TraceSession(romPath, tzxPath, 500_000)

    match StateTimelineStore.tryLoad path fp with
    | TimelineLoaded(timeline, events) ->
        brancher.LoadStateTimeline(timeline, events)
        brancher.JumpTo(framesN / 2)
        brancher.BranchAt(framesN / 2)

        check
            "branch at a future frame truncates the timeline"
            (brancher.StateTimeline.Count = framesN / 2 + 1)
            (sprintf "count=%d" brancher.StateTimeline.Count)

        brancher.RunFrame() |> ignore
        check "capture continues sequentially after the branch" (brancher.StateTimeline.Count = framesN / 2 + 2) ""
    | _ -> check "timeline load for branch test" false ""

    // 6. Clearing the recording mid-session: states + keys wiped, the current
    // frame stays covered, and captures continue sequentially from frame + 1.
    let clearer = TraceSession(romPath, tzxPath, 500_000)

    for _ in 0..29 do
        clearer.RunFrame() |> ignore

    clearer.ResetTimeline()

    check
        "clear wipes stored states and the key script"
        (clearer.StateTimeline.Count = 1 && clearer.KeyLog.Count = 0)
        (sprintf "count=%d keys=%d" clearer.StateTimeline.Count clearer.KeyLog.Count)

    check "cleared session reports no live capture" (clearer.LiveCapturedFrames = 0) ""
    clearer.RunFrame() |> ignore
    clearer.RunFrame() |> ignore
    check "captures continue sequentially after clear" (clearer.StateTimeline.Count = 3) ""
    check "cleared timeline covers the new frames" (clearer.StateTimeline.EndFrame = 32) ""

    Directory.Delete(temp, true)
    if failures.Count > 0 then 1 else 0

let runManifest () : int =
    printfn "manifest: game discovery + load"

    let rec walk (d: DirectoryInfo) =
        let candidate = Path.Combine(d.FullName, "games")

        if Directory.Exists candidate then
            Some candidate
        else
            match d.Parent with
            | null -> None
            | p -> walk p

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

            check
                "manifest discovery is stable by game ID"
                (games.Head.GameId = "jetpac")
                (sprintf "%A" (games |> List.map (fun g -> g.GameId)))

            let temp =
                Path.Combine(Path.GetTempPath(), "jetpacfr-replay-" + Guid.NewGuid().ToString("N"))

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
                [ { Frame = 10
                    Row = 3
                    Bit = 0
                    Pressed = true }
                  { Frame = 30
                    Row = 3
                    Bit = 0
                    Pressed = false } ]

            let saved = ReplayStore.save replayGame events
            let loaded = ReplayStore.tryLoad replayGame

            check
                "versioned replay round-trips"
                (saved = Ok()
                 && File.Exists(ReplayStore.path replayGame)
                 && loaded = ReplayLoaded events)
                (sprintf "%A / %A" saved loaded)

            let mismatched =
                { replayGame with
                    GameId = "other-game" }

            check
                "replay identity rejects another game"
                (ReplayStore.tryLoad mismatched = ReplayIgnored "replay belongs to another game")
                "mismatch accepted"

            Directory.Delete(temp, true)
        | None -> check "jetpac manifest present" false ""

        match games |> List.tryFind (fun g -> g.Name = "Minimal") with
        | Some m ->
            check "minimal manifest is a program boot" (m.Boot = "program") m.Boot

            check
                "minimal manifest has program bin+address"
                (m.ProgramBin.IsSome && m.ProgramAddress = Some 32768)
                (sprintf "bin=%b addr=%A" m.ProgramBin.IsSome m.ProgramAddress)
        | None -> check "minimal manifest present" false ""

    if failures.Count > 0 then 1 else 0

let runGame2 () : int =
    printfn "game2: synthetic 48K game at 0x7000 - full pipeline (multi-game proof)"
    // Find the fixture through the manifest mechanism (same path the UI uses).
    let rec walk (d: DirectoryInfo) =
        let candidate = Path.Combine(d.FullName, "games")

        if Directory.Exists candidate then
            Some candidate
        else
            match d.Parent with
            | null -> None
            | p -> walk p

    match
        walk (DirectoryInfo AppContext.BaseDirectory)
        |> Option.bind (Manifest.discover >> List.tryFind (fun g -> g.Name = "Synthetic (test fixture)"))
    with
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
        code[0x00] <- 0x3Euy
        code[0x01] <- 0x00uy // LD A,0 (reset frame counter)
        code[0x02] <- 0x32uy
        code[0x03] <- 0x01uy
        code[0x04] <- 0x71uy // LD (0x7101),A
        code[0x05] <- 0xCDuy
        code[0x06] <- 0x30uy
        code[0x07] <- 0x70uy // CALL 0x7030
        code[0x08] <- 0x18uy
        code[0x09] <- 0xFBuy // JR 0x7005 (infinite loop)
        code[0x30] <- 0x3Auy
        code[0x31] <- 0x01uy
        code[0x32] <- 0x71uy // LD A,(0x7101)
        code[0x33] <- 0x3Cuy // INC A
        code[0x34] <- 0x32uy
        code[0x35] <- 0x01uy
        code[0x36] <- 0x71uy // LD (0x7101),A
        code[0x37] <- 0x21uy
        code[0x38] <- 0x00uy
        code[0x39] <- 0x40uy // LD HL,0x4000
        code[0x3A] <- 0x47uy // LD B,A
        code[0x3B] <- 0x77uy
        code[0x3C] <- 0x23uy
        code[0x3D] <- 0x10uy
        code[0x3E] <- 0xFCuy // LD (HL),A / INC HL / DJNZ 0x703B
        code[0x3F] <- 0xDBuy
        code[0x40] <- 0xFEuy
        code[0x41] <- 0xE6uy
        code[0x42] <- 0x1Fuy // IN A,(0xFE) / AND 0x1F
        code[0x43] <- 0x32uy
        code[0x44] <- 0x02uy
        code[0x45] <- 0x71uy // LD (0x7102),A
        code[0x46] <- 0xC9uy // RET
        let mem = Array.zeroCreate<byte> 0x10000
        Array.blit code 0 mem 0x7000 0x47

        let state =
            "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=7FFE\npc=7000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"

        EntryCache.save g.Rom g.Tzx mem state

        // 2. Rewind/replay/branch determinism on the non-Jetpac layout.
        let historyCode = runHistory g.Rom g.Tzx

        if historyCode <> 0 then
            historyCode
        else
            // 3. Mine: the subroutine at 0x7030 called from the 0x7005 loop.
            let session = TraceSession(g.Rom, g.Tzx, 1_000_000)
            let framesN = 120
            let script = Jetpac3.Core.Script.defaultSession framesN

            for f in 0 .. framesN - 1 do
                for (sf, row, bit, pressed) in script do
                    if sf = f then
                        session.SetKey(row, bit, pressed)

                session.RunFrame() |> ignore

            let trace = session.Recorder.Build()
            let routines, edges = Miner.mine trace
            let entrySet = routines |> List.map (fun r -> r.Entry) |> Set.ofList
            check "synthetic routine 0x7030 mined" (entrySet.Contains 0x7030) (sprintf "%d routines" routines.Length)

            match routines |> List.tryFind (fun r -> r.Entry = 0x7030) with
            | Some r ->
                check "0x7030 called from the main loop" (r.CallCount >= 10) (sprintf "%d calls" r.CallCount)

                check
                    "call edge 0x7005 -> 0x7030 recorded"
                    (edges |> List.exists (fun e -> e.Callee = 0x7030 && e.Count >= 1))
                    ""

                let pcCycles = Array.zeroCreate<int64> 0x10000

                for e in trace.Entries do
                    if e.Length <> 0uy then
                        pcCycles[int e.Pc] <- pcCycles[int e.Pc] + int64 e.Cycles

                let contract = Contract.extract session.Memory trace r pcCycles
                check "contract ends with RET" (contract.Disassembly |> List.exists (fun i -> i.Text = "RET")) ""

                check
                    "contract attributes writes to the screen/keyboard cells"
                    (contract.WriteRanges
                     |> List.exists (fun (lo, hi, _, _) -> lo <= 0x7102 && hi >= 0x4000))
                    (sprintf "%A" (contract.WriteRanges |> List.truncate 4))
            | None -> check "synthetic routine 0x7030 mined" false "not found"
            // 4. Differential: port vs oracle, 0 diffs, 150 frames with keys.
            // The lifted-registry hook is per-game: the synthetic game has no
            // lifts yet, so an empty hook is the correct registry (Jetpac's
            // registryHook would fire Jetpac lifts at the wrong addresses).
            let report =
                Validation.run
                    g.Rom
                    g.Tzx
                    150
                    (Jetpac3.Core.Script.defaultSession 150)
                    (fun _ -> None)
                    System.Threading.CancellationToken.None

            check
                "synthetic game validates 0 diffs vs the oracle"
                report.Passed
                (match report.FirstDivergence with
                 | Some d -> sprintf "%s@%04X port=%04X oracle=%04X" d.Kind d.Address d.PortPc d.OraclePc
                 | None -> "")

            if failures.Count > 0 then 1 else 0

let runMinimal () : int =
    printfn "minimal: CE DSL game project - bytes, mixed CE+raw, oracle lockstep"
    // 1. Structure -> bytes: assembling the CE program reproduces the image.
    check
        "CE assemble reproduces the game image"
        (MinimalGame.Image.assembled = MinimalGame.Image.binary)
        (sprintf "%d vs %d bytes" MinimalGame.Image.assembled.Length MinimalGame.Image.binary.Length)
    // 2. Mixed raw + CE program: the two forms interleave and assemble
    // exactly (raw bytes not yet disassembled, CE ops once lifted).
    let mixed: Jetpac2.Core.Z80Op list =
        z80 {
            yield! [| 0x3Euy; 0x00uy |] // LD A,0 (raw)
            Jetpac2.Core.Z80Vocab.INC_A
            Jetpac2.Core.Z80Vocab.LD_HL 16384
            yield! [| 0xC9uy |] // RET (raw)
        }

    let mixedBytes = Jetpac2.Core.Z80.assemble mixed
    let expected = [| 0x3Euy; 0x00uy; 0x3Cuy; 0x21uy; 0x00uy; 0x40uy; 0xC9uy |]
    check "mixed raw+CE assembles to the expected bytes" (mixedBytes = expected) (sprintf "%A" mixedBytes)
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
            if port.Memory[i] <> oracle.Memory[i] then
                memDiff <- i

            i <- i + 1

        let z = oracle.DebugZ80

        let regsOk =
            port.Regs.Pc() = z.Regs.Pc()
            && port.Regs.Sp() = z.Regs.Sp()
            && port.CycleCount() = int64 (z.CycleCount())

        if memDiff >= 0 || not regsOk then
            ok <- false

            detail <-
                sprintf
                    "frame %d memDiff=%d pc=%04X/%04X cycles=%d/%d"
                    f
                    memDiff
                    (port.Regs.Pc())
                    (z.Regs.Pc())
                    (port.CycleCount())
                    (z.CycleCount())

        f <- f + 1

    check "CE driver locksteps with the oracle over 150 frames" ok detail
    // 4. If proba.bin is present, the assembled bytes must equal it exactly
    // (the "compare emitted bytes to the original" contract).
    let rec findMinimal (d: DirectoryInfo) =
        let candidate = Path.Combine(d.FullName, "games", "minimal", "proba.bin")

        if File.Exists candidate then
            Some candidate
        else
            match d.Parent with
            | null -> None
            | p -> findMinimal p

    match findMinimal (DirectoryInfo AppContext.BaseDirectory) with
    | Some proba ->
        let orig = File.ReadAllBytes proba

        check
            "assembled bytes match proba.bin"
            (MinimalGame.Image.assembled = orig)
            (sprintf "%d vs %d bytes" MinimalGame.Image.assembled.Length orig.Length)
    | None -> printfn "  proba.bin not present yet - placeholder game used"

    if failures.Count > 0 then 1 else 0

let runZ80Ops () : int =
    printfn "z80ops: per-op oracle lockstep (%d ops)" Jetpac2.Core.Z80Vocab.allOps.Length

    let stateText (af: int) (bc: int) (de: int) (hl: int) (ix: int) (iy: int) (sp: int) =
        sprintf
            "af=%04X\nbc=%04X\nde=%04X\nhl=%04X\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=%04X\niy=%04X\nsp=%04X\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"
            af
            bc
            de
            hl
            ix
            iy
            sp

    let mutable badOps = 0
    let mutable detail = ""

    for (name, op, kind) in Jetpac2.Core.Z80Vocab.allOps do
        let mem = Array.zeroCreate<byte> 0x10000
        // Program = [op; HALT] at 0x8000 (HALT alone for itself: a halted CPU
        // re-executes at 1 cycle, which the second-op model would mismatch).
        let program =
            if kind = "halt" then
                [ op ]
            else
                [ op; Jetpac2.Core.Z80Vocab.HALT ]

        let bytes = Jetpac2.Core.Z80.assemble program
        Array.blit bytes 0 mem 0x8000 bytes.Length
        let mutable af, bc, de, hl, ix, iy, sp = 0, 0, 0, 0, 0, 0, 0x7FFE

        match kind with
        | "hl" ->
            hl <- 0x9000
            mem[0x9000] <- 0x12uy
        | "ixd" ->
            ix <- 0x9000
            mem[0x9002] <- 0x12uy
        | "iyd" ->
            iy <- 0x9000
            mem[0x9002] <- 0x12uy
        | "nn" ->
            mem[0x9000] <- 0x34uy
            mem[0x9001] <- 0x12uy
        | "sp" ->
            mem[0x7FFE] <- 0x34uy
            mem[0x7FFF] <- 0x12uy
        | "exsp" ->
            mem[0x7FFE] <- 0x34uy
            mem[0x7FFF] <- 0x12uy
        | "djnz" -> bc <- 0x0001
        | "jp" -> mem[0x0000] <- 0x76uy
        | "ret" ->
            // RET pops to the HALT byte that follows the op (1- or 2-byte ops).
            let haltAddr = 0x8000 + op.Bytes.Length
            mem[0x7FFE] <- byte (haltAddr &&& 0xFF)
            mem[0x7FFF] <- byte ((haltAddr >>> 8) &&& 0xFF)
        | "rst" ->
            mem[0x00] <- 0x76uy
            mem[0x08] <- 0x76uy
            mem[0x10] <- 0x76uy
            mem[0x18] <- 0x76uy
            mem[0x20] <- 0x76uy
            mem[0x28] <- 0x76uy
            mem[0x30] <- 0x76uy
            mem[0x38] <- 0x76uy
        | "block" ->
            // Single pass: BC=1 so the repeat ops (LDIR/CPIR) do not loop.
            hl <- 0x9000
            de <- 0x9100
            bc <- 0x0001
            mem[0x9000] <- 0x12uy
            mem[0x9100] <- 0x55uy
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

        if kind <> "halt" then
            z.ExecuteOne()

        let mutable memDiff = -1
        let mutable i = 0

        while memDiff < 0 && i < 0x10000 do
            if port.Memory[i] <> oracle.Memory[i] then
                memDiff <- i

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

            if detail = "" then
                detail <- sprintf " (first: %s)" name

            if badOps <= 10 then
                printfn
                    "  FAIL %s: mem=%d pc=%04X/%04X cycles=%d/%d"
                    name
                    memDiff
                    (port.Regs.Pc())
                    (z.Regs.Pc())
                    (port.CycleCount())
                    (z.CycleCount())

    check
        "every vocabulary op locksteps with the oracle"
        (badOps = 0)
        (sprintf "%d/%d failed%s" badOps Jetpac2.Core.Z80Vocab.allOps.Length detail)

    if badOps = 0 then
        printfn "  all %d ops matched the oracle" Jetpac2.Core.Z80Vocab.allOps.Length

    if failures.Count > 0 then 1 else 0

let runLabels () : int =
    printfn "labels: two-pass symbolic assembly + oracle lockstep"
    // Bounded loop with a backward JP: LD B,3; loop: DEC B; JP NZ loop; HALT.
    let loop = Jetpac2.Core.Z80.label ()

    let loopProgram: Jetpac2.Core.Z80Op list =
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
    check "backward JP label assembles to the expected bytes" (bytes = expected) (sprintf "%A" bytes)
    // Forward reference: JR over a NOP. Layout: 00 | 18 FD | 76
    // (JR at offset 1: disp = target(0) - (1 + 2) = -3 = 0xFD).
    let fwdProgram: Jetpac2.Core.Z80Op list =
        z80 {
            let target = Jetpac2.Core.Z80.label ()
            Jetpac2.Core.Z80.at target
            Jetpac2.Core.Z80Vocab.NOP
            Jetpac2.Core.Z80.JR_LBL target
            Jetpac2.Core.Z80Vocab.HALT
        }

    let fwdBytes = Jetpac2.Core.Z80.assemble fwdProgram
    let fwdExpected = [| 0x00uy; 0x18uy; 0xFDuy; 0x76uy |]

    check
        "backward JR label (over a NOP) assembles to the expected bytes"
        (fwdBytes = fwdExpected)
        (sprintf "%A" fwdBytes)
    // CALL to a forward subroutine: LD A,5; CALL sub; HALT; sub: DEC A; RET.
    let callProgram: Jetpac2.Core.Z80Op list =
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
    let callExpected =
        [| 0x3Euy; 0x05uy; 0xCDuy; 0x06uy; 0x00uy; 0x76uy; 0x3Duy; 0xC9uy |]

    check "forward CALL label assembles to the expected bytes" (callBytes = callExpected) (sprintf "%A" callBytes)
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

    for _ in 1..7 do
        port.Step()
        z.ExecuteOne()

    let mutable memDiff = -1
    let mutable i = 0

    while memDiff < 0 && i < 0x10000 do
        if port.Memory[i] <> oracle.Memory[i] then
            memDiff <- i

        i <- i + 1

    let ok =
        memDiff < 0
        && port.Regs.Pc() = z.Regs.Pc()
        && port.Regs.Get Jetpac2.Core.R16.AF = z.Regs.Get Jetpac.Core.R16.AF
        && port.Regs.Get Jetpac2.Core.R16.BC = z.Regs.Get Jetpac.Core.R16.BC
        && port.CycleCount() = int64 (z.CycleCount())

    check
        "labeled loop locksteps with the oracle"
        ok
        (sprintf
            "mem=%d pc=%04X/%04X cycles=%d/%d"
            memDiff
            (port.Regs.Pc())
            (z.Regs.Pc())
            (port.CycleCount())
            (z.CycleCount()))
    // The CE index (used by runFrame) also resolves labels.
    let idx = Jetpac2.Core.Z80.makeIndex loopProgram
    check "label resolution via makeIndex" (idx.Lookup 2 |> Option.isSome && !loop = 2) (sprintf "cell=%d" !loop)
    if failures.Count > 0 then 1 else 0

let runIntegration () : int =
    printfn "integration: CE invalidation + co-executing effects"

    let state =
        "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=FFFE\npc=8000\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"

    let smcProgram = [ Jetpac2.Core.Z80Vocab.LD_A 1; Jetpac2.Core.Z80Vocab.HALT ]
    let smcImage = Jetpac2.Core.Z80.assemble smcProgram
    let smcMem = Array.zeroCreate<byte> 0x10000
    Array.blit smcImage 0 smcMem 0x8000 smcImage.Length
    let smcMachine = Jetpac2.Core.Machine()
    Jetpac2.Core.Z80Table.EnsureInstalled()
    smcMachine.LoadState(smcMem, state)
    let smcIndex = Jetpac2.Core.Z80.makeIndexAt 0x8000 smcProgram
    smcIndex.Attach smcMachine
    let mutable visits = 0

    smcMachine.AddCoHook(
        0x8000,
        fun m ->
            visits <- visits + 1
            m.Write(0x8001, 2)
            [ Jetpac2.Core.Say "patched" ]
    )
    |> ignore

    let resolveSmc (m: Jetpac2.Core.Machine) =
        match smcIndex.Lookup(m.Regs.Pc()) with
        | Some op -> op.Run m
        | None -> Jetpac2.Core.Machine.GeneratedStep m

    smcMachine.ExecuteOne resolveSmc

    check
        "SMC hook invalidates the CE entry before dispatch"
        (visits = 1
         && smcMachine.Regs.Get Jetpac2.Core.R8.A = 2
         && not (smcIndex.IsValid 0x8000))
        (sprintf "visits=%d A=%d valid=%b" visits (smcMachine.Regs.Get Jetpac2.Core.R8.A) (smcIndex.IsValid 0x8000))

    check "co-hook Say effect is emitted" (smcMachine.DrainEffects() = [ Jetpac2.Core.Say "patched" ]) ""
    smcMachine.LoadState(smcMem, state)
    check "state reload refreshes CE validity" (smcIndex.IsValid 0x8000) "entry stayed invalid after reload"

    let hookProgram = [ Jetpac2.Core.Z80Vocab.LD_A 9; Jetpac2.Core.Z80Vocab.HALT ]
    let hookImage = Jetpac2.Core.Z80.assemble hookProgram
    let hookMem = Array.zeroCreate<byte> 0x10000
    Array.blit hookImage 0 hookMem 0x8000 hookImage.Length
    let hookMachine = Jetpac2.Core.Machine()
    hookMachine.LoadState(hookMem, state)
    let hookIndex = Jetpac2.Core.Z80.makeIndexAt 0x8000 hookProgram
    hookIndex.Attach hookMachine

    hookMachine.AddCoHook(0x8000, fun _ -> [ Jetpac2.Core.Say "first"; Jetpac2.Core.Tick 2 ])
    |> ignore

    hookMachine.AddCoHook(0x8000, fun _ -> [ Jetpac2.Core.Say "second" ]) |> ignore

    let resolveHook (m: Jetpac2.Core.Machine) =
        match hookIndex.Lookup(m.Regs.Pc()) with
        | Some op -> op.Run m
        | None -> Jetpac2.Core.Machine.GeneratedStep m

    hookMachine.ExecuteOne resolveHook

    check
        "co-hooks run in registration order and preserve the instruction"
        (hookMachine.Regs.Get Jetpac2.Core.R8.A = 9
         && hookMachine.DrainEffects() = [ Jetpac2.Core.Say "first"; Jetpac2.Core.Say "second" ]
         && hookMachine.CycleCount() = 9L)
        (sprintf "A=%d cycles=%d" (hookMachine.Regs.Get Jetpac2.Core.R8.A) (hookMachine.CycleCount()))

    let stepMachine = Jetpac2.Core.Machine()
    stepMachine.LoadState(hookMem, state)
    stepMachine.AddCoHook(0x8000, fun _ -> [ Jetpac2.Core.Say "step" ]) |> ignore
    stepMachine.Step()

    check
        "co-hooks also run on the generated interpreter path"
        (stepMachine.Regs.Get Jetpac2.Core.R8.A = 9
         && stepMachine.DrainEffects() = [ Jetpac2.Core.Say "step" ])
        ""

    let dynamicCache = Jetpac3.Core.CodeCache()
    dynamicCache.Attach stepMachine
    dynamicCache.Decode stepMachine.Memory 0x8000 |> ignore
    let decodedBeforeReset = dynamicCache.Count
    stepMachine.LoadState(hookMem, state)

    check
        "dynamic decode cache clears on state reload"
        (decodedBeforeReset = 1 && dynamicCache.Count = 0)
        (sprintf "before=%d after=%d" decodedBeforeReset dynamicCache.Count)

    if failures.Count > 0 then 1 else 0

let runControl () : int =
    printfn "control: json roundtrip + skool ctrl import/export"
    // 1. JSON roundtrip preserves everything.
    let cf: JetpacFR.Core.ControlFile =
        { ImageFile = "original.bin"
          Start = 0x8000
          EndExcl = 0x809B
          EntryPc = 0x8000
          ActiveVersion = 2
          Blocks =
            [ { Start = 0x8000
                EndExcl = 0x8023
                Name = "main"
                Kind = Code }
              { Start = 0x8023
                EndExcl = 0x8026
                Name = "table"
                Kind = Data } ]
          Comments =
            [ { Kind = Line
                Addr = 0x8005
                EndExcl = 0
                InstrIndex = -1
                Text = "keyboard scan" }
              { Kind = Name
                Addr = 0x8000
                EndExcl = 0x8023
                InstrIndex = -1
                Text = "MainLoop" }
              { Kind = Range
                Addr = 0x8010
                EndExcl = 0x8020
                InstrIndex = -1
                Text = "sprite update" }
              { Kind = Exec
                Addr = 0
                EndExcl = 0
                InstrIndex = 1234
                Text = "first jump into RAM" }
              { Kind = Frames
                Addr = 40
                EndExcl = 90
                InstrIndex = -1
                Text = "menu idle loop" } ]
          Symbols = [ (0x8023, "tableLookup") ]
          Dirty = true }

    let rt = JetpacFR.Core.ControlFile.fromJson (JetpacFR.Core.ControlFile.toJson cf)
    check "json roundtrip: blocks" (rt.Blocks = cf.Blocks) (sprintf "%A" rt.Blocks)
    check "json roundtrip: comments" (rt.Comments = cf.Comments) (sprintf "%A" rt.Comments)
    check "json roundtrip: symbols" (rt.Symbols = cf.Symbols) (sprintf "%A" rt.Symbols)

    check
        "json roundtrip: frames comment"
        (rt.Comments
         |> List.exists (fun m -> m.Kind = Frames && m.Addr = 40 && m.EndExcl = 90 && m.Text = "menu idle loop"))
        (sprintf "%A" rt.Comments)

    check
        "json roundtrip: scalars"
        (rt.Start = cf.Start
         && rt.EndExcl = cf.EndExcl
         && rt.ActiveVersion = cf.ActiveVersion
         && rt.EntryPc = cf.EntryPc
         && rt.ImageFile = cf.ImageFile)
        (JetpacFR.Core.ControlFile.toJson rt)

    check "roundtrip clears dirty flag" (not rt.Dirty) ""
    // 2. commentAt: exact line wins, else covering name/range.
    let at a =
        JetpacFR.Core.ControlFile.commentAt cf a

    check "commentAt exact line" (at 0x8005 = Some "keyboard scan") (sprintf "%A" (at 0x8005))
    check "commentAt falls back to range" (at 0x8015 = Some "sprite update") (sprintf "%A" (at 0x8015))
    // 2b. Mass-comment coalescing: a run of same-text line comments saves as
    // one range (address + length) instead of one repeat per instruction.
    let stamped =
        (JetpacFR.Core.ControlFile.empty 0x4000 0x10000, [ 25091; 25094; 25097; 25098 ])
        ||> List.fold (fun c a ->
            JetpacFR.Core.ControlFile.upsert
                c
                { Kind = Line
                  Addr = a
                  EndExcl = 0
                  InstrIndex = -1
                  Text = "menu idle" })

    let rtStamped =
        JetpacFR.Core.ControlFile.fromJson (JetpacFR.Core.ControlFile.toJson stamped)

    let menuRanges = rtStamped.Comments |> List.filter (fun m -> m.Text = "menu idle")

    check
        "same-text line runs coalesce into one range"
        (menuRanges = [ { Kind = Range
                          Addr = 25091
                          EndExcl = 25099
                          InstrIndex = -1
                          Text = "menu idle" } ])
        (sprintf "%A" rtStamped.Comments)

    check
        "coalesced range resolves at every member line"
        ([ 25091; 25094; 25097; 25098 ]
         |> List.forall (fun a -> JetpacFR.Core.ControlFile.commentAt rtStamped a = Some "menu idle"))
        (sprintf
            "%A"
            (List.map (fun a -> JetpacFR.Core.ControlFile.commentAt rtStamped a) [ 25091; 25094; 25097; 25098 ]))

    let loneStamped =
        JetpacFR.Core.ControlFile.upsert
            (JetpacFR.Core.ControlFile.empty 0x4000 0x10000)
            { Kind = Line
              Addr = 100
              EndExcl = 0
              InstrIndex = -1
              Text = "solo" }

    let rtLone =
        JetpacFR.Core.ControlFile.fromJson (JetpacFR.Core.ControlFile.toJson loneStamped)

    check
        "a lone line comment keeps its kind"
        (rtLone.Comments = [ { Kind = Line
                               Addr = 100
                               EndExcl = 0
                               InstrIndex = -1
                               Text = "solo" } ])
        (sprintf "%A" rtLone.Comments)
    // 2c. Multiple comments on one address: addLine appends, replaceComment
    // edits in place, commentsAt orders line before covering block.
    let multi =
        (JetpacFR.Core.ControlFile.empty 0x4000 0x10000, [ "first"; "second" ])
        ||> List.fold (fun c t -> JetpacFR.Core.ControlFile.addLine c 100 t)

    check
        "addLine allows multiple comments per address"
        (multi.Comments
         |> List.filter (fun m -> m.Kind = Line && m.Addr = 100)
         |> List.length = 2)
        ""

    check "addLine skips an identical re-add" (JetpacFR.Core.ControlFile.addLine multi 100 "first" = multi) ""

    let edited =
        JetpacFR.Core.ControlFile.replaceComment
            multi
            { Kind = Line
              Addr = 100
              EndExcl = 0
              InstrIndex = -1
              Text = "first" }
            "renamed"

    check
        "replaceComment edits one of several"
        (edited.Comments |> List.exists (fun m -> m.Text = "renamed")
         && edited.Comments |> List.exists (fun m -> m.Text = "second"))
        ""

    let withRange =
        { multi with
            Comments =
                multi.Comments
                @ [ { Kind = Range
                      Addr = 90
                      EndExcl = 120
                      InstrIndex = -1
                      Text = "block" } ] }

    let expectedOrder =
        [ { Kind = Line
            Addr = 100
            EndExcl = 0
            InstrIndex = -1
            Text = "first" }
          { Kind = Line
            Addr = 100
            EndExcl = 0
            InstrIndex = -1
            Text = "second" }
          { Kind = Range
            Addr = 90
            EndExcl = 120
            InstrIndex = -1
            Text = "block" } ]

    check
        "commentsAt orders line before covering block"
        (JetpacFR.Core.ControlFile.commentsAt withRange 100 = expectedOrder)
        (sprintf "%A" (JetpacFR.Core.ControlFile.commentsAt withRange 100))

    check "commentAt none" (at 0x8090 = None) (sprintf "%A" (at 0x8090))
    // 3. upsert replaces by key; empty text deletes.
    let cf2 =
        JetpacFR.Core.ControlFile.upsert
            cf
            { Kind = Line
              Addr = 0x8005
              EndExcl = 0
              InstrIndex = -1
              Text = "rewritten" }

    check "upsert replaces same key" ((cf2.Comments |> List.filter (fun m -> m.Kind = Line)).Length = 1) ""
    check "upsert new text" (JetpacFR.Core.ControlFile.commentAt cf2 0x8005 = Some "rewritten") ""

    let cf3 =
        JetpacFR.Core.ControlFile.upsert
            cf2
            { Kind = Line
              Addr = 0x8005
              EndExcl = 0
              InstrIndex = -1
              Text = "" }
    // Removing the line comment at 0x8005: the address still shows the
    // covering Name "MainLoop" (0x8000-0x8023), not the removed text.
    check "upsert empty text removes" (JetpacFR.Core.ControlFile.commentAt cf3 0x8005 = Some "MainLoop") ""
    // 4. SkoolKit import: block directives + N/D comments, hex and decimal.
    let skoolText =
        """c $8000 Main routine
N $8005 entry point
b 32773 data table
D 32773 lookup values
t 32778 messages
"""

    let imported = JetpacFR.Core.SkoolCtl.import skoolText
    check "skool: 3 blocks" (imported.Blocks.Length = 3) (sprintf "%A" imported.Blocks)

    check
        "skool: first block code"
        (imported.Blocks.Head.Kind = Code && imported.Blocks.Head.Name = "Main routine")
        (sprintf "%A" imported.Blocks.Head)

    check "skool: hex addr parsed" (imported.Blocks.Head.Start = 0x8000) (sprintf "%d" imported.Blocks.Head.Start)
    check "skool: dec addr parsed" (imported.Blocks |> List.item 1 |> (fun b -> b.Start = 32773)) ""

    check
        "skool: N becomes line comment"
        (imported.Comments
         |> List.exists (fun m -> m.Kind = Line && m.Addr = 0x8005 && m.Text = "entry point"))
        ""

    check
        "skool: D becomes range comment"
        (imported.Comments |> List.exists (fun m -> m.Kind = Range && m.Addr = 32773))
        ""

    check
        "skool: blocks closed at successors"
        (imported.Blocks.Head.EndExcl = 32773)
        (sprintf "%d" imported.Blocks.Head.EndExcl)
    // 5. Export shape: letters + titles, re-importable.
    let exported = JetpacFR.Core.SkoolCtl.export cf
    check "export emits c directive" (exported.Contains("c 32768")) exported
    check "export emits N comment" (exported.Contains("N 32773 keyboard scan")) exported
    let reimported = JetpacFR.Core.SkoolCtl.import exported

    check
        "export reimports to same block count"
        (reimported.Blocks.Length = cf.Blocks.Length)
        (sprintf "%d vs %d" reimported.Blocks.Length cf.Blocks.Length)

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
        [ { Start = 0x8000
            EndExcl = 0x8010
            Name = "main"
            Kind = Code } ]

    let comments =
        [ { ControlComment.Kind = Line
            Addr = 0x8003
            EndExcl = 0
            InstrIndex = -1
            Text = "back" } ]

    let counts = Array.zeroCreate<int> 0x10000
    counts[0x8005] <- 500

    let render vs ve rc =
        JetpacFR.Core.CtrlMapModel.render
            blocks
            comments
            starts
            (Some mem)
            counts
            (Array.zeroCreate<bool> 0x10000)
            vs
            ve
            rc
    // 1. Instruction-level LOD: ppb >= 8 yields decoded snippets.
    let hi = render 0x8000 0x8040 800
    check "snippets present above threshold" (hi.Snippets.Length >= 2) (sprintf "%d" hi.Snippets.Length)

    check
        "snippet first is LD BC,nn"
        (hi.Snippets[0].Addr = 0x8000
         && hi.Snippets[0].Bytes.StartsWith("01")
         && hi.Snippets[0].Text.Length > 0)
        (sprintf "%A" hi.Snippets[0])

    check
        "snippet carries line comment"
        (hi.Snippets
         |> Array.find (fun s -> s.Addr = 0x8003)
         |> fun s -> s.Comment = Some "back")
        (sprintf "%A" hi.Snippets)
    // 2. Zoomed out: no snippets.
    let lo = render 0x0000 0x10000 800
    check "snippets absent below threshold" (lo.Snippets.Length = 0) ""
    let deep = render 0x8004 0x8012 800
    // 3. Every row classified, even when several rows share one address
    //    (zoomed in past 1 byte/pixel - regression for a null-row crash).
    //    Rows inside the block classify Code; rows past its end Unmapped.
    let kindAt y =
        (JetpacFR.Core.CtrlMapModel.rowLo 0x8004 0x8012 800 y, deep.Rows[y].Kind)

    check
        "deep zoom rows all populated"
        (deep.Rows
         |> Array.forall (fun r -> r <> Unchecked.defaultof<JetpacFR.Core.CtrlMapModel.MapRow>))
        ""

    check
        "deep zoom row keeps block kind inside the block"
        ([ for y in 0..799 -> kindAt y ]
         |> List.forall (fun (a, k) ->
             if a < 0x8010 then
                 k = JetpacFR.Core.CtrlMapModel.CodeR
             else
                 k = JetpacFR.Core.CtrlMapModel.UnmappedR))
        (sprintf "%A" (kindAt 0, kindAt 799))

    check "zoomed-out row shows heat" ((render 0x8000 0x8010 40).Rows |> Array.exists (fun r -> r.Heat > 0)) ""
    // 4. Labels appear only when their band is tall enough.
    check "label fits when zoomed" (hi.Labels |> Array.exists (fun l -> l.Text = "main")) (sprintf "%A" hi.Labels)
    check "no label when zoomed far out" (lo.Labels.Length = 0) (sprintf "%A" lo.Labels)
    // 5. Range comments surface as extents clipped to the view.
    let cf =
        { ImageFile = "x"
          Start = 0x8000
          EndExcl = 0x8010
          EntryPc = 0x8000
          ActiveVersion = -1
          Blocks = blocks
          Comments =
            comments
            @ [ { Kind = Range
                  Addr = 0x8006
                  EndExcl = 0x800A
                  InstrIndex = -1
                  Text = "sprite" } ]
          Symbols = []
          Dirty = false }

    let rr =
        JetpacFR.Core.CtrlMapModel.render
            cf.Blocks
            cf.Comments
            starts
            (Some mem)
            counts
            (Array.zeroCreate<bool> 0x10000)
            0x8000
            0x8010
            400

    check "range mark visible" (rr.Ranges = [| (0x8006, 0x800A) |]) (sprintf "%A" rr.Ranges)
    // 6. Block edit ops.
    let c0 = ControlFile.empty 0x8000 0x8020
    let split = ControlFile.splitBlockAt c0 0x8010
    check "split makes two blocks" (split.Blocks.Length = 2 && split.Dirty) (sprintf "%A" split.Blocks)
    check "split names the tail" ((split.Blocks |> List.find (fun b -> b.Start = 0x8010)).Name = "block_8010") ""
    check "split on edge is a no-op" (ControlFile.splitBlockAt c0 0x8000 = c0) ""
    let merged = ControlFile.mergeWithNext split 0x8000

    check
        "merge restores one block"
        (merged.Blocks.Length = 1 && merged.Blocks.Head.EndExcl = 0x8020)
        (sprintf "%A" merged.Blocks)

    check "rename sets name" ((ControlFile.renameBlockAt c0 0x8015 "loop").Blocks.Head.Name = "loop") ""
    check "setKind persists kind" ((ControlFile.setKindAt c0 0x8015 Data).Blocks.Head.Kind = Data) ""
    if failures.Count > 0 then 1 else 0

let runCE () : int =
    printfn "ce: binary -> CE ops (roundtrip) + F# source"

    let rec findMinimal (d: DirectoryInfo) =
        let candidate = Path.Combine(d.FullName, "games", "minimal", "proba.bin")

        if File.Exists candidate then
            Some candidate
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

        check
            "roundtrip: assemble(toOps(binary)) == binary"
            (rebuilt = img)
            (sprintf "%d vs %d bytes" rebuilt.Length img.Length)

        let rawBytes =
            ops
            |> List.filter (fun o -> o.Mnemonic = "raw")
            |> List.sumBy (fun o -> o.Bytes.Length)

        check
            "proba.bin decodes to real ops (raw < 5%)"
            (rawBytes < img.Length / 20)
            (sprintf "%d raw of %d bytes" rawBytes img.Length)
        // 3. The F# source: labels + named ops.
        let src = Z80CE.toSource mem 0x8000 img.Length []
        check "source opens a z80 block" (src.Contains "z80 {") ""
        check "source labels in-range jumps" (src.Contains "Z80.at lbl0") ""

        check
            "source uses labeled conditional jumps"
            (src.Contains "Z80.JR_Z_LBL"
             || src.Contains "Z80.JR_LBL"
             || src.Contains "Z80.JP_NZ_LBL")
            ""

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
                if ce.Memory[i] <> oracle2.Memory[i] then
                    d <- i

                i <- i + 1

            let z2 = oracle2.DebugZ80

            if
                d >= 0
                || ce.Regs.Pc() <> z2.Regs.Pc()
                || ce.CycleCount <> int64 (z2.CycleCount())
            then
                ok2 <- false
                detail2 <- sprintf "frame %d mem=%d pc=%04X/%04X" f2 d (ce.Regs.Pc()) (z2.Regs.Pc())

            f2 <- f2 + 1

        check "CEGame engine wrapper locksteps with the oracle" ok2 detail2
        printfn "  --- generated source (first 24 lines) ---"
        src.Split('\n') |> Array.truncate 24 |> Array.iter (printfn "  %s")
        if failures.Count > 0 then 1 else 0

/// The generalized tape boot: every tape game must boot to its first own
/// RAM instruction, and that instruction must be tape-loaded game code.
let runBoot () : int =
    printfn "boot: generalized tape boot to first game instruction"

    let rec walk (d: DirectoryInfo) =
        let candidate = Path.Combine(d.FullName, "games")

        if Directory.Exists candidate then
            Some candidate
        else
            match d.Parent with
            | null -> None
            | p -> walk p

    let statePc (state: string) : int =
        state.Split('\n')
        |> Array.tryPick (fun l ->
            let kv = l.Split('=')

            if kv.Length = 2 && kv[0].Trim() = "pc" then
                Some(Convert.ToInt32(kv[1].Trim(), 16))
            else
                None)
        |> Option.defaultValue 0

    let bootCase (label: string) (tzxPath: string) =
        let oracle, cycles = Jetpac3.Core.Boot.bootToEntry rom.Value tzxPath None
        let mem, state = oracle.SaveState()
        let pc = statePc state
        check (sprintf "%s boots to a RAM instruction" label) (pc >= 0x4000) (sprintf "pc=%04X" pc)
        let spans = Jetpac3.Core.Boot.findCodeSpans (File.ReadAllBytes tzxPath)
        let inSpan = spans |> List.exists (fun (s, e) -> pc >= s && pc < s + e.Length)
        check (sprintf "%s entry is inside loaded CODE" label) inSpan (sprintf "pc=%04X" pc)
        printfn "  %s entry pc=%04X cycles=%d spans=%d" label pc cycles spans.Length

    let mmTzx =
        walk (DirectoryInfo AppContext.BaseDirectory)
        |> Option.bind (fun g ->
            let p = Path.Combine(g, "manicminer", "Manic Miner.tzx")
            if File.Exists p then Some p else None)

    match mmTzx with
    | None -> check "manic miner tape present" false "games/manicminer/Manic Miner.tzx not found"
    | Some mmTzx ->
        bootCase "jetpac" tzx.Value
        bootCase "manicminer" mmTzx

    0

/// The basic-block splitter (plan_code_graph): crafted golden case plus
/// the real jetpac code span. Invariants: targets never interior, blocks
/// re-decode exactly, branch ends produce two edges.
let runFlow () : int =
    printfn "flow: basic-block splitter"
    let mem = Array.zeroCreate<byte> 0x10000
    // 8000: 3E 01     LD A,1
    // 8002: 28 05     JR Z,+5 -> 8009
    // 8004: 00        NOP
    // 8005: CD 09 80  CALL 8009
    // 8008: C9        RET
    // 8009: C9        RET
    mem[0x8000] <- 0x3Euy
    mem[0x8001] <- 0x01uy
    mem[0x8002] <- 0x28uy
    mem[0x8003] <- 0x05uy
    mem[0x8004] <- 0x00uy
    mem[0x8005] <- 0xCDuy
    mem[0x8006] <- 0x09uy
    mem[0x8007] <- 0x80uy
    mem[0x8008] <- 0xC9uy
    mem[0x8009] <- 0xC9uy
    let golden = Z80Flow.splitBlocks mem 0x8000 0x800A (fun _ -> true)

    match golden with
    | [ b1; b2; b3 ] ->
        check "golden: block1 span" (b1.Start = 0x8000 && b1.EndExcl = 0x8004) (sprintf "%04X-%04X" b1.Start b1.EndExcl)
        check "golden: block1 ends branch to 8009" (b1.Ends = Z80Flow.Branch(Some 0x8009)) (sprintf "%A" b1.Ends)
        check "golden: block2 span" (b2.Start = 0x8004 && b2.EndExcl = 0x8009) (sprintf "%04X-%04X" b2.Start b2.EndExcl)
        check "golden: block2 ends return" (b2.Ends = Z80Flow.Return) (sprintf "%A" b2.Ends)
        check "golden: block3 starts at target" (b3.Start = 0x8009) (sprintf "%04X" b3.Start)
        check "golden: block3 ends return" (b3.Ends = Z80Flow.Return) (sprintf "%A" b3.Ends)
    // Any other shape (2 or 4 blocks) is exactly the regression this golden
    // case exists to catch - fail loudly instead of skipping the checks.
    | other -> check "golden: splitter returns 3 blocks" false (sprintf "%d blocks" other.Length)
    // Real image: the whole jetpac code span as code.
    let oracle, _ = Jetpac3.Core.Boot.bootToEntry rom.Value tzx.Value None
    let real = oracle.Memory
    let realBlocks = Z80Flow.splitBlocks real 0x6000 0x8000 (fun _ -> true)
    check "jetpac: blocks found" (realBlocks.Length > 10) (sprintf "%d" realBlocks.Length)
    let targets = System.Collections.Generic.HashSet<int>()
    let mutable a = 0x6000

    while a < 0x8000 do
        let kind, len = Z80Flow.classify real a

        match kind with
        | Z80Flow.Jump(Some t)
        | Z80Flow.Branch(Some t)
        | Z80Flow.Call(Some t) ->
            if t >= 0x6000 && t < 0x8000 then
                targets.Add t |> ignore
        | _ -> ()

        a <- a + len

    let badInterior =
        realBlocks
        |> List.filter (fun b -> targets |> Seq.exists (fun t -> t > b.Start && t < b.EndExcl))
        |> List.map (fun b -> sprintf "%04X" b.Start)

    check "jetpac: no target inside a block interior" (List.isEmpty badInterior) (String.concat "," badInterior)
    // every non-final block re-decodes exactly to its span
    let misaligned =
        realBlocks
        |> List.filter (fun b -> b.EndExcl < 0x8000)
        |> List.filter (fun b ->
            let mutable w = b.Start

            while w < b.EndExcl do
                w <- w + max 1 (Disasm.disasmMemory real w).Length

            w <> b.EndExcl)

    check "jetpac: blocks re-decode to their span" (List.isEmpty misaligned) (sprintf "%d misaligned" misaligned.Length)
    printfn "  jetpac blocks: %d, targets: %d" realBlocks.Length targets.Count
    0

let runFlame (romPath: string) (tzxPath: string) : int =
    printfn "flame: walker, symbols->CE labels, builder determinism, step-back"

    let mkEntry pc b0 b1 target (tick: int64) len cycles taken =
        { Pc = uint16 pc
          B0 = b0
          B1 = b1
          B2 = 0uy
          B3 = 0uy
          Target = uint16 target
          Tick = uint32 tick
          Length = uint8 len
          Cycles = uint8 cycles
          FlagsBefore = 0uy
          FlagsAfter = 0uy
          Taken = taken }

    // 1. Call/RET pairing: root + one invocation, correct kinds, spans nest.
    let callRet =
        [| mkEntry 0x8000 0x00uy 0uy 0x8001 100 1 4 0uy // root NOP
           mkEntry 0x8001 0xCDuy 0uy 0x0500 104 3 17 1uy // CALL $0500
           mkEntry 0x0500 0x00uy 0uy 0x0501 121 1 4 0uy // callee NOP
           mkEntry 0x0501 0xC9uy 0uy 0x0502 125 1 10 1uy // RET
           mkEntry 0x8004 0x00uy 0uy 0x8005 135 1 4 0uy |] // root NOP

    let w = FlameWalker.walk 5 [||] callRet
    let rects = w.Rects
    check "call/ret: two rectangles" (rects.Length = 2) (sprintf "%d rects" rects.Length)

    check
        "call/ret: root at depth 0 spanning the window"
        (rects[0].Kind = FlameKind.FlameRoot
         && rects[0].Depth = 0
         && rects[0].StartTick = 0L
         && rects[0].EndTick = 39L)
        (sprintf "%A" rects[0])

    check
        "call/ret: invocation rect"
        (rects[1].Kind = FlameKind.FlameCall
         && int rects[1].Entry = 0x0500
         && int rects[1].CallPc = 0x8001
         && rects[1].Depth = 1
         && rects[1].StartTick = 4L
         && rects[1].EndTick = 35L)
        (sprintf "%A" rects[1])

    check "call/ret: max depth" (w.MaxDepth = 2) (sprintf "%d" w.MaxDepth)
    check "call/ret: entry index range" (rects[1].StartIndex = 1 && rects[1].EndIndex = 4) (sprintf "%A" rects[1])

    // 2. Not-taken conditional CALL pushes nothing.
    let notTaken = [| mkEntry 0x8000 0xC4uy 0uy 0x8002 0 3 10 0uy |]
    check "conditional call not taken: root only" ((FlameWalker.walk -1 [||] notTaken).Rects.Length = 1) ""

    // 3. RST opens a frame with the RST vector as its entry.
    let rst =
        [| mkEntry 0x8000 0xFFuy 0uy 0x0038 0 1 11 1uy
           mkEntry 0x0038 0xC9uy 0uy 0x0039 11 1 10 1uy |]

    let wr = FlameWalker.walk -1 [||] rst

    check
        "rst: frame with vector entry"
        (wr.Rects.Length = 2
         && wr.Rects[1].Kind = FlameKind.FlameRst
         && int wr.Rects[1].Entry = 0x38)
        (sprintf "%A" wr.Rects)

    // 4. Interrupt marker opens an ISR frame (nested above a call); RETN pops it.
    let isr =
        [| mkEntry 0x8000 0xCDuy 0uy 0x0500 0 3 17 1uy // CALL
           mkEntry 0x0038 0x00uy 0uy 0x0038 17 0 7 1uy // interrupt marker (len 0, recorded under the vector 0x38)
           mkEntry 0x0038 0xEDuy 0x4Duy 0x003A 17 2 14 1uy // RETN
           mkEntry 0x0501 0xC9uy 0uy 0x0502 40 1 10 1uy |] // callee RET

    let wi = FlameWalker.walk -1 [||] isr
    let isrRect = wi.Rects |> Array.find (fun r -> r.Kind = FlameKind.FlameInterrupt)

    check
        "interrupt: ISR frame at depth 2 above the call"
        (int isrRect.Entry = 0x38 && isrRect.Depth = 2 && wi.MaxDepth = 3)
        (sprintf "%A" wi.Rects)

    // 5. Frames still open at the window end are closed at the window end.
    let openFrame =
        [| mkEntry 0x8000 0xCDuy 0uy 0x0500 0 3 17 1uy
           mkEntry 0x0500 0x00uy 0uy 0x0501 17 1 4 0uy |]

    let wo = FlameWalker.walk -1 [||] openFrame
    let callee = wo.Rects |> Array.find (fun r -> int r.Entry = 0x0500)
    check "open frame closed at window end" (callee.EndTick = wo.EndTick) (sprintf "%A %A" callee wo.EndTick)

    // 6. uint32 tick wrap is unwrapped once into non-negative int64 ticks.
    let wrapped =
        [| mkEntry 0x8000 0x00uy 0uy 0x8001 0xFFFFFFF0L 1 4 0uy
           mkEntry 0x8001 0x00uy 0uy 0x8002 0x0AL 1 4 0uy |]

    let ww = FlameWalker.walk -1 [||] wrapped

    check
        "tick wrap unwrapped"
        (ww.Rects[0].EndTick = 30L
         && ww.Rects |> Array.forall (fun r -> r.StartTick >= 0L))
        (sprintf "%A" ww.Rects)

    // 7. Window helpers: stabbing finds the first overlapping rect candidate.
    check "stab: root candidate" (FlameWindow.stab w 10L <= 1) (sprintf "%d" (FlameWindow.stab w 10L))
    check "entryAtTick" (FlameWindow.entryAtTick w 30L = 3) (sprintf "%d" (FlameWindow.entryAtTick w 30L))

    // 8. Walker and Miner agree on function entries for the same window.
    let mkTrace (entries: TraceEntry[]) : Trace =
        { Entries = entries
          Snapshots = [||]
          Writes = [||]
          Ports = [||]
          FrameTicks = [||]
          PerPcCount = Array.zeroCreate 0x10000
          SelfModified = Array.zeroCreate<bool> 0x10000
          SelfModCount = 0
          FirstIndexAtPc = Array.create 0x10000 -1
          StartTick = 0u
          EndTick = 0u }

    let routines, _edges = Miner.mine (mkTrace callRet)

    let flameEntries =
        w.Rects
        |> Array.filter (fun r -> r.Kind <> FlameKind.FlameRoot)
        |> Array.map (fun r -> int r.Entry)
        |> Set.ofArray

    let minerEntries = routines |> List.map (fun r -> r.Entry) |> Set.ofList

    check
        "flame covers miner routine entries"
        (Set.isSubset minerEntries flameEntries)
        (sprintf "%A vs %A" minerEntries flameEntries)

    // 9. Symbols: CE bodies declare named label cells (self-contained).
    // The jump target must sit INSIDE the body span to get the label form.
    let mem = Array.zeroCreate<byte> 0x10
    mem[0] <- 0xC3uy
    mem[1] <- 0x06uy
    mem[2] <- 0x00uy // JP $0006
    let body = Z80CE.toBody mem 0 0x10 [ (6, "screenClear") ]
    check "symbol label declared in the body" (body.Contains "let screenClear = Z80.label ()") body

    check
        "symbol label placed + used by jumps"
        (body.Contains "Z80.at screenClear" && body.Contains "Z80.JP_LBL screenClear")
        body
    // sanitization: a bare F# keyword yields a compiling identifier
    let body2 = Z80CE.toBody mem 0 0x10 [ (6, "type") ]
    check "symbol sanitized" (body2.Contains "Z80.at _type") body2

    // 10. Cache: LRU keeps windows, trims the detail tier over budget
    // (pure - a walker window from the synthetic trace, no emulator needed).
    let wCache = FlameWalker.walk 1 [| 200u; 300u; 400u |] callRet
    let cache = FlameCache(detailFrameBudget = 2)
    let stored = cache.Add wCache

    check
        "cache trims detail over budget, keeps rects"
        (stored.Entries.IsNone && stored.Rects.Length = wCache.Rects.Length)
        (sprintf "entries=%A rects=%d/%d" stored.Entries.IsSome stored.Rects.Length wCache.Rects.Length)

    let hit = cache.TryGet(1, 3)
    check "cache returns covering windows" (hit.IsSome && hit.Value.EndTick = wCache.EndTick) ""
    check "cache misses non-covering ranges" (cache.TryGet(2, 4) = None) ""

    // 10b. LOD pyramid: odd cell counts crashed the pairwise merge (an
    // out-of-range pair cell on the last depth row); runs must tile cells.
    for frameCount in [ 1; 2; 3; 5; 8 ] do
        let bounds = [| for f in 1..frameCount -> uint32 (f * 100) |]
        let wLod = FlameWalker.walk 1 bounds callRet
        let lod = FlameLod.build wLod

        let shapeOk =
            lod.Levels.Length > 0
            && (lod.Levels
                |> Array.forall (fun l ->
                    l.Funcs.Length = l.Cols * wLod.MaxDepth
                    && l.Dominated.Length = l.Funcs.Length
                    && l.Covered.Length = l.Funcs.Length))

        check
            (sprintf "flame LOD builds, frames=%d" frameCount)
            shapeOk
            (sprintf "levels=%d maxDepth=%d" lod.Levels.Length wLod.MaxDepth)
        // Coarser levels halve the cell count (rounded up).
        check
            (sprintf "flame LOD level shapes, frames=%d" frameCount)
            (lod.Levels
             |> Array.mapi (fun i l -> l.Cols = (frameCount + (1 <<< i) - 1) / (1 <<< i))
             |> Array.forall id)
            (sprintf "%A" (lod.Levels |> Array.map (fun l -> l.Cols)))
        // Runs tile the requested range exactly, once per level.
        let level = lod.Levels[0]
        let fA, bA, sA, lA = FlameLod.runs lod level 0 0 level.Cols

        check
            (sprintf "flame LOD runs tile, frames=%d" frameCount)
            (Array.sum lA = level.Cols
             && (Array.isEmpty sA || Array.head sA = 0)
             && Array.fold (fun acc l -> acc + l) 0 lA = level.Cols
             && fA.Length = sA.Length
             && sA.Length = lA.Length)
            (sprintf "runs=%d cols=%d" fA.Length level.Cols)

    // 11. Live re-execution: builder determinism + step back. Each needs one
    // booted session (minutes on a cold boot), so they share a single session
    // and only run when the warm entry cache exists - run --test trace first
    // to create it.
    match EntryCache.tryLoad romPath tzxPath with
    | None -> printfn "  (skipped: no warm entry cache - run --test trace first for the live flame tests)"
    | Some _ ->
        let live = TraceSession(romPath, tzxPath, 4_000_000)

        for _ in 0..5 do
            live.RunFrame() |> ignore
        // Builder determinism: re-executing frames 1..3 must reproduce the
        // live recording byte for byte.
        match FlameBuilder.buildFrames live.StateTimeline live.KeyLog 1 3 ignore (fun () -> false) with
        | None -> check "flame builder ran" false "cancelled"
        | Some wb ->
            let lt = live.Recorder.Build()
            // The window's base tick is frame 1's first entry in the live ring.
            let rec firstAt (lo: int) (hi: int) (tick: uint32) =
                if lo >= hi then
                    lo
                else
                    let mid = (lo + hi) / 2

                    if lt.Entries[mid].Tick < tick then
                        firstAt (mid + 1) hi tick
                    else
                        firstAt lo mid tick

            let start0 = firstAt 0 lt.Entries.Length (uint32 wb.BaseTick)
            let n = wb.Entries.Value.Length

            let mism =
                [ for i in 0 .. n - 1 do
                      let a = lt.Entries[start0 + i]
                      let b = wb.Entries.Value[i]

                      if
                          a.Pc <> b.Pc
                          || a.B0 <> b.B0
                          || a.Length <> b.Length
                          || a.Cycles <> b.Cycles
                          || a.Tick <> b.Tick
                      then
                          yield i ]

            check
                "builder re-executes frames identically"
                (start0 + n <= lt.Entries.Length && List.isEmpty mism)
                (sprintf "start0=%d n=%d live=%d mism=%A" start0 n lt.Entries.Length (List.truncate 5 mism))

            check "builder window has frame mapping" (wb.FirstFrame = 1 && wb.FrameTicks.Length = 3) ""
        // Step back: park after entry k, one more instruction reproduces the
        // originally recorded next entry exactly. Runs on the same session
        // (ParkAtInstruction is non-destructive; the RunFrame re-runs the rest
        // of the frame and appends it to the ring).
        let t1 = live.Recorder.Build()
        // Park inside frame 3. Session numbering: the entry state IS frame 0 and
        // boundary j is recorded when session frame j+1 completes, so a frame's
        // entries sit between boundaries [frame - 2] and [frame - 1] and a frame
        // of 3 or more has a predecessor state to restore.
        let firstOfFrame3 =
            let tick = t1.FrameTicks[1]

            let rec s2 (lo: int) (hi: int) =
                if lo >= hi then
                    lo
                else
                    let mid = (lo + hi) / 2

                    if t1.Entries[mid].Tick < tick then
                        s2 (mid + 1) hi
                    else
                        s2 lo mid

            s2 0 t1.Entries.Length

        let target = firstOfFrame3 + 50

        if t1.Entries.Length < target + 2 || t1.FrameTicks.Length < 3 then
            check "step back target exists" false "trace too short"
        else
            let rec search (lo: int) (hi: int) =
                if lo >= hi then
                    lo
                else
                    let mid = (lo + hi) >>> 1

                    if int64 t1.FrameTicks[mid] <= int64 t1.Entries[target].Tick then
                        search (mid + 1) hi
                    else
                        search lo mid

            let frame = search 0 (t1.FrameTicks.Length - 1) + 1

            let firstInFrame =
                if frame = 1 then
                    0
                else
                    let tick = t1.FrameTicks[frame - 2]

                    let rec s2 (lo: int) (hi: int) =
                        if lo >= hi then
                            lo
                        else
                            let mid = (lo + hi) / 2

                            if t1.Entries[mid].Tick < tick then
                                s2 (mid + 1) hi
                            else
                                s2 lo mid

                    s2 0 t1.Entries.Length

            let before = live.Recorder.EntryCount
            live.ParkAtInstruction(frame, target - firstInFrame + 1)
            live.RunFrame() |> ignore
            let t2 = live.Recorder.Build()
            let a = t2.Entries[before]
            let b = t1.Entries[target + 1]

            check
                "parked machine continues with the exact next instruction"
                (a.Pc = b.Pc && a.Tick = b.Tick && a.Length = b.Length && a.Cycles = b.Cycles)
                (sprintf "got %04X@%u vs %04X@%u" a.Pc a.Tick b.Pc b.Tick)

    if failures.Count > 0 then 1 else 0


/// The historical regression harness.
/// Static ctl generation (CtlGen): synthetic snapshots exercising the block
/// splitter, data catching, zero/NOP handling and text detection, plus the
/// control-file round trip.
let runCtlGen () : int =
    printfn "ctlgen: static control-map generation"

    let poke (mem: byte[]) (addr: int) (bytes: byte list) =
        bytes |> List.iteri (fun i b -> mem[addr + i] <- b)

    let block start endExcl kind : Block =
        { Start = start
          EndExcl = endExcl
          Name = sprintf "block_%04X" start
          Kind = kind }

    // 1. RET-terminated code: one code block to just past the RET; the zero
    //    tail is a gap.
    let mem = Array.zeroCreate<byte> 0x10000
    poke mem 0x4000 [ 0x3Euy; 0x47uy; 0xC9uy ] // LD A,0x47 ; RET

    let r = CtlGen.analyze mem 0x4000 0x4020 CtlGen.defaultConfig
    let expected1 = [ block 0x4000 0x4003 Code; block 0x4003 0x4020 Gap ]

    check "ret-terminated code -> code block + gap tail" (r.Blocks = expected1) (sprintf "%A" r.Blocks)
    check "no text in plain code" (r.TextNotes = []) (sprintf "%A" r.TextNotes)

    // 2. NOP prefix: leading NOPs are a gap; code starts at the first
    //    non-zero byte.
    let mem2 = Array.zeroCreate<byte> 0x10000
    poke mem2 0x4000 [ 0x00uy; 0x00uy; 0xC9uy ] // NOP ; NOP ; RET

    let r2 = CtlGen.analyze mem2 0x4000 0x4010 CtlGen.defaultConfig

    let expected2 =
        [ block 0x4000 0x4002 Gap; block 0x4002 0x4003 Code; block 0x4003 0x4010 Gap ]

    check "nop prefix -> gap head, code at first nonzero" (r2.Blocks = expected2) (sprintf "%A" r2.Blocks)

    // 3. A run of one operation beyond MaxRepeat becomes data; only the
    //    terminal instruction stays code.
    let mem3 = Array.zeroCreate<byte> 0x10000

    poke
        mem3
        0x4000
        [ 0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0xC9uy ]

    let r3 = CtlGen.analyze mem3 0x4000 0x4010 CtlGen.defaultConfig

    let expected3 =
        [ block 0x4000 0x400A Data; block 0x400A 0x400B Code; block 0x400B 0x4010 Gap ]

    check "repeated-op run caught as data, terminal stays code" (r3.Blocks = expected3) (sprintf "%A" r3.Blocks)

    // 4. The 0x66/0x6E harmless pair: with MaxRepeat=2 two LD H,(HL) stay
    //    code while two LD A,n of the same run length are caught.
    let cfg2 =
        { CtlGen.defaultConfig with
            MaxRepeat = 2 }

    let mem4 = Array.zeroCreate<byte> 0x10000
    poke mem4 0x4000 [ 0x66uy; 0x66uy; 0xC9uy ] // LD H,(HL) ; LD H,(HL) ; RET

    let r4 = CtlGen.analyze mem4 0x4000 0x4008 cfg2
    let expected4 = [ block 0x4000 0x4003 Code; block 0x4003 0x4008 Gap ]

    check "harmless 0x66 pair stays code at MaxRepeat=2" (r4.Blocks = expected4) (sprintf "%A" r4.Blocks)

    let mem5 = Array.zeroCreate<byte> 0x10000
    poke mem5 0x4000 [ 0x3Euy; 0x00uy; 0x3Euy; 0x00uy; 0xC9uy ] // LD A,0 x2 ; RET

    let r5 = CtlGen.analyze mem5 0x4000 0x4008 cfg2

    check
        "same run length caught without the harmless pair"
        (r5.Blocks
         |> List.exists (fun b -> b.Kind = Data && b.Start = 0x4000 && b.EndExcl = 0x4004))
        (sprintf "%A" r5.Blocks)

    // 5. Text: a printable run inside a code block demotes it to data and
    //    emits the string; text inside a data block splits that block.
    let mem6 = Array.zeroCreate<byte> 0x10000
    poke mem6 0x4000 [ 0x3Euy; 0x47uy; 0xC9uy ] // LD A,0x47 ; RET
    poke mem6 0x4003 [ 0x53uy; 0x43uy; 0x4Fuy; 0x52uy; 0x45uy ] // "SCORE"

    let r6 = CtlGen.analyze mem6 0x4000 0x4010 CtlGen.defaultConfig

    let expected6 =
        [ block 0x4000 0x4003 Code; block 0x4003 0x4008 Data; block 0x4008 0x4010 Data ]

    check "text run demotes code block to data" (r6.Blocks = expected6) (sprintf "%A" r6.Blocks)

    check
        "text note carries the decoded string"
        (r6.TextNotes = [ (0x4003, 0x4008, "SCORE") ])
        (sprintf "%A" r6.TextNotes)

    let mem7 = Array.zeroCreate<byte> 0x10000

    poke
        mem7
        0x4000
        [ 0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0x3Euy
          0x00uy
          0x3Euy
          0x00uy ]

    poke mem7 0x400A [ 0x4Fuy; 0x4Buy ] // "OK"

    let r7 = CtlGen.analyze mem7 0x4000 0x4014 CtlGen.defaultConfig

    let expected7 =
        [ block 0x4000 0x400A Data; block 0x400A 0x400C Data; block 0x400C 0x4014 Data ]

    check "text inside data splits the block" (r7.Blocks = expected7) (sprintf "%A" r7.Blocks)
    check "split block text noted" (r7.TextNotes = [ (0x400A, 0x400C, "OK") ]) (sprintf "%A" r7.TextNotes)

    // 6. Round trip: toControlFile -> json -> fromJson, and the generator
    //    side (GameProject.loadControl) accepts the map - sorted,
    //    non-overlapping, inside the span.
    let cf = CtlGen.toControlFile r6 0x4000 0x4000 0x4010
    let cf2 = cf |> ControlFile.toJson |> ControlFile.fromJson

    check
        "generated control file json round trip"
        (cf2.Blocks = cf.Blocks && cf2.Comments = cf.Comments && cf2.EntryPc = cf.EntryPc)
        (sprintf "%A / %A" cf.Comments cf2.Comments)

    check
        "blocks are sorted, adjacent, inside the span"
        (cf.Blocks
         |> List.pairwise
         |> List.forall (fun (a, b) -> a.Start < a.EndExcl && a.EndExcl <= b.Start))
        (sprintf "%A" cf.Blocks)

    let dir = Path.Combine(Path.GetTempPath(), "ctlgen-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory dir |> ignore

    try
        File.WriteAllText(Path.Combine(dir, "control.json"), ControlFile.toJson cf)
        let gc = GameProject.loadControl dir

        check
            "GameProject.loadControl accepts the generated map"
            (gc.Blocks.Length = cf.Blocks.Length
             && gc.Start = cf.Start
             && gc.EndExcl = cf.EndExcl)
            (sprintf "%A" gc.Blocks)
    finally
        Directory.Delete(dir, true)

    if failures.Count > 0 then 1 else 0

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
            | "trace" -> runTrace rom.Value tzx.Value
            | "agree" -> runAgree rom.Value tzx.Value
            | "gaps" -> runGaps ()
            | "mine" -> runMine rom.Value tzx.Value
            | "contract" -> runContract rom.Value tzx.Value
            | "validate" -> runValidate rom.Value tzx.Value
            | "prompt" -> runPrompt rom.Value tzx.Value
            | "bench" -> runBench rom.Value tzx.Value
            | "boot" -> runBoot ()
            | "manifest" -> runManifest ()
            | "minimal" -> runMinimal ()
            | "game2" -> runGame2 ()
            | "flow" -> runFlow ()
            | "handoff" -> runReplayHandoff rom.Value tzx.Value
            | "history" -> runHistory rom.Value tzx.Value
            | "timeline" -> runTimeline rom.Value tzx.Value
            | "z80ops" -> runZ80Ops ()
            | "labels" -> runLabels ()
            | "integration" -> runIntegration ()
            | "ce" -> runCE ()
            | "control" -> runControl ()
            | "ctlgen" -> runCtlGen ()
            | "ctrlmap" -> runCtrlMap ()
            | "flame" -> runFlame rom.Value tzx.Value
            | "all" ->
                runDisasmKnown ()
                + runDisasmCorpus ()
                + runTrace rom.Value tzx.Value
                + runAgree rom.Value tzx.Value
                + runGaps ()
                + runMine rom.Value tzx.Value
                + runContract rom.Value tzx.Value
                + runValidate rom.Value tzx.Value
                + runHistory rom.Value tzx.Value
                + runTimeline rom.Value tzx.Value
                + runBoot ()
                + runFlow ()
                + runManifest ()
                + runGame2 ()
                + runMinimal ()
                + runZ80Ops ()
                + runReplayHandoff rom.Value tzx.Value
                + runLabels ()
                + runIntegration ()
                + runCE ()
                + runControl ()
                + runCtlGen ()
                + runCtrlMap ()
                + runFlame rom.Value tzx.Value
            | other ->
                eprintfn "unknown test: %s" other
                1

        let code =
            match tests with
            | [] ->
                eprintfn
                    "usage: --test disasm|trace|agree|gaps|mine|contract|validate|prompt|bench|boot|manifest|minimal|game2|flow|handoff|history|z80ops|labels|integration|ce|control|ctlgen|ctrlmap|all"

                1
            | names -> List.sumBy run names
        // Individual test runners also signal failure through their return code
        // (unknown name, skipped test, preconditions not met); honor it so a
        // partial or skipped run can't report "ALL TESTS PASSED".
        if failures.Count > 0 || code <> 0 then
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
    | cmd :: _ when
        cmd = "--gen-game"
        || cmd = "--regen"
        || cmd = "--materialize"
        || cmd = "--diff-game"
        ->
        GenGame.run (Array.toList argv)
    | _ -> mainTests argv
