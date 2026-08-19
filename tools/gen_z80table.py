#!/usr/bin/env python3
"""Phase A + Phase 2: build the generic Z80 table.

Phase A: extract the per-address Generated.fs arms, re-key by opcode, dedupe.
Phase 2: fill every remaining slot with the full ISA (templates written from
the oracle's Z80Ops.fs semantics + the validated corpus arm patterns). The
per-op test (--test z80ops) validates each slot against the oracle.
"""
import re
import sys

GEN = 'src/Jetpac2.Core/Generated.fs'
OUT = 'src/Jetpac2.Core/Z80Table.fs'
MEM = 'tests/JetpacFR.Core.Tests/bin/Debug/net10.0/entry-cache/memory.bin'

mem = bytearray(open(MEM, 'rb').read())
if len(mem) != 0x10000:
    print(f'FATAL: memory image is {len(mem)} bytes, expected 65536')
    sys.exit(1)

text = open(GEN, encoding='utf-8').read()
parts = re.split(r'let (page[0-9A-F]{2}) =', text)


def depth_collect(lines, start):
    k = start
    depth = 0
    body = []
    k += 1
    while k < len(lines):
        line = lines[k]
        depth += line.count('(') - line.count(')')
        if depth < 0:
            break
        body.append(line)
        k += 1
    return body, k + 1


arms = []
for i in range(1, len(parts), 2):
    page = int(parts[i][4:], 16)
    lines = parts[i + 1].split('\n')
    j = 0
    while j < len(lines):
        m = re.match(r'^\s*\| 0x([0-9A-F]{2}) ->$', lines[j])
        if not m:
            j += 1
            continue
        off = int(m.group(1), 16)
        addr = page * 0x100 + off
        body, j = depth_collect(lines, j + 1)
        guard = None
        if body and re.match(
            r'^\s*if m\.Memory\.\[0x[0-9A-F]{4}\] <> 0x[0-9A-F]{2}uy then failwithf "self-modifying code at 0x[0-9A-F]{4}"$',
            body[0]):
            gm = re.match(r'^\s*if m\.Memory\.\[0x([0-9A-F]{4})\] <> 0x([0-9A-F]{2})uy', body[0])
            guard = (int(gm.group(1), 16), int(gm.group(2), 16))
            body = body[1:]
        arms.append((addr, guard, body))


def norm(body):
    return [l.strip() for l in body]


def key_and_body(addr, guard, body):
    mm = re.match(r'^\s*match m\.Memory\.\[0x[0-9A-F]{4}\] with$', body[0]) if body else None
    if mm:
        c = 1
        while c < len(body):
            cm = re.match(r'^\s*\| 0x([0-9A-F]{2})uy ->$', body[c])
            if cm:
                byte = int(cm.group(1), 16)
                c += 1
                cbody = []
                while c < len(body) and not re.match(r'^\s*\| (0x[0-9A-F]{2}uy|b) ->', body[c]):
                    cbody.append(body[c])
                    c += 1
                yield (('main', byte), cbody)
            elif re.match(r'^\s*\| b ->', body[c]):
                c += 1
                while c < len(body) and not re.match(r'^\s*\| 0x[0-9A-F]{2}uy ->', body[c]):
                    c += 1
            else:
                c += 1
        return
    if guard is None:
        return
    gaddr, first = guard
    assert gaddr == addr
    if first in (0xDD, 0xFD, 0xED, 0xCB):
        second = mem[(addr + 1) & 0xFFFF]
        name = {0xDD: 'dd', 0xFD: 'fd', 0xED: 'ed', 0xCB: 'cb'}[first]
        if second == 0xCB:
            x = mem[(addr + 3) & 0xFFFF]
            yield ((f'{name}_cb', x), body)
        else:
            yield ((name, second), body)
    else:
        yield (('main', first), body)


tables = {}
for addr, guard, body in arms:
    for key, b in key_and_body(addr, guard, body):
        tname, byte = key
        tables.setdefault(tname, {})
        if tname not in tables or byte not in tables[tname]:
            tables.setdefault(tname, {}).setdefault(byte, b)

# ---------------------------------------------------------------------------
# Phase 2: fill every remaining slot with the full ISA.
# ---------------------------------------------------------------------------

