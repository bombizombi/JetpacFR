namespace JetpacFR.Desktop

open System
open System.IO
open JetpacFR.Core

/// The CE builder behind the emulator's CE tab: turns the game's control
/// file into a version body (the `--regen` twin). Two switches:
///   - Labels: control-file symbols become named label cells; off emits a
///     purely numeric body (no label cells at all).
///   - Disassembly: code blocks decode to vocab ops; off leaves every byte
///     raw - together with Labels off this is the null CE used to test the
///     CE runtime.
/// The parity gate is absolute: the assembled op stream must equal the image
/// span byte-for-byte, otherwise the build reports a failure and nothing
/// should be saved.
module CeBuild =

    type Options = { Labels: bool; Disassembly: bool }

    type BuildResult =
        { Body: string
          Lines: int
          OpCount: int
          RawBytes: int
          RawPct: float
          LabelCount: int
          ParityOk: bool
          ParityDiffAt: int
          ParityLengths: int * int }

    /// The 64K image the control file refers to: original bytes laid at the
    /// span start (the GameProject.loadImage shape without the record).
    let imageOf (dir: string) (c: ControlFile) : byte[] =
        let mem = Array.zeroCreate<byte> 0x10000
        let path = Path.Combine(dir, c.ImageFile)

        if File.Exists path then
            let raw = File.ReadAllBytes path
            let start = max 0 (min 0xFFFF c.Start)
            let len = min raw.Length (0x10000 - start)
            Array.blit raw 0 mem start len

        mem

    let build (mem: byte[]) (c: ControlFile) (opts: Options) : BuildResult =
        let spanStart = max 0 (min 0xFFFF c.Start)
        let spanEnd = max spanStart (min 0x10000 c.EndExcl)
        let sb = Text.StringBuilder()
        let ops = ResizeArray<Jetpac2.Core.Z80Op>()

        let nameHeaders = ControlFile.namesSorted c

        let emitComments (addr: int) =
            for h in nameHeaders do
                if h.Addr = addr then
                    sb.AppendLine(sprintf "  // ========== %s ($%04X) ==========" h.Text addr)
                    |> ignore

            for r in c.Comments do
                if r.Kind = Range && r.Addr = addr then
                    sb.AppendLine(sprintf "  // [%04X..] %s" r.Addr r.Text) |> ignore

            for m in c.Comments do
                if m.Kind = Line && m.Addr = addr then
                    sb.AppendLine(sprintf "  // $%04X: %s" addr m.Text) |> ignore

        let symbols = ControlFile.symbolsSorted c
        let mutable rawBytes = 0

        let rawYield (lo: int) (hi: int) (label: string) =
            let bytes = mem[lo .. hi - 1]
            sb.AppendLine(sprintf "  // %s" label) |> ignore

            sb.AppendLine(
                sprintf
                    "  yield! [| %s |]"
                    (bytes |> Array.map (fun x -> sprintf "0x%02Xuy" x) |> String.concat "; ")
            )
            |> ignore

            rawBytes <- rawBytes + bytes.Length
            ops.Add(Z80CE.rawOp bytes)

        // Blocks in address order; stretches of the span no block covers and
        // overlapping blocks (the GUI's map grows freely) are re-emitted as
        // raw bytes, so a partially mapped control file still builds a
        // parity-clean program.
        let sorted =
            c.Blocks
            |> List.sortBy (fun b -> b.Start)
            |> List.filter (fun b -> b.EndExcl > spanStart && b.Start < spanEnd)

        let mutable cur = spanStart

        for b in sorted do
            let e = min b.EndExcl spanEnd

            if e > cur then
                let s = max b.Start cur

                if s > cur then
                    rawYield cur s (sprintf "uncovered $%04X-$%04X (raw)" cur s)

                emitComments b.Start

                match b.Kind with
                | Code when opts.Disassembly ->
                    sb.Append(Z80CE.toBody mem s (e - s) opts.Labels symbols) |> ignore
                    ops.AddRange(Z80CE.toOps mem s (e - s))
                | _ ->
                    let kind =
                        match b.Kind with
                        | Data -> "data"
                        | Gap -> "gap"
                        | Code -> "code as raw bytes"

                    rawYield s e (sprintf "%s (%s)" b.Name kind)

                cur <- e

        if cur < spanEnd then
            rawYield cur spanEnd (sprintf "uncovered $%04X-$%04X (raw)" cur spanEnd)

        let text = sb.ToString()
        let opList = ops |> Seq.toList
        let rebuilt = Jetpac2.Core.Z80.assemble opList
        let expected = mem[spanStart .. spanEnd - 1]
        let mutable d = -1
        let mutable i = 0

        while d < 0 && i < min rebuilt.Length expected.Length do
            if rebuilt[i] <> expected[i] then d <- i

            i <- i + 1

        let parityOk = d < 0 && rebuilt.Length = expected.Length

        {
          Body = text
          Lines = text.Split('\n').Length
          OpCount = opList.Length
          RawBytes = rawBytes
          RawPct = float rawBytes * 100.0 / float (max 1 (spanEnd - spanStart))
          LabelCount = text.Split([| " = Z80.label ()" |], StringSplitOptions.None).Length - 1
          ParityOk = parityOk
          ParityDiffAt = (if parityOk then -1 else spanStart + max 0 d)
          ParityLengths = (rebuilt.Length, expected.Length) }

    /// File header + body for a saved version / CeProgram.fs.
    let fileText (opts: Options) (body: string) : string =
        sprintf
            "// GENERATED by the emulator CE tab (labels=%b, disassembly=%b) - regenerate there, do not hand-edit.\n"
            opts.Labels
            opts.Disassembly
        + body
