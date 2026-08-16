
import { Record, Union } from "../fable_modules/fable-library-js.5.13.0/Types.js";
import { option_type, list_type, record_type, bool_type, int32_type, int64_type, union_type, string_type } from "../fable_modules/fable-library-js.5.13.0/Reflection.js";
import { toFail, substring, startsWith, split, printf, toText, join, isNullOrWhiteSpace } from "../fable_modules/fable-library-js.5.13.0/String.js";
import { Exception } from "../fable_modules/fable-library-js.5.13.0/Util.js";
import { Operators_IsNull } from "../fable_modules/fable-library-js.5.13.0/FSharp.Core.js";
import { tail, head, isEmpty, choose, tryPick, sortBy, map, ofArray, singleton, append, empty } from "../fable_modules/fable-library-js.5.13.0/List.js";
import { toInt64_unchecked, compare } from "../fable_modules/fable-library-js.5.13.0/BigInt.js";
import { defaultArg } from "../fable_modules/fable-library-js.5.13.0/Option.js";
import { parse } from "../fable_modules/fable-library-js.5.13.0/Int32.js";
import { parse as parse_1 } from "../fable_modules/fable-library-js.5.13.0/Long.js";
import { parse as parse_2 } from "../fable_modules/fable-library-js.5.13.0/Boolean.js";

/**
 * Stable identifier for a checkpoint in the explorer graph.
 */
export class CheckpointId extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["CheckpointId"];
    }
}

export function CheckpointId_$reflection() {
    return union_type("Jetpac3.Core.CheckpointId", [], CheckpointId, () => [[["Item", string_type]]]);
}

export function CheckpointIdModule_value(_arg) {
    return _arg.fields[0];
}

export function CheckpointIdModule_create(id) {
    if (isNullOrWhiteSpace(id)) {
        throw new Exception("Checkpoint ID cannot be empty (Parameter \'id\')");
    }
    return new CheckpointId(id);
}

/**
 * Human-readable label attached to a branch edge.
 */
export class BranchLabel extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["BranchLabel"];
    }
}

export function BranchLabel_$reflection() {
    return union_type("Jetpac3.Core.BranchLabel", [], BranchLabel, () => [[["Item", string_type]]]);
}

export function BranchLabelModule_value(_arg) {
    return _arg.fields[0];
}

export function BranchLabelModule_create(label) {
    return new BranchLabel(Operators_IsNull(label) ? "" : label);
}

/**
 * Timestamped physical/emulated input transition.
 */
export class InputEvent extends Record {
    constructor(TState, Frame, Row, Bit, Pressed) {
        super();
        this.TState = TState;
        this.Frame = (Frame | 0);
        this.Row = (Row | 0);
        this.Bit = (Bit | 0);
        this.Pressed = Pressed;
    }
}

export function InputEvent_$reflection() {
    return record_type("Jetpac3.Core.InputEvent", [], InputEvent, () => [["TState", int64_type], ["Frame", int32_type], ["Row", int32_type], ["Bit", int32_type], ["Pressed", bool_type]]);
}

/**
 * Replayable input sequence. T-state is authoritative; Frame is a display/index aid.
 */
export class InputTimeline extends Record {
    constructor(Version, RomSha256, TzxSha256, Events) {
        super();
        this.Version = (Version | 0);
        this.RomSha256 = RomSha256;
        this.TzxSha256 = TzxSha256;
        this.Events = Events;
    }
}

export function InputTimeline_$reflection() {
    return record_type("Jetpac3.Core.InputTimeline", [], InputTimeline, () => [["Version", int32_type], ["RomSha256", string_type], ["TzxSha256", string_type], ["Events", list_type(InputEvent_$reflection())]]);
}

export function InputTimelineModule_empty(romSha256, tzxSha256) {
    return new InputTimeline(1, romSha256, tzxSha256, empty());
}

export function InputTimelineModule_append(event, timeline) {
    return new InputTimeline(timeline.Version, timeline.RomSha256, timeline.TzxSha256, append(timeline.Events, singleton(event)));
}

