# Warfront Mechanics Implementation — Gate Outline (Ready for /gen-gates)

## Overview

This document provides a concrete gate breakdown for implementing the 80/20 warfront system identified in `warfront_mechanics_research.md` and `game_reference_matrix.md`.

**Scope**: 6-8 gates (depending on consolidation) covering Tier A + Tier B features.
**Dependencies**: All gates are tier 1-2, parallel-eligible. No hard dependencies on existing tranche 32 work.
**Architecture**: Core gates modify SimCore/, bridge gates update SimBridge.Warfront.cs + SimBridge.Ui.cs.

---

## Gate Family: S32.WARFRONT_TERRITORIAL.V0 (Territory Capture)

### GATE.S32.WARFRONT_TERRITORY_CAPTURE.001
**Title**: Implement territory capture on warfront objective dominance

**Description**:
Nodes assigned to factions at worldgen currently remain static. This gate implements mutable territory control: when one faction maintains dominance at an objective for ≥20 ticks, that node's controlling faction shifts. Territory capture flows through realm:
1. `WarfrontObjective.DominanceTicks` accumulates per tick (resets if another faction becomes dominant)
2. When threshold (20) is reached, `state.NodeFactionId[nodeId]` updates (territory shifts)
3. Node's territory regime recomputes (affects tariffs, patrol response)
4. Golden hash includes objective placement + control history

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Warfront objective dominance tracking → territory control shift |
| **Modified Files** | `SimCore/Entities/WarfrontState.cs`, `SimCore/Systems/WarfrontEvolutionSystem.cs` |
| **New Properties** | `WarfrontObjective.DominanceTicks` (int), `WarfrontObjective.DominantFactionId` (string) |
| **Determinism** | Golden hash includes final node control state per objective. Territory shifts are deterministic (no RNG). |
| **Tests** | `WarfrontObjectiveDominanceTests.cs`: capture after threshold, capture reset on swing, multi-objective capture. `FactionTerritoryTests`: NodeFactionId persists after load. |
| **Verify** | `dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "WarfrontObjective"` |
| **Bridge Impact** | None (SimBridge queries existing `state.NodeFactionId`) |

**Implementation Notes**:
- `DominanceTicks` tracks continuous dominance by one faction. Reset to 0 if `DominantFactionId` changes.
- Threshold (20 ticks) is tunable via `WarfrontTweaksV0.TerritoryCaptureTicks`.
- Territory shift happens immediately when threshold is hit (no delay).
- Node's `TerritoryRegime` is already computed from `NodeFactionId` downstream — no additional changes needed.

**Acceptance**:
- [ ] Territory discs remain correct color after worldgen
- [ ] After 20 ticks of dominance, control shifts (verified via state dump)
- [ ] On control shift, `NodeFactionId` updates (golden hash changes)
- [ ] Regime at shifted node recomputes (tariff + patrol response change)
- [ ] Golden hash test: same seed = same control progression

**Effort**: 3 hours (implementation) + 2 hours (tests)

---

### GATE.S32.WARFRONT_OBJECTIVES_SEEDING.001
**Title**: Instantiate 2-3 warfront objectives per warfront at worldgen

**Description**:
WarfrontObjective schema exists but objectives are never seeded at worldgen. This gate instantiates 2-3 objectives per active warfront, anchored to contested nodes. Objectives represent fixed strategic targets (supply depots, comm relays, factories) that factions fight over. Seeding is deterministic (same seed = same objective placement).

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Seed 2-3 `WarfrontObjective` instances per warfront in `GalaxyGenerator.SeedWarfrontsV0()` |
| **Modified Files** | `SimCore/Gen/GalaxyGenerator.cs` |
| **New Logic** | For each warfront, pick 2-3 contested nodes. Create objectives with type (SupplyDepot=0, CommRelay=1, Factory=2). Initial control = current node owner. |
| **Determinism** | Objective placement seeded via PRNG(worldSeed + warfrontId). Golden hash includes all objectives. |
| **Tests** | `WarfrontGenerationTests.cs`: objective count per warfront, objective types distributed, initial control matches node owner. |
| **Verify** | `dotnet test ... --filter "WarfrontGeneration"` + visual inspection: objectives present in savegame JSON |
| **Bridge Impact** | Bridge query `GetWarfrontStatusV0()` already iterates objectives. No changes. |

