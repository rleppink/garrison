# M4 — Planning phase & Supply

> The `Planning/` slice + the **placement** half of `Defenses/`.
> **Slice(s):** `Planning/` + `Defenses/` (placement only) · **Depends on:** M3
> (needs the NPC/cone stub to assign bodies to) · **Proves:** a defender can
> build a position and a crew can pick approaches — the round now has a setup.

Detail for M4 in [`../plan.md`](../plan.md). Planning design in
[`../../design/concept.md`](../../design/concept.md) ("Phase 1 — Planning") and
the IN-list in [`../../design/mvp-scope.md`](../../design/mvp-scope.md).

## Why here

Combat (M2) and fog (M3) give a live firefight in a visible world; M4 gives both
sides something to *prepare*. This milestone is fundamentally about **data and a
phase lifecycle**, not runtime behaviour — everything placed here gets its
*behaviour* in M5. So it's shaped around the artifact both sides author (the
Plan), the lifecycle that locks it, and the two editing surfaces.

## The Plan (the artifact both sides build)

The central thing M4 produces is a **Plan** — the round's setup, authored in this
phase and handed read-only to execution. It holds:

- **Supply ledger** (defender): budget `25 + 5N`, debited by placements and
  bodies. **Caps emerge from cost, never from rules.**
- **Placements** (defender): instances of the 7-item toolkit positioned in the
  world — wire / flare / barricade / S-mine / MG nest / searchlight. Here each is
  just `type + position + cost`; behaviour is M5.
- **Body assignments** (defender): each NPC body tagged with a role — patrol
  route / tower sentry / MG gunner.
- **Valuable placements** (defender): the three objectives positioned — the
  **concentrate vs spread** decision.
- **Spawn selections** (per attacker): an edge position each.
- **Gear draft** (attackers): allocation from the shared finite pool + who
  carries the scarce tools (detector, syrettes).
- **Assigned mission** (round): which one of three the crew must take.

## Planning lifecycle

A phase state machine:

```
Enter planning → edit loop (both sides, blind to each other) → LOCK
```

At **lock**, roles / placements / valuables freeze and the Plan becomes the
read-only input execution consumes. **"No mid-round re-tasking" and "role locked"
are *consequences of the lock*,** not separate rules — there's nowhere to change
a role after it.

## The two authoring surfaces

- **Defender board:** spend Supply; place the toolkit and the three valuables;
  assign bodies to roles; and **preview the scene from each attacker spawn POV**
  to stress-test blind hedging before committing.
- **Attacker board:** pick your *own* edge spawn — **mutable throughout planning,
  visible to teammates** so the crew can plan a split (feint front, slip a side);
  draft from the shared gear pool; read the assigned mission.
- **Legibility requirement:** both boards must stay readable with **5–6 attacker
  markers** — designed for general `N`, not built for 2 and stretched.

## Map fixtures (referenced, not owned)

Watchtowers, lockable doors, portcullis-as-locked-fixture, alarm bells, pre-built
positions are **free** and exist in the world independent of the Plan (greybox
stand-ins until M8). The Plan *references* them; it doesn't own them.

## Seam to execution

The locked Plan is the hand-off. **M5** reads placements + body assignments and
brings them to life; **M6** reads valuable placements. The Plan is the contract
between planning and execution — built here, consumed there.

## Config surface introduced
- `supplyBase` (25), `supplyPerN` (5).
- The 7 item costs (the table in `mvp-scope.md`).
- Mission-assignment source (random per round vs host-set).

## Audio
- Light place/confirm UI cues. The alarm is "pre-sounded" *fiction* here; actual
  alarm/bell audio lands with the fixtures in M5.

## Done when
- A defender spends Supply on a mixed build, assigns bodies to roles (locked at
  end), and places valuables concentrate-or-spread.
- Each attacker picks an edge spawn, sees teammates' picks, and the crew can plan
  a split.
- The defender previews the scene from each spawn POV.
- Both boards stay readable at 5–6 markers.

## Explicitly not in M4
- Any execution behaviour of the placed things (M5), objective carry/tracking
  (M6), reinforcements (M7).
