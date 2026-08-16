
import { Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { record_type, int32_type, string_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { max, min } from "../fable_modules/fable-library-js.5.13.0/Double.js";
import { printf, toText, join, substring } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { item, map } from "../fable_modules/fable-library-js.5.13.0/Array.js";

export class Insn extends Record {
    constructor(Text$, Length) {
        super();
        this.Text = Text$;
        this.Length = (Length | 0);
    }
}

export function Insn_$reflection() {
    return record_type("JetpacFR.Core.Disasm.Insn", [], Insn, () => [["Text", string_type], ["Length", int32_type]]);
}

const reg8 = ["B", "C", "D", "E", "H", "L", "(HL)", "A"];

const reg16 = ["BC", "DE", "HL", "SP"];

const popPush = ["BC", "DE", "HL", "AF"];

const conds = ["NZ", "Z", "NC", "C", "PO", "PE", "P", "M"];

const alu = ["ADD", "ADC", "SUB", "SBC", "AND", "XOR", "OR", "CP"];

const rot = ["RLC", "RRC", "RL", "RR", "SLA", "SRA", "SLL", "SRL"];

function s8(b) {
    return ((b + 0x80 & 0xFF) - 0x80) | 0;
}

function sign(d) {
    if (d < 0) {
        return "-";
    }
    else {
        return "+";
    }
}

function usesHlMem(op) {
    let matchResult;
    switch (op) {
        case 52:
        case 53:
        case 54: {
            matchResult = 0;
            break;
        }
        case 70:
        case 78:
        case 86:
        case 94:
        case 102:
        case 110:
        case 126: {
            matchResult = 1;
            break;
        }
        case 112:
        case 113:
        case 114:
        case 115:
        case 116:
        case 117:
        case 119: {
            matchResult = 2;
            break;
        }
        default:
            if ((op >= 128) && (op <= 191)) {
                matchResult = 3;
            }
            else if (((op >= 64) && (op <= 127)) && (op !== 118)) {
                matchResult = 4;
            }
            else {
                matchResult = 5;
            }
    }
    switch (matchResult) {
        case 0:
            return true;
        case 1:
            return true;
        case 2:
            return true;
        case 3:
            return (op & 7) === 6;
        case 4:
            if (((op >> 3) & 7) === 6) {
                return true;
            }
            else {
                return (op & 7) === 6;
            }
        default:
            return false;
    }
}

function baseLen(op) {
    switch (op) {
        case 1:
        case 17:
        case 33:
        case 34:
        case 42:
        case 49:
        case 50:
        case 58:
        case 194:
        case 195:
        case 196:
        case 202:
        case 204:
        case 205:
        case 210:
        case 212:
        case 218:
        case 220:
        case 226:
        case 228:
        case 234:
        case 236:
        case 242:
        case 244:
        case 250:
        case 252:
            return 3;
        case 6:
        case 14:
        case 16:
        case 22:
        case 24:
        case 30:
        case 32:
        case 38:
        case 40:
        case 46:
        case 48:
        case 54:
        case 56:
        case 62:
        case 198:
        case 206:
        case 211:
        case 214:
        case 219:
        case 222:
        case 230:
        case 238:
        case 246:
        case 254:
            return 2;
        case 203:
        case 237:
            return 2;
        case 221:
        case 253:
            return 1;
        default:
            return 1;
    }
}

function subst(ixName, d, withDisp, text) {
    let idx;
    const matchValue = text.indexOf(" ") | 0;
    const matchValue_1 = text.indexOf(",") | 0;
    let matchResult, s_6, c_6, s_7;
    if (matchValue === -1) {
        if (matchValue_1 >= 0) {
            matchResult = 0;
        }
        else if (matchValue_1 === -1) {
            if (matchValue >= 0) {
                matchResult = 1;
                s_6 = matchValue;
            }
            else if ((matchValue >= 0) && (matchValue_1 >= 0)) {
                matchResult = 2;
                c_6 = matchValue_1;
                s_7 = matchValue;
            }
            else {
                matchResult = 3;
            }
        }
        else if ((matchValue >= 0) && (matchValue_1 >= 0)) {
            matchResult = 2;
            c_6 = matchValue_1;
            s_7 = matchValue;
        }
        else {
            matchResult = 3;
        }
    }
    else if (matchValue_1 === -1) {
        if (matchValue >= 0) {
            matchResult = 1;
            s_6 = matchValue;
        }
        else if ((matchValue >= 0) && (matchValue_1 >= 0)) {
            matchResult = 2;
            c_6 = matchValue_1;
            s_7 = matchValue;
        }
        else {
            matchResult = 3;
        }
    }
    else if ((matchValue >= 0) && (matchValue_1 >= 0)) {
        matchResult = 2;
        c_6 = matchValue_1;
        s_7 = matchValue;
    }
    else {
        matchResult = 3;
    }
    switch (matchResult) {
        case 0: {
            idx = matchValue_1;
            break;
        }
        case 1: {
            idx = s_6;
            break;
        }
        case 2: {
            idx = min(s_7, c_6);
            break;
        }
        default:
            idx = -1;
    }
    if (idx < 0) {
        return text;
    }
    else {
        return substring(text, 0, idx + 1) + join(",", map((o) => {
            switch (o) {
                case "(HL)":
                    if (withDisp) {
                        const arg_1 = sign(d);
                        const arg_2 = Math.abs(d) | 0;
                        return toText(printf("(%s%s0x%02X)"))(ixName)(arg_1)(arg_2);
                    }
                    else {
                        return toText(printf("(%s)"))(ixName);
                    }
                case "HL":
                    return ixName;
                case "H":
                    return ixName + "H";
                case "L":
                    return ixName + "L";
                default:
                    return o;
            }
        }, substring(text, idx + 1).split(",")));
    }
}

function renderBase(get$, pc, ix) {
    const at = (o) => get$(pc + o);
    const n1 = () => (~~at(1) | 0);
    const n2 = () => (~~at(2) | 0);
    const nn = () => ((n1() | (n2() << 8)) | 0);
    const op = ~~at(0) | 0;
    const rp = ((op >> 4) & 3) | 0;
    const cc = ((op >> 3) & 7) | 0;
    const target = () => ((((pc + 2) + s8(at(1))) & 65535) | 0);
    let text;
    let matchResult;
    switch (op) {
        case 0: {
            matchResult = 0;
            break;
        }
        case 1:
        case 17:
        case 33:
        case 49: {
            matchResult = 1;
            break;
        }
        case 2: {
            matchResult = 2;
            break;
        }
        case 3:
        case 19:
        case 35:
        case 51: {
            matchResult = 10;
            break;
        }
        case 4:
        case 12:
        case 20:
        case 28:
        case 36:
        case 44:
        case 52:
        case 60: {
            matchResult = 13;
            break;
        }
        case 5:
        case 13:
        case 21:
        case 29:
        case 37:
        case 45:
        case 53:
        case 61: {
            matchResult = 14;
            break;
        }
        case 6:
        case 14:
        case 22:
        case 30:
        case 38:
        case 46:
        case 62: {
            matchResult = 15;
            break;
        }
        case 7: {
            matchResult = 16;
            break;
        }
        case 8: {
            matchResult = 20;
            break;
        }
        case 9:
        case 25:
        case 41:
        case 57: {
            matchResult = 12;
            break;
        }
        case 10: {
            matchResult = 3;
            break;
        }
        case 11:
        case 27:
        case 43:
        case 59: {
            matchResult = 11;
            break;
        }
        case 15: {
            matchResult = 17;
            break;
        }
        case 16: {
            matchResult = 21;
            break;
        }
        case 18: {
            matchResult = 4;
            break;
        }
        case 23: {
            matchResult = 18;
            break;
        }
        case 24: {
            matchResult = 22;
            break;
        }
        case 26: {
            matchResult = 5;
            break;
        }
        case 31: {
            matchResult = 19;
            break;
        }
        case 32:
        case 40:
        case 48:
        case 56: {
            matchResult = 23;
            break;
        }
        case 34: {
            matchResult = 6;
            break;
        }
        case 39: {
            matchResult = 24;
            break;
        }
        case 42: {
            matchResult = 7;
            break;
        }
        case 47: {
            matchResult = 25;
            break;
        }
        case 50: {
            matchResult = 8;
            break;
        }
        case 54: {
            matchResult = 28;
            break;
        }
        case 55: {
            matchResult = 26;
            break;
        }
        case 58: {
            matchResult = 9;
            break;
        }
        case 63: {
            matchResult = 27;
            break;
        }
        case 192: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 193: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 32;
            }
            break;
        }
        case 194: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 195: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 35;
            }
            break;
        }
        case 196: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 197: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 33;
            }
            break;
        }
        case 198: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 199: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 200: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 201: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 40;
            }
            break;
        }
        case 202: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 204: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 205: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 37;
            }
            break;
        }
        case 206: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 207: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 208: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 209: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 32;
            }
            break;
        }
        case 210: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 211: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 41;
            }
            break;
        }
        case 212: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 213: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 33;
            }
            break;
        }
        case 214: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 215: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 216: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 217: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 43;
            }
            break;
        }
        case 218: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 219: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 42;
            }
            break;
        }
        case 220: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 222: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 223: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 224: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 225: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 32;
            }
            break;
        }
        case 226: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 227: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 44;
            }
            break;
        }
        case 228: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 229: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 33;
            }
            break;
        }
        case 230: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 231: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 232: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 233: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 45;
            }
            break;
        }
        case 234: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 235: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 46;
            }
            break;
        }
        case 236: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 238: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 239: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 240: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 241: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 32;
            }
            break;
        }
        case 242: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 243: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 47;
            }
            break;
        }
        case 244: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 245: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 33;
            }
            break;
        }
        case 246: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 247: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        case 248: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 31;
            }
            break;
        }
        case 249: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 49;
            }
            break;
        }
        case 250: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 34;
            }
            break;
        }
        case 251: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 48;
            }
            break;
        }
        case 252: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 36;
            }
            break;
        }
        case 254: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 38;
            }
            break;
        }
        case 255: {
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 39;
            }
            break;
        }
        default:
            if ((op >= 64) && (op <= 127)) {
                matchResult = 29;
            }
            else if ((op >= 128) && (op <= 191)) {
                matchResult = 30;
            }
            else {
                matchResult = 50;
            }
    }
    switch (matchResult) {
        case 0: {
            text = "NOP";
            break;
        }
        case 1: {
            const arg = item(rp, reg16);
            const arg_1 = nn() | 0;
            text = toText(printf("LD %s,0x%04X"))(arg)(arg_1);
            break;
        }
        case 2: {
            text = "LD (BC),A";
            break;
        }
        case 3: {
            text = "LD A,(BC)";
            break;
        }
        case 4: {
            text = "LD (DE),A";
            break;
        }
        case 5: {
            text = "LD A,(DE)";
            break;
        }
        case 6: {
            const arg_2 = nn() | 0;
            text = toText(printf("LD (0x%04X),HL"))(arg_2);
            break;
        }
        case 7: {
            const arg_3 = nn() | 0;
            text = toText(printf("LD HL,(0x%04X)"))(arg_3);
            break;
        }
        case 8: {
            const arg_4 = nn() | 0;
            text = toText(printf("LD (0x%04X),A"))(arg_4);
            break;
        }
        case 9: {
            const arg_5 = nn() | 0;
            text = toText(printf("LD A,(0x%04X)"))(arg_5);
            break;
        }
        case 10: {
            const arg_6 = item(rp, reg16);
            text = toText(printf("INC %s"))(arg_6);
            break;
        }
        case 11: {
            const arg_7 = item(rp, reg16);
            text = toText(printf("DEC %s"))(arg_7);
            break;
        }
        case 12: {
            const arg_8 = item(rp, reg16);
            text = toText(printf("ADD HL,%s"))(arg_8);
            break;
        }
        case 13: {
            const arg_9 = item((op >> 3) & 7, reg8);
            text = toText(printf("INC %s"))(arg_9);
            break;
        }
        case 14: {
            const arg_10 = item((op >> 3) & 7, reg8);
            text = toText(printf("DEC %s"))(arg_10);
            break;
        }
        case 15: {
            const arg_11 = item((op >> 3) & 7, reg8);
            const arg_12 = n1() | 0;
            text = toText(printf("LD %s,0x%02X"))(arg_11)(arg_12);
            break;
        }
        case 16: {
            text = "RLCA";
            break;
        }
        case 17: {
            text = "RRCA";
            break;
        }
        case 18: {
            text = "RLA";
            break;
        }
        case 19: {
            text = "RRA";
            break;
        }
        case 20: {
            text = "EX AF,AF\'";
            break;
        }
        case 21: {
            const arg_13 = target() | 0;
            text = toText(printf("DJNZ 0x%04X"))(arg_13);
            break;
        }
        case 22: {
            const arg_14 = target() | 0;
            text = toText(printf("JR 0x%04X"))(arg_14);
            break;
        }
        case 23: {
            const arg_15 = item((op >> 3) & 3, conds);
            const arg_16 = target() | 0;
            text = toText(printf("JR %s,0x%04X"))(arg_15)(arg_16);
            break;
        }
        case 24: {
            text = "DAA";
            break;
        }
        case 25: {
            text = "CPL";
            break;
        }
        case 26: {
            text = "SCF";
            break;
        }
        case 27: {
            text = "CCF";
            break;
        }
        case 28: {
            if (ix == null) {
                const arg_18 = n1() | 0;
                text = toText(printf("LD (HL),0x%02X"))(arg_18);
            }
            else {
                const arg_17 = n2() | 0;
                text = toText(printf("LD (HL),0x%02X"))(arg_17);
            }
            break;
        }
        case 29: {
            if (op === 118) {
                text = "HALT";
            }
            else {
                const arg_19 = item((op >> 3) & 7, reg8);
                const arg_20 = item(op & 7, reg8);
                text = toText(printf("LD %s,%s"))(arg_19)(arg_20);
            }
            break;
        }
        case 30: {
            const al = item((op >> 3) & 7, alu);
            if (((al === "ADD") ? true : (al === "ADC")) ? true : (al === "SBC")) {
                const arg_22 = item(op & 7, reg8);
                text = toText(printf("%s A,%s"))(al)(arg_22);
            }
            else {
                const arg_24 = item(op & 7, reg8);
                text = toText(printf("%s %s"))(al)(arg_24);
            }
            break;
        }
        case 31: {
            const arg_25 = item(cc, conds);
            text = toText(printf("RET %s"))(arg_25);
            break;
        }
        case 32: {
            const arg_26 = item((op >> 4) & 3, popPush);
            text = toText(printf("POP %s"))(arg_26);
            break;
        }
        case 33: {
            const arg_27 = item((op >> 4) & 3, popPush);
            text = toText(printf("PUSH %s"))(arg_27);
            break;
        }
        case 34: {
            const arg_28 = item(cc, conds);
            const arg_29 = nn() | 0;
            text = toText(printf("JP %s,0x%04X"))(arg_28)(arg_29);
            break;
        }
        case 35: {
            const arg_30 = nn() | 0;
            text = toText(printf("JP 0x%04X"))(arg_30);
            break;
        }
        case 36: {
            const arg_31 = item(cc, conds);
            const arg_32 = nn() | 0;
            text = toText(printf("CALL %s,0x%04X"))(arg_31)(arg_32);
            break;
        }
        case 37: {
            const arg_33 = nn() | 0;
            text = toText(printf("CALL 0x%04X"))(arg_33);
            break;
        }
        case 38: {
            const al_1 = item((op >> 3) & 7, alu);
            if (((al_1 === "ADD") ? true : (al_1 === "ADC")) ? true : (al_1 === "SBC")) {
                const arg_35 = n1() | 0;
                text = toText(printf("%s A,0x%02X"))(al_1)(arg_35);
            }
            else {
                const arg_37 = n1() | 0;
                text = toText(printf("%s 0x%02X"))(al_1)(arg_37);
            }
            break;
        }
        case 39: {
            const arg_38 = (op & 56) | 0;
            text = toText(printf("RST 0x%02X"))(arg_38);
            break;
        }
        case 40: {
            text = "RET";
            break;
        }
        case 41: {
            const arg_39 = n1() | 0;
            text = toText(printf("OUT (0x%02X),A"))(arg_39);
            break;
        }
        case 42: {
            const arg_40 = n1() | 0;
            text = toText(printf("IN A,(0x%02X)"))(arg_40);
            break;
        }
        case 43: {
            text = "EXX";
            break;
        }
        case 44: {
            text = "EX (SP),HL";
            break;
        }
        case 45: {
            text = "JP (HL)";
            break;
        }
        case 46: {
            text = "EX DE,HL";
            break;
        }
        case 47: {
            text = "DI";
            break;
        }
        case 48: {
            text = "EI";
            break;
        }
        case 49: {
            text = "LD SP,HL";
            break;
        }
        default:
            text = toText(printf("DB %02X"))(op);
    }
    if (ix != null) {
        return subst(ix[0], ix[1], ix[2], text);
    }
    else {
        return text;
    }
}

