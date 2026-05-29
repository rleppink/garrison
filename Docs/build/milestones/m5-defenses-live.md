# M5 — Defenses live (execution side)

> The **behavioural** half of `Defenses/`.
> **Slice(s):** `Defenses/` (behaviour) · **Depends on:** M4 (the placed things)
> + M3 (the cones the AI drives) · **Proves:** the **combined-arms thesis** is
> testable.

Detail for M5 in [`../plan.md`](../plan.md). The combined-arms principle and the
defender toolkit are in [`../../design/concept.md`](../../design/concept.md).

## Why here

M4 lets the defender *place* a position; M5 makes it *do* something. The kit is
seven different devices plus NPCs, so this file is shaped around **the one
contract every device shares** (which is what makes "lethal if unseen, beatable
if seen" a *consistent* rule rather than seven special cases), then the NPC
behaviour state machine, then the home-field visibility rule.

## The device contract (the shape every placeable shares)

Every device fills the same five slots:

- **Trigger** — what activates it (proximity / tripwire / line-of-fire / passive).
- **Effect** — what it does (damage / alert / light / block-slow / cover).
- **Tell** — how an attentive crew spots it.
- **Counter** — how a *seeing* crew beats it.
- **Config** — its dial(s).

This contract *is* the information-gating design: every device has a tell and a
counter, so scouting time is the "pay attention" tax.

## The device catalog

| Device | Trigger | Effect | Tell | Counter | Key config |
|---|---|---|---|---|---|
| **S-mine** | proximity | damage (open: 1♥+alarm *or* 2♥) | placed object; detector | detector reveal; route around | **S-mine damage** |
| **Barbed wire** | passive | block / slow | visible | cut (time + noise) | — |
| **Trip flare** | tripwire | light + alert, no damage, single-use | the wire | spot & step over | — |
| **Barricade** | passive | cover + shape | visible | route around | — |
| **MG nest** | line-of-fire *(if crewed)* | suppressing fire — or bluff, if not | crewed-vs-not is *the* read | flank; draw away; body-snipe | — |
| **Mobile searchlight** | passive (arc) | reveals an arc | the beam | stay out of arc | arc width |

The MG nest's **crewed-vs-not** is the headline deception: unmanned, it reads
crewed from a distance; a body-snipe turns a 12-Supply manned MG into a 5-Supply
inert nest — which makes "where do I put the gunner so they aren't an easy shot?"
a real planning question.

## NPC behaviour (drives the M3 cones)

A perception→action state machine layered on the M3 stub, parameterised by role:

```
Patrol/Watch ──perceives target (M3)──► Alert ──acts per role──► (target lost / timeout) ──► Patrol/Watch
```

- **Patrol:** walks the assigned route; cone sweeps.
- **Tower sentry:** static post, watches an arc.
- **MG gunner:** crews a nest, fires within its arc; **flankable / drawable** —
  the body is a separate entity from the nest.
- **Permadeath:** a killed body is gone for the round; reinforcements don't
  backfill assigned posts; the placeable underneath persists as inert cover,
  crewable by the defender themselves.

## Map-fixture behaviour
- **Locked doors / portcullis:** default-locked, breachable at a time + noise
  cost. **No live defender trigger** (that's a parked spike verb).
- **Alarm bells:** the source of sector-coarse alarm audio.

## Defender home-field (a visibility rule, not a device)
The defender **always sees their own placements** (mines/wire/MG arcs are never
fog to them) and knows the safe lane through their own kill-boxes — retreat
*through* the minefield is literal. Counterplay is diegetic: an attentive crew
copies the path the defender takes.

## Config surface introduced
- **S-mine damage** (the open 1♥+alarm vs 2♥ dial).
- Searchlight arc; NPC alert threshold / patrol speed; breach time + noise.

## Audio
- Mine blast, flare pop, searchlight presence, MG fire.
- NPC alert callouts (fleshed out from the M3 stub).
- **Sector-coarse alarm bells** — the defender reads the raid by ear.

## Done when (the combined-arms test)
- A crew that **ignores the defender can still crack a pure turtle** — defenses
  are strong but not *sealed*.
- A defender **stripped of defenses loses** a straight 1vN.
- Both must hold; tune by cost and dials, not rule-text.
- The bluff pays off ("is that nest crewed?") and a body-snipe visibly neuters an
  expensive placement.

## Explicitly not in M5
- Defender spike verbs (parked, post-MVP), reinforcements (M7), objective
  carry/tracking (M6).
