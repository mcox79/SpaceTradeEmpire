# Mission Design v0 — Space Trade Empire

> "Does this mission require the player to make an interesting decision, or just
> spend time?" If the answer is "just spend time" — cut it.

---

## 1. Philosophy: What We Refuse and What We Demand

### The Five Refusals

1. **No "go to X, get Y, come back" without narrative wrapping.** Every mission
   must have a *reason* rooted in world state — a shortage, a war, a discovery,
   a relationship. If we can't articulate why this mission exists in the fiction
   right now, it doesn't exist.

2. **No missions the player can complete on autopilot.** If no interesting
   decision is required between acceptance and completion, the mission is padding.
   Cut it. A decision can be route choice, cargo tradeoff, faction alignment,
   risk assessment, or information interpretation — but there must be one.

3. **No rewards that become irrelevant.** If passive income ever outscales
   mission rewards, the mission system is dead. Rewards must include things money
   can't buy: reputation, knowledge, access, tech unlocks, Haven upgrades,
   faction-exclusive goods.

4. **No missions disconnected from world state.** The galaxy must look different
   depending on whether the player does the mission or not. Price shifts,
   reputation changes, faction disposition, supply chain effects. If nothing
   changes, the mission is a lie.

5. **No mission boards.** Missions are not a menu. They emerge from
   conversations, discoveries, world events, faction relationships, and economic
   conditions. The player should feel like they found an opportunity, not that
   they picked from a list.

### The Five Demands

1. **Every mission teaches something.** A system, a relationship, a route, a
   danger, a secret. The player should know more about the galaxy after every
   mission than before.

2. **Every mission reveals something.** Lore, faction motivation, economic truth,
   geographic advantage. Even a cargo run should reveal why that cargo matters
   here, now.

3. **Every mission pressures a choice.** Faction alignment, resource allocation,
   risk tolerance, route planning. The player's identity emerges from mission
   choices, not character creation.

4. **Missions respect the player's time.** Short missions that matter beat long
   missions that don't. A 2-minute delivery that shifts a warfront is worth more
   than a 20-minute fetch quest.

5. **Failure is content.** Failed missions produce consequences, not just "try
   again." A failed supply run means the station falls. A missed deadline means
   the faction loses ground. New missions emerge from failure states.

---

## 2. The Four Layers

Missions operate in four layers. Each layer has different authoring cost,
replayability, and emotional weight.

### Layer 1: Systemic Missions (Zero Authoring Cost)

The economy, warfront, and faction systems *generate* missions through their
natural operation. These are not authored — they emerge.

**How it works:** Every tick, the simulation produces conditions that are
mission-shaped. A system scans for actionable conditions and surfaces them as
opportunities at stations, through First Officer commentary, or via faction
contacts.

**Examples of systemic conditions → missions:**

| World Condition | Mission Shape | Decision |
|---|---|---|
| Warfront consumes all munitions at Node X | "Munitions desperately needed at Node X" | Risk running the blockade vs. safer but slower routes |
| Faction embargo blocks electronics to Weavers | "Weavers will pay triple for electronics" | Smuggle through hostile territory vs. respect the embargo |
| Production chain breaks (ore shortage → no metal → no composites) | "Composites production halted — raw ore needed" | Which link in the chain to fix first |
| Discovery site scanned but unanalyzed | "Analysis requires exotic crystals — source them" | Spend resources to learn more vs. move on |
| Edge heat maxed on primary trade route | "Congested route — find alternate path" | Slower safe route vs. fast hot route |
| Haven needs rare metals for tier upgrade | "Haven expansion requires 10 rare metals" | Prioritize home base vs. continue earning |
| NPC patrol destroyed on trade lane | "Lane unpatrolled — merchants at risk" | Escort duty vs. exploit the gap yourself |
| Faction reputation decaying toward hostile | "Standing with Valorin deteriorating" | Repair relations vs. commit to their enemy |

**Key principle:** The player never sees a "mission board." They dock at a
station and the station's *situation* implies the opportunity. Empty market
shelves, faction contacts mentioning shortages, FO observing price anomalies.
Show, don't list.

**Reward:** Market advantage (buy low here, sell high there), reputation gain
with the benefiting faction, trade route intelligence.

### Layer 2: Template Missions (Low Authoring Cost, High Variety)

Authored mission *structures* with procedural *variables* pulled from current
world state. Each template is written once; the simulation fills in the specifics
each playthrough.

**Template anatomy:**

