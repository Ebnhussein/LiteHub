@echo off
setlocal
cd /d "%~dp0"
if not exist bin mkdir bin
copy /y lib\*.dll bin\ >nul
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /win32icon:assets\logo.ico /out:bin\LiteHub.exe ^
 /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
 /r:lib\Microsoft.Web.WebView2.Core.dll /r:lib\Microsoft.Web.WebView2.WinForms.dll Program.cs Core.cs Controls.cs Panels.cs Loc.cs Ds.cs Import.cs Toast.cs Log.cs AssemblyInfo.cs
if errorlevel 1 exit /b 1
echo Built: %~dp0bin\LiteHub.exe
