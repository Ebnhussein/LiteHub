using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// Account tile: rounded square (logo or colored letter), brand pill when selected, green dot while running, red count badge.
class AccountIcon : Control
{
    public AppDef App;
    public bool Selected, Hover;

    public AccountIcon(AppDef a)
    {
        App = a;
        Size = new Size(52, 48);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent;
        AllowDrop = true;
    }

    // Files dropped on a tile are sent to that account (dropped into its page, e.g. an open WhatsApp chat).
    public static Action<AppDef, string[]> FilesDropped;
    bool dropHover;

    protected override void OnDragEnter(DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) { e.Effect = DragDropEffects.Copy; dropHover = true; Invalidate(); } base.OnDragEnter(e); }
    protected override void OnDragOver(DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; base.OnDragOver(e); }
    protected override void OnDragLeave(EventArgs e) { dropHover = false; Invalidate(); base.OnDragLeave(e); }
    protected override void OnDragDrop(DragEventArgs e)
    {
        dropHover = false; Invalidate();
        string[] f = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (f != null && FilesDropped != null) FilesDropped(App, f);
        base.OnDragDrop(e);
    }

    // Drag a tile onto another one to reorder (a plain click still opens the account).
    public static AppDef DropTarget;
    public static Action<AppDef, AppDef> Reorder;
    Point downAt; bool downActive, dragging, justDragged;

    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { downAt = e.Location; downActive = true; } base.OnMouseDown(e); }

    AppDef TargetAt(Point screen)
    {
        if (Parent == null) return null;
        foreach (Control c in Parent.Controls)
        {
            AccountIcon t = c as AccountIcon;
            if (t != null && t.Visible && t.RectangleToScreen(t.ClientRectangle).Contains(screen)) return t.App;
        }
        return null;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!downActive) return;
        if (!dragging && (Math.Abs(e.X - downAt.X) > 6 || Math.Abs(e.Y - downAt.Y) > 6)) { dragging = true; Capture = true; Cursor = Cursors.SizeAll; }
        if (!dragging) return;
        AppDef t = TargetAt(Control.MousePosition);
        if (t != DropTarget)
        {
            AppDef old = DropTarget; DropTarget = t;
            foreach (Control c in Parent.Controls) { AccountIcon i = c as AccountIcon; if (i != null && (i.App == old || i.App == t)) i.Invalidate(); }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        downActive = false;
        if (!dragging) return;
        dragging = false; Capture = false; Cursor = Cursors.Hand; justDragged = true;
        AppDef t = DropTarget; DropTarget = null;
        if (Parent != null) foreach (Control c in Parent.Controls) c.Invalidate();
        if (t != null && t != App && Reorder != null) Reorder(App, t);
    }

    protected override void OnClick(EventArgs e) { if (justDragged) { justDragged = false; return; } base.OnClick(e); }

    // Side bar with names (wide) or icons only.
    public static bool Wide { get { return Cfg.LeftNav && Cfg.RailWide; } }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        Control p = Parent;
        while (p != null && !(p is IconHost)) p = p.Parent;
        if (p != null) ((IconHost)p).ScrollBy(e.Delta / 2);
        base.OnMouseWheel(e);
    }

    protected override void OnMouseEnter(EventArgs e) { Hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { Hover = false; Invalidate(); base.OnMouseLeave(e); }

    public static GraphicsPath Rounded(Rectangle r, int rad)
    {
        rad = Math.Max(1, Math.Min(rad, Math.Min(r.Width, r.Height) / 2));
        GraphicsPath p = new GraphicsPath(); int d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.CompositingQuality = CompositingQuality.HighQuality;
        Color bg = Pal.Bar;   // background comes from the parent (rail gradient)
        Rectangle r = new Rectangle(8, 6, 36, 36);
        if (Selected || Hover)
            using (GraphicsPath hp = Rounded(new Rectangle(4, 2, Wide ? Width - 8 : 44, 44), 14))
            using (Brush hb = new SolidBrush(Selected ? Pal.Blend(bg, App.AccentColor, 0.35) : Pal.Hover)) g.FillPath(hb, hp);
        if (Selected) using (Brush pb = new SolidBrush(Pal.Brand)) g.FillRectangle(pb, 0, 13, 3, 22);
        using (GraphicsPath p = Rounded(r, Ds.RTile))
        {
            if (!App.LetterThumb && App.Icon != null)
            {
                if (App.Icon.Width >= 96) { g.SetClip(p); g.DrawImage(App.Icon, r); g.ResetClip(); }
                else   // small source icon: sharp, centered on a light tile instead of a blurry stretch
                {
                    using (Brush tb = new SolidBrush(Color.FromArgb(Pal.Dark ? 235 : 255, 255, 255, 255))) g.FillPath(tb, p);
                    int sz = App.Icon.Width <= 20 ? App.Icon.Width : 24;
                    g.DrawImage(App.Icon, new Rectangle(r.X + (r.Width - sz) / 2, r.Y + (r.Height - sz) / 2, sz, sz));
                }
            }
            else
            {
                using (Brush b = new SolidBrush(App.AccentColor)) g.FillPath(b, p);
                using (Font f = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(App.Name.Substring(0, 1).ToUpper(), f, Ui.Light(App.AccentColor) ? Brushes.Black : Brushes.White, r, sf);
            }
        }
        if (App.HasSibling)   // several accounts of one platform: ring in the account color
            using (GraphicsPath rp = Rounded(new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), 13))
            using (Pen pen = new Pen(App.AccentColor, 2.5f)) g.DrawPath(pen, rp);
        if (App.Running)
        {
            using (Brush ob = new SolidBrush(bg)) g.FillEllipse(ob, r.Right - 9, r.Bottom - 9, 12, 12);
            using (Brush gb = new SolidBrush(Ds.Success)) g.FillEllipse(gb, r.Right - 7, r.Bottom - 7, 8, 8);
        }
        if (App.Locked)
        {
            using (Brush lb = new SolidBrush(Pal.Card)) g.FillEllipse(lb, r.X - 4, r.Bottom - 10, 14, 14);
            using (Font lf = new Font("Segoe MDL2 Assets", 7f)) TextRenderer.DrawText(g, "", lf, new Point(r.X - 3, r.Bottom - 8), Pal.Text);
        }
        if (dropHover)
            using (GraphicsPath dp = Rounded(new Rectangle(2, 2, Width - 5, Height - 5), 13)) using (Pen dpen = new Pen(Ds.Success, 3f)) g.DrawPath(dpen, dp);
        if (DropTarget == App)
            using (GraphicsPath dp = Rounded(new Rectangle(2, 2, Width - 5, Height - 5), 13)) using (Pen dpen = new Pen(Pal.BrandInk, 2.5f)) g.DrawPath(dpen, dp);
        if (Wide)
            using (Font nf = new Font("Segoe UI", 10f, Selected ? FontStyle.Bold : FontStyle.Regular))
                TextRenderer.DrawText(g, App.Name, nf, new Rectangle(58, 0, Width - 58 - (App.Unread > 0 && Cfg.BadgeList && App.Badge ? 46 : 12), Height), Pal.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (App.Unread > 0 && Cfg.BadgeList && App.Badge)
        {
            string t = App.Unread > 999 ? "999+" : App.Unread.ToString();
            using (Font f = new Font("Segoe UI", 7f, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(t, f);
                float w = Math.Max(16, sz.Width + 4);
                RectangleF br = Wide ? new RectangleF(Width - w - 12, 17, w, 15) : new RectangleF(Width - w - 1, 0, w, 15);
                using (Brush rb = new SolidBrush(Ds.Danger)) g.FillEllipse(rb, br);
                g.DrawString(t, f, Brushes.White, br.X + (w - sz.Width) / 2, 1);
            }
        }
    }
}
// On/off switch.
class Toggle : Control
{
    bool on;
    public event EventHandler Changed;
    public Color OnColor = Ds.Success;   // iOS switches are green when on

    public Toggle() { Size = new Size(48, 28); Cursor = Cursors.Hand; DoubleBuffered = true; }

    public bool Checked
    {
        get { return on; }
        set { if (on != value) { on = value; Invalidate(); } }
    }

    protected override void OnClick(EventArgs e)
    {
        if (!Enabled) return;
        on = !on; Invalidate();
        if (Changed != null) Changed(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent != null ? Parent.BackColor : Pal.Card);
        Color track = on ? OnColor : Pal.Blend(Pal.Card, Pal.Dark ? Color.White : Color.Black, Pal.Dark ? 0.22 : 0.16);
        if (!Enabled) track = Pal.Blend(Pal.Card, track, 0.4);
        Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath p = AccountIcon.Rounded(r, Height / 2)) using (Brush b = new SolidBrush(track)) g.FillPath(b, p);
        int k = Height - 6, x = on ? Width - k - 3 : 3;
        using (Brush sh = new SolidBrush(Color.FromArgb(50, 0, 0, 0))) g.FillEllipse(sh, x, 4, k, k);
        g.FillEllipse(Brushes.White, x, 3, k, k);
    }
}
static class Ui
{
    public static Label Text(string text, float size, bool bold, bool sub)
    {
        Label l = new Label();
        l.Text = Loc.T(text); l.AutoSize = true; l.BackColor = Color.Transparent;
        if (Loc.IsRtl) l.RightToLeft = RightToLeft.Yes;   // right order for Arabic text mixed with names like LiteHub
        l.ForeColor = sub ? Pal.Sub : Pal.Text;
        l.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
        return l;
    }

    // A row with a title, a hint line, and a control on the right.
    public static Panel Row(int width, string title, string hint, Control right)
    {
        Panel p = new GlassPanel();
        p.Size = new Size(width, Ds.RowH);
        p.BackColor = Pal.Card;
        Label t = Text(title, Ds.Body, false, false); t.Location = new Point(Ds.RowPad, hint != null ? 16 : (Ds.RowH - 21) / 2);
        p.Controls.Add(t);
        if (hint != null) { Label h = Text(hint, Ds.Caption, false, true); h.Location = new Point(Ds.RowPad, 42); h.MaximumSize = new Size(width - 260, 0); p.Controls.Add(h); }
        if (right != null)
        {
            right.Location = new Point(width - right.Width - Ds.RowPad, (Ds.RowH - right.Height) / 2);
            right.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            p.Controls.Add(right);
        }
        p.Margin = new Padding(0, 0, 0, Ds.RowGap);
        return p;
    }

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr h, string app, string ids);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);

    // Gray placeholder text inside an empty box.
    public static void Cue(TextBox t, string text)
    {
        EventHandler apply = delegate { SendMessage(t.Handle, 0x1501, (IntPtr)1, Loc.T(text)); };
        if (t.IsHandleCreated) apply(t, EventArgs.Empty); else t.HandleCreated += apply;
    }

    // Dark scrollbars (Windows 10 1809+; harmless elsewhere).
    public static void DarkScroll(Control c)
    {
        EventHandler apply = delegate { try { SetWindowTheme(c.Handle, Pal.Dark ? "DarkMode_Explorer" : "Explorer", null); } catch (Exception) { } };
        if (c.IsHandleCreated) apply(c, EventArgs.Empty); else c.HandleCreated += apply;
    }

    // Brand-filled call-to-action button.
    public static Button Primary(Button b)
    {
        ((RoundButton)b).Primary = true; b.ForeColor = Color.White;
        return b;
    }

    public static bool Light(Color c) { return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B > 170; }

    // Round colored badge with the first letter (platform color), for the service list.
    public static PictureBox Dot(int rgb, string name, int size)
    {
        Bitmap bmp = new Bitmap(size, size);
        Color c = Color.FromArgb(rgb | unchecked((int)0xFF000000));
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Brush b = new SolidBrush(c)) g.FillEllipse(b, 0, 0, size - 1, size - 1);
            using (Font f = new Font("Segoe UI", size * 0.4f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(name.Substring(0, 1).ToUpper(), f, Light(c) ? Brushes.Black : Brushes.White, new RectangleF(0, 0, size, size), sf);
        }
        PictureBox p = new PictureBox(); p.Image = bmp; p.Size = new Size(size, size); p.BackColor = Color.Transparent;
        return p;
    }

    public static Toggle Switch(bool value, EventHandler changed)
    {
        Toggle t = new Toggle();
        t.Checked = value;
        t.Changed += changed;
        return t;
    }

    public static Button Btn(string text, int width, EventHandler click)
    {
        Button b = new RoundButton();
        b.Text = Loc.T(text); b.Width = width; b.Height = 30;
        b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Pal.Border;
        b.BackColor = Pal.Hover; b.ForeColor = Pal.Text; b.Cursor = Cursors.Hand;
        b.Font = new Font("Segoe UI", 9f);
        b.Click += click;
        return b;
    }

    public static ComboBox Combo(string[] items, int selected, int width, EventHandler changed)
    {
        ComboBox c = new GlassCombo();
        c.DropDownStyle = ComboBoxStyle.DropDownList; c.FlatStyle = FlatStyle.Flat;
        foreach (string it in items) c.Items.Add(Loc.T(it));
        c.SelectedIndex = selected; c.Width = width;
        c.BackColor = Pal.Hover; c.ForeColor = Pal.Text; c.Font = new Font("Segoe UI", 9.5f);
        c.SelectedIndexChanged += changed;
        return c;
    }

    public static TextBox Edit(string text, int width)
    {
        TextBox t = new GlassEdit();
        t.Text = text; t.Width = width;
        t.BackColor = Pal.Page; t.ForeColor = Pal.Text; t.Font = new Font("Segoe UI", Ds.Body);   // inputs are recessed: page color inside a card
        return t;
    }
}