```
TEMPLATE: supply_run_urgent
  TRIGGER_CONDITION: Warfront intensity >= Skirmish at any node
  OFFERED_BY: Faction contact at adjacent station
  CONTEXT: "{FactionName} forces at {WarNode} are running low on {WarGood}."
  OBJECTIVE: Deliver {Qty} {WarGood} to {WarNode} within {TickWindow} ticks
  TWIST_SLOT: [blockade | ambush | price_spike | rival_runner | contraband_mixed]
  REWARD: {Credits} + {FactionRep} + {WarGood}_supplier_permit
  FAILURE: {WarNode} supply tier drops, {FactionName} loses ground, prices spike
  DECISION: Route choice (fast through contested vs. slow through safe space)
```

**The twist slot** is what separates templates from fetch quests. Every template
has 3-5 possible complications drawn from world state:

| Twist | What Happens | Player Decision |
|---|---|---|
| **blockade** | Enemy faction patrols the delivery route | Fight through, bribe, find alternate route |
| **ambush** | Pirates camping the destination | Arrive armed, hire escort, or abort |
| **price_spike** | Goods you're carrying spike in price en route | Deliver as promised (rep) or sell for profit (credits) |
| **rival_runner** | Another trader is running the same goods | Race to arrive first or cooperate |
| **contraband_mixed** | Cargo contains undeclared goods | Declare (lose some, keep rep) or smuggle (keep all, risk rep) |
| **shortage_shift** | Destination no longer needs the goods; new destination does | Reroute for reduced reward or deliver as contracted |
| **intelligence** | FO notices something about the cargo/route/destination | Side objective: investigate anomaly for bonus reward |

**Template categories (15-20 templates per category):**

**Supply & Logistics:**
- Urgent warfront resupply (time pressure + blockade risk)
- Production chain repair (multi-hop: source raw → deliver to factory → collect output)
- Embargo circumvention (smuggle goods past faction patrols)
- Bulk contract (large qty, multiple trips, escalating trust + reward)
- Humanitarian relief (food/medicine to war-disrupted nodes, reputation-heavy reward)

**Exploration & Discovery:**
- Survey expedition (travel to unscanned region, deploy equipment, analyze findings)
- Salvage recovery (retrieve tech from derelict, navigate hazards)
- Lead chase (follow rumor chain across 2-3 systems, each hop reveals more)
- Void site investigation (fracture-space anomaly, high risk, high knowledge reward)
- Cartography contract (visit and scan N unvisited systems, map data as reward)

**Combat & Security:**
- Bounty hunting (eliminate specific hostile NPC fleet, recover proof)
- Lane patrol (protect trade route for N ticks, respond to attacks)
- Escort duty (accompany NPC trader through hostile territory)
- Warfront reinforcement (deliver munitions + fight alongside faction fleet)
- Blockade running (force passage through enemy-controlled chokepoint)

**Reputation & Politics:**
- Trade demonstration (prove profitable route to skeptical faction)
- Diplomatic courier (carry sealed message between faction leaders, learn politics)
- Defector extraction (transport NPC from hostile faction space)
- Market access negotiation (complete trade tasks to earn market permit)
- Intelligence gathering (observe enemy faction movements, report back)

### Layer 3: Faction Storylines (High Authoring Cost, Maximum Impact)

Hand-crafted mission chains that define each faction's identity and push the
player toward endgame alignment. These are the game's narrative backbone.

**Structure:** Each faction offers a storyline of 8-12 missions. The player can
engage with multiple faction storylines simultaneously in early acts, but
late-game missions demand exclusivity — helping Concord's endgame means closing
doors with Communion.

**Faction storyline design principles:**

1. **Each storyline has a thesis.** Concord's thesis: "Order requires sacrifice."
   Valorin's thesis: "Freedom requires risk." The storyline proves and then
   complicates its thesis.

2. **Early missions are welcoming.** All factions feel helpful and reasonable in
   Act 1. The player shouldn't feel like any faction is "the evil one" until
   deep in Act 2.

3. **Mid-game missions reveal uncomfortable truths.** Concord is suppressing
   fracture data. Chitin's Hive makes decisions individuals disagree with.
   Valorin's expansion is destroying ancient sites. Weavers' infrastructure
   depends on exploitation. Communion's patience looks like passivity when
   people are dying.

4. **Late-game missions force a choice.** The player must decide which truth they
   can live with. No faction is right. Each endgame path has real costs.

5. **Storylines interweave.** A Concord mission might send you to a Weaver
   station where you overhear something that changes your understanding of the
   Weaver storyline. Completing a Valorin mission might unlock a Communion
   side-mission.

**Per-faction storyline sketches:**

#### Concord Storyline: "The Weight of Order"

