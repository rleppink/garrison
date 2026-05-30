# M1 — Implementation plan (commit-by-commit)

> Engineer-facing companion to [`m1-camera-movement.md`](m1-camera-movement.md).
> That doc owns the *why*, the go/no-go gate, and the contracts. This one owns
> the *how* and the *order you type it in*. Conventions are in
> [`../architecture.md`](../architecture.md); camera/vision design in
> [`../../design/concept.md`](../../design/concept.md) ("Camera & vision").

## How to read this

Each section below is **one commit**: a self-contained, compiling, reviewable
step, ordered by dependency — do them top to bottom. Every commit lists:

- **Goal** — the one thing this commit makes true.
- **Build** — files/assets/components to add or move (paths under `Assets/`).
- **Notes** — PurrNet/Unity specifics, design intent, and gotchas.
- **Done when** — how you know the commit is finished.

Two things differ from the M0 plan:

- **No separate config commit.** M0 *built* the config system (`Shared/Config`,
  `IConfig`, `ConfigService`); it already exists. M1 only *adds keys* to it, so
  every dial lands **inline** with the system that reads it — a new `ConfigKey`
  + a `ConfigDefaults` entry, read through `IConfig`. The "config, not
  constants" rule still holds: **no camera or movement tunable is a magic
  number.** (The `mvp-scope.md` config table is the running checklist.)
- **The deliverable is a verdict.** M1's output is the go/no-go feel gate, not
  just code. The dial commits (C4–C5) exist to make the feel *tunable*; the
  acceptance commit (C8) exists to *render the verdict* and is allowed to send
  us back.

PurrNet API names are written to the pinned-version conventions
(`NetworkBehaviour`, `[ServerRpc]`/`[ObserversRpc]`, `SyncVar<T>`,
`NetworkTransform`, the `PlayersManager` module). The netcode is the one place
to **verify signatures against the pinned PurrNet docs, not assume** — version
drift on netcode is the tax `architecture.md` warns about.

### Starting state (verified)
- Unity `6000.4.9f1`, URP, Input System; PurrNet `1.19.1` pinned. M0 is
  **Done** and accepted (`m0-implementation.md` C11).
- The player body is a **throwaway skeleton in `Shared/Player`**
  (`PlayerCapsule`, `PlayerInput`, `PlayerMovement`) — server-authoritative WASD,
  `NetworkTransform` replication, no aim, no camera follow. The M0 scenes use a
  **static camera**; nothing follows the player yet.
- `RoundController` (`Shared/Round`) currently **spawns the capsule directly**
  (references the `PlayerCapsule` type, calls `Assign`). Moving the body into a
  slice (C1) is therefore a spawn-ownership handoff, not just a file move.
- `Vision/` is an **empty slice** (just `Garrison.Vision.asmdef`, referencing
  `Garrison.Shared` + `PurrNet.Runtime`).
- `ConfigKey` holds `PlayerCount`, `MoveSpeed` (4.5). M1 grows this enum.

### What M1 is *not* (from the milestone doc)
- No shoot/accuracy resolution (M2 reads the movement-state seam), no LOS fog or
  sight cones (M3), no planning **high-overview** camera mode (M4). M1 builds
  only the **execution-gameplay** camera.
- The camera is **pure local presentation — not networked.** Aim is **local in
  M1** (it only drives the camera); networking aim/facing is an M2/M3 concern.

---

## Build status — team-lead running log

*Maintained by the lead overseeing M1. Per-commit detail lives in each commit's
**Status** line below; this is the cross-cutting view that doesn't belong to a
single commit.*

### Progress

| Commit | What | Status | Hash |
|--------|------|--------|------|
| C1 | `Player/` slice + spawn-ownership handoff | ✅ Done | `ae9a9c5` |
| C2 | Orthographic follow-camera rig + local-player seam | ✅ Done | `e97912b` |
| C3 | Mouse aim → `IAimSource` seam | ✅ Done | `e1c7d55` |
| C4 | Aim-push camera (core feel-bet) | ✅ Done | `b18e50a` |
| C5 | Return behaviour & coupling dials | ✅ Done | `b185aae` |
| C6 | Movement feel + movement-state seam | ✅ Done | `f11a216` |
| C6b | *(conditional)* PurrDiction prediction | — | — |
| C7 | Footsteps (first audio-bus consumer) | ⚠️ Wired; runtime verification pending | `8945050` |
| C8 | Acceptance pass — go/no-go feel gate | ❌ Live pre-acceptance failed; runtime verification pending | — |

