# Economy Simulation Implementation Plan

> Aggressive evaluation of all recommendations in `economy_simulation_v0.md`.
> Each item rated: **DO** (clear ROI), **DEFER** (not yet needed), **KILL** (over-engineering).
> Verified against codebase 2026-03-21.

---

## Audit Results: What We Thought Was Broken vs. Reality

Before planning, we verified every claimed gap against source code. **Six of
eleven "Phase 2 gaps" were already working.** This matters — implementing
"fixes" for working systems wastes gates and risks breaking determinism.

| Originally Claimed | Actual Code State | Action |
|---|---|---|
| Module sustain not enforced | `SustainSystem.ProcessModuleSustain()` actively deducts cargo | NO ACTION — working |
| Fleet fuel not consumed | Player every tick, NPC every 2nd tick | NO ACTION — working |
| PressureSystem dead code | 8 active InjectDelta() call sites | NO ACTION — working |
| Price breakdown UI missing | `GetPriceBreakdownV0()` fully implemented | NO ACTION — working |
| Only 3-4/9 recipes placed | 8/9 placed, only Electronics missing | SCOPE REDUCED to 1 recipe |
| Credit tracking missing | Full transaction ledger exists | SCOPE REDUCED to macro aggregation only |

**Lesson:** Always verify against code before planning. Design docs and chat
summaries drift from implementation reality.

---

## TIER 1: DO NOW (Highest Impact, Lowest Risk)

These items have clear, measurable impact on player experience and align
directly with core design pillars. Each is a single gate.

### 1.1 Per-Good Base Prices

**Rating: DO — Critical**

**Problem:** All 13 goods use `BasePrice=100`. Exotic Matter costs the same
baseline as Fuel. The entire value progression curve is flat.

**Lore requirement:** `trade_goods_v0.md` explicitly defines price bands:
- Low (50-100): Fuel, Ore, Organics, Food
- Mid (150-300): Metal, Composites, Munitions, Electronics
- High (400-800): Components, Rare Metals, Exotic Crystals, Salvaged Tech
- Very High (1000-2000): Exotic Matter

**Implementation:**
```
1. Add BasePrice field to GoodDefV0 (or ContentRegistry good definitions)
2. Set per-good values matching design bands:
   Fuel=60, Ore=70, Organics=80, Food=90,
   Metal=200, Composites=250, Munitions=220, Electronics=280,
   Components=500, RareMetals=600, ExoticCrystals=650, SalvagedTech=550,
   ExoticMatter=1500
3. Update Market.GetMidPrice() to use good-specific BasePrice
4. Update golden hashes (all tick logic changes)
5. Verify NPC trade still self-starts (profit thresholds may need scaling)
```

**Risk:** NPC profit threshold (3 cr) is calibrated for 100 cr goods.
With 1500 cr Exotic Matter, 3 cr is meaningless. **Must scale profit
threshold to percentage of base price** (e.g., 3% of base = 3 cr for Fuel,
45 cr for Exotic Matter). Otherwise NPCs trade everything regardless.

**Effort:** 1 gate. Hash-affecting. ~1-2 hours.

**Why now:** Without this, the first trade the player makes (buying Fuel)
feels identical in value to an endgame Exotic Matter trade. The entire
progression curve is invisible. This is the single highest-impact economy
change for player experience.

---

### 1.2 AssembleElectronics Industry Site

**Rating: DO — Critical for pentagon ring**

**Problem:** Electronics can't be produced in-world. The Communion→Chitin→Weavers
dependency chain (the deepest and most strategically interesting part of the
pentagon ring) is dead. Players never see Exotic Crystals feeding Electronics
production because there are no Electronics factories.

**Lore requirement:** `trade_goods_v0.md` defines the recipe:
`Exotic Crystals + Fuel → Electronics` at specific nodes.

