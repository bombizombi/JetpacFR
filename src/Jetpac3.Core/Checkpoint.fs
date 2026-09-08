namespace Jetpac3.Core

open System
open System.IO

/// A saved machine state plus metadata for the explorer UI. `Memory` (64K) and
/// `StateText` are authoritative; scalar metadata is presentation-only.
type Checkpoint =
    { Id: string
      ParentId: string option
      Frame: int
      Pc: int
      Cycles: int64
      InputSummary: string
      CreatedAt: DateTime
      Memory: byte[]
      StateText: string }

module CheckpointSchema =
    [<Literal>]
    let CurrentVersion = 1

    let RequiredStateKeys =
        [ "af"
          "bc"
          "de"
          "hl"
          "af2"
          "bc2"
          "de2"
          "hl2"
          "ix"
          "iy"
          "sp"
          "pc"
          "i"
          "r"
          "wz"
          "iff1"
          "iff2"
          "im"
          "halted"
          "border"
          "beeper"
          "tapeEar"
          "cycles"
          "videoNextTime"
          "nextWrap" ]

/// The key=value register/timing text shared with the Jetpac2 fixture schema.
module StateText =
    let parse (text: string) : System.Collections.Generic.Dictionary<string, string> =
        let kv = System.Collections.Generic.Dictionary<string, string>()

        for line in text.Split('\n') do
            let line = line.Trim()

            if line.Length > 0 then
                match line.IndexOf '=' with
                | -1 -> ()
                | i -> kv[line.Substring(0, i).Trim()] <- line.Substring(i + 1).Trim()

        kv

    let validate (text: string) =
        if isNull text then
            failwith "checkpoint state.txt is null"

        let kv = parse text

        let missing =
            CheckpointSchema.RequiredStateKeys
            |> List.filter (fun key -> not (kv.ContainsKey key))

        if not (List.isEmpty missing) then
            failwithf "checkpoint state.txt is missing required keys: %s" (String.Join(", ", missing))

        let parseHex key =
            try
                Convert.ToInt32((kv[key]).Replace("0x", ""), 16) |> ignore
            with ex ->
                failwithf "checkpoint state key %s is not valid hexadecimal: %s" key ex.Message

        let parseInt64 key =
            try
                Int64.Parse kv[key] |> ignore
            with ex ->
                failwithf "checkpoint state key %s is not a valid integer: %s" key ex.Message

        let parseBool key =
            let value = kv[key].ToLowerInvariant()

            if value <> "true" && value <> "false" && value <> "0" && value <> "1" then
                failwithf "checkpoint state key %s is not a valid boolean: %s" key kv[key]

        [ "af"
          "bc"
          "de"
          "hl"
          "af2"
          "bc2"
          "de2"
          "hl2"
          "ix"
          "iy"
          "sp"
          "pc"
          "i"
          "r"
          "wz"
          "im"
          "border" ]
        |> List.iter parseHex

        [ "iff1"; "iff2"; "halted"; "beeper"; "tapeEar" ] |> List.iter parseBool
        [ "cycles"; "videoNextTime"; "nextWrap" ] |> List.iter parseInt64

type CanonicalStateHash =
    { Cpu: string
      Hardware: string
      MemoryPages: string list
      Full: string }

module CanonicalState =
    let private digest (bytes: byte[]) =
        use sha = Security.Cryptography.SHA256.Create()

        sha.ComputeHash bytes
        |> Seq.map (fun value -> value.ToString("x2"))
        |> String.concat ""

    let private hardwareText (trace: HardwareTrace) =
        let ports =
            trace.PortEvents
            |> List.map (fun event ->
                sprintf "%d:%04x:%02x:%d:%b" event.Tick event.Port event.Value event.Border event.Beeper)

        let writes =
            trace.MemoryWrites
            |> List.map (fun event ->
                sprintf "%d:%04x:%02x:%02x" event.Tick event.Address event.OldValue event.NewValue)

        String.concat "|" (ports @ writes)

    let compute (memory: byte[]) (stateText: string) (trace: HardwareTrace) : CanonicalStateHash =
        if isNull memory || memory.Length <> 0x10000 then
            invalidArg (nameof memory) "Canonical state requires 65536 bytes"

        StateText.validate stateText
        let cpu = digest (Text.Encoding.UTF8.GetBytes stateText)
        let pages = memory |> Array.chunkBySize 256 |> Array.toList |> List.map digest
        let hardware = digest (Text.Encoding.UTF8.GetBytes(hardwareText trace))

        let full =
            digest (Text.Encoding.UTF8.GetBytes(String.concat "|" (cpu :: hardware :: pages)))

        { Cpu = cpu
          Hardware = hardware
          MemoryPages = pages
          Full = full }

    let checkpoint (checkpoint: Checkpoint) =
        compute checkpoint.Memory checkpoint.StateText HardwareTrace.empty

