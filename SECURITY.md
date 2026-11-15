# Security policy

## Reporting a vulnerability

Please **do not** open a public GitHub issue for a security problem. Email **ebnhussein404@gmail.com** with
"LiteHub security" in the subject, what you found, and how to reproduce it. You will get an answer within a few days.

## What LiteHub does and does not do

- Logins live in separate browser profiles on your PC (`%LOCALAPPDATA%\LiteHub\profiles`); nothing is uploaded.
- The launch passcode is stored as a salted PBKDF2 hash. "Forgot passcode?" removes it and signs you out of every account.
- LiteHub makes no network requests of its own except fetching each site's icon from that site, and "Check for updates" when you click it.
- Logs contain no message text and no passwords.

## Supported versions

Only the latest release gets fixes.