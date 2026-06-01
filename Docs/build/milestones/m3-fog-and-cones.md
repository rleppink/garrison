# M3 — LOS fog of war + sight cones

> Extend `Vision/`.
> **Slice(s):** `Vision/` (fog) · **Depends on:** M2 · **Proves:**
> peeking / flanking / "is that nest crewed?" become real questions, and fog is
> server-authoritative, not client-trusted.

Detail for M3 in [`../plan.md`](../plan.md). See "Camera & vision" in
[`../../design/concept.md`](../../design/concept.md). The commit-by-commit build
order is in [`m3-implementation.md`](m3-implementation.md).

## Why here

The camera (M1) decides what's *on screen*; fog decides what your character can
actually *see*, turning the top-down view from god-mode into a game of
information. The defining property of this milestone is a **trust boundary**, so
the systems below are described with the authority/data-flow front and centre.

## Systems this milestone builds

### Server-truth visibility
- **Responsibility:** compute, per character, what it can currently see — and
  ensure each client only *receives* what its character sees.
- **Owns:** the per-viewer visible-set (entities/areas currently in LOS).
- **Trust boundary (the point of the milestone):** the server computes
  visibility and **withholds hidden state**. A modified client can't reveal
  through terrain because the data never reaches it. This is "server-truth, not
  client-trusted" made concrete.
- Terrain blocks sight. **Players have no cone** — their facing reads from
  posture/animation only, preserving human unpredictability.
- **Seam:** once both exist, M2's hit resolution consults this for LOS;
  rendering/observation consumes the visible-set.
- **Resolved (impl plan):** *per-object on a fixed tick* (was: per-object vs
  per-tile/region; cadence per-tick vs throttled) — cheapest that still feels
  responsive. Fog = **entity hide/reveal**, server-*withheld* (the client never
  receives out-of-LOS state), not a terrain shroud.

### NPC perception stub
- **Responsibility:** the first body that can "see" — a cone you can read and
  break.
- **Owns:** a facing, a cone (arc + range), and a can-see check against the
  visibility rules above.
- **Deliberately minimal:** it detects, but patrol/sentry/gunner *behaviour* is
  M5. Just enough to exercise fog and the readable tell.
- **Seam to M5:** the perception result (acquired target / lost target) is the
  input M5's NPC behaviour acts on. The stub *raises* it; nobody acts yet.
- **The bluff hook:** a cone sweeping the wrong arc must read visually — so M5's
  decoy-routed patrols pay off as deception.

## Config surface introduced
- Fog/LOS: view distance, update cadence.
- NPC cone: arc, range. (Light — most NPC tuning is M5.)

## Audio
- A minimal NPC alert cue (acquired-target) stub — fleshed out with the AI in M5.

## Done when
- Peeking, flanking, and "is that nest crewed?" are real questions, not solved by
  a god-view.
- Fog holds as server-authoritative — a client can't reveal through terrain by
  cheating.
- An NPC cone visibly sweeps, and you can break its LOS with terrain.

## Explicitly not in M3
- NPC patrol/sentry/gunner behaviour (M5), planning-time NPC placement (M4), the
  See tools — searchlights/flares (M5).