| # | Mission | What Player Does | What Player Learns |
|---|---|---|---|
| C1 | Relief Convoy | Deliver food to war-disrupted system | Concord infrastructure is genuinely humanitarian |
| C2 | Census Run | Visit 3 systems, report economic data | Concord is monitoring everything; data goes somewhere |
| C3 | Suppression Detail | Deliver "calibration equipment" to frontier station | Equipment is actually a signal jammer. What are they jamming? |
| C4 | Stability Audit | Investigate a station reporting anomalous readings | The anomaly is real. Concord orders you to file it as equipment malfunction |
| C5 | Containment Protocol | Seal a fracture vent discovered near trade lane | The vent is natural. Concord is sealing them to prevent discovery, not danger |
| C6 | The Archive | Gain access to Concord data archive | Historical records show Concord has known about fractures for decades |
| C7 | Whistleblower | Choose: leak archive data or bury it | Defines player's Concord relationship: loyalist or dissident |
| C8 | Order's Price | Final mission varies by C7 choice | Loyalist: enforce containment at a cost. Dissident: undermine containment to reveal truth |

#### Chitin Storyline: "The Probability of Us"

| # | Mission | What Player Does | What Player Learns |
|---|---|---|---|
| H1 | Market Optimization | Help a Chitin merchant optimize a failing route | Chitin individuals are warm and thoughtful, not the hive-mind stereotype |
| H2 | Swarm Logistics | Coordinate a multi-ship delivery for Chitin collective | Hive decision-making is elegant but sometimes overrides individual preference |
| H3 | Probability Audit | Gather data from 3 markets for Hive analysis | The Hive's models are extraordinarily accurate — suspiciously so |
| H4 | Outlier Investigation | A Chitin individual's behavior deviates from Hive prediction | Individual Chitin can disagree. The Hive accommodates this, but barely |
| H5 | Model Collapse | Hive probability model fails catastrophically at a node | The fracture module breaks Chitin prediction models. They can't model what they don't know exists |
| H6 | The Dissenter | Help a Chitin individual act against Hive recommendation | The individual was right. The Hive acknowledges this. Tension remains |
| H7 | Collective Choice | Hive asks player to make a decision they can't compute | Reveals the limit of collective intelligence: some choices are personal |
| H8 | Probability and Will | Final mission: help Hive integrate fracture data or protect them from it | Both choices have consequences for Chitin society |

#### Valorin Storyline: "What Lies Beyond"

| # | Mission | What Player Does | What Player Learns |
|---|---|---|---|
| V1 | Frontier Survey | Scout an uncharted system for Valorin expansion | Valorin scouts find things nobody else does — first contact with void space |
| V2 | Claim Staking | Establish Valorin mining presence at discovery site | Valorin expansion is aggressive but effective; they build fast |
| V3 | Relic Recovery | Retrieve ancient artifact from deep exploration site | The artifact is older than the threads. Valorin don't know what it means |
| V4 | Border Skirmish | Defend Valorin mining outpost from Weaver territorial claim | Both sides have legitimate claims. Valorin shoot first |
| V5 | The Deep Signal | Follow an anomalous signal past known space | Signal is accommodation geometry. Valorin don't understand it; they just want to exploit it |
| V6 | Collateral Survey | Return to a system Valorin mined and moved on | Ecological/structural damage from aggressive extraction. Consequences of frontier mentality |
| V7 | The Map's Edge | Escort a Valorin pathfinder to the galaxy's limit | What's beyond the threads isn't nothing — it's everything |
| V8 | Frontier's Cost | Final: push deeper (risk) or consolidate (safety) | Defines Valorin's future: reckless expansion or measured exploration |

#### Weaver Storyline: "Built to Last"

| # | Mission | What Player Does | What Player Learns |
|---|---|---|---|
| W1 | Material Test | Deliver composites to a Weaver construction site | Weaver construction is beautiful and intentional. Every joint matters |
| W2 | Structural Analysis | Scan an ancient structure for Weaver engineers | The ancient builders used similar principles to Weaver craft. Coincidence? |
| W3 | Supply Chain Forge | Help establish a sustainable production pipeline | Weaver infrastructure is designed for centuries, not quarters |
| W4 | The Flaw | A Weaver structure is failing. Investigate | Failure is caused by accommodation geometry shift. Their materials work because they accidentally mirror ancient design |
| W5 | Reverse Engineering | Bring accommodation fragment to Weaver lab | Weavers recognize the geometry. Their entire tradition is an echo of something older |
| W6 | The Commission | Build something using accommodation-informed design | The result works better than anything modern. Weavers are shaken |
| W7 | Legacy or Innovation | Weavers split: preserve tradition or adopt accommodation methods | Both paths have merit. Tradition is identity. Innovation is survival |
| W8 | Enduring Work | Final: build infrastructure that bridges old and new | Defines Weavers' relationship with the ancient truth |

#### Communion Storyline: "The Long Listening"

