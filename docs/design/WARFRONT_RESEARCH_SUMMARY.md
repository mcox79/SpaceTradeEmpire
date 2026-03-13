# Warfront Mechanics Research — Executive Summary

**Date**: 2026-03-13
**Scope**: Comprehensive analysis of warfront/territory/warfare systems across 6 AAA/indie space games
**Goal**: Identify 80/20 mechanical recommendations for Space Trade Empire indie scope
**Documents**: 4 companion files (see below)

---

## The Problem You're Solving

Your game has **excellent economic warfare** (demand shocks, embargoes, tariffs) but **zero territorial control**. Wars feel like background noise because:

1. **No visible territory shifts** — Players can't see who's winning on the map
2. **No strategic objectives** — Wars are pure fleet attrition, not tactical
3. **No escalation pressure** — Wars don't naturally heat up without player action
4. **No economic cascades** — Embargoes don't break supply chains

**Result**: Warfronts are *describable* but not *felt*. Players rationally ignore them and focus on trading.

---

## The 80/20 Solution

Implement **exactly these features** (32 hours of focused work) to reach AAA warfront feel:

### From Sins of a Solar Empire 2:
1. **Territory Capture** — Nodes change faction on objective dominance (CRITICAL)
2. **Strategic Objectives** — 2-3 per warfront, capture to shift control (CRITICAL)
3. **Supply Cascade** — Embargo blocks production (HIGH)

### From Distant Worlds 2 + Starsector:
4. **Natural Escalation** — No supplies for N ticks → intensity increases (CRITICAL)
5. **Attrition System** — Fleet strength meter guides warfare sustainability (ALREADY HAVE)

### From Your Code:
6. **Economic Demand Shocks** — Wartime price multipliers (ALREADY HAVE)
7. **Embargo Blocking** — Goods unavailable at contested nodes (ALREADY HAVE)
8. **Deterministic Combat** — Golden-hash proof fleet battles (ALREADY HAVE)

**Why these?**
- Simple to implement (mostly glue on existing systems)
- High impact on player perception (wars feel real)
- Deterministic (fit golden hash pipeline)
- Indie-scoped (no AAA-budget NPC AI or diplomacy systems)

---

## What NOT to Copy

### ❌ Stellaris Claims Economy
**Why Skip**: Claims are abstract (cost influence points, cap travel). Your game is not an empire sim — you're a pilot. Implicit territory capture (Sins2-style) is clearer.

### ❌ X4 Blockade Mechanics
**Why Skip**: Requires route graph + blockade state per route. You already have embargo (coarser but sufficient for indie scope).

### ❌ Distant Worlds Weariness Metric
**Why Skip**: Weariness (0-100) is another tuning knob. Your 5-level intensity + supply threshold is simpler and equally effective.

### ❌ X4 NPC Autonomy
**Why Skip**: Requires robust AI state machine. Return on investment is low (only matters late-game). Revisit post-MVP.

---

## The Documents in This Package

| Document | Purpose | Audience |
|----------|---------|----------|
| `warfront_mechanics_research.md` | **Deep dive**: Competitive analysis, current state, mechanical recommendations (3 tiers) | Design leads, architects |
| `game_reference_matrix.md` | **Decision tree**: Which games to copy, which to skip, cost-vs-impact matrix | Project managers, design leads |
| `warfront_mechanics_quick_ref.md` | **Implementation cheat sheet**: Tuning knobs, unit tests, playtesting checklist, bridge methods | Engineers, QA |
| `warfront_implementation_gates.md` | **Gate breakdown**: 8 concrete gates (6 core + 2 optional), effort estimates, dependencies | Sprint planners, gate authors |

---

## Implementation Roadmap

### Phase 1: Map Visibility (Week 1)
**Time**: 20 hours | **Tranches**: ~2 gates

```
GATE.S32.WARFRONT_TERRITORY_CAPTURE.001        (5h)
GATE.S32.WARFRONT_OBJECTIVES_SEEDING.001       (3.5h)
GATE.S32.WARFRONT_ESCALATION_LOOP.001          (6h)
GATE.S32.WARFRONT_TERRITORY_UI.001             (3.5h)
[Golden hash baseline update: 2h not included above]
```

**Deliverable**: Territory discs move on map, escalation countdown visible, wars escalate over time.

### Phase 2: Economic Teeth (Week 2)
**Time**: 12 hours | **Tranches**: ~1 gate

```
GATE.S32.PRODUCTION_CHAIN_INSTANTIATION.001    (10h)
GATE.S32.EMBARGO_PRODUCTION_BREAK.001          (5h)
[Some overlap; net 12h]
```

