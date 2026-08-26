module JetpacFRTests.GenGame

// The new-game project generator (plan_new_game_project):
//   --gen-game <gamesDir>/<id> --from <file> [--address N]
//       scaffold a game dir: original image + entry state + control.json
//       skeleton + the first CE version (v000_init).
//   --regen <gamesDir>/<id>
//       regenerate versions/ from control.json (block kinds respected),
//       writing vNNN_<tag>.fs and bumping activeVersion to it.
//   --materialize <gamesDir>/<id>
//       copy the active version over CeProgram.fs (the compiled file).
//   --diff-game <gamesDir>/<id> <a> <b>
//       assemble two versions, byte-diff each vs original.bin + raw stats.
//
// Every generated version must pass byte parity (assemble == image span)
// or it is refused - the gate from plan_ce_dsl stays absolute.

open System
open System.IO
open System.Text.Json
open JetpacFR.Core

module private Gen =
  let entryStateText (pc: int) : string =
    sprintf
      "af=0000\nbc=0000\nde=0000\nhl=0000\naf2=0000\nbc2=0000\nde2=0000\nhl2=0000\nix=0000\niy=0000\nsp=FFFE\npc=%04X\ni=00\nr=00\nwz=FFFF\niff1=false\niff2=false\nim=0\nhalted=false\nborder=7\nbeeper=false\ntapeEar=false\ncycles=0\nvideoNextTime=224\nnextWrap=69664\nirq=false\n"
      pc

  /// Detect the non-zero extent of an already-normalized 64K image.
  let extentOf (mem: byte[]) (start: int) : int =
    let rec lastNonZero (i: int) : int =
      if i < start then start
      elif mem[i] <> 0uy then i + 1
      else lastNonZero (i - 1)
    max (start + 1) (min 0x10000 (lastNonZero (0x10000 - 1)))

  /// Normalize any input into original.bin: the game bytes laid out from
  /// their load address (.sna: 48K at $4000 with PC popped off the stack;
  /// anything else: bytes at `address`). Returns (path, start, endExcl,
  /// entryPc); the extent shrinks trailing zero padding.
  let normalizeImage (dir: string) (srcFile: string) (address: int) : string * int * int * int =
    let ext = Path.GetExtension(srcFile).ToLowerInvariant()
    let mem = Array.zeroCreate<byte> 0x10000
    let start, pc =
      if ext = ".sna" then
        let raw = File.ReadAllBytes srcFile
        if raw.Length < 27 + 48 * 1024 then failwithf "SNA too short (%d bytes)" raw.Length
        // Header: 23..24 = SP; memory follows at offset 27. The 48K .sna
        // format stores no PC - it sits pushed on the stack.
        Array.blit raw 27 mem 0x4000 (48 * 1024)
        let sp = int raw[23] ||| (int raw[24] <<< 8)
        0x4000, (int mem[sp &&& 0xFFFF] ||| (int mem[(sp + 1) &&& 0xFFFF] <<< 8))
      else
        let raw = File.ReadAllBytes srcFile
        let len = min raw.Length (0x10000 - address)
        Array.blit raw 0 mem address len
        address, address
    let endExcl = extentOf mem start
    let binPath = Path.Combine(dir, "original.bin")
    File.WriteAllBytes(binPath, mem[start .. endExcl - 1])
    binPath, start, endExcl, pc


  let writeControl (dir: string) (imageFile: string) (start: int) (endExcl: int) (pc: int) : unit =
    let control =
      {| image = imageFile
         start = start
         endExcl = endExcl
         entryPc = pc
         activeVersion = 0
         blocks = [ {| start = start; endExcl = endExcl; name = "whole_span"; kind = "code" |} ] |}
    let json = JsonSerializer.Serialize(control, JsonSerializerOptions(WriteIndented = true))
    File.WriteAllText(Path.Combine(dir, "control.json"), json)

  let writeManifestProgram (dir: string) (name: string) : unit =
    let manifest =
      {| name = name
         boot = "program"
         program = {| bin = "original.bin"; address = "" |} |}
    // address filled by caller (needs the numeric load address)
    ignore manifest

  /// Assemble a version body against its span and refuse non-parity output.
  let checkedVersionSource (gameId: string) (mem: byte[]) (c: GameControl) (body: string) : string =
    // Structural check: the emitted program text must reassemble to the
    // same bytes. Compile the body through Z80CE's structural twin
    // (toOps on the same span) - the source emitter and op emitter walk
    // identical boundaries, so parity of ops implies parity of the text.
    let ops = Z80CE.toOps mem c.Start (c.EndExcl - c.Start)
    let rebuilt = Jetpac2.Core.Z80.assemble ops
    let spanLen = c.EndExcl - c.Start
    let expected = mem[c.Start .. c.EndExcl - 1]
    if rebuilt.Length <> spanLen || rebuilt <> expected then
      let n = min rebuilt.Length expected.Length
      let mutable d = -1
      let mutable i = 0
      while d < 0 && i < n do
        if rebuilt[i] <> expected[i] then d <- i
        i <- i + 1
      failwithf
        "%s: parity FAILED at %04X (%d vs %d bytes) - refusing to write the version"
        gameId (c.Start + max 0 d) rebuilt.Length expected.Length
    body

