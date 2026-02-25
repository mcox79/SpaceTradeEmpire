# 54_EPICS

This is the canonical development ledger and roadmap.
Architecture and design-law docs define the spec. This doc defines execution order and tracking.

Generated artifacts (status packets, scans, test outputs) live under docs/generated and are evidence only.

Status meanings:
- TODO
- IN_PROGRESS
- DONE
- DEFERRED

Note: Status tokens are exact: TODO, IN_PROGRESS, DONE, DEFERRED (no spaces, no variants).

Update rule:
- Each dev session should target 1–3 gate movements or epic movements, and end with an update here plus evidence references.

Primary anchors:
- docs/50_51_52_53_Docs_Combined.md (slice locks, gate tests)
- docs/30_CONNECTIVITY_AND_INTERFACES.md
- docs/20_TESTING_AND_DETERMINISM.md
- docs/21_90_TERMS_UNITS_IDS.md
- docs/40_TOOLING_AND_HOOKS.md

---

## Status semantics (source of truth)
- docs/56_SESSION_LOG.md is authoritative for what was completed (PASS entries).
- docs/55_GATES.md is the authoritative current status ledger (must match session log).
- Epic and Slice statuses in this file are summaries derived from gates (see docs/generated/epic_status_v0.md).
- If any mismatch exists, fix docs/55_GATES.md first, then regenerate epic status.

---

## A. Slice map (Layer 1)

## Canonical Epic Bullets (authoritative for scanning and next-gate selection)
## Rule: each EPIC ID appears exactly once in this section.

- EPIC.S2_5.WGEN.DISTINCTNESS.REPORT.V0 [DONE]: Deterministic world class stats report (byte-for-byte stable, no timestamps) over Seeds 1..100 using worldgen-era signals only; emits docs/generated/class_stats_report_v0.txt (gates: GATE.S2_5.WGEN.DISTINCTNESS.REPORT.*)
- EPIC.S2_5.WGEN.DISTINCTNESS.TARGETS.V0 [DONE]: Enforce class separation targets using report metrics; violations list seeds + deltas sorted; exits nonzero on failure (gates: GATE.S2_5.WGEN.DISTINCTNESS.TARGETS.*)
- EPIC.S3.RISK_MODEL.V0 [DONE]: Deterministic lane%route risk bands emit schema-bound SecurityEvents (delay, loss, inspection) with deterministic cause chains; surfaced in Station timeline; save%load preserved; no Slice 5 combat coupling (gates: GATE.S3.RISK_MODEL.*)
- EPIC.S3_5.CONTENT_PACK_CONTRACT.V0 [IN_PROGRESS]: Versioned registries (goods%recipes%modules) with schema validation, canonical hashing, deterministic load order (gates: GATE.X.CONTENT_SUBSTRATE.001, GATE.S3_5.CONTENT_SUBSTRATE.001)
- EPIC.S3_5.PACK_VALIDATION_REPORT.V0 [TODO]: Deterministic validation report with stable ordering and nonzero exit on invalid packs (gates: GATE.S3_5.CONTENT_SUBSTRATE.002)
- EPIC.S3_5.WORLD_BINDING.V0 [TODO]: World identity binds pack digest and persists through save%load; repro surface includes pack id%version (gates: GATE.S3_5.CONTENT_SUBSTRATE.003)
- EPIC.S3_5.HARDCODE_GUARD.V0 [TODO]: Deterministic scan or contract test flags new hardcoded content IDs in systems that must be data-driven; violations sorted and reproducible (gates: GATE.S3_5.CONTENT_SUBSTRATE.004)

Note: Slice tables below are informational. Canonical Epic Bullets drive scanning and next-gate selection.

### Cross-cutting epics (apply to all slices)
These epics are always-on dependencies. Each slice should reference them as needed and must not violate them.

Epics:
- EPIC.X.DETERMINISM: Deterministic ordering rules, stable IDs, canonical serialization, replayable outcomes
- EPIC.X.TWEAKS: Versioned tweak config with canonical hashing and deterministic injection; numeric-literal routing guard to enforce “knobs not constants”
- EPIC.X.CONTENT_SUBSTRATE: Registries + schemas + authoring tools + validators for goods%recipes%modules%weapons%tech%anomalies%factions%warfronts%story beats
- EPIC.X.WORLDGEN: Seed plumbing + deterministic world generation + invariant suites + injectors + seed explorer tooling (Civ-like procedural requirement)
- EPIC.X.UI_EXPLAINABILITY: Explain-events everywhere; UI must surface “why” (profit, loss, blocked, shifts, escalations)
- EPIC.X.PLAYER_LOOP_CONTRACT: “Greatness spec” player loops and non-negotiables (see below)

Greatness spec (non-negotiables, enforced by gates over time):
- Every major failure has an explainable cause chain surfaced in UI
- Every seed guarantees early viability (>= 3 viable early trade loops) and reachability (paths exist to industry, exploration, factions, endgame)
- Every major discovery introduces a new strategic option, not only numeric upgrades
- Warfront outcomes persist and reshape lane regimes, not just prices
- Automation feels competent: predictable, diagnosable, tunable (not dice)
- Explainability always maps to player actions (intervention verbs)
- Procedural worlds feel distinct by class (not just cosmetic differences)

Status: IN_PROGRESS (ALWAYS_ON discipline; do not mark DONE)

---

### Contracts (LOCKED, apply to all slices)
These are binding architectural rules. If any slice requires breaking a contract, update this section first and record the rationale in the session log.

