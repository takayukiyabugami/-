param(
    [string]$OutDir = ".\white_pawn_rig"
)

Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"

function New-Color {
    param([string]$Hex, [int]$Alpha = 255)
    $h = $Hex.TrimStart("#")
    return [System.Drawing.Color]::FromArgb(
        $Alpha,
        [Convert]::ToInt32($h.Substring(0, 2), 16),
        [Convert]::ToInt32($h.Substring(2, 2), 16),
        [Convert]::ToInt32($h.Substring(4, 2), 16)
    )
}

function New-Brush {
    param([string]$Hex, [int]$Alpha = 255)
    return New-Object System.Drawing.SolidBrush (New-Color $Hex $Alpha)
}

function New-Pen {
    param([string]$Hex, [float]$Width = 1.0, [int]$Alpha = 255)
    $pen = New-Object System.Drawing.Pen ((New-Color $Hex $Alpha), $Width)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    return $pen
}

function ConvertTo-PointFArray {
    param([object[]]$Points)
    $arr = New-Object System.Drawing.PointF[] ($Points.Count)
    for ($i = 0; $i -lt $Points.Count; $i++) {
        $arr[$i] = New-Object System.Drawing.PointF ([single]$Points[$i][0], [single]$Points[$i][1])
    }
    return ,$arr
}

function Fill-Polygon {
    param($Graphics, [object[]]$Points, $Brush, $Pen = $null)
    $arr = ConvertTo-PointFArray $Points
    $Graphics.FillPolygon($Brush, $arr)
    if ($null -ne $Pen) {
        $Graphics.DrawPolygon($Pen, $arr)
    }
}

function Draw-Line {
    param($Graphics, [float]$X1, [float]$Y1, [float]$X2, [float]$Y2, $Pen)
    $Graphics.DrawLine($Pen, [single]$X1, [single]$Y1, [single]$X2, [single]$Y2)
}

function Add-PlateTexture {
    param($Graphics, [int]$X, [int]$Y, [int]$W, [int]$H, [int]$Seed, [int]$Count = 28)
    $rand = New-Object System.Random $Seed
    $speck = New-Brush "#2d2924" 70
    $scratch = New-Pen "#5d574d" 1.0 95
    for ($i = 0; $i -lt $Count; $i++) {
        $sx = $X + $rand.Next([Math]::Max(1, $W))
        $sy = $Y + $rand.Next([Math]::Max(1, $H))
        if (($i % 4) -eq 0) {
            $Graphics.DrawLine($scratch, $sx, $sy, $sx + $rand.Next(-10, 11), $sy + $rand.Next(5, 18))
        }
        else {
            $Graphics.FillEllipse($speck, $sx, $sy, $rand.Next(2, 5), $rand.Next(2, 5))
        }
    }
}

function Save-Png {
    param([string]$Name, [int]$Width, [int]$Height, [scriptblock]$Draw)
    $path = Join-Path $OutDir $Name
    $bitmap = New-Object System.Drawing.Bitmap ($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)
    & $Draw $graphics $Width $Height
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
}

function Draw-CenterStripe {
    param($Graphics, [float]$X, [float]$Top, [float]$Bottom)
    $black = New-Brush "#111111" 235
    $darkPen = New-Pen "#0a0a0a" 2.0 240
    Fill-Polygon $Graphics @(
        @(($X), ($Top)),
        @(($X + 14), ($Top + 62)),
        @(($X + 5), ($Top + 72)),
        @(($X + 10), ($Bottom - 34)),
        @(($X), ($Bottom)),
        @(($X - 10), ($Bottom - 34)),
        @(($X - 5), ($Top + 72)),
        @(($X - 14), ($Top + 62))
    ) $black $darkPen
    Fill-Polygon $Graphics @(
        @(($X - 6), ($Top + 88)),
        @(($X - 36), ($Top + 116)),
        @(($X - 13), ($Top + 123))
    ) $black $null
    Fill-Polygon $Graphics @(
        @(($X + 6), ($Top + 88)),
        @(($X + 36), ($Top + 116)),
        @(($X + 13), ($Top + 123))
    ) $black $null
}

