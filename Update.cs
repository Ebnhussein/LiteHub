using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

// Looks for a newer GitHub release and, with the user's OK, downloads that release's Setup.exe,
// checks it against the published SHA256 and runs it silently over the current install.
static class Updater
{
    static string offered = "";   // newest version already offered in this run, so the timer does not ask twice
    static bool busy;

    // manual: the user clicked "Check", so always answer. Otherwise only speak up when a newer version exists.
    public static async void Run(bool manual)
    {
        if (busy) return;
        busy = true;
        try
        {
            string url = Cfg.UpdateUrl; Uri u;
            if (url.Length == 0 || !Uri.TryCreate(url, UriKind.Absolute, out u) || (u.Scheme != "http" && u.Scheme != "https"))
            { if (manual) MessageBox.Show(Loc.T("No update address is set. Add a line UpdateUrl=... to settings.txt."), "LiteHub"); return; }
            string json = await Task.Run<string>(new Func<string>(delegate
            {
                using (WebClient c = new WebClient()) { c.Headers[HttpRequestHeader.UserAgent] = "LiteHub"; return c.DownloadString(url); }
            }));
            Match tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
            Match link = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
            Version cur = new Version(Application.ProductVersion), latest;
            if (!tag.Success || !Version.TryParse(tag.Groups[1].Value.TrimStart('v', 'V'), out latest)) throw new FormatException();
            if (latest <= cur) { if (manual) MessageBox.Show(Loc.F("You have the latest version ({0}).", cur), "LiteHub"); return; }
            if (!manual && offered == latest.ToString()) return;
            offered = latest.ToString();

            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            if (!Paths.Portable && File.Exists(Path.Combine(dir, "Uninstall.exe")))   // installed by Setup.exe: update in place
            {
                if (MessageBox.Show(Loc.F("Version {0} is available. Update now?", latest), "LiteHub", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                await Install(latest, dir);
            }
            else if (MessageBox.Show(Loc.F("Version {0} is available. Open the download page?", latest), "LiteHub", MessageBoxButtons.YesNo) == DialogResult.Yes
                && link.Success && link.Groups[1].Value.StartsWith("https://"))
                Process.Start(link.Groups[1].Value);
        }
        catch (Exception ex) { Log.Info("update check failed: " + ex.Message); if (manual) MessageBox.Show(Loc.T("Could not check for updates."), "LiteHub"); }
        finally { busy = false; }
    }

    static async Task Install(Version v, string dir)
    {
        string name = "LiteHub-" + v + "-Setup.exe", root = Log.RepoUrl + "/releases/download/v" + v + "/";
        string file = Path.Combine(Path.GetTempPath(), name);
        bool ok = await Task.Run<bool>(new Func<bool>(delegate
        {
            using (WebClient c = new WebClient())
            {
                c.Headers[HttpRequestHeader.UserAgent] = "LiteHub";
                string want = Regex.Match(c.DownloadString(root + name + ".sha256"), "[0-9A-Fa-f]{64}").Value;
                c.DownloadFile(root + name, file);
                using (FileStream s = File.OpenRead(file))
                using (SHA256 sha = SHA256.Create())
                    return want.Length == 64 && BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").Equals(want, StringComparison.OrdinalIgnoreCase);
            }
        }));
        if (!ok) { try { File.Delete(file); } catch (Exception) { } throw new InvalidDataException("update file did not match its checksum"); }
        Log.Info("updating to " + v);
        Process.Start(file, "/S /RUN /NODESKTOP /D=\"" + dir + "\"");   // Setup closes this program, replaces the files and starts the new one
        Application.Exit();
    }
}
