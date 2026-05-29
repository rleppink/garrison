# GARRISON — Concept

*A living braindump. Asymmetric multiplayer WWII raid game.*

---

## The pitch

Asymmetric multiplayer. **1 defender** (the **Garrison**) holds a fortified
WWII position (castle, trainyard, country house). **N attackers** (**The Cell**,
SOE-flavored) try to steal one of three valuables and escape. Quick rounds,
~10 min, hard time-floor around 10–12 min so nothing drags.

Three phases, **not hard-marked** during play — they blur into one continuous
round:

1. **Planning** — both sides prepare, secret from each other.
2. **Execution** — the raid happens.
3. **Getaway** — blends out of execution the moment loot is lifted.

---

## The governing pillar

**Nothing teleports. Everything arrives from a place.**

This is the spine of the whole design and the reason it has emergent, readable
play (and the explicit rejection of GTA-style cops spawning in your blind spot).
Applied everywhere:
- Attackers spawn at chosen map edges.
- Reinforcements march/drive in from a road or barracks — visible, readable,
  route-aroundable.
- A killed defender drives back in from an outpost.

Corollary value: **diegetic causality.** Effects should have in-world causes
players can see, predict, and manipulate. That's where counterplay comes from.
(Not a universal law — a design value matched to *this* game's goals of
emergence and readability.)

---

## The combined-arms principle

**Defenses alone lose. The defender alone loses. Only the two combined are
hard.**

The autonomous defenses are a *backbone* that must never *seal* the position —
a crew that ignores the defender can still crack a pure turtle. The lone
defender, stripped of their defenses, is one body against N and should lose a
straight fight. The round is hard only where the two *multiply*: the defender's
live presence is the spike on top of the prepared plan, not a second
independent threat.

This is the lens for tuning every defender tool — **weak in isolation, lethal
in combination.** (See also the automated-vs-present needle in Phase 2, which is
the same principle viewed from the balance side.)

---

## Phase 1 — Planning (short, simultaneous, blind to each other)

**Defender** spends a defense budget (scaled to attacker count N) and places the
three valuables. The alarm is **pre-sounded** — the defender "got wind" of the
raid — which is why reinforcements are already en route from t=0, and why the
defender had time to fortify. Their toolkit does three jobs:

- **Hurt** — anti-personnel mines (S-mine style: maim/incapacitate, not kill),
  MG nests (placeable cover; an unmanned MG is sandbags + bluff — *never
  auto-turrets*, the gunner is a separate body that can be flanked or drawn
  away), the defender's own weapon.
- **Shape** — barbed wire, barricades. (Lockable doors and portcullises are
  *map fixtures*, not defender-placed — default-locked because the Garrison
  got wind.) Funnel attackers into prepared ground. Counter = breaching, which
  costs time and noise.
- **See** — trip flares (wire pops a flare, lights + alerts, no damage), mobile
  searchlights (placed in a chosen arc), sentries crewing existing watchtowers,
  alarm bells. (NOTE: cameras were cut — anachronistic for WWII.)
- **Deceive** *emerges from placement*, not a separate budget category. An
  uncrewed MG nest or watchtower reads as crewed from a distance; wire density
  bluffs where the loot sits; patrol routes are real patrols routed away from
  it. Punishes assumption, rewards scouting.

**Defense budget — Supply.** The unit is **Supply** (faction-neutral: works for
allied or axis garrisons, no shopkeeper fiction). One pool covers placeables
and bodies; the defender divides it however they like — **caps come from cost,
never from rules.** Budget scales with N; starting dial: `25 + 5N`.

Three classes of thing the defender works with:
- **Map fixtures** (provided by the building, free to use): watchtowers, doors,
  portcullis, alarm bells, pre-built positions. Different maps offer different
  loadouts — a major source of strategic variety, and a reason map design
  carries half the defense.
