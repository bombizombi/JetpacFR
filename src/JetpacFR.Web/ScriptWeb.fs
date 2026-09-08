namespace Jetpac3.Core

/// WEB SHELL SHIM (JetpacFR.Web): Jetpac3.Core.Script.defaultSession verbatim
/// from Jetpac3.Core/Session.fs (the rest of that file is file-IO bound and
/// not compiled here).

module Script =
    let defaultSession (framesN: int) : (int * int * int * bool) list =
        let script =
            [ (10, 30, (3, 0)) // press "1" (start 1-player game)
              (35, 50, (3, 4)) // hold 5 (right)
              (90, 110, (3, 4)) // 5 again
              (120, 135, (4, 2)) // hold 8 (left)
              (160, 165, (7, 0)) // fire (space)
              (200, 220, (7, 0)) // fire
              (260, 275, (0, 0)) // up (CAPS SHIFT + 7): the shift...
              (260, 275, (4, 3)) // ...and the 7
              (300, 315, (0, 0)) // up
              (300, 315, (4, 3)) // ...and the 7
              (350, 365, (4, 4)) // down (6)
              (400, 420, (7, 0)) ] // fire

        [ for (press, release, (row, bit)) in script do
              if release <= framesN then
                  yield (press, row, bit, true)
                  yield (release, row, bit, false) ]
