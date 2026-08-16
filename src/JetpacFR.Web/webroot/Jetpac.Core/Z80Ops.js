
import { Union } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { union_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { Exception, equals } from "../fable_modules/fable-library-js.5.13.0/Util.js";
import { RegisterFile__Iy, RegisterFile__Ix, RegisterFile__Sp, RegisterFile__SetSp_Z524259A4, RegisterFile__Exx, RegisterFile__Ex_Z1C3BEB40, RegisterFile__R, RegisterFile__I, RegisterFile__SetR_Z524259A4, RegisterFile__SetI_Z524259A4, RegisterFile__SetPc_Z524259A4, RegisterFile__Pc, RegisterFile__SetWz_Z524259A4, RegisterFile__Set_488BADFE, RegisterFile__Get_6F21F62, RegisterFile__Set_Z54B079DF, RegisterFile__Get_2EC184DD, RegisterFile__Wz, R8, R16 } from "./Registers.js";
import { Z80Dispatch_set_Run_3708BEA5, Z80__ReadOpcode, Z80__Push16_Z524259A4, Z80__set_Iff2_Z1FBCCD16, Z80__Halt, Z80__Branch_Z524259A4, Z80__set_IrqMode_Z524259A4, Z80__Pop16, Z80__get_Iff2, Z80__set_Iff1_Z1FBCCD16, Z80__ReadImmediate16, Z80__Out_Z37302880, Z80__In_Z524259A4, Z80__SetFlags_Z7353D318, Z80__Flags, Z80__PassTime_Z524259A4, Z80__Write_Z37302880, Z80__ReadImmediate, Z80__get_Regs, Z80__Read_Z524259A4 } from "./Z80.js";
import { Flags_op_LogicalNot_Z7353D318, Flags_Subtract, Flags_HalfCarry, Flags__get_half_carry, Flags_Overflow, Flags_Flag5, Flags, Flags_Flag3, Flags_Carry, Flags_Zero, Flags_Sign, Flags_op_BitwiseOr_Z51621B00, Flags_op_BitwiseAnd_Z51621B00, Flags__get_sign, Flags__get_parity, Flags__get_carry, Flags__get_zero } from "./Flags.js";
import { ccf, scf, cpl, daa, fastRotate8, fastRotateCircular8, dec8, inc8, add16, iff2FlagsFor, adc16, sbc16, parityFlagsFor, bit as bit_1, shiftLogical8, shiftArithmetic8, rotate8, Direction, rotateCircular8, cmp8, or8, xor8, and8, sub8, add8 } from "./Alu.js";
import { item } from "../fable_modules/fable-library-js.5.13.0/Array.js";

export class HlSet extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Base", "Ix", "Iy"];
    }
    static Base = new HlSet(0, []);
    static Ix = new HlSet(1, []);
    static Iy = new HlSet(2, []);
}

export function HlSet_$reflection() {
    return union_type("Jetpac.Core.Z80Ops.HlSet", [], HlSet, () => [[], [], []]);
}

function isIndexed(ctx) {
    return !equals(ctx, HlSet.Base);
}

function hlHighLow(ctx) {
    switch (ctx.tag) {
        case 1:
            return R16.IX;
        case 2:
            return R16.IY;
        default:
            return R16.HL;
    }
}

function hlHigh(ctx) {
    switch (ctx.tag) {
        case 1:
            return R8.IXH;
        case 2:
            return R8.IYH;
        default:
            return R8.H;
    }
}

function hlLow(ctx) {
    switch (ctx.tag) {
        case 1:
            return R8.IXL;
        case 2:
            return R8.IYL;
        default:
            return R8.L;
    }
}

function rpHighLow(ctx, p) {
    switch (p) {
        case 0:
            return R16.BC;
        case 1:
            return R16.DE;
        case 2:
            return hlHighLow(ctx);
        case 3:
            return R16.SP;
        default:
            throw new Exception("bad rp");
    }
}

function rpHigh(ctx, p) {
    switch (p) {
        case 0:
            return R8.B;
        case 1:
            return R8.D;
        case 2:
            return hlHigh(ctx);
        case 3:
            return R8.SPH;
        default:
            throw new Exception("bad rp");
    }
}

function rpLow(ctx, p) {
    switch (p) {
        case 0:
            return R8.C;
        case 1:
            return R8.E;
        case 2:
            return hlLow(ctx);
        case 3:
            return R8.SPL;
        default:
            throw new Exception("bad rp");
    }
}

function rp2HighLow(ctx, p) {
    switch (p) {
        case 0:
            return R16.BC;
        case 1:
            return R16.DE;
        case 2:
            return hlHighLow(ctx);
        case 3:
            return R16.AF;
        default:
            throw new Exception("bad rp2");
    }
}

