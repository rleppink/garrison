# GARRISON — Architecture Notes

*The few ground rules for how the code is organized. Not a framework, not a
spec — just the conventions we agree to follow so the codebase stays legible.
Companion to `../design/concept.md` (what we're building) and
`../design/mvp-scope.md` (what we're building first). This doc is about *how*.*

---

## Stack

- **Engine:** Unity 6 (latest LTS at time of writing). One editor version,
  pinned for everyone — version drift is a tax we don't pay during a prototype.
- **Multiplayer:** [PurrNet](https://purrnet.gg/). Server-authoritative where
  it matters (LOS fog, combat, loot state — see the concept doc's "server-truth, not
  client-trusted" note). Client prediction layered on later via PurrDiction
  only where the feel demands it (local movement), not pre-emptively.

---

## Scenes & object lifetime

**A persistent `Bootstrap` scene is the lifetime spine — not
`DontDestroyOnLoad`.** The long-lived services (PurrNet `NetworkManager` +
transport, `ConfigService`, `AudioBus`, `RoundController`, `LobbyController`)
live on a `GameSystems` object in a `Bootstrap` scene that loads first and is
never unloaded. Raw `DontDestroyOnLoad` would fight two of our own rules: you
can't wire a DDOL'd object from another scene in the inspector, so it pushes you
toward `FindObjectOfType` / `*.Instance` lookups — the exact `GetComponent` and
singleton smells we ban. An authored, never-unloaded scene gives the same
"outlives everything" guarantee while keeping everything `[SerializeField]`-wired
at author time.

The scenes:

- **`Bootstrap`** — persistent services; loaded first, never unloaded. The
  DDOL-container, done as a scene.
- **`MainMenu`** — host / join screen.
- **`Lobby`** — pre-game waiting room: player list, config, Start. It's UI over
  the Bootstrap services; make it a scene only if you prefer the pattern — at
  this point it carries no state forward, so there's no architectural difference.
- **`Map`** — the round itself. The one networked, swappable content scene
  (greybox now; real maps later — a major variety axis). Load it through
  PurrNet's scene sync rather than hand-rolling `SceneManager` + a "tell clients
  to load too" RPC. Spawn all gameplay (capsules, defenses, NPCs, loot) **into**
  this scene so it tears down clean on round reset; Bootstrap holds only the
  immortal services.

**Round phases are state over the live `Map` scene, not separate scenes.**
Planning, Execution, and Getaway all play out in the one loaded map, driven by
`RoundController` state. Getaway is continuous with Execution by design ("blends
out of execution on the grab"), and Planning is a **high-overview camera / input
/ UI mode over the real map** — not a separate scene, not an abstract board.
Planning needs the real geometry anyway (attacker-POV sightline previews, the
defender seeing their own placements); a board would mean new artwork plus a
visual↔world mapping for no gain. Splitting Planning into its own scene would
also force the entire plan — every placement, role, spawn pick, drawing — across
a scene boundary and rebuild it as networked objects. That cross-scene state
transfer, not the load, is the real cost.

**The litmus for a new scene:** is it a *distinct world / content set*, or just
a *different view and verb set over the same live state*? Distinct world → a
scene earns its keep. Different view over shared live state → a mode on an
existing scene. The scene *load* is cheap; carrying live, networked state across
it is not — that's the thing to weigh.

---

## Organize by feature, not by layer

**Vertical slices.** Code lives next to the feature it serves, not in a
project-wide `Scripts/Managers`, `Scripts/UI`, `Scripts/Data` pile. A feature
folder owns its behaviours, its data, its UI, its networking glue:

```
Assets/
  Planning/        # supply budget, placement, spawn selection, planning board
  Combat/          # hearts, hits, downed/revive, armor
  Defenses/        # mines, wire, flares, MG nests, searchlights, bodies
  Loot/            # objectives, carry, relay, drop-as-football
  Tracking/        # DF beacon, hold-to-consult HUD
  Reinforcements/  # the ramp, outpost respawn
  Vision/          # LOS fog, camera-push, sight cones
  Shared/          # only the genuinely cross-cutting stuff (see below)
```

A slice should be readable end-to-end without jumping across five sibling
folders. If you're explaining how mines work, everything you point at should
be in `Defenses/`.

---

## Coupling hurts more than duplication

This is the governing rule, the way "nothing teleports" governs the design.

When you're tempted to extract a shared abstraction so two features can reuse
it: **stop and ask whether you're buying reuse at the cost of a coupling.** Two
features that import the same "helper" now move together forever — change it for
one and you've changed it for the other, often without noticing. That bill comes
due later and it's bigger than the bill for a little copied code.

- **Default to duplicating** a small piece of logic across two slices.
- **Extract only when** the thing is genuinely *one* concept (not two that
  happen to look alike today) AND it's stable AND the duplication has actually
  started to hurt. Three strikes, not two.
- `Shared/` is for the load-bearing cross-cutting concerns only — networking
  conventions, the config/lobby-settings system, core math, audio bus. It is
  **not** a junk drawer for "might be reused." Adding to `Shared/` should feel
  like a small decision you can defend, not a reflex.
- A slice may depend on `Shared/`. A slice should **not** reach sideways into
  another slice's internals. If two slices need to talk, do it through an
  explicit, deliberate seam (an event, an interface in `Shared/`), not by
  grabbing each other's components.

The litmus test: *if I delete this feature folder, how much breaks elsewhere?*
A good slice deletes clean.

---

## Wiring references

**`SerializeField` for script references; assign them in the inspector.** This
is the default for everything that exists in a scene or prefab at author time —
which is almost everything.

```csharp
[SerializeField] private MgNest _mgNest;
[SerializeField] private AudioSource _alarmSource;
```

Why: dependencies are visible and explicit in the inspector, they're caught at
edit time instead of failing at runtime, and there's no hidden order-of-
execution lookup happening every spawn.

**`GetComponent` is a code smell.** Reaching for it usually means "I didn't wire
this up" — which is the thing we're avoiding. It hides dependencies and pays a
lookup cost for something you almost always knew at author time.

The legitimate exceptions are narrow:
- **Dynamically constructed objects** — something `Instantiate`d at runtime
  whose pieces genuinely can't be wired in the inspector (e.g. inspecting a
  prefab handed to you, or a runtime hit on an arbitrary collider:
  `collision.collider.GetComponent<IDamageable>()`).
- That's basically it. If you're writing `GetComponent` on a sibling that
  exists in the same prefab at author time, wire it with `SerializeField`
  instead.

When you *do* use `GetComponent` on a dynamic object, prefer `TryGetComponent`
and handle the miss — don't assume the component is there.

---

## A few smaller conventions

- **Private serialized fields over public fields.** `[SerializeField] private`
  exposes to the inspector without exposing to all of C#. Keep encapsulation.
- **No singletons by reflex.** A static `GameManager.Instance` is the fastest
  way to couple every slice to one god object. If something is truly global,
  put it in `Shared/` and inject it; don't reach for the global handle.
- **Config, not constants.** Per `../design/mvp-scope.md`: anything we'd tune between rounds is
  host-settable lobby config from day one. No magic numbers baked into
  behaviours.
- **Server-authoritative by default.** Anything a cheating client could lie
  about (position for fog, who shot whom, loot state) is decided on the server.
  Prototype it authoritative first; optimize feel with prediction second.
