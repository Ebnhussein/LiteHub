# Builds docs\design-system.html from the real color values in Ds.cs and Core.cs (Pal.Set), so the page cannot drift from the code.
# Run: powershell -ExecutionPolicy Bypass -File tools\make-design-doc.ps1
$root = Split-Path $PSScriptRoot -Parent
$ds = [IO.File]::ReadAllText("$root\Ds.cs"); $core = [IO.File]::ReadAllText("$root\Core.cs")

function Hex($r, $g, $b) { '#{0:X2}{1:X2}{2:X2}' -f $r, $g, $b }
function Lum($r, $g, $b) {
    $v = @($r, $g, $b) | % { $s = $_ / 255.0; if ($s -le 0.03928) { $s / 12.92 } else { [math]::Pow(($s + 0.055) / 1.055, 2.4) } }
    0.2126 * $v[0] + 0.7152 * $v[1] + 0.0722 * $v[2]
}
function Ratio($a, $b) { $la = Lum @a; $lb = Lum @b; ([math]::Max($la, $lb) + 0.05) / ([math]::Min($la, $lb) + 0.05) }

# brand + status from Ds.cs
$tok = @{}
foreach ($m in [regex]::Matches($ds, '(Brand2?|Success|Danger|Warning) = Color\.FromArgb\((\d+), (\d+), (\d+)\)')) { $tok[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value, [int]$m.Groups[4].Value) }

# surfaces from Pal.Set (dark block, then light block)
$set = [regex]::Match($core, 'public static void Set\(bool dark\)(.*?)public static Color Blend', 'Singleline').Groups[1].Value
$parts = $set -split '\belse\b'
function ParseBlock($txt) {
    $h = @{}
    foreach ($m in [regex]::Matches($txt, '(Bar|Page|Card|Text|Sub|Hover|Border) = (?:Color\.White|Color\.FromArgb\((\d+), (\d+), (\d+)\))')) {
        if ($m.Groups[2].Success) { $h[$m.Groups[1].Value] = @([int]$m.Groups[2].Value, [int]$m.Groups[3].Value, [int]$m.Groups[4].Value) } else { $h[$m.Groups[1].Value] = @(255, 255, 255) }
    }
    $h
}
$dark = ParseBlock $parts[0]; $light = ParseBlock $parts[1]
$darkInk = @(160, 155, 248); $lightInk = $tok['Brand']

function Sw($name, $c, $note) { "<div class=sw><i style='background:$(Hex @c)'></i><b>$name</b><code>$(Hex @c)</code><span>$note</span></div>" }
function Chip($label, $fg, $bg, $min) {
    $r = Ratio $fg $bg; $ok = $r -ge $min
    "<tr><td>$label</td><td><span class=pill style='background:$(Hex @bg);color:$(Hex @fg)'>Aa</span></td><td>$([math]::Round($r,1)) : 1</td><td>$min : 1</td><td class='$(if ($ok) {'ok'} else {'bad'})'>$(if ($ok) {'pass'} else {'FAIL'})</td></tr>"
}
$white = @(255, 255, 255)
$rows = ''
foreach ($t in @(@('Dark', $dark, $darkInk), @('Light', $light, $lightInk))) {
    $n = $t[0]; $p = $t[1]; $ink = $t[2]
    $rows += Chip "$n  text on page" $p.Text $p.Page 7
    $rows += Chip "$n  text on card" $p.Text $p.Card 7
    $rows += Chip "$n  secondary text on page" $p.Sub $p.Page 4.5
    $rows += Chip "$n  secondary text on card" $p.Sub $p.Card 4.5
    $rows += Chip "$n  secondary text on bar" $p.Sub $p.Bar 4.5
    $rows += Chip "$n  brand ink on page" $ink $p.Page 4.5
    $rows += Chip "$n  brand ink on card" $ink $p.Card 4.5
}
$rows += Chip 'White on Brand (primary button)' $white $tok.Brand 4.5
$rows += Chip 'White on Danger (badge, remove)' $white $tok.Danger 4.5
$rows += Chip 'White on Brand 2 (gradient end, large UI)' $white $tok.Brand2 3

function Surf($name, $p) {
    $s = "<h3>$name</h3><div class=grid>"
    foreach ($k in 'Bar', 'Page', 'Card', 'Hover', 'Border', 'Text', 'Sub') { $s += Sw $k $p[$k] '' }
    $s + '</div>'
}

