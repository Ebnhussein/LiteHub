using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

// One account (a website with its own login).
class AppDef
{
    public string Id, Name, Url, DownloadDir = "", Workspace = "";
    public bool KeepAlive, ExternalLinks, Suspended, LetterThumb, Notify = true, Badge = true, Locked, Unlocked;
    public int Accent;                       // RGB, 0 = pick one from the name
    public WebView2 View;
    public AccountIcon Btn;
    public Image Icon;
    public DateTime LastUsed = DateTime.Now;
    public int Unread;
    public bool IconBusy, HasSibling;
    public DateTime LastCrash = DateTime.MinValue;
    public bool Running { get { return View != null; } }

    public Color AccentColor
    {
        get
        {
            if (Accent != 0) return Color.FromArgb(Accent | unchecked((int)0xFF000000));
            int h = 0; foreach (char c in Name) h = h * 31 + c;
            return Color.FromArgb(70 + (h & 0x7F), 70 + ((h >> 7) & 0x7F), 110 + ((h >> 14) & 0x6F));
        }
    }
}

class Service
{
    public string Category, Name, Url;
    public int Color;                         // brand color (RGB)
    public bool Keep, Ext;
    public Service(string cat, string name, string url, int color, bool keep, bool ext)
    { Category = cat; Name = name; Url = url; Color = color; Keep = keep; Ext = ext; }
    // "{workspace}" in the address means: ask the user for that part
    public bool IsTemplate { get { return Url.IndexOf('{') >= 0; } }
}

