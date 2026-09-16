
import { Z80_runOf, Z80_mkOp } from "./Z80Asm.js";
import { fd_cb, dd_cb, cb, ed, fd, dd, main } from "./Z80Table.js";
import { ofArray } from "../fable_modules/fable-library-js.5.17.2/List.js";

export const LD_B_B = Z80_mkOp("LD B,B", new Uint8Array([64]), Z80_runOf(main, 64));

export const LD_B_C = Z80_mkOp("LD B,C", new Uint8Array([65]), Z80_runOf(main, 65));

export const LD_B_D = Z80_mkOp("LD B,D", new Uint8Array([66]), Z80_runOf(main, 66));

export const LD_B_E = Z80_mkOp("LD B,E", new Uint8Array([67]), Z80_runOf(main, 67));

export const LD_B_H = Z80_mkOp("LD B,H", new Uint8Array([68]), Z80_runOf(main, 68));

export const LD_B_L = Z80_mkOp("LD B,L", new Uint8Array([69]), Z80_runOf(main, 69));

export const LD_B_A = Z80_mkOp("LD B,A", new Uint8Array([71]), Z80_runOf(main, 71));

export const LD_C_B = Z80_mkOp("LD C,B", new Uint8Array([72]), Z80_runOf(main, 72));

export const LD_C_C = Z80_mkOp("LD C,C", new Uint8Array([73]), Z80_runOf(main, 73));

export const LD_C_D = Z80_mkOp("LD C,D", new Uint8Array([74]), Z80_runOf(main, 74));

export const LD_C_E = Z80_mkOp("LD C,E", new Uint8Array([75]), Z80_runOf(main, 75));

export const LD_C_H = Z80_mkOp("LD C,H", new Uint8Array([76]), Z80_runOf(main, 76));

export const LD_C_L = Z80_mkOp("LD C,L", new Uint8Array([77]), Z80_runOf(main, 77));

export const LD_C_A = Z80_mkOp("LD C,A", new Uint8Array([79]), Z80_runOf(main, 79));

export const LD_D_B = Z80_mkOp("LD D,B", new Uint8Array([80]), Z80_runOf(main, 80));

export const LD_D_C = Z80_mkOp("LD D,C", new Uint8Array([81]), Z80_runOf(main, 81));

export const LD_D_D = Z80_mkOp("LD D,D", new Uint8Array([82]), Z80_runOf(main, 82));

export const LD_D_E = Z80_mkOp("LD D,E", new Uint8Array([83]), Z80_runOf(main, 83));

export const LD_D_H = Z80_mkOp("LD D,H", new Uint8Array([84]), Z80_runOf(main, 84));

export const LD_D_L = Z80_mkOp("LD D,L", new Uint8Array([85]), Z80_runOf(main, 85));

export const LD_D_A = Z80_mkOp("LD D,A", new Uint8Array([87]), Z80_runOf(main, 87));

export const LD_E_B = Z80_mkOp("LD E,B", new Uint8Array([88]), Z80_runOf(main, 88));

export const LD_E_C = Z80_mkOp("LD E,C", new Uint8Array([89]), Z80_runOf(main, 89));

export const LD_E_D = Z80_mkOp("LD E,D", new Uint8Array([90]), Z80_runOf(main, 90));

export const LD_E_E = Z80_mkOp("LD E,E", new Uint8Array([91]), Z80_runOf(main, 91));

export const LD_E_H = Z80_mkOp("LD E,H", new Uint8Array([92]), Z80_runOf(main, 92));

export const LD_E_L = Z80_mkOp("LD E,L", new Uint8Array([93]), Z80_runOf(main, 93));

export const LD_E_A = Z80_mkOp("LD E,A", new Uint8Array([95]), Z80_runOf(main, 95));

export const LD_H_B = Z80_mkOp("LD H,B", new Uint8Array([96]), Z80_runOf(main, 96));

export const LD_H_C = Z80_mkOp("LD H,C", new Uint8Array([97]), Z80_runOf(main, 97));

export const LD_H_D = Z80_mkOp("LD H,D", new Uint8Array([98]), Z80_runOf(main, 98));

export const LD_H_E = Z80_mkOp("LD H,E", new Uint8Array([99]), Z80_runOf(main, 99));

export const LD_H_H = Z80_mkOp("LD H,H", new Uint8Array([100]), Z80_runOf(main, 100));

export const LD_H_L = Z80_mkOp("LD H,L", new Uint8Array([101]), Z80_runOf(main, 101));

export const LD_H_A = Z80_mkOp("LD H,A", new Uint8Array([103]), Z80_runOf(main, 103));

export const LD_L_B = Z80_mkOp("LD L,B", new Uint8Array([104]), Z80_runOf(main, 104));

export const LD_L_C = Z80_mkOp("LD L,C", new Uint8Array([105]), Z80_runOf(main, 105));

export const LD_L_D = Z80_mkOp("LD L,D", new Uint8Array([106]), Z80_runOf(main, 106));

export const LD_L_E = Z80_mkOp("LD L,E", new Uint8Array([107]), Z80_runOf(main, 107));

export const LD_L_H = Z80_mkOp("LD L,H", new Uint8Array([108]), Z80_runOf(main, 108));

export const LD_L_L = Z80_mkOp("LD L,L", new Uint8Array([109]), Z80_runOf(main, 109));

export const LD_L_A = Z80_mkOp("LD L,A", new Uint8Array([111]), Z80_runOf(main, 111));

export const LD_A_B = Z80_mkOp("LD A,B", new Uint8Array([120]), Z80_runOf(main, 120));

export const LD_A_C = Z80_mkOp("LD A,C", new Uint8Array([121]), Z80_runOf(main, 121));

export const LD_A_D = Z80_mkOp("LD A,D", new Uint8Array([122]), Z80_runOf(main, 122));

export const LD_A_E = Z80_mkOp("LD A,E", new Uint8Array([123]), Z80_runOf(main, 123));

export const LD_A_H = Z80_mkOp("LD A,H", new Uint8Array([124]), Z80_runOf(main, 124));

export const LD_A_L = Z80_mkOp("LD A,L", new Uint8Array([125]), Z80_runOf(main, 125));

export const LD_A_A = Z80_mkOp("LD A,A", new Uint8Array([127]), Z80_runOf(main, 127));

export const LD_B_PTR_HL = Z80_mkOp("LD B,(HL)", new Uint8Array([70]), Z80_runOf(main, 70));

export const LD_C_PTR_HL = Z80_mkOp("LD C,(HL)", new Uint8Array([78]), Z80_runOf(main, 78));

export const LD_D_PTR_HL = Z80_mkOp("LD D,(HL)", new Uint8Array([86]), Z80_runOf(main, 86));

export const LD_E_PTR_HL = Z80_mkOp("LD E,(HL)", new Uint8Array([94]), Z80_runOf(main, 94));

export const LD_H_PTR_HL = Z80_mkOp("LD H,(HL)", new Uint8Array([102]), Z80_runOf(main, 102));

export const LD_L_PTR_HL = Z80_mkOp("LD L,(HL)", new Uint8Array([110]), Z80_runOf(main, 110));

export const LD_A_PTR_HL = Z80_mkOp("LD A,(HL)", new Uint8Array([126]), Z80_runOf(main, 126));

export const LD_PTR_HL_B = Z80_mkOp("LD (HL),B", new Uint8Array([112]), Z80_runOf(main, 112));

export const LD_PTR_HL_C = Z80_mkOp("LD (HL),C", new Uint8Array([113]), Z80_runOf(main, 113));

export const LD_PTR_HL_D = Z80_mkOp("LD (HL),D", new Uint8Array([114]), Z80_runOf(main, 114));

export const LD_PTR_HL_E = Z80_mkOp("LD (HL),E", new Uint8Array([115]), Z80_runOf(main, 115));

export const LD_PTR_HL_H = Z80_mkOp("LD (HL),H", new Uint8Array([116]), Z80_runOf(main, 116));

export const LD_PTR_HL_L = Z80_mkOp("LD (HL),L", new Uint8Array([117]), Z80_runOf(main, 117));

export const LD_PTR_HL_A = Z80_mkOp("LD (HL),A", new Uint8Array([119]), Z80_runOf(main, 119));

export function LD_B(n) {
    return Z80_mkOp("LD B,n", new Uint8Array([6, (n & 255) & 0xFF]), Z80_runOf(main, 6));
}

export function LD_C(n) {
    return Z80_mkOp("LD C,n", new Uint8Array([14, (n & 255) & 0xFF]), Z80_runOf(main, 14));
}

export function LD_D(n) {
    return Z80_mkOp("LD D,n", new Uint8Array([22, (n & 255) & 0xFF]), Z80_runOf(main, 22));
}

export function LD_E(n) {
    return Z80_mkOp("LD E,n", new Uint8Array([30, (n & 255) & 0xFF]), Z80_runOf(main, 30));
}

export function LD_H(n) {
    return Z80_mkOp("LD H,n", new Uint8Array([38, (n & 255) & 0xFF]), Z80_runOf(main, 38));
}

export function LD_L(n) {
    return Z80_mkOp("LD L,n", new Uint8Array([46, (n & 255) & 0xFF]), Z80_runOf(main, 46));
}

export function LD_A(n) {
    return Z80_mkOp("LD A,n", new Uint8Array([62, (n & 255) & 0xFF]), Z80_runOf(main, 62));
}

