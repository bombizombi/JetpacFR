namespace Jetpac3.Core

/// A deterministic routine-entry corpus case.
type DifferentialCase =
    { Name: string
      CheckpointId: string
      Frames: int
      Script: (int * int * int * bool) list }

type DifferentialResult =
    { Name: string
      Passed: bool
      FirstFrame: int
      Message: string
      MaxTimingDifference: int64 }

module DifferentialCorpus =
    let run (session: Session) (cases: DifferentialCase list) : DifferentialResult list =
        cases
        |> List.map (fun testCase ->
            try
                session.RestoreCheckpoint testCase.CheckpointId
                let report = session.RunDiff(testCase.Frames, testCase.Script)

                { Name = testCase.Name
                  Passed = report.Frame < 0
                  FirstFrame = report.Frame
                  Message = report.Message
                  MaxTimingDifference = 0L }
            with ex ->
                { Name = testCase.Name
                  Passed = false
                  FirstFrame = -1
                  Message = ex.Message
                  MaxTimingDifference = 0L })

    let expand cases =
        cases
        |> List.collect (fun testCase ->
            let mutations =
                [ for bit in 0..4 do
                      { testCase with
                          Name = sprintf "%s-key%d" testCase.Name bit
                          Script = testCase.Script @ [ (0, 7, bit, true); (1, 7, bit, false) ] } ]

            testCase :: mutations)

    let allPassed results =
        results |> List.forall (fun result -> result.Passed)
