
import { Machine__Step, Machine__CycleCount, Machine__get_FrameEnd, Machine__get_Regs, RegisterFile__Pc } from "../Jetpac2.Core/Machine.js";
import { compare } from "../fable_modules/fable-library-js.5.13.0/BigInt.js";
import { Z80Op } from "../Jetpac2.Core/Z80Asm.js";
import { empty, cons, reverse, head, tail, isEmpty } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { map, initialize, item } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { rows as rows_1 } from "../Jetpac2.Core/Z80Decode.js";
import { max, min } from "../fable_modules/fable-library-js.5.13.0/Double.js";
import { disasmMemory } from "./Disasm.js";
import { tryGetValue, addToSet } from "../fable_modules/fable-library-js.5.13.0/MapUtil.js";
import { defaultOf, disposeSafe, comparePrimitives, getEnumerator } from "../fable_modules/fable-library-js.5.13.0/Util.js";
import { sort } from "../fable_modules/fable-library-js.5.13.0/Seq.js";
import { join, printf, toText } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { StringBuilder__AppendLine_Z721C83C5, StringBuilder_$ctor } from "../fable_modules/fable-library-js.5.13.0/System.Text.js";
import { toString, FSharpRef } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { value as value_8 } from "../fable_modules/fable-library-js.5.13.0/Option.js";

function rawOp(bytes) {
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

/**
 * Emit the F# `z80 { ... }` body for the binary: named ops, symbolic
 * labels for in-range jump targets, raw blocks for the rest.
 */
export function toSource(image, start, count) {
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
    const labels = new Map([]);
    let n = 0;
    const enumerator = getEnumerator(sort(targets, {
        Compare: (x, y) => (comparePrimitives(x, y) | 0),
    }));
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            let arg;
            const t_1 = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]() | 0;
            labels.set(t_1, (arg = (n | 0), toText(printf("lbl%d"))(arg)));
            n = ((n + 1) | 0);
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const sb = StringBuilder_$ctor();
    StringBuilder__AppendLine_Z721C83C5(sb, "z80 {");
    let idx = 0;
    let pc_1 = start;
    while (pc_1 < limit) {
        let arg_2;
        if ((idx < sites.length) && (pc_1 === item(idx, sites)[0])) {
            const patternInput_1 = item(idx, sites);
            const row_1 = patternInput_1[1];
            let matchValue_1;
            let outArg = defaultOf();
            matchValue_1 = [tryGetValue(labels, patternInput_1[0], new FSharpRef(() => outArg, (v) => {
                outArg = v;
            })), outArg];
            if (matchValue_1[0]) {
                StringBuilder__AppendLine_Z721C83C5(sb, toText(printf("  Z80.at %s"))(matchValue_1[1]));
            }
            if (row_1.LabelName != null) {
                const t_2 = targetOf(patternInput_1[2], pc_1) | 0;
                let matchValue_2;
                let outArg_1 = defaultOf();
                matchValue_2 = [tryGetValue(labels, t_2, new FSharpRef(() => outArg_1, (v_1) => {
                    outArg_1 = v_1;
                })), outArg_1];
                if (matchValue_2[0]) {
                    StringBuilder__AppendLine_Z721C83C5(sb, (arg_2 = value_8(row_1.LabelName), toText(printf("  Z80.%s %s"))(arg_2)(matchValue_2[1])));
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
            const hex = join("; ", map((b) => toText(printf("0x%02Xuy"))(b), initialize(rawEnd - pc_1, (i) => item(pc_1 + i, image), Uint8Array)));
            StringBuilder__AppendLine_Z721C83C5(sb, toText(printf("  yield! [| %s |]"))(hex));
            pc_1 = (rawEnd | 0);
        }
    }
    StringBuilder__AppendLine_Z721C83C5(sb, "  }");
    return toString(sb);
}

