# M3 — Implementation plan (commit-by-commit)

> Engineer-facing companion to [`m3-fog-and-cones.md`](m3-fog-and-cones.md). That
> doc owns the *why* — the trust boundary, the two systems, and what M3 has to
> prove. This one owns the *how* and the *order you type it in*. Conventions are in
> [`../architecture.md`](../architecture.md); vision design in
> [`../../design/concept.md`](../../design/concept.md) ("Camera & vision").

## How to read this

Each section below is **one commit**: a self-contained, compiling, reviewable
step, ordered by dependency — do them top to bottom. Every commit lists:

- **Goal** — the one thing this commit makes true.
- **Build** — files/assets/components to add or move (paths under `Assets/`).
- **Notes** — PurrNet/Unity specifics, design intent, and gotchas.
- **Done when** — how you know the commit is finished.

As in M1/M2, **there is no separate config commit.** The config *system* is M0's;
M3 only *adds keys*, so every dial lands **inline** with the system that reads it —
a new `ConfigKey` enum entry plus a typed field + `Entries()` yield in
`ConfigDefaults`, read through `IConfig`. The "config, not constants" rule holds:
**no LOS or cone tunable is a magic number** (the `mvp-scope.md` config table is
the running checklist).

The netcode is the one place to **verify signatures against the pinned PurrNet
docs, not assume** — M1 paid the version-drift tax once (the
`SyncVar<MovementState>` hasher crash). M3's load-bearing assumption is
**per-observer network visibility** (the server *withholds* state from a client
that shouldn't see it). That assumption is the whole fog half of the milestone, so
— exactly like M2's prefab-variant composition — **C1 spikes it before anything
builds on it.**

### Starting state (verified)

- Unity `6000.4.9f1`, URP, Input System; PurrNet `1.19.1`. M2 is **Accepted**
  (`m2-implementation.md` C9, `2026-05-31` "feels good").
- **`Vision/` holds only the camera** — `Garrison.Vision.asmdef` (references
  `Garrison.Shared` + `PurrNet.Runtime`) and `CameraRig.cs` (local, not
  networked). Everything M3 adds is the slice's first *networked* contents.
- The player body is the only networked gameplay entity today. It already exposes
  the Shared seams M3 reads:
  - `IAssignedPlayer` (`PlayerID AssignedPlayer`) — maps a body to its player, so
    the visibility service can address an **observer** (PurrNet visibility is
    per-`PlayerID`).
  - `IFacingSource` (`Vector2 Facing`) — server-truth facing (M2 C2). Not needed
    for fog (players cast no cone); it is the pattern the NPC cone mirrors.
  - `ILocalPlayerView` / `LocalPlayerRegistry` — the persistent Bootstrap service
    that **observes PurrNet's `HierarchyFactory.onIdentityAdded/Removed`** via the
    inspector-wired `NetworkManager`. **M3's visibility service reuses this exact
    observation pattern** to enumerate vision agents without `Find`/singletons.
- `PlayerSpawner` (Player slice) spawns one **server-owned** `Combatant.prefab`
  (the M2 variant of `PlayerBody`) per player into the `Map` scene; the base
  `NetworkTransform` is `_ownerAuth: 0` (server-authoritative position — the thing
  fog has to be able to withhold).
- `WeaponFire` (Combat) already server-raycasts shots against terrain colliders
  (`hitMask`), so **a bullet is already physically blocked by terrain.** M3 does
  **not** add an LOS gate to hit resolution (see "What M3 is *not*").
- The `Map` scene is a near-empty greybox plane — **there is no sight-blocking
  geometry yet** (the real three-lane greybox is M8). M3 stands up *throwaway*
  blockers (C2) and *one throwaway hand-placed NPC* (C4) to exercise fog and the
  cone.
- `AudioChannel` has the M0 channels (`Weapons`, `Footsteps`, and an alarm/alerts
  channel); the NPC alert cue (C5) is the first customer for the alert channel —
  **verify the exact `AudioChannel` member before wiring.**
- `ConfigKey` ends at the M2 weapon/syrette keys. M3 grows the enum
  (`ViewDistance`, `LosTickRate`, `NpcConeArc`, `NpcConeRange`).

### What M3 is *not* (from the milestone doc)

- **No NPC behaviour.** The NPC is a *perception stub* — facing, a visible cone,
  and a can-see check that raises acquired/lost-target. **Patrol/sentry/gunner
  behaviour is M5**, which consumes the perception seam this milestone defines.
  The only motion M3 gives the cone is a trivial scripted **sweep** so the tell
  reads as moving — that's presentation, not AI, and M5 replaces it.
- **No terrain shroud overlay.** "Fog" here is **entity hide/reveal** — the server
  withholds out-of-LOS entities and they pop in/out as LOS changes. There is no
  darkened-unseen-terrain rendering; it isn't part of the trust boundary.
- **No LOS gate on hit resolution.** `WeaponFire`'s raycast already stops at
  terrain, so terrain already blocks shots. We leave the seam *obvious* (a one-line
  note at the hitscan) but add no coupling — the visible-set is for
  rendering/observation, not the bullet.
- **No prediction.** Visibility and perception are server-decided; there is nothing
  to predict.
- **No shared-team vision and no defender home-field reveal.** Fog is strictly
  **per-character** (the concept's shoulder-spectator — "same LOS, same fog,
  switchable to another teammate" — assumes each player has their *own* fog, not a
  team union). The defender's "always sees their own placements" is **M4/M5** (no
  placements exist yet). A `TeamVisionShared` escape hatch is noted in case
  playtest finds pure per-character fog too punishing.
