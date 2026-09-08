namespace JetpacFR.Core

open System
open System.IO
open System.Text.Json

/// One marked block of the game image, from games/<id>/control.json.
/// Kinds steer CE emission: "code" disassembles to vocab ops, "data" and
/// "gap" stay as single labeled raw blocks (gap blocks are emitted but
/// usually zero-filled padding).
type BlockKind =
    | Code
    | Data
    | Gap

type Block =
    { Start: int
      EndExcl: int
      Name: string
      Kind: BlockKind }

/// The analysis-project control file: original image reference, the span
/// under analysis, entry info, the active version index, and the block
/// map. Lives at games/<id>/control.json.
type GameControl =
    { ImageFile: string
      Start: int
      EndExcl: int
      EntryPc: int
      ActiveVersion: int
      Blocks: Block list }

module GameProject =

    let private failf format = Printf.kprintf failwith format

    /// Parse an address: "$4000"/"0x4000" as hex, otherwise decimal.
    let parseAddr (s: string) : int =
        let t = s.Trim()

        if t.StartsWith "$" then
            Convert.ToInt32(t.Substring(1), 16)
        elif t.StartsWith("0x", StringComparison.OrdinalIgnoreCase) then
            Convert.ToInt32(t.Substring(2), 16)
        else
            int32 t



    let private parseBlocks (e: JsonElement) : Block list =
        match e.ValueKind with
        | JsonValueKind.Array ->
            [ for b in e.EnumerateArray() do
                  let get (k: string) =
                      match b.TryGetProperty k with
                      | true, v -> Some v
                      | _ -> None

                  let start = (get "start").Value.GetInt32()
                  let endE = (get "endExcl").Value.GetInt32()

                  let name =
                      match get "name" with
                      | Some v when v.ValueKind = JsonValueKind.String -> v.GetString()
                      | _ -> sprintf "block_%04X" start

                  let kind =
                      match get "kind" with
                      | Some v when v.ValueKind = JsonValueKind.String ->
                          match v.GetString() with
                          | "data" -> Data
                          | "gap" -> Gap
                          | _ -> Code
                      | _ -> Code

                  { Start = start
                    EndExcl = endE
                    Name = name
                    Kind = kind } ]
        | _ -> []

    /// Load and validate games/<id>/control.json.
    let loadControl (dir: string) : GameControl =
        let path = Path.Combine(dir, "control.json")
        let doc = JsonDocument.Parse(File.ReadAllText path)
        let root = doc.RootElement

        let str (k: string) (def: string) =
            match root.TryGetProperty k with
            | true, v when v.ValueKind = JsonValueKind.String -> v.GetString()
            | _ -> def

        let intOf (k: string) (def: int) =
            match root.TryGetProperty k with
            | true, v when v.ValueKind = JsonValueKind.Number -> v.GetInt32()
            | _ -> def

        let start = intOf "start" 0
        let endExcl = intOf "endExcl" 0x10000

        let c =
            { ImageFile = str "image" "original.bin"
              Start = start
              EndExcl = endExcl
              EntryPc = intOf "entryPc" start
              ActiveVersion = intOf "activeVersion" -1
              Blocks =
                root.TryGetProperty "blocks"
                |> function
                    | true, e -> parseBlocks e
                    | _ -> [] }

        if c.EndExcl <= c.Start then
            failf "control.json: endExcl (%d) must be > start (%d)" c.EndExcl c.Start

        if c.EndExcl > 0x10000 then
            failf "control.json: endExcl %d exceeds 64K" c.EndExcl

        let sorted = c.Blocks |> List.sortBy (fun b -> b.Start)
        // Overlap check (adjacent is fine).
        for pair in List.pairwise sorted do
            if snd(pair).Start < fst(pair).EndExcl then
                failf "control.json: blocks %s and %s overlap" (fst (pair)).Name (snd (pair)).Name

        for b in sorted do
            if b.Start < c.Start || b.EndExcl > c.EndExcl || b.Start >= b.EndExcl then
                failf
                    "control.json: block %s [%04X,%04X) outside span [%04X,%04X)"
                    b.Name
                    b.Start
                    b.EndExcl
                    c.Start
                    c.EndExcl

        { c with Blocks = sorted }

    /// The full 64K image assembled from the project's original.bin laid at
    /// `start` (bytes below `start` stay zero; ROM area included for parity
    /// reads). The generator works on this canonical form.
    let loadImage (dir: string) (c: GameControl) : byte[] =
        let mem = Array.zeroCreate<byte> 0x10000
        let raw = File.ReadAllBytes(Path.Combine(dir, c.ImageFile))
        let len = min raw.Length (0x10000 - c.Start)
        Array.blit raw 0 mem c.Start len
        mem

    /// The active version file (or the only one when activeVersion is unset).
    let activeVersionPath (dir: string) (c: GameControl) : string option =
        let vd = Path.Combine(dir, "versions")

        if not (Directory.Exists vd) then
            None
        else
            let files = Directory.GetFiles(vd, "v*.fs") |> Array.sort

            if files.Length = 0 then
                None
            elif c.ActiveVersion >= 0 then
                match
                    files
                    |> Array.tryFind (fun f -> Path.GetFileName(f).StartsWith(sprintf "v%03d_" c.ActiveVersion))
                with
                | Some f -> Some f
                | None ->
                    failf "control.json: activeVersion %d has no versions/v%03d_*.fs" c.ActiveVersion c.ActiveVersion
            else
                Some files[0]

    /// Materialize the active version into the compiled CeProgram.fs.
    /// Returns the copied version filename.
    let materialize (dir: string) : string =
        let c = loadControl dir

        match activeVersionPath dir c with
        | None -> failf "no versions/v*.fs in %s - run --gen-game first" dir
        | Some src ->
            let dst = Path.Combine(dir, "CeProgram.fs")
            File.Copy(src, dst, overwrite = true)
            Path.GetFileName src

    /// The Fable-clean module text for a game's Image.fs given the CE body
    /// lines and module name. The program compiles against Jetpac2.Core;
    /// binary bytes are NOT embedded here (the original stays on disk and
    /// parity runs in the shells/tests via the manifest image).
    let emitImageModule (moduleName: string) (body: string) : string =
        let sb = Text.StringBuilder()

        sb.AppendLine(sprintf "/// GENERATED by --gen-game: the active CE version of this game.")
        |> ignore

        sb.AppendLine("/// Regenerate via tools/watchgame.ps1 or the game fsproj build target.")
        |> ignore

        sb.AppendLine(sprintf "module %s" moduleName) |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine("open Jetpac2.Core") |> ignore
        sb.AppendLine("open Jetpac2.Core.Z80BuilderInstance") |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine("/// The per-frame CE driver over this game's program.") |> ignore
        sb.AppendLine("let program : Z80Op list = z80 {") |> ignore
        sb.Append(body) |> ignore
        sb.AppendLine("}") |> ignore
        sb.ToString()

    /// The Fable-clean runner module text (Game.fs twin of minimal's shape).
    let emitGameModule (moduleName: string) (imageModuleName: string) (entryPc: int) : string =
        let sb = Text.StringBuilder()

        sb.AppendLine(sprintf "/// GENERATED by --gen-game: drives the CE program on a port Machine.")
        |> ignore

        sb.AppendLine("/// Fable-clean: no I/O, no GUI - shells and tests share it.")
        |> ignore

        sb.AppendLine(sprintf "module %s" moduleName) |> ignore
        sb.AppendLine() |> ignore
        sb.AppendLine("open Jetpac2.Core") |> ignore
        sb.AppendLine() |> ignore

        sb.AppendLine(sprintf "/// Entry state: PC=0x%04X, SP=0xFFFE, interrupts off." entryPc)
        |> ignore

        sb.AppendLine("let entryState () : string =") |> ignore

        sb.AppendLine(
            "  \"af=0000\\nbc=0000\\nde=0000\\nhl=0000\\naf2=0000\\nbc2=0000\\nde2=0000\\nhl2=0000\\nix=0000\\niy=0000\\nsp=FFFE\\n"
            + sprintf "pc=%04X" entryPc
            + "\\ni=00\\nr=00\\nwz=FFFF\\niff1=false\\niff2=false\\nim=0\\nhalted=false\\nborder=7\\nbeeper=false\\ntapeEar=false\\ncycles=0\\nvideoNextTime=224\\nnextWrap=69664\\nirq=false\\n\""
        )
        |> ignore

        sb.ToString()
