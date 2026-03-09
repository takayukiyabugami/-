$ErrorActionPreference = "Stop"

$Lines = [System.Collections.Generic.List[string]]::new()
$AllPoints = [System.Collections.Generic.List[object]]::new()

function Add-Dxf {
    param([string[]]$Items)
    foreach ($item in $Items) {
        $script:Lines.Add($item)
    }
}

function F {
    param([double]$Value)
    return [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, "{0:0.000000}", $Value)
}

function Add-Point {
    param([double]$X, [double]$Y)
    $script:AllPoints.Add(@($X, $Y))
}

function Add-LineEntity {
    param(
        [string]$Layer,
        [double]$X1,
        [double]$Y1,
        [double]$X2,
        [double]$Y2
    )
    Add-Dxf @(
        "0", "LINE", "8", $Layer,
        "10", (F $X1), "20", (F $Y1), "30", "0.0",
        "11", (F $X2), "21", (F $Y2), "31", "0.0"
    )
}

function Add-PolylineClosed {
    param(
        [string]$Layer,
        [object[]]$Points
    )
    Add-Dxf @("0", "POLYLINE", "8", $Layer, "66", "1", "70", "1")
    foreach ($pt in $Points) {
        Add-Dxf @(
            "0", "VERTEX", "8", $Layer,
            "10", (F ([double]$pt[0])),
            "20", (F ([double]$pt[1])),
            "30", "0.0"
        )
    }
    Add-Dxf @("0", "SEQEND", "8", $Layer)
}

function Add-CircleEntity {
    param(
        [string]$Layer,
        [double]$CX,
        [double]$CY,
        [double]$R
    )
    Add-Dxf @(
        "0", "CIRCLE", "8", $Layer,
        "10", (F $CX), "20", (F $CY), "30", "0.0",
        "40", (F $R)
    )
}

function Add-TextEntity {
    param(
        [string]$Layer,
        [double]$X,
        [double]$Y,
        [string]$Value,
        [double]$Height = 140.0,
        [double]$Rotation = 0.0
    )
    Add-Dxf @(
        "0", "TEXT", "8", $Layer,
        "10", (F $X), "20", (F $Y), "30", "0.0",
        "40", ("{0:0.0}" -f $Height),
        "1", $Value,
        "50", ("{0:0.0}" -f $Rotation),
        "72", "1",
        "11", (F $X), "21", (F $Y), "31", "0.0"
    )
}

function Get-RectPoints {
    param(
        [double]$PX,
        [double]$PY,
        [double]$SX,
        [double]$SY
    )
    $hx = $SX / 2.0
    $hy = $SY / 2.0
    return @(
        @(($PX - $hx), ($PY + $hy)),
        @(($PX + $hx), ($PY + $hy)),
        @(($PX + $hx), ($PY - $hy)),
        @(($PX - $hx), ($PY - $hy))
    )
}

function Add-RectItem {
    param(
        [int]$No,
        [string]$Name,
        [double]$SX,
        [double]$SY,
        [double]$PX,
        [double]$PY,
        [double]$SliceStep = 0.0
    )
    $pts = Get-RectPoints -PX $PX -PY $PY -SX $SX -SY $SY
    Add-PolylineClosed -Layer "STRUCT" -Points $pts
    foreach ($pt in $pts) {
        Add-Point -X ([double]$pt[0]) -Y ([double]$pt[1])
    }

    if ($SliceStep -gt 0) {
        $xLeft = $PX - $SX / 2.0
        $xRight = $PX + $SX / 2.0
        $yTop = $PY + $SY / 2.0
        $yBottom = $PY - $SY / 2.0
        $x = $xLeft + $SliceStep
        while ($x -lt $xRight) {
            Add-LineEntity -Layer "MARK" -X1 $x -Y1 $yTop -X2 $x -Y2 $yBottom
            $x += $SliceStep
        }
    }

    Add-TextEntity -Layer "TEXT" -X $PX -Y ($PY + $SY / 2.0 + 110.0) -Value "$No. $Name" -Height 90.0
}

function Add-CircleItem {
    param(
        [int]$No,
        [string]$Name,
        [double]$Diameter,
        [double]$PX,
        [double]$PY
    )
    $r = $Diameter / 2.0
    Add-CircleEntity -Layer "STRUCT" -CX $PX -CY $PY -R $r
    Add-Point -X ($PX - $r) -Y ($PY - $r)
    Add-Point -X ($PX + $r) -Y ($PY + $r)
    Add-TextEntity -Layer "TEXT" -X $PX -Y ($PY + $r + 110.0) -Value "$No. $Name" -Height 90.0
}

# Header / Tables
Add-Dxf @(
    "0", "SECTION", "2", "HEADER",
    "9", "$ACADVER", "1", "AC1009",
    "9", "$DWGCODEPAGE", "3", "ANSI_932",
    "9", "$INSUNITS", "70", "4",
    "9", "$MEASUREMENT", "70", "1",
    "0", "ENDSEC"
)

Add-Dxf @("0", "SECTION", "2", "TABLES")
Add-Dxf @(
    "0", "TABLE", "2", "LTYPE", "70", "1",
    "0", "LTYPE", "2", "CONTINUOUS", "70", "0", "3", "Solid line",
    "72", "65", "73", "0", "40", "0.0",
    "0", "ENDTAB"
)
Add-Dxf @("0", "TABLE", "2", "LAYER", "70", "4")
Add-Dxf @("0", "LAYER", "2", "BASE", "70", "0", "62", "8", "6", "CONTINUOUS")
Add-Dxf @("0", "LAYER", "2", "STRUCT", "70", "0", "62", "7", "6", "CONTINUOUS")
Add-Dxf @("0", "LAYER", "2", "MARK", "70", "0", "62", "1", "6", "CONTINUOUS")
Add-Dxf @("0", "LAYER", "2", "TEXT", "70", "0", "62", "2", "6", "CONTINUOUS")
Add-Dxf @("0", "ENDTAB")
Add-Dxf @(
    "0", "TABLE", "2", "STYLE", "70", "1",
    "0", "STYLE", "2", "STANDARD", "70", "0",
    "40", "0.0", "41", "1.0", "50", "0.0",
    "71", "0", "42", "200.0", "3", "txt", "4", "",
    "0", "ENDTAB",
    "0", "ENDSEC"
)
Add-Dxf @("0", "SECTION", "2", "BLOCKS", "0", "ENDSEC")
Add-Dxf @("0", "SECTION", "2", "ENTITIES")

