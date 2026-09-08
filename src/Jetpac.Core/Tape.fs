namespace Jetpac.Core

/// TZX tape support: parsing (from the verified TzxScreen parser) plus the
/// pulse playback state machine from specbolt's Tape.cpp. The tape is TZX-only
/// (the C++ reference is TAP-only); each block carries its own timings.
module Tape =

    let PilotCycles = 2168
    let Sync1Cycles = 667
    let Sync2Cycles = 735
    let Data0Cycles = 855
    let Data1Cycles = 1710
    let PilotDataEdges = 3223
    let PilotHeaderEdges = 8063

    /// One playable frame: flag byte + payload + checksum, exactly as recorded.
    type FrameBlock =
        { Data: byte[]
          PauseCycles: int
          Bit0Cycles: int
          Bit1Cycles: int
          PilotEdges: int }

    type TzxBlock =
        | StandardData of pauseMs: int * payload: byte[]
        | TurboData of pauseMs: int * pilotMs: int * bit0: int * bit1: int * payload: byte[]
        | Skipped of id: byte

    let u16 (b: byte[]) (i: int) = int b[i] ||| (int b[i + 1] <<< 8)

    /// "ZXTape!" as raw bytes (Fable lacks Encoding.ASCII).
    let private zxTapeSig = [| 0x5Auy; 0x58uy; 0x54uy; 0x61uy; 0x70uy; 0x65uy; 0x21uy |]

    /// Parse a whole TZX file into a block list (ported from TzxScreen/Program.fs).
    let parseTzx (data: byte[]) : TzxBlock list =
        if data.Length < 10 || not (Array.forall2 (=) zxTapeSig data[0..6]) then
            failwith "Not a TZX file (missing 'ZXTape!' signature)."

        let mutable pos = 7

        if data[pos] <> 0x1Auy then
            failwith "Malformed TZX header (missing 0x1A)."

        pos <- pos + 1

        if data[pos] = 0x1Auy then
            pos <- pos + 1 // tolerate single or double 0x1A terminator

        pos <- pos + 2 // version

        let blocks = System.Collections.Generic.List<TzxBlock>()

        while pos < data.Length do
            let id = data[pos]
            pos <- pos + 1

            match id with
            | 0x10uy -> // standard speed: pause(2) len(2) data
                let len = u16 data (pos + 2)
                blocks.Add(StandardData(u16 data pos, data[pos + 4 .. pos + 4 + len - 1]))
                pos <- pos + 4 + len
            | 0x11uy -> // turbo: pause(2) pilot(2) sync1(2) sync2(2) bit0(2) bit1(2) pause(2) usedBits(3) len(2) data
                let len = u16 data (pos + 17)

                blocks.Add(
                    TurboData(
                        u16 data pos,
                        u16 data (pos + 2),
                        u16 data (pos + 8),
                        u16 data (pos + 10),
                        data[pos + 19 .. pos + 19 + len - 1]
                    )
                )

                pos <- pos + 19 + len
            | 0x12uy ->
                pos <- pos + 4
                blocks.Add(Skipped id)
            | 0x13uy ->
                let n = int data[pos]
                pos <- pos + 1 + n * 2
                blocks.Add(Skipped id)
            | 0x14uy ->
                let len = u16 data (pos + 15)
                pos <- pos + 17 + len
                blocks.Add(Skipped id)
            | 0x15uy ->
                let len =
                    int data[pos + 6] ||| (int data[pos + 7] <<< 8) ||| (int data[pos + 8] <<< 16)

                pos <- pos + 9 + len
                blocks.Add(Skipped id)
            | 0x16uy ->
                pos <- pos + 3
                blocks.Add(Skipped id)
            | 0x17uy ->
                let len = u16 data (pos + 13)
                pos <- pos + 15 + len
                blocks.Add(Skipped id)
            | 0x18uy ->
                let len = u16 data (pos + 2)
                pos <- pos + 16 + len
                blocks.Add(Skipped id)
            | 0x19uy -> // generalized data: symbol-coded; skip whole
                let pilotLen = u16 data pos
                let d2 = pos + 2 + pilotLen
                let dataLen = u16 data d2
                let symCount = u16 data (d2 + 2)
                let mutable p = d2 + 4

                for _ in 1..symCount do
                    let pulseLen = u16 data (p + 1)
                    p <- p + 3 + pulseLen

                let maxSym = u16 data p
                let dataSym = u16 data (p + 2)
                p <- p + 4

                for _ in 1..dataSym do
                    p <- p + 2 + maxSym

                p <- p + dataLen
                pos <- p
                blocks.Add(Skipped id)
            | 0x20uy ->
                pos <- pos + 2
                blocks.Add(Skipped id)
            | 0x21uy
            | 0x28uy
            | 0x30uy ->
                let n = int data[pos]
                pos <- pos + 1 + n
                blocks.Add(Skipped id)
            | 0x22uy
            | 0x25uy -> blocks.Add(Skipped id)
            | 0x23uy
            | 0x24uy ->
                pos <- pos + 2
                blocks.Add(Skipped id)
            | 0x26uy ->
                pos <- pos + 4
                blocks.Add(Skipped id)
            | 0x27uy ->
                pos <- pos + 1
                blocks.Add(Skipped id)
            | 0x31uy ->
                let n = int data[pos + 1]
                pos <- pos + 2 + n
                blocks.Add(Skipped id)
            | 0x32uy ->
                let count = u16 data pos
                let mutable p = pos + 2

                for _ in 1..count do
                    let n = u16 data (p + 2)
                    p <- p + 4 + n

                pos <- p
                blocks.Add(Skipped id)
            | 0x33uy ->
                let n = int data[pos]
                pos <- pos + 1 + n * 3
                blocks.Add(Skipped id)
            | 0x34uy ->
                let descLen = u16 data pos
                let n = int data[pos + 2 + descLen]
                pos <- pos + 3 + descLen + n
                blocks.Add(Skipped id)
            | 0x35uy ->
                pos <- pos + 4
                blocks.Add(Skipped id)
            | other -> failwithf "Unknown TZX block 0x%02X at offset %d" other pos

        List.ofSeq blocks

    /// Payload of a tape frame with the flag byte and (when present) checksum
    /// removed (ported from TzxScreen/Program.fs).
    let stripFrame (frame: byte[]) : byte[] =
        let n = frame.Length

        if n >= 2 && (frame[0] = 0xFFuy || frame[0] = 0x00uy) then
            if n >= 3 then
                let mutable x = 0uy

                for i in 0 .. n - 2 do
                    x <- x ^^^ frame[i]

                if x = frame[n - 1] then
                    frame[1 .. n - 2] // flag + payload + checksum
                else
                    frame[1..] // flag + payload only
            else
                frame[1..]
        else
            frame

    /// Decode a 17-byte tape header frame. Returns (type, name, length, param1, param2).
    let headerInfo (frame: byte[]) : (string * string * int * int * int) option =
        let p = stripFrame frame

        if p.Length = 17 && frame[0] = 0x00uy then
            let t =
                match p[0] with
                | 0x00uy -> "PROGRAM"
                | 0x01uy -> "NUM ARRAY"
                | 0x02uy -> "CHAR ARRAY"
                | 0x03uy -> "CODE"
                | other -> sprintf "TYPE %d" other

            Some(t, System.Text.Encoding.UTF8.GetString(p, 1, 10).TrimEnd(), u16 p 11, u16 p 13, u16 p 15)
        else
            None

    /// Convert a TZX data block (0x10/0x11) into playable frames. The payload
    /// is the raw tape frame (flag + payload + checksum); 0x10 blocks use the
    /// standard timings, 0x11 blocks carry their own per-block timings.
    let framesFor (block: TzxBlock) : FrameBlock list =
        match block with
        | StandardData(pauseMs, frame) ->
            [ { Data = frame
                PauseCycles = pauseMs * 3500
                Bit0Cycles = Data0Cycles
                Bit1Cycles = Data1Cycles
                PilotEdges =
                  if frame[0] &&& 0x80uy <> 0uy then
                      PilotDataEdges
                  else
                      PilotHeaderEdges } ]
        | TurboData(pauseMs, pilotMs, bit0, bit1, frame) ->
            // Pilot edge count approximated from the pilot tone duration.
            let pilotEdges = max 1 ((pilotMs * 3500) / PilotCycles)

            [ { Data = frame
                PauseCycles = pauseMs * 3500
                Bit0Cycles = bit0
                Bit1Cycles = bit1
                PilotEdges = pilotEdges } ]
        | Skipped _ -> []

    type State =
        | Idle
        | Pilot
        | Sync1
        | Sync2
        | Data1
        | Data2
        | Pause