**Implementation:**
```
1. In MarketInitGen.cs, add placement rule:
   if (i % 8 == 4) → AssembleElectronics site
   (every 8th node, offset 4 — ~12.5% placement)
   Inputs: 2 Exotic Crystals + 1 Fuel per tick
   Outputs: 2 Electronics per tick
2. Update golden hashes
3. Verify Electronics production starts flowing
```

**Risk:** Exotic Crystals are scarce (fracture-only). Electronics factories
will run at low efficiency because crystal supply is limited. **This is
correct behavior** — it makes the Electronics chain the bottleneck it's
designed to be.

**Effort:** 1 gate. Hash-affecting. ~30 min.

---

### 1.3 NPC Warfront-Aware Trade Scoring

**Rating: DO — Aligns with Dynamic Tension pillar**

**Problem:** NPCs evaluate trades on raw profit margin only. A Concord trader
happily routes through a TotalWar Valorin warzone because the margin is 5 cr
higher there. This contradicts both lore (factions have self-preservation) and
game feel (NPCs should visibly avoid danger).

**Lore requirement:** `dynamic_tension_v0.md` — "war felt through prices, not
battlefields." If NPCs avoid warfronts, supply drops at contested nodes,
prices rise, creating the designed economic cascade.

**Implementation:**
```
In NpcTradeSystem.FindBestOpportunity():

  // After computing baseScore:
  int warfrontTier = state.GetWarfrontTierForNode(destNode);
  int instability = state.GetInstabilityLevel(destNode);

  // Risk multiplier: 1.0 (safe) → 0.3 (TotalWar)
  int riskPenaltyBps = warfrontTier * 2000 + instability * 50;
  int riskMultBps = Math.Max(3000, 10000 - riskPenaltyBps);

  adjustedScore = baseScore * riskMultBps / 10000;
```

**Risk:** If too aggressive, contested nodes get zero NPC supply and prices
spike to infinity. **Cap risk penalty at 70% reduction** (riskMultBps min 3000)
so some brave NPCs still trade at warfronts.

**Effort:** 1 gate. Hash-affecting. ~1 hour.

**Why this matters:** This single change activates the entire warfront→economic
cascade design. Right now warfronts have elevated demand but NPCs happily
supply them, preventing scarcity. With risk avoidance, NPC supply drops
at warfronts → prices spike → player sees opportunity → player becomes the
warfront supplier → player feels war through wallet.

---

### 1.4 NPC Trade Cooldown (Anti-Ping-Pong)

**Rating: DO — Low effort, immediate quality improvement**

**Problem:** A trader who sells Ore at Node A can immediately buy Ore at Node B
and sell back at Node A if the spread is right. This creates visible
oscillation that looks broken.

**Implementation:**
```
// Per-fleet state: Dictionary<(string goodId, string nodeId), int lastTradeTick>
// In FindBestOpportunity, skip if same good traded at same node within 45 ticks

if (fleet.TradeCooldowns.TryGetValue((goodId, nodeId), out int lastTick)
    && state.Tick - lastTick < NpcTradeTweaksV0.TradeCooldownTicks)
    continue;
```

**Risk:** Minimal. 45-tick cooldown is 3 evaluation cycles. Worst case,
traders idle briefly before finding alternative routes.

**Effort:** 1 gate. Hash-affecting. ~30 min.

---

## TIER 2: DO SOON (Clear Value, Moderate Effort)

### 2.1 Dynamic Fleet Population (Soft-Cap Replacement)

**Rating: DO — But simpler than the design doc proposes**

**Problem:** War losses are permanent. A faction that loses 3 traders in a
warfront never recovers trade capacity in that region.

**Aggressive evaluation:** The full X4-style system with goods-consuming
shipyard spawning is over-engineered for STE's 60-70 fleet economy.

