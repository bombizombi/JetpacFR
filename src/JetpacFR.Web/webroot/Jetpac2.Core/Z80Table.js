
import { Machine_set_GeneratedStep_5007B66A, Machine__get_Memory, Alu_bit, Alu_shiftLogical8, Alu_shiftArithmetic8, Alu_rotate8, Alu_rotateCircular8, Flags_op_LogicalNot_2901ED1A, Flags_Subtract, Flags_HalfCarry, Flags__get_half_carry, Flags_Overflow, Flags_Flag5, Flags, Flags_Flag3, Flags_Zero, Flags_Sign, RegisterFile__R, Alu_iff2FlagsFor, RegisterFile__I, RegisterFile__SetR_Z524259A4, Alu_adc16, RegisterFile__SetI_Z524259A4, Machine__set_IrqMode_Z524259A4, Machine__get_Iff2, Alu_sbc16, Alu_parityFlagsFor, Flags_Carry, Flags_op_BitwiseAnd_603E7D40, Flags_op_BitwiseOr_603E7D40, RegisterFile__Iy, RegisterFile__Ix, RegisterFile__SetSp_Z524259A4, Machine__set_Iff2_Z1FBCCD16, Machine__set_Iff1_Z1FBCCD16, Flags__get_sign, RegisterFile__Sp, Flags__get_parity, Machine__In_Z524259A4, RegisterFile__Exx, Machine__Out_Z37302880, RegisterFile__Pc, Machine__Push16_Z524259A4, Machine__Pop16, RegisterFile__SetPc_Z524259A4, Alu_cmp8, Alu_or8, Alu_xor8, Alu_and8, Alu_sub8, Alu_add8, Machine__Halt, Alu_ccf, Alu_scf, RegisterFile__Wz, RegisterFile__SetWz_Z524259A4, Flags__get_carry, Alu_cpl, Alu_daa, Machine__ReadImm16, Flags__get_zero, Alu_fastRotate8, Machine__Branch_Z524259A4, Machine__Read_Z524259A4, Alu_add16, RegisterFile__Ex_Z3F9DF200, Alu_Direction, Alu_fastRotateCircular8, Alu_dec8, Machine__SetFlags_2901ED1A, Machine__Flags, Alu_inc8, Machine__PassTime_Z524259A4, RegisterFile__Set_ZC22B834, RegisterFile__Get_Z600F6D11, R16, RegisterFile__Get_Z61FD1070, Machine__Write_Z37302880, Machine__get_Regs, Machine__ReadImm, R8, RegisterFile__Set_33BF5693, Machine__Fetch } from "./Machine.js";
import { item } from "../fable_modules/fable-library-js.5.13.0/Array.js";
import { printf, toFail } from "../fable_modules/fable-library-js.5.13.0/String.js";

export const main = [(m) => {
    Machine__Fetch(m);
}, (m_1) => {
    Machine__Fetch(m_1);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.C, Machine__ReadImm(m_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.B, Machine__ReadImm(m_1));
}, (m_2) => {
    Machine__Fetch(m_2);
    Machine__Write_Z37302880(m_2, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_2), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_2), R8.A));
}, (m_3) => {
    Machine__Fetch(m_3);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_3), R16.BC, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_3), R16.BC) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_3, 2);
}, (m_4) => {
    Machine__Fetch(m_4);
    const patternInput = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_4), R8.B), Machine__Flags(m_4));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_4), R8.B, patternInput[0]);
    Machine__SetFlags_2901ED1A(m_4, patternInput[1]);
}, (m_5) => {
    Machine__Fetch(m_5);
    const patternInput_1 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_5), R8.B), Machine__Flags(m_5));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_5), R8.B, patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_5, patternInput_1[1]);
}, (m_6) => {
    Machine__Fetch(m_6);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_6), R8.B, Machine__ReadImm(m_6));
}, (m_7) => {
    Machine__Fetch(m_7);
    const patternInput_2 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_7), R8.A), Alu_Direction.Left, Machine__Flags(m_7));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_7), R8.A, patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_7, patternInput_2[1]);
}, (m_8) => {
    Machine__Fetch(m_8);
    RegisterFile__Ex_Z3F9DF200(Machine__get_Regs(m_8), R16.AF, R16.AF_);
}, (m_9) => {
    Machine__Fetch(m_9);
    const patternInput_3 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_9), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_9), R16.BC), Machine__Flags(m_9));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_9), R16.HL, patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_9, patternInput_3[1]);
    Machine__PassTime_Z524259A4(m_9, 7);
}, (m_10) => {
    Machine__Fetch(m_10);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_10), R8.A, Machine__Read_Z524259A4(m_10, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_10), R16.BC)));
}, (m_11) => {
    Machine__Fetch(m_11);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_11), R16.BC, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_11), R16.BC) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_11, 2);
}, (m_12) => {
    Machine__Fetch(m_12);
    const patternInput_4 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_12), R8.C), Machine__Flags(m_12));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_12), R8.C, patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_12, patternInput_4[1]);
}, (m_13) => {
    Machine__Fetch(m_13);
    const patternInput_5 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_13), R8.C), Machine__Flags(m_13));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_13), R8.C, patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_13, patternInput_5[1]);
}, (m_14) => {
    Machine__Fetch(m_14);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_14), R8.C, Machine__ReadImm(m_14));
}, (m_15) => {
    Machine__Fetch(m_15);
    const patternInput_6 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_15), R8.A), Alu_Direction.Right, Machine__Flags(m_15));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_15), R8.A, patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_15, patternInput_6[1]);
}, (m_16) => {
    Machine__Fetch(m_16);
    const offset = Machine__ReadImm(m_16) | 0;
    const offset_1 = ((offset >= 128) ? (offset - 256) : offset) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    const newB = (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_16), R8.B) - 1) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_16), R8.B, newB);
    if (newB !== 0) {
        Machine__PassTime_Z524259A4(m_16, 5);
        Machine__Branch_Z524259A4(m_16, offset_1);
    }
}, (m_17) => {
    Machine__Fetch(m_17);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.E, Machine__ReadImm(m_17));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.D, Machine__ReadImm(m_17));
}, (m_18) => {
    Machine__Fetch(m_18);
    Machine__Write_Z37302880(m_18, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_18), R16.DE), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_18), R8.A));
}, (m_19) => {
    Machine__Fetch(m_19);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_19), R16.DE, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_19), R16.DE) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_19, 2);
}, (m_20) => {
    Machine__Fetch(m_20);
    const patternInput_7 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_20), R8.D), Machine__Flags(m_20));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_20), R8.D, patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_20, patternInput_7[1]);
}, (m_21) => {
    Machine__Fetch(m_21);
    const patternInput_8 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_21), R8.D), Machine__Flags(m_21));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_21), R8.D, patternInput_8[0]);
    Machine__SetFlags_2901ED1A(m_21, patternInput_8[1]);
}, (m_22) => {
    Machine__Fetch(m_22);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_22), R8.D, Machine__ReadImm(m_22));
}, (m_23) => {
    Machine__Fetch(m_23);
    const patternInput_9 = Alu_fastRotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_23), R8.A), Alu_Direction.Left, Machine__Flags(m_23));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_23), R8.A, patternInput_9[0]);
    Machine__SetFlags_2901ED1A(m_23, patternInput_9[1]);
}, (m_24) => {
    Machine__Fetch(m_24);
    const offset_2 = Machine__ReadImm(m_24) | 0;
    const offset_3 = ((offset_2 >= 128) ? (offset_2 - 256) : offset_2) | 0;
    Machine__PassTime_Z524259A4(m_24, 5);
    Machine__Branch_Z524259A4(m_24, offset_3);
}, (m_25) => {
    Machine__Fetch(m_25);
    const patternInput_10 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_25), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_25), R16.DE), Machine__Flags(m_25));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_25), R16.HL, patternInput_10[0]);
    Machine__SetFlags_2901ED1A(m_25, patternInput_10[1]);
    Machine__PassTime_Z524259A4(m_25, 7);
}, (m_26) => {
    Machine__Fetch(m_26);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_26), R8.A, Machine__Read_Z524259A4(m_26, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_26), R16.DE)));
}, (m_27) => {
    Machine__Fetch(m_27);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_27), R16.DE, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_27), R16.DE) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_27, 2);
}, (m_28) => {
    Machine__Fetch(m_28);
    const patternInput_11 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_28), R8.E), Machine__Flags(m_28));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_28), R8.E, patternInput_11[0]);
    Machine__SetFlags_2901ED1A(m_28, patternInput_11[1]);
}, (m_29) => {
    Machine__Fetch(m_29);
    const patternInput_12 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_29), R8.E), Machine__Flags(m_29));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_29), R8.E, patternInput_12[0]);
    Machine__SetFlags_2901ED1A(m_29, patternInput_12[1]);
}, (m_30) => {
    Machine__Fetch(m_30);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_30), R8.E, Machine__ReadImm(m_30));
}, (m_31) => {
    Machine__Fetch(m_31);
    const patternInput_13 = Alu_fastRotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_31), R8.A), Alu_Direction.Right, Machine__Flags(m_31));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_31), R8.A, patternInput_13[0]);
    Machine__SetFlags_2901ED1A(m_31, patternInput_13[1]);
}, (m_32) => {
    let copyOfStruct;
    Machine__Fetch(m_32);
    const offset_4 = Machine__ReadImm(m_32) | 0;
    const offset_5 = ((offset_4 >= 128) ? (offset_4 - 256) : offset_4) | 0;
    if (!((copyOfStruct = Machine__Flags(m_32), Flags__get_zero(copyOfStruct)))) {
        Machine__PassTime_Z524259A4(m_32, 5);
        Machine__Branch_Z524259A4(m_32, offset_5);
    }
}, (m_33) => {
    Machine__Fetch(m_33);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.L, Machine__ReadImm(m_33));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.H, Machine__ReadImm(m_33));
}, (m_34) => {
    Machine__Fetch(m_34);
    const addr = Machine__ReadImm16(m_34) | 0;
    Machine__Write_Z37302880(m_34, addr, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.L));
    Machine__Write_Z37302880(m_34, (addr + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.H));
}, (m_35) => {
    Machine__Fetch(m_35);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_35), R16.HL, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_35), R16.HL) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_35, 2);
}, (m_36) => {
    Machine__Fetch(m_36);
    const patternInput_14 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_36), R8.H), Machine__Flags(m_36));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_36), R8.H, patternInput_14[0]);
    Machine__SetFlags_2901ED1A(m_36, patternInput_14[1]);
}, (m_37) => {
    Machine__Fetch(m_37);
    const patternInput_15 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_37), R8.H), Machine__Flags(m_37));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_37), R8.H, patternInput_15[0]);
    Machine__SetFlags_2901ED1A(m_37, patternInput_15[1]);
}, (m_38) => {
    Machine__Fetch(m_38);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_38), R8.H, Machine__ReadImm(m_38));
}, (m_39) => {
    Machine__Fetch(m_39);
    const patternInput_16 = Alu_daa(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_39), R8.A), Machine__Flags(m_39));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_39), R8.A, patternInput_16[0]);
    Machine__SetFlags_2901ED1A(m_39, patternInput_16[1]);
}, (m_40) => {
    let copyOfStruct_1;
    Machine__Fetch(m_40);
    const offset_6 = Machine__ReadImm(m_40) | 0;
    const offset_7 = ((offset_6 >= 128) ? (offset_6 - 256) : offset_6) | 0;
    if ((copyOfStruct_1 = Machine__Flags(m_40), Flags__get_zero(copyOfStruct_1))) {
        Machine__PassTime_Z524259A4(m_40, 5);
        Machine__Branch_Z524259A4(m_40, offset_7);
    }
}, (m_41) => {
    Machine__Fetch(m_41);
    const patternInput_17 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_41), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_41), R16.HL), Machine__Flags(m_41));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_41), R16.HL, patternInput_17[0]);
    Machine__SetFlags_2901ED1A(m_41, patternInput_17[1]);
    Machine__PassTime_Z524259A4(m_41, 7);
}, (m_42) => {
    Machine__Fetch(m_42);
    const addr_1 = Machine__ReadImm16(m_42) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.L, Machine__Read_Z524259A4(m_42, addr_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.H, Machine__Read_Z524259A4(m_42, (addr_1 + 1) & 65535));
}, (m_43) => {
    Machine__Fetch(m_43);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_43), R16.HL, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_43), R16.HL) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_43, 2);
}, (m_44) => {
    Machine__Fetch(m_44);
    const patternInput_18 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_44), R8.L), Machine__Flags(m_44));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_44), R8.L, patternInput_18[0]);
    Machine__SetFlags_2901ED1A(m_44, patternInput_18[1]);
}, (m_45) => {
    Machine__Fetch(m_45);
    const patternInput_19 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_45), R8.L), Machine__Flags(m_45));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_45), R8.L, patternInput_19[0]);
    Machine__SetFlags_2901ED1A(m_45, patternInput_19[1]);
}, (m_46) => {
    Machine__Fetch(m_46);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_46), R8.L, Machine__ReadImm(m_46));
}, (m_47) => {
    Machine__Fetch(m_47);
    const patternInput_20 = Alu_cpl(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_47), R8.A), Machine__Flags(m_47));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_47), R8.A, patternInput_20[0]);
    Machine__SetFlags_2901ED1A(m_47, patternInput_20[1]);
}, (m_48) => {
    let copyOfStruct_2;
    Machine__Fetch(m_48);
    const offset_8 = Machine__ReadImm(m_48) | 0;
    const offset_9 = ((offset_8 >= 128) ? (offset_8 - 256) : offset_8) | 0;
    if (!((copyOfStruct_2 = Machine__Flags(m_48), Flags__get_carry(copyOfStruct_2)))) {
        Machine__PassTime_Z524259A4(m_48, 5);
        Machine__Branch_Z524259A4(m_48, offset_9);
    }
}, (m_49) => {
    Machine__Fetch(m_49);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.SPL, Machine__ReadImm(m_49));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.SPH, Machine__ReadImm(m_49));
}, (m_50) => {
    Machine__Fetch(m_50);
    Machine__Write_Z37302880(m_50, Machine__ReadImm16(m_50), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_50), R8.A));
}, (m_51) => {
    Machine__Fetch(m_51);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_51), R16.SP, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_51), R16.SP) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_51, 2);
}, (m_52) => {
    Machine__Fetch(m_52);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_52), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_52), R16.HL));
    Machine__PassTime_Z524259A4(m_52, 1);
    const patternInput_21 = Alu_inc8(Machine__Read_Z524259A4(m_52, RegisterFile__Wz(Machine__get_Regs(m_52))), Machine__Flags(m_52));
    Machine__Write_Z37302880(m_52, RegisterFile__Wz(Machine__get_Regs(m_52)), patternInput_21[0]);
    Machine__SetFlags_2901ED1A(m_52, patternInput_21[1]);
}, (m_53) => {
    Machine__Fetch(m_53);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_53), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_53), R16.HL));
    Machine__PassTime_Z524259A4(m_53, 1);
    const patternInput_22 = Alu_dec8(Machine__Read_Z524259A4(m_53, RegisterFile__Wz(Machine__get_Regs(m_53))), Machine__Flags(m_53));
    Machine__Write_Z37302880(m_53, RegisterFile__Wz(Machine__get_Regs(m_53)), patternInput_22[0]);
    Machine__SetFlags_2901ED1A(m_53, patternInput_22[1]);
}, (m_54) => {
    Machine__Fetch(m_54);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_54), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_54), R16.HL));
    Machine__Write_Z37302880(m_54, RegisterFile__Wz(Machine__get_Regs(m_54)), Machine__ReadImm(m_54));
}, (m_55) => {
    Machine__Fetch(m_55);
    const patternInput_23 = Alu_scf(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_55), R8.A), Machine__Flags(m_55));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_55), R8.A, patternInput_23[0]);
    Machine__SetFlags_2901ED1A(m_55, patternInput_23[1]);
}, (m_56) => {
    let copyOfStruct_3;
    Machine__Fetch(m_56);
    const offset_10 = Machine__ReadImm(m_56) | 0;
    const offset_11 = ((offset_10 >= 128) ? (offset_10 - 256) : offset_10) | 0;
    if ((copyOfStruct_3 = Machine__Flags(m_56), Flags__get_carry(copyOfStruct_3))) {
        Machine__PassTime_Z524259A4(m_56, 5);
        Machine__Branch_Z524259A4(m_56, offset_11);
    }
}, (m_57) => {
    Machine__Fetch(m_57);
    const patternInput_24 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_57), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_57), R16.SP), Machine__Flags(m_57));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_57), R16.HL, patternInput_24[0]);
    Machine__SetFlags_2901ED1A(m_57, patternInput_24[1]);
    Machine__PassTime_Z524259A4(m_57, 7);
}, (m_58) => {
    Machine__Fetch(m_58);
    const addr_3 = Machine__ReadImm16(m_58) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_58), R8.A, Machine__Read_Z524259A4(m_58, addr_3));
}, (m_59) => {
    Machine__Fetch(m_59);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_59), R16.SP, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_59), R16.SP) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_59, 2);
}, (m_60) => {
    Machine__Fetch(m_60);
    const patternInput_25 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_60), R8.A), Machine__Flags(m_60));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_60), R8.A, patternInput_25[0]);
    Machine__SetFlags_2901ED1A(m_60, patternInput_25[1]);
}, (m_61) => {
    Machine__Fetch(m_61);
    const patternInput_26 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_61), R8.A), Machine__Flags(m_61));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_61), R8.A, patternInput_26[0]);
    Machine__SetFlags_2901ED1A(m_61, patternInput_26[1]);
}, (m_62) => {
    Machine__Fetch(m_62);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_62), R8.A, Machine__ReadImm(m_62));
}, (m_63) => {
    Machine__Fetch(m_63);
    const patternInput_27 = Alu_ccf(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_63), R8.A), Machine__Flags(m_63));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_63), R8.A, patternInput_27[0]);
    Machine__SetFlags_2901ED1A(m_63, patternInput_27[1]);
}, (m_64) => {
    Machine__Fetch(m_64);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_64), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_64), R8.B));
}, (m_65) => {
    Machine__Fetch(m_65);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_65), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_65), R8.C));
}, (m_66) => {
    Machine__Fetch(m_66);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_66), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_66), R8.D));
}, (m_67) => {
    Machine__Fetch(m_67);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_67), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.E));
}, (m_68) => {
    Machine__Fetch(m_68);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_68), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_68), R8.H));
}, (m_69) => {
    Machine__Fetch(m_69);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_69), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_69), R8.L));
}, (m_70) => {
    Machine__Fetch(m_70);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_70), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_70), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_70), R8.B, Machine__Read_Z524259A4(m_70, RegisterFile__Wz(Machine__get_Regs(m_70))));
}, (m_71) => {
    Machine__Fetch(m_71);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_71), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_71), R8.A));
}, (m_72) => {
    Machine__Fetch(m_72);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_72), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_72), R8.B));
}, (m_73) => {
    Machine__Fetch(m_73);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_73), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_73), R8.C));
}, (m_74) => {
    Machine__Fetch(m_74);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_74), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_74), R8.D));
}, (m_75) => {
    Machine__Fetch(m_75);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_75), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_75), R8.E));
}, (m_76) => {
    Machine__Fetch(m_76);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_76), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_76), R8.H));
}, (m_77) => {
    Machine__Fetch(m_77);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_77), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_77), R8.L));
}, (m_78) => {
    Machine__Fetch(m_78);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_78), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_78), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_78), R8.C, Machine__Read_Z524259A4(m_78, RegisterFile__Wz(Machine__get_Regs(m_78))));
}, (m_79) => {
    Machine__Fetch(m_79);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_79), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_79), R8.A));
}, (m_80) => {
    Machine__Fetch(m_80);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_80), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_80), R8.B));
}, (m_81) => {
    Machine__Fetch(m_81);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_81), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_81), R8.C));
}, (m_82) => {
    Machine__Fetch(m_82);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_82), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_82), R8.D));
}, (m_83) => {
    Machine__Fetch(m_83);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_83), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.E));
}, (m_84) => {
    Machine__Fetch(m_84);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_84), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_84), R8.H));
}, (m_85) => {
    Machine__Fetch(m_85);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_85), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_85), R8.L));
}, (m_86) => {
    Machine__Fetch(m_86);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_86), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_86), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_86), R8.D, Machine__Read_Z524259A4(m_86, RegisterFile__Wz(Machine__get_Regs(m_86))));
}, (m_87) => {
    Machine__Fetch(m_87);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_87), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_87), R8.A));
}, (m_88) => {
    Machine__Fetch(m_88);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_88), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_88), R8.B));
}, (m_89) => {
    Machine__Fetch(m_89);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_89), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_89), R8.C));
}, (m_90) => {
    Machine__Fetch(m_90);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_90), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_90), R8.D));
}, (m_91) => {
    Machine__Fetch(m_91);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_91), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_91), R8.E));
}, (m_92) => {
    Machine__Fetch(m_92);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_92), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_92), R8.H));
}, (m_93) => {
    Machine__Fetch(m_93);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_93), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_93), R8.L));
}, (m_94) => {
    Machine__Fetch(m_94);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_94), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_94), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_94), R8.E, Machine__Read_Z524259A4(m_94, RegisterFile__Wz(Machine__get_Regs(m_94))));
}, (m_95) => {
    Machine__Fetch(m_95);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_95), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_95), R8.A));
}, (m_96) => {
    Machine__Fetch(m_96);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_96), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_96), R8.B));
}, (m_97) => {
    Machine__Fetch(m_97);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_97), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_97), R8.C));
}, (m_98) => {
    Machine__Fetch(m_98);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_98), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_98), R8.D));
}, (m_99) => {
    Machine__Fetch(m_99);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_99), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.E));
}, (m_100) => {
    Machine__Fetch(m_100);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_100), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_100), R8.H));
}, (m_101) => {
    Machine__Fetch(m_101);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_101), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_101), R8.L));
}, (m_102) => {
    Machine__Fetch(m_102);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_102), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_102), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_102), R8.H, Machine__Read_Z524259A4(m_102, RegisterFile__Wz(Machine__get_Regs(m_102))));
}, (m_103) => {
    Machine__Fetch(m_103);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_103), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_103), R8.A));
}, (m_104) => {
    Machine__Fetch(m_104);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_104), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_104), R8.B));
}, (m_105) => {
    Machine__Fetch(m_105);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_105), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_105), R8.C));
}, (m_106) => {
    Machine__Fetch(m_106);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_106), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_106), R8.D));
}, (m_107) => {
    Machine__Fetch(m_107);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_107), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_107), R8.E));
}, (m_108) => {
    Machine__Fetch(m_108);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_108), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_108), R8.H));
}, (m_109) => {
    Machine__Fetch(m_109);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_109), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_109), R8.L));
}, (m_110) => {
    Machine__Fetch(m_110);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_110), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_110), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_110), R8.L, Machine__Read_Z524259A4(m_110, RegisterFile__Wz(Machine__get_Regs(m_110))));
}, (m_111) => {
    Machine__Fetch(m_111);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_111), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_111), R8.A));
}, (m_112) => {
    Machine__Fetch(m_112);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_112), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_112), R16.HL));
    Machine__Write_Z37302880(m_112, RegisterFile__Wz(Machine__get_Regs(m_112)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_112), R8.B));
}, (m_113) => {
    Machine__Fetch(m_113);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_113), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_113), R16.HL));
    Machine__Write_Z37302880(m_113, RegisterFile__Wz(Machine__get_Regs(m_113)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_113), R8.C));
}, (m_114) => {
    Machine__Fetch(m_114);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_114), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_114), R16.HL));
    Machine__Write_Z37302880(m_114, RegisterFile__Wz(Machine__get_Regs(m_114)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_114), R8.D));
}, (m_115) => {
    Machine__Fetch(m_115);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_115), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_115), R16.HL));
    Machine__Write_Z37302880(m_115, RegisterFile__Wz(Machine__get_Regs(m_115)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.E));
}, (m_116) => {
    Machine__Fetch(m_116);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_116), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_116), R16.HL));
    Machine__Write_Z37302880(m_116, RegisterFile__Wz(Machine__get_Regs(m_116)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_116), R8.H));
}, (m_117) => {
    Machine__Fetch(m_117);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_117), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_117), R16.HL));
    Machine__Write_Z37302880(m_117, RegisterFile__Wz(Machine__get_Regs(m_117)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_117), R8.L));
}, (m_118) => {
    Machine__Fetch(m_118);
    Machine__Halt(m_118);
}, (m_119) => {
    Machine__Fetch(m_119);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_119), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_119), R16.HL));
    Machine__Write_Z37302880(m_119, RegisterFile__Wz(Machine__get_Regs(m_119)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_119), R8.A));
}, (m_120) => {
    Machine__Fetch(m_120);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_120), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_120), R8.B));
}, (m_121) => {
    Machine__Fetch(m_121);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_121), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_121), R8.C));
}, (m_122) => {
    Machine__Fetch(m_122);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_122), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_122), R8.D));
}, (m_123) => {
    Machine__Fetch(m_123);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_123), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_123), R8.E));
}, (m_124) => {
    Machine__Fetch(m_124);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_124), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_124), R8.H));
}, (m_125) => {
    Machine__Fetch(m_125);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_125), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_125), R8.L));
}, (m_126) => {
    Machine__Fetch(m_126);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_126), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_126), R16.HL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_126), R8.A, Machine__Read_Z524259A4(m_126, RegisterFile__Wz(Machine__get_Regs(m_126))));
}, (m_127) => {
    Machine__Fetch(m_127);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_127), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_127), R8.A));
}, (m_128) => {
    Machine__Fetch(m_128);
    const patternInput_28 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.B), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_128), R8.A, patternInput_28[0]);
    Machine__SetFlags_2901ED1A(m_128, patternInput_28[1]);
}, (m_129) => {
    Machine__Fetch(m_129);
    const patternInput_29 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.C), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_129), R8.A, patternInput_29[0]);
    Machine__SetFlags_2901ED1A(m_129, patternInput_29[1]);
}, (m_130) => {
    Machine__Fetch(m_130);
    const patternInput_30 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.D), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_130), R8.A, patternInput_30[0]);
    Machine__SetFlags_2901ED1A(m_130, patternInput_30[1]);
}, (m_131) => {
    Machine__Fetch(m_131);
    const patternInput_31 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.E), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_131), R8.A, patternInput_31[0]);
    Machine__SetFlags_2901ED1A(m_131, patternInput_31[1]);
}, (m_132) => {
    Machine__Fetch(m_132);
    const patternInput_32 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.H), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_132), R8.A, patternInput_32[0]);
    Machine__SetFlags_2901ED1A(m_132, patternInput_32[1]);
}, (m_133) => {
    Machine__Fetch(m_133);
    const patternInput_33 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.L), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_133), R8.A, patternInput_33[0]);
    Machine__SetFlags_2901ED1A(m_133, patternInput_33[1]);
}, (m_134) => {
    Machine__Fetch(m_134);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_134), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_134), R16.HL));
    const patternInput_34 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_134), R8.A), Machine__Read_Z524259A4(m_134, RegisterFile__Wz(Machine__get_Regs(m_134))), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_134), R8.A, patternInput_34[0]);
    Machine__SetFlags_2901ED1A(m_134, patternInput_34[1]);
}, (m_135) => {
    Machine__Fetch(m_135);
    const patternInput_35 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_135), R8.A, patternInput_35[0]);
    Machine__SetFlags_2901ED1A(m_135, patternInput_35[1]);
}, (m_136) => {
    let copyOfStruct_4;
    Machine__Fetch(m_136);
    const patternInput_36 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.B), (copyOfStruct_4 = Machine__Flags(m_136), Flags__get_carry(copyOfStruct_4)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_136), R8.A, patternInput_36[0]);
    Machine__SetFlags_2901ED1A(m_136, patternInput_36[1]);
}, (m_137) => {
    let copyOfStruct_5;
    Machine__Fetch(m_137);
    const patternInput_37 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.C), (copyOfStruct_5 = Machine__Flags(m_137), Flags__get_carry(copyOfStruct_5)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_137), R8.A, patternInput_37[0]);
    Machine__SetFlags_2901ED1A(m_137, patternInput_37[1]);
}, (m_138) => {
    let copyOfStruct_6;
    Machine__Fetch(m_138);
    const patternInput_38 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.D), (copyOfStruct_6 = Machine__Flags(m_138), Flags__get_carry(copyOfStruct_6)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_138), R8.A, patternInput_38[0]);
    Machine__SetFlags_2901ED1A(m_138, patternInput_38[1]);
}, (m_139) => {
    let copyOfStruct_7;
    Machine__Fetch(m_139);
    const patternInput_39 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.E), (copyOfStruct_7 = Machine__Flags(m_139), Flags__get_carry(copyOfStruct_7)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_139), R8.A, patternInput_39[0]);
    Machine__SetFlags_2901ED1A(m_139, patternInput_39[1]);
}, (m_140) => {
    let copyOfStruct_8;
    Machine__Fetch(m_140);
    const patternInput_40 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.H), (copyOfStruct_8 = Machine__Flags(m_140), Flags__get_carry(copyOfStruct_8)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_140), R8.A, patternInput_40[0]);
    Machine__SetFlags_2901ED1A(m_140, patternInput_40[1]);
}, (m_141) => {
    let copyOfStruct_9;
    Machine__Fetch(m_141);
    const patternInput_41 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.L), (copyOfStruct_9 = Machine__Flags(m_141), Flags__get_carry(copyOfStruct_9)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_141), R8.A, patternInput_41[0]);
    Machine__SetFlags_2901ED1A(m_141, patternInput_41[1]);
}, (m_142) => {
    let copyOfStruct_10;
    Machine__Fetch(m_142);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_142), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_142), R16.HL));
    const patternInput_42 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_142), R8.A), Machine__Read_Z524259A4(m_142, RegisterFile__Wz(Machine__get_Regs(m_142))), (copyOfStruct_10 = Machine__Flags(m_142), Flags__get_carry(copyOfStruct_10)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_142), R8.A, patternInput_42[0]);
    Machine__SetFlags_2901ED1A(m_142, patternInput_42[1]);
}, (m_143) => {
    let copyOfStruct_11;
    Machine__Fetch(m_143);
    const patternInput_43 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A), (copyOfStruct_11 = Machine__Flags(m_143), Flags__get_carry(copyOfStruct_11)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_143), R8.A, patternInput_43[0]);
    Machine__SetFlags_2901ED1A(m_143, patternInput_43[1]);
}, (m_144) => {
    Machine__Fetch(m_144);
    const patternInput_44 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.B), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_144), R8.A, patternInput_44[0]);
    Machine__SetFlags_2901ED1A(m_144, patternInput_44[1]);
}, (m_145) => {
    Machine__Fetch(m_145);
    const patternInput_45 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.C), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_145), R8.A, patternInput_45[0]);
    Machine__SetFlags_2901ED1A(m_145, patternInput_45[1]);
}, (m_146) => {
    Machine__Fetch(m_146);
    const patternInput_46 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.D), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_146), R8.A, patternInput_46[0]);
    Machine__SetFlags_2901ED1A(m_146, patternInput_46[1]);
}, (m_147) => {
    Machine__Fetch(m_147);
    const patternInput_47 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.E), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_147), R8.A, patternInput_47[0]);
    Machine__SetFlags_2901ED1A(m_147, patternInput_47[1]);
}, (m_148) => {
    Machine__Fetch(m_148);
    const patternInput_48 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.H), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_148), R8.A, patternInput_48[0]);
    Machine__SetFlags_2901ED1A(m_148, patternInput_48[1]);
}, (m_149) => {
    Machine__Fetch(m_149);
    const patternInput_49 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.L), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_149), R8.A, patternInput_49[0]);
    Machine__SetFlags_2901ED1A(m_149, patternInput_49[1]);
}, (m_150) => {
    Machine__Fetch(m_150);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_150), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_150), R16.HL));
    const patternInput_50 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_150), R8.A), Machine__Read_Z524259A4(m_150, RegisterFile__Wz(Machine__get_Regs(m_150))), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_150), R8.A, patternInput_50[0]);
    Machine__SetFlags_2901ED1A(m_150, patternInput_50[1]);
}, (m_151) => {
    Machine__Fetch(m_151);
    const patternInput_51 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_151), R8.A, patternInput_51[0]);
    Machine__SetFlags_2901ED1A(m_151, patternInput_51[1]);
}, (m_152) => {
    let copyOfStruct_12;
    Machine__Fetch(m_152);
    const patternInput_52 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.B), (copyOfStruct_12 = Machine__Flags(m_152), Flags__get_carry(copyOfStruct_12)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_152), R8.A, patternInput_52[0]);
    Machine__SetFlags_2901ED1A(m_152, patternInput_52[1]);
}, (m_153) => {
    let copyOfStruct_13;
    Machine__Fetch(m_153);
    const patternInput_53 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.C), (copyOfStruct_13 = Machine__Flags(m_153), Flags__get_carry(copyOfStruct_13)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_153), R8.A, patternInput_53[0]);
    Machine__SetFlags_2901ED1A(m_153, patternInput_53[1]);
}, (m_154) => {
    let copyOfStruct_14;
    Machine__Fetch(m_154);
    const patternInput_54 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.D), (copyOfStruct_14 = Machine__Flags(m_154), Flags__get_carry(copyOfStruct_14)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_154), R8.A, patternInput_54[0]);
    Machine__SetFlags_2901ED1A(m_154, patternInput_54[1]);
}, (m_155) => {
    let copyOfStruct_15;
    Machine__Fetch(m_155);
    const patternInput_55 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.E), (copyOfStruct_15 = Machine__Flags(m_155), Flags__get_carry(copyOfStruct_15)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_155), R8.A, patternInput_55[0]);
    Machine__SetFlags_2901ED1A(m_155, patternInput_55[1]);
}, (m_156) => {
    let copyOfStruct_16;
    Machine__Fetch(m_156);
    const patternInput_56 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.H), (copyOfStruct_16 = Machine__Flags(m_156), Flags__get_carry(copyOfStruct_16)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_156), R8.A, patternInput_56[0]);
    Machine__SetFlags_2901ED1A(m_156, patternInput_56[1]);
}, (m_157) => {
    let copyOfStruct_17;
    Machine__Fetch(m_157);
    const patternInput_57 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.L), (copyOfStruct_17 = Machine__Flags(m_157), Flags__get_carry(copyOfStruct_17)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_157), R8.A, patternInput_57[0]);
    Machine__SetFlags_2901ED1A(m_157, patternInput_57[1]);
}, (m_158) => {
    let copyOfStruct_18;
    Machine__Fetch(m_158);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_158), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_158), R16.HL));
    const patternInput_58 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_158), R8.A), Machine__Read_Z524259A4(m_158, RegisterFile__Wz(Machine__get_Regs(m_158))), (copyOfStruct_18 = Machine__Flags(m_158), Flags__get_carry(copyOfStruct_18)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_158), R8.A, patternInput_58[0]);
    Machine__SetFlags_2901ED1A(m_158, patternInput_58[1]);
}, (m_159) => {
    let copyOfStruct_19;
    Machine__Fetch(m_159);
    const patternInput_59 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A), (copyOfStruct_19 = Machine__Flags(m_159), Flags__get_carry(copyOfStruct_19)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_159), R8.A, patternInput_59[0]);
    Machine__SetFlags_2901ED1A(m_159, patternInput_59[1]);
}, (m_160) => {
    Machine__Fetch(m_160);
    const patternInput_60 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_160), R8.A, patternInput_60[0]);
    Machine__SetFlags_2901ED1A(m_160, patternInput_60[1]);
}, (m_161) => {
    Machine__Fetch(m_161);
    const patternInput_61 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_161), R8.A, patternInput_61[0]);
    Machine__SetFlags_2901ED1A(m_161, patternInput_61[1]);
}, (m_162) => {
    Machine__Fetch(m_162);
    const patternInput_62 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_162), R8.A, patternInput_62[0]);
    Machine__SetFlags_2901ED1A(m_162, patternInput_62[1]);
}, (m_163) => {
    Machine__Fetch(m_163);
    const patternInput_63 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_163), R8.A, patternInput_63[0]);
    Machine__SetFlags_2901ED1A(m_163, patternInput_63[1]);
}, (m_164) => {
    Machine__Fetch(m_164);
    const patternInput_64 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.H));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_164), R8.A, patternInput_64[0]);
    Machine__SetFlags_2901ED1A(m_164, patternInput_64[1]);
}, (m_165) => {
    Machine__Fetch(m_165);
    const patternInput_65 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.L));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_165), R8.A, patternInput_65[0]);
    Machine__SetFlags_2901ED1A(m_165, patternInput_65[1]);
}, (m_166) => {
    Machine__Fetch(m_166);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_166), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_166), R16.HL));
    const patternInput_66 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_166), R8.A), Machine__Read_Z524259A4(m_166, RegisterFile__Wz(Machine__get_Regs(m_166))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_166), R8.A, patternInput_66[0]);
    Machine__SetFlags_2901ED1A(m_166, patternInput_66[1]);
}, (m_167) => {
    Machine__Fetch(m_167);
    const patternInput_67 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_167), R8.A, patternInput_67[0]);
    Machine__SetFlags_2901ED1A(m_167, patternInput_67[1]);
}, (m_168) => {
    Machine__Fetch(m_168);
    const patternInput_68 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_168), R8.A, patternInput_68[0]);
    Machine__SetFlags_2901ED1A(m_168, patternInput_68[1]);
}, (m_169) => {
    Machine__Fetch(m_169);
    const patternInput_69 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_169), R8.A, patternInput_69[0]);
    Machine__SetFlags_2901ED1A(m_169, patternInput_69[1]);
}, (m_170) => {
    Machine__Fetch(m_170);
    const patternInput_70 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_170), R8.A, patternInput_70[0]);
    Machine__SetFlags_2901ED1A(m_170, patternInput_70[1]);
}, (m_171) => {
    Machine__Fetch(m_171);
    const patternInput_71 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_171), R8.A, patternInput_71[0]);
    Machine__SetFlags_2901ED1A(m_171, patternInput_71[1]);
}, (m_172) => {
    Machine__Fetch(m_172);
    const patternInput_72 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.H));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_172), R8.A, patternInput_72[0]);
    Machine__SetFlags_2901ED1A(m_172, patternInput_72[1]);
}, (m_173) => {
    Machine__Fetch(m_173);
    const patternInput_73 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.L));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_173), R8.A, patternInput_73[0]);
    Machine__SetFlags_2901ED1A(m_173, patternInput_73[1]);
}, (m_174) => {
    Machine__Fetch(m_174);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_174), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_174), R16.HL));
    const patternInput_74 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_174), R8.A), Machine__Read_Z524259A4(m_174, RegisterFile__Wz(Machine__get_Regs(m_174))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_174), R8.A, patternInput_74[0]);
    Machine__SetFlags_2901ED1A(m_174, patternInput_74[1]);
}, (m_175) => {
    Machine__Fetch(m_175);
    const patternInput_75 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_175), R8.A, patternInput_75[0]);
    Machine__SetFlags_2901ED1A(m_175, patternInput_75[1]);
}, (m_176) => {
    Machine__Fetch(m_176);
    const patternInput_76 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_176), R8.A, patternInput_76[0]);
    Machine__SetFlags_2901ED1A(m_176, patternInput_76[1]);
}, (m_177) => {
    Machine__Fetch(m_177);
    const patternInput_77 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_177), R8.A, patternInput_77[0]);
    Machine__SetFlags_2901ED1A(m_177, patternInput_77[1]);
}, (m_178) => {
    Machine__Fetch(m_178);
    const patternInput_78 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_178), R8.A, patternInput_78[0]);
    Machine__SetFlags_2901ED1A(m_178, patternInput_78[1]);
}, (m_179) => {
    Machine__Fetch(m_179);
    const patternInput_79 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_179), R8.A, patternInput_79[0]);
    Machine__SetFlags_2901ED1A(m_179, patternInput_79[1]);
}, (m_180) => {
    Machine__Fetch(m_180);
    const patternInput_80 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.H));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_180), R8.A, patternInput_80[0]);
    Machine__SetFlags_2901ED1A(m_180, patternInput_80[1]);
}, (m_181) => {
    Machine__Fetch(m_181);
    const patternInput_81 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.L));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_181), R8.A, patternInput_81[0]);
    Machine__SetFlags_2901ED1A(m_181, patternInput_81[1]);
}, (m_182) => {
    Machine__Fetch(m_182);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_182), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_182), R16.HL));
    const patternInput_82 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_182), R8.A), Machine__Read_Z524259A4(m_182, RegisterFile__Wz(Machine__get_Regs(m_182))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_182), R8.A, patternInput_82[0]);
    Machine__SetFlags_2901ED1A(m_182, patternInput_82[1]);
}, (m_183) => {
    Machine__Fetch(m_183);
    const patternInput_83 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_183), R8.A, patternInput_83[0]);
    Machine__SetFlags_2901ED1A(m_183, patternInput_83[1]);
}, (m_184) => {
    Machine__Fetch(m_184);
    const patternInput_84 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_184), R8.A, patternInput_84[0]);
    Machine__SetFlags_2901ED1A(m_184, patternInput_84[1]);
}, (m_185) => {
    Machine__Fetch(m_185);
    const patternInput_85 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_185), R8.A, patternInput_85[0]);
    Machine__SetFlags_2901ED1A(m_185, patternInput_85[1]);
}, (m_186) => {
    Machine__Fetch(m_186);
    const patternInput_86 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_186), R8.A, patternInput_86[0]);
    Machine__SetFlags_2901ED1A(m_186, patternInput_86[1]);
}, (m_187) => {
    Machine__Fetch(m_187);
    const patternInput_87 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_187), R8.A, patternInput_87[0]);
    Machine__SetFlags_2901ED1A(m_187, patternInput_87[1]);
}, (m_188) => {
    Machine__Fetch(m_188);
    const patternInput_88 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.H));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_188), R8.A, patternInput_88[0]);
    Machine__SetFlags_2901ED1A(m_188, patternInput_88[1]);
}, (m_189) => {
    Machine__Fetch(m_189);
    const patternInput_89 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.L));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_189), R8.A, patternInput_89[0]);
    Machine__SetFlags_2901ED1A(m_189, patternInput_89[1]);
}, (m_190) => {
    Machine__Fetch(m_190);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_190), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_190), R16.HL));
    const patternInput_90 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_190), R8.A), Machine__Read_Z524259A4(m_190, RegisterFile__Wz(Machine__get_Regs(m_190))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_190), R8.A, patternInput_90[0]);
    Machine__SetFlags_2901ED1A(m_190, patternInput_90[1]);
}, (m_191) => {
    Machine__Fetch(m_191);
    const patternInput_91 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_191), R8.A, patternInput_91[0]);
    Machine__SetFlags_2901ED1A(m_191, patternInput_91[1]);
}, (m_192) => {
    let copyOfStruct_20;
    Machine__Fetch(m_192);
    Machine__PassTime_Z524259A4(m_192, 1);
    if (!((copyOfStruct_20 = Machine__Flags(m_192), Flags__get_zero(copyOfStruct_20)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_192), Machine__Pop16(m_192));
    }
}, (m_193) => {
    Machine__Fetch(m_193);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_193), R16.BC, Machine__Pop16(m_193));
}, (m_194) => {
    let copyOfStruct_21;
    Machine__Fetch(m_194);
    const jumpAddress = Machine__ReadImm16(m_194) | 0;
    if (!((copyOfStruct_21 = Machine__Flags(m_194), Flags__get_zero(copyOfStruct_21)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_194), jumpAddress);
    }
}, (m_195) => {
    Machine__Fetch(m_195);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_195), Machine__ReadImm16(m_195));
}, (m_196) => {
    let copyOfStruct_22;
    Machine__Fetch(m_196);
    const jumpAddress_1 = Machine__ReadImm16(m_196) | 0;
    if (!((copyOfStruct_22 = Machine__Flags(m_196), Flags__get_zero(copyOfStruct_22)))) {
        Machine__PassTime_Z524259A4(m_196, 1);
        Machine__Push16_Z524259A4(m_196, RegisterFile__Pc(Machine__get_Regs(m_196)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_196), jumpAddress_1);
    }
}, (m_197) => {
    Machine__Fetch(m_197);
    Machine__PassTime_Z524259A4(m_197, 1);
    Machine__Push16_Z524259A4(m_197, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_197), R16.BC));
}, (m_198) => {
    Machine__Fetch(m_198);
    const patternInput_92 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_198), R8.A), Machine__ReadImm(m_198), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_198), R8.A, patternInput_92[0]);
    Machine__SetFlags_2901ED1A(m_198, patternInput_92[1]);
}, (m_199) => {
    Machine__Fetch(m_199);
    Machine__PassTime_Z524259A4(m_199, 1);
    Machine__Push16_Z524259A4(m_199, RegisterFile__Pc(Machine__get_Regs(m_199)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_199), 0);
}, (m_200) => {
    let copyOfStruct_23;
    Machine__Fetch(m_200);
    Machine__PassTime_Z524259A4(m_200, 1);
    if ((copyOfStruct_23 = Machine__Flags(m_200), Flags__get_zero(copyOfStruct_23))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_200), Machine__Pop16(m_200));
    }
}, (m_201) => {
    Machine__Fetch(m_201);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_201), Machine__Pop16(m_201));
}, (m_202) => {
    let copyOfStruct_24;
    Machine__Fetch(m_202);
    const jumpAddress_2 = Machine__ReadImm16(m_202) | 0;
    if ((copyOfStruct_24 = Machine__Flags(m_202), Flags__get_zero(copyOfStruct_24))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_202), jumpAddress_2);
    }
}, undefined, (m_203) => {
    let copyOfStruct_25;
    Machine__Fetch(m_203);
    const jumpAddress_3 = Machine__ReadImm16(m_203) | 0;
    if ((copyOfStruct_25 = Machine__Flags(m_203), Flags__get_zero(copyOfStruct_25))) {
        Machine__PassTime_Z524259A4(m_203, 1);
        Machine__Push16_Z524259A4(m_203, RegisterFile__Pc(Machine__get_Regs(m_203)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_203), jumpAddress_3);
    }
}, (m_204) => {
    Machine__Fetch(m_204);
    const jumpAddress_4 = Machine__ReadImm16(m_204) | 0;
    Machine__PassTime_Z524259A4(m_204, 1);
    Machine__Push16_Z524259A4(m_204, RegisterFile__Pc(Machine__get_Regs(m_204)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_204), jumpAddress_4);
}, (m_205) => {
    let copyOfStruct_26;
    Machine__Fetch(m_205);
    const patternInput_93 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_205), R8.A), Machine__ReadImm(m_205), (copyOfStruct_26 = Machine__Flags(m_205), Flags__get_carry(copyOfStruct_26)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_205), R8.A, patternInput_93[0]);
    Machine__SetFlags_2901ED1A(m_205, patternInput_93[1]);
}, (m_206) => {
    Machine__Fetch(m_206);
    Machine__PassTime_Z524259A4(m_206, 1);
    Machine__Push16_Z524259A4(m_206, RegisterFile__Pc(Machine__get_Regs(m_206)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_206), 8);
}, (m_207) => {
    let copyOfStruct_27;
    Machine__Fetch(m_207);
    Machine__PassTime_Z524259A4(m_207, 1);
    if (!((copyOfStruct_27 = Machine__Flags(m_207), Flags__get_carry(copyOfStruct_27)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_207), Machine__Pop16(m_207));
    }
}, (m_208) => {
    Machine__Fetch(m_208);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_208), R16.DE, Machine__Pop16(m_208));
}, (m_209) => {
    let copyOfStruct_28;
    Machine__Fetch(m_209);
    const jumpAddress_5 = Machine__ReadImm16(m_209) | 0;
    if (!((copyOfStruct_28 = Machine__Flags(m_209), Flags__get_carry(copyOfStruct_28)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_209), jumpAddress_5);
    }
}, (m_210) => {
    Machine__Fetch(m_210);
    const a = RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_210), R8.A) | 0;
    const port = ((Machine__ReadImm(m_210) | (a << 8)) & 65535) | 0;
    Machine__PassTime_Z524259A4(m_210, 4);
    Machine__Out_Z37302880(m_210, port, a);
}, (m_211) => {
    let copyOfStruct_29;
    Machine__Fetch(m_211);
    const jumpAddress_6 = Machine__ReadImm16(m_211) | 0;
    if (!((copyOfStruct_29 = Machine__Flags(m_211), Flags__get_carry(copyOfStruct_29)))) {
        Machine__PassTime_Z524259A4(m_211, 1);
        Machine__Push16_Z524259A4(m_211, RegisterFile__Pc(Machine__get_Regs(m_211)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_211), jumpAddress_6);
    }
}, (m_212) => {
    Machine__Fetch(m_212);
    Machine__PassTime_Z524259A4(m_212, 1);
    Machine__Push16_Z524259A4(m_212, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_212), R16.DE));
}, (m_213) => {
    Machine__Fetch(m_213);
    const patternInput_94 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_213), R8.A), Machine__ReadImm(m_213), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_213), R8.A, patternInput_94[0]);
    Machine__SetFlags_2901ED1A(m_213, patternInput_94[1]);
}, (m_214) => {
    Machine__Fetch(m_214);
    Machine__PassTime_Z524259A4(m_214, 1);
    Machine__Push16_Z524259A4(m_214, RegisterFile__Pc(Machine__get_Regs(m_214)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_214), 16);
}, (m_215) => {
    let copyOfStruct_30;
    Machine__Fetch(m_215);
    Machine__PassTime_Z524259A4(m_215, 1);
    if ((copyOfStruct_30 = Machine__Flags(m_215), Flags__get_carry(copyOfStruct_30))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_215), Machine__Pop16(m_215));
    }
}, (m_216) => {
    Machine__Fetch(m_216);
    RegisterFile__Exx(Machine__get_Regs(m_216));
}, (m_217) => {
    let copyOfStruct_31;
    Machine__Fetch(m_217);
    const jumpAddress_7 = Machine__ReadImm16(m_217) | 0;
    if ((copyOfStruct_31 = Machine__Flags(m_217), Flags__get_carry(copyOfStruct_31))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_217), jumpAddress_7);
    }
}, (m_218) => {
    Machine__Fetch(m_218);
    const port_1 = ((Machine__ReadImm(m_218) | (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_218), R8.A) << 8)) & 65535) | 0;
    Machine__PassTime_Z524259A4(m_218, 4);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_218), R8.A, Machine__In_Z524259A4(m_218, port_1));
}, (m_219) => {
    let copyOfStruct_32;
    Machine__Fetch(m_219);
    const jumpAddress_8 = Machine__ReadImm16(m_219) | 0;
    if ((copyOfStruct_32 = Machine__Flags(m_219), Flags__get_carry(copyOfStruct_32))) {
        Machine__PassTime_Z524259A4(m_219, 1);
        Machine__Push16_Z524259A4(m_219, RegisterFile__Pc(Machine__get_Regs(m_219)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_219), jumpAddress_8);
    }
}, undefined, (m_220) => {
    let copyOfStruct_33;
    Machine__Fetch(m_220);
    const patternInput_95 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_220), R8.A), Machine__ReadImm(m_220), (copyOfStruct_33 = Machine__Flags(m_220), Flags__get_carry(copyOfStruct_33)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_220), R8.A, patternInput_95[0]);
    Machine__SetFlags_2901ED1A(m_220, patternInput_95[1]);
}, (m_221) => {
    Machine__Fetch(m_221);
    Machine__PassTime_Z524259A4(m_221, 1);
    Machine__Push16_Z524259A4(m_221, RegisterFile__Pc(Machine__get_Regs(m_221)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_221), 24);
}, (m_222) => {
    let copyOfStruct_34;
    Machine__Fetch(m_222);
    Machine__PassTime_Z524259A4(m_222, 1);
    if (!((copyOfStruct_34 = Machine__Flags(m_222), Flags__get_parity(copyOfStruct_34)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_222), Machine__Pop16(m_222));
    }
}, (m_223) => {
    Machine__Fetch(m_223);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_223), R16.HL, Machine__Pop16(m_223));
}, (m_224) => {
    let copyOfStruct_35;
    Machine__Fetch(m_224);
    const jumpAddress_9 = Machine__ReadImm16(m_224) | 0;
    if (!((copyOfStruct_35 = Machine__Flags(m_224), Flags__get_parity(copyOfStruct_35)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_224), jumpAddress_9);
    }
}, (m_225) => {
    Machine__Fetch(m_225);
    const sp = RegisterFile__Sp(Machine__get_Regs(m_225)) | 0;
    const spOldLow = Machine__Read_Z524259A4(m_225, sp) | 0;
    Machine__PassTime_Z524259A4(m_225, 1);
    const spOldHigh = Machine__Read_Z524259A4(m_225, (sp + 1) & 65535) | 0;
    Machine__Write_Z37302880(m_225, sp, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_225), R8.L));
    Machine__PassTime_Z524259A4(m_225, 2);
    Machine__Write_Z37302880(m_225, (sp + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_225), R8.H));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_225), R16.HL, (spOldLow | (spOldHigh << 8)) & 65535);
}, (m_226) => {
    let copyOfStruct_36;
    Machine__Fetch(m_226);
    const jumpAddress_10 = Machine__ReadImm16(m_226) | 0;
    if (!((copyOfStruct_36 = Machine__Flags(m_226), Flags__get_parity(copyOfStruct_36)))) {
        Machine__PassTime_Z524259A4(m_226, 1);
        Machine__Push16_Z524259A4(m_226, RegisterFile__Pc(Machine__get_Regs(m_226)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_226), jumpAddress_10);
    }
}, (m_227) => {
    Machine__Fetch(m_227);
    Machine__PassTime_Z524259A4(m_227, 1);
    Machine__Push16_Z524259A4(m_227, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_227), R16.HL));
}, (m_228) => {
    Machine__Fetch(m_228);
    const patternInput_96 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_228), R8.A), Machine__ReadImm(m_228));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_228), R8.A, patternInput_96[0]);
    Machine__SetFlags_2901ED1A(m_228, patternInput_96[1]);
}, (m_229) => {
    Machine__Fetch(m_229);
    Machine__PassTime_Z524259A4(m_229, 1);
    Machine__Push16_Z524259A4(m_229, RegisterFile__Pc(Machine__get_Regs(m_229)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_229), 32);
}, (m_230) => {
    let copyOfStruct_37;
    Machine__Fetch(m_230);
    Machine__PassTime_Z524259A4(m_230, 1);
    if ((copyOfStruct_37 = Machine__Flags(m_230), Flags__get_parity(copyOfStruct_37))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_230), Machine__Pop16(m_230));
    }
}, (m_231) => {
    Machine__Fetch(m_231);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_231), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_231), R16.HL));
}, (m_232) => {
    let copyOfStruct_38;
    Machine__Fetch(m_232);
    const jumpAddress_11 = Machine__ReadImm16(m_232) | 0;
    if ((copyOfStruct_38 = Machine__Flags(m_232), Flags__get_parity(copyOfStruct_38))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_232), jumpAddress_11);
    }
}, (m_233) => {
    Machine__Fetch(m_233);
    RegisterFile__Ex_Z3F9DF200(Machine__get_Regs(m_233), R16.DE, R16.HL);
}, (m_234) => {
    let copyOfStruct_39;
    Machine__Fetch(m_234);
    const jumpAddress_12 = Machine__ReadImm16(m_234) | 0;
    if ((copyOfStruct_39 = Machine__Flags(m_234), Flags__get_parity(copyOfStruct_39))) {
        Machine__PassTime_Z524259A4(m_234, 1);
        Machine__Push16_Z524259A4(m_234, RegisterFile__Pc(Machine__get_Regs(m_234)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_234), jumpAddress_12);
    }
}, undefined, (m_235) => {
    Machine__Fetch(m_235);
    const patternInput_97 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_235), R8.A), Machine__ReadImm(m_235));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_235), R8.A, patternInput_97[0]);
    Machine__SetFlags_2901ED1A(m_235, patternInput_97[1]);
}, (m_236) => {
    Machine__Fetch(m_236);
    Machine__PassTime_Z524259A4(m_236, 1);
    Machine__Push16_Z524259A4(m_236, RegisterFile__Pc(Machine__get_Regs(m_236)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_236), 40);
}, (m_237) => {
    let copyOfStruct_40;
    Machine__Fetch(m_237);
    Machine__PassTime_Z524259A4(m_237, 1);
    if (!((copyOfStruct_40 = Machine__Flags(m_237), Flags__get_sign(copyOfStruct_40)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_237), Machine__Pop16(m_237));
    }
}, (m_238) => {
    Machine__Fetch(m_238);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_238), R16.AF, Machine__Pop16(m_238));
}, (m_239) => {
    let copyOfStruct_41;
    Machine__Fetch(m_239);
    const jumpAddress_13 = Machine__ReadImm16(m_239) | 0;
    if (!((copyOfStruct_41 = Machine__Flags(m_239), Flags__get_sign(copyOfStruct_41)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_239), jumpAddress_13);
    }
}, (m_240) => {
    Machine__Fetch(m_240);
    Machine__set_Iff1_Z1FBCCD16(m_240, false);
    Machine__set_Iff2_Z1FBCCD16(m_240, false);
}, (m_241) => {
    let copyOfStruct_42;
    Machine__Fetch(m_241);
    const jumpAddress_14 = Machine__ReadImm16(m_241) | 0;
    if (!((copyOfStruct_42 = Machine__Flags(m_241), Flags__get_sign(copyOfStruct_42)))) {
        Machine__PassTime_Z524259A4(m_241, 1);
        Machine__Push16_Z524259A4(m_241, RegisterFile__Pc(Machine__get_Regs(m_241)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_241), jumpAddress_14);
    }
}, (m_242) => {
    Machine__Fetch(m_242);
    Machine__PassTime_Z524259A4(m_242, 1);
    Machine__Push16_Z524259A4(m_242, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_242), R16.AF));
}, (m_243) => {
    Machine__Fetch(m_243);
    const patternInput_98 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_243), R8.A), Machine__ReadImm(m_243));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_243), R8.A, patternInput_98[0]);
    Machine__SetFlags_2901ED1A(m_243, patternInput_98[1]);
}, (m_244) => {
    Machine__Fetch(m_244);
    Machine__PassTime_Z524259A4(m_244, 1);
    Machine__Push16_Z524259A4(m_244, RegisterFile__Pc(Machine__get_Regs(m_244)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_244), 48);
}, (m_245) => {
    let copyOfStruct_43;
    Machine__Fetch(m_245);
    Machine__PassTime_Z524259A4(m_245, 1);
    if ((copyOfStruct_43 = Machine__Flags(m_245), Flags__get_sign(copyOfStruct_43))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_245), Machine__Pop16(m_245));
    }
}, (m_246) => {
    Machine__Fetch(m_246);
    Machine__PassTime_Z524259A4(m_246, 2);
    RegisterFile__SetSp_Z524259A4(Machine__get_Regs(m_246), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_246), R16.HL));
}, (m_247) => {
    let copyOfStruct_44;
    Machine__Fetch(m_247);
    const jumpAddress_15 = Machine__ReadImm16(m_247) | 0;
    if ((copyOfStruct_44 = Machine__Flags(m_247), Flags__get_sign(copyOfStruct_44))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_247), jumpAddress_15);
    }
}, (m_248) => {
    Machine__Fetch(m_248);
    Machine__set_Iff1_Z1FBCCD16(m_248, true);
    Machine__set_Iff2_Z1FBCCD16(m_248, true);
}, (m_249) => {
    let copyOfStruct_45;
    Machine__Fetch(m_249);
    const jumpAddress_16 = Machine__ReadImm16(m_249) | 0;
    if ((copyOfStruct_45 = Machine__Flags(m_249), Flags__get_sign(copyOfStruct_45))) {
        Machine__PassTime_Z524259A4(m_249, 1);
        Machine__Push16_Z524259A4(m_249, RegisterFile__Pc(Machine__get_Regs(m_249)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_249), jumpAddress_16);
    }
}, undefined, (m_250) => {
    Machine__Fetch(m_250);
    const patternInput_99 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_250), R8.A), Machine__ReadImm(m_250));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_250), R8.A, patternInput_99[0]);
    Machine__SetFlags_2901ED1A(m_250, patternInput_99[1]);
}, (m_251) => {
    Machine__Fetch(m_251);
    Machine__PassTime_Z524259A4(m_251, 1);
    Machine__Push16_Z524259A4(m_251, RegisterFile__Pc(Machine__get_Regs(m_251)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_251), 56);
}];

export const dd = [(m) => {
    Machine__Fetch(m);
}, (m_1) => {
    Machine__Fetch(m_1);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.C, Machine__ReadImm(m_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.B, Machine__ReadImm(m_1));
}, (m_2) => {
    Machine__Fetch(m_2);
    Machine__Write_Z37302880(m_2, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_2), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_2), R8.A));
}, (m_3) => {
    Machine__Fetch(m_3);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_3), R16.BC, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_3), R16.BC) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_3, 2);
}, (m_4) => {
    Machine__Fetch(m_4);
    const patternInput = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_4), R8.B), Machine__Flags(m_4));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_4), R8.B, patternInput[0]);
    Machine__SetFlags_2901ED1A(m_4, patternInput[1]);
}, (m_5) => {
    Machine__Fetch(m_5);
    const patternInput_1 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_5), R8.B), Machine__Flags(m_5));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_5), R8.B, patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_5, patternInput_1[1]);
}, (m_6) => {
    Machine__Fetch(m_6);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_6), R8.B, Machine__ReadImm(m_6));
}, (m_7) => {
    Machine__Fetch(m_7);
    const patternInput_2 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_7), R8.A), Alu_Direction.Left, Machine__Flags(m_7));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_7), R8.A, patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_7, patternInput_2[1]);
}, (m_8) => {
    Machine__Fetch(m_8);
    RegisterFile__Ex_Z3F9DF200(Machine__get_Regs(m_8), R16.AF, R16.AF_);
}, (m_9) => {
    Machine__Fetch(m_9);
    Machine__Fetch(m_9);
    const patternInput_3 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_9), R16.IX), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_9), R16.BC), Machine__Flags(m_9));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_9), R16.IX, patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_9, patternInput_3[1]);
    Machine__PassTime_Z524259A4(m_9, 7);
}, (m_10) => {
    Machine__Fetch(m_10);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_10), R8.A, Machine__Read_Z524259A4(m_10, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_10), R16.BC)));
}, (m_11) => {
    Machine__Fetch(m_11);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_11), R16.BC, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_11), R16.BC) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_11, 2);
}, (m_12) => {
    Machine__Fetch(m_12);
    const patternInput_4 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_12), R8.C), Machine__Flags(m_12));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_12), R8.C, patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_12, patternInput_4[1]);
}, (m_13) => {
    Machine__Fetch(m_13);
    const patternInput_5 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_13), R8.C), Machine__Flags(m_13));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_13), R8.C, patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_13, patternInput_5[1]);
}, (m_14) => {
    Machine__Fetch(m_14);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_14), R8.C, Machine__ReadImm(m_14));
}, (m_15) => {
    Machine__Fetch(m_15);
    const patternInput_6 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_15), R8.A), Alu_Direction.Right, Machine__Flags(m_15));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_15), R8.A, patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_15, patternInput_6[1]);
}, (m_16) => {
    Machine__Fetch(m_16);
    const offset = Machine__ReadImm(m_16) | 0;
    const offset_1 = ((offset >= 128) ? (offset - 256) : offset) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    const newB = (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_16), R8.B) - 1) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_16), R8.B, newB);
    if (newB !== 0) {
        Machine__PassTime_Z524259A4(m_16, 5);
        Machine__Branch_Z524259A4(m_16, offset_1);
    }
}, (m_17) => {
    Machine__Fetch(m_17);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.E, Machine__ReadImm(m_17));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.D, Machine__ReadImm(m_17));
}, (m_18) => {
    Machine__Fetch(m_18);
    Machine__Write_Z37302880(m_18, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_18), R16.DE), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_18), R8.A));
}, (m_19) => {
    Machine__Fetch(m_19);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_19), R16.DE, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_19), R16.DE) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_19, 2);
}, (m_20) => {
    Machine__Fetch(m_20);
    const patternInput_7 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_20), R8.D), Machine__Flags(m_20));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_20), R8.D, patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_20, patternInput_7[1]);
}, (m_21) => {
    Machine__Fetch(m_21);
    const patternInput_8 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_21), R8.D), Machine__Flags(m_21));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_21), R8.D, patternInput_8[0]);
    Machine__SetFlags_2901ED1A(m_21, patternInput_8[1]);
}, (m_22) => {
    Machine__Fetch(m_22);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_22), R8.D, Machine__ReadImm(m_22));
}, (m_23) => {
    Machine__Fetch(m_23);
    const patternInput_9 = Alu_fastRotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_23), R8.A), Alu_Direction.Left, Machine__Flags(m_23));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_23), R8.A, patternInput_9[0]);
    Machine__SetFlags_2901ED1A(m_23, patternInput_9[1]);
}, (m_24) => {
    Machine__Fetch(m_24);
    const offset_2 = Machine__ReadImm(m_24) | 0;
    const offset_3 = ((offset_2 >= 128) ? (offset_2 - 256) : offset_2) | 0;
    Machine__PassTime_Z524259A4(m_24, 5);
    Machine__Branch_Z524259A4(m_24, offset_3);
}, (m_25) => {
    Machine__Fetch(m_25);
    Machine__Fetch(m_25);
    const patternInput_10 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_25), R16.IX), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_25), R16.DE), Machine__Flags(m_25));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_25), R16.IX, patternInput_10[0]);
    Machine__SetFlags_2901ED1A(m_25, patternInput_10[1]);
    Machine__PassTime_Z524259A4(m_25, 7);
}, (m_26) => {
    Machine__Fetch(m_26);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_26), R8.A, Machine__Read_Z524259A4(m_26, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_26), R16.DE)));
}, (m_27) => {
    Machine__Fetch(m_27);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_27), R16.DE, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_27), R16.DE) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_27, 2);
}, (m_28) => {
    Machine__Fetch(m_28);
    const patternInput_11 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_28), R8.E), Machine__Flags(m_28));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_28), R8.E, patternInput_11[0]);
    Machine__SetFlags_2901ED1A(m_28, patternInput_11[1]);
}, (m_29) => {
    Machine__Fetch(m_29);
    const patternInput_12 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_29), R8.E), Machine__Flags(m_29));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_29), R8.E, patternInput_12[0]);
    Machine__SetFlags_2901ED1A(m_29, patternInput_12[1]);
}, (m_30) => {
    Machine__Fetch(m_30);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_30), R8.E, Machine__ReadImm(m_30));
}, (m_31) => {
    Machine__Fetch(m_31);
    const patternInput_13 = Alu_fastRotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_31), R8.A), Alu_Direction.Right, Machine__Flags(m_31));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_31), R8.A, patternInput_13[0]);
    Machine__SetFlags_2901ED1A(m_31, patternInput_13[1]);
}, (m_32) => {
    let copyOfStruct;
    Machine__Fetch(m_32);
    const offset_4 = Machine__ReadImm(m_32) | 0;
    const offset_5 = ((offset_4 >= 128) ? (offset_4 - 256) : offset_4) | 0;
    if (!((copyOfStruct = Machine__Flags(m_32), Flags__get_zero(copyOfStruct)))) {
        Machine__PassTime_Z524259A4(m_32, 5);
        Machine__Branch_Z524259A4(m_32, offset_5);
    }
}, (m_33) => {
    Machine__Fetch(m_33);
    Machine__Fetch(m_33);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.IXL, Machine__ReadImm(m_33));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.IXH, Machine__ReadImm(m_33));
}, (m_34) => {
    Machine__Fetch(m_34);
    Machine__Fetch(m_34);
    const addr = Machine__ReadImm16(m_34) | 0;
    Machine__Write_Z37302880(m_34, addr, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.IXL));
    Machine__Write_Z37302880(m_34, (addr + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.IXH));
}, (m_35) => {
    Machine__Fetch(m_35);
    Machine__Fetch(m_35);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_35), R16.IX, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_35), R16.IX) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_35, 2);
}, (m_36) => {
    Machine__Fetch(m_36);
    Machine__Fetch(m_36);
    const patternInput_14 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_36), R8.IXH), Machine__Flags(m_36));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_36), R8.IXH, patternInput_14[0]);
    Machine__SetFlags_2901ED1A(m_36, patternInput_14[1]);
}, (m_37) => {
    Machine__Fetch(m_37);
    Machine__Fetch(m_37);
    const patternInput_15 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_37), R8.IXH), Machine__Flags(m_37));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_37), R8.IXH, patternInput_15[0]);
    Machine__SetFlags_2901ED1A(m_37, patternInput_15[1]);
}, (m_38) => {
    Machine__Fetch(m_38);
    Machine__Fetch(m_38);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_38), R8.IXH, Machine__ReadImm(m_38));
}, (m_39) => {
    Machine__Fetch(m_39);
    const patternInput_16 = Alu_daa(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_39), R8.A), Machine__Flags(m_39));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_39), R8.A, patternInput_16[0]);
    Machine__SetFlags_2901ED1A(m_39, patternInput_16[1]);
}, (m_40) => {
    let copyOfStruct_1;
    Machine__Fetch(m_40);
    const offset_6 = Machine__ReadImm(m_40) | 0;
    const offset_7 = ((offset_6 >= 128) ? (offset_6 - 256) : offset_6) | 0;
    if ((copyOfStruct_1 = Machine__Flags(m_40), Flags__get_zero(copyOfStruct_1))) {
        Machine__PassTime_Z524259A4(m_40, 5);
        Machine__Branch_Z524259A4(m_40, offset_7);
    }
}, (m_41) => {
    Machine__Fetch(m_41);
    Machine__Fetch(m_41);
    const patternInput_17 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_41), R16.IX), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_41), R16.IX), Machine__Flags(m_41));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_41), R16.IX, patternInput_17[0]);
    Machine__SetFlags_2901ED1A(m_41, patternInput_17[1]);
    Machine__PassTime_Z524259A4(m_41, 7);
}, (m_42) => {
    Machine__Fetch(m_42);
    Machine__Fetch(m_42);
    const addr_1 = Machine__ReadImm16(m_42) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.IXL, Machine__Read_Z524259A4(m_42, addr_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.IXH, Machine__Read_Z524259A4(m_42, (addr_1 + 1) & 65535));
}, (m_43) => {
    Machine__Fetch(m_43);
    Machine__Fetch(m_43);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_43), R16.IX, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_43), R16.IX) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_43, 2);
}, (m_44) => {
    Machine__Fetch(m_44);
    Machine__Fetch(m_44);
    const patternInput_18 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_44), R8.IXL), Machine__Flags(m_44));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_44), R8.IXL, patternInput_18[0]);
    Machine__SetFlags_2901ED1A(m_44, patternInput_18[1]);
}, (m_45) => {
    Machine__Fetch(m_45);
    Machine__Fetch(m_45);
    const patternInput_19 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_45), R8.IXL), Machine__Flags(m_45));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_45), R8.IXL, patternInput_19[0]);
    Machine__SetFlags_2901ED1A(m_45, patternInput_19[1]);
}, (m_46) => {
    Machine__Fetch(m_46);
    Machine__Fetch(m_46);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_46), R8.IXL, Machine__ReadImm(m_46));
}, (m_47) => {
    Machine__Fetch(m_47);
    const patternInput_20 = Alu_cpl(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_47), R8.A), Machine__Flags(m_47));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_47), R8.A, patternInput_20[0]);
    Machine__SetFlags_2901ED1A(m_47, patternInput_20[1]);
}, (m_48) => {
    let copyOfStruct_2;
    Machine__Fetch(m_48);
    const offset_8 = Machine__ReadImm(m_48) | 0;
    const offset_9 = ((offset_8 >= 128) ? (offset_8 - 256) : offset_8) | 0;
    if (!((copyOfStruct_2 = Machine__Flags(m_48), Flags__get_carry(copyOfStruct_2)))) {
        Machine__PassTime_Z524259A4(m_48, 5);
        Machine__Branch_Z524259A4(m_48, offset_9);
    }
}, (m_49) => {
    Machine__Fetch(m_49);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.SPL, Machine__ReadImm(m_49));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.SPH, Machine__ReadImm(m_49));
}, (m_50) => {
    Machine__Fetch(m_50);
    Machine__Write_Z37302880(m_50, Machine__ReadImm16(m_50), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_50), R8.A));
}, (m_51) => {
    Machine__Fetch(m_51);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_51), R16.SP, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_51), R16.SP) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_51, 2);
}, (m_52) => {
    Machine__Fetch(m_52);
    Machine__Fetch(m_52);
    const d = Machine__ReadImm(m_52) | 0;
    const d_1 = ((d >= 128) ? (d - 256) : d) | 0;
    Machine__PassTime_Z524259A4(m_52, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_52), (RegisterFile__Ix(Machine__get_Regs(m_52)) + d_1) & 65535);
    Machine__PassTime_Z524259A4(m_52, 1);
    const patternInput_21 = Alu_inc8(Machine__Read_Z524259A4(m_52, RegisterFile__Wz(Machine__get_Regs(m_52))), Machine__Flags(m_52));
    Machine__Write_Z37302880(m_52, RegisterFile__Wz(Machine__get_Regs(m_52)), patternInput_21[0]);
    Machine__SetFlags_2901ED1A(m_52, patternInput_21[1]);
}, (m_53) => {
    Machine__Fetch(m_53);
    Machine__Fetch(m_53);
    const d_2 = Machine__ReadImm(m_53) | 0;
    const d_3 = ((d_2 >= 128) ? (d_2 - 256) : d_2) | 0;
    Machine__PassTime_Z524259A4(m_53, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_53), (RegisterFile__Ix(Machine__get_Regs(m_53)) + d_3) & 65535);
    Machine__PassTime_Z524259A4(m_53, 1);
    const patternInput_22 = Alu_dec8(Machine__Read_Z524259A4(m_53, RegisterFile__Wz(Machine__get_Regs(m_53))), Machine__Flags(m_53));
    Machine__Write_Z37302880(m_53, RegisterFile__Wz(Machine__get_Regs(m_53)), patternInput_22[0]);
    Machine__SetFlags_2901ED1A(m_53, patternInput_22[1]);
}, (m_54) => {
    Machine__Fetch(m_54);
    Machine__Fetch(m_54);
    const d_4 = Machine__ReadImm(m_54) | 0;
    const d_5 = ((d_4 >= 128) ? (d_4 - 256) : d_4) | 0;
    Machine__PassTime_Z524259A4(m_54, 2);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_54), (RegisterFile__Ix(Machine__get_Regs(m_54)) + d_5) & 65535);
    Machine__Write_Z37302880(m_54, RegisterFile__Wz(Machine__get_Regs(m_54)), Machine__ReadImm(m_54));
}, (m_55) => {
    Machine__Fetch(m_55);
    const patternInput_23 = Alu_scf(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_55), R8.A), Machine__Flags(m_55));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_55), R8.A, patternInput_23[0]);
    Machine__SetFlags_2901ED1A(m_55, patternInput_23[1]);
}, (m_56) => {
    let copyOfStruct_3;
    Machine__Fetch(m_56);
    const offset_10 = Machine__ReadImm(m_56) | 0;
    const offset_11 = ((offset_10 >= 128) ? (offset_10 - 256) : offset_10) | 0;
    if ((copyOfStruct_3 = Machine__Flags(m_56), Flags__get_carry(copyOfStruct_3))) {
        Machine__PassTime_Z524259A4(m_56, 5);
        Machine__Branch_Z524259A4(m_56, offset_11);
    }
}, (m_57) => {
    Machine__Fetch(m_57);
    Machine__Fetch(m_57);
    const patternInput_24 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_57), R16.IX), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_57), R16.SP), Machine__Flags(m_57));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_57), R16.IX, patternInput_24[0]);
    Machine__SetFlags_2901ED1A(m_57, patternInput_24[1]);
    Machine__PassTime_Z524259A4(m_57, 7);
}, (m_58) => {
    Machine__Fetch(m_58);
    const addr_3 = Machine__ReadImm16(m_58) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_58), R8.A, Machine__Read_Z524259A4(m_58, addr_3));
}, (m_59) => {
    Machine__Fetch(m_59);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_59), R16.SP, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_59), R16.SP) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_59, 2);
}, (m_60) => {
    Machine__Fetch(m_60);
    const patternInput_25 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_60), R8.A), Machine__Flags(m_60));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_60), R8.A, patternInput_25[0]);
    Machine__SetFlags_2901ED1A(m_60, patternInput_25[1]);
}, (m_61) => {
    Machine__Fetch(m_61);
    const patternInput_26 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_61), R8.A), Machine__Flags(m_61));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_61), R8.A, patternInput_26[0]);
    Machine__SetFlags_2901ED1A(m_61, patternInput_26[1]);
}, (m_62) => {
    Machine__Fetch(m_62);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_62), R8.A, Machine__ReadImm(m_62));
}, (m_63) => {
    Machine__Fetch(m_63);
    const patternInput_27 = Alu_ccf(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_63), R8.A), Machine__Flags(m_63));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_63), R8.A, patternInput_27[0]);
    Machine__SetFlags_2901ED1A(m_63, patternInput_27[1]);
}, (m_64) => {
    Machine__Fetch(m_64);
    Machine__Fetch(m_64);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_64), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_64), R8.B));
}, (m_65) => {
    Machine__Fetch(m_65);
    Machine__Fetch(m_65);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_65), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_65), R8.C));
}, (m_66) => {
    Machine__Fetch(m_66);
    Machine__Fetch(m_66);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_66), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_66), R8.D));
}, (m_67) => {
    Machine__Fetch(m_67);
    Machine__Fetch(m_67);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_67), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.E));
}, (m_68) => {
    Machine__Fetch(m_68);
    Machine__Fetch(m_68);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_68), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_68), R8.IXH));
}, (m_69) => {
    Machine__Fetch(m_69);
    Machine__Fetch(m_69);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_69), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_69), R8.IXL));
}, (m_70) => {
    Machine__Fetch(m_70);
    Machine__Fetch(m_70);
    const d_6 = Machine__ReadImm(m_70) | 0;
    const d_7 = ((d_6 >= 128) ? (d_6 - 256) : d_6) | 0;
    Machine__PassTime_Z524259A4(m_70, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_70), (RegisterFile__Ix(Machine__get_Regs(m_70)) + d_7) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_70), R8.B, Machine__Read_Z524259A4(m_70, RegisterFile__Wz(Machine__get_Regs(m_70))));
}, (m_71) => {
    Machine__Fetch(m_71);
    Machine__Fetch(m_71);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_71), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_71), R8.A));
}, (m_72) => {
    Machine__Fetch(m_72);
    Machine__Fetch(m_72);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_72), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_72), R8.B));
}, (m_73) => {
    Machine__Fetch(m_73);
    Machine__Fetch(m_73);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_73), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_73), R8.C));
}, (m_74) => {
    Machine__Fetch(m_74);
    Machine__Fetch(m_74);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_74), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_74), R8.D));
}, (m_75) => {
    Machine__Fetch(m_75);
    Machine__Fetch(m_75);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_75), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_75), R8.E));
}, (m_76) => {
    Machine__Fetch(m_76);
    Machine__Fetch(m_76);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_76), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_76), R8.IXH));
}, (m_77) => {
    Machine__Fetch(m_77);
    Machine__Fetch(m_77);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_77), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_77), R8.IXL));
}, (m_78) => {
    Machine__Fetch(m_78);
    Machine__Fetch(m_78);
    const d_8 = Machine__ReadImm(m_78) | 0;
    const d_9 = ((d_8 >= 128) ? (d_8 - 256) : d_8) | 0;
    Machine__PassTime_Z524259A4(m_78, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_78), (RegisterFile__Ix(Machine__get_Regs(m_78)) + d_9) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_78), R8.C, Machine__Read_Z524259A4(m_78, RegisterFile__Wz(Machine__get_Regs(m_78))));
}, (m_79) => {
    Machine__Fetch(m_79);
    Machine__Fetch(m_79);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_79), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_79), R8.A));
}, (m_80) => {
    Machine__Fetch(m_80);
    Machine__Fetch(m_80);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_80), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_80), R8.B));
}, (m_81) => {
    Machine__Fetch(m_81);
    Machine__Fetch(m_81);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_81), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_81), R8.C));
}, (m_82) => {
    Machine__Fetch(m_82);
    Machine__Fetch(m_82);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_82), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_82), R8.D));
}, (m_83) => {
    Machine__Fetch(m_83);
    Machine__Fetch(m_83);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_83), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.E));
}, (m_84) => {
    Machine__Fetch(m_84);
    Machine__Fetch(m_84);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_84), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_84), R8.IXH));
}, (m_85) => {
    Machine__Fetch(m_85);
    Machine__Fetch(m_85);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_85), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_85), R8.IXL));
}, (m_86) => {
    Machine__Fetch(m_86);
    Machine__Fetch(m_86);
    const d_10 = Machine__ReadImm(m_86) | 0;
    const d_11 = ((d_10 >= 128) ? (d_10 - 256) : d_10) | 0;
    Machine__PassTime_Z524259A4(m_86, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_86), (RegisterFile__Ix(Machine__get_Regs(m_86)) + d_11) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_86), R8.D, Machine__Read_Z524259A4(m_86, RegisterFile__Wz(Machine__get_Regs(m_86))));
}, (m_87) => {
    Machine__Fetch(m_87);
    Machine__Fetch(m_87);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_87), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_87), R8.A));
}, (m_88) => {
    Machine__Fetch(m_88);
    Machine__Fetch(m_88);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_88), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_88), R8.B));
}, (m_89) => {
    Machine__Fetch(m_89);
    Machine__Fetch(m_89);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_89), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_89), R8.C));
}, (m_90) => {
    Machine__Fetch(m_90);
    Machine__Fetch(m_90);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_90), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_90), R8.D));
}, (m_91) => {
    Machine__Fetch(m_91);
    Machine__Fetch(m_91);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_91), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_91), R8.E));
}, (m_92) => {
    Machine__Fetch(m_92);
    Machine__Fetch(m_92);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_92), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_92), R8.IXH));
}, (m_93) => {
    Machine__Fetch(m_93);
    Machine__Fetch(m_93);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_93), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_93), R8.IXL));
}, (m_94) => {
    Machine__Fetch(m_94);
    Machine__Fetch(m_94);
    const d_12 = Machine__ReadImm(m_94) | 0;
    const d_13 = ((d_12 >= 128) ? (d_12 - 256) : d_12) | 0;
    Machine__PassTime_Z524259A4(m_94, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_94), (RegisterFile__Ix(Machine__get_Regs(m_94)) + d_13) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_94), R8.E, Machine__Read_Z524259A4(m_94, RegisterFile__Wz(Machine__get_Regs(m_94))));
}, (m_95) => {
    Machine__Fetch(m_95);
    Machine__Fetch(m_95);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_95), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_95), R8.A));
}, (m_96) => {
    Machine__Fetch(m_96);
    Machine__Fetch(m_96);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_96), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_96), R8.B));
}, (m_97) => {
    Machine__Fetch(m_97);
    Machine__Fetch(m_97);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_97), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_97), R8.C));
}, (m_98) => {
    Machine__Fetch(m_98);
    Machine__Fetch(m_98);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_98), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_98), R8.D));
}, (m_99) => {
    Machine__Fetch(m_99);
    Machine__Fetch(m_99);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_99), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.E));
}, (m_100) => {
    Machine__Fetch(m_100);
    Machine__Fetch(m_100);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_100), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_100), R8.IXH));
}, (m_101) => {
    Machine__Fetch(m_101);
    Machine__Fetch(m_101);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_101), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_101), R8.IXL));
}, (m_102) => {
    Machine__Fetch(m_102);
    Machine__Fetch(m_102);
    const d_14 = Machine__ReadImm(m_102) | 0;
    const d_15 = ((d_14 >= 128) ? (d_14 - 256) : d_14) | 0;
    Machine__PassTime_Z524259A4(m_102, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_102), (RegisterFile__Ix(Machine__get_Regs(m_102)) + d_15) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_102), R8.H, Machine__Read_Z524259A4(m_102, RegisterFile__Wz(Machine__get_Regs(m_102))));
}, (m_103) => {
    Machine__Fetch(m_103);
    Machine__Fetch(m_103);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_103), R8.IXH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_103), R8.A));
}, (m_104) => {
    Machine__Fetch(m_104);
    Machine__Fetch(m_104);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_104), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_104), R8.B));
}, (m_105) => {
    Machine__Fetch(m_105);
    Machine__Fetch(m_105);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_105), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_105), R8.C));
}, (m_106) => {
    Machine__Fetch(m_106);
    Machine__Fetch(m_106);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_106), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_106), R8.D));
}, (m_107) => {
    Machine__Fetch(m_107);
    Machine__Fetch(m_107);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_107), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_107), R8.E));
}, (m_108) => {
    Machine__Fetch(m_108);
    Machine__Fetch(m_108);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_108), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_108), R8.IXH));
}, (m_109) => {
    Machine__Fetch(m_109);
    Machine__Fetch(m_109);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_109), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_109), R8.IXL));
}, (m_110) => {
    Machine__Fetch(m_110);
    Machine__Fetch(m_110);
    const d_16 = Machine__ReadImm(m_110) | 0;
    const d_17 = ((d_16 >= 128) ? (d_16 - 256) : d_16) | 0;
    Machine__PassTime_Z524259A4(m_110, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_110), (RegisterFile__Ix(Machine__get_Regs(m_110)) + d_17) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_110), R8.L, Machine__Read_Z524259A4(m_110, RegisterFile__Wz(Machine__get_Regs(m_110))));
}, (m_111) => {
    Machine__Fetch(m_111);
    Machine__Fetch(m_111);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_111), R8.IXL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_111), R8.A));
}, (m_112) => {
    Machine__Fetch(m_112);
    Machine__Fetch(m_112);
    const d_18 = Machine__ReadImm(m_112) | 0;
    const d_19 = ((d_18 >= 128) ? (d_18 - 256) : d_18) | 0;
    Machine__PassTime_Z524259A4(m_112, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_112), (RegisterFile__Ix(Machine__get_Regs(m_112)) + d_19) & 65535);
    Machine__Write_Z37302880(m_112, RegisterFile__Wz(Machine__get_Regs(m_112)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_112), R8.B));
}, (m_113) => {
    Machine__Fetch(m_113);
    Machine__Fetch(m_113);
    const d_20 = Machine__ReadImm(m_113) | 0;
    const d_21 = ((d_20 >= 128) ? (d_20 - 256) : d_20) | 0;
    Machine__PassTime_Z524259A4(m_113, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_113), (RegisterFile__Ix(Machine__get_Regs(m_113)) + d_21) & 65535);
    Machine__Write_Z37302880(m_113, RegisterFile__Wz(Machine__get_Regs(m_113)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_113), R8.C));
}, (m_114) => {
    Machine__Fetch(m_114);
    Machine__Fetch(m_114);
    const d_22 = Machine__ReadImm(m_114) | 0;
    const d_23 = ((d_22 >= 128) ? (d_22 - 256) : d_22) | 0;
    Machine__PassTime_Z524259A4(m_114, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_114), (RegisterFile__Ix(Machine__get_Regs(m_114)) + d_23) & 65535);
    Machine__Write_Z37302880(m_114, RegisterFile__Wz(Machine__get_Regs(m_114)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_114), R8.D));
}, (m_115) => {
    Machine__Fetch(m_115);
    Machine__Fetch(m_115);
    const d_24 = Machine__ReadImm(m_115) | 0;
    const d_25 = ((d_24 >= 128) ? (d_24 - 256) : d_24) | 0;
    Machine__PassTime_Z524259A4(m_115, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_115), (RegisterFile__Ix(Machine__get_Regs(m_115)) + d_25) & 65535);
    Machine__Write_Z37302880(m_115, RegisterFile__Wz(Machine__get_Regs(m_115)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.E));
}, (m_116) => {
    Machine__Fetch(m_116);
    Machine__Fetch(m_116);
    const d_26 = Machine__ReadImm(m_116) | 0;
    const d_27 = ((d_26 >= 128) ? (d_26 - 256) : d_26) | 0;
    Machine__PassTime_Z524259A4(m_116, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_116), (RegisterFile__Ix(Machine__get_Regs(m_116)) + d_27) & 65535);
    Machine__Write_Z37302880(m_116, RegisterFile__Wz(Machine__get_Regs(m_116)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_116), R8.H));
}, (m_117) => {
    Machine__Fetch(m_117);
    Machine__Fetch(m_117);
    const d_28 = Machine__ReadImm(m_117) | 0;
    const d_29 = ((d_28 >= 128) ? (d_28 - 256) : d_28) | 0;
    Machine__PassTime_Z524259A4(m_117, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_117), (RegisterFile__Ix(Machine__get_Regs(m_117)) + d_29) & 65535);
    Machine__Write_Z37302880(m_117, RegisterFile__Wz(Machine__get_Regs(m_117)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_117), R8.L));
}, (m_118) => {
    Machine__Fetch(m_118);
    Machine__Fetch(m_118);
    Machine__PassTime_Z524259A4(m_118, 1);
    Machine__Halt(m_118);
}, (m_119) => {
    Machine__Fetch(m_119);
    Machine__Fetch(m_119);
    const d_30 = Machine__ReadImm(m_119) | 0;
    const d_31 = ((d_30 >= 128) ? (d_30 - 256) : d_30) | 0;
    Machine__PassTime_Z524259A4(m_119, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_119), (RegisterFile__Ix(Machine__get_Regs(m_119)) + d_31) & 65535);
    Machine__Write_Z37302880(m_119, RegisterFile__Wz(Machine__get_Regs(m_119)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_119), R8.A));
}, (m_120) => {
    Machine__Fetch(m_120);
    Machine__Fetch(m_120);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_120), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_120), R8.B));
}, (m_121) => {
    Machine__Fetch(m_121);
    Machine__Fetch(m_121);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_121), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_121), R8.C));
}, (m_122) => {
    Machine__Fetch(m_122);
    Machine__Fetch(m_122);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_122), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_122), R8.D));
}, (m_123) => {
    Machine__Fetch(m_123);
    Machine__Fetch(m_123);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_123), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_123), R8.E));
}, (m_124) => {
    Machine__Fetch(m_124);
    Machine__Fetch(m_124);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_124), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_124), R8.IXH));
}, (m_125) => {
    Machine__Fetch(m_125);
    Machine__Fetch(m_125);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_125), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_125), R8.IXL));
}, (m_126) => {
    Machine__Fetch(m_126);
    Machine__Fetch(m_126);
    const d_32 = Machine__ReadImm(m_126) | 0;
    const d_33 = ((d_32 >= 128) ? (d_32 - 256) : d_32) | 0;
    Machine__PassTime_Z524259A4(m_126, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_126), (RegisterFile__Ix(Machine__get_Regs(m_126)) + d_33) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_126), R8.A, Machine__Read_Z524259A4(m_126, RegisterFile__Wz(Machine__get_Regs(m_126))));
}, (m_127) => {
    Machine__Fetch(m_127);
    Machine__Fetch(m_127);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_127), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_127), R8.A));
}, (m_128) => {
    Machine__Fetch(m_128);
    Machine__Fetch(m_128);
    const patternInput_28 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.B), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_128), R8.A, patternInput_28[0]);
    Machine__SetFlags_2901ED1A(m_128, patternInput_28[1]);
}, (m_129) => {
    Machine__Fetch(m_129);
    Machine__Fetch(m_129);
    const patternInput_29 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.C), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_129), R8.A, patternInput_29[0]);
    Machine__SetFlags_2901ED1A(m_129, patternInput_29[1]);
}, (m_130) => {
    Machine__Fetch(m_130);
    Machine__Fetch(m_130);
    const patternInput_30 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.D), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_130), R8.A, patternInput_30[0]);
    Machine__SetFlags_2901ED1A(m_130, patternInput_30[1]);
}, (m_131) => {
    Machine__Fetch(m_131);
    Machine__Fetch(m_131);
    const patternInput_31 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.E), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_131), R8.A, patternInput_31[0]);
    Machine__SetFlags_2901ED1A(m_131, patternInput_31[1]);
}, (m_132) => {
    Machine__Fetch(m_132);
    Machine__Fetch(m_132);
    const patternInput_32 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.IXH), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_132), R8.A, patternInput_32[0]);
    Machine__SetFlags_2901ED1A(m_132, patternInput_32[1]);
}, (m_133) => {
    Machine__Fetch(m_133);
    Machine__Fetch(m_133);
    const patternInput_33 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.IXL), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_133), R8.A, patternInput_33[0]);
    Machine__SetFlags_2901ED1A(m_133, patternInput_33[1]);
}, (m_134) => {
    Machine__Fetch(m_134);
    Machine__Fetch(m_134);
    const d_34 = Machine__ReadImm(m_134) | 0;
    const d_35 = ((d_34 >= 128) ? (d_34 - 256) : d_34) | 0;
    Machine__PassTime_Z524259A4(m_134, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_134), (RegisterFile__Ix(Machine__get_Regs(m_134)) + d_35) & 65535);
    const patternInput_34 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_134), R8.A), Machine__Read_Z524259A4(m_134, RegisterFile__Wz(Machine__get_Regs(m_134))), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_134), R8.A, patternInput_34[0]);
    Machine__SetFlags_2901ED1A(m_134, patternInput_34[1]);
}, (m_135) => {
    Machine__Fetch(m_135);
    Machine__Fetch(m_135);
    const patternInput_35 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_135), R8.A, patternInput_35[0]);
    Machine__SetFlags_2901ED1A(m_135, patternInput_35[1]);
}, (m_136) => {
    let copyOfStruct_4;
    Machine__Fetch(m_136);
    Machine__Fetch(m_136);
    const patternInput_36 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.B), (copyOfStruct_4 = Machine__Flags(m_136), Flags__get_carry(copyOfStruct_4)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_136), R8.A, patternInput_36[0]);
    Machine__SetFlags_2901ED1A(m_136, patternInput_36[1]);
}, (m_137) => {
    let copyOfStruct_5;
    Machine__Fetch(m_137);
    Machine__Fetch(m_137);
    const patternInput_37 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.C), (copyOfStruct_5 = Machine__Flags(m_137), Flags__get_carry(copyOfStruct_5)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_137), R8.A, patternInput_37[0]);
    Machine__SetFlags_2901ED1A(m_137, patternInput_37[1]);
}, (m_138) => {
    let copyOfStruct_6;
    Machine__Fetch(m_138);
    Machine__Fetch(m_138);
    const patternInput_38 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.D), (copyOfStruct_6 = Machine__Flags(m_138), Flags__get_carry(copyOfStruct_6)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_138), R8.A, patternInput_38[0]);
    Machine__SetFlags_2901ED1A(m_138, patternInput_38[1]);
}, (m_139) => {
    let copyOfStruct_7;
    Machine__Fetch(m_139);
    Machine__Fetch(m_139);
    const patternInput_39 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.E), (copyOfStruct_7 = Machine__Flags(m_139), Flags__get_carry(copyOfStruct_7)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_139), R8.A, patternInput_39[0]);
    Machine__SetFlags_2901ED1A(m_139, patternInput_39[1]);
}, (m_140) => {
    let copyOfStruct_8;
    Machine__Fetch(m_140);
    Machine__Fetch(m_140);
    const patternInput_40 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.IXH), (copyOfStruct_8 = Machine__Flags(m_140), Flags__get_carry(copyOfStruct_8)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_140), R8.A, patternInput_40[0]);
    Machine__SetFlags_2901ED1A(m_140, patternInput_40[1]);
}, (m_141) => {
    let copyOfStruct_9;
    Machine__Fetch(m_141);
    Machine__Fetch(m_141);
    const patternInput_41 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.IXL), (copyOfStruct_9 = Machine__Flags(m_141), Flags__get_carry(copyOfStruct_9)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_141), R8.A, patternInput_41[0]);
    Machine__SetFlags_2901ED1A(m_141, patternInput_41[1]);
}, (m_142) => {
    let copyOfStruct_10;
    Machine__Fetch(m_142);
    Machine__Fetch(m_142);
    const d_36 = Machine__ReadImm(m_142) | 0;
    const d_37 = ((d_36 >= 128) ? (d_36 - 256) : d_36) | 0;
    Machine__PassTime_Z524259A4(m_142, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_142), (RegisterFile__Ix(Machine__get_Regs(m_142)) + d_37) & 65535);
    const patternInput_42 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_142), R8.A), Machine__Read_Z524259A4(m_142, RegisterFile__Wz(Machine__get_Regs(m_142))), (copyOfStruct_10 = Machine__Flags(m_142), Flags__get_carry(copyOfStruct_10)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_142), R8.A, patternInput_42[0]);
    Machine__SetFlags_2901ED1A(m_142, patternInput_42[1]);
}, (m_143) => {
    let copyOfStruct_11;
    Machine__Fetch(m_143);
    Machine__Fetch(m_143);
    const patternInput_43 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A), (copyOfStruct_11 = Machine__Flags(m_143), Flags__get_carry(copyOfStruct_11)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_143), R8.A, patternInput_43[0]);
    Machine__SetFlags_2901ED1A(m_143, patternInput_43[1]);
}, (m_144) => {
    Machine__Fetch(m_144);
    Machine__Fetch(m_144);
    const patternInput_44 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.B), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_144), R8.A, patternInput_44[0]);
    Machine__SetFlags_2901ED1A(m_144, patternInput_44[1]);
}, (m_145) => {
    Machine__Fetch(m_145);
    Machine__Fetch(m_145);
    const patternInput_45 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.C), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_145), R8.A, patternInput_45[0]);
    Machine__SetFlags_2901ED1A(m_145, patternInput_45[1]);
}, (m_146) => {
    Machine__Fetch(m_146);
    Machine__Fetch(m_146);
    const patternInput_46 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.D), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_146), R8.A, patternInput_46[0]);
    Machine__SetFlags_2901ED1A(m_146, patternInput_46[1]);
}, (m_147) => {
    Machine__Fetch(m_147);
    Machine__Fetch(m_147);
    const patternInput_47 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.E), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_147), R8.A, patternInput_47[0]);
    Machine__SetFlags_2901ED1A(m_147, patternInput_47[1]);
}, (m_148) => {
    Machine__Fetch(m_148);
    Machine__Fetch(m_148);
    const patternInput_48 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.IXH), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_148), R8.A, patternInput_48[0]);
    Machine__SetFlags_2901ED1A(m_148, patternInput_48[1]);
}, (m_149) => {
    Machine__Fetch(m_149);
    Machine__Fetch(m_149);
    const patternInput_49 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.IXL), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_149), R8.A, patternInput_49[0]);
    Machine__SetFlags_2901ED1A(m_149, patternInput_49[1]);
}, (m_150) => {
    Machine__Fetch(m_150);
    Machine__Fetch(m_150);
    const d_38 = Machine__ReadImm(m_150) | 0;
    const d_39 = ((d_38 >= 128) ? (d_38 - 256) : d_38) | 0;
    Machine__PassTime_Z524259A4(m_150, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_150), (RegisterFile__Ix(Machine__get_Regs(m_150)) + d_39) & 65535);
    const patternInput_50 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_150), R8.A), Machine__Read_Z524259A4(m_150, RegisterFile__Wz(Machine__get_Regs(m_150))), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_150), R8.A, patternInput_50[0]);
    Machine__SetFlags_2901ED1A(m_150, patternInput_50[1]);
}, (m_151) => {
    Machine__Fetch(m_151);
    Machine__Fetch(m_151);
    const patternInput_51 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_151), R8.A, patternInput_51[0]);
    Machine__SetFlags_2901ED1A(m_151, patternInput_51[1]);
}, (m_152) => {
    let copyOfStruct_12;
    Machine__Fetch(m_152);
    Machine__Fetch(m_152);
    const patternInput_52 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.B), (copyOfStruct_12 = Machine__Flags(m_152), Flags__get_carry(copyOfStruct_12)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_152), R8.A, patternInput_52[0]);
    Machine__SetFlags_2901ED1A(m_152, patternInput_52[1]);
}, (m_153) => {
    let copyOfStruct_13;
    Machine__Fetch(m_153);
    Machine__Fetch(m_153);
    const patternInput_53 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.C), (copyOfStruct_13 = Machine__Flags(m_153), Flags__get_carry(copyOfStruct_13)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_153), R8.A, patternInput_53[0]);
    Machine__SetFlags_2901ED1A(m_153, patternInput_53[1]);
}, (m_154) => {
    let copyOfStruct_14;
    Machine__Fetch(m_154);
    Machine__Fetch(m_154);
    const patternInput_54 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.D), (copyOfStruct_14 = Machine__Flags(m_154), Flags__get_carry(copyOfStruct_14)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_154), R8.A, patternInput_54[0]);
    Machine__SetFlags_2901ED1A(m_154, patternInput_54[1]);
}, (m_155) => {
    let copyOfStruct_15;
    Machine__Fetch(m_155);
    Machine__Fetch(m_155);
    const patternInput_55 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.E), (copyOfStruct_15 = Machine__Flags(m_155), Flags__get_carry(copyOfStruct_15)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_155), R8.A, patternInput_55[0]);
    Machine__SetFlags_2901ED1A(m_155, patternInput_55[1]);
}, (m_156) => {
    let copyOfStruct_16;
    Machine__Fetch(m_156);
    Machine__Fetch(m_156);
    const patternInput_56 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.IXH), (copyOfStruct_16 = Machine__Flags(m_156), Flags__get_carry(copyOfStruct_16)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_156), R8.A, patternInput_56[0]);
    Machine__SetFlags_2901ED1A(m_156, patternInput_56[1]);
}, (m_157) => {
    let copyOfStruct_17;
    Machine__Fetch(m_157);
    Machine__Fetch(m_157);
    const patternInput_57 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.IXL), (copyOfStruct_17 = Machine__Flags(m_157), Flags__get_carry(copyOfStruct_17)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_157), R8.A, patternInput_57[0]);
    Machine__SetFlags_2901ED1A(m_157, patternInput_57[1]);
}, (m_158) => {
    let copyOfStruct_18;
    Machine__Fetch(m_158);
    Machine__Fetch(m_158);
    const d_40 = Machine__ReadImm(m_158) | 0;
    const d_41 = ((d_40 >= 128) ? (d_40 - 256) : d_40) | 0;
    Machine__PassTime_Z524259A4(m_158, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_158), (RegisterFile__Ix(Machine__get_Regs(m_158)) + d_41) & 65535);
    const patternInput_58 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_158), R8.A), Machine__Read_Z524259A4(m_158, RegisterFile__Wz(Machine__get_Regs(m_158))), (copyOfStruct_18 = Machine__Flags(m_158), Flags__get_carry(copyOfStruct_18)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_158), R8.A, patternInput_58[0]);
    Machine__SetFlags_2901ED1A(m_158, patternInput_58[1]);
}, (m_159) => {
    let copyOfStruct_19;
    Machine__Fetch(m_159);
    Machine__Fetch(m_159);
    const patternInput_59 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A), (copyOfStruct_19 = Machine__Flags(m_159), Flags__get_carry(copyOfStruct_19)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_159), R8.A, patternInput_59[0]);
    Machine__SetFlags_2901ED1A(m_159, patternInput_59[1]);
}, (m_160) => {
    Machine__Fetch(m_160);
    Machine__Fetch(m_160);
    const patternInput_60 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_160), R8.A, patternInput_60[0]);
    Machine__SetFlags_2901ED1A(m_160, patternInput_60[1]);
}, (m_161) => {
    Machine__Fetch(m_161);
    Machine__Fetch(m_161);
    const patternInput_61 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_161), R8.A, patternInput_61[0]);
    Machine__SetFlags_2901ED1A(m_161, patternInput_61[1]);
}, (m_162) => {
    Machine__Fetch(m_162);
    Machine__Fetch(m_162);
    const patternInput_62 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_162), R8.A, patternInput_62[0]);
    Machine__SetFlags_2901ED1A(m_162, patternInput_62[1]);
}, (m_163) => {
    Machine__Fetch(m_163);
    Machine__Fetch(m_163);
    const patternInput_63 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_163), R8.A, patternInput_63[0]);
    Machine__SetFlags_2901ED1A(m_163, patternInput_63[1]);
}, (m_164) => {
    Machine__Fetch(m_164);
    Machine__Fetch(m_164);
    const patternInput_64 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.IXH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_164), R8.A, patternInput_64[0]);
    Machine__SetFlags_2901ED1A(m_164, patternInput_64[1]);
}, (m_165) => {
    Machine__Fetch(m_165);
    Machine__Fetch(m_165);
    const patternInput_65 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.IXL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_165), R8.A, patternInput_65[0]);
    Machine__SetFlags_2901ED1A(m_165, patternInput_65[1]);
}, (m_166) => {
    Machine__Fetch(m_166);
    Machine__Fetch(m_166);
    const d_42 = Machine__ReadImm(m_166) | 0;
    const d_43 = ((d_42 >= 128) ? (d_42 - 256) : d_42) | 0;
    Machine__PassTime_Z524259A4(m_166, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_166), (RegisterFile__Ix(Machine__get_Regs(m_166)) + d_43) & 65535);
    const patternInput_66 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_166), R8.A), Machine__Read_Z524259A4(m_166, RegisterFile__Wz(Machine__get_Regs(m_166))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_166), R8.A, patternInput_66[0]);
    Machine__SetFlags_2901ED1A(m_166, patternInput_66[1]);
}, (m_167) => {
    Machine__Fetch(m_167);
    Machine__Fetch(m_167);
    const patternInput_67 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_167), R8.A, patternInput_67[0]);
    Machine__SetFlags_2901ED1A(m_167, patternInput_67[1]);
}, (m_168) => {
    Machine__Fetch(m_168);
    Machine__Fetch(m_168);
    const patternInput_68 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_168), R8.A, patternInput_68[0]);
    Machine__SetFlags_2901ED1A(m_168, patternInput_68[1]);
}, (m_169) => {
    Machine__Fetch(m_169);
    Machine__Fetch(m_169);
    const patternInput_69 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_169), R8.A, patternInput_69[0]);
    Machine__SetFlags_2901ED1A(m_169, patternInput_69[1]);
}, (m_170) => {
    Machine__Fetch(m_170);
    Machine__Fetch(m_170);
    const patternInput_70 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_170), R8.A, patternInput_70[0]);
    Machine__SetFlags_2901ED1A(m_170, patternInput_70[1]);
}, (m_171) => {
    Machine__Fetch(m_171);
    Machine__Fetch(m_171);
    const patternInput_71 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_171), R8.A, patternInput_71[0]);
    Machine__SetFlags_2901ED1A(m_171, patternInput_71[1]);
}, (m_172) => {
    Machine__Fetch(m_172);
    Machine__Fetch(m_172);
    const patternInput_72 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.IXH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_172), R8.A, patternInput_72[0]);
    Machine__SetFlags_2901ED1A(m_172, patternInput_72[1]);
}, (m_173) => {
    Machine__Fetch(m_173);
    Machine__Fetch(m_173);
    const patternInput_73 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.IXL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_173), R8.A, patternInput_73[0]);
    Machine__SetFlags_2901ED1A(m_173, patternInput_73[1]);
}, (m_174) => {
    Machine__Fetch(m_174);
    Machine__Fetch(m_174);
    const d_44 = Machine__ReadImm(m_174) | 0;
    const d_45 = ((d_44 >= 128) ? (d_44 - 256) : d_44) | 0;
    Machine__PassTime_Z524259A4(m_174, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_174), (RegisterFile__Ix(Machine__get_Regs(m_174)) + d_45) & 65535);
    const patternInput_74 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_174), R8.A), Machine__Read_Z524259A4(m_174, RegisterFile__Wz(Machine__get_Regs(m_174))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_174), R8.A, patternInput_74[0]);
    Machine__SetFlags_2901ED1A(m_174, patternInput_74[1]);
}, (m_175) => {
    Machine__Fetch(m_175);
    Machine__Fetch(m_175);
    const patternInput_75 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_175), R8.A, patternInput_75[0]);
    Machine__SetFlags_2901ED1A(m_175, patternInput_75[1]);
}, (m_176) => {
    Machine__Fetch(m_176);
    Machine__Fetch(m_176);
    const patternInput_76 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_176), R8.A, patternInput_76[0]);
    Machine__SetFlags_2901ED1A(m_176, patternInput_76[1]);
}, (m_177) => {
    Machine__Fetch(m_177);
    Machine__Fetch(m_177);
    const patternInput_77 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_177), R8.A, patternInput_77[0]);
    Machine__SetFlags_2901ED1A(m_177, patternInput_77[1]);
}, (m_178) => {
    Machine__Fetch(m_178);
    Machine__Fetch(m_178);
    const patternInput_78 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_178), R8.A, patternInput_78[0]);
    Machine__SetFlags_2901ED1A(m_178, patternInput_78[1]);
}, (m_179) => {
    Machine__Fetch(m_179);
    Machine__Fetch(m_179);
    const patternInput_79 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_179), R8.A, patternInput_79[0]);
    Machine__SetFlags_2901ED1A(m_179, patternInput_79[1]);
}, (m_180) => {
    Machine__Fetch(m_180);
    Machine__Fetch(m_180);
    const patternInput_80 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.IXH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_180), R8.A, patternInput_80[0]);
    Machine__SetFlags_2901ED1A(m_180, patternInput_80[1]);
}, (m_181) => {
    Machine__Fetch(m_181);
    Machine__Fetch(m_181);
    const patternInput_81 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.IXL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_181), R8.A, patternInput_81[0]);
    Machine__SetFlags_2901ED1A(m_181, patternInput_81[1]);
}, (m_182) => {
    Machine__Fetch(m_182);
    Machine__Fetch(m_182);
    const d_46 = Machine__ReadImm(m_182) | 0;
    const d_47 = ((d_46 >= 128) ? (d_46 - 256) : d_46) | 0;
    Machine__PassTime_Z524259A4(m_182, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_182), (RegisterFile__Ix(Machine__get_Regs(m_182)) + d_47) & 65535);
    const patternInput_82 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_182), R8.A), Machine__Read_Z524259A4(m_182, RegisterFile__Wz(Machine__get_Regs(m_182))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_182), R8.A, patternInput_82[0]);
    Machine__SetFlags_2901ED1A(m_182, patternInput_82[1]);
}, (m_183) => {
    Machine__Fetch(m_183);
    Machine__Fetch(m_183);
    const patternInput_83 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_183), R8.A, patternInput_83[0]);
    Machine__SetFlags_2901ED1A(m_183, patternInput_83[1]);
}, (m_184) => {
    Machine__Fetch(m_184);
    Machine__Fetch(m_184);
    const patternInput_84 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_184), R8.A, patternInput_84[0]);
    Machine__SetFlags_2901ED1A(m_184, patternInput_84[1]);
}, (m_185) => {
    Machine__Fetch(m_185);
    Machine__Fetch(m_185);
    const patternInput_85 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_185), R8.A, patternInput_85[0]);
    Machine__SetFlags_2901ED1A(m_185, patternInput_85[1]);
}, (m_186) => {
    Machine__Fetch(m_186);
    Machine__Fetch(m_186);
    const patternInput_86 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_186), R8.A, patternInput_86[0]);
    Machine__SetFlags_2901ED1A(m_186, patternInput_86[1]);
}, (m_187) => {
    Machine__Fetch(m_187);
    Machine__Fetch(m_187);
    const patternInput_87 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_187), R8.A, patternInput_87[0]);
    Machine__SetFlags_2901ED1A(m_187, patternInput_87[1]);
}, (m_188) => {
    Machine__Fetch(m_188);
    Machine__Fetch(m_188);
    const patternInput_88 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.IXH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_188), R8.A, patternInput_88[0]);
    Machine__SetFlags_2901ED1A(m_188, patternInput_88[1]);
}, (m_189) => {
    Machine__Fetch(m_189);
    Machine__Fetch(m_189);
    const patternInput_89 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.IXL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_189), R8.A, patternInput_89[0]);
    Machine__SetFlags_2901ED1A(m_189, patternInput_89[1]);
}, (m_190) => {
    Machine__Fetch(m_190);
    Machine__Fetch(m_190);
    const d_48 = Machine__ReadImm(m_190) | 0;
    const d_49 = ((d_48 >= 128) ? (d_48 - 256) : d_48) | 0;
    Machine__PassTime_Z524259A4(m_190, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_190), (RegisterFile__Ix(Machine__get_Regs(m_190)) + d_49) & 65535);
    const patternInput_90 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_190), R8.A), Machine__Read_Z524259A4(m_190, RegisterFile__Wz(Machine__get_Regs(m_190))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_190), R8.A, patternInput_90[0]);
    Machine__SetFlags_2901ED1A(m_190, patternInput_90[1]);
}, (m_191) => {
    Machine__Fetch(m_191);
    Machine__Fetch(m_191);
    const patternInput_91 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_191), R8.A, patternInput_91[0]);
    Machine__SetFlags_2901ED1A(m_191, patternInput_91[1]);
}, (m_192) => {
    let copyOfStruct_20;
    Machine__Fetch(m_192);
    Machine__PassTime_Z524259A4(m_192, 1);
    if (!((copyOfStruct_20 = Machine__Flags(m_192), Flags__get_zero(copyOfStruct_20)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_192), Machine__Pop16(m_192));
    }
}, (m_193) => {
    Machine__Fetch(m_193);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_193), R16.BC, Machine__Pop16(m_193));
}, (m_194) => {
    let copyOfStruct_21;
    Machine__Fetch(m_194);
    const jumpAddress = Machine__ReadImm16(m_194) | 0;
    if (!((copyOfStruct_21 = Machine__Flags(m_194), Flags__get_zero(copyOfStruct_21)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_194), jumpAddress);
    }
}, (m_195) => {
    Machine__Fetch(m_195);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_195), Machine__ReadImm16(m_195));
}, (m_196) => {
    let copyOfStruct_22;
    Machine__Fetch(m_196);
    const jumpAddress_1 = Machine__ReadImm16(m_196) | 0;
    if (!((copyOfStruct_22 = Machine__Flags(m_196), Flags__get_zero(copyOfStruct_22)))) {
        Machine__PassTime_Z524259A4(m_196, 1);
        Machine__Push16_Z524259A4(m_196, RegisterFile__Pc(Machine__get_Regs(m_196)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_196), jumpAddress_1);
    }
}, (m_197) => {
    Machine__Fetch(m_197);
    Machine__PassTime_Z524259A4(m_197, 1);
    Machine__Push16_Z524259A4(m_197, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_197), R16.BC));
}, (m_198) => {
    Machine__Fetch(m_198);
    const patternInput_92 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_198), R8.A), Machine__ReadImm(m_198), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_198), R8.A, patternInput_92[0]);
    Machine__SetFlags_2901ED1A(m_198, patternInput_92[1]);
}, (m_199) => {
    Machine__Fetch(m_199);
    Machine__PassTime_Z524259A4(m_199, 1);
    Machine__Push16_Z524259A4(m_199, RegisterFile__Pc(Machine__get_Regs(m_199)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_199), 0);
}, (m_200) => {
    let copyOfStruct_23;
    Machine__Fetch(m_200);
    Machine__PassTime_Z524259A4(m_200, 1);
    if ((copyOfStruct_23 = Machine__Flags(m_200), Flags__get_zero(copyOfStruct_23))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_200), Machine__Pop16(m_200));
    }
}, (m_201) => {
    Machine__Fetch(m_201);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_201), Machine__Pop16(m_201));
}, (m_202) => {
    let copyOfStruct_24;
    Machine__Fetch(m_202);
    const jumpAddress_2 = Machine__ReadImm16(m_202) | 0;
    if ((copyOfStruct_24 = Machine__Flags(m_202), Flags__get_zero(copyOfStruct_24))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_202), jumpAddress_2);
    }
}, (m_203) => {
    Machine__Fetch(m_203);
    Machine__Fetch(m_203);
}, (m_204) => {
    let copyOfStruct_25;
    Machine__Fetch(m_204);
    const jumpAddress_3 = Machine__ReadImm16(m_204) | 0;
    if ((copyOfStruct_25 = Machine__Flags(m_204), Flags__get_zero(copyOfStruct_25))) {
        Machine__PassTime_Z524259A4(m_204, 1);
        Machine__Push16_Z524259A4(m_204, RegisterFile__Pc(Machine__get_Regs(m_204)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_204), jumpAddress_3);
    }
}, (m_205) => {
    Machine__Fetch(m_205);
    const jumpAddress_4 = Machine__ReadImm16(m_205) | 0;
    Machine__PassTime_Z524259A4(m_205, 1);
    Machine__Push16_Z524259A4(m_205, RegisterFile__Pc(Machine__get_Regs(m_205)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_205), jumpAddress_4);
}, (m_206) => {
    let copyOfStruct_26;
    Machine__Fetch(m_206);
    const patternInput_93 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_206), R8.A), Machine__ReadImm(m_206), (copyOfStruct_26 = Machine__Flags(m_206), Flags__get_carry(copyOfStruct_26)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_206), R8.A, patternInput_93[0]);
    Machine__SetFlags_2901ED1A(m_206, patternInput_93[1]);
}, (m_207) => {
    Machine__Fetch(m_207);
    Machine__PassTime_Z524259A4(m_207, 1);
    Machine__Push16_Z524259A4(m_207, RegisterFile__Pc(Machine__get_Regs(m_207)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_207), 8);
}, (m_208) => {
    let copyOfStruct_27;
    Machine__Fetch(m_208);
    Machine__PassTime_Z524259A4(m_208, 1);
    if (!((copyOfStruct_27 = Machine__Flags(m_208), Flags__get_carry(copyOfStruct_27)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_208), Machine__Pop16(m_208));
    }
}, (m_209) => {
    Machine__Fetch(m_209);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_209), R16.DE, Machine__Pop16(m_209));
}, (m_210) => {
    let copyOfStruct_28;
    Machine__Fetch(m_210);
    const jumpAddress_5 = Machine__ReadImm16(m_210) | 0;
    if (!((copyOfStruct_28 = Machine__Flags(m_210), Flags__get_carry(copyOfStruct_28)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_210), jumpAddress_5);
    }
}, (m_211) => {
    Machine__Fetch(m_211);
    const a = RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_211), R8.A) | 0;
    const port = ((Machine__ReadImm(m_211) | (a << 8)) & 65535) | 0;
    Machine__PassTime_Z524259A4(m_211, 4);
    Machine__Out_Z37302880(m_211, port, a);
}, (m_212) => {
    let copyOfStruct_29;
    Machine__Fetch(m_212);
    const jumpAddress_6 = Machine__ReadImm16(m_212) | 0;
    if (!((copyOfStruct_29 = Machine__Flags(m_212), Flags__get_carry(copyOfStruct_29)))) {
        Machine__PassTime_Z524259A4(m_212, 1);
        Machine__Push16_Z524259A4(m_212, RegisterFile__Pc(Machine__get_Regs(m_212)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_212), jumpAddress_6);
    }
}, (m_213) => {
    Machine__Fetch(m_213);
    Machine__PassTime_Z524259A4(m_213, 1);
    Machine__Push16_Z524259A4(m_213, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_213), R16.DE));
}, (m_214) => {
    Machine__Fetch(m_214);
    const patternInput_94 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_214), R8.A), Machine__ReadImm(m_214), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_214), R8.A, patternInput_94[0]);
    Machine__SetFlags_2901ED1A(m_214, patternInput_94[1]);
}, (m_215) => {
    Machine__Fetch(m_215);
    Machine__PassTime_Z524259A4(m_215, 1);
    Machine__Push16_Z524259A4(m_215, RegisterFile__Pc(Machine__get_Regs(m_215)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_215), 16);
}, (m_216) => {
    let copyOfStruct_30;
    Machine__Fetch(m_216);
    Machine__PassTime_Z524259A4(m_216, 1);
    if ((copyOfStruct_30 = Machine__Flags(m_216), Flags__get_carry(copyOfStruct_30))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_216), Machine__Pop16(m_216));
    }
}, (m_217) => {
    Machine__Fetch(m_217);
    RegisterFile__Exx(Machine__get_Regs(m_217));
}, (m_218) => {
    let copyOfStruct_31;
    Machine__Fetch(m_218);
    const jumpAddress_7 = Machine__ReadImm16(m_218) | 0;
    if ((copyOfStruct_31 = Machine__Flags(m_218), Flags__get_carry(copyOfStruct_31))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_218), jumpAddress_7);
    }
}, (m_219) => {
    Machine__Fetch(m_219);
    const port_1 = ((Machine__ReadImm(m_219) | (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_219), R8.A) << 8)) & 65535) | 0;
    Machine__PassTime_Z524259A4(m_219, 4);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_219), R8.A, Machine__In_Z524259A4(m_219, port_1));
}, (m_220) => {
    let copyOfStruct_32;
    Machine__Fetch(m_220);
    const jumpAddress_8 = Machine__ReadImm16(m_220) | 0;
    if ((copyOfStruct_32 = Machine__Flags(m_220), Flags__get_carry(copyOfStruct_32))) {
        Machine__PassTime_Z524259A4(m_220, 1);
        Machine__Push16_Z524259A4(m_220, RegisterFile__Pc(Machine__get_Regs(m_220)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_220), jumpAddress_8);
    }
}, (m_221) => {
    Machine__Fetch(m_221);
    Machine__Fetch(m_221);
}, (m_222) => {
    let copyOfStruct_33;
    Machine__Fetch(m_222);
    Machine__Fetch(m_222);
    const patternInput_95 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_222), R8.A), Machine__ReadImm(m_222), (copyOfStruct_33 = Machine__Flags(m_222), Flags__get_carry(copyOfStruct_33)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_222), R8.A, patternInput_95[0]);
    Machine__SetFlags_2901ED1A(m_222, patternInput_95[1]);
}, (m_223) => {
    Machine__Fetch(m_223);
    Machine__PassTime_Z524259A4(m_223, 1);
    Machine__Push16_Z524259A4(m_223, RegisterFile__Pc(Machine__get_Regs(m_223)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_223), 24);
}, (m_224) => {
    let copyOfStruct_34;
    Machine__Fetch(m_224);
    Machine__PassTime_Z524259A4(m_224, 1);
    if (!((copyOfStruct_34 = Machine__Flags(m_224), Flags__get_parity(copyOfStruct_34)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_224), Machine__Pop16(m_224));
    }
}, (m_225) => {
    Machine__Fetch(m_225);
    Machine__Fetch(m_225);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_225), R16.IX, Machine__Pop16(m_225));
}, (m_226) => {
    let copyOfStruct_35;
    Machine__Fetch(m_226);
    const jumpAddress_9 = Machine__ReadImm16(m_226) | 0;
    if (!((copyOfStruct_35 = Machine__Flags(m_226), Flags__get_parity(copyOfStruct_35)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_226), jumpAddress_9);
    }
}, (m_227) => {
    Machine__Fetch(m_227);
    Machine__Fetch(m_227);
    const sp = RegisterFile__Sp(Machine__get_Regs(m_227)) | 0;
    const spOldLow = Machine__Read_Z524259A4(m_227, sp) | 0;
    Machine__PassTime_Z524259A4(m_227, 1);
    const spOldHigh = Machine__Read_Z524259A4(m_227, (sp + 1) & 65535) | 0;
    Machine__Write_Z37302880(m_227, sp, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_227), R8.IXL));
    Machine__PassTime_Z524259A4(m_227, 2);
    Machine__Write_Z37302880(m_227, (sp + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_227), R8.IXH));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_227), R16.IX, (spOldLow | (spOldHigh << 8)) & 65535);
}, (m_228) => {
    let copyOfStruct_36;
    Machine__Fetch(m_228);
    const jumpAddress_10 = Machine__ReadImm16(m_228) | 0;
    if (!((copyOfStruct_36 = Machine__Flags(m_228), Flags__get_parity(copyOfStruct_36)))) {
        Machine__PassTime_Z524259A4(m_228, 1);
        Machine__Push16_Z524259A4(m_228, RegisterFile__Pc(Machine__get_Regs(m_228)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_228), jumpAddress_10);
    }
}, (m_229) => {
    Machine__Fetch(m_229);
    Machine__Fetch(m_229);
    Machine__PassTime_Z524259A4(m_229, 1);
    Machine__Push16_Z524259A4(m_229, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_229), R16.IX));
}, (m_230) => {
    Machine__Fetch(m_230);
    const patternInput_96 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_230), R8.A), Machine__ReadImm(m_230));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_230), R8.A, patternInput_96[0]);
    Machine__SetFlags_2901ED1A(m_230, patternInput_96[1]);
}, (m_231) => {
    Machine__Fetch(m_231);
    Machine__PassTime_Z524259A4(m_231, 1);
    Machine__Push16_Z524259A4(m_231, RegisterFile__Pc(Machine__get_Regs(m_231)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_231), 32);
}, (m_232) => {
    let copyOfStruct_37;
    Machine__Fetch(m_232);
    Machine__PassTime_Z524259A4(m_232, 1);
    if ((copyOfStruct_37 = Machine__Flags(m_232), Flags__get_parity(copyOfStruct_37))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_232), Machine__Pop16(m_232));
    }
}, (m_233) => {
    Machine__Fetch(m_233);
    Machine__Fetch(m_233);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_233), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_233), R16.IX));
}, (m_234) => {
    let copyOfStruct_38;
    Machine__Fetch(m_234);
    const jumpAddress_11 = Machine__ReadImm16(m_234) | 0;
    if ((copyOfStruct_38 = Machine__Flags(m_234), Flags__get_parity(copyOfStruct_38))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_234), jumpAddress_11);
    }
}, (m_235) => {
    Machine__Fetch(m_235);
    RegisterFile__Ex_Z3F9DF200(Machine__get_Regs(m_235), R16.DE, R16.HL);
}, (m_236) => {
    let copyOfStruct_39;
    Machine__Fetch(m_236);
    const jumpAddress_12 = Machine__ReadImm16(m_236) | 0;
    if ((copyOfStruct_39 = Machine__Flags(m_236), Flags__get_parity(copyOfStruct_39))) {
        Machine__PassTime_Z524259A4(m_236, 1);
        Machine__Push16_Z524259A4(m_236, RegisterFile__Pc(Machine__get_Regs(m_236)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_236), jumpAddress_12);
    }
}, (m_237) => {
    Machine__Fetch(m_237);
    Machine__Fetch(m_237);
}, (m_238) => {
    Machine__Fetch(m_238);
    const patternInput_97 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_238), R8.A), Machine__ReadImm(m_238));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_238), R8.A, patternInput_97[0]);
    Machine__SetFlags_2901ED1A(m_238, patternInput_97[1]);
}, (m_239) => {
    Machine__Fetch(m_239);
    Machine__PassTime_Z524259A4(m_239, 1);
    Machine__Push16_Z524259A4(m_239, RegisterFile__Pc(Machine__get_Regs(m_239)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_239), 40);
}, (m_240) => {
    let copyOfStruct_40;
    Machine__Fetch(m_240);
    Machine__PassTime_Z524259A4(m_240, 1);
    if (!((copyOfStruct_40 = Machine__Flags(m_240), Flags__get_sign(copyOfStruct_40)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_240), Machine__Pop16(m_240));
    }
}, (m_241) => {
    Machine__Fetch(m_241);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_241), R16.AF, Machine__Pop16(m_241));
}, (m_242) => {
    let copyOfStruct_41;
    Machine__Fetch(m_242);
    const jumpAddress_13 = Machine__ReadImm16(m_242) | 0;
    if (!((copyOfStruct_41 = Machine__Flags(m_242), Flags__get_sign(copyOfStruct_41)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_242), jumpAddress_13);
    }
}, (m_243) => {
    Machine__Fetch(m_243);
    Machine__set_Iff1_Z1FBCCD16(m_243, false);
    Machine__set_Iff2_Z1FBCCD16(m_243, false);
}, (m_244) => {
    let copyOfStruct_42;
    Machine__Fetch(m_244);
    const jumpAddress_14 = Machine__ReadImm16(m_244) | 0;
    if (!((copyOfStruct_42 = Machine__Flags(m_244), Flags__get_sign(copyOfStruct_42)))) {
        Machine__PassTime_Z524259A4(m_244, 1);
        Machine__Push16_Z524259A4(m_244, RegisterFile__Pc(Machine__get_Regs(m_244)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_244), jumpAddress_14);
    }
}, (m_245) => {
    Machine__Fetch(m_245);
    Machine__PassTime_Z524259A4(m_245, 1);
    Machine__Push16_Z524259A4(m_245, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_245), R16.AF));
}, (m_246) => {
    Machine__Fetch(m_246);
    const patternInput_98 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_246), R8.A), Machine__ReadImm(m_246));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_246), R8.A, patternInput_98[0]);
    Machine__SetFlags_2901ED1A(m_246, patternInput_98[1]);
}, (m_247) => {
    Machine__Fetch(m_247);
    Machine__PassTime_Z524259A4(m_247, 1);
    Machine__Push16_Z524259A4(m_247, RegisterFile__Pc(Machine__get_Regs(m_247)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_247), 48);
}, (m_248) => {
    let copyOfStruct_43;
    Machine__Fetch(m_248);
    Machine__PassTime_Z524259A4(m_248, 1);
    if ((copyOfStruct_43 = Machine__Flags(m_248), Flags__get_sign(copyOfStruct_43))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_248), Machine__Pop16(m_248));
    }
}, (m_249) => {
    Machine__Fetch(m_249);
    Machine__Fetch(m_249);
    RegisterFile__SetSp_Z524259A4(Machine__get_Regs(m_249), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_249), R16.IX));
    Machine__PassTime_Z524259A4(m_249, 2);
}, (m_250) => {
    let copyOfStruct_44;
    Machine__Fetch(m_250);
    const jumpAddress_15 = Machine__ReadImm16(m_250) | 0;
    if ((copyOfStruct_44 = Machine__Flags(m_250), Flags__get_sign(copyOfStruct_44))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_250), jumpAddress_15);
    }
}, (m_251) => {
    Machine__Fetch(m_251);
    Machine__set_Iff1_Z1FBCCD16(m_251, true);
    Machine__set_Iff2_Z1FBCCD16(m_251, true);
}, (m_252) => {
    let copyOfStruct_45;
    Machine__Fetch(m_252);
    const jumpAddress_16 = Machine__ReadImm16(m_252) | 0;
    if ((copyOfStruct_45 = Machine__Flags(m_252), Flags__get_sign(copyOfStruct_45))) {
        Machine__PassTime_Z524259A4(m_252, 1);
        Machine__Push16_Z524259A4(m_252, RegisterFile__Pc(Machine__get_Regs(m_252)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_252), jumpAddress_16);
    }
}, (m_253) => {
    Machine__Fetch(m_253);
    Machine__Fetch(m_253);
}, (m_254) => {
    Machine__Fetch(m_254);
    Machine__Fetch(m_254);
    const patternInput_99 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_254), R8.A), Machine__ReadImm(m_254));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_254), R8.A, patternInput_99[0]);
    Machine__SetFlags_2901ED1A(m_254, patternInput_99[1]);
}, (m_255) => {
    Machine__Fetch(m_255);
    Machine__PassTime_Z524259A4(m_255, 1);
    Machine__Push16_Z524259A4(m_255, RegisterFile__Pc(Machine__get_Regs(m_255)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_255), 56);
}];

export const fd = [(m) => {
    Machine__Fetch(m);
}, (m_1) => {
    Machine__Fetch(m_1);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.C, Machine__ReadImm(m_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.B, Machine__ReadImm(m_1));
}, (m_2) => {
    Machine__Fetch(m_2);
    Machine__Write_Z37302880(m_2, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_2), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_2), R8.A));
}, (m_3) => {
    Machine__Fetch(m_3);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_3), R16.BC, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_3), R16.BC) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_3, 2);
}, (m_4) => {
    Machine__Fetch(m_4);
    const patternInput = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_4), R8.B), Machine__Flags(m_4));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_4), R8.B, patternInput[0]);
    Machine__SetFlags_2901ED1A(m_4, patternInput[1]);
}, (m_5) => {
    Machine__Fetch(m_5);
    const patternInput_1 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_5), R8.B), Machine__Flags(m_5));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_5), R8.B, patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_5, patternInput_1[1]);
}, (m_6) => {
    Machine__Fetch(m_6);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_6), R8.B, Machine__ReadImm(m_6));
}, (m_7) => {
    Machine__Fetch(m_7);
    const patternInput_2 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_7), R8.A), Alu_Direction.Left, Machine__Flags(m_7));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_7), R8.A, patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_7, patternInput_2[1]);
}, (m_8) => {
    Machine__Fetch(m_8);
    RegisterFile__Ex_Z3F9DF200(Machine__get_Regs(m_8), R16.AF, R16.AF_);
}, (m_9) => {
    Machine__Fetch(m_9);
    Machine__Fetch(m_9);
    const patternInput_3 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_9), R16.IY), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_9), R16.BC), Machine__Flags(m_9));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_9), R16.IY, patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_9, patternInput_3[1]);
    Machine__PassTime_Z524259A4(m_9, 7);
}, (m_10) => {
    Machine__Fetch(m_10);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_10), R8.A, Machine__Read_Z524259A4(m_10, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_10), R16.BC)));
}, (m_11) => {
    Machine__Fetch(m_11);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_11), R16.BC, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_11), R16.BC) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_11, 2);
}, (m_12) => {
    Machine__Fetch(m_12);
    const patternInput_4 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_12), R8.C), Machine__Flags(m_12));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_12), R8.C, patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_12, patternInput_4[1]);
}, (m_13) => {
    Machine__Fetch(m_13);
    const patternInput_5 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_13), R8.C), Machine__Flags(m_13));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_13), R8.C, patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_13, patternInput_5[1]);
}, (m_14) => {
    Machine__Fetch(m_14);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_14), R8.C, Machine__ReadImm(m_14));
}, (m_15) => {
    Machine__Fetch(m_15);
    const patternInput_6 = Alu_fastRotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_15), R8.A), Alu_Direction.Right, Machine__Flags(m_15));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_15), R8.A, patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_15, patternInput_6[1]);
}, (m_16) => {
    Machine__Fetch(m_16);
    const offset = Machine__ReadImm(m_16) | 0;
    const offset_1 = ((offset >= 128) ? (offset - 256) : offset) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    const newB = (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_16), R8.B) - 1) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_16), R8.B, newB);
    if (newB !== 0) {
        Machine__PassTime_Z524259A4(m_16, 5);
        Machine__Branch_Z524259A4(m_16, offset_1);
    }
}, (m_17) => {
    Machine__Fetch(m_17);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.E, Machine__ReadImm(m_17));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.D, Machine__ReadImm(m_17));
}, (m_18) => {
    Machine__Fetch(m_18);
    Machine__Write_Z37302880(m_18, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_18), R16.DE), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_18), R8.A));
}, (m_19) => {
    Machine__Fetch(m_19);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_19), R16.DE, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_19), R16.DE) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_19, 2);
}, (m_20) => {
    Machine__Fetch(m_20);
    const patternInput_7 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_20), R8.D), Machine__Flags(m_20));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_20), R8.D, patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_20, patternInput_7[1]);
}, (m_21) => {
    Machine__Fetch(m_21);
    const patternInput_8 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_21), R8.D), Machine__Flags(m_21));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_21), R8.D, patternInput_8[0]);
    Machine__SetFlags_2901ED1A(m_21, patternInput_8[1]);
}, (m_22) => {
    Machine__Fetch(m_22);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_22), R8.D, Machine__ReadImm(m_22));
}, (m_23) => {
    Machine__Fetch(m_23);
    const patternInput_9 = Alu_fastRotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_23), R8.A), Alu_Direction.Left, Machine__Flags(m_23));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_23), R8.A, patternInput_9[0]);
    Machine__SetFlags_2901ED1A(m_23, patternInput_9[1]);
}, (m_24) => {
    Machine__Fetch(m_24);
    const offset_2 = Machine__ReadImm(m_24) | 0;
    const offset_3 = ((offset_2 >= 128) ? (offset_2 - 256) : offset_2) | 0;
    Machine__PassTime_Z524259A4(m_24, 5);
    Machine__Branch_Z524259A4(m_24, offset_3);
}, (m_25) => {
    Machine__Fetch(m_25);
    Machine__Fetch(m_25);
    const patternInput_10 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_25), R16.IY), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_25), R16.DE), Machine__Flags(m_25));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_25), R16.IY, patternInput_10[0]);
    Machine__SetFlags_2901ED1A(m_25, patternInput_10[1]);
    Machine__PassTime_Z524259A4(m_25, 7);
}, (m_26) => {
    Machine__Fetch(m_26);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_26), R8.A, Machine__Read_Z524259A4(m_26, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_26), R16.DE)));
}, (m_27) => {
    Machine__Fetch(m_27);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_27), R16.DE, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_27), R16.DE) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_27, 2);
}, (m_28) => {
    Machine__Fetch(m_28);
    const patternInput_11 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_28), R8.E), Machine__Flags(m_28));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_28), R8.E, patternInput_11[0]);
    Machine__SetFlags_2901ED1A(m_28, patternInput_11[1]);
}, (m_29) => {
    Machine__Fetch(m_29);
    const patternInput_12 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_29), R8.E), Machine__Flags(m_29));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_29), R8.E, patternInput_12[0]);
    Machine__SetFlags_2901ED1A(m_29, patternInput_12[1]);
}, (m_30) => {
    Machine__Fetch(m_30);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_30), R8.E, Machine__ReadImm(m_30));
}, (m_31) => {
    Machine__Fetch(m_31);
    const patternInput_13 = Alu_fastRotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_31), R8.A), Alu_Direction.Right, Machine__Flags(m_31));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_31), R8.A, patternInput_13[0]);
    Machine__SetFlags_2901ED1A(m_31, patternInput_13[1]);
}, (m_32) => {
    let copyOfStruct;
    Machine__Fetch(m_32);
    const offset_4 = Machine__ReadImm(m_32) | 0;
    const offset_5 = ((offset_4 >= 128) ? (offset_4 - 256) : offset_4) | 0;
    if (!((copyOfStruct = Machine__Flags(m_32), Flags__get_zero(copyOfStruct)))) {
        Machine__PassTime_Z524259A4(m_32, 5);
        Machine__Branch_Z524259A4(m_32, offset_5);
    }
}, (m_33) => {
    Machine__Fetch(m_33);
    Machine__Fetch(m_33);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.IYL, Machine__ReadImm(m_33));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.IYH, Machine__ReadImm(m_33));
}, (m_34) => {
    Machine__Fetch(m_34);
    Machine__Fetch(m_34);
    const addr = Machine__ReadImm16(m_34) | 0;
    Machine__Write_Z37302880(m_34, addr, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.IYL));
    Machine__Write_Z37302880(m_34, (addr + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.IYH));
}, (m_35) => {
    Machine__Fetch(m_35);
    Machine__Fetch(m_35);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_35), R16.IY, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_35), R16.IY) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_35, 2);
}, (m_36) => {
    Machine__Fetch(m_36);
    Machine__Fetch(m_36);
    const patternInput_14 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_36), R8.IYH), Machine__Flags(m_36));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_36), R8.IYH, patternInput_14[0]);
    Machine__SetFlags_2901ED1A(m_36, patternInput_14[1]);
}, (m_37) => {
    Machine__Fetch(m_37);
    Machine__Fetch(m_37);
    const patternInput_15 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_37), R8.IYH), Machine__Flags(m_37));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_37), R8.IYH, patternInput_15[0]);
    Machine__SetFlags_2901ED1A(m_37, patternInput_15[1]);
}, (m_38) => {
    Machine__Fetch(m_38);
    Machine__Fetch(m_38);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_38), R8.IYH, Machine__ReadImm(m_38));
}, (m_39) => {
    Machine__Fetch(m_39);
    const patternInput_16 = Alu_daa(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_39), R8.A), Machine__Flags(m_39));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_39), R8.A, patternInput_16[0]);
    Machine__SetFlags_2901ED1A(m_39, patternInput_16[1]);
}, (m_40) => {
    let copyOfStruct_1;
    Machine__Fetch(m_40);
    const offset_6 = Machine__ReadImm(m_40) | 0;
    const offset_7 = ((offset_6 >= 128) ? (offset_6 - 256) : offset_6) | 0;
    if ((copyOfStruct_1 = Machine__Flags(m_40), Flags__get_zero(copyOfStruct_1))) {
        Machine__PassTime_Z524259A4(m_40, 5);
        Machine__Branch_Z524259A4(m_40, offset_7);
    }
}, (m_41) => {
    Machine__Fetch(m_41);
    Machine__Fetch(m_41);
    const patternInput_17 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_41), R16.IY), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_41), R16.IY), Machine__Flags(m_41));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_41), R16.IY, patternInput_17[0]);
    Machine__SetFlags_2901ED1A(m_41, patternInput_17[1]);
    Machine__PassTime_Z524259A4(m_41, 7);
}, (m_42) => {
    Machine__Fetch(m_42);
    Machine__Fetch(m_42);
    const addr_1 = Machine__ReadImm16(m_42) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.IYL, Machine__Read_Z524259A4(m_42, addr_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.IYH, Machine__Read_Z524259A4(m_42, (addr_1 + 1) & 65535));
}, (m_43) => {
    Machine__Fetch(m_43);
    Machine__Fetch(m_43);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_43), R16.IY, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_43), R16.IY) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_43, 2);
}, (m_44) => {
    Machine__Fetch(m_44);
    Machine__Fetch(m_44);
    const patternInput_18 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_44), R8.IYL), Machine__Flags(m_44));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_44), R8.IYL, patternInput_18[0]);
    Machine__SetFlags_2901ED1A(m_44, patternInput_18[1]);
}, (m_45) => {
    Machine__Fetch(m_45);
    Machine__Fetch(m_45);
    const patternInput_19 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_45), R8.IYL), Machine__Flags(m_45));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_45), R8.IYL, patternInput_19[0]);
    Machine__SetFlags_2901ED1A(m_45, patternInput_19[1]);
}, (m_46) => {
    Machine__Fetch(m_46);
    Machine__Fetch(m_46);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_46), R8.IYL, Machine__ReadImm(m_46));
}, (m_47) => {
    Machine__Fetch(m_47);
    const patternInput_20 = Alu_cpl(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_47), R8.A), Machine__Flags(m_47));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_47), R8.A, patternInput_20[0]);
    Machine__SetFlags_2901ED1A(m_47, patternInput_20[1]);
}, (m_48) => {
    let copyOfStruct_2;
    Machine__Fetch(m_48);
    const offset_8 = Machine__ReadImm(m_48) | 0;
    const offset_9 = ((offset_8 >= 128) ? (offset_8 - 256) : offset_8) | 0;
    if (!((copyOfStruct_2 = Machine__Flags(m_48), Flags__get_carry(copyOfStruct_2)))) {
        Machine__PassTime_Z524259A4(m_48, 5);
        Machine__Branch_Z524259A4(m_48, offset_9);
    }
}, (m_49) => {
    Machine__Fetch(m_49);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.SPL, Machine__ReadImm(m_49));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.SPH, Machine__ReadImm(m_49));
}, (m_50) => {
    Machine__Fetch(m_50);
    Machine__Write_Z37302880(m_50, Machine__ReadImm16(m_50), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_50), R8.A));
}, (m_51) => {
    Machine__Fetch(m_51);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_51), R16.SP, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_51), R16.SP) + 1) & 65535);
    Machine__PassTime_Z524259A4(m_51, 2);
}, (m_52) => {
    Machine__Fetch(m_52);
    Machine__Fetch(m_52);
    const d = Machine__ReadImm(m_52) | 0;
    const d_1 = ((d >= 128) ? (d - 256) : d) | 0;
    Machine__PassTime_Z524259A4(m_52, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_52), (RegisterFile__Iy(Machine__get_Regs(m_52)) + d_1) & 65535);
    Machine__PassTime_Z524259A4(m_52, 1);
    const patternInput_21 = Alu_inc8(Machine__Read_Z524259A4(m_52, RegisterFile__Wz(Machine__get_Regs(m_52))), Machine__Flags(m_52));
    Machine__Write_Z37302880(m_52, RegisterFile__Wz(Machine__get_Regs(m_52)), patternInput_21[0]);
    Machine__SetFlags_2901ED1A(m_52, patternInput_21[1]);
}, (m_53) => {
    Machine__Fetch(m_53);
    Machine__Fetch(m_53);
    const d_2 = Machine__ReadImm(m_53) | 0;
    const d_3 = ((d_2 >= 128) ? (d_2 - 256) : d_2) | 0;
    Machine__PassTime_Z524259A4(m_53, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_53), (RegisterFile__Iy(Machine__get_Regs(m_53)) + d_3) & 65535);
    Machine__PassTime_Z524259A4(m_53, 1);
    const patternInput_22 = Alu_dec8(Machine__Read_Z524259A4(m_53, RegisterFile__Wz(Machine__get_Regs(m_53))), Machine__Flags(m_53));
    Machine__Write_Z37302880(m_53, RegisterFile__Wz(Machine__get_Regs(m_53)), patternInput_22[0]);
    Machine__SetFlags_2901ED1A(m_53, patternInput_22[1]);
}, (m_54) => {
    Machine__Fetch(m_54);
    Machine__Fetch(m_54);
    const d_4 = Machine__ReadImm(m_54) | 0;
    const d_5 = ((d_4 >= 128) ? (d_4 - 256) : d_4) | 0;
    Machine__PassTime_Z524259A4(m_54, 2);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_54), (RegisterFile__Iy(Machine__get_Regs(m_54)) + d_5) & 65535);
    Machine__Write_Z37302880(m_54, RegisterFile__Wz(Machine__get_Regs(m_54)), Machine__ReadImm(m_54));
}, (m_55) => {
    Machine__Fetch(m_55);
    const patternInput_23 = Alu_scf(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_55), R8.A), Machine__Flags(m_55));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_55), R8.A, patternInput_23[0]);
    Machine__SetFlags_2901ED1A(m_55, patternInput_23[1]);
}, (m_56) => {
    let copyOfStruct_3;
    Machine__Fetch(m_56);
    const offset_10 = Machine__ReadImm(m_56) | 0;
    const offset_11 = ((offset_10 >= 128) ? (offset_10 - 256) : offset_10) | 0;
    if ((copyOfStruct_3 = Machine__Flags(m_56), Flags__get_carry(copyOfStruct_3))) {
        Machine__PassTime_Z524259A4(m_56, 5);
        Machine__Branch_Z524259A4(m_56, offset_11);
    }
}, (m_57) => {
    Machine__Fetch(m_57);
    Machine__Fetch(m_57);
    const patternInput_24 = Alu_add16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_57), R16.IY), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_57), R16.SP), Machine__Flags(m_57));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_57), R16.IY, patternInput_24[0]);
    Machine__SetFlags_2901ED1A(m_57, patternInput_24[1]);
    Machine__PassTime_Z524259A4(m_57, 7);
}, (m_58) => {
    Machine__Fetch(m_58);
    const addr_3 = Machine__ReadImm16(m_58) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_58), R8.A, Machine__Read_Z524259A4(m_58, addr_3));
}, (m_59) => {
    Machine__Fetch(m_59);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_59), R16.SP, (RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_59), R16.SP) - 1) & 65535);
    Machine__PassTime_Z524259A4(m_59, 2);
}, (m_60) => {
    Machine__Fetch(m_60);
    const patternInput_25 = Alu_inc8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_60), R8.A), Machine__Flags(m_60));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_60), R8.A, patternInput_25[0]);
    Machine__SetFlags_2901ED1A(m_60, patternInput_25[1]);
}, (m_61) => {
    Machine__Fetch(m_61);
    const patternInput_26 = Alu_dec8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_61), R8.A), Machine__Flags(m_61));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_61), R8.A, patternInput_26[0]);
    Machine__SetFlags_2901ED1A(m_61, patternInput_26[1]);
}, (m_62) => {
    Machine__Fetch(m_62);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_62), R8.A, Machine__ReadImm(m_62));
}, (m_63) => {
    Machine__Fetch(m_63);
    const patternInput_27 = Alu_ccf(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_63), R8.A), Machine__Flags(m_63));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_63), R8.A, patternInput_27[0]);
    Machine__SetFlags_2901ED1A(m_63, patternInput_27[1]);
}, (m_64) => {
    Machine__Fetch(m_64);
    Machine__Fetch(m_64);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_64), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_64), R8.B));
}, (m_65) => {
    Machine__Fetch(m_65);
    Machine__Fetch(m_65);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_65), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_65), R8.C));
}, (m_66) => {
    Machine__Fetch(m_66);
    Machine__Fetch(m_66);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_66), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_66), R8.D));
}, (m_67) => {
    Machine__Fetch(m_67);
    Machine__Fetch(m_67);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_67), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.E));
}, (m_68) => {
    Machine__Fetch(m_68);
    Machine__Fetch(m_68);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_68), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_68), R8.IYH));
}, (m_69) => {
    Machine__Fetch(m_69);
    Machine__Fetch(m_69);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_69), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_69), R8.IYL));
}, (m_70) => {
    Machine__Fetch(m_70);
    Machine__Fetch(m_70);
    const d_6 = Machine__ReadImm(m_70) | 0;
    const d_7 = ((d_6 >= 128) ? (d_6 - 256) : d_6) | 0;
    Machine__PassTime_Z524259A4(m_70, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_70), (RegisterFile__Iy(Machine__get_Regs(m_70)) + d_7) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_70), R8.B, Machine__Read_Z524259A4(m_70, RegisterFile__Wz(Machine__get_Regs(m_70))));
}, (m_71) => {
    Machine__Fetch(m_71);
    Machine__Fetch(m_71);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_71), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_71), R8.A));
}, (m_72) => {
    Machine__Fetch(m_72);
    Machine__Fetch(m_72);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_72), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_72), R8.B));
}, (m_73) => {
    Machine__Fetch(m_73);
    Machine__Fetch(m_73);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_73), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_73), R8.C));
}, (m_74) => {
    Machine__Fetch(m_74);
    Machine__Fetch(m_74);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_74), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_74), R8.D));
}, (m_75) => {
    Machine__Fetch(m_75);
    Machine__Fetch(m_75);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_75), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_75), R8.E));
}, (m_76) => {
    Machine__Fetch(m_76);
    Machine__Fetch(m_76);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_76), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_76), R8.IYH));
}, (m_77) => {
    Machine__Fetch(m_77);
    Machine__Fetch(m_77);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_77), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_77), R8.IYL));
}, (m_78) => {
    Machine__Fetch(m_78);
    Machine__Fetch(m_78);
    const d_8 = Machine__ReadImm(m_78) | 0;
    const d_9 = ((d_8 >= 128) ? (d_8 - 256) : d_8) | 0;
    Machine__PassTime_Z524259A4(m_78, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_78), (RegisterFile__Iy(Machine__get_Regs(m_78)) + d_9) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_78), R8.C, Machine__Read_Z524259A4(m_78, RegisterFile__Wz(Machine__get_Regs(m_78))));
}, (m_79) => {
    Machine__Fetch(m_79);
    Machine__Fetch(m_79);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_79), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_79), R8.A));
}, (m_80) => {
    Machine__Fetch(m_80);
    Machine__Fetch(m_80);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_80), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_80), R8.B));
}, (m_81) => {
    Machine__Fetch(m_81);
    Machine__Fetch(m_81);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_81), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_81), R8.C));
}, (m_82) => {
    Machine__Fetch(m_82);
    Machine__Fetch(m_82);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_82), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_82), R8.D));
}, (m_83) => {
    Machine__Fetch(m_83);
    Machine__Fetch(m_83);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_83), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.E));
}, (m_84) => {
    Machine__Fetch(m_84);
    Machine__Fetch(m_84);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_84), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_84), R8.IYH));
}, (m_85) => {
    Machine__Fetch(m_85);
    Machine__Fetch(m_85);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_85), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_85), R8.IYL));
}, (m_86) => {
    Machine__Fetch(m_86);
    Machine__Fetch(m_86);
    const d_10 = Machine__ReadImm(m_86) | 0;
    const d_11 = ((d_10 >= 128) ? (d_10 - 256) : d_10) | 0;
    Machine__PassTime_Z524259A4(m_86, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_86), (RegisterFile__Iy(Machine__get_Regs(m_86)) + d_11) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_86), R8.D, Machine__Read_Z524259A4(m_86, RegisterFile__Wz(Machine__get_Regs(m_86))));
}, (m_87) => {
    Machine__Fetch(m_87);
    Machine__Fetch(m_87);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_87), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_87), R8.A));
}, (m_88) => {
    Machine__Fetch(m_88);
    Machine__Fetch(m_88);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_88), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_88), R8.B));
}, (m_89) => {
    Machine__Fetch(m_89);
    Machine__Fetch(m_89);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_89), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_89), R8.C));
}, (m_90) => {
    Machine__Fetch(m_90);
    Machine__Fetch(m_90);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_90), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_90), R8.D));
}, (m_91) => {
    Machine__Fetch(m_91);
    Machine__Fetch(m_91);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_91), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_91), R8.E));
}, (m_92) => {
    Machine__Fetch(m_92);
    Machine__Fetch(m_92);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_92), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_92), R8.IYH));
}, (m_93) => {
    Machine__Fetch(m_93);
    Machine__Fetch(m_93);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_93), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_93), R8.IYL));
}, (m_94) => {
    Machine__Fetch(m_94);
    Machine__Fetch(m_94);
    const d_12 = Machine__ReadImm(m_94) | 0;
    const d_13 = ((d_12 >= 128) ? (d_12 - 256) : d_12) | 0;
    Machine__PassTime_Z524259A4(m_94, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_94), (RegisterFile__Iy(Machine__get_Regs(m_94)) + d_13) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_94), R8.E, Machine__Read_Z524259A4(m_94, RegisterFile__Wz(Machine__get_Regs(m_94))));
}, (m_95) => {
    Machine__Fetch(m_95);
    Machine__Fetch(m_95);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_95), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_95), R8.A));
}, (m_96) => {
    Machine__Fetch(m_96);
    Machine__Fetch(m_96);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_96), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_96), R8.B));
}, (m_97) => {
    Machine__Fetch(m_97);
    Machine__Fetch(m_97);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_97), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_97), R8.C));
}, (m_98) => {
    Machine__Fetch(m_98);
    Machine__Fetch(m_98);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_98), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_98), R8.D));
}, (m_99) => {
    Machine__Fetch(m_99);
    Machine__Fetch(m_99);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_99), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.E));
}, (m_100) => {
    Machine__Fetch(m_100);
    Machine__Fetch(m_100);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_100), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_100), R8.IYH));
}, (m_101) => {
    Machine__Fetch(m_101);
    Machine__Fetch(m_101);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_101), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_101), R8.IYL));
}, (m_102) => {
    Machine__Fetch(m_102);
    Machine__Fetch(m_102);
    const d_14 = Machine__ReadImm(m_102) | 0;
    const d_15 = ((d_14 >= 128) ? (d_14 - 256) : d_14) | 0;
    Machine__PassTime_Z524259A4(m_102, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_102), (RegisterFile__Iy(Machine__get_Regs(m_102)) + d_15) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_102), R8.H, Machine__Read_Z524259A4(m_102, RegisterFile__Wz(Machine__get_Regs(m_102))));
}, (m_103) => {
    Machine__Fetch(m_103);
    Machine__Fetch(m_103);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_103), R8.IYH, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_103), R8.A));
}, (m_104) => {
    Machine__Fetch(m_104);
    Machine__Fetch(m_104);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_104), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_104), R8.B));
}, (m_105) => {
    Machine__Fetch(m_105);
    Machine__Fetch(m_105);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_105), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_105), R8.C));
}, (m_106) => {
    Machine__Fetch(m_106);
    Machine__Fetch(m_106);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_106), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_106), R8.D));
}, (m_107) => {
    Machine__Fetch(m_107);
    Machine__Fetch(m_107);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_107), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_107), R8.E));
}, (m_108) => {
    Machine__Fetch(m_108);
    Machine__Fetch(m_108);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_108), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_108), R8.IYH));
}, (m_109) => {
    Machine__Fetch(m_109);
    Machine__Fetch(m_109);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_109), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_109), R8.IYL));
}, (m_110) => {
    Machine__Fetch(m_110);
    Machine__Fetch(m_110);
    const d_16 = Machine__ReadImm(m_110) | 0;
    const d_17 = ((d_16 >= 128) ? (d_16 - 256) : d_16) | 0;
    Machine__PassTime_Z524259A4(m_110, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_110), (RegisterFile__Iy(Machine__get_Regs(m_110)) + d_17) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_110), R8.L, Machine__Read_Z524259A4(m_110, RegisterFile__Wz(Machine__get_Regs(m_110))));
}, (m_111) => {
    Machine__Fetch(m_111);
    Machine__Fetch(m_111);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_111), R8.IYL, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_111), R8.A));
}, (m_112) => {
    Machine__Fetch(m_112);
    Machine__Fetch(m_112);
    const d_18 = Machine__ReadImm(m_112) | 0;
    const d_19 = ((d_18 >= 128) ? (d_18 - 256) : d_18) | 0;
    Machine__PassTime_Z524259A4(m_112, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_112), (RegisterFile__Iy(Machine__get_Regs(m_112)) + d_19) & 65535);
    Machine__Write_Z37302880(m_112, RegisterFile__Wz(Machine__get_Regs(m_112)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_112), R8.B));
}, (m_113) => {
    Machine__Fetch(m_113);
    Machine__Fetch(m_113);
    const d_20 = Machine__ReadImm(m_113) | 0;
    const d_21 = ((d_20 >= 128) ? (d_20 - 256) : d_20) | 0;
    Machine__PassTime_Z524259A4(m_113, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_113), (RegisterFile__Iy(Machine__get_Regs(m_113)) + d_21) & 65535);
    Machine__Write_Z37302880(m_113, RegisterFile__Wz(Machine__get_Regs(m_113)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_113), R8.C));
}, (m_114) => {
    Machine__Fetch(m_114);
    Machine__Fetch(m_114);
    const d_22 = Machine__ReadImm(m_114) | 0;
    const d_23 = ((d_22 >= 128) ? (d_22 - 256) : d_22) | 0;
    Machine__PassTime_Z524259A4(m_114, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_114), (RegisterFile__Iy(Machine__get_Regs(m_114)) + d_23) & 65535);
    Machine__Write_Z37302880(m_114, RegisterFile__Wz(Machine__get_Regs(m_114)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_114), R8.D));
}, (m_115) => {
    Machine__Fetch(m_115);
    Machine__Fetch(m_115);
    const d_24 = Machine__ReadImm(m_115) | 0;
    const d_25 = ((d_24 >= 128) ? (d_24 - 256) : d_24) | 0;
    Machine__PassTime_Z524259A4(m_115, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_115), (RegisterFile__Iy(Machine__get_Regs(m_115)) + d_25) & 65535);
    Machine__Write_Z37302880(m_115, RegisterFile__Wz(Machine__get_Regs(m_115)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.E));
}, (m_116) => {
    Machine__Fetch(m_116);
    Machine__Fetch(m_116);
    const d_26 = Machine__ReadImm(m_116) | 0;
    const d_27 = ((d_26 >= 128) ? (d_26 - 256) : d_26) | 0;
    Machine__PassTime_Z524259A4(m_116, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_116), (RegisterFile__Iy(Machine__get_Regs(m_116)) + d_27) & 65535);
    Machine__Write_Z37302880(m_116, RegisterFile__Wz(Machine__get_Regs(m_116)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_116), R8.H));
}, (m_117) => {
    Machine__Fetch(m_117);
    Machine__Fetch(m_117);
    const d_28 = Machine__ReadImm(m_117) | 0;
    const d_29 = ((d_28 >= 128) ? (d_28 - 256) : d_28) | 0;
    Machine__PassTime_Z524259A4(m_117, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_117), (RegisterFile__Iy(Machine__get_Regs(m_117)) + d_29) & 65535);
    Machine__Write_Z37302880(m_117, RegisterFile__Wz(Machine__get_Regs(m_117)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_117), R8.L));
}, (m_118) => {
    Machine__Fetch(m_118);
    Machine__Fetch(m_118);
    Machine__PassTime_Z524259A4(m_118, 1);
    Machine__Halt(m_118);
}, (m_119) => {
    Machine__Fetch(m_119);
    Machine__Fetch(m_119);
    const d_30 = Machine__ReadImm(m_119) | 0;
    const d_31 = ((d_30 >= 128) ? (d_30 - 256) : d_30) | 0;
    Machine__PassTime_Z524259A4(m_119, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_119), (RegisterFile__Iy(Machine__get_Regs(m_119)) + d_31) & 65535);
    Machine__Write_Z37302880(m_119, RegisterFile__Wz(Machine__get_Regs(m_119)), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_119), R8.A));
}, (m_120) => {
    Machine__Fetch(m_120);
    Machine__Fetch(m_120);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_120), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_120), R8.B));
}, (m_121) => {
    Machine__Fetch(m_121);
    Machine__Fetch(m_121);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_121), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_121), R8.C));
}, (m_122) => {
    Machine__Fetch(m_122);
    Machine__Fetch(m_122);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_122), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_122), R8.D));
}, (m_123) => {
    Machine__Fetch(m_123);
    Machine__Fetch(m_123);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_123), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_123), R8.E));
}, (m_124) => {
    Machine__Fetch(m_124);
    Machine__Fetch(m_124);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_124), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_124), R8.IYH));
}, (m_125) => {
    Machine__Fetch(m_125);
    Machine__Fetch(m_125);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_125), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_125), R8.IYL));
}, (m_126) => {
    Machine__Fetch(m_126);
    Machine__Fetch(m_126);
    const d_32 = Machine__ReadImm(m_126) | 0;
    const d_33 = ((d_32 >= 128) ? (d_32 - 256) : d_32) | 0;
    Machine__PassTime_Z524259A4(m_126, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_126), (RegisterFile__Iy(Machine__get_Regs(m_126)) + d_33) & 65535);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_126), R8.A, Machine__Read_Z524259A4(m_126, RegisterFile__Wz(Machine__get_Regs(m_126))));
}, (m_127) => {
    Machine__Fetch(m_127);
    Machine__Fetch(m_127);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_127), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_127), R8.A));
}, (m_128) => {
    Machine__Fetch(m_128);
    Machine__Fetch(m_128);
    const patternInput_28 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.B), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_128), R8.A, patternInput_28[0]);
    Machine__SetFlags_2901ED1A(m_128, patternInput_28[1]);
}, (m_129) => {
    Machine__Fetch(m_129);
    Machine__Fetch(m_129);
    const patternInput_29 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.C), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_129), R8.A, patternInput_29[0]);
    Machine__SetFlags_2901ED1A(m_129, patternInput_29[1]);
}, (m_130) => {
    Machine__Fetch(m_130);
    Machine__Fetch(m_130);
    const patternInput_30 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.D), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_130), R8.A, patternInput_30[0]);
    Machine__SetFlags_2901ED1A(m_130, patternInput_30[1]);
}, (m_131) => {
    Machine__Fetch(m_131);
    Machine__Fetch(m_131);
    const patternInput_31 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.E), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_131), R8.A, patternInput_31[0]);
    Machine__SetFlags_2901ED1A(m_131, patternInput_31[1]);
}, (m_132) => {
    Machine__Fetch(m_132);
    Machine__Fetch(m_132);
    const patternInput_32 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.IYH), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_132), R8.A, patternInput_32[0]);
    Machine__SetFlags_2901ED1A(m_132, patternInput_32[1]);
}, (m_133) => {
    Machine__Fetch(m_133);
    Machine__Fetch(m_133);
    const patternInput_33 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.IYL), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_133), R8.A, patternInput_33[0]);
    Machine__SetFlags_2901ED1A(m_133, patternInput_33[1]);
}, (m_134) => {
    Machine__Fetch(m_134);
    Machine__Fetch(m_134);
    const d_34 = Machine__ReadImm(m_134) | 0;
    const d_35 = ((d_34 >= 128) ? (d_34 - 256) : d_34) | 0;
    Machine__PassTime_Z524259A4(m_134, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_134), (RegisterFile__Iy(Machine__get_Regs(m_134)) + d_35) & 65535);
    const patternInput_34 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_134), R8.A), Machine__Read_Z524259A4(m_134, RegisterFile__Wz(Machine__get_Regs(m_134))), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_134), R8.A, patternInput_34[0]);
    Machine__SetFlags_2901ED1A(m_134, patternInput_34[1]);
}, (m_135) => {
    Machine__Fetch(m_135);
    Machine__Fetch(m_135);
    const patternInput_35 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_135), R8.A, patternInput_35[0]);
    Machine__SetFlags_2901ED1A(m_135, patternInput_35[1]);
}, (m_136) => {
    let copyOfStruct_4;
    Machine__Fetch(m_136);
    Machine__Fetch(m_136);
    const patternInput_36 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.B), (copyOfStruct_4 = Machine__Flags(m_136), Flags__get_carry(copyOfStruct_4)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_136), R8.A, patternInput_36[0]);
    Machine__SetFlags_2901ED1A(m_136, patternInput_36[1]);
}, (m_137) => {
    let copyOfStruct_5;
    Machine__Fetch(m_137);
    Machine__Fetch(m_137);
    const patternInput_37 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.C), (copyOfStruct_5 = Machine__Flags(m_137), Flags__get_carry(copyOfStruct_5)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_137), R8.A, patternInput_37[0]);
    Machine__SetFlags_2901ED1A(m_137, patternInput_37[1]);
}, (m_138) => {
    let copyOfStruct_6;
    Machine__Fetch(m_138);
    Machine__Fetch(m_138);
    const patternInput_38 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.D), (copyOfStruct_6 = Machine__Flags(m_138), Flags__get_carry(copyOfStruct_6)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_138), R8.A, patternInput_38[0]);
    Machine__SetFlags_2901ED1A(m_138, patternInput_38[1]);
}, (m_139) => {
    let copyOfStruct_7;
    Machine__Fetch(m_139);
    Machine__Fetch(m_139);
    const patternInput_39 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.E), (copyOfStruct_7 = Machine__Flags(m_139), Flags__get_carry(copyOfStruct_7)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_139), R8.A, patternInput_39[0]);
    Machine__SetFlags_2901ED1A(m_139, patternInput_39[1]);
}, (m_140) => {
    let copyOfStruct_8;
    Machine__Fetch(m_140);
    Machine__Fetch(m_140);
    const patternInput_40 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.IYH), (copyOfStruct_8 = Machine__Flags(m_140), Flags__get_carry(copyOfStruct_8)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_140), R8.A, patternInput_40[0]);
    Machine__SetFlags_2901ED1A(m_140, patternInput_40[1]);
}, (m_141) => {
    let copyOfStruct_9;
    Machine__Fetch(m_141);
    Machine__Fetch(m_141);
    const patternInput_41 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.IYL), (copyOfStruct_9 = Machine__Flags(m_141), Flags__get_carry(copyOfStruct_9)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_141), R8.A, patternInput_41[0]);
    Machine__SetFlags_2901ED1A(m_141, patternInput_41[1]);
}, (m_142) => {
    let copyOfStruct_10;
    Machine__Fetch(m_142);
    Machine__Fetch(m_142);
    const d_36 = Machine__ReadImm(m_142) | 0;
    const d_37 = ((d_36 >= 128) ? (d_36 - 256) : d_36) | 0;
    Machine__PassTime_Z524259A4(m_142, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_142), (RegisterFile__Iy(Machine__get_Regs(m_142)) + d_37) & 65535);
    const patternInput_42 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_142), R8.A), Machine__Read_Z524259A4(m_142, RegisterFile__Wz(Machine__get_Regs(m_142))), (copyOfStruct_10 = Machine__Flags(m_142), Flags__get_carry(copyOfStruct_10)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_142), R8.A, patternInput_42[0]);
    Machine__SetFlags_2901ED1A(m_142, patternInput_42[1]);
}, (m_143) => {
    let copyOfStruct_11;
    Machine__Fetch(m_143);
    Machine__Fetch(m_143);
    const patternInput_43 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A), (copyOfStruct_11 = Machine__Flags(m_143), Flags__get_carry(copyOfStruct_11)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_143), R8.A, patternInput_43[0]);
    Machine__SetFlags_2901ED1A(m_143, patternInput_43[1]);
}, (m_144) => {
    Machine__Fetch(m_144);
    Machine__Fetch(m_144);
    const patternInput_44 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.B), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_144), R8.A, patternInput_44[0]);
    Machine__SetFlags_2901ED1A(m_144, patternInput_44[1]);
}, (m_145) => {
    Machine__Fetch(m_145);
    Machine__Fetch(m_145);
    const patternInput_45 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.C), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_145), R8.A, patternInput_45[0]);
    Machine__SetFlags_2901ED1A(m_145, patternInput_45[1]);
}, (m_146) => {
    Machine__Fetch(m_146);
    Machine__Fetch(m_146);
    const patternInput_46 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.D), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_146), R8.A, patternInput_46[0]);
    Machine__SetFlags_2901ED1A(m_146, patternInput_46[1]);
}, (m_147) => {
    Machine__Fetch(m_147);
    Machine__Fetch(m_147);
    const patternInput_47 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.E), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_147), R8.A, patternInput_47[0]);
    Machine__SetFlags_2901ED1A(m_147, patternInput_47[1]);
}, (m_148) => {
    Machine__Fetch(m_148);
    Machine__Fetch(m_148);
    const patternInput_48 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.IYH), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_148), R8.A, patternInput_48[0]);
    Machine__SetFlags_2901ED1A(m_148, patternInput_48[1]);
}, (m_149) => {
    Machine__Fetch(m_149);
    Machine__Fetch(m_149);
    const patternInput_49 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.IYL), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_149), R8.A, patternInput_49[0]);
    Machine__SetFlags_2901ED1A(m_149, patternInput_49[1]);
}, (m_150) => {
    Machine__Fetch(m_150);
    Machine__Fetch(m_150);
    const d_38 = Machine__ReadImm(m_150) | 0;
    const d_39 = ((d_38 >= 128) ? (d_38 - 256) : d_38) | 0;
    Machine__PassTime_Z524259A4(m_150, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_150), (RegisterFile__Iy(Machine__get_Regs(m_150)) + d_39) & 65535);
    const patternInput_50 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_150), R8.A), Machine__Read_Z524259A4(m_150, RegisterFile__Wz(Machine__get_Regs(m_150))), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_150), R8.A, patternInput_50[0]);
    Machine__SetFlags_2901ED1A(m_150, patternInput_50[1]);
}, (m_151) => {
    Machine__Fetch(m_151);
    Machine__Fetch(m_151);
    const patternInput_51 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_151), R8.A, patternInput_51[0]);
    Machine__SetFlags_2901ED1A(m_151, patternInput_51[1]);
}, (m_152) => {
    let copyOfStruct_12;
    Machine__Fetch(m_152);
    Machine__Fetch(m_152);
    const patternInput_52 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.B), (copyOfStruct_12 = Machine__Flags(m_152), Flags__get_carry(copyOfStruct_12)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_152), R8.A, patternInput_52[0]);
    Machine__SetFlags_2901ED1A(m_152, patternInput_52[1]);
}, (m_153) => {
    let copyOfStruct_13;
    Machine__Fetch(m_153);
    Machine__Fetch(m_153);
    const patternInput_53 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.C), (copyOfStruct_13 = Machine__Flags(m_153), Flags__get_carry(copyOfStruct_13)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_153), R8.A, patternInput_53[0]);
    Machine__SetFlags_2901ED1A(m_153, patternInput_53[1]);
}, (m_154) => {
    let copyOfStruct_14;
    Machine__Fetch(m_154);
    Machine__Fetch(m_154);
    const patternInput_54 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.D), (copyOfStruct_14 = Machine__Flags(m_154), Flags__get_carry(copyOfStruct_14)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_154), R8.A, patternInput_54[0]);
    Machine__SetFlags_2901ED1A(m_154, patternInput_54[1]);
}, (m_155) => {
    let copyOfStruct_15;
    Machine__Fetch(m_155);
    Machine__Fetch(m_155);
    const patternInput_55 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.E), (copyOfStruct_15 = Machine__Flags(m_155), Flags__get_carry(copyOfStruct_15)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_155), R8.A, patternInput_55[0]);
    Machine__SetFlags_2901ED1A(m_155, patternInput_55[1]);
}, (m_156) => {
    let copyOfStruct_16;
    Machine__Fetch(m_156);
    Machine__Fetch(m_156);
    const patternInput_56 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.IYH), (copyOfStruct_16 = Machine__Flags(m_156), Flags__get_carry(copyOfStruct_16)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_156), R8.A, patternInput_56[0]);
    Machine__SetFlags_2901ED1A(m_156, patternInput_56[1]);
}, (m_157) => {
    let copyOfStruct_17;
    Machine__Fetch(m_157);
    Machine__Fetch(m_157);
    const patternInput_57 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.IYL), (copyOfStruct_17 = Machine__Flags(m_157), Flags__get_carry(copyOfStruct_17)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_157), R8.A, patternInput_57[0]);
    Machine__SetFlags_2901ED1A(m_157, patternInput_57[1]);
}, (m_158) => {
    let copyOfStruct_18;
    Machine__Fetch(m_158);
    Machine__Fetch(m_158);
    const d_40 = Machine__ReadImm(m_158) | 0;
    const d_41 = ((d_40 >= 128) ? (d_40 - 256) : d_40) | 0;
    Machine__PassTime_Z524259A4(m_158, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_158), (RegisterFile__Iy(Machine__get_Regs(m_158)) + d_41) & 65535);
    const patternInput_58 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_158), R8.A), Machine__Read_Z524259A4(m_158, RegisterFile__Wz(Machine__get_Regs(m_158))), (copyOfStruct_18 = Machine__Flags(m_158), Flags__get_carry(copyOfStruct_18)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_158), R8.A, patternInput_58[0]);
    Machine__SetFlags_2901ED1A(m_158, patternInput_58[1]);
}, (m_159) => {
    let copyOfStruct_19;
    Machine__Fetch(m_159);
    Machine__Fetch(m_159);
    const patternInput_59 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A), (copyOfStruct_19 = Machine__Flags(m_159), Flags__get_carry(copyOfStruct_19)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_159), R8.A, patternInput_59[0]);
    Machine__SetFlags_2901ED1A(m_159, patternInput_59[1]);
}, (m_160) => {
    Machine__Fetch(m_160);
    Machine__Fetch(m_160);
    const patternInput_60 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_160), R8.A, patternInput_60[0]);
    Machine__SetFlags_2901ED1A(m_160, patternInput_60[1]);
}, (m_161) => {
    Machine__Fetch(m_161);
    Machine__Fetch(m_161);
    const patternInput_61 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_161), R8.A, patternInput_61[0]);
    Machine__SetFlags_2901ED1A(m_161, patternInput_61[1]);
}, (m_162) => {
    Machine__Fetch(m_162);
    Machine__Fetch(m_162);
    const patternInput_62 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_162), R8.A, patternInput_62[0]);
    Machine__SetFlags_2901ED1A(m_162, patternInput_62[1]);
}, (m_163) => {
    Machine__Fetch(m_163);
    Machine__Fetch(m_163);
    const patternInput_63 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_163), R8.A, patternInput_63[0]);
    Machine__SetFlags_2901ED1A(m_163, patternInput_63[1]);
}, (m_164) => {
    Machine__Fetch(m_164);
    Machine__Fetch(m_164);
    const patternInput_64 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.IYH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_164), R8.A, patternInput_64[0]);
    Machine__SetFlags_2901ED1A(m_164, patternInput_64[1]);
}, (m_165) => {
    Machine__Fetch(m_165);
    Machine__Fetch(m_165);
    const patternInput_65 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.IYL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_165), R8.A, patternInput_65[0]);
    Machine__SetFlags_2901ED1A(m_165, patternInput_65[1]);
}, (m_166) => {
    Machine__Fetch(m_166);
    Machine__Fetch(m_166);
    const d_42 = Machine__ReadImm(m_166) | 0;
    const d_43 = ((d_42 >= 128) ? (d_42 - 256) : d_42) | 0;
    Machine__PassTime_Z524259A4(m_166, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_166), (RegisterFile__Iy(Machine__get_Regs(m_166)) + d_43) & 65535);
    const patternInput_66 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_166), R8.A), Machine__Read_Z524259A4(m_166, RegisterFile__Wz(Machine__get_Regs(m_166))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_166), R8.A, patternInput_66[0]);
    Machine__SetFlags_2901ED1A(m_166, patternInput_66[1]);
}, (m_167) => {
    Machine__Fetch(m_167);
    Machine__Fetch(m_167);
    const patternInput_67 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_167), R8.A, patternInput_67[0]);
    Machine__SetFlags_2901ED1A(m_167, patternInput_67[1]);
}, (m_168) => {
    Machine__Fetch(m_168);
    Machine__Fetch(m_168);
    const patternInput_68 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_168), R8.A, patternInput_68[0]);
    Machine__SetFlags_2901ED1A(m_168, patternInput_68[1]);
}, (m_169) => {
    Machine__Fetch(m_169);
    Machine__Fetch(m_169);
    const patternInput_69 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_169), R8.A, patternInput_69[0]);
    Machine__SetFlags_2901ED1A(m_169, patternInput_69[1]);
}, (m_170) => {
    Machine__Fetch(m_170);
    Machine__Fetch(m_170);
    const patternInput_70 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_170), R8.A, patternInput_70[0]);
    Machine__SetFlags_2901ED1A(m_170, patternInput_70[1]);
}, (m_171) => {
    Machine__Fetch(m_171);
    Machine__Fetch(m_171);
    const patternInput_71 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_171), R8.A, patternInput_71[0]);
    Machine__SetFlags_2901ED1A(m_171, patternInput_71[1]);
}, (m_172) => {
    Machine__Fetch(m_172);
    Machine__Fetch(m_172);
    const patternInput_72 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.IYH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_172), R8.A, patternInput_72[0]);
    Machine__SetFlags_2901ED1A(m_172, patternInput_72[1]);
}, (m_173) => {
    Machine__Fetch(m_173);
    Machine__Fetch(m_173);
    const patternInput_73 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.IYL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_173), R8.A, patternInput_73[0]);
    Machine__SetFlags_2901ED1A(m_173, patternInput_73[1]);
}, (m_174) => {
    Machine__Fetch(m_174);
    Machine__Fetch(m_174);
    const d_44 = Machine__ReadImm(m_174) | 0;
    const d_45 = ((d_44 >= 128) ? (d_44 - 256) : d_44) | 0;
    Machine__PassTime_Z524259A4(m_174, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_174), (RegisterFile__Iy(Machine__get_Regs(m_174)) + d_45) & 65535);
    const patternInput_74 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_174), R8.A), Machine__Read_Z524259A4(m_174, RegisterFile__Wz(Machine__get_Regs(m_174))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_174), R8.A, patternInput_74[0]);
    Machine__SetFlags_2901ED1A(m_174, patternInput_74[1]);
}, (m_175) => {
    Machine__Fetch(m_175);
    Machine__Fetch(m_175);
    const patternInput_75 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_175), R8.A, patternInput_75[0]);
    Machine__SetFlags_2901ED1A(m_175, patternInput_75[1]);
}, (m_176) => {
    Machine__Fetch(m_176);
    Machine__Fetch(m_176);
    const patternInput_76 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_176), R8.A, patternInput_76[0]);
    Machine__SetFlags_2901ED1A(m_176, patternInput_76[1]);
}, (m_177) => {
    Machine__Fetch(m_177);
    Machine__Fetch(m_177);
    const patternInput_77 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_177), R8.A, patternInput_77[0]);
    Machine__SetFlags_2901ED1A(m_177, patternInput_77[1]);
}, (m_178) => {
    Machine__Fetch(m_178);
    Machine__Fetch(m_178);
    const patternInput_78 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_178), R8.A, patternInput_78[0]);
    Machine__SetFlags_2901ED1A(m_178, patternInput_78[1]);
}, (m_179) => {
    Machine__Fetch(m_179);
    Machine__Fetch(m_179);
    const patternInput_79 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_179), R8.A, patternInput_79[0]);
    Machine__SetFlags_2901ED1A(m_179, patternInput_79[1]);
}, (m_180) => {
    Machine__Fetch(m_180);
    Machine__Fetch(m_180);
    const patternInput_80 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.IYH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_180), R8.A, patternInput_80[0]);
    Machine__SetFlags_2901ED1A(m_180, patternInput_80[1]);
}, (m_181) => {
    Machine__Fetch(m_181);
    Machine__Fetch(m_181);
    const patternInput_81 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.IYL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_181), R8.A, patternInput_81[0]);
    Machine__SetFlags_2901ED1A(m_181, patternInput_81[1]);
}, (m_182) => {
    Machine__Fetch(m_182);
    Machine__Fetch(m_182);
    const d_46 = Machine__ReadImm(m_182) | 0;
    const d_47 = ((d_46 >= 128) ? (d_46 - 256) : d_46) | 0;
    Machine__PassTime_Z524259A4(m_182, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_182), (RegisterFile__Iy(Machine__get_Regs(m_182)) + d_47) & 65535);
    const patternInput_82 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_182), R8.A), Machine__Read_Z524259A4(m_182, RegisterFile__Wz(Machine__get_Regs(m_182))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_182), R8.A, patternInput_82[0]);
    Machine__SetFlags_2901ED1A(m_182, patternInput_82[1]);
}, (m_183) => {
    Machine__Fetch(m_183);
    Machine__Fetch(m_183);
    const patternInput_83 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_183), R8.A, patternInput_83[0]);
    Machine__SetFlags_2901ED1A(m_183, patternInput_83[1]);
}, (m_184) => {
    Machine__Fetch(m_184);
    Machine__Fetch(m_184);
    const patternInput_84 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.B));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_184), R8.A, patternInput_84[0]);
    Machine__SetFlags_2901ED1A(m_184, patternInput_84[1]);
}, (m_185) => {
    Machine__Fetch(m_185);
    Machine__Fetch(m_185);
    const patternInput_85 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.C));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_185), R8.A, patternInput_85[0]);
    Machine__SetFlags_2901ED1A(m_185, patternInput_85[1]);
}, (m_186) => {
    Machine__Fetch(m_186);
    Machine__Fetch(m_186);
    const patternInput_86 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.D));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_186), R8.A, patternInput_86[0]);
    Machine__SetFlags_2901ED1A(m_186, patternInput_86[1]);
}, (m_187) => {
    Machine__Fetch(m_187);
    Machine__Fetch(m_187);
    const patternInput_87 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.E));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_187), R8.A, patternInput_87[0]);
    Machine__SetFlags_2901ED1A(m_187, patternInput_87[1]);
}, (m_188) => {
    Machine__Fetch(m_188);
    Machine__Fetch(m_188);
    const patternInput_88 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.IYH));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_188), R8.A, patternInput_88[0]);
    Machine__SetFlags_2901ED1A(m_188, patternInput_88[1]);
}, (m_189) => {
    Machine__Fetch(m_189);
    Machine__Fetch(m_189);
    const patternInput_89 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.IYL));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_189), R8.A, patternInput_89[0]);
    Machine__SetFlags_2901ED1A(m_189, patternInput_89[1]);
}, (m_190) => {
    Machine__Fetch(m_190);
    Machine__Fetch(m_190);
    const d_48 = Machine__ReadImm(m_190) | 0;
    const d_49 = ((d_48 >= 128) ? (d_48 - 256) : d_48) | 0;
    Machine__PassTime_Z524259A4(m_190, 5);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_190), (RegisterFile__Iy(Machine__get_Regs(m_190)) + d_49) & 65535);
    const patternInput_90 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_190), R8.A), Machine__Read_Z524259A4(m_190, RegisterFile__Wz(Machine__get_Regs(m_190))));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_190), R8.A, patternInput_90[0]);
    Machine__SetFlags_2901ED1A(m_190, patternInput_90[1]);
}, (m_191) => {
    Machine__Fetch(m_191);
    Machine__Fetch(m_191);
    const patternInput_91 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_191), R8.A, patternInput_91[0]);
    Machine__SetFlags_2901ED1A(m_191, patternInput_91[1]);
}, (m_192) => {
    let copyOfStruct_20;
    Machine__Fetch(m_192);
    Machine__PassTime_Z524259A4(m_192, 1);
    if (!((copyOfStruct_20 = Machine__Flags(m_192), Flags__get_zero(copyOfStruct_20)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_192), Machine__Pop16(m_192));
    }
}, (m_193) => {
    Machine__Fetch(m_193);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_193), R16.BC, Machine__Pop16(m_193));
}, (m_194) => {
    let copyOfStruct_21;
    Machine__Fetch(m_194);
    const jumpAddress = Machine__ReadImm16(m_194) | 0;
    if (!((copyOfStruct_21 = Machine__Flags(m_194), Flags__get_zero(copyOfStruct_21)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_194), jumpAddress);
    }
}, (m_195) => {
    Machine__Fetch(m_195);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_195), Machine__ReadImm16(m_195));
}, (m_196) => {
    let copyOfStruct_22;
    Machine__Fetch(m_196);
    const jumpAddress_1 = Machine__ReadImm16(m_196) | 0;
    if (!((copyOfStruct_22 = Machine__Flags(m_196), Flags__get_zero(copyOfStruct_22)))) {
        Machine__PassTime_Z524259A4(m_196, 1);
        Machine__Push16_Z524259A4(m_196, RegisterFile__Pc(Machine__get_Regs(m_196)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_196), jumpAddress_1);
    }
}, (m_197) => {
    Machine__Fetch(m_197);
    Machine__PassTime_Z524259A4(m_197, 1);
    Machine__Push16_Z524259A4(m_197, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_197), R16.BC));
}, (m_198) => {
    Machine__Fetch(m_198);
    const patternInput_92 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_198), R8.A), Machine__ReadImm(m_198), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_198), R8.A, patternInput_92[0]);
    Machine__SetFlags_2901ED1A(m_198, patternInput_92[1]);
}, (m_199) => {
    Machine__Fetch(m_199);
    Machine__PassTime_Z524259A4(m_199, 1);
    Machine__Push16_Z524259A4(m_199, RegisterFile__Pc(Machine__get_Regs(m_199)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_199), 0);
}, (m_200) => {
    let copyOfStruct_23;
    Machine__Fetch(m_200);
    Machine__PassTime_Z524259A4(m_200, 1);
    if ((copyOfStruct_23 = Machine__Flags(m_200), Flags__get_zero(copyOfStruct_23))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_200), Machine__Pop16(m_200));
    }
}, (m_201) => {
    Machine__Fetch(m_201);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_201), Machine__Pop16(m_201));
}, (m_202) => {
    let copyOfStruct_24;
    Machine__Fetch(m_202);
    const jumpAddress_2 = Machine__ReadImm16(m_202) | 0;
    if ((copyOfStruct_24 = Machine__Flags(m_202), Flags__get_zero(copyOfStruct_24))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_202), jumpAddress_2);
    }
}, (m_203) => {
    Machine__Fetch(m_203);
    Machine__Fetch(m_203);
}, (m_204) => {
    let copyOfStruct_25;
    Machine__Fetch(m_204);
    const jumpAddress_3 = Machine__ReadImm16(m_204) | 0;
    if ((copyOfStruct_25 = Machine__Flags(m_204), Flags__get_zero(copyOfStruct_25))) {
        Machine__PassTime_Z524259A4(m_204, 1);
        Machine__Push16_Z524259A4(m_204, RegisterFile__Pc(Machine__get_Regs(m_204)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_204), jumpAddress_3);
    }
}, (m_205) => {
    Machine__Fetch(m_205);
    const jumpAddress_4 = Machine__ReadImm16(m_205) | 0;
    Machine__PassTime_Z524259A4(m_205, 1);
    Machine__Push16_Z524259A4(m_205, RegisterFile__Pc(Machine__get_Regs(m_205)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_205), jumpAddress_4);
}, (m_206) => {
    let copyOfStruct_26;
    Machine__Fetch(m_206);
    const patternInput_93 = Alu_add8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_206), R8.A), Machine__ReadImm(m_206), (copyOfStruct_26 = Machine__Flags(m_206), Flags__get_carry(copyOfStruct_26)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_206), R8.A, patternInput_93[0]);
    Machine__SetFlags_2901ED1A(m_206, patternInput_93[1]);
}, (m_207) => {
    Machine__Fetch(m_207);
    Machine__PassTime_Z524259A4(m_207, 1);
    Machine__Push16_Z524259A4(m_207, RegisterFile__Pc(Machine__get_Regs(m_207)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_207), 8);
}, (m_208) => {
    let copyOfStruct_27;
    Machine__Fetch(m_208);
    Machine__PassTime_Z524259A4(m_208, 1);
    if (!((copyOfStruct_27 = Machine__Flags(m_208), Flags__get_carry(copyOfStruct_27)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_208), Machine__Pop16(m_208));
    }
}, (m_209) => {
    Machine__Fetch(m_209);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_209), R16.DE, Machine__Pop16(m_209));
}, (m_210) => {
    let copyOfStruct_28;
    Machine__Fetch(m_210);
    const jumpAddress_5 = Machine__ReadImm16(m_210) | 0;
    if (!((copyOfStruct_28 = Machine__Flags(m_210), Flags__get_carry(copyOfStruct_28)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_210), jumpAddress_5);
    }
}, (m_211) => {
    Machine__Fetch(m_211);
    const a = RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_211), R8.A) | 0;
    const port = ((Machine__ReadImm(m_211) | (a << 8)) & 65535) | 0;
    Machine__PassTime_Z524259A4(m_211, 4);
    Machine__Out_Z37302880(m_211, port, a);
}, (m_212) => {
    let copyOfStruct_29;
    Machine__Fetch(m_212);
    const jumpAddress_6 = Machine__ReadImm16(m_212) | 0;
    if (!((copyOfStruct_29 = Machine__Flags(m_212), Flags__get_carry(copyOfStruct_29)))) {
        Machine__PassTime_Z524259A4(m_212, 1);
        Machine__Push16_Z524259A4(m_212, RegisterFile__Pc(Machine__get_Regs(m_212)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_212), jumpAddress_6);
    }
}, (m_213) => {
    Machine__Fetch(m_213);
    Machine__PassTime_Z524259A4(m_213, 1);
    Machine__Push16_Z524259A4(m_213, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_213), R16.DE));
}, (m_214) => {
    Machine__Fetch(m_214);
    const patternInput_94 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_214), R8.A), Machine__ReadImm(m_214), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_214), R8.A, patternInput_94[0]);
    Machine__SetFlags_2901ED1A(m_214, patternInput_94[1]);
}, (m_215) => {
    Machine__Fetch(m_215);
    Machine__PassTime_Z524259A4(m_215, 1);
    Machine__Push16_Z524259A4(m_215, RegisterFile__Pc(Machine__get_Regs(m_215)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_215), 16);
}, (m_216) => {
    let copyOfStruct_30;
    Machine__Fetch(m_216);
    Machine__PassTime_Z524259A4(m_216, 1);
    if ((copyOfStruct_30 = Machine__Flags(m_216), Flags__get_carry(copyOfStruct_30))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_216), Machine__Pop16(m_216));
    }
}, (m_217) => {
    Machine__Fetch(m_217);
    RegisterFile__Exx(Machine__get_Regs(m_217));
}, (m_218) => {
    let copyOfStruct_31;
    Machine__Fetch(m_218);
    const jumpAddress_7 = Machine__ReadImm16(m_218) | 0;
    if ((copyOfStruct_31 = Machine__Flags(m_218), Flags__get_carry(copyOfStruct_31))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_218), jumpAddress_7);
    }
}, (m_219) => {
    Machine__Fetch(m_219);
    const port_1 = ((Machine__ReadImm(m_219) | (RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_219), R8.A) << 8)) & 65535) | 0;
    Machine__PassTime_Z524259A4(m_219, 4);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_219), R8.A, Machine__In_Z524259A4(m_219, port_1));
}, (m_220) => {
    let copyOfStruct_32;
    Machine__Fetch(m_220);
    const jumpAddress_8 = Machine__ReadImm16(m_220) | 0;
    if ((copyOfStruct_32 = Machine__Flags(m_220), Flags__get_carry(copyOfStruct_32))) {
        Machine__PassTime_Z524259A4(m_220, 1);
        Machine__Push16_Z524259A4(m_220, RegisterFile__Pc(Machine__get_Regs(m_220)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_220), jumpAddress_8);
    }
}, (m_221) => {
    Machine__Fetch(m_221);
    Machine__Fetch(m_221);
}, (m_222) => {
    let copyOfStruct_33;
    Machine__Fetch(m_222);
    const patternInput_95 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_222), R8.A), Machine__ReadImm(m_222), (copyOfStruct_33 = Machine__Flags(m_222), Flags__get_carry(copyOfStruct_33)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_222), R8.A, patternInput_95[0]);
    Machine__SetFlags_2901ED1A(m_222, patternInput_95[1]);
}, (m_223) => {
    Machine__Fetch(m_223);
    Machine__PassTime_Z524259A4(m_223, 1);
    Machine__Push16_Z524259A4(m_223, RegisterFile__Pc(Machine__get_Regs(m_223)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_223), 24);
}, (m_224) => {
    let copyOfStruct_34;
    Machine__Fetch(m_224);
    Machine__PassTime_Z524259A4(m_224, 1);
    if (!((copyOfStruct_34 = Machine__Flags(m_224), Flags__get_parity(copyOfStruct_34)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_224), Machine__Pop16(m_224));
    }
}, (m_225) => {
    Machine__Fetch(m_225);
    Machine__Fetch(m_225);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_225), R16.IY, Machine__Pop16(m_225));
}, (m_226) => {
    let copyOfStruct_35;
    Machine__Fetch(m_226);
    const jumpAddress_9 = Machine__ReadImm16(m_226) | 0;
    if (!((copyOfStruct_35 = Machine__Flags(m_226), Flags__get_parity(copyOfStruct_35)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_226), jumpAddress_9);
    }
}, (m_227) => {
    Machine__Fetch(m_227);
    Machine__Fetch(m_227);
    const sp = RegisterFile__Sp(Machine__get_Regs(m_227)) | 0;
    const spOldLow = Machine__Read_Z524259A4(m_227, sp) | 0;
    Machine__PassTime_Z524259A4(m_227, 1);
    const spOldHigh = Machine__Read_Z524259A4(m_227, (sp + 1) & 65535) | 0;
    Machine__Write_Z37302880(m_227, sp, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_227), R8.IYL));
    Machine__PassTime_Z524259A4(m_227, 2);
    Machine__Write_Z37302880(m_227, (sp + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_227), R8.IYH));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_227), R16.IY, (spOldLow | (spOldHigh << 8)) & 65535);
}, (m_228) => {
    let copyOfStruct_36;
    Machine__Fetch(m_228);
    const jumpAddress_10 = Machine__ReadImm16(m_228) | 0;
    if (!((copyOfStruct_36 = Machine__Flags(m_228), Flags__get_parity(copyOfStruct_36)))) {
        Machine__PassTime_Z524259A4(m_228, 1);
        Machine__Push16_Z524259A4(m_228, RegisterFile__Pc(Machine__get_Regs(m_228)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_228), jumpAddress_10);
    }
}, (m_229) => {
    Machine__Fetch(m_229);
    Machine__Fetch(m_229);
    Machine__PassTime_Z524259A4(m_229, 1);
    Machine__Push16_Z524259A4(m_229, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_229), R16.IY));
}, (m_230) => {
    Machine__Fetch(m_230);
    const patternInput_96 = Alu_and8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_230), R8.A), Machine__ReadImm(m_230));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_230), R8.A, patternInput_96[0]);
    Machine__SetFlags_2901ED1A(m_230, patternInput_96[1]);
}, (m_231) => {
    Machine__Fetch(m_231);
    Machine__PassTime_Z524259A4(m_231, 1);
    Machine__Push16_Z524259A4(m_231, RegisterFile__Pc(Machine__get_Regs(m_231)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_231), 32);
}, (m_232) => {
    let copyOfStruct_37;
    Machine__Fetch(m_232);
    Machine__PassTime_Z524259A4(m_232, 1);
    if ((copyOfStruct_37 = Machine__Flags(m_232), Flags__get_parity(copyOfStruct_37))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_232), Machine__Pop16(m_232));
    }
}, (m_233) => {
    Machine__Fetch(m_233);
    Machine__Fetch(m_233);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_233), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_233), R16.IY));
}, (m_234) => {
    let copyOfStruct_38;
    Machine__Fetch(m_234);
    const jumpAddress_11 = Machine__ReadImm16(m_234) | 0;
    if ((copyOfStruct_38 = Machine__Flags(m_234), Flags__get_parity(copyOfStruct_38))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_234), jumpAddress_11);
    }
}, (m_235) => {
    Machine__Fetch(m_235);
    RegisterFile__Ex_Z3F9DF200(Machine__get_Regs(m_235), R16.DE, R16.HL);
}, (m_236) => {
    let copyOfStruct_39;
    Machine__Fetch(m_236);
    const jumpAddress_12 = Machine__ReadImm16(m_236) | 0;
    if ((copyOfStruct_39 = Machine__Flags(m_236), Flags__get_parity(copyOfStruct_39))) {
        Machine__PassTime_Z524259A4(m_236, 1);
        Machine__Push16_Z524259A4(m_236, RegisterFile__Pc(Machine__get_Regs(m_236)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_236), jumpAddress_12);
    }
}, (m_237) => {
    Machine__Fetch(m_237);
    Machine__Fetch(m_237);
}, (m_238) => {
    Machine__Fetch(m_238);
    const patternInput_97 = Alu_xor8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_238), R8.A), Machine__ReadImm(m_238));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_238), R8.A, patternInput_97[0]);
    Machine__SetFlags_2901ED1A(m_238, patternInput_97[1]);
}, (m_239) => {
    Machine__Fetch(m_239);
    Machine__PassTime_Z524259A4(m_239, 1);
    Machine__Push16_Z524259A4(m_239, RegisterFile__Pc(Machine__get_Regs(m_239)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_239), 40);
}, (m_240) => {
    let copyOfStruct_40;
    Machine__Fetch(m_240);
    Machine__PassTime_Z524259A4(m_240, 1);
    if (!((copyOfStruct_40 = Machine__Flags(m_240), Flags__get_sign(copyOfStruct_40)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_240), Machine__Pop16(m_240));
    }
}, (m_241) => {
    Machine__Fetch(m_241);
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_241), R16.AF, Machine__Pop16(m_241));
}, (m_242) => {
    let copyOfStruct_41;
    Machine__Fetch(m_242);
    const jumpAddress_13 = Machine__ReadImm16(m_242) | 0;
    if (!((copyOfStruct_41 = Machine__Flags(m_242), Flags__get_sign(copyOfStruct_41)))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_242), jumpAddress_13);
    }
}, (m_243) => {
    Machine__Fetch(m_243);
    Machine__set_Iff1_Z1FBCCD16(m_243, false);
    Machine__set_Iff2_Z1FBCCD16(m_243, false);
}, (m_244) => {
    let copyOfStruct_42;
    Machine__Fetch(m_244);
    const jumpAddress_14 = Machine__ReadImm16(m_244) | 0;
    if (!((copyOfStruct_42 = Machine__Flags(m_244), Flags__get_sign(copyOfStruct_42)))) {
        Machine__PassTime_Z524259A4(m_244, 1);
        Machine__Push16_Z524259A4(m_244, RegisterFile__Pc(Machine__get_Regs(m_244)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_244), jumpAddress_14);
    }
}, (m_245) => {
    Machine__Fetch(m_245);
    Machine__PassTime_Z524259A4(m_245, 1);
    Machine__Push16_Z524259A4(m_245, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_245), R16.AF));
}, (m_246) => {
    Machine__Fetch(m_246);
    const patternInput_98 = Alu_or8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_246), R8.A), Machine__ReadImm(m_246));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_246), R8.A, patternInput_98[0]);
    Machine__SetFlags_2901ED1A(m_246, patternInput_98[1]);
}, (m_247) => {
    Machine__Fetch(m_247);
    Machine__PassTime_Z524259A4(m_247, 1);
    Machine__Push16_Z524259A4(m_247, RegisterFile__Pc(Machine__get_Regs(m_247)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_247), 48);
}, (m_248) => {
    let copyOfStruct_43;
    Machine__Fetch(m_248);
    Machine__PassTime_Z524259A4(m_248, 1);
    if ((copyOfStruct_43 = Machine__Flags(m_248), Flags__get_sign(copyOfStruct_43))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_248), Machine__Pop16(m_248));
    }
}, (m_249) => {
    Machine__Fetch(m_249);
    Machine__Fetch(m_249);
    RegisterFile__SetSp_Z524259A4(Machine__get_Regs(m_249), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_249), R16.IY));
    Machine__PassTime_Z524259A4(m_249, 2);
}, (m_250) => {
    let copyOfStruct_44;
    Machine__Fetch(m_250);
    const jumpAddress_15 = Machine__ReadImm16(m_250) | 0;
    if ((copyOfStruct_44 = Machine__Flags(m_250), Flags__get_sign(copyOfStruct_44))) {
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_250), jumpAddress_15);
    }
}, (m_251) => {
    Machine__Fetch(m_251);
    Machine__set_Iff1_Z1FBCCD16(m_251, true);
    Machine__set_Iff2_Z1FBCCD16(m_251, true);
}, (m_252) => {
    let copyOfStruct_45;
    Machine__Fetch(m_252);
    const jumpAddress_16 = Machine__ReadImm16(m_252) | 0;
    if ((copyOfStruct_45 = Machine__Flags(m_252), Flags__get_sign(copyOfStruct_45))) {
        Machine__PassTime_Z524259A4(m_252, 1);
        Machine__Push16_Z524259A4(m_252, RegisterFile__Pc(Machine__get_Regs(m_252)));
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_252), jumpAddress_16);
    }
}, (m_253) => {
    Machine__Fetch(m_253);
    Machine__Fetch(m_253);
}, (m_254) => {
    Machine__Fetch(m_254);
    const patternInput_99 = Alu_cmp8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_254), R8.A), Machine__ReadImm(m_254));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_254), R8.A, patternInput_99[0]);
    Machine__SetFlags_2901ED1A(m_254, patternInput_99[1]);
}, (m_255) => {
    Machine__Fetch(m_255);
    Machine__PassTime_Z524259A4(m_255, 1);
    Machine__Push16_Z524259A4(m_255, RegisterFile__Pc(Machine__get_Regs(m_255)));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_255), 56);
}];

export const ed = [(m) => {
    Machine__Fetch(m);
    Machine__Fetch(m);
}, (m_1) => {
    Machine__Fetch(m_1);
    Machine__Fetch(m_1);
}, (m_2) => {
    Machine__Fetch(m_2);
    Machine__Fetch(m_2);
}, (m_3) => {
    Machine__Fetch(m_3);
    Machine__Fetch(m_3);
}, (m_4) => {
    Machine__Fetch(m_4);
    Machine__Fetch(m_4);
}, (m_5) => {
    Machine__Fetch(m_5);
    Machine__Fetch(m_5);
}, (m_6) => {
    Machine__Fetch(m_6);
    Machine__Fetch(m_6);
}, (m_7) => {
    Machine__Fetch(m_7);
    Machine__Fetch(m_7);
}, (m_8) => {
    Machine__Fetch(m_8);
    Machine__Fetch(m_8);
}, (m_9) => {
    Machine__Fetch(m_9);
    Machine__Fetch(m_9);
}, (m_10) => {
    Machine__Fetch(m_10);
    Machine__Fetch(m_10);
}, (m_11) => {
    Machine__Fetch(m_11);
    Machine__Fetch(m_11);
}, (m_12) => {
    Machine__Fetch(m_12);
    Machine__Fetch(m_12);
}, (m_13) => {
    Machine__Fetch(m_13);
    Machine__Fetch(m_13);
}, (m_14) => {
    Machine__Fetch(m_14);
    Machine__Fetch(m_14);
}, (m_15) => {
    Machine__Fetch(m_15);
    Machine__Fetch(m_15);
}, (m_16) => {
    Machine__Fetch(m_16);
    Machine__Fetch(m_16);
}, (m_17) => {
    Machine__Fetch(m_17);
    Machine__Fetch(m_17);
}, (m_18) => {
    Machine__Fetch(m_18);
    Machine__Fetch(m_18);
}, (m_19) => {
    Machine__Fetch(m_19);
    Machine__Fetch(m_19);
}, (m_20) => {
    Machine__Fetch(m_20);
    Machine__Fetch(m_20);
}, (m_21) => {
    Machine__Fetch(m_21);
    Machine__Fetch(m_21);
}, (m_22) => {
    Machine__Fetch(m_22);
    Machine__Fetch(m_22);
}, (m_23) => {
    Machine__Fetch(m_23);
    Machine__Fetch(m_23);
}, (m_24) => {
    Machine__Fetch(m_24);
    Machine__Fetch(m_24);
}, (m_25) => {
    Machine__Fetch(m_25);
    Machine__Fetch(m_25);
}, (m_26) => {
    Machine__Fetch(m_26);
    Machine__Fetch(m_26);
}, (m_27) => {
    Machine__Fetch(m_27);
    Machine__Fetch(m_27);
}, (m_28) => {
    Machine__Fetch(m_28);
    Machine__Fetch(m_28);
}, (m_29) => {
    Machine__Fetch(m_29);
    Machine__Fetch(m_29);
}, (m_30) => {
    Machine__Fetch(m_30);
    Machine__Fetch(m_30);
}, (m_31) => {
    Machine__Fetch(m_31);
    Machine__Fetch(m_31);
}, (m_32) => {
    Machine__Fetch(m_32);
    Machine__Fetch(m_32);
}, (m_33) => {
    Machine__Fetch(m_33);
    Machine__Fetch(m_33);
}, (m_34) => {
    Machine__Fetch(m_34);
    Machine__Fetch(m_34);
}, (m_35) => {
    Machine__Fetch(m_35);
    Machine__Fetch(m_35);
}, (m_36) => {
    Machine__Fetch(m_36);
    Machine__Fetch(m_36);
}, (m_37) => {
    Machine__Fetch(m_37);
    Machine__Fetch(m_37);
}, (m_38) => {
    Machine__Fetch(m_38);
    Machine__Fetch(m_38);
}, (m_39) => {
    Machine__Fetch(m_39);
    Machine__Fetch(m_39);
}, (m_40) => {
    Machine__Fetch(m_40);
    Machine__Fetch(m_40);
}, (m_41) => {
    Machine__Fetch(m_41);
    Machine__Fetch(m_41);
}, (m_42) => {
    Machine__Fetch(m_42);
    Machine__Fetch(m_42);
}, (m_43) => {
    Machine__Fetch(m_43);
    Machine__Fetch(m_43);
}, (m_44) => {
    Machine__Fetch(m_44);
    Machine__Fetch(m_44);
}, (m_45) => {
    Machine__Fetch(m_45);
    Machine__Fetch(m_45);
}, (m_46) => {
    Machine__Fetch(m_46);
    Machine__Fetch(m_46);
}, (m_47) => {
    Machine__Fetch(m_47);
    Machine__Fetch(m_47);
}, (m_48) => {
    Machine__Fetch(m_48);
    Machine__Fetch(m_48);
}, (m_49) => {
    Machine__Fetch(m_49);
    Machine__Fetch(m_49);
}, (m_50) => {
    Machine__Fetch(m_50);
    Machine__Fetch(m_50);
}, (m_51) => {
    Machine__Fetch(m_51);
    Machine__Fetch(m_51);
}, (m_52) => {
    Machine__Fetch(m_52);
    Machine__Fetch(m_52);
}, (m_53) => {
    Machine__Fetch(m_53);
    Machine__Fetch(m_53);
}, (m_54) => {
    Machine__Fetch(m_54);
    Machine__Fetch(m_54);
}, (m_55) => {
    Machine__Fetch(m_55);
    Machine__Fetch(m_55);
}, (m_56) => {
    Machine__Fetch(m_56);
    Machine__Fetch(m_56);
}, (m_57) => {
    Machine__Fetch(m_57);
    Machine__Fetch(m_57);
}, (m_58) => {
    Machine__Fetch(m_58);
    Machine__Fetch(m_58);
}, (m_59) => {
    Machine__Fetch(m_59);
    Machine__Fetch(m_59);
}, (m_60) => {
    Machine__Fetch(m_60);
    Machine__Fetch(m_60);
}, (m_61) => {
    Machine__Fetch(m_61);
    Machine__Fetch(m_61);
}, (m_62) => {
    Machine__Fetch(m_62);
    Machine__Fetch(m_62);
}, (m_63) => {
    Machine__Fetch(m_63);
    Machine__Fetch(m_63);
}, (m_64) => {
    Machine__Fetch(m_64);
    Machine__Fetch(m_64);
    Machine__PassTime_Z524259A4(m_64, 4);
    const result = Machine__In_Z524259A4(m_64, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_64), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_64, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_64), Flags_Carry()), Alu_parityFlagsFor(result)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_64), R8.B, result);
}, (m_65) => {
    Machine__Fetch(m_65);
    Machine__Fetch(m_65);
    Machine__PassTime_Z524259A4(m_65, 4);
    Machine__Out_Z37302880(m_65, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_65), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_65), R8.B));
}, (m_66) => {
    let copyOfStruct;
    Machine__Fetch(m_66);
    Machine__Fetch(m_66);
    const rhs = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_66), R16.BC) | 0;
    const patternInput = Alu_sbc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_66), R16.HL), rhs, (copyOfStruct = Machine__Flags(m_66), Flags__get_carry(copyOfStruct)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_66), R16.HL, patternInput[0]);
    Machine__SetFlags_2901ED1A(m_66, patternInput[1]);
    Machine__PassTime_Z524259A4(m_66, 7);
}, (m_67) => {
    Machine__Fetch(m_67);
    Machine__Fetch(m_67);
    const addr = Machine__ReadImm16(m_67) | 0;
    Machine__Write_Z37302880(m_67, addr, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.C));
    Machine__Write_Z37302880(m_67, (addr + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.B));
}, (m_68) => {
    Machine__Fetch(m_68);
    Machine__Fetch(m_68);
    const patternInput_1 = Alu_sub8(0, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_68), R8.A), false);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_68), R8.A, patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_68, patternInput_1[1]);
}, (m_69) => {
    Machine__Fetch(m_69);
    Machine__Fetch(m_69);
    Machine__set_Iff1_Z1FBCCD16(m_69, Machine__get_Iff2(m_69));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_69), Machine__Pop16(m_69));
}, (m_70) => {
    Machine__Fetch(m_70);
    Machine__Fetch(m_70);
    Machine__set_IrqMode_Z524259A4(m_70, 0);
}, (m_71) => {
    Machine__Fetch(m_71);
    Machine__Fetch(m_71);
    Machine__PassTime_Z524259A4(m_71, 1);
    RegisterFile__SetI_Z524259A4(Machine__get_Regs(m_71), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_71), R8.A));
}, (m_72) => {
    Machine__Fetch(m_72);
    Machine__Fetch(m_72);
    Machine__PassTime_Z524259A4(m_72, 4);
    const result_3 = Machine__In_Z524259A4(m_72, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_72), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_72, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_72), Flags_Carry()), Alu_parityFlagsFor(result_3)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_72), R8.C, result_3);
}, (m_73) => {
    Machine__Fetch(m_73);
    Machine__Fetch(m_73);
    Machine__PassTime_Z524259A4(m_73, 4);
    Machine__Out_Z37302880(m_73, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_73), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_73), R8.C));
}, (m_74) => {
    let copyOfStruct_1;
    Machine__Fetch(m_74);
    Machine__Fetch(m_74);
    const patternInput_2 = Alu_adc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_74), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_74), R16.BC), (copyOfStruct_1 = Machine__Flags(m_74), Flags__get_carry(copyOfStruct_1)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_74), R16.HL, patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_74, patternInput_2[1]);
    Machine__PassTime_Z524259A4(m_74, 7);
}, (m_75) => {
    Machine__Fetch(m_75);
    Machine__Fetch(m_75);
    const addr_1 = Machine__ReadImm16(m_75) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_75), R8.C, Machine__Read_Z524259A4(m_75, addr_1));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_75), R8.B, Machine__Read_Z524259A4(m_75, (addr_1 + 1) & 65535));
}, (m_76) => {
    Machine__Fetch(m_76);
    Machine__Fetch(m_76);
}, (m_77) => {
    Machine__Fetch(m_77);
    Machine__Fetch(m_77);
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_77), Machine__Pop16(m_77));
}, (m_78) => {
    Machine__Fetch(m_78);
    Machine__Fetch(m_78);
    Machine__set_IrqMode_Z524259A4(m_78, 0);
}, (m_79) => {
    Machine__Fetch(m_79);
    Machine__Fetch(m_79);
    Machine__PassTime_Z524259A4(m_79, 1);
    RegisterFile__SetR_Z524259A4(Machine__get_Regs(m_79), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_79), R8.A));
}, (m_80) => {
    Machine__Fetch(m_80);
    Machine__Fetch(m_80);
    Machine__PassTime_Z524259A4(m_80, 4);
    const result_4 = Machine__In_Z524259A4(m_80, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_80), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_80, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_80), Flags_Carry()), Alu_parityFlagsFor(result_4)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_80), R8.D, result_4);
}, (m_81) => {
    Machine__Fetch(m_81);
    Machine__Fetch(m_81);
    Machine__PassTime_Z524259A4(m_81, 4);
    Machine__Out_Z37302880(m_81, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_81), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_81), R8.D));
}, (m_82) => {
    let copyOfStruct_2;
    Machine__Fetch(m_82);
    Machine__Fetch(m_82);
    const rhs_1 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_82), R16.DE) | 0;
    const patternInput_3 = Alu_sbc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_82), R16.HL), rhs_1, (copyOfStruct_2 = Machine__Flags(m_82), Flags__get_carry(copyOfStruct_2)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_82), R16.HL, patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_82, patternInput_3[1]);
    Machine__PassTime_Z524259A4(m_82, 7);
}, (m_83) => {
    Machine__Fetch(m_83);
    Machine__Fetch(m_83);
    const addr_2 = Machine__ReadImm16(m_83) | 0;
    Machine__Write_Z37302880(m_83, addr_2, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.E));
    Machine__Write_Z37302880(m_83, (addr_2 + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.D));
}, (m_84) => {
    Machine__Fetch(m_84);
    Machine__Fetch(m_84);
}, (m_85) => {
    Machine__Fetch(m_85);
    Machine__Fetch(m_85);
    Machine__set_Iff1_Z1FBCCD16(m_85, Machine__get_Iff2(m_85));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_85), Machine__Pop16(m_85));
}, (m_86) => {
    Machine__Fetch(m_86);
    Machine__Fetch(m_86);
    Machine__set_IrqMode_Z524259A4(m_86, 1);
}, (m_87) => {
    Machine__Fetch(m_87);
    Machine__Fetch(m_87);
    const result_6 = RegisterFile__I(Machine__get_Regs(m_87)) | 0;
    Machine__PassTime_Z524259A4(m_87, 1);
    Machine__SetFlags_2901ED1A(m_87, Alu_iff2FlagsFor(result_6, Machine__Flags(m_87), Machine__get_Iff2(m_87)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_87), R8.A, result_6);
}, (m_88) => {
    Machine__Fetch(m_88);
    Machine__Fetch(m_88);
    Machine__PassTime_Z524259A4(m_88, 4);
    const result_7 = Machine__In_Z524259A4(m_88, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_88), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_88, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_88), Flags_Carry()), Alu_parityFlagsFor(result_7)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_88), R8.E, result_7);
}, (m_89) => {
    Machine__Fetch(m_89);
    Machine__Fetch(m_89);
    Machine__PassTime_Z524259A4(m_89, 4);
    Machine__Out_Z37302880(m_89, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_89), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_89), R8.E));
}, (m_90) => {
    let copyOfStruct_3;
    Machine__Fetch(m_90);
    Machine__Fetch(m_90);
    const rhs_2 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_90), R16.DE) | 0;
    const patternInput_4 = Alu_adc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_90), R16.HL), rhs_2, (copyOfStruct_3 = Machine__Flags(m_90), Flags__get_carry(copyOfStruct_3)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_90), R16.HL, patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_90, patternInput_4[1]);
    Machine__PassTime_Z524259A4(m_90, 7);
}, (m_91) => {
    Machine__Fetch(m_91);
    Machine__Fetch(m_91);
    const addr_3 = Machine__ReadImm16(m_91) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_91), R8.E, Machine__Read_Z524259A4(m_91, addr_3));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_91), R8.D, Machine__Read_Z524259A4(m_91, (addr_3 + 1) & 65535));
}, (m_92) => {
    Machine__Fetch(m_92);
    Machine__Fetch(m_92);
}, (m_93) => {
    Machine__Fetch(m_93);
    Machine__Fetch(m_93);
    Machine__set_Iff1_Z1FBCCD16(m_93, Machine__get_Iff2(m_93));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_93), Machine__Pop16(m_93));
}, (m_94) => {
    Machine__Fetch(m_94);
    Machine__Fetch(m_94);
    Machine__set_IrqMode_Z524259A4(m_94, 2);
}, (m_95) => {
    Machine__Fetch(m_95);
    Machine__Fetch(m_95);
    const result_9 = RegisterFile__R(Machine__get_Regs(m_95)) | 0;
    Machine__PassTime_Z524259A4(m_95, 1);
    Machine__SetFlags_2901ED1A(m_95, Alu_iff2FlagsFor(result_9, Machine__Flags(m_95), Machine__get_Iff2(m_95)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_95), R8.A, result_9);
}, (m_96) => {
    Machine__Fetch(m_96);
    Machine__Fetch(m_96);
    Machine__PassTime_Z524259A4(m_96, 4);
    const result_10 = Machine__In_Z524259A4(m_96, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_96), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_96, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_96), Flags_Carry()), Alu_parityFlagsFor(result_10)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_96), R8.H, result_10);
}, (m_97) => {
    Machine__Fetch(m_97);
    Machine__Fetch(m_97);
    Machine__PassTime_Z524259A4(m_97, 4);
    Machine__Out_Z37302880(m_97, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_97), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_97), R8.H));
}, (m_98) => {
    let copyOfStruct_4;
    Machine__Fetch(m_98);
    Machine__Fetch(m_98);
    const patternInput_5 = Alu_sbc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_98), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_98), R16.HL), (copyOfStruct_4 = Machine__Flags(m_98), Flags__get_carry(copyOfStruct_4)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_98), R16.HL, patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_98, patternInput_5[1]);
    Machine__PassTime_Z524259A4(m_98, 7);
}, (m_99) => {
    Machine__Fetch(m_99);
    Machine__Fetch(m_99);
    const addr_4 = Machine__ReadImm16(m_99) | 0;
    Machine__Write_Z37302880(m_99, addr_4, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.L));
    Machine__Write_Z37302880(m_99, (addr_4 + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.H));
}, (m_100) => {
    Machine__Fetch(m_100);
    Machine__Fetch(m_100);
}, (m_101) => {
    Machine__Fetch(m_101);
    Machine__Fetch(m_101);
    Machine__set_Iff1_Z1FBCCD16(m_101, Machine__get_Iff2(m_101));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_101), Machine__Pop16(m_101));
}, (m_102) => {
    Machine__Fetch(m_102);
    Machine__Fetch(m_102);
    Machine__set_IrqMode_Z524259A4(m_102, 0);
}, (m_103) => {
    Machine__Fetch(m_103);
    Machine__Fetch(m_103);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_103), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_103), R16.HL));
    const indHl = Machine__Read_Z524259A4(m_103, RegisterFile__Wz(Machine__get_Regs(m_103))) | 0;
    const prevA = RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_103), R8.A) | 0;
    const newA = ((prevA & 240) | (indHl & 15)) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_103), R8.A, newA);
    Machine__PassTime_Z524259A4(m_103, 4);
    Machine__Write_Z37302880(m_103, RegisterFile__Wz(Machine__get_Regs(m_103)), ((indHl >> 4) | ((prevA & 15) << 4)) & 255);
    Machine__SetFlags_2901ED1A(m_103, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_103), Flags_Carry()), Alu_parityFlagsFor(newA)));
}, (m_104) => {
    Machine__Fetch(m_104);
    Machine__Fetch(m_104);
    Machine__PassTime_Z524259A4(m_104, 4);
    const result_11 = Machine__In_Z524259A4(m_104, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_104), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_104, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_104), Flags_Carry()), Alu_parityFlagsFor(result_11)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_104), R8.L, result_11);
}, (m_105) => {
    Machine__Fetch(m_105);
    Machine__Fetch(m_105);
    Machine__PassTime_Z524259A4(m_105, 4);
    Machine__Out_Z37302880(m_105, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_105), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_105), R8.L));
}, (m_106) => {
    let copyOfStruct_5;
    Machine__Fetch(m_106);
    Machine__Fetch(m_106);
    const rhs_3 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_106), R16.HL) | 0;
    const patternInput_6 = Alu_adc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_106), R16.HL), rhs_3, (copyOfStruct_5 = Machine__Flags(m_106), Flags__get_carry(copyOfStruct_5)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_106), R16.HL, patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_106, patternInput_6[1]);
    Machine__PassTime_Z524259A4(m_106, 7);
}, (m_107) => {
    Machine__Fetch(m_107);
    Machine__Fetch(m_107);
    const addr_5 = Machine__ReadImm16(m_107) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_107), R8.L, Machine__Read_Z524259A4(m_107, addr_5));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_107), R8.H, Machine__Read_Z524259A4(m_107, (addr_5 + 1) & 65535));
}, (m_108) => {
    Machine__Fetch(m_108);
    Machine__Fetch(m_108);
}, (m_109) => {
    Machine__Fetch(m_109);
    Machine__Fetch(m_109);
    Machine__set_Iff1_Z1FBCCD16(m_109, Machine__get_Iff2(m_109));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_109), Machine__Pop16(m_109));
}, (m_110) => {
    Machine__Fetch(m_110);
    Machine__Fetch(m_110);
    Machine__set_IrqMode_Z524259A4(m_110, 0);
}, (m_111) => {
    Machine__Fetch(m_111);
    Machine__Fetch(m_111);
    const address = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_111), R16.HL) | 0;
    const indHl_1 = Machine__Read_Z524259A4(m_111, address) | 0;
    const prevA_1 = RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_111), R8.A) | 0;
    const newA_1 = ((prevA_1 & 240) | ((indHl_1 >> 4) & 15)) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_111), R8.A, newA_1);
    Machine__PassTime_Z524259A4(m_111, 4);
    Machine__Write_Z37302880(m_111, address, ((indHl_1 << 4) | (prevA_1 & 15)) & 255);
    Machine__SetFlags_2901ED1A(m_111, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_111), Flags_Carry()), Alu_parityFlagsFor(newA_1)));
}, (m_112) => {
    Machine__Fetch(m_112);
    Machine__Fetch(m_112);
    Machine__PassTime_Z524259A4(m_112, 4);
    const result_13 = Machine__In_Z524259A4(m_112, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_112), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_112, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_112), Flags_Carry()), Alu_parityFlagsFor(result_13)));
}, (m_113) => {
    Machine__Fetch(m_113);
    Machine__Fetch(m_113);
    Machine__PassTime_Z524259A4(m_113, 4);
    Machine__Out_Z37302880(m_113, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_113), R16.BC), 0);
}, (m_114) => {
    let copyOfStruct_6;
    Machine__Fetch(m_114);
    Machine__Fetch(m_114);
    const rhs_4 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_114), R16.SP) | 0;
    const patternInput_7 = Alu_sbc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_114), R16.HL), rhs_4, (copyOfStruct_6 = Machine__Flags(m_114), Flags__get_carry(copyOfStruct_6)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_114), R16.HL, patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_114, patternInput_7[1]);
    Machine__PassTime_Z524259A4(m_114, 7);
}, (m_115) => {
    Machine__Fetch(m_115);
    Machine__Fetch(m_115);
    const addr_6 = Machine__ReadImm16(m_115) | 0;
    Machine__Write_Z37302880(m_115, addr_6, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.SPL));
    Machine__Write_Z37302880(m_115, (addr_6 + 1) & 65535, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.SPH));
}, (m_116) => {
    Machine__Fetch(m_116);
    Machine__Fetch(m_116);
}, (m_117) => {
    Machine__Fetch(m_117);
    Machine__Fetch(m_117);
    Machine__set_Iff1_Z1FBCCD16(m_117, Machine__get_Iff2(m_117));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_117), Machine__Pop16(m_117));
}, (m_118) => {
    Machine__Fetch(m_118);
    Machine__Fetch(m_118);
    Machine__set_IrqMode_Z524259A4(m_118, 1);
}, (m_119) => {
    Machine__Fetch(m_119);
    Machine__Fetch(m_119);
}, (m_120) => {
    Machine__Fetch(m_120);
    Machine__Fetch(m_120);
    Machine__PassTime_Z524259A4(m_120, 4);
    const result_15 = Machine__In_Z524259A4(m_120, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_120), R16.BC)) | 0;
    Machine__SetFlags_2901ED1A(m_120, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_120), Flags_Carry()), Alu_parityFlagsFor(result_15)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_120), R8.A, result_15);
}, (m_121) => {
    Machine__Fetch(m_121);
    Machine__Fetch(m_121);
    Machine__PassTime_Z524259A4(m_121, 4);
    Machine__Out_Z37302880(m_121, RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_121), R16.BC), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_121), R8.A));
}, (m_122) => {
    let copyOfStruct_7;
    Machine__Fetch(m_122);
    Machine__Fetch(m_122);
    const patternInput_8 = Alu_adc16(RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_122), R16.HL), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_122), R16.SP), (copyOfStruct_7 = Machine__Flags(m_122), Flags__get_carry(copyOfStruct_7)));
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_122), R16.HL, patternInput_8[0]);
    Machine__SetFlags_2901ED1A(m_122, patternInput_8[1]);
    Machine__PassTime_Z524259A4(m_122, 7);
}, (m_123) => {
    Machine__Fetch(m_123);
    Machine__Fetch(m_123);
    const addr_7 = Machine__ReadImm16(m_123) | 0;
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_123), R8.SPL, Machine__Read_Z524259A4(m_123, addr_7));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_123), R8.SPH, Machine__Read_Z524259A4(m_123, (addr_7 + 1) & 65535));
}, (m_124) => {
    Machine__Fetch(m_124);
    Machine__Fetch(m_124);
}, (m_125) => {
    Machine__Fetch(m_125);
    Machine__Fetch(m_125);
    Machine__set_Iff1_Z1FBCCD16(m_125, Machine__get_Iff2(m_125));
    RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_125), Machine__Pop16(m_125));
}, (m_126) => {
    Machine__Fetch(m_126);
    Machine__Fetch(m_126);
    Machine__set_IrqMode_Z524259A4(m_126, 2);
}, (m_127) => {
    Machine__Fetch(m_127);
    Machine__Fetch(m_127);
}, (m_128) => {
    Machine__Fetch(m_128);
    Machine__Fetch(m_128);
}, (m_129) => {
    Machine__Fetch(m_129);
    Machine__Fetch(m_129);
}, (m_130) => {
    Machine__Fetch(m_130);
    Machine__Fetch(m_130);
}, (m_131) => {
    Machine__Fetch(m_131);
    Machine__Fetch(m_131);
}, (m_132) => {
    Machine__Fetch(m_132);
    Machine__Fetch(m_132);
}, (m_133) => {
    Machine__Fetch(m_133);
    Machine__Fetch(m_133);
}, (m_134) => {
    Machine__Fetch(m_134);
    Machine__Fetch(m_134);
}, (m_135) => {
    Machine__Fetch(m_135);
    Machine__Fetch(m_135);
}, (m_136) => {
    Machine__Fetch(m_136);
    Machine__Fetch(m_136);
}, (m_137) => {
    Machine__Fetch(m_137);
    Machine__Fetch(m_137);
}, (m_138) => {
    Machine__Fetch(m_138);
    Machine__Fetch(m_138);
}, (m_139) => {
    Machine__Fetch(m_139);
    Machine__Fetch(m_139);
}, (m_140) => {
    Machine__Fetch(m_140);
    Machine__Fetch(m_140);
}, (m_141) => {
    Machine__Fetch(m_141);
    Machine__Fetch(m_141);
}, (m_142) => {
    Machine__Fetch(m_142);
    Machine__Fetch(m_142);
}, (m_143) => {
    Machine__Fetch(m_143);
    Machine__Fetch(m_143);
}, (m_144) => {
    Machine__Fetch(m_144);
    Machine__Fetch(m_144);
}, (m_145) => {
    Machine__Fetch(m_145);
    Machine__Fetch(m_145);
}, (m_146) => {
    Machine__Fetch(m_146);
    Machine__Fetch(m_146);
}, (m_147) => {
    Machine__Fetch(m_147);
    Machine__Fetch(m_147);
}, (m_148) => {
    Machine__Fetch(m_148);
    Machine__Fetch(m_148);
}, (m_149) => {
    Machine__Fetch(m_149);
    Machine__Fetch(m_149);
}, (m_150) => {
    Machine__Fetch(m_150);
    Machine__Fetch(m_150);
}, (m_151) => {
    Machine__Fetch(m_151);
    Machine__Fetch(m_151);
}, (m_152) => {
    Machine__Fetch(m_152);
    Machine__Fetch(m_152);
}, (m_153) => {
    Machine__Fetch(m_153);
    Machine__Fetch(m_153);
}, (m_154) => {
    Machine__Fetch(m_154);
    Machine__Fetch(m_154);
}, (m_155) => {
    Machine__Fetch(m_155);
    Machine__Fetch(m_155);
}, (m_156) => {
    Machine__Fetch(m_156);
    Machine__Fetch(m_156);
}, (m_157) => {
    Machine__Fetch(m_157);
    Machine__Fetch(m_157);
}, (m_158) => {
    Machine__Fetch(m_158);
    Machine__Fetch(m_158);
}, (m_159) => {
    Machine__Fetch(m_159);
    Machine__Fetch(m_159);
}, (m_160) => {
    Machine__Fetch(m_160);
    Machine__Fetch(m_160);
    const hl = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_160), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_160), R16.HL, (hl + 1) & 65535);
    const byte = Machine__Read_Z524259A4(m_160, hl) | 0;
    const de = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_160), R16.DE) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_160), R16.DE, (de + 1) & 65535);
    Machine__Write_Z37302880(m_160, de, byte);
    Machine__PassTime_Z524259A4(m_160, 2);
    const flagBits = (byte + RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.A)) | 0;
    const newBc = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_160), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_160), R16.BC, newBc);
    const preservedFlags = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_160), Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Carry()));
    const flagsFromBits = Flags_op_BitwiseOr_603E7D40(((flagBits & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc = (newBc !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_160, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags, flagsFromBits), flagsFromBc));
}, (m_161) => {
    Machine__Fetch(m_161);
    Machine__Fetch(m_161);
    const hl_1 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_161), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_161), R16.HL, (hl_1 + 1) & 65535);
    const byte_1 = Machine__Read_Z524259A4(m_161, hl_1) | 0;
    const patternInput_9 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.A), byte_1, false);
    const subtractFlags = patternInput_9[1];
    const result_16 = patternInput_9[0] | 0;
    Machine__PassTime_Z524259A4(m_161, 5);
    const flagBits_1 = (Flags__get_half_carry(subtractFlags) ? (result_16 - 1) : result_16) | 0;
    const newBc_1 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_161), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_161), R16.BC, newBc_1);
    const fromSubtractMask = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_HalfCarry(), Flags_Zero()), Flags_Sign()), Flags_Subtract());
    const preservedFlags_1 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_161), Flags_op_LogicalNot_2901ED1A(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()), fromSubtractMask), Flags_Overflow())));
    const flagsFromBits_1 = Flags_op_BitwiseOr_603E7D40(((flagBits_1 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_1 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_1 = (newBc_1 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_161, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_1, flagsFromBits_1), flagsFromBc_1), Flags_op_BitwiseAnd_603E7D40(fromSubtractMask, subtractFlags)));
}, (m_162) => {
    Machine__Fetch(m_162);
    Machine__Fetch(m_162);
}, (m_163) => {
    Machine__Fetch(m_163);
    Machine__Fetch(m_163);
}, (m_164) => {
    Machine__Fetch(m_164);
    Machine__Fetch(m_164);
}, (m_165) => {
    Machine__Fetch(m_165);
    Machine__Fetch(m_165);
}, (m_166) => {
    Machine__Fetch(m_166);
    Machine__Fetch(m_166);
}, (m_167) => {
    Machine__Fetch(m_167);
    Machine__Fetch(m_167);
}, (m_168) => {
    Machine__Fetch(m_168);
    Machine__Fetch(m_168);
    const hl_2 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_168), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_168), R16.HL, (hl_2 + 65535) & 65535);
    const byte_2 = Machine__Read_Z524259A4(m_168, hl_2) | 0;
    const de_1 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_168), R16.DE) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_168), R16.DE, (de_1 + 65535) & 65535);
    Machine__Write_Z37302880(m_168, de_1, byte_2);
    Machine__PassTime_Z524259A4(m_168, 2);
    const flagBits_2 = (byte_2 + RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.A)) | 0;
    const newBc_2 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_168), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_168), R16.BC, newBc_2);
    const preservedFlags_2 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_168), Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Carry()));
    const flagsFromBits_2 = Flags_op_BitwiseOr_603E7D40(((flagBits_2 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_2 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_2 = (newBc_2 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_168, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_2, flagsFromBits_2), flagsFromBc_2));
}, (m_169) => {
    Machine__Fetch(m_169);
    Machine__Fetch(m_169);
    const hl_3 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_169), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_169), R16.HL, (hl_3 + 65535) & 65535);
    const byte_3 = Machine__Read_Z524259A4(m_169, hl_3) | 0;
    const patternInput_10 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.A), byte_3, false);
    const subtractFlags_1 = patternInput_10[1];
    const result_17 = patternInput_10[0] | 0;
    Machine__PassTime_Z524259A4(m_169, 5);
    const flagBits_3 = (Flags__get_half_carry(subtractFlags_1) ? (result_17 - 1) : result_17) | 0;
    const newBc_3 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_169), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_169), R16.BC, newBc_3);
    const fromSubtractMask_1 = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_HalfCarry(), Flags_Zero()), Flags_Sign()), Flags_Subtract());
    const preservedFlags_3 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_169), Flags_op_LogicalNot_2901ED1A(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()), fromSubtractMask_1), Flags_Overflow())));
    const flagsFromBits_3 = Flags_op_BitwiseOr_603E7D40(((flagBits_3 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_3 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_3 = (newBc_3 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_169, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_3, flagsFromBits_3), flagsFromBc_3), Flags_op_BitwiseAnd_603E7D40(fromSubtractMask_1, subtractFlags_1)));
}, (m_170) => {
    Machine__Fetch(m_170);
    Machine__Fetch(m_170);
}, (m_171) => {
    Machine__Fetch(m_171);
    Machine__Fetch(m_171);
}, (m_172) => {
    Machine__Fetch(m_172);
    Machine__Fetch(m_172);
}, (m_173) => {
    Machine__Fetch(m_173);
    Machine__Fetch(m_173);
}, (m_174) => {
    Machine__Fetch(m_174);
    Machine__Fetch(m_174);
}, (m_175) => {
    Machine__Fetch(m_175);
    Machine__Fetch(m_175);
}, (m_176) => {
    Machine__Fetch(m_176);
    Machine__Fetch(m_176);
    const hl_4 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_176), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_176), R16.HL, (hl_4 + 1) & 65535);
    const byte_4 = Machine__Read_Z524259A4(m_176, hl_4) | 0;
    const de_2 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_176), R16.DE) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_176), R16.DE, (de_2 + 1) & 65535);
    Machine__Write_Z37302880(m_176, de_2, byte_4);
    Machine__PassTime_Z524259A4(m_176, 2);
    const flagBits_4 = (byte_4 + RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.A)) | 0;
    const newBc_4 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_176), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_176), R16.BC, newBc_4);
    const preservedFlags_4 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_176), Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Carry()));
    const flagsFromBits_4 = Flags_op_BitwiseOr_603E7D40(((flagBits_4 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_4 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_4 = (newBc_4 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_176, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_4, flagsFromBits_4), flagsFromBc_4));
    if (newBc_4 !== 0) {
        RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_176), (RegisterFile__Pc(Machine__get_Regs(m_176)) - 1) & 65535);
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_176), (RegisterFile__Pc(Machine__get_Regs(m_176)) - 2) & 65535);
        Machine__PassTime_Z524259A4(m_176, 5);
    }
}, (m_177) => {
    Machine__Fetch(m_177);
    Machine__Fetch(m_177);
    const hl_5 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_177), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_177), R16.HL, (hl_5 + 1) & 65535);
    const byte_5 = Machine__Read_Z524259A4(m_177, hl_5) | 0;
    const patternInput_11 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.A), byte_5, false);
    const subtractFlags_2 = patternInput_11[1];
    const result_18 = patternInput_11[0] | 0;
    Machine__PassTime_Z524259A4(m_177, 5);
    const flagBits_5 = (Flags__get_half_carry(subtractFlags_2) ? (result_18 - 1) : result_18) | 0;
    const newBc_5 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_177), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_177), R16.BC, newBc_5);
    const fromSubtractMask_2 = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_HalfCarry(), Flags_Zero()), Flags_Sign()), Flags_Subtract());
    const preservedFlags_5 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_177), Flags_op_LogicalNot_2901ED1A(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()), fromSubtractMask_2), Flags_Overflow())));
    const flagsFromBits_5 = Flags_op_BitwiseOr_603E7D40(((flagBits_5 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_5 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_5 = (newBc_5 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_177, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_5, flagsFromBits_5), flagsFromBc_5), Flags_op_BitwiseAnd_603E7D40(fromSubtractMask_2, subtractFlags_2)));
    if ((newBc_5 !== 0) && !Flags__get_zero(subtractFlags_2)) {
        RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_177), (RegisterFile__Pc(Machine__get_Regs(m_177)) - 1) & 65535);
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_177), (RegisterFile__Pc(Machine__get_Regs(m_177)) - 2) & 65535);
        Machine__PassTime_Z524259A4(m_177, 5);
    }
}, (m_178) => {
    Machine__Fetch(m_178);
    Machine__Fetch(m_178);
}, (m_179) => {
    Machine__Fetch(m_179);
    Machine__Fetch(m_179);
}, (m_180) => {
    Machine__Fetch(m_180);
    Machine__Fetch(m_180);
}, (m_181) => {
    Machine__Fetch(m_181);
    Machine__Fetch(m_181);
}, (m_182) => {
    Machine__Fetch(m_182);
    Machine__Fetch(m_182);
}, (m_183) => {
    Machine__Fetch(m_183);
    Machine__Fetch(m_183);
}, (m_184) => {
    Machine__Fetch(m_184);
    Machine__Fetch(m_184);
    const hl_6 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_184), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_184), R16.HL, (hl_6 + 65535) & 65535);
    const byte_6 = Machine__Read_Z524259A4(m_184, hl_6) | 0;
    const de_3 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_184), R16.DE) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_184), R16.DE, (de_3 + 65535) & 65535);
    Machine__Write_Z37302880(m_184, de_3, byte_6);
    Machine__PassTime_Z524259A4(m_184, 2);
    const flagBits_6 = (byte_6 + RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.A)) | 0;
    const newBc_6 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_184), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_184), R16.BC, newBc_6);
    const preservedFlags_6 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_184), Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Sign(), Flags_Zero()), Flags_Carry()));
    const flagsFromBits_6 = Flags_op_BitwiseOr_603E7D40(((flagBits_6 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_6 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_6 = (newBc_6 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_184, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_6, flagsFromBits_6), flagsFromBc_6));
    if (newBc_6 !== 0) {
        RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_184), (RegisterFile__Pc(Machine__get_Regs(m_184)) - 1) & 65535);
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_184), (RegisterFile__Pc(Machine__get_Regs(m_184)) - 2) & 65535);
        Machine__PassTime_Z524259A4(m_184, 5);
    }
}, (m_185) => {
    Machine__Fetch(m_185);
    Machine__Fetch(m_185);
    const hl_7 = RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_185), R16.HL) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_185), R16.HL, (hl_7 + 65535) & 65535);
    const byte_7 = Machine__Read_Z524259A4(m_185, hl_7) | 0;
    const patternInput_12 = Alu_sub8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.A), byte_7, false);
    const subtractFlags_3 = patternInput_12[1];
    const result_19 = patternInput_12[0] | 0;
    Machine__PassTime_Z524259A4(m_185, 5);
    const flagBits_7 = (Flags__get_half_carry(subtractFlags_3) ? (result_19 - 1) : result_19) | 0;
    const newBc_7 = ((RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_185), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_ZC22B834(Machine__get_Regs(m_185), R16.BC, newBc_7);
    const fromSubtractMask_3 = Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_HalfCarry(), Flags_Zero()), Flags_Sign()), Flags_Subtract());
    const preservedFlags_7 = Flags_op_BitwiseAnd_603E7D40(Machine__Flags(m_185), Flags_op_LogicalNot_2901ED1A(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_Flag3(), Flags_Flag5()), fromSubtractMask_3), Flags_Overflow())));
    const flagsFromBits_7 = Flags_op_BitwiseOr_603E7D40(((flagBits_7 & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits_7 & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc_7 = (newBc_7 !== 0) ? Flags_Overflow() : (new Flags(0));
    Machine__SetFlags_2901ED1A(m_185, Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(Flags_op_BitwiseOr_603E7D40(preservedFlags_7, flagsFromBits_7), flagsFromBc_7), Flags_op_BitwiseAnd_603E7D40(fromSubtractMask_3, subtractFlags_3)));
    if ((newBc_7 !== 0) && !Flags__get_zero(subtractFlags_3)) {
        RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_185), (RegisterFile__Pc(Machine__get_Regs(m_185)) - 1) & 65535);
        RegisterFile__SetPc_Z524259A4(Machine__get_Regs(m_185), (RegisterFile__Pc(Machine__get_Regs(m_185)) - 2) & 65535);
        Machine__PassTime_Z524259A4(m_185, 5);
    }
}, (m_186) => {
    Machine__Fetch(m_186);
    Machine__Fetch(m_186);
}, (m_187) => {
    Machine__Fetch(m_187);
    Machine__Fetch(m_187);
}, (m_188) => {
    Machine__Fetch(m_188);
    Machine__Fetch(m_188);
}, (m_189) => {
    Machine__Fetch(m_189);
    Machine__Fetch(m_189);
}, (m_190) => {
    Machine__Fetch(m_190);
    Machine__Fetch(m_190);
}, (m_191) => {
    Machine__Fetch(m_191);
    Machine__Fetch(m_191);
}, (m_192) => {
    Machine__Fetch(m_192);
    Machine__Fetch(m_192);
}, (m_193) => {
    Machine__Fetch(m_193);
    Machine__Fetch(m_193);
}, (m_194) => {
    Machine__Fetch(m_194);
    Machine__Fetch(m_194);
}, (m_195) => {
    Machine__Fetch(m_195);
    Machine__Fetch(m_195);
}, (m_196) => {
    Machine__Fetch(m_196);
    Machine__Fetch(m_196);
}, (m_197) => {
    Machine__Fetch(m_197);
    Machine__Fetch(m_197);
}, (m_198) => {
    Machine__Fetch(m_198);
    Machine__Fetch(m_198);
}, (m_199) => {
    Machine__Fetch(m_199);
    Machine__Fetch(m_199);
}, (m_200) => {
    Machine__Fetch(m_200);
    Machine__Fetch(m_200);
}, (m_201) => {
    Machine__Fetch(m_201);
    Machine__Fetch(m_201);
}, (m_202) => {
    Machine__Fetch(m_202);
    Machine__Fetch(m_202);
}, (m_203) => {
    Machine__Fetch(m_203);
    Machine__Fetch(m_203);
}, (m_204) => {
    Machine__Fetch(m_204);
    Machine__Fetch(m_204);
}, (m_205) => {
    Machine__Fetch(m_205);
    Machine__Fetch(m_205);
}, (m_206) => {
    Machine__Fetch(m_206);
    Machine__Fetch(m_206);
}, (m_207) => {
    Machine__Fetch(m_207);
    Machine__Fetch(m_207);
}, (m_208) => {
    Machine__Fetch(m_208);
    Machine__Fetch(m_208);
}, (m_209) => {
    Machine__Fetch(m_209);
    Machine__Fetch(m_209);
}, (m_210) => {
    Machine__Fetch(m_210);
    Machine__Fetch(m_210);
}, (m_211) => {
    Machine__Fetch(m_211);
    Machine__Fetch(m_211);
}, (m_212) => {
    Machine__Fetch(m_212);
    Machine__Fetch(m_212);
}, (m_213) => {
    Machine__Fetch(m_213);
    Machine__Fetch(m_213);
}, (m_214) => {
    Machine__Fetch(m_214);
    Machine__Fetch(m_214);
}, (m_215) => {
    Machine__Fetch(m_215);
    Machine__Fetch(m_215);
}, (m_216) => {
    Machine__Fetch(m_216);
    Machine__Fetch(m_216);
}, (m_217) => {
    Machine__Fetch(m_217);
    Machine__Fetch(m_217);
}, (m_218) => {
    Machine__Fetch(m_218);
    Machine__Fetch(m_218);
}, (m_219) => {
    Machine__Fetch(m_219);
    Machine__Fetch(m_219);
}, (m_220) => {
    Machine__Fetch(m_220);
    Machine__Fetch(m_220);
}, (m_221) => {
    Machine__Fetch(m_221);
    Machine__Fetch(m_221);
}, (m_222) => {
    Machine__Fetch(m_222);
    Machine__Fetch(m_222);
}, (m_223) => {
    Machine__Fetch(m_223);
    Machine__Fetch(m_223);
}, (m_224) => {
    Machine__Fetch(m_224);
    Machine__Fetch(m_224);
}, (m_225) => {
    Machine__Fetch(m_225);
    Machine__Fetch(m_225);
}, (m_226) => {
    Machine__Fetch(m_226);
    Machine__Fetch(m_226);
}, (m_227) => {
    Machine__Fetch(m_227);
    Machine__Fetch(m_227);
}, (m_228) => {
    Machine__Fetch(m_228);
    Machine__Fetch(m_228);
}, (m_229) => {
    Machine__Fetch(m_229);
    Machine__Fetch(m_229);
}, (m_230) => {
    Machine__Fetch(m_230);
    Machine__Fetch(m_230);
}, (m_231) => {
    Machine__Fetch(m_231);
    Machine__Fetch(m_231);
}, (m_232) => {
    Machine__Fetch(m_232);
    Machine__Fetch(m_232);
}, (m_233) => {
    Machine__Fetch(m_233);
    Machine__Fetch(m_233);
}, (m_234) => {
    Machine__Fetch(m_234);
    Machine__Fetch(m_234);
}, (m_235) => {
    Machine__Fetch(m_235);
    Machine__Fetch(m_235);
}, (m_236) => {
    Machine__Fetch(m_236);
    Machine__Fetch(m_236);
}, (m_237) => {
    Machine__Fetch(m_237);
    Machine__Fetch(m_237);
}, (m_238) => {
    Machine__Fetch(m_238);
    Machine__Fetch(m_238);
}, (m_239) => {
    Machine__Fetch(m_239);
    Machine__Fetch(m_239);
}, (m_240) => {
    Machine__Fetch(m_240);
    Machine__Fetch(m_240);
}, (m_241) => {
    Machine__Fetch(m_241);
    Machine__Fetch(m_241);
}, (m_242) => {
    Machine__Fetch(m_242);
    Machine__Fetch(m_242);
}, (m_243) => {
    Machine__Fetch(m_243);
    Machine__Fetch(m_243);
}, (m_244) => {
    Machine__Fetch(m_244);
    Machine__Fetch(m_244);
}, (m_245) => {
    Machine__Fetch(m_245);
    Machine__Fetch(m_245);
}, (m_246) => {
    Machine__Fetch(m_246);
    Machine__Fetch(m_246);
}, (m_247) => {
    Machine__Fetch(m_247);
    Machine__Fetch(m_247);
}, (m_248) => {
    Machine__Fetch(m_248);
    Machine__Fetch(m_248);
}, (m_249) => {
    Machine__Fetch(m_249);
    Machine__Fetch(m_249);
}, (m_250) => {
    Machine__Fetch(m_250);
    Machine__Fetch(m_250);
}, (m_251) => {
    Machine__Fetch(m_251);
    Machine__Fetch(m_251);
}, (m_252) => {
    Machine__Fetch(m_252);
    Machine__Fetch(m_252);
}, (m_253) => {
    Machine__Fetch(m_253);
    Machine__Fetch(m_253);
}, (m_254) => {
    Machine__Fetch(m_254);
    Machine__Fetch(m_254);
}, (m_255) => {
    Machine__Fetch(m_255);
    Machine__Fetch(m_255);
}];

export const cb = [(m) => {
    Machine__Fetch(m);
    Machine__Fetch(m);
    const patternInput = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m), R8.B), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m), R8.B, patternInput[0]);
    Machine__SetFlags_2901ED1A(m, patternInput[1]);
}, (m_1) => {
    Machine__Fetch(m_1);
    Machine__Fetch(m_1);
    const patternInput_1 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_1), R8.C), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_1), R8.C, patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_1, patternInput_1[1]);
}, (m_2) => {
    Machine__Fetch(m_2);
    Machine__Fetch(m_2);
    const patternInput_2 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_2), R8.D), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_2), R8.D, patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_2, patternInput_2[1]);
}, (m_3) => {
    Machine__Fetch(m_3);
    Machine__Fetch(m_3);
    const patternInput_3 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_3), R8.E), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_3), R8.E, patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_3, patternInput_3[1]);
}, (m_4) => {
    Machine__Fetch(m_4);
    Machine__Fetch(m_4);
    const patternInput_4 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_4), R8.H), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_4), R8.H, patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_4, patternInput_4[1]);
}, (m_5) => {
    Machine__Fetch(m_5);
    Machine__Fetch(m_5);
    const patternInput_5 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_5), R8.L), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_5), R8.L, patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_5, patternInput_5[1]);
}, (m_6) => {
    Machine__Fetch(m_6);
    Machine__Fetch(m_6);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_6), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_6), R16.HL));
    const lhs = Machine__Read_Z524259A4(m_6, RegisterFile__Wz(Machine__get_Regs(m_6))) | 0;
    Machine__PassTime_Z524259A4(m_6, 1);
    const patternInput_6 = Alu_rotateCircular8(lhs, Alu_Direction.Left);
    Machine__Write_Z37302880(m_6, RegisterFile__Wz(Machine__get_Regs(m_6)), patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_6, patternInput_6[1]);
}, (m_7) => {
    Machine__Fetch(m_7);
    Machine__Fetch(m_7);
    const patternInput_7 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_7), R8.A), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_7), R8.A, patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_7, patternInput_7[1]);
}, (m_8) => {
    Machine__Fetch(m_8);
    Machine__Fetch(m_8);
    const patternInput_8 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_8), R8.B), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_8), R8.B, patternInput_8[0]);
    Machine__SetFlags_2901ED1A(m_8, patternInput_8[1]);
}, (m_9) => {
    Machine__Fetch(m_9);
    Machine__Fetch(m_9);
    const patternInput_9 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_9), R8.C), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_9), R8.C, patternInput_9[0]);
    Machine__SetFlags_2901ED1A(m_9, patternInput_9[1]);
}, (m_10) => {
    Machine__Fetch(m_10);
    Machine__Fetch(m_10);
    const patternInput_10 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_10), R8.D), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_10), R8.D, patternInput_10[0]);
    Machine__SetFlags_2901ED1A(m_10, patternInput_10[1]);
}, (m_11) => {
    Machine__Fetch(m_11);
    Machine__Fetch(m_11);
    const patternInput_11 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_11), R8.E), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_11), R8.E, patternInput_11[0]);
    Machine__SetFlags_2901ED1A(m_11, patternInput_11[1]);
}, (m_12) => {
    Machine__Fetch(m_12);
    Machine__Fetch(m_12);
    const patternInput_12 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_12), R8.H), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_12), R8.H, patternInput_12[0]);
    Machine__SetFlags_2901ED1A(m_12, patternInput_12[1]);
}, (m_13) => {
    Machine__Fetch(m_13);
    Machine__Fetch(m_13);
    const patternInput_13 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_13), R8.L), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_13), R8.L, patternInput_13[0]);
    Machine__SetFlags_2901ED1A(m_13, patternInput_13[1]);
}, (m_14) => {
    Machine__Fetch(m_14);
    Machine__Fetch(m_14);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_14), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_14), R16.HL));
    const lhs_1 = Machine__Read_Z524259A4(m_14, RegisterFile__Wz(Machine__get_Regs(m_14))) | 0;
    Machine__PassTime_Z524259A4(m_14, 1);
    const patternInput_14 = Alu_rotateCircular8(lhs_1, Alu_Direction.Right);
    Machine__Write_Z37302880(m_14, RegisterFile__Wz(Machine__get_Regs(m_14)), patternInput_14[0]);
    Machine__SetFlags_2901ED1A(m_14, patternInput_14[1]);
}, (m_15) => {
    Machine__Fetch(m_15);
    Machine__Fetch(m_15);
    const patternInput_15 = Alu_rotateCircular8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_15), R8.A), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_15), R8.A, patternInput_15[0]);
    Machine__SetFlags_2901ED1A(m_15, patternInput_15[1]);
}, (m_16) => {
    let copyOfStruct;
    Machine__Fetch(m_16);
    Machine__Fetch(m_16);
    const patternInput_16 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_16), R8.B), Alu_Direction.Left, (copyOfStruct = Machine__Flags(m_16), Flags__get_carry(copyOfStruct)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_16), R8.B, patternInput_16[0]);
    Machine__SetFlags_2901ED1A(m_16, patternInput_16[1]);
}, (m_17) => {
    let copyOfStruct_1;
    Machine__Fetch(m_17);
    Machine__Fetch(m_17);
    const patternInput_17 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_17), R8.C), Alu_Direction.Left, (copyOfStruct_1 = Machine__Flags(m_17), Flags__get_carry(copyOfStruct_1)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_17), R8.C, patternInput_17[0]);
    Machine__SetFlags_2901ED1A(m_17, patternInput_17[1]);
}, (m_18) => {
    let copyOfStruct_2;
    Machine__Fetch(m_18);
    Machine__Fetch(m_18);
    const patternInput_18 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_18), R8.D), Alu_Direction.Left, (copyOfStruct_2 = Machine__Flags(m_18), Flags__get_carry(copyOfStruct_2)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_18), R8.D, patternInput_18[0]);
    Machine__SetFlags_2901ED1A(m_18, patternInput_18[1]);
}, (m_19) => {
    let copyOfStruct_3;
    Machine__Fetch(m_19);
    Machine__Fetch(m_19);
    const patternInput_19 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_19), R8.E), Alu_Direction.Left, (copyOfStruct_3 = Machine__Flags(m_19), Flags__get_carry(copyOfStruct_3)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_19), R8.E, patternInput_19[0]);
    Machine__SetFlags_2901ED1A(m_19, patternInput_19[1]);
}, (m_20) => {
    let copyOfStruct_4;
    Machine__Fetch(m_20);
    Machine__Fetch(m_20);
    const patternInput_20 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_20), R8.H), Alu_Direction.Left, (copyOfStruct_4 = Machine__Flags(m_20), Flags__get_carry(copyOfStruct_4)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_20), R8.H, patternInput_20[0]);
    Machine__SetFlags_2901ED1A(m_20, patternInput_20[1]);
}, (m_21) => {
    let copyOfStruct_5;
    Machine__Fetch(m_21);
    Machine__Fetch(m_21);
    const patternInput_21 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_21), R8.L), Alu_Direction.Left, (copyOfStruct_5 = Machine__Flags(m_21), Flags__get_carry(copyOfStruct_5)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_21), R8.L, patternInput_21[0]);
    Machine__SetFlags_2901ED1A(m_21, patternInput_21[1]);
}, (m_22) => {
    let copyOfStruct_6;
    Machine__Fetch(m_22);
    Machine__Fetch(m_22);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_22), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_22), R16.HL));
    const lhs_2 = Machine__Read_Z524259A4(m_22, RegisterFile__Wz(Machine__get_Regs(m_22))) | 0;
    Machine__PassTime_Z524259A4(m_22, 1);
    const patternInput_22 = Alu_rotate8(lhs_2, Alu_Direction.Left, (copyOfStruct_6 = Machine__Flags(m_22), Flags__get_carry(copyOfStruct_6)));
    Machine__Write_Z37302880(m_22, RegisterFile__Wz(Machine__get_Regs(m_22)), patternInput_22[0]);
    Machine__SetFlags_2901ED1A(m_22, patternInput_22[1]);
}, (m_23) => {
    let copyOfStruct_7;
    Machine__Fetch(m_23);
    Machine__Fetch(m_23);
    const patternInput_23 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_23), R8.A), Alu_Direction.Left, (copyOfStruct_7 = Machine__Flags(m_23), Flags__get_carry(copyOfStruct_7)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_23), R8.A, patternInput_23[0]);
    Machine__SetFlags_2901ED1A(m_23, patternInput_23[1]);
}, (m_24) => {
    let copyOfStruct_8;
    Machine__Fetch(m_24);
    Machine__Fetch(m_24);
    const patternInput_24 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_24), R8.B), Alu_Direction.Right, (copyOfStruct_8 = Machine__Flags(m_24), Flags__get_carry(copyOfStruct_8)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_24), R8.B, patternInput_24[0]);
    Machine__SetFlags_2901ED1A(m_24, patternInput_24[1]);
}, (m_25) => {
    let copyOfStruct_9;
    Machine__Fetch(m_25);
    Machine__Fetch(m_25);
    const patternInput_25 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_25), R8.C), Alu_Direction.Right, (copyOfStruct_9 = Machine__Flags(m_25), Flags__get_carry(copyOfStruct_9)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_25), R8.C, patternInput_25[0]);
    Machine__SetFlags_2901ED1A(m_25, patternInput_25[1]);
}, (m_26) => {
    let copyOfStruct_10;
    Machine__Fetch(m_26);
    Machine__Fetch(m_26);
    const patternInput_26 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_26), R8.D), Alu_Direction.Right, (copyOfStruct_10 = Machine__Flags(m_26), Flags__get_carry(copyOfStruct_10)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_26), R8.D, patternInput_26[0]);
    Machine__SetFlags_2901ED1A(m_26, patternInput_26[1]);
}, (m_27) => {
    let copyOfStruct_11;
    Machine__Fetch(m_27);
    Machine__Fetch(m_27);
    const patternInput_27 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_27), R8.E), Alu_Direction.Right, (copyOfStruct_11 = Machine__Flags(m_27), Flags__get_carry(copyOfStruct_11)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_27), R8.E, patternInput_27[0]);
    Machine__SetFlags_2901ED1A(m_27, patternInput_27[1]);
}, (m_28) => {
    let copyOfStruct_12;
    Machine__Fetch(m_28);
    Machine__Fetch(m_28);
    const patternInput_28 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_28), R8.H), Alu_Direction.Right, (copyOfStruct_12 = Machine__Flags(m_28), Flags__get_carry(copyOfStruct_12)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_28), R8.H, patternInput_28[0]);
    Machine__SetFlags_2901ED1A(m_28, patternInput_28[1]);
}, (m_29) => {
    let copyOfStruct_13;
    Machine__Fetch(m_29);
    Machine__Fetch(m_29);
    const patternInput_29 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_29), R8.L), Alu_Direction.Right, (copyOfStruct_13 = Machine__Flags(m_29), Flags__get_carry(copyOfStruct_13)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_29), R8.L, patternInput_29[0]);
    Machine__SetFlags_2901ED1A(m_29, patternInput_29[1]);
}, (m_30) => {
    let copyOfStruct_14;
    Machine__Fetch(m_30);
    Machine__Fetch(m_30);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_30), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_30), R16.HL));
    const lhs_3 = Machine__Read_Z524259A4(m_30, RegisterFile__Wz(Machine__get_Regs(m_30))) | 0;
    Machine__PassTime_Z524259A4(m_30, 1);
    const patternInput_30 = Alu_rotate8(lhs_3, Alu_Direction.Right, (copyOfStruct_14 = Machine__Flags(m_30), Flags__get_carry(copyOfStruct_14)));
    Machine__Write_Z37302880(m_30, RegisterFile__Wz(Machine__get_Regs(m_30)), patternInput_30[0]);
    Machine__SetFlags_2901ED1A(m_30, patternInput_30[1]);
}, (m_31) => {
    let copyOfStruct_15;
    Machine__Fetch(m_31);
    Machine__Fetch(m_31);
    const patternInput_31 = Alu_rotate8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_31), R8.A), Alu_Direction.Right, (copyOfStruct_15 = Machine__Flags(m_31), Flags__get_carry(copyOfStruct_15)));
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_31), R8.A, patternInput_31[0]);
    Machine__SetFlags_2901ED1A(m_31, patternInput_31[1]);
}, (m_32) => {
    Machine__Fetch(m_32);
    Machine__Fetch(m_32);
    const patternInput_32 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_32), R8.B), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_32), R8.B, patternInput_32[0]);
    Machine__SetFlags_2901ED1A(m_32, patternInput_32[1]);
}, (m_33) => {
    Machine__Fetch(m_33);
    Machine__Fetch(m_33);
    const patternInput_33 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_33), R8.C), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_33), R8.C, patternInput_33[0]);
    Machine__SetFlags_2901ED1A(m_33, patternInput_33[1]);
}, (m_34) => {
    Machine__Fetch(m_34);
    Machine__Fetch(m_34);
    const patternInput_34 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_34), R8.D), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_34), R8.D, patternInput_34[0]);
    Machine__SetFlags_2901ED1A(m_34, patternInput_34[1]);
}, (m_35) => {
    Machine__Fetch(m_35);
    Machine__Fetch(m_35);
    const patternInput_35 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_35), R8.E), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_35), R8.E, patternInput_35[0]);
    Machine__SetFlags_2901ED1A(m_35, patternInput_35[1]);
}, (m_36) => {
    Machine__Fetch(m_36);
    Machine__Fetch(m_36);
    const patternInput_36 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_36), R8.H), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_36), R8.H, patternInput_36[0]);
    Machine__SetFlags_2901ED1A(m_36, patternInput_36[1]);
}, (m_37) => {
    Machine__Fetch(m_37);
    Machine__Fetch(m_37);
    const patternInput_37 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_37), R8.L), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_37), R8.L, patternInput_37[0]);
    Machine__SetFlags_2901ED1A(m_37, patternInput_37[1]);
}, (m_38) => {
    Machine__Fetch(m_38);
    Machine__Fetch(m_38);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_38), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_38), R16.HL));
    const lhs_4 = Machine__Read_Z524259A4(m_38, RegisterFile__Wz(Machine__get_Regs(m_38))) | 0;
    Machine__PassTime_Z524259A4(m_38, 1);
    const patternInput_38 = Alu_shiftArithmetic8(lhs_4, Alu_Direction.Left);
    Machine__Write_Z37302880(m_38, RegisterFile__Wz(Machine__get_Regs(m_38)), patternInput_38[0]);
    Machine__SetFlags_2901ED1A(m_38, patternInput_38[1]);
}, (m_39) => {
    Machine__Fetch(m_39);
    Machine__Fetch(m_39);
    const patternInput_39 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_39), R8.A), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_39), R8.A, patternInput_39[0]);
    Machine__SetFlags_2901ED1A(m_39, patternInput_39[1]);
}, (m_40) => {
    Machine__Fetch(m_40);
    Machine__Fetch(m_40);
    const patternInput_40 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_40), R8.B), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_40), R8.B, patternInput_40[0]);
    Machine__SetFlags_2901ED1A(m_40, patternInput_40[1]);
}, (m_41) => {
    Machine__Fetch(m_41);
    Machine__Fetch(m_41);
    const patternInput_41 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_41), R8.C), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_41), R8.C, patternInput_41[0]);
    Machine__SetFlags_2901ED1A(m_41, patternInput_41[1]);
}, (m_42) => {
    Machine__Fetch(m_42);
    Machine__Fetch(m_42);
    const patternInput_42 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_42), R8.D), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_42), R8.D, patternInput_42[0]);
    Machine__SetFlags_2901ED1A(m_42, patternInput_42[1]);
}, (m_43) => {
    Machine__Fetch(m_43);
    Machine__Fetch(m_43);
    const patternInput_43 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_43), R8.E), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_43), R8.E, patternInput_43[0]);
    Machine__SetFlags_2901ED1A(m_43, patternInput_43[1]);
}, (m_44) => {
    Machine__Fetch(m_44);
    Machine__Fetch(m_44);
    const patternInput_44 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_44), R8.H), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_44), R8.H, patternInput_44[0]);
    Machine__SetFlags_2901ED1A(m_44, patternInput_44[1]);
}, (m_45) => {
    Machine__Fetch(m_45);
    Machine__Fetch(m_45);
    const patternInput_45 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_45), R8.L), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_45), R8.L, patternInput_45[0]);
    Machine__SetFlags_2901ED1A(m_45, patternInput_45[1]);
}, (m_46) => {
    Machine__Fetch(m_46);
    Machine__Fetch(m_46);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_46), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_46), R16.HL));
    const lhs_5 = Machine__Read_Z524259A4(m_46, RegisterFile__Wz(Machine__get_Regs(m_46))) | 0;
    Machine__PassTime_Z524259A4(m_46, 1);
    const patternInput_46 = Alu_shiftArithmetic8(lhs_5, Alu_Direction.Right);
    Machine__Write_Z37302880(m_46, RegisterFile__Wz(Machine__get_Regs(m_46)), patternInput_46[0]);
    Machine__SetFlags_2901ED1A(m_46, patternInput_46[1]);
}, (m_47) => {
    Machine__Fetch(m_47);
    Machine__Fetch(m_47);
    const patternInput_47 = Alu_shiftArithmetic8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_47), R8.A), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_47), R8.A, patternInput_47[0]);
    Machine__SetFlags_2901ED1A(m_47, patternInput_47[1]);
}, (m_48) => {
    Machine__Fetch(m_48);
    Machine__Fetch(m_48);
    const patternInput_48 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_48), R8.B), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_48), R8.B, patternInput_48[0]);
    Machine__SetFlags_2901ED1A(m_48, patternInput_48[1]);
}, (m_49) => {
    Machine__Fetch(m_49);
    Machine__Fetch(m_49);
    const patternInput_49 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_49), R8.C), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_49), R8.C, patternInput_49[0]);
    Machine__SetFlags_2901ED1A(m_49, patternInput_49[1]);
}, (m_50) => {
    Machine__Fetch(m_50);
    Machine__Fetch(m_50);
    const patternInput_50 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_50), R8.D), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_50), R8.D, patternInput_50[0]);
    Machine__SetFlags_2901ED1A(m_50, patternInput_50[1]);
}, (m_51) => {
    Machine__Fetch(m_51);
    Machine__Fetch(m_51);
    const patternInput_51 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_51), R8.E), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_51), R8.E, patternInput_51[0]);
    Machine__SetFlags_2901ED1A(m_51, patternInput_51[1]);
}, (m_52) => {
    Machine__Fetch(m_52);
    Machine__Fetch(m_52);
    const patternInput_52 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_52), R8.H), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_52), R8.H, patternInput_52[0]);
    Machine__SetFlags_2901ED1A(m_52, patternInput_52[1]);
}, (m_53) => {
    Machine__Fetch(m_53);
    Machine__Fetch(m_53);
    const patternInput_53 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_53), R8.L), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_53), R8.L, patternInput_53[0]);
    Machine__SetFlags_2901ED1A(m_53, patternInput_53[1]);
}, (m_54) => {
    Machine__Fetch(m_54);
    Machine__Fetch(m_54);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_54), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_54), R16.HL));
    const lhs_6 = Machine__Read_Z524259A4(m_54, RegisterFile__Wz(Machine__get_Regs(m_54))) | 0;
    Machine__PassTime_Z524259A4(m_54, 1);
    const patternInput_54 = Alu_shiftLogical8(lhs_6, Alu_Direction.Left);
    Machine__Write_Z37302880(m_54, RegisterFile__Wz(Machine__get_Regs(m_54)), patternInput_54[0]);
    Machine__SetFlags_2901ED1A(m_54, patternInput_54[1]);
}, (m_55) => {
    Machine__Fetch(m_55);
    Machine__Fetch(m_55);
    const patternInput_55 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_55), R8.A), Alu_Direction.Left);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_55), R8.A, patternInput_55[0]);
    Machine__SetFlags_2901ED1A(m_55, patternInput_55[1]);
}, (m_56) => {
    Machine__Fetch(m_56);
    Machine__Fetch(m_56);
    const patternInput_56 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_56), R8.B), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_56), R8.B, patternInput_56[0]);
    Machine__SetFlags_2901ED1A(m_56, patternInput_56[1]);
}, (m_57) => {
    Machine__Fetch(m_57);
    Machine__Fetch(m_57);
    const patternInput_57 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_57), R8.C), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_57), R8.C, patternInput_57[0]);
    Machine__SetFlags_2901ED1A(m_57, patternInput_57[1]);
}, (m_58) => {
    Machine__Fetch(m_58);
    Machine__Fetch(m_58);
    const patternInput_58 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_58), R8.D), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_58), R8.D, patternInput_58[0]);
    Machine__SetFlags_2901ED1A(m_58, patternInput_58[1]);
}, (m_59) => {
    Machine__Fetch(m_59);
    Machine__Fetch(m_59);
    const patternInput_59 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_59), R8.E), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_59), R8.E, patternInput_59[0]);
    Machine__SetFlags_2901ED1A(m_59, patternInput_59[1]);
}, (m_60) => {
    Machine__Fetch(m_60);
    Machine__Fetch(m_60);
    const patternInput_60 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_60), R8.H), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_60), R8.H, patternInput_60[0]);
    Machine__SetFlags_2901ED1A(m_60, patternInput_60[1]);
}, (m_61) => {
    Machine__Fetch(m_61);
    Machine__Fetch(m_61);
    const patternInput_61 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_61), R8.L), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_61), R8.L, patternInput_61[0]);
    Machine__SetFlags_2901ED1A(m_61, patternInput_61[1]);
}, (m_62) => {
    Machine__Fetch(m_62);
    Machine__Fetch(m_62);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_62), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_62), R16.HL));
    const lhs_7 = Machine__Read_Z524259A4(m_62, RegisterFile__Wz(Machine__get_Regs(m_62))) | 0;
    Machine__PassTime_Z524259A4(m_62, 1);
    const patternInput_62 = Alu_shiftLogical8(lhs_7, Alu_Direction.Right);
    Machine__Write_Z37302880(m_62, RegisterFile__Wz(Machine__get_Regs(m_62)), patternInput_62[0]);
    Machine__SetFlags_2901ED1A(m_62, patternInput_62[1]);
}, (m_63) => {
    Machine__Fetch(m_63);
    Machine__Fetch(m_63);
    const patternInput_63 = Alu_shiftLogical8(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_63), R8.A), Alu_Direction.Right);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_63), R8.A, patternInput_63[0]);
    Machine__SetFlags_2901ED1A(m_63, patternInput_63[1]);
}, (m_64) => {
    Machine__Fetch(m_64);
    Machine__Fetch(m_64);
    Machine__SetFlags_2901ED1A(m_64, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_64), R8.B), 1 << 0, Machine__Flags(m_64), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_64), R8.B)));
}, (m_65) => {
    Machine__Fetch(m_65);
    Machine__Fetch(m_65);
    Machine__SetFlags_2901ED1A(m_65, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_65), R8.C), 1 << 0, Machine__Flags(m_65), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_65), R8.C)));
}, (m_66) => {
    Machine__Fetch(m_66);
    Machine__Fetch(m_66);
    Machine__SetFlags_2901ED1A(m_66, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_66), R8.D), 1, Machine__Flags(m_66), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_66), R8.D)));
}, (m_67) => {
    Machine__Fetch(m_67);
    Machine__Fetch(m_67);
    Machine__SetFlags_2901ED1A(m_67, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.E), 1, Machine__Flags(m_67), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_67), R8.E)));
}, (m_68) => {
    Machine__Fetch(m_68);
    Machine__Fetch(m_68);
    Machine__SetFlags_2901ED1A(m_68, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_68), R8.H), 1, Machine__Flags(m_68), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_68), R8.H)));
}, (m_69) => {
    Machine__Fetch(m_69);
    Machine__Fetch(m_69);
    Machine__SetFlags_2901ED1A(m_69, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_69), R8.L), 1 << 0, Machine__Flags(m_69), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_69), R8.L)));
}, (m_70) => {
    Machine__Fetch(m_70);
    Machine__Fetch(m_70);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_70), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_70), R16.HL));
    const lhs_8 = Machine__Read_Z524259A4(m_70, RegisterFile__Wz(Machine__get_Regs(m_70))) | 0;
    Machine__PassTime_Z524259A4(m_70, 1);
    const busNoise = ((RegisterFile__Wz(Machine__get_Regs(m_70)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_70, Alu_bit(lhs_8, 1 << 0, Machine__Flags(m_70), busNoise));
}, (m_71) => {
    Machine__Fetch(m_71);
    Machine__Fetch(m_71);
    Machine__SetFlags_2901ED1A(m_71, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_71), R8.A), 1 << 0, Machine__Flags(m_71), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_71), R8.A)));
}, (m_72) => {
    Machine__Fetch(m_72);
    Machine__Fetch(m_72);
    Machine__SetFlags_2901ED1A(m_72, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_72), R8.B), 2, Machine__Flags(m_72), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_72), R8.B)));
}, (m_73) => {
    Machine__Fetch(m_73);
    Machine__Fetch(m_73);
    Machine__SetFlags_2901ED1A(m_73, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_73), R8.C), 1 << 1, Machine__Flags(m_73), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_73), R8.C)));
}, (m_74) => {
    Machine__Fetch(m_74);
    Machine__Fetch(m_74);
    Machine__SetFlags_2901ED1A(m_74, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_74), R8.D), 2, Machine__Flags(m_74), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_74), R8.D)));
}, (m_75) => {
    Machine__Fetch(m_75);
    Machine__Fetch(m_75);
    Machine__SetFlags_2901ED1A(m_75, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_75), R8.E), 2, Machine__Flags(m_75), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_75), R8.E)));
}, (m_76) => {
    Machine__Fetch(m_76);
    Machine__Fetch(m_76);
    Machine__SetFlags_2901ED1A(m_76, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_76), R8.H), 2, Machine__Flags(m_76), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_76), R8.H)));
}, (m_77) => {
    Machine__Fetch(m_77);
    Machine__Fetch(m_77);
    Machine__SetFlags_2901ED1A(m_77, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_77), R8.L), 2, Machine__Flags(m_77), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_77), R8.L)));
}, (m_78) => {
    Machine__Fetch(m_78);
    Machine__Fetch(m_78);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_78), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_78), R16.HL));
    const lhs_9 = Machine__Read_Z524259A4(m_78, RegisterFile__Wz(Machine__get_Regs(m_78))) | 0;
    Machine__PassTime_Z524259A4(m_78, 1);
    const busNoise_1 = ((RegisterFile__Wz(Machine__get_Regs(m_78)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_78, Alu_bit(lhs_9, 2, Machine__Flags(m_78), busNoise_1));
}, (m_79) => {
    Machine__Fetch(m_79);
    Machine__Fetch(m_79);
    Machine__SetFlags_2901ED1A(m_79, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_79), R8.A), 1 << 1, Machine__Flags(m_79), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_79), R8.A)));
}, (m_80) => {
    Machine__Fetch(m_80);
    Machine__Fetch(m_80);
    Machine__SetFlags_2901ED1A(m_80, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_80), R8.B), 1 << 2, Machine__Flags(m_80), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_80), R8.B)));
}, (m_81) => {
    Machine__Fetch(m_81);
    Machine__Fetch(m_81);
    Machine__SetFlags_2901ED1A(m_81, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_81), R8.C), 4, Machine__Flags(m_81), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_81), R8.C)));
}, (m_82) => {
    Machine__Fetch(m_82);
    Machine__Fetch(m_82);
    Machine__SetFlags_2901ED1A(m_82, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_82), R8.D), 4, Machine__Flags(m_82), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_82), R8.D)));
}, (m_83) => {
    Machine__Fetch(m_83);
    Machine__Fetch(m_83);
    Machine__SetFlags_2901ED1A(m_83, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.E), 1 << 2, Machine__Flags(m_83), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_83), R8.E)));
}, (m_84) => {
    Machine__Fetch(m_84);
    Machine__Fetch(m_84);
    Machine__SetFlags_2901ED1A(m_84, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_84), R8.H), 4, Machine__Flags(m_84), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_84), R8.H)));
}, (m_85) => {
    Machine__Fetch(m_85);
    Machine__Fetch(m_85);
    Machine__SetFlags_2901ED1A(m_85, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_85), R8.L), 4, Machine__Flags(m_85), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_85), R8.L)));
}, (m_86) => {
    Machine__Fetch(m_86);
    Machine__Fetch(m_86);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_86), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_86), R16.HL));
    const lhs_10 = Machine__Read_Z524259A4(m_86, RegisterFile__Wz(Machine__get_Regs(m_86))) | 0;
    Machine__PassTime_Z524259A4(m_86, 1);
    const busNoise_2 = ((RegisterFile__Wz(Machine__get_Regs(m_86)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_86, Alu_bit(lhs_10, 1 << 2, Machine__Flags(m_86), busNoise_2));
}, (m_87) => {
    Machine__Fetch(m_87);
    Machine__Fetch(m_87);
    Machine__SetFlags_2901ED1A(m_87, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_87), R8.A), 1 << 2, Machine__Flags(m_87), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_87), R8.A)));
}, (m_88) => {
    Machine__Fetch(m_88);
    Machine__Fetch(m_88);
    Machine__SetFlags_2901ED1A(m_88, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_88), R8.B), 8, Machine__Flags(m_88), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_88), R8.B)));
}, (m_89) => {
    Machine__Fetch(m_89);
    Machine__Fetch(m_89);
    Machine__SetFlags_2901ED1A(m_89, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_89), R8.C), 8, Machine__Flags(m_89), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_89), R8.C)));
}, (m_90) => {
    Machine__Fetch(m_90);
    Machine__Fetch(m_90);
    Machine__SetFlags_2901ED1A(m_90, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_90), R8.D), 1 << 3, Machine__Flags(m_90), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_90), R8.D)));
}, (m_91) => {
    Machine__Fetch(m_91);
    Machine__Fetch(m_91);
    Machine__SetFlags_2901ED1A(m_91, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_91), R8.E), 1 << 3, Machine__Flags(m_91), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_91), R8.E)));
}, (m_92) => {
    Machine__Fetch(m_92);
    Machine__Fetch(m_92);
    Machine__SetFlags_2901ED1A(m_92, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_92), R8.H), 8, Machine__Flags(m_92), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_92), R8.H)));
}, (m_93) => {
    Machine__Fetch(m_93);
    Machine__Fetch(m_93);
    Machine__SetFlags_2901ED1A(m_93, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_93), R8.L), 8, Machine__Flags(m_93), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_93), R8.L)));
}, (m_94) => {
    Machine__Fetch(m_94);
    Machine__Fetch(m_94);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_94), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_94), R16.HL));
    const lhs_11 = Machine__Read_Z524259A4(m_94, RegisterFile__Wz(Machine__get_Regs(m_94))) | 0;
    Machine__PassTime_Z524259A4(m_94, 1);
    const busNoise_3 = ((RegisterFile__Wz(Machine__get_Regs(m_94)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_94, Alu_bit(lhs_11, 1 << 3, Machine__Flags(m_94), busNoise_3));
}, (m_95) => {
    Machine__Fetch(m_95);
    Machine__Fetch(m_95);
    Machine__SetFlags_2901ED1A(m_95, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_95), R8.A), 1 << 3, Machine__Flags(m_95), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_95), R8.A)));
}, (m_96) => {
    Machine__Fetch(m_96);
    Machine__Fetch(m_96);
    Machine__SetFlags_2901ED1A(m_96, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_96), R8.B), 16, Machine__Flags(m_96), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_96), R8.B)));
}, (m_97) => {
    Machine__Fetch(m_97);
    Machine__Fetch(m_97);
    Machine__SetFlags_2901ED1A(m_97, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_97), R8.C), 16, Machine__Flags(m_97), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_97), R8.C)));
}, (m_98) => {
    Machine__Fetch(m_98);
    Machine__Fetch(m_98);
    Machine__SetFlags_2901ED1A(m_98, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_98), R8.D), 16, Machine__Flags(m_98), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_98), R8.D)));
}, (m_99) => {
    Machine__Fetch(m_99);
    Machine__Fetch(m_99);
    Machine__SetFlags_2901ED1A(m_99, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.E), 1 << 4, Machine__Flags(m_99), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_99), R8.E)));
}, (m_100) => {
    Machine__Fetch(m_100);
    Machine__Fetch(m_100);
    Machine__SetFlags_2901ED1A(m_100, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_100), R8.H), 16, Machine__Flags(m_100), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_100), R8.H)));
}, (m_101) => {
    Machine__Fetch(m_101);
    Machine__Fetch(m_101);
    Machine__SetFlags_2901ED1A(m_101, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_101), R8.L), 16, Machine__Flags(m_101), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_101), R8.L)));
}, (m_102) => {
    Machine__Fetch(m_102);
    Machine__Fetch(m_102);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_102), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_102), R16.HL));
    const lhs_12 = Machine__Read_Z524259A4(m_102, RegisterFile__Wz(Machine__get_Regs(m_102))) | 0;
    Machine__PassTime_Z524259A4(m_102, 1);
    const busNoise_4 = ((RegisterFile__Wz(Machine__get_Regs(m_102)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_102, Alu_bit(lhs_12, 16, Machine__Flags(m_102), busNoise_4));
}, (m_103) => {
    Machine__Fetch(m_103);
    Machine__Fetch(m_103);
    Machine__SetFlags_2901ED1A(m_103, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_103), R8.A), 1 << 4, Machine__Flags(m_103), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_103), R8.A)));
}, (m_104) => {
    Machine__Fetch(m_104);
    Machine__Fetch(m_104);
    Machine__SetFlags_2901ED1A(m_104, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_104), R8.B), 1 << 5, Machine__Flags(m_104), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_104), R8.B)));
}, (m_105) => {
    Machine__Fetch(m_105);
    Machine__Fetch(m_105);
    Machine__SetFlags_2901ED1A(m_105, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_105), R8.C), 1 << 5, Machine__Flags(m_105), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_105), R8.C)));
}, (m_106) => {
    Machine__Fetch(m_106);
    Machine__Fetch(m_106);
    Machine__SetFlags_2901ED1A(m_106, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_106), R8.D), 32, Machine__Flags(m_106), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_106), R8.D)));
}, (m_107) => {
    Machine__Fetch(m_107);
    Machine__Fetch(m_107);
    Machine__SetFlags_2901ED1A(m_107, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_107), R8.E), 32, Machine__Flags(m_107), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_107), R8.E)));
}, (m_108) => {
    Machine__Fetch(m_108);
    Machine__Fetch(m_108);
    Machine__SetFlags_2901ED1A(m_108, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_108), R8.H), 32, Machine__Flags(m_108), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_108), R8.H)));
}, (m_109) => {
    Machine__Fetch(m_109);
    Machine__Fetch(m_109);
    Machine__SetFlags_2901ED1A(m_109, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_109), R8.L), 32, Machine__Flags(m_109), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_109), R8.L)));
}, (m_110) => {
    Machine__Fetch(m_110);
    Machine__Fetch(m_110);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_110), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_110), R16.HL));
    const lhs_13 = Machine__Read_Z524259A4(m_110, RegisterFile__Wz(Machine__get_Regs(m_110))) | 0;
    Machine__PassTime_Z524259A4(m_110, 1);
    const busNoise_5 = ((RegisterFile__Wz(Machine__get_Regs(m_110)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_110, Alu_bit(lhs_13, 32, Machine__Flags(m_110), busNoise_5));
}, (m_111) => {
    Machine__Fetch(m_111);
    Machine__Fetch(m_111);
    Machine__SetFlags_2901ED1A(m_111, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_111), R8.A), 1 << 5, Machine__Flags(m_111), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_111), R8.A)));
}, (m_112) => {
    Machine__Fetch(m_112);
    Machine__Fetch(m_112);
    Machine__SetFlags_2901ED1A(m_112, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_112), R8.B), 1 << 6, Machine__Flags(m_112), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_112), R8.B)));
}, (m_113) => {
    Machine__Fetch(m_113);
    Machine__Fetch(m_113);
    Machine__SetFlags_2901ED1A(m_113, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_113), R8.C), 1 << 6, Machine__Flags(m_113), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_113), R8.C)));
}, (m_114) => {
    Machine__Fetch(m_114);
    Machine__Fetch(m_114);
    Machine__SetFlags_2901ED1A(m_114, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_114), R8.D), 64, Machine__Flags(m_114), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_114), R8.D)));
}, (m_115) => {
    Machine__Fetch(m_115);
    Machine__Fetch(m_115);
    Machine__SetFlags_2901ED1A(m_115, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.E), 64, Machine__Flags(m_115), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_115), R8.E)));
}, (m_116) => {
    Machine__Fetch(m_116);
    Machine__Fetch(m_116);
    Machine__SetFlags_2901ED1A(m_116, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_116), R8.H), 64, Machine__Flags(m_116), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_116), R8.H)));
}, (m_117) => {
    Machine__Fetch(m_117);
    Machine__Fetch(m_117);
    Machine__SetFlags_2901ED1A(m_117, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_117), R8.L), 64, Machine__Flags(m_117), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_117), R8.L)));
}, (m_118) => {
    Machine__Fetch(m_118);
    Machine__Fetch(m_118);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_118), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_118), R16.HL));
    const lhs_14 = Machine__Read_Z524259A4(m_118, RegisterFile__Wz(Machine__get_Regs(m_118))) | 0;
    Machine__PassTime_Z524259A4(m_118, 1);
    const busNoise_6 = ((RegisterFile__Wz(Machine__get_Regs(m_118)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_118, Alu_bit(lhs_14, 1 << 6, Machine__Flags(m_118), busNoise_6));
}, (m_119) => {
    Machine__Fetch(m_119);
    Machine__Fetch(m_119);
    Machine__SetFlags_2901ED1A(m_119, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_119), R8.A), 1 << 6, Machine__Flags(m_119), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_119), R8.A)));
}, (m_120) => {
    Machine__Fetch(m_120);
    Machine__Fetch(m_120);
    Machine__SetFlags_2901ED1A(m_120, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_120), R8.B), 1 << 7, Machine__Flags(m_120), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_120), R8.B)));
}, (m_121) => {
    Machine__Fetch(m_121);
    Machine__Fetch(m_121);
    Machine__SetFlags_2901ED1A(m_121, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_121), R8.C), 1 << 7, Machine__Flags(m_121), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_121), R8.C)));
}, (m_122) => {
    Machine__Fetch(m_122);
    Machine__Fetch(m_122);
    Machine__SetFlags_2901ED1A(m_122, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_122), R8.D), 1 << 7, Machine__Flags(m_122), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_122), R8.D)));
}, (m_123) => {
    Machine__Fetch(m_123);
    Machine__Fetch(m_123);
    Machine__SetFlags_2901ED1A(m_123, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_123), R8.E), 1 << 7, Machine__Flags(m_123), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_123), R8.E)));
}, (m_124) => {
    Machine__Fetch(m_124);
    Machine__Fetch(m_124);
    Machine__SetFlags_2901ED1A(m_124, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_124), R8.H), 128, Machine__Flags(m_124), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_124), R8.H)));
}, (m_125) => {
    Machine__Fetch(m_125);
    Machine__Fetch(m_125);
    Machine__SetFlags_2901ED1A(m_125, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_125), R8.L), 128, Machine__Flags(m_125), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_125), R8.L)));
}, (m_126) => {
    Machine__Fetch(m_126);
    Machine__Fetch(m_126);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_126), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_126), R16.HL));
    const lhs_15 = Machine__Read_Z524259A4(m_126, RegisterFile__Wz(Machine__get_Regs(m_126))) | 0;
    Machine__PassTime_Z524259A4(m_126, 1);
    const busNoise_7 = ((RegisterFile__Wz(Machine__get_Regs(m_126)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_126, Alu_bit(lhs_15, 1 << 7, Machine__Flags(m_126), busNoise_7));
}, (m_127) => {
    Machine__Fetch(m_127);
    Machine__Fetch(m_127);
    Machine__SetFlags_2901ED1A(m_127, Alu_bit(RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_127), R8.A), 1 << 7, Machine__Flags(m_127), RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_127), R8.A)));
}, (m_128) => {
    Machine__Fetch(m_128);
    Machine__Fetch(m_128);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_128), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_128), R8.B) & ~(1 << 0));
}, (m_129) => {
    Machine__Fetch(m_129);
    Machine__Fetch(m_129);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_129), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_129), R8.C) & ~1);
}, (m_130) => {
    Machine__Fetch(m_130);
    Machine__Fetch(m_130);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_130), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_130), R8.D) & ~(1 << 0));
}, (m_131) => {
    Machine__Fetch(m_131);
    Machine__Fetch(m_131);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_131), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_131), R8.E) & ~1);
}, (m_132) => {
    Machine__Fetch(m_132);
    Machine__Fetch(m_132);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_132), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_132), R8.H) & ~1);
}, (m_133) => {
    Machine__Fetch(m_133);
    Machine__Fetch(m_133);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_133), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_133), R8.L) & ~1);
}, (m_134) => {
    Machine__Fetch(m_134);
    Machine__Fetch(m_134);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_134), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_134), R16.HL));
    const lhs_16 = Machine__Read_Z524259A4(m_134, RegisterFile__Wz(Machine__get_Regs(m_134))) | 0;
    Machine__PassTime_Z524259A4(m_134, 1);
    Machine__Write_Z37302880(m_134, RegisterFile__Wz(Machine__get_Regs(m_134)), lhs_16 & ~(1 << 0));
}, (m_135) => {
    Machine__Fetch(m_135);
    Machine__Fetch(m_135);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_135), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_135), R8.A) & ~1);
}, (m_136) => {
    Machine__Fetch(m_136);
    Machine__Fetch(m_136);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_136), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_136), R8.B) & ~2);
}, (m_137) => {
    Machine__Fetch(m_137);
    Machine__Fetch(m_137);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_137), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_137), R8.C) & ~2);
}, (m_138) => {
    Machine__Fetch(m_138);
    Machine__Fetch(m_138);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_138), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_138), R8.D) & ~(1 << 1));
}, (m_139) => {
    Machine__Fetch(m_139);
    Machine__Fetch(m_139);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_139), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_139), R8.E) & ~2);
}, (m_140) => {
    Machine__Fetch(m_140);
    Machine__Fetch(m_140);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_140), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_140), R8.H) & ~2);
}, (m_141) => {
    Machine__Fetch(m_141);
    Machine__Fetch(m_141);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_141), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_141), R8.L) & ~2);
}, (m_142) => {
    Machine__Fetch(m_142);
    Machine__Fetch(m_142);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_142), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_142), R16.HL));
    const lhs_17 = Machine__Read_Z524259A4(m_142, RegisterFile__Wz(Machine__get_Regs(m_142))) | 0;
    Machine__PassTime_Z524259A4(m_142, 1);
    Machine__Write_Z37302880(m_142, RegisterFile__Wz(Machine__get_Regs(m_142)), lhs_17 & ~(1 << 1));
}, (m_143) => {
    Machine__Fetch(m_143);
    Machine__Fetch(m_143);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_143), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_143), R8.A) & ~2);
}, (m_144) => {
    Machine__Fetch(m_144);
    Machine__Fetch(m_144);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_144), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_144), R8.B) & ~4);
}, (m_145) => {
    Machine__Fetch(m_145);
    Machine__Fetch(m_145);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_145), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_145), R8.C) & ~4);
}, (m_146) => {
    Machine__Fetch(m_146);
    Machine__Fetch(m_146);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_146), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_146), R8.D) & ~4);
}, (m_147) => {
    Machine__Fetch(m_147);
    Machine__Fetch(m_147);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_147), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_147), R8.E) & ~4);
}, (m_148) => {
    Machine__Fetch(m_148);
    Machine__Fetch(m_148);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_148), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_148), R8.H) & ~4);
}, (m_149) => {
    Machine__Fetch(m_149);
    Machine__Fetch(m_149);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_149), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_149), R8.L) & ~4);
}, (m_150) => {
    Machine__Fetch(m_150);
    Machine__Fetch(m_150);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_150), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_150), R16.HL));
    const lhs_18 = Machine__Read_Z524259A4(m_150, RegisterFile__Wz(Machine__get_Regs(m_150))) | 0;
    Machine__PassTime_Z524259A4(m_150, 1);
    Machine__Write_Z37302880(m_150, RegisterFile__Wz(Machine__get_Regs(m_150)), lhs_18 & ~(1 << 2));
}, (m_151) => {
    Machine__Fetch(m_151);
    Machine__Fetch(m_151);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_151), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_151), R8.A) & ~4);
}, (m_152) => {
    Machine__Fetch(m_152);
    Machine__Fetch(m_152);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_152), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_152), R8.B) & ~8);
}, (m_153) => {
    Machine__Fetch(m_153);
    Machine__Fetch(m_153);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_153), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_153), R8.C) & ~8);
}, (m_154) => {
    Machine__Fetch(m_154);
    Machine__Fetch(m_154);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_154), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_154), R8.D) & ~8);
}, (m_155) => {
    Machine__Fetch(m_155);
    Machine__Fetch(m_155);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_155), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_155), R8.E) & ~8);
}, (m_156) => {
    Machine__Fetch(m_156);
    Machine__Fetch(m_156);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_156), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_156), R8.H) & ~8);
}, (m_157) => {
    Machine__Fetch(m_157);
    Machine__Fetch(m_157);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_157), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_157), R8.L) & ~8);
}, (m_158) => {
    Machine__Fetch(m_158);
    Machine__Fetch(m_158);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_158), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_158), R16.HL));
    const lhs_19 = Machine__Read_Z524259A4(m_158, RegisterFile__Wz(Machine__get_Regs(m_158))) | 0;
    Machine__PassTime_Z524259A4(m_158, 1);
    Machine__Write_Z37302880(m_158, RegisterFile__Wz(Machine__get_Regs(m_158)), lhs_19 & ~(1 << 3));
}, (m_159) => {
    Machine__Fetch(m_159);
    Machine__Fetch(m_159);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_159), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_159), R8.A) & ~8);
}, (m_160) => {
    Machine__Fetch(m_160);
    Machine__Fetch(m_160);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_160), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_160), R8.B) & ~16);
}, (m_161) => {
    Machine__Fetch(m_161);
    Machine__Fetch(m_161);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_161), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_161), R8.C) & ~16);
}, (m_162) => {
    Machine__Fetch(m_162);
    Machine__Fetch(m_162);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_162), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_162), R8.D) & ~16);
}, (m_163) => {
    Machine__Fetch(m_163);
    Machine__Fetch(m_163);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_163), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_163), R8.E) & ~16);
}, (m_164) => {
    Machine__Fetch(m_164);
    Machine__Fetch(m_164);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_164), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_164), R8.H) & ~16);
}, (m_165) => {
    Machine__Fetch(m_165);
    Machine__Fetch(m_165);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_165), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_165), R8.L) & ~16);
}, (m_166) => {
    Machine__Fetch(m_166);
    Machine__Fetch(m_166);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_166), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_166), R16.HL));
    const lhs_20 = Machine__Read_Z524259A4(m_166, RegisterFile__Wz(Machine__get_Regs(m_166))) | 0;
    Machine__PassTime_Z524259A4(m_166, 1);
    Machine__Write_Z37302880(m_166, RegisterFile__Wz(Machine__get_Regs(m_166)), lhs_20 & ~16);
}, (m_167) => {
    Machine__Fetch(m_167);
    Machine__Fetch(m_167);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_167), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_167), R8.A) & ~16);
}, (m_168) => {
    Machine__Fetch(m_168);
    Machine__Fetch(m_168);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_168), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_168), R8.B) & ~(1 << 5));
}, (m_169) => {
    Machine__Fetch(m_169);
    Machine__Fetch(m_169);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_169), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_169), R8.C) & ~32);
}, (m_170) => {
    Machine__Fetch(m_170);
    Machine__Fetch(m_170);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_170), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_170), R8.D) & ~32);
}, (m_171) => {
    Machine__Fetch(m_171);
    Machine__Fetch(m_171);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_171), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_171), R8.E) & ~32);
}, (m_172) => {
    Machine__Fetch(m_172);
    Machine__Fetch(m_172);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_172), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_172), R8.H) & ~32);
}, (m_173) => {
    Machine__Fetch(m_173);
    Machine__Fetch(m_173);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_173), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_173), R8.L) & ~32);
}, (m_174) => {
    Machine__Fetch(m_174);
    Machine__Fetch(m_174);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_174), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_174), R16.HL));
    const lhs_21 = Machine__Read_Z524259A4(m_174, RegisterFile__Wz(Machine__get_Regs(m_174))) | 0;
    Machine__PassTime_Z524259A4(m_174, 1);
    Machine__Write_Z37302880(m_174, RegisterFile__Wz(Machine__get_Regs(m_174)), lhs_21 & ~(1 << 5));
}, (m_175) => {
    Machine__Fetch(m_175);
    Machine__Fetch(m_175);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_175), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_175), R8.A) & ~32);
}, (m_176) => {
    Machine__Fetch(m_176);
    Machine__Fetch(m_176);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_176), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_176), R8.B) & ~64);
}, (m_177) => {
    Machine__Fetch(m_177);
    Machine__Fetch(m_177);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_177), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_177), R8.C) & ~(1 << 6));
}, (m_178) => {
    Machine__Fetch(m_178);
    Machine__Fetch(m_178);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_178), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_178), R8.D) & ~64);
}, (m_179) => {
    Machine__Fetch(m_179);
    Machine__Fetch(m_179);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_179), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_179), R8.E) & ~64);
}, (m_180) => {
    Machine__Fetch(m_180);
    Machine__Fetch(m_180);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_180), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_180), R8.H) & ~64);
}, (m_181) => {
    Machine__Fetch(m_181);
    Machine__Fetch(m_181);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_181), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_181), R8.L) & ~64);
}, (m_182) => {
    Machine__Fetch(m_182);
    Machine__Fetch(m_182);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_182), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_182), R16.HL));
    const lhs_22 = Machine__Read_Z524259A4(m_182, RegisterFile__Wz(Machine__get_Regs(m_182))) | 0;
    Machine__PassTime_Z524259A4(m_182, 1);
    Machine__Write_Z37302880(m_182, RegisterFile__Wz(Machine__get_Regs(m_182)), lhs_22 & ~(1 << 6));
}, (m_183) => {
    Machine__Fetch(m_183);
    Machine__Fetch(m_183);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_183), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_183), R8.A) & ~64);
}, (m_184) => {
    Machine__Fetch(m_184);
    Machine__Fetch(m_184);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_184), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_184), R8.B) & ~128);
}, (m_185) => {
    Machine__Fetch(m_185);
    Machine__Fetch(m_185);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_185), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_185), R8.C) & ~(1 << 7));
}, (m_186) => {
    Machine__Fetch(m_186);
    Machine__Fetch(m_186);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_186), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_186), R8.D) & ~128);
}, (m_187) => {
    Machine__Fetch(m_187);
    Machine__Fetch(m_187);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_187), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_187), R8.E) & ~128);
}, (m_188) => {
    Machine__Fetch(m_188);
    Machine__Fetch(m_188);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_188), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_188), R8.H) & ~(1 << 7));
}, (m_189) => {
    Machine__Fetch(m_189);
    Machine__Fetch(m_189);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_189), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_189), R8.L) & ~128);
}, (m_190) => {
    Machine__Fetch(m_190);
    Machine__Fetch(m_190);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_190), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_190), R16.HL));
    const lhs_23 = Machine__Read_Z524259A4(m_190, RegisterFile__Wz(Machine__get_Regs(m_190))) | 0;
    Machine__PassTime_Z524259A4(m_190, 1);
    Machine__Write_Z37302880(m_190, RegisterFile__Wz(Machine__get_Regs(m_190)), lhs_23 & ~(1 << 7));
}, (m_191) => {
    Machine__Fetch(m_191);
    Machine__Fetch(m_191);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_191), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_191), R8.A) & ~128);
}, (m_192) => {
    Machine__Fetch(m_192);
    Machine__Fetch(m_192);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_192), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_192), R8.B) | 1);
}, (m_193) => {
    Machine__Fetch(m_193);
    Machine__Fetch(m_193);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_193), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_193), R8.C) | 1);
}, (m_194) => {
    Machine__Fetch(m_194);
    Machine__Fetch(m_194);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_194), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_194), R8.D) | (1 << 0));
}, (m_195) => {
    Machine__Fetch(m_195);
    Machine__Fetch(m_195);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_195), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_195), R8.E) | 1);
}, (m_196) => {
    Machine__Fetch(m_196);
    Machine__Fetch(m_196);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_196), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_196), R8.H) | 1);
}, (m_197) => {
    Machine__Fetch(m_197);
    Machine__Fetch(m_197);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_197), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_197), R8.L) | 1);
}, (m_198) => {
    Machine__Fetch(m_198);
    Machine__Fetch(m_198);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_198), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_198), R16.HL));
    const lhs_24 = Machine__Read_Z524259A4(m_198, RegisterFile__Wz(Machine__get_Regs(m_198))) | 0;
    Machine__PassTime_Z524259A4(m_198, 1);
    Machine__Write_Z37302880(m_198, RegisterFile__Wz(Machine__get_Regs(m_198)), lhs_24 | (1 << 0));
}, (m_199) => {
    Machine__Fetch(m_199);
    Machine__Fetch(m_199);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_199), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_199), R8.A) | 1);
}, (m_200) => {
    Machine__Fetch(m_200);
    Machine__Fetch(m_200);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_200), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_200), R8.B) | 2);
}, (m_201) => {
    Machine__Fetch(m_201);
    Machine__Fetch(m_201);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_201), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_201), R8.C) | 2);
}, (m_202) => {
    Machine__Fetch(m_202);
    Machine__Fetch(m_202);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_202), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_202), R8.D) | (1 << 1));
}, (m_203) => {
    Machine__Fetch(m_203);
    Machine__Fetch(m_203);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_203), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_203), R8.E) | 2);
}, (m_204) => {
    Machine__Fetch(m_204);
    Machine__Fetch(m_204);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_204), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_204), R8.H) | 2);
}, (m_205) => {
    Machine__Fetch(m_205);
    Machine__Fetch(m_205);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_205), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_205), R8.L) | 2);
}, (m_206) => {
    Machine__Fetch(m_206);
    Machine__Fetch(m_206);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_206), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_206), R16.HL));
    const lhs_25 = Machine__Read_Z524259A4(m_206, RegisterFile__Wz(Machine__get_Regs(m_206))) | 0;
    Machine__PassTime_Z524259A4(m_206, 1);
    Machine__Write_Z37302880(m_206, RegisterFile__Wz(Machine__get_Regs(m_206)), lhs_25 | 2);
}, (m_207) => {
    Machine__Fetch(m_207);
    Machine__Fetch(m_207);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_207), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_207), R8.A) | 2);
}, (m_208) => {
    Machine__Fetch(m_208);
    Machine__Fetch(m_208);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_208), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_208), R8.B) | 4);
}, (m_209) => {
    Machine__Fetch(m_209);
    Machine__Fetch(m_209);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_209), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_209), R8.C) | 4);
}, (m_210) => {
    Machine__Fetch(m_210);
    Machine__Fetch(m_210);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_210), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_210), R8.D) | 4);
}, (m_211) => {
    Machine__Fetch(m_211);
    Machine__Fetch(m_211);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_211), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_211), R8.E) | (1 << 2));
}, (m_212) => {
    Machine__Fetch(m_212);
    Machine__Fetch(m_212);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_212), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_212), R8.H) | 4);
}, (m_213) => {
    Machine__Fetch(m_213);
    Machine__Fetch(m_213);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_213), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_213), R8.L) | 4);
}, (m_214) => {
    Machine__Fetch(m_214);
    Machine__Fetch(m_214);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_214), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_214), R16.HL));
    const lhs_26 = Machine__Read_Z524259A4(m_214, RegisterFile__Wz(Machine__get_Regs(m_214))) | 0;
    Machine__PassTime_Z524259A4(m_214, 1);
    Machine__Write_Z37302880(m_214, RegisterFile__Wz(Machine__get_Regs(m_214)), lhs_26 | (1 << 2));
}, (m_215) => {
    Machine__Fetch(m_215);
    Machine__Fetch(m_215);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_215), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_215), R8.A) | 4);
}, (m_216) => {
    Machine__Fetch(m_216);
    Machine__Fetch(m_216);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_216), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_216), R8.B) | 8);
}, (m_217) => {
    Machine__Fetch(m_217);
    Machine__Fetch(m_217);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_217), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_217), R8.C) | (1 << 3));
}, (m_218) => {
    Machine__Fetch(m_218);
    Machine__Fetch(m_218);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_218), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_218), R8.D) | 8);
}, (m_219) => {
    Machine__Fetch(m_219);
    Machine__Fetch(m_219);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_219), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_219), R8.E) | (1 << 3));
}, (m_220) => {
    Machine__Fetch(m_220);
    Machine__Fetch(m_220);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_220), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_220), R8.H) | 8);
}, (m_221) => {
    Machine__Fetch(m_221);
    Machine__Fetch(m_221);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_221), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_221), R8.L) | 8);
}, (m_222) => {
    Machine__Fetch(m_222);
    Machine__Fetch(m_222);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_222), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_222), R16.HL));
    const lhs_27 = Machine__Read_Z524259A4(m_222, RegisterFile__Wz(Machine__get_Regs(m_222))) | 0;
    Machine__PassTime_Z524259A4(m_222, 1);
    Machine__Write_Z37302880(m_222, RegisterFile__Wz(Machine__get_Regs(m_222)), lhs_27 | (1 << 3));
}, (m_223) => {
    Machine__Fetch(m_223);
    Machine__Fetch(m_223);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_223), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_223), R8.A) | 8);
}, (m_224) => {
    Machine__Fetch(m_224);
    Machine__Fetch(m_224);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_224), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_224), R8.B) | 16);
}, (m_225) => {
    Machine__Fetch(m_225);
    Machine__Fetch(m_225);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_225), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_225), R8.C) | 16);
}, (m_226) => {
    Machine__Fetch(m_226);
    Machine__Fetch(m_226);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_226), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_226), R8.D) | 16);
}, (m_227) => {
    Machine__Fetch(m_227);
    Machine__Fetch(m_227);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_227), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_227), R8.E) | (1 << 4));
}, (m_228) => {
    Machine__Fetch(m_228);
    Machine__Fetch(m_228);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_228), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_228), R8.H) | 16);
}, (m_229) => {
    Machine__Fetch(m_229);
    Machine__Fetch(m_229);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_229), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_229), R8.L) | 16);
}, (m_230) => {
    Machine__Fetch(m_230);
    Machine__Fetch(m_230);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_230), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_230), R16.HL));
    const lhs_28 = Machine__Read_Z524259A4(m_230, RegisterFile__Wz(Machine__get_Regs(m_230))) | 0;
    Machine__PassTime_Z524259A4(m_230, 1);
    Machine__Write_Z37302880(m_230, RegisterFile__Wz(Machine__get_Regs(m_230)), lhs_28 | 16);
}, (m_231) => {
    Machine__Fetch(m_231);
    Machine__Fetch(m_231);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_231), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_231), R8.A) | 16);
}, (m_232) => {
    Machine__Fetch(m_232);
    Machine__Fetch(m_232);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_232), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_232), R8.B) | (1 << 5));
}, (m_233) => {
    Machine__Fetch(m_233);
    Machine__Fetch(m_233);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_233), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_233), R8.C) | (1 << 5));
}, (m_234) => {
    Machine__Fetch(m_234);
    Machine__Fetch(m_234);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_234), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_234), R8.D) | 32);
}, (m_235) => {
    Machine__Fetch(m_235);
    Machine__Fetch(m_235);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_235), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_235), R8.E) | 32);
}, (m_236) => {
    Machine__Fetch(m_236);
    Machine__Fetch(m_236);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_236), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_236), R8.H) | 32);
}, (m_237) => {
    Machine__Fetch(m_237);
    Machine__Fetch(m_237);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_237), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_237), R8.L) | 32);
}, (m_238) => {
    Machine__Fetch(m_238);
    Machine__Fetch(m_238);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_238), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_238), R16.HL));
    const lhs_29 = Machine__Read_Z524259A4(m_238, RegisterFile__Wz(Machine__get_Regs(m_238))) | 0;
    Machine__PassTime_Z524259A4(m_238, 1);
    Machine__Write_Z37302880(m_238, RegisterFile__Wz(Machine__get_Regs(m_238)), lhs_29 | 32);
}, (m_239) => {
    Machine__Fetch(m_239);
    Machine__Fetch(m_239);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_239), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_239), R8.A) | 32);
}, (m_240) => {
    Machine__Fetch(m_240);
    Machine__Fetch(m_240);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_240), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_240), R8.B) | 64);
}, (m_241) => {
    Machine__Fetch(m_241);
    Machine__Fetch(m_241);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_241), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_241), R8.C) | (1 << 6));
}, (m_242) => {
    Machine__Fetch(m_242);
    Machine__Fetch(m_242);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_242), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_242), R8.D) | 64);
}, (m_243) => {
    Machine__Fetch(m_243);
    Machine__Fetch(m_243);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_243), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_243), R8.E) | (1 << 6));
}, (m_244) => {
    Machine__Fetch(m_244);
    Machine__Fetch(m_244);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_244), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_244), R8.H) | 64);
}, (m_245) => {
    Machine__Fetch(m_245);
    Machine__Fetch(m_245);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_245), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_245), R8.L) | 64);
}, (m_246) => {
    Machine__Fetch(m_246);
    Machine__Fetch(m_246);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_246), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_246), R16.HL));
    const lhs_30 = Machine__Read_Z524259A4(m_246, RegisterFile__Wz(Machine__get_Regs(m_246))) | 0;
    Machine__PassTime_Z524259A4(m_246, 1);
    Machine__Write_Z37302880(m_246, RegisterFile__Wz(Machine__get_Regs(m_246)), lhs_30 | (1 << 6));
}, (m_247) => {
    Machine__Fetch(m_247);
    Machine__Fetch(m_247);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_247), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_247), R8.A) | 64);
}, (m_248) => {
    Machine__Fetch(m_248);
    Machine__Fetch(m_248);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_248), R8.B, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_248), R8.B) | (1 << 7));
}, (m_249) => {
    Machine__Fetch(m_249);
    Machine__Fetch(m_249);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_249), R8.C, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_249), R8.C) | (1 << 7));
}, (m_250) => {
    Machine__Fetch(m_250);
    Machine__Fetch(m_250);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_250), R8.D, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_250), R8.D) | (1 << 7));
}, (m_251) => {
    Machine__Fetch(m_251);
    Machine__Fetch(m_251);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_251), R8.E, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_251), R8.E) | (1 << 7));
}, (m_252) => {
    Machine__Fetch(m_252);
    Machine__Fetch(m_252);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_252), R8.H, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_252), R8.H) | 128);
}, (m_253) => {
    Machine__Fetch(m_253);
    Machine__Fetch(m_253);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_253), R8.L, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_253), R8.L) | (1 << 7));
}, (m_254) => {
    Machine__Fetch(m_254);
    Machine__Fetch(m_254);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_254), RegisterFile__Get_Z61FD1070(Machine__get_Regs(m_254), R16.HL));
    const lhs_31 = Machine__Read_Z524259A4(m_254, RegisterFile__Wz(Machine__get_Regs(m_254))) | 0;
    Machine__PassTime_Z524259A4(m_254, 1);
    Machine__Write_Z37302880(m_254, RegisterFile__Wz(Machine__get_Regs(m_254)), lhs_31 | (1 << 7));
}, (m_255) => {
    Machine__Fetch(m_255);
    Machine__Fetch(m_255);
    RegisterFile__Set_33BF5693(Machine__get_Regs(m_255), R8.A, RegisterFile__Get_Z600F6D11(Machine__get_Regs(m_255), R8.A) | (1 << 7));
}];

export const dd_cb = [undefined, undefined, undefined, undefined, undefined, undefined, (m) => {
    Machine__Fetch(m);
    Machine__Fetch(m);
    const d = Machine__ReadImm(m) | 0;
    const d_1 = ((d >= 128) ? (d - 256) : d) | 0;
    Machine__PassTime_Z524259A4(m, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m), (RegisterFile__Ix(Machine__get_Regs(m)) + d_1) & 65535);
    Machine__Fetch(m);
    const lhs = Machine__Read_Z524259A4(m, RegisterFile__Wz(Machine__get_Regs(m))) | 0;
    Machine__PassTime_Z524259A4(m, 1);
    const patternInput = Alu_rotateCircular8(lhs, Alu_Direction.Left);
    Machine__Write_Z37302880(m, RegisterFile__Wz(Machine__get_Regs(m)), patternInput[0]);
    Machine__SetFlags_2901ED1A(m, patternInput[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_1) => {
    Machine__Fetch(m_1);
    Machine__Fetch(m_1);
    const d_2 = Machine__ReadImm(m_1) | 0;
    const d_3 = ((d_2 >= 128) ? (d_2 - 256) : d_2) | 0;
    Machine__PassTime_Z524259A4(m_1, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_1), (RegisterFile__Ix(Machine__get_Regs(m_1)) + d_3) & 65535);
    Machine__Fetch(m_1);
    const lhs_1 = Machine__Read_Z524259A4(m_1, RegisterFile__Wz(Machine__get_Regs(m_1))) | 0;
    Machine__PassTime_Z524259A4(m_1, 1);
    const patternInput_1 = Alu_rotateCircular8(lhs_1, Alu_Direction.Right);
    Machine__Write_Z37302880(m_1, RegisterFile__Wz(Machine__get_Regs(m_1)), patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_1, patternInput_1[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_2) => {
    let copyOfStruct;
    Machine__Fetch(m_2);
    Machine__Fetch(m_2);
    const d_4 = Machine__ReadImm(m_2) | 0;
    const d_5 = ((d_4 >= 128) ? (d_4 - 256) : d_4) | 0;
    Machine__PassTime_Z524259A4(m_2, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_2), (RegisterFile__Ix(Machine__get_Regs(m_2)) + d_5) & 65535);
    Machine__Fetch(m_2);
    const lhs_2 = Machine__Read_Z524259A4(m_2, RegisterFile__Wz(Machine__get_Regs(m_2))) | 0;
    Machine__PassTime_Z524259A4(m_2, 1);
    const patternInput_2 = Alu_rotate8(lhs_2, Alu_Direction.Left, (copyOfStruct = Machine__Flags(m_2), Flags__get_carry(copyOfStruct)));
    Machine__Write_Z37302880(m_2, RegisterFile__Wz(Machine__get_Regs(m_2)), patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_2, patternInput_2[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_3) => {
    let copyOfStruct_1;
    Machine__Fetch(m_3);
    Machine__Fetch(m_3);
    const d_6 = Machine__ReadImm(m_3) | 0;
    const d_7 = ((d_6 >= 128) ? (d_6 - 256) : d_6) | 0;
    Machine__PassTime_Z524259A4(m_3, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_3), (RegisterFile__Ix(Machine__get_Regs(m_3)) + d_7) & 65535);
    Machine__Fetch(m_3);
    const lhs_3 = Machine__Read_Z524259A4(m_3, RegisterFile__Wz(Machine__get_Regs(m_3))) | 0;
    Machine__PassTime_Z524259A4(m_3, 1);
    const patternInput_3 = Alu_rotate8(lhs_3, Alu_Direction.Right, (copyOfStruct_1 = Machine__Flags(m_3), Flags__get_carry(copyOfStruct_1)));
    Machine__Write_Z37302880(m_3, RegisterFile__Wz(Machine__get_Regs(m_3)), patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_3, patternInput_3[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_4) => {
    Machine__Fetch(m_4);
    Machine__Fetch(m_4);
    const d_8 = Machine__ReadImm(m_4) | 0;
    const d_9 = ((d_8 >= 128) ? (d_8 - 256) : d_8) | 0;
    Machine__PassTime_Z524259A4(m_4, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_4), (RegisterFile__Ix(Machine__get_Regs(m_4)) + d_9) & 65535);
    Machine__Fetch(m_4);
    const lhs_4 = Machine__Read_Z524259A4(m_4, RegisterFile__Wz(Machine__get_Regs(m_4))) | 0;
    Machine__PassTime_Z524259A4(m_4, 1);
    const patternInput_4 = Alu_shiftArithmetic8(lhs_4, Alu_Direction.Left);
    Machine__Write_Z37302880(m_4, RegisterFile__Wz(Machine__get_Regs(m_4)), patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_4, patternInput_4[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_5) => {
    Machine__Fetch(m_5);
    Machine__Fetch(m_5);
    const d_10 = Machine__ReadImm(m_5) | 0;
    const d_11 = ((d_10 >= 128) ? (d_10 - 256) : d_10) | 0;
    Machine__PassTime_Z524259A4(m_5, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_5), (RegisterFile__Ix(Machine__get_Regs(m_5)) + d_11) & 65535);
    Machine__Fetch(m_5);
    const lhs_5 = Machine__Read_Z524259A4(m_5, RegisterFile__Wz(Machine__get_Regs(m_5))) | 0;
    Machine__PassTime_Z524259A4(m_5, 1);
    const patternInput_5 = Alu_shiftArithmetic8(lhs_5, Alu_Direction.Right);
    Machine__Write_Z37302880(m_5, RegisterFile__Wz(Machine__get_Regs(m_5)), patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_5, patternInput_5[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_6) => {
    Machine__Fetch(m_6);
    Machine__Fetch(m_6);
    const d_12 = Machine__ReadImm(m_6) | 0;
    const d_13 = ((d_12 >= 128) ? (d_12 - 256) : d_12) | 0;
    Machine__PassTime_Z524259A4(m_6, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_6), (RegisterFile__Ix(Machine__get_Regs(m_6)) + d_13) & 65535);
    Machine__Fetch(m_6);
    const lhs_6 = Machine__Read_Z524259A4(m_6, RegisterFile__Wz(Machine__get_Regs(m_6))) | 0;
    Machine__PassTime_Z524259A4(m_6, 1);
    const patternInput_6 = Alu_shiftLogical8(lhs_6, Alu_Direction.Left);
    Machine__Write_Z37302880(m_6, RegisterFile__Wz(Machine__get_Regs(m_6)), patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_6, patternInput_6[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_7) => {
    Machine__Fetch(m_7);
    Machine__Fetch(m_7);
    const d_14 = Machine__ReadImm(m_7) | 0;
    const d_15 = ((d_14 >= 128) ? (d_14 - 256) : d_14) | 0;
    Machine__PassTime_Z524259A4(m_7, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_7), (RegisterFile__Ix(Machine__get_Regs(m_7)) + d_15) & 65535);
    Machine__Fetch(m_7);
    const lhs_7 = Machine__Read_Z524259A4(m_7, RegisterFile__Wz(Machine__get_Regs(m_7))) | 0;
    Machine__PassTime_Z524259A4(m_7, 1);
    const patternInput_7 = Alu_shiftLogical8(lhs_7, Alu_Direction.Right);
    Machine__Write_Z37302880(m_7, RegisterFile__Wz(Machine__get_Regs(m_7)), patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_7, patternInput_7[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_8) => {
    Machine__Fetch(m_8);
    Machine__Fetch(m_8);
    const d_16 = Machine__ReadImm(m_8) | 0;
    const d_17 = ((d_16 >= 128) ? (d_16 - 256) : d_16) | 0;
    Machine__PassTime_Z524259A4(m_8, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_8), (RegisterFile__Ix(Machine__get_Regs(m_8)) + d_17) & 65535);
    Machine__Fetch(m_8);
    const lhs_8 = Machine__Read_Z524259A4(m_8, RegisterFile__Wz(Machine__get_Regs(m_8))) | 0;
    Machine__PassTime_Z524259A4(m_8, 1);
    const busNoise = ((RegisterFile__Wz(Machine__get_Regs(m_8)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_8, Alu_bit(lhs_8, 1, Machine__Flags(m_8), busNoise));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_9) => {
    Machine__Fetch(m_9);
    Machine__Fetch(m_9);
    const d_18 = Machine__ReadImm(m_9) | 0;
    const d_19 = ((d_18 >= 128) ? (d_18 - 256) : d_18) | 0;
    Machine__PassTime_Z524259A4(m_9, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_9), (RegisterFile__Ix(Machine__get_Regs(m_9)) + d_19) & 65535);
    Machine__Fetch(m_9);
    const lhs_9 = Machine__Read_Z524259A4(m_9, RegisterFile__Wz(Machine__get_Regs(m_9))) | 0;
    Machine__PassTime_Z524259A4(m_9, 1);
    const busNoise_1 = ((RegisterFile__Wz(Machine__get_Regs(m_9)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_9, Alu_bit(lhs_9, 2, Machine__Flags(m_9), busNoise_1));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_10) => {
    Machine__Fetch(m_10);
    Machine__Fetch(m_10);
    const d_20 = Machine__ReadImm(m_10) | 0;
    const d_21 = ((d_20 >= 128) ? (d_20 - 256) : d_20) | 0;
    Machine__PassTime_Z524259A4(m_10, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_10), (RegisterFile__Ix(Machine__get_Regs(m_10)) + d_21) & 65535);
    Machine__Fetch(m_10);
    const lhs_10 = Machine__Read_Z524259A4(m_10, RegisterFile__Wz(Machine__get_Regs(m_10))) | 0;
    Machine__PassTime_Z524259A4(m_10, 1);
    const busNoise_2 = ((RegisterFile__Wz(Machine__get_Regs(m_10)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_10, Alu_bit(lhs_10, 4, Machine__Flags(m_10), busNoise_2));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_11) => {
    Machine__Fetch(m_11);
    Machine__Fetch(m_11);
    const d_22 = Machine__ReadImm(m_11) | 0;
    const d_23 = ((d_22 >= 128) ? (d_22 - 256) : d_22) | 0;
    Machine__PassTime_Z524259A4(m_11, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_11), (RegisterFile__Ix(Machine__get_Regs(m_11)) + d_23) & 65535);
    Machine__Fetch(m_11);
    const lhs_11 = Machine__Read_Z524259A4(m_11, RegisterFile__Wz(Machine__get_Regs(m_11))) | 0;
    Machine__PassTime_Z524259A4(m_11, 1);
    const busNoise_3 = ((RegisterFile__Wz(Machine__get_Regs(m_11)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_11, Alu_bit(lhs_11, 8, Machine__Flags(m_11), busNoise_3));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_12) => {
    Machine__Fetch(m_12);
    Machine__Fetch(m_12);
    const d_24 = Machine__ReadImm(m_12) | 0;
    const d_25 = ((d_24 >= 128) ? (d_24 - 256) : d_24) | 0;
    Machine__PassTime_Z524259A4(m_12, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_12), (RegisterFile__Ix(Machine__get_Regs(m_12)) + d_25) & 65535);
    Machine__Fetch(m_12);
    const lhs_12 = Machine__Read_Z524259A4(m_12, RegisterFile__Wz(Machine__get_Regs(m_12))) | 0;
    Machine__PassTime_Z524259A4(m_12, 1);
    const busNoise_4 = ((RegisterFile__Wz(Machine__get_Regs(m_12)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_12, Alu_bit(lhs_12, 16, Machine__Flags(m_12), busNoise_4));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_13) => {
    Machine__Fetch(m_13);
    Machine__Fetch(m_13);
    const d_26 = Machine__ReadImm(m_13) | 0;
    const d_27 = ((d_26 >= 128) ? (d_26 - 256) : d_26) | 0;
    Machine__PassTime_Z524259A4(m_13, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_13), (RegisterFile__Ix(Machine__get_Regs(m_13)) + d_27) & 65535);
    Machine__Fetch(m_13);
    const lhs_13 = Machine__Read_Z524259A4(m_13, RegisterFile__Wz(Machine__get_Regs(m_13))) | 0;
    Machine__PassTime_Z524259A4(m_13, 1);
    const busNoise_5 = ((RegisterFile__Wz(Machine__get_Regs(m_13)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_13, Alu_bit(lhs_13, 32, Machine__Flags(m_13), busNoise_5));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_14) => {
    Machine__Fetch(m_14);
    Machine__Fetch(m_14);
    const d_28 = Machine__ReadImm(m_14) | 0;
    const d_29 = ((d_28 >= 128) ? (d_28 - 256) : d_28) | 0;
    Machine__PassTime_Z524259A4(m_14, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_14), (RegisterFile__Ix(Machine__get_Regs(m_14)) + d_29) & 65535);
    Machine__Fetch(m_14);
    const lhs_14 = Machine__Read_Z524259A4(m_14, RegisterFile__Wz(Machine__get_Regs(m_14))) | 0;
    Machine__PassTime_Z524259A4(m_14, 1);
    const busNoise_6 = ((RegisterFile__Wz(Machine__get_Regs(m_14)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_14, Alu_bit(lhs_14, 1 << 6, Machine__Flags(m_14), busNoise_6));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_15) => {
    Machine__Fetch(m_15);
    Machine__Fetch(m_15);
    const d_30 = Machine__ReadImm(m_15) | 0;
    const d_31 = ((d_30 >= 128) ? (d_30 - 256) : d_30) | 0;
    Machine__PassTime_Z524259A4(m_15, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_15), (RegisterFile__Ix(Machine__get_Regs(m_15)) + d_31) & 65535);
    Machine__Fetch(m_15);
    const lhs_15 = Machine__Read_Z524259A4(m_15, RegisterFile__Wz(Machine__get_Regs(m_15))) | 0;
    Machine__PassTime_Z524259A4(m_15, 1);
    const busNoise_7 = ((RegisterFile__Wz(Machine__get_Regs(m_15)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_15, Alu_bit(lhs_15, 1 << 7, Machine__Flags(m_15), busNoise_7));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_16) => {
    Machine__Fetch(m_16);
    Machine__Fetch(m_16);
    const d_32 = Machine__ReadImm(m_16) | 0;
    const d_33 = ((d_32 >= 128) ? (d_32 - 256) : d_32) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_16), (RegisterFile__Ix(Machine__get_Regs(m_16)) + d_33) & 65535);
    Machine__Fetch(m_16);
    const lhs_16 = Machine__Read_Z524259A4(m_16, RegisterFile__Wz(Machine__get_Regs(m_16))) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    Machine__Write_Z37302880(m_16, RegisterFile__Wz(Machine__get_Regs(m_16)), lhs_16 & ~1);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_17) => {
    Machine__Fetch(m_17);
    Machine__Fetch(m_17);
    const d_34 = Machine__ReadImm(m_17) | 0;
    const d_35 = ((d_34 >= 128) ? (d_34 - 256) : d_34) | 0;
    Machine__PassTime_Z524259A4(m_17, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_17), (RegisterFile__Ix(Machine__get_Regs(m_17)) + d_35) & 65535);
    Machine__Fetch(m_17);
    const lhs_17 = Machine__Read_Z524259A4(m_17, RegisterFile__Wz(Machine__get_Regs(m_17))) | 0;
    Machine__PassTime_Z524259A4(m_17, 1);
    Machine__Write_Z37302880(m_17, RegisterFile__Wz(Machine__get_Regs(m_17)), lhs_17 & ~2);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_18) => {
    Machine__Fetch(m_18);
    Machine__Fetch(m_18);
    const d_36 = Machine__ReadImm(m_18) | 0;
    const d_37 = ((d_36 >= 128) ? (d_36 - 256) : d_36) | 0;
    Machine__PassTime_Z524259A4(m_18, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_18), (RegisterFile__Ix(Machine__get_Regs(m_18)) + d_37) & 65535);
    Machine__Fetch(m_18);
    const lhs_18 = Machine__Read_Z524259A4(m_18, RegisterFile__Wz(Machine__get_Regs(m_18))) | 0;
    Machine__PassTime_Z524259A4(m_18, 1);
    Machine__Write_Z37302880(m_18, RegisterFile__Wz(Machine__get_Regs(m_18)), lhs_18 & ~(1 << 2));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_19) => {
    Machine__Fetch(m_19);
    Machine__Fetch(m_19);
    const d_38 = Machine__ReadImm(m_19) | 0;
    const d_39 = ((d_38 >= 128) ? (d_38 - 256) : d_38) | 0;
    Machine__PassTime_Z524259A4(m_19, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_19), (RegisterFile__Ix(Machine__get_Regs(m_19)) + d_39) & 65535);
    Machine__Fetch(m_19);
    const lhs_19 = Machine__Read_Z524259A4(m_19, RegisterFile__Wz(Machine__get_Regs(m_19))) | 0;
    Machine__PassTime_Z524259A4(m_19, 1);
    Machine__Write_Z37302880(m_19, RegisterFile__Wz(Machine__get_Regs(m_19)), lhs_19 & ~8);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_20) => {
    Machine__Fetch(m_20);
    Machine__Fetch(m_20);
    const d_40 = Machine__ReadImm(m_20) | 0;
    const d_41 = ((d_40 >= 128) ? (d_40 - 256) : d_40) | 0;
    Machine__PassTime_Z524259A4(m_20, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_20), (RegisterFile__Ix(Machine__get_Regs(m_20)) + d_41) & 65535);
    Machine__Fetch(m_20);
    const lhs_20 = Machine__Read_Z524259A4(m_20, RegisterFile__Wz(Machine__get_Regs(m_20))) | 0;
    Machine__PassTime_Z524259A4(m_20, 1);
    Machine__Write_Z37302880(m_20, RegisterFile__Wz(Machine__get_Regs(m_20)), lhs_20 & ~16);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_21) => {
    Machine__Fetch(m_21);
    Machine__Fetch(m_21);
    const d_42 = Machine__ReadImm(m_21) | 0;
    const d_43 = ((d_42 >= 128) ? (d_42 - 256) : d_42) | 0;
    Machine__PassTime_Z524259A4(m_21, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_21), (RegisterFile__Ix(Machine__get_Regs(m_21)) + d_43) & 65535);
    Machine__Fetch(m_21);
    const lhs_21 = Machine__Read_Z524259A4(m_21, RegisterFile__Wz(Machine__get_Regs(m_21))) | 0;
    Machine__PassTime_Z524259A4(m_21, 1);
    Machine__Write_Z37302880(m_21, RegisterFile__Wz(Machine__get_Regs(m_21)), lhs_21 & ~32);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_22) => {
    Machine__Fetch(m_22);
    Machine__Fetch(m_22);
    const d_44 = Machine__ReadImm(m_22) | 0;
    const d_45 = ((d_44 >= 128) ? (d_44 - 256) : d_44) | 0;
    Machine__PassTime_Z524259A4(m_22, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_22), (RegisterFile__Ix(Machine__get_Regs(m_22)) + d_45) & 65535);
    Machine__Fetch(m_22);
    const lhs_22 = Machine__Read_Z524259A4(m_22, RegisterFile__Wz(Machine__get_Regs(m_22))) | 0;
    Machine__PassTime_Z524259A4(m_22, 1);
    Machine__Write_Z37302880(m_22, RegisterFile__Wz(Machine__get_Regs(m_22)), lhs_22 & ~(1 << 6));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_23) => {
    Machine__Fetch(m_23);
    Machine__Fetch(m_23);
    const d_46 = Machine__ReadImm(m_23) | 0;
    const d_47 = ((d_46 >= 128) ? (d_46 - 256) : d_46) | 0;
    Machine__PassTime_Z524259A4(m_23, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_23), (RegisterFile__Ix(Machine__get_Regs(m_23)) + d_47) & 65535);
    Machine__Fetch(m_23);
    const lhs_23 = Machine__Read_Z524259A4(m_23, RegisterFile__Wz(Machine__get_Regs(m_23))) | 0;
    Machine__PassTime_Z524259A4(m_23, 1);
    Machine__Write_Z37302880(m_23, RegisterFile__Wz(Machine__get_Regs(m_23)), lhs_23 & ~(1 << 7));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_24) => {
    Machine__Fetch(m_24);
    Machine__Fetch(m_24);
    const d_48 = Machine__ReadImm(m_24) | 0;
    const d_49 = ((d_48 >= 128) ? (d_48 - 256) : d_48) | 0;
    Machine__PassTime_Z524259A4(m_24, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_24), (RegisterFile__Ix(Machine__get_Regs(m_24)) + d_49) & 65535);
    Machine__Fetch(m_24);
    const lhs_24 = Machine__Read_Z524259A4(m_24, RegisterFile__Wz(Machine__get_Regs(m_24))) | 0;
    Machine__PassTime_Z524259A4(m_24, 1);
    Machine__Write_Z37302880(m_24, RegisterFile__Wz(Machine__get_Regs(m_24)), lhs_24 | 1);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_25) => {
    Machine__Fetch(m_25);
    Machine__Fetch(m_25);
    const d_50 = Machine__ReadImm(m_25) | 0;
    const d_51 = ((d_50 >= 128) ? (d_50 - 256) : d_50) | 0;
    Machine__PassTime_Z524259A4(m_25, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_25), (RegisterFile__Ix(Machine__get_Regs(m_25)) + d_51) & 65535);
    Machine__Fetch(m_25);
    const lhs_25 = Machine__Read_Z524259A4(m_25, RegisterFile__Wz(Machine__get_Regs(m_25))) | 0;
    Machine__PassTime_Z524259A4(m_25, 1);
    Machine__Write_Z37302880(m_25, RegisterFile__Wz(Machine__get_Regs(m_25)), lhs_25 | (1 << 1));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_26) => {
    Machine__Fetch(m_26);
    Machine__Fetch(m_26);
    const d_52 = Machine__ReadImm(m_26) | 0;
    const d_53 = ((d_52 >= 128) ? (d_52 - 256) : d_52) | 0;
    Machine__PassTime_Z524259A4(m_26, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_26), (RegisterFile__Ix(Machine__get_Regs(m_26)) + d_53) & 65535);
    Machine__Fetch(m_26);
    const lhs_26 = Machine__Read_Z524259A4(m_26, RegisterFile__Wz(Machine__get_Regs(m_26))) | 0;
    Machine__PassTime_Z524259A4(m_26, 1);
    Machine__Write_Z37302880(m_26, RegisterFile__Wz(Machine__get_Regs(m_26)), lhs_26 | (1 << 2));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_27) => {
    Machine__Fetch(m_27);
    Machine__Fetch(m_27);
    const d_54 = Machine__ReadImm(m_27) | 0;
    const d_55 = ((d_54 >= 128) ? (d_54 - 256) : d_54) | 0;
    Machine__PassTime_Z524259A4(m_27, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_27), (RegisterFile__Ix(Machine__get_Regs(m_27)) + d_55) & 65535);
    Machine__Fetch(m_27);
    const lhs_27 = Machine__Read_Z524259A4(m_27, RegisterFile__Wz(Machine__get_Regs(m_27))) | 0;
    Machine__PassTime_Z524259A4(m_27, 1);
    Machine__Write_Z37302880(m_27, RegisterFile__Wz(Machine__get_Regs(m_27)), lhs_27 | 8);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_28) => {
    Machine__Fetch(m_28);
    Machine__Fetch(m_28);
    const d_56 = Machine__ReadImm(m_28) | 0;
    const d_57 = ((d_56 >= 128) ? (d_56 - 256) : d_56) | 0;
    Machine__PassTime_Z524259A4(m_28, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_28), (RegisterFile__Ix(Machine__get_Regs(m_28)) + d_57) & 65535);
    Machine__Fetch(m_28);
    const lhs_28 = Machine__Read_Z524259A4(m_28, RegisterFile__Wz(Machine__get_Regs(m_28))) | 0;
    Machine__PassTime_Z524259A4(m_28, 1);
    Machine__Write_Z37302880(m_28, RegisterFile__Wz(Machine__get_Regs(m_28)), lhs_28 | 16);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_29) => {
    Machine__Fetch(m_29);
    Machine__Fetch(m_29);
    const d_58 = Machine__ReadImm(m_29) | 0;
    const d_59 = ((d_58 >= 128) ? (d_58 - 256) : d_58) | 0;
    Machine__PassTime_Z524259A4(m_29, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_29), (RegisterFile__Ix(Machine__get_Regs(m_29)) + d_59) & 65535);
    Machine__Fetch(m_29);
    const lhs_29 = Machine__Read_Z524259A4(m_29, RegisterFile__Wz(Machine__get_Regs(m_29))) | 0;
    Machine__PassTime_Z524259A4(m_29, 1);
    Machine__Write_Z37302880(m_29, RegisterFile__Wz(Machine__get_Regs(m_29)), lhs_29 | 32);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_30) => {
    Machine__Fetch(m_30);
    Machine__Fetch(m_30);
    const d_60 = Machine__ReadImm(m_30) | 0;
    const d_61 = ((d_60 >= 128) ? (d_60 - 256) : d_60) | 0;
    Machine__PassTime_Z524259A4(m_30, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_30), (RegisterFile__Ix(Machine__get_Regs(m_30)) + d_61) & 65535);
    Machine__Fetch(m_30);
    const lhs_30 = Machine__Read_Z524259A4(m_30, RegisterFile__Wz(Machine__get_Regs(m_30))) | 0;
    Machine__PassTime_Z524259A4(m_30, 1);
    Machine__Write_Z37302880(m_30, RegisterFile__Wz(Machine__get_Regs(m_30)), lhs_30 | (1 << 6));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_31) => {
    Machine__Fetch(m_31);
    Machine__Fetch(m_31);
    const d_62 = Machine__ReadImm(m_31) | 0;
    const d_63 = ((d_62 >= 128) ? (d_62 - 256) : d_62) | 0;
    Machine__PassTime_Z524259A4(m_31, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_31), (RegisterFile__Ix(Machine__get_Regs(m_31)) + d_63) & 65535);
    Machine__Fetch(m_31);
    const lhs_31 = Machine__Read_Z524259A4(m_31, RegisterFile__Wz(Machine__get_Regs(m_31))) | 0;
    Machine__PassTime_Z524259A4(m_31, 1);
    Machine__Write_Z37302880(m_31, RegisterFile__Wz(Machine__get_Regs(m_31)), lhs_31 | (1 << 7));
}, undefined];

export const fd_cb = [undefined, undefined, undefined, undefined, undefined, undefined, (m) => {
    Machine__Fetch(m);
    Machine__Fetch(m);
    const d = Machine__ReadImm(m) | 0;
    const d_1 = ((d >= 128) ? (d - 256) : d) | 0;
    Machine__PassTime_Z524259A4(m, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m), (RegisterFile__Iy(Machine__get_Regs(m)) + d_1) & 65535);
    Machine__Fetch(m);
    const lhs = Machine__Read_Z524259A4(m, RegisterFile__Wz(Machine__get_Regs(m))) | 0;
    Machine__PassTime_Z524259A4(m, 1);
    const patternInput = Alu_rotateCircular8(lhs, Alu_Direction.Left);
    Machine__Write_Z37302880(m, RegisterFile__Wz(Machine__get_Regs(m)), patternInput[0]);
    Machine__SetFlags_2901ED1A(m, patternInput[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_1) => {
    Machine__Fetch(m_1);
    Machine__Fetch(m_1);
    const d_2 = Machine__ReadImm(m_1) | 0;
    const d_3 = ((d_2 >= 128) ? (d_2 - 256) : d_2) | 0;
    Machine__PassTime_Z524259A4(m_1, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_1), (RegisterFile__Iy(Machine__get_Regs(m_1)) + d_3) & 65535);
    Machine__Fetch(m_1);
    const lhs_1 = Machine__Read_Z524259A4(m_1, RegisterFile__Wz(Machine__get_Regs(m_1))) | 0;
    Machine__PassTime_Z524259A4(m_1, 1);
    const patternInput_1 = Alu_rotateCircular8(lhs_1, Alu_Direction.Right);
    Machine__Write_Z37302880(m_1, RegisterFile__Wz(Machine__get_Regs(m_1)), patternInput_1[0]);
    Machine__SetFlags_2901ED1A(m_1, patternInput_1[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_2) => {
    let copyOfStruct;
    Machine__Fetch(m_2);
    Machine__Fetch(m_2);
    const d_4 = Machine__ReadImm(m_2) | 0;
    const d_5 = ((d_4 >= 128) ? (d_4 - 256) : d_4) | 0;
    Machine__PassTime_Z524259A4(m_2, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_2), (RegisterFile__Iy(Machine__get_Regs(m_2)) + d_5) & 65535);
    Machine__Fetch(m_2);
    const lhs_2 = Machine__Read_Z524259A4(m_2, RegisterFile__Wz(Machine__get_Regs(m_2))) | 0;
    Machine__PassTime_Z524259A4(m_2, 1);
    const patternInput_2 = Alu_rotate8(lhs_2, Alu_Direction.Left, (copyOfStruct = Machine__Flags(m_2), Flags__get_carry(copyOfStruct)));
    Machine__Write_Z37302880(m_2, RegisterFile__Wz(Machine__get_Regs(m_2)), patternInput_2[0]);
    Machine__SetFlags_2901ED1A(m_2, patternInput_2[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_3) => {
    let copyOfStruct_1;
    Machine__Fetch(m_3);
    Machine__Fetch(m_3);
    const d_6 = Machine__ReadImm(m_3) | 0;
    const d_7 = ((d_6 >= 128) ? (d_6 - 256) : d_6) | 0;
    Machine__PassTime_Z524259A4(m_3, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_3), (RegisterFile__Iy(Machine__get_Regs(m_3)) + d_7) & 65535);
    Machine__Fetch(m_3);
    const lhs_3 = Machine__Read_Z524259A4(m_3, RegisterFile__Wz(Machine__get_Regs(m_3))) | 0;
    Machine__PassTime_Z524259A4(m_3, 1);
    const patternInput_3 = Alu_rotate8(lhs_3, Alu_Direction.Right, (copyOfStruct_1 = Machine__Flags(m_3), Flags__get_carry(copyOfStruct_1)));
    Machine__Write_Z37302880(m_3, RegisterFile__Wz(Machine__get_Regs(m_3)), patternInput_3[0]);
    Machine__SetFlags_2901ED1A(m_3, patternInput_3[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_4) => {
    Machine__Fetch(m_4);
    Machine__Fetch(m_4);
    const d_8 = Machine__ReadImm(m_4) | 0;
    const d_9 = ((d_8 >= 128) ? (d_8 - 256) : d_8) | 0;
    Machine__PassTime_Z524259A4(m_4, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_4), (RegisterFile__Iy(Machine__get_Regs(m_4)) + d_9) & 65535);
    Machine__Fetch(m_4);
    const lhs_4 = Machine__Read_Z524259A4(m_4, RegisterFile__Wz(Machine__get_Regs(m_4))) | 0;
    Machine__PassTime_Z524259A4(m_4, 1);
    const patternInput_4 = Alu_shiftArithmetic8(lhs_4, Alu_Direction.Left);
    Machine__Write_Z37302880(m_4, RegisterFile__Wz(Machine__get_Regs(m_4)), patternInput_4[0]);
    Machine__SetFlags_2901ED1A(m_4, patternInput_4[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_5) => {
    Machine__Fetch(m_5);
    Machine__Fetch(m_5);
    const d_10 = Machine__ReadImm(m_5) | 0;
    const d_11 = ((d_10 >= 128) ? (d_10 - 256) : d_10) | 0;
    Machine__PassTime_Z524259A4(m_5, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_5), (RegisterFile__Iy(Machine__get_Regs(m_5)) + d_11) & 65535);
    Machine__Fetch(m_5);
    const lhs_5 = Machine__Read_Z524259A4(m_5, RegisterFile__Wz(Machine__get_Regs(m_5))) | 0;
    Machine__PassTime_Z524259A4(m_5, 1);
    const patternInput_5 = Alu_shiftArithmetic8(lhs_5, Alu_Direction.Right);
    Machine__Write_Z37302880(m_5, RegisterFile__Wz(Machine__get_Regs(m_5)), patternInput_5[0]);
    Machine__SetFlags_2901ED1A(m_5, patternInput_5[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_6) => {
    Machine__Fetch(m_6);
    Machine__Fetch(m_6);
    const d_12 = Machine__ReadImm(m_6) | 0;
    const d_13 = ((d_12 >= 128) ? (d_12 - 256) : d_12) | 0;
    Machine__PassTime_Z524259A4(m_6, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_6), (RegisterFile__Iy(Machine__get_Regs(m_6)) + d_13) & 65535);
    Machine__Fetch(m_6);
    const lhs_6 = Machine__Read_Z524259A4(m_6, RegisterFile__Wz(Machine__get_Regs(m_6))) | 0;
    Machine__PassTime_Z524259A4(m_6, 1);
    const patternInput_6 = Alu_shiftLogical8(lhs_6, Alu_Direction.Left);
    Machine__Write_Z37302880(m_6, RegisterFile__Wz(Machine__get_Regs(m_6)), patternInput_6[0]);
    Machine__SetFlags_2901ED1A(m_6, patternInput_6[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_7) => {
    Machine__Fetch(m_7);
    Machine__Fetch(m_7);
    const d_14 = Machine__ReadImm(m_7) | 0;
    const d_15 = ((d_14 >= 128) ? (d_14 - 256) : d_14) | 0;
    Machine__PassTime_Z524259A4(m_7, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_7), (RegisterFile__Iy(Machine__get_Regs(m_7)) + d_15) & 65535);
    Machine__Fetch(m_7);
    const lhs_7 = Machine__Read_Z524259A4(m_7, RegisterFile__Wz(Machine__get_Regs(m_7))) | 0;
    Machine__PassTime_Z524259A4(m_7, 1);
    const patternInput_7 = Alu_shiftLogical8(lhs_7, Alu_Direction.Right);
    Machine__Write_Z37302880(m_7, RegisterFile__Wz(Machine__get_Regs(m_7)), patternInput_7[0]);
    Machine__SetFlags_2901ED1A(m_7, patternInput_7[1]);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_8) => {
    Machine__Fetch(m_8);
    Machine__Fetch(m_8);
    const d_16 = Machine__ReadImm(m_8) | 0;
    const d_17 = ((d_16 >= 128) ? (d_16 - 256) : d_16) | 0;
    Machine__PassTime_Z524259A4(m_8, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_8), (RegisterFile__Iy(Machine__get_Regs(m_8)) + d_17) & 65535);
    Machine__Fetch(m_8);
    const lhs_8 = Machine__Read_Z524259A4(m_8, RegisterFile__Wz(Machine__get_Regs(m_8))) | 0;
    Machine__PassTime_Z524259A4(m_8, 1);
    const busNoise = ((RegisterFile__Wz(Machine__get_Regs(m_8)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_8, Alu_bit(lhs_8, 1 << 0, Machine__Flags(m_8), busNoise));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_9) => {
    Machine__Fetch(m_9);
    Machine__Fetch(m_9);
    const d_18 = Machine__ReadImm(m_9) | 0;
    const d_19 = ((d_18 >= 128) ? (d_18 - 256) : d_18) | 0;
    Machine__PassTime_Z524259A4(m_9, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_9), (RegisterFile__Iy(Machine__get_Regs(m_9)) + d_19) & 65535);
    Machine__Fetch(m_9);
    const lhs_9 = Machine__Read_Z524259A4(m_9, RegisterFile__Wz(Machine__get_Regs(m_9))) | 0;
    Machine__PassTime_Z524259A4(m_9, 1);
    const busNoise_1 = ((RegisterFile__Wz(Machine__get_Regs(m_9)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_9, Alu_bit(lhs_9, 1 << 1, Machine__Flags(m_9), busNoise_1));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_10) => {
    Machine__Fetch(m_10);
    Machine__Fetch(m_10);
    const d_20 = Machine__ReadImm(m_10) | 0;
    const d_21 = ((d_20 >= 128) ? (d_20 - 256) : d_20) | 0;
    Machine__PassTime_Z524259A4(m_10, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_10), (RegisterFile__Iy(Machine__get_Regs(m_10)) + d_21) & 65535);
    Machine__Fetch(m_10);
    const lhs_10 = Machine__Read_Z524259A4(m_10, RegisterFile__Wz(Machine__get_Regs(m_10))) | 0;
    Machine__PassTime_Z524259A4(m_10, 1);
    const busNoise_2 = ((RegisterFile__Wz(Machine__get_Regs(m_10)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_10, Alu_bit(lhs_10, 1 << 2, Machine__Flags(m_10), busNoise_2));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_11) => {
    Machine__Fetch(m_11);
    Machine__Fetch(m_11);
    const d_22 = Machine__ReadImm(m_11) | 0;
    const d_23 = ((d_22 >= 128) ? (d_22 - 256) : d_22) | 0;
    Machine__PassTime_Z524259A4(m_11, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_11), (RegisterFile__Iy(Machine__get_Regs(m_11)) + d_23) & 65535);
    Machine__Fetch(m_11);
    const lhs_11 = Machine__Read_Z524259A4(m_11, RegisterFile__Wz(Machine__get_Regs(m_11))) | 0;
    Machine__PassTime_Z524259A4(m_11, 1);
    const busNoise_3 = ((RegisterFile__Wz(Machine__get_Regs(m_11)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_11, Alu_bit(lhs_11, 1 << 3, Machine__Flags(m_11), busNoise_3));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_12) => {
    Machine__Fetch(m_12);
    Machine__Fetch(m_12);
    const d_24 = Machine__ReadImm(m_12) | 0;
    const d_25 = ((d_24 >= 128) ? (d_24 - 256) : d_24) | 0;
    Machine__PassTime_Z524259A4(m_12, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_12), (RegisterFile__Iy(Machine__get_Regs(m_12)) + d_25) & 65535);
    Machine__Fetch(m_12);
    const lhs_12 = Machine__Read_Z524259A4(m_12, RegisterFile__Wz(Machine__get_Regs(m_12))) | 0;
    Machine__PassTime_Z524259A4(m_12, 1);
    const busNoise_4 = ((RegisterFile__Wz(Machine__get_Regs(m_12)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_12, Alu_bit(lhs_12, 1 << 4, Machine__Flags(m_12), busNoise_4));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_13) => {
    Machine__Fetch(m_13);
    Machine__Fetch(m_13);
    const d_26 = Machine__ReadImm(m_13) | 0;
    const d_27 = ((d_26 >= 128) ? (d_26 - 256) : d_26) | 0;
    Machine__PassTime_Z524259A4(m_13, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_13), (RegisterFile__Iy(Machine__get_Regs(m_13)) + d_27) & 65535);
    Machine__Fetch(m_13);
    const lhs_13 = Machine__Read_Z524259A4(m_13, RegisterFile__Wz(Machine__get_Regs(m_13))) | 0;
    Machine__PassTime_Z524259A4(m_13, 1);
    const busNoise_5 = ((RegisterFile__Wz(Machine__get_Regs(m_13)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_13, Alu_bit(lhs_13, 1 << 5, Machine__Flags(m_13), busNoise_5));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_14) => {
    Machine__Fetch(m_14);
    Machine__Fetch(m_14);
    const d_28 = Machine__ReadImm(m_14) | 0;
    const d_29 = ((d_28 >= 128) ? (d_28 - 256) : d_28) | 0;
    Machine__PassTime_Z524259A4(m_14, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_14), (RegisterFile__Iy(Machine__get_Regs(m_14)) + d_29) & 65535);
    Machine__Fetch(m_14);
    const lhs_14 = Machine__Read_Z524259A4(m_14, RegisterFile__Wz(Machine__get_Regs(m_14))) | 0;
    Machine__PassTime_Z524259A4(m_14, 1);
    const busNoise_6 = ((RegisterFile__Wz(Machine__get_Regs(m_14)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_14, Alu_bit(lhs_14, 1 << 6, Machine__Flags(m_14), busNoise_6));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_15) => {
    Machine__Fetch(m_15);
    Machine__Fetch(m_15);
    const d_30 = Machine__ReadImm(m_15) | 0;
    const d_31 = ((d_30 >= 128) ? (d_30 - 256) : d_30) | 0;
    Machine__PassTime_Z524259A4(m_15, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_15), (RegisterFile__Iy(Machine__get_Regs(m_15)) + d_31) & 65535);
    Machine__Fetch(m_15);
    const lhs_15 = Machine__Read_Z524259A4(m_15, RegisterFile__Wz(Machine__get_Regs(m_15))) | 0;
    Machine__PassTime_Z524259A4(m_15, 1);
    const busNoise_7 = ((RegisterFile__Wz(Machine__get_Regs(m_15)) >> 8) & 255) | 0;
    Machine__SetFlags_2901ED1A(m_15, Alu_bit(lhs_15, 1 << 7, Machine__Flags(m_15), busNoise_7));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_16) => {
    Machine__Fetch(m_16);
    Machine__Fetch(m_16);
    const d_32 = Machine__ReadImm(m_16) | 0;
    const d_33 = ((d_32 >= 128) ? (d_32 - 256) : d_32) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_16), (RegisterFile__Iy(Machine__get_Regs(m_16)) + d_33) & 65535);
    Machine__Fetch(m_16);
    const lhs_16 = Machine__Read_Z524259A4(m_16, RegisterFile__Wz(Machine__get_Regs(m_16))) | 0;
    Machine__PassTime_Z524259A4(m_16, 1);
    Machine__Write_Z37302880(m_16, RegisterFile__Wz(Machine__get_Regs(m_16)), lhs_16 & ~(1 << 0));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_17) => {
    Machine__Fetch(m_17);
    Machine__Fetch(m_17);
    const d_34 = Machine__ReadImm(m_17) | 0;
    const d_35 = ((d_34 >= 128) ? (d_34 - 256) : d_34) | 0;
    Machine__PassTime_Z524259A4(m_17, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_17), (RegisterFile__Iy(Machine__get_Regs(m_17)) + d_35) & 65535);
    Machine__Fetch(m_17);
    const lhs_17 = Machine__Read_Z524259A4(m_17, RegisterFile__Wz(Machine__get_Regs(m_17))) | 0;
    Machine__PassTime_Z524259A4(m_17, 1);
    Machine__Write_Z37302880(m_17, RegisterFile__Wz(Machine__get_Regs(m_17)), lhs_17 & ~(1 << 1));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_18) => {
    Machine__Fetch(m_18);
    Machine__Fetch(m_18);
    const d_36 = Machine__ReadImm(m_18) | 0;
    const d_37 = ((d_36 >= 128) ? (d_36 - 256) : d_36) | 0;
    Machine__PassTime_Z524259A4(m_18, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_18), (RegisterFile__Iy(Machine__get_Regs(m_18)) + d_37) & 65535);
    Machine__Fetch(m_18);
    const lhs_18 = Machine__Read_Z524259A4(m_18, RegisterFile__Wz(Machine__get_Regs(m_18))) | 0;
    Machine__PassTime_Z524259A4(m_18, 1);
    Machine__Write_Z37302880(m_18, RegisterFile__Wz(Machine__get_Regs(m_18)), lhs_18 & ~(1 << 2));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_19) => {
    Machine__Fetch(m_19);
    Machine__Fetch(m_19);
    const d_38 = Machine__ReadImm(m_19) | 0;
    const d_39 = ((d_38 >= 128) ? (d_38 - 256) : d_38) | 0;
    Machine__PassTime_Z524259A4(m_19, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_19), (RegisterFile__Iy(Machine__get_Regs(m_19)) + d_39) & 65535);
    Machine__Fetch(m_19);
    const lhs_19 = Machine__Read_Z524259A4(m_19, RegisterFile__Wz(Machine__get_Regs(m_19))) | 0;
    Machine__PassTime_Z524259A4(m_19, 1);
    Machine__Write_Z37302880(m_19, RegisterFile__Wz(Machine__get_Regs(m_19)), lhs_19 & ~8);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_20) => {
    Machine__Fetch(m_20);
    Machine__Fetch(m_20);
    const d_40 = Machine__ReadImm(m_20) | 0;
    const d_41 = ((d_40 >= 128) ? (d_40 - 256) : d_40) | 0;
    Machine__PassTime_Z524259A4(m_20, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_20), (RegisterFile__Iy(Machine__get_Regs(m_20)) + d_41) & 65535);
    Machine__Fetch(m_20);
    const lhs_20 = Machine__Read_Z524259A4(m_20, RegisterFile__Wz(Machine__get_Regs(m_20))) | 0;
    Machine__PassTime_Z524259A4(m_20, 1);
    Machine__Write_Z37302880(m_20, RegisterFile__Wz(Machine__get_Regs(m_20)), lhs_20 & ~(1 << 4));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_21) => {
    Machine__Fetch(m_21);
    Machine__Fetch(m_21);
    const d_42 = Machine__ReadImm(m_21) | 0;
    const d_43 = ((d_42 >= 128) ? (d_42 - 256) : d_42) | 0;
    Machine__PassTime_Z524259A4(m_21, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_21), (RegisterFile__Iy(Machine__get_Regs(m_21)) + d_43) & 65535);
    Machine__Fetch(m_21);
    const lhs_21 = Machine__Read_Z524259A4(m_21, RegisterFile__Wz(Machine__get_Regs(m_21))) | 0;
    Machine__PassTime_Z524259A4(m_21, 1);
    Machine__Write_Z37302880(m_21, RegisterFile__Wz(Machine__get_Regs(m_21)), lhs_21 & ~32);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_22) => {
    Machine__Fetch(m_22);
    Machine__Fetch(m_22);
    const d_44 = Machine__ReadImm(m_22) | 0;
    const d_45 = ((d_44 >= 128) ? (d_44 - 256) : d_44) | 0;
    Machine__PassTime_Z524259A4(m_22, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_22), (RegisterFile__Iy(Machine__get_Regs(m_22)) + d_45) & 65535);
    Machine__Fetch(m_22);
    const lhs_22 = Machine__Read_Z524259A4(m_22, RegisterFile__Wz(Machine__get_Regs(m_22))) | 0;
    Machine__PassTime_Z524259A4(m_22, 1);
    Machine__Write_Z37302880(m_22, RegisterFile__Wz(Machine__get_Regs(m_22)), lhs_22 & ~(1 << 6));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_23) => {
    Machine__Fetch(m_23);
    Machine__Fetch(m_23);
    const d_46 = Machine__ReadImm(m_23) | 0;
    const d_47 = ((d_46 >= 128) ? (d_46 - 256) : d_46) | 0;
    Machine__PassTime_Z524259A4(m_23, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_23), (RegisterFile__Iy(Machine__get_Regs(m_23)) + d_47) & 65535);
    Machine__Fetch(m_23);
    const lhs_23 = Machine__Read_Z524259A4(m_23, RegisterFile__Wz(Machine__get_Regs(m_23))) | 0;
    Machine__PassTime_Z524259A4(m_23, 1);
    Machine__Write_Z37302880(m_23, RegisterFile__Wz(Machine__get_Regs(m_23)), lhs_23 & ~(1 << 7));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_24) => {
    Machine__Fetch(m_24);
    Machine__Fetch(m_24);
    const d_48 = Machine__ReadImm(m_24) | 0;
    const d_49 = ((d_48 >= 128) ? (d_48 - 256) : d_48) | 0;
    Machine__PassTime_Z524259A4(m_24, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_24), (RegisterFile__Iy(Machine__get_Regs(m_24)) + d_49) & 65535);
    Machine__Fetch(m_24);
    const lhs_24 = Machine__Read_Z524259A4(m_24, RegisterFile__Wz(Machine__get_Regs(m_24))) | 0;
    Machine__PassTime_Z524259A4(m_24, 1);
    Machine__Write_Z37302880(m_24, RegisterFile__Wz(Machine__get_Regs(m_24)), lhs_24 | (1 << 0));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_25) => {
    Machine__Fetch(m_25);
    Machine__Fetch(m_25);
    const d_50 = Machine__ReadImm(m_25) | 0;
    const d_51 = ((d_50 >= 128) ? (d_50 - 256) : d_50) | 0;
    Machine__PassTime_Z524259A4(m_25, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_25), (RegisterFile__Iy(Machine__get_Regs(m_25)) + d_51) & 65535);
    Machine__Fetch(m_25);
    const lhs_25 = Machine__Read_Z524259A4(m_25, RegisterFile__Wz(Machine__get_Regs(m_25))) | 0;
    Machine__PassTime_Z524259A4(m_25, 1);
    Machine__Write_Z37302880(m_25, RegisterFile__Wz(Machine__get_Regs(m_25)), lhs_25 | (1 << 1));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_26) => {
    Machine__Fetch(m_26);
    Machine__Fetch(m_26);
    const d_52 = Machine__ReadImm(m_26) | 0;
    const d_53 = ((d_52 >= 128) ? (d_52 - 256) : d_52) | 0;
    Machine__PassTime_Z524259A4(m_26, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_26), (RegisterFile__Iy(Machine__get_Regs(m_26)) + d_53) & 65535);
    Machine__Fetch(m_26);
    const lhs_26 = Machine__Read_Z524259A4(m_26, RegisterFile__Wz(Machine__get_Regs(m_26))) | 0;
    Machine__PassTime_Z524259A4(m_26, 1);
    Machine__Write_Z37302880(m_26, RegisterFile__Wz(Machine__get_Regs(m_26)), lhs_26 | (1 << 2));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_27) => {
    Machine__Fetch(m_27);
    Machine__Fetch(m_27);
    const d_54 = Machine__ReadImm(m_27) | 0;
    const d_55 = ((d_54 >= 128) ? (d_54 - 256) : d_54) | 0;
    Machine__PassTime_Z524259A4(m_27, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_27), (RegisterFile__Iy(Machine__get_Regs(m_27)) + d_55) & 65535);
    Machine__Fetch(m_27);
    const lhs_27 = Machine__Read_Z524259A4(m_27, RegisterFile__Wz(Machine__get_Regs(m_27))) | 0;
    Machine__PassTime_Z524259A4(m_27, 1);
    Machine__Write_Z37302880(m_27, RegisterFile__Wz(Machine__get_Regs(m_27)), lhs_27 | (1 << 3));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_28) => {
    Machine__Fetch(m_28);
    Machine__Fetch(m_28);
    const d_56 = Machine__ReadImm(m_28) | 0;
    const d_57 = ((d_56 >= 128) ? (d_56 - 256) : d_56) | 0;
    Machine__PassTime_Z524259A4(m_28, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_28), (RegisterFile__Iy(Machine__get_Regs(m_28)) + d_57) & 65535);
    Machine__Fetch(m_28);
    const lhs_28 = Machine__Read_Z524259A4(m_28, RegisterFile__Wz(Machine__get_Regs(m_28))) | 0;
    Machine__PassTime_Z524259A4(m_28, 1);
    Machine__Write_Z37302880(m_28, RegisterFile__Wz(Machine__get_Regs(m_28)), lhs_28 | 16);
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_29) => {
    Machine__Fetch(m_29);
    Machine__Fetch(m_29);
    const d_58 = Machine__ReadImm(m_29) | 0;
    const d_59 = ((d_58 >= 128) ? (d_58 - 256) : d_58) | 0;
    Machine__PassTime_Z524259A4(m_29, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_29), (RegisterFile__Iy(Machine__get_Regs(m_29)) + d_59) & 65535);
    Machine__Fetch(m_29);
    const lhs_29 = Machine__Read_Z524259A4(m_29, RegisterFile__Wz(Machine__get_Regs(m_29))) | 0;
    Machine__PassTime_Z524259A4(m_29, 1);
    Machine__Write_Z37302880(m_29, RegisterFile__Wz(Machine__get_Regs(m_29)), lhs_29 | (1 << 5));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_30) => {
    Machine__Fetch(m_30);
    Machine__Fetch(m_30);
    const d_60 = Machine__ReadImm(m_30) | 0;
    const d_61 = ((d_60 >= 128) ? (d_60 - 256) : d_60) | 0;
    Machine__PassTime_Z524259A4(m_30, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_30), (RegisterFile__Iy(Machine__get_Regs(m_30)) + d_61) & 65535);
    Machine__Fetch(m_30);
    const lhs_30 = Machine__Read_Z524259A4(m_30, RegisterFile__Wz(Machine__get_Regs(m_30))) | 0;
    Machine__PassTime_Z524259A4(m_30, 1);
    Machine__Write_Z37302880(m_30, RegisterFile__Wz(Machine__get_Regs(m_30)), lhs_30 | (1 << 6));
}, undefined, undefined, undefined, undefined, undefined, undefined, undefined, (m_31) => {
    Machine__Fetch(m_31);
    Machine__Fetch(m_31);
    const d_62 = Machine__ReadImm(m_31) | 0;
    const d_63 = ((d_62 >= 128) ? (d_62 - 256) : d_62) | 0;
    Machine__PassTime_Z524259A4(m_31, 1);
    RegisterFile__SetWz_Z524259A4(Machine__get_Regs(m_31), (RegisterFile__Iy(Machine__get_Regs(m_31)) + d_63) & 65535);
    Machine__Fetch(m_31);
    const lhs_31 = Machine__Read_Z524259A4(m_31, RegisterFile__Wz(Machine__get_Regs(m_31))) | 0;
    Machine__PassTime_Z524259A4(m_31, 1);
    Machine__Write_Z37302880(m_31, RegisterFile__Wz(Machine__get_Regs(m_31)), lhs_31 | (1 << 7));
}, undefined];

export function step(m) {
    const pc = RegisterFile__Pc(Machine__get_Regs(m)) | 0;
    const op = ~~item(pc & 65535, Machine__get_Memory(m)) | 0;
    const run = (table, idx) => {
        const matchValue = item(idx, table);
        if (matchValue == null) {
            toFail(printf("no code at 0x%04X"))(pc);
        }
        else {
            matchValue(m);
        }
    };
    switch (op) {
        case 203: {
            run(cb, ~~item((pc + 1) & 65535, Machine__get_Memory(m)));
            break;
        }
        case 221: {
            const second = ~~item((pc + 1) & 65535, Machine__get_Memory(m)) | 0;
            switch (second) {
                case 203: {
                    run(dd_cb, ~~item((pc + 3) & 65535, Machine__get_Memory(m)));
                    break;
                }
                case 221:
                case 237:
                case 253: {
                    run(dd, second);
                    break;
                }
                default:
                    run(dd, second);
            }
            break;
        }
        case 237: {
            run(ed, ~~item((pc + 1) & 65535, Machine__get_Memory(m)));
            break;
        }
        case 253: {
            const second_1 = ~~item((pc + 1) & 65535, Machine__get_Memory(m)) | 0;
            switch (second_1) {
                case 203: {
                    run(fd_cb, ~~item((pc + 3) & 65535, Machine__get_Memory(m)));
                    break;
                }
                case 221:
                case 237:
                case 253: {
                    run(fd, second_1);
                    break;
                }
                default:
                    run(fd, second_1);
            }
            break;
        }
        default:
            run(main, op);
    }
}

/**
 * Install the generic step into Machine (replaces the per-address pages).
 */
export function EnsureInstalled() {
    Machine_set_GeneratedStep_5007B66A((m) => {
        step(m);
    });
}

