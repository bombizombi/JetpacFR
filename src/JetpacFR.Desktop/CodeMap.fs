namespace JetpacFR.Desktop

open System
open JetpacFR.Core

/// The recursive-descent code mapper (the bench `codemap` twin with a GUI
/// front end): from one entry address, decode linearly, follow CALL targets
/// as functions and branch targets as block starts, optionally seeding every
/// remaining CALL target in the image so the whole program maps. Pure over a
/// byte[] snapshot, so it runs on a background task.
module CodeMap =

    /// Walk result: mapped instruction starts, function/block entries and the
    /// call/jump cross-references. `EntryReachable` is the (funcs, blocks,
    /// code bytes) triple after the entry pass, before seed-all.
    type Result =
        { Entry: int
          SeedAll: bool
          IsCode: bool[]
          Functions: int list
          Blocks: int list
          CallSites: (int * int list) list
          JumpSites: (int * int list) list
          EntryReachable: int * int * int }

    /// Map the image. `progress` fires after each block drains with
    /// (functions, blocks, code bytes) so the UI can show live counts. With
    /// `followRom` false, discovered targets below $4000 are recorded as
    /// cross-references but never walked - the 48K ROM is well-known, and
    /// following it floods the map with interrupt vectors and BIOS helpers.
    let map
        (mem: byte[])
        (entry: int)
        (seedAll: bool)
        (followRom: bool)
        (progress: int * int * int -> unit)
        : Result =
        let isCode = Array.zeroCreate<bool> 0x10000
        let funcs = System.Collections.Generic.HashSet<int>()
        let blocks = System.Collections.Generic.HashSet<int>()
        let callSites = System.Collections.Generic.Dictionary<int, int list>()
        let jumpSites = System.Collections.Generic.Dictionary<int, int list>()

        let recordXref (d: System.Collections.Generic.Dictionary<int, int list>) (target: int) (site: int) =
            d[target] <-
                match d.TryGetValue target with
                | true, xs -> site :: xs
                | _ -> [ site ]

        let work = System.Collections.Generic.Queue<int * bool>()
        let inWork = System.Collections.Generic.HashSet<int * bool>()

        let enqueue (a: int) (isFn: bool) =
            let key = (a &&& 0xFFFF, isFn)

            if
                (followRom || fst key >= 0x4000)
                && not (isCode[fst key])
                && inWork.Add key
            then
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

        let mutable codeMarked = 0

        let drainQueue () =
            while work.Count > 0 do
                let start, isFn = work.Dequeue()
                inWork.Remove(start, isFn) |> ignore

                if not isCode[start] then
                    if isFn then funcs.Add start |> ignore

                    blocks.Add start |> ignore
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
                            codeMarked <- codeMarked + 1
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
                            | 0xC3 ->
                                (match imm with
                                 | Some t ->
                                     enqueue t false
                                     recordXref jumpSites t a
                                 | None ->())

                                go <- false // JP nn
                            | 0x18 ->
                                (match rel with
                                 | Some t -> enqueue t false
                                 | None -> ())

                                go <- false // JR e
                            | 0xE9 -> go <- false // JP (HL)
                            | 0xDD
                            | 0xFD when mem[(a + 1) &&& 0xFFFF] = 0xE9uy -> go <- false // JP (IX/IY)
                            | 0xED when mem[(a + 1) &&& 0xFFFF] = 0x5Duy -> go <- false // RETN
                            | 0xED when mem[(a + 1) &&& 0xFFFF] = 0x4Duy -> go <- false // RETI
                            | 0xC0
                            | 0xC8
                            | 0xD0
                            | 0xD8
                            | 0xE0
                            | 0xE8
                            | 0xF0
                            | 0xF8 -> a <- next // conditional RET: fall-through possible
                            | 0x10 ->
                                (match rel with
                                 | Some t ->
                                     enqueue t false
                                     recordXref jumpSites t a
                                 | None -> ())

                                a <- next // DJNZ: taken + fall-through
                            | 0x20
                            | 0x28
                            | 0x30
                            | 0x38 ->
                                (match rel with
                                 | Some t ->
                                     enqueue t false
                                     recordXref jumpSites t a
                                 | None -> ())

                                a <- next // conditional JR: taken + fall-through
                            | 0xC2
                            | 0xCA
                            | 0xD2
                            | 0xDA
                            | 0xE2
                            | 0xEA
                            | 0xF2
                            | 0xFA ->
                                (match rel with
                                 | Some t -> enqueue t false
                                 | None -> ())

                                (match imm with
                                 | Some t ->
                                     enqueue t false
                                     recordXref jumpSites t a
                                 | None -> ())

                                a <- next // conditional JP: taken + fall-through
                            | 0xCD ->
                                (match imm with
                                 | Some t ->
                                     enqueue t true
                                     recordXref callSites t a
                                 | None -> ())

                                a <- next // CALL nn
                            | 0xC4
                            | 0xCC
                            | 0xD4
                            | 0xDC
                            | 0xE4
                            | 0xEC
                            | 0xF4
                            | 0xFC ->
                                (match imm with
                                 | Some t ->
                                     enqueue t true
                                     recordXref callSites t a
                                 | None -> ())

                                a <- next // CALL cc
                            | 0xD3 -> a <- next // OUT - continue
                            | _ ->
                                if b0 &&& 0xC7 = 0xC7 then
                                    enqueue (int b0 &&& 0x38) true // RST -> fixed function
                                    recordXref callSites (int b0 &&& 0x38) a

                                a <- next

                    progress (funcs.Count, blocks.Count, codeMarked)

        drainQueue ()
        let entryReachable = funcs.Count, blocks.Count, codeMarked

        if seedAll then
            seedAllTargets ()
            drainQueue ()

        let dictToList (d: System.Collections.Generic.Dictionary<int, int list>) =
            [ for kv in d -> (kv.Key, List.rev kv.Value) ] |> List.sortBy fst

        { Entry = entry
          SeedAll = seedAll
          IsCode = isCode
          Functions = funcs |> Seq.sort |> Seq.toList
          Blocks = blocks |> Seq.sort |> Seq.toList
          CallSites = dictToList callSites
          JumpSites = dictToList jumpSites
          EntryReachable = entryReachable }

    /// The readable text report: overview, function table, top lists, call
    /// graph, loose ends, then the full listing. `names` are the control
    /// file's symbols - unnamed functions stay address-labelled.
    let renderReport (mem: byte[]) (r: Result) (names: (int * string) list) : string list =
        let nameOf (a: int) =
            names
            |> List.tryFind (fun (x, _) -> x = a)
            |> Option.map snd
            |> Option.defaultValue ""

        let spanStart, spanEnd = 0x4000, 0x10000

        let codeIn (lo: int) (hi: int) =
            let mutable n = 0

            for i in lo .. hi - 1 do
                if r.IsCode[i] then n <- n + 1

            n

        let ramCode = codeIn spanStart spanEnd
        let ramBytes = spanEnd - spanStart
        let pct (n: int) = float n * 100.0 / float ramBytes

        /// contiguous mapped run from the entry (the map joins adjacent
        /// blocks, so this is an upper bound on the function's size)
        let sizeOf (f: int) =
            let mutable e = f

            while (e + 1) < 0x10000 && r.IsCode[e + 1] do
                e <- e + 1

            e - f + 1

        let callersOf (f: int) =
            r.CallSites
            |> List.tryFind (fun (a, _) -> a = f)
            |> Option.map snd
            |> Option.defaultValue []

        let funcSet = System.Collections.Generic.HashSet<int>(r.Functions)
        let blockSet = System.Collections.Generic.HashSet<int>(r.Blocks)
        let withSizes = r.Functions |> List.map (fun f -> f, sizeOf f, (callersOf f).Length)

        let out = ResizeArray<string>()

        out.Add(sprintf "; recursive code map from entry $%04X (seed-all: %b)" r.Entry r.SeedAll)

        let ef, eb, ec = r.EntryReachable
        out.Add(sprintf "; entry-reachable: %d functions, %d blocks, %d bytes" ef eb ec)
        out.Add(";")
        out.Add(
            sprintf
                "OVERVIEW: %d functions, %d blocks, %d code bytes"
                r.Functions.Length
                r.Blocks.Length
                (codeIn 0 0x10000)
        )

        out.Add(
            sprintf
                "  RAM $4000-$FFFF: %d bytes mapped as code (%.1f%%), %d bytes still unknown (%.1f%%)"
                ramCode
                (pct ramCode)
                (ramBytes - ramCode)
                (pct (ramBytes - ramCode))
        )

        out.Add("")
        out.Add("FUNCTIONS (address, name, mapped size, callers, first instruction):")

        for f in r.Functions do
            let n = nameOf f
            let label = if n = "" then "" else n
            let insn = Disasm.disasmMemory mem f

            out.Add(
                sprintf
                    "  $%04X  %-34s %5d bytes  %2d callers  %s"
                    f
                    label
                    (sizeOf f)
                    (callersOf f).Length
                    insn.Text
            )

        if r.Functions.IsEmpty then
            out.Add("  (nothing mapped - check the entry address)")

        out.Add("")

        out.Add("MOST CALLED (top 10):")

        for f, _, c in (withSizes |> List.sortByDescending (fun (_, _, c) -> c) |> List.truncate 10) do
            let n = nameOf f
            out.Add(sprintf "  %2d callers  $%04X  %s" c f n)

        out.Add("")
        out.Add("LARGEST (top 10):")

        for f, sz, _ in (withSizes |> List.sortByDescending (fun (_, sz, _) -> sz) |> List.truncate 10) do
            let n = nameOf f
            out.Add(sprintf "  %5d bytes  $%04X  %s" sz f n)

        out.Add("")
        out.Add("CALL GRAPH (function <- call sites):")

        for f in r.Functions do
            let n = nameOf f
            let suffix = if n = "" then "" else " " + n

            match callersOf f with
            | [] -> out.Add(sprintf "  $%04X%s <- (none: entry/interrupt/dynamic)" f suffix)
            | cs -> out.Add(sprintf "  $%04X%s <- %s" f suffix (String.concat " " (cs |> List.map (sprintf "$%04X"))))

        out.Add("")
        out.Add("LOOSE ENDS (block starts reached only by jumps - tables or mid-function labels?):")

        let mutable looseCount = 0

        for b in r.Blocks do
            if not (funcSet.Contains b) then
                looseCount <- looseCount + 1
                let js = r.JumpSites |> List.tryFind (fun (a, _) -> a = b) |> Option.map snd |> Option.defaultValue []

                out.Add(sprintf "  $%04X <- %s" b (String.concat " " (js |> List.map (sprintf "$%04X"))))

        if looseCount = 0 then
            out.Add("  (none)")

        out.Add("")
        out.Add("LISTING:")

        let mutable a = 0

        while a < 0x10000 do
            if r.IsCode[a] then
                if funcSet.Contains a then
                    let n = nameOf a
                    let suffix = if n = "" then "" else " " + n
                    out.Add(sprintf "F> $%04X%s" a suffix)
                elif blockSet.Contains a then
                    out.Add(sprintf "B> $%04X" a)

                let insn = Disasm.disasmMemory mem a
                let len = min insn.Length (0x10000 - a)
                let bytes = mem[a .. a + len - 1] |> Array.map (sprintf "%02X") |> String.concat " "

                out.Add(sprintf "    $%04X  %-11s %s" a bytes insn.Text)
                a <- a + insn.Length
            else
                a <- a + 1

        out |> Seq.toList
