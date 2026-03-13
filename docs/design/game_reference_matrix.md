# Warfront Mechanics Reference Matrix — What to Copy, What to Skip

## Mechanic Priority Matrix: Cost vs Impact

This matrix helps you decide which AAA features to implement based on your indie time budget.

```
                          High Impact
                              ▲
                              │
    ┌─────────────────────────┼─────────────────────────┐
    │                         │                         │
    │      Sins2 Territory    │     Stellaris Claims    │
    │      Territory Shift    │     War Goals           │
    │      (IMPLEMENT)        │     (SKIP for now)      │
    │                         │                         │
    │   Starsector NPC        │   Distant Worlds        │
    │   Autonomy             │   Weariness Curve       │
    │   (NICE-TO-HAVE)       │   (SKIP)                │
    │                         │                         │
Low ├──────────────────┬──────┼──────┬──────────────────┤ High
Effort                 │      │      │
    │   X4 Blockades   │      │      │  Stellaris        │
    │   (SKIP)         │      │      │  Exhaustion       │
    │                  │      │      │  Metric           │
    │                  │      │      │  (SKIP)           │
    └──────────────────┴──────┼──────┴──────────────────┘
                              │
                              │
                          Low Impact
```

---

## Decision Tree: Which Game Mechanic to Copy?

### START: "I want warfronts to feel alive."

**Question 1: Can the player see the war moving?**
- NO → Copy **Sins2 territory capture** (Priority 1)
- YES → Go to Q2

**Question 2: Does the war escalate naturally without player action?**
- NO → Copy **Starsector/Distant Worlds escalation loop** (Priority 2)
- YES → Go to Q3

**Question 3: Do wars break supply chains?**
- NO → Copy **Sins2 supply cascade** (Priority 3)
- YES → Go to Q4

**Question 4: Can players control strategic objectives?**
- NO → Copy **Sins2 objective capture** (Priority 4)
- YES → Warfronts are mature. Consider **X4 NPC autonomy** (Priority 5, nice-to-have)

---

## Feature Adoption Guide

### Priority 1: Copy This (Sins of a Solar Empire 2)
**Feature**: Territory Control + Visible Shift

**What Sins2 Does**:
- Each planet has a **controlling faction** field
- When a fleet battles at a planet and wins, control **immediately shifts**
- Planets display faction color gradient (territory overlay)
- Capturing a planet gives controlling faction production bonus there

**Why It's Essential**:
- Wars feel *static* without visible territory shifts
- Players lack feedback on who's winning
- Map becomes decoration instead of story

**How to Adapt It (Time: 12 hours)**:
```csharp
// Your nodes already have NodeFactionId (static)
// Add:
// NodeControllingFactionId (mutable)
// WarfrontObjective with DominanceTicks (cumulative)

// On combat victory at node:
if (victor_fleet.FactionId == dominant_faction && !objIsHeld)
{
    obj.DominanceTicks++;
    if (obj.DominanceTicks >= 20)
    {
        state.NodeFactionId[nodeId] = dominant_faction; // SHIFT
    }
}
```

**GDScript Impact**:
```gdscript
# Update territory disc color real-time
var color = faction_colors[state.NodeFactionId[node_id]]
territory_disc.modulate = color
```

---

### Priority 2: Copy This (Distant Worlds 2 + Starsector)
**Feature**: Natural Escalation Without Player Input

**What DW2/Starsector Do**:
- Wars escalate naturally over time (no supplies → intensity increases)
- Attrition drains fleet strength
- De-escalation when one side is crippled

