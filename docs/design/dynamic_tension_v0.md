# Dynamic Tension — Goal Doc

> Design goal document for making Space Trade Empire compelling and replayable
> through dynamic pressure from tick 1. Companion to `trade_goods_v0.md`,
> `ship_modules_v0.md`, and `EmpireDashboard.md`.

## Implementation Status (as of Tranche 20, 2026-03-08)

| Pillar | Status | Key Systems |
|--------|--------|-------------|
| 1. Galaxy at War | 60% Implemented | WarfrontDemandSystem.cs, GalaxyGenerator.SeedWarfrontsV0 |
| 2. Maintenance Treadmill | 20% Implemented | MaintenanceSystem.cs (industry only) |
| 3. Economic Cascades | 70% Implemented | MarketSystem.cs, EmbargoState.cs, WarfrontDemandSystem.cs |
| 4. Shrinking Middle | 85% Implemented | ReputationSystem.cs, MarketSystem.cs, FactionTweaksV0.cs |
| 5. Fracture Temptation | 50% Implemented | FractureSystem.cs, FractureTweaksV0.cs |

### Open Questions — RESOLVED
- Simultaneous warfronts: **RESOLVED** — 1 hot (Valorin vs Weavers, OpenWar) + 1 cold (Concord vs Chitin, Tension)
- Demand shocks: **RESOLVED** — Deterministic, scaled by warfront intensity (Munitions 4x, Fuel 3x, Composites 2.5x)
- Neutrality tax curve: **RESOLVED** — Stepped at warfront intensity thresholds (Skirmish +5%, OpenWar +10%, TotalWar +15%)
- Ceasefire brokering: **RESOLVED** — Supply-driven only (cumulative deliveries exceed threshold → intensity decreases)

---

## The Problem

The player can coast through early/mid game with zero pressure. Warfronts are
ignorable backdrop. Fracture threat only matters late. There is no reason to
take risks until the endgame forces it.

## The Identity (What We Are NOT)

- **Not a 4X race game.** You are not the Emperor. You are Han Solo — a
  pilot-entrepreneur caught in a galactic war, trying to profit and survive
  while larger forces reshape the galaxy around you.
- **Not a roguelite.** No meta-progression, no run-based unlocks. Each
  playthrough is a long campaign (15-30 hrs). Replayability comes from
  different seeds, faction choices, and win paths — not from unlocking more
  stuff.

---

## The Five Pillars of Tension

### Pillar 1: The Galaxy Starts at War (Early Game)

> **Implementation: 60%**
> - ✅ Warfronts seeded at worldgen — 2 wars active from tick 1 (SeedWarfrontsV0)
> - ✅ War-driven demand consumption at contested nodes (WarfrontDemandSystem.Process)
> - ✅ Warfront tariff surcharges scale with intensity (300 bps/level, MarketSystem)
> - ❌ Starter system not placed near warfront — uses arbitrary first node
> - Code: WarfrontDemandSystem.cs, GalaxyGenerator.cs, WarfrontTweaksV0.cs

The player spawns into active conflict, not a quiet galaxy that eventually
heats up.

- Faction wars are already consuming goods and distorting prices at tick 1.
- The player's starter system borders a warfront — not safely nested in core
  space.
- Trade opportunities exist **because of** war (price spikes, supply
  shortages), not despite it.
- Safe routes are already thin-margin (NPC traders have arbitraged them). The
  juicy margins are near contested systems.
- The player's very first trade decision is shaped by conflict: "Metal is 3x
  price at the besieged station two hops away — do I risk it?"

**Design consequence**: The procedural galaxy generator seeds warfronts as part
of world creation, not as a mid-game event. Different seeds = different warfront
geography = different starting puzzles.

### Pillar 2: The Maintenance Treadmill (Baseline Urgency)

> **Implementation: 20%**
> - ✅ MaintenanceSystem exists for industry site decay/repair (health degradation, supply consumption)
> - ❌ No passive fleet fuel consumption — ships sit docked at zero cost
> - ❌ No module sustain enforcement — SustainInputs defined but resources never consumed
> - ❌ No "standing still costs money" pressure — early game has no urgency
> - Code: MaintenanceSystem.cs, MaintenanceTweaksV0.cs (industry only)

Standing still costs money. The player must always be earning.

