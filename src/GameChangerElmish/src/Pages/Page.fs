module GameChangerElmish.Pages.Page

// The Game Changer play page, mobile-first: Spectrum screen on top, touch
// controls below (fire on the left third, an 8-direction keypad on the right
// two-thirds). All styling lives in index.html; this module is behavior:
// pointer tracking that maps finger position to Spectrum key state and
// forwards it to the imperative host (GameChangerElmish.Host).

open Feliz
open Fable.Core.JsInterop
open ElmishLand
open GameChangerElmish.Shared
open GameChangerElmish.Host
open GameChangerElmish.Pages

type Model =
    { Dirs: Set<KeyMap.Dir>
      Fire: bool
      Running: bool
      Muted: bool }

type Msg =
    | LayoutMsg of Layout.Msg
    | DirsChange of Set<KeyMap.Dir>
    | FireChange of bool
    | ToggleRun
    | ToggleMute

let init () =
    // The host needs the canvas in the DOM; init runs before React mounts,
    // so boot it on a short delay (the App.fs warm-start pattern).
    Dom.setTimeout (fun () -> ignore (Emulator.start "gameScreen")) 30 |> ignore

    { Dirs = Set.empty
      Fire = false
      Running = true
      Muted = true },
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
    | DirsChange dirs ->
        Emulator.applyKeys dirs model.Fire
        { model with Dirs = dirs }, Command.none
    | FireChange fire ->
        Emulator.applyKeys model.Dirs fire
        { model with Fire = fire }, Command.none
    | ToggleRun ->
        Emulator.setRunning (not model.Running)
        { model with Running = not model.Running }, Command.none
    | ToggleMute ->
        Audio.SetEnabled model.Muted // muted -> enabled
        { model with Muted = not model.Muted }, Command.none

// ---- pointer plumbing -----------------------------------------------------
//
// Each control (fire button, keypad) captures its pointer on pointerdown, so
// fingers can slide around freely (and leave the element) while events keep
// arriving at the capturing element. Fire and keypad track their own pointer
// id, so both work simultaneously with two fingers. Event members are read
// through the dynamic operator - currentTarget has no typed rect accessor.

type Ev = Browser.Types.PointerEvent

let mutable private keypadPid: int option = None
let mutable private firePid: int option = None

let private pointerId (e: Ev) : int = unbox (e?pointerId)

let private capture (e: Ev) : unit =
    try
        e?currentTarget?setPointerCapture (e?pointerId)
    with _ ->
        ()

let private samePointer (pid: int option) (e: Ev) : bool =
    match pid with
    | Some p -> p = pointerId e
    | None -> false

/// Map a pointer position on the keypad element to its 3x3 cell, then to the
/// direction it implies (center cell = no direction).
let private dirAt (e: Ev) : KeyMap.Dir option =
    let rect = e?currentTarget?getBoundingClientRect ()
    let x: float = unbox (e?clientX) - unbox (rect?left)
    let y: float = unbox (e?clientY) - unbox (rect?top)
    let w: float = unbox (rect?width)
    let h: float = unbox (rect?height)

    let clamp (v: int) = max 0 (min 2 v)

    let col = clamp (int (x / w * 3.0))
    let row = clamp (int (y / h * 3.0))

    match row, col with
    | 0, 0 -> Some KeyMap.NW
    | 0, 1 -> Some KeyMap.N
    | 0, 2 -> Some KeyMap.NE
    | 1, 0 -> Some KeyMap.W
    | 1, 1 -> None
    | 1, 2 -> Some KeyMap.E
    | 2, 0 -> Some KeyMap.SW
    | 2, 1 -> Some KeyMap.S
    | 2, 2 -> Some KeyMap.SE
    | _ -> None

/// Keypad press or drag: re-presses as the finger moves across cells.
let private keypadDown (dispatch: Msg -> unit) (e: Ev) : unit =
    e.preventDefault ()
    capture e
    keypadPid <- Some(pointerId e)
    dispatch (DirsChange(dirAt e |> function Some d -> Set.singleton d | None -> Set.empty))

