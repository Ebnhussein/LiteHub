using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

class DownloadItem
{
    public string Account, Path, State = "Downloading";
    public CoreWebView2DownloadOperation Op;
    public DateTime Time = DateTime.Now;
}

// Settings screen: General, Personalization, Notifications, Downloads, one page per account, About.
class SettingsPanel : Panel
{
    static readonly int[] Swatches = { 0x9D8FE0, 0xFFB900, 0xF7630C, 0xDA3B01, 0xE74856, 0x881798, 0x6B69D6, 0x0078D7,
                                       0x0063B1, 0x00B7C3, 0x00B294, 0x10893E, 0x498205, 0x107C10, 0x7E735F, 0x767676 };
    readonly MainForm m;
    readonly Panel nav = new Panel();
    readonly FlowLayoutPanel content = new FlowLayoutPanel();
    string page;
    bool reorder;
    int builtWidth;
    FlowLayoutPanel perf;
    readonly Timer perfTimer = new Timer();

    protected override void Dispose(bool disposing) { if (disposing) perfTimer.Dispose(); base.Dispose(disposing); }

    public SettingsPanel(MainForm m, string page)
    {
        perfTimer.Interval = 4000; perfTimer.Tick += delegate { FillPerf(); };
        this.m = m; this.page = page;
        Dock = DockStyle.Fill; BackColor = Pal.Page;
        nav.Dock = DockStyle.Left; nav.Width = 210; nav.BackColor = Pal.Page; nav.AutoScroll = true;
        content.Dock = DockStyle.Fill; content.FlowDirection = FlowDirection.TopDown; content.WrapContents = false;
        content.AutoScroll = true; content.Padding = new Padding(40, 24, 20, 20); content.BackColor = Pal.Page;
        Controls.Add(content);
        Controls.Add(nav);
        Ui.DarkScroll(content); Ui.DarkScroll(nav);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width > 400 && Math.Abs(Width - builtWidth) > 4) Build();
    }

    int RowWidth { get { return Math.Max(420, Width - 210 - 40 - 20 - SystemInformation.VerticalScrollBarWidth - 10); } }

    void Go(string p) { page = p; Build(); }

    void Build()
    {
        builtWidth = Width;
        BackColor = Pal.Page; nav.BackColor = Pal.Page; content.BackColor = Pal.Page;
        content.SuspendLayout();
        content.Controls.Clear();
        BuildNav();
        AppDef acc = m.Apps.Find(delegate(AppDef a) { return a.Id == page; });
        if (acc != null) AccountPage(acc);
        else if (page == "Personalization") Personalization();
        else if (page == "Notifications") Notifications();
        else if (page == "Downloads") DownloadsPage();
        else if (page == "Performance") PerformancePage();
        else if (page == "Shortcuts") ShortcutsPage();
        else if (page == "About") About();
        else General();
        content.Controls.Add(new Panel { Width = 1, Height = 40 });   // room under the last row so scrolling reaches it
        if (page != "Performance") perfTimer.Stop();
        content.ResumeLayout();
    }

    // ---------- left menu ----------

    Button NavButton(string text, int y, bool selected, EventHandler click)
    {
        Button b = new RoundButton();
        b.Text = Loc.T(text); b.SetBounds(8, y, 194, 38);
        b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Pal.Hover;
        b.TextAlign = ContentAlignment.MiddleLeft; b.Padding = new Padding(12, 0, 0, 0);
        b.BackColor = selected ? Pal.Card : Color.Transparent; b.ForeColor = Pal.Text; b.Font = new Font("Segoe UI", 10f);
        ((RoundButton)b).Bar = selected;
        b.Cursor = Cursors.Hand; b.Click += click;
        nav.Controls.Add(b);
        return b;
    }

    void BuildNav()
    {
        nav.Controls.Clear();
        int y = 8;
        foreach (string p in new string[] { "General", "Personalization", "Notifications", "Downloads", "Performance", "Shortcuts" })
        {
            string key = p;
            NavButton(p, y, page == p, delegate { Go(key); });
            y += 40;
        }
        y += 8;
        Label h = Ui.Text("Accounts", 10f, true, false); h.Location = new Point(14, y + 6); nav.Controls.Add(h);
        Button sort = Ui.Btn("⇅", 40, delegate { reorder = !reorder; Build(); });
        sort.Location = new Point(150, y); sort.Height = 28; sort.BackColor = reorder ? Pal.Card : Pal.Hover; nav.Controls.Add(sort);
        y += 36;
        foreach (AppDef a in m.Apps)
        {
            AppDef acc = a;
            NavButton(a.Name, y, page == a.Id, delegate { Go(acc.Id); }).Width = reorder ? 140 : 194;
            if (reorder)
            {
                Button up = Ui.Btn("▲", 24, delegate { m.MoveApp(acc, -1); Build(); }); up.SetBounds(150, y + 5, 24, 28);
                Button dn = Ui.Btn("▼", 24, delegate { m.MoveApp(acc, 1); Build(); }); dn.SetBounds(176, y + 5, 24, 28);
                nav.Controls.Add(up); nav.Controls.Add(dn);
            }
            y += 40;
        }
        y += 8;
        NavButton("About", y, page == "About", delegate { Go("About"); });
    }

    // ---------- pages ----------

    void Title(string t)
    {
        Label l = Ui.Text(t, 20f, true, false); l.Margin = new Padding(0, 0, 0, 14); content.Controls.Add(l);
    }

    void Heading(string t)
    {
        Label l = Ui.Text(t, 11f, true, false); l.Margin = new Padding(0, 14, 0, 8); content.Controls.Add(l);
    }

    void Add(Control c) { content.Controls.Add(c); }

    void General()
    {
        int w = RowWidth;
        Title("General");
        string[] langs = new string[Loc.Codes.Length + 1]; langs[0] = "Automatic";
        for (int li = 0; li < Loc.Codes.Length; li++) langs[li + 1] = Loc.Names[li];
        ComboBox lc = Ui.Combo(langs, Cfg.Lang == "" ? 0 : Math.Max(0, Array.IndexOf(Loc.Codes, Cfg.Lang) + 1), 170, null);
        lc.SelectedIndexChanged += delegate
        {
            Cfg.Lang = lc.SelectedIndex == 0 ? "" : Loc.Codes[lc.SelectedIndex - 1]; Cfg.Save(); Loc.Set(Cfg.Lang);
            MessageBox.Show(Loc.T("Restart LiteHub to apply the language everywhere."), "LiteHub"); Build();
        };
        Add(Ui.Row(w, "Language", "Follows Windows when available", lc));
        Add(Ui.Row(w, "Startup", "Launch LiteHub with Windows so notifications keep working",
            Ui.Combo(new string[] { "Disabled", "Enabled", "Enabled, hidden in tray" }, Cfg.Startup, 170,
                delegate(object s, EventArgs e) { Cfg.Startup = ((ComboBox)s).SelectedIndex; Cfg.Save(); m.ApplyStartup(); })));
        Add(Ui.Row(w, "Close button", "Keep LiteHub running in the tray when you close the window",
            Ui.Switch(Cfg.CloseToTray, delegate(object s, EventArgs e) { Cfg.CloseToTray = ((Toggle)s).Checked; Cfg.Save(); })));
        Add(Ui.Row(w, "Keyboard shortcut", "Quickly open or hide LiteHub from anywhere",
            Ui.Switch(Cfg.HotkeyOn, delegate(object s, EventArgs e) { Cfg.HotkeyOn = ((Toggle)s).Checked; Cfg.Save(); m.RegisterHotkey(); })));
        Label keys = Ui.Text(Cfg.Hotkey, 10f, true, false);
        Panel kr = Ui.Row(w, "Shortcut keys", "Needs Alt, Ctrl, Shift or Win plus one key", null);
        keys.Location = new Point(w - 330, 22); kr.Controls.Add(keys);
        Button rec = Ui.Btn("Record", 80, delegate
        {
            string k = Dialogs.RecordHotkey();
            if (k == null) return;
            string old = Cfg.Hotkey;
            Cfg.Hotkey = k;
            if (!m.RegisterHotkey()) { MessageBox.Show(Loc.F("Another program already uses {0}. Pick a different shortcut.", k), "LiteHub"); Cfg.Hotkey = old; m.RegisterHotkey(); return; }
            Cfg.Save(); keys.Text = k;
        });
        rec.Location = new Point(w - 190, 17); kr.Controls.Add(rec);
        Button reset = Ui.Btn("Reset", 80, delegate { Cfg.Hotkey = "Alt+E"; Cfg.Save(); m.RegisterHotkey(); keys.Text = Cfg.Hotkey; });
        reset.Location = new Point(w - 100, 17); kr.Controls.Add(reset);
        Add(kr);
        Heading("Privacy");
        Toggle pass = null;
        pass = Ui.Switch(Cfg.HasPass, delegate
        {
            if (pass.Checked)
            {
                string p1 = Dialogs.Ask("Passcode", "New passcode", "", true);
                string p2 = p1 == null ? null : Dialogs.Ask("Passcode", "Type it again", "", true);
                if (p1 != null && p1.Length > 0 && p1 == p2) { Cfg.SetPass(p1); Cfg.Save(); }
            }
            else
            {
                string cur = Dialogs.Ask("Passcode", "Current passcode", "", true);
                if (cur != null && Cfg.CheckPass(cur)) { Cfg.SetPass(null); Cfg.Save(); }
            }
            pass.Checked = Cfg.HasPass;
        });
        Add(Ui.Row(w, "Passcode", "Ask for a passcode each time LiteHub is launched", pass));
    }

    void Personalization()
    {
        int w = RowWidth;
        Title("Personalization");
        Add(Ui.Row(w, "Theme", "System follows Windows",
            Ui.Combo(new string[] { "System", "Light", "Dark" }, Cfg.Theme, 150,
                delegate(object s, EventArgs e) { Cfg.Theme = ((ComboBox)s).SelectedIndex; Cfg.Save(); m.ApplyTheme(); Build(); })));
        Add(Ui.Row(w, "App layout", "Account icons on the top bar or on the left",
            Ui.Combo(new string[] { "Top navigation", "Left navigation" }, Cfg.LeftNav ? 1 : 0, 170,
                delegate(object s, EventArgs e) { Cfg.LeftNav = ((ComboBox)s).SelectedIndex == 1; Cfg.Save(); m.ApplyLayout(); })));
        Add(Ui.Row(w, "Colorful menus", "Show the selected account's color on the top bar",
            Ui.Switch(Cfg.Colorful, delegate(object s, EventArgs e) { Cfg.Colorful = ((Toggle)s).Checked; Cfg.Save(); m.ApplyTheme(); })));
    }

    void Notifications()
    {
        int w = RowWidth;
        Title("Notifications");
        Add(Ui.Row(w, "Receive notifications", "Allow accounts to show notifications for new messages",
            Ui.Switch(Cfg.Notify, delegate(object s, EventArgs e) { Cfg.Notify = ((Toggle)s).Checked; Cfg.Save(); })));
        bool dnd = Cfg.DndActive; int off = dnd ? 1 : 0;
        int[] mins = { 0, 30, 60, 120, -1 };
        List<string> dn = new List<string>();
        if (dnd) dn.Add(Loc.F("Until {0}", new DateTime(Cfg.DndUntil).ToString("HH:mm")));
        dn.AddRange(new string[] { "Off", "30 minutes", "1 hour", "2 hours", "Until tomorrow 8:00" });
        Add(Ui.Row(w, "Do not disturb", "Mute pop-up notifications for a while (they still reach the Inbox)",
            Ui.Combo(dn.ToArray(), 0, 190, delegate(object s, EventArgs e)
            {
                int k = ((ComboBox)s).SelectedIndex - off;
                if (k < 0) return;
                m.SetDnd(mins[k]); BeginInvoke(new Action(Build));
            })));
        Add(Ui.Row(w, "Remember inbox", "Keep a local copy of notification previews on this PC",
            Ui.Switch(Cfg.KeepInbox, delegate(object s, EventArgs e) { Cfg.KeepInbox = ((Toggle)s).Checked; Cfg.Save(); m.InboxSave(); })));
        Heading("Badges");
        Add(Ui.Row(w, "Account list", "Show a count next to each account icon",
            Ui.Switch(Cfg.BadgeList, delegate(object s, EventArgs e) { Cfg.BadgeList = ((Toggle)s).Checked; Cfg.Save(); m.RefreshIcons(); })));
        Add(Ui.Row(w, "Taskbar", "Show the total count on LiteHub's taskbar icon",
            Ui.Switch(Cfg.BadgeTaskbar, delegate(object s, EventArgs e) { Cfg.BadgeTaskbar = ((Toggle)s).Checked; Cfg.Save(); m.UpdateTaskbar(); })));
    }

    // Folder row shared by the Downloads page and each account page.
    Panel FolderRow(int w, string hint, string current, Action<string> set)
    {
        Toggle t = null;
        TextBox path = Ui.Edit(string.IsNullOrEmpty(current) ? Paths.DefaultDownloads : current, 300);
        path.ReadOnly = true;
        Button browse = Ui.Btn("Browse", 80, null);
        Panel p = Ui.Row(w, "Custom download location", hint, null);
        t = Ui.Switch(!string.IsNullOrEmpty(current), delegate
        {
            if (!t.Checked) { set(""); path.Text = Paths.DefaultDownloads; }
            else if (!PickFolder(path, set)) t.Checked = false;
            path.Visible = browse.Visible = t.Checked;
        });
        t.Location = new Point(w - t.Width - 16, 21); t.Anchor = AnchorStyles.Top | AnchorStyles.Right; p.Controls.Add(t);
        browse.Click += delegate { PickFolder(path, set); };
        path.Location = new Point(w - 16 - 44 - 12 - 80 - 8 - 300, 17); browse.Location = new Point(w - 16 - 44 - 12 - 80, 17);
        path.Visible = browse.Visible = t.Checked;
        p.Controls.Add(path); p.Controls.Add(browse);
        return p;
    }

    static bool PickFolder(TextBox path, Action<string> set)
    {
        using (FolderBrowserDialog d = new FolderBrowserDialog())
        {
            d.SelectedPath = path.Text;
            if (d.ShowDialog() != DialogResult.OK) return false;
            path.Text = d.SelectedPath; set(d.SelectedPath);
            return true;
        }
    }

    void DownloadsPage()
    {
        int w = RowWidth;
        Title("Downloads");
        Add(FolderRow(w, "Choose where downloads are saved", Cfg.DownloadDir, delegate(string p) { Cfg.DownloadDir = p; Cfg.Save(); }));
        Add(Ui.Row(w, "Filter downloads list by account", "Only show downloads of the selected account when opening the list",
            Ui.Switch(Cfg.FilterDl, delegate(object s, EventArgs e) { Cfg.FilterDl = ((Toggle)s).Checked; Cfg.Save(); })));
    }

    void PerformancePage()
    {
        Title("Performance");
        int[] caps = { 0, 1024, 1536, 2048, 3072 };
        Add(Ui.Row(RowWidth, "Memory limit", "Sleep the least recently used accounts when LiteHub uses more than this",
            Ui.Combo(new string[] { "No limit", "1 GB", "1.5 GB", "2 GB", "3 GB" }, Math.Max(0, Array.IndexOf(caps, Cfg.MemCapMB)), 150,
                delegate(object s, EventArgs e) { Cfg.MemCapMB = caps[((ComboBox)s).SelectedIndex]; Cfg.Save(); })));
        perf = new FlowLayoutPanel(); perf.FlowDirection = FlowDirection.TopDown; perf.WrapContents = false; perf.AutoSize = true;
        perf.Margin = new Padding(0); perf.BackColor = Color.Transparent;
        Add(perf);
        FillPerf(); perfTimer.Start();
    }

    async void FillPerf()
    {
        FlowLayoutPanel target = perf;
        MainForm.MemReport r = await m.Memory();
        if (IsDisposed || target != perf || target.IsDisposed) return;
        int w = RowWidth;
        long total = r.Shared; foreach (long v in r.PerApp.Values) total += v;
        target.SuspendLayout();
        target.Controls.Clear();
        target.Controls.Add(Ui.Row(w, Loc.T("Memory used by LiteHub") + ":  " + (total >> 20) + " MB",
            Loc.F("Per-account numbers are estimates; shared browser engine: {0} MB", r.Shared >> 20),
            Ui.Btn("Sleep inactive", 130, delegate { foreach (AppDef a in new List<AppDef>(m.Apps)) m.Sleep(a); FillPerf(); })));
        foreach (AppDef a in m.Apps)
        {
            AppDef acc = a;
            long mem; r.PerApp.TryGetValue(a, out mem);
            string state = a.View == null ? "Not loaded" : a == m.Active ? "Active" : a.Suspended ? "Suspended" : "Running";
            Button sleep = Ui.Btn("Sleep", 80, delegate { m.Sleep(acc); FillPerf(); });
            sleep.Enabled = a.View != null && a != m.Active;
            target.Controls.Add(Ui.Row(w, a.Name, Loc.T(state) + (a.View != null ? "  -  " + (mem >> 20) + " MB" : ""), sleep));
        }
        target.ResumeLayout();
    }

    void AccountPage(AppDef a)
    {
        int w = RowWidth;
        Title("Account settings");
        Add(Ui.Row(w, "Add another account", "Same service, its own login and name",
            Ui.Primary(Ui.Btn("Add", 90, delegate { m.AddAnother(a); }))));
        TextBox name = Ui.Edit(a.Name, 260);
        Action commit = delegate
        {
            string n = name.Text.Trim();
            if (n.Length == 0 || n.IndexOf('|') >= 0 || m.Apps.Exists(delegate(AppDef x) { return x != a && x.Name == n; })) { name.Text = a.Name; return; }
            if (n != a.Name) { a.Name = n; m.AccountChanged(a); Build(); }
        };
        name.Leave += delegate { commit(); };
        name.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; commit(); } };
        string host = "";
        try { host = new Uri(a.Url).Host; } catch (Exception) { }
        Add(Ui.Row(w, "Name", host, name));
        Add(Ui.Row(w, "Account thumbnail", "Service logo, or a colored letter",
            Ui.Combo(new string[] { "Service logo", "Letter" }, a.LetterThumb ? 1 : 0, 150,
                delegate(object s, EventArgs e) { a.LetterThumb = ((ComboBox)s).SelectedIndex == 1; m.AccountChanged(a); })));
        Add(Ui.Row(w, "Platform icon", "Download the icon again from the platform's website",
            Ui.Btn("Refresh", 90, delegate { m.RefreshIcon(a); })));
        Panel colors = new GlassPanel(); colors.Size = new Size(w, 110); colors.BackColor = Pal.Card; colors.Margin = new Padding(0, 0, 0, 4);
        Label cl = Ui.Text("Accent color", 10f, false, false); cl.Location = new Point(16, 10); colors.Controls.Add(cl);
        int i = 0;
        foreach (int sw in Swatches)
        {
            int rgb = sw;
            Button b = new RoundButton();
            b.SetBounds(16 + (i % 8) * 44, 38 + (i / 8) * 36, 36, 28);
            b.BackColor = Color.FromArgb(rgb | unchecked((int)0xFF000000)); b.Cursor = Cursors.Hand;
            b.Font = new Font("Segoe UI Symbol", 10f, FontStyle.Bold); b.ForeColor = Ui.Light(b.BackColor) ? Color.Black : Color.White;
            if (a.Accent == rgb) b.Text = "✓";
            b.Click += delegate { a.Accent = rgb; m.AccountChanged(a); Build(); };
            colors.Controls.Add(b); i++;
        }
        Add(colors);
        Heading("Notifications");
        Toggle notify = Ui.Switch(a.Notify && Cfg.Notify, delegate(object s, EventArgs e) { a.Notify = ((Toggle)s).Checked; m.SaveApps(); });
        notify.Enabled = Cfg.Notify;
        Add(Ui.Row(w, "Receive notifications", Cfg.Notify ? "Show notifications for new messages" : "Turned off for all accounts in Notifications", notify));
        Add(Ui.Row(w, "Badges", "Show this account's count in the account list and the taskbar badge",
            Ui.Switch(a.Badge, delegate(object s, EventArgs e) { a.Badge = ((Toggle)s).Checked; m.SaveApps(); m.RefreshIcons(); m.UpdateTaskbar(); })));
        Heading("Behavior");
        Add(Ui.Row(w, "Keep running in background", "Needed to get notifications while you use another account",
            Ui.Switch(a.KeepAlive, delegate(object s, EventArgs e) { a.KeepAlive = ((Toggle)s).Checked; m.SaveApps(); })));
        Add(Ui.Row(w, "Open links in default browser", "Links from messages open in your normal browser",
            Ui.Switch(a.ExternalLinks, delegate(object s, EventArgs e) { a.ExternalLinks = ((Toggle)s).Checked; m.SaveApps(); })));
        Toggle lk = null;
        lk = Ui.Switch(a.Locked, delegate
        {
            if (lk.Checked)
            {
                if (!Cfg.HasPass)
                {
                    string p1 = Dialogs.Ask("Passcode", "New passcode", "", true);
                    string p2 = p1 == null ? null : Dialogs.Ask("Passcode", "Type it again", "", true);
                    if (p1 != null && p1.Length > 0 && p1 == p2) { Cfg.SetPass(p1); Cfg.Save(); a.Locked = true; a.Unlocked = true; }
                }
                else { a.Locked = true; a.Unlocked = true; }
            }
            else
            {
                string cur = Dialogs.Ask("Passcode", "Current passcode", "", true);
                if (cur != null && Cfg.CheckPass(cur)) a.Locked = false;
            }
            m.SaveApps(); m.RefreshIcons(); lk.Checked = a.Locked;
        });
        Add(Ui.Row(w, "Lock with passcode", "Ask for the passcode before opening this account (same passcode as Privacy)", lk));
        Heading("Downloads");
        Add(FolderRow(w, "Choose where this account's downloads are saved", a.DownloadDir, delegate(string p) { a.DownloadDir = p; m.SaveApps(); }));
        Heading("Manage account");
        Button rm = Ui.Btn("Remove", 90, delegate { m.RemoveApp(a); });
        rm.BackColor = Color.FromArgb(120, 40, 40); rm.ForeColor = Color.White;
        Panel rr = Ui.Row(w, "Remove account", "Its saved login stays on disk until you delete the profile folder", rm);
        rr.BackColor = Pal.Dark ? Color.FromArgb(60, 35, 35) : Color.FromArgb(250, 225, 225);
        Add(rr);
    }

    void ShortcutsPage()
    {
        int w = RowWidth;
        Title("Shortcuts");
        string[][] rows = {
            new string[] { "Ctrl+K", "Search accounts and actions" },
            new string[] { "Ctrl+1 ... Ctrl+9", "Jump to account 1-9" },
            new string[] { "Ctrl+Tab / Ctrl+Shift+Tab", "Next / previous account" },
            new string[] { Cfg.Hotkey, "Open or hide LiteHub" },
            new string[] { "Esc", "Close the search or a pop-up" } };
        foreach (string[] r in rows)
        {
            Label k = Ui.Text(r[0], 10f, true, false); k.AutoSize = false; k.Size = new Size(300, 26); k.TextAlign = ContentAlignment.MiddleRight;
            Add(Ui.Row(w, r[1], null, k));
        }
    }

    async void CheckUpdate(Button b)
    {
        b.Enabled = false;
        try
        {
            string url = Cfg.UpdateUrl; Uri u;
            if (url.Length == 0 || !Uri.TryCreate(url, UriKind.Absolute, out u) || (u.Scheme != "http" && u.Scheme != "https"))
            { MessageBox.Show(Loc.T("No update address is set. Add a line UpdateUrl=... to settings.txt."), "LiteHub"); return; }
            string json = await System.Threading.Tasks.Task.Run<string>(new Func<string>(delegate
            {
                using (System.Net.WebClient c = new System.Net.WebClient()) { c.Headers[System.Net.HttpRequestHeader.UserAgent] = "LiteHub"; return c.DownloadString(url); }
            }));
            System.Text.RegularExpressions.Match tag = System.Text.RegularExpressions.Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
            System.Text.RegularExpressions.Match link = System.Text.RegularExpressions.Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
            Version cur = new Version(Application.ProductVersion), latest;
            if (!tag.Success || !Version.TryParse(tag.Groups[1].Value.TrimStart('v', 'V'), out latest)) throw new FormatException();
            if (latest > cur)
            {
                if (MessageBox.Show(Loc.F("Version {0} is available. Open the download page?", latest), "LiteHub", MessageBoxButtons.YesNo) == DialogResult.Yes
                    && link.Success && (link.Groups[1].Value.StartsWith("https://") || link.Groups[1].Value.StartsWith("http://")))
                    Process.Start(link.Groups[1].Value);
            }
            else MessageBox.Show(Loc.F("You have the latest version ({0}).", cur), "LiteHub");
        }
        catch (Exception) { MessageBox.Show(Loc.T("Could not check for updates."), "LiteHub"); }
        finally { if (!b.IsDisposed) b.Enabled = true; }
    }

    void About()
    {
        Title("About");
        PictureBox logo = new PictureBox(); logo.Size = new Size(72, 72); logo.SizeMode = PictureBoxSizeMode.Zoom;
        logo.Image = new Icon(MainForm.AppIcon, 48, 48).ToBitmap(); logo.BackColor = Color.Transparent; logo.Margin = new Padding(0, 0, 0, 8);
        Add(logo);
        Add(Ui.Text("LiteHub  " + Application.ProductVersion, 14f, true, false));
        Label l = Ui.Text("All your web accounts in one light window.\nEach account loads only when you open it and is freed when you stop using it.\n\nNo ads. No telemetry. Your logins stay on this PC.\nOpen source, MIT license.", 10.5f, false, true);
        l.Margin = new Padding(0, 10, 0, 14);
        Add(l);
        Add(Ui.Row(RowWidth, "Check for updates", null, Ui.Btn("Check", 90, delegate(object s, EventArgs e) { CheckUpdate((Button)s); })));
    }
}