| # | Mission | What Player Does | What Player Learns |
|---|---|---|---|
| U1 | Shimmer Walk | Visit a meditation site with a Communion guide | Communion experiences spacetime variance directly. It's not mysticism — it's perception |
| U2 | Fragment Reading | Bring a discovery fragment to a Communion sage | They recognize the module's signature. They've seen "threshold-crossers" before |
| U3 | The Vigil | Maintain a watch post at a shimmer zone for N ticks | Patient observation reveals patterns invisible to scanning. Time passes. Something changes |
| U4 | Lost Crosser | Find traces of a previous threshold-crosser who didn't survive | The module's history is longer than anyone admits. Most users don't last |
| U5 | The Question | Communion asks player why they use the module | No right answer. The question matters more than the response |
| U6 | Resonance Site | Travel deep into fracture space to a site the Communion reveres | The instability isn't chaos. It's the natural state of spacetime. The threads suppress it |
| U7 | The Old Agreement | Discover evidence of the original compact between builders and metric | The threads weren't imposed. They were negotiated. And the negotiation is expiring |
| U8 | Renegotiation | Final: attempt contact with the metric variance itself | The most alien, most transformative ending. Not combat, not engineering — understanding |

### Layer 4: Milestone Missions (Medium Cost, Progression Gates)

One-time missions that mark major game progression moments. Not faction-specific
but critical to pacing.

| Milestone | Trigger | Mission | Unlocks |
|---|---|---|---|
| First Profit | Complete first trade | FO comments on the route | Mission awareness (systemic missions start appearing) |
| Warfront Contact | Enter warfront-adjacent system | Witness supply convoy under attack | Warfront mission templates unlock |
| Fracture Discovery | Research fracture module | Test the module on a short jump | Fracture-space exploration, Act 2 missions |
| Haven Discovery | Follow breadcrumbs (Communion rep or exploration) | Power up the ancient starbase | Home base, Haven upgrade chain |
| Pentagon Revelation | Complete trades that break the ring via fracture | FO connects the pattern | Understanding of economic control, Act 3 pressure |
| Endgame Threshold | Reach Act 3 tick + sufficient faction rep | All factions request exclusive commitment | Final faction storyline missions unlock |

---

## 3. Trigger System Evolution

The current mission system supports three trigger types:
- `ArriveAtNode` — Player reaches a location
- `HaveCargoMin` — Player has N units of a good
- `NoCargoAtNode` — Player has no cargo of type X at a location

This is insufficient for best-in-class missions. The trigger system must expand
to support the mission designs above.

### Required New Trigger Types

| Trigger | Parameters | Use Case |
|---|---|---|
| `ReputationMin` | FactionId, MinRep | Gate missions behind faction standing |
| `ReputationMax` | FactionId, MaxRep | "Your standing with X has dropped below Y" |
| `CreditsMin` | Amount | Gate behind economic threshold (Freelancer pattern) |
| `TechUnlocked` | TechId | Gate behind research completion |
| `DiscoveryPhase` | DiscoveryId, Phase | Gate behind exploration progress |
| `WarfrontIntensity` | NodeId, MinIntensity | Trigger on war escalation |
| `NpcDestroyed` | FleetId or FleetRole | Bounty completion |
| `TimerExpired` | TickDeadline | Urgency / failure condition |
| `ChoiceMade` | ChoiceId, OptionId | Branch on previous player decision |
| `HavenTier` | MinTier | Gate behind home base progression |
| `MissionFailed` | MissionId | Unlock failure-consequence missions |
| `CargoValue` | MinValue | "Carry at least X credits worth of goods" |
| `EdgeTraversed` | EdgeId | "Travel through this specific lane" |
| `EscortAlive` | FleetId | Escort target survived |

### Branching Steps

Current steps are linear (0 → 1 → 2 → done). Missions need conditional paths:

```
Step 0: ArriveAtNode {destination}
Step 1: CHOICE "The buyer offers two deals"
  → Option A: Sell at market price (safe, +credits, +1 rep)
    Step 2A: NoCargoAtNode {destination} {good}
    Step 3A: COMPLETE (credits + rep reward)
  → Option B: Hold for the warfront premium (risky, +big credits, +3 rep, possible ambush)
    Step 2B: ArriveAtNode {warfront_node}
    Step 3B: [TWIST: ambush check]
    Step 4B: NoCargoAtNode {warfront_node} {good}
    Step 5B: COMPLETE (big credits + rep + intel reward)
```

**CHOICE steps** present 2-3 options. Each option leads to a different step
sequence. This is where missions stop being fetch quests and become stories.

### Failure Conditions

Missions can fail. Failure is not "try again" — it's "the world changed."

| Failure Type | Condition | Consequence |
|---|---|---|
| **Timeout** | TickDeadline exceeded | Supply didn't arrive; station situation worsens |
| **Escort death** | EscortAlive fails | NPC died; faction blames player, -rep |
| **Abandonment** | Player explicitly abandons | Reputation penalty, mission locked for N ticks |
| **Betrayal** | Player sells mission cargo for personal profit | Faction trust permanently damaged |
| **Combat loss** | Player fleet destroyed during mission | Mission auto-fails; respawn penalty |

