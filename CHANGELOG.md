# Changelog

## 1.2.0
- Workspaces stay apart: the quick switcher (Ctrl+K), Ctrl+1..9, the tray menu and the Inbox only show the accounts of the workspace you are in.
- Workspace password: set one from the workspace menu; leaving a protected workspace (to another one, or moving its accounts out) asks for it. Inside a protected workspace, other workspaces raise no alerts.

## 1.1.1
- One message, one alert: the bare "new messages" alert no longer shows before the detailed one.
- Settings > Notifications > Hide message details: alerts (and the Inbox) say only which account has a new message, never the sender or the text.

## 1.1.0
- Updates: LiteHub looks for a new version at start-up and every few hours and asks before updating; "Update now" downloads the installer, checks its SHA256 and installs over the current copy (Settings > About > Automatic updates turns it off).
- Setup lets you choose the install folder (Browse...).

## 1.0.2
- README download button always gets the newest installer (`LiteHub-Setup.exe` is now attached to every release).

## 1.0.1
- WhatsApp links from the browser (whatsapp://, wa.me, api.whatsapp.com) open in LiteHub; pick the account each time or set a default (Settings > General).
- First start asks what it needs: start with Windows and handling WhatsApp links.
- Pop-up menus (link chooser, account and workspace menus) instead of plain menus.
- Tray menu lists your accounts with their icons.
- Inbox entries disappear once you open that account.
- Portable zip (data kept next to LiteHub.exe) next to the installer.
- Settings > About: Email contact.

## 1.0.0
First public release: one light window for all your web accounts. Side bar with names or icons, workspaces, unified inbox, notification cards,
quick switcher (Ctrl+K), split view, import from Chrome/Edge/Brave/Vivaldi/Opera or a bookmarks file, drag and drop files to send,
memory meter and limit, do-not-disturb, passcode (launch and per account), 8 languages, iOS-style design system, logs and diagnostics, installer.