function tableR(ctx, noRemap, idx) {
    switch (idx) {
        case 0:
            return R8.B;
        case 1:
            return R8.C;
        case 2:
            return R8.D;
        case 3:
            return R8.E;
        case 4:
            if (noRemap) {
                return R8.H;
            }
            else {
                return hlHigh(ctx);
            }
        case 5:
            if (noRemap) {
                return R8.L;
            }
            else {
                return hlLow(ctx);
            }
        case 7:
            return R8.A;
        default:
            throw new Exception("bad tableR index");
    }
}

function getR(z80, ctx, idx, noRemap) {
    switch (idx) {
        case 6:
            return Z80__Read_Z524259A4(z80, RegisterFile__Wz(Z80__get_Regs(z80))) | 0;
        case 8:
            return Z80__ReadImmediate(z80) | 0;
        default:
            return RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), tableR(ctx, noRemap, idx)) | 0;
    }
}

function setR(z80, ctx, idx, noRemap, value) {
    switch (idx) {
        case 6: {
            Z80__Write_Z37302880(z80, RegisterFile__Wz(Z80__get_Regs(z80)), value);
            break;
        }
        case 8: {
            throw new Exception("cannot set immediate value");
            break;
        }
        default:
            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), tableR(ctx, noRemap, idx), value);
    }
}

function signedImm(v) {
    if (v >= 128) {
        return (v - 256) | 0;
    }
    else {
        return v | 0;
    }
}

function ccCheck(cc, flags) {
    switch (cc) {
        case 0:
            return !Flags__get_zero(flags);
        case 1:
            return Flags__get_zero(flags);
        case 2:
            return !Flags__get_carry(flags);
        case 3:
            return Flags__get_carry(flags);
        case 4:
            return !Flags__get_parity(flags);
        case 5:
            return Flags__get_parity(flags);
        case 6:
            return !Flags__get_sign(flags);
        case 7:
            return Flags__get_sign(flags);
        default:
            throw new Exception("bad cc");
    }
}

const imTable = new Int32Array([0, 0, 1, 2, 0, 0, 1, 2]);

function aluOp(y, lhs, rhs, flags) {
    switch (y) {
        case 0:
            return add8(lhs, rhs, false);
        case 1:
            return add8(lhs, rhs, Flags__get_carry(flags));
        case 2:
            return sub8(lhs, rhs, false);
        case 3:
            return sub8(lhs, rhs, Flags__get_carry(flags));
        case 4:
            return and8(lhs, rhs);
        case 5:
            return xor8(lhs, rhs);
        case 6:
            return or8(lhs, rhs);
        case 7:
            return cmp8(lhs, rhs);
        default:
            throw new Exception("bad alu y");
    }
}

function blockLoad(z80, increment, repeat) {
    const add = (increment ? 1 : 65535) | 0;
    const hl = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL) | 0;
    RegisterFile__Set_488BADFE(Z80__get_Regs(z80), R16.HL, (hl + add) & 65535);
    const byte = Z80__Read_Z524259A4(z80, hl) | 0;
    const de = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.DE) | 0;
    RegisterFile__Set_488BADFE(Z80__get_Regs(z80), R16.DE, (de + add) & 65535);
    Z80__Write_Z37302880(z80, de, byte);
    Z80__PassTime_Z524259A4(z80, 2);
    const flagBits = (byte + RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A)) | 0;
    const newBc = ((RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_488BADFE(Z80__get_Regs(z80), R16.BC, newBc);
    const preservedFlags = Flags_op_BitwiseAnd_Z51621B00(Z80__Flags(z80), Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Sign(), Flags_Zero()), Flags_Carry()));
    const flagsFromBits = Flags_op_BitwiseOr_Z51621B00(((flagBits & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc = (newBc !== 0) ? Flags_Overflow() : (new Flags(0));
    Z80__SetFlags_Z7353D318(z80, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(preservedFlags, flagsFromBits), flagsFromBc));
    if (repeat && (newBc !== 0)) {
        RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Pc(Z80__get_Regs(z80)) - 1) & 65535);
        RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Pc(Z80__get_Regs(z80)) - 2) & 65535);
        Z80__PassTime_Z524259A4(z80, 5);
    }
}