function cbText(op, opnd) {
    if (op < 64) {
        const arg = item((op >> 3) & 7, rot);
        return toText(printf("%s %s"))(arg)(opnd);
    }
    else if (op < 128) {
        const arg_2 = ((op >> 3) & 7) | 0;
        return toText(printf("BIT %d,%s"))(arg_2)(opnd);
    }
    else if (op < 192) {
        const arg_4 = ((op >> 3) & 7) | 0;
        return toText(printf("RES %d,%s"))(arg_4)(opnd);
    }
    else {
        const arg_6 = ((op >> 3) & 7) | 0;
        return toText(printf("SET %d,%s"))(arg_6)(opnd);
    }
}

function edLen(op) {
    switch (op) {
        case 67:
        case 75:
        case 83:
        case 91:
        case 99:
        case 107:
        case 115:
        case 123:
            return 4;
        default:
            return 2;
    }
}

function edText(get$, pc) {
    let op_1;
    const op = ~~get$(pc) | 0;
    const at = (o) => get$(pc + o);
    const rp = ((op >> 4) & 3) | 0;
    let matchResult;
    if ((op_1 = (op | 0), ((op_1 >= 64) && (op_1 <= 127)) && (((op_1 & 7) === 0) ? true : ((op_1 & 7) === 1)))) {
        matchResult = 0;
    }
    else {
        switch (op) {
            case 66:
            case 82:
            case 98:
            case 114: {
                matchResult = 1;
                break;
            }
            case 67:
            case 83:
            case 99:
            case 115: {
                matchResult = 3;
                break;
            }
            case 68:
            case 76:
            case 84:
            case 92:
            case 100:
            case 108:
            case 116:
            case 124: {
                matchResult = 5;
                break;
            }
            case 69:
            case 85:
            case 101:
            case 117:
            case 125: {
                matchResult = 6;
                break;
            }
            case 70:
            case 78:
            case 102:
            case 110: {
                matchResult = 8;
                break;
            }
            case 71: {
                matchResult = 11;
                break;
            }
            case 74:
            case 90:
            case 106:
            case 122: {
                matchResult = 2;
                break;
            }
            case 75:
            case 91:
            case 107:
            case 123: {
                matchResult = 4;
                break;
            }
            case 77: {
                matchResult = 7;
                break;
            }
            case 79: {
                matchResult = 12;
                break;
            }
            case 86:
            case 118: {
                matchResult = 9;
                break;
            }
            case 87: {
                matchResult = 13;
                break;
            }
            case 94:
            case 126: {
                matchResult = 10;
                break;
            }
            case 95: {
                matchResult = 14;
                break;
            }
            case 103: {
                matchResult = 15;
                break;
            }
            case 111: {
                matchResult = 16;
                break;
            }
            case 160: {
                matchResult = 17;
                break;
            }
            case 161: {
                matchResult = 18;
                break;
            }
            case 162: {
                matchResult = 19;
                break;
            }
            case 163: {
                matchResult = 20;
                break;
            }
            case 168: {
                matchResult = 21;
                break;
            }
            case 169: {
                matchResult = 22;
                break;
            }
            case 170: {
                matchResult = 23;
                break;
            }
            case 171: {
                matchResult = 24;
                break;
            }
            case 176: {
                matchResult = 25;
                break;
            }
            case 177: {
                matchResult = 26;
                break;
            }
            case 178: {
                matchResult = 27;
                break;
            }
            case 179: {
                matchResult = 28;
                break;
            }
            case 184: {
                matchResult = 29;
                break;
            }
            case 185: {
                matchResult = 30;
                break;
            }
            case 186: {
                matchResult = 31;
                break;
            }
            case 187: {
                matchResult = 32;
                break;
            }
            default:
                matchResult = 33;
        }
    }
    switch (matchResult) {
        case 0: {
            const op_2 = op | 0;
            switch (op_2) {
                case 112:
                    return "IN (C)";
                case 113:
                    return "OUT (C),0";
                default:
                    if ((op_2 & 1) === 0) {
                        const arg = item((op_2 >> 3) & 7, reg8);
                        return toText(printf("IN %s,(C)"))(arg);
                    }
                    else {
                        const arg_1 = item((op_2 >> 3) & 7, reg8);
                        return toText(printf("OUT (C),%s"))(arg_1);
                    }
            }
        }
        case 1: {
            const arg_2 = item(rp, reg16);
            return toText(printf("SBC HL,%s"))(arg_2);
        }
        case 2: {
            const arg_3 = item(rp, reg16);
            return toText(printf("ADC HL,%s"))(arg_3);
        }
        case 3: {
            const arg_4 = (~~at(1) | (~~at(2) << 8)) | 0;
            const arg_5 = item(rp, reg16);
            return toText(printf("LD (0x%04X),%s"))(arg_4)(arg_5);
        }
        case 4: {
            const arg_6 = item(rp, reg16);
            const arg_7 = (~~at(1) | (~~at(2) << 8)) | 0;
            return toText(printf("LD %s,(0x%04X)"))(arg_6)(arg_7);
        }
        case 5:
            return "NEG";
        case 6:
            return "RETN";
        case 7:
            return "RETI";
        case 8:
            return "IM 0";
        case 9:
            return "IM 1";
        case 10:
            return "IM 2";
        case 11:
            return "LD I,A";
        case 12:
            return "LD R,A";
        case 13:
            return "LD A,I";
        case 14:
            return "LD A,R";
        case 15:
            return "RRD";
        case 16:
            return "RLD";
        case 17:
            return "LDI";
        case 18:
            return "CPI";
        case 19:
            return "INI";
        case 20:
            return "OUTI";
        case 21:
            return "LDD";
        case 22:
            return "CPD";
        case 23:
            return "IND";
        case 24:
            return "OUTD";
        case 25:
            return "LDIR";
        case 26:
            return "CPIR";
        case 27:
            return "INIR";
        case 28:
            return "OTIR";
        case 29:
            return "LDDR";
        case 30:
            return "CPDR";
        case 31:
            return "INDR";
        case 32:
            return "OTDR";
        default:
            return toText(printf("DB ED %02X"))(op);
    }
}

