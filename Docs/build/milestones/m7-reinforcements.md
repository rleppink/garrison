# M7 — Reinforcement ramp & respawn

> The `Reinforcements/` slice.
> **Slice(s):** `Reinforcements/` · **Depends on:** M6 · **Proves:** the **ramp**
> as an abort-or-commit gradient (and the automated-vs-present needle — does
> killing the defender matter?). Defender win conditions become reachable.

Detail for M7 in [`../plan.md`](../plan.md). The ramp and defender respawn are in
[`../../design/concept.md`](../../design/concept.md) ("Phase 2 — Execution").

## Why here

M6 lets attackers win by extracting; M7 gives the defender a path to win and puts
a clock on the whole thing. This milestone is fundamentally about **time-driven
pressure**, so it's shaped around the schedule (as config'd data), the
no-teleport arrival it produces, and the defender's respawn loop.

## The dual clock (pressure as a schedule)

Two timelines run from t=0, both expressed as **config'd schedule values, not
hardcoded waves:**

- **Reinforcement ramp:** none → **trickle ~5:00** → ramp toward "fighting an
  army." A **ramp, not a wall** — a continuous gradient, so there's an
  abort-or-commit decision every minute *and* room for the occasional glorious
  minute-9 extraction. It's also the anti-stall device: patience is inherently
  costly.
- **Hard floor ~10–12 min:** the backstop where the defender simply wins.

## Arrival (nothing teleports)

The schedule decides *when* and *how many*; the no-teleport rule decides *where
from*. Reinforcements **march/drive in from a road/edge** — visible, readable,
route-aroundable. They're a moving threat with a real spawn point, never a pop-in
in your blind spot.

## Defender respawn loop

A loop, not a stun:

```
death → respawn at outpost → drive back in (~15–30s, config) → rejoin
```

The respawn *distance* is the delay — a **positional** cost, not a frozen timer.
Death stays lethal and satisfying; "you can hear the engine coming back" (the
returning vehicle's audio is the tell).

## What this unlocks (seam to resolution)

With baseline pressure always rising, two things become true:

- The defender's **win-by-elimination is reachable** — the ramp does the heavy
  lifting of making "kill the whole crew" achievable.
- The **automated-vs-present needle** is finally readable: does killing the live
  defender buy meaningful time, *without* the static defenses being mere
  decoration?

M9 wires the formal win-resolution; M7 makes the defender's win reachable.

## Config surface introduced
- Reinforcement: trickle-start (~5:00), hard floor (~10–12 min), ramp
  rate/composition over time.
- Defender respawn drive-in (~15–30s).

## Audio
- Reinforcement engines — directional, the ramp's intensity readable by ear.
- The defender's returning vehicle (the "engine coming back" beat).

## Done when
- The ramp reads as abort-or-commit: early is calm, late is desperate, the floor
  is a wall.
- **Automated-vs-present needle:** killing the defender buys real time without
  defenses being decoration.
- Defender win-by-elimination is now reachable; respawn death stays
  lethal-but-positional.

## Explicitly not in M7
- The formal win-resolution screen / round reset (M9), shoulder-spectator (M9).
