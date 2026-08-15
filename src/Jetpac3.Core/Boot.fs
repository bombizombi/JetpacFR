namespace Jetpac3.Core

open System
open System.IO

/// Boot the oracle emulator (JetpacFSharp Spectrum48) to the game entry, the
/// same logic Jetpac2's extract/test harness uses. No new logic here; it is
/// kept in the core so the GUI and tests share one boot path.
module Boot =

  let findCodeBlocks (tzxBytes: byte[]) : byte[] * byte[] =
    let blocks = Jetpac.Core.Tape.parseTzx tzxBytes
    let frameOf (b: Jetpac.Core.Tape.TzxBlock) : byte[] =
      match b with
      | Jetpac.Core.Tape.StandardData (_, p) -> p
      | Jetpac.Core.Tape.TurboData (_, _, _, _, p) -> p
      | Jetpac.Core.Tape.Skipped _ -> [||]
    let mutable pendingHeader: (string * int) option = None
    let mutable screen: byte[] option = None
    let mutable gameCode: byte[] option = None
    for block in blocks do
      let payload = frameOf block
      if payload.Length > 0 then
        match Jetpac.Core.Tape.headerInfo payload with
        | Some (t, _, len, _, _) -> pendingHeader <- Some(t, len)
        | None ->
          let s = Jetpac.Core.Tape.stripFrame payload
          match pendingHeader with
          | Some ("CODE", 6912) when screen.IsNone -> screen <- Some(s.[0 .. 6911])
          | Some ("CODE", 8192) when gameCode.IsNone -> gameCode <- Some(s.[0 .. 8191])
          | _ -> ()
    match screen, gameCode with
    | Some s, Some g -> s, g
    | None, _ -> failwith "Could not find a 6912-byte CODE block in the TZX"
    | _, None -> failwith "Could not find an 8192-byte CODE block in the TZX"

  /// Boot to the game entry instruction; returns (spec, entryCycles).
  let bootToEntry (romPath: string) (tzxPath: string) : Jetpac.Core.Spectrum48 * int64 =
    let rom = File.ReadAllBytes romPath
    let tzx = File.ReadAllBytes tzxPath
    let (expectedScreen, expectedGameCode) = findCodeBlocks tzx
    let spec = Jetpac.Core.Spectrum48()
    spec.LoadRom rom
    spec.InsertTape tzx

    let mutable frames = 0
    let mutable loaded = false
    while frames < 8000 && not loaded do
      spec.RunFrame()
      frames <- frames + 1
      let mutable matchCount = 0
      let mutable i = 0x4000
      while i <= 0x5AFF && matchCount >= 0 do
        if spec.Memory.[i] = expectedScreen.[i - 0x4000] then matchCount <- matchCount + 1
        else matchCount <- -1
        i <- i + 1
      if matchCount = 6912 then loaded <- true
    if not loaded then
      failwithf "Loading screen never matched after %d frames" frames

    let mutable codeLoaded = false
    let mutable codeFrames = frames
    while codeFrames < 8000 && not codeLoaded do
      spec.RunFrame()
      codeFrames <- codeFrames + 1
      let mutable m = 0
      let mutable ok = true
      while m < 8192 && ok do
        if spec.Memory.[0x6000 + m] <> expectedGameCode.[m] then ok <- false
        m <- m + 1
      if ok then codeLoaded <- true
    if not codeLoaded then
      failwithf "Game code at 0x6000 never matched by frame %d" codeFrames

    let mutable moverStarted = false
    let mutable g = 0
    while not moverStarted && g < 2000 do
      spec.RunFrame()
      g <- g + 1
      if spec.Memory.[0x6000] <> expectedGameCode.[0] then moverStarted <- true
    if not moverStarted then
      failwithf "Game loader never started after %d frames" g
    let z80 = spec.DebugZ80
    g <- 0
    while (z80.Regs.Pc() < 0x6000 || z80.Regs.Pc() > 0x7FFF) && g < 300000 do
      z80.ExecuteOne()
      g <- g + 1
    if z80.Regs.Pc() < 0x6000 || z80.Regs.Pc() > 0x7FFF then
      failwithf "Never reached game entry (pc=%04X)" (z80.Regs.Pc())
    spec, int64 (z80.CycleCount())