// Passcode, name and hotkey dialogs (plain, themed forms).
static class Dialogs
{
    static Form Make(string title, int w, int h)
    {
        Form f = new Form();
        f.Text = Loc.T(title); f.FormBorderStyle = FormBorderStyle.FixedDialog; f.StartPosition = FormStartPosition.CenterScreen;
        f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false; f.TopMost = true;
        f.ClientSize = new Size(w, h); f.BackColor = Pal.Page; f.ForeColor = Pal.Text; f.Font = new Font("Segoe UI", 9.5f);
        return f;
    }

    public const string Reset = "@@RESET@@";

    // Passcode box for the launch lock: like Ask, plus a "Forgot passcode?" button that returns Reset.
    public static string AskPass(string title, string label)
    {
        using (Form f = Make(title, 380, 130))
        {
            Label l = new Label(); l.Text = Loc.T(label); l.SetBounds(14, 12, 350, 20);
            TextBox t = Ui.Edit("", 350); t.SetBounds(14, 38, 350, 26); t.UseSystemPasswordChar = true;
            Button ok = Ui.Btn("OK", 80, null); ok.DialogResult = DialogResult.OK; ok.Location = new Point(204, 84);
            Button cancel = Ui.Btn("Cancel", 80, null); cancel.DialogResult = DialogResult.Cancel; cancel.Location = new Point(290, 84);
            Button forgot = Ui.Btn("Forgot passcode?", 150, null); forgot.BackColor = Color.Transparent; forgot.ForeColor = Pal.Sub; forgot.DialogResult = DialogResult.Abort; forgot.Location = new Point(10, 84);
            f.Controls.AddRange(new Control[] { l, t, ok, cancel, forgot });
            f.AcceptButton = ok; f.CancelButton = cancel;
            DialogResult r = f.ShowDialog();
            return r == DialogResult.OK ? t.Text : r == DialogResult.Abort ? Reset : null;
        }
    }

