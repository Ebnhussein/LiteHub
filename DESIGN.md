# LiteHub design system

Based on Apple's iOS system design (Liquid Glass era): system colors with accessible tints, capsule controls, flat grouped cards with
rounded corners, glass reserved for floating chrome, green switches. Tokens live in `Ds.cs`; light/dark surfaces are in `Pal.Set` (`Core.cs`).
`docs/design-system.html` is generated from them by `tools/make-design-doc.ps1`; `bin\LiteHub.exe --selftest` fails if text or accent
contrast drops below WCAG limits in either theme. Segoe UI stands in for SF Pro on Windows.

Rules: one primary button per view; Danger only for destructive actions; Brand 2 only as gradient end; screens start at
`Ds.PageX`/`Ds.PageTop` with one `Ds.Title`; use `Ui.Row`, `Ui.Btn`, `Ds.Hairline`, never raw colors or radii.
