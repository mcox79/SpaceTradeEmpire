# Warfront Mechanics — Quick Reference & Implementation Checklist

## One-Page Comparison: Warfront Systems Across Games

### Mechanic Depth Comparison (⭐ = feature breadth)

```
Stellaris:       ⭐⭐⭐⭐⭐ (war goals, claims, exhaustion, ideology, fleet quality)
Sins of a Solar: ⭐⭐⭐⭐   (territory, objectives, sustainability, attrition)
X4 Foundations:  ⭐⭐⭐⭐   (NPC autonomy, economic blockade, dynamic fleets)
Distant Worlds2: ⭐⭐⭐    (weariness, diplomacy, trade-focused)
Starsector:      ⭐⭐⭐    (sector control, market disruption, casualties)
Your Game:       ⭐⭐     (demand shocks, embargoes, attrition resolver)
```

### Territory Control Mechanic Matrix

| Game | Model | Shift Condition | Visibility | NPC Behavior | De-escalation |
|------|-------|---|---|---|---|
| **Stellaris** | Claims-based (cost in influence, EC) | War goal completion (50% threshold) | Map shows claims, war progress | Auto-expansion by borders | Status quo peace |
| **Sins2** | Direct control (planet allegiance) | Battle victory at planet | Planets glow per faction | Continuous redeployment | Cease-fire tickers |
| **X4** | Sector ownership (station + gates) | Fleet battles + occupation | Dynamic sector colors | Patrols establish control | Diplomatic truce |
| **Starsector** | Emergent (NPC patrol presence) | Patrol fleet wins at location | Sector "stability" (number) | Fleets seek supply, avoid losses | Negotiated peace |
| **Space Trade Empire** | Faction-assigned (static) | **[NOT IMPLEMENTED]** | Disc color fixed | Patrol response changes | Supply threshold only |

**Key Insight**: All AAA games tie **visibility to gameplay**. Your discs don't move → wars feel static.

---

## Critical Implementation Sequence

### Phase 0: Foundation (Already Done ✓)
- [x] WarfrontState entity with Intensity enum
- [x] WarfrontDemandSystem (wartime demand multipliers)
- [x] EmbargoState (good blocking)
- [x] TerritoryRegime (4-state access control)
- [x] StrategicResolverV0 (deterministic combat)
- [x] WarfrontObjective schema (waiting for instantiation)

### Phase 1: Map Visibility (CRITICAL)
**Time estimate: 20-24 hours**

**1.1 Territory Capture State Machine**
- [ ] Add `ControllingFactionId` migration (nodes now mutable, not just initial state)
- [ ] Add `DominanceTicks` per objective
- [ ] Add `LastSupplyDeliveryTick` to WarfrontState
- [ ] Implement capture logic: `if DominanceTicks >= THRESHOLD (20) then ControllingFactionId = DominantFactionId`
- [ ] Write tests: territory capture, control persistence, regime update
- **Cost**: 6-8 hours

**1.2 Objective Seeding at Worldgen**
- [ ] In GalaxyGenerator.SeedWarfrontsV0(), instantiate 2-3 objectives per warfront
- [ ] Objectives anchored to contested nodes
- [ ] Initialize control to current node owner
- [ ] Golden hash includes objective placement
- **Cost**: 2-3 hours

**1.3 Warfront Escalation Loop**
- [ ] Implement natural escalation: no supplies for N ticks → intensity += 1
- [ ] Implement natural de-escalation: both sides below 30% strength → intensity -= 1
- [ ] Add escalation countdown to WarfrontState
- [ ] Golden hash includes escalation schedule deterministically
- **Cost**: 4-5 hours

**1.4 Galaxy Map UI Update**
- [ ] Territory discs now change color on control shift (real-time)
- [ ] Toast: "Valorin gains [Node]" when control shifts
- [ ] Toast: "Warfront [Name] escalates to OpenWar" on escalation
- [ ] Layer: objective icons (depot, relay, factory)
- **Cost**: 6-8 hours

---

### Phase 2: Economic Consequence (HIGH PRIORITY)
**Time estimate: 12-16 hours**

**2.1 Supply Chain Instantiation**
- [ ] Audit production recipes in TradeGoodsContentV0.cs: only 3/9 are instantiated as industry sites
- [ ] Create industry sites for remaining 6 recipes
- [ ] Validate at worldgen: no recipe lacks production (crash if so)
- [ ] Golden hash includes all production sites
- **Cost**: 8-10 hours (6 recipes × 90 min design/test each)

**2.2 Embargo Supply Chain Break**
- [ ] Embargo blocks intermediate goods (not just end goods)
- [ ] Downstream producers detect missing inputs, stall
- [ ] Stalled production → output drops → prices spike
- [ ] Test: embargo Munitions → Components can't produce → Refits price spikes
- **Cost**: 4-6 hours

