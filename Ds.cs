using System;
using System.Drawing;

// LiteHub design system: the single place for brand and status colors, shape, spacing and type.
// Surfaces and text colors (light and dark) live in Pal; the rules and the reasoning are in DESIGN.md.
static class Ds
{
    // ---- brand ----
    public static readonly Color Brand = Color.FromArgb(91, 84, 232);     // indigo: primary actions, selection
    public static readonly Color Brand2 = Color.FromArgb(168, 85, 247);   // violet: gradient end only (never for text)

    // ---- status ----
    public static readonly Color Success = Color.FromArgb(34, 197, 94);   // running, done
    public static readonly Color Danger = Color.FromArgb(214, 58, 63);    // unread counts, destructive actions
    public static readonly Color Warning = Color.FromArgb(245, 165, 36);  // reserved: cautions

    // ---- shape (px) ----
    public const int RControl = 8;     // buttons, inputs, drop-downs
    public const int RTile = 11;       // account tiles
    public const int RCard = 12;       // cards and rows
    public const int ROverlay = 16;    // floating panels (quick switcher)

    // ---- spacing scale (px) ----
    public const int S1 = 4, S2 = 8, S3 = 12, S4 = 16, S5 = 24, S6 = 32;
    public const int PageX = S6, PageTop = S5;   // every full-page screen starts here

    // ---- type scale (Segoe UI, pt) ----
    public const float Micro = 7f;      // badges
    public const float Caption = 8.5f;  // hints, timestamps
    public const float Body = 10f;      // rows, buttons, lists
    public const float Heading = 11f;   // section headings
    public const float Title = 20f;     // one per screen

    // ---- lines and highlights (follow the theme) ----
    public static Color Hairline { get { return Color.FromArgb(Pal.Dark ? 42 : 72, Pal.Dark ? Color.White : Brand); } }
    public static Color Sheen { get { return Color.FromArgb(Pal.Dark ? 24 : 60, Color.White); } }
}
