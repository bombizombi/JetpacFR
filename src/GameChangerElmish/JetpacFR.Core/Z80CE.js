
import { Machine__Step, Machine__CycleCount, Machine__get_FrameEnd, Machine__get_Regs, RegisterFile__Pc } from "../Jetpac2.Core/Machine.js";
import { compare } from "../fable_modules/fable-library-js.5.17.2/BigInt.js";
import { Z80Op } from "../Jetpac2.Core/Z80Asm.js";
import { tryFind, map as map_1, filter, append, sort, empty, cons, reverse, head, tail, isEmpty } from "../fable_modules/fable-library-js.5.17.2/List.js";
import { map as map_2, initialize, item } from "../fable_modules/fable-library-js.5.17.2/Array.js";
import { rows as rows_1 } from "../Jetpac2.Core/Z80Decode.js";
import { max, min } from "../fable_modules/fable-library-js.5.17.2/Double.js";
import { disasmMemory } from "./Disasm.js";
import { FSharpSet__Contains, ofSeq } from "../fable_modules/fable-library-js.5.17.2/Set.js";
import { defaultOf, disposeSafe, numberHash, getEnumerator, comparePrimitives } from "../fable_modules/fable-library-js.5.17.2/Util.js";
import { delay, toList, map } from "../fable_modules/fable-library-js.5.17.2/Seq.js";
import { isDigit, isLetterOrDigit } from "../fable_modules/fable-library-js.5.17.2/Char.js";
import { join, printf, toText } from "../fable_modules/fable-library-js.5.17.2/String.js";
import { tryGetValue, addToSet } from "../fable_modules/fable-library-js.5.17.2/MapUtil.js";
import { List_distinct } from "../fable_modules/fable-library-js.5.17.2/Seq2.js";
import { StringBuilder__Append_Z721C83C5, StringBuilder__AppendLine_Z721C83C5, StringBuilder_$ctor } from "../fable_modules/fable-library-js.5.17.2/System.Text.js";
import { toString, FSharpRef } from "../fable_modules/fable-library-js.5.17.2/Types.js";
import { value as value_7 } from "../fable_modules/fable-library-js.5.17.2/Option.js";

/**
 * A raw byte block op (public: the game-project tooling reuses it when
 * checking the parity of emitted per-block programs).
 */
export function rawOp(bytes) {
    return new Z80Op("raw", bytes, (m) => {
        const start = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
        const limit = ((start + bytes.length) & 65535) | 0;
        const frameEnd = Machine__get_FrameEnd(m);
        let pc = RegisterFile__Pc(Machine__get_Regs(m));
        while (((pc >= start) && (pc < limit)) && (compare(Machine__CycleCount(m), frameEnd) < 0)) {
            Machine__Step(m);
            pc = (RegisterFile__Pc(Machine__get_Regs(m)) | 0);
        }
    }, undefined, undefined);
}

/**
 * Decode the instruction at `pc`: the matching row + the concrete op.
 */
export function decode(image, pc) {
    const n = image.length | 0;
    const go = (rows_mut) => {
        go:
        while (true) {
            const rows = rows_mut;
            if (!isEmpty(rows)) {
                const rest = tail(rows);
                const r = head(rows);
                const len = r.Prefix.length | 0;
                if ((pc + len) <= n) {
                    let ok = true;
                    let i = 0;
                    while (ok && (i < len)) {
                        if ((item(i, r.Mask) !== 0) && (item(pc + i, image) !== item(i, r.Prefix))) {
                            ok = false;
                        }
                        i = ((i + 1) | 0);
                    }
                    if (ok) {
                        return [r, r.Make(image, pc)];
                    }
                    else {
                        rows_mut = rest;
                        continue go;
                    }
                }
                else {
                    rows_mut = rest;
                    continue go;
                }
            }
            else {
                return undefined;
            }
            break;
        }
    };
    return go(rows_1);
}

function targetOf(op, pc) {
    if (op.Mnemonic.startsWith("JR") ? true : op.Mnemonic.startsWith("DJNZ")) {
        return (((pc + 2) + ((item(1, op.Bytes) + 0x80 & 0xFF) - 0x80)) & 65535) | 0;
    }
    else {
        return (~~item(1, op.Bytes) | (~~item(2, op.Bytes) << 8)) | 0;
    }
}

/**
 * Disassemble `count` bytes starting at `start` into CE ops. Covered
 * instructions become their ops; everything else merges into raw blocks.
 */