function blockCompare(z80, increment, repeat) {
    const add = (increment ? 1 : 65535) | 0;
    const hl = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL) | 0;
    RegisterFile__Set_488BADFE(Z80__get_Regs(z80), R16.HL, (hl + add) & 65535);
    const byte = Z80__Read_Z524259A4(z80, hl) | 0;
    const patternInput = sub8(RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A), byte, false);
    const subtractFlags = patternInput[1];
    const result = patternInput[0] | 0;
    Z80__PassTime_Z524259A4(z80, 5);
    const flagBits = (Flags__get_half_carry(subtractFlags) ? (result - 1) : result) | 0;
    const newBc = ((RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC) - 1) & 65535) | 0;
    RegisterFile__Set_488BADFE(Z80__get_Regs(z80), R16.BC, newBc);
    const fromSubtractMask = Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_HalfCarry(), Flags_Zero()), Flags_Sign()), Flags_Subtract());
    const preservedFlags = Flags_op_BitwiseAnd_Z51621B00(Z80__Flags(z80), Flags_op_LogicalNot_Z7353D318(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_Flag3(), Flags_Flag5()), fromSubtractMask), Flags_Overflow())));
    const flagsFromBits = Flags_op_BitwiseOr_Z51621B00(((flagBits & 8) !== 0) ? Flags_Flag3() : (new Flags(0)), ((flagBits & 2) !== 0) ? Flags_Flag5() : (new Flags(0)));
    const flagsFromBc = (newBc !== 0) ? Flags_Overflow() : (new Flags(0));
    Z80__SetFlags_Z7353D318(z80, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseOr_Z51621B00(preservedFlags, flagsFromBits), flagsFromBc), Flags_op_BitwiseAnd_Z51621B00(fromSubtractMask, subtractFlags)));
    if ((repeat && (newBc !== 0)) && !Flags__get_zero(subtractFlags)) {
        RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Pc(Z80__get_Regs(z80)) - 1) & 65535);
        RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Pc(Z80__get_Regs(z80)) - 2) & 65535);
        Z80__PassTime_Z524259A4(z80, 5);
    }
}

function cbOp(z80, opcode, ctx) {
    let copyOfStruct, copyOfStruct_1;
    const x = (opcode >> 6) | 0;
    const y = ((opcode >> 3) & 7) | 0;
    const z = (opcode & 7) | 0;
    switch (x) {
        case 0: {
            const lhs = getR(z80, ctx, z, false) | 0;
            if (z === 6) {
                Z80__PassTime_Z524259A4(z80, 1);
            }
            let patternInput;
            switch (y) {
                case 0: {
                    patternInput = rotateCircular8(lhs, Direction.Left);
                    break;
                }
                case 1: {
                    patternInput = rotateCircular8(lhs, Direction.Right);
                    break;
                }
                case 2: {
                    patternInput = rotate8(lhs, Direction.Left, (copyOfStruct = Z80__Flags(z80), Flags__get_carry(copyOfStruct)));
                    break;
                }
                case 3: {
                    patternInput = rotate8(lhs, Direction.Right, (copyOfStruct_1 = Z80__Flags(z80), Flags__get_carry(copyOfStruct_1)));
                    break;
                }
                case 4: {
                    patternInput = shiftArithmetic8(lhs, Direction.Left);
                    break;
                }
                case 5: {
                    patternInput = shiftArithmetic8(lhs, Direction.Right);
                    break;
                }
                case 6: {
                    patternInput = shiftLogical8(lhs, Direction.Left);
                    break;
                }
                case 7: {
                    patternInput = shiftLogical8(lhs, Direction.Right);
                    break;
                }
                default:
                    throw new Exception("bad cb y");
            }
            setR(z80, ctx, z, false, patternInput[0]);
            Z80__SetFlags_Z7353D318(z80, patternInput[1]);
            break;
        }
        case 1: {
            const lhs_1 = getR(z80, ctx, z, false) | 0;
            if (z === 6) {
                Z80__PassTime_Z524259A4(z80, 1);
            }
            const busNoise = ((z === 6) ? ((RegisterFile__Wz(Z80__get_Regs(z80)) >> 8) & 255) : lhs_1) | 0;
            Z80__SetFlags_Z7353D318(z80, bit_1(lhs_1, 1 << y, Z80__Flags(z80), busNoise));
            break;
        }
        case 2:
        case 3: {
            const lhs_2 = getR(z80, ctx, z, false) | 0;
            if (z === 6) {
                Z80__PassTime_Z524259A4(z80, 1);
            }
            const bit = (1 << y) | 0;
            setR(z80, ctx, z, false, (x === 2) ? (lhs_2 & ~bit) : (lhs_2 | bit));
            break;
        }
        default:
            throw new Exception("bad cb x");
    }
}

