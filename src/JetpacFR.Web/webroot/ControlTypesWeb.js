
import { toString, Record, Union } from "./fable_modules/fable-library-js.5.13.0/Types.js";
import { bool_type, list_type, record_type, string_type, int32_type, union_type } from "./fable_modules/fable-library-js.5.13.0/Reflection.js";
import { collect, empty, singleton, delay, toList } from "./fable_modules/fable-library-js.5.13.0/Seq.js";
import { tail as tail_1, head as head_1, cons, skipWhile, skip, ofArrayWithTail, map, tryHead, tryFind, singleton as singleton_1, append, filter, sortBy, isEmpty, empty as empty_1 } from "./fable_modules/fable-library-js.5.13.0/List.js";
import { compareArrays, comparePrimitives, defaultOf, equals, disposeSafe, getEnumerator } from "./fable_modules/fable-library-js.5.13.0/Util.js";
import { isNullOrWhiteSpace, printf, toText } from "./fable_modules/fable-library-js.5.13.0/String.js";
import { rangeDouble } from "./fable_modules/fable-library-js.5.13.0/Range.js";
import { orElse } from "./fable_modules/fable-library-js.5.13.0/Option.js";

/**
 * Web shim for the control-file model: same shapes as JetpacFR.Core's
 * ControlFile.fs minus everything System.IO (the desktop keeps the
 * canonical file-backed implementation; the browser persists through
 * localStorage + export/import instead). Exists so CtrlMapModel.fs and
 * the web UI can share the desktop's block/comment vocabulary.
 */
export class BlockKind extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Code", "Data", "Gap"];
    }
    static Code = new BlockKind(0, []);
    static Data = new BlockKind(1, []);
    static Gap = new BlockKind(2, []);
}

export function BlockKind_$reflection() {
    return union_type("JetpacFR.Core.BlockKind", [], BlockKind, () => [[], [], []]);
}

export class Block extends Record {
    constructor(Start, EndExcl, Name, Kind) {
        super();
        this.Start = (Start | 0);
        this.EndExcl = (EndExcl | 0);
        this.Name = Name;
        this.Kind = Kind;
    }
}

export function Block_$reflection() {
    return record_type("JetpacFR.Core.Block", [], Block, () => [["Start", int32_type], ["EndExcl", int32_type], ["Name", string_type], ["Kind", BlockKind_$reflection()]]);
}

export class CommentKind extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Line", "Name", "Range", "Exec"];
    }
    static Line = new CommentKind(0, []);
    static Name = new CommentKind(1, []);
    static Range$ = new CommentKind(2, []);
    static Exec = new CommentKind(3, []);
}

export function CommentKind_$reflection() {
    return union_type("JetpacFR.Core.CommentKind", [], CommentKind, () => [[], [], [], []]);
}

export class ControlComment extends Record {
    constructor(Kind, Addr, EndExcl, InstrIndex, Text$) {
        super();
        this.Kind = Kind;
        this.Addr = (Addr | 0);
        this.EndExcl = (EndExcl | 0);
        this.InstrIndex = (InstrIndex | 0);
        this.Text = Text$;
    }
}

export function ControlComment_$reflection() {
    return record_type("JetpacFR.Core.ControlComment", [], ControlComment, () => [["Kind", CommentKind_$reflection()], ["Addr", int32_type], ["EndExcl", int32_type], ["InstrIndex", int32_type], ["Text", string_type]]);
}

export class ControlFile extends Record {
    constructor(ImageFile, Start, EndExcl, EntryPc, ActiveVersion, Blocks, Comments, Dirty) {
        super();
        this.ImageFile = ImageFile;
        this.Start = (Start | 0);
        this.EndExcl = (EndExcl | 0);
        this.EntryPc = (EntryPc | 0);
        this.ActiveVersion = (ActiveVersion | 0);
        this.Blocks = Blocks;
        this.Comments = Comments;
        this.Dirty = Dirty;
    }
}

