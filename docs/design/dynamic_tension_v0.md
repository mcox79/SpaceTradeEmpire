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

Most of the simulation infrastructure exists. The priority changes are:
1. **Instantiate remaining production chains** (6 recipes) to enable economic cascades
2. **Fleet fuel/sustain consumption** to create early-game urgency
3. **Starter placement near warfront** to ensure conflict from tick 1
4. **Trace consequences** beyond rep penalty to create the dual doom clock

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
