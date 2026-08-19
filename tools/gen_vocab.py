#!/usr/bin/env python3
"""Phases 2+4: generate the CE vocabulary (Z80Vocab.fs) and the decoder
table (Z80Decode.fs).

Every named op carries its assembled encoding and dispatches its Run to the
validated Z80Table slot (runOf). Parameterized ops are functions of their
operands (LD_A n, LD_HL nn, ADD_A_IXd d, ...). Z80Decode.fs maps instruction
bytes -> CE op + F# source text (the reverse direction, Phase 4); fixed
rows have all-fixed prefixes, parameterized rows use a byte mask (wildcards
for operands, e.g. DD CB d X has the d masked out).
"""
import re

OUT_VOCAB = 'src/Jetpac2.Core/Z80Vocab.fs'
OUT_DECODE = 'src/Jetpac2.Core/Z80Decode.fs'

r8 = ['B', 'C', 'D', 'E', 'H', 'L', 'A']          # register order 0..6 (A=7)
r8idx = {'B': 0, 'C': 1, 'D': 2, 'E': 3, 'H': 4, 'L': 5, 'A': 7}
pairs = [('BC', 'R16.BC'), ('DE', 'R16.DE'), ('HL', 'R16.HL'), ('SP', 'R16.SP')]
alu = [('ADD', 'add8', 0, 'false'), ('ADC', 'add8', 1, '(m.Flags()).carry'),
       ('SUB', 'sub8', 2, 'false'), ('SBC', 'sub8', 3, '(m.Flags()).carry'),
       ('AND', 'and8', 4, ''), ('XOR', 'xor8', 5, ''), ('OR', 'or8', 6, ''), ('CP', 'cmp8', 7, '')]
cc = [('NZ', 0), ('Z', 1), ('NC', 2), ('C', 3), ('PO', 4), ('PE', 5), ('P', 6), ('M', 7)]
rot = [('RLC', 0), ('RRC', 1), ('RL', 2), ('RR', 3), ('SLA', 4), ('SRA', 5), ('SRL', 7)]

entries = []      # F# op definitions
instances = []    # (name, allOps-expr, kind)
rows = []         # (prefix_bytes, mask_bytes, make_fs, format_fs, label_name)


def cb_slot(base, r):
    return base | (r8idx[r] if r != 'HL' else 6)


def read_of(off, size, signed):
    if signed:
        return f'(int (sbyte img.[pc + {off}]))'
    if size == 1:
        return f'(int img.[pc + {off}])'
    return f'((int img.[pc + {off}]) ||| ((int img.[pc + {off + 1}]) <<< 8))'


def fmt_of(off, size, signed):
    if signed:
        return f'sprintf "%d" (sbyte img.[pc + {off}])'
    if size == 1:
        return f'sprintf "0x%02X" (int img.[pc + {off}])'
    return f'sprintf "0x%04X" ((int img.[pc + {off}]) ||| ((int img.[pc + {off + 1}]) <<< 8))'


def add_row(fixed, params, name, label):
    """fixed: [(off, byte)]; params: [(pname, off, size, signed)]."""
    plen = 0
    for off, _ in fixed:
        plen = max(plen, off + 1)
    for _, off, size, _ in params:
        plen = max(plen, off + size)
    prefix = bytearray(plen)
    mask = bytearray(plen)
    for off, b in fixed:
        prefix[off] = b
        mask[off] = 0xFF
    reads = [read_of(off, size, signed) for (_, off, size, signed) in params]
    fmts = [fmt_of(off, size, signed) for (_, off, size, signed) in params]
    make = f'(fun img pc -> Z80Vocab.{name} {" ".join(reads)})'
    args = ' + " " + '.join(fmts) if fmts else '""'
    format_ = f'(fun img pc -> "Z80Vocab.{name}" + (if {len(fmts)} = 0 then "" else " " + ({args})))'
    lbl = f'Some "{label}"' if label else 'None'
    rows.append((bytes(prefix), bytes(mask), make, format_, lbl))


def emit(name, mnemonic, bytes_lit, slot_expr, kind, label=None):
    """Fixed-encoding op (no operands)."""
    entries.append(f'  let {name} =\n    mkOp "{mnemonic}" {bytes_lit} (runOf {slot_expr})')
    instances.append((name, name, kind))
    b = [int(x, 16) for x in re.findall(r'0x([0-9a-fA-F]{2})uy', bytes_lit)]
    fixed = list(enumerate(b))
    add_row(fixed, [], name, label)


