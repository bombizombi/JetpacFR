namespace Jetpac3.Core

open System
open System.Collections.Generic
open System.IO
open System.Text

/// A single beeper output event: the speaker state (ULA port 0xFE bit 4)
/// after an OUT at absolute CPU cycle `Tick`. Port of the reference C#
/// `BeeperEvent(long Tick, bool State)` record struct.
[<Struct>]
type BeeperEvent =
  { Tick: int64
    State: bool }

/// Renders the 1-bit beeper event log into 16-bit PCM mono WAV data.
/// Port of the reference C# `SpectrumBeeperWav` static class.
module SpectrumBeeperWav =

  /// Piecewise-integrate the beeper signal over `durationTicks` CPU cycles
  /// into `sampleRate` Hz 16-bit PCM. Events at or before a sample's start
  /// set its level; same-tick events apply last-wins; a 20 Hz DC blocker
  /// turns a constant beeper state into silence instead of a DC offset.
  let RenderPcm16
      (events: IReadOnlyList<BeeperEvent>)
      (durationTicks: int64)
      (cpuHz: double)
      (sampleRate: int)
      (volume: double)
      (initialState: bool)
      : int16[] =
    let sampleCount = int (Math.Ceiling(float durationTicks * float sampleRate / cpuHz))
    let pcm = Array.zeroCreate<int16> sampleCount

    let ticksPerSample = cpuHz / float sampleRate
    let gain = volume * 32767.0

    let mutable ev = 0
    let mutable state = initialState

    let level (s: bool) = if s then 1.0 else -1.0

    // Simple DC blocker/high-pass filter (20 Hz).
    let dcR = Math.Exp(-2.0 * Math.PI * 20.0 / float sampleRate)
    let mutable hpInitialized = false
    let mutable prevX = 0.0
    let mutable prevY = 0.0

    let mutable n = 0
    while n < sampleCount do
      let start = float n * ticksPerSample
      let finish = min (float durationTicks) ((float n + 1.0) * ticksPerSample)

      if finish > start then
        // Apply events that happen at or before the start of this sample.
        while ev < events.Count && events.[ev].Tick <= int64 start do
          let tick = events.[ev].Tick
          let mutable newState = events.[ev].State
          // If multiple events have the same tick, the last one wins.
          while ev < events.Count && events.[ev].Tick = tick do
            newState <- events.[ev].State
            ev <- ev + 1
          state <- newState

        let mutable pos = start
        let mutable acc = 0.0

        // Integrate the piecewise-constant beeper signal over this sample.
        while ev < events.Count && events.[ev].Tick < int64 finish do
          let eventTick = float events.[ev].Tick
          acc <- acc + level state * (eventTick - pos)
          let tick = events.[ev].Tick
          let mutable newState = events.[ev].State
          while ev < events.Count && events.[ev].Tick = tick do
            newState <- events.[ev].State
            ev <- ev + 1
          state <- newState
          pos <- eventTick

        acc <- acc + level state * (finish - pos)

        let mutable x = acc / (finish - start) // -1.0 to +1.0 approximately

        // Optional DC blocker.
        if hpInitialized then
          let y = x - prevX + dcR * prevY
          prevX <- x
          prevY <- y
          x <- y
        else
          prevX <- x
          prevY <- 0.0
          x <- 0.0
          hpInitialized <- true

        let v = int (Math.Round(x * gain))

        pcm.[n] <- int16 (max (int Int16.MinValue) (min (int Int16.MaxValue) v))

      n <- n + 1

    pcm

  /// Wrap 16-bit mono PCM in a canonical 44-byte RIFF/WAVE header.
  let WavFromPcm16Mono (samples: int16[]) (sampleRate: int) : byte[] =
    let channels = 1s
    let bitsPerSample = 16s
    let bytesPerSample = 2s

    let dataSize = samples.Length * int bytesPerSample
    let byteRate = sampleRate * int channels * int bytesPerSample
    let blockAlign = channels * bytesPerSample

    use ms = new MemoryStream(44 + dataSize)
    use bw = new BinaryWriter(ms, Encoding.ASCII)

    bw.Write(Encoding.ASCII.GetBytes "RIFF")
    bw.Write(36 + dataSize)
    bw.Write(Encoding.ASCII.GetBytes "WAVE")

    bw.Write(Encoding.ASCII.GetBytes "fmt ")
    bw.Write(16: int)           // PCM fmt chunk size
    bw.Write 1s                 // audio format: 1 = PCM
    bw.Write channels
    bw.Write sampleRate
    bw.Write byteRate
    bw.Write blockAlign
    bw.Write bitsPerSample

    bw.Write(Encoding.ASCII.GetBytes "data")
    bw.Write dataSize

    for s in samples do
      bw.Write s

    ms.ToArray()

  /// Render the event log to a complete WAV file. Defaults: 3.5 MHz CPU
  /// clock, 48 kHz mono 16-bit, 0.25 volume, beeper initially off.
  let RenderWav
      (events: IReadOnlyList<BeeperEvent>)
      (durationTicks: int64)
      (cpuHz: double)
      (sampleRate: int)
      (volume: double)
      (initialState: bool)
      : byte[] =
    let pcm = RenderPcm16 events durationTicks cpuHz sampleRate volume initialState
    WavFromPcm16Mono pcm sampleRate