- Ship consumes Fuel passively (docked or flying).
- Module sustain costs tick whether you are trading or idle.
- Credits drain toward zero if you stop moving.
- This transforms every decision from "what is optimal?" to "what keeps me
  alive this cycle?"

**Design consequence**: The sustain mechanic (which already exists) needs to
bite from the start. The starter ship's running costs should be tuned so the
player has ~50-80 ticks of runway before going broke if they do nothing. Not
punishing — just enough to say "you need to be doing something."

**Reference**: Sunless Sea's fuel gauge. Every journey costs something, which
makes every journey meaningful.

### Pillar 3: Warfront Economic Cascades (Mid Game)

> **Implementation: 70%**
> - ✅ Embargo blocks trade in war-critical goods (EmbargoState, SeedEmbargoesV0)
> - ✅ War demand drains contested node inventories → price spikes (WarfrontDemandSystem)
> - ✅ NPC trade stabilizes prices but cannot prevent warfront-driven shortages
> - ❌ Supply chains don't naturally break yet — only 3/9 production recipes instantiated as industry sites
> - ❌ No cascading price shocks beyond direct war nodes (needs full production chain graph)
> - Code: MarketSystem.cs, WarfrontDemandSystem.cs, EmbargoState.cs, NpcTradeSystem.cs

When warfronts shift, the consequences are economically inescapable.

- Faction loses territory -> trade routes change control -> tariffs change ->
  your automation programs stall or become unprofitable.
- Faction wins decisively -> they embargo the loser's goods -> supply chains
  break -> price shocks ripple through connected systems.
- The player's empire is always exposed to at least one warfront's economic
  blast radius — there is no "safe corner" of the galaxy.
- Wars should be felt in **prices**, not just on a battlefield. A Munitions
  spike drives up Metal and Fuel, which pressures Components, which degrades
  your automation programs.

**Design consequence**: The economic simulation needs warfront-driven demand
shocks. When a faction is fighting, it consumes goods at elevated rates,
creating scarcity that ripples outward. The player does not fight in wars —
they feel wars through their wallet.

### Pillar 4: The Shrinking Middle Ground (Late Mid Game)

> **Implementation: 85%**
> - ✅ Rep-based tariff scaling: allied=-15%, hostile=+20% (ReputationSystem.cs)
> - ✅ Neutrality tax at warfront intensity ≥2: Skirmish +5%, OpenWar +10%, TotalWar +15%
> - ✅ Trade access gating by rep tier — dock/trade/tech thresholds (Hostile blocks docking)
> - ✅ Territory regime matrix: TradePolicy + RepTier → Open/Guarded/Restricted/Hostile
> - ✅ War profiteering: sell war goods → +2 buyer rep, -1 enemy rep
> - ❌ No explicit faction contract offers (neutrality cost is implicit via tariffs, not explicit)
> - Code: ReputationSystem.cs, MarketSystem.cs, FactionTweaksV0.cs

Neutrality becomes increasingly expensive as wars escalate.

- **Early**: All factions welcome traders. Tariffs are low. Ports are open.
- **Mid**: Factions start offering exclusive supply contracts. "Sell Munitions
  only to us -> Allied pricing. Sell to our enemy -> blockade."
- **Late**: Neutral traders face climbing tariffs, hostile inspections,
  restricted port access. The neutrality tax grows until it forces a choice.
- The player must decide: **commit to a faction** (gaining security, losing
  freedom and access to enemy markets) or **stay independent** (paying
  escalating costs, maintaining access to all sides).

**Design consequence**: Faction reputation thresholds should have economic
teeth that scale with warfront intensity. At peace, reputation barely matters.
At total war, reputation determines whether you can even dock.

**Reference**: Mount & Blade's faction system. The middle ground exists but
shrinks as wars escalate. Staying neutral is a valid choice, but it is a choice
with costs — not a free default.

### Pillar 5: The Fracture Temptation (Cross-cutting Pressure)