def emitP(name, mnemonic, fixed, params, slot_expr, kind, label=None, sample=None):
    """Parameterized op: function of its operands; decoder row with wildcards."""
    byoff = {}
    for off, b in fixed:
        byoff[off] = f'0x{b:02x}uy'
    for pname, off, size, signed in params:
        byoff[off] = f'byte ({pname} &&& 0xFF)'
        if size == 2:
            byoff[off + 1] = f'byte (({pname} >>> 8) &&& 0xFF)'
    last = max(byoff) + 1
    bytes_expr = '[| ' + '; '.join(byoff[i] for i in range(last)) + ' |]'
    ps = ' '.join(f'({pname} : int)' for pname, _, _, _ in params)
    entries.append(f'  let {name} {ps} =\n    mkOp "{mnemonic}" {bytes_expr} (runOf {slot_expr})')
    if sample is None:
        sample = ' '.join('0x1234' if size == 2 else '0x55' for _, _, size, _ in params)
    instances.append((name, f'{name} {sample}', kind))
    add_row(fixed, params, name, label)


# ---- 8-bit loads: LD r,r' and LD r,(HL) / LD (HL),r ------------------------
for d in r8:
    for s in r8:
        slot = 0x40 | (r8idx[d] << 3) | r8idx[s]
        if slot == 0x76:
            continue  # HALT
        emit(f'LD_{d}_{s}', f'LD {d},{s}', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'default')
for d in r8:
    slot = 0x40 | (r8idx[d] << 3) | 6
    emit(f'LD_{d}_PTR_HL', f'LD {d},(HL)', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'hl')
for s in r8:
    slot = 0x40 | (6 << 3) | r8idx[s]
    emit(f'LD_PTR_HL_{s}', f'LD (HL),{s}', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'hl')

# ---- LD r,n ----------------------------------------------------------------
for d in r8:
    slot = 0x06 | (r8idx[d] << 3)
    emitP(f'LD_{d}', f'LD {d},n', [(0, slot)], [('n', 1, 1, False)], f'Z80Table.main 0x{slot:02X}', 'default')

# ---- LD A,(nn) / LD (nn),A / LD A,(BC|DE) / LD (BC|DE),A -------------------
emitP('LD_A_ptr', 'LD A,(nn)', [(0, 0x3A)], [('nn', 1, 2, False)], 'Z80Table.main 0x3A', 'nn')
emitP('LD_ptr_A', 'LD (nn),A', [(0, 0x32)], [('nn', 1, 2, False)], 'Z80Table.main 0x32', 'nn')
emit('LD_A_BC', 'LD A,(BC)', '[| 0x0Auy |]', 'Z80Table.main 0x0A', 'hl')
emit('LD_A_DE', 'LD A,(DE)', '[| 0x1Auy |]', 'Z80Table.main 0x1A', 'hl')
emit('LD_BC_A', 'LD (BC),A', '[| 0x02uy |]', 'Z80Table.main 0x02', 'hl')
emit('LD_DE_A', 'LD (DE),A', '[| 0x12uy |]', 'Z80Table.main 0x12', 'hl')

# ---- 16-bit loads ----------------------------------------------------------
for (name, rr), base in zip(pairs, [0x01, 0x11, 0x21, 0x31]):
    emitP(f'LD_{name}', f'LD {name},nn', [(0, base)], [('imm', 1, 2, False)], f'Z80Table.main 0x{base:02X}', 'default')
