# AGENTS.md

Working conventions for coding agents in this repository (Jetpac FreeRange:
ZX Spectrum emulator + trace-driven reverse-engineering workbench, F#/.NET).

## F# coding conventions (required)

### Indentation — 4 spaces, enforced by Fantomas
### Indentation — 4 spaces

All F# code is indented with **4 spaces** (never 2, never tabs). The whole
codebase is formatted with **Fantomas** (pinned as a local dotnet tool in
`dotnet-tools.json`, settings in `.fantomasconfig.json`: indent 4 spaces,
max line length 120).
All F# code is indented with **4 spaces** (never 2, never tabs), max line
length 120. The codebase was formatted with **Fantomas** 7.0.6 (settings in
`.fantomasconfig.json`, pinned as a local dotnet tool in `dotnet-tools.json`).

After editing or creating `.fs` files, format them:

```
dotnet fantomas path/to/file.fs
```

or all files at once:

```
dotnet fantomas src tests games tools -r
```

(`-r` recurses; Fantomas skips `bin`/`obj` content it is given explicitly.)
A whole-codebase reformat from 2-space to 4-space was done with Fantomas
7.0.6 - do not hand-reformat or reintroduce 2-space indentation.
Do NOT run Fantomas as a routine step - not after edits, not on builds.
Write new code directly in the existing style. (A whole-codebase reformat
from 2-space to 4-space was done with Fantomas - do not reintroduce
2-space indentation.)

### Array/list/string indexing — modern syntax only

Access indexable values with `name[index]`. The old `name.[index]` syntax is
banned in all F# code (`.fs`, `.fsx`, `.fsi`) in this repo:

```fsharp
// REQUIRED
let b = memory[addr]
memory[addr] <- value
let slice = data[lo..hi]

// FORBIDDEN (legacy)
let b = memory.[addr]
memory.[addr] <- value
```

This applies to arrays, lists, strings, `Span`/`ReadOnlySpan`, and
multidimensional indexing (`m[i, j]`, never `m.[i, j]`).

- The same rule is already injected into LLM prompts that generate F#
  (see `src/JetpacFR.Core/Prompt.fs`), so machine-generated code follows it too.
- `tools/convert_index_syntax.py` is the one-shot migration that removed the
  legacy syntax from this codebase; it skips string literals. Use it as a
  reference if new legacy occurrences ever appear in bulk.

### Web shell build (src/JetpacFR.Web)

The web shell is Fable-compiled F# over raw DOM; generated `.js` lands in
`src/JetpacFR.Web/webroot` (checked in) and Vite bundles it.

- Rebuild after editing web F# or `webroot/index.html`:
  1. `fable src/JetpacFR.Web --outDir src/JetpacFR.Web/webroot`
     (Fable = dotnet global tool, `dotnet tool install -g fable`)
  2. `cd src/JetpacFR.Web/webroot && npx vite build` (dev server: `npx vite`)
- `emitJsExpr` only accepts **literal** JS strings; pass dynamic values as
  `$0`-style arguments, never via sprintf-built JS text.
- The browser launcher lists projects from the static
  `webroot/games/index.json` - regenerate with `python tools/gen_web_index.py`
  after adding projects or traces. A project can only open in the browser
  when its assets are deployed under `webroot/games/<id>` (`web: true`).

### Mobile game pages (src/GameChangerElmish)

`src/GameChangerElmish` is an **Elmish Land 2.0** app (Fable 5 + Feliz 3 +
Vite 8, hash routing) hosting mobile-first touch play pages; games are picked
with `?game=<id>` and register themselves in `src/Host.fs` (`Games`). Its
emulator core is the same Fable-clean subset JetpacFR.Web compiles, plus the
game image module `games/<id>/WebImage.fs`.

- Run dev server: `cd src/GameChangerElmish && elmish-land server`
  (http://localhost:5173). Production build: `elmish-land build` -> `dist/`.
- After editing its F#, recompile the JS the dev server serves:
  `dotnet fable GameChangerElmish.fsproj --outDir .` then reload the page
  (fable is pinned in the project-local `dotnet-tools.json`; `dotnet fable`
  resolves it there). Fable writes outside-project outputs **next to their
  sources** (`src/Jetpac2.Core/*.fs.js`, `games/uridium/WebImage.fs.js`) -
  gitignored, do not commit or hand-edit them. `.elmish-land/` is generated
  (`elmish-land restore`); never edit it.
- The `global.json` there pins the 11.0.100 RC SDK (`allowPrerelease`) - the
  same SDK the rest of the repo's net11.0 projects build with.
- Null-CE game versions (raw bytes in the CE file, the default version):
  `dotnet run --project tests/JetpacFR.Core.Tests -- --null-ce games/<id>
  [--web-image]`. Writes `versions/vNNN_nullce.fs` (parity-gated), bumps
  `activeVersion`, materializes `CeProgram.fs`; `--web-image` also generates
  `WebImage.fs` (needs `memory.bin` + `state.txt` in the game dir). Add the
  generated `WebImage.fs` to the GameChangerElmish fsproj and one `GameDef`
  entry in `Host.fs` (`Games.all`) to ship a new game.
- Warm-start captures come from the real tape, not hand-built states:
  `dotnet run --project tests/JetpacFR.Core.Tests -- --capture-tape
  games/<id>/<game>.tzx games/<id>` boots ROM+TZX through
  `Jetpac3.Core.Boot.bootToEntry`, settles into gameplay, and rewrites
  `memory.bin` + `state.txt` (previous files kept as `.bak`). A capture with
  `pc` inside loader code shows a frozen loading screen - always verify the
  captured state actually animates (beeper events + changing screen) before
  shipping it.
- Touch controls: pointer events with `setPointerCapture` (fire and keypad
  track separate pointer ids); key mapping lives in `Host.fs` `KeyMap`
  (default: cursor-joystick CapsShift+5/6/7/8, fire 0).

## Notes

- Desktop GUI layout (src/JetpacFR.Desktop): the launcher view lives in
  `LauncherView.fs` (with the `GameImages` registry and `Projects`
  discovery modules), the cheat-engine window in `CheatEngineWindow.fs`,
  the bare game view (screen + reverse playback, a phase of the main
  window - not a second window) in `GameOnlyView.fs`, and the emulator UI
  plus all shared session state in `App.fs` (MainWindow). The emulator
  construction is deeply coupled to MainWindow's state - keep it there.
- Strings that are *generated-code templates* (e.g. in `Prompt.fs`) contain
  code-text on purpose - do not "fix" syntax inside them blindly.
- `games/<id>/` directories are projects: one `manifest.json` each, folder
  name (lowercased) is the GameId.