> **Implementation: 50%**
> - ✅ Fracture travel with fuel cost (20/jump), hull stress (10 HP/jump), 10x speed penalty
> - ✅ Fracture market pricing: 1.5x volatility, 2x spread, 50% volume cap
> - ✅ Trace accumulation: +0.5 per arrival, decay -0.01/tick, detection at 1.0 → -10 rep
> - ✅ Fracture goods flow: exotic_matter/exotic_crystals/salvaged_tech at 10%/tick into thread hubs
> - ❌ Trace consequences limited to rep penalty — no interdiction waves, supply shocks, or doom clock
> - ❌ No delayed fracture discovery gating (available immediately, not at tick 300+)
> - Code: FractureSystem.cs, FractureTweaksV0.cs

The fracture module is **not available at game start.** The player spends
Hours 0-3 (roughly tick 0-300) as a pure thread trader — learning markets,
feeling warfront pressure, upgrading with standard T1 equipment. The module
is discovered at a frontier derelict near the warfront around Hour 3-4, once
the player has internalized the constraints of thread-space.

This delayed discovery is critical:
- The player learns the rules before they can break them.
- Standard equipment creates a baseline — exotic tech is exciting because
  you know what "normal" feels like.
- The warfront is personal before the escape valve appears. By Hour 3, the
  player has lost margins to tariffs, been inspected by patrols, felt the
  squeeze. They WANT an alternative.

Once discovered, Fracture travel becomes the escape valve for warfront
pressure — but using it creates its own doom clock.

- When a warfront closes a critical trade thread, Fracture travel is the only
  way to maintain supply chains.
- But Fracture use accumulates Trace -> eventual interdiction waves, supply
  shocks, thread disruptions.
- The player is squeezed between two pressures: warfront economics pushing
  them toward Fracture, and Fracture consequences pulling them toward the
  endgame crisis.
- This creates a **dual doom clock**: external pressure (war) and internal
  pressure (Trace). The player cannot solve both simultaneously — they must
  balance.

**Design consequence**: Fracture should feel like a deal with the devil. Every
use solves an immediate problem and creates a future one. The player who never
uses Fracture faces warfront pressure they cannot escape. The player who
overuses Fracture accelerates the endgame crisis.

**The deeper consequence** (see `factions_and_lore_v0.md` → "The Deeper Truth:
The Ring Is Engineered"): Fracture trade doesn't just bypass blockades — it
demonstrates that the pentagon dependency ring is artificial. Trade routes
established in fracture space break the ring pattern: stations can produce
goods they "shouldn't" be able to, because the suppression of those capabilities
was part of the thread infrastructure's economic engineering. Every fracture trade
route the player builds is not just an economic shortcut — it is evidence that
the galaxy's economic geography is a cage. This discovery (Revelation 3, ~Hour
15) recontextualizes the player's entire trading career as participation in
a system of control.

> Full timing details: see `factions_and_lore_v0.md` — "Fracture Module
> Timing" section.

---

## Replayability Engine

No meta-progression. No unlocks across runs. Replayability comes from
**starting conditions and branching decisions**:

| Seed Variable | Effect on Playthrough |
|---|---|
| Warfront location relative to start | Which goods are disrupted, which routes are dangerous |
| Faction territory layout | Which alliances are geographically viable |
| Resource distribution | Which supply chains are easy/hard to build |
| Number of active warfronts | How much neutral space exists |
| Chosen win condition | Which faction relationships matter, which Fracture exposure is needed |

Each seed poses a different strategic puzzle. "I wonder what happens if I ally
with the Syndicate this time" is the replay hook — not "I need to unlock the
Cruiser class."

**One possible addition**: Challenge modifiers unlocked after first win (faster
warfront escalation, higher maintenance, restricted Fracture). Harder puzzles
for experienced players — not more tools.

---

## Player Experience Arc

| Phase | Ticks | Player Feeling |
|---|---|---|
| **Scramble** (early) | 0-150 | "I need to find a profitable route before I go broke. That warfront is disrupting Metal prices — opportunity or death trap? These Communion stations need Food badly..." |
| **Establish** (early-mid) | 150-400 | "My routes are working. I have upgraded my ship with T1 modules. The warfront is shifting — I need to diversify. What is this strange derelict on the frontier...?" |
| **Revelation** (mid-early) | 400-600 | "The fracture module changes everything. I can bypass the blockade — but at what cost? The Haven is a safe harbor, but Trace is accumulating..." |
| **Choose** (mid) | 600-1200 | "Concord wants exclusive supply rights. The Chitin are offering better margins. Staying neutral is costing me 15% in tariffs. Who do I back?" |
| **Commit** (late-mid) | 1200-2000 | "I have backed Concord but the front is losing. Fracture routes keep my supply chain alive, but the Trace cost is climbing..." |
| **Endgame** | 2000+ | "Trace is critical. Warfronts are destabilizing. The module is not what I thought it was. I need to choose a path and commit before the galaxy collapses around me." |

