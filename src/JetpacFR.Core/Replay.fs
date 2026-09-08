namespace JetpacFR.Core

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json

/// Asset identity attached to an automatic per-game replay. A recording is
/// never replayed against a different ROM, tape, or program image.
type ReplayFingerprint =
    { RomSha256: string
      TzxSha256: string
      ProgramSha256: string
      ProgramAddress: int option }

type ReplayLoad =
    | ReplayMissing
    | ReplayLoaded of KeyEvent list
    | ReplayIgnored of string

module ReplayStore =
    let Format = "jetpacfr-replay"
    let Version = 1
    let FileName = "replay.json"

    let private sha256Bytes (bytes: byte[]) =
        use sha = SHA256.Create()

        sha.ComputeHash bytes
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let private sha256File (path: string) =
        if String.IsNullOrWhiteSpace path || not (File.Exists path) then
            ""
        else
            sha256Bytes (File.ReadAllBytes path)

    let fingerprint (game: GameManifest) : ReplayFingerprint =
        { RomSha256 = sha256File game.Rom
          TzxSha256 = sha256File game.Tzx
          ProgramSha256 = game.ProgramBin |> Option.map sha256File |> Option.defaultValue ""
          ProgramAddress = game.ProgramAddress }

    let path (game: GameManifest) =
        Path.Combine(game.GameDirectory, FileName)

    let private quote (value: string) =
        JsonSerializer.Serialize(if isNull value then "" else value)

    let private optionalInt (value: int option) =
        match value with
        | Some v -> string v
        | None -> "null"

    let private serialize (game: GameManifest) (events: KeyEvent list) =
        let fp = fingerprint game

        let eventText =
            events
            |> List.map (fun e ->
                sprintf "{\"frame\":%d,\"row\":%d,\"bit\":%d,\"pressed\":%b}" e.Frame e.Row e.Bit e.Pressed)
            |> String.concat ","

        sprintf
            "{\"format\":%s,\"version\":%d,\"gameId\":%s,\"assetFingerprint\":{\"romSha256\":%s,\"tzxSha256\":%s,\"programSha256\":%s,\"programAddress\":%s},\"events\":[%s]}"
            (quote Format)
            Version
            (quote game.GameId)
            (quote fp.RomSha256)
            (quote fp.TzxSha256)
            (quote fp.ProgramSha256)
            (optionalInt fp.ProgramAddress)
            eventText

    let save (game: GameManifest) (events: seq<KeyEvent>) : Result<unit, string> =
        try
            Directory.CreateDirectory game.GameDirectory |> ignore
            let target = path game
            let temporary = target + ".tmp"
            let body = serialize game (events |> Seq.toList)
            File.WriteAllText(temporary, body, Encoding.UTF8)
            File.Move(temporary, target, true)
            Ok()
        with ex ->
            Error ex.Message

    let private tryString (root: JsonElement) (name: string) =
        match root.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.String -> Some(value.GetString())
        | _ -> None

    let private tryInt (root: JsonElement) (name: string) =
        match root.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.Number -> Some(value.GetInt32())
        | _ -> None

    let private parseEvent (value: JsonElement) =
        match tryInt value "frame", tryInt value "row", tryInt value "bit", value.TryGetProperty "pressed" with
        | Some frame, Some row, Some bit, (true, pressed) when
            frame >= 0
            && row >= 0
            && row < 8
            && bit >= 0
            && bit < 5
            && (pressed.ValueKind = JsonValueKind.True
                || pressed.ValueKind = JsonValueKind.False)
            ->
            Some
                { Frame = frame
                  Row = row
                  Bit = bit
                  Pressed = pressed.GetBoolean() }
        | _ -> None

    let private parseEvents (array: JsonElement) =
        if array.ValueKind <> JsonValueKind.Array then
            None
        else
            let parsed = array.EnumerateArray() |> Seq.map parseEvent |> Seq.toList

            if parsed |> List.forall Option.isSome then
                Some(parsed |> List.choose id)
            else
                None

    let private matchesFingerprint (game: GameManifest) (root: JsonElement) =
        let current = fingerprint game

        match root.TryGetProperty "assetFingerprint" with
        | false, _ -> false
        | true, fp ->
            let programAddress = tryInt fp "programAddress"

            tryString fp "romSha256" = Some current.RomSha256
            && tryString fp "tzxSha256" = Some current.TzxSha256
            && tryString fp "programSha256" = Some current.ProgramSha256
            && programAddress = current.ProgramAddress

    let tryLoad (game: GameManifest) : ReplayLoad =
        let target = path game

        if not (File.Exists target) then
            ReplayMissing
        else
            try
                use doc = JsonDocument.Parse(File.ReadAllText target)
                let root = doc.RootElement
                // Accept the old manually exported bare array as a one-time legacy
                // import. Automatic files always use the versioned object format.
                if root.ValueKind = JsonValueKind.Array then
                    match parseEvents root with
                    | Some events -> ReplayLoaded events
                    | None -> ReplayIgnored "legacy replay contains an invalid event"
                else
                    let format = tryString root "format" |> Option.defaultValue ""
                    let version = tryInt root "version" |> Option.defaultValue 0
                    let gameId = tryString root "gameId" |> Option.defaultValue ""

                    if format <> Format then
                        ReplayIgnored "unsupported replay format"
                    elif version <> Version then
                        ReplayIgnored(sprintf "unsupported replay version %d" version)
                    elif gameId <> game.GameId then
                        ReplayIgnored "replay belongs to another game"
                    elif not (matchesFingerprint game root) then
                        ReplayIgnored "replay assets do not match the selected game"
                    else
                        match root.TryGetProperty "events" with
                        | true, events ->
                            match parseEvents events with
                            | Some parsed -> ReplayLoaded parsed
                            | None -> ReplayIgnored "replay contains an invalid event"
                        | _ -> ReplayIgnored "replay has no events array"
            with ex ->
                ReplayIgnored ex.Message
