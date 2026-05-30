# GARRISON — MVP Scope ("First Playable")

*What the first prototype builds, and what it deliberately refuses to build.
Companion to `concept.md` (the ideation braindump). When the two
disagree, this doc wins for MVP purposes — the concept doc explores the whole
game; this describes the first slice we put in front of 5–6 people on a LAN.*

---

## The goal of the first playable

A complete round, start to finish, playable on a LAN with **4–6 people**
(usually one defender + 4–5 attackers). Not pretty, not balanced — *playable
and tunable*. The point is to put the core loop and the three scariest feel-bets
in front of real players and start a playtest log.

**Design stance for MVP:** no hardcoded player caps, no baked-in magic numbers.
Anything we'd want to tweak between rounds is a host-settable lobby config option
(see below). We balance by playing, not by spreadsheet.

---

## IN — the spine that makes a round a round

**Players & scaling**
- Solo defender + N attackers, **N general (target 4–5, must not break at 6).**
- Planning board and spawn UI must stay legible with 5–6 attacker markers — not
  designed for 2 and stretched.

**Phase 1 — Planning**
- Supply budget, scaling `25 + 5N` (values host-configurable — see config).
- The placeable/body toolkit (the 7-item table from the concept doc): barbed wire,
  trip flare, barricade, S-mine, unmanned MG nest, mobile searchlight, NPC body
  (assigned to patrol / tower sentry / MG gunner). Role locked at planning end;
  permadeath; no mid-round re-tasking.
- Map fixtures usable for free (watchtowers, doors, portcullis-as-locked-fixture,
  alarm bells, pre-built positions).
- Valuable placement: defender spreads / concentrates the three objectives.
- Attacker shared finite gear pool, drafted in planning (incl. mine detector,
  syrettes); tool handoff decided here.
- Per-player edge spawn selection — changeable during planning, visible to
  teammates.
- Defender's planning-time preview of the scene from each attacker spawn POV.

**Phase 2 — Execution**
- LOS fog of war (server-truth, not client-trusted — see architecture doc).
- Aim-extends-camera (Hotline-Miami style), character never leaves screen.
- Visible sight cones on **NPCs only**; player facing read from posture.
- 3-heart combat; gunshot = 1 heart; movement reduces accuracy; **1 heart =
  downed & immobile, 0 = dead**; no healing — a syrette (drafted item, = the only
  revive) gets a downed player up and mobile but still at 1 heart, so the next hit
  kills; otherwise they bleed out.
- Defender armor (absorbs first hit per heart unless focus-fired by 2+).
- Defender respawn from outpost, drives back in (~15–30s, configurable).
- Attacker permadeath → shoulder-spectator (locked to one living teammate's
  exact view, switchable).
- Reinforcement ramp + hard time-floor (trickle start / floor times
  configurable).

**Phase 3 — Getaway**
- The three objectives (documents / gold / heavy gear) — **all three, full
  triangle.** This is the variety engine; cutting it tests a blander game.
- Assigned mission per round (crew told which one).
- DF tracking on documents only: hold-to-consult HUD panel, audible ping on each
  fix, full-speed opaque overlay, fire/ADS dismisses.
- Universal relay (any teammate can carry any objective; tracking follows the
  object).
- Loot-as-football (kill carrier → loot drops → re-stealable).
- All map edges are valid exits.