Every phase has pressure. Every phase has meaningful choices. The player is
never coasting. The first 3-4 hours are pure thread-space — the player earns
their understanding of the galaxy before the fracture module recontextualizes
it.

---

## Implementation Surface

| Area | Current State (Tranche 20) | Remaining Work |
|---|---|---|
| `GalaxyGenerator` | ✅ Seeds 2 warfronts at tick 1 (SeedWarfrontsV0). Faction territories via BFS | Starter system placement near a warfront (currently arbitrary) |
| Sustain/Maintenance | ✅ Industry site decay/repair. Module SustainInputs schema defined | Fleet fuel consumption. Module sustain enforcement. ~50-80 tick runway tuning |
| `MarketSystem` | ✅ Warfront demand shocks (Munitions 4x, Fuel 3x, Composites 2.5x). Embargo enforcement | Full production chain instantiation (only 3/9 recipes have industry sites) |
| Faction reputation | ✅ Rep tiers, tariff scaling, neutrality tax, regime matrix, war profiteering | Exclusive supply contracts, faction-specific deal offers |
| Fracture | ✅ Travel costs, trace accumulation (0.5/arrival), rep penalty on detection | Trace doom clock (interdiction, supply shocks). Delayed discovery gating |
| Win conditions | 🔮 Future (Slice 8) | Full endgame system — see factions_and_lore_v0.md aspirational sections |
| Faction equipment sustain | 🔮 Designed (faction_equipment_and_research_v0.md) | 40 T2 modules + 10 signature modules with pentagon dependency sustain. Module sustain enforcement creates mid-game pressure via supply chain dependency. When enforced, warfront embargoes directly degrade your loadout |
| Research system | 🔮 Designed (faction_equipment_and_research_v0.md Part 2) | Three research pillars (Faction Labs, Field Discovery, Haven). Rep gates research access — warfronts erode rep with enemy factions, constraining which tech trees are available. Research agenda IS affected by warfront politics |
| Haven Starbase | 🔮 Designed (haven_starbase_v0.md) | 5-tier upgrade system consuming exotic matter. Competes with T3 module sustain for the same discovery-only resource. Creates an additional resource treadmill: exploration must continue to feed both Haven and endgame equipment |
| Galaxy resource alignment | ❌ Not yet constrained | Each faction's territory must contain resources for their pentagon production good (see trade_goods_v0.md Open Questions). Without this, pentagon ring breaks at world gen |

Most of the simulation infrastructure exists. The priority changes are:
1. **Instantiate remaining production chains** (6 recipes) to enable economic cascades
2. **Fleet fuel/sustain consumption** to create early-game urgency
3. **Starter placement near warfront** to ensure conflict from tick 1
4. **Trace consequences** beyond rep penalty to create the dual doom clock
5. **Module sustain enforcement** to connect T2 equipment to pentagon ring pressure (see `faction_equipment_and_research_v0.md` Part 14)
6. **Galaxy resource alignment** to ensure faction territories support pentagon production

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why It Fails | Our Rule |
|---|---|---|
| **Invisible pressure** | Player does not know why things got harder | Every pressure source is surfaced in UI with cause + 2 mitigations |
| **Unfair death** | Player killed by something they could not see coming | Show the danger. Let the player walk into it willingly |
| **Grind unlocks** | Meta-progression gates content behind hours of replay | All content available in every run. Replay value is strategic, not unlock-based |
| **Binary faction choice** | "Pick A or B" with no middle ground | Neutrality is always viable, just increasingly costly |
| **Punishment without agency** | Bad things happen and the player has no response | Every setback has at least two recovery paths |
| **Linear escalation** | Pressure increases monotonically with no relief | Warfronts ebb and flow. Ceasefires create breathing room. New fronts open as old ones close |

---

## Design Principles (Inherited)

These principles from the broader design docs apply directly:

