# M2 — Combat core

> The `Combat/` slice.
> **Slice(s):** `Combat/` · **Depends on:** M1 (movement-state hook + camera) ·
> **Proves:** a firefight reads as *tactical, not twitch* — and the Sten TTK @
> 20m bellwether.

Detail for M2 in [`../plan.md`](../plan.md). Combat model in
[`../../design/concept.md`](../../design/concept.md) ("Combat model").

## Why here

With the camera proven (M1), combat is the next feel-bet and the thing every
later defense/objective interaction resolves against. It's still playable with 2
people on a greybox, so we tune the bellwether before fog, NPCs, or objectives
muddy the read.

## Systems this milestone builds

Five systems live in `Combat/`. Each is described by what it's responsible for,
the state it owns, the config it reads, and the seam it exposes or consumes —
*not* by its classes or scene wiring (that's `architecture.md`'s call at build
time).

### 1. Life-state
- **Responsibility:** the single source of truth for whether a character is up,
  down, or out.
- **Owns:** `hearts` (current / max), `armorPerHeart` (defender only — see §4),
  and a lifestate machine:

  ```
  Healthy ──hearts hit 0──► Downed ──revive completes──► Healthy (at 1 heart)
                              │
                              └──bleed-out timer expires / finished off──► Dead
  ```

  - **Downed:** crawl-only movement, can't fire, a bleed-out timer runs (length
    = revive window X).
  - **Dead:** permadeath for the round. The character goes inert and leaves play;
    *where its dropped gear goes* is M6's concern, *what the dead player sees* is
    M9's (shoulder-spectator). M2 only owns the transition.
- **Config read:** `maxHearts`, `reviveWindowSec`.
- **Seam exposed:** emits lifestate-change events (became-downed / revived /
  died). M6 (loot drop), M9 (spectator) subscribe later. Life-state never reaches
  into those slices — it announces, they listen.

### 2. Hit resolution (server-authoritative)
- **Responsibility:** the one server-side path that turns "someone fired" into
  "hearts/armor removed." Clients never self-report hits.
- **Flow (the seam):** client sends a *fire input* → server decides whether it
  connects, using shooter position + aim + the accuracy deviation from §3 (and,
  once M3 lands, LOS) → on a connect, it applies damage to the target's §1
  life-state.
- **Damage rule:** gunshot = 1 heart. Application order is **armor first (if any,
  and unbroken), then hearts.**
- **Open decision to nail here:** hitscan-with-deviation vs projectile.
  *Recommendation: hitscan-with-deviation* — most readable in a top-down view and
  makes TTK a clean dial.

### 3. Accuracy
- **Responsibility:** turn movement into a fire-time penalty — the single biggest
  anti-rush-B lever.
- **Owns:** current spread, derived each frame from the **M1 movement-state
  hook** (idle = min spread, moving → sprint = max).
- **Config read:** the `speed → spread` curve (idle / moving / sprint spread) and
  the weapon's base spread.
- **Seam consumed:** read by §2 at fire time. Adds no new networking — it's an
  input to the server's hit check.

### 4. Defender armor
- **Responsibility:** make "focused fire breaks the defender" a legible rule
  while keeping both sides on the same 3-heart numerics.
- **Owns:** per-heart "armor intact" flags, plus a short **focus-fire tracker**
  (recent hits tagged by distinct attacker).
- **Rule:** a hit that would cost a heart is *absorbed* by that heart's armor —
  **unless ≥ `focusFireThreshold` distinct attackers have landed hits inside
  `focusFireWindowSec`,** in which case armor breaks and the heart is lost
  instead.
- **Config read:** `focusFireWindowSec`, `focusFireThreshold` (= 2).
- **Open decision to nail here:** does absorbed armor regenerate within a round?
  *Recommendation: no regen for MVP* — armor is a one-shot buffer per heart;
  revisit only if playtest demands it.

### 5. Test weapon
- **Responsibility:** give §2/§3 something to fire and the bellwether something to
  tune.
- **Owns:** one Sten-like weapon as a **data profile** (fire rate, damage = 1
  heart, base spread, range/falloff), so the **Sten TTK @ 20m** is a number we
  turn, not code we edit.

## Config surface introduced

| Key | Shape | Starting value |
|---|---|---|
| `maxHearts` | int | 3 |
| `reviveWindowSec` | float | TBD (tunes rescue drama) |
| `weapon.*` | profile: fireRate, damageHearts, baseSpread, rangeFalloff | tuned to Sten TTK @ 20m |
| `accuracy.spreadCurve` | idleSpread / movingSpread / sprintSpread | TBD |
| `focusFireWindowSec` | float | TBD |
| `focusFireThreshold` | int | 2 |

(All into the lobby-config system from M0.)

## Audio

Each cue is triggered off a §1/§2 event and played positionally through the M0
bus:
- **Gunfire** (directional, distance-attenuated) — the loudest read in the raid.
- **Hit / down / revive** cues, off the life-state transitions.

## Open decisions to resolve in M2
- Hitscan-with-deviation vs projectile (recommend hitscan).
- Armor regen within a round (recommend no).
- Downed crawl speed, and whether a downed player can be "finished off" early vs
  only bleeding out.
- Syrette healing — stub or in? *Recommend stub here*; the shared-gear-pool draft
  that makes syrettes scarce is M4.

## Done when
- A small firefight (e.g. 2v1) on the greybox reads tactical, not twitch.
- The movement penalty visibly punishes run-and-gun.
- Defender armor tanks a lone shooter but breaks under coordinated 2+ fire inside
  the focus-fire window.
- The Sten TTK @ 20m feels right — and is reachable by turning the dial.

## Explicitly not in M2
- S-mine damage (lands with mines in M5), fog/LOS (M3 — hit resolution gets its
  LOS check then), NPCs (M3 stub), shoulder-spectator and gear-drop-on-death (M9
  / M6).
