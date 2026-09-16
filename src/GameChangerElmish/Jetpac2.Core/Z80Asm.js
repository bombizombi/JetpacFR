
import { FSharpRef, Record } from "../fable_modules/fable-library-js.5.17.2/Types.js";
import { class_type, option_type, record_type, int32_type, lambda_type, unit_type, array_type, uint8_type, string_type } from "../fable_modules/fable-library-js.5.17.2/Reflection.js";
import { Machine__set_FrameEnd_Z524259C1, Machine_get_GeneratedStep, Machine__get_Memory, Machine__AddMemoryResetHandler_3A5B6456, Machine__AddMemoryWriteHandler_Z3CB4FF01, Machine__ExecuteOne_5007B66A, Machine__Step, Machine__CycleCount, Machine__get_FrameEnd, Machine__get_Regs, RegisterFile__Pc, Machine_$reflection } from "./Machine.js";
import { empty, append, singleton } from "../fable_modules/fable-library-js.5.17.2/List.js";
import { op_Addition, toInt64_unchecked, compare } from "../fable_modules/fable-library-js.5.17.2/BigInt.js";
import { collect, toList } from "../fable_modules/fable-library-js.5.17.2/Seq.js";
import { disposeSafe, getEnumerator, Exception } from "../fable_modules/fable-library-js.5.17.2/Util.js";
import { getSubArray, initialize, fill, copyTo, item } from "../fable_modules/fable-library-js.5.17.2/Array.js";
import { printf, toFail } from "../fable_modules/fable-library-js.5.17.2/String.js";
import { main } from "./Z80Table.js";

/**
 * One instruction in the Z80 DSL. Formlet-style dual meaning, in the
 * spirit of the design note: the structure of the computation determines
 * BOTH the assembled machine-code bytes (via `assemble`) and the direct
 * execution semantics (`Run`, no interpreter needed). The CE body is the
 * single source of truth; the two interpretations are derived from it.
 */
export class Z80Op extends Record {
    constructor(Mnemonic, Bytes, Run, Label, Encode) {
        super();
        this.Mnemonic = Mnemonic;
        this.Bytes = Bytes;
        this.Run = Run;
        this.Label = Label;
        this.Encode = Encode;
    }
}

export function Z80Op_$reflection() {
    return record_type("Jetpac2.Core.Z80Op", [], Z80Op, () => [["Mnemonic", string_type], ["Bytes", array_type(uint8_type)], ["Run", lambda_type(Machine_$reflection(), unit_type)], ["Label", option_type(record_type("Microsoft.FSharp.Core.FSharpRef`1", [int32_type], FSharpRef, () => [["contents", int32_type]]))], ["Encode", option_type(lambda_type(int32_type, array_type(uint8_type)))]]);
}

/**
 * The computation-expression builder. Instruction values (`Z80.INC_A`,
 * `Z80.LD_HL 16384`, ...) appear as CE lines; raw byte blocks appear via
 * `yield! [| ... |]` and keep their interpreter semantics until they are
 * disassembled into CE form. CE code and raw bytes can call each other
 * freely (CALL/JR cross the boundary because both mutate the same machine).
 */
export class Z80Builder {
    constructor() {
    }
}

export function Z80Builder_$reflection() {
    return class_type("Jetpac2.Core.Z80Builder", undefined, Z80Builder);
}

export function Z80Builder_$ctor() {
    return new Z80Builder();
}

export function Z80Builder__Yield_2B0B22E8(_, o) {
    return singleton(o);
}

export function Z80Builder__Yield_34270A2A(_, xs) {
    return xs;
}

export function Z80Builder__YieldFrom_34270A2A(_, xs) {
    return xs;
}

/**
 * Raw bytes not yet disassembled into CE form: they execute through the
 * interpreter (Machine.Step) until PC leaves the block or the current
 * frame ends (a looping block must be frame-bounded to return).
 */