R = ['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '(HL)', 'R8.A']
F = ['m.Fetch()', 'm.Fetch()']


def lines(*ls):
    return list(ls)


def alu_body(fn, lhs, rhs, extra=''):
    return lines('m.Fetch()', f'let struct (r, f) = Alu.{fn} {lhs} {rhs}{extra}',
                 'm.Regs.Set(R8.A, r)', 'm.SetFlags f')


# ---- main: 7 missing -------------------------------------------------------
main_fill = {
    0x64: lines('m.Fetch()', 'm.Regs.Set(R8.H, m.Regs.Get R8.H)'),
    0x6D: lines('m.Fetch()', 'm.Regs.Set(R8.L, m.Regs.Get R8.L)'),
    0x75: lines('m.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)', 'm.Write(m.Regs.Wz(), m.Regs.Get R8.L)'),
    0x8D: alu_body('add8', '(m.Regs.Get R8.A)', '(m.Regs.Get R8.L)', ' (m.Flags()).carry'),
    0x8E: lines('m.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
                'let struct (r, f) = Alu.add8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) (m.Flags()).carry',
                'm.Regs.Set(R8.A, r)', 'm.SetFlags f'),
    0x96: lines('m.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
                'let struct (r, f) = Alu.sub8 (m.Regs.Get R8.A) (m.Read(m.Regs.Wz())) false',
                'm.Regs.Set(R8.A, r)', 'm.SetFlags f'),
    0xA3: alu_body('and8', '(m.Regs.Get R8.A)', '(m.Regs.Get R8.E)'),
}

# ---- CB: all 256 -----------------------------------------------------------
cb_fill = {}
rot = [
    (0, 'rotateCircular8', 'Alu.Left', ''),
    (1, 'rotateCircular8', 'Alu.Right', ''),
    (2, 'rotate8', 'Alu.Left', ' (m.Flags()).carry'),
    (3, 'rotate8', 'Alu.Right', ' (m.Flags()).carry'),
    (4, 'shiftArithmetic8', 'Alu.Left', ''),
    (5, 'shiftArithmetic8', 'Alu.Right', ''),
    (6, 'shiftLogical8', 'Alu.Left', ''),  # SLL
    (7, 'shiftLogical8', 'Alu.Right', ''),  # SRL
]
for y, fn, d, carry in rot:
    base = y << 3
    for r in range(8):
        slot = base | r
        if r == 6:  # (HL)
            b = lines('m.Fetch()', 'm.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
                      'let lhs = m.Read(m.Regs.Wz())', 'm.PassTime 1',
                      f'let struct (r, f) = Alu.{fn} lhs {d}{carry}',
                      'm.Write(m.Regs.Wz(), r)', 'm.SetFlags f')
        else:
            b = lines('m.Fetch()', 'm.Fetch()',
                      f'let struct (r, f) = Alu.{fn} (m.Regs.Get {R[r]}) {d}{carry}',
                      f'm.Regs.Set({R[r]}, r)', 'm.SetFlags f')
        cb_fill[slot] = b
for y in range(8):  # BIT/RES/SET
    bit = 1 << y
    for x, base in [(1, 0x40), (2, 0x80), (3, 0xC0)]:
        for r in range(8):
            slot = base | (y << 3) | r
            if x == 1:  # BIT
                if r == 6:
                    b = lines('m.Fetch()', 'm.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
                              'let lhs = m.Read(m.Regs.Wz())', 'm.PassTime 1',
                              'let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF',
                              f'm.SetFlags(Alu.bit lhs ({bit}) (m.Flags()) busNoise)')
                else:
                    b = lines('m.Fetch()', 'm.Fetch()',
                              f'm.SetFlags(Alu.bit (m.Regs.Get {R[r]}) ({bit}) (m.Flags()) (m.Regs.Get {R[r]}))')
            else:
                op = '&&&' if x == 2 else '|||'
                val = f'(m.Regs.Get {R[r]}) {op} ~~~({bit})' if x == 2 else f'(m.Regs.Get {R[r]}) {op} ({bit})'
                if r == 6:
                    b = lines('m.Fetch()', 'm.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
                              'let lhs = m.Read(m.Regs.Wz())', 'm.PassTime 1',
                              f'm.Write(m.Regs.Wz(), lhs {op} ({"~~~" if x == 2 else ""}({bit})))')
                else:
                    b = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({R[r]}, {val})')
            cb_fill[slot] = b

# ---- ED: documented set + undefined -> 2-byte NOP --------------------------
ed_fill = {}
ed_nop = lines('m.Fetch()', 'm.Fetch()')
for slot in range(256):
    ed_fill[slot] = ed_nop
RP = [(0, 'R8.B', 'R8.C'), (1, 'R8.D', 'R8.E'), (2, 'R8.H', 'R8.L'), (3, 'R8.SPH', 'R8.SPL')]
for p in range(4):
    rr = ['R16.BC', 'R16.DE', 'R16.HL', 'R16.SP'][p]
    hi, lo = RP[p][1], RP[p][2]  # RP = (high, low); store/load LOW first
    q0, q1 = p << 4, (p << 4) | 0x08
    ed_fill[0x42 | q0] = lines('m.Fetch()', 'm.Fetch()',
        f'let struct (r, f) = Alu.sbc16 (m.Regs.Get R16.HL) (m.Regs.Get {rr}) (m.Flags()).carry',
        'm.Regs.Set(R16.HL, r)', 'm.SetFlags f', 'm.PassTime 7')
    ed_fill[0x42 | q1] = lines('m.Fetch()', 'm.Fetch()',
        f'let struct (r, f) = Alu.adc16 (m.Regs.Get R16.HL) (m.Regs.Get {rr}) (m.Flags()).carry',
        'm.Regs.Set(R16.HL, r)', 'm.SetFlags f', 'm.PassTime 7')
    ed_fill[0x43 | q0] = lines('m.Fetch()', 'm.Fetch()', 'let addr = m.ReadImm16()',
        f'm.Write(addr, m.Regs.Get {lo})', f'm.Write((addr + 1) &&& 0xFFFF, m.Regs.Get {hi})')
    ed_fill[0x43 | q1] = lines('m.Fetch()', 'm.Fetch()', 'let addr = m.ReadImm16()',
        f'm.Regs.Set({lo}, m.Read addr)', f'm.Regs.Set({hi}, m.Read((addr + 1) &&& 0xFFFF))')
# IN (C) / OUT (C),0 (y = 6): same timing as the register forms.
ed_fill[0x70] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 4',
    'let result = m.In(m.Regs.Get R16.BC)',
    'm.SetFlags((m.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor result)')