static class Catalog
{
    public static readonly Service[] All = {
        new Service("Messaging", "WhatsApp", "https://web.whatsapp.com", 0x25D366, true, true),
        new Service("Messaging", "Telegram", "https://web.telegram.org/a/", 0x26A5E4, true, true),
        new Service("Messaging", "Messenger", "https://www.messenger.com", 0x0084FF, true, true),
        new Service("Messaging", "Discord", "https://discord.com/app", 0x5865F2, true, true),
        new Service("Messaging", "Slack", "https://app.slack.com", 0x4A154B, true, false),
        new Service("Messaging", "Microsoft Teams", "https://teams.microsoft.com", 0x6264A7, true, false),
        new Service("Messaging", "Google Chat", "https://chat.google.com", 0x00AC47, true, false),
        new Service("Messaging", "Android Messages", "https://messages.google.com/web", 0x1A73E8, true, true),
        new Service("Messaging", "Skype", "https://web.skype.com", 0x00AFF0, false, true),
        new Service("Messaging", "WeChat", "https://web.wechat.com", 0x07C160, true, true),
        new Service("Messaging", "Element (Matrix)", "https://app.element.io", 0x0DBD8B, true, true),
        new Service("Messaging", "Snapchat", "https://web.snapchat.com", 0xFFFC00, true, true),
        new Service("Meetings", "Zoom", "https://app.zoom.us/wc", 0x2D8CFF, false, false),
        new Service("Meetings", "Google Meet", "https://meet.google.com", 0x00897B, false, false),
        new Service("Meetings", "Webex", "https://web.webex.com", 0x00BCF2, false, false),
        new Service("Social", "Instagram", "https://www.instagram.com/direct/inbox/", 0xE4405F, false, true),
        new Service("Social", "Facebook", "https://www.facebook.com", 0x1877F2, false, true),
        new Service("Social", "Threads", "https://www.threads.net", 0x1F1F1F, false, true),
        new Service("Social", "X (Twitter)", "https://x.com", 0x14171A, false, true),
        new Service("Social", "LinkedIn", "https://www.linkedin.com/messaging/", 0x0A66C2, false, false),
        new Service("Social", "TikTok", "https://www.tiktok.com", 0xFE2C55, false, true),
        new Service("Social", "Reddit", "https://www.reddit.com", 0xFF4500, false, true),
        new Service("Social", "Pinterest", "https://www.pinterest.com", 0xE60023, false, true),
        new Service("Social", "Tumblr", "https://www.tumblr.com", 0x001935, false, true),
        new Service("Social", "Bluesky", "https://bsky.app", 0x1185FE, false, true),
        new Service("Social", "Mastodon", "https://mastodon.social", 0x6364FF, false, true),
        new Service("Email", "Gmail", "https://mail.google.com", 0xEA4335, true, false),
        new Service("Email", "Outlook", "https://outlook.live.com/mail", 0x0078D4, true, false),
        new Service("Email", "Outlook (Microsoft 365)", "https://outlook.office.com/mail", 0x0078D4, true, false),
        new Service("Email", "Yahoo Mail", "https://mail.yahoo.com", 0x6001D2, false, false),
        new Service("Email", "iCloud Mail", "https://www.icloud.com/mail", 0x3693F3, false, false),
        new Service("Email", "Proton Mail", "https://mail.proton.me", 0x6D4AFF, true, false),
        new Service("Email", "Zoho Mail", "https://mail.zoho.com", 0xE42527, false, false),
        new Service("Email", "Fastmail", "https://app.fastmail.com", 0x3969CD, false, false),
        new Service("Email", "AOL Mail", "https://mail.aol.com", 0x3399FF, false, false),
        new Service("Email", "Tuta Mail", "https://app.tuta.com", 0x850122, false, false),
        new Service("Email", "HEY", "https://app.hey.com", 0x5522FA, false, false),
        new Service("Office & files", "Google Calendar", "https://calendar.google.com", 0x4285F4, false, false),
        new Service("Office & files", "Google Drive", "https://drive.google.com", 0x1FA463, false, false),
        new Service("Office & files", "Google Docs", "https://docs.google.com", 0x4285F4, false, false),
        new Service("Office & files", "Google Sheets", "https://sheets.google.com", 0x0F9D58, false, false),
        new Service("Office & files", "Google Keep", "https://keep.google.com", 0xFBBC04, false, false),
        new Service("Office & files", "Google Contacts", "https://contacts.google.com", 0x4285F4, false, false),
        new Service("Office & files", "Google Photos", "https://photos.google.com", 0xEA4335, false, false),
        new Service("Office & files", "Microsoft 365", "https://www.microsoft365.com", 0xD83B01, false, false),
        new Service("Office & files", "OneDrive", "https://onedrive.live.com", 0x0078D4, false, false),
        new Service("Office & files", "OneNote", "https://www.onenote.com", 0x7719AA, false, false),
        new Service("Office & files", "iCloud", "https://www.icloud.com", 0x3693F3, false, false),
        new Service("Office & files", "Dropbox", "https://www.dropbox.com", 0x0061FF, false, false),
        new Service("Productivity", "Notion", "https://www.notion.so", 0x1F1F1F, false, false),
        new Service("Productivity", "Notion Calendar", "https://calendar.notion.so", 0x1F1F1F, false, false),
        new Service("Productivity", "Evernote", "https://www.evernote.com/client/web", 0x00A82D, false, false),
        new Service("Productivity", "Trello", "https://trello.com", 0x0079BF, false, false),
        new Service("Productivity", "Asana", "https://app.asana.com", 0xF06A6A, false, false),
        new Service("Productivity", "monday.com", "https://auth.monday.com", 0xFF3D57, false, false),
        new Service("Productivity", "ClickUp", "https://app.clickup.com", 0x7B68EE, false, false),
        new Service("Productivity", "Linear", "https://linear.app", 0x5E6AD2, false, false),
        new Service("Productivity", "Todoist", "https://app.todoist.com", 0xE44332, false, false),
        new Service("Productivity", "Airtable", "https://airtable.com", 0x18BFFF, false, false),
        new Service("Productivity", "Miro", "https://miro.com", 0xFFD02F, false, false),
        new Service("Productivity", "Figma", "https://www.figma.com/files", 0xF24E1E, false, false),
        new Service("Productivity", "Canva", "https://www.canva.com", 0x00C4CC, false, false),
        new Service("Productivity", "Calendly", "https://calendly.com", 0x006BFF, false, false),
        new Service("Productivity", "Jira (your site)", "https://{site}.atlassian.net", 0x0052CC, false, false),
        new Service("Developer", "GitHub", "https://github.com", 0x24292F, false, false),
        new Service("Developer", "GitLab", "https://gitlab.com", 0xFC6D26, false, false),
        new Service("Developer", "Cloudflare", "https://dash.cloudflare.com", 0xF38020, false, false),
        new Service("Developer", "Vercel", "https://vercel.com", 0x1F1F1F, false, false),
        new Service("Developer", "Supabase", "https://supabase.com/dashboard", 0x3ECF8E, false, false),
        new Service("Business & ads", "Meta Business Suite", "https://business.facebook.com", 0x0866FF, true, false),
        new Service("Business & ads", "Meta Ads Manager", "https://adsmanager.facebook.com", 0x0866FF, false, false),
        new Service("Business & ads", "Google Ads", "https://ads.google.com", 0x4285F4, false, false),
        new Service("Business & ads", "Google Analytics", "https://analytics.google.com", 0xE37400, false, false),
        new Service("Business & ads", "Search Console", "https://search.google.com/search-console", 0x458CF5, false, false),
        new Service("Business & ads", "Looker Studio", "https://lookerstudio.google.com", 0x4285F4, false, false),
        new Service("Business & ads", "TikTok Ads", "https://ads.tiktok.com", 0xFE2C55, false, false),
        new Service("Business & ads", "Shopify (your store)", "https://admin.shopify.com/store/{store}", 0x95BF47, false, false),
        new Service("Business & ads", "Stripe", "https://dashboard.stripe.com", 0x635BFF, false, false),
        new Service("Business & ads", "PayPal", "https://www.paypal.com", 0x003087, false, false),
        new Service("Business & ads", "HubSpot", "https://app.hubspot.com", 0xFF7A59, false, false),
        new Service("Business & ads", "Mailchimp", "https://login.mailchimp.com", 0xFFE01B, false, false),
        new Service("Business & ads", "Zendesk (your company)", "https://{company}.zendesk.com", 0x03363D, false, false),
        new Service("AI", "ChatGPT", "https://chatgpt.com", 0x10A37F, false, false),
        new Service("AI", "Claude", "https://claude.ai", 0xD97757, false, false),
        new Service("AI", "Gemini", "https://gemini.google.com", 0x4285F4, false, false),
        new Service("AI", "Microsoft Copilot", "https://copilot.microsoft.com", 0x0F6CBD, false, false),
        new Service("AI", "Perplexity", "https://www.perplexity.ai", 0x20808D, false, false),
        new Service("AI", "DeepSeek", "https://chat.deepseek.com", 0x4D6BFE, false, false),
        new Service("AI", "Grok", "https://grok.com", 0x1F1F1F, false, false),
        new Service("Music & video", "YouTube", "https://www.youtube.com", 0xFF0000, false, true),
        new Service("Music & video", "YouTube Music", "https://music.youtube.com", 0xFF0000, false, false),
        new Service("Music & video", "Spotify", "https://open.spotify.com", 0x1DB954, false, false),
        new Service("Music & video", "Anghami", "https://play.anghami.com", 0x6C13BF, false, false),
        new Service("Music & video", "SoundCloud", "https://soundcloud.com", 0xFF5500, false, false),
        new Service("Music & video", "Deezer", "https://www.deezer.com", 0xA238FF, false, false),
        new Service("Music & video", "Apple Music", "https://music.apple.com", 0xFA243C, false, false),
        new Service("Music & video", "Twitch", "https://www.twitch.tv", 0x9146FF, false, true),
        new Service("Learning", "Duolingo", "https://www.duolingo.com", 0x58CC02, false, false),
    };