export function Z80Builder__YieldFrom_Z3F6BC7B1(_, bytes) {
    return singleton(new Z80Op("raw", bytes, (m) => {
        const start = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
        const limit = ((start + bytes.length) & 65535) | 0;
        const frameEnd = Machine__get_FrameEnd(m);
        let pc = RegisterFile__Pc(Machine__get_Regs(m));
        while (((pc >= start) && (pc < limit)) && (compare(Machine__CycleCount(m), frameEnd) < 0)) {
            Machine__Step(m);
            pc = (RegisterFile__Pc(Machine__get_Regs(m)) | 0);
        }
    }, undefined, undefined));
}

export function Z80Builder__Combine_Z72D0BAC0(_, a, b) {
    return append(a, b);
}

export function Z80Builder__Delay_535E0280(_, f) {
    return f();
}

export function Z80Builder__Zero(_) {
    return empty();
}

export function Z80Builder__ReturnFrom_34270A2A(_, xs) {
    return xs;
}

/**
 * Plain let bindings inside the CE - generated programs declare their
 * label cells this way (`let screenClear = Z80.label ()`) so an emitted
 * body is self-contained without touching the enclosing module.
 */
export function Z80Builder__Let_Z4EE50B04(_, x, f) {
    return f(x);
}

/**
 * Unrolled repetition at assembly time (structure is static; a bounded
 * while emits the body repeatedly).
 */
export function Z80Builder__For_Z4AEB52B(_, xs, f) {
    return toList(collect(f, xs));
}

export function Z80Builder__While_Z26247C40(_, cond, body) {
    let guard = 0;
    let acc = empty();
    while (cond() && (guard < 1000000)) {
        acc = append(acc, body());
        guard = ((guard + 1) | 0);
    }
    if (guard >= 1000000) {
        throw new Exception("Z80 DSL While exceeded 1M unrolled iterations");
    }
    return acc;
}

export const Z80BuilderInstance_z80 = Z80Builder_$ctor();

export function Z80_mkOp(mnemonic, bytes, run) {
    return new Z80Op(mnemonic, bytes, run, undefined, undefined);
}

export function Z80_b16(v) {
    return new Uint8Array([(v & 255) & 0xFF, ((v >> 8) & 255) & 0xFF]);
}

/**
 * Dispatch to a Z80Table slot: the machine's memory holds the assembled
 * bytes at PC, so the slot's Fetch/ReadImm read the operands. The CE op
 * therefore IS the validated table arm — track-perfect by construction.
 */
export function Z80_runOf(table, slot) {
    const matchValue = item(slot, table);
    if (matchValue == null) {
        const clo_1 = toFail(printf("Z80 slot %02X not covered by the table"))(slot);
        return (arg_1) => {
            clo_1(arg_1);
        };
    }
    else {
        return matchValue;
    }
}

/**
 * A symbolic address cell, resolved by the two-pass assembler.
 */
export function Z80_label() {
    return new FSharpRef(-1);
}

/**
 * Place a label: a zero-width op whose pass-1 address is recorded into
 * the cell. Forward references work (pass 1 lays out the whole program
 * before pass 2 encodes the jump operands).
 */
export function Z80_at(c) {
    return new Z80Op("label", new Uint8Array([]), (_arg) => {
    }, c, undefined);
}

function Z80_lblOp(mnemonic, bytes, run, c, encode) {
    return new Z80Op(mnemonic, bytes, run, undefined, (addr) => {
        if (c.contents < 0) {
            toFail(printf("unresolved label for %s"))(mnemonic);
        }
        return encode(addr);
    });
}

