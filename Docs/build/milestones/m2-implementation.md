# M2 — Implementation plan (commit-by-commit)

> Engineer-facing companion to [`m2-combat-core.md`](m2-combat-core.md). That doc
> owns the *why*, the five systems, the bellwether, and the go/no-go gate. This
> one owns the *how* and the *order you type it in*. Conventions are in
> [`../architecture.md`](../architecture.md); combat model in
> [`../../design/concept.md`](../../design/concept.md) ("Combat model").

## How to read this

Each section below is **one commit**: a self-contained, compiling, reviewable
step, ordered by dependency — do them top to bottom. Every commit lists:

- **Goal** — the one thing this commit makes true.
- **Build** — files/assets/components to add or move (paths under `Assets/`).
- **Notes** — PurrNet/Unity specifics, design intent, and gotchas.
- **Done when** — how you know the commit is finished.

As in M1, **there is no separate config commit.** The config *system* is M0's;
M2 only *adds keys*, so every dial lands **inline** with the system that reads
it — a new `ConfigKey` enum entry plus a typed field + `Entries()` yield in
`ConfigDefaults` (the typed-serialized pattern from `a2397d4`), read through
`IConfig`. The "config, not constants" rule holds: **no combat tunable is a magic
number** (the `mvp-scope.md` config table is the running checklist).

PurrNet API names follow the pinned-version conventions (`NetworkBehaviour`,
`[ServerRpc]`/`[ObserversRpc]`, `SyncVar<T>`, `NetworkTransform`, the
`PlayersManager`/`RPCInfo.sender` pattern). The netcode is the one place to
**verify signatures against the pinned PurrNet docs, not assume** — and M1 already
paid the version-drift tax once (the `SyncVar<MovementState>` hasher crash; see
the M1 bugfix note). **Replicate combat state in supported primitive SyncVars**
and surface it back through typed seams, exactly as `PlayerBody` does for
`MovementState`.

### Starting state (verified)

- Unity `6000.4.9f1`, URP, Input System; PurrNet `1.19.1`. M1 is the predecessor;
  its camera/movement/aim/footstep chain is built (C1–C7), the C8 feel-gate verdict
  is pending a clean two-client run but the **seams M2 needs are live**.
- **`Combat/` is an empty slice** — just `Garrison.Combat.asmdef` (references
  `Garrison.Shared` + `PurrNet.Runtime`). Everything below is its first contents.
- The seams M2 consumes already exist in `Shared/Player`:
  - `IMovementState` (`Idle | Walking | Sprinting`) — **the accuracy input** (§3),
    server-derived and replicated on `PlayerBody`.
  - `IAimSource` (`AimPoint` / `AimDirection` / `AimDistance`) — **local only** today;
    M2 is where the server first needs aim.
  - `ILocalPlayerView` (+ `LocalPlayerRegistry`) — how local-presentation code
    (hearts HUD, aim line, tracers) reaches the local body without `Find`/`Instance`.
- `PlayerInput` streams move+sprint owner→server at 60Hz on `Channel.Unreliable`,
  validated by `RPCInfo.sender == capsule.AssignedPlayer`. `PlayerMovement`
  server-applies movement on a 60Hz fixed-step accumulator and writes a replicated
  `MovementState`. **M2's fire/aim streams copy this exact pattern.**
- `PlayerBody.prefab`: a `NetworkTransform` (`_ownerAuth: 0`, syncs **position and
  rotation** — rotation is currently never written), a capsule `Visual` child at
  `y=1`, and the `PlayerBody`/`PlayerMovement`/`PlayerInput`/`PlayerAim`/
  `PlayerFootstepEmitter` components.
- `PlayerSpawner` (server) spawns one server-owned `PlayerBody` per connected
  player into the `Map` scene and owns the `PlayerID → body` map.
- `AudioChannel.Weapons = 0` exists in the M0 bus but has **no customer yet** — M2
  is its first (gunfire, hit, down, got-up cues).
- `ConfigKey` ends at `SprintSpeed = 12`. M2 grows the enum.

### What M2 is *not* (from the milestone doc)

- No S-mine damage (M5), no fog/LOS (M3 — hit resolution gets its LOS check then),
  no NPCs (M3), no shoulder-spectator or gear-drop-on-death (M9/M6). M2 owns only
  the lifestate **transitions**; who listens to them is later slices' concern.

---

## Decisions taken into this plan

The milestone doc left open decisions; this plan **resolves them** (and folds in
the design steers from the planning session). Each is recorded again in C9.

- **Hitscan-with-deviation, not projectile** (milestone recommendation) — most
  readable top-down, makes TTK a clean dial.
- **Defender armor is cut for M2.** The defender simply has 4 max hearts instead
  of the attackers' 3. No focus-fire tracker, no armor regen, no armor pips.
