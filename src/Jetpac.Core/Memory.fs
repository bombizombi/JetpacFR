namespace Jetpac.Core

type MemoryWrite =
    { Address: int
      OldValue: byte
      NewValue: byte }

/// Port of specbolt's Memory (peripherals/include/peripherals/Memory.hpp).
/// Writes to ROM pages are ignored; mutable RAM writes can be observed by the
/// execution trace without changing the emulator's behavior.
type Memory(numPages: int) =
    let mutable rom = [| true; false; false; false |]
    let mutable pageTable = [| 0; 1; 2; 3 |]
    let addressSpace = Array.zeroCreate<byte> (numPages * 0x4000)
    let writeHandlers = System.Collections.Generic.List<MemoryWrite -> unit>()
    member this.PageSize = 0x4000
    member this.AddressSpace = addressSpace

    member this.OffsetFor(address: int) =
        pageTable[address >>> 14] * 0x4000 + (address &&& 0x3FFF)

    member this.Read(address: int) : int =
        int addressSpace[this.OffsetFor(address)]

    member this.Read16(address: int) : int =
        ((this.Read((address + 1) &&& 0xFFFF) <<< 8) ||| this.Read address) &&& 0xFFFF

    member this.Write(address: int, value: int) =
        if not (Array.item (address / 0x4000) rom) then
            let offset = this.OffsetFor address
            let oldValue = Array.item offset addressSpace
            let newValue = byte (value &&& 0xFF)
            Array.set addressSpace offset newValue

            if oldValue <> newValue then
                let event =
                    { Address = address &&& 0xFFFF
                      OldValue = oldValue
                      NewValue = newValue }

                for handler in writeHandlers do
                    handler event

    member this.AddWriteHandler(handler: MemoryWrite -> unit) = writeHandlers.Add handler

    member this.Write16(address: int, word: int) =
        this.Write(address, word &&& 0xFF)
        this.Write((address + 1) &&& 0xFFFF, (word >>> 8) &&& 0xFF)

    member this.RawWrite(address: int, value: int) =
        addressSpace[this.OffsetFor(address)] <- byte (value &&& 0xFF)

    member this.RawWrite(page: int, offset: int, value: int) =
        addressSpace[page * 0x4000 + offset] <- byte (value &&& 0xFF)

    member this.RawRead(page: int, offset: int) : int =
        int addressSpace[page * 0x4000 + offset]

    member this.LoadBytes(bytes: byte[], page: int, offset: int, size: int) =
        Array.blit bytes 0 addressSpace (page * 0x4000 + offset) size

    member this.SetPageTable(table: int[]) = pageTable <- table
    member this.SetRomFlags(flags: bool[]) = rom <- flags