emitP('LD_IX', 'LD IX,nn', [(0, 0xDD), (1, 0x21)], [('imm', 2, 2, False)], 'Z80Table.dd 0x21', 'default')
emitP('LD_IY', 'LD IY,nn', [(0, 0xFD), (1, 0x21)], [('imm', 2, 2, False)], 'Z80Table.fd 0x21', 'default')
emitP('LD_HL_ptr', 'LD HL,(nn)', [(0, 0x2A)], [('nn', 1, 2, False)], 'Z80Table.main 0x2A', 'nn')
emitP('LD_ptr_HL', 'LD (nn),HL', [(0, 0x22)], [('nn', 1, 2, False)], 'Z80Table.main 0x22', 'nn')
emitP('LD_IX_ptr', 'LD IX,(nn)', [(0, 0xDD), (1, 0x2A)], [('nn', 2, 2, False)], 'Z80Table.dd 0x2A', 'nn')
emitP('LD_ptr_IX', 'LD (nn),IX', [(0, 0xDD), (1, 0x22)], [('nn', 2, 2, False)], 'Z80Table.dd 0x22', 'nn')
emitP('LD_IY_ptr', 'LD IY,(nn)', [(0, 0xFD), (1, 0x2A)], [('nn', 2, 2, False)], 'Z80Table.fd 0x2A', 'nn')
emitP('LD_ptr_IY', 'LD (nn),IY', [(0, 0xFD), (1, 0x22)], [('nn', 2, 2, False)], 'Z80Table.fd 0x22', 'nn')
for (name, rr), q in zip([('BC', 'R16.BC'), ('DE', 'R16.DE')], [0x00, 0x10]):
    emitP(f'LD_ptr_{name}', f'LD (nn),{name}', [(0, 0xED), (1, 0x43 | q)], [('nn', 2, 2, False)], f'Z80Table.ed 0x{0x43 | q:02X}', 'nn')
    emitP(f'LD_{name}_ptr', f'LD {name},(nn)', [(0, 0xED), (1, 0x4B | q)], [('nn', 2, 2, False)], f'Z80Table.ed 0x{0x4B | q:02X}', 'nn')
emitP('LD_ptr_SP', 'LD (nn),SP', [(0, 0xED), (1, 0x73)], [('nn', 2, 2, False)], 'Z80Table.ed 0x73', 'nn')
emitP('LD_SP_ptr', 'LD SP,(nn)', [(0, 0xED), (1, 0x7B)], [('nn', 2, 2, False)], 'Z80Table.ed 0x7B', 'nn')
emit('LD_SP_HL', 'LD SP,HL', '[| 0xF9uy |]', 'Z80Table.main 0xF9', 'default')
emit('LD_SP_IX', 'LD SP,IX', '[| 0xDDuy; 0xF9uy |]', 'Z80Table.dd 0xF9', 'default')
emit('LD_SP_IY', 'LD SP,IY', '[| 0xFDuy; 0xF9uy |]', 'Z80Table.fd 0xF9', 'default')
emit('LD_A_I', 'LD A,I', '[| 0xEDuy; 0x57uy |]', 'Z80Table.ed 0x57', 'default')
emit('LD_A_R', 'LD A,R', '[| 0xEDuy; 0x5Fuy |]', 'Z80Table.ed 0x5F', 'default')
emit('LD_I_A', 'LD I,A', '[| 0xEDuy; 0x47uy |]', 'Z80Table.ed 0x47', 'default')
emit('LD_R_A', 'LD R,A', '[| 0xEDuy; 0x4Fuy |]', 'Z80Table.ed 0x4F', 'default')
# IXH/IXL loads
for (nm, slot) in [('LD_B_IXH', 0x44), ('LD_B_IXL', 0x45), ('LD_C_IXH', 0x4C), ('LD_C_IXL', 0x4D),
                   ('LD_D_IXH', 0x54), ('LD_D_IXL', 0x55), ('LD_E_IXH', 0x5C), ('LD_E_IXL', 0x5D),
                   ('LD_IXH_B', 0x60), ('LD_IXH_C', 0x61), ('LD_IXH_D', 0x62), ('LD_IXH_E', 0x63),
                   ('LD_IXH_A', 0x67), ('LD_IXL_B', 0x68), ('LD_IXL_C', 0x69), ('LD_IXL_D', 0x6A),
                   ('LD_IXL_E', 0x6B), ('LD_IXL_A', 0x6F)]:
    mnem = nm[3:].replace('_', ',')
    emit(nm, f'LD {mnem}', f'[| 0xDDuy; 0x{slot:02x}uy |]', f'Z80Table.dd 0x{slot:02X}', 'default')