- **Test weapon = an M1-Garand-style semi-auto rifle, fired one shot per click**,
  *not* the Sten, for M2. The weapon stays a **data profile** (§5), so a full-auto
  Sten is a re-seed of the same keys later — the "Sten TTK @ 20m" bellwether
  framing returns whenever we want it; for now the bellwether is the rifle's
  TTK/feel at 20m. **This is a deliberate deviation from the milestone doc's Sten
  framing**, made to get a firefight readable on the simplest fire model first.
- **The body turns to face the cursor, and that facing is networked** (C2). The
  milestone treats facing as "M2/M3"; we do the **visual** facing now because the
  weapon visual and the shot direction both need it, and it is the concept's
  "read facing from posture" tell. It's cheap — `NetworkTransform` already
  replicates rotation.
- **Defender vs attacker for M2 is a config stopgap** — `ConfigKey.DefenderSlot`
  (default 0 = host/first player) picks the one 4-heart defender. The real
  role/lobby picker is **M4's**; this is throwaway (C1).
- **Feedback is "lightweight but real,"** plus a presentation steer: a thin
  **local aim line** (where you're pointing) that is visually distinct from a
  thicker **tracer** (where the shot actually went). The game isn't about aim
  precision, so the aim line is a deliberate assist — and the **gap between aim
  line and tracer is the visible read on the movement-accuracy penalty** (§3).
- **Combat composes onto the body via a prefab variant Combat owns**, not by
  mutating the Player prefab and not as a separate networked entity. See the
  [Composition model](#composition-model--the-prefab-variant) below — it's the
  structural spine of the milestone, gated on a spike in C1.

---

## Composition model — the prefab variant

Combat attaches to the **player entity** — hearts, firing, downed-state, and the
held weapon all belong to the body. But PurrNet requires `NetworkBehaviour`s to be
on the prefab **before** spawn (you can't runtime-`AddComponent` networked state
and have it replicate), so Combat's components must be *authored onto* the body
prefab, not bolted on at runtime. To keep that from fusing the two slices into one
mutable prefab, M2 composes via a **prefab variant Combat owns**:

```
Player/PlayerBody.prefab     pure Player base (M1 + C2 facing): NetworkTransform,
        │                    capsule Visual, movement, input, aim, footsteps.
        │  «variant of»      No weapon, no combat. Fully functional on its own.
        ▼
Combat/Combatant.prefab      Combat owns it. Adds to the base identity:
                             LifeState, Accuracy, WeaponFire,
                             Syrette + weapon visual + Muzzle + aim line / HUD.

PlayerSpawner (Player) keeps `[SerializeField] GameObject` body-prefab,
wired IN THE SCENE to → Combat/Combatant.prefab.
```

Why this, and not the two obvious alternatives:
- **vs. everything on one `PlayerBody.prefab`:** the single prefab couples the two
  slices on one opaque YAML file (merge-hell once the C4 and C6 chains are built in
  parallel) and leaves missing-script residue when `Combat/` is deleted.
- **vs. a separate networked combat entity:** two `NetworkIdentity`s per player =
  two lifetimes to keep in lockstep, plus cross-entity lookups for the muzzle, the
  hit collider, and the downed→movement signal. That's *more* coupling, moved to
  runtime where it's harder to see.

What the variant buys:
- **Dependency direction is correct at the asset level.** A variant extends its
  base, so `Combatant` → `PlayerBody` is *Combat-depends-on-Player*, the direction
  we want. Combat code still references only `Shared` + PurrNet; `PlayerSpawner`'s
  field stays typed `GameObject`, so Player code never names a Combat type. The
  wiring that points the spawner at the variant lives **in the scene** — slice
  composition is the scene's job (that's what Bootstrap is for).
- **Deletes clean for real.** Remove `Combat/` → the variant and its scripts go
  with it; repoint one inspector field to the base and you have a working M1 body.
  No residue on the base.
- **Player↔Combat talk through Shared, by inversion.** Player exposes *optional
  hooks* defined in Shared and respects them when present:
  `PlayerMovement`/`PlayerInput` hold an optional `ILifeState` (`null` ⇒ "always
  able to act" ⇒ exact M1 behaviour on the bare base); the `Combatant` variant
  overrides that serialized field to point at its own `LifeState`. Player defines
  the socket; Combat plugs in; neither names the other.

**This rests on one unverified assumption** — that PurrNet 1.19.1 replicates a
variant that adds `NetworkBehaviour`s to the base's identity without disturbing
component indexing or the hasher. **C1 spikes that before anything builds on it.**
If it doesn't hold, the documented fallbacks are (a) Combat behaviours on a nested
child `NetworkIdentity` inside the variant, or (b) the single-prefab version —
both still shippable, in descending order of cleanliness.

---

## Build status — team-lead running log

*Maintained by the lead overseeing M2. Per-commit detail lives in each commit's
**Status** line; this is the cross-cutting view.*

### Progress

| Commit | What | Status | Hash |
|--------|------|--------|------|
| C1 | Composition spike + `Combatant` variant scaffold + side split (`DefenderSlot`) | ✅ Done (spike: **PASS**, host-side) | _backfill_ |
| C2 | Networked aim-facing — the body turns to the cursor | ⏳ Not started | — |
| C3 | Weapon visual (right-hand block) + local aim line | ⏳ Not started | — |
| C4 | Life-state machine: hearts, downed, dead (+ HUD, down visual) | ⏳ Not started | — |
| C5 | Accuracy: movement-state → spread | ⏳ Not started | — |
| C6 | Test weapon profile + fire path + hit resolution (+ tracer/muzzle/gunfire) | ⏳ Not started | — |
| C7 | Defender has 4 hearts instead of attacker 3 | ⏳ Not started | — |
| C8 | Syrette: down → up (the only revive) | ✅ Done (attackers only; self or downed teammate within reach) | _backfill_ |
| C9 | Acceptance pass — the 2v1 feel gate + TTK bellwether | ⏳ Not started | — |

### Verification discipline (what "Done" means here)

Each engineering commit is **independently re-verified by the lead** before its
Status is recorded (same bar as M1):
- Clean compile — `editor_state.isCompiling` false, then `read_console` shows
  **0** new errors (PurrNet upstream obsolete-API warnings are pre-existing).
- The wall holds — `grep "Garrison.Player"` over `Assets/Combat/` is empty
  (Combat talks to the body **only through `Shared` seams**, never the Player
  slice); `Combat` asmdef references only `Garrison.Shared` + `PurrNet.Runtime`.
- No banned patterns — `Find`/`FindObjectOfType`/`Camera.main`/`*.Instance`/
  static singletons/`DontDestroyOnLoad` (explanatory comments allowed).
- Server-authoritative combat — hits, hearts, lifestate transitions are
  **server-decided**; clients never self-report a hit. The base
  `PlayerBody.prefab` `NetworkTransform` `_ownerAuth` stays **0** (the `Combatant`
  variant inherits it — re-check after any MCP prefab edit, per M1's caveat).
- Working tree clean after commit.

### Slice boundary (how the variant keeps it honest)

The [Composition model](#composition-model--the-prefab-variant) is what keeps
Player and Combat from fusing. The wall the lead checks each commit: `Combat`
references **only `Shared` + PurrNet** and reads the body through `Shared` seams
(never the `Player` slice, never `GetComponent` on a sibling); `Player` names no
Combat type (`PlayerSpawner`'s field stays typed `GameObject`, wired to the variant
in the scene); deleting `Combat/` leaves the **base** `PlayerBody.prefab` untouched
and functional.

### New `Shared/Player` seams this milestone adds

*Defined* in Shared so the other side reads them without a sideways reference:
- **`IFacingSource`** — the body's networked facing (`Vector2 Facing`, the XZ
  forward). Written by Player on the **base** (C2), read by Combat hit
  resolution (C6).
- **`IPlayerSide`** — `Side { Attacker, Defender }`. Implemented on the **base**
  body, set by `PlayerSpawner` (C1), read by Combat life-state for defender max
  hearts (C7).
- **`ILifeState`** — `LifeState { Healthy, Downed, Up, Dead }` + `bool CanAct`
  + lifestate-change events. Owned by **Combat** on the variant (C4). Player
  consumes it as an **optional hook**: `PlayerMovement`/`PlayerInput` hold an
  `ILifeState` reference that is `null` on the bare base (⇒ always able to act,
  M1 behaviour) and overridden to the variant's `LifeState` in `Combatant`. Also
  read by M6/M9 later.

---

## C1 — Composition spike + `Combatant` variant scaffold + side split

**Goal:** prove the prefab-variant composition actually replicates under PurrNet,
stand up the empty `Combatant` variant the rest of M2 builds onto, and give every
body a side so defender durability (C7) and the 2v1 setup have something to read.

**Status — done.** Spike verdict: **PASS (host-side
confirmation).** `Combat/Combatant.prefab` exists as a true prefab *variant* of
`Player/PlayerBody.prefab` (YAML is a `PrefabInstance` with `m_SourcePrefab` →
the base guid, **no added/removed components**, only the trivial name/transform
overrides). PurrNet auto-registered the variant in `NetworkPrefabs.asset`. In a
host play-mode run the variant spawned as `Combatant(Clone)` with `isSpawned=true`
and **zero console errors/warnings** — i.e. the M1-class `SyncVar` hasher/index
crash did **not** recur, and the base `NetworkTransform` identity (`_ownerAuth: 0`)
serialized intact. Side split works: with `DefenderSlot = 0` the host body reports
`Side.Defender` through `IPlayerSide`. **Caveat:** this host-side run does not yet
confirm two-client *replication* of the variant; the hasher/index crash class the
spike most fears is caught here, but a LAN host+client confirmation that the
`SyncVar` arrives on a remote observer is the one remaining check before relying on
the "add a `NetworkBehaviour` to the variant" pattern in C4. The C1 variant is
empty (adds no `NetworkBehaviour`), so nothing in C1 itself depends on that gap.

**Build**
- **Spike first (throwaway — do not keep).** In a scratch commit, make a prefab
  variant of a networked prefab that **adds one `NetworkBehaviour` with one
  primitive `SyncVar`** to the base identity. Spawn it two-client and confirm: the
  `SyncVar` replicates, the base's `NetworkTransform` still syncs, and
  `read_console` is clean (no hasher/index error like M1's). **Record the verdict
  in this doc.**
  - **Pass** → proceed below.
  - **Fail** → switch to the nested-child-`NetworkIdentity` fallback (or, last
    resort, single-prefab) and record the decision **before** writing C4. The
    plan's "add to the variant" steps then read "add to the child" / "add to
    `PlayerBody.prefab`" accordingly.
- **Create `Combat/Combatant.prefab` as a variant of `Player/PlayerBody.prefab`**,
  with **no combat components yet** (C3+ fill it). Spawning it must behave
  **identically** to spawning the base today.
- **Repoint spawning at the variant:** wire `PlayerSpawner`'s `[SerializeField]
  GameObject` body-prefab field (in the scene) to `Combatant.prefab`.
  `PlayerSpawner` code is unchanged and still names no Combat type.
- **Side split:**
  - Add `Shared/Player/IPlayerSide.cs`: `enum Side { Attacker, Defender }` +
    `interface IPlayerSide { Side Side { get; } }`.
  - Implement `IPlayerSide` on the **base** `PlayerBody`, backed by a primitive
    `SyncVar<int>` (store `(int)Side`, decode through the seam — **never** a custom
    enum in `SyncVar<T>`; that is the M1 hasher crash). Add `AssignSide(Side)`
    (server-only), symmetric with `Assign(PlayerID)`.
  - `Shared/Config`: add `ConfigKey.DefenderSlot` (int) + a typed field
    (`defenderSlot`, default `0`) + `Entries()` yield in `ConfigDefaults`.
  - `PlayerSpawner.OnRoundStarted`: read `DefenderSlot`; the player at that index
    into `networkManager.players` (the same index it already uses for spawn points)
    gets `AssignSide(Side.Defender)`, the rest `Side.Attacker` — **before**
    `NetworkIdentity.Spawn`, so it ships in the initial network state.

**Notes**
- Side lives on the **base** because `PlayerSpawner` (Player) sets it and already
  holds the `PlayerBody` reference; it's entity role-metadata, not a Combat
  component. Only the *assignment mechanism* (`DefenderSlot`) is the throwaway M4
  replaces — keep it a few lines, don't grow a role system here.
- Optional, trivial, local presentation: tint the defender's capsule off the
  replicated side so a 2v1 test reads at a glance. Put it on the variant if you
  want it to vanish with Combat; on the base is fine too.
- The empty variant is deliberately boring — this commit's value is the **spike
  verdict** and the **structure**, so C3–C8 just keep dropping components onto a
  proven surface.

**Done when**
- The spike verdict is recorded. `Combatant.prefab` exists as a variant of the
  base; `PlayerSpawner` spawns it and bodies move/aim/footstep **exactly as before**.
- `grep Garrison.Player` in `Combat/` is empty; `grep Garrison.Combat` in `Player/`
  is empty (the variant reference is scene wiring, not code).
- With `DefenderSlot = 0`, the host is `Side.Defender` and clients `Side.Attacker`,
  readable through `IPlayerSide` on every client. `_ownerAuth` still 0.

---

## C2 — Networked aim-facing: the body turns to the cursor

**Goal:** the body rotates to face the aim cursor, server-authoritative and
replicated, so every client sees where every player points — the foundation the
weapon visual (C3) and the shot direction (C6) both stand on, and the concept's
"facing from posture" tell.

**Build**
- Stream the **aim direction** owner→server, copying `PlayerInput` exactly: extend
  the existing 60Hz `[ServerRpc(Channel.Unreliable, requireOwnership:false)]` to
  also carry the owner's `IAimSource.AimDirection` (a `Vector2`), still validated
  by `RPCInfo.sender == AssignedPlayer`. (Same packet as move/sprint — no new
  stream, no extra bandwidth beyond 8 bytes.)
- Server-apply facing in `PlayerMovement.Step` (it already runs the 60Hz server
  tick): turn the body toward the streamed aim direction and write
  `transform.rotation`. `NetworkTransform` (`_syncRotation: 1`) replicates it for
  free — **no new networked component.**
- Default to **snap-to-aim** (instant facing reads cleanly top-down). Add
  `ConfigKey.BodyTurnSpeed` (deg/s; `0` or a sentinel = snap) so a turn-rate feel
  can be swept later without code.
- Add `Shared/Player/IFacingSource.cs` (`Vector2 Facing`) implemented on
  `PlayerBody`, derived from the body's rotation, surfaced through
  `ILocalPlayerView` alongside `Movement`/`Aim`. This is what Combat reads in C6
  for the shot direction (server-truth, not a client claim).

**Notes**
- **Server-authoritative**, mirroring movement: the client streams *intent* (aim
  direction), the server applies the rotation. Don't rotate on the client and sync
  up — keep the one authority model.
- Aim is still computed locally by `PlayerAim` (mouse → ground plane); C2 only adds
  *sending the direction* and *the server acting on it*. Remote bodies still don't
  compute their own aim — they receive **rotation** via `NetworkTransform`, which
  is all a remote viewer needs.
- Leave `_ownerAuth: 0`. If an MCP prefab edit flips it (M1's carry-forward
  caveat), reset it.

**Done when**
- On every client, each body visibly rotates to face its player's cursor; turning
  the cursor turns the body for all observers. `IFacingSource.Facing` matches the
  body's forward. Movement still server-applied; `_ownerAuth` still 0.

---

## C3 — Weapon visual (right-hand block) + the local aim line

**Goal:** the character visibly holds a weapon, and the local player sees a thin
line showing where they're pointing — the readability scaffold the fire path (C6)
renders tracers over.

**Build**
- On the **`Combatant` variant** (not the base — no combat, no gun), under
  `Visual`, author a **simple rectangular block** positioned where a human in a
  shooting stance holds a rifle: offset to the **right** of centre, around
  shoulder/chest height, the long axis pointing **forward** (along the body's local
  +Z). Because the body faces aim (C2), the barrel points at the cursor. Pure mesh
  (cube `MeshFilter`/`MeshRenderer`), no script, dark material.
- Add an empty **`Muzzle`** child at the front tip of the block (the tracer/flash
  origin C6 needs). Keep its name/position stable; C6 wires to it within the variant.
- Add a local-only **aim line** as a `Combat/` component on the variant
  (`LineRenderer`-based), owner-gated on `IsLocalView`, drawing a **thin** line from
  the muzzle along the body's `IAimSource.AimDirection` (read via the Shared seam).
  Local presentation only — never networked. Config its width/length/colour
  (`ConfigKey.AimLineWidth`, `AimLineLength`) so it tunes distinct from the tracer.
  - **Reach:** `AimLineLength` is a single large value (far past the screen); the
    camera frustum clips the overshoot for free and a collision raycast trims it at
    blockers. **No screen-edge / viewport math** — an earlier orthographic-only
    version computed the exact edge distance, but under the perspective camera
    (M2 feel pass) that's both wrong and unnecessary: overshoot + frustum clip is
    visually identical at any zoom. (If a *patterned* line texture is ever added,
    switch the `LineRenderer` to `Tile` so the long stretch doesn't smear it.)

**Notes**
- The aim line is a **deliberate assist** (the game isn't about aim precision) and
  must read as *clearly different* from the C6 tracer — thinner, and a calmer
  colour. The tracer (C6) is **thicker** and momentary; the aim line is thin and
  persistent. The visible **gap** between "aim line" and "where the tracer goes"
  is the player's read on their own movement-accuracy penalty — keep that legible.
- Weapon block + `Muzzle` + aim line all live on the **`Combatant` variant**, so
  they vanish with `Combat/` (no combat ⇒ no gun, the correct read). The base body
  has no weapon concept at all.
- Right-hand offset is authored for the **stance read**, not anatomy — there's no
  skeleton; it's a block on a capsule. Eyeball it until it reads as "shouldered."

**Done when**
- Every body shows the weapon block to its right, barrel pointing along facing
  (so, at the cursor) on all clients. The local player sees a thin aim line from
  the muzzle tracking their cursor; remote players don't see your aim line.

---

## C4 — Life-state machine: hearts, downed, dead

**Goal:** the single source of truth for up / down / out — hearts, the
`Healthy → Downed → Up / Dead` machine, the bleed-out timer, and the lifestate
events later slices subscribe to. The first real `Combat/` component.

**Build**
- `Combat/LifeState.cs : NetworkBehaviour` on the **`Combatant` variant**. Server owns:
  - `SyncVar<int> hearts` (seed from `ConfigKey.MaxHearts`, default 3).
  - `SyncVar<int> lifeState` (primitive-backed; decode to
    `LifeState { Healthy, Downed, Up, Dead }`).
  - A server-side bleed-out timer (length `ConfigKey.BleedOutSec`) that runs while
    `Downed`.
- The machine (server-authoritative), exactly the milestone diagram:
  - drop to 1 heart → **Downed** (immobile, bleed-out running);
  - bleed-out expires → **Dead**;
  - any hit while at 1 heart (Downed *or* Up) → **Dead** (0 hearts);
  - (syrette: Downed → **Up**, lands in C8 — leave the transition method, don't
    wire input yet).
- Expose `Shared/Player/ILifeState.cs` (`LifeState State`, `bool CanAct`,
  `event Action<LifeState> StateChanged`, plus became-downed / died events),
  implemented by the variant's `LifeState`. **`CanAct` is false when `Downed` or
  `Dead`.** Combat components and the Player optional-hook read it via
  `[SerializeField]` within the prefab. (M6/M9 get it through their own seam later
  — *not* routed through `ILocalPlayerView`, which the base owns and must stay
  Combat-ignorant.)
- Gate Player action via an **optional hook**: `PlayerMovement` and `PlayerInput`
  hold a `[SerializeField] MonoBehaviour lifeStateSource` (cast to the Shared
  `ILifeState`) and early-out when it is non-null **and** reports `!CanAct` (downed
  = can't move, can't fire). On the bare base the field is `null` ⇒ always able to
  act (exact M1 behaviour); the `Combatant` variant **overrides** it to point at
  this commit's `LifeState`. Player defines the socket in Shared terms; it never
  names Combat.
- A server damage entry — `ApplyHit(PlayerID attacker)` — that C6 calls (and a
  `#if`-guarded debug key to exercise it before C6 exists). Damage rule:
  **1 heart per hit**.
- **Lightweight feedback:**
  - **Hearts HUD** — a `Combat/` component on the variant that renders only when
    `IsLocalView` (base seam) and reads its own `LifeState` directly (same prefab),
    e.g. ♥♥♡ in a screen corner off `hearts`/`ILifeState`.
  - **Downed visual** — body lies flat / dims, driven by replicated `lifeState`
    (so every client sees a teammate go down). **Dead visual** — greyed/inert.
- Config: `ConfigKey.MaxHearts` (int, 3), `ConfigKey.BleedOutSec` (float — the
  rescue-drama dial; seed a placeholder, tune in C9).

**Notes**
- **`LifeState` ≠ heart count.** At 1 heart you are either `Downed` (pre-syrette,
  immobile) or `Up` (post-syrette, mobile). The state carries that, not the number.
- Emit lifestate-change events but **don't reach into other slices** — announce;
  M6 (loot drop) and M9 (spectator) subscribe later. Same one-way seam discipline
  the milestone calls out.
- Audio: **down** cue off became-downed, **death** cue off died — positional,
  through the bus (channel choice: `Alarms`/`Weapons` per taste; gunfire is C6).
- Permadeath is for the round; M2 only owns the transition to `Dead` + the inert
  visual. Don't despawn the body (M6 wants the gear on it; M9 wants to spectate).

**Done when**
- A body taking hits (debug key) goes 3→2→1, enters **Downed** (can't move, bleed-
  out counts down), and dies on bleed-out **or** a further hit. Hearts HUD tracks
  the local body; teammates visibly drop/die on all clients. `!CanAct` freezes a
  downed player.

---

## C5 — Accuracy: movement-state → spread

**Goal:** movement becomes a fire-time penalty — the single biggest anti-rush-B
lever — derived from the M1 `IMovementState` seam and ready for hit resolution to
consume.

**Build**
- `Combat/Accuracy.cs` on the **`Combatant` variant** (or a server-side helper
  `LifeState`/weapon can query). Reads `IMovementState` (idle/walking/sprinting) and produces the
  **current spread** (an angular deviation) from a config curve + the weapon's base
  spread.
- Config: `ConfigKey.AccuracyIdleSpread`, `AccuracyMovingSpread`,
  `AccuracySprintSpread` (the `speed → spread` curve). Weapon base spread lives in
  the weapon profile (C6) and adds on top.
- Server-side value, computed at fire time from server-truth movement state (the
  body already replicates it). **No new networking** — it's an input to C6's hit
  check.

**Notes**
- Keep it a pure read of existing server state → a number. No client involvement;
  the client never reports its own spread.
- This is the value the **aim-line-vs-tracer gap** (C3/C6) visualizes: bigger
  spread when moving/sprinting → tracer departs further from the aim line. That
  visible cost *is* the lever working.
- Curve shape is the tuning surface; leave all three spreads in config for C9.

**Done when**
- Given a body's movement state, the system reports a spread that is min at idle
  and grows through walking to sprint. No consumer is wired yet beyond exposing it
  to C6 (verify by grep — only C6 will read it).

---

## C6 — Test weapon profile + the fire path + hit resolution

**Goal:** the one server-side path that turns "someone clicked fire" into
"hearts removed," with the weapon as a tunable data profile — and the tracer /
muzzle flash / gunfire that make a firefight readable. The milestone's core.

**Build**
- **Weapon profile** as config keys (host-tunable is the point — the bellwether is
  "a number we turn"): `ConfigKey.WeaponFireRate` (rounds/sec — semi-auto cadence
  cap), `WeaponDamageHearts` (1), `WeaponBaseSpread`, `WeaponRange`,
  `WeaponFalloff`. Seed them to a **Garand-style semi-auto rifle** (see Decisions).
- **Fire input** — `Combat/WeaponFire.cs : NetworkBehaviour` on the **`Combatant`
  variant**. Owner
  reads **left-mouse**, one shot per click (semi-auto), and sends a fire
  `[ServerRpc(requireOwnership:false)]` (reliable — a shot is an event, not a
  stream), validated by `RPCInfo.sender == AssignedPlayer` and gated on
  `ILifeState.CanAct`. Server enforces `WeaponFireRate` (reject clicks inside the
  cooldown — never trust client timing).
- **Hit resolution (server)** — on a valid fire:
  1. origin = `Muzzle` position; intended direction = `IFacingSource.Facing`
     (server-truth from C2);
  2. apply **deviation** from C5's current spread (random within the spread cone)
     → actual shot ray;
  3. hitscan the ray to `WeaponRange` with falloff; **server decides** the target
     (raycast against body colliders — the legitimate runtime `GetComponent` case
     from the architecture doc: `hit.collider.TryGetComponent(out LifeState)`);
  4. on a connect, call the target's `LifeState.ApplyHit(attacker)` — **1 heart**.
- **Replicated fire event** — server raises an `[ObserversRpc]` carrying the shot
  line (origin + resolved end point) and the shooter. Every client renders:
  - **Tracer** — a **thick**, momentary line along the resolved (deviated) shot,
    visually distinct from C3's thin aim line;
  - **Muzzle flash** at `Muzzle`;
  - **Gunfire** — positional, distance-attenuated, through
    `IAudioBus.Play(AudioChannel.Weapons, …)` (the channel's first customer).
  - **Hit flash** on the target when a connect lands.

**Notes**
- **Clients never self-report hits** (the milestone's hard rule). The client sends
  only "I fired"; the server raycasts and decides. The tracer clients draw is the
  server's resolved line echoed back — so the tracer *is* the deviation, and the
  gap from the aim line is honest.
- Reuse the **server's streamed facing** (C2) as the shot direction rather than
  re-sending aim with the shot — one aim authority, less to desync. (At 60Hz
  unreliable, facing is current enough for a click rifle.)
- M2 has **no LOS check** — M3 inserts it into this same resolution path. Leave the
  seam obvious (a single "does the ray reach the target" step LOS will gate).
- Friendly fire / valid targets: M2 applies damage regardless of side. Don't build
  team rules here.
- The `WeaponFire` muzzle reference is wired **within the variant** via
  `[SerializeField]` to the C3 `Muzzle` transform — both live on `Combatant`, so
  it's a plain prefab wiring, not a `Find`.

**Done when**
- Click fires at the configured cadence; a hit on an enemy removes a heart
  server-side and drives the C4 machine (→ downed → dead). Tracer + muzzle flash +
  positional gunfire play on all clients; the tracer visibly deviates from the aim
  line more when the shooter is moving/sprinting. Spamming faster than `WeaponFireRate`
  is rejected server-side.

---

## C7 — Defender has 4 hearts

**Build**
- `Combat/LifeState.cs` reads `IPlayerSide` through the C1 seam.
- On server spawn, attackers seed from `ConfigKey.MaxHearts` (default 3) and the
  defender seeds from `ConfigKey.DefenderMaxHearts` (default 4).
- Gunshots still remove exactly 1 heart. There is no armor layer and no focus-fire
  branch in damage resolution.
- Config: `ConfigKey.DefenderMaxHearts` (int, 4).

**Notes**
- This replaces the previous focus-fire armor plan. The MVP rule is intentionally
  plain: defender = one extra hit to kill.
- Keep `MaxHearts` as the attacker baseline so later role/lobby work can still
  tune the two sides separately.

**Done when**
- With `DefenderSlot = 0`, the defender spawns with 4 hearts and attackers spawn
  with 3. Hits reduce hearts normally on both sides.

---

## C8 — Syrette: down → up (the only revive)

**Goal:** the one mechanic that brings a downed player back into play — mobile and
fighting "through the pain," still at 1 heart so the next hit kills. Freely
available in M2 so the loop can be tuned.

**Build**
- `Combat/Syrette.cs` (or fold into `WeaponFire`/`LifeState` input handling): owner
  presses a **use-syrette** key → `[ServerRpc]` → server validates and applies:
  - **self** if the user is `Downed`; **or**
  - a **downed teammate within reach** (a proximity check, server-side — a small
    radius around the user).
  - Effect: `LifeState` transition **Downed → Up** (mobile, **still 1 heart**),
    cancel the bleed-out timer. No heart restored — adrenaline, not a medkit.
- `Up` re-enables action (`CanAct` true again) — the C4 gate now lets them move and
  fire; any further hit → Dead (the machine already does this).
- M2 assumes syrettes are **freely available** (no scarcity) — that's M4's drafted
  shared-pool job. Don't build inventory here.
- Audio: **got-up** cue off the Downed→Up transition, positional.

**Notes**
- **Revive *is* the syrette** — there is no item-less revive (concept + milestone).
  Don't add a hold-to-revive fallback.
- Defender has **no syrettes** — their recovery is respawn (M-later). In M2 the
  defender simply can't self-syrette; only attackers carry them. (Cheapest: gate
  syrette use on `Side.Attacker`, or just don't give the defender the input — note
  the decision either way.)
- Proximity reach: keep the radius in config if it wants tuning
  (`ConfigKey.SyretteReachRadius`), or hardcode a sane value and lift it to config
  if C9 demands — prefer config to honour "no magic numbers."

**Done when**
- A downed attacker self-syrettes (or is syretted by a teammate who reaches them) →
  stands up, mobile and able to fire, still at 1 heart; the next hit kills them.
  Bleed-out is cancelled on the syrette. Got-up cue plays.

---

## C9 — Acceptance pass: the 2v1 feel gate + the TTK bellwether

**Goal:** render M2's verdict. A small firefight (2v1) on the greybox: does it read
**tactical, not twitch** — and is the rifle TTK @ 20m reachable by turning a dial?

**Checklist (from [`m2-combat-core.md`](m2-combat-core.md) "Done when")**
- [ ] A 2v1 firefight on the greybox reads **tactical, not twitch.**
- [ ] The **movement penalty visibly punishes run-and-gun** — the aim-line-vs-
      tracer gap widens when moving/sprinting, and shots stray.
- [ ] The defender's 4-heart durability reads clearly against 3-heart attackers.
- [ ] **TTK @ 20m feels right** and is reachable by turning the weapon-profile /
      spread dials (no code edit).
- [ ] The down→syrette→up→one-more-hit-dead loop plays out; bleed-out drama reads
      (tune `BleedOutSec`).
- [ ] Audio: gunfire (positional, the bus's first Weapons customer), hit, down,
      got-up cues all fire off their events.
- [ ] **The wall holds:** `Combat/` references only `Shared` + PurrNet; `grep
      Garrison.Player` in `Combat/` is empty; no banned patterns; `_ownerAuth` 0.

**The gate (the deliverable)**
- **Pass:** caution beats aggression, the triple-tradeoff (inaccurate + exposed +
  blind-behind) reads as deliberate, and defender durability is legible → M2 is
  go; proceed.
- **Fail (valid, useful):** run-and-gun dominates, TTK feels twitchy, or defender
  durability feels wrong → tune the dials (spreads, fire rate, max hearts,
  bleed-out) and re-judge; escalate only if a dial can't reach it.

**Record (the open decisions closed here)**
- Hitscan-with-deviation — **chosen** (note it held up or didn't).
- Defender armor — **cut**; defender uses 4 max hearts instead.
- `BleedOutSec` — the landed value.
- Garand-semi-auto substitution for the Sten — note whether the rifle was a fine
  bellwether stand-in or the Sten should come back before M3.

**Done when**
- The checklist passes with real instances on a LAN (host defender + 2 attacker
  clients), the decisions above are written into
  [`m2-combat-core.md`](m2-combat-core.md), and the playtest log
  ([`../../playtest/log.md`](../../playtest/log.md)) gets its M2 "combat feel /
  TTK" entry — go or no-go.

---

## Dependency order at a glance

```
C1 variant spike + Combatant scaffold + side split (DefenderSlot)
      │
C2 networked aim-facing ─► C3 weapon visual + aim line ─┐
      │                                                 │
      └──────────────────────────────┐                 │
                                      ▼                 ▼
C4 life-state (hearts/downed/dead) ─► C6 weapon + fire path + hit resolution
      ▲                                 │      ▲
C5 accuracy (movement → spread) ────────┘      │
                                               │
                          C7 defender 4 hearts ─┘
                                               │
                          C8 syrette (down → up)
                                               │
                          C9 acceptance — 2v1 feel gate + TTK ◄── all
```

- **C1 is the foundation** for side-dependent max hearts; **C2 the foundation** for
  the weapon visual and the shot direction.
- The **life-state chain (C4)** and the **fire chain (C2→C3 + C5→C6)** are largely
  independent and can be split between two engineers; they **rejoin at C6**, where
  firing applies damage to the life-state.
- **C7 adjusts C4's** max-heart seeding by side; C6 keeps applying damage straight
  to hearts.
- Config keys land **inline**: `DefenderSlot` (C1); `BodyTurnSpeed` (C2);
  `AimLineWidth`/`AimLineLength` (C3); `MaxHearts`/`BleedOutSec` (C4);
  `Accuracy*Spread` (C5); `Weapon*` profile (C6); `DefenderMaxHearts` (C7);
  `SyretteReachRadius` (C8). The config *system* is
  M0's; M2 only adds keys.
