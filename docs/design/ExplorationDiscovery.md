# Exploration & Discovery — Design Bible

> Design doc for the exploration loop: discovery phases, scanner range, knowledge
> management, anomaly encounters, anomaly chains, automation graduation, and the
> visual language of curiosity and revelation.
> Companion to `GalaxyMap.md` (fog of war, exploration overlay), `factions_and_lore_v0.md`,
> and `dynamic_tension_v0.md` (Pillar 5 — the revelation arc).
> Content authoring specs: `content/NarrativeContent_TBA.md` (discovery text templates),
> `content/LoreContent_TBA.md` (ancient data logs).
> Epics: `EPIC.S6.UI_DISCOVERY`, `EPIC.S6.ANOMALY_ECOLOGY`, `EPIC.S6.ARTIFACT_RESEARCH`,
> `EPIC.S6.TECH_LEADS`, `EPIC.S6.EXPEDITION_PROG`, `EPIC.S6.SCIENCE_CENTER`,
> `EPIC.S6.CLASS_DISCOVERY_PROFILES`, `EPIC.S6.MYSTERY_MARKERS`.
>
> **Last revised**: 2026-03-25 — v2 overhaul: reconciled implementation status with
> T41/T48 reality, consolidated 17 principles → 6 core axioms, added centaur model
> (3-tier competence aligned with fo_trade_manager_v0.md, Bainbridge paradox),
> personality-colored FO confidence (not bars), world adaptation (not error recovery),
> trade-history-as-evidence, MARKET_RUIN family, discovery failure states, KG player
> verbs + dual-mode display + link feedback (Obra Dinn model), re-exploration verb,
> qualitative Ancient Tech redesign, sustain→exploration loop, spectator trough
> prevention, FO observation limits. NPC route competition bumped to P1.
> Original v1 content (2026-03-20) preserved under axioms as supporting detail.

## Why This Doc Exists

