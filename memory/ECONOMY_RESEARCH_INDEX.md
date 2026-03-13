# Economy Research — Master Index

**Quick navigation** for research documents created 2026-03-13.

---

## The Five Research Documents

### 1. Economy Research Summary (START HERE)
**File**: `memory/economy_research_summary.md`
**Length**: 400 lines
**Best for**: 10-minute overview, project status, Phase 2 planning

**What it covers**:
- Executive summary of all findings
- Three core findings (design is correct, execution incomplete)
- Comparative ranking vs 6 reference games
- Why your 13-good scope is correct
- Phase 2 roadmap with time estimates
- Success metrics

**Read when**: You want the TL;DR. This is the one-page brief.

---

### 2. Economy Mechanics Reference
**File**: `memory/economy_mechanics_reference.md`
**Length**: 700 lines
**Best for**: Implementation, formulas, tuning parameters

**What it covers**:
- Price calculation formulas (supply/demand, warfront, tariff multipliers)
- Scarcity index computation
- Warfront demand drain rates (exact numbers for each intensity level)
- Embargo enforcement logic
- NPC trade profitability calculation
- Module sustain model (designed, not yet enforced)
- Geographic distribution formulas
- Validation tests (price volatility, supply chain breakage, warfront impact)
- Master list of all tuning parameters (CatalogTweaksV0.cs reference)

**Read when**: You need to code Phase 2, understand a formula, or validate economy behavior.

---

### 3. Production Chain Phase 2 Guide
**File**: `memory/production_chain_phase2_guide.md`
**Length**: 600 lines
**Best for**: Phase 2 implementation, code patterns, risk mitigation

**What it covers**:
- The proven MarketInitGen pattern (copy-paste reusable)
- Recipe 1: ProcessFood (Organics+Fuel → Food)
- Recipe 2: FabricateComposites (Metal+Organics → Composites)
- Recipe 3: AssembleElectronics (Exotic Crystals+Fuel → Electronics, fracture-only)
- Recipe 4: AssembleComponents (Electronics+Metal → Components)
- Recipes 5-6: Salvage processors (optional)
- Placement logic, inputs/outputs, tweak constants for each
- Risk mitigation (what if Composites never reach buyers? What if Electronics bottlenecks?)
- Testing checklist
- Tuning parameters

**Read when**: You're ready to code Phase 2. Line-by-line walkthrough.

---

### 4. NPC Economy Synthesis
**File**: `memory/npc_economy_synthesis.md`
**Length**: 2400 lines
**Best for**: Deep design understanding, best practices, Phase 2+ planning

**What it covers**:
- What makes economies feel "alive" vs static
- The three ingredients: supply fragmentation, demand diversity, feedback loops
- X4 Foundations deep dive (35 wares, destructive disruption)
- Starsector deep dive (sustain enforcement, fleet scaling pressure)
- Patrician/Port Royale deep dive (NPC visibility, seasonal cycles)
- Stellaris deep dive (economic scaling)
- Star Traders Frontiers deep dive (faction-based supply gating)
- Elite Dangerous (what NOT to do)
- STE vs each game: strengths/weaknesses
- 6 design principles for your economy
- 6 anti-patterns to avoid
- Phase 2 checklist (production, sustain, visibility, tuning)
- Design metrics (validation framework)

**Read when**: You want comprehensive design context, broader industry understanding, or planning Phase 3+.

---

### 5. Economy Game Comparison
**File**: `memory/economy_game_comparison.md`
**Length**: 800 lines
**Best for**: Game-by-game analysis, design philosophy, replayability

**What it covers**:
- X4 model vs STE (complexity vs comprehension trade-off)
- Starsector model vs STE (sustain scaling is identical)
- Patrician model vs STE (NPC visibility difference)
- Stellaris model vs STE (scope difference)
- Star Traders model vs STE (faction gating resonance)
- Why STE is NOT trying to be X4 (intentional design choice)
- What STE is uniquely positioned to do (space trader sandbox, visible + consequential + procedural)
- Recommendations for Phase 2+ (feature prioritization)

**Read when**: You want philosophical alignment check or to explain design to others.

---

## Quick Reference: Find What You Need

### By Topic

**I want to understand the big picture**
→ Read: Economy Research Summary (400 lines, 10 min)
→ Then: Economy Game Comparison (800 lines, 20 min)

**I need to code Phase 2 production recipes**
→ Read: Production Chain Phase 2 Guide (600 lines, exact code patterns)
→ Reference: Economy Mechanics Reference (formulas, tuning)

