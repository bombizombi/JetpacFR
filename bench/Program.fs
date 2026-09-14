// Throwaway analysis host. Modes:
//   (no args)   - jetpac per-frame cost benchmark (Debug vs Release comparison)
//   uridium     - boot Uridium.tzx, save the snapshot, record the first
//                 executed frames, extract call targets, dump disassembly

module JetpacFR.Bench

open System
open System.IO
open JetpacFR.Core

let jetpacBench () =
    let rom = LocalAssets.find "48.rom"
    let tzx = LocalAssets.find "Jetpac.tzx"

    printfn "config: %s" (if System.Diagnostics.Debugger.IsAttached then "debugger" else "no debugger")

    let sw = Diagnostics.Stopwatch.StartNew()
    let s = TraceSession(rom, tzx, 4_000_000, fastBoot = true)
    sw.Stop()
    printfn "boot: %d ms (frame %d, warm=%b)" sw.ElapsedMilliseconds s.Frame s.WarmStart

    for _ in 1..50 do
        s.RunFrame() |> ignore

    let n = 300
    sw.Restart()

    for _ in 1..n do
        s.RunFrame() |> ignore

    sw.Stop()

    let per = float sw.ElapsedMilliseconds / float n

    printfn "run: %d frames in %d ms -> %.2f ms/frame = %.1f fps" n sw.ElapsedMilliseconds per (1000.0 / per)

    // === Replay path, exactly like the app's autoplay: load the recorded
    // slot timeline, StartReplay, then RunFrame until the script ends. ===
    let gameDir = Path.GetFullPath "games/jetpac"
    let manifest: GameManifest =
        { GameId = "jetpac"
          ManifestPath = Path.Combine(gameDir, "manifest.json")
          GameDirectory = gameDir
          Name = "Jetpac"
          Default = true
          Rom = rom
          Tzx = tzx
          Boot = "auto"
          Script = None
          ProgramBin = None
          ProgramAddress = None }

    let slotFile = Path.Combine(gameDir, "timeline.jst")

    let fp = ReplayStore.fingerprint manifest

    let tfp: TimelineFingerprint =
        { GameId = manifest.GameId
          RomSha256 = fp.RomSha256
          TzxSha256 = fp.TzxSha256
          ProgramSha256 = fp.ProgramSha256
          ProgramAddress = fp.ProgramAddress }

    match StateTimelineStore.tryLoad slotFile tfp with
    | TimelineLoaded(tl, events) ->
        printfn "slot: %d frames, %d key events" tl.Count events.Length
        s.LoadStateTimeline(tl, events)
        s.StartReplay()

        sw.Restart()
        let mutable played = 0

        while not s.ReplayFinished && played < 2000 do
            s.RunFrame() |> ignore
            played <- played + 1

        sw.Stop()

        let rper = float sw.ElapsedMilliseconds / float (max 1 played)

        printfn
            "replay: %d frames in %d ms -> %.2f ms/frame = %.1f fps"
            played
            sw.ElapsedMilliseconds
            rper
            (1000.0 / rper)
    | other -> printfn "slot load failed: %A" other

let isCallOp (b: byte) =
    match b with
    | 0xCDuy
    | 0xC4uy
    | 0xCCuy
    | 0xD4uy
    | 0xDCuy
    | 0xE4uy
    | 0xECuy
    | 0xF4uy
    | 0xFCuy -> true
    | _ -> false