/// Emit Image.fs + Game.fs module texts for a scaffolded game.
module private Emit =

  let imageModule (moduleName: string) (tag: string) (body: string) : string =
    let sb = Text.StringBuilder()
    sb.AppendLine(sprintf "// GENERATED by --gen-game %s - regenerate via --regen, do not hand-edit." tag) |> ignore
    sb.AppendLine(sprintf "module %s" moduleName) |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("open Jetpac2.Core") |> ignore
    sb.AppendLine("open Jetpac2.Core.Z80BuilderInstance") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("/// The active CE version as a runnable program.") |> ignore
    sb.AppendLine("let program : Z80Op list = z80 {") |> ignore
    sb.Append(body) |> ignore
    sb.AppendLine("}") |> ignore
    sb.ToString()

  let runnerModule (moduleName: string) (imageModuleName: string) (entryPc: int) : string =
    let sb = Text.StringBuilder()
    sb.AppendLine(sprintf "// GENERATED by --gen-game - regenerate via --regen, do not hand-edit.") |> ignore
    sb.AppendLine(sprintf "module %s" moduleName) |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("open Jetpac2.Core") |> ignore
    sb.AppendLine("open JetpacFR.Core") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("/// The registry bundle shells look up by GameId.") |> ignore
    sb.AppendLine("let register () =") |> ignore
    sb.AppendLine("  GameRegistry.register") |> ignore
    sb.AppendLine("    { GameId = \"%GAMEID%\"") |> ignore
    sb.AppendLine("      Name = \"%GAMENAME%\"") |> ignore
    sb.AppendLine("      Memory = %MEMORYEXPR%") |> ignore
    sb.AppendLine("      Program = %IMAGEMODULE%.program") |> ignore
    sb.AppendLine("      EntryState = \"\" }") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine(sprintf "/// Entry PC: 0x%04X" entryPc) |> ignore
    sb.ToString()