function Draw-Rivets {
    param($Graphics, [object[]]$Points)
    $rim = New-Brush "#8a6c3c" 255
    $dot = New-Brush "#fff0c6" 210
    foreach ($p in $Points) {
        $Graphics.FillEllipse($rim, [float]$p[0] - 4, [float]$p[1] - 4, 8, 8)
        $Graphics.FillEllipse($dot, [float]$p[0] - 1.5, [float]$p[1] - 2, 3, 3)
    }
}

function Draw-SideLimb {
    param($Graphics, [int]$Width, [int]$Height, [string]$Kind, [string]$Side)
    $plate = New-Brush "#efe8d8" 245
    $shade = New-Brush "#b9b0a0" 180
    $edge = New-Pen "#332a1d" 3.0 230
    $gold = New-Pen "#8a6c3c" 3.0 230
    $mail = New-Brush "#252525" 220
    $cx = $Width / 2

    if ($Kind -eq "upper_arm") {
        $Graphics.FillEllipse($mail, $cx - 30, 24, 60, 90)
        $Graphics.FillEllipse($plate, $cx - 42, 10, 84, 62)
        $Graphics.DrawEllipse($edge, $cx - 42, 10, 84, 62)
        $Graphics.DrawArc($gold, $cx - 34, 20, 68, 48, 12, 156)
        Fill-Polygon $Graphics @(
            @(($cx - 26), 58), @(($cx + 30), 62), @(($cx + 22), 132), @(($cx - 22), 124)
        ) $plate $edge
        Add-PlateTexture $Graphics 18 14 ($Width - 36) 118 (930 + $Width + $Height) 16
    }
    elseif ($Kind -eq "forearm") {
        $Graphics.FillEllipse($mail, $cx - 24, 5, 48, 36)
        Fill-Polygon $Graphics @(
            @(($cx - 30), 18), @(($cx + 30), 18), @(($cx + 22), 118), @(($cx - 18), 130)
        ) $plate $edge
        $Graphics.FillRectangle($shade, $cx - 25, 62, 50, 12)
        $Graphics.DrawLine($gold, $cx - 21, 44, $cx + 22, 46)
        $Graphics.DrawLine($gold, $cx - 17, 104, $cx + 18, 99)
        Add-PlateTexture $Graphics 17 18 ($Width - 34) 112 (1100 + $Width) 18
    }
    elseif ($Kind -eq "hand") {
        $Graphics.FillEllipse($mail, $cx - 22, 2, 44, 34)
        Fill-Polygon $Graphics @(
            @(($cx - 24), 18), @(($cx + 21), 15), @(($cx + 28), 44), @(($cx + 8), 65), @(($cx - 18), 58)
        ) $plate $edge
        $Graphics.DrawLine((New-Pen "#554c42" 1.5 160), $cx - 10, 34, $cx + 18, 37)
        $Graphics.DrawLine((New-Pen "#554c42" 1.5 160), $cx - 8, 45, $cx + 11, 50)
        Add-PlateTexture $Graphics 18 18 ($Width - 34) 42 (1200 + $Width) 7
    }
    elseif ($Kind -eq "thigh") {
        $Graphics.FillEllipse($mail, $cx - 28, 5, 56, 34)
        Fill-Polygon $Graphics @(
            @(($cx - 34), 20), @(($cx + 34), 20), @(($cx + 24), 126), @(($cx - 24), 132)
        ) $plate $edge
        $Graphics.DrawLine($gold, $cx - 27, 54, $cx + 27, 52)
        $Graphics.DrawLine((New-Pen "#6f665a" 2.0 130), $cx, 28, $cx - 4, 118)
        Add-PlateTexture $Graphics 16 22 ($Width - 32) 108 (1300 + $Width) 22
    }
    elseif ($Kind -eq "shin") {
        $Graphics.FillEllipse($mail, $cx - 25, 2, 50, 34)
        Fill-Polygon $Graphics @(
            @(($cx - 31), 18), @(($cx + 31), 18), @(($cx + 23), 126), @(($cx + 10), 150), @(($cx - 17), 150), @(($cx - 25), 128)
        ) $plate $edge
        $Graphics.DrawLine($gold, $cx - 22, 47, $cx + 23, 46)
        $Graphics.DrawLine((New-Pen "#6f665a" 2.0 130), $cx + 2, 28, $cx - 3, 132)
        Add-PlateTexture $Graphics 17 18 ($Width - 34) 132 (1400 + $Width) 24
    }
    elseif ($Kind -eq "foot") {
        $dir = if ($Side -eq "left") { -1 } else { 1 }
        $toe = $cx + (36 * $dir)
        Fill-Polygon $Graphics @(
            @(($cx - (24 * $dir)), 16),
            @($toe, 21),
            @(($toe + (16 * $dir)), 42),
            @(($cx + (18 * $dir)), 58),
            @(($cx - (35 * $dir)), 52),
            @(($cx - (33 * $dir)), 28)
        ) $plate $edge
        $Graphics.DrawLine($gold, $cx - (18 * $dir), 33, $toe + (5 * $dir), 37)
        Add-PlateTexture $Graphics 12 18 ($Width - 24) 38 (1500 + $Width) 10
    }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Save-Png "body.png" 260 380 {
    param($g, $w, $h)
    $mail = New-Brush "#1f1f1f" 235
    $cloth = New-Brush "#e8dfcd" 245
    $plate = New-Brush "#eee7d7" 248
    $plateShade = New-Brush "#bdb4a4" 160
    $leather = New-Brush "#4a321e" 245
    $edge = New-Pen "#2d241a" 3.5 235
    $gold = New-Pen "#8a6c3c" 3.0 235

    $g.FillEllipse($mail, 87, 22, 86, 54)
    Fill-Polygon $g @(@(62, 54), @(198, 54), @(226, 192), @(182, 246), @(78, 246), @(34, 192)) $plate $edge
    $g.FillEllipse($plateShade, 56, 70, 46, 86)
    $g.FillEllipse($plateShade, 158, 70, 46, 86)
    $g.DrawLine($gold, 72, 68, 188, 68)
    $g.DrawLine((New-Pen "#6a5b48" 2.0 120), 130, 62, 130, 230)

    Draw-CenterStripe $g 130 86 232
    Add-PlateTexture $g 42 58 176 176 9029 46

    $g.FillRectangle($leather, 55, 210, 150, 28)
    $g.DrawRectangle($edge, 55, 210, 150, 28)
    $g.FillRectangle((New-Brush "#987641" 245), 118, 207, 24, 34)
    $g.DrawRectangle((New-Pen "#2d241a" 2.0 220), 118, 207, 24, 34)

    Fill-Polygon $g @(@(82, 230), @(178, 230), @(164, 356), @(137, 330), @(129, 372), @(113, 331), @(91, 356)) $cloth $edge
    $g.DrawLine((New-Pen "#7a715f" 1.5 120), 96, 252, 160, 250)
    Draw-CenterStripe $g 130 246 338
    Add-PlateTexture $g 86 232 88 116 9931 22

    $g.FillRectangle((New-Brush "#312315" 230), 42, 218, 36, 72)
    $g.FillRectangle((New-Brush "#312315" 230), 182, 218, 36, 72)
    Draw-Rivets $g @(@(69, 78), @(191, 78), @(66, 212), @(194, 212), @(76, 228), @(184, 228))
}

Save-Png "head.png" 160 170 {
    param($g, $w, $h)
    $plate = New-Brush "#efe8d8" 250
    $shade = New-Brush "#bdb4a4" 170
    $dark = New-Brush "#111111" 245
    $edge = New-Pen "#2d241a" 3.2 235
    $gold = New-Pen "#8a6c3c" 2.8 235

    $g.FillEllipse($plate, 38, 18, 84, 112)
    $g.DrawEllipse($edge, 38, 18, 84, 112)
    Fill-Polygon $g @(@(44, 78), @(116, 78), @(111, 132), @(80, 148), @(49, 132)) $plate $edge
    $g.FillRectangle($dark, 50, 72, 60, 16)
    $g.FillRectangle($dark, 73, 45, 14, 72)
    $g.DrawLine($gold, 80, 18, 80, 150)
    $g.DrawArc($gold, 47, 28, 66, 88, 205, 130)
    $g.DrawLine($gold, 48, 101, 112, 101)
    $g.FillEllipse($shade, 55, 122, 50, 20)
    Add-PlateTexture $g 40 22 80 120 9100 22
    Draw-Rivets $g @(@(80, 25), @(47, 94), @(113, 94), @(59, 124), @(101, 124))
}

foreach ($side in @("left", "right")) {
    Save-Png "$($side)_upper_arm.png" 110 150 {
        param($g, $w, $h)
        Draw-SideLimb $g $w $h "upper_arm" $side
    }
    Save-Png "$($side)_forearm.png" 96 150 {
        param($g, $w, $h)
        Draw-SideLimb $g $w $h "forearm" $side
    }
    Save-Png "$($side)_hand.png" 74 78 {
        param($g, $w, $h)
        Draw-SideLimb $g $w $h "hand" $side
    }
    Save-Png "$($side)_thigh.png" 98 145 {
        param($g, $w, $h)
        Draw-SideLimb $g $w $h "thigh" $side
    }
    Save-Png "$($side)_shin.png" 92 160 {
        param($g, $w, $h)
        Draw-SideLimb $g $w $h "shin" $side
    }
    Save-Png "$($side)_foot.png" 106 68 {
        param($g, $w, $h)
        Draw-SideLimb $g $w $h "foot" $side
    }
}

Save-Png "sword.png" 92 340 {
    param($g, $w, $h)
    $blade = New-Brush "#d8d8d2" 250
    $bladeShade = New-Brush "#8d8f8c" 155
    $edge = New-Pen "#2a2a28" 2.5 230
    $gold = New-Brush "#9a763d" 245
    $leather = New-Brush "#3b2717" 245

    Fill-Polygon $g @(@(46, 8), @(66, 206), @(46, 246), @(26, 206)) $blade $edge
    Fill-Polygon $g @(@(46, 18), @(56, 207), @(46, 238)) $bladeShade $null
    $g.DrawLine((New-Pen "#f8f8f1" 1.5 160), 46, 18, 46, 238)
    Fill-Polygon $g @(@(13, 218), @(79, 218), @(68, 235), @(24, 235)) $gold $edge
    $g.FillRectangle($leather, 38, 233, 16, 70)
    $g.DrawRectangle($edge, 38, 233, 16, 70)
    $g.FillEllipse($gold, 32, 300, 28, 28)
    $g.DrawEllipse($edge, 32, 300, 28, 28)
}

Save-Png "shield.png" 188 270 {
    param($g, $w, $h)
    $face = New-Brush "#eee6d4" 248
    $shade = New-Brush "#bdb4a4" 145
    $rim = New-Pen "#8a6c3c" 8.0 245
    $edge = New-Pen "#2d241a" 3.2 235
    $leather = New-Brush "#4a321e" 235

    $shield = @(@(94, 13), @(166, 44), @(158, 178), @(94, 254), @(30, 178), @(22, 44))
    Fill-Polygon $g $shield $face $edge
    $g.DrawPolygon($rim, (ConvertTo-PointFArray $shield))
    Fill-Polygon $g @(@(94, 22), @(151, 49), @(145, 162), @(94, 236)) $shade $null
    Draw-CenterStripe $g 94 72 214
    Add-PlateTexture $g 32 38 124 180 9200 36
    Draw-Rivets $g @(@(94, 26), @(36, 56), @(152, 56), @(34, 166), @(154, 166), @(94, 238))
    $g.FillRectangle($leather, 58, 126, 72, 20)
    $g.DrawRectangle((New-Pen "#2d241a" 2.0 220), 58, 126, 72, 20)
}

Save-Png "shadow.png" 340 90 {
    param($g, $w, $h)
    for ($i = 0; $i -lt 12; $i++) {
        $alpha = 36 - ($i * 2)
        $brush = New-Brush "#000000" $alpha
        $padX = 20 + ($i * 8)
        $padY = 24 + ($i * 2)
        $g.FillEllipse($brush, $padX, $padY, $w - ($padX * 2), $h - ($padY * 2))
    }
}

$parts = [ordered]@{
    body            = [ordered]@{ file = "body.png";            size = @(260, 380); pivot = @(130, 68);  parent = $null; attach = @(0, 0);      rotation = 0 }
    head            = [ordered]@{ file = "head.png";            size = @(160, 170); pivot = @(80, 146);  parent = "body"; attach = @(130, 54); rotation = 0 }
    left_upper_arm  = [ordered]@{ file = "left_upper_arm.png";  size = @(110, 150); pivot = @(55, 38);   parent = "body"; attach = @(55, 88);  rotation = -12 }
    left_forearm    = [ordered]@{ file = "left_forearm.png";    size = @(96, 150);  pivot = @(48, 22);   parent = "left_upper_arm"; attach = @(52, 120); rotation = -9 }
    left_hand       = [ordered]@{ file = "left_hand.png";       size = @(74, 78);   pivot = @(37, 17);   parent = "left_forearm"; attach = @(50, 128); rotation = -4 }
    right_upper_arm = [ordered]@{ file = "right_upper_arm.png"; size = @(110, 150); pivot = @(55, 38);   parent = "body"; attach = @(205, 88); rotation = 12 }
    right_forearm   = [ordered]@{ file = "right_forearm.png";   size = @(96, 150);  pivot = @(48, 22);   parent = "right_upper_arm"; attach = @(58, 120); rotation = 9 }
    right_hand      = [ordered]@{ file = "right_hand.png";      size = @(74, 78);   pivot = @(37, 17);   parent = "right_forearm"; attach = @(46, 128); rotation = 4 }
    sword           = [ordered]@{ file = "sword.png";           size = @(92, 340);  pivot = @(46, 246);  parent = "right_hand"; attach = @(40, 50); rotation = -32 }
    shield          = [ordered]@{ file = "shield.png";          size = @(188, 270); pivot = @(94, 136);  parent = "left_forearm"; attach = @(44, 88); rotation = 6 }
    left_thigh      = [ordered]@{ file = "left_thigh.png";      size = @(98, 145);  pivot = @(49, 22);   parent = "body"; attach = @(92, 232); rotation = 7 }
    left_shin       = [ordered]@{ file = "left_shin.png";       size = @(92, 160);  pivot = @(46, 20);   parent = "left_thigh"; attach = @(49, 128); rotation = -3 }
    left_foot       = [ordered]@{ file = "left_foot.png";       size = @(106, 68);  pivot = @(50, 18);   parent = "left_shin"; attach = @(45, 148); rotation = -3 }
    right_thigh     = [ordered]@{ file = "right_thigh.png";     size = @(98, 145);  pivot = @(49, 22);   parent = "body"; attach = @(168, 232); rotation = -7 }
    right_shin      = [ordered]@{ file = "right_shin.png";      size = @(92, 160);  pivot = @(46, 20);   parent = "right_thigh"; attach = @(49, 128); rotation = 3 }
    right_foot      = [ordered]@{ file = "right_foot.png";      size = @(106, 68);  pivot = @(56, 18);   parent = "right_shin"; attach = @(47, 148); rotation = 3 }
    shadow          = [ordered]@{ file = "shadow.png";          size = @(340, 90);  pivot = @(170, 44);  parent = $null; attach = @(130, 360); rotation = 0 }
}

$rig = [ordered]@{
    unit = "white_pawn_western_warrior"
    version = 1
    meta = [ordered]@{
        units = "pixels"
        origin = "body_pivot"
        coordinate = "+X right, +Y down"
        background = "transparent"
        intended_use = "2d modular game rig"
        style = "white western armored chess pawn infantry"
    }
    parts = $parts
    draw_order = @(
        "shadow",
        "right_thigh",
        "right_shin",
        "right_foot",
        "body",
        "head",
        "left_thigh",
        "left_shin",
        "left_foot",
        "left_upper_arm",
        "left_forearm",
        "left_hand",
        "shield",
        "right_upper_arm",
        "right_forearm",
        "right_hand",
        "sword"
    )
    default_pose = [ordered]@{
        note = "Place body pivot at actor root. Each child pivot snaps to its parent's attach point, then applies rotation degrees."
        root_position = @(0, 0)
    }
    animation_notes = [ordered]@{
        idle = "Small head and shield sway, 1-2 degrees."
        walk = "Legs counter-rotate; keep shield in front layer."
        attack_thrust = "Right arm extends, sword rotation eases toward -78 degrees, body leans forward."
    }
}

$json = $rig | ConvertTo-Json -Depth 8
Set-Content -LiteralPath (Join-Path $OutDir "rig.json") -Value $json -Encoding UTF8

Get-ChildItem -LiteralPath $OutDir | Sort-Object Name | Select-Object Name, Length
