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

    /// Matrix cells for the number keys 1..5 (menu / level select row).
    let numCells (n: int) : (int * int) list = [ (3, n - 1) ]

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

    let manicminer: GameDef =
        { Id = "manicminer"
          Name = "Manic Miner"
          BaseAddress = 0x4000
          Memory = ManicminerGame.WebImage.memory
          Image = ManicminerGame.WebImage.image
          Program = ManicminerGame.WebImage.program
          EntryState = ManicminerGame.WebImage.entryState }

    let all: GameDef list =
        [ manicminer
          uridium ]

    let registerAll () : unit =
        for g in all do
            GameRegistry.register
                { GameId = g.Id
                  Name = g.Name
                  Memory = g.Memory
                  BaseAddress = g.BaseAddress
                  Program = g.Program
                  EntryState = g.EntryState }

/// The daily-mission watch. Armed by the Play page with the active mission's
/// detection rule; evaluated ONCE PER FRAME inside the frame loop (never
/// across the MVU boundary - only the single completion event dispatches).
/// The clock starts on the first held input and the HUD span is updated
/// imperatively, exactly like the rest of the per-frame DOM writes.
module Mission =

    module Js =
        let now () : float = emitJsExpr () "Date.now()"

    let mutable private mission: Missions.Mission option = None
    let mutable private startMillis: float option = None
    let mutable private fired = false
    let mutable private lastRatio = 0.0
    let mutable private transitions = 0
    let mutable private tickCount = 0

    /// Invoked once when the detection rule fires; carries the elapsed ms.
    let mutable OnCompleted: (int -> unit) option = None

    /// Arm the watch for a game's mission (a no-op for games without one).
    let Arm (gameId: string) : unit =
        mission <- Missions.forGame gameId
        startMillis <- None
        fired <- false
        lastRatio <- 0.0
        transitions <- 0
        tickCount <- 0

    let ElapsedMillis () : int option =
        startMillis |> Option.map (fun t -> int (Js.now () - t))

    /// First held input starts the clock (called from the input sync).
    let NotifyInput () =
        if startMillis.IsNone then startMillis <- Some(Js.now ())

    /// Bright-pixel ratio of the rendered frame: the fraction of RGBA pixels
    /// whose lightest channel exceeds 170.
    let private brightRatio (buf: byte[]) : float =
        let mutable bright = 0
        let total = 320 * 256

        for i in 0 .. total - 1 do
            let o = i * 4
            let r, g, b = int buf[o], int buf[o + 1], int buf[o + 2]

            if max r (max g b) > 170 then bright <- bright + 1

        float bright / float total

    /// Per-frame evaluation. Returns true exactly once when the mission's
    /// rule fires (the emulator keeps running afterwards - the completion is
    /// an overlay, not a stop). Nothing can fire before the player's first
    /// input: the clock gate doubles as the "game is being played" gate.
    let private evaluate (g: CEGame) : bool =
        if fired then
            false
        else
            match mission, ElapsedMillis () with
            | Some m, Some _ ->
                let hit =
                    match m.Detect with
                    | Missions.MemoryEquals (addr, value) -> int g.Memory[addr] = value
                    | Missions.MemoryNotEqual (addr, value) -> int g.Memory[addr] <> value
                    | Missions.ScreenFlash ->
                        // The cavern-complete flash ALTERNATES the whole
                        // display bright/dark, so the signature is large
                        // frame-to-frame swings of the bright ratio, not a
                        // high absolute ratio (the menu's grey panel sits
                        // high but never swings). Count big swings in a
                        // sliding window.
                        tickCount <- tickCount + 1
                        let ratio = brightRatio g.ScreenBuffer

                        if abs (ratio - lastRatio) > 0.5 then
                            transitions <- transitions + 1

                        lastRatio <- ratio

                        if tickCount % 120 = 0 then
                            transitions <- max 0 (transitions - 2)

                        transitions >= 3

                if hit then fired <- true

                hit
            | _ -> false

    /// Update the HUD timer span (imperative, called from the frame loop).
    let private tickHud () =
        match ElapsedMillis () with
        | Some ms when not fired ->
            let el: obj = Dom.byId "missionTime"

            if not (isNull el) then
                let total = ms / 1000
                el?textContent <- sprintf "%d:%02d" (total / 60) (total % 60)
        | _ -> ()

    /// Called by the frame loop after each frame.
    let FrameTick (g: CEGame) : unit =
        tickHud ()

        if evaluate g then
            match ElapsedMillis (), OnCompleted with
            | Some ms, Some callback -> callback ms
            | _ ->
                emitJsExpr () "((window.__log = window.__log || []).push('mission completed but no handler/clock'))"
                |> ignore

    // Demo/test hook: window.__puzzxleFireMission() fires the completion path
    // (overlay + leaderboard record) without playing the mission.
    do
        let fire () =
            if not fired then
                fired <- true

                let ms =
                    match ElapsedMillis () with
                    | Some m -> m
                    | None -> 4242

                match OnCompleted with
                | Some callback -> callback ms
                | None -> ()

        emitJsExpr fire "window.__puzzxleFireMission = $0" |> ignore

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

    /// GameId of the registered game the host booted with.
    let mutable CurrentGameId: string option = None
    /// Per-tick input poller (the gamepad library exposes polled state, not
    /// events; its poll runs at the top of every frame).
    let mutable private inputPoller: (unit -> unit) option = None

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

                            match inputPoller with
                            | Some f -> f ()
                            | None -> ()

                            let frameStart, _ = g.RunFrame()

                            Mission.FrameTick g

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

                            if Audio.Enabled then
                                Audio.Play(g.DrainBeeperSamples frameStart)
                            else
                                g.DiscardBeeper()
                        | None -> ())
                    20
        elif not on && running then
            running <- false
            Dom.clearInterval timerId

    let Running = running

    // Input sources, merged into the Spectrum matrix on every change. The
    // on-screen controls (the @rbuljan/gamepad dpad + fire, the number row)
    // and the physical keyboard each hold their own state.
    let mutable private padDirs: Set<KeyMap.Dir> = Set.empty
    let mutable private padFire = false
    let mutable private padNums: Set<int> = Set.empty
    let mutable private keyDirs: Set<KeyMap.Dir> = Set.empty
    let mutable private keyNums: Set<int> = Set.empty

    /// Raw matrix cells held by physical keys (letters, ENTER, 6-0, ...).
    let mutable private keyRaw: Set<int * int> = Set.empty

    let private sync () =
        let dirs = Set.union padDirs keyDirs
        let fire = padFire
        let nums = Set.union padNums keyNums

        let wanted =
            (Set.empty, dirs)
            ||> Set.fold (fun acc d -> Set.union acc (Set.ofList (KeyMap.dirCells d)))
            |> fun acc -> if fire then Set.union acc (Set.ofList KeyMap.fireCells) else acc
            |> fun acc ->
                (acc, nums)
                ||> Set.fold (fun acc n -> Set.union acc (Set.ofList (KeyMap.numCells n)))
            |> fun acc -> Set.union acc keyRaw

        match game with
        | None -> ()
        | Some g ->
            for c in held do
                if not (wanted.Contains c) then
                    g.SetKey(fst c, snd c, false)

            for c in wanted do
                if not (held.Contains c) then
                    g.SetKey(fst c, snd c, true)

            if wanted <> held then
                log (sprintf "keys: %A" (Set.toList wanted))

            if not wanted.IsEmpty then Mission.NotifyInput ()

            held <- wanted

    /// Direction state from the on-screen movement control.
    let SetPadDirs(d: Set<KeyMap.Dir>) : unit =
        if padDirs <> d then
            padDirs <- d
            sync ()

    /// Fire state from the on-screen fire button.
    let SetPadFire(f: bool) : unit =
        if padFire <> f then
            padFire <- f
            sync ()

    /// Number-key state (1..5) from the on-screen row.
    let SetPadNums(n: Set<int>) : unit =
        if padNums <> n then
            padNums <- n
            sync ()

    /// Register the per-frame input poller (called once at host boot).
    let SetInputPoller(f: unit -> unit) : unit = inputPoller <- Some f

    /// Physical-keyboard input (desktop play): the whole ZX keyboard, not
    /// just the touch controls' subset. Two layers, merged with the on-screen
    /// controls in `sync`:
    ///   - arrows AND WASD drive the cursor-joystick direction scheme
    ///     (KeyMap), space fires;
    ///   - every other key maps to its raw matrix cell, so games' own key
    ///     choices work: ENTER starts (Manic Miner's title), letters reach
    ///     the game's keyboard/redefine modes, 6-0 are the second number row.
    /// Cell values follow the repo's mirrored-bit convention (the same table
    /// the Desktop shell's keyMap uses).
    let private keyDir (key: string) : KeyMap.Dir option =
        match key with
        | "ArrowUp"
        | "w"
        | "W" -> Some KeyMap.N
        | "ArrowDown"
        | "s"
        | "S" -> Some KeyMap.S
        | "ArrowLeft"
        | "a"
        | "A" -> Some KeyMap.W
        | "ArrowRight"
        | "d"
        | "D" -> Some KeyMap.E
        | _ -> None

    let private keyNum (key: string) : int option =
        match key with
        | "1" -> Some 1
        | "2" -> Some 2
        | "3" -> Some 3
        | "4" -> Some 4
        | "5" -> Some 5
        | _ -> None

    /// Raw matrix cell for a physical key, outside the direction/number-row
    /// aliases above (WASD and arrows are taken by directions, 1-5 by the
    /// number row).
    let private keyRawCell (key: string) : (int * int) option =
        match key with
        | "q"
        | "Q" -> Some(2, 0)
        | "e"
        | "E" -> Some(2, 2)
        | "r"
        | "R" -> Some(2, 3)
        | "t"
        | "T" -> Some(2, 4)
        | "z"
        | "Z" -> Some(0, 1)
        | "x"
        | "X" -> Some(0, 2)
        | "c"
        | "C" -> Some(0, 3)
        | "v"
        | "V" -> Some(0, 4)
        | "f"
        | "F" -> Some(1, 3)
        | "g"
        | "G" -> Some(1, 4)
        | "6" -> Some(4, 4)
        | "7" -> Some(4, 3)
        | "8" -> Some(4, 2)
        | "9" -> Some(4, 1)
        | "0" -> Some(4, 0)
        | "p"
        | "P" -> Some(5, 0)
        | "o"
        | "O" -> Some(5, 1)
        | "i"
        | "I" -> Some(5, 2)
        | "u"
        | "U" -> Some(5, 3)
        | "y"
        | "Y" -> Some(5, 4)
        | "Enter" -> Some(6, 0)
        | "l"
        | "L" -> Some(6, 1)
        | "k"
        | "K" -> Some(6, 2)
        | "j"
        | "J" -> Some(6, 3)
        | "h"
        | "H" -> Some(6, 4)
        | " " -> Some(7, 0)
        | "Control" -> Some(7, 1)
        | "m"
        | "M" -> Some(7, 2)
        | "n"
        | "N" -> Some(7, 3)
        | "b"
        | "B" -> Some(7, 4)
        | "Shift" -> Some(0, 0)
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
                        keyDirs <- Set.add d keyDirs
                        sync ()
                    | None ->

                        match keyNum key with
                        | Some n ->
                            keyNums <- Set.add n keyNums
                            sync ()
                        | None ->

                            match keyRawCell key with
                            | Some cell ->
                                keyRaw <- Set.add cell keyRaw
                                sync ()
                            | None -> ()
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
                        keyDirs <- Set.remove d keyDirs
                        sync ()
                    | None ->

                        match keyNum key with
                        | Some n ->
                            keyNums <- Set.remove n keyNums
                            sync ()
                        | None ->

                            match keyRawCell key with
                            | Some cell ->
                                keyRaw <- Set.remove cell keyRaw
                                sync ()
                            | None -> ()
        )

    /// Boot the emulator host against a canvas already in the DOM.
    /// `?game=<id>` selects a registered game; default: the first one. With
    /// the hash router the query rides in location.hash (#play?game=x), so
    /// both the search part and the hash are consulted. Returns a status
    /// line with the game name and its CE parity result.
    let start (canvasId: string) : string =
        Games.registerAll ()
        installKeyboardFallback ()

        let rawQuery: string =
            let search =
                match Dom.window?location?search with
                | null -> ""
                | s -> string s

            let hash =
                match Dom.window?location?hash with
                | null -> ""
                | h -> string h

            let fromPart (p: string) =
                match p.Split('?') with
                | [| _ |] -> ""
                | parts -> parts[parts.Length - 1]

            let s = fromPart search
            if s <> "" then s else fromPart hash

        let wanted =
            if rawQuery.StartsWith "game=" then
                rawQuery.Substring(5).ToLowerInvariant()
            else
                ""

        let selected =
            match wanted with
            | "" -> Games.all |> List.tryHead
            | w -> Games.all |> List.tryFind (fun g -> g.Id = w)
            |> Option.defaultWith (fun () -> Games.all |> List.head)

        CurrentGameId <- Some selected.Id
        Mission.Arm selected.Id

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
