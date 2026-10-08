module MissionProbe.Main

// Headless mission-spot probe: boot a tape through the oracle, optionally
// replay a games/<id>/replay.json key script, poke memory, render ASCII
// screens, and diff RAM across a frame range. The interactive workbench
// (trace, miner, cheat engine) answers "where does the game keep X?" from
// the GUI; this answers it from the console so it can run in automation.
//
//   dotnet run --project tools/MissionProbe -- run \
//     --rom assets/48.rom --tzx "games/manicminer/Manic Miner.tzx" \
//     [--replay games/manicminer/replay.json] [--max 2000] \
//     [--at FRAME --poke 0x5E6C=0x12,...] [--diff FRAME2] \
//     [--shots 300,600,900] [--watch 0x5E6C,0x5E6D] [--csv out.csv]

open System
open System.IO
open System.Text
open System.Text.Json

let parseNum (s: string) : int =
    let s = if s.StartsWith "0x" || s.StartsWith "0X" then s.Substring 2 else s

    if s.StartsWith "-" then -Convert.ToInt32(s.Substring 1, 16)
    else Convert.ToInt32(s, 16)

/// "frame:row:bit:p,frame:row:bit:p,..." script (p = 0/1).
let loadScript (s: string) : Map<int, (int * int * bool) list> =
    s.Split(',')
    |> Array.choose (fun kv ->
        match kv.Split(':') with
        | [| f; r; b; p |] -> Some(int f, int r, int b, (p = "1"))
        | _ -> None)
    |> Array.toList
    |> List.groupBy (fun (f, _, _, _) -> f)
    |> List.map (fun (f, xs) -> f, xs |> List.map (fun (_, r, b, p) -> (r, b, p)))
    |> Map.ofList

let argsMap (argv: string list) : Map<string, string> =
    let rec go (acc: Map<string, string>) (xs: string list) : Map<string, string> =
        match xs with
        | a :: v :: t when a.StartsWith "--" -> go (Map.add (a.Substring 2) v acc) t
        | [] -> acc
        | bad -> failwithf "bad args near %A" bad

    go Map.empty argv

let optInt (m: Map<string, string>) (k: string) : int option = m |> Map.tryFind k |> Option.map int

let csvOf (m: Map<string, string>) (k: string) : int list =
    match m |> Map.tryFind k with
    | None -> []
    | Some s -> s.Split(',') |> Array.map int |> Array.toList

/// Replay.json events grouped by frame (keys land before the frame runs -
/// the app's replay convention).
let loadReplay (path: string) : Map<int, (int * int * bool) list> =
    let doc: JsonDocument = JsonDocument.Parse(File.ReadAllText path)

    let events =
        doc.RootElement.GetProperty("events").EnumerateArray()
        |> Seq.map (fun (e: JsonElement) ->
            int (e.GetProperty("frame").GetInt32()),
            int (e.GetProperty("row").GetInt32()),
            int (e.GetProperty("bit").GetInt32()),
            e.GetProperty("pressed").GetBoolean())
        |> Seq.toList

    (Map.empty, events |> List.groupBy (fun (f, _, _, _) -> f))
    ||> List.fold (fun acc (f, xs) ->
        Map.add f (xs |> List.map (fun (_, r, b, p) -> (r, b, p))) acc)

/// popcount over one byte.
let popcount (b: byte) : int =
    let mutable v = int b
    let mutable c = 0

    while v <> 0 do
        if v &&& 1 = 1 then c <- c + 1
        v <- v >>> 1

    c

/// Cheap screen signature: attr ink+bright mass and bitmap ink mass.
let screenStats (mem: byte[]) : int * int =
    let mutable attrSum = 0
    let mutable bitSum = 0

    for a in 0x5800 .. 0x5AFF do
        let attr = int mem[a]
        attrSum <- attrSum + (attr &&& 7) + (if attr &&& 0x40 <> 0 then 8 else 0)

    for a in 0x4000 .. 0x5AFF do
        bitSum <- bitSum + popcount mem[a]

    attrSum, bitSum