emitP('LD_IXH_n', 'LD IXH,n', [(0, 0xDD), (1, 0x26)], [('n', 2, 1, False)], 'Z80Table.dd 0x26', 'default')
emitP('LD_IXL_n', 'LD IXL,n', [(0, 0xDD), (1, 0x2E)], [('n', 2, 1, False)], 'Z80Table.dd 0x2E', 'default')
# LD r,(IX+d) / LD (IX+d),r — both IX and IY.
for dreg, slot, prefix, tbl in [('B', 0x46, 0xDD, 'dd'), ('C', 0x4E, 0xDD, 'dd'), ('D', 0x56, 0xDD, 'dd'),
                                ('E', 0x5E, 0xDD, 'dd'), ('H', 0x66, 0xDD, 'dd'), ('L', 0x6E, 0xDD, 'dd'),
                                ('A', 0x7E, 0xDD, 'dd'), ('B', 0x46, 0xFD, 'fd'), ('C', 0x4E, 0xFD, 'fd'),
                                ('D', 0x56, 0xFD, 'fd'), ('E', 0x5E, 0xFD, 'fd'), ('H', 0x66, 0xFD, 'fd'),
                                ('L', 0x6E, 0xFD, 'fd'), ('A', 0x7E, 0xFD, 'fd')]:
    ix = 'IX' if prefix == 0xDD else 'IY'
    emitP(f'LD_{dreg}_{ix}d', f'LD {dreg},({ix}+d)', [(0, prefix), (1, slot)],
          [('d', 2, 1, True)], f'Z80Table.{tbl} 0x{slot:02X}', 'ixd' if prefix == 0xDD else 'iyd')
for sreg, slot, prefix, tbl in [('B', 0x70, 0xDD, 'dd'), ('C', 0x71, 0xDD, 'dd'), ('D', 0x72, 0xDD, 'dd'),
                                ('E', 0x73, 0xDD, 'dd'), ('H', 0x74, 0xDD, 'dd'), ('L', 0x75, 0xDD, 'dd'),
                                ('A', 0x77, 0xDD, 'dd'), ('B', 0x70, 0xFD, 'fd'), ('C', 0x71, 0xFD, 'fd'),
                                ('D', 0x72, 0xFD, 'fd'), ('E', 0x73, 0xFD, 'fd'), ('H', 0x74, 0xFD, 'fd'),
                                ('L', 0x75, 0xFD, 'fd'), ('A', 0x77, 0xFD, 'fd')]:
    ix = 'IX' if prefix == 0xDD else 'IY'
    emitP(f'LD_{ix}d_{sreg}', f'LD ({ix}+d),{sreg}', [(0, prefix), (1, slot)],
          [('d', 2, 1, True)], f'Z80Table.{tbl} 0x{slot:02X}', 'ixd' if prefix == 0xDD else 'iyd')

# ---- ALU -------------------------------------------------------------------
for (aname, fn, y, carry) in alu:
    for r in r8:
        slot = 0x80 | (y << 3) | r8idx[r]
        emit(f'{aname}_A_{r}', f'{aname} A,{r}', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'default')
    slot = 0x80 | (y << 3) | 6
    emit(f'{aname}_A_PTR_HL', f'{aname} A,(HL)', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'hl')
    nslot = 0xC6 | (y << 3)
    emitP(f'{aname}_A_n', f'{aname} A,n', [(0, nslot)], [('n', 1, 1, False)], f'Z80Table.main 0x{nslot:02X}', 'default')
    emitP(f'{aname}_A_IXd', f'{aname} A,(IX+d)', [(0, 0xDD), (1, 0x86 | (y << 3))],
          [('d', 2, 1, True)], f'Z80Table.dd 0x{0x86 | (y << 3):02X}', 'ixd')
    emitP(f'{aname}_A_IYd', f'{aname} A,(IY+d)', [(0, 0xFD), (1, 0x86 | (y << 3))],
          [('d', 2, 1, True)], f'Z80Table.fd 0x{0x86 | (y << 3):02X}', 'iyd')
# 16-bit ALU
for (name, rr), q in zip(pairs, [0, 1, 2, 3]):
    for (aname, fn) in [('ADD', 'add16'), ('ADC', 'adc16'), ('SBC', 'sbc16')]:
        if aname == 'ADD':
            slot = 0x09 | (q << 4)
            emit(f'ADD_HL_{name}', f'ADD HL,{name}', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'default')
        else:
            slot = 0x4A | (q << 4) if aname == 'ADC' else 0x42 | (q << 4)
            emit(f'{aname}_HL_{name}', f'{aname} HL,{name}', f'[| 0xEDuy; 0x{slot:02x}uy |]', f'Z80Table.ed 0x{slot:02X}', 'default')
for (name, rr), q in zip(pairs, [0, 1, 2, 3]):
    slot = 0x09 | (q << 4)
    emit(f'ADD_IX_{name}', f'ADD IX,{name}', f'[| 0xDDuy; 0x{slot:02x}uy |]', f'Z80Table.dd 0x{slot:02X}', 'default')
    emit(f'ADD_IY_{name}', f'ADD IY,{name}', f'[| 0xFDuy; 0x{slot:02x}uy |]', f'Z80Table.fd 0x{slot:02X}', 'default')