export function toOps(image, start, count) {
    const limit = min(start + count, image.length) | 0;
    const walk = (pc_mut, acc_mut) => {
        walk:
        while (true) {
            const pc = pc_mut, acc = acc_mut;
            if (pc >= limit) {
                return reverse(acc);
            }
            else {
                const insn = disasmMemory(image, pc);
                const matchValue = decode(image, pc);
                if (matchValue == null) {
                    let endPc = pc;
                    while ((endPc < limit) && (decode(image, endPc) == null)) {
                        endPc = ((endPc + max(1, disasmMemory(image, endPc).Length)) | 0);
                    }
                    const block = initialize(endPc - pc, (i) => item(pc + i, image), Uint8Array);
                    pc_mut = endPc;
                    acc_mut = cons(rawOp(block), acc);
                    continue walk;
                }
                else {
                    pc_mut = (pc + insn.Length);
                    acc_mut = cons(matchValue[1], acc);
                    continue walk;
                }
            }
            break;
        }
    };
    return walk(start, empty());
}

const fsharpKeywords = ofSeq(["abstract", "and", "as", "assert", "base", "begin", "class", "const", "default", "delegate", "do", "done", "downcast", "downto", "elif", "else", "end", "enum", "exception", "extern", "false", "finally", "fixed", "for", "fun", "function", "global", "if", "in", "inherit", "inline", "interface", "internal", "lazy", "let", "match", "member", "module", "mutable", "namespace", "new", "not", "null", "of", "open", "or", "override", "private", "public", "rec", "return", "sig", "static", "struct", "then", "to", "true", "try", "type", "upcast", "use", "val", "void", "when", "while", "with", "yield"], {
    Compare: (x, y) => (comparePrimitives(x, y) | 0),
});

function sanitizeLabel(used, addr, raw) {
    const cleaned = Array.from(map((ch) => {
        if (isLetterOrDigit(ch) ? true : (ch === "_")) {
            return ch;
        }
        else {
            return "_";
        }
    }, raw.split(""))).join('');
    const baseName = (cleaned.length === 0) ? toText(printf("sym_%04X"))(addr) : (isDigit(cleaned[0]) ? ("_" + cleaned) : cleaned);
    const name = FSharpSet__Contains(fsharpKeywords, baseName.toLowerCase()) ? ("_" + baseName) : baseName;
    if (addToSet(name, used)) {
        return name;
    }
    else {
        let candidate = toText(printf("%s_%04X"))(name)(addr);
        let n = 1;
        while (!addToSet(candidate, used)) {
            let arg_5;
            candidate = ((arg_5 = (n | 0), toText(printf("%s_%04X_%d"))(name)(addr)(arg_5)));
            n = ((n + 1) | 0);
        }
        return candidate;
    }
}

/**
 * Emit the inner lines (no enclosing braces) of the F# `z80 { ... }` body
 * for the binary: named ops, symbolic labels for in-range jump targets,
 * raw blocks for the rest. The composable form used by the project
 * generator to mix code segments with marked data blocks.
 * 
 * `useLabels` false keeps the body purely numeric: no label cells, no
 * `Z80.at` placements, every jump in numeric form - the null-CE shape
 * the CE tab offers for runner testing.
 * 
 * `symbols` names function entry points (from control.json): each address
 * inside the span becomes a named label site (`Z80.at screenClear`) and
 * jump targets there reuse the name; other targets stay `lblN`. All label
 * cells are declared by emitted `let` bindings at the top of the body, so
 * the body compiles standalone inside the CE.
 */