/// Minimal JSON for fixed checkpoint metadata. The schema version is required
/// so incomplete or incompatible snapshots fail before machine mutation.
module private MetaJson =
    let private esc (s: string) =
        (if isNull s then "" else s).Replace("\\", "\\\\").Replace("\"", "\\\"")

    let write (frame: int) (parent: string option) (input: string) (created: DateTime) =
        let parentJs =
            match parent with
            | Some p -> "\"" + esc p + "\""
            | None -> "null"

        sprintf
            "{\"version\":%d,\"frame\":%d,\"parent\":%s,\"input\":\"%s\",\"created\":\"%s\"}"
            CheckpointSchema.CurrentVersion
            frame
            parentJs
            (esc input)
            (created.ToString("o"))

    let private readStr (key: string) (json: string) : string option =
        let m =
            Text.RegularExpressions.Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"")

        if m.Success then
            Some(m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\"))
        else
            None

    let private readInt (key: string) (json: string) : int option =
        let m =
            Text.RegularExpressions.Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+)")

        if m.Success then Some(int m.Groups[1].Value) else None

    let parse (json: string) : int option * int option * string option * string option * DateTime option =
        let created =
            readStr "created" json
            |> Option.bind (fun s ->
                match DateTime.TryParse(s) with
                | true, d -> Some d
                | _ -> None)

        readInt "version" json, readInt "frame" json, readStr "parent" json, readStr "input" json, created

type CheckpointStore(rootDir: string) =
    member this.Root = rootDir

    member private this.ValidateId(id: string) =
        if String.IsNullOrWhiteSpace id then
            invalidArg (nameof id) "Checkpoint ID cannot be empty"

        if
            id = "."
            || id = ".."
            || Path.GetFileName id <> id
            || id.IndexOfAny([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |])
               >= 0
        then
            invalidArg (nameof id) "Checkpoint ID must be a single safe path component"

    member this.Init() =
        Directory.CreateDirectory rootDir |> ignore

    member private this.Dir(id: string) =
        this.ValidateId id
        Path.Combine(rootDir, id)

    member this.Save(c: Checkpoint) =
        let dir = this.Dir c.Id

        if c.Memory.Length <> 0x10000 then
            failwithf "checkpoint %s memory must be exactly 65536 bytes" c.Id

        StateText.validate c.StateText
        this.Init()
        Directory.CreateDirectory dir |> ignore
        File.WriteAllBytes(Path.Combine(dir, "memory.bin"), c.Memory)
        File.WriteAllText(Path.Combine(dir, "state.txt"), c.StateText)
        File.WriteAllText(Path.Combine(dir, "meta.json"), MetaJson.write c.Frame c.ParentId c.InputSummary c.CreatedAt)

    member this.Load(id: string) : Checkpoint =
        let dir = this.Dir id

        if not (Directory.Exists dir) then
            failwithf "checkpoint %s does not exist" id

        let memoryPath = Path.Combine(dir, "memory.bin")
        let statePath = Path.Combine(dir, "state.txt")
        let metaPath = Path.Combine(dir, "meta.json")

        for path in [ memoryPath; statePath; metaPath ] do
            if not (File.Exists path) then
                failwithf "checkpoint %s is incomplete; missing %s" id path

        let memory = File.ReadAllBytes memoryPath

        if memory.Length <> 0x10000 then
            failwithf "checkpoint %s memory is %d bytes; expected 65536" id memory.Length

        let state = File.ReadAllText statePath
        StateText.validate state
        let json = File.ReadAllText metaPath
        let version, frame, parent, input, created = MetaJson.parse json

        match version with
        | Some v when v = CheckpointSchema.CurrentVersion -> ()
        | Some v ->
            failwithf
                "checkpoint %s uses unsupported schema version %d (expected %d)"
                id
                v
                CheckpointSchema.CurrentVersion
        | None -> failwithf "checkpoint %s metadata has no schema version" id

        let kv = StateText.parse state

        let hex (k: string) =
            Convert.ToInt32(kv[k].Replace("0x", ""), 16)

        let cycles = Int64.Parse kv["cycles"]

        match frame, created with
        | Some _, Some _ -> ()
        | _ -> failwithf "checkpoint %s metadata is missing a valid frame or created timestamp" id

        { Id = id
          ParentId = parent
          Frame = frame.Value
          Pc = hex "pc"
          Cycles = cycles
          InputSummary = defaultArg input ""
          CreatedAt = created.Value
          Memory = memory
          StateText = state }

    member this.List() : Checkpoint list =
        if not (Directory.Exists rootDir) then
            []
        else
            Directory.GetDirectories rootDir
            |> Array.map (fun d -> this.Load(Path.GetFileName d))
            |> Array.toList

    member this.Delete(id: string) =
        let dir = this.Dir id

        if Directory.Exists dir then
            Directory.Delete(dir, true)