ed_fill[0x71] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 4',
    'm.Out(m.Regs.Get R16.BC, 0)')
for y in range(8):
    if y != 6:
        r8 = ['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][y]
        # IN r,(C)
        ed_fill[0x40 | (y << 3)] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 4',
            'let result = m.In(m.Regs.Get R16.BC)',
            'm.SetFlags((m.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor result)',
            f'm.Regs.Set({r8}, result)' if y != 6 else '()')
        # OUT (C),r
        if y == 6:
            ed_fill[0x41 | (y << 3)] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 4',
                'm.Out(m.Regs.Get R16.BC, 0)')
        else:
            ed_fill[0x41 | (y << 3)] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 4',
                f'm.Out(m.Regs.Get R16.BC, m.Regs.Get {r8})')
    # RETN/RETI family (z = 5)
    ed_fill[0x45 | (y << 3)] = lines('m.Fetch()', 'm.Fetch()',
        f'if {y} <> 1 then m.Iff1 <- m.Iff2', 'm.Regs.SetPc(m.Pop16())')
    # IM (z = 6)
    im = [0, 0, 1, 2, 0, 0, 1, 2][y]
    ed_fill[0x46 | (y << 3)] = lines('m.Fetch()', 'm.Fetch()', f'm.IrqMode <- {im}')
    # z = 7 specials
    if y == 0:
        ed_fill[0x47] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 1', 'm.Regs.SetI(m.Regs.Get R8.A)')
    elif y == 1:
        ed_fill[0x4F] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 1', 'm.Regs.SetR(m.Regs.Get R8.A)')
    elif y == 2:
        ed_fill[0x57] = lines('m.Fetch()', 'm.Fetch()', 'let result = m.Regs.I()', 'm.PassTime 1',
            'm.SetFlags(Alu.iff2FlagsFor result (m.Flags()) (m.Iff2))', 'm.Regs.Set(R8.A, result)')
    elif y == 3:
        ed_fill[0x5F] = lines('m.Fetch()', 'm.Fetch()', 'let result = m.Regs.R()', 'm.PassTime 1',
            'm.SetFlags(Alu.iff2FlagsFor result (m.Flags()) (m.Iff2))', 'm.Regs.Set(R8.A, result)')
    elif y == 4:  # RRD
        ed_fill[0x67] = lines('m.Fetch()', 'm.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
            'let indHl = m.Read(m.Regs.Wz())', 'let prevA = m.Regs.Get R8.A',
            'let newA = (prevA &&& 0xF0) ||| (indHl &&& 0xF)', 'm.Regs.Set(R8.A, newA)',
            'm.PassTime 4', 'm.Write(m.Regs.Wz(), ((indHl >>> 4) ||| ((prevA &&& 0xF) <<< 4)) &&& 0xFF)',
            'm.SetFlags((m.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor newA)')
    elif y == 5:  # RLD
        ed_fill[0x6F] = lines('m.Fetch()', 'm.Fetch()', 'm.Regs.SetWz(m.Regs.Get R16.HL)',
            'let indHl = m.Read(m.Regs.Wz())', 'let prevA = m.Regs.Get R8.A',
            'let newA = (prevA &&& 0xF0) ||| ((indHl >>> 4) &&& 0xF)', 'm.Regs.Set(R8.A, newA)',
            'm.PassTime 4', 'm.Write(m.Regs.Wz(), ((indHl <<< 4) ||| (prevA &&& 0xF)) &&& 0xFF)',
            'm.SetFlags((m.Flags() &&& Flags.Carry()) ||| Alu.parityFlagsFor newA)')

