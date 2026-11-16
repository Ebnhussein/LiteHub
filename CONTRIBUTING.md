# Contributing

Thanks for helping. Bugs and ideas: open an issue (Settings > About > Report a problem copies the diagnostics for you).

- Build: `build.bat`, then `bin\LiteHub.exe --selftest` (exit code 0 means the built-in checks pass). Needs only what ships with Windows.
- Keep changes small and match the surrounding style (plain C# 5, no extra dependencies).
- New text for the interface goes in `Loc.cs` as one row for all 8 languages.
- Do not log message text or passwords.
- Security problems: email, do not open a public issue (see SECURITY.md).
