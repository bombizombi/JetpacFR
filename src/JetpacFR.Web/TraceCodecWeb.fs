namespace JetpacFR.Core

/// WEB SHELL SHIM (JetpacFR.Web): byte-compatible reimplementation of the
/// JetpacFR.Core TraceCodec (.jpt binary trace format) for the browser.
/// The Core module uses System.IO + Marshal/fixed (not Fable-compilable); this
/// one builds/parses the identical byte stream so traces saved in the browser
/// load in the desktop app and vice versa.
///
/// Format (all little-endian), verified against the desktop writer:
///   magic "JPTR" (4)
///   version u16 = 1 (2)
///   startTick u32, endTick u32 (8)
///   entryCount u32, snapCount u32, writeCount u32, portCount u32, frameCount u32 (20)
///   entries  entryCount x 20  (Pc u16 @0, B0..B3 u8 @2..5, Target u16 @6,
///                              Tick u32 @8, Length/Cycles/FlagsBefore/FlagsAfter/Taken u8 @12..16)
///   snaps    snapCount x 32   (Tick u32 @0, Af..Pc u16 @4..26, I/R u8 @28/29)
///   writes   writeCount x 8   (Tick u32 @0, Address u16 @4, OldValue u8 @6, NewValue u8 @7)
///   ports    portCount x 8    (Tick u32 @0, Port u16 @4, Value u8 @6, Border u8 @7)
///   frameTicks frameCount x 4
///   perPc    0x10000 x 4
///   selfMod  0x10000 (omitted by pre-self-mod writers; reader tolerates EOF)
module TraceCodecWeb =

    open System

    let private magic = [| byte 'J'; byte 'P'; byte 'T'; byte 'R' |]

    let private w8 (b: byte[]) (o: int) (v: int) = b[o] <- byte (v &&& 0xFF)

    let private w16 (b: byte[]) (o: int) (v: int) =
        b[o] <- byte (v &&& 0xFF)
        b[o + 1] <- byte ((v >>> 8) &&& 0xFF)

    let private w32 (b: byte[]) (o: int) (v: int) =
        b[o] <- byte (v &&& 0xFF)
        b[o + 1] <- byte ((v >>> 8) &&& 0xFF)
        b[o + 2] <- byte ((v >>> 16) &&& 0xFF)
        b[o + 3] <- byte ((v >>> 24) &&& 0xFF)

    let private r8 (b: byte[]) (o: int) = int b[o]
    let private r16 (b: byte[]) (o: int) = int b[o] ||| (int b[o + 1] <<< 8)

    let private r32 (b: byte[]) (o: int) =
        int b[o]
        ||| (int b[o + 1] <<< 8)
        ||| (int b[o + 2] <<< 16)
        ||| (int b[o + 3] <<< 24)

    /// Serialize a trace to the .jpt byte stream (identical layout to Core's writer).
    let encode (trace: Trace) : byte[] =
        let total =
            4
            + 2
            + 8
            + 20
            + trace.Entries.Length * 20
            + trace.Snapshots.Length * 32
            + trace.Writes.Length * 8
            + trace.Ports.Length * 8
            + trace.FrameTicks.Length * 4
            + 0x10000 * 4
            + 0x10000

        let b = Array.zeroCreate<byte> total
        let mutable o = 0
        Array.Copy(magic, 0, b, 0, 4)
        o <- o + 4
        w16 b o 1
        o <- o + 2
        w32 b o (int trace.StartTick)
        o <- o + 4
        w32 b o (int trace.EndTick)
        o <- o + 4
        w32 b o trace.Entries.Length
        o <- o + 4
        w32 b o trace.Snapshots.Length
        o <- o + 4
        w32 b o trace.Writes.Length
        o <- o + 4
        w32 b o trace.Ports.Length
        o <- o + 4
        w32 b o trace.FrameTicks.Length
        o <- o + 4

        for e in trace.Entries do
            w16 b o (int e.Pc)
            w8 b (o + 2) (int e.B0)
            w8 b (o + 3) (int e.B1)
            w8 b (o + 4) (int e.B2)
            w8 b (o + 5) (int e.B3)
            w16 b (o + 6) (int e.Target)
            w32 b (o + 8) (int e.Tick)
            w8 b (o + 12) (int e.Length)
            w8 b (o + 13) (int e.Cycles)
            w8 b (o + 14) (int e.FlagsBefore)
            w8 b (o + 15) (int e.FlagsAfter)
            w8 b (o + 16) (int e.Taken)
            o <- o + 20

        for s in trace.Snapshots do
            w32 b o (int s.Tick)
            w16 b (o + 4) (int s.Af)
            w16 b (o + 6) (int s.Bc)
            w16 b (o + 8) (int s.De)
            w16 b (o + 10) (int s.Hl)
            w16 b (o + 12) (int s.Af2)
            w16 b (o + 14) (int s.Bc2)
            w16 b (o + 16) (int s.De2)
            w16 b (o + 18) (int s.Hl2)
            w16 b (o + 20) (int s.Ix)
            w16 b (o + 22) (int s.Iy)
            w16 b (o + 24) (int s.Sp)
            w16 b (o + 26) (int s.Pc)
            w8 b (o + 28) (int s.I)
            w8 b (o + 29) (int s.R)
            o <- o + 32

        for w in trace.Writes do
            w32 b o (int w.Tick)
            w16 b (o + 4) (int w.Address)
            w8 b (o + 6) (int w.OldValue)
            w8 b (o + 7) (int w.NewValue)
            o <- o + 8

        for p in trace.Ports do
            w32 b o (int p.Tick)
            w16 b (o + 4) (int p.Port)
            w8 b (o + 6) (int p.Value)
            w8 b (o + 7) (int p.Border)
            o <- o + 8

        for t in trace.FrameTicks do
            w32 b o (int t)
            o <- o + 4

        for i in 0..0xFFFF do
            w32 b o trace.PerPcCount[i]
            o <- o + 4

        for i in 0..0xFFFF do
            w8 b o (if trace.SelfModified[i] then 1 else 0)
            o <- o + 1

        if o <> total then
            failwithf "TraceCodecWeb: wrote %d bytes, expected %d" o total

        b

    /// Parse a .jpt byte stream into a Trace (tolerates the omitted self-mod array).
    let decode (b: byte[]) : Trace =
        let mutable o = 0

        if b.Length < 34 then
            failwithf "trace file too short (%d bytes)" b.Length

        for i in 0..3 do
            if b[i] <> magic[i] then
                failwith "not a Jetpac trace file (bad magic)"

        o <- o + 4
        let v = r16 b o
        o <- o + 2

        if v <> 1 then
            failwithf "unsupported trace version %d" v

        let startTick = uint32 (r32 b o)
        o <- o + 4
        let endTick = uint32 (r32 b o)
        o <- o + 4
        let entryCount = r32 b o
        o <- o + 4
        let snapCount = r32 b o
        o <- o + 4
        let writeCount = r32 b o
        o <- o + 4
        let portCount = r32 b o
        o <- o + 4
        let frameCount = r32 b o
        o <- o + 4

        let need =
            entryCount * 20
            + snapCount * 32
            + writeCount * 8
            + portCount * 8
            + frameCount * 4
            + 0x10000 * 4

        if o + need > b.Length then
            failwithf "trace file truncated: header claims %d more bytes but only %d remain" need (b.Length - o)

        let entries = Array.zeroCreate<TraceEntry> entryCount

        for i in 0 .. entryCount - 1 do
            entries[i] <-
                { Pc = uint16 (r16 b o)
                  B0 = byte (r8 b (o + 2))
                  B1 = byte (r8 b (o + 3))
                  B2 = byte (r8 b (o + 4))
                  B3 = byte (r8 b (o + 5))
                  Target = uint16 (r16 b (o + 6))
                  Tick = uint32 (r32 b (o + 8))
                  Length = byte (r8 b (o + 12))
                  Cycles = byte (r8 b (o + 13))
                  FlagsBefore = byte (r8 b (o + 14))
                  FlagsAfter = byte (r8 b (o + 15))
                  Taken = byte (r8 b (o + 16)) }

            o <- o + 20

        let snaps = Array.zeroCreate<RegSnapshot> snapCount

        for i in 0 .. snapCount - 1 do
            snaps[i] <-
                { Tick = uint32 (r32 b o)
                  Af = uint16 (r16 b (o + 4))
                  Bc = uint16 (r16 b (o + 6))
                  De = uint16 (r16 b (o + 8))
                  Hl = uint16 (r16 b (o + 10))
                  Af2 = uint16 (r16 b (o + 12))
                  Bc2 = uint16 (r16 b (o + 14))
                  De2 = uint16 (r16 b (o + 16))
                  Hl2 = uint16 (r16 b (o + 18))
                  Ix = uint16 (r16 b (o + 20))
                  Iy = uint16 (r16 b (o + 22))
                  Sp = uint16 (r16 b (o + 24))
                  Pc = uint16 (r16 b (o + 26))
                  I = byte (r8 b (o + 28))
                  R = byte (r8 b (o + 29)) }

            o <- o + 32

        let writes = Array.zeroCreate<MemWriteEvent> writeCount

        for i in 0 .. writeCount - 1 do
            writes[i] <-
                { Tick = uint32 (r32 b o)
                  Address = uint16 (r16 b (o + 4))
                  OldValue = byte (r8 b (o + 6))
                  NewValue = byte (r8 b (o + 7)) }

            o <- o + 8

        let ports = Array.zeroCreate<PortEvent> portCount

        for i in 0 .. portCount - 1 do
            ports[i] <-
                { Tick = uint32 (r32 b o)
                  Port = uint16 (r16 b (o + 4))
                  Value = byte (r8 b (o + 6))
                  Border = byte (r8 b (o + 7)) }

            o <- o + 8

        let frameTicks = Array.zeroCreate<uint32> frameCount

        for i in 0 .. frameCount - 1 do
            frameTicks[i] <- uint32 (r32 b o)
            o <- o + 4

        let perPc = Array.zeroCreate<int> 0x10000

        for i in 0..0xFFFF do
            perPc[i] <- r32 b o
            o <- o + 4
        // Traces saved before self-mod tracking omit the final array; treat those
        // as having no self-modification evidence.
        let selfMod =
            if o + 0x10000 <= b.Length then
                let a = Array.zeroCreate<bool> 0x10000

                for i in 0..0xFFFF do
                    a[i] <- b[o + i] <> 0uy

                a
            else
                Array.zeroCreate<bool> 0x10000

        let firstAt = Array.create 0x10000 -1

        for i in 0 .. entries.Length - 1 do
            let pc = int entries[i].Pc

            if firstAt[pc] < 0 then
                firstAt[pc] <- i

        { Entries = entries
          Snapshots = snaps
          Writes = writes
          Ports = ports
          FrameTicks = frameTicks
          PerPcCount = perPc
          SelfModified = selfMod
          SelfModCount = selfMod |> Array.filter id |> Array.length
          FirstIndexAtPc = firstAt
          StartTick = startTick
          EndTick = endTick }
