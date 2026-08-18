#!/usr/bin/env python3
"""Phase A: extract a generic per-opcode Z80 table from the per-address
Generated.fs page tables.

The generated tables key arms by memory ADDRESS; each arm implements the
instruction at that address. This script re-keys them by OPCODE (and by the
second byte for DD/FD/ED/CB prefixes), emitting one implementation per
instruction form into Jetpac2.Core/Z80Table.fs. The port then dispatches on
the byte(s) at the current PC, so any game using a covered opcode runs, and
address-level gaps disappear. The second byte of prefixed instructions is
read from the entry-state memory image the generator translated.
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
# parts: [header, name, body, name, body, ...]

def depth_collect(lines, start):
    """Collect lines of one `(fun (m: Machine) -> ...)` arm starting at
    `start` (the fun header line). Generated arm bodies are line-balanced
    (every line completes its parens); the arm ends at the line that closes
    the fun's opening paren (net depth < 0). Returns (body_lines, next)."""
    k = start
    assert re.match(r'^\s*\(fun \(m: Machine\) ->$', lines[k]), lines[k]
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

arms = []  # (addr, guard_byte or None, body_lines)
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

print(f'parsed {len(arms)} arms from {len(parts) // 2} pages')

# Decompose multi-case (self-modifying) arms and key everything by opcode.
def norm(body):
    return [l.strip() for l in body]

def key_and_body(addr, guard, body):
    """Yield (key, body_lines). key = ('main', byte) or ('dd'|'fd'|'ed'|'cb', byte)."""
    mm = re.match(r'^\s*match m\.Memory\.\[0x[0-9A-F]{4}\] with$', body[0]) if body else None
    if mm:
        # self-modifying site: each case is an ordinary opcode body
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
        print(f'  WARNING: guard-less leaf arm at {addr:04X}: {body[0][:60] if body else "empty"}')
        return
    gaddr, first = guard
    assert gaddr == addr, f'guard address {gaddr:04X} != arm address {addr:04X}'
    if first in (0xDD, 0xFD, 0xED, 0xCB):
        second = mem[(addr + 1) & 0xFFFF]
        name = {0xDD: 'dd', 0xFD: 'fd', 0xED: 'ed', 0xCB: 'cb'}[first]
        if second == 0xCB:
            # DD/FD CB d X: the arm body implements the fourth byte X.
            x = mem[(addr + 3) & 0xFFFF]
            yield ((f'{name}_cb', x), body)
        else:
            yield ((name, second), body)
    else:
        yield (('main', first), body)

tables = {}  # table_name -> {byte: body}
conflicts = []
order = {}
for addr, guard, body in arms:
    for key, b in key_and_body(addr, guard, body):
        tname, byte = key
        tables.setdefault(tname, {})
        prev = tables[tname].get(byte)
        if prev is not None and norm(prev) != norm(b):
            conflicts.append((tname, byte, addr))
        else:
            tables[tname].setdefault(byte, b)
            order.setdefault(tname, []).append(byte)

print('coverage:')
for tname in ['main', 'dd', 'fd', 'ed', 'cb', 'dd_cb', 'fd_cb']:
    t = tables.get(tname, {})
    print(f'  {tname}: {len(t)} entries')

if conflicts:
    print(f'CONFLICTS ({len(conflicts)}): bodies differ for same opcode')
    for c in conflicts[:20]:
        print(f'  {c[0]}[{c[1]:02X}] at {c[2]:04X}')

# Emit.
def emit_table(tname):
    t = tables.get(tname, {})
    out = [f'  let {tname} : (Machine -> unit) option[] = [|']
    for byte in range(256):
        b = t.get(byte)
        if b is None:
            out.append('    None')
        else:
            out.append('    Some (fun (m: Machine) ->')
            out.extend(b)
            out.append('    )')
    out.append('  |]')
    return '\n'.join(out)

header = '''namespace Jetpac2.Core

/// Generic per-opcode instruction table (multi-game support, Phase A).
/// Extracted from the per-address Generated.fs page tables: every distinct
/// executed opcode (and DD/FD/ED/CB sub-opcode) is implemented once and
/// dispatched by the byte(s) at the current PC. Address-level gaps (the old
/// per-page fallthroughs) are gone; a byte whose opcode is not covered yet
/// still raises "no code at 0x...." so the UI's static-disasm path works.
/// Uncovered opcodes are grown per game as they are encountered.
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
''')

print(f'wrote {OUT}')
