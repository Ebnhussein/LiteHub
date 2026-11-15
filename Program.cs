using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

// LiteHub: one window, many web accounts. Each account has its own login (profile), is created on
// first click, throttled/suspended when hidden and destroyed when idle (unless keep-alive).
class MainForm : Form
{
    const int SuspendMinutes = 1;        // hidden account is suspended (no CPU) after this
    const int IdleMinutes = 5;           // hidden account is destroyed (no RAM) after this
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    readonly List<AppDef> apps = new List<AppDef>();
    public readonly List<DownloadItem> Downloads = new List<DownloadItem>();
    readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    readonly Panel top = new Panel();
    readonly Panel leftBar = new RailPanel();
    readonly Panel strip = new Panel();
    readonly Panel railHead = new ClearPanel();
    Button wsBtn;
    readonly Panel tab = new Panel();
    readonly Panel tools = new ClearPanel();
    readonly Button[] toolBtns = new Button[5];
    Button gridBtn;
    readonly FlowLayoutPanel icons = new ClearFlow();
    readonly Panel host = new Panel();
    readonly ToolTip tip = new ToolTip();
    readonly Queue<AppDef> warmQueue = new Queue<AppDef>();
    readonly NotifyIcon tray = new NotifyIcon();
    readonly bool background;
    Task<CoreWebView2Environment> envTask;   // one browser process group shared by all accounts
    AppDef active;
    Panel overlay;                           // settings / add account / downloads screen
    string tabTitle;
    bool reallyExit, startMaximized, locked;
    string lastApp;
    System.Threading.EventWaitHandle showEvt;   // a second launch sets this to bring us to front

    public static readonly Icon AppIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

    public List<AppDef> Apps { get { return apps; } }
    public AppDef Active { get { return active; } }

    const string Header =
        "# Name|URL|KeepAlive|ExternalLinks|Accent(RRGGBB)|LetterIcon|Notify|Badge|DownloadFolder|Id\r\n";
    const string DefaultApps =
        "WhatsApp|https://web.whatsapp.com|1|1\r\n" +
        "Gmail|https://mail.google.com|1|0\r\n" +
        "Outlook|https://outlook.live.com/mail|0|0\r\n" +
        "Messenger|https://www.messenger.com|0|1\r\n" +
        "Telegram|https://web.telegram.org/a/|0|1\r\n" +
        "Instagram|https://www.instagram.com/direct/inbox/|0|1\r\n" +
        "LinkedIn|https://www.linkedin.com/messaging/|0|0\r\n" +
        "Zoom|https://app.zoom.us/wc|0|0\r\n" +
        "Slack|https://app.slack.com|0|0\r\n" +
        "Notion|https://www.notion.so|0|0\r\n" +
        "Trello|https://trello.com|0|0\r\n";