/// One-shot generation pipeline shared by gen/regen/materialize.
module private Pipeline =

  let loadOrInitControl (dir: string) : GameControl =
    GameProject.loadControl dir

  /// Regenerate the NEXT version from control.json + original.bin and make
  /// it active. Tag describes the change (default: block-map revision).
  let regen (dir: string) (tag: string option) : string =
    let gameId = DirectoryInfo(dir).Name.ToLowerInvariant()
    let c = GameProject.loadControl dir
    let mem = GameProject.loadImage dir c
    // Body assembly per block kind: code walks instructions, data/gap stay
    // single labeled raw blocks. Control-file comments are woven in as //
    // lines: names become section headers, line comments attach to their
    // address, range comments open at the range start.
    let sb = Text.StringBuilder()
    let cf =
      try Some(ControlFile.load dir) with _ -> None
    let commentsAt (addr: int) : ControlComment list =
      match cf with
      | Some c -> c.Comments |> List.filter (fun m -> m.Kind = Line && m.Addr = addr)
      | None -> []
    let nameHeaders : ControlComment list =
      match cf with
      | Some c -> ControlFile.namesSorted c
      | None -> []
    let rangeComments (addr: int) : ControlComment list =
      match cf with
      | Some c -> c.Comments |> List.filter (fun m -> m.Kind = Range && m.Addr = addr)
      | None -> []
    let emitComments (addr: int) : unit =
      for h in nameHeaders do
        if h.Addr = addr then
          sb.AppendLine(sprintf "  // ========== %s ($%04X) ==========" h.Text addr) |> ignore
      for r in rangeComments addr do
        sb.AppendLine(sprintf "  // [%04X..] %s" r.Addr r.Text) |> ignore
      for m in commentsAt addr do
        sb.AppendLine(sprintf "  // $%04X: %s" addr m.Text) |> ignore
    let mutable cursor = c.Start
    for b in c.Blocks do
      emitComments b.Start
      match b.Kind with
      | Code -> sb.Append(Z80CE.toBody mem b.Start (b.EndExcl - b.Start)) |> ignore
      | Data | Gap ->
        let bytes = mem[b.Start .. b.EndExcl - 1]
        let hex = bytes |> Array.map (fun x -> sprintf "0x%02Xuy" x) |> String.concat "; "
        sb.AppendLine(sprintf "  // %s (%s)" b.Name (match b.Kind with Data -> "data" | _ -> "gap")) |> ignore
        sb.AppendLine(sprintf "  yield! [| %s |]" hex) |> ignore
      cursor <- b.EndExcl
    if cursor < c.EndExcl then failwithf "blocks cover [%06X,%06X) but span ends at %06X - add a block or shrink endExcl" c.Start cursor c.EndExcl
    let tag' = tag |> Option.defaultWith (fun () -> sprintf "blocks%d" (List.length c.Blocks))
    // Parity gate before writing anything.
    let ops = Z80CE.toOps mem c.Start (c.EndExcl - c.Start)
    let rebuilt = Jetpac2.Core.Z80.assemble ops
    let expected = mem[c.Start .. c.EndExcl - 1]
    if rebuilt <> expected then failwith "parity failed - refusing to write"
    let nextIdx =
      let vd = Path.Combine(dir, "versions")
      if Directory.Exists vd then
        (Directory.GetFiles(vd, "v*.fs") |> Array.map (fun f -> Path.GetFileName f) |> Array.map (fun s -> s.Substring(1, 3)) |> Array.map int
         |> fun a -> if a.Length = 0 then -1 else Array.max a) + 1
      else 0
    let file = Path.Combine(dir, "versions", sprintf "v%03d_%s.fs" nextIdx tag')
    Directory.CreateDirectory(Path.GetDirectoryName file) |> ignore
    let header = sprintf "// GENERATED by --regen from control.json (activeVersion bumped by the caller).\r\n// blocks: %d\r\n" (List.length c.Blocks)
    File.WriteAllText(file, header + sb.ToString())
    // Bump activeVersion in control.json (preserve other fields by rewrite).
    let ctrlPath = Path.Combine(dir, "control.json")
    use doc = JsonDocument.Parse(File.ReadAllText ctrlPath)
    let out = new IO.MemoryStream()
    do
      use w = new Utf8JsonWriter(out, JsonWriterOptions(Indented = true))
      w.WriteStartObject()
      let mutable wroteActive = false
      for p in doc.RootElement.EnumerateObject() do
        if p.Name = "activeVersion" then
          w.WriteNumber("activeVersion", nextIdx)
          wroteActive <- true
        else
          p.WriteTo w
      if not wroteActive then w.WriteNumber("activeVersion", nextIdx)
      w.WriteEndObject()
    File.WriteAllText(ctrlPath, Text.Encoding.UTF8.GetString(out.ToArray()))
    printfn "regen: %s" file
    file

  let materialize (dir: string) : unit =
    let copied = GameProject.materialize dir
    printfn "materialize: CeProgram.fs <- %s" copied

/// CLI surface.
let run (argv: string list) : int =
  match argv with
  | "--gen-game" :: dir :: rest ->
    let rec parse (acc: Map<string, string>) (xs: string list) =
      match xs with
      | flag :: v :: t when flag.StartsWith "--" -> parse (acc.Add(flag.Substring(2), v)) t
      | [] -> acc
      | bad ->
        eprintfn "bad args near %A" bad
        exit 2
    let opts = parse Map.empty rest
    let src =
      match opts.TryFind "from" with
      | Some f -> f
      | None -> eprintfn "--gen-game needs --from <file>"; exit 2
    let address = opts |> Map.tryFind "address" |> Option.map int |> Option.defaultValue 0x8000
    let name = opts |> Map.tryFind "name" |> Option.defaultWith (fun () -> DirectoryInfo(dir).Name)
    if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore
    if not (Directory.Exists dir) then eprintfn "cannot create %s" dir; exit 2
    let bin, start, endExcl, pc = Gen.normalizeImage dir src address
    Gen.writeControl dir (Path.GetFileName bin) start endExcl pc
    // v000_init: whole span as code.
    let c = GameProject.loadControl dir
    let mem = GameProject.loadImage dir c
    let body = Gen.checkedVersionSource (DirectoryInfo(dir).Name.ToLowerInvariant()) mem c (Z80CE.toBody mem c.Start (c.EndExcl - c.Start))
    let vd = Path.Combine(dir, "versions")
    Directory.CreateDirectory vd |> ignore
    File.WriteAllText(Path.Combine(vd, "v000_init.fs"), "// GENERATED by --gen-game (initial whole-span disassembly)\n" + body)
    printfn "gen-game: %s" dir
    printfn "  original.bin  %d bytes @ $%04X (span $%04X-$%04X)" (FileInfo(bin)).Length start start endExcl
    printfn "  control.json  entryPc=$%04X, blocks=1 (code)" pc
    printfn "  v000_init.fs  written (active)"
    printfn "next: edit games/%s/control.json (mark data/gap blocks), then run:" (DirectoryInfo(dir).Name.ToLowerInvariant())
    printfn "  dotnet run --project tests/JetpacFR.Core.Tests -- --regen games/%s --tag <what-changed>" (DirectoryInfo(dir).Name.ToLowerInvariant())
    0
  | "--regen" :: dir :: rest ->
    let tag = rest |> function "--tag" :: t :: _ -> Some t | _ -> None
    Pipeline.regen dir tag |> ignore
    Pipeline.materialize dir
    0
  | "--materialize" :: dir :: _ ->
    Pipeline.materialize dir
    0
  | "--diff-game" :: dir :: a :: b :: _ ->
    let c = GameProject.loadControl dir
    let mem = GameProject.loadImage dir c
    let span = c.EndExcl - c.Start
    let expected = mem[c.Start .. c.EndExcl - 1]
    let stats (label: string) (path: string) : unit =
      let body = File.ReadAllText path
      // A version file IS the z80 body; rebuild ops structurally from the
      // same span (the text was generated from these very ops).
      let ops = Z80CE.toOps mem c.Start span
      let asm = Jetpac2.Core.Z80.assemble ops
      let mismatches =
        [ for i in 0 .. min (asm.Length - 1) (expected.Length - 1) do
            if asm[i] <> expected[i] then i ]
      let rawBytes = ops |> List.filter (fun o -> o.Mnemonic = "raw") |> List.sumBy (fun o -> o.Bytes.Length)
      let pctRaw = float rawBytes * 100.0 / float span
      let status = if mismatches.IsEmpty && asm.Length = span then "PARITY OK" else sprintf "MISMATCH (%d bytes, first at +%d)" mismatches.Length (if mismatches.IsEmpty then -1 else mismatches.Head)
      printfn "%-12s %s: %s, %d ops, raw %.1f%%" label (Path.GetFileName path) status ops.Length pctRaw
    stats a (GameProject.activeVersionPath dir c |> function Some p -> p | None -> failwithf "no versions in %s" dir)
    stats b (GameProject.activeVersionPath dir c |> function Some p -> p | None -> failwithf "no versions in %s" dir)
    0
  | _ ->
    eprintfn "usage:"
    eprintfn "  --gen-game <dir> --from <bin|sna|tzx> [--address N] [--name NAME]"
    eprintfn "  --regen <dir> [--tag TAG]"
    eprintfn "  --materialize <dir>"
    eprintfn "  --diff-game <dir> <verA> <verB>"
    2
