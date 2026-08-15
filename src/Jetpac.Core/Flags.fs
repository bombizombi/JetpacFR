namespace Jetpac.Core

/// The Z80 flag register, ported from specbolt (z80/common/include/z80/common/Flags.hpp).
/// Stored as an int; 8-bit operators mask to 0xFF. A struct so ALU operations
/// allocate nothing (critical for emulator throughput).
[<Struct>]
type Flags =
  val Value: int
  new(value: int) = { Value = value &&& 0xFF }

  member this.ToU8() = this.Value

  static member (&&&) (lhs: Flags, rhs: Flags) = Flags(lhs.Value &&& rhs.Value)
  static member (|||) (lhs: Flags, rhs: Flags) = Flags(lhs.Value ||| rhs.Value)
  static member (^^^) (lhs: Flags, rhs: Flags) = Flags(lhs.Value ^^^ rhs.Value)
  static member (~~~) (f: Flags) = Flags(~~~f.Value &&& 0xFF)

  static member Carry() = Flags(0x01)
  static member Subtract() = Flags(0x02)
  static member Parity() = Flags(0x04)
  static member Overflow() = Flags(0x04)
  static member Flag3() = Flags(0x08)
  static member HalfCarry() = Flags(0x10)
  static member Flag5() = Flags(0x20)
  static member Zero() = Flags(0x40)
  static member Sign() = Flags(0x80)

  member this.carry = this.Value &&& 0x01 <> 0
  member this.subtract = this.Value &&& 0x02 <> 0
  member this.parity = this.Value &&& 0x04 <> 0
  member this.overflow = this.Value &&& 0x04 <> 0
  member this.half_carry = this.Value &&& 0x10 <> 0
  member this.zero = this.Value &&& 0x40 <> 0
  member this.sign = this.Value &&& 0x80 <> 0

  override this.ToString() = sprintf "Flags(0x%02x)" this.Value
