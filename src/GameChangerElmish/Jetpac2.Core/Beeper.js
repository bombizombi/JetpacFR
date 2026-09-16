
import { item, length } from "../fable_modules/fable-library-js.5.17.2/List.js";
import { fromFloat64, op_Addition, toInt64_unchecked, compare } from "../fable_modules/fable-library-js.5.17.2/BigInt.js";
import { setItem } from "../fable_modules/fable-library-js.5.17.2/Array.js";

export const SampleRate = 44100;

export const SamplesPerFrame = 882;

export const TstatesPerSample = 69888 / SamplesPerFrame;

/**
 * Resample the transitions for the cycles in [startCycle, startCycle +
 * frameLength) into 882 float samples in [-1, 1]. The sample value is the
 * beeper level at the sample's start cycle; transitions at or before it
 * flip the level.
 */
export function ToSamples(trace, startCycle, frameLength) {
    const samples = new Float32Array(SamplesPerFrame);
    if (length(trace) === 0) {
        return samples;
    }
    else {
        let idx = 0;
        while (((idx + 1) < length(trace)) && (compare(item(idx + 1, trace)[0], startCycle) <= 0)) {
            idx = ((idx + 1) | 0);
        }
        let level = (compare(item(idx, trace)[0], startCycle) > 0) ? !item(idx, trace)[1] : item(idx, trace)[1];
        let next = idx + 1;
        let s = 0;
        while (s < SamplesPerFrame) {
            const sampleStart = toInt64_unchecked(op_Addition(startCycle, toInt64_unchecked(fromFloat64(s * TstatesPerSample))));
            while ((next < length(trace)) && (compare(item(next, trace)[0], sampleStart) <= 0)) {
                level = item(next, trace)[1];
                next = ((next + 1) | 0);
            }
            setItem(samples, s, level ? 0.800000011920929 : -0.800000011920929);
            s = ((s + 1) | 0);
        }
        return samples;
    }
}

