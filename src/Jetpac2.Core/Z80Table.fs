namespace Jetpac2.Core

/// Generic per-opcode instruction table (multi-game support, Phase A).
/// Extracted from the per-address Generated.fs page tables: every distinct
/// executed opcode (and DD/FD/ED/CB sub-opcode) is implemented once and
/// dispatched by the byte(s) at the current PC. Address-level gaps (the old
/// per-page fallthroughs) are gone; a byte whose opcode is not covered yet
/// still raises "no code at 0x...." so the UI's static-disasm path works.
/// Uncovered opcodes are grown per game as they are encountered.
module Z80Table =
  let main : (Machine -> unit) option[] = [|
    Some (fun (m: Machine) ->
          m.Fetch()
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.ReadImm())
          m.Regs.Set(R8.B, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Write(m.Regs.Get R16.BC, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.BC, (m.Regs.Get R16.BC + 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.B) (m.Flags())
          m.Regs.Set(R8.B, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.B) (m.Flags())
          m.Regs.Set(R8.B, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Ex(R16.AF, R16.AF_)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.BC) (m.Flags())
          m.Regs.Set(R16.HL, r)
          m.SetFlags f
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.BC))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.BC, (m.Regs.Get R16.BC - 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.C) (m.Flags())
          m.Regs.Set(R8.C, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.C) (m.Flags())
          m.Regs.Set(R8.C, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.fastRotateCircular8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let offset = m.ReadImm()
          let offset = if offset >= 0x80 then offset - 0x100 else offset
          m.PassTime 1
          let newB = m.Regs.Get R8.B - 1
          m.Regs.Set(R8.B, newB)
          if newB <> 0 then
            m.PassTime 5
            m.Branch offset
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.ReadImm())
          m.Regs.Set(R8.D, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Write(m.Regs.Get R16.DE, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.DE, (m.Regs.Get R16.DE + 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.D) (m.Flags())
          m.Regs.Set(R8.D, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.D) (m.Flags())
          m.Regs.Set(R8.D, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.fastRotate8 (m.Regs.Get R8.A) Alu.Left (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let offset = m.ReadImm()
          let offset = if offset >= 0x80 then offset - 0x100 else offset
          m.PassTime 5
          m.Branch offset
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.DE) (m.Flags())
          m.Regs.Set(R16.HL, r)
          m.SetFlags f
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Read(m.Regs.Get R16.DE))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.DE, (m.Regs.Get R16.DE - 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.E) (m.Flags())
          m.Regs.Set(R8.E, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.E) (m.Flags())
          m.Regs.Set(R8.E, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.fastRotate8 (m.Regs.Get R8.A) Alu.Right (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let offset = m.ReadImm()
          let offset = if offset >= 0x80 then offset - 0x100 else offset
          if not (m.Flags().zero) then
            m.PassTime 5
            m.Branch offset
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.ReadImm())
          m.Regs.Set(R8.H, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let addr = m.ReadImm16()
          m.Write(addr, m.Regs.Get R8.L)
          m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.HL, (m.Regs.Get R16.HL + 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.H) (m.Flags())
          m.Regs.Set(R8.H, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.H) (m.Flags())
          m.Regs.Set(R8.H, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.daa (m.Regs.Get R8.A) (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let offset = m.ReadImm()
          let offset = if offset >= 0x80 then offset - 0x100 else offset
          if m.Flags().zero then
            m.PassTime 5
            m.Branch offset
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.HL) (m.Flags())
          m.Regs.Set(R16.HL, r)
          m.SetFlags f
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let addr = m.ReadImm16()
          m.Regs.Set(R8.L, m.Read addr)
          m.Regs.Set(R8.H, m.Read((addr + 1) &&& 0xFFFF))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.HL, (m.Regs.Get R16.HL - 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.L) (m.Flags())
          m.Regs.Set(R8.L, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.L) (m.Flags())
          m.Regs.Set(R8.L, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cpl (m.Regs.Get R8.A) (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let offset = m.ReadImm()
          let offset = if offset >= 0x80 then offset - 0x100 else offset
          if not (m.Flags().carry) then
            m.PassTime 5
            m.Branch offset
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.SPL, m.ReadImm())
          m.Regs.Set(R8.SPH, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let addr = m.ReadImm16()
          m.Write(addr, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.SP, (m.Regs.Get R16.SP + 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.PassTime 1
          let struct (r, f) = Alu.inc8 (m.Read(m.Regs.Wz())) (m.Flags())
          m.Write(m.Regs.Wz(), r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.PassTime 1
          let struct (r, f) = Alu.dec8 (m.Read(m.Regs.Wz())) (m.Flags())
          m.Write(m.Regs.Wz(), r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.scf (m.Regs.Get R8.A) (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let offset = m.ReadImm()
          let offset = if offset >= 0x80 then offset - 0x100 else offset
          if m.Flags().carry then
            m.PassTime 5
            m.Branch offset
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add16 (m.Regs.Get R16.HL) (m.Regs.Get R16.SP) (m.Flags())
          m.Regs.Set(R16.HL, r)
          m.SetFlags f
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let addr = m.ReadImm16()
          m.Regs.Set(R8.A, m.Read addr)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.SP, (m.Regs.Get R16.SP - 1) &&& 0xFFFF)
          m.PassTime 2
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.inc8 (m.Regs.Get R8.A) (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.dec8 (m.Regs.Get R8.A) (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.ccf (m.Regs.Get R8.A) (m.Flags())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.L)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.B, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.L)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.C, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.L)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.D, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.L)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.E, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.E)
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.L)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.H, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.H)
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.L, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.H)
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Halt()
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.L)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          m.Regs.Set(R8.A, m.Read(m.Regs.Wz()))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.A)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.B) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.C) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.D) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.E) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.H) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.L) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.A) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.B) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.C) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.D) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.E) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.H) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.A) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.B) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.C) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.D) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.E) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.H) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.L) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.A) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.B) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.C) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.D) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.E) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.H) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.L) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.A) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.B)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.D)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.H)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.B)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.D)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.E)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.H)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.B)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.D)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.E)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.H)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.B)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.C)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.D)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.E)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.H)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.L)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.A)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if not (m.Flags().zero) then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.BC, m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().zero) then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetPc(m.ReadImm16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().zero) then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Get R16.BC)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.ReadImm()) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 0
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if m.Flags().zero then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().zero then
            m.Regs.SetPc jumpAddress
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().zero then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.ReadImm()) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 8
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if not (m.Flags().carry) then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.DE, m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().carry) then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let a = m.Regs.Get R8.A
          let port = (m.ReadImm() ||| (a <<< 8)) &&& 0xFFFF
          m.PassTime 4
          m.Out(port, a)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().carry) then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Get R16.DE)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.ReadImm()) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 16
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if m.Flags().carry then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Exx()
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().carry then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let port = (m.ReadImm() ||| ((m.Regs.Get R8.A) <<< 8)) &&& 0xFFFF
          m.PassTime 4
          m.Regs.Set(R8.A, m.In port)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().carry then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.ReadImm()) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 24
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if not (m.Flags().parity) then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.HL, m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().parity) then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let sp = m.Regs.Sp()
          let spOldLow = m.Read sp
          m.PassTime 1
          let spOldHigh = m.Read((sp + 1) &&& 0xFFFF)
          m.Write(sp, m.Regs.Get R8.L)
          m.PassTime 2
          m.Write((sp + 1) &&& 0xFFFF, m.Regs.Get R8.H)
          m.Regs.Set(R16.HL, (spOldLow ||| (spOldHigh <<< 8)) &&& 0xFFFF)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().parity) then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Get R16.HL)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.and8 (m.Regs.Get R8.A) (m.ReadImm())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 32
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if m.Flags().parity then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.SetPc(m.Regs.Get R16.HL)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().parity then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Ex(R16.DE, R16.HL)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().parity then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.ReadImm())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 40
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if not (m.Flags().sign) then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Regs.Set(R16.AF, m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().sign) then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Iff1 <- false
          m.Iff2 <- false
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if not (m.Flags().sign) then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Get R16.AF)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.ReadImm())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 48
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          if m.Flags().sign then
            m.Regs.SetPc(m.Pop16())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 2
          m.Regs.SetSp(m.Regs.Get R16.HL)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().sign then
            m.Regs.SetPc jumpAddress
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Iff1 <- true
          m.Iff2 <- true
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          let jumpAddress = m.ReadImm16()
          if m.Flags().sign then
            m.PassTime 1
            m.Push16(m.Regs.Pc())
            m.Regs.SetPc jumpAddress
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.ReadImm())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Pc())
          m.Regs.SetPc 56
    )
  |]

  let dd : (Machine -> unit) option[] = [|
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.add16 (m.Regs.Get R16.IX) (m.Regs.Get R16.BC) (m.Flags())
          m.Regs.Set(R16.IX, r)
          m.SetFlags f
          m.PassTime 7
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.add16 (m.Regs.Get R16.IX) (m.Regs.Get R16.DE) (m.Flags())
          m.Regs.Set(R16.IX, r)
          m.SetFlags f
          m.PassTime 7
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.IXL, m.ReadImm())
          m.Regs.Set(R8.IXH, m.ReadImm())
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Write(addr, m.Regs.Get R8.IXL)
          m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.IXH)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R16.IX, (m.Regs.Get R16.IX + 1) &&& 0xFFFF)
          m.PassTime 2
    )
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Regs.Set(R8.IXL, m.Read addr)
          m.Regs.Set(R8.IXH, m.Read((addr + 1) &&& 0xFFFF))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R16.IX, (m.Regs.Get R16.IX - 1) &&& 0xFFFF)
          m.PassTime 2
    )
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.PassTime 1
          let struct (r, f) = Alu.inc8 (m.Read(m.Regs.Wz())) (m.Flags())
          m.Write(m.Regs.Wz(), r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
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
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 2
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.ReadImm())
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.B, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.C, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.D, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.E, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.H, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.L, m.Read(m.Regs.Wz()))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.C)
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.L)
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.A)
    )
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Regs.Set(R8.A, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Regs.Get R8.D) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Regs.Get R8.A) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.or8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Regs.Get R8.E)
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.ReadImm()) (m.Flags()).carry
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R16.IX, m.Pop16())
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.PassTime 1
          m.Push16(m.Regs.Get R16.IX)
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetPc(m.Regs.Get R16.IX)
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.ReadImm())
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
  |]

  let fd : (Machine -> unit) option[] = [|
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.IYL, m.ReadImm())
          m.Regs.Set(R8.IYH, m.ReadImm())
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.PassTime 1
          let struct (r, f) = Alu.inc8 (m.Read(m.Regs.Wz())) (m.Flags())
          m.Write(m.Regs.Wz(), r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.PassTime 1
          let struct (r, f) = Alu.dec8 (m.Read(m.Regs.Wz())) (m.Flags())
          m.Write(m.Regs.Wz(), r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 2
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.ReadImm())
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Regs.Set(R8.B, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Regs.Set(R8.C, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Regs.Set(R8.D, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Regs.Set(R8.E, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Regs.Set(R8.L, m.Read(m.Regs.Wz()))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.C)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.D)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.E)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.H)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Write(m.Regs.Wz(), m.Regs.Get R8.L)
    )
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Regs.Set(R8.A, m.Read(m.Regs.Wz()))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) false
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.xor8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 5
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          let struct (r, f) = Alu.cmp8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz()))
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
  |]

  let ed : (Machine -> unit) option[] = [|
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let rhs = m.Regs.Get R16.BC
          let lhs = m.Regs.Get R16.HL
          let struct (result, flags) = Alu.sbc16 lhs rhs (m.Flags()).carry
          m.Regs.Set(R16.HL, result)
          m.SetFlags flags
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Write(addr, m.Regs.Get R8.C)
          m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.B)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (result, flags) = Alu.sub8 0 (m.Regs.Get R8.A) false
          m.Regs.Set(R8.A, result)
          m.SetFlags flags
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.PassTime 1
          m.Regs.SetI(m.Regs.Get R8.A)
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Regs.Set(R8.C, m.Read addr)
          m.Regs.Set(R8.B, m.Read((addr + 1) &&& 0xFFFF))
    )
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let rhs = m.Regs.Get R16.DE
          let lhs = m.Regs.Get R16.HL
          let struct (result, flags) = Alu.sbc16 lhs rhs (m.Flags()).carry
          m.Regs.Set(R16.HL, result)
          m.SetFlags flags
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Write(addr, m.Regs.Get R8.E)
          m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.D)
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.IrqMode <- 1
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let rhs = m.Regs.Get R16.DE
          let lhs = m.Regs.Get R16.HL
          let struct (result, flags) = Alu.adc16 lhs rhs (m.Flags()).carry
          m.Regs.Set(R16.HL, result)
          m.SetFlags flags
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Regs.Set(R8.E, m.Read addr)
          m.Regs.Set(R8.D, m.Read((addr + 1) &&& 0xFFFF))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let result = m.Regs.R()
          m.PassTime 1
          m.SetFlags(Alu.iff2FlagsFor result (m.Flags()) (m.Iff2))
          m.Regs.Set(R8.A, result)
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let rhs = m.Regs.Get R16.HL
          let lhs = m.Regs.Get R16.HL
          let struct (result, flags) = Alu.adc16 lhs rhs (m.Flags()).carry
          m.Regs.Set(R16.HL, result)
          m.SetFlags flags
          m.PassTime 7
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let address = m.Regs.Get R16.HL
          let indHl = m.Read address
          let prevA = m.Regs.Get R8.A
          let newA = (prevA &&& 0xF0) ||| ((indHl >>> 4) &&& 0xF)
          m.Regs.Set(R8.A, newA)
          m.PassTime 4
          m.Write(address, ((indHl <<< 4) ||| (prevA &&& 0xF)) &&& 0xFF)
          m.SetFlags((m.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor newA)
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let rhs = m.Regs.Get R16.SP
          let lhs = m.Regs.Get R16.HL
          let struct (result, flags) = Alu.sbc16 lhs rhs (m.Flags()).carry
          m.Regs.Set(R16.HL, result)
          m.SetFlags flags
          m.PassTime 7
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Write(addr, m.Regs.Get R8.SPL)
          m.Write((addr + 1) &&& 0xFFFF, m.Regs.Get R8.SPH)
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.PassTime 4
          let result = m.In(m.Regs.Get R16.BC)
          m.SetFlags((m.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor result)
          m.Regs.Set(R8.A, result)
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let addr = m.ReadImm16()
          m.Regs.Set(R8.SPL, m.Read addr)
          m.Regs.Set(R8.SPH, m.Read((addr + 1) &&& 0xFFFF))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
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
          let flagsFromBits = (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags()) ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())
          let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()
          m.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc)
          if newBc <> 0 then
            m.Regs.SetWz((m.Regs.Pc() - 1) &&& 0xFFFF)
            m.Regs.SetPc((m.Regs.Pc() - 2) &&& 0xFFFF)
            m.PassTime 5
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let add = 0xFFFF
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
          let flagsFromBits = (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags()) ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())
          let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()
          m.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc)
          if newBc <> 0 then
            m.Regs.SetWz((m.Regs.Pc() - 1) &&& 0xFFFF)
            m.Regs.SetPc((m.Regs.Pc() - 2) &&& 0xFFFF)
            m.PassTime 5
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
  |]

  let cb : (Machine -> unit) option[] = [|
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotateCircular8 (m.Regs.Get R8.B) Alu.Left
          m.Regs.Set(R8.B, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotateCircular8 (m.Regs.Get R8.A) Alu.Left
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotateCircular8 (m.Regs.Get R8.C) Alu.Right
          m.Regs.Set(R8.C, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.B) Alu.Left (m.Flags()).carry
          m.Regs.Set(R8.B, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.C) Alu.Left (m.Flags()).carry
          m.Regs.Set(R8.C, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.D) Alu.Left (m.Flags()).carry
          m.Regs.Set(R8.D, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.E) Alu.Left (m.Flags()).carry
          m.Regs.Set(R8.E, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.L) Alu.Left (m.Flags()).carry
          m.Regs.Set(R8.L, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let struct (r, f) = Alu.rotate8 (lhs) Alu.Left (m.Flags()).carry
          m.Write(m.Regs.Wz(), r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.B) Alu.Right (m.Flags()).carry
          m.Regs.Set(R8.B, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.D) Alu.Right (m.Flags()).carry
          m.Regs.Set(R8.D, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.E) Alu.Right (m.Flags()).carry
          m.Regs.Set(R8.E, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.H) Alu.Right (m.Flags()).carry
          m.Regs.Set(R8.H, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.rotate8 (m.Regs.Get R8.L) Alu.Right (m.Flags()).carry
          m.Regs.Set(R8.L, r)
          m.SetFlags f
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.B) Alu.Left
          m.Regs.Set(R8.B, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.C) Alu.Left
          m.Regs.Set(R8.C, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.E) Alu.Left
          m.Regs.Set(R8.E, r)
          m.SetFlags f
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.A) Alu.Left
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftArithmetic8 (m.Regs.Get R8.L) Alu.Right
          m.Regs.Set(R8.L, r)
          m.SetFlags f
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftLogical8 (m.Regs.Get R8.C) Alu.Right
          m.Regs.Set(R8.C, r)
          m.SetFlags f
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftLogical8 (m.Regs.Get R8.H) Alu.Right
          m.Regs.Set(R8.H, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftLogical8 (m.Regs.Get R8.L) Alu.Right
          m.Regs.Set(R8.L, r)
          m.SetFlags f
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let struct (r, f) = Alu.shiftLogical8 (m.Regs.Get R8.A) Alu.Right
          m.Regs.Set(R8.A, r)
          m.SetFlags f
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.B) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.B))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.C))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.L) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.L))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 0) (m.Flags()) busNoise)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 0) (m.Flags()) (m.Regs.Get R8.A))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 1) (m.Flags()) (m.Regs.Get R8.C))
    )
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 1) (m.Flags()) (m.Regs.Get R8.A))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.B) (1 <<< 2) (m.Flags()) (m.Regs.Get R8.B))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 2) (m.Flags()) (m.Regs.Get R8.E))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 2) (m.Flags()) busNoise)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 2) (m.Flags()) (m.Regs.Get R8.A))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.D) (1 <<< 3) (m.Flags()) (m.Regs.Get R8.D))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 3) (m.Flags()) (m.Regs.Get R8.E))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 3) (m.Flags()) busNoise)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 3) (m.Flags()) (m.Regs.Get R8.A))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 4) (m.Flags()) (m.Regs.Get R8.E))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 4) (m.Flags()) (m.Regs.Get R8.A))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.B) (1 <<< 5) (m.Flags()) (m.Regs.Get R8.B))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 5) (m.Flags()) (m.Regs.Get R8.C))
    )
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 5) (m.Flags()) (m.Regs.Get R8.A))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.B) (1 <<< 6) (m.Flags()) (m.Regs.Get R8.B))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 6) (m.Flags()) (m.Regs.Get R8.C))
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 6) (m.Flags()) busNoise)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 6) (m.Flags()) (m.Regs.Get R8.A))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.B) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.B))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.C) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.C))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.D) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.D))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.E) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.E))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 7) (m.Flags()) busNoise)
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.SetFlags(Alu.bit (m.Regs.Get R8.A) (1 <<< 7) (m.Flags()) (m.Regs.Get R8.A))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.B &&& ~~~(1 <<< 0))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.D &&& ~~~(1 <<< 0))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 0))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.D &&& ~~~(1 <<< 1))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 1))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 2))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 3))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.B &&& ~~~(1 <<< 5))
    )
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 5))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C &&& ~~~(1 <<< 6))
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 6))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C &&& ~~~(1 <<< 7))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.H, m.Regs.Get R8.H &&& ~~~(1 <<< 7))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 7))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.D ||| (1 <<< 0))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 0))
    )
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.D ||| (1 <<< 1))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.E ||| (1 <<< 2))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 2))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C ||| (1 <<< 3))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.E ||| (1 <<< 3))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 3))
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.E ||| (1 <<< 4))
    )
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.B ||| (1 <<< 5))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C ||| (1 <<< 5))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C ||| (1 <<< 6))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.E ||| (1 <<< 6))
    )
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 6))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.B, m.Regs.Get R8.B ||| (1 <<< 7))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.C, m.Regs.Get R8.C ||| (1 <<< 7))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.D, m.Regs.Get R8.D ||| (1 <<< 7))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.E, m.Regs.Get R8.E ||| (1 <<< 7))
    )
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.L, m.Regs.Get R8.L ||| (1 <<< 7))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.SetWz(m.Regs.Get R16.HL)
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    )
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          m.Regs.Set(R8.A, m.Regs.Get R8.A ||| (1 <<< 7))
    )
  |]

  let dd_cb : (Machine -> unit) option[] = [|
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
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
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
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
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 2))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 6))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
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
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 1))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Ix() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 2))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
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
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
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
    )
    None
  |]

  let fd_cb : (Machine -> unit) option[] = [|
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 0) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 1) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 2) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 3) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 4) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 5) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 6) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF
          m.SetFlags(Alu.bit lhs (1 <<< 7) (m.Flags()) busNoise)
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 0))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 1))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 2))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 4))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 6))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs &&& ~~~(1 <<< 7))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 0))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 1))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 2))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 3))
    )
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 5))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 6))
    )
    None
    None
    None
    None
    None
    None
    None
    Some (fun (m: Machine) ->
          m.Fetch()
          m.Fetch()
          let d = m.ReadImm()
          let d = if d >= 0x80 then d - 0x100 else d
          m.PassTime 1
          m.Regs.SetWz((m.Regs.Iy() + d) &&& 0xFFFF)
          m.Fetch()
          let lhs = m.Read(m.Regs.Wz())
          m.PassTime 1
          m.Write(m.Regs.Wz(), lhs ||| (1 <<< 7))
    )
    None
  |]

  let step (m: Machine) =
    let pc = m.Regs.Pc()
    let op = int m.Memory.[pc &&& 0xFFFF]
    let run (table: (Machine -> unit) option[]) (idx: int) =
      match table.[idx] with
      | Some f -> f m
      | None -> failwithf "no code at 0x%04X" pc
    let prefix (name: string) (table: (Machine -> unit) option[]) =
      let second = int m.Memory.[(pc + 1) &&& 0xFFFF]
      if second = 0xCB then
        // DD/FD CB d X: the fourth byte selects the implementation.
        run (if name = "dd" then dd_cb else fd_cb) (int m.Memory.[(pc + 3) &&& 0xFFFF])
      else
        run table second
    match op with
    | 0xDD -> prefix "dd" dd
    | 0xFD -> prefix "fd" fd
    | 0xED -> run ed (int m.Memory.[(pc + 1) &&& 0xFFFF])
    | 0xCB -> run cb (int m.Memory.[(pc + 1) &&& 0xFFFF])
    | _ -> run main op

  /// Install the generic step into Machine (replaces the per-address pages).
  let EnsureInstalled () =
    Machine.GeneratedStep <- step