1. **Every number answers "so what?"** — Not "Tariff: 12%" but "Tariff: 12%
   (Sol Federation war surcharge, expires at ceasefire)."
2. **The player is a Pilot** — You supply warfronts, you do not command them.
   You feel wars through prices, not through tactical combat orders.
3. **Automation feels competent** — When warfront shifts break your trade
   programs, the failure is diagnosable and fixable. Not "program failed" but
   "Trade Charter stalled: Proxima tariff now 25%, margin negative. Reroute to
   Vega? [Yes] [Edit Route]."
4. **Procedural worlds feel distinct** — Different seeds produce different
   warfront puzzles, not cosmetically different versions of the same puzzle.

---

## Open Questions — Status

- ~~How many simultaneous warfronts at game start?~~ **RESOLVED**: 1 hot (Valorin vs Weavers, OpenWar intensity 3) + 1 cold (Concord vs Chitin, Tension intensity 1).
- ~~Should warfront demand shocks be deterministic?~~ **RESOLVED**: Deterministic. Scaled by warfront intensity multipliers in WarfrontTweaksV0.cs.
- ~~What is the exact neutrality tax curve?~~ **RESOLVED**: Stepped at intensity thresholds — Skirmish +500bps, OpenWar +1000bps, TotalWar +1500bps.
- ~~Should the player be able to actively broker ceasefires?~~ **RESOLVED**: Supply-driven only. Cumulative deliveries exceeding threshold reduce warfront intensity by 1.
- How does the Fracture temptation interact with the 5 win conditions? Does
  each win path have a different Trace tolerance? **STILL OPEN** — deferred to Slice 8 endgame design.

---

## Mechanical Specification: Pressure System

> Quantitative specification for how tension is computed, accumulated, and
> enforced. These systems implement the philosophy above as tick-level mechanics.
> See `PressureSystem.cs`, `InstabilitySystem.cs`, `LaneFlowSystem.cs`.

### Pressure Domain Model

The PressureSystem tracks accumulated stress across named **domains** (e.g.,
"trade", "piracy", "warfront"). Each domain has independent state:

```
PressureDomainState
  DomainId: string
  AccumulatedPressureBps: int (0–10000, basis points)
  Tier: PressureTier enum (Normal, Strained, Unstable, Critical, Collapsed)
  Direction: PressureDirection (Stable, Worsening, Improving)
  WindowStartTick: int
  LastTransitionTick: int
  AlertCount: int
  LastConsequenceTick: int
```

### 5-Tier Pressure Ladder

| Tier | Threshold (bps) | Player Impact | Reference |
|------|-----------------|---------------|-----------|
| **Normal** | 0–1999 | No effects. Standard trade conditions. | Stellaris peacetime |
| **Strained** | 2000–3999 | UI warning indicators. FO commentary. | "Margins are tightening" |
| **Unstable** | 4000–6999 | Market price jitter begins. | Starsector supply warning |
| **Critical** | 7000–8999 | +20% market fee surcharge. Crisis alerts. | Stellaris deficit penalty |
| **Collapsed** | 9000–10000 | Piracy escalation injected. Domain crisis. | Starsector 0-CR cripple |

### Pressure Lifecycle

**Injection**: External systems inject deltas into specific domains:
```
PressureSystem.InjectDelta(state, domainId, reasonCode, magnitude)
```
Deltas are clamped to [0, MaxAccumulatedBps] after accumulation.

**CRITICAL: Injection Sources (NOT YET IMPLEMENTED)**

No gameplay system currently calls `InjectDelta` — only the piracy self-cascade
and test code do. The following injection points must be wired:

| Source System | Domain | Reason Code | Magnitude | Trigger |
|--------------|--------|-------------|-----------|---------|
| `WarfrontDemandSystem` | `"warfront"` | `"intensity_change"` | intensity × 500 | Warfront intensity increases |
| `WarfrontDemandSystem` | `"supply"` | `"war_goods_shortage"` | 200 per good at 0 stock | Contested node market depleted |
| `IndustrySystem` | `"supply"` | `"shortfall"` | efficiencyDeficit / 10 | ShortfallEvent emitted |
| `MarketSystem` | `"trade"` | `"embargo_active"` | 300 per active embargo | Embargo blocks trade |
| `FractureSystem` | `"fracture"` | `"trace_detection"` | 1000 | Trace exceeds threshold |
| `NpcTradeSystem` | `"piracy"` | `"patrol_gap"` | 100 | Patrol circuit has >3 uncovered nodes |
| `MaintenanceSystem` | `"supply"` | `"site_critical"` | 500 | Site health below 25% |