    // Returns the typed text, or null if cancelled.
    public static string Ask(string title, string label, string initial, bool secret)
    {
        using (Form f = Make(title, 380, 130))
        {
            Label l = new Label(); l.Text = Loc.T(label); l.SetBounds(14, 12, 350, 20);
            TextBox t = Ui.Edit(initial, 350); t.SetBounds(14, 38, 350, 26); t.UseSystemPasswordChar = secret;
            Button ok = Ui.Btn("OK", 80, null); ok.DialogResult = DialogResult.OK; ok.Location = new Point(204, 84);
            Button cancel = Ui.Btn("Cancel", 80, null); cancel.DialogResult = DialogResult.Cancel; cancel.Location = new Point(290, 84);
            f.Controls.AddRange(new Control[] { l, t, ok, cancel });
            f.AcceptButton = ok; f.CancelButton = cancel;
            return f.ShowDialog() == DialogResult.OK ? t.Text : null;
        }
    }

    // First start: ask once for the things LiteHub needs to be useful. Everything can be changed later in Settings > General.
    public static void Setup()
    {
        using (Form f = Make("Welcome to LiteHub", 440, 250))
        {
            Label l = new Label(); l.Text = Loc.T("Choose what LiteHub may do. You can change this any time in Settings."); l.SetBounds(14, 12, 410, 40);
            CheckBox boot = new CheckBox(); boot.Text = Loc.T("Start with Windows, so notifications keep working"); boot.Checked = true; boot.SetBounds(14, 62, 410, 24);
            CheckBox wa = new CheckBox(); wa.Text = Loc.T("Open WhatsApp links from my browser in LiteHub"); wa.Checked = true; wa.SetBounds(14, 94, 410, 24);
            CheckBox nt = new CheckBox(); nt.Text = Loc.T("Show notifications for new messages"); nt.Checked = true; nt.SetBounds(14, 126, 410, 24);
            Button ok = Ui.Btn("OK", 80, null); ok.DialogResult = DialogResult.OK; ok.Location = new Point(344, 176);
            f.Controls.AddRange(new Control[] { l, boot, wa, nt, ok });
            f.AcceptButton = ok;
            f.ShowDialog();
            Cfg.Startup = boot.Checked ? 2 : 0; Cfg.LinkWa = wa.Checked; Cfg.Notify = nt.Checked; Cfg.Asked = true; Cfg.Save();
        }
    }