#### CONTRACT.X.API_BOUNDARIES
Rules:
- SimCore owns truth. GameShell owns presentation. No exceptions.
- GameShell must not read SimCore entity graphs directly for UI. All UI reads go through SimBridge query contracts.
- SimCore must not depend on Godot types or UI constructs.
- SimCore exposes:
  - Facts: stable state snapshots intended for UI consumption (query outputs)
  - Events: explainable change narratives with cause chains (event stream)
- GameShell responsibilities:
  - Build view models from Facts%Events
  - Layout, navigation, input binding, rendering, audio, camera, moment-to-moment feel
- Adapters are the only allowed crossing point (SimBridge and other explicitly named adapter layers).

Acceptance proof:
- A deterministic guard exists ensuring UI code cannot access SimCore entity graphs directly (static scan enforced in tests; see GATE.X.API_BOUNDARIES.GUARD.001)
- All UI-facing reads are traceable to a SimBridge query contract

#### CONTRACT.X.EVENT_TAXONOMY
Events are the language of explainability. Keep the vocabulary small and stable.

Event categories (stable):
- EVT.MARKET.* (price update, shortage, surplus, trade execution)
- EVT.TRAVEL.* (route planned, lane blocked, delay incurred, arrival)
- EVT.PROGRAM.* (quote issued, job created, job failed, job completed)
- EVT.LOGISTICS.* (shipment created, shipment stalled, buffer underflow, buffer overflow)
- EVT.SECURITY.* (incident, interdiction, inspection, loss, salvage)
- EVT.DISCOVERY.* (anomaly found, hypothesis advanced, artifact contained, tech lead created)
- EVT.WARFRONT.* (theater created, state advanced, regime changed, objective captured)
- EVT.POLICING.* (trace threshold crossed, action taken, escalation phase change, counterplay success)
- EVT.PROJECT.* (construction started, stage advanced, blocker surfaced, stage completed)

Event schema requirements (all events):
- event_id (stable, deterministic)
- tick
- category
- subject_ids (stable IDs of primary entities)
- cause_ids (0..N references to prior events)
- summary (short)
- details (structured)
- severity (0..3)
- suggested_actions (0..N, optional, structured, see CONTRACT.X.INTERVENTION_VERBS)

Acceptance proof:
- Each new epic emits at least 1 new event that appears in an incident timeline
- Each slice has at least 1 failure mode with a traversable cause chain

#### CONTRACT.X.UI_INFORMATION_ARCHITECTURE
This is about data organization, not layout. Pages are stable even if UI layout changes.

Canonical pages (stable):
- Bridge (Throneroom): empire posture, tech posture, faction posture, warfront posture, trace posture, megaproject posture
- Station: market, inventory, services, contracts, local intel
- Fleet: fleet state, route, cargo, role%doctrine, incidents
- Programs: program list, quotes, execution timeline, outcomes, explain failures
- Logistics: shipments, buffers, bottlenecks, capacity, flow summaries
- Discoveries: anomaly catalog, hypotheses, artifacts, tech leads, expeditions
- Warfronts: theaters, supply needs, projected outcomes, regime map
- Policing%Trace: trace meters, escalation phase, actions, counterplay options
- Projects: construction pipelines, megaproject pipelines, blockers, time-to-capability

View model rule:
- Each page consumes only:
  - Facts returned from a SimBridge query (schema versioned)
  - Events from the event stream (schema versioned)
- No page may depend on internal SimCore classes directly.

Acceptance proof:
- Each new slice adds or extends at least 1 page schema and includes a minimal UI readout using Facts%Events
- A page-level schema exists for each canonical page (even if minimal)

#### CONTRACT.X.SLICE_COMPLETION
A slice is not DONE without player-facing proof and regression protection.

DONE requires:
- 1 forced playable path (mission or guided flow) that uses the new capability end-to-end
- 1 explainability path that answers “why” for a representative failure mode
- Regression suite updates:
- Golden replay updated or expanded
- If worldgen is touched: N-seed invariant suite passes (N = 100)
- If content schemas change: compatibility checks pass (old packs load or fail with explicit reasons)
- Evidence recorded in docs (gates moved, tests listed, artifacts referenced)

Also required:
- If UI is touched: GameShell smoke test passes (load minimal scene, run N ticks, exit; see GATE.X.GAMESHELL.SMOKE.001)
- If save or world identity is touched: save/load identity regression passes (see CONTRACT.X.SAVE_IDENTITY)

#### CONTRACT.X.INTERFACE_FREEZE_MILESTONES
Purpose: Preserve late-stage flexibility by stabilizing the right interfaces early.

Rules:
- Before Slice 3 starts: schemas and events may change, but must remain deterministic and versioned.
- After Slice 3 is marked DONE:
  - Additive-only for: Event categories, registry IDs, and page schemas for Bridge, Station, Fleet, Programs, Logistics
  - Breaking changes require: major version bump, explicit migration story, and session log entry
- After Slice 6 is marked DONE:
  - Additive-only for: Discoveries schemas (Discoveries page, anomaly families, artifact lead shapes)
  - Breaking changes require the same major version process
- After Slice 7 is marked DONE:
  - Additive-only for: Warfront and Territory regime schemas (Warfronts page, regime change events)
  - Breaking changes require the same major version process

Acceptance proof:
- A compatibility test exists for schema version loading and clear failure messages

