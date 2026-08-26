namespace JetpacFR.Core

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

/// One comment in a game's control file. Kinds:
/// - Line:    comment attached to one address (both disassembly modes).
/// - Name:    region title (block name); the timeline selector shows it.
/// - Range:   free-text comment over an address range.
/// - Exec:    comment attached to a trace instruction index.
type CommentKind =
  | Line
  | Name
  | Range
  | Exec

type ControlComment =
  { Kind: CommentKind
    /// Address for Line; range start for Name/Range. Unused for Exec.
    Addr: int
    /// Exclusive end for Name/Range. 0 otherwise.
    EndExcl: int
    /// Trace instruction index for Exec. -1 otherwise.
    InstrIndex: int
    Text: string }

/// The per-game control file (games/<id>/control.json): the block map the
/// project generator consumes plus the comment store the GUI feeds. All
/// fields tolerate absence (older files keep loading).
type ControlFile =
  { ImageFile: string
    Start: int
    EndExcl: int
    EntryPc: int
    ActiveVersion: int
    Blocks: Block list
    Comments: ControlComment list
    /// Set when edits are not yet on disk (GUI dirty flag lives here so
    /// save/load roundtrips through one module).
    Dirty: bool }

module ControlFile =

  let private failf format = Printf.kprintf failwith format

  let empty (start: int) (endExcl: int) : ControlFile =
    { ImageFile = "original.bin"
      Start = start
      EndExcl = endExcl
      EntryPc = start
      ActiveVersion = -1
      Blocks =
        [ if endExcl > start then
            { Start = start; EndExcl = endExcl; Name = "whole_span"; Kind = Code } ]
      Comments = []
      Dirty = false }

  // ---- JSON --------------------------------------------------------------

  let kindToString =
    function Line -> "line" | Name -> "name" | Range -> "range" | Exec -> "exec"

  let private kindFromString (s: string) : CommentKind =
    match s with
    | "name" -> Name
    | "range" -> Range
    | "exec" -> Exec
    | _ -> Line

  /// Serialize to control.json shape (comments included). Written with an
  /// indented writer; key order is stable so diffs stay readable.
  let toJson (c: ControlFile) : string =
    use out = new IO.MemoryStream()
    do
      use w = new Utf8JsonWriter(out, JsonWriterOptions(Indented = true))
      w.WriteStartObject()
      w.WriteString("image", c.ImageFile)
      w.WriteNumber("start", c.Start)
      w.WriteNumber("endExcl", c.EndExcl)
      w.WriteNumber("entryPc", c.EntryPc)
      if c.ActiveVersion >= 0 then w.WriteNumber("activeVersion", c.ActiveVersion)
      w.WriteStartArray("blocks")
      for b in c.Blocks do
        w.WriteStartObject()
        w.WriteNumber("start", b.Start)
        w.WriteNumber("endExcl", b.EndExcl)
        w.WriteString("name", b.Name)
        w.WriteString("kind", (function Code -> "code" | Data -> "data" | Gap -> "gap") b.Kind)
        w.WriteEndObject()
      w.WriteEndArray()
      if not (List.isEmpty c.Comments) then
        w.WriteStartArray("comments")
        for m in c.Comments do
          w.WriteStartObject()
          w.WriteString("kind", kindToString m.Kind)
          if m.Kind = Exec then w.WriteNumber("instrIndex", m.InstrIndex)
          else w.WriteNumber("addr", m.Addr)
          if m.Kind = Name || m.Kind = Range then w.WriteNumber("endExcl", m.EndExcl)
          w.WriteString("text", m.Text)
          w.WriteEndObject()
        w.WriteEndArray()
      w.WriteEndObject()
    System.Text.Encoding.UTF8.GetString(out.ToArray())

  let fromJson (text: string) : ControlFile =
    use doc = JsonDocument.Parse text
    let root = doc.RootElement
    if root.ValueKind <> JsonValueKind.Object then failwith "control file: not a JSON object"
    let str (k: string) (def: string) =
      match root.TryGetProperty k with
      | true, v when v.ValueKind = JsonValueKind.String -> v.GetString()
      | _ -> def
    let intOf (k: string) (def: int) =
      match root.TryGetProperty k with
      | true, v when v.ValueKind = JsonValueKind.Number -> v.GetInt32()
      | _ -> def
    let blocks =
      match root.TryGetProperty "blocks" with
      | true, e when e.ValueKind = JsonValueKind.Array ->
        [ for b in e.EnumerateArray() do
            let get (k: string) =
              match b.TryGetProperty k with
              | true, v -> Some v
              | _ -> None
            let start = (get "start").Value.GetInt32()
            let endE = (get "endExcl").Value.GetInt32()
            let name =
              match get "name" with
              | Some v when v.ValueKind = JsonValueKind.String -> v.GetString()
              | _ -> sprintf "block_%04X" start
            let kind =
              match get "kind" with
              | Some v when v.ValueKind = JsonValueKind.String ->
                match v.GetString() with
                | "data" -> Data
                | "gap" -> Gap
                | _ -> Code
              | _ -> Code
            { Start = start; EndExcl = endE; Name = name; Kind = kind } ]
      | _ -> []
    let comments =
      match root.TryGetProperty "comments" with
      | true, e when e.ValueKind = JsonValueKind.Array ->
        [ for m in e.EnumerateArray() do
            let get (k: string) =
              match m.TryGetProperty k with
              | true, v -> Some v
              | _ -> None
            let kind =
              match get "kind" with
              | Some v when v.ValueKind = JsonValueKind.String -> kindFromString (v.GetString())
              | _ -> Line
            let addr = (get "addr") |> Option.map (fun v -> v.GetInt32()) |> Option.defaultValue 0
            let idx = (get "instrIndex") |> Option.map (fun v -> v.GetInt32()) |> Option.defaultValue -1
            let endE = (get "endExcl") |> Option.map (fun v -> v.GetInt32()) |> Option.defaultValue 0
            let txt =
              match get "text" with
              | Some v when v.ValueKind = JsonValueKind.String -> v.GetString()
              | _ -> ""
            { Kind = kind; Addr = addr; EndExcl = endE; InstrIndex = idx; Text = txt } ]
      | _ -> []
    let start = intOf "start" 0
    let endExcl = intOf "endExcl" (if start > 0 then 0x10000 else 0)
    { ImageFile = str "image" "original.bin"
      Start = start
      EndExcl = endExcl
      EntryPc = intOf "entryPc" start
      ActiveVersion = intOf "activeVersion" -1
      Blocks = blocks |> List.sortBy (fun b -> b.Start)
      Comments = comments
      Dirty = false }

  let load (dir: string) : ControlFile =
    fromJson (File.ReadAllText(Path.Combine(dir, "control.json")))

  /// Persist only when dirty; always clears the flag in the returned copy.
  let save (dir: string) (c: ControlFile) : ControlFile =
    File.WriteAllText(Path.Combine(dir, "control.json"), toJson c)
    { c with Dirty = false }

  /// Load-or-create: missing file yields a whole-span skeleton over the
  /// image's non-zero extent (or the given fallback span).
  let loadOrCreate (dir: string) (fallbackSpan: int * int) : ControlFile =
    let path = Path.Combine(dir, "control.json")
    if File.Exists path then load dir
    else
      let mem =
        let imgPath = Path.Combine(dir, "original.bin")
        if File.Exists imgPath then
          let raw = File.ReadAllBytes imgPath
          let rec lastNonZero (i: int) : int =
            if i < 0 then 0
            elif raw[i] <> 0uy then i + 1
            else lastNonZero (i - 1)
          max 1 (lastNonZero (raw.Length - 1))
        else 0
      let start, endE =
        if mem > 0 then 0, mem else fallbackSpan
      empty start endE

  // ---- comment ops -------------------------------------------------------

  /// Upsert by key: line keyed on Addr, exec on InstrIndex, name/range on
  /// (Addr, EndExcl). Empty text removes the entry (typing nothing = clear).
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
  /// Name/Range covering it.
  let commentAt (c: ControlFile) (addr: int) : string option =
    // Exact line hit wins; else the most SPECIFIC covering entry
    // (Range over Name, then narrowest span).
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

  let namesSorted (c: ControlFile) : ControlComment list =
    c.Comments |> List.filter (fun m -> m.Kind = Name) |> List.sortBy (fun m -> m.Addr)

  /// Region names sorted by address (timeline + regen headers).
  let pcsInFrames (t: Trace) (fStart: int) (fEnd: int) : int list =
    if t.Entries.Length = 0 || t.FrameTicks.Length = 0 then []
    else
      let tickAt (f: int) =
        if f <= 0 then uint32 0
        elif f >= t.FrameTicks.Length then t.FrameTicks[t.FrameTicks.Length - 1]
        else t.FrameTicks[f]
      let lo = min fStart fEnd
      let hi = max fStart fEnd
      let loTick = tickAt lo
      let hiTick = tickAt hi
      let seen = System.Collections.Generic.HashSet<int>()
      for e in t.Entries do
        if e.Tick >= loTick && e.Tick < hiTick then seen.Add(int e.Pc) |> ignore
      seen |> Seq.toList |> List.sort

  // ---- block ops ---------------------------------------------------------

  /// The block containing addr, if any.
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

  /// Split the block containing addr at an instruction start; a no-op when
  /// addr sits on an edge or outside any block.
  let splitBlockAt (c: ControlFile) (addr: int) : ControlFile =
    match c.Blocks |> List.tryFind (fun b -> b.Start < addr && addr < b.EndExcl) with
    | None -> c
    | Some b ->
      let head = { b with EndExcl = addr }
      let tail = { b with Start = addr; Name = sprintf "block_%04X" addr }
      withBlocks c (head :: tail :: (c.Blocks |> List.filter ((<>) b)))

  /// Merge the block containing addr into the block that follows it.
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

  /// Map frame ranges to PC lists via pcsInFrames; A-only / B-only sets
  /// derive from set difference of these.
  let diffPcs (inA: int list) (inB: int list) : int list * int list =
    let sa = System.Collections.Generic.HashSet<int>(inA)
    let sb = System.Collections.Generic.HashSet<int>(inB)
    let oa = inA |> List.filter (fun p -> not (sb.Contains p)) |> List.distinct
    let ob = inB |> List.filter (fun p -> not (sa.Contains p)) |> List.distinct
    oa, ob

