# Warfront & Territory Control Mechanics Research

## Executive Summary

This document synthesizes best-in-class warfront mechanics from major 4X and space strategy games, contrasts them with Space Trade Empire's current architecture, and provides concrete mechanical recommendations for progression toward AAA parity while maintaining indie scope.

**Key finding**: Your game has solid **economic warfare** (demand shocks, embargoes, tariffs) but lacks three critical systems that make warfronts feel dynamic and alive:
1. **Visible territorial control shifts** (players see the war moving across the map)
2. **Cascading consequences** (wars create supply shocks that ripple through the economy)
3. **Player strategic agency within the war** (more than just delivering goods)

---

## Part 1: Competitive Analysis

### Game Matrix: Warfront Mechanics

| Mechanic | Stellaris | Starsector | X4 Foundations | Sins2 | Distant Worlds 2 | SPAZ 2 |
|---|---|---|---|---|---|---|
| **Territory visual** | Claims map, hexes shift | Sector ownership, pirate/NPC fleets | Systems owned, gates controlled | Planets change hands, visual shimmer | Stellar map, faction % influence | Zone ownership clear |
| **Capture mechanic** | War goals (claims needed), status quo ±10% | Patrol presence, location defeat | Capture mechanics via station/battle | Territory allegiance gradient | Diplomatic osmosis + military | Direct capture on hold |
| **War exhaustion** | Warp Focus accumulation, hard limit at 100% | Reputation decay, casualty accumulation | Economic attrition (no formal meter) | Sustainability cost per planet | War Weariness affects income | Attrition-based (casualties) |
| **Economic impact** | Tariffs shift, goods embargo, production drop | Market disruption localized to sector | Trade routes destroyed, stations blockaded | Mineral lock per planet, supply caps | Embargo blocks 75% trade | Price spikes, supply interruption |
| **De-escalation path** | Status quo peace (50% war goals), ceasefire | Negotiated peace, reputation threshold | Truce system (formal diplomacy) | Cease-fire tickers, peace deals | Negotiated treaty | Formal surrender or destruction |
| **Attrition system** | War exhaustion ÷ time (both sides) | Fleet losses accumulate (soft cap) | Ambient losses if outnumbered | Attrition points per tick (heat-driven) | War weariness (5-50 pts/turn) | Fleet destruction only |
| **Strategic objectives** | War goals are abstract (credits, tech, systems) | ⚠️ Limited — mostly fleet combat | Named targets (stations, planets) | Strategic Warfront Objectives (capture points) | ⚠️ Minimal — economic only | ⚠️ None — pure combat |
| **NPC behavior** | Automatic escalation, reputation-driven deals | Fleets patrol, engage on sight | Autonomous faction expansion | AI continuously redeployment | Negotiated alliances, opportunism | Simple flee/attack logic |

#### Key Observations:
- **Stellaris** = most mature system (claims economy, exhaustion curve, abstract war goals)
- **Sins2** = territory + objectives + sustainability create strongest *feeling* of strategic warfare
- **X4** = most dynamic (NPC behavior creates emergent conflicts) but requires massive AI overhead
- **Starsector** = lean system with strong economic integration (fits indie budget)
- **Distant Worlds 2** = weariness system interesting but underutilized (diplomacy overshadows)
- **SPAZ 2** = pure combat, zero territory mechanics (not applicable)

---

## Part 2: Current Space Trade Empire Architecture

### Implemented Warfront Systems

**✅ What's Working:**

1. **Warfront Intensity States** (Peace, Tension, Skirmish, OpenWar, TotalWar)
   - 5-level escalation ladder ✓
   - Deterministic, no RNG ✓
   - Affects multiple systems (tariffs, demand, combat) ✓

2. **Economic Warfare**
   - War-driven demand shocks (4x Munitions, 3x Fuel, 2.5x Composites at contested nodes) ✓
   - Embargo state system ✓
   - Neutrality tax (Skirmish +500bps, OpenWar +1000bps, TotalWar +1500bps) ✓
   - Rep-based tariff scaling (Allied -15%, Hostile +20%) ✓
   - Supply ledger tracking (deliveries) ✓

