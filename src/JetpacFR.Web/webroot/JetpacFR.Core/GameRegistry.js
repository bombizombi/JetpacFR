
import { Record } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { record_type, list_type, int32_type, array_type, uint8_type, string_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { Z80_assemble, Z80Op_$reflection } from "../Jetpac2.Core/Z80Asm.js";
import { ofSeq, tryFind, reverse, cons, empty } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { min } from "../fable_modules/fable-library-js.5.13.0/Double.js";
import { item } from "../fable_modules/fable-library-js.5.13.0/Array.js";

/**
 * The per-game CE registry entry: one compiled game version, exposed by
 * each games/<id> project as a module value so shells can look it up by
 * manifest GameId without hardcoding names. Shaped for Fable: records +
 * closures only.
 */
export class GameImage extends Record {
    constructor(GameId, Name, Memory, BaseAddress, Program, EntryState) {
        super();
        this.GameId = GameId;
        this.Name = Name;
        this.Memory = Memory;
        this.BaseAddress = (BaseAddress | 0);
        this.Program = Program;
        this.EntryState = EntryState;
    }
}

export function GameImage_$reflection() {
    return record_type("JetpacFR.Core.GameImage", [], GameImage, () => [["GameId", string_type], ["Name", string_type], ["Memory", array_type(uint8_type)], ["BaseAddress", int32_type], ["Program", list_type(Z80Op_$reflection())], ["EntryState", string_type]]);
}

let GameRegistry_entries = empty();

/**
 * Register a game image (called once per game module at shell startup).
 */
export function GameRegistry_register(g) {
    GameRegistry_entries = cons(g, GameRegistry_entries);
}

/**
 * All registered games (registration order reversed - stable enough:
 * registration happens in one deterministic module-init sequence).
 */
export function GameRegistry_all() {
    return reverse(GameRegistry_entries);
}

/**
 * Lookup by manifest GameId.
 */
export function GameRegistry_tryFind(gameId) {
    return tryFind((g) => (g.GameId === gameId), GameRegistry_all());
}

/**
 * Byte parity of the registered program vs its own memory image,
 * compared over [BaseAddress, BaseAddress + assembled.Length).
 */
export function GameRegistry_parity(g) {
    const assembled = Z80_assemble(g.Program);
    const n = min(assembled.length, 65536 - g.BaseAddress) | 0;
    let matching = 0;
    const mismatches = [];
    for (let i = 0; i <= (n - 1); i++) {
        if (item(i, assembled) === item(g.BaseAddress + i, g.Memory)) {
            matching = ((matching + 1) | 0);
        }
        else if (mismatches.length < 8) {
            void (mismatches.push(i));
        }
    }
    if ((assembled.length > n) && (mismatches.length < 8)) {
        void (mismatches.push(-1));
    }
    return [matching, n, ofSeq(mismatches)];
}

