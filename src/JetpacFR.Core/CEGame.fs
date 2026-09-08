namespace JetpacFR.Core

open Jetpac2.Core

/// The CE execution engine (Phase 5): drives a port Machine with a CE
/// program's per-frame driver (`Z80.runFrame`). CE ops dispatch by PC from
/// the program index and run directly; raw blocks and anything outside the
/// CE layout fall through to the interpreter. Same Machine underneath, so
/// screen, audio, and keys behave exactly like the interpreter path — the
/// "both versions of the game" toggle.
type CEGame(program: Z80Op list, entryMem: byte[], entryState: string) =
    let port = Jetpac2.Core.Machine()
    let mutable frame = 0

    let index =
        Jetpac2.Core.Z80Table.EnsureInstalled()
        port.LoadState(entryMem, entryState)
        Jetpac2.Core.Z80.makeIndexAt (port.Regs.Pc()) program

    do index.Attach port

    member _.RunFrame() : int64 * int64 =
        let r = Z80.runFrame index port
        frame <- frame + 1
        r

    member _.AddCoHook(address: int, hook: Machine -> Effect list) = port.AddCoHook(address, hook)
    member _.RemoveCoHook(token: int) = port.RemoveCoHook token
    member _.DrainEffects() : Effect list = port.DrainEffects()

    member _.SetKey(row: int, bit: int, pressed: bool) = port.SetKey(row, bit, pressed)
    member _.ScreenBuffer = port.Video.BlitTo()
    member _.Regs = port.Regs
    member _.Memory = port.Memory
    member _.CycleCount = port.CycleCount()
    member _.Frame = frame

    member _.DrainBeeperSamples(frameStart: int64) : float32[] =
        let trace = port.BeeperTrace |> Seq.toList
        port.BeeperTrace.Clear()
        Jetpac2.Core.Beeper.ToSamples trace frameStart (port.CycleCount() - frameStart)


/// CE parity: the assembled CE program vs the original image it was
/// disassembled from — the visible "source of truth" claim.
module CEParity =

    /// (matching bytes, total compared, first diverging offsets; -1 marks a
    /// length difference).
    let check (program: Z80Op list) (image: byte[]) : int * int * int list =
        let assembled = Z80.assemble program
        let n = min assembled.Length image.Length
        let mutable matching = 0
        let mismatches = ResizeArray<int>()

        for i in 0 .. n - 1 do
            if assembled[i] = image[i] then
                matching <- matching + 1
            elif mismatches.Count < 8 then
                mismatches.Add i

        let total = max assembled.Length image.Length

        if assembled.Length <> image.Length && mismatches.Count < 8 then
            mismatches.Add -1

        matching, total, List.ofSeq mismatches