function edOp(z80, opcode) {
    let copyOfStruct, copyOfStruct_1;
    const x = (opcode >> 6) | 0;
    const y = ((opcode >> 3) & 7) | 0;
    const z = (opcode & 7) | 0;
    const p = (y >> 1) | 0;
    const q = (y & 1) | 0;
    switch (x) {
        case 0:
        case 3: {
            break;
        }
        case 1: {
            switch (z) {
                case 0: {
                    Z80__PassTime_Z524259A4(z80, 4);
                    const result = Z80__In_Z524259A4(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC)) | 0;
                    Z80__SetFlags_Z7353D318(z80, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(Z80__Flags(z80), Flags_Carry()), parityFlagsFor(result)));
                    if (y !== 6) {
                        setR(z80, HlSet.Base, y, false, result);
                    }
                    break;
                }
                case 1: {
                    Z80__PassTime_Z524259A4(z80, 4);
                    if (y === 6) {
                        Z80__Out_Z37302880(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC), 0);
                    }
                    else {
                        Z80__Out_Z37302880(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC), getR(z80, HlSet.Base, y, false));
                    }
                    break;
                }
                case 2: {
                    const rhs = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), rpHighLow(HlSet.Base, p)) | 0;
                    const lhs = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL) | 0;
                    const patternInput = (q === 0) ? sbc16(lhs, rhs, (copyOfStruct = Z80__Flags(z80), Flags__get_carry(copyOfStruct))) : adc16(lhs, rhs, (copyOfStruct_1 = Z80__Flags(z80), Flags__get_carry(copyOfStruct_1)));
                    RegisterFile__Set_488BADFE(Z80__get_Regs(z80), R16.HL, patternInput[0]);
                    Z80__SetFlags_Z7353D318(z80, patternInput[1]);
                    Z80__PassTime_Z524259A4(z80, 7);
                    break;
                }
                case 3: {
                    const addr = Z80__ReadImmediate16(z80) | 0;
                    if (q === 0) {
                        Z80__Write_Z37302880(z80, addr, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), rpLow(HlSet.Base, p)));
                        Z80__Write_Z37302880(z80, (addr + 1) & 65535, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), rpHigh(HlSet.Base, p)));
                    }
                    else {
                        RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), rpLow(HlSet.Base, p), Z80__Read_Z524259A4(z80, addr));
                        RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), rpHigh(HlSet.Base, p), Z80__Read_Z524259A4(z80, (addr + 1) & 65535));
                    }
                    break;
                }
                case 4: {
                    const patternInput_1 = sub8(0, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A), false);
                    RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_1[0]);
                    Z80__SetFlags_Z7353D318(z80, patternInput_1[1]);
                    break;
                }
                case 5: {
                    if (y !== 1) {
                        Z80__set_Iff1_Z1FBCCD16(z80, Z80__get_Iff2(z80));
                    }
                    const returnAddress = Z80__Pop16(z80) | 0;
                    RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), returnAddress);
                    break;
                }
                case 6: {
                    Z80__set_IrqMode_Z524259A4(z80, item(y, imTable));
                    break;
                }
                case 7: {
                    switch (y) {
                        case 0: {
                            Z80__PassTime_Z524259A4(z80, 1);
                            RegisterFile__SetI_Z524259A4(Z80__get_Regs(z80), RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A));
                            break;
                        }
                        case 1: {
                            Z80__PassTime_Z524259A4(z80, 1);
                            RegisterFile__SetR_Z524259A4(Z80__get_Regs(z80), RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A));
                            break;
                        }
                        case 2: {
                            const result_3 = RegisterFile__I(Z80__get_Regs(z80)) | 0;
                            Z80__PassTime_Z524259A4(z80, 1);
                            Z80__SetFlags_Z7353D318(z80, iff2FlagsFor(result_3, Z80__Flags(z80), Z80__get_Iff2(z80)));
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, result_3);
                            break;
                        }
                        case 3: {
                            const result_4 = RegisterFile__R(Z80__get_Regs(z80)) | 0;
                            Z80__PassTime_Z524259A4(z80, 1);
                            Z80__SetFlags_Z7353D318(z80, iff2FlagsFor(result_4, Z80__Flags(z80), Z80__get_Iff2(z80)));
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, result_4);
                            break;
                        }
                        case 4: {
                            const address = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL) | 0;
                            const indHl = Z80__Read_Z524259A4(z80, address) | 0;
                            const prevA = RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A) | 0;
                            const newA = ((prevA & 240) | (indHl & 15)) | 0;
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, newA);
                            Z80__PassTime_Z524259A4(z80, 4);
                            Z80__Write_Z37302880(z80, address, ((indHl >> 4) | ((prevA & 15) << 4)) & 255);
                            Z80__SetFlags_Z7353D318(z80, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(Z80__Flags(z80), Flags_Carry()), parityFlagsFor(newA)));
                            break;
                        }
                        case 5: {
                            const address_1 = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL) | 0;
                            const indHl_1 = Z80__Read_Z524259A4(z80, address_1) | 0;
                            const prevA_1 = RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A) | 0;
                            const newA_1 = ((prevA_1 & 240) | ((indHl_1 >> 4) & 15)) | 0;
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, newA_1);
                            Z80__PassTime_Z524259A4(z80, 4);
                            Z80__Write_Z37302880(z80, address_1, ((indHl_1 << 4) | (prevA_1 & 15)) & 255);
                            Z80__SetFlags_Z7353D318(z80, Flags_op_BitwiseOr_Z51621B00(Flags_op_BitwiseAnd_Z51621B00(Z80__Flags(z80), Flags_Carry()), parityFlagsFor(newA_1)));
                            break;
                        }
                        default:
                            undefined;
                    }
                    break;
                }
                default:
                    throw new Exception("bad ed z");
            }
            break;
        }
        case 2: {
            switch (z) {
                case 0: {
                    blockLoad(z80, (y & 1) === 0, (y & 2) !== 0);
                    break;
                }
                case 1: {
                    blockCompare(z80, (y & 1) === 0, (y & 2) !== 0);
                    break;
                }
                default:
                    undefined;
            }
            break;
        }
        default:
            throw new Exception("bad ed x");
    }
}

