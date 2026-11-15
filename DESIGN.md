# LiteHub design system

Tokens live in `Ds.cs` (brand, status, radii, spacing, type) and `Pal.Set` in `Core.cs` (light/dark surfaces and text).
`docs/design-system.html` is generated from them by `tools/make-design-doc.ps1` (palette, contrast table, components, rules).
`bin\LiteHub.exe --selftest` fails if text/brand contrast drops below WCAG limits in either theme.

Rules: one primary (gradient) button per view; Danger only for destructive actions; Brand 2 only as gradient end;
inputs are recessed (Page color inside a Card); screens start at `Ds.PageX`/`Ds.PageTop` with one `Ds.Title`;
use `Ui.Row`, `Ui.Btn`, `Ds.Hairline`, `Ds.Sheen`, never raw colors or radii.
