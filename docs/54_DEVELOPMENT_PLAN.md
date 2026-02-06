# 54_DEVELOPMENT_PLAN

This is the canonical development ledger and roadmap.
Architecture and design-law docs define the spec. This doc defines execution order and tracking.

Generated artifacts (status packets, scans, test outputs) live under docs/generated and are evidence only.

Status meanings:
- TODO
- IN_PROGRESS
- DONE
- DEFERRED

Update rule:
- Each dev session should target 1–3 gate movements or epic movements, and end with an update here plus evidence references.

Primary anchors:
- docs/52_DEVELOPMENT_LOCK_RECOMMENDATIONS.md (slice locks, gate tests)
- docs/50_EARLY_MISSION_LADDER_AND_TRAVEL.md
- docs/51_ECONOMY_AND_TRADE_DESIGN_LAW.md
- docs/53_PROGRAMS_FLEETS_DOCTRINES_CONTROL_SURFACE.md
- docs/30_CONNECTIVITY_AND_INTERFACES.md
- docs/20_TESTING_AND_DETERMINISM.md
- docs/21_UNITS_AND_INVARIANTS.md
- docs/40_TOOLING_AND_HOOKS.md

---

## A. Slice map (Layer 1)

### Slice 0: Repo + determinism foundation (pre-slice, always on)
Purpose: Make LLM-driven development safe, deterministic, and boundary-respecting.

Epics:
- EPIC.S0.TOOLING: DevTool commands for repeatable workflows (packets, scans, test runs)
- EPIC.S0.DETERMINISM: Golden hashes, replay harness, stable world hashing
- EPIC.S0.CONNECTIVITY: Connectivity scanner and zero-violation policy for Slice scope
- EPIC.S0.QUALITY: Minimal CI-like local scripts (format, build, tests)

Status: IN_PROGRESS

---

### Slice 1 (LOCKED): Logistics + Market + Intel micro-world
Purpose: Prove the core economic simulation loop in a tiny world, deterministically, with minimal UI.

Gates: (tracked in section B)
Status: TODO

---

### Slice 1.5 (LOCKED): Tech sustainment via supply chain
Purpose: Prove “industry enablement depends on supply” with clear failure modes and UI.

Gates: (tracked in section B)
Status: TODO

---

### Slice 2: Programs as the primary player control surface
Purpose: Shift player power from manual micromanagement to programs, quotes, doctrines.

Epics:
- EPIC.S2.PROG.MODEL: Program, Fleet, Doctrine core models align to docs/53
- EPIC.S2.PROG.QUOTE: Liaison Quote flow for “do X”, cost, time, risks, constraints
- EPIC.S2.PROG.EXEC: Program execution pipeline (intent-driven, deterministic)
- EPIC.S2.PROG.UI: Control surface UI for creating programs and reading outcomes
- EPIC.S2.PROG.SAFETY: Guardrails against direct state mutation, only intents

Status: TODO

---

### Slice 3: Fleet automation and logistics scaling
Purpose: Multi-route trade, hauling, and supply operations at scale without micromanagement.

Epics:
- EPIC.S3.LOGI.ROUTES: Route planning and lane scheduling primitives
- EPIC.S3.FLEET.ROLES: Fleet compositions and role constraints (miner, hauler, escort)
- EPIC.S3.MARKET.ARB: Automation that exploits spreads but is not money-printing
- EPIC.S3.RISK.MODEL: Predictable risk bands, losses, insurance-like sinks
- EPIC.S3.UI.DASH: Dashboards for flows, margins, bottlenecks, intel quality

Status: TODO

---

### Slice 4: Production, industry, and construction programs
Purpose: Player and NPCs can build and operate industry nodes with supply dependencies.

Epics:
- EPIC.S4.INDU.STRUCT: Production chains beyond 2 goods, modular recipes
- EPIC.S4.CONSTR.PROG: Construction programs (starports, refineries, science centers)
- EPIC.S4.MAINT.SUSTAIN: Maintenance as sustained supply, not “repair minigame”
- EPIC.S4.UI.INDU: Industrial planning UI and time-to-capability readouts
- EPIC.S4.NPC.INDU: NPC industry and trade responding to incentives

Status: TODO

---

### Slice 5: Combat abstraction + security as an economic factor
Purpose: Make force and safety matter economically without real-time twitch combat.

Epics:
- EPIC.S5.SECURITY.LANES: Security levels affect risk, delay, pricing, insurance
- EPIC.S5.COMBAT.RESOLVE: Deterministic combat resolver (time, attrition, outcomes)
- EPIC.S5.ESCORT.PROG: Escort, patrol, interdiction as programs
- EPIC.S5.UI.SECURITY: Threat maps, convoy planning, loss analysis

Status: TODO

---

### Slice 6: Exploration, anomalies, and off-lane travel
Purpose: Expand the map with long-horizon exploration, anomaly discovery, and high cost travel.