function isIndirectMain(opcode) {
    const x = (opcode >> 6) | 0;
    const y = ((opcode >> 3) & 7) | 0;
    const z = (opcode & 7) | 0;
    if (((((x === 0) && ((z === 4) ? true : (z === 5))) && (y === 6)) ? true : (((x === 0) && (z === 6)) && (y === 6))) ? true : (((x === 1) && !((y === 6) && (z === 6))) && ((y === 6) ? true : (z === 6)))) {
        return true;
    }
    else if (x === 2) {
        return z === 6;
    }
    else {
        return false;
    }
}

function mainOp(z80, opcode, ctx) {
    const x = (opcode >> 6) | 0;
    const y = ((opcode >> 3) & 7) | 0;
    const z = (opcode & 7) | 0;
    const p = (y >> 1) | 0;
    const q = (y & 1) | 0;
    switch (x) {
        case 0: {
            switch (z) {
                case 0: {
                    switch (y) {
                        case 0: {
                            break;
                        }
                        case 1: {
                            RegisterFile__Ex_Z1C3BEB40(Z80__get_Regs(z80), R16.AF, R16.AF_);
                            break;
                        }
                        case 2: {
                            Z80__PassTime_Z524259A4(z80, 1);
                            const offset = signedImm(Z80__ReadImmediate(z80)) | 0;
                            const newB = (RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.B) - 1) | 0;
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.B, newB);
                            if (newB !== 0) {
                                Z80__PassTime_Z524259A4(z80, 5);
                                Z80__Branch_Z524259A4(z80, offset);
                            }
                            break;
                        }
                        case 3: {
                            const offset_1 = signedImm(Z80__ReadImmediate(z80)) | 0;
                            Z80__PassTime_Z524259A4(z80, 5);
                            Z80__Branch_Z524259A4(z80, offset_1);
                            break;
                        }
                        default: {
                            const offset_2 = signedImm(Z80__ReadImmediate(z80)) | 0;
                            if (ccCheck(y - 4, Z80__Flags(z80))) {
                                Z80__PassTime_Z524259A4(z80, 5);
                                Z80__Branch_Z524259A4(z80, offset_2);
                            }
                        }
                    }
                    break;
                }
                case 1: {
                    if (q === 0) {
                        RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), rpLow(ctx, p), Z80__ReadImmediate(z80));
                        RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), rpHigh(ctx, p), Z80__ReadImmediate(z80));
                    }
                    else {
                        const rhs = RegisterFile__Get_6F21F62(Z80__get_Regs(z80), rpHighLow(ctx, p)) | 0;
                        const patternInput = add16(RegisterFile__Get_6F21F62(Z80__get_Regs(z80), hlHighLow(ctx)), rhs, Z80__Flags(z80));
                        RegisterFile__Set_488BADFE(Z80__get_Regs(z80), hlHighLow(ctx), patternInput[0]);
                        Z80__SetFlags_Z7353D318(z80, patternInput[1]);
                        Z80__PassTime_Z524259A4(z80, 7);
                    }
                    break;
                }
                case 2: {
                    if (q === 0) {
                        switch (p) {
                            case 0: {
                                Z80__Write_Z37302880(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC), RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A));
                                break;
                            }
                            case 1: {
                                Z80__Write_Z37302880(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.DE), RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A));
                                break;
                            }
                            case 2: {
                                const address = Z80__ReadImmediate16(z80) | 0;
                                Z80__Write_Z37302880(z80, address, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), hlLow(ctx)));
                                Z80__Write_Z37302880(z80, (address + 1) & 65535, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), hlHigh(ctx)));
                                break;
                            }
                            case 3: {
                                Z80__Write_Z37302880(z80, Z80__ReadImmediate16(z80), RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A));
                                break;
                            }
                            default:
                                throw new Exception("bad p");
                        }
                    }
                    else {
                        switch (p) {
                            case 0: {
                                RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, Z80__Read_Z524259A4(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.BC)));
                                break;
                            }
                            case 1: {
                                RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, Z80__Read_Z524259A4(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.DE)));
                                break;
                            }
                            case 2: {
                                const address_2 = Z80__ReadImmediate16(z80) | 0;
                                RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), hlLow(ctx), Z80__Read_Z524259A4(z80, address_2));
                                RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), hlHigh(ctx), Z80__Read_Z524259A4(z80, (address_2 + 1) & 65535));
                                break;
                            }
                            case 3: {
                                const address_3 = Z80__ReadImmediate16(z80) | 0;
                                RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, Z80__Read_Z524259A4(z80, address_3));
                                break;
                            }
                            default:
                                throw new Exception("bad p");
                        }
                    }
                    break;
                }
                case 3: {
                    if (q === 0) {
                        RegisterFile__Set_488BADFE(Z80__get_Regs(z80), rpHighLow(ctx, p), (RegisterFile__Get_6F21F62(Z80__get_Regs(z80), rpHighLow(ctx, p)) + 1) & 65535);
                        Z80__PassTime_Z524259A4(z80, 2);
                    }
                    else {
                        RegisterFile__Set_488BADFE(Z80__get_Regs(z80), rpHighLow(ctx, p), (RegisterFile__Get_6F21F62(Z80__get_Regs(z80), rpHighLow(ctx, p)) - 1) & 65535);
                        Z80__PassTime_Z524259A4(z80, 2);
                    }
                    break;
                }
                case 4: {
                    const rhs_1 = getR(z80, ctx, y, false) | 0;
                    if (y === 6) {
                        Z80__PassTime_Z524259A4(z80, 1);
                    }
                    const patternInput_1 = inc8(rhs_1, Z80__Flags(z80));
                    setR(z80, ctx, y, false, patternInput_1[0]);
                    Z80__SetFlags_Z7353D318(z80, patternInput_1[1]);
                    break;
                }
                case 5: {
                    const rhs_2 = getR(z80, ctx, y, false) | 0;
                    if (y === 6) {
                        Z80__PassTime_Z524259A4(z80, 1);
                    }
                    const patternInput_2 = dec8(rhs_2, Z80__Flags(z80));
                    setR(z80, ctx, y, false, patternInput_2[0]);
                    Z80__SetFlags_Z7353D318(z80, patternInput_2[1]);
                    break;
                }
                case 6: {
                    setR(z80, ctx, y, false, Z80__ReadImmediate(z80));
                    break;
                }
                case 7: {
                    const a = RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A) | 0;
                    const flags_3 = Z80__Flags(z80);
                    switch (y) {
                        case 0: {
                            const patternInput_3 = fastRotateCircular8(a, Direction.Left, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_3[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_3[1]);
                            break;
                        }
                        case 1: {
                            const patternInput_4 = fastRotateCircular8(a, Direction.Right, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_4[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_4[1]);
                            break;
                        }
                        case 2: {
                            const patternInput_5 = fastRotate8(a, Direction.Left, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_5[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_5[1]);
                            break;
                        }
                        case 3: {
                            const patternInput_6 = fastRotate8(a, Direction.Right, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_6[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_6[1]);
                            break;
                        }
                        case 4: {
                            const patternInput_7 = daa(a, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_7[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_7[1]);
                            break;
                        }
                        case 5: {
                            const patternInput_8 = cpl(a, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_8[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_8[1]);
                            break;
                        }
                        case 6: {
                            const patternInput_9 = scf(a, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_9[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_9[1]);
                            break;
                        }
                        case 7: {
                            const patternInput_10 = ccf(a, flags_3);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_10[0]);
                            Z80__SetFlags_Z7353D318(z80, patternInput_10[1]);
                            break;
                        }
                        default:
                            throw new Exception("bad fast alu y");
                    }
                    break;
                }
                default:
                    throw new Exception("bad z");
            }
            break;
        }
        case 1: {
            if ((y === 6) && (z === 6)) {
                Z80__Halt(z80);
            }
            else {
                setR(z80, ctx, y, z === 6, getR(z80, ctx, z, y === 6));
            }
            break;
        }
        case 2: {
            const patternInput_11 = aluOp(y, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A), getR(z80, ctx, z, false), Z80__Flags(z80));
            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_11[0]);
            Z80__SetFlags_Z7353D318(z80, patternInput_11[1]);
            break;
        }
        case 3: {
            switch (z) {
                case 0: {
                    Z80__PassTime_Z524259A4(z80, 1);
                    if (ccCheck(y, Z80__Flags(z80))) {
                        const returnAddress = Z80__Pop16(z80) | 0;
                        RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), returnAddress);
                    }
                    break;
                }
                case 1: {
                    if (q === 0) {
                        const result_4 = Z80__Pop16(z80) | 0;
                        RegisterFile__Set_488BADFE(Z80__get_Regs(z80), rp2HighLow(ctx, p), result_4);
                    }
                    else {
                        switch (p) {
                            case 0: {
                                const returnAddress_1 = Z80__Pop16(z80) | 0;
                                RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), returnAddress_1);
                                break;
                            }
                            case 1: {
                                RegisterFile__Exx(Z80__get_Regs(z80));
                                break;
                            }
                            case 2: {
                                RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), RegisterFile__Get_6F21F62(Z80__get_Regs(z80), hlHighLow(ctx)));
                                break;
                            }
                            case 3: {
                                Z80__PassTime_Z524259A4(z80, 2);
                                RegisterFile__SetSp_Z524259A4(Z80__get_Regs(z80), RegisterFile__Get_6F21F62(Z80__get_Regs(z80), hlHighLow(ctx)));
                                break;
                            }
                            default:
                                throw new Exception("bad p");
                        }
                    }
                    break;
                }
                case 2: {
                    const jumpAddress = Z80__ReadImmediate16(z80) | 0;
                    if (ccCheck(y, Z80__Flags(z80))) {
                        RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), jumpAddress);
                    }
                    break;
                }
                case 3: {
                    switch (y) {
                        case 0: {
                            const jumpAddress_1 = Z80__ReadImmediate16(z80) | 0;
                            RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), jumpAddress_1);
                            break;
                        }
                        case 1: {
                            switch (ctx.tag) {
                                case 1: {
                                    decodeAndRunDDCB(z80);
                                    break;
                                }
                                case 2: {
                                    decodeAndRunFDCB(z80);
                                    break;
                                }
                                default:
                                    decodeAndRunCB(z80);
                            }
                            break;
                        }
                        case 2: {
                            const a_2 = RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A) | 0;
                            const port = ((Z80__ReadImmediate(z80) | (a_2 << 8)) & 65535) | 0;
                            Z80__PassTime_Z524259A4(z80, 4);
                            Z80__Out_Z37302880(z80, port, a_2);
                            break;
                        }
                        case 3: {
                            const port_1 = ((Z80__ReadImmediate(z80) | (RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A) << 8)) & 65535) | 0;
                            Z80__PassTime_Z524259A4(z80, 4);
                            RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, Z80__In_Z524259A4(z80, port_1));
                            break;
                        }
                        case 4: {
                            const sp = RegisterFile__Sp(Z80__get_Regs(z80)) | 0;
                            const spOldLow = Z80__Read_Z524259A4(z80, sp) | 0;
                            Z80__PassTime_Z524259A4(z80, 1);
                            const spOldHigh = Z80__Read_Z524259A4(z80, (sp + 1) & 65535) | 0;
                            Z80__Write_Z37302880(z80, sp, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), hlLow(ctx)));
                            Z80__PassTime_Z524259A4(z80, 2);
                            Z80__Write_Z37302880(z80, (sp + 1) & 65535, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), hlHigh(ctx)));
                            RegisterFile__Set_488BADFE(Z80__get_Regs(z80), hlHighLow(ctx), (spOldLow | (spOldHigh << 8)) & 65535);
                            break;
                        }
                        case 5: {
                            RegisterFile__Ex_Z1C3BEB40(Z80__get_Regs(z80), R16.DE, hlHighLow(ctx));
                            break;
                        }
                        case 6: {
                            Z80__set_Iff1_Z1FBCCD16(z80, false);
                            Z80__set_Iff2_Z1FBCCD16(z80, false);
                            break;
                        }
                        case 7: {
                            Z80__set_Iff1_Z1FBCCD16(z80, true);
                            Z80__set_Iff2_Z1FBCCD16(z80, true);
                            break;
                        }
                        default:
                            throw new Exception("bad y");
                    }
                    break;
                }
                case 4: {
                    const jumpAddress_2 = Z80__ReadImmediate16(z80) | 0;
                    if (ccCheck(y, Z80__Flags(z80))) {
                        Z80__PassTime_Z524259A4(z80, 1);
                        Z80__Push16_Z524259A4(z80, RegisterFile__Pc(Z80__get_Regs(z80)));
                        RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), jumpAddress_2);
                    }
                    break;
                }
                case 5: {
                    if (q === 0) {
                        Z80__PassTime_Z524259A4(z80, 1);
                        Z80__Push16_Z524259A4(z80, RegisterFile__Get_6F21F62(Z80__get_Regs(z80), rp2HighLow(ctx, p)));
                    }
                    else {
                        switch (p) {
                            case 0: {
                                const jumpAddress_3 = Z80__ReadImmediate16(z80) | 0;
                                Z80__PassTime_Z524259A4(z80, 1);
                                Z80__Push16_Z524259A4(z80, RegisterFile__Pc(Z80__get_Regs(z80)));
                                RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), jumpAddress_3);
                                break;
                            }
                            case 1: {
                                decodeAndRunDD(z80);
                                break;
                            }
                            case 2: {
                                decodeAndRunED(z80);
                                break;
                            }
                            case 3: {
                                decodeAndRunFD(z80);
                                break;
                            }
                            default:
                                throw new Exception("bad p");
                        }
                    }
                    break;
                }
                case 6: {
                    const patternInput_12 = aluOp(y, RegisterFile__Get_2EC184DD(Z80__get_Regs(z80), R8.A), Z80__ReadImmediate(z80), Z80__Flags(z80));
                    RegisterFile__Set_Z54B079DF(Z80__get_Regs(z80), R8.A, patternInput_12[0]);
                    Z80__SetFlags_Z7353D318(z80, patternInput_12[1]);
                    break;
                }
                case 7: {
                    Z80__PassTime_Z524259A4(z80, 1);
                    Z80__Push16_Z524259A4(z80, RegisterFile__Pc(Z80__get_Regs(z80)));
                    RegisterFile__SetPc_Z524259A4(Z80__get_Regs(z80), y * 8);
                    break;
                }
                default:
                    throw new Exception("bad z");
            }
            break;
        }
        default:
            throw new Exception("bad x");
    }
}

