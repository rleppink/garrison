# M8 — The greybox map

> The space the whole round runs in.
> **Slice(s):** content, not a code slice · **Depends on:** can develop in
> **parallel from M3 on** (later milestones only need *a* space) · **Proves:**
> each lane's character exercises its matching objective profile, and the night
> readability bar holds with no daylight crutch.

Detail for M8 in [`../plan.md`](../plan.md). The map spec is in
[`../../design/mvp-scope.md`](../../design/mvp-scope.md) ("The MVP map").

## Why it floats, and why it's an affordances doc

M8 is the one piece anyone *not* on the core loop can build, because every other
milestone only needs *a* space to run in. It has no runtime systems — so this
file isn't about state or seams, it's about **what the space must afford**: the
gameplay each lane has to make possible, and the fixtures other milestones expect
to find here.

## What each lane must afford

Functional requirement → diegetic skin → what it must make *possible* (the
requirement, not the art):

| Lane | Skin | Must afford | Serves |
|---|---|---|---|
| **Open / exposed** | open road across farmland | long sightlines, no cover — crossing = being seen | searchlights & tower sightlines bite; gold's dash hates it, documents' sprint avoids it |
| **Covered / clearable** | forest with a track | dense cover to leapfrog + a funnel the defender can read | the natural home for wire / mines / trip-flares |
| **Choke / pocket** | town below the keep | short hops between hard cover, blind corners, hide-and-fight rooms | the heavy-gear haul's habitat |

## Fixtures the map must provide (the contract M4/M5 lean on)

The map is *where the free fixtures live*, so it has to host: **watchtowers,
lockable doors, a portcullis-as-locked-fixture, alarm bells, pre-built
positions.** M4 places defenses *relative to* these; M5 gives them behaviour. If
the map doesn't provide them, those milestones have nothing to attach to — this
is the real dependency, even though M8 is listed late.

## Topology

- **Multiple edges** — enough to support 5–6 *independent* per-player spawns and
  all-edge extraction.
- **A clear "above it all" keep** holding the objectives, **spread** so each
  lane's character gets exercised (exact placement is a future map doc, not here).

## Night-only (and the bar it sets)

Night-only lighting, **deliberately no daylight crutch.** This is an *acceptance
bar*, not a mood choice: NPC cones, the "is that nest crewed?" tells, and the
audio cues must all work in the dark. Choosing night is choosing to validate
exactly those systems with no fallback — that's the point.

## Config surface introduced
- Minimal — light level at most. M8 is content, not dials.

## Audio
- An ambient night bed. The map provides the acoustic *space* more than new cues.

## Done when
- Each lane's character exercises its matching objective profile (gold on the
  open road, heavy gear in the town, documents on the long routes out of tower
  sightlines).
- The **night readability bar holds** — cones, defense tells, and audio all work
  with no daylight.
- The topology supports 5–6 independent spawns and all-edge extraction.

## Explicitly not in M8
- Beach/water sim, forest density pass, biome art, multiple maps, the four-biome
  "real" successor map — all named OUT in `mvp-scope.md`.
