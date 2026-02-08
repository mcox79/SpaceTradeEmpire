# 54_DEVELOPMENT_PLAN

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
- EPIC.S0.EVIDENCE: Context packet must include or reference latest scan + test + hash artifacts

Exit criteria for DONE:
- Context packet reliably surfaces scan + test + determinism evidence, or explicitly reports why missing
- Connectivity violations remain empty for current slice scope
- Golden replay + long-run + save/load determinism regressions are stable

Status: IN_PROGRESS

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

Status: IN_PROGRESS

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
| GATE.EVID.001 | Context packet reports latest scan + test summary + hash snapshot presence (or explicit “not found” reasons) | DONE | docs/generated/01_CONTEXT_PACKET.md ([SYSTEM HEALTH] shows Connectivity OK + Tests OK + Hash Snapshot present) |
| GATE.MAP.001 | Repo evidence export exists (tests index + grep + map) | DONE | docs/generated/evidence/simcore_tests_index.txt + docs/generated/evidence/gate_evidence_grep.txt + docs/generated/evidence/gate_evidence_map.json |
| GATE.FILE.001 | Runtime File Contract enforced (runtime IO restricted to res:// and user://; SimCore has no System.IO IO) | DONE | SimCore.Tests/Invariants/RuntimeFileContractTests.cs |

### B2. Slice 1 critical gates
| Gate ID | Gate | Status | Evidence |
|---|---|---|---|
| GATE.TIME.001 | 60x time contract enforced: 1s real = 1 min sim, no acceleration | DONE | SimCore.Tests/TimeContractTests.cs |
| GATE.INTENT.001 | Deterministic intent pipeline exists | DONE | SimCore.Tests/Intents/IntentSystemTests.cs + scripts/bridge/SimBridge.cs (EnqueueIntent) |
| GATE.WORLD.001 | 2 stations, 1 lane, 2 goods micro-world config | DONE | SimCore.Tests/World/World001_MicroWorldLoadTests.cs + SimCore.Tests/Intents/IntentSystemTests.cs (KernelWithWorld001) |
| GATE.STA.001 | Station inventory ledger and invariants | DONE | SimCore.Tests/Systems/InventoryLedgerTests.cs + SimCore.Tests/Invariants/InventoryConservationTests.cs; SimCore.Tests/Invariants/BasicStateInvariantsTests.cs |
| GATE.LANE.001 | Lane flow with deterministic delay arrivals | DONE | SimCore.Tests/Systems/LaneFlowSystemTests.cs |
| GATE.MKT.001 | Inventory-based pricing with spread | DONE | SimCore.Tests/MarketTests.cs + SimCore.Tests/Systems/MarketPublishCadenceTests.cs |
| GATE.MKT.002 | Price publish cadence every 12 game hours | DONE | SimCore.Tests/Systems/MarketPublishCadenceTests.cs |
| GATE.INTEL.001 | Local truth, remote banded intel + age | DONE | SimCore.Tests/Systems/IntelContractTests.cs |
| GATE.UI.001 | Minimal panel shows inventory, price, intel age | DONE | scripts/ui/StationMenu.cs + scripts/bridge/SimBridge.cs |
| GATE.UI.002 | Buy/sell generates intent, no direct mutation | DONE | scripts/ui/StationMenu.cs (SubmitBuyIntent/SubmitSellIntent) + scripts/bridge/SimBridge.cs (EnqueueIntent + BuyIntent/SellIntent) + SimCore.Tests/Intents/IntentSystemTests.cs |
| GATE.DET.001 | 10,000 tick run stable world hash | DONE | SimCore.Tests/Determinism/LongRunWorldHashTests.cs (LongRunWorldHash) + docs/generated/05_TEST_SUMMARY.txt |
| GATE.SAVE.001 | Save/load round trip preserves hash | DONE | SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs (SaveLoadWorldHash) + docs/generated/05_TEST_SUMMARY.txt |
| GATE.INV.001 | Invariants suite passes | DONE | SimCore.Tests/Invariants/InventoryConservationTests.cs (InventoryConservation); SimCore.Tests/Invariants/BasicStateInvariantsTests.cs (BasicStateInvariants) + docs/generated/05_TEST_SUMMARY.txt |

### B3. Slice 1.5 sustainment gates
| Gate ID | Gate | Status | Evidence |
|---|---|---|---|
| GATE.TECH.001 | One tech requires 2 goods per tick to remain enabled | DONE | SimCore.Tests/Sustainment/TechUpkeepConsumesGoodsTests.cs |
| GATE.TECH.002 | Buffers sized in days of game time | DONE | SimCore.Tests/Sustainment/BufferSizingDaysTests.cs |
| GATE.TECH.003 | Deterministic degradation under undersupply | DONE | SimCore.Tests/Sustainment/DeterministicDegradationTests.cs |
| GATE.UI.101 | UI shows sustainment margin and time-to-failure | DONE | scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Systems/SustainmentReport.cs |
| GATE.DET.101 | Sustainment determinism regression passes | DONE | SimCore.Tests/Sustainment/SustainmentDeterminismRegressionTests.cs |
| GATE.INV.101 | Buffer math invariants pass | DONE | SimCore.Tests/Sustainment/BufferMathInvariantsTests.cs |

### B4. Slice 2 programs gates (v1)
| Gate ID | Gate | Status | Evidence |
|---|---|---|---|
| GATE.PROG.001 | Program schema v1 exists (TradeProgram only) and is versioned | DONE | SimCore/Schemas/ProgramSchema.json + SimCore.Tests/Programs/ProgramContractTests.cs (PROG_001) |
| GATE.FLEET.001 | Fleet binding v1 exists (single trader fleet) and is deterministic | DONE | SimCore/World/WorldLoader.cs + SimCore.Tests/Programs/FleetBindingContractTests.cs + docs/generated/05_TEST_SUMMARY.txt |
| GATE.DOCTRINE.001 | DefaultDoctrine exists (max 2 toggles) and is deterministic | DONE | SimCore/Programs/DefaultDoctrine.cs + SimCore.Tests/Programs/DefaultDoctrineContractTests.cs |
| GATE.QUOTE.001 | Liaison Quote is deterministic: request + snapshot => quote (cost/time/risks/constraints) | DONE | SimCore/Programs/ProgramQuote.cs + SimCore/Programs/ProgramQuoteSnapshot.cs + SimCore.Tests/Programs/ProgramQuoteContractTests.cs + SimCore.Tests/TestData/Snapshots/program_quote_001.json + docs/generated/05_TEST_SUMMARY.txt |
| GATE.EXPLAIN.001 | Explain events are schema-bound (no free-text) for quote and outcomes | DONE | SimCore.Tests/Programs/ProgramContractTests.cs (EXPLAIN_001) |
| GATE.PROG.EXEC.001 | Program execution emits intents only, no direct ledger mutation | DONE | SimCore.Tests/Programs/ProgramContractTests.cs (PROG_EXEC_001) + SimCore/Programs/ProgramSystem.cs |
| GATE.PROG.EXEC.002 | TradeProgram drives buy/sell intents against Slice 1 micro-world and affects outcomes only via SimCore tick | DONE | SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs + docs/generated/05_TEST_SUMMARY.txt |
| GATE.BRIDGE.PROG.001 | GameShell -> SimCore bridge supports program lifecycle (create/start/pause) without direct state mutation | DONE | scripts/bridge/SimBridge.cs + SimCore.Tests/Programs/ProgramLifecycleContractTests.cs + SimCore.Tests/Programs/ProgramStatusCommandContractTests.cs |
| GATE.UI.PROG.001 | Minimal Programs UI: create, view quote, start/pause, last-tick outcomes | DONE | scripts/ui/ProgramsMenu.cs + scripts/ui/StationMenu.cs + scripts/bridge/SimBridge.cs + scenes/playable_prototype.tscn |
| GATE.DET.PROG.001 | Determinism regression includes program lifecycle (create/start/pause) with stable hash | DONE | SimCore.Tests/Determinism/ProgramDeterminismTests.cs + SimCore.Tests/SaveLoad/ProgramSaveLoadContractTests.cs |

---

## C. Session log (append only)
Format: YYYY-MM-DD, branch, summary, gates or epics moved

- 2026-02-05, <branch>, initialized Layer 1 plan (EPIC map created)
- 2026-02-06, main, GATE.TIME.001 DONE (1 tick = 1 game minute, 60x), tests: SimCore.Tests/Time/TimeContractTests.cs
- 2026-02-06, main, GATE.INTENT.001 DONE (intent queue + deterministic ordering), tests: SimCore.Tests/Intents/IntentSystemTests.cs
- 2026-02-06, main, GATE.MKT.002 DONE (published prices update every 720 ticks), tests: SimCore.Tests/Systems/MarketPublishCadenceTests.cs
- 2026-02-06, main, GATE.INTEL.001 DONE (local truth, remote banded intel + age), tests: SimCore.Tests/Systems/IntelContractTests.cs
- 2026-02-06, main, GATE.DET.001 DONE (10,000 tick determinism), tests: SimCore.Tests/Determinism/LongRunWorldHashTests.cs, evidence: docs/generated/05_TEST_SUMMARY.txt
- 2026-02-06, main, GATE.SAVE.001 DONE (save/load preserves world hash), tests: SimCore.Tests/SaveLoad/SaveLoadWorldHashTests.cs, evidence: docs/generated/05_TEST_SUMMARY.txt
- 2026-02-06, main, GATE.INV.001 DONE (invariants suite), tests: SimCore.Tests/Invariants/InventoryConservationTests.cs; SimCore.Tests/Invariants/BasicStateInvariantsTests.cs, evidence: docs/generated/05_TEST_SUMMARY.txt
- 2026-02-06, main, GATE.UI.001 DONE (StationMenu shows inventory, price, intel age), evidence: scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs
- 2026-02-06, main, GATE.UI.002 DONE (StationMenu submits buy/sell intents via bridge, no TradeCommand in UI), evidence: scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Intents/BuyIntent.cs; SimCore/Intents/SellIntent.cs
- 2026-02-06, main, SimCore regression run PASS (dotnet test SimCore.Tests.csproj: 40 passed, 0 failed). Evidence: docs/generated/05_TEST_SUMMARY.txt
- 2026-02-06, main, Slice 0 status kept IN_PROGRESS: context packet did not surface test summary despite tests present; added GATE.EVID.001 TODO. Evidence: docs/generated/01_CONTEXT_PACKET.md; docs/generated/05_TEST_SUMMARY.txt
- 2026-02-06, main, GATE.EVID.001 DONE (context packet surfaces connectivity + test summary + hash snapshot presence). Evidence: docs/generated/01_CONTEXT_PACKET.md; docs/generated/05_TEST_SUMMARY.txt; docs/generated/snapshots/golden_replay_hashes.txt
- 2026-02-06, main, Added Slice 2 v1 scope lock and B4 Slice 2 gate table (all TODO)
- 2026-02-06, main, Pinned Slice 1 evidence paths from docs/generated/evidence/* (World001, lane flow, inventory ledger, market tests)
- 2026-02-07, main, GATE.TECH.001/002/003 DONE (tech upkeep inputs, buffer sizing in days, deterministic degradation; logistics wired into tick), tests: SimCore.Tests/Sustainment/TechUpkeepConsumesGoodsTests.cs; SimCore.Tests/Sustainment/BufferSizingDaysTests.cs; SimCore.Tests/Sustainment/DeterministicDegradationTests.cs
- 2026-02-07, main, GATE.TEST.002 DONE (golden replay snapshot updated to match deterministic genesis+final hashes), tests: SimCore.Tests/Determinism/LongRunWorldHashTests.cs, evidence: docs/generated/snapshots/golden_replay_hashes.txt
- 2026-02-07, main, GATE.DET.101 + GATE.INV.101 DONE (sustainment determinism regression + buffer math invariants), tests: SimCore.Tests/Sustainment/SustainmentDeterminismRegressionTests.cs; SimCore.Tests/Sustainment/BufferMathInvariantsTests.cs
- 2026-02-07, main, GATE.UI.101 DONE (StationMenu shows sustainment margin + time-to-failure via bridge + SustainmentReport), evidence: scripts/ui/StationMenu.cs; scripts/bridge/SimBridge.cs; SimCore/Systems/SustainmentReport.cs
- 2026-02-07, main, GATE.PROG.001 + GATE.PROG.EXEC.001 + GATE.EXPLAIN.001 DONE (program schema present and versioned; program explain is schema-bound + deterministic; programs emit intents only), tests: SimCore.Tests/Programs/ProgramContractTests.cs, evidence: SimCore/Schemas/ProgramSchema.json
- 2026-02-07, main, GATE.BRIDGE.PROG.001 + GATE.DET.PROG.001 DONE (bridge program lifecycle via commands; determinism and save/load include programs), tests: SimCore.Tests/Programs/ProgramLifecycleContractTests.cs; SimCore.Tests/Programs/ProgramStatusCommandContractTests.cs; SimCore.Tests/Determinism/ProgramDeterminismTests.cs; SimCore.Tests/SaveLoad/ProgramSaveLoadContractTests.cs, evidence: scripts/bridge/SimBridge.cs
- 2026-02-08, main, GATE.UI.PROG.001 DONE (ProgramsMenu UI present, opens as modal, can create program and view quote/outcome; blocks clicks behind), evidence: scripts/ui/ProgramsMenu.cs; scripts/ui/StationMenu.cs; scenes/playable_prototype.tscn
- 2026-02-08, main, UI polish: StationMenu widened to fit program controls; ProgramsMenu row wrapping + hide-cancelled default; Escape closes ProgramsMenu, and Escape undocks when StationMenu focused.
- 2026-02-08, main, GATE.PROG.EXEC.002 DONE (integration: program drives BUY+SELL against World001 and outcomes change only via tick). Evidence: SimCore.Tests/Programs/ProgramExecutionIntegrationTests.cs; docs/generated/05_TEST_SUMMARY.txt
- 2026-02-08, main, GATE.QUOTE.001 DONE (deterministic request+snapshot=>quote with golden). Evidence: SimCore/Programs/ProgramQuote.cs; SimCore/Programs/ProgramQuoteSnapshot.cs; SimCore.Tests/Programs/ProgramQuoteContractTests.cs; SimCore.Tests/TestData/Snapshots/program_quote_001.json; docs/generated/05_TEST_SUMMARY.txt
- 2026-02-08, main, GATE.FLEET.001 DONE (deterministic single player trader fleet created by WorldLoader). Evidence: SimCore/World/WorldLoader.cs; SimCore.Tests/Programs/FleetBindingContractTests.cs; docs/generated/05_TEST_SUMMARY.txt
- 2026-02-08, main, GATE.DOCTRINE.001 DONE (DefaultDoctrine deterministic, max 2 toggles). Evidence: SimCore/Programs/DefaultDoctrine.cs; SimCore.Tests/Programs/DefaultDoctrineContractTests.cs