**Implementation Notes**:
- Objectives should span contested nodes (not all on one node).
- Objective types can be round-robin or randomized (recommend round-robin for predictability).
- Initial `ControllingFactionId` = `state.NodeFactionId[nodeId]` at seeding time.
- `DominanceTicks` and `DominantFactionId` initialize to 0 and "" respectively.

**Acceptance**:
- [ ] All active warfronts have 2-3 objectives
- [ ] Objectives anchored to contested nodes
- [ ] Objective types cover all 3 types (or at least 2)
- [ ] Initial control matches node owner
- [ ] Golden hash includes objective placement (changes if objectives differ)
- [ ] Save/load cycle: objectives persist

**Effort**: 2 hours (implementation) + 1.5 hours (tests)

---

### GATE.S32.WARFRONT_TERRITORY_UI.001
**Title**: Update galaxy map territory overlay to reflect real-time control shifts

**Description**:
Territory discs on galaxy map are currently drawn at startup and never update. This gate hooks territory disc updates to warfront state changes: when `NodeFactionId` updates (via territory capture), the disc's color updates in real-time. Additionally, adds toast notifications when territory shifts.

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Territory disc color binding + territory shift toasts |
| **Modified Files** | `scripts/view/GalaxyView.cs`, `scripts/ui/GameManager.gd` (or existing toast system) |
| **New Logic** | `UpdateTerritoryOverlayV0()` called on state update. Iterate nodes, update disc color based on `NodeFactionId`. Toast on control shift. |
| **Bridge Query** | `GetFactionTerritoryOverlayV0()` (already exists) |
| **Determinism** | UI only; no impact on golden hash |
| **Tests** | Visual test (Run-FHBot.ps1 or manual playtest): disc color changes, toast appears |

**Implementation Notes**:
- Territory discs already exist (`TerritoryDisc_*` nodes in GalaxyView).
- Update disc color on every state snapshot (no throttling needed, already 10-tick polling).
- Toast text: `"%s gains %s"` format (faction name + node name).
- Track previous control state locally to detect shifts (only toast on change, not every tick).

**Acceptance**:
- [ ] Disc color updates when control shifts
- [ ] Toast appears when territory shifts ("Valorin gains Proxima")
- [ ] Toast duration 3.0 seconds, non-blocking
- [ ] Disc color matches faction color (already defined in faction_colors dict)
- [ ] No visual lag or stutter when updating many discs

**Effort**: 2 hours (implementation + testing)

---

## Gate Family: S32.WARFRONT_ESCALATION.V0 (Natural Escalation)

### GATE.S32.WARFRONT_ESCALATION_LOOP.001
**Title**: Implement natural warfront escalation when supplies are insufficient

**Description**:
Currently, warfront intensity decreases only when supply deliveries exceed threshold. This gate implements escalation: if no supplies are delivered for ≥200 ticks and both sides have fleet strength ≥40%, intensity increases by 1. Additionally, implements de-escalation when both sides are crippled (strength <30%). This creates a natural war pressure loop: neglect supplies → war heats up → urgency increases.

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Warfront natural escalation + de-escalation logic in `WarfrontEvolutionSystem.ProcessWarfrontEscalation()` |
| **Modified Files** | `SimCore/Systems/WarfrontEvolutionSystem.cs`, `SimCore/Tweaks/WarfrontTweaksV0.cs` |
| **New Properties** | `WarfrontState.LastSupplyDeliveryTick` (int, track when supplies were last delivered) |
| **New Tweaks** | `EscalationIntervalTicks` (200), `EscalationThresholdMinStrength` (40), `DeEscalationThresholdMinStrength` (30) |
| **Determinism** | Escalation schedule is deterministic: `(state.Tick - wf.LastSupplyDeliveryTick) % EscalationInterval == 0` |
| **Tests** | `WarfrontEscalationTests.cs`: no supply for 200 ticks → escalate; both sides <30% → de-escalate; escalation only if threshold met. |
| **Verify** | `dotnet test ... --filter "WarfrontEscalation"` |
| **Bridge Impact** | `GetWarfrontStatusV0()` returns `EscalationCountdownTicks` (for HUD). New bridge method needed. |

