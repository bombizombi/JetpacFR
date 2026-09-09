namespace JetpacFR.Desktop

open System
open System.IO
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Threading

/// Cheat Engine: a phase of the main window (no second window) over the
/// LIVE session (shared state - the accessors hand out the machine's own
/// memory and screen buffer, not copies, so pokes hit the running game).
/// Left: the Spectrum screen, refreshed live. Right: a memory scanner with
/// the classic first-scan / narrow / poke workflow over RAM ($4000-$FFFF),
/// byte or 16-bit word values. Escape (or the main-menu button) returns to
/// the launcher via the onExit callback.
type CheatEngineView
    (
        getScreen: unit -> byte[] option,
        getMemory: unit -> byte[] option,
        onExit: unit -> unit
    ) as self =
    inherit Grid()

    let bg = SolidColorBrush(Color.FromRgb(0x10uy, 0x10uy, 0x16uy))
    let panel = SolidColorBrush(Color.FromRgb(0x18uy, 0x1Cuy, 0x24uy))
    let dim = SolidColorBrush(Color.FromRgb(0x8Auy, 0x8Auy, 0x92uy))
    let green = SolidColorBrush(Color.FromRgb(0x4Euy, 0xE0uy, 0x60uy))
    let red = SolidColorBrush(Color.FromRgb(0xE8uy, 0x54uy, 0x54uy))
    let mono = FontFamily("Consolas")

    let mutable results: int list = []

    let screenBmp = WriteableBitmap(320, 256, 96.0, 96.0, PixelFormats.Bgra32, null)
    let screenImg = Image(Source = screenBmp, Width = 320.0, Height = 256.0)

    do RenderOptions.SetBitmapScalingMode(screenImg, BitmapScalingMode.NearestNeighbor)

    let resultsList = ListBox(Background = panel, FontFamily = mono, FontSize = 12.0)

    let countLabel =
        TextBlock(Foreground = dim, FontSize = 11.0, Margin = Thickness(8.0, 0.0, 0.0, 0.0))

    let scanType = ComboBox(Width = 110.0, VerticalAlignment = VerticalAlignment.Center)
    let valueBox = TextBox(Width = 90.0)
    let pokeBox = TextBox(Width = 90.0)

    let statusLabel =
        TextBlock(Foreground = dim, FontSize = 11.5, Margin = Thickness(0.0, 4.0, 0.0, 0.0), MinHeight = 18.0)

    let setStatus (text: string) (brush: SolidColorBrush) =
        statusLabel.Text <- text
        statusLabel.Foreground <- brush

    let parseValue (s: string) : int option =
        let t = s.Trim().ToLowerInvariant()

        if t.StartsWith("0x") || t.StartsWith("$") then
            let h = if t.StartsWith "0x" then t.Substring 2 else t.Substring 1

            match
                Int32.TryParse(h, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture)
            with
            | true, v when v >= 0 && v <= 0xFFFF -> Some v
            | _ -> None
        else
            match Int32.TryParse t with
            | true, v when v >= 0 && v <= 0xFFFF -> Some v
            | _ -> None

    let isWord () : bool =
        (unbox<string> scanType.SelectedItem).StartsWith "Word"

    let valueAt (mem: byte[]) (a: int) : int =
        if isWord () then
            (int mem[a &&& 0xFFFF]) ||| (int mem[(a + 1) &&& 0xFFFF] <<< 8)
        else
            int mem[a]

    let showResults () =
        resultsList.Items.Clear()

        match getMemory () with
        | Some mem ->
            for a in List.truncate 500 results do
                resultsList.Items.Add(sprintf "$%04X   %d" a (valueAt mem a)) |> ignore
        | None -> ()

        countLabel.Text <-
            sprintf
                "%d matches%s"
                (List.length results)
                (if List.length results > 500 then
                     "  (showing first 500)"
                 else
                     "")

    let doScan (first: bool) : unit =
        match getMemory () with
        | None -> setStatus "no live machine - open a project in the main window first" red
        | Some mem ->
            match parseValue valueBox.Text with
            | None -> setStatus "enter a value first (decimal, or 0x/$ for hex)" red
            | Some v ->
                if first then
                    let hi = if isWord () then 0xFFFE else 0xFFFF

                    results <-
                        [ for a in 0x4000..hi do
                              if valueAt mem a = v then
                                  a ]
                else
                    results <- results |> List.filter (fun a -> valueAt mem a = v)

                showResults ()

                setStatus
                    (if List.isEmpty results then
                         "no matches - try Next scan with a new value, or Reset"
                     else
                         sprintf "scan done at $%04X-$%04X" 0x4000 0xFFFF)
                    green

    let doPoke () : unit =
        match resultsList.SelectedIndex with
        | i when i >= 0 && i < List.length results ->
            match parseValue pokeBox.Text with
            | None -> setStatus "poke: enter a value first" red
            | Some v ->
                match getMemory () with
                | None -> setStatus "no live machine" red
                | Some mem ->
                    let addr = List.item i results

                    if isWord () then
                        mem[addr &&& 0xFFFF] <- byte (v &&& 0xFF)
                        mem[(addr + 1) &&& 0xFFFF] <- byte ((v >>> 8) &&& 0xFF)
                    else
                        mem[addr &&& 0xFFFF] <- byte (v &&& 0xFF)

                    setStatus (sprintf "poked $%04X" addr) green
        | _ -> setStatus "select a result row first" red

    let resetScan () : unit =
        results <- []
        resultsList.Items.Clear()
        countLabel.Text <- ""
        setStatus "scan reset" dim

    do
        scanType.Items.Add("Byte (8-bit)") |> ignore
        scanType.Items.Add("Word (16-bit)") |> ignore
        scanType.SelectedIndex <- 0

        let menuBtn =
            Button(Content = "<- main menu", Width = 110.0, HorizontalAlignment = HorizontalAlignment.Left)

        menuBtn.Click.Add(fun _ -> onExit ())

        let screenTitle =
            TextBlock(
                Text = "Spectrum (live)",
                Foreground = dim,
                FontSize = 12.0,
                Margin = Thickness(0.0, 0.0, 0.0, 6.0)
            )

        let hint =
            TextBlock(
                Text =
                    "the machine may run or be paused while scanning. Pokes write the live machine memory. Escape returns to the main menu.",
                Foreground = dim,
                FontSize = 11.0,
                TextWrapping = TextWrapping.Wrap,
                Margin = Thickness(0.0, 10.0, 0.0, 0.0)
            )

        let left = StackPanel(Margin = Thickness(0.0, 0.0, 16.0, 0.0))
        left.Children.Add menuBtn |> ignore
        left.Children.Add screenTitle |> ignore
        left.Children.Add screenImg |> ignore
        left.Children.Add hint |> ignore

        let scanRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 6.0))

        let mkBtn (text: string) (onClick: unit -> unit) =
            let b =
                Button(Content = text, MinWidth = 92.0, Margin = Thickness(0.0, 0.0, 6.0, 0.0))

            b.Click.Add(fun _ -> onClick ())
            b

        scanRow.Children.Add scanType |> ignore
        scanRow.Children.Add valueBox |> ignore
        scanRow.Children.Add(mkBtn "First scan" (fun () -> doScan true)) |> ignore
        scanRow.Children.Add(mkBtn "Next scan" (fun () -> doScan false)) |> ignore
        scanRow.Children.Add(mkBtn "Reset" resetScan) |> ignore

        let pokeRow =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 6.0, 0.0, 0.0))

        pokeRow.Children.Add pokeBox |> ignore
        pokeRow.Children.Add(mkBtn "Poke selected" doPoke) |> ignore
        pokeRow.Children.Add countLabel |> ignore

        let right = DockPanel()
        DockPanel.SetDock(scanRow, Dock.Top)
        right.Children.Add scanRow |> ignore
        DockPanel.SetDock(pokeRow, Dock.Bottom)
        right.Children.Add pokeRow |> ignore
        DockPanel.SetDock(statusLabel, Dock.Bottom)
        right.Children.Add statusLabel |> ignore
        right.Children.Add resultsList |> ignore

        let root = Grid(Margin = Thickness(12.0))
        root.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(352.0)))
        root.ColumnDefinitions.Add(ColumnDefinition(Width = GridLength(1.0, GridUnitType.Star)))
        Grid.SetColumn(left, 0)
        Grid.SetColumn(right, 1)
        root.Children.Add left |> ignore
        root.Children.Add right |> ignore
        self.Children.Add root |> ignore
        self.Focusable <- true
        self.Background <- bg

        // Escape returns to the launcher (needs keyboard focus on the view)
        self.PreviewKeyDown.Add(fun e ->
            if e.Key = Key.Escape then
                onExit ()
                e.Handled <- true)

        self.Loaded.Add(fun _ -> self.Focus() |> ignore)

        let screenTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 50.0)

        screenTimer.Tick.Add(fun _ ->
            match getScreen () with
            | Some buf -> screenBmp.WritePixels(Int32Rect(0, 0, 320, 256), buf, 320 * 4, 0)
            | None -> screenTitle.Text <- "Spectrum (no live machine)")

        screenTimer.Start()