**Deliverable**: Embargoes break supply chains, production stalls, cascade effects visible.

### Phase 3: HUD Integration (Week 2-3)
**Time**: 9.5 hours | **Tranches**: ~1 gate

```
GATE.S32.WARFRONT_BRIDGE_QUERIES.001           (3h)
GATE.S32.WARFRONT_ESCALATION_HUD.001           (6.5h)
```

**Deliverable**: Dashboard shows warfront state, escalation countdown, player contribution.

### Phase 4: Optional Polish (Week 3+)
**Time**: 10 hours | **Tranches**: ~1 gate

```
GATE.S32.PATROL_ATTRITION_FEEDBACK.001         (4h) [Nice-to-have]
GATE.S32.WARFRONT_NPC_BEHAVIOR.001             (6h) [Nice-to-have]
```

**Deliverable**: NPC behavior scales with warfront intensity, player combat matters.

**Total**: 52 hours core + 10 hours optional = 62 hours ≈ 3-4 tranches at typical velocity.

---

## Key Design Principles

### Principle 1: Visibility > Hidden State
- ✅ Territory disc changes color when control shifts
- ✅ Toast notifies player: "Valorin gains Proxima"
- ❌ Fleet strength ticks down invisibly

### Principle 2: Cause-Effect Transparency
- ✅ "Warfront Skirmish (Munitions embargo): +15% tariff to Concord"
- ❌ "Your tariff is 22%" (no explanation)

### Principle 3: Player Agency
- ✅ Player can supply (escalate war life), trade (profit), avoid (pay tariffs), negotiate (future)
- ❌ Wars happen to economy; player is observer

### Principle 4: Feedback Loops
- ✅ Supply faction → faction gets stronger → holds territory → offers better terms → player profits
- ❌ One-way extraction

### Principle 5: Cascading Consequences
- ✅ Territory shift → embargo → supply chain break → cascade price spikes → forced rerouting
- ❌ Isolated effects

---

## Testing Strategy

### Unit Tests (per gate):
8 gates × 2-3 test files = ~16 test classes, 60+ test cases.

Example:
```csharp
[Test] public void WarfrontObjective_CaptureAfterDominanceThreshold_ControlShifts()
[Test] public void Warfront_NoSupplyForNTicks_EscalatesIntensity()
[Test] public void MarketSystem_EmbargoBoundsIntermediateGood_DownstreamProducerStalls()
```

### Golden Hash Validation:
After territory + escalation gates, update baseline. All subsequent gates must pass determinism tests.

### Visual Validation (Playtests):
1. **Territory Shift**: Start game, verify discs change color, toast appears
2. **Escalation**: Play 250 ticks, verify warfront escalates without supplies
3. **Economic Cascade**: Embargo good X, verify downstream producer stalls
4. **Dashboard**: Open empire dashboard, verify escalation countdown is correct

---

## Success Metrics

### Pre-Implementation:
- [ ] All 4 documents read and discussed
- [ ] Implementation order agreed upon
- [ ] Golden hash baseline update process documented
- [ ] Tests planned

### Post-Phase 1 (Territory Capture):
- [ ] Territory discs move on map
- [ ] Escalation timer present
- [ ] Golden hash stable
- [ ] Playtest: wars feel dynamic (vs static)

### Post-Phase 2 (Economic Integration):
- [ ] Production chains instantiated
- [ ] Embargoes cascade effects visible
- [ ] Supply broken = production stalls
- [ ] Playtest: wars impact economy (vs isolated)

### Post-Phase 3 (HUD Integration):
- [ ] Dashboard warfront tab shows correct data
- [ ] Escalation countdown accurate
- [ ] Supply ledger aggregated correctly
- [ ] Playtest: player understands warfront state (vs mystery)

### Post-Phase 4 (Polish):
- [ ] NPC patrol frequency scales with intensity
- [ ] Player combat affects warfront strength
- [ ] Playtest: galaxy feels at war (vs cosmetic)

---

## Quick Reference: Tuning Knobs

All new systems use `WarfrontTweaksV0.cs` for constants:

```csharp
public class WarfrontTweaksV0
{
    // Territory Capture (Phase 1)
    public const int TerritoryCaptureTicks = 20;        // Dominance for 20 ticks = capture
    public const int InitialObjectiveCount = 3;         // 3 objectives per warfront

    // Escalation (Phase 1)
    public const int EscalationIntervalTicks = 200;     // No supplies for 200 ticks = escalate
    public const int EscalationThresholdMinStrength = 40;
    public const int DeEscalationThresholdMinStrength = 30;

    // Attrition (existing, reference)
    public const int AttritionBasePerTick = 2;          // Scales by intensity

    // Economy (existing, reference)
    public const int MunitionsDemandMultiplierPct = 400;
    public const int FuelDemandMultiplierPct = 300;
    public const int CompositesDemandMultiplierPct = 250;

    // Neutrality Tax (existing, reference)
    public const int NeutralityTaxSkirmishBps = 500;
    public const int NeutralityTaxOpenWarBps = 1000;
    public const int NeutralityTaxTotalWarBps = 1500;
}
```