---

### Phase 3: Player Agency (MEDIUM PRIORITY)
**Time estimate: 10-12 hours**

**3.1 Warfront HUD Tab**
- [ ] EmpireDashboard: "Warfront" section showing all active warfronts
- [ ] Per warfront: name, combatants, intensity, fleetsA vs B (bar chart)
- [ ] Objectives list with current control + dominance progress
- [ ] Supply ledger: "You delivered 450 Munitions to WF_hot_0"
- [ ] Escalation countdown: "Escalates in 87 ticks without supplies"
- **Cost**: 6-7 hours

**3.2 Patrol Attrition Feedback Loop**
- [ ] On player fleet victory: `wf.FleetStrengthA += 5` (symbolic victory)
- [ ] On player supply delivery: `wf.FleetStrengthA += (units / 50)` (sustain)
- [ ] Surface in WarConsequenceSystem: "Your victory damaged the enemy fleet. FleetB -15."
- [ ] Bridge query: `GetWarfrontContributionV0()` shows player's supply + combat impact
- **Cost**: 3-4 hours

---

### Phase 4: NPC Behavior Scaling (NICE-TO-HAVE)
**Time estimate: 8-10 hours**

**4.1 Patrol Behavior Warfront Scaling**
- [ ] At Tension: normal patrol routes, scan only
- [ ] At Skirmish+: increased patrol frequency, more aggressive scans
- [ ] At OpenWar+: patrol wings (squad behavior), full attack-on-sight for hostile cargo
- [ ] Attrition scales patrol availability: if fleet depleted, fewer patrols
- **Cost**: 5-6 hours

**4.2 Route Avoidance at Warfronts**
- [ ] NPC traders avoid contested nodes (path + 50% cost penalty)
- [ ] NPC supply fleets still use routes but accept higher risk
- [ ] Creates scarcity at warfront markets (player opportunity)
- **Cost**: 3-4 hours

---

## Tuning Knobs (Tweaks Reference)

### Intensity Escalation
```csharp
public class WarfrontTweaksV0
{
    // Existing
    public const int MunitionsDemandMultiplierPct = 400;
    public const int CompositesDemandMultiplierPct = 250;
    public const int FuelDemandMultiplierPct = 300;

    // NEW
    public const int EscalationIntervalTicks = 200; // No supplies for 200 ticks → escalate
    public const int EscalationThresholdMinStrength = 40; // Both sides must be > 40 to escalate
    public const int DeEscalationThresholdMinStrength = 30; // Both sides < 30 → de-escalate

    // Territory Capture
    public const int TerritoryCaptureTicks = 20; // Dominance for 20 ticks = control shift
    public const int InitialObjectiveCount = 3; // 3 objectives per warfront

    // Attrition per intensity level
    public const int AttritionBasePerTick = 2;
    // At Skirmish (2): 2*2 = 4/tick → 100 ticks to defeat
    // At OpenWar (3): 2*3 = 6/tick → 67 ticks
    // At TotalWar (4): 2*4 = 8/tick → 50 ticks
}
```

### Warfront Status Board (Expected HUD Output)

```
╔════════════════════════════════════════════════════════════════╗
║ WARFRONTS                                                      ║
╠════════════════════════════════════════════════════════════════╣
║                                                                ║
║ Valorin vs Weavers — HOT WAR — OpenWar (Intensity 3)          ║
║ Started: 45 days ago  |  Duration: 1800 ticks                 ║
║                                                                ║
║ Fleet Strength:                                                ║
║   Valorin:  ████████░░ 82%                                    ║
║   Weavers:  ████░░░░░░ 45%                                    ║
║                                                                ║
║ Contested Nodes: Proxima, Kepler, Vega [3/5 held by Valorin]  ║
║                                                                ║
║ Objectives:                                                    ║
║   ⚙ SupplyDepot @ Proxima — Valorin [14/20 ticks to hold]     ║
║   ⦿ CommRelay @ Kepler — Neutral [2/20 ticks to Weavers]      ║
║   ◼ Factory @ Vega — Weavers [HELD]                           ║
║                                                                ║
║ Escalation Countdown: 73 ticks without supplies               ║
║                                                                ║
║ Your Contribution This Session:                               ║
║   Munitions: 450 units delivered                              ║
║   Composites: 200 units delivered                             ║
║   Reputation Gain: +30 (Valorin) / -15 (Weavers)              ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
```

---

## Unit Tests to Write