    // Brand color for an address we already know (so existing accounts get one too); 0 = unknown.
    public static int ColorFor(string url)
    {
        Uri u;
        if (!Uri.TryCreate(url, UriKind.Absolute, out u)) return 0;
        foreach (Service s in All)
        {
            Uri su;
            if (!s.IsTemplate && Uri.TryCreate(s.Url, UriKind.Absolute, out su) && su.Host == u.Host) return s.Color;
        }
        return 0;
    }
}

// Real platform icons, taken from the platform's own web page (no third-party icon service).
static class Icons
{
    const string UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36 Edg/130.0";

    // shortcut: a DNS name that resolves to a private IP is not blocked; fine for known sites, revisit if you add unknown ones
    public static bool IsPublicHttps(Uri u)
    {
        return u.Scheme == "https" && u.HostNameType == UriHostNameType.Dns && u.Host.IndexOf('.') >= 0;
    }

    static async System.Threading.Tasks.Task<byte[]> Get(Uri u, int max, bool truncate)
    {
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(u);
        req.Timeout = 10000; req.UserAgent = UA; req.MaximumAutomaticRedirections = 4;
        using (WebResponse r = await req.GetResponseAsync())
        {
            if (!IsPublicHttps(r.ResponseUri)) return null;      // redirected to somewhere private
            using (Stream rs = r.GetResponseStream())
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buf = new byte[8192];
                int got;
                while ((got = await rs.ReadAsync(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, got);
                    if (ms.Length > max) { if (truncate) break; return null; }
                }
                return ms.ToArray();
            }
        }
    }

