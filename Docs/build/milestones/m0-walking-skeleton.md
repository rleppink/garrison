# M0 — Walking skeleton & seams

> Stand up the spine everything hangs off.
> **Slice(s):** `Shared/` (+ a greybox plane) · **Depends on:** nothing ·
> **Proves:** 2–6 people connect on a LAN and move around together; the net
> spine and config plumbing are real.

Detail for M0 in [`../plan.md`](../plan.md). Conventions live in
[`../architecture.md`](../architecture.md).

## Why it's first

Nothing else can be felt until people can connect, spawn, and move in the same
world. M0's deliverable isn't a feature — it's **the contracts every later slice
plugs into**. So this file is shaped around the foundations laid and the seam
each one exposes downstream, plus the one vertical flow that proves they're real.

## Foundations laid (and the contract each exposes)

### Lobby & config system
- **Responsibility:** hold host-settable settings, apply them at round start,
  allow change between rounds.
- **Owns:** the config table (a keyed set of typed values) and which player is
  host.
- **Contract exposed:** every later system reads its dials from here *by key* —
  nothing hardcodes a tunable. `N` (attacker count) is read as a general value;
  **no player-cap constant exists anywhere.** This is the vehicle the whole
  "config, not constants" thread rides on.

### Networking conventions
- **Responsibility:** settle the patterns every slice copies, so they don't get
  re-invented seven times.
- **Contract exposed:** *server decides, clients observe* as the default
  authority model; ownership conventions; and the **seam pattern** — a slice
  signals another through an event or a `Shared/` interface, and **never** grabs
  another slice's components sideways.

### Audio bus
- **Responsibility:** a positional-audio routing layer (groups/channels) that's
  live but silent.
- **Contract exposed:** later systems play cues *into* this bus, positionally; no
  one spins up ad-hoc audio outside it. Built now because the concept doc flags
  audio can't be retrofitted.

## The flow that proves it (the walking skeleton)

A single vertical path, end to end:

```
host opens session → clients join (LAN/direct address, no matchmaking)
   → lobby lists players + shows config
   → on start, server spawns one authoritative capsule per player on a flat greybox plane
   → movement replicates from the server
```

- **Server-authoritative movement, no prediction.** Accept the latency feel for
  now.
- **Open decision (deferred to M1):** whether local-movement prediction
  (PurrDiction) is worth adding — decided by how authoritative movement *feels*,
  not pre-emptively.

## Config surface introduced
- The config framework itself (the vehicle for every later dial).
- Seeded only with player count, read as general `N` (display-only at this stage).

## Audio
- Bus stub, silent. Proving the routing exists is the whole deliverable.

## Done when
- 2–6 people on a LAN join one host's lobby and spawn capsules.
- Everyone sees everyone move, server-authoritative, on the greybox plane.
- The host sets a config value and it survives a round reset (mechanism proven
  even with few values present).
- "Deletes clean" check: slice folders are empty and nothing couples to a
  god-object/singleton.

## Explicitly not in M0
- Camera feel (M1), combat/accuracy (M2), client prediction, any objective, NPC,
  or fog.