#### CONTRACT.X.PACING_CONSTANTS
Purpose: Prevent late-stage rework by keeping time scales within designed ranges.

Rules:
- Declare pacing constants as ranges, not exact numbers, until Slice 9 lock.
- Each slice that changes pacing must update these ranges and add a regression test that asserts values remain within range.

Initial pacing ranges [TBD, tune later]:
- Early manual trade loop: 5 to 15 minutes per meaningful run
- Late automated trade cycle: 1 to 5 minutes per cycle per program (player review cadence)
- Warfront meaningful shift: 30 to 120 minutes of play
- Policing escalation phase: hours of play, not minutes
- Megaproject stage: 1 to 3 hours of play per stage (multiple stages per project)

Acceptance proof:
- At least 1 scenario sim test asserts that measured cycle times fall within current ranges [OPEN: measurement method]

#### CONTRACT.X.PERF_BUDGETS
Purpose: Prevent sim complexity from exploding unnoticed.

Rules:
- Each slice that adds simulation complexity must add or extend at least 1 performance test:
  - Either a tick-cost budget assertion, or a micro-benchmark for the new system
- Budgets start loose, tighten over time. Slice 9 locks final budgets.

Acceptance proof:
- A perf regression test exists and runs in CI-like local workflow (see GATE.S3.PERF_BUDGET.001)

#### CONTRACT.X.SAVE_IDENTITY
Purpose: Ensure procedural worlds are stable and replayable, and prevent save from becoming a Slice 9 surprise.

Rules:
- From Slice 2.5 onward, save must preserve world identity:
  - Save includes the seed and all generation-relevant parameters (world class, injectors config versions)
  - Load must reproduce the exact same world identity and determinism hashes
- Save schemas are versioned; breaking changes require migration story (even if minimal).
- Save/load hash equivalence is required for the deterministic subset of state.

Acceptance proof:
- A save/load identity regression test exists:
  - generate world with seed S
  - save
  - load
  - world hash matches and core invariants still hold

#### CONTRACT.X.DIFFICULTY_CURVES
Purpose: Difficulty is systemic and procedural, not enemy HP.

Rules:
- Difficulty modifies curves and budgets, not fundamental rules.
- Pressure sources (all must have tunable curves):
  - Security risk (incidents%loss rate)
  - Warfront intensity (demand and territorial volatility)
  - Policing escalation (trace thresholds and response rate)
  - Economic volatility (price dispersion and shock frequency)
- World class defines baseline curves; difficulty shifts within bounded ranges.

Acceptance proof:
- Seed-suite produces a “pressure profile report” per world class and per difficulty [OPEN: report format]
- No difficulty setting yields dead-on-arrival seeds (see CONTRACT.X.ONBOARDING_INVARIANTS)

#### CONTRACT.X.ONBOARDING_INVARIANTS
Purpose: Every seed must be playable and teach the loops.

Rules (must hold for all generated worlds):
- Starter region includes:
  - 1 stable hub station with basic services
  - >= 3 viable early trade loops within the starter region graph
  - At most 1 high-risk chokepoint on required M1 routes
  - Availability of required starter goods and basic ship loadout support
- Early threats exist but are legible and avoid unavoidable early losses.

Acceptance proof:
- N-seed invariant suite includes onboarding checks and reports failures with a minimizable seed repro

#### CONTRACT.X.ANTI_EXPLOIT
Purpose: Prevent trivial money printers while preserving satisfying arbitrage and scaling.

Rules:
- Any profitable loop at scale must be bounded by at least 2 friction sources from:
  - transport time, lane slots, inspections, spoilage, tariffs, information staleness, risk, capital lockup
- The game must preserve “small profitable runs” early and “portfolio management” late without a single dominant loop.
- Fixes should prefer adding friction and counterplay, not nerfing rewards into boredom.

Acceptance proof:
- Balance harness includes an “exploit sweep” scenario suite [OPEN: suite definition]
- No scenario shows unbounded growth without binding constraints

#### CONTRACT.X.MOD_SAFETY
Purpose: Keep determinism and stability while allowing content extensibility.

Rules:
- Content packs are data-only. No code execution in packs.
- Packs cannot override base registry IDs unless explicitly declared as total conversion [OPEN: whether total conversions are in scope]
- Packs must declare compatibility range for schema versions.
- Validators must reject unsafe or inconsistent packs with explicit errors.

Acceptance proof:
- Pack validator tests exist (good pack loads, bad pack rejects with clear message)

#### CONTRACT.X.GAMESHELL_SMOKE_TESTS
Purpose: Prevent GameShell drift while SimCore evolves.

Rules:
- Any slice that touches UI must include at least 1 headless GameShell smoke test:
  - load minimal scene
  - bind SimBridge
  - run N ticks
  - exit cleanly
- Smoke tests must be deterministic and run in the standard local workflow.

Acceptance proof:
- Smoke test runs in CI-like local script and is referenced in evidence

---

### Content waves (REQUIRED, keeps progress fun and validates systems)
Purpose: Prevent “infrastructure-only” slices and ensure player-facing proof exists continuously.

Rule:
- Each slice that introduces a new system must ship a minimal content wave that exercises it end-to-end.

