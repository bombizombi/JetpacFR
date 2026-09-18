module GameChangerElmish.Pages.Page

// The Game Changer play page, mobile-first: Spectrum screen on top, touch
// controls below. Movement (4-direction DPad + FIRE) comes from the
// @rbuljan/gamepad library, mounted into the empty #controls container by
// GamepadCtl; the number row (1-5, for menus) is ours.
//
// Touch input deliberately BYPASSES the MVU loop - a dispatch per press or
// drag would re-render the whole page on every event and starve the 20 ms
// emulator loop. Handlers apply keys straight to the imperative host and
// toggle "on" classes directly. The model only carries rarely-changing UI
// state (run/pause, sound).

open Feliz
open Fable.Core.JsInterop
open ElmishLand
open GameChangerElmish
open GameChangerElmish.Shared
open GameChangerElmish.Host
open GameChangerElmish.Pages

type Model =
    { Running: bool
      Muted: bool }

type Msg =
    | LayoutMsg of Layout.Msg
    | ToggleRun
    | ToggleMute

let init () =
    // The host and the gamepad need their DOM; init runs before React
    // mounts, so boot on a short delay (the App.fs warm-start pattern).
    Dom.setTimeout
        (fun () ->
            ignore (Emulator.start "gameScreen")
            GamepadCtl.start "#controls")
        30
    |> ignore

    { Running = true; Muted = true }, Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
    | ToggleRun ->
        Emulator.setRunning (not model.Running)
        { model with Running = not model.Running }, Command.none
    | ToggleMute ->
        Audio.SetEnabled model.Muted // muted -> enabled
        { model with Muted = not model.Muted }, Command.none

// ---- number row (imperative, outside React) --------------------------------
//
// Press-and-hold Spectrum keys 1..5 for the game's menus. Each key tracks
// its own pointer id, so several can be held at once.

type Ev = Browser.Types.PointerEvent

let mutable private numHeld: Set<int> = Set.empty
let mutable private numPids: Map<int, int> = Map.empty // pointerId -> number key

let private capture (e: Ev) : unit =
    try
        e?currentTarget?setPointerCapture (e?pointerId)
    with _ ->
        ()

let private pointerId (e: Ev) : int = unbox (e?pointerId)

/// Mirror the held number keys onto their "on" classes.
let private markNums () =
    for n in 1 .. 5 do
        let el: obj = emitJsExpr n "document.getElementById('num-' + $0)"

        if not (isNull el) then
            el?classList?toggle ("on", numHeld.Contains n) |> ignore

let private numDown (n: int) (e: Ev) : unit =
    e.preventDefault ()
    capture e
    numPids <- Map.add (pointerId e) n numPids

    if not (numHeld.Contains n) then
        numHeld <- Set.add n numHeld
        Emulator.SetPadNums numHeld
        markNums ()

let private numUp (e: Ev) : unit =
    match Map.tryFind (pointerId e) numPids with
    | Some n ->
        numPids <- Map.remove (pointerId e) numPids

        if numHeld.Contains n then
            numHeld <- Set.remove n numHeld
            Emulator.SetPadNums numHeld
            markNums ()
    | None -> ()

// ---- view ------------------------------------------------------------------

/// The Spectrum number keys 1..5 (menus, level select) - a slimmer row above
/// the gamepad area.
let private numRow =
    Html.div [
        prop.className "numrow"
        prop.children [
            for n in 1 .. 5 ->
                Html.div [
                    prop.id ("num-" + string n)
                    prop.className "numkey"
                    prop.text (string n)
                    prop.onPointerDown (numDown n)
                    prop.onPointerUp numUp
                    prop.onPointerCancel numUp
                ]
        ]
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
            numRow
            // Movement controls (dpad + fire) mount here via @rbuljan/gamepad.
            Html.div [ prop.id "controls"; prop.className "controls" ]
        ]
    ]

let page (_shared: SharedModel) (_route: HomeRoute) =
    Page.from init update view () LayoutMsg