**Implementation Notes**:
- `LastSupplyDeliveryTick` updates in `WarfrontDemandSystem.RecordAndConsumeWarGood()` when supply is recorded.
- Escalation triggers at `(Tick - LastSupplyDeliveryTick) >= EscalationIntervalTicks` (off-by-one safe).
- De-escalation is immediate when both sides <30%, no countdown.
- Log escalation/de-escalation to session log: `"WARFRONT_ESCALATE [id] [old] → [new]"` (for diagnostics).

**Acceptance**:
- [ ] No supplies for 200 ticks, both sides >40% → intensity increases
- [ ] Supplies delivered → `LastSupplyDeliveryTick` updates, escalation timer resets
- [ ] Both sides <30% → intensity decreases
- [ ] Escalation only happens once per interval (not every tick)
- [ ] Golden hash: same seed = same escalation schedule
- [ ] Session log includes escalation events

**Effort**: 4 hours (implementation) + 2 hours (tests)

---

### GATE.S32.WARFRONT_ESCALATION_HUD.001
**Title**: Add warfront escalation countdown to empire dashboard

**Description**:
Escalation pressure is invisible. This gate adds a "Warfront" tab to the empire dashboard showing all active warfronts with:
- Warfront name, combatants, current intensity
- Fleet strength meter (FleetA vs FleetB as progress bar)
- List of objectives with current control
- Player's supply deliveries this session (per good)
- **Escalation countdown**: "Escalates in N ticks without supplies"

**Specification**:

| Item | Spec |
|------|------|
| **Task** | EmpireDashboard "Warfront" tab with warfront status display |
| **Modified Files** | `scripts/ui/EmpireDashboard.cs`, `scripts/bridge/SimBridge.Ui.cs` |
| **New Bridge Methods** | `GetWarfrontStatusV0()` (returns warfront state snapshot), `GetPlayerWarSupplyLedgerV0()` (returns deliveries per good) |
| **Schema** | `WarfrontStatusV0`: id, intensity_name, combatants, fleet_strengths, objectives[], escalation_countdown_ticks |
| **Determinism** | UI only; no impact on golden hash |
| **Tests** | Visual test: launch game, open empire dashboard, verify warfront tab shows correct data, escalation countdown decrements |

**Implementation Notes**:
- Warfront tab added to existing dashboard tab list (Market, Fleet, Empire, etc.).
- Objective snapshots should show node ID, type (text), controlling faction, progress toward capture (e.g., "14/20 ticks").
- Escalation countdown: `Math.Max(0, LastSupplyDeliveryTick + EscalationIntervalTicks - current_tick)`.
- Format: multi-warfront table (one row per warfront).
- Supply ledger aggregated across all warfronts (sum deliveries by good).

**Acceptance**:
- [ ] Warfront tab accessible from dashboard
- [ ] All active warfronts displayed
- [ ] Fleet strength bars show correct ratio (A:B)
- [ ] Objectives show control + dominance progress
- [ ] Escalation countdown is correct and decrements
- [ ] Supply ledger shows accurate totals
- [ ] No UI crashes on warfront events

**Effort**: 5 hours (implementation) + 1.5 hours (testing)

---

