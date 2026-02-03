\# 20\_TESTING\_AND\_DETERMINISM



This document defines the testing contract for SimCore and the determinism requirements that make the simulation balanceable, debuggable, and safe for LLM-driven iteration.



\## A. Purpose



Define:

\- what “deterministic” means in this repo

\- what must be testable headlessly (without Godot)

\- the test unit hierarchy (unit, invariant, scenario, suite)

\- what outputs are considered stable and diffable

\- how to structure “proof” for changes (what must fail before and pass after)



This file is canonical. If practice drifts from this file, either:

\- fix the tooling/tests to match this contract, or

\- update this contract first (with an explicit reason).



\## B. Non-negotiable invariants (locked contract)



TEST-001 SimCore is headless-testable

\- SimCore must be testable from a .NET console runner (or equivalent), with zero dependency on Godot runtime objects or Godot namespaces.

\- Any testing harness for simulation logic must not rely on frame timing.



TEST-002 Determinism

\- Given the same InitialSeed and the same deterministic inputs (scenario/config/commands), SimCore must produce identical outputs across runs on the same build.

\- “Identical outputs” means:

&nbsp; - identical final state (bitwise where feasible, otherwise a stable canonical hash)

&nbsp; - identical emitted event stream and metrics, when those outputs are enabled



TEST-003 No hidden nondeterminism

\- The simulation must not depend on:

&nbsp; - wall clock time

&nbsp; - machine-specific absolute paths

&nbsp; - locale-dependent sorting or formatting

&nbsp; - unspecified iteration ordering (for example: relying on dictionary enumeration order)

&nbsp; - unseeded randomness



TEST-004 Repro-first workflow

\- If a bug exists, the workflow is:

&nbsp; 1) add a failing deterministic test (unit test, invariant, or scenario)

&nbsp; 2) fix until it passes

&nbsp; 3) keep the test as regression coverage



\## C. Determinism contract details



\### C1. Deterministic inputs



The deterministic input set must be explicit and serializable. At minimum, one of the following must fully define a run:



Option A: Scenario-driven run (preferred)

\- ScenarioId

\- InitialSeed

\- StopCondition (for example: simulate N days)

\- Optional config overrides (must be serializable and stable)

\- Optional command script (a deterministic list of commands with timestamps/ticks)



Option B: Harness-driven run (acceptable early)

\- InitialSeed

\- Explicit set of systems enabled/disabled

\- Explicit run length (N days or N ticks)

\- Explicit command list



\### C2. Forbidden sources of nondeterminism



In SimCore logic (and in tests that claim determinism), avoid:

\- DateTime.Now / UtcNow and equivalents

\- Guid.NewGuid and equivalents

\- Random without a seeded RNG injected from the deterministic input set

\- unordered collection iteration as a “behavioral dependency”

\- floating point behavior that depends on platform-specific math paths without normalization



If a system requires “randomness,” it must consume an injected RNG seeded from InitialSeed (or a derived deterministic seed per system).



\### C3. Stable ordering rules



If outputs contain collections (events, metrics, entities), ordering must be explicit:

\- sort by stable keys (EntityID, stable tick index, stable type id, stable name)

\- never rely on “incidental” in-memory ordering



\## D. Test units and what they prove



\### D1. Unit tests

Goal: verify pure logic in isolation.



\- Prefer unit tests for:

&nbsp; - economy math

&nbsp; - inventory transforms

&nbsp; - routing decisions that can be expressed without a full world

\- Run via the repo’s .NET test project(s) when present.



\### D2. Invariants (silent drift catchers)

Goal: halt immediately on impossible state.



\- Invariants are assertions evaluated during simulation steps in debug/test contexts.

\- Invariants must be:

&nbsp; - deterministic (no time/random)

&nbsp; - cheap enough to run frequently in test builds

&nbsp; - actionable (clear failure message with stable IDs)



Minimum recommended invariant classes (expand over time):

\- numeric sanity: no NaN / Infinity

