using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// LiteHub's own desktop notification card (instead of the plain Windows toast): account icon, title, text, slides in at the
// bottom-right, up to four stacked, pauses while the mouse is over it, click opens the account, x closes it.
class Toast : Form
{
    static readonly List<Toast> open = new List<Toast>();
    const int W = 372, H = 92, Gap = 10, Margin = 16;
    readonly AppDef app;
    readonly string title, body;
    readonly Action click;
    readonly Timer life = new Timer(), anim = new Timer();
    int slide = 40, wait;
    bool hover, closing;

    public static void Show(AppDef a, string title, string body, Action click)
    {
        Toast t = new Toast(a, title ?? "", body ?? "", click);
        while (open.Count >= 4) open[0].Dismiss(true);
        open.Add(t);
        Restack();
        t.Opacity = 0; t.Left = t.Target() + t.slide;
        ((Form)t).Show();
        t.anim.Start(); t.life.Start();
    }

    Toast(AppDef a, string title, string body, Action click)
    {
        app = a; this.title = title; this.body = body; this.click = click;
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
        Size = new Size(W, H); DoubleBuffered = true; Cursor = Cursors.Hand;
        using (GraphicsPath p = AccountIcon.Rounded(new Rectangle(0, 0, W, H), Ds.RCard)) Region = new Region(p);
        life.Interval = 250; life.Tick += delegate { if (!hover && (wait += 250) >= 7000) Dismiss(false); };
        anim.Interval = 15; anim.Tick += Animate;
        MouseEnter += delegate { hover = true; Invalidate(); };
        MouseLeave += delegate { hover = false; wait = 0; Invalidate(); };
    }

    // never steal the focus from what the user is typing
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams
    {
        get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x00000080; return cp; }   // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
    }

    int Target() { return Screen.PrimaryScreen.WorkingArea.Right - W - Margin; }

    static void Restack()
    {
        Rectangle wa = Screen.PrimaryScreen.WorkingArea;
        for (int i = 0; i < open.Count; i++) open[i].Top = wa.Bottom - Margin - (i + 1) * H - i * Gap;
    }

    void Animate(object s, EventArgs e)
    {
        if (closing) { Opacity -= 0.16; Left += 6; if (Opacity <= 0.02) { anim.Stop(); open.Remove(this); Restack(); Close(); } return; }
        if (slide > 0) { slide = Math.Max(0, slide - 5); Left = Target() + slide; Opacity = Math.Min(1, Opacity + 0.14); }
        else anim.Stop();
    }

    void Dismiss(bool now)
    {
        if (closing) return;
        closing = true; life.Stop();
        if (now) { open.Remove(this); Restack(); Close(); return; }
        anim.Start();
    }

    protected override void Dispose(bool disposing) { if (disposing) { life.Dispose(); anim.Dispose(); } base.Dispose(disposing); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.X > W - 34 && e.Y < 34) { Dismiss(false); return; }
        Dismiss(false);
        if (click != null) click();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using (Brush bg = new SolidBrush(Pal.Card)) g.FillRectangle(bg, 0, 0, W, H);
        using (LinearGradientBrush sh = new LinearGradientBrush(new Rectangle(0, 0, W, H), Ds.Sheen, Color.FromArgb(0, Color.White), 90f)) g.FillRectangle(sh, 0, 0, W, H);
        using (GraphicsPath edge = AccountIcon.Rounded(new Rectangle(0, 0, W - 1, H - 1), Ds.RCard)) using (Pen pen = new Pen(Ds.Hairline, 1.2f)) g.DrawPath(pen, edge);
        Rectangle r = new Rectangle(16, 20, 52, 52);
        using (GraphicsPath tile = AccountIcon.Rounded(r, 13))
        {
            if (app != null && app.Icon != null && app.Icon.Width >= 96) { g.SetClip(tile); g.DrawImage(app.Icon, r); g.ResetClip(); }
            else if (app != null && app.Icon != null)
            {
                using (Brush w = new SolidBrush(Color.White)) g.FillPath(w, tile);
                int sz = Math.Min(app.Icon.Width <= 20 ? app.Icon.Width * 2 : 32, 36);
                g.DrawImage(app.Icon, new Rectangle(r.X + (r.Width - sz) / 2, r.Y + (r.Height - sz) / 2, sz, sz));
            }
            else
            {
                Color c = app != null ? app.AccentColor : Pal.Brand;
                using (Brush b = new SolidBrush(c)) g.FillPath(b, tile);
                using (Font f = new Font("Segoe UI", 18f, FontStyle.Bold)) TextRenderer.DrawText(g, app != null ? app.Name.Substring(0, 1).ToUpper() : "L", f, r, Ui.Light(c) ? Color.Black : Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
        string name = app != null ? app.Name : "LiteHub";
        using (Font f = new Font("Segoe UI", Ds.Caption, FontStyle.Bold))
            TextRenderer.DrawText(g, name, f, new Rectangle(82, 12, W - 82 - 52, 18), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        using (Font f = new Font("Segoe UI", Ds.Caption))
            TextRenderer.DrawText(g, hover ? "✕" : Loc.T("now"), f, new Rectangle(W - 52, 12, 38, 18), hover ? Pal.Text : Pal.Sub, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        int y = 32;
        if (title.Length > 0)
        {
            using (Font f = new Font("Segoe UI", Ds.Body, FontStyle.Bold))
                TextRenderer.DrawText(g, title, f, new Rectangle(82, y, W - 82 - 14, 20), Pal.Text, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            y += 22;
        }
        using (Font f = new Font("Segoe UI", Ds.Caption + 0.5f))
            TextRenderer.DrawText(g, body, f, new Rectangle(82, y, W - 82 - 14, H - y - 8), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
}
