namespace JetpacFR.Desktop

open System
open System.IO

open System
open System.IO
open JetpacFR.Core
open System.Windows
open System.Windows.Controls
open System.Windows.Media

/// Per-game CE image registration shared by the launcher and the main
/// window: the launcher reports CE availability before the main window is
/// ever constructed, so registration lives here instead of in its ctor.
/// New game = new games/<id> fsproj + a line here.
module GameImages =
    let register () =
        GameRegistry.register
            { GameId = "minimal"
              Name = "Minimal"
              Memory = fst (MinimalGame.Game.entryState MinimalGame.Image.binary)
              BaseAddress = 0x8000
              Program = MinimalGame.Image.program
              EntryState = snd (MinimalGame.Game.entryState MinimalGame.Image.binary) }

/// Game-project discovery shared by the launcher window and the main
/// window's game combo.
module Projects =
    /// Walk up from the app base directory to the first folder containing a
    /// games/ directory (the manifests live at the repository root).
    let findGamesDir () =
        let rec walk (d: System.IO.DirectoryInfo) =
            let candidate = System.IO.Path.Combine(d.FullName, "games")

            if System.IO.Directory.Exists candidate then
                candidate
            else
                match d.Parent with
                | null -> ""
                | parent -> walk parent

        walk (System.IO.DirectoryInfo AppContext.BaseDirectory)

    /// All game manifests. With no reachable games/ folder the explicit
    /// Jetpac fallback keeps the app usable without manifests.
    let discover () : GameManifest list =
        let games = Manifest.discover (findGamesDir ())

        if List.isEmpty games then
            [ { GameId = "jetpac"
                ManifestPath = ""
                GameDirectory = Directory.GetCurrentDirectory()
                Name = "Jetpac"
                Default = true
                Rom = LocalAssets.find "48.rom"
                Tzx = LocalAssets.find "Jetpac.tzx"
                Boot = "auto"
                Script = None
                ProgramBin = None
                ProgramAddress = None } ]
        else
            games

    /// The project the app auto-loads on startup: the manifest flagged
    /// default, else Jetpac, else the first discovered project.
    let startupOf (games: GameManifest list) : GameManifest =
        match games |> List.tryFind (fun g -> g.Default) with
        | Some g -> g
        | None ->
            match games |> List.tryFind (fun g -> g.GameId = "jetpac") with
            | Some g -> g
            | None -> List.head games


