namespace JetpacFR.Core

open System

/// Pure layout model behind the desktop ControlMap panel: turns the
/// control file's structure plus optional trace/memory overlays into a
/// per-pixel-row description the renderer just paints. No WPF types here,
/// so the same geometry can drive a canvas renderer later.
///
/// The vertical axis is the address space: row 0 is viewStart, row
/// rowCount-1 the top of viewEnd's pixel. Address <-> row mapping is
/// linear through `addrY` / `rowLo` / `rowHi`.
module CtrlMapModel =

    /// Semantic kind of one pixel row (Mixed = several kinds sampled).
    type RowKind =
        | UnmappedR
        | CodeR
        | DataR
        | GapR
        | MixedR

    /// One pixel row of the strip.
    type MapRow =
        {
            Kind: RowKind
            /// 0-9 log-scaled execution heat (0 = never executed).
            Heat: int
            SelfMod: bool
            /// Any Line comment hit or Name/Range cover inside the row.
            Comment: bool
        }

    type LabelInfo = { Addr: int; Text: string }

    /// Instruction-level readout, present only at high zoom.
    type InsnSnippet =
        { Addr: int
          Bytes: string
          Text: string
          Comment: string option }

    type MapRender =
        {
            ViewStart: int
            ViewEnd: int
            Rows: MapRow[]
            /// Visible block-name labels (band tall enough to hold one line).
            Labels: LabelInfo[]
            /// Visible range-comment extents as address pairs.
            Ranges: (int * int)[]
            Snippets: InsnSnippet[]
        }

    /// Address -> fractional row index.
    let addrY (viewStart: int) (viewEnd: int) (rowCount: int) (addr: int) : float =
        float rowCount * float (addr - viewStart) / float (max 1 (viewEnd - viewStart))

    /// Top address of pixel row y.
    let rowLo (viewStart: int) (viewEnd: int) (rowCount: int) (y: int) : int =
        viewStart + (viewEnd - viewStart) * y / rowCount

    let private classify (blocks: Block list) (addr: int) : RowKind =
        match blocks |> List.tryFind (fun b -> b.Start <= addr && addr < b.EndExcl) with
        | None -> UnmappedR
        | Some b ->
            match b.Kind with
            | Code -> CodeR
            | Data -> DataR
            | Gap -> GapR

    /// Build the render for the current viewport.
    ///
    /// counts/selfMod follow Trace.PerPcCount/SelfModified conventions
    /// (indexed by address); pass empty arrays when no trace exists.
    let render
        (blocks: Block list)
        (comments: ControlComment list)
        (instrStarts: bool[] option)
        (mem: byte[] option)
        (counts: int[])
        (selfMod: bool[])
        (viewStart: int)
        (viewEnd: int)
        (rowCount: int)
        : MapRender =

        let viewStart = max 0 (min 0xFFFF viewStart)
        let viewEnd = max (viewStart + 1) (min 0x10000 viewEnd)
        let span = viewEnd - viewStart
        let ppb = float rowCount / float span

        // Heat scale: same log mapping as refreshHeatmap so both views agree.
        let maxC = if counts.Length = 0 then 1 else max 1 (Array.max counts)
        let logMax = log10 (float maxC)

        let level (a: int) =
            let c = if a < counts.Length then counts[a] else 0

            if c <= 0 then
                0
            else
                min 9 (int (9.0 * (log10 (float c) / logMax)))

        // Comment coverage bitmap: Line hits plus everything a Name/Range spans.
        // Frames comments cover trace-state ranges, not addresses - skipped.
        let covered = Array.create 0x10000 false

        for m in comments do
            if m.Kind <> CommentKind.Frames then
                let hi =
                    if m.Kind = CommentKind.Line then
                        m.Addr + 1
                    else
                        max m.Addr m.EndExcl

                let lo = min 0xFFFF m.Addr
                let hi = min 0x10000 hi

                for a in lo .. hi - 1 do
                    covered[a] <- true

        // Row classification by sampling; 8 probes resolve mixed content well
        // enough at every zoom the renderer allows.
        let samples = [| for i in 0..7 -> float i * float span / 8.0 |> int |]
        let rows = Array.zeroCreate<MapRow> rowCount

        for y in 0 .. rowCount - 1 do
            // When zoomed in past 1 byte/pixel several rows share one address;
            // clamp to a non-empty probe range instead of inheriting a neighbor.
            let lo = min (rowLo viewStart viewEnd rowCount y) (viewEnd - 1)
            let hi = max (lo + 1) (rowLo viewStart viewEnd rowCount (y + 1))

            let kinds =
                samples |> Array.map (fun off -> classify blocks (min 0xFFFF (lo + off)))

            let distinct = kinds |> Array.distinct

            let kind =
                if distinct.Length = 1 then
                    distinct[0]
                elif distinct.Length = 2 && distinct |> Array.contains UnmappedR then
                    distinct |> Array.find ((<>) UnmappedR)
                else
                    MixedR

            let heat = samples |> Array.map (fun off -> level (lo + off)) |> Array.max

            let selfModHit =
                samples
                |> Array.exists (fun off ->
                    let a = lo + off
                    a < selfMod.Length && selfMod[a])

            let hasComment =
                let a0 = min 0xFFFF lo
                let a1 = min 0xFFFF (hi - 1)
                covered[a0] || covered[a1]

            rows[y] <-
                { Kind = kind
                  Heat = heat
                  SelfMod = selfModHit
                  Comment = hasComment }

        let labels =
            blocks
            |> List.choose (fun b ->
                let y0 = addrY viewStart viewEnd rowCount b.Start
                let y1 = addrY viewStart viewEnd rowCount b.EndExcl

                if y1 - y0 >= 10.0 && b.Start < viewEnd && b.EndExcl > viewStart then
                    Some { Addr = b.Start; Text = b.Name }
                else
                    None)
            |> Array.ofList

        // Range-comment extents clipped to the viewport.
        let ranges =
            comments
            |> List.filter (fun m -> m.Kind = CommentKind.Range && m.EndExcl > m.Addr)
            |> List.map (fun m -> (max viewStart m.Addr, min viewEnd m.EndExcl))
            |> List.filter (fun (a, b) -> b > a)
            |> Array.ofList

        // Instruction-level readouts past the readability threshold.
        let snippets =
            match instrStarts, mem with
            | Some starts, Some memory when ppb >= 8.0 ->
                let mutable snips = []
                let mutable a = viewStart
                let mutable guard = 0

                while a < viewEnd && guard < 256 do
                    guard <- guard + 1

                    while a < viewEnd && a < 0x10000 && not starts[a] do
                        a <- a + 1

                    if a < viewEnd && a < 0x10000 then
                        let insn = Disasm.disasmMemory memory a
                        let len = max 1 insn.Length

                        let hex =
                            seq { for i in 0 .. len - 1 -> sprintf "%02X" memory[(a + i) &&& 0xFFFF] }
                            |> String.concat " "

                        let cmt =
                            comments
                            |> List.tryFind (fun m -> m.Kind = CommentKind.Line && m.Addr = a)
                            |> Option.map (fun m -> m.Text)

                        snips <-
                            { Addr = a
                              Bytes = hex
                              Text = insn.Text
                              Comment = cmt }
                            :: snips

                        a <- a + len

                snips |> List.rev |> Array.ofList
            | _ -> [||]

        { ViewStart = viewStart
          ViewEnd = viewEnd
          Rows = rows
          Labels = labels
          Ranges = ranges
          Snippets = snippets }