Failed missions produce **consequence missions**: new opportunities born from
the failure. The station that didn't get supplies now needs evacuation. The dead
escort's faction wants an investigation. The betrayed merchant spreads your
reputation.

---

## 4. Mission Offer System: No Boards, Just Situations

Missions surface through four channels, never through a menu:

### Channel 1: Station Context

When the player docks, the station's current situation implies opportunities.

- **Empty market shelves** → "This station needs {good}. {FactionContact} is
  offering premium rates."
- **Damaged infrastructure** → "Station repairs require {composites/rare_metals}.
  Hazard pay available."
- **Refugee influx** → "War evacuees need transport to {safe_node}. Humanitarian
  credits offered."
- **Research lab active** → "Lab needs {exotic_matter} to continue project.
  Priority contract available."

The dock menu's existing tab structure (Market / Jobs / Services) becomes the
natural surface. The **Jobs tab** shows 0-3 contextual opportunities based on
station state — not a random board.

### Channel 2: First Officer Commentary

The FO observes conditions and makes suggestions. This is not a quest marker —
it's a companion's opinion.

- *Analyst*: "Price differential on electronics between here and {node} is 3.2
  standard deviations above mean. Statistically anomalous."
- *Veteran*: "I served near {warfront_node} once. They'll be burning through
  munitions. Might be worth a run."
- *Pathfinder*: "The shimmer readings from {discovery_node} are unlike anything
  in the charts. Something's there."

FO comments are **not mission acceptance prompts.** They are atmospheric
observations that imply opportunities. The player decides whether to act.

### Channel 3: Faction Contacts

As reputation grows, faction NPCs at stations begin offering direct missions.
These escalate with trust:

| Rep Tier | Contact Behavior |
|---|---|
| Neutral | Generic: "Traders welcome. Standard rates." |
| Friendly (+25) | Personal: "We remember your last delivery. We have a priority contract." |
| Allied (+50) | Confidential: "This doesn't leave this room. We need someone we trust." |

Faction contacts are the gateway to **Layer 3 storyline missions.** They appear
only at faction-controlled stations and only when reputation qualifies.

### Channel 4: Discovery & Exploration

The knowledge graph and discovery system generate investigation missions:

- **Scanned site reveals lead** → "Analysis suggests a related site in {system}.
  Worth investigating."
- **Data log fragment** → "This log references coordinates. Cross-referencing
  with known systems..."
- **Derelict recovery** → "Salvaged tech contains encrypted route data. Could
  lead to something."

These missions are **self-directed.** The player follows breadcrumbs, not
assignments. The knowledge graph connects discoveries into chains that the player
assembles.

---

## 5. Reward Architecture

Credits alone are not sufficient. Rewards must include things the player cannot
get any other way.

### Reward Types

| Type | Description | Why It Matters |
|---|---|---|
| **Credits** | Direct payment | Survival fuel, especially early game |
| **Reputation** | Faction standing change | Gates market access, tariff rates, storyline missions |
| **Market Intel** | Price data for distant systems | Information advantage = profit advantage |
| **Trade Permits** | Access to restricted markets/goods | Opens new trade routes |
| **Tech Unlock** | Research prerequisite satisfied | Progression gate cleared without grinding |
| **Discovery Lead** | Knowledge graph connection revealed | Points toward new exploration content |
| **Haven Resources** | Materials for Haven tier upgrades | Home base progression |
| **Ship Access** | Faction-exclusive ship class purchasable | New capabilities |
| **Module Access** | Faction-exclusive module purchasable | Specialization options |
| **Lore Fragment** | Data log, ancient record, faction secret | Knowledge-as-progression (narrative reward) |
| **Route Intelligence** | Safe route through hostile territory | Practical navigation advantage |
| **Contact Upgrade** | Faction contact becomes more open | Future missions become available |

### Reward Scaling Philosophy

| Game Phase | Primary Reward | Secondary Reward | Why |
|---|---|---|---|
| Act 1 (0-400 ticks) | Credits + Market Intel | Reputation | Player needs money to survive and information to earn |
| Act 2 (400-1200) | Reputation + Access | Tech Unlocks + Lore | Player has money; needs relationships and progression |
| Act 3 (1200+) | Lore + Haven + Exclusive | Ship/Module Access | Player has everything except understanding and endgame tools |

**Critical rule:** Mission credit rewards must always exceed what the player
could earn trading during the same time. If a mission takes 50 ticks and trading
earns 500 credits in 50 ticks, the mission must pay 800+ credits. Missions
compete with the core loop for player attention; they must win on total value
(credits + non-credit rewards).

---

## 6. Faction Mission Identity

