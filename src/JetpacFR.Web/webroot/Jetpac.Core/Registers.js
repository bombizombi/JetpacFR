
import { Union } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { class_type, union_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { item, initialize } from "../fable_modules/fable-library-js.5.17.0/Array.js";

/**
 * 8-bit register selector.
 */
export class R8 extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["A", "F", "B", "C", "D", "E", "H", "L", "A_", "F_", "B_", "C_", "D_", "E_", "H_", "L_", "SPH", "SPL", "IXH", "IXL", "IYH", "IYL"];
    }
    static A = new R8(0, []);
    static F = new R8(1, []);
    static B = new R8(2, []);
    static C = new R8(3, []);
    static D = new R8(4, []);
    static E = new R8(5, []);
    static H = new R8(6, []);
    static L = new R8(7, []);
    static A_ = new R8(8, []);
    static F_ = new R8(9, []);
    static B_ = new R8(10, []);
    static C_ = new R8(11, []);
    static D_ = new R8(12, []);
    static E_ = new R8(13, []);
    static H_ = new R8(14, []);
    static L_ = new R8(15, []);
    static SPH = new R8(16, []);
    static SPL = new R8(17, []);
    static IXH = new R8(18, []);
    static IXL = new R8(19, []);
    static IYH = new R8(20, []);
    static IYL = new R8(21, []);
}

export function R8_$reflection() {
    return union_type("Jetpac.Core.R8", [], R8, () => [[], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], [], []]);
}

/**
 * 16-bit register pair selector.
 */
export class R16 extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["AF", "BC", "DE", "HL", "AF_", "BC_", "DE_", "HL_", "SP", "IX", "IY"];
    }
    static AF = new R16(0, []);
    static BC = new R16(1, []);
    static DE = new R16(2, []);
    static HL = new R16(3, []);
    static AF_ = new R16(4, []);
    static BC_ = new R16(5, []);
    static DE_ = new R16(6, []);
    static HL_ = new R16(7, []);
    static SP = new R16(8, []);
    static IX = new R16(9, []);
    static IY = new R16(10, []);
}

export function R16_$reflection() {
    return union_type("Jetpac.Core.R16", [], R16, () => [[], [], [], [], [], [], [], [], [], [], []]);
}

/**
 * A high/low byte pair. All values are `int`; 8-bit values are masked to
 * 0xFF and 16-bit values to 0xFFFF at the point of use (Fable numbers do not
 * wrap). Initialised to 0xFFFF like the C++ constructor (low_ = 0xff).
 */
export class RegPair {
    constructor() {
        this.low = 255;
        this.high = 255;
    }
}

export function RegPair_$reflection() {
    return class_type("Jetpac.Core.RegPair", undefined, RegPair);
}

export function RegPair_$ctor() {
    return new RegPair();
}

export function RegPair__High(this$) {
    return this$.high | 0;
}

export function RegPair__SetHigh_Z524259A4(this$, v) {
    this$.high = ((v & 255) | 0);
}

export function RegPair__Low(this$) {
    return this$.low | 0;
}

export function RegPair__SetLow_Z524259A4(this$, v) {
    this$.low = ((v & 255) | 0);
}

export function RegPair__HighLow(this$) {
    return (((this$.high << 8) | this$.low) & 65535) | 0;
}

export function RegPair__SetHighLow_Z524259A4(this$, v) {
    this$.high = (((v >> 8) & 255) | 0);
    this$.low = ((v & 255) | 0);
}

/**
 * Port of specbolt's RegisterFile (z80/common/include/z80/common/RegisterFile.hpp).
 */
export class RegisterFile {
    constructor() {
        this.regs = initialize(11, (_arg) => RegPair_$ctor());
        this.wz_ = 65535;
        this.pc_ = 0;
        this.r_ = 0;
        this.i_ = 0;
    }
}

export function RegisterFile_$reflection() {
    return class_type("Jetpac.Core.RegisterFile", undefined, RegisterFile);
}

export function RegisterFile_$ctor() {
    return new RegisterFile();
}

function RegisterFile_PairIndex_2EC184DD(r8) {
    switch (r8.tag) {
        case 2:
        case 3:
            return 1;
        case 4:
        case 5:
            return 2;
        case 6:
        case 7:
            return 3;
        case 8:
        case 9:
            return 4;
        case 10:
        case 11:
            return 5;
        case 12:
        case 13:
            return 6;
        case 14:
        case 15:
            return 7;
        case 16:
        case 17:
            return 8;
        case 18:
        case 19:
            return 9;
        case 20:
        case 21:
            return 10;
        default:
            return 0;
    }
}

