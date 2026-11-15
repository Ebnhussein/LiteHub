# Draws simple stand-in icons (brand-colored tile + plain pictogram, not the vendors' own logos) for services whose site gives no usable icon.
# They live in assets\services\generated and are replaced automatically as soon as the site serves a real icon.
# Run: powershell -ExecutionPolicy Bypass -File tools\make-generated-icons.ps1
Add-Type -AssemblyName System.Drawing
$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'assets\services\generated'
New-Item -ItemType Directory -Force $out | Out-Null
$S = 256
function C($r, $g, $b) { [Drawing.Color]::FromArgb(255, $r, $g, $b) }
$white = [Drawing.Brushes]::White

function New-Tile($c1, $c2) {
    $bmp = New-Object Drawing.Bitmap $S, $S
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; $g.Clear([Drawing.Color]::Transparent)
    $r = 58; $d = 2 * $r
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $p.AddArc(0, 0, $d, $d, 180, 90); $p.AddArc($S - $d, 0, $d, $d, 270, 90); $p.AddArc($S - $d, $S - $d, $d, $d, 0, 90); $p.AddArc(0, $S - $d, $d, $d, 90, 90); $p.CloseFigure()
    $rect = New-Object Drawing.Rectangle 0, 0, $S, $S
    $br = New-Object Drawing.Drawing2D.LinearGradientBrush $rect, $c1, $c2, 90
    $g.FillPath($br, $p)
    @($bmp, $g)
}
function Save($t, $host_) { $t[1].Dispose(); $t[0].Save((Join-Path $out "$host_.png"), [Drawing.Imaging.ImageFormat]::Png); $t[0].Dispose() }
function Letter($g, $text, $size) {
    $f = New-Object Drawing.Font 'Segoe UI', $size, ([Drawing.FontStyle]::Bold), ([Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object Drawing.StringFormat; $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
    $g.DrawString($text, $f, $white, (New-Object Drawing.RectangleF 0, 6, $S, $S), $sf)
}
function Bubble($g, $col, $x, $y, $w, $h) {
    $b = New-Object Drawing.SolidBrush $col
    $g.FillEllipse($b, $x, $y, $w, $h)
    $pts = @((New-Object Drawing.PointF ($x + $w * 0.22), ($y + $h * 0.8)), (New-Object Drawing.PointF ($x + $w * 0.12), ($y + $h * 1.18)), (New-Object Drawing.PointF ($x + $w * 0.5), ($y + $h * 0.95)))
    $g.FillPolygon($b, [Drawing.PointF[]]$pts)
}
function Envelope($g, $fill, $line, $x, $y, $w, $h) {
    $b = New-Object Drawing.SolidBrush $fill
    $g.FillRectangle($b, $x, $y, $w, $h)
    $pen = New-Object Drawing.Pen $line, 9; $pen.LineJoin = 'Round'
    $g.DrawLines($pen, [Drawing.PointF[]]@((New-Object Drawing.PointF $x, $y), (New-Object Drawing.PointF ($x + $w / 2), ($y + $h * 0.58)), (New-Object Drawing.PointF ($x + $w), $y)))
}

# Messenger: blue-violet bubble with a bolt
$t = New-Tile (C 0 132 255) (C 160 51 255); Bubble $t[1] ([Drawing.Color]::White) 46 46 164 150
$bolt = @((New-Object Drawing.PointF 98, 148), (New-Object Drawing.PointF 132, 100), (New-Object Drawing.PointF 138, 128), (New-Object Drawing.PointF 168, 112), (New-Object Drawing.PointF 134, 160), (New-Object Drawing.PointF 128, 134))
$t[1].FillPolygon((New-Object Drawing.SolidBrush (C 70 100 255)), [Drawing.PointF[]]$bolt); Save $t 'messenger.com'
# Google Chat: green bubble
$t = New-Tile (C 0 172 71) (C 0 140 60); Bubble $t[1] ([Drawing.Color]::White) 52 52 152 140; Save $t 'chat.google.com'
# Skype: cyan with S
$t = New-Tile (C 0 175 240) (C 0 120 212); Letter $t[1] 'S' 170; Save $t 'web.skype.com'
# Outlook (both hosts): blue envelope
foreach ($h in 'outlook.live.com', 'outlook.office.com') { $t = New-Tile (C 20 140 235) (C 0 90 180); Envelope $t[1] ([Drawing.Color]::White) (C 0 105 200) 48 76 160 110; Save $t $h }
# iCloud Mail: light blue envelope
$t = New-Tile (C 90 190 255) (C 30 130 245); Envelope $t[1] ([Drawing.Color]::White) (C 30 130 245) 48 76 160 110; Save $t 'icloud.com'
# Tuta: red tile, T
$t = New-Tile (C 170 25 40) (C 120 10 25); Letter $t[1] 'T' 170; Save $t 'app.tuta.com'
# Google Calendar: white tile, blue top, 31
$t = New-Tile (C 255 255 255) (C 240 244 255); $t[1].FillRectangle((New-Object Drawing.SolidBrush (C 66 133 244)), 40, 40, 176, 54)
$f = New-Object Drawing.Font 'Segoe UI', 96, ([Drawing.FontStyle]::Bold), ([Drawing.GraphicsUnit]::Pixel); $sf = New-Object Drawing.StringFormat; $sf.Alignment = 'Center'; $sf.LineAlignment = 'Center'
$t[1].DrawString('31', $f, (New-Object Drawing.SolidBrush (C 66 133 244)), (New-Object Drawing.RectangleF 0, 96, $S, 130), $sf); Save $t 'calendar.google.com'
# Google Sheets: green with grid
$t = New-Tile (C 15 157 88) (C 10 125 70); $pen = New-Object Drawing.Pen ([Drawing.Color]::White), 12
$t[1].DrawRectangle($pen, 62, 56, 132, 144); $t[1].DrawLine($pen, 62, 104, 194, 104); $t[1].DrawLine($pen, 62, 152, 194, 152); $t[1].DrawLine($pen, 128, 56, 128, 200); Save $t 'sheets.google.com'
# Google Keep: yellow with note
$t = New-Tile (C 251 188 4) (C 240 160 0); $t[1].FillRectangle($white, 70, 56, 116, 144); $t[1].FillEllipse((New-Object Drawing.SolidBrush (C 251 188 4)), 104, 92, 48, 48); Save $t 'keep.google.com'
# Google Contacts: blue person
$t = New-Tile (C 66 133 244) (C 40 100 215); $t[1].FillEllipse($white, 94, 52, 68, 68); $t[1].FillPie($white, 62, 120, 132, 130, 180, 180); Save $t 'contacts.google.com'
# Microsoft 365: four squares
$t = New-Tile (C 235 80 20) (C 200 50 0); foreach ($q in @(@(62, 62), @(134, 62), @(62, 134), @(134, 134))) { $t[1].FillRectangle($white, $q[0], $q[1], 60, 60) }; Save $t 'microsoft365.com'
# Trello: blue board with two columns
$t = New-Tile (C 0 121 191) (C 0 90 160); foreach ($cx in @(@(66, 56, 54, 130), @(136, 56, 54, 84))) { $p = New-Object Drawing.Drawing2D.GraphicsPath; $p.AddRectangle((New-Object Drawing.Rectangle $cx[0], $cx[1], $cx[2], $cx[3])); $t[1].FillPath($white, $p) }; Save $t 'trello.com'
# Google Analytics: orange bars
$t = New-Tile (C 249 171 0) (C 230 120 0); $t[1].FillRectangle($white, 150, 54, 46, 148); $t[1].FillRectangle($white, 104, 100, 40, 102); $t[1].FillEllipse($white, 60, 154, 44, 44); Save $t 'analytics.google.com'
# DeepSeek: blue bubble with dot
$t = New-Tile (C 77 107 254) (C 50 80 230); Bubble $t[1] ([Drawing.Color]::White) 50 56 156 130; $t[1].FillEllipse((New-Object Drawing.SolidBrush (C 77 107 254)), 112, 108, 32, 32); Save $t 'chat.deepseek.com'
# Anghami: purple with play
$t = New-Tile (C 130 50 220) (C 90 20 180); $t[1].FillPolygon($white, [Drawing.PointF[]]@((New-Object Drawing.PointF 98, 70), (New-Object Drawing.PointF 98, 186), (New-Object Drawing.PointF 196, 128))); Save $t 'play.anghami.com'
# Duolingo: green with two eyes
$t = New-Tile (C 88 204 2) (C 60 170 0); $t[1].FillEllipse($white, 62, 78, 62, 82); $t[1].FillEllipse($white, 132, 78, 62, 82)
$t[1].FillEllipse((New-Object Drawing.SolidBrush (C 40 40 40)), 92, 112, 24, 32); $t[1].FillEllipse((New-Object Drawing.SolidBrush (C 40 40 40)), 140, 112, 24, 32); Save $t 'duolingo.com'
Get-ChildItem $out | Select-Object Name, Length
