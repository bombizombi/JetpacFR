namespace Jetpac.Core

/// Port of specbolt's v2 Z80 core (z80/common/Z80Base.* + z80/v2/Z80.*).
/// All registers are ints; 8/16-bit values are masked at the point of use.
/// The instruction set lives in Z80Ops.fs; this class delegates opcode
/// execution through `Z80Dispatch.Run`, installed by Z80Ops.
type Z80(scheduler: Scheduler, memory: Memory) =
  let mutable halted_ = false
  let mutable irqPending_ = false
  let mutable iff1_ = false
  let mutable iff2_ = false
  let mutable irqMode_ = 0
  let inHandlers_ = System.Collections.Generic.List<int -> int option>()
  let outHandlers_ = System.Collections.Generic.List<int -> int -> unit>()
  let regs_ = RegisterFile()

  member this.Regs = regs_
  member this.Memory = memory

  member this.Iff1
    with get () = iff1_
    and set v = iff1_ <- v
  member this.Iff2
    with get () = iff2_
    and set v = iff2_ <- v
  member this.Halted
    with get () = halted_
    and set v = halted_ <- v
  member this.IrqMode
    with get () = irqMode_
    and set v = irqMode_ <- v

  member this.Flags() = Flags(regs_.Get R8.F)
  member this.SetFlags(flags: Flags) = regs_.Set(R8.F, flags.ToU8())

  member this.CycleCount() = scheduler.Cycles
  member this.PassTime(tstates: int) = scheduler.Tick (uint64 tstates)

  member this.Interrupt() = irqPending_ <- true
  member this.AddInHandler(handler: int -> int option) = inHandlers_.Add handler
  member this.AddOutHandler(handler: int -> int -> unit) = outHandlers_.Add handler

  /// Clear interrupt/iff/halt state (used by Spectrum48.Reset).
  member this.ResetInterruptState() =
    halted_ <- false
    irqPending_ <- false
    iff1_ <- false
    iff2_ <- false
    irqMode_ <- 0

  member this.In(port: int) : int =
    let mutable combined = 0xFF
    for handler in inHandlers_ do
      match handler port with
      | Some v -> combined <- combined &&& v
      | None -> ()
    combined

  member this.Out(port: int, value: int) =
    for handler in outHandlers_ do
      handler port value

  member this.Halt() =
    halted_ <- true
    regs_.SetPc((regs_.Pc() - 1) &&& 0xFFFF)

  member this.Branch(offset: int) = regs_.SetPc((regs_.Pc() + offset) &&& 0xFFFF)

  member this.ReadOpcode() : int =
    let opcode = this.ReadImmediate()
    // One cycle refresh.
    regs_.SetR((regs_.R() &&& 0x80) ||| ((regs_.R() + 1) &&& 0x7F))
    this.PassTime 1
    opcode

  member this.ReadImmediate() : int =
    this.PassTime 3
    let addr = regs_.Pc()
    regs_.SetPc((addr + 1) &&& 0xFFFF)
    memory.Read addr

  member this.ReadImmediate16() : int =
    let low = this.ReadImmediate()
    let high = this.ReadImmediate()
    ((high <<< 8) ||| low) &&& 0xFFFF

  member this.Write(address: int, value: int) =
    this.PassTime 3
    memory.Write(address &&& 0xFFFF, value &&& 0xFF)

  member this.Read(address: int) : int =
    this.PassTime 3
    memory.Read(address &&& 0xFFFF)

  member this.Pop8() : int =
    let value = this.Read(regs_.Sp())
    regs_.SetSp((regs_.Sp() + 1) &&& 0xFFFF)
    value

  member this.Pop16() : int =
    let low = this.Pop8()
    let high = this.Pop8()
    ((high <<< 8) ||| low) &&& 0xFFFF

  member this.Push8(value: int) =
    regs_.SetSp((regs_.Sp() - 1) &&& 0xFFFF)
    this.Write(regs_.Sp(), value &&& 0xFF)

  member this.Push16(value: int) =
    this.Push8((value >>> 8) &&& 0xFF)
    this.Push8(value &&& 0xFF)

  member private this.HandleInterrupt() =
    irqPending_ <- false
    if not iff1_ then
      ()
    else
      // Some dark business with the parity flag ignored here.
      if halted_ then
        halted_ <- false
        regs_.SetPc((regs_.Pc() + 1) &&& 0xFFFF)
      iff1_ <- false
      iff2_ <- false
      this.PassTime 7
      regs_.SetSp((regs_.Sp() - 2) &&& 0xFFFF)
      memory.Write16(regs_.Sp(), regs_.Pc())
      match irqMode_ with
      | 0
      | 1 -> regs_.SetPc 0x38
      | 2 ->
        // Assume the bus is at 0xff.
        let addr = 0xFF ||| ((regs_.I() <<< 8) &&& 0xFF00)
        regs_.SetPc(memory.Read16 addr)
      | _ -> failwith "Inconceivable interrupt mode"

  member this.ExecuteOne() =
    if irqPending_ then
      this.HandleInterrupt()
    if halted_ then
      this.PassTime 1
    else
      let opcode = this.ReadOpcode()
      Z80Dispatch.Run this opcode

/// Opcode execution dispatcher. Z80Ops.fs installs its table here; calling
/// execute_one() before installation fails loudly.
and Z80Dispatch =
  static member val Run: Z80 -> int -> unit =
    (fun _ _ -> failwith "Z80 instruction dispatcher not installed (Z80Ops.fs not initialized)") with get, set
