namespace JetpacFR.Core


/// The per-game CE registry entry: one compiled game version, exposed by
/// each games/<id> project as a module value so shells can look it up by
/// manifest GameId without hardcoding names. Shaped for Fable: records +
/// closures only.
type GameImage =
  { /// Manifest GameId this bundle serves (e.g. "minimal").
    GameId: string
    /// Display name (engine toggle / parity label).
    Name: string
    /// Full 64K entry memory with the image at its load address (the
    /// exact array handed to Machine.LoadState, which blits from 0).
    Memory: byte[]
    /// Load address of the program inside Memory (parity span origin).
    BaseAddress: int
    /// The active CE program; assemble == Memory[Base..] byte-for-byte.
    Program: Jetpac2.Core.Z80Op list
    /// Entry register state string (ProgramEntry shape) for LoadState.
    EntryState: string }

module GameRegistry =
  let mutable private entries: GameImage list = []

  /// Register a game image (called once per game module at shell startup).
  let register (g: GameImage) : unit = entries <- g :: entries

  /// All registered games (registration order reversed - stable enough:
  /// registration happens in one deterministic module-init sequence).
  let all () : GameImage list = List.rev entries

  /// Lookup by manifest GameId.
  let tryFind (gameId: string) : GameImage option =
    all () |> List.tryFind (fun g -> g.GameId = gameId)

  /// Byte parity of the registered program vs its own memory image,
  /// compared over [BaseAddress, BaseAddress + assembled.Length).
  let parity (g: GameImage) : int * int * int list =
    let assembled = Jetpac2.Core.Z80.assemble g.Program
    let n = min assembled.Length (0x10000 - g.BaseAddress)
    let mutable matching = 0
    let mismatches = ResizeArray<int>()
    for i in 0 .. n - 1 do
      if assembled[i] = g.Memory[g.BaseAddress + i] then matching <- matching + 1
      elif mismatches.Count < 8 then mismatches.Add i
    if assembled.Length > n && mismatches.Count < 8 then mismatches.Add -1
    matching, n, List.ofSeq mismatches