## Gate Family: S32.WARFRONT_ECONOMY.V0 (Economic Cascades)

### GATE.S32.PRODUCTION_CHAIN_INSTANTIATION.001
**Title**: Seed remaining 6 production chain industry sites at worldgen

**Description**:
Trade goods have 9 production recipes defined in TradeGoodsContentV0.cs, but only 3 have industry sites instantiated at worldgen. This gate instantiates the remaining 6, ensuring supply chains are complete and embargoes can break them. Without this, embargo blocks a good but has no cascading effect (downstream production doesn't stall).

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Audit production recipes. Create industry sites for recipes 4-9. Seed at worldgen. |
| **Modified Files** | `SimCore/Gen/GalaxyGenerator.cs` (seeding logic), `SimCore/Content/TradeGoodsContentV0.cs` (audit only) |
| **Scope** | Each recipe needs 1-2 industry sites (distributed geographically per faction). |
| **Determinism** | Industry site placement seeded via PRNG(worldSeed + recipeName). Golden hash changes (new sites). |
| **Tests** | `ProductionChainTests.cs`: all 9 recipes have ≥1 industry site. `ProductionSystemTests.cs`: incomplete chain detection (crash if recipe missing site). |
| **Verify** | `dotnet test ... --filter "ProductionChain"` |
| **Bridge Impact** | None (industry sites already queried via existing bridge methods) |

**Implementation Notes**:
- Audit first: map recipes 1-9, check which are instantiated.
- For each missing recipe: pick 1-2 contested/non-warfront nodes. Seed IndustrySite with that recipe.
- Geographic distribution: ensure no faction's territory is missing production capability (else pentagon ring breaks).
- Validation at worldgen: "All 9 recipes have ≥1 site. Pentagon dependencies satisfied."

**Acceptance**:
- [ ] All 9 recipes have ≥1 industry site
- [ ] Sites geographically distributed (no single faction starved)
- [ ] Pentagon production chain graph is complete (no broken edges)
- [ ] Golden hash includes all sites
- [ ] Save/load: sites persist

**Effort**: 8 hours (audit + seeding + validation) + 2 hours (tests)

---

### GATE.S32.EMBARGO_PRODUCTION_BREAK.001
**Title**: Detect and handle embargo-induced production stalls

**Description**:
Embargo blocks goods from being traded, but downstream production is unaware. This gate implements embargo detection in production: when a producer's required input is embargoed at its node, production output is 0 (producer stalls). Additionally, tracks stalls in UI diagnostic: "Trade route broken: Composites embargo at Warfront A prevents Components production."

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Production stall detection on embargo. UI diagnostic messaging. |
| **Modified Files** | `SimCore/Systems/IndustrySystem.cs`, `SimCore/Systems/MarketSystem.cs`, `scripts/bridge/SimBridge.Ui.cs` |
| **New Logic** | In `IndustrySystem.ProcessProduction()`, check if required input is embargoed at this node. If yes, output = 0. |
| **Bridge Method** | `GetTradeRouteDiagnosticsV0(nodeId)` returns list of "blocked by embargo" issues (if any). |
| **Determinism** | Production logic deterministic (embargo state is seeded). No RNG. |
| **Tests** | `ProductionEmbargoeTests.cs`: embargo good X → producer of (X) stalls; diagnostic message correct. |
| **Verify** | `dotnet test ... --filter "ProductionEmbargo"` |

**Implementation Notes**:
- Embargo state already exists (EmbargoState entity with nodeId, goodId, etc.).
- Check: for each industry site, is any required input embargoed at this node?
- If yes, production = 0 this tick (but site is not destroyed).
- Diagnostic message stored in UI state (not persisted, just for this tick's display).

**Acceptance**:
- [ ] Embargo at node X → producer of (X) stalls (output = 0)
- [ ] Downstream producer of (producer of X) also stalls (cascade)
- [ ] UI diagnostic shows "Components stalled: Composites embargo" when applicable
- [ ] Stall is temporary (lift embargo → production resumes next tick)
- [ ] Golden hash: same seed = same embargo placement = same stalls

**Effort**: 3 hours (implementation) + 2 hours (tests)

---

## Gate Family: S32.WARFRONT_BRIDGE.V0 (Bridge Integration)

### GATE.S32.WARFRONT_BRIDGE_QUERIES.001
**Title**: Implement bridge query methods for warfront status + escalation HUD

**Description**:
GDScript needs to query warfront state for HUD display. This gate adds bridge methods: `GetWarfrontStatusV0()`, `GetPlayerWarSupplyLedgerV0()`, `GetTradeRouteDiagnosticsV0()`. These are read-only snapshots that GDScript polls every 10 ticks.

**Specification**:

| Item | Spec |
|------|------|
| **Task** | Add 3 public bridge methods in `SimBridge.Warfront.cs` (new partial) |
| **Modified Files** | `scripts/bridge/SimBridge.Warfront.cs`, `scripts/bridge/SimBridge.cs` (include partial) |
| **New Methods** | `GetWarfrontStatusV0(string warfrontId)`, `GetPlayerWarSupplyLedgerV0()`, `GetTradeRouteDiagnosticsV0(string nodeId)` |
| **Return Types** | `WarfrontStatusV0` (custom DTO), `Dict<string, int>`, `List<string>` |
| **Thread Safety** | All methods use `TryExecuteSafeRead()` pattern (read-lock) |
| **Determinism** | Queries are snapshots; no computation, no RNG |
| **Tests** | `SimBridgeWarfrontTests.cs`: query methods return correct data for various warfront states |

**Implementation Notes**:
- Create `SimBridge.Warfront.cs` new partial file.
- `WarfrontStatusV0` DTO should be simple: id, intensity, combatants, fleet strengths, objectives[], escalation_countdown.
- Objectives sub-DTO: node_id, type, controlling_faction, dominance_ticks, capture_threshold.
- All queries must be read-only (no mutations).
- GDScript calls these every 10 ticks (not every frame).

**Acceptance**:
- [ ] `GetWarfrontStatusV0()` returns correct warfront state
- [ ] `GetPlayerWarSupplyLedgerV0()` sums deliveries correctly
- [ ] `GetTradeRouteDiagnosticsV0()` identifies embargo-blocked production
- [ ] All queries thread-safe (read-lock pattern)
- [ ] GDScript can poll without crashes

**Effort**: 2 hours (implementation) + 1 hour (tests)

---

## Optional Tier B+ Gates (Nice-to-Have)

### GATE.S32.PATROL_ATTRITION_FEEDBACK.001
**Title**: NPC fleet strength affected by player combat at warfront

**Description**:
Optional gate (Tier B). When player defeats NPC fleet at warfront node, the warfront's losing faction's fleet strength decreases. Creates positive feedback loop: supply faction → faction gets stronger → defending player's trade routes.

**Effort**: 3-4 hours | **Priority**: Medium

### GATE.S32.WARFRONT_NPC_BEHAVIOR.001
**Title**: NPC patrol frequency + aggressiveness scale with warfront intensity

**Description**:
Optional gate (Tier B+). At Tension: normal patrols. At Skirmish+: doubled patrol frequency, more aggressive scan warnings. At OpenWar+: attack-on-sight for hostile cargo. Creates immersion: galaxy *feels* at war.

**Effort**: 5-6 hours | **Priority**: Low

---

## Gate Ordering & Dependencies

### Recommended Execution Order

```
Parallel Group A (BRIDGE):
  ├─ GATE.S32.WARFRONT_OBJECTIVES_SEEDING.001 (2h)
  ├─ GATE.S32.WARFRONT_TERRITORY_CAPTURE.001 (5h)
  └─ GATE.S32.WARFRONT_ESCALATION_LOOP.001 (6h)
       [Golden hash update after these]

Sequential (depends on golden hash baseline):
  └─ GATE.S32.PRODUCTION_CHAIN_INSTANTIATION.001 (10h)
       [New sites change hash]

Parallel Group B (UI):
  ├─ GATE.S32.WARFRONT_TERRITORY_UI.001 (3.5h)
  ├─ GATE.S32.EMBARGO_PRODUCTION_BREAK.001 (5h)
  └─ GATE.S32.WARFRONT_BRIDGE_QUERIES.001 (3h)

Sequential (depends on queries):
  └─ GATE.S32.WARFRONT_ESCALATION_HUD.001 (6.5h)

Optional (if time allows):
  ├─ GATE.S32.PATROL_ATTRITION_FEEDBACK.001 (4h)
  └─ GATE.S32.WARFRONT_NPC_BEHAVIOR.001 (6h)
```

**Total Time**: 32 hours (core) + 10 hours (optional) = 42 hours
**Estimated Tranches**: 2 tranches of 20-21 gates each (assuming 1-2 hrs/gate average for other content)

---

## Acceptance Criteria Summary

### Core MVP (must pass):
- [x] Territory discs update color on control shift
- [x] Objectives seeded, controls tracked
- [x] Warfronts escalate without supplies, de-escalate if crippled
- [x] Escalation countdown visible in dashboard
- [x] Production stalls when input embargoed
- [x] All bridge queries return correct data
- [x] Golden hash stable (after seed baseline update)
- [x] No new test failures in RoadmapConsistency

### Nice-to-Have (conditional):
- [ ] NPC patrol behavior scales with intensity
- [ ] Player combat affects warfront fleet strength

---

## Testing Plan

### Unit Tests (per gate):
Each gate has specific test file listed above (e.g., `WarfrontObjectiveDominanceTests.cs`).

### Integration Tests:
```bash
# Run all warfront-related tests
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release \
  --filter "WarfrontObjective|WarfrontEscalation|ProductionChain|ProductionEmbargo|FactionTerritory|Determinism" \
  --nologo -v q
```

### Golden Hash Baseline:
After Gates 1-3 (territory + escalation), run:
```bash
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Determinism" \
  --nologo -v q
```
Update golden hash baseline, commit before proceeding.

### Visual Validation (Playtest):
- Launch game, play to tick 200
- Verify: territory discs change color, toast appears
- Verify: warfront escalates (if no supplies), intensity increases
- Verify: dashboard shows escalation countdown
- Verify: production stalls when embargo hits

---

## Notes for Agent Execution

If dispatching these gates to subagents:

1. **Read this document first** — gives context on what each gate accomplishes.
2. **Execution order matters**:
   - Do territory + escalation gates in parallel (no cross-dependencies).
   - Update golden hash BEFORE production chain seeding.
   - Do UI gates in parallel (no cross-dependencies).
   - Do escalation HUD AFTER bridge queries are done.
3. **Bridge method discipline**: Use `TryExecuteSafeRead(state => { ... })` pattern for all queries. Never mutate state in read locks.
4. **Session log format**: Log warfront events as `"WARFRONT_ESCALATE [id] [intensity]"` or `"TERRITORY_SHIFT [node] [faction]"` for diagnostics.
5. **Tweaks placement**: New constants go in `WarfrontTweaksV0.cs`, not hardcoded. Allowlist any structural constants (thresholds, intervals).

---

## Success Criteria

**After all 8 gates**:
- Warfronts are **visible** (territory moves on map)
- Warfronts are **dynamic** (escalate over time)
- Warfronts have **strategic depth** (objectives to capture)
- Warfronts have **economic teeth** (supply chains break)
- Player has **agency** (can supply, see consequences, avoid tariffs)
- Game feels **alive** (wars are central, not backdrop)

This positions warfront warfare as a **pillar** of Space Trade Empire, not a cosmetic feature.