BLOCK_LOAD = [
    'm.Fetch()', 'm.Fetch()',
    'let add = 1',  # placeholder; replaced below
    'let hl = m.Regs.Get R16.HL', 'm.Regs.Set(R16.HL, (hl + add) &&& 0xFFFF)',
    'let byte = m.Read hl', 'let de = m.Regs.Get R16.DE', 'm.Regs.Set(R16.DE, (de + add) &&& 0xFFFF)',
    'm.Write(de, byte)', 'm.PassTime 2', 'let flagBits = byte + m.Regs.Get R8.A',
    'let newBc = (m.Regs.Get R16.BC - 1) &&& 0xFFFF', 'm.Regs.Set(R16.BC, newBc)',
    'let preservedFlags = m.Flags() &&& (Flags.Sign() ||| Flags.Zero() ||| Flags.Carry())',
    'let flagsFromBits = (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags()) ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())',
    'let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()',
    'm.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc)',
    'if repeat && newBc <> 0 then',
    '  m.Regs.SetWz((m.Regs.Pc() - 1) &&& 0xFFFF)',
    '  m.Regs.SetPc((m.Regs.Pc() - 2) &&& 0xFFFF)',
    '  m.PassTime 5',
]


def block_load(inc, rep):
    b = list(BLOCK_LOAD)
    b[2] = 'let add = 1' if inc else 'let add = 0xFFFF'
    b[17] = f'if {"true" if rep else "false"} && newBc <> 0 then'
    return b


