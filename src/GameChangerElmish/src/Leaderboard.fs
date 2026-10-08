module GameChangerElmish.Leaderboard

// Minimal daily leaderboard for a mission, kept entirely in the browser:
// localStorage under "puzzxle_lb_<mission>_<yyyy-MM-dd>". The multi-user part
// is FAKED for v1 - a deterministic set of ghost entries is derived from the
// mission id + date, so everyone sees a plausible board and the player's own
// real time is merged into it. When an API exists, `entries`/`record` are the
// two seams to replace.

open System
open Fable.Core
open Fable.Core.JsInterop

type Entry =
    { Name: string
      Millis: int
      You: bool }

module private Js =

    let localStorageGet (key: string) : string option =
        emitJsExpr
            key
            "(() => { try { const v = window.localStorage.getItem($0); return v === null ? null : v; } catch (e) { return null; } })()"
        |> Option.ofObj

    let localStorageSet (key: string) (value: string) : unit =
        emitJsExpr (key, value) "try { window.localStorage.setItem($0, $1); } catch (e) {}"

/// Deterministic ghosts: same mission + same day always yields the same
/// competitors, so the board does not shuffle under the player.
let private ghosts (missionId: string) (date: DateTime) (parSeconds: int) : Entry list =
    let seed =
        (missionId, date.ToString("yyyy-MM-dd"))
        ||> sprintf "%s|%s"
        |> fun s -> s.GetHashCode()

    let rnd = Random(seed)

    let names =
        [ "ZxBob"
          "SinclairSuzy"
          "CavernKing"
          "RZXAddict"
          "48K_Dani"
          "TapeLoader"
          "PixelPete"
          "BorderFlash"
          "KempstonKid"
          "BleepBloop" ]

    // Log-ish spread around the par time: a couple of fast ghosts, most
    // clustered past it.
    let millis i =
        let factor = 0.55 + (rnd.NextDouble() * rnd.NextDouble()) * 2.2 + float i * 0.12

        int (float parSeconds * factor * 1000.0)

    let count = 7 + rnd.Next(3)

    names
    |> List.sortBy (fun _ -> rnd.Next())
    |> List.truncate count
    |> List.mapi (fun i n -> { Name = n; Millis = millis i; You = false })
    |> List.sortBy (fun e -> e.Millis)

let private storageKey (missionId: string) (date: DateTime) =
    sprintf "puzzxle_lb_%s_%s" missionId (date.ToString("yyyy-MM-dd"))

[<Literal>]
let PlayerName = "You"

/// Your stored best for the mission today, if any.
let yourBest (missionId: string) (date: DateTime) : int option =
    Js.localStorageGet (storageKey missionId date)
    |> Option.bind (fun s ->
        match Int32.TryParse s with
        | true, v when v > 0 -> Some v
        | _ -> None)

/// The full board: ghosts + your best, sorted.
let entries (mission: Missions.Mission) (date: DateTime) : Entry list =
    let yours =
        yourBest mission.Id date
        |> Option.map (fun ms -> [ { Name = PlayerName; Millis = ms; You = true } ])
        |> Option.defaultValue []

    ghosts mission.Id date mission.ParSeconds @ yours
    |> List.sortBy (fun e -> e.Millis)

/// Record a run: keeps your best time for the day, returns the updated board
/// and the 1-based rank your result landed on.
let record (mission: Missions.Mission) (date: DateTime) (millis: int) : Entry list * int =
    let previous = yourBest mission.Id date |> Option.defaultValue Int32.MaxValue
    let best = min previous millis
    Js.localStorageSet (storageKey mission.Id date) (string best)

    let board =
        ghosts mission.Id date mission.ParSeconds @ [ { Name = PlayerName; Millis = best; You = true } ]
        |> List.sortBy (fun e -> e.Millis)

    let rank =
        board
        |> List.indexed
        |> List.tryPick (fun (i, e) -> if e.You then Some(i + 1) else None)
        |> Option.defaultValue (board.Length)

    board, rank

let format (millis: int) : string =
    let total = millis / 1000
    sprintf "%d:%02d.%02d" (total / 60) (total % 60) (millis % 1000 / 10)