Wave requirements (minimums, numbers are [TBD] but the existence is mandatory):
- Slice 1: Starter goods (>= 10), 2 stations, 1 lane, 1 basic ship loadout, 1 simple contract flow (M1)
- Slice 2: 1 program type (TradeProgram), 1 doctrine stub, 1 quote flow, 1 failure reason surfaced
- Slice 2.5: >= 3 world classes (CORE, FRONTIER, RIM), 3 to 5 factions, seed suite produces distinct early loops and onboarding validity
- Slice 3: 3 fleet roles (trader%hauler%patrol), multi-route choices, 1 congestion scenario, 1 bottleneck fix visible in UI, headless playable trade loop proof (incl save%load)
- Slice 4: 1 reverse-engineer chain (lead -> prototype), 1 manufacturing chain, 1 refit kit pipeline
- Slice 5: 1 weapon family, 1 counter family, 1 escort doctrine, 1 strategic resolver scenario
- Slice 6: >= 5 anomaly families, 1 extinct-tech lead family, 1 containment failure mode with counterplay
- Slice 7: >= 2 warfront theater types, 1 territory regime flip, 1 faction-unique tech gate
- Slice 8: >= 2 policing phases, >= 1 megaproject chain, >= 2 win scenarios wired into state machine
- Slice 9: final content expansion within locked constraints + balance targets

---

### Epic and gate template (REQUIRED for all new work)

#### Epic bullet format v1 (REQUIRED)
Every epic line must be machine-scannable and gate-derived.

- Format:
  - EPIC: `- EPIC.<...> [TODO|IN_PROGRESS|DONE]: <description> (gates: <selector or list>)`

- Examples:
  - `- EPIC.S2_5.SEEDS [DONE]: Seed plumbing everywhere (world, save/load, tests, tools) (gates: GATE.S2_5.SEEDS.*)`
  - `- EPIC.S3.LOGI.ROUTES [DONE]: Route planning + explainability (gates: GATE.ROUTE.*, GATE.S3.ROUTES.*)`
  - `- EPIC.X.CONTENT_SUBSTRATE [TODO]: Content substrate v0 (gates: GATE.X.CONTENT_SUBSTRATE.*)`

- Status computation (no exceptions if a gates selector exists):
  - DONE: all matched gates are DONE
  - IN_PROGRESS: some matched gates are DONE or IN_PROGRESS, but not all DONE
  - TODO: all matched gates are TODO
  - If no `(gates: ...)` is present, the epic cannot be marked DONE.

- OPEN items rule:
  - Anything that blocks DONE must be represented by a gate and included in `(gates: ...)`.
  - Otherwise it must be moved to a different epic or explicitly declared non-blocking.

#### Gate naming
- Preferred: `GATE.<slice_or_domain>.<topic>.<NNN>` where NNN is 001, 002, ...
- Rule: a gate must belong to at least 1 epic via an epic `(gates: ...)` selector.
  - If you create a new gate prefix, also add or update the owning epic selector.

#### Every gate must include (minimum metadata)
- Scope: smallest meaningful vertical slice
- Files: expected touched paths
- Tests: at least 1 new or expanded test
- Evidence: objective completion proof (test filter, artifact path, deterministic transcript, screenshot if applicable)
- Determinism notes: ordering%IDs%serialization%tie-break rules
- Failure mode: 1 explicit failure and how it is exposed (Facts%Events or scan output)
- Intervention verbs: what the player can do about it (see CONTRACT.X.INTERVENTION_VERBS)

#### Standard 5-gate decomposition (default)
1) CONTRACT gate
   - Schema/query contract and event types
2) CORE LOGIC gate
   - Minimal SimCore behavior
3) DETERMINISM gate
   - Stable IDs%tie-breaks%serialization%golden replay coverage
4) UI gate
   - Minimal readout using Facts%Events (no layout polish)
5) EXPLAIN gate
   - Cause chain + suggested actions surfaced for the failure mode

If any gate exceeds caps, split it.

#### Caps (hard limits for a single gate)
- Net change <= 500 lines (measured by `git diff --stat`)
- New tests <= 3
- New schemas <= 1 version bump
- New content packs <= 1 starter or incremental pack

#### Acceptance proof for a gate
A gate is DONE only if all are true:
- Proof command passes
  - `dotnet test` passes (filtered ok for the gate, but the final gate closing an epic requires full suite)
- Gate ledger updated
  - `55_GATES.md` row set to DONE with proof command and evidence paths
- Session log appended
  - `56_SESSION_LOG.md` includes a PASS entry for the gate
- Epic status stays consistent
  - Epic `(gates: ...)` selector would compute DONE/IN_PROGRESS/TODO matching the epic marker
- Connectivity violations remain empty for slice scope

#### CONTRACT.X.INTERVENTION_VERBS (binding list, extend additively)
Purpose: Explainability must always connect to player agency.

Rules:
- Each explain chain must map to 1 to 3 intervention verbs that are available on a relevant canonical page.
- Verbs are coarse, policy-driven, and program-centric (no per-ship micromanagement leaks).

Initial verb set (extend additively):
- Programs: raise budget, lower budget, pause program, resume program, change doctrine toggle, change risk tolerance, change route preference
- Logistics: reprioritize shipment class, allocate capacity to route, reroute around chokepoint, schedule convoy window
- Industry: queue build stage, queue refit, build depot, build shipyard stage, allocate science throughput
- Discoveries: run expedition, escalate containment, run analysis step, defer exploitation, mark as hazard
- Warfronts: commit supply package, commit escort package, change faction alignment stance, negotiate access
- Policing: run counterplay action, reduce fracture usage policy, deploy scrubber project stage, misdirect [OPEN: in-scope set]

