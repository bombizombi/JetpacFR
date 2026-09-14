namespace Jetpac3.Core

/// Flash-load a tape game into the entry state without emulating the tape
/// pulses: a ROM-trap fast loader. The oracle boots normally until the 48K
/// ROM's tape routine (LD-BYTES, 0x0556) is entered by the LOAD command; there
/// the next TZX block's payload is written straight into memory and PC is
/// parked on the ROM's own success epilogue (0x05DF: LD A,H / CP +01 / RET),
/// so the ROM computes its exact exit flags and returns to BASIC exactly as
/// after a real load. The ROM's two LD-BYTES call sites consume only the carry
/// flag afterwards (the header load retries on NC, the data load RETs on C and
/// reports "R Tape loading error" otherwise), so the exit state is exact.
/// Everything else - header dispatch, BASIC chaining, sysvar bookkeeping,
/// screen artifacts - is performed by the real ROM running natively; the game
/// entry (first instruction at PC >= 0x4000) is reached the same way the slow
/// boot reaches it, just in seconds instead of minutes.
///
/// Anything unexpected (a verify request, exhausted or mismatched blocks, a
/// loader that never reaches LD-BYTES) abandons the fast attempt and delegates
/// to Boot.bootToEntry: flash loading can never be worse than the slow path.
/// volatile state (interrupt-timed sysvars such as FRAMES) differs from the
/// slow boot because the pulse timing is skipped.
module FastBoot =

    let private LD_BYTES = 0x0556

    /// LD A,H / CP +01 / RET - the ROM's own post-checksum success exit: carry
    /// set iff H=0. Reached with H=0, DE=0 and IX advanced past the block.
    let private LD_EPILOGUE = 0x05DF

    let private FRAME_CYCLES = 70000UL

    /// Tape data blocks in playback order: (flag byte, payload without the
    /// leading flag and trailing checksum). parseTzx yields the full frame per
    /// StandardData/TurboData block; pilot/tone/pause blocks carry no frame.
    let tapeBlocks (tzxBytes: byte[]) : (byte * byte[]) list =
        Jetpac.Core.Tape.parseTzx tzxBytes
        |> List.choose (fun block ->
            let frame =
                match block with
                | Jetpac.Core.Tape.StandardData(_, p) -> Some p
                | Jetpac.Core.Tape.TurboData(_, _, _, _, p) -> Some p
                | Jetpac.Core.Tape.Skipped _ -> None

            frame
            |> Option.filter (fun f -> f.Length >= 2)
            |> Option.map (fun f -> f[0], f[1 .. f.Length - 2]))

    /// Write one block payload at dest (at most `len` bytes), ROM-style:
    /// the destination wraps at 0x10000. Returns the bytes written.
    let blitBlock (memory: byte[]) (dest: int) (len: int) (data: byte[]) : int =
        let count = min len data.Length

        for i in 0 .. count - 1 do
            memory[(dest + i) &&& 0xFFFF] <- data[i]

        count

    /// Serve the ROM standing at LD-BYTES: consume the next block (its flag
    /// byte must match the requested one), blit the payload and park the CPU
    /// on the success epilogue. False when the trap cannot be served - the
    /// caller falls back to the slow boot.
    let private serveLoad (spec: Jetpac.Core.Spectrum48) (blocks: (byte * byte[]) array) (nextBlock: int ref) : bool =
        let regs = spec.DebugZ80.Regs
        let af = regs.Get Jetpac.Core.R16.AF
        let carry = af &&& 1 = 1
        let flag = (af >>> 8) &&& 0xFF

        if not carry || (flag <> 0x00 && flag <> 0xFF) || nextBlock.Value >= blocks.Length then
            false
        else
            let blockFlag, data = blocks[nextBlock.Value]

            if int blockFlag <> flag then
                false
            else
                nextBlock.Value <- nextBlock.Value + 1
                let ix = regs.Get Jetpac.Core.R16.IX
                let de = regs.Get Jetpac.Core.R16.DE
                let written = blitBlock spec.Memory ix de data
                // Advance IX past payload + checksum byte, empty DE, clear H
                // (zero checksum) - then the ROM's own epilogue does the rest.
                regs.Set(Jetpac.Core.R16.IX, (ix + written + 1) &&& 0xFFFF)
                regs.Set(Jetpac.Core.R16.DE, 0)
                regs.Set(Jetpac.Core.R16.HL, regs.Get Jetpac.Core.R16.HL &&& 0xFF)
                regs.SetPc LD_EPILOGUE
                true

    /// The frame-granular boot macro (typing LOAD ""), mirroring
    /// Spectrum48.RunBootMacro but driven through the public SetKey.
    let private macroKeys: (int * int * int * bool) list =
        [ 150, 6, 3, true // J = LOAD keyword
          158, 6, 3, false
          162, 7, 1, true // SYM SHIFT + P = quote
          162, 5, 0, true
          170, 7, 1, false
          170, 5, 0, false
          174, 7, 1, true // second quote
          174, 5, 0, true
          182, 7, 1, false
          182, 5, 0, false
          186, 6, 0, true // ENTER
          194, 6, 0, false ]

    /// One flash-load attempt: Some(spec, entry cycles) when the trap carried
    /// the boot through the tape and the machine settled into the game, None
    /// when the loader never trapped or asked for something the tape cannot
    /// serve.
    let private attempt
        (romPath: string)
        (tzxPath: string)
        (onFrame: (byte[] * int -> unit) option)
        : (Jetpac.Core.Spectrum48 * int64) option =
        let spec = Jetpac.Core.Spectrum48()
        spec.LoadRom(System.IO.File.ReadAllBytes romPath)
        spec.InsertTape(System.IO.File.ReadAllBytes tzxPath)
        let z80 = spec.DebugZ80
        let blocks = Array.ofList (tapeBlocks (System.IO.File.ReadAllBytes tzxPath))
        let nextBlock = ref 0
        let mutable failed = false
        let mutable inRam = false
        let mutable frame = 0

        // Phase A: serve the ROM loader's blocks until the first RAM
        // instruction (the game's BASIC-level entry).
        while frame < 20000 && not failed && not inRam do
            let frameEnd = z80.CycleCount() + FRAME_CYCLES

            for macroFrame, row, bit, down in macroKeys do
                if macroFrame = frame then
                    spec.SetKey(row, bit, down)

            while (z80.CycleCount() < frameEnd && not failed && not inRam) do
                if z80.Regs.Pc() = LD_BYTES then
                    failed <- not (serveLoad spec blocks nextBlock)
                elif z80.Regs.Pc() >= 0x4000 then
                    inRam <- true
                else
                    z80.ExecuteOne()

            frame <- frame + 1

            match onFrame with
            | Some f -> f (spec.ScreenBuffer, frame)
            | None -> ()

        // Phase B: play the tape out. Blocks the ROM never requested are
        // read by the game's own loader through the EAR pin - often a RAM
        // copy of LD-BYTES (Uridium-style load-over-everything) whose
        // once-per-frame "press play" polls never trigger the Fuse
        // auto-play heuristic, so playback starts here explicitly at the
        // first block the trap did not serve. The gate is real
        // end-of-tape; fully flash-served tapes skip this phase
        // immediately. Later ROM loads keep being served while blocks
        // remain - an exhausted tape must reach the real ROM routine.
        if inRam && not failed && nextBlock.Value < blocks.Length then
            spec.TapePlayFrom nextBlock.Value

        while
            not failed
            && frame < 60000
            && not (nextBlock.Value >= blocks.Length || spec.DebugTapeAtEnd) do
            frame <- frame + 1
            let frameEnd = z80.CycleCount() + FRAME_CYCLES

            while (z80.CycleCount() < frameEnd && not failed) do
                if z80.Regs.Pc() = LD_BYTES && nextBlock.Value < blocks.Length then
                    failed <- not (serveLoad spec blocks nextBlock)
                else
                    z80.ExecuteOne()

            match onFrame with
            | Some f -> f (spec.ScreenBuffer, frame)
            | None -> ()

        // Phase C: settle - the loader chain hands over to the game in the
        // frames right after the tape ends, and the entry snapshot must
        // show the game, not its loader.
        let mutable settled = 0

        while not failed && settled < Boot.settleFrames do
            settled <- settled + 1
            let frameEnd = z80.CycleCount() + FRAME_CYCLES

            while (z80.CycleCount() < frameEnd && not failed) do
                if z80.Regs.Pc() = LD_BYTES && nextBlock.Value < blocks.Length then
                    failed <- not (serveLoad spec blocks nextBlock)
                else
                    z80.ExecuteOne()

            frame <- frame + 1

            match onFrame with
            | Some f -> f (spec.ScreenBuffer, frame)
            | None -> ()

        if inRam && not failed then
            Some(spec, int64 (z80.CycleCount()))
        else
            None

    /// Boot to the game entry by flash loading; falls back to the slow
    /// emulated-tape boot whenever the fast attempt cannot complete. Same
    /// contract as Boot.bootToEntry.
    let bootToEntryFast
        (romPath: string)
        (tzxPath: string)
        (onFrame: (byte[] * int -> unit) option)
        : Jetpac.Core.Spectrum48 * int64 =
        match attempt romPath tzxPath onFrame with
        | Some result -> result
        | None -> Boot.bootToEntry romPath tzxPath onFrame