function RegisterFile_IsHigh_2EC184DD(r8) {
    switch (r8.tag) {
        case 0:
        case 2:
        case 4:
        case 6:
        case 8:
        case 10:
        case 12:
        case 14:
        case 16:
        case 18:
        case 20:
            return true;
        default:
            return false;
    }
}

function RegisterFile_PairIndex16_6F21F62(r16) {
    switch (r16.tag) {
        case 1:
            return 1;
        case 2:
            return 2;
        case 3:
            return 3;
        case 4:
            return 4;
        case 5:
            return 5;
        case 6:
            return 6;
        case 7:
            return 7;
        case 8:
            return 8;
        case 9:
            return 9;
        case 10:
            return 10;
        default:
            return 0;
    }
}

function RegisterFile__RegFor_2EC184DD(this$, r8) {
    return item(RegisterFile_PairIndex_2EC184DD(r8), this$.regs);
}

function RegisterFile__RegFor_6F21F62(this$, r16) {
    return item(RegisterFile_PairIndex16_6F21F62(r16), this$.regs);
}

export function RegisterFile__Get_2EC184DD(this$, r8) {
    const pair = RegisterFile__RegFor_2EC184DD(this$, r8);
    if (RegisterFile_IsHigh_2EC184DD(r8)) {
        return RegPair__High(pair) | 0;
    }
    else {
        return RegPair__Low(pair) | 0;
    }
}

export function RegisterFile__Set_Z54B079DF(this$, r8, value) {
    const pair = RegisterFile__RegFor_2EC184DD(this$, r8);
    if (RegisterFile_IsHigh_2EC184DD(r8)) {
        RegPair__SetHigh_Z524259A4(pair, value);
    }
    else {
        RegPair__SetLow_Z524259A4(pair, value);
    }
}

export function RegisterFile__Get_6F21F62(this$, r16) {
    return RegPair__HighLow(RegisterFile__RegFor_6F21F62(this$, r16)) | 0;
}

export function RegisterFile__Set_488BADFE(this$, r16, value) {
    RegPair__SetHighLow_Z524259A4(RegisterFile__RegFor_6F21F62(this$, r16), value);
}

export function RegisterFile__Ix(this$) {
    return RegisterFile__Get_6F21F62(this$, R16.IX) | 0;
}

export function RegisterFile__Iy(this$) {
    return RegisterFile__Get_6F21F62(this$, R16.IY) | 0;
}

export function RegisterFile__Sp(this$) {
    return RegisterFile__Get_6F21F62(this$, R16.SP) | 0;
}

export function RegisterFile__SetSp_Z524259A4(this$, v) {
    RegisterFile__Set_488BADFE(this$, R16.SP, v);
}

export function RegisterFile__Pc(this$) {
    return this$.pc_ | 0;
}

export function RegisterFile__SetPc_Z524259A4(this$, v) {
    this$.pc_ = ((v & 65535) | 0);
}

export function RegisterFile__R(this$) {
    return this$.r_ | 0;
}

export function RegisterFile__SetR_Z524259A4(this$, v) {
    this$.r_ = ((v & 255) | 0);
}

export function RegisterFile__I(this$) {
    return this$.i_ | 0;
}

export function RegisterFile__SetI_Z524259A4(this$, v) {
    this$.i_ = ((v & 255) | 0);
}

export function RegisterFile__Wz(this$) {
    return this$.wz_ | 0;
}

export function RegisterFile__SetWz_Z524259A4(this$, v) {
    this$.wz_ = ((v & 65535) | 0);
}

export function RegisterFile__Ex_Z1C3BEB40(this$, lhs, rhs) {
    const l = RegisterFile__RegFor_6F21F62(this$, lhs);
    const r = RegisterFile__RegFor_6F21F62(this$, rhs);
    const tmp = RegPair__HighLow(l) | 0;
    RegPair__SetHighLow_Z524259A4(l, RegPair__HighLow(r));
    RegPair__SetHighLow_Z524259A4(r, tmp);
}

export function RegisterFile__Exx(this$) {
    RegisterFile__Ex_Z1C3BEB40(this$, R16.BC, R16.BC_);
    RegisterFile__Ex_Z1C3BEB40(this$, R16.DE, R16.DE_);
    RegisterFile__Ex_Z1C3BEB40(this$, R16.HL, R16.HL_);
}