**I need to understand a specific formula**
→ Read: Economy Mechanics Reference, Part 1-8 (find by name)

**I want to tune economy after Phase 2**
→ Read: Economy Mechanics Reference, Part 9-11 (validation, tuning params)

**I want design depth for broader decisions (Phase 3+)**
→ Read: NPC Economy Synthesis (2400 lines, comprehensive)

**I want to validate that my 13-good scope is correct**
→ Read: Economy Research Summary Part "Why 13 goods is right scope"
→ Then: Economy Game Comparison Part "Synthesis"

**I want to know what will break and how to fix it**
→ Read: Production Chain Phase 2 Guide Part "Risk Mitigation"
→ Then: NPC Economy Synthesis Part "Anti-Patterns"

---

### By Time Commitment

**5 minutes**: Economy Research Summary (intro only)
**15 minutes**: Economy Research Summary + Economy Game Comparison (intro)
**30 minutes**: Economy Research Summary + Production Chain Phase 2 Guide (skimming)
**1 hour**: All of above + Economy Mechanics Reference Part 1-3
**2 hours**: All of above + Economy Mechanics Reference complete
**4 hours**: All documents, full read
**8 hours**: All documents + implementation + testing

---

### By Role

**Project Manager**
→ Economy Research Summary (status, timeline, risk)
→ Production Chain Phase 2 Guide (implementation plan)

**Programmer**
→ Production Chain Phase 2 Guide (code patterns)
→ Economy Mechanics Reference (formulas, validation)
→ Economy Research Summary (big picture after coding)

**Designer**
→ NPC Economy Synthesis (best practices, principles)
→ Economy Game Comparison (design philosophy)
→ Economy Research Summary (findings, Phase 2+ recommendations)

**Player / Tester**
→ Economy Mechanics Reference Part 9 (validation tests, success metrics)
→ Economy Research Summary (what to expect after Phase 2)

---

## Document Map

```
economy_research_summary.md
├─ START HERE (overview, Phase 2 roadmap)
├─ References: npc_economy_synthesis.md (best practices)
├─ References: economy_game_comparison.md (game analysis)
├─ References: production_chain_phase2_guide.md (Phase 2 plan)
└─ References: economy_mechanics_reference.md (formulas)

production_chain_phase2_guide.md
├─ Code implementation (6 recipe deployments)
├─ References: ContentRegistryLoader.cs (recipe definitions)
├─ References: MarketInitGen.cs (deployment patterns)
├─ References: CatalogTweaksV0.cs (tuning parameters)
└─ References: economy_mechanics_reference.md (validation)

npc_economy_synthesis.md
├─ Best practices (8 principles, 6 anti-patterns)
├─ References: X4 Foundations (destructive disruption)
├─ References: Starsector (sustain enforcement)
├─ References: Patrician (NPC visibility)
├─ References: Star Traders (faction gating)
└─ Phase 2 checklist + design metrics

economy_game_comparison.md
├─ Game-by-game deep dive (5 major games + ED)
├─ STE vs each game (strengths, weaknesses)
├─ Design philosophy (why STE is not X4)
└─ Phase 2+ recommendations

economy_mechanics_reference.md
├─ Price calculation (base, supply/demand, warfront, tariff)
├─ Scarcity index (ideal stock, thresholds)
├─ Warfront demand (drain rates by intensity)
├─ Embargo enforcement (cascade logic)
├─ NPC trade (profit calculation)
├─ Module sustain (designed model)
├─ Geographic distribution (extraction good percentages)
├─ Testing formulas (volatility, breakage, impact)
└─ Tuning parameters (master list)
```

---

## Key Numbers (Cheat Sheet)

### Good Counts
- **Total goods**: 13
- **Total recipes**: 9
- **Currently deployed**: 3 (ExtractOre, RefineMetal, ManufactureMunitions)
- **Remaining**: 6 (ProcessFood, FabricateComposites, AssembleElectronics, AssembleComponents, SalvageToMetal, SalvageToComponents)

### Geographic Distribution
- **Fuel wells**: 17% of nodes (every 6th node)
- **Ore deposits**: 50% of nodes (every other node)
- **Organics**: 40% of nodes
- **Rare Metals**: 15% of nodes
- **Exotic Crystals**: Fracture-only (discovery-driven)

### Warfront Demand Multipliers (at TotalWar intensity)
- **Munitions**: 4.0x base price
- **Composites**: 2.5x base price
- **Fuel**: 3.0x base price