export function ControlFile_$reflection() {
    return record_type("JetpacFR.Core.ControlFile", [], ControlFile, () => [["ImageFile", string_type], ["Start", int32_type], ["EndExcl", int32_type], ["EntryPc", int32_type], ["ActiveVersion", int32_type], ["Blocks", list_type(Block_$reflection())], ["Comments", list_type(ControlComment_$reflection())], ["Dirty", bool_type]]);
}

export function ControlFileModule_empty(start, endExcl) {
    return new ControlFile("original.bin", start, endExcl, start, -1, toList(delay(() => ((endExcl > start) ? singleton(new Block(start, endExcl, "whole_span", BlockKind.Code)) : empty()))), empty_1(), false);
}

export function ControlFileModule_kindToString(_arg) {
    switch (_arg.tag) {
        case 1:
            return "name";
        case 2:
            return "range";
        case 3:
            return "exec";
        default:
            return "line";
    }
}

function ControlFileModule_kindFromString(s) {
    switch (s) {
        case "name":
            return CommentKind.Name;
        case "range":
            return CommentKind.Range$;
        case "exec":
            return CommentKind.Exec;
        default:
            return CommentKind.Line;
    }
}

function ControlFileModule_kindToBlockString(_arg) {
    switch (_arg.tag) {
        case 1:
            return "data";
        case 2:
            return "gap";
        default:
            return "code";
    }
}

function ControlFileModule_blockKindFromString(s) {
    switch (s) {
        case "data":
            return BlockKind.Data;
        case "gap":
            return BlockKind.Gap;
        default:
            return BlockKind.Code;
    }
}

/**
 * Serialize to the control.json shape (desktop-compatible).
 */
export function ControlFileModule_toJson(c) {
    const root = ({});
    root.image = c.ImageFile;
    root.start = c.Start;
    root.endExcl = c.EndExcl;
    root.entryPc = c.EntryPc;
    if (c.ActiveVersion >= 0) {
        root.activeVersion = c.ActiveVersion;
    }
    const blocks = ([]);
    const enumerator = getEnumerator(c.Blocks);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const b = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const bo = ({});
            bo.start = b.Start;
            bo.endExcl = b.EndExcl;
            bo.name = b.Name;
            bo.kind = ControlFileModule_kindToBlockString(b.Kind);
            blocks.push(bo);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    root.blocks = blocks;
    if (!isEmpty(c.Comments)) {
        const arr = ([]);
        const enumerator_1 = getEnumerator(c.Comments);
        try {
            while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
                const m = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const mo = ({});
                mo.kind = ControlFileModule_kindToString(m.Kind);
                if (equals(m.Kind, CommentKind.Exec)) {
                    mo.instrIndex = m.InstrIndex;
                }
                else {
                    mo.addr = m.Addr;
                }
                if (equals(m.Kind, CommentKind.Name) ? true : equals(m.Kind, CommentKind.Range$)) {
                    mo.endExcl = m.EndExcl;
                }
                mo.text = m.Text;
                arr.push(mo);
            }
        }
        finally {
            disposeSafe(enumerator_1);
        }
        root.comments = arr;
    }
    return JSON.stringify(root, null, 2);
}