// "Select a service" screen.
class AddAccountPanel : Panel
{
    readonly MainForm m;
    readonly FlowLayoutPanel cards = new FlowLayoutPanel();   // one heading + one wrapping row per category
    readonly TextBox search;
    FlowLayoutPanel row;
    int builtWidth;

    public AddAccountPanel(MainForm m)
    {
        this.m = m;
        Dock = DockStyle.Fill; BackColor = Pal.Page;
        Panel left = new Panel(); left.Dock = DockStyle.Left; left.Width = 320; left.BackColor = Pal.Page;
        Label t = Ui.Text("Select a service", 16f, true, false); t.Location = new Point(28, 26);
        search = Ui.Edit("", 260); search.Location = new Point(28, 74); Ui.Cue(search, "Search services");
        Label hint = Ui.Text("You can add the same service many times\n(two WhatsApps, three Gmails...). Each one has\nits own login and can run at the same time.", 9f, false, true);
        hint.Location = new Point(28, 112);
        left.Controls.AddRange(new Control[] { t, search, hint });
        cards.Dock = DockStyle.Fill; cards.AutoScroll = true; cards.Padding = new Padding(10, 14, 10, 10); cards.BackColor = Pal.Page;
        cards.FlowDirection = FlowDirection.TopDown; cards.WrapContents = false;
        Controls.Add(cards);
        Controls.Add(left);
        Ui.DarkScroll(cards);
        search.TextChanged += delegate { Fill(); };
        cards.Resize += delegate { if (Math.Abs(cards.ClientSize.Width - builtWidth) > 40) Fill(); };
        Fill();
    }

