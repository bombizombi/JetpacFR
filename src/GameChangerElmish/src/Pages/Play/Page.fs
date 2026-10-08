module GameChangerElmish.Pages.Play.Page

// The Daily puzZXle play page, mobile-first: Spectrum screen on top, mission
// HUD, touch controls below. Movement (4-direction DPad + FIRE) comes from
// the @rbuljan/gamepad library, mounted into the empty #controls container by
// GamepadCtl; the number row (1-5, for menus) is ours.
//
// Touch input deliberately BYPASSES the MVU loop - a dispatch per press or
// drag would re-render the whole page on every event and starve the 20 ms
// emulator loop. Handlers apply keys straight to the imperative host and
// toggle "on" classes directly. The model only carries rarely-changing UI
// state (run/pause, sound) plus the one thing that does cross the boundary:
// the mission completion event, which fires exactly once per run. The timer
// in the HUD is written imperatively by the host (span #missionTime).

open Feliz
open Fable.Core.JsInterop
open Elmish
open ElmishLand
open GameChangerElmish
open GameChangerElmish.Shared
open GameChangerElmish.Host
open GameChangerElmish.Pages

type Model =
    { Running: bool
      Muted: bool
      CompletedMillis: int option
      Board: Leaderboard.Entry list
      Rank: int option }

type Msg =
    | LayoutMsg of Layout.Msg
    | ToggleRun
    | ToggleMute
    | Booted
    | MissionCompleted of int
    | DismissOverlay

let private missionForCurrentGame () : Missions.Mission option =
    Emulator.CurrentGameId |> Option.bind Missions.forGame

let init () =
    // The host and the gamepad need their DOM; init runs before React
    // mounts, so boot on a short delay (the App.fs warm-start pattern). The
    // follow-up Booted dispatch re-renders the page with the mission HUD
    // (the host only knows the selected game after start).
    let cmd =
        Command.ofCmd (
            Cmd.ofEffect (fun dispatch ->
                Mission.OnCompleted <- Some(fun ms -> dispatch (MissionCompleted ms))

                Dom.setTimeout
                    (fun () ->
                        ignore (Emulator.start "gameScreen")
                        GamepadCtl.start "#controls"
                        dispatch Booted)
                    30
                |> ignore)
        )

    let model =
        { Running = true
          Muted = true
          CompletedMillis = None
          Board = []
          Rank = None }

    model, cmd

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none
    | Booted -> model, Command.none
    | ToggleRun ->
        Emulator.setRunning (not model.Running)
        { model with Running = not model.Running }, Command.none
    | ToggleMute ->
        Audio.SetEnabled model.Muted // muted -> enabled
        { model with Muted = not model.Muted }, Command.none
    | MissionCompleted millis ->
        let board, rank =
            match missionForCurrentGame () with
            | Some m -> Leaderboard.record m System.DateTime.Today millis
            | None -> [], 0

        { model with
            CompletedMillis = Some millis
            Board = board
            Rank = (if board.IsEmpty then None else Some rank) },
        Command.none
    | DismissOverlay -> { model with CompletedMillis = None }, Command.none

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

// ---- mission pieces --------------------------------------------------------

let private missionOf () : Missions.Mission option = missionForCurrentGame ()

let private hud () : ReactElement =
    match missionOf () with
    | Some m ->
        Html.div [
            prop.className "hud"
            prop.children [
                Html.span [ prop.className "hudGoal"; prop.title m.Goal; prop.text m.Title ]
                Html.span [
                    prop.className "hudTimer"
                    prop.id "missionTime"
                    prop.text "0:00"
                ]
            ]
        ]
    | None -> Html.none

let private hintLine () : ReactElement =
    match missionOf () with
    | Some m -> Html.div [ prop.className "hintbar"; prop.text m.Hint ]
    | None -> Html.none

let private boardRow (i: int) (e: Leaderboard.Entry) : ReactElement =
    Html.div [
        prop.className (if e.You then "lbrow you" else "lbrow")
        prop.children [
            Html.span [ prop.className "lbrank"; prop.text (string (i + 1)) ]
            Html.span [ prop.className "lbname"; prop.text e.Name ]
            Html.span [ prop.className "lbtime"; prop.text (Leaderboard.format e.Millis) ]
        ]
    ]

let private overlay (model: Model) (dispatch: Msg -> unit) : ReactElement =
    match model.CompletedMillis with
    | None -> Html.none
    | Some ms ->
        Html.div [
            prop.className "overlay"
            prop.children [
                Html.div [
                    prop.className "panel"
                    prop.children [
                        Html.div [ prop.className "panelTitle"; prop.text "MISSION COMPLETE" ]
                        Html.div [
                            prop.className "panelTime"
                            prop.text (Leaderboard.format ms)
                        ]
                        Html.div [
                            prop.className "panelRank"
                            prop.text (
                                match model.Rank with
                                | Some r ->
                                    sprintf "rank %d of %d today" r (max model.Board.Length 1)
                                | None -> ""
                            )
                        ]
                        Html.div [
                            prop.className "lb"
                            prop.children (model.Board |> List.truncate 8 |> List.mapi boardRow)
                        ]
                        Html.div [
                            prop.className "panelNote"
                            prop.text "The emulator keeps running - watch the rest of the level."
                        ]
                        Html.div [
                            prop.className "panelBtns"
                            prop.children [
                                Html.button [
                                    prop.className "tbtn"
                                    prop.text "Keep watching"
                                    prop.onClick (fun _ -> dispatch DismissOverlay)
                                ]
                                Html.button [
                                    prop.className "tbtn primary"
                                    prop.text "Play again"
                                    prop.onClick (fun _ ->
                                        emitJsExpr () "window.location.reload ()" |> ignore)
                                ]
                                Html.a [
                                    prop.className "tbtn"
                                    prop.href "#/"
                                    prop.text "Daily"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

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
                    Html.a [ prop.className "title"; prop.href "#/"; prop.text "DAILY PUZXLE" ]
                    hud ()
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
            Html.div [
                prop.className "screenWrap"
                prop.children [
                    Html.canvas [
                        prop.id "gameScreen"
                        prop.className "screen"
                        prop.width 320
                        prop.height 256
                    ]
                    overlay model dispatch
                ]
            ]
            Html.div [
                prop.className "statusbar"
                prop.id "statusLine"
                prop.text "booting emulator..."
            ]
            hintLine ()
            numRow
            // Movement controls (dpad + fire) mount here via @rbuljan/gamepad.
            Html.div [ prop.id "controls"; prop.className "controls" ]
        ]
    ]

let page (_shared: SharedModel) (_route: PlayRoute) =
    Page.from init update view () LayoutMsg
