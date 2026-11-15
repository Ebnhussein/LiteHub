using System;
using System.Diagnostics;
using System.IO;
using System.Text;

// One log for everything that helps to find a problem: start-up, accounts starting and being freed, crashed or failing pages,
// unhandled errors, "window not responding" and a memory line every 10 minutes. It never contains message text, passwords or
// account names (an account shows as its site plus a short number), so it is safe to attach to a GitHub issue.
static class Log
{
    public const string Author = "Ahmed Hussein";
    public const string Email = "ebnhussein404@gmail.com";
    public const string RepoUrl = "https://github.com/Ebnhussein/LiteHub";
    public const string IssuesUrl = RepoUrl + "/issues/new?template=bug_report.md";
    public const string UpdateApi = "https://api.github.com/repos/Ebnhussein/LiteHub/releases/latest";

    static readonly object gate = new object();
    public static readonly Stopwatch Clock = Stopwatch.StartNew();

    public static string Dir { get { return Path.Combine(Paths.Data, "logs"); } }
    static string Today { get { return Path.Combine(Dir, "litehub-" + DateTime.Now.ToString("yyyyMMdd") + ".log"); } }

    public static void Info(string s) { Write("INFO ", s); }
    public static void Warn(string s) { Write("WARN ", s); }
    public static void Error(string s, Exception ex) { Write("ERROR", s + (ex == null ? "" : "\r\n" + ex)); }

    static void Write(string level, string s)
    {
        try
        {
            lock (gate)
            {
                Directory.CreateDirectory(Dir);
                string f = Today;
                if (File.Exists(f) && new FileInfo(f).Length > 2 * 1024 * 1024) File.Move(f, f + "." + DateTime.Now.ToString("HHmmss"));   // a runaway day cannot fill the disk
                File.AppendAllText(f, DateTime.Now.ToString("HH:mm:ss.fff") + " " + level + " " + s + "\r\n");
            }
        }
        catch (Exception) { }   // logging must never be the thing that breaks
    }

    // An account as "site#number": enough to tell two WhatsApps apart, nothing personal.
    public static string Who(AppDef a)
    {
        string host = "?";
        try { host = new Uri(a.Url).Host; } catch (Exception) { }
        return host + "#" + ((uint)(a.Id ?? "").GetHashCode() % 1000);
    }

    public static void Startup()
    {
        Info("---- LiteHub " + Application_Version + " started ----");
        Info(Os() + ", .NET " + Environment.Version + ", language " + Loc.Lang);
        Prune();
    }

    // Environment.OSVersion says 6.2 for programs without a Windows 10 manifest, so read the real name and build from the registry.
    static string Os()
    {
        string name = "Windows", build = "", disp = "";
        try
        {
            using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                if (k != null) { name = (k.GetValue("ProductName") ?? "Windows").ToString(); build = (k.GetValue("CurrentBuild") ?? "").ToString(); disp = (k.GetValue("DisplayVersion") ?? k.GetValue("ReleaseId") ?? "").ToString(); }
        }
        catch (Exception) { }
        return name + (disp.Length > 0 ? " " + disp : "") + (build.Length > 0 ? " (build " + build + ")" : "") + (Environment.Is64BitOperatingSystem ? ", 64-bit" : ", 32-bit");
    }

    static string Application_Version { get { return System.Windows.Forms.Application.ProductVersion; } }

    // Keep ten days of logs.
    static void Prune()
    {
        try { foreach (string f in Directory.GetFiles(Dir, "litehub-*")) if ((DateTime.Now - File.GetLastWriteTime(f)).TotalDays > 10) File.Delete(f); }
        catch (Exception) { }
    }

    public static string Tail(int lines)
    {
        try
        {
            string[] files = Directory.GetFiles(Dir, "litehub-*.log");
            if (files.Length == 0) return "";
            Array.Sort(files);
            string[] all = File.ReadAllLines(files[files.Length - 1]);
            StringBuilder sb = new StringBuilder();
            for (int i = Math.Max(0, all.Length - lines); i < all.Length; i++) sb.AppendLine(all[i]);
            return sb.ToString();
        }
        catch (Exception) { return ""; }
    }

    // What to paste into a bug report. No names, no messages.
    public static string Diagnostics(int accounts, int loaded)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("LiteHub " + Application_Version);
        sb.AppendLine(Os() + ", .NET " + Environment.Version);
        string wv = "?";
        try { wv = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString(); } catch (Exception) { wv = "not found"; }
        sb.AppendLine("WebView2 runtime " + wv);
        sb.AppendLine("Language " + Loc.Lang + ", theme " + Cfg.Theme + ", layout " + (Cfg.LeftNav ? (Cfg.RailWide ? "side bar" : "side bar (icons)") : "top bar") + ", notifications " + (Cfg.ToastStyle == 0 ? "LiteHub card" : "Windows"));
        sb.AppendLine("Accounts " + accounts + " (loaded " + loaded + "), memory limit " + Cfg.MemCapMB + " MB, uptime " + (int)(Clock.Elapsed.TotalMinutes) + " min");
        sb.AppendLine("Working set " + (Process.GetCurrentProcess().WorkingSet64 >> 20) + " MB");
        sb.AppendLine();
        sb.AppendLine("---- last log lines ----");
        sb.Append(Tail(60));
        return sb.ToString();
    }
}
