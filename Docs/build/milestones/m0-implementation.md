# M0 — Implementation plan (commit-by-commit)

> Engineer-facing companion to [`m0-walking-skeleton.md`](m0-walking-skeleton.md).
> That doc owns the *why* and the contracts. This one owns the *how* and the
> *order you type it in*. Conventions are in [`../architecture.md`](../architecture.md).

## How to read this

Each section below is **one commit**: a self-contained, compiling, reviewable
step. They're ordered by dependency — do them top to bottom. Every commit lists:

- **Goal** — the one thing this commit makes true.
- **Build** — files/assets/components to add (paths are under `Assets/`).
- **Notes** — PurrNet/Unity specifics and gotchas.
- **Done when** — how you know the commit is finished.

PurrNet API names below are written to the published PurrNet conventions
(`NetworkManager`, `NetworkBehaviour`, `[ServerRpc]`/`[ObserversRpc]`,
`SyncVar<T>`/`SyncDictionary<,>`, `NetworkTransform`, the `PlayersManager`
module). **Confirm exact signatures against the PurrNet docs for the pinned
version before relying on them** — version drift on the netcode is the one place
to verify, not assume.

### Starting state (verified)
- Unity `6000.4.9f1`, URP, Input System already in the project.
- PurrNet **not** integrated yet.
- `Assets/` contains only `Scenes/` and `Settings/`. No feature folders, no
  game scripts. M0 is a true cold start.

---

## C1 — Integrate PurrNet & pin the toolchain

**Status:** Done. PurrNet `dev.purrnet.purrnet` `1.19.1` is pinned through the
Unity Package Manager, and the toolchain note lives at
[`../toolchain.md`](../toolchain.md).

**Goal:** PurrNet compiles in the project; everyone is on one editor version.

**Build**
- Add PurrNet via Package Manager (git URL or Asset Store import per PurrNet's
  install docs). Pin the exact version in `Packages/manifest.json` /
  `packages-lock.json` and commit both.
- Add a short "Toolchain" note to the repo `README` (or `Docs/build/`): pinned
  editor `6000.4.9f1`, pinned PurrNet version, "do not bump without a heads-up"
  — the `architecture.md` "version drift is a tax" rule made real.

**Notes**
- Don't add PurrDiction (prediction) yet. M0 is server-authoritative with no
  prediction; PurrDiction is an M1 open decision.

**Done when**
- Project compiles with PurrNet referenced. A throwaway `NetworkManager` can be
  dropped on a GameObject in a scratch scene (delete before committing).

---

## C2 — Feature-folder skeleton + assembly definitions

**Goal:** the `architecture.md` slice layout exists and the dependency
direction is *enforced by the compiler*, not just by good intentions.

**Build**
- Create empty slice folders, each with an `.asmdef`:
  `Planning/`, `Combat/`, `Defenses/`, `Loot/`, `Tracking/`,
  `Reinforcements/`, `Vision/`, and `Shared/`.
- `Shared/Garrison.Shared.asmdef` references PurrNet (+ Input System,
  Unity modules it needs).
- Each slice asmdef (`Garrison.Planning`, etc.) references **`Garrison.Shared`
  and PurrNet only — never another slice.** This makes "a slice never reaches
  sideways into another slice" a build error, not a code-review note.
- Slice folders stay otherwise empty (a `.gitkeep` is fine). This preserves the
  M0 "deletes clean: slice folders are empty" check.

**Notes**
- Sub-namespaces inside `Shared/` (`Shared/Net`, `Shared/Config`,
  `Shared/Audio`, `Shared/Lobby`, `Shared/Round`, `Shared/Player`) can share the
  one `Garrison.Shared` assembly — no need to split asmdefs within Shared.

**Done when**
- Solution shows the 8 assemblies. Adding a `using Garrison.Combat;` inside
  `Garrison.Planning` fails to compile (proves the wall).

---

## C3 — Net spine: NetworkManager, transport, host/client bootstrap

