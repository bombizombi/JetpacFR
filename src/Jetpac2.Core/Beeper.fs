namespace Jetpac2.Core

/// Beeper waveform synthesis from the per-frame (cycle, level) trace.
/// Model: one game frame is 69888 T-states (312 scanlines x 224); at 50Hz
/// that is 882 output samples per frame at 44100Hz.
module Beeper =

    let SampleRate = 44100
    let SamplesPerFrame = 882
    let TstatesPerSample = 69888.0 / float SamplesPerFrame

    /// Resample the transitions for the cycles in [startCycle, startCycle +
    /// frameLength) into 882 float samples in [-1, 1]. The sample value is the
    /// beeper level at the sample's start cycle; transitions at or before it
    /// flip the level.
    let ToSamples (trace: (int64 * bool) list) (startCycle: int64) (frameLength: int64) : float32[] =
        let samples = Array.zeroCreate<float32> SamplesPerFrame

        if trace.Length = 0 then
            samples
        else
            // Initial level: the last transition at or before startCycle. When the
            // first transition is still ahead of startCycle (the normal drain
            // case), the sounding level is the opposite of that transition's new
            // value - transitions record the level they switch TO.
            let mutable idx = 0

            while idx + 1 < trace.Length && (fst trace[idx + 1]) <= startCycle do
                idx <- idx + 1

            let mutable level =
                if (fst trace[idx]) > startCycle then
                    not (snd trace[idx])
                else
                    snd trace[idx]

            let mutable next = idx + 1
            let mutable s = 0

            while s < SamplesPerFrame do
                let sampleStart = startCycle + int64 (float s * TstatesPerSample)

                while next < trace.Length && (fst trace[next]) <= sampleStart do
                    level <- snd trace[next]
                    next <- next + 1

                samples[s] <- (if level then 0.8f else -0.8f)
                s <- s + 1

            samples
