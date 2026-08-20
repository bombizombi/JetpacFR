namespace JetpacFR.Core

open System
open System.IO
open System.Text.Json

/// One game's description. Everything the shells need to boot and drive a
/// game lives here, so adding a game is adding a folder + manifest instead
/// of editing code.
type GameManifest =
  { /// Stable directory-derived identifier (for example `jetpac`).
    GameId: string
    /// Absolute path to the manifest file.
    ManifestPath: string
    /// Absolute directory containing the manifest and game-owned files.
    GameDirectory: string
    /// Display name (game selector).
    Name: string
    /// Explicit startup default. At most one manifest should set this.
    Default: bool
    /// Path to the 48K ROM image (tape games).
    Rom: string
    /// Path to the TZX tape (tape games).
    Tzx: string
    /// "auto": Boot.bootToEntry heuristics (loading screen + code match +
    /// PC in the loaded region). "manual": the user boots the loader in the
    /// oracle and presses "Set as game entry" (works for ANY game).
    /// "program": a raw Z80 image is loaded at `ProgramAddress` and run
    /// (no tape loader involved).
    Boot: string
    /// Optional demo/attest key script (frame, row, bit, pressed).
    Script: string option
    /// Program games: the raw binary image.
    ProgramBin: string option
    /// Program games: the load address (e.g. 32768).
    ProgramAddress: int option }

module Manifest =

  let load (path: string) : GameManifest =
    use doc = JsonDocument.Parse(File.ReadAllText path)
    let root = doc.RootElement
    let str (k: string) (def: string) =
      match root.TryGetProperty k with
      | true, e when e.ValueKind = JsonValueKind.String -> e.GetString()
      | _ -> def
    let manifestPath = Path.GetFullPath path
    let baseDir = Path.GetDirectoryName manifestPath
    let gameId = DirectoryInfo(baseDir).Name.ToLowerInvariant()
    let boolv (k: string) (def: bool) =
      match root.TryGetProperty k with
      | true, e when e.ValueKind = JsonValueKind.True -> true
      | true, e when e.ValueKind = JsonValueKind.False -> false
      | _ -> def
    let resolve (p: string) = if Path.IsPathRooted p then p else Path.Combine(baseDir, p)
    let script =
      match root.TryGetProperty "script" with
      | true, e when e.ValueKind = JsonValueKind.String -> Some(resolve (e.GetString()))
      | _ -> None
    let programBin, programAddress =
      match root.TryGetProperty "program" with
      | true, e when e.ValueKind = JsonValueKind.Object ->
        let bin =
          match e.TryGetProperty "bin" with
          | true, v when v.ValueKind = JsonValueKind.String -> Some(resolve (v.GetString()))
          | _ -> None
        let addr =
          match e.TryGetProperty "address" with
          | true, v when v.ValueKind = JsonValueKind.Number -> Some(v.GetInt32())
          | _ -> None
        bin, addr
      | _ -> None, None
    { GameId = gameId
      ManifestPath = manifestPath
      GameDirectory = baseDir
      Name = str "name" "unnamed"
      Default = boolv "default" false
      Rom = resolve (str "rom" "")
      Tzx = resolve (str "tzx" "")
      Boot = str "boot" "auto"
      Script = script
      ProgramBin = programBin
      ProgramAddress = programAddress }

  /// Discover `games/*/manifest.json` under `gamesDir`, in stable ID order.
  let discover (gamesDir: string) : GameManifest list =
    if not (Directory.Exists gamesDir) then []
    else
      Directory.GetDirectories gamesDir
      |> Array.choose (fun d ->
        let p = Path.Combine(d, "manifest.json")
        if File.Exists p then Some(load p) else None)
      |> Array.sortBy (fun g -> g.GameId)
      |> Array.toList

/// Load a raw Z80 program image (manifest boot mode "program") into the
/// entry cache: bytes at `address`, PC=address, SP=0xFFFE, interrupts off.
/// The cache is keyed on the image file itself (per-game slot), so the
/// seeding runs once per image.
module ProgramEntry =

  let seed (binPath: string) (address: int) : unit =
    let image = File.ReadAllBytes binPath
    let mem = Array.zeroCreate<byte> 0x10000
    let len = min image.Length (0x10000 - address)
    Array.blit image 0 mem address len
    let state =
      sprintf
        "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=FFFE\npc=%04X\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"
        address
    EntryCache.save binPath binPath mem state
