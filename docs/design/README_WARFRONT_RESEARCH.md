# Warfront Mechanics Research — Reading Guide

**Created**: 2026-03-13
**Status**: Complete research package (5 documents)
**Scope**: Analysis of 6 AAA/indie space games + concrete implementation roadmap

---

## What This Package Contains

You have 5 new research documents covering warfront/territory/warfare mechanics:

| Document | Size | Audience | Purpose |
|----------|------|----------|---------|
| **WARFRONT_RESEARCH_SUMMARY.md** | 4 KB | Everyone | **Start here.** Executive summary, problem statement, 80/20 solution. |
| **warfront_mechanics_research.md** | 32 KB | Design leads, architects | **Deep dive.** Competitive analysis (Stellaris, X4, Sins2, etc.), current state, 3-tier mechanical recommendations. |
| **game_reference_matrix.md** | 24 KB | Project managers, design | **Decision tree.** Which games to copy, cost-vs-impact matrix, feature adoption guide. |
| **warfront_mechanics_quick_ref.md** | 28 KB | Engineers, QA | **Implementation cheat sheet.** Tuning knobs, unit tests, playtesting checklist, bridge methods. |
| **warfront_implementation_gates.md** | 18 KB | Sprint planners, gate authors | **Gate breakdown.** 8 concrete gates (6 core + 2 optional), effort estimates, dependencies, acceptance criteria. |
| **warfront_before_after_examples.md** | 16 KB | Everyone (motivation) | **Visual examples.** Before/after screenshots showing impact of each feature. |

---

## Reading Paths

### Path 1: "Just Tell Me What to Do" (20 minutes)
1. Read `WARFRONT_RESEARCH_SUMMARY.md` (4 min)
2. Skim `game_reference_matrix.md` → "The 80/20 Warfront Bundle" section (3 min)
3. Skim `warfront_implementation_gates.md` → "Gate Ordering & Dependencies" (4 min)
4. Read `warfront_before_after_examples.md` (9 min, motivational)

**Deliverable**: Understand what to build and why. Ready for sprint planning.

---

### Path 2: "I Need to Understand the Design" (1-1.5 hours)
1. **WARFRONT_RESEARCH_SUMMARY.md** — Full read (10 min)
2. **warfront_mechanics_research.md** — Parts 1-4 (competitive analysis + current state) (25 min)
3. **game_reference_matrix.md** — Full read (15 min)
4. **warfront_before_after_examples.md** — Skim examples (10 min)
5. **warfront_mechanics_quick_ref.md** — Tuning knobs + bridge methods sections (15 min)

**Deliverable**: Full design picture. Can make design decisions, tune parameters, review gate implementations.

---

### Path 3: "I'm Implementing a Gate" (2 hours)
1. **warfront_implementation_gates.md** → Your specific gate (10 min read)
2. **warfront_mechanics_quick_ref.md** → Full read (30 min)
3. **warfront_mechanics_research.md** → Part 3 (mechanical recommendations for your gate) (20 min)
4. **warfront_before_after_examples.md** → Examples related to your gate (15 min)
5. Reference docs while coding: test templates, bridge method signatures, tuning knobs (25 min)

**Deliverable**: Ready to implement with full context, test structure, and tuning guidance.

---

### Path 4: "I'm Planning Tranches" (45 minutes)
1. **WARFRONT_RESEARCH_SUMMARY.md** → Full read (10 min)
2. **warfront_implementation_gates.md** → Effort estimates + dependencies (10 min)
3. **game_reference_matrix.md** → "Priority Matrix" and "Feature Adoption Guide" (15 min)
4. **warfront_mechanics_quick_ref.md** → "Implementation Sequence" sections (10 min)

**Deliverable**: Understand gate scope, parallel groups, dependencies. Can estimate tranches (2-3 for MVP, 4+ for full).

---

## Document Summary

### WARFRONT_RESEARCH_SUMMARY.md
**The Elevator Pitch**

- **The Problem**: Wars are invisible, passive, isolated. Players ignore them.
- **The Solution**: Implement visibility (territory shifts), escalation (natural pressure), cascades (supply chains break).
- **The Payoff**: 32-40 hours of work → warfronts feel AAA while staying indie-scoped.
- **What to Do**: Read the 4 documents, plan 3-4 tranches, execute 8 gates.

---

### warfront_mechanics_research.md
**The Deep Dive**

