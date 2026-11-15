# Generates the LiteHub logo set into ..\assets (icon, PNGs, wordmark, social preview).
# Run: powershell -ExecutionPolicy Bypass -File tools\make-logo.ps1
Add-Type -AssemblyName System.Drawing
$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets'
New-Item -ItemType Directory -Force $out | Out-Null

$c1 = [Drawing.Color]::FromArgb(255, 91, 84, 232)    # indigo
$c2 = [Drawing.Color]::FromArgb(255, 168, 85, 247)   # violet

function New-Canvas($w, $h) {
    $b = New-Object Drawing.Bitmap $w, $h
    $g = [Drawing.Graphics]::FromImage($b)
    $g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([Drawing.Color]::Transparent)
    return @($b, $g)
}

function Round-Rect($x, $y, $w, $h, $r) {
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, 2 * $r, 2 * $r, 180, 90); $p.AddArc($x + $w - 2 * $r, $y, 2 * $r, 2 * $r, 270, 90)
    $p.AddArc($x + $w - 2 * $r, $y + $h - 2 * $r, 2 * $r, 2 * $r, 0, 90); $p.AddArc($x, $y + $h - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $p.CloseFigure(); return $p
}

# The mark: rounded square, a center node and four account nodes around it.
function Draw-Mark($g, $x, $y, $s) {
    $path = Round-Rect $x $y $s $s ($s * 0.22)
    $br = New-Object Drawing.Drawing2D.LinearGradientBrush ([Drawing.PointF]::new($x, $y)), ([Drawing.PointF]::new($x + $s, $y + $s)), $c1, $c2
    $g.FillPath($br, $path)
    $cx = $x + $s / 2; $cy = $y + $s / 2
    $white = [Drawing.Color]::White
    $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(150, 255, 255, 255)), ($s * 0.045)
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $d = $s * 0.27
    $nodes = @(@(-1, -1), @(1, -1), @(-1, 1), @(1, 1))
    foreach ($n in $nodes) {
        $nx = $cx + $n[0] * $d; $ny = $cy + $n[1] * $d
        $g.DrawLine($pen, [single]$cx, [single]$cy, [single]$nx, [single]$ny)
    }
    $sb = New-Object Drawing.SolidBrush ([Drawing.Color]::White)
    $r2 = $s * 0.085
    foreach ($n in $nodes) { $g.FillEllipse($sb, [single]($cx + $n[0] * $d - $r2), [single]($cy + $n[1] * $d - $r2), [single](2 * $r2), [single](2 * $r2)) }
    $r1 = $s * 0.14
    $g.FillEllipse((New-Object Drawing.SolidBrush $white), [single]($cx - $r1), [single]($cy - $r1), [single](2 * $r1), [single](2 * $r1))
}

function Mark-Png($size) {
    $cv = New-Canvas $size $size
    Draw-Mark $cv[1] 0 0 $size
    $cv[1].Dispose(); return $cv[0]
}

# --- PNG sizes and the .ico (PNG frames) ---
$sizes = 16, 24, 32, 48, 64, 128, 256
$frames = @()
foreach ($s in $sizes) {
    $bmp = Mark-Png $s
    $ms = New-Object IO.MemoryStream; $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png); $frames += , @($s, $ms.ToArray()); $bmp.Dispose()
}
$big = Mark-Png 512; $big.Save((Join-Path $out 'logo-512.png'), [Drawing.Imaging.ImageFormat]::Png); $big.Dispose()

$ms = New-Object IO.MemoryStream; $bw = New-Object IO.BinaryWriter $ms
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $dim = if ($f[0] -ge 256) { 0 } else { $f[0] }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$f[1].Length); $bw.Write([uint32]$offset)
    $offset += $f[1].Length
}
foreach ($f in $frames) { $bw.Write($f[1]) }
[IO.File]::WriteAllBytes((Join-Path $out 'logo.ico'), $ms.ToArray())

# --- wordmark (README banner) and social preview ---
function Wordmark($w, $h, $markSize, $title, $tag, $bg) {
    $cv = New-Canvas $w $h; $g = $cv[1]
    if ($bg) { $g.Clear($bg) }
    $my = ($h - $markSize) / 2
    Draw-Mark $g ($markSize * 0.4) $my $markSize
    $tx = $markSize * 0.4 + $markSize + $markSize * 0.25
    $fTitle = New-Object Drawing.Font 'Segoe UI Semibold', ($markSize * 0.36), ([Drawing.FontStyle]::Regular), ([Drawing.GraphicsUnit]::Pixel)
    $fs = $markSize * 0.13
    do {
        $fTag = New-Object Drawing.Font 'Segoe UI', $fs, ([Drawing.FontStyle]::Regular), ([Drawing.GraphicsUnit]::Pixel)
        $fits = $g.MeasureString($tag, $fTag).Width -lt ($w - $tx - $markSize * 0.3)
        if (-not $fits) { $fs = $fs - 1 }
    } while (-not $fits -and $fs -gt 8)
    $dark = [Drawing.Color]::FromArgb(255, 30, 27, 75)
    $g.DrawString($title, $fTitle, (New-Object Drawing.SolidBrush $dark), [single]$tx, [single]($my + $markSize * 0.12))
    $g.DrawString($tag, $fTag, (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 90, 90, 120))), [single]($tx + 4), [single]($my + $markSize * 0.62))
    $g.Dispose(); return $cv[0]
}
$tag = 'All your web accounts in one light window'
$wm = Wordmark 1200 360 240 'LiteHub' $tag ([Drawing.Color]::White)
$wm.Save((Join-Path $out 'logo-wordmark.png'), [Drawing.Imaging.ImageFormat]::Png); $wm.Dispose()
$sp = Wordmark 1280 640 320 'LiteHub' $tag ([Drawing.Color]::FromArgb(255, 244, 243, 255))
$sp.Save((Join-Path $out 'social-preview.png'), [Drawing.Imaging.ImageFormat]::Png); $sp.Dispose()
Get-ChildItem $out | Select-Object Name, Length
