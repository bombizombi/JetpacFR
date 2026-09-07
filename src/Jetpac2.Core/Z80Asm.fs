namespace Jetpac2.Core

/// One instruction in the Z80 DSL. Formlet-style dual meaning, in the
/// spirit of the design note: the structure of the computation determines
/// BOTH the assembled machine-code bytes (via `assemble`) and the direct
/// execution semantics (`Run`, no interpreter needed). The CE body is the
/// single source of truth; the two interpretations are derived from it.
[<Struct>]
type Z80Op =
  { Mnemonic: string
    /// The assembled machine-code bytes for this instruction.
    Bytes: byte[]
    /// Direct execution on the port machine, mirroring the generated
    /// instruction arms exactly (same Fetch/PassTime/register calls), so
    /// cycle counts and flags are track-perfect by construction.
    Run: Machine -> unit
    /// Zero-width label: when set, the assembler records this op's address
    /// into the cell during pass 1 (used by JP_LBL/JR_LBL/DJNZ_LBL/...).
    Label: int ref option
    /// Position-aware encoding (symbols): given this op's start address,
    /// produce the final bytes in pass 2. None = `Bytes` is final. The
    /// length never depends on the address, so pass 1 can lay out with
    /// `Bytes.Length` alone.
    Encode: (int -> byte[]) option }

/// The computation-expression builder. Instruction values (`Z80.INC_A`,
/// `Z80.LD_HL 16384`, ...) appear as CE lines; raw byte blocks appear via
/// `yield! [| ... |]` and keep their interpreter semantics until they are
/// disassembled into CE form. CE code and raw bytes can call each other
/// freely (CALL/JR cross the boundary because both mutate the same machine).
type Z80Builder() =
  member _.Yield(o: Z80Op) : Z80Op list = [ o ]
  member _.Yield(xs: Z80Op list) : Z80Op list = xs
  member _.YieldFrom(xs: Z80Op list) : Z80Op list = xs

  /// Raw bytes not yet disassembled into CE form: they execute through the
  /// interpreter (Machine.Step) until PC leaves the block or the current
  /// frame ends (a looping block must be frame-bounded to return).
  member _.YieldFrom(bytes: byte[]) : Z80Op list =
    [ { Mnemonic = "raw"
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
        Encode = None } ]

  member _.Combine(a: Z80Op list, b: Z80Op list) = a @ b
  member _.Delay(f: unit -> Z80Op list) = f ()
  member _.Zero() : Z80Op list = []
  member _.ReturnFrom(xs: Z80Op list) = xs

  /// Plain let bindings inside the CE - generated programs declare their
  /// label cells this way (`let screenClear = Z80.label ()`) so an emitted
  /// body is self-contained without touching the enclosing module.
  member _.Let(x: 'a, f: 'a -> Z80Op list) : Z80Op list = f x

  /// Unrolled repetition at assembly time (structure is static; a bounded
  /// while emits the body repeatedly).
  member _.For(xs: seq<'a>, f: 'a -> Z80Op list) = xs |> Seq.collect f |> Seq.toList
  member _.While(cond: unit -> bool, body: unit -> Z80Op list) =
    let mutable guard = 0
    let mutable acc: Z80Op list = []
    while cond () && guard < 1_000_000 do
      acc <- acc @ body ()
      guard <- guard + 1
    if guard >= 1_000_000 then invalidOp "Z80 DSL While exceeded 1M unrolled iterations"
    acc

module Z80BuilderInstance =
  /// The CE builder instance: `z80 { ... }`. Opened by game projects as
  /// `open Jetpac2.Core.Z80BuilderInstance`.
  let z80 = Z80Builder()