    // Best icon first: apple-touch-icon, then big PNG, then .ico. SVG/WebP cannot be drawn here, so they are skipped.
    public static List<KeyValuePair<int, Uri>> Candidates(Uri page, string html, string declared)
    {
        List<KeyValuePair<int, Uri>> list = new List<KeyValuePair<int, Uri>>();
        if (html != null)
            foreach (Match m in Regex.Matches(html, @"<link\b[^>]*>", RegexOptions.IgnoreCase))
            {
                string rel = Attr(m.Value, "rel").ToLower(), href = Attr(m.Value, "href"), type = Attr(m.Value, "type").ToLower();
                if (rel.IndexOf("icon") < 0 || href.Length == 0) continue;
                string low = href.ToLower();
                if (low.EndsWith(".svg") || low.EndsWith(".webp") || type.Contains("svg") || type.Contains("webp") || low.StartsWith("data:")) continue;
                Uri u;
                if (!Uri.TryCreate(page, href, out u) || !IsPublicHttps(u)) continue;
                int size = 0;
                Match sz = Regex.Match(Attr(m.Value, "sizes"), @"(\d+)x\d+");
                if (sz.Success) int.TryParse(sz.Groups[1].Value, out size);
                int score = rel.Contains("apple-touch-icon") ? 1000 : low.EndsWith(".ico") ? 100 : 500;
                list.Add(new KeyValuePair<int, Uri>(score + Math.Min(size, 512), u));
            }
        Uri d;
        if (!string.IsNullOrEmpty(declared) && Uri.TryCreate(declared, UriKind.Absolute, out d) && IsPublicHttps(d) &&
            !declared.ToLower().EndsWith(".svg")) list.Add(new KeyValuePair<int, Uri>(300, d));
        list.Add(new KeyValuePair<int, Uri>(50, new Uri(page, "/favicon.ico")));
        list.Sort(delegate(KeyValuePair<int, Uri> a, KeyValuePair<int, Uri> b) { return b.Key.CompareTo(a.Key); });
        return list;
    }