**Design Decision**: Pressure injection should happen at NATURAL system boundaries
(where events already emit), not as a separate scanning pass. Each system above
already computes the relevant metric — it just needs to call `InjectDelta` when
thresholds cross.

**Natural Decay**: Every tick, accumulated pressure decays by `NaturalDecayBps` (10 bps).
This means unrefreshed pressure naturally resolves over ~1000 ticks (10000/10).

**Tier Evaluation**: Each tick, the system maps accumulated pressure to a target tier.

**One-Jump Rule**: Tier transitions are rate-limited:
- Max 1 tier jump per enforcement window (50 ticks)
- If already transitioned this window, further jumps are held
- Prevents catastrophic tier collapse from a single spike event

**Direction Tracking**: The system reports whether pressure is worsening (above
tier threshold), improving (below tier lower bound), or stable.

### Consequence Enforcement

| Tier | Consequence |
|------|-------------|
| **Critical** | `CrisisFeeSurcharge` event emitted → +20% market fees (2000 bps) |
| **Collapsed** | `CollapseEscalation` event → injects 500 bps into "piracy" domain |

**Collapsed → Piracy Cascade**: When a domain collapses, it injects pressure into
the piracy domain, which can itself escalate to Critical/Collapsed, creating a
cascading failure. This models how economic collapse breeds lawlessness.

### AAA Comparison (Pressure)

| Game | Pressure Model | Our Analog |
|------|---------------|------------|
| **Stellaris** | Resource deficit → fleet/empire debuffs. Monthly check. | Accumulated bps → tier → fee/piracy |
| **Rimworld** | Colony mood bar. Events push mood down, time restores. | AccumulatedBps decays via NaturalDecayBps |
| **Dwarf Fortress** | Tantrum spiral — stress cascades through population. | Collapsed → piracy injection (cross-domain cascade) |
| **Civilization** | War Weariness accumulates, decays in peace. | EnforcementWindow + NaturalDecay |

---

## Mechanical Specification: Instability System

### Per-Node Instability Model

Every node has an `InstabilityLevel` (0–150) determining its phase:

| Phase | Level Range | Effects |
|-------|-------------|---------|
| **Stable** | 0–24 | Normal trade conditions. Standard lane capacity. |
| **Shimmer** | 25–49 | ±5% price jitter. +10% lane delay. Sensor ghosts. |
| **Drift** | 50–74 | +15% trade route instability. +20% lane delay. Discovery sites shift. |
| **Fracture** | 75–99 | +30% price spikes. +40% lane delay. Lane closures possible. Hostile anomalies. |
| **Void** | 100+ | Markets may fail. Lanes severed. Void entities. Fracture travel required. |

### Instability Evolution (Per-Tick)

```
For each node (sorted by ID):
  if node is warfront-contested:
    gain = BaseGainPerTick (1) × warfront intensity (1-4)
    InstabilityLevel = min(InstabilityLevel + gain, MaxInstability=150)
  else if InstabilityLevel > 0:
    every DecayIntervalTicks (100) ticks:
      InstabilityLevel = max(0, InstabilityLevel - DecayAmountPerInterval=1)
```

**Key Properties**:
- Warfront-contested nodes gain instability proportional to war intensity
- TotalWar (intensity 4) pushes nodes to Void phase in ~25 ticks
- Distant nodes stabilize very slowly (1 point per 100 ticks)
- A node at Void (100) takes 10,000 ticks to fully stabilize after war ends

### Instability Consequences

**Lane Delay Scaling** (LaneFlowSystem integration):
```
maxPhase = max(source phase, destination phase)
delay bonus:
  Shimmer (1): +10%
  Drift (2):   +20%
  Fracture (3): +40%
  Void (4):    lane SEVERED — no transfers possible
```

**Market Volatility**:
```
volatility multiplier = 10000 + (level × VolatilityMaxBps / MaxInstability)
At level 0:   1.0x prices
At level 150: 1.5x prices
```