**Cross-cutting**
- **Audio as a primary sense** — directional engines/footsteps/gunfire, distinct
  DF-ping and reinforcement cues, sector-coarse alarm audio. Networked positional
  audio is in from day one (the concept doc flags it can't be bolted on later).
- **Night-only lighting** (see decision below).
- **One greybox map** (see map section).

---

## OUT — named so it can't creep back in

- All **defender spike verbs** (controllable portcullis, placeable demo charge,
  searchlight re-aim / lights-out). Portcullis exists only as a default-locked
  map fixture, not a live-triggered tool.
- **Paradrop** spawn type.
- **Two-person simultaneous heavy-gear carry** (MVP heavy gear is slow-solo).
- **Matchmaking** — friends-first / direct LAN connect only.
- **Day lighting / day-night cycle** — night-only for MVP.
- **Multiple maps** — one map.
- **Destructible DF towers**, beacon-ditching counterplay.
- **See-investment scaling** on tracking (documents tracking is intrinsic and
  unconditional, as designed).

---

## DEFERRED-BUT-DECIDED

These are *designed* (decisions made in the concept doc) but not built for MVP — listed
so the OUT list doesn't read like a pile of unsolved problems:

- Spike verbs — designed (weak-alone/lethal-combined, timing-not-location).
- Paradrop — fully specced (descent seconds, swoosh audio, objective-dependent
  value).
- Four-biome "real" map — the named successor to the MVP greybox (see below).
- Prisoner/escort objective — **dropped permanently**, replaced by heavy gear.

---

## Key MVP decisions (resolved this session)

**No hardcoded player caps.** We LAN with 5–6; we playtest with everyone. N is
general everywhere it appears.

**Tuning is host-settable lobby config, not constants.** Every "tune in playtest"
value is a lobby option the host can change between rounds. Build them as config
from the start — retrofitting sliders onto hardcoded constants later is painful.
Minimum config surface for MVP:

| Config | Concept-doc starting value |
|---|---|
| Supply base | 25 |
| Supply per attacker (N) | 5 |
| Barbed wire / Trip flare / Barricade / S-mine / MG nest / Searchlight / Body cost | 1 / 2 / 2 / 3 / 5 / 5 / 7 |
| S-mine damage | open (1 heart + alarm, or 2 hearts) |
| Bleed-out timer (X sec) | TBD |
| Defender respawn drive-in time | ~15–30s |
| Reinforcement trickle-start time | ~5:00 |
| Hard time-floor | ~10–12 min |
| DF ping cadence | ~5s |
| DF fix type | bearing-only vs position-fix |
| Loot-carrier slowdown | TBD |

**Night-only.** More diegetic (you raid at night), and it makes flares /
searchlights / trip-flares earn their place immediately instead of being
half-dead in daylight. Consequence, stated deliberately: night raises the bar on
exactly the systems we most want to validate — NPC sight cones, the "is that nest
crewed?" tells, and audio cues all have to actually work in MVP, with no daylight
readability crutch. That's the point.

---

## The MVP map

Simpler than the eventual four-biome castle. The map only has to satisfy the
*functional* requirements; the fiction is a thin diegetic skin so greybox
decisions aren't arbitrary.

**Framing fiction:** a castle on high ground presiding over a town, approached
across farmland on one side and through forest on another. The keep holds the
objectives.

**Functional requirement → diegetic skin → greybox scope:**

- **Open / exposed lane → open road across farmland.** *Why exposed:* open
  ground, crossing = being seen; where searchlights and tower sightlines bite.
  Greybox: flat plane + road, no grass/crop detail. The gold dash hates it; the
  documents sprint wants to avoid it.
- **Covered / clearable lane → forest with a track through it.** *Why clearable:*
  dense cover lets the team leapfrog and clear ahead. Greybox: cover blockers
  flanking a path. Natural home for wire / mines / trip-flares (defender knows
  you're funneled to the track).
- **Choke / pocket-y lane → the town below the castle.** *Why pockets:* urban
  geometry = short hops between hard cover, blind corners, hide-and-fight rooms.
  The heavy-gear haul's habitat.

**Also provides for free:** multiple edges for per-player spawn selection and
all-edges extraction, and a clear "above it all" keep.

**MVP scope:** grey-box terrain. No beach/water sim, no forest density pass, no
biome art. Objectives spread so each lane's character is exercised (exact
placement = future map doc, not here).

**Successor:** the N-grassland / E-hills / S-forest / W-beach castle is the same
idea dressed up — first "real" map, its own doc later.

---

## What we're watching in playtest

Not a formal validation methodology — this game's core questions are feel, not
statistics. This is a checklist to keep asking out loud so we don't go numb to
the things that matter.

**The one real number — the master dial:**
> Across a session, are attacker wins and defender wins roughly balanced, or is
> one side always winning? Chase it with the lobby config. Everything below is
> feel.

**Feel-questions to keep asking (the scary bets):**
- **The triple-tradeoff camera** — aim = accuracy = camera-push, with 3 hearts
  (not instakill like Hotline Miami). Does aiming-far-then-dealing-with-blind-
  behind feel elegant, or like fighting the controls?
- **Camera-push UX** — snap vs lazy-follow on release, push radius/shape. Easy to
  feel awful; watch it.
- **DF hold-to-consult chase** — hold + ~5s ping + driving + bearing-only stacks
  up. Does documents land as "hardest objective" (fine) or "defender literally
  can't catch them" (not fine)? Dials: ping cadence, bearing-vs-position, overlay
  opacity.
- **Combined-arms thesis** *(the core design claim)* — does a crew that ignores
  the defender still crack a pure turtle? AND does a defender stripped of
  defenses lose 1vN? Both must be true.
- **Automated-vs-present needle** — does killing the defender feel like it
  matters, without defenses being mere decoration?
- **The ramp** — abort-or-commit gradient with the occasional glorious minute-9
  extraction, or just a loss timer?
- **Sten TTK @ 20m** *(the bellwether)* — if this feels wrong, "tactical not
  rush-B" is wrong, and everything downstream feels wrong.

Record what you see in the playtest log (`../playtest/log.md`).