### Test 1: Territory Capture Determinism
```csharp
[Test]
public void WarfrontObjective_CaptureAfterDominanceThreshold_ControlShifts()
{
    // Given: Objective held by FactionA, DominanceTicks = 0
    // When: Set DominantFactionId = FactionB, tick 20 times
    // Then: ControllingFactionId should shift to FactionB

    var wf = new WarfrontState { ... };
    var obj = new WarfrontObjective { ControllingFactionId = "A", DominanceTicks = 0 };

    for (int i = 0; i < 20; i++)
    {
        obj.DominanceTicks++;
    }

    // Trigger capture check
    if (obj.DominanceTicks >= THRESHOLD)
    {
        obj.ControllingFactionId = obj.DominantFactionId;
    }

    Assert.AreEqual("B", obj.ControllingFactionId);
}
```

### Test 2: Escalation Without Supply
```csharp
[Test]
public void Warfront_NoSupplyForNTicks_EscalatesIntensity()
{
    var wf = new WarfrontState
    {
        Intensity = WarfrontIntensity.Tension,
        LastSupplyDeliveryTick = 0,
        FleetStrengthA = 80,
        FleetStrengthB = 75
    };

    // Tick 200 times, no supply delivery
    for (int tick = 1; tick <= 200; tick++)
    {
        WarfrontEvolutionSystem.ProcessWarfrontEscalation(wf, tick);
    }

    // Should have escalated once
    Assert.AreEqual(WarfrontIntensity.Skirmish, wf.Intensity);
}
```

### Test 3: Supply Chain Break Cascade
```csharp
[Test]
public void MarketSystem_EmbargoBoundsIntermediateGood_DownstreamProducerStalls()
{
    // Setup: Munitions → Components → Refits chain
    // Action: Embargo Munitions at node
    // Expected: Components producer at that node stalls, price spikes downstream

    var state = CreateTestState();

    // Embargo Munitions
    EmbargoState embargo = new() { GoodId = WellKnownGoodIds.Munitions, ... };
    state.Embargoes[embargo.Id] = embargo;

    // Tick production
    ProductionSystem.Process(state);
    IndustrySystem.Process(state);

    // Components output should be zero (missing Munitions input)
    var componentsMarket = state.Markets[productionNodeMarketId];
    Assert.IsTrue(componentsMarket.Inventory[Components] < expected);
}
```

---

## Bridge Query Methods (Implement for HUD)

```csharp
// In SimBridge.Warfront.cs (partial)

public WarfrontStatusV0 GetWarfrontStatusV0(string warfrontId)
{
    TryExecuteSafeRead(state =>
    {
        var wf = state.Warfronts[warfrontId];
        return new WarfrontStatusV0
        {
            Id = wf.Id,
            Intensity = (int)wf.Intensity,
            IntensityName = wf.Intensity.ToString(),
            CombatantA = wf.CombatantA,
            CombatantB = wf.CombatantB,
            FleetStrengthA = wf.FleetStrengthA,
            FleetStrengthB = wf.FleetStrengthB,
            ContestedNodeCount = wf.ContestedNodeIds.Count,
            ObjectiveSnapshots = wf.Objectives.Select(o => new ObjectiveSnapshotV0
            {
                NodeId = o.NodeId,
                Type = o.Type.ToString(),
                ControllingFaction = o.ControllingFactionId,
                DominanceTicks = o.DominanceTicks,
                CaptureThreshold = TERRITORY_CAPTURE_THRESHOLD
            }).ToList(),
            LastSupplyDeliveryTick = wf.LastSupplyDeliveryTick,
            EscalationCountdownTicks = Math.Max(0,
                wf.LastSupplyDeliveryTick + ESCALATION_INTERVAL - _kernel.State.Tick)
        };
    });
}

public Dictionary<string, int> GetPlayerWarSupplyLedgerV0()
{
    TryExecuteSafeRead(state =>
    {
        // Sum all deliveries by good across all warfronts
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var wfId in state.WarSupplyLedger.Keys)
        {
            if (state.WarSupplyLedger.TryGetValue(wfId, out var goodLedger))
            {
                foreach (var goodId in goodLedger.Keys)
                {
                    result.TryGetValue(goodId, out int existing);
                    result[goodId] = existing + goodLedger[goodId];
                }
            }
        }
        return result;
    });
}
```

---

## Gotchas & Testing Heuristics

### Gotcha 1: Golden Hash Invalidation
**Problem**: Add objectives to worldgen → golden hash changes → all existing tests fail.

**Solution**:
1. Update GalaxyGenerator golden hash baseline FIRST
2. Re-run all determinism tests
3. Commit golden hash update before merging feature

**Test Command**:
```bash
dotnet test SimCore.Tests/SimCore.Tests.csproj -c Release --filter "Determinism" --nologo -v q
```

### Gotcha 2: Warfront Intensity Enum Arithmetic
**Problem**: `(WarfrontIntensity)((int)wf.Intensity + 1)` can overflow to (int)5.

**Solution**: Always clamp before cast:
```csharp
if (wf.Intensity < WarfrontIntensity.TotalWar)
{
    wf.Intensity = (WarfrontIntensity)((int)wf.Intensity + 1);
}
```

