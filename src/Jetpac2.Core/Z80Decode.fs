namespace Jetpac2.Core

/// Decoder table (generated): instruction bytes -> CE op (Make) and F#
/// source text (Format), the reverse of assemble. Rows carry a byte mask
/// (0xFF = fixed, 0x00 = operand wildcard, e.g. DD CB d X has the d masked
/// out); `LabelName` marks relative/absolute jumps so Z80CE can emit
/// symbolic labels.
module Z80Decode =
  open Jetpac2.Core

  type Row =
    { Prefix: byte[]
      Mask: byte[]
      Make: byte[] -> int -> Z80Op
      Format: byte[] -> int -> string
      LabelName: string option }

  let rows : Row list =
    [
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x06uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RLC_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x06uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RLC_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x0euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RRC_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x0euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RRC_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x16uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RL_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x16uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RL_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x1euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RR_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x1euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RR_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x26uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SLA_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x26uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SLA_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x2euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SRA_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x2euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SRA_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x3euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SRL_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x3euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SRL_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x46uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT0_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x46uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT0_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x86uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES0_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x86uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES0_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xc6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET0_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xc6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET0_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x4euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT1_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x4euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT1_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x8euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES1_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x8euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES1_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xceuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET1_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xceuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET1_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x56uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT2_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x56uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT2_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x96uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES2_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x96uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES2_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xd6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET2_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xd6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET2_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x5euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT3_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x5euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT3_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x9euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES3_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x9euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES3_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xdeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET3_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xdeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET3_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x66uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT4_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x66uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT4_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xa6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES4_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xa6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES4_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xe6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET4_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xe6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET4_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x6euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT5_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x6euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT5_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xaeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES5_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xaeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES5_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xeeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET5_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xeeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET5_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x76uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT6_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x76uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT6_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xb6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES6_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xb6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES6_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xf6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET6_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xf6uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET6_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0x7euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT7_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0x7euy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.BIT7_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xbeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES7_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xbeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.RES7_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xcbuy; 0x00uy; 0xfeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET7_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xcbuy; 0x00uy; 0xfeuy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_PTR_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SET7_PTR_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x21uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IX ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_IX" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x21uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IY ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_IY" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x2auy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IX_ptr ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_IX_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x22uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_IX ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_IX" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x2auy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IY_ptr ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_IY_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x22uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_IY ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_IY" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x43uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_BC ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_BC" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x4buy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_BC_ptr ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_BC_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x53uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_DE ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_DE" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x5buy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_DE_ptr ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_DE_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x73uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_SP ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_SP" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x7buy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_SP_ptr ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_SP_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 2]) ||| ((int img.[pc + 3]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xf9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_SP_IX )
        Format = (fun img pc -> "Z80Vocab.LD_SP_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xf9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_SP_IY )
        Format = (fun img pc -> "Z80Vocab.LD_SP_IY" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x57uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_I )
        Format = (fun img pc -> "Z80Vocab.LD_A_I" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x5fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_R )
        Format = (fun img pc -> "Z80Vocab.LD_A_R" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x47uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_I_A )
        Format = (fun img pc -> "Z80Vocab.LD_I_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x4fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_R_A )
        Format = (fun img pc -> "Z80Vocab.LD_R_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x44uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_IXH )
        Format = (fun img pc -> "Z80Vocab.LD_B_IXH" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x45uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_IXL )
        Format = (fun img pc -> "Z80Vocab.LD_B_IXL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x4cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_IXH )
        Format = (fun img pc -> "Z80Vocab.LD_C_IXH" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x4duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_IXL )
        Format = (fun img pc -> "Z80Vocab.LD_C_IXL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x54uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_IXH )
        Format = (fun img pc -> "Z80Vocab.LD_D_IXH" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x55uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_IXL )
        Format = (fun img pc -> "Z80Vocab.LD_D_IXL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x5cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_IXH )
        Format = (fun img pc -> "Z80Vocab.LD_E_IXH" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x5duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_IXL )
        Format = (fun img pc -> "Z80Vocab.LD_E_IXL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x60uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXH_B )
        Format = (fun img pc -> "Z80Vocab.LD_IXH_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x61uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXH_C )
        Format = (fun img pc -> "Z80Vocab.LD_IXH_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x62uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXH_D )
        Format = (fun img pc -> "Z80Vocab.LD_IXH_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x63uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXH_E )
        Format = (fun img pc -> "Z80Vocab.LD_IXH_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x67uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXH_A )
        Format = (fun img pc -> "Z80Vocab.LD_IXH_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x68uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXL_B )
        Format = (fun img pc -> "Z80Vocab.LD_IXL_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x69uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXL_C )
        Format = (fun img pc -> "Z80Vocab.LD_IXL_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x6auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXL_D )
        Format = (fun img pc -> "Z80Vocab.LD_IXL_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x6buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXL_E )
        Format = (fun img pc -> "Z80Vocab.LD_IXL_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x6fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_IXL_A )
        Format = (fun img pc -> "Z80Vocab.LD_IXL_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x26uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXH_n (int img.[pc + 2]))
        Format = (fun img pc -> "Z80Vocab.LD_IXH_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x2euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXL_n (int img.[pc + 2]))
        Format = (fun img pc -> "Z80Vocab.LD_IXL_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x46uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_B_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_B_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x4euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_C_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_C_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x56uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_D_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_D_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x5euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_E_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_E_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x66uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_H_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_H_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x6euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_L_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_L_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x7euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x46uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_B_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_B_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x4euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_C_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_C_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x56uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_D_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_D_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x5euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_E_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_E_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x66uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_H_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_H_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x6euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_L_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_L_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x7euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x70uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_B (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_B" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x71uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_C (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_C" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x72uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_D (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_D" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x73uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_E (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_E" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x74uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_H (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_H" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x75uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_L (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_L" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x77uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IXd_A (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IXd_A" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x70uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_B (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_B" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x71uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_C (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_C" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x72uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_D (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_D" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x73uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_E (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_E" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x74uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_H (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_H" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x75uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_L (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_L" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x77uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_IYd_A (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.LD_IYd_A" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x86uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.ADD_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x86uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.ADD_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x8euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.ADC_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x8euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.ADC_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x96uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SUB_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x96uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SUB_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x9euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SBC_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x9euy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.SBC_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xa6uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.AND_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.AND_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xa6uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.AND_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.AND_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xaeuy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.XOR_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xaeuy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.XOR_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xb6uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.OR_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.OR_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xb6uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.OR_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.OR_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xbeuy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CP_A_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.CP_A_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xbeuy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CP_A_IYd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.CP_A_IYd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x4auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_HL_BC )
        Format = (fun img pc -> "Z80Vocab.ADC_HL_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x42uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_HL_BC )
        Format = (fun img pc -> "Z80Vocab.SBC_HL_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x5auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_HL_DE )
        Format = (fun img pc -> "Z80Vocab.ADC_HL_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x52uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_HL_DE )
        Format = (fun img pc -> "Z80Vocab.SBC_HL_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x6auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_HL_HL )
        Format = (fun img pc -> "Z80Vocab.ADC_HL_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x62uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_HL_HL )
        Format = (fun img pc -> "Z80Vocab.SBC_HL_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x7auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_HL_SP )
        Format = (fun img pc -> "Z80Vocab.ADC_HL_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x72uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_HL_SP )
        Format = (fun img pc -> "Z80Vocab.SBC_HL_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x09uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IX_BC )
        Format = (fun img pc -> "Z80Vocab.ADD_IX_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x09uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IY_BC )
        Format = (fun img pc -> "Z80Vocab.ADD_IY_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x19uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IX_DE )
        Format = (fun img pc -> "Z80Vocab.ADD_IX_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x19uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IY_DE )
        Format = (fun img pc -> "Z80Vocab.ADD_IY_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x29uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IX_HL )
        Format = (fun img pc -> "Z80Vocab.ADD_IX_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x29uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IY_HL )
        Format = (fun img pc -> "Z80Vocab.ADD_IY_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x39uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IX_SP )
        Format = (fun img pc -> "Z80Vocab.ADD_IX_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0x39uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_IY_SP )
        Format = (fun img pc -> "Z80Vocab.ADD_IY_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x34uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.INC_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.INC_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x35uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.DEC_PTR_IXd (int (sbyte img.[pc + 2])))
        Format = (fun img pc -> "Z80Vocab.DEC_PTR_IXd" + (if 1 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x36uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_IXd_n (int (sbyte img.[pc + 2])) (int img.[pc + 3]))
        Format = (fun img pc -> "Z80Vocab.LD_PTR_IXd_n" + (if 2 = 0 then "" else " " + (sprintf "%d" (sbyte img.[pc + 2]) + " " + sprintf "0x%02X" (int img.[pc + 3]))))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x23uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_IX )
        Format = (fun img pc -> "Z80Vocab.INC_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x2buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_IX )
        Format = (fun img pc -> "Z80Vocab.DEC_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x24uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_IXH )
        Format = (fun img pc -> "Z80Vocab.INC_IXH" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x25uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_IXH )
        Format = (fun img pc -> "Z80Vocab.DEC_IXH" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x2cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_IXL )
        Format = (fun img pc -> "Z80Vocab.INC_IXL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0x2duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_IXL )
        Format = (fun img pc -> "Z80Vocab.DEC_IXL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xe5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.PUSH_IX )
        Format = (fun img pc -> "Z80Vocab.PUSH_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xe1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.POP_IX )
        Format = (fun img pc -> "Z80Vocab.POP_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x00uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_B )
        Format = (fun img pc -> "Z80Vocab.RLC_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x01uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_C )
        Format = (fun img pc -> "Z80Vocab.RLC_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x02uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_D )
        Format = (fun img pc -> "Z80Vocab.RLC_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x03uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_E )
        Format = (fun img pc -> "Z80Vocab.RLC_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x04uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_H )
        Format = (fun img pc -> "Z80Vocab.RLC_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x05uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_L )
        Format = (fun img pc -> "Z80Vocab.RLC_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x07uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_A )
        Format = (fun img pc -> "Z80Vocab.RLC_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x06uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLC_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RLC_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x08uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_B )
        Format = (fun img pc -> "Z80Vocab.RRC_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x09uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_C )
        Format = (fun img pc -> "Z80Vocab.RRC_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x0auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_D )
        Format = (fun img pc -> "Z80Vocab.RRC_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x0buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_E )
        Format = (fun img pc -> "Z80Vocab.RRC_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x0cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_H )
        Format = (fun img pc -> "Z80Vocab.RRC_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x0duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_L )
        Format = (fun img pc -> "Z80Vocab.RRC_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x0fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_A )
        Format = (fun img pc -> "Z80Vocab.RRC_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x0euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRC_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RRC_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x10uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_B )
        Format = (fun img pc -> "Z80Vocab.RL_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x11uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_C )
        Format = (fun img pc -> "Z80Vocab.RL_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x12uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_D )
        Format = (fun img pc -> "Z80Vocab.RL_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x13uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_E )
        Format = (fun img pc -> "Z80Vocab.RL_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x14uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_H )
        Format = (fun img pc -> "Z80Vocab.RL_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x15uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_L )
        Format = (fun img pc -> "Z80Vocab.RL_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x17uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_A )
        Format = (fun img pc -> "Z80Vocab.RL_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x16uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RL_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RL_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x18uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_B )
        Format = (fun img pc -> "Z80Vocab.RR_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x19uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_C )
        Format = (fun img pc -> "Z80Vocab.RR_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x1auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_D )
        Format = (fun img pc -> "Z80Vocab.RR_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x1buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_E )
        Format = (fun img pc -> "Z80Vocab.RR_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x1cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_H )
        Format = (fun img pc -> "Z80Vocab.RR_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x1duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_L )
        Format = (fun img pc -> "Z80Vocab.RR_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x1fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_A )
        Format = (fun img pc -> "Z80Vocab.RR_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x1euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RR_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RR_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x20uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_B )
        Format = (fun img pc -> "Z80Vocab.SLA_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x21uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_C )
        Format = (fun img pc -> "Z80Vocab.SLA_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x22uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_D )
        Format = (fun img pc -> "Z80Vocab.SLA_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x23uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_E )
        Format = (fun img pc -> "Z80Vocab.SLA_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x24uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_H )
        Format = (fun img pc -> "Z80Vocab.SLA_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x25uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_L )
        Format = (fun img pc -> "Z80Vocab.SLA_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x27uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_A )
        Format = (fun img pc -> "Z80Vocab.SLA_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x26uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SLA_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SLA_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x28uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_B )
        Format = (fun img pc -> "Z80Vocab.SRA_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x29uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_C )
        Format = (fun img pc -> "Z80Vocab.SRA_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x2auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_D )
        Format = (fun img pc -> "Z80Vocab.SRA_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x2buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_E )
        Format = (fun img pc -> "Z80Vocab.SRA_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x2cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_H )
        Format = (fun img pc -> "Z80Vocab.SRA_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x2duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_L )
        Format = (fun img pc -> "Z80Vocab.SRA_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x2fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_A )
        Format = (fun img pc -> "Z80Vocab.SRA_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x2euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRA_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SRA_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x38uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_B )
        Format = (fun img pc -> "Z80Vocab.SRL_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x39uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_C )
        Format = (fun img pc -> "Z80Vocab.SRL_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x3auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_D )
        Format = (fun img pc -> "Z80Vocab.SRL_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x3buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_E )
        Format = (fun img pc -> "Z80Vocab.SRL_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x3cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_H )
        Format = (fun img pc -> "Z80Vocab.SRL_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x3duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_L )
        Format = (fun img pc -> "Z80Vocab.SRL_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x3fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_A )
        Format = (fun img pc -> "Z80Vocab.SRL_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x3euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SRL_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SRL_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x40uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_B )
        Format = (fun img pc -> "Z80Vocab.BIT0_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x41uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_C )
        Format = (fun img pc -> "Z80Vocab.BIT0_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x42uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_D )
        Format = (fun img pc -> "Z80Vocab.BIT0_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x43uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_E )
        Format = (fun img pc -> "Z80Vocab.BIT0_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x44uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_H )
        Format = (fun img pc -> "Z80Vocab.BIT0_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x45uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_L )
        Format = (fun img pc -> "Z80Vocab.BIT0_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x47uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_A )
        Format = (fun img pc -> "Z80Vocab.BIT0_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x46uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT0_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT0_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x80uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_B )
        Format = (fun img pc -> "Z80Vocab.RES0_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x81uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_C )
        Format = (fun img pc -> "Z80Vocab.RES0_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x82uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_D )
        Format = (fun img pc -> "Z80Vocab.RES0_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x83uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_E )
        Format = (fun img pc -> "Z80Vocab.RES0_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x84uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_H )
        Format = (fun img pc -> "Z80Vocab.RES0_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x85uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_L )
        Format = (fun img pc -> "Z80Vocab.RES0_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x87uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_A )
        Format = (fun img pc -> "Z80Vocab.RES0_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x86uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES0_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES0_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_B )
        Format = (fun img pc -> "Z80Vocab.SET0_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_C )
        Format = (fun img pc -> "Z80Vocab.SET0_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc2uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_D )
        Format = (fun img pc -> "Z80Vocab.SET0_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_E )
        Format = (fun img pc -> "Z80Vocab.SET0_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc4uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_H )
        Format = (fun img pc -> "Z80Vocab.SET0_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_L )
        Format = (fun img pc -> "Z80Vocab.SET0_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc7uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_A )
        Format = (fun img pc -> "Z80Vocab.SET0_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc6uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET0_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET0_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x48uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_B )
        Format = (fun img pc -> "Z80Vocab.BIT1_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x49uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_C )
        Format = (fun img pc -> "Z80Vocab.BIT1_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x4auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_D )
        Format = (fun img pc -> "Z80Vocab.BIT1_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x4buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_E )
        Format = (fun img pc -> "Z80Vocab.BIT1_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x4cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_H )
        Format = (fun img pc -> "Z80Vocab.BIT1_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x4duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_L )
        Format = (fun img pc -> "Z80Vocab.BIT1_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x4fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_A )
        Format = (fun img pc -> "Z80Vocab.BIT1_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x4euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT1_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT1_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x88uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_B )
        Format = (fun img pc -> "Z80Vocab.RES1_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x89uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_C )
        Format = (fun img pc -> "Z80Vocab.RES1_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x8auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_D )
        Format = (fun img pc -> "Z80Vocab.RES1_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x8buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_E )
        Format = (fun img pc -> "Z80Vocab.RES1_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x8cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_H )
        Format = (fun img pc -> "Z80Vocab.RES1_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x8duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_L )
        Format = (fun img pc -> "Z80Vocab.RES1_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x8fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_A )
        Format = (fun img pc -> "Z80Vocab.RES1_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x8euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES1_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES1_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_B )
        Format = (fun img pc -> "Z80Vocab.SET1_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xc9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_C )
        Format = (fun img pc -> "Z80Vocab.SET1_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xcauy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_D )
        Format = (fun img pc -> "Z80Vocab.SET1_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xcbuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_E )
        Format = (fun img pc -> "Z80Vocab.SET1_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xccuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_H )
        Format = (fun img pc -> "Z80Vocab.SET1_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xcduy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_L )
        Format = (fun img pc -> "Z80Vocab.SET1_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xcfuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_A )
        Format = (fun img pc -> "Z80Vocab.SET1_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xceuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET1_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET1_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x50uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_B )
        Format = (fun img pc -> "Z80Vocab.BIT2_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x51uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_C )
        Format = (fun img pc -> "Z80Vocab.BIT2_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x52uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_D )
        Format = (fun img pc -> "Z80Vocab.BIT2_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x53uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_E )
        Format = (fun img pc -> "Z80Vocab.BIT2_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x54uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_H )
        Format = (fun img pc -> "Z80Vocab.BIT2_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x55uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_L )
        Format = (fun img pc -> "Z80Vocab.BIT2_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x57uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_A )
        Format = (fun img pc -> "Z80Vocab.BIT2_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x56uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT2_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT2_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x90uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_B )
        Format = (fun img pc -> "Z80Vocab.RES2_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x91uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_C )
        Format = (fun img pc -> "Z80Vocab.RES2_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x92uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_D )
        Format = (fun img pc -> "Z80Vocab.RES2_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x93uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_E )
        Format = (fun img pc -> "Z80Vocab.RES2_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x94uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_H )
        Format = (fun img pc -> "Z80Vocab.RES2_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x95uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_L )
        Format = (fun img pc -> "Z80Vocab.RES2_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x97uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_A )
        Format = (fun img pc -> "Z80Vocab.RES2_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x96uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES2_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES2_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_B )
        Format = (fun img pc -> "Z80Vocab.SET2_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_C )
        Format = (fun img pc -> "Z80Vocab.SET2_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd2uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_D )
        Format = (fun img pc -> "Z80Vocab.SET2_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_E )
        Format = (fun img pc -> "Z80Vocab.SET2_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd4uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_H )
        Format = (fun img pc -> "Z80Vocab.SET2_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_L )
        Format = (fun img pc -> "Z80Vocab.SET2_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd7uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_A )
        Format = (fun img pc -> "Z80Vocab.SET2_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd6uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET2_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET2_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x58uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_B )
        Format = (fun img pc -> "Z80Vocab.BIT3_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x59uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_C )
        Format = (fun img pc -> "Z80Vocab.BIT3_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x5auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_D )
        Format = (fun img pc -> "Z80Vocab.BIT3_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x5buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_E )
        Format = (fun img pc -> "Z80Vocab.BIT3_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x5cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_H )
        Format = (fun img pc -> "Z80Vocab.BIT3_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x5duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_L )
        Format = (fun img pc -> "Z80Vocab.BIT3_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x5fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_A )
        Format = (fun img pc -> "Z80Vocab.BIT3_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x5euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT3_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT3_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x98uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_B )
        Format = (fun img pc -> "Z80Vocab.RES3_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x99uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_C )
        Format = (fun img pc -> "Z80Vocab.RES3_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x9auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_D )
        Format = (fun img pc -> "Z80Vocab.RES3_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x9buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_E )
        Format = (fun img pc -> "Z80Vocab.RES3_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x9cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_H )
        Format = (fun img pc -> "Z80Vocab.RES3_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x9duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_L )
        Format = (fun img pc -> "Z80Vocab.RES3_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x9fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_A )
        Format = (fun img pc -> "Z80Vocab.RES3_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x9euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES3_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES3_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_B )
        Format = (fun img pc -> "Z80Vocab.SET3_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xd9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_C )
        Format = (fun img pc -> "Z80Vocab.SET3_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xdauy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_D )
        Format = (fun img pc -> "Z80Vocab.SET3_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xdbuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_E )
        Format = (fun img pc -> "Z80Vocab.SET3_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xdcuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_H )
        Format = (fun img pc -> "Z80Vocab.SET3_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xdduy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_L )
        Format = (fun img pc -> "Z80Vocab.SET3_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xdfuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_A )
        Format = (fun img pc -> "Z80Vocab.SET3_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xdeuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET3_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET3_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x60uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_B )
        Format = (fun img pc -> "Z80Vocab.BIT4_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x61uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_C )
        Format = (fun img pc -> "Z80Vocab.BIT4_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x62uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_D )
        Format = (fun img pc -> "Z80Vocab.BIT4_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x63uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_E )
        Format = (fun img pc -> "Z80Vocab.BIT4_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x64uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_H )
        Format = (fun img pc -> "Z80Vocab.BIT4_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x65uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_L )
        Format = (fun img pc -> "Z80Vocab.BIT4_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x67uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_A )
        Format = (fun img pc -> "Z80Vocab.BIT4_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x66uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT4_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT4_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_B )
        Format = (fun img pc -> "Z80Vocab.RES4_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_C )
        Format = (fun img pc -> "Z80Vocab.RES4_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa2uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_D )
        Format = (fun img pc -> "Z80Vocab.RES4_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_E )
        Format = (fun img pc -> "Z80Vocab.RES4_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa4uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_H )
        Format = (fun img pc -> "Z80Vocab.RES4_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_L )
        Format = (fun img pc -> "Z80Vocab.RES4_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa7uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_A )
        Format = (fun img pc -> "Z80Vocab.RES4_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa6uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES4_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES4_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_B )
        Format = (fun img pc -> "Z80Vocab.SET4_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_C )
        Format = (fun img pc -> "Z80Vocab.SET4_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe2uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_D )
        Format = (fun img pc -> "Z80Vocab.SET4_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_E )
        Format = (fun img pc -> "Z80Vocab.SET4_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe4uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_H )
        Format = (fun img pc -> "Z80Vocab.SET4_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_L )
        Format = (fun img pc -> "Z80Vocab.SET4_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe7uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_A )
        Format = (fun img pc -> "Z80Vocab.SET4_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe6uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET4_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET4_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x68uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_B )
        Format = (fun img pc -> "Z80Vocab.BIT5_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x69uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_C )
        Format = (fun img pc -> "Z80Vocab.BIT5_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x6auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_D )
        Format = (fun img pc -> "Z80Vocab.BIT5_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x6buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_E )
        Format = (fun img pc -> "Z80Vocab.BIT5_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x6cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_H )
        Format = (fun img pc -> "Z80Vocab.BIT5_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x6duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_L )
        Format = (fun img pc -> "Z80Vocab.BIT5_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x6fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_A )
        Format = (fun img pc -> "Z80Vocab.BIT5_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x6euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT5_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT5_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_B )
        Format = (fun img pc -> "Z80Vocab.RES5_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xa9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_C )
        Format = (fun img pc -> "Z80Vocab.RES5_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xaauy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_D )
        Format = (fun img pc -> "Z80Vocab.RES5_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xabuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_E )
        Format = (fun img pc -> "Z80Vocab.RES5_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xacuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_H )
        Format = (fun img pc -> "Z80Vocab.RES5_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xaduy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_L )
        Format = (fun img pc -> "Z80Vocab.RES5_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xafuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_A )
        Format = (fun img pc -> "Z80Vocab.RES5_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xaeuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES5_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES5_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_B )
        Format = (fun img pc -> "Z80Vocab.SET5_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xe9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_C )
        Format = (fun img pc -> "Z80Vocab.SET5_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xeauy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_D )
        Format = (fun img pc -> "Z80Vocab.SET5_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xebuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_E )
        Format = (fun img pc -> "Z80Vocab.SET5_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xecuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_H )
        Format = (fun img pc -> "Z80Vocab.SET5_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xeduy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_L )
        Format = (fun img pc -> "Z80Vocab.SET5_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xefuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_A )
        Format = (fun img pc -> "Z80Vocab.SET5_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xeeuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET5_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET5_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x70uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_B )
        Format = (fun img pc -> "Z80Vocab.BIT6_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x71uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_C )
        Format = (fun img pc -> "Z80Vocab.BIT6_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x72uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_D )
        Format = (fun img pc -> "Z80Vocab.BIT6_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x73uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_E )
        Format = (fun img pc -> "Z80Vocab.BIT6_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x74uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_H )
        Format = (fun img pc -> "Z80Vocab.BIT6_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x75uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_L )
        Format = (fun img pc -> "Z80Vocab.BIT6_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x77uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_A )
        Format = (fun img pc -> "Z80Vocab.BIT6_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x76uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT6_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT6_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_B )
        Format = (fun img pc -> "Z80Vocab.RES6_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_C )
        Format = (fun img pc -> "Z80Vocab.RES6_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb2uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_D )
        Format = (fun img pc -> "Z80Vocab.RES6_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_E )
        Format = (fun img pc -> "Z80Vocab.RES6_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb4uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_H )
        Format = (fun img pc -> "Z80Vocab.RES6_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_L )
        Format = (fun img pc -> "Z80Vocab.RES6_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb7uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_A )
        Format = (fun img pc -> "Z80Vocab.RES6_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb6uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES6_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES6_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_B )
        Format = (fun img pc -> "Z80Vocab.SET6_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_C )
        Format = (fun img pc -> "Z80Vocab.SET6_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf2uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_D )
        Format = (fun img pc -> "Z80Vocab.SET6_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_E )
        Format = (fun img pc -> "Z80Vocab.SET6_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf4uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_H )
        Format = (fun img pc -> "Z80Vocab.SET6_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf5uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_L )
        Format = (fun img pc -> "Z80Vocab.SET6_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf7uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_A )
        Format = (fun img pc -> "Z80Vocab.SET6_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf6uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET6_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET6_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x78uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_B )
        Format = (fun img pc -> "Z80Vocab.BIT7_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x79uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_C )
        Format = (fun img pc -> "Z80Vocab.BIT7_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x7auy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_D )
        Format = (fun img pc -> "Z80Vocab.BIT7_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x7buy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_E )
        Format = (fun img pc -> "Z80Vocab.BIT7_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x7cuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_H )
        Format = (fun img pc -> "Z80Vocab.BIT7_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x7duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_L )
        Format = (fun img pc -> "Z80Vocab.BIT7_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x7fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_A )
        Format = (fun img pc -> "Z80Vocab.BIT7_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0x7euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.BIT7_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.BIT7_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_B )
        Format = (fun img pc -> "Z80Vocab.RES7_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xb9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_C )
        Format = (fun img pc -> "Z80Vocab.RES7_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xbauy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_D )
        Format = (fun img pc -> "Z80Vocab.RES7_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xbbuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_E )
        Format = (fun img pc -> "Z80Vocab.RES7_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xbcuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_H )
        Format = (fun img pc -> "Z80Vocab.RES7_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xbduy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_L )
        Format = (fun img pc -> "Z80Vocab.RES7_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xbfuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_A )
        Format = (fun img pc -> "Z80Vocab.RES7_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xbeuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RES7_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.RES7_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_B )
        Format = (fun img pc -> "Z80Vocab.SET7_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xf9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_C )
        Format = (fun img pc -> "Z80Vocab.SET7_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xfauy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_D )
        Format = (fun img pc -> "Z80Vocab.SET7_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xfbuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_E )
        Format = (fun img pc -> "Z80Vocab.SET7_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xfcuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_H )
        Format = (fun img pc -> "Z80Vocab.SET7_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xfduy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_L )
        Format = (fun img pc -> "Z80Vocab.SET7_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xffuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_A )
        Format = (fun img pc -> "Z80Vocab.SET7_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcbuy; 0xfeuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SET7_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SET7_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xe9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.JP_IX )
        Format = (fun img pc -> "Z80Vocab.JP_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xe9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.JP_IY )
        Format = (fun img pc -> "Z80Vocab.JP_IY" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x4duy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RETI )
        Format = (fun img pc -> "Z80Vocab.RETI" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x45uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RETN )
        Format = (fun img pc -> "Z80Vocab.RETN" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x44uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.NEG )
        Format = (fun img pc -> "Z80Vocab.NEG" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x46uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IM_0 )
        Format = (fun img pc -> "Z80Vocab.IM_0" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x56uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IM_1 )
        Format = (fun img pc -> "Z80Vocab.IM_1" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x5euy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IM_2 )
        Format = (fun img pc -> "Z80Vocab.IM_2" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x67uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRD )
        Format = (fun img pc -> "Z80Vocab.RRD" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x6fuy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLD )
        Format = (fun img pc -> "Z80Vocab.RLD" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xa0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LDI )
        Format = (fun img pc -> "Z80Vocab.LDI" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xa8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LDD )
        Format = (fun img pc -> "Z80Vocab.LDD" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xb0uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LDIR )
        Format = (fun img pc -> "Z80Vocab.LDIR" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xb8uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LDDR )
        Format = (fun img pc -> "Z80Vocab.LDDR" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xa1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CPI )
        Format = (fun img pc -> "Z80Vocab.CPI" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xa9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CPD )
        Format = (fun img pc -> "Z80Vocab.CPD" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xb1uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CPIR )
        Format = (fun img pc -> "Z80Vocab.CPIR" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0xb9uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CPDR )
        Format = (fun img pc -> "Z80Vocab.CPDR" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdduy; 0xe3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EX_SP_IX )
        Format = (fun img pc -> "Z80Vocab.EX_SP_IX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfduy; 0xe3uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EX_SP_IY )
        Format = (fun img pc -> "Z80Vocab.EX_SP_IY" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x40uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_B_C )
        Format = (fun img pc -> "Z80Vocab.IN_B_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x41uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_B )
        Format = (fun img pc -> "Z80Vocab.OUT_C_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x48uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_C_C )
        Format = (fun img pc -> "Z80Vocab.IN_C_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x49uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_C )
        Format = (fun img pc -> "Z80Vocab.OUT_C_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x50uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_D_C )
        Format = (fun img pc -> "Z80Vocab.IN_D_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x51uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_D )
        Format = (fun img pc -> "Z80Vocab.OUT_C_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x58uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_E_C )
        Format = (fun img pc -> "Z80Vocab.IN_E_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x59uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_E )
        Format = (fun img pc -> "Z80Vocab.OUT_C_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x60uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_H_C )
        Format = (fun img pc -> "Z80Vocab.IN_H_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x61uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_H )
        Format = (fun img pc -> "Z80Vocab.OUT_C_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x68uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_L_C )
        Format = (fun img pc -> "Z80Vocab.IN_L_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x69uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_L )
        Format = (fun img pc -> "Z80Vocab.OUT_C_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x78uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_A_C )
        Format = (fun img pc -> "Z80Vocab.IN_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x79uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_A )
        Format = (fun img pc -> "Z80Vocab.OUT_C_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x70uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.IN_PTR_C )
        Format = (fun img pc -> "Z80Vocab.IN_PTR_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeduy; 0x71uy |]
        Mask = [| 0xffuy; 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OUT_C_0 )
        Format = (fun img pc -> "Z80Vocab.OUT_C_0" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x40uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_B )
        Format = (fun img pc -> "Z80Vocab.LD_B_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x41uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_C )
        Format = (fun img pc -> "Z80Vocab.LD_B_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x42uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_D )
        Format = (fun img pc -> "Z80Vocab.LD_B_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x43uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_E )
        Format = (fun img pc -> "Z80Vocab.LD_B_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x44uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_H )
        Format = (fun img pc -> "Z80Vocab.LD_B_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x45uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_L )
        Format = (fun img pc -> "Z80Vocab.LD_B_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x47uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_A )
        Format = (fun img pc -> "Z80Vocab.LD_B_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x48uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_B )
        Format = (fun img pc -> "Z80Vocab.LD_C_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x49uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_C )
        Format = (fun img pc -> "Z80Vocab.LD_C_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x4auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_D )
        Format = (fun img pc -> "Z80Vocab.LD_C_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x4buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_E )
        Format = (fun img pc -> "Z80Vocab.LD_C_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x4cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_H )
        Format = (fun img pc -> "Z80Vocab.LD_C_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x4duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_L )
        Format = (fun img pc -> "Z80Vocab.LD_C_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x4fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_A )
        Format = (fun img pc -> "Z80Vocab.LD_C_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x50uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_B )
        Format = (fun img pc -> "Z80Vocab.LD_D_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x51uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_C )
        Format = (fun img pc -> "Z80Vocab.LD_D_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x52uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_D )
        Format = (fun img pc -> "Z80Vocab.LD_D_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x53uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_E )
        Format = (fun img pc -> "Z80Vocab.LD_D_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x54uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_H )
        Format = (fun img pc -> "Z80Vocab.LD_D_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x55uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_L )
        Format = (fun img pc -> "Z80Vocab.LD_D_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x57uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_A )
        Format = (fun img pc -> "Z80Vocab.LD_D_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x58uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_B )
        Format = (fun img pc -> "Z80Vocab.LD_E_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x59uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_C )
        Format = (fun img pc -> "Z80Vocab.LD_E_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x5auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_D )
        Format = (fun img pc -> "Z80Vocab.LD_E_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x5buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_E )
        Format = (fun img pc -> "Z80Vocab.LD_E_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x5cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_H )
        Format = (fun img pc -> "Z80Vocab.LD_E_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x5duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_L )
        Format = (fun img pc -> "Z80Vocab.LD_E_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x5fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_A )
        Format = (fun img pc -> "Z80Vocab.LD_E_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x60uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_B )
        Format = (fun img pc -> "Z80Vocab.LD_H_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x61uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_C )
        Format = (fun img pc -> "Z80Vocab.LD_H_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x62uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_D )
        Format = (fun img pc -> "Z80Vocab.LD_H_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x63uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_E )
        Format = (fun img pc -> "Z80Vocab.LD_H_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x64uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_H )
        Format = (fun img pc -> "Z80Vocab.LD_H_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x65uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_L )
        Format = (fun img pc -> "Z80Vocab.LD_H_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x67uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_A )
        Format = (fun img pc -> "Z80Vocab.LD_H_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x68uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_B )
        Format = (fun img pc -> "Z80Vocab.LD_L_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x69uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_C )
        Format = (fun img pc -> "Z80Vocab.LD_L_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x6auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_D )
        Format = (fun img pc -> "Z80Vocab.LD_L_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x6buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_E )
        Format = (fun img pc -> "Z80Vocab.LD_L_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x6cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_H )
        Format = (fun img pc -> "Z80Vocab.LD_L_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x6duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_L )
        Format = (fun img pc -> "Z80Vocab.LD_L_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x6fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_A )
        Format = (fun img pc -> "Z80Vocab.LD_L_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x78uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_B )
        Format = (fun img pc -> "Z80Vocab.LD_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x79uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_C )
        Format = (fun img pc -> "Z80Vocab.LD_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x7auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_D )
        Format = (fun img pc -> "Z80Vocab.LD_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x7buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_E )
        Format = (fun img pc -> "Z80Vocab.LD_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x7cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_H )
        Format = (fun img pc -> "Z80Vocab.LD_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x7duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_L )
        Format = (fun img pc -> "Z80Vocab.LD_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x7fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_A )
        Format = (fun img pc -> "Z80Vocab.LD_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x46uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_B_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_B_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x4euy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_C_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_C_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x56uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_D_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_D_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x5euy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_E_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_E_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x66uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_H_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_H_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x6euy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_L_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_L_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x7euy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.LD_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x70uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_B )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x71uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_C )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x72uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_D )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x73uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_E )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x74uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_H )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x75uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_L )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x77uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_PTR_HL_A )
        Format = (fun img pc -> "Z80Vocab.LD_PTR_HL_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x06uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_B (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_B" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x0euy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_C (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_C" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x16uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_D (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_D" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x1euy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_E (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_E" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x26uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_H (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_H" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x2euy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_L (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_L" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x3euy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_A (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.LD_A" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x3auy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_A_ptr ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_A_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x32uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_A ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_A" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x0auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_BC )
        Format = (fun img pc -> "Z80Vocab.LD_A_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x1auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_A_DE )
        Format = (fun img pc -> "Z80Vocab.LD_A_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x02uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_BC_A )
        Format = (fun img pc -> "Z80Vocab.LD_BC_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x12uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_DE_A )
        Format = (fun img pc -> "Z80Vocab.LD_DE_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x01uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_BC ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_BC" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x11uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_DE ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_DE" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x21uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_HL ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_HL" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x31uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_SP ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_SP" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x2auy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_HL_ptr ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_HL_ptr" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0x22uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.LD_ptr_HL ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.LD_ptr_HL" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = None }
      { Prefix = [| 0xf9uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.LD_SP_HL )
        Format = (fun img pc -> "Z80Vocab.LD_SP_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x80uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_B )
        Format = (fun img pc -> "Z80Vocab.ADD_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x81uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_C )
        Format = (fun img pc -> "Z80Vocab.ADD_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x82uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_D )
        Format = (fun img pc -> "Z80Vocab.ADD_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x83uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_E )
        Format = (fun img pc -> "Z80Vocab.ADD_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x84uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_H )
        Format = (fun img pc -> "Z80Vocab.ADD_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x85uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_L )
        Format = (fun img pc -> "Z80Vocab.ADD_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x87uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_A )
        Format = (fun img pc -> "Z80Vocab.ADD_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x86uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.ADD_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc6uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.ADD_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.ADD_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x88uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_B )
        Format = (fun img pc -> "Z80Vocab.ADC_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x89uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_C )
        Format = (fun img pc -> "Z80Vocab.ADC_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x8auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_D )
        Format = (fun img pc -> "Z80Vocab.ADC_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x8buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_E )
        Format = (fun img pc -> "Z80Vocab.ADC_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x8cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_H )
        Format = (fun img pc -> "Z80Vocab.ADC_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x8duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_L )
        Format = (fun img pc -> "Z80Vocab.ADC_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x8fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_A )
        Format = (fun img pc -> "Z80Vocab.ADC_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x8euy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.ADC_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xceuy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.ADC_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.ADC_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x90uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_B )
        Format = (fun img pc -> "Z80Vocab.SUB_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x91uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_C )
        Format = (fun img pc -> "Z80Vocab.SUB_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x92uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_D )
        Format = (fun img pc -> "Z80Vocab.SUB_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x93uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_E )
        Format = (fun img pc -> "Z80Vocab.SUB_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x94uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_H )
        Format = (fun img pc -> "Z80Vocab.SUB_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x95uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_L )
        Format = (fun img pc -> "Z80Vocab.SUB_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x97uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_A )
        Format = (fun img pc -> "Z80Vocab.SUB_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x96uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SUB_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd6uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.SUB_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.SUB_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x98uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_B )
        Format = (fun img pc -> "Z80Vocab.SBC_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x99uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_C )
        Format = (fun img pc -> "Z80Vocab.SBC_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x9auy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_D )
        Format = (fun img pc -> "Z80Vocab.SBC_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x9buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_E )
        Format = (fun img pc -> "Z80Vocab.SBC_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x9cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_H )
        Format = (fun img pc -> "Z80Vocab.SBC_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x9duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_L )
        Format = (fun img pc -> "Z80Vocab.SBC_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x9fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_A )
        Format = (fun img pc -> "Z80Vocab.SBC_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x9euy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.SBC_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdeuy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.SBC_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.SBC_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0xa0uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_B )
        Format = (fun img pc -> "Z80Vocab.AND_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa1uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_C )
        Format = (fun img pc -> "Z80Vocab.AND_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa2uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_D )
        Format = (fun img pc -> "Z80Vocab.AND_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa3uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_E )
        Format = (fun img pc -> "Z80Vocab.AND_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa4uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_H )
        Format = (fun img pc -> "Z80Vocab.AND_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa5uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_L )
        Format = (fun img pc -> "Z80Vocab.AND_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa7uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_A )
        Format = (fun img pc -> "Z80Vocab.AND_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa6uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.AND_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.AND_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe6uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.AND_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.AND_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0xa8uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_B )
        Format = (fun img pc -> "Z80Vocab.XOR_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xa9uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_C )
        Format = (fun img pc -> "Z80Vocab.XOR_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xaauy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_D )
        Format = (fun img pc -> "Z80Vocab.XOR_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xabuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_E )
        Format = (fun img pc -> "Z80Vocab.XOR_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xacuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_H )
        Format = (fun img pc -> "Z80Vocab.XOR_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xaduy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_L )
        Format = (fun img pc -> "Z80Vocab.XOR_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xafuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_A )
        Format = (fun img pc -> "Z80Vocab.XOR_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xaeuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.XOR_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xeeuy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.XOR_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.XOR_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0xb0uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_B )
        Format = (fun img pc -> "Z80Vocab.OR_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb1uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_C )
        Format = (fun img pc -> "Z80Vocab.OR_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb2uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_D )
        Format = (fun img pc -> "Z80Vocab.OR_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb3uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_E )
        Format = (fun img pc -> "Z80Vocab.OR_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb4uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_H )
        Format = (fun img pc -> "Z80Vocab.OR_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb5uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_L )
        Format = (fun img pc -> "Z80Vocab.OR_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb7uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_A )
        Format = (fun img pc -> "Z80Vocab.OR_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb6uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.OR_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.OR_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf6uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.OR_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.OR_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0xb8uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_B )
        Format = (fun img pc -> "Z80Vocab.CP_A_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xb9uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_C )
        Format = (fun img pc -> "Z80Vocab.CP_A_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xbauy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_D )
        Format = (fun img pc -> "Z80Vocab.CP_A_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xbbuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_E )
        Format = (fun img pc -> "Z80Vocab.CP_A_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xbcuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_H )
        Format = (fun img pc -> "Z80Vocab.CP_A_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xbduy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_L )
        Format = (fun img pc -> "Z80Vocab.CP_A_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xbfuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_A )
        Format = (fun img pc -> "Z80Vocab.CP_A_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xbeuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CP_A_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.CP_A_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfeuy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CP_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.CP_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0x09uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_HL_BC )
        Format = (fun img pc -> "Z80Vocab.ADD_HL_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x19uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_HL_DE )
        Format = (fun img pc -> "Z80Vocab.ADD_HL_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x29uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_HL_HL )
        Format = (fun img pc -> "Z80Vocab.ADD_HL_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x39uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.ADD_HL_SP )
        Format = (fun img pc -> "Z80Vocab.ADD_HL_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x04uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_B )
        Format = (fun img pc -> "Z80Vocab.INC_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x05uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_B )
        Format = (fun img pc -> "Z80Vocab.DEC_B" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x0cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_C )
        Format = (fun img pc -> "Z80Vocab.INC_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x0duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_C )
        Format = (fun img pc -> "Z80Vocab.DEC_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x14uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_D )
        Format = (fun img pc -> "Z80Vocab.INC_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x15uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_D )
        Format = (fun img pc -> "Z80Vocab.DEC_D" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x1cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_E )
        Format = (fun img pc -> "Z80Vocab.INC_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x1duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_E )
        Format = (fun img pc -> "Z80Vocab.DEC_E" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x24uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_H )
        Format = (fun img pc -> "Z80Vocab.INC_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x25uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_H )
        Format = (fun img pc -> "Z80Vocab.DEC_H" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x2cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_L )
        Format = (fun img pc -> "Z80Vocab.INC_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x2duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_L )
        Format = (fun img pc -> "Z80Vocab.DEC_L" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x3cuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_A )
        Format = (fun img pc -> "Z80Vocab.INC_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x3duy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_A )
        Format = (fun img pc -> "Z80Vocab.DEC_A" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x34uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.INC_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x35uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_PTR_HL )
        Format = (fun img pc -> "Z80Vocab.DEC_PTR_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x03uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_BC )
        Format = (fun img pc -> "Z80Vocab.INC_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x0buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_BC )
        Format = (fun img pc -> "Z80Vocab.DEC_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x13uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_DE )
        Format = (fun img pc -> "Z80Vocab.INC_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x1buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_DE )
        Format = (fun img pc -> "Z80Vocab.DEC_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x23uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_HL )
        Format = (fun img pc -> "Z80Vocab.INC_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x2buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_HL )
        Format = (fun img pc -> "Z80Vocab.DEC_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x33uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.INC_SP )
        Format = (fun img pc -> "Z80Vocab.INC_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x3buy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DEC_SP )
        Format = (fun img pc -> "Z80Vocab.DEC_SP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc5uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.PUSH_BC )
        Format = (fun img pc -> "Z80Vocab.PUSH_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc1uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.POP_BC )
        Format = (fun img pc -> "Z80Vocab.POP_BC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd5uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.PUSH_DE )
        Format = (fun img pc -> "Z80Vocab.PUSH_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd1uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.POP_DE )
        Format = (fun img pc -> "Z80Vocab.POP_DE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe5uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.PUSH_HL )
        Format = (fun img pc -> "Z80Vocab.PUSH_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe1uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.POP_HL )
        Format = (fun img pc -> "Z80Vocab.POP_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf5uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.PUSH_AF )
        Format = (fun img pc -> "Z80Vocab.PUSH_AF" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf1uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.POP_AF )
        Format = (fun img pc -> "Z80Vocab.POP_AF" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc3uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_nn ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_nn" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_LBL" }
      { Prefix = [| 0xc2uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_NZ ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_NZ" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_NZ_LBL" }
      { Prefix = [| 0xcauy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_Z ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_Z" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_Z_LBL" }
      { Prefix = [| 0xd2uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_NC ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_NC" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_NC_LBL" }
      { Prefix = [| 0xdauy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_C ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_C" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_C_LBL" }
      { Prefix = [| 0xe2uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_PO ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_PO" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_PO_LBL" }
      { Prefix = [| 0xeauy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_PE ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_PE" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_PE_LBL" }
      { Prefix = [| 0xf2uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_P ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_P" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_P_LBL" }
      { Prefix = [| 0xfauy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JP_M ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.JP_M" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "JP_M_LBL" }
      { Prefix = [| 0xe9uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.JP_HL )
        Format = (fun img pc -> "Z80Vocab.JP_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x18uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JR_e (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.JR_e" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = Some "JR_LBL" }
      { Prefix = [| 0x20uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JR_NZ (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.JR_NZ" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = Some "JR_NZ_LBL" }
      { Prefix = [| 0x28uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JR_Z (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.JR_Z" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = Some "JR_Z_LBL" }
      { Prefix = [| 0x30uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JR_NC (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.JR_NC" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = Some "JR_NC_LBL" }
      { Prefix = [| 0x38uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.JR_C (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.JR_C" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = Some "JR_C_LBL" }
      { Prefix = [| 0x10uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.DJNZ_e (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.DJNZ_e" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = Some "DJNZ_LBL" }
      { Prefix = [| 0xcduy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_nn ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_nn" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_LBL" }
      { Prefix = [| 0xc4uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_NZ ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_NZ" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_NZ_LBL" }
      { Prefix = [| 0xccuy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_Z ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_Z" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_Z_LBL" }
      { Prefix = [| 0xd4uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_NC ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_NC" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_NC_LBL" }
      { Prefix = [| 0xdcuy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_C ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_C" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_C_LBL" }
      { Prefix = [| 0xe4uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_PO ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_PO" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_PO_LBL" }
      { Prefix = [| 0xecuy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_PE ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_PE" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_PE_LBL" }
      { Prefix = [| 0xf4uy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_P ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_P" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_P_LBL" }
      { Prefix = [| 0xfcuy; 0x00uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.CALL_M ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))
        Format = (fun img pc -> "Z80Vocab.CALL_M" + (if 1 = 0 then "" else " " + (sprintf "0x%04X" ((int img.[pc + 1]) ||| ((int img.[pc + 2]) <<< 8)))))
        LabelName = Some "CALL_M_LBL" }
      { Prefix = [| 0xc9uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET )
        Format = (fun img pc -> "Z80Vocab.RET" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc0uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_NZ )
        Format = (fun img pc -> "Z80Vocab.RET_NZ" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc8uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_Z )
        Format = (fun img pc -> "Z80Vocab.RET_Z" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd0uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_NC )
        Format = (fun img pc -> "Z80Vocab.RET_NC" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd8uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_C )
        Format = (fun img pc -> "Z80Vocab.RET_C" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe0uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_PO )
        Format = (fun img pc -> "Z80Vocab.RET_PO" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe8uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_PE )
        Format = (fun img pc -> "Z80Vocab.RET_PE" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf0uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_P )
        Format = (fun img pc -> "Z80Vocab.RET_P" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf8uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RET_M )
        Format = (fun img pc -> "Z80Vocab.RET_M" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xc7uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_00 )
        Format = (fun img pc -> "Z80Vocab.RST_00" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xcfuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_08 )
        Format = (fun img pc -> "Z80Vocab.RST_08" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd7uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_10 )
        Format = (fun img pc -> "Z80Vocab.RST_10" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdfuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_18 )
        Format = (fun img pc -> "Z80Vocab.RST_18" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe7uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_20 )
        Format = (fun img pc -> "Z80Vocab.RST_20" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xefuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_28 )
        Format = (fun img pc -> "Z80Vocab.RST_28" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf7uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_30 )
        Format = (fun img pc -> "Z80Vocab.RST_30" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xffuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RST_38 )
        Format = (fun img pc -> "Z80Vocab.RST_38" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x00uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.NOP )
        Format = (fun img pc -> "Z80Vocab.NOP" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x76uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.HALT )
        Format = (fun img pc -> "Z80Vocab.HALT" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xf3uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DI )
        Format = (fun img pc -> "Z80Vocab.DI" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xfbuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EI )
        Format = (fun img pc -> "Z80Vocab.EI" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x37uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.SCF )
        Format = (fun img pc -> "Z80Vocab.SCF" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x3fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CCF )
        Format = (fun img pc -> "Z80Vocab.CCF" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x2fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.CPL )
        Format = (fun img pc -> "Z80Vocab.CPL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x27uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.DAA )
        Format = (fun img pc -> "Z80Vocab.DAA" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xebuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EX_DE_HL )
        Format = (fun img pc -> "Z80Vocab.EX_DE_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x08uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EX_AF_AF )
        Format = (fun img pc -> "Z80Vocab.EX_AF_AF" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xd9uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EXX )
        Format = (fun img pc -> "Z80Vocab.EXX" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x07uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLCA )
        Format = (fun img pc -> "Z80Vocab.RLCA" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x0fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRCA )
        Format = (fun img pc -> "Z80Vocab.RRCA" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x17uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RLA )
        Format = (fun img pc -> "Z80Vocab.RLA" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0x1fuy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.RRA )
        Format = (fun img pc -> "Z80Vocab.RRA" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xe3uy |]
        Mask = [| 0xffuy |]
        Make = (fun img pc -> Z80Vocab.EX_SP_HL )
        Format = (fun img pc -> "Z80Vocab.EX_SP_HL" + (if 0 = 0 then "" else " " + ("")))
        LabelName = None }
      { Prefix = [| 0xdbuy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.IN_A_n (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.IN_A_n" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
      { Prefix = [| 0xd3uy; 0x00uy |]
        Mask = [| 0xffuy; 0x00uy |]
        Make = (fun img pc -> Z80Vocab.OUT_n_A (int img.[pc + 1]))
        Format = (fun img pc -> "Z80Vocab.OUT_n_A" + (if 1 = 0 then "" else " " + (sprintf "0x%02X" (int img.[pc + 1]))))
        LabelName = None }
    ]
