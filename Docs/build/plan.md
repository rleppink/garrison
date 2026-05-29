# GARRISON — Build Plan ("The Grand Main Plan")

*The high-level order in which we build the MVP. Companion to `../design/mvp-scope.md`
(what the first playable contains) and `architecture.md` (how the code is organized).
This doc is about **sequence** — what we build first, why, and what each step
has to prove before we move on.*

---

## The two ordering principles

Everything below follows from two commitments already made elsewhere:

1. **Front-load the scary feel-bets** (per `../design/mvp-scope.md`). The riskiest, most
   un-spreadsheet-able mechanics get built and felt *first*, while they're
   cheap to throw away. The camera + combat + movement triad is the biggest
   "elegant or fiddly?" gamble and gates everything else, so it leads — and it's
   testable with 2 people before a single objective or NPC exists.

2. **Build in vertical slices** (per `architecture.md`). Each milestone grows
   one or two feature folders end-to-end (behaviour + data + UI + net glue),
   not a horizontal layer across the whole game. A milestone is "done" when you
   can *play* the thing it added, not when a system compiles.

Two threads run **cross-cutting, alongside every milestone**, never as a
final-step bolt-on:

- **Config, not constants** — every "tune in playtest" value lands as
  host-settable lobby config the moment its system is built (the config table
  in `../design/mvp-scope.md` is the running checklist).
- **Audio as a primary sense** — the concept doc flags it can't be retrofitted. The
  audio bus goes in at M0; each system adds its cues *as it lands*; M9 is a
  consolidation/mix pass, not the first time sound exists.

And one rule under all of it: **server-authoritative first** (fog, combat, loot
state), prediction layered on later only where feel demands it.

---

## Milestones

*Each milestone below is broken out into a detailed sub-plan in
[`milestones/`](milestones/) — what sub-features it grows, the config dials and
audio cues it lands, and what it has to prove. The summaries here own the
**sequence**; the sub-plans own the **contents**.*

### M0 — Walking skeleton & seams
Stand up the spine everything hangs off.
- Unity 6 project, pinned editor version; PurrNet integrated.
- The `Assets/` feature-folder layout from `architecture.md`.
- Scene & lifetime spine (`architecture.md`): a persistent `Bootstrap` scene
  hosts the long-lived services; the greybox loads as the one swappable `Map`
  scene. No `DontDestroyOnLoad`, no per-phase scenes.
- `Shared/`: lobby/config system (host-settable), networking conventions, audio
  bus stub.
- LAN connect: host + clients join a lobby and spawn server-authoritative
  capsules on a flat greybox plane; networked movement (no prediction yet).

**Proves:** 2–6 people connect on a LAN and move around together. Net spine and
config plumbing are real.

### M1 — The camera & movement feel-bet *(the riskiest thing, so it's first)*
Build the `Vision/` slice's camera before anything shoots.
- Top-down/iso camera; aim-extends-camera (Hotline-Miami); hard rule "character
  never leaves the screen"; camera-push UX dials (snap vs lazy-follow, push
  radius/shape) all as config.
- Movement tuned toward "tactical, not rush-B," with the hooks for
  movement-reduces-accuracy in place.

**Proves (go/no-go):** does aim = accuracy = camera-push with the blind-behind
tradeoff feel elegant or like fighting the controls? If this is bad, we learn it
now — before we've built a game on top of it.

### M2 — Combat core
The `Combat/` slice.
- 3 hearts; gunshot = 1 heart; movement-reduces-accuracy live; downed at 0 +
  teammate revive window; attacker permadeath.
- Defender armor (absorbs first hit per heart unless focus-fired by 2+).
- One test weapon tuned toward the **Sten TTK @ 20m bellwether**.

**Proves:** a firefight reads as tactical, not twitch. The bellwether number.

### M3 — LOS fog of war + sight cones
Extend `Vision/`.
- Server-truth LOS fog; terrain blocks sight.
- Visible sight cones on a basic NPC (first stub of a body); player facing read
  from posture, no cone.