Acceptance proof:
- For each slice, at least 1 failure mode presents a suggested action that is executable via a UI control

---

### Slice 0: Repo + determinism foundation (pre-slice, always on)
Purpose: Make LLM-driven development safe, deterministic, and boundary-respecting.

Epics:
- EPIC.S0.TOOLING: DevTool commands for repeatable workflows (packets, scans, test runs)
- EPIC.S0.DETERMINISM: Golden hashes, replay harness, stable world hashing
- EPIC.S0.CONNECTIVITY: Connectivity scanner and zero-violation policy for Slice scope
- EPIC.S0.QUALITY: Minimal CI-like local scripts (format, build, tests)
- EPIC.S0.EVIDENCE: Context packet must include or reference latest scan + test + hash artifacts
- EPIC.S0.REPO_HEALTH: One-command repo health runner enforcing generated hygiene, forbidden artifact policy, LLM size budgets, and connectivity delta discipline

Exit criteria for DONE:
- Context packet reliably surfaces scan + test + determinism evidence, or explicitly reports why missing
- Connectivity violations remain empty for current slice scope
- Golden replay + long-run + save/load determinism regressions are stable

Status: IN_PROGRESS (ALWAYS_ON discipline; do not mark DONE. New invariants and boundaries will continue to be added over time.)

---

### Slice 1 (LOCKED): Logistics + Market + Intel micro-world
Purpose: Prove the core economic simulation loop in a tiny world, deterministically, with minimal UI.

Gates: see docs/55_GATES.md (source of truth)
Status: DONE

---

### Slice 1.5 (LOCKED): Tech sustainment via supply chain
Purpose: Prove “industry enablement depends on supply” with clear failure modes and UI.

Gates: see docs/55_GATES.md (source of truth)
Status: DONE

---

### Slice 2: Programs as the primary player control surface
Purpose: Shift player power from manual micromanagement to programs, quotes, doctrines.

v1 scope (LOCK ONCE SLICE 2 STARTS):
- One program type: TradeProgram only
- One fleet binding: single trader fleet only
- One doctrine: DefaultDoctrine only (max 2 toggles if needed)
- No mining, patrol, construction, staffing, or multi-route automation in Slice 2

Epics:
- EPIC.S2.PROG.MODEL [DONE]: Program, Fleet, Doctrine core models align to docs/53 (gates: GATE.PROG.001, GATE.FLEET.001, GATE.DOCTRINE.001)
- EPIC.S2.PROG.QUOTE [DONE]: Liaison Quote flow for “do X”, cost, time, risks, constraints (gates: GATE.QUOTE.001)
- EPIC.S2.PROG.EXEC [DONE]: Program execution pipeline (intent-driven, deterministic) (gates: GATE.PROG.EXEC.001, GATE.PROG.EXEC.002)
- EPIC.S2.PROG.UI [DONE]: Control surface UI for creating programs and reading outcomes (gates: GATE.UI.PROG.001, GATE.VIEW.001)
- EPIC.S2.PROG.SAFETY [DONE]: Guardrails against direct state mutation, only intents (gates: GATE.BRIDGE.PROG.001, GATE.PROG.EXEC.001)
- EPIC.S2.EXPLAIN [DONE]: Schema-bound “Explain” events for program outcomes and constraints (gates: GATE.EXPLAIN.001)

Status: DONE

---

### Slice 2.5: Worldgen foundations (Civ-like procedural requirement)
Purpose: Procedural galaxy%economy%factions become real and testable, not just anomalies.

Epics:
- EPIC.S2_5.SEEDS [DONE]: Seed plumbing everywhere (world, save/load, tests, tools) (gates: GATE.S2_5.SEEDS.*)
- EPIC.S2_5.WGEN.GALAXY.V0 [DONE]: Topology, lanes, chokepoints, capacities, regimes; starter safe region (gates: GATE.S2_5.WGEN.GALAXY.001)
- EPIC.S2_5.WGEN.ECON.V0 [DONE]: Role distribution, recipe placement, demand sinks, initial inventories; early loop guarantees (gates: GATE.S2_5.WGEN.ECON.001)
- EPIC.S2_5.WGEN.FACTION.V0 [DONE]: 3 to 5 factions, home regions, doctrines, initial relations (gates: GATE.S2_5.WGEN.FACTION.001)
- EPIC.S2_5.WGEN.WORLD_CLASSES.V0 [DONE]: World classes v0 implemented (CORE, FRONTIER, RIM) with deterministic assignment and measurable effect (fee_multiplier) (gates: GATE.S2_5.WGEN.WORLD_CLASSES.001)
- EPIC.S2_5.WGEN.INVARIANTS [DONE]: Connectivity, early viability, reachability, onboarding invariants (gates: GATE.S2_5.WGEN.INVARIANTS.001)
- EPIC.S2_5.WGEN.N_SEED_TESTS [DONE]: Distribution bounds over N seeds (v0 uses N = 100; can increase later) (gates: GATE.S2_5.WGEN.DISTRIBUTION.001, GATE.S2_5.WGEN.NSEED.001)
- EPIC.S2_5.WGEN.DISTINCTNESS.REPORT.V0 [DONE]: Deterministic seed-suite stats report for class differences using worldgen-only signals (gates: GATE.S2_5.WGEN.DISTINCTNESS.REPORT.*)
- EPIC.S2_5.WGEN.DISTINCTNESS.TARGETS.V0 [TODO]: Thresholds proving class separation on worldgen-only metrics, enforced in N-seed suite (gates: GATE.S2_5.WGEN.DISTINCTNESS.TARGETS.*)
- EPIC.S2_5.SAVE_IDENTITY [DONE]: Save seed%params, load exact identity, hash equivalence regression (gates: GATE.S2_5.SAVELOAD.WORLDGEN.001)
- EPIC.S2_5.TOOL.SEED_EXPLORER [DONE]: Preview, diff, invariant failure drill-down (gates: GATE.S2_5.TOOL.SEED_EXPLORER.001)

