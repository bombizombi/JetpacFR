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

    /// How long the machine runs after the tape finishes, so the loader
    /// chain gives way to the game proper before the entry snapshot is
    /// taken. Stopping at the first RAM instruction parks the boot inside
    /// the game's own multiload tape loader on Uridium-style tapes.
    let settleFrames = 120

    /// Hard cap on tape-playback frames (~20 minutes of tape time). The
    /// exhaust gate relies on the tape state machine, but a broken tape
    /// must not hang the boot forever.
    let maxTapeFrames = 60000

    /// Boot to the first instruction the game's own code executes after the
    /// tape finishes loading: run frames until every CODE block has landed
    /// at its header start address (when the tape declares any), keep going
    /// until the tape is exhausted (custom EAR loaders read pulses long
    /// after the ROM load finished), then single-step until PC leaves the
    /// ROM and settle. Returns (spec, entryCycles).
    let bootToEntry
        (romPath: string)
        (tzxPath: string)
        (onFrame: (byte[] * int -> unit) option)
        : Jetpac.Core.Spectrum48 * int64 =
        let rom = File.ReadAllBytes romPath
        let tzx = File.ReadAllBytes tzxPath
        let spans = findCodeSpans tzx
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
        // Headerless/Program-header tapes (load-over-everything cracks)
        // declare no CODE spans and are gated by the tape exhaust below.
        if not (List.isEmpty spans) then
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

        // Play the tape out: the game's own loader (Uridium reads the EAR
        // bit by bit) keeps consuming pulses long after the ROM load, and
        // only real end-of-tape means the whole game is in memory. The
        // playback counts as boot-driven: a loader polling once per frame
        // must not trip the heuristic's auto-stop.
        spec.TapeSetManual()

        let mutable tapeFrames = 0

        while tapeFrames < maxTapeFrames && not spec.DebugTapeAtEnd do
            spec.RunFrame()
            tapeFrames <- tapeFrames + 1

            match onFrame with
            | Some f -> f (spec.ScreenBuffer, spec.FrameCount)
            | None -> ()

        let z80 = spec.DebugZ80
        let mutable g = 0

        while z80.Regs.Pc() < 0x4000 && g < 300000 do
            z80.ExecuteOne()
            g <- g + 1

        if z80.Regs.Pc() < 0x4000 then
            failwithf "Never reached a RAM instruction after loading (pc=%04X)" (z80.Regs.Pc())

        // Settle: the loader chain hands over to the game (title paint,
        // attribute work) in the frames right after the tape ends.
        for _ in 1 .. settleFrames do
            spec.RunFrame()

            match onFrame with
            | Some f -> f (spec.ScreenBuffer, spec.FrameCount)
            | None -> ()

        spec, int64 (z80.CycleCount())
