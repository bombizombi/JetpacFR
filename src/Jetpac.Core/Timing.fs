namespace Jetpac2.Core

/// Port of JetpacFSharp (Jetpac.Core) Scheduler — specbolt's sorted task
/// list, with 64-bit cycle counts so long browser sessions never wrap.
type SchedulerTask(run: int64 -> unit) =
    let mutable scheduled = false
    member this.Run = run

    member this.Scheduled
        with get () = scheduled
        and set v = scheduled <- v

/// Tasks kept in a list sorted ascending by absolute cycle; a task may only
/// be scheduled once at a time. Tick advances the cycle counter, running
/// every task whose scheduled cycle falls inside the tick window. The front
/// task is copied and erased BEFORE it runs, matching specbolt semantics.
type Scheduler() =
    let mutable cycles_ = 0L
    let tasks = System.Collections.Generic.List<int64 * SchedulerTask>()

    member this.Schedule(task: SchedulerTask, inCycles: int64) =
        if not task.Scheduled then
            let whenToRun = cycles_ + inCycles
            let mutable i = 0

            while i < tasks.Count && (fst tasks[i]) < whenToRun do
                i <- i + 1

            tasks.Insert(i, (whenToRun, task))
            task.Scheduled <- true

    member this.Tick(cycles: int64) =
        let endCycle = cycles_ + cycles
        let mutable continueLoop = true

        while continueLoop && cycles_ < endCycle do
            if tasks.Count = 0 then
                continueLoop <- false
            else
                let (cycle, task) = tasks[0]

                if cycle <= endCycle then
                    task.Scheduled <- false
                    tasks.RemoveAt(0)
                    cycles_ <- cycle
                    task.Run cycle
                else
                    continueLoop <- false

        cycles_ <- endCycle

    member this.Cycles = cycles_

    /// Clear every task and rewind the clock (used by Machine.LoadState).
    member this.Reset(cycles: int64) =
        for (_, t) in tasks do
            t.Scheduled <- false

        tasks.Clear()
        cycles_ <- cycles