/// The instruction vocabulary. Every op carries its assembled bytes (hand
/// encoded, matching the disassembler's encodings) and the exact execution
/// semantics of the generated arms, so CE programs are track-perfect.
module Z80 =

  let mkOp (mnemonic: string) (bytes: byte[]) (run: Machine -> unit) : Z80Op =
    { Mnemonic = mnemonic; Bytes = bytes; Run = run; Label = None; Encode = None }

  let b16 (v: int) = [| byte (v &&& 0xFF); byte ((v >>> 8) &&& 0xFF) |]

  /// Dispatch to a Z80Table slot: the machine's memory holds the assembled
  /// bytes at PC, so the slot's Fetch/ReadImm read the operands. The CE op
  /// therefore IS the validated table arm — track-perfect by construction.
  let runOf (table: (Machine -> unit) option[]) (slot: int) : Machine -> unit =
    match table.[slot] with
    | Some f -> f
    | None -> failwithf "Z80 slot %02X not covered by the table" slot

  // ---- labels + position-aware ops (Phase 3) ------------------------------

  /// A symbolic address cell, resolved by the two-pass assembler.
  let label () : int ref = ref -1

  /// Place a label: a zero-width op whose pass-1 address is recorded into
  /// the cell. Forward references work (pass 1 lays out the whole program
  /// before pass 2 encodes the jump operands).
  let at (c: int ref) : Z80Op =
    { Mnemonic = "label"; Bytes = [||]; Run = (fun _ -> ()); Label = Some c; Encode = None }

  let private lblOp (mnemonic: string) (bytes: byte[]) (run: Machine -> unit) (c: int ref) (encode: int -> byte[]) : Z80Op =
    { Mnemonic = mnemonic
      Bytes = bytes
      Run = run
      Label = None
      Encode =
        Some(fun addr ->
          if !c < 0 then failwithf "unresolved label for %s" mnemonic
          encode addr) }

  let JP_LBL (c: int ref) =
    lblOp "JP label" [| 0xC3uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xC3) c (fun _ -> [| 0xC3uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_NZ_LBL (c: int ref) =
    lblOp "JP NZ,label" [| 0xC2uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xC2) c (fun _ -> [| 0xC2uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_Z_LBL (c: int ref) =
    lblOp "JP Z,label" [| 0xCAuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xCA) c (fun _ -> [| 0xCAuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_NC_LBL (c: int ref) =
    lblOp "JP NC,label" [| 0xD2uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xD2) c (fun _ -> [| 0xD2uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_C_LBL (c: int ref) =
    lblOp "JP C,label" [| 0xDAuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xDA) c (fun _ -> [| 0xDAuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_PO_LBL (c: int ref) =
    lblOp "JP PO,label" [| 0xE2uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xE2) c (fun _ -> [| 0xE2uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_PE_LBL (c: int ref) =
    lblOp "JP PE,label" [| 0xEAuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xEA) c (fun _ -> [| 0xEAuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_P_LBL (c: int ref) =
    lblOp "JP P,label" [| 0xF2uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xF2) c (fun _ -> [| 0xF2uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JP_M_LBL (c: int ref) =
    lblOp "JP M,label" [| 0xFAuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xFA) c (fun _ -> [| 0xFAuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_LBL (c: int ref) =
    lblOp "CALL label" [| 0xCDuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xCD) c (fun _ -> [| 0xCDuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_NZ_LBL (c: int ref) =
    lblOp "CALL NZ,label" [| 0xC4uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xC4) c (fun _ -> [| 0xC4uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_Z_LBL (c: int ref) =
    lblOp "CALL Z,label" [| 0xCCuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xCC) c (fun _ -> [| 0xCCuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_NC_LBL (c: int ref) =
    lblOp "CALL NC,label" [| 0xD4uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xD4) c (fun _ -> [| 0xD4uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_C_LBL (c: int ref) =
    lblOp "CALL C,label" [| 0xDCuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xDC) c (fun _ -> [| 0xDCuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_PO_LBL (c: int ref) =
    lblOp "CALL PO,label" [| 0xE4uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xE4) c (fun _ -> [| 0xE4uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_PE_LBL (c: int ref) =
    lblOp "CALL PE,label" [| 0xECuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xEC) c (fun _ -> [| 0xECuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_P_LBL (c: int ref) =
    lblOp "CALL P,label" [| 0xF4uy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xF4) c (fun _ -> [| 0xF4uy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let CALL_M_LBL (c: int ref) =
    lblOp "CALL M,label" [| 0xFCuy; 0x00uy; 0x00uy |] (runOf Z80Table.main 0xFC) c (fun _ -> [| 0xFCuy; byte (!c &&& 0xFF); byte ((!c >>> 8) &&& 0xFF) |])
  let JR_LBL (c: int ref) =
    lblOp "JR label" [| 0x18uy; 0x00uy |] (runOf Z80Table.main 0x18) c (fun addr ->
      [| 0x18uy; byte ((!c - (addr + 2)) &&& 0xFF) |])
  let JR_NZ_LBL (c: int ref) =
    lblOp "JR NZ,label" [| 0x20uy; 0x00uy |] (runOf Z80Table.main 0x20) c (fun addr ->
      [| 0x20uy; byte ((!c - (addr + 2)) &&& 0xFF) |])
  let JR_Z_LBL (c: int ref) =
    lblOp "JR Z,label" [| 0x28uy; 0x00uy |] (runOf Z80Table.main 0x28) c (fun addr ->
      [| 0x28uy; byte ((!c - (addr + 2)) &&& 0xFF) |])
  let JR_NC_LBL (c: int ref) =
    lblOp "JR NC,label" [| 0x30uy; 0x00uy |] (runOf Z80Table.main 0x30) c (fun addr ->
      [| 0x30uy; byte ((!c - (addr + 2)) &&& 0xFF) |])
  let JR_C_LBL (c: int ref) =
    lblOp "JR C,label" [| 0x38uy; 0x00uy |] (runOf Z80Table.main 0x38) c (fun addr ->
      [| 0x38uy; byte ((!c - (addr + 2)) &&& 0xFF) |])
  let DJNZ_LBL (c: int ref) =
    lblOp "DJNZ label" [| 0x10uy; 0x00uy |] (runOf Z80Table.main 0x10) c (fun addr ->
      [| 0x10uy; byte ((!c - (addr + 2)) &&& 0xFF) |])

  /// Assemble a program into its machine-code bytes (structure -> bytes).
  /// Two passes: pass 1 lays out the program and resolves labels; pass 2
  /// emits the final bytes (position-aware encodings read the label cells).
  let assemble (program: Z80Op list) : byte[] =
    let mutable addr = 0
    for o in program do
      match o.Label with
      | Some c -> c := addr
      | None -> addr <- addr + o.Bytes.Length
    let buf = Array.zeroCreate<byte> addr
    let mutable i = 0
    for o in program do
      match o.Label with
      | Some _ -> ()
      | None ->
        let bytes = match o.Encode with Some f -> f i | None -> o.Bytes
        Array.blit bytes 0 buf i bytes.Length
        i <- i + bytes.Length
    buf

  /// Run a LINEAR program directly. Normal CE operations pass through the
  /// machine's common dispatch wrapper so co-hooks and replacement hooks have
  /// the same ordering as frame execution. Raw blocks already call Step per
  /// instruction and therefore must not receive an additional outer hook.
  let run (program: Z80Op list) (m: Machine) : unit =
    for o in program do
      if o.Mnemonic = "raw" || o.Label.IsSome then
        o.Run m
      else
        m.ExecuteOne(fun _ -> o.Run m)

  /// PC-indexed CE entries plus reverse byte dependencies. Invalid entries
  /// deliberately fall back to the generated interpreter, which reads the
  /// current opcode from memory and is therefore safe for arbitrary SMC.
  type ExecutionIndex(program: Z80Op list, baseAddress: int) =
    let table = Array.zeroCreate<Z80Op option> 0x10000
    let valid = Array.zeroCreate<bool> 0x10000
    let dependents : ResizeArray<int>[] = Array.init 0x10000 (fun _ -> ResizeArray())
    let entries = ResizeArray<int * int * byte[]>()
    let mutable attachedMachine : Machine option = None
    let assembled = assemble program

    do
      let mutable offset = 0
      for o in program do
        match o.Label with
        | Some c -> c := offset
        | None ->
          let start = (baseAddress + offset) &&& 0xFFFF
          let length = o.Bytes.Length
          if length > 0 && o.Mnemonic <> "raw" && offset + length <= assembled.Length then
            table.[start] <- Some o
            valid.[start] <- true
            entries.Add(start, offset, Array.sub assembled offset length)
            for i in 0 .. length - 1 do
              dependents.[(start + i) &&& 0xFFFF].Add start
          offset <- offset + length

    member _.BaseAddress = baseAddress &&& 0xFFFF

    member this.Lookup(address: int) : Z80Op option =
      let addr = address &&& 0xFFFF
      if valid.[addr] then table.[addr] else None

    member this.Invalidate(address: int) =
      let addr = address &&& 0xFFFF
      for start in dependents.[addr] do
        valid.[start] <- false

    member this.Refresh(memory: byte[]) =
      for (start, offset, expected) in entries do
        let mutable matches = true
        let mutable i = 0
        while matches && i < expected.Length do
          if memory.[(start + i) &&& 0xFFFF] <> expected.[i] then matches <- false
          i <- i + 1
        valid.[start] <- matches

    /// Attach once to a machine. State loads refresh validity from the loaded
    /// bytes; changed instruction bytes remain on the interpreter path.
    member this.Attach(machine: Machine) =
      match attachedMachine with
      | Some current when obj.ReferenceEquals(current, machine) -> ()
      | _ ->
        attachedMachine <- Some machine
        machine.AddMemoryWriteHandler(fun event -> this.Invalidate event.Address)
        machine.AddMemoryResetHandler(fun () -> this.Refresh machine.Memory)
        this.Refresh machine.Memory

    member _.EntryCount = entries.Count
    member this.IsValid(address: int) = valid.[address &&& 0xFFFF]

  /// Build an index for a CE image loaded at address zero.
  let makeIndex (program: Z80Op list) : ExecutionIndex =
    ExecutionIndex(program, 0)

  /// Build an index for a CE image loaded at an explicit 16-bit address.
  let makeIndexAt (baseAddress: int) (program: Z80Op list) : ExecutionIndex =
    ExecutionIndex(program, baseAddress)

  /// Execute one frame. Resolution happens inside ExecuteOne, after hooks,
  /// so writes performed by a hook cannot leave a stale CE operation selected.
  let runFrame (index: ExecutionIndex) (m: Machine) : int64 * int64 =
    let frameStart = m.CycleCount()
    let frameEnd = m.FrameEnd
    let resolve (machine: Machine) =
      match index.Lookup(machine.Regs.Pc()) with
      | Some o -> o.Run machine
      | None -> Machine.GeneratedStep machine
    while m.CycleCount() < frameEnd do
      m.ExecuteOne resolve
    m.FrameEnd <- frameEnd + 69888L
    frameStart, m.CycleCount()