    public MainForm(bool background)
    {
        this.background = background;
        locked = Cfg.HasPass && background;
        Text = "LiteHub";
        Icon = AppIcon;
        Size = new Size(1280, 800);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Directory.CreateDirectory(Paths.Icons);
        LoadApps();
        LoadState();
        LoadInbox();

        top.Dock = DockStyle.Top; top.Height = 48;
        leftBar.Dock = DockStyle.Left; leftBar.Width = 64; leftBar.Visible = false;
        host.Dock = DockStyle.Fill;
        host.Resize += delegate { ArrangeSplit(); };
        Controls.Add(host);
        Controls.Add(leftBar);
        strip.Dock = DockStyle.Top; strip.Height = 3;   // brand gradient under the top bar
        strip.Paint += delegate(object s, PaintEventArgs e)
        {
            using (System.Drawing.Drawing2D.LinearGradientBrush gb = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, strip.Width), 3), Pal.Brand, Pal.Brand2, 0f))
                e.Graphics.FillRectangle(gb, 0, 0, strip.Width, 3);
        };
        strip.Resize += delegate { strip.Invalidate(); };
        Controls.Add(strip);
        Controls.Add(top);

        // account icons + add / downloads / settings
        tools.Size = new Size(214, 48);
        toolBtns[0] = ToolButton("\u25A5", "Split view: show several accounts side by side", delegate(object s, EventArgs e) { SplitMenu((Control)s); });
        toolBtns[0].Font = new Font("Segoe UI Symbol", 12f);
        toolBtns[1] = ToolButton("\uE77B", "Add account", delegate { ShowOverlay(new AddAccountPanel(this), "Add account"); });
        toolBtns[2] = ToolButton("\uE896", "Downloads", delegate { ShowOverlay(new DownloadsPanel(this), "Downloads"); });
        toolBtns[3] = ToolButton("\uE713", "Settings", delegate { ShowSettings("General"); });
        toolBtns[4] = ToolButton("", "Inbox", delegate { ShowOverlay(new InboxPanel(this), "Inbox"); });
        foreach (Button b in toolBtns) tools.Controls.Add(b);
        ((RoundButton)toolBtns[4]).Badge = InboxUnread;
        icons.AutoSize = true; icons.WrapContents = false; icons.Margin = new Padding(0);

        // left of the top bar: logo, active tab, all-accounts button
        PictureBox logo = new PictureBox(); logo.SetBounds(16, 10, 28, 28); logo.SizeMode = PictureBoxSizeMode.Zoom;
        logo.Image = new Icon(AppIcon, 32, 32).ToBitmap(); logo.BackColor = Color.Transparent;
        tab.SetBounds(82, 8, 224, 32);
        tab.Paint += PaintTab;
        gridBtn = ToolButton("", "Search accounts and actions", null);
        gridBtn.SetBounds(316, 8, 36, 32);
        gridBtn.Click += delegate { OpenPalette(); };
        top.Controls.Add(gridBtn);
        top.Controls.Add(tab);
        top.Controls.Add(logo);
        foreach (AppDef a in apps) AddIcon(a);
        RefreshSiblings();
        ApplyLayout();

        SetupTray();
        showEvt = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "LiteHub.show");
        System.Threading.ThreadPool.RegisterWaitForSingleObject(showEvt,
            delegate { if (IsHandleCreated) BeginInvoke(new Action(Restore)); }, null, -1, false);

        Timer sweep = new Timer();
        sweep.Interval = 30000;
        sweep.Tick += delegate { Sweep(); };
        sweep.Start();

        Timer warm = new Timer();
        warm.Interval = 4000;            // stagger background starts so the UI never stalls
        warm.Tick += delegate
        {
            if (warmQueue.Count == 0) { warm.Stop(); return; }
            AppDef a = warmQueue.Dequeue();
            if (apps.Contains(a) && a.View == null) Start(a, false);
        };

        if (background) { WindowState = FormWindowState.Minimized; ShowInTaskbar = false; }
        Shown += delegate
        {
            if (apps.Count > 0)
            {
                AppDef first = apps.Find(delegate(AppDef x) { return x.Name == lastApp; }) ?? apps[0];
                ShowApp(first);
                foreach (AppDef a in apps) if (a.KeepAlive && a != first) warmQueue.Enqueue(a);
                warm.Start();
            }
            FetchMissingIcons();
            if (apps.Count == 0 && !background) ShowOverlay(new AddAccountPanel(this), "Add account");
            if (background) Hide();
            UpdateTaskbar();
        };
    }

    // ---------- layout / theme ----------

    protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutIcons(); }

    // Show only the icons that fit; the grid button lists every account.
    void LayoutIcons()
    {
        int fit = Cfg.LeftNav ? (ClientSize.Height - 92 - 220 - 10) / 48 : (ClientSize.Width - 352 - 214 - 16) / 54;
        fit = Math.Max(0, fit);
        int shown = 0;
        foreach (AppDef a in apps)
            if (a.Btn != null) a.Btn.Visible = InWs(a) && shown++ < fit;
    }

    public void ApplyLayout()
    {
        bool left = Cfg.LeftNav;
        leftBar.Visible = left; top.Visible = !left; strip.Visible = !left;
        if (railHead.Controls.Count == 0)
        {
            railHead.Dock = DockStyle.Top; railHead.Height = 92; railHead.Cursor = Cursors.Hand;
            wsBtn = new RoundButton(); wsBtn.SetBounds(8, 56, 48, 24); wsBtn.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            wsBtn.Text = Cfg.Workspace == "" ? Loc.T("All") : Cfg.Workspace; wsBtn.Cursor = Cursors.Hand; wsBtn.BackColor = Pal.Card; wsBtn.ForeColor = Pal.Text;
            wsBtn.Click += delegate { WorkspaceMenu(wsBtn); };
            tip.SetToolTip(wsBtn, Loc.T("Workspace"));
            railHead.Controls.Add(wsBtn);
            PictureBox rl = new PictureBox(); rl.SetBounds(16, 14, 32, 32); rl.SizeMode = PictureBoxSizeMode.Zoom;
            rl.Image = new Icon(AppIcon, 32, 32).ToBitmap(); rl.BackColor = Color.Transparent; rl.Cursor = Cursors.Hand;
            rl.Click += delegate { OpenPalette(); }; railHead.Click += delegate { OpenPalette(); };
            tip.SetToolTip(rl, Loc.T("Search accounts and actions") + "  (Ctrl+K)");
            railHead.Controls.Add(rl);
        }
        leftBar.Controls.Remove(railHead);
        top.Controls.Remove(icons); top.Controls.Remove(tools);
        leftBar.Controls.Remove(icons); leftBar.Controls.Remove(tools);
        if (left)
        {
            icons.FlowDirection = FlowDirection.TopDown; icons.Dock = DockStyle.Top; icons.Padding = new Padding(0, 6, 0, 0);
            tools.Dock = DockStyle.Bottom; tools.Size = new Size(64, 220);
            leftBar.Controls.Add(icons); leftBar.Controls.Add(railHead); leftBar.Controls.Add(tools);
            int[] order = { 4, 0, 1, 2, 3 };   // inbox bell first
            for (int i = 0; i < order.Length; i++) toolBtns[order[i]].Location = new Point(14, 6 + i * 42);
        }
        else
        {
            icons.FlowDirection = FlowDirection.LeftToRight; icons.Dock = DockStyle.Right; icons.Padding = new Padding(0, 2, 8, 0);
            tools.Dock = DockStyle.Right; tools.Size = new Size(214, 48);
            top.Controls.Add(icons); top.Controls.Add(tools);
            int[] order = { 4, 0, 1, 2, 3 };
            for (int i = 0; i < order.Length; i++) toolBtns[order[i]].Location = new Point(4 + i * 42, 6);
        }
        foreach (AppDef a in apps) a.Btn.Margin = left ? new Padding(6, 0, 6, 0) : new Padding(1, 0, 1, 0);
        LayoutIcons();
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        Pal.Update();
        BackColor = Pal.Page; host.BackColor = Pal.Page;
        Color bar = Pal.Bar;
        if (Cfg.Colorful && active != null) bar = Pal.Blend(Pal.Bar, active.AccentColor, Pal.Dark ? 0.22 : 0.3);
        top.BackColor = bar; leftBar.BackColor = bar; tools.BackColor = Color.Transparent; icons.BackColor = Color.Transparent; railHead.BackColor = Color.Transparent; leftBar.Invalidate();
        if (wsBtn != null) { wsBtn.BackColor = Pal.Card; wsBtn.ForeColor = Pal.Text; }
        tab.BackColor = Pal.Card;
        foreach (Button b in new Button[] { toolBtns[0], toolBtns[1], toolBtns[2], toolBtns[3], toolBtns[4], gridBtn })
        {
            b.BackColor = Color.Transparent; b.ForeColor = Pal.Text; b.FlatAppearance.MouseOverBackColor = Pal.Hover;
        }
        foreach (AppDef a in apps) a.Btn.Invalidate();
        tab.Invalidate();
        foreach (AppDef a in apps) if (a.View != null && a.View.CoreWebView2 != null) ApplyScheme(a.View.CoreWebView2);
        ArrangeSplit();
    }

    static void ApplyScheme(CoreWebView2 c)
    {
        try
        {
            c.Profile.PreferredColorScheme = Cfg.Theme == 0 ? CoreWebView2PreferredColorScheme.Auto
                : Cfg.Theme == 2 ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
        }
        catch (Exception) { }   // older WebView2 runtime
    }

    Button ToolButton(string glyph, string tooltip, EventHandler click)
    {
        Button b = new RoundButton();
        b.BackColor = Color.Transparent;
        b.Text = glyph; b.Font = new Font("Segoe MDL2 Assets", 12f);
        b.SetBounds(0, 6, 36, 36);
        b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand;
        b.Click += click;
        tip.SetToolTip(b, Loc.T(tooltip));
        return b;
    }

    void PaintTab(object s, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color accent = active != null ? active.AccentColor : Pal.Brand;
        using (Pen p = new Pen(tabTitle != null ? Pal.Border : accent, 2)) g.DrawRectangle(p, 1, 1, tab.Width - 3, tab.Height - 3);
        using (Brush tb = new SolidBrush(Pal.Text))
        {
            if (tabTitle != null)
            {
                using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold)) g.DrawString(tabTitle, f, tb, 14, 6);
                return;
            }
            if (active == null) return;
            int x = 14;
            if (active.Icon != null && !active.LetterThumb) { g.DrawImage(active.Icon, new Rectangle(12, 6, 20, 20)); x = 44; }
            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold)) g.DrawString(active.Name, f, tb, x, 6);
            using (Font f = new Font("Segoe MDL2 Assets", 9f))
            using (Brush sb = new SolidBrush(Pal.Sub)) g.DrawString("", f, sb, tab.Width - 28, 9);
        }
    }

    void AddIcon(AppDef a)
    {
        AccountIcon b = new AccountIcon(a);
        b.Click += delegate
        {   // a second click on the open account shows its quick menu (add another account, split, reload...)
            if (a == active && overlay == null && b.ContextMenuStrip != null) b.ContextMenuStrip.Show(b, new Point(b.Width, 0));
            else ShowApp(a);
        };
        tip.SetToolTip(b, a.Name);
        ContextMenuStrip m = new AutoMenu();
        ToolStripMenuItem reload = new ToolStripMenuItem(Loc.T("Reload"));
        reload.Click += delegate { if (a.View != null && a.View.CoreWebView2 != null) a.View.Reload(); };
        ToolStripMenuItem set = new ToolStripMenuItem(Loc.T("Account settings"));
        set.Click += delegate { ShowSettings(a.Id); };
        ToolStripMenuItem remove = new ToolStripMenuItem(Loc.T("Remove"));
        remove.Click += delegate { RemoveApp(a); };
        ToolStripMenuItem side = new ToolStripMenuItem(Loc.T("Open side by side"));
        side.Click += delegate { AddToSplit(a); };
        ToolStripMenuItem another = new ToolStripMenuItem(Loc.T("Add another account"));
        another.Click += delegate { AddAnother(a); };
        ToolStripMenuItem wsm = new ToolStripMenuItem(Loc.T("Workspace"));
        wsm.DropDownOpening += delegate { FillWorkspaceItems(wsm, a); };
        wsm.DropDownItems.Add(new ToolStripMenuItem("..."));
        m.Items.Add(another); m.Items.Add(wsm); m.Items.Add(side); m.Items.Add(reload); m.Items.Add(set); m.Items.Add(remove);
        b.ContextMenuStrip = m;
        a.Btn = b;
        string png = IconFile(a);
        if (File.Exists(png)) try { a.Icon = LoadCopy(png); } catch (Exception) { }
        if (a.Icon != null && a.Icon.Width < 32) { a.Icon.Dispose(); a.Icon = null; }   // tiny old icon: fetch a better one
        icons.Controls.Add(b);
    }

    void GridMenu(Control anchor)
    {
        ContextMenuStrip m = new AutoMenu();
        foreach (AppDef a in apps)
        {
            AppDef x = a;
            ToolStripMenuItem i = new ToolStripMenuItem(a.Name + (a.Unread > 0 ? "  (" + a.Unread + ")" : ""));
            i.Checked = a == active;
            i.Click += delegate { ShowApp(x); };
            m.Items.Add(i);
        }
        m.Show(anchor, new Point(0, anchor.Height));
    }

    // ---------- workspaces: named groups of accounts (Work, Personal...); the rail shows one at a time ----------

    public bool InWs(AppDef a) { return Cfg.Workspace == "" || a.Workspace == Cfg.Workspace; }

    List<string> Workspaces()
    {
        List<string> l = new List<string>();
        foreach (AppDef a in apps) if (a.Workspace.Length > 0 && !l.Contains(a.Workspace)) l.Add(a.Workspace);
        if (Cfg.Workspace.Length > 0 && !l.Contains(Cfg.Workspace)) l.Add(Cfg.Workspace);
        return l;
    }

    public void SetWorkspace(string w)
    {
        Cfg.Workspace = w; Cfg.Save();
        if (wsBtn != null) wsBtn.Text = w == "" ? Loc.T("All") : w;
        LayoutIcons();
        if (active != null && !InWs(active)) { AppDef f = apps.Find(InWs); if (f != null) ShowApp(f); }
    }

    string AskWorkspace()
    {
        string n = Dialogs.Ask("Workspace", "Workspace name (for example Work)", "", false);
        n = n == null ? "" : n.Replace("|", "").Trim();
        return n;
    }

    void WorkspaceMenu(Control anchor)
    {
        ContextMenuStrip m = new AutoMenu();
        ToolStripMenuItem all = new ToolStripMenuItem(Loc.T("All accounts")); all.Checked = Cfg.Workspace == "";
        all.Click += delegate { SetWorkspace(""); }; m.Items.Add(all);
        foreach (string w in Workspaces())
        {
            string name = w; ToolStripMenuItem it = new ToolStripMenuItem(w); it.Checked = Cfg.Workspace == w;
            it.Click += delegate { SetWorkspace(name); }; m.Items.Add(it);
        }
        m.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem nw = new ToolStripMenuItem(Loc.T("New workspace..."));
        nw.Click += delegate { string n = AskWorkspace(); if (n.Length > 0) SetWorkspace(n); }; m.Items.Add(nw);
        m.Show(anchor, new Point(0, anchor.Height));
    }

    void MoveToWorkspace(AppDef a, string w)
    {
        a.Workspace = w; SaveApps(); LayoutIcons();
        if (a == active && !InWs(a)) { AppDef f = apps.Find(InWs); if (f != null) ShowApp(f); }
    }

    // "Workspace" submenu of an account's right-click menu.
    void FillWorkspaceItems(ToolStripMenuItem parent, AppDef a)
    {
        parent.DropDownItems.Clear();
        ToolStripMenuItem none = new ToolStripMenuItem(Loc.T("No workspace")); none.Checked = a.Workspace == "";
        none.Click += delegate { MoveToWorkspace(a, ""); }; parent.DropDownItems.Add(none);
        foreach (string w in Workspaces())
        {
            string name = w; ToolStripMenuItem it = new ToolStripMenuItem(w); it.Checked = a.Workspace == w;
            it.Click += delegate { MoveToWorkspace(a, name); }; parent.DropDownItems.Add(it);
        }
        ToolStripMenuItem nw = new ToolStripMenuItem(Loc.T("New workspace..."));
        nw.Click += delegate { string n = AskWorkspace(); if (n.Length > 0) MoveToWorkspace(a, n); }; parent.DropDownItems.Add(nw);
    }

    // New account of the same service as `a` (own name, own login).
    public void AddAnother(AppDef a)
    {
        Uri au; Uri.TryCreate(a.Url, UriKind.Absolute, out au);
        Service svc = au == null ? null : Array.Find(Catalog.All, delegate(Service s)
        {
            Uri su; return !s.IsTemplate && Uri.TryCreate(s.Url, UriKind.Absolute, out su) && su.Host == au.Host;
        });
        if (svc != null) { AddFromService(svc); return; }
        string n = Dialogs.Ask("Add account", "Account name (for example Work or Personal)", FreeName(a.Name), false);
        if (n != null) AddAccount(n.Trim(), a.Url, a.KeepAlive, a.ExternalLinks, a.Accent);
    }

    // ---------- memory report and manual sleep ----------

    public class MemReport { public Dictionary<AppDef, long> PerApp = new Dictionary<AppDef, long>(); public long Shared; }

    static string Reg(string host) { string[] p = host.Split('.'); return p.Length <= 2 ? host : p[p.Length - 2] + "." + p[p.Length - 1]; }

    // Private memory of the WebView2 processes. A renderer is credited to the account whose site it shows;
    // accounts of the same site split it, everything else (browser, GPU...) is "shared".
    public async Task<MemReport> Memory()
    {
        MemReport r = new MemReport();
        if (envTask == null || !envTask.IsCompleted) return r;
        try
        {
            foreach (CoreWebView2ProcessExtendedInfo info in await envTask.Result.GetProcessExtendedInfosAsync())
            {
                long mem = 0;
                try { mem = System.Diagnostics.Process.GetProcessById(info.ProcessInfo.ProcessId).PrivateMemorySize64; } catch (Exception) { continue; }
                List<AppDef> hit = new List<AppDef>();
                if (info.ProcessInfo.Kind == CoreWebView2ProcessKind.Renderer)
                    foreach (CoreWebView2FrameInfo f in info.AssociatedFrameInfos)
                    {
                        Uri fu;
                        if (!Uri.TryCreate(f.Source, UriKind.Absolute, out fu)) continue;
                        foreach (AppDef a in apps)
                        {
                            Uri au;
                            if (a.View != null && !hit.Contains(a) && Uri.TryCreate(a.Url, UriKind.Absolute, out au) && Reg(au.Host) == Reg(fu.Host)) hit.Add(a);
                        }
                    }
                if (hit.Count == 0) r.Shared += mem;
                else foreach (AppDef a in hit) { long cur; r.PerApp.TryGetValue(a, out cur); r.PerApp[a] = cur + mem / hit.Count; }
            }
        }
        catch (Exception) { }
        return r;
    }

    // Free an account's browser now (it reloads when opened again). Never the one on screen.
    public bool Sleep(AppDef a)
    {
        if (a.View == null || (a == active && Visible) || (split.Contains(a) && Visible)) return false;
        Free(a); return true;
    }

    // ---------- quick switcher (Ctrl+K) and shortcuts ----------

    void CycleAccount(int dir)
    {
        List<AppDef> l = apps.FindAll(InWs);
        if (l.Count < 2) return;
        int i = l.IndexOf(active);
        ShowApp(l[i < 0 ? 0 : (i + dir + l.Count) % l.Count]);
    }

    // minutes: 0 = off, -1 = until 8:00 (today if it is earlier, else tomorrow)
    public void SetDnd(int minutes)
    {
        DateTime until = minutes == 0 ? DateTime.MinValue : minutes > 0 ? DateTime.Now.AddMinutes(minutes)
            : (DateTime.Now.Hour < 8 ? DateTime.Today.AddHours(8) : DateTime.Today.AddDays(1).AddHours(8));
        Cfg.DndUntil = until.Ticks; Cfg.Save(); DndText();
    }

    void DndText() { tray.Text = Cfg.DndActive ? "LiteHub (" + Loc.T("Do not disturb") + ")" : "LiteHub"; }

    bool capBusy;

    // Over the memory limit: sleep the least recently used accounts (never the open one or keep-alive ones) until under it.
    async void EnforceCap()
    {
        if (capBusy) return;
        capBusy = true;
        try
        {
            MemReport r = await Memory();
            long total = r.Shared; foreach (long v in r.PerApp.Values) total += v;
            long cap = (long)Cfg.MemCapMB << 20;
            if (total > cap)
            {
                List<AppDef> c = apps.FindAll(delegate(AppDef a) { return a.View != null && !a.KeepAlive && a != active && !split.Contains(a); });
                c.Sort(delegate(AppDef x, AppDef y) { return x.LastUsed.CompareTo(y.LastUsed); });
                foreach (AppDef a in c)
                {
                    long mem; r.PerApp.TryGetValue(a, out mem);
                    if (Sleep(a)) total -= mem;
                    if (total <= cap) break;
                }
            }
        }
        catch (Exception) { }
        finally { capBusy = false; }
    }

    PalettePanel palette;

    void OpenPalette()
    {
        if (palette != null && !palette.IsDisposed) { palette.Close(); palette = null; return; }
        List<PaletteItem> items = new List<PaletteItem>(); List<Action> acts = new List<Action>();
        foreach (AppDef a in apps)
        {
            AppDef x = a;
            items.Add(new PaletteItem { Text = a.Name, Dot = a.AccentColor, Hint = a.Unread > 0 ? a.Unread + " new" : a.Workspace });
            acts.Add(delegate { ShowApp(x); });
        }
        items.Add(new PaletteItem { Text = Loc.T("Inbox"), Hint = InboxUnread > 0 ? InboxUnread + " new" : "" }); acts.Add(delegate { ShowOverlay(new InboxPanel(this), "Inbox"); });
        items.Add(new PaletteItem { Text = Loc.F("Do not disturb: {0}", Loc.T("1 hour")) }); acts.Add(delegate { SetDnd(60); });
        items.Add(new PaletteItem { Text = Loc.F("Do not disturb: {0}", Loc.T("Off")) }); acts.Add(delegate { SetDnd(0); });
        items.Add(new PaletteItem { Text = Loc.F("Workspace: {0}", Loc.T("All")) }); acts.Add(delegate { SetWorkspace(""); });
        foreach (string w in Workspaces()) { string name = w; items.Add(new PaletteItem { Text = Loc.F("Workspace: {0}", w) }); acts.Add(delegate { SetWorkspace(name); }); }
        items.Add(new PaletteItem { Text = Loc.T("Add account") }); acts.Add(delegate { ShowOverlay(new AddAccountPanel(this), "Add account"); });
        items.Add(new PaletteItem { Text = Loc.T("Settings") }); acts.Add(delegate { ShowSettings("General"); });
        items.Add(new PaletteItem { Text = Loc.T("Downloads") }); acts.Add(delegate { ShowOverlay(new DownloadsPanel(this), "Downloads"); });
        items.Add(new PaletteItem { Text = Loc.T("2 side by side") }); acts.Add(delegate { SetSplit(2); });
        palette = new PalettePanel(items, delegate(int i) { if (i >= 0) acts[i](); });
        palette.ShowIn(host);
    }

    // ---------- unified inbox: notifications from every account ----------

    public class InboxItem { public AppDef App; public string Title, Body; public DateTime Time; public bool Read; }
    public readonly List<InboxItem> Inbox = new List<InboxItem>();
    public Action InboxChanged;
    public int InboxUnread { get { return Inbox.FindAll(delegate(InboxItem i) { return !i.Read; }).Count; } }

    void AddInbox(AppDef a, string title, string body)
    {
        bool seen = a == active && Visible && overlay == null && WindowState != FormWindowState.Minimized;
        Inbox.Insert(0, new InboxItem { App = a, Title = title ?? "", Body = body ?? "", Time = DateTime.Now, Read = seen });
        if (Inbox.Count > 200) Inbox.RemoveRange(200, Inbox.Count - 200);
        InboxRefresh();
    }

    static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "").Replace("\n", "\\n"); }

    static string Unesc(string s)
    {
        StringBuilder b = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length) { i++; b.Append(s[i] == 't' ? '\t' : s[i] == 'n' ? '\n' : s[i]); }
            else b.Append(s[i]);
        }
        return b.ToString();
    }

    // inbox.txt: ticks <tab> read <tab> account id <tab> title <tab> body (escaped), newest first
    void LoadInbox()
    {
        if (!Cfg.KeepInbox || !File.Exists(Paths.Inbox)) return;
        try
        {
            foreach (string line in File.ReadAllLines(Paths.Inbox))
            {
                string[] p = line.Split('\t');
                long ticks; AppDef a = p.Length < 5 ? null : apps.Find(delegate(AppDef x) { return x.Id == p[2]; });
                if (a == null || !long.TryParse(p[0], out ticks)) continue;
                Inbox.Add(new InboxItem { App = a, Time = new DateTime(ticks), Read = p[1] == "1", Title = Unesc(p[3]), Body = Unesc(p[4]) });
            }
        }
        catch (Exception) { }   // damaged file: start empty
    }

    public void InboxSave()
    {
        try
        {
            if (!Cfg.KeepInbox) { if (File.Exists(Paths.Inbox)) File.Delete(Paths.Inbox); return; }
            StringBuilder sb = new StringBuilder();
            foreach (InboxItem i in Inbox)
                sb.Append(i.Time.Ticks + "\t" + (i.Read ? "1" : "0") + "\t" + i.App.Id + "\t" + Esc(i.Title) + "\t" + Esc(i.Body) + "\r\n");
            File.WriteAllText(Paths.Inbox + ".tmp", sb.ToString());
            if (File.Exists(Paths.Inbox)) File.Replace(Paths.Inbox + ".tmp", Paths.Inbox, null); else File.Move(Paths.Inbox + ".tmp", Paths.Inbox);
        }
        catch (Exception) { }
    }

    void InboxRefresh()
    {
        InboxSave();
        ((RoundButton)toolBtns[4]).Badge = InboxUnread; toolBtns[4].Invalidate();
        if (InboxChanged != null) InboxChanged();
    }

    public void OpenInbox(InboxItem it)
    {
        it.Read = true; InboxRefresh();
        if (apps.Contains(it.App)) ShowApp(it.App);
    }

    public void InboxMarkAllRead() { foreach (InboxItem i in Inbox) i.Read = true; InboxRefresh(); }
    public void InboxClear() { Inbox.Clear(); InboxRefresh(); }
    // Ctrl+K opens the switcher, Ctrl+1..9 jumps to an account (when the window chrome has focus).
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.K)) { OpenPalette(); return true; }
        if (keyData == (Keys.Control | Keys.Tab)) { CycleAccount(1); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.Tab)) { CycleAccount(-1); return true; }
        int n = (int)(keyData & Keys.KeyCode) - (int)Keys.D1;
        if ((keyData & Keys.Modifiers) == Keys.Control && n >= 0 && n < 9 && n < apps.Count) { ShowApp(apps[n]); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ---------- split view: several accounts side by side ----------

    readonly List<AppDef> split = new List<AppDef>();
    readonly Dictionary<AppDef, Label> heads = new Dictionary<AppDef, Label>();

    void SplitMenu(Control anchor)
    {
        ContextMenuStrip m = new AutoMenu();
        string[] names = { "Single view", "2 side by side", "3 columns", "2 x 2 (4 accounts)", "3 x 2 (6 accounts)" };
        int[] counts = { 1, 2, 3, 4, 6 };
        for (int i = 0; i < names.Length; i++)
        {
            int n = counts[i];
            ToolStripMenuItem it = new ToolStripMenuItem(Loc.T(names[i]));
            it.Checked = n == 1 ? split.Count == 0 : split.Count == n;
            it.Click += delegate { SetSplit(n); };
            m.Items.Add(it);
        }
        m.Show(anchor, new Point(0, anchor.Height));
    }

    // Resume a hidden account that is about to be shown.
    static void Wake(AppDef a)
    {
        if (a.View == null || a.View.CoreWebView2 == null) return;
        if (a.Suspended) { a.View.CoreWebView2.Resume(); a.Suspended = false; }
        a.View.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
    }

    // Show n accounts at once: the current one, then the ones already running, then the next in the list.
    public void SetSplit(int n)
    {
        HideOverlay();
        if (n <= 1 || apps.Count == 0) { ExitSplit(); return; }
        n = Math.Min(n, apps.Count);
        List<AppDef> pick = new List<AppDef>();
        if (active != null) pick.Add(active);
        foreach (AppDef a in apps) if (pick.Count < n && !pick.Contains(a) && a.View != null && (!a.Locked || a.Unlocked)) pick.Add(a);
        foreach (AppDef a in apps) if (pick.Count < n && !pick.Contains(a) && (!a.Locked || a.Unlocked)) pick.Add(a);
        split.Clear(); split.AddRange(pick);
        if (active == null) active = split[0];
        foreach (AppDef a in split) { a.LastUsed = DateTime.Now; if (a.View == null) Start(a, true); else Wake(a); }
        ArrangeSplit();
    }

    public void AddToSplit(AppDef a)
    {
        if (!Unlock(a)) return;
        HideOverlay();
        if (split.Contains(a)) { FocusSplit(a); return; }
        if (split.Count == 0)
        {
            if (active == null || active == a) { active = a; SetSplit(2); return; }
            split.Add(active);
        }
        if (split.Count >= 6) { MessageBox.Show(this, Loc.T("Six panes at most."), "LiteHub"); return; }
        split.Add(a); active = a; a.LastUsed = DateTime.Now;
        if (a.View == null) Start(a, true); else Wake(a);
        ArrangeSplit();
    }

    void FocusSplit(AppDef a)
    {
        active = a; a.LastUsed = DateTime.Now;
        ArrangeSplit();
        Text = "LiteHub - " + a.Name; tab.Invalidate();
        if (a.View != null) a.View.Focus();
    }

    void CloseSplitPane(AppDef a)
    {
        split.Remove(a);
        Background(a); a.LastUsed = DateTime.Now;
        if (split.Count < 2) { ExitSplit(); return; }
        if (active == a) active = split[0];
        ArrangeSplit();
    }

    void ExitSplit()
    {
        if (split.Count == 0 && heads.Count == 0) return;
        split.Clear();
        foreach (Label l in heads.Values) { host.Controls.Remove(l); l.Dispose(); }
        heads.Clear();
        foreach (AppDef a in apps) if (a.View != null) { a.View.Dock = DockStyle.Fill; a.View.Visible = false; }
        if (active != null) ShowApp(active);
    }

    // Lay the panes out in a grid; each has a small title bar (click = focus, x = close).
    void ArrangeSplit()
    {
        if (split.Count == 0) return;
        int n = split.Count;
        int cols = n <= 3 ? n : (n == 4 ? 2 : 3), rows = (n + cols - 1) / cols;
        int cw = host.ClientSize.Width / cols, ch = host.ClientSize.Height / rows;
        foreach (AppDef a in apps) if (a.View != null && !split.Contains(a)) a.View.Visible = false;
        foreach (AppDef a in new List<AppDef>(heads.Keys))
            if (!split.Contains(a)) { host.Controls.Remove(heads[a]); heads[a].Dispose(); heads.Remove(a); }
        for (int i = 0; i < n; i++)
        {
            AppDef a = split[i];
            Rectangle cell = new Rectangle((i % cols) * cw, (i / cols) * ch, cw - 2, ch - 2);
            Label h;
            if (!heads.TryGetValue(a, out h))
            {
                h = new Label(); h.AutoSize = false; h.Cursor = Cursors.Hand; h.TextAlign = ContentAlignment.MiddleLeft;
                h.Font = new Font("Segoe UI", 9f, FontStyle.Bold); h.Padding = new Padding(8, 0, 0, 0);
                AppDef acc = a;
                h.Click += delegate { FocusSplit(acc); };
                Label x = new Label(); x.Text = "\u2715"; x.Dock = DockStyle.Right; x.Width = 28; x.TextAlign = ContentAlignment.MiddleCenter;
                x.Cursor = Cursors.Hand; x.Click += delegate { CloseSplitPane(acc); };
                h.Controls.Add(x);
                heads[a] = h; host.Controls.Add(h);
            }
            h.SetBounds(cell.X, cell.Y, cell.Width, 24);
            h.BackColor = a == active ? Pal.Blend(Pal.Card, a.AccentColor, 0.6) : Pal.Card;
            h.ForeColor = x_color(h.BackColor);
            h.Controls[0].ForeColor = h.ForeColor;
            h.Text = a.Name + (a.Unread > 0 ? "  (" + a.Unread + ")" : "");
            if (a.View != null)
            {
                a.View.Dock = DockStyle.None;
                a.View.Bounds = new Rectangle(cell.X, cell.Y + 24, cell.Width, cell.Height - 24);
                a.View.Visible = true;
            }
        }
        foreach (AppDef a in apps) { a.Btn.Selected = a == active; a.Btn.Invalidate(); }
        tab.Invalidate();
    }

    static Color x_color(Color bg) { return Ui.Light(bg) ? Color.Black : Color.White; }

    // ---------- screens (settings, add account, downloads) ----------

    void ShowOverlay(Panel p, string title)
    {
        HideOverlay();
        overlay = p; tabTitle = title = Loc.T(title);
        host.Controls.Add(p);
        p.BringToFront();
        foreach (AppDef a in apps) { a.Btn.Selected = false; a.Btn.Invalidate(); }
        Text = "LiteHub - " + title;
        tab.Invalidate();
    }

    void HideOverlay()
    {
        if (overlay == null) return;
        host.Controls.Remove(overlay);
        overlay.Dispose();
        overlay = null; tabTitle = null;
    }

    public void ShowSettings(string page) { ShowOverlay(new SettingsPanel(this, page), "Settings"); }

    // ---------- accounts: add, remove, rename ----------

    string NewId(string name)
    {
        string b = Util.ProfileName(name), id = b;
        for (int i = 2; apps.Exists(delegate(AppDef x) { return x.Id == id; }); i++) id = b + "_" + i;
        return id;
    }

    public void AddFromService(Service s)
    {
        string url = s.Url;
        if (s.IsTemplate)
        {
            int i = url.IndexOf('{'), j = url.IndexOf('}');
            string key = url.Substring(i + 1, j - i - 1);
            string val = Dialogs.Ask(s.Name, Loc.F("Your {0} name (the part of the address, e.g. mycompany)", key), "", false);
            if (val == null) return;
            val = val.Trim().ToLower();
            if (!System.Text.RegularExpressions.Regex.IsMatch(val, "^[a-z0-9][a-z0-9-]*$")) { MessageBox.Show(this, Loc.T("Use letters, numbers and dashes only."), "LiteHub"); return; }
            url = url.Replace("{" + key + "}", val);
        }
        string baseName = System.Text.RegularExpressions.Regex.Replace(s.Name, @"\s*\(.*\)$", "");
        string name = Dialogs.Ask("Add account", "Account name (for example Work or Personal)", FreeName(baseName), false);
        if (name != null) AddAccount(name.Trim(), url, s.Keep, s.Ext, s.Color);
    }
    public void AddCustom()
    {
        string n = Dialogs.Ask("Custom website", "Name", "", false);
        if (n == null) return;
        string u = Dialogs.Ask("Custom website", "Address (https://...)", "https://", false);
        if (u != null) AddAccount(n.Trim(), u.Trim(), false, false, 0);
    }

    string FreeName(string b)
    {
        string n = b;
        for (int i = 2; apps.Exists(delegate(AppDef x) { return x.Name == n; }); i++) n = b + " " + i;
        return n;
    }

    void AddAccount(string n, string url, bool keep, bool ext, int color)
    {
        Uri u;
        string err = null;
        if (n.Length == 0 || n.IndexOf('|') >= 0) err = "Name is empty or contains '|'.";
        else if (apps.Exists(delegate(AppDef x) { return x.Name == n; })) err = "An account with this name already exists.";
        else if (!Uri.TryCreate(url, UriKind.Absolute, out u) || (u.Scheme != "http" && u.Scheme != "https"))
            err = "Address must start with http:// or https://";
        if (err != null) { MessageBox.Show(this, err, "LiteHub"); return; }
        AppDef a = new AppDef();
        a.Name = n; a.Url = url; a.KeepAlive = keep; a.ExternalLinks = ext; a.Id = NewId(n);
        a.Accent = color != 0 ? color : Catalog.ColorFor(url);
        a.Workspace = Cfg.Workspace;   // a new account lands in the workspace you are looking at
        apps.Add(a);
        AddIcon(a);
        a.Btn.Margin = Cfg.LeftNav ? new Padding(6, 0, 6, 0) : new Padding(1, 0, 1, 0);
        RefreshSiblings();
        SetIcon(a, null);
        LayoutIcons();
        SaveApps();
        ShowApp(a);
    }

    public void RemoveApp(AppDef a)
    {
        if (MessageBox.Show(this, Loc.F("Remove {0}? (its saved login stays on disk)", a.Name), "LiteHub",
            MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        if (a.View != null) { host.Controls.Remove(a.View); a.View.Dispose(); a.View = null; }
        apps.Remove(a);
        Inbox.RemoveAll(delegate(InboxItem i) { return i.App == a; }); InboxRefresh();
        icons.Controls.Remove(a.Btn);
        RefreshSiblings();
        LayoutIcons();
        if (active == a) active = null;
        if (split.Remove(a))
        {
            if (active == null && split.Count > 0) active = split[0];
            if (split.Count < 2) ExitSplit(); else ArrangeSplit();
        }
        SaveApps();
        UpdateTaskbar();
        ShowSettings("General");
    }

    public void MoveApp(AppDef a, int delta)
    {
        int i = apps.IndexOf(a), j = i + delta;
        if (j < 0 || j >= apps.Count) return;
        apps.RemoveAt(i); apps.Insert(j, a);
        icons.Controls.SetChildIndex(a.Btn, j);
        LayoutIcons();
        SaveApps();
    }

    public void AccountChanged(AppDef a)
    {
        SaveApps();
        a.Btn.Invalidate();
        tip.SetToolTip(a.Btn, a.Name);
        if (a == active) { tab.Invalidate(); ApplyTheme(); if (tabTitle == null) Text = "LiteHub - " + a.Name; }
    }

    public void RefreshIcons() { foreach (AppDef a in apps) a.Btn.Invalidate(); }

    // ---------- config / state ----------

    void LoadApps()
    {
        string file = Paths.Apps;
        if (!File.Exists(file))
        {   // first run: take the list that sits next to the exe (older versions) or the defaults
            string old = Path.Combine(baseDir, "apps.txt");
            File.WriteAllText(file, File.Exists(old) ? File.ReadAllText(old) : Header + DefaultApps);
        }
        foreach (string raw in File.ReadAllLines(file))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            string[] p = line.Split('|');
            Uri u;
            if (p.Length < 2 || !Uri.TryCreate(p[1].Trim(), UriKind.Absolute, out u) ||
                (u.Scheme != "http" && u.Scheme != "https")) continue;   // file is user-editable: only web URLs
            AppDef a = new AppDef();
            a.Name = p[0].Trim();
            a.Url = p[1].Trim();
            a.KeepAlive = p.Length > 2 && p[2].Trim() == "1";
            a.ExternalLinks = p.Length > 3 && p[3].Trim() == "1";
            int rgb;
            if (p.Length > 4 && int.TryParse(p[4].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb)) a.Accent = rgb & 0xFFFFFF;
            a.LetterThumb = p.Length > 5 && p[5].Trim() == "1";
            a.Notify = p.Length <= 6 || p[6].Trim() != "0";
            a.Badge = p.Length <= 7 || p[7].Trim() != "0";
            a.DownloadDir = p.Length > 8 ? p[8].Trim() : "";
            a.Id = p.Length > 9 && p[9].Trim().Length > 0 ? p[9].Trim() : Util.ProfileName(a.Name);
            a.Workspace = p.Length > 10 ? p[10].Trim() : "";
            a.Locked = p.Length > 11 && p[11].Trim() == "1";
            if (a.Accent == 0) a.Accent = Catalog.ColorFor(a.Url);
            // empty name, or it would share another account's login
            if (a.Name.Length == 0 || apps.Exists(delegate(AppDef x) { return x.Id == a.Id || x.Name == a.Name; })) continue;
            apps.Add(a);
        }
    }

    public void SaveApps()
    {
        StringBuilder sb = new StringBuilder(Header);
        foreach (AppDef a in apps)
            sb.Append(a.Name + "|" + a.Url + "|" + (a.KeepAlive ? "1" : "0") + "|" + (a.ExternalLinks ? "1" : "0") + "|" +
                (a.Accent != 0 ? a.Accent.ToString("X6") : "") + "|" + (a.LetterThumb ? "1" : "0") + "|" + (a.Notify ? "1" : "0") + "|" +
                (a.Badge ? "1" : "0") + "|" + a.DownloadDir.Replace("|", "") + "|" + a.Id + "|" + a.Workspace + "|" + (a.Locked ? "1" : "0") + "\r\n");
        try
        {
            File.WriteAllText(Paths.Apps + ".tmp", sb.ToString());   // write aside, then swap: a crash cannot leave a half file
            File.Replace(Paths.Apps + ".tmp", Paths.Apps, null);
        }
        catch (Exception ex) { MessageBox.Show(this, "Could not save accounts: " + ex.Message, "LiteHub"); }
    }

    string IconFile(AppDef a) { return Path.Combine(Paths.Icons, a.Id + ".png"); }

    static Image LoadCopy(string path)
    {
        using (Image i = Image.FromFile(path)) return new Bitmap(i);   // no lock on the file
    }

    // state.txt: line 1 = last app, line 2 = "x,y,w,h,maximized"
    void LoadState()
    {
        try
        {
            string[] l = File.ReadAllLines(Paths.State);
            lastApp = l[0];
            string[] b = l[1].Split(',');
            Rectangle r = new Rectangle(int.Parse(b[0]), int.Parse(b[1]), int.Parse(b[2]), int.Parse(b[3]));
            foreach (Screen s in Screen.AllScreens)
                if (s.WorkingArea.IntersectsWith(r) && r.Width > 300 && r.Height > 200)
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = r;
                    if (b[4] == "1") { WindowState = FormWindowState.Maximized; startMaximized = true; }
                    break;
                }
        }
        catch (Exception) { }   // first run or damaged file: use defaults
    }

    void SaveState()
    {
        try
        {
            Rectangle r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            File.WriteAllText(Paths.State, (active != null ? active.Name : "") + "\r\n" +
                r.X + "," + r.Y + "," + r.Width + "," + r.Height + "," + (WindowState == FormWindowState.Maximized ? "1" : "0"));
        }
        catch (Exception) { }
    }

    // ---------- tray / startup / hotkey / lock ----------

    void SetupTray()
    {
        tray.Icon = AppIcon;
        tray.Text = "LiteHub";
        ContextMenuStrip m = new AutoMenu();
        ToolStripMenuItem open = new ToolStripMenuItem(Loc.T("Open"));
        open.Click += delegate { Restore(); };
        ToolStripMenuItem exit = new ToolStripMenuItem(Loc.T("Exit"));
        exit.Click += delegate { reallyExit = true; Close(); };
        m.Items.Add(open);
        m.Items.Add(exit);
        tray.ContextMenuStrip = m;
        tray.DoubleClick += delegate { Restore(); };
        tray.Visible = true;
    }

    void Restore()
    {
        if (locked)
        {
            if (!Unlock()) return;
            locked = false;
        }
        ShowInTaskbar = true;
        Show();
        if (WindowState == FormWindowState.Minimized) { WindowState = startMaximized ? FormWindowState.Maximized : FormWindowState.Normal; startMaximized = false; }
        Activate();
        if (active != null && tabTitle == null) ShowApp(active);   // brings it back if it was freed while hidden
        UpdateTaskbar();
    }

    void HideToTray()
    {
        if (active != null) active.LastUsed = DateTime.Now;
        Hide();
    }

    public static bool Unlock()
    {
        while (true)
        {
            string p = Dialogs.Ask("LiteHub", "Enter your passcode", "", true);
            if (p == null) return false;
            if (Cfg.CheckPass(p)) return true;
        }
    }

    public void ApplyStartup()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
        {
            if (Cfg.Startup == 0) k.DeleteValue("LiteHub", false);
            else k.SetValue("LiteHub", "\"" + Application.ExecutablePath + "\"" + (Cfg.Startup == 2 ? " --background" : ""));
        }
    }

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);

    // False when another program already owns the shortcut.
    public bool RegisterHotkey()
    {
        if (!IsHandleCreated) return true;
        UnregisterHotKey(Handle, 1);
        uint mod; Keys key;
        return !(Cfg.HotkeyOn && Util.ParseHotkey(Cfg.Hotkey, out mod, out key)) || RegisterHotKey(Handle, 1, mod, (uint)key);
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); RegisterHotkey(); }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1)
        {
            if (Visible && WindowState != FormWindowState.Minimized && ActiveForm == this) HideToTray(); else Restore();
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveState();
        if (!reallyExit && Cfg.CloseToTray && (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None))   // None = X on the taskbar preview
        {
            e.Cancel = true;                       // keep keep-alive accounts (notifications) running
            HideToTray();
            return;
        }
        if (IsHandleCreated) UnregisterHotKey(Handle, 1);
        tray.Visible = false;
        tray.Dispose();
        base.OnFormClosing(e);
    }

    // Total unread count on the taskbar button.
    public void UpdateTaskbar()
    {
        int total = 0;
        foreach (AppDef a in apps) if (a.Badge) total += a.Unread;
        Taskbar.SetBadge(Handle, Cfg.BadgeTaskbar ? total : 0);
        tray.Text = total > 0 ? "LiteHub - " + total + " unread" : "LiteHub";
    }

    // ---------- accounts ----------

    // notify.log: what happened to notifications (no message text), to find out why one did not show up.
    static void NLog(string line)
    {
        try
        {
            string f = Path.Combine(Paths.Data, "notify.log");
            if (File.Exists(f) && new FileInfo(f).Length > 100000) File.Delete(f);
            File.AppendAllText(f, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n");
        }
        catch (Exception) { }
    }

    // Locked accounts ask for the passcode once; they lock again after sitting unused (see Sweep).
    bool Unlock(AppDef a)
    {
        if (!a.Locked || a.Unlocked) return true;
        if (!Cfg.HasPass) { a.Locked = false; return true; }
        string p = Dialogs.Ask("Passcode", "Enter your passcode", "", true);
        if (p == null || !Cfg.CheckPass(p)) return false;
        a.Unlocked = true; return true;
    }

    public void ShowApp(AppDef a)
    {
        if (!Unlock(a)) return;
        HideOverlay();
        if (split.Count > 0)
        {   // split view: a new account takes the focused pane, an open one just gets the focus
            if (!split.Contains(a))
            {
                AppDef old = split[Math.Max(0, split.IndexOf(active))];
                split[Math.Max(0, split.IndexOf(active))] = a;
                Background(old); old.LastUsed = DateTime.Now;
            }
            active = a; a.LastUsed = DateTime.Now;
            if (a.View == null) Start(a, true); else Wake(a);
            ArrangeSplit();
            Text = "LiteHub - " + a.Name; tab.Invalidate();
            if (Cfg.Colorful) ApplyTheme();
            return;
        }
        if (active != null && active != a) { Background(active); active.LastUsed = DateTime.Now; }
        if (active != null && active.View != null) active.View.Visible = false;
        active = a;
        a.LastUsed = DateTime.Now;
        foreach (AppDef x in apps) { x.Btn.Selected = (x == a); x.Btn.Invalidate(); }
        if (a.View == null) Start(a, true);
        else
        {
            if (a.Suspended && a.View.CoreWebView2 != null) { a.View.CoreWebView2.Resume(); a.Suspended = false; }
            if (a.View.CoreWebView2 != null) a.View.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
            a.View.Visible = true;
            a.View.BringToFront();
        }
        Text = "LiteHub - " + a.Name;
        tab.Invalidate();
        if (Cfg.Colorful) ApplyTheme();
    }

    // Hidden accounts keep running (notifications) but ask the browser to hold as little memory as it can.
    static void Background(AppDef a)
    {
        if (a.View != null && a.View.CoreWebView2 != null)
            a.View.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
    }

    async void Start(AppDef a, bool show)
    {
        WebView2 v = new WebView2();
        v.Dock = DockStyle.Fill;
        v.Visible = show;
        v.DefaultBackgroundColor = Pal.Page;
        a.View = v;
        host.Controls.Add(v);
        if (split.Contains(a)) ArrangeSplit();
        if (overlay != null) overlay.BringToFront();
        a.Btn.Invalidate();
        try
        {
            if (envTask == null) envTask = CoreWebView2Environment.CreateAsync(null, Path.Combine(Paths.Data, "profiles"));
            CoreWebView2Environment env = await envTask;
            // Own cookies/storage per account (separate login) but shared browser/GPU processes.
            CoreWebView2ControllerOptions opt = env.CreateCoreWebView2ControllerOptions();
            opt.ProfileName = Util.ProfileName(a.Id);
            await v.EnsureCoreWebView2Async(env, opt);
            if (a.View != v) return;     // freed or removed while starting
            CoreWebView2 c = v.CoreWebView2;
            ApplyScheme(c);
            // WebView2 swallows keys, so Ctrl+K / Ctrl+1..9 are caught here (private field: this WinForms version has no public controller property)
            System.Reflection.FieldInfo cf = typeof(Microsoft.Web.WebView2.WinForms.WebView2).GetField("_coreWebView2Controller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            CoreWebView2Controller ctl = cf == null ? null : cf.GetValue(v) as CoreWebView2Controller;
            if (ctl != null) ctl.AcceleratorKeyPressed += delegate(object s, CoreWebView2AcceleratorKeyPressedEventArgs e)
            {
                if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown || (Control.ModifierKeys & Keys.Control) == 0) return;
                int k = (int)e.VirtualKey, n = k - (int)Keys.D1;
                if (k == (int)Keys.K) { e.Handled = true; BeginInvoke(new Action(OpenPalette)); }
                else if (k == (int)Keys.Tab) { e.Handled = true; int dir = (Control.ModifierKeys & Keys.Shift) != 0 ? -1 : 1; BeginInvoke(new Action(delegate { CycleAccount(dir); })); }
                else if (n >= 0 && n < 9 && n < apps.Count) { e.Handled = true; BeginInvoke(new Action(delegate { ShowApp(apps[n]); })); }
            };
            c.NotificationReceived += delegate(object s, CoreWebView2NotificationReceivedEventArgs e)
            {   // the system toast still shows; this only keeps a copy for the Inbox
                string nt = e.Notification.Title, nb = e.Notification.Body;
                NLog(a.Name + ": notification received (do not disturb " + Cfg.DndActive + ", locked " + a.Locked + ")");
                if (a.Locked) { nt = ""; nb = Loc.T("New message"); }
                if (Cfg.DndActive || a.Locked) e.Handled = true;   // no pop-up; a locked account never shows its text
                if (a.Locked && !Cfg.DndActive) tray.ShowBalloonTip(3000, a.Name, Loc.T("New message"), ToolTipIcon.Info);
                BeginInvoke(new Action(delegate { AddInbox(a, nt, nb); }));
            };
            c.PermissionRequested += delegate(object s, CoreWebView2PermissionRequestedEventArgs e)
            {
                if (e.PermissionKind == CoreWebView2PermissionKind.Notifications)
                {   // only an "allow" is remembered: a "deny" while notifications were off must not stay in the profile for good
                    bool ok = Cfg.Notify && a.Notify;
                    e.State = ok ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
                    e.SavesInProfile = ok;
                    NLog(a.Name + ": notification permission asked -> " + (ok ? "allow" : "deny"));
                }
            };
            Uri origin;
            if (Cfg.Notify && a.Notify && Uri.TryCreate(a.Url, UriKind.Absolute, out origin))
            {   // clear an old "denied" decision so the site sees notifications as granted
                try { await c.Profile.SetPermissionStateAsync(CoreWebView2PermissionKind.Notifications, origin.GetLeftPart(UriPartial.Authority), CoreWebView2PermissionState.Allow); NLog(a.Name + ": notifications set to allowed for " + origin.Host); }
                catch (Exception ex) { NLog(a.Name + ": could not set permission: " + ex.Message); }
            }
            c.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs e)
            {
                if (!a.ExternalLinks) return;
                e.Handled = true;
                if (e.IsUserInitiated && e.Uri.StartsWith("http")) Process.Start(e.Uri);
            };
            c.DownloadStarting += delegate(object s, CoreWebView2DownloadStartingEventArgs e) { OnDownload(a, e); };
            c.ProcessFailed += delegate(object s, CoreWebView2ProcessFailedEventArgs e)
            {
                bool page = e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited ||
                            e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessUnresponsive;
                if (!page || (DateTime.Now - a.LastCrash).TotalSeconds < 20) return;
                a.LastCrash = DateTime.Now;
                BeginInvoke(new Action(delegate { if (a.View == v) c.Reload(); }));
            };
            c.DocumentTitleChanged += delegate { UpdateBadge(a, c.DocumentTitle); };
            c.FaviconChanged += delegate { if (a.Icon == null) SetIcon(a, c.FaviconUri); };
            if (a != active) Background(a);
            c.Navigate(a.Url);
        }
        catch (Exception ex)
        {
            if (a.View != v) return;     // removed or freed meanwhile: nothing to report
            a.View = null;
            host.Controls.Remove(v);
            v.Dispose();
            a.Btn.Invalidate();
            if (ex is WebView2RuntimeNotFoundException)
                MessageBox.Show("Microsoft Edge WebView2 Runtime is not installed.\nInstall it from https://go.microsoft.com/fwlink/p/?LinkId=2124703 and start LiteHub again.", "LiteHub");
            else MessageBox.Show(a.Name + ": " + ex.Message);
        }
    }

    // Save into the account's (or the global) folder, keep a list, tell the user when done.
    void OnDownload(AppDef a, CoreWebView2DownloadStartingEventArgs e)
    {
        string dir = !string.IsNullOrEmpty(a.DownloadDir) ? a.DownloadDir : !string.IsNullOrEmpty(Cfg.DownloadDir) ? Cfg.DownloadDir : Paths.DefaultDownloads;
        try { Directory.CreateDirectory(dir); }
        catch (Exception) { return; }   // bad folder: let the browser show its own save dialog
        string path = Util.UniquePath(Path.Combine(dir, Util.SafeFileName(Path.GetFileName(e.ResultFilePath))));
        e.ResultFilePath = path;
        e.Handled = true;
        DownloadItem d = new DownloadItem();
        d.Account = a.Name; d.Path = path; d.Op = e.DownloadOperation;
        Downloads.Add(d);
        d.Op.StateChanged += delegate
        {
            if (d.Op.State == CoreWebView2DownloadState.Completed)
            {
                d.State = "Done";
                tray.ShowBalloonTip(2000, Loc.T("Download finished"), Path.GetFileName(path), ToolTipIcon.Info);
            }
            else if (d.Op.State == CoreWebView2DownloadState.Interrupted) d.State = "Failed";
        };
    }

    static string HostOf(string url) { Uri u; return Uri.TryCreate(url, UriKind.Absolute, out u) ? u.Host : url; }
    static readonly int[] Ring = { 0x9D8FE0, 0xFFB900, 0xF7630C, 0xE74856, 0x0078D7, 0x00B7C3, 0x10893E, 0x881798 };

    // Several accounts of one platform (two WhatsApps...): the 2nd, 3rd... get their own ring color.
    public void RefreshSiblings()
    {
        foreach (AppDef a in apps)
        {
            List<AppDef> same = apps.FindAll(delegate(AppDef x) { return HostOf(x.Url) == HostOf(a.Url); });
            a.HasSibling = same.Count > 1;
            int pos = same.IndexOf(a);
            if (pos > 0 && a.Accent == Catalog.ColorFor(a.Url)) a.Accent = Ring[(pos - 1) % Ring.Length];
            if (a.Btn != null) a.Btn.Invalidate();
        }
    }

    // Icons come from the platform's own page (apple-touch-icon / favicon), not from an icon service.
    async void SetIcon(AppDef a, string declared)
    {
        if (a.IconBusy) return;
        a.IconBusy = true;
        try
        {
            Bitmap b = await Icons.Fetch(a.Url, declared);
            if (b == null || !apps.Contains(a)) return;
            Image old = a.Icon;
            a.Icon = b;
            b.Save(IconFile(a), System.Drawing.Imaging.ImageFormat.Png);
            if (old != null) old.Dispose();
            a.Btn.Invalidate();
            if (a == active) tab.Invalidate();
        }
        catch (Exception) { }   // icon is cosmetic
        finally { a.IconBusy = false; }
    }

    // Accounts without an icon (first run, new account) get one, one at a time.
    async void FetchMissingIcons()
    {
        foreach (AppDef a in apps.ToArray())
        {
            if (a.Icon != null || !apps.Contains(a)) continue;
            SetIcon(a, null);
            await Task.Delay(1500);
        }
    }

    public void RefreshIcon(AppDef a)
    {
        try { File.Delete(IconFile(a)); } catch (Exception) { }
        if (a.Icon != null) { a.Icon.Dispose(); a.Icon = null; }
        a.Btn.Invalidate();
        SetIcon(a, null);
    }
    // Titles like "(3) WhatsApp" or "Inbox (781) - ..." -> unread count on the account icon and the taskbar.
    void UpdateBadge(AppDef a, string title)
    {
        a.Unread = Util.ParseUnread(title);
        a.Btn.Invalidate();
        UpdateTaskbar();
    }

    void Sweep()
    {
        DndText();
        if (Cfg.MemCapMB > 0) EnforceCap();
        foreach (AppDef a in apps)
        {
            if (a.Locked && a.Unlocked && a != active && (DateTime.Now - a.LastUsed).TotalMinutes >= IdleMinutes) a.Unlocked = false;
            if (a.View == null || a.KeepAlive) continue;
            if (a == active && Visible) continue;       // being looked at
            if (split.Contains(a) && Visible) continue;       // shown in a pane
            if (a.View.CoreWebView2 != null && a.View.CoreWebView2.IsDocumentPlayingAudio) { a.LastUsed = DateTime.Now; continue; }
            double idle = (DateTime.Now - a.LastUsed).TotalMinutes;
            if (idle >= IdleMinutes) Free(a);
            else if (idle >= SuspendMinutes && a != active && !a.Suspended && a.View.CoreWebView2 != null)
            {
                a.Suspended = true;
                SuspendQuietly(a.View.CoreWebView2);
            }
        }
    }

    static async void SuspendQuietly(CoreWebView2 c)
    {
        try { await c.TrySuspendAsync(); } catch (Exception) { }
    }

    async void Free(AppDef a)
    {
        WebView2 v = a.View;
        a.View = null;
        a.Suspended = false; a.Unlocked = false;
        a.Unread = 0;
        a.Btn.Invalidate();
        UpdateTaskbar();
        // Disk cache is what grows to GBs: wipe it past 300MB (logins/cookies are kept).
        try
        {
            string dir = Path.Combine(Paths.Data, "profiles", "EBWebView", "WV2Profile_" + Util.ProfileName(a.Id));
            if (await Task.Run(() => Util.DirSize(dir)) > 300L * 1024 * 1024)
                await v.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.DiskCache);
        }
        catch (Exception) { }
        host.Controls.Remove(v);
        v.Dispose();
    }

    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main(string[] args)
    {
        if (Array.IndexOf(args, "--selftest") >= 0) Environment.Exit(Util.SelfTest() ? 0 : 1);
        ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;   // TLS 1.2 on older .NET
        SetProcessDPIAware();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        bool warned = false;
        Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
        {
            try { File.AppendAllText(Path.Combine(Paths.Data, "crash.log"), DateTime.Now + " " + e.Exception + "\r\n\r\n"); } catch (Exception) { }
            if (warned) return;
            warned = true;   // one message per run, details are in the log
            MessageBox.Show("Something went wrong. LiteHub keeps running.\nDetails: " + Path.Combine(Paths.Data, "crash.log"), "LiteHub");
        };
        Application.EnableVisualStyles();
        bool first;
        using (System.Threading.Mutex one = new System.Threading.Mutex(true, "LiteHub.single", out first))
        {
            if (!first)
            {   // already running (maybe hidden in the tray): ask it to show itself
                try { System.Threading.EventWaitHandle.OpenExisting("LiteHub.show").Set(); } catch (Exception) { }
                return;
            }
            Directory.CreateDirectory(Paths.Data);
            Cfg.Load(); Loc.Set(Cfg.Lang);
            Pal.Update();
            bool bg = Array.IndexOf(args, "--background") >= 0;
            if (Cfg.HasPass && !bg && !Unlock()) return;
            Application.Run(new MainForm(bg));
        }
    }
}