export function decodeAndRunCB(z80) {
    const opcode = Z80__ReadOpcode(z80) | 0;
    if ((opcode & 7) === 6) {
        RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL));
    }
    cbOp(z80, opcode, HlSet.Base);
}

export function decodeAndRunED(z80) {
    edOp(z80, Z80__ReadOpcode(z80));
}

export function decodeAndRunDD(z80) {
    const opcode = Z80__ReadOpcode(z80) | 0;
    if (isIndirectMain(opcode)) {
        const offset = signedImm(Z80__ReadImmediate(z80)) | 0;
        Z80__PassTime_Z524259A4(z80, (opcode === 54) ? 2 : 5);
        RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Ix(Z80__get_Regs(z80)) + offset) & 65535);
    }
    mainOp(z80, opcode, HlSet.Ix);
}

export function decodeAndRunFD(z80) {
    const opcode = Z80__ReadOpcode(z80) | 0;
    if (isIndirectMain(opcode)) {
        const offset = signedImm(Z80__ReadImmediate(z80)) | 0;
        Z80__PassTime_Z524259A4(z80, (opcode === 54) ? 2 : 5);
        RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Iy(Z80__get_Regs(z80)) + offset) & 65535);
    }
    mainOp(z80, opcode, HlSet.Iy);
}