/**
 * Decode one instruction at `pc` using the byte getter (absolute index).
 */
export function decode(get$, pc) {
    let arg_1, arg_2;
    const op = ~~get$(pc) | 0;
    switch (op) {
        case 203: {
            const op2 = ~~get$(pc + 1) | 0;
            return new Insn(cbText(op2, item(op2 & 7, reg8)), 2);
        }
        case 221:
        case 253: {
            const name = (op === 221) ? "IX" : "IY";
            const inner = ~~get$(pc + 1) | 0;
            switch (inner) {
                case 203: {
                    const d = s8(get$(pc + 2)) | 0;
                    return new Insn(cbText(~~get$(pc + 3), (arg_1 = sign(d), (arg_2 = (Math.abs(d) | 0), toText(printf("(%s%s0x%02X)"))(name)(arg_1)(arg_2)))), 4);
                }
                case 237:
                    return new Insn(edText(get$, pc + 1), edLen(inner) + 1);
                default:
                    return new Insn(renderBase(get$, pc + 1, [name, s8(get$(pc + 2)), usesHlMem(inner)]), (baseLen(inner) + (usesHlMem(inner) ? 1 : 0)) + 1);
            }
        }
        case 237: {
            const op2_1 = ~~get$(pc + 1) | 0;
            return new Insn(edText(get$, pc + 1), edLen(op2_1));
        }
        default: {
            const len_1 = (baseLen(op) + 0) | 0;
            return new Insn(renderBase(get$, pc, undefined), len_1);
        }
    }
}

/**
 * Disassemble one instruction at `address` in a 64K memory image.
 */
export function disasmMemory(memory, address) {
    return decode((a) => item(a & 65535, memory), address & 65535);
}

/**
 * Disassemble one instruction at `offset` in a byte slice (trace entries).
 */
export function disasmBytes(bytes, offset) {
    return decode((a) => ((a < bytes.length) ? item(a, bytes) : 0), max(0, offset));
}

