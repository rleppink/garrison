# Camera Fairness — Pontification

*The north/south sightline asymmetry problem and the search for a fix.*

---

## Intent / goal

Fights in Garrison are unfair because of the camera. The view is a **fixed,
world-locked perspective camera looking from the south toward the north**, so a
player sees **further north than south**. Consequence: a player facing north can
see a player facing south before that player can see *them*. Every north/south
engagement is lopsided, and "always attack from the south" becomes the dominant
(degenerate) strategy.

**The goal: make engagements fair regardless of approach direction, without
wrecking the game feel** — i.e. keep the tactical "spot before you're spotted"
read, the volumetric look of the world, and a camera that doesn't make people
sick.

---

## Hard constraints

- **Fights must be 100% fair (symmetric sightlines in every direction).**
  *Promoted to hard.* Competitiveness is a core value; even seeing slightly further
  north than south makes the game unbalanceable. This eliminates every "minimize
  the asymmetry" compromise (incl. Option 5) — only *perfectly* symmetric cameras
  qualify. Note the tension with "no disorienting camera": the chosen dome scheme
  buys full fairness at the risk of disorientation, which the PoC exists to test.

- **Perspective parallax is required (peek around walls).**
  Orthographic projection has fixed occlusion — you can never see *around* a wall
  by moving, and wall side-faces don't reveal dynamically. Peeking past cover by
  repositioning is core tactical legibility, and only perspective gives it.

- **No disorienting / nauseating camera.**
  A camera that yaws/rotates with the player was prototyped and actively hated.
  Whatever the fix, the camera must not spin under the player.

- **Preserve the long sightline / spotting fantasy.**
  The genre revolves around scoping, spotting, and seeing things before the enemy
  does. A symmetric view clamped to the *short* side (~12 m) is far too close and
  guts the fantasy — explicitly rejected.

---

## Preferences

- **A tilt feels better than flat top-down.**
  100% straight-down (90°) reads as flat/board-gamey and loses the sense of
  height. *Yielded (PoC result):* perspective top-down (1b) is the only fully-fair,
  non-nauseating outcome — and 1b isn't dead-flat anyway (perspective gives
  height-read toward the screen edges, unlike ortho 1a).

- **Keep the current control scheme (free cursor aim + world-locked WASD).**
  The known-good build aims with a screen cursor and moves WASD in world space.
  *May yield:* only if the camera itself rotates (which forces camera-relative
  movement) — but since the rotating camera is rejected, this preference currently
  holds firmly.

- **Don't shrink the characters too much (keep zoom/intimacy).**
  *May yield:* some zoom-out is acceptable if it meaningfully helps fairness, but
  tiny models are undesirable.

---

## Key insight that frames everything

The asymmetry is a **perspective (projective-divide) artifact, not a tilt
artifact.** Equal pixels near the top of the screen (toward the horizon/north)
map to ever-larger chunks of ground — that convergence is what stretches the
north view and foreshortens the south. *Tilt alone doesn't cause it; perspective
does.*

**But** the very thing that causes the asymmetry (perspective) is also the thing
that gives wall peeking/parallax. And the long north sightline that feels good is
*the same property* as the unfairness — you can't separate the benefit from the
cost.

