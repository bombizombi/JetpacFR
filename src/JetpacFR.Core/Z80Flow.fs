namespace JetpacFR.Core

/// Control-flow classification + basic-block splitting for the code graph
/// view (plan_code_graph). Pure byte-level functions: no emulator state.
/// Lengths come from the verified table disassembler; flow kinds from the
/// leading opcode bytes.
module Z80Flow =

  /// How a basic block ends. A call does not end a block (execution
  /// continues after it) but its target is still a block leader.
  type FlowEnd =
    | Linear
    | Call of int option
    | Jump of int option      // unconditional; None = indirect (JP (HL))
    | Branch of int option    // conditional; None = indirect/unknown
    | Return

  type Block =
    { Start: int
      EndExcl: int
      Ends: FlowEnd }

  /// Classify the instruction at addr: (flow kind, length).
  let classify (mem: byte[]) (addr: int) : FlowEnd * int =
    let len = max 1 (Disasm.disasmMemory mem addr).Length
    let a = addr &&& 0xFFFF
    let b0 = mem[a]
    let b1 = mem[(a + 1) &&& 0xFFFF]
    let b2 = mem[(a + 2) &&& 0xFFFF]
    let abs16 = int b1 ||| (int b2 <<< 8)
    let rel = a + len + int (sbyte b1)
    let rst = (int b0 >>> 3) * 8
    let kind =
      match b0 with
      | 0x10uy -> Branch(Some rel)                       // DJNZ
      | 0x18uy -> Jump(Some rel)                         // JR
      | 0x20uy | 0x28uy | 0x30uy | 0x38uy -> Branch(Some rel)
      | 0xC3uy -> Jump(Some abs16)
      | 0xC2uy | 0xCAuy | 0xD2uy | 0xDAuy
      | 0xE2uy | 0xEAuy | 0xF2uy | 0xFAuy -> Branch(Some abs16)
      | 0xC4uy | 0xCCuy | 0xD4uy | 0xDCuy
      | 0xE4uy | 0xECuy | 0xF4uy | 0xFCuy | 0xCDuy -> Call(Some abs16)
      | 0xC7uy | 0xCFuy | 0xD7uy | 0xDFuy
      | 0xE7uy | 0xEFuy | 0xF7uy | 0xFFuy -> Call(Some rst)
      | 0xC9uy | 0xC0uy | 0xC8uy | 0xD0uy | 0xD8uy -> Return
      | 0xE9uy -> Jump None                              // JP (HL)
      | 0xEDuy when b1 = 0x45uy || b1 = 0x4Duy -> Return // RETN/RETI
      | 0xDDuy when b1 = 0xE9uy -> Jump None
      | 0xFDuy when b1 = 0xE9uy -> Jump None
      | _ -> Linear
    kind, len

  /// Split [start,endExcl) into basic blocks. `isCode` steers which
  /// addresses decode (control-file Code blocks); non-code bytes cut the
  /// runs. One unified walk: a block closes at a block-ending instruction,
  /// at a jump target the walk lands on, or just before a jump target that
  /// falls inside the next linear instruction (that target resyncs the
  /// walk; the straddled bytes belong to no block). Guarantees: a target
  /// is never inside a block interior, and every block re-decodes exactly
  /// from its start to its end. Spans are within [0, 0x10000], no wrap.
  let splitBlocks (mem: byte[]) (start: int) (endExcl: int) (isCode: int -> bool) : Block list =
    // pass 1: linear decode of the span to collect static targets
    let targets = System.Collections.Generic.HashSet<int>()
    let mutable a = start
    while a < endExcl do
      if not (isCode a) then a <- a + 1
      else
        let kind, len = classify mem a
        let len = max 1 (min len (endExcl - a))
        match kind with
        | Jump (Some t) | Branch (Some t) | Call (Some t) ->
          if t >= start && t < endExcl then targets.Add t |> ignore
        | _ -> ()
        a <- a + len
    // pass 2: unified walk with resync
    let blocks = ResizeArray<Block>()
    let mutable a = start
    while a < endExcl do
      if not (isCode a) then a <- a + 1
      else
        let bs = a
        let mutable lastA = a
        let mutable closeAt = -1                 // exclusive end when set
        let mutable resync = -1                  // jump-to address on straddle
        while a < endExcl && closeAt < 0 do
          if a <> bs && targets.Contains a then
            closeAt <- a                          // walk landed on a target
          else
            let kind, len = classify mem a
            let len = max 1 (min len (endExcl - a))
            let na = a + len
            // a target strictly inside this instruction forces a resync:
            // the block ends before it and the straddled bytes are orphaned
            let straddled =
              [ for t in targets do
                  if t > a && t < na then t ]
              |> List.sort
              |> List.tryHead
            match straddled with
            | Some t ->
              closeAt <- a
              resync <- t
            | None ->
              lastA <- a
              match kind with
              | Jump _ | Branch _ | Return ->
                closeAt <- na
                a <- na
              | _ -> a <- na
        if closeAt < 0 then closeAt <- endExcl
        if closeAt > bs then
          let kind, _ = classify mem lastA
          blocks.Add { Start = bs; EndExcl = closeAt; Ends = kind }
        // continue at the resync target, else at the block's end (which is
        // always a walk position > bs, so the loop makes progress)
        a <- if resync >= 0 then resync else closeAt
    List.ofSeq blocks