### M1 bugfix after failed C8 preflight

Live two-client pre-acceptance found M1 nonfunctional before any C8 verdict:
camera follow/aim/push and footsteps did not come online because `PlayerBody`
replicated `MovementState` directly as `SyncVar<MovementState>`, and PurrNet
1.19.1 did not have that custom enum registered with its hasher/packer. The
bugfix stores movement state in a supported primitive SyncVar and exposes it back
through the existing `IMovementState` / `MovementState` seam, clamping unknown
wire values to `Idle`.

C7 and C8 remain runtime-verification pending until a fresh two-client round
confirms camera handoff, aim-push, movement state, and positional footsteps.

Doc-only `M1 docs ...` commits carry these Status updates; engineering commits
carry only their own code/asset changes.

### Verification discipline (what "Done" means here)

Each engineering commit is **independently re-verified by the lead** (not taken
on the implementing agent's word) before its Status is recorded:
- Clean compile — `editor_state.isCompiling` false, then `read_console` (errors)
  shows **0** new errors (PurrNet upstream obsolete-API warnings are pre-existing).
- Both compiler walls hold — `grep "Garrison.Player"` over `Assets/Vision/` and
  `Assets/Shared/` is empty.
- No banned patterns introduced — `Find`/`FindObjectOfType`/`Camera.main`/
  `*.Instance`/static singletons/`DontDestroyOnLoad` (comments explaining their
  avoidance are allowed).
- Server-authoritative movement preserved — `PlayerBody.prefab` NetworkTransform
  `_ownerAuth: 0`.
- Working tree clean after commit.

### ⚠️ Not yet runtime-verified

C1–C3 are **"compiles + wired correctly," not "felt good."** The camera-follow,
aim-tracking, and (coming) push behaviours have **not** run in play mode. By
design, all behavioural confirmation folds into the **C8 go/no-go feel gate**,
which needs a live two-instance LAN run. Nothing in the camera/aim chain is
proven to *feel* right until then — that is the whole point of M1.

### Cross-cutting design decisions (reused downstream — C4, M9)

The runtime-spawned, **server-owned** player body cannot be inspector-wired to
persistent Bootstrap scene services, and `Find`/`Camera.main`/`Instance`/static
are banned. Two deliberate decisions resolve this and are **reused by later
commits and M9 shoulder-spectator**, so don't undo them casually:
- **C2 — registry *observes* spawns.** `LocalPlayerRegistry` subscribes to
  PurrNet's manager-level `HierarchyFactory.onIdentityAdded/Removed` (via the
  inspector-wired `NetworkManager`, the same `TryGetModule` pattern
  `RoundController` uses) and sets `Current` to the spawned identity reporting
  `IsLocalView`. Verified against pinned PurrNet 1.19.1 source, including that
  `OnSpawned`/SyncVar application runs **before** `onIdentityAdded` fires, so the
  `assignedPlayer`/`localPlayer` check is valid. The body holds no registry handle.
- **C3 — registry *injects* the camera.** The registry holds the persistent Main
  Camera (`[SerializeField]`) and hands it to the local body via
  `ILocalPlayerView.BindCamera` when it becomes `Current`, so the `Player` slice
  does zero camera lookups for the aim raycast.

### Accepted deviations from the literal plan

- **C1:** `Garrison.Player` asmdef also references `Unity.InputSystem` (an engine
  module `PlayerInput` needs — not a sibling slice, so the coupling wall holds;
  same as M0's `Shared` asmdef).
- **C1:** `PlayerSpawner` lives on the `NetworkedSystems` object (alongside the
  other networked services) rather than the literal `GameSystems` — "the systems
  object" read in context. (`GameSystems` holds the non-networked
  `NetworkManager`/transport/`AudioBus`; `LocalPlayerRegistry` lives there.)