### Gotcha 3: Territory Regime Hysteresis
**Problem**: Regime flickers between Guarded and Restricted as warfront intensity oscillates.

**Solution**: Use existing `NodeTerritoryRegime` + `NodeProposedRegime` hysteresis:
- Compute proposed regime
- Only commit if different for 5+ ticks
- Prevents UI flicker

---

## Playtesting Checklist

### Session 1: Territory Capture Feels Real
- [ ] Start game, note initial territory disc colors
- [ ] Play to first objective capture (target: 200 ticks)
- [ ] Verify: disc changes color on control shift
- [ ] Verify: toast appears "Faction gains [Node]"
- [ ] Verify: trading regime at that node changes (new tariffs)

### Session 2: Escalation Without Supply
- [ ] Start game, cold warfront (Tension)
- [ ] Play 250 ticks without supplying warfront
- [ ] Verify: at tick 200, toast "Warfront escalates"
- [ ] Verify: intensity increases, munitions price spikes
- [ ] Verify: HUD countdown shows escalation timer

### Session 3: Supply Delivery Feedback
- [ ] At active warfront, deliver 200 Munitions
- [ ] Verify: toast "Supply delivered, warfront stabilizes"
- [ ] Verify: LastSupplyDeliveryTick updates
- [ ] Verify: FleetStrength increases for receiving faction
- [ ] Verify: NPC traders around that faction become more friendly (lower tariffs)

### Session 4: Economic Cascade
- [ ] Embargo active warfront's Munitions
- [ ] Verify: Components producer at that node stalls
- [ ] Verify: Components price spikes 2x+ within 10 ticks
- [ ] Verify: Refits margin becomes negative (can't source Components)
- [ ] Verify: UI diagnostic "Trade route broken: Munitions embargo"

---

## Formulas Reference

### Warfront Demand Multiplier (Existing)
```
Munitions:  100 + (intensity - 1) * 100  [100%, 100%, 200%, 300%, 400% at Peace-TotalWar]
Fuel:       100 + (intensity - 1) * 100
Composites: 100 + (intensity - 1) * 66   [66%, 66%, 133%, 200%, 266%]
```

### Neutrality Tax (Existing)
```
Base:      0 bps
Skirmish:  +500 bps
OpenWar:   +1000 bps
TotalWar:  +1500 bps
```

### Attrition Per Tick (NEW)
```
Base:     2 HP/tick
Skirmish: 2 * 2 = 4/tick
OpenWar:  2 * 3 = 6/tick
TotalWar: 2 * 4 = 8/tick
```

### Escalation Threshold (NEW)
```
No supplies for N ticks → escalate (if both sides > 40%)
Escalation interval: 200 ticks
De-escalate if both sides < 30%
```

---

## GDScript Integration Patterns

### Query Warfront Status for HUD Display
```gdscript
func _on_show_warfront_panel():
    var status = bridge.call("GetWarfrontStatusV0", warfront_id)

    var intensity_bar = ProgressBar.new()
    intensity_bar.value = (status["FleetStrengthA"] / 100.0) * 100.0
    intensity_bar.custom_minimum_size = Vector2(200, 30)
    add_child(intensity_bar)

    var escalation_label = Label.new()
    escalation_label.text = "Escalates in %d ticks" % status["EscalationCountdownTicks"]
    add_child(escalation_label)
```

### Toast on Territory Shift
```gdscript
func _on_warfront_update():
    # Polled every 10 ticks
    var current_status = bridge.call("GetWarfrontStatusV0", warfront_id)

    if current_status["ObjectiveSnapshots"].size() > 0:
        var obj = current_status["ObjectiveSnapshots"][0]
        if obj["ControllingFaction"] != previous_control:
            var toast = "%s gains %s" % [obj["ControllingFaction"], obj["NodeId"]]
            game_manager.call("show_toast", toast, 3.0)
            previous_control = obj["ControllingFaction"]
```

---

## Summary: 80/20 Implementation

**If you have only 1 week**, implement:
1. Territory capture (4-5 hours)
2. Objectives seeding (2-3 hours)
3. Escalation loop (4-5 hours)
4. Warfront HUD tab (6-7 hours)

**Result**: Warfronts visibly move on map, intensify over time, have strategic depth.

**If you have 2 weeks**, add:
5. Production chain instantiation (8-10 hours)
6. Patrol attrition feedback (3-4 hours)
7. Supply chain break detection (4-6 hours)

**Result**: Wars have economic teeth. Supply chains break. Your support feels meaningful.

This is a **force multiplier** on what you already have. You've got the combat logic, the economic simulation, the tariff system. You just need the visibility layer (territory) and the escalation loop (time-based pressure).

