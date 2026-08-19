
import { Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { record_type, lambda_type, unit_type, list_type, int32_type, string_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { Alu_add16, Alu_add8, Alu_dec8, Alu_sbc16, Alu_sub8, Flags__get_zero, RegisterFile__Wz, RegisterFile__Ix, RegisterFile__SetWz_Z524259A4, Alu_and8, Alu_inc8, Alu_Direction, Alu_fastRotateCircular8, Machine__Push16_Z524259A4, Machine__Read_Z524259A4, RegisterFile__Exx, Machine__ReadImm16, Machine__Pop16, RegisterFile__SetPc_Z524259A4, Machine__Branch_Z524259A4, Flags__get_carry, Machine__Flags, Machine__ReadImm, Alu_cmp8, Machine__SetFlags_2901ED1A, RegisterFile__Set_33BF5693, RegisterFile__Set_ZC22B834, Machine__PassTime_Z524259A4, R8, RegisterFile__Get_Z600F6D11, R16, RegisterFile__Get_Z61FD1070, Machine__Write_Z37302880, Machine__Fetch, Machine__get_Regs, RegisterFile__Pc, Machine_$reflection } from "../Jetpac2.Core/Machine.js";
import { ExecutionMode, ExecutionMode_$reflection } from "./Explorer.js";
import { printf, toFail } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { ofArray } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { item, setItem, fill } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { defaultOf, disposeSafe, getEnumerator } from "../fable_modules/fable-library-js.5.13.0/Util.js";

/**
 * The converted `screenClear` routine (0x71B8 / 0x71C6): idiomatic F# replacing
 * the generated instruction table for this one routine. The implementation is
 * instruction-granular (PC-dispatched per instruction) so an interrupt can land
 * mid-clear at the exact cycle, matching the oracle; a single-shot whole-loop
 * function would delay the interrupt past the routine and diverge.
 * Registry metadata for a lifted routine. Execution still mutates the shared
 * Jetpac2 machine instance; no shadow state is introduced.
 */
export class LiftedRoutine extends Record {
    constructor(Name, EntryAddresses, Execute, ImplementationMode) {
        super();
        this.Name = Name;
        this.EntryAddresses = EntryAddresses;
        this.Execute = Execute;
        this.ImplementationMode = ImplementationMode;
    }
}

export function LiftedRoutine_$reflection() {
    return record_type("Jetpac3.Core.LiftedRoutine", [], LiftedRoutine, () => [["Name", string_type], ["EntryAddresses", list_type(int32_type)], ["Execute", lambda_type(Machine_$reflection(), unit_type)], ["ImplementationMode", ExecutionMode_$reflection()]]);
}

export const LiftedRoutines_ClearAttrColour = 71;

/**
 * 0x71BF-0x71C5 — the shared clear loop: LD (HL),C; INC HL; LD A,H; CP B;
 * JR C; RET.
 */
export function LiftedRoutines_clearLoop(m) {
    let copyOfStruct;
    const matchValue = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
    switch (matchValue) {
        case 29119: {
            Machine__Fetch(m);
            Machine__Write_Z37302880(m, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.C));
            break;
        }
        case 29120: {
            Machine__Fetch(m);
            Machine__PassTime_Z524259A4(m, 2);
            RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL) + 1) & 65535);
            break;
        }
        case 29121: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL) >> 8) & 255);
            break;
        }
        case 29122: {
            Machine__Fetch(m);
            Machine__SetFlags_2901ED1A(m, Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.B))[1]);
            break;
        }
        case 29123: {
            Machine__Fetch(m);
            const offset = Machine__ReadImm(m) | 0;
            const offset_1 = ((offset >= 128) ? (offset - 256) : offset) | 0;
            if ((copyOfStruct = Machine__Flags(m), Flags__get_carry(copyOfStruct))) {
                Machine__PassTime_Z524259A4(m, 5);
                Machine__Branch_Z524259A4(m, offset_1);
            }
            break;
        }
        case 29125: {
            Machine__Fetch(m);
            RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), Machine__Pop16(m));
            break;
        }
        default: {
            const arg = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
            toFail(printf("ScreenClearLoop: unhandled resume at %04X"))(arg);
        }
    }
}

/**
 * 0x71B8 — clear the pixel area (0x4000-0x57FF with 0x00).
 */