- **C3:** the world-space reticle was **skipped** (optional nice-to-have for the
  feel pass; trivial to add later).

### ⚠️ Carry-forward caveat

In C3, a Unity MCP prefab edit **re-serialized `PlayerBody.prefab`'s
NetworkTransform** YAML (it flipped `_ownerAuth` to `1`; reset to `0` to preserve
server authority). Benign, but if a later commit edits that prefab via MCP,
re-check the NetworkTransform block (`_ownerAuth` must stay `0`).

---

## C1 — `Player/` slice + spawn-ownership handoff

**Status:** Done (`ae9a9c5`). The M0 skeleton moved out of `Shared/Player` into a
deletable `Garrison.Player` slice (`PlayerCapsule` → `PlayerBody`, plus
`PlayerInput`/`PlayerMovement`, GUIDs preserved). `RoundController` no longer
references any player type — it owns scene load/unload and raises a server-side
phase seam (`event Action<Scene> RoundStarted` / `event Action RoundReset`).
The new server `Player/PlayerSpawner` subscribes to that seam and owns
spawning/despawning server-owned bodies + the `PlayerID → body` routing map.
Compile clean (0 errors); `grep Shared/` for player types is clean.
Two accepted deviations from the literal spec: (1) the `Garrison.Player` asmdef
also references `Unity.InputSystem` (a Unity engine module `PlayerInput` needs —
not a sibling slice, so the coupling wall holds; same as M0's `Shared`); (2)
`PlayerSpawner` lives on the `NetworkedSystems` object (where the networked
services `RoundController`/`ConfigService` live) rather than the non-networked
`GameSystems` object — "the systems object" read in context.

**Goal:** the player body lives in its own deletable slice, and *the Player
slice owns spawning it* — `Shared` no longer references any player type.

**Build**
- Create `Player/Garrison.Player.asmdef`, referencing **`Garrison.Shared` and
  `PurrNet.Runtime` only** (the same wall every slice obeys).
- **Move** the M0 skeleton out of `Shared/Player` into `Player/`:
  `PlayerCapsule` → `PlayerBody`, plus `PlayerInput`, `PlayerMovement`. Namespace
  `Garrison.Player`. Behaviour stays **identical** for now — this commit is a
  relocation + ownership move, not a feel change.
- Add a **round-phase seam in `Shared`** so the Player slice can react to round
  start/reset without `Shared` depending on it. Either an event on the existing
  `RoundController` (`event Action RoundStarted/RoundReset`, server-side) or a
  small `Shared/Round/IRoundPhases` interface. `RoundController` stops
  instantiating the body and **stops referencing the player type**; it only
  raises phase transitions and still owns `SpawnPoints`.
- Add `Player/PlayerSpawner.cs : NetworkBehaviour` (server) on `GameSystems`.
  Subscribes to the round-phase seam: on start, spawn one **server-owned**
  `PlayerBody` per connected player at a free `SpawnPoint` **into the `Map`
  scene**, assign its `PlayerID`, and map `PlayerID → body` for input routing; on
  reset, despawn all bodies. This is the spawn logic lifted out of
  `RoundController`, now owned by the slice that owns the body.
- Update [`../architecture.md`](../architecture.md): add `Player/` to the
  canonical slice list (the body is genuinely cross-cutting — Vision, Combat,
  Loot all consume it). Note its responsibility = the player body, input,
  locomotion, and the movement/aim seams.

**Notes**
- The M0 header on `PlayerCapsule` said "M1 relocates/replaces it" — this is that
  relocation. `Shared/Player` should be **gone** after this commit.
- Keep `PlayerInput`'s owner→server `[ServerRpc]` pattern and `PlayerMovement`'s
  server-apply split exactly as M0 left them; later commits build on top.
- Don't reach for prediction here. PurrDiction is the explicit open decision,
  resolved on feel after C8 (see C6 notes).

**Done when**
- Project compiles; `Shared` references no player type (grep `Shared/` for
  `PlayerBody`/`PlayerCapsule`/`PlayerMovement` is clean).
- Start → bodies spawn and move exactly as in M0; Reset despawns them cleanly.
- The `Player/` folder **deletes clean** (only `Player/` breaks); `Garrison.Player`
  references only `Garrison.Shared` + `PurrNet.Runtime`.

---

## C2 — Orthographic camera rig that follows the local body

**Status:** Done (`e97912b`). `Vision/CameraRig` (local `MonoBehaviour`, not
networked) drives the persistent Bootstrap Main Camera: orthographic, fixed
top-down/iso angle (serialized `viewDirection`/`distance` dials), zoom via
`ConfigKey.CameraZoom` (default 10, re-applied live on `IConfig.Changed`). The
**local-player handoff seam** lives in `Shared/Player`: `ILocalPlayerView`
(`ViewTarget` + `IsLocalView`), `ILocalPlayerRegistry` (`Current` +
`CurrentChanged`), and `LocalPlayerRegistry` — a persistent Bootstrap service
that **observes PurrNet's `HierarchyFactory.onIdentityAdded/Removed`** (reached
via inspector-wired `NetworkManager`, the same `TryGetModule` pattern
`RoundController` uses) and sets `Current` when a spawned identity reports
`IsLocalView`. `PlayerBody` implements `ILocalPlayerView`. No `Find`/`Instance`/
static; the body never holds a registry handle. Both walls hold (`Vision`↛
`Player`, `Shared`↛`Player`). Compile clean; **not** runtime-verified yet
(follow behaviour needs a live two-instance run — folded into the C8 gate).
This is the seam C3 (aim) and M9 (spectator) reuse.