Epics:
- EPIC.S6.MAP.GALAXY: Galaxy map UX, navigation, discovery state
- EPIC.S6.OFFLANE: Off-lane travel rules and cost curves
- EPIC.S6.ANOMALY: Procedural anomaly generation and discovery outcomes
- EPIC.S6.SCIENCE: Science centers and research programs (if in scope)
- EPIC.S6.INTEL.EXPAND: Intel model extended to exploration and unknowns

Status: TODO

---

### Slice 7: Factions, governance, and systemic response
Purpose: The world pushes back. NPC factions adapt to trade, war, and scarcity.

Epics:
- EPIC.S7.FACTION.MODEL: Faction goals, resources, constraints
- EPIC.S7.POLICY: Taxes, tariffs, embargoes, enforcement as economic levers
- EPIC.S7.RESPONSE: NPC response to player dominance (competition, alliances)
- EPIC.S7.UI.DIPLO: Diplomacy and faction intel UI

Status: TODO

---

### Slice 8: Existential threat arc and endgame pressure
Purpose: A late-game forcing function that reshapes incentives without timed missions.

Epics:
- EPIC.S8.THREAT.SIM: Threat simulation model and triggers
- EPIC.S8.THREAT.IMPACT: Supply shocks, lane disruption, territory changes
- EPIC.S8.PLAYER.COUNTER: Counter-programs and industrial mobilization
- EPIC.S8.UI.WARROOM: Warfront and mobilization dashboards
- EPIC.S8.WINLOSE: End-state conditions and postgame continuity choice

Status: TODO

---

### Slice 9: Polish, UX hardening, and mod hooks
Purpose: Make it shippable and extendable without breaking determinism.

Epics:
- EPIC.S9.SAVE: Robust save UX, migrations, corruption handling
- EPIC.S9.UI: Information architecture cleanup, tooltips, clarity passes
- EPIC.S9.MOD: Data-driven configs, safe mod surface, validation tooling
- EPIC.S9.PERF: Performance profiling, tick cost budgets, memory budgets
- EPIC.S9.ONBOARD: Tutorialization via safe, non-timed guidance

Status: TODO

---

## B. Slice 1 and 1.5 gates (locked execution gates)

### B1. Workflow and tooling gates
| Gate ID | Gate | Status | Evidence |
|---|---|---|---|
| GATE.TOOL.001 | Deterministic status packet generation exists (diff-driven, capped) | DONE | docs/generated/02_STATUS_PACKET.txt |
| GATE.CONN.002 | Connectivity violations empty for Slice scope | DONE | docs/generated/connectivity_violations.json |
| GATE.TEST.001 | Headless determinism harness exists | DONE | SimCore.Tests/GoldenReplayTests.cs |
| GATE.TEST.002 | Golden world hash regression exists and is stable | DONE | docs/generated/snapshots/golden_replay_hashes.txt |

### B2. Slice 1 critical gates
| Gate ID | Gate | Status | Evidence |
|---|---|---|---|
| GATE.TIME.001 | 60x time contract enforced: 1s real = 1 min sim, no acceleration | DONE | SimCore.Tests/* |
| GATE.INTENT.001 | Deterministic intent pipeline exists | TODO | SimCore.Tests/* |
| GATE.WORLD.001 | 2 stations, 1 lane, 2 goods micro-world config | DONE | tests + config paths |
| GATE.STA.001 | Station inventory ledger and invariants | DONE | tests |
| GATE.LANE.001 | Lane flow with deterministic delay arrivals | DONE | tests |
| GATE.MKT.001 | Inventory-based pricing with spread | TODO | tests |
| GATE.MKT.002 | Price publish cadence every 12 game hours | TODO | tests |
| GATE.INTEL.001 | Local truth, remote banded intel + age | TODO | tests |
| GATE.UI.001 | Minimal panel shows inventory, price, intel age | TODO | UI path |
| GATE.UI.002 | Buy/sell generates intent, no direct mutation | TODO | tests + UI path |
| GATE.DET.001 | 10,000 tick run stable world hash | TODO | test output |
| GATE.SAVE.001 | Save/load round trip preserves hash | TODO | test output |
| GATE.INV.001 | Invariants suite passes | TODO | test output |

### B3. Slice 1.5 sustainment gates
| Gate ID | Gate | Status | Evidence |
|---|---|---|---|
| GATE.TECH.001 | One tech requires 2 goods per tick to remain enabled | TODO | tests |
| GATE.TECH.002 | Buffers sized in days of game time | TODO | tests |
| GATE.TECH.003 | Deterministic degradation under undersupply | TODO | tests |
| GATE.UI.101 | UI shows sustainment margin and time-to-failure | TODO | UI path |
| GATE.DET.101 | Sustainment determinism regression passes | TODO | test output |
| GATE.INV.101 | Buffer math invariants pass | TODO | test output |

---

## C. Session log (append only)
Format: YYYY-MM-DD, branch, summary, gates or epics moved

- 2026-02-05, <branch>, initialized Layer 1 plan (EPIC map created)
- 2026-02-06, main, GATE.TIME.001 DONE (1 tick = 1 game minute, 60x), tests: SimCore.Tests/Time/TimeContractTests.cs