BLOCK_CMP = [
    'm.Fetch()', 'm.Fetch()',
    'let add = 1',
    'let hl = m.Regs.Get R16.HL', 'm.Regs.Set(R16.HL, (hl + add) &&& 0xFFFF)',
    'let byte = m.Read hl', 'let struct (result, subtractFlags) = Alu.sub8 (m.Regs.Get R8.A) byte false',
    'm.PassTime 5', 'let flagBits = if subtractFlags.half_carry then result - 1 else result',
    'let newBc = (m.Regs.Get R16.BC - 1) &&& 0xFFFF', 'm.Regs.Set(R16.BC, newBc)',
    'let fromSubtractMask = Flags.HalfCarry() ||| Flags.Zero() ||| Flags.Sign() ||| Flags.Subtract()',
    'let preservedFlags = m.Flags() &&& ~~~(Flags.Flag3() ||| Flags.Flag5() ||| fromSubtractMask ||| Flags.Overflow())',
    'let flagsFromBits = (if flagBits &&& 0x08 <> 0 then Flags.Flag3() else Flags()) ||| (if flagBits &&& 0x02 <> 0 then Flags.Flag5() else Flags())',
    'let flagsFromBc = if newBc <> 0 then Flags.Overflow() else Flags()',
    'm.SetFlags(preservedFlags ||| flagsFromBits ||| flagsFromBc ||| (fromSubtractMask &&& subtractFlags))',
    'if repeat && newBc <> 0 && not subtractFlags.zero then',
    '  m.Regs.SetWz((m.Regs.Pc() - 1) &&& 0xFFFF)',
    '  m.Regs.SetPc((m.Regs.Pc() - 2) &&& 0xFFFF)',
    '  m.PassTime 5',
]


def block_cmp(inc, rep):
    b = list(BLOCK_CMP)
    b[2] = 'let add = 1' if inc else 'let add = 0xFFFF'
    b[16] = f'if {"true" if rep else "false"} && newBc <> 0 && not subtractFlags.zero then'
    return b


for inc, rep, slot in [(True, False, 0xA0), (False, False, 0xA8), (True, True, 0xB0), (False, True, 0xB8)]:
    ed_fill[slot] = block_load(inc, rep)
for inc, rep, slot in [(True, False, 0xA1), (False, False, 0xA9), (True, True, 0xB1), (False, True, 0xB9)]:
    ed_fill[slot] = block_cmp(inc, rep)