    int Added(Service s)
    {
        Uri su;
        if (s.IsTemplate || !Uri.TryCreate(s.Url, UriKind.Absolute, out su)) return 0;
        return m.Apps.FindAll(delegate(AppDef a) { Uri u; return Uri.TryCreate(a.Url, UriKind.Absolute, out u) && u.Host == su.Host; }).Count;
    }

    Panel Card(Service s, Action add)
    {
        int n = Added(s);
        Panel p = new GlassPanel(); p.Size = new Size(290, 100); p.BackColor = Pal.Card; p.Margin = new Padding(8);
        PictureBox dot = Ui.Dot(s.Color, s.Name, 36); dot.Location = new Point(14, 14); p.Controls.Add(dot);
        Label name = Ui.Text(s.Name, 11f, true, false); name.Location = new Point(60, 12); name.MaximumSize = new Size(220, 0); p.Controls.Add(name);
        string host = s.IsTemplate ? Loc.T("your own address") : new Uri(s.Url).Host;
        Label sub = Ui.Text(n > 0 ? host + "  •  " + Loc.F("{0} added", n) : host, 8.5f, false, true); sub.Location = new Point(60, 38); p.Controls.Add(sub);
        Button b = Ui.Primary(Ui.Btn(n > 0 ? "Add another  >" : "Add account  >", 130, delegate { add(); }));
        b.Location = new Point(146, 62); p.Controls.Add(b);
        return p;
    }