let private keypadMove (dispatch: Msg -> unit) (e: Ev) : unit =
    if samePointer keypadPid e then
        e.preventDefault ()
        dispatch (DirsChange(dirAt e |> function Some d -> Set.singleton d | None -> Set.empty))

let private keypadUp (dispatch: Msg -> unit) (e: Ev) : unit =
    if samePointer keypadPid e then
        keypadPid <- None
        dispatch (DirsChange Set.empty)

let private fireDown (dispatch: Msg -> unit) (e: Ev) : unit =
    e.preventDefault ()
    capture e
    firePid <- Some(pointerId e)
    dispatch (FireChange true)

let private fireUp (dispatch: Msg -> unit) (e: Ev) : unit =
    if samePointer firePid e then
        firePid <- None
        dispatch (FireChange false)

// ---- view ------------------------------------------------------------------

let private dirName (d: KeyMap.Dir) =
    match d with
    | KeyMap.N -> "↑"
    | KeyMap.NE -> "↗"
    | KeyMap.E -> "→"
    | KeyMap.SE -> "↘"
    | KeyMap.S -> "↓"
    | KeyMap.SW -> "↙"
    | KeyMap.W -> "←"
    | KeyMap.NW -> "↖"

/// One keypad cell; cells whose direction is held get the "on" highlight.
let private cell (model: Model) (row: int) (col: int) (dir: KeyMap.Dir option) =
    let active =
        match dir with
        | Some d -> model.Dirs.Contains d
        | None -> false

    Html.div [
        prop.className [
            "cell"
            if active then "on"
            if dir.IsNone then "center"
        ]
        prop.key (sprintf "cell-%d-%d" row col)
        if dir.IsSome then
            prop.text (dirName dir.Value)
    ]

let private keypad (model: Model) (dispatch: Msg -> unit) =
    let grid =
        [ [ Some KeyMap.NW; Some KeyMap.N; Some KeyMap.NE ]
          [ Some KeyMap.W; None; Some KeyMap.E ]
          [ Some KeyMap.SW; Some KeyMap.S; Some KeyMap.SE ] ]

    let cells =
        grid
        |> List.mapi (fun r row -> row |> List.mapi (fun c dir -> cell model r c dir))
        |> List.collect id

    Html.div [
        prop.className "keypad"
        prop.onPointerDown (keypadDown dispatch)
        prop.onPointerMove (keypadMove dispatch)
        prop.onPointerUp (keypadUp dispatch)
        prop.onPointerCancel (keypadUp dispatch)
        prop.children cells
    ]

let private fireButton (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className [
            "fire"
            if model.Fire then "on"
        ]
        prop.text "FIRE"
        prop.onPointerDown (fireDown dispatch)
        prop.onPointerUp (fireUp dispatch)
        prop.onPointerCancel (fireUp dispatch)
    ]

let view (model: Model) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "app"
        prop.children [
            Html.div [
                prop.className "topbar"
                prop.children [
                    Html.span [
                        prop.className "title"
                        prop.text "GAME CHANGER"
                    ]
                    Html.button [
                        prop.className "tbtn"
                        prop.text (if model.Running then "Pause" else "Run")
                        prop.onClick (fun _ -> dispatch ToggleRun)
                    ]
                    Html.button [
                        prop.className "tbtn"
                        prop.text (if model.Muted then "Sound off" else "Sound on")
                        prop.onClick (fun _ -> dispatch ToggleMute)
                    ]
                ]
            ]
            Html.canvas [
                prop.id "gameScreen"
                prop.className "screen"
                prop.width 320
                prop.height 256
            ]
            Html.div [
                prop.className "statusbar"
                prop.id "statusLine"
                prop.text "booting emulator..."
            ]
            Html.div [
                prop.className "controls"
                prop.children [ fireButton model dispatch; keypad model dispatch ]
            ]
        ]
    ]

let page (_shared: SharedModel) (_route: HomeRoute) =
    Page.from init update view () LayoutMsg