Each faction's missions must *feel* different — not just in content but in
structure, pacing, and emotional texture.

### Concord: Institutional, Methodical, Controlled

- **Pacing:** Steady. Clear objectives, defined parameters, predictable rewards.
- **Structure:** Sequential steps with documentation. "Report to X, receive
  briefing, execute, debrief."
- **Emotional arc:** Competence → unease. Everything works until you notice
  what's being hidden.
- **Twist tendency:** Information suppression. The mission succeeds but the
  player learns something Concord didn't want them to know.
- **Signature reward:** Access credentials, data archives, infrastructure permits.
- **Voice:** Bureaucratic precision. "Contract terms are non-negotiable."

### Chitin: Analytical, Collaborative, Emergent

- **Pacing:** Parallel. Multiple sub-objectives that can be completed in any
  order.
- **Structure:** Data-gathering → synthesis → action. "Observe these three
  markets, report patterns, act on findings."
- **Emotional arc:** Curiosity → discomfort. Collective intelligence is impressive
  until you see what it costs individuals.
- **Twist tendency:** Model failure. The Hive's prediction was wrong; the player
  must improvise.
- **Signature reward:** Probability data (market predictions), optimization
  algorithms, Hive trade network access.
- **Voice:** Precise, warm, slightly alien. "The collective assesses 73%
  probability of favorable outcome."

### Weavers: Patient, Crafted, Purposeful

- **Pacing:** Deliberate. Missions involve preparation, material gathering, and
  careful execution.
- **Structure:** Blueprint → source materials → construct → test. "Study the
  design, gather what's needed, build it right."
- **Emotional arc:** Satisfaction → revelation. Building feels good until you
  realize why it works.
- **Twist tendency:** Material resonance. A component behaves unexpectedly
  because it mirrors accommodation geometry.
- **Signature reward:** Construction capabilities, material science knowledge,
  structural blueprints, Haven upgrade components.
- **Voice:** Deliberate, craft-proud. "Measure twice. The structure will outlast
  us both."

### Valorin: Direct, Bold, Consequential

- **Pacing:** Fast. Go now, decide on arrival, deal with consequences later.
- **Structure:** Deploy → encounter → react. "We need someone at these
  coordinates yesterday."
- **Emotional arc:** Excitement → accountability. The frontier is thrilling until
  you see what's been left behind.
- **Twist tendency:** Collateral discovery. The mission target is adjacent to
  something ancient and fragile.
- **Signature reward:** Frontier coordinates, exploration data, combat modules,
  salvage rights.
- **Voice:** Military-casual, confident. "No plan survives first contact. That's
  what makes it fun."

### Communion: Patient, Observational, Transformative

- **Pacing:** Slow. Missions involve waiting, watching, and interpreting.
- **Structure:** Arrive → observe → wait → understand. "Go to this place. Be
  still. Tell us what you notice."
- **Emotional arc:** Skepticism → awe. Meditation seems pointless until the
  shimmer resolves into meaning.
- **Twist tendency:** Perception shift. What the player thought they were looking
  for isn't what they find.
- **Signature reward:** Perception upgrades, fracture navigation data, shimmer
  zone maps, ancient context, module resonance.
- **Voice:** Gentle, measured, occasionally unsettling. "You've been watched
  since you arrived. Not by us."

---

## 7. Act Structure: Missions as Narrative Engine

### Act 1: The Rules (Tick 0-400)

**Player state:** Broke, confused, learning.
**Mission purpose:** Teach systems through doing. Build faction familiarity.

**Mission availability:**
- 3-4 systemic missions at any station (supply runs, delivery contracts)
- 2-3 template missions per dock (contextual to station state)
- Faction storyline missions C1/H1/V1/W1/U1 available at faction stations once
  reputation reaches Friendly (+25)

**Pacing rule:** Maximum 1 active mission. Player is still learning the core
trade loop. Missions supplement trading, they don't replace it.

**Key missions:**
- **First Trade** (milestone): FO observes first profitable route
- **Warfront Witness** (milestone): Player enters contested system, sees combat
- **Faction Introduction** (storyline entry): First faction mission for any
  faction reaching Friendly rep

### Act 2: The Escape (Tick 400-1200)

**Player state:** Established trader, fracture module discovered, Haven found.
**Mission purpose:** Reveal faction complexities. Introduce fracture-space
content. Haven progression.

**Mission availability:**
- Systemic missions now include fracture-space opportunities
- Template missions include escort, investigation, and faction-political types
- Faction storylines C2-C5/H2-H5/V2-V5/W2-W5/U2-U5 gated by reputation and
  Act 2 trigger
- Haven upgrade missions unlock with each tier

**Pacing rule:** Up to 2 concurrent missions (one systemic/template + one
storyline). Player has enough mastery to multitask.