3. **NPC Fleet Combat**
   - Deterministic attrition resolver (StrategicResolverV0) ✓
   - Heat system, radiator modules, stance-based armor ✓
   - Zone armor (4 directional HP pools) ✓
   - Golden hash determinism ✓

4. **Territory Regimes**
   - 4 regime states: Open, Guarded, Restricted, Hostile ✓
   - Computed from (RepTier, TradePolicy, WarfrontIntensity) ✓
   - Affects patrol response (scan warning, pursuit, attack-on-sight) ✓

### Missing / Incomplete Systems

**❌ Critical Gaps:**

| System | Current State | Impact | Priority |
|---|---|---|---|
| **Territory map shifts** | None — nodes assigned to factions at worldgen, never change | Wars feel static; no visible front movement | **Critical** |
| **Objective capture** | Schema exists (WarfrontObjective), not instantiated at worldgen | Wars have no strategic targets | **Critical** |
| **Fleet attrition gates** | `ApplyFleetAttrition()` reduces strength but not tied to game pace | Attrition is invisible; doesn't escalate wars | **High** |
| **Supply-driven escalation** | Deliveries reduce intensity by 1, but no escalation by conflict duration | Wars don't naturally heat up (only start hot) | **High** |
| **Strategic consequence surfacing** | WarConsequenceSystem exists but just text blips, no gameplay impact | Consequences feel decorative | **Medium** |
| **Patrol attrition** | NPCs attack, but no reinforcement loop or fleet healing | War doesn't affect NPC fleet balance | **Medium** |
| **Endgame territory control** | No win condition tied to territory (exists as idea, not code) | Player can ignore warfronts entirely | **High** |

---

## Part 3: Mechanical Recommendations

### Tier 1: Critical (Affects Core Feel)

#### 1a. **Territory Capture System**
**Problem**: Wars are invisible on the map. Nodes belong to factions but don't change hands.

**Best Practice (Stellaris + Sins2 hybrid)**:
- Nodes have a `ControllingFactionId` (currently one-way) + `DominantFactionId` (proposed)
- Dominance tracked via `DominanceTicks` counter
- When FleetA defeats FleetB at Node X:
  - `DominanceTicks[FleetA] += 1`
  - If `DominanceTicks >= CaptureThreshold` (20 ticks), control shifts: `ControllingFactionId = FleetA`
  - Territory disc updates color in real-time

**Indie Implementation** (12 hours):
```csharp
// In WarfrontEvolutionSystem.ProcessObjectives()
foreach (var obj in wf.Objectives)
{
    // After a combat at this node, check dominance
    if (obj.DominanceTicks >= CAPTURE_THRESHOLD)
    {
        // Transfer control
        NodeFactionId[obj.NodeId] = obj.DominantFactionId;
        obj.ControllingFactionId = obj.DominantFactionId;
        obj.DominanceTicks = 0; // Reset for next swing
    }
}
```

**Player Experience**:
- Galaxy map territory disc changes color over ~20 ticks (visible flow)
- UI shows "Valorin gains territory" toast when control shifts
- Previous owner's traders face Hostile regime at that node (dynamic repercussions)

**See Also**: Stellaris' claims system (abstract), Sins2's planet capture (direct), Starsector's sector control (emergent).

---

#### 1b. **Fleet Attrition Escalation Loop**
**Problem**: Wars start at intensity 3+ but don't escalate naturally. Attrition reduces strength but doesn't feed back into intensity.

**Best Practice (Distant Worlds 2 weariness + Sins2 sustainability)**:
- Warfronts escalate when `DurationTicks % EscalationInterval == 0` if no peace deliveries
- Escalation only if `FleetStrengthA > Threshold` (war is sustainable)
- If fleet strength drops below 40%, intensity decreases (war becomes unaffordable)

