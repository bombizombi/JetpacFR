namespace JetpacFR.Desktop

open System
open System.Globalization
open System.IO
open System.Text.Json

/// Unimportant GUI settings (theme, main-window bounds) plus the recording /
/// flame tunables in gui.cfg next to the exe. Never game state: timelines,
/// control files and the entry cache keep their own files. Everything
/// tolerates a missing or corrupt file by falling back to defaults.
module GuiConfig =

    let fileName = "gui.cfg"

    let private path () =
        Path.Combine(AppContext.BaseDirectory, fileName)

    /// In-memory settings. Theme is a bool (true = light); bounds are None
    /// until the window has been placed once (first run centers a default).
    /// The recording tunables feed TimelineCadence and the flame graph's
    /// zoomed-out block width (see there for units).
    type private State =
        { Light: bool
          Width: float option
          Height: float option
          X: float option
          Y: float option
          DenseSeconds: int
          SparseEverySeconds: int
          OverviewBlockPx: float }

    let private defaults =
        { Light = false
          Width = None
          Height = None
          X = None
          Y = None
          DenseSeconds = 10
          SparseEverySeconds = 1
          OverviewBlockPx = 8.0 }

    let private num (root: JsonElement) (key: string) : float option =
        let mutable el = Unchecked.defaultof<JsonElement>

        if root.TryGetProperty(key, &el) && el.ValueKind = JsonValueKind.Number then
            try
                Some(el.GetDouble())
            with _ ->
                None
        else
            None

    let private numOr (root: JsonElement) (key: string) (fallback: float) =
        match num root key with
        | Some v when Double.IsFinite v && v > 0.0 -> v
        | _ -> fallback

    let private intOr (root: JsonElement) (key: string) (fallback: int) =
        match num root key with
        | Some v when v >= 0.0 && v <= 600.0 -> int (Math.Round v)
        | _ -> fallback

    let private themeOf (root: JsonElement) : bool =
        let mutable el = Unchecked.defaultof<JsonElement>

        if root.TryGetProperty("theme", &el) && el.ValueKind = JsonValueKind.String then
            el.GetString().Trim().ToLowerInvariant() = "light"
        else
            // One-time import from the earlier theme.txt world.
            try
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "theme.txt")).Trim().ToLowerInvariant() = "light"
            with _ ->
                false

    let private parse (text: string) : State =
        try
            use doc = JsonDocument.Parse text
            let root = doc.RootElement

            if root.ValueKind <> JsonValueKind.Object then
                defaults
            else
                { Light = themeOf root
                  Width = num root "width"
                  Height = num root "height"
                  X = num root "x"
                  Y = num root "y"
                  DenseSeconds = intOr root "denseSeconds" defaults.DenseSeconds
                  SparseEverySeconds = intOr root "sparseEverySeconds" defaults.SparseEverySeconds
                  OverviewBlockPx = numOr root "overviewBlockPx" defaults.OverviewBlockPx }
        with _ ->
            defaults

    let mutable private state =
        try
            if File.Exists(path ()) then parse (File.ReadAllText(path ())) else defaults
        with _ ->
            defaults

    let isLight () = state.Light

    let setLight (v: bool) = state <- { state with Light = v }

    /// Recording cadence: full states for the first `denseSeconds` seconds,
    /// then one state pair per `sparseEverySeconds` (at 50 fps).
    let denseSeconds () = state.DenseSeconds
    let sparseEverySeconds () = max 1 state.SparseEverySeconds

    /// Zoomed-out flame blocks paint ~this many pixels wide.
    let overviewBlockPx () = state.OverviewBlockPx

    /// Saved bounds when a previous run placed the window.
    let bounds () : (float * float * float * float) option =
        match state.Width, state.Height, state.X, state.Y with
        | Some w, Some h, Some x, Some y when w > 0.0 && h > 0.0 -> Some(w, h, x, y)
        | _ -> None

    let setBounds (w: float, h: float, x: float, y: float) =
        // RestoreBounds reports infinities once the HWND is gone; never let a
        // teardown read clobber good state.
        if Double.IsFinite w && Double.IsFinite h && Double.IsFinite x && Double.IsFinite y then
            state <- { state with Width = Some w; Height = Some h; X = Some x; Y = Some y }

    let private f (v: float) =
        v.ToString("0.##", CultureInfo.InvariantCulture)

    /// Persist the in-memory state. Best effort: a failed write must never
    /// break shutdown.
    let save () =
        try
            let theme = if state.Light then "light" else "dark"

            let boundsText =
                match bounds () with
                | Some(w, h, x, y) ->
                    sprintf
                        ",\n  \"width\": %s,\n  \"height\": %s,\n  \"x\": %s,\n  \"y\": %s"
                        (f w)
                        (f h)
                        (f x)
                        (f y)
                | None -> ""

            File.WriteAllText(
                path (),
                sprintf
                    "{\n  \"theme\": \"%s\"%s,\n  \"denseSeconds\": %d,\n  \"sparseEverySeconds\": %d,\n  \"overviewBlockPx\": %s\n}\n"
                    theme
                    boundsText
                    state.DenseSeconds
                    (max 1 state.SparseEverySeconds)
                    (f state.OverviewBlockPx)
            )

            try
                File.Delete(Path.Combine(AppContext.BaseDirectory, "theme.txt"))
            with _ ->
                ()
        with _ ->
            ()
