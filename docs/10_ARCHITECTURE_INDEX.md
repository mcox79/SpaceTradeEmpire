# 10_ARCHITECTURE_INDEX

This file routes readers and LLM sessions into the canonical architecture document without attaching it by default.

Canonical architecture body:
- `docs/ArchitectureV7.4.txt`

Rule:
- Do not attach `docs/ArchitectureV7.4.txt` by default.
- Use this index to select the minimum excerpt needed for the scoped task.


## A. How to use this index in an LLM session

1) In the Context Packet, state which architecture excerpt(s) are required by ID (below).
2) Paste only those excerpt sections (or attach only those excerpts if you maintain them as separate files).
3) If a task can be completed without architecture excerpts, do not attach any architecture content.


## B. Excerpt IDs

Each excerpt ID maps to a topic and the recommended source section(s) within `docs/ArchitectureV7.4.txt`.

If the architecture document changes structure, update this index.


### ARCH-001: Layer model and authority boundaries
Use when:
- clarifying SimCore vs GameShell responsibilities
- enforcing “SimCore is headless” and “Adapters bridge”

Covers:
- SimCore authority and constraints (no Godot dependencies)
- GameShell scope (presentation and input)
- Adapter layer rules (bridge only, no authority drift)


### ARCH-002: SimCore deterministic simulation contract
Use when:
- changing simulation logic, scheduling, or data model
- discussing determinism, ordering, or RNG injection

Covers:
- deterministic inputs and seed usage
- ordering rules
- event streams and stable identifiers (EntityID, ReasonCode)


### ARCH-003: GameShell responsibilities and constraints
Use when:
- changing Godot layer flow, UI, scenes, or input
- changing how GameShell calls into SimCore

Covers:
- what belongs in GameShell
- what must not leak into SimCore
- UI and presentation boundaries


### ARCH-004: Adapter boundary patterns
Use when:
- writing or changing bridge code
- wiring signals/events between runtime and simulation
- mapping Godot input to SimCore commands

Covers:
- adapter interfaces
- translation rules (IDs, payload normalization)
- lifecycle ownership and where state is held


### ARCH-005: Connectivity and interface discipline
Use when:
- reviewing cross-layer coupling
- interpreting `docs/generated/connectivity_*.json`
- adding new events/signals, resource loads, or scene instantiation patterns

Covers:
- allowed dependency directions
- review gates and what counts as a violation


### ARCH-006: Testing surface and runners
Use when:
- creating or modifying test harnesses
- discussing `SimCore.Tests/` and any runners
- defining smoke tests and scenario testing strategy

Covers:
- how headless tests are executed
- what artifacts are expected (when implemented)
- determinism regression strategy


## C. What to attach when asked for “architecture”

Default rule:
- Attach the Context Packet and only the excerpt(s) required.

If the user asks for the full architecture:
- confirm the request is necessary for the task, and otherwise provide excerpt(s) only.

If you must attach the full architecture:
- attach `docs/ArchitectureV7.4.txt` once, then return to excerpt-only behavior.
