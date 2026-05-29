# M1 — The camera & movement feel-bet

> Build the `Vision/` slice's camera before anything shoots — the riskiest
> thing, so it goes first.
> **Slice(s):** `Vision/` (camera + movement feel) · **Depends on:** M0 ·
> **Proves (go/no-go):** does aim = accuracy = camera-push with the
> blind-behind tradeoff feel *elegant*, or like fighting the controls?

Detail for M1 in [`../plan.md`](../plan.md). Camera/vision design in
[`../../design/concept.md`](../../design/concept.md) ("Camera & vision").

## Why it's the gate

This milestone's deliverable is really a **decision**, not just a system — so
it's shaped around one system, the seam it must leave behind, and the experiment
that judges it. It sits under everything, it's the biggest "elegant or fiddly?"
gamble in the design, and it's testable with 2 people before a single objective
exists. If it's bad, this is the cheapest possible moment to learn it.

## The camera system
- **Responsibility:** present a top-down/iso view that follows the character and
  extends toward where they aim — under one hard invariant.
- **Owns:** the camera's target framing, derived from (character position, aim
  vector, push dials).
- **Invariant (non-negotiable):** *the character never leaves the screen.* Every
  dial below operates inside this constraint.
- **The dials (all config — this is the part that's easy to feel awful):**
  - push coupling: tied to aim vs a separate input
  - return behaviour: snap vs lazy-follow on release
  - push extent/shape: radius / ellipse / asymmetric
  - zoom: tuned so off-screen props (DF towers) aren't normally visible
- **The framing it has to sell:** one input, three costs — aiming far lets you
  *see ahead*, but you go *blind behind*, and (from M2) you also *shoot less
  accurately*.

## The movement-state seam
- **Responsibility:** expose the character's current movement state
  (idle / moving / sprinting, or a continuous speed) as a value other slices read.
- **Why it lives here, not in M2:** accuracy (M2) consumes it, but it's a
  property *of movement*, so movement owns it. M1 ships the hook even though
  nothing reads it yet — that's the seam.

## The feel experiment (the go/no-go gate)
Because M1's output is a verdict, name what we're testing and what each outcome
looks like:
- **The bet:** 3 hearts + aim-pans-camera + (incoming) movement-inaccuracy is a
  *new* combination. Hotline Miami only got away with aim-pans-camera because
  everything was instakill; here the cursor is aim *and* camera *and* accuracy.
- **Pass:** aiming far to peek or cover reads as a deliberate, controllable
  tradeoff.
- **Fail:** it feels like fighting the camera, nausea on release, or losing track
  of your own character.
- A fail is *allowed* to send us back to rethink the camera before any game is
  built on it.

## Config surface introduced
- Camera: push coupling, snap-vs-lazy-follow, push radius/shape, zoom.
- Movement speed(s).

## Audio
- **Footsteps** — the first real consumer of the M0 bus: directional,
  distance-attenuated. Movement is the first thing in the game that makes noise.

## Done when (go/no-go)
- Two people move and aim around the greybox: aiming far pushes the camera, the
  character stays on screen, the blind-behind tradeoff is felt.
- Release behaviour reads as controllable, not nauseating.
- **The gate:** elegant, or fighting the controls? A "no" is a valid, useful
  result.

## Open decision to resolve here
- Local-movement prediction (PurrDiction) — add it *only* if authoritative
  movement feels bad in this milestone.

## Explicitly not in M1
- Shoot/accuracy resolution (M2 reads the movement hook), LOS fog (M3), sight
  cones (M3), any weapon or damage.
- The planning-phase **high-overview camera mode** (M4). M1 builds only the
  execution-gameplay camera; planning is a later `Vision/` mode over the live
  `Map` (per `architecture.md`), not built here.
