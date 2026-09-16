module GameChangerElmish.Host

// Imperative emulator host for the Game Changer pages: registers every
// compiled game in the GameRegistry, drives the active one's CE program on
// a 20 ms interval, and paints/auds it exactly like JetpacFR.Web's CEHost.
// The Elmish layer only forwards input state here (applyKeys/setRunning/...);
// no per-frame dispatches cross the MVU boundary.

open System
open Fable.Core
open Fable.Core.JsInterop
open JetpacFR.Core

module Dom =

    let window: obj = emitJsExpr () "window"
    let document: obj = emitJsExpr () "document"

    let byId (id: string) : obj =
        emitJsExpr id "document.getElementById($0)"

    let setTimeout (f: unit -> unit) (ms: int) : obj =
        emitJsExpr (f, ms) "setTimeout($0, $1)"

    let setInterval (f: unit -> unit) (ms: int) : obj =
        emitJsExpr (f, ms) "setInterval($0, $1)"

    let clearInterval (id: obj) : unit =
        emitJsExpr id "clearInterval($0)"

/// Beeper playback via Web Audio: 44100 Hz mono ring buffer pulled by a
/// ScriptProcessorNode (the JetpacFR.Web App.fs shape). Muted by default;
/// unmuting from a click resumes the AudioContext (browser gesture rule).
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
                emitJsExpr
                    ()
                    "((window.AudioContext || window.webkitAudioContext) ? new (window.AudioContext || window.webkitAudioContext)() : null)"

            if not (isNull ctx) then
                node <- emitJsExpr ctx "$0.createScriptProcessor(4096, 0, 1)"

                node?onaudioprocess <-
                    fun (e: obj) ->
                        let out: float32[] = unbox (e?outputBuffer?getChannelData (0))
                        let n: int = out.Length

                        for i in 0 .. n - 1 do
                            if rpos <> wpos then
                                out[i] <- ring[rpos]
                                rpos <- (rpos + 1) % ring.Length
                            else
                                out[i] <- 0.0f

                emitJsExpr (node, ctx) "$0.connect($1.destination)" |> ignore

    let Play (samples: float32[]) =
        if Enabled then
            ensure ()

            if not (isNull ctx) then
                for s in samples do
                    ring[wpos] <- s * 0.2f
                    wpos <- (wpos + 1) % ring.Length

    let SetEnabled (on: bool) =
        Enabled <- on

        if on then
            ensure ()

            if not (isNull ctx) then
                emitJsExpr ctx "$0.resume()" |> ignore
        // flush any stale buffered samples
        rpos <- wpos

/// The on-screen controls speak in directions and fire; the KeyMap translates
/// them into ZX Spectrum matrix cells. Uridium's snapshot polls the classic
/// cursor-joystick layout: Caps Shift + 5/6/7/8 for directions, 0 for fire.
/// Every touch control maps through here, so changing the scheme is one
/// function. (QAOP+Space alternative, kept at hand:)
///   N -> (2,0) S -> (1,0) W -> (2,2) E -> (2,3) fire -> (7,0)
module KeyMap =

    type Dir =
        | N
        | NE
        | E
        | SE
        | S
        | SW
        | W
        | NW

    let all: Dir list = [ N; NE; E; SE; S; SW; W; NW ]

    /// Matrix cells (row, bit) a direction holds down. Diagonals resolve to
    /// their vertical/horizontal component.
    let dirCells (d: Dir) : (int * int) list =
        match d with
        | N
        | NE
        | NW -> [ (0, 0); (4, 3) ] // Caps Shift + 7
        | S
        | SE
        | SW -> [ (0, 0); (4, 4) ] // Caps Shift + 6
        | W
        | NW
        | SW -> [ (0, 0); (3, 4) ] // Caps Shift + 5
        | E
        | NE
        | SE -> [ (0, 0); (4, 2) ] // Caps Shift + 8

    let fireCells: (int * int) list = [ (4, 0) ] // 0

/// All compiled games the shell can run. Adding a game: generate its
/// WebImage.fs (--null-ce <dir> --capture-tape/--web-image), include it in
/// the fsproj, and add one GameDef here.
module Games =

    type GameDef =
        { Id: string
          Name: string
          BaseAddress: int
          /// Live machine capture the emulator starts from (ROM + RAM).
          Memory: byte[]
          /// The fresh image span the CE program mirrors byte-for-byte (the
          /// parity claim's true target - live RAM drifts as the game runs).
          Image: byte[]
          Program: Jetpac2.Core.Z80Op list
          EntryState: string }

    let uridium: GameDef =
        { Id = "uridium"
          Name = "Uridium"
          BaseAddress = 0x4000
          Memory = UridiumGame.WebImage.memory
          Image = UridiumGame.WebImage.image
          Program = UridiumGame.WebImage.program
          EntryState = UridiumGame.WebImage.entryState }

    let all: GameDef list = [ uridium ]

    let registerAll () : unit =
        for g in all do
            GameRegistry.register
                { GameId = g.Id
                  Name = g.Name
                  Memory = g.Memory
                  BaseAddress = g.BaseAddress
                  Program = g.Program
                  EntryState = g.EntryState }