**Goal:** a top-down/iso orthographic camera follows the **local** player and
keeps them centered — the trivial baseline of the *"character never leaves the
screen"* invariant, before any push exists.

**Build**
- `Vision/CameraRig.cs` (local, **not** a `NetworkBehaviour`) — drives the scene
  camera in `LateUpdate` to frame a target transform from a fixed top-down/iso
  angle. **Orthographic** projection; zoom = `Camera.orthographicSize`, read
  through `IConfig`.
- A **local-player handoff seam in `Shared`** so the rig finds the runtime-spawned
  local body without `GetComponent`/`Find` (both banned). e.g.
  `Shared/Player/ILocalPlayerView` + a tiny `Shared` registry on `GameSystems`:
  when a body spawns and `AssignedPlayer == localPlayer`, it registers its
  transform; the rig reads the current local target from the registry. (This same
  seam is what M9 shoulder-spectator re-points to switch the followed body.)
- Add `ConfigKey.CameraZoom`; seed `ConfigDefaults` with a zoom tuned so
  off-screen props (future DF towers) aren't normally visible (per the concept
  doc's central framing constraint).

**Notes**
- `Vision` reads the body **through the `Shared` seam**, never the `Player`
  slice directly — the two slices stay mutually ignorant (the compiler wall
  proves it).
- Camera is **per-client local presentation**; do not network it. The body it
  follows is the already-replicated `NetworkTransform`, so the view is correct on
  every client for free.
- The orthographic-iso choice is settled (greybox is flat; clean tactical read).
  Keep the angle/zoom in `[SerializeField]`/config so the feel pass can sweep it.

**Done when**
- On round start, each client's camera snaps to and follows *their own* body at
  the configured zoom; the body stays centered as it moves; other players are
  framed relatively. Changing `CameraZoom` host-side re-frames live.

---

## C3 — Mouse aim → aim vector seam

**Status:** Done (`e1c7d55`). New `Shared/Player/IAimSource` (`AimPoint`,
`AimDirection` XZ-normalized, `AimDistance`), exposed off the local view via
`ILocalPlayerView.Aim`. `Player/PlayerAim` (local `MonoBehaviour`) computes it
each frame **only for the local body** (`IsLocalView` gate): `Mouse.current`
→ `ScreenPointToRay` → intersection with a math `Plane` at the body's height (no
physics raycast). The gameplay camera reaches the runtime body via the **C2
registry**: `LocalPlayerRegistry` holds the Main Camera (`[SerializeField]`) and
injects it through `ILocalPlayerView.BindCamera` when the body becomes `Current`
— no `Find`/`Camera.main`/`Instance`/static. **Local only** — no aim `ServerRpc`
(nothing server-side reads aim until M2). Reticle skipped (optional; trivial to
add for the feel pass). Server-authoritative movement preserved (NetworkTransform
`_ownerAuth: 0`). Walls hold; compile clean; **not** runtime-verified (cursor
tracking folds into the C8 gate).