# ---- INC/DEC ---------------------------------------------------------------
for r in r8:
    emit(f'INC_{r}', f'INC {r}', f'[| 0x{0x04 | (r8idx[r] << 3):02x}uy |]', f'Z80Table.main 0x{0x04 | (r8idx[r] << 3):02X}', 'default')
    emit(f'DEC_{r}', f'DEC {r}', f'[| 0x{0x05 | (r8idx[r] << 3):02x}uy |]', f'Z80Table.main 0x{0x05 | (r8idx[r] << 3):02X}', 'default')
emit('INC_PTR_HL', 'INC (HL)', '[| 0x34uy |]', 'Z80Table.main 0x34', 'hl')
emit('DEC_PTR_HL', 'DEC (HL)', '[| 0x35uy |]', 'Z80Table.main 0x35', 'hl')
emitP('INC_PTR_IXd', 'INC (IX+d)', [(0, 0xDD), (1, 0x34)], [('d', 2, 1, True)], 'Z80Table.dd 0x34', 'ixd')
emitP('DEC_PTR_IXd', 'DEC (IX+d)', [(0, 0xDD), (1, 0x35)], [('d', 2, 1, True)], 'Z80Table.dd 0x35', 'ixd')
emitP('LD_PTR_IXd_n', 'LD (IX+d),n', [(0, 0xDD), (1, 0x36)], [('d', 2, 1, True), ('n', 3, 1, False)],
      'Z80Table.dd 0x36', 'ixd', sample='0x02 0x55')
for (name, rr), q in zip(pairs, [0, 1, 2, 3]):
    emit(f'INC_{name}', f'INC {name}', f'[| 0x{0x03 | (q << 4):02x}uy |]', f'Z80Table.main 0x{0x03 | (q << 4):02X}', 'default')
    emit(f'DEC_{name}', f'DEC {name}', f'[| 0x{0x0B | (q << 4):02x}uy |]', f'Z80Table.main 0x{0x0B | (q << 4):02X}', 'default')
emit('INC_IX', 'INC IX', '[| 0xDDuy; 0x23uy |]', 'Z80Table.dd 0x23', 'default')
emit('DEC_IX', 'DEC IX', '[| 0xDDuy; 0x2Buy |]', 'Z80Table.dd 0x2B', 'default')
emit('INC_IXH', 'INC IXH', '[| 0xDDuy; 0x24uy |]', 'Z80Table.dd 0x24', 'default')
emit('DEC_IXH', 'DEC IXH', '[| 0xDDuy; 0x25uy |]', 'Z80Table.dd 0x25', 'default')
emit('INC_IXL', 'INC IXL', '[| 0xDDuy; 0x2Cuy |]', 'Z80Table.dd 0x2C', 'default')
emit('DEC_IXL', 'DEC IXL', '[| 0xDDuy; 0x2Duy |]', 'Z80Table.dd 0x2D', 'default')

# ---- PUSH/POP --------------------------------------------------------------
for (name, rr), q in zip([('BC', 'BC'), ('DE', 'DE'), ('HL', 'HL'), ('AF', 'AF')], [0, 1, 2, 3]):
    emit(f'PUSH_{name}', f'PUSH {name}', f'[| 0x{0xC5 | (q << 4):02x}uy |]', f'Z80Table.main 0x{0xC5 | (q << 4):02X}', 'sp')
    emit(f'POP_{name}', f'POP {name}', f'[| 0x{0xC1 | (q << 4):02x}uy |]', f'Z80Table.main 0x{0xC1 | (q << 4):02X}', 'sp')
emit('PUSH_IX', 'PUSH IX', '[| 0xDDuy; 0xE5uy |]', 'Z80Table.dd 0xE5', 'sp')
emit('POP_IX', 'POP IX', '[| 0xDDuy; 0xE1uy |]', 'Z80Table.dd 0xE1', 'sp')

# ---- CB rotate/shift family -------------------------------------------------
for (rname, y) in rot:
    for r in r8:
        slot = cb_slot(y << 3, r)
        emit(f'{rname}_{r}', f'{rname} {r}', f'[| 0xCBuy; 0x{slot:02x}uy |]', f'Z80Table.cb 0x{slot:02X}', 'default')
    slot = cb_slot(y << 3, 'HL')
    emit(f'{rname}_PTR_HL', f'{rname} (HL)', f'[| 0xCBuy; 0x{slot:02x}uy |]', f'Z80Table.cb 0x{slot:02X}', 'hl')
    for prefix, tbl, ix in [(0xDD, 'dd_cb', 'IX'), (0xFD, 'fd_cb', 'IY')]:
        # DD/FD CB d X: the d (offset 2) is the operand, X (offset 3) fixed.
        emitP(f'{rname}_PTR_{ix}d', f'{rname} ({ix}+d)', [(0, prefix), (1, 0xCB), (3, slot)],
              [('d', 2, 1, True)], f'Z80Table.{tbl} 0x{slot:02X}', 'ixd' if prefix == 0xDD else 'iyd')