/// The running emulator: one CEGame, its frame loop, and the Spectrum key
/// matrix state the touch controls set. `held` is re-diffed on every input
/// change so shared cells (Caps Shift) release only when no remaining
/// direction needs them.
module Emulator =

    let mutable private game: CEGame option = None
    let mutable private timerId: obj = null
    let mutable private ctx: obj = null
    let mutable private img: obj = null
    let mutable private running = false
    let mutable private statusText = ""
    let mutable private held: Set<int * int> = Set.empty
    let mutable private frames = 0

    /// Diagnostics trail (browser console: window.__log).
    let private log (msg: string) : unit =
        emitJsExpr msg "((window.__log = window.__log || []).push($0))" |> ignore

    /// Frames run since boot (liveness probe).
    let Frames () = frames

    /// Start (or stop) the 20 ms frame loop (RunFrame + paint + beeper).
    let setRunning (on: bool) : unit =
        log (sprintf "setRunning %b (was %b)" on running)

        if on && not running then
            running <- true

            timerId <-
                Dom.setInterval
                    (fun () ->
                        match game with
                        | Some g ->
                            frames <- frames + 1
                            let frameStart, _ = g.RunFrame()

                            if not (isNull ctx) then
                                let src: byte[] = g.ScreenBuffer
                                let buf: Fable.Core.JS.Uint8ClampedArray = unbox (img?data)

                                for i in 0 .. 320 * 256 - 1 do
                                    let o = i * 4
                                    buf[o] <- src[o + 2]
                                    buf[o + 1] <- src[o + 1]
                                    buf[o + 2] <- src[o]
                                    buf[o + 3] <- src[o + 3]

                                ctx?putImageData (img, 0, 0)

                            Audio.Play(g.DrainBeeperSamples frameStart)
                        | None -> ())
                    20
        elif not on && running then
            running <- false
            Dom.clearInterval timerId

    let Running = running

    /// Wanted key state from the controls; diff against what is held.
    let applyKeys (dirs: Set<KeyMap.Dir>) (fire: bool) : unit =
        let wanted =
            (Set.empty, dirs)
            ||> Set.fold (fun acc d -> Set.union acc (Set.ofList (KeyMap.dirCells d)))
            |> fun acc -> if fire then Set.union acc (Set.ofList KeyMap.fireCells) else acc

        match game with
        | None -> ()
        | Some g ->
            for c in held do
                if not (wanted.Contains c) then
                    g.SetKey(fst c, snd c, false)

            for c in wanted do
                if not (held.Contains c) then
                    g.SetKey(fst c, snd c, true)

            held <- wanted

    /// Physical-keyboard fallback (desktop testing): arrows drive the same
    /// direction set, space fires. Reuses applyKeys so keyboard + touch agree.
    let mutable private dirsFromKeys: Set<KeyMap.Dir> = Set.empty
    let mutable private fireFromKeys = false

    let private syncKeys () = applyKeys dirsFromKeys fireFromKeys

    let private keyDir (key: string) : KeyMap.Dir option =
        match key with
        | "ArrowUp" -> Some KeyMap.N
        | "ArrowDown" -> Some KeyMap.S
        | "ArrowLeft" -> Some KeyMap.W
        | "ArrowRight" -> Some KeyMap.E
        | _ -> None

    let private isEditableTarget (e: obj) =
        let t: obj = e?target
        let tag = string (t?tagName)

        tag = "INPUT"
        || tag = "TEXTAREA"
        || tag = "SELECT"
        || string (t?isContentEditable) = "true"

    let private installKeyboardFallback () =
        Dom.window?addEventListener (
            "keydown",
            fun (e: obj) ->
                if isEditableTarget e then
                    ()
                else
                    let key: string = unbox (e?key)

                    match keyDir key with
                    | Some d ->
                        dirsFromKeys <- Set.add d dirsFromKeys
                        syncKeys ()
                    | None ->

                        if key = " " then
                            fireFromKeys <- true
                            syncKeys ()
        )

        Dom.window?addEventListener (
            "keyup",
            fun (e: obj) ->
                if isEditableTarget e then
                    ()
                else
                    let key: string = unbox (e?key)

                    match keyDir key with
                    | Some d ->
                        dirsFromKeys <- Set.remove d dirsFromKeys
                        syncKeys ()
                    | None ->

                        if key = " " then
                            fireFromKeys <- false
                            syncKeys ()
        )

    /// Boot the emulator host against a canvas already in the DOM. `?game=<id>`
    /// selects a registered game; default: the first one. Returns a status
    /// line with the game name and its CE parity result.
    let start (canvasId: string) : string =
        Games.registerAll ()
        installKeyboardFallback ()

        let search =
            match Dom.window?location?search with
            | null -> ""
            | s -> string s

        let wanted =
            if search.StartsWith "?game=" then
                search.Substring(6).ToLowerInvariant()
            else
                ""

        let selected =
            match wanted with
            | "" -> Games.all |> List.tryHead
            | w -> Games.all |> List.tryFind (fun g -> g.Id = w)
            |> Option.defaultWith (fun () -> Games.all |> List.head)

        let canvas = Dom.byId canvasId
        ctx <- canvas?getContext ("2d")
        img <- emitJsExpr (320, 256) "new ImageData($0, $1)"

        // The CE claim: the assembled program equals the game's fresh image
        // span byte-for-byte (checked via CEParity, not against live RAM,
        // which legitimately drifts as soon as the game runs).
        let matching, total, divergences = CEParity.check selected.Program selected.Image

        statusText <-
            if divergences.IsEmpty then
                sprintf "%s - CE parity: %d/%d bytes match" selected.Name matching total
            else
                sprintf "%s - CE parity: %d/%d, diverges at %A" selected.Name matching total divergences

        game <- Some(CEGame(selected.Program, selected.Memory, selected.EntryState))
        setRunning true
        log (sprintf "start: %s (parity %d/%d)" selected.Name matching total)

        let status = Dom.byId "statusLine"

        if not (isNull status) then
            status?textContent <- statusText

        statusText