**Goal:** the local body exposes an **aim vector** derived from the mouse cursor
— the input the camera-push (C4) and, later, accuracy (M2) both consume.

**Build**
- In `Player/`, read `Mouse.current.position` (Input System) and raycast to the
  **ground plane** to get a world aim point; aim vector = `(aimPoint − bodyPos)`
  on the XZ plane. This runs **only for the local/owner body**.
- Expose it via a **`Shared` seam** `Shared/Player/IAimSource` (e.g.
  `Vector3 AimPoint`, `Vector2 AimDirection`, `float AimDistance`), implemented
  on `PlayerBody`, surfaced through the local-player registry from C2. Symmetric
  with the movement-state seam (C6): aim is a *property of the player*, exposed
  for other slices to read.
- Optional: a minimal world-space reticle at the aim point (local-only; helps the
  feel pass read where "far" is). Keep it trivially deletable.

**Notes**
- **Local only in M1.** Aim drives the camera (next commit) and nothing else.
  The server does not need aim until M2 (accuracy) / M3 (facing) — don't add an
  aim `[ServerRpc]` yet; it's wasted bandwidth until something server-side reads
  it.
- Ground-plane raycast (a flat math plane at `y = body.y`) is enough on the
  greybox — no need for physics raycasts against geometry that doesn't exist yet.

**Done when**
- The local body's `IAimSource` reports a stable aim point that tracks the cursor
  across the ground; aim distance grows as the cursor moves away from the
  character. No effect on the camera yet (that's C4).

---

## C4 — Aim-push camera (the core feel-bet)

**Status:** Done (`b18e50a`). `Vision/CameraRig` now reads
`ILocalPlayerView.Aim` through the existing Shared local-player seam and offsets
the orthographic frame target along the local aim vector, clamped after shaping
so the body remains inside a configurable safe viewport inset even at extreme
push extents. Added config defaults for `CameraPushExtent` and
`CameraPushShape`, plus shape/safety parameters (`CameraPushHorizontalScale`,
`CameraPushForwardScale`, `CameraPushBackwardScale`, `CameraSafeViewportInset`)
so circle, ellipse, and asymmetric push are live-tunable through `IConfig`.
Compile clean (0 errors); `CameraRig` validates clean; walls and banned-pattern
greps are clean; server-authoritative movement preserved (`_ownerAuth: 0`).
Still not runtime-feel-verified — C8 owns the live two-instance verdict.

**Goal:** aiming farther pushes the camera toward the aim — *see ahead, go blind
behind* — under the hard invariant that **the character never leaves the screen.**

**Build**
- In `CameraRig`, offset the camera's framing target from the body **along the
  aim vector** (read via `IAimSource`), scaled by aim distance up to a max push.
- Enforce the invariant **after** computing the desired offset: clamp the push so
  the body's screen position stays inside a safe inset of the viewport (compute
  from `orthographicSize` + aspect; the body is never allowed past the inset
  edge). This clamp is **non-negotiable** and sits below every dial.
- Land the first two push dials as config (`IConfig`):
  - `ConfigKey.CameraPushExtent` — how far max aim pushes the frame.
  - `ConfigKey.CameraPushShape` — radius / ellipse / asymmetric (encode the mode
    as an int in `ConfigValue`, with the shape params as further keys if needed).

**Notes**
- This is the commit the whole milestone is shaped around — "the part that's easy
  to feel awful." Get it *adjustable*, not perfect; C5 adds release behaviour and
  C8 renders the verdict.
- Coupling is **push-tied-to-aim** by default (one input, three costs). The
  "push tied to aim vs separate input" dial is part of C5's coupling key — here,
  wire it tied-to-aim.
- The clamp protects the invariant even at max `CameraPushExtent`; a dial can
  never push the character off-screen. Test by cranking the extent to an absurd
  value — the body must still stay on screen.