export function InputTimelineCodec_serialize(timeline) {
    return join("\n", append(ofArray([toText(printf("version=%d"))(timeline.Version), "rom=" + timeline.RomSha256, "tzx=" + timeline.TzxSha256]), map((event_1) => toText(printf("event|%d|%d|%d|%d|%b"))(event_1.TState)(event_1.Frame)(event_1.Row)(event_1.Bit)(event_1.Pressed), sortBy((event) => event.TState, timeline.Events, {
        Compare: (x, y) => (compare(x, y) | 0),
    })))) + "\n";
}

export function InputTimelineCodec_parse(text) {
    let option_1;
    if (Operators_IsNull(text)) {
        throw new Exception("Timeline text cannot be null (Parameter \'text\')");
    }
    const lines = ofArray(split(text, ["\n"], undefined, 1));
    const value = (key) => tryPick((line) => {
        const prefix = key + "=";
        if (startsWith(line, prefix, 4)) {
            return substring(line, prefix.length);
        }
        else {
            return undefined;
        }
    }, lines);
    const version = defaultArg((option_1 = value("version"), (option_1 != null) ? parse(option_1, 511, false, 32) : undefined), 0) | 0;
    if (version !== 1) {
        toFail(printf("unsupported input timeline version %d"))(version);
    }
    return new InputTimeline(version, defaultArg(value("rom"), ""), defaultArg(value("tzx"), ""), choose((line_1) => {
        const matchValue = ofArray(line_1.split("|"));
        let matchResult, bit, frame, pressed, row, t;
        if (!isEmpty(matchValue)) {
            if (head(matchValue) === "event") {
                if (!isEmpty(tail(matchValue))) {
                    if (!isEmpty(tail(tail(matchValue)))) {
                        if (!isEmpty(tail(tail(tail(matchValue))))) {
                            if (!isEmpty(tail(tail(tail(tail(matchValue)))))) {
                                if (!isEmpty(tail(tail(tail(tail(tail(matchValue))))))) {
                                    if (isEmpty(tail(tail(tail(tail(tail(tail(matchValue)))))))) {
                                        matchResult = 0;
                                        bit = head(tail(tail(tail(tail(matchValue)))));
                                        frame = head(tail(tail(matchValue)));
                                        pressed = head(tail(tail(tail(tail(tail(matchValue))))));
                                        row = head(tail(tail(tail(matchValue))));
                                        t = head(tail(matchValue));
                                    }
                                    else {
                                        matchResult = 1;
                                    }
                                }
                                else {
                                    matchResult = 1;
                                }
                            }
                            else {
                                matchResult = 1;
                            }
                        }
                        else {
                            matchResult = 1;
                        }
                    }
                    else {
                        matchResult = 1;
                    }
                }
                else {
                    matchResult = 1;
                }
            }
            else {
                matchResult = 1;
            }
        }
        else {
            matchResult = 1;
        }
        switch (matchResult) {
            case 0:
                return new InputEvent(toInt64_unchecked(parse_1(t, 511, false, 64)), parse(frame, 511, false, 32), parse(row, 511, false, 32), parse(bit, 511, false, 32), parse_2(pressed));
            default:
                return undefined;
        }
    }, lines));
}

/**
 * Which implementation owns execution for the current run.
 */
export class ExecutionMode extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["OracleGenerated", "Lifted", "Differential"];
    }
    static OracleGenerated = new ExecutionMode(0, []);
    static Lifted = new ExecutionMode(1, []);
    static Differential = new ExecutionMode(2, []);
}

export function ExecutionMode_$reflection() {
    return union_type("Jetpac3.Core.ExecutionMode", [], ExecutionMode, () => [[], [], []]);
}

/**
 * Boundary used by run/step commands.
 */
export class RunTarget extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["OneFrame", "Frames", "UntilNextDecision", "UntilReturn"];
    }
    static OneFrame = new RunTarget(0, []);
    static UntilNextDecision = new RunTarget(2, []);
    static UntilReturn = new RunTarget(3, []);
}