$css = @'
:root{--bg:#f7f6ff;--card:#fff;--text:#1c1a32;--sub:#605e7d;--line:#cdcbe6;--brand:#5b54e8}
@media (prefers-color-scheme:dark){:root{--bg:#1d1c29;--card:#292838;--text:#fff;--sub:#b0aec4;--line:#484664;--brand:#a09bf8}}
body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 "Segoe UI",system-ui,sans-serif}
main{max-width:980px;margin:0 auto;padding:32px 16px 64px}
h1{font-size:28px;margin:0 0 4px}h2{margin:40px 0 12px;font-size:20px}h3{margin:20px 0 8px;font-size:15px;color:var(--sub)}
p.lead{color:var(--sub);margin:0 0 8px}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:10px}
.sw{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:10px;display:flex;flex-direction:column;gap:2px;font-size:13px}
.sw i{display:block;height:44px;border-radius:8px;border:1px solid var(--line);margin-bottom:6px}
code{font:12px Consolas,monospace;color:var(--sub)}.sw span{color:var(--sub);font-size:12px}
table{border-collapse:collapse;width:100%;background:var(--card);border:1px solid var(--line);border-radius:12px;overflow:hidden;font-size:13px}
td,th{padding:6px 10px;text-align:left;border-bottom:1px solid var(--line)}
.pill{display:inline-block;padding:2px 12px;border-radius:8px;font-weight:600}
.ok{color:#22a559;font-weight:600}.bad{color:#d63a3f;font-weight:700}
.row{display:flex;flex-wrap:wrap;gap:12px;align-items:center}
.btn{border:1px solid var(--line);background:transparent;color:var(--text);padding:7px 16px;border-radius:8px;font:inherit;font-size:13px}
.btn.p{background:linear-gradient(90deg,#5b54e8,#a855f7);color:#fff;border-color:transparent}
.btn.d{background:#d63a3f;color:#fff;border-color:transparent}
.card{background:var(--card);border:1px solid var(--line);border-radius:12px;padding:14px 16px;min-width:260px}
.card small{color:var(--sub);display:block}
.tog{width:44px;height:22px;border-radius:11px;background:#5b54e8;position:relative;display:inline-block}.tog:after{content:"";position:absolute;right:4px;top:5px;width:12px;height:12px;border-radius:6px;background:#fff}
.tile{width:36px;height:36px;border-radius:11px;display:inline-grid;place-items:center;color:#fff;font-weight:700}
.badge{background:#d63a3f;color:#fff;font:700 11px "Segoe UI";border-radius:8px;padding:1px 6px}
.rad{width:84px;height:56px;border:2px solid var(--brand);display:grid;place-items:center;font-size:12px;color:var(--sub)}
.sp{background:var(--brand);height:16px;display:inline-block;border-radius:3px}
ul{color:var(--sub)}
'@

$type = '<table><tr><th>Token</th><th>Size</th><th>Use</th></tr><tr><td>Title</td><td>20 pt</td><td>One per screen</td></tr><tr><td>Heading</td><td>11 pt bold</td><td>Section headings</td></tr><tr><td>Body</td><td>10 pt</td><td>Rows, buttons, lists</td></tr><tr><td>Caption</td><td>8.5 pt</td><td>Hints, timestamps</td></tr><tr><td>Micro</td><td>7 pt bold</td><td>Count badges</td></tr></table><p class=lead>Font: Segoe UI (Segoe UI Semibold for the wordmark, Segoe MDL2 Assets for icon glyphs).</p>'

$html = @"
<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content="width=device-width,initial-scale=1"><title>LiteHub Design System</title>
<style>$css</style><main>
<h1>LiteHub design system</h1><p class=lead>Generated from <code>Ds.cs</code> and <code>Core.cs</code> by <code>tools/make-design-doc.ps1</code>. The code is the source of truth; <code>--selftest</code> fails if the contrast table below stops passing.</p>
<h2>Brand and status</h2><div class=grid>
$(Sw 'Brand' $tok.Brand 'primary actions, selection')$(Sw 'Brand 2' $tok.Brand2 'gradient end only, never text')$(Sw 'Success' $tok.Success 'running, done')$(Sw 'Danger' $tok.Danger 'unread counts, remove')$(Sw 'Warning' $tok.Warning 'reserved for cautions')$(Sw 'Brand ink (dark)' $darkInk 'focus rings, small text on dark')</div>
<h2>Surfaces and text</h2>$(Surf 'Dark' $dark)$(Surf 'Light' $light)
<h2>Contrast (WCAG)</h2><table><tr><th>Pair</th><th></th><th>Ratio</th><th>Need</th><th></th></tr>$rows</table>
<h2>Type</h2>$type
<h2>Shape and space</h2><div class=row><div class=rad style="border-radius:8px">8 control</div><div class=rad style="border-radius:11px">11 tile</div><div class=rad style="border-radius:12px">12 card</div><div class=rad style="border-radius:16px">16 overlay</div></div>
<p class=lead>Spacing scale: <span class=sp style="width:4px"></span> 4 &nbsp;<span class=sp style="width:8px"></span> 8 &nbsp;<span class=sp style="width:12px"></span> 12 &nbsp;<span class=sp style="width:16px"></span> 16 &nbsp;<span class=sp style="width:24px"></span> 24 &nbsp;<span class=sp style="width:32px"></span> 32. Every full-page screen starts 32 from the left and 24 from the top.</p>
<h2>Components</h2><div class=row>
<button class="btn p">Primary</button><button class=btn>Secondary</button><button class="btn d">Remove</button><span class=tog></span>
<span class=tile style="background:#25d366">W</span><span class=tile style="background:#ea4335">G</span><span class=badge>2</span></div>
<div class=row style="margin-top:12px"><div class=card><b>Card title</b><small>Hint text on a card, 8.5 pt secondary color</small></div></div>
<h2>Rules</h2><ul>
<li>One primary (gradient) button per view; everything else is secondary. Danger only for destructive actions.</li>
<li>Brand 2 appears only as the end of the brand gradient; text and thin lines use Brand (light) or Brand ink (dark).</li>
<li>Inputs are recessed: the Page color inside a Card. Cards are rounded 12 with a hairline and a soft top sheen.</li>
<li>Status is never color-only: unread has a count, running has a dot plus the "Running" label in Performance.</li>
<li>New screens use <code>Ds.PageX/PageTop</code>, <code>Ds.Title</code>, <code>Ui.Row</code> cards and <code>Ui.Btn</code>; do not add raw colors or radii.</li>
</ul></main></html>
"@
New-Item -ItemType Directory -Force "$root\docs" | Out-Null
[IO.File]::WriteAllText("$root\docs\design-system.html", $html, (New-Object Text.UTF8Encoding($false)))
"wrote docs\design-system.html"