# Base line AB
$ax = 0.0
$ay = 0.0
$bx = 10360.0
$by = 0.0
Add-Point -X $ax -Y $ay
Add-Point -X $bx -Y $by
Add-LineEntity -Layer "BASE" -X1 $ax -Y1 $ay -X2 $bx -Y2 $by
Add-TextEntity -Layer "TEXT" -X $ax -Y ($ay - 220.0) -Value "A(0,0)" -Height 100.0
Add-TextEntity -Layer "TEXT" -X $bx -Y ($by - 220.0) -Value "B(10360,0)" -Height 100.0

# Items 1-18
Add-RectItem -No 1 -Name "NTT valve" -SX 1240.0 -SY 640.0 -PX 8930.0 -PY 8400.0
Add-CircleItem -No 2 -Name "Manhole" -Diameter 700.0 -PX 3310.0 -PY 6950.0
Add-CircleItem -No 3 -Name "Stop valve" -Diameter 160.0 -PX 7060.0 -PY 8440.0
Add-RectItem -No 4 -Name "Electric box" -SX 1550.0 -SY 500.0 -PX 2235.0 -PY 5910.0
Add-RectItem -No 5 -Name "Masu" -SX 620.0 -SY 310.0 -PX 1860.0 -PY 4965.0
Add-RectItem -No 6 -Name "As" -SX 8000.0 -SY 3700.0 -PX 6360.0 -PY 7210.0
Add-RectItem -No 7 -Name "Apron" -SX 8000.0 -SY 180.0 -PX 6360.0 -PY 5270.0 -SliceStep 2000.0
Add-RectItem -No 8 -Name "Gutter" -SX 9360.0 -SY 500.0 -PX 5680.0 -PY 4930.0
Add-RectItem -No 9 -Name "Apron" -SX 1000.0 -SY 180.0 -PX 1860.0 -PY 5270.0
Add-RectItem -No 10 -Name "Whale" -SX 1360.0 -SY 180.0 -PX 680.0 -PY 5270.0
Add-RectItem -No 11 -Name "Fence" -SX 2900.0 -SY 110.0 -PX 1890.0 -PY 5495.0
Add-RectItem -No 12 -Name "Vertical apron" -SX 8000.0 -SY 200.0 -PX 6360.0 -PY -1900.0 -SliceStep 2000.0
Add-RectItem -No 13 -Name "Vertical apron" -SX 2060.0 -SY 200.0 -PX 1330.0 -PY -1900.0

$whaleRel = @(@(0.0, 0.0), @(300.0, 0.0), @(300.0, 200.0), @(0.0, 50.0))
$whaleAbs = @()
foreach ($pt in $whaleRel) {
    $absX = [double]$pt[0] + 0.0
    $absY = [double]$pt[1] - 2000.0
    $whaleAbs += ,@($absX, $absY)
    Add-Point -X $absX -Y $absY
}
Add-PolylineClosed -Layer "STRUCT" -Points $whaleAbs
$cX = ($whaleAbs | ForEach-Object { $_[0] } | Measure-Object -Average).Average
$cY = ($whaleAbs | ForEach-Object { $_[1] } | Measure-Object -Average).Average
Add-TextEntity -Layer "TEXT" -X $cX -Y ($cY + 160.0) -Value "14. Whale" -Height 90.0

Add-RectItem -No 15 -Name "Apron hole" -SX 320.0 -SY 120.0 -PX 7360.0 -PY -2040.0
Add-RectItem -No 16 -Name "Fence" -SX 2900.0 -SY 790.0 -PX 3290.0 -PY -1605.0
Add-RectItem -No 17 -Name "Box" -SX 1550.0 -SY 1230.0 -PX 1275.0 -PY -1385.0
Add-RectItem -No 18 -Name "Tree" -SX 1840.0 -SY 1240.0 -PX 5680.0 -PY 5980.0

$xs = $AllPoints | ForEach-Object { [double]$_[0] }
$ys = $AllPoints | ForEach-Object { [double]$_[1] }
$minX = ($xs | Measure-Object -Minimum).Minimum
$maxX = ($xs | Measure-Object -Maximum).Maximum
$maxY = ($ys | Measure-Object -Maximum).Maximum
$titleX = ($minX + $maxX) / 2.0
Add-TextEntity -Layer "TEXT" -X $titleX -Y ($maxY + 500.0) -Value "Requested layout drawing" -Height 220.0
Add-TextEntity -Layer "TEXT" -X $titleX -Y ($maxY + 250.0) -Value "Base AB: A(0,0) B(10360,0)" -Height 120.0

Add-Dxf @("0", "ENDSEC", "0", "EOF")

$outDir = Join-Path $PSScriptRoot "outputs"
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

$outPath = Join-Path $outDir "requested_layout_ab_10360.dxf"
$enc = [System.Text.Encoding]::GetEncoding(932)
[System.IO.File]::WriteAllLines($outPath, $Lines, $enc)

Write-Output ("DXF written: {0}" -f $outPath)
Write-Output "Items: 18"