    // Press the keys you want; returns e.g. "Ctrl+Alt+M" or null.
    public static string RecordHotkey()
    {
        using (Form f = Make("Record shortcut", 340, 120))
        {
            Label l = new Label(); l.Text = Loc.T("Press the new shortcut (needs Alt, Ctrl, Shift or Win + a key)"); l.SetBounds(14, 14, 312, 40);
            Label shown = new Label(); shown.SetBounds(14, 60, 312, 24); shown.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            f.Controls.AddRange(new Control[] { l, shown });
            string result = null;
            f.KeyPreview = true;
            f.KeyDown += delegate(object s, KeyEventArgs e)
            {
                e.SuppressKeyPress = true;
                Keys k = e.KeyCode;
                if (k == Keys.ControlKey || k == Keys.Menu || k == Keys.ShiftKey || k == Keys.LWin || k == Keys.RWin) return;
                if (k == Keys.Escape) { f.Close(); return; }
                string s2 = (e.Control ? "Ctrl+" : "") + (e.Alt ? "Alt+" : "") + (e.Shift ? "Shift+" : "") + k;
                shown.Text = s2;
                if (e.Control || e.Alt || e.Shift) { result = s2; f.Close(); }
            };
            f.ShowDialog();
            return result;
        }
    }
}

// Anti-aliased rounded button. BackColor is the fill (Transparent = icon button that only shows on hover); Primary = brand gradient.
class RoundButton : Button
{
    public bool Primary, Bar;   // Bar = brand pill on the left edge (selected menu row)
    public int Badge;           // red count bubble (0 = none)
    public int Radius = Ds.RControl;
    bool hover, down;

