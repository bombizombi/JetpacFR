
import { Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { bool_type, int64_type, tuple_type, list_type, record_type, string_type, int32_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { RegSnapshot_$reflection } from "../TraceTypesWeb.js";
import { map as map_1, head, isEmpty, empty as empty_1, ofArray } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { disasmMemory } from "./Disasm.js";
import { item } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { fromInt32, op_Division, toInt64_unchecked, toInt32_unchecked } from "../fable_modules/fable-library-js.5.13.0/BigInt.js";
import { map, empty, singleton, collect, delay, toList } from "../fable_modules/fable-library-js.5.13.0/Seq.js";
import { List_distinct, distinct } from "../fable_modules/fable-library-js.5.13.0/Seq2.js";
import { numberHash, stringHash } from "../fable_modules/fable-library-js.5.13.0/Util.js";

export class ContractInsn extends Record {
    constructor(Address, Text$, Length, Executions, AvgCycles) {
        super();
        this.Address = (Address | 0);
        this.Text = Text$;
        this.Length = (Length | 0);
        this.Executions = (Executions | 0);
        this.AvgCycles = (AvgCycles | 0);
    }
}

export function ContractInsn_$reflection() {
    return record_type("JetpacFR.Core.Contract.ContractInsn", [], ContractInsn, () => [["Address", int32_type], ["Text", string_type], ["Length", int32_type], ["Executions", int32_type], ["AvgCycles", int32_type]]);
}

export class RoutineContract extends Record {
    constructor(Entry, SpanLo, SpanHi, CallCount, Disassembly, WriteRanges, RegisterDelta, InclusiveTStates, Callsites, ResumeAddresses, InputSamples, ExitSamples, SelfModifying, ApiHints) {
        super();
        this.Entry = (Entry | 0);
        this.SpanLo = (SpanLo | 0);
        this.SpanHi = (SpanHi | 0);
        this.CallCount = (CallCount | 0);
        this.Disassembly = Disassembly;
        this.WriteRanges = WriteRanges;
        this.RegisterDelta = RegisterDelta;
        this.InclusiveTStates = InclusiveTStates;
        this.Callsites = Callsites;
        this.ResumeAddresses = ResumeAddresses;
        this.InputSamples = InputSamples;
        this.ExitSamples = ExitSamples;
        this.SelfModifying = SelfModifying;
        this.ApiHints = ApiHints;
    }
}

export function RoutineContract_$reflection() {
    return record_type("JetpacFR.Core.Contract.RoutineContract", [], RoutineContract, () => [["Entry", int32_type], ["SpanLo", int32_type], ["SpanHi", int32_type], ["CallCount", int32_type], ["Disassembly", list_type(ContractInsn_$reflection())], ["WriteRanges", list_type(tuple_type(int32_type, int32_type, int32_type, int32_type))], ["RegisterDelta", list_type(tuple_type(string_type, int32_type, int32_type))], ["InclusiveTStates", int64_type], ["Callsites", list_type(int32_type)], ["ResumeAddresses", list_type(int32_type)], ["InputSamples", list_type(RegSnapshot_$reflection())], ["ExitSamples", list_type(RegSnapshot_$reflection())], ["SelfModifying", bool_type], ["ApiHints", list_type(string_type)]]);
}

function apiHint(b0) {
    let op_1, op_2, op_3, op_4, op_5, op_6, op_7, op_8, op_9, op_10, op_11, op_12, op_13, op_14, op_15, op_16, op_17, op_18, op_19, op_20, op_21, op_22, op_23, op_24, op_25, op_26, op_27, op_28, op_29, op_30, op_31, op_32, op_33, op_34, op_35, op_36, op_37, op_38, op_39, op_40, op_41, op_42, op_43, op_44, op_45, op_46, op_47, op_48, op_49, op_50, op_51, op_52, op_53, op_54;
    const op = ~~b0 | 0;
    let matchResult, op_55, op_56, op_57;
    switch (op) {
        case 203: {
            matchResult = 0;
            break;
        }
        case 211:
        case 219: {
            matchResult = 3;
            break;
        }
        case 221:
        case 253: {
            matchResult = 2;
            break;
        }
        case 237: {
            matchResult = 1;
            break;
        }
        case 2: {
            if ((op_1 = (op | 0), (op_1 >= 192) && (op_1 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_2 = (op | 0), (op_2 >= 128) && (op_2 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_3 = (op | 0), (op_3 >= 64) && (op_3 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 10: {
            if ((op_4 = (op | 0), (op_4 >= 192) && (op_4 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_5 = (op | 0), (op_5 >= 128) && (op_5 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_6 = (op | 0), (op_6 >= 64) && (op_6 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 16: {
            if ((op_7 = (op | 0), (op_7 >= 192) && (op_7 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_8 = (op | 0), (op_8 >= 128) && (op_8 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_9 = (op | 0), (op_9 >= 64) && (op_9 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 10;
            }
            break;
        }
        case 18: {
            if ((op_10 = (op | 0), (op_10 >= 192) && (op_10 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_11 = (op | 0), (op_11 >= 128) && (op_11 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_12 = (op | 0), (op_12 >= 64) && (op_12 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 24: {
            if ((op_13 = (op | 0), (op_13 >= 192) && (op_13 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_14 = (op | 0), (op_14 >= 128) && (op_14 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_15 = (op | 0), (op_15 >= 64) && (op_15 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 10;
            }
            break;
        }
        case 26: {
            if ((op_16 = (op | 0), (op_16 >= 192) && (op_16 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_17 = (op | 0), (op_17 >= 128) && (op_17 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_18 = (op | 0), (op_18 >= 64) && (op_18 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 32: {
            if ((op_19 = (op | 0), (op_19 >= 192) && (op_19 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_20 = (op | 0), (op_20 >= 128) && (op_20 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_21 = (op | 0), (op_21 >= 64) && (op_21 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 10;
            }
            break;
        }
        case 34: {
            if ((op_22 = (op | 0), (op_22 >= 192) && (op_22 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_23 = (op | 0), (op_23 >= 128) && (op_23 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_24 = (op | 0), (op_24 >= 64) && (op_24 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 40: {
            if ((op_25 = (op | 0), (op_25 >= 192) && (op_25 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_26 = (op | 0), (op_26 >= 128) && (op_26 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_27 = (op | 0), (op_27 >= 64) && (op_27 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 10;
            }
            break;
        }
        case 42: {
            if ((op_28 = (op | 0), (op_28 >= 192) && (op_28 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_29 = (op | 0), (op_29 >= 128) && (op_29 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_30 = (op | 0), (op_30 >= 64) && (op_30 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 48: {
            if ((op_31 = (op | 0), (op_31 >= 192) && (op_31 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_32 = (op | 0), (op_32 >= 128) && (op_32 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_33 = (op | 0), (op_33 >= 64) && (op_33 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 10;
            }
            break;
        }
        case 50: {
            if ((op_34 = (op | 0), (op_34 >= 192) && (op_34 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_35 = (op | 0), (op_35 >= 128) && (op_35 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_36 = (op | 0), (op_36 >= 64) && (op_36 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        case 52: {
            if ((op_37 = (op | 0), (op_37 >= 192) && (op_37 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_38 = (op | 0), (op_38 >= 128) && (op_38 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_39 = (op | 0), (op_39 >= 64) && (op_39 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 7;
            }
            break;
        }
        case 53: {
            if ((op_40 = (op | 0), (op_40 >= 192) && (op_40 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_41 = (op | 0), (op_41 >= 128) && (op_41 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_42 = (op | 0), (op_42 >= 64) && (op_42 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 7;
            }
            break;
        }
        case 54: {
            if ((op_43 = (op | 0), (op_43 >= 192) && (op_43 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_44 = (op | 0), (op_44 >= 128) && (op_44 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_45 = (op | 0), (op_45 >= 64) && (op_45 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 8;
            }
            break;
        }
        case 56: {
            if ((op_46 = (op | 0), (op_46 >= 192) && (op_46 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_47 = (op | 0), (op_47 >= 128) && (op_47 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_48 = (op | 0), (op_48 >= 64) && (op_48 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 10;
            }
            break;
        }
        case 58: {
            if ((op_49 = (op | 0), (op_49 >= 192) && (op_49 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_50 = (op | 0), (op_50 >= 128) && (op_50 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_51 = (op | 0), (op_51 >= 64) && (op_51 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 9;
            }
            break;
        }
        default:
            if ((op_52 = (op | 0), (op_52 >= 192) && (op_52 <= 255))) {
                matchResult = 4;
                op_55 = op;
            }
            else if ((op_53 = (op | 0), (op_53 >= 128) && (op_53 <= 191))) {
                matchResult = 5;
                op_56 = op;
            }
            else if ((op_54 = (op | 0), (op_54 >= 64) && (op_54 <= 127))) {
                matchResult = 6;
                op_57 = op;
            }
            else {
                matchResult = 11;
            }
    }
    switch (matchResult) {
        case 0:
            return "Alu.rotate8 / bit / res / set";
        case 1:
            return "Alu block ops / RETI-RETN (IFF flags)";
        case 2:
            return "Regs.Get/Set (IX/IY, displacement via ReadImm)";
        case 3:
            return "Out / In (port I/O)";
        case 4: {
            const low = (op_55 & 15) | 0;
            if (((op_55 === 201) ? true : (low === 0)) ? true : (low === 8)) {
                return "Pop16 + Regs.SetPc (return)";
            }
            else if ((low === 4) ? true : (low === 12)) {
                return "Push16 (return addr) + Regs.SetPc (call)";
            }
            else if (((op_55 === 195) ? true : (low === 2)) ? true : (low === 10)) {
                return "Regs.SetPc (jump)";
            }
            else {
                switch (low) {
                    case 1:
                        return "Pop16";
                    case 5:
                        return "Push16";
                    default:
                        if ((low === 6) ? true : (low === 14)) {
                            return "Alu.* (immediate) + SetFlags";
                        }
                        else if ((low === 7) ? true : (low === 15)) {
                            return "Push16 (RST return addr) + Regs.SetPc";
                        }
                        else {
                            return "Regs.SetPc / Push16 / Pop16";
                        }
                }
            }
        }
        case 5:
            return "Alu.add8/sub8/cmp8/and8/or8/xor8 + SetFlags";
        case 6:
            if (op_57 === 118) {
                return "HALT (PassTime)";
            }
            else if ((((op_57 >> 3) & 7) === 6) ? true : ((op_57 & 7) === 6)) {
                return "Read/Write (memory via (HL))";
            }
            else {
                return "Regs.Get/Set (R8 pairs)";
            }
        case 7:
            return "Alu.inc8/dec8 (memory via (HL))";
        case 8:
            return "Write (LD (HL),n)";
        case 9:
            return "Read/Write (memory via (nn))";
        case 10:
            return "Branch (relative)";
        default:
            return "Fetch/ReadImm (immediates), Regs.Get/Set (R16/R8)";
    }
}

const regNames = ofArray(["AF", "BC", "DE", "HL", "AF\'", "BC\'", "DE\'", "HL\'", "IX", "IY", "SP", "I", "R"]);

function regOf(s, name) {
    switch (name) {
        case "AF":
            return ~~s.Af | 0;
        case "BC":
            return ~~s.Bc | 0;
        case "DE":
            return ~~s.De | 0;
        case "HL":
            return ~~s.Hl | 0;
        case "AF\'":
            return ~~s.Af2 | 0;
        case "BC\'":
            return ~~s.Bc2 | 0;
        case "DE\'":
            return ~~s.De2 | 0;
        case "HL\'":
            return ~~s.Hl2 | 0;
        case "IX":
            return ~~s.Ix | 0;
        case "IY":
            return ~~s.Iy | 0;
        case "SP":
            return ~~s.Sp | 0;
        case "I":
            return ~~s.I | 0;
        case "R":
            return ~~s.R | 0;
        default:
            return 0;
    }
}

/**
 * Extract the contract. `memory` is the memory image to disassemble the
 * span from (normally the port's current memory; for self-modifying code
 * it may differ from what executed). `pcCycles` is the per-PC total cycle
 * count over the trace window (int64[65536]) used for averages.
 */
export function extract(memory, trace, r, pcCycles) {
    const disasm = [];
    let addr = r.SpanLo;
    while ((addr <= r.SpanHi) && (disasm.length < 512)) {
        const insn = disasmMemory(memory, addr);
        const executions = ((addr < trace.PerPcCount.length) ? item(addr, trace.PerPcCount) : 0) | 0;
        const avgCycles = (((addr < pcCycles.length) && (executions > 0)) ? ~~toInt32_unchecked(toInt64_unchecked(op_Division(item(addr, pcCycles), toInt64_unchecked(fromInt32(executions))))) : 0) | 0;
        void (disasm.push(new ContractInsn(addr, insn.Text, insn.Length, executions, avgCycles)));
        addr = (((addr + insn.Length) & 65535) | 0);
    }
    let registerDelta;
    const matchValue = r.InputSamples;
    const matchValue_1 = r.ExitSamples;
    let matchResult, exit, input;
    if (!isEmpty(matchValue)) {
        if (!isEmpty(matchValue_1)) {
            matchResult = 0;
            exit = head(matchValue_1);
            input = head(matchValue);
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
            registerDelta = toList(delay(() => collect((name) => {
                const before = regOf(input, name) | 0;
                const after = regOf(exit, name) | 0;
                return (after !== before) ? singleton([name, before, after]) : empty();
            }, regNames)));
            break;
        }
        default:
            registerDelta = empty_1();
    }
    const apiHints = toList(distinct(map((i) => apiHint(item(i.Address & 65535, memory)), disasm), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => (stringHash(x) | 0),
    }));
    return new RoutineContract(r.Entry, r.SpanLo, r.SpanHi, r.CallCount, toList(disasm), r.WriteRanges, registerDelta, r.InclusiveTStates, List_distinct(map_1((tuple) => (tuple[0] | 0), r.CallSites), {
        Equals: (x_1, y_1) => (x_1 === y_1),
        GetHashCode: (x_1) => (numberHash(x_1) | 0),
    }), List_distinct(map_1((tuple_1) => (tuple_1[1] | 0), r.CallSites), {
        Equals: (x_2, y_2) => (x_2 === y_2),
        GetHashCode: (x_2) => (numberHash(x_2) | 0),
    }), r.InputSamples, r.ExitSamples, r.SelfModifying, apiHints);
}

