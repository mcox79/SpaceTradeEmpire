# EA Performance Baseline — Space Trade Empire

**Generated:** 2026-03-22
**Build config:** Release / .NET 8 / x64
**Test machine:** Windows 11 Home (development workstation)

---

## 1. Tick Time Baseline

### Methodology

Two separate tick-budget test suites measure raw tick throughput:

| Test | Seed | Galaxy | Warmup ticks | Measured ticks | Budget |
|---|---|---|---|---|---|
| `PerfBudget_Slice3_V0` | 424242 | 20-node | 300 | 300 | 50 ms/tick |
| `PerfBudget_Slice4_V0` | 434343 | 20-node | 400 | 400 | 75 ms/tick |
| `TickBudgetTests` | 42-46 (5 seeds) | 20-node | 50 | 500 | 20 ms/tick |

### Measured Results

| Test | Measured avg (ms/tick) | Budget | Headroom |
|---|---|---|---|
| Slice3 (seed 424242, 300 ticks) | **3.343** | 50 ms | 93.3% under budget |
| Slice4 (seed 434343, 400 ticks) | **3.873** | 75 ms | 94.8% under budget |
| AllIndustrySystems 1000 ticks | < 0.13 ms avg | — | Pass |

**Summary: Average tick cost at EA scale (20-node galaxy, live systems) is ~3.3–3.9 ms.**

---

## 2. Memory Baseline

### Test: `SimState_50NodeGalaxy_MemoryUnder256MB`

| Parameter | Value |
|---|---|
| Galaxy size | 50 nodes, radius 200 |
| Tick count | 500 |
| Seed | 42 |
| GC mode | Full collection forced before measurement |
| **Measured heap** | **4.33 MB** |
| Budget | 256 MB |
| Headroom | **98.3% under budget** |

The SimCore state is extremely memory-efficient. The entire simulation heap after 500 ticks on a 50-node galaxy fits in 4.33 MB — well within any realistic target for a desktop game running alongside Godot's renderer.

---

## 3. Collection Size Baseline

Measured from `SimState_CollectionCounts_Reasonable` — 50 nodes, 500 ticks, seed 42:

| Collection | Measured size | Sanity limit | Notes |
|---|---|---|---|
| Fleets | 170 | 10,000 | NPC + player fleets in circulation |
| Markets | 50 | 10,000 | One per node |
| Nodes | 50 | 10,000 | Matches galaxy size |
| Edges | 56 | 10,000 | Lane graph; slightly more than nodes due to connectivity |
| IndustrySites | 126 | 10,000 | ~2.5 sites per node on average |
| IndustryBuilds | 0 | 10,000 | No active construction queued at tick 500 |
| Warfronts | 2 | 10,000 | Active warfronts spawned by generation |
| Megaprojects | 0 | 10,000 | None started by bots |
| Embargoes | 2 | 10,000 | Active diplomacy embargoes |
| InFlightTransfers | 0 | 10,000 | Settled; cargo in transit is transient |

Slice3/Slice4 tests (20-node galaxy, more NPC activity) measured:
- Fleets: 182–195
- Active transfers: 200 (at cap, showing NPC circulation is saturated)
- Industry sites: 156–159
- Construction-enabled sites: 0–1

No runaway growth detected across all collection types.

---

## 4. System-by-System Tick Budget Allocation

All 63 profiled call sites from `SimKernel.Step()` are listed below with expected time budget. Budgets are engineering targets — not measured per-system (profiling instrumentation is available via `SimKernel.ProfilingEnabled = true` for future per-system breakdown).

The total tick budget envelope is **8 ms** for the simulation thread at 60fps (leaving 8.6 ms for render + GC). Current average is 3.3–3.9 ms, giving ~4–5 ms of headroom.

### Tier 1 — Sub-0.05 ms (trivial, O(1) or O(small-N))

| System | Call | Expected budget |
|---|---|---|
| LaneFlowSystem | `Process` | < 0.05 ms |
| FractureWeightSystem | `Process` | < 0.05 ms |
| FleetUpkeepSystem | `Process` | < 0.05 ms |
| PowerBudgetSystem | `Process` | < 0.05 ms |
| HavenUpgradeSystem | `ProcessKeeper` | < 0.05 ms |
| HavenResearchLabSystem | `Process` | < 0.05 ms |
| HavenFabricatorSystem | `Process` | < 0.05 ms |
| HavenMarketSystem | `Process` | < 0.05 ms |
| MegaprojectSystem | `Process` | < 0.05 ms |
| ConstructionSystem | `ProcessConstruction` | < 0.05 ms |
| DreadDrainSystem | `Process` | < 0.05 ms |
| ExposureTrackSystem | `Process` | < 0.05 ms |
| SensorGhostSystem | `Process` | < 0.05 ms |
| InformationFogSystem | `Process` | < 0.05 ms |
| LatticeFaunaSystem | `Process` | < 0.05 ms |
| PentagonBreakSystem | `Process` | < 0.05 ms |
| HavenEndgameSystem | `Process` | < 0.05 ms |
| LossDetectionSystem | `Process` | < 0.05 ms |
| WinConditionSystem | `Process` | < 0.05 ms |
| StoryStateMachineSystem | `Process` | < 0.05 ms |
| WarConsequenceSystem | `Process` | < 0.05 ms |
| CommissionSystem | `Process` | < 0.05 ms |
| DiplomacySystem | `Process` | < 0.05 ms |
| DiscoveryOutcomeSystem | `Process` | < 0.05 ms |
| PlanetScanSystem | `Process` | < 0.05 ms |
| TopologyShiftSystem | `Process` | < 0.05 ms |