**Sharper statement (supersedes Option 6's premise): the asymmetry is governed by
how horizontal the camera's forward/tilt axis is.** The camera always *looks
north*, so the far distance is always north and far-south sits behind the tilt —
unreachable by any reframing.
- Fully horizontal (side-on) → infinite north, zero south → max unfairness.
- Tilted toward north (today ~63°) → long north, short south → unfair.
- **Straight down (90°) → forward axis points down → N and S are mirror images →
  perfectly fair.**

So fairness is just *how far you've tilted toward vertical.* This unifies the doc:
**Option 5 (steepen) and Option 1 (top-down) are the same dial** — top-down is
Option 5 taken to its limit. And it kills Option 6: a framing/dolly shift moves
the visible band but **cannot** re-point the north-locked tilt, so it can never
bring far-south into view (confirmed: already implemented, still unfair).

Independent of tilt is the **projection** choice (perspective vs orthographic),
which controls peeking — *not* fairness. These are two separate axes the doc kept
conflating.

This collapses the whole problem into a three-corner tradeoff:

| Keep                                                        | Sacrifice                                                                                |
|-------------------------------------------------------------|------------------------------------------------------------------------------------------|
| **Rotate** sightline to your facing (fair + long sightline) | no camera rotation — *rejected (hated)*                                                  |
| **Symmetric** view (fair + no rotation)                     | the long sightline — ortho kills peeking; steep/zoomed perspective shrinks the sightline |
| **Fixed long sightline** (current look)                     | perfect fairness                                                                         |

**The trilemma holds only under two assumptions, and breaking either yields a
candidate fourth corner:**
- *"Sightline follows facing ⇒ rotation."* False if you point the vision by
  **translating** the framing N/S instead of yawing → **Option 6**.
- *"The projection is a uniform pinhole."* False if you make it **anisotropic**
  (perspective in X, ~ortho in depth), decoupling lateral peek-parallax from the
  N/S divide → **Option 7**.

Both are unproven on *feel/visual coherence*, not on geometry — that's why they're
fourth-corner *candidates*, not free lunches.

---

## Approaches / options

### 1. Full top-down (90°, straight down) — OPEN, ACTIVE
The endpoint of the tilt dial. Fixes fairness completely (forward axis points
down → N and S are mirror images). **Crucially, "top-down" ≠ "orthographic" —
split them:**

- **1a. Orthographic top-down** — fair, but flat *and* walls hide nothing (you see
  only their tops) → **no peeking** → violates a hard constraint. This is the
  board-gamey version everyone pictures and dislikes. *Rejected.*
- **1b. Perspective top-down** — fair (radial symmetry) **and keeps perspective**,
  so walls splay outward from screen center and reveal their sides as you move →
  **peeking preserved.** This is the live option. Not flat the way 1a is: you get
  height read toward the edges.

**What 1b actually costs** (peeking and fairness are *not* on this list):
1. **The directional long sightline** — no more scoping far down a north lane; you
   see a *circle* around yourself. "How far can I see" becomes one clean dial:
   **zoom/height vs character size** (zoom out to see more → smaller models). The
   whole trilemma reduces to this single tradeoff.
2. **Flatness/intimacy at center** — stuff right next to your character (screen
   center) reads flattest; sides only show toward the edges.

Open tension: this guts the *directional* "long sightline" hard constraint, but
may satisfy a *re-read* of spotting as symmetric situational awareness at a chosen
radius. Needs a call on whether that reinterpretation is acceptable.

### 2. Tilted orthographic (isometric / dimetric, à la XCOM/Commandos)
**Rejected.** Initially attractive: keeps the tilt and the volumetric look while
removing the perspective asymmetry. But it's still orthographic, so **occlusion
is fixed** — you can't peek around walls by moving, and side-reveal doesn't
change with position. Violates the hard "perspective parallax" constraint.

### 3. Rotate the camera to follow facing (Family A — "the long sightline follows you")
**Prototyped, then hated and reverted.** The idea: yaw the camera so your facing
reads "up the screen," converting a fixed-direction bias into a fair,
facing-relative one (you always see far in front, blind behind — like eyes).

Findings along the way:
- A rotating camera forces a single frame of reference: **movement must become
  camera-relative** (W = up the screen), or world-locked WASD feels broken (W
  still walked north while "up" rotated).
- **Movement-driven yaw + camera-relative movement = strafing spins the world.**
  So yaw must be driven by *look* (aim/facing), not movement.
- Two coherent control schemes emerged:
  - **Scheme A — free-cursor twin-stick:** keeps free aim; camera lazily yaws to
    aim. Risk: held-W path curves; cursor "chase."
  - **Scheme B — turn-to-face (third-person-shooter style):** mouse turns you,
    reticle forward, WASD strafes; maximally coherent and fits "tactical, not
    rush-B." **Chosen and implemented** (mouse-X integrated heading, cursor lock,
    facing-relative movement, stiff aim-driven camera yaw).
- **Verdict: the rotating camera itself was disliked, hard.** Reverted all three
  touched files (`PlayerAim`, `PlayerMovement`, `CameraRig`) back to known-good.
