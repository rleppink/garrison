# GARRISON — Milestone Sub-Plans

Per-milestone detail, broken out from [`../plan.md`](../plan.md). `plan.md` owns
the **sequence and the why**; each file here owns **what one milestone contains,
what config and audio it lands, and what it has to prove** before we move on.

These stay above the "wire this component to that component" level — *how* the
code is organized is [`../architecture.md`](../architecture.md)'s job. Each
sub-plan answers: what sub-features does this milestone grow, which config dials
and audio cues come online with them, and what can you *play* when it's done.

## Order & dependency shape

```
M0 ─► M1 ─► M2 ─┬─► M3 ─► M5 ┐
                │            ├─► M6 ─► M7 ─► M9
                └─► M4 ──────┘
                         M8 (map) can develop in parallel from M3 on
```

| Milestone                            | Slice(s) grown                         | Proves                                                       |
|--------------------------------------|----------------------------------------|--------------------------------------------------------------|
| [M0](m0-walking-skeleton.md)         | `Shared/`                              | LAN connect + net spine + config plumbing are real           |
| [M1](m1-camera-movement.md)          | `Vision/` (camera)                     | the triple-tradeoff camera — go/no-go feel gate              |
| [M2](m2-combat-core.md)              | `Combat/`                              | a firefight reads tactical, not twitch (Sten TTK bellwether) |
| [M3](m3-fog-and-cones.md)            | `Vision/` (fog)                        | peeking/flanking are real; fog is server-truth               |
| [M4](m4-planning-and-supply.md)      | `Planning/` + `Defenses/` (placement)  | a defender can build a position; a crew can pick approaches  |
| [M5](m5-defenses-live.md)            | `Defenses/` (behaviour)                | the combined-arms thesis is testable                         |
| [M6](m6-objectives-and-getaway.md)   | `Loot/` + `Tracking/`                  | a full round runs end-to-end; first DF-chase read            |
| [M7](m7-reinforcements.md)           | `Reinforcements/`                      | the ramp as abort-or-commit; defender wins reachable         |
| [M8](m8-greybox-map.md)              | (the map)                              | each lane exercises its objective; night readability holds   |
| [M9](m9-mix-spectator-resolution.md) | `Shared/` mix + spectator + resolution | ready for the first real 5–6 person LAN session              |

## Two threads that run through *every* file

Straight from `plan.md`, restated here because they're the easiest things to let
slip into a final-step bolt-on:

- **Config, not constants.** Every "tune in playtest" value lands as
  host-settable lobby config *the moment its system is built*. Each sub-plan has
  a **Config surface** section listing what it adds to the table in
  [`../../design/mvp-scope.md`](../../design/mvp-scope.md).
- **Audio as a primary sense.** The audio bus goes in at M0; each system adds
  its cues *as it lands*. Each sub-plan has an **Audio** section. M9 is a
  consolidation/mix pass, not the first time sound exists.

And under all of it: **server-authoritative first**, prediction layered on later
only where feel demands it.