# ---- DD/FD: IX-specific forms + prefix-ignored (copy main) + nested -------
def ix_fill(prefix_ix, ixr, irl, is_iy):
    """prefix_ix: 'R16.IX'|'R16.IY'; ixr: 'R8.IXH'|'R8.IYH'; irl: IXL/IYL."""
    fill = {}
    main_slot = tables['main']
    for slot in range(256):
        fill[slot] = main_slot.get(slot)  # default: prefix ignored
    # 16-bit IX loads / adds / inc / dec
    fill[0x21] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({irl}, m.ReadImm())', f'm.Regs.Set({ixr}, m.ReadImm())')
    fill[0x22] = lines('m.Fetch()', 'm.Fetch()', 'let addr = m.ReadImm16()',
                       f'm.Write(addr, m.Regs.Get {irl})', f'm.Write((addr + 1) &&& 0xFFFF, m.Regs.Get {ixr})')
    fill[0x2A] = lines('m.Fetch()', 'm.Fetch()', 'let addr = m.ReadImm16()',
                       f'm.Regs.Set({irl}, m.Read addr)', f'm.Regs.Set({ixr}, m.Read((addr + 1) &&& 0xFFFF))')
    for p, rr in [(0, 'R16.BC'), (1, 'R16.DE'), (2, prefix_ix), (3, 'R16.SP')]:
        fill[0x09 | (p << 4)] = lines('m.Fetch()', 'm.Fetch()',
            f'let struct (r, f) = Alu.add16 (m.Regs.Get {prefix_ix}) (m.Regs.Get {rr}) (m.Flags())',
            f'm.Regs.Set({prefix_ix}, r)', 'm.SetFlags f', 'm.PassTime 7')
    fill[0x23] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({prefix_ix}, (m.Regs.Get {prefix_ix} + 1) &&& 0xFFFF)', 'm.PassTime 2')
    fill[0x2B] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({prefix_ix}, (m.Regs.Get {prefix_ix} - 1) &&& 0xFFFF)', 'm.PassTime 2')
    # IXH/IXL inc/dec/load
    fill[0x24] = lines('m.Fetch()', 'm.Fetch()', f'let struct (r, f) = Alu.inc8 (m.Regs.Get {ixr}) (m.Flags())', f'm.Regs.Set({ixr}, r)', 'm.SetFlags f')
    fill[0x25] = lines('m.Fetch()', 'm.Fetch()', f'let struct (r, f) = Alu.dec8 (m.Regs.Get {ixr}) (m.Flags())', f'm.Regs.Set({ixr}, r)', 'm.SetFlags f')
    fill[0x26] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({ixr}, m.ReadImm())')
    fill[0x2C] = lines('m.Fetch()', 'm.Fetch()', f'let struct (r, f) = Alu.inc8 (m.Regs.Get {irl}) (m.Flags())', f'm.Regs.Set({irl}, r)', 'm.SetFlags f')
    fill[0x2D] = lines('m.Fetch()', 'm.Fetch()', f'let struct (r, f) = Alu.dec8 (m.Regs.Get {irl}) (m.Flags())', f'm.Regs.Set({irl}, r)', 'm.SetFlags f')
    fill[0x2E] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({irl}, m.ReadImm())')
    # (IX+d) inc/dec/ld
    ind = f'(m.Regs.{("Ix" if not is_iy else "Iy")}() + d) &&& 0xFFFF'
    fill[0x34] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                       'm.PassTime 5', f'm.Regs.SetWz({ind})', 'm.PassTime 1',
                       'let struct (r, f) = Alu.inc8 (m.Read(m.Regs.Wz())) (m.Flags())', 'm.Write(m.Regs.Wz(), r)', 'm.SetFlags f')
    fill[0x35] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                       'm.PassTime 5', f'm.Regs.SetWz({ind})', 'm.PassTime 1',
                       'let struct (r, f) = Alu.dec8 (m.Read(m.Regs.Wz())) (m.Flags())', 'm.Write(m.Regs.Wz(), r)', 'm.SetFlags f')
    fill[0x36] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                       'm.PassTime 2', f'm.Regs.SetWz({ind})', 'm.Write(m.Regs.Wz(), m.ReadImm())')
    # LD r,(IX+d) and LD (IX+d),r and the IXH/IXL register block (0x40-0x7F)
    for y in range(8):
        for z in range(8):
            slot = 0x40 | (y << 3) | z
            if y == 6 and z == 6:
                fill[slot] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 1', 'm.Halt()')
                continue
            if z == 6:  # source (IX+d): dest = plain register
                if y == 6:
                    fill[slot] = None  # handled above
                    continue
                dest = ['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][y]
                fill[slot] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                                   'm.PassTime 5', f'm.Regs.SetWz({ind})', f'm.Regs.Set({dest}, m.Read(m.Regs.Wz()))')
                continue
            if y == 6:  # dest (IX+d): source = plain register
                src = ['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][z]
                fill[slot] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                                   'm.PassTime 5', f'm.Regs.SetWz({ind})', f'm.Write(m.Regs.Wz(), m.Regs.Get {src})')
                continue
            # both plain (or remapped H/L -> IXH/IXL)
            def rem(r8):
                return {'R8.H': ixr, 'R8.L': irl}.get(r8, r8)
            dest, src = rem(['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][y]), rem(['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][z])
            fill[slot] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({dest}, m.Regs.Get {src})')
    # ALU block: 0x80-0xBF
    alu = ['add8', 'add8', 'sub8', 'sub8', 'and8', 'xor8', 'or8', 'cmp8']
    for y in range(8):
        for z in range(8):
            slot = 0x80 | (y << 3) | z
            fn = alu[y]
            carry = ' (m.Flags()).carry' if y in (1, 3) else ' false' if y in (0, 2) else ''
            if z == 6:
                rhs = '(m.Read(m.Regs.Wz()))'
                b = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                          'm.PassTime 5', f'm.Regs.SetWz({ind})',
                          f'let struct (r, f) = Alu.{fn} (m.Regs.Get R8.A) {rhs}{carry}',
                          'm.Regs.Set(R8.A, r)', 'm.SetFlags f')
            else:
                zr = {'R8.H': ixr, 'R8.L': irl}.get(['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][z],
                                                     ['R8.B', 'R8.C', 'R8.D', 'R8.E', 'R8.H', 'R8.L', '', 'R8.A'][z])
                b = lines('m.Fetch()', 'm.Fetch()',
                          f'let struct (r, f) = Alu.{fn} (m.Regs.Get R8.A) (m.Regs.Get {zr}){carry}',
                          'm.Regs.Set(R8.A, r)', 'm.SetFlags f')
            fill[slot] = b
    # stack / jp / misc
    fill[0xE1] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.Set({prefix_ix}, m.Pop16())')
    fill[0xE5] = lines('m.Fetch()', 'm.Fetch()', 'm.PassTime 1', f'm.Push16(m.Regs.Get {prefix_ix})')
    fill[0xE9] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.SetPc(m.Regs.Get {prefix_ix})')
    fill[0xF9] = lines('m.Fetch()', 'm.Fetch()', f'm.Regs.SetSp(m.Regs.Get {prefix_ix})', 'm.PassTime 2')
    ixlo = ixr.replace('H', 'L')
    fill[0xE3] = lines('m.Fetch()', 'm.Fetch()', 'let sp = m.Regs.Sp()',
                       'let spOldLow = m.Read sp', 'm.PassTime 1',
                       'let spOldHigh = m.Read((sp + 1) &&& 0xFFFF)',
                       f'm.Write(sp, m.Regs.Get {irl})', 'm.PassTime 2',
                       f'm.Write((sp + 1) &&& 0xFFFF, m.Regs.Get {ixr})',
                       f'm.Regs.Set({prefix_ix}, (spOldLow ||| (spOldHigh <<< 8)) &&& 0xFFFF)')
    # Nested prefixes (DD DD, DD FD, ...): the oracle reads ONE prefix, then
    # treats a second prefix byte as the (undefined) opcode -> 2-byte NOP.
    nested = lines('m.Fetch()', 'm.Fetch()')
    fill[0xDD] = nested
    fill[0xFD] = nested
    fill[0xED] = nested
    fill[0xCB] = nested
    return fill


dd_fill = ix_fill('R16.IX', 'R8.IXH', 'R8.IXL', False)
fd_fill = ix_fill('R16.IY', 'R8.IYH', 'R8.IYL', True)

# ---- DD CB / FD CB (all 256 each) ------------------------------------------
def ddcb_fill(is_iy):
    fill = {}
    ind = f'(m.Regs.{("Iy" if is_iy else "Ix")}() + d) &&& 0xFFFF'
    rot = [
        (0, 'rotateCircular8', 'Alu.Left', ''),
        (1, 'rotateCircular8', 'Alu.Right', ''),
        (2, 'rotate8', 'Alu.Left', ' (m.Flags()).carry'),
        (3, 'rotate8', 'Alu.Right', ' (m.Flags()).carry'),
        (4, 'shiftArithmetic8', 'Alu.Left', ''),
        (5, 'shiftArithmetic8', 'Alu.Right', ''),
        (6, 'shiftLogical8', 'Alu.Left', ''),
        (7, 'shiftLogical8', 'Alu.Right', ''),
    ]
    for y, fn, d, carry in rot:
        fill[(y << 3) | 6] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                             'm.PassTime 1', f'm.Regs.SetWz({ind})', 'm.Fetch()', 'let lhs = m.Read(m.Regs.Wz())',
                             'm.PassTime 1', f'let struct (r, f) = Alu.{fn} lhs {d}{carry}',
                             'm.Write(m.Regs.Wz(), r)', 'm.SetFlags f')
    for b in range(8):
        bit = 1 << b
        fill[0x40 | (b << 3) | 6] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                                      'm.PassTime 1', f'm.Regs.SetWz({ind})', 'm.Fetch()', 'let lhs = m.Read(m.Regs.Wz())',
                                      'm.PassTime 1', 'let busNoise = (m.Regs.Wz() >>> 8) &&& 0xFF',
                                      f'm.SetFlags(Alu.bit lhs ({bit}) (m.Flags()) busNoise)')
        fill[0x80 | (b << 3) | 6] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                                      'm.PassTime 1', f'm.Regs.SetWz({ind})', 'm.Fetch()', 'let lhs = m.Read(m.Regs.Wz())',
                                      'm.PassTime 1', f'm.Write(m.Regs.Wz(), lhs &&& ~~~({bit}))')
        fill[0xC0 | (b << 3) | 6] = lines('m.Fetch()', 'm.Fetch()', 'let d = m.ReadImm()', 'let d = if d >= 0x80 then d - 0x100 else d',
                                      'm.PassTime 1', f'm.Regs.SetWz({ind})', 'm.Fetch()', 'let lhs = m.Read(m.Regs.Wz())',
                                      'm.PassTime 1', f'm.Write(m.Regs.Wz(), lhs ||| ({bit}))')
    return fill