    public RoundButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.Opaque, false);   // ButtonBase is Opaque by default, which skips OnPaintBackground
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = Enabled; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnEnabledChanged(EventArgs e) { hover = false; Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaintBackground(PaintEventArgs pe)
    {
        if (Parent == null) { pe.Graphics.Clear(Pal.Page); return; }
        for (Control a = Parent; a != null; a = a.Parent) if (a is RailPanel) { RailPanel.PaintFor(this, pe.Graphics); return; }
        GraphicsState st = pe.Graphics.Save();
        pe.Graphics.TranslateTransform(-Left, -Top);
        PaintEventArgs pa = new PaintEventArgs(pe.Graphics, new Rectangle(Left, Top, Width, Height));
        InvokePaintBackground(Parent, pa);
        InvokePaint(Parent, pa);   // include the parent card (GlassPanel draws its fill in OnPaint)
        pe.Graphics.Restore(st);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
        bool clear = BackColor.A == 0;
        Color tint = Pal.Dark ? Color.White : Color.Black;
        using (GraphicsPath p = AccountIcon.Rounded(r, Radius))
        {
            if (Primary)
                using (LinearGradientBrush b = new LinearGradientBrush(r, Pal.Blend(Pal.Brand, Color.White, hover ? 0.12 : 0), Pal.Blend(Pal.Brand2, Color.White, hover ? 0.12 : 0), 0f))
                    g.FillPath(b, p);
            else if (!clear || hover || down)
                using (Brush b = new SolidBrush(clear ? Pal.Hover : Pal.Blend(BackColor, tint, down ? 0.14 : hover ? 0.08 : 0)))
                    g.FillPath(b, p);
            if (!clear)
            {
                g.SetClip(p);
                using (LinearGradientBrush sh = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), Ds.Sheen, Color.FromArgb(0, Color.White), 90f))
                    g.FillRectangle(sh, 0, 0, Width, Height);
                g.ResetClip();
                using (Pen pen = new Pen(Primary ? Color.FromArgb(70, Color.White) : Ds.Hairline, 1f)) g.DrawPath(pen, p);
            }
        }
        if (Badge > 0)
        {
            string bt = Badge > 99 ? "99+" : Badge.ToString();
            using (Font bf = new Font("Segoe UI", 7f, FontStyle.Bold))
            {
                SizeF bs = g.MeasureString(bt, bf); float bw = Math.Max(15, bs.Width + 3);
                RectangleF br = new RectangleF(Width - bw, 0, bw, 14);
                using (Brush rb = new SolidBrush(Ds.Danger)) g.FillEllipse(rb, br);
                g.DrawString(bt, bf, Brushes.White, br.X + (bw - bs.Width) / 2, 0.5f);
            }
        }
        if (Bar) using (Brush pb = new SolidBrush(Pal.BrandInk)) g.FillRectangle(pb, 0, 9, 3, Height - 18);
        bool left = TextAlign == ContentAlignment.MiddleLeft;
        TextRenderer.DrawText(g, Text, Font, left ? new Rectangle(Padding.Left, 0, Width - Padding.Left, Height) : ClientRectangle, Enabled ? ForeColor : Pal.Blend(Pal.Card, ForeColor, 0.35),
            (left ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
}

// Rounded "glass" card: fill, soft top highlight, hairline border.
class GlassPanel : Panel
{
    public int Radius = Ds.RCard;

    public GlassPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs pe)
    {
        if (Parent == null) { pe.Graphics.Clear(Pal.Page); return; }
        GraphicsState st = pe.Graphics.Save();
        pe.Graphics.TranslateTransform(-Left, -Top);
        InvokePaintBackground(Parent, new PaintEventArgs(pe.Graphics, new Rectangle(Left, Top, Width, Height)));
        pe.Graphics.Restore(st);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath p = AccountIcon.Rounded(r, Radius))
        {
            using (Brush b = new SolidBrush(BackColor)) g.FillPath(b, p);
            g.SetClip(p);
            using (LinearGradientBrush sh = new LinearGradientBrush(new Rectangle(0, 0, Width, Math.Max(2, Height)), Ds.Sheen, Color.FromArgb(0, Color.White), 90f))
                g.FillRectangle(sh, 0, 0, Width, Math.Max(2, Height));
            g.ResetClip();
            using (Pen pen = new Pen(Ds.Hairline, 1f)) g.DrawPath(pen, p);
        }
        base.OnPaint(e);
    }
}

// Left rail background: deep indigo gradient with a soft brand glow in the corner.
class RailPanel : Panel
{
    public RailPanel() { SetStyle(ControlStyles.ResizeRedraw, true); }

