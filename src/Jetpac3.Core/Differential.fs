namespace Jetpac3.Core

/// First-diff report from a lockstep oracle-vs-port run.
type DiffReport =
  { Frame: int            // first diff frame, or -1
    MemDiff: int          // memory address, or -1
    BorderDiff: bool
    TraceDiff: bool
    RegsOk: bool
    Message: string }

/// Lockstep differential execution: drives the oracle and the port to the same
/// frame boundary each frame, applies scripted keys, and compares memory,
/// border, beeper trace and registers. Mirrors Jetpac2's oracle harness.
module Differential =

  let runDiff (oracle: Jetpac.Core.Spectrum48)
              (port: Jetpac2.Core.Machine)
              (framesN: int)
              (script: (int * int * int * bool) list) : DiffReport =
    let z80 = oracle.DebugZ80
    let mutable diffs = 0
    let mutable firstFrame = -1
    let mutable firstMessage = ""
    let mutable frame = 0
    while frame < framesN && diffs = 0 do
      // One frame on each side; both start at the same absolute cycle and
      // advance identically, so they land on the same instruction boundary.
      oracle.RunGameFrame()
      let portEnd = port.FrameEnd
      while port.CycleCount() < portEnd do
        port.Step()
      port.FrameEnd <- port.FrameEnd + 69888L

      for (f, row, bit, pressed) in script do
        if f = frame then
          oracle.SetKey(row, bit, pressed)
          port.SetKey(row, bit, pressed)

      let mutable memDiff = -1
      let mutable i = 0x4000
      while memDiff < 0 && i < 0x10000 do
        if port.Memory.[i] <> oracle.Memory.[i] then memDiff <- i
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

      if memDiff >= 0 || borderDiff || traceDiff || not regsOk then
        diffs <- diffs + 1
        if firstFrame < 0 then
          firstFrame <- frame
          firstMessage <-
            sprintf "frame %d (mem=%s border=%b trace=%b regs=%b)"
              frame
              (if memDiff >= 0 then sprintf "%04X" memDiff else "-")
              borderDiff traceDiff regsOk
      frame <- frame + 1

    if diffs = 0 then
      { Frame = -1; MemDiff = -1; BorderDiff = false; TraceDiff = false; RegsOk = true; Message = "OK" }
    else
      { Frame = firstFrame; MemDiff = -1; BorderDiff = false; TraceDiff = false; RegsOk = false; Message = firstMessage }
