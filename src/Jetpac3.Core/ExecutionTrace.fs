namespace Jetpac3.Core

open System
open System.IO

/// Buffered writer for the oracle's instruction trace. The hot execution path
/// only appends events; file I/O is flushed at most once per second.
type ExecutionTraceWriter(path: string) =
    let pending = ResizeArray<string>()

    let writer =
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath path))
        |> ignore

        new StreamWriter(path, false)

    let mutable lastFlush = DateTime.UtcNow

    do writer.WriteLine("# tstate|address|opcode|nextpc|writes(address:old:new,...)")

    member this.Path = path

    member this.Append(events: Jetpac.Core.InstructionTraceEvent list) =
        for event in events do
            let writes =
                event.Writes
                |> List.map (fun write ->
                    sprintf "%04X:%02X:%02X" write.Address (int write.OldValue) (int write.NewValue))
                |> String.concat ","

            pending.Add(
                sprintf "%d|%04X|%02X|%04X|%s" event.TState event.Address (int event.Opcode) event.NextPc writes
            )

        if (DateTime.UtcNow - lastFlush).TotalSeconds >= 1.0 then
            this.Flush()

    member this.Flush() =
        for line in pending do
            writer.WriteLine line

        pending.Clear()
        writer.Flush()
        lastFlush <- DateTime.UtcNow

    member this.Dispose() =
        this.Flush()
        writer.Dispose()
