namespace JetpacFR.Web

open System
open System.Collections.Generic
open Fable.Core
open Fable.Core.JsInterop
open JetpacFR.Core

/// DOM plumbing: bare createElement/getElementById sugar over emitJsExpr,
/// the same style JetpacFSharp's web shell uses.
module Dom =

  let document: obj = emitJsExpr () "document"
  let window: obj = emitJsExpr () "window"

  let byId (id: string) : obj = emitJsExpr id "document.getElementById($0)"
  let el (tag: string) : obj = emitJsExpr tag "document.createElement($0)"
  let append (parent: obj) (child: obj) : obj = emitJsExpr (parent, child) "$0.appendChild($1)"
  let clear (node: obj) : unit = node?textContent <- ""
  let setTimeout (f: unit -> unit) (ms: int) : obj = emitJsExpr (f, ms) "setTimeout($0, $1)"
  let setInterval (f: unit -> unit) (ms: int) : obj = emitJsExpr (f, ms) "setInterval($0, $1)"

/// Beeper playback via Web Audio: 44100 Hz mono. Per-frame samples from
/// DrainBeeperSamples land in a 2-second ring buffer; a ScriptProcessorNode
/// pulls from it (zero-fill on underrun). Mirrors the desktop NAudio module:
/// muted by default, 20% volume when enabled. ScriptProcessorNode is
/// deprecated but supported everywhere; an AudioWorklet would need a second
/// module file.
module Audio =

  let mutable Enabled = false
  let private ring = Array.zeroCreate<float32> 88200
  let mutable private wpos = 0
  let mutable private rpos = 0
  let mutable private ctx: obj = null
  let mutable private node: obj = null

  let private ensure () =
    if isNull ctx then
      ctx <-
        emitJsExpr ()
          "((window.AudioContext || window.webkitAudioContext) ? new (window.AudioContext || window.webkitAudioContext)() : null)"
      if not (isNull ctx) then
        node <- emitJsExpr ctx "$0.createScriptProcessor(4096, 0, 1)"
        node?onaudioprocess <- fun (e: obj) ->
          let out: float32[] = unbox (e?outputBuffer?getChannelData(0))
          let n: int = out.Length
          for i in 0 .. n - 1 do
            if rpos <> wpos then
              out.[i] <- ring.[rpos]
              rpos <- (rpos + 1) % ring.Length
            else
              out.[i] <- 0.0f
        emitJsExpr (node, ctx) "$0.connect($1.destination)" |> ignore

  let Play (samples: float32[]) =
    if Enabled then
      ensure ()
      if not (isNull ctx) then
        for s in samples do
          ring.[wpos] <- s * 0.2f
          wpos <- (wpos + 1) % ring.Length

  let SetEnabled (on: bool) =
    Enabled <- on
    if on then
      ensure ()
      if not (isNull ctx) then
        emitJsExpr ctx "$0.resume()" |> ignore
    // flush any stale buffered samples
    rpos <- wpos

