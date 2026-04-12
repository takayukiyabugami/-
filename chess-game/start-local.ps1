param(
  [int]$Port = 5173
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Start-With {
  param(
    [string]$Exe,
    [string[]]$Args
  )
  Write-Host "Serving $root on http://localhost:$Port/"
  & $Exe @Args
}

$pyLauncher = Get-Command py -ErrorAction SilentlyContinue
if ($pyLauncher) {
  Start-With -Exe $pyLauncher.Source -Args @("-3", "-m", "http.server", "$Port")
  exit 0
}

$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
  Start-With -Exe $python.Source -Args @("-m", "http.server", "$Port")
  exit 0
}

throw "Python not found. Install Python 3 or run another local HTTP server in: $root"
