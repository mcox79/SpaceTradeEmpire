# Dynamic Tension — Goal Doc

> Design goal document for making Space Trade Empire compelling and replayable
> through dynamic pressure from tick 1. Companion to `trade_goods_v0.md`,
> `ship_modules_v0.md`, and `EmpireDashboard.md`.

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

The fracture module is **not available at game start.** The player spends
Hours 0-3 (roughly tick 0-300) as a pure lane trader — learning markets,
feeling warfront pressure, upgrading with standard T1 equipment. The module
is discovered at a frontier derelict near the warfront around Hour 3-4, once
the player has internalized the constraints of lane-space.

This delayed discovery is critical:
- The player learns the rules before they can break them.
- Standard equipment creates a baseline — exotic tech is exciting because
  you know what "normal" feels like.
- The warfront is personal before the escape valve appears. By Hour 3, the
  player has lost margins to tariffs, been inspected by patrols, felt the
  squeeze. They WANT an alternative.

Once discovered, Fracture travel becomes the escape valve for warfront
pressure — but using it creates its own doom clock.

- When a warfront closes a critical trade lane, Fracture travel is the only
  way to maintain supply chains.
- But Fracture use accumulates Trace -> eventual interdiction waves, supply
  shocks, lane disruptions.
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
never coasting. The first 3-4 hours are pure lane-space — the player earns
their understanding of the galaxy before the fracture module recontextualizes
it.

---

## Implementation Surface

| Area | Current State | What Changes |
|---|---|---|
| `GalaxyGenerator` | Seeds warfronts (already exists) | Warfronts active from tick 1, starter system placement near a front |
| Sustain/Maintenance | Module sustain exists | Baseline ship fuel consumption, tuned to create ~50-80 tick runway |
| `MarketSystem` | Price model exists | Warfront demand shocks — factions consuming goods at elevated rates during war |
| Faction reputation | Reputation + tariffs exist | Tariff/access scaling with warfront intensity. Neutrality tax curve |
| Fracture | Trace accumulation exists | Fracture as explicit warfront escape valve — closed lanes push player toward it |
| Win conditions | Five defined (Slice 8) | Faction allegiance mid-game feeds into which endgame paths are viable |

Most of the simulation infrastructure exists. The changes are primarily
**tuning and connection** — making existing systems exert pressure from the
start rather than ramping up slowly.

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

## Open Questions

- How many simultaneous warfronts at game start? (1 active + 1 simmering seems
  right for not overwhelming the player.)
- Should warfront demand shocks be deterministic (predictable from faction
  state) or stochastic (random spikes within a range)?
- What is the exact neutrality tax curve? Linear? Exponential? Stepped at
  warfront intensity thresholds?
- Should the player be able to actively broker ceasefires, or are warfront
  outcomes purely supply-driven?
- How does the Fracture temptation interact with the 5 win conditions? Does
  each win path have a different Trace tolerance?
