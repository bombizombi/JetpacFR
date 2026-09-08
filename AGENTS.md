# AGENTS.md

Working conventions for coding agents in this repository (Jetpac FreeRange:
ZX Spectrum emulator + trace-driven reverse-engineering workbench, F#/.NET).

## F# coding conventions (required)

### Indentation — 4 spaces, enforced by Fantomas

All F# code is indented with **4 spaces** (never 2, never tabs). The whole
codebase is formatted with **Fantomas** (pinned as a local dotnet tool in
`dotnet-tools.json`, settings in `.fantomasconfig.json`: indent 4 spaces,
max line length 120).

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
