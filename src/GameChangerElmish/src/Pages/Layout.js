
import { Union } from "../../fable_modules/fable-library-js.5.17.2/Types.js";
import { union_type } from "../../fable_modules/fable-library-js.5.17.2/Reflection.js";
import { Command_none } from "../../.elmish-land/Base/Command.js";
import { Layout_from } from "../../.elmish-land/Base/Layout.js";

export class Msg extends Union {
    constructor() {
        super();
        this.tag = 0;
        this.fields = [];
    }
    cases() {
        return ["NoOp"];
    }
    static NoOp = new Msg();
}

export function Msg_$reflection() {
    return union_type("GameChangerElmish.Pages.Layout.Msg", [], Msg, () => [[]]);
}

export function init() {
    return [undefined, Command_none()];
}

export function update(msg, model) {
    return [model, Command_none()];
}

export function routeChanged(model) {
    return [model, Command_none()];
}

export function view(_model, content, _dispatch) {
    return content;
}

export function layout(_props, _route, _shared) {
    return Layout_from(init, (msg, model) => update(msg, undefined), () => routeChanged(undefined), (_model, content, _dispatch) => view(undefined, content, _dispatch));
}