    static string Attr(string tag, string name)
    {
        Match m = Regex.Match(tag, @"\b" + name + @"\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase);
        return m.Success ? (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value) : "";
    }

    public static async System.Threading.Tasks.Task<Bitmap> Fetch(string pageUrl, string declared)
    {
        Uri page;
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out page) || !IsPublicHttps(page)) return null;
        string html = null;
        try
        {
            byte[] h = await Get(page, 400000, true);
            if (h != null) html = Encoding.UTF8.GetString(h);
        }
        catch (Exception) { }   // no page: still try /favicon.ico
        int tried = 0;
        foreach (KeyValuePair<int, Uri> c in Candidates(page, html, declared))
        {
            if (tried++ >= 4) break;
            try
            {
                byte[] data = await Get(c.Value, 300000, false);
                if (data == null) continue;
                using (MemoryStream ms = new MemoryStream(data))
                using (Image i = Image.FromStream(ms)) return new Bitmap(i);
            }
            catch (Exception) { }   // not an image we can read: next candidate
        }
        return null;
    }
}

// Paths used everywhere.
static class Paths
{
    // LITEHUB_DATA points a test run at a throw-away folder (and its own single-instance names), so tests never touch real accounts.
    public static readonly string TestData = Environment.GetEnvironmentVariable("LITEHUB_DATA");
    public static string Tag { get { return string.IsNullOrEmpty(TestData) ? "" : ".test"; } }
    public static readonly string Data = !string.IsNullOrEmpty(TestData) ? TestData : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiteHub");
    public static string Apps { get { return Path.Combine(Data, "apps.txt"); } }
    public static string State { get { return Path.Combine(Data, "state.txt"); } }
    public static string Settings { get { return Path.Combine(Data, "settings.txt"); } }
    public static string Inbox { get { return Path.Combine(Data, "inbox.txt"); } }
    public static string Icons { get { return Path.Combine(Data, "icons"); } }
    public static string DefaultDownloads
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"); }
    }
}

// App-wide settings, saved as key=value lines.
static class Cfg
{
    public static int Theme = 2;              // 0 system, 1 light, 2 dark
    public static int Startup;                // 0 off, 1 with Windows, 2 with Windows, hidden in the tray
    public static bool LeftNav = true, RailWide = true, Colorful, FilterDl, KeepInbox = true;
    public static bool Notify = true, BadgeList = true, BadgeTaskbar = true, HotkeyOn = true, CloseToTray = true;
    public static long DndUntil;
    public static int MemCapMB;
    public static bool DndActive { get { return DateTime.Now.Ticks < DndUntil; } }
    public static string UpdateUrl = "", Workspace = "", Lang = "", Hotkey = "Alt+E", PassSalt = "", PassHash = "", DownloadDir = "";

    public static void Load()
    {
        try
        {
            foreach (string line in File.ReadAllLines(Paths.Settings))
            {
                int i = line.IndexOf('=');
                if (i < 1) continue;
                string k = line.Substring(0, i), v = line.Substring(i + 1);
                bool b = v == "1";
                int n; int.TryParse(v, out n);
                switch (k)
                {
                    case "Theme": Theme = Math.Max(0, Math.Min(2, n)); break;
                    case "Startup": Startup = Math.Max(0, Math.Min(2, n)); break;
                    case "LeftNav": LeftNav = b; break;
                    case "RailWide": RailWide = b; break;
                    case "Colorful": Colorful = b; break;
                    case "FilterDl": FilterDl = b; break;
                    case "Notify": Notify = b; break;
                    case "BadgeList": BadgeList = b; break;
                    case "BadgeTaskbar": BadgeTaskbar = b; break;
                    case "HotkeyOn": HotkeyOn = b; break;
                    case "CloseToTray": CloseToTray = b; break;
                    case "Hotkey": Hotkey = v; break;
                    case "Lang": Lang = v.Trim(); break;
                    case "Workspace": Workspace = v.Trim(); break;
                    case "KeepInbox": KeepInbox = b; break;
                    case "DndUntil": long.TryParse(v, out DndUntil); break;
                    case "MemCapMB": MemCapMB = Math.Max(0, n); break;
                    case "UpdateUrl": UpdateUrl = v.Trim(); break;
                    case "PassSalt": PassSalt = v; break;
                    case "PassHash": PassHash = v; break;
                    case "DownloadDir": DownloadDir = v; break;
                }
            }
        }
        catch (Exception) { }   // first run: defaults
    }

