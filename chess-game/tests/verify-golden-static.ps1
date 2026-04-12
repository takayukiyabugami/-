$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$goldenPath = Join-Path (Split-Path -Parent $root) "chess-spec\golden-moves.json"
$domainPath = Join-Path $root "domain.js"

if (-not (Test-Path $goldenPath)) {
  throw "golden file not found: $goldenPath"
}
if (-not (Test-Path $domainPath)) {
  throw "domain.js not found: $domainPath"
}

$golden = Get-Content -Raw -Path $goldenPath | ConvertFrom-Json
if (-not $golden.cases -or $golden.cases.Count -lt 1) {
  throw "golden cases are empty"
}

$movePattern = '^[a-h][1-8][a-h][1-8][qrbn]?$'
foreach ($case in $golden.cases) {
  if (-not $case.id) { throw "case id missing" }
  if (-not $case.moves) { throw "moves missing in case: $($case.id)" }
  foreach ($m in $case.moves) {
    if ($m -notmatch $movePattern) {
      throw "invalid move token '$m' in case $($case.id)"
    }
  }
}

$domainText = Get-Content -Raw -Path $domainPath
$requiredExports = @(
  "export function createInitialState",
  "export function applyMove",
  "export function buildReplayLog",
  "export function replayFromLog",
  "export function computeDeterministicHash"
)
foreach ($token in $requiredExports) {
  if (-not $domainText.Contains($token)) {
    throw "domain.js is missing required export: $token"
  }
}

Write-Host "static-golden-verify: PASS ($($golden.cases.Count) cases checked)"