**Key missions:**
- **Fracture First Jump** (milestone): Test the module
- **Haven Arrival** (milestone): Cinematic first dock at Haven
- **Pentagon Glimpse** (storyline): Cross-faction trade via fracture reveals
  economic dependency pattern
- **Faction Uncomfortable Truth** (storyline): Each faction's mid-storyline
  reveal (C4, H4, V4, W4, U4)

### Act 3: The Truth (Tick 1200+)

**Player state:** Wealthy, connected, informed. Choosing sides.
**Mission purpose:** Force commitment. Reveal endgame paths. Build toward
final choice.

**Mission availability:**
- Systemic missions reflect escalating warfront pressure
- Template missions include high-stakes warfront supply, fracture-space
  deep exploration, diplomatic crises
- Faction storylines C6-C8/H6-H8/V6-V8/W6-W8/U6-U8 — late missions
  demand exclusivity
- Haven tier 4-5 missions unlock endgame capabilities

**Pacing rule:** Up to 3 concurrent missions. Galaxy is in crisis; the player
is a major actor. Missions overlap and interact.

**Key missions:**
- **Pentagon Revelation** (milestone): Full understanding of the engineered
  dependency ring
- **Faction Commitment** (storyline): Point of no return — exclusive alignment
- **Endgame Path** (milestone): Choose Reinforce / Naturalize / Renegotiate
- **Final Mission** (storyline): Faction-specific culmination

---

## 8. Procedural Generation Rules

Template missions are procedurally instantiated but must follow strict quality
rules to avoid the Elite Dangerous trap.

### Rule 1: Context Binding

Every template variable must resolve from current world state, not random:

```
BAD:  "Deliver {random_good} to {random_station}"
GOOD: "Deliver {good_with_highest_shortage_at_dest} to {station_with_lowest_supply}"
```

The player should be able to look at the mission and think "yes, that makes
sense given what I know about this system."

### Rule 2: Decision Density

Every mission of 3+ steps must contain at least one decision point. Decisions
can be:
- **Route choice:** Two valid paths with different risk/reward
- **Resource allocation:** Spend resources now (easier) or save them (harder but
  more profitable)
- **Faction alignment:** Action benefits one faction at another's expense
- **Information use:** Use discovered intel for mission advantage or sell it
- **Risk assessment:** Take the safe approach or gamble for better reward

### Rule 3: Twist Probability

60% of template missions should generate with a twist. The twist must be:
- **Discoverable before commitment** (30% of twists): Player sees warning signs
  and can prepare or abort
- **Revealed mid-mission** (50% of twists): Complications emerge after commitment
  but with player agency to respond
- **Revealed at completion** (20% of twists): Outcome differs from expectation,
  setting up follow-on content

### Rule 4: Reward Proportionality

```
mission_reward = base_value * (1 + risk_multiplier + urgency_multiplier + distance_multiplier)
```

Where:
- `base_value` = current market value of equivalent free-trade activity
- `risk_multiplier` = 0.2 per hostile territory hop, 0.5 for warfront destination
- `urgency_multiplier` = 0.3 for timed missions
- `distance_multiplier` = 0.1 per hop beyond 2

This ensures missions *always* outpay equivalent free trading.

### Rule 5: Spawn Rate Control

| Station Type | Max Missions Available | Refresh Rate |
|---|---|---|
| Backwater | 1 | Every 200 ticks |
| Trade hub | 2-3 | Every 100 ticks |
| Warfront-adjacent | 2-3 (combat-heavy) | Every 50 ticks |
| Faction capital | 1-2 (storyline) + 1-2 (template) | Storyline: permanent. Template: every 100 ticks |
| Haven | 1-2 (Haven-specific) | Per tier unlock |

Fewer missions, each meaningful, beats a long list of filler.

### Rule 6: No Duplicates

The generator must not offer:
- Two missions to the same destination simultaneously
- Two missions requiring the same good simultaneously
- A mission that duplicates a currently active mission's objective
- A mission whose reward is less than 80% of the most recent completed mission's
  reward (anti-regression)

---

## 9. Quality Gates: The Mission Litmus Tests

Every mission — hand-crafted or procedural — must pass all five tests before
shipping.

### Test 1: The Decision Test

> "What interesting decision does this mission require?"

If the answer is "none — the player just goes there and does the thing," the
mission fails. Add a decision point or cut the mission.

### Test 2: The Knowledge Test

> "What does the player know after this mission that they didn't know before?"

If the answer is "nothing — just credits," the mission fails. Add a revelation
(economic, geographic, factional, lore) or cut the mission.

### Test 3: The World Test

> "What changes in the galaxy because of this mission?"

If the answer is "nothing — the player's wallet changed," the mission fails.
Add a world-state consequence (price shift, reputation change, supply chain
effect, faction disposition) or cut the mission.

### Test 4: The Autopilot Test