/// The startup picker UI ("ZX Spectrum Game Changer"): the three open-with
/// actions stacked on top (button left, explanation right), the project
/// list below it (project name left, aligned status columns right), and
/// small Rescan / Exit buttons along the bottom edge. Pure view - every
/// action is a callback in LauncherContext, the shell owns the boot
/// pipeline.
module LauncherView =

    type LauncherContext =
        { OpenEmulator: GameManifest -> unit
          OpenCheatEngine: GameManifest -> unit
          OpenJustGame: GameManifest -> unit
          BootSlow: GameManifest -> unit
          SetTheme: Theme.Mode -> unit
          IsLight: unit -> bool }

    let build (ctx: LauncherContext) : DockPanel * (unit -> unit) =
        let bg = Theme.bg
        let panel = Theme.panel
        let normal = Theme.normal
        let dim = Theme.dim
        let green = Theme.green
        let mono = FontFamily("Consolas")

        let mutable selectedGame: GameManifest option = None
        let mutable games = Projects.discover ()
        let startupGame = Projects.startupOf games

        // ---- top: the three open-with actions, one below the other, each
        // with its explanation on the right
        let actions = StackPanel(Margin = Thickness(0.0, 0.0, 0.0, 12.0))

        let actionRow (caption: string) (explanation: string) (isDefault: bool) (onClick: GameManifest -> unit) =
            let row =
                StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 8.0))

            let b =
                Button(
                    Content = caption,
                    Width = 190.0,
                    Height = 42.0,
                    FontSize = 15.0,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    IsDefault = isDefault
                )

            b.Click.Add(fun _ ->
                match selectedGame with
                | Some g -> onClick g
                | None -> ())

            row.Children.Add b |> ignore

            let expl =
                TextBlock(
                    Text = explanation,
                    Foreground = dim,
                    FontSize = 12.0,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = Thickness(14.0, 0.0, 0.0, 0.0)
                )

            row.Children.Add expl |> ignore
            actions.Children.Add row |> ignore

        actionRow
            "Emulator"
            "Boot the selected project in the emulator window: boot to game entry, load its saved trace and autoplay it. (Enter, or double-click the project)"
            true
            ctx.OpenEmulator

        actionRow
            "Just the Game"
            "Shows only the Spectrum screen of the selected project - plus hold-to-play-in-reverse (button or F3). No emulator chrome."
            false
            ctx.OpenJustGame

        actionRow
            "Cheat Engine"
            "A separate window over the selected project: scan RAM for values, narrow with next scans, and poke the live machine."
            false
            ctx.OpenCheatEngine

        // ---- project list: name left, fixed-width right-aligned status
        // columns right, so trace sizes line up one below the other
        let statusCell (parent: #Panel) (width: float) (text: string) (ok: bool) =
            let tb =
                TextBlock(
                    Text = text,
                    Width = width,
                    TextAlignment = TextAlignment.Right,
                    Foreground = (if ok then green else dim),
                    FontFamily = mono,
                    FontSize = 12.0,
                    VerticalAlignment = VerticalAlignment.Center
                )

            parent.Children.Add tb |> ignore

        let buildRow (g: GameManifest) =
            let border =
                Border(
                    Background = panel,
                    CornerRadius = CornerRadius(6.0),
                    Padding = Thickness(12.0, 8.0, 12.0, 8.0),
                    Margin = Thickness(4.0)
                )

            let grid = Grid()
            grid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
            grid.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength.Auto))

            let nameCol = StackPanel()

            nameCol.Children.Add(
                TextBlock(Text = g.Name, Foreground = normal, FontSize = 15.0, FontWeight = FontWeights.SemiBold)
            )
            |> ignore

            let sub =
                (sprintf "(%s)" g.GameId) + (if g.Default then "  -  auto-load default" else "")

            nameCol.Children.Add(TextBlock(Text = sub, Foreground = dim, FontSize = 11.5))
            |> ignore

            Grid.SetColumn(nameCol, 0)
            grid.Children.Add nameCol |> ignore

            let status = StackPanel(Orientation = Orientation.Horizontal)
            let slots = TimelineSlots.list g.GameDirectory
            let hasKeys = FileInfo(ReplayStore.path g).Exists
            let hasCtrl = FileInfo(Path.Combine(g.GameDirectory, "control.json")).Exists
            let hasCE = GameRegistry.tryFind g.GameId |> Option.isSome

            if List.isEmpty slots then
                statusCell status 110.0 "traces: none" false
            else
                let totalMB = slots |> List.sumBy (fun s -> float s.Bytes / (1024.0 * 1024.0))
                statusCell status 110.0 (sprintf "traces: %d (%.1f MB)" slots.Length totalMB) true

            statusCell status 82.0 (sprintf "keys: %s" (if hasKeys then "yes" else "no")) hasKeys

            statusCell
                status
                106.0
                (sprintf "control: %s" (if hasCtrl then "yes" else "no"))
                hasCtrl

            statusCell status 86.0 (sprintf "CE: %s" (if hasCE then "yes" else "no")) hasCE

            statusCell status 92.0 (sprintf "boot: %s" g.Boot) true
            Grid.SetColumn(status, 1)
            grid.Children.Add status |> ignore
            border.Child <- grid
            ListBoxItem(Content = border, Tag = g, Padding = Thickness(2.0))

        let root =
            DockPanel(Margin = Thickness(20.0), MaxWidth = 1180.0, HorizontalAlignment = HorizontalAlignment.Center)

        let header = StackPanel(Margin = Thickness(0.0, 0.0, 0.0, 14.0))

        header.Children.Add(
            TextBlock(
                Text = "ZX Spectrum Game Changer",
                Foreground = normal,
                FontSize = 26.0,
                FontWeight = FontWeights.Bold
            )
        )
        |> ignore

        header.Children.Add(
            TextBlock(
                Text =
                    "Pick a project, then choose how to open it: the emulator, the bare game view, or the cheat engine.",
                Foreground = dim,
                FontSize = 13.0,
                Margin = Thickness(0.0, 6.0, 0.0, 0.0),
                TextWrapping = TextWrapping.Wrap
            )
        )
        |> ignore

        DockPanel.SetDock(header, Dock.Top)
        root.Children.Add header |> ignore
        DockPanel.SetDock(actions, Dock.Top)
        root.Children.Add actions |> ignore

        let list = ListBox(Background = panel, BorderThickness = Thickness(0.0))

        let refill () =
            list.Items.Clear()

            for g in games do
                list.Items.Add(buildRow g) |> ignore

        let gameAt (index: int) : GameManifest option =
            match list.Items[index] with
            | :? ListBoxItem as it -> Some(it.Tag :?> GameManifest)
            | _ -> None

        /// Re-read the games folder and rebuild the list, keeping the user's
        /// selection: the previously chosen game stays selected while it
        /// exists (returning from the emulator lands back on it), and only a
        /// vanished project falls back to the startup default. The capture
        /// must happen before refill - Items.Clear resets the selection.
        let rescanNow () =
            let keep = selectedGame
            games <- Projects.discover ()
            refill ()

            let preferred =
                match keep with
                | Some s when games |> List.exists (fun g -> g.GameId = s.GameId) -> s.GameId
                | _ -> (Projects.startupOf games).GameId

            match games |> List.tryFindIndex (fun g -> g.GameId = preferred) with
            | Some i -> list.SelectedIndex <- i
            | None -> ()

        list.SelectionChanged.Add(fun _ ->
            selectedGame <-
                (if list.SelectedIndex >= 0 then
                     gameAt list.SelectedIndex
                 else
                     None))

        list.MouseDoubleClick.Add(fun _ ->
            match selectedGame with
            | Some g -> ctx.OpenEmulator g
            | None -> ())

        rescanNow ()

        // ---- bottom edge: small Rescan (left) and Exit (right)
        let bottom = Grid(Margin = Thickness(0.0, 12.0, 0.0, 0.0))
        bottom.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
        bottom.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))

        let rescan =
            Button(
                Content = "Rescan",
                Width = 84.0,
                Height = 24.0,
                HorizontalAlignment = HorizontalAlignment.Left,
                ToolTip = "re-read the games folder: picks up newly added projects, traces and status changes"
            )

        rescan.Click.Add(fun _ -> rescanNow ())

        // Import a tape file (.tzx) that has no games/<id> project yet: copy it
        // into a fresh games/<id>/ folder with a manifest (rom = the shared
        // 48.rom, boot = auto) and rescan, so the game joins the list without
        // any hand-written project files.
        let importGame () =
            let gamesDir = Projects.findGamesDir ()

            if gamesDir = "" then
                System.Windows.MessageBox.Show(
                    "no games/ folder found next to the app - cannot import",
                    "Add game"
                )
                |> ignore
            else
                let dlg =
                    Microsoft.Win32.OpenFileDialog(
                        Title = "Add game - pick a TZX tape",
                        Filter = "ZX Spectrum tape (*.tzx)|*.tzx|All files (*.*)|*.*"
                    )

                if dlg.ShowDialog() = Nullable<bool>(true) then
                    try
                        let srcFile = dlg.FileName
                        let baseName = IO.Path.GetFileNameWithoutExtension srcFile

                        let id =
                            System.String(
                                baseName.ToLowerInvariant().ToCharArray()
                                |> Array.filter System.Char.IsLetterOrDigit
                            )

                        let id = if id = "" then "game" else id
                        let rom = LocalAssets.find "48.rom" // throws when assets/ is unreachable
                        let mutable dir = IO.Path.Combine(gamesDir, id)
                        let mutable n = 1

                        while Directory.Exists dir do
                            dir <- IO.Path.Combine(gamesDir, sprintf "%s%d" id n)
                            n <- n + 1

                        Directory.CreateDirectory dir |> ignore
                        let tapeName = IO.Path.GetFileName srcFile
                        File.Copy(srcFile, IO.Path.Combine(dir, tapeName))
                        // The manifest sits in games/<id>/, so the shared ROM is
                        // two levels up: ../../assets/<rom>.
                        let romRel = sprintf "../../assets/%s" (IO.Path.GetFileName rom)

                        let json =
                            sprintf
                                "{\n  \"name\": %s,\n  \"rom\": %s,\n  \"tzx\": %s,\n  \"boot\": \"auto\"\n}"
                                (System.Text.Json.JsonSerializer.Serialize baseName)
                                (System.Text.Json.JsonSerializer.Serialize romRel)
                                (System.Text.Json.JsonSerializer.Serialize tapeName)

                        IO.File.WriteAllText(IO.Path.Combine(dir, "manifest.json"), json)
                        rescanNow ()
                    with ex ->
                        System.Windows.MessageBox.Show(
                            sprintf "import failed: %s" ex.Message,
                            "Add game"
                        )
                        |> ignore

        let addGame =
            Button(
                Content = "Add game…",
                Width = 96.0,
                Height = 24.0,
                Margin = Thickness(8.0, 0.0, 0.0, 0.0),
                ToolTip =
                    "import a .tzx tape as a new project: creates games/<id> with a manifest (boot: auto, shared 48K rom) and rescans the list"
            )

        addGame.Click.Add(fun _ -> importGame ())

        let leftBox =
            StackPanel(Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left)

        Grid.SetColumn(leftBox, 0)
        bottom.Children.Add leftBox |> ignore
        leftBox.Children.Add rescan |> ignore
        leftBox.Children.Add addGame |> ignore

        // The deliberately small alternative boot path: emulate the real tape
        // instead of the flash loader (for custom loaders the trap cannot
        // serve, or for authenticity).
        let slowBoot =
            Button(
                Content = "Slow tape boot",
                Width = 112.0,
                Height = 24.0,
                Margin = Thickness(8.0, 0.0, 0.0, 0.0),
                ToolTip =
                    "open the selected game by emulating the real tape load (the flash loader is skipped; ~40 s the first time, cached afterwards)"
            )

        slowBoot.Click.Add(fun _ ->
            match selectedGame with
            | Some g -> ctx.BootSlow g
            | None -> ())

        leftBox.Children.Add slowBoot |> ignore

        let themeToggle =
            CheckBox(
                Content = "Light theme",
                IsChecked = Nullable<bool>(ctx.IsLight()),
                Foreground = dim,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(12.0, 0.0, 0.0, 0.0),
                ToolTip = "day/night UI theme (saved to gui.cfg)"
            )

        themeToggle.Checked.Add(fun _ -> ctx.SetTheme Theme.Light)
        themeToggle.Unchecked.Add(fun _ -> ctx.SetTheme Theme.Dark)
        leftBox.Children.Add themeToggle |> ignore

        let exit =
            Button(
                Content = "Exit",
                Width = 64.0,
                Height = 24.0,
                HorizontalAlignment = HorizontalAlignment.Right,
                ToolTip =
                    "close the app without opening a project (no recording is lost: timelines are only written by the emulator)"
            )

        exit.Click.Add(fun _ ->
            match Window.GetWindow root with
            | null -> ()
            | w -> w.Close())

        Grid.SetColumn(exit, 1)
        bottom.Children.Add exit |> ignore
        DockPanel.SetDock(bottom, Dock.Bottom)
        root.Children.Add bottom |> ignore

        root.Children.Add list |> ignore

        root, rescanNow
