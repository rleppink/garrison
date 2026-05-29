# GARRISON — Docs

Asymmetric multiplayer WWII raid game. **1 defender** (the **Garrison**) holds a
fortified position; **N attackers** (**The Cell**) try to steal a valuable and
escape. This repo is the design + build documentation.

## Where things live

| Doc                                              | What it's for                                                                                                      |
|--------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| [`design/concept.md`](design/concept.md)        | The concept doc — a living braindump of the game's ideation. Broad and exploratory, not a settled spec. The *what* and *why*. |
| [`design/mvp-scope.md`](design/mvp-scope.md)     | The first playable: what the MVP builds and what it deliberately refuses to. Wins over the concept doc for MVP purposes. |
| [`build/architecture.md`](build/architecture.md) | Code-organization ground rules (Unity 6 + PurrNet, vertical slices). The *how*.                                    |
| [`build/plan.md`](build/plan.md)                 | The build plan — the milestone sequence (M0–M9) and dependency shape. The *order*.                                 |
| [`build/milestones/`](build/milestones/)         | Per-milestone plan docs, broken out from `plan.md`.                                                                |
| [`playtest/log.md`](playtest/log.md)             | Session-by-session playtest log. Balance is chased by playing, not spreadsheet.                                    |

## Reading order

New here? Read `design/concept.md` for the vision, then `design/mvp-scope.md` for
what we're building first, then `build/architecture.md` and `build/plan.md` for
how and in what order.