Status: IN_PROGRESS

---

### Slice 3: Fleet automation and logistics scaling
Purpose: Multi-route trade, hauling, and supply operations at scale without micromanagement.

Epics:
- EPIC.S3.LOGI.ROUTES [DONE]: Route planning primitives (multi-candidate, stable tie-breaks) (gates: GATE.ROUTE.001, GATE.S3.ROUTES.001)
- EPIC.S3.LOGI.EXEC [DONE]: Logistics job model and execution pipeline (cargo, xfer, reserve, fulfill, cancel, determinism, save%load) (gates: GATE.LOGI.*, GATE.FLEET.ROUTE.001)
- EPIC.S3.FLEET_ROLES [DONE]: Fleet roles and constraints (trader, hauler, patrol) that deterministically influence route-choice selection (gates: GATE.S3.FLEET.ROLES.001)
- EPIC.S3.MARKET_ARB [DONE]: Automation that exploits spreads but is not money-printing (anti-exploit constraints enforced) (gates: GATE.S3.MARKET_ARB.001)
- EPIC.S3.RISK_SINKS.V0 [TODO]: Predictable risk frictions for automation (delays%losses%insurance-like sinks) without requiring Slice 5 combat (gates: GATE.S3.RISK_SINKS.*)
- EPIC.S3.CAPACITY_SCARCITY [DONE]: Lane slot scarcity model (queueing v0) (gates: GATE.S3.CAPACITY_SCARCITY.001)
- EPIC.S3.UI_DASH [DONE]: Dashboards for flows, margins, bottlenecks, intel quality (gates: GATE.S3.UI.DASH.001)
- EPIC.S3.UI_LOGISTICS [DONE]: Logistics UI readout and incident timeline (Facts%Events, deterministic ordering) (gates: GATE.UI.LOGISTICS.001, GATE.UI.LOGISTICS.EVENT.001)
- EPIC.S3.UI_FLEET [DONE]: Fleet UI playability surface (select, cancel job, override, save%load visible state, deterministic event tail) (gates: GATE.UI.FLEET.*, GATE.UI.FLEET.PLAY.001)
- EPIC.S3.EXPLAINABILITY [DONE]: Explain capstone plus cross-surface “why” chains for representative failures (gates: GATE.UI.EXPLAIN.PLAY.001, GATE.UI.PROGRAMS.001, GATE.UI.PROGRAMS.EVENT.001, GATE.PROG.UI.001, GATE.UI.DOCK.NONSTATION.001)
- EPIC.S3.PERF_BUDGET [DONE]: Tick budget tests extended for logistics scaling (gates: GATE.S3.PERF_BUDGET.001)
- EPIC.S3.PLAY_LOOP_PROOF [DONE]: Headless playable trade loop proof, including deterministic save%load continuation (gates: GATE.UI.PLAY.TRADELOOP.001, GATE.UI.PLAY.TRADELOOP.SAVELOAD.001, GATE.S3.SAVELOAD.SCALING.001)

Status: IN_PROGRESS

---

### Slice 3.5: Content substrate foundations (prereq for Slice 4+)
Purpose: Prevent hardcoded content. Establish deterministic registries%schemas%validators%minimal authoring loop.

Epics:
- EPIC.S3_5.CONTENT_SUBSTRATE.V0 [DONE]: Registries + schema loading + validation + deterministic ordering%IDs for authored packs (gates: GATE.X.CONTENT_SUBSTRATE.*)

Status: DONE

---

### Slice 4: Industry, construction, and technology industrialization
Purpose: Convert discoveries into sustainable capability via supply-bound projects.

Dependency:
- Slice 4 requires Slice 3.5 content substrate DONE.

Epics:
- EPIC.S4.INDU_STRUCT [TODO]: Production chains beyond 2 goods, bounded complexity, bounded byproducts (gates: GATE.S4.INDU_STRUCT.*)
- EPIC.S4.CONSTR_PROG [TODO]: Construction programs (depots, shipyards, refineries, science centers) (gates: GATE.S4.CONSTR_PROG.*)
- EPIC.S4.MAINT_SUSTAIN [TODO]: Maintenance as sustained supply (no repair minigame) (gates: GATE.S4.MAINT_SUSTAIN.*)
- EPIC.S4.TECH_INDUSTRIALIZE [TODO]: Reverse engineering pipeline (lead -> prototype -> manufacturable) (gates: GATE.S4.TECH_INDUSTRIALIZE.*)
- EPIC.S4.UPGRADE_PIPELINE [TODO]: Refit kits, install queues, yard capacity, time costs (gates: GATE.S4.UPGRADE_PIPELINE.*)
- EPIC.S4.UI_INDU [TODO]: Dependency graphs, time-to-capability, “why blocked” and “what to build next” (gates: GATE.S4.UI_INDU.*)
- EPIC.S4.NPC_INDU [TODO]: NPC industry reacts to incentives and war demand (gates: GATE.S4.NPC_INDU.*)
- EPIC.S4.PERF_BUDGET [TODO]: Tick budget tests extended for industry (gates: GATE.S4.PERF_BUDGET.*)