**Goal:** a host can open a session and a client can connect by direct address.
No UI yet — driven by inspector fields + a temporary key/console hook.

**Build**
- A `Bootstrap` scene (`Scenes/Bootstrap.unity`) with a persistent
  `GameSystems` GameObject holding the PurrNet `NetworkManager` + its transport
  (the UDP/LiteNetLib transport).
- `Shared/Net/ConnectionLauncher.cs` — thin wrapper over `NetworkManager`:
  `Host()`, `JoinByAddress(string ip, ushort port)`, `Disconnect()`. Reads
  address/port from `[SerializeField]` for now.
- `Shared/Net/README.md` (or a header comment): write down the conventions this
  milestone is *settling* so later slices copy them — **server decides, clients
  observe** as default authority; ownership conventions; "talk through a
  `Shared/` interface or a network event, never grab another slice's
  components." This is a deliverable of M0, not decoration.

**Notes**
- Wire references with `[SerializeField]`, assigned in the inspector — no
  `GetComponent`, no `NetworkManager.Instance` singleton reach. `GameSystems` is
  the one scene object slices get handed references to.
- `Bootstrap` is the **lifetime spine** (`../architecture.md`, "Scenes & object
  lifetime"): loaded first, **never unloaded.** Get persistence by *not
  unloading the scene* — do **not** also `DontDestroyOnLoad` the object; doing
  both invites duplicates on any reload.
- LAN/direct only. No matchmaking, no lobby browser.

**Done when**
- Two editor instances (or editor + build) connect over localhost/LAN; the
  PurrNet `PlayersManager` fires join/leave (log it to confirm).

---

## C4 — Lobby & player list

**Goal:** host opens a session, clients join, and the lobby shows who's in.

**Build**
- `Shared/Lobby/LobbyController.cs : NetworkBehaviour` (server-authoritative)
  on `GameSystems`. Tracks joined players via the `PlayersManager` join/leave
  events; holds a `SyncList`/`SyncDictionary` of `(PlayerID, displayName)`.
  Exposes a server-side `StartRound()` the host triggers (no-op target for now).
- `Shared/Lobby/LobbyUI.cs` (client, uGUI or UI Toolkit) — lists connected
  players from the synced list; shows host vs client; host sees a (disabled-for-
  now) **Start** button.

**Notes**
- "Which player is host" lives here (per the M0 contract) — the host is the
  player who opened the session. Don't hardcode a host concept elsewhere.

**Done when**
- 2–6 instances join one host; every client's lobby list shows all players and
  updates live on join/leave.

---

## C5 — Config system (the "config, not constants" vehicle)

**Goal:** a keyed, typed, host-settable, network-synced config table. Seeded
with one value (`PlayerCount`, read as general `N`), shown in the lobby.

**Build**
- `Shared/Config/ConfigKey.cs` — keys (start with `PlayerCount`). Keep it
  extensible; every later milestone adds keys here, not magic numbers in
  behaviours.
- `Shared/Config/ConfigValue.cs` — a small tagged variant (type + int/float/bool
  payload) so the table is typed but uniform.
- `Shared/Config/ConfigDefaults.asset` (ScriptableObject) — the **config table**
  as data: key → default value. This is where starting values live (the
  `mvp-scope.md` table is the running checklist; M0 seeds only `PlayerCount`).
- `Shared/Config/ConfigService.cs : NetworkBehaviour` on `GameSystems`. Holds a
  `SyncDictionary<ConfigKey, ConfigValue>` populated from `ConfigDefaults` on
  the server. Exposes read-by-key: `GetInt/GetFloat/GetBool(ConfigKey)`. Host-
  only setters that write the synced dictionary.
- `Shared/Config/IConfig.cs` — the read interface slices depend on. Slices read
  dials **by key through `IConfig`**; nothing hardcodes a tunable. **No
  player-cap constant exists anywhere** — `N` is just a config read.
- Lobby (`C4`) reads `PlayerCount`/`N` from `IConfig` and displays it.

**Notes**
- `PlayerCount` is display-only at this stage — it doesn't gate joins (no cap).
- Read by key; never cache a constant copy at load. Survival across resets is
  proven in C6.

**Done when**
- Lobby shows the seeded value, sourced via `IConfig.GetInt(PlayerCount)`.
  Changing it host-side syncs to all clients live.

---

## C6 — Round lifecycle + config survives reset

**Goal:** a server round state machine that applies config at round start and
preserves config across a reset.

**Build**
- `Shared/Round/RoundController.cs : NetworkBehaviour` (server) on
  `GameSystems`. States: `Lobby → InRound`, with `StartRound()` (called by
  `LobbyController.StartRound`) and `ResetRound()` (back to `Lobby`).
- On `StartRound`: snapshot/apply config (read the dials it needs via `IConfig`).
- On `ResetRound`: tear down round state, return to lobby — **`ConfigService`
  values are not touched** (config outlives the round; only round state resets).
- Sync current state to clients so `LobbyUI` can show lobby-vs-in-round.

**Notes**
- Enable the lobby **Start** button now (host-only); add a host **Reset** path.

**Done when**
- Host sets a config value → Start → Reset → value is still present (the M0
  "survives a round reset" check, proven even with one value in the table).

---

## C7 — Greybox plane & spawn points

**Goal:** a flat world to stand on and authored points to spawn at.

**Build**
- `Scenes/Greybox.unity` — the first **`Map` scene** (`../architecture.md`),
  kept **separate from `Bootstrap`**: a large flat plane with a collider, neutral
  material, basic lighting.
- A set of `SpawnPoint` markers (empty transforms) — enough for 6 players.
  `Shared/Round/SpawnPoints.cs` collects them (assigned via `[SerializeField]`)
  and hands the server a free point per player at spawn time.

**Notes**
- Greybox only — no map geometry, no night lighting (that's M8). Just a floor.
- Don't fold the plane into `Bootstrap` — it's the swappable `Map`, loaded
  **additively** over the persistent Bootstrap (real maps replace it later).
  Load it through **PurrNet's scene sync** (the server loads, clients follow),
  not a hand-rolled `SceneManager.LoadScene` + a "tell clients to load too" RPC.

**Done when**
- Scene loads with a visible plane and N reachable spawn markers.

---

## C8 — Player capsule + server-authoritative spawn

**Goal:** on round start, the **server** spawns one authoritative capsule per
player at a spawn point.

**Build**
- `Shared/Player/PlayerCapsule.prefab` — capsule mesh + collider +
  `NetworkIdentity` + `NetworkTransform` + the controller scripts (added in C9).
  Register it as a PurrNet network prefab.
- In `RoundController.StartRound` (server): for each player, instantiate the
  capsule at a `SpawnPoint` and network-spawn it **into the `Map` (Greybox)
  scene**, not `Bootstrap` — gameplay lives in the swappable scene so it tears
  down clean. Capsule is **server-owned** (authority stays on the server); map
  `PlayerID → capsule` so input in C9 can route to the right one. Despawn all
  capsules on `ResetRound`.

**Notes**
- The capsule lives in `Shared/Player` *deliberately as throwaway skeleton* —
  there's no slice that owns "player" until movement feel (M1) and combat (M2)
  arrive. Keeping it in Shared keeps slice folders empty for the M0 deletes-clean
  check; M1 relocates/replaces it. Note this in the file header so it isn't
  mistaken for a permanent home.

**Done when**
- Start a round with 2–6 players → each gets exactly one capsule on the plane,
  visible to everyone; Reset despawns them cleanly.

---

## C9 — Server-authoritative movement (no prediction)

**Goal:** each player moves their own capsule; movement is decided on the server
and replicated out. Latency feel is accepted.

**Build**
- `Shared/Player/PlayerInput.cs` (owner/client) — reads WASD via the Input
  System on the local player's capsule and sends intent to the server via a
  `[ServerRpc]` (e.g. `SendMoveInput(Vector2 dir)`), at a fixed cadence.
- `Shared/Player/PlayerMovement.cs` (server) — in `FixedUpdate`/server tick,
  applies the latest received input to the capsule (CharacterController or
  Rigidbody; movement speed read from `IConfig` so even this isn't a constant).
- `NetworkTransform` on the prefab replicates the **server's** transform to all
  clients, including the owner — so the owning player sees their capsule move
  with full round-trip latency. That latency is the expected M0 feel.

**Notes**
- Server-authoritative on purpose: **no client prediction.** Whether to add
  PurrDiction is an explicit M1 decision, made on feel — don't add it here.
- Keep `PlayerInput` strictly "read input + send"; keep `PlayerMovement` strictly
  "server applies." Clean owner/server split sets the pattern later slices copy.

**Done when**
- All players move independently; everyone sees everyone move; positions are
  consistent because the server is the single source of truth.

---

## C10 — Audio bus stub (live but silent)

**Goal:** the positional-audio routing layer exists so later systems play *into*
it instead of spinning up ad-hoc audio. Silent for now.

**Build**
- `Shared/Audio/GarrisonMixer.mixer` — an AudioMixer with groups under Master:
  e.g. `Weapons`, `Footsteps`, `Engines`, `Ambience`, `Alarms`, `UI`. These are
  the channels later milestones route into.
- `Shared/Audio/AudioBus.cs` (Shared service on `GameSystems`) — API like
  `Play(AudioChannel channel, AudioClip clip, Vector3 worldPos)` that routes a
  pooled **3D** `AudioSource` to the matching mixer group. No clips are wired
  yet, so it's silent — proving the routing exists is the whole M0 deliverable.
- `Shared/Audio/IAudioBus.cs` — the interface slices depend on; no slice creates
  audio outside the bus.

**Notes**
- Positional from day one (3D sources) — the concept doc flags audio can't be
  retrofitted. Networking of positional cues is a later concern (M9 mix pass);
  M0 only stands up the local routing layer + the seam.

**Done when**
- `AudioBus` is on `GameSystems`, exposes `IAudioBus`, and a manual test call
  routes a clip to a group positionally (verify in profiler/mixer; then leave it
  silent). No ad-hoc `AudioSource.PlayClipAtPoint` anywhere.

---

## C11 — Acceptance pass & "deletes clean" check

**Goal:** confirm every M0 "Done when" from the milestone doc, and that the
architecture invariants hold.

**Checklist (from [`m0-walking-skeleton.md`](m0-walking-skeleton.md))**
- [ ] 2–6 people on a LAN join one host's lobby and spawn capsules.
- [ ] Everyone sees everyone move, server-authoritative, on the greybox plane.
- [ ] Host sets a config value and it survives a round reset.
- [ ] **Deletes clean:** the seven slice folders are still empty; nothing
      couples to a god-object/singleton (no `*.Instance`); the only shared state
      lives in `Shared/` services injected via `[SerializeField]` / interfaces.
- [ ] Compiler wall holds: no slice assembly references another slice.

**Done when**
- The above all pass with real instances on a LAN, and the playtest log
  (`../../playtest/log.md`) gets its first "M0 skeleton walks" entry.

---

## Dependency order at a glance

```
C1 PurrNet ─► C2 asmdefs/skeleton ─► C3 net spine ─► C4 lobby ─► C5 config
                                                                    │
                                              C6 round + reset ◄────┘
                                                    │
                              C7 greybox ─► C8 spawn ─► C9 movement
                                                    │
                                              C10 audio bus (independent of C7–C9;
                                                    can land any time after C2)
                                                    │
                                              C11 acceptance
```

C10 (audio bus) only depends on the Shared assembly existing, so it can be
picked up in parallel by a second engineer once C2 lands.
