# UI Upscaling Change — 1920×1080 reference

## What we want
Switch `LobbyPanelSettings` (and any future UI panels) from a **1280×720**
reference resolution to **1920×1080**, keeping `Scale With Screen Size`.

## Why
On a 1080p display the current 1280×720 reference makes UI Toolkit upscale
everything ~1.5×. That bilinear upscale is what softens thin text. A 1920×1080
reference renders 1:1 on 1080p — no upscale, crisp text — and still scales
cleanly to other resolutions because the aspect ratio is unchanged.

## The catch
At 1:1, every px in USS is a real screen px, so the UI renders ~1.5× **smaller**
than it does now. To keep the current visual size we'd bump the type scale and
spacing tokens in `GarrisonTokens.uss` by roughly that factor (e.g. `--text-base`
14 → ~21, `--space-4` 16 → ~24, etc.). Token-only change; layout stays the same.

## Status
**Done (2026-06-04).** `LobbyPanelSettings` reference is now 1920×1080; the
spacing / radius / type / tracking tokens in `GarrisonTokens.uss` were scaled
×1.5 (round half up). The hard-coded structural sizes in `LobbyUI.uss` and the
stamp/checkmark in `GarrisonTheme.uss` were scaled too — the "token-only" hope
didn't hold because those bypass the tokens. Borders/hairlines were left at their
whole-pixel widths so they render crisp at 1:1.

The convention for future panels now lives in `STYLE_GUIDE.md` §8. Final visual
sign-off still wants a look in the Unity editor at 1080p.
