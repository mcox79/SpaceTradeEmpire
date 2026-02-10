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

## A. Slice map (Layer 1)

### Cross-cutting epics (apply to all slices)
These epics are always-on dependencies. Each slice should reference them as needed and must not violate them.

Epics:
- EPIC.X.DETERMINISM: Deterministic ordering rules, stable IDs, canonical serialization, replayable outcomes
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
- A guard exists ensuring UI code cannot access SimCore entity graphs directly [OPEN: enforcement mechanism, prefer static]
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
  - If worldgen is touched: N-seed invariant suite passes (N >= 100) [TBD: N]
  - If content schemas change: compatibility checks pass (old packs load or fail with explicit reasons)
- Evidence recorded in docs (gates moved, tests listed, artifacts referenced)

Also required:
- If UI is touched: GameShell smoke test passes (load minimal scene, run N ticks, exit) [OPEN: harness]
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
- A perf regression test exists and runs in CI-like local workflow [OPEN: exact harness]

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
- Slice 2.5: >= 3 world classes [OPEN], 3 to 5 factions, seed suite produces distinct early loops and onboarding validity
- Slice 3: 2 fleet roles (hauler%trader), 2 routes, 1 congestion scenario, 1 bottleneck fix visible in UI
- Slice 4: 1 reverse-engineer chain (lead -> prototype), 1 manufacturing chain, 1 refit kit pipeline
- Slice 5: 1 weapon family, 1 counter family, 1 escort doctrine, 1 strategic resolver scenario
- Slice 6: >= 5 anomaly families, 1 extinct-tech lead family, 1 containment failure mode with counterplay
- Slice 7: >= 2 warfront theater types, 1 territory regime flip, 1 faction-unique tech gate
- Slice 8: >= 2 policing phases, >= 1 megaproject chain, >= 2 win scenarios wired into state machine
- Slice 9: final content expansion within locked constraints + balance targets

---

### Epic gate template (REQUIRED for all new work)
All epics must be decomposed into gates. A gate is sized to fit into an LLM execution chunk and must produce objective proof.

Gate naming:
- GATE.<EPIC>.<NNN> where NNN is 001, 002, ...

Every gate must include:
- Scope: the smallest meaningful vertical slice
- Files: expected touched paths
- Tests: at least 1 new or expanded test
- Evidence: how to prove completion (test name, artifact, screenshot)
- Determinism notes: ordering%IDs%serialization considerations
- Failure mode: 1 explicit failure case and how it is explained in UI
- Intervention verbs: what the player can do about it (see CONTRACT.X.INTERVENTION_VERBS)

Standard 5-gate decomposition for most epics:
1) CONTRACT gate
   - Add or update schema/query contract and event types needed
2) CORE LOGIC gate
   - Implement minimal SimCore behavior for the capability
3) DETERMINISM gate
   - Tie-breaks, stable IDs, deterministic serialization, golden replay coverage
4) UI gate
   - Minimal page readout using Facts%Events (no layout polish required)
5) EXPLAIN gate
   - Cause chain and suggested actions surfaced for a failure mode

Caps (hard limits for a single gate):
- Net change <= 500 lines [TBD: adjust if too tight]
- New tests <= 3
- New schemas <= 1 version bump
- New content packs <= 1 starter or incremental pack
If a gate would exceed caps, split it.

Acceptance proof for a gate:
- `dotnet test` passes (filtered if needed, but final gate of an epic requires full suite)
- Evidence updated (doc line moved, tests referenced)
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

Exit criteria for DONE:
- Context packet reliably surfaces scan + test + determinism evidence, or explicitly reports why missing
- Connectivity violations remain empty for current slice scope
- Golden replay + long-run + save/load determinism regressions are stable

Status: IN_PROGRESS (ALWAYS_ON discipline; do not mark DONE. New invariants and boundaries will continue to be added over time.)

---

### Slice 1 (LOCKED): Logistics + Market + Intel micro-world
Purpose: Prove the core economic simulation loop in a tiny world, deterministically, with minimal UI.

Gates: (tracked in section B)
Status: DONE

---

### Slice 1.5 (LOCKED): Tech sustainment via supply chain
Purpose: Prove “industry enablement depends on supply” with clear failure modes and UI.

Gates: (tracked in section B)
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
- EPIC.S2.PROG.MODEL: Program, Fleet, Doctrine core models align to docs/53
- EPIC.S2.PROG.QUOTE: Liaison Quote flow for “do X”, cost, time, risks, constraints
- EPIC.S2.PROG.EXEC: Program execution pipeline (intent-driven, deterministic)
- EPIC.S2.PROG.UI: Control surface UI for creating programs and reading outcomes
- EPIC.S2.PROG.SAFETY: Guardrails against direct state mutation, only intents
- EPIC.S2.EXPLAIN: Schema-bound “Explain” events for program outcomes and constraints

Status: DONE

---

### Slice 2.5: Worldgen foundations (Civ-like procedural requirement)
Purpose: Procedural galaxy%economy%factions become real and testable, not just anomalies.

