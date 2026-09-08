"""Replace the picker do-block in App.fs with the LauncherView-based version."""
import io

APP = "src/JetpacFR.Desktop/App.fs"
with io.open(APP, encoding="utf-8-sig") as f:
    lines = f.read().split("\n")

start = next(i for i, l in enumerate(lines) if l.strip() == "GameImages.register () // CE availability for the status chips") - 1
end = next(i for i, l in enumerate(lines) if l.strip() == "self.Content <- root" and i > start)

new_block = '''    do
        GameImages.register () // CE availability for the status chips
        self.Title <- "ZX Spectrum Game Changer"
        // One size for the whole app lifetime: the emulator layout needs this
        // room, and neither phase ever resizes the window.
        self.Width <- 1560.0
        self.Height <- 900.0
        self.WindowStartupLocation <- WindowStartupLocation.CenterScreen
        self.Background <- bg

        // Open a project per the chosen phase: "emulator" swaps the full
        // emulator UI in, "game" swaps in the bare game view (screen +
        // reverse playback) so the emulator chrome never shows.
        let openProject (g: GameManifest) =
            menuOpen <- false

            match emulatorRoot with
            | None -> self.BuildEmulator g
            | Some r ->
                (match currentGame with
                 | Some cur when cur.GameId <> g.GameId -> gameCombo.SelectedItem <- g
                 | _ -> ())

                if launchPhase = "game" then
                    self.ShowGameOnly()
                else
                    self.Title <- "JetpacFR - Game Changer"
                    self.Content <- r

                refreshControlLists ()

        let ctx: LauncherView.LauncherContext =
            { OpenEmulator =
                  fun g ->
                      launchPhase <- "emulator"
                      openProject g
              OpenCheatEngine =
                  fun g ->
                      launchPhase <- "emulator"
                      openProject g
                      self.OpenCheatEngine()
              OpenJustGame =
                  fun g ->
                      launchPhase <- "game"
                      openProject g }

        let root, refreshLauncher = LauncherView.build ctx
        pickerRoot <- root
        refreshPicker <- refreshLauncher
        self.Content <- root'''

lines[start:end + 1] = new_block.split("\n")

with io.open(APP, "w", encoding="utf-8", newline="") as f:
    f.write("\n".join(lines))

print("picker do-block replaced (was lines", start + 1, "to", end + 1, ")")