let uridium () =
    let rom = LocalAssets.find "48.rom"
    let tzx = Path.GetFullPath "Uridium.tzx"
    Directory.CreateDirectory("games/uridium") |> ignore
    let sw = Diagnostics.Stopwatch.StartNew()

    // The tape: Text block, 637-byte BASIC loader, then ONE 49154-byte
    // standard block whose payload (flag + 49152 bytes + checksum) is a full
    // 48K memory image loaded at $4000 - the classic load-over-everything
    // loader. The game IS that byte image; no disassembly needed.
    let tzxBytes = File.ReadAllBytes tzx

    let blocks = ResizeArray<byte[]>()
    let mutable pos = 7

    if tzxBytes[pos] = 0x1Auy then pos <- pos + 1

    pos <- pos + 2

    while pos < tzxBytes.Length do
        let id = tzxBytes[pos]
        pos <- pos + 1

        match id with
        | 0x10uy ->
            let len = int tzxBytes[pos + 2] ||| (int tzxBytes[pos + 3] <<< 8)
            blocks.Add(tzxBytes[pos + 4 .. pos + 4 + len - 1])
            pos <- pos + 4 + len
        | 0x30uy -> pos <- pos + 1 + int tzxBytes[pos]
        | 0x31uy -> pos <- pos + 1 + int tzxBytes[pos]
        | 0x21uy -> pos <- pos + 2
        | 0x22uy -> ()
        | 0x20uy -> pos <- pos + 2
        | other -> failwithf "unsupported tzx block 0x%02X at %d" other pos

    let dataBlock = blocks[blocks.Count - 1]
    let data48 = dataBlock[1 .. dataBlock.Length - 2] // strip flag + checksum
    printfn "tape: %d blocks, image block = %d bytes" blocks.Count data48.Length

    // Build the 64K post-load machine state: ROM + the image at $4000.
    let image = Array.zeroCreate<byte> 0x10000
    Array.blit (File.ReadAllBytes rom) 0 image 0 0x4000
    Array.blit data48 0 image 0x4000 0xC000

    // Registers: the loader stub RETs to $F500 (verified: the image holds the
    // game init there - DI + clear $4000..). SP/I etc. come from the mid-load
    // state; the init code re-inits what it needs.
    let midState = File.ReadAllText("games/uridium/state.txt")

    let spLine = midState.Split('\n') |> Array.find (fun l -> l.StartsWith "sp=")
    let sp = Convert.ToInt32(spLine.Substring(3).Trim(), 16)

    let state =
        midState.Split('\n')
        |> Array.map (fun l ->
            if l.StartsWith "pc=" then "pc=F500"
            elif l.StartsWith "sp=" then sprintf "sp=%04X" sp
            else l)
        |> String.concat "\n"

    File.WriteAllBytes("games/uridium/memory.bin", image)
    File.WriteAllText("games/uridium/state.txt", state)
    // the table interpreter cannot serve this tape, so both cache slots get
    // the hand-built snapshot and the desktop warm-boots straight into the game
    EntryCache.saveMode rom tzx true image state
    EntryCache.saveMode rom tzx false image state
    printfn "snapshot: games/uridium/memory.bin + state.txt (pc=$F500, sp=$%04X, %d ms)" sp sw.ElapsedMilliseconds

    // run frames on the port machine, recording everything that executes
    Jetpac2.Core.Z80Table.EnsureInstalled()
    let m = Jetpac2.Core.Machine()
    m.LoadState(image, state)

    let executed = System.Collections.Generic.HashSet<int>()
    let execOrder = ResizeArray<int>()
    let calls = System.Collections.Generic.Dictionary<int, int>()
    let screen0 = image[0x4000 .. 0x5AFF]
    let frames = 400

    for _ in 1..frames do
        let frameEnd = m.FrameEnd + 69888L
        let memArr = m.Memory

        while m.CycleCount() < frameEnd do
            let pc = m.Regs.Pc() &&& 0xFFFF

            if executed.Add pc then
                if execOrder.Count < 6000 then
                    execOrder.Add pc

            let b0 = memArr[pc]

            if isCallOp b0 && m.CycleCount() < frameEnd - 8L then
                let target = (int memArr[(pc + 1) &&& 0xFFFF]) ||| (int memArr[(pc + 2) &&& 0xFFFF] <<< 8)

                calls[target] <- (match calls.TryGetValue target with true, c -> c | _ -> 0) + 1

            m.Step()

        m.FrameEnd <- m.FrameEnd + 69888L

    let screenWrites =
        [ 0x4000 .. 0x5AFF ] |> List.filter (fun a -> m.Memory[a] <> screen0[a - 0x4000]) |> List.length

    printfn
        "executed: %d distinct PCs over %d frames; %d call targets; %d screen bytes changed"
        executed.Count
        frames
        calls.Count
        screenWrites

    // ordered execution trace of the start
    do
        let lines =
            [ for i, pc in Seq.indexed execOrder do
                  let insn = Disasm.disasmMemory m.Memory pc
                  sprintf "%5d  $%04X  %s" i pc insn.Text ]

        File.WriteAllLines("uridium_trace.txt", lines)

    // disassemble each called function (linear from entry until RET/JP (HL)
    // or an instruction limit)
    do
        let sorted =
            calls
            |> Seq.map (fun kv -> kv.Key, kv.Value)
            |> Seq.sortByDescending snd
            |> Seq.toList

        let mutable out =
            [ sprintf
                "; Uridium - disassembly of code executed in the first %d frames after game start (pc=$F500)"
                frames
              sprintf "; distinct executed PCs: %d, call targets: %d, screen bytes changed: %d" executed.Count calls.Count screenWrites ]

        let retLike (text: string) =
            let t = text.Trim().ToUpperInvariant()

            t = "RET" || t.StartsWith "RET " || t.StartsWith "JP (HL)" || t.StartsWith "JP (IX)" || t.StartsWith "JP (IY)"

        for addr, count in sorted do
            out <-
                out
                @ [ ""
                    sprintf "=== $%04X  called %dx ===" addr count ]

            let mutable a = addr
            let mutable n = 0

            while n < 260 do
                let insn = Disasm.disasmMemory m.Memory a
                let bytes = m.Memory[a .. a + insn.Length - 1] |> Array.map (sprintf "%02X") |> String.concat " "
                out <- out @ [ sprintf "$%04X  %-11s %s" a bytes insn.Text ]

                if retLike insn.Text && n > 0 then
                    n <- 999
                else
                    a <- a + insn.Length
                    n <- n + 1

        File.WriteAllLines("uridium_disasm.txt", out)
        printfn "wrote uridium_disasm.txt (%d functions) and uridium_trace.txt (%d steps)" (List.length sorted) execOrder.Count

