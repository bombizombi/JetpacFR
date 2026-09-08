
import { Record, Union } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { tuple_type, array_type, option_type, string_type, record_type, bool_type, int32_type, union_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { min, max } from "../fable_modules/fable-library-js.5.17.0/Double.js";
import { reverse, cons, empty, map as map_2, filter, choose, toArray as toArray_1, tryFind } from "../fable_modules/fable-library-js.5.17.0/List.js";
import { find, contains, item, map as map_1, setItem, fill, max as max_1 } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { safeHash, equals, disposeSafe, getEnumerator, comparePrimitives } from "../fable_modules/fable-library-js.5.17.0/Util.js";
import { CommentKind } from "../ControlTypesWeb.js";
import { map, delay, toArray } from "../fable_modules/fable-library-js.5.17.0/Seq.js";
import { rangeDouble } from "../fable_modules/fable-library-js.5.17.0/Range.js";
import { Array_distinct } from "../fable_modules/fable-library-js.5.17.0/Seq2.js";
import { disasmMemory } from "./Disasm.js";
import { printf, toText, join } from "../fable_modules/fable-library-js.5.17.0/String.js";

/**
 * Semantic kind of one pixel row (Mixed = several kinds sampled).
 */
export class RowKind extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UnmappedR", "CodeR", "DataR", "GapR", "MixedR"];
    }
    static UnmappedR = new RowKind(0, []);
    static CodeR = new RowKind(1, []);
    static DataR = new RowKind(2, []);
    static GapR = new RowKind(3, []);
    static MixedR = new RowKind(4, []);
}

export function RowKind_$reflection() {
    return union_type("JetpacFR.Core.CtrlMapModel.RowKind", [], RowKind, () => [[], [], [], [], []]);
}

/**
 * One pixel row of the strip.
 */
export class MapRow extends Record {
    constructor(Kind, Heat, SelfMod, Comment$) {
        super();
        this.Kind = Kind;
        this.Heat = (Heat | 0);
        this.SelfMod = SelfMod;
        this.Comment = Comment$;
    }
}

export function MapRow_$reflection() {
    return record_type("JetpacFR.Core.CtrlMapModel.MapRow", [], MapRow, () => [["Kind", RowKind_$reflection()], ["Heat", int32_type], ["SelfMod", bool_type], ["Comment", bool_type]]);
}

export class LabelInfo extends Record {
    constructor(Addr, Text$) {
        super();
        this.Addr = (Addr | 0);
        this.Text = Text$;
    }
}

export function LabelInfo_$reflection() {
    return record_type("JetpacFR.Core.CtrlMapModel.LabelInfo", [], LabelInfo, () => [["Addr", int32_type], ["Text", string_type]]);
}

/**
 * Instruction-level readout, present only at high zoom.
 */
export class InsnSnippet extends Record {
    constructor(Addr, Bytes, Text$, Comment$) {
        super();
        this.Addr = (Addr | 0);
        this.Bytes = Bytes;
        this.Text = Text$;
        this.Comment = Comment$;
    }
}

export function InsnSnippet_$reflection() {
    return record_type("JetpacFR.Core.CtrlMapModel.InsnSnippet", [], InsnSnippet, () => [["Addr", int32_type], ["Bytes", string_type], ["Text", string_type], ["Comment", option_type(string_type)]]);
}

export class MapRender extends Record {
    constructor(ViewStart, ViewEnd, Rows, Labels, Ranges, Snippets) {
        super();
        this.ViewStart = (ViewStart | 0);
        this.ViewEnd = (ViewEnd | 0);
        this.Rows = Rows;
        this.Labels = Labels;
        this.Ranges = Ranges;
        this.Snippets = Snippets;
    }
}

export function MapRender_$reflection() {
    return record_type("JetpacFR.Core.CtrlMapModel.MapRender", [], MapRender, () => [["ViewStart", int32_type], ["ViewEnd", int32_type], ["Rows", array_type(MapRow_$reflection())], ["Labels", array_type(LabelInfo_$reflection())], ["Ranges", array_type(tuple_type(int32_type, int32_type))], ["Snippets", array_type(InsnSnippet_$reflection())]]);
}

/**
 * Address -> fractional row index.
 */
export function addrY(viewStart, viewEnd, rowCount, addr) {
    return (rowCount * (addr - viewStart)) / max(1, viewEnd - viewStart);
}

/**
 * Top address of pixel row y.
 */
export function rowLo(viewStart, viewEnd, rowCount, y) {
    return (viewStart + ~~(((viewEnd - viewStart) * y) / rowCount)) | 0;
}

function classify(blocks, addr) {
    const matchValue = tryFind((b) => {
        if (b.Start <= addr) {
            return addr < b.EndExcl;
        }
        else {
            return false;
        }
    }, blocks);
    if (matchValue != null) {
        const matchValue_1 = matchValue.Kind;
        switch (matchValue_1.tag) {
            case 1:
                return RowKind.DataR;
            case 2:
                return RowKind.GapR;
            default:
                return RowKind.CodeR;
        }
    }
    else {
        return RowKind.UnmappedR;
    }
}

/**
 * Build the render for the current viewport.
 * 
 * counts/selfMod follow Trace.PerPcCount/SelfModified conventions
 * (indexed by address); pass empty arrays when no trace exists.
 */