// Count on the taskbar button (ITaskbarList3 overlay icon).
static class Taskbar
{
    [ComImport, Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITaskbarList3
    {
        [PreserveSig] int HrInit();
        [PreserveSig] int AddTab(IntPtr h);
        [PreserveSig] int DeleteTab(IntPtr h);
        [PreserveSig] int ActivateTab(IntPtr h);
        [PreserveSig] int SetActiveAlt(IntPtr h);
        [PreserveSig] int MarkFullscreenWindow(IntPtr h, [MarshalAs(UnmanagedType.Bool)] bool f);
        [PreserveSig] int SetProgressValue(IntPtr h, ulong done, ulong total);
        [PreserveSig] int SetProgressState(IntPtr h, int flags);
        [PreserveSig] int RegisterTab(IntPtr tab, IntPtr mdi);
        [PreserveSig] int UnregisterTab(IntPtr tab);
        [PreserveSig] int SetTabOrder(IntPtr tab, IntPtr before);
        [PreserveSig] int SetTabActive(IntPtr tab, IntPtr mdi, uint reserved);
        [PreserveSig] int ThumbBarAddButtons(IntPtr h, uint c, IntPtr buttons);
        [PreserveSig] int ThumbBarUpdateButtons(IntPtr h, uint c, IntPtr buttons);
        [PreserveSig] int ThumbBarSetImageList(IntPtr h, IntPtr list);
        [PreserveSig] int SetOverlayIcon(IntPtr h, IntPtr icon, [MarshalAs(UnmanagedType.LPWStr)] string desc);
    }

    [ComImport, Guid("56fdf344-fd6d-11d0-958a-006097c9a090"), ClassInterface(ClassInterfaceType.None)]
    class TaskbarListCom { }

    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    static ITaskbarList3 list;
    static IntPtr lastIcon = IntPtr.Zero;
    static int lastCount = -1;

    public static void SetBadge(IntPtr hwnd, int count)
    {
        if (hwnd == IntPtr.Zero || count == lastCount) return;
        try
        {
            if (list == null) { list = (ITaskbarList3)new TaskbarListCom(); list.HrInit(); }
            IntPtr icon = IntPtr.Zero;
            if (count > 0)
                using (Bitmap bmp = new Bitmap(16, 16))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (Brush b = new SolidBrush(Color.FromArgb(220, 60, 20))) g.FillEllipse(b, 0, 0, 15, 15);
                        using (Font f = new Font("Segoe UI", count > 99 ? 6f : 7.5f, FontStyle.Bold))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            g.DrawString(count > 99 ? "99+" : count.ToString(), f, Brushes.White, new RectangleF(0, 0, 16, 16), sf);
                    }
                    icon = bmp.GetHicon();
                }
            list.SetOverlayIcon(hwnd, icon, count > 0 ? count + " unread" : "");
            if (lastIcon != IntPtr.Zero) DestroyIcon(lastIcon);
            lastIcon = icon;
            lastCount = count;
        }
        catch (Exception) { }   // the badge is cosmetic
    }
}
