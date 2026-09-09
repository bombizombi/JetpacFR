namespace JetpacFR.Desktop

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Threading

/// The bare game view: only the live Spectrum screen plus a hold-to-play-
/// in-reverse control (button or F3). It is embedded in the main window as
/// a phase - no second window - and talks to the shared session through
/// the accessor callbacks. Escape (or the main-menu button) returns to the
/// launcher via the onExit callback.
type GameOnlyView
    (
        getScreen: unit -> byte[] option,
        getFrame: unit -> int,
        minFrame: unit -> int,
        seekFrame: int -> unit,
        pauseGame: unit -> unit,
        onExit: unit -> unit
    ) as self =
    inherit Grid()

    let dim = Theme.dim

    let screenBmp = WriteableBitmap(320, 256, 96.0, 96.0, PixelFormats.Bgra32, null)

    let screenImg =
        Image(Source = screenBmp, Width = 640.0, Height = 512.0, Margin = Thickness(0.0, 0.0, 0.0, 10.0))

    do RenderOptions.SetBitmapScalingMode(screenImg, BitmapScalingMode.NearestNeighbor)

    let mutable reversing = false

    let revLabel =
        TextBlock(
            Foreground = dim,
            FontSize = 12.0,
            Text =
                "hold the button or F3 to play in reverse - Run in the emulator resumes live from the parked frame - Escape returns to the main menu"
        )

    let revTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 20.0)

    let stopReverse () =
        if reversing then
            reversing <- false
            revTimer.Stop()

    let startReverse () =
        if not reversing then
            pauseGame ()

            let f = getFrame () - 1

            if f < minFrame () then
                revLabel.Text <- "already at the earliest recorded frame"
            else
                reversing <- true
                revTimer.Start()
                seekFrame f

    do
        revTimer.Tick.Add(fun _ ->
            let f = getFrame () - 1

            if f < minFrame () then
                stopReverse ()
                revLabel.Text <- "reached the earliest recorded frame"
            else
                seekFrame f)

    do
        let menuBtn =
            Button(Content = "<- main menu", Width = 110.0, HorizontalAlignment = HorizontalAlignment.Left)

        menuBtn.Click.Add(fun _ ->
            stopReverse ()
            onExit ())

        let title =
            TextBlock(Text = "Just the Game", Foreground = dim, FontSize = 12.0, Margin = Thickness(0.0, 0.0, 0.0, 6.0))

        let revBtn =
            Button(
                Content = "\u25C0 Play in reverse (hold, or F3)",
                Height = 44.0,
                FontSize = 14.0,
                FontWeight = FontWeights.SemiBold,
                Margin = Thickness(0.0, 10.0, 0.0, 0.0)
            )

        revBtn.PreviewMouseLeftButtonDown.Add(fun _ -> startReverse ())
        revBtn.PreviewMouseLeftButtonUp.Add(fun _ -> stopReverse ())
        revBtn.MouseLeave.Add(fun _ -> stopReverse ())

        let col = StackPanel(Margin = Thickness(12.0))
        col.Children.Add menuBtn |> ignore
        col.Children.Add title |> ignore
        col.Children.Add screenImg |> ignore
        col.Children.Add revBtn |> ignore
        col.Children.Add revLabel |> ignore
        self.Children.Add col |> ignore
        self.Focusable <- true
        self.Background <- Theme.bg

        // keyboard on the view itself: F3 hold = reverse, Escape = main menu
        self.PreviewKeyDown.Add(fun e ->
            if e.Key = Key.F3 && not reversing then
                startReverse ()
                e.Handled <- true
            elif e.Key = Key.Escape then
                stopReverse ()
                onExit ()
                e.Handled <- true)

        self.PreviewKeyUp.Add(fun e ->
            if e.Key = Key.F3 then
                stopReverse ()
                e.Handled <- true)

        self.Loaded.Add(fun _ -> self.Focus() |> ignore)

    let drawTimer = DispatcherTimer(Interval = TimeSpan.FromMilliseconds 20.0)

    do
        drawTimer.Tick.Add(fun _ ->
            match getScreen () with
            | Some buf -> screenBmp.WritePixels(Int32Rect(0, 0, 320, 256), buf, 320 * 4, 0)
            | None -> revLabel.Text <- "no live machine - open a project from the main menu first")

        drawTimer.Start()