dd_cb_fill = ddcb_fill(False)
fd_cb_fill = ddcb_fill(True)


def apply_fill(tname, fill):
    t = tables.setdefault(tname, {})
    for slot in range(256):
        if slot not in t:
            b = fill.get(slot)
            if b is not None:
                t[slot] = b


apply_fill('main', main_fill)
apply_fill('cb', cb_fill)
apply_fill('ed', ed_fill)
apply_fill('dd', dd_fill)
apply_fill('fd', fd_fill)
apply_fill('dd_cb', dd_cb_fill)
apply_fill('fd_cb', fd_cb_fill)

print('coverage after ISA fill:')
for tname in ['main', 'dd', 'fd', 'ed', 'cb', 'dd_cb', 'fd_cb']:
    print('  %s: %d entries' % (tname, len(tables.get(tname, {}))))

# ---------------------------------------------------------------------------
# Emit.
# ---------------------------------------------------------------------------
def emit_table(tname):
    t = tables.get(tname, {})
    out = [f'  let {tname} : (Machine -> unit) option[] = [|']
    for byte in range(256):
        b = t.get(byte)
        if b is None:
            out.append('    None')
        else:
            # Normalize indentation: strip the body's common leading
            # whitespace, re-indent at 10 (corpus arms carry 10, templates 0).
            nonempty = [l for l in b if l.strip()]
            minind = min(len(l) - len(l.lstrip()) for l in nonempty) if nonempty else 0
            out.append('    Some (fun (m: Machine) ->')
            for l in b:
                if l.strip():
                    out.append('          ' + l[minind:])
                else:
                    out.append('')
            out.append('    )')
    out.append('  |]')
    return '\n'.join(out)