### Tier 2 — 0.05–0.2 ms (light per-fleet or per-node iteration)

| System | Call | Expected budget |
|---|---|---|
| IntentSystem | `Process` | 0.05–0.1 ms |
| MovementSystem | `Process` | 0.05–0.1 ms |
| SustainSystem | `Process` | 0.05–0.1 ms |
| LatticeDroneSpawnSystem | `Process` | 0.05–0.1 ms |
| LatticeDroneCombatSystem | `Process` | 0.05–0.1 ms |
| LootTableSystem | `ProcessDespawn` | 0.05–0.1 ms |
| JumpEventSystem | `Process` | 0.05–0.1 ms |
| RiskSystem | `Process` | 0.05–0.1 ms |
| EscortSystem | `Process` | 0.05–0.1 ms |
| SecurityLaneSystem | `ProcessSecurityLanes` | 0.05–0.1 ms |
| PressureSystem | `ProcessPressure` | 0.05–0.1 ms |
| PressureSystem | `EnforceConsequences` | 0.05–0.1 ms |
| ReputationSystem | `Process` | 0.05–0.1 ms |
| MilestoneSystem | `Process` | 0.05–0.1 ms |
| TutorialSystem | `Process` | 0.05–0.1 ms |
| FirstOfficerSystem | `Process` | 0.05–0.1 ms |
| NarrativeNpcSystem | `Process` | 0.05–0.1 ms |
| FractureSystem | `Process` | 0.05–0.1 ms |
| FractureSystem | `ApplyFractureGoodsFlowV0` | 0.05–0.1 ms |
| HavenUpgradeSystem | `Process` | 0.05–0.1 ms |
| ResearchSystem | `ProcessResearch` | 0.05–0.1 ms |
| RefitSystem | `ProcessRefitQueue` | 0.05–0.1 ms |
| MaintenanceSystem | `ProcessDecay` | 0.05–0.1 ms |
| StationConsumptionSystem | `Process` | 0.05–0.1 ms |
| WarfrontDemandSystem | `Process` | 0.05–0.1 ms |
| WarfrontEvolutionSystem | `Process` | 0.05–0.1 ms |
| InstabilitySystem | `Process` | 0.05–0.1 ms |
| SupplyShockSystem | `Process` | 0.05–0.1 ms |

### Tier 3 — 0.2–1.0 ms (moderate N iteration, market or fleet loops)

| System | Call | Expected budget | Risk flag |
|---|---|---|---|
| ProgramSystem | `Process` | 0.2–0.5 ms | Low — program list is bounded |
| LogisticsSystem | `Process` | 0.2–0.5 ms | Low — logistics jobs scale with sites |
| IndustrySystem | `Process` | 0.2–0.5 ms | Low — bounded per site |
| StationContextSystem | `Process` | 0.2–0.5 ms | Low |
| MissionSystem | `Process` | 0.2–0.5 ms | Low — active mission list is small |
| SystemicMissionSystem | `Process` | 0.2–0.5 ms | Low |
| FleetPopulationSystem | `Process` | 0.2–0.5 ms | Low — replacement capped per tick |
| KnowledgeGraphSystem | `Process` | 0.2–0.5 ms | Low |
| IntelSystem | `UpdateNodeObservation` | 0.2–0.5 ms | Low |
| IntelSystem | `ProcessScannerIntel` | 0.2–0.5 ms | Low |
| IntelSystem | `ProcessPriceHistory` | 0.2–0.5 ms | **Watch:** price history grows with node count |

### Tier 4 — 1.0–3.0 ms (hot paths, O(fleets * nodes) candidates)

| System | Call | Expected budget | Risk flag |
|---|---|---|---|
| NpcTradeSystem | `ProcessNpcTrade` | 1.0–2.0 ms | **WATCH:** iterates all NPC fleets x markets |
| NpcFleetCombatSystem | `Process` | 0.5–1.5 ms | **WATCH:** combat resolution for all active NPCs |
| NpcIndustrySystem | `ProcessNpcIndustry` | 0.5–1.0 ms | Watch at large galaxy sizes |
| NpcIndustrySystem | `ProcessNpcReaction` | 0.5–1.0 ms | Watch at large galaxy sizes |

---

## 5. 60fps Feasibility Analysis

### Frame budget at 60fps

```
Total frame budget:         16.67 ms
Godot renderer (est.):       4.0  ms  (3D galaxy view, minimal geometry)
GC pressure (est.):          1.5  ms  (amortized; .NET 8 background GC)
SimBridge read-lock:         0.5  ms  (snapshot serialization per frame)
SimCore tick (measured):     3.9  ms  (worst case from Slice4 test)
                           --------
Remaining headroom:          6.8  ms
```

