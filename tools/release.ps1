# Builds dist\LiteHub-<version>-win-x64.zip (exe, WebView2 DLLs, README, LICENSE), LiteHub-<version>-Setup.exe (installer) and SHA256 files.
$root = Split-Path $PSScriptRoot -Parent
& cmd /c "`"$root\build.bat`""
if ($LASTEXITCODE -ne 0) { throw 'build failed' }
$p = Start-Process "$root\bin\LiteHub.exe" -ArgumentList '--selftest' -Wait -PassThru
if ($p.ExitCode -ne 0) { throw 'selftest failed' }
$ver = (Get-Item "$root\bin\LiteHub.exe").VersionInfo.ProductVersion
$stage = Join-Path $env:TEMP "LiteHub-$ver"; Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $stage | Out-Null
Copy-Item "$root\bin\*.exe", "$root\bin\*.dll" $stage
Copy-Item "$root\README.md", "$root\LICENSE", "$root\THIRD-PARTY-NOTICES.md" $stage
Copy-Item "$root\assets" "$stage\assets" -Recurse
New-Item -ItemType Directory -Force "$root\dist" | Out-Null
$zip = "$root\dist\LiteHub-$ver-win-x64.zip"; Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$stage\*" -DestinationPath $zip
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
"$hash  $(Split-Path $zip -Leaf)" | Set-Content "$zip.sha256" -Encoding ASCII
Remove-Item $stage -Recurse -Force

# Setup exe: a small per-user installer (no admin rights) that carries the zip inside it
$setup = "$root\dist\LiteHub-$ver-Setup.exe"; Remove-Item $setup -Force -ErrorAction SilentlyContinue
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$res = "/resource:$zip,payload.zip"
$ico = "/win32icon:$root\assets\logo.ico"
$out = "/out:$setup"
& $csc /nologo /target:winexe $ico $out $res /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll "$root\installer\Setup.cs" "$root\AssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'installer build failed' }
$shash = (Get-FileHash $setup -Algorithm SHA256).Hash
"$shash  $(Split-Path $setup -Leaf)" | Set-Content "$setup.sha256" -Encoding ASCII

"$zip`nSHA256 $hash"
"$setup`nSHA256 $shash"
