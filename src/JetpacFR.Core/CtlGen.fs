namespace JetpacFR.Core

/// Static control-map generation: classify a memory snapshot into code /
/// data / gap blocks with no execution information - a port of SkoolKit's
/// "generate ctls without a code map" algorithm. The walk decodes
/// instructions linearly, ends a code block at every terminal instruction
/// (JP/JR/DJNZ/RET, via Z80Flow.classify), demotes runs of one repeated
/// operation that are too long to be plausible code to data (catchData),
/// marks NOP-prefixed code blocks and all-zero blocks as gaps, joins
/// adjacent data/gap blocks, and finally scans data and code blocks for
/// text runs. A text run becomes a Data block plus its decoded string
/// (TextNotes) - the control file has no first-class text kind, so
/// toControlFile attaches the string as a Range comment.
///
/// Not ported from SkoolKit: code-map reading (textual profiles, Z80/SpecEmu
/// bitmaps), the with-code-map refinement and CALL-referrer promotion (the
/// workbench can seed those from real traces later), RST argument
/// sub-directives, the comment generator, and textual ctl output
/// (SkoolCtl.export already writes .ctl files).
module CtlGen =

    open System
    open System.Collections.Generic

    type Config =
        {
            /// Characters eligible for being marked as text.
            TextChars: string
            TextMinLengthCode: int
            TextMinLengthData: int
            /// If non-empty, a string is text only if it contains one of these words.
            Words: string list
            /// Repeats of one operation beyond this look like data (0 = never catch).
            MaxRepeat: int
        }

    let defaultConfig =
        { TextChars = String([| for c in 0x20uy .. 0x7Euy -> char c |])
          TextMinLengthCode = 3
          TextMinLengthData = 2
          Words = []
          MaxRepeat = 4 }

    /// Static-analysis outcome: the block map over [start, endExcl) plus the
    /// text runs found in it as (start, endExcl, decoded string).
    type Result =
        { Blocks: Block list
          TextNotes: (int * int * string) list }

    let private isTerminal (mem: byte[]) (addr: int) =
        let kind, _ = Z80Flow.classify mem addr

        match kind with
        | Z80Flow.FlowEnd.Jump _
        | Z80Flow.FlowEnd.Branch _
        | Z80Flow.FlowEnd.Return -> true
        | Z80Flow.Linear
        | Z80Flow.FlowEnd.Call _ -> false

    /// Normalized operation identity (the stand-in for SkoolKit's OpId):
    /// prefix group (unprefixed / DD-FD collapsed / ED) plus the opcode byte,
    /// with the condition field of the RET cc / JP cc / CALL cc families
    /// masked out - a run of "the same kind of operation" is what looks like
    /// data, and differing conditions must not hide one.
    let private opKey (mem: byte[]) (addr: int) (len: int) : int * int =
        let b0 = int mem[addr]

        let mask b =
            let m = b &&& 0xC7

            if m = 0xC0 || m = 0xC2 || m = 0xC4 then m else b

        if (b0 = 0xDD || b0 = 0xFD) && len >= 2 then
            (1, int mem[(addr + 1) &&& 0xFFFF])
        elif b0 = 0xED && len >= 2 then
            (2, int mem[(addr + 1) &&& 0xFFFF])
        else
            (0, mask b0)

    /// If the run of identical operations is too long to be plausible code,
    /// start a data block at `ctlAddr` and continue at `addr`; otherwise keep
    /// `ctlAddr`. A two-instruction run of LD H,(HL) / LD L,(HL)
    /// (0x66 / 0x6E) is legitimate and never caught.
    let private catchData (ctls: ResizeArray<int * char>) ctlAddr count maxRepeat addr (opBytes: byte[]) =
        let isHarmlessPair =
            count = 2 && opBytes.Length > 0 && (opBytes[0] = 0x66uy || opBytes[0] = 0x6Euy)

        if count >= maxRepeat && maxRepeat > 0 && not isHarmlessPair then
            if ctls.Count = 0 || snd ctls[ctls.Count - 1] <> 'b' then
                ctls.Add(ctlAddr, 'b')

            addr
        else
            ctlAddr

    let private isAllowedText (text: string) minLength (words: string list) =
        text.Length >= minLength
        && (List.isEmpty words
            || (let lower = text.ToLower() in words |> List.exists lower.Contains))

    /// Finds the (start, endExcl) ranges within [start, endExcl) that look
    /// like text.
    let private getTextBlocks (mem: byte[]) start finish (config: Config) isData =
        let minLength =
            if isData then
                config.TextMinLengthData
            else
                config.TextMinLengthCode

        let blocks = ResizeArray<int * int>()

        if finish - start >= minLength then
            let mutable textStart = start
            let mutable length = 0

            let endRun runEnd =
                if length > 0 then
                    let text = String(Array.init length (fun i -> char mem[textStart + i]))

                    if isAllowedText text minLength config.Words then
                        blocks.Add(textStart, runEnd)

                    length <- 0

            for address in start .. finish - 1 do
                let ch = char mem[address]

                if config.TextChars.Contains ch then
                    if length = 0 then
                        textStart <- address

                    length <- length + 1
                else
                    endRun address

            endRun finish

        List.ofSeq blocks

    let private isAllZero (mem: byte[]) (start: int) (endExcl: int) =
        let mutable i = start
        let mutable zero = true

        while zero && i < endExcl do
            if mem[i] <> 0uy then zero <- false else i <- i + 1

        zero

    /// Classify [start, endExcl) of the snapshot. Blocks come out sorted,
    /// non-overlapping, covering the span; names use the block_%04X
    /// auto-name convention (an unnamed block, renameable in the GUI).
    let analyze (memory: byte[]) (start: int) (endExcl: int) (config: Config) : Result =
        if endExcl <= start || memory.Length < endExcl then
            { Blocks = []; TextNotes = [] }
        else
            let finish = endExcl
            let ctlList = ResizeArray<int * char>()
            let mutable ctlAddr = start
            let mutable prevKey: (int * int) option = None
            let mutable prevBytes: byte[] = [||]
            let mutable count = 1
            let mutable addr = start

            while addr < finish do
                let len = max 1 (min (Disasm.disasmLength memory addr) (finish - addr))

                if isTerminal memory addr then
                    ctlAddr <- catchData ctlList ctlAddr count config.MaxRepeat addr prevBytes
                    ctlList.Add(ctlAddr, 'c')
                    ctlAddr <- addr + len
                    prevKey <- None
                    prevBytes <- [||]
                    count <- 1
                else
                    match prevKey with
                    | Some k when k = opKey memory addr len -> count <- count + 1
                    | Some _ ->
                        ctlAddr <- catchData ctlList ctlAddr count config.MaxRepeat addr prevBytes
                        count <- 1
                    | None -> ()

                    prevKey <- Some(opKey memory addr len)
                    let opBytes = memory[addr .. addr + len - 1]
                    prevBytes <- opBytes

                addr <- addr + len

            if ctlList.Count = 0 || fst ctlList[ctlList.Count - 1] <> ctlAddr then
                ctlList.Add(ctlAddr, 'b')
            // The 'i' terminator bounds the last block; never a block start.
            ctlList.Add(finish, 'i')

            let ctls = Dictionary<int, char>()

            for address, ctl in ctlList do
                ctls[address] <- ctl

            let edges () = ctls.Keys |> Seq.sort |> Seq.toList

            // Mark a NOP sequence at the beginning of a code block as a zero
            // block, and mark a data block of all zeroes as a zero block.
            for blockStart, blockEnd in edges () |> List.pairwise do
                if ctls[blockStart] = 'c' then
                    ctls[blockStart] <- 's'

                    let mutable firstNonZero = None
                    let mutable a = blockStart

                    while firstNonZero.IsNone && a < blockEnd do
                        if memory[a] <> 0uy then
                            firstNonZero <- Some a
                        else
                            a <- a + 1

                    match firstNonZero with
                    | Some address -> ctls[address] <- 'c'
                    | None -> ()
                elif isAllZero memory blockStart blockEnd then
                    ctls[blockStart] <- 's'

            // Join any adjacent data and zero blocks.
            let isDataLike ctl = ctl = 'b' || ctl = 's'
            let sorted = edges () |> List.map (fun a -> a, ctls[a])
            let mutable prevAddr, prevCtl = List.head sorted

            for address, ctl in List.tail sorted do
                if isDataLike ctl && isDataLike prevCtl then
                    ctls[prevAddr] <- 'b'
                    ctls.Remove(address) |> ignore
                else
                    prevAddr <- address
                    prevCtl <- ctl

            // Look for text.
            for blockStart, blockEnd in edges () |> List.pairwise do
                match ctls[blockStart] with
                | 'b' ->
                    for textStart, textEnd in getTextBlocks memory blockStart blockEnd config true do
                        ctls[textStart] <- 't'

                        if textEnd < blockEnd then
                            ctls[textEnd] <- 'b'
                | 'c' ->
                    match getTextBlocks memory blockStart blockEnd config false with
                    | [] -> ()
                    | textBlocks ->
                        ctls[blockStart] <- 'b'

                        for textStart, textEnd in textBlocks do
                            ctls[textStart] <- 't'

                            if textEnd < blockEnd then
                                ctls[textEnd] <- 'b'

                        let lastEnd = snd (List.last textBlocks)

                        if lastEnd < blockEnd then
                            ctls[lastEnd] <- 'c'
                | _ -> ()

            // Assemble blocks; collect the text runs for the caller.
            let keys = edges ()
            let mutable notesRev = []

            let blocks =
                [ for blockStart, blockEnd in List.pairwise keys do
                      let autoName = sprintf "block_%04X" blockStart

                      let block kind =
                          { Start = blockStart
                            EndExcl = blockEnd
                            Name = autoName
                            Kind = kind }

                      match ctls[blockStart] with
                      | 'c' -> block Code
                      | 's' -> block Gap
                      | 't' ->
                          let text =
                              String(Array.init (blockEnd - blockStart) (fun i -> char memory[blockStart + i]))

                          notesRev <- (blockStart, blockEnd, text) :: notesRev
                          block Data
                      | _ -> block Data ]

            { Blocks = blocks
              TextNotes = List.rev notesRev }

    /// The default analysis span for a full address-space snapshot: RAM from
    /// 0x4000 to the last non-zero byte + 1 (the same rule loadOrCreate
    /// applies to original.bin). Smaller images are analyzed whole.
    let ramSpan (memory: byte[]) : int * int =
        if memory.Length < 0x4001 then
            0, max 1 memory.Length
        else
            let mutable i = (min memory.Length 0x10000) - 1

            while i > 0x4000 && memory[i] = 0uy do
                i <- i - 1

            0x4000, min 0x10000 (max 0x4001 (i + 1))

    /// Assemble a complete ControlFile: the generated blocks, text runs as
    /// Range comments (the file has no first-class text kind), the entry PC
    /// anchor, no symbols. Dirty stays false - the caller decides when the
    /// file reaches disk (ControlFile.save).
    let toControlFile (r: Result) (entryPc: int) (start: int) (endExcl: int) : ControlFile =
        { ControlFile.empty start endExcl with
            EntryPc = entryPc
            Blocks = r.Blocks
            Comments =
                r.TextNotes
                |> List.map (fun (a, b, text) ->
                    { Kind = CommentKind.Range
                      Addr = a
                      EndExcl = b
                      InstrIndex = -1
                      Text = text }) }
