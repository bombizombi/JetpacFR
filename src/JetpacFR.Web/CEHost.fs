namespace JetpacFR.Web

open System
open Fable.Core
open Fable.Core.JsInterop
open JetpacFR.Core

/// Standalone CE host (Phase 6): runs the minimal game's compiled-F# CE
/// program in the browser — the same Machine driven by `Z80.runFrame`, so
/// covered instructions execute directly (no interpreter path), raw blocks
/// fall through to the interpreter, and the byte-emission runs on start
/// (the parity line). DOM: #ceScreen canvas, #ceBtn toggle, #ceStatus.
module CEHost =

  let mutable private game: CEGame option = None
  let mutable private running = false
  let mutable private timerId: obj = null
  let mutable private ctx: obj = null
  let mutable private img: obj = null

  let private keyCells (key: string) : (int * int) list =
    match key with
    | "q" -> [ (2, 0) ]
    | "w" -> [ (2, 1) ]
    | "e" -> [ (2, 2) ]
    | "r" -> [ (2, 3) ]
    | "t" -> [ (2, 4) ]
    | "a" -> [ (1, 0) ]
    | "s" -> [ (1, 1) ]
    | "d" -> [ (1, 2) ]
    | "f" -> [ (1, 3) ]
    | "g" -> [ (1, 4) ]
    | "1" -> [ (3, 0) ]
    | "2" -> [ (3, 1) ]
    | "3" -> [ (3, 2) ]
    | "4" -> [ (3, 3) ]
    | "5" -> [ (3, 4) ]
    | "6" -> [ (4, 4) ]
    | "7" -> [ (4, 3) ]
    | "8" -> [ (4, 2) ]
    | "9" -> [ (4, 1) ]
    | "0" -> [ (4, 0) ]
    | " " -> [ (7, 0) ]
    | "Shift" -> [ (0, 0) ]
    | "ArrowLeft" -> [ (0, 0); (3, 4) ]
    | "ArrowRight" -> [ (0, 0); (4, 2) ]
    | "ArrowUp" -> [ (0, 0); (4, 3) ]
    | "ArrowDown" -> [ (0, 0); (4, 4) ]
    | _ -> []

  let private paint () =
    match game with
    | Some g ->
      let src: byte[] = g.ScreenBuffer
      let buf: Fable.Core.JS.Uint8ClampedArray = unbox (img?data)
      for i in 0 .. 320 * 256 - 1 do
        let o = i * 4
        buf.[o] <- src.[o + 2]
        buf.[o + 1] <- src.[o + 1]
        buf.[o + 2] <- src.[o]
        buf.[o + 3] <- src.[o + 3]
      ctx?putImageData(img, 0, 0)
    | None -> ()

  let start () =
    let canvas = Dom.byId "ceScreen"
    if isNull canvas then
      ()
    else
      ctx <- canvas?getContext("2d")
      img <- emitJsExpr (320, 256) "new ImageData($0, $1)"
      let btn = Dom.byId "ceBtn"
      let status = Dom.byId "ceStatus"
      // Byte emission on start + parity vs the original image. The game
      // comes from the registry (?game=<id> in the URL selects; default:
      // the first registered game). Shells register their games here;
      // today only minimal exists.
      let mem, state = MinimalGame.Game.entryState MinimalGame.Image.binary
      GameRegistry.register
        { GameId = "minimal"
          Name = "Minimal"
          Memory = mem
          BaseAddress = 0x8000
          Program = MinimalGame.Image.program
          EntryState = state }
      let search =
        match Dom.window?location?search with
        | null -> ""
        | s -> string s
      let wanted =
        if search.StartsWith "?game=" then search.Substring(6).ToLowerInvariant() else ""
      let selected =
        match wanted with
        | "" -> GameRegistry.all () |> List.tryHead
        | w -> GameRegistry.tryFind w
        |> Option.defaultWith (fun () ->
          { GameId = "minimal"
            Name = "Minimal"
            Memory = mem
            BaseAddress = 0x8000
            Program = MinimalGame.Image.program
            EntryState = state })
      let image = selected.Memory
      let matching, total, divergences = GameRegistry.parity selected
      let parity =
        if divergences.IsEmpty then sprintf "CE parity: %d/%d bytes match" matching total
        else sprintf "CE parity: %d/%d - diverges at %A" matching total divergences
      game <- Some(CEGame(selected.Program, image, selected.EntryState))
      status?textContent <- sprintf "CE engine - frame 0, %s" parity
      btn?onclick <- fun _ ->
        running <- not running
        if running then
          btn?textContent <- "Pause CE"
          timerId <-
            Dom.setInterval
              (fun () ->
                match game with
                | Some g ->
                  let frameStart, _ = g.RunFrame()
                  paint ()
                  Audio.Play(g.DrainBeeperSamples frameStart)
                  status?textContent <- sprintf "CE engine - frame %d, %s" g.Frame parity
                | None -> ())
              20
        else
          btn?textContent <- "Run CE"
          Dom.window?clearInterval(timerId)
      Dom.window?addEventListener("keydown", fun (e: obj) ->
        let key: string = unbox (e?key)
        match game with
        | Some g -> List.iter (fun (r, b) -> g.SetKey(r, b, true)) (keyCells key)
        | None -> ())
      Dom.window?addEventListener("keyup", fun (e: obj) ->
        let key: string = unbox (e?key)
        match game with
        | Some g -> List.iter (fun (r, b) -> g.SetKey(r, b, false)) (keyCells key)
        | None -> ())