# ---- BIT/RES/SET ------------------------------------------------------------
for b in range(8):
    for (opname, base) in [('BIT', 0x40), ('RES', 0x80), ('SET', 0xC0)]:
        for r in r8:
            slot = cb_slot(base | (b << 3), r)
            emit(f'{opname}{b}_{r}', f'{opname} {b},{r}', f'[| 0xCBuy; 0x{slot:02x}uy |]', f'Z80Table.cb 0x{slot:02X}', 'default')
        slot = cb_slot(base | (b << 3), 'HL')
        emit(f'{opname}{b}_PTR_HL', f'{opname} {b},(HL)', f'[| 0xCBuy; 0x{slot:02x}uy |]', f'Z80Table.cb 0x{slot:02X}', 'hl')
        for prefix, tbl, ix in [(0xDD, 'dd_cb', 'IX'), (0xFD, 'fd_cb', 'IY')]:
            emitP(f'{opname}{b}_PTR_{ix}d', f'{opname} {b},({ix}+d)', [(0, prefix), (1, 0xCB), (3, slot)],
                  [('d', 2, 1, True)], f'Z80Table.{tbl} 0x{slot:02X}', 'ixd' if prefix == 0xDD else 'iyd')

# ---- control flow -----------------------------------------------------------
emitP('JP_nn', 'JP nn', [(0, 0xC3)], [('nn', 1, 2, False)], 'Z80Table.main 0xC3', 'jp', label='JP_LBL', sample='0x8003')
for (cname, cv) in cc:
    emitP(f'JP_{cname}', f'JP {cname},nn', [(0, 0xC2 | (cv << 3))], [('nn', 1, 2, False)],
          f'Z80Table.main 0x{0xC2 | (cv << 3):02X}', 'jp', label=f'JP_{cname}_LBL', sample='0x8003')
emit('JP_HL', 'JP (HL)', '[| 0xE9uy |]', 'Z80Table.main 0xE9', 'jp')
emit('JP_IX', 'JP (IX)', '[| 0xDDuy; 0xE9uy |]', 'Z80Table.dd 0xE9', 'jp')
emit('JP_IY', 'JP (IY)', '[| 0xFDuy; 0xE9uy |]', 'Z80Table.fd 0xE9', 'jp')
emitP('JR_e', 'JR e', [(0, 0x18)], [('e', 1, 1, False)], 'Z80Table.main 0x18', 'jr', label='JR_LBL', sample='0x00')
for (cname, cv) in cc[:4]:
    emitP(f'JR_{cname}', f'JR {cname},e', [(0, 0x20 | (cv << 3))], [('e', 1, 1, False)],
          f'Z80Table.main 0x{0x20 | (cv << 3):02X}', 'jr', label=f'JR_{cname}_LBL', sample='0x00')
emitP('DJNZ_e', 'DJNZ e', [(0, 0x10)], [('e', 1, 1, False)], 'Z80Table.main 0x10', 'djnz', label='DJNZ_LBL', sample='0x00')
emitP('CALL_nn', 'CALL nn', [(0, 0xCD)], [('nn', 1, 2, False)], 'Z80Table.main 0xCD', 'call', label='CALL_LBL', sample='0x8003')
for (cname, cv) in cc:
    emitP(f'CALL_{cname}', f'CALL {cname},nn', [(0, 0xC4 | (cv << 3))], [('nn', 1, 2, False)],
          f'Z80Table.main 0x{0xC4 | (cv << 3):02X}', 'call', label=f'CALL_{cname}_LBL', sample='0x8003')
emit('RET', 'RET', '[| 0xC9uy |]', 'Z80Table.main 0xC9', 'ret')
for (cname, cv) in cc:
    emit(f'RET_{cname}', f'RET {cname}', f'[| 0x{0xC0 | (cv << 3):02x}uy |]', f'Z80Table.main 0x{0xC0 | (cv << 3):02X}', 'ret')
