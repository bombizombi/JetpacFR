namespace JetpacFR.Core

open System
open System.IO
open System.Text.Json

/// One game's description. Everything the shells need to boot and drive a
/// game lives here, so adding a game is adding a folder + manifest instead
/// of editing code.
type GameManifest =
  { /// Display name (game selector).
    Name: string
    /// Path to the 48K ROM image.
    Rom: string
    /// Path to the TZX tape.
    Tzx: string
    /// "auto": Boot.bootToEntry heuristics (loading screen + code match +
    /// PC in the loaded region). "manual": the user boots the loader in the
    /// oracle and presses "Set as game entry" (works for ANY game).
    Boot: string
    /// Optional demo/attest key script (frame, row, bit, pressed).
    Script: string option }

module Manifest =

  let load (path: string) : GameManifest =
    use doc = JsonDocument.Parse(File.ReadAllText path)
    let root = doc.RootElement
    let str (k: string) (def: string) =
      match root.TryGetProperty k with
      | true, e when e.ValueKind = JsonValueKind.String -> e.GetString()
      | _ -> def
    let baseDir = Path.GetDirectoryName(Path.GetFullPath path)
    let resolve (p: string) = if Path.IsPathRooted p then p else Path.Combine(baseDir, p)
    let script =
      match root.TryGetProperty "script" with
      | true, e when e.ValueKind = JsonValueKind.String -> Some(resolve (e.GetString()))
      | _ -> None
    { Name = str "name" "unnamed"
      Rom = resolve (str "rom" "")
      Tzx = resolve (str "tzx" "")
      Boot = str "boot" "auto"
      Script = script }

  /// Discover `games/*/manifest.json` under `gamesDir`.
  let discover (gamesDir: string) : GameManifest list =
    if not (Directory.Exists gamesDir) then []
    else
      Directory.GetDirectories gamesDir
      |> Array.choose (fun d ->
        let p = Path.Combine(d, "manifest.json")
        if File.Exists p then Some(load p) else None)
      |> Array.toList
