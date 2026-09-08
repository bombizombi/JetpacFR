"""Generate webroot/games/index.json from the desktop games/ folder.

The web launcher (ZX Spectrum Game Changer) fetches this static index -
static hosting cannot list directories. Fields mirror the desktop
launcher's status chips: trace/key script/control file presence, CE
program availability, and `web` = the project is actually deployed to
webroot/games/<id> (has a boot path in the browser).
"""
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
GAMES = ROOT / "games"
WEB_GAMES = ROOT / "src" / "JetpacFR.Web" / "webroot" / "games"


def manifest_of(d: Path) -> dict:
    m = d / "manifest.json"
    if m.exists():
        try:
            return json.loads(m.read_text(encoding="utf-8"))
        except Exception:
            pass
    return {}


def main() -> None:
    entries = []
    for d in sorted(GAMES.iterdir()):
        if not d.is_dir():
            continue
        m = manifest_of(d)
        web_dir = WEB_GAMES / d.name
        entries.append(
            {
                "id": d.name.lower(),
                "name": m.get("name", d.name),
                "default": bool(m.get("default", False)),
                "boot": m.get("boot", "auto"),
                "web": (web_dir / "Game.js").exists(),
                "trace": (d / "timeline.jst").exists(),
                "keys": (d / "replay.json").exists(),
                "control": (d / "control.json").exists(),
                "ce": (web_dir / "Game.js").exists(),
            }
        )
    out = WEB_GAMES / "index.json"
    out.write_text(json.dumps(entries, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {out}")
    for e in entries:
        print(" ", e)


if __name__ == "__main__":
    main()