- (Side note: during the B prototype a "doubled 3D image" appeared in the scene;
  diagnosed as a single-camera rendering artifact, never fully root-caused —
  abandoned with the revert since the whole approach was dropped.)

**The rotation sub-trilemma.** Re-pointing the forward axis along your aim *does*
fully fix fairness (the key-insight lever). But three things conflict and you can
only ever hold **two**:
- **Long sightline** in the aim direction (the fairness fix).
- **North-up** (compass locked → controls stay world-locked).
- **Gravity-up** (the world's vertical stays at the top of the screen → characters
  and walls upright).

| Keep | Keep | Sacrifice | Result |
|---|---|---|---|
| Long sightline | Gravity-up | **North-up** | **Option 3** — compass spins, movement goes camera-relative → *rejected (hated)* |
| Long sightline | North-up | **Gravity-up** | **Dome scheme (below)** — controls locked, but the world tips over as you aim |
| North-up | Gravity-up | **Long sightline** | **Option 8** — no rotation at all, but only partial fairness |

**Dome scheme — slide the camera on a hemisphere, look-at-player, north-up.**
Concrete transforms (character at origin): apex `pos(0,20,0) rot(90,0,0)` when
aiming at self; aim N `pos(0,20,-10) rot(50,0,0)` (today); aim S `pos(0,20,10)
rot(130,0,0)`; aim W `pos(10,20,0) rot(50,-90,-90)`; aim E `pos(-10,20,0)
rot(50,90,90)`. Verified: **north stays up, east stays right** at every azimuth —
no compass spin, no camera-relative movement, fully fair. **The price** is forced
by geometry, not chosen: keeping north-up while looking from the far side tilts the
camera *past* vertical (note aim-S pitch = 130° > 90°; its up-vector
`(0,-0.643,0.766)` has a **negative y** → world-up points toward screen-bottom).
So the **world's vertical rolls with aim**: characters/walls stand upright aiming N,
lie on their side aiming E/W, and go **fully upside-down aiming S**. One full tumble
per aim revolution. Unavoidable — it's the `Z=±90` in the E/W transforms, and
pitch-past-90 does it for S even with `Z=0`.
- *Verdict:* **Rejected (PoC failed).** Looking north is fine; looking south —
  camera on the north rim, pitch past 90° → "through your legs," inverted — is odd
  and disorienting. The gating risk hit exactly where predicted. **Not tunable
  away:** south pitch is `90 + sigma`, so *any* nonzero swing inverts at full south
  aim (smaller swing = less upside-down, never upright). Queued mitigations
  (counter-rotated characters, facing arrows) only right the *characters*, not the
  tipping *environment* — no save in that direction. → drag swing to 0 → ship 1b.

### 4. Decouple vision from camera — symmetric character-centric fog (Family B)
**Rejected.** Make enemy visibility a radially symmetric, character-centric
range, with the camera framing being cosmetic. Problem: to be symmetric it must
clamp to the *short* (south) side ≈ 12 m, which is far too close for a
spotting/scoping game. Expanding the south to match the north isn't possible
because the camera doesn't *show* the south that far (you'd be shot from below the
screen edge).

### 5. Commit to fixed perspective, shrink the asymmetry by tuning (corner 3)
Accept that perfect fairness is impossible without rotation, and instead drive the
residual asymmetry down to "doesn't decide fights" using **two dials already in
the rig, neither of which touches movement or aim:**
- **Tilt steepness** (`viewDirection`): currently ~63° down. Steepening toward
  ~73–76° pushes the vanishing point far off-screen, making convergence across the
  visible band much gentler → N/S evens out. Cost: walls lean less (less
  side-reveal).
- **Perspective strength** (`distance` / FOV): more telephoto = less convergence =
  less asymmetry, but flatter parallax (less peek). Wide-angle = the reverse.