**Security Demand Skew** (Drift+ only):
```
Fuel and munitions get +SecurityDemandSkewBps (2000) per phase above Shimmer
Phase 2 (Drift):    +2000 bps (+20%)
Phase 3 (Fracture): +4000 bps (+40%)
Phase 4 (Void):     +6000 bps (+60%)
```

### AAA Comparison (Instability)

| Game | Instability Model | Our Analog |
|------|------------------|------------|
| **Stellaris** | War in Heaven / Crisis — galaxy-wide threat escalation | Void phase = regional crisis, not global |
| **Starsector** | Hyperspace storms — temporary route disruption | Shimmer/Drift = trade delay scaling |
| **Sunless Sea** | Terror accumulation in darkness → crew mutiny | InstabilityLevel climbs near warfronts |
| **FTL** | Rebel fleet advancing — time pressure on movement | Void phase severs lanes, forces fracture travel |

---

## Mechanical Specification: Lane Flow System

### Goods-in-Transit Model

The LaneFlowSystem manages deterministic goods transfers between markets. Unlike
fleet-based movement (MovementSystem), these transfers represent background
trade infrastructure — automated supply chains, NPC logistics, and player
automation programs.

### Transfer Lifecycle

1. **Enqueue**: `TryEnqueueTransfer(from, to, goodId, quantity, transferId)`
   - Validates: edge exists, markets exist, no duplicate ID
   - **Void check**: if either endpoint is phase 4 (Void), transfer rejected
   - Removes goods from source market immediately
   - Computes delay: `ceil(edge.Distance)` ticks, minimum 1
   - Applies instability delay bonus (10-40% based on max endpoint phase)

2. **In-Flight**: Transfer exists in `state.InFlightTransfers` until arrival tick

3. **Delivery**: When `ArriveTick <= currentTick`:
   - Delivers up to `edge.TotalCapacity` units per lane per tick
   - Overflow is deterministically deferred to next tick (`ArriveTick = now + 1`)
   - Delivered goods added to destination market inventory

4. **Cleanup**: Fully delivered transfers (quantity = 0) removed from state

### Capacity Enforcement

```
Per lane per tick:
  capacity = edge.TotalCapacity (if > 0)
           = state.Tweaks.DefaultLaneCapacityK (if > 0)
           = unlimited (legacy behavior)

  Transfers processed in order: ArriveTick → EdgeId → TransferId
  When capacity exhausted: remaining transfers deferred to next tick
```

**Sustained overload** creates multi-tick delays via repeated deferrals. This
models traffic congestion — a heavily-used trade lane becomes a bottleneck.

### Lane Utilization Report

Each tick, the system emits a deterministic report:
```
LANE_UTILIZATION_REPORT_V0
tick=N
lane_id|delivered|capacity|queued
edge_001|50|100|0
edge_002|100|100|25    ← congested lane
```

This report feeds the empire dashboard's trade infrastructure health display.

---

## Pressure Constants Reference

All values in `SimCore/Tweaks/PressureTweaksV0.cs`:

```
# Tier Thresholds (bps)
StrainedThresholdBps       = 2000   (20%)
UnstableThresholdBps       = 4000   (40%)
CriticalThresholdBps       = 7000   (70%)
CollapsedThresholdBps      = 9000   (90%)
MaxAccumulatedBps          = 10000  (100%)

# Rate Limiting
MaxTierJumpPerWindow       = 1
EnforcementWindowTicks     = 50
MaxAlertsPerWindowNormal   = 3
MaxAlertsPerWindowCrisis   = 5
NaturalDecayBps            = 10

# Consequences
CrisisFeeIncreaseBps       = 2000   (+20% market fee)
CollapsePiracyEscalationMagnitude = 500
CrisisTierMin              = 3      (Critical)
```

All values in `SimCore/Tweaks/InstabilityTweaksV0.cs`:

```
# Phase Thresholds
StableMin=0, ShimmerMin=25, DriftMin=50, FractureMin=75, VoidMin=100

# Evolution
BaseGainPerTick            = 1      (per intensity level)
DecayAmountPerInterval     = 1
DecayIntervalTicks         = 100
MaxInstability             = 150

# Lane Delay
ShimmerLaneDelayPct        = 10
DriftLaneDelayPct          = 20
FractureLaneDelayPct       = 40

# Market Effects
ShimmerPriceJitterPct      = 5      (±5%)
VolatilityMaxBps           = 5000   (max +50% at level 150)
SecurityDemandSkewBps      = 2000   (per phase above Shimmer)
```