- **Placeables** (equipment the defender positions in planning).
- **Bodies** (NPCs; role assigned at planning — patrol, tower sentry, or MG
  gunner. **No mid-round re-tasking** — a body's role is locked once planning
  ends, no sprinting to crew an empty nest under pressure. **Permadeath:** a
  killed body is gone for the round; reinforcements don't backfill assigned
  posts. The placeable underneath (MG nest, tower) remains as cover and can be
  crewed by the defender themselves. Body-snipe to neuter an expensive
  placement is intended attacker counterplay — a 12-Supply manned MG becomes a
  5-Supply inert nest the moment the gunner dies, which makes "where do I put
  the gunner so they aren't an easy shot" a real planning question.)

Starting costs (all to tune in playtest):

| Item | Supply | Notes |
|---|---|---|
| Barbed wire segment | 1 | Cuttable, plentiful |
| Trip flare | 2 | Info-only, single-use |
| Barricade (sandbags, cart) | 2 | Cover + shape |
| S-mine | 3 | Lethal if unseen, detector-counterable |
| MG nest (unmanned) | 5 | Cover, bluff; can be crewed by a body |
| Mobile searchlight | 5 | Wheeled to a chosen arc |
| NPC body | 7 | Assigned to patrol route, tower sentry, or MG gunner |

So a manned MG = 5 + 7 = **12 Supply** — two items stacked, exactly "very
strong, very expensive." 30 Supply buys ~2 manned MGs, OR 1 manned MG + 1
patrol + a fistful of wire/mines, OR a wire-and-flare maze with no bodies at
all. Every composition should read differently from the attacker side.

Failure modes to watch in playtest:
- *Pure stacks.* All mines → 1 detector breaks the round. All bodies →
  suppress and flank. All wire → no Hurt. Mixed builds should dominate via
  cost alone; if a pure stack wins, tune costs, not rules.
- *Mines that never get hit ≠ waste.* The threat forces scouting; the unfired
  mine bought time. Same as real minefields.
- *Soft caps only.* Tempted to write "max 3 manned MGs"? Resist. If
  MG-stacking is the meta, raise its cost or give attackers a counter (smoke?)
  before adding rule-text.

Key defender decision: **valuable placement = concentrate vs spread.** Pile all
three in one hardened vault (strong but one crack exposes the lot) vs spread
them (forces the crew to commit to a location & travel, but stretches
See/Shape/Hurt thin).

**Attackers** receive an **assigned mission** (a briefing — "secure the cipher
component in the west wing"), telling them which *one* of the three to take.
They draw on a shared map, draft a **finite shared gear pool** (weapons, ammo,
grenades, shovel, mine detector — the real 1941 article), and assign who carries
scarce specialist tools. Handoff of tools is decided here in planning, then you
live with it.

**Spawn selection (CONFIRMED / MVP).** Each attacker picks their *own* spawn
point independently — any position along the map edges. Choices are **changeable
throughout planning** and **visible to teammates** (the planning board shows
who's set up where), so the crew can coordinate split approaches — e.g. a feint
at the front gate while one slips a side. (A paradrop spawn type layers on top
of this later — see Addons.)

The defender knows there are three valuables but **not which one** the crew is
after, so they must hedge. The attackers' target is the only true blind spot —
both sides place bets blind to the other.

---

## Phase 2 — Execution

Doors open; attackers arrive from their edges. They **scout live** (costs
precious time). Defenses are **information-gated**: every device has a *tell*
and a *counter* — lethal if unseen, beatable if seen. Scouting time *is* the
"pay attention" tax. A **kill box** = a spot the defender has rigged so a
careless crew is torn up and an attentive crew walks around.

**Defender = quick-reaction force (QRF):** armored (armor degrades under
*focused* fire — one attacker can't break it, two or three coordinating can),
home-field aware, decisive on prepared ground ("defender's advantage"). But the
**autonomous defenses are the backbone** — they must hold the line even while
the defender is dead/away, or killing the defender becomes a free win. The
defender tips *marginal* fights and plugs the gap the crew is currently
exploiting.

**Home-field, concretely (the MVP solo-defender multiplier).** The defender
always sees their own placements — mines, wire, MG arcs are never fog to them —
and knows the safe lane through their own kill-boxes. So "retreat to prepared
ground" is literal: they can flee *through* the minefield they built and an
unequipped crew can't follow. (Counterplay is diegetic — an attentive crew
watches which path the defender takes and copies the safe lane.) That, plus the
QRF body that wins 1v1s and **reading the raid by ear** (gunfire, mines,
engines, NPC callouts — audio is a primary sense; sector-coarse via alarm
bells/klaxons, pinpoint only if you're close), is the whole MVP defender
multiplier. **No live "spike" verbs needed for MVP** — those are parked under
Addons; ship this lean kit first and add spikes only if the live defender feels
like a janitor for their own placement.

> **THE central balance needle:** how much Hurt is automated vs requires the
> defender's presence. Too much automated Hurt = a turtle that gets cracked once
> and never feared again. Defenses must be *strong but not sealed* so the
> defender's death still matters.

**Attackers don't respawn** — gunshot death is permadeath for the round (v1),
and dropped gear stays on the body for a teammate to retrieve
(escort-your-specialist drama). BUT most of what the defense *does* is
**incapacitate, not kill** (bear traps, gas, downs) so the common outcome of a
mistake is lost time / a rescue, not a benching. Truly-dead players become
**shoulder-spectators**: they pick one living teammate and see *exactly* what
that teammate sees (same camera, same LOS, same fog), switchable to another
teammate. No map overview, no view of enemies/NPCs/traps the living teammate
hasn't seen. Comms stay live. The dead player can stitch a slightly wider
local picture by switching across teammates — accepted leak; if it gets
abused in playtest, add a short blackout cost on switch. The point: enough
agency to stay engaged, not enough info to make "sacrifice a guy to scout" a
strategy.

**Defender death:** always respawns **immediately at an outpost** and drives
back in ~15–30s (motorcycle / kübelwagen). The respawn distance *is* the delay
mechanic — death stays lethal and satisfying; the cost is positional, not a
stun. The "you can hear the engine coming back" beat.

**Reinforcement ramp (dual clock):**
- A pre-sounded alarm means reinforcements are coming from t=0.
- ~5:00 — reinforcements begin trickling in (from a road/edge, visible).
- → ~10:00 — ramps toward "fighting an army," with a **hard floor** (~10–12 min)
  where the defender simply wins. A *ramp, not a wall* — so there's a continuous
  abort-or-commit gradient and the occasional glorious minute-9 extraction. The
  ramp also does double duty: pressure during the break-in AND the gauntlet to
  flee through during the getaway. It's also the anti-stall device (patience is
  inherently costly) and the thing that makes "kill all attackers" reachable for
  the defender.

---

## Phase 3 — Getaway (blends out of execution on the grab)

The moment someone lifts a valuable is the **hinge of the round** — the
defender's uncertainty about *which* objective collapses, and the pursuit
begins.

**Loot tracking** (defender-only, **documents only**). The documents carry a
concealed transmitter the Garrison planted; the position has **radio
direction-finding** capability (real, iconic WWII tech) and gets **periodic
bearings** on it once it's grabbed — a *read, not a lock* (the ~5s cadence is
justified by a fix taking a moment to take). Tracking is **intrinsic and
unconditional** for the documents mission — there's no See-investment scaling
and no defeating it.

**How the Garrison reads the fix.** A portable DF receiver, abstracted as a
**hold-to-consult HUD panel**: hold a key to bring up a small map showing the
latest bearing/fix; release to dismiss. An **audible ping** plays each time a
fresh fix lands, regardless of panel state, so the defender knows new info is
available and chooses *when* to look. Cost: while consulting, they can't
effectively fight or drive — mirrors the Cell's scouting tax. **Hold, not
toggle**, so the panel can't be left up by accident during a firefight.
- Movement during consult: **full speed, motor control intact.** The panel is a
  mostly-opaque overlay (paper map held up in front of your face, not a faint
  HUD layer). Cost is *attention*, not speed — you can't see threats, terrain,
  or where your shot would land. Diegetic ("checking your map while
  walking/driving"), pillar-consistent with the Cell's scouting tax.
- Audio stays live during consult (engines, gunfire, the ping itself) — so
  consulting in the open isn't instant death and players actually use it.
- Driving keeps the heading: vehicle continues on the last input, no
  auto-steering or auto-slowing. Straight road = fine; courtyard chase = you
  clip a wall. Self-punishing without a designer-imposed penalty.
- Firing / ADS dismisses (or disables) the panel — otherwise the defender
  dual-wields aim + fix, which dodges the "can't fight while consulting" tax.
- Bearing-only vs two-tower position-fix is another dial — *not committed*.
  Bearing-only forces the defender to *interpret* (drive along the line, close
  the gap); position-fix is more generous. Prototype the harsher version first.
- The towers themselves are off-screen at the camera's zoom level, so the cue
  has to live on the HUD / in the device, not in the world.

Tower fiction:
- The **DF tower(s) are non-destructible props** — atmosphere/skyline only, no
  interaction. (Decision: destructibility opens too many questions — reach,
  degrade-vs-blind, rebuild, "rush the towers" meta — not worth it now.)
- No tower-destruction or beacon-ditching counterplay needed: the counterplay is
  **baked into the documents profile** (fast + can shoot). You don't hide from
  tracking, you outrun and out-fight it. (A beacon-ditching counterplay could be
  added later if tracking proves too strong — parked, not built.)
- Gold & heavy gear are **untracked** — the defender hunts them via the See
  tools (flares/searchlights/watchtowers/patrols) and the readable slow-carrier
  pursuit below.

**Extraction:** all map edges are valid exits. The defender does *not* know the
destination, but the loot-carrier is **slow + trackable**, so the escape is a
**readable pursuit**, not a guessing game. (slowdown × ping-interval = size of
the "where are they now" circle = the single getaway-difficulty dial.)

**Loot as a football:** kill the carrier and the loot drops where they fell.
Re-stealable. The defender can recover it but **only ever wins by eliminating
the whole crew** (the ramp makes that reachable) — so recovered loot is pure
*denial*, which dodges a both-sides-hug-the-loot stalemate.

---

## Win conditions

- **Attackers win** the instant a *single survivor* crosses any map edge with
  the loot.
- **Defender wins** by eliminating the whole crew (ramp does the heavy lifting)
  or reaching the hard time-floor.

---

## The three valuables (the objective triangle)

The mission is **assigned**, not chosen — so the three must be *differently*
hard, not *rankably* hard, or a draw feels unfair. Differentiated across
multiple axes (tracked / speed / free hands / carry shape):

| | Tracked | Speed | Hands / Combat | Plays as |
|---|---|---|---|---|
| **Documents** | Yes (beacon) | Fast | Yes (one hand) | *Only tracked* — can never hide. Run-and-gun. |
| **Gold** | No | Fast | No (two hands) | *Only defenceless* — outrun & vanish, but lean on the team to clear ahead. |
| **Heavy gear** (captured radar/cipher component — cf. the real **Bruneval raid**) | No | Slow (back-carried, encumbered) | Yes (hands free) | *Only slow* — can't outrun, can only hide & fight. Cornering = death. |

**Why it's clean:** each objective differs from the other two on exactly two of
the three axes, and each is the sole odd-one-out on exactly one (documents =
only tracked; gold = only defenceless; heavy gear = only slow). Three distinct
weaknesses, no strict ranking, so an assigned mission is always "a different
kind of round."

Diegetic notes: beacon-only-on-documents lands (intel bugs a courier pouch; you
don't bug bullion or bulky enemy kit). Infiltration/exfiltration asymmetry:
documents are easy to *reach*, brutal to *leave* with (beacon wakes on grab);
gold & heavy gear make the *carry* the hard part but let you vanish.

### Objective status (resolved this session)

- **Relay is universal** — any teammate can take over any objective
  (bucket-brigade tactics on every mission). Tracking follows the *object*
  (beacon's on the documents), not the carrier: pass to a fresh body, but you
  can never pass off the visibility.
- **Prisoner / escort: DROPPED.** Two killer reasons: (a) diegetic mess — if the
  defender shoots the prisoner do attackers lose, and why wouldn't the defender
  just execute them? No clean answer without artificial plot-armor. (b) escort
  missions are near-universally frustrating (clueless following AI, snagging on
  geometry, babysitting). The role it reached for — a vulnerable, team-dependent
  extraction — is now filled by **heavy gear**: escort *tension* without escort
  *frustration* (no AI to babysit, nothing alive to shoot).
- **Heavy gear = LOCKED as the third objective** (replaces prisoner). Captured
  radar/cipher component à la Bruneval. Slow-solo carry for MVP; spicier
  two-person-simultaneous-carry variant held in reserve as a later option.

---

## Resolved design values / guardrails

- Friends-first (coordination-heavy asymmetric games require it); earn
  matchmaking later "like Among Us did."
- Day/night as a variety axis (night raid favors stealth + makes
  flares/searchlights the defender's lifeline; day flips it).
- No download/hacking objective — anachronistic for WWII.
- **Tactical, not rush-B.** Movement speed and weapon lethality tuned so
  caution dominates aggression — players plan, scout, peek, and commit, rather
  than sprint-and-frag. WWII weapons help naturally (bolt-actions, no laser
  ADS), but pin it down deliberately. The Sten TTK at 20m is the bellwether
  number — get that one right and the rest follows.
- **Names (provisional):** defender side = **the Garrison**; attacker side =
  **The Cell** (SOE-flavored). No per-role title for the defender — "Garrison"
  scales from a lone outpost sentry up to a castle commander without rank
  baggage. Body of this doc still uses "defender" / "attackers" as mechanical
  role terms; the names are for fiction and UI.

---

## Camera & vision

The view is **top-down 3D / isometric**, zoomed in enough that off-screen
props (e.g. DF towers) aren't typically visible during play. That's the central
craft problem of the genre: the *player* sees less than their *character*
diegetically would. Resolved cluster of decisions:

- **Aim extends the camera (Hotline Miami style).** Aiming farther pushes the
  camera in that direction, with a hard rule: *your character never leaves
  the screen.* Looking far is a tradeoff — you see ahead but go blind behind,
  and your aim is also your camera. One input, three costs (focus, exposure,
  facing).
- **LOS fog of war.** You see what your character can see; terrain blocks
  sight. Makes peeking, flanking, and "is that nest crewed?" real questions
  rather than top-down god-mode.
- **Visible sight cones on NPCs only.** Sentries, patrols, MG gunners show
  their cone arcs. Player characters (defender, attackers) do *not* — read
  facing from posture/animation. Preserves human unpredictability and makes
  decoy-routed patrols pay off visually (you can see the bluff cone sweeping
  the wrong arc).
- **Audio is a primary sense, not polish.** Directional engines, footsteps,
  distance-attenuated gunfire, distinct cues for DF pings, reinforcement
  trucks, paradrop swooshes. The off-screen world arrives through the
  speakers. Networked positional audio is a production line-item — invest
  early, don't bolt on.
- **Planning UI preview from attacker POV.** Defender can preview the scene
  from each attacker spawn point's POV during planning, so blind hedge
  placement isn't pure guesswork — they can stress-test what a flanking
  approach will actually reveal before committing.

---

## Combat model

Discrete and readable; tactical-shooter shape inside a top-down camera.

- **3 hearts.** Full health = 3, takes one hit to bring you to 2, etc.
- **Gunshot = 1 heart.** Two gunshots = down to 1, third = down.
- **S-mine damage:** *open.* Initial proposal: 2 hearts (solo mine = down from
  full, mine + follow-up = dead). Alternative: 1 heart + the wire triggers a
  flare/alarm that brings the defender to finish you (damage AND alarm in one
  wire). Both fit "maim, not kill" — pick in playtest.
- **Movement reduces accuracy.** Stand-still to shoot reliably; sprinting
  fire is wild. The single biggest anti-rush-B lever.
- **0 hearts = downed, not dead.** Crawling/incapacitated. Teammate revive
  inside a window of X seconds restores you to 1 heart; otherwise you bleed
  out to permadeath. (X TBD; tunes the rescue-the-specialist drama.)
- **Healing:** *open.* Recommended starting point — **syrettes in the Cell's
  shared gear pool.** Scarce, drafted in planning ("do we bring 2 syrettes or
  trade them for an extra grenade?"), applied to self or a teammate. Defender
  has no healing; their healing *is* respawn.
- **Defender armor.** Same 3 hearts, plus an armor layer that absorbs the
  first hit per heart *unless* two+ attackers land hits within a short focus-
  fire window (then armor breaks instead of absorbing). Makes the "focused
  fire breaks the defender" rule legible and keeps numerics consistent across
  both sides.

Combination check: 3 hearts + movement inaccuracy + Hotline-Miami camera-push
is a *new* combination. Hotline Miami got away with aim-pans-camera because
everything was instant-kill; here the cursor is also accuracy AND camera, so
moving-while-aiming-far has a triple tradeoff (inaccurate + exposed + blind
behind). Prototype early to feel whether it's elegant or fiddly.

---

## Addons / post-MVP (cool extras, NOT core pillars)

### Defender spike verbs (timed, fixture-based)
Live defender controls that *spike* on top of the static plan — the "delivers
the haymaker at the right second" layer (see the combined-arms principle).
Explicitly post-MVP. Governing rule for all of them: **weak alone, lethal in
combination; the surprise is the *timing*, never the *location*** (a hidden
command-detonated device has no tell and is uncounterable — don't build that).

- **Controllable portcullis** — drop a (known, breachable) gate mid-round to
  split the crew; a non-lethal *shape* tool, not a kill. Cheapest to build
  (state toggle on a fixture already in the map), so the natural *first* spike
  verb. Breachable by the crew at a time + noise cost, which is what stops the
  defender just sealing the position at t=0.
- **Placeable demolition charge** — defender positions an *obviously-rigged*
  charge in planning (never concealed — concealment reinvents the uncounterable
  command-mine), detonates live. More creative than map-static charges, same
  "is he at the switch?" tension. Counter = route around, rush before he
  triggers, or sapper-disarm (time + noise).
- **Searchlight re-aim / lights-out** — already-visible See tools, re-aimed or
  cut live for a timing beat.

### Paradrop spawn type
An *additional* spawn option layered on the confirmed per-player spawn selection
— **explicitly post-MVP, not a core pillar.** Pillar-consistent (you come from
somewhere, visibly — and historically on the nose; Bruneval was an airborne
drop).

- Player may choose to **paradrop at a location of their choosing** instead of
  an edge spawn.
- **Not instant:** the descent costs ~15–30s, eating into the round's time
  budget. This does double duty — it's the attacker's time tax *and* the
  defender's reaction window. It must NOT be instant (that would break
  no-teleport and rob the lone defender of any response). The descent seconds
  are the single risk dial.
- **Defenceless landing window** of X seconds — drop right inside and a ready
  defender can pick you off mid-vulnerability.
- **Audible from round start.** If any of The Cell paradrops, the defender
  hears a transport plane circling from t=0 and **one distinct swoosh per
  paradropper** as each bails. Leaks *that* paradrops are happening and *how
  many*, before anyone touches ground — adds reaction time on top of the
  descent seconds. (Pillar-consistent: you can hear them coming.)
- **Telegraphs more than position — it leaks your objective.** Drop by the gold
  vault and the defender's blind-bet uncertainty collapses early; they fold
  defenses onto it before you've grabbed. So the deep drop costs time + safety +
  *secrecy* (3-way cost).
- **Counterplay lives in planning:** a defender expecting paratroopers
  pre-sights obvious drop zones (courtyard, flat roof) with mines/flare/MG arc.
  Deep drop = read-vs-read, and you're landing in terrain you never scouted
  (possibly a kill-box).
- **Trades a hard ingress for a hard egress** (you're deep, far from any edge) →
  so its value is **objective-dependent**: viable for untracked gold/heavy gear
  (drop, grab, vanish on the long way out), near-suicidal for tracked documents
  (deep AND lit up on grab). Slots into the triangle rather than bolting on.
- **Swingy if mass-dropped** (hands the defender one big "catch them all in the
  open" moment) — split/stagger is the safer play.

---

## Still open / not yet built (core)

- **Map design** — the next big structural piece. Must hold 2–3 spread
  objectives with multiple route types (gold's fast-but-defenceless dash wants
  covered lanes the team can clear; documents' sprint wants long routes out of
  tower sightlines; heavy gear's slow haul wants hide-and-fight pockets and
  short hops between cover). Day/night a live variable.
- Defender planning-phase length & moment-to-moment engagement (likely fine:
  place defenses, then sweep See tools / read DF bearings / QRF — but confirm no
  dead time).
- Exact grenade tuning (wreck mines & wire, suppress/scatter NPCs rather than
  annihilate; keep scarce).
- How N scales the budgets in practice.
- **S-mine damage value** (1 heart + alarm vs 2 hearts; see Combat model).
- **Healing model** (syrettes-in-shared-pool recommended; revive-window length
  X TBD).
- **DF chase ergonomics watchpoint.** Hold-to-consult + ping every ~5s +
  driving + bearing-only stacks up; the documents pursuit may feel unwinnable
  for the defender. Playtest target: does the documents mission land as
  "hardest objective" (fine) or "unwinnable for defender" (not fine)? Dials:
  ping cadence, bearing-vs-position fix, consult overlay opacity.
- **Camera-push UX** (snap vs lazy follow on release; push tied to aim or
  separate input; limit shape — radius/ellipse/asymmetric). Looks fine on
  paper, easy to feel awful in the first prototype.