\- bounds: values remain within documented ranges

\- conservation-style checks where applicable

\- structural checks (graph connectivity or reachability where required)



\### D3. Scenarios (smallest integration unit)

Goal: verify multi-system behavior deterministically.



A scenario is the smallest unit of integration testing for SimCore. Any non-trivial SimCore change must be verifiable by at least one scenario (or an equivalent deterministic harness run) that can be re-run exactly.



Scenario file format is intentionally unspecified here until the schema is implemented. When implemented, this doc must be updated to reference:

\- the schema location

\- the canonical runner command



\### D4. Smoke tests (fast system sanity)

Goal: “does it run” plus basic invariants.



A smoke test should:

\- generate a world from a fixed seed

\- simulate a short, fixed horizon (for example: 30 days)

\- assert no crashes and invariants remain satisfied



A longer-horizon smoke test is recommended once performance supports it.



\## E. Performance budgets



These are targets. Treat them as budgets to enforce once the runner and benchmarks exist.



PERF-001 Headless speed target (budget)

\- Target: simulate 1 year in under 5 seconds in headless mode on a developer machine.



PERF-002 Regression detection

\- Once benchmarks exist, any material slowdown should be caught in Tier 2 (CI/nightly) runs.



\## F. Runner and artifacts



\### F1. Headless runner (CLI surface)



When a headless runner exists, it should support:

\- running one scenario

\- running a batch across N seeds

\- emitting artifacts to an output directory

\- exiting non-zero on:

&nbsp; - determinism failure

&nbsp; - invariant failure

&nbsp; - schema failure

&nbsp; - crash



If a headless runner does not exist yet, treat the above as a required milestone (not optional) for balancing and long-horizon tuning.



\### F2. Stable artifacts (when implemented)



When artifact emission is implemented, the minimum stable outputs should be deterministic and diffable:

\- run\_manifest.json

&nbsp; - ScenarioId, seed, config hash, build/version id, git commit hash (if available)

\- metrics.json

&nbsp; - schema version, units, cadence

\- events.jsonl

&nbsp; - structured event log with stable tick/day index and stable EntityIDs



Snapshots may exist behind a flag and must have size limits.



If artifacts are not implemented yet, do not invent them in workflow claims. Add a TODO and keep evidence in the scenario/tests.



\## G. Validation tiers and when to run them



This doc defines testing semantics. The operational “what commands to run” lives in:

\- docs/40\_TOOLING\_AND\_HOOKS.md



Mapping (intent):

\- Tier 0: always-fast checks (build/parse/basic validators)

\- Tier 1: correctness gates when wiring or simulation changes occur (tests, connectivity scan, smoke tests)

\- Tier 2: long horizon, perf, multi-seed determinism regression



\## H. “Proof” requirements for changes



For any SimCore behavior change, include at least one:

\- unit test, or

\- invariant, or

\- scenario



And it must be able to demonstrate:

\- fail-before, pass-after (or a measurable metric change with explicit acceptance criteria)



For any determinism-sensitive change, include:

\- a determinism check (same inputs, identical outputs) in the test evidence.



\## I. What to attach in LLM sessions



Do not attach large logs by default.



Attach only what is necessary:

\- the generated Context Packet: docs/generated/01\_CONTEXT\_PACKET.md

\- the minimal allowlisted files needed for the change

\- if a test is failing:

&nbsp; - the smallest failing test/scenario file

&nbsp; - the minimal runner output excerpt needed to diagnose



If determinism is in question:

\- attach a small excerpt of the event log or a single deterministic state hash line, not the whole run output.



\## J. TODOs (only if missing in the repo)



If any of the following do not exist yet, they should be tracked explicitly as tooling milestones:

\- a headless runner entrypoint for SimCore scenarios

\- a scenario schema and a small scenario library (at least 3 scenarios)

\- a deterministic artifact writer (manifest, metrics, events)

\- a Tier 2 determinism regression run across multiple seeds



