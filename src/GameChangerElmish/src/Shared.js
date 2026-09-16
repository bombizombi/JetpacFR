
import { Union } from "../fable_modules/fable-library-js.5.17.2/Types.js";
import { union_type } from "../fable_modules/fable-library-js.5.17.2/Reflection.js";
import { Command_none } from "../.elmish-land/Base/Command.js";
import { empty } from "../fable_modules/fable-library-js.5.17.2/List.js";

export class SharedMsg extends Union {
    constructor() {
        super();
        this.tag = 0;
        this.fields = [];
    }
    cases() {
        return ["NoOp"];
    }
    static NoOp = new SharedMsg();
}

export function SharedMsg_$reflection() {
    return union_type("GameChangerElmish.Shared.SharedMsg", [], SharedMsg, () => [[]]);
}

export function init() {
    return [undefined, Command_none()];
}

export function update(msg, model) {
    return [model, Command_none()];
}

export function subscriptions(_model) {
    return empty();
}