export function ControlFileModule_fromJson(text) {
    let matchValue;
    const root = JSON.parse(text);
    const prop = (k) => ((root[k] === undefined ? null : root[k]));
    const intOf = (k_2, def_1) => {
        const matchValue_1 = prop(k_2);
        if (equals(matchValue_1, defaultOf())) {
            return def_1 | 0;
        }
        else {
            return matchValue_1 | 0;
        }
    };
    let blocks;
    const matchValue_2 = prop("blocks");
    if (equals(matchValue_2, defaultOf())) {
        blocks = empty_1();
    }
    else {
        const e = matchValue_2;
        blocks = toList(delay(() => collect((i) => {
            let matchValue_3, matchValue_4;
            const bo = e[i];
            const get$ = (k_3) => ((bo[k_3] === undefined ? null : bo[k_3]));
            const start = get$("start") | 0;
            return singleton(new Block(start, get$("endExcl"), (matchValue_3 = get$("name"), equals(matchValue_3, defaultOf()) ? toText(printf("block_%04X"))(start) : toString(matchValue_3)), (matchValue_4 = get$("kind"), equals(matchValue_4, defaultOf()) ? BlockKind.Code : ControlFileModule_blockKindFromString(toString(matchValue_4)))));
        }, rangeDouble(0, 1, (e.length) - 1))));
    }
    let comments;
    const matchValue_5 = prop("comments");
    if (equals(matchValue_5, defaultOf())) {
        comments = empty_1();
    }
    else {
        const e_1 = matchValue_5;
        comments = toList(delay(() => collect((i_1) => {
            let matchValue_9, matchValue_10;
            const mo = e_1[i_1];
            const get$_1 = (k_4) => ((mo[k_4] === undefined ? null : mo[k_4]));
            let kind_1;
            const matchValue_6 = get$_1("kind");
            kind_1 = (equals(matchValue_6, defaultOf()) ? CommentKind.Line : ControlFileModule_kindFromString(toString(matchValue_6)));
            let addr;
            const matchValue_7 = get$_1("addr");
            addr = (equals(matchValue_7, defaultOf()) ? 0 : matchValue_7);
            let idx;
            const matchValue_8 = get$_1("instrIndex");
            idx = (equals(matchValue_8, defaultOf()) ? -1 : matchValue_8);
            return singleton(new ControlComment(kind_1, addr, (matchValue_9 = get$_1("endExcl"), equals(matchValue_9, defaultOf()) ? 0 : matchValue_9), idx, (matchValue_10 = get$_1("text"), equals(matchValue_10, defaultOf()) ? "" : toString(matchValue_10))));
        }, rangeDouble(0, 1, (e_1.length) - 1))));
    }
    const start_1 = intOf("start", 0) | 0;
    return new ControlFile((matchValue = prop("image"), equals(matchValue, defaultOf()) ? "original.bin" : toString(matchValue)), start_1, intOf("endExcl", (start_1 > 0) ? 65536 : 0), intOf("entryPc", start_1), intOf("activeVersion", -1), sortBy((b) => (b.Start | 0), blocks, {
        Compare: (x, y) => (comparePrimitives(x, y) | 0),
    }), comments, false);
}

/**
 * Upsert by key: line keyed on Addr, exec on InstrIndex, name/range on
 * (Addr, EndExcl). Empty text removes the entry.
 */
export function ControlFileModule_upsert(c, m) {
    const others = filter((arg) => {
        let existing, matchValue;
        return !((existing = arg, (matchValue = m.Kind, (matchValue.tag === 3) ? (equals(existing.Kind, CommentKind.Exec) && (existing.InstrIndex === m.InstrIndex)) : ((matchValue.tag === 1) ? ((equals(existing.Kind, m.Kind) && (existing.Addr === m.Addr)) && (existing.EndExcl === m.EndExcl)) : ((matchValue.tag === 2) ? ((equals(existing.Kind, m.Kind) && (existing.Addr === m.Addr)) && (existing.EndExcl === m.EndExcl)) : (equals(existing.Kind, CommentKind.Line) && (existing.Addr === m.Addr)))))));
    }, c.Comments);
    if (isNullOrWhiteSpace(m.Text)) {
        return new ControlFile(c.ImageFile, c.Start, c.EndExcl, c.EntryPc, c.ActiveVersion, c.Blocks, others, true);
    }
    else {
        return new ControlFile(c.ImageFile, c.Start, c.EndExcl, c.EntryPc, c.ActiveVersion, c.Blocks, append(others, singleton_1(new ControlComment(m.Kind, m.Addr, m.EndExcl, m.InstrIndex, m.Text.trim()))), true);
    }
}

/**
 * The comment shown on an address row: exact Line hit first, else any
 * covering Name/Range (most specific wins).
 */
