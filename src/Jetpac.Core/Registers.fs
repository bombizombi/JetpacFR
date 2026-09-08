namespace Jetpac.Core

/// 8-bit register selector.
type R8 =
    | A
    | F
    | B
    | C
    | D
    | E
    | H
    | L
    | A_
    | F_
    | B_
    | C_
    | D_
    | E_
    | H_
    | L_
    | SPH
    | SPL
    | IXH
    | IXL
    | IYH
    | IYL

/// 16-bit register pair selector.
type R16 =
    | AF
    | BC
    | DE
    | HL
    | AF_
    | BC_
    | DE_
    | HL_
    | SP
    | IX
    | IY

/// A high/low byte pair. All values are `int`; 8-bit values are masked to
/// 0xFF and 16-bit values to 0xFFFF at the point of use (Fable numbers do not
/// wrap). Initialised to 0xFFFF like the C++ constructor (low_ = 0xff).
type RegPair() =
    let mutable low = 0xFF
    let mutable high = 0xFF

    member this.High() = high
    member this.SetHigh(v: int) = high <- v &&& 0xFF
    member this.Low() = low
    member this.SetLow(v: int) = low <- v &&& 0xFF
    member this.HighLow() = ((high <<< 8) ||| low) &&& 0xFFFF

    member this.SetHighLow(v: int) =
        high <- (v >>> 8) &&& 0xFF
        low <- v &&& 0xFF

/// Port of specbolt's RegisterFile (z80/common/include/z80/common/RegisterFile.hpp).
type RegisterFile() =
    let regs = Array.init 11 (fun _ -> RegPair())
    let mutable wz_ = 0xFFFF
    let mutable pc_ = 0
    let mutable r_ = 0
    let mutable i_ = 0

    static member private PairIndex(r8: R8) =
        match r8 with
        | A
        | F -> 0
        | B
        | C -> 1
        | D
        | E -> 2
        | H
        | L -> 3
        | A_
        | F_ -> 4
        | B_
        | C_ -> 5
        | D_
        | E_ -> 6
        | H_
        | L_ -> 7
        | SPH
        | SPL -> 8
        | IXH
        | IXL -> 9
        | IYH
        | IYL -> 10

    static member private IsHigh(r8: R8) =
        match r8 with
        | A
        | B
        | D
        | H
        | A_
        | B_
        | D_
        | H_
        | SPH
        | IXH
        | IYH -> true
        | _ -> false

    static member private PairIndex16(r16: R16) =
        match r16 with
        | AF -> 0
        | BC -> 1
        | DE -> 2
        | HL -> 3
        | AF_ -> 4
        | BC_ -> 5
        | DE_ -> 6
        | HL_ -> 7
        | SP -> 8
        | IX -> 9
        | IY -> 10

    member private this.RegFor(r8: R8) = regs[RegisterFile.PairIndex r8]
    member private this.RegFor(r16: R16) = regs[RegisterFile.PairIndex16 r16]

    member this.Get(r8: R8) : int =
        let pair = this.RegFor r8
        if RegisterFile.IsHigh r8 then pair.High() else pair.Low()

    member this.Set(r8: R8, value: int) =
        let pair = this.RegFor r8

        if RegisterFile.IsHigh r8 then
            pair.SetHigh value
        else
            pair.SetLow value

    member this.Get(r16: R16) : int = this.RegFor(r16).HighLow()
    member this.Set(r16: R16, value: int) = this.RegFor(r16).SetHighLow value

    member this.Ix() = this.Get R16.IX
    member this.Iy() = this.Get R16.IY
    member this.Sp() = this.Get R16.SP
    member this.SetSp(v: int) = this.Set(R16.SP, v)
    member this.Pc() = pc_
    member this.SetPc(v: int) = pc_ <- v &&& 0xFFFF
    member this.R() = r_
    member this.SetR(v: int) = r_ <- v &&& 0xFF
    member this.I() = i_
    member this.SetI(v: int) = i_ <- v &&& 0xFF
    member this.Wz() = wz_
    member this.SetWz(v: int) = wz_ <- v &&& 0xFFFF

    member this.Ex(lhs: R16, rhs: R16) =
        let l = this.RegFor lhs
        let r = this.RegFor rhs
        let tmp = l.HighLow()
        l.SetHighLow(r.HighLow())
        r.SetHighLow tmp

    member this.Exx() =
        this.Ex(R16.BC, R16.BC_)
        this.Ex(R16.DE, R16.DE_)
        this.Ex(R16.HL, R16.HL_)
