param(
  [Parameter(Mandatory = $true)]
  [string]$UnityExePath,

  [ValidateSet("EditMode", "PlayMode")]
  [string]$TestPlatform = "EditMode",

  [string]$ProjectPath = (Resolve-Path "..").Path,
  [string]$ResultPath = (Join-Path (Resolve-Path "..").Path "TestResults.xml")
)

if (-not (Test-Path $UnityExePath)) {
  throw "Unity executable not found: $UnityExePath"
}

Write-Host "Running Unity tests..."
Write-Host "  Project   : $ProjectPath"
Write-Host "  Platform  : $TestPlatform"
Write-Host "  Result XML: $ResultPath"

if (-not (Test-Path $ProjectPath)) {
  throw "Project path not found: $ProjectPath"
}

$arguments = @(
  "-batchmode",
  "-quit",
  "-projectPath", $ProjectPath,
  "-runTests",
  "-testPlatform", $TestPlatform,
  "-testResults", $ResultPath,
  "-logFile", "-"
)

$process = Start-Process -FilePath $UnityExePath -ArgumentList $arguments -Wait -PassThru
$exitCode = $process.ExitCode
if ($null -eq $exitCode) {
  $exitCode = 1
}

if ($exitCode -ne 0) {
  throw "Unity test run failed with exit code $exitCode"
}

Write-Host "Unity tests completed successfully."