    public static void Save()
    {
        StringBuilder s = new StringBuilder();
        s.AppendLine("Theme=" + Theme); s.AppendLine("Startup=" + Startup);
        s.AppendLine("LeftNav=" + B(LeftNav)); s.AppendLine("RailWide=" + B(RailWide)); s.AppendLine("Colorful=" + B(Colorful)); s.AppendLine("FilterDl=" + B(FilterDl));
        s.AppendLine("Notify=" + B(Notify)); s.AppendLine("BadgeList=" + B(BadgeList)); s.AppendLine("BadgeTaskbar=" + B(BadgeTaskbar));
        s.AppendLine("HotkeyOn=" + B(HotkeyOn)); s.AppendLine("CloseToTray=" + B(CloseToTray));
        s.AppendLine("Lang=" + Lang); s.AppendLine("Workspace=" + Workspace.Replace("\r", "").Replace("\n", "")); s.AppendLine("KeepInbox=" + B(KeepInbox));
        s.AppendLine("DndUntil=" + DndUntil); s.AppendLine("MemCapMB=" + MemCapMB); s.AppendLine("UpdateUrl=" + UpdateUrl.Replace("\r", "").Replace("\n", "")); s.AppendLine("Hotkey=" + Hotkey); s.AppendLine("PassSalt=" + PassSalt); s.AppendLine("PassHash=" + PassHash);
        s.AppendLine("DownloadDir=" + DownloadDir.Replace("\r", "").Replace("\n", ""));
        try
        {
            Directory.CreateDirectory(Paths.Data);
            File.WriteAllText(Paths.Settings + ".tmp", s.ToString());
            if (File.Exists(Paths.Settings)) File.Replace(Paths.Settings + ".tmp", Paths.Settings, null);
            else File.Move(Paths.Settings + ".tmp", Paths.Settings);
        }
        catch (Exception) { }
    }

    static string B(bool v) { return v ? "1" : "0"; }

    // ----- passcode: salted PBKDF2 hash, never the passcode itself -----
    public static bool HasPass { get { return PassHash.Length > 0; } }

    public static void SetPass(string pass)
    {
        if (pass == null || pass.Length == 0) { PassSalt = ""; PassHash = ""; return; }
        byte[] salt = new byte[16];
        using (RNGCryptoServiceProvider r = new RNGCryptoServiceProvider()) r.GetBytes(salt);
        PassSalt = Convert.ToBase64String(salt);
        PassHash = Hash(pass, salt);
    }

    public static bool CheckPass(string pass)
    {
        if (!HasPass) return true;
        return pass != null && Hash(pass, Convert.FromBase64String(PassSalt)) == PassHash;
    }

    static string Hash(string pass, byte[] salt)
    {
        using (Rfc2898DeriveBytes d = new Rfc2898DeriveBytes(pass, salt, 20000)) return Convert.ToBase64String(d.GetBytes(32));
    }
}

// Surfaces and text for light / dark (brand and status colors are in Ds).
static class Pal
{
    public static bool Dark = true;
    public static readonly Color Brand = Ds.Brand, Brand2 = Ds.Brand2;
    public static Color Bar, Page, Card, Text, Sub, Hover, Border;

    // Brand color that is readable as a thin line or small text on the current surfaces (focus rings, unread dots).
    public static Color BrandInk { get { return Dark ? Color.FromArgb(160, 155, 248) : Brand; } }