    void Head(string text)
    {
        Label l = Ui.Text(text, 11f, true, false);
        l.Margin = new Padding(8, 12, 0, 2);
        cards.Controls.Add(l);
        row = new FlowLayoutPanel();
        row.AutoSize = true; row.AutoSizeMode = AutoSizeMode.GrowAndShrink; row.WrapContents = true;
        row.MaximumSize = new Size(Math.Max(320, cards.ClientSize.Width - 40), 0);
        row.Margin = new Padding(0); row.BackColor = Pal.Page;
        cards.Controls.Add(row);
    }

    void AddCard(Service s) { Service svc = s; row.Controls.Add(Card(s, delegate { m.AddFromService(svc); })); }

    void Fill()
    {
        builtWidth = cards.ClientSize.Width;
        cards.SuspendLayout();
        cards.Controls.Clear();
        string q = search.Text.Trim().ToLower();
        if (q.Length == 0)
        {
            List<Service> sug = new List<Service>();
            foreach (string n in new string[] { "WhatsApp", "Gmail", "Telegram", "Instagram", "Slack", "Discord", "LinkedIn" })
            {
                Service s = Array.Find(Catalog.All, delegate(Service x) { return x.Name == n; });
                if (s != null && Added(s) == 0 && sug.Count < 4) sug.Add(s);
            }
            if (sug.Count > 0) { Head("Suggestions"); foreach (Service s in sug) AddCard(s); }
        }
        string cat = null;
        foreach (Service s in Catalog.All)
        {
            if (q.Length > 0 && s.Name.ToLower().IndexOf(q) < 0 && s.Category.ToLower().IndexOf(q) < 0) continue;
            if (s.Category != cat) { cat = s.Category; Head(cat); }
            AddCard(s);
        }
        if (q.Length == 0 || "custom website".IndexOf(q) >= 0)
        {
            Head("Any other website");
            Service custom = new Service("Other", "Custom website", "https://example.com", 0x7C6EF2, false, false);
            row.Controls.Add(Card(custom, delegate { m.AddCustom(); }));
        }
        cards.ResumeLayout();
    }
}