emit('RETI', 'RETI', '[| 0xEDuy; 0x4Duy |]', 'Z80Table.ed 0x4D', 'ret')
emit('RETN', 'RETN', '[| 0xEDuy; 0x45uy |]', 'Z80Table.ed 0x45', 'ret')
for p in range(8):
    slot = 0xC7 | (p << 3)
    emit(f'RST_{0x00 + (p << 3):02X}', f'RST 0x{p << 3:02X}', f'[| 0x{slot:02x}uy |]', f'Z80Table.main 0x{slot:02X}', 'rst')

# ---- misc / io / block -------------------------------------------------------
misc = [
    ('NOP', 'NOP', '[| 0x00uy |]', 'Z80Table.main 0x00', 'default'),
    ('HALT', 'HALT', '[| 0x76uy |]', 'Z80Table.main 0x76', 'halt'),
    ('DI', 'DI', '[| 0xF3uy |]', 'Z80Table.main 0xF3', 'default'),
    ('EI', 'EI', '[| 0xFBuy |]', 'Z80Table.main 0xFB', 'default'),
    ('SCF', 'SCF', '[| 0x37uy |]', 'Z80Table.main 0x37', 'default'),
    ('CCF', 'CCF', '[| 0x3Fuy |]', 'Z80Table.main 0x3F', 'default'),
    ('CPL', 'CPL', '[| 0x2Fuy |]', 'Z80Table.main 0x2F', 'default'),
    ('DAA', 'DAA', '[| 0x27uy |]', 'Z80Table.main 0x27', 'default'),
    ('NEG', 'NEG', '[| 0xEDuy; 0x44uy |]', 'Z80Table.ed 0x44', 'default'),
    ('EX_DE_HL', 'EX DE,HL', '[| 0xEBuy |]', 'Z80Table.main 0xEB', 'default'),
    ('EX_AF_AF', "EX AF,AF'", '[| 0x08uy |]', 'Z80Table.main 0x08', 'default'),
    ('EXX', 'EXX', '[| 0xD9uy |]', 'Z80Table.main 0xD9', 'default'),
    ('RLCA', 'RLCA', '[| 0x07uy |]', 'Z80Table.main 0x07', 'default'),
    ('RRCA', 'RRCA', '[| 0x0Fuy |]', 'Z80Table.main 0x0F', 'default'),
    ('RLA', 'RLA', '[| 0x17uy |]', 'Z80Table.main 0x17', 'default'),
    ('RRA', 'RRA', '[| 0x1Fuy |]', 'Z80Table.main 0x1F', 'default'),
    ('IM_0', 'IM 0', '[| 0xEDuy; 0x46uy |]', 'Z80Table.ed 0x46', 'default'),
    ('IM_1', 'IM 1', '[| 0xEDuy; 0x56uy |]', 'Z80Table.ed 0x56', 'default'),
    ('IM_2', 'IM 2', '[| 0xEDuy; 0x5Euy |]', 'Z80Table.ed 0x5E', 'default'),
    ('RRD', 'RRD', '[| 0xEDuy; 0x67uy |]', 'Z80Table.ed 0x67', 'hl'),
    ('RLD', 'RLD', '[| 0xEDuy; 0x6Fuy |]', 'Z80Table.ed 0x6F', 'hl'),
    ('LDI', 'LDI', '[| 0xEDuy; 0xA0uy |]', 'Z80Table.ed 0xA0', 'block'),
    ('LDD', 'LDD', '[| 0xEDuy; 0xA8uy |]', 'Z80Table.ed 0xA8', 'block'),
    ('LDIR', 'LDIR', '[| 0xEDuy; 0xB0uy |]', 'Z80Table.ed 0xB0', 'block'),
    ('LDDR', 'LDDR', '[| 0xEDuy; 0xB8uy |]', 'Z80Table.ed 0xB8', 'block'),
    ('CPI', 'CPI', '[| 0xEDuy; 0xA1uy |]', 'Z80Table.ed 0xA1', 'block'),
    ('CPD', 'CPD', '[| 0xEDuy; 0xA9uy |]', 'Z80Table.ed 0xA9', 'block'),
    ('CPIR', 'CPIR', '[| 0xEDuy; 0xB1uy |]', 'Z80Table.ed 0xB1', 'block'),
    ('CPDR', 'CPDR', '[| 0xEDuy; 0xB9uy |]', 'Z80Table.ed 0xB9', 'block'),
    ('EX_SP_HL', 'EX (SP),HL', '[| 0xE3uy |]', 'Z80Table.main 0xE3', 'exsp'),
    ('EX_SP_IX', 'EX (SP),IX', '[| 0xDDuy; 0xE3uy |]', 'Z80Table.dd 0xE3', 'exsp'),
    ('EX_SP_IY', 'EX (SP),IY', '[| 0xFDuy; 0xE3uy |]', 'Z80Table.fd 0xE3', 'exsp'),
]
for (name, mnemonic, bl, slot, kind) in misc:
    emit(name, mnemonic, bl, slot, kind)
