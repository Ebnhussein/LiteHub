# Contributing

Thanks for helping. Bugs and ideas: open an issue (please use the bug-report template, or *Settings > About > Report a problem* in the app).

## Build

```
build.bat
bin\LiteHub.exe --selftest        (exit code 0 = built-in checks pass)
```

Only the .NET Framework C# compiler that ships with Windows is needed (C# 5 syntax). WebView2 DLLs are in `lib\`.

## Before a pull request

- `--selftest` must pass. It checks parsers, passcode hashing, the translation table and the colour contrast of the palette.
- Use the design tokens (`Ds.cs`, `Pal` in `Core.cs`, see DESIGN.md); no raw colours, radii or font sizes.
- User-visible text goes through `Loc.T` and gets a row in `Loc.cs` for every language.
- Never test destructive flows (reset, wipe, uninstall) on your real data: set `LITEHUB_DATA` to a throw-away folder.
- Keep logs free of message text, passwords and account names (`Log.Who(account)` gives `site#number`).