**Indie Implementation** (8 hours):
```csharp
// WarfrontEvolutionSystem.ProcessColdWar()
public static void ProcessWarfrontEscalation(WarfrontState wf, int currentTick)
{
    if (wf.Intensity == WarfrontIntensity.Peace) return;

    // Check if war should escalate (no supplies delivered recently)
    int ticksSinceLastDelivery = currentTick - wf.LastSupplyDeliveryTick;
    if (ticksSinceLastDelivery >= ESCALATION_INTERVAL &&
        wf.Intensity < WarfrontIntensity.TotalWar)
    {
        // Only escalate if both sides can sustain it
        if (wf.FleetStrengthA >= ESCALATION_THRESHOLD &&
            wf.FleetStrengthB >= ESCALATION_THRESHOLD)
        {
            wf.Intensity = (WarfrontIntensity)((int)wf.Intensity + 1);
            // Log to session: "Warfront heats up: now at [Intensity]"
        }
    }

    // Attrition drains strength
    if (wf.Intensity >= WarfrontIntensity.Skirmish)
    {
        int drain = ATTRITION_BASE * (int)wf.Intensity;
        wf.FleetStrengthA -= drain;
        wf.FleetStrengthB -= drain;
    }

    // De-escalation: if fleet strength critical, step down intensity
    if (wf.FleetStrengthA < 30 && wf.FleetStrengthB < 30)
    {
        wf.Intensity = WarfrontIntensity.Tension;
        // Both sides are exhausted, war cools
    }
}
```

**Player Experience**:
- Turn 100: Cold war (Tension, +500bps tariff)
- Turn 300: Player hasn't supplied war goods → "Warfront escalates" toast
- Turn 400: Now OpenWar, Munitions 4x price, player forced to engage
- Turn 500: Player supplies 100 Munitions → intensity drops to Skirmish next turn

---

#### 1c. **Strategic Objectives Instantiation**
**Problem**: WarfrontObjective schema exists but objectives aren't seeded at worldgen.

**Best Practice (Sins2: SupplyDepot + CommRelay, Stellaris: abstract war goals)**:
- Seed 2-4 objectives per warfront at worldgen
- Types: SupplyDepot (reduces attrition drain when held), CommRelay (visibility buff), Factory (production boost)
- Objectives don't move; they're anchored to nodes

**Indie Implementation** (6 hours in GalaxyGenerator.SeedWarfrontsV0()):
```csharp
// Seed objectives for each warfront
foreach (var wf in state.Warfronts.Values)
{
    // Pick 2-3 nodes from contested list
    var objectiveNodes = wf.ContestedNodeIds.Take(2).ToList();

    foreach (var nodeId in objectiveNodes)
    {
        var obj = new WarfrontObjective
        {
            NodeId = nodeId,
            Type = ObjectiveType.SupplyDepot, // Could randomize
            ControllingFactionId = GetOwnerOfNode(state, nodeId),
            DominanceTicks = 0,
            DominantFactionId = ""
        };
        wf.Objectives.Add(obj);
    }
}
```

**Player Experience**:
- Galaxy map shows objective icons (depot, relay, factory) at contested nodes
- Capturing a SupplyDepot reduces attrition cost at that warfront (makes war sustainable)
- NPC factions fight over objectives, making some nodes "hotter" than others

---

### Tier 2: High-Impact (Gameplay Polish)

#### 2a. **Supply Chain Fragility**
**Current**: Embargo blocks trade. Warfront demand spikes prices.

**Enhancement**: Instantiate remaining 6 production chains (currently only 3/9) so warfront embargoes break actual supply chains.

**Example**:
- Scenario: Munitions embargo at Warfront A
- Munitions = final good (no dependencies)
- But Components uses Munitions (downstream dependency)
- Embargo → no munitions → Components production halts → price spikes → cascades to Refits

**Cost**: ~16 hours (6 chains × 2-3 hours design + seeding)

**Impact**: Makes economic warfare feel *real*. Wars aren't background noise; they actively break your supply chains.

---

#### 2b. **Patrol Attrition Reinforcement**
**Current**: NPCs patrol and attack, but no feedback loop.