// List of files downloaded in this session.
class DownloadsPanel : Panel
{
    readonly MainForm m;
    readonly ListView list = new ListView();
    readonly Timer refresh = new Timer();

    public DownloadsPanel(MainForm m)
    {
        this.m = m;
        Dock = DockStyle.Fill; BackColor = Pal.Page;
        Label t = Ui.Text("Downloads", 16f, true, false); t.Location = new Point(28, 20); Controls.Add(t);
        list.Location = new Point(28, 64); list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        list.View = View.Details; list.FullRowSelect = true; list.BorderStyle = BorderStyle.None; list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        list.BackColor = Pal.Card; list.ForeColor = Pal.Text; list.Font = new Font("Segoe UI", 10f);
        list.Columns.Add(Loc.T("File"), 380); list.Columns.Add(Loc.T("Account"), 140); list.Columns.Add(Loc.T("Status"), 140);
        list.OwnerDraw = true;   // so the header follows the theme
        list.DrawColumnHeader += delegate(object s, DrawListViewColumnHeaderEventArgs e)
        {
            using (Brush b = new SolidBrush(Pal.Hover)) e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height),
                Pal.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        };
        list.DrawItem += delegate(object s, DrawListViewItemEventArgs e) { e.DrawDefault = true; };
        list.DrawSubItem += delegate(object s, DrawListViewSubItemEventArgs e) { e.DrawDefault = true; };
        list.DoubleClick += delegate { Open(false); };
        ContextMenuStrip cm = new AutoMenu();
        ToolStripMenuItem open = new ToolStripMenuItem(Loc.T("Open")); open.Click += delegate { Open(false); };
        ToolStripMenuItem show = new ToolStripMenuItem(Loc.T("Show in folder")); show.Click += delegate { Open(true); };
        cm.Items.Add(open); cm.Items.Add(show);
        list.ContextMenuStrip = cm;
        Controls.Add(list);
        Ui.DarkScroll(list);
        Button folder = Ui.Btn("Open downloads folder", 170, delegate
        {
            string d = string.IsNullOrEmpty(Cfg.DownloadDir) ? Paths.DefaultDownloads : Cfg.DownloadDir;
            if (Directory.Exists(d)) Process.Start(d);
        });
        folder.Location = new Point(28, 0); folder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; Controls.Add(folder);
        Button clear = Ui.Btn("Clear list", 100, delegate { m.Downloads.Clear(); Fill(); });
        clear.Location = new Point(210, 0); clear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; Controls.Add(clear);
        Resize += delegate
        {
            list.Size = new Size(Width - 56, Height - 64 - 56);
            folder.Top = clear.Top = Height - 44;
            list.Columns[2].Width = Math.Max(140, list.ClientSize.Width - 380 - 140);   // last column fills the header
        };
        refresh.Interval = 1000; refresh.Tick += delegate { Fill(); }; refresh.Start();
        Fill();
    }

    protected override void Dispose(bool disposing) { if (disposing) refresh.Dispose(); base.Dispose(disposing); }

    List<DownloadItem> Shown()
    {
        List<DownloadItem> r = new List<DownloadItem>();
        foreach (DownloadItem d in m.Downloads)
            if (!Cfg.FilterDl || m.Active == null || d.Account == m.Active.Name) r.Add(d);
        r.Reverse();
        return r;
    }

    void Fill()
    {
        int sel = list.SelectedIndices.Count > 0 ? list.SelectedIndices[0] : -1;
        list.BeginUpdate();
        list.Items.Clear();
        foreach (DownloadItem d in Shown())
        {
            string status = d.State;
            try { if (d.State == "Downloading" && d.Op != null && d.Op.TotalBytesToReceive > 0) status = (d.Op.BytesReceived * 100 / (long)d.Op.TotalBytesToReceive) + "%"; }
            catch (Exception) { }
            ListViewItem it = new ListViewItem(new string[] { Path.GetFileName(d.Path), d.Account, Loc.T(status) });
            it.Tag = d;
            list.Items.Add(it);
        }
        if (sel >= 0 && sel < list.Items.Count) list.Items[sel].Selected = true;
        list.EndUpdate();
    }

    void Open(bool folder)
    {
        if (list.SelectedItems.Count == 0) return;
        DownloadItem d = (DownloadItem)list.SelectedItems[0].Tag;
        if (!File.Exists(d.Path)) return;
        if (folder) Process.Start("explorer.exe", "/select,\"" + d.Path + "\"");
        else if (d.State == "Done") Process.Start(d.Path);
    }
}