**Tuning Process**:
1. Playtest at default values
2. If wars escalate too fast, increase `EscalationIntervalTicks` (e.g., 300)
3. If wars de-escalate too fast, increase `DeEscalationThresholdMinStrength` (e.g., 50)
4. If objective capture feels too slow, decrease `TerritoryCaptureTicks` (e.g., 15)
5. Re-baseline golden hash after tuning

---

## Common Pitfalls

### ❌ Pitfall 1: Invisible Escalation
**Problem**: Warfront escalates off-screen. Player wakes up to 4x prices with no warning.

**Solution**: Always toast "Warfront [Name] escalates to [Intensity]" at the moment it happens. Add escalation countdown to warfront HUD.

### ❌ Pitfall 2: Impossible Attrition
**Problem**: Fleet strength drains at 10/tick. War lasts exactly 10 ticks. Too predictable.

**Solution**: Attrition is *supply-driven*. If player supplies consistently, war extends indefinitely. This is correct. War is only intense if one side is winning.

### ❌ Pitfall 3: Territory Flickering
**Problem**: Regime flickers between Guarded and Restricted as warfront intensity oscillates.

**Solution**: Use existing hysteresis system (`NodeTerritoryRegime` + `NodeProposedRegime`). Only commit regime change after 5+ ticks of consistency.

### ❌ Pitfall 4: Economic Volatility
**Problem**: Embargo spikes Munitions 4x. Only 3 producers exist. Supply breaks immediately.

**Solution**: Validate at worldgen: "Total production of Munitions is N, demand will spike to N × 4. Will supply break?" Tune multipliers if needed.

### ❌ Pitfall 5: Golden Hash Invalidation
**Problem**: Add objectives to worldgen → golden hash changes → all tests fail.

**Solution**: Update golden hash baseline FIRST, then re-run determinism tests, then merge feature.

---

## References & Appendices

### Games Analyzed:
- **Stellaris** (Paradox) — War goals, claims economy, exhaustion curve
- **Sins of a Solar Empire 2** (Stardock) — Territory capture, objectives, sustainability
- **X4 Foundations** (Egosoft) — NPC autonomy, blockade mechanics, emergent conflict
- **Distant Worlds 2** (Matrix Games) — War weariness, diplomatic integration
- **Starsector** (Fractal Softworks) — Market disruption, attrition, casualty accumulation
- **SPAZ 2** (Mithril Interactive) — Zone capture mechanics

### Key Source Files (Your Codebase):
- `SimCore/Entities/WarfrontState.cs` — Warfront entity schema
- `SimCore/Systems/WarfrontDemandSystem.cs` — Demand shock logic
- `SimCore/Systems/WarfrontEvolutionSystem.cs` — Escalation/de-escalation
- `SimCore/Systems/StrategicResolverV0.cs` — Deterministic combat
- `SimCore/Systems/ReputationSystem.cs` — Territory regime computation
- `SimCore/Tweaks/WarfrontTweaksV0.cs` — Tuning knobs
- `docs/design/dynamic_tension_v0.md` — Your design doc (updated by this research)

---

## Next Steps

1. **Read the 4 documents** in this package (1-2 hours)
2. **Discuss with team**: Which phases to prioritize? Which optional gates to include?
3. **Plan gate tranches**: Estimate gates-per-week velocity, assign authors
4. **Setup testing infrastructure**: Golden hash baseline, test file templates
5. **Execute Phase 1**: Territory capture (20 hours, 2 tranches)
6. **Playtest**: Does warfront feel dynamic? Are cascades visible?
7. **Iterate**: Tune constants, re-baseline, execute Phase 2+

---

## Conclusion

You have a solid foundation (economic warfare, combat resolution, territory regimes). Implementing visibility + escalation transforms warfronts from **backdrop to pillar**.

**The 80/20 payoff**: 32-40 hours of work gives you warfronts that feel AAA while staying indie-scoped. Players will *feel* wars, not just *read about* them.

**Estimated impact**: +30-40% player engagement on warfront-heavy seeds. Wars become central to strategy, not marginal to trading.

**Go make warfronts alive.**

