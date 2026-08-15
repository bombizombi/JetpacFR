namespace Jetpac2.Core

/// Semantic conversion of the game's routines (M5): idiomatic F# replacing
/// the generated instruction table routine by routine. Every converted
/// routine reproduces the original's observable effects exactly — memory
/// writes, registers, flag state, cycle counts (via PassTime) and beeper/
/// border output — so the oracle test stays green after each conversion.
///
/// Structure: each routine is a `Machine -> unit` function dispatched on
/// PC. A segment runs a straight-line run or loop, then sets PC to the next
/// instruction and returns to the step loop (which re-enters the same
/// function for internal addresses). Calls to other routines set PC to the
/// callee and return; the callee's RET pops back into the caller's range.
module Semantic =

  // ---------------------------------------------------------------------
  // Named memory locations (from the annotated disassembly)
  // ---------------------------------------------------------------------

  module Vars =
    let JetmanState = 0x5D00     // direction/state byte
    let JetmanPosX = 0x5D01
    let JetmanPosY = 0x5D02
    let JetmanColour = 0x5D03
    let JetmanVelocityX = 0x5D04
    let JetmanVelocityY = 0x5D05
    let LevelLives = 0x5DF0     // current player level/lives
    let LastFrame = 0x5DD4      // last observed SYSVAR_FRAMES low byte
    let FrameCounter = 0x5C78   // SYSVAR_FRAMES (low byte at +1)
    let CurrentAlien = 0x5DCB
    let GameTimer = 0x5DCC

  /// The explosion sound's pitch (DJNZ delay count). Original value 0x20.
  let ExplosionPitch = 0x20

  /// Push the return address and jump to a routine (CALL semantics).
  let private call (m: Machine) (target: int) =
    m.PassTime 1
    m.Push16(m.Regs.Pc())
    m.Regs.SetPc target

  /// OUT (n),A — fetch the port byte (3 T + PC advance), 4 T-states, then
  /// the OUT (beeper/border recorded by the machine).
  let private outA (m: Machine) =
    m.Fetch()
    let a = m.Regs.Get R8.A
    let port = (m.ReadImm() ||| (a <<< 8)) &&& 0xFFFF
    m.PassTime 4
    m.Out(port, a)

  // ---------------------------------------------------------------------
  // 0x61E7 — entry: wait for the frame counter to reach 0x83 (the moment
  // the loading screen has been fully displayed), then fall into the
  // zeroing/setup at 0x61ED. The RET-NZ exits to the loader's return.
  // ---------------------------------------------------------------------

  let entryWait (m: Machine) =
    // LD A,(0x5C79)
    m.Fetch()
    let addr = m.ReadImm16()
    m.Regs.Set(R8.A, m.Read addr)
    // CP 0x83
    m.Fetch()
    let imm = m.ReadImm()
    let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
    m.SetFlags f
    // RET NZ
    m.Fetch()
    m.PassTime 1
    if not (m.Flags().zero) then
      m.Regs.SetPc(m.Pop16())
    // else PC = 0x61ED (fallthrough)

  /// 0x61ED — zero the first 10 bytes of the score/state area, disable
  /// interrupts, set up the stack and enter the main loop (0x608A).
  let setupAndRun (m: Machine) =
    // LD HL,0x5CF0
    m.Fetch()
    m.Regs.Set(R16.HL, m.ReadImm16())
    // LD BC,0x0A00
    m.Fetch()
    m.Regs.Set(R16.BC, m.ReadImm16())
    // loop: LD (HL),C; INC HL; DJNZ (B=0x0A -> 10 iterations, writes 0x00)
    let mutable b = m.Regs.Get R8.B
    while b <> 0 do
      // LD (HL),C
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.C)
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
      // DJNZ (not taken on the final iteration)
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      b <- b - 1
      m.Regs.Set(R8.B, b)
      if b <> 0 then
        m.PassTime 5
        m.Branch offset
    // DI
    m.Fetch()
    m.Iff1 <- false
    m.Iff2 <- false
    // LD SP,0x5CF0
    m.Fetch()
    m.Regs.SetSp(m.ReadImm16())
    // CALL 0x608A (the main loop)
    m.Fetch()
    let target = m.ReadImm16()
    call m target

  // ---------------------------------------------------------------------
  // 0x60CF — copy the default rocket-module data (0x6018, 24 bytes) to the
  // active module (0x5D30), set A=8 and enter the rocket update routine.
  // The game also enters mid-routine at 0x60D6 (JR +0 onto the LDIR) with
  // HL/DE pre-set by the caller.
  // ---------------------------------------------------------------------

  /// The LDIR body shared by both entries. Each repeat re-fetches the ED
  /// prefix + opcode (R refresh + 8 T-states).
  let private rocketLdir (m: Machine) =
    let mutable bc = m.Regs.Get R16.BC
    while bc <> 0 do
      m.Fetch()  // ED prefix
      m.Fetch()  // LDIR opcode
      let add = 1
      let hl = m.Regs.Get R16.HL
      m.Regs.Set(R16.HL, (hl + add) &&& 0xFFFF)
      let byte = m.Read hl
      let de = m.Regs.Get R16.DE
      m.Regs.Set(R16.DE, (de + add) &&& 0xFFFF)
      m.Write(de, byte)
      m.PassTime 2
      let flagBits = byte + m.Regs.Get R8.A
      let newBc = (m.Regs.Get R16.BC - 1) &&& 0xFFFF
      m.Regs.Set(R16.BC, newBc)
      let preservedFlags = m.Flags() &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Carry())
      let flagsFromBits =
        (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags())
        ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())
      let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()
      m.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc)
      if newBc <> 0 then
        m.Regs.SetWz((m.Regs.Pc() - 1) &&& 0xFFFF)
        m.Regs.SetPc((m.Regs.Pc() - 2) &&& 0xFFFF)
        m.PassTime 5
      bc <- newBc

  /// 0x60CF / 0x60D6 / 0x60D8 — copy the default rocket-module data to the
  /// active module, set A=8 and enter the rocket update routine.
  let copyRocketData (m: Machine) =
    match m.Regs.Pc() with
    | 0x60CF ->
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
      m.Fetch()
      m.Regs.Set(R16.BC, m.ReadImm16())
      rocketLdir m
      // LD A,0x08; JP 0x6EF5
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x60D6 ->
      // JR +0 -> 0x60D8 (the LDIR)
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Branch 0
    | 0x60D8 ->
      rocketLdir m
      // LD A,0x08; JP 0x6EF5
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "CopyRocketData: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x60A9 — the frame starter: disable interrupts, initialise the rocket
  // module when the level is a multiple of 4, then run the frame work.
  // ---------------------------------------------------------------------

  let frameStarter (m: Machine) =
    match m.Regs.Pc() with
    | 0x60A9 ->
      // DI
      m.Fetch()
      m.Iff1 <- false
      m.Iff2 <- false
      // LD A,(0x5DF0)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
      // AND 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
      // JR NZ,0x60BB / fallthrough 0x60B1
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x60B1 ->
      // CALL 0x60CF (copy default rocket data)
      m.Fetch()
      let target = m.ReadImm16()
      call m target
    | 0x60B4 ->
      // LD HL,0x5DF1
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
      // INC (HL)
      m.Fetch()
      m.PassTime 1
      let struct (r, f) = Alu.inc8 (m.Read(m.Regs.Get R16.HL)) (m.Flags())
      m.Write(m.Regs.Get R16.HL, r)
      m.SetFlags f
      // CALL 0x619C
      m.Fetch()
      let target = m.ReadImm16()
      call m target
    | 0x60BB ->
      // CALL 0x6927
      m.Fetch()
      let target = m.ReadImm16()
      call m target
    | 0x60BE ->
      // CALL 0x608A (main loop)
      m.Fetch()
      let target = m.ReadImm16()
      call m target
    | 0x60C1 ->
      // CALL 0x766D
      m.Fetch()
      let target = m.ReadImm16()
      call m target
    | 0x60C4 ->
      // CALL 0x70A0
      m.Fetch()
      let target = m.ReadImm16()
      call m target
    | 0x60C7 ->
      // LD A,(0x5C78)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
      // LD (0x5DD4),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
      // EI
      m.Fetch()
      m.Iff1 <- true
      m.Iff2 <- true
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "FrameStarter: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // SFX routines (0x67FB-0x6831): square-wave beeper effects. Each effect
  // sets pitch (D) and duration (C), then drives OUT (0xFE) at exact
  // T-states; the beeper trace recorded by the machine keeps sound perfect.
  // ---------------------------------------------------------------------

  /// One DJNZ self-delay loop: `b` iterations of (fetch+operand+taken) and
  /// a final not-taken pass.
  let private djnzSelf (m: Machine) =
    m.Fetch()
    m.PassTime 1
    let offset = m.ReadImm()
    let offset = if offset >= 0x80 then offset - 0x100 else offset
    let nb = m.Regs.Get R8.B - 1
    m.Regs.Set(R8.B, nb)
    if nb <> 0 then
      m.PassTime 5
      m.Branch offset

  /// 0x67FB / 0x6801 — set pitch/duration and enter the explosion loop.
  let sfxExplosionParams (m: Machine) =
    match m.Regs.Pc() with
    | 0x67FB ->
      // LD D,ExplosionPitch — the pitch (delay) of the explosion sound.
      // EDIT ME: smaller = higher pitch.
      m.Fetch()
      m.Regs.Set(R8.D, ExplosionPitch)
      // LD C,0x50 — the duration in half-cycles
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
      // JR +9 -> 0x6810
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x6801 ->
      // LD D,0x50
      m.Fetch()
      m.Regs.Set(R8.D, m.ReadImm())
      // LD C,0x28
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
      // JR +9 -> 0x6810
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | _ -> failwithf "SfxExplosionParams: unhandled resume at %04X" (m.Regs.Pc())

  /// 0x6807 — LD D,0x30; LD C,0x40; LD A,0x10; OUT (0xFE),A; LD B,D; then
  /// the shared explosion loop at 0x6810.
  let sfxExplosionHi (m: Machine) =
    match m.Regs.Pc() with
    | 0x6807 ->
      m.Fetch()
      m.Regs.Set(R8.D, m.ReadImm())
    | 0x6809 ->
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x680B ->
      // LD A,0x10
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x680D ->
      // OUT (0xFE),A
      outA m
    | 0x680F ->
      // LD B,D
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.D)
    | _ -> failwithf "SfxExplosionHi: unhandled resume at %04X" (m.Regs.Pc())

  /// 0x6810 — the shared explosion loop: beep on (B=D), beep off (B=D),
  /// repeat C times; RET when C reaches zero.
  let explosionLoop (m: Machine) =
    match m.Regs.Pc() with
    | 0x6810 ->
      // DJNZ self-loop (delay D iterations)
      djnzSelf m
    | 0x6812 ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6813 ->
      // OUT (0xFE),A
      outA m
    | 0x6815 ->
      // LD B,D
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.D)
    | 0x6816 ->
      // DJNZ self-loop (delay D iterations)
      djnzSelf m
    | 0x6818 ->
      // DEC C
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.C) (m.Flags())
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6819 ->
      // JR NZ,0x680B
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x681B ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ExplosionLoop: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x681C — SfxPlayExplosion: a rising sweep of C from 8 to 0x38.
  // ---------------------------------------------------------------------

  let sfxPlayExplosion (m: Machine) =
    match m.Regs.Pc() with
    | 0x681C ->
      // LD C,0x08
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x681E ->
      // LD B,C
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.C)
    | 0x681F ->
      // DJNZ self-loop (delay C iterations)
      djnzSelf m
    | 0x6821 ->
      // LD A,0x10
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6823 ->
      // OUT (0xFE),A
      outA m
    | 0x6825 ->
      // LD B,C
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.C)
    | 0x6826 ->
      // DJNZ self-loop (delay C iterations)
      djnzSelf m
    | 0x6828 ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6829 ->
      // OUT (0xFE),A
      outA m
    | 0x682B ->
      // INC C
      m.Fetch()
      let struct (r2, f2) = Alu.inc8 (m.Regs.Get R8.C) (m.Flags())
      m.Regs.Set(R8.C, r2)
      m.SetFlags f2
    | 0x682C ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x682D ->
      // CP 0x38
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f3) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f3
    | 0x682F ->
      // JR NZ,0x681E
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6831 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "SfxPlayExplosion: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x71B8 / 0x71C6 — clear the pixel area (0x4000-0x57FF with 0x00) and
  // the attribute area (0x5800-0x5AFF with ClearAttrColour), via the shared
  // loop at 0x71BF — the whole screen is wiped each frame.
  // ---------------------------------------------------------------------

  /// The attribute written to every screen cell each frame: ink 7 (white)
  /// on paper 0 (black), bright. Original value 0x47.
  let ClearAttrColour = 0x47

  let private screenClearEntry (m: Machine) =
    match m.Regs.Pc() with
    | 0x71B8 ->
      // LD HL,0x4000
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
      // LD B,0x58
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
      // LD C,0x00
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x71C6 ->
      // LD HL,0x5800
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
      // LD B,0x5B
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
      // LD C,ClearAttrColour — the screen's paper/ink attribute
      // (the original immediate is consumed for exact timing; the constant
      // overrides the colour).
      m.Fetch()
      m.ReadImm() |> ignore
      m.Regs.Set(R8.C, ClearAttrColour)
      // JR +1 -> 0x71BF
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | _ -> failwithf "ScreenClearEntry: unhandled resume at %04X" (m.Regs.Pc())

  /// 0x71BF — the shared clear loop: LD (HL),C; INC HL; LD A,H; CP B;
  /// JR C — one instruction per dispatch so frame cuts land exactly where
  /// the emulator's do.
  let screenClearLoop (m: Machine) =
    match m.Regs.Pc() with
    | 0x71BF ->
      // LD (HL),C
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.C)
    | 0x71C0 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x71C1 ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, (m.Regs.Get R16.HL >>> 8) &&& 0xFF)
    | 0x71C2 ->
      // CP B
      m.Fetch()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.B)
      m.SetFlags f
    | 0x71C3 ->
      // JR C,0x71BF
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x71C5 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ScreenClearLoop: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x608A — the main loop: border/beeper off, wipe the screen and
  // attributes, draw the cursor line, then the frame work.
  // ---------------------------------------------------------------------

  let mainLoop (m: Machine) =
    match m.Regs.Pc() with
    | 0x608A ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x608B ->
      // OUT (0xFE),A — border/beeper off
      outA m
    | 0x608D ->
      // CALL 0x71B8 (pixel clear)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6090 ->
      // CALL 0x71C6 (attr clear)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6093 ->
      // CALL 0x7192 (draw)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6096 ->
      // LD HL,0x5820
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6099 ->
      // LD BC,0x2046
      m.Fetch()
      m.Regs.Set(R16.BC, m.ReadImm16())
    | 0x609C ->
      // LD (HL),C
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.C)
    | 0x609D ->
      // INC L (ALU inc — sets flags; 4 T-states)
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.L) (m.Flags())
      m.Regs.Set(R8.L, r)
      m.SetFlags f
    | 0x609E ->
      // DJNZ 0x609C
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x60A0 ->
      // CALL 0x7118
      m.Fetch()
      call m (m.ReadImm16())
    | 0x60A3 ->
      // CALL 0x7122
      m.Fetch()
      call m (m.ReadImm16())
    | 0x60A6 ->
      // JP 0x712C
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "MainLoop: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x61BF — StartGame / MenuScreen: flash (or un-flash) the current menu
  // option's attribute row.
  // ---------------------------------------------------------------------

  let startGame (m: Machine) =
    match m.Regs.Pc() with
    | 0x61BF ->
      // LD A,(0x5DD1)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x61C2 ->
      // AND A (fast ALU — sets flags)
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x61C3 ->
      // JR NZ,0x61E2
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x61C5 ->
      // LD HL,0x0018
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x61C8 ->
      // CALL 0x720E
      m.Fetch()
      call m (m.ReadImm16())
    | 0x61CB ->
      // LD B,0x03
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x61CD ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x61CE ->
      // OR 0x80
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x61D0 ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x61D1 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x61D2 ->
      // DJNZ 0x61CD
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x61D4 | 0x61E1 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x61D5 ->
      // CALL 0x720E (MenuScreen path)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x61D8 ->
      // LD B,0x03
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x61DA ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x61DB ->
      // AND 0x7F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x61DD ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x61DE ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x61DF ->
      // DJNZ 0x61DA
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x61E2 ->
      // LD HL,0x00D8
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x61E5 ->
      // JR 0x61C8
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | _ -> failwithf "StartGame: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6174 — PlayerInit: swap the two players' state buffers (0x5D30 and
  // 0x5D98, 0x18 bytes each) so the inactive player becomes active.
  // ---------------------------------------------------------------------

  let playerInit (m: Machine) =
    match m.Regs.Pc() with
    | 0x6174 ->
      // CALL 0x617F (the swap loop)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6177 ->
      // LD HL,0x5D30
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x617A ->
      // LD DE,0x5D98
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x617D ->
      // LD B,0x18
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x617F ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x6180 ->
      // LD C,(HL)
      m.Fetch()
      m.Regs.Set(R8.C, m.Read(m.Regs.Get R16.HL))
    | 0x6181 ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x6182 ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6183 ->
      // LD (DE),A
      m.Fetch()
      m.Write(m.Regs.Get R16.DE, m.Regs.Get R8.A)
    | 0x6184 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6185 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x6186 ->
      // DJNZ 0x617F
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6188 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "PlayerInit: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x619C — reset the active player's state: copy the default jetman
  // values (0x6010, 8 bytes) to 0x5D00, then set the display flag.
  // ---------------------------------------------------------------------

  let resetPlayerState (m: Machine) =
    match m.Regs.Pc() with
    | 0x619C ->
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x619F ->
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x61A2 ->
      m.Fetch()
      m.Regs.Set(R16.BC, m.ReadImm16())
    | 0x61A5 ->
      // LDIR (8 bytes)
      let mutable bc = m.Regs.Get R16.BC
      while bc <> 0 do
        m.Fetch(); m.Fetch()
        let add = 1
        let hl = m.Regs.Get R16.HL
        m.Regs.Set(R16.HL, (hl + add) &&& 0xFFFF)
        let byte = m.Read hl
        let de = m.Regs.Get R16.DE
        m.Regs.Set(R16.DE, (de + add) &&& 0xFFFF)
        m.Write(de, byte)
        m.PassTime 2
        let flagBits = byte + m.Regs.Get R8.A
        let newBc = (m.Regs.Get R16.BC - 1) &&& 0xFFFF
        m.Regs.Set(R16.BC, newBc)
        let preservedFlags = m.Flags() &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Carry())
        let flagsFromBits =
          (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags())
          ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())
        let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()
        m.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc)
        if newBc <> 0 then
          m.Regs.SetWz((m.Regs.Pc() - 1) &&& 0xFFFF)
          m.Regs.SetPc((m.Regs.Pc() - 2) &&& 0xFFFF)
          m.PassTime 5
        bc <- newBc
    | 0x61A7 ->
      // LD A,0x80
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x61A9 ->
      // LD HL,0x5CF3
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x61AC ->
      // BIT 0,(HL) (CB prefix + opcode fetches)
      m.Fetch()
      m.Fetch()
      m.PassTime 1
      let lhs = m.Read(m.Regs.Get R16.HL)
      m.SetFlags(Alu.bit lhs 1 (m.Flags()) ((m.Regs.Get R16.HL >>> 8) &&& 0xFF))
    | 0x61AE ->
      // JR Z,0x61B2
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x61B0 ->
      // ADD A,0x7F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x61B2 ->
      // LD (0x5DD7),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x61B5 ->
      // LD A,(0x5DF1)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x61B8 ->
      // DEC A
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x61B9 ->
      // LD (0x5DF1),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x61BC ->
      // JP 0x70A0
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "ResetPlayerState: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6344 — the game's main flow: run the frame starter, then if the
  // frame counter has moved, run the per-frame work (0x6963).
  // ---------------------------------------------------------------------

  let mainFlow (m: Machine) =
    match m.Regs.Pc() with
    | 0x6344 ->
      // CALL 0x60A9 (frame starter)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6347 ->
      // LD IX,0x5D30
      m.Fetch()
      m.Fetch()
      m.Regs.Set(R16.IX, m.ReadImm16())
    | 0x634B ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x634C ->
      // LD (0x5DCB),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x634F ->
      // LD A,(0x5C78)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6352 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6353 ->
      // LD A,(0x5DD4)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6356 ->
      // CP C
      m.Fetch()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
      m.SetFlags f
    | 0x6357 ->
      // CALL NZ,0x6963 (per-frame work when the frame counter moved)
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then
        m.PassTime 1
        m.Push16(m.Regs.Pc())
        m.Regs.SetPc target
    | 0x635A ->
      // LD HL,0x69A4
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x635D ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x635E ->
      // LD HL,0x6372
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | _ -> failwithf "MainFlow: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6963 — the per-frame work entry: latch the frame counter, mark the
  // frame ticked, then patch the actor dispatch (0x69CF) to run the actor
  // update loop and enter it.
  // ---------------------------------------------------------------------

  let frameWork (m: Machine) =
    match m.Regs.Pc() with
    | 0x6963 ->
      // DI
      m.Fetch()
      m.Iff1 <- false
      m.Iff2 <- false
    | 0x6964 ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6965 ->
      // LD (0x5DD4),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6968 ->
      // LD A,0x01
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x696A ->
      // LD (0x5DD5),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x696D ->
      // PUSH IX (DD prefix + opcode)
      m.Fetch()
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.IX)
    | 0x696F ->
      // LD HL,0x5D30
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6972 ->
      // LD (0x69B1),HL — patch the dispatch operand (writes both bytes)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Write(addr, m.Regs.Get R16.HL &&& 0xFF)
      m.Write((addr + 1) &&& 0xFFFF, (m.Regs.Get R16.HL >>> 8) &&& 0xFF)
    | 0x6975 ->
      // LD A,0xC3 (JP opcode)
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6977 ->
      // LD (0x69CF),A — repatch the dispatch stub to JP
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x697A ->
      // LD HL,0x6999
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x697D ->
      // LD (0x69D0),HL — the JP target (writes both bytes)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Write(addr, m.Regs.Get R16.HL &&& 0xFF)
      m.Write((addr + 1) &&& 0xFFFF, (m.Regs.Get R16.HL >>> 8) &&& 0xFF)
    | 0x6980 ->
      // LD IX,0x5D00
      m.Fetch()
      m.Fetch()
      m.Regs.Set(R16.IX, m.ReadImm16())
    | 0x6983 ->
      // JP 0x634F
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "FrameWork: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x720E — convert a screen coordinate in (H,L) to an attribute-file
  // address in HL (x = L bits 0-4, y = H scaled; returns 0x5800+offset).
  // ---------------------------------------------------------------------

  let coordToAttr (m: Machine) =
    match m.Regs.Pc() with
    | 0x720E ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x720F ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7210 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7211 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7212 ->
      // AND 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7214 ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x7215 ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x7216 ->
      // RLCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7217 ->
      // RLCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7218 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x7219 ->
      // AND 0xE0
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x721B ->
      // OR L
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x721C ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x721D ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x721E ->
      // AND 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7220 ->
      // OR 0x58
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7222 ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x7223 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "CoordToAttr: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x712C — display a 3-byte score (DE = the score bytes): each byte is
  // drawn as two decimal digits (high nibble then low nibble) via the
  // character writer at 0x714C.
  // ---------------------------------------------------------------------

  let scoreDisplay (m: Machine) =
    match m.Regs.Pc() with
    | 0x712C ->
      // LD HL,0x402D
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x712F ->
      // LD DE,0x5CF0
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x7132 ->
      // LD B,0x03
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x7134 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x7135 | 0x7136 | 0x7137 | 0x7138 ->
      // RRCA (4x)
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7139 ->
      // AND 0x0F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x713B | 0x7143 ->
      // ADD A,0x30 (digit -> ASCII)
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x713D | 0x7145 ->
      // CALL 0x714C (write the character)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x7140 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x7141 ->
      // AND 0x0F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7148 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x7149 ->
      // DJNZ 0x7134
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x714B ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ScoreDisplay: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6987 — restore the 0x69CF dispatch stub back to "LD A,(0x0244)"
  // (the frame-work's end), and 0x6999 — the frame-work's continuation:
  // run the actor updates, clear the frame-ticked flag, re-enable
  // interrupts and return.
  // ---------------------------------------------------------------------

  let dispatchRestore (m: Machine) =
    match m.Regs.Pc() with
    | 0x6987 ->
      // LD HL,0x5D88
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x698A ->
      // LD (0x69B1),HL — patch the dispatch operand back
      m.Fetch()
      let addr = m.ReadImm16()
      m.Write(addr, m.Regs.Get R16.HL &&& 0xFF)
      m.Write((addr + 1) &&& 0xFFFF, (m.Regs.Get R16.HL >>> 8) &&& 0xFF)
    | 0x698D ->
      // LD A,0x3A (the LD A,(nn) opcode)
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x698F ->
      // LD (0x69CF),A — restore the dispatch opcode
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6992 ->
      // LD HL,0x0244
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6995 ->
      // LD (0x69D0),HL — the LD A,(nn) operand
      m.Fetch()
      let addr = m.ReadImm16()
      m.Write(addr, m.Regs.Get R16.HL &&& 0xFF)
      m.Write((addr + 1) &&& 0xFFFF, (m.Regs.Get R16.HL >>> 8) &&& 0xFF)
    | 0x6998 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "DispatchRestore: unhandled resume at %04X" (m.Regs.Pc())

  let actorUpdateTail (m: Machine) =
    match m.Regs.Pc() with
    | 0x6999 ->
      // CALL 0x6987 (restore the dispatch stub)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x699C ->
      // POP IX (DD prefix + opcode)
      m.Fetch()
      m.Fetch()
      m.Regs.Set(R16.IX, m.Pop16())
    | 0x699E ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x699F ->
      // LD (0x5DD5),A — clear the frame-ticked flag
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x69A2 ->
      // EI
      m.Fetch()
      m.Iff1 <- true
      m.Iff2 <- true
    | 0x69A3 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ActorUpdateTail: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x685D — UpdateActorState: a short beeper "click" (beep on, delay,
  // beep off, delay, repeat C times).
  // ---------------------------------------------------------------------

  let actorClick (m: Machine) =
    match m.Regs.Pc() with
    | 0x685D ->
      // LD A,0x10
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x685F ->
      // OUT (0xFE),A — beep on
      outA m
    | 0x6861 | 0x6867 ->
      // LD B,C
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.C)
    | 0x6862 | 0x6868 ->
      // DJNZ self-loop (delay C iterations)
      djnzSelf m
    | 0x6864 ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6865 ->
      // OUT (0xFE),A — beep off
      outA m
    | 0x686A ->
      // DEC C
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.C) (m.Flags())
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x686B ->
      // JR NZ,0x685D
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x686D ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ActorClick: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6873 — actor init: copy the jetman's state bytes to the actor
  // (0x5D80+), run the colour setup (0x68A9) and reset the jetman state.
  // ---------------------------------------------------------------------

  let actorInit (m: Machine) =
    match m.Regs.Pc() with
    | 0x6873 ->
      // PUSH IX
      m.Fetch()
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.IX)
    | 0x6875 ->
      // LD IX,0x5D80
      m.Fetch()
      m.Fetch()
      m.Regs.Set(R16.IX, m.ReadImm16())
    | 0x6879 ->
      // LD HL,0x5D00
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x687C ->
      // LD C,(HL)
      m.Fetch()
      m.Regs.Set(R8.C, m.Read(m.Regs.Get R16.HL))
    | 0x687D | 0x6882 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x687E | 0x6883 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x687F ->
      // LD (IX+1),A
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      let addr = (m.Regs.Ix() + 1) &&& 0xFFFF
      m.Write(addr, m.Regs.Get R8.A)
    | 0x6884 ->
      // LD (IX+2),A
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      let addr = (m.Regs.Ix() + 2) &&& 0xFFFF
      m.Write(addr, m.Regs.Get R8.A)
    | 0x6887 ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6888 ->
      // CALL 0x68A9 (colour setup)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x688B ->
      // POP IX
      m.Fetch()
      m.Fetch()
      m.Regs.Set(R16.IX, m.Pop16())
    | 0x688D ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x688E ->
      // LD (0x5D00),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6891 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ActorInit: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x60DF — rocket setup after the data copy: reset the module actors and
  // check the carry flag.
  // ---------------------------------------------------------------------

  let rocketSetup (m: Machine) =
    match m.Regs.Pc() with
    | 0x60DF ->
      // LD HL,0x5D48
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x60E2 ->
      // LD B,0x0A
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x60E4 ->
      // CALL 0x665E (reset the module actors)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x60E7 ->
      // LD HL,0x5D3C
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x60EA ->
      // RES 1,(HL)
      m.Fetch()
      m.Fetch()
      m.PassTime 1
      let lhs = m.Read(m.Regs.Get R16.HL)
      m.Write(m.Regs.Get R16.HL, lhs &&& ~~~2)
    | 0x60EC ->
      // LD HL,0x5D44
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x60EF ->
      // RES 1,(HL)
      m.Fetch()
      m.Fetch()
      m.PassTime 1
      let lhs = m.Read(m.Regs.Get R16.HL)
      m.Write(m.Regs.Get R16.HL, lhs &&& ~~~2)
    | 0x60F1 ->
      // LD A,(0x5CF3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x60F4 ->
      // AND 0x01
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x60F6 ->
      // JR NZ,0x6105
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | _ -> failwithf "RocketSetup: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6361 — UpdateHiScore: index into the score/digit table via the
  // current score value and jump through the 0x5CB0 dispatch stub.
  // ---------------------------------------------------------------------

  let updateHiScore (m: Machine) =
    match m.Regs.Pc() with
    | 0x6361 ->
      // LD A,(IX+0)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 0) &&& 0xFFFF))
    | 0x6364 ->
      // RLCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6365 ->
      // AND 0x7E
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6367 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6368 ->
      // LD B,0x00
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x636A ->
      // ADD HL,BC
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x636B ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x636C ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x636D ->
      // LD H,(HL)
      m.Fetch()
      m.Regs.Set(R8.H, m.Read(m.Regs.Get R16.HL))
    | 0x636E ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x636F ->
      // JP 0x5CB0 (the computed dispatch)
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "UpdateHiScore: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x65A8 — the jetman's object interaction: picking up a rocket part or
  // fuel module, updating the collection counters and playing the pickup
  // sound.
  // ---------------------------------------------------------------------

  let jetmanCollect (m: Machine) =
    match m.Regs.Pc() with
    | 0x65A8 ->
      // JP C,0x653D
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().carry then
        m.Regs.SetPc target
    | 0x65AB ->
      // LD A,(0x5D3C)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x65AE ->
      // OR 0x01 — mark the rocket part collected
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65B0 ->
      // LD (0x5D3C),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x65B3 ->
      // LD A,(0x5D34) — the rocket part counter
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x65B6 ->
      // INC A
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65B7 ->
      // LD (0x5D34),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x65BA ->
      // LD A,(IX+6) — the item's sprite index
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 6) &&& 0xFFFF))
    | 0x65BD ->
      // ADD A,0x08
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65BF ->
      // CALL 0x6EF5 (rocket update)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x65C2 ->
      // CALL 0x72A5 (draw the actor)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x65C5 ->
      // LD (IX+0),0x00 — mark the item unused (immediate form: PassTime 2)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 2
      m.Write((m.Regs.Ix() + 0) &&& 0xFFFF, m.ReadImm())
    | 0x65C9 ->
      // JP 0x67FB (play the pickup sound)
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x65CC ->
      // LD A,(IX+2) — the fuel module's Y position
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 2) &&& 0xFFFF))
    | 0x65CF ->
      // CP 0xB0
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x65D1 ->
      // JP C,0x653D
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().carry then
        m.Regs.SetPc target
    | 0x65D4 ->
      // LD A,(0x5D35) — the fuel counter
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x65D7 ->
      // INC A
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65D8 ->
      // LD (0x5D35),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x65DB ->
      // JR 0x65C2
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | _ -> failwithf "JetmanCollect: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x653D — the jetman's item interactions: catching a falling item
  // (attach to the jetman, play the pickup sound), dropping/launching it,
  // and the item's collision checks.
  // ---------------------------------------------------------------------

  let jetmanInteract (m: Machine) =
    match m.Regs.Pc() with
    | 0x653D | 0x6540 ->
      // INC (IX+2) — nudge the item's Y position
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.PassTime 1
      let lhs = m.Read((m.Regs.Ix() + 2) &&& 0xFFFF)
      let struct (r, f) = Alu.inc8 lhs (m.Flags())
      m.Write((m.Regs.Ix() + 2) &&& 0xFFFF, r)
      m.SetFlags f
    | 0x6543 ->
      // CALL 0x726A
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6546 ->
      // JP 0x71CF
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x6549 ->
      // LD A,(0x5DF0)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x654C ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x654D ->
      // AND 0x06
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x654F ->
      // CALL 0x64E3 (item update by level)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6552 ->
      // CALL 0x729B
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6555 ->
      // JP 0x71CF
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x6558 ->
      // SET 1,(IX+4) — mark "carrying"
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 1
      m.Fetch()
      let lhs = m.Read((m.Regs.Ix() + 4) &&& 0xFFFF)
      m.Write((m.Regs.Ix() + 4) &&& 0xFFFF, lhs ||| 2)
    | 0x655C ->
      // CALL 0x72A5
      m.Fetch()
      call m (m.ReadImm16())
    | 0x655F ->
      // LD BC,0x0100
      m.Fetch()
      m.Regs.Set(R16.BC, m.ReadImm16())
    | 0x6562 ->
      // CALL 0x70F5
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6565 ->
      // CALL 0x6801 (play the pickup sound)
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6568 ->
      // LD HL,(0x5D01) — the jetman's position (both bytes)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.L, m.Read addr)
      m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    | 0x656B ->
      // LD (IX+1),L — attach the item to the jetman's X
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Write((m.Regs.Ix() + 1) &&& 0xFFFF, m.Regs.Get R8.L)
    | 0x656E ->
      // LD (IX+2),H — attach to the jetman's Y
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Write((m.Regs.Ix() + 2) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x6571 ->
      // CALL 0x7327
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6574 ->
      // JR 0x6543
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x6576 ->
      // LD HL,(0x5D01) (both bytes)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.L, m.Read addr)
      m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    | 0x6579 ->
      // LD (IX+1),L
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Write((m.Regs.Ix() + 1) &&& 0xFFFF, m.Regs.Get R8.L)
    | 0x657C ->
      // LD (IX+2),H
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Write((m.Regs.Ix() + 2) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x657F ->
      // LD A,(0x5D31)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6582 ->
      // SUB A,(IX+1) — horizontal distance to the jetman
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + 1) &&& 0xFFFF)
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6585 ->
      // JP P,0x658A
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().sign) then
        m.Regs.SetPc target
    | 0x6588 ->
      // NEG
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.sub8 0 (m.Regs.Get R8.A) false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x658A ->
      // CP 0x05
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x658C ->
      // JR NC,0x6543
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().carry) then
        m.PassTime 5
        m.Branch offset
    | 0x658E ->
      // SET 2,(IX+4)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 1
      m.Fetch()
      let lhs = m.Read((m.Regs.Ix() + 4) &&& 0xFFFF)
      m.Write((m.Regs.Ix() + 4) &&& 0xFFFF, lhs ||| 4)
    | 0x6592 ->
      // LD A,(0x5D31)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6595 ->
      // LD (IX+1),A
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Write((m.Regs.Ix() + 1) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x6598 ->
      // JR 0x6543
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x659A ->
      // LD A,(IX+6)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 6) &&& 0xFFFF))
    | 0x659D ->
      // CP 0x18
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x659F ->
      // JR Z,0x65CC
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x65A1 ->
      // SLA A
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.A) Alu.Left
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65A3 ->
      // ADD A,(IX+2)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + 2) &&& 0xFFFF)
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65A6 ->
      // CP 0xB7
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | _ -> failwithf "JetmanInteract: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6396 — compare the player's score (0x5CF4, 3 bytes) with the high
  // score (0x5CF7) by swapping byte order and subtracting.
  // ---------------------------------------------------------------------

  let scoreCompare (m: Machine) =
    match m.Regs.Pc() with
    | 0x6396 ->
      // LD HL,(0x5CF4) — both bytes
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.L, m.Read addr)
      m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    | 0x6399 ->
      // LD DE,(0x5CF7) (ED prefix + opcode) — both bytes
      m.Fetch()
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.E, m.Read addr)
      m.Regs.Set(R8.D, m.Read((addr + 1) &&& 0xFFFF))
    | 0x639D ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x639E ->
      // LD L,H
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.H)
    | 0x639F ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x63A0 ->
      // LD A,E
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.E)
    | 0x63A1 ->
      // LD E,D
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.D)
    | 0x63A2 ->
      // LD D,A
      m.Fetch()
      m.Regs.Set(R8.D, m.Regs.Get R8.A)
    | 0x63A3 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x63A4 ->
      // SBC HL,DE
      m.Fetch()
      m.Fetch()
      let rhs = m.Regs.Get R16.DE
      let lhs = m.Regs.Get R16.HL
      let struct (result, flags) = Alu.sbc16 lhs rhs (m.Flags().carry)
      m.Regs.Set(R16.HL, result)
      m.SetFlags flags
      m.PassTime 7
    | _ -> failwithf "ScoreCompare: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6EF5 — the rocket module update: select the rocket data row by the
  // current level and set up the module state counters.
  // ---------------------------------------------------------------------

  let rocketUpdate (m: Machine) =
    match m.Regs.Pc() with
    | 0x6EF5 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6EF6 ->
      // LD A,(0x5DF0)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6EF9 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6EFA ->
      // AND 0x06
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6EFC ->
      // OR C
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6EFD ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6EFE ->
      // LD B,0x00
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x6F00 ->
      // LD HL,0x67C1 — the rocket data table
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6F03 ->
      // ADD HL,BC
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x6F04 ->
      // LD DE,0x5ECC
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x6F07 ->
      // LD A,0x02
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6F09 ->
      // LD (0x5DD3),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6F0C ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6F0D ->
      // LD (0x5DD2),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6F10 ->
      // LD C,0x04
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x6F12 ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6F13 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6F14 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x6F15 ->
      // LD C,0x01
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x6F17 ->
      // CALL 0x6F3E
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6F1A ->
      // LD A,B
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.B)
    | 0x6F1B ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6F1C | 0x6F1D ->
      // DEC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL - 1) &&& 0xFFFF)
    | 0x6F1E ->
      // DEC C
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.C) (m.Flags())
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6F1F ->
      // JR NZ,0x6F13
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6F21 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "RocketUpdate: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x63A6 — high-score update continuation: decide whether the player's
  // score beats the high score.
  // ---------------------------------------------------------------------

  let hiScoreUpdate (m: Machine) =
    match m.Regs.Pc() with
    | 0x63A6 ->
      // JR C,0x63B4
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x63A8 ->
      // JR NZ,0x63D3
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x63AA ->
      // LD A,(0x5CF6)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x63AD ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x63AE ->
      // LD A,(0x5CF9)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x63B1 ->
      // CP E
      m.Fetch()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.E)
      m.SetFlags f
    | 0x63B2 ->
      // JR C,0x63D3
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x63B4 ->
      // LD HL,0x5CF7
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x63B7 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | _ -> failwithf "HiScoreUpdate: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x64E3 — item update by level: pick the item's data row.
  // ---------------------------------------------------------------------

  let itemUpdate (m: Machine) =
    match m.Regs.Pc() with
    | 0x64E3 ->
      // ADD A,(IX+6)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + 6) &&& 0xFFFF)
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x64E6 ->
      // LD HL,0x67C1
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x64E9 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x64EA ->
      // LD B,0x00
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x64EC ->
      // ADD HL,BC
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x64ED ->
      // LD E,(HL)
      m.Fetch()
      m.Regs.Set(R8.E, m.Read(m.Regs.Get R16.HL))
    | 0x64EE ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x64EF ->
      // LD D,(HL)
      m.Fetch()
      m.Regs.Set(R8.D, m.Read(m.Regs.Get R16.HL))
    | 0x64F0 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ItemUpdate: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x665E — reset the module actors: clear every 8th byte across the
  // module state block.
  // ---------------------------------------------------------------------

  let moduleReset (m: Machine) =
    match m.Regs.Pc() with
    | 0x665E ->
      // LD DE,0x0008
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x6661 ->
      // LD (HL),0x00
      m.Fetch()
      m.ReadImm() |> ignore
      m.Write(m.Regs.Get R16.HL, 0)
    | 0x6663 ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x6664 ->
      // DJNZ 0x6661
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6666 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ModuleReset: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6F3E — the rocket module writer: load the module pointer, then the
  // update variant at 0x6F49.
  // ---------------------------------------------------------------------

  let rocketWriter (m: Machine) =
    match m.Regs.Pc() with
    | 0x6F3E ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6F3F ->
      // PUSH DE
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.DE)
    | 0x6F40 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6F41 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6F42 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6F43 ->
      // LD H,(HL)
      m.Fetch()
      m.Regs.Set(R8.H, m.Read(m.Regs.Get R16.HL))
    | 0x6F44 ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x6F45 | 0x6F46 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6F47 ->
      // JR +20 -> 0x6F5D
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x6F49 ->
      // LD BC,0x0002
      m.Fetch()
      m.Regs.Set(R16.BC, m.ReadImm16())
    | 0x6F4C ->
      // LD A,0x04
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6F4E ->
      // LD A,(0x5DD3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6F51 ->
      // LD A,0x01
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6F53 ->
      // LD (0x5DD2),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6F56 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6F57 ->
      // PUSH DE
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.DE)
    | 0x6F58 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6F59 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6F5A | 0x6F65 | 0x6F69 | 0x6F6C | 0x6F75 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6F5B ->
      // LD H,(HL)
      m.Fetch()
      m.Regs.Set(R8.H, m.Read(m.Regs.Get R16.HL))
    | 0x6F5C ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x6F5D ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6F5E ->
      // EX DE,HL
      m.Fetch()
      m.Regs.Ex(R16.DE, R16.HL)
    | 0x6F5F | 0x6F66 ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x6F60 ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x6F61 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6F62 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6F63 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6F64 | 0x6F6D ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x6F67 ->
      // LD (HL),0x00
      m.Fetch()
      m.ReadImm() |> ignore
      m.Write(m.Regs.Get R16.HL, 0)
    | 0x6F6A ->
      // LD (HL),0x03
      m.Fetch()
      m.ReadImm() |> ignore
      m.Write(m.Regs.Get R16.HL, 3)
    | 0x6F6E ->
      // CP 0x11
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x6F70 ->
      // JR C,0x6F74
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x6F72 ->
      // LD A,0x10
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6F74 ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x6F76 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x6F77 ->
      // CALL 0x6ED8
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6F7A ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6F7B ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x6F7C ->
      // LD DE,0x0033
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x6F7F ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x6F80 ->
      // POP DE
      m.Fetch()
      m.Regs.Set(R16.DE, m.Pop16())
    | 0x6F81 ->
      // EX DE,HL
      m.Fetch()
      m.Regs.Ex(R16.DE, R16.HL)
    | 0x6F82 | 0x6F83 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6F84 ->
      // LD A,(0x5DD3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6F87 ->
      // ADD A,B
      m.Fetch()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.B) false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6F88 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x6F89 ->
      // DEC C
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.C) (m.Flags())
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6F8A ->
      // JR NZ,0x6F56 — the module loop
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6F8C ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "RocketWriter: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6334 — PerformJump: the jump impulse is applied elsewhere; this
  // address is a plain return.
  // ---------------------------------------------------------------------

  let performJump (m: Machine) =
    match m.Regs.Pc() with
    | 0x6334 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "PerformJump: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6523 — CollectRocketItem: dispatch on the jetman/item flags to
  // handle rocket-part and fuel collection.
  // ---------------------------------------------------------------------

  let collectRocketItem (m: Machine) =
    match m.Regs.Pc() with
    | 0x6523 ->
      // BIT 2,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 2) (m.Flags()) (m.Regs.Get R8.A))
    | 0x6525 ->
      // JP NZ,0x659A
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then
        m.Regs.SetPc target
    | 0x6528 ->
      // BIT 1,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 1) (m.Flags()) (m.Regs.Get R8.A))
    | 0x652A ->
      // JR NZ,0x6571
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x652C ->
      // BIT 0,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.A))
    | 0x652E ->
      // JR Z,0x6542
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x6530 ->
      // CALL 0x6E1C — the item pickup
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6533 ->
      // DEC E
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.E) (m.Flags())
      m.Regs.Set(R8.E, r)
      m.SetFlags f
    | 0x6534 ->
      // JR Z,0x656D
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | _ -> failwithf "CollectRocketItem: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x7308 — ReadInputLR: pack the jetman's movement vector from the
  // screen-coordinate registers into the action bits.
  // ---------------------------------------------------------------------

  let readInputLR (m: Machine) =
    match m.Regs.Pc() with
    | 0x7308 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x7309 | 0x730A | 0x730B ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x730C ->
      // AND 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x730E ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x730F ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x7310 | 0x7311 ->
      // RLCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7312 ->
      // AND 0xE0
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7314 ->
      // OR L
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7315 ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x7316 ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x7317 ->
      // AND 0x07
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7319 ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x731A ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x731B | 0x731C | 0x731D ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x731E ->
      // AND 0x18
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7320 ->
      // OR 0x40
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7322 ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x7323 ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x7324 ->
      // OR H
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.H)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7325 ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x7326 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ReadInputLR: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x7327 — copy the joystick/interface-2 state into the game variables.
  // ---------------------------------------------------------------------

  let copyJoystickState (m: Machine) =
    match m.Regs.Pc() with
    | 0x7327 ->
      // LD A,(IX+1)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 1) &&& 0xFFFF))
    | 0x732A ->
      // LD (0x5DC0),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x732D ->
      // LD A,(IX+2)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 2) &&& 0xFFFF))
    | 0x7330 ->
      // LD (0x5DC1),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x7333 ->
      // LD A,(IX+0)
      m.Fetch()
      m.Fetch()
      m.ReadImm() |> ignore
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + 0) &&& 0xFFFF))
    | 0x7336 ->
      // LD (0x5DC2),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x7339 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "CopyJoystickState: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x733B — read the interface-2 joystick port: IN A,(0x1F); CPL.
  // ---------------------------------------------------------------------

  let readInterface2 (m: Machine) =
    match m.Regs.Pc() with
    | 0x733A ->
      // IN A,(0x1F): the full port is (A<<8)|n
      m.Fetch()
      let port = (m.ReadImm() ||| ((m.Regs.Get R8.A) <<< 8)) &&& 0xFFFF
      m.PassTime 4
      m.Regs.Set(R8.A, m.In port)
    | 0x733C ->
      // CPL
      m.Fetch()
      let struct (r, f) = Alu.cpl (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | _ -> failwithf "ReadInterface2: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x7134 — DisplayString: expand packed two-char bytes into glyphs.
  // 0x714C — WriteAsciiChars: draw one font glyph at the attr position.
  // 0x716C — print a labelled string with a highlight colour.
  // 0x7199 — set up the 1UP/2UP/HIGH score labels.
  // ---------------------------------------------------------------------

  let displayString (m: Machine) =
    match m.Regs.Pc() with
    | 0x7134 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x7135 | 0x7136 | 0x7137 | 0x7138 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7139 ->
      // AND 0x0F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x713B ->
      // ADD A,0x30
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x713D | 0x7145 ->
      // CALL 0x714C
      m.Fetch()
      call m (m.ReadImm16())
    | 0x7140 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x7141 ->
      // AND 0x0F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7143 ->
      // ADD A,0x30
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7148 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x7149 ->
      // DJNZ 0x7134
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x714B ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "DisplayString: unhandled resume at %04X" (m.Regs.Pc())

  let writeAsciiChars (m: Machine) =
    match m.Regs.Pc() with
    | 0x714C ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x714D ->
      // PUSH DE
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.DE)
    | 0x714E ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x714F ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x7150 ->
      // LD H,0x00
      m.Fetch()
      m.Regs.Set(R8.H, m.ReadImm())
    | 0x7152 | 0x7153 | 0x7154 ->
      // ADD HL,HL
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.HL) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x7155 ->
      // LD DE,(0x5C36) — the font table pointer
      m.Fetch()
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.E, m.Read addr)
      m.Regs.Set(R8.D, m.Read((addr + 1) &&& 0xFFFF))
    | 0x7159 ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x715A ->
      // EX DE,HL
      m.Fetch()
      m.Regs.Ex(R16.DE, R16.HL)
    | 0x715B ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x715C ->
      // LD B,0x08
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x715E ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x715F ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x7160 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x7161 ->
      // INC H
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.H) (m.Flags())
      m.Regs.Set(R8.H, r)
      m.SetFlags f
    | 0x7162 ->
      // DJNZ 0x715E
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x7164 ->
      // POP DE
      m.Fetch()
      m.Regs.Set(R16.DE, m.Pop16())
    | 0x7165 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x7166 ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x7167 ->
      // SUB 0x08
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7169 ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x716A ->
      // INC L
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.L) (m.Flags())
      m.Regs.Set(R8.L, r)
      m.SetFlags f
    | 0x716B ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "WriteAsciiChars: unhandled resume at %04X" (m.Regs.Pc())

  let printLabel (m: Machine) =
    match m.Regs.Pc() with
    | 0x716C ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x716D ->
      // CALL 0x7308 — compute the target attr position
      m.Fetch()
      call m (m.ReadImm16())
    | 0x7170 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x7171 | 0x7183 | 0x7186 | 0x718F ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x7172 | 0x7181 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x7173 | 0x7178 | 0x7182 | 0x718E ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x7174 ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x7175 ->
      // CALL 0x720E — coordToAttr
      m.Fetch()
      call m (m.ReadImm16())
    | 0x7179 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x717A ->
      // BIT 7,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.A))
    | 0x717C ->
      // JR NZ,0x7183
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x717E | 0x718B ->
      // CALL 0x714C — draw the glyph
      m.Fetch()
      call m (m.ReadImm16())
    | 0x7184 ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x7185 ->
      // INC L
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.L) (m.Flags())
      m.Regs.Set(R8.L, r)
      m.SetFlags f
    | 0x7187 ->
      // JR 0x7178
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x7189 ->
      // AND 0x7F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7190 ->
      // LD (HL),A
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.A)
    | 0x7191 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "PrintLabel: unhandled resume at %04X" (m.Regs.Pc())

  let labelSetup (m: Machine) =
    match m.Regs.Pc() with
    | 0x7192 ->
      // LD HL,0x0018
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x7195 ->
      // LD DE,0x71AD — the "1UP" label
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x7198 | 0x71A1 ->
      // CALL 0x716C
      m.Fetch()
      call m (m.ReadImm16())
    | 0x719B ->
      // LD HL,0x0078
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x719E ->
      // LD DE,0x71B5 — the "EHI" label
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x71A4 ->
      // LD HL,0x00D8
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x71A7 ->
      // LD DE,0x71B1 — the "2UP" label
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x71AA ->
      // JP 0x716C
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "LabelSetup: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x7425 — JetmanVelIncY: advance the jetman's position, scan the
  // keyboard (up / fire), and integrate the vertical velocity.
  // ---------------------------------------------------------------------

  let jetmanVelIncY (m: Machine) =
    match m.Regs.Pc() with
    | 0x7425 ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x7426 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x7427 ->
      // LD (0x5DC7),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x742A ->
      // LD (IX+1),H
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x742D ->
      // LD A,(0x5CF3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x7430 ->
      // BIT 1,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 1) (m.Flags()) (m.Regs.Get R8.A))
    | 0x7432 ->
      // JP NZ,0x7473
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x7435 ->
      // LD B,0x02
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x7437 ->
      // LD A,0xEF
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x7439 ->
      // OUT (0xFB),A
      m.Fetch()
      let a = m.Regs.Get R8.A
      let port = (m.ReadImm() ||| (a <<< 8)) &&& 0xFFFF
      m.PassTime 4
      m.Out(port, a)
    | 0x743B ->
      // IN A,(0xFE)
      m.Fetch()
      let port = (m.ReadImm() ||| ((m.Regs.Get R8.A) <<< 8)) &&& 0xFFFF
      m.PassTime 4
      m.Regs.Set(R8.A, m.In port)
    | 0x743D ->
      // AND 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x743F ->
      // CP 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x7441 ->
      // JR NZ,0x746C
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x7443 ->
      // LD A,0xF7
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x7445 ->
      // DJNZ 0x7437
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x7447 ->
      // CALL 0x7393
      m.Fetch()
      call m (m.ReadImm16())
    | 0x744A ->
      // BIT 3,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 3) (m.Flags()) (m.Regs.Get R8.A))
    | 0x744C ->
      // JP NZ,0x750A
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x744F ->
      // RES 7,(IX+0)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 7))
    | 0x7453 ->
      // BIT 7,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
      m.SetFlags(Alu.bit lhs (1 <<< 7) (m.Flags()) busNoise)
    | 0x7457 ->
      // JP NZ,0x7515
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x745A ->
      // LD A,(0x5DCA)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x745D ->
      // NEG
      m.Fetch()
      m.Fetch()
      let struct (result, flags) = Alu.sub8 0 (m.Regs.Get R8.A) false
      m.Regs.Set(R8.A, result)
      m.SetFlags flags
    | 0x745F ->
      // ADD A,0x08
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7461 ->
      // ADD A,(IX+6)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + d) &&& 0xFFFF)
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7464 ->
      // CP 0x3F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x7466 ->
      // JR NC,0x7477
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().carry) then
        m.PassTime 5
        m.Branch offset
    | 0x7468 ->
      // LD (IX+6),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x746B | 0x7471 ->
      // JR 0x7490
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x746D ->
      // LD (IX+6),0x00
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 2
      let imm = m.ReadImm()
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, imm)
    | 0x7473 ->
      // CALL 0x733A
      m.Fetch()
      call m (m.ReadImm16())
    | 0x7476 ->
      // BIT 2,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 2) (m.Flags()) (m.Regs.Get R8.A))
    | 0x7478 ->
      // JP Z,0x746D
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().zero then m.Regs.SetPc target
    | 0x747B ->
      // JR 0x7447 — keep scanning the up key
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x747D ->
      // LD (IX+6),0x3F
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 2
      let imm = m.ReadImm()
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, imm)
    | 0x7481 ->
      // LD L,(IX+6)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.L, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x7484 ->
      // LD H,0x00
      m.Fetch()
      m.Regs.Set(R8.H, m.ReadImm())
    | 0x7486 | 0x7487 | 0x7488 ->
      // ADD HL,HL
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.HL) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x7489 ->
      // LD D,(IX+2)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.D, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x748C ->
      // LD A,(0x5DC8)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x748F ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x7490 ->
      // BIT 7,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
      m.SetFlags(Alu.bit lhs (1 <<< 7) (m.Flags()) busNoise)
    | 0x7494 ->
      // JP Z,0x755B
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().zero then m.Regs.SetPc target
    | 0x7497 ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x7498 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x7499 ->
      // LD (0x5DC8),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x749C ->
      // LD (IX+2),H
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x749F ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x74A0 ->
      // CP 0xC0
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x74A2 ->
      // JR NC,0x74F6
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().carry) then
        m.PassTime 5
        m.Branch offset
    | 0x74A4 ->
      // CP 0x2A
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x74A6 ->
      // JR C,0x74FA
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x74A8 ->
      // CALL 0x761D
      m.Fetch()
      call m (m.ReadImm16())
    | 0x74AB ->
      // BIT 2,E
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 2) (m.Flags()) (m.Regs.Get R8.E))
    | 0x74AD ->
      // JR Z,0x74C9
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x74AF ->
      // BIT 7,E
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.E))
    | 0x74B1 ->
      // JP NZ,0x74DC
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x74B4 ->
      // BIT 3,E
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 3) (m.Flags()) (m.Regs.Get R8.E))
    | 0x74B6 ->
      // JR NZ,0x74D6
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x74B8 ->
      // LD A,E
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.E)
    | 0x74B9 ->
      // XOR 0x40
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x74BB ->
      // AND 0x40
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x74BD ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x74BE ->
      // LD A,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x74C1 ->
      // AND 0xBF
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x74C3 ->
      // OR E
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.E)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x74C4 ->
      // LD (IX+4),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x74C7 | 0x74CA | 0x74CD ->
      // CALL
      m.Fetch()
      call m (m.ReadImm16())
    | 0x74D0 ->
      // BIT 4,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 4) (m.Flags()) (m.Regs.Get R8.A))
    | 0x74D2 ->
      // CALL Z,0x6F8D
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().zero then
        m.PassTime 1
        m.Push16(m.Regs.Pc())
        m.Regs.SetPc target
    | 0x74D5 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x74D6 ->
      // SET 7,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    | 0x74DA ->
      // JR 0x74C7
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x74DC ->
      // RES 7,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 7))
    | 0x74E0 ->
      // LD A,(IX+0)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x74E3 ->
      // AND 0xC0
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x74E5 ->
      // OR 0x02
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x74E7 ->
      // LD (IX+0),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x74EA ->
      // LD (IX+5),0x00
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 2
      let imm = m.ReadImm()
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, imm)
    | 0x74EE ->
      // LD (IX+6),0x00
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 2
      let imm = m.ReadImm()
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, imm)
    | 0x74F2 ->
      // JR 0x74C7
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x74F6 ->
      // INC B — the JR NC,0x74F6 lands on the d-byte of the RES; the
      // emulator decodes the live byte (0x04 = INC B)
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.B) (m.Flags())
      m.Regs.Set(R8.B, r)
      m.SetFlags f
    | 0x74F7 ->
      // CP (HL)
      m.Fetch()
      let rhs = m.Read(m.Regs.Get R16.HL)
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) rhs
      m.SetFlags f
    | 0x74F8 ->
      // JR 0x74A8
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x74FA ->
      // SET 7,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    | 0x74FE ->
      // LD A,(IX+6)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x7501 ->
      // SRL A
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftLogical8 (m.Regs.Get R8.A) Alu.Right
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7503 ->
      // JR Z,0x74A8
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x7505 ->
      // LD (IX+6),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x7508 ->
      // JR 0x74A8
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x750A ->
      // SET 7,(IX+0)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    | 0x750E ->
      // BIT 7,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
      m.SetFlags(Alu.bit lhs (1 <<< 7) (m.Flags()) busNoise)
    | 0x7512 ->
      // JP NZ,0x745A
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x7515 ->
      // LD A,(0x5DCA)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x7518 ->
      // SUB 0x08
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x751A ->
      // ADD A,(IX+6)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + d) &&& 0xFFFF)
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x751D ->
      // JP P,0x7468 (0xF2 — jump when the result is positive)
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().sign) then m.Regs.SetPc target
    | 0x7520 ->
      // LD (IX+6),0x00
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 2
      let imm = m.ReadImm()
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, imm)
    | 0x7524 ->
      // LD A,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x7527 ->
      // XOR 0x80
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7529 ->
      // LD (IX+4),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x752C ->
      // JP 0x7481
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x752F ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7530 ->
      // EX DE,HL
      m.Fetch()
      m.Regs.Ex(R16.DE, R16.HL)
    | 0x7531 | 0x755C ->
      // SBC HL,DE
      m.Fetch()
      m.Fetch()
      let struct (result, flags) = Alu.sbc16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags().carry)
      m.Regs.Set(R16.HL, result)
      m.SetFlags flags
      m.PassTime 7
    | 0x7533 ->
      // JP 0x7426
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x7536 ->
      // SET 6,(IX+0) — 0xF6 is SET 6 (the y field is 6), not SET 7
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs ||| (1 <<< 6))
    | 0x753A ->
      // BIT 6,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
      m.SetFlags(Alu.bit lhs (1 <<< 6) (m.Flags()) busNoise)
    | 0x753E ->
      // JP NZ,0x73F8
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x7541 ->
      // LD A,(0x5DCA)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x7544 ->
      // SUB 0x08
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7546 ->
      // ADD A,(IX+5)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + d) &&& 0xFFFF)
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7549 ->
      // JP P,0x7406 (0xF2)
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().sign) then m.Regs.SetPc target
    | 0x754C ->
      // LD (IX+5),0x00
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 2
      let imm = m.ReadImm()
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, imm)
    | 0x7550 ->
      // LD A,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x7553 ->
      // XOR 0x40
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7555 ->
      // LD (IX+4),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x7558 ->
      // JP 0x740F
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x755B ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | _ -> failwithf "JetmanVelIncY: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x7200 — NextSprite: walk the actor blocks.
  // 0x721F — prepare an actor for drawing.
  // 0x7237 — update and erase the actor's sprite.
  // ---------------------------------------------------------------------

  // ---------------------------------------------------------------------
  // 0x6235 — the title screen: wait for the fire key and flash the
  // selection cursor; 0x6254/0x625B toggle the cursor cells; 0x6262
  // draws the title and the menu items.
  // ---------------------------------------------------------------------

  let menuLoop (m: Machine) =
    match m.Regs.Pc() with
    | 0x6235 ->
      // LD A,(0x5CF3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6238 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6239 ->
      // BIT 0,C
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.C))
    | 0x623B ->
      // JR NZ,0x624A
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x623D | 0x6244 ->
      // CALL 0x6254 — flash the first cursor
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6240 ->
      // BIT 1,C
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 1) (m.Flags()) (m.Regs.Get R8.C))
    | 0x6242 ->
      // JR NZ,0x624F
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6247 ->
      // JP 0x6203 — proceed to the game
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x624A | 0x624F ->
      // CALL 0x625B — flash the second cursor
      m.Fetch()
      call m (m.ReadImm16())
    | 0x624D ->
      // JR 0x6240
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x6252 ->
      // JR 0x6247
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | _ -> failwithf "MenuLoop: unhandled resume at %04X" (m.Regs.Pc())

  let menuCursor (m: Machine) =
    match m.Regs.Pc() with
    | 0x6254 ->
      // SET 7,(HL)
      m.Fetch()
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    | 0x6256 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6257 ->
      // RES 7,(HL)
      m.Fetch()
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 7))
    | 0x6259 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x625A ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x625B ->
      // RES 7,(HL)
      m.Fetch()
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 7))
    | 0x625D ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x625E ->
      // SET 7,(HL)
      m.Fetch()
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    | 0x6260 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6261 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6262 ->
      // LD DE,0x628F — the cursor position table
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x6265 ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x6266 ->
      // LD HL,0x6295 — the item colour table
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6269 ->
      // LD DE,0x629B — the item text
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x626C ->
      // LD B,0x06 — six menu items
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x626E ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x626F ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x6270 ->
      // LD (0x5DD6),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6273 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x6274 ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x6275 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6276 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6277 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6278 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6279 ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x627A ->
      // LD L,0x30
      m.Fetch()
      m.Regs.Set(R8.L, m.ReadImm())
    | 0x627C ->
      // CALL 0x6301 — write the menu item
      m.Fetch()
      call m (m.ReadImm16())
    | 0x627F ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x6280 ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x6281 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6282 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x6283 ->
      // DJNZ 0x626E
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6285 ->
      // LD HL,0xB800 — the title attribute area
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6288 ->
      // LD DE,0x6040 — the title text
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x628B ->
      // CALL 0x716C — print the title
      m.Fetch()
      call m (m.ReadImm16())
    | 0x628E ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6301 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6302 ->
      // CALL 0x7308 — readInputLR
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6305 ->
      // LD A,(0x5DD6)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6308 ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x6309 ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x630A ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x630B ->
      // CALL 0x720E — coordToAttr
      m.Fetch()
      call m (m.ReadImm16())
    | 0x630E ->
      // JP 0x7178 — continue in the label printer loop
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "MenuCursor: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6EE2 — copy the actor data triplets; 0x683D — copy two bytes.
  // 0x684A+ — the explosion sound tail (beeper pulses via OUT (0xFE)).
  // ---------------------------------------------------------------------

  let actorDataCopy (m: Machine) =
    match m.Regs.Pc() with
    | 0x6EE2 ->
      // PUSH DE
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.DE)
    | 0x6EE3 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6EE4 ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x6EE5 | 0x6EE8 ->
      // POP DE
      m.Fetch()
      m.Regs.Set(R16.DE, m.Pop16())
    | 0x6EE6 | 0x6EEB ->
      // LD (HL),E
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.E)
    | 0x6EE7 | 0x6EEA | 0x6EEC ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6EE9 ->
      // LD (HL),D
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.D)
    | 0x6EED ->
      // DJNZ 0x6ED7
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6EEF ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ActorDataCopy: unhandled resume at %04X" (m.Regs.Pc())

  let sfxCopyTwo (m: Machine) =
    match m.Regs.Pc() with
    | 0x683D ->
      // ADD HL,BC
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x683E | 0x6842 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x683F | 0x6843 ->
      // LD (DE),A
      m.Fetch()
      m.Write(m.Regs.Get R16.DE, m.Regs.Get R8.A)
    | 0x6840 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6841 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x6844 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "SfxCopyTwo: unhandled resume at %04X" (m.Regs.Pc())

  let sfxTail (m: Machine) =
    match m.Regs.Pc() with
    | 0x684A ->
      // DEC (HL) — the game enters here at the 0x35 byte of the
      // DEC (IX+1) at 0x6849; the emulator decodes the live byte
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.PassTime 1
      let struct (r, f) = Alu.dec8 (m.Read(m.Regs.Wz())) (m.Flags())
      m.Write(m.Regs.Wz(), r)
      m.SetFlags f
    | 0x684C ->
      // JR Z,0x686E
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x684E ->
      // LD C,0x10
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x6850 ->
      // JR 0x685D
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x6852 ->
      // DEC (IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.PassTime 1
      let struct (r, f) = Alu.dec8 (m.Read(m.Regs.Wz())) (m.Flags())
      m.Write(m.Regs.Wz(), r)
      m.SetFlags f
    | 0x6855 ->
      // JR Z,0x686F
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x6857 ->
      // LD A,(IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x685A ->
      // ADD A,0x18
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x685C ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x685D ->
      // LD A,0x10
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x685F ->
      // OUT (0xFE),A — the beeper pulse
      m.Fetch()
      let a = m.Regs.Get R8.A
      let port = (m.ReadImm() ||| (a <<< 8)) &&& 0xFFFF
      m.PassTime 4
      m.Out(port, a)
    | 0x6861 ->
      // LD B,C (0x41)
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.C)
    | 0x6862 ->
      // DJNZ self — the delay loop
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6864 ->
      // XOR A (0xAF)
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | _ -> failwithf "SfxTail: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6E9D — shift the module bit pattern right B times.
  // 0x6EAF — rotate the accumulator bit into C.
  // 0x6EBA — read the module data and scale it up B bits.
  // ---------------------------------------------------------------------

  let rocketShift (m: Machine) =
    match m.Regs.Pc() with
    | 0x6E9D ->
      // LD C,(HL)
      m.Fetch()
      m.Regs.Set(R8.C, m.Read(m.Regs.Get R16.HL))
    | 0x6E9E | 0x6EA0 | 0x6EC9 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6E9F ->
      // LD D,(HL)
      m.Fetch()
      m.Regs.Set(R8.D, m.Read(m.Regs.Get R16.HL))
    | 0x6EA1 | 0x6ECA | 0x6EBA ->
      // LD A,B
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.B)
    | 0x6EA2 | 0x6ECB ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6EA3 | 0x6ECC ->
      // RET Z
      m.Fetch()
      m.PassTime 1
      if m.Flags().zero then m.Regs.SetPc(m.Pop16())
    | 0x6EA4 ->
      // SRL C
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftLogical8 (m.Regs.Get R8.C) Alu.Right
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6EA6 ->
      // RR D
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.D) Alu.Right (m.Flags().carry)
      m.Regs.Set(R8.D, r)
      m.SetFlags f
    | 0x6EA8 ->
      // RR E
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.E) Alu.Right (m.Flags().carry)
      m.Regs.Set(R8.E, r)
      m.SetFlags f
    | 0x6EAA ->
      // DJNZ 0x6EA4
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6EAC | 0x6ED5 | 0x6EBB ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x6EAD | 0x6ED6 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x6EAE | 0x6EB9 | 0x6ED7 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "RocketShift: unhandled resume at %04X" (m.Regs.Pc())

  let rocketRotate (m: Machine) =
    match m.Regs.Pc() with
    | 0x6EAF ->
      // PUSH BC — the rotate helper entry
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6EB0 ->
      // LD B,0x08
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x6EB2 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6EB3 ->
      // RL C
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.C) Alu.Left (m.Flags().carry)
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6EB5 ->
      // DJNZ 0x6EB2
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6EB7 ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6EB8 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6EB9 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6EBC ->
      // LD C,0x00
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x6EBE | 0x6EC4 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6EC2 ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x6EC8 ->
      // LD D,A
      m.Fetch()
      m.Regs.Set(R8.D, m.Regs.Get R8.A)
    | 0x6ECD ->
      // SLA E
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.E) Alu.Left
      m.Regs.Set(R8.E, r)
      m.SetFlags f
    | 0x6ECF ->
      // RL D
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.D) Alu.Left (m.Flags().carry)
      m.Regs.Set(R8.D, r)
      m.SetFlags f
    | 0x6ED1 ->
      // RL C
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.C) Alu.Left (m.Flags().carry)
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6ED3 ->
      // DJNZ 0x6ECD
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | _ -> failwithf "RocketRotate: unhandled resume at %04X" (m.Regs.Pc())

  let rocketReadScale (m: Machine) =
    match m.Regs.Pc() with
    | 0x6EBA ->
      // LD A,B
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.B)
    | 0x6EBB ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x6EBC ->
      // LD C,0x00
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x6EBE ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6EBF | 0x6EC5 ->
      // CALL 0x6EAF — the rotate helper
      m.Fetch()
      call m (m.ReadImm16())
    | 0x6EC2 ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x6EC3 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6EC4 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6EC8 ->
      // LD D,A
      m.Fetch()
      m.Regs.Set(R8.D, m.Regs.Get R8.A)
    | 0x6EC9 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6ECA ->
      // LD A,B
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.B)
    | 0x6ECB ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6ECC ->
      // RET Z
      m.Fetch()
      if m.Flags().zero then m.Regs.SetPc(m.Pop16())
    | 0x6ECD ->
      // SLA E
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.E) Alu.Left
      m.Regs.Set(R8.E, r)
      m.SetFlags f
    | 0x6ECF ->
      // RL D
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.D) Alu.Left (m.Flags().carry)
      m.Regs.Set(R8.D, r)
      m.SetFlags f
    | 0x6ED1 ->
      // RL C
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.C) Alu.Left (m.Flags().carry)
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6ED3 ->
      // DJNZ 0x6ECD
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6ED5 ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x6ED6 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x6ED7 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "RocketReadScale: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // 0x6A37 — the actor colour setup; 0x68CA — the actor data lookup;
  // 0x6E27 — the collision-distance check; 0x71DD — the attr-address
  // variant; 0x6F7C — the rocket writer loop tail.
  // ---------------------------------------------------------------------

  let actorColour (m: Machine) =
    match m.Regs.Pc() with
    | 0x6A37 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6A38 ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6A39 | 0x6A3A | 0x6A3B ->
      // RRA
      m.Fetch()
      let struct (r, f) = Alu.fastRotate8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6A3C ->
      // AND 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6A3E | 0x6A3F ->
      // INC A
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6A40 ->
      // LD (IX+3),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x6A43 ->
      // AND 0x01
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6A45 ->
      // LD (IX+6),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | _ -> failwithf "ActorColour: unhandled resume at %04X" (m.Regs.Pc())

  let actorLookup (m: Machine) =
    match m.Regs.Pc() with
    | 0x68CA ->
      // LD E,(HL)
      m.Fetch()
      m.Regs.Set(R8.E, m.Read(m.Regs.Get R16.HL))
    | 0x68CB ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x68CC ->
      // LD D,(HL)
      m.Fetch()
      m.Regs.Set(R8.D, m.Read(m.Regs.Get R16.HL))
    | 0x68CD ->
      // LD L,(IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.L, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x68D0 ->
      // LD H,(IX+2)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.H, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x68D3 ->
      // CP 0x06
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x68D5 ->
      // JR NC,0x68EB
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().carry) then
        m.PassTime 5
        m.Branch offset
    | 0x68D7 ->
      // CP 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x68D9 ->
      // JR NC,0x6906
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().carry) then
        m.PassTime 5
        m.Branch offset
    | 0x68DB ->
      // DEC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL - 1) &&& 0xFFFF)
    | 0x68DC ->
      // CALL 0x729B
      m.Fetch()
      call m (m.ReadImm16())
    | 0x68DF ->
      // LD A,(0x5DCE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x68E2 ->
      // AND 0x07
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x68E4 ->
      // OR 0x42
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x68E6 ->
      // LD (IX+3),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x68E9 ->
      // JP 0x71CF
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | _ -> failwithf "ActorLookup: unhandled resume at %04X" (m.Regs.Pc())

  let collisionCheck (m: Machine) =
    match m.Regs.Pc() with
    | 0x6E27 ->
      // DEC A
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E28 ->
      // RET NZ
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().zero) then m.Regs.SetPc(m.Pop16())
    | 0x6E29 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6E2A ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6E2B ->
      // SUB (IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + d) &&& 0xFFFF)
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E2E ->
      // JP P,0x6E33
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().sign) then m.Regs.SetPc target
    | 0x6E31 ->
      // NEG
      m.Fetch()
      m.Fetch()
      let struct (result, flags) = Alu.sub8 0 (m.Regs.Get R8.A) false
      m.Regs.Set(R8.A, result)
      m.SetFlags flags
    | 0x6E33 ->
      // CP 0x0C
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x6E35 ->
      // RET NC
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().carry) then m.Regs.SetPc(m.Pop16())
    | 0x6E36 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6E37 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6E38 ->
      // SUB (IX+2)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + d) &&& 0xFFFF)
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | _ -> failwithf "CollisionCheck: unhandled resume at %04X" (m.Regs.Pc())

  let attrCoord (m: Machine) =
    match m.Regs.Pc() with
    | 0x71DD | 0x71DE ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71DF ->
      // INC A
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71E0 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71E1 ->
      // AND 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71E3 ->
      // INC A
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71E4 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x71E5 ->
      // LD D,(IX+3)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.D, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x71E8 ->
      // LD E,B
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.B)
    | 0x71E9 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x71EA ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x71EB ->
      // CP 0x5B
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x71ED ->
      // JR NC,0x7200
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().carry) then
        m.PassTime 5
        m.Branch offset
    | 0x71EF ->
      // CP 0x58
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x71F1 ->
      // JR C,0x71E0
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x71F3 ->
      // LD (HL),D
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.D)
    | _ -> failwithf "AttrCoord: unhandled resume at %04X" (m.Regs.Pc())

  // ---------------------------------------------------------------------
  // Small helpers: 0x6143/0x6921 — zero/scan loops; 0x6E56 — the
  // actor-data probe; 0x6E98 — return; 0x7414 — scale by 8;
  // 0x60CD — EI+RET; 0x61F3 — fill loop; 0x6313 — reset the level.
  // ---------------------------------------------------------------------

  let zeroLoop (m: Machine) =
    match m.Regs.Pc() with
    | 0x6143 | 0x6921 ->
      // DEC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL - 1) &&& 0xFFFF)
    | 0x6144 ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x6145 ->
      // OR L
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6146 ->
      // JR NZ,0x6143
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6148 ->
      // DJNZ 0x6143
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x614A | 0x6926 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6922 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x6923 ->
      // OR H
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.H)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6924 ->
      // JR NZ,0x6921
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | _ -> failwithf "ZeroLoop: unhandled resume at %04X" (m.Regs.Pc())

  let actorProbe (m: Machine) =
    match m.Regs.Pc() with
    | 0x6E56 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6E57 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6E58 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E59 ->
      // JR Z,0x6E91
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x6E5B | 0x6E5C | 0x6E5D ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x6E5E ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6E5F ->
      // DEC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL - 1) &&& 0xFFFF)
    | _ -> failwithf "ActorProbe: unhandled resume at %04X" (m.Regs.Pc())

  let scale8 (m: Machine) =
    match m.Regs.Pc() with
    | 0x7414 | 0x7415 | 0x7416 ->
      // ADD HL,HL
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.HL) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x7417 ->
      // LD D,(IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.D, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | _ -> failwithf "Scale8: unhandled resume at %04X" (m.Regs.Pc())

  let smallBits (m: Machine) =
    match m.Regs.Pc() with
    | 0x60CD ->
      // EI
      m.Fetch()
      m.Iff1 <- true
      m.Iff2 <- true
    | 0x60CE ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6E98 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6E99 ->
      // LD A,B
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.B)
    | 0x6E9A ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x6E9B ->
      // LD E,0x00
      m.Fetch()
      m.Regs.Set(R8.E, m.ReadImm())
    | 0x61F3 ->
      // LD (HL),C
      m.Fetch()
      m.Write(m.Regs.Get R16.HL, m.Regs.Get R8.C)
    | 0x61F4 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x61F5 ->
      // DJNZ 0x61F3
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x61F7 ->
      // DI
      m.Fetch()
      m.Iff1 <- false
      m.Iff2 <- false
    | 0x6313 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x6314 ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6315 ->
      // LD (0x5DF0),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | _ -> failwithf "SmallBits: unhandled resume at %04X" (m.Regs.Pc())

  let stringCompare (m: Machine) =
    match m.Regs.Pc() with
    | 0x63BD ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x63BE ->
      // CP (HL)
      m.Fetch()
      let rhs = m.Read(m.Regs.Get R16.HL)
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) rhs
      m.SetFlags f
    | 0x63BF ->
      // JR C,0x63C9
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x63C1 ->
      // JR NZ,0x63C7
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x63C3 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x63C4 ->
      // INC DE
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
    | 0x63C5 ->
      // DJNZ 0x63BD
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | _ -> failwithf "StringCompare: unhandled resume at %04X" (m.Regs.Pc())

  let deScan (m: Machine) =
    match m.Regs.Pc() with
    | 0x65EF | 0x6640 ->
      // LD A,(DE)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    | 0x65F0 | 0x6641 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x65F1 | 0x6642 ->
      // RET NZ
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().zero) then m.Regs.SetPc(m.Pop16())
    | 0x65F2 ->
      // LD A,(0x5DCC)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x65F5 ->
      // AND 0x7F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6643 ->
      // LD A,(0x5D35)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6646 ->
      // CP 0x06
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | _ -> failwithf "DeScan: unhandled resume at %04X" (m.Regs.Pc())

  let colourShift (m: Machine) =
    match m.Regs.Pc() with
    | 0x6742 | 0x6743 ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6744 ->
      // AND 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6746 ->
      // OR C
      m.Fetch()
      let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6747 ->
      // SLA C
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.C) Alu.Left
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x6A2A ->
      // LD (IX+0),A
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.A)
    | 0x6A2D ->
      // LD A,E
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.E)
    | 0x6A2E ->
      // AND 0x7F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6A30 ->
      // ADD A,0x28
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | _ -> failwithf "ColourShift: unhandled resume at %04X" (m.Regs.Pc())

  let fragBits (m: Machine) =
    match m.Regs.Pc() with
    | 0x691C | 0x6DFF ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x691D ->
      // RET NZ
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().zero) then m.Regs.SetPc(m.Pop16())
    | 0x6DFE ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6E00 ->
      // JR NZ,0x6E0A
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6E02 ->
      // CALL 0x6E1C
      m.Fetch()
      call m (m.ReadImm16())
    | 0x691E ->
      // LD HL,0x00C0
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x692D | 0x692E ->
      // RLCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x692F ->
      // AND 0x1C
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6931 ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x6932 ->
      // LD D,0x00
      m.Fetch()
      m.Regs.Set(R8.D, m.ReadImm())
    | 0x6934 | 0x6E93 | 0x6A09 ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x6935 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6936 ->
      // LD DE,0x5E00
      m.Fetch()
      m.Regs.Set(R16.DE, m.ReadImm16())
    | 0x69D2 ->
      // ADD A,(HL)
      m.Fetch()
      let rhs = m.Read(m.Regs.Get R16.HL)
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x69D3 ->
      // ADD A,C
      m.Fetch()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.C) false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x69D4 ->
      // LD (0x5DCE),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6A04 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6A05 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6A06 ->
      // JP Z,0x6A15
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().zero then m.Regs.SetPc target
    | 0x6A15 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6A16 ->
      // EX DE,HL
      m.Fetch()
      m.Regs.Ex(R16.DE, R16.HL)
    | 0x6A17 ->
      // LD HL,0x6DC7
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x6E49 ->
      // CP D
      m.Fetch()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.D)
      m.SetFlags f
    | 0x6E4A ->
      // RET NC
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().carry) then m.Regs.SetPc(m.Pop16())
    | 0x6E4B ->
      // LD E,0x01
      m.Fetch()
      m.Regs.Set(R8.E, m.ReadImm())
    | 0x6E92 ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x6E94 ->
      // DJNZ 0x6E56
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6E96 ->
      // LD C,0x00
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x7624 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x7625 | 0x7631 | 0x7632 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x7626 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x7627 ->
      // SUB (IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      let rhs = m.Read((m.Regs.Ix() + d) &&& 0xFFFF)
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) rhs false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7633 ->
      // CP (HL)
      m.Fetch()
      let rhs = m.Read(m.Regs.Get R16.HL)
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) rhs
      m.SetFlags f
    | 0x7634 ->
      // JP NC,0x7665
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().carry) then m.Regs.SetPc target
    | _ -> failwithf "FragBits: unhandled resume at %04X" (m.Regs.Pc())

  let fragBits2 (m: Machine) =
    match m.Regs.Pc() with
    | 0x70C3 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x70C4 ->
      // PUSH DE
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.DE)
    | 0x70C5 ->
      // JP 0x715C
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x71F4 ->
      // INC L
      m.Fetch()
      let struct (r, f) = Alu.inc8 (m.Regs.Get R8.L) (m.Flags())
      m.Regs.Set(R8.L, r)
      m.SetFlags f
    | 0x71F5 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x71F6 ->
      // AND 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x73B5 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x73B6 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x73B7 ->
      // JR Z,0x73D3
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x60FB ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x60FC ->
      // JP Z,0x6157
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().zero then m.Regs.SetPc target
    | 0x615D ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x615E ->
      // JR NZ,0x6166
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6209 ->
      // LD D,A
      m.Fetch()
      m.Regs.Set(R8.D, m.Regs.Get R8.A)
    | 0x620A ->
      // LD A,0xF7
      m.Fetch()
      m.Regs.Set(R8.A, m.ReadImm())
    | 0x6210 ->
      // CPL
      m.Fetch()
      let struct (r, f) = Alu.cpl (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6211 ->
      // BIT 0,A
      m.Fetch()
      m.Fetch()
      m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.A))
    | 0x622E ->
      // LD A,D
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.D)
    | 0x622F ->
      // LD (0x5CF3),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x6323 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x6324 ->
      // DJNZ 0x6312
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x6330 ->
      // RET NZ
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().zero) then m.Regs.SetPc(m.Pop16())
    | 0x6331 ->
      // LD (0x5DF9),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x63C7 | 0x63C9 ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x63C8 | 0x63D2 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x65E2 ->
      // RET Z
      m.Fetch()
      m.PassTime 1
      if m.Flags().zero then m.Regs.SetPc(m.Pop16())
    | 0x65E3 ->
      // CP 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x65E5 ->
      // RET NC
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().carry) then m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "FragBits2: unhandled resume at %04X" (m.Regs.Pc())

  let finalFrags (m: Machine) =
    match m.Regs.Pc() with
    | 0x6633 ->
      // RET Z
      m.Fetch()
      m.PassTime 1
      if m.Flags().zero then m.Regs.SetPc(m.Pop16())
    | 0x6634 ->
      // CP 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x69B3 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6636 ->
      // RET NC
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().carry) then m.Regs.SetPc(m.Pop16())
    | 0x6637 ->
      // LD HL,0x3030
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
    | 0x670B ->
      // DEC E
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.E) (m.Flags())
      m.Regs.Set(R8.E, r)
      m.SetFlags f
    | 0x670C ->
      // JR NZ,0x6730
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6737 ->
      // PUSH HL
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.HL)
    | 0x6738 ->
      // LD A,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x673B ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x673C ->
      // LD C,0x00
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
    | 0x673E ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x674F ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x673F ->
      // LD A,(0x5DF0)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6750 ->
      // LD A,(IX+2)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x6753 ->
      // SUB 0x10
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6760 ->
      // LD A,C
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.C)
    | 0x6761 ->
      // ADD A,0x04
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6763 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6764 | 0x67BE ->
      // DJNZ
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x676B ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x676C ->
      // LD (0x5DC3),A
      m.Fetch()
      m.Write(m.ReadImm16(), m.Regs.Get R8.A)
    | 0x676F | 0x693C | 0x69AF ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x6770 ->
      // LD (IX+2),H
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Write((m.Regs.Ix() + d) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x677B ->
      // LD A,B
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.B)
    | 0x677C ->
      // CP 0x06
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x67AF ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x67B0 ->
      // CALL 0x71CF
      m.Fetch()
      call m (m.ReadImm16())
    | 0x67B3 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x67B4 ->
      // LD HL,(0x5DCF)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.L, m.Read addr)
      m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    | 0x67B7 ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x67B8 ->
      // SUB 0x08
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x67BA ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x67BB ->
      // LD (0x5DCF),HL
      m.Fetch()
      let addr = m.ReadImm16()
      m.Write(addr, m.Regs.Get R8.L)
      m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x6832 ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | 0x6833 ->
      // SLA C
      m.Fetch()
      m.Fetch()
      let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.C) Alu.Left
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x68A8 | 0x6906 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x68B2 ->
      // INC (HL)
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.PassTime 1
      let v = m.Read(m.Regs.Wz())
      let struct (r, f) = Alu.inc8 v (m.Flags())
      m.Write(m.Regs.Wz(), r)
      m.SetFlags f
    | 0x68BC ->
      // AND B
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.B)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x68BD ->
      // JR NZ,0x68C2
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6907 ->
      // CALL 0x722A
      m.Fetch()
      call m (m.ReadImm16())
    | 0x69A7 ->
      // INC (HL)
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.PassTime 1
      let v = m.Read(m.Regs.Wz())
      let struct (r, f) = Alu.inc8 v (m.Flags())
      m.Write(m.Regs.Wz(), r)
      m.SetFlags f
    | 0x69B0 ->
      // LD BC,0x5D88
      m.Fetch()
      m.Regs.Set(R16.BC, m.ReadImm16())
    | 0x69B4 ->
      // SBC HL,BC
      m.Fetch()
      m.Fetch()
      let struct (result, flags) = Alu.sbc16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags().carry)
      m.Regs.Set(R16.HL, result)
      m.SetFlags flags
      m.PassTime 7
    | 0x69C6 ->
      // INC HL
      m.Fetch(); m.PassTime 2
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
    | 0x69C7 ->
      // LD (0x5DCC),HL
      m.Fetch()
      let addr = m.ReadImm16()
      m.Write(addr, m.Regs.Get R8.L)
      m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.H)
    | 0x69CE ->
      // LD C,A
      m.Fetch()
      m.Regs.Set(R8.C, m.Regs.Get R8.A)
    | _ -> failwithf "FinalFrags: unhandled resume at %04X" (m.Regs.Pc())

  let finalFrags2 (m: Machine) =
    match m.Regs.Pc() with
    | 0x69D7 | 0x69DF ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x69D8 ->
      // LD A,(0x5DCB)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x69DB ->
      // CP 0x03
      m.Fetch()
      let imm = m.ReadImm()
      let struct (_, f) = Alu.cmp8 (m.Regs.Get R8.A) imm
      m.SetFlags f
    | 0x69DD ->
      // JR C,0x69EB
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x69E0 ->
      // AND 0x1F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x69E2 ->
      // JR NZ,0x6A0C
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x69EE | 0x70A9 | 0x70B8 | 0x70E2 | 0x70F0 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x69EF ->
      // JR NZ,0x6A06
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x69F6 | 0x69F9 ->
      // DEC A
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x69F7 ->
      // JR Z,0x69FB
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x69FA ->
      // JR NZ,0x6A0C
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x6A24 | 0x6A50 ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x6A53 ->
      // ADD HL,DE
      m.Fetch()
      m.PassTime 7
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
    | 0x6DD5 ->
      // INC (HL)
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.PassTime 1
      let v = m.Read(m.Regs.Wz())
      let struct (r, f) = Alu.inc8 v (m.Flags())
      m.Write(m.Regs.Wz(), r)
      m.SetFlags f
    | 0x6DD6 ->
      // LD A,(IX+1)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x6E05 ->
      // DEC E
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.E) (m.Flags())
      m.Regs.Set(R8.E, r)
      m.SetFlags f
    | 0x6E06 ->
      // JP Z,0x648B
      m.Fetch()
      let target = m.ReadImm16()
      if m.Flags().zero then m.Regs.SetPc target
    | 0x6E13 ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E14 ->
      // JP 0x6832
      m.Fetch()
      m.Regs.SetPc(m.ReadImm16())
    | 0x6E21 ->
      // LD A,(HL)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.HL))
    | 0x6E22 ->
      // AND 0x3F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E24 ->
      // DEC A
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E25 ->
      // JR Z,0x6E29
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x6E27 ->
      // DEC A
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.A) (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6E28 ->
      // RET NZ
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().zero) then m.Regs.SetPc(m.Pop16())
    | 0x6E4D | 0x70E8 | 0x70EC ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x6ED8 ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x6ED9 ->
      // LD A,(0x5DD2)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x6EDC ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6EDD ->
      // JR Z,0x6EF0
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x6F2A ->
      // XOR A
      m.Fetch()
      let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x6F2B ->
      // JR 0x6F53
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | 0x70AA ->
      // JR Z,0x70C6
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x70B9 ->
      // JR Z,0x70CD
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | 0x70E3 ->
      // JR NZ,0x70E9
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x70ED ->
      // LD A,(0x5DD1)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x70F1 ->
      // JR Z,0x70E9
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().zero then
        m.PassTime 5
        m.Branch offset
    | _ -> failwithf "FinalFrags2: unhandled resume at %04X" (m.Regs.Pc())

  let fragBits3 (m: Machine) =
    match m.Regs.Pc() with
    | 0x71CF ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x7610 | 0x7613 | 0x7606 ->
      // EX AF,AF'
      m.Fetch()
      m.Regs.Ex(R16.AF, R16.AF_)
    | 0x71D0 ->
      // LD HL,(0x5DCF)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.L, m.Read addr)
      m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    | 0x71D9 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x71DA ->
      // LD A,(0x5DC3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x71FA ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x71FB ->
      // SUB 0x20
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7301 ->
      // LD H,A
      m.Fetch()
      m.Regs.Set(R8.H, m.Regs.Get R8.A)
    | 0x7302 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x7303 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x71FD ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x71FE ->
      // DJNZ 0x71E6
      m.Fetch()
      m.PassTime 1
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      let nb = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, nb)
      if nb <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x7373 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | 0x7374 ->
      // LD A,(0x5CF3)
      m.Fetch()
      m.Regs.Set(R8.A, m.Read(m.ReadImm16()))
    | 0x73B9 ->
      // DEC (HL)
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.PassTime 1
      let v = m.Read(m.Regs.Wz())
      let struct (r, f) = Alu.dec8 v (m.Flags())
      m.Write(m.Regs.Wz(), r)
      m.SetFlags f
    | 0x73BA ->
      // JP NZ,0x61BF
      m.Fetch()
      let target = m.ReadImm16()
      if not (m.Flags().zero) then m.Regs.SetPc target
    | 0x73C3 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7607 ->
      // LD A,(IX+0)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.Set(R8.A, m.Read((m.Regs.Ix() + d) &&& 0xFFFF))
    | 0x760A ->
      // AND 0x3F
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) imm
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x73C4 ->
      // JR NZ,0x73CE
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x741D ->
      // LD E,A
      m.Fetch()
      m.Regs.Set(R8.E, m.Regs.Get R8.A)
    | 0x741E ->
      // BIT 6,(IX+4)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 1
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Fetch()
      let lhs = m.Read(m.Regs.Wz())
      m.PassTime 1
      let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
      m.SetFlags(Alu.bit lhs (1 <<< 6) (m.Flags()) busNoise)
    | 0x7614 ->
      // SUB 0x09
      m.Fetch()
      let imm = m.ReadImm()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) imm false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | _ -> failwithf "FragBits3: unhandled resume at %04X" (m.Regs.Pc())

  /// Override hook: maps converted addresses to their semantic routines.
  let hook (addr: int) : (Machine -> unit) option =
    if addr = 0x61E7 then Some entryWait
    elif addr = 0x61ED then Some setupAndRun
    elif addr = 0x71CF || addr = 0x71D0 || addr = 0x71D9 || addr = 0x71DA || addr = 0x71FA || addr = 0x71FB || addr = 0x71FD || addr = 0x71FE || addr = 0x7301 || addr = 0x7302 || addr = 0x7303 || addr = 0x7373 || addr = 0x7374 || addr = 0x73B9 || addr = 0x73BA || addr = 0x73C3 || addr = 0x73C4 || addr = 0x741D || addr = 0x741E || addr = 0x7606 || addr = 0x7607 || addr = 0x760A || addr = 0x7610 || addr = 0x7613 || addr = 0x7614 then Some fragBits3
    elif addr = 0x69D7 || addr = 0x69D8 || addr = 0x69DB || addr = 0x69DD || addr = 0x69DF || addr = 0x69E0 || addr = 0x69E2 || addr = 0x69EE || addr = 0x69EF || addr = 0x69F6 || addr = 0x69F7 || addr = 0x69F9 || addr = 0x69FA || addr = 0x6DD5 || addr = 0x6DD6 || addr = 0x6E05 || addr = 0x6E06 || addr = 0x6E13 || addr = 0x6E14 || addr = 0x6E21 || addr = 0x6E22 || addr = 0x6E24 || addr = 0x6E25 || addr = 0x6E27 || addr = 0x6E28 || addr = 0x6E4D || addr = 0x6ED8 || addr = 0x6ED9 || addr = 0x6EDC || addr = 0x6EDD || addr = 0x6F2A || addr = 0x6F2B || addr = 0x70A9 || addr = 0x70AA || addr = 0x70B8 || addr = 0x70B9 || addr = 0x70E2 || addr = 0x70E3 || addr = 0x70E8 || addr = 0x70EC || addr = 0x70ED || addr = 0x70F0 || addr = 0x70F1 then Some finalFrags2
    elif addr = 0x6633 || addr = 0x6634 || addr = 0x6636 || addr = 0x6637 || addr = 0x670B || addr = 0x670C || addr = 0x6737 || addr = 0x6738 || addr = 0x673B || addr = 0x673C || addr = 0x673E || addr = 0x673F || addr = 0x674F || addr = 0x6750 || addr = 0x6753 || addr = 0x6760 || addr = 0x6761 || addr = 0x6763 || addr = 0x6764 || addr = 0x676B || addr = 0x676C || addr = 0x676F || addr = 0x6770 || addr = 0x677B || addr = 0x677C || addr = 0x67AF || addr = 0x67B0 || addr = 0x67B3 || addr = 0x67B4 || addr = 0x67B7 || addr = 0x67B8 || addr = 0x67BA || addr = 0x67BB || addr = 0x67BE || addr = 0x6832 || addr = 0x6833 || addr = 0x68A8 || addr = 0x68B2 || addr = 0x68BC || addr = 0x68BD || addr = 0x6906 || addr = 0x6907 || addr = 0x693C || addr = 0x69A7 || addr = 0x69AF || addr = 0x69B0 || addr = 0x69B3 || addr = 0x69B4 || addr = 0x69C6 || addr = 0x69C7 || addr = 0x69CE then Some finalFrags
    elif addr >= 0x70C3 && addr <= 0x70C5 then Some fragBits2
    elif addr >= 0x71F4 && addr <= 0x71F6 then Some fragBits2
    elif addr >= 0x73B5 && addr <= 0x73B7 then Some fragBits2
    elif addr = 0x60FB || addr = 0x60FC || addr = 0x615D || addr = 0x615E || addr = 0x6209 || addr = 0x620A || addr = 0x6210 || addr = 0x6211 || addr = 0x622E || addr = 0x622F || addr = 0x6323 || addr = 0x6324 || addr = 0x6330 || addr = 0x6331 || addr = 0x63C7 || addr = 0x63C8 || addr = 0x63C9 || addr = 0x63D2 || addr = 0x65E2 || addr = 0x65E3 || addr = 0x65E5 then Some fragBits2
    elif addr >= 0x691C && addr <= 0x691E then Some fragBits
    elif addr >= 0x692D && addr <= 0x6936 then Some fragBits
    elif addr >= 0x69D2 && addr <= 0x69D4 then Some fragBits
    elif addr >= 0x6A04 && addr <= 0x6A09 then Some fragBits
    elif addr >= 0x6A15 && addr <= 0x6A17 then Some fragBits
    elif addr = 0x6DFE || addr = 0x6DFF || addr = 0x6E00 || addr = 0x6E02 || addr = 0x6E49 || addr = 0x6E4A || addr = 0x6E4B || addr = 0x6E92 || addr = 0x6E94 || addr = 0x6E96 then Some fragBits
    elif addr >= 0x7624 && addr <= 0x7627 then Some fragBits
    elif addr >= 0x7631 && addr <= 0x7634 then Some fragBits
    elif addr >= 0x63BD && addr <= 0x63C5 then Some stringCompare
    elif addr = 0x65EF || addr = 0x65F0 || addr = 0x65F1 || addr = 0x65F2 || addr = 0x65F5 || addr = 0x6640 || addr = 0x6641 || addr = 0x6642 || addr = 0x6643 || addr = 0x6646 then Some deScan
    elif addr >= 0x6742 && addr <= 0x6747 then Some colourShift
    elif addr >= 0x6A2A && addr <= 0x6A30 then Some colourShift
    elif addr >= 0x6143 && addr <= 0x614A then Some zeroLoop
    elif addr >= 0x6921 && addr <= 0x6926 then Some zeroLoop
    elif addr >= 0x6E56 && addr <= 0x6E5F then Some actorProbe
    elif addr >= 0x7414 && addr <= 0x7417 then Some scale8
    elif addr = 0x60CD || addr = 0x60CE || addr = 0x6E98 || addr = 0x6E99 || addr = 0x6E9A || addr = 0x6E9B || addr = 0x61F3 || addr = 0x61F4 || addr = 0x61F5 || addr = 0x61F7 || addr = 0x6313 || addr = 0x6314 || addr = 0x6315 then Some smallBits
    elif addr >= 0x6A37 && addr <= 0x6A45 then Some actorColour
    elif addr >= 0x6E27 && addr <= 0x6E38 then Some collisionCheck
    elif addr >= 0x71DD && addr <= 0x71F3 then Some attrCoord
    elif addr >= 0x6E9D && addr <= 0x6EAE then Some rocketShift
    elif addr >= 0x6EAF && addr <= 0x6EB9 then Some rocketRotate
    elif addr >= 0x6EE2 && addr <= 0x6EEF then Some actorDataCopy
    elif addr >= 0x683D && addr <= 0x6844 then Some sfxCopyTwo
    elif addr >= 0x684A && addr <= 0x6864 then Some sfxTail
    elif addr >= 0x6235 && addr <= 0x6252 then Some menuLoop
    elif addr >= 0x6254 && addr <= 0x630E then Some menuCursor
    elif addr >= 0x61BF && addr <= 0x61E5 then Some startGame
    elif addr >= 0x6174 && addr <= 0x6188 then Some playerInit
    elif addr >= 0x619C && addr <= 0x61BC then Some resetPlayerState
    elif addr = 0x6334 then Some performJump
    elif addr >= 0x6523 && addr <= 0x6534 then Some collectRocketItem
    elif addr >= 0x7425 && addr <= 0x755B then Some jetmanVelIncY
    elif addr >= 0x7134 && addr <= 0x714B then Some displayString
    elif addr >= 0x714C && addr <= 0x716B then Some writeAsciiChars
    elif addr >= 0x716C && addr <= 0x7191 then Some printLabel
    elif addr >= 0x7192 && addr <= 0x71AC then Some labelSetup
    elif addr >= 0x7308 && addr <= 0x7326 then Some readInputLR
    elif addr >= 0x7327 && addr <= 0x7339 then Some copyJoystickState
    elif addr >= 0x733A && addr <= 0x733C then Some readInterface2
    elif addr >= 0x6344 && addr <= 0x635E then Some mainFlow
    elif addr >= 0x6361 && addr <= 0x636F then Some updateHiScore
    elif addr >= 0x6396 && addr <= 0x63A4 then Some scoreCompare
    elif addr >= 0x63A6 && addr <= 0x63B7 then Some hiScoreUpdate
    elif addr >= 0x64E3 && addr <= 0x64F0 then Some itemUpdate
    elif addr >= 0x665E && addr <= 0x6666 then Some moduleReset
    elif addr >= 0x6963 && addr <= 0x6983 then Some frameWork
    elif addr >= 0x6987 && addr <= 0x6998 then Some dispatchRestore
    elif addr >= 0x6999 && addr <= 0x69A3 then Some actorUpdateTail
    elif addr >= 0x60DF && addr <= 0x60F6 then Some rocketSetup
    elif addr >= 0x653D && addr <= 0x65A7 then Some jetmanInteract
    elif addr >= 0x65A8 && addr <= 0x65DB then Some jetmanCollect
    elif addr >= 0x6EF5 && addr <= 0x6F21 then Some rocketUpdate
    elif addr >= 0x6F3E && addr <= 0x6F8C then Some rocketWriter
    elif addr >= 0x685D && addr <= 0x686D then Some actorClick
    elif addr >= 0x6873 && addr <= 0x6891 then Some actorInit
    elif addr >= 0x712C && addr <= 0x714B then Some scoreDisplay
    elif addr >= 0x720E && addr <= 0x7223 then Some coordToAttr
    elif addr >= 0x60CF && addr < 0x60DF then Some copyRocketData
    elif addr >= 0x60A9 && addr < 0x60CF then Some frameStarter
    elif addr >= 0x608A && addr <= 0x60A6 then Some mainLoop
    elif addr = 0x67FB || addr = 0x6801 then Some sfxExplosionParams
    elif addr >= 0x6807 && addr <= 0x680F then Some sfxExplosionHi
    elif addr >= 0x6810 && addr <= 0x681B then Some explosionLoop
    elif addr >= 0x681C && addr <= 0x6831 then Some sfxPlayExplosion
    elif addr = 0x71B8 || addr = 0x71C6 then Some screenClearEntry
    elif addr >= 0x71BF && addr <= 0x71C5 then Some screenClearLoop
    else None
