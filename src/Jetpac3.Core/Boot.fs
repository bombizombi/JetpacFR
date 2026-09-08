namespace Jetpac3.Core

open System
open System.IO

/// Boot the oracle emulator (JetpacFSharp Spectrum48) to the game entry.
/// Generalized: the expected memory layout is derived from the tape's own
/// CODE headers, so any standard-speed multi-block tape boots without
/// per-game heuristics. Kept in the core so the GUI and tests share one
/// boot path.
module Boot =

    /// Expected memory spans derived from the tape's CODE blocks:
    /// (start address, expected bytes) pairs in tape order. Each CODE
    /// header's param1 is the load address; the following data frame's
    /// payload carries the bytes.
    let findCodeSpans (tzxBytes: byte[]) : (int * byte[]) list =
        let blocks = Jetpac.Core.Tape.parseTzx tzxBytes

        let frameOf (b: Jetpac.Core.Tape.TzxBlock) : byte[] =
            match b with
            | Jetpac.Core.Tape.StandardData(_, p) -> p
            | Jetpac.Core.Tape.TurboData(_, _, _, _, p) -> p
            | Jetpac.Core.Tape.Skipped _ -> [||]

        let spans = ResizeArray<int * byte[]>()
        let mutable pending: (int * int) option = None

        for block in blocks do
            let payload = frameOf block

            if payload.Length > 0 then
                match Jetpac.Core.Tape.headerInfo payload with
                | Some("CODE", _, len, start, _) when len > 0 -> pending <- Some(start, len)
                | Some _ -> pending <- None
                | None ->
                    match pending with
                    | Some(start, len) ->
                        let data = Jetpac.Core.Tape.stripFrame payload
                        spans.Add(start, data[0 .. len - 1])
                        pending <- None
                    | None -> ()

        List.ofSeq spans

    /// Boot to the first instruction the game's own code executes after the
    /// tape finishes loading: run frames until every CODE block has landed
    /// at its header start address, then single-step until PC leaves the
    /// ROM (the load routine and BASIC all run in ROM; the first RAM
    /// instruction is the USR jump target). Returns (spec, entryCycles).
    let bootToEntry
        (romPath: string)
        (tzxPath: string)
        (onFrame: (byte[] * int -> unit) option)
        : Jetpac.Core.Spectrum48 * int64 =
        let rom = File.ReadAllBytes romPath
        let tzx = File.ReadAllBytes tzxPath
        let spans = findCodeSpans tzx

        if spans.IsEmpty then
            failwith "TZX contains no CODE blocks - cannot auto-boot"

        let spec = Jetpac.Core.Spectrum48()
        spec.LoadRom rom
        spec.InsertTape tzx

        let spanLoaded (start: int) (expected: byte[]) : bool =
            let mutable i = 0
            let mutable ok = true

            while ok && i < expected.Length do
                if spec.Memory[start + i] <> expected[i] then
                    ok <- false

                i <- i + 1

            ok

        // Each span counts once it has fully landed: later overwrites (the
        // ROM loader prints header names onto the loading screen, BASIC
        // POKEs loader variables) must not invalidate an already-loaded
        // block. The gate is "every CODE block arrived at its address".
        let matched = Array.create spans.Length false
        let mutable pendingCount = spans.Length
        let mutable frames = 0

        while frames < 20000 && pendingCount > 0 do
            spec.RunFrame()
            frames <- frames + 1

            match onFrame with
            | Some f -> f (spec.ScreenBuffer, spec.FrameCount)
            | None -> ()

            for j in 0 .. spans.Length - 1 do
                if not matched[j] then
                    let s, e = spans[j]

                    if spanLoaded s e then
                        matched[j] <- true
                        pendingCount <- pendingCount - 1

        if pendingCount > 0 then
            failwithf "Tape CODE blocks never matched after %d frames" frames

        let z80 = spec.DebugZ80
        let mutable g = 0

        while z80.Regs.Pc() < 0x4000 && g < 300000 do
            z80.ExecuteOne()
            g <- g + 1

        if z80.Regs.Pc() < 0x4000 then
            failwithf "Never reached a RAM instruction after loading (pc=%04X)" (z80.Regs.Pc())

        spec, int64 (z80.CycleCount())
