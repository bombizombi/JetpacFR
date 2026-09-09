"""Make flame-click failures visible: every fallback path reports its reason
in the status bar instead of silently doing nothing (or clamping unnoticed).
"""
import io

APP = "src/JetpacFR.Desktop/App.fs"
with io.open(APP, encoding="utf-8-sig") as f:
    text = f.read()

# 1) frame-level fallback helper, defined before the handler
anchor1 = "        flameCtrl.SeekRequested.Add(fun (tick, frame) ->\n            match session, flameCtrl.Window, flameTrace with\n"
helper = """        // Frame-level fallback for flame clicks: clamps the frame into the
        // recording and parks there. Every caller passes the reason the
        // instruction-level seek could not run, and it lands in the status
        // bar - flame clicks are never silent.
        let frameSeek (reason: string) (frame: int) =
            match session with
            | None ->
                statusText.Text <-
                    sprintf
                        "flame click: %s - but there is no live session (CE engine active), nothing to seek"
                        reason
            | Some s ->
                let target = max 0 (min frame s.TimelineExtent)

                let clampNote =
                    if frame > s.TimelineExtent then " - beyond the recording, clamped" else ""

                if target = s.Frame && not running && not replaying then
                    statusText.Text <-
                        sprintf "flame click: %s - already parked at frame %d%s" reason target clampNote
                else
                    statusText.Text <- sprintf "flame click: %s - frame seek to %d%s" reason target clampNote
                    scrubToFrame (int64 frame)

        flameCtrl.SeekRequested.Add(fun (tick, frame) ->
            match session, flameCtrl.Window, flameTrace with
"""
assert anchor1 in text, "anchor1 missing"
text = text.replace(anchor1, helper, 1)

# 2) instruction index out of range -> reported fallback
old2 = """                if idx < 0 || idx >= ft.Entries.Length then
                    scrubToFrame (int64 frame)
                else"""
new2 = """                if idx < 0 || idx >= ft.Entries.Length then
                    frameSeek "the clicked tick has no instruction in the flame trace" frame
                else"""
assert old2 in text, "anchor2 missing"
text = text.replace(old2, new2, 1)

# 3) steps/extent guard -> reported fallback
old3 = """                    if steps <= 0 || frameNo < 1 || frameNo - 1 > s.TimelineExtent then
                        scrubToFrame (int64 frame)
                    else"""
new3 = """                    if steps <= 0 || frameNo < 1 || frameNo - 1 > s.TimelineExtent then
                        let reason =
                            if frameNo - 1 > s.TimelineExtent then
                                "the clicked frame is beyond the recording"
                            else
                                "the tick precedes any instruction of its frame"

                        frameSeek reason frame
                    else"""
assert old3 in text, "anchor3 missing"
text = text.replace(old3, new3, 1)

# 4) last two arms: no session/trace/window reasons + no-mapping message
old4 = """            | _ when frame >= 0 -> scrubToFrame (int64 frame)
            | _ -> statusText.Text <- "flame: window has no frame mapping")"""
new4 = """            | _ when frame >= 0 ->
                let reason =
                    match session, flameTrace, flameCtrl.Window with
                    | None, _, _ -> "no live session (CE engine active)"
                    | _, None, _ -> "the game is running - instruction data is dropped while playing"
                    | _, _, None -> "no flame window loaded"
                    | _ -> "unmapped click"

                frameSeek reason frame
            | _ -> statusText.Text <- "flame click: this window has no frame mapping - click ignored")"""
assert old4 in text, "anchor4 missing"
text = text.replace(old4, new4, 1)

with io.open(APP, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("flame click failures are now visible")
