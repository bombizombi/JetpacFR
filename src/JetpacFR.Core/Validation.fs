namespace JetpacFR.Core

/// Lockstep differential validation of a lifted-registry port against the
/// oracle, with a fine-grained replay of the first divergent frame for the
/// theater view. The port override hook is injectable so tests can force a
/// divergence (and so the harness does not depend on the live registry).
module Validation =

  open System

  type Divergence =
    { Frame: int
      /// "instruction" (PC/regs/cycles diverged in the replay) or the
      /// frame-level kind (memory@NNNN / beeper trace / border / registers).
      Kind: string
      Address: int
      PortPc: int
      OraclePc: int
      PortExecuted: string
      OracleExecuted: string
      PortRegs: string
      OracleRegs: string }

  type Report =
    { Frames: int
      Passed: bool
      FirstDivergence: Divergence option }

  let private entryState (romPath: string) (tzxPath: string) : byte[] * string =
    match EntryCache.tryLoad romPath tzxPath with
    | Some s -> s
    | None ->
      let oracle, _ = Jetpac3.Core.Boot.bootToEntry romPath tzxPath
      let mem, state = oracle.SaveState()
      EntryCache.save romPath tzxPath mem state
      mem, state

  let private portRegs (r: Jetpac2.Core.RegisterFile) : string =
    sprintf "AF=%04X BC=%04X DE=%04X HL=%04X IX=%04X IY=%04X SP=%04X"
      (r.Get Jetpac2.Core.R16.AF) (r.Get Jetpac2.Core.R16.BC) (r.Get Jetpac2.Core.R16.DE)
      (r.Get Jetpac2.Core.R16.HL) (r.Ix()) (r.Iy()) (r.Sp())

  let private oracleRegs (r: Jetpac.Core.RegisterFile) : string =
    sprintf "AF=%04X BC=%04X DE=%04X HL=%04X IX=%04X IY=%04X SP=%04X"
      (r.Get Jetpac.Core.R16.AF) (r.Get Jetpac.Core.R16.BC) (r.Get Jetpac.Core.R16.DE)
      (r.Get Jetpac.Core.R16.HL) (r.Ix()) (r.Iy()) (r.Sp())

  /// Replay the divergent frame instruction-by-instruction to pin the exact
  /// first divergence and capture both machines' context for the theater.
  let private replayFrame
      (oracle: Jetpac.Core.Spectrum48)
      (port: Jetpac2.Core.Machine)
      (startMem: byte[])
      (startState: string)
      (frame: int)
      (script: (int * int * int * bool) list)
      (fallbackKind: string)
      (fallbackAddress: int) : Divergence =
    oracle.LoadState(startMem, startState)
    port.LoadState(startMem, startState)
    for (f, row, bit, pressed) in script do
      if f < frame then
        oracle.SetKey(row, bit, pressed)
        port.SetKey(row, bit, pressed)
    let z80 = oracle.DebugZ80
    let frameEnd = port.FrameEnd
    let mutable steps = 0
    let mutable found: Divergence option = None
    while found.IsNone && port.CycleCount() < frameEnd && steps < 100_000 do
      let pcBefore = port.Regs.Pc()
      let oPcBefore = z80.Regs.Pc()
      z80.ExecuteOne()
      port.Step()
      steps <- steps + 1
      if port.Regs.Pc() <> z80.Regs.Pc()
         || port.CycleCount() <> int64 (z80.CycleCount()) then
        found <-
          Some
            { Frame = frame
              Kind = "instruction"
              Address = pcBefore
              PortPc = pcBefore
              OraclePc = oPcBefore
              PortExecuted = Disasm.disasmMemory port.Memory pcBefore |> fun i -> i.Text
              OracleExecuted = Disasm.disasmMemory oracle.Memory oPcBefore |> fun i -> i.Text
              PortRegs = portRegs port.Regs
              OracleRegs = oracleRegs z80.Regs }
    match found with
    | Some d -> d
    | None ->
      // The frame diverged without PC/cycle divergence: a pure memory/border/
      // beeper difference (e.g. the lift wrote a wrong value and corrected it).
      { Frame = frame
        Kind = fallbackKind
        Address = fallbackAddress
        PortPc = port.Regs.Pc()
        OraclePc = z80.Regs.Pc()
        PortExecuted = ""
        OracleExecuted = ""
        PortRegs = portRegs port.Regs
        OracleRegs = oracleRegs z80.Regs }

  /// Run `framesN` lockstep frames: oracle reference vs port with
  /// `overrideHook` (normally LiftedRoutines.registryHook). Returns the first
  /// divergence, or Passed when every frame matches.
  let run
      (romPath: string)
      (tzxPath: string)
      (framesN: int)
      (script: (int * int * int * bool) list)
      (overrideHook: int -> (Jetpac2.Core.Machine -> unit) option)
      (cancel: System.Threading.CancellationToken) : Report =
    let mem, state = entryState romPath tzxPath
    let oracle = Jetpac.Core.Spectrum48()
    oracle.LoadState(mem, state)
    let port = Jetpac2.Core.Machine()
    Jetpac2.Core.Z80Table.EnsureInstalled()
    port.Override <- overrideHook
    port.LoadState(mem, state)
    let mutable frame = 0
    let mutable divergence: Divergence option = None
    let mutable frameStartMem = mem
    let mutable frameStartState = state
    while frame < framesN && divergence.IsNone && not cancel.IsCancellationRequested do
      let m, s = oracle.SaveState()
      frameStartMem <- m
      frameStartState <- s
      oracle.RunGameFrame()
      let portEnd = port.FrameEnd
      while port.CycleCount() < portEnd do
        port.Step()
      port.FrameEnd <- port.FrameEnd + 69888L
      for (f, row, bit, pressed) in script do
        if f = frame then
          oracle.SetKey(row, bit, pressed)
          port.SetKey(row, bit, pressed)
      let z80 = oracle.DebugZ80
      let mutable memDiff = -1
      let mutable i = 0x4000
      while memDiff < 0 && i < 0x10000 do
        if port.Memory[i] <> oracle.Memory[i] then memDiff <- i
        i <- i + 1
      let borderDiff = port.Border <> oracle.Border
      let portSlice = port.BeeperTrace |> Seq.toList
      port.BeeperTrace.Clear()
      let oracleSlice = oracle.DrainBeeperTrace()
      let traceDiff = portSlice <> oracleSlice
      let regsOk =
        port.Regs.Pc() = z80.Regs.Pc()
        && port.Regs.Sp() = z80.Regs.Sp()
        && port.Regs.Get Jetpac2.Core.R16.AF = z80.Regs.Get Jetpac.Core.R16.AF
        && port.Regs.Get Jetpac2.Core.R16.BC = z80.Regs.Get Jetpac.Core.R16.BC
        && port.Regs.Get Jetpac2.Core.R16.DE = z80.Regs.Get Jetpac.Core.R16.DE
        && port.Regs.Get Jetpac2.Core.R16.HL = z80.Regs.Get Jetpac.Core.R16.HL
        && port.Regs.Get Jetpac2.Core.R16.AF_ = z80.Regs.Get Jetpac.Core.R16.AF_
        && port.Regs.Get Jetpac2.Core.R16.BC_ = z80.Regs.Get Jetpac.Core.R16.BC_
        && port.Regs.Get Jetpac2.Core.R16.DE_ = z80.Regs.Get Jetpac.Core.R16.DE_
        && port.Regs.Get Jetpac2.Core.R16.HL_ = z80.Regs.Get Jetpac.Core.R16.HL_
        && port.Regs.Ix() = z80.Regs.Ix()
        && port.Regs.Iy() = z80.Regs.Iy()
        && port.Regs.I() = z80.Regs.I()
        && port.Regs.R() = z80.Regs.R()
        && port.Iff1 = z80.Iff1
        && port.Iff2 = z80.Iff2
        && port.IrqMode = z80.IrqMode
        && port.Halted = z80.Halted
      let kind =
        if not regsOk then "registers"
        elif memDiff >= 0 then sprintf "memory@%04X" memDiff
        elif traceDiff then "beeper trace"
        else "border"
      if memDiff >= 0 || borderDiff || traceDiff || not regsOk then
        divergence <-
          Some(replayFrame oracle port frameStartMem frameStartState frame script kind (max 0 memDiff))
      frame <- frame + 1
    { Frames = framesN
      Passed = divergence.IsNone
      FirstDivergence = divergence }
