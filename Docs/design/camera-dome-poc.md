# Camera Dome PoC — Implementation Brief

*Proof-of-concept for the fully-fair "dome" camera. Companion to
`camera-fairness.md` (the why); this is the how.*

---

## Goal

Replace the fixed-angle camera with one that **slides on a hemisphere around the
local body**, always looking toward the aim direction. Because the rim pitch is
identical in every compass direction, sightlines are **symmetric in all directions
→ 100% fair** (the hard constraint). The known cost is that keeping north-up forces
the world's vertical to tip with aim (sideways at E/W, upside-down at due-S).

**The PoC answers one question:** is the tipping/inverted world *legible, playable,
and not nauseating?* If yes → we have a fully-fair camera. If no → set the swing
dial to 0 and we've shipped perspective top-down (the fallback), same build.

---

## The one dial

A single `swingDegrees` (0–~40) is the master knob. `rimPitch = 90 − swingDegrees`.

- **swing 0** → camera never leaves the apex → **perspective top-down** (fallback).
- **swing 40** → strong dome, today's-length sightlines at full aim, max world-tip.

**Invariant: every value of `swingDegrees` is 100% fair** (rotationally symmetric
rim). The dial trades sightline-length + world-tip against legibility only —
fairness never moves. Tune freely for feel without touching balance.

Swing magnitude is **driven by cursor distance** from the body (decided):
`t = clamp01(aimDistance / aimRangeForFullSwing)`, optional easing curve.
`sigma = swingDegrees * t`. Apex when the cursor is on the body; leans harder the
further out you aim. This self-tames the center singularity — at `t≈0` the orbit
radius is ~0, so the volatile azimuth moves nothing.

---

## Core math — the north-up dome rotation

**Do NOT use `Quaternion.LookRotation(lookDir, Vector3.up)`.** With a near-vertical
or past-vertical look direction it degenerates and mirrors the axes — exactly the
flip warned about in `CameraRig.cs:42-45`. Instead **build from top-down + tilt:**

```csharp
// Inputs each LateUpdate:
//   aimDir  = horizontal unit vector from body toward cursor (XZ plane)
//   t       = clamp01(aimDistance / aimRangeForFullSwing)   // optionally eased
float sigma = swingDegrees * t;

// Top-down reference: look straight down, screen-up = world +z (north),
// screen-right = world +x (east).
Quaternion topDown = Quaternion.LookRotation(Vector3.down, Vector3.forward);

// Tilt the whole rig toward the aim direction about the horizontal axis
// perpendicular to aim. This sign keeps NORTH pinned to screen-up at every
// azimuth (verified against the hand-authored N/S/E/W transforms).
Vector3 tiltAxis = Vector3.Cross(aimDir, Vector3.up);
Quaternion domeRotation = Quaternion.AngleAxis(sigma, tiltAxis) * topDown;

// Position: frame the body at `distance` back along the look axis.
Vector3 lookDir = domeRotation * Vector3.forward;
camTransform.rotation = domeRotation;
camTransform.position = bodyPosition - lookDir * distance;
```

Sanity check vs the authored transforms (body at origin, distance/height ~ the 10/20
given): aim N → camera south, `rot(50,0,0)`-equivalent; aim S → camera north,
pitch 130° (past vertical → up-vector y < 0 → world inverted); aim E/W → camera
W/E with the Y/Z=±90 bookkeeping. North stays up and east stays right throughout.

---

## Two orthogonal jobs — keep the aim-push

The dome and the existing aim-push do **different, independent** things, and both
are rotationally symmetric, so stacking them stays 100% fair:

- **Dome → viewing *angle*** (re-points the forward axis; sets sightline length in
  the aim direction). This is what Option 6 lacked.
- **Push → body's *screen position*** (slides the body to an edge). This is what
  Option 6 did — and it's still wanted: it's how the body lands at the *bottom*
  when aiming north so the whole screen looks ahead. Centered-body (dome only)
  would waste half the screen looking behind you.

They compose: aim north → dome tilts north **and** push drops the body to the
bottom → full-screen northward view; aim south → dome tilts south **and** push
lifts the body to the top → mirror-identical. The push *failed alone* only because
the old tilt was pinned north; the dome supplies the missing re-point, so the push
now frames a view that actually exists.

## Integration into `CameraRig.cs`

Add a PoC path; keep the known-good fixed camera one toggle away.

- Add `[SerializeField] bool useDome` and the dome knobs (below).
- In `LateUpdate` (currently lines 106–130), when `useDome`: derive the
  **camera-independent** aim offset exactly as `ComputeAimPush` already does —
  `view.Aim.AimPoint − bodyPosition − currentPush` (lines 153–173; keep the
  `currentPush` subtraction — it breaks the feedback loop). From that offset take
  `aimDir` + `aimDistance → t` and build `domeRotation`. Then **keep the push
  pipeline**: feed `domeRotation` into `ComputeAimPush`/`GetShapeRadius`/
  `UpdatePush` in place of the old fixed `cameraRotation`, and frame
  `bodyPosition + currentPush` as today. Only the *rotation source* changes
  (dome instead of the fixed `viewDirection` block); the push is unchanged and
  composes on top.
- `ApplyProjection()` (lines 261–273) is unchanged: FOV still derives from
  `zoom`/`distance`, so perspective (and therefore wall-peeking) is preserved.
  At swing 0 this is literally perspective top-down.

### Inspector knobs (PoC)

| Knob | Meaning |
|---|---|
| `useDome` | Toggle dome vs known-good fixed camera. |
| `swingDegrees` (0–40) | Master dial. `rimPitch = 90 − swingDegrees`. 0 = top-down. |
| `aimRangeForFullSwing` | Cursor distance from body that maps to full swing. |
| `swingCurve` (AnimationCurve) | Linear vs eased t→swing. Eased keeps the center calm. |
| `followSmoothing` | Lag of the camera behind the cursor (feel ↔ fairness tension). |
| `distance` / `zoom` | Existing. Dome radius + perspective strength (peek). |
| `pushExtent` / `safeViewportInset` | Existing. How far the body slides toward the aim edge; lower the inset (→0) to ride the body to the very bottom when aiming north. |

### Smoothing

Smooth the **2D aim vector** (the driver of both azimuth and `t`) with
`Vector2.SmoothDamp` before deriving `aimDir`/`t`, rather than smoothing the
quaternion — avoids slerp artifacts and keeps the singularity tame. `followSmoothing`
is the knob you'll fiddle with most: too snappy → world jerks on every flick; too
laggy → sightline trails aim and fairness feels mushy.

---

## Legibility experiments (toggles, build in order)

1. **Raw / honest baseline (default for first build).** Everything tips with the
   world, characters included. Look at the true worst case before mitigating.
2. **Counter-rotated characters.** Billboard character renderers to stay
   screen-upright while the environment tips. Candidate legibility save.
3. **Facing indicators.** Strong directional silhouette / ground-decal arrow so
   orientation survives inversion.

---

## Verify & evaluate

**Fairness check (should pass by construction):** drop two markers equidistant N and
S of the body (then E/W); confirm identical on-screen separation at any
`swingDegrees`. If not symmetric, the rotation build is wrong.

**The real eval (subjective, the whole point):**
- *Legible?* Can you read characters/threats when the world is sideways (E/W aim)
  and upside-down (S aim)?
- *Playable?* Can you fight and aim across all directions without the tip throwing
  you off?
- *Nauseating?* Does the world tipping as you sweep aim make you sick?

Tune `swingDegrees` down toward 0 as needed; if only 0 is tolerable, that's the
top-down fallback and the PoC has still done its job.