// Notifications collected from every account, newest first. Click one to jump to its account.
class InboxPanel : Panel
{
    readonly MainForm m;
    readonly FlowLayoutPanel list = new FlowLayoutPanel();
    readonly Button markAll, clear;
    readonly TextBox q;
    int builtWidth;

    public InboxPanel(MainForm m)
    {
        this.m = m;
        Dock = DockStyle.Fill; BackColor = Pal.Page;
        Label t = Ui.Text("Inbox", 16f, true, false); t.Location = new Point(28, 20); Controls.Add(t);
        markAll = Ui.Btn("Mark all read", 130, delegate { m.InboxMarkAllRead(); });
        clear = Ui.Btn("Clear", 80, delegate { m.InboxClear(); });
        Controls.Add(markAll); Controls.Add(clear);
        q = Ui.Edit("", 200); Ui.Cue(q, "Search"); Controls.Add(q); q.TextChanged += delegate { Fill(); };
        list.FlowDirection = FlowDirection.TopDown; list.WrapContents = false; list.AutoScroll = true; list.BackColor = Pal.Page;
        Controls.Add(list); Ui.DarkScroll(list);
        m.InboxChanged = delegate { Fill(); };
        Layout_();
        Fill();
    }

    protected override void Dispose(bool disposing) { if (disposing && m.InboxChanged != null) m.InboxChanged = null; base.Dispose(disposing); }