header = '''namespace Jetpac2.Core

/// Generic per-opcode instruction table. The per-address Generated.fs page
/// tables are merged by opcode (Phase A), then completed to the full Z80 ISA
/// (Phase 2) with arms written from the oracle's Z80Ops semantics; the
/// --test z80ops harness validates every slot against the oracle. The DD/FD
/// tables hold the IX/IY-specific forms; other DD/FD slots are
/// prefix-ignored (the base opcode body). Slots dispatched only via the
/// step's prefix routing (0xCB/0xDD/0xED/0xFD in main) are None.
module Z80Table =
'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(header)
    for tname in ['main', 'dd', 'fd', 'ed', 'cb', 'dd_cb', 'fd_cb']:
        f.write(emit_table(tname) + '\n\n')
    f.write('''  let step (m: Machine) =
    let pc = m.Regs.Pc()
    let op = int m.Memory.[pc &&& 0xFFFF]
    let run (table: (Machine -> unit) option[]) (idx: int) =
      match table.[idx] with
      | Some f -> f m
      | None -> failwithf "no code at 0x%04X" pc
    match op with
    | 0xDD ->
      let second = int m.Memory.[(pc + 1) &&& 0xFFFF]
      match second with
      | 0xCB -> run dd_cb (int m.Memory.[(pc + 3) &&& 0xFFFF])
      | 0xDD | 0xFD | 0xED -> run dd second // nested prefix: consume+dispatch
      | _ -> run dd second
    | 0xFD ->
      let second = int m.Memory.[(pc + 1) &&& 0xFFFF]
      match second with
      | 0xCB -> run fd_cb (int m.Memory.[(pc + 3) &&& 0xFFFF])
      | 0xDD | 0xFD | 0xED -> run fd second
      | _ -> run fd second
    | 0xED -> run ed (int m.Memory.[(pc + 1) &&& 0xFFFF])
    | 0xCB -> run cb (int m.Memory.[(pc + 1) &&& 0xFFFF])
    | _ -> run main op

  /// Install the generic step into Machine (replaces the per-address pages).
  let EnsureInstalled () =
    Machine.GeneratedStep <- step
''')

print(f'wrote {OUT}')