export function render(blocks, comments, instrStarts, mem, counts, selfMod, viewStart, viewEnd, rowCount) {
    let memory_1, starts_1, snips, a_4, guard;
    const viewStart_1 = max(0, min(65535, viewStart)) | 0;
    const viewEnd_1 = max(viewStart_1 + 1, min(65536, viewEnd)) | 0;
    const span = (viewEnd_1 - viewStart_1) | 0;
    const ppb = rowCount / span;
    const maxC = ((counts.length === 0) ? 1 : max(1, max_1(counts, {
        Compare: (x, y) => (comparePrimitives(x, y) | 0),
    }))) | 0;
    const logMax = Math.log10(maxC);
    const covered = fill(new Array(65536), 0, 65536, false);
    const enumerator = getEnumerator(comments);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const m = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            if (!equals(m.Kind, CommentKind.Frames)) {
                const hi = (equals(m.Kind, CommentKind.Line) ? (m.Addr + 1) : max(m.Addr, m.EndExcl)) | 0;
                const lo = min(65535, m.Addr) | 0;
                const hi_1 = min(65536, hi) | 0;
                for (let a_1 = lo; a_1 <= (hi_1 - 1); a_1++) {
                    setItem(covered, a_1, true);
                }
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const samples = toArray(delay(() => map((i) => (~~((i * span) / 8) | 0), rangeDouble(0, 1, 7))));
    const rows = fill(new Array(rowCount), 0, rowCount, null);
    for (let y_1 = 0; y_1 <= (rowCount - 1); y_1++) {
        const lo_1 = min(rowLo(viewStart_1, viewEnd_1, rowCount, y_1), viewEnd_1 - 1) | 0;
        const hi_2 = max(lo_1 + 1, rowLo(viewStart_1, viewEnd_1, rowCount, y_1 + 1)) | 0;
        const distinct = Array_distinct(map_1((off) => classify(blocks, min(65535, lo_1 + off)), samples), {
            Equals: equals,
            GetHashCode: (x_1) => (safeHash(x_1) | 0),
        });
        const kind = (distinct.length === 1) ? item(0, distinct) : (((distinct.length === 2) && contains(RowKind.UnmappedR, distinct, {
            Equals: equals,
            GetHashCode: (x_2) => (safeHash(x_2) | 0),
        })) ? find((y_4) => !equals(RowKind.UnmappedR, y_4), distinct) : RowKind.MixedR);
        const heat = max_1(map_1((off_1) => {
            const a = (lo_1 + off_1) | 0;
            const c = ((a < counts.length) ? item(a, counts) : 0) | 0;
            if (c <= 0) {
                return 0;
            }
            else {
                return min(9, ~~(9 * (Math.log10(c) / logMax))) | 0;
            }
        }, samples, Int32Array), {
            Compare: (x_4, y_5) => (comparePrimitives(x_4, y_5) | 0),
        }) | 0;
        const selfModHit = samples.some((off_2) => {
            const a_2 = (lo_1 + off_2) | 0;
            if (a_2 < selfMod.length) {
                return item(a_2, selfMod);
            }
            else {
                return false;
            }
        });
        let hasComment;
        const a0 = min(65535, lo_1) | 0;
        const a1 = min(65535, hi_2 - 1) | 0;
        hasComment = (item(a0, covered) ? true : item(a1, covered));
        setItem(rows, y_1, new MapRow(kind, heat, selfModHit, hasComment));
    }
    return new MapRender(viewStart_1, viewEnd_1, rows, toArray_1(choose((b) => {
        const y0 = addrY(viewStart_1, viewEnd_1, rowCount, b.Start);
        if ((((addrY(viewStart_1, viewEnd_1, rowCount, b.EndExcl) - y0) >= 10) && (b.Start < viewEnd_1)) && (b.EndExcl > viewStart_1)) {
            return new LabelInfo(b.Start, b.Name);
        }
        else {
            return undefined;
        }
    }, blocks)), toArray_1(filter((tupledArg) => (tupledArg[1] > tupledArg[0]), map_2((m_2) => [max(viewStart_1, m_2.Addr), min(viewEnd_1, m_2.EndExcl)], filter((m_1) => {
        if (equals(m_1.Kind, CommentKind.Range$)) {
            return m_1.EndExcl > m_1.Addr;
        }
        else {
            return false;
        }
    }, comments)))), (instrStarts != null) ? ((mem != null) ? ((ppb >= 8) ? ((memory_1 = mem, (starts_1 = instrStarts, (snips = empty(), (a_4 = viewStart_1, (guard = 0, ((() => {
        while ((a_4 < viewEnd_1) && (guard < 256)) {
            guard = ((guard + 1) | 0);
            while (((a_4 < viewEnd_1) && (a_4 < 65536)) && !item(a_4, starts_1)) {
                a_4 = ((a_4 + 1) | 0);
            }
            if ((a_4 < viewEnd_1) && (a_4 < 65536)) {
                const insn = disasmMemory(memory_1, a_4);
                const len = max(1, insn.Length) | 0;
                const hex = join(" ", delay(() => map((i_1) => {
                    const arg = item((a_4 + i_1) & 65535, memory_1);
                    return toText(printf("%02X"))(arg);
                }, rangeDouble(0, 1, len - 1))));
                let cmt;
                const option_1 = tryFind((m_3) => {
                    if (equals(m_3.Kind, CommentKind.Line)) {
                        return m_3.Addr === a_4;
                    }
                    else {
                        return false;
                    }
                }, comments);
                cmt = ((option_1 != null) ? option_1.Text : undefined);
                snips = cons(new InsnSnippet(a_4, hex, insn.Text, cmt), snips);
                a_4 = ((a_4 + len) | 0);
            }
        }
    })(), toArray_1(reverse(snips))))))))) : []) : []) : []);
}