**Simplified implementation:**
```
Every 500 ticks, per faction:
  current = count active faction fleets
  target = faction.TerritoryNodes.Count * doctrine.FleetsPerNode

  if current < target * 0.8:
    // Spawn at most prosperous station
    // Deduct 50 Metal + 20 Components from that station's market
    // (ties fleet replacement to economy health)
    SpawnReplacementFleet(faction, bestStation)
    // Cap: 1 replacement per faction per cycle

  // NO retirement system — static cap is fine for 60-70 fleets
```

**Kill the retirement logic.** Fleet oversupply self-corrects: idle NPCs
don't cause problems, they just don't trade. Actively despawning NPCs
removes visual presence and feels hostile.

**Effort:** 2 gates (1 SimCore, 1 test+balancing). Hash-affecting.

---

### 2.2 Ambient Life Bridge Signals

**Rating: DO — Enables all Phase 3 visual work**

**Problem:** Godot has no SimCore-derived signals for ambient atmosphere.
Every Phase 3 visual gate depends on this.

**Implementation:**
```csharp
// SimBridge methods:
Dictionary GetNodeEconomySnapshotV0(string nodeId)
{
    return new Dictionary {
        ["traffic_level"]  = CountFleetsTargetingNode(nodeId),  // 0-10
        ["prosperity"]     = AvgInventoryRatio(nodeId),          // 0.0-2.0
        ["industry_type"]  = GetPrimaryIndustryType(nodeId),     // "mine"/"refinery"/"fab"/etc
        ["warfront_tier"]  = GetWarfrontTier(nodeId),            // 0-4
        ["faction_id"]     = state.NodeFactionId[nodeId],
        ["docked_fleets"]  = CountDockedFleets(nodeId),          // int
    };
}
```

**Effort:** 1 gate. Non-hash-affecting (bridge only).

---

### 2.3 Economy Digest Reports

**Rating: DO — But minimal viable version only**

**Problem:** Player can't see macro economy trends. Distant events are invisible.

