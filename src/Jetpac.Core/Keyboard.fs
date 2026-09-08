namespace Jetpac.Core

/// Port of specbolt's keyboard matrix (peripherals/Keyboard.cpp). Only the
/// matrix and its port decode; the SDL keycode table belongs to the shells.
type Keyboard() =
    // Fable does not support 2D arrays; use a jagged array.
    let keys = Array.init 8 (fun _ -> Array.zeroCreate<bool> 5)

    member this.SetKey(row: int, bit: int, pressed: bool) = keys[row][bit] <- pressed

    /// Keyboard responds to any even address.
    member this.In(address: int) : int option =
        if address &&& 1 = 1 then
            None
        else
            let mutable value = 0xFF
            let res = (address >>> 8) &&& 0xFF
            // The low bit in the top address picks the row; with multiple low bits
            // the results AND together.
            for row in 0..7 do
                if res &&& (1 <<< row) = 0 then
                    for bit in 0..4 do
                        if keys[row][bit] then
                            value <- value &&& ~~~(1 <<< bit)

            Some value