export function Z80_JP_LBL(c) {
    return Z80_lblOp("JP label", new Uint8Array([195, 0, 0]), Z80_runOf(main, 195), c, (_arg) => (new Uint8Array([195, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_NZ_LBL(c) {
    return Z80_lblOp("JP NZ,label", new Uint8Array([194, 0, 0]), Z80_runOf(main, 194), c, (_arg) => (new Uint8Array([194, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_Z_LBL(c) {
    return Z80_lblOp("JP Z,label", new Uint8Array([202, 0, 0]), Z80_runOf(main, 202), c, (_arg) => (new Uint8Array([202, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_NC_LBL(c) {
    return Z80_lblOp("JP NC,label", new Uint8Array([210, 0, 0]), Z80_runOf(main, 210), c, (_arg) => (new Uint8Array([210, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_C_LBL(c) {
    return Z80_lblOp("JP C,label", new Uint8Array([218, 0, 0]), Z80_runOf(main, 218), c, (_arg) => (new Uint8Array([218, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_PO_LBL(c) {
    return Z80_lblOp("JP PO,label", new Uint8Array([226, 0, 0]), Z80_runOf(main, 226), c, (_arg) => (new Uint8Array([226, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_PE_LBL(c) {
    return Z80_lblOp("JP PE,label", new Uint8Array([234, 0, 0]), Z80_runOf(main, 234), c, (_arg) => (new Uint8Array([234, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_P_LBL(c) {
    return Z80_lblOp("JP P,label", new Uint8Array([242, 0, 0]), Z80_runOf(main, 242), c, (_arg) => (new Uint8Array([242, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JP_M_LBL(c) {
    return Z80_lblOp("JP M,label", new Uint8Array([250, 0, 0]), Z80_runOf(main, 250), c, (_arg) => (new Uint8Array([250, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_LBL(c) {
    return Z80_lblOp("CALL label", new Uint8Array([205, 0, 0]), Z80_runOf(main, 205), c, (_arg) => (new Uint8Array([205, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_NZ_LBL(c) {
    return Z80_lblOp("CALL NZ,label", new Uint8Array([196, 0, 0]), Z80_runOf(main, 196), c, (_arg) => (new Uint8Array([196, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_Z_LBL(c) {
    return Z80_lblOp("CALL Z,label", new Uint8Array([204, 0, 0]), Z80_runOf(main, 204), c, (_arg) => (new Uint8Array([204, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_NC_LBL(c) {
    return Z80_lblOp("CALL NC,label", new Uint8Array([212, 0, 0]), Z80_runOf(main, 212), c, (_arg) => (new Uint8Array([212, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_C_LBL(c) {
    return Z80_lblOp("CALL C,label", new Uint8Array([220, 0, 0]), Z80_runOf(main, 220), c, (_arg) => (new Uint8Array([220, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_PO_LBL(c) {
    return Z80_lblOp("CALL PO,label", new Uint8Array([228, 0, 0]), Z80_runOf(main, 228), c, (_arg) => (new Uint8Array([228, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_PE_LBL(c) {
    return Z80_lblOp("CALL PE,label", new Uint8Array([236, 0, 0]), Z80_runOf(main, 236), c, (_arg) => (new Uint8Array([236, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_P_LBL(c) {
    return Z80_lblOp("CALL P,label", new Uint8Array([244, 0, 0]), Z80_runOf(main, 244), c, (_arg) => (new Uint8Array([244, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_CALL_M_LBL(c) {
    return Z80_lblOp("CALL M,label", new Uint8Array([252, 0, 0]), Z80_runOf(main, 252), c, (_arg) => (new Uint8Array([252, (c.contents & 255) & 0xFF, ((c.contents >> 8) & 255) & 0xFF])));
}

export function Z80_JR_LBL(c) {
    return Z80_lblOp("JR label", new Uint8Array([24, 0]), Z80_runOf(main, 24), c, (addr) => (new Uint8Array([24, ((c.contents - (addr + 2)) & 255) & 0xFF])));
}

export function Z80_JR_NZ_LBL(c) {
    return Z80_lblOp("JR NZ,label", new Uint8Array([32, 0]), Z80_runOf(main, 32), c, (addr) => (new Uint8Array([32, ((c.contents - (addr + 2)) & 255) & 0xFF])));
}

export function Z80_JR_Z_LBL(c) {
    return Z80_lblOp("JR Z,label", new Uint8Array([40, 0]), Z80_runOf(main, 40), c, (addr) => (new Uint8Array([40, ((c.contents - (addr + 2)) & 255) & 0xFF])));
}

export function Z80_JR_NC_LBL(c) {
    return Z80_lblOp("JR NC,label", new Uint8Array([48, 0]), Z80_runOf(main, 48), c, (addr) => (new Uint8Array([48, ((c.contents - (addr + 2)) & 255) & 0xFF])));
}

export function Z80_JR_C_LBL(c) {
    return Z80_lblOp("JR C,label", new Uint8Array([56, 0]), Z80_runOf(main, 56), c, (addr) => (new Uint8Array([56, ((c.contents - (addr + 2)) & 255) & 0xFF])));
}

export function Z80_DJNZ_LBL(c) {
    return Z80_lblOp("DJNZ label", new Uint8Array([16, 0]), Z80_runOf(main, 16), c, (addr) => (new Uint8Array([16, ((c.contents - (addr + 2)) & 255) & 0xFF])));
}

/**
 * Assemble a program into its machine-code bytes (structure -> bytes).
 * Two passes: pass 1 lays out the program and resolves labels; pass 2
 * emits the final bytes (position-aware encodings read the label cells).
 */
export function Z80_assemble(program) {
    let addr = 0;
    const enumerator = getEnumerator(program);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const o = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const matchValue = o.Label;
            if (matchValue == null) {
                addr = ((addr + o.Bytes.length) | 0);
            }
            else {
                const c = matchValue;
                c.contents = (addr | 0);
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    const buf = new Uint8Array(addr);
    let i = 0;
    const enumerator_1 = getEnumerator(program);
    try {
        while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
            const o_1 = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]();
            if (o_1.Label == null) {
                let bytes;
                const matchValue_2 = o_1.Encode;
                bytes = ((matchValue_2 == null) ? o_1.Bytes : matchValue_2(i));
                copyTo(bytes, 0, buf, i, bytes.length);
                i = ((i + bytes.length) | 0);
            }
        }
    }
    finally {
        disposeSafe(enumerator_1);
    }
    return buf;
}

/**
 * Run a LINEAR program directly. Normal CE operations pass through the
 * machine's common dispatch wrapper so co-hooks and replacement hooks have
 * the same ordering as frame execution. Raw blocks already call Step per
 * instruction and therefore must not receive an additional outer hook.
 */
export function Z80_run(program, m) {
    const enumerator = getEnumerator(program);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const o = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            if ((o.Mnemonic === "raw") ? true : (o.Label != null)) {
                o.Run(m);
            }
            else {
                Machine__ExecuteOne_5007B66A(m, (_arg) => {
                    o.Run(m);
                });
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

/**
 * PC-indexed CE entries plus reverse byte dependencies. Invalid entries
 * deliberately fall back to the generated interpreter, which reads the
 * current opcode from memory and is therefore safe for arbitrary SMC.
 */
export class Z80_ExecutionIndex {
    constructor(program, baseAddress) {
        this.baseAddress = (baseAddress | 0);
        this.table = fill(new Array(65536), 0, 65536, null);
        this.valid = fill(new Array(65536), 0, 65536, false);
        this.dependents = initialize(65536, (_arg) => []);
        this.entries = [];
        this.attachedMachine = undefined;
        const assembled = Z80_assemble(program);
        let offset = 0;
        const enumerator = getEnumerator(program);
        try {
            while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                const o = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
                const matchValue = o.Label;
                if (matchValue == null) {
                    const start = ((this.baseAddress + offset) & 65535) | 0;
                    const length = o.Bytes.length | 0;
                    if (((length > 0) && (o.Mnemonic !== "raw")) && ((offset + length) <= assembled.length)) {
                        this.table[start] = o;
                        this.valid[start] = true;
                        void (this.entries.push([start, offset, getSubArray(assembled, offset, length)]));
                        for (let i = 0; i <= (length - 1); i++) {
                            void (item((start + i) & 65535, this.dependents).push(start));
                        }
                    }
                    offset = ((offset + length) | 0);
                }
                else {
                    const c = matchValue;
                    c.contents = (offset | 0);
                }
            }
        }
        finally {
            disposeSafe(enumerator);
        }
    }
}

export function Z80_ExecutionIndex_$reflection() {
    return class_type("Jetpac2.Core.Z80.ExecutionIndex", undefined, Z80_ExecutionIndex);
}

export function Z80_ExecutionIndex_$ctor_14B5E936(program, baseAddress) {
    return new Z80_ExecutionIndex(program, baseAddress);
}

export function Z80_ExecutionIndex__get_BaseAddress(_) {
    return (_.baseAddress & 65535) | 0;
}

export function Z80_ExecutionIndex__Lookup_Z524259A4(this$, address) {
    const addr = (address & 65535) | 0;
    if (item(addr, this$.valid)) {
        return item(addr, this$.table);
    }
    else {
        return undefined;
    }
}

export function Z80_ExecutionIndex__Invalidate_Z524259A4(this$, address) {
    let enumerator = getEnumerator(item(address & 65535, this$.dependents));
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const start = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]() | 0;
            this$.valid[start] = false;
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

export function Z80_ExecutionIndex__Refresh_Z3F6BC7B1(this$, memory) {
    let enumerator = getEnumerator(this$.entries);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const forLoopVar = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const start = forLoopVar[0] | 0;
            const expected = forLoopVar[2];
            let matches = true;
            let i = 0;
            while (matches && (i < expected.length)) {
                if (item((start + i) & 65535, memory) !== item(i, expected)) {
                    matches = false;
                }
                i = ((i + 1) | 0);
            }
            this$.valid[start] = matches;
        }
    }
    finally {
        disposeSafe(enumerator);
    }
}

/**
 * Attach once to a machine. State loads refresh validity from the loaded
 * bytes; changed instruction bytes remain on the interpreter path.
 */
export function Z80_ExecutionIndex__Attach_3EE36980(this$, machine) {
    const matchValue = this$.attachedMachine;
    let matchResult, current_1;
    if (matchValue != null) {
        if (matchValue === machine) {
            matchResult = 0;
            current_1 = matchValue;
        }
        else {
            matchResult = 1;
        }
    }
    else {
        matchResult = 1;
    }
    switch (matchResult) {
        case 0: {
            break;
        }
        case 1: {
            this$.attachedMachine = machine;
            Machine__AddMemoryWriteHandler_Z3CB4FF01(machine, (event) => {
                Z80_ExecutionIndex__Invalidate_Z524259A4(this$, event.Address);
            });
            Machine__AddMemoryResetHandler_3A5B6456(machine, () => {
                Z80_ExecutionIndex__Refresh_Z3F6BC7B1(this$, Machine__get_Memory(machine));
            });
            Z80_ExecutionIndex__Refresh_Z3F6BC7B1(this$, Machine__get_Memory(machine));
            break;
        }
    }
}

export function Z80_ExecutionIndex__get_EntryCount(_) {
    return _.entries.length | 0;
}

export function Z80_ExecutionIndex__IsValid_Z524259A4(this$, address) {
    return item(address & 65535, this$.valid);
}

/**
 * Build an index for a CE image loaded at address zero.
 */
export function Z80_makeIndex(program) {
    return Z80_ExecutionIndex_$ctor_14B5E936(program, 0);
}

/**
 * Build an index for a CE image loaded at an explicit 16-bit address.
 */
export function Z80_makeIndexAt(baseAddress, program) {
    return Z80_ExecutionIndex_$ctor_14B5E936(program, baseAddress);
}

/**
 * Execute one frame. Resolution happens inside ExecuteOne, after hooks,
 * so writes performed by a hook cannot leave a stale CE operation selected.
 */
export function Z80_runFrame(index, m) {
    const frameStart = Machine__CycleCount(m);
    const frameEnd = Machine__get_FrameEnd(m);
    while (compare(Machine__CycleCount(m), frameEnd) < 0) {
        Machine__ExecuteOne_5007B66A(m, (machine) => {
            const matchValue = Z80_ExecutionIndex__Lookup_Z524259A4(index, RegisterFile__Pc(Machine__get_Regs(machine)));
            if (matchValue == null) {
                Machine_get_GeneratedStep()(machine);
            }
            else {
                matchValue.Run(machine);
            }
        });
    }
    Machine__set_FrameEnd_Z524259C1(m, toInt64_unchecked(op_Addition(frameEnd, 69888n)));
    return [frameStart, Machine__CycleCount(m)];
}