**Why It's Essential**:
- Wars create passive pressure (player can't ignore forever)
- Replayability: different escalation paths per run
- Matches your 5-level intensity system perfectly

**How to Adapt It (Time: 8 hours)**:
```csharp
// WarfrontEvolutionSystem.ProcessWarfrontEscalation()
// Existing: intensity decreases when supplies exceed threshold
// NEW: intensity increases when no supplies for N ticks

if (state.Tick - wf.LastSupplyDeliveryTick >= ESCALATION_INTERVAL &&
    wf.FleetStrengthA >= ESCALATION_THRESHOLD &&
    wf.FleetStrengthB >= ESCALATION_THRESHOLD)
{
    if (wf.Intensity < WarfrontIntensity.TotalWar)
    {
        wf.Intensity++;
        LogToSessionLog($"Warfront {wf.Id} escalates to {wf.Intensity}");
    }
}
```

**GDScript Impact**:
```gdscript
# Show escalation countdown on HUD
var ticks_until_escalate = warfront.LastSupplyDeliveryTick + 200 - current_tick
if ticks_until_escalate > 0:
    hud.escalation_label.text = "Escalates in %d ticks" % ticks_until_escalate
```

---

### Priority 3: Copy This (Sins of a Solar Empire 2)
**Feature**: Strategic Objectives (Capture Points)

**What Sins2 Does**:
- 3 objective types: Supply Depot, Communications Relay, Industrial Facility
- Each objective provides gameplay benefit when controlled:
  - Supply Depot: reduces fleet attrition cost
  - Comm Relay: reveals enemy fleet positions
  - Factory: production bonus

**Why It's Essential**:
- Transforms wars from "who has bigger fleet" to "control these 3 points"
- Creates local battles instead of fleet-wide battles
- Objectives are *fixed*, reducing complexity vs destructible targets

**How to Adapt It (Time: 6 hours)**:

Your WarfrontObjective schema **already exists**. You just need to:
1. Instantiate objectives at worldgen (2-3 per warfront)
2. Wire up control logic (done in Priority 1's DominanceTicks)
3. Add UI layer to galaxy map (objective icons)

```csharp
// In GalaxyGenerator.SeedWarfrontsV0()
foreach (var wf in state.Warfronts.Values)
{
    var nodeIds = wf.ContestedNodeIds.Take(2).ToList();
    foreach (var nodeId in nodeIds)
    {
        var obj = new WarfrontObjective
        {
            NodeId = nodeId,
            Type = (ObjectiveType)(nodeIds.IndexOf(nodeId) % 3),
            ControllingFactionId = state.NodeFactionId[nodeId]
        };
        wf.Objectives.Add(obj);
    }
}
```

**GDScript Impact**:
```gdscript
# Draw objective icons on galaxy map
for obj in warfront.ObjectiveSnapshots:
    var icon = Icon.new()
    icon.type = obj.Type  # SupplyDepot, CommRelay, Factory
    icon.position = get_node_screen_pos(obj.NodeId)
    add_child(icon)
```

---

### Priority 4: Implement Conditionally
**Feature**: Supply Chain Cascade (Sins2-inspired)

**What Sins2 Does**:
- Capturing mineral-producing planets blocks mineral export
- Downstream production (alloys, ships) stalls
- Creates economic collapse cascades

**Why Conditional**:
- You already have embargo system ✓
- You have demand shocks ✓
- You lack only: connected production chains

**Implementation Threshold**:
- DO: If you instantiate remaining 6 production chains (currently 3/9 active)
- SKIP: If you leave chains uninstantiated (embargo works fine without them)

**Time Estimate**: 10-15 hours to instantiate + test 6 chains

**Payoff**: High (wars have economic *teeth*)

---

### Priority 5: Nice-to-Have (Skip for MVP)
**Feature**: NPC Autonomy (X4 Foundations)

**What X4 Does**:
- NPCs autonomously:
  - Build fleets
  - Establish patrol routes
  - Engage enemies
  - Negotiate trade
  - Expand territory

**Why Skip for MVP**:
- Requires robust NPC AI state machine
- You already have:
  - NPC fleet spawning ✓
  - Combat resolution ✓
  - Basic patrol routes ✓
- Return on investment: diminishing (only matters late-game)

**When to Revisit**: Post-Tranche 32 (after core warfront feel is solid)

---

### Skip Entirely (Wrong Fit for Your Game)
**Feature**: Stellaris War Goals + Claims Economy

**Why Skip**:
- Requires abstract "war goal" mechanic (capture X systems, destroy Y fleets, etc.)
- Your game is **not** an empire simulator — you're a pilot
- Players don't *declare* wars; wars happen around them
- Claims economy adds complexity without fitting your scope

**Alternative**: Your implicit "objective capture" system is cleaner. Copy Sins2, not Stellaris.

---

## Feature Comparison: What Each Game Does Best

### Territorial Movement (Who to Copy)

| Game | Model | Clarity | NPC Behavior | Your Take |
|------|-------|---------|---|---|
| **Sins2** | Planets glow faction color. Immediate. | Crystal clear. | Continuous redeployment. | ✅ COPY (territory shift feels real) |
| **Stellaris** | Claims overlay. Status quo at 50%. | Subtle. Claims hard to read. | Slower (diplomatic). | ❌ SKIP (too abstract) |
| **X4** | Sector stability % vs faction presence. | Emerges from patrol frequency. | Patrols establish control. | ⚠️ MAYBE (requires NPC AI) |
| **Starsector** | NPC fleets patrol. Control is implicit. | Invisible without checking reputation. | Fleets seek profit & safety. | ⚠️ MAYBE (lean, fits budget) |

**Verdict**: **Sins2 territorial model** is clearest + most achievable.

### Escalation (Who to Copy)

| Game | Model | Feels Natural? | Tunable? | Your Take |
|------|-------|---|---|---|
| **Distant Worlds** | Weariness accumulates (soft meter). De-escalates at threshold. | Yes | Very | ✅ COPY (simple + elegant) |
| **Stellaris** | War exhaustion hard stop at 100%. Peace forced. | Yes | Limited | ⚠️ MAYBE (complex) |
| **Starsector** | Casualty accumulation. Negotiated peace. | Yes | Very | ✅ COPY (you have casualty proxy in attrition) |
| **Sins2** | Attrition cost per unit. War unsustainable at full intensity. | Yes | High | ✅ COPY (already in your code!) |

**Verdict**: **Distant Worlds + Sins2 hybrid**: No supplies for N ticks → escalate. Attrition + fleet strength → sustainability.

### Economic Impact (Who to Copy)

| Game | Model | Visibility | Cascade? | Your Take |
|------|-------|---|---|---|
| **Stellaris** | Tariff + blockade. Slow, diplomatic. | Low | Weak | ❌ SKIP |
| **Sins2** | Supply lock. Cascades through production. | High | Yes | ✅ COPY (if you instantiate chains) |
| **X4** | Trade route destruction. Station blockade. | High | Yes | ⚠️ MAYBE (route system needed) |
| **Starsector** | Market disruption. Supply bottleneck. | High | Yes | ✅ COPY (matches your embargo) |

**Verdict**: **Sins2 + Starsector**: Embargo blocks goods. Downstream cascades. Economic pressure is *felt*.

### Strategic Objectives (Who to Copy)

| Game | Model | Depth | NPC Interestingness | Your Take |
|------|-------|---|---|---|
| **Sins2** | 3 types (Depot, Relay, Factory). Fixed locations. | Moderate | High (objectives are fought over) | ✅ COPY (you have schema!) |
| **Stellaris** | War goals are abstract (subjugation, etc.). | High | Medium (goals are diplomatic) | ❌ SKIP (doesn't fit pilot fantasy) |
| **X4** | Stations. Dynamic. Destroyed = gone forever. | Very High | High (economies collapse) | ❌ SKIP (too complex) |
| **Starsector** | None (pure combat). | Low | Low | ❌ SKIP |

**Verdict**: **Sins2 objectives**: 2-3 fixed objectives per warfront. Capture = control shift.

---

## The 80/20 Warfront Bundle

If you implement **exactly these features**, you get 80% of AAA warfront feel with 20% of the complexity:

### From Sins2:
1. ✅ Territory capture (nodes change hands)
2. ✅ Strategic objectives (2-3 per warfront)
3. ✅ Supply cascade (embargo → production stall)

### From Distant Worlds 2 + Starsector:
4. ✅ Natural escalation (no supplies for N ticks)
5. ✅ Attrition system (fleet strength meter)

### From Your Code (Already Done):
6. ✅ Economic demand shocks (wartime multipliers)
7. ✅ Embargo state (goods blocking)
8. ✅ Deterministic combat (hash-proof)

**Total Implementation**: 32-40 hours of focused work.

**Result**: Warfronts feel alive. Visible, dynamic, consequential.

---

## Feature Depth Tiers (How Much Time to Invest?)

### Tier A: Core Feel (MUST HAVE)
- Territory shift on objective capture (4 hours)
- Objectives seeding (2 hours)
- Natural escalation loop (4 hours)
- **Total: 10 hours**
- **Payoff**: Wars move visibly on map, escalate over time

### Tier B: Economic Teeth (SHOULD HAVE)
- Supply chain instantiation (10 hours)
- Embargo cascade detection (4 hours)
- Patrol attrition feedback (3 hours)
- **Total: 17 hours**
- **Payoff**: Wars break supply chains, your support matters

### Tier C: NPC Immersion (NICE-TO-HAVE)
- Patrol behavior scaling (5 hours)
- Route avoidance at warfronts (3 hours)
- NPC fleet reinforcement (4 hours)
- **Total: 12 hours**
- **Payoff**: Galaxy feels at war, NPC behavior reflects intensity

### Tier D: Diplomacy (Future)
- Ceasefire negotiation UI (10 hours)
- Faction reputation leverage (5 hours)
- Peace treaty mechanics (8 hours)
- **Total: 23 hours**
- **Payoff**: Player agency in peace/war outcomes

---

## Danger Zone: Features That Sound Good But Aren't Worth It

### ❌ Stellaris-Style Claims Economy
**What It Does**: Factions accumulate "claim points" on systems. War claims provide +25% war score per claim.

**Why Skip**:
- Requires abstract "war score" tracking (you use concrete intensity instead)
- Players don't declare wars (you use seed-based warfronts)
- Adds complexity without changing gameplay (tariffs already model tension)

**Better Alternative**: Your territory capture (Sins2-style) is simpler + clearer.

### ❌ X4 Blockade Mechanics
**What It Does**: Trade routes destroyed by patrols. Stations cut off. Economic collapse.

**Why Skip**:
- Requires route graph + blockade state per route
- You already have embargo (coarser but sufficient)
- NPC autonomy overhead not worth it for this outcome

**Better Alternative**: Embargo blocks goods directly (deterministic, no routes needed).

### ❌ Distant Worlds Weariness Metric
**What It Does**: War weariness accumulates (0-100). Peace forced at 100%. Affects income, morale.

**Why Skip**:
- You already have intensity (0-4) which is simpler
- Weariness curve tuning is fiddly (requires playtesting)
- Your supply-threshold system (implicit weariness) is more elegant

**Better Alternative**: Stick with intensity escalation + fleet strength attrition.

---

## Decision Checklist: Which Games to Reference

### If You Want Warfronts to Feel **Territorial**
- ✅ Sins of a Solar Empire 2 (copy: territory shift)
- ❌ Stellaris (too abstract)
- ⚠️ X4 (too autonomous, overkill)

### If You Want Warfronts to Feel **Economic**
- ✅ Sins2 (copy: supply cascade)
- ✅ Starsector (copy: market disruption)
- ❌ Distant Worlds 2 (weariness not worth tuning)
- ⚠️ Stellaris (tariffs are too slow)

### If You Want Warfronts to Feel **Strategic**
- ✅ Sins2 (copy: objectives)
- ✅ Starsector (copy: location importance)
- ❌ Stellaris (war goals are diplomatic, not tactical)

### If You Want Warfronts to Feel **Alive**
- ✅ Distant Worlds 2 (copy: escalation loop)
- ✅ Starsector (copy: attrition)
- ⚠️ X4 (NPC autonomy, high cost)

---

## TL;DR: The Shopping List

Copy **exactly this**:

| Feature | From Game | Time | Impact |
|---------|-----------|------|--------|
| Territory capture on objective hold | Sins2 | 4h | **High** |
| Strategic objectives seeding | Sins2 | 2h | **High** |
| Natural escalation without supply | DW2 + Starsector | 4h | **High** |
| Supply chain cascade on embargo | Sins2 | 4h | **High** |
| Warfront escalation countdown HUD | Original | 6h | **High** |
| Patrol attrition feedback | Starsector | 3h | **Medium** |

**Total: 23 hours** for a warfront system that **feels AAA** while staying **indie-scoped**.

Anything beyond this is diminishing returns.