### Price Bands
- **Low**: 50-100 credits (Fuel, Ore, Organics, Food)
- **Mid**: 150-300 credits (Metal, Composites, Munitions, Electronics)
- **High**: 400-800 credits (Components, Rare Metals, Exotic Crystals, Salvaged Tech)
- **Very High**: 1000-2000 credits (Exotic Matter)

### Phase 2 Timeline
- **Critical path**: 3 hours (deploy recipes + enforce sustain)
- **Full Phase 2**: 8 hours (includes testing, tuning, validation)
- **Lines of code**: <200 (mostly copy-paste)

### Success Metrics
- Price volatility: CV ≥ 0.20 (minimum 20%)
- Margin availability: ≥3 profitable routes at any tick
- Supply chain breakage: ≥1 chain breaks per 1000 ticks
- Warfront impact: Contested prices ≥1.5x peaceful
- Good availability: ≥85% of markets have stock
- Scarcity frequency: ≥1 good reaches 50% scarcity per 500 ticks

---

## Cross-References to Your Code

### Existing Implementations (Already in Code)
- **WarfrontDemandSystem.cs**: Warfront drain logic (working)
- **EmbargoState.cs**: Embargo enforcement (working)
- **NpcTradeSystem.cs**: NPC trade evaluation (working, range limited)
- **MarketSystem.cs**: Supply/demand price modifiers (working)
- **ReputationSystem.cs**: Tariff scaling (working)
- **MaintenanceSystem.cs**: Sustain skeleton (exists, not enforced)
- **MarketInitGen.cs**: Industry site deployment (pattern proven for 3 recipes)

### Phase 2 Modifications
- **MarketInitGen.cs**: Add 6 recipe deployment blocks
- **CatalogTweaksV0.cs**: Add 16 new constants
- **ContentRegistryLoader.cs**: Populate GoodDefV0.BasePrice and PriceSpread fields
- **SimBridge.cs**: Add scarcity snapshot methods
- **SimKernel.cs**: Enable MaintenanceSystem.ProcessFleetModuleSustain() call

### Testing
- **Trade_goods_v0.md**: Validation (all formulas match)
- **Dynamic_tension_v0.md**: Validation (all systems match)
- **RoadmapConsistency tests**: Must pass post-Phase 2

---

## FAQ

**Q: Do I need to read all five documents?**
A: No. Start with Economy Research Summary. Dive into Production Chain Phase 2 Guide when you're ready to code. Others are reference/context.

**Q: Are these documents up to date?**
A: Yes. Created 2026-03-13. Cross-referenced against code as of 2026-03-13.

**Q: Can I use this research to pitch to publisher/stakeholder?**
A: Yes. Show them Economy Research Summary Part "Comparative Ranking." Demonstrates you're learning from industry best practices and executing sound design.

**Q: Do I need web sources for these findings?**
A: No. This is design analysis + your code comparison. All claims are verifiable against your codebase or published game design documents.

**Q: What if I disagree with a recommendation?**
A: That's fine. These are informed suggestions, not mandates. All recommendations are motivated (they explain the "why"). You're empowered to deviate if you have a better idea.

---

## Document Stats

| Document | Length | Time to Read | Best For |
|---|---|---|---|
| economy_research_summary.md | 400 lines | 10 min | Overview, status, timeline |
| production_chain_phase2_guide.md | 600 lines | 20 min | Phase 2 implementation |
| economy_mechanics_reference.md | 700 lines | 30 min | Formulas, validation, tuning |
| npc_economy_synthesis.md | 2400 lines | 90 min | Design depth, best practices |
| economy_game_comparison.md | 800 lines | 30 min | Game analysis, philosophy |
| **Total** | **5100 lines** | **3 hours** | **Comprehensive economy guide** |

---

## Next Steps

1. **Read Economy Research Summary** (10 min)
2. **Decide**: Phase 2 immediately, or Phase 3 (after current tranche)?
3. **If Phase 2**: Read Production Chain Phase 2 Guide (20 min)
4. **Code**: MarketInitGen 6 recipe deployments (2-3 hours)
5. **Test**: Golden hash, ExplorationBot, manual price validation (2 hours)
6. **Tune**: Measure price volatility, adjust multipliers (2 hours)
7. **Reference**: Keep Economy Mechanics Reference handy for tuning decisions

---

**All research documents are in**: `c:\Users\marsh\Documents\Space Trade Empire\memory\`

**Questions?** Re-read the document that matches your question. All claims are sourced within the documents themselves.

---

End of Index.
