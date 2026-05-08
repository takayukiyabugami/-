param(
    [Parameter(Mandatory=$true)]
    [string]$Title,

    [string]$OutputRoot = "outputs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-ToSlug {
    param([string]$Value)

    $slug = $Value.Trim().ToLowerInvariant()
    $slug = $slug -replace '[^\p{L}\p{Nd}]+', '-'
    $slug = $slug.Trim('-')
    if ([string]::IsNullOrWhiteSpace($slug)) {
        return "story-project"
    }
    return $slug
}

$slug = Convert-ToSlug -Value $Title
$root = Join-Path -Path $OutputRoot -ChildPath $slug
New-Item -ItemType Directory -Force -Path $root | Out-Null

$files = @{
    "concept.md" = @"
# $Title

## Premise

## Reader Promise

## Tone

## Theme Pressure

## Constraints
"@
    "characters.md" = @"
# Characters

## Protagonist
- Desire:
- Wound:
- Contradiction:
- Speech:
- Limit:
- Arc:

## Key Cast
"@
    "outline.md" = @"
# Outline

## Arc Map

## Chapter Beats

## Reversals

## Foreshadowing Ledger
"@
    "manuscript.md" = @"
# Manuscript

## Chapter 1
"@
    "revision-notes.md" = @"
# Revision Notes

## Diagnosis

## Decisions

## Continuity Changes
"@
}

foreach ($entry in $files.GetEnumerator()) {
    $path = Join-Path -Path $root -ChildPath $entry.Key
    if (-not (Test-Path -LiteralPath $path)) {
        Set-Content -LiteralPath $path -Value $entry.Value -Encoding UTF8
    }
}

Write-Output $root