Epics:
- EPIC.S2_5.SEEDS: Seed plumbing everywhere (world, save/load, tests, tools)
- EPIC.S2_5.WGEN.GALAXY.V0: Topology, lanes, chokepoints, capacities, regimes; starter safe region
- EPIC.S2_5.WGEN.ECON.V0: Role distribution, recipe placement, demand sinks, initial inventories; early loop guarantees
- EPIC.S2_5.WGEN.FACTION.V0: 3 to 5 factions, home regions, doctrines, initial relations
- EPIC.S2_5.WGEN.WORLD_CLASSES: World classes for distinct strategic feel [OPEN: final class set + distinctiveness criteria]
- EPIC.S2_5.WGEN.INVARIANTS: Connectivity, early viability, reachability, risk gradients, onboarding invariants
- EPIC.S2_5.WGEN.N_SEED_TESTS: Distribution bounds across many seeds (100 to 1000) [TBD: exact N]
- EPIC.S2_5.WGEN.DISTINCTNESS_GATE: Each world class must have:
  - at least 1 dominant constraint (eg choke scarcity vs sparse lanes vs hostile regimes)
  - at least 1 distinct discovery%warfront profile
  - a seed-suite stats report proving class differences [OPEN: report format]
- EPIC.S2_5.SAVE_IDENTITY: Save seed%params, load exact identity, hash equivalence regression
- EPIC.S2_5.TOOL.SEED_EXPLORER: Preview, diff, invariant failure drill-down

Status: TODO

---

### Slice 3: Fleet automation and logistics scaling
Purpose: Multi-route trade, hauling, and supply operations at scale without micromanagement.

Epics:
- EPIC.S3.LOGI.ROUTES: Route planning and lane scheduling primitives
- EPIC.S3.FLEET_ROLES: Fleet compositions and role constraints (miner, hauler, escort)
- EPIC.S3.MARKET_ARB: Automation that exploits spreads but is not money-printing (anti-exploit constraints enforced)
- EPIC.S3.RISK_MODEL: Predictable risk bands, losses, insurance-like sinks
- EPIC.S3.UI_DASH: Dashboards for flows, margins, bottlenecks, intel quality
- EPIC.S3.CAPACITY_SCARCITY: Lane slot scarcity model [OPEN: booking vs queuing vs hybrid]
- EPIC.S3.PERF_BUDGET: Tick budget tests extended for logistics scaling

Status: IN_PROGRESS

---

### Slice 4: Industry, construction, and technology industrialization
Purpose: Convert discoveries into sustainable capability via supply-bound projects.

Epics:
- EPIC.S4.INDU_STRUCT: Production chains beyond 2 goods, bounded complexity, byproducts bounded [OPEN]
- EPIC.S4.CONSTR_PROG: Construction programs (depots, shipyards, refineries, science centers)
- EPIC.S4.MAINT_SUSTAIN: Maintenance as sustained supply (no repair minigame)
- EPIC.S4.TECH_INDUSTRIALIZE: Reverse engineering pipeline (lead -> prototype -> manufacturable)
- EPIC.S4.UPGRADE_PIPELINE: Refit kits, install queues, yard capacity, time costs
- EPIC.S4.UI_INDU: Dependency graphs, time-to-capability, “why blocked” and “what to build next”
- EPIC.S4.NPC_INDU: NPC industry reacts to incentives and war demand
- EPIC.S4.PERF_BUDGET: Tick budget tests extended for industry

Status: TODO

---

### Slice 5: Security and combat (local real-time + strategic resolution)
Purpose: Force matters economically, hero combat is real, fleets resolve at scale.

Default coupling rule (until overridden by an explicit gate):
- Local combat primarily affects: tactical incidents, local security posture, salvage, short-term access
- Strategic warfront outcomes primarily move via: supply delivery and the strategic resolver
- If local combat creates strategic impact, it must be via explicit events and bounded effects (no continuous hidden coupling)

Epics:
- EPIC.S5.SECURITY_LANES: Risk, delay, inspections, insurance sinks, lane regimes
- EPIC.S5.COMBAT_LOCAL: Hero ship turret%missile combat in-bubble, deterministic input replay
- EPIC.S5.COMBAT_RESOLVE: Deterministic strategic resolver (attrition, outcomes, salvage)
- EPIC.S5.ESCORT_PROG: Escort, patrol, interdiction, convoy programs (policy-driven)
- EPIC.S5.LOSS_RECOVERY: Salvage, capture, replacement pipelines tied to industry
- EPIC.S5.UI_SECURITY: Threat maps, convoy planning, incident timelines, “why we lost” explain chains
- EPIC.S5.COUPLING_RULES: Local combat influence on strategic outcomes [OPEN: coupling limits, must remain bounded]
- EPIC.S5.PERF_BUDGET: Tick budget tests extended for security systems

Status: TODO

---

### Slice 6: Exploration, anomalies, extinct infrastructure, artifact tech
Purpose: Crazy discoveries create leverage and new strategies, feeding industry.

