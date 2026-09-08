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


/// The startup picker UI ("ZX Spectrum Game Changer"): project rows with
/// status chips plus the open-with buttons (Emulator / Cheat Engine /
/// Just the Game). Pure view - every action is a callback in
/// LauncherContext, the shell owns the boot pipeline.
module LauncherView =

    type LauncherContext =
        { OpenEmulator: GameManifest -> unit
          OpenCheatEngine: GameManifest -> unit
          OpenJustGame: GameManifest -> unit }

    let build (ctx: LauncherContext) : DockPanel * (unit -> unit) =
        let bg = SolidColorBrush(Color.FromRgb(0x10uy, 0x10uy, 0x16uy))
        let panel = SolidColorBrush(Color.FromRgb(0x18uy, 0x1Cuy, 0x24uy))
        let normal = SolidColorBrush(Color.FromRgb(0xC8uy, 0xC8uy, 0xCEuy))
        let dim = SolidColorBrush(Color.FromRgb(0x8Auy, 0x8Auy, 0x92uy))
        let green = SolidColorBrush(Color.FromRgb(0x4Euy, 0xE0uy, 0x60uy))
        let mono = FontFamily("Consolas")

        let mutable selectedGame: GameManifest option = None
        let mutable games = Projects.discover ()
        let startupGame = Projects.startupOf games

        let chip (parent: #Panel) (text: string) (present: bool) =
            let tb =
                TextBlock(
                    Text = text,
                    Foreground = (if present then green else dim),
                    FontFamily = mono,
                    FontSize = 12.0,
                    Margin = Thickness(0.0, 0.0, 18.0, 0.0),
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

            let col = StackPanel()

            let title =
                sprintf "%s   (%s)" g.Name g.GameId
                |> fun s -> if g.Default then s + "  - auto-load default" else s

            col.Children.Add(
                TextBlock(
                    Text = title,
                    Foreground = normal,
                    FontSize = 16.0,
                    FontWeight = FontWeights.SemiBold,
                    Margin = Thickness(0.0, 0.0, 0.0, 6.0)
                )
            )
            |> ignore

            let status = StackPanel(Orientation = Orientation.Horizontal)
            let tracePath = Path.Combine(g.GameDirectory, StateTimelineStore.FileName)
            let fi = FileInfo(tracePath)

            if fi.Exists then
                chip status (sprintf "trace: %.1f MB" (float fi.Length / (1024.0 * 1024.0))) true
            else
                chip status "trace: none" false

            chip
                status
                (sprintf "key script: %s" (if FileInfo(ReplayStore.path g).Exists then "yes" else "no"))
                (FileInfo(ReplayStore.path g).Exists)

            chip
                status
                (sprintf
                    "control file: %s"
                    (if FileInfo(Path.Combine(g.GameDirectory, "control.json")).Exists then
                         "yes"
                     else
                         "no"))
                (FileInfo(Path.Combine(g.GameDirectory, "control.json")).Exists)

            chip
                status
                (sprintf
                    "CE program: %s"
                    (if GameRegistry.tryFind g.GameId |> Option.isSome then
                         "yes"
                     else
                         "no"))
                (GameRegistry.tryFind g.GameId |> Option.isSome)

            chip status (sprintf "boot: %s" g.Boot) true
            col.Children.Add status |> ignore
            border.Child <- col
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
                    "Pick a project, then choose how to open it: the emulator, the cheat engine, or the bare game view.",
                Foreground = dim,
                FontSize = 13.0,
                Margin = Thickness(0.0, 6.0, 0.0, 0.0),
                TextWrapping = TextWrapping.Wrap
            )
        )
        |> ignore

        DockPanel.SetDock(header, Dock.Top)
        root.Children.Add header |> ignore

        let list = ListBox(Background = panel, BorderThickness = Thickness(0.0))

        let refill () =
            list.Items.Clear()

            for g in games do
                list.Items.Add(buildRow g) |> ignore

        let gameAt (index: int) : GameManifest option =
            match list.Items[index] with
            | :? ListBoxItem as it -> Some(it.Tag :?> GameManifest)
            | _ -> None

        let reselectDefault () =
            let def = Projects.startupOf games
            list.SelectedIndex <- games |> List.findIndex (fun g -> g.GameId = def.GameId)

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

        refill ()
        reselectDefault ()

        let btnRow = Grid(Margin = Thickness(0.0, 14.0, 0.0, 0.0))

        for i in 0..4 do
            btnRow.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))

        let buttonColumn (col: int) (caption: string) (explanation: string) (isDefault: bool) (onClick: unit -> unit) =
            let stack =
                StackPanel(Margin = Thickness((if col = 0 then 0.0 else 8.0), 0.0, 0.0, 0.0))

            Grid.SetColumn(stack, col)

            let b =
                Button(
                    Content = caption,
                    Height = 52.0,
                    MinWidth = 180.0,
                    FontSize = 15.0,
                    FontWeight = FontWeights.SemiBold,
                    IsDefault = isDefault
                )

            b.Click.Add(fun _ -> onClick ())
            stack.Children.Add b |> ignore

            stack.Children.Add(
                TextBlock(
                    Text = explanation,
                    Foreground = dim,
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = Thickness(2.0, 5.0, 2.0, 0.0)
                )
            )
            |> ignore

            btnRow.Children.Add stack |> ignore

        buttonColumn
            0
            "Emulator"
            "Boot the selected project in the emulator window: boot to game entry, load its saved trace and autoplay it. (Enter, or double-click the row)"
            true
            (fun () ->
                match selectedGame with
                | Some g -> ctx.OpenEmulator g
                | None -> ())

        buttonColumn
            1
            "Cheat Engine"
            "Opens the project first (if needed), then a cheat-engine window over it: scan RAM for values, narrow with next scans, and poke the live machine."
            false
            (fun () ->
                match selectedGame with
                | Some g -> ctx.OpenCheatEngine g
                | None -> ())

        buttonColumn
            2
            "Just the Game"
            "Opens the project first (if needed), then shows only the Spectrum screen - plus hold-to-play-in-reverse (button or F3)."
            false
            (fun () ->
                match selectedGame with
                | Some g -> ctx.OpenJustGame g
                | None -> ())

        buttonColumn
            3
            "Rescan"
            "Re-read the games folder: picks up newly added projects, traces and status changes without restarting the app."
            false
            (fun () ->
                games <- Projects.discover ()
                refill ()
                reselectDefault ())

        buttonColumn
            4
            "Exit"
            "Close the app without opening a project. No recording is lost: timelines are only written by the emulator."
            false
            (fun () ->
                match Window.GetWindow root with
                | null -> ()
                | w -> w.Close())

        DockPanel.SetDock(btnRow, Dock.Bottom)
        root.Children.Add btnRow |> ignore
        root.Children.Add list |> ignore

        root,
        (fun () ->
            games <- Projects.discover ()
            refill ()
            reselectDefault ())