    // Children that are transparent over the rail call this so they paint their own slice of the gradient.
    public static void PaintFor(Control c, Graphics g)
    {
        Control p = c.Parent;
        while (p != null && !(p is RailPanel)) p = p.Parent;
        if (p == null) { g.Clear(c.Parent == null ? Pal.Bar : c.Parent.BackColor); return; }
        Point o = p.PointToClient(c.PointToScreen(Point.Empty));
        GraphicsState st = g.Save();
        g.TranslateTransform(-o.X, -o.Y);
        ((RailPanel)p).DrawRail(g);
        g.Restore(st);
    }

    protected override void OnPaintBackground(PaintEventArgs pe) { DrawRail(pe.Graphics); }

    void DrawRail(Graphics g)
    {
        Rectangle r = ClientRectangle;
        if (r.Width < 2 || r.Height < 2) return;
        Color top = Pal.Dark ? Color.FromArgb(12, 30, 66) : Color.FromArgb(218, 232, 255);
        Color bot = Pal.Dark ? Color.FromArgb(0, 0, 0) : Color.FromArgb(235, 235, 240);
        using (LinearGradientBrush b = new LinearGradientBrush(r, top, bot, 90f)) g.FillRectangle(b, r);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath gp = new GraphicsPath())
        {
            gp.AddEllipse(-70, -70, 220, 220);
            using (PathGradientBrush pg = new PathGradientBrush(gp))
            {
                pg.CenterColor = Color.FromArgb(Pal.Dark ? 90 : 50, Pal.Brand);
                pg.SurroundColors = new Color[] { Color.FromArgb(0, Pal.Brand) };
                g.FillPath(pg, gp);
            }
        }
        using (Pen line = new Pen(Ds.Hairline, 1f)) g.DrawLine(line, r.Right - 1, 0, r.Right - 1, r.Bottom);
    }
}
class ClearPanel : Panel { protected override void OnPaintBackground(PaintEventArgs e) { RailPanel.PaintFor(this, e.Graphics); } }
class ClearFlow : FlowLayoutPanel { protected override void OnPaintBackground(PaintEventArgs e) { RailPanel.PaintFor(this, e.Graphics); } }
// Themed drop-down: rounded closed box with a chevron, rounded-highlight list items.
class GlassCombo : ComboBox
{
    bool hover;

    public GlassCombo()
    {
        DropDownStyle = ComboBoxStyle.DropDownList; DrawMode = DrawMode.OwnerDrawFixed; ItemHeight = 26; FlatStyle = FlatStyle.Flat;
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool sel = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
        using (Brush bg = new SolidBrush(Pal.Card)) e.Graphics.FillRectangle(bg, e.Bounds);
        if (sel)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = AccountIcon.Rounded(new Rectangle(e.Bounds.X + 3, e.Bounds.Y + 2, e.Bounds.Width - 7, e.Bounds.Height - 5), 6))
            using (Brush b = new SolidBrush(Pal.Brand)) e.Graphics.FillPath(b, p);
        }
        TextRenderer.DrawText(e.Graphics, Convert.ToString(Items[e.Index]), Font, new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
            sel ? Color.White : Pal.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == 0xF) PaintBox();   // WM_PAINT: draw our own box over the system one
    }

    void PaintBox()
    {
        if (Width < 20 || Height < 10) return;
        using (Graphics g = CreateGraphics())
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color around = Parent != null ? Parent.BackColor : Pal.Card;
            g.Clear(around);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = AccountIcon.Rounded(r, Ds.RControl))
            {
                using (Brush b = new SolidBrush(Pal.Blend(Pal.Card, Pal.Dark ? Color.White : Color.Black, hover || DroppedDown ? 0.14 : 0.07))) g.FillPath(b, p);
                using (Pen pen = new Pen(hover || DroppedDown ? Pal.BrandInk : Ds.Hairline, 1f)) g.DrawPath(pen, p);
            }
            string t = SelectedIndex >= 0 ? Convert.ToString(Items[SelectedIndex]) : "";
            TextRenderer.DrawText(g, t, Font, new Rectangle(10, 0, Width - 34, Height), Pal.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            int cx = Width - 16, cy = Height / 2;
            using (Pen ch = new Pen(Pal.Sub, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            { g.DrawLine(ch, cx - 4, cy - 2, cx, cy + 2); g.DrawLine(ch, cx, cy + 2, cx + 4, cy - 2); }
        }
    }
}
// Dark/light context menus and drop-downs that follow the theme.
class ThemeTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground { get { return Pal.Card; } }
    public override Color ImageMarginGradientBegin { get { return Pal.Card; } }
    public override Color ImageMarginGradientMiddle { get { return Pal.Card; } }
    public override Color ImageMarginGradientEnd { get { return Pal.Card; } }
    public override Color MenuBorder { get { return Pal.Border; } }
    public override Color MenuItemBorder { get { return Pal.Brand; } }
    public override Color MenuItemSelected { get { return Pal.Blend(Pal.Card, Pal.Brand, 0.55); } }
    public override Color MenuItemSelectedGradientBegin { get { return Pal.Blend(Pal.Card, Pal.Brand, 0.55); } }
    public override Color MenuItemSelectedGradientEnd { get { return Pal.Blend(Pal.Card, Pal.Brand, 0.55); } }
    public override Color SeparatorDark { get { return Pal.Border; } }
    public override Color CheckBackground { get { return Pal.Brand; } }
}