These pull against wall-legibility on the same axis, so it's a **knee-finding
exercise**: the steepest, most-telephoto setup that *still* lets you peek past a
wall and read its sides. It will never reach symmetric — the **residual is carried
by map design and audio** (cover/lane placement and hearing enemies before the
sightline matters are the genre's real fairness tools anyway).

---

### 6. Facing-driven vertical framing offset (dolly, not yaw)
**Already implemented — and proven insufficient.** The character *does* slide on
screen with aim (full north → bottom of screen, full south → top). The hope was
that framing south would equalize the south view. It doesn't: the camera's tilt
stays locked pointing north, so far-south is behind the tilt and never comes into
view — a framing shift moves the *near* band but can't re-point the forward axis.
Result: the north-facing player still spots the south-facing player first.
**Eliminated** as a fairness fix (see the sharpened key insight). Lesson: only
re-pointing the tilt fixes this — by rotation (3, hated) or by going vertical
(1, top-down).
**But retained as a framing layer atop the dome:** the push does *screen position*
(body to the bottom when aiming N → full-screen forward view), which is orthogonal
to the dome's *viewing angle* and equally symmetric → composes without breaking
fairness. It failed *alone* only because the old tilt was pinned north; the dome
supplies the re-point it lacked. Dome = angle, push = position; keep both.

### 7. Anisotropic / sheared projection (perspective in X, ~ortho in depth) — NEW CANDIDATE
**Untested, speculative.** The asymmetry is the projective divide along the
**forward (N/S)** axis; wall-peeking parallax is mostly the divide along the
**lateral (E/W)** axis. They're different axes, so a custom projection matrix can
keep perspective in screen-X (→ peeking) while flattening toward orthographic in
depth (→ symmetric N/S). If it works, it's the genuine fourth corner: peek +
symmetric + no rotation + long sightline. Risk: non-pinhole projection → occlusion
may render inconsistently (walls lean only in X, side-reveal that doesn't match a
coherent eye-ray). May look subtly wrong; needs a visual spike to judge.

### 8. Dynamic pitch toward top-down (the non-flipping kernel of the orbit idea)
**Untested.** Keep the camera **always north-up, always south-side — never orbit
to the other side.** Modulate only the *pitch magnitude* with aim: aim north →
shallow ~65° (full long lane); aim south / at character → steepen toward top-down.
- **No rotation, no flip** — north stays up the whole time → satisfies the hard
  constraint. Pitch-only motion is far gentler on the stomach than yaw/flip.
- Effect: you get the long scope when you look down the lane, and the view
  **flattens to fair top-down when you look the "unfair" way** (south), giving a
  symmetric radius instead of near-nothing.
- **Not perfectly fair** — your top-down south radius is still shorter than an
  opponent's long north lane; it removes the *worst* of the asymmetry, not all.
- Essentially a *dynamic blend between today and Option 1b, driven by aim.* Open
  questions: does pitch animating with aim feel good or queasy? Does it muddy
  read by constantly changing the projection?

## Status

**RESOLVED → perspective top-down (Option 1b).** The dome PoC was built and
played; north was fine but **south aim ("through your legs," inverted) was odd and
disorienting** — the predicted gating risk, and structurally un-tunable (any swing
inverts at full south aim). So the swing dial lands at **0 = perspective top-down**,
which the PoC build already ships as its built-in fallback.

Why 1b is the right landing spot, not a compromise:
- **Fully fair** — radial symmetry, the hard constraint, satisfied exactly.
- **Keeps perspective → keeps wall-peeking** (the other hard constraint); only the
  *directional* long sightline is given up, replaced by a symmetric awareness radius
  whose size is one clean dial (zoom/height vs character size).
- **Not flat like ortho 1a** — perspective splays walls outward and gives
  height-read toward the edges.

The "tilt feels better" preference yields here (see Preferences) — it was always
allowed to yield if top-down was the only fully-fair, non-nauseating outcome, which
the PoC confirmed it is.

**Path history (all eliminated):** 5/6/3-plain/1a/2/4 out on the fairness hard
constraint or peeking; **dome out on the PoC** (south inversion); 8 out (only
partial fairness); **7 (anisotropic) remains the sole far-future wildcard** if the
loss of the directional long sightline ever proves intolerable.

Next: set `swingDegrees = 0` (or strip the dome path), keep the perspective
top-down rig, and tune the single zoom/height ↔ character-size dial for the
awareness radius.

Next: implement per the PoC doc, then tune `swingDegrees` + `followSmoothing` live
and make the legibility call.
