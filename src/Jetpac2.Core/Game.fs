namespace Jetpac2.Core

/// The Jetpac2 product API: key presses in, the 64K ZX Spectrum memory block
/// out. `RunFrame` advances one game frame (until the next 50Hz interrupt,
/// 69888 T-states), which is the unit the shells render and the oracle test
/// compares.
type Jetpac() =
    let machine = Machine()
    let mutable frameCount = 0
    let mutable fixtureBytes: byte[] = null
    let mutable fixtureRegsText = ""

    do
        Z80Table.EnsureInstalled()
        machine.Override <- Semantic.hook

    member this.DebugMachine = machine

    /// Load the 64K memory image and register state captured at the game
    /// entry point (PC=0x61E5). `regsText` is the key=value state file text.
    member this.LoadState(bytes: byte[], regsText: string) =
        fixtureBytes <- bytes
        fixtureRegsText <- regsText
        machine.LoadState(bytes, regsText)
        frameCount <- 0

    /// Load the embedded fixture (web/desktop entry).
    member this.LoadEmbedded() =
        let bytes = System.Convert.FromBase64String(EmbeddedFixture.FixtureB64)

        let regsText =
            System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(EmbeddedFixture.RegsTextB64))

        this.LoadState(bytes, regsText)

    member this.Memory: byte[] = machine.Memory
    member this.ScreenBuffer: byte[] = machine.Video.BlitTo()
    member this.FrameCount = frameCount
    member this.Border = machine.Border
    member this.Beeper = machine.BeeperLevel

    /// Full beeper transition trace so far (absolute cycle, level).
    member this.BeeperTrace: (int64 * bool) list = machine.BeeperTrace |> Seq.toList

    /// Consume the beeper transitions since the last call (per-frame slices
    /// for the shells).
    member this.DrainBeeperTrace() : (int64 * bool) list =
        let items = machine.BeeperTrace |> Seq.toList
        machine.BeeperTrace.Clear()
        items

    member this.SetKey(row: int, bit: int, pressed: bool) = machine.SetKey(row, bit, pressed)

    /// Advance one game frame: run until the next scanline-wrap interrupt
    /// (69888 T-states), then advance the frame clock.
    member this.RunFrame() =
        let endCycle = machine.FrameEnd

        while machine.CycleCount() < endCycle do
            machine.Step()

        machine.FrameEnd <- machine.FrameEnd + 69888L
        frameCount <- frameCount + 1

    member this.Reset() =
        if fixtureBytes <> null then
            machine.LoadState(fixtureBytes, fixtureRegsText)

        frameCount <- 0