export function LiftedRoutines_clearPixels(m) {
    if (RegisterFile__Pc(Machine__get_Regs(m)) === 29112) {
        Machine__Fetch(m);
        RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, Machine__ReadImm16(m));
        Machine__Fetch(m);
        RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, Machine__ReadImm(m));
        Machine__Fetch(m);
        RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.C, Machine__ReadImm(m));
    }
    else {
        const arg = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
        toFail(printf("ClearPixels: unhandled resume at %04X"))(arg);
    }
}

/**
 * 0x71C6 — clear the attribute area (0x5800-0x5AFF with ClearAttrColour).
 */
export function LiftedRoutines_clearAttrs(m) {
    if (RegisterFile__Pc(Machine__get_Regs(m)) === 29126) {
        Machine__Fetch(m);
        RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, Machine__ReadImm16(m));
        Machine__Fetch(m);
        RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, Machine__ReadImm(m));
        Machine__Fetch(m);
        Machine__ReadImm(m);
        RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.C, LiftedRoutines_ClearAttrColour);
        Machine__Fetch(m);
        const offset = Machine__ReadImm(m) | 0;
        const offset_1 = ((offset >= 128) ? (offset - 256) : offset) | 0;
        Machine__PassTime_Z524259A4(m, 5);
        Machine__Branch_Z524259A4(m, offset_1);
    }
    else {
        const arg = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
        toFail(printf("ClearAttrs: unhandled resume at %04X"))(arg);
    }
}

/**
 * Address -> converted routine (None falls through to the generated layer).
 */
export function LiftedRoutines_screenClearHook(addr) {
    switch (addr) {
        case 29112:
            return (m) => {
                LiftedRoutines_clearPixels(m);
            };
        case 29126:
            return (m_1) => {
                LiftedRoutines_clearAttrs(m_1);
            };
        default:
            if ((addr >= 29119) && (addr <= 29125)) {
                return (m_2) => {
                    LiftedRoutines_clearLoop(m_2);
                };
            }
            else {
                return undefined;
            }
    }
}

export const LiftedRoutines_screenClearRoutine = new LiftedRoutine("screen-clear", ofArray([29112, 29126, 29119, 29120, 29121, 29122, 29123, 29125]), (machine) => {
    const matchValue = LiftedRoutines_screenClearHook(RegisterFile__Pc(Machine__get_Regs(machine)));
    if (matchValue == null) {
        const arg = RegisterFile__Pc(Machine__get_Regs(machine)) | 0;
        toFail(printf("screen-clear has no implementation at %04X"))(arg);
    }
    else {
        matchValue(machine);
    }
}, ExecutionMode.Lifted);

/**
 * 0x71CF-0x720D — the real per-frame screen clear this game build runs:
 * an EXX-protected walk of the visible screen driven by the game's shadow
 * variables at 0x5DC3/0x5DC4/0x5DCF. Translated 1:1 from the generated
 * table (byte-for-byte fetches, exact PassTime) so the R register and
 * T-states match; an interrupt can land on any of the 40 instruction-start
 * addresses, hence one match arm each.
 */
