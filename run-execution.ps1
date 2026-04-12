$ErrorActionPreference = "Continue"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$unityProject = Join-Path $root "chess-unity-presentation"
$browserProject = Join-Path $root "chess-game"
$results = [ordered]@{
  BrowserStaticGolden = "SKIPPED"
  BrowserNodeGolden = "SKIPPED"
  UnityEditMode = "SKIPPED"
  UnityPlayMode = "SKIPPED"
}

Write-Host "== Execution Start =="

# Browser static parity (PowerShell only)
$staticScript = Join-Path $browserProject "tests\verify-golden-static.ps1"
if (Test-Path $staticScript) {
  try {
    powershell -ExecutionPolicy Bypass -File $staticScript
    if ($LASTEXITCODE -eq 0) {
      $results.BrowserStaticGolden = "PASS"
    } else {
      $results.BrowserStaticGolden = "FAIL"
    }
  } catch {
    $results.BrowserStaticGolden = "FAIL"
  }
} else {
  $results.BrowserStaticGolden = "MISSING_SCRIPT"
}

# Browser node parity
$nodeSource = $null
$nodeCmd = Get-Command node -ErrorAction SilentlyContinue
if ($nodeCmd) {
  $nodeSource = $nodeCmd.Source
} else {
  $nodeCandidates = @(
    (Join-Path $env:ProgramFiles "nodejs\node.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "nodejs\node.exe")
  )
  foreach ($candidate in $nodeCandidates) {
    if ($candidate -and (Test-Path $candidate)) {
      $nodeSource = $candidate
      break
    }
  }
}

if ($nodeSource) {
  $nodeTest = Join-Path $browserProject "tests\golden-parity.test.mjs"
  if (Test-Path $nodeTest) {
    try {
      & $nodeSource $nodeTest
      if ($LASTEXITCODE -eq 0) {
        $results.BrowserNodeGolden = "PASS"
      } else {
        $results.BrowserNodeGolden = "FAIL"
      }
    } catch {
      $results.BrowserNodeGolden = "FAIL"
    }
  } else {
    $results.BrowserNodeGolden = "MISSING_SCRIPT"
  }
} else {
  $results.BrowserNodeGolden = "NODE_NOT_FOUND"
}

# Unity tests (batchmode)
$unityExe = $null
$candidates = @(
  "C:\Program Files\Unity 2022.3.62f3\Editor\Unity.exe",
  "C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe",
  "C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe",
  "C:\Program Files\Unity\Hub\Editor\2022.3.49f1\Editor\Unity.exe"
)
foreach ($candidate in $candidates) {
  if (Test-Path $candidate) {
    $unityExe = $candidate
    break
  }
}

if (-not $unityExe) {
  $hubEditorRoot = "C:\Program Files\Unity\Hub\Editor"
  if (Test-Path $hubEditorRoot) {
    $unityDynamic = Get-ChildItem -Path $hubEditorRoot -Directory -ErrorAction SilentlyContinue |
      Sort-Object Name -Descending |
      ForEach-Object { Join-Path $_.FullName "Editor\Unity.exe" } |
      Where-Object { Test-Path $_ } |
      Select-Object -First 1
    if ($unityDynamic) {
      $unityExe = $unityDynamic
    }
  }
}

if ($unityExe) {
  $runner = Join-Path $unityProject "tools\run-unity-tests.ps1"
  if (Test-Path $runner) {
    try {
      powershell -ExecutionPolicy Bypass -File $runner -UnityExePath $unityExe -TestPlatform EditMode -ProjectPath $unityProject -ResultPath (Join-Path $unityProject "TestResults.EditMode.xml")
      if ($LASTEXITCODE -eq 0) {
        $results.UnityEditMode = "PASS"
      } else {
        $results.UnityEditMode = "FAIL"
      }
    } catch {
      $results.UnityEditMode = "FAIL"
    }

    try {
      powershell -ExecutionPolicy Bypass -File $runner -UnityExePath $unityExe -TestPlatform PlayMode -ProjectPath $unityProject -ResultPath (Join-Path $unityProject "TestResults.PlayMode.xml")
      if ($LASTEXITCODE -eq 0) {
        $results.UnityPlayMode = "PASS"
      } else {
        $results.UnityPlayMode = "FAIL"
      }
    } catch {
      $results.UnityPlayMode = "FAIL"
    }
  } else {
    $results.UnityEditMode = "MISSING_RUNNER"
    $results.UnityPlayMode = "MISSING_RUNNER"
  }
} else {
  $results.UnityEditMode = "UNITY_NOT_FOUND"
  $results.UnityPlayMode = "UNITY_NOT_FOUND"
}

Write-Host ""
Write-Host "== Execution Summary =="
$results.GetEnumerator() | ForEach-Object {
  Write-Host ("{0,-22}: {1}" -f $_.Key, $_.Value)
}

$anyFail = $results.Values -contains "FAIL"
if ($anyFail) {
  exit 1
}
exit 0
