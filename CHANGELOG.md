# Changelog

## 1.3.2
- The WhatsApp link handler never remembers another LiteHub copy as the app to give the link back to; uninstall then simply removes the handler.

## 1.3.1
- Start with Windows is re-applied at every start of an installed or portable copy, so it survives an uninstall and reinstall.

## 1.3.0
- Arabic now mirrors the whole window (side bar on the right, rows and dialogs right to left).
- Log lines say when a count-only alert was skipped because the real notification came, or shown because none came.
- Arabic wording fix in the layout setting.

## 1.2.2
- Uninstall also removes the start-with-Windows entry and gives the WhatsApp link back to its previous app.
- Clear messages when the WebView2 runtime is missing or when a portable copy sits on a read-only drive.
- The WhatsApp link chooser never offers accounts of a locked workspace.

## 1.2.1
- Workspace password now guards opening: a protected workspace asks for its password every time it is opened (also after a restart), and "All" and the quick switcher hide it until then. Its accounts raise no alerts while it is locked.

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