- **Part 1**: Competitive analysis table (Stellaris, Starsector, X4, Sins2, Distant Worlds, SPAZ 2)
- **Part 2**: Your current architecture (what's working, what's missing)
- **Part 3**: Mechanical recommendations (3 tiers: critical, high-impact, AAA polish)
- **Part 4**: Indie vs AAA scope (MVP = 32 hours, Enhanced = 52 hours, Full = 86 hours)
- **Part 5**: Design principles (visibility, transparency, agency, feedback loops, cascades)
- **Part 6**: Reference patterns (intensity-driven multipliers, hysteresis, deterministic schedulers)
- **Part 7**: Testing & validation (3 test types, what to verify)
- **Part 8**: Game comparison matrix (detailed, feature-by-feature)
- **Part 9**: Gotchas & anti-patterns (5 common mistakes)
- **Part 10**: Implementation roadmap (week-by-week breakdown)

**Best For**: Design leads making big-picture decisions. Architects planning system interactions.

---

### game_reference_matrix.md
**The Decision Tree**

- **Decision Matrix**: Cost vs Impact (which features to copy)
- **Feature Adoption Guide**: Priority 1-5 (what to implement first)
- **Comparison by Game**: Territory, Escalation, Economics, Objectives
- **What NOT to Copy**: Claims economy (Stellaris), blockades (X4), weariness (Distant Worlds), NPC autonomy (X4)
- **Feature Depth Tiers**: A (Core Feel), B (Economic Teeth), C (NPC Immersion), D (Diplomacy)
- **The 80/20 Bundle**: Exactly 6 features from 3 games = AAA feel
- **Shopping List**: Table with time + impact for each feature

**Best For**: Project managers deciding what to build. Design leads choosing which games to reference.

---

### warfront_mechanics_quick_ref.md
**The Implementation Cheat Sheet**

- **One-page comparison matrix**: All 6 games, all 8 mechanics
- **Critical implementation sequence**: Phase 0-3, ordered by dependency
- **Phase 1**: Territory capture (20 hours) — 1a-d gates
- **Phase 2**: Economic consequence (12-16 hours) — 2a-b gates
- **Phase 3**: Player agency (10-12 hours) — 3a-b gates
- **Phase 4**: Optional polish (10 hours) — 3c gates
- **Tuning knobs**: All constants in one place (copy-paste ready)
- **Warfront status board mock-up**: What HUD displays to player
- **Unit test templates**: 3 example tests (territory capture, escalation, embargo)
- **Bridge query methods**: Signatures + implementation hints
- **Gotchas**: 5 specific pitfalls + solutions
- **Playtesting checklist**: 4 sessions, specific assertions

**Best For**: Engineers implementing gates. QA planning playtest sessions.

---

### warfront_implementation_gates.md
**The Gate Breakdown**

- **Family S32.WARFRONT_TERRITORIAL.V0**: 2 gates (territory capture + objectives)
- **Family S32.WARFRONT_ESCALATION.V0**: 2 gates (escalation loop + HUD)
- **Family S32.WARFRONT_ECONOMY.V0**: 2 gates (production chains + embargo)
- **Family S32.WARFRONT_BRIDGE.V0**: 1 gate (bridge queries)
- **Optional**: 2 gates (patrol feedback, NPC behavior)

Each gate includes:
- Task description (1-2 sentences)
- Modified files list
- New properties/tweaks
- Determinism notes
- Test requirements
- Acceptance criteria
- Effort estimate

**Execution Order Diagram**: Shows parallel groups (can run in parallel) vs sequential (must wait).

**Best For**: Sprint planners assigning gates. Gate authors understanding requirements.

---

### warfront_before_after_examples.md
**The Visual Examples**

- **Example 1**: Territory capture (discs change color on control shift)
- **Example 2**: Escalation pressure (no supplies → intensity increases)
- **Example 3**: Supply chain cascade (embargo breaks downstream production)
- **Example 4**: Territory shifts → tariff shocks (routes become unaffordable)
- **Example 5**: Escalation countdown HUD (live dashboard example)
- **Example 6**: Economic pressure over 1000 ticks (player ignores war, pays price)
- **Example 7**: Objective capture mechanics (dominance ticks tracking)
- **Example 8**: Dynamic NPC behavior (patrol frequency scales with intensity)
- **Summary**: Before/after transformation table

**Best For**: Motivation. Showing stakeholders what change looks like. Playtesting to verify examples match reality.

---

## Key Numbers to Remember

### Time Investment
- **MVP** (visible warfronts): 32 hours (2 tranches)
- **Enhanced** (+ economic cascades): 52 hours (2-3 tranches)
- **Full** (+ NPC immersion): 62 hours (3-4 tranches)