Status: TODO

---

### Slice 5: Security and combat (local real-time + strategic resolution)
Purpose: Force matters economically, hero combat is real, fleets resolve at scale.

Default coupling rule (until overridden by an explicit gate):
- Local combat primarily affects: tactical incidents, local security posture, salvage, short-term access
- Strategic warfront outcomes primarily move via: supply delivery and the strategic resolver
- If local combat creates strategic impact, it must be via explicit events and bounded effects (no continuous hidden coupling)

Epics:
- EPIC.S5.SECURITY_LANES [TODO]: Risk, delay, inspections, insurance sinks, lane regimes (gates: GATE.S5.SECURITY_LANES.*)
- EPIC.S5.COMBAT_LOCAL [TODO]: Hero ship turret%missile combat in-bubble, deterministic input replay (gates: GATE.S5.COMBAT_LOCAL.*)
- EPIC.S5.COMBAT_RESOLVE [TODO]: Deterministic strategic resolver (attrition, outcomes, salvage) (gates: GATE.S5.COMBAT_RESOLVE.*)
- EPIC.S5.ESCORT_PROG [TODO]: Escort, patrol, interdiction, convoy programs (policy-driven) (gates: GATE.S5.ESCORT_PROG.*)
- EPIC.S5.LOSS_RECOVERY [TODO]: Salvage, capture, replacement pipelines tied to industry (gates: GATE.S5.LOSS_RECOVERY.*)
- EPIC.S5.UI_SECURITY [TODO]: Threat maps, convoy planning, incident timelines, “why we lost” explain chains (gates: GATE.S5.UI_SECURITY.*)
- EPIC.S5.COUPLING_LIMITS.V0 [TODO]: Explicit bounded coupling limits and event contracts for local -> strategic influence (gates: GATE.S5.COUPLING_LIMITS.*)
- EPIC.S5.PERF_BUDGET [TODO]: Tick budget tests extended for security systems (gates: GATE.S5.PERF_BUDGET.*)

Status: TODO

---

### Slice 6: Exploration, anomalies, extinct infrastructure, artifact tech
Purpose: Crazy discoveries create leverage and new strategies, feeding industry.

Epics:
- EPIC.S6.MAP_GALAXY [TODO]: Navigation, discovery state, bookmarking, expedition planning (gates: GATE.S6.MAP_GALAXY.*)
- EPIC.S6.OFFLANE_FRACTURE [TODO]: Fracture travel rules, risk bands, stable discovery markers, trace generation (gates: GATE.S6.OFFLANE_FRACTURE.*)
- EPIC.S6.ANOMALY_ECOLOGY [TODO]: Procedural anomaly distribution with deterministic seeds and spatial logic (gates: GATE.S6.ANOMALY_ECOLOGY.*)
- EPIC.S6.DISCOVERY_OUTCOMES [TODO]: Persistent value outputs (intel, resources, artifacts, maps, leads) (gates: GATE.S6.DISCOVERY_OUTCOMES.*)
- EPIC.S6.ARTIFACT_RESEARCH [TODO]: Identification, containment, experiments, failure modes (trace spikes, incidents) (gates: GATE.S6.ARTIFACT_RESEARCH.*)
- EPIC.S6.TECH_LEADS [TODO]: Tech leads become prototype candidates, gated by science throughput (gates: GATE.S6.TECH_LEADS.*)
- EPIC.S6.EXPEDITION_PROG [TODO]: Survey, salvage, escort exploration programs (gates: GATE.S6.EXPEDITION_PROG.*)
- EPIC.S6.SCIENCE_CENTER [TODO]: Analysis throughput, reverse engineering gates, special material handling (gates: GATE.S6.SCIENCE_CENTER.*)
- EPIC.S6.UI_DISCOVERY [TODO]: Anomaly catalog, hypothesis%verification UI, “next action to advance” hints (gates: GATE.S6.UI_DISCOVERY.*)
- EPIC.S6.CLASS_DISCOVERY_PROFILES.V0 [TODO]: World class influences discovery families and outcomes (integrates Slice 2.5 classes with Slice 6) (gates: GATE.S6.CLASS_DISCOVERY_PROFILES.*)
- EPIC.S6.MYSTERY_MARKERS.V0 [TODO]: Mystery style policy and UI contracts (systemic mystery vs explicit markers) (gates: GATE.S6.MYSTERY_MARKERS.*)
- EPIC.S6.PERF_BUDGET [TODO]: Tick budget tests extended for exploration systems (gates: GATE.S6.PERF_BUDGET.*)

Status: TODO

---

### Slice 7: Factions, warfronts, governance, and map change
Purpose: Logistics shapes wars and the galaxy’s political topology, with lasting consequences.

