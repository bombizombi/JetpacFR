namespace Jetpac3.Core

/// WEB SHELL SHIM (JetpacFR.Web): Jetpac3.Core.Boot for the browser.
/// The Core file reads ROM/TZX from disk; here assets are resolved through
/// an injectable key -> bytes provider (the shell wires it to the embedded
/// base64 assets). findCodeSpans/bootToEntry mirror the Core file.
open System

module Boot =

    /// Asset key -> raw bytes. The web shell sets this once at startup
    /// ("rom" and "tzx" resolve to the embedded base64 assets).
    let mutable AssetProvider: string -> byte[] =
        fun key -> failwithf "Boot.AssetProvider not set for %s" key

    /// Expected memory spans from the tape's CODE blocks - see Core Boot.fs.
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
    /// tape finishes loading - see Core Boot.fs. Keys are resolved through
    /// AssetProvider (browser: embedded assets). The onFrame option, when
    /// set, receives (screen buffer, frame count) after every tape frame.
    let bootToEntry
        (romKey: string)
        (tzxKey: string)
        (onFrame: (byte[] * int -> unit) option)
        : Jetpac.Core.Spectrum48 * int64 =
        let rom = AssetProvider romKey
        let tzx = AssetProvider tzxKey
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