- **No See tools** (searchlights / flares — M5), **no planning-time NPC placement**
  (M4). M3's NPC is hand-placed throwaway scaffolding.

---

## Decisions taken into this plan

The milestone doc left decisions open; this plan **resolves them** (folding in this
session's steers). Each is recorded again in C6.

- **Fog = entity hide/reveal, server-withheld — not a terrain shroud.** The
  defining property is the trust boundary: hidden entity state *never reaches* the
  client, so a modified client can't reveal it. (This session.)
- **Per-object visibility on a fixed server tick** (milestone recommendation):
  per viewer, raycast to each candidate entity each LOS tick; terrain colliders
  block the ray; tick rate is a config dial. Not per-tile/region — overkill for
  MVP entity counts, and per-object matches M2's fixed-step server tick. The NPC's
  can-see check reuses the same tick + the same linecast. (This session.)
- **NPC kept in M3 as a minimal perception stub** (facing + visible sweeping cone +
  can-see → acquired/lost), **no behaviour** — confirmed this session. Keeps M4's
  attacker-POV-preview groundwork and the milestone's "is that nest crewed?" read.
- **Fog is per-character, no team union** — default, with a documented
  `TeamVisionShared` escape hatch if playtest demands it. (This session.)
- **The visibility mechanism is gated on a C1 spike.** The plan assumes PurrNet
  1.19.1 supports server-controlled per-`PlayerID` observer filtering on a spawned
  identity (state withheld, not just renderer-culled). If the spike fails, the
  documented fallbacks (C1) apply — in descending order of trust-safety.

---

## The trust boundary — why withholding, not hiding

The milestone's entire point is **"server-truth, not client-trusted."** The naive
top-down fog — spawn everything on every client, then *hide the renderers* of
things you shouldn't see — fails the boundary completely: the state is already on
the cheating client's machine, so a modified client just re-enables the renderers
and sees through walls. M3's fog is only real if the hidden entity's **data never
leaves the server** for that client.

PurrNet expresses this as **observers / network visibility**: a spawned
`NetworkIdentity` has a set of observing players, and the server can scope that
set. A player who is not an observer of an identity does not receive its spawn or
its `NetworkTransform` updates — there is nothing on their client to un-hide.
That is the mechanism M3 drives from server-computed LOS.

**This rests on one unverified assumption** — that 1.19.1 lets the server
*dynamically* add/remove a player from a spawned identity's observers at runtime,
cheaply, every LOS tick, without tearing the identity's lifetime or desyncing the
base `NetworkTransform`. **C1 spikes exactly that** before C2/C3 build on it.

*(The NPC's perception (C5) is a separate concern: it does not "withhold" anything
— the NPC has no client. It is the server asking "does this cone, range, and LOS
reach a player?" and raising an event. Don't conflate the two.)*

---

## Build status — team-lead running log

*Maintained by the lead overseeing M3. Per-commit detail lives in each commit's
**Status** line; this is the cross-cutting view.*

### Progress

| Commit | What | Status | Hash |
|--------|------|--------|------|
| C1 | Per-observer visibility spike + `Vision` visibility-service scaffold + `IVisionAgent` seam | ✅ Done (spike: **PASS**) | — |
| C2 | LOS computation (per-object, fixed tick) + throwaway sight-blockers + config | ✅ Done | 6652d2b |
| C3 | Wire LOS → PurrNet observers — the fog trust boundary goes live | ✅ Done | 17bd5a1 |
| C4 | Minimal NPC body + visible sweeping cone (the tell), hand-placed throwaway | ✅ Done | b824101 |
| C5 | NPC perception: cone+range+LOS can-see → acquired/lost seam + alert cue | ✅ Done | 44febd6 |
| C6 | Acceptance pass — fog/trust gate + "is that nest crewed?" cone read | ✅ Accepted | — |

### Lead fix note — 2026-06-01

C6 exposed two runtime issues before acceptance:
- `RoundStarted` could fire after Unity loaded `Greybox` but before PurrNet had a
  hierarchy for that scene, leaving `Combatant(Clone)` / `NpcBody(Clone)` as plain
  unspawned GameObjects. `RoundController` now unloads stale non-network map scenes,
  reloads through PurrNet, and waits for the map hierarchy before slices spawn.
- Observer withholding remains behind `ConfigKey.FogObserverWithholding`, but the
  M3 test default is now `true` so C6 exercises the trust-boundary path without
  extra manual config. Disable it only when deliberately comparing against the
  pre-fog baseline.

### Lead fix note — 2026-06-02

Host + two-client testing showed no observable fog/perception and players could
walk through the LOS blockers. Follow-up fixes:
- `ConfigDefaults` now enables `FogObserverWithholding` by default, and
  `ServerVisibility` logs each hide/reveal observer change.
- `PlayerMovement` now capsule-casts server-side movement against the LOS obstacle
  layer before applying its kinematic transform write, so the M3 blockers are
  physical cover as well as linecast blockers.
- `NpcPerception` logs acquired/lost targets, and `NpcAlertCue` warns when the
  event fires without an alert clip wired.
- Host-local play is fog-gated the same as connected clients. Server authority
  still computes all visibility, but the host player no longer force-observes
  every target.
- Observer withholding is applied to every `NetworkIdentity` component on the
  tracked target's GameObject. PurrNet evaluates visibility across all identities
  on a transform, so blacklisting only the `IVisionAgent` component logs hide
  events but leaves the object visible through sibling network behaviours.
- Listen-host presentation uses local renderer culling for the host player,
  because PurrNet's despawn packet path deliberately skips `NetworkManager.localPlayer`.
  Remote clients still use observer withholding; the host cannot have true data
  withholding from itself while it is also the server.

### Acceptance note — 2026-06-03

C6 accepted after the follow-up fixes above. Fog, blocker collision, NPC cone
readability, and NPC acquired/lost perception all worked in live host + client
testing. Remote clients use PurrNet observer withholding for the trust boundary;
the listen-host caveat remains presentation-only culling because the host is also
the server.

### Verification discipline (what "Done" means here)

Each engineering commit is **independently re-verified by the lead** before its
Status is recorded (same bar as M1/M2):
- Clean compile — `editor_state.isCompiling` false, then `read_console` shows
  **0** new errors (PurrNet upstream obsolete-API warnings are pre-existing).
- The wall holds — `grep "Garrison.Player"` and `grep "Garrison.Combat"` over
  `Assets/Vision/` are **empty** (Vision reads bodies **only through `Shared`
  seams** + PurrNet, never a sibling slice); `Vision` asmdef references only
  `Garrison.Shared` + `PurrNet.Runtime`.
- No banned patterns — `Find`/`FindObjectOfType`/`Camera.main`/`*.Instance`/
  static singletons/`DontDestroyOnLoad` (explanatory comments allowed).
- **Server-authoritative & trust-safe** — visibility and perception are decided on
  the server; a client never self-reports what it can see. The base
  `NetworkTransform` `_ownerAuth` stays **0** (re-check after any MCP prefab edit,
  per M1's caveat).
- Working tree clean after commit.

### Slice boundary (how Vision stays deletable)

The wall the lead checks each commit: `Vision` references **only `Shared` +
PurrNet** and reaches every body through `Shared` seams (`IVisionAgent`,
`IAssignedPlayer`) + PurrNet's identity/observer API — never the `Player` or
`Combat` slice, never `GetComponent` on a foreign type. Deleting `Vision/` leaves
the player bodies fully functional (just with no fog — everyone observes everyone,
the pre-M3 state) and removes the throwaway NPC. `Player`/`Combat` name no Vision
type.

**Where the NPC lives:** M3's throwaway NPC prefab + its perception/cone components
live in **`Vision/`** (it is purely a vision/LOS vehicle here — no defense
behaviour). **M5 migration is explicit:** M5 moves the NPC *body* into `Defenses/`
and **reuses** the Vision perception component via prefab composition (the M2
`Combatant`-variant pattern — slice composition is the scene/asset's job), driving
behaviour off the **Shared perception seam** (`IPerception`) this milestone
defines. So the reusable logic + its result seam are positioned for M5 now; only
the throwaway placement is discarded.

### New `Shared` seams this milestone adds

*Defined* in Shared so the consuming side reads them without a sideways reference:
- **`IVisionAgent`** (`Shared/Vision/`) — marks a networked entity as a participant
  in fog and exposes its **eye point** (e.g. `Vector3 EyePosition`, the body
  position raised by an eye height). Implemented on the **base** `PlayerBody` (C1)
  and on the NPC (C4). The fog service pairs it with the existing `IAssignedPlayer`
  to resolve a viewer's observer `PlayerID`.
- **`IPerception`** (`Shared/Vision/`) — the NPC perception **result**:
  `event Action<PlayerID> TargetAcquired` / `TargetLost` (+ a current-targets
  read). Implemented by the NPC's perception component (C5); **nobody acts on it in
  M3** — it is the seam **M5's** NPC behaviour subscribes to. Defined in Shared so
  the M5 Defenses slice never references Vision.

---

## C1 — Per-observer visibility spike + visibility-service scaffold + `IVisionAgent`

**Goal:** prove PurrNet can **withhold** a spawned entity's state from a chosen
client at runtime (server-controlled observers), stand up the empty server-side
visibility service the fog half fills, and give bodies the `IVisionAgent` seam the
service enumerates.

**Status — done.** Spike verdict: **PASS (local source verification; runtime
mechanism confirmed in PurrNet `1.19.1`).** The relevant public API is
`NetworkIdentity.BlacklistPlayer(PlayerID)` / `RemoveBlacklistPlayer(PlayerID)`,
followed by `NetworkIdentity.EvaluateVisibility(PlayerID)` (or
`EvaluateVisibility()` for all players). In the pinned package source,
`NetworkIdentity.TryAddObserver` / `TryRemoveObserver` are **internal**, but the
visibility pass in `VisibilityV2.Evaluate(...)` consumes the public
whitelist/blacklist state and mutates observers there, and
`HierarchyV2.OnVisibilityChanged(...)` sends the spawn/despawn packets when a
player gains or loses visibility. `NetworkVisibilityRuleSet` /
`INetworkVisibilityRule` also exist for rule-driven visibility, but C1's verdict
is that **dynamic per-observer withholding is supported** through the public
blacklist/evaluate path, so M3 can proceed without falling back to client-side
renderer hiding. This commit does **not** change observers yet; it only records the
verified API and stands up the server-side registry scaffold C2/C3 will drive.

**Build**
- **Spike first (throwaway — do not keep).** With two bodies spawned two-client,
  make the **server** remove client B's player from body-A's observer set and
  confirm on client B: A's `NetworkTransform` updates **stop arriving** (A
  freezes / despawns on B), re-adding the observer brings it back, and the
  host/server still sees everything. The check that matters: the withheld position
  is **not on B's machine** (data not sent), not merely a hidden renderer.
  **Record the exact PurrNet 1.19.1 API used** and the verdict in this doc.
  - **Pass** → proceed below; C3 uses that API.
  - **Fail** → record the decision before C2 and switch to the best available
    fallback, in **descending trust-safety**: (a) a custom interest-management /
    scoped-spawn approach if PurrNet exposes one; (b) **last resort**, client-side
    renderer culling driven by the server visible-set — *explicitly NOT
    trust-safe*, logged as a known cheat gap, so M3's "client can't peek through
    walls" claim is qualified honestly rather than silently false.
- **`Shared/Vision/IVisionAgent.cs`** — `interface IVisionAgent { Vector3
  EyePosition { get; } }` (eye = body position + a serialized eye height).
  Implement on the **base** `PlayerBody`.
- **`Vision/ServerVisibility.cs : NetworkBehaviour`** (server) on the
  `NetworkedSystems` object (where `RoundController`/`PlayerSpawner` live).
  Scaffold only: it **observes** PurrNet's `HierarchyFactory.onIdentityAdded/
  Removed` (the `LocalPlayerRegistry`/`TryGetModule` pattern) and keeps a live
  list of `IVisionAgent` identities (each with its `IAssignedPlayer`, which is
  `null` for the NPC). **No LOS, no observer changes yet** — just the registry.
  Server-only (`isServer` guard).

**Notes**
- Side lives nowhere here — fog doesn't care about attacker/defender (per-character
  LOS for everyone). Don't pull `IPlayerSide` in.
- The service is a **single server-side authority**, not a per-body component: one
  place computes every viewer's set, so the O(viewers × targets) tick is legible
  and cheap to throttle.
- Reuse `LocalPlayerRegistry`'s proven hierarchy-observation approach, but this is
  a **separate server service** on `NetworkedSystems` (that one is
  local-presentation on Bootstrap; this is networked authority).

**Done when**
- The spike verdict + the exact visibility API are recorded. `IVisionAgent` is on
  the base body; `ServerVisibility` exists, compiles, and logs the live agent set
  as bodies spawn/despawn. No observer changes yet (everyone still sees everyone).
- `grep Garrison.Player`/`grep Garrison.Combat` in `Vision/` is empty;
  `_ownerAuth` still 0.

---

## C2 — LOS computation (per-object, fixed tick) + sight-blockers + config

**Goal:** the server computes, **per viewer, the set of agents currently in line
of sight** — view-distance + a terrain-blocked raycast, recomputed on a fixed
tick — and there is throwaway geometry to break LOS against. Computed and
*exposed*, not yet *enforced*.

**Build**
- In `ServerVisibility`, add the LOS pass on a **fixed-step accumulator** (mirror
  `PlayerMovement.Step`'s pattern, at `LosTickRate`): for each viewer agent V and
  each other agent T —
  1. range cull: `dist(V,T) <= ViewDistance`;
  2. LOS raycast: `Physics.Linecast(V.EyePosition, T.EyePosition, obstacleMask)`
     — clear ⇒ visible, blocked by terrain ⇒ hidden;
  - V always sees **itself**. Result: a per-viewer `HashSet<agent>` held
    server-side. Expose a reusable `HasLineOfSight(from, to)` helper here — **C5's
    NPC can-see reuses it** (one LOS implementation, not two).
- **Throwaway sight-blockers:** add a handful of cube "walls" **with colliders** on
  the `obstacleMask` layer into the `Map` scene, under a named throwaway parent
  (`_LosTestBlockers`, with a `// M3 scaffolding — M8 greybox replaces` note). Just
  enough to peek around. Scene scaffolding, not a Vision asset.
- **Config (inline):**
  - `ConfigKey.ViewDistance` (float) — max sight range; placeholder, tune in C6.
  - `ConfigKey.LosTickRate` (float, Hz) — recompute cadence (e.g. 10). The
    responsiveness-vs-cost dial; the NPC perception tick reuses it.
  - Eye height + `obstacleMask` are **`[SerializeField]`** on `ServerVisibility`
    (author-time wiring, not a between-rounds tunable — so not config, per the
    architecture rule).

**Notes**
- **Pure server read → a set.** No client involvement; the client never reports
  what it sees.
- `Linecast` is the cheapest correct check on the greybox — players have **360°
  awareness** within view distance; only terrain and range gate them (per "players
  have no cone"). **No FOV arc for players** — that's the NPC cone's job (C5).
- Keep the tick **fixed and throttled** (`LosTickRate`), decoupled from frame
  rate, so cost is predictable and C3's observer churn is bounded.

**Done when**
- With blockers in the scene, the per-viewer visible-set is **correct** (verify by
  log/gizmo): an agent behind a blocker or beyond `ViewDistance` is absent from the
  viewer's set; stepping into the open adds it. Recomputes on the `LosTickRate`
  tick. **Nothing withheld yet** — observers unchanged (grep: only C3 touches the
  observer API).

---

## C3 — Wire LOS → PurrNet observers: the fog trust boundary goes live

**Goal:** feed C2's visible-set into C1's per-observer visibility so an
**out-of-LOS entity's state is withheld** from the client — the milestone's
defining property.

**Build**
- Each LOS tick, after C2 computes the sets, reconcile **observer membership**:
  for each target agent T, the set of observing `PlayerID`s = the
  `IAssignedPlayer` of every *player* viewer that currently has T in its
  visible-set (plus T's own player if it has one — you always observe yourself).
- Apply via the C1-spiked API: add observers that gained LOS, remove observers that
  lost it. **Diff against last tick** so only changes hit the network.
- **Edge cases to handle explicitly:**
  - **Own body always observed** by its player (never fog yourself).
  - **Host-local player** is fog-gated the same as connected clients.
  - **The NPC** (no `IAssignedPlayer`) is a **target only** here — players must
    have LOS to receive it ("is that nest crewed?" — you have to get eyes on it).
    It is not a fog *viewer* (no client to withhold from); its "sight" is the C5
    perception cone, a separate path.
  - **Despawn/disconnect** mid-tick: drop the agent cleanly from all sets (C1's
    `onIdentityRemoved` feeds this).
  - **Spectator (M9) note:** leave a comment that M9 re-points observation to a
    followed teammate — don't build it, just don't design it out.

**Notes**
- This is where "hide the renderer" would have been wrong — we change **who
  receives the entity**. Verify directly in C6 (a client log shows the identity
  absent, not present-but-hidden).
- Churn discipline: at `LosTickRate` the diff is small; don't re-send the whole set.
  If pop-in feels harsh, that's a **cadence/hysteresis** tuning question for C6
  (e.g. a short linger before dropping an observer), not a reason to abandon
  withholding.
- `_ownerAuth` stays 0; observer scoping is orthogonal to authority.

**Done when**
- On a client, an enemy body / the NPC **vanishes** when you break LOS (behind a
  blocker / beyond `ViewDistance`) and **reappears** on re-acquire. A client-side
  log/inspection confirms the hidden identity is **absent from the client** (state
  not received), not merely renderer-disabled. The host sees all. `_ownerAuth`
  still 0; walls hold.

---

## C4 — Minimal NPC body + the visible sweeping cone

**Goal:** the first body that *has* a readable sight cone — a hand-placed,
throwaway NPC that participates in fog (you must get LOS to see it) and renders a
**visibly sweeping cone** as its tell. No perception yet (C5), no behaviour (M5).

**Build**
- **`Vision/NpcBody.prefab`** — a networked NPC: a `NetworkIdentity` +
  `NetworkTransform` (`_ownerAuth: 0`, server-authoritative), a simple capsule/
  block `Visual` distinct from players, and a `Muzzle`-style facing it can point.
  Spawn it **server-side** — extend `PlayerSpawner`'s round-start hook *or* add a
  tiny `Vision/NpcSpawner` (server) that drops one NPC at a serialized point in the
  `Map` scene. Hand-placed point = throwaway (M4 owns real placement). Implement
  **`IVisionAgent`** on it so C2/C3 treat it as a fog **target**.
- **Cone visual** — a `Vision/` component rendering the cone arc (a mesh/decal or
  `LineRenderer` fan) on the ground, sized by `NpcConeArc` + `NpcConeRange`. It is
  **world presentation every client renders** (when the NPC is visible to them) —
  so the bluff reads: an uncrewed-looking nest with a sweeping cone vs none.
- **The sweep** — a trivial scripted oscillation of the NPC's facing (server-auth,
  replicated via `NetworkTransform` rotation, same as the player body's facing) so
  the cone visibly sweeps. Mark it clearly as a **placeholder tell-driver, not AI**
  — M5's patrol/scan behaviour replaces it.
- **Config (inline):** `ConfigKey.NpcConeArc` (degrees), `ConfigKey.NpcConeRange`
  (metres). (Light — most NPC tuning is M5.)

**Notes**
- The NPC is a **fog target** (C3) — so on a client you only see the NPC *and* its
  cone once your character has LOS to it. That is the "is that nest crewed?" read:
  you must expose yourself to find out.
- Keep the cone render **legible from the top-down camera** and distinct from the
  C3-era player aim line / M2 tracer — it is a persistent ground arc, not a beam.
- Server owns facing; clients render the cone off the replicated rotation. Don't
  drive the sweep client-side (it would desync the tell from the server-truth the
  perception check uses in C5).

**Done when**
- One NPC spawns at the hand-placed point; its cone renders and **visibly sweeps**;
  the NPC + cone are **subject to fog** (hidden until a client's character has LOS,
  per C3). `_ownerAuth` 0; walls hold. No perception/acquire yet.

---

## C5 — NPC perception: can-see → acquired/lost + the alert cue

**Goal:** the NPC can *see* — a server-side can-see check (cone arc + range + LOS)
that raises **acquired-target / lost-target**, the input M5's behaviour will act
on, plus a minimal alert cue. Nothing acts on the result in M3.

**Build**
- **`Vision/NpcPerception.cs`** (server) on the NPC. On the shared `LosTickRate`
  tick, for each player agent P: target is perceived iff
  1. within `NpcConeRange`, **and**
  2. inside the `NpcConeArc` around the NPC's current (swept) facing, **and**
  3. `ServerVisibility.HasLineOfSight(npc.EyePosition, P.EyePosition)` (reuse C2's
     helper — terrain breaks it).
- Track the perceived set; on add → `TargetAcquired(PlayerID)`, on drop →
  `TargetLost(PlayerID)`. Implement **`Shared/Vision/IPerception`** so M5 reads it.
  **No consumer in M3** (verify by grep — only the seam + the cue read it).
- **Alert cue (stub)** — on `TargetAcquired`, play a minimal positional cue through
  `IAudioBus.Play(AudioChannel.<alert>, clip, npcPos)` (**verify the exact
  `AudioChannel` member**; this is the alert channel's first customer). One short
  clip; the fleshed-out NPC callouts are M5.

**Notes**
- **Server-authoritative** — the NPC's sight is decided server-side off
  server-truth facing + the same LOS as fog; a client can't spoof being unseen.
- Reuse the LOS helper from C2 — **one** linecast implementation. The cone test is
  cheap (dot/angle); do it before the linecast to skip raycasts for targets behind
  the NPC.
- The acquired/lost events are an **announcement** — M3 raises them and nobody
  acts (the milestone's one-way seam). Don't add behaviour, alarms-network, or
  defender notification here; that's M5/M9.

**Done when**
- Walking a player into the NPC's swept cone within range and LOS raises
  `TargetAcquired` and plays the alert cue; breaking the cone with terrain or
  stepping out of arc/range raises `TargetLost`. The result is exposed via
  `IPerception` with **no consumer** wired (grep-verified). Walls hold.

---

## C6 — Acceptance pass: the fog/trust gate + the cone read

**Goal:** render M3's verdict. On the greybox-with-blockers + the NPC: do
**peeking, flanking, and "is that nest crewed?"** become real questions — and does
fog hold as **server-authoritative**, uncheatable through terrain?

**Checklist (from [`m3-fog-and-cones.md`](m3-fog-and-cones.md) "Done when")**
- [x] Peeking and flanking are real questions — you lose sight of an enemy around
      terrain and regain it on the peek; positioning to *see without being seen*
      matters.
- [x] Fog holds **server-authoritative** — a client cannot reveal a hidden
      body/NPC by cheating, because its state was never sent (confirm by
      client-side inspection, not just "the renderer was off").
- [x] **The NPC cone visibly sweeps and you can break its LOS with terrain** —
      stepping behind cover drops you from its perception (`TargetLost` / no alert).
- [x] **"Is that nest crewed?"** reads — the NPC + cone are fogged until you get
      eyes on, so scouting costs exposure.
- [x] Pop-in/out reads acceptably at the chosen `LosTickRate` (tune cadence /
      add linger-hysteresis if it strobes).
- [x] **The wall holds:** `Vision/` references only `Shared` + PurrNet; `grep
      Garrison.Player`/`Garrison.Combat` in `Vision/` is empty; no banned
      patterns; `_ownerAuth` 0; deleting `Vision/` returns the pre-M3
      everyone-sees-everyone behaviour and removes the NPC cleanly.

**The gate (the deliverable)**
- **Pass:** information becomes a resource — caution and positioning beat
  god-view, the cone is a readable tell, and the trust boundary verifiably holds →
  M3 is go; proceed.
- **Fail (valid, useful):** fog strobes/pops, view distance or cone arc/range feel
  wrong, the cone doesn't read, or (critical) the C1 fallback left a cheat gap →
  tune the dials (`ViewDistance`, `LosTickRate`, `NpcConeArc`, `NpcConeRange`,
  hysteresis); escalate only if withholding itself can't be made to hold.

**Record (the decisions closed here)**
- Fog = entity hide/reveal, server-withheld — **held** (or note the C1-fallback
  qualification if the spike failed).
- Per-object on a fixed tick — **held**; final `ViewDistance` / `LosTickRate`.
- NPC kept as a minimal perception stub (no behaviour) — **held**; final
  `NpcConeArc` / `NpcConeRange`; confirm `IPerception` is positioned for M5.
- Per-character fog (no team union) — **held**; `TeamVisionShared` was not added.

**Accepted record — 2026-06-03**
- Fog = entity hide/reveal, server-withheld for remote clients; listen-host uses
  local presentation culling only because it is the server.
- Per-object LOS on a fixed tick held with `ViewDistance = 35` and
  `LosTickRate = 10`.
- NPC stub held with `NpcConeArc = 70` and `NpcConeRange = 8`; `IPerception`
  remains the M5 seam, with no M3 behaviour consumer.
- Per-character fog held; no team union or defender home-field reveal added.

**Done when**
- The checklist passed with real instances on a LAN (host + clients, players
  peeking around the throwaway blockers and the NPC), the decisions are written
  into [`m3-fog-and-cones.md`](m3-fog-and-cones.md), and the playtest log
  ([`../../playtest/log.md`](../../playtest/log.md)) gets its M3 "fog / trust
  boundary + cone" entry — go or no-go.

---

## Dependency order at a glance

```
C1 visibility spike + ServerVisibility scaffold + IVisionAgent
      │   (spike verdict gates the fog half)
      ▼
C2 LOS computation (per-object, fixed tick) + blockers + config + HasLineOfSight helper
      ├─────────────────────────────┐
      ▼                             ▼
C3 wire visible-set → observers   C4 NPC body + sweeping cone (fog target)
   (fog trust boundary live)        │
      │                             ▼
      │                        C5 NPC perception (reuses C2 LOS) → IPerception + alert cue
      │                             │
      └───────────────┬─────────────┘
                      ▼
            C6 acceptance — fog/trust gate + cone read  ◄── renders the verdict
```

- **C1 is the foundation** — the spike proves withholding is possible at all; the
  scaffold + `IVisionAgent` give C2–C5 a surface.
- After C2, the **fog enforcement (C3)** and the **NPC (C4→C5)** are independent and
  can be split between two engineers; both reuse C2's LOS. They rejoin at **C6**.
- The NPC's perception (C5) **reuses C2's `HasLineOfSight`** — one LOS
  implementation, server-side, for both fog and the cone.
- Config keys land **inline**: `ViewDistance` + `LosTickRate` (C2); `NpcConeArc` +
  `NpcConeRange` (C4). The config *system* is M0's; M3 only adds keys.