export function ControlFileModule_commentAt(c, addr) {
    const matchValue = orElse(tryFind((m_2) => {
        if (equals(m_2.Kind, CommentKind.Line)) {
            return m_2.Addr === addr;
        }
        else {
            return false;
        }
    }, c.Comments), tryHead(sortBy((m_1) => [equals(m_1.Kind, CommentKind.Name) ? 1 : 0, m_1.EndExcl - m_1.Addr], filter((m) => {
        if ((equals(m.Kind, CommentKind.Name) ? true : equals(m.Kind, CommentKind.Range$)) && (m.Addr <= addr)) {
            return addr < m.EndExcl;
        }
        else {
            return false;
        }
    }, c.Comments), {
        Compare: (x, y) => (compareArrays(x, y) | 0),
    })));
    if (matchValue == null) {
        return undefined;
    }
    else {
        return matchValue.Text;
    }
}

export function ControlFileModule_blockAt(c, addr) {
    return tryFind((b) => {
        if (b.Start <= addr) {
            return addr < b.EndExcl;
        }
        else {
            return false;
        }
    }, c.Blocks);
}

function ControlFileModule_withBlocks(c, blocks) {
    return new ControlFile(c.ImageFile, c.Start, c.EndExcl, c.EntryPc, c.ActiveVersion, sortBy((b) => (b.Start | 0), blocks, {
        Compare: (x, y) => (comparePrimitives(x, y) | 0),
    }), c.Comments, true);
}

export function ControlFileModule_renameBlockAt(c, addr, name) {
    return new ControlFile(c.ImageFile, c.Start, c.EndExcl, c.EntryPc, c.ActiveVersion, map((b) => {
        if ((b.Start <= addr) && (addr < b.EndExcl)) {
            return new Block(b.Start, b.EndExcl, name, b.Kind);
        }
        else {
            return b;
        }
    }, c.Blocks), c.Comments, true);
}

export function ControlFileModule_setKindAt(c, addr, kind) {
    return new ControlFile(c.ImageFile, c.Start, c.EndExcl, c.EntryPc, c.ActiveVersion, map((b) => {
        if ((b.Start <= addr) && (addr < b.EndExcl)) {
            return new Block(b.Start, b.EndExcl, b.Name, kind);
        }
        else {
            return b;
        }
    }, c.Blocks), c.Comments, true);
}

export function ControlFileModule_splitBlockAt(c, addr) {
    const matchValue = tryFind((b) => {
        if (b.Start < addr) {
            return addr < b.EndExcl;
        }
        else {
            return false;
        }
    }, c.Blocks);
    if (matchValue != null) {
        const b_1 = matchValue;
        return ControlFileModule_withBlocks(c, ofArrayWithTail([new Block(b_1.Start, addr, b_1.Name, b_1.Kind), new Block(addr, b_1.EndExcl, toText(printf("block_%04X"))(addr), b_1.Kind)], filter((y) => !equals(b_1, y), c.Blocks)));
    }
    else {
        return c;
    }
}

export function ControlFileModule_mergeWithNext(c, addr) {
    const matchValue = ControlFileModule_blockAt(c, addr);
    if (matchValue != null) {
        const b = matchValue;
        const sorted = sortBy((x) => (x.Start | 0), c.Blocks, {
            Compare: (x_1, y) => (comparePrimitives(x_1, y) | 0),
        });
        const matchValue_1 = skip(1, skipWhile((x_2) => !equals(x_2, b), sorted));
        let matchResult, next_1, rest_1;
        if (!isEmpty(matchValue_1)) {
            if (head_1(matchValue_1).Start === b.EndExcl) {
                matchResult = 0;
                next_1 = head_1(matchValue_1);
                rest_1 = tail_1(matchValue_1);
            }
            else {
                matchResult = 1;
            }
        }
        else {
            matchResult = 1;
        }
        switch (matchResult) {
            case 0:
                return ControlFileModule_withBlocks(c, append(cons(new Block(b.Start, next_1.EndExcl, b.Name, b.Kind), rest_1), filter((x_3) => {
                    if (!equals(x_3, b)) {
                        return !equals(x_3, next_1);
                    }
                    else {
                        return false;
                    }
                }, sorted)));
            default:
                return c;
        }
    }
    else {
        return c;
    }
}