**60fps is firmly achievable.** The simulation tick is 3.3–3.9 ms at EA scale on the development machine. Even if tick cost doubles at 100-node galaxy sizes, it would still be within budget.

### Tick frequency note

SimCore does not tick at 60 Hz. The game design runs **one sim tick per player action** (trade, travel, etc.) with NPC ticks driven by a configurable cadence (default: every game-day = several real seconds). The tick cost above is a worst-case ceiling for burst catchup scenarios, not a per-frame cost.

---

## 6. Optimization Targets

Systems flagged for monitoring as content volume grows:

| System | Risk | Trigger condition | Mitigation available |
|---|---|---|---|
| `NpcTradeSystem` | MEDIUM | Fleets x markets O(N²) scan at 100+ nodes | Spatial index on lanes; bucket NPCs by home region |
| `IntelSystem.ProcessPriceHistory` | LOW-MEDIUM | Price history dictionary grows unboundedly | Already capped at per-node ring buffer; verify cap holds at 100 nodes |
| `NpcFleetCombatSystem` | LOW | All fleets checked for combat eligibility each tick | Early-exit guards already in place; verify at 200+ NPCs |
| `NpcIndustrySystem` | LOW | Scales with IndustrySite count (~2.5x node count) | Sites are static post-generation; no concern at EA scale |
| `LogisticsSystem` | LOW | Buffer shortages trigger job creation proportional to sites | Job list is bounded; watch at 80+ nodes |
| `KnowledgeGraphSystem` | LOW | Revelation chain checks on each node reveal | Graph is acyclic DAG; traversal is O(edges) not O(N²) |

No system is currently projected to exceed 2 ms in isolation at EA launch scale (50-node campaigns). The 2 ms single-system threshold becomes relevant only if the total galaxy grows to 150+ nodes in post-launch content.

---

## 7. Recommendations

### Ship for EA

The current performance envelope is well-suited for Early Access:

- **Tick cost**: 3.3–3.9 ms average. Budget is 8 ms (half of 16.6 ms frame after render). **4–5 ms headroom.**
- **Memory**: 4.33 MB heap after 500 ticks on a 50-node galaxy. Even 10x growth stays under 50 MB — well below the 256 MB budget.
- **No runaway collections**: All 10 tracked collections show stable sizes post-warmup.

### Before 1.0 (full release)

1. **Enable profiler instrumentation** (`SimKernel.ProfilingEnabled = true`) in a 100-node stress campaign and capture `GetTickProfile()` output. Identify which Tier 3/4 systems dominate at larger scale.
2. **NpcTradeSystem spatial bucketing**: If 100-node profiling shows NpcTradeSystem > 2 ms, introduce a region-partitioned lookup so each NPC only evaluates markets within its current trade zone (~10 nodes), reducing O(fleets * 100) to O(fleets * 10).
3. **Price history cap audit**: Confirm `IntelSystem.ProcessPriceHistory` ring buffer cap is enforced at runtime — memory stayed at 4.33 MB in testing, which suggests the cap is working, but add an explicit size assertion in PerfBudgetTests.
4. **GC pressure baseline**: Measure GC pause frequency under the profiler during a 10-minute play session. .NET 8 background GC rarely causes hitches, but NpcTradeSystem and LogisticsSystem allocate per-tick lists that may warrant pooling at higher fleet counts.
5. **Extend TickBudgetTests to 50-node galaxy**: Current `TickBudgetTests` uses a 20-node galaxy. Add a 50-node variant with the same 20 ms budget to catch regressions at EA campaign scale before they reach players.

### Keep watching

- The tick budget test (`MaxAverageTickMs = 20.0 ms`) is deliberately conservative. The actual EA target should be **8 ms** (half-frame). Consider tightening the TickBudgetTests budget constant to 8 ms once 50-node coverage is added.

---

## 8. Test Coverage Summary

| Test | Result | Gate |
|---|---|---|
| `PerfBudget_Slice3_V0_AverageTickTime_WithinBudget_And_ReportDeterministic` | PASS (3.343 ms avg) | GATE.T46.PERF.TICK_BUDGET |
| `PerfBudget_Slice4_V0_AverageTickTime_WithinBudget_And_ReportDeterministic` | PASS (3.873 ms avg) | GATE.T46.PERF.TICK_BUDGET |
| `AllIndustrySystems_1000Ticks_UnderBudget` | PASS | GATE.T46.PERF.TICK_BUDGET |
| `PerfBudget_WithActiveConstruction` | PASS | GATE.T46.PERF.TICK_BUDGET |
| `SimState_50NodeGalaxy_MemoryUnder256MB` | PASS (4.33 MB) | GATE.T46.PERF.MEMORY_BUDGET.001 |
| `SimState_CollectionCounts_Reasonable` | PASS (all within limits) | GATE.T46.PERF.MEMORY_BUDGET.001 |
| `AverageTick_WithinBudget_AcrossSeeds` | PASS | GATE.X.PERF.TICK_BUDGET.001 |

All 7 performance tests pass. Total test suite: 1324 tests passing.
