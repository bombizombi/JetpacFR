
import { class_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { op_Subtraction, compare, op_Addition, toUInt64_unchecked } from "../fable_modules/fable-library-js.5.17.0/BigInt.js";
import { item } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { clear, disposeSafe, getEnumerator } from "../fable_modules/fable-library-js.5.17.0/Util.js";

/**
 * A scheduler task. The `Run` function receives the absolute cycle count at
 * which it was scheduled to run.
 */
export class SchedulerTask {
    constructor(run) {
        this.run = run;
        this.scheduled = false;
    }
}

export function SchedulerTask_$reflection() {
    return class_type("Jetpac.Core.SchedulerTask", undefined, SchedulerTask);
}

export function SchedulerTask_$ctor_5D8E2020(run) {
    return new SchedulerTask(run);
}

export function SchedulerTask__get_Run(this$) {
    return this$.run;
}

export function SchedulerTask__get_Scheduled(this$) {
    return this$.scheduled;
}

export function SchedulerTask__set_Scheduled_Z1FBCCD16(this$, v) {
    this$.scheduled = v;
}

/**
 * Port of specbolt's Scheduler (z80/common/include/z80/common/Scheduler.hpp).
 * Tasks are kept in a list sorted ascending by absolute cycle; a task may only
 * be scheduled once at a time. Tick advances the cycle counter, running every
 * task whose scheduled cycle falls inside the tick window. The front task is
 * copied and erased BEFORE it runs, matching the C++ semantics.
 */
export class Scheduler {
    constructor() {
        this.cycles_ = (0n);
        this.tasks = [];
    }
}

export function Scheduler_$reflection() {
    return class_type("Jetpac.Core.Scheduler", undefined, Scheduler);
}

export function Scheduler_$ctor() {
    return new Scheduler();
}

/**
 * Schedule `task` to run `inCycles` cycles from now. No-op if already scheduled.
 */
export function Scheduler__Schedule_Z9FDE619(this$, task, inCycles) {
    if (!SchedulerTask__get_Scheduled(task)) {
        const whenToRun = toUInt64_unchecked(op_Addition(this$.cycles_, inCycles));
        let i = 0;
        while ((i < this$.tasks.length) && (compare(item(i, this$.tasks)[0], whenToRun) < 0)) {
            i = ((i + 1) | 0);
        }
        this$.tasks.splice(i, 0, [whenToRun, task]);
        SchedulerTask__set_Scheduled_Z1FBCCD16(task, true);
    }
}

/**
 * Advance the clock by `cycles` cycles, running due tasks.
 */
export function Scheduler__Tick_Z6EF827B6(this$, cycles) {
    const endCycle = toUInt64_unchecked(op_Addition(this$.cycles_, cycles));
    let continueLoop = true;
    while (continueLoop && (compare(this$.cycles_, endCycle) < 0)) {
        if (this$.tasks.length === 0) {
            continueLoop = false;
        }
        else {
            const patternInput = item(0, this$.tasks);
            const task = patternInput[1];
            const cycle = patternInput[0];
            if (compare(cycle, endCycle) <= 0) {
                SchedulerTask__set_Scheduled_Z1FBCCD16(task, false);
                this$.tasks.splice(0, 1);
                this$.cycles_ = cycle;
                SchedulerTask__get_Run(task)(cycle);
            }
            else {
                continueLoop = false;
            }
        }
    }
    this$.cycles_ = endCycle;
}

export function Scheduler__get_Cycles(this$) {
    return this$.cycles_;
}

/**
 * Clear every task and rewind the clock to `cycles` (used by
 * Spectrum48.LoadState to reconstruct the video schedule).
 */
export function Scheduler__Reset_Z6EF827B6(this$, cycles) {
    let enumerator = getEnumerator(this$.tasks);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            SchedulerTask__set_Scheduled_Z1FBCCD16(enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()[1], false);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    clear(this$.tasks);
    this$.cycles_ = cycles;
}

/**
 * Cycles until the next scheduled task (UInt64.MaxValue if the queue is empty).
 */
export function Scheduler__get_Headroom(this$) {
    if (this$.tasks.length === 0) {
        return 18446744073709551615n;
    }
    else {
        return toUInt64_unchecked(op_Subtraction(item(0, this$.tasks)[0], this$.cycles_));
    }
}