export function LiftedRoutines_screenClear71CF(m) {
    let copyOfStruct, copyOfStruct_1, copyOfStruct_2, copyOfStruct_3, copyOfStruct_4;
    const matchValue = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
    switch (matchValue) {
        case 29135: {
            Machine__Fetch(m);
            RegisterFile__Exx(Machine__get_Regs(m));
            break;
        }
        case 29136: {
            Machine__Fetch(m);
            const addr = Machine__ReadImm16(m) | 0;
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.L, Machine__Read_Z524259A4(m, addr));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.H, Machine__Read_Z524259A4(m, (addr + 1) & 65535));
            break;
        }
        case 29139: {
            Machine__Fetch(m);
            const jumpAddress = Machine__ReadImm16(m) | 0;
            Machine__PassTime_Z524259A4(m, 1);
            Machine__Push16_Z524259A4(m, RegisterFile__Pc(Machine__get_Regs(m)));
            RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), jumpAddress);
            break;
        }
        case 29142: {
            Machine__Fetch(m);
            const addr_1 = Machine__ReadImm16(m) | 0;
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, Machine__Read_Z524259A4(m, addr_1));
            break;
        }
        case 29145: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            break;
        }
        case 29146: {
            Machine__Fetch(m);
            const addr_2 = Machine__ReadImm16(m) | 0;
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, Machine__Read_Z524259A4(m, addr_2));
            break;
        }
        case 29149: {
            Machine__Fetch(m);
            const patternInput = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Alu_Direction.Right, Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput[0]);
            Machine__SetFlags_2901ED1A(m, patternInput[1]);
            break;
        }
        case 29150: {
            Machine__Fetch(m);
            const patternInput_1 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Alu_Direction.Right, Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_1[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_1[1]);
            break;
        }
        case 29151: {
            Machine__Fetch(m);
            const patternInput_2 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_2[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_2[1]);
            break;
        }
        case 29152: {
            Machine__Fetch(m);
            const patternInput_3 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Alu_Direction.Right, Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_3[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_3[1]);
            break;
        }
        case 29153: {
            Machine__Fetch(m);
            const patternInput_4 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_4[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_4[1]);
            break;
        }
        case 29155: {
            Machine__Fetch(m);
            const patternInput_5 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_5[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_5[1]);
            break;
        }
        case 29156: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            break;
        }
        case 29157: {
            Machine__Fetch(m);
            Machine__Fetch(m);
            const d = Machine__ReadImm(m) | 0;
            const d_1 = ((d >= 128) ? (d - 256) : d) | 0;
            Machine__PassTime_Z524259A4(m, 5);
            RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m), (RegisterFile__Ix(Machine__get_Regs(m)) + d_1) & 65535);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.D, Machine__Read_Z524259A4(m, RegisterFile__Wz(Machine__get_Regs(m))));
            break;
        }
        case 29160: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.B));
            break;
        }
        case 29161: {
            Machine__Fetch(m);
            Machine__PassTime_Z524259A4(m, 1);
            Machine__Push16_Z524259A4(m, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL));
            break;
        }
        case 29162: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.H));
            break;
        }
        case 29163: {
            Machine__Fetch(m);
            const patternInput_6 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_6[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_6[1]);
            break;
        }
        case 29165: {
            Machine__Fetch(m);
            const offset = Machine__ReadImm(m) | 0;
            const offset_1 = ((offset >= 128) ? (offset - 256) : offset) | 0;
            if (!((copyOfStruct = Machine__Flags(m), Flags__get_carry(copyOfStruct)))) {
                Machine__PassTime_Z524259A4(m, 5);
                Machine__Branch_Z524259A4(m, offset_1);
            }
            break;
        }
        case 29167: {
            Machine__Fetch(m);
            const patternInput_7 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_7[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_7[1]);
            break;
        }
        case 29169: {
            Machine__Fetch(m);
            const offset_2 = Machine__ReadImm(m) | 0;
            const offset_3 = ((offset_2 >= 128) ? (offset_2 - 256) : offset_2) | 0;
            if ((copyOfStruct_1 = Machine__Flags(m), Flags__get_carry(copyOfStruct_1))) {
                Machine__PassTime_Z524259A4(m, 5);
                Machine__Branch_Z524259A4(m, offset_3);
            }
            break;
        }
        case 29171: {
            Machine__Fetch(m);
            RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL));
            Machine__Write_Z37302880(m, RegisterFile__Wz(Machine__get_Regs(m)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.D));
            break;
        }
        case 29172: {
            Machine__Fetch(m);
            const patternInput_8 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.L), Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.L, patternInput_8[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_8[1]);
            break;
        }
        case 29173: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.L));
            break;
        }
        case 29174: {
            Machine__Fetch(m);
            const patternInput_9 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_9[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_9[1]);
            break;
        }
        case 29176: {
            Machine__Fetch(m);
            const offset_4 = Machine__ReadImm(m) | 0;
            const offset_5 = ((offset_4 >= 128) ? (offset_4 - 256) : offset_4) | 0;
            if (!((copyOfStruct_2 = Machine__Flags(m), Flags__get_zero(copyOfStruct_2)))) {
                Machine__PassTime_Z524259A4(m, 5);
                Machine__Branch_Z524259A4(m, offset_5);
            }
            break;
        }
        case 29178: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.L));
            break;
        }
        case 29179: {
            Machine__Fetch(m);
            const patternInput_10 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m), false);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_10[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_10[1]);
            break;
        }
        case 29181: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            break;
        }
        case 29182: {
            Machine__Fetch(m);
            const offset_6 = Machine__ReadImm(m) | 0;
            const offset_7 = ((offset_6 >= 128) ? (offset_6 - 256) : offset_6) | 0;
            Machine__PassTime_Z524259A4(m, 1);
            const newB = (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.B) - 1) | 0;
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, newB);
            if (newB !== 0) {
                Machine__PassTime_Z524259A4(m, 5);
                Machine__Branch_Z524259A4(m, offset_7);
            }
            break;
        }
        case 29184: {
            Machine__Fetch(m);
            RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, Machine__Pop16(m));
            break;
        }
        case 29185: {
            Machine__Fetch(m);
            Machine__PassTime_Z524259A4(m, 1);
            Machine__Push16_Z524259A4(m, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.BC));
            break;
        }
        case 29186: {
            Machine__Fetch(m);
            const patternInput_11 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_11[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_11[1]);
            break;
        }
        case 29187: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.C, Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, Machine__ReadImm(m));
            break;
        }
        case 29190: {
            Machine__Fetch(m);
            Machine__Fetch(m);
            const rhs = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.BC) | 0;
            const patternInput_12 = Alu_sbc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL), rhs, (copyOfStruct_3 = Machine__Flags(m), Flags__get_carry(copyOfStruct_3)));
            RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, patternInput_12[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_12[1]);
            Machine__PassTime_Z524259A4(m, 7);
            break;
        }
        case 29192: {
            Machine__Fetch(m);
            RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.BC, Machine__Pop16(m));
            break;
        }
        case 29193: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.E));
            break;
        }
        case 29194: {
            Machine__Fetch(m);
            const patternInput_13 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.C), Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.C, patternInput_13[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_13[1]);
            break;
        }
        case 29195: {
            Machine__Fetch(m);
            const offset_8 = Machine__ReadImm(m) | 0;
            const offset_9 = ((offset_8 >= 128) ? (offset_8 - 256) : offset_8) | 0;
            if (!((copyOfStruct_4 = Machine__Flags(m), Flags__get_zero(copyOfStruct_4)))) {
                Machine__PassTime_Z524259A4(m, 5);
                Machine__Branch_Z524259A4(m, offset_9);
            }
            break;
        }
        case 29197: {
            Machine__Fetch(m);
            RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), Machine__Pop16(m));
            break;
        }
        default: {
            const arg = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
            toFail(printf("ScreenClear71CF: unhandled resume at %04X"))(arg);
        }
    }
}

