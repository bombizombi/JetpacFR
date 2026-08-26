# Fast game-iteration loop (plan_new_game_project).
#
#   tools/watchgame.ps1 [-GameDir games/<id>] [-Watch]
#
# 1. materializes the active CE version into <GameDir>/CeProgram.fs
#    (via --materialize),
# 2. recompiles the Fable web build into src/JetpacFR.Web/webroot,
# 3. with -Watch: loops on any change under the game dir or the web
#    sources; otherwise one pass and exit.
#
# The browser (vite dev server) picks up changed static JS on refresh.

param(
  [string]$GameDir = "",
  [switch]$Watch
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$testsProj = "tests/JetpacFR.Core.Tests/JetpacFR.Core.Tests.fsproj"
$webProj = "src/JetpacFR.Web/JetpacFR.Web.fsproj"

if ($GameDir -eq "") {
  $dirs = Get-ChildItem games -Directory | Where-Object { Test-Path (Join-Path $_.FullName "control.json") }
  if ($dirs.Count -eq 0) { Write-Error "no games/*/control.json found - run --gen-game first" }
  $GameDir = $dirs[0].FullName
  Write-Host "no -GameDir given, using $GameDir"
}
$GameDir = (Resolve-Path $GameDir).Path

function Invoke-Materialize {
  Write-Host "[watchgame] materializing $(Split-Path -Leaf $GameDir)" -ForegroundColor Cyan
  dotnet run --project $testsProj --no-build -- --materialize "$GameDir"
  if ($LASTEXITCODE -ne 0) { throw "materialize failed" }
}

function Invoke-Fable {
  Write-Host "[watchgame] fable -> webroot" -ForegroundColor Cyan
  dotnet fable "$webProj" --outDir src/JetpacFR.Web/webroot --run fast serve src/JetpacFR.Web/webroot -p 8123
}

# Build the generator host once so --no-build calls stay cheap.
dotnet build $testsProj -v q --nologo

Invoke-Materialize

if (-not $Watch) {
  # One pass: materialize + fable compile (no dev server).
  dotnet fable "$webProj" --outDir src/JetpacFR.Web/webroot
  exit $LASTEXITCODE
}

# Watch mode: poll for changes (FileSystemWatcher is flaky across editors;
# a 500ms hash poll is deterministic and portable).
$fable = Start-Process dotnet -ArgumentList "fable `"$webProj`" --outDir src/JetpacFR.Web/webroot --watch" -NoNewWindow -PassThru
try {
  function Snapshot($paths) {
    $paths | Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue |
      Sort-Object FullName |
      ForEach-Object { "{0}:{1}:{2}" -f $_.FullName, $_.Length, $_.LastWriteTimeUtc.Ticks }
  }
  $watchPaths = @("$GameDir/versions", "$GameDir/control.json", "$GameDir/CeProgram.fs")
  $last = Snapshot $watchPaths
  while ($true) {
    Start-Sleep -Milliseconds 500
    $now = Snapshot $watchPaths
    if ((Compare-Object $last $now) -ne $null) {
      $last = $now
      try { Invoke-Materialize } catch { Write-Host $_.Message -ForegroundColor Red }
      # fable --watch picks up CeProgram.fs changes itself.
    }
  }
}
finally {
  if (-not $fable.HasExited) { $fable.Kill() }
}