export function toBody(image, start, count, useLabels, symbols) {
    let list_4;
    const limit = min(start + count, image.length) | 0;
    const sites = [];
    const targets = new Set([]);
    const collect = (pc_mut) => {
        collect:
        while (true) {
            const pc = pc_mut;
            if (pc < limit) {
                const matchValue = decode(image, pc);
                if (matchValue == null) {
                    let endPc = pc;
                    while ((endPc < limit) && (decode(image, endPc) == null)) {
                        endPc = ((endPc + max(1, disasmMemory(image, endPc).Length)) | 0);
                    }
                    pc_mut = endPc;
                    continue collect;
                }
                else {
                    const row = matchValue[0];
                    const op = matchValue[1];
                    void (sites.push([pc, row, op]));
                    if (row.LabelName != null) {
                        const t = targetOf(op, pc) | 0;
                        if ((t >= start) && (t < limit)) {
                            addToSet(t, targets);
                        }
                    }
                    pc_mut = (pc + disasmMemory(image, pc).Length);
                    continue collect;
                }
            }
            break;
        }
    };
    collect(start);
    const used = new Set([]);
    const labels = new Map([]);
    const enumerator = getEnumerator(useLabels ? sort(List_distinct((list_4 = toList(targets), append(filter((a_1) => {
        if (a_1 >= start) {
            return a_1 < limit;
        }
        else {
            return false;
        }
    }, map_1((tuple_1) => (tuple_1[0] | 0), symbols)), list_4)), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => (numberHash(x) | 0),
    }), {
        Compare: (x_1, y_1) => (comparePrimitives(x_1, y_1) | 0),
    }) : empty());
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const t_2 = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]() | 0;
            let name;
            let matchValue_1;
            const option_1 = tryFind((tupledArg) => (tupledArg[0] === t_2), symbols);
            matchValue_1 = ((option_1 != null) ? option_1[1] : undefined);
            if (matchValue_1 == null) {
                const n = toText(printf("lbl_%04X"))(t_2);
                name = (addToSet(n, used) ? n : (n + "_dup"));
            }
            else {
                name = sanitizeLabel(used, t_2, matchValue_1);
            }
            labels.set(t_2, name);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const decls = join("\n", toList(delay(() => map((kv) => toText(printf("  let %s = Z80.label ()"))(kv[1]), labels))));
    const sb = StringBuilder_$ctor();
    if (decls.length > 0) {
        StringBuilder__AppendLine_Z721C83C5(sb, decls);
    }
    let idx = 0;
    let pc_1 = start;
    while (pc_1 < limit) {
        let arg_3;
        if ((idx < sites.length) && (pc_1 === item(idx, sites)[0])) {
            const patternInput_1 = item(idx, sites);
            const row_1 = patternInput_1[1];
            let matchValue_2;
            let outArg = defaultOf();
            matchValue_2 = [tryGetValue(labels, patternInput_1[0], new FSharpRef(() => outArg, (v) => {
                outArg = v;
            })), outArg];
            if (matchValue_2[0]) {
                StringBuilder__AppendLine_Z721C83C5(sb, toText(printf("  Z80.at %s"))(matchValue_2[1]));
            }
            if (row_1.LabelName != null) {
                const t_3 = targetOf(patternInput_1[2], pc_1) | 0;
                let matchValue_3;
                let outArg_1 = defaultOf();
                matchValue_3 = [tryGetValue(labels, t_3, new FSharpRef(() => outArg_1, (v_1) => {
                    outArg_1 = v_1;
                })), outArg_1];
                if (matchValue_3[0]) {
                    StringBuilder__AppendLine_Z721C83C5(sb, (arg_3 = value_7(row_1.LabelName), toText(printf("  Z80.%s %s"))(arg_3)(matchValue_3[1])));
                }
                else {
                    StringBuilder__AppendLine_Z721C83C5(sb, "  " + row_1.Format(image, pc_1));
                }
            }
            else {
                StringBuilder__AppendLine_Z721C83C5(sb, "  " + row_1.Format(image, pc_1));
            }
            pc_1 = ((pc_1 + disasmMemory(image, pc_1).Length) | 0);
            idx = ((idx + 1) | 0);
        }
        else {
            const rawEnd = min((idx < sites.length) ? item(idx, sites)[0] : limit, limit) | 0;
            const hex = join("; ", map_2((b) => toText(printf("0x%02Xuy"))(b), initialize(rawEnd - pc_1, (i) => item(pc_1 + i, image), Uint8Array)));
            StringBuilder__AppendLine_Z721C83C5(sb, toText(printf("  yield! [| %s |]"))(hex));
            pc_1 = (rawEnd | 0);
        }
    }
    return toString(sb);
}

/**
 * Wrap a body in the `z80 { ... }` block (the historical toSource shape).
 */
export function toSource(image, start, count, symbols) {
    const sb = StringBuilder_$ctor();
    StringBuilder__AppendLine_Z721C83C5(sb, "z80 {");
    StringBuilder__Append_Z721C83C5(sb, toBody(image, start, count, true, symbols));
    StringBuilder__AppendLine_Z721C83C5(sb, "  }");
    return toString(sb);
}

