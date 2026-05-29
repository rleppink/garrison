# M9 — Audio mix, spectator, round resolution → first full playtest

> Consolidation plus the last pieces that make a *session* real.
> **Slice(s):** `Shared/` (mix) + spectator + round resolution · **Depends on:**
> M6 + M7 (and the M8 space) · **Proves:** ready for the first real 5–6 person
> LAN session.

Detail for M9 in [`../plan.md`](../plan.md). Spectator and win conditions are in
[`../../design/concept.md`](../../design/concept.md); the playtest checklist is
in [`../../design/mvp-scope.md`](../../design/mvp-scope.md).

## Why last

Everything mechanical exists by M7; the map by M8. M9 is **three distinct
things** that finish the loop, so each gets the shape that fits it: an *inventory*
for the audio pass, a *system* for the spectator, a *lifecycle* for resolution.
Note M9 is not where sound is born — each system has added cues since M0; this is
the mix.

## 1. Audio mix — a consolidation pass (inventory + goals)

No new systems; a balancing pass over everything that's accreted. The cue
inventory to mix:

- engines (reinforcements, defender respawn) · footsteps · gunfire ·
  S-mine / flare · DF ping · sector-coarse alarm bells

Mix goals:
- the **off-screen world is legible by ear** — direction + rough distance;
- the **distinct cues are unmistakable** — DF ping, reinforcement engine, alarm
  can't be confused for each other;
- it holds at **night with no visual crutch**.

Deliverable = the balanced mix, not new code.

## 2. Shoulder-spectator (a system)

- **Responsibility:** keep a dead attacker engaged without leaking info.
- **Owns:** the bound living-teammate reference, and the rule that the spectator
  sees *exactly* that teammate's view — same camera, LOS, fog. **No** map
  overview, **no** enemy/NPC/trap the teammate hasn't seen.
- **Switch:** rebind to another living teammate; comms stay live.
- **The leak and its lever:** switching across teammates can stitch a slightly
  wider local picture — an accepted leak. Add a **switch-blackout cost** (config,
  default off) *only if* it gets abused. The design line: enough agency to stay
  engaged, not enough to make "sacrifice a guy to scout" a strategy.

## 3. Round resolution (a lifecycle state machine)

The round/session loop closes here:

```
Lobby ──start──► Planning ──lock──► Execution/Getaway ──win condition──► Resolved ──reset──► Lobby
```

- **Win evaluation:** attackers win the instant one survivor crosses any edge
  with the loot; the defender wins by eliminating the whole crew or reaching the
  hard floor.
- **Reset:** back to lobby/planning, **config changeable between rounds** — this
  closes the loop the whole "config, not constants" thread was built for.
- **Playtest hooks:** capture into [`../../playtest/log.md`](../../playtest/log.md)
  what a session needs to record.

## What "ready" means (the master dial)

M9's real deliverable is a **runnable session**: 5–6 people, full round, clean
resolution, reset, repeat — and the ability to start chasing **the one number**
(are attacker and defender wins roughly balanced?) by turning config, not by
spreadsheet.

## Config surface introduced
- Audio levels / mix; `switchBlackoutCost` (default off). Nothing new
  mechanically — M9 is consolidation.

## Audio
- The mix itself *is* the deliverable: the moment the night / no-god-mode bet has
  to fully pay off through the speakers.

## Done when (ready for the first real LAN session)
- 5–6 people play a full round to a clean resolution and reset for another.
- Dead players stay engaged via shoulder-spectator without an info leak that
  makes scout-sacrifice a strategy.
- The off-screen world reads by ear — the night, no-daylight-crutch bet holds.
- You can run a session, fill in `playtest/log.md`, and start chasing the master
  dial with config.

## Explicitly not in M9
- Anything on the `mvp-scope.md` OUT list. M9 ships the lean kit and we *play*.
