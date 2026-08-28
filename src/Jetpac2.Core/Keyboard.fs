namespace Jetpac2.Core

/// Port of JetpacFSharp (Jetpac.Core) Keyboard — specbolt's keyboard matrix
/// (peripherals/Keyboard.cpp). Only the matrix and its port decode; the
/// keycode table belongs to the shells.
type Keyboard() =
  // Fable does not support 2D arrays; use a jagged array.
  let keys = Array.init 8 (fun _ -> Array.zeroCreate<bool> 5)

  member this.SetKey(row: int, bit: int, pressed: bool) = keys.[row].[bit] <- pressed
  member this.GetKey(row: int, bit: int) : bool = keys.[row].[bit]
  /// Currently pressed cells, e.g. for a controlled teardown (replay end).
  member this.PressedCells() : (int * int) list =
    [ for row in 0 .. 7 do
        for bit in 0 .. 4 do
          if keys.[row].[bit] then row, bit ]

  /// Keyboard responds to any even address.
  member this.In(address: int) : int option =
    if address &&& 1 = 1 then
      None
    else
      let mutable value = 0xFF
      let res = (address >>> 8) &&& 0xFF
      // The low bit in the top address picks the row; with multiple low bits
      // the results AND together.
      for row in 0 .. 7 do
        if res &&& (1 <<< row) = 0 then
          for bit in 0 .. 4 do
            if keys.[row].[bit] then
              value <- value &&& ~~~(1 <<< bit)
      Some value

  /// The 8 half-row bytes (pressed bits inverted: 0 = pressed), the IN form.
  member this.ToBytes() : byte[] =
    let k = Array.zeroCreate<byte> 8
    for row in 0 .. 7 do
      let mutable bits = 0
      for bit in 0 .. 4 do
        if keys.[row].[bit] then bits <- bits ||| (1 <<< bit)
      k.[row] <- byte (0xFF &&& ~~~bits)
    k

  /// Apply half-row bytes captured by `ToBytes`.
  member this.Load(bytes: byte[]) =
    for row in 0 .. 7 do
      for bit in 0 .. 4 do
        this.SetKey(row, bit, ((int bytes.[row] >>> bit) &&& 1 = 0))