**Done when**
- Moving the cursor far pushes the view in that direction and reveals ahead while
  the area behind the character scrolls off; the character is always visible. The
  push extent/shape dials visibly change the feel live.

---

## C5 — Return behaviour & coupling dials (the tuning surface)

**Status:** Done (`b185aae`). `Vision/CameraRig` now has config-backed
`CameraReturn` (snap vs lazy-follow), `CameraReturnSpeed`, and
`CameraPushCoupling`, completing the M1 camera tuning surface alongside zoom and
the C4 push extent/shape keys. Lazy return uses frame-rate-independent
`Vector3.SmoothDamp`; snap mode updates immediately; the C4 safe-viewport clamp
still applies after smoothing every frame. The separate-input coupling mode is
exposed as a config value, but intentionally still uses aim in M1 because no
separate push input exists yet. Compile clean (0 errors); `CameraRig` validates
clean; walls and banned-pattern greps are clean; server-authoritative movement
preserved (`_ownerAuth: 0`). Still not runtime-feel-verified — C8 owns the live
two-instance verdict.

**Goal:** complete the camera's config surface — how it *returns* when aim
relaxes, and the coupling choice — so the feel is fully tunable for the gate.

**Build**
- Return behaviour on aim release/relax, as config `ConfigKey.CameraReturn`:
  **snap** vs **lazy-follow**, with a lazy-follow smoothing time
  (`ConfigKey.CameraReturnSpeed` or a damping param). Implement lazy-follow as a
  critically-damped smooth toward the target so it can't overshoot/oscillate
  (nausea is an explicit fail mode).
- `ConfigKey.CameraPushCoupling` — push tied to aim (default) vs a separate input
  — exposed even if the default is the only one playtested first.
- Make sure **every** camera dial named in the milestone doc is now a config key:
  push coupling, snap-vs-lazy-follow (+ speed), push radius/shape, zoom. No
  camera tunable remains a literal.

**Notes**
- Smoothing must be **frame-rate independent** (`Time.deltaTime`-based damping,
  e.g. `Vector3.SmoothDamp` or an exponential decay) — a feel that changes with
  FPS will lie to the playtest.
- Keep the invariant clamp (C4) applied **after** smoothing, every frame, so a
  lazy return can never momentarily strand the character off-screen.

**Done when**
- Releasing aim returns the view per the configured mode; lazy-follow reads as
  smooth, never nauseating or oscillating; toggling each dial host-side changes
  the feel live with no recompile.

---

## C6 — Movement feel + the movement-state seam

**Status:** Done (`f11a216`). `PlayerInput` now includes Left Shift sprint in
the existing client→server input RPC, still validated with `RPCInfo.sender`
against `AssignedPlayer`. `PlayerMovement` server-applies walk vs sprint speeds
from config (`MoveSpeed` remains walk; new `SprintSpeed` default 5.8) and updates
a server-auth replicated movement state on `PlayerBody`. Added the Shared seam
`MovementState` / `IMovementState`, surfaced through `ILocalPlayerView.Movement`
without `Shared` referencing the Player slice. Verified seam-only: movement
state has no M1 consumer beyond Shared definitions/plumbing and Player
implementation. Compile clean (0 errors); changed Player scripts validate clean;
walls and banned-pattern greps are clean; server-authoritative movement
preserved (`_ownerAuth: 0`). Still not runtime-feel-verified — C8 owns the live
two-instance verdict.

**Goal:** movement is tuned toward *tactical, not rush-B* with **walk + sprint**,
and the character exposes its **movement state** — the seam M2 will read for
movement-reduces-accuracy, shipped here even though nothing reads it yet.

**Build**
- Walk/sprint speeds as config: keep `ConfigKey.MoveSpeed` as the **walk** speed
  and add `ConfigKey.SprintSpeed`. A sprint key (Left Shift) is part of
  `PlayerInput`'s intent → forwarded to the server alongside the move vector
  (extend the existing `[ServerRpc]`; sprint is server-applied, same authority
  split as movement).
