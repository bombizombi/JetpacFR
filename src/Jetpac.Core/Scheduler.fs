namespace Jetpac.Core

/// A scheduler task. The `Run` function receives the absolute cycle count at
/// which it was scheduled to run.
type SchedulerTask(run: uint64 -> unit) =
  let mutable scheduled = false
  member this.Run = run
  member this.Scheduled
    with get () = scheduled
    and set v = scheduled <- v

/// Port of specbolt's Scheduler (z80/common/include/z80/common/Scheduler.hpp).
/// Tasks are kept in a list sorted ascending by absolute cycle; a task may only
/// be scheduled once at a time. Tick advances the cycle counter, running every
/// task whose scheduled cycle falls inside the tick window. The front task is
/// copied and erased BEFORE it runs, matching the C++ semantics.
type Scheduler() =
  let mutable cycles_ : uint64 = 0UL
  let tasks = System.Collections.Generic.List<uint64 * SchedulerTask>()

  /// Schedule `task` to run `inCycles` cycles from now. No-op if already scheduled.
  member this.Schedule(task: SchedulerTask, inCycles: uint64) =
    if not task.Scheduled then
      let whenToRun = cycles_ + inCycles
      let mutable i = 0
      while i < tasks.Count && (fst tasks.[i]) < whenToRun do
        i <- i + 1
      tasks.Insert(i, (whenToRun, task))
      task.Scheduled <- true

  /// Advance the clock by `cycles` cycles, running due tasks.
  member this.Tick(cycles: uint64) =
    let endCycle = cycles_ + cycles
    let mutable continueLoop = true
    while continueLoop && cycles_ < endCycle do
      if tasks.Count = 0 then
        continueLoop <- false
      else
        let (cycle, task) = tasks.[0]
        if cycle <= endCycle then
          task.Scheduled <- false
          tasks.RemoveAt(0)
          cycles_ <- cycle
          task.Run cycle
        else
          continueLoop <- false
    cycles_ <- endCycle

  member this.Cycles = cycles_

  /// Clear every task and rewind the clock to `cycles` (used by
  /// Spectrum48.LoadState to reconstruct the video schedule).
  member this.Reset(cycles: uint64) =
    for (_, t) in tasks do
      t.Scheduled <- false
    tasks.Clear()
    cycles_ <- cycles

  /// Cycles until the next scheduled task (UInt64.MaxValue if the queue is empty).
  member this.Headroom =
    if tasks.Count = 0 then System.UInt64.MaxValue
    else (fst tasks.[0]) - cycles_