Epics:
- EPIC.S7.FACTION_MODEL [TODO]: Goals, doctrines, policies, constraints, tech preferences (gates: GATE.S7.FACTION_MODEL.*)
- EPIC.S7.WARFRONT_THEATERS [TODO]: Procedural warfront seeding from geography and faction goals (gates: GATE.S7.WARFRONT_THEATERS.*)
- EPIC.S7.WARFRONT_STATE [TODO]: Front lines, objectives, supply demand, attrition, morale, stability (gates: GATE.S7.WARFRONT_STATE.*)
- EPIC.S7.SUPPLY_IMPACT [TODO]: Delivered goods and services move warfront state with persistent consequences (gates: GATE.S7.SUPPLY_IMPACT.*)
- EPIC.S7.TERRITORY_REGIMES [TODO]: Permissions, tariffs, embargoes, inspections, closures; hysteresis rules (gates: GATE.S7.TERRITORY_REGIMES.*)
- EPIC.S7.TECH_ACCESS [TODO]: Exclusives, embargoed tech, licensing, doctrine-based variants (gates: GATE.S7.TECH_ACCESS.*)
- EPIC.S7.DIPLOMACY_VERBS.V0 [TODO]: Diplomacy verbs set definition and contracts (treaties%bounties%sanctions%privateering) (gates: GATE.S7.DIPLOMACY_VERBS.*)
- EPIC.S7.REPUTATION_INFLUENCE [TODO]: Reputation drives access, pricing, inspection posture, and deal availability (gates: GATE.S7.REPUTATION_INFLUENCE.*)
- EPIC.S7.UI_DIPLO [TODO]: Faction intel, deal making, “why policy changed” (gates: GATE.S7.UI_DIPLO.*)
- EPIC.S7.UI_WARFRONT [TODO]: Dashboards, projected outcomes, intervention options, supply checklists (gates: GATE.S7.UI_WARFRONT.*)
- EPIC.S7.BRIDGE_THRONEROOM_V0 [TODO]: Bridge layer as strategic view + unlock surface tied to factions%warfronts%tech posture (gates: GATE.S7.BRIDGE_THRONEROOM_V0.*)
- EPIC.S7.CLASS_WARFRONT_PROFILES.V0 [TODO]: World class influences warfront seeding and supply shapes (integrates Slice 2.5 classes with Slice 7) (gates: GATE.S7.CLASS_WARFRONT_PROFILES.*)
- EPIC.S7.PERF_BUDGET [TODO]: Tick budget tests extended for warfront systems (gates: GATE.S7.PERF_BUDGET.*)

Status: TODO

---

### Slice 8: Fracture policing, existential pressure, megaproject endgames
Purpose: Lane builders police fracture; pressure escalates; win via massive supply-bound projects under multiple scenarios.

Epics:
- EPIC.S8.POLICING_SIM [TODO]: Trace-driven escalation model, legible actions, counterplay verbs (gates: GATE.S8.POLICING_SIM.*)
- EPIC.S8.THREAT_IMPACT [TODO]: Supply shocks, lane disruption, interdiction waves, faction realignment (gates: GATE.S8.THREAT_IMPACT.*)
- EPIC.S8.PLAYER_COUNTERPLAY.V0 [TODO]: Counter-programs, corridor hardening, trace scrubbers, misdirection (gates: GATE.S8.PLAYER_COUNTERPLAY.*)
- EPIC.S8.MEGAPROJECT_SET.V0 [TODO]: Canonical megaproject set and their rule changes (anchors, stabilizers, pylons, corridors) (gates: GATE.S8.MEGAPROJECT_SET.*)
- EPIC.S8.MEGAPROJECTS [TODO]: Multi-stage projects that reshape map rules under supply constraints (gates: GATE.S8.MEGAPROJECTS.*)
- EPIC.S8.WIN_SCENARIOS [TODO]: Multiple scenario wins (containment, alliance, dominance, escape, reconciliation) + explicit loss states (gates: GATE.S8.WIN_SCENARIOS.*)
- EPIC.S8.UI_WARROOM [TODO]: Warfronts + policing + megaproject pipelines + bottlenecks (gates: GATE.S8.UI_WARROOM.*)
- EPIC.S8.STORY_STATE_MACHINE [TODO]: Story beats via discovery%trace%warfront phases, not timed missions (gates: GATE.S8.STORY_STATE_MACHINE.*)
- EPIC.S8.BRIDGE_THRONEROOM_V1 [TODO]: Endgame readiness, scenario selection, empire posture surface (gates: GATE.S8.BRIDGE_THRONEROOM_V1.*)
- EPIC.S8.PERF_BUDGET [TODO]: Tick budget tests extended for endgame pressure systems (gates: GATE.S8.PERF_BUDGET.*)

Status: TODO

---

### Slice 9: Polish, UX hardening, and mod hooks
Purpose: Make it shippable and extendable without breaking determinism.

Epics:
- EPIC.S9.SAVE [TODO]: Robust save UX, migrations, corruption handling (lock final migration policy) (gates: GATE.S9.SAVE.*)
- EPIC.S9.UI [TODO]: Information architecture cleanup, tooltips, clarity passes, onboarding guidance (gates: GATE.S9.UI.*)
- EPIC.S9.MOD [TODO]: Content packs, compatibility rules, safe mod surface, validation tooling (gates: GATE.S9.MOD.*)
- EPIC.S9.PERF [TODO]: Performance profiling, tick cost budgets, memory budgets (lock final budgets) (gates: GATE.S9.PERF.*)
- EPIC.S9.ACCESS.V0 [TODO]: Basic accessibility and input configuration (gates: GATE.S9.ACCESS.*)
- EPIC.S9.BALANCE_LOCK.V0 [TODO]: Tuning targets and regression bounds locked (gates: GATE.S9.BALANCE_LOCK.*)
- EPIC.S9.CONTENT_WAVES [TODO]: Final archetype families, world classes, endgame megaproject variety (gates: GATE.S9.CONTENT_WAVES.*)

Status: TODO