Epics:
- EPIC.S6.MAP_GALAXY: Navigation, discovery state, bookmarking, expedition planning
- EPIC.S6.OFFLANE_FRACTURE: Fracture travel rules, risk bands, stable discovery markers, trace generation
- EPIC.S6.ANOMALY_ECOLOGY: Procedural anomaly distribution with deterministic seeds and spatial logic
- EPIC.S6.DISCOVERY_OUTCOMES: Persistent value outputs (intel, resources, artifacts, maps, leads)
- EPIC.S6.ARTIFACT_RESEARCH: Identification, containment, experiments, failure modes (trace spikes, incidents)
- EPIC.S6.TECH_LEADS: Tech leads become prototype candidates, gated by science throughput
- EPIC.S6.EXPEDITION_PROG: Survey, salvage, escort exploration programs
- EPIC.S6.SCIENCE_CENTER: Analysis throughput, reverse engineering gates, special material handling
- EPIC.S6.UI_DISCOVERY: Anomaly catalog, hypothesis%verification UI, “next action to advance” hints
- EPIC.S6.MYSTERY_MARKERS: Mystery style [OPEN: systemic mystery vs explicit quest markers]
- EPIC.S6.PERF_BUDGET: Tick budget tests extended for exploration systems

Status: TODO

---

### Slice 7: Factions, warfronts, governance, and map change
Purpose: Logistics shapes wars and the galaxy’s political topology, with lasting consequences.

Epics:
- EPIC.S7.FACTION_MODEL: Goals, doctrines, policies, constraints, tech preferences
- EPIC.S7.WARFRONT_THEATERS: Procedural warfront seeding from geography and faction goals
- EPIC.S7.WARFRONT_STATE: Front lines, objectives, supply demand, attrition, morale, stability
- EPIC.S7.SUPPLY_IMPACT: Delivered goods and services move warfront state with persistent consequences
- EPIC.S7.TERRITORY_REGIMES: Permissions, tariffs, embargoes, inspections, closures; hysteresis rules
- EPIC.S7.TECH_ACCESS: Exclusives, embargoed tech, licensing, doctrine-based variants
- EPIC.S7.REPUTATION_INFLUENCE: Diplomacy verbs [OPEN: in-scope set, eg treaties%bounties%sanctions%privateering]
- EPIC.S7.UI_DIPLO: Faction intel, deal making, “why policy changed”
- EPIC.S7.UI_WARFRONT: Dashboards, projected outcomes, intervention options, supply checklists
- EPIC.S7.BRIDGE_THRONEROOM_V0: Bridge layer as strategic view + unlock surface tied to factions%warfronts%tech posture
- EPIC.S7.PERF_BUDGET: Tick budget tests extended for warfront systems

Status: TODO

---

### Slice 8: Fracture policing, existential pressure, megaproject endgames
Purpose: Lane builders police fracture; pressure escalates; win via massive supply-bound projects under multiple scenarios.

Epics:
- EPIC.S8.POLICING_SIM: Trace-driven escalation model, legible actions, counterplay verbs
- EPIC.S8.THREAT_IMPACT: Supply shocks, lane disruption, interdiction waves, faction realignment
- EPIC.S8.PLAYER_COUNTERPLAY: Counter-programs, corridor hardening, trace scrubbers, misdirection [OPEN]
- EPIC.S8.MEGAPROJECTS: Multi-stage projects that reshape map rules (anchors, stabilizers, pylons, corridors) [OPEN: final set]
- EPIC.S8.WIN_SCENARIOS: Multiple scenario wins (containment, alliance, dominance, escape, reconciliation) + explicit loss states
- EPIC.S8.UI_WARROOM: Warfronts + policing + megaproject pipelines + bottlenecks
- EPIC.S8.STORY_STATE_MACHINE: Story beats via discovery%trace%warfront phases, not timed missions
- EPIC.S8.BRIDGE_THRONEROOM_V1: Endgame readiness, scenario selection, empire posture surface
- EPIC.S8.PERF_BUDGET: Tick budget tests extended for endgame pressure systems

Status: TODO

---

### Slice 9: Polish, UX hardening, and mod hooks
Purpose: Make it shippable and extendable without breaking determinism.

Epics:
- EPIC.S9.SAVE: Robust save UX, migrations, corruption handling (lock final migration policy)
- EPIC.S9.UI: Information architecture cleanup, tooltips, clarity passes, onboarding guidance
- EPIC.S9.MOD: Content packs, compatibility rules, safe mod surface, validation tooling
- EPIC.S9.PERF: Performance profiling, tick cost budgets, memory budgets (lock final budgets)
- EPIC.S9.ACCESS: Basic accessibility and input configuration [OPEN: scope]
- EPIC.S9.BALANCE_LOCK: Tuning targets and regression bounds locked [TBD: numeric targets]
- EPIC.S9.CONTENT_WAVES: Final archetype families, world classes, endgame megaproject variety

Status: TODO
