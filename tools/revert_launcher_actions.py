"""Revert the launcher action rows to plain stacked buttons (pre spectrum keys)."""
import io

APP = "src/JetpacFR.Desktop/LauncherView.fs"
with io.open(APP, encoding="utf-8-sig") as f:
    lines = f.read().split("\n")

start = next(i for i, l in enumerate(lines) if "top: the three open-with actions" in l)
end = next(i for i, l in enumerate(lines) if l.strip() == "ctx.OpenCheatEngine" and i > start + 5)

new_block = '''        // ---- top: the three open-with actions, one below the other, each
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
            ctx.OpenCheatEngine'''

lines[start:end + 1] = new_block.split("\n")

with io.open(APP, "w", encoding="utf-8", newline="") as f:
    f.write("\n".join(lines))

print("reverted to plain action buttons")
