# M6 — Objectives & getaway

> The `Loot/` + `Tracking/` slices.
> **Slice(s):** `Loot/` + `Tracking/` · **Depends on:** M5 · **Proves:** a full
> round runs end-to-end (grab and run for an edge), and the **DF hold-to-consult
> chase** gets its first read.

Detail for M6 in [`../plan.md`](../plan.md). The objective triangle and DF
tracking are in [`../../design/concept.md`](../../design/concept.md)
("Phase 3 — Getaway", "The three valuables").

## Why here

With a position that defends itself (M5), there's finally something to steal. M6
closes the core loop — plan, break in, grab, run — and lands the second big
feel-bet, the documents DF chase. It's shaped around the carry *data*, the
ownership *state machine* that makes relay and the football work, and the DF
*system*.

## The carry model (the variety engine, as data)

The three objectives differ on three axes; each is the sole odd-one-out on
exactly one. As data:

| Objective | Tracked | Speed | Hands / combat | Plays as |
|---|---|---|---|---|
| **Documents** | yes (beacon) | fast | one hand (can shoot) | *only tracked* — can't hide; run-and-gun |
| **Gold** | no | fast | two hands (can't fight) | *only defenceless* — outrun & vanish, lean on team |
| **Heavy gear** | no | slow | hands free | *only slow* — hide & fight; cornering = death |

Picking one up applies its profile (movement/combat modifiers) to the carrier.
**The profile lives on the object,** which is what makes relay and tracking
behave correctly below.

## Objective ownership (pickup / relay / football)

A state machine on the **object**, not the carrier:

```
Stashed ──pickup──► Carried(by X) ──relay──► Carried(by Y)
                       │
                       └──carrier killed──► Dropped(in place) ──re-pickup──► Carried
```

- **Universal relay:** any teammate can take over; the profile *and* the beacon
  follow the object, never the carrier. You pass the load — you can never pass
  off the visibility.
- **Football:** kill the carrier → drop where they fell → re-stealable by either
  side. Defender recovery is **denial only** (they still win only by
  elimination / time-floor), which dodges a both-sides-hug-the-loot stalemate.

## DF tracking (documents only)

A defender-only system, **intrinsic and unconditional** for the documents
mission:

- **Beacon → periodic fix:** once the documents are carried, the position gets a
  bearing every ~5s (config) — a *read, not a lock*.
- **Hold-to-consult panel:** hold a key → a mostly-opaque paper-map overlay with
  the latest fix; release dismisses. **Full speed, motor control intact — the
  cost is *attention*** (you can't see threats, terrain, or where your shot
  lands). Driving keeps the last heading. **Hold, not toggle**, so it can't be
  left up in a firefight.
- **Audible ping** on each fresh fix regardless of panel state — you know new
  info exists and choose *when* to look.
- **Fire / ADS dismisses** the panel — no dual-wielding aim + fix.
- **Bearing-only vs two-tower position-fix** is the dial; prototype the harsher
  **bearing-only** first.
- Towers are non-destructible, off-screen props — the cue lives on the HUD, not
  in the world.

## Extraction & win check (seam)
All map edges are valid exits; **attackers win the instant a single survivor
crosses any edge with the loot.** The defender's path to win (elimination) needs
the ramp, which is M7 — so M6 resolves an attacker win and a manual/elimination
end; M7 makes the defender's win *reachable* and M9 wires the formal resolution.

## Config surface introduced
- DF ping cadence (~5s); fix type (bearing-only vs position-fix); consult overlay
  opacity.
- **Loot-carrier slowdown** — the single getaway dial (slowdown × ping-interval =
  the size of the "where are they now" circle).

## Audio
- The **DF ping** — a distinct cue, the heart of the chase.
- Objective pickup / drop cues; heavier footstep audio for the slow heavy-gear
  haul.

## Done when
- A full round runs end-to-end: plan → break in → grab → run for an edge →
  win/lose.
- Each objective plays as its profile (tracked / defenceless / slow), not as a
  reskin.
- Relay and football work — pass off, drop-on-kill, re-steal.
- First read on the DF chase: does documents land as the "hardest objective"
  (fine) or "the defender literally can't catch them" (not fine)? Chase it with
  the dials.

## Explicitly not in M6
- The reinforcement ramp and defender respawn (M7), shoulder-spectator (M9).