Simple exploration (visit nodes, see what's there) works fine without a design doc. But
knowledge tracking — how the player records, reviews, and acts on discoveries — fails
catastrophically without architecture. Outer Wilds' Ship Log is praised because every
discovery connects to something larger. No Man's Sky at launch was criticized because
procedural discoveries had no persistent connections.

This doc defines what the game remembers for the player, what connects to what, and how
discovery milestones feel. It prevents exploration from becoming "checking boxes."

**Since v0**: This doc now also defines how discovery connects to the automation core loop
(the Factorio principle), how anomaly chains create multi-session narrative arcs, and how
information asymmetry creates the economic motivation for exploration.

---

## Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| IntelBook (observations, routes, prices) | Done | SimCore entity with persistence |
| Three discovery phases (Seen -> Scanned -> Analyzed) | Done | ScanDiscoveryCommand, phase transitions |
| DiscoverySitePanel UI (phase display, scan button) | Done | Polls per second, shows reward |
| DiscoveryOutcomeSystem (loot generation on analysis) | Done | Family-specific: Derelict/Ruin/Signal |
| Discovery seed types (Resource/Corridor/Anomaly) | Done | Deterministic seeding at world gen |
| Discovery phase markers on galaxy map | Done (T36) | Gray/amber/green icons at node positions |
| Knowledge graph entity model | Done (T18) | KnowledgeConnection entity + KnowledgeGraphSystem |
| Knowledge Web panel UI | Done (T36) | Enhanced in T36; breadcrumb trail still aspirational |
| Encounter narrative text / flavor text | Done (T30) | DiscoveryOutcomeSystem template-driven FlavorText |
| Progressive recontextualization | Done (T30) | Surface/Deep/Connection text gated by phase |
| Adaptation Fragments (16 + 8 resonance pairs) | Done (T34) | Dual cover/revealed lore, worldgen placement |
| T3 "Relic" discovery-only modules (9) | Done (T34) | Exotic matter sustain, IsDiscoveryOnly gate |
| Ancient ship hulls (3) | Done (T34) | Bastion/Seeker/Threshold, Haven T3+ restoration |
| Data log content (25 logs, 6 threads) | Done (T18) | DataLogContentV0.cs — 5 scientists authored |
| Narrative placement (BFS-based) | Done (T18) | NarrativePlacementGen.cs |
| Knowledge graph revelation triggers | Done (T39) | R1/R3/R5 seed connections on revelation fire |
| Kepler narrative chain (6 pieces) | Done (T18) | KeplerChainContentV0.cs |
| Active leads HUD display | Done (T36) | Leads surfaced in UI |
| Local system scan visualization | Done (T36) | Pulse effect, discovery site highlight |
| Cover-story naming discipline | Done (T37) | CoverName/RevealedName switch on R1 |
| Fracture derelict discovery gating | Done (T20) | FractureDerelict VoidSite, tick-gated unlock |
| Rumor/lead system | Partial | RumorLeads in IntelBook, leads in UI, full graph view pending |
| Scanner range visualization | Not implemented | No visual frontier ring on map |
| Exploration overlay lens | Not implemented | GalaxyMap.md aspiration |
| Discovery milestone audio/visual feedback | Not implemented | Phase transitions are silent |
| Breadcrumb trail visualization | Not implemented | No "here's what led you here" display |
| Scanner sweep animation on system entry | Not implemented | No ring-expanding animation |
| **Discovery-as-trade-intelligence** | **Partial (T41)** | `GenerateDiscoveryTradeIntel()` creates `TradeRouteIntel` with `SourceDiscoveryId`. FO triggers `FIRST_TRADE_ROUTE_DISCOVERED` + `TRADE_INTEL_STALE` with 3 archetype variants. **Remaining**: typed `EconomicIntel` entity, per-discovery `DISCOVERY_OPPORTUNITY` trigger, margin buffer wiring |
| **Anomaly chains (multi-site escalation)** | **Partial (T48)** | `AnomalyChainSystem` + `AnomalyChainContentV0` + entity. `TryAdvanceChains()` wired in DiscoveryOutcomeSystem. **Remaining**: per-step `ChainIntel`, FO commentary per step |
| **Automation graduation for scanning** | **Partial (T41)** | `SURVEY_AUTOMATION_SUGGESTED` trigger fires at 3+ manual scans with 3 archetype variants. **Remaining**: `SurveyProgram` program type |
| **Information asymmetry / intel decay** | **Partial (T41)** | `ApplyDiscoveryRouteDecay()` with distance bands (near 50t/mid 150t/deep 400t/fracture never). `DiscoveryIntelTweaksV0` exists. **Remaining**: margin buffer connection to ProgramSystem, NPC competition mechanics |
| **Late-game discovery continuation** | **Not implemented** | Economy-triggered anomaly spawning, instability-gated reveals |
| **Audio discovery vocabulary (4 signatures)** | **Not implemented** | Ping/process/reveal/insight audio signatures |
| **Anomaly ecology (spatial distribution)** | **Not implemented** | EPIC.S6.ANOMALY_ECOLOGY |
| **Artifact research (containment/experiments)** | **Not implemented** | EPIC.S6.ARTIFACT_RESEARCH |
| **Science center (analysis throughput)** | **Not implemented** | EPIC.S6.SCIENCE_CENTER |
| **Discovery failure states** | **Not implemented** | 6 failure types + partial success mechanic (§Discovery Failure States) |
| **Knowledge Graph player verbs** | **Not implemented** | Pin, Annotate, Link, Flag for FO, Compare (§KG Player Verbs) |
| **Knowledge Graph dual-mode display** | **Not implemented** | Geographic + relational views (§Dual-Mode Display) |
| **Re-exploration verb** | **Not implemented** | Re-Scan, Deep Analysis, Recontextualization (§Re-Exploration) |
| **Trade history as revelation evidence** | **Not implemented** | 3 progressive triggers linking player trade data to pentagon proof (§Trade History) |
| **FO confidence through personality** | **Not implemented** | Personality-specific confidence language per FO archetype (§FO Confidence Through Personality) |
| **Player-during-automation spectator prevention** | **Not implemented** | 5 boredom circuit breakers (§What the Player Does During Automation) |
| **Link feedback / Obra Dinn batch model** | **Not implemented** | Speculative→Plausible→Confirmed→Contradicted states (§Link Feedback) |
| **FO world adaptation + learning** | **Not implemented** | 5 world event types, 4 behavioral adaptation patterns (§FO Adaptation, §FO Learning) |

---

## Design Principles

### The Six Axioms (v2)

> These axioms are the design's load-bearing walls. Every feature, every gate, every
> prioritization decision should trace back to one of these six. If a proposed feature
> doesn't serve at least one axiom, it doesn't belong in the exploration system.
>
> Derived from: Outer Wilds, Subnautica, Stellaris, Elite Dangerous, X4: Foundations,
> FTL, Mass Effect, Factorio, EVE Online, No Man's Sky, and automation research
> (Lee & See 2004, Bainbridge 1983). See "Reference Games" section for full list.

**Axiom 1: Discovery is knowledge, not completion.**
The player builds understanding, not map coverage. Each discovery answers a question AND
raises a new one. Absence of data is itself information — "No data available" motivates
more than a hidden system. The knowledge graph reveals connections: two isolated facts
become a story. Never show a global completion percentage on the HUD. (Outer Wilds, Mass
Effect.) Our cover-story naming (CoverName → RevealedName on R1) embodies this: what
the player "knows" about an artifact changes completely at revelation.

**Axiom 2: Discovery feeds automation — the centaur model.**
Every discovery yields at minimum ONE of: (a) a new trade route to automate, (b) a
technology that improves existing automation, (c) trade intelligence that makes current
programs more profitable. The player explores; the FO builds. Neither succeeds alone.
This is the **centaur model** (chess term: human + AI > either alone). The player
provides access (fracture drive, faction contacts, personal judgment); the FO provides
analysis (pattern detection, route optimization, economic evidence). The rhythm:
Explore → discover intelligence → FO deploys automation → revenue funds next expedition
→ cycle repeats. Discovery disconnected from automation is a side activity.
(Factorio, X4, EVE. See §The Centaur Model for full spec.)

**Axiom 3: Chains create stories; one-shots create trivia.**
Every significant discovery opens a chain: 3-5 sites, escalating in risk, capability
requirement, and narrative weight. Each step yields better loot AND economic intel for
the FO. Early seeds pay off late — a Precursor inscription that seems decorative in
Act 1 becomes the key to a late-game system. Chains are the mid-game content engine
(tick 600-1200) that prevent the spectator trough. (Stellaris anomaly chains, Mass
Effect Prothean trail, Outer Wilds interconnected sites.)

**Axiom 4: Knowledge is perishable — explore or decay.**
Discovered trade intelligence is (a) exclusive initially, (b) perishable (NPCs learn
routes, intel ages), (c) deeper at higher scan tiers. The player must feel the
bottleneck BEFORE the discovery that resolves it (Factorio's "pain before relief").
Stale intel = wider margins = less profit = motivation to explore again. Fracture-space
discoveries are permanent exclusives (NPCs can't fracture-travel) — the deepest
knowledge is the most durable. (EVE Online, X4, Elite Dangerous.)

**Axiom 5: Mystery degrades gracefully, never fully resolves.**
Never fully explain the thread builders. Never name them. Never show them. Constrained
randomness (4-6 outcomes per family) lets players develop intuition without prediction.
Procedural sites with authored skeletons (No Man's Sky Desolation pattern): every site
feels designed because the dramatic arc is authored, only details are generated. The game
remembers so the player doesn't have to — IntelBook records everything, surfaces
connections, stores ambiguous findings for later recontextualization. Incomplete knowledge
is always more compelling than complete knowledge. (Mass Effect, FTL, No Man's Sky,
Obra Dinn.)

**Axiom 6: Milestones are moments — celebrate then automate.**
Phase transitions deserve celebration: brief pause, distinct audio, visual flourish, card
showing what was learned. Silent transitions waste emotional investment. But: the 500th
scan must NOT feel like the 1st. After 3+ manual encounters with a family, the FO suggests
automation (SurveyProgram). First-discovery credit (player name on first-analyzed sites)
is cheap and disproportionately motivating. Environment shows what happened; FO explains
what people thought about it — neither alone is sufficient. (Zelda secret-found jingle,
Elite Dangerous first-discovery credit, Subnautica scan visor.)

### Supporting Principles (detail)

> These expand on the axioms with specific design guidance. Each traces to an axiom.

| # | Principle | Axiom | Key Insight |
|---|-----------|-------|-------------|
| S1 | Curiosity, not completion percentage | 1 | Show what the player DOESN'T know. Never put completion % on HUD — only in Intel tab |
| S2 | The game remembers so the player doesn't have to | 1 | IntelBook records everything. Player decides what to do with knowledge, not remembering it |
| S3 | Discoveries connect to each other | 1, 3 | Isolated discovery = trivia. Two connected discoveries = story. Knowledge graph reveals relationships |
| S4 | Automate the routine, preserve the novel | 2, 6 | Scan #1 = full atmospheric manual experience. Scan #500 = SurveyProgram toast notification |
| S5 | Pain before relief | 4 | Player feels the bottleneck (saturated route, negative margin) BEFORE discovery that resolves it |
| S6 | Planted seeds pay off late | 3, 5 | Early flavor text contains buried references only meaningful after later discoveries. CoverName → RevealedName |
| S7 | Discovery must continue into late game | 4 | 5 sources: fracture layers, faction intel, economy-triggered, chain unlocks, instability reveals |
| S8 | Environment shows; FO explains | 2, 6 | Discovery sites show evidence (damage patterns, layout); FO delivers analysis. Diegetic delivery |
| S9 | Constrained randomness, not chaos | 5 | 4-6 outcomes per family. Player develops intuition. Variance within known range is exciting |
| S10 | Procedural sites with authored skeletons | 5 | 15-20 narrative templates with dramatic arcs. Procedural fills specifics. Every site has one guaranteed memorable moment |
| S11 | First-discovery credit | 6 | Player/FO/ship name permanently on first-analyzed sites. Cheap to implement, disproportionately motivating (Elite Dangerous) |
| S12 | Incomplete knowledge > complete knowledge | 5 | "Scientists cannot yet transcribe." No single discovery explains a complete system. Every answer opens a deeper question |

---

## The Discovery Lifecycle

### Phase 1: Seen

**Trigger:** Player enters a system with seeded discoveries. Automatic — no player action.

**What the player learns:** Something is here. Type (Derelict/Ruin/Signal) and location.
No details.

**Visual treatment:**
- Galaxy map: dim gray marker at the node
- Local system: faint marker sphere at discovery position
- Discovery panel: "? Seen" label in muted text

**Emotional beat:** Curiosity. "What is that? Should I investigate?"

### Phase 2: Scanned

**Trigger:** Player clicks "Scan" in the DiscoverySitePanel while at the node.

**What the player learns:** Surface-level data. Material composition, energy signatures,
approximate age. Enough to decide whether to invest time in full analysis.

**NEW (v1):** Scanning now reveals **trade intelligence**: the scanned site's economic
fingerprint. For a Derelict, this includes salvage value estimates. For a Ruin, mineral
survey data. For a Signal, the frequency pattern that indicates what connected system
has a price anomaly. This intelligence is exclusive to the player until decay (see
"Information Asymmetry" section below).

**Visual treatment:**
- Galaxy map: amber marker at the node
- Local system: marker sphere brightens, gains subtle pulse
- Discovery panel: "~ Scanned" in amber, preview data revealed

**Emotional beat:** Investment. "This looks promising. I want to know more."

**Milestone feedback (aspiration):**
```
+-- SCAN COMPLETE -----------------------------------------+
|                                                           |
|  ~ Derelict Wreck -- Kepler System                       |
|                                                           |
|  "Sensor readings detect a hull fragment with             |
|   residual power. Construction pattern matches            |
|   no known active faction."                               |
|                                                           |
|  Energy signature: Fading (est. 200+ years)              |
|  Material: Unknown composite alloy                        |
|  Size: Small vessel (corvette class)                      |
|                                                           |
|  TRADE INTEL:                                             |
|    Salvage yield est.: 120-180 Salvaged Tech              |
|    Nearest buyer: Weaver station (2 hops, ~15cr/unit)     |
|                                                           |
|  [Analyze]  [Later]                                       |
+-----------------------------------------------------------+
```

### Phase 3: Analyzed

**Trigger:** Player clicks "Analyze" in the DiscoverySitePanel. Must be at the node.

**What the player learns:** Full data. Loot recovered, narrative revealed, connections
to other discoveries shown. **NEW (v1):** Full economic data enters the IntelBook with
fresh timestamps. Any connected Leads also include economic previews of the destination.

**Visual treatment:**
- Galaxy map: green marker at the node
- Local system: marker transforms into resolved visual (wreck debris, ruin structure)
- Discovery panel: "! Analyzed" in green, full reward text

**Emotional beat:** Revelation. "So THAT'S what happened here."

**Milestone feedback (aspiration):**
```
+-- ANALYSIS COMPLETE -------------------------------------+
|                                                           |
|  ! Derelict Wreck -- Kepler System                       |
|                                                           |
|  "The hull fragment is a Valorin scout vessel,            |
|   destroyed approximately 300 years ago. The damage       |
|   pattern suggests Communion weapons. This far from       |
|   Valorin space, the scout was likely on a covert         |
|   mission when intercepted."                              |
|                                                           |
|  RECOVERED:                                               |
|    Salvaged Tech x150                                     |
|    Credits: +500                                          |
|                                                           |
|  TRADE INTEL:                                             |
|    Weaver Drydock at Altair pays 18cr/unit (fresh)        |
|    Route margin: +1,200cr (before tariffs)                |
|    Intel freshness: 100% (decays over 200 ticks)          |
|                                                           |
|  CONNECTED DISCOVERY:                                     |
|    -> Signal detected at Altair-7                         |
|    "Similar energy signature. Possible second vessel"     |
|    (Altair trade intel: Electronics deficit, +30% margin) |
|                                                           |
|  [Collect Salvage]  [View in Intel Tab]                  |
+-----------------------------------------------------------+
```

---

## Discovery-as-Trade-Intelligence (NEW)

> **Axiom #2:** Discovery feeds automation — the centaur model.
> **Reference:** X4 (scanning = economic intel), EVE (information asymmetry),
> Factorio (discovery resolves current bottleneck).

### Why This Matters

Our game's core loop is automation. The player scouts routes and deploys programs. If
discovery only yields loot (exotic matter, credits, salvaged tech), it's a side activity
disconnected from the main game. Discovery must yield **trade intelligence** — the fuel
that makes automation programs more profitable.

### The Exploration → Automation Pipeline (Status)

The FO Trade Manager (`fo_trade_manager_v0.md`) depends on this system. The full pipeline:

```
Player explores → Discovery yields trade intel → FO evaluates intel →
FO builds automation → Revenue funds exploration → Cycle repeats
```

| Link | Status | Component |
|------|--------|-----------|
| 1. Player explores | **Done** | Galaxy gen, discovery phases, fracture travel |
| 2. Discovery yields trade intel | **PARTIAL (T41)** | `GenerateDiscoveryTradeIntel()` creates `TradeRouteIntel` entries. **Gap**: no typed `EconomicIntel` entity, no per-discovery FO beat |
| 3. FO evaluates intel | **PARTIAL (T41)** | `FIRST_TRADE_ROUTE_DISCOVERED` + `TRADE_INTEL_STALE` triggers fire with 3 archetype variants. **Gap**: no per-discovery `DISCOVERY_OPPORTUNITY` trigger, no margin buffer wiring |
| 4. FO builds automation | **Done** | ProgramSystem, TradeCharter, ResourceTap |
| 5. Revenue funds exploration | **Done** | Credit flow, module purchases |
| 6. Cycle repeats | **Done** | Route depreciation + intel decay creates exploration pressure |

**Links 2-3 are partially wired (T41) but incomplete.** The plumbing exists: discoveries
generate trade route intel, the FO fires stale-intel and first-route triggers with archetype
dialogue. What's missing: (a) typed `EconomicIntel` entity per discovery family, (b) per-
discovery `DISCOVERY_OPPORTUNITY` trigger (currently only aggregate triggers fire), (c) margin
buffer wiring so intel freshness mechanically affects program profitability. Without (a-c),
the player sees the FO react to discoveries in aggregate but doesn't experience the centaur
model moment-by-moment: "I found this → FO evaluated it → FO built something from it."

### Intel Decay → Program Profitability (BUILD FIRST)

This is the single highest-leverage feature. It creates the mechanical pressure loop
with ZERO new systems — IntelBook exists, ProgramSystem exists, FO dialogue exists.
It's wiring, not architecture.

**Mechanical spec:**

`IntelBook` already tracks per-node freshness (`LastVisitTick`). `ProgramSystem` already
reads source/dest nodes for trade programs. The missing wire:

```
ProgramSystem.CalculateEffectiveMargin():
  worstFreshness = Max(IntelBook.GetNodeAge(sourceId),
                       IntelBook.GetNodeAge(destId))
  marginBuffer   = FreshnessToBuffer(worstFreshness)
  effectiveMargin = rawMargin - marginBuffer
```

Freshness thresholds — **canonical schedule (v2, reconciled with T41 code)**:

> **IMPORTANT:** Intel decay is DISTANCE-BASED, not time-based. The T41 implementation
> (`ApplyDiscoveryRouteDecay()` in IntelSystem.cs) uses hop distance from player start
> to set decay windows. Deeper discoveries decay slower = stronger frontier motivation.
> This is the EVE Online model: wormhole intel is valuable because it's far away.

| Distance Band | Decay Window (ticks) | Margin Buffer | FO Confidence | FO Behavior |
|---------------|---------------------|---------------|---------------|-------------|
| **Near** (≤ 2 hops) | 50 ticks | 5% → 15% → 25% | "High / Moderate / Low" | Tight → conservative → paused |
| **Mid** (3-5 hops) | 150 ticks | 5% → 15% → 25% | "High / Moderate / Low" | Same progression, 3x slower |
| **Deep** (6+ hops) | 400 ticks | 5% → 15% → 25% | "High / Moderate / Low" | Same progression, 8x slower |
| **Fracture** | Never | 5% (permanent) | "High (stable)" | Permanent exclusive advantage |

Within each distance band, the margin buffer escalates at 33% and 66% of the decay window:

| Intel Status | When (% of decay window) | Margin Buffer | Display |
|-------------|------------------------|---------------|---------|
| **Fresh** | 0-33% | 5% | Green — exact values |
| **Aging** | 33-66% | 15% | Amber — values drift ±10% |
| **Stale** | 66-100% | 25% | Red — values drift ±25% |
| **Expired** | > 100% | Route paused | Gray — "Data unreliable" |

**FO dialogue trigger:** When a route's freshness degrades from Fresh → Aging, fire
`TRADE_INTEL_STALE` with personality-colored message:

```
Maren: "Route Delta's data is 800 ticks old. Margins are
       wider to compensate. Fresh intel would tighten them."
Dask:  "Route Delta's running on old maps. I'm playing it safe."
Lira:  "Haven't been out that way in a while. Route Delta
       could use a visit — or a new discovery nearby."
```

**Why this comes first:** Visiting a node refreshes intel → tightens FO margins →
increases profit. The player FEELS the connection between exploration and economic
output immediately. No new entity model, no new system — just a margin modifier
reading from existing `IntelBook.GetNodeAge()`.

### What Each Discovery Family Yields

| Family | Phase 2 (Scanned) Intel | Phase 3 (Analyzed) Intel |
|--------|------------------------|-------------------------|
| **Derelict** | Salvage value estimate, nearest buyer | Exact salvage manifest, optimal sell route, faction origin (reputation intel) |
| **Ruin** | Mineral survey (which goods are concentrated nearby) | Full resource map, production deficit at nearby stations, technology hint |
| **Signal** | Frequency pattern -> "this system has unusual trade activity" | Exact price anomaly data, supply/demand curves at destination, hidden route |
| **Market Ruin** (NEW) | Abandoned trade manifests, collapsed station infrastructure | Full failure autopsy: what was traded, supply chain that failed, early-warning indicators. FO can model current markets for similar vulnerabilities |

### NEW: Economic Intel Types (DiscoveryOutcome Extension)

> **CANONICAL SPEC:** This is the single authoritative definition of the
> `EconomicIntel` entity. All other references in this doc (§Chain Intel,
> §Trade History as Evidence, §Implementation Roadmap) point here. Do not
> duplicate this spec — extend it by adding new `IntelType` variants to the
> table below.

Currently `DiscoveryOutcomeSystem` produces loot (SalvagedTech, ExoticMatter, credits).
It must ALSO produce economic intel that writes to `IntelBook` and feeds the FO. Each
discovery family produces a specific `EconomicIntel` variant alongside existing loot:

| Discovery Family | Existing Loot | NEW: Economic Intel Produced |
|------------------|---------------|------------------------------|
| **Resource Site** | ExoticMatter, Ore | `ResourceDeposit`: node, good, estimated quantity. FO can propose extraction operation |
| **Derelict** | SalvagedTech, credits | `CargoManifest`: reveals what this ship was hauling and between which nodes. FO can infer a trade route |
| **Signal/Ruin** | DataLog, KnowledgeGraph connection | `MarketAnomaly`: reveals price data, faction consumption patterns, or hidden demand. FO can optimize existing routes or propose new ones |
| **Anomaly Chain** (intermediate steps) | Progressive per step | `ChainIntel`: each step reveals partial economic picture. Final step = comprehensive regional economic intel |
| **Market Ruin** (NEW v2) | Abandoned trade infrastructure, old manifests | `MarketRuin`: reveals what a market USED to trade, why it collapsed, and what economic conditions caused the failure. FO can infer current vulnerabilities in similar markets |

**Entity extension:**

```csharp
// NEW field on DiscoveryOutcome (or new sibling entity)
public sealed class EconomicIntel
{
    public string IntelType { get; set; } = "";  // ResourceDeposit|CargoManifest|MarketAnomaly|ChainIntel
    public string SourceNodeId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";  // inferred destination (if applicable)
    public string GoodId { get; set; } = "";
    public int EstimatedMarginBps { get; set; }      // basis points
    public int FreshnessTicks { get; set; }           // how long this intel stays exclusive
    public string NarrativeHint { get; set; } = "";   // FO-readable description
}
```

### NEW: FO Evaluation Beat (DISCOVERY_OPPORTUNITY Trigger)

When `DiscoveryOutcomeSystem` produces economic intel, it must fire
`FirstOfficerSystem.TryFireTrigger(state, "DISCOVERY_OPPORTUNITY")`. This is the moment
the exploration→automation pipeline becomes **visible to the player**.

```csharp
// In DiscoveryOutcomeSystem, after producing economic intel:
if (outcome.EconomicIntel != null)
    FirstOfficerSystem.TryFireTrigger(state, "DISCOVERY_OPPORTUNITY");
```

The FO immediately evaluates the intel in character — three variants per archetype:

```
[Player analyzes derelict at Node 14]
System: +1 SalvagedTech, +200cr
System: NEW — CargoManifest intel acquired (ore, Node 14 → Node 7)

Maren: "That wreck was hauling ore on a run from here to Node 7.
       Interesting — Node 7 is a refinery with steady demand.
       I estimate 22% margin on that route. Want me to set it up?"

Dask:  "Found the cargo log. This ship was running ore to the
       refinery at Node 7. Reliable run — I've seen that kind of
       demand hold. I can handle it if you want."

Lira:  "The manifest shows ore headed for Node 7 — a refinery
       tucked behind Chitin space. Good margins if we can
       navigate the politics. Want me to try?"
```

**This is the single most important UX moment in the entire system.** The player scans
a derelict, gets loot AND intel, and the FO immediately says "I can use this." That's
the centaur model made tangible. Without this beat, discovery and automation feel like
separate games.

### How This Connects to Automation

```
Player hits wall: "My TradeCharter on the Kepler-Altair route is earning 3cr/trip.
                    The route is saturated."

Player explores:  Travels to unexplored Deneb system. Discovers a Ruin (Phase 2 scan).
                  Ruin intel: "Heavy mineral concentrations — Rare Metals surplus at
                  Deneb, but Deneb station has no buyer."

Player analyzes:  Phase 3 analysis reveals: Valorin frontier station 2 hops away buys
                  Rare Metals at 22cr (vs 8cr average). Production deficit in Composites.
                  NEW: EconomicIntel(MarketAnomaly) written to IntelBook.
                  NEW: DISCOVERY_OPPORTUNITY fires — FO offers to build the route.

Player approves:  FO charters a hauler on Deneb → Valorin. Earning 14cr/trip.
                  The discovery RESOLVED the bottleneck AND the FO built the solution.

Next wall:        Valorin station saturates after 50 ticks. Intel ages → margins widen.
                  FO: "Deneb route is thinning. Fresh intel would help — or a new find."
                  Player needs a new discovery to find the next opportunity.
```

### The Centaur Model (NEW v2)

> **Core axiom (#2):** The player explores; the FO builds. Neither succeeds alone.
> **Research basis:** Kasparov's "Advanced Chess" (human + computer > either alone),
> Lee & See 2004 (trust calibration in automation), Bainbridge 1983 (ironies of
> automation), Parasuraman & Riley 1997 (trust ladder in human-automation teams).

The player-FO relationship is the game's central design challenge. Get it right and
the exploration→automation pipeline creates a uniquely satisfying partnership. Get it
wrong and the FO becomes either a notification system (too passive) or a replacement
for player agency (too active).

#### Competence Tiers (aligned with fo_trade_manager_v0.md)

> **Canonical source:** `fo_trade_manager_v0.md` §Competence Model. **Resolved:
> 3 tiers, growth through crisis survival. No domains, no XP bars.**
> The FO gets better as the player plays. Growth manifests as DIALOGUE and
> CAPABILITY, not numbers.

The trust research (Lee & See 2004, Parasuraman & Riley 1997, Bainbridge 1983)
informs the *principles* — transparency, never-skip-levels, manual-skill-maintenance
— but the mechanical structure is 3 tiers, not 5 levels:

| Tier | Name | FO Capability | When It Happens | **Mechanical Gate** |
|------|------|--------------|-----------------|---------------------|
| **1** | **Novice** | Charter-only. Runs 1-2 player-demonstrated routes. Basic sustain (fuel). Observes, comments on what the player does | FO selected during tutorial. Default state | Default — no gate |
| **2** | **Competent** | Runs 3-5 routes. Manages all sustain. Suggests adjacent-node extensions. Recommends first ship purchase | ~15 manual trades, 5+ nodes explored, first warfront survived. FO: "After the composites crisis, I started stockpiling reserves. Give me a few more routes — I can handle them now." | Crisis survival triggers tier-up dialogue. Player must explicitly approve expanded scope |
| **3** | **Master** | Full network (15+ routes). Fleet optimization. Proactive rebalancing. Warfront briefings with strategic depth. Economic anomaly detection | Haven operational, 8+ systems explored, endgame tier approached. FO proposes regional trade networks | Player approves strategic-level delegation. Can demote at any time |

**Growth through crisis, not accumulation.** The FO's most meaningful growth happens
when they survive adversity — a sustain emergency handled well, a warfront that
disrupted routes and was navigated. This makes growth feel earned and narrative.

**Tier never auto-advances.** Each crisis-survival gate fires a dialogue prompt;
the player must explicitly approve expanded scope. The player can also DEMOTE the
FO at any time via the Empire Dashboard. This is the anti-Stellaris rule: Paradox
removed sector automation entirely in patch 3.9 because players couldn't constrain
AI scope. Our FO's scope is always player-set.

**Trust calibration is organic, not metered.** The Route Query interaction
(`fo_trade_manager_v0.md` §Route Query) serves as trust calibration — the player
asks "why did you do that?" and gets a good answer, building trust through observed
competence. No trust bars, no XP meters, no visible numbers.

**Critical design constraint (Stellaris Sector Problem):** Delegation fails when
the AI is (a) stupid, (b) invisible, (c) requires full takeover to correct, or (d)
operates at the wrong abstraction level. X4's autotraders fail because players
*"display 'No traderoute found' despite profitable trades existing"* (Steam forums).
Stellaris sectors fail because *"the AI removes jobs and creates unemployed pops and
builds wrong buildings"* (Paradox community). Both share the root cause: the player
delegates, then witnesses incompetence they can't fix granularly.

**Our solution:** The FO's reasoning is always visible. The Empire Dashboard shows
WHY the FO chose a route (intel source, estimated margin, freshness). The player
can override any individual decision without dismantling the entire stack.

#### FO Confidence Through Personality (not bars)

> **Canonical source:** `fo_trade_manager_v0.md` Principle #5 — "FO Personality
> Shapes Flavor, Not Strategy." The FO IS the confidence display. Confidence
> information flows through dialogue, not alongside it.

Every FO trade recommendation carries implicit confidence — but expressed through the
FO's personality, not a generic UI element. The FO is a person, not a tooltip:

| Confidence | Maren (Analyst) | Dask (Veteran) | Lira (Pathfinder) |
|-----------|----------------|----------------|-------------------|
| **High** | "22% margin, ±2%. Intel is 12 ticks old. This is solid." | "Solid route. I've seen these hold." | "This one feels right. Everything lines up." |
| **Moderate** | "Estimate 18% margin, but data is 80 ticks old. Could be ±8%." | "Should work, but I've been burned by stale data before." | "Something about that market calls to me, but I can't pin down the risk." |
| **Low** | "I'd project positive, but with old data the error bars are wide." | "Dicey. I wouldn't bet my ship on it." | "My gut says yes, but my gut's been wrong in new territory." |
| **Unknown** | "Insufficient data. I can't even estimate." | "No idea. You'd need to go look." | "That's blank space on my map. Could be anything." |

**Implementation:** `TradeRouteIntel` gains a `ConfidenceScore` (0-100) computed from:
intel age (worst of source/dest), market volatility history, route proven-count (how
many successful trips). The FO translates this score into personality-appropriate
language. No colored bars — the confidence is in the words, tone, and certainty of
the FO's recommendation. Players learn to read their specific FO's signals.

**Why not bars?** A universal 4-level colored bar flattens the personality difference
into a generic HUD element. The whole point of choosing an FO is that they present
information differently. Maren players learn "±2%" means safe; Lira players learn
"feels right" means safe. This is the same information, delivered through character.

#### FO Adaptation to World Events

> **Canonical source:** `fo_trade_manager_v0.md` §FO Mistakes — "**Resolved: World
> failure, not FO incompetence.** The FO makes correct decisions that produce bad
> outcomes because the galaxy is unpredictable." The FO doesn't make mistakes — the
> galaxy surprises everyone.

When world events disrupt the FO's plans, it must (a) adapt the affected action,
(b) report clearly, (c) explain what changed in the world, and (d) suggest options:

| World Event | FO Adaptation | Player Options | FO Dialogue (Maren) |
|------------|--------------|---------------|---------------------|
| **Tariffs increased** (route becomes unprofitable) | Pauses route, holds cargo | Resume / reroute / abandon | "Route Delta went negative — tariffs increased 15% since our last visit. I've paused. Want me to reroute, or should you investigate?" |
| **Intel aged out** (data too old to trust) | Widens margin buffers, flags for rescan | Rescan node / find alternative / accept wider margins | "I'm operating blind on the Kepler leg. Data is 400 ticks old. I've widened margins to compensate, but I'd rather have fresh intel." |
| **Faction conflict** (border closed) | Reroutes if alternatives exist, escalates if not | Approve reroute / negotiate access / find new route | "Chitin border closed. I've rerouted through Valorin space — 8% less margin but safe. Approve?" |
| **Market shifted** (prices diverged from intel) | Adjusts route parameters, reports discrepancy | Accept adjusted route / abandon | "Prices at Deneb shifted — actual margins are 20% below what our discovery data predicted. Route still works but thinner." |
| **Player manual trade** (overlaps FO route) | Asks for clarification, does NOT override | Explain intent / let FO adapt / revoke FO authority on that route | "You just sold Ore at Nexus-7 — that's the same good I was routing to Kepler. Want me to adjust, or are you taking that market back?" |

**Framing matters:** The FO is never wrong — the galaxy is unpredictable. Pirates hit
between intel refreshes. Warfronts disrupt routes. Markets shift. The FO's competence
isn't in question; the galaxy's reliability is. This preserves trust while creating
interesting adaptation moments.

**Rule: the FO NEVER silently fails.** Every disruption produces a report. Route status
is visible on the Empire Dashboard: "Routes: 12 active, 2 flagged, 1 paused."
This is the anti-X4 rule: *"Ships bounce in and out of stations for extended periods"*
with no explanation. Our FO always explains.

#### FO Learning from Player Behavior

> **Research basis:** Star Traders: Frontiers — "destroying ships makes crew bloodthirsty;
> exploring makes them intrepid." The centaur model requires bidirectional adaptation.

The FO should learn from the player's manual trading patterns and adjust its
recommendations accordingly. This is NOT a hidden stat — it's visible and adjustable:

| Player Pattern | FO Adaptation | FO Dialogue |
|---------------|--------------|-------------|
| Player consistently takes high-risk/high-reward routes | FO suggests riskier routes with higher margins | "You seem comfortable with contested space. I've flagged a Chitin-border route — 35% margin, but there's patrol risk." |
| Player avoids a specific faction | FO deprioritizes that faction's space | "I've noticed you steer clear of Communion territory. I'll route around it unless you say otherwise." |
| Player manually trades a good the FO isn't covering | FO offers to automate that good | "You've been running Rare Metals manually for 5 trips. Want me to set up a charter for that?" |
| Player overrides FO route decisions frequently | FO becomes more conservative, asks before acting | "I've been getting it wrong lately. I'll check with you before committing to new routes for a while." |

**The learning is transparent.** The Empire Dashboard shows "FO Profile: Risk-tolerant,
Communion-averse, learning from 47 manual trades." The player can reset or adjust these
preferences anytime. This is the anti-Black & White rule: players couldn't tell what the
creature had learned. Our FO shows its work.

#### Bainbridge's Paradox (1983)

> "The more advanced a control system is, the more crucial may be the contribution
> of the human operator." — Lisanne Bainbridge, "Ironies of Automation"

Applied to our game: **the player must be able to do the FO's job manually at any
time, and must occasionally be REQUIRED to.**

| Bainbridge Requirement | Implementation |
|----------------------|----------------|
| Player must maintain the skill to intervene | Manual trading always available and occasionally more profitable than FO routes (edge cases, faction reputation gates) |
| Automation must be transparent | FO decision log: "Route Delta chosen because Deneb discovery showed 22% margin. Intel age: 45 ticks." |
| Failure must require human judgment | When intel expires, FO escalates: "Route Delta's data is unreliable. I need you to visit Deneb or find a new route." |
| Player must understand the system enough to diagnose failures | FO explanations in character, not in system terms. "The Valorin raised tariffs — that's why Route Delta is bleeding credits" |

**Anti-pattern to avoid:** "Black & White" (2001) promised a creature AI that learned
from observation — but players couldn't tell what the creature had learned or why it
was making decisions. Teaching-by-demonstration only works when the student shows its
work. Our FO's decision log is mandatory, not optional.

#### FO Observation Limits

The FO must NOT become a notification stream. Observation beats are powerful because
they're rare and well-timed. Design constraints:

| Constraint | Rule | Rationale |
|-----------|------|-----------|
| **Cooldown** | Minimum 60 ticks between non-critical FO observations | Player needs breathing room. Constant commentary becomes background noise |
| **Length** | FO dialogue ≤ 2 sentences for routine observations, ≤ 4 for significant events | Brevity is respect. The player is busy |
| **Stacking** | Max 2 queued observations. If queue is full, lowest-priority observation is replaced and an overflow indicator appears ("FO has more to say" pulse on Intel tab). Replaced observations are logged to the FO history panel, never permanently lost | Better to defer a comment than lose information. The player can always catch up via the Intel tab |
| **Escalation** | Only `DISCOVERY_OPPORTUNITY`, `TRADE_INTEL_STALE`, and chain/revelation triggers can bypass cooldown | These are the moments that matter |
| **Personality budget** | Each FO archetype has a "chattiness" parameter (Analyst: low, Veteran: medium, Pathfinder: high) | Players who pick Analyst want fewer interruptions |
| **Player-initiated** | The player can ALWAYS ask the FO for analysis via the Intel tab. The cooldown only limits unsolicited observations | Player agency over information flow |

**The silence principle:** The most powerful FO observations are the ones that break
a silence. If the FO has been quiet for 200 ticks and then says "Captain — you should
see this," that has impact. If the FO comments every 30 ticks, nothing has impact.

### Intel Freshness & Decay

Discovery intel is perishable. Axiom #4: knowledge is perishable — explore or decay.

> **Canonical decay schedule:** See §Intel Decay → Program Profitability above for
> the unified distance-based decay table. Intel decays by DISTANCE BAND (hop count
> from player start), not by absolute time. This section describes the mechanic;
> the canonical numbers are in the margin buffer spec.

**Mechanic:** NPC traders also discover routes over time. The player's exclusive
advantage from a discovery decays as NPC trade programs compete on the same route.
The decay rate is SLOWER for deep-frontier discoveries (fewer NPCs) and FASTER for
near-starter discoveries (many NPCs). This creates a natural pressure toward the
frontier — deeper discoveries stay profitable longer. Fracture-space discoveries
NEVER decay (NPCs can't fracture-travel) — permanent exclusive advantage.

**Implementation status:** `ApplyDiscoveryRouteDecay()` in IntelSystem.cs (T41)
implements distance-based decay with bands: Near ≤2 hops (50t), Mid 3-5 (150t),
Deep 6+ (400t), Fracture (never). **Remaining**: margin buffer wiring to
`ProgramSystem.CalculateEffectiveMargin()` so decay mechanically affects profit.

---

## Anomaly Chains (NEW)

> **Axiom #3:** Chains create stories; one-shots create trivia.
> **Reference:** Stellaris (anomaly event chains), Outer Wilds (interconnected sites),
> Mass Effect (Prothean artifact trail).

### Why Chains Work

A one-shot discovery (scan, loot, done) is quickly forgotten. A chain that spans 3-5
sites, escalating in difficulty and narrative weight, becomes a memorable story arc that
the player carries across multiple play sessions. Stellaris's most remembered content
is always anomaly chains, never individual anomalies.

### Chain Architecture

Each chain has:
- **Hook**: The first discovery in the chain. Found through normal exploration.
  Low difficulty. Yields modest loot + a Lead pointing to the next site.
- **Development** (1-2 sites): Intermediate discoveries that require better sensors,
  deeper fracture access, or faction reputation. Each adds narrative context and
  escalates the mystery. Each yields better loot + the next Lead.
- **Climax**: The final discovery. Requires the highest-tier capability the chain
  demands. Yields significant loot, a narrative revelation, and a Knowledge Graph
  connection to the broader mystery.

### Authored Chain Templates

Pre-author 8-12 chain templates. Procedural generation fills location, faction, and
specific goods. Each template has a fixed dramatic arc.

**Chain A: The Valorin Expedition (3 sites)**
1. **Hook** (Derelict, near frontier): Valorin scout wreck. Transponder log references
   a second vessel. Lead -> [frontier system].
   *Intel: Valorin territorial expansion routes — profitable smuggling paths.*
2. **Development** (Signal, deep frontier): Emergency beacon. The second vessel was
   attacked. Weapon signatures match no known faction. Lead -> [Phase 2+ system].
   *Intel: Ancient weapons data -> tech lead for research.*
3. **Climax** (Ruin, Phase 2+ space): Ancient defense installation. Automated weapons
   that attacked the Valorin scouts are thread-builder perimeter defenses. The ruins
   predate all modern factions. Data log placement: LOG.LATTICE series.
   *Intel: The defense installation's power source reveals a stable fracture corridor.*
   *Knowledge graph: connects to Lattice degradation thread.*

**Chain B: The Communion Frequency (4 sites)**
1. **Hook** (Signal, Shimmer-zone): Unusual harmonic in local sensor noise. Communion
   pilots call it "the hum." FO notes it matches no known natural phenomenon.
   Lead -> [Communion border system].
   *Intel: Communion crystal harvesting routes — exotic crystal sources.*
2. **Development** (Ruin, Communion territory): Ancient listening post. The "hum" is
   an accommodation geometry test signal, still broadcasting after millions of years.
   The Communion built their settlements near these signals without knowing why they
   felt drawn there. Lead -> [deep Shimmer-zone].
   *Intel: Accommodation resonance data -> fragment location hint.*
3. **Development** (Signal, deep Shimmer): The frequency resolves into structured data
   when measured with a fracture-exposed scanner. FO: "It's not a signal. It's a
   calibration tone. Something is testing whether local spacetime can sustain
   accommodation geometry." Lead -> [Phase 3 space, requires fracture drive].
   *Intel: Calibration data reduces scanner variance by 2% permanently.*
4. **Climax** (Ruin, Phase 3 space): Accommodation geometry prototype. The test site
   where Vael's team ran their experiments. Still functional. The Haven was built on
   the same principles but at larger scale. Data log placement: LOG.ACCOM series.
   *Intel: Haven approach vector — reveals path to Haven if not yet discovered.*
   *Knowledge graph: connects Communion territory to Accommodation research thread.*

**Chain C: The Pentagon Audit (5 sites)**
> This chain is the primary vehicle for Revelation R3 (pentagon ring discovery).
> The player assembles economic evidence BEFORE reading the data logs that explain it.
> Per LoreContent_TBA.md: "The gameplay is the revelation. The logs are the explanation."

1. **Hook** (Signal, faction border): Trade route anomaly. FO notes that goods flowing
   between two factions follow a suspiciously optimal pattern — as if the supply and
   demand were designed to complement each other.
   *Intel: Optimal cross-faction arbitrage route (+30% margin).*
2. **Development** (Ruin, second faction border): Geological survey data. FO: "The
   mineral deposits in this region are... unusual. The distribution pattern looks
   natural, but the statistical probability of five complementary deficiency rings
   forming by chance is approximately zero."
   *Intel: All 5 faction resource dependencies mapped — full pentagon visible.*
3. **Development** (Derelict, Concord space): Classified Concord intelligence vessel.
   Contains sealed data about thread network resource distribution. Concord marked it
   "THREAD OPTIMIZATION — CLASSIFIED." FO: "They knew. Or at least, someone did."
   *Intel: Concord classified trade routes — bypass tariffs via sealed routes.*
4. **Development** (Ruin, fracture space): Ancient resource processing facility. The
   mineral suppression equipment Senn described. Still operational at residual levels.
   FO: "This machine selectively suppresses geological processes. It's been running
   for millions of years." Data log placement: LOG.ECON.001-003.
   *Intel: Locations where suppression has weakened -> untapped resource deposits.*
5. **Climax** (Ruin, deep fracture): Oruth's administrative archive. The approval
   documents for the pentagon ring. Data log placement: LOG.ECON.004-006.
   *Revelation R3 fires if not already triggered by gameplay.*
   *Knowledge graph: connects all 5 faction resource nodes to ancient engineering.*

**Additional chains (to be designed):**
- Chain D: The Lattice Threshold (3 sites) — degradation engineering, Tal's grief
- Chain E: The Rebel Ship (4 sites) — tracing the accommodation vessel's flight path
- Chain F: The Kesh Confession (3 sites) — Kesh's private logs, moral cowardice arc
- Chain G: The Living Geometry (4 sites) — fragment resonance leading to R5
- Chain H: The Departure Record (3 sites) — why the thread builders left

### NEW: Anomaly Chain → FO Intel Pipeline

Anomaly chains are the tick 600-1200 content engine. The `AnomalyChainSystem` and
`AnomalyChainContentV0` exist (T45), but chains need to produce **FO-relevant
intelligence at intermediate steps**, not just final loot. Without this, chains are
multi-site treasure hunts disconnected from the automation core loop.

**Design principle:** Chains should reward the player with ESCALATING economic value at
each step. This keeps the player motivated to continue the chain AND gives the FO material
to work with during the mid-game. The player follows the mystery; the FO builds
infrastructure behind them. That's the centaur model in action.

Each chain step produces a `ChainIntel` variant of `EconomicIntel` with escalating scope:

| Chain Step | Intel Type | FO Response | Example |
|-----------|-----------|-------------|---------|
| **Step 1 (Surface)** | Location data — reveals a node/region of interest | FO acknowledges, no action yet | "That signal you traced points to something in the Drift sector. No economic data yet, but it's worth investigating." |
| **Step 2 (Investigation)** | Partial economic picture — production records, resource hints | FO begins planning | "The second site had production records. Whatever was here manufactured [good] at industrial scale. If any of that infrastructure survived..." |
| **Step 3 (Deep)** | Actionable trade intelligence — full supply chain data | FO proposes routes | "I can now map the supply chain this installation fed. Three nodes, two goods, margins I haven't seen in thread space. Want me to start building routes?" |
| **Final Step (Resolution)** | Artifact + comprehensive regional intel | FO installs artifact upgrade | "The [Ancient Navigation Beacon] — it's not just a trophy. It contains pathfinding data that improves every route in this region. Route Delta efficiency: +40%." |

**Chain intel also feeds the pentagon ring revelation.** The FO's discovery emphasis
system (see `fo_trade_manager_v0.md` §Discovery Emphasis by FO Personality) should
produce different FO commentary per chain step:

- **Maren** notices price pattern anomalies across chain data → economic evidence
- **Dask** notices tactical signatures and military logistics → strategic evidence
- **Lira** notices navigational oddities and geometry distortions → spatial evidence

Same chain, different FO commentary. This is the discovery emphasis system at work
during chains, not just at static sites. It creates replayability: different FO =
different path through the same chain's narrative.

### Chain Gating

| Chain Tier | Minimum Requirement | Discovery Density |
|------------|-------------------|-------------------|
| Tier 1 (hooks) | Basic scanner, any space | Common — 1 hook per 3-4 systems |
| Tier 2 (development) | Mk1+ scanner, faction reputation | Moderate — requires travel to specific regions |
| Tier 3 (climax) | Mk2+ scanner, fracture drive, or high faction rep | Rare — deep frontier or instability zones |

### Anomaly Difficulty Gating

Directly from Stellaris: anomaly difficulty should be gated by scanner/sensor tier.

| Discovery Difficulty | Required Scanner | Scan Duration | Risk |
|---------------------|-----------------|---------------|------|
| 1-3 (common) | Basic | 1 tick | None |
| 4-6 (uncommon) | Mk1 | 3 ticks | Minor hazard (hull stress) |
| 7-9 (rare) | Mk2 | 5 ticks | Moderate hazard (hull + crew) |
| 10 (legendary) | Mk3 + fracture module | 10 ticks | Severe hazard (instability exposure) |

---

## Automation Graduation (NEW)

> **Axiom #6:** Milestones are moments — celebrate then automate.
> **Reference:** Elite Dangerous (what NOT to do), Factorio (automation as progression).

### The Anti-Fatigue Mechanism

The player should never scan the same type of discovery the same way 50 times. The
progression:

1. **First encounter with a family**: Full manual process. FO narrates. Milestone card.
   Audio sting. The player LEARNS what this family means.
2. **2nd-3rd encounter**: Still manual, but faster. FO commentary is shorter. The player
   is developing intuition about the family's risk/reward profile.
3. **4th encounter**: FO suggests automation. "I notice we've scanned several derelicts.
   I could configure a survey program to handle the initial scan automatically."
4. **5th+ encounter**: Player can deploy a **SurveyProgram** (new automation program type)
   that auto-scans Seen discoveries of that family within the program's operating range.
   Results arrive via toast notification. Novel discoveries (chain links, unique finds)
   still require manual analysis.

### SurveyProgram Design

```
Program: SurveyProgram
  Type: Derelict | Ruin | Signal | Any
  Home: [station]
  Range: [hops from home]
  Action: Auto-advance Seen -> Scanned for matching discoveries
  Output: Intel report per scan (appended to IntelBook)
  Limitation: Cannot advance Scanned -> Analyzed (requires player presence)
  Limitation: Cannot process chain links (flagged for manual attention)
```

**Why Analyzed stays manual:** The analysis phase is where narrative happens. Where
connections are revealed. Where the "aha" moment lives. Automating Phase 2 eliminates
tedium. Automating Phase 3 would eliminate meaning.

**FO trigger:** The FO's suggestion is gated by `DiscoveriesScannedOfFamily >= 3`.
This ensures the player has experienced the family manually before automation is offered.
The suggestion is a natural dialogue beat, not a tutorial popup.

---

## Discovery Failure States (NEW v2)

> **Axiom #5:** Mystery degrades gracefully. **Axiom #4:** Knowledge is perishable.
> A system with no failure states has no tension. Every exploration game that lets
> discovery always succeed eventually makes discovery feel trivial.

### Why Failure Matters

Currently, every scan succeeds. Every analysis yields loot. Every chain step completes.
This means the player never fears a discovery going wrong — which means they never feel
genuine tension during the scan process. Failure states create the emotional range that
makes success meaningful.

### Failure Types

| Failure Type | Trigger | Player Experience | Recovery |
|-------------|---------|-------------------|----------|
| **Scan Interference** | Instability zone + low scanner tier | Scan returns partial/corrupted data. FO: "Too much interference. We got fragments — not enough for a full read." | Upgrade scanner, reduce local instability, or return when metric stabilizes |
| **Hazard Abort** | Discovery site has hull-damage hazard exceeding ship's tolerance | Scan aborts partway. FO: "Hull stress too high — I'm pulling us back. We need better shielding." | Install defensive module, repair hull, return with stronger ship |
| **Intel Spoilage** | Discovery analyzed but player waits too long to act on intel | Intel degrades to Expired before FO can deploy automation. Opportunity lost. | Find a new discovery, or revisit to re-scan (partial data recovery) |
| **Chain Dead End** | Chain step requires capability the player doesn't have yet | Chain pauses. FO: "The trail continues into fracture space. We can't follow — yet." Lead remains in Knowledge Graph as a "locked" node | Acquire the required capability (scanner tier, fracture drive, faction rep) |
| **Contested Discovery** | NPC faction fleet arrives during scan/analysis | Scan interrupted by combat. Partial data retained. Discovery site may be claimed by the NPC faction | Return after resolving the faction conflict, or negotiate access via diplomacy |
| **False Positive** | Instability zone + phantom marker mechanic | Player travels to a discovery marker that doesn't exist. FO: "Scanner ghost. The instability is playing tricks on our instruments." | No discovery, but the journey itself may have revealed other intel (visited nodes refresh IntelBook) |

### Failure as Narrative

The critical design insight: failure states should produce FO dialogue that **reveals
character**. A scan failure isn't just "try again" — it's a moment where the FO's
personality shows:

```
[Scan Interference at Node 19]
Maren: "Partial data. 62% confidence. I can extrapolate, but I wouldn't
       recommend trading on incomplete models."
Dask:  "Instruments are screaming. We got something, but not enough.
       I don't like operating blind, Captain."
Lira:  "The noise is beautiful, actually. Like the void is singing.
       But I can't read through it. Not with this scanner."
```

Failure states also serve as natural gating for capability progression. The player
encounters a scan failure → understands they need a better scanner → researches the
upgrade → returns and succeeds. This is Axiom #4 (pain before relief) applied to the
scanner itself.

### Partial Success

Not all failures are binary. Introduce a "partial success" state:

| Outcome | Data Quality | FO Response |
|---------|-------------|-------------|
| **Full success** (> 80% signal) | Complete data, exact values | Normal analysis flow |
| **Partial success** (40-80%) | Approximate values, some fields marked "est." | FO flags uncertainty: "Margin estimate: 15-25%. I'd want better data before committing a charter." |
| **Failure** (< 40%) | Discovery reverts to Seen. Must re-scan | FO explains why and what would help |

Partial success is more interesting than binary pass/fail because it forces the player
to make a judgment call: act on uncertain data, or invest time re-scanning?

---

## Artifact Research → Ancient Tech Multipliers (NEW)

> **Cross-ref:** `fo_trade_manager_v0.md` §Ancient Tech as Automation Multipliers
> **Epic:** EPIC.S6.ARTIFACT_RESEARCH (prerequisite for FO Trade Manager Phase 3)

### Why This Section Exists

The FO Trade Manager spec describes a table of ancient artifacts that slot into the FO's
operations as visible, named upgrades (e.g., Ancient Navigation Beacon → +40% route
efficiency). But the pipeline from "player finds artifact at discovery site" to "FO
installs upgrade" has no mechanical spec. EPIC.S6.ARTIFACT_RESEARCH is TODO. This
section specifies the flow so implementers don't need to cross-reference two docs.

### Artifact → FO Installation Pipeline

```
1. Player finds artifact at discovery site         (existing — DiscoveryOutcomeSystem)
2. Artifact enters research queue at Science Center (EPIC.S6.SCIENCE_CENTER)
3. Research completes after N ticks                 (throughput-gated by Science Center tier)
4. Research produces an AncientTechUpgrade          (new entity, typed effect)
5. FO installs upgrade into TradeManager            (TradeManager.AncientTechSlots)
6. FO acknowledges installation with character beat (ANCIENT_TECH_INSTALLED trigger)
```

### AncientTechUpgrade Effects — Qualitative Shifts (v2 redesign)

> **Design philosophy change (v2):** Ancient tech must NOT be percentage bonuses.
> "+40% route efficiency" is a spreadsheet improvement — the player doesn't FEEL it.
> Ancient tech must enable **qualitatively new capabilities** that change HOW the player
> plays, not just how WELL they play. Reference: Satisfactory's alternate recipes
> (same product, completely different production chain), Factorio's nuclear power (not
> "better coal" — an entirely new logistics challenge), Subnautica's Cyclops (not "faster
> Seamoth" — a mobile base that changes expedition structure).
>
> **The rule:** Every Ancient Tech upgrade must make the player say "I couldn't do that
> before" — not "I can do that 40% better."

| Artifact | Effect Type | Qualitative Capability | FO Dialogue |
|----------|------------|----------------------|-------------|
| **Ancient Navigation Beacon** | `PhantomRoute` | Reveals a **hidden lane** between two systems with no visible lane gate. Only the beacon holder can use it. Creates exclusive shortcuts bypassing faction chokepoints | "That beacon — it's not a map. It's a KEY. There's a passage between Kepler and Deneb that doesn't appear on any chart. Our ships can use it. Nobody else can." |
| **Accommodation Data Cache** | `DemandForesight` | FO sees **demand shifts 100 ticks before they happen** — not prediction, but actual future state read from residual accommodation geometry. Enables pre-positioning goods BEFORE demand arrives | "The cache doesn't predict the future. It reads it. Accommodation geometry doesn't distinguish between 'now' and 'soon.' The Valorin will need electronics in 80 ticks. We can be there first." |
| **Precursor Trade Ledger** | `ShadowMarket` | Unlocks a **parallel trade channel** between any two systems where the player has installed Precursor infrastructure. Bypasses tariffs, NPC competition, and faction restrictions. Limited to 2 active channels | "The ancients didn't trade through stations. They traded through the geometry itself. I can set up two channels — no tariffs, no competition. No one can see these routes except us." |
| **Metric Calibration Module** | `PerfectIntel` | Intel at calibrated systems **never decays**. The module stabilizes local metric enough to maintain permanent, exact economic data. Limited to 3 systems | "Intel doesn't decay because information doesn't decay — measurement does. This module fixes measurement. Three systems, permanent data. Choose wisely." |
| **Resonance Amplifier** (NEW) | `ChainSense` | Detect anomaly chain hooks from **2x scanner range**. Chain steps glow on Knowledge Graph. Dramatically accelerates chain discovery in late-game | "The amplifier resonates with intent. Ancient intent. The chain markers are singing to it. I can feel the next link from here." |
| **Threshold Key** (NEW) | `FractureShortcut` | Opens a stable fracture corridor between two visited fracture-space nodes. Permanent, bidirectional, no instability cost. The ancients' highway system | "A stable corridor. Not a fracture jump — a road. The thread builders built these. We're using their infrastructure. That changes everything about deep-space operations." |

### Why Qualitative > Quantitative

| Percentage Bonus (old) | Qualitative Shift (v2) | Why the Shift Wins |
|------------------------|------------------------|-------------------|
| "+40% route efficiency" | Hidden lane only you can use | Player remembers the ROUTE, not the number |
| "Predict demand 50 ticks early" | See actual future demand state | Player makes different DECISIONS, not better versions of the same decisions |
| "Hidden supply/demand revealed" | Parallel trade channel bypassing all friction | Player builds a DIFFERENT trade network, not an optimized version |
| "+1 hop intel range" | 3 systems with permanent intel | Strategic CHOICE (which 3?) — not a passive bonus |

**The Satisfactory alt-recipe principle:** Each Ancient Tech artifact should make the
player redesign part of their trade network. Not "Route Delta is 40% better" but "I can
now build routes that were IMPOSSIBLE before." The discovery creates a new possibility
space, not a better value in the existing one.

### Key Design Constraints

**Constraint 1: The player sees the causal chain.** "I explored that dangerous site →
found this artifact → researched it → FO installed it → now I can do something new."
Every link visible, named, specific. No hidden capability unlocks.

**Constraint 2: Ancient Tech creates dilemmas.** PerfectIntel covers 3 systems — which
3? ShadowMarket allows 2 channels — which routes? These are strategic decisions that
matter because the capability is powerful but limited. Percentage bonuses never create
dilemmas because "apply everywhere" is always the right answer.

**Constraint 3: Ancient Tech changes the FO's behavior.** When DemandForesight is
installed, the FO's decision log shows "Pre-positioning Electronics at Altair. Demand
spike in 80 ticks." The player SEES the artifact working through FO actions, not stats.

The `AncientTechSlots` dictionary in the FO's `TradeManager` entity (see
`fo_trade_manager_v0.md` §Entity Model) stores the artifact ID, source discovery ID,
effect type, configuration (which systems, which routes), and installed tick. Empire
Dashboard shows installed artifacts with source and current effect: "Phantom Route:
Kepler ↔ Deneb (Beacon from Kepler Drift Site, installed T450)."

### Pre-Science Center Workaround

Until EPIC.S6.SCIENCE_CENTER is implemented, artifacts can install directly into the
FO's `AncientTechSlots` at analysis time (skip steps 2-3). This preserves the
exploration→automation feedback loop for Phase 2-3 of FO Trade Manager without blocking
on the full Science Center implementation. The research queue adds depth later but isn't
load-bearing for the core loop.

---

## Information Asymmetry (NEW)

> **Axiom #4:** Knowledge is perishable — explore or decay.
> **Reference:** EVE Online (probe scanning as competitive edge), X4 (scan for trade data).

### Tiered Economic Intelligence

| Scan Depth | What's Revealed | How It Helps |
|------------|----------------|-------------|
| **Seen** (auto) | System exists, discovery type visible | "Something is there" — curiosity only |
| **Scanned** (manual) | Surface intel: estimated values, nearest buyer, rough margins | Enough to decide whether to send a program |
| **Analyzed** (manual) | Full intel: exact prices, production deficits, supply curves, hidden routes | Enough to deploy optimized automation |
| **Deep Scan** (Mk2+ sensor) | Extended intel: price prediction curves, faction supply chain dependencies, seasonal patterns | Enough to build multi-hop trade empires |

### Economic Fog of War

Without scanning, the player is trading blind at nodes they haven't visited recently.
This is the X4 model: exploration IS economic reconnaissance. The galaxy map should
show economic health indicators ONLY for systems with fresh intel.

```
+-- GALAXY MAP (economic overlay) -------------------------+
|                                                           |
|  Kepler [fresh: 12t ago]    Altair [stale: 180t ago]     |
|  Electronics: 48cr (exact)   Electronics: ~40-55cr (est)  |
|  Margin: +12cr/unit          Margin: ??? (needs rescan)   |
|                                                           |
|  Deneb [UNKNOWN]            Vega [expired: 350t ago]      |
|  "No trade data"            "Data unreliable"             |
|                                                           |
+-----------------------------------------------------------+
```

### Intel Competition

NPC traders also gather intel. The player's exclusive advantage from a discovery
erodes as NPC fleets discover the same routes:

| Discovery Location | NPC Discovery Delay | Player Advantage Window |
|-------------------|--------------------|-----------------------|
| Near starter space | 50-100 ticks | Short — exploit fast |
| Mid frontier | 150-250 ticks | Moderate — deploy automation within 100 ticks |
| Deep frontier | 400-600 ticks | Long — exclusive access for extended period |
| Fracture space | Never (NPCs can't fracture-travel) | Permanent — only the player trades here |

**Design implication:** Fracture-space discoveries are the most valuable because NPCs
can never compete on those routes. This creates a natural economic motivation for
fracture travel beyond the narrative motivation.

---

## Late-Game Discovery Continuation (NEW)

> **Axiom #4:** Knowledge is perishable. Discovery must not dry up.
> **Reference:** Stellaris (what NOT to do — anomalies stop spawning in mature empires).

### Five Sources of Late-Game Discovery

| Source | Trigger | Content |
|--------|---------|---------|
| **Fracture layers** | Player upgrades fracture module | Deeper instability zones reveal sites invisible to basic sensors. Phase 3+ space has discoveries no one else can reach |
| **Faction intelligence** | Reputation milestones | High-rep faction contacts share classified intel: sealed trade routes, hidden caches, military supply chains |
| **Economy-triggered anomalies** | Market saturation, price collapse | When a region's economy stagnates, new anomalies spawn: abandoned stations, refugee signals, smuggler derelicts. Economic failure creates exploration opportunities |
| **Chain unlocks** | Completing earlier chains | Tier 3 chains only become visible after completing related Tier 1-2 chains. The pentagon audit chain is invisible until the player has discovered cross-faction trade anomalies |
| **Instability progression** | Thread degradation advances | As instability phases escalate, previously stable systems develop anomalies. A ruin that was always there but hidden by stable-metric suppression becomes detectable when the local thread degrades to Phase 2+. The Lattice failing literally reveals the past |

### The Instability-Reveals-History Mechanic

This is thematically perfect for our setting. The thread builders' containment
infrastructure literally suppresses information (metric consistency = hiding the
turbulence). As containment fails, information that was suppressed for millions of
years becomes detectable. Ancient sites that were invisible in stable space become
scannable in degraded space.

**Implementation:** Discovery sites seeded at world gen can have an `instability_gate`
field. The site exists in the data model but is invisible to the player until the local
instability phase reaches the gate threshold. From the player's perspective, the ruin
"appeared" when the thread degraded — but it was always there, hidden by the same
infrastructure that kept space stable.

**Narrative resonance:** The player discovers that the containment system doesn't just
stabilize spacetime — it suppresses history. The thread builders' infrastructure hides
the evidence of its own construction. As it fails, it reveals itself. This is the
thematic core of the game: the infrastructure that enables civilization also constrains
it, and its failure reveals truths that stability concealed.

### The Sustain → Exploration Closed Loop (NEW v2)

> **Cross-ref:** `dynamic_tension_v0.md` Pillar 2 (Maintenance Treadmill), Pillar 5
> (Revelation Arc). **Gap:** The dynamic tension doc defines sustain pressure but never
> shows how it connects to exploration. This section closes the loop.

The SustainSystem creates economic pressure: hull degrades, modules wear, fuel costs
accumulate. This pressure is currently invisible to the exploration system — sustain
costs are just overhead. But sustain pressure should DRIVE exploration:

```
┌─────────────────────────────────────────────────────────┐
│                THE SUSTAIN-EXPLORATION LOOP              │
│                                                         │
│  Sustain costs rise ──→ Margins compress ──→ FO flags   │
│       ↑                                     declining   │
│       │                                     profitability│
│       │                                         │       │
│       │                                         ▼       │
│  Discovery yields  ←── Player explores ←── FO suggests  │
│  repair materials,      frontier for        "We need    │
│  new supply routes,     better sources      new sources │
│  ancient tech that                          to maintain  │
│  reduces sustain costs                      operations"  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

| Sustain Pressure | Exploration Response | Discovery Payoff |
|-----------------|---------------------|-----------------|
| Hull repair costs rising | Derelict discoveries yield salvage + repair materials | Direct cost reduction |
| Fuel becoming scarce | Signal discoveries reveal fuel depot locations or efficient routes | Route optimization |
| Module degradation accelerating | Ruin discoveries yield ancient alloys that slow degradation | Maintenance treadmill relief |
| Operating at loss due to stale intel | Any discovery refreshes local intel → tightens margins | Immediate profit recovery |
| Sustain costs exceed revenue | Market Ruin discovery reveals WHY a similar operation failed → FO restructures routes to avoid same fate | Strategic adaptation |

**The key insight:** Sustain pressure without exploration relief feels like punishment.
Exploration without sustain pressure feels like tourism. Together, they create the
Factorio rhythm: you're always solving the current problem while creating the next one.

**FO integration:** The FO should frame exploration suggestions in sustain terms:
```
Maren: "Hull repair costs have increased 30% over the last 200 ticks.
       Our current supply routes don't include salvage sources. I've
       flagged two unexplored systems in scanner range that show
       derelict signatures."
```

This makes the FO's exploration suggestions feel NECESSARY, not optional. The player
explores because their empire needs it, not because the game is nagging them.

### What the Player Does During Automation (NEW v2)

> **The #1 design risk in a centaur model:** The player sets up automation, then
> has nothing to do. The FO is running routes, the programs are ticking — what is
> the PLAYER doing? If the answer is "watching numbers go up," we've built a
> screensaver, not a game.

**The Spectator Trough Problem:** Factorio solves this by making automation create
NEW bottlenecks (throughput, logistics, power). Our equivalent: automation reveals
new INFORMATION that only the player can act on.

#### The Exploration–Automation Interleave

At any given moment, the player should be doing exactly ONE of these:

| Player Mode | What They're Doing | What Automation Is Doing | Transition Trigger |
|-------------|-------------------|------------------------|-------------------|
| **Scouting** | Flying to unexplored systems, scanning | FO runs existing routes, reports margin changes | Scanner finds something interesting → Analyzing |
| **Analyzing** | Choosing scan phases, reading discovery results | FO flags intel from other routes ("while you were scanning...") | Discovery yields EconomicIntel → Deciding |
| **Deciding** | Reviewing FO proposals, approving/rejecting routes | FO queues proposals, waits for player input | Player approves → Optimizing, or player rejects → Scouting |
| **Optimizing** | Reviewing Knowledge Graph, comparing routes, adjusting programs | FO executes approved routes, accumulates performance data | Margins compress / sustain pressure rises → Scouting |

**The key insight:** The player is never idle because each mode creates the
conditions for the next. Scouting produces discoveries. Discoveries produce
proposals. Proposals produce routes. Routes produce intel decay and sustain
pressure. Pressure produces the need to scout again.

#### Active Player Roles at Each Competence Tier

The 3-tier competence model (§Centaur Model) must ensure the player has meaningful
work at every automation level:

| Competence Tier | FO Does | Player Must Do | Why Player Can't Delegate This |
|----------------|---------|---------------|-------------------------------|
| **Novice** (Tier 1) | Runs 1-2 player-demonstrated routes, basic sustain | Everything else: fly, scan, buy, sell, explore, handle threats | Learning the mechanics + building mental model of what works. FO is observing and learning alongside the player |
| **Competent** (Tier 2) | Runs 3-5 routes, manages all sustain, suggests extensions | Monitor performance, handle exceptions (faction conflict, piracy, price crash), explore frontiers, make strategic decisions | Exceptions require judgment. Strategic decisions (which faction, which chains) shape the empire's identity. NPC competition and world events keep routes dynamic |
| **Master** (Tier 3) | Full network, fleet optimization, proactive rebalancing, warfront briefings | Explore deep frontier, pursue revelation evidence, manage diplomacy, direct research priorities, handle major disruptions | Only the player can decide what the empire BECOMES. The galaxy is unpredictable enough that even a Master FO needs human judgment for crises |

**Anti-pattern: The Stellaris Sector Problem.** At high trust levels, if the
player's only job is "check the numbers occasionally," they'll alt-tab. The fix:
high-trust automation GENERATES more exploration opportunities (FO detects
anomalies in trade data, flags suspicious patterns, discovers routes that pass
near unexplored systems). The more you automate, the more the game gives you to
explore.

#### Boredom Circuit Breakers

If the player stays in Optimizing mode too long without scouting:

| Trigger | What Happens | Design Intent |
|---------|-------------|---------------|
| No new discovery for 300 ticks | FO: "Our intel is getting stale. Scanner shows signatures in [region]." + map ping | Gentle nudge toward exploration |
| Margin compression on 3+ routes | FO: "We're competing with ourselves. Need new markets." | Economic pressure to explore |
| Sustain costs exceed 40% of revenue | FO: "Maintenance is eating our profits. Derelicts in [region] might have salvage." | Sustain→Exploration loop kicks in |
| Chain intel suggests connected site | FO: "That anomaly chain continues at [location]. The next link might explain the pattern." | Curiosity pull |
| 500 ticks since last revelation progress | Story system plants a new lead visible on galaxy map | Narrative pull toward revelation content |

**None of these are mandatory quests.** They're informational nudges that make
exploration feel like the obvious next move. The player always chooses; the game
ensures the choice is interesting.

### Re-Exploration: The Late-Game Verb (NEW v2)

> **Axiom #1:** Discovery is knowledge. Knowledge changes with context.
> **Gap:** Currently, once a site is Analyzed, it's "done." But revelations (R1/R3/R5)
> change the meaning of everything the player has already seen.

**The problem:** After R1 fires and cover names flip, the player's understanding of
every previously-analyzed discovery fundamentally changes. That derelict at Kepler
isn't just a "Valorin scout" anymore — it's evidence of thread-builder perimeter
enforcement. But the player has no reason to GO BACK. The discovery is green, analyzed,
done. The game treats it as complete when the player's new knowledge makes it incomplete.

**The solution: Re-analysis.**

| Phase | Trigger | What Changes |
|-------|---------|-------------|
| **Re-Scan** | Player returns to an Analyzed site after a new revelation fires | New `Connection text` layer appears based on revelation context. FO commentary reflects new understanding |
| **Deep Analysis** | Player with Mk2+ scanner revisits an Analyzed site | Additional data extracted: hidden signatures invisible to original scan tier. New intel enters IntelBook |
| **Recontextualization** | Automatic (no player action) | After R1/R3/R5, all previously-analyzed sites gain a "New Context Available" marker on the galaxy map. Cover-story names flip to revealed names |

**Visual treatment:** Analyzed (green) markers gain a subtle gold shimmer when the
player has gained context that would change the discovery's meaning. This is NOT a
quest marker — it's an invitation: "You know more now. Want to look again?"

**FO dialogue on re-visit:**
```
[Player returns to Kepler Derelict after R1]
Maren: "Captain, with what we know now... this wasn't a scout on a
       covert mission. It was a perimeter patrol. The weapons that
       destroyed it were automated. The thread builders were defending
       something."
```

**Why this is load-bearing for late-game:** Without re-exploration, the late game has
no reason to revisit safe space. The player pushes deeper and never looks back. Re-
exploration means the ENTIRE map refreshes after each revelation — early-game systems
become late-game content. This is Outer Wilds' core design: new knowledge makes old
locations new again.

### NEW: The Pipeline Inversion (Late-Game Centaur Deepening)

> **Cross-ref:** `fo_trade_manager_v0.md` §Natural Route Depreciation, §Trade Data as
> Lore Evidence

Early game: **exploration feeds the FO.** The player discovers nodes, the FO builds
routes. The FO is downstream of the player's exploration.

Late game: **the FO's economic analysis feeds exploration targets.** This inverts the
pipeline — the FO becomes a source of exploration leads, not just a consumer.

| Source | Mechanism | FO Dialogue |
|--------|-----------|-------------|
| **Route depreciation** | Natural margin compression (NPC competition, §Natural Route Depreciation) creates steady demand for new routes. FO surfaces this as exploration motivation | "Route Alpha's margins have thinned — the locals figured it out. But that new system you visited has something interesting." |
| **LORE_ANOMALY detection** | As FO detects economic anomalies (§Trade Data as Lore Evidence), the Knowledge Graph produces `Lead` entries pointing to specific locations | "The price patterns I've been tracking point to something at Node 19. It doesn't match any model I know. Worth investigating." |
| **Faction intelligence shifts** | Warfront outcomes change faction territorial control, exposing previously protected space | "The Chitin withdrawal exposed something at Node 19. My supply chain models suggest there's infrastructure the Chitin were guarding. Worth a visit." |
| **Pentagon ring evidence** | FO economic analysis reveals anomalies that only make sense if the resource distribution is artificial — these become Knowledge Graph leads | "I've been comparing trade data across all five factions. The dependency pattern is too clean. I've flagged three locations where the pattern breaks down — those breaks might tell us why." |

**The centaur model deepens, it doesn't flatten.** The player and FO become
increasingly interdependent: the player provides access (fracture drive, faction
contacts, personal judgment), the FO provides analysis (pattern detection, route
optimization, economic evidence). Neither can reach the endgame alone.

---

## Trade History as Revelation Evidence (NEW v2)

> **Axiom #2:** The centaur model. **Axiom #1:** Discovery is knowledge.
> **Genre-defining opportunity:** No trading game has ever used the player's own
> completed trade data as evidence for a narrative revelation. This is unique.

### Why This Section Exists

The pentagon ring revelation (R3) depends on the player assembling evidence that the
five-faction resource distribution is engineered. Currently, that evidence comes from
anomaly chains and data logs. But the player has been GENERATING evidence since tick 1:
every trade route they've operated, every margin they've earned, every price pattern
they've observed is data about the economic system. The FO has been watching this data
accumulate. The player's own trade history should be the FIRST evidence, not the last.

### The Player's Ledger as Discovery

The FO's `TradeManager` already tracks completed transactions: source, destination,
good, margin, volume, tick. This ledger IS economic evidence — if you trade enough
routes across enough factions, the pattern becomes visible.

**FO observation triggers (progressive):**

| Trigger | Condition | FO Dialogue | Evidence Type |
|---------|-----------|-------------|---------------|
| `TRADE_PATTERN_NOTICED` | Player has traded 3+ unique goods across 2+ factions | "I've been tracking our routes. There's a pattern I can't explain yet — the price differentials between [Faction A] and [Faction B] are unusually stable." | Preliminary |
| `TRADE_PATTERN_SUSPICIOUS` | Player has traded across 3+ factions with consistent margins | "Captain, the margins are too clean. [Faction A] needs exactly what [Faction B] produces, and vice versa. That's not market dynamics — that's architecture." | Structural |
| `TRADE_PATTERN_PROOF` | Player has operated routes touching all 5 factions | "I've mapped every route we've ever run. Five factions. Five resource deficiencies. Five perfect complements. The probability of this occurring naturally is... I can't calculate it. It's zero." | Definitive |

**Knowledge Graph integration:** Each trigger adds a `TradeEvidenceConnection` to the
knowledge graph linking the involved faction-pairs. By the time `TRADE_PATTERN_PROOF`
fires, the player can open the Knowledge Web and see the pentagon ring drawn from their
own trade data — not from a data log, not from an NPC, but from routes they personally
operated.

### Why This Is Genre-Defining

In most games, narrative evidence is found in the world. In our game, the player
GENERATES narrative evidence by playing the core loop. Trading IS investigation. The
player who trades widely reaches the revelation faster — not because they explored more
anomaly chains, but because their own ledger contains the proof. This means:

1. **The core loop IS the revelation vehicle.** Trading broadly across factions isn't
   just economically optimal — it's narratively revelatory.
2. **The FO's analysis is genuinely useful.** The FO isn't summarizing what the player
   already knows — it's synthesizing patterns across hundreds of transactions that no
   human would notice.
3. **Different players reach R3 via different evidence.** An explorer finds it through
   anomaly chains. A trader finds it through their ledger. A completionist finds it
   through data logs. All paths converge on the same revelation.

### Pre-Revelation FO Decision Log Entries

The FO's trade decision log (see §Centaur Model) should include breadcrumbs that only
make sense after R3 fires:

```
[Tick 340] Route established: Kepler (Valorin) → Altair (Communion)
           FO note: "Valorin electronics deficit perfectly complemented by Communion
           surplus. Unusually clean fit."

[Tick 580] Route established: Altair (Communion) → Wolf (Chitin)
           FO note: "Third faction pair with near-perfect complementarity. I've
           flagged this for deeper analysis."

[Tick 900] TRADE_PATTERN_PROOF fires
           FO: "I've been building a model from our ledger data. Five factions, five
           perfect dependencies, forming a closed ring. This isn't economics. This
           is engineering."
           → Knowledge Graph: Pentagon Ring evidence appears, drawn from player routes
```

---

## The Knowledge Graph

### What It Is

A relationship map showing how discoveries connect to each other and to game systems.
Inspired by Outer Wilds' Ship Log, where each discovery is a node and connections show
how knowledge relates. **Implemented** in `KnowledgeGraphSystem.cs` and
`KnowledgeGraphContentV0.cs` with revelation-triggered connections (R1/R3/R5).

### Structure

```
+-- INTEL -- KNOWLEDGE WEB --------------------------------+
|                                                           |
|       [Valorin Wreck]                                    |
|       Kepler, Analyzed !                                 |
|            |                                              |
|     "same energy signature"                               |
|            |                                              |
|       [Signal Source]              [Ancient Ruin]         |
|       Altair, Scanned ~     ---- Deneb, Analyzed !       |
|            |                        |                     |
|     "beacon frequency"        "Communion weapon marks"    |
|            |                        |                     |
|       [Unknown]                [Communion Territory]      |
|       ???, Unseen               (faction link)            |
|                                                           |
|  Legend: ! Analyzed  ~ Scanned  . Seen  ??? Unknown       |
+-----------------------------------------------------------+
```

### Dual-Mode Display (NEW v2)

> **Reference:** Outer Wilds' Ship Log has two modes — a geographic rumor map
> (where things are in space) and a relational detective board (how things
> connect conceptually). We need both, because geographic proximity and
> conceptual proximity are independent.

The Knowledge Graph has two view modes, toggled by the player:

| Mode | What It Shows | Layout | When To Use |
|------|--------------|--------|-------------|
| **Geographic** | Discoveries overlaid on the galaxy map | Nodes positioned at their actual star system. Connections drawn as arcs across space. Clusters visible at a glance | "Where should I explore next?" — spatial planning, route scouting, frontier identification |
| **Relational** | Discoveries as a detective board | Force-directed graph (like Outer Wilds Ship Log). Related nodes cluster regardless of physical distance. Orphan nodes drift to edges | "How do these connect?" — theory building, chain tracking, revelation hunting |

**Geographic mode** is the default — players naturally think in space. The galaxy
map already exists; the KG geographic mode is an overlay lens on it. Discoveries
appear as icons at their system, connections as curved lines. Scanner range is
visible as a frontier boundary.

**Relational mode** is the investigation tool. When the player switches to it, the
layout animates from geographic positions to force-directed positions (nodes that
share connections pull together). This transition itself is informative — watching
two distant discoveries snap together reveals a connection the player might have
missed on the geographic view.

```
GEOGRAPHIC MODE                    RELATIONAL MODE

  ·Kepler                            [Valorin Wreck]──┐
    [Wreck]                                            │
         ╲                           "same energy"     │
          ╲                                            │
  ·Altair  ╲                         [Signal Source]   │
    [Signal] ·Deneb                       │            │
              [Ruin]                 "beacon freq"     │
                                          │            │
                                     [Unknown ???]  [Ancient Ruin]
                                                    [Communion ──]
```

**Interaction:** Both modes support all Player Verbs (§KG Player Verbs below).
Pin markers persist across mode switches. Speculative Links are visible in both
modes (geographic shows the spatial gap; relational shows the conceptual gap).

### Connection Types

| Connection | Meaning | Visual |
|------------|---------|--------|
| **Same origin** | Two discoveries from the same ancient event | Solid line |
| **Lead** | One discovery points to another location | Dashed arrow |
| **Faction link** | Discovery connects to a known faction | Dotted line to faction node |
| **Tech unlock** | Discovery reveals a researchable technology | Gold line |
| **Lore fragment** | Discovery adds to a narrative thread | Italic label on connection |
| **Chain link** (NEW) | Discovery is part of an anomaly chain | Bold numbered arrow (1->2->3) |
| **Revelation** (NEW) | Knowledge graph connection revealed by story revelation | Glowing line (appears when R1/R3/R5 fires) |
| **Speculative** (NEW v2) | Player-drawn hypothesis link | Dashed gray/amber/gold depending on state (§Link Feedback) |

### Knowledge Graph Player Verbs (NEW v2)

> **Axiom #1:** Discovery is knowledge. The Knowledge Graph is currently read-only —
> the player views it but cannot interact with it. An interactive Knowledge Graph
> transforms passive observation into active investigation.

The Knowledge Graph should support player verbs — actions the player can take ON the
graph, not just through it. Reference: Outer Wilds' Ship Log (mark/unmark as explored),
detective games (pin evidence, draw connections), Obra Dinn (mark identities).

| Verb | Player Action | Effect | Why It Matters |
|------|--------------|--------|----------------|
| **Pin** | Select a discovery node and pin it to the HUD | Pinned discoveries show as waypoint markers on galaxy map. Max 3 pins. | Transforms "I should go there" into a tracked objective without a quest system |
| **Annotate** | Add a short player note to any discovery node | Note visible on hover. Persisted in save. Max 50 characters | "I think this connects to the Communion frequency" — player's own theory |
| **Link** | Draw a speculative connection between two nodes | Dashed line appears. If the game later confirms the connection, it upgrades to solid | Player feels smart when their guess is validated. Outer Wilds' strongest emotional moment |
| **Flag for FO** | Mark a discovery for FO analysis | FO evaluates the flagged discovery and provides trade/strategic assessment. Uses one "FO analysis" charge (replenishes every 100 ticks) | Player-initiated centaur interaction. Respects FO observation limits (§Centaur Model) |
| **Compare** | Select two discoveries and view their shared attributes | Side-by-side panel showing faction, era, damage type, goods, connections | Detective mechanic. "These two derelicts have the same weapon marks — connected?" |

**Design constraint:** Verbs must be lightweight. No crafting, no resource cost, no
failure state. The Knowledge Graph is a thinking tool, not a gameplay system. The
player uses it to organize their understanding — the game rewards them when their
understanding is correct (speculative links confirmed, flagged discoveries yield intel).

**The "Link" verb is the most powerful.** When the player draws a speculative connection
and the game later confirms it (via anomaly chain completion, revelation trigger, or FO
analysis), the moment is uniquely satisfying: "I figured it out before the game told me."
This is the Obra Dinn feeling — assembling truth from fragments.

#### Link Feedback: The Obra Dinn Batch Model

Wrong links should not immediately fail. Reference: Obra Dinn doesn't tell you
which of your three identifications is wrong — it only confirms when a batch of
three is ALL correct. Our equivalent:

| Link State | Visual | Trigger | Player Experience |
|-----------|--------|---------|-------------------|
| **Speculative** | Dashed gray line | Player draws the link | "I think these connect" |
| **Plausible** | Dashed amber line | FO analysis finds shared attributes (faction, era, good type) | "There's something here" |
| **Confirmed** | Solid gold line + chime | Game event validates the connection (chain completion, revelation, trade data proof) | "I was right!" — the Obra Dinn moment |
| **Contradicted** | Dashed line fades to red, then dims | Game event proves the connection impossible (different factions, incompatible eras, contradictory trade data) | "Not that — but now I know more" |

**Critical rule: contradicted links are NEVER immediately punished.** The link
stays visible (dimmed) so the player can see their reasoning history. A
contradicted link might become relevant later — "Wait, I was wrong about WHY
they connected, but they DO connect through a different mechanism." This preserves
the detective feeling. Obra Dinn never deletes your wrong guesses; it just doesn't
confirm them.

**Batch confirmation (3-link rule):** When 3+ speculative links form a connected
subgraph and ALL are independently confirmed, the player receives an "Insight"
bonus: a unique FO commentary, a Knowledge Graph cosmetic (the subgraph glows),
and a small intel reward. This encourages building theories, not just guessing
individual connections.

**FO integration:** When the player draws a link, the FO can optionally comment
(using one observation slot):
```
Maren: "Interesting theory. The spectral data from both sites does
       show similar Resonance_Composite signatures. I'll watch for
       confirming data."
Dask:  "Maybe. I've seen ships with those markings before — both
       sites are near old patrol routes. Could be coincidence."
Lira:  "Oh, I like that connection. If you're right, there might
       be a third site along the same vector."
```

### Knowledge Graph Progressive Disclosure

*Reference: Portal — one mechanic per encounter. The Knowledge Graph has 5
player verbs and 2 view modes. Exposing all of them at first discovery would
violate every progressive disclosure principle in our design bible.*

KG features unlock at **gameplay milestones**, not tick counts. The player
earns each verb by demonstrating they need it:

| Milestone | KG Feature Unlocked | Why This Order |
|-----------|-------------------|----------------|
| **First discovery** | Geographic mode (read-only). Discoveries appear as icons on galaxy map | The player just found something — show them WHERE it is. No verbs needed yet |
| **3 discoveries** | Pin verb (max 3). Pinned discoveries show as waypoints | Player has multiple sites — they need to prioritize which to visit next. Pin solves "where do I go?" |
| **First anomaly chain started** (2+ linked discoveries) | Relational mode toggle. Connection lines visible | The player has connected discoveries for the first time — now the RELATIONSHIP view makes sense. Before this, it would show disconnected dots |
| **5 discoveries + 1 chain** | Annotate verb (50 chars per node) | The player is building theories. They need to externalize their thinking. Appears naturally as the graph gets complex enough to forget details |
| **First FO analysis** (Flag for FO used via dock interaction) | Flag for FO verb (1 charge / 100 ticks) | The FO has been analyzing discoveries in dialogue. The verb formalizes what the player already knows the FO can do |
| **8 discoveries + 2 chains** | Link verb (speculative connections). Obra Dinn batch model active | The player has enough data points to form hypotheses. Link is the most powerful verb — gating it ensures the player understands what connections MEAN before they draw their own |
| **First Insight** (3 confirmed speculative links) | Compare verb (side-by-side attributes). Batch Insight audio plays | The player has proven they can think like a detective. Compare is the advanced analytical tool — it rewards the player who is already engaged with Link |

**Unlock UX:** Each new verb appears with a subtle KG border glow + FO
comment explaining it in character (uses one observation slot):

```
[Pin unlocks]
Maren: "Three sites catalogued. I can mark priority targets on
your nav display — just pin the ones you want to visit."

[Link unlocks]
Lira: "You're seeing patterns I can't prove yet. If you think
two sites are connected, draw the line. I'll watch for evidence."
```

**Design rules:**
- Each unlock is announced ONCE (FO dialogue), then the verb is silently
  available forever. No tutorials, no tooltips, no "Press X to Link"
- Milestones are checked at discovery completion, not continuously — the
  unlock moment coincides with the natural excitement of a new find
- Players who skip the KG entirely (pure traders) lose nothing mechanically —
  all KG features are optional. The FO still provides intel through dialogue
- The progressive unlock order mirrors the player's cognitive journey:
  observe (Geographic) → prioritize (Pin) → connect (Relational) →
  theorize (Annotate) → investigate (Flag/Link) → analyze (Compare)

### Rumor Leads

When a discovery reveals a "lead" (the Signal outcome generates a `discovery_lead_node`),
it creates an entry in the knowledge graph pointing to a new location:

```
! Derelict (Kepler) --> "Signal detected at Altair-7" --> . Unknown (Altair)
```

The lead appears on the galaxy map as a blinking marker at the destination, motivating
the player to travel there. This is the breadcrumb trail that chains exploration into
a narrative rather than random wandering.

---

## Scanner Range & Exploration Frontier

### Scanner as Gameplay Mechanic

The scanner determines how far the player can "see" from their current position.
Upgrading scanners through research expands the knowledge frontier.

| Scanner Level | Range (hops) | Unlocked By |
|---------------|-------------|-------------|
| Basic | 1 hop (adjacent only) | Default |
| Mk1 | 2 hops | Sensors Mk1 research |
| Mk2 | 3 hops | Deep Scan research |
| Mk3 | 5 hops | Advanced Sensors research |

### What Scanning Reveals

| Distance | Information | Update Frequency |
|----------|------------|-----------------|
| Current system | Full real-time data (market prices, fleets, industry, security) | Every tick |
| Within scanner range | System existence, star class, discovery presence, stale market data | On scan (once per visit) |
| Beyond scanner range | Nothing — system shows as "?" on galaxy map | N/A |

### Scanner Sweep Visualization

When the player arrives at a new system, a scanner sweep animation plays:

- Ring of light expands from player's position outward
- As the ring passes through systems within range, they transition from Unknown -> Discovered
- Discovery sites within scanner range get "Seen" status automatically
- Audio: radar ping sound, once per system revealed

This makes the scanner feel like a tool the player is using, not a passive stat.

---

### Scanner Unreliability in Instability Zones

In systems experiencing metric bleed, the scanner itself becomes unreliable —
reinforcing the core theme that measurement breaks down.

| Instability Level | Scanner Effect | Player Experience |
|---|---|---|
| None (stable) | Normal readings, exact values | Baseline — player trusts instruments |
| Low (Drift) | +/-5% variance on market prices, fuel estimates | Subtle — player may not notice immediately |
| Medium (Drift+) | +/-15% variance, discovery phase markers occasionally flicker | Noticeable — player realizes instruments are unreliable |
| High (Fracture) | +/-30% variance, phantom discovery markers appear and vanish, ETAs drift | Alarming — player must rely on experience, not data |
| Critical (Collapse) | Scanner sweeps return contradictory data, prices shown as ranges not values | Visceral — the game's UI is telling you that reality is unstable |

### Implementation Notes

- Variance is cosmetic only — underlying SimCore values remain deterministic
- Displayed values = true value * (1 + random_range(-variance, +variance)), rerolled each
  UI refresh
- Phantom markers: 10% chance per tick in Fracture+ zones to display a false discovery
  marker that disappears on the next refresh
- The First Officer comments on scanner unreliability at each escalation threshold
- This mechanic is NEVER explained in a tutorial — the player discovers it by noticing
  their instruments disagree with reality

---

## Encounter Narratives

### Discovery Family Templates

Each discovery family has a set of narrative templates that give discoveries personality.
Generated deterministically from seed data. **Implemented** in
`DiscoveryOutcomeSystem.cs` with progressive recontextualization per phase.

**Derelict Family:**
- "A [faction] scout vessel, destroyed approximately [age] years ago. [Damage type]
  suggests [enemy faction] weapons."
- "An abandoned cargo hauler. Emergency logs indicate the crew evacuated after
  [event]. The cargo hold still contains [loot]."
- "A warship fragment lodged in an asteroid. Serial markings identify it as
  [ship name], lost during [historical event]."

**Ruin Family:**
- "Ancient structures partially buried under [terrain]. Architecture does not match
  any known faction. [Material] samples suggest [age] years of exposure."
- "A collapsed research outpost. Equipment markings indicate [faction] origin.
  Data cores contain fragmentary records of [research topic]."

**Signal Family:**
- "A repeating beacon on frequency [freq]. Signal analysis suggests artificial origin,
  broadcasting for [duration]. Source direction: [nearby system]."
- "Intermittent energy spikes from [location]. Pattern analysis shows non-random
  structure. Possible [theory]."

**Market Ruin Family (NEW v2):**
- "An abandoned trading post. Cargo bays still contain unsold [good] — whatever
  happened here, traders left in a hurry. Manifests show a once-thriving [good]
  exchange between [faction A] and [faction B]."
- "A collapsed station with economic records intact. Final entries show cascading
  supply failures: [good] shortages triggered credit runs, which triggered
  evacuation. This economy died in [timeframe]."
- "Infrastructure for a trade route that no longer exists. Automated systems still
  transmit outdated price data on a dead channel. FO notes: the collapse pattern
  matches a known economic vulnerability."

**Why Market Ruins matter:** They are economic archaeology. The player discovers HOW
economies fail — and the FO uses that knowledge to protect current operations. A Market
Ruin at Node 14 might reveal that over-dependence on a single good caused cascading
failure. The FO can then flag that the player's current route portfolio has the same
single-good dependency. Discovery of the PAST prevents failure in the PRESENT. This
directly serves Axiom #2 (discovery feeds automation) in a way no other family does:
Market Ruins don't create new routes, they make existing routes more resilient.

### Narrative Chaining

When discoveries connect through leads, their narratives should reference each other:

```
Kepler Derelict: "A Valorin scout vessel..."
          | (lead)
Altair Signal: "The beacon frequency matches the Valorin scout's
                emergency transponder. A second vessel was here."
          | (lead)
Deneb Ruin: "Communion weapon marks on ancient structures match
             the damage pattern on both Valorin vessels."
```

The player assembles the story: the Valorin sent scouts into Communion territory 300 years
ago. Two were destroyed. The ruins predate both factions. There's a deeper mystery.

### Progressive Recontextualization (implemented)

Each discovery family has three layers of narrative that reveal at each phase:

| Phase | Layer | Example (RESOURCE_POOL_MARKER) |
|-------|-------|-------------------------------|
| Seen | Surface text | "Sensor echoes suggest mineral concentrations in this system" |
| Scanned | Deep text | "The mineral pattern is unusual — it looks like an ancient excavation site" |
| Analyzed | Connection text | "This site predates all known civilizations. The excavation tools used accommodation geometry" |

The "oh, THAT's what that was" moment: what seemed like a natural mineral deposit at
Phase 1 is revealed as an ancient mining operation at Phase 3. The player's understanding
of the discovery changes completely without the physical evidence changing at all.

---

## Exploration Progress Tracking

### Empire Dashboard — Intel Tab Enhancement

(Cross-reference: `EmpireDashboard.md` Explore tab)

```
+-- EXPLORATION -------------------------------------------+
|                                                           |
|  Scanner: Mk2 (3 hops) | Systems: 14/22 | 64% explored  |
|                                                           |
|  -- DISCOVERY PROGRESS --                                |
|  Derelicts:  3 Analyzed, 1 Scanned, 0 Seen              |
|  Ruins:      1 Analyzed, 2 Scanned, 1 Seen              |
|  Signals:    2 Analyzed, 0 Scanned, 1 Seen              |
|                                                           |
|  -- ACTIVE CHAINS --     (NEW)                           |
|  Valorin Expedition: 2/3 complete --> next: Deneb        |
|  Communion Frequency: 1/4 complete --> next: Shimmer-7   |
|                                                           |
|  -- ACTIVE LEADS --                                      |
|  -> Signal at Altair-7 (from Kepler Derelict) [Waypoint] |
|  -> Energy at Wolf-3 (from Deneb Ruin)        [Waypoint] |
|                                                           |
|  -- SURVEY PROGRAMS --   (NEW)                           |
|  Derelict survey (Kepler): 2 scans this cycle, +340cr    |
|  Signal survey (Altair): idle (no unseen signals)        |
|                                                           |
|  -- KNOWLEDGE WEB --                                     |
|  [View full knowledge graph]                             |
|                                                           |
|  -- RECENT DISCOVERIES --                                |
|  ! Deneb Ruin — Analyzed 45t ago — Ancient samples +300  |
|  ~ Barnard Signal — Scanned 120t ago — Awaiting analysis |
|  . Kepler Anomaly — Seen 200t ago — Unscanned           |
+-----------------------------------------------------------+
```

### Exploration Completionism vs. Curiosity

The design must balance two player types:

| Player Type | What They Need | Design Approach |
|-------------|---------------|-----------------|
| **Completionist** | "How much have I found?" | Progress bar: 14/22 systems, 6/12 discoveries resolved |
| **Curious** | "What don't I know yet?" | Active leads pointing to new mysteries, "?" markers on map |

Both are served by the same system. The completionist sees "64% explored." The curious
player sees "2 active leads -> Altair-7 and Wolf-3." Neither should feel the game is
pushing them toward the other's playstyle.

**Rule:** Never show a global completion percentage on the HUD. That turns exploration
into a progress bar, which kills curiosity. Show it ONLY in the Intel tab where the
player deliberately looks for it.

---

## Discovery Density Rules — Mid-Game Pacing

The mid-game (hours 3-8) is the exploration loop's most vulnerable period. Players have
exhausted nearby systems but haven't reached deep frontier discoveries. Without careful
density tuning, exploration becomes "fly to empty system, fly to next empty system."

### Density Gradient

| Distance from Start | Discovery Density | Rationale |
|---|---|---|
| 0-1 hops (starter space) | 1 guaranteed discovery in first 2 hops | Tutorial — teaches the scan/analyze loop |
| 2-3 hops (near frontier) | 60% of systems contain discoveries | Mid-game density — maintains exploration momentum |
| 4-5 hops (deep frontier) | 40% of systems contain discoveries | Natural thinning — but offset by higher-value finds |
| 6+ hops (outer reach) | 25% of systems, but all are high-value | Rarity creates anticipation — every find is significant |

### Jump Event Density

Jump events (scanner anomalies, communication fragments, thread turbulence) bridge
the gaps between discovery sites:

| Zone | Jump Event Frequency | Types Available |
|---|---|---|
| Starter space | Every 5th jump | Routine only (calibration, minor readings) |
| Near frontier | Every 3rd jump | Routine + anomaly + faction encounters |
| Deep frontier | Every 2nd jump | Full range including instability hints |
| Outer reach | Nearly every jump | Instability-heavy, ancient signal fragments |

### Anti-Drought Rule

No player should go more than 10 minutes of active play without encountering SOME
narrative touchpoint (discovery site, jump event, faction dialogue, or First Officer
observation). If the current route would create a drought, seed a jump event to bridge
the gap.

### Late-Game Discovery Injection (NEW)

When the player has analyzed 80%+ of seeded discoveries, new sources activate:

1. **Economy-triggered anomalies**: market crashes, route saturation, or faction
   conflict spawn new Derelict/Signal discoveries at affected nodes
2. **Instability-gated reveals**: Phase 2+ instability reveals previously hidden sites
3. **Chain tier escalation**: completing a Tier 1-2 chain reveals Tier 3 chain hooks
4. **Faction intelligence drops**: high-rep NPCs share classified site locations

This prevents the Stellaris problem: mature empires with nothing left to discover.

---

## Discovery Audio & Visual Milestone Feedback

Each phase transition deserves celebration. Cross-reference `AudioDesign.md` for
audio specifications.

### Audio Vocabulary (4 Signatures) — NEW

Discovery needs a distinct audio language with four signatures that the player learns
to distinguish instantly. Reference: Zelda's secret-found jingle, Subnautica's scanner
beep, Metroid Prime's scan visor lock-on.

| Signature | When | Character | Duration |
|-----------|------|-----------|----------|
| **Anomaly Ping** | Scanner detects something at a new system | Short, sharp, attention-grabbing. A radar blip with harmonic overtone. Must be rare enough to trigger immediate attention. Should be audible above ambient music | 0.3s |
| **Scan Process** | During active scan (Phase 1->2 transition) | Tension-building ambient. Rising tone or rhythmic pulse that resolves on completion. Tempo increases as scan approaches completion | 2-5s (scales with scan duration) |
| **Discovery Reveal** | Scan or analysis complete | Brief musical phrase (3-5 notes). Variants by discovery tier. Tier 1 = simple ascending. Tier 2 = more complex, minor key. Tier 3 = full fanfare with reverb | 1-2s |
| **Insight Chime** | Knowledge graph connection formed, or FO explains significance | Gentler, contemplative. Two-note bell-like tone. Signals "here's context" rather than "here's loot." Distinct from the reveal sting | 0.5s |

### Phase Transition Feedback

| Phase Transition | Visual | Audio | Duration |
|-----------------|--------|-------|----------|
| -> Seen (auto) | Marker appears with subtle fade-in | Anomaly Ping | 0.5s |
| -> Scanned (player action) | Marker brightens, scan-line sweeps | Scan Process -> Discovery Reveal | 1.0-5.0s |
| -> Analyzed (player action) | Full reveal card, marker transforms | Discovery Reveal (tier-appropriate) | 2.0s + card |
| Lead discovered | Blinking marker at destination | Insight Chime | 1.0s |
| Chain link found | Chain progress indicator pulses | Insight Chime + chain-specific motif | 1.5s |
| Knowledge connection | Graph line animates between nodes | Insight Chime (softer variant) | 0.5s |
| **Revelation fires** | Full-screen flash, FO speaks | Unique revelation fanfare (reserved for R1-R5 only) | 3.0s + dialogue |

---

## Lore Integration — How Discovery Delivers the Mystery

### The Thread Builder Mystery Arc

Discovery is the primary vehicle for the game's central mystery. The player assembles
understanding through gameplay evidence, data logs, and fragment collection — in that
order. Per `factions_and_lore_v0.md`:

**Layer 1: Physical evidence (exploration)**
- Find derelicts, ruins, signals -> evidence of an ancient civilization
- No explanation yet. Just: "someone was here before, a long time ago"

**Layer 2: Fragments (collection)**
- Collect adaptation fragments -> understand what the ancients BUILT
- Cover-story names ("Regenerative Polymer Sample") until R1 fires
- Dual lore: mundane interpretation flips to alien truth after revelation

**Layer 3: Data logs (narrative)**
- Read conversations between scientists at discovery sites
- Assemble the Containment-vs-Accommodation debate across 25+ logs
- Five named scientists with contradictions revealed across multiple sites
- CRITICAL: logs are private records, never addressed to posterity

**Layer 4: Personal experience (fracture travel)**
- Using the fracture module IS accommodation geometry in action
- The UI distortions in unstable space are the pilot seeing MORE accurately
- The module adapts the pilot neurologically (never stated, only implied)
- Scanner precision improves with cumulative fracture exposure

**Layer 5: Revelations (story state machine)**
- R1 (Module Origin): "The module is not technology I built — it was waiting"
- R3 (Pentagon Ring): "The resource distribution is engineered"
- R5 (Living Geometry): "The instability is not chaos — it's alive"
- Each revelation recontextualizes everything the player has already seen

### Ancient Data Log Placement Rules

Per `content/LoreContent_TBA.md` and `factions_and_lore_v0.md`:

1. **Logs are NEVER addressed to the future.** They are internal records.
2. **No single log reveals a scientist's full contradiction.** Assembly required.
3. **Every log contains at least one mechanical hook** (coordinates, trade intel,
   calibration data, resonance location hint).
4. **Logs are placed at anomaly chain nodes** — the chain leads the player to the
   log, not the other way around.
5. **Revelation tier gating**: Tier 1 logs found in safe space, Tier 3 logs found in
   Phase 3+ space requiring fracture drive. The player must go deeper to learn more.

### The Cover-Story System

Until Revelation R1 fires, all ancient artifacts use mundane cover names:

| Real Name | Cover Name | Flipped At |
|-----------|-----------|-----------|
| Growth Lattice | Regenerative Polymer Sample | R1 |
| Void Girder | Structural Resonance Amplifier | R1 |
| Cascade Core | Exotic Energy Matrix | R1 |
| Pattern Engine | Advanced Computation Substrate | R1 |
| Fracture Drive | Structural Resonance Engine | R1 |

The cover names are completely plausible scientific terms. The player never suspects
these artifacts are alien technology — they seem like advanced materials science. R1
flips all names simultaneously, creating a cascade of "wait, ALL of those were...?"

---

## Anti-Patterns to Avoid

| Anti-Pattern | Game That Failed | Our Rule |
|---|---|---|
| **Exploration as checkbox** | No Man's Sky at launch | Discoveries connect to each other and to faction lore |
| **No knowledge persistence** | Games where the player must take notes | IntelBook records everything with timestamps |
| **Silent phase transitions** | Current implementation | Audio + visual card + brief pause for every milestone |
| **100% completion pressure** | Ubisoft open worlds | Completion % only in Intel tab, never on HUD |
| **Discoveries without narrative** | Procedural games with no context | Every discovery has flavor text from family templates |
| **No breadcrumb trail** | Random exploration without direction | Leads chain discoveries into narrative sequences |
| **Scanner as passive stat** | "Range: 3" with no visual impact | Scanner sweep animation when entering new systems |
| **Scan fatigue** | Elite Dangerous FSS | Automation graduation: manual first, programs after 4th scan of a type |
| **Discovery disconnected from economy** | Many exploration games | Every discovery yields actionable trade intelligence (§EconomicIntel) |
| **One-shot discoveries** | Stellaris individual anomalies | Anomaly chains (3-5 sites) create memorable arcs |
| **Discovery dries up** | Stellaris mature empires | 5 late-game sources keep discoveries flowing |
| **Pure randomness** | No Man's Sky at launch | Constrained pools where players develop intuition |
| **Full explanation** | Over-exposited sci-fi | Incomplete knowledge is more compelling. Never name the thread builders |
| **Every scan succeeds** | Games without scan risk | 6 failure types with partial success. Failure reveals information too (§Discovery Failure States) |
| **FO as notification stream** | Stellaris advisor spam | FO observation limits: cooldown, queue cap, overflow indicator. Player controls information flow (§FO Observation Limits) |
| **Invisible automation** | X4 "auto-pillock" | FO confidence through personality-colored dialogue. World adaptation is visible. Competence tiers never auto-advance (§Centaur Model) |
| **Spectator trough** | Stellaris late-game | Automation generates new exploration opportunities. 5 boredom circuit breakers ensure player always has meaningful work (§Player During Automation) |
| **Stupid delegation AI** | Stellaris sectors (pre-3.9 removal) | FO learns from player behavior, expresses confidence through personality, adapts to world events transparently. Player can always override (§Competence Tiers) |
| **Read-only knowledge** | Most exploration games | 5 player verbs on Knowledge Graph. Speculative links create Obra Dinn detective moments (§KG Player Verbs) |

---

## Reference Games — Expanded

| Mechanic | Best Reference | Key Lesson |
|---|---|---|
| Knowledge graph | **Outer Wilds** Ship Log | Every discovery connects to something — web of knowledge, not a list. The only progression is what you know. Rumor mode creates interconnected curiosity web |
| Discovery phases | **Subnautica** | Find -> scan -> understand lifecycle. PDA as diegetic delivery (our FO fills this role). Scanning is brief but requires proximity, creating a "lean in" moment |
| Scanner as tool | **Mass Effect** planetary scan | Scanning FEELS like an action — visual sweep, audio feedback. But: avoid ME2's tedium (scan every planet for resources) |
| Narrative chaining | **Return of the Obra Dinn** | Fragments assemble into a story the player pieces together. No single fragment tells the whole story |
| Exploration motivation | **Outer Wilds** | Curiosity-driven, not reward-driven. "I wonder what's there" > "+50 XP" |
| Intel freshness | **Elite Dangerous** community tools | Stale data shown differently from fresh — motivates re-scanning |
| Anomaly chains | **Stellaris** | Multi-event chains with branching choices are the most remembered content. One-shots are forgotten immediately |
| Discovery-as-economy | **X4: Foundations** | Scanning a station reveals its buy/sell orders. Exploration directly feeds trading efficiency |
| Information asymmetry | **EVE Online** | Discovered wormhole routes = real economic advantage. Knowledge is perishable and exclusive |
| Anti-fatigue | **Elite Dangerous** (what NOT to do) | Scan #500 must not feel like scan #1. Automate the routine, preserve the novel |
| Pain before relief | **Factorio** | Feel the bottleneck BEFORE the discovery that resolves it. Discovery is the answer to a question the player is already asking |
| Procedural + authored | **No Man's Sky** Desolation update | Derelict freighters: procedural layouts with hand-crafted story skeletons. Every site feels designed because the dramatic arc is authored |
| Constrained randomness | **FTL** | Curated event pools where players develop intuition. 2-4 outcomes per event type. Variance within a known range |
| Planted seeds | **Mass Effect** Eletania ruin | Throwaway in ME1, significant in ME3. Early discoveries contain buried significance |
| Incomplete knowledge | **Mass Effect** Prothean discs | "Scientists cannot yet transcribe." Mystery is the reward. Never fully explain |
| First-discovery credit | **Elite Dangerous** exploration | Player's name permanently on a first-discovery. Cheap to implement, disproportionately motivating |
| Environmental storytelling | **Subnautica** alien facilities | Environment shows what happened; text explains what people thought about it. Neither alone is sufficient |
| Diegetic discovery UI | **Dead Space**, **Subnautica** | The scanning tool exists in the game world. Our FO is the diegetic delivery mechanism |
| Audio discovery palette | **Zelda** secret-found jingle, **Metroid Prime** scan visor | Distinct signatures for detection, scanning, revelation, and insight. Must be rare enough to trigger immediate attention |

---

## SimBridge Queries — Existing and Needed

### Existing (implemented)
| Query | Purpose | Location |
|-------|---------|----------|
| `DispatchScanDiscoveryV0(siteId)` | Advance discovery phase | SimBridge.cs |
| `AdvanceDiscoveryPhaseV0(discoveryId)` | Inline phase advance with reason-code | SimBridge.cs |
| `GetDiscoveryListSnapshotV0()` | All discoveries ordered by ID | SimBridge.Reports.cs |
| `GetDiscoverySnapshotV0(nodeId)` | Discoveries at a node with phase status | SimBridge.TradeIntel.cs |
| `GetDiscoveryOutcomesV0()` | All AnomalyEncounter outcomes | SimBridge.TradeIntel.cs |
| `GetDiscoveryPhaseMarkersV0()` | Per-node phase for galaxy map icons | SimBridge.GalaxyMap.cs |
| `GetAnomalyEncounterSnapshotV0(id)` | Single encounter snapshot | SimBridge.Combat.cs |
| `GetActiveEncountersV0()` | All AnomalyEncounters sorted | SimBridge.Combat.cs |
| `GetFractureDiscoveryStatusV0()` | FractureUnlocked + derelict location | SimBridge.Fracture.cs |
| `GetAvailableVoidSitesV0()` | Discovered/surveyed void sites | SimBridge.Fracture.cs |
| `GetAdaptationFragmentsV0()` | All 16 fragments with collection status | SimBridge.Haven.cs |
| `CollectFragmentV0(fragmentId)` | Collect adaptation fragment | SimBridge.Haven.cs |
| `GetResonancePairsV0()` | 8 pairs with bonus descriptions | SimBridge.Haven.cs |
| `GetTrophyWallV0()` | Collected fragments + resonance completion | SimBridge.Haven.cs |
| `GetIntelFreshnessByNodeV0()` | Per-node intel age | SimBridge.TradeIntel.cs |

### Needed (new design requirements)
| Query | Purpose | Design Section |
|-------|---------|---------------|
| `GetKnowledgeGraphV0()` | All discoveries + connections + chain progress | Knowledge Graph |
| `GetActiveLeadsV0()` | Unresolved leads pointing to undiscovered locations | Rumor Leads |
| `GetExplorationProgressV0()` | Systems visited, discoveries by phase, completion stats | Progress Tracking |
| `GetScannerRangeV0()` | Current scanner level and hop range | Scanner Range |
| `GetDiscoveryNarrativeV0(discoveryId)` | Generated flavor text for a discovery | Encounter Narratives |
| `GetConnectedDiscoveriesV0(discoveryId)` | Discoveries linked to this one | Knowledge Graph |
| `GetDiscoveryTradeIntelV0(discoveryId)` | Economic intel from a discovery (NEW) | Discovery-as-Trade-Intelligence |
| `GetChainProgressV0(chainId)` | Anomaly chain completion status (NEW) | Anomaly Chains |
| `GetActiveChainsV0()` | All in-progress chains with next-step hints (NEW) | Anomaly Chains |
| `GetSurveyProgramStatusV0()` | Active survey programs and their results (NEW) | Automation Graduation |
| `GetIntelDecayStatusV0(nodeId)` | Intel freshness with economic impact estimate (NEW) | Information Asymmetry |
| `GetInstabilityRevealedSitesV0()` | Sites newly visible due to instability (NEW) | Late-Game Discovery |

---

## Implementation Roadmap

### FO Trade Manager Prerequisites — Build Order

> **CRITICAL:** The 6 TODO S6 epics ARE the FO Trade Manager prerequisites, not separate
> work. The priority order below is driven by which FO Trade Manager phase each feature
> unblocks. See `fo_trade_manager_v0.md` §Cross-System Dependencies and §Migration Path.

| Priority | Feature | Effort | Unlocks (FO Phase) | Axioms |
|----------|---------|--------|-------------------|-----------|
| **P0** | Intel decay → margin buffer wiring | Small | Mechanical exploration pressure loop (wiring only). Unblocks **FO Phase 2** | 2, 4 |
| **P0** | Discovery-as-trade-intel (DiscoveryOutcome ext + EconomicIntel entity + DISCOVERY_OPPORTUNITY trigger) | Medium | Exploration→automation pipeline links 2-3. Unblocks **FO Phase 2** (first handoff) | 2 |
| **P0** | Audio discovery vocabulary (4 signatures) | Small | Feel — silent phase transitions waste emotional investment | 6 |
| **P1** | Anomaly chain intel integration (ChainIntel at each step + FO evaluation per step). **MUST ship with personality-colored FO commentary** — Maren notices economic patterns, Dask notices tactical signatures, Lira notices navigational oddities. Same chain, different FO perspective = replayability hook. Use placeholder personality variants if full discovery emphasis system (item 17) isn't ready | Medium | Mid-game content engine, discovery emphasis per chain. Unblocks **FO Phase 3** (competence, ancient tech) | 3 |
| **P1** | SurveyProgram automation graduation | Medium | Scan fatigue prevention. Directly serves automation core loop | 6 |
| **P1** | Discovery milestone cards (visual) | Small | The visual complement to audio. SCAN COMPLETE / ANALYSIS COMPLETE cards | 6 |
| **P2** | Artifact research → AncientTechUpgrade (EPIC.S6.ARTIFACT_RESEARCH) | Large | Ancient Tech qualitative shifts, endgame exploration payoff. Unblocks **FO Phase 4-5** (fleet, lore evidence) | 2, 5 |
| **P2** | Late-game discovery continuation (economy-triggered + instability-gated + pipeline inversion) | Medium | Prevents Stellaris exploration death. Unblocks **FO Phase 4-5** | 4 |
| **P1** | NPC route competition (NpcTradeSystem margin compression over 300-500 ticks) | Medium | Second depreciation mechanism — intel decay alone isn't enough if player keeps visiting same nodes. Creates steady background pressure that keeps exploration necessary | 4 |
| **P2** | Scanner sweep animation | Small | Visual frontier that makes the scanner feel like a tool | existing |
| **P2** | Breadcrumb trail visualization | Medium | Visual chain threading on galaxy map | existing |
| **P2** | EPIC.S6.CLASS_DISCOVERY_PROFILES | Medium | WorldClass influences discovery families and outcomes | 5 |
| **P3** | EPIC.S6.ANOMALY_ECOLOGY | Medium | Procedural anomaly distribution with spatial logic | 5 |
| **P3** | EPIC.S6.SCIENCE_CENTER | Large | Analysis throughput, reverse engineering — late-game depth | 2 |
| **P3** | EPIC.S6.TECH_LEADS | Medium | Tech leads become prototype candidates | 2 |
| **P3** | EPIC.S6.EXPEDITION_PROG | Medium | Multi-step expedition programs | 6 |
| **P1** | Discovery Failure States (6 failure types + partial success) | Small | Prevents "every scan succeeds" anti-pattern. Creates tension during scanning | 1, 3 |
| **P1** | FO Confidence through personality (archetype-specific confidence language) | Small | Player trust via personality-colored reasoning, not generic bars. Unblocks Tier 2+ | 2 |
| **P2** | Knowledge Graph Player Verbs (Pin, Annotate, Link, Flag, Compare) | Medium | Transforms KG from passive display to investigation tool. Link verb = Obra Dinn moment | 1 |
| **P2** | Knowledge Graph dual-mode display (geographic + relational) | Medium | Geographic for "where next?", relational for "how do these connect?" | 1 |
| **P2** | Re-Exploration verb (Re-Scan, Deep Analysis, Recontextualization) | Medium | Late-game content refresh. Prevents "nothing left to find." Post-revelation map refresh | 4, 6 |
| **P2** | Trade History as Revelation Evidence (3 progressive triggers) | Medium | Player's own trade data as proof for pentagon ring. Unblocks **FO Phase 4-5** | 1, 3 |
| **P2** | Player-during-automation circuit breakers (spectator trough prevention) | Small | 5 boredom triggers that nudge player back to exploration. Prevents alt-tab syndrome | 2 |
| **P3** | Link feedback system (Obra Dinn batch confirmation, 3-link Insight bonus) | Medium | Wrong-link feedback + batch confirmation. Reward theory-building, not guessing | 1 |
| **P3** | FO World Adaptation + Learning system | Medium | 5 world event types with adaptation, 4 behavioral patterns. World fails, FO adapts — not FO mistakes | 2 |
| **P4** | EPIC.S6.MYSTERY_MARKERS | Small | Mystery style policy — systemic vs explicit markers | 5 |
| **P4** | First-discovery credit system | Small | Player name on first-discoveries. Low effort, high reward | 6 |
| **P4** | Exploration overlay lens | Medium | GalaxyMap.md aspiration | existing |

### FO Trade Manager Phase Gate Summary

```
FO Phase 2 (first handoff, tick ~200) BLOCKED UNTIL:
  ✓ IntelBook.IntelFreshness exists                    (DONE)
  ✓ ProgramSystem exists                               (DONE)
  ✓ GenerateDiscoveryTradeIntel()                      (DONE — T41)
  ✓ FIRST_TRADE_ROUTE_DISCOVERED trigger + dialogue    (DONE — T41, 3 archetypes)
  ✓ TRADE_INTEL_STALE trigger + dialogue               (DONE — T41, 3 archetypes)
  ✓ ApplyDiscoveryRouteDecay() distance bands          (DONE — T41)
  ✗ Intel decay → margin buffer wiring                 (P0 — CalculateEffectiveMargin)
  ✗ Typed EconomicIntel entity per family              (P0 — extends DiscoveryOutcome)
  ✗ Per-discovery DISCOVERY_OPPORTUNITY trigger         (P0 — moment-by-moment centaur beat)

FO Phase 3 (competence tiers, tick ~600) BLOCKED UNTIL:
  ✓ AnomalyChainSystem + entity + content              (DONE — T48)
  ✓ TryAdvanceChains() wired in DiscoveryOutcome       (DONE — T48)
  ✓ CHAIN_LINK_DISCOVERED + CHAIN_COMPLETED triggers   (DONE — T41, 3 archetypes)
  ✗ Per-step ChainIntel with escalating scope          (P1 — 4-tier intel per chain step)
  ✗ FO discovery emphasis per chain step               (P1 — personality-colored commentary)
  ✗ Artifact → FO installation pipeline                (P2 — AncientTechUpgrade, pre-Science
                                                         Center workaround available)

FO Phase 4-5 (fleet, lore evidence, tick ~1200) BLOCKED UNTIL:
  ✗ Late-game discovery continuation                   (P2 — pipeline inversion)
  ✗ EPIC.S6.ARTIFACT_RESEARCH                          (P2 — full research queue)
  ✗ Trade data as lore evidence                        (P2 — LORE_ANOMALY + player trade history)
  ✗ Trade history as revelation evidence               (P2 — player's own data as proof, §NEW)
```

### Implementation Dependencies

```
Intel Decay → Margin Buffer (P0, BUILD FIRST)
  |
  +-- IntelBook.GetNodeAge(nodeId) (DONE)
  +-- ApplyDiscoveryRouteDecay() distance bands (DONE — T41)
  +-- DiscoveryIntelTweaksV0 (DONE — decay thresholds)
  +-- TRADE_INTEL_STALE trigger + 3 archetype dialogue (DONE — T41)
  +-- ProgramSystem.CalculateEffectiveMargin() (NEW — reads worst freshness)
  +-- FO margin-impact dialogue (NEW — "Route Delta earning less because intel aged")

Discovery-as-Trade-Intelligence (P0, BUILD SECOND)
  |
  +-- GenerateDiscoveryTradeIntel() (DONE — T41, creates TradeRouteIntel)
  +-- TradeRouteIntel.SourceDiscoveryId (DONE — T41, links route to discovery)
  +-- FIRST_TRADE_ROUTE_DISCOVERED trigger + dialogue (DONE — T41, 3 archetypes)
  +-- DiscoveryOutcomeSystem (DONE — extend with typed EconomicIntel entity)
  +-- IntelBook (DONE — extend with structured intel entries per family)
  +-- FirstOfficerSystem.TryFireTrigger("DISCOVERY_OPPORTUNITY") (NEW — per-discovery)
  +-- FO dialogue: 3 variants × 3 archetypes for per-discovery evaluation (NEW)

Anomaly Chain Intel Integration (P1)
  |
  +-- AnomalyChainSystem (DONE — T48)
  +-- AnomalyChainContentV0 (DONE — T48, extend with per-step ChainIntel)
  +-- TryAdvanceChains() wired in DiscoveryOutcome (DONE — T48)
  +-- CHAIN_LINK_DISCOVERED + CHAIN_COMPLETED triggers (DONE — T41, 3 archetypes)
  +-- FO discovery emphasis commentary per chain step (NEW)
  +-- KnowledgeGraphSystem (DONE — chain steps produce graph connections)

Artifact → FO Installation (P2)
  |
  +-- DiscoveryOutcomeSystem (DONE — already produces artifacts)
  +-- EPIC.S6.SCIENCE_CENTER (P3 — or use pre-Science Center workaround)
  +-- AncientTechUpgrade entity (NEW — typed effects, see §Artifact Research)
  +-- TradeManager.AncientTechSlots (NEW — FO Trade Manager entity)
  +-- ANCIENT_TECH_INSTALLED trigger (NEW — fo_trade_manager_v0.md)

Late-Game Discovery (P2)
  |
  +-- Instability-gated reveals (DiscoverySeedGen — extend with instability_gate)
  +-- Economy-triggered anomalies (new system: DynamicAnomalySystem.cs)
  +-- Pipeline inversion: FO LORE_ANOMALY → Knowledge Graph leads (NEW)
  +-- Chain tier escalation (AnomalyChain — tier unlock logic) (DONE — T45)

Audio Vocabulary (P0)
  |
  +-- 4 audio signatures (assets — composition/sourcing)
  +-- Phase transition hooks in GameShell (GDScript signal handlers)
  +-- Tier-variant reveal stings (3 variants per signature)

Information Asymmetry (P2)
  |
  +-- NPC trade route discovery delay (NpcTradeSystem — extend)
  +-- Intel decay curves (IntelBook — extend with explicit decay)
  +-- Discovery freshness → economic fog on galaxy map (NEW — UI)

Discovery Failure States (P1)
  |
  +-- DiscoveryOutcomeSystem (DONE — extend with failure outcomes)
  +-- 6 failure types: Scan Interference, Hazard Abort, Intel Spoilage,
  |   Chain Dead End, Contested Discovery, False Positive
  +-- Partial success mechanic (failure still yields information)
  +-- FO personality-specific failure commentary (3 archetypes)

FO Confidence + World Adaptation + Learning (P1-P3)
  |
  +-- FO Confidence through personality — archetype-specific language (P1)
  +-- FO World Adaptation — 5 world event types, FO adapts not errs (P3)
  +-- FO Learning from Player — 4 adaptation patterns (P3)
  +-- 3-tier competence model (§Centaur Model) — prerequisite

NPC Route Competition (P1, bumped from P2)
  |
  +-- NpcTradeSystem (DONE — extend with route discovery over time)
  +-- Margin compression: 30% → 10-15% over 300-500 ticks as NPCs rebalance
  +-- ROUTE_DEGRADED + ROUTE_DEAD FO triggers (integrate with existing triggers)
  +-- FO suggests replacements: "locals figured it out, but that new system..."

Knowledge Graph Player Verbs + Link Feedback (P2-P3)
  |
  +-- KnowledgeGraphSystem (DONE — extend with player verb handlers)
  +-- 5 verbs: Pin, Annotate, Link, Flag for FO, Compare (P2)
  +-- Dual-mode display: geographic + relational (P2)
  +-- Link feedback: Speculative→Plausible→Confirmed→Contradicted (P3)
  +-- Batch confirmation (3-link Insight bonus) (P3)

Re-Exploration + Trade History as Evidence (P2)
  |
  +-- Re-Scan, Deep Analysis, Recontextualization phases (P2)
  +-- Post-revelation gold shimmer markers (P2)
  +-- Trade History: 3 progressive triggers (NOTICED→SUSPICIOUS→PROOF) (P2)
  +-- KnowledgeGraphSystem connection for trade proof nodes (P2)
  +-- FO pre-revelation breadcrumbs in decision log (P2)

Spectator Trough Prevention (P2)
  |
  +-- 5 boredom circuit breakers (triggered by tick/margin/sustain thresholds)
  +-- FO nudge dialogue (3 archetypes per trigger)
  +-- Story system lead planting (500-tick revelation progress check)
  +-- Depends on: 3-tier competence model, FO Observation Limits, Sustain→Exploration loop
```