**Enhancement**: When player defeats an NPC fleet at a node:
- `ApplyFleetAttrition()` reduces that faction's fleet strength at that warfront
- If player supplies a faction with Munitions, fleet strength recovers (+5 per unit)
- Creates a *positive feedback loop*: supply warfront → faction gets stronger → player's trading is defended

**Cost**: 4 hours (modify NpcFleetCombatSystem + WarfrontEvolutionSystem)

**Impact**: Player's warfront support feels consequential. Factions you supply actually get stronger.

---

#### 2c. **Warfront Status Visibility**
**Current**: Session log records deliveries. No HUD display.

**Enhancement**: EmpireDashboard "Warfront" tab showing:
- Warfront name, intensity, duration
- FleetStrengthA vs FleetStrengthB (progress bar)
- Objectives with control status
- Your deliveries this session (total units)
- Escalation countdown ("Escalates in 45 ticks without supplies")

**Cost**: 6 hours (UI + bridge query methods)

**Impact**: Players understand warfront state without external tabs. Warfront pressure becomes legible.

---

### Tier 3: Advanced (AAA Polish)

#### 3a. **Territory-Driven Supply Chain Rerouting**
When territory shifts (Faction A → Faction B), existing trade routes become:
- Invalid (enemy territory)
- More expensive (new tariff)
- Partially routable (fracture bypass)

AutomationProgram detects this and offers: "Route interrupted: Faction B now controls Proxima. Reroute via Vega? [Yes] [Edit]"

**Cost**: 12 hours (route recompute logic, UI feedback)

**Impact**: Wars directly sabotage your infrastructure. High tension.

---

#### 3b. **Warfront Intensity Feedback on NPC Behavior**
At OpenWar+:
- NPC traders avoid warfront corridors (choose safer routes)
- NPC fleets organize into patrol wings (squad behavior)
- Patrol aggression increases (more scan warnings)

At Tension:
- NPC traders still use all routes
- Individual patrol fleets
- Scan warning only if cargo is war-relevant

**Cost**: 8 hours (NPC routing logic, fleet behavior)

**Impact**: Galaxy *feels* at war. NPC behavior mirrors warfront state.

---

#### 3c. **Ceasefire Brokering (Diplomacy Integration)**
Currently: Supply deliveries reduce intensity (implicit ceasefire).

Enhancement: Formal ceasefire deal:
- Player visits a neutral station, negotiates with both combatants
- "I will deliver 200 Munitions to establish supply line"
- Once threshold met: formal ceasefire, intensity frozen for N ticks
- Both sides extract reputation gain from player (player is the mediator)

**Cost**: 14 hours (dialog system, negotiation state machine, NPC routing)

**Impact**: Player becomes a stakeholder in peace. High narrative resonance.

---

## Part 4: Indie vs AAA Scope Recommendation

### MVP (Playable Warfront Warfare) — 32 hours
1. Territory capture system (1a) — **12 hours**
2. Fleet attrition escalation (1b) — **8 hours**
3. Objectives instantiation (1c) — **6 hours**
4. Warfront status HUD (2c) — **6 hours**

**Deliverable**: Visible warfront movement, escalation timer, legible fleet strength, objective capture.

### Enhanced (Economic Consequences) — +20 hours
5. Supply chain instantiation (2a) — **16 hours**
6. Patrol attrition reinforcement (2b) — **4 hours**

**Deliverable**: Wars break supply chains. Player support feels mechanically meaningful.

### AAA (Emergent Warfare) — +34 hours
7. Territory-driven rerouting (3a) — **12 hours**
8. Warfront NPC behavior (3b) — **8 hours**
9. Ceasefire diplomacy (3c) — **14 hours**

**Total: 86 hours for full AAA warfront system.**

---

## Part 5: How to Make Warfronts Feel *Alive* (Design Principles)

### Principle 1: **Visibility Over Hidden State**
- ❌ Fleet strength ticks down invisibly
- ✅ Territory disc changes color, toast "Valorin gains 3 nodes", warfront HUD shows progress bar

### Principle 2: **Cause-Effect Transparency**
- ❌ "Your tariff went up to 22%"
- ✅ "Warfront Skirmish (Munitions embargo): +15% tariff to Concord territory"

