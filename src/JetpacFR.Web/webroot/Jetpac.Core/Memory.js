
import { Record } from "../fable_modules/fable-library-js.5.17.0/Types.js";
import { class_type, record_type, uint8_type, int32_type } from "../fable_modules/fable-library-js.5.17.0/Reflection.js";
import { copyTo, setItem, item } from "../fable_modules/fable-library-js.5.17.0/Array.js";
import { disposeSafe, getEnumerator } from "../fable_modules/fable-library-js.5.17.0/Util.js";

export class MemoryWrite extends Record {
    constructor(Address, OldValue, NewValue) {
        super();
        this.Address = (Address | 0);
        this.OldValue = OldValue;
        this.NewValue = NewValue;
    }
}

export function MemoryWrite_$reflection() {
    return record_type("Jetpac.Core.MemoryWrite", [], MemoryWrite, () => [["Address", int32_type], ["OldValue", uint8_type], ["NewValue", uint8_type]]);
}

/**
 * Port of specbolt's Memory (peripherals/include/peripherals/Memory.hpp).
 * Writes to ROM pages are ignored; mutable RAM writes can be observed by the
 * execution trace without changing the emulator's behavior.
 */
export class Memory {
    constructor(numPages) {
        this.rom = [true, false, false, false];
        this.pageTable = (new Int32Array([0, 1, 2, 3]));
        this.addressSpace = (new Uint8Array(numPages * 16384));
        this.writeHandlers = [];
    }
}

export function Memory_$reflection() {
    return class_type("Jetpac.Core.Memory", undefined, Memory);
}

export function Memory_$ctor_Z524259A4(numPages) {
    return new Memory(numPages);
}

export function Memory__get_PageSize(this$) {
    return 16384;
}

export function Memory__get_AddressSpace(this$) {
    return this$.addressSpace;
}

export function Memory__OffsetFor_Z524259A4(this$, address) {
    return ((item(address >> 14, this$.pageTable) * 16384) + (address & 16383)) | 0;
}

export function Memory__Read_Z524259A4(this$, address) {
    return ~~item(Memory__OffsetFor_Z524259A4(this$, address), this$.addressSpace) | 0;
}

export function Memory__Read16_Z524259A4(this$, address) {
    return (((Memory__Read_Z524259A4(this$, (address + 1) & 65535) << 8) | Memory__Read_Z524259A4(this$, address)) & 65535) | 0;
}

export function Memory__Write_Z37302880(this$, address, value) {
    if (!item(~~(address / 16384), this$.rom)) {
        const offset = Memory__OffsetFor_Z524259A4(this$, address) | 0;
        const oldValue = item(offset, this$.addressSpace);
        const newValue = (value & 255) & 0xFF;
        setItem(this$.addressSpace, offset, newValue);
        if (oldValue !== newValue) {
            const event = new MemoryWrite(address & 65535, oldValue, newValue);
            let enumerator = getEnumerator(this$.writeHandlers);
            try {
                while (enumerator["System.Collections.IEnumerator.MoveNext"]()) {
                    enumerator["System.Collections.Generic.IEnumerator`1.get_Current"]()(event);
                }
            }
            finally {
                disposeSafe(enumerator);
            }
        }
    }
}

export function Memory__AddWriteHandler_48E2F4A1(this$, handler) {
    void (this$.writeHandlers.push(handler));
}

export function Memory__Write16_Z37302880(this$, address, word) {
    Memory__Write_Z37302880(this$, address, word & 255);
    Memory__Write_Z37302880(this$, (address + 1) & 65535, (word >> 8) & 255);
}

export function Memory__RawWrite_Z37302880(this$, address, value) {
    this$.addressSpace[Memory__OffsetFor_Z524259A4(this$, address)] = ((value & 255) & 0xFF);
}

export function Memory__RawWrite_4F7761DC(this$, page, offset, value) {
    this$.addressSpace[(page * 16384) + offset] = ((value & 255) & 0xFF);
}

export function Memory__RawRead_Z37302880(this$, page, offset) {
    return ~~item((page * 16384) + offset, this$.addressSpace) | 0;
}

export function Memory__LoadBytes_6BA4C033(this$, bytes, page, offset, size) {
    copyTo(bytes, 0, this$.addressSpace, (page * 16384) + offset, size);
}

export function Memory__SetPageTable_4F10E657(this$, table) {
    this$.pageTable = table;
}

export function Memory__SetRomFlags_5907F3E1(this$, flags) {
    this$.rom = flags;
}