**Proves:** peeking/flanking/"is that nest crewed?" are real questions; fog is
server-authoritative, not client-trusted.

### M4 — Planning phase & Supply
The `Planning/` slice + the placement half of `Defenses/`.
- Supply budget (config-scaled `25 + 5N`); the 7-item toolkit placed in the
  world; bodies assigned to patrol / sentry / gunner with role locked at
  planning end.
- Valuable placement (concentrate vs spread); per-player edge spawn selection
  (changeable, visible to teammates); defender's attacker-POV preview.
- Planning runs as a high-overview camera/UI **mode over the live `Map` scene**,
  not a separate scene — placements *are* the real world objects and previews
  use the real geometry (see `architecture.md`).

**Proves:** a defender can build a position and a crew can pick approaches — the
round now has a setup.

### M5 — Defenses live (execution side)
The behavioural half of `Defenses/`.
- Mines, wire, trip flares, MG nest (manned by a body / crewable by the
  defender), mobile searchlight; NPC patrol/sentry/gunner AI wired to the M3
  cones; map fixtures (locked doors, watchtowers, alarm bells).

**Proves:** the **combined-arms thesis** is testable — does a crew that ignores
the defender still crack a pure turtle, and does a stripped defender lose 1vN?

### M6 — Objectives & getaway
`Loot/` + `Tracking/`.
- All three objectives with their carry profiles (documents=tracked/fast/one-hand,
  gold=untracked/fast/two-hands, heavy gear=untracked/slow/hands-free); assigned
  mission per round; universal relay; loot-as-football.
- DF beacon on documents; hold-to-consult HUD; audible ping per fix.

**Proves:** a full round runs end-to-end — grab and run for an edge. The **DF
hold-to-consult chase** feel-bet gets its first read.

### M7 — Reinforcement ramp & respawn
The `Reinforcements/` slice.
- Dual clock: reinforcements en route from t=0, trickle ~5:00, ramp toward the
  hard time-floor; reinforcements arrive from a road/edge (no teleport).
- Defender outpost respawn + drive-in delay.

**Proves:** the **ramp** as an abort-or-commit gradient (and the
automated-vs-present needle — does killing the defender matter?). Defender win
conditions become reachable.

### M8 — The greybox map
- Three-lane greybox per `../design/mvp-scope.md`: open road across farmland / forest track /
  town pockets, with the keep above and multiple edges for spawn + extraction.
- **Night-only** lighting.

**Proves:** each lane's character exercises its matching objective profile; the
night readability bar (cones, tells, audio) holds with no daylight crutch.

### M9 — Audio mix, spectator, round resolution → first full playtest
- Consolidate networked positional audio (engines, footsteps, gunfire, DF ping,
  reinforcement cues, sector-coarse alarm).
- Shoulder-spectator for dead attackers (locked to one living teammate's exact
  view, switchable).
- Win-condition resolution, round reset, and `../playtest/log.md` hooks.

**Proves:** ready for the first real 5–6 person LAN session.

---

## Dependency shape (not strictly linear)

```
M0 ─► M1 ─► M2 ─┬─► M3 ─► M5 ┐
                │            ├─► M6 ─► M7 ─► M9
                └─► M4 ──────┘
                         M8 (map) can develop in parallel from M3 on
```

M1→M2 is the critical early path (the feel gate). M4 (planning) can begin once
M3's NPC/cone stub exists. M8 (the greybox) is the one piece that can be built
in parallel by anyone not on the core loop, since later milestones only need
*a* space to run in.

---

## Out of scope (so it can't creep into the plan)

Straight from `../design/mvp-scope.md`'s OUT list — none of these get a milestone: defender
spike verbs, paradrop, two-person heavy-gear carry, matchmaking, day/night
cycle, multiple maps, destructible DF towers, See-investment tracking scaling.

---

## The one number that judges the whole plan

Per `../design/mvp-scope.md`: across a session, are attacker and defender wins roughly balanced?
Every milestone above exists to make that question *askable* with real players —
and we answer it by playing and turning config dials, not by spreadsheet.