### Principle 3: **Player Agency in Conflict**
- ❌ Wars happen to the economy
- ✅ Player can: supply (escalate), trade (profit), avoid (pay tariffs), negotiate (broker peace)

### Principle 4: **Feedback Loops**
- Deliver Munitions → faction gets stronger → faction holds territory → faction offers better trading terms → player profits
- Create virtuous cycle, not one-way extraction

### Principle 5: **Cascading Consequences**
- Territory shift → embargo → supply chain break → cascade price spikes → forces rerouting
- Single event creates multi-tick ripple effect

---

## Part 6: Reference Implementation Patterns

### Pattern 1: Intensity-Driven Multiplier (Already in your code!)
```csharp
int intensity = (int)wf.Intensity; // Peace=0, Tension=1, ..., TotalWar=4
int demandMultiplier = 100 + ((intensity - 1) * 100); // 100%, 100%, 200%, 300%, 400%
```
**Reuse this pattern** for attrition, tariffs, NPC patrol frequency.

### Pattern 2: Hysteresis (Territory Commitment)
Nodes have two regimes: `ProposedRegime` (computed) and `CommittedRegime` (sticky).
- When `ProposedRegime` differs for 5+ ticks, commit the change
- Prevents flickering at boundaries

**You already have this!** `NodeTerritoryRegime` + `NodeProposedRegime` in SimState.Properties.cs

### Pattern 3: Deterministic Scheduler
Use `wf.TickStarted + (currentTick - wf.TickStarted) % ESCALATION_INTERVAL == 0` to trigger escalations.
- Deterministic (same seed = same escalation schedule)
- No RNG drift
- Golden hash compatible

---

## Part 7: Testing & Validation

### Test 1: Territory Shift Cascade
**Setup**: Warfront with 3 contested nodes (A, B, C).
**Action**: Player delivers 150 Munitions at Node A.
**Expected**:
- [ ] FleetStrengthA increases by 5
- [ ] LastSupplyDeliveryTick updates
- [ ] After 20+ ticks without new supply, intensity escalates
- [ ] If Node A held by FactionA for 20 ticks, control shifts
- [ ] Node A's regime shifts from Guarded → Open (for allied traders)
- [ ] NPC patrol at Node A updates response (from attack-on-sight → scan only)

### Test 2: Supply Chain Break
**Setup**: Warfront embargo blocks Munitions at Node X. Node X supplies Munitions to production chain elsewhere.
**Expected**:
- [ ] Downstream producer (Components) stalls
- [ ] Components price spikes 2x within 10 ticks
- [ ] Refits (dependent on Components) become unprofitable
- [ ] Player's automation programs show "route broken" diagnostic

### Test 3: Warfront Escalation Without Supply
**Setup**: Cold war at Tension, no supply deliveries for 200 ticks.
**Expected**:
- [ ] At tick 200, "Warfront escalates" toast
- [ ] Intensity → Skirmish
- [ ] Munitions demand 2x (no longer 1x)
- [ ] NPC patrol frequency increases

---

## Part 8: Comparison to Target Games

### Stellaris vs Your Game
| Aspect | Stellaris | You | Gap |
|--------|-----------|-----|-----|
| Claims economy | Claims cost influence, cap travel | None (implicit faction ownership) | Claims should be expensive to take |
| War goals | Abstract (subjugation, humiliation, ideology) | Implicit (control) | Add war goal types: supply, prestige, ideology |
| Exhaustion curve | Gradual accumulation, hits hard at 100% | Discrete (intensity 0-4) | Your 5-level system is cleaner |
| De-escalation | Negotiated surrender or white peace | Supply-driven only | Add formal negotiations (3a) |
| Economic impact | Tariff + occupation penalty | Tariff + embargo | Occupation penalty missing (implement 3a) |
| **Verdict** | Infinitely complex; production-game-adjacent | Elegant minimalism; fits your scope | You're simpler → faster to implement |

