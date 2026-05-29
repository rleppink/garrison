# GARRISON — Playtest Log

*The trail that turns "feels and vibes" into something readable across sessions.
One entry per playtest session. The job: track what config we ran, who won, and
what we noticed — so balance is chased by playing, not guessing.*

How to use:
- One **session block** per LAN night. Set the config, play several rounds, tally.
- Win-rate balance is the master dial — log it every round.
- The "Notes / feel" column is where the scary bets get watched (see the
  "What we're watching" checklist in `../design/mvp-scope.md`). Write down gut
  reactions while they're fresh; patterns emerge across sessions.
- When you change a config value between rounds because of what you saw, note
  *why* — that causal trail is the whole point.

---

## Template (copy this block per session)

### Session YYYY-MM-DD — <players, e.g. "1 def + 5 att">

**Config this session** *(note mid-session changes inline in the rounds table)*

| Config                                               | Value                       |
|------------------------------------------------------|-----------------------------|
| Supply base / per-N                                  | 25 / 5                      |
| Item costs (wire/flare/barricade/mine/MG/light/body) | 1/2/2/3/5/5/7               |
| S-mine damage                                        |                             |
| Revive window (s)                                    |                             |
| Defender respawn drive-in (s)                        |                             |
| Reinforcement trickle-start                          | 5:00                        |
| Hard floor                                           | 10:00–12:00                 |
| DF ping cadence (s)                                  | 5                           |
| DF fix type                                          | bearing-only / position-fix |
| Carrier slowdown                                     |                             |
| Map                                                  | (MVP greybox castle)        |

**Rounds**

| # | Objective           | Winner    | ~Length | Config change after | Notes / feel |
|---|---------------------|-----------|---------|---------------------|--------------|
| 1 | docs / gold / heavy | att / def |         |                     |              |
| 2 |                     |           |         |                     |              |
| 3 |                     |           |         |                     |              |
| 4 |                     |           |         |                     |              |
| 5 |                     |           |         |                     |              |

**Session tally:** attackers __ / defenders __

**Watching (the scary bets) — what did we feel this session?**
- Camera triple-tradeoff (aim/accuracy/push):
- Camera-push UX (snap vs lazy, radius):
- DF hold-to-consult chase (docs winnable for def?):
- Combined-arms (turtle crackable? lone def loses 1vN?):
- Automated-vs-present (does killing def matter?):
- Ramp (gradient or just a timer?):
- Sten TTK @ 20m (tactical or rush-B?):
- Per-objective feel (each of the 3 a "different round"?):

**Decisions / changes carried into next session:**
-

---

<!-- Add new session blocks below this line, newest at the bottom. -->

### Session 2026-05-29 — M0 skeleton local acceptance

**Config this session**

| Config      | Value |
|-------------|-------|
| PlayerCount | 6     |
| MoveSpeed   | 4.5   |
| Map         | Greybox |

**Result**

Local implementation acceptance only. Unity batchmode import/compile succeeded,
`Garrison.Shared` built with 0 errors, the Shared services/prefab/map are wired
in assets, and the slice dependency wall still holds.

**Still pending**

- Multi-instance LAN run with 2–6 players.
- Runtime confirmation that Start loads `Greybox`, spawns one capsule per
  player, moves server-authoritatively, and Reset preserves config.
- Real AudioMixer asset/group routing once the Unity editor is responsive.
