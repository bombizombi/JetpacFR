"""Names tab: per-row right-click delete for symbols and comments."""
import io

APP = "src/JetpacFR.Desktop/App.fs"
with io.open(APP, encoding="utf-8-sig") as f:
    lines = f.read().split("\n")

start = next(i for i, l in enumerate(lines) if l.strip().startswith("let namesRow (text: string)"))
end = next(i for i, l in enumerate(lines) if l.strip() == "namesList.BorderThickness <- Thickness(0.0)")

new_block = '''        let addRow (text: string) (color: Brush) (addr: int) (menuItems: (string * (unit -> unit)) list) =
            let tb =
                TextBlock(
                    Text = text,
                    Foreground = color,
                    FontFamily = mono,
                    FontSize = 11.5,
                    Margin = Thickness(6.0, 1.0, 6.0, 1.0)
                )

            let item = ListBoxItem(Content = tb, Tag = box addr)

            if not (List.isEmpty menuItems) then
                let menu = ContextMenu()

                for (label, act) in menuItems do
                    let mi = MenuItem(Header = label)
                    mi.Click.Add(fun _ -> act ())
                    menu.Items.Add mi |> ignore

                item.ContextMenu <- menu

            namesList.Items.Add item |> ignore

        let refreshNamesList () =
            namesList.Items.Clear()
            namesCountLabel.Text <- ""

            let refreshAfterEdit (msg: string) =
                refreshControlLists ()
                refreshDisasm ()
                rebuildCommentPane true
                statusText.Text <- msg

            match control with
            | None ->
                addRow "no control file loaded - use 'New ctrl' or 'Import ctrl...' first" dim -1 []
            | Some c ->
                // named function entry points (feed the flame graph labels);
                // right-click deletes the symbol
                for (a, n) in c.Symbols do
                    addRow
                        (sprintf "symbol   %04X            %s" a n)
                        green
                        a
                        [ (sprintf "delete symbol '%s' at $%04X" n a,
                           fun () ->
                               control <- Some(ControlFile.renameSymbol c a "")
                               refreshAfterEdit (sprintf "symbol at $%04X deleted" a)) ]

                // the block map (structural - no delete here)
                for b in c.Blocks do
                    let kind =
                        match b.Kind with
                        | Code -> "code"
                        | Data -> "data"
                        | Gap -> "gap"

                    addRow
                        (sprintf "block    %04X..%04X  %s  (%s)" b.Start b.EndExcl b.Name kind)
                        cyan
                        b.Start
                        []

                // every comment in the file; right-click deletes it
                for m in c.Comments do
                    let span =
                        if m.EndExcl > m.Addr then
                            sprintf "%04X..%04X" m.Addr m.EndExcl
                        else
                            sprintf "%04X" m.Addr

                    let color =
                        match m.Kind with
                        | CommentKind.Name -> yellow
                        | CommentKind.Exec -> orange
                        | _ -> normal

                    let kindText = ControlFile.kindToString m.Kind

                    addRow
                        (sprintf "%-8s %s   %s" kindText span m.Text)
                        color
                        m.Addr
                        [ (sprintf "delete this %s comment" kindText,
                           fun () ->
                               control <- Some(ControlFile.upsert c { m with Text = "" })
                               refreshAfterEdit "comment deleted") ]

                namesCountLabel.Text <-
                    sprintf "%d symbols, %d blocks, %d comments" c.Symbols.Length c.Blocks.Length c.Comments.Length

        let refreshNamesIfVisible () =
            if right.SelectedItem = namesTab then
                refreshNamesList ()

        namesList.MouseDoubleClick.Add(fun _ ->
            match namesList.SelectedItem with
            | :? ListBoxItem as it ->
                match it.Tag with
                | :? int as a when a >= 0 ->
                    let addr = a &&& 0xFFFF
                    // Land on the row's line whatever it takes: execution view when the
                    // address executed in the trace, otherwise switch to the memory
                    // sweep and center on the address there.
                    if disasmModeMemory then
                        focusCodeView "names tab" addr
                    else
                        match currentTrace () with
                        | Some t when
                            t.Entries.Length > 0
                            && addr < t.FirstIndexAtPc.Length
                            && t.FirstIndexAtPc[addr] >= 0
                            ->
                            focusCodeView "names tab" addr
                        | _ ->
                            pendingMemCursor <- Some addr
                            memModeBtn.IsChecked <- Nullable<bool>(true)
                | _ -> ()
            | _ -> ())

        namesRefreshBtn.Click.Add(fun _ -> refreshNamesList ())

        let namesTop =
            StackPanel(Orientation = Orientation.Horizontal, Margin = Thickness(0.0, 0.0, 0.0, 4.0))

        namesTop.Children.Add namesRefreshBtn |> ignore
        namesTop.Children.Add namesCountLabel |> ignore

        namesTop.Children.Add(
            TextBlock(
                Text = "double-click a row to jump - right-click a symbol or comment row to delete it",
                Foreground = dim,
                FontSize = 11.0,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Thickness(8.0, 0.0, 0.0, 0.0)
            )
        )
        |> ignore

        DockPanel.SetDock(namesTop, Dock.Top)
        namesPanel.Children.Add namesTop |> ignore
        namesPanel.Children.Add namesList |> ignore // last child fills the pane
        namesTab.Content <- namesPanel
        // First tab in the strip, and the one open by default. Selecting it
        // here also fires the refresh above, populating the list at startup.
        right.Items.Insert(0, namesTab)
        right.SelectedIndex <- 0
        right.SelectionChanged.Add(fun _ ->
            if right.SelectedItem = namesTab then
                refreshNamesList ())
        // Phase switches and game switches route through this hook (declared
        // at class level): the names list AND the mass-comment list re-read
        // the freshly loaded control file instead of keeping the previous
        // project's rows.
        refreshControlLists <-
            fun () ->
                refreshNamesList ()
                refreshMassIfVisible ()'''

lines[start:end + 1] = new_block.split("\n")

with io.open(APP, "w", encoding="utf-8", newline="") as f:
    f.write("\n".join(lines))

print("names list rewritten with per-row delete")