export const LiftedRoutines_screenClear71CFRoutine = new LiftedRoutine("screen-clear-71cf", ofArray([29135, 29136, 29139, 29142, 29145, 29146, 29149, 29150, 29151, 29152, 29153, 29155, 29156, 29157, 29160, 29161, 29162, 29163, 29165, 29167, 29169, 29171, 29172, 29173, 29174, 29176, 29178, 29179, 29181, 29182, 29184, 29185, 29186, 29187, 29190, 29192, 29193, 29194, 29195, 29197]), (m) => {
    LiftedRoutines_screenClear71CF(m);
}, ExecutionMode.Lifted);

/**
 * 0x72EE-0x7302 — step a screen address in HL up one character row:
 * DEC H, and when the low 3 bits wrap (H was a multiple of 8), move L back
 * 0x20 and adjust H by 0x08 unless L underflowed. Translated 1:1 from the
 * generated table (byte-for-byte fetches, exact PassTime). The hottest
 * routine in the game: ~35k calls per trace window, so its timing must be
 * exact.
 */
export function LiftedRoutines_screenStep(m) {
    let copyOfStruct, copyOfStruct_1;
    const matchValue = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
    switch (matchValue) {
        case 29422: {
            Machine__Fetch(m);
            const patternInput = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.H), Machine__Flags(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.H, patternInput[0]);
            Machine__SetFlags_2901ED1A(m, patternInput[1]);
            break;
        }
        case 29423: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.H));
            break;
        }
        case 29424: {
            Machine__Fetch(m);
            const patternInput_1 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_1[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_1[1]);
            break;
        }
        case 29426: {
            Machine__Fetch(m);
            const patternInput_2 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_2[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_2[1]);
            break;
        }
        case 29428: {
            Machine__Fetch(m);
            Machine__PassTime_Z524259A4(m, 1);
            if (!((copyOfStruct = Machine__Flags(m), Flags__get_zero(copyOfStruct)))) {
                RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), Machine__Pop16(m));
            }
            break;
        }
        case 29429: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.L));
            break;
        }
        case 29430: {
            Machine__Fetch(m);
            const patternInput_3 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m), false);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_3[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_3[1]);
            break;
        }
        case 29432: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            break;
        }
        case 29433: {
            Machine__Fetch(m);
            const patternInput_4 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_4[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_4[1]);
            break;
        }
        case 29435: {
            Machine__Fetch(m);
            const patternInput_5 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_5[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_5[1]);
            break;
        }
        case 29437: {
            Machine__Fetch(m);
            Machine__PassTime_Z524259A4(m, 1);
            if ((copyOfStruct_1 = Machine__Flags(m), Flags__get_zero(copyOfStruct_1))) {
                RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), Machine__Pop16(m));
            }
            break;
        }
        case 29438: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.H));
            break;
        }
        case 29439: {
            Machine__Fetch(m);
            const patternInput_6 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A), Machine__ReadImm(m), false);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.A, patternInput_6[0]);
            Machine__SetFlags_2901ED1A(m, patternInput_6[1]);
            break;
        }
        case 29441: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            break;
        }
        case 29442: {
            Machine__Fetch(m);
            RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), Machine__Pop16(m));
            break;
        }
        default: {
            const arg = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
            toFail(printf("ScreenStep: unhandled resume at %04X"))(arg);
        }
    }
}