class ThemeRenderer : ToolStripProfessionalRenderer
{
    public ThemeRenderer() : base(new ThemeTable()) { RoundedEdges = false; }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { e.TextColor = Pal.Text; base.OnRenderItemText(e); }
}
// Text box without the system border; the rounded frame (brand colored while focused) is painted by the parent behind it.
class GlassEdit : TextBox
{
    public GlassEdit() { BorderStyle = BorderStyle.None; }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (Parent != null) { Parent.Paint -= Frame; Parent.Paint += Frame; }
    }

    void Redraw() { if (Parent != null) Parent.Invalidate(new Rectangle(Left - 10, Top - 8, Width + 20, Height + 16)); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Redraw(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Redraw(); }
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); Redraw(); }

    void Frame(object s, PaintEventArgs e)
    {
        if (!Visible) return;
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath p = AccountIcon.Rounded(new Rectangle(Left - 8, Top - 6, Width + 15, Height + 11), Ds.RInput))
        {
            using (Brush b = new SolidBrush(BackColor)) g.FillPath(b, p);
            using (Pen pen = new Pen(Focused ? Pal.BrandInk : Ds.Hairline, Focused ? 1.6f : 1f)) g.DrawPath(pen, p);
        }
    }
}
class PaletteItem
{
    public string Text, Hint = "";
    public Color Dot = Color.Empty;
    public override string ToString() { return Text; }
}

// Quick switcher shown inside the window (type to filter, Up/Down, Enter; Esc or clicking elsewhere closes it).
class PalettePanel : GlassPanel
{
    readonly System.Collections.Generic.List<PaletteItem> items;
    readonly Action<int> pick;
    readonly TextBox t = new TextBox();
    readonly ListBox l = new ListBox();
    bool armed;

    public PalettePanel(System.Collections.Generic.List<PaletteItem> items, Action<int> pick, string prompt = "Search accounts and actions")
    {
        this.items = items; this.pick = pick;
        Radius = Ds.ROverlay; BackColor = Pal.Card; Size = new Size(520, Math.Max(200, Math.Min(392, 102 + items.Count * 40)));   // short lists get a short pop-up
        t.BorderStyle = BorderStyle.None; t.Font = new Font("Segoe UI", 13f); t.BackColor = Pal.Card; t.ForeColor = Pal.Text;
        t.SetBounds(50, 20, Width - 76, 28); Ui.Cue(t, prompt);
        l.BorderStyle = BorderStyle.None; l.BackColor = Pal.Card; l.ForeColor = Pal.Text; l.IntegralHeight = false;
        l.DrawMode = DrawMode.OwnerDrawFixed; l.ItemHeight = 40; l.SetBounds(10, 64, Width - 20, Height - 64 - 38);
        Controls.Add(l); Controls.Add(t);
        Ui.DarkScroll(l);
        l.DrawItem += DrawRow;
        t.TextChanged += delegate { Fill(); };
        t.KeyDown += Keys_; l.KeyDown += Keys_;
        l.MouseClick += delegate(object s, MouseEventArgs e) { int i = l.IndexFromPoint(e.Location); if (i >= 0) { l.SelectedIndex = i; Accept(); } };
        t.LostFocus += Blur; l.LostFocus += Blur;
        using (GraphicsPath rp = AccountIcon.Rounded(new Rectangle(0, 0, Width, Height), Radius)) Region = new Region(rp);   // no square corners over the web page
        Fill();
    }

    void Blur(object s, EventArgs e)
    {
        if (!armed || IsDisposed) return;
        BeginInvoke(new Action(delegate { if (!IsDisposed && !t.Focused && !l.Focused) Close(); }));
    }

