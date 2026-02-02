\# 10\_ARCHITECTURE\_INDEX



This file is a router into the canonical architecture document. Do not attach the full architecture by default. Use this index to pull only the relevant sections.



Canonical architecture:

\- `./ArchitectureV7.4.txt`



\## How to use this index



1\) Start every session with a Context Packet (`templates/01\_CONTEXT\_PACKET.template.md`).

2\) If the work touches architecture invariants, attach:

&nbsp;  - this index

&nbsp;  - only the relevant excerpt(s) or module(s) from the canonical architecture



\## Architecture usage patterns



\### If you are changing simulation logic (SimCore)

Consult:

\- SimCore authority and engine-agnostic boundary rules

\- Determinism, invariants, and scenario-based testing requirements

\- Performance budgets for headless simulation



Expected proof:

\- A headless scenario or invariant that fails before your change and passes after.



\### If you are changing UI/gameplay glue (GameShell)

Consult:

\- Air gap rules (no direct mutation of SimCore internals)

\- Adapter-only boundary constraints

\- Explainability requirements (ReasonCode, EntityID)



Expected proof:

\- Integration test or harness path that exercises the adapter boundary, with deterministic outputs where feasible.



\### If you are changing boundaries (adapters, events, connectivity)

Consult:

\- Layering and allowed dependency direction rules

\- Explainability payload standards

\- Connectivity map requirements (generated artifacts)



Expected proof:

\- Updated generated connectivity outputs plus at least one integration test for the boundary.



\## Invariants and IDs



Use stable IDs in docs, issues, and PR notes. Keep these short and action-oriented.



Suggested ID namespaces:

\- `INV-###` architecture invariants

\- `TEST-###` testing requirements

\- `CONN-###` connectivity requirements

\- `PROC-###` workflow/process requirements



\### Required invariants (seed list)

These are the invariants that must always remain true. Expand as you formalize more.



\- `INV-001` SimCore is engine-agnostic and must not depend on Godot runtime objects.

\- `INV-002` GameShell must not directly mutate SimCore internal state. Cross-layer mutation is adapter-only.

\- `INV-003` Determinism: same InitialSeed + same CommandList yields identical FinalState in headless mode.

\- `INV-004` SimCore must be testable via a headless runner (console app), not frame-driven.

\- `INV-005` Explainability: meaningful negative outcomes must carry ReasonCode, and UI-facing events must reference stable EntityID.



\## Architecture excerpts to attach by task (cheat sheet)



Attach only what you need.



\- Economy and logistics changes:

&nbsp; - determinism and invariants sections

&nbsp; - scenario schema and runner requirements

&nbsp; - performance budget section



\- Threat/pressure systems changes:

&nbsp; - bounded opacity rules (what can be hidden vs must be explainable)

&nbsp; - event/telemetry semantics (ReasonCode, EntityID)



\- Save/load, serialization, state snapshots:

&nbsp; - determinism constraints

&nbsp; - snapshot/artifact constraints and schema versioning



\- Tooling or validation changes:

&nbsp; - contract header rules

&nbsp; - validation gates and staged-content rules



\## Notes

This index is intentionally short. The canonical architecture text remains the authority.

If something is a hard rule, give it an ID and keep it listed here.