/// Pulse playback state machine (Tape.cpp). InsertTzx parses the TZX; play()
/// starts from the current block.
type Tape() =
    let mutable numEdges = 0
    let mutable nextTransition = 0
    let mutable bitCycles = 0
    let mutable level = false
    let mutable state = Tape.State.Idle
    let mutable currentBlockIndex = 0
    let mutable bitOffset = 0
    let mutable blocks: Tape.FrameBlock list = []

    member this.InsertTzx(bytes: byte[]) =
        let parsed = Tape.parseTzx bytes
        let frames = parsed |> List.collect Tape.framesFor
        blocks <- frames
        currentBlockIndex <- 0
        bitOffset <- 0
        state <- Tape.State.Idle
        nextTransition <- 0
        level <- false

    member private this.CurrentBlock() : Tape.FrameBlock option =
        if currentBlockIndex < blocks.Length then
            Some blocks[currentBlockIndex]
        else
            None

    member this.NextTransition() = nextTransition
    member this.Level() = level
    member this.Playing() = state <> Tape.State.Idle

    /// Restore the ear level after a checkpoint load (used by Spectrum48.LoadState).
    member this.SetLevel(v: bool) = level <- v

    member this.PassTime(cycles: int) =
        nextTransition <- nextTransition - min cycles nextTransition

        if nextTransition = 0 then
            this.Next()

    member this.Play() =
        match this.CurrentBlock(), state with
        | Some block, Tape.State.Idle ->
            state <- Tape.State.Pilot
            nextTransition <- 1
            numEdges <- block.PilotEdges
        | _ -> ()

    member this.Stop() =
        state <- Tape.State.Idle
        nextTransition <- 0

    member private this.Next() =
        level <- not level

        match state with
        | Tape.State.Pilot ->
            nextTransition <- Tape.PilotCycles
            numEdges <- numEdges - 1

            if numEdges = 0 then
                state <- Tape.State.Sync1
        | Tape.State.Sync1 ->
            nextTransition <- Tape.Sync1Cycles
            state <- Tape.State.Sync2
        | Tape.State.Sync2 ->
            nextTransition <- Tape.Sync2Cycles
            state <- this.NextBit()
        | Tape.State.Data1 ->
            nextTransition <- bitCycles
            state <- Tape.State.Data2
        | Tape.State.Data2 ->
            nextTransition <- bitCycles
            state <- this.NextBit()
        | Tape.State.Pause ->
            // NOTE: the toggle at Next() entry is load-bearing here - it supplies
            // the block's final half-pulse edge the ROM loader's last LD-EDGE
            // waits for. A "pause = silence" refinement (no toggle) was tried and
            // broke tape loading entirely.
            nextTransition <- bitCycles

            match this.CurrentBlock() with
            | Some block ->
                numEdges <- block.PilotEdges
                state <- Tape.State.Pilot
            | None ->
                // End of tape: stop scheduling. The old code stayed in Pause with a
                // nonzero nextTransition, toggling forever and keeping Playing().
                state <- Tape.State.Idle
                nextTransition <- 0
        | Tape.State.Idle -> nextTransition <- 0

    member private this.NextBit() : Tape.State =
        match this.CurrentBlock() with
        | None -> Tape.State.Idle
        | Some block ->
            let byteOffset = bitOffset / 8

            if byteOffset >= block.Data.Length then
                bitCycles <- block.PauseCycles
                bitOffset <- 0
                currentBlockIndex <- currentBlockIndex + 1
                Tape.State.Pause
            else
                bitCycles <-
                    if block.Data[byteOffset] &&& (1uy <<< (7 - bitOffset % 8)) <> 0uy then
                        block.Bit1Cycles
                    else
                        block.Bit0Cycles

                bitOffset <- bitOffset + 1
                Tape.State.Data1
