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
- **Owns:** `hearts` (current / max), with attackers at 3 max hearts and the
  defender at 4 max hearts (see §4), and a lifestate machine:

  ```
  Healthy ──drops to 1 heart──► Downed ──syrette──► Up (mobile, still 1 heart)
   (3 / 2)                         │                       │
                                   ├─bleed-out expires──► Dead
                                   └─any further hit─────► Dead (0 hearts)
  ```

  - **Downed (at 1 heart):** immobile — can't move, can't fire — with a bleed-out
    timer running (length `bleedOutSec`). The *only* exit back into play is a
    **syrette**, applied to self or to a teammate you reach; there is no item-less
    revive. (M2 assumes the syrette is freely available so the loop can be tuned;
    M4 makes it a scarce drafted resource.)
  - **Up (post-syrette, still 1 heart):** mobile and able to fight again — walk,
    run, shoot "through the pain" — but the syrette restored *no* heart, so the
    next hit is fatal.
  - **Dead:** permadeath for the round, reached by **0 hearts however you get
    there** — any hit while at 1 heart (downed or up), or the bleed-out timer
    expiring. The character goes inert and leaves play; *where its dropped gear
    goes* is M6's concern, *what the dead player sees* is M9's (shoulder-
    spectator). M2 only owns the transition.
- **Config read:** `maxHearts`, `defenderMaxHearts`, `bleedOutSec`.
- **Seam exposed:** emits lifestate-change events (became-downed / got-up /
  died). M6 (loot drop), M9 (spectator) subscribe later. Life-state never reaches
  into those slices — it announces, they listen.

### 2. Hit resolution (server-authoritative)
- **Responsibility:** the one server-side path that turns "someone fired" into
  "hearts removed." Clients never self-report hits.
- **Flow (the seam):** client sends a *fire input* → server decides whether it
  connects, using shooter position + aim + the accuracy deviation from §3 (and,
  once M3 lands, LOS) → on a connect, it applies damage to the target's §1
  life-state.
- **Damage rule:** gunshot = 1 heart.
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

### 4. Defender durability
- **Responsibility:** give the solo defender a small durability edge without a
  second armor subsystem.
- **Rule:** the defender has 4 max hearts. Attackers have 3 max hearts. Gunshots
  still remove exactly 1 heart.
- **Config read:** `defenderMaxHearts` (default 4), alongside attacker
  `maxHearts` (default 3).
- **Decision recorded:** the previous focus-fire armor idea was cut during M2
  implementation as needless complexity. Armor is just extra hearts for the MVP.

### 5. Test weapon
- **Responsibility:** give §2/§3 something to fire and the bellwether something to
  tune.
- **Owns:** one Sten-like weapon as a **data profile** (fire rate, damage = 1
  heart, base spread, range/falloff), so the **Sten TTK @ 20m** is a number we
  turn, not code we edit.

## Config surface introduced

| Key                    | Shape                                                     | Starting value           |
|------------------------|-----------------------------------------------------------|--------------------------|
| `maxHearts`            | int                                                       | 3                        |
| `defenderMaxHearts`    | int                                                       | 4                        |
| `bleedOutSec`          | float                                                     | TBD (tunes rescue drama) |
| `weapon.*`             | profile: fireRate, damageHearts, baseSpread, rangeFalloff | tuned to Sten TTK @ 20m  |
| `accuracy.spreadCurve` | idleSpread / movingSpread / sprintSpread                  | TBD                      |

(All into the lobby-config system from M0.)

## Audio

Each cue is triggered off a §1/§2 event and played positionally through the M0
bus:
- **Gunfire** (directional, distance-attenuated) — the loudest read in the raid.
- **Hit / down / got-up (syrette)** cues, off the life-state transitions.

## Open decisions to resolve in M2
- Hitscan-with-deviation vs projectile (recommend hitscan).
- Defender armor model — **resolved:** no focus-fire armor layer; defender has 4
  hearts, attackers have 3.
- Bleed-out timer length (`bleedOutSec`) — the rescue-drama dial.
- Syrette is **in** as the down→up mechanic (and the only revive); M2 assumes it's
  freely available to tune the loop, the shared-gear-pool draft that makes it
  scarce is M4.

## Done when
- A small firefight (e.g. 2v1) on the greybox reads tactical, not twitch.
- The movement penalty visibly punishes run-and-gun.
- Defender durability reads simply: the defender takes one more hit than an
  attacker, with no special armor rules.
- The Sten TTK @ 20m feels right — and is reachable by turning the dial.

## Explicitly not in M2
- S-mine damage (lands with mines in M5), fog/LOS (M3 — hit resolution gets its
  LOS check then), NPCs (M3 stub), shoulder-spectator and gear-drop-on-death (M9
  / M6).
