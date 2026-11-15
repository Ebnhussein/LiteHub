using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// Import: instead of adding accounts one by one, read the sites you already use from your browser's bookmarks and profiles
// (Chrome, Edge, Brave, Vivaldi, Opera) or from an exported bookmarks file. Everything is read locally; logins cannot be copied
// from a browser, so each imported account starts signed out.
class ImportItem
{
    public string Name, Url, Host, Source, Group;   // Group: gmail, apps, other
    public bool Keep, Ext, Checked, Existing;
    public int Color;
}

static class BrowserImport
{
    class Browser { public string Name, Root; public bool Single; }

    static string Host(string url)
    {
        Uri u;
        if (!Uri.TryCreate(url, UriKind.Absolute, out u)) return null;
        return u.Host.StartsWith("www.") ? u.Host.Substring(4) : u.Host;
    }

    static List<Browser> Browsers()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), roam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        List<Browser> l = new List<Browser>();
        string[][] all = {
            new string[] { "Chrome", Path.Combine(local, @"Google\Chrome\User Data") },
            new string[] { "Edge", Path.Combine(local, @"Microsoft\Edge\User Data") },
            new string[] { "Brave", Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data") },
            new string[] { "Vivaldi", Path.Combine(local, @"Vivaldi\User Data") } };
        foreach (string[] b in all) if (Directory.Exists(b[1])) l.Add(new Browser { Name = b[0], Root = b[1] });
        string opera = Path.Combine(roam, @"Opera Software\Opera Stable");
        if (Directory.Exists(opera)) l.Add(new Browser { Name = "Opera", Root = opera, Single = true });
        return l;
    }

    static string Read(string path)
    {
        try { FileInfo fi = new FileInfo(path); return fi.Exists && fi.Length < 40 * 1024 * 1024 ? File.ReadAllText(path) : null; }
        catch (Exception) { return null; }
    }

    // Chrome-family Bookmarks file: {"name": "...", "type": "url", "url": "..."} objects.
    public static List<KeyValuePair<string, string>> ParseChromium(string json)
    {
        List<KeyValuePair<string, string>> r = new List<KeyValuePair<string, string>>();
        if (json == null) return r;
        foreach (Match m in Regex.Matches(json, "\"name\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"[^{}]*?\"type\"\\s*:\\s*\"url\"[^{}]*?\"url\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
            try { r.Add(new KeyValuePair<string, string>(Regex.Unescape(m.Groups[1].Value), Regex.Unescape(m.Groups[2].Value))); } catch (Exception) { }
        return r;
    }

    // Netscape bookmarks HTML, which every browser can export.
    public static List<KeyValuePair<string, string>> ParseHtml(string html)
    {
        List<KeyValuePair<string, string>> r = new List<KeyValuePair<string, string>>();
        if (html == null) return r;
        foreach (Match m in Regex.Matches(html, "<A\\s[^>]*?HREF=\"([^\"]+)\"[^>]*>(.*?)</A>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            r.Add(new KeyValuePair<string, string>(WebUtility.HtmlDecode(Regex.Replace(m.Groups[2].Value, "<[^>]+>", "")), WebUtility.HtmlDecode(m.Groups[1].Value)));
        return r;
    }

    static List<KeyValuePair<string, string>> ParseAny(string text)
    {
        if (text == null) return new List<KeyValuePair<string, string>>();
        List<KeyValuePair<string, string>> r = text.IndexOf("\"type\"", StringComparison.Ordinal) >= 0 ? ParseChromium(text) : ParseHtml(text);
        if (r.Count == 0) foreach (Match m in Regex.Matches(text, "https?://[^\\s\"'<>,;]+")) r.Add(new KeyValuePair<string, string>("", m.Value));
        return r;
    }

    // Everything found, grouped: Gmail accounts of Chrome profiles, services from the catalog, and other websites.
    public static List<ImportItem> Scan(List<AppDef> existing, string file, out string detected)
    {
        List<ImportItem> res = new List<ImportItem>();
        List<string> found = new List<string>();
        Dictionary<string, bool> seen = new Dictionary<string, bool>();
        Service gmail = Array.Find(Catalog.All, delegate(Service s) { return s.Name == "Gmail"; });
        bool anyGmail = false;
        List<KeyValuePair<string, KeyValuePair<string, string>>> marks = new List<KeyValuePair<string, KeyValuePair<string, string>>>();   // source -> (title, url)

        foreach (Browser b in Browsers())
        {
            int before = marks.Count;
            if (b.Name == "Chrome")
            {
                string ls = Read(Path.Combine(b.Root, "Local State"));
                if (ls != null && gmail != null)
                    foreach (Match m in Regex.Matches(ls, "\"(?:Default|Profile \\d+)\"\\s*:\\s*\\{[^{}]*?\"user_name\"\\s*:\\s*\"([^\"]+)\""))
                    {
                        string mail = m.Groups[1].Value;
                        if (mail.IndexOf('@') < 0 || seen.ContainsKey("mail:" + mail)) continue;
                        seen["mail:" + mail] = true; anyGmail = true;
                        res.Add(new ImportItem { Name = "Gmail (" + mail + ")", Url = gmail.Url, Host = "mail.google.com", Source = "Chrome", Group = "gmail", Keep = false, Ext = gmail.Ext, Color = gmail.Color });
                    }
            }
            List<string> files = new List<string>();
            if (b.Single) files.Add(Path.Combine(b.Root, "Bookmarks"));
            else
                foreach (string dir in Directory.GetDirectories(b.Root))
                {
                    string n = Path.GetFileName(dir);
                    if (n == "Default" || n.StartsWith("Profile ")) files.Add(Path.Combine(dir, "Bookmarks"));
                }
            foreach (string f in files) foreach (KeyValuePair<string, string> kv in ParseChromium(Read(f))) marks.Add(new KeyValuePair<string, KeyValuePair<string, string>>(b.Name, kv));
            found.Add(b.Name);
        }
        if (!string.IsNullOrEmpty(file))
        {
            foreach (KeyValuePair<string, string> kv in ParseAny(Read(file))) marks.Add(new KeyValuePair<string, KeyValuePair<string, string>>(Path.GetFileName(file), kv));
            found.Add(Path.GetFileName(file));
        }

        int others = 0;
        foreach (KeyValuePair<string, KeyValuePair<string, string>> mk in marks)
        {
            string host = Host(mk.Value.Value);
            if (host == null || !(mk.Value.Value.StartsWith("http://") || mk.Value.Value.StartsWith("https://"))) continue;
            Service svc = Array.Find(Catalog.All, delegate(Service s) { return !s.IsTemplate && Host(s.Url) == host; });
            if (svc != null)
            {
                if (svc == gmail && anyGmail) continue;   // the profile emails already cover Gmail
                if (seen.ContainsKey("svc:" + svc.Name)) continue;
                seen["svc:" + svc.Name] = true;
                res.Add(new ImportItem { Name = svc.Name, Url = svc.Url, Host = host, Source = mk.Key, Group = "apps", Keep = svc.Keep, Ext = svc.Ext, Color = svc.Color });
            }
            else if (mk.Value.Value.StartsWith("https://") && others < 300 && !seen.ContainsKey("host:" + host) && host.IndexOf('.') > 0)
            {
                seen["host:" + host] = true; others++;
                string title = (mk.Value.Key ?? "").Replace("|", " ").Trim();
                if (title.Length == 0 || title.Length > 40) title = host;
                res.Add(new ImportItem { Name = title, Url = new Uri(mk.Value.Value).GetLeftPart(UriPartial.Authority), Host = host, Source = mk.Key, Group = "other" });
            }
        }
        foreach (ImportItem it in res)
        {
            ImportItem cur = it;
            cur.Existing = existing.Exists(delegate(AppDef a) { return Host(a.Url) == cur.Host && (cur.Group != "gmail" || a.Name == cur.Name); });
            cur.Checked = !cur.Existing && cur.Group != "other";
        }
        detected = found.Count == 0 ? "" : string.Join(", ", found.ToArray());
        return res;
    }
}

// "Import accounts" screen.
class ImportPanel : Panel
{
    readonly MainForm m;
    readonly FlowLayoutPanel list = new FlowLayoutPanel();
    readonly Label summary;
    readonly Button go;
    List<ImportItem> items;
    string detected = "";
    bool showOther;
    int builtW;

    public ImportPanel(MainForm m)
    {
        this.m = m;
        Dock = DockStyle.Fill; BackColor = Pal.Page;
        Label t = Ui.Text("Import accounts", Ds.Title, true, false); t.Location = new Point(Ds.PageX, Ds.PageTop); Controls.Add(t);
        Label d = Ui.Text("Pick the sites you already use. LiteHub reads your browser's bookmarks and profile list on this PC only and uploads nothing. Logins cannot be copied from a browser, so you sign in once in each new account.", Ds.Caption + 0.5f, false, true);
        d.MaximumSize = new Size(760, 0); d.Location = new Point(Ds.PageX, 68); Controls.Add(d);
        Button file = Ui.Btn("Import from a file...", 170, delegate { PickFile(); });
        Button all = Ui.Btn("Select all", 100, delegate { foreach (ImportItem i in items) if (i.Group != "other" || showOther) i.Checked = true; Fill(); });
        Button none = Ui.Btn("None", 80, delegate { foreach (ImportItem i in items) i.Checked = false; Fill(); });
        file.Location = new Point(Ds.PageX, 124); all.Location = new Point(Ds.PageX + 178, 124); none.Location = new Point(Ds.PageX + 286, 124);
        Controls.AddRange(new Control[] { file, all, none });
        list.FlowDirection = FlowDirection.TopDown; list.WrapContents = false; list.AutoScroll = true; list.BackColor = Pal.Page;
        list.Location = new Point(Ds.PageX, 168); list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(list); Ui.DarkScroll(list);
        summary = Ui.Text("", Ds.Body, false, true); summary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; Controls.Add(summary);
        go = Ui.Primary(Ui.Btn("Import", 190, delegate { DoImport(); })); go.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; Controls.Add(go);
        Resize += delegate
        {
            list.Size = new Size(Math.Max(300, Width - 2 * Ds.PageX), Math.Max(100, Height - 168 - 64));
            summary.Location = new Point(Ds.PageX, Height - 44); go.Location = new Point(Width - Ds.PageX - go.Width, Height - 50);
            if (items != null && Math.Abs(list.ClientSize.Width - builtW) > 40) Fill();   // rows follow the window width
        };
        items = BrowserImport.Scan(m.Apps, null, out detected);
        Fill();
    }

    void PickFile()
    {
        using (OpenFileDialog f = new OpenFileDialog())
        {
            f.Filter = "Bookmarks (*.html;*.htm;*.json;*.txt;*.csv)|*.html;*.htm;*.json;*.txt;*.csv|All files|*.*";
            if (f.ShowDialog() != DialogResult.OK) return;
            string d2;
            List<ImportItem> more = BrowserImport.Scan(m.Apps, f.FileName, out d2);
            foreach (ImportItem it in more)
                if (!items.Exists(delegate(ImportItem x) { return x.Host == it.Host && x.Name == it.Name; })) items.Add(it);
            detected = d2; Fill();
        }
    }

    void Heading(string text) { Label l = Ui.Text(text, Ds.Heading, true, false); l.Margin = new Padding(0, 14, 0, 6); list.Controls.Add(l); }

    void Fill()
    {
        builtW = list.ClientSize.Width;
        int w = Math.Max(320, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        list.SuspendLayout(); list.Controls.Clear();
        if (items.Count == 0)
        {
            Label e = Ui.Text("No browser data found. Use Import from a file... with a bookmarks file exported from your browser.", Ds.Body, false, true);
            e.Margin = new Padding(0, 20, 0, 0); list.Controls.Add(e);
        }
        string[] groups = { "gmail", "apps", "other" };
        string[] titles = { Loc.F("Gmail accounts found in {0}", "Chrome"), "Apps found in your bookmarks", "Other websites" };
        for (int g = 0; g < 3; g++)
        {
            List<ImportItem> part = items.FindAll(delegate(ImportItem i) { return i.Group == groups[g]; });
            if (part.Count == 0) continue;
            if (g == 2 && !showOther)
            {
                Button more = Ui.Btn(Loc.F("Show other websites ({0})", part.Count), 260, delegate { showOther = true; Fill(); });
                more.Margin = new Padding(0, 16, 0, 0); list.Controls.Add(more);
                continue;
            }
            Heading(titles[g]);
            foreach (ImportItem it in part)
            {
                ImportItem item = it;
                Toggle tg = Ui.Switch(item.Checked, delegate(object s, EventArgs e) { item.Checked = ((Toggle)s).Checked; UpdateSummary(); });
                string hint = item.Host + "  -  " + item.Source + (item.Existing ? "  -  " + Loc.T("Already added") : "");
                list.Controls.Add(Ui.Row(w, item.Name, hint, tg));
            }
        }
        list.Controls.Add(new Panel { Width = 1, Height = 24 });
        list.ResumeLayout();
        UpdateSummary();
    }

    void UpdateSummary()
    {
        int n = items.FindAll(delegate(ImportItem i) { return i.Checked; }).Count;
        summary.Text = (detected.Length > 0 ? Loc.T("Found in") + ": " + detected + "   " : "") + Loc.F("{0} selected", n);
        go.Text = Loc.F("Import {0} accounts", n); go.Enabled = n > 0;
    }

    void DoImport()
    {
        List<ImportItem> sel = items.FindAll(delegate(ImportItem i) { return i.Checked; });
        if (sel.Count == 0) return;
        m.ImportAccounts(sel);
    }
}