    public static void Update()
    {
        bool dark = Cfg.Theme == 2;
        if (Cfg.Theme == 0)
        {
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) dark = k == null || (int)k.GetValue("AppsUseLightTheme", 0) == 0; }
            catch (Exception) { dark = true; }
        }
        Set(dark);
        ToolStripManager.Renderer = new ThemeRenderer();
    }

    public static void Set(bool dark)
    {
        Dark = dark;
        if (dark)
        {
            Bar = Color.FromArgb(24, 23, 34); Page = Color.FromArgb(29, 28, 41); Card = Color.FromArgb(41, 40, 58);
            Text = Color.White; Sub = Color.FromArgb(176, 174, 196); Hover = Color.FromArgb(56, 54, 80); Border = Color.FromArgb(72, 70, 100);
        }
        else
        {
            Bar = Color.FromArgb(237, 236, 250); Page = Color.FromArgb(247, 246, 255); Card = Color.White;
            Text = Color.FromArgb(28, 26, 50); Sub = Color.FromArgb(96, 94, 125); Hover = Color.FromArgb(222, 220, 244); Border = Color.FromArgb(205, 203, 230);
        }
    }

    public static Color Blend(Color a, Color b, double t)
    {
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
}
// Pure helpers (also covered by --selftest).
static class Util
{
    public static string ProfileName(string s)
    {
        StringBuilder b = new StringBuilder();
        bool plain = true;
        foreach (char c in s) { if (c >= 128) plain = false; b.Append(char.IsLetterOrDigit(c) && c < 128 ? c : '_'); }
        // Arabic etc. would all become "___" and share one login, so add a hash of the name
        if (!plain) { int h = 17; foreach (char c in s) h = h * 31 + c; b.Append("_" + (h & 0x7FFFFFFF).ToString("x")); }
        return b.ToString();
    }

    public static int ParseUnread(string title)
    {
        if (title == null) return 0;
        Match m = Regex.Match(title, @"\((\d+)\)");
        int n;
        return m.Success && int.TryParse(m.Groups[1].Value, out n) ? n : 0;
    }

    // "Alt+E" -> modifiers (1 Alt, 2 Ctrl, 4 Shift, 8 Win) and key.
    public static bool ParseHotkey(string s, out uint mod, out Keys key)
    {
        mod = 0; key = Keys.None;
        foreach (string part in s.Split('+'))
        {
            string p = part.Trim();
            if (p == "Alt") mod |= 1; else if (p == "Ctrl") mod |= 2; else if (p == "Shift") mod |= 4; else if (p == "Win") mod |= 8;
            else { try { key = (Keys)Enum.Parse(typeof(Keys), p, true); } catch (Exception) { return false; } }
        }
        return key != Keys.None && mod != 0;
    }

