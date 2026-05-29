# Shared Net Conventions

M0 settles the default networking pattern for every later slice:

- Server decides, clients observe. Clients send intent; server owns game state.
- Scene and prefab references are inspector-wired from `GameSystems`; feature
  code does not reach for `NetworkManager.main` or a singleton.
- Bootstrap is the lifetime spine and stays loaded. Do not also mark
  `GameSystems` with `DontDestroyOnLoad`.
- Gameplay objects spawn into the networked Map scene, not Bootstrap, so reset
  can tear the round down cleanly.
- A slice talks to another slice through a `Shared/` interface or a network
  event. It does not grab sibling slice components directly.
