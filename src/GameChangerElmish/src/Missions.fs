module GameChangerElmish.Missions

// The "Daily puzZXle" mission catalogue. A mission pairs a registered game
// with the emulator "spot" that fires when the goal is reached. Spots are
// deliberately small data so they can be swapped as the reverse-engineering
// work sharpens them:
//
//   - ScreenFlash: the Manic Miner cavern-complete sequence flashes the whole
//     display bright; the host measures the bright-pixel ratio of the rendered
//     frame, so the detector works no matter what the game keeps in RAM.
//   - MemoryEquals / MemoryNotEqual: polled against live RAM every frame.
//
// Neither spot needs an API: they are evaluated inside the frame loop.

open System

type Detect =
    | MemoryEquals of address: int * value: int
    | MemoryNotEqual of address: int * value: int
    | ScreenFlash

type Mission =
    { /// Stable id used by storage keys (leaderboards).
      Id: string
      /// GameId in the emulator registry.
      GameId: string
      Title: string
      Goal: string
      /// Shown under the screen; the Spectrum menus are authentic.
      Hint: string
      /// Rough par time, only used to scatter the fake leaderboard.
      ParSeconds: int
      Detect: Detect }

let manicMiner: Mission =
    { Id = "mm-central-cavern"
      GameId = "manicminer"
      Title = "Manic Miner"
      Goal = "Clear Central Cavern"
      Hint =
        "Press ENTER to start. Keyboard: arrows or WASD to move, Space to jump. Touch: pad + FIRE."
      ParSeconds = 120
      Detect = ScreenFlash }

// $DC3B is the wave-state machine's monotonic progress max (clamp-store cell,
// see uridium_found_symbols $8B2C/$A796): mid-base-1 play holds 0x0E, the
// title screen 0x00, and the wave state machine keys on it reaching $FF -
// the "base cleared, land now" state. Swap here when a played landing pins
// the exact post-landing cell.
let uridium: Mission =
    { Id = "uridium-base-1"
      GameId = "uridium"
      Title = "Uridium"
      Goal = "Clear Base 1 and land the ship"
      Hint =
        "Fly the run, shoot the ground defences, then land when the coast is clear. Keyboard: arrows or WASD, Space to fire."
      ParSeconds = 210
      Detect = MemoryEquals(0xDC3B, 0xFF) }

let all: Mission list =
    [ manicMiner
      uridium ]

let forGame (gameId: string) : Mission option =
    all |> List.tryFind (fun m -> m.GameId = gameId.ToLowerInvariant())

/// The mission the calendar highlights. With two missions this alternates
/// daily; both stay playable.
let todaysIndex (date: DateTime) : int =
    (date.DayOfYear - 1) % all.Length

let todays (date: DateTime) : Mission = all[todaysIndex date]
