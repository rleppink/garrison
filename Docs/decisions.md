# Decisions

The decision log for Garrison — game design, UX, controls, and technical calls in
one place. (It's the ADR pattern, minus the "architecture" baggage; these are just
*decision records*.)

**How this works**

- One section per headline decision: the call as the heading, a one-line *why*, and
  a link to the full reasoning.
- **What earns a section:** there was a real fork and future-you would want the
  reasoning. Mundane/expected calls (the obvious-best choice nobody would
  relitigate) don't — they're just *building the thing*, not deciding it.
- **Record outcomes, not investigations.** A spike, prototype, or PoC isn't a
  decision; the decision is what you concluded from it. A rejected option lives in
  the winning decision's *why*, not as its own section.
- **Append-only, newest at the bottom.** Don't rewrite a settled decision. If one is
  reversed or refined, add a new section; if it replaces an earlier one, note
  *"Supersedes: \<that decision\>"* under the new heading and *"Superseded by:
  \<this one\>"* under the old.

---

## Fairness is a hard constraint (symmetric sightlines)
Competitiveness is core; even a slight north-over-south sightline bias makes the game unbalanceable.
[design/camera-fairness.md](design/camera-fairness.md)

## Keep free cursor aim + world-locked WASD
A camera that yaws/rotates with the player was prototyped and actively hated.
[design/camera-fairness.md](design/camera-fairness.md) (Option 3)

## Ship perspective top-down camera
The dome scheme (the other fully-fair candidate) inverts the world on south aim ("through your legs") and is nauseating; top-down is the only fully-fair, non-nauseating view, and perspective keeps wall-peeking.
[design/camera-fairness.md](design/camera-fairness.md) (Option 1b, Status)