**Aggressive evaluation:** A full "Galactic Trade Commission" report with
aggregate indices is over-scoped. The player needs 3 things:
1. Price alerts (significant changes at nodes they've visited)
2. Stockout warnings (goods running out somewhere relevant)
3. War impact summary (demand spikes at warfronts)

**Implementation:**
```csharp
// SimBridge method:
Array GetMarketAlertsV0(int sinceTick)
{
    var alerts = new List<Dictionary>();
    foreach node:
        foreach good where |price_now - price_at_sinceTick| > 20%:
            alerts.Add(new { nodeId, goodId, oldPrice, newPrice, reason });
        if any good stock == 0:
            alerts.Add(new { nodeId, goodId, type = "stockout" });
    return alerts (max 10, sorted by magnitude);
}
```

**Kill:** Galaxy-wide inflation indices, trade flow heatmaps, NPC activity
reports. These are analytics tools, not player-facing features. The player
needs actionable trade intelligence, not an economics dashboard.

**Effort:** 1 gate. Non-hash-affecting.

---

### 2.4 Macro Credit Monitoring

**Rating: DEFER — Not player-facing, diagnostic only**

**Aggressive evaluation:** The Eve Online MER pattern is for live-service MMOs
with player-driven economies and 20+ years of inflation history. STE is a
single-player game with a 10-20 hour campaign. Credit inflation over 20 hours
is either a tuning problem (fix the constants) or a design failure (fix the
sinks). Runtime monitoring doesn't solve either.

**Instead:** Add a stress bot test that runs 2000 ticks and asserts:
- No node has zero trades for 200+ consecutive ticks
- Average good price stays within 2× of base price
- Player credit growth rate is positive but bounded

This catches economy drift during development without runtime overhead.

**Effort:** 1 gate (test only). Non-hash-affecting.

---

## TIER 3: DEFER (Nice-to-Have, Low Urgency)

### 3.1 NPC Personality Seeds

**Rating: DEFER — Low player-visible impact**

**Aggressive evaluation:** Players rarely observe individual NPC trading
decisions long enough to notice personality. What they DO notice is variety
in visual behavior (different ships, different routes). That comes from
faction doctrines (already implemented) and ambient traffic (Phase 3 visual).

**The personality seed system adds complexity to a deterministic simulation
for a benefit players won't perceive.** If every Concord trader has
slightly different risk tolerance, the aggregate behavior looks the same
from 10,000 feet.

**When to reconsider:** If playtesters specifically report "all NPCs feel
identical" after ambient visuals are in place.

---

### 3.2 Convoy Behavior

**Rating: KILL — Over-engineering**

**Aggressive evaluation:** Convoys are a multi-fleet coordination system
requiring: proximity detection, leader/follower AI, formation maintenance,
and special-case handling when the leader stops. This is 2 gates of complex
work for a visual-only benefit.

**What actually makes lanes feel alive:** Cosmetic ship sprites (Phase 3,
1 gate). Same visual result, 1/4 the complexity.

**When to reconsider:** Never, unless the game adds fleet-level combat where
convoy tactics matter mechanically.

---

### 3.3 Dynamic Station Construction

**Rating: KILL — Wrong game**

**Aggressive evaluation:** This is X4's endgame. STE is a trader-pilot game
where you're Han Solo, not a station builder. Factions building new stations
based on demand requires: construction AI, resource allocation priorities,
site selection algorithms, and 3D station placement. 3 gates minimum.

The pentagon ring is DESIGNED to constrain station placement. Dynamic
construction undermines the designed scarcity that makes the economy
interesting.

---

### 3.4 Embargo Smuggling Infrastructure

**Rating: DEFER — Needs narrative content first**

**Aggressive evaluation:** The mechanics are straightforward (detect embargoed
good in cargo, apply smuggling flag, offer black market prices). But the
payoff requires narrative reactions — NPCs confronting you, faction
consequences, smuggling storylines. Without narrative content, it's just
"sell at higher price with a flag."

**When to reconsider:** After NARRATIVE_CONTENT epic progresses.

---

### 3.5 NPC Faction Loyalty

**Rating: DEFER — Emergent from warfront avoidance**

**Aggressive evaluation:** If NPC warfront avoidance (1.3) is implemented,
NPCs naturally avoid enemy territory (warfronts are AT faction borders).
This creates de facto faction loyalty without explicit trade policy checks.

A Concord trader won't route through Valorin territory during war because
the risk multiplier penalizes the score. Same result, zero new code.

**When to reconsider:** If cross-faction NPC trading during peacetime is
reported as immersion-breaking.

---

## TIER 4: PHASE 3 VISUAL WORK (Depends on Tier 2.2)

All of these require the ambient life bridge signals (2.2) to be in place
first. They are Godot-only work (non-hash-affecting) and should be gated
after 2.2 completes.

| Item | Effort | Assessment |
|------|--------|------------|
| Ambient station traffic | 2 gates | **DO** — Biggest world-feel win. Cosmetic shuttles driven by traffic_level. |
| Mining operation visuals | 1 gate | **DO** — Extraction beams at industry nodes. Simple VFX, high immersion. |
| Station prosperity visuals | 1 gate | **DO** — Lighting/damage tiers from prosperity signal. |
| Lane traffic cosmetics | 1 gate | **DO** — Billboarded distant ship sprites. Cheap, effective. |
| Warfront VFX | 2 gates | **DEFER** — Distant explosions need audio+VFX work. Do after combat feel is stable. |
| Fleet personality in UI | 1 gate | **DEFER** — Named captains need content (name generators, faction name pools). |
| Station personality | 1 gate | **DEFER** — Architecture variants need 3D asset work. |

---

## Execution Order

```
TIER 1 (do first, parallel where possible):
  ┌─ 1.1 Per-Good Base Prices ─────────── hash-affecting, SimCore
  │  1.2 Electronics Industry Site ─────── hash-affecting, SimCore
  │  1.3 NPC Warfront Avoidance ────────── hash-affecting, SimCore
  └─ 1.4 NPC Trade Cooldown ───────────── hash-affecting, SimCore

  All four are SimCore-only, hash-affecting gates that can be implemented
  sequentially in one session (shared golden hash update at the end).

TIER 2 (after Tier 1 golden hashes stabilize):
  ┌─ 2.1 Dynamic Fleet Population ──────── hash-affecting, SimCore (2 gates)
  │  2.2 Ambient Life Bridge Signals ───── non-hash, SimBridge
  └─ 2.3 Economy Digest Reports ────────── non-hash, SimBridge
  +  2.4 Economy Stress Test ───────────── non-hash, test only

  2.2 and 2.3 can parallel with 2.1 (different files).

TIER 4 (after 2.2 completes):
  ┌─ Ambient Station Traffic (2 gates) ─── Godot-only
  │  Mining Operation Visuals (1 gate) ──── Godot-only
  │  Station Prosperity Visuals (1 gate) ── Godot-only
  └─ Lane Traffic Cosmetics (1 gate) ────── Godot-only

  All Godot-only, fully parallelizable.
```

**Total gates:** 14 gates across Tiers 1-4 (down from 26+ in the original
uncritical assessment).

**Killed:** 3 items (convoys, dynamic station construction, NPC convoy formation)
**Deferred:** 5 items (macro credit monitoring, personality seeds, embargo smuggling,
  faction loyalty, warfront VFX + fleet names + station personality)

---

## Lore Alignment Checklist

Every DO item verified against design documents:

| Item | Pentagon Ring | Dynamic Tension | Trade Goods | Haven Rules |
|------|-------------|----------------|-------------|-------------|
| 1.1 Base Prices | Preserves ring (affinity still applies) | Enhances (value progression) | **Required** by doc | No impact |
| 1.2 Electronics Site | **Completes** ring chain | Enables cascade | **Required** by doc | No impact |
| 1.3 Warfront Avoidance | Strengthens ring (NPCs stay in faction) | **Enables** pillar 1 cascade | No impact | No impact |
| 1.4 Trade Cooldown | No impact | No impact | No impact | No impact |
| 2.1 Fleet Replacement | Preserves ring (faction doctrine) | Prevents stagnation | No impact | No impact |
| 2.2 Bridge Signals | No impact | No impact | No impact | No impact |
| 2.3 Digest Reports | No impact | Enhances (war visibility) | No impact | No impact |

**No item violates any hard constraint.** Pentagon ring preserved. Haven rules
untouched. Warfront determinism maintained. All hash-affecting changes go
through golden hash update.

---

## What This Plan Does NOT Do (Consciously)

1. **Does not add new goods.** 13 is the right count. Adding goods doesn't
   add depth — it adds width. Depth comes from interconnection.

2. **Does not add NPC credit budgets.** NPCs trade for free. This is correct.
   NPC credit economies create cascade failures (bankrupt NPCs stop trading →
   economy dies). Eve Online learned this — NPC buy/sell orders are infinite.

3. **Does not add production chain visualization.** The player discovers
   chains through play, not through diagrams. Outer Wilds principle: knowledge
   gates, not UI gates.

4. **Does not add hauler search radius scaling.** 2 hops is fine for a 20-node
   galaxy. If galaxy size grows, revisit then. YAGNI.

5. **Does not add station construction.** STE is a trading game, not a
   city-builder. The designed scarcity of fixed station placement IS the
   game's economic tension.

---

## Implementation Status (2026-03-21)

All Tier 1 + Tier 2 items are **DONE**. 1400/1400 tests passing.

| Item | Status | Files Changed |
|------|--------|---------------|
| 1.1 Per-Good Base Prices | DONE | ContentRegistryLoader.cs, Market.cs, NpcTradeTweaksV0.cs |
| 1.2 AssembleElectronics | DONE | MarketInitGen.cs, CatalogTweaksV0.cs |
| 1.3 NPC Warfront Avoidance | DONE | NpcTradeSystem.cs, NpcTradeTweaksV0.cs |
| 1.4 NPC Trade Cooldown | DONE | Fleet.cs, NpcTradeSystem.cs, NpcTradeTweaksV0.cs |
| 2.1 Dynamic Fleet Population | DONE | FleetPopulationSystem.cs (new), SimKernel.cs, FleetPopulationTweaksV0.cs |
| 2.2 Ambient Life Bridge Signals | DONE | SimBridge.Market.cs (GetNodeEconomySnapshotV0) |
| 2.3 Economy Digest Reports | DONE | SimBridge.Market.cs (GetMarketAlertsV0) |
| 2.4 Economy Stress Test | DONE | EconomyStressTests.cs (new, 2 tests) |

Golden hashes updated in GoldenReplayTests.cs and LongRunWorldHashTests.cs.
Balance baseline regenerated. TweakRoutingGuard allowlist updated.

---

## Deferred Items (Future Consideration)

These items were evaluated and consciously deferred. Each has a clear trigger
condition for when to revisit.

### NPC Personality Seeds
**Status:** DEFER
**Trigger:** Playtesters report "all NPCs feel identical" after ambient visuals
are in place. Currently, faction doctrines (different fleet compositions per
faction) and warfront avoidance (1.3) create sufficient behavioral variety.

### Convoy Behavior
**Status:** KILLED
**Reason:** Multi-fleet coordination (leader/follower AI, proximity detection,
formation maintenance) is 2 gates for a visual-only benefit. Cosmetic lane
sprites achieve the same "lanes feel alive" goal at 1/4 the complexity.
**Revisit:** Never, unless fleet-level combat tactics become a mechanic.

### Dynamic Station Construction
**Status:** KILLED
**Reason:** STE is a trader-pilot game, not X4. Fixed station placement IS
the designed scarcity that makes the economy interesting. The pentagon ring
constrains station placement deliberately. NPC station construction undermines
this design pillar.

### Embargo Smuggling
**Status:** DEFER
**Trigger:** NARRATIVE_CONTENT epic progresses. The mechanics are simple (detect
embargoed cargo, offer black market prices), but the payoff requires narrative
reactions — NPC confrontations, faction consequences, smuggling storylines.
Without narrative content, it's just a price flag.

### NPC Faction Loyalty
**Status:** DEFER (Emergent)
**Reason:** Warfront avoidance (1.3) creates de facto faction loyalty. NPCs
naturally avoid enemy territory because warfronts sit at faction borders.
A Concord trader won't route through Valorin warzone because the risk multiplier
penalizes the score. Same result, zero new code.
**Trigger:** Cross-faction NPC trading during peacetime reported as immersion-breaking.

### Hauler Search Radius Scaling
**Status:** DEFER (YAGNI)
**Reason:** 2-hop search is fine for a 20-node galaxy. If galaxy size grows
past 40 nodes, haulers may need adaptive search radius. Until then, the
current search covers sufficient trade opportunities.

### Macro Credit Monitoring
**Status:** REPLACED by Stress Test (2.4)
**Reason:** Runtime credit monitoring is an MMO pattern (Eve MER). STE is a
single-player 10-20 hour campaign. Economy drift is either a tuning problem
(fix constants) or design failure (fix sinks). The stress test
(EconomyStressTests.cs) catches drift during development without runtime overhead.

### Ambient Station Traffic (Godot)
**Status:** DEFER to Phase 3
**Depends on:** 2.2 (Ambient Life Bridge Signals) — now done.
**Scope:** Cosmetic shuttles driven by `traffic_level` signal. 2 Godot-only gates.
**Ready to implement** when visual polish tranche begins.

### Mining/Warfront/Prosperity Visuals (Godot)
**Status:** DEFER to Phase 3
**Depends on:** 2.2 (Ambient Life Bridge Signals) — now done.
**Scope:** Mining extraction beams, station lighting tiers, lane traffic sprites.
4 Godot-only gates total. All bridge signals are in place.