export const LiftedRoutines_screenStepRoutine = new LiftedRoutine("screen-addr-step", ofArray([29422, 29423, 29424, 29426, 29428, 29429, 29430, 29432, 29433, 29435, 29437, 29438, 29439, 29441, 29442]), (m) => {
    LiftedRoutines_screenStep(m);
}, ExecutionMode.Lifted);

/**
 * 0x64E6-0x64F0 — table lookup: DE = word at (0x67C1 + A*2). Translated
 * 1:1 from the generated table (WZ mirroring on the (HL) reads, exact
 * PassTime on ADD HL,BC and INC HL).
 */
export function LiftedRoutines_tableLookup64E6(m) {
    const matchValue = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
    switch (matchValue) {
        case 25830: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.L, Machine__ReadImm(m));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.H, Machine__ReadImm(m));
            break;
        }
        case 25833: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.A));
            break;
        }
        case 25834: {
            Machine__Fetch(m);
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, Machine__ReadImm(m));
            break;
        }
        case 25836: {
            Machine__Fetch(m);
            const patternInput = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.BC), Machine__Flags(m));
            RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, patternInput[0]);
            Machine__SetFlags_2901ED1A(m, patternInput[1]);
            Machine__PassTime_Z524259A4(m, 7);
            break;
        }
        case 25837: {
            Machine__Fetch(m);
            RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.E, Machine__Read_Z524259A4(m, RegisterFile__Wz(Machine__get_Regs(m))));
            break;
        }
        case 25838: {
            Machine__Fetch(m);
            RegisterFile__Set_ZC22B834(Machine__get_Regs(m), R16.HL, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL) + 1) & 65535);
            Machine__PassTime_Z524259A4(m, 2);
            break;
        }
        case 25839: {
            Machine__Fetch(m);
            RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m), R16.HL));
            RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.D, Machine__Read_Z524259A4(m, RegisterFile__Wz(Machine__get_Regs(m))));
            break;
        }
        case 25840: {
            Machine__Fetch(m);
            RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m), Machine__Pop16(m));
            break;
        }
        default: {
            const arg = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
            toFail(printf("TableLookup64E6: unhandled resume at %04X"))(arg);
        }
    }
}

export const LiftedRoutines_tableLookup64E6Routine = new LiftedRoutine("table-lookup-64e6", ofArray([25830, 25833, 25834, 25836, 25837, 25838, 25839, 25840]), (m) => {
    LiftedRoutines_tableLookup64E6(m);
}, ExecutionMode.Lifted);

export const LiftedRoutines_registry = ofArray([LiftedRoutines_screenClearRoutine, LiftedRoutines_screenClear71CFRoutine, LiftedRoutines_screenStepRoutine, LiftedRoutines_tableLookup64E6Routine]);

const LiftedRoutines_hookTable = (() => {
    const table = fill(new Array(65536), 0, 65536, null);
    const enumerator = getEnumerator(LiftedRoutines_registry);
    try {
        while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
            const routine = enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]();
            const enumerator_1 = getEnumerator(routine.EntryAddresses);
            try {
                while (enumerator_1["System.Collections.IEnumerator.MoveNext"]()) {
                    const addr = enumerator_1["System.Collections.Generic.IEnumerator`1.get_Current"]() | 0;
                    setItem(table, addr, routine.Execute);
                }
            }
            finally {
                disposeSafe(enumerator_1);
            }
        }
    }
    finally {
        disposeSafe(enumerator);
    }
    return table;
})();

export function LiftedRoutines_registryHook(address) {
    const f = item(address & 65535, LiftedRoutines_hookTable);
    if (f === defaultOf()) {
        return undefined;
    }
    else {
        return f;
    }
}