- Expose the movement state via a **`Shared` seam** `Shared/Player/IMovementState`
  (e.g. an enum `Idle | Walking | Sprinting`, or a continuous normalized speed),
  computed **server-side** from applied velocity and surfaced through the
  local-player registry / on the body so M2 can read it. **Nothing reads it in
  M1** — that's the seam, per the milestone doc.
- Tune walk/sprint toward caution-dominates-aggression (the concept's anti-rush-B
  lever); leave the numbers in config for the feel pass.

**Notes**
- Mostly **independent of the camera** (C2–C5): it needs only the `Player` slice
  (C1) and can be built in parallel by a second engineer.
- **PurrDiction (open decision):** stay server-authoritative here. Only if C8's
  feel pass shows authoritative walk/sprint feels bad do we add local-movement
  prediction — that's the **conditional commit below**, not a default.
- Sprinting feeds the M2 triple-tradeoff (inaccurate + exposed + blind-behind);
  keeping state server-derived means accuracy (M2) consumes a server-truth value,
  not a client claim.

**Done when**
- Players walk and sprint (Shift) at distinct configured speeds, server-applied
  and replicated; `IMovementState` reports idle/walking/sprinting correctly; no
  consumer is wired (verified by grep — only the seam exists).

---

## C6b — *(conditional)* PurrDiction local-movement prediction

**Status:** Build **only if** C8's feel pass judges authoritative movement bad.
This resolves the milestone's open decision.

**Goal:** local movement predicts client-side and reconciles, removing the
round-trip latency on your own character — *if and only if* the feel demands it.

**Build**
- Layer PurrDiction onto `PlayerMovement` per the `purrdiction-csp` rules:
  deterministic `Simulate`, an `IPredictedData` movement state, reconciliation.
  Keep the server authoritative; prediction is a feel layer, not a new authority.

**Notes**
- Follow the `purrdiction-csp` skill (determinism, side-effect dispatch,
  reconciliation) — this is the one place desync bugs hide.
- If C8 says authoritative is fine, **skip this and record the decision** in the
  milestone doc — "we didn't need prediction" is a valid, useful outcome.

**Done when**
- Local character responds with no perceptible input latency and no desync/
  rubber-banding under induced lag — *or* the commit is intentionally skipped and
  the open decision is closed in writing.

---

## C7 — Footsteps: the first real audio-bus consumer

**Status:** Front-work and editor wiring done (`98a8c70`, plus `7b67311` and
`8945050`); runtime verification still pending. Added
`PlayerFootstepEmitter` on `PlayerBody.prefab`, driven by the C6 replicated
`IMovementState` seam with separate walk/sprint cadences and routed through
`IAudioBus.Play(AudioChannel.Footsteps, clip, worldPos)`. Added a Shared
`IAudioBusSink` / `AudioBusBinder` injection seam so spawned network identities
can receive the persistent Bootstrap `AudioBus` without `Find`/singletons/static
lookups. The emitter now randomizes across the ten imported `thump001-010`
clips and applies small per-step volume/pitch variance through the audio bus.
No `AudioSource.PlayClipAtPoint`; compile clean (0 errors); changed scripts
validate clean; walls and banned-pattern greps are clean; server-authoritative
movement preserved (`_ownerAuth: 0`).

Editor wiring is now done: `AudioBusBinder` is on `GameSystems`,
`GarrisonAudio.mixer` has a `Footsteps` group, and `AudioBus.routes` maps
`AudioChannel.Footsteps` to that group. Pending before C7 can be marked Done:
runtime-verify positional playback in a live run.

**Goal:** movement makes noise — directional, distance-attenuated footsteps
played **into the M0 audio bus**, the bus's first real customer.

**Build**
- A footstep emitter in `Player/` driven by `IMovementState` (cadence scales
  walk vs sprint), calling `IAudioBus.Play(AudioChannel.Footsteps, clip,
  worldPos)` — the channel the M0 mixer already defines. No ad-hoc `AudioSource`.
- Wire a placeholder footstep clip into the `Footsteps` mixer group (the bus has
  been silent since M0; this is where it first sounds).