emitP('IN_A_n', 'IN A,(n)', [(0, 0xDB)], [('n', 1, 1, False)], 'Z80Table.main 0xDB', 'io')
emitP('OUT_n_A', 'OUT (n),A', [(0, 0xD3)], [('n', 1, 1, False)], 'Z80Table.main 0xD3', 'io')
for r in r8:
    emit(f'IN_{r}_C', f'IN {r},(C)', f'[| 0xEDuy; 0x{0x40 | (r8idx[r] << 3):02x}uy |]', f'Z80Table.ed 0x{0x40 | (r8idx[r] << 3):02X}', 'io')
    emit(f'OUT_C_{r}', f'OUT (C),{r}', f'[| 0xEDuy; 0x{0x41 | (r8idx[r] << 3):02x}uy |]', f'Z80Table.ed 0x{0x41 | (r8idx[r] << 3):02X}', 'io')
emit('IN_PTR_C', 'IN (C)', '[| 0xEDuy; 0x70uy |]', 'Z80Table.ed 0x70', 'io')
emit('OUT_C_0', 'OUT (C),0', '[| 0xEDuy; 0x71uy |]', 'Z80Table.ed 0x71', 'io')

# ---- emit -------------------------------------------------------------------
rows.sort(key=lambda r: sum(1 for m in r[1] if m == 0xFF), reverse=True)

header = '''namespace Jetpac2.Core

/// The CE instruction vocabulary (generated by tools/gen_z80table.py). Every
/// named op carries its assembled encoding and dispatches its Run to the
/// validated Z80Table slot, so each op is track-perfect by construction and
/// the per-op oracle test (--test z80ops) proves it. Memory operands use the
/// PTR suffix ((HL), (nn)); (IX+d)/(IY+d) use IXd/IYd; BIT/RES/SET bake the
/// bit number into the name (BIT0_B ... SET7_PTR_IYd).
module Z80Vocab =

  open Z80
'''
with open(OUT_VOCAB, 'w', encoding='utf-8') as f:
    f.write(header)
    f.write('\n'.join(entries))
    f.write('\n\n  /// Concrete instances for the per-op oracle test:\n')
    f.write('  /// (name, op, setup kind).\n')
    f.write('  let allOps : (string * Z80Op * string) list =\n')
    f.write('    [\n')
    for name, expr, kind in instances:
        f.write(f'    ("{name}", {expr}, "{kind}")\n')
    f.write('    ]\n')

dec_header = '''namespace Jetpac2.Core

/// Decoder table (generated): instruction bytes -> CE op (Make) and F#
/// source text (Format), the reverse of assemble. Rows carry a byte mask
/// (0xFF = fixed, 0x00 = operand wildcard, e.g. DD CB d X has the d masked
/// out); `LabelName` marks relative/absolute jumps so Z80CE can emit
/// symbolic labels.
module Z80Decode =
  open Jetpac2.Core

  type Row =
    { Prefix: byte[]
      Mask: byte[]
      Make: byte[] -> int -> Z80Op
      Format: byte[] -> int -> string
      LabelName: string option }

  let rows : Row list =
    [
'''
with open(OUT_DECODE, 'w', encoding='utf-8') as f:
    f.write(dec_header)
    for prefix, mask, make, format_, lbl in rows:
        pb = '; '.join('0x%02xuy' % b for b in prefix)
        mb = '; '.join('0x%02xuy' % m for m in mask)
        f.write(f'      {{ Prefix = [| {pb} |]\n')
        f.write(f'        Mask = [| {mb} |]\n')
        f.write(f'        Make = {make}\n')
        f.write(f'        Format = {format_}\n')
        f.write(f'        LabelName = {lbl} }}\n')
    f.write('    ]\n')

print(f'wrote {OUT_VOCAB}: {len(entries)} ops')
print(f'wrote {OUT_DECODE}: {len(rows)} decode rows')