module App =

  // ---- palette (matches the desktop) -------------------------------------
  let private bg = "#101016"
  let private panel = "#181C24"
  let private normal = "#C8C8CE"
  let private dim = "#8A8A92"
  let private green = "#4EE060"
  let private cyan = "#4ED0E0"
  let private yellow = "#E6D04E"
  let private red = "#E85454"
  let private orange = "#E88A2E"
  let private hiBg = "#2E3444"
  let private mono = "Consolas, monospace"

  let private heatLut =
    [| "#08080C"; "#0A1E4A"; "#0A3A5E"; "#0A5C6A"; "#12865E"; "#3AA83C"; "#96B82C"; "#D8A022"; "#E86618"; "#F03828" |]

  // ---- UI element refs (bound once in start) -----------------------------
  let mutable private statusText: obj = null
  let mutable private disasmBox: obj = null
  let mutable private cursorLabel: obj = null
  let mutable private slider: obj = null
  let mutable private pcCell: obj = null
  let mutable private routineList: obj = null
  let mutable private routineCountLabel: obj = null
  let mutable private routineDetail: obj = null
  let mutable private tray: obj = null
  let mutable private trayCountLabel: obj = null
  let mutable private contractText: obj = null
  let mutable private contractLabel: obj = null
  let mutable private theaterText: obj = null
  let mutable private pasteBox: obj = null
  let mutable private playBtn: obj = null
  let mutable private chkRec: obj = null
  let mutable private chkMute: obj = null
  let mutable private gameCtx: obj = null
  let mutable private gameImg: obj = null
  let mutable private heatCtx: obj = null
  let mutable private heatImg: obj = null
  let mutable private stripCtx: obj = null
  let mutable private stripImg: obj = null
  let mutable private graphCtx: obj = null
  let mutable private graphCanvas: obj = null
  let mutable private frameTimerId: obj = null
  let mutable private cinemaTimerId: obj = null

  // ---- emulator state ----------------------------------------------------
  let mutable private session: TraceSession option = None
  let mutable private running = false
  let mutable private cursor = 0
  let mutable private built: Trace option = None
  let mutable private builtAtCount = -1
  let mutable private loaded: Trace option = None
  let mutable private syncingSlider = false
  let mutable private syncingRoutines = false
  let mutable private cinemaPlaying = false
  let mutable private cinemaSpeed = 10
  let mutable private markerPc = 0
  let mutable private minedRoutines: Miner.Routine list = []
  let mutable private minedEdges: Miner.CallEdge list = []
  let mutable private selectedEntries: Set<int> = Set.empty
  let mutable private activeRoutine: Miner.Routine option = None
  let mutable private pcCyclesCache: int64[] option = None
  let mutable private pcCyclesForTrace: Trace = Unchecked.defaultof<Trace>

  // node positions for the call-graph canvas hit test
  let private graphNodes = ResizeArray<int * float * float>()
  let private regCells = Dictionary<string, obj>()
  let private flagCells = Array.init 8 (fun _ -> (null: obj))

  /// Web ring-buffer capacity. JS objects are ~6x heavier than .NET structs,
  /// so this is smaller than the desktop's 4M (still ~40 seconds of play).
  let private capacity = 1_000_000

  // ---- physical key -> ZX matrix cells (same layout as the desktop) ------
  let private keyMap (key: string) : (int * int) list =
    match key with
    | "a" -> [ (1, 0) ]
    | "s" -> [ (1, 1) ]
    | "d" -> [ (1, 2) ]
    | "f" -> [ (1, 3) ]
    | "g" -> [ (1, 4) ]
    | "q" -> [ (2, 0) ]
    | "w" -> [ (2, 1) ]
    | "e" -> [ (2, 2) ]
    | "r" -> [ (2, 3) ]
    | "t" -> [ (2, 4) ]
    | "p" -> [ (5, 0) ]
    | "o" -> [ (5, 1) ]
    | "i" -> [ (5, 2) ]
    | "u" -> [ (5, 3) ]
    | "y" -> [ (5, 4) ]
    | "l" -> [ (6, 1) ]
    | "k" -> [ (6, 2) ]
    | "j" -> [ (6, 3) ]
    | "h" -> [ (6, 4) ]
    | "m" -> [ (7, 2) ]
    | "n" -> [ (7, 3) ]
    | "b" -> [ (7, 4) ]
    | "z" -> [ (0, 1) ]
    | "x" -> [ (0, 2) ]
    | "c" -> [ (0, 3) ]
    | "v" -> [ (0, 4) ]
    | "1" -> [ (3, 0) ]
    | "2" -> [ (3, 1) ]
    | "3" -> [ (3, 2) ]
    | "4" -> [ (3, 3) ]
    | "5" -> [ (3, 4) ]
    | "0" -> [ (4, 0) ]
    | "9" -> [ (4, 1) ]
    | "8" -> [ (4, 2) ]
    | "7" -> [ (4, 3) ]
    | "6" -> [ (4, 4) ]
    | " " -> [ (7, 0) ]
    | "Enter" -> [ (6, 0) ]
    | "Shift" -> [ (0, 0) ]
    | "Control" -> [ (7, 1) ]
    | "ArrowLeft" -> [ (0, 0); (3, 4) ]
    | "ArrowRight" -> [ (0, 0); (4, 2) ]
    | "ArrowUp" -> [ (0, 0); (4, 3) ]
    | "ArrowDown" -> [ (0, 0); (4, 4) ]
    | _ -> []

  let private setKeyFor (key: string) (pressed: bool) =
    match session with
    | Some s -> List.iter (fun (r, b) -> s.SetKey(r, b, pressed)) (keyMap key)
    | None -> ()

  let private isCall (b: byte) =
    match b with
    | 0xCDuy | 0xC4uy | 0xCCuy | 0xD4uy | 0xDCuy | 0xE4uy | 0xECuy | 0xF4uy | 0xFCuy -> true
    | _ -> false

  let private isRet (b: byte) =
    match b with
    | 0xC9uy | 0xC0uy | 0xC8uy | 0xD0uy | 0xD8uy | 0xE0uy | 0xE8uy | 0xF0uy | 0xF8uy -> true
    | _ -> false

  let private isBranch (b: byte) =
    isCall b || isRet b
    || match b with
       | 0xC3uy | 0xC2uy | 0xCAuy | 0xD2uy | 0xDAuy | 0xE2uy | 0xEAuy | 0xF2uy | 0xFAuy
       | 0x10uy | 0x18uy | 0x20uy | 0x28uy | 0x30uy | 0x38uy | 0xE9uy -> true
       | _ -> false

  // ---- trace plumbing ----------------------------------------------------
  let private ensureBuilt () =
    match session with
    | Some s ->
      let recorder = s.Recorder
      if builtAtCount <> recorder.EntryCount then
        built <- Some(recorder.Build())
        builtAtCount <- recorder.EntryCount
    | None -> ()

  let private currentTrace () : Trace option =
    match loaded with
    | Some t -> Some t
    | None ->
      ensureBuilt ()
      built

  let private currentEntryCount () =
    match currentTrace () with
    | Some t -> t.Entries.Length
    | None -> 0

  let private clampCursor () =
    let n = currentEntryCount ()
    if n = 0 then cursor <- 0
    else cursor <- max 0 (min cursor (n - 1))

  let private buildTraceNow () = ensureBuilt ()

  // ---- view refresh ------------------------------------------------------
  let private paintGame () =
    match session with
    | Some s ->
      let src: byte[] = s.ScreenBuffer
      let buf: Fable.Core.JS.Uint8ClampedArray = unbox (gameImg?data)
      for i in 0 .. 320 * 256 - 1 do
        let o = i * 4
        buf.[o] <- src.[o + 2]
        buf.[o + 1] <- src.[o + 1]
        buf.[o + 2] <- src.[o]
        buf.[o + 3] <- src.[o + 3]
      gameCtx?putImageData(gameImg, 0, 0)
    | None -> ()

  /// Static linear disassembly of port memory around `pc`, shown when the
  /// emulator stops (e.g. "no code at 0xNNNN").
  let private showErrorDisasm (pc: int) =
    match session with
    | Some s ->
      let mem = s.Memory
      let start = ((pc - 0x40) + 0x10000) &&& 0xFFF8
      let rows = ResizeArray<string>()
      let mutable addr = start
      while rows.Count < 48 && addr <= start + 0x80 do
        let insn = Disasm.disasmMemory mem addr
        let hex =
          [ for i in 0 .. insn.Length - 1 -> sprintf "%02X" mem.[(addr + i) &&& 0xFFFF] ]
          |> String.concat " "
        let tag =
          if addr = pc then
            sprintf "-> %04X  %-11s  %s   <<< cannot execute here" addr hex insn.Text
          else
            sprintf "   %04X  %-11s  %s" addr hex insn.Text
        rows.Add tag
        addr <- (addr + insn.Length) &&& 0xFFFF
      Dom.clear disasmBox
      for r in rows do
        let d = Dom.el "div"
        d?textContent <- r
        d?className <- "disasm-row"
        Dom.append disasmBox d |> ignore
      cursorLabel?textContent <- sprintf "emulator stopped - static disassembly around 0x%04X" pc
    | None -> ()

  let private renderFrame () =
    match session with
    | Some s ->
      try
        let frameStart, _ = s.RunFrame()
        paintGame ()
        Audio.Play(s.DrainBeeperSamples(frameStart))
      with ex ->
        running <- false
        let pc = s.Regs.Pc()
        statusText?textContent <- sprintf "emulator error at 0x%04X: %s" pc ex.Message
        showErrorDisasm pc
    | None -> ()

  let private currentSelfModified () : bool[] =
    match loaded with
    | Some t -> t.SelfModified
    | None ->
      match session with
      | Some s -> s.Recorder.SelfModified
      | None -> Array.zeroCreate<bool> 0x10000

  let private rowFor (t: Trace) (e: TraceEntry) (idx: int) (isCurrent: bool) : string * string =
    if e.Length = 0uy then
      (sprintf "%07d  ----  INT -> %04X   (%d tstates)" idx e.Target e.Cycles, red)
    else
      let bytes = [| e.B0; e.B1; e.B2; e.B3 |]
      let insn = Disasm.disasmBytes bytes 0
      let hex =
        [ for i in 0 .. int e.Length - 1 -> sprintf "%02X" bytes.[i] ]
        |> String.concat " "
      let tail =
        if e.Taken = 1uy then sprintf "   -> %04X" e.Target
        elif isBranch e.B0 then "   (not taken)"
        else ""
      let sm =
        if (currentSelfModified ()).[int e.Pc] then "   [self-mod]" else ""
      let brush =
        if isCall e.B0 then cyan
        elif isRet e.B0 then yellow
        elif isBranch e.B0 then green
        else normal
      (sprintf "%07d  %04X  %-11s  %s%s%s" idx e.Pc hex insn.Text tail sm, brush)

  let private refreshDisasm () =
    Dom.clear disasmBox
    match currentTrace () with
    | None -> ()
    | Some t ->
      if t.Entries.Length > 0 then
        let c = max 0 (min cursor (t.Entries.Length - 1))
        for j in -20 .. 20 do
          let idx = c + j
          if idx >= 0 && idx < t.Entries.Length then
            let tag, brush = rowFor t t.Entries.[idx] idx (idx = c)
            let d = Dom.el "div"
            d?textContent <- tag
            d?className <- "disasm-row" + (if idx = c then " current" else "")
            d?style?color <- brush
            Dom.append disasmBox d |> ignore

  let private setReg (name: string) (v: int) =
    match regCells.TryGetValue name with
    | true, cell -> cell?textContent <- sprintf "%04X" v
    | _ -> ()

  let private updateFlags (af: int) =
    let f = af &&& 0xFF
    let bits = [| 0x80; 0x40; 0x20; 0x10; 0x08; 0x04; 0x02; 0x01 |]
    for i in 0 .. 7 do
      flagCells.[i]?textContent <- if f &&& bits.[i] <> 0 then "1" else "0"
      flagCells.[i]?style?color <- if f &&& bits.[i] <> 0 then green else dim

  let private refreshRegs () =
    match currentTrace () with
    | Some t when t.Entries.Length > 0 ->
      let c = max 0 (min cursor (t.Entries.Length - 1))
      let e = t.Entries.[c]
      let snapIdx = TraceQuery.nearestSnapshotBefore t.Snapshots e.Tick
      if snapIdx >= 0 then
        let s = t.Snapshots.[snapIdx]
        setReg "AF" (int s.Af)
        setReg "BC" (int s.Bc)
        setReg "DE" (int s.De)
        setReg "HL" (int s.Hl)
        setReg "AF'" (int s.Af2)
        setReg "BC'" (int s.Bc2)
        setReg "DE'" (int s.De2)
        setReg "HL'" (int s.Hl2)
        setReg "IX" (int s.Ix)
        setReg "IY" (int s.Iy)
        setReg "SP" (int s.Sp)
        setReg "I" (int s.I)
        setReg "R" (int s.R)
        pcCell?textContent <- sprintf "%04X" e.Pc
        updateFlags (int s.Af)
      markerPc <- int e.Pc
    | _ -> ()

  let private riskColor (risk: string) =
    match risk with
    | "green" -> green
    | "yellow" -> yellow
    | "orange" -> orange
    | "red" -> red
    | _ -> normal

  let private refreshHeatmap () =
    let counts =
      match currentTrace () with
      | Some t -> t.PerPcCount
      | None -> Array.zeroCreate<int> 0x10000
    let selfMod = currentSelfModified ()
    let maxC = max 1 (Array.max counts)
    let logMax = log10 (float maxC)
    let buf: Fable.Core.JS.Uint8ClampedArray = unbox (heatImg?data)
    let mutable i = 0
    for y in 0 .. 255 do
      for x in 0 .. 255 do
        let c = counts.[i]
        let li = if c <= 0 then 0 else min 9 (int (9.0 * (log10 (float c) / logMax)))
        let col = heatLut.[li]
        let p = i * 4
        let r = Convert.ToInt32(col.Substring(1, 2), 16)
        let g = Convert.ToInt32(col.Substring(3, 2), 16)
        let b = Convert.ToInt32(col.Substring(5, 2), 16)
        if selfMod.[i] then
          // executed-then-written: blend toward magenta (matches the desktop)
          buf.[p] <- byte ((b + 0xC0) / 2)
          buf.[p + 1] <- byte ((g + 0x30) / 2)
          buf.[p + 2] <- byte ((r + 0xF0) / 2)
        else
          buf.[p] <- byte b
          buf.[p + 1] <- byte g
          buf.[p + 2] <- byte r
        buf.[p + 3] <- 255uy
        i <- i + 1
    // white cursor marker around the current PC
    let pc = markerPc
    let x = pc &&& 0xFF
    let y = pc >>> 8
    for dy in -1 .. 1 do
      for dx in -1 .. 1 do
        let yy = y + dy
        if yy >= 0 && yy < 256 then
          let p = ((yy * 256) + ((x + dx) &&& 0xFF)) * 4
          buf.[p] <- 255uy
          buf.[p + 1] <- 255uy
          buf.[p + 2] <- 255uy
          buf.[p + 3] <- 255uy
    heatCtx?putImageData(heatImg, 0, 0)

  let private segmentsOf (t: Trace) : int[] =
    let n = max 1 (t.Entries.Length / 512)
    let segs = Array.zeroCreate<int> 512
    for e in t.Entries do
      segs.[min 511 (int e.Tick / n)] <- segs.[min 511 (int e.Tick / n)] + 1
    segs

  let private refreshStrip () =
    let segs =
      match loaded with
      | Some t -> segmentsOf t
      | None ->
        match session with
        | Some s -> s.Recorder.SegmentCounts
        | None -> Array.empty
    if segs.Length > 0 then
      let maxS = max 1 (Array.max segs)
      let logMax = log10 (float (maxS + 1))
      let buf: Fable.Core.JS.Uint8ClampedArray = unbox (stripImg?data)
      for sx in 0 .. segs.Length - 1 do
        let v = log10 (float (segs.[sx] + 1)) / logMax
        let r = 60 + int (v * 160.0)
        let g = 120 + int (v * 100.0)
        let b = 60 + int (v * 60.0)
        for sy in 0 .. 23 do
          let p = (sy * 512 + sx) * 4
          buf.[p] <- byte b
          buf.[p + 1] <- byte g
          buf.[p + 2] <- byte r
          buf.[p + 3] <- 255uy
      let n = currentEntryCount ()
      if n > 1 then
        let cx = int (int64 cursor * 511L / int64 (n - 1))
        for sy in 0 .. 23 do
          let p = (sy * 512 + cx) * 4
          buf.[p] <- 255uy
          buf.[p + 1] <- 255uy
          buf.[p + 2] <- 255uy
          buf.[p + 3] <- 255uy
      stripCtx?putImageData(stripImg, 0, 0)

  let private refreshCursorLabel () =
    let n = currentEntryCount ()
    if n = 0 then cursorLabel?textContent <- "no trace yet - play the game"
    else
      let c = max 0 (min cursor (n - 1))
      cursorLabel?textContent <-
        sprintf "instr %d / %d   pc=%04X" c (n - 1) (int (currentTrace()).Value.Entries.[c].Pc)

  let private refreshAll () =
    refreshDisasm ()
    refreshRegs ()
    refreshHeatmap ()
    refreshStrip ()
    refreshCursorLabel ()

  let private syncSlider () =
    let n = currentEntryCount ()
    syncingSlider <- true
    if n > 1 then
      slider?min <- 0
      slider?max <- n - 1
      slider?value <- max 0 (min cursor (n - 1))
      slider?disabled <- false
    else
      slider?disabled <- true
      slider?value <- 0
    syncingSlider <- false

  // ---- phase 2: mined routines, call graph, selection --------------------
  /// Per-PC total cycle counts over the current trace, cached per trace
  /// instance (reference identity), for contract averages.
  let private getPcCycles () : int64[] =
    match currentTrace () with
    | None -> Array.zeroCreate<int64> 0x10000
    | Some t ->
      if not (System.Object.ReferenceEquals(pcCyclesForTrace, t)) then
        let cyc = Array.zeroCreate<int64> 0x10000
        for e in t.Entries do
          if e.Length <> 0uy then cyc.[int e.Pc] <- cyc.[int e.Pc] + int64 e.Cycles
        pcCyclesCache <- Some cyc
        pcCyclesForTrace <- t
      match pcCyclesCache with
      | Some c -> c
      | None -> Array.zeroCreate<int64> 0x10000

  let private generateContract () =
    match activeRoutine with
    | None -> contractText?value <- "select a mined routine to generate its contract + prompt"
    | Some r ->
      match currentTrace (), session with
      | Some t, Some s ->
        let contract = Contract.extract s.Memory t r (getPcCycles ())
        let prompt = Prompt.generate contract
        contractText?value <- prompt
        contractLabel?textContent <-
          sprintf "contract for 0x%04X  span=%04X-%04X  calls=%d  writes=%d"
            r.Entry r.SpanLo r.SpanHi r.CallCount contract.WriteRanges.Length
      | _ -> contractText?value <- "no trace to extract from"

  let private maxDepth (columns: Dictionary<int, ResizeArray<int>>) : int =
    let mutable m = 0
    for kv in columns do m <- max m kv.Key
    m

  let private showRoutineDetail (r: Miner.Routine) =
    let loops =
      r.LoopExtents
      |> List.truncate 5
      |> List.map (fun (back, tgt, iters) -> sprintf "%04X->%04X x%d" back tgt iters)
    routineDetail?textContent <-
      sprintf "%04X  (%s)\ncalls=%d  span=%04X-%04X\nincl=%d  excl=%d tstates\nself-modifying=%b  overlapping=%b\n%s%s\ninput samples: %d"
        r.Entry r.Risk r.CallCount r.SpanLo r.SpanHi r.InclusiveTStates r.ExclusiveTStates
        r.SelfModifying r.Overlapping
        (r.Reasons |> String.concat "; ")
        (if List.isEmpty loops then "" else "\nloops: " + String.concat ", " loops)
        r.InputSamples.Length

  let rec private refreshRoutines () =
    syncingRoutines <- true
    Dom.clear routineList
    for r in minedRoutines do
      let row = Dom.el "div"
      row?textContent <-
        sprintf "%04X  calls=%5d  span=%04X-%04X  incl=%7d  excl=%7d  %s"
          r.Entry r.CallCount r.SpanLo r.SpanHi r.InclusiveTStates r.ExclusiveTStates r.Risk
      row?className <- "rout-row" + (if selectedEntries.Contains r.Entry then " selected" else "")
      row?style?color <- riskColor r.Risk
      row?addEventListener("click", fun _ -> selectRoutine r.Entry true)
      Dom.append routineList row |> ignore
    routineCountLabel?textContent <- sprintf "%d routines" minedRoutines.Length
    syncingRoutines <- false

  and private refreshTray () =
    Dom.clear tray
    for entry in selectedEntries |> Set.toList do
      match minedRoutines |> List.tryFind (fun r -> r.Entry = entry) with
      | Some r ->
        let chip = Dom.el "button"
        chip?textContent <- sprintf "%04X x%d" r.Entry r.CallCount
        chip?className <- "chip"
        chip?addEventListener("click", fun _ -> selectRoutine r.Entry false)
        Dom.append tray chip |> ignore
      | None -> ()
    trayCountLabel?textContent <- sprintf "%d in lift queue" selectedEntries.Count

  and private drawGraph () =
    Dom.clear graphNodes
    if List.isEmpty minedRoutines then
      graphCanvas?width <- 400
      graphCanvas?height <- 60
      graphCtx?fillStyle <- dim
      graphCtx?font <- "12px " + mono
      graphCtx?textAlign <- "left"
      graphCtx?fillText("mine a trace first (Functions tab)", 8, 30)
    else
      // BFS-ish depth assignment from the synthetic root (-1).
      let depth = Dictionary<int, int>()
      depth.[-1] <- 0
      for r in minedRoutines do depth.[r.Entry] <- 100
      let mutable changed = true
      let mutable guard = 0
      while changed && guard < 20 do
        changed <- false
        guard <- guard + 1
        for e in minedEdges do
          let dFrom =
            match depth.TryGetValue e.Caller with
            | true, v -> v
            | _ -> 100
          if dFrom < 100 && depth.[e.Callee] > dFrom + 1 then
            depth.[e.Callee] <- dFrom + 1
            changed <- true
      let colWidth = 150.0
      let rowHeight = 44.0
      let columns = Dictionary<int, ResizeArray<int>>()
      for r in minedRoutines do
        let d =
          match depth.TryGetValue r.Entry with
          | true, v -> min v 9
          | _ -> 1
        match columns.TryGetValue d with
        | true, list -> list.Add r.Entry
        | _ ->
          let list = ResizeArray<int>()
          list.Add r.Entry
          columns.[d] <- list
      let pos = Dictionary<int, float * float>()
      for kv in columns do
        kv.Value |> Seq.iteri (fun row entry ->
          pos.[entry] <- (20.0 + float kv.Key * colWidth, 20.0 + float row * rowHeight))
      pos.[-1] <- (20.0, 20.0)
      graphCanvas?width <- 40.0 + (float (maxDepth columns) + 1.0) * colWidth
      graphCanvas?height <-
        40.0 + float (columns.Values |> Seq.map (fun l -> l.Count) |> Seq.fold max 0) * rowHeight
      // edges under nodes
      graphCtx?strokeStyle <- dim
      graphCtx?fillStyle <- dim
      graphCtx?font <- "10px " + mono
      graphCtx?textAlign <- "center"
      for e in minedEdges do
        match pos.TryGetValue e.Caller, pos.TryGetValue e.Callee with
        | (true, (x1, y1)), (true, (x2, y2)) ->
          graphCtx?beginPath()
          graphCtx?moveTo(x1 + 104.0, y1 + 14.0)
          graphCtx?lineTo(x2, y2 + 14.0)
          graphCtx?stroke()
          graphCtx?fillText(string e.Count, (x1 + x2) / 2.0, (y1 + y2) / 2.0)
        | _ -> ()
      let nodeBox (entry: int) (title: string) (sub: string) (color: string) =
        let x, y = pos.[entry]
        graphCtx?fillStyle <- (if selectedEntries.Contains entry then hiBg else panel)
        graphCtx?strokeStyle <- color
        graphCtx?lineWidth <- 2.0
        graphCtx?fillRect(x, y, 104.0, 28.0)
        graphCtx?strokeRect(x, y, 104.0, 28.0)
        graphCtx?fillStyle <- normal
        graphCtx?font <- "11px " + mono
        graphCtx?textAlign <- "center"
        graphCtx?fillText(title, x + 52.0, y + 12.0)
        graphCtx?fillStyle <- dim
        graphCtx?font <- "9px " + mono
        graphCtx?fillText(sub, x + 52.0, y + 24.0)
        graphNodes.Add(entry, x, y)
        graphCtx?lineWidth <- 1.0
      nodeBox -1 "TOP" "" normal
      for r in minedRoutines do
        nodeBox r.Entry (sprintf "%04X" r.Entry) (sprintf "x%d" r.CallCount) (riskColor r.Risk)

  and private selectRoutine (entry: int) (jump: bool) =
    let wasSelected = selectedEntries.Contains entry
    selectedEntries <-
      if wasSelected then Set.remove entry selectedEntries
      else Set.add entry selectedEntries
    match minedRoutines |> List.tryFind (fun r -> r.Entry = entry) with
    | Some r ->
      activeRoutine <- Some r
      showRoutineDetail r
    | None -> ()
    refreshRoutines ()
    refreshTray ()
    drawGraph ()
    generateContract ()
    if (not wasSelected) && jump then
      match currentTrace () with
      | Some t when entry < t.FirstIndexAtPc.Length && t.FirstIndexAtPc.[entry] >= 0 ->
        cursor <- t.FirstIndexAtPc.[entry]
        refreshAll ()
        syncSlider ()
      | _ -> ()

  let private mineNow () =
    buildTraceNow ()
    match currentTrace () with
    | Some t when t.Entries.Length > 0 ->
      let routines, edges = Miner.mine t
      minedRoutines <- routines
      minedEdges <- edges
      selectedEntries <- Set.empty
      refreshRoutines ()
      refreshTray ()
      drawGraph ()
      statusText?textContent <- sprintf "mined %d routines, %d call edges (self-mod addresses: %d)"
        routines.Length edges.Length t.SelfModCount
    | _ -> statusText?textContent <- "no trace to mine - play the game first"

  // ---- actions -----------------------------------------------------------
  let private pauseGame () =
    running <- false
    cinemaPlaying <- false
    playBtn?textContent <- "Play"
    buildTraceNow ()
    clampCursor ()
    syncSlider ()
    refreshAll ()

  let private seek (delta: int) =
    cinemaPlaying <- false
    playBtn?textContent <- "Play"
    buildTraceNow ()
    clampCursor ()
    cursor <- max 0 (min (cursor + delta) (max 0 (currentEntryCount () - 1)))
    refreshAll ()
    syncSlider ()

  let private stepBack100 () = seek -100
  let private stepBack10 () = seek -10
  let private stepBack1 () = seek -1
  let private stepFwd1 () = seek 1
  let private stepFwd10 () = seek 10
  let private stepFwd100 () = seek 100

  let private toggleCinema () =
    buildTraceNow ()
    if cinemaPlaying then
      cinemaPlaying <- false
      playBtn?textContent <- "Play"
    else
      if currentEntryCount () > 0 then
        cinemaPlaying <- true
        playBtn?textContent <- "Pause"

  let private saveTrace () =
    try
      let t =
        match loaded with
        | Some t -> t
        | None ->
          buildTraceNow ()
          match built with
          | Some t -> t
          | None -> failwith "no trace recorded yet"
      if t.Entries.Length = 0 then failwith "trace is empty"
      let bytes = TraceCodecWeb.encode t
      let blob: obj = emitJsExpr bytes "new Blob([new Uint8Array($0)], { type: 'application/octet-stream' })"
      let url: obj = emitJsExpr blob "URL.createObjectURL($0)"
      let a = Dom.el "a"
      a?href <- url
      a?download <- "trace.jpt"
      Dom.append (Dom.document?body) a |> ignore
      emitJsExpr a "$0.click()" |> ignore
      emitJsExpr url "URL.revokeObjectURL($0)" |> ignore
      statusText?textContent <- sprintf "saved %d instructions to trace.jpt" t.Entries.Length
    with ex -> statusText?textContent <- "save failed: " + ex.Message

  let private loadTrace () =
    let input = Dom.el "input"
    (?<-) input "type" "file"
    input?accept <- ".jpt"
    input?addEventListener("change", fun _ ->
      let f: obj = emitJsExpr input "$0.files[0]"
      if not (isNull f) then
        let reader: obj = emitJsExpr () "new FileReader()"
        reader?onload <- (fun () ->
          let buf: obj = reader?result
          let bytes: byte[] = emitJsExpr buf "Array.from(new Uint8Array($0))"
          try
            let t = TraceCodecWeb.decode bytes
            loaded <- Some t
            cursor <- 0
            markerPc <- int t.Entries.[0].Pc
            syncSlider ()
            refreshAll ()
            let routines, edges = Miner.mine t
            minedRoutines <- routines
            minedEdges <- edges
            selectedEntries <- Set.empty
            refreshRoutines ()
            refreshTray ()
            drawGraph ()
            statusText?textContent <- sprintf "loaded %d instructions; mined %d routines"
              t.Entries.Length routines.Length
          with ex -> statusText?textContent <- "load failed: " + ex.Message)
        emitJsExpr (reader, f) "$0.readAsArrayBuffer($1)" |> ignore)
    emitJsExpr input "$0.click()" |> ignore

  let private startGame () =
    match session with
    | Some s ->
      chkRec?checked <- s.Recorder.RecordEnabled
      running <- true
      statusText?textContent <-
        sprintf "emulator ready (%s) - frame 0, recording %s, saved input %d events"
          (if s.WarmStart then "cached entry state" else "booted to game entry")
          (if s.Recorder.RecordEnabled then "ON" else "OFF") s.ReplayEventCount
    | None -> ()

  // ---- construction ------------------------------------------------------
  let start () =
    // assets: embedded base64, resolved by key (Validation/Boot/EntryCache use
    // the string-keyed API; the desktop resolves the same keys from disk).
    Jetpac3.Core.Boot.AssetProvider <- fun key ->
      match key with
      | "rom" -> Embedded.decode Embedded.RomB64
      | "tzx" -> Embedded.decode Embedded.TzxB64
      | _ -> failwithf "unknown asset key %s" key

    // Warm-start seed: on the first visit localStorage holds no entry cache, so
    // prime it from the desktop-captured snapshot (instant boot). Later visits
    // reuse localStorage. A changed game invalidates by marker mismatch.
    match EntryCache.tryLoad "rom" "tzx" with
    | Some _ -> ()
    | None ->
      let mem = Embedded.decode Embedded.WarmMemoryB64
      if mem.Length = 0x10000 then EntryCache.save "rom" "tzx" mem Embedded.WarmState

    // bind element refs
    statusText <- Dom.byId "status"
    disasmBox <- Dom.byId "disasmBox"
    cursorLabel <- Dom.byId "cursorLabel"
    slider <- Dom.byId "slider"
    pcCell <- Dom.byId "pcCell"
    routineList <- Dom.byId "routineList"
    routineCountLabel <- Dom.byId "routineCount"
    routineDetail <- Dom.byId "routineDetail"
    tray <- Dom.byId "tray"
    trayCountLabel <- Dom.byId "trayCount"
    contractText <- Dom.byId "contractText"
    contractLabel <- Dom.byId "contractLabel"
    theaterText <- Dom.byId "theaterText"
    pasteBox <- Dom.byId "pasteBox"
    playBtn <- Dom.byId "btnPlay"
    chkRec <- Dom.byId "chkRec"
    chkMute <- Dom.byId "chkMute"

    let gameCanvas = Dom.byId "canvasScreen"
    gameCtx <- gameCanvas?getContext("2d")
    gameImg <- emitJsExpr (320, 256) "new ImageData($0, $1)"

    let heatCanvas = Dom.byId "canvasHeat"
    heatCtx <- heatCanvas?getContext("2d")
    heatImg <- emitJsExpr (256, 256) "new ImageData($0, $1)"

    let stripCanvas = Dom.byId "canvasStrip"
    stripCtx <- stripCanvas?getContext("2d")
    stripImg <- emitJsExpr (512, 24) "new ImageData($0, $1)"

    graphCanvas <- Dom.byId "canvasGraph"
    graphCtx <- graphCanvas?getContext("2d")

    // registers board
    let regNames = [ "AF"; "BC"; "DE"; "HL"; "AF'"; "BC'"; "DE'"; "HL'"; "IX"; "IY"; "SP"; "I"; "R" ]
    let regsBox = Dom.byId "regsBox"
    regCells.Clear()
    for name in regNames do
      let row = Dom.el "div"
      row?className <- "reg-row"
      let nameSpan = Dom.el "span"
      nameSpan?textContent <- name
      nameSpan?className <- "reg-name"
      let valSpan = Dom.el "span"
      valSpan?textContent <- "----"
      valSpan?className <- "reg-val"
      Dom.append row nameSpan |> ignore
      Dom.append row valSpan |> ignore
      Dom.append regsBox row |> ignore
      regCells.[name] <- valSpan
    let pcRow = Dom.el "div"
    pcRow?className <- "reg-row"
    let pcName = Dom.el "span"
    pcName?textContent <- "PC"
    pcName?className <- "reg-name"
    let pcVal = Dom.el "span"
    pcVal?textContent <- "----"
    pcVal?className <- "reg-val"
    Dom.append pcRow pcName |> ignore
    Dom.append pcRow pcVal |> ignore
    Dom.append regsBox pcRow |> ignore
    regCells.["PC"] <- pcVal
    pcCell <- pcVal

    // flags board
    let flagsBox = Dom.byId "flagsBox"
    Dom.clear flagsBox
    let flagNames = [| "S"; "Z"; "5"; "H"; "3"; "P"; "N"; "C" |]
    for i in 0 .. 7 do
      let row = Dom.el "div"
      row?className <- "flag-row"
      let nameSpan = Dom.el "span"
      nameSpan?textContent <- flagNames.[i]
      nameSpan?className <- "reg-name"
      let valSpan = Dom.el "span"
      valSpan?textContent <- "0"
      valSpan?className <- "reg-val"
      Dom.append row nameSpan |> ignore
      Dom.append row valSpan |> ignore
      Dom.append flagsBox row |> ignore
      flagCells.[i] <- valSpan

    // timers (created once; Run/Pause and Play flip the flags)
    frameTimerId <- Dom.setInterval (fun () -> if running then renderFrame ()) 20
    cinemaTimerId <- Dom.setInterval
      (fun () ->
        if cinemaPlaying then
          let n = currentEntryCount ()
          if n > 0 then
            cursor <- min (n - 1) (cursor + cinemaSpeed)
            refreshDisasm ()
            refreshRegs ()
            refreshHeatmap ()
            refreshStrip ()
            refreshCursorLabel ()
            syncingSlider <- true
            slider?value <- cursor
            syncingSlider <- false
          else
            cinemaPlaying <- false) 30
    Dom.setInterval
      (fun () ->
        match session with
        | None -> ()
        | Some s ->
          if running then
            statusText?textContent <-
              sprintf "frame=%d  tick=%.2fM  instr=%d/%d  distinct-pc=%d  selfmod=%d  rec=%s"
                s.Frame
                (float s.CycleCount / 1_000_000.0)
                s.Recorder.EntryCount
                s.Recorder.Capacity
                (s.Recorder.PerPcCount |> Array.filter (fun c -> c > 0) |> Array.length)
                s.Recorder.SelfModCount
                (if s.Recorder.RecordEnabled then "ON" else "OFF")
          refreshHeatmap ()
          refreshStrip ()
          if running then
            syncingSlider <- true
            let n = s.Recorder.EntryCount
            if n > 1 then
              slider?max <- n - 1
              slider?disabled <- false
            syncingSlider <- false) 100
          |> ignore

    // toolbar wiring
    (Dom.byId "btnRun")?addEventListener("click", fun _ ->
      pauseGame ()
      running <- true)
    (Dom.byId "btnPause")?addEventListener("click", fun _ -> pauseGame ())
    (Dom.byId "btnStepFrame")?addEventListener("click", fun _ ->
      pauseGame ()
      renderFrame ())
    (Dom.byId "btnSaveTrace")?addEventListener("click", fun _ -> saveTrace ())
    (Dom.byId "btnLoadTrace")?addEventListener("click", fun _ -> loadTrace ())
    chkRec?addEventListener("change", fun _ ->
      match session with
      | Some s -> s.Recorder.RecordEnabled <- unbox<bool> (chkRec?checked)
      | None -> ())
    chkMute?addEventListener("change", fun _ ->
      Audio.SetEnabled (not (unbox<bool> (chkMute?checked))))

    // cinema controls
    (Dom.byId "btnB100")?addEventListener("click", fun _ -> stepBack100 ())
    (Dom.byId "btnB10")?addEventListener("click", fun _ -> stepBack10 ())
    (Dom.byId "btnB1")?addEventListener("click", fun _ -> stepBack1 ())
    (Dom.byId "btnF1")?addEventListener("click", fun _ -> stepFwd1 ())
    (Dom.byId "btnF10")?addEventListener("click", fun _ -> stepFwd10 ())
    (Dom.byId "btnF100")?addEventListener("click", fun _ -> stepFwd100 ())
    playBtn?addEventListener("click", fun _ -> toggleCinema ())
    let selSpeed = Dom.byId "selSpeed"
    selSpeed?addEventListener("change", fun _ ->
      cinemaSpeed <- Int32.Parse (string (selSpeed?value)))

    // slider
    slider?addEventListener("input", fun _ ->
      if not syncingSlider && currentEntryCount () > 0 then
        cursor <- Int32.Parse (string (slider?value))
        refreshDisasm ()
        refreshRegs ()
        refreshHeatmap ()
        refreshStrip ()
        refreshCursorLabel ())

    // heatmap click/hover
    heatCanvas?addEventListener("click", fun (e: obj) ->
      match currentTrace () with
      | Some t when t.Entries.Length > 0 ->
        let x = unbox<int> (e?offsetX)
        let y = unbox<int> (e?offsetY)
        if x >= 0 && x < 256 && y >= 0 && y < 256 then
          let pc = (y <<< 8) ||| x
          let idx = if pc < t.FirstIndexAtPc.Length then t.FirstIndexAtPc.[pc] else -1
          if idx >= 0 then
            cursor <- idx
            refreshAll ()
            syncSlider ()
            statusText?textContent <- sprintf "jumped to 0x%04X (first of %d executions)" pc (if pc < t.PerPcCount.Length then t.PerPcCount.[pc] else 0)
      | _ -> ())
    heatCanvas?addEventListener("mousemove", fun (e: obj) ->
      let x = unbox<int> (e?offsetX)
      let y = unbox<int> (e?offsetY)
      if x >= 0 && x < 256 && y >= 0 && y < 256 then
        let pc = (y <<< 8) ||| x
        let count =
          match currentTrace () with
          | Some t when pc < t.PerPcCount.Length -> t.PerPcCount.[pc]
          | _ -> 0
        heatCanvas?title <- sprintf "0x%04X  (%d executions)" pc count)

    // functions tab
    (Dom.byId "btnMine")?addEventListener("click", fun _ -> mineNow ())

    // call graph click/hover
    graphCanvas?addEventListener("click", fun (e: obj) ->
      let x = float (unbox<int> (e?offsetX))
      let y = float (unbox<int> (e?offsetY))
      let mutable hit = -1
      for (entry, nx, ny) in graphNodes do
        if hit < 0 && x >= nx && x <= nx + 104.0 && y >= ny && y <= ny + 28.0 then hit <- entry
      if hit >= 0 then selectRoutine hit true)
    graphCanvas?addEventListener("mousemove", fun (e: obj) ->
      let x = float (unbox<int> (e?offsetX))
      let y = float (unbox<int> (e?offsetY))
      let mutable hit = -1
      for (entry, nx, ny) in graphNodes do
        if hit < 0 && x >= nx && x <= nx + 104.0 && y >= ny && y <= ny + 28.0 then hit <- entry
      if hit >= 0 then graphCanvas?title <- sprintf "0x%04X" hit
      else graphCanvas?title <- "")

    // contract/prompt tab
    (Dom.byId "btnSaveMd")?addEventListener("click", fun _ ->
      if (string (contractText?value)).Length > 0 then
        let blob: obj = emitJsExpr (contractText?value) "new Blob([$0], { type: 'text/markdown' })"
        let url: obj = emitJsExpr blob "URL.createObjectURL($0)"
        let a = Dom.el "a"
        a?href <- url
        a?download <- "prompt.md"
        Dom.append (Dom.document?body) a |> ignore
        emitJsExpr a "$0.click()" |> ignore
        emitJsExpr url "URL.revokeObjectURL($0)" |> ignore
        statusText?textContent <- "prompt saved to prompt.md"
      else statusText?textContent <- "select a routine first")
    (Dom.byId "btnCopy")?addEventListener("click", fun _ ->
      if (string (contractText?value)).Length > 0 then
        emitJsExpr (contractText?value) "navigator.clipboard.writeText($0)" |> ignore
        statusText?textContent <- "prompt copied to clipboard"
      else statusText?textContent <- "select a routine first")

    // theater tab
    (Dom.byId "btnStage")?addEventListener("click", fun _ ->
      match activeRoutine with
      | Some r ->
        if (string (pasteBox?value)).Trim().Length > 0 then
          let header =
            sprintf "// Staged lift for 0x%04X (span 0x%04X-0x%04X)\n// Add this file to Jetpac3.Core in Visual Studio, register the routine in\n// LiftedRoutines.registry, rebuild, then validate from the Theater tab.\n\n"
              r.Entry r.SpanLo r.SpanHi
          let content: string = header + (string (pasteBox?value))
          let blob: obj = emitJsExpr content "new Blob([$0], { type: 'text/plain' })"
          let url: obj = emitJsExpr blob "URL.createObjectURL($0)"
          let a = Dom.el "a"
          a?href <- url
          a?download <- sprintf "fn%04X.fs" r.Entry
          Dom.append (Dom.document?body) a |> ignore
          emitJsExpr a "$0.click()" |> ignore
          emitJsExpr url "URL.revokeObjectURL($0)" |> ignore
          statusText?textContent <- sprintf "staged fn%04X.fs downloaded" r.Entry
        else statusText?textContent <- "paste the model's F# first"
      | None -> statusText?textContent <- "select a routine first")
    (Dom.byId "btnValidate")?addEventListener("click", fun _ ->
      match activeRoutine, session with
      | Some r, Some _ ->
        let registered =
          match Jetpac3.Core.LiftedRoutines.registryHook r.Entry with
          | Some _ -> true
          | None -> false
        if not registered then
          theaterText?textContent <-
            sprintf "0x%04X is not covered by the lifted registry yet.\n\nStage the pasted F# (below), add it to Jetpac3.Core in Visual Studio, rebuild, then validate again." r.Entry
        else
          let framesN = 300
          let script = Jetpac3.Core.Script.defaultSession framesN
          theaterText?textContent <- sprintf "validating 0x%04X over %d frames..." r.Entry framesN
          // Synchronous: the browser tab blocks for the run (the desktop runs
          // it on a worker thread).
          try
            let rep =
              Validation.run "rom" "tzx" framesN script
                Jetpac3.Core.LiftedRoutines.registryHook
                System.Threading.CancellationToken.None
            if rep.Passed then
              theaterText?textContent <-
                sprintf "VALIDATION PASSED: %d frames, 0 diffs (lifted 0x%04X == oracle)" rep.Frames r.Entry
            else
              match rep.FirstDivergence with
              | Some d ->
                theaterText?textContent <-
                  sprintf "VALIDATION FAILED at frame %d (%s)\n\nport   executed %04X: %s\noracle executed %04X: %s\n\nport   regs: %s\noracle regs: %s"
                    d.Frame d.Kind d.PortPc d.PortExecuted d.OraclePc d.OracleExecuted d.PortRegs d.OracleRegs
              | None -> theaterText?textContent <- "VALIDATION FAILED: no divergence details"
          with ex -> theaterText?textContent <- "validation crashed: " + ex.Message
      | None, _ -> theaterText?textContent <- "select a mined routine to validate"
      | _ -> theaterText?textContent <- "emulator not ready")

    // clear tray
    (Dom.byId "btnClearTray")?addEventListener("click", fun _ ->
      selectedEntries <- Set.empty
      refreshRoutines ()
      refreshTray ()
      drawGraph ())

    // keyboard -> ZX matrix
    Dom.window?addEventListener("keydown", fun (e: obj) ->
      let key: string = unbox (e?key)
      setKeyFor key true
      if key = " " || key.StartsWith "Arrow" then e?preventDefault())
    Dom.window?addEventListener("keyup", fun (e: obj) ->
      setKeyFor (string (e?key)) false)

    let storageKey = "jetpacfr.control.minimal"
    let mutable control: ControlFile option = None
    let saveControlLocal () =
      match control with
      | Some c ->
        let local: obj = Dom.window?localStorage
        emitJsExpr (local, storageKey, ControlFile.toJson c) "$0.setItem($1, $2)" |> ignore
        control <- Some { c with Dirty = false }
      | None -> ()

    // ---- timeline selector + control map (desktop GUI parity) --------------
    // Canvas ports of TimelineSelector + ControlMap. The web session keeps
    // no frame-snapshot history, so "preview" sweeps the trace cursor over
    // the selection instead of replaying screen frames.

    let tlCanvas = Dom.byId "canvasTimeline"
    let tlCtx: obj = tlCanvas?getContext("2d")
    let mutable tlLen = 300
    let mutable rangeA = (0, 150)
    let mutable rangeB = (150, 300)
    let mutable brushA = true
    let mutable tlDragAnchor: int option = None
    let mutable tlChipDrag: bool option = None // Some true = A's chip
    let mutable tlStartX = 0.0
    let mutable tlMoved = false
    let mutable chipRectA = (0.0, 0.0, 0.0, 0.0)
    let mutable chipRectB = (0.0, 0.0, 0.0, 0.0)
    let brushAColor = "#38BDF8"
    let brushBColor = "#FBBF24"

    let tlFrameCount () =
      match currentTrace () with
      | Some t when t.FrameTicks.Length > 0 -> t.FrameTicks.Length
      | _ -> 300

    /// First trace entry at or after frame `f` (FrameTicks binary search).
    let entryAtFrame (f: int) : int =
      match currentTrace () with
      | Some t when t.Entries.Length > 0 && t.FrameTicks.Length > 0 ->
        let last = t.FrameTicks.[t.FrameTicks.Length - 1]
        let tick =
          if f <= 0 then 0u
          elif f >= t.FrameTicks.Length then last
          else t.FrameTicks.[f]
        let mutable lo = 0
        let mutable hi = t.Entries.Length - 1
        let mutable res = t.Entries.Length - 1
        while lo <= hi do
          let mid = (lo + hi) / 2
          if t.Entries.[mid].Tick >= tick then (res <- mid; hi <- mid - 1)
          else lo <- mid + 1
        res
      | _ -> 0

    let tlNiceStep (length: int) : int =
      let nice = [| 1.0; 2.0; 2.5; 5.0 |]
      let rec niceCeil (v: float) base10 =
        let m = v / base10
        match nice |> Array.tryFind (fun n -> n >= m - 1e-9) with
        | Some n -> n * base10
        | None ->
          if base10 >= 1e9 then v
          else niceCeil v (base10 * 10.0)
      let l = float length
      let mutable step = niceCeil (l / 4.0) 1.0
      while System.Math.Floor(l / step) + 1.0 > 4.0 do step <- niceCeil (step * 1.01) 1.0
      while System.Math.Floor(l / step) + 1.0 < 3.0 && step > 1.0 do step <- niceCeil (step / 2.02) 1.0
      max 1 (int (System.Math.Ceiling step))

    let tlRender () =
      let w: float = unbox tlCanvas?width
      let h: float = unbox tlCanvas?height
      tlCtx?fillStyle <- "#101016"
      tlCtx?fillRect(0.0, 0.0, w, h)
      tlCtx?strokeStyle <- "#3A3F4C"
      tlCtx?strokeRect(0.5, 0.5, w - 1.0, h - 1.0)
      let labelZone = 13.0
      let top = 2.0
      let bottom = h - labelZone - 2.0
      let len = max 1 tlLen
      let drawBrush (which: bool) ((rs, re): int * int) =
        let color = if which then brushAColor else brushBColor
        let x0 = min w (max 0.0 (float rs / float len * w))
        let x1 = min w (max 0.0 (float re / float len * w))
        // tinted range: hex + 26 alpha
        tlCtx?fillStyle <- (if which then "rgba(56,189,248,0.15)" else "rgba(251,191,36,0.15)")
        tlCtx?fillRect(min x0 x1, top, max 2.0 (abs (x1 - x0)), bottom - top)
        tlCtx?strokeStyle <- color
        tlCtx?lineWidth <- 2.0
        tlCtx?beginPath()
        tlCtx?moveTo(x0, top)
        tlCtx?lineTo(x0, bottom)
        tlCtx?moveTo(x1, top)
        tlCtx?lineTo(x1, bottom)
        tlCtx?stroke()
        // label chip: A bottom-left, B bottom-right
        let label = if which then "A" else "B"
        let chipW = 14.0
        let chipH = 12.0
        let chipX = if which then min x0 (max 0.0 (w - chipW)) else max 0.0 (min (x1 - chipW) (w - chipW))
        let chipY = bottom - chipH
        tlCtx?fillStyle <- color
        tlCtx?fillRect(chipX, chipY, chipW, chipH)
        tlCtx?fillStyle <- "#020617"
        tlCtx?font <- "bold 9px Consolas"
        tlCtx?fillText(label, chipX + 4.0, chipY + 9.5)
        if which then chipRectA <- (chipX, chipY, chipW, chipH) else chipRectB <- (chipX, chipY, chipW, chipH)
      drawBrush true rangeA
      drawBrush false rangeB
      // ruler
      let step = tlNiceStep len
      tlCtx?strokeStyle <- "#8A8A92"
      tlCtx?lineWidth <- 1.0
      tlCtx?fillStyle <- "#8A8A92"
      tlCtx?font <- "8px Consolas"
      let mutable t = 0
      while t <= len do
        let x = float t / float len * w
        tlCtx?beginPath()
        tlCtx?moveTo(x, bottom - 3.0)
        tlCtx?lineTo(x, bottom + 1.0)
        tlCtx?stroke()
        tlCtx?fillText(string t, min (max 2.0 (x - 8.0)) (w - 20.0), h - 4.0)
        t <- t + step

    let tlSetRange (which: bool) (r: int * int) =
      if which then rangeA <- r else rangeB <- r
      tlRender ()

    let tlUnitAt (x: float) =
      let w: float = unbox tlCanvas?width
      let clamped = min (max x 0.0) w
      int (System.Math.Round(clamped / w * float (max 1 tlLen)))

    let tlX (e: obj) =
      let rect: obj = tlCanvas?getBoundingClientRect()
      (unbox<float> e?clientX - unbox<float> rect?left) * (unbox<float> tlCanvas?width / unbox<float> rect?width)

    let inChip (r: float * float * float * float) (x: float) (y: float) =
      let (rx, ry, rw, rh) = r
      x >= rx - 3.0 && x <= rx + rw + 3.0 && y >= ry - 3.0 && y <= ry + rh + 3.0

    let tlY (e: obj) =
      let rect: obj = tlCanvas?getBoundingClientRect()
      unbox<float> e?clientY - unbox<float> rect?top

    tlCanvas?addEventListener("mousedown", fun e ->
      let x = tlX e
      let y = tlY e
      tlStartX <- x
      tlMoved <- false
      if inChip chipRectA x y then tlChipDrag <- Some true
      elif inChip chipRectB x y then tlChipDrag <- Some false
      else
        tlChipDrag <- None
        tlDragAnchor <- Some (tlUnitAt x)
        // scrub immediately to the pressed frame
        cursor <- entryAtFrame (tlUnitAt x)
        refreshAll ()
        syncSlider ())
    tlCanvas?addEventListener("mousemove", fun e ->
      let x = tlX e
      match tlChipDrag, tlDragAnchor with
      | Some which, _ ->
        if abs (x - tlStartX) > 3.0 then
          let u = tlUnitAt x
          match which with
          | true ->
            let (rs, re) = rangeA
            tlSetRange true (min u re, re)
          | false ->
            let (rs, re) = rangeB
            tlSetRange false (rs, max rs u)
      | None, Some anchor ->
        if abs (x - tlStartX) > 3.0 then
          tlMoved <- true
          let u = tlUnitAt x
          tlSetRange brushA (min anchor u, max anchor u)
        // scrub on every move, both directions
        cursor <- entryAtFrame (tlUnitAt x)
        refreshAll ()
        syncSlider ()
      | _ -> ())
    Dom.window?addEventListener("mouseup", fun _ ->
      match tlDragAnchor with
      | Some anchor when not tlMoved ->
        // clean click: point range
        tlSetRange brushA (anchor, anchor)
      | _ -> ()
      tlDragAnchor <- None
      tlChipDrag <- None)

    let brushStyle () =
      let a = Dom.byId "btnBrushA"
      let b = Dom.byId "btnBrushB"
      a?className <- (if brushA then "brush-btn active-brush-a" else "brush-btn")
      b?className <- (if brushA then "brush-btn" else "brush-btn active-brush-b")
    (Dom.byId "btnBrushA")?addEventListener("click", fun _ -> brushA <- true; brushStyle ())
    (Dom.byId "btnBrushB")?addEventListener("click", fun _ -> brushA <- false; brushStyle ())
    Dom.window?addEventListener("keydown", fun e ->
      let k = string e?key
      if k = "a" || k = "A" then (brushA <- true; brushStyle ())
      elif k = "b" || k = "B" then (brushA <- false; brushStyle ()))

    // Preview = sweep the trace cursor over the selection in exactly 1s
    // (50 ticks x 20 ms). The web session has no frame-snapshot history,
    // so this animates the instruction cursor, not the screen.
    let mutable previewFrom = 0
    let mutable previewSpan = 0
    let mutable previewTicks = 0
    let mutable previewActive = false
    let rangeOf (which: bool) = if which then rangeA else rangeB
    let previewRange (which: bool) =
      let fStart, fEnd = rangeOf which
      let label = if which then "A" else "B"
      let n = tlFrameCount ()
      if fEnd - 1 < n && fEnd - 1 >= 0 then
        pauseGame ()
        previewFrom <- max 0 fStart
        previewSpan <- max 0 (fEnd - 1 - previewFrom)
        previewTicks <- 0
        previewActive <- true
        cursor <- entryAtFrame previewFrom
        refreshAll ()
        syncSlider ()
        statusText?textContent <- sprintf "preview %s: frames %d..%d over 1s (cursor sweep)" label fStart fEnd
      else
        statusText?textContent <- sprintf "preview %s: range outside the recording (%d frames)" label n
    (Dom.byId "btnPreviewA")?addEventListener("click", fun _ -> previewRange true)
    (Dom.byId "btnPreviewB")?addEventListener("click", fun _ -> previewRange false)
    Dom.setInterval
      (fun () ->
        if previewActive then
          previewTicks <- previewTicks + 1
          let frac = min previewTicks 50
          cursor <- entryAtFrame (previewFrom + previewSpan * frac / 50)
          refreshAll ()
          syncSlider ()
          if previewTicks >= 50 then previewActive <- false)
      20
    |> ignore

    // ---- control map --------------------------------------------------------
    let cmCanvas = Dom.byId "canvasCtrlMap"
    let cmCtx: obj = cmCanvas?getContext("2d")
    let mutable cmViewStart = 0
    let mutable cmViewEnd = 0x10000
    let mutable instrStartsCache: bool[] option = None
    let mutable instrStartsAt = 0.0
    let mutable cmCursorAddr = -1
    let mutable cmDragAddr: int option = None
    let mutable cmDisasmLo = -1
    let mutable cmDisasmHi = -1
    let cmMinSpan () = max 32 (int (unbox<float> cmCanvas?height / 32.0))
    let cmClamp () =
      let span = max (cmMinSpan ()) (cmViewEnd - cmViewStart)
      cmViewStart <- max 0 (min (0x10000 - span) cmViewStart)
      cmViewEnd <- cmViewStart + span

    let cmInstrStarts () : bool[] option =
      match session with
      | None -> None
      | Some s ->
        let now: float = emitJsExpr () "Date.now()"
        if instrStartsCache.IsNone || now - instrStartsAt > 1000.0 then
          let st = Array.create 0x10000 false
          let mutable a = 0
          while a < 0x10000 do
            st.[a] <- true
            let len = (Disasm.disasmMemory s.Memory a).Length
            a <- a + (if len < 1 then 1 else min 8 len)
          instrStartsCache <- Some st
          instrStartsAt <- now
        instrStartsCache

    let cmCounts () =
      match loaded with
      | Some t -> t.PerPcCount
      | None ->
        match session with
        | Some s -> s.Recorder.PerPcCount
        | None -> Array.zeroCreate<int> 0x10000
    let cmSelfMod () =
      match loaded with
      | Some t -> t.SelfModified
      | None ->
        match session with
        | Some s -> s.Recorder.SelfModified
        | None -> Array.zeroCreate<bool> 0x10000

    let cmAddrY (addr: int) =
      float (unbox<float> cmCanvas?height) * float (addr - cmViewStart) / float (max 1 (cmViewEnd - cmViewStart))

    let cmRender () =
      let w: float = unbox cmCanvas?width
      let h: float = unbox cmCanvas?height
      // keep the backing store in step with the CSS box
      let cw: float = unbox cmCanvas?clientWidth
      let ch: float = unbox cmCanvas?clientHeight
      if cw > 0.0 && int cw <> int w then cmCanvas?width <- int cw
      if ch > 0.0 && int ch <> int h then cmCanvas?height <- int ch
      let w: float = unbox cmCanvas?width
      let h: float = unbox cmCanvas?height
      cmCtx?fillStyle <- "#0A0A10"
      cmCtx?fillRect(0.0, 0.0, w, h)
      let blocks = match control with Some c -> c.Blocks | None -> []
      let comments = match control with Some c -> c.Comments | None -> []
      let rowCount = max 1 (int (Math.Ceiling h))
      let m =
        JetpacFR.Core.CtrlMapModel.render blocks comments (cmInstrStarts ()) (session |> Option.map (fun s -> s.Memory))
          (cmCounts ()) (cmSelfMod ()) cmViewStart cmViewEnd rowCount
      let baseColor =
        function
        | JetpacFR.Core.CtrlMapModel.CodeR -> (0x2E, 0x7D, 0xD1)
        | JetpacFR.Core.CtrlMapModel.DataR -> (0xC8, 0x8A, 0x2E)
        | JetpacFR.Core.CtrlMapModel.GapR -> (0x3A, 0x3F, 0x4C)
        | JetpacFR.Core.CtrlMapModel.MixedR -> (0x55, 0x51, 0x6B)
        | JetpacFR.Core.CtrlMapModel.UnmappedR -> (0x0A, 0x0A, 0x10)
      let heatLutI =
        [| (0x08, 0x08, 0x0C); (0x0A, 0x1E, 0x4A); (0x0A, 0x3A, 0x5E); (0x0A, 0x5C, 0x6A);
           (0x12, 0x86, 0x5E); (0x3A, 0xA8, 0x3C); (0x96, 0xB8, 0x2C); (0xD8, 0xA0, 0x22);
           (0xE8, 0x66, 0x18); (0xF0, 0x38, 0x28) |]
      let rowColor kind heat =
        let (br, bgc, bb) = baseColor kind
        if heat <= 0 then sprintf "rgb(%d,%d,%d)" br bgc bb
        else
          let (hr, hg, hb) = heatLutI.[min 9 heat]
          let t = 0.15 + 0.65 * float heat / 9.0
          let mix x y = int (Math.Round(float x + (float y - float x) * t))
          sprintf "rgb(%d,%d,%d)" (mix br hr) (mix bgc hg) (mix bb hb)
      if m.Rows.Length > 0 then
        let rowH = h / float m.Rows.Length
        let mutable y = 0
        while y < m.Rows.Length do
          let r = m.Rows.[y]
          let col = rowColor r.Kind r.Heat
          let mutable y2 = y + 1
          while y2 < m.Rows.Length && m.Rows.[y2].Kind = r.Kind && m.Rows.[y2].Heat = r.Heat do
            y2 <- y2 + 1
          cmCtx?fillStyle <- col
          cmCtx?fillRect(0.0, float y * rowH, w, float (y2 - y) * rowH + 0.5)
          for yy in y .. y2 - 1 do
            let ry = float yy * rowH
            if m.Rows.[yy].SelfMod then
              cmCtx?fillStyle <- "#F030F0"
              cmCtx?fillRect(0.0, ry, 2.0, 1.0)
            if m.Rows.[yy].Comment then
              cmCtx?fillStyle <- "#4EE060"
              cmCtx?fillRect(2.0, ry, 2.0, 1.0)
          y <- y2
        for (a, b) in m.Ranges do
          let y0 = JetpacFR.Core.CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length a
          let y1 = JetpacFR.Core.CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length b
          cmCtx?fillStyle <- "rgba(78,224,96,0.19)"
          cmCtx?fillRect(0.0, y0 * rowH, w, (y1 - y0) * rowH)
        cmCtx?fillStyle <- "#E8E8EC"
        cmCtx?font <- "9px Consolas"
        for lbl in m.Labels do
          let y = JetpacFR.Core.CtrlMapModel.addrY m.ViewStart m.ViewEnd m.Rows.Length lbl.Addr
          cmCtx?fillText(lbl.Text, 5.0, y * rowH + 9.0)
      // disassembly viewport thumb
      if cmDisasmLo >= 0 && cmDisasmHi > cmDisasmLo then
        cmCtx?strokeStyle <- "rgba(255,255,255,0.5)"
        cmCtx?lineWidth <- 1.0
        cmCtx?strokeRect(1.0, cmAddrY cmDisasmLo, w - 2.0, max 3.0 (cmAddrY cmDisasmHi - cmAddrY cmDisasmLo))
      // cursor hairline
      if cmCursorAddr >= 0 && cmCursorAddr >= cmViewStart && cmCursorAddr < cmViewEnd then
        cmCtx?strokeStyle <- "#4ED0E0"
        cmCtx?beginPath()
        cmCtx?moveTo(0.0, cmAddrY cmCursorAddr)
        cmCtx?lineTo(w, cmAddrY cmCursorAddr)
        cmCtx?stroke()
      // view readout
      cmCtx?fillStyle <- "#6A6A74"
      cmCtx?font <- "8px Consolas"
      cmCtx?fillText(sprintf "%04X-%04X" cmViewStart cmViewEnd, 2.0, 8.0)

    let cmCenterOn (addr: int) =
      let span = cmViewEnd - cmViewStart
      cmViewStart <- addr - span / 2
      cmClamp ()
      cmRender ()

    let cmXy (e: obj) =
      let rect: obj = cmCanvas?getBoundingClientRect()
      (unbox<float> e?clientX - unbox<float> rect?left, unbox<float> e?clientY - unbox<float> rect?top)

    let cmAddrAt (y: float) =
      let h: float = unbox cmCanvas?height
      cmViewStart
      + int (min (y / h * float (cmViewEnd - cmViewStart)) (float (cmViewEnd - cmViewStart - 1)))

    let cmNavigate (addr: int) =
      match currentTrace () with
      | Some t when t.Entries.Length > 0 && addr < t.FirstIndexAtPc.Length && t.FirstIndexAtPc.[addr] >= 0 ->
        cursor <- t.FirstIndexAtPc.[addr]
        refreshAll ()
        syncSlider ()
      | _ -> ()

    cmCanvas?addEventListener("mousedown", fun e ->
      let (x, y) = cmXy e
      let addr = cmAddrAt y
      cmDragAddr <- Some addr
      cmNavigate addr)
    cmCanvas?addEventListener("mousemove", fun e ->
      let (x, y) = cmXy e
      match cmDragAddr with
      | Some _ ->
        let addr = cmAddrAt y
        cmDragAddr <- Some addr
        cmNavigate addr
      | None ->
        let addr = cmAddrAt y
        let blockTxt =
          match control with
          | Some c ->
            ControlFile.blockAt c addr
            |> Option.map (fun b -> sprintf " %s (%A)" b.Name b.Kind)
            |> Option.defaultValue " <unmapped>"
          | None -> ""
        let counts = cmCounts ()
        let n = if addr < counts.Length then counts.[addr] else 0
        cmCanvas?title <- sprintf "0x%04X%s (%d executions)" addr blockTxt n)
    Dom.window?addEventListener("mouseup", fun _ -> cmDragAddr <- None)
    cmCanvas?addEventListener("wheel", fun e ->
      e?preventDefault()
      let delta: float = unbox e?deltaY
      let (_, y) = cmXy e
      let h: float = unbox cmCanvas?height
      let frac = min 1.0 (max 0.0 (y / max 1.0 h))
      let oldSpan = cmViewEnd - cmViewStart
      let anchor = cmViewStart + int (frac * float oldSpan)
      let factor = Math.Pow(1.15, -delta / 100.0)
      let newSpan = max (cmMinSpan ()) (min 0x10000 (int (float oldSpan / factor)))
      cmViewStart <- anchor - int (frac * float newSpan)
      cmViewEnd <- cmViewStart + newSpan
      cmClamp ()
      cmRender ())
    cmCanvas?addEventListener("contextmenu", fun e ->
      e?preventDefault()
      let (_, y) = cmXy e
      let addr = cmAddrAt y
      match control with
      | None -> statusText?textContent <- "no control file - click New first"
      | Some _ ->
        let menu = Dom.byId "ctrlMenu"
        Dom.clear menu
        let addItem (txt: string) (act: ControlFile -> ControlFile) =
          let item = Dom.el "div"
          item?textContent <- txt
          item?addEventListener("click", fun _ ->
            match control with
            | Some c ->
              control <- Some (act c)
              saveControlLocal ()
              refreshDisasm ()
              cmRender ()
              statusText?textContent <- sprintf "%s @ %04X" txt addr
            | None -> ()
            menu?style?display <- "none")
          Dom.append menu item |> ignore
        addItem "kind: code" (fun c -> ControlFile.setKindAt c addr Code)
        addItem "kind: data" (fun c -> ControlFile.setKindAt c addr Data)
        addItem "kind: gap" (fun c -> ControlFile.setKindAt c addr Gap)
        Dom.append menu (Dom.el "hr") |> ignore
        let split = Dom.el "div"
        split?textContent <- "split block here"
        split?addEventListener("click", fun _ ->
          match control with
          | Some c ->
            let canSplit =
              match instrStartsCache with
              | Some s -> addr < s.Length && s.[addr]
              | None -> true
            if canSplit then
              control <- Some (ControlFile.splitBlockAt c addr)
              saveControlLocal ()
              cmRender ()
              statusText?textContent <- sprintf "block split at %04X" addr
            else statusText?textContent <- sprintf "%04X is not an instruction start" addr
          | None -> ()
          menu?style?display <- "none")
        Dom.append menu split |> ignore
        addItem "merge into next block" (fun c -> ControlFile.mergeWithNext c addr)
        addItem "rename block..." (fun c ->
          let name: string = unbox (Dom.window?prompt("block name", ""))
          if isNull (box name) || name.Trim().Length = 0 then c
          else ControlFile.renameBlockAt c addr (name.Trim()))
        addItem "line comment..." (fun c ->
          let txt: string = unbox (Dom.window?prompt(sprintf "comment at %04X" addr, ""))
          if isNull (box txt) then c
          else ControlFile.upsert c { Kind = Line; Addr = addr; EndExcl = 0; InstrIndex = -1; Text = txt })
        addItem "range over block..." (fun c ->
          let txt: string = unbox (Dom.window?prompt("comment text for the whole block", ""))
          if isNull (box txt) then c
          else
            match ControlFile.blockAt c addr with
            | Some b -> ControlFile.upsert c { Kind = Range; Addr = b.Start; EndExcl = b.EndExcl; InstrIndex = -1; Text = txt }
            | None -> c)
        menu?style?left <- sprintf "%dpx" (int (unbox<float> e?clientX))
        menu?style?top <- sprintf "%dpx" (int (unbox<float> e?clientY))
        menu?style?display <- "block")
    Dom.window?addEventListener("mousedown", fun e ->
      let menu = Dom.byId "ctrlMenu"
      if menu?style?display = "block" && not (isNull e?target) && not (obj.ReferenceEquals(e?target, menu)) then
        let inside: bool = emitJsExpr (e?target, menu) "$0.closest && $0.closest('#ctrlMenu') != null"
        if not inside then menu?style?display <- "none")

    // boot (blocking; cold only if the embedded seed is unusable)
    statusText?textContent <- "booting emulator to game entry..."
    Dom.setTimeout
      (fun () ->
        try
          session <-
            Some
              (TraceSession
                (Embedded.decode Embedded.RomB64, Embedded.decode Embedded.TzxB64, capacity))
          startGame ()
          refreshAll ()
          syncSlider ()
        with ex ->
          statusText?textContent <- "boot failed: " + ex.Message) 30

    // keep the timeline extent in step with the recording
    Dom.setInterval
      (fun () ->
        let n = tlFrameCount ()
        if n <> tlLen then
          tlLen <- n
          tlRender ()) 100
    |> ignore

    // control file persistence: localStorage is the primary store (the
    // site is statically hosted, so there is no server to write to);
    // download/import round-trip the desktop's games/<id>/control.json.
    (Dom.byId "btnCtrlNew")?addEventListener("click", fun _ ->
      control <- Some (ControlFile.empty 0x4000 0x10000)
      cmRender ()
      statusText?textContent <- "empty control file created over $4000-$FFFF")
    (Dom.byId "btnCtrlSave")?addEventListener("click", fun _ ->
      match control with
      | Some c when c.Dirty ->
        saveControlLocal ()
        statusText?textContent <- "control saved to browser storage"
      | Some _ -> statusText?textContent <- "control unchanged"
      | None -> statusText?textContent <- "no control file - click New first")
    (Dom.byId "btnCtrlDl")?addEventListener("click", fun _ ->
      match control with
      | Some c ->
        let json = ControlFile.toJson c
        let blob: obj = emitJsExpr json "new Blob([$0], { type: 'application/json' })"
        let url: string = emitJsExpr blob "URL.createObjectURL($0)"
        let a = Dom.el "a"
        a?href <- url
        a?download <- "control.json"
        emitJsExpr a "$0.click()" |> ignore
        statusText?textContent <- "control.json downloaded"
      | None -> statusText?textContent <- "no control file - click New first")
    (Dom.byId "btnCtrlImport")?addEventListener("click", fun _ ->
      emitJsExpr () "document.getElementById('ctrlFileInput').click()" |> ignore)
    (Dom.byId "ctrlFileInput")?addEventListener("change", fun e ->
      let file: obj = emitJsExpr e "$0.target.files[0]"
      if not (isNull file) then
        let reader: obj = emitJsExpr () "new FileReader()"
        reader?onload <- fun _ ->
          let text: string = unbox reader?result
          try
            control <- Some (ControlFile.fromJson text)
            cmRender ()
            statusText?textContent <- "control file imported"
          with ex -> statusText?textContent <- "import failed: " + ex.Message
        emitJsExpr (reader, file) "$0.readAsText($1)" |> ignore)
    // initial load: browser storage first, then a served control.json
    let local: obj = Dom.window?localStorage
    let applyFetched (txt: obj) =
      if not (isNull txt) && control.IsNone then
        control <- Some (ControlFile.fromJson (string txt))
        cmRender ()
        statusText?textContent <- "control file loaded from games/minimal/control.json"
    match emitJsExpr (local, storageKey) "$0.getItem($1)" with
    | null ->
      let p: obj = emitJsExpr () "fetch('games/minimal/control.json').then(function (r) { return r.ok ? r.text() : null }).catch(function () { return null })"
      emitJsExpr (p, applyFetched) "$0.then($1)" |> ignore
    | stored ->
      control <- Some (ControlFile.fromJson (string stored))
      cmRender ()
