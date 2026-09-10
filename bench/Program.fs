// Throwaway per-frame cost benchmark: boots a TraceSession the same way the
// Desktop shell does (warm entry cache when present) and times interpreter
// RunFrame calls. Build Debug vs Release to compare.

module JetpacFR.Bench

open System
open System.IO
open JetpacFR.Core

[<EntryPoint>]
let main _ =
    let rom = LocalAssets.find "48.rom"
    let tzx = LocalAssets.find "Jetpac.tzx"

    printfn "config: %s" (if System.Diagnostics.Debugger.IsAttached then "debugger" else "no debugger")

    let sw = Diagnostics.Stopwatch.StartNew()
    let s = TraceSession(rom, tzx, 4_000_000, fastBoot = true)
    sw.Stop()
    printfn "boot: %d ms (frame %d, warm=%b)" sw.ElapsedMilliseconds s.Frame s.WarmStart

    for _ in 1..50 do
        s.RunFrame() |> ignore

    let n = 300
    sw.Restart()

    for _ in 1..n do
        s.RunFrame() |> ignore

    sw.Stop()

    let per = float sw.ElapsedMilliseconds / float n

    printfn "run: %d frames in %d ms -> %.2f ms/frame = %.1f fps" n sw.ElapsedMilliseconds per (1000.0 / per)

    // === Replay path, exactly like the app's autoplay: load the recorded
    // slot timeline, StartReplay, then RunFrame until the script ends. ===
    let gameDir = Path.GetFullPath "games/jetpac"
    let manifest: GameManifest =
        { GameId = "jetpac"
          ManifestPath = Path.Combine(gameDir, "manifest.json")
          GameDirectory = gameDir
          Name = "Jetpac"
          Default = true
          Rom = rom
          Tzx = tzx
          Boot = "auto"
          Script = None
          ProgramBin = None
          ProgramAddress = None }

    let slotFile = Path.Combine(gameDir, "timeline.jst")

    let fp = ReplayStore.fingerprint manifest

    let tfp: TimelineFingerprint =
        { GameId = manifest.GameId
          RomSha256 = fp.RomSha256
          TzxSha256 = fp.TzxSha256
          ProgramSha256 = fp.ProgramSha256
          ProgramAddress = fp.ProgramAddress }

    match StateTimelineStore.tryLoad slotFile tfp with
    | TimelineLoaded(tl, events) ->
        printfn "slot: %d frames, %d key events" tl.Count events.Length
        s.LoadStateTimeline(tl, events)
        s.StartReplay()

        sw.Restart()
        let mutable played = 0

        while not s.ReplayFinished && played < 2000 do
            s.RunFrame() |> ignore
            played <- played + 1

        sw.Stop()

        let rper = float sw.ElapsedMilliseconds / float (max 1 played)

        printfn
            "replay: %d frames in %d ms -> %.2f ms/frame = %.1f fps"
            played
            sw.ElapsedMilliseconds
            rper
            (1000.0 / rper)
    | other -> printfn "slot load failed: %A" other

    0