// ---- SkoolKit interop ------------------------------------------------------

module SkoolCtl =

  let private parseAddrTok (tok: string) : int option =
    let t = tok.Trim()
    if t.Length = 0 then None
    elif t.StartsWith "$" then
      match Int32.TryParse(t.Substring(1), Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture) with
      | true, v -> Some v
      | _ -> None
    else
      match Int32.TryParse(t, Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture) with
      | true, v -> Some v
      | _ -> None

  /// Parse a SkoolKit control file (block directives + N/D/E comments).
  /// Sub-blocks (B/C/S/T/W/M/L/@/./:/>), loops and number-base suffixes are
  /// accepted-but-skipped: their addresses still land as block boundaries.
  /// Returns a ControlFile-shaped record (blocks + comments).
  let import (text: string) : ControlFile =
    let lines = text.Replace("\r\n", "\n").Split('\n')
    let mutable blocks = []
    let mutable comments = []
    let mutable minAddr = Int32.MaxValue
    let mutable maxAddr = 0
    for ln in lines do
      let line = ln.TrimEnd()
      if line.Length > 0 && not (line.StartsWith "#") && not (line.StartsWith "%") then
        if line.StartsWith ";" then ()
        else
          let parts = line.Split(' ', 2)
          let d = parts[0].Trim()
          let rest = if parts.Length > 1 then parts[1].Trim() else ""
          match d with
          | "c" | "b" | "g" | "i" | "s" | "t" | "u" | "w" ->
            let toks = rest.Split(' ', 2)
            match parseAddrTok toks[0] with
            | Some addr ->
              let title = if toks.Length > 1 then toks[1] else ""
              let kind =
                match d with
                | "b" | "g" | "s" | "w" | "t" -> Data
                | "u" | "i" -> Gap
                | _ -> Code
              blocks <- { Start = addr; EndExcl = 0; Name = title; Kind = kind } :: blocks
              if not (String.IsNullOrWhiteSpace title) then
                comments <-
                  { Kind = Name; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = title } :: comments
              minAddr <- min minAddr addr
              maxAddr <- max maxAddr addr
            | None -> ()
          | "D" ->
            let toks = rest.Split(' ', 2)
            match parseAddrTok toks[0] with
            | Some addr ->
              let txt = if toks.Length > 1 then toks[1] else ""
              comments <- { Kind = Range; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = txt } :: comments
            | None -> ()
          | "N" ->
            let toks = rest.Split(' ', 2)
            match parseAddrTok toks[0] with
            | Some addr ->
              let txt = if toks.Length > 1 then toks[1] else ""
              comments <- { Kind = Line; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = txt } :: comments
            | None -> ()
          | _ ->
            // Sub-blocks/directives: harvest the leading address if present.
            let tok = d.Split(',')[0]
            match parseAddrTok tok with
            | Some addr ->
              minAddr <- min minAddr addr
              maxAddr <- max maxAddr addr
            | None -> ()
    // Close each block at its successor's start (SkoolKit semantics); the
    // final block ends where data understanding stops - span end.
    let sorted =
      blocks
      |> List.sortBy (fun b -> b.Start)
      |> List.map (fun b -> b)
    let closed =
      sorted
      |> List.mapi (fun i b ->
        let next =
          if i + 1 < sorted.Length then sorted[i + 1].Start
          else max (maxAddr + 1) (if sorted.IsEmpty then 0 else List.head sorted |> fun x -> x.Start + 1)
        { b with EndExcl = max b.Start (next) })
    let start = if sorted.IsEmpty then 0 else sorted[0].Start
    let endE =
      if closed.IsEmpty then 0
      else (closed |> List.maxBy (fun b -> b.EndExcl)).EndExcl
    { ImageFile = "original.bin"
      Start = start
      EndExcl = endE
      EntryPc = start
      ActiveVersion = -1
      Blocks = closed |> List.filter (fun b -> b.EndExcl > b.Start)
      Comments = comments |> List.rev
      Dirty = false }

  /// Export blocks + comments as SkoolKit ctrl text (decimal addresses,
  /// matching skool2ctl.py output style).
  let export (c: ControlFile) : string =
    let sb = Text.StringBuilder()
    let letter =
      function Code -> "c" | Data -> "b" | Gap -> "u"
    for b in c.Blocks do
      let title =
        c.Comments
        |> List.tryFind (fun m -> m.Kind = Name && m.Addr = b.Start)
        |> Option.map (fun m -> " " + m.Text)
        |> Option.defaultWith (fun () -> "")
      sb.AppendLine(sprintf "%s %d%s" (letter b.Kind) b.Start title) |> ignore
    for m in c.Comments do
      match m.Kind with
      | Line -> sb.AppendLine(sprintf "N %d %s" m.Addr m.Text) |> ignore
      | Range -> sb.AppendLine(sprintf "D %d %s" m.Addr m.Text) |> ignore
      | Exec -> sb.AppendLine(sprintf "; [exec #%d] %s" m.InstrIndex m.Text) |> ignore
      | Name -> () // already emitted as block titles
    sb.ToString()