export function RunTarget_$reflection() {
    return union_type("Jetpac3.Core.RunTarget", [], RunTarget, () => [[], [["Item", int32_type]], [], []]);
}

/**
 * Optional automatic checkpoint policy. Decision-point capture is supplied by
 * the concrete explorer; frame intervals are handled directly by Session.
 */
export class CheckpointCaptureOptions extends Record {
    constructor(FrameInterval, CaptureDecisionPoints) {
        super();
        this.FrameInterval = FrameInterval;
        this.CaptureDecisionPoints = CaptureDecisionPoints;
    }
}

export function CheckpointCaptureOptions_$reflection() {
    return record_type("Jetpac3.Core.CheckpointCaptureOptions", [], CheckpointCaptureOptions, () => [["FrameInterval", option_type(int32_type)], ["CaptureDecisionPoints", bool_type]]);
}

/**
 * First-start policy: the game remains playable while the oracle trace
 * collects new executable addresses until the configured target is reached.
 */
export class StartupExplorationPolicy extends Record {
    constructor(NewExecutableAddresses, MaxFrames) {
        super();
        this.NewExecutableAddresses = (NewExecutableAddresses | 0);
        this.MaxFrames = (MaxFrames | 0);
    }
}

export function StartupExplorationPolicy_$reflection() {
    return record_type("Jetpac3.Core.StartupExplorationPolicy", [], StartupExplorationPolicy, () => [["NewExecutableAddresses", int32_type], ["MaxFrames", int32_type]]);
}

/**
 * Mutable-session presentation state. Machine state remains in the Core machine.
 */
export class ExplorerState extends Record {
    constructor(SelectedCheckpoint, CurrentCheckpoint, Running, CurrentFrame, CurrentTState, PendingInput, ActiveImplementation, RunTarget, BranchLabel) {
        super();
        this.SelectedCheckpoint = SelectedCheckpoint;
        this.CurrentCheckpoint = CurrentCheckpoint;
        this.Running = Running;
        this.CurrentFrame = (CurrentFrame | 0);
        this.CurrentTState = CurrentTState;
        this.PendingInput = PendingInput;
        this.ActiveImplementation = ActiveImplementation;
        this.RunTarget = RunTarget;
        this.BranchLabel = BranchLabel;
    }
}

export function ExplorerState_$reflection() {
    return record_type("Jetpac3.Core.ExplorerState", [], ExplorerState, () => [["SelectedCheckpoint", option_type(CheckpointId_$reflection())], ["CurrentCheckpoint", option_type(CheckpointId_$reflection())], ["Running", bool_type], ["CurrentFrame", int32_type], ["CurrentTState", int64_type], ["PendingInput", list_type(InputEvent_$reflection())], ["ActiveImplementation", ExecutionMode_$reflection()], ["RunTarget", option_type(RunTarget_$reflection())], ["BranchLabel", option_type(BranchLabel_$reflection())]]);
}

/**
 * Graph metadata independent of WPF controls or rendered thumbnails.
 */
export class CheckpointMetadata extends Record {
    constructor(Id, ParentId, Children, Branch, Frame, TState, Pc, InputSummary, Status) {
        super();
        this.Id = Id;
        this.ParentId = ParentId;
        this.Children = Children;
        this.Branch = Branch;
        this.Frame = (Frame | 0);
        this.TState = TState;
        this.Pc = (Pc | 0);
        this.InputSummary = InputSummary;
        this.Status = Status;
    }
}

export function CheckpointMetadata_$reflection() {
    return record_type("Jetpac3.Core.CheckpointMetadata", [], CheckpointMetadata, () => [["Id", CheckpointId_$reflection()], ["ParentId", option_type(CheckpointId_$reflection())], ["Children", list_type(CheckpointId_$reflection())], ["Branch", option_type(BranchLabel_$reflection())], ["Frame", int32_type], ["TState", int64_type], ["Pc", int32_type], ["InputSummary", string_type], ["Status", ExecutionMode_$reflection()]]);
}