- Footsteps fire for **every** body a client can perceive (3D positional), so you
  hear teammates/enemies move — the off-screen world arriving through the
  speakers, per the concept's "audio is a primary sense."

**Notes**
- **Local presentation, driven by replicated state** — each client plays
  footsteps from each body's movement state at its `NetworkTransform` position.
  Networked/authoritative positional audio is the **M9** mix pass; M1 only proves
  movement feeds the bus positionally.
- Keep it 3D and distance-attenuated from the start (the bus is already 3D) — the
  concept flags audio can't be retrofitted as an afterthought.

**Done when**
- Walking/sprinting produces positional footsteps through the `Footsteps` mixer
  group; a teammate moving off to one side is audibly directional and fades with
  distance; no `AudioSource.PlayClipAtPoint` anywhere.

---

## C8 — Acceptance pass: the go/no-go feel gate

**Status:** Live pre-acceptance **failed before a verdict**: spawned observer
sync threw `InvalidOperationException: [Hasher] Type
'Garrison.Shared.Player.MovementState' is not registered` from
`PlayerBody`'s movement-state SyncVar, so the camera/audio local-presentation
handoff never had a working local body to follow or bind. Do **not** mark C8
accepted from that run. Retest after the M1 bugfix commit.

**Goal:** render M1's verdict. Two people move and aim around the greybox; we
decide **elegant, or fighting the controls?**

**Checklist (from [`m1-camera-movement.md`](m1-camera-movement.md))**
- [ ] Two people move and aim on the greybox; aiming far pushes the camera and
      the **character never leaves the screen** (verified even at extreme
      `CameraPushExtent`).
- [ ] The **blind-behind tradeoff is felt** — peeking ahead visibly costs
      awareness behind.
- [ ] Release/return reads as **controllable, not nauseating** (sweep snap vs
      lazy-follow + return speed).
- [ ] Walk/sprint feels **tactical, not rush-B**; `IMovementState` is live and
      correct (the M2 hook), with **no consumer** wired.
- [ ] Footsteps play positionally through the bus.
- [ ] **Deletes clean:** `Player/` and `Vision/` each delete without breaking the
      other; neither references the other (only `Shared` seams between them);
      `Shared` references no slice; no `*.Instance` / `DontDestroyOnLoad`.

**The gate (the actual deliverable)**
- **Pass:** aiming far to peek or cover reads as a deliberate, controllable
  tradeoff → M1 is go; proceed to M2.
- **Fail:** fighting the camera, nausea on release, or losing track of your own
  character → a **valid, useful** result. Either tune the C4–C5 dials and
  re-judge, or escalate: build **C6b** (prediction) if *movement* is the culprit,
  or send the camera back to rethink before any game is built on it.

**Local verification**
- `Unity -batchmode -quit -projectPath ...` exits `0` after import/compile.
- `Player/` and `Vision/` asmdefs reference only `Garrison.Shared` +
  `PurrNet.Runtime`; grep confirms no `Player`↔`Vision` type references.

**Done when**
- The above pass with two real instances on a LAN, the camera-dial verdict and
  the PurrDiction open decision are **recorded in the milestone doc**, and the
  playtest log (`../../playtest/log.md`) gets its M1 "feel gate" entry — go or
  no-go.

---

## Dependency order at a glance

```
C1 Player slice + spawn handoff
      │
      ├─► C2 camera rig (follow) ─► C3 aim seam ─► C4 aim-push ─► C5 return/coupling dials
      │                                                                   │
      └─► C6 movement feel + state seam ─► C7 footsteps                   │
                              │                                           │
                              └───────────────► C8 acceptance (go/no-go) ◄┘
                                                       │
                                       C6b PurrDiction (conditional, only if C8 fails on movement)
```

- **C1 is the foundation** — the slice + spawn-ownership move everything else
  hangs off.
- The **camera chain (C2→C5)** and **movement chain (C6→C7)** are independent
  after C1 and can be split between two engineers; they rejoin at **C8**.
- **C6b** is conditional: it exists only if the gate fails *on movement feel*.
- Config keys land **inline** (zoom in C2; push extent/shape in C4; return +
  coupling in C5; sprint in C6) — the config *system* is M0's; M1 only adds keys.
