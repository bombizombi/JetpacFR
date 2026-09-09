namespace JetpacFR.Desktop

open System
open System.Windows.Media

/// Day/night palette for the whole desktop UI. One module owns every shared
/// brush instance; views alias these instead of constructing their own, so a
/// theme switch only mutates `.Color` in place and every holder (list rows,
/// data-trigger setters, pens wrapping a shared brush) follows with no
/// re-wiring. Specialty visuals (flame hues, heat LUT, map kind colors) keep
/// working on both backgrounds and live in their own controls; only the
/// chrome + text colors below have per-mode presets.
module Theme =

    type Mode =
        | Dark
        | Light

    /// Per-mode values for the shared chrome/text brushes.
    type private Preset =
        { Bg: Color
          Panel: Color
          Normal: Color
          Dim: Color
          Green: Color
          Cyan: Color
          Yellow: Color
          Red: Color
          Orange: Color
          DarkFg: Color
          HiBg: Color
          Bright: Color }

    let private dark =
        { Bg = Color.FromRgb(0x10uy, 0x10uy, 0x16uy)
          Panel = Color.FromRgb(0x18uy, 0x1Cuy, 0x24uy)
          Normal = Color.FromRgb(0xC8uy, 0xC8uy, 0xCEuy)
          Dim = Color.FromRgb(0x8Auy, 0x8Auy, 0x92uy)
          Green = Color.FromRgb(0x4Euy, 0xE0uy, 0x60uy)
          Cyan = Color.FromRgb(0x4Euy, 0xD0uy, 0xE0uy)
          Yellow = Color.FromRgb(0xE6uy, 0xD0uy, 0x4Euy)
          Red = Color.FromRgb(0xE8uy, 0x54uy, 0x54uy)
          Orange = Color.FromRgb(0xE8uy, 0x8Auy, 0x2Euy)
          DarkFg = Color.FromRgb(0x02uy, 0x06uy, 0x17uy)
          HiBg = Color.FromRgb(0x2Euy, 0x34uy, 0x44uy)
          Bright = Color.FromRgb(0xECuy, 0xEFuy, 0xF8uy) }

    let private light =
        { Bg = Color.FromRgb(0xF1uy, 0xF4uy, 0xF9uy)
          Panel = Color.FromRgb(0xFFuy, 0xFFuy, 0xFFuy)
          Normal = Color.FromRgb(0x1Auy, 0x1Duy, 0x24uy)
          Dim = Color.FromRgb(0x5Auy, 0x61uy, 0x6Euy)
          Green = Color.FromRgb(0x16uy, 0xA3uy, 0x4Auy)
          Cyan = Color.FromRgb(0x0Euy, 0x74uy, 0x90uy)
          Yellow = Color.FromRgb(0xA1uy, 0x62uy, 0x07uy)
          Red = Color.FromRgb(0xDCuy, 0x26uy, 0x26uy)
          Orange = Color.FromRgb(0xEAuy, 0x58uy, 0x0Cuy)
          DarkFg = Color.FromRgb(0x02uy, 0x06uy, 0x17uy)
          HiBg = Color.FromRgb(0xC7uy, 0xD7uy, 0xF5uy)
          Bright = Color.FromRgb(0x0Fuy, 0x17uy, 0x2Auy) }

    let mutable mode = if GuiConfig.isLight () then Light else Dark

    let private preset () = if mode = Light then light else dark

    let private brush (get: Preset -> Color) =
        SolidColorBrush(get (preset ()))

    // Shared instances: alias these, never construct the same color locally.
    let bg = brush (fun p -> p.Bg)
    let panel = brush (fun p -> p.Panel)
    let normal = brush (fun p -> p.Normal)
    let dim = brush (fun p -> p.Dim)
    let green = brush (fun p -> p.Green)
    let cyan = brush (fun p -> p.Cyan)
    let yellow = brush (fun p -> p.Yellow)
    let red = brush (fun p -> p.Red)
    let orange = brush (fun p -> p.Orange)
    let darkFg = brush (fun p -> p.DarkFg)
    let hiBg = brush (fun p -> p.HiBg)
    let bright = brush (fun p -> p.Bright)

    // Accent hues shared by the brush-A/B toggles; identical in both modes.
    let sky = Color.FromRgb(0x38uy, 0xBDuy, 0xF8uy)
    let amber = Color.FromRgb(0xFBuy, 0xBFuy, 0x24uy)

    // Execution-heat ramp (cold -> hot). Dark-navy cold cells read as
    // "unvisited" on both backgrounds, so one LUT serves both modes.
    let heatLut =
        [| Color.FromRgb(0x08uy, 0x08uy, 0x0Cuy)
           Color.FromRgb(0x0Auy, 0x1Euy, 0x4Auy)
           Color.FromRgb(0x0Auy, 0x3Auy, 0x5Euy)
           Color.FromRgb(0x0Auy, 0x5Cuy, 0x6Auy)
           Color.FromRgb(0x12uy, 0x86uy, 0x5Euy)
           Color.FromRgb(0x3Auy, 0xA8uy, 0x3Cuy)
           Color.FromRgb(0x96uy, 0xB8uy, 0x2Cuy)
           Color.FromRgb(0xD8uy, 0xA0uy, 0x22uy)
           Color.FromRgb(0xE8uy, 0x66uy, 0x18uy)
           Color.FromRgb(0xF0uy, 0x38uy, 0x28uy) |]

    let isLight () = mode = Light

    /// Switch mode: mutate the shared brushes in place (every alias follows).
    /// Syncs the in-memory gui.cfg state; the caller persists it (toggle saves
    /// at once, shutdown saves with the window bounds). Controls with private
    /// pens/memos expose their own RefreshTheme; call those right after.
    let apply (m: Mode) =
        mode <- m
        GuiConfig.setLight (m = Light)
        let p = preset ()
        bg.Color <- p.Bg
        panel.Color <- p.Panel
        normal.Color <- p.Normal
        dim.Color <- p.Dim
        green.Color <- p.Green
        cyan.Color <- p.Cyan
        yellow.Color <- p.Yellow
        red.Color <- p.Red
        orange.Color <- p.Orange
        darkFg.Color <- p.DarkFg
        hiBg.Color <- p.HiBg
        bright.Color <- p.Bright