### Feature Priority
1. Territory capture (game-changer)
2. Escalation loop (creates pressure)
3. Strategic objectives (tactical depth)
4. Supply chains (economic teeth)
5. HUD (visibility)

### Test Count
- 8 gates × 2-3 test files = ~16 test classes
- ~60+ test cases (determinism + unit)
- Golden hash updates needed after gates 1-3

### Tuning Knobs (All in WarfrontTweaksV0.cs)
- `TerritoryCaptureTicks` = 20
- `EscalationIntervalTicks` = 200
- `AttritionBasePerTick` = 2
- `MunitionsDemandMultiplierPct` = 400
- (+ 6 more in the document)

---

## How to Use This Package

### Week 1: Planning
- [ ] Everyone reads WARFRONT_RESEARCH_SUMMARY.md (20 min)
- [ ] Design lead reads warfront_mechanics_research.md (1.5 hr)
- [ ] PM reads game_reference_matrix.md + warfront_implementation_gates.md (1 hr)
- [ ] Team discusses: agree on phases to implement (MVP vs Enhanced vs Full)
- [ ] Assign gate authors based on expertise (core vs bridge vs UI)

### Week 2: Sprint 1
- [ ] Gate authors read warfront_implementation_gates.md (their gates) + warfront_mechanics_quick_ref.md (15 min each)
- [ ] Implement Phase 1 gates (territory + escalation) in parallel
- [ ] Unit tests, golden hash update
- [ ] Code review using research as context

### Week 3: Sprint 2
- [ ] Implement Phase 2 gates (production chains, embargo cascade)
- [ ] Playtest against warfront_before_after_examples.md examples
- [ ] Tune constants in WarfrontTweaksV0.cs based on playtest feedback
- [ ] QA runs playtesting checklist from warfront_mechanics_quick_ref.md

### Week 4: Polish
- [ ] Implement Phase 3 gates (HUD, bridge)
- [ ] Optional Phase 4 gates (NPC behavior) if time allows
- [ ] Full playtesting, visual validation
- [ ] Write-up: "How warfronts became alive" (for postmortem)

---

## Questions This Package Answers

**Q: What should we copy from AAA games?**
A: Territory capture (Sins2), escalation (DW2), supply cascades (Sins2), objectives (Sins2). Not claims (Stellaris), not blockades (X4), not weariness (DW2).

**Q: How much work is this?**
A: MVP = 32 hours (2 tranches). Full = 62 hours (3-4 tranches).

**Q: Which gates are blocking?**
A: None. Phase 1 gates can run parallel. Phase 2 can run parallel. Phase 3 depends on Phase 1 only (golden hash).

**Q: What's the minimum viable warfront?**
A: Territory capture + escalation loop. 20 hours. Makes wars feel dynamic.

**Q: How do I test this?**
A: Golden hash tests for determinism. Unit tests for logic. Playtests for feel. See warfront_mechanics_quick_ref.md for templates.

**Q: How do I tune it?**
A: All constants in WarfrontTweaksV0.cs. Adjust, rerun tests, commit. See tuning knobs section.

**Q: Why not just implement everything at once?**
A: Phased approach allows MVP validation + iteration before investing in polish. Reduces risk.

---

## Next Steps

### Right Now
1. Pick a reading path above based on your role
2. Read the documents (20 min to 2 hours depending on path)
3. Discuss with team: "Which phases do we do? MVP? Full?"

### This Week
1. Design lead: finalize which features to build
2. PM: estimate gates + assign to tranches
3. QA: prep playtest checklist + test harness

### Next Week
1. Gate authors: start implementing Phase 1 gates
2. QA: set up golden hash baseline
3. Engineers: review each gate against research docs

---

## Contact / Questions

If you have questions while reading:
1. Check the relevant document's "Gotchas" or "FAQ" section
2. Reference the game comparison matrix to clarify AAA patterns
3. Look up the specific gate in warfront_implementation_gates.md
4. Check warfront_mechanics_quick_ref.md for tuning guidance

---

## Appendix: Document Map

```
README_WARFRONT_RESEARCH.md (you are here)
├─ WARFRONT_RESEARCH_SUMMARY.md (start here for overview)
├─ warfront_mechanics_research.md (competitive analysis + deep dive)
├─ game_reference_matrix.md (decision tree + which to copy)
├─ warfront_mechanics_quick_ref.md (implementation cheat sheet)
├─ warfront_implementation_gates.md (concrete gate breakdown)
└─ warfront_before_after_examples.md (visual motivation)
```

All docs are in `docs/design/` directory.

---

**Happy reading. Go make warfronts alive.**

