namespace Jetpac3.Core

/// The converted `screenClear` routine (0x71B8 / 0x71C6): idiomatic F# replacing
/// the generated instruction table for this one routine. The implementation is
/// instruction-granular (PC-dispatched per instruction) so an interrupt can land
/// mid-clear at the exact cycle, matching the oracle; a single-shot whole-loop
/// function would delay the interrupt past the routine and diverge.
/// Registry metadata for a lifted routine. Execution still mutates the shared
/// Jetpac2 machine instance; no shadow state is introduced.
type LiftedRoutine =
  { Name: string
    EntryAddresses: int list
    Execute: Jetpac2.Core.Machine -> unit
    ImplementationMode: ExecutionMode }

module LiftedRoutines =

  open Jetpac2.Core

  /// The attribute written to every screen cell each frame: ink 7 (white) on
  /// paper 0 (black), bright. Original value 0x47.
  let ClearAttrColour = 0x47

  /// 0x71BF-0x71C5 — the shared clear loop: LD (HL),C; INC HL; LD A,H; CP B;
  /// JR C; RET.
  let clearLoop (m: Machine) =
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

  /// 0x71B8 — clear the pixel area (0x4000-0x57FF with 0x00).
  let clearPixels (m: Machine) =
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
    | _ -> failwithf "ClearPixels: unhandled resume at %04X" (m.Regs.Pc())

  /// 0x71C6 — clear the attribute area (0x5800-0x5AFF with ClearAttrColour).
  let clearAttrs (m: Machine) =
    match m.Regs.Pc() with
    | 0x71C6 ->
      // LD HL,0x5800
      m.Fetch()
      m.Regs.Set(R16.HL, m.ReadImm16())
      // LD B,0x5B
      m.Fetch()
      m.Regs.Set(R8.B, m.ReadImm())
      // LD C,ClearAttrColour (consume the original immediate for timing)
      m.Fetch()
      m.ReadImm() |> ignore
      m.Regs.Set(R8.C, ClearAttrColour)
      // JR +1 -> 0x71BF
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 5
      m.Branch offset
    | _ -> failwithf "ClearAttrs: unhandled resume at %04X" (m.Regs.Pc())

  /// Address -> converted routine (None falls through to the generated layer).
  let screenClearHook (addr: int) : (Machine -> unit) option =
    if addr = 0x71B8 then Some clearPixels
    elif addr = 0x71C6 then Some clearAttrs
    elif addr >= 0x71BF && addr <= 0x71C5 then Some clearLoop
    else None
  let screenClearRoutine : LiftedRoutine =
    { Name = "screen-clear"
      EntryAddresses = [ 0x71B8; 0x71C6; 0x71BF; 0x71C0; 0x71C1; 0x71C2; 0x71C3; 0x71C5 ]
      Execute = fun machine ->
        match screenClearHook (machine.Regs.Pc()) with
        | Some execute -> execute machine
        | None -> failwithf "screen-clear has no implementation at %04X" (machine.Regs.Pc())
      ImplementationMode = Lifted }
  /// 0x71CF-0x720D — the real per-frame screen clear this game build runs:
  /// an EXX-protected walk of the visible screen driven by the game's shadow
  /// variables at 0x5DC3/0x5DC4/0x5DCF. Translated 1:1 from the generated
  /// table (byte-for-byte fetches, exact PassTime) so the R register and
  /// T-states match; an interrupt can land on any of the 40 instruction-start
  /// addresses, hence one match arm each.
  let screenClear71CF (m: Machine) =
    match m.Regs.Pc() with
    | 0x71CF ->
      // EXX
      m.Fetch()
      m.Regs.Exx()
    | 0x71D0 ->
      // LD HL,(0x5DCF)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.L, m.Read addr)
      m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    | 0x71D3 ->
      // CALL 0x720E
      m.Fetch()
      let jumpAddress = m.ReadImm16()
      m.PassTime 1
      m.Push16(m.Regs.Pc())
      m.Regs.SetPc jumpAddress
    | 0x71D6 ->
      // LD A,(0x5DC4)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.A, m.Read addr)
    | 0x71D9 ->
      // LD B,A
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.A)
    | 0x71DA ->
      // LD A,(0x5DC3)
      m.Fetch()
      let addr = m.ReadImm16()
      m.Regs.Set(R8.A, m.Read addr)
    | 0x71DD ->
      // RRCA
      m.Fetch()
      let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71DE ->
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
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.ReadImm())
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
      // LD D,(IX+0x03)
      m.Fetch()
      m.Fetch()
      let d = m.ReadImm()
      let d = if d >= 0x80 then d - 0x100 else d
      m.PassTime 5
      m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
      m.Regs.Set(R8.D, m.Read(m.Regs.Wz()))
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
      let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
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
      let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71F1 ->
      // JR C,0x7200
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if m.Flags().carry then
        m.PassTime 5
        m.Branch offset
    | 0x71F3 ->
      // LD (HL),D
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.Write(m.Regs.Wz(), m.Regs.Get R8.D)
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
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71F8 ->
      // JR NZ,0x71FE
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x71FA ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x71FB ->
      // SUB 0x20
      m.Fetch()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.ReadImm()) false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x71FD ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x71FE ->
      // DJNZ 0x71EA
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      m.PassTime 1
      let newB = m.Regs.Get R8.B - 1
      m.Regs.Set(R8.B, newB)
      if newB <> 0 then
        m.PassTime 5
        m.Branch offset
    | 0x7200 ->
      // POP HL
      m.Fetch()
      m.Regs.Set(R16.HL, m.Pop16())
    | 0x7201 ->
      // PUSH BC
      m.Fetch()
      m.PassTime 1
      m.Push16(m.Regs.Get R16.BC)
    | 0x7202 ->
      // AND A
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x7203 ->
      // LD BC,0x0020
      m.Fetch()
      m.Regs.Set(R8.C, m.ReadImm())
      m.Regs.Set(R8.B, m.ReadImm())
    | 0x7206 ->
      // SBC HL,BC
      m.Fetch()
      m.Fetch()
      let rhs = m.Regs.Get R16.BC
      let lhs = m.Regs.Get R16.HL
      let struct (result, flags) = Alu.sbc16 lhs rhs (m.Flags()).carry
      m.Regs.Set(R16.HL, result)
      m.SetFlags flags
      m.PassTime 7
    | 0x7208 ->
      // POP BC
      m.Fetch()
      m.Regs.Set(R16.BC, m.Pop16())
    | 0x7209 ->
      // LD B,E
      m.Fetch()
      m.Regs.Set(R8.B, m.Regs.Get R8.E)
    | 0x720A ->
      // DEC C
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.C) (m.Flags())
      m.Regs.Set(R8.C, r)
      m.SetFlags f
    | 0x720B ->
      // JR NZ,0x71E9
      m.Fetch()
      let offset = m.ReadImm()
      let offset = if offset >= 0x80 then offset - 0x100 else offset
      if not (m.Flags().zero) then
        m.PassTime 5
        m.Branch offset
    | 0x720D ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "ScreenClear71CF: unhandled resume at %04X" (m.Regs.Pc())

  let screenClear71CFRoutine : LiftedRoutine =
    { Name = "screen-clear-71cf"
      EntryAddresses =
        [ 0x71CF; 0x71D0; 0x71D3; 0x71D6; 0x71D9; 0x71DA; 0x71DD; 0x71DE
          0x71DF; 0x71E0; 0x71E1; 0x71E3; 0x71E4; 0x71E5; 0x71E8; 0x71E9
          0x71EA; 0x71EB; 0x71ED; 0x71EF; 0x71F1; 0x71F3; 0x71F4; 0x71F5
          0x71F6; 0x71F8; 0x71FA; 0x71FB; 0x71FD; 0x71FE; 0x7200; 0x7201
          0x7202; 0x7203; 0x7206; 0x7208; 0x7209; 0x720A; 0x720B; 0x720D ]
      Execute = screenClear71CF
      ImplementationMode = Lifted }

  /// 0x72EE-0x7302 — step a screen address in HL up one character row:
  /// DEC H, and when the low 3 bits wrap (H was a multiple of 8), move L back
  /// 0x20 and adjust H by 0x08 unless L underflowed. Translated 1:1 from the
  /// generated table (byte-for-byte fetches, exact PassTime). The hottest
  /// routine in the game: ~35k calls per trace window, so its timing must be
  /// exact.
  let screenStep (m: Machine) =
    match m.Regs.Pc() with
    | 0x72EE ->
      // DEC H
      m.Fetch()
      let struct (r, f) = Alu.dec8 (m.Regs.Get R8.H) (m.Flags())
      m.Regs.Set(R8.H, r)
      m.SetFlags f
    | 0x72EF ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x72F0 ->
      // AND 0x07
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x72F2 ->
      // CP 0x07
      m.Fetch()
      let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x72F4 ->
      // RET NZ
      m.Fetch()
      m.PassTime 1
      if not (m.Flags().zero) then
        m.Regs.SetPc(m.Pop16())
    | 0x72F5 ->
      // LD A,L
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.L)
    | 0x72F6 ->
      // SUB 0x20
      m.Fetch()
      let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.ReadImm()) false
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x72F8 ->
      // LD L,A
      m.Fetch()
      m.Regs.Set(R8.L, m.Regs.Get R8.A)
    | 0x72F9 ->
      // AND 0xE0
      m.Fetch()
      let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x72FB ->
      // CP 0xE0
      m.Fetch()
      let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.ReadImm())
      m.Regs.Set(R8.A, r)
      m.SetFlags f
    | 0x72FD ->
      // RET Z
      m.Fetch()
      m.PassTime 1
      if m.Flags().zero then
        m.Regs.SetPc(m.Pop16())
    | 0x72FE ->
      // LD A,H
      m.Fetch()
      m.Regs.Set(R8.A, m.Regs.Get R8.H)
    | 0x72FF ->
      // ADD A,0x08
      m.Fetch()
      let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.ReadImm()) false
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
    | _ -> failwithf "ScreenStep: unhandled resume at %04X" (m.Regs.Pc())

  let screenStepRoutine : LiftedRoutine =
    { Name = "screen-addr-step"
      EntryAddresses =
        [ 0x72EE; 0x72EF; 0x72F0; 0x72F2; 0x72F4; 0x72F5; 0x72F6; 0x72F8
          0x72F9; 0x72FB; 0x72FD; 0x72FE; 0x72FF; 0x7301; 0x7302 ]
      Execute = screenStep
      ImplementationMode = Lifted }

  /// 0x64E6-0x64F0 — table lookup: DE = word at (0x67C1 + A*2). Translated
  /// 1:1 from the generated table (WZ mirroring on the (HL) reads, exact
  /// PassTime on ADD HL,BC and INC HL).
  let tableLookup64E6 (m: Machine) =
    match m.Regs.Pc() with
    | 0x64E6 ->
      // LD HL,0x67C1
      m.Fetch()
      m.Regs.Set(R8.L, m.ReadImm())
      m.Regs.Set(R8.H, m.ReadImm())
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
      let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags())
      m.Regs.Set(R16.HL, r)
      m.SetFlags f
      m.PassTime 7
    | 0x64ED ->
      // LD E,(HL)
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.Regs.Set(R8.E, m.Read(m.Regs.Wz()))
    | 0x64EE ->
      // INC HL
      m.Fetch()
      m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
      m.PassTime 2
    | 0x64EF ->
      // LD D,(HL)
      m.Fetch()
      m.Regs.SetWz(m.Regs.Get R16.HL)
      m.Regs.Set(R8.D, m.Read(m.Regs.Wz()))
    | 0x64F0 ->
      // RET
      m.Fetch()
      m.Regs.SetPc(m.Pop16())
    | _ -> failwithf "TableLookup64E6: unhandled resume at %04X" (m.Regs.Pc())

  let tableLookup64E6Routine : LiftedRoutine =
    { Name = "table-lookup-64e6"
      EntryAddresses = [ 0x64E6; 0x64E9; 0x64EA; 0x64EC; 0x64ED; 0x64EE; 0x64EF; 0x64F0 ]
      Execute = tableLookup64E6
      ImplementationMode = Lifted }

  let registry = [ screenClearRoutine; screenClear71CFRoutine; screenStepRoutine; tableLookup64E6Routine ]

  /// Per-address dispatch table built once from the registry. The hook runs
  /// on every executed instruction, so this must be O(1): scanning each
  /// routine's arm list per instruction cost more than the lifted work itself.
  let private hookTable : (Machine -> unit)[] =
    let table = Array.zeroCreate<Machine -> unit> 0x10000
    for routine in registry do
      for addr in routine.EntryAddresses do
        table[addr] <- routine.Execute
    table

  let registryHook (address: int) : (Machine -> unit) option =
    let f = hookTable[address &&& 0xFFFF]
    if obj.ReferenceEquals(f, null) then None else Some f
