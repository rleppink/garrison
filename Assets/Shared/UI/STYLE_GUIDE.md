# Garrison — UI Style Guide

The visual language for every Unity (UI Toolkit) element in the game.

## 1. Concept

**A classified WW2 dossier.** The UI reads like field documents on a darkened
desk: aged paper, dark sepia ink, typed labels, the occasional rubber stamp.
It is period-flavored but **high-contrast and modern-clean underneath** — we
borrow Apple/Tailwind discipline (spacing rhythm, restraint, soft corners) and
dress it in 1940s costume.

**Principles**
- Dark ink on light paper. Always. High contrast, readable first.
- Period flavor comes from *color, type, and a few marks* — not from clutter or
  skeuomorphic noise.
- One primary action per screen. Color earns attention; most of the UI is calm.
- Nothing should look like default Unity UI.

## 2. Files

| File | Role |
| --- | --- |
| `Theme/GarrisonTokens.uss` | All design tokens (`:root` custom properties). The single source of truth. |
| `Theme/GarrisonTheme.uss` | Base styling for Unity controls + reusable `.g-*` component/utility classes. |
| *screen*`.uss` | Per-screen **layout only**, consuming tokens + `.g-*` classes. |

A screen's USS pulls the theme in at the top:

```css
@import url("../UI/Theme/GarrisonTokens.uss");
@import url("../UI/Theme/GarrisonTheme.uss");
```

**Rule:** never hard-code a color, radius, spacing, or font size in a screen.
If you need a value that isn't a token, add the token to `GarrisonTokens.uss`.

## 3. Color

Warm paper + sepia. Defined as tokens; never use raw hex in components.

| Token | Value | Use |
| --- | --- | --- |
| `--backdrop` | `rgba(26,22,16,.72)` | Dimmed scene behind floating documents |
| `--surface-canvas` | `#DED2B8` | Base desk / manila |
| `--surface` | `#F4EEDF` | A document |
| `--surface-raised` | `#FBF8EF` | Card / button resting on a document |
| `--surface-sunken` | `#E8DEC6` | Inset fields, unchecked toggles |
| `--surface-header` | `#DED2B8` | Dossier header band |
| `--ink` | `#2B2620` | Primary text |
| `--ink-soft` | `#5A5040` | Secondary text, labels |
| `--ink-faint` | `#8A7E69` | Hints, metadata, placeholders |
| `--line` / `--line-strong` | `#C9BC9E` / `#A89A78` | Hairline / structural borders |
| `--accent` *(ink-blue)* | `#2F4A63` | **Primary** action, links, focus |
| `--danger` *(stamp-red)* | `#8A2B22` | **Destructive / alert only** |
| `--success` / `--warning` | `#4A5D2A` / `#9A6B1E` | Status |

**Accent discipline:** ink-blue is the everyday accent (primary buttons, focus,
selection). Stamp-red is *reserved* — destructive actions, hard alerts, and
stamp marks. Don't use red for decoration or it stops meaning "danger."

## 4. Typography

Sans body with **typewriter accents** for the dossier voice.

| Role | Token font | Used for |
| --- | --- | --- |
| Body / UI | `--font-body` → **Inter** (OFL) | Paragraphs, buttons, values |
| Typewriter | `--font-mono` → **Special Elite** (Apache 2.0) | Labels, headers, metadata, stamps |

Type scale: `--text-xs 11` · `--text-sm 13` · `--text-base 14` · `--text-md 16`
· `--text-lg 20` · `--text-xl 26` · `--text-2xl 34`.

Uppercase section/field labels carry `--tracking-label` (2px) so they read as
*typed* even before the typewriter font is installed.

> UI Toolkit has no `text-transform`; write labels uppercase in the UXML/string.

### Installing the fonts

Fonts ship under OFL/Apache and are free to bundle. They are **not committed
yet** — the theme falls back to the Unity default until you add them:

1. Download **Inter** (https://rsms.me/inter/ or Google Fonts) and **Special
   Elite** (Google Fonts). Both license texts go in `Assets/Shared/UI/Fonts/`.
2. Drop the `.ttf` files into `Assets/Shared/UI/Fonts/`.
3. For each, right-click → **Create → Text → Font Asset** (TextMeshType).
   Name them `Inter.asset` and `SpecialElite.asset`.
4. Uncomment the `--font-body` / `--font-mono` lines at the bottom of
   `GarrisonTokens.uss` and fix the paths/GUIDs.
5. In `GarrisonTheme.uss`, add to the type classes:
   - `.g-body` → `-unity-font-definition: var(--font-body);`
   - `.g-label`, `.g-meta`, `.g-title`, `.g-stamp` → `-unity-font-definition: var(--font-mono);`

(If Special Elite is too rough at 11px, **Cutive Mono** (OFL) is the cleaner
typewriter fallback.)

## 5. Shape & spacing

- **Radius is mixed by element:** `--radius-sm 3px` (buttons, inputs, toggles),
  `--radius-md 6px` (cards, panels, dossiers), `--radius-lg 10px` (large
  containers / modals). Chrome and rules stay crisp.
- **Spacing** is a 4px scale: `--space-1 4` … `--space-6 32`. Pad and gap with
  tokens only — it keeps the vertical rhythm consistent across screens.

## 6. Components (`.g-*`)

| Class | What it is |
| --- | --- |
| `.g-panel` / `.g-card` | Plain paper surface / raised card |
| `.g-dossier` + `__header` / `__body` | A classified document: manila header band + body |
| `.g-stamp` | Rotated rubber stamp (`TOP SECRET`, `CONFIDENTIAL`). Use sparingly |
| `.g-button` (+ `--primary` / `--danger` / `--ghost`) | Buttons; bare `Button` is styled too |
| `.g-title` / `.g-heading` / `.g-label` / `.g-meta` / `.g-body` | Type roles |
| `.g-divider` | Hairline rule |

`TextField` and `Toggle` are themed globally — no class needed; add only sizing.

**Naming:** `.g-block`, `.g-block__element`, `.g-block--modifier` (BEM-ish).
New shared component → add it to `GarrisonTheme.uss`. One-screen layout → keep
it in that screen's USS.

## 7. Reference implementation

`Assets/Shared/Lobby/` is the worked example: two `.g-dossier` documents on a
dimmed desk, typed group headers on hairline rules, an ink-blue primary
**Start** button, themed fields and toggles. Copy its structure for new screens.

## 8. Resolution & crispness

Every screen renders through **one shared PanelSettings asset**
(`Assets/Shared/Lobby/LobbyPanelSettings.asset`): *Scale With Screen Size*,
reference **1920×1080**. At 1080p that's a 1:1 mapping — no bilinear upscale, so
thin text and hairlines stay crisp — and it still scales cleanly to other 16:9
resolutions.

**For any new panel:** point its `UIDocument` at that same PanelSettings asset.
The reference resolution lives on the asset, not the panel, so every screen
inherits it automatically — there is nothing per-panel to set or remember.
(The name says "Lobby" for historical reasons; treat it as the game-wide
default.)

Because 1px in USS ≈ 1 real px at 1080p:

- **Size in tokens.** The spacing / radius / type tokens are tuned for this
  reference. Don't hard-code structural sizes (widths, heights, paddings) in a
  screen — if you must, remember they're real pixels now, not 0.66× of one.
- **Avoid fractional-px borders/radii.** `1.5px` reintroduces the soft edges this
  setup exists to remove. Keep borders and hairlines at whole-pixel values.
- Hairlines stay `1px` on purpose — crisp is the point. They are *not* scaled
  with the size tokens.