export function LD_A_ptr(nn) {
    return Z80_mkOp("LD A,(nn)", new Uint8Array([58, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 58));
}

export function LD_ptr_A(nn) {
    return Z80_mkOp("LD (nn),A", new Uint8Array([50, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 50));
}

export const LD_A_BC = Z80_mkOp("LD A,(BC)", new Uint8Array([10]), Z80_runOf(main, 10));

export const LD_A_DE = Z80_mkOp("LD A,(DE)", new Uint8Array([26]), Z80_runOf(main, 26));

export const LD_BC_A = Z80_mkOp("LD (BC),A", new Uint8Array([2]), Z80_runOf(main, 2));

export const LD_DE_A = Z80_mkOp("LD (DE),A", new Uint8Array([18]), Z80_runOf(main, 18));

export function LD_BC(imm) {
    return Z80_mkOp("LD BC,nn", new Uint8Array([1, (imm & 255) & 0xFF, ((imm >> 8) & 255) & 0xFF]), Z80_runOf(main, 1));
}

export function LD_DE(imm) {
    return Z80_mkOp("LD DE,nn", new Uint8Array([17, (imm & 255) & 0xFF, ((imm >> 8) & 255) & 0xFF]), Z80_runOf(main, 17));
}

export function LD_HL(imm) {
    return Z80_mkOp("LD HL,nn", new Uint8Array([33, (imm & 255) & 0xFF, ((imm >> 8) & 255) & 0xFF]), Z80_runOf(main, 33));
}

export function LD_SP(imm) {
    return Z80_mkOp("LD SP,nn", new Uint8Array([49, (imm & 255) & 0xFF, ((imm >> 8) & 255) & 0xFF]), Z80_runOf(main, 49));
}

export function LD_IX(imm) {
    return Z80_mkOp("LD IX,nn", new Uint8Array([221, 33, (imm & 255) & 0xFF, ((imm >> 8) & 255) & 0xFF]), Z80_runOf(dd, 33));
}

export function LD_IY(imm) {
    return Z80_mkOp("LD IY,nn", new Uint8Array([253, 33, (imm & 255) & 0xFF, ((imm >> 8) & 255) & 0xFF]), Z80_runOf(fd, 33));
}

export function LD_HL_ptr(nn) {
    return Z80_mkOp("LD HL,(nn)", new Uint8Array([42, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 42));
}

export function LD_ptr_HL(nn) {
    return Z80_mkOp("LD (nn),HL", new Uint8Array([34, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 34));
}

export function LD_IX_ptr(nn) {
    return Z80_mkOp("LD IX,(nn)", new Uint8Array([221, 42, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(dd, 42));
}

export function LD_ptr_IX(nn) {
    return Z80_mkOp("LD (nn),IX", new Uint8Array([221, 34, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(dd, 34));
}

export function LD_IY_ptr(nn) {
    return Z80_mkOp("LD IY,(nn)", new Uint8Array([253, 42, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(fd, 42));
}

export function LD_ptr_IY(nn) {
    return Z80_mkOp("LD (nn),IY", new Uint8Array([253, 34, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(fd, 34));
}

export function LD_ptr_BC(nn) {
    return Z80_mkOp("LD (nn),BC", new Uint8Array([237, 67, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(ed, 67));
}

export function LD_BC_ptr(nn) {
    return Z80_mkOp("LD BC,(nn)", new Uint8Array([237, 75, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(ed, 75));
}

export function LD_ptr_DE(nn) {
    return Z80_mkOp("LD (nn),DE", new Uint8Array([237, 83, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(ed, 83));
}

export function LD_DE_ptr(nn) {
    return Z80_mkOp("LD DE,(nn)", new Uint8Array([237, 91, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(ed, 91));
}

export function LD_ptr_SP(nn) {
    return Z80_mkOp("LD (nn),SP", new Uint8Array([237, 115, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(ed, 115));
}

export function LD_SP_ptr(nn) {
    return Z80_mkOp("LD SP,(nn)", new Uint8Array([237, 123, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(ed, 123));
}

export const LD_SP_HL = Z80_mkOp("LD SP,HL", new Uint8Array([249]), Z80_runOf(main, 249));

export const LD_SP_IX = Z80_mkOp("LD SP,IX", new Uint8Array([221, 249]), Z80_runOf(dd, 249));

export const LD_SP_IY = Z80_mkOp("LD SP,IY", new Uint8Array([253, 249]), Z80_runOf(fd, 249));

export const LD_A_I = Z80_mkOp("LD A,I", new Uint8Array([237, 87]), Z80_runOf(ed, 87));

export const LD_A_R = Z80_mkOp("LD A,R", new Uint8Array([237, 95]), Z80_runOf(ed, 95));

export const LD_I_A = Z80_mkOp("LD I,A", new Uint8Array([237, 71]), Z80_runOf(ed, 71));

export const LD_R_A = Z80_mkOp("LD R,A", new Uint8Array([237, 79]), Z80_runOf(ed, 79));

export const LD_B_IXH = Z80_mkOp("LD B,IXH", new Uint8Array([221, 68]), Z80_runOf(dd, 68));

export const LD_B_IXL = Z80_mkOp("LD B,IXL", new Uint8Array([221, 69]), Z80_runOf(dd, 69));

export const LD_C_IXH = Z80_mkOp("LD C,IXH", new Uint8Array([221, 76]), Z80_runOf(dd, 76));

export const LD_C_IXL = Z80_mkOp("LD C,IXL", new Uint8Array([221, 77]), Z80_runOf(dd, 77));

export const LD_D_IXH = Z80_mkOp("LD D,IXH", new Uint8Array([221, 84]), Z80_runOf(dd, 84));

export const LD_D_IXL = Z80_mkOp("LD D,IXL", new Uint8Array([221, 85]), Z80_runOf(dd, 85));

export const LD_E_IXH = Z80_mkOp("LD E,IXH", new Uint8Array([221, 92]), Z80_runOf(dd, 92));

export const LD_E_IXL = Z80_mkOp("LD E,IXL", new Uint8Array([221, 93]), Z80_runOf(dd, 93));

export const LD_IXH_B = Z80_mkOp("LD IXH,B", new Uint8Array([221, 96]), Z80_runOf(dd, 96));

export const LD_IXH_C = Z80_mkOp("LD IXH,C", new Uint8Array([221, 97]), Z80_runOf(dd, 97));

export const LD_IXH_D = Z80_mkOp("LD IXH,D", new Uint8Array([221, 98]), Z80_runOf(dd, 98));

export const LD_IXH_E = Z80_mkOp("LD IXH,E", new Uint8Array([221, 99]), Z80_runOf(dd, 99));

export const LD_IXH_A = Z80_mkOp("LD IXH,A", new Uint8Array([221, 103]), Z80_runOf(dd, 103));

export const LD_IXL_B = Z80_mkOp("LD IXL,B", new Uint8Array([221, 104]), Z80_runOf(dd, 104));

export const LD_IXL_C = Z80_mkOp("LD IXL,C", new Uint8Array([221, 105]), Z80_runOf(dd, 105));

export const LD_IXL_D = Z80_mkOp("LD IXL,D", new Uint8Array([221, 106]), Z80_runOf(dd, 106));

export const LD_IXL_E = Z80_mkOp("LD IXL,E", new Uint8Array([221, 107]), Z80_runOf(dd, 107));

export const LD_IXL_A = Z80_mkOp("LD IXL,A", new Uint8Array([221, 111]), Z80_runOf(dd, 111));

export function LD_IXH_n(n) {
    return Z80_mkOp("LD IXH,n", new Uint8Array([221, 38, (n & 255) & 0xFF]), Z80_runOf(dd, 38));
}

export function LD_IXL_n(n) {
    return Z80_mkOp("LD IXL,n", new Uint8Array([221, 46, (n & 255) & 0xFF]), Z80_runOf(dd, 46));
}

export function LD_B_IXd(d) {
    return Z80_mkOp("LD B,(IX+d)", new Uint8Array([221, 70, (d & 255) & 0xFF]), Z80_runOf(dd, 70));
}

export function LD_C_IXd(d) {
    return Z80_mkOp("LD C,(IX+d)", new Uint8Array([221, 78, (d & 255) & 0xFF]), Z80_runOf(dd, 78));
}

export function LD_D_IXd(d) {
    return Z80_mkOp("LD D,(IX+d)", new Uint8Array([221, 86, (d & 255) & 0xFF]), Z80_runOf(dd, 86));
}

export function LD_E_IXd(d) {
    return Z80_mkOp("LD E,(IX+d)", new Uint8Array([221, 94, (d & 255) & 0xFF]), Z80_runOf(dd, 94));
}

export function LD_H_IXd(d) {
    return Z80_mkOp("LD H,(IX+d)", new Uint8Array([221, 102, (d & 255) & 0xFF]), Z80_runOf(dd, 102));
}

export function LD_L_IXd(d) {
    return Z80_mkOp("LD L,(IX+d)", new Uint8Array([221, 110, (d & 255) & 0xFF]), Z80_runOf(dd, 110));
}

export function LD_A_IXd(d) {
    return Z80_mkOp("LD A,(IX+d)", new Uint8Array([221, 126, (d & 255) & 0xFF]), Z80_runOf(dd, 126));
}

export function LD_B_IYd(d) {
    return Z80_mkOp("LD B,(IY+d)", new Uint8Array([253, 70, (d & 255) & 0xFF]), Z80_runOf(fd, 70));
}

export function LD_C_IYd(d) {
    return Z80_mkOp("LD C,(IY+d)", new Uint8Array([253, 78, (d & 255) & 0xFF]), Z80_runOf(fd, 78));
}

export function LD_D_IYd(d) {
    return Z80_mkOp("LD D,(IY+d)", new Uint8Array([253, 86, (d & 255) & 0xFF]), Z80_runOf(fd, 86));
}

export function LD_E_IYd(d) {
    return Z80_mkOp("LD E,(IY+d)", new Uint8Array([253, 94, (d & 255) & 0xFF]), Z80_runOf(fd, 94));
}

export function LD_H_IYd(d) {
    return Z80_mkOp("LD H,(IY+d)", new Uint8Array([253, 102, (d & 255) & 0xFF]), Z80_runOf(fd, 102));
}

export function LD_L_IYd(d) {
    return Z80_mkOp("LD L,(IY+d)", new Uint8Array([253, 110, (d & 255) & 0xFF]), Z80_runOf(fd, 110));
}

export function LD_A_IYd(d) {
    return Z80_mkOp("LD A,(IY+d)", new Uint8Array([253, 126, (d & 255) & 0xFF]), Z80_runOf(fd, 126));
}

export function LD_IXd_B(d) {
    return Z80_mkOp("LD (IX+d),B", new Uint8Array([221, 112, (d & 255) & 0xFF]), Z80_runOf(dd, 112));
}

export function LD_IXd_C(d) {
    return Z80_mkOp("LD (IX+d),C", new Uint8Array([221, 113, (d & 255) & 0xFF]), Z80_runOf(dd, 113));
}

export function LD_IXd_D(d) {
    return Z80_mkOp("LD (IX+d),D", new Uint8Array([221, 114, (d & 255) & 0xFF]), Z80_runOf(dd, 114));
}

export function LD_IXd_E(d) {
    return Z80_mkOp("LD (IX+d),E", new Uint8Array([221, 115, (d & 255) & 0xFF]), Z80_runOf(dd, 115));
}

export function LD_IXd_H(d) {
    return Z80_mkOp("LD (IX+d),H", new Uint8Array([221, 116, (d & 255) & 0xFF]), Z80_runOf(dd, 116));
}

export function LD_IXd_L(d) {
    return Z80_mkOp("LD (IX+d),L", new Uint8Array([221, 117, (d & 255) & 0xFF]), Z80_runOf(dd, 117));
}

export function LD_IXd_A(d) {
    return Z80_mkOp("LD (IX+d),A", new Uint8Array([221, 119, (d & 255) & 0xFF]), Z80_runOf(dd, 119));
}

export function LD_IYd_B(d) {
    return Z80_mkOp("LD (IY+d),B", new Uint8Array([253, 112, (d & 255) & 0xFF]), Z80_runOf(fd, 112));
}

export function LD_IYd_C(d) {
    return Z80_mkOp("LD (IY+d),C", new Uint8Array([253, 113, (d & 255) & 0xFF]), Z80_runOf(fd, 113));
}

export function LD_IYd_D(d) {
    return Z80_mkOp("LD (IY+d),D", new Uint8Array([253, 114, (d & 255) & 0xFF]), Z80_runOf(fd, 114));
}

export function LD_IYd_E(d) {
    return Z80_mkOp("LD (IY+d),E", new Uint8Array([253, 115, (d & 255) & 0xFF]), Z80_runOf(fd, 115));
}

export function LD_IYd_H(d) {
    return Z80_mkOp("LD (IY+d),H", new Uint8Array([253, 116, (d & 255) & 0xFF]), Z80_runOf(fd, 116));
}

export function LD_IYd_L(d) {
    return Z80_mkOp("LD (IY+d),L", new Uint8Array([253, 117, (d & 255) & 0xFF]), Z80_runOf(fd, 117));
}

export function LD_IYd_A(d) {
    return Z80_mkOp("LD (IY+d),A", new Uint8Array([253, 119, (d & 255) & 0xFF]), Z80_runOf(fd, 119));
}

export const ADD_A_B = Z80_mkOp("ADD A,B", new Uint8Array([128]), Z80_runOf(main, 128));

export const ADD_A_C = Z80_mkOp("ADD A,C", new Uint8Array([129]), Z80_runOf(main, 129));

export const ADD_A_D = Z80_mkOp("ADD A,D", new Uint8Array([130]), Z80_runOf(main, 130));

export const ADD_A_E = Z80_mkOp("ADD A,E", new Uint8Array([131]), Z80_runOf(main, 131));

export const ADD_A_H = Z80_mkOp("ADD A,H", new Uint8Array([132]), Z80_runOf(main, 132));

export const ADD_A_L = Z80_mkOp("ADD A,L", new Uint8Array([133]), Z80_runOf(main, 133));

export const ADD_A_A = Z80_mkOp("ADD A,A", new Uint8Array([135]), Z80_runOf(main, 135));

export const ADD_A_PTR_HL = Z80_mkOp("ADD A,(HL)", new Uint8Array([134]), Z80_runOf(main, 134));

export function ADD_A_n(n) {
    return Z80_mkOp("ADD A,n", new Uint8Array([198, (n & 255) & 0xFF]), Z80_runOf(main, 198));
}

export function ADD_A_IXd(d) {
    return Z80_mkOp("ADD A,(IX+d)", new Uint8Array([221, 134, (d & 255) & 0xFF]), Z80_runOf(dd, 134));
}

export function ADD_A_IYd(d) {
    return Z80_mkOp("ADD A,(IY+d)", new Uint8Array([253, 134, (d & 255) & 0xFF]), Z80_runOf(fd, 134));
}

export const ADC_A_B = Z80_mkOp("ADC A,B", new Uint8Array([136]), Z80_runOf(main, 136));

export const ADC_A_C = Z80_mkOp("ADC A,C", new Uint8Array([137]), Z80_runOf(main, 137));

export const ADC_A_D = Z80_mkOp("ADC A,D", new Uint8Array([138]), Z80_runOf(main, 138));

export const ADC_A_E = Z80_mkOp("ADC A,E", new Uint8Array([139]), Z80_runOf(main, 139));

export const ADC_A_H = Z80_mkOp("ADC A,H", new Uint8Array([140]), Z80_runOf(main, 140));

export const ADC_A_L = Z80_mkOp("ADC A,L", new Uint8Array([141]), Z80_runOf(main, 141));

export const ADC_A_A = Z80_mkOp("ADC A,A", new Uint8Array([143]), Z80_runOf(main, 143));

export const ADC_A_PTR_HL = Z80_mkOp("ADC A,(HL)", new Uint8Array([142]), Z80_runOf(main, 142));

export function ADC_A_n(n) {
    return Z80_mkOp("ADC A,n", new Uint8Array([206, (n & 255) & 0xFF]), Z80_runOf(main, 206));
}

export function ADC_A_IXd(d) {
    return Z80_mkOp("ADC A,(IX+d)", new Uint8Array([221, 142, (d & 255) & 0xFF]), Z80_runOf(dd, 142));
}

export function ADC_A_IYd(d) {
    return Z80_mkOp("ADC A,(IY+d)", new Uint8Array([253, 142, (d & 255) & 0xFF]), Z80_runOf(fd, 142));
}

export const SUB_A_B = Z80_mkOp("SUB A,B", new Uint8Array([144]), Z80_runOf(main, 144));

export const SUB_A_C = Z80_mkOp("SUB A,C", new Uint8Array([145]), Z80_runOf(main, 145));

export const SUB_A_D = Z80_mkOp("SUB A,D", new Uint8Array([146]), Z80_runOf(main, 146));

export const SUB_A_E = Z80_mkOp("SUB A,E", new Uint8Array([147]), Z80_runOf(main, 147));

export const SUB_A_H = Z80_mkOp("SUB A,H", new Uint8Array([148]), Z80_runOf(main, 148));

export const SUB_A_L = Z80_mkOp("SUB A,L", new Uint8Array([149]), Z80_runOf(main, 149));

export const SUB_A_A = Z80_mkOp("SUB A,A", new Uint8Array([151]), Z80_runOf(main, 151));

export const SUB_A_PTR_HL = Z80_mkOp("SUB A,(HL)", new Uint8Array([150]), Z80_runOf(main, 150));

export function SUB_A_n(n) {
    return Z80_mkOp("SUB A,n", new Uint8Array([214, (n & 255) & 0xFF]), Z80_runOf(main, 214));
}

export function SUB_A_IXd(d) {
    return Z80_mkOp("SUB A,(IX+d)", new Uint8Array([221, 150, (d & 255) & 0xFF]), Z80_runOf(dd, 150));
}

export function SUB_A_IYd(d) {
    return Z80_mkOp("SUB A,(IY+d)", new Uint8Array([253, 150, (d & 255) & 0xFF]), Z80_runOf(fd, 150));
}

export const SBC_A_B = Z80_mkOp("SBC A,B", new Uint8Array([152]), Z80_runOf(main, 152));

export const SBC_A_C = Z80_mkOp("SBC A,C", new Uint8Array([153]), Z80_runOf(main, 153));

export const SBC_A_D = Z80_mkOp("SBC A,D", new Uint8Array([154]), Z80_runOf(main, 154));

export const SBC_A_E = Z80_mkOp("SBC A,E", new Uint8Array([155]), Z80_runOf(main, 155));

export const SBC_A_H = Z80_mkOp("SBC A,H", new Uint8Array([156]), Z80_runOf(main, 156));

export const SBC_A_L = Z80_mkOp("SBC A,L", new Uint8Array([157]), Z80_runOf(main, 157));

export const SBC_A_A = Z80_mkOp("SBC A,A", new Uint8Array([159]), Z80_runOf(main, 159));

export const SBC_A_PTR_HL = Z80_mkOp("SBC A,(HL)", new Uint8Array([158]), Z80_runOf(main, 158));

export function SBC_A_n(n) {
    return Z80_mkOp("SBC A,n", new Uint8Array([222, (n & 255) & 0xFF]), Z80_runOf(main, 222));
}

export function SBC_A_IXd(d) {
    return Z80_mkOp("SBC A,(IX+d)", new Uint8Array([221, 158, (d & 255) & 0xFF]), Z80_runOf(dd, 158));
}

export function SBC_A_IYd(d) {
    return Z80_mkOp("SBC A,(IY+d)", new Uint8Array([253, 158, (d & 255) & 0xFF]), Z80_runOf(fd, 158));
}

export const AND_A_B = Z80_mkOp("AND A,B", new Uint8Array([160]), Z80_runOf(main, 160));

export const AND_A_C = Z80_mkOp("AND A,C", new Uint8Array([161]), Z80_runOf(main, 161));

export const AND_A_D = Z80_mkOp("AND A,D", new Uint8Array([162]), Z80_runOf(main, 162));

export const AND_A_E = Z80_mkOp("AND A,E", new Uint8Array([163]), Z80_runOf(main, 163));

export const AND_A_H = Z80_mkOp("AND A,H", new Uint8Array([164]), Z80_runOf(main, 164));

export const AND_A_L = Z80_mkOp("AND A,L", new Uint8Array([165]), Z80_runOf(main, 165));

export const AND_A_A = Z80_mkOp("AND A,A", new Uint8Array([167]), Z80_runOf(main, 167));

export const AND_A_PTR_HL = Z80_mkOp("AND A,(HL)", new Uint8Array([166]), Z80_runOf(main, 166));

export function AND_A_n(n) {
    return Z80_mkOp("AND A,n", new Uint8Array([230, (n & 255) & 0xFF]), Z80_runOf(main, 230));
}

export function AND_A_IXd(d) {
    return Z80_mkOp("AND A,(IX+d)", new Uint8Array([221, 166, (d & 255) & 0xFF]), Z80_runOf(dd, 166));
}

export function AND_A_IYd(d) {
    return Z80_mkOp("AND A,(IY+d)", new Uint8Array([253, 166, (d & 255) & 0xFF]), Z80_runOf(fd, 166));
}

export const XOR_A_B = Z80_mkOp("XOR A,B", new Uint8Array([168]), Z80_runOf(main, 168));

export const XOR_A_C = Z80_mkOp("XOR A,C", new Uint8Array([169]), Z80_runOf(main, 169));

export const XOR_A_D = Z80_mkOp("XOR A,D", new Uint8Array([170]), Z80_runOf(main, 170));

export const XOR_A_E = Z80_mkOp("XOR A,E", new Uint8Array([171]), Z80_runOf(main, 171));

export const XOR_A_H = Z80_mkOp("XOR A,H", new Uint8Array([172]), Z80_runOf(main, 172));

export const XOR_A_L = Z80_mkOp("XOR A,L", new Uint8Array([173]), Z80_runOf(main, 173));

export const XOR_A_A = Z80_mkOp("XOR A,A", new Uint8Array([175]), Z80_runOf(main, 175));

export const XOR_A_PTR_HL = Z80_mkOp("XOR A,(HL)", new Uint8Array([174]), Z80_runOf(main, 174));

export function XOR_A_n(n) {
    return Z80_mkOp("XOR A,n", new Uint8Array([238, (n & 255) & 0xFF]), Z80_runOf(main, 238));
}

export function XOR_A_IXd(d) {
    return Z80_mkOp("XOR A,(IX+d)", new Uint8Array([221, 174, (d & 255) & 0xFF]), Z80_runOf(dd, 174));
}

export function XOR_A_IYd(d) {
    return Z80_mkOp("XOR A,(IY+d)", new Uint8Array([253, 174, (d & 255) & 0xFF]), Z80_runOf(fd, 174));
}

export const OR_A_B = Z80_mkOp("OR A,B", new Uint8Array([176]), Z80_runOf(main, 176));

export const OR_A_C = Z80_mkOp("OR A,C", new Uint8Array([177]), Z80_runOf(main, 177));

export const OR_A_D = Z80_mkOp("OR A,D", new Uint8Array([178]), Z80_runOf(main, 178));

export const OR_A_E = Z80_mkOp("OR A,E", new Uint8Array([179]), Z80_runOf(main, 179));

export const OR_A_H = Z80_mkOp("OR A,H", new Uint8Array([180]), Z80_runOf(main, 180));

export const OR_A_L = Z80_mkOp("OR A,L", new Uint8Array([181]), Z80_runOf(main, 181));

export const OR_A_A = Z80_mkOp("OR A,A", new Uint8Array([183]), Z80_runOf(main, 183));

export const OR_A_PTR_HL = Z80_mkOp("OR A,(HL)", new Uint8Array([182]), Z80_runOf(main, 182));

export function OR_A_n(n) {
    return Z80_mkOp("OR A,n", new Uint8Array([246, (n & 255) & 0xFF]), Z80_runOf(main, 246));
}

export function OR_A_IXd(d) {
    return Z80_mkOp("OR A,(IX+d)", new Uint8Array([221, 182, (d & 255) & 0xFF]), Z80_runOf(dd, 182));
}

export function OR_A_IYd(d) {
    return Z80_mkOp("OR A,(IY+d)", new Uint8Array([253, 182, (d & 255) & 0xFF]), Z80_runOf(fd, 182));
}

export const CP_A_B = Z80_mkOp("CP A,B", new Uint8Array([184]), Z80_runOf(main, 184));

export const CP_A_C = Z80_mkOp("CP A,C", new Uint8Array([185]), Z80_runOf(main, 185));

export const CP_A_D = Z80_mkOp("CP A,D", new Uint8Array([186]), Z80_runOf(main, 186));

export const CP_A_E = Z80_mkOp("CP A,E", new Uint8Array([187]), Z80_runOf(main, 187));

export const CP_A_H = Z80_mkOp("CP A,H", new Uint8Array([188]), Z80_runOf(main, 188));

export const CP_A_L = Z80_mkOp("CP A,L", new Uint8Array([189]), Z80_runOf(main, 189));

export const CP_A_A = Z80_mkOp("CP A,A", new Uint8Array([191]), Z80_runOf(main, 191));

export const CP_A_PTR_HL = Z80_mkOp("CP A,(HL)", new Uint8Array([190]), Z80_runOf(main, 190));

export function CP_A_n(n) {
    return Z80_mkOp("CP A,n", new Uint8Array([254, (n & 255) & 0xFF]), Z80_runOf(main, 254));
}

export function CP_A_IXd(d) {
    return Z80_mkOp("CP A,(IX+d)", new Uint8Array([221, 190, (d & 255) & 0xFF]), Z80_runOf(dd, 190));
}

export function CP_A_IYd(d) {
    return Z80_mkOp("CP A,(IY+d)", new Uint8Array([253, 190, (d & 255) & 0xFF]), Z80_runOf(fd, 190));
}

export const ADD_HL_BC = Z80_mkOp("ADD HL,BC", new Uint8Array([9]), Z80_runOf(main, 9));

export const ADC_HL_BC = Z80_mkOp("ADC HL,BC", new Uint8Array([237, 74]), Z80_runOf(ed, 74));

export const SBC_HL_BC = Z80_mkOp("SBC HL,BC", new Uint8Array([237, 66]), Z80_runOf(ed, 66));

export const ADD_HL_DE = Z80_mkOp("ADD HL,DE", new Uint8Array([25]), Z80_runOf(main, 25));

export const ADC_HL_DE = Z80_mkOp("ADC HL,DE", new Uint8Array([237, 90]), Z80_runOf(ed, 90));

export const SBC_HL_DE = Z80_mkOp("SBC HL,DE", new Uint8Array([237, 82]), Z80_runOf(ed, 82));

export const ADD_HL_HL = Z80_mkOp("ADD HL,HL", new Uint8Array([41]), Z80_runOf(main, 41));

export const ADC_HL_HL = Z80_mkOp("ADC HL,HL", new Uint8Array([237, 106]), Z80_runOf(ed, 106));

export const SBC_HL_HL = Z80_mkOp("SBC HL,HL", new Uint8Array([237, 98]), Z80_runOf(ed, 98));

export const ADD_HL_SP = Z80_mkOp("ADD HL,SP", new Uint8Array([57]), Z80_runOf(main, 57));

export const ADC_HL_SP = Z80_mkOp("ADC HL,SP", new Uint8Array([237, 122]), Z80_runOf(ed, 122));

export const SBC_HL_SP = Z80_mkOp("SBC HL,SP", new Uint8Array([237, 114]), Z80_runOf(ed, 114));

export const ADD_IX_BC = Z80_mkOp("ADD IX,BC", new Uint8Array([221, 9]), Z80_runOf(dd, 9));

export const ADD_IY_BC = Z80_mkOp("ADD IY,BC", new Uint8Array([253, 9]), Z80_runOf(fd, 9));

export const ADD_IX_DE = Z80_mkOp("ADD IX,DE", new Uint8Array([221, 25]), Z80_runOf(dd, 25));

export const ADD_IY_DE = Z80_mkOp("ADD IY,DE", new Uint8Array([253, 25]), Z80_runOf(fd, 25));

export const ADD_IX_HL = Z80_mkOp("ADD IX,HL", new Uint8Array([221, 41]), Z80_runOf(dd, 41));

export const ADD_IY_HL = Z80_mkOp("ADD IY,HL", new Uint8Array([253, 41]), Z80_runOf(fd, 41));

export const ADD_IX_SP = Z80_mkOp("ADD IX,SP", new Uint8Array([221, 57]), Z80_runOf(dd, 57));

export const ADD_IY_SP = Z80_mkOp("ADD IY,SP", new Uint8Array([253, 57]), Z80_runOf(fd, 57));

export const INC_B = Z80_mkOp("INC B", new Uint8Array([4]), Z80_runOf(main, 4));

export const DEC_B = Z80_mkOp("DEC B", new Uint8Array([5]), Z80_runOf(main, 5));

export const INC_C = Z80_mkOp("INC C", new Uint8Array([12]), Z80_runOf(main, 12));

export const DEC_C = Z80_mkOp("DEC C", new Uint8Array([13]), Z80_runOf(main, 13));

export const INC_D = Z80_mkOp("INC D", new Uint8Array([20]), Z80_runOf(main, 20));

export const DEC_D = Z80_mkOp("DEC D", new Uint8Array([21]), Z80_runOf(main, 21));

export const INC_E = Z80_mkOp("INC E", new Uint8Array([28]), Z80_runOf(main, 28));

export const DEC_E = Z80_mkOp("DEC E", new Uint8Array([29]), Z80_runOf(main, 29));

export const INC_H = Z80_mkOp("INC H", new Uint8Array([36]), Z80_runOf(main, 36));

export const DEC_H = Z80_mkOp("DEC H", new Uint8Array([37]), Z80_runOf(main, 37));

export const INC_L = Z80_mkOp("INC L", new Uint8Array([44]), Z80_runOf(main, 44));

export const DEC_L = Z80_mkOp("DEC L", new Uint8Array([45]), Z80_runOf(main, 45));

export const INC_A = Z80_mkOp("INC A", new Uint8Array([60]), Z80_runOf(main, 60));

export const DEC_A = Z80_mkOp("DEC A", new Uint8Array([61]), Z80_runOf(main, 61));

export const INC_PTR_HL = Z80_mkOp("INC (HL)", new Uint8Array([52]), Z80_runOf(main, 52));

export const DEC_PTR_HL = Z80_mkOp("DEC (HL)", new Uint8Array([53]), Z80_runOf(main, 53));

export function INC_PTR_IXd(d) {
    return Z80_mkOp("INC (IX+d)", new Uint8Array([221, 52, (d & 255) & 0xFF]), Z80_runOf(dd, 52));
}

export function DEC_PTR_IXd(d) {
    return Z80_mkOp("DEC (IX+d)", new Uint8Array([221, 53, (d & 255) & 0xFF]), Z80_runOf(dd, 53));
}

export function LD_PTR_IXd_n(d, n) {
    return Z80_mkOp("LD (IX+d),n", new Uint8Array([221, 54, (d & 255) & 0xFF, (n & 255) & 0xFF]), Z80_runOf(dd, 54));
}

export const INC_BC = Z80_mkOp("INC BC", new Uint8Array([3]), Z80_runOf(main, 3));

export const DEC_BC = Z80_mkOp("DEC BC", new Uint8Array([11]), Z80_runOf(main, 11));

export const INC_DE = Z80_mkOp("INC DE", new Uint8Array([19]), Z80_runOf(main, 19));

export const DEC_DE = Z80_mkOp("DEC DE", new Uint8Array([27]), Z80_runOf(main, 27));

export const INC_HL = Z80_mkOp("INC HL", new Uint8Array([35]), Z80_runOf(main, 35));

export const DEC_HL = Z80_mkOp("DEC HL", new Uint8Array([43]), Z80_runOf(main, 43));

export const INC_SP = Z80_mkOp("INC SP", new Uint8Array([51]), Z80_runOf(main, 51));

export const DEC_SP = Z80_mkOp("DEC SP", new Uint8Array([59]), Z80_runOf(main, 59));

export const INC_IX = Z80_mkOp("INC IX", new Uint8Array([221, 35]), Z80_runOf(dd, 35));

export const DEC_IX = Z80_mkOp("DEC IX", new Uint8Array([221, 43]), Z80_runOf(dd, 43));

export const INC_IXH = Z80_mkOp("INC IXH", new Uint8Array([221, 36]), Z80_runOf(dd, 36));

export const DEC_IXH = Z80_mkOp("DEC IXH", new Uint8Array([221, 37]), Z80_runOf(dd, 37));

export const INC_IXL = Z80_mkOp("INC IXL", new Uint8Array([221, 44]), Z80_runOf(dd, 44));

export const DEC_IXL = Z80_mkOp("DEC IXL", new Uint8Array([221, 45]), Z80_runOf(dd, 45));

export const PUSH_BC = Z80_mkOp("PUSH BC", new Uint8Array([197]), Z80_runOf(main, 197));

export const POP_BC = Z80_mkOp("POP BC", new Uint8Array([193]), Z80_runOf(main, 193));

export const PUSH_DE = Z80_mkOp("PUSH DE", new Uint8Array([213]), Z80_runOf(main, 213));

export const POP_DE = Z80_mkOp("POP DE", new Uint8Array([209]), Z80_runOf(main, 209));

export const PUSH_HL = Z80_mkOp("PUSH HL", new Uint8Array([229]), Z80_runOf(main, 229));

export const POP_HL = Z80_mkOp("POP HL", new Uint8Array([225]), Z80_runOf(main, 225));

export const PUSH_AF = Z80_mkOp("PUSH AF", new Uint8Array([245]), Z80_runOf(main, 245));

export const POP_AF = Z80_mkOp("POP AF", new Uint8Array([241]), Z80_runOf(main, 241));

export const PUSH_IX = Z80_mkOp("PUSH IX", new Uint8Array([221, 229]), Z80_runOf(dd, 229));

export const POP_IX = Z80_mkOp("POP IX", new Uint8Array([221, 225]), Z80_runOf(dd, 225));

export const RLC_B = Z80_mkOp("RLC B", new Uint8Array([203, 0]), Z80_runOf(cb, 0));

export const RLC_C = Z80_mkOp("RLC C", new Uint8Array([203, 1]), Z80_runOf(cb, 1));

export const RLC_D = Z80_mkOp("RLC D", new Uint8Array([203, 2]), Z80_runOf(cb, 2));

export const RLC_E = Z80_mkOp("RLC E", new Uint8Array([203, 3]), Z80_runOf(cb, 3));

export const RLC_H = Z80_mkOp("RLC H", new Uint8Array([203, 4]), Z80_runOf(cb, 4));

export const RLC_L = Z80_mkOp("RLC L", new Uint8Array([203, 5]), Z80_runOf(cb, 5));

export const RLC_A = Z80_mkOp("RLC A", new Uint8Array([203, 7]), Z80_runOf(cb, 7));

export const RLC_PTR_HL = Z80_mkOp("RLC (HL)", new Uint8Array([203, 6]), Z80_runOf(cb, 6));

export function RLC_PTR_IXd(d) {
    return Z80_mkOp("RLC (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 6]), Z80_runOf(dd_cb, 6));
}

export function RLC_PTR_IYd(d) {
    return Z80_mkOp("RLC (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 6]), Z80_runOf(fd_cb, 6));
}

export const RRC_B = Z80_mkOp("RRC B", new Uint8Array([203, 8]), Z80_runOf(cb, 8));

export const RRC_C = Z80_mkOp("RRC C", new Uint8Array([203, 9]), Z80_runOf(cb, 9));

export const RRC_D = Z80_mkOp("RRC D", new Uint8Array([203, 10]), Z80_runOf(cb, 10));

export const RRC_E = Z80_mkOp("RRC E", new Uint8Array([203, 11]), Z80_runOf(cb, 11));

export const RRC_H = Z80_mkOp("RRC H", new Uint8Array([203, 12]), Z80_runOf(cb, 12));

export const RRC_L = Z80_mkOp("RRC L", new Uint8Array([203, 13]), Z80_runOf(cb, 13));

export const RRC_A = Z80_mkOp("RRC A", new Uint8Array([203, 15]), Z80_runOf(cb, 15));

export const RRC_PTR_HL = Z80_mkOp("RRC (HL)", new Uint8Array([203, 14]), Z80_runOf(cb, 14));

export function RRC_PTR_IXd(d) {
    return Z80_mkOp("RRC (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 14]), Z80_runOf(dd_cb, 14));
}

export function RRC_PTR_IYd(d) {
    return Z80_mkOp("RRC (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 14]), Z80_runOf(fd_cb, 14));
}

export const RL_B = Z80_mkOp("RL B", new Uint8Array([203, 16]), Z80_runOf(cb, 16));

export const RL_C = Z80_mkOp("RL C", new Uint8Array([203, 17]), Z80_runOf(cb, 17));

export const RL_D = Z80_mkOp("RL D", new Uint8Array([203, 18]), Z80_runOf(cb, 18));

export const RL_E = Z80_mkOp("RL E", new Uint8Array([203, 19]), Z80_runOf(cb, 19));

export const RL_H = Z80_mkOp("RL H", new Uint8Array([203, 20]), Z80_runOf(cb, 20));

export const RL_L = Z80_mkOp("RL L", new Uint8Array([203, 21]), Z80_runOf(cb, 21));

export const RL_A = Z80_mkOp("RL A", new Uint8Array([203, 23]), Z80_runOf(cb, 23));

export const RL_PTR_HL = Z80_mkOp("RL (HL)", new Uint8Array([203, 22]), Z80_runOf(cb, 22));

export function RL_PTR_IXd(d) {
    return Z80_mkOp("RL (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 22]), Z80_runOf(dd_cb, 22));
}

export function RL_PTR_IYd(d) {
    return Z80_mkOp("RL (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 22]), Z80_runOf(fd_cb, 22));
}

export const RR_B = Z80_mkOp("RR B", new Uint8Array([203, 24]), Z80_runOf(cb, 24));

export const RR_C = Z80_mkOp("RR C", new Uint8Array([203, 25]), Z80_runOf(cb, 25));

export const RR_D = Z80_mkOp("RR D", new Uint8Array([203, 26]), Z80_runOf(cb, 26));

export const RR_E = Z80_mkOp("RR E", new Uint8Array([203, 27]), Z80_runOf(cb, 27));

export const RR_H = Z80_mkOp("RR H", new Uint8Array([203, 28]), Z80_runOf(cb, 28));

export const RR_L = Z80_mkOp("RR L", new Uint8Array([203, 29]), Z80_runOf(cb, 29));

export const RR_A = Z80_mkOp("RR A", new Uint8Array([203, 31]), Z80_runOf(cb, 31));

export const RR_PTR_HL = Z80_mkOp("RR (HL)", new Uint8Array([203, 30]), Z80_runOf(cb, 30));

export function RR_PTR_IXd(d) {
    return Z80_mkOp("RR (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 30]), Z80_runOf(dd_cb, 30));
}

export function RR_PTR_IYd(d) {
    return Z80_mkOp("RR (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 30]), Z80_runOf(fd_cb, 30));
}

export const SLA_B = Z80_mkOp("SLA B", new Uint8Array([203, 32]), Z80_runOf(cb, 32));

export const SLA_C = Z80_mkOp("SLA C", new Uint8Array([203, 33]), Z80_runOf(cb, 33));

export const SLA_D = Z80_mkOp("SLA D", new Uint8Array([203, 34]), Z80_runOf(cb, 34));

export const SLA_E = Z80_mkOp("SLA E", new Uint8Array([203, 35]), Z80_runOf(cb, 35));

export const SLA_H = Z80_mkOp("SLA H", new Uint8Array([203, 36]), Z80_runOf(cb, 36));

export const SLA_L = Z80_mkOp("SLA L", new Uint8Array([203, 37]), Z80_runOf(cb, 37));

export const SLA_A = Z80_mkOp("SLA A", new Uint8Array([203, 39]), Z80_runOf(cb, 39));

export const SLA_PTR_HL = Z80_mkOp("SLA (HL)", new Uint8Array([203, 38]), Z80_runOf(cb, 38));

export function SLA_PTR_IXd(d) {
    return Z80_mkOp("SLA (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 38]), Z80_runOf(dd_cb, 38));
}

export function SLA_PTR_IYd(d) {
    return Z80_mkOp("SLA (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 38]), Z80_runOf(fd_cb, 38));
}

export const SRA_B = Z80_mkOp("SRA B", new Uint8Array([203, 40]), Z80_runOf(cb, 40));

export const SRA_C = Z80_mkOp("SRA C", new Uint8Array([203, 41]), Z80_runOf(cb, 41));

export const SRA_D = Z80_mkOp("SRA D", new Uint8Array([203, 42]), Z80_runOf(cb, 42));

export const SRA_E = Z80_mkOp("SRA E", new Uint8Array([203, 43]), Z80_runOf(cb, 43));

export const SRA_H = Z80_mkOp("SRA H", new Uint8Array([203, 44]), Z80_runOf(cb, 44));

export const SRA_L = Z80_mkOp("SRA L", new Uint8Array([203, 45]), Z80_runOf(cb, 45));

export const SRA_A = Z80_mkOp("SRA A", new Uint8Array([203, 47]), Z80_runOf(cb, 47));

export const SRA_PTR_HL = Z80_mkOp("SRA (HL)", new Uint8Array([203, 46]), Z80_runOf(cb, 46));

export function SRA_PTR_IXd(d) {
    return Z80_mkOp("SRA (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 46]), Z80_runOf(dd_cb, 46));
}

export function SRA_PTR_IYd(d) {
    return Z80_mkOp("SRA (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 46]), Z80_runOf(fd_cb, 46));
}

export const SRL_B = Z80_mkOp("SRL B", new Uint8Array([203, 56]), Z80_runOf(cb, 56));

export const SRL_C = Z80_mkOp("SRL C", new Uint8Array([203, 57]), Z80_runOf(cb, 57));

export const SRL_D = Z80_mkOp("SRL D", new Uint8Array([203, 58]), Z80_runOf(cb, 58));

export const SRL_E = Z80_mkOp("SRL E", new Uint8Array([203, 59]), Z80_runOf(cb, 59));

export const SRL_H = Z80_mkOp("SRL H", new Uint8Array([203, 60]), Z80_runOf(cb, 60));

export const SRL_L = Z80_mkOp("SRL L", new Uint8Array([203, 61]), Z80_runOf(cb, 61));

export const SRL_A = Z80_mkOp("SRL A", new Uint8Array([203, 63]), Z80_runOf(cb, 63));

export const SRL_PTR_HL = Z80_mkOp("SRL (HL)", new Uint8Array([203, 62]), Z80_runOf(cb, 62));

export function SRL_PTR_IXd(d) {
    return Z80_mkOp("SRL (IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 62]), Z80_runOf(dd_cb, 62));
}

export function SRL_PTR_IYd(d) {
    return Z80_mkOp("SRL (IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 62]), Z80_runOf(fd_cb, 62));
}

export const BIT0_B = Z80_mkOp("BIT 0,B", new Uint8Array([203, 64]), Z80_runOf(cb, 64));

export const BIT0_C = Z80_mkOp("BIT 0,C", new Uint8Array([203, 65]), Z80_runOf(cb, 65));

export const BIT0_D = Z80_mkOp("BIT 0,D", new Uint8Array([203, 66]), Z80_runOf(cb, 66));

export const BIT0_E = Z80_mkOp("BIT 0,E", new Uint8Array([203, 67]), Z80_runOf(cb, 67));

export const BIT0_H = Z80_mkOp("BIT 0,H", new Uint8Array([203, 68]), Z80_runOf(cb, 68));

export const BIT0_L = Z80_mkOp("BIT 0,L", new Uint8Array([203, 69]), Z80_runOf(cb, 69));

export const BIT0_A = Z80_mkOp("BIT 0,A", new Uint8Array([203, 71]), Z80_runOf(cb, 71));

export const BIT0_PTR_HL = Z80_mkOp("BIT 0,(HL)", new Uint8Array([203, 70]), Z80_runOf(cb, 70));

export function BIT0_PTR_IXd(d) {
    return Z80_mkOp("BIT 0,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 70]), Z80_runOf(dd_cb, 70));
}

export function BIT0_PTR_IYd(d) {
    return Z80_mkOp("BIT 0,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 70]), Z80_runOf(fd_cb, 70));
}

export const RES0_B = Z80_mkOp("RES 0,B", new Uint8Array([203, 128]), Z80_runOf(cb, 128));

export const RES0_C = Z80_mkOp("RES 0,C", new Uint8Array([203, 129]), Z80_runOf(cb, 129));

export const RES0_D = Z80_mkOp("RES 0,D", new Uint8Array([203, 130]), Z80_runOf(cb, 130));

export const RES0_E = Z80_mkOp("RES 0,E", new Uint8Array([203, 131]), Z80_runOf(cb, 131));

export const RES0_H = Z80_mkOp("RES 0,H", new Uint8Array([203, 132]), Z80_runOf(cb, 132));

export const RES0_L = Z80_mkOp("RES 0,L", new Uint8Array([203, 133]), Z80_runOf(cb, 133));

export const RES0_A = Z80_mkOp("RES 0,A", new Uint8Array([203, 135]), Z80_runOf(cb, 135));

export const RES0_PTR_HL = Z80_mkOp("RES 0,(HL)", new Uint8Array([203, 134]), Z80_runOf(cb, 134));

export function RES0_PTR_IXd(d) {
    return Z80_mkOp("RES 0,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 134]), Z80_runOf(dd_cb, 134));
}

export function RES0_PTR_IYd(d) {
    return Z80_mkOp("RES 0,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 134]), Z80_runOf(fd_cb, 134));
}

export const SET0_B = Z80_mkOp("SET 0,B", new Uint8Array([203, 192]), Z80_runOf(cb, 192));

export const SET0_C = Z80_mkOp("SET 0,C", new Uint8Array([203, 193]), Z80_runOf(cb, 193));

export const SET0_D = Z80_mkOp("SET 0,D", new Uint8Array([203, 194]), Z80_runOf(cb, 194));

export const SET0_E = Z80_mkOp("SET 0,E", new Uint8Array([203, 195]), Z80_runOf(cb, 195));

export const SET0_H = Z80_mkOp("SET 0,H", new Uint8Array([203, 196]), Z80_runOf(cb, 196));

export const SET0_L = Z80_mkOp("SET 0,L", new Uint8Array([203, 197]), Z80_runOf(cb, 197));

export const SET0_A = Z80_mkOp("SET 0,A", new Uint8Array([203, 199]), Z80_runOf(cb, 199));

export const SET0_PTR_HL = Z80_mkOp("SET 0,(HL)", new Uint8Array([203, 198]), Z80_runOf(cb, 198));

export function SET0_PTR_IXd(d) {
    return Z80_mkOp("SET 0,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 198]), Z80_runOf(dd_cb, 198));
}

export function SET0_PTR_IYd(d) {
    return Z80_mkOp("SET 0,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 198]), Z80_runOf(fd_cb, 198));
}

export const BIT1_B = Z80_mkOp("BIT 1,B", new Uint8Array([203, 72]), Z80_runOf(cb, 72));

export const BIT1_C = Z80_mkOp("BIT 1,C", new Uint8Array([203, 73]), Z80_runOf(cb, 73));

export const BIT1_D = Z80_mkOp("BIT 1,D", new Uint8Array([203, 74]), Z80_runOf(cb, 74));

export const BIT1_E = Z80_mkOp("BIT 1,E", new Uint8Array([203, 75]), Z80_runOf(cb, 75));

export const BIT1_H = Z80_mkOp("BIT 1,H", new Uint8Array([203, 76]), Z80_runOf(cb, 76));

export const BIT1_L = Z80_mkOp("BIT 1,L", new Uint8Array([203, 77]), Z80_runOf(cb, 77));

export const BIT1_A = Z80_mkOp("BIT 1,A", new Uint8Array([203, 79]), Z80_runOf(cb, 79));

export const BIT1_PTR_HL = Z80_mkOp("BIT 1,(HL)", new Uint8Array([203, 78]), Z80_runOf(cb, 78));

export function BIT1_PTR_IXd(d) {
    return Z80_mkOp("BIT 1,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 78]), Z80_runOf(dd_cb, 78));
}

export function BIT1_PTR_IYd(d) {
    return Z80_mkOp("BIT 1,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 78]), Z80_runOf(fd_cb, 78));
}

export const RES1_B = Z80_mkOp("RES 1,B", new Uint8Array([203, 136]), Z80_runOf(cb, 136));

export const RES1_C = Z80_mkOp("RES 1,C", new Uint8Array([203, 137]), Z80_runOf(cb, 137));

export const RES1_D = Z80_mkOp("RES 1,D", new Uint8Array([203, 138]), Z80_runOf(cb, 138));

export const RES1_E = Z80_mkOp("RES 1,E", new Uint8Array([203, 139]), Z80_runOf(cb, 139));

export const RES1_H = Z80_mkOp("RES 1,H", new Uint8Array([203, 140]), Z80_runOf(cb, 140));

export const RES1_L = Z80_mkOp("RES 1,L", new Uint8Array([203, 141]), Z80_runOf(cb, 141));

export const RES1_A = Z80_mkOp("RES 1,A", new Uint8Array([203, 143]), Z80_runOf(cb, 143));

export const RES1_PTR_HL = Z80_mkOp("RES 1,(HL)", new Uint8Array([203, 142]), Z80_runOf(cb, 142));

export function RES1_PTR_IXd(d) {
    return Z80_mkOp("RES 1,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 142]), Z80_runOf(dd_cb, 142));
}

export function RES1_PTR_IYd(d) {
    return Z80_mkOp("RES 1,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 142]), Z80_runOf(fd_cb, 142));
}

export const SET1_B = Z80_mkOp("SET 1,B", new Uint8Array([203, 200]), Z80_runOf(cb, 200));

export const SET1_C = Z80_mkOp("SET 1,C", new Uint8Array([203, 201]), Z80_runOf(cb, 201));

export const SET1_D = Z80_mkOp("SET 1,D", new Uint8Array([203, 202]), Z80_runOf(cb, 202));

export const SET1_E = Z80_mkOp("SET 1,E", new Uint8Array([203, 203]), Z80_runOf(cb, 203));

export const SET1_H = Z80_mkOp("SET 1,H", new Uint8Array([203, 204]), Z80_runOf(cb, 204));

export const SET1_L = Z80_mkOp("SET 1,L", new Uint8Array([203, 205]), Z80_runOf(cb, 205));

export const SET1_A = Z80_mkOp("SET 1,A", new Uint8Array([203, 207]), Z80_runOf(cb, 207));

export const SET1_PTR_HL = Z80_mkOp("SET 1,(HL)", new Uint8Array([203, 206]), Z80_runOf(cb, 206));

export function SET1_PTR_IXd(d) {
    return Z80_mkOp("SET 1,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 206]), Z80_runOf(dd_cb, 206));
}

export function SET1_PTR_IYd(d) {
    return Z80_mkOp("SET 1,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 206]), Z80_runOf(fd_cb, 206));
}

export const BIT2_B = Z80_mkOp("BIT 2,B", new Uint8Array([203, 80]), Z80_runOf(cb, 80));

export const BIT2_C = Z80_mkOp("BIT 2,C", new Uint8Array([203, 81]), Z80_runOf(cb, 81));

export const BIT2_D = Z80_mkOp("BIT 2,D", new Uint8Array([203, 82]), Z80_runOf(cb, 82));

export const BIT2_E = Z80_mkOp("BIT 2,E", new Uint8Array([203, 83]), Z80_runOf(cb, 83));

export const BIT2_H = Z80_mkOp("BIT 2,H", new Uint8Array([203, 84]), Z80_runOf(cb, 84));

export const BIT2_L = Z80_mkOp("BIT 2,L", new Uint8Array([203, 85]), Z80_runOf(cb, 85));

export const BIT2_A = Z80_mkOp("BIT 2,A", new Uint8Array([203, 87]), Z80_runOf(cb, 87));

export const BIT2_PTR_HL = Z80_mkOp("BIT 2,(HL)", new Uint8Array([203, 86]), Z80_runOf(cb, 86));

export function BIT2_PTR_IXd(d) {
    return Z80_mkOp("BIT 2,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 86]), Z80_runOf(dd_cb, 86));
}

export function BIT2_PTR_IYd(d) {
    return Z80_mkOp("BIT 2,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 86]), Z80_runOf(fd_cb, 86));
}

export const RES2_B = Z80_mkOp("RES 2,B", new Uint8Array([203, 144]), Z80_runOf(cb, 144));

export const RES2_C = Z80_mkOp("RES 2,C", new Uint8Array([203, 145]), Z80_runOf(cb, 145));

export const RES2_D = Z80_mkOp("RES 2,D", new Uint8Array([203, 146]), Z80_runOf(cb, 146));

export const RES2_E = Z80_mkOp("RES 2,E", new Uint8Array([203, 147]), Z80_runOf(cb, 147));

export const RES2_H = Z80_mkOp("RES 2,H", new Uint8Array([203, 148]), Z80_runOf(cb, 148));

export const RES2_L = Z80_mkOp("RES 2,L", new Uint8Array([203, 149]), Z80_runOf(cb, 149));

export const RES2_A = Z80_mkOp("RES 2,A", new Uint8Array([203, 151]), Z80_runOf(cb, 151));

export const RES2_PTR_HL = Z80_mkOp("RES 2,(HL)", new Uint8Array([203, 150]), Z80_runOf(cb, 150));

export function RES2_PTR_IXd(d) {
    return Z80_mkOp("RES 2,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 150]), Z80_runOf(dd_cb, 150));
}

export function RES2_PTR_IYd(d) {
    return Z80_mkOp("RES 2,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 150]), Z80_runOf(fd_cb, 150));
}

export const SET2_B = Z80_mkOp("SET 2,B", new Uint8Array([203, 208]), Z80_runOf(cb, 208));

export const SET2_C = Z80_mkOp("SET 2,C", new Uint8Array([203, 209]), Z80_runOf(cb, 209));

export const SET2_D = Z80_mkOp("SET 2,D", new Uint8Array([203, 210]), Z80_runOf(cb, 210));

export const SET2_E = Z80_mkOp("SET 2,E", new Uint8Array([203, 211]), Z80_runOf(cb, 211));

export const SET2_H = Z80_mkOp("SET 2,H", new Uint8Array([203, 212]), Z80_runOf(cb, 212));

export const SET2_L = Z80_mkOp("SET 2,L", new Uint8Array([203, 213]), Z80_runOf(cb, 213));

export const SET2_A = Z80_mkOp("SET 2,A", new Uint8Array([203, 215]), Z80_runOf(cb, 215));

export const SET2_PTR_HL = Z80_mkOp("SET 2,(HL)", new Uint8Array([203, 214]), Z80_runOf(cb, 214));

export function SET2_PTR_IXd(d) {
    return Z80_mkOp("SET 2,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 214]), Z80_runOf(dd_cb, 214));
}

export function SET2_PTR_IYd(d) {
    return Z80_mkOp("SET 2,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 214]), Z80_runOf(fd_cb, 214));
}

export const BIT3_B = Z80_mkOp("BIT 3,B", new Uint8Array([203, 88]), Z80_runOf(cb, 88));

export const BIT3_C = Z80_mkOp("BIT 3,C", new Uint8Array([203, 89]), Z80_runOf(cb, 89));

export const BIT3_D = Z80_mkOp("BIT 3,D", new Uint8Array([203, 90]), Z80_runOf(cb, 90));

export const BIT3_E = Z80_mkOp("BIT 3,E", new Uint8Array([203, 91]), Z80_runOf(cb, 91));

export const BIT3_H = Z80_mkOp("BIT 3,H", new Uint8Array([203, 92]), Z80_runOf(cb, 92));

export const BIT3_L = Z80_mkOp("BIT 3,L", new Uint8Array([203, 93]), Z80_runOf(cb, 93));

export const BIT3_A = Z80_mkOp("BIT 3,A", new Uint8Array([203, 95]), Z80_runOf(cb, 95));

export const BIT3_PTR_HL = Z80_mkOp("BIT 3,(HL)", new Uint8Array([203, 94]), Z80_runOf(cb, 94));

export function BIT3_PTR_IXd(d) {
    return Z80_mkOp("BIT 3,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 94]), Z80_runOf(dd_cb, 94));
}

export function BIT3_PTR_IYd(d) {
    return Z80_mkOp("BIT 3,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 94]), Z80_runOf(fd_cb, 94));
}

export const RES3_B = Z80_mkOp("RES 3,B", new Uint8Array([203, 152]), Z80_runOf(cb, 152));

export const RES3_C = Z80_mkOp("RES 3,C", new Uint8Array([203, 153]), Z80_runOf(cb, 153));

export const RES3_D = Z80_mkOp("RES 3,D", new Uint8Array([203, 154]), Z80_runOf(cb, 154));

export const RES3_E = Z80_mkOp("RES 3,E", new Uint8Array([203, 155]), Z80_runOf(cb, 155));

export const RES3_H = Z80_mkOp("RES 3,H", new Uint8Array([203, 156]), Z80_runOf(cb, 156));

export const RES3_L = Z80_mkOp("RES 3,L", new Uint8Array([203, 157]), Z80_runOf(cb, 157));

export const RES3_A = Z80_mkOp("RES 3,A", new Uint8Array([203, 159]), Z80_runOf(cb, 159));

export const RES3_PTR_HL = Z80_mkOp("RES 3,(HL)", new Uint8Array([203, 158]), Z80_runOf(cb, 158));

export function RES3_PTR_IXd(d) {
    return Z80_mkOp("RES 3,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 158]), Z80_runOf(dd_cb, 158));
}

export function RES3_PTR_IYd(d) {
    return Z80_mkOp("RES 3,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 158]), Z80_runOf(fd_cb, 158));
}

export const SET3_B = Z80_mkOp("SET 3,B", new Uint8Array([203, 216]), Z80_runOf(cb, 216));

export const SET3_C = Z80_mkOp("SET 3,C", new Uint8Array([203, 217]), Z80_runOf(cb, 217));

export const SET3_D = Z80_mkOp("SET 3,D", new Uint8Array([203, 218]), Z80_runOf(cb, 218));

export const SET3_E = Z80_mkOp("SET 3,E", new Uint8Array([203, 219]), Z80_runOf(cb, 219));

export const SET3_H = Z80_mkOp("SET 3,H", new Uint8Array([203, 220]), Z80_runOf(cb, 220));

export const SET3_L = Z80_mkOp("SET 3,L", new Uint8Array([203, 221]), Z80_runOf(cb, 221));

export const SET3_A = Z80_mkOp("SET 3,A", new Uint8Array([203, 223]), Z80_runOf(cb, 223));

export const SET3_PTR_HL = Z80_mkOp("SET 3,(HL)", new Uint8Array([203, 222]), Z80_runOf(cb, 222));

export function SET3_PTR_IXd(d) {
    return Z80_mkOp("SET 3,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 222]), Z80_runOf(dd_cb, 222));
}

export function SET3_PTR_IYd(d) {
    return Z80_mkOp("SET 3,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 222]), Z80_runOf(fd_cb, 222));
}

export const BIT4_B = Z80_mkOp("BIT 4,B", new Uint8Array([203, 96]), Z80_runOf(cb, 96));

export const BIT4_C = Z80_mkOp("BIT 4,C", new Uint8Array([203, 97]), Z80_runOf(cb, 97));

export const BIT4_D = Z80_mkOp("BIT 4,D", new Uint8Array([203, 98]), Z80_runOf(cb, 98));

export const BIT4_E = Z80_mkOp("BIT 4,E", new Uint8Array([203, 99]), Z80_runOf(cb, 99));

export const BIT4_H = Z80_mkOp("BIT 4,H", new Uint8Array([203, 100]), Z80_runOf(cb, 100));

export const BIT4_L = Z80_mkOp("BIT 4,L", new Uint8Array([203, 101]), Z80_runOf(cb, 101));

export const BIT4_A = Z80_mkOp("BIT 4,A", new Uint8Array([203, 103]), Z80_runOf(cb, 103));

export const BIT4_PTR_HL = Z80_mkOp("BIT 4,(HL)", new Uint8Array([203, 102]), Z80_runOf(cb, 102));

export function BIT4_PTR_IXd(d) {
    return Z80_mkOp("BIT 4,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 102]), Z80_runOf(dd_cb, 102));
}

export function BIT4_PTR_IYd(d) {
    return Z80_mkOp("BIT 4,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 102]), Z80_runOf(fd_cb, 102));
}

export const RES4_B = Z80_mkOp("RES 4,B", new Uint8Array([203, 160]), Z80_runOf(cb, 160));

export const RES4_C = Z80_mkOp("RES 4,C", new Uint8Array([203, 161]), Z80_runOf(cb, 161));

export const RES4_D = Z80_mkOp("RES 4,D", new Uint8Array([203, 162]), Z80_runOf(cb, 162));

export const RES4_E = Z80_mkOp("RES 4,E", new Uint8Array([203, 163]), Z80_runOf(cb, 163));

export const RES4_H = Z80_mkOp("RES 4,H", new Uint8Array([203, 164]), Z80_runOf(cb, 164));

export const RES4_L = Z80_mkOp("RES 4,L", new Uint8Array([203, 165]), Z80_runOf(cb, 165));

export const RES4_A = Z80_mkOp("RES 4,A", new Uint8Array([203, 167]), Z80_runOf(cb, 167));

export const RES4_PTR_HL = Z80_mkOp("RES 4,(HL)", new Uint8Array([203, 166]), Z80_runOf(cb, 166));

export function RES4_PTR_IXd(d) {
    return Z80_mkOp("RES 4,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 166]), Z80_runOf(dd_cb, 166));
}

export function RES4_PTR_IYd(d) {
    return Z80_mkOp("RES 4,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 166]), Z80_runOf(fd_cb, 166));
}

export const SET4_B = Z80_mkOp("SET 4,B", new Uint8Array([203, 224]), Z80_runOf(cb, 224));

export const SET4_C = Z80_mkOp("SET 4,C", new Uint8Array([203, 225]), Z80_runOf(cb, 225));

export const SET4_D = Z80_mkOp("SET 4,D", new Uint8Array([203, 226]), Z80_runOf(cb, 226));

export const SET4_E = Z80_mkOp("SET 4,E", new Uint8Array([203, 227]), Z80_runOf(cb, 227));

export const SET4_H = Z80_mkOp("SET 4,H", new Uint8Array([203, 228]), Z80_runOf(cb, 228));

export const SET4_L = Z80_mkOp("SET 4,L", new Uint8Array([203, 229]), Z80_runOf(cb, 229));

export const SET4_A = Z80_mkOp("SET 4,A", new Uint8Array([203, 231]), Z80_runOf(cb, 231));

export const SET4_PTR_HL = Z80_mkOp("SET 4,(HL)", new Uint8Array([203, 230]), Z80_runOf(cb, 230));

export function SET4_PTR_IXd(d) {
    return Z80_mkOp("SET 4,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 230]), Z80_runOf(dd_cb, 230));
}

export function SET4_PTR_IYd(d) {
    return Z80_mkOp("SET 4,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 230]), Z80_runOf(fd_cb, 230));
}

export const BIT5_B = Z80_mkOp("BIT 5,B", new Uint8Array([203, 104]), Z80_runOf(cb, 104));

export const BIT5_C = Z80_mkOp("BIT 5,C", new Uint8Array([203, 105]), Z80_runOf(cb, 105));

export const BIT5_D = Z80_mkOp("BIT 5,D", new Uint8Array([203, 106]), Z80_runOf(cb, 106));

export const BIT5_E = Z80_mkOp("BIT 5,E", new Uint8Array([203, 107]), Z80_runOf(cb, 107));

export const BIT5_H = Z80_mkOp("BIT 5,H", new Uint8Array([203, 108]), Z80_runOf(cb, 108));

export const BIT5_L = Z80_mkOp("BIT 5,L", new Uint8Array([203, 109]), Z80_runOf(cb, 109));

export const BIT5_A = Z80_mkOp("BIT 5,A", new Uint8Array([203, 111]), Z80_runOf(cb, 111));

export const BIT5_PTR_HL = Z80_mkOp("BIT 5,(HL)", new Uint8Array([203, 110]), Z80_runOf(cb, 110));

export function BIT5_PTR_IXd(d) {
    return Z80_mkOp("BIT 5,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 110]), Z80_runOf(dd_cb, 110));
}

export function BIT5_PTR_IYd(d) {
    return Z80_mkOp("BIT 5,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 110]), Z80_runOf(fd_cb, 110));
}

export const RES5_B = Z80_mkOp("RES 5,B", new Uint8Array([203, 168]), Z80_runOf(cb, 168));

export const RES5_C = Z80_mkOp("RES 5,C", new Uint8Array([203, 169]), Z80_runOf(cb, 169));

export const RES5_D = Z80_mkOp("RES 5,D", new Uint8Array([203, 170]), Z80_runOf(cb, 170));

export const RES5_E = Z80_mkOp("RES 5,E", new Uint8Array([203, 171]), Z80_runOf(cb, 171));

export const RES5_H = Z80_mkOp("RES 5,H", new Uint8Array([203, 172]), Z80_runOf(cb, 172));

export const RES5_L = Z80_mkOp("RES 5,L", new Uint8Array([203, 173]), Z80_runOf(cb, 173));

export const RES5_A = Z80_mkOp("RES 5,A", new Uint8Array([203, 175]), Z80_runOf(cb, 175));

export const RES5_PTR_HL = Z80_mkOp("RES 5,(HL)", new Uint8Array([203, 174]), Z80_runOf(cb, 174));

export function RES5_PTR_IXd(d) {
    return Z80_mkOp("RES 5,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 174]), Z80_runOf(dd_cb, 174));
}

export function RES5_PTR_IYd(d) {
    return Z80_mkOp("RES 5,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 174]), Z80_runOf(fd_cb, 174));
}

export const SET5_B = Z80_mkOp("SET 5,B", new Uint8Array([203, 232]), Z80_runOf(cb, 232));

export const SET5_C = Z80_mkOp("SET 5,C", new Uint8Array([203, 233]), Z80_runOf(cb, 233));

export const SET5_D = Z80_mkOp("SET 5,D", new Uint8Array([203, 234]), Z80_runOf(cb, 234));

export const SET5_E = Z80_mkOp("SET 5,E", new Uint8Array([203, 235]), Z80_runOf(cb, 235));

export const SET5_H = Z80_mkOp("SET 5,H", new Uint8Array([203, 236]), Z80_runOf(cb, 236));

export const SET5_L = Z80_mkOp("SET 5,L", new Uint8Array([203, 237]), Z80_runOf(cb, 237));

export const SET5_A = Z80_mkOp("SET 5,A", new Uint8Array([203, 239]), Z80_runOf(cb, 239));

export const SET5_PTR_HL = Z80_mkOp("SET 5,(HL)", new Uint8Array([203, 238]), Z80_runOf(cb, 238));

export function SET5_PTR_IXd(d) {
    return Z80_mkOp("SET 5,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 238]), Z80_runOf(dd_cb, 238));
}

export function SET5_PTR_IYd(d) {
    return Z80_mkOp("SET 5,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 238]), Z80_runOf(fd_cb, 238));
}

export const BIT6_B = Z80_mkOp("BIT 6,B", new Uint8Array([203, 112]), Z80_runOf(cb, 112));

export const BIT6_C = Z80_mkOp("BIT 6,C", new Uint8Array([203, 113]), Z80_runOf(cb, 113));

export const BIT6_D = Z80_mkOp("BIT 6,D", new Uint8Array([203, 114]), Z80_runOf(cb, 114));

export const BIT6_E = Z80_mkOp("BIT 6,E", new Uint8Array([203, 115]), Z80_runOf(cb, 115));

export const BIT6_H = Z80_mkOp("BIT 6,H", new Uint8Array([203, 116]), Z80_runOf(cb, 116));

export const BIT6_L = Z80_mkOp("BIT 6,L", new Uint8Array([203, 117]), Z80_runOf(cb, 117));

export const BIT6_A = Z80_mkOp("BIT 6,A", new Uint8Array([203, 119]), Z80_runOf(cb, 119));

export const BIT6_PTR_HL = Z80_mkOp("BIT 6,(HL)", new Uint8Array([203, 118]), Z80_runOf(cb, 118));

export function BIT6_PTR_IXd(d) {
    return Z80_mkOp("BIT 6,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 118]), Z80_runOf(dd_cb, 118));
}

export function BIT6_PTR_IYd(d) {
    return Z80_mkOp("BIT 6,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 118]), Z80_runOf(fd_cb, 118));
}

export const RES6_B = Z80_mkOp("RES 6,B", new Uint8Array([203, 176]), Z80_runOf(cb, 176));

export const RES6_C = Z80_mkOp("RES 6,C", new Uint8Array([203, 177]), Z80_runOf(cb, 177));

export const RES6_D = Z80_mkOp("RES 6,D", new Uint8Array([203, 178]), Z80_runOf(cb, 178));

export const RES6_E = Z80_mkOp("RES 6,E", new Uint8Array([203, 179]), Z80_runOf(cb, 179));

export const RES6_H = Z80_mkOp("RES 6,H", new Uint8Array([203, 180]), Z80_runOf(cb, 180));

export const RES6_L = Z80_mkOp("RES 6,L", new Uint8Array([203, 181]), Z80_runOf(cb, 181));

export const RES6_A = Z80_mkOp("RES 6,A", new Uint8Array([203, 183]), Z80_runOf(cb, 183));

export const RES6_PTR_HL = Z80_mkOp("RES 6,(HL)", new Uint8Array([203, 182]), Z80_runOf(cb, 182));

export function RES6_PTR_IXd(d) {
    return Z80_mkOp("RES 6,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 182]), Z80_runOf(dd_cb, 182));
}

export function RES6_PTR_IYd(d) {
    return Z80_mkOp("RES 6,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 182]), Z80_runOf(fd_cb, 182));
}

export const SET6_B = Z80_mkOp("SET 6,B", new Uint8Array([203, 240]), Z80_runOf(cb, 240));

export const SET6_C = Z80_mkOp("SET 6,C", new Uint8Array([203, 241]), Z80_runOf(cb, 241));

export const SET6_D = Z80_mkOp("SET 6,D", new Uint8Array([203, 242]), Z80_runOf(cb, 242));

export const SET6_E = Z80_mkOp("SET 6,E", new Uint8Array([203, 243]), Z80_runOf(cb, 243));

export const SET6_H = Z80_mkOp("SET 6,H", new Uint8Array([203, 244]), Z80_runOf(cb, 244));

export const SET6_L = Z80_mkOp("SET 6,L", new Uint8Array([203, 245]), Z80_runOf(cb, 245));

export const SET6_A = Z80_mkOp("SET 6,A", new Uint8Array([203, 247]), Z80_runOf(cb, 247));

export const SET6_PTR_HL = Z80_mkOp("SET 6,(HL)", new Uint8Array([203, 246]), Z80_runOf(cb, 246));

export function SET6_PTR_IXd(d) {
    return Z80_mkOp("SET 6,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 246]), Z80_runOf(dd_cb, 246));
}

export function SET6_PTR_IYd(d) {
    return Z80_mkOp("SET 6,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 246]), Z80_runOf(fd_cb, 246));
}

export const BIT7_B = Z80_mkOp("BIT 7,B", new Uint8Array([203, 120]), Z80_runOf(cb, 120));

export const BIT7_C = Z80_mkOp("BIT 7,C", new Uint8Array([203, 121]), Z80_runOf(cb, 121));

export const BIT7_D = Z80_mkOp("BIT 7,D", new Uint8Array([203, 122]), Z80_runOf(cb, 122));

export const BIT7_E = Z80_mkOp("BIT 7,E", new Uint8Array([203, 123]), Z80_runOf(cb, 123));

export const BIT7_H = Z80_mkOp("BIT 7,H", new Uint8Array([203, 124]), Z80_runOf(cb, 124));

export const BIT7_L = Z80_mkOp("BIT 7,L", new Uint8Array([203, 125]), Z80_runOf(cb, 125));

export const BIT7_A = Z80_mkOp("BIT 7,A", new Uint8Array([203, 127]), Z80_runOf(cb, 127));

export const BIT7_PTR_HL = Z80_mkOp("BIT 7,(HL)", new Uint8Array([203, 126]), Z80_runOf(cb, 126));

export function BIT7_PTR_IXd(d) {
    return Z80_mkOp("BIT 7,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 126]), Z80_runOf(dd_cb, 126));
}

export function BIT7_PTR_IYd(d) {
    return Z80_mkOp("BIT 7,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 126]), Z80_runOf(fd_cb, 126));
}

export const RES7_B = Z80_mkOp("RES 7,B", new Uint8Array([203, 184]), Z80_runOf(cb, 184));

export const RES7_C = Z80_mkOp("RES 7,C", new Uint8Array([203, 185]), Z80_runOf(cb, 185));

export const RES7_D = Z80_mkOp("RES 7,D", new Uint8Array([203, 186]), Z80_runOf(cb, 186));

export const RES7_E = Z80_mkOp("RES 7,E", new Uint8Array([203, 187]), Z80_runOf(cb, 187));

export const RES7_H = Z80_mkOp("RES 7,H", new Uint8Array([203, 188]), Z80_runOf(cb, 188));

export const RES7_L = Z80_mkOp("RES 7,L", new Uint8Array([203, 189]), Z80_runOf(cb, 189));

export const RES7_A = Z80_mkOp("RES 7,A", new Uint8Array([203, 191]), Z80_runOf(cb, 191));

export const RES7_PTR_HL = Z80_mkOp("RES 7,(HL)", new Uint8Array([203, 190]), Z80_runOf(cb, 190));

export function RES7_PTR_IXd(d) {
    return Z80_mkOp("RES 7,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 190]), Z80_runOf(dd_cb, 190));
}

export function RES7_PTR_IYd(d) {
    return Z80_mkOp("RES 7,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 190]), Z80_runOf(fd_cb, 190));
}

export const SET7_B = Z80_mkOp("SET 7,B", new Uint8Array([203, 248]), Z80_runOf(cb, 248));

export const SET7_C = Z80_mkOp("SET 7,C", new Uint8Array([203, 249]), Z80_runOf(cb, 249));

export const SET7_D = Z80_mkOp("SET 7,D", new Uint8Array([203, 250]), Z80_runOf(cb, 250));

export const SET7_E = Z80_mkOp("SET 7,E", new Uint8Array([203, 251]), Z80_runOf(cb, 251));

export const SET7_H = Z80_mkOp("SET 7,H", new Uint8Array([203, 252]), Z80_runOf(cb, 252));

export const SET7_L = Z80_mkOp("SET 7,L", new Uint8Array([203, 253]), Z80_runOf(cb, 253));

export const SET7_A = Z80_mkOp("SET 7,A", new Uint8Array([203, 255]), Z80_runOf(cb, 255));

export const SET7_PTR_HL = Z80_mkOp("SET 7,(HL)", new Uint8Array([203, 254]), Z80_runOf(cb, 254));

export function SET7_PTR_IXd(d) {
    return Z80_mkOp("SET 7,(IX+d)", new Uint8Array([221, 203, (d & 255) & 0xFF, 254]), Z80_runOf(dd_cb, 254));
}

export function SET7_PTR_IYd(d) {
    return Z80_mkOp("SET 7,(IY+d)", new Uint8Array([253, 203, (d & 255) & 0xFF, 254]), Z80_runOf(fd_cb, 254));
}

export function JP_nn(nn) {
    return Z80_mkOp("JP nn", new Uint8Array([195, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 195));
}

export function JP_NZ(nn) {
    return Z80_mkOp("JP NZ,nn", new Uint8Array([194, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 194));
}

export function JP_Z(nn) {
    return Z80_mkOp("JP Z,nn", new Uint8Array([202, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 202));
}

export function JP_NC(nn) {
    return Z80_mkOp("JP NC,nn", new Uint8Array([210, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 210));
}

export function JP_C(nn) {
    return Z80_mkOp("JP C,nn", new Uint8Array([218, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 218));
}

export function JP_PO(nn) {
    return Z80_mkOp("JP PO,nn", new Uint8Array([226, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 226));
}

export function JP_PE(nn) {
    return Z80_mkOp("JP PE,nn", new Uint8Array([234, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 234));
}

export function JP_P(nn) {
    return Z80_mkOp("JP P,nn", new Uint8Array([242, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 242));
}

export function JP_M(nn) {
    return Z80_mkOp("JP M,nn", new Uint8Array([250, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 250));
}

export const JP_HL = Z80_mkOp("JP (HL)", new Uint8Array([233]), Z80_runOf(main, 233));

export const JP_IX = Z80_mkOp("JP (IX)", new Uint8Array([221, 233]), Z80_runOf(dd, 233));

export const JP_IY = Z80_mkOp("JP (IY)", new Uint8Array([253, 233]), Z80_runOf(fd, 233));

export function JR_e(e) {
    return Z80_mkOp("JR e", new Uint8Array([24, (e & 255) & 0xFF]), Z80_runOf(main, 24));
}

export function JR_NZ(e) {
    return Z80_mkOp("JR NZ,e", new Uint8Array([32, (e & 255) & 0xFF]), Z80_runOf(main, 32));
}

export function JR_Z(e) {
    return Z80_mkOp("JR Z,e", new Uint8Array([40, (e & 255) & 0xFF]), Z80_runOf(main, 40));
}

export function JR_NC(e) {
    return Z80_mkOp("JR NC,e", new Uint8Array([48, (e & 255) & 0xFF]), Z80_runOf(main, 48));
}

export function JR_C(e) {
    return Z80_mkOp("JR C,e", new Uint8Array([56, (e & 255) & 0xFF]), Z80_runOf(main, 56));
}

export function DJNZ_e(e) {
    return Z80_mkOp("DJNZ e", new Uint8Array([16, (e & 255) & 0xFF]), Z80_runOf(main, 16));
}

export function CALL_nn(nn) {
    return Z80_mkOp("CALL nn", new Uint8Array([205, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 205));
}

export function CALL_NZ(nn) {
    return Z80_mkOp("CALL NZ,nn", new Uint8Array([196, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 196));
}

export function CALL_Z(nn) {
    return Z80_mkOp("CALL Z,nn", new Uint8Array([204, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 204));
}

export function CALL_NC(nn) {
    return Z80_mkOp("CALL NC,nn", new Uint8Array([212, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 212));
}

export function CALL_C(nn) {
    return Z80_mkOp("CALL C,nn", new Uint8Array([220, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 220));
}

export function CALL_PO(nn) {
    return Z80_mkOp("CALL PO,nn", new Uint8Array([228, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 228));
}

export function CALL_PE(nn) {
    return Z80_mkOp("CALL PE,nn", new Uint8Array([236, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 236));
}

export function CALL_P(nn) {
    return Z80_mkOp("CALL P,nn", new Uint8Array([244, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 244));
}

export function CALL_M(nn) {
    return Z80_mkOp("CALL M,nn", new Uint8Array([252, (nn & 255) & 0xFF, ((nn >> 8) & 255) & 0xFF]), Z80_runOf(main, 252));
}

export const RET = Z80_mkOp("RET", new Uint8Array([201]), Z80_runOf(main, 201));

export const RET_NZ = Z80_mkOp("RET NZ", new Uint8Array([192]), Z80_runOf(main, 192));

export const RET_Z = Z80_mkOp("RET Z", new Uint8Array([200]), Z80_runOf(main, 200));

export const RET_NC = Z80_mkOp("RET NC", new Uint8Array([208]), Z80_runOf(main, 208));

export const RET_C = Z80_mkOp("RET C", new Uint8Array([216]), Z80_runOf(main, 216));

export const RET_PO = Z80_mkOp("RET PO", new Uint8Array([224]), Z80_runOf(main, 224));

export const RET_PE = Z80_mkOp("RET PE", new Uint8Array([232]), Z80_runOf(main, 232));

export const RET_P = Z80_mkOp("RET P", new Uint8Array([240]), Z80_runOf(main, 240));

export const RET_M = Z80_mkOp("RET M", new Uint8Array([248]), Z80_runOf(main, 248));

export const RETI = Z80_mkOp("RETI", new Uint8Array([237, 77]), Z80_runOf(ed, 77));

export const RETN = Z80_mkOp("RETN", new Uint8Array([237, 69]), Z80_runOf(ed, 69));

export const RST_00 = Z80_mkOp("RST 0x00", new Uint8Array([199]), Z80_runOf(main, 199));

export const RST_08 = Z80_mkOp("RST 0x08", new Uint8Array([207]), Z80_runOf(main, 207));

export const RST_10 = Z80_mkOp("RST 0x10", new Uint8Array([215]), Z80_runOf(main, 215));

export const RST_18 = Z80_mkOp("RST 0x18", new Uint8Array([223]), Z80_runOf(main, 223));

export const RST_20 = Z80_mkOp("RST 0x20", new Uint8Array([231]), Z80_runOf(main, 231));

export const RST_28 = Z80_mkOp("RST 0x28", new Uint8Array([239]), Z80_runOf(main, 239));

export const RST_30 = Z80_mkOp("RST 0x30", new Uint8Array([247]), Z80_runOf(main, 247));

export const RST_38 = Z80_mkOp("RST 0x38", new Uint8Array([255]), Z80_runOf(main, 255));

export const NOP = Z80_mkOp("NOP", new Uint8Array([0]), Z80_runOf(main, 0));

export const HALT = Z80_mkOp("HALT", new Uint8Array([118]), Z80_runOf(main, 118));

export const DI = Z80_mkOp("DI", new Uint8Array([243]), Z80_runOf(main, 243));

export const EI = Z80_mkOp("EI", new Uint8Array([251]), Z80_runOf(main, 251));

export const SCF = Z80_mkOp("SCF", new Uint8Array([55]), Z80_runOf(main, 55));

export const CCF = Z80_mkOp("CCF", new Uint8Array([63]), Z80_runOf(main, 63));

export const CPL = Z80_mkOp("CPL", new Uint8Array([47]), Z80_runOf(main, 47));

export const DAA = Z80_mkOp("DAA", new Uint8Array([39]), Z80_runOf(main, 39));

export const NEG = Z80_mkOp("NEG", new Uint8Array([237, 68]), Z80_runOf(ed, 68));

export const EX_DE_HL = Z80_mkOp("EX DE,HL", new Uint8Array([235]), Z80_runOf(main, 235));

export const EX_AF_AF = Z80_mkOp("EX AF,AF\'", new Uint8Array([8]), Z80_runOf(main, 8));

export const EXX = Z80_mkOp("EXX", new Uint8Array([217]), Z80_runOf(main, 217));

export const RLCA = Z80_mkOp("RLCA", new Uint8Array([7]), Z80_runOf(main, 7));

export const RRCA = Z80_mkOp("RRCA", new Uint8Array([15]), Z80_runOf(main, 15));

export const RLA = Z80_mkOp("RLA", new Uint8Array([23]), Z80_runOf(main, 23));

export const RRA = Z80_mkOp("RRA", new Uint8Array([31]), Z80_runOf(main, 31));

export const IM_0 = Z80_mkOp("IM 0", new Uint8Array([237, 70]), Z80_runOf(ed, 70));

export const IM_1 = Z80_mkOp("IM 1", new Uint8Array([237, 86]), Z80_runOf(ed, 86));

export const IM_2 = Z80_mkOp("IM 2", new Uint8Array([237, 94]), Z80_runOf(ed, 94));

export const RRD = Z80_mkOp("RRD", new Uint8Array([237, 103]), Z80_runOf(ed, 103));

export const RLD = Z80_mkOp("RLD", new Uint8Array([237, 111]), Z80_runOf(ed, 111));

export const LDI = Z80_mkOp("LDI", new Uint8Array([237, 160]), Z80_runOf(ed, 160));

export const LDD = Z80_mkOp("LDD", new Uint8Array([237, 168]), Z80_runOf(ed, 168));

export const LDIR = Z80_mkOp("LDIR", new Uint8Array([237, 176]), Z80_runOf(ed, 176));

export const LDDR = Z80_mkOp("LDDR", new Uint8Array([237, 184]), Z80_runOf(ed, 184));

export const CPI = Z80_mkOp("CPI", new Uint8Array([237, 161]), Z80_runOf(ed, 161));

export const CPD = Z80_mkOp("CPD", new Uint8Array([237, 169]), Z80_runOf(ed, 169));

export const CPIR = Z80_mkOp("CPIR", new Uint8Array([237, 177]), Z80_runOf(ed, 177));

export const CPDR = Z80_mkOp("CPDR", new Uint8Array([237, 185]), Z80_runOf(ed, 185));

export const EX_SP_HL = Z80_mkOp("EX (SP),HL", new Uint8Array([227]), Z80_runOf(main, 227));

export const EX_SP_IX = Z80_mkOp("EX (SP),IX", new Uint8Array([221, 227]), Z80_runOf(dd, 227));

export const EX_SP_IY = Z80_mkOp("EX (SP),IY", new Uint8Array([253, 227]), Z80_runOf(fd, 227));

export function IN_A_n(n) {
    return Z80_mkOp("IN A,(n)", new Uint8Array([219, (n & 255) & 0xFF]), Z80_runOf(main, 219));
}

export function OUT_n_A(n) {
    return Z80_mkOp("OUT (n),A", new Uint8Array([211, (n & 255) & 0xFF]), Z80_runOf(main, 211));
}

export const IN_B_C = Z80_mkOp("IN B,(C)", new Uint8Array([237, 64]), Z80_runOf(ed, 64));

export const OUT_C_B = Z80_mkOp("OUT (C),B", new Uint8Array([237, 65]), Z80_runOf(ed, 65));

export const IN_C_C = Z80_mkOp("IN C,(C)", new Uint8Array([237, 72]), Z80_runOf(ed, 72));

export const OUT_C_C = Z80_mkOp("OUT (C),C", new Uint8Array([237, 73]), Z80_runOf(ed, 73));

export const IN_D_C = Z80_mkOp("IN D,(C)", new Uint8Array([237, 80]), Z80_runOf(ed, 80));

export const OUT_C_D = Z80_mkOp("OUT (C),D", new Uint8Array([237, 81]), Z80_runOf(ed, 81));

export const IN_E_C = Z80_mkOp("IN E,(C)", new Uint8Array([237, 88]), Z80_runOf(ed, 88));

export const OUT_C_E = Z80_mkOp("OUT (C),E", new Uint8Array([237, 89]), Z80_runOf(ed, 89));

export const IN_H_C = Z80_mkOp("IN H,(C)", new Uint8Array([237, 96]), Z80_runOf(ed, 96));

export const OUT_C_H = Z80_mkOp("OUT (C),H", new Uint8Array([237, 97]), Z80_runOf(ed, 97));

export const IN_L_C = Z80_mkOp("IN L,(C)", new Uint8Array([237, 104]), Z80_runOf(ed, 104));

export const OUT_C_L = Z80_mkOp("OUT (C),L", new Uint8Array([237, 105]), Z80_runOf(ed, 105));

export const IN_A_C = Z80_mkOp("IN A,(C)", new Uint8Array([237, 120]), Z80_runOf(ed, 120));

export const OUT_C_A = Z80_mkOp("OUT (C),A", new Uint8Array([237, 121]), Z80_runOf(ed, 121));

export const IN_PTR_C = Z80_mkOp("IN (C)", new Uint8Array([237, 112]), Z80_runOf(ed, 112));

export const OUT_C_0 = Z80_mkOp("OUT (C),0", new Uint8Array([237, 113]), Z80_runOf(ed, 113));

export const allOps = ofArray([["LD_B_B", LD_B_B, "default"], ["LD_B_C", LD_B_C, "default"], ["LD_B_D", LD_B_D, "default"], ["LD_B_E", LD_B_E, "default"], ["LD_B_H", LD_B_H, "default"], ["LD_B_L", LD_B_L, "default"], ["LD_B_A", LD_B_A, "default"], ["LD_C_B", LD_C_B, "default"], ["LD_C_C", LD_C_C, "default"], ["LD_C_D", LD_C_D, "default"], ["LD_C_E", LD_C_E, "default"], ["LD_C_H", LD_C_H, "default"], ["LD_C_L", LD_C_L, "default"], ["LD_C_A", LD_C_A, "default"], ["LD_D_B", LD_D_B, "default"], ["LD_D_C", LD_D_C, "default"], ["LD_D_D", LD_D_D, "default"], ["LD_D_E", LD_D_E, "default"], ["LD_D_H", LD_D_H, "default"], ["LD_D_L", LD_D_L, "default"], ["LD_D_A", LD_D_A, "default"], ["LD_E_B", LD_E_B, "default"], ["LD_E_C", LD_E_C, "default"], ["LD_E_D", LD_E_D, "default"], ["LD_E_E", LD_E_E, "default"], ["LD_E_H", LD_E_H, "default"], ["LD_E_L", LD_E_L, "default"], ["LD_E_A", LD_E_A, "default"], ["LD_H_B", LD_H_B, "default"], ["LD_H_C", LD_H_C, "default"], ["LD_H_D", LD_H_D, "default"], ["LD_H_E", LD_H_E, "default"], ["LD_H_H", LD_H_H, "default"], ["LD_H_L", LD_H_L, "default"], ["LD_H_A", LD_H_A, "default"], ["LD_L_B", LD_L_B, "default"], ["LD_L_C", LD_L_C, "default"], ["LD_L_D", LD_L_D, "default"], ["LD_L_E", LD_L_E, "default"], ["LD_L_H", LD_L_H, "default"], ["LD_L_L", LD_L_L, "default"], ["LD_L_A", LD_L_A, "default"], ["LD_A_B", LD_A_B, "default"], ["LD_A_C", LD_A_C, "default"], ["LD_A_D", LD_A_D, "default"], ["LD_A_E", LD_A_E, "default"], ["LD_A_H", LD_A_H, "default"], ["LD_A_L", LD_A_L, "default"], ["LD_A_A", LD_A_A, "default"], ["LD_B_PTR_HL", LD_B_PTR_HL, "hl"], ["LD_C_PTR_HL", LD_C_PTR_HL, "hl"], ["LD_D_PTR_HL", LD_D_PTR_HL, "hl"], ["LD_E_PTR_HL", LD_E_PTR_HL, "hl"], ["LD_H_PTR_HL", LD_H_PTR_HL, "hl"], ["LD_L_PTR_HL", LD_L_PTR_HL, "hl"], ["LD_A_PTR_HL", LD_A_PTR_HL, "hl"], ["LD_PTR_HL_B", LD_PTR_HL_B, "hl"], ["LD_PTR_HL_C", LD_PTR_HL_C, "hl"], ["LD_PTR_HL_D", LD_PTR_HL_D, "hl"], ["LD_PTR_HL_E", LD_PTR_HL_E, "hl"], ["LD_PTR_HL_H", LD_PTR_HL_H, "hl"], ["LD_PTR_HL_L", LD_PTR_HL_L, "hl"], ["LD_PTR_HL_A", LD_PTR_HL_A, "hl"], ["LD_B", LD_B(85), "default"], ["LD_C", LD_C(85), "default"], ["LD_D", LD_D(85), "default"], ["LD_E", LD_E(85), "default"], ["LD_H", LD_H(85), "default"], ["LD_L", LD_L(85), "default"], ["LD_A", LD_A(85), "default"], ["LD_A_ptr", LD_A_ptr(4660), "nn"], ["LD_ptr_A", LD_ptr_A(4660), "nn"], ["LD_A_BC", LD_A_BC, "hl"], ["LD_A_DE", LD_A_DE, "hl"], ["LD_BC_A", LD_BC_A, "hl"], ["LD_DE_A", LD_DE_A, "hl"], ["LD_BC", LD_BC(4660), "default"], ["LD_DE", LD_DE(4660), "default"], ["LD_HL", LD_HL(4660), "default"], ["LD_SP", LD_SP(4660), "default"], ["LD_IX", LD_IX(4660), "default"], ["LD_IY", LD_IY(4660), "default"], ["LD_HL_ptr", LD_HL_ptr(4660), "nn"], ["LD_ptr_HL", LD_ptr_HL(4660), "nn"], ["LD_IX_ptr", LD_IX_ptr(4660), "nn"], ["LD_ptr_IX", LD_ptr_IX(4660), "nn"], ["LD_IY_ptr", LD_IY_ptr(4660), "nn"], ["LD_ptr_IY", LD_ptr_IY(4660), "nn"], ["LD_ptr_BC", LD_ptr_BC(4660), "nn"], ["LD_BC_ptr", LD_BC_ptr(4660), "nn"], ["LD_ptr_DE", LD_ptr_DE(4660), "nn"], ["LD_DE_ptr", LD_DE_ptr(4660), "nn"], ["LD_ptr_SP", LD_ptr_SP(4660), "nn"], ["LD_SP_ptr", LD_SP_ptr(4660), "nn"], ["LD_SP_HL", LD_SP_HL, "default"], ["LD_SP_IX", LD_SP_IX, "default"], ["LD_SP_IY", LD_SP_IY, "default"], ["LD_A_I", LD_A_I, "default"], ["LD_A_R", LD_A_R, "default"], ["LD_I_A", LD_I_A, "default"], ["LD_R_A", LD_R_A, "default"], ["LD_B_IXH", LD_B_IXH, "default"], ["LD_B_IXL", LD_B_IXL, "default"], ["LD_C_IXH", LD_C_IXH, "default"], ["LD_C_IXL", LD_C_IXL, "default"], ["LD_D_IXH", LD_D_IXH, "default"], ["LD_D_IXL", LD_D_IXL, "default"], ["LD_E_IXH", LD_E_IXH, "default"], ["LD_E_IXL", LD_E_IXL, "default"], ["LD_IXH_B", LD_IXH_B, "default"], ["LD_IXH_C", LD_IXH_C, "default"], ["LD_IXH_D", LD_IXH_D, "default"], ["LD_IXH_E", LD_IXH_E, "default"], ["LD_IXH_A", LD_IXH_A, "default"], ["LD_IXL_B", LD_IXL_B, "default"], ["LD_IXL_C", LD_IXL_C, "default"], ["LD_IXL_D", LD_IXL_D, "default"], ["LD_IXL_E", LD_IXL_E, "default"], ["LD_IXL_A", LD_IXL_A, "default"], ["LD_IXH_n", LD_IXH_n(85), "default"], ["LD_IXL_n", LD_IXL_n(85), "default"], ["LD_B_IXd", LD_B_IXd(85), "ixd"], ["LD_C_IXd", LD_C_IXd(85), "ixd"], ["LD_D_IXd", LD_D_IXd(85), "ixd"], ["LD_E_IXd", LD_E_IXd(85), "ixd"], ["LD_H_IXd", LD_H_IXd(85), "ixd"], ["LD_L_IXd", LD_L_IXd(85), "ixd"], ["LD_A_IXd", LD_A_IXd(85), "ixd"], ["LD_B_IYd", LD_B_IYd(85), "iyd"], ["LD_C_IYd", LD_C_IYd(85), "iyd"], ["LD_D_IYd", LD_D_IYd(85), "iyd"], ["LD_E_IYd", LD_E_IYd(85), "iyd"], ["LD_H_IYd", LD_H_IYd(85), "iyd"], ["LD_L_IYd", LD_L_IYd(85), "iyd"], ["LD_A_IYd", LD_A_IYd(85), "iyd"], ["LD_IXd_B", LD_IXd_B(85), "ixd"], ["LD_IXd_C", LD_IXd_C(85), "ixd"], ["LD_IXd_D", LD_IXd_D(85), "ixd"], ["LD_IXd_E", LD_IXd_E(85), "ixd"], ["LD_IXd_H", LD_IXd_H(85), "ixd"], ["LD_IXd_L", LD_IXd_L(85), "ixd"], ["LD_IXd_A", LD_IXd_A(85), "ixd"], ["LD_IYd_B", LD_IYd_B(85), "iyd"], ["LD_IYd_C", LD_IYd_C(85), "iyd"], ["LD_IYd_D", LD_IYd_D(85), "iyd"], ["LD_IYd_E", LD_IYd_E(85), "iyd"], ["LD_IYd_H", LD_IYd_H(85), "iyd"], ["LD_IYd_L", LD_IYd_L(85), "iyd"], ["LD_IYd_A", LD_IYd_A(85), "iyd"], ["ADD_A_B", ADD_A_B, "default"], ["ADD_A_C", ADD_A_C, "default"], ["ADD_A_D", ADD_A_D, "default"], ["ADD_A_E", ADD_A_E, "default"], ["ADD_A_H", ADD_A_H, "default"], ["ADD_A_L", ADD_A_L, "default"], ["ADD_A_A", ADD_A_A, "default"], ["ADD_A_PTR_HL", ADD_A_PTR_HL, "hl"], ["ADD_A_n", ADD_A_n(85), "default"], ["ADD_A_IXd", ADD_A_IXd(85), "ixd"], ["ADD_A_IYd", ADD_A_IYd(85), "iyd"], ["ADC_A_B", ADC_A_B, "default"], ["ADC_A_C", ADC_A_C, "default"], ["ADC_A_D", ADC_A_D, "default"], ["ADC_A_E", ADC_A_E, "default"], ["ADC_A_H", ADC_A_H, "default"], ["ADC_A_L", ADC_A_L, "default"], ["ADC_A_A", ADC_A_A, "default"], ["ADC_A_PTR_HL", ADC_A_PTR_HL, "hl"], ["ADC_A_n", ADC_A_n(85), "default"], ["ADC_A_IXd", ADC_A_IXd(85), "ixd"], ["ADC_A_IYd", ADC_A_IYd(85), "iyd"], ["SUB_A_B", SUB_A_B, "default"], ["SUB_A_C", SUB_A_C, "default"], ["SUB_A_D", SUB_A_D, "default"], ["SUB_A_E", SUB_A_E, "default"], ["SUB_A_H", SUB_A_H, "default"], ["SUB_A_L", SUB_A_L, "default"], ["SUB_A_A", SUB_A_A, "default"], ["SUB_A_PTR_HL", SUB_A_PTR_HL, "hl"], ["SUB_A_n", SUB_A_n(85), "default"], ["SUB_A_IXd", SUB_A_IXd(85), "ixd"], ["SUB_A_IYd", SUB_A_IYd(85), "iyd"], ["SBC_A_B", SBC_A_B, "default"], ["SBC_A_C", SBC_A_C, "default"], ["SBC_A_D", SBC_A_D, "default"], ["SBC_A_E", SBC_A_E, "default"], ["SBC_A_H", SBC_A_H, "default"], ["SBC_A_L", SBC_A_L, "default"], ["SBC_A_A", SBC_A_A, "default"], ["SBC_A_PTR_HL", SBC_A_PTR_HL, "hl"], ["SBC_A_n", SBC_A_n(85), "default"], ["SBC_A_IXd", SBC_A_IXd(85), "ixd"], ["SBC_A_IYd", SBC_A_IYd(85), "iyd"], ["AND_A_B", AND_A_B, "default"], ["AND_A_C", AND_A_C, "default"], ["AND_A_D", AND_A_D, "default"], ["AND_A_E", AND_A_E, "default"], ["AND_A_H", AND_A_H, "default"], ["AND_A_L", AND_A_L, "default"], ["AND_A_A", AND_A_A, "default"], ["AND_A_PTR_HL", AND_A_PTR_HL, "hl"], ["AND_A_n", AND_A_n(85), "default"], ["AND_A_IXd", AND_A_IXd(85), "ixd"], ["AND_A_IYd", AND_A_IYd(85), "iyd"], ["XOR_A_B", XOR_A_B, "default"], ["XOR_A_C", XOR_A_C, "default"], ["XOR_A_D", XOR_A_D, "default"], ["XOR_A_E", XOR_A_E, "default"], ["XOR_A_H", XOR_A_H, "default"], ["XOR_A_L", XOR_A_L, "default"], ["XOR_A_A", XOR_A_A, "default"], ["XOR_A_PTR_HL", XOR_A_PTR_HL, "hl"], ["XOR_A_n", XOR_A_n(85), "default"], ["XOR_A_IXd", XOR_A_IXd(85), "ixd"], ["XOR_A_IYd", XOR_A_IYd(85), "iyd"], ["OR_A_B", OR_A_B, "default"], ["OR_A_C", OR_A_C, "default"], ["OR_A_D", OR_A_D, "default"], ["OR_A_E", OR_A_E, "default"], ["OR_A_H", OR_A_H, "default"], ["OR_A_L", OR_A_L, "default"], ["OR_A_A", OR_A_A, "default"], ["OR_A_PTR_HL", OR_A_PTR_HL, "hl"], ["OR_A_n", OR_A_n(85), "default"], ["OR_A_IXd", OR_A_IXd(85), "ixd"], ["OR_A_IYd", OR_A_IYd(85), "iyd"], ["CP_A_B", CP_A_B, "default"], ["CP_A_C", CP_A_C, "default"], ["CP_A_D", CP_A_D, "default"], ["CP_A_E", CP_A_E, "default"], ["CP_A_H", CP_A_H, "default"], ["CP_A_L", CP_A_L, "default"], ["CP_A_A", CP_A_A, "default"], ["CP_A_PTR_HL", CP_A_PTR_HL, "hl"], ["CP_A_n", CP_A_n(85), "default"], ["CP_A_IXd", CP_A_IXd(85), "ixd"], ["CP_A_IYd", CP_A_IYd(85), "iyd"], ["ADD_HL_BC", ADD_HL_BC, "default"], ["ADC_HL_BC", ADC_HL_BC, "default"], ["SBC_HL_BC", SBC_HL_BC, "default"], ["ADD_HL_DE", ADD_HL_DE, "default"], ["ADC_HL_DE", ADC_HL_DE, "default"], ["SBC_HL_DE", SBC_HL_DE, "default"], ["ADD_HL_HL", ADD_HL_HL, "default"], ["ADC_HL_HL", ADC_HL_HL, "default"], ["SBC_HL_HL", SBC_HL_HL, "default"], ["ADD_HL_SP", ADD_HL_SP, "default"], ["ADC_HL_SP", ADC_HL_SP, "default"], ["SBC_HL_SP", SBC_HL_SP, "default"], ["ADD_IX_BC", ADD_IX_BC, "default"], ["ADD_IY_BC", ADD_IY_BC, "default"], ["ADD_IX_DE", ADD_IX_DE, "default"], ["ADD_IY_DE", ADD_IY_DE, "default"], ["ADD_IX_HL", ADD_IX_HL, "default"], ["ADD_IY_HL", ADD_IY_HL, "default"], ["ADD_IX_SP", ADD_IX_SP, "default"], ["ADD_IY_SP", ADD_IY_SP, "default"], ["INC_B", INC_B, "default"], ["DEC_B", DEC_B, "default"], ["INC_C", INC_C, "default"], ["DEC_C", DEC_C, "default"], ["INC_D", INC_D, "default"], ["DEC_D", DEC_D, "default"], ["INC_E", INC_E, "default"], ["DEC_E", DEC_E, "default"], ["INC_H", INC_H, "default"], ["DEC_H", DEC_H, "default"], ["INC_L", INC_L, "default"], ["DEC_L", DEC_L, "default"], ["INC_A", INC_A, "default"], ["DEC_A", DEC_A, "default"], ["INC_PTR_HL", INC_PTR_HL, "hl"], ["DEC_PTR_HL", DEC_PTR_HL, "hl"], ["INC_PTR_IXd", INC_PTR_IXd(85), "ixd"], ["DEC_PTR_IXd", DEC_PTR_IXd(85), "ixd"], ["LD_PTR_IXd_n", LD_PTR_IXd_n(2, 85), "ixd"], ["INC_BC", INC_BC, "default"], ["DEC_BC", DEC_BC, "default"], ["INC_DE", INC_DE, "default"], ["DEC_DE", DEC_DE, "default"], ["INC_HL", INC_HL, "default"], ["DEC_HL", DEC_HL, "default"], ["INC_SP", INC_SP, "default"], ["DEC_SP", DEC_SP, "default"], ["INC_IX", INC_IX, "default"], ["DEC_IX", DEC_IX, "default"], ["INC_IXH", INC_IXH, "default"], ["DEC_IXH", DEC_IXH, "default"], ["INC_IXL", INC_IXL, "default"], ["DEC_IXL", DEC_IXL, "default"], ["PUSH_BC", PUSH_BC, "sp"], ["POP_BC", POP_BC, "sp"], ["PUSH_DE", PUSH_DE, "sp"], ["POP_DE", POP_DE, "sp"], ["PUSH_HL", PUSH_HL, "sp"], ["POP_HL", POP_HL, "sp"], ["PUSH_AF", PUSH_AF, "sp"], ["POP_AF", POP_AF, "sp"], ["PUSH_IX", PUSH_IX, "sp"], ["POP_IX", POP_IX, "sp"], ["RLC_B", RLC_B, "default"], ["RLC_C", RLC_C, "default"], ["RLC_D", RLC_D, "default"], ["RLC_E", RLC_E, "default"], ["RLC_H", RLC_H, "default"], ["RLC_L", RLC_L, "default"], ["RLC_A", RLC_A, "default"], ["RLC_PTR_HL", RLC_PTR_HL, "hl"], ["RLC_PTR_IXd", RLC_PTR_IXd(85), "ixd"], ["RLC_PTR_IYd", RLC_PTR_IYd(85), "iyd"], ["RRC_B", RRC_B, "default"], ["RRC_C", RRC_C, "default"], ["RRC_D", RRC_D, "default"], ["RRC_E", RRC_E, "default"], ["RRC_H", RRC_H, "default"], ["RRC_L", RRC_L, "default"], ["RRC_A", RRC_A, "default"], ["RRC_PTR_HL", RRC_PTR_HL, "hl"], ["RRC_PTR_IXd", RRC_PTR_IXd(85), "ixd"], ["RRC_PTR_IYd", RRC_PTR_IYd(85), "iyd"], ["RL_B", RL_B, "default"], ["RL_C", RL_C, "default"], ["RL_D", RL_D, "default"], ["RL_E", RL_E, "default"], ["RL_H", RL_H, "default"], ["RL_L", RL_L, "default"], ["RL_A", RL_A, "default"], ["RL_PTR_HL", RL_PTR_HL, "hl"], ["RL_PTR_IXd", RL_PTR_IXd(85), "ixd"], ["RL_PTR_IYd", RL_PTR_IYd(85), "iyd"], ["RR_B", RR_B, "default"], ["RR_C", RR_C, "default"], ["RR_D", RR_D, "default"], ["RR_E", RR_E, "default"], ["RR_H", RR_H, "default"], ["RR_L", RR_L, "default"], ["RR_A", RR_A, "default"], ["RR_PTR_HL", RR_PTR_HL, "hl"], ["RR_PTR_IXd", RR_PTR_IXd(85), "ixd"], ["RR_PTR_IYd", RR_PTR_IYd(85), "iyd"], ["SLA_B", SLA_B, "default"], ["SLA_C", SLA_C, "default"], ["SLA_D", SLA_D, "default"], ["SLA_E", SLA_E, "default"], ["SLA_H", SLA_H, "default"], ["SLA_L", SLA_L, "default"], ["SLA_A", SLA_A, "default"], ["SLA_PTR_HL", SLA_PTR_HL, "hl"], ["SLA_PTR_IXd", SLA_PTR_IXd(85), "ixd"], ["SLA_PTR_IYd", SLA_PTR_IYd(85), "iyd"], ["SRA_B", SRA_B, "default"], ["SRA_C", SRA_C, "default"], ["SRA_D", SRA_D, "default"], ["SRA_E", SRA_E, "default"], ["SRA_H", SRA_H, "default"], ["SRA_L", SRA_L, "default"], ["SRA_A", SRA_A, "default"], ["SRA_PTR_HL", SRA_PTR_HL, "hl"], ["SRA_PTR_IXd", SRA_PTR_IXd(85), "ixd"], ["SRA_PTR_IYd", SRA_PTR_IYd(85), "iyd"], ["SRL_B", SRL_B, "default"], ["SRL_C", SRL_C, "default"], ["SRL_D", SRL_D, "default"], ["SRL_E", SRL_E, "default"], ["SRL_H", SRL_H, "default"], ["SRL_L", SRL_L, "default"], ["SRL_A", SRL_A, "default"], ["SRL_PTR_HL", SRL_PTR_HL, "hl"], ["SRL_PTR_IXd", SRL_PTR_IXd(85), "ixd"], ["SRL_PTR_IYd", SRL_PTR_IYd(85), "iyd"], ["BIT0_B", BIT0_B, "default"], ["BIT0_C", BIT0_C, "default"], ["BIT0_D", BIT0_D, "default"], ["BIT0_E", BIT0_E, "default"], ["BIT0_H", BIT0_H, "default"], ["BIT0_L", BIT0_L, "default"], ["BIT0_A", BIT0_A, "default"], ["BIT0_PTR_HL", BIT0_PTR_HL, "hl"], ["BIT0_PTR_IXd", BIT0_PTR_IXd(85), "ixd"], ["BIT0_PTR_IYd", BIT0_PTR_IYd(85), "iyd"], ["RES0_B", RES0_B, "default"], ["RES0_C", RES0_C, "default"], ["RES0_D", RES0_D, "default"], ["RES0_E", RES0_E, "default"], ["RES0_H", RES0_H, "default"], ["RES0_L", RES0_L, "default"], ["RES0_A", RES0_A, "default"], ["RES0_PTR_HL", RES0_PTR_HL, "hl"], ["RES0_PTR_IXd", RES0_PTR_IXd(85), "ixd"], ["RES0_PTR_IYd", RES0_PTR_IYd(85), "iyd"], ["SET0_B", SET0_B, "default"], ["SET0_C", SET0_C, "default"], ["SET0_D", SET0_D, "default"], ["SET0_E", SET0_E, "default"], ["SET0_H", SET0_H, "default"], ["SET0_L", SET0_L, "default"], ["SET0_A", SET0_A, "default"], ["SET0_PTR_HL", SET0_PTR_HL, "hl"], ["SET0_PTR_IXd", SET0_PTR_IXd(85), "ixd"], ["SET0_PTR_IYd", SET0_PTR_IYd(85), "iyd"], ["BIT1_B", BIT1_B, "default"], ["BIT1_C", BIT1_C, "default"], ["BIT1_D", BIT1_D, "default"], ["BIT1_E", BIT1_E, "default"], ["BIT1_H", BIT1_H, "default"], ["BIT1_L", BIT1_L, "default"], ["BIT1_A", BIT1_A, "default"], ["BIT1_PTR_HL", BIT1_PTR_HL, "hl"], ["BIT1_PTR_IXd", BIT1_PTR_IXd(85), "ixd"], ["BIT1_PTR_IYd", BIT1_PTR_IYd(85), "iyd"], ["RES1_B", RES1_B, "default"], ["RES1_C", RES1_C, "default"], ["RES1_D", RES1_D, "default"], ["RES1_E", RES1_E, "default"], ["RES1_H", RES1_H, "default"], ["RES1_L", RES1_L, "default"], ["RES1_A", RES1_A, "default"], ["RES1_PTR_HL", RES1_PTR_HL, "hl"], ["RES1_PTR_IXd", RES1_PTR_IXd(85), "ixd"], ["RES1_PTR_IYd", RES1_PTR_IYd(85), "iyd"], ["SET1_B", SET1_B, "default"], ["SET1_C", SET1_C, "default"], ["SET1_D", SET1_D, "default"], ["SET1_E", SET1_E, "default"], ["SET1_H", SET1_H, "default"], ["SET1_L", SET1_L, "default"], ["SET1_A", SET1_A, "default"], ["SET1_PTR_HL", SET1_PTR_HL, "hl"], ["SET1_PTR_IXd", SET1_PTR_IXd(85), "ixd"], ["SET1_PTR_IYd", SET1_PTR_IYd(85), "iyd"], ["BIT2_B", BIT2_B, "default"], ["BIT2_C", BIT2_C, "default"], ["BIT2_D", BIT2_D, "default"], ["BIT2_E", BIT2_E, "default"], ["BIT2_H", BIT2_H, "default"], ["BIT2_L", BIT2_L, "default"], ["BIT2_A", BIT2_A, "default"], ["BIT2_PTR_HL", BIT2_PTR_HL, "hl"], ["BIT2_PTR_IXd", BIT2_PTR_IXd(85), "ixd"], ["BIT2_PTR_IYd", BIT2_PTR_IYd(85), "iyd"], ["RES2_B", RES2_B, "default"], ["RES2_C", RES2_C, "default"], ["RES2_D", RES2_D, "default"], ["RES2_E", RES2_E, "default"], ["RES2_H", RES2_H, "default"], ["RES2_L", RES2_L, "default"], ["RES2_A", RES2_A, "default"], ["RES2_PTR_HL", RES2_PTR_HL, "hl"], ["RES2_PTR_IXd", RES2_PTR_IXd(85), "ixd"], ["RES2_PTR_IYd", RES2_PTR_IYd(85), "iyd"], ["SET2_B", SET2_B, "default"], ["SET2_C", SET2_C, "default"], ["SET2_D", SET2_D, "default"], ["SET2_E", SET2_E, "default"], ["SET2_H", SET2_H, "default"], ["SET2_L", SET2_L, "default"], ["SET2_A", SET2_A, "default"], ["SET2_PTR_HL", SET2_PTR_HL, "hl"], ["SET2_PTR_IXd", SET2_PTR_IXd(85), "ixd"], ["SET2_PTR_IYd", SET2_PTR_IYd(85), "iyd"], ["BIT3_B", BIT3_B, "default"], ["BIT3_C", BIT3_C, "default"], ["BIT3_D", BIT3_D, "default"], ["BIT3_E", BIT3_E, "default"], ["BIT3_H", BIT3_H, "default"], ["BIT3_L", BIT3_L, "default"], ["BIT3_A", BIT3_A, "default"], ["BIT3_PTR_HL", BIT3_PTR_HL, "hl"], ["BIT3_PTR_IXd", BIT3_PTR_IXd(85), "ixd"], ["BIT3_PTR_IYd", BIT3_PTR_IYd(85), "iyd"], ["RES3_B", RES3_B, "default"], ["RES3_C", RES3_C, "default"], ["RES3_D", RES3_D, "default"], ["RES3_E", RES3_E, "default"], ["RES3_H", RES3_H, "default"], ["RES3_L", RES3_L, "default"], ["RES3_A", RES3_A, "default"], ["RES3_PTR_HL", RES3_PTR_HL, "hl"], ["RES3_PTR_IXd", RES3_PTR_IXd(85), "ixd"], ["RES3_PTR_IYd", RES3_PTR_IYd(85), "iyd"], ["SET3_B", SET3_B, "default"], ["SET3_C", SET3_C, "default"], ["SET3_D", SET3_D, "default"], ["SET3_E", SET3_E, "default"], ["SET3_H", SET3_H, "default"], ["SET3_L", SET3_L, "default"], ["SET3_A", SET3_A, "default"], ["SET3_PTR_HL", SET3_PTR_HL, "hl"], ["SET3_PTR_IXd", SET3_PTR_IXd(85), "ixd"], ["SET3_PTR_IYd", SET3_PTR_IYd(85), "iyd"], ["BIT4_B", BIT4_B, "default"], ["BIT4_C", BIT4_C, "default"], ["BIT4_D", BIT4_D, "default"], ["BIT4_E", BIT4_E, "default"], ["BIT4_H", BIT4_H, "default"], ["BIT4_L", BIT4_L, "default"], ["BIT4_A", BIT4_A, "default"], ["BIT4_PTR_HL", BIT4_PTR_HL, "hl"], ["BIT4_PTR_IXd", BIT4_PTR_IXd(85), "ixd"], ["BIT4_PTR_IYd", BIT4_PTR_IYd(85), "iyd"], ["RES4_B", RES4_B, "default"], ["RES4_C", RES4_C, "default"], ["RES4_D", RES4_D, "default"], ["RES4_E", RES4_E, "default"], ["RES4_H", RES4_H, "default"], ["RES4_L", RES4_L, "default"], ["RES4_A", RES4_A, "default"], ["RES4_PTR_HL", RES4_PTR_HL, "hl"], ["RES4_PTR_IXd", RES4_PTR_IXd(85), "ixd"], ["RES4_PTR_IYd", RES4_PTR_IYd(85), "iyd"], ["SET4_B", SET4_B, "default"], ["SET4_C", SET4_C, "default"], ["SET4_D", SET4_D, "default"], ["SET4_E", SET4_E, "default"], ["SET4_H", SET4_H, "default"], ["SET4_L", SET4_L, "default"], ["SET4_A", SET4_A, "default"], ["SET4_PTR_HL", SET4_PTR_HL, "hl"], ["SET4_PTR_IXd", SET4_PTR_IXd(85), "ixd"], ["SET4_PTR_IYd", SET4_PTR_IYd(85), "iyd"], ["BIT5_B", BIT5_B, "default"], ["BIT5_C", BIT5_C, "default"], ["BIT5_D", BIT5_D, "default"], ["BIT5_E", BIT5_E, "default"], ["BIT5_H", BIT5_H, "default"], ["BIT5_L", BIT5_L, "default"], ["BIT5_A", BIT5_A, "default"], ["BIT5_PTR_HL", BIT5_PTR_HL, "hl"], ["BIT5_PTR_IXd", BIT5_PTR_IXd(85), "ixd"], ["BIT5_PTR_IYd", BIT5_PTR_IYd(85), "iyd"], ["RES5_B", RES5_B, "default"], ["RES5_C", RES5_C, "default"], ["RES5_D", RES5_D, "default"], ["RES5_E", RES5_E, "default"], ["RES5_H", RES5_H, "default"], ["RES5_L", RES5_L, "default"], ["RES5_A", RES5_A, "default"], ["RES5_PTR_HL", RES5_PTR_HL, "hl"], ["RES5_PTR_IXd", RES5_PTR_IXd(85), "ixd"], ["RES5_PTR_IYd", RES5_PTR_IYd(85), "iyd"], ["SET5_B", SET5_B, "default"], ["SET5_C", SET5_C, "default"], ["SET5_D", SET5_D, "default"], ["SET5_E", SET5_E, "default"], ["SET5_H", SET5_H, "default"], ["SET5_L", SET5_L, "default"], ["SET5_A", SET5_A, "default"], ["SET5_PTR_HL", SET5_PTR_HL, "hl"], ["SET5_PTR_IXd", SET5_PTR_IXd(85), "ixd"], ["SET5_PTR_IYd", SET5_PTR_IYd(85), "iyd"], ["BIT6_B", BIT6_B, "default"], ["BIT6_C", BIT6_C, "default"], ["BIT6_D", BIT6_D, "default"], ["BIT6_E", BIT6_E, "default"], ["BIT6_H", BIT6_H, "default"], ["BIT6_L", BIT6_L, "default"], ["BIT6_A", BIT6_A, "default"], ["BIT6_PTR_HL", BIT6_PTR_HL, "hl"], ["BIT6_PTR_IXd", BIT6_PTR_IXd(85), "ixd"], ["BIT6_PTR_IYd", BIT6_PTR_IYd(85), "iyd"], ["RES6_B", RES6_B, "default"], ["RES6_C", RES6_C, "default"], ["RES6_D", RES6_D, "default"], ["RES6_E", RES6_E, "default"], ["RES6_H", RES6_H, "default"], ["RES6_L", RES6_L, "default"], ["RES6_A", RES6_A, "default"], ["RES6_PTR_HL", RES6_PTR_HL, "hl"], ["RES6_PTR_IXd", RES6_PTR_IXd(85), "ixd"], ["RES6_PTR_IYd", RES6_PTR_IYd(85), "iyd"], ["SET6_B", SET6_B, "default"], ["SET6_C", SET6_C, "default"], ["SET6_D", SET6_D, "default"], ["SET6_E", SET6_E, "default"], ["SET6_H", SET6_H, "default"], ["SET6_L", SET6_L, "default"], ["SET6_A", SET6_A, "default"], ["SET6_PTR_HL", SET6_PTR_HL, "hl"], ["SET6_PTR_IXd", SET6_PTR_IXd(85), "ixd"], ["SET6_PTR_IYd", SET6_PTR_IYd(85), "iyd"], ["BIT7_B", BIT7_B, "default"], ["BIT7_C", BIT7_C, "default"], ["BIT7_D", BIT7_D, "default"], ["BIT7_E", BIT7_E, "default"], ["BIT7_H", BIT7_H, "default"], ["BIT7_L", BIT7_L, "default"], ["BIT7_A", BIT7_A, "default"], ["BIT7_PTR_HL", BIT7_PTR_HL, "hl"], ["BIT7_PTR_IXd", BIT7_PTR_IXd(85), "ixd"], ["BIT7_PTR_IYd", BIT7_PTR_IYd(85), "iyd"], ["RES7_B", RES7_B, "default"], ["RES7_C", RES7_C, "default"], ["RES7_D", RES7_D, "default"], ["RES7_E", RES7_E, "default"], ["RES7_H", RES7_H, "default"], ["RES7_L", RES7_L, "default"], ["RES7_A", RES7_A, "default"], ["RES7_PTR_HL", RES7_PTR_HL, "hl"], ["RES7_PTR_IXd", RES7_PTR_IXd(85), "ixd"], ["RES7_PTR_IYd", RES7_PTR_IYd(85), "iyd"], ["SET7_B", SET7_B, "default"], ["SET7_C", SET7_C, "default"], ["SET7_D", SET7_D, "default"], ["SET7_E", SET7_E, "default"], ["SET7_H", SET7_H, "default"], ["SET7_L", SET7_L, "default"], ["SET7_A", SET7_A, "default"], ["SET7_PTR_HL", SET7_PTR_HL, "hl"], ["SET7_PTR_IXd", SET7_PTR_IXd(85), "ixd"], ["SET7_PTR_IYd", SET7_PTR_IYd(85), "iyd"], ["JP_nn", JP_nn(32771), "jp"], ["JP_NZ", JP_NZ(32771), "jp"], ["JP_Z", JP_Z(32771), "jp"], ["JP_NC", JP_NC(32771), "jp"], ["JP_C", JP_C(32771), "jp"], ["JP_PO", JP_PO(32771), "jp"], ["JP_PE", JP_PE(32771), "jp"], ["JP_P", JP_P(32771), "jp"], ["JP_M", JP_M(32771), "jp"], ["JP_HL", JP_HL, "jp"], ["JP_IX", JP_IX, "jp"], ["JP_IY", JP_IY, "jp"], ["JR_e", JR_e(0), "jr"], ["JR_NZ", JR_NZ(0), "jr"], ["JR_Z", JR_Z(0), "jr"], ["JR_NC", JR_NC(0), "jr"], ["JR_C", JR_C(0), "jr"], ["DJNZ_e", DJNZ_e(0), "djnz"], ["CALL_nn", CALL_nn(32771), "call"], ["CALL_NZ", CALL_NZ(32771), "call"], ["CALL_Z", CALL_Z(32771), "call"], ["CALL_NC", CALL_NC(32771), "call"], ["CALL_C", CALL_C(32771), "call"], ["CALL_PO", CALL_PO(32771), "call"], ["CALL_PE", CALL_PE(32771), "call"], ["CALL_P", CALL_P(32771), "call"], ["CALL_M", CALL_M(32771), "call"], ["RET", RET, "ret"], ["RET_NZ", RET_NZ, "ret"], ["RET_Z", RET_Z, "ret"], ["RET_NC", RET_NC, "ret"], ["RET_C", RET_C, "ret"], ["RET_PO", RET_PO, "ret"], ["RET_PE", RET_PE, "ret"], ["RET_P", RET_P, "ret"], ["RET_M", RET_M, "ret"], ["RETI", RETI, "ret"], ["RETN", RETN, "ret"], ["RST_00", RST_00, "rst"], ["RST_08", RST_08, "rst"], ["RST_10", RST_10, "rst"], ["RST_18", RST_18, "rst"], ["RST_20", RST_20, "rst"], ["RST_28", RST_28, "rst"], ["RST_30", RST_30, "rst"], ["RST_38", RST_38, "rst"], ["NOP", NOP, "default"], ["HALT", HALT, "halt"], ["DI", DI, "default"], ["EI", EI, "default"], ["SCF", SCF, "default"], ["CCF", CCF, "default"], ["CPL", CPL, "default"], ["DAA", DAA, "default"], ["NEG", NEG, "default"], ["EX_DE_HL", EX_DE_HL, "default"], ["EX_AF_AF", EX_AF_AF, "default"], ["EXX", EXX, "default"], ["RLCA", RLCA, "default"], ["RRCA", RRCA, "default"], ["RLA", RLA, "default"], ["RRA", RRA, "default"], ["IM_0", IM_0, "default"], ["IM_1", IM_1, "default"], ["IM_2", IM_2, "default"], ["RRD", RRD, "hl"], ["RLD", RLD, "hl"], ["LDI", LDI, "block"], ["LDD", LDD, "block"], ["LDIR", LDIR, "block"], ["LDDR", LDDR, "block"], ["CPI", CPI, "block"], ["CPD", CPD, "block"], ["CPIR", CPIR, "block"], ["CPDR", CPDR, "block"], ["EX_SP_HL", EX_SP_HL, "exsp"], ["EX_SP_IX", EX_SP_IX, "exsp"], ["EX_SP_IY", EX_SP_IY, "exsp"], ["IN_A_n", IN_A_n(85), "io"], ["OUT_n_A", OUT_n_A(85), "io"], ["IN_B_C", IN_B_C, "io"], ["OUT_C_B", OUT_C_B, "io"], ["IN_C_C", IN_C_C, "io"], ["OUT_C_C", OUT_C_C, "io"], ["IN_D_C", IN_D_C, "io"], ["OUT_C_D", OUT_C_D, "io"], ["IN_E_C", IN_E_C, "io"], ["OUT_C_E", OUT_C_E, "io"], ["IN_H_C", IN_H_C, "io"], ["OUT_C_H", OUT_C_H, "io"], ["IN_L_C", IN_L_C, "io"], ["OUT_C_L", OUT_C_L, "io"], ["IN_A_C", IN_A_C, "io"], ["OUT_C_A", OUT_C_A, "io"], ["IN_PTR_C", IN_PTR_C, "io"], ["OUT_C_0", OUT_C_0, "io"]]);