/// 24x32 ASCII render of the bitmap + attrs, one line per pixel row of cells.
let renderAscii (mem: byte[]) : string =
    let sb = StringBuilder()
    let ramp = " .:*#@"

    // Spectrum bitmap interleave: char row r (0..7), cell row within third.
    for third in 0..2 do
        for crow in 0..7 do
            for line in 0..7 do
                for col in 0..31 do
                    let addr =
                        0x4000 + third * 0x800 + crow * 0x20 + (line % 8) * 0x100 + col

                    let attrAddr = 0x5800 + (third * 8 + crow) * 32 + col
                    let attr = int mem[attrAddr]
                    let ink = attr &&& 7
                    let bright = if attr &&& 0x40 <> 0 then 1 else 0
                    let cov = float (popcount mem[addr]) / 8.0
                    let level = int (Math.Round(cov * 2.0)) + bright
                    let idx = min 5 ((max 0 level) + (if ink > 0 && cov > 0.01 then 1 else 0))
                    sb.Append ramp[idx] |> ignore

                sb.AppendLine() |> ignore

    sb.ToString()

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "warm" :: rest ->
        // Run a game's shipped web image (the exact site boot path: CEGame
        // over WebImage memory/state/program) and watch cells, so mission
        // spots are validated against the real runtime.
        let opts =
            let rec go (acc: Map<string, string>) (xs: string list) =
                match xs with
                | a :: v :: t when a.StartsWith "--" -> go (Map.add (a.Substring 2) v acc) t
                | [] -> acc
                | bad -> failwithf "bad args near %A" bad

            go Map.empty rest

        let gameId = opts |> Map.tryFind "game" |> Option.defaultValue "uridium"

        let registry =
            match gameId with
            | "manicminer" ->
                Some(
                    ManicminerGame.WebImage.program,
                    ManicminerGame.WebImage.memory,
                    ManicminerGame.WebImage.entryState
                )
            | _ ->
                Some(
                    UridiumGame.WebImage.program,
                    UridiumGame.WebImage.memory,
                    UridiumGame.WebImage.entryState
                )

        let program, memory, entryState = registry |> Option.get
        let g = JetpacFR.Core.CEGame(program, memory, entryState)
        let maxF = opts |> Map.tryFind "max" |> Option.map int |> Option.defaultValue 3000

        let watch =
            opts
            |> Map.tryFind "watch"
            |> Option.map (fun s -> s.Split(',') |> Array.map parseNum |> Array.toList)
            |> Option.defaultValue []

        for f in 1 .. maxF do
            let frameStart, _ = g.RunFrame()
            g.DiscardBeeper()

            if f % 100 = 0 then
                // attr + bitmap ink mass over the screen file: enough to tell
                // title / gameplay / loading regimes apart.
                let mem = g.Memory
                let mutable attrSum = 0
                let mutable bitSum = 0

                for a in 0x5800 .. 0x5AFF do
                    attrSum <- attrSum + (int mem[a] &&& 7)

                for a in 0x4000 .. 0x5AFF do
                    bitSum <- bitSum + popcount mem[a]

                printfn
                    "f=%d %s attr=%d bit=%d"
                    f
                    (watch |> List.map (fun a -> sprintf "[%04X]=%02X" a (int g.Memory[a])) |> String.concat " ")
                    attrSum
                    bitSum

        0
    | "writeimage" :: rest ->
        // Assemble a game's original.bin from its tape CODE spans.
        let opts =
            let rec go (acc: Map<string, string>) (xs: string list) =
                match xs with
                | a :: v :: t when a.StartsWith "--" -> go (Map.add (a.Substring 2) v acc) t
                | [] -> acc
                | bad -> failwithf "bad args near %A" bad

            go Map.empty rest

        let spans = Jetpac3.Core.Boot.findCodeSpans (File.ReadAllBytes opts["tzx"])
        let mem = Array.zeroCreate<byte> 0x10000

        for (start, bytes) in spans do
            Array.blit bytes 0 mem start bytes.Length

        let out = opts |> Map.tryFind "out" |> Option.defaultValue "original.bin"
        File.WriteAllBytes(out, mem)

        printfn
            "wrote %s from %d spans (%d bytes)"
            out
            spans.Length
            (spans |> List.sumBy (fun (_, b) -> b.Length))

        0
    | _ ->
    try
        let m =
            match Array.toList argv with
            | _cmd :: rest -> argsMap rest
            | [] -> Map.empty

        let rom = m["rom"]
        let tzx = m["tzx"]
        let maxF = optInt m "max" |> Option.defaultValue 100000
        let at = optInt m "at"
        let diffF = optInt m "diff"
        let shots = csvOf m "shots" |> Set.ofList

        let watch =
            match m |> Map.tryFind "watch" with
            | None -> []
            | Some s -> s.Split(',') |> Array.map parseNum |> Array.toList

        let pokes =
            match m |> Map.tryFind "poke" with
            | None -> []
            | Some s ->
                s.Split(',')
                |> Array.map (fun kv ->
                    let a, v = kv.Split('=') |> fun x -> x[0], x[1]
                    parseNum a, parseNum v)
                |> Array.toList

        let replayPath = m |> Map.tryFind "replay"

        let events =
            match replayPath with
            | None -> Map.empty
            | Some p ->
                let until = optInt m "replay-until" |> Option.defaultValue Int32.MaxValue

                loadReplay p
                |> Map.map (fun f xs -> if f <= until then xs else [])
                |> Map.filter (fun _ xs -> not xs.IsEmpty)

        let events =
            match m |> Map.tryFind "script" with
            | None -> events
            | Some s -> Map.fold (fun acc k v -> Map.add k v acc) events (loadScript s)

        let csvPath = m |> Map.tryFind "csv"
        let dumpAt = optInt m "dumpram"

        let spec, entryCycles = Jetpac3.Core.Boot.bootToEntry rom tzx None
        printfn "booted to entry at cycles %d" entryCycles

        let lastEventFrame =
            if Map.isEmpty events then 0 else Map.toList events |> List.maxBy fst |> fst

        let mutable snapAt: byte[] option = None
        let mutable watchLog = Map.empty<int, (int * int) list>
        let csv = csvPath |> Option.map (fun p -> new StreamWriter(File.Create p))

        csv
        |> Option.iter (fun w -> w.WriteLine "frame,attrSum,bitSum")

        for f in 0 .. maxF - 1 do
            match Map.tryFind f events with
            | Some evs ->
                for (r, b, p) in evs do
                    spec.SetKey(r, b, p)
            | None -> ()

            match at with
            | Some a when f = a ->
                for (addr, v) in pokes do
                    spec.Memory[addr] <- byte v
                    printfn "poke: [%04X] <- %02X" addr v

                snapAt <- Some(Array.copy spec.Memory)
            | _ -> ()

            spec.RunFrame()

            match dumpAt with
            | Some d when f = d ->
                let path = m |> Map.tryFind "dumpfile" |> Option.defaultValue "ramdump.bin"
                File.WriteAllBytes(path, spec.Memory)
                printfn "ram dump at frame %d -> %s" f path
            | _ -> ()

            match csv with
            | Some w ->
                let a, b = screenStats spec.Memory
                w.WriteLine(sprintf "%d,%d,%d" f a b)
            | None -> ()

            if not (watchLog.ContainsKey f) then
                let vals = watch |> List.map (fun addr -> addr, int spec.Memory[addr])
                watchLog <- Map.add f vals watchLog

            if shots.Contains f then
                printfn "---- frame %d ----" f
                printfn "%s" (renderAscii spec.Memory)

            ignore lastEventFrame

        match diffF, snapAt with
        | Some d, Some a ->
            let b = Array.copy spec.Memory
            printfn "---- RAM diff %s .. %d (changed runs) ----" (at |> Option.map string |> Option.defaultValue "?") d
            let mutable i = 0x4000

            while i < 0x10000 do
                if a[i] <> b[i] then
                    let start = i
                    let mutable j = i

                    while j < 0x10000 && a[j] <> b[j] do
                        j <- j + 1

                    let width = min 16 (j - start)
                    let oldHex = [ for k in start .. start + width - 1 -> sprintf "%02X" (int a[k]) ] |> String.concat " "
                    let newHex = [ for k in start .. start + width - 1 -> sprintf "%02X" (int b[k]) ] |> String.concat " "
                    printfn "%04X-%04X  %s  ->  %s" start (j - 1) oldHex newHex
                    i <- j
                else
                    i <- i + 1
        | _ -> ()

        if not (List.isEmpty watch) then
            printfn "---- watch ----"
            let step = max 1 (maxF / 100)

            for f in 0 .. step .. maxF - 1 do
                match watchLog.TryFind f with
                | Some vals ->
                    printfn
                        "f=%d %s"
                        f
                        (vals |> List.map (fun (a, v) -> sprintf "[%04X]=%02X" a v) |> String.concat " ")
                | None -> ()

        csv |> Option.iter (fun w ->
            w.Close()
            printfn "csv written")

        0
    with ex ->
        eprintfn "probe failed: %s" ex.Message
        1