### Sins2 vs Your Game
| Aspect | Sins2 | You | Gap |
|--------|-------|-----|-----|
| Territory capture | Planets change hands per battle | Nodes static | **Critical**: implement 1a |
| Objectives | 3 types with gameplay effects | Schema exists, not used | **High**: instantiate 1c |
| Sustainability | Production cost per planet, attrition if can't pay | Implicit in supply threshold | Your model is cleaner; attrition works fine |
| Economic warfare | Supply chains break if planet captured | Embargo only | Implement 2a (production chains) |
| **Verdict** | Complex, AI-intensive, AAA production values | Clean mechanics, indie-friendly | Focus on 1a + 1c for Sins2 feel |

### Starsector vs Your Game
| Aspect | Starsector | You | Gap |
|--------|-----------|-----|-----|
| Territory | Sector ownership (NPC emergent) | Faction-assigned (static) | Add NPC fleet patrol pressure (3b) |
| Economics | Market disruption via blockade | Embargo + demand shocks | **You're ahead here** |
| Tactics | Fleets engage via standing orders | You don't have active fleet control | **Acceptable**: you're not a tactics game |
| Attrition | Casualties accumulate | FleetStrength meters | Your abstraction is fine |
| **Verdict** | Sandbox; infinite replayability; high complexity | Narrative-driven; 15-30 hrs/run | Your design is more focused |

---

## Part 9: Gotchas & Anti-Patterns

### ❌ Gotcha 1: Invisible Escalation
**Problem**: Warfront escalates off-screen. Player wakes up to 4x prices with no warning.

**Solution**: Always toast "Warfront [Name] escalates to [Intensity]" at the moment it happens. Add escalation countdown to warfront HUD.

### ❌ Gotcha 2: Impossible Attrition Drain
**Problem**: Fleet strength drains at 10/tick. At 100 initial, war lasts exactly 10 ticks. Too predictable.

**Solution**: Attrition is supply-driven. If player supplies consistently, war extends indefinitely. This is *correct*. War is only intense if one side is winning. If both are supplied equally, stalemate (Tension).

### ❌ Gotcha 3: Territory Changes Break Savegames
**Problem**: Node control shifted. Old save references old faction. Loading crashes.

**Solution**: NodeFactionId is a mutable dict. On load, recompute territory regimes (they're idempotent). No breakage.

### ❌ Gotcha 4: War Economics Too Volatile
**Problem**: Supply spikes Munitions 4x. Only 3 nodes produce it. Player can't meet demand.

**Solution**: Tuning. If only 3 producers, wartime demand should be 2x not 4x. Use WellKnownGoodIds and production recipes to validate at worldgen: "Total production of Munitions is N, demand will spike to N × 4. Will supply break?"

---

## Part 10: Implementation Roadmap

### Week 1: MVP Territory Capture
- [ ] Implement territory capture (1a)
- [ ] Add objectives seeding (1c)
- [ ] Implement attrition escalation (1b)
- [ ] Add warfront HUD tab (2c)
- [ ] Golden hash baseline update

### Week 2: Economic Integration
- [ ] Instantiate remaining production chains (2a)
- [ ] Implement patrol attrition feedback (2b)
- [ ] Test supply chain breaks
- [ ] Playtest warfront + economy interactions

### Week 3+: AAA Polish
- [ ] Territory-driven rerouting (3a)
- [ ] NPC behavior warfront scaling (3b)
- [ ] Ceasefire diplomacy (3c)
- [ ] Full gameplay loop testing

---

## Conclusion

Your warfront system has a **solid economic foundation** (demand shocks, embargoes, tariffs are well-tuned). The missing piece is **visible territory control and strategic objectives** — the systems that make wars *felt* on the map.

**Recommendation**: Implement Tier 1 (32 hours) to reach "warfronts feel real" threshold. This takes you from economic-only warfare to territorial warfare, which is the AAA standard.

The great news: you've already built the hardest part (attrition resolver, heat system, golden hash determinism). Bolting on territory capture is mechanical glue, not rocket science.

**Target**: 2 tranche-weeks (40-45 gates) to implement MVP + economic integration. This positions warfronts as a central pillar rather than backdrop.

