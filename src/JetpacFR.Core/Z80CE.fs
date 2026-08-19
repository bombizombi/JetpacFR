namespace JetpacFR.Core

open Jetpac2.Core

/// Disassemble a binary into CE form (Phase 4): the reverse of `assemble`.
/// Covered instructions become their vocab ops (numeric operands); anything
/// else merges into raw byte blocks, so `assemble (toOps image start count)`
/// reproduces the image byte-for-byte by construction. `toSource` emits the
/// F# `z80 { ... }` body with symbolic labels for in-range jump targets.
module Z80CE =

  let private rawOp (bytes: byte[]) : Z80Op =
    { Mnemonic = "raw"
      Bytes = bytes
      Run =
        fun m ->
          let start = m.Regs.Pc()
          let limit = (start + bytes.Length) &&& 0xFFFF
          let frameEnd = m.FrameEnd
          let mutable pc = m.Regs.Pc()
          while pc >= start && pc < limit && m.CycleCount() < frameEnd do
            m.Step()
            pc <- m.Regs.Pc()
      Label = None
      Encode = None }

  /// Decode the instruction at `pc`: the matching row + the concrete op.
  let decode (image: byte[]) (pc: int) : (Z80Decode.Row * Z80Op) option =
    let n = image.Length
    let rec go (rows: Z80Decode.Row list) =
      match rows with
      | [] -> None
      | r :: rest ->
        let len = r.Prefix.Length
        if pc + len <= n then
          let mutable ok = true
          let mutable i = 0
          while ok && i < len do
            if r.Mask[i] <> 0uy && image[pc + i] <> r.Prefix[i] then ok <- false
            i <- i + 1
          if ok then Some(r, r.Make image pc)
          else go rest
        else go rest
    go Z80Decode.rows

  /// The jump target of a label-carrying op, from its encoded bytes.
  let private targetOf (op: Z80Op) (pc: int) : int =
    if op.Mnemonic.StartsWith "JR" || op.Mnemonic.StartsWith "DJNZ" then
      (pc + 2 + int (sbyte op.Bytes[1])) &&& 0xFFFF
    else
      (int op.Bytes[1]) ||| ((int op.Bytes[2]) <<< 8)

  /// Disassemble `count` bytes starting at `start` into CE ops. Covered
  /// instructions become their ops; everything else merges into raw blocks.
  let toOps (image: byte[]) (start: int) (count: int) : Z80Op list =
    let limit = min (start + count) image.Length
    let rec walk (pc: int) (acc: Z80Op list) : Z80Op list =
      if pc >= limit then List.rev acc
      else
        let insn = Disasm.disasmMemory image pc
        match decode image pc with
        | Some (_, op) -> walk (pc + insn.Length) (op :: acc)
        | None ->
          let mutable endPc = pc
          while endPc < limit && (decode image endPc).IsNone do
            endPc <- endPc + max 1 (Disasm.disasmMemory image endPc).Length
          let block = Array.init (endPc - pc) (fun i -> image[pc + i])
          walk endPc (rawOp block :: acc)
    walk start []

  /// Emit the F# `z80 { ... }` body for the binary: named ops, symbolic
  /// labels for in-range jump targets, raw blocks for the rest.
  let toSource (image: byte[]) (start: int) (count: int) : string =
    let limit = min (start + count) image.Length
    // Pass 1: instruction boundaries + in-range jump targets.
    let sites = ResizeArray<int * Z80Decode.Row * Z80Op>()
    let targets = System.Collections.Generic.HashSet<int>()
    let rec collect (pc: int) =
      if pc < limit then
        match decode image pc with
        | Some (row, op) ->
          sites.Add(pc, row, op)
          if row.LabelName.IsSome then
            let t = targetOf op pc
            if t >= start && t < limit then targets.Add t |> ignore
          collect (pc + (Disasm.disasmMemory image pc).Length)
        | None ->
          let mutable endPc = pc
          while endPc < limit && (decode image endPc).IsNone do
            endPc <- endPc + max 1 (Disasm.disasmMemory image endPc).Length
          collect endPc
    collect start
    let labels = System.Collections.Generic.Dictionary<int, string>()
    let mutable n = 0
    for t in Seq.sort targets do
      labels[t] <- sprintf "lbl%d" n
      n <- n + 1
    let sb = System.Text.StringBuilder()
    sb.AppendLine "z80 {" |> ignore
    let mutable idx = 0
    let mutable pc = start
    while pc < limit do
      if idx < sites.Count && pc = (let (a, _, _) = sites[idx] in a) then
        let (a, row, op) = sites[idx]
        match labels.TryGetValue a with
        | true, l -> sb.AppendLine(sprintf "  Z80.at %s" l) |> ignore
        | _ -> ()
        if row.LabelName.IsSome then
          let t = targetOf op pc
          match labels.TryGetValue t with
          | true, l -> sb.AppendLine(sprintf "  Z80.%s %s" row.LabelName.Value l) |> ignore
          | _ -> sb.AppendLine("  " + row.Format image pc) |> ignore
        else
          sb.AppendLine("  " + row.Format image pc) |> ignore
        pc <- pc + (Disasm.disasmMemory image pc).Length
        idx <- idx + 1
      else
        let nextSite = if idx < sites.Count then (let (a, _, _) = sites[idx] in a) else limit
        let rawEnd = min nextSite limit
        let bytes = Array.init (rawEnd - pc) (fun i -> image[pc + i])
        let hex = bytes |> Array.map (fun b -> sprintf "0x%02Xuy" b) |> String.concat "; "
        sb.AppendLine(sprintf "  yield! [| %s |]" hex) |> ignore
        pc <- rawEnd
    sb.AppendLine "  }" |> ignore
    sb.ToString()