    public static string SafeFileName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Length == 0 ? "download" : s;
    }

    public static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        string dir = Path.GetDirectoryName(path), name = Path.GetFileNameWithoutExtension(path), ext = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            string p = Path.Combine(dir, name + " (" + i + ")" + ext);
            if (!File.Exists(p)) return p;
        }
    }

    public static long DirSize(string dir)
    {
        long n = 0;
        try { foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) n += new FileInfo(f).Length; }
        catch (Exception) { }
        return n;
    }

    // Every catalog entry must be usable: unique name, https address (or a {placeholder} template), a color.
    static bool CatalogOk()
    {
        List<string> names = new List<string>();
        foreach (Service s in Catalog.All)
        {
            Uri u;
            bool ok = s.Name.Length > 0 && s.Category.Length > 0 && !names.Contains(s.Name) && s.Color != 0;
            if (s.IsTemplate) ok = ok && s.Url.IndexOf('}') > s.Url.IndexOf('{') && s.Url.StartsWith("https://");
            else ok = ok && Uri.TryCreate(s.Url, UriKind.Absolute, out u) && u.Scheme == "https";
            if (!ok) return false;
            names.Add(s.Name);
        }
        return Catalog.ColorFor("https://web.whatsapp.com/") == 0x25D366 && Catalog.ColorFor("https://example.org") == 0;
    }

    // Icon choice: apple-touch-icon first, then big PNG, then .ico; SVG/WebP skipped; private hosts refused.
    static bool IconPickOk()
    {
        string html = "<link rel=\"icon\" href=\"/a.svg\" type=\"image/svg+xml\"><link rel='shortcut icon' href='/small.ico'>" +
                      "<link rel=\"icon\" type=\"image/png\" sizes=\"192x192\" href=\"/big.png\"><link rel=\"apple-touch-icon\" sizes=\"180x180\" href=\"/touch.png\">" +
                      "<link rel=\"icon\" href=\"https://127.0.0.1/evil.png\">";
        List<KeyValuePair<int, Uri>> c = Icons.Candidates(new Uri("https://site.example.com/app"), html, null);
        return c.Count >= 3 && c[0].Value.AbsolutePath == "/touch.png" && c[1].Value.AbsolutePath == "/big.png" &&
               !c.Exists(delegate(KeyValuePair<int, Uri> k) { return k.Value.AbsolutePath == "/a.svg" || k.Value.Host == "127.0.0.1"; });
    }

    // WCAG contrast ratio between two colors (1 to 21).
    public static double Contrast(Color a, Color b)
    {
        double la = Lum(a), lb = Lum(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    static double Lum(Color c)
    {
        double[] v = { c.R / 255.0, c.G / 255.0, c.B / 255.0 };
        for (int i = 0; i < 3; i++) v[i] = v[i] <= 0.03928 ? v[i] / 12.92 : Math.Pow((v[i] + 0.055) / 1.055, 2.4);
        return 0.2126 * v[0] + 0.7152 * v[1] + 0.0722 * v[2];
    }

    // The palette must stay readable in both themes: text 4.5:1, interface lines and large text 3:1.
    static bool DesignOk()
    {
        bool was = Pal.Dark, ok = true;
        foreach (bool dark in new bool[] { true, false })
        {
            Pal.Set(dark);
            ok = ok && Contrast(Pal.Text, Pal.Page) >= 7 && Contrast(Pal.Text, Pal.Card) >= 7 && Contrast(Pal.Text, Pal.Bar) >= 7 &&
                 Contrast(Pal.Sub, Pal.Page) >= 4.5 && Contrast(Pal.Sub, Pal.Card) >= 4.5 && Contrast(Pal.Sub, Pal.Bar) >= 4.5 &&
                 Contrast(Color.White, Ds.Brand) >= 4.5 && Contrast(Color.White, Ds.Danger) >= 4.5 &&
                 Contrast(Pal.BrandInk, Pal.Page) >= 4.5 && Contrast(Pal.BrandInk, Pal.Card) >= 4.5 &&
                 Contrast(Color.White, Ds.Brand2) >= 3;
        }
        Pal.Set(was);
        return ok;
    }

    static bool PassRoundTrip()
    {
        string salt = Cfg.PassSalt, hash = Cfg.PassHash;
        Cfg.SetPass("p@ss");
        bool ok = Cfg.HasPass && Cfg.CheckPass("p@ss") && !Cfg.CheckPass("nope") && !Cfg.CheckPass(null);
        Cfg.SetPass(null);
        ok = ok && !Cfg.HasPass && Cfg.CheckPass("anything");
        Cfg.PassSalt = salt; Cfg.PassHash = hash;
        return ok;
    }

    public static bool SelfTest()
    {
        string a = "شغل", b = "بيت";
        uint mod; Keys key;
        return ParseUnread("Inbox (781) - a@gmail.com - Gmail") == 781 && ParseUnread("(3) WhatsApp") == 3 &&
               ParseUnread("WhatsApp") == 0 && ParseUnread(null) == 0 && ParseUnread("Chapter (x)") == 0 &&
               ProfileName("Gmail Business") == "Gmail_Business" &&
               ProfileName(a) != ProfileName(b) && ProfileName(a).Length > 0 &&
               ParseHotkey("Alt+E", out mod, out key) && mod == 1 && key == Keys.E &&
               ParseHotkey("Ctrl+Shift+F5", out mod, out key) && mod == 6 && key == Keys.F5 &&
               !ParseHotkey("E", out mod, out key) && !ParseHotkey("Alt+Nope", out mod, out key) &&
               SafeFileName("a/b:c.txt") == "a_b_c.txt" && PassRoundTrip() && DesignOk() && Loc.Ok() && CatalogOk() && IconPickOk();
    }
}