export function decodeAndRunDDCB(z80) {
    const offset = signedImm(Z80__ReadImmediate(z80)) | 0;
    Z80__PassTime_Z524259A4(z80, 1);
    RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Ix(Z80__get_Regs(z80)) + offset) & 65535);
    cbOp(z80, Z80__ReadOpcode(z80), HlSet.Ix);
}

export function decodeAndRunFDCB(z80) {
    const offset = signedImm(Z80__ReadImmediate(z80)) | 0;
    Z80__PassTime_Z524259A4(z80, 1);
    RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), (RegisterFile__Iy(Z80__get_Regs(z80)) + offset) & 65535);
    cbOp(z80, Z80__ReadOpcode(z80), HlSet.Iy);
}

/**
 * Top-level dispatch for the base table (build_execute_hl).
 */
export function executeOpcode(z80, opcode) {
    if (isIndirectMain(opcode)) {
        RegisterFile__SetWz_Z524259A4(Z80__get_Regs(z80), RegisterFile__Get_6F21F62(Z80__get_Regs(z80), R16.HL));
    }
    mainOp(z80, opcode, HlSet.Base);
}

function install() {
    Z80Dispatch_set_Run_3708BEA5((z, opcode) => {
        executeOpcode(z, opcode);
    });
}

/**
 * Ensure the opcode dispatcher is installed. Call before any ExecuteOne.
 */
export function EnsureInstalled() {
    install();
}

install();

