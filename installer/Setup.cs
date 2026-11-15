using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

// LiteHub-Setup.exe: per-user installer (no admin). The release zip is embedded as the resource "payload.zip".
//   Setup.exe            install with a small window
//   Setup.exe /S         silent install        (/D=C:\path picks the folder, /NODESKTOP skips the desktop shortcut)
//   Setup.exe /uninstall remove it             (add /S to skip questions; /KEEPDATA is the default in silent mode)
static class Setup
{
    const string App = "LiteHub";
    static readonly string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + App;

    static string Arg(string[] a, string prefix)
    {
        foreach (string s in a) if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return s.Substring(prefix.Length).Trim('"');
        return null;
    }
    static bool Has(string[] a, string flag) { return Array.Exists(a, delegate(string s) { return s.Equals(flag, StringComparison.OrdinalIgnoreCase); }); }

    static string DefaultDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", App); } }
    static string StartMenuLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), App + ".lnk"); } }
    static string DesktopLink { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), App + ".lnk"); } }

    [STAThread]
    static int Main(string[] args)
    {
        bool silent = Has(args, "/S");
        try
        {
            if (Has(args, "/uninstall")) return Uninstall(silent);
            string dir = Arg(args, "/D=") ?? DefaultDir;
            if (silent) { Install(dir, !Has(args, "/NODESKTOP")); return 0; }
            Application.EnableVisualStyles();
            Application.Run(new SetupForm(dir));
            return 0;
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show(ex.Message, App + " Setup");
            return 1;
        }
    }

    public static void Install(string dir, bool desktop)
    {
        foreach (Process p in Process.GetProcessesByName(App)) { try { p.Kill(); p.WaitForExit(5000); } catch (Exception) { } }
        Directory.CreateDirectory(dir);
        string root = Path.GetFullPath(dir) + Path.DirectorySeparatorChar;
        using (Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
        using (ZipArchive zip = new ZipArchive(res))
            foreach (ZipArchiveEntry e in zip.Entries)
            {
                string target = Path.GetFullPath(Path.Combine(dir, e.FullName));
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;   // never write outside the install folder
                if (e.FullName.EndsWith("/")) { Directory.CreateDirectory(target); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (Stream src = e.Open()) using (FileStream dst = File.Create(target)) src.CopyTo(dst);
            }
        string exe = Path.Combine(dir, App + ".exe");
        string uninstaller = Path.Combine(dir, "Uninstall.exe");
        File.Copy(Assembly.GetExecutingAssembly().Location, uninstaller, true);
        Link(StartMenuLink, exe, dir);
        if (desktop) Link(DesktopLink, exe, dir);
        string ver = FileVersionInfo.GetVersionInfo(exe).ProductVersion;
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            k.SetValue("DisplayName", App); k.SetValue("DisplayVersion", ver); k.SetValue("Publisher", "Ahmed Hussein"); k.SetValue("URLInfoAbout", "https://github.com/Ebnhussein/LiteHub");
            k.SetValue("InstallLocation", dir); k.SetValue("DisplayIcon", exe);
            k.SetValue("UninstallString", "\"" + uninstaller + "\" /uninstall");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord); k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
    }

    static void Link(string path, string target, string workDir)
    {
        Type t = Type.GetTypeFromProgID("WScript.Shell");
        object sh = Activator.CreateInstance(t);
        object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { path });
        Type lt = lnk.GetType();
        lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
        lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
        lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { target + ",0" });
        lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
    }

    static bool LinkInto(string path, string dir)
    {
        if (!File.Exists(path)) return false;
        Type t = Type.GetTypeFromProgID("WScript.Shell");
        object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, Activator.CreateInstance(t), new object[] { path });
        string target = lnk.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, lnk, null) as string;
        return !string.IsNullOrEmpty(target) && target.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase);
    }

    static int Uninstall(bool silent)
    {
        string dir = null;
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(UninstallKey)) if (k != null) dir = k.GetValue("InstallLocation") as string;
        if (string.IsNullOrEmpty(dir)) dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!silent && MessageBox.Show("Remove " + App + " from this PC?", App + " Setup", MessageBoxButtons.YesNo) != DialogResult.Yes) return 0;
        bool wipe = !silent && MessageBox.Show("Also delete your accounts, logins and settings?\n(Choose No to keep them for a later install.)", App + " Setup", MessageBoxButtons.YesNo) == DialogResult.Yes;
        foreach (Process p in Process.GetProcessesByName(App)) { try { p.Kill(); p.WaitForExit(5000); } catch (Exception) { } }
        foreach (string f in new string[] { StartMenuLink, DesktopLink }) { try { if (LinkInto(f, dir)) File.Delete(f); } catch (Exception) { } }   // only our own shortcuts
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);
        if (wipe) { try { Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), App), true); } catch (Exception) { } }
        // this exe lives in the folder being removed: let a helper delete it once we have exited
        string me = Assembly.GetExecutingAssembly().Location;
        if (me.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
        {
            foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) { if (!f.Equals(me, StringComparison.OrdinalIgnoreCase)) { try { File.Delete(f); } catch (Exception) { } } }
            Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"" + dir + "\"") { CreateNoWindow = true, UseShellExecute = false });
        }
        else try { Directory.Delete(dir, true); } catch (Exception) { }
        if (!silent) MessageBox.Show(App + " was removed.", App + " Setup");
        return 0;
    }
}

class SetupForm : Form
{
    public SetupForm(string dir)
    {
        Text = "LiteHub Setup"; ClientSize = new Size(460, 250); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(29, 28, 41); ForeColor = Color.White;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
        PictureBox logo = new PictureBox(); logo.SetBounds(24, 22, 48, 48); logo.SizeMode = PictureBoxSizeMode.Zoom;
        try { logo.Image = new Icon(Icon, 48, 48).ToBitmap(); } catch (Exception) { }
        Label t = new Label(); t.Text = "LiteHub"; t.Font = new Font("Segoe UI Semibold", 18f); t.SetBounds(84, 22, 340, 34); t.AutoSize = false;
        Label sub = new Label(); sub.Text = "All your web accounts in one light window."; sub.ForeColor = Color.FromArgb(176, 174, 196); sub.SetBounds(86, 56, 340, 22);
        Label where = new Label(); where.Text = "Install to:  " + dir; where.SetBounds(24, 96, 412, 22); where.AutoEllipsis = true;
        CheckBox desk = new CheckBox(); desk.Text = "Create a desktop shortcut"; desk.Checked = true; desk.SetBounds(24, 126, 300, 24);
        CheckBox run = new CheckBox(); run.Text = "Start LiteHub when setup finishes"; run.Checked = true; run.SetBounds(24, 152, 300, 24);
        Button go = new Button(); go.Text = "Install"; go.SetBounds(330, 200, 106, 32); go.FlatStyle = FlatStyle.Flat; go.BackColor = Color.FromArgb(91, 84, 232); go.ForeColor = Color.White;
        go.FlatAppearance.BorderSize = 0; go.Cursor = Cursors.Hand;
        go.Click += delegate
        {
            go.Enabled = false; go.Text = "Installing...";
            try
            {
                Setup.Install(dir, desk.Checked);
                if (run.Checked) Process.Start(Path.Combine(dir, "LiteHub.exe"));
                Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "LiteHub Setup"); go.Enabled = true; go.Text = "Install"; }
        };
        Controls.AddRange(new Control[] { logo, t, sub, where, desk, run, go });
    }
}
