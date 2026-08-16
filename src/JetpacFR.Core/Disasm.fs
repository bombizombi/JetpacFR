namespace JetpacFR.Core

/// Z80 disassembler for the trace cinema and contract views. Table-driven;
/// covers the base set, CB, ED and DD/FD prefixes. Lengths agree with what
/// the machine actually executes for every instruction it steps (verified by
/// the test harness against a live trace).
module Disasm =

  type Insn =
    { Text: string
      Length: int }

  let private reg8 = [| "B"; "C"; "D"; "E"; "H"; "L"; "(HL)"; "A" |]
  let private reg16 = [| "BC"; "DE"; "HL"; "SP" |]
  let private popPush = [| "BC"; "DE"; "HL"; "AF" |]
  let private conds = [| "NZ"; "Z"; "NC"; "C"; "PO"; "PE"; "P"; "M" |]
  let private alu = [| "ADD"; "ADC"; "SUB"; "SBC"; "AND"; "XOR"; "OR"; "CP" |]
  let private rot = [| "RLC"; "RRC"; "RL"; "RR"; "SLA"; "SRA"; "SLL"; "SRL" |]

  let private s8 (b: byte) = int (sbyte b)

  let private sign (d: int) = if d < 0 then "-" else "+"

  /// Base opcodes whose operand set includes (HL) — under a DD/FD prefix these
  /// gain a displacement byte (and their immediate, if any, shifts right).
  let private usesHlMem (op: int) : bool =
    match op with
    | 0x34 | 0x35 | 0x36 -> true
    | 0x46 | 0x4E | 0x56 | 0x5E | 0x66 | 0x6E | 0x7E -> true
    | 0x70 | 0x71 | 0x72 | 0x73 | 0x74 | 0x75 | 0x77 -> true
    | _ when op >= 0x80 && op <= 0xBF -> (op &&& 7) = 6
    | _ when op >= 0x40 && op <= 0x7F && op <> 0x76 -> ((op >>> 3) &&& 7) = 6 || (op &&& 7) = 6
    | _ -> false

  /// Length of the base (unprefixed) form of an opcode, excluding CB/ED/DD/FD
  /// which the caller handles.
  let private baseLen (op: int) : int =
    match op with
    | 0x01 | 0x11 | 0x21 | 0x31 | 0x22 | 0x2A | 0x32 | 0x3A
    | 0xC3 | 0xCD
    | 0xC2 | 0xC4 | 0xCA | 0xCC | 0xD2 | 0xD4 | 0xDA | 0xDC
    | 0xE2 | 0xE4 | 0xEA | 0xEC | 0xF2 | 0xF4 | 0xFA | 0xFC -> 3
    | 0x10 | 0x18 | 0x20 | 0x28 | 0x30 | 0x38
    | 0x06 | 0x0E | 0x16 | 0x1E | 0x26 | 0x2E | 0x36 | 0x3E
    | 0xD3 | 0xDB
    | 0xC6 | 0xCE | 0xD6 | 0xDE | 0xE6 | 0xEE | 0xF6 | 0xFE -> 2
    | 0xCB | 0xED -> 2
    | 0xDD | 0xFD -> 1
    | _ -> 1

  /// Substitute HL/H/L operands for the IX/IY forms under a DD/FD prefix.
  /// Operands are every token after the first space or comma. A (HL) operand
  /// becomes (IX+d) only for instructions that take a displacement byte;
  /// JP (HL)/EX (SP),HL just rename to (IX)/(SP),IX.
  let private subst (ixName: string) (d: int) (withDisp: bool) (text: string) : string =
    let idx =
      match text.IndexOf ' ', text.IndexOf ',' with
      | -1, c when c >= 0 -> c
      | s, -1 when s >= 0 -> s
      | s, c when s >= 0 && c >= 0 -> min s c
      | _ -> -1
    if idx < 0 then text
    else
      let head = text.Substring(0, idx + 1)
      let operands =
        text.Substring(idx + 1).Split(',')
        |> Array.map (fun o ->
          match o with
          | "(HL)" ->
            if withDisp then sprintf "(%s%s0x%02X)" ixName (sign d) (abs d)
            else sprintf "(%s)" ixName
          | "HL" -> ixName
          | "H" -> ixName + "H"
          | "L" -> ixName + "L"
          | other -> other)
      head + String.concat "," operands

  /// Render the base instruction text. `ix` is Some when a DD/FD prefix is in
  /// effect: operand text is substituted and the LD (HL),n immediate moves
  /// one byte later. Length is handled by the callers.
  let private renderBase (get: int -> byte) (pc: int) (ix: (string * int * bool) option) : string =
    let at (o: int) = get (pc + o)
    let n1 () = int (at 1)
    let n2 () = int (at 2)
    let nn () = n1 () ||| (n2 () <<< 8)
    let d8 () = s8 (at 1)
    let op = int (at 0)
    let rp = (op >>> 4) &&& 3
    let cc = (op >>> 3) &&& 7
    let target () = (pc + 2 + d8 ()) &&& 0xFFFF
    let text =
      match op with
      | 0x00 -> "NOP"
      | 0x01 | 0x11 | 0x21 | 0x31 -> sprintf "LD %s,0x%04X" reg16[rp] (nn ())
      | 0x02 -> "LD (BC),A"
      | 0x0A -> "LD A,(BC)"
      | 0x12 -> "LD (DE),A"
      | 0x1A -> "LD A,(DE)"
      | 0x22 -> sprintf "LD (0x%04X),HL" (nn ())
      | 0x2A -> sprintf "LD HL,(0x%04X)" (nn ())
      | 0x32 -> sprintf "LD (0x%04X),A" (nn ())
      | 0x3A -> sprintf "LD A,(0x%04X)" (nn ())
      | 0x03 | 0x13 | 0x23 | 0x33 -> sprintf "INC %s" reg16[rp]
      | 0x0B | 0x1B | 0x2B | 0x3B -> sprintf "DEC %s" reg16[rp]
      | 0x09 | 0x19 | 0x29 | 0x39 -> sprintf "ADD HL,%s" reg16[rp]
      | 0x04 | 0x0C | 0x14 | 0x1C | 0x24 | 0x2C | 0x34 | 0x3C ->
        sprintf "INC %s" reg8[(op >>> 3) &&& 7]
      | 0x05 | 0x0D | 0x15 | 0x1D | 0x25 | 0x2D | 0x35 | 0x3D ->
        sprintf "DEC %s" reg8[(op >>> 3) &&& 7]
      | 0x06 | 0x0E | 0x16 | 0x1E | 0x26 | 0x2E | 0x3E ->
        sprintf "LD %s,0x%02X" reg8[(op >>> 3) &&& 7] (n1 ())
      | 0x07 -> "RLCA"
      | 0x0F -> "RRCA"
      | 0x17 -> "RLA"
      | 0x1F -> "RRA"
      | 0x08 -> "EX AF,AF'"
      | 0x10 -> sprintf "DJNZ 0x%04X" (target ())
      | 0x18 -> sprintf "JR 0x%04X" (target ())
      | 0x20 | 0x28 | 0x30 | 0x38 ->
        sprintf "JR %s,0x%04X" conds[(op >>> 3) &&& 3] (target ())
      | 0x27 -> "DAA"
      | 0x2F -> "CPL"
      | 0x37 -> "SCF"
      | 0x3F -> "CCF"
      | 0x36 ->
        match ix with
        | Some _ -> sprintf "LD (HL),0x%02X" (n2 ())
        | None -> sprintf "LD (HL),0x%02X" (n1 ())
      | _ when op >= 0x40 && op <= 0x7F ->
        if op = 0x76 then "HALT"
        else sprintf "LD %s,%s" reg8[(op >>> 3) &&& 7] reg8[op &&& 7]
      | _ when op >= 0x80 && op <= 0xBF ->
        let al = alu[(op >>> 3) &&& 7]
        if al = "ADD" || al = "ADC" || al = "SBC" then
          sprintf "%s A,%s" al reg8[op &&& 7]
        else
          sprintf "%s %s" al reg8[op &&& 7]
      | 0xC0 | 0xC8 | 0xD0 | 0xD8 | 0xE0 | 0xE8 | 0xF0 | 0xF8 ->
        sprintf "RET %s" conds[cc]
      | 0xC1 | 0xD1 | 0xE1 | 0xF1 -> sprintf "POP %s" popPush[(op >>> 4) &&& 3]
      | 0xC5 | 0xD5 | 0xE5 | 0xF5 -> sprintf "PUSH %s" popPush[(op >>> 4) &&& 3]
      | 0xC2 | 0xCA | 0xD2 | 0xDA | 0xE2 | 0xEA | 0xF2 | 0xFA ->
        sprintf "JP %s,0x%04X" conds[cc] (nn ())
      | 0xC3 -> sprintf "JP 0x%04X" (nn ())
      | 0xC4 | 0xCC | 0xD4 | 0xDC | 0xE4 | 0xEC | 0xF4 | 0xFC ->
        sprintf "CALL %s,0x%04X" conds[cc] (nn ())
      | 0xCD -> sprintf "CALL 0x%04X" (nn ())
      | 0xC6 | 0xCE | 0xD6 | 0xDE | 0xE6 | 0xEE | 0xF6 | 0xFE ->
        let al = alu[(op >>> 3) &&& 7]
        if al = "ADD" || al = "ADC" || al = "SBC" then
          sprintf "%s A,0x%02X" al (n1 ())
        else
          sprintf "%s 0x%02X" al (n1 ())
      | 0xC7 | 0xCF | 0xD7 | 0xDF | 0xE7 | 0xEF | 0xF7 | 0xFF ->
        sprintf "RST 0x%02X" (op &&& 0x38)
      | 0xC9 -> "RET"
      | 0xD3 -> sprintf "OUT (0x%02X),A" (n1 ())
      | 0xDB -> sprintf "IN A,(0x%02X)" (n1 ())
      | 0xD9 -> "EXX"
      | 0xE3 -> "EX (SP),HL"
      | 0xE9 -> "JP (HL)"
      | 0xEB -> "EX DE,HL"
      | 0xF3 -> "DI"
      | 0xFB -> "EI"
      | 0xF9 -> "LD SP,HL"
      | _ -> sprintf "DB %02X" op
    match ix with
    | None -> text
    | Some (name, d, withDisp) -> subst name d withDisp text

  let private cbText (op: int) (opnd: string) : string =
    if op < 0x40 then sprintf "%s %s" rot[(op >>> 3) &&& 7] opnd
    elif op < 0x80 then sprintf "BIT %d,%s" ((op >>> 3) &&& 7) opnd
    elif op < 0xC0 then sprintf "RES %d,%s" ((op >>> 3) &&& 7) opnd
    else sprintf "SET %d,%s" ((op >>> 3) &&& 7) opnd

  let private edLen (op: int) : int =
    match op with
    | 0x43 | 0x53 | 0x63 | 0x73 | 0x4B | 0x5B | 0x6B | 0x7B -> 4
    | _ -> 2

  let private edText (get: int -> byte) (pc: int) : string =
    let op = int (get pc)
    let at (o: int) = get (pc + o)
    let rp = (op >>> 4) &&& 3
    match op with
    | op when op >= 0x40 && op <= 0x7F && (op &&& 7 = 0 || op &&& 7 = 1) ->
      if op = 0x70 then "IN (C)"
      elif op = 0x71 then "OUT (C),0"
      elif op &&& 1 = 0 then sprintf "IN %s,(C)" reg8[(op >>> 3) &&& 7]
      else sprintf "OUT (C),%s" reg8[(op >>> 3) &&& 7]
    | 0x42 | 0x52 | 0x62 | 0x72 -> sprintf "SBC HL,%s" reg16[rp]
    | 0x4A | 0x5A | 0x6A | 0x7A -> sprintf "ADC HL,%s" reg16[rp]
    | 0x43 | 0x53 | 0x63 | 0x73 ->
      sprintf "LD (0x%04X),%s" (int (at 1) ||| (int (at 2) <<< 8)) reg16[rp]
    | 0x4B | 0x5B | 0x6B | 0x7B ->
      sprintf "LD %s,(0x%04X)" reg16[rp] (int (at 1) ||| (int (at 2) <<< 8))
    | 0x44 | 0x4C | 0x54 | 0x5C | 0x64 | 0x6C | 0x74 | 0x7C -> "NEG"
    | 0x45 | 0x55 | 0x65 | 0x75 | 0x7D -> "RETN"
    | 0x4D -> "RETI"
    | 0x46 | 0x4E | 0x66 | 0x6E -> "IM 0"
    | 0x56 | 0x76 -> "IM 1"
    | 0x5E | 0x7E -> "IM 2"
    | 0x47 -> "LD I,A"
    | 0x4F -> "LD R,A"
    | 0x57 -> "LD A,I"
    | 0x5F -> "LD A,R"
    | 0x67 -> "RRD"
    | 0x6F -> "RLD"
    | 0xA0 -> "LDI"
    | 0xA1 -> "CPI"
    | 0xA2 -> "INI"
    | 0xA3 -> "OUTI"
    | 0xA8 -> "LDD"
    | 0xA9 -> "CPD"
    | 0xAA -> "IND"
    | 0xAB -> "OUTD"
    | 0xB0 -> "LDIR"
    | 0xB1 -> "CPIR"
    | 0xB2 -> "INIR"
    | 0xB3 -> "OTIR"
    | 0xB8 -> "LDDR"
    | 0xB9 -> "CPDR"
    | 0xBA -> "INDR"
    | 0xBB -> "OTDR"
    | _ -> sprintf "DB ED %02X" op

  /// Decode one instruction at `pc` using the byte getter (absolute index).
  let decode (get: int -> byte) (pc: int) : Insn =
    let op = int (get pc)
    match op with
    | 0xDD | 0xFD ->
      let name = if op = 0xDD then "IX" else "IY"
      let inner = int (get (pc + 1))
      if inner = 0xCB then
        let d = s8 (get (pc + 2))
        let text = cbText (int (get (pc + 3))) (sprintf "(%s%s0x%02X)" name (sign d) (abs d))
        { Text = text; Length = 4 }
      elif inner = 0xED then
        { Text = edText get (pc + 1); Length = edLen inner + 1 }
      else
        let text = renderBase get (pc + 1) (Some (name, s8 (get (pc + 2)), usesHlMem inner))
        let len = baseLen inner + (if usesHlMem inner then 1 else 0) + 1
        { Text = text; Length = len }
    | 0xCB ->
      let op2 = int (get (pc + 1))
      { Text = cbText op2 reg8[op2 &&& 7]; Length = 2 }
    | 0xED ->
      let op2 = int (get (pc + 1))
      { Text = edText get (pc + 1); Length = edLen op2 }
    | _ ->
      let op2 = op
      let len = baseLen op2 + 0
      { Text = renderBase get pc None; Length = len }

  /// Instruction length only — no mnemonic rendering, no closures. The
  /// recorder's hot path calls this for every executed instruction; the
  /// rendered text is produced on demand for the cinema and contract views.
  /// Mirrors `decode`'s prefix logic without any string or lambda work.
  let disasmLength (memory: byte[]) (address: int) : int =
    let pc = address &&& 0xFFFF
    let op = int memory[pc]
    match op with
    | 0xDD | 0xFD ->
      let inner = int memory[(pc + 1) &&& 0xFFFF]
      if inner = 0xCB then 4
      elif inner = 0xED then edLen inner + 1
      else baseLen inner + (if usesHlMem inner then 1 else 0) + 1
    | 0xCB -> 2
    | 0xED -> edLen (int memory[(pc + 1) &&& 0xFFFF])
    | _ -> baseLen op

  /// Disassemble one instruction at `address` in a 64K memory image.
  let disasmMemory (memory: byte[]) (address: int) : Insn =
    let get (a: int) = memory[a &&& 0xFFFF]
    decode get (address &&& 0xFFFF)

  /// Disassemble one instruction at `offset` in a byte slice (trace entries).
  let disasmBytes (bytes: byte[]) (offset: int) : Insn =
    decode (fun a -> if a < bytes.Length then bytes[a] else 0uy) (max 0 offset)