    void Keys_(object s, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Close(); e.Handled = e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Enter) { Accept(); e.Handled = e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Down && s == t && l.SelectedIndex < l.Items.Count - 1) { l.SelectedIndex++; e.Handled = true; }
        else if (e.KeyCode == Keys.Up && s == t && l.SelectedIndex > 0) { l.SelectedIndex--; e.Handled = true; }
    }

    void Fill()
    {
        string q = t.Text.Trim().ToLower();
        l.BeginUpdate(); l.Items.Clear();
        foreach (PaletteItem it in items) if (it.Text.ToLower().IndexOf(q) >= 0) l.Items.Add(it);
        l.EndUpdate();
        if (l.Items.Count > 0) l.SelectedIndex = 0;
    }

    void Accept()
    {
        PaletteItem it = l.SelectedItem as PaletteItem;
        if (it == null) return;
        int idx = items.IndexOf(it);
        Close(); pick(idx);
    }

    public void ShowIn(Control host)
    {
        Left = Math.Max(0, (host.Width - Width) / 2); Top = 40;
        host.Controls.Add(this); BringToFront();
        t.Focus(); armed = true;
    }

    public void Close() { if (Parent != null) Parent.Controls.Remove(this); Dispose(); }

    void DrawRow(object s, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        PaletteItem it = (PaletteItem)l.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;
        Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using (Brush bg = new SolidBrush(Pal.Card)) g.FillRectangle(bg, e.Bounds);
        Rectangle r = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 2, e.Bounds.Width - 5, e.Bounds.Height - 4);
        if (sel)
            using (GraphicsPath p = AccountIcon.Rounded(r, 9))
            using (LinearGradientBrush gb = new LinearGradientBrush(r, Pal.Brand, Pal.Brand2, 0f)) g.FillPath(gb, p);
        int x = r.X + 14;
        if (it.Dot != Color.Empty) { using (Brush db = new SolidBrush(it.Dot)) g.FillEllipse(db, x, r.Y + (r.Height - 12) / 2, 12, 12); x += 24; }
        Color fore = sel ? Color.White : Pal.Text, sub = sel ? Color.FromArgb(225, 255, 255, 255) : Pal.Sub;
        int hw = it.Hint.Length > 0 ? TextRenderer.MeasureText(it.Hint, Font).Width + 8 : 0;
        TextRenderer.DrawText(g, it.Text, Font, new Rectangle(x, r.Y, r.Right - x - hw - 8, r.Height), fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (hw > 0) TextRenderer.DrawText(g, it.Hint, Font, new Rectangle(r.Right - hw - 8, r.Y, hw, r.Height), sub, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPadding);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using (GraphicsPath p = AccountIcon.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
        using (Pen pen = new Pen(Color.FromArgb(170, Pal.Brand), 1.6f)) g.DrawPath(pen, p);
        using (Font f = new Font("Segoe MDL2 Assets", 12f)) TextRenderer.DrawText(g, "", f, new Point(18, 22), Pal.Sub);
        using (Pen sep = new Pen(Ds.Hairline, 1f)) g.DrawLine(sep, 14, 58, Width - 14, 58);
        using (Font f = new Font("Segoe UI", 8f))
            TextRenderer.DrawText(g, Loc.T("Enter = open     Esc = close"), f, new Rectangle(0, Height - 30, Width, 24), Pal.Sub, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
// Context menu that also closes on a click inside a web page (WebView2 does not send its clicks to our window).
class AutoMenu : ContextMenuStrip
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    readonly Timer t = new Timer();
    bool wasDown;

    public AutoMenu() { t.Interval = 40; t.Tick += Check; }

    static bool Down() { return (GetAsyncKeyState(0x01) & 0x8000) != 0 || (GetAsyncKeyState(0x02) & 0x8000) != 0; }

    protected override void OnOpened(EventArgs e) { base.OnOpened(e); wasDown = Down(); t.Start(); }
    protected override void OnClosed(ToolStripDropDownClosedEventArgs e) { t.Stop(); base.OnClosed(e); }
    protected override void Dispose(bool disposing) { if (disposing) t.Dispose(); base.Dispose(disposing); }

    bool Inside(Point p)
    {
        if (Bounds.Contains(p)) return true;
        foreach (ToolStripItem i in Items)
        {
            ToolStripMenuItem m = i as ToolStripMenuItem;
            if (m != null && m.DropDown.Visible && m.DropDown.Bounds.Contains(p)) return true;
        }
        return false;
    }

    void Check(object s, EventArgs e)
    {
        bool down = Down();
        if (down && !wasDown && !Inside(Cursor.Position)) Close(ToolStripDropDownCloseReason.AppClicked);
        wasDown = down;
    }
}
// Viewport for the account list: a taller list inside scrolls with the mouse wheel (no scroll bar).
class IconHost : Panel
{
    public Control Inner;

    protected override void OnPaintBackground(PaintEventArgs e) { RailPanel.PaintFor(this, e.Graphics); }

    public void ScrollBy(int d)
    {
        if (Inner == null) return;
        int min = Math.Min(0, Height - Inner.Height - 8);
        Inner.Top = Math.Max(min, Math.Min(0, Inner.Top + d));
    }

    public void EnsureVisible(Control c)
    {
        if (Inner == null || c.Parent != Inner) return;
        int y = Inner.Top + c.Top;
        if (y < 4) ScrollBy(4 - y);
        else if (y + c.Height > Height - 4) ScrollBy(Height - 4 - (y + c.Height));
    }

    protected override void OnMouseWheel(MouseEventArgs e) { ScrollBy(e.Delta / 2); base.OnMouseWheel(e); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); ScrollBy(0); }
}