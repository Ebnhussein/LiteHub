using System;
using System.Drawing;

// LiteHub design system, based on Apple's iOS system design (Liquid Glass era): system colors with accessible tints,
// capsule controls, large rounded grouped cards, SF-style type scale. Surfaces and text colors (light/dark) are in Pal.Set (Core.cs).
// The rules and the reasoning are in DESIGN.md; docs/design-system.html is generated from these values.
static class Ds
{
    // ---- accent (iOS "systemBlue", tuned so white text on it is 4.5:1) ----
    public static readonly Color Brand = Color.FromArgb(0, 102, 235);     // primary actions, selection
    public static readonly Color Brand2 = Color.FromArgb(88, 86, 214);    // iOS systemIndigo: gradient end only

    // ---- status (iOS system colors, accessible variants) ----
    public static readonly Color Success = Color.FromArgb(52, 199, 89);   // systemGreen: switches on, running
    public static readonly Color Danger = Color.FromArgb(215, 0, 21);     // systemRed (accessible): unread counts, destructive
    public static readonly Color Warning = Color.FromArgb(255, 149, 0);   // systemOrange: cautions

    // ---- shape (px) ----
    public const int RControl = 16;    // buttons, drop-downs: capsule (clamped to half the height)
    public const int RInput = 10;      // text fields
    public const int RTile = 9;        // account tiles (iOS app-icon squircle is about 22% of the size)
    public const int RCard = 18;       // grouped cards and rows
    public const int ROverlay = 26;    // floating panels (quick switcher)

    // ---- spacing scale (px) ----
    public const int S1 = 4, S2 = 8, S3 = 12, S4 = 16, S5 = 24, S6 = 32;
    public const int PageX = 40, PageTop = 32;   // every full-page screen starts here
    public const int RowH = 76, RowPad = 20, RowGap = 10;   // cards: 20 px inside, 10 px between

    // ---- type scale (Segoe UI stands in for SF Pro, pt) ----
    public const float Micro = 7f;      // badges
    public const float Caption = 8.5f;  // footnotes, timestamps
    public const float Body = 10f;      // rows, buttons, lists
    public const float Heading = 11f;   // section headings (semibold)
    public const float Title = 22f;     // one per screen

    // ---- lines and highlights (follow the theme) ----
    public static Color Hairline { get { return Pal.Dark ? Color.FromArgb(70, 84, 84, 88) : Color.FromArgb(90, 60, 60, 67); } }   // iOS separator
    public static Color Sheen { get { return Color.FromArgb(Pal.Dark ? 14 : 0, Color.White); } }   // only a whisper: cards are flat, glass is for floating chrome
}