> "Can the player complete this while watching Netflix?"

If yes, the mission fails. Add engagement requirements (combat, routing,
decision, timing) or cut the mission.

### Test 5: The Dinner Test

> "Would the player describe this mission to a friend over dinner?"

If no, the mission is forgettable. Elevate the stakes, add a twist, or make the
outcome more surprising. The best missions become stories the player tells.

---

## 10. Implementation Priority

### Phase 1: Foundation (Current → Near-term)

Expand the trigger system to support the new trigger types. This is the
prerequisite for everything else.

- Add `ReputationMin`, `CreditsMin`, `TechUnlocked`, `TimerExpired` triggers
- Add mission failure/abandonment
- Add non-credit rewards (reputation, access, intel)
- Add CHOICE steps (branching)
- Migrate existing 10 missions to new system

### Phase 2: Systemic Layer

Build the mission generator that reads world state and surfaces opportunities.

- Market shortage detector → supply mission generation
- Warfront state reader → combat/logistics mission generation
- Discovery state reader → exploration mission generation
- Station context system (dock shows situation, not board)
- FO mission commentary hooks

### Phase 3: Templates

Author the template library.

- 15-20 Supply & Logistics templates
- 10-15 Exploration & Discovery templates
- 10-15 Combat & Security templates
- 10-15 Reputation & Politics templates
- Twist slot system with world-state-driven selection
- Reward scaling formula

### Phase 4: Faction Storylines

Author the hand-crafted faction mission chains.

- Concord 8-mission chain
- Chitin 8-mission chain
- Valorin 8-mission chain
- Weaver 8-mission chain
- Communion 8-mission chain
- Cross-faction interweave points
- Endgame path branching

### Phase 5: Polish

- FO commentary per mission archetype per FO personality
- Mission consequence propagation (failure → new missions)
- Haven mission integration
- Milestone mission cinematics
- Knowledge graph mission integration

---

## Appendix A: Current System Baseline

For reference, here is what the mission system supports today:

| Feature | Status |
|---|---|
| Linear step chains | Implemented |
| 3 trigger types (ArriveAtNode, HaveCargoMin, NoCargoAtNode) | Implemented |
| Prerequisite chains | Implemented |
| Binding tokens ($PLAYER_START, $ADJACENT_1, $MARKET_GOOD_1) | Implemented |
| Credit-only rewards | Implemented |
| Single active mission | Implemented |
| Deterministic event log | Implemented |
| Save/load persistence | Implemented |
| SimBridge query methods | Implemented |
| 10 authored missions | Implemented |
| Mission failure | Not implemented |
| Branching steps | Not implemented |
| Non-credit rewards | Not implemented |
| Procedural generation | Not implemented |
| Faction storylines | Not implemented |
| Twist system | Not implemented |
| Timer/deadline triggers | Not implemented |
| Reputation/tech/discovery triggers | Not implemented |
| Mission UI panel | Not implemented |
| FO mission commentary | Not implemented |
| World-state consequences | Not implemented |

## Appendix B: Inspiration Sources

| Game | What to Learn | What to Avoid |
|---|---|---|
| **Starsector** | Systems generate missions; every trip is an expedition | — |
| **Freelancer** | Story gates + sandbox freedom between; 13 missions feel epic | — |
| **EV Nova** | Exclusive faction paths create identity and replayability | — |
| **FTL** | Compressed dilemmas with real consequences; scarcity = stakes | — |
| **Stellaris** | Event chains as scaffolding; multi-approach resolution | Event spam fatigue |
| **Elite Dangerous** | — | Random mission boards, disconnected from world, no consequences |
| **X4** | — | Opaque objectives, missions obsoleted by passive income |
| **No Man's Sky** | — | Quest state confusion, repetitive structures |
| **CDPR (Witcher 3 / Cyberpunk)** | Reject 90% of pitches; "play, show, tell" hierarchy | AAA scope expectations |

## Appendix C: Mission Content Budget

Estimated content scope for a complete mission system:

| Layer | Count | Authoring Effort | Replayability |
|---|---|---|---|
| Systemic missions | Infinite (generated) | Zero per instance | High (world-state-driven) |
| Template missions | 50-65 templates | 2-4 hours per template | Medium (variable slots) |
| Faction storylines | 40 missions (5 factions x 8) | 8-16 hours per chain | High (exclusive paths = 5x replay) |
| Milestone missions | 6-8 | 4-8 hours each | Low (one-time) |
| **Total authored** | **~100-115 templates + missions** | **~300-500 hours** | **Per playthrough: ~30-40 missions seen** |

The player sees approximately 30-40 missions per playthrough (6-8 milestones +
8 faction storyline + 15-25 systemic/template). With 5 exclusive faction paths,
full content exhaustion requires 3-5 playthroughs.
