module GameChangerElmish.Pages.Page

// The Daily puzZXle landing page: today's mission, every defined mission as
// a card, personal bests and a teaser of today's (faked, browser-local)
// leaderboard. Pure MVU - the emulator is not on this page.

open Feliz
open ElmishLand
open GameChangerElmish
open GameChangerElmish.Shared
open GameChangerElmish.Pages

type Model = unit

type Msg =
    | LayoutMsg of Layout.Msg

let init () =
    (),
    Command.none

let update (msg: Msg) (model: Model) =
    match msg with
    | LayoutMsg _ -> model, Command.none

let private today = System.DateTime.Today

let private missionCard (m: Missions.Mission) (isToday: bool) : ReactElement =
    let board = Leaderboard.entries m today
    let top = board |> List.truncate 3
    let yourBest = Leaderboard.yourBest m.Id today

    Html.div [
        prop.className (if isToday then "card today" else "card")
        prop.children [
            Html.div [
                prop.className "cardHead"
                prop.children [
                    Html.span [
                        prop.className "cardGame"
                        prop.text m.Title
                    ]
                    if isToday then
                        Html.span [ prop.className "badge"; prop.text "TODAY" ]
                ]
            ]
            Html.div [ prop.className "cardGoal"; prop.text m.Goal ]
            Html.div [
                prop.className "cardPb"
                prop.text (
                    match yourBest with
                    | Some ms -> sprintf "Your best today: %s" (Leaderboard.format ms)
                    | None -> "Not played today"
                )
            ]
            Html.div [
                prop.className "lb mini"
                prop.children (
                    top
                    |> List.mapi (fun i e ->
                        Html.div [
                            prop.className (if e.You then "lbrow you" else "lbrow")
                            prop.key (sprintf "%s-%s" m.Id e.Name)
                            prop.children [
                                Html.span [ prop.className "lbrank"; prop.text (string (i + 1)) ]
                                Html.span [ prop.className "lbname"; prop.text e.Name ]
                                Html.span [
                                    prop.className "lbtime"
                                    prop.text (Leaderboard.format e.Millis)
                                ]
                            ]
                        ])
                )
            ]
            Html.a [
                prop.className "playbtn"
                prop.href (sprintf "#play?game=%s" m.GameId)
                prop.text "Play mission"
            ]
        ]
    ]

let view (_model: Model) (_dispatch: Msg -> unit) : ReactElement =
    let todayIdx = Missions.todaysIndex today

    Html.div [
        prop.className "daily"
        prop.children [
            Html.header [
                prop.className "dailyHead"
                prop.children [
                    Html.h1 [ prop.text "DAILY PUZXLE" ]
                    Html.p [
                        prop.className "tagline"
                        prop.text "One ZX Spectrum mission a day. A couple of minutes..."
                    ]
                    Html.p [
                        prop.className "date"
                        prop.text (today.ToString "dddd, d MMMM yyyy")
                    ]
                ]
            ]
            Html.div [
                prop.className "cards"
                prop.children (
                    Missions.all
                    |> List.mapi (fun i m -> missionCard m (i = todayIdx))
                )
            ]
            Html.footer [
                prop.className "dailyFoot"
                prop.text "Runs on an in-browser ZX Spectrum emulator. Scores live in your browser only."
            ]
        ]
    ]

let page (_shared: SharedModel) (_route: HomeRoute) =
    Page.from init update view () LayoutMsg