let uridium4 (args: string list) =
    let image = Array.zeroCreate<byte> 0x10000
    Array.blit (File.ReadAllBytes(LocalAssets.find "48.rom")) 0 image 0 0x4000
    let data48 = File.ReadAllBytes "games/uridium/uridium_48k.bin"
    Array.blit data48 0 image 0x4000 0xC000
    let start =
        match args with
        | a :: _ -> Convert.ToInt32(a, 16)
        | _ -> 0xF500

    let count =
        match args with
        | _ :: c :: _ -> int c
        | _ -> 120

    let mutable a = start

    for _ in 1..count do
        let insn = Disasm.disasmMemory image a
        let bytes = image[a .. a + insn.Length - 1] |> Array.map (sprintf "%02X") |> String.concat " "
        printfn "$%04X  %-11s %s" a bytes insn.Text
        a <- a + insn.Length



// Validated function pass: decode every CD target, reject targets that run
// into long zero runs (data/BSS), keep those terminating in RET, and dump
// only genuine code with per-function stats.
let scanCodeMap (args: string list) =
    let image = Array.zeroCreate<byte> 0x10000
    Array.blit (File.ReadAllBytes(LocalAssets.find "48.rom")) 0 image 0 0x4000
    let data48 = File.ReadAllBytes "games/uridium/uridium_48k.bin"
    Array.blit data48 0 image 0x4000 0xC000
    let mem = image

    let entry =
        match args with
        | a :: _ -> Convert.ToInt32(a, 16)
        | _ -> 0xF500

    let isCode = Array.zeroCreate<bool> 0x10000
    let funcs = System.Collections.Generic.HashSet<int>()
    let blocks = System.Collections.Generic.HashSet<int>()
    let dynJumps = System.Collections.Generic.HashSet<int>()
    let callSites = System.Collections.Generic.Dictionary<int, int list>()
    let jumpSites = System.Collections.Generic.Dictionary<int, int list>()

    let recordXref (d: System.Collections.Generic.Dictionary<int, int list>) (target: int) (site: int) =
        d[target] <- match d.TryGetValue target with true, xs -> site :: xs | _ -> [ site ]

    let work = System.Collections.Generic.Queue<int * bool>()
    let inWork = System.Collections.Generic.HashSet<int * bool>()

    let enqueue (a: int) (isFn: bool) =
        let key = (a &&& 0xFFFF, isFn)

        if not (isCode[fst key]) && inWork.Add key then
            work.Enqueue key

    let isZeroRun (a: int) =
        let mutable z = true
        let mutable i = 0

        while z && i < 16 do
            if mem[(a + i) &&& 0xFFFF] <> 0uy then z <- false

            i <- i + 1

        z

    let seedAllTargets () =
        for a in 0x4000 .. 0xFFFC do
            if mem[a] = 0xCDuy then
                let t = (int mem[a + 1]) ||| (int mem[a + 2] <<< 8)

                if t >= 0x4000 then enqueue t true

    enqueue entry true

    let mutable blocksWalked = 0

    let drainQueue () =
        while work.Count > 0 do
            let start, isFn = work.Dequeue()
            inWork.Remove(start, isFn) |> ignore

            if not isCode[start] then
                if isFn then funcs.Add start |> ignore

                blocks.Add start |> ignore
                blocksWalked <- blocksWalked + 1
                let mutable a = start
                let mutable go = true

                while go do
                    if isCode[a] || isZeroRun a then
                        // joined an already-mapped block or reached zeroed (BSS) memory
                        go <- false
                    else
                        let b0 = int mem[a]
                        let insn = Disasm.disasmMemory mem a
                        isCode[a] <- true
                        let next = (a + insn.Length) &&& 0xFFFF

                        let imm =
                            if insn.Length >= 3 then
                                Some((int mem[a + 1]) ||| (int mem[a + 2] <<< 8))
                            else
                                None

                        let rel =
                            if insn.Length >= 2 then
                                Some((a + insn.Length + int (sbyte mem[a + 1])) &&& 0xFFFF)
                            else
                                None

                        match b0 with
                        | 0xC9 -> go <- false // RET
                        | 0xC3 -> (match imm with Some t -> enqueue t false; recordXref jumpSites t a | None -> () )
                                  go <- false // JP nn
                        | 0x18 -> (match rel with Some t -> enqueue t false | None -> ())
                                  go <- false // JR e
                        | 0xE9 ->
                            dynJumps.Add a |> ignore
                            go <- false // JP (HL)
                        | 0xDD
                        | 0xFD when mem[(a + 1) &&& 0xFFFF] = 0xE9uy ->
                            dynJumps.Add a |> ignore
                            go <- false // JP (IX/IY)
                        | 0xED when mem[(a + 1) &&& 0xFFFF] = 0x5Duy -> go <- false // RETN
                        | 0xED when mem[(a + 1) &&& 0xFFFF] = 0x4Duy -> go <- false // RETI
                        | 0xC0 | 0xC8 | 0xD0 | 0xD8 | 0xE0 | 0xE8 | 0xF0 | 0xF8 ->
                            a <- next // conditional RET: fall-through possible
                        | 0x10 -> (match rel with Some t -> enqueue t false; recordXref jumpSites t a | None -> ())
                                  a <- next // DJNZ: taken + fall-through
                        | 0x20 | 0x28 | 0x30 | 0x38 ->
                            (match rel with Some t -> enqueue t false; recordXref jumpSites t a | None -> ())
                            a <- next // conditional JR: taken + fall-through
                        | 0xC2 | 0xCA | 0xD2 | 0xDA | 0xE2 | 0xEA | 0xF2 | 0xFA ->
                            (match imm with Some t -> enqueue t false; recordXref jumpSites t a | None -> ())
                            a <- next // conditional JP: taken + fall-through
                        | 0xCD ->
                            (match imm with Some t -> enqueue t true; recordXref callSites t a | None -> ())
                            a <- next // CALL nn
                        | 0xC4 | 0xCC | 0xD4 | 0xDC | 0xE4 | 0xEC | 0xF4 | 0xFC ->
                            (match imm with Some t -> enqueue t true; recordXref callSites t a | None -> ())
                            a <- next // CALL cc
                        | 0xD3 -> a <- next // OUT - continue
                        | _ ->
                            if b0 &&& 0xC7 = 0xC7 then
                                enqueue (int b0 &&& 0x38) true // RST -> fixed function
                                recordXref callSites (int b0 &&& 0x38) a

                            a <- next

    drainQueue ()

    let entryCodeBytes = isCode |> Array.filter id |> Array.length
    let entryFuncs = funcs.Count
    let entryBlocks = blocks.Count

    // extended pass (opt-in): when the entry-reachable map is exhausted, also
    // seed every remaining CALL target in the image so the whole code maps.
    let seedAll = args |> List.exists (fun a -> a = "--all")

    if seedAll then
        seedAllTargets ()
        drainQueue ()

    // ---- report ----
    // symbol names from uridium_found_symbols.txt (if present) for annotation
    let names =
        let d = System.Collections.Generic.Dictionary<int, string>()
        let symFile = "uridium_found_symbols.txt"

        if File.Exists symFile then
            for line in File.ReadAllLines symFile do
                if line.StartsWith "$" then
                    let parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)

                    if parts.Length >= 2 then
                        match System.Int32.TryParse(parts[0].Substring(1), Globalization.NumberStyles.HexNumber, null) with
                        | true, a -> d[a] <- parts[1]
                        | _ -> ()

        d

    let nameOf (a: int) = match names.TryGetValue a with true, n -> " " + n | _ -> ""

    let codeBytes = isCode |> Array.filter id |> Array.length
    let funcList = funcs |> Seq.sort |> Seq.toList
    let blockList = blocks |> Seq.sort |> Seq.toList

    let out = ResizeArray<string>()
    out.Add(sprintf "; Uridium recursive code map from entry $%04X (seed-all: %b)" entry seedAll)
    out.Add(sprintf "; entry-reachable: %d functions, %d blocks, %d bytes" entryFuncs entryBlocks entryCodeBytes)
    out.Add(sprintf "; total mapped: %d functions, %d block starts, %d blocks walked, %d code bytes" funcs.Count blocks.Count blocksWalked codeBytes)
    out.Add "; computed jumps (JP (HL)/(IX)/(IY)) at:"
    for d in dynJumps do out.Add(sprintf ";   dynamic jump $%04X" d)
    out.Add ";"
    out.Add "; function entries:"

    for f in funcList do out.Add(sprintf "; F $%04X%s" f (nameOf f))

    out.Add ";"
    out.Add "; call graph (function <- call sites):"

    for f in funcList do
        let cs = match callSites.TryGetValue f with true, xs -> List.rev xs | _ -> []

        if not cs.IsEmpty then
            out.Add(sprintf "; $%04X%s <- %s" f (nameOf f) (String.concat " " (cs |> List.map (sprintf "$%04X"))))
        else
            out.Add(sprintf "; $%04X%s <- (none: entry/interrupt/dynamic)" f (nameOf f))

    out.Add ";"
    out.Add "; block jump references (block <- jump sites):"

    for b in blockList do
        match jumpSites.TryGetValue b with
        | true, xs ->
            if xs.Length > 1 || not (funcs.Contains b) then
                out.Add(sprintf "; B $%04X <- %s" b (String.concat " " (xs |> List.map (sprintf "$%04X"))))
        | _ -> ()


    out.Add ";"
    out.Add "; block starts:"

    for b in blockList do out.Add(sprintf "; B $%04X" b)

    out.Add ";"
    out.Add "; listing:"

    let mutable a = 0

    while a < 0x10000 do
        if isCode[a] then
            if funcs.Contains a then out.Add(sprintf "F> $%04X%s" a (nameOf a))
            elif blocks.Contains a then out.Add(sprintf "B> $%04X" a)

            let insn = Disasm.disasmMemory mem a
            let bytes = mem[a .. a + insn.Length - 1] |> Array.map (sprintf "%02X") |> String.concat " "
            out.Add(sprintf "    $%04X  %-11s %s" a bytes insn.Text)
            a <- a + insn.Length
        else
            a <- a + 1

    File.WriteAllLines("uridium_codemap.txt", out)

    // ---- function reference skeleton (merged with analysis afterwards) ----
    let refLines = ResizeArray<string>()

    for f in funcList do
        let cs = match callSites.TryGetValue f with true, xs -> List.rev xs | _ -> []
        // span: consecutive mapped bytes from the entry
        let mutable e = f

        while (e + 1) < 0x10000 && isCode[e + 1] do e <- e + 1

        let fname = nameOf f |> fun n -> if n.Trim().Length > 0 then n.Trim() else "(undocumented)"
        refLines.Add(sprintf "FUNC $%04X  %s" f fname)
        refLines.Add(sprintf "  CALLERS: %s" (if cs.IsEmpty then "(none - entered dynamically)" else String.concat " " (cs |> List.map (sprintf "$%04X"))))
        refLines.Add(sprintf "  SIZE   : %d bytes mapped from entry" (e - f + 1))
        let insn = Disasm.disasmMemory mem f
        refLines.Add(sprintf "  FIRST  : %s" insn.Text)
        refLines.Add("  IN     : (not yet documented)")
        refLines.Add("  OUT    : (not yet documented)")
        refLines.Add("  DESC   : (not yet documented)")

    File.WriteAllLines("uridium_funcref_raw.txt", refLines)
    printfn
        "codemap: %d functions, %d blocks, %d code bytes, %d computed jumps -> uridium_codemap.txt"
        funcs.Count
        blocks.Count
        codeBytes
        dynJumps.Count

    // ---- control-file emission (--ctl <gamedir>) ----------------------------
    // Turns the map into games/<id>/original.bin + control.json: code blocks
    // from the walk, the data/gap complement split at symbol anchors, symbols
    // and the reference-file documentation woven in - --regen then emits the
    // fully disassembled, labelled CE version with the parity gate.
    let ctlDir =
        match args |> List.tryFindIndex (fun a -> a = "--ctl") with
        | Some i -> args |> List.skip (i + 1) |> List.tryHead
        | None -> None

    let writeControl (dir: string) =
        Directory.CreateDirectory dir |> ignore
        let spanStart, spanEnd = 0x4000, 0x10000
        File.WriteAllBytes(Path.Combine(dir, "original.bin"), mem[spanStart .. spanEnd - 1])

        // symbols: sanitized, globally unique (they become F# label cells)
        let syms =
            names
            |> Seq.map (fun kv -> kv.Key, kv.Value)
            |> Seq.toList
            |> List.sortBy fst
            |> List.map (fun (a, n) ->
                a,
                System.String(
                    n
                    |> Seq.map (fun c ->
                        if System.Char.IsLetterOrDigit c || c = '_' then
                            c
                        else
                            '_')
                    |> Array.ofSeq
                ))

        let seen = System.Collections.Generic.HashSet<string>()

        let syms =
            syms
            |> List.map (fun (a, n) ->
                let baseName = if n.Length = 0 then "sym" else n

                if seen.Add baseName then
                    a, baseName
                else
                    let n2 = sprintf "%s_%04X" baseName a
                    seen.Add n2 |> ignore
                    a, n2)

        let codeStarts = blockList |> List.filter (fun a -> a >= spanStart && a < spanEnd)
        let startsSet = System.Collections.Generic.HashSet<int>(codeStarts)
        let symMap = syms |> Map.ofList

        // Decode contiguous runs of mapped instruction starts (the listing's
        // walk: the map marks only instruction STARTS, so a plain byte scan
        // would stop at the first operand byte). Segments split only at
        // instruction-aligned block starts - a boundary mid-instruction makes
        // the assembled stream straddle it and parity fails. Overlapping
        // walks that began mid-operand are swallowed into the covering
        // segment instead of forcing such a boundary.
        let codeRanges =
            let acc = ResizeArray<int * int>()
            // every byte an emitted range covers; a later segment may not
            // begin on claimed bytes - walks that started mid-operand of an
            // earlier walk's instruction would otherwise produce overlapping
            // segments (each block must decode from its start to exactly its
            // end or the assembled stream straddles the boundary)
            let claimed = Array.zeroCreate<bool> 0x10000

            let addRange (lo: int) (hi: int) =
                if hi > lo then
                    acc.Add(lo, hi)

                    for i in lo .. hi - 1 do
                        claimed[i] <- true

            for s in codeStarts do
                if not (claimed[s]) then
                    let mutable segStart = s
                    let mutable a = s
                    let mutable go = true

                    while go do
                        if a >= spanEnd || not (isCode[a]) then
                            addRange segStart a
                            go <- false
                        else
                            if startsSet.Contains a && a <> segStart then
                                addRange segStart a
                                segStart <- a

                            let len = (Disasm.disasmMemory mem a).Length

                            if a + len > spanEnd then
                                // the tail instruction would cross the image
                                // end - close the segment and let the rest
                                // fall into the data complement
                                addRange segStart a
                                go <- false
                            else
                                a <- a + len

            acc |> Seq.toList

        // Symbols are labels, not boundaries: one falling inside a code block
        // only adds a label cell there (parity-neutral). Data-region splits
        // at symbol anchors re-emit literal bytes, so any address is safe.
        let symsFinal = syms
        let symAddrs = syms |> List.map fst
        let blocksAcc = ResizeArray<Block>()
        let commentsAcc = ResizeArray<ControlComment>()

        let emitData (lo: int) (hi: int) =
            let bnds = symAddrs |> List.filter (fun a -> a > lo && a < hi)
            let starts = lo :: (bnds @ [ hi ])

            for i in 0 .. starts.Length - 2 do
                let s = starts[i]
                let e = starts[i + 1]

                if e > s then
                    let isGap = mem[s .. e - 1] |> Array.forall (fun b -> b = 0uy)

                    let name =
                        match symMap |> Map.tryFind s with
                        | Some n -> n
                        | None -> sprintf "block_%04X" s

                    blocksAcc.Add
                        { Start = s
                          EndExcl = e
                          Name = name
                          Kind = (if isGap then Gap else Data) }

        let mutable cur = spanStart

        for (s, e) in codeRanges do
            emitData cur s

            let name =
                match symMap |> Map.tryFind s with
                | Some n -> n
                | None -> sprintf "block_%04X" s

            blocksAcc.Add { Start = s; EndExcl = e; Name = name; Kind = Code }
            cur <- e

        emitData cur spanEnd

        for (a, n) in symsFinal do
            commentsAcc.Add
                { Kind = Name
                  Addr = a
                  EndExcl = a + 1
                  InstrIndex = -1
                  Text = n }

        // documentation from the reference file: IN/OUT/DESC/CALLERS become
        // line comments at the function entry (a block start -> regen weaves
        // them in). Identical placeholder texts are skipped so the control
        // file's same-text coalescing never range-ifies them.
        let refPath = "uridium_function_reference.txt"

        if File.Exists refPath then
            let fieldKeys = set [ "KIND"; "IN"; "OUT"; "DESC"; "CALLERS"; "SIZE"; "FIRST" ]
            let mutable curAddr = -1
            let mutable fields = System.Collections.Generic.Dictionary<string, string>()

            let flush () =
                if curAddr >= 0 then
                    for k in [ "CALLERS"; "IN"; "OUT"; "DESC" ] do
                        match fields.TryGetValue k with
                        | true, v when
                            v.Trim() <> "-"
                            && v.Trim().Length > 0
                            && not (v.Contains "not yet documented")
                            && not (v.StartsWith "(none")
                            ->
                            commentsAcc.Add
                                { Kind = Line
                                  Addr = curAddr
                                  EndExcl = 0
                                  InstrIndex = -1
                                  Text = sprintf "%s: %s" k (v.Trim()) }
                        | _ -> ()

            for line in File.ReadAllLines refPath do
                let t = line.Trim()

                if t.StartsWith "FUNC $" then
                    flush ()
                    fields <- System.Collections.Generic.Dictionary()
                    let parts = t.Split(' ', StringSplitOptions.RemoveEmptyEntries)

                    match System.Int32.TryParse(parts[1].Substring(1), Globalization.NumberStyles.HexNumber, null) with
                    | true, a -> curAddr <- a
                    | _ -> curAddr <- -1
                elif curAddr >= 0 && t.Contains ":" then
                    let i = t.IndexOf ':'
                    let k = t.Substring(0, i).Trim().ToUpperInvariant()

                    if fieldKeys.Contains k then
                        fields[k] <- t.Substring(i + 1).Trim()

            flush ()

        let cf: ControlFile =
            { ImageFile = "original.bin"
              Start = spanStart
              EndExcl = spanEnd
              EntryPc = entry
              ActiveVersion = -1
              Blocks = blocksAcc |> Seq.toList |> List.sortBy (fun b -> b.Start)
              Comments = commentsAcc |> Seq.toList
              Symbols = symsFinal
              Dirty = false }

        File.WriteAllText(Path.Combine(dir, "control.json"), ControlFile.toJson cf)

        printfn
            "control: %s (%d blocks, %d code ranges from %d block starts, %d symbols, %d comments)"
            (Path.Combine(dir, "control.json"))
            cf.Blocks.Length
            codeRanges.Length
            codeStarts.Length
            symsFinal.Length
            commentsAcc.Count

    match ctlDir with
    | Some d -> writeControl d
    | None -> ()


[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "uridium" :: _ -> uridium ()
    | "codemap" :: rest -> scanCodeMap rest
    | "uridium4" :: rest -> uridium4 rest
    | _ -> jetpacBench ()

    0

// Recursive-descent static pass over the Uridium 48K image: from the start
// points, decode linearly, follow calls/jumps, collect function entries.
