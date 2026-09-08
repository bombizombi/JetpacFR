namespace JetpacFR.Core

open System
open Fable.Core
open Fable.Core.JsInterop

/// Web shim for the control-file model: same shapes as JetpacFR.Core's
/// ControlFile.fs minus everything System.IO (the desktop keeps the
/// canonical file-backed implementation; the browser persists through
/// localStorage + export/import instead). Exists so CtrlMapModel.fs and
/// the web UI can share the desktop's block/comment vocabulary.
type BlockKind =
  | Code
  | Data
  | Gap

type Block =
  { Start: int
    EndExcl: int
    Name: string
    Kind: BlockKind }

type CommentKind =
  | Line
  | Name
  | Range
  | Exec
  | Frames

type ControlComment =
  { Kind: CommentKind
    Addr: int
    EndExcl: int
    InstrIndex: int
    Text: string }

type ControlFile =
  { ImageFile: string
    Start: int
    EndExcl: int
    EntryPc: int
    ActiveVersion: int
    Blocks: Block list
    Comments: ControlComment list
    Dirty: bool }

module ControlFile =

  let empty (start: int) (endExcl: int) : ControlFile =
    { ImageFile = "original.bin"
      Start = start
      EndExcl = endExcl
      EntryPc = start
      ActiveVersion = -1
      Blocks = [ if endExcl > start then { Start = start; EndExcl = endExcl; Name = "whole_span"; Kind = Code } ]
      Comments = []
      Dirty = false }

  let kindToString =
    function Line -> "line" | Name -> "name" | Range -> "range" | Exec -> "exec"

  let private kindFromString (s: string) : CommentKind =
    match s with
    | "name" -> Name
    | "range" -> Range
    | "exec" -> Exec
    | _ -> Line

  let private kindToBlockString =
    function Code -> "code" | Data -> "data" | Gap -> "gap"

  let private blockKindFromString (s: string) : BlockKind =
    match s with
    | "data" -> Data
    | "gap" -> Gap
    | _ -> Code

  /// Serialize to the control.json shape (desktop-compatible).
  let toJson (c: ControlFile) : string =
    let root: obj = emitJsExpr () "({})"
    root?image <- c.ImageFile
    root?start <- c.Start
    root?endExcl <- c.EndExcl
    root?entryPc <- c.EntryPc
    if c.ActiveVersion >= 0 then root?activeVersion <- c.ActiveVersion
    let blocks: obj = emitJsExpr () "([])"
    for b in c.Blocks do
      let bo: obj = emitJsExpr () "({})"
      bo?start <- b.Start
      bo?endExcl <- b.EndExcl
      bo?name <- b.Name
      bo?kind <- kindToBlockString b.Kind
      emitJsExpr (blocks, bo) "$0.push($1)" |> ignore
    root?blocks <- blocks
    if not (List.isEmpty c.Comments) then
      let arr: obj = emitJsExpr () "([])"
      for m in c.Comments do
        let mo: obj = emitJsExpr () "({})"
        mo?kind <- kindToString m.Kind
        if m.Kind = Exec then mo?instrIndex <- m.InstrIndex else mo?addr <- m.Addr
        if m.Kind = Name || m.Kind = Range then mo?endExcl <- m.EndExcl
        mo?text <- m.Text
        emitJsExpr (arr, mo) "$0.push($1)" |> ignore
      root?comments <- arr
    emitJsExpr root "JSON.stringify($0, null, 2)"

  let fromJson (text: string) : ControlFile =
    let root: obj = emitJsExpr text "JSON.parse($0)"
    let prop (k: string) : obj = emitJsExpr (root, k) "($0[$1] === undefined ? null : $0[$1])"
    let str (k: string) (def: string) =
      match prop k with
      | null -> def
      | v -> string v
    let intOf (k: string) (def: int) =
      match prop k with
      | null -> def
      | v -> unbox<int> v
    let blocks =
      match prop "blocks" with
      | null -> []
      | e ->
        [ let n: int = emitJsExpr e "$0.length"
          for i in 0 .. n - 1 do
            let bo: obj = emitJsExpr (e, i) "$0[$1]"
            let get (k: string) : obj = emitJsExpr (bo, k) "($0[$1] === undefined ? null : $0[$1])"
            let start = unbox<int> (get "start")
            let endE = unbox<int> (get "endExcl")
            let name =
              match get "name" with
              | null -> sprintf "block_%04X" start
              | v -> string v
            let kind =
              match get "kind" with
              | null -> Code
              | v -> blockKindFromString (string v)
            { Start = start; EndExcl = endE; Name = name; Kind = kind } ]
    let comments =
      match prop "comments" with
      | null -> []
      | e ->
        [ let n: int = emitJsExpr e "$0.length"
          for i in 0 .. n - 1 do
            let mo: obj = emitJsExpr (e, i) "$0[$1]"
            let get (k: string) : obj = emitJsExpr (mo, k) "($0[$1] === undefined ? null : $0[$1])"
            let kind =
              match get "kind" with
              | null -> Line
              | v -> kindFromString (string v)
            let addr = match get "addr" with null -> 0 | v -> unbox<int> v
            let idx = match get "instrIndex" with null -> -1 | v -> unbox<int> v
            let endE = match get "endExcl" with null -> 0 | v -> unbox<int> v
            let txt = match get "text" with null -> "" | v -> string v
            { Kind = kind; Addr = addr; EndExcl = endE; InstrIndex = idx; Text = txt } ]
    let start = intOf "start" 0
    { ImageFile = str "image" "original.bin"
      Start = start
      EndExcl = intOf "endExcl" (if start > 0 then 0x10000 else 0)
      EntryPc = intOf "entryPc" start
      ActiveVersion = intOf "activeVersion" -1
      Blocks = blocks |> List.sortBy (fun b -> b.Start)
      Comments = comments
      Dirty = false }

  /// Upsert by key: line keyed on Addr, exec on InstrIndex, name/range on
  /// (Addr, EndExcl). Empty text removes the entry.
  let upsert (c: ControlFile) (m: ControlComment) : ControlFile =
    let sameKey (existing: ControlComment) =
      match m.Kind with
      | Line -> existing.Kind = Line && existing.Addr = m.Addr
      | Exec -> existing.Kind = Exec && existing.InstrIndex = m.InstrIndex
      | Name | Range ->
        (existing.Kind = m.Kind && existing.Addr = m.Addr && existing.EndExcl = m.EndExcl)
    let others = c.Comments |> List.filter (sameKey >> not)
    if String.IsNullOrWhiteSpace m.Text then
      { c with Comments = others; Dirty = true }
    else
      { c with Comments = others @ [{ m with Text = m.Text.Trim() }]; Dirty = true }

  /// The comment shown on an address row: exact Line hit first, else any
  /// covering Name/Range (most specific wins).
  let commentAt (c: ControlFile) (addr: int) : string option =
    let covering =
      c.Comments
      |> List.filter (fun m ->
        (m.Kind = Name || m.Kind = Range) && m.Addr <= addr && addr < m.EndExcl)
      |> List.sortBy (fun m ->
        (if m.Kind = Name then 1 else 0), (m.EndExcl - m.Addr))
    match
      c.Comments |> List.tryFind (fun m -> m.Kind = Line && m.Addr = addr)
      |> Option.orElse (covering |> List.tryHead)
    with
    | Some m -> Some m.Text
    | None -> None

  let blockAt (c: ControlFile) (addr: int) : Block option =
    c.Blocks |> List.tryFind (fun b -> b.Start <= addr && addr < b.EndExcl)

  let private withBlocks (c: ControlFile) (blocks: Block list) : ControlFile =
    { c with Blocks = blocks |> List.sortBy (fun b -> b.Start); Dirty = true }

  let renameBlockAt (c: ControlFile) (addr: int) (name: string) : ControlFile =
    { c with
        Blocks =
          c.Blocks
          |> List.map (fun b ->
            if b.Start <= addr && addr < b.EndExcl then { b with Name = name } else b)
        Dirty = true }

  let setKindAt (c: ControlFile) (addr: int) (kind: BlockKind) : ControlFile =
    { c with
        Blocks =
          c.Blocks
          |> List.map (fun b ->
            if b.Start <= addr && addr < b.EndExcl then { b with Kind = kind } else b)
        Dirty = true }

  let splitBlockAt (c: ControlFile) (addr: int) : ControlFile =
    match c.Blocks |> List.tryFind (fun b -> b.Start < addr && addr < b.EndExcl) with
    | None -> c
    | Some b ->
      let head = { b with EndExcl = addr }
      let tail = { b with Start = addr; Name = sprintf "block_%04X" addr }
      withBlocks c (head :: tail :: (c.Blocks |> List.filter ((<>) b)))

  let mergeWithNext (c: ControlFile) (addr: int) : ControlFile =
    match blockAt c addr with
    | None -> c
    | Some b ->
      let sorted = c.Blocks |> List.sortBy (fun x -> x.Start)
      match sorted |> List.skipWhile (fun x -> x <> b) |> List.skip 1 with
      | next :: rest when next.Start = b.EndExcl ->
        let merged = { b with EndExcl = next.EndExcl }
        withBlocks c (merged :: rest @ (sorted |> List.filter (fun x -> x <> b && x <> next)))
      | _ -> c