    void Layout_()
    {
        markAll.Location = new Point(Width - 28 - 80 - 8 - 130, 18); clear.Location = new Point(Width - 28 - 80, 18);
        q.Location = new Point(Math.Max(200, Width - 28 - 80 - 8 - 130 - 32 - 200), 24);
        list.SetBounds(28, 64, Math.Max(100, Width - 56), Math.Max(100, Height - 64 - 12));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (list == null || Width < 400) return;
        Layout_();
        if (Math.Abs(Width - builtWidth) > 40) Fill();
    }

    static string When(DateTime t)
    {
        return t.Date == DateTime.Today ? t.ToString("HH:mm") : t.ToString("dd/MM HH:mm");
    }

    void Fill()
    {
        if (list.IsDisposed) return;
        builtWidth = Width;
        int w = Math.Max(320, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        list.SuspendLayout();
        list.Controls.Clear();
        string f = q.Text.Trim().ToLower();
        List<MainForm.InboxItem> shown = m.Inbox.FindAll(delegate(MainForm.InboxItem i)
        {
            return m.InWs(i.App) && (f.Length == 0 || (i.App.Name + " " + i.Title + " " + i.Body).ToLower().IndexOf(f) >= 0);
        });
        if (shown.Count == 0)
        {
            Label e = Ui.Text("No notifications yet. New messages from all your accounts will appear here.", 10.5f, false, true);
            e.Margin = new Padding(4, 20, 0, 0); list.Controls.Add(e);
        }
        foreach (MainForm.InboxItem it in shown)
        {
            MainForm.InboxItem item = it;
            Panel p = new GlassPanel(); p.Size = new Size(w, 80); p.BackColor = Pal.Card; p.Margin = new Padding(0, 0, 0, 6); p.Cursor = Cursors.Hand;
            PictureBox dot = Ui.Dot(it.App.AccentColor.ToArgb() & 0xFFFFFF, it.App.Name, 38); dot.Location = new Point(16, 20); p.Controls.Add(dot);
            Label name = Ui.Text(it.App.Name, 10f, true, false); name.Location = new Point(66, 10); p.Controls.Add(name);
            Label time = Ui.Text(When(it.Time), 8.5f, false, true); time.Location = new Point(w - 90, 12); p.Controls.Add(time);
            Label title = Ui.Text(it.Title, it.Read ? 9.5f : 9.5f, !it.Read, false); title.AutoSize = false; title.AutoEllipsis = true;
            title.SetBounds(66, 32, w - 110, 20); p.Controls.Add(title);
            Label body = Ui.Text(it.Body.Replace("\r", " ").Replace("\n", " "), 9f, false, true); body.AutoSize = false; body.AutoEllipsis = true;
            body.SetBounds(66, 54, w - 110, 18); p.Controls.Add(body);
            if (!it.Read) { Label u = Ui.Text("●", 9f, false, false); u.ForeColor = Pal.Brand; u.Location = new Point(w - 26, 34); p.Controls.Add(u); u.Click += delegate { m.OpenInbox(item); }; }
            foreach (Control c in p.Controls) { c.Cursor = Cursors.Hand; c.Click += delegate { m.OpenInbox(item); }; }
            p.Click += delegate { m.OpenInbox(item); };
            list.Controls.Add(p);
        }
        list.ResumeLayout();
    }
}