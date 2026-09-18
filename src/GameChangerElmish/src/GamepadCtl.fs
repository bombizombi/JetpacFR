module GameChangerElmish.GamepadCtl

// On-screen movement controls from @rbuljan/gamepad
// (https://github.com/rokobuljan/gamepad): a 4-direction DPad plus a FIRE
// button, mounted inside the page's #controls container.
//
// The library exposes polled state (`gamepad.controllers[id].value/.direction`),
// not F#-friendly events, so we read it once per frame through the emulator
// host's input-poller slot - two property reads and a diff per tick keeps
// input render-free. `onInput` is deliberately unused (its callback relies
// on JS `this`, which F# lambdas don't bind).

open Fable.Core
open Fable.Core.JsInterop
open GameChangerElmish.Host

[<Import("Gamepad", "@rbuljan/gamepad")>]
let private GamepadClass: obj = jsNative

let mutable private gamepad: obj = null
let mutable private parentSel = ""
let mutable private lastDirs: Set<KeyMap.Dir> = Set.empty
let mutable private lastFire = false

let private ctl (id: string) : obj =
    emitJsExpr (gamepad, id) "$0.controllers[$1]"

/// Mount the gamepad inside `parentSel` and register the poller. Safe to
/// call repeatedly: skips when already mounted.
let private mount (parentSel: string) : unit =
    let parent: obj = emitJsExpr parentSel "document.querySelector($0)"

    if isNull parent then
        ()
    elif parent?childElementCount > 0 then
        ()
    else
        let move =
            {| id = "move"
               ``type`` = "dpad"
               parent = parentSel
               axis = "all"
               directionsTot = 4
               radius = 56
               position = {| left = "70%"; top = "50%" |} |}

        let fire =
            {| id = "fire"
               ``type`` = "button"
               parent = parentSel
               text = "FIRE"
               radius = 40
               position = {| left = "17%"; top = "50%" |} |}

        gamepad <- createNew GamepadClass ([| box move; box fire |] : obj[])
        lastDirs <- Set.empty
        lastFire <- false

/// Read dpad/fire state and push changes into the emulator host. The dpad's
/// `direction` index runs clockwise from the right (0=R, 1=D, 2=L, 3=U with
/// `directionsTot: 4`); `value` is 0 in the dead zone / on release. If the
/// controls container got wiped (e.g. by a re-render), re-mount first.
let private poll () : unit =
    let parent: obj = emitJsExpr parentSel "document.querySelector($0)"

    if isNull parent then
        ()
    elif parent?childElementCount = 0 then
        mount parentSel
    else
        let move = ctl "move"
        let fire = ctl "fire"

        let dirs =
            if unbox (move?value) = 1 then
                match unbox (move?direction) with
                | 0 -> Set.singleton KeyMap.E
                | 1 -> Set.singleton KeyMap.S
                | 2 -> Set.singleton KeyMap.W
                | 3 -> Set.singleton KeyMap.N
                | _ -> Set.empty
            else
                Set.empty

        let fire' = unbox (fire?value) = 1

        if dirs <> lastDirs || fire' <> lastFire then
            lastDirs <- dirs
            lastFire <- fire'
            Emulator.SetPadDirs dirs
            Emulator.SetPadFire fire'

/// Mount the controls and start per-frame polling.
let start (selector: string) : unit =
    parentSel <- selector
    let parent: obj = emitJsExpr selector "document.querySelector($0)"

    if not (isNull parent) then
        Emulator.SetInputPoller poll
        mount selector