---

## Cross-System Tick Execution Order

`SimKernel.Step()` executes all systems in a fixed order each tick. This ordering
is load-bearing — systems depend on prior systems' writes within the same tick.

```
SimKernel.Step():
  ┌─ PHASE 1: Input Resolution ──────────────────────────────────────
  │  LaneFlowSystem          — resolve in-flight goods arrivals
  │  ProgramSystem            — emit automation intents
  │  IntentSystem             — resolve competing route choices
  │  MovementSystem           — fleet traversal, arrivals
  │  FractureWeightSystem     — cargo weight shift on arrival
  │  SustainSystem            — fuel drain, module sustain cycle
  │  NpcFleetCombatSystem     — destroy NPC fleets
  │  LootTableSystem          — despawn expired loot
  │  JumpEventSystem          — random events on arrival
  │  RiskSystem               — security incidents
  │
  ├─ PHASE 2: Economic Simulation ───────────────────────────────────
  │  LogisticsSystem          — shortage detection, fleet jobs
  │  IndustrySystem           — input→output production, efficiency
  │  StationContextSystem     — per-station economic context
  │  MissionSystem            — mission trigger/advance
  │  SystemicMissionSystem    — world-state mission detection
  │  ResearchSystem           — tech research progress
  │  RefitSystem              — module installation queue
  │  MaintenanceSystem        — health decay, efficiency coupling
  │  PowerBudgetSystem        — power budget enforcement
  │  HavenUpgradeSystem       — station tier progression
  │  ConstructionSystem       — construction step advancement
  │  NpcIndustrySystem (×2)   — NPC demand drain + reaction boost
  │
  ├─ PHASE 3: Intel & Discovery ─────────────────────────────────────
  │  IntelSystem (×3)         — observation, scanner, price history
  │  DiscoveryOutcomeSystem   — rewards on Analyzed phase
  │
  ├─ PHASE 4: Warfront & Pressure ───────────────────────────────────
  │  WarfrontDemandSystem     — war goods consumption
  │  WarfrontEvolutionSystem  — intensity transitions
  │  InstabilitySystem        — per-node instability evolution
  │  TopologyShiftSystem      — edge mutation on arrival
  │
  ├─ PHASE 5: NPC & Security ───────────────────────────────────────
  │  NpcTradeSystem           — NPC trade circulation
  │  SecurityLaneSystem       — security lane updates
  │  EscortSystem             — escort/patrol programs
  │
  ├─ PHASE 6: Pressure & Reputation ─────────────────────────────────
  │  PressureSystem (×2)      — tier transitions + consequence enforce
  │  ReputationSystem         — natural rep decay
  │  WarConsequenceSystem     — narrative war consequences
  │
  └─ PHASE 7: Player Feedback ──────────────────────────────────────
     MilestoneSystem          — milestone evaluation
     FirstOfficerSystem       — FO commentary triggers
     NarrativeNpcSystem       — war faces lifecycle
     KnowledgeGraphSystem     — connection reveals
     FractureSystem (×2)      — fracture gate + goods flow
```

### Critical Ordering Dependencies

| Dependency | Reason |
|------------|--------|
| LaneFlowSystem → IndustrySystem | Goods must arrive at markets before production consumes them |
| MovementSystem → JumpEventSystem | Arrivals must be recorded before jump events fire |
| IndustrySystem → NpcIndustrySystem | Production must run before NPC demand drains |
| WarfrontDemandSystem → WarfrontEvolutionSystem | War goods drain before intensity transitions |
| WarfrontEvolutionSystem → InstabilitySystem | Intensity must be set before instability evolves |
| PressureSystem → MilestoneSystem | Pressure consequences applied before milestone check |
| All systems → KnowledgeGraphSystem | Discovery phases from intel must be final before graph evaluates |

### KNOWN GAP: No Injection Wiring

`PressureSystem.InjectDelta()` is never called by any gameplay system (only tests
and self-cascade). The injection sources table in the Pressure System section above
documents the INTENDED wiring — each row requires a 1-line call added to the source
system. This is the single highest-impact gap for dynamic tension.
