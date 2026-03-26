# First Officer Trade Manager — Design Spec v0

> **Status**: DRAFT v6
> **Date**: 2026-03-25
> **Supersedes**: `AutomationPrograms.md` (program configuration model)
> **Companion to**: `fo_commentary_v0.md` (FO personality),
> `fleet_logistics_v0.md` (upkeep/sustain), `economy_simulation_v0.md` (economy),
> `haven_starbase_v0.md` (Haven tiers / hangar / construction),
> `warfront_mechanics_v0.md` (warfront intensity / supply shift),
> `ExplorationDiscovery.md` (discovery-as-trade-intelligence, automation graduation),
> `dynamic_tension_v0.md` (five pillars, fracture temptation, dual doom clock),
> `NarrativeDesign.md` (12 narrative principles, player-assembled meaning),
> `factions_and_lore_v0.md` (pentagon ring, ancient civilization, endgame paths)

---

## Why This Doc Exists

The current automation system (8 program types, manual configuration) is
functionally complete but experientially wrong. Players configure programs
through parameter forms — source market, destination market, good ID,
cadence ticks, quantity. This is spreadsheet work, not gameplay.

The First Officer already exists as a rich narrative companion (3 archetypes,
78+ dialogue lines, 5 progression tiers, blind spots, endgame branching). But
the FO has **zero mechanical impact** — pure commentary. The most interesting
character in the game does nothing.

This document redesigns the automation system around a single insight:

> **The FO is the automation system.**

The player doesn't configure programs. They explore the galaxy — flying
routes, discovering markets, uncovering ancient sites, gathering intelligence.
The FO observes, learns, and builds the trade empire behind them. The player's
role is explorer-strategist. The FO's role is logistics officer.

> **The player is Han Solo, not Emperor Palpatine.** (economy_simulation_v0.md)

The trade empire grows as a CONSEQUENCE of exploration, not as a parallel
management activity. The player barely opens the empire dashboard. Their
attention is on the galaxy — the mystery, the factions, the frontier.

---

## The Core Identity

### What the Player Does

The player **explores**. They fly to new systems, discover resources, follow
anomaly chains, uncover ancient sites, navigate warfronts, build faction
relationships, and piece together the central mystery of the thread builders
and the fracture module.

Every node they visit, every site they scan, every faction they engage with
generates intelligence that the FO turns into trade infrastructure. The
player's exploration IS the economy.

### What the FO Does

The FO **exploits**. They take the intelligence the player gathers and build
a trade network: chartering haulers, optimizing routes, managing sustain
logistics, responding to warfront disruptions. The FO handles everything the
player demonstrated or discovered, communicating in the personality the player
chose.

### The Centaur Model

Neither succeeds alone. The FO can't explore — they can't fly the ship, can't
interpret ancient technology, can't make the moral choices. The player can't
manually manage 15 trade routes while exploring fracture space. Together,
they're more capable than either alone.

> This is the centaur pattern from chess (human + AI beats both alone):
> human provides judgment and access, AI provides execution and analysis.

---

## Industry Reference

| Game | Model | What Works | What Fails |
|------|-------|------------|------------|
| **Factorio** | Player builds, factory runs | Pain before relief. Automation is the tool for the next problem. | No character. |
| **Factorio Space Age** | Rocket launch = beginning of arc 2 | "End of automation setup" opens planetary exploration. Universally praised. | N/A (validates our structure). |
| **Starsector** | Officers level up. Colonies fed by exploration loot. AI Cores as automation multipliers. | AI Cores from ruins slot into colonies as visible named upgrades. Officer-to-administrator pipeline. The "flip moment" when income goes positive. | Raid loop becomes a chore. Colony management is disconnected from exploration. |
| **X4: Foundations** | Satellite networks enable automated traders. | Automation bounded by player's exploration footprint. "Deploy satellite = unlock trade in this sector." | Endgame = spreadsheet monitoring. Ship loss has no notification. |
| **Outer Wilds** | Knowledge is the only progression. | Ship Log as knowledge inventory. Every discovery enables action. No grinding. | No economy (by design). |
| **Subnautica** | Scanning fragments unlocks blueprints. Infrastructure enables deeper exploration. | Thermal plant at dangerous vent powers base for deeper dives. Exploration reward = capability expansion. | Almost no economic automation. |
| **Sunless Sea** | Port Reports as trade intel. Economy funds the journey. | Journey IS the content. Economic scarcity creates narrative agency. Goods are narrative objects. | Small scale. |
| **Stellaris** | Science ships survey, find anomalies and precursor chains. | Triple-reward exploration: strategic info + narrative content + mechanical unlock. | Exploration dies once map is known. Mid-game doldrums. |
| **RimWorld** | Wealth accumulation IS the doom clock. Named characters with cascading consequences. | Prosperity creates adversity. Every crisis involves named characters → personal stories. | N/A (different genre). |
| **Distant Worlds 2** | Full empire delegation with advisors. | "Automatic but show me" supervision mode. | Advisors are tonally blank. Player has nothing to do at full automation. |
| **Victoria 3** | Laws as leverage points, not direct commands. | Player sets conditions; system executes. Interest groups create emergent politics. | Delegation confused with abstraction. Late-game gridlock. |
| **Persona 5** | Confidant ranks unlock thematically coherent abilities. | Doctor confidant → better medicine. Journalist → stealth. Unlock feels narratively logical. | Mid-tier ranks feel underpowered. |
| **Hades** | Boon relationships affect mechanical power. | Narrative relationship IS the delivery mechanism for upgrades. Duo Boons from combined relationships. | Front-loaded — nothing new after max Affinity. |
| **Old World** | Council characters with Desires, relationships, delegation. | Characters surface personal requests during autonomous operation. Death anxiety for key characters. | Relationship UI is opaque. |
| **Offworld Trading Company** | Price movements as communication channel. | Players "read" strategy through price signals. Economy IS a language. | Multiplayer only. |

### The Gap None of Them Fill

No game combines **character relationship** with **trade automation** with
**exploration-driven intelligence** with **narrative mystery delivery through
economic data**. STE's FO Trade Manager fills this gap: a named character who
builds your trade empire from what you discovered, whose economic data reveals
the game's central mystery, and whose relationship with you deepens through
shared experience.

---

## Core Design Principles

### 1. Exploration Drives Everything

Every discovery feeds the FO's automation (ExplorationDiscovery.md Principle
#6). The player explores → discovers nodes, resources, ancient sites →
the FO evaluates what was found → builds trade infrastructure → revenue
funds better exploration equipment → player reaches further → cycle repeats.

The FO should acknowledge this cycle explicitly: *"Those scanner upgrades
from the Drift site already improved Route Gamma's margin by 8%. Keep
exploring — it pays for itself."*

**Hard exploration dependency**: The FO's most capable operations require
resources, route maps, or faction contacts found ONLY through exploration.
If the FO can run on known systems alone, the player stops exploring.

### 2. The Player Teaches by Playing

The player doesn't open a "Create Program" screen. They play the game — fly
routes, discover markets, make trades. The FO observes and offers to take over
what the player has demonstrated.

**The manual trade IS the template.** When the player buys food at Node A and
sells it at Node B, the FO records the route, the good, the spread, and the
conditions. Then offers: *"I can run this one, Captain."*

**Teaching Replay**: When the FO offers to take over, it confirms what it
learned: *"I noticed you buy when the spread is above 20% and sell within
two hops. You avoid that station when Chitin tariffs are up. I'll use those
patterns."* The player can correct: *"Actually, don't avoid it — the tariffs
are worth the access to electronics."*

This prevents the Black & White problem of opaque AI learning and makes
teaching feel like a conversation, not a hand-wave.

### 3. The FO Is Peripheral, Not Central

The player's attention should be on the galaxy — exploration, mystery, combat,
factions. The FO surfaces RARELY, only when something genuinely needs the
player's input. Think: Subnautica's PDA (brief, contextual, non-intrusive)
crossed with a character you care about.

**Exception surfacing, not operation reporting.** The FO never says "Route
Alpha earned 80cr this cycle." The FO says "Route Alpha is stalling — Chitin
tariffs. Want me to reroute or wait it out?" If the FO is reporting routine
operations, the design has failed.

### 4. One Decision at a Time, When It Matters

No configuration screens. No parameter forms. The FO presents decisions at
natural moments with clear tradeoffs:

- After a profitable trade: "Want me to run this?"
- During a warfront: "Here's what changed, here are your options"
- When sustain is threatened: "Your scanner needs rare_elements. Options..."

**Notification triage:**

| Tier | When | Format | Example |
|------|------|--------|---------|
| **BRIEFING** | Every 100-200 ticks | Contextual dialogue, batched | "3 routes profitable, 1 stalling. One thing needs you." |
| **ALERT** | Event-driven (rare) | Short toast | "Sustain critical: scanner offline in 1 cycle" |
| **LOG** | Continuous | Dashboard only (player checks when they want) | Route performance, fleet status |

**Dashboard integration**: The LOG tier lives on the **EmpireDashboard
Fleet tab** — a single screen showing: active routes (map overlay with
ship dots), fleet roster (named ships, status, veteran badge), network
P&L (revenue vs costs), and FO competence indicator. The player opens
this when they're curious, not when forced. Empire fleet ships are
**visible on the galaxy map** as small icons moving along their routes —
the player should see their empire in the world, not just in a menu.
This serves both trust calibration (you see what the FO is doing) and
visual satisfaction (your empire is alive in the galaxy).

### 5. FO Personality Shapes Flavor, Not Strategy

**Critical design constraint:** The FO choice must NOT be a hidden test. The
player picks their FO during the tutorial based on personality appeal — they
have zero information that this will affect trade. All three FOs must reach
the same economic endpoint through different-feeling paths.

| Aspect | Maren (Analyst) | Dask (Veteran) | Lira (Pathfinder) |
|--------|----------------|----------------|-------------------|
| **Data presentation** | Numbers-first: "31% margin, 89% confidence" | Experience-first: "Solid route, I've seen these hold" | Intuition-first: "Something about that market feels right" |
| **What they notice first** | Spread percentages, stale data | Risks, fallback plans | Frontier nodes, unexplored opportunities |
| **Warfront framing** | Leads with margin analysis | Leads with survival concerns | Leads with untapped opportunities |
| **Economic outcome** | Same as others | Same as others | Same as others |

**One exception — discovery emphasis** (see below): FO personality affects
what the FO NOTICES during the player's exploration, creating different
breadcrumbs through the mystery. This is disclosed, positive divergence
(different interesting content, not better/worse outcomes), and it creates
replayability.

### 6. The FO's #1 Job Is Keeping You Alive

Before profit, before reputation, the FO ensures **module sustain** — the
resource flow that keeps your ship's equipment online. The FO monitors sustain
automatically and warns proactively:

```
FO: "Captain, you installed the Shield MK2 last cycle. It needs
2 metal and 3 ore every sustain cycle. I've added a supply run.
Your shield stays online. I'll let you know if supply gets tight."
```

### 7. The FO Can Disagree

At key moments — warfront stances, risky capital investments, routes through
dangerous space — the FO voices concerns with actionable reasoning:

```
Player chooses [Support Valorin].

Dask: "Understood, Captain. But Chitin controls our composites
supply. If they retaliate, your shield module goes offline in
4 cycles. I'd recommend stockpiling composites first."

[Proceed anyway]  [Stockpile first]  [Reconsider]
```

**Rules:** FO always executes the player's final decision. Pushback happens
BEFORE execution, never after. Must contain actionable information. Maximum
1 pushback per decision. If the player was right and the FO was wrong, the
FO acknowledges it later.

### 8. The FO Proposes — The Player Disposes (DESIGN LAW)

**No route, purchase, construction, or strategic commitment ever begins
without explicit player approval.** The FO observes, evaluates, and
recommends. The player confirms, corrects, or declines. This is inviolable.

This reconciles the FO-as-automation-system model with AutomationPrograms.md
Principle 1 ("Nothing should happen that the player didn't set up"). The
setup is *conversational* instead of *parametric* — the player "sets up"
automation by confirming Teaching Replays and approving FO proposals — but
the principle is preserved: nothing runs without the captain's word.

**Levels of Automation by domain** (Sheridan & Verplank 1978, adapted for
entertainment context per Cummings & Mitchell 2007):

| Domain | LOA | What This Means |
|--------|-----|-----------------|
| **Route creation** | 5 (execute if approved) | FO proposes via Teaching Replay or ROUTE_SUGGESTED; player confirms |
| **Route optimization** (rerouting, margin adjustment) | 6 (act, inform after) | FO reroutes for tariffs/degradation; player can inspect via Route Query |
| **Sustain logistics** | 7 (act, report on exception) | FO diverts supply automatically; only surfaces at WARNING/CRITICAL |
| **Ship purchase** | 4 (suggest one alternative) | FO recommends specific ship for specific route; player decides |
| **Warfront response** | 4 (suggest with rationale) | Crisis briefing with FO recommendation highlighted; player chooses |
| **Construction** | 5 (execute if approved) | FO proposes; player confirms |

Higher LOA for routine operations (sustain, rerouting) prevents
micromanagement. Lower LOA for capital decisions (purchases, warfronts)
preserves player agency. Route optimization at LOA 6 is the only domain
where the FO acts without pre-approval — and Route Query Interaction
(§FO Communication) ensures the player can always ask "why did you do
that?" and get an in-character answer. This prevents automation surprise
(Sarter, Woods & Billings 1997) while avoiding the Stellaris sector problem
(LOA 4 for everything = the player micromanages anyway).

**Route revert**: For route changes (reroute, ship reassignment), the FO
maintains the previous configuration for 200 ticks. If the player says
"that was wrong, go back," the FO reverts. Simple undo with a time limit.
This addresses Nielsen Heuristic #3 (User Control and Freedom) — the player
always has a clearly marked exit.

---

## The Exploration-Exploitation Pipeline

### How Discovery Feeds the FO

Every discovery yields at minimum ONE of (ExplorationDiscovery.md #6):
- (a) A new trade route the FO can automate
- (b) A technology that improves existing automation
- (c) Trade intelligence that makes current routes more profitable

The FO evaluates each discovery in character:

```
FO: "That mineral deposit you found at Node 12 — I can set up an
extraction operation there. Exotic_crystals, enough to sustain your
scanner AND feed Route Delta.

Construction contract: 4 phases, 250cr each. Once built, I'll
charter a mining vessel.

 [Start construction]  [Not now]  [Tell me more]"
```

### Fracture Discovery: Where Only the Player Can Go

The fracture module can't be replicated — empire fleet ships can't use it.
Fracture-space sites (ancient installations, off-thread resource deposits,
accommodation geometry anomalies) are accessible ONLY to the player.

The player doesn't manually RUN fracture trade routes. They EXPLORE fracture
space, DISCOVER what's there, and the FO figures out how to connect those
discoveries to the thread-space network:

```
FO: "That exotic_crystals deposit you found in fracture space —
I can't send ships there. But Node 14 is two thread-hops from
the exit point. If I build a relay depot, I can route crystals
into the network. Want me to start?"
```

This creates an irreplaceable player role that GROWS more important as the
game progresses. Early game: the player teaches the FO basic routes. Late
game: the player is the only one who can access the most valuable sites in
the galaxy. The FO builds the infrastructure; the player opens the frontier.

### Ancient Tech as Automation Multipliers

*Inspired by Starsector's AI Core mechanic.*

Ancient technology found during exploration (data caches, navigation beacons,
accommodation geometry fragments) doesn't just provide credits — it slots
into the FO's operation as a visible, named upgrade:

| Find | FO Application | Visible Effect |
|------|---------------|----------------|
| **Ancient Navigation Beacon** | Route pathfinding optimization | "Route Delta efficiency: +40% (Beacon from Site 7)" |
| **Accommodation Data Cache** | Market prediction from fracture-space price patterns | "FO predicted Valorin demand spike 50 ticks early" |
| **Precursor Trade Ledger** | Reveals hidden supply/demand relationships | "New route opportunity: Concord electronics → Communion, previously unknown connection" |
| **Metric Calibration Module** | Scanner range improvement for intel freshness | "Intel range: 2 hops (Calibration Module from Drift Site)" |

The player sees the causal chain: "I explored that dangerous site → found
this artifact → now the FO's Route Delta is 40% more efficient." The
connection between exploration and automation is visible, named, and specific.

### The Intel Loop (Background System)

The existing `IntelSystem` handles intel as a background system. The player
contributes by VISITING PLACES — no inventory management, no "sell or give
report" decisions. Visiting a node automatically refreshes price data. The
FO acts on it.

| Intel Quality | FO Behavior | Margin Buffer |
|---------------|-------------|---------------|
| **Fresh** (< 720 ticks) | Tight margins, confident routing | 5% |
| **Aging** (720-2160 ticks) | Wider margins, conservative | 15% |
| **Stale** (> 2160 ticks) | Very wide margins or route paused | 25% |

Scanner tech extends intel range passively (0 → 1 → 2 hops). Mid-game:
visiting one node updates adjacent nodes. Late-game: normal travel keeps
most of the network fresh. The player never goes somewhere JUST for intel —
it's always a side effect of doing something interesting.

### Natural Route Depreciation

Routes degrade through two mechanisms, not just warfront disruption:

1. **Market equilibration**: NPC traders discover profitable routes over time,
   increasing competition and compressing margins. A route that starts at 30%
   margin naturally declines to 10-15% over 300-500 ticks as the NPC economy
   rebalances. This is the steady background pressure that keeps the player
   exploring.

2. **Event disruption**: Warfronts, embargoes, faction shifts, and pirate
   activity create sudden route disruption (existing warfront system).

The FO surfaces degradation through existing triggers (ROUTE_DEGRADED,
ROUTE_DEAD) but also suggests replacements: *"Route Alpha's margins have
thinned — the locals figured it out. But that new system you visited has
something interesting."*

**Design intent**: The player should always have a reason to explore new
space. If existing routes lasted forever, the exploration loop dies — this
is Stellaris's critical flaw (exploration content dries up once borders
stabilize). Natural depreciation ensures the FO always needs fresh
intelligence, which the player provides by playing the game.

### Discovery Emphasis by FO Personality

*Inspired by Endless Space 2's faction-colored exploration.*

The FO notices different things based on personality — same galaxy, different
breadcrumbs. This is the ONE place where FO personality creates gameplay
divergence, and it's disclosed and positive (different interesting content,
not better/worse outcomes):

| FO | Notices | Leads To |
|----|---------|----------|
| **Maren (Analyst)** | Data anomalies in trade patterns — price movements that don't match standard models | Economic mysteries → pentagon ring evidence |
| **Dask (Veteran)** | Tactical signatures — weapons residue, fleet debris, patrol gaps | Warfront intelligence → faction secret evidence |
| **Lira (Pathfinder)** | Navigational oddities — thread distortions, gravity anomalies | Fracture-space content → ancient civilization evidence |

All three paths lead to the same central mystery. The player who picks Maren
discovers the pentagon ring is artificial through economic data. The player
who picks Lira discovers it through navigational anomalies. Different
breadcrumbs, same revelation. This creates replayability — different FO =
different path through the mystery.

---

## Trade Data as Lore Evidence

### The Economy Tells the Story

*NarrativeDesign.md: "The player assembles — never receives."*
*dynamic_tension_v0.md: "Every fracture trade route is evidence that the
galaxy's economic geography is a cage."*

As the FO builds routes through fracture-adjacent space, the economic data
reveals the pentagon dependency ring's artificial nature. The FO — with no
ancient knowledge — notices anomalies from pure economics:

**Early (Tick 400-800):**
```
Maren: "Route Delta's margins are unusually high for a standard goods
corridor. The model says this trade shouldn't be this profitable."

Dask: "Something about this route feels too good. In my experience,
when margins are this fat, someone's keeping competition out."

Lira: "The markets out here don't follow the patterns I learned in
thread space. It's like the economy has a different shape."
```

**Mid (Tick 800-1400):**
```
FO: "Captain, our fracture-adjacent routes outperform thread routes
by 40%. According to standard economic models, that's impossible —
the thread network should be optimal by definition. Unless the
thread network was designed to be something other than optimal."
```

**Late (Tick 1400+):**
```
FO: "I've been reviewing five months of trade data. The pentagon
pattern — every faction depending on the next — it's too clean.
Natural economies don't form perfect circles. Someone built this.

The routes you've been opening in fracture space prove it. Trade
works BETTER without the threads. The threads aren't optimizing
commerce — they're constraining it."
```

This is narrative delivery through gameplay systems. The player doesn't read
a lore dump about the pentagon ring. Their own trade data proves it. The FO
— their trusted companion — is the one who puts it into words.

### Economic Anomalies Feed the Knowledge Graph

Price anomalies noticed by the FO can be "connected" in the Knowledge Graph
as evidence. When the FO flags that Concord is overpaying for a good with
no production use in their territory, the player can investigate — and
discover Concord's classified programs. Price data IS evidence.

---

## Warfront Integration

### Where the FO Becomes Essential

Warfronts create the game's most impactful economic disruptions. The FO
transforms these from opaque systems into clear strategic choices.

When a warfront escalates, the FO delivers a **crisis briefing**:

```
FO: "Captain, the Valorin-Chitin front just hit OpenWar.

 1. SUSTAIN RISK: Your scanner needs exotic_crystals. Our source
    is in Chitin space — compromised. 4 cycles of reserves.

 2. OPPORTUNITY: Munitions at 3x at contested Proxima. +400cr/cycle
    if I divert a hauler — but it costs Chitin reputation.

 3. YOUR CALL:
    [Support Valorin]  — Rep gain, Chitin rep loss
    [Support Chitin]   — Maintain supply chains
    [Stay neutral]     — Accept margin loss
    [Broker peace]     — Divert cargo for de-escalation"
```

#### Decision Dialogue Design Rules

All multi-option FO decision dialogues (crisis briefings, fleet disposition,
construction offers) follow these rules:

1. **FO recommendation is always highlighted.** One option has a visual
   accent (brighter border, FO portrait adjacent) indicating the FO's
   preferred action. The recommendation is personality-driven: Dask
   highlights the defensive option, Maren the margin-optimal option,
   Lira the exploratory option. This serves as the "default" from choice
   architecture research (Johnson et al. 2012) — uncertain players follow
   the recommendation; expert players deviate deliberately.

2. **Keep all options visible.** Do not collapse options behind "Tell me
   more." The player is a captain receiving a briefing, not a consumer
   being shielded from complexity. 4 options with clear tradeoff axes
   (rep/supply/profit/peace) are well within expert decision-making
   capacity (Hick's Law: equiprobable choices slow decisions; hierarchical
   choices with distinct axes do not).

3. **Context precedes decision.** The briefing structure is: SITUATION →
   STAKES → OPTIONS. The player absorbs context before seeing choices.
   This follows Johnson et al.'s "structuring" and "providing information"
   choice tools.

4. **Quantify consequences.** Every option shows its primary consequence
   in concrete terms ("4 cycles of reserves," "+400cr/cycle," "Chitin
   rep loss"). Use color coding: red for threats to assets, green for
   opportunities, amber for tradeoffs. No option should require the
   player to guess the outcome.

5. **One briefing at a time.** If multiple warfronts escalate
   simultaneously, the FO queues briefings by severity. Never stack two
   decision dialogues. The player resolves one, then the next surfaces.

### FO Pushback During Warfronts

```
Player chooses [Support Valorin].

Dask: "Captain, Chitin controls our composites supply. Embargo in
4 cycles means your shield module goes offline. I'll set it up,
but stockpile composites first."

[Proceed anyway]  [Stockpile first, then commit]  [Reconsider]
```

### Haven Crew Dissent

During major warfront decisions, the non-chosen FOs at Haven can offer
counter-opinions through the chosen FO:

```
Dask: "I recommend pulling back from contested space.

 ...though Maren sent a note — she thinks the margin data supports
 staying in. 'Tariff spike is temporary. Historical pattern says
 80 ticks max.' Your call, Captain."
```

**Rules:** Only during warfronts and major capital investments. Maximum one
dissenting voice per briefing. Chosen FO presents their recommendation first.
Haven crew opinions arrive as "notes" — they're not present.

### Warfront Interactions With Fleet

Owned ships in contested space create real stakes:

```
FO: "We have 2 owned Haulers in contested space. Options:

 [Pull back]        — Lose 200cr/cycle, ships safe
 [Keep running]     — Risk of ship loss, revenue continues
 [Hire escort]      — +60cr/cycle overhead, loss risk drops to 2%
 [Switch to charter] — Park our ships, charter locally instead"
```

### Warfront Missions

Time-limited trade opportunities from warfront activity:

- **War supply contract**: "Valorin offering 3x for munitions. 100 ticks."
- **Smuggling run**: "Chitin station desperate for electronics. High margin, contested route."
- **Peace brokering**: "Deliver 200 medical supplies to both sides. Costs 3 cycles of profit, but tariff relief pays for itself in 10."

---

## The Empire Fleet: Charter > Own Progression

### Two Separate Fleets

The player's **personal ships** (1-3, Haven hangar) are for exploration,
combat, and missions. The **empire fleet** is managed by the FO for trade
operations. Completely separate.

### Early Game: Everything Chartered

When the FO starts managing routes, there are no empire-owned ships. The FO
charters NPC haulers — hiring them per-cycle.

```
FO: "Route Alpha is set up. Chartering a local hauler —
40cr/cycle in fees. Net: +80cr."
```

**Charter = higher cost, zero risk, instantly available.**

The FO picks charter providers based on route needs. The player doesn't
manage charter relationships — the FO handles it and only surfaces charter
issues when something dramatic happens (ship lost, provider refuses a
dangerous route during warfront).

### Mid Game: The Buy Decision

At Haven Tier 2 (drydock online), the FO raises the purchase question for
well-established routes:

```
FO: "Route Alpha has run for 300 ticks. We've paid 12,000cr in
charter fees. A Hauler costs 8,000cr and runs ~90cr/cycle upkeep.
Buying pays for itself in ~90 ticks.

 [Buy a Hauler]       — 8,000cr upfront, lower ongoing cost
 [Keep chartering]    — No commitment, higher ongoing cost
 [Tell me more]       — Full cost breakdown"
```

**Own = lower cost, upfront investment, risk of real loss.**

The decision is **capital allocation** — the kind of decision an explorer-
strategist makes between expeditions, not a fleet management minigame.

### The Flip Moment

*Inspired by Starsector's income-goes-positive transition.*

When the empire crosses from net-negative (charter costs > revenue) to
net-positive, the FO delivers a character beat — not a notification:

```
FO: "Captain. Just ran the numbers. We're in the black.

Three routes running, two chartered haulers, one extraction site.
After all costs — charter fees, sustain logistics, everything —
we're generating 120 credits a cycle. Net positive.

Remember that first food run to Kepler? [beat] We've come a way."
```

This is a designed emotional landmark. The player feels the transition from
"building" to "running."

#### Flip Moment — Multi-Sensory Specification

The flip moment is the emotional payoff of the entire centaur model. Every
reference game marks its equivalent moment with unforgettable multi-sensory
feedback: Factorio's first rocket launch (cinematic + achievement),
Subnautica's first surface breach (FOV widen + music swell), Outer Wilds'
first sun explosion (unique music + screen transformation). Our flip moment
must match this standard.

| Channel | Spec | Duration |
|---------|------|----------|
| **FO dialogue** | Character beat (already designed above). Personality-variant. | ~5s |
| **Letterbox** | Brief cinematic bars (reuse warp arrival letterbox from hud.gd). Creates a "the game noticed what you did" moment. | 0.5s on, hold through dialogue, 0.5s off |
| **Audio fanfare** | Unique one-time sound. NOT the standard FO chime — a distinct ascending motif that plays exactly once in the entire game. Warm, orchestral. Think Factorio's rocket launch sting. | 3-5s |
| **Credit counter glow** | HUD credits display pulses golden for 3 seconds. Subtle but visible — the number itself celebrates. | 3s |
| **Sparkline crossover** | If the player opens the Empire Dashboard within 200 ticks, the Overview shows a revenue-vs-cost sparkline graph with the crossover point highlighted. Annotated: "Net positive — tick [N]." | Persistent in Overview |
| **Haven trophy wall** | "Empire Milestone: Net Positive" entry added automatically. | Permanent |

**The letterbox is critical.** Our camera_cinematics doc reserves dramatic
framing for special moments. The flip moment IS a special moment. The brief
bars create a "this is a cutscene" feeling without actually being one — the
world continues, but the framing says "pay attention." God of War uses this
technique for Mimir's most important boat stories.

### Ship Identity

Owned ships get names — the FO names them in personality-appropriate style.
The player can rename. When the FO reports ship activity, it uses names:

- Maren: *"Hull-7A"*, *"Trade Vector 3"*
- Dask: *"Steady Run"*, *"Old Reliable"*
- Lira: *"Far Reach"*, *"Wanderlust"*

Ship loss is narrated as a character moment:

```
Dask: "Captain... 'Steady Run' didn't make it back. Pirate ambush
on Route Delta. Crew got to the pods. But the ship and cargo are
gone. She ran that route for 400 ticks. Never missed a delivery.

 [Replace her]  [Charter the route for now]  [Retire the route]"
```

Ships accumulate service history (ticks active, cargo delivered, revenue
generated). Veteran ships (500+ ticks) get a route-specific efficiency bonus
— the crew knows the stations, dock crews know them, paperwork is faster.
Losing a veteran means losing that accumulated efficiency.

### Fleet Composition

The FO recommends ship types based on need. The player never picks from a
catalogue — the FO recommends a specific ship for a specific purpose:

| Ship Type | Role | When FO Recommends |
|-----------|------|-------------------|
| **Hauler** | Bulk trade routes | Stable, high-volume routes proven over 200+ ticks |
| **Clipper** | Fast courier runs | Time-sensitive goods, short-hop routes |
| **Mining Vessel** | Resource extraction | When FO identifies extractable sites |
| **Corvette** | Route protection | Contested/pirate space |
| **Scout** | Intel refresh | When staleness is costing money (late game) |

### Fleet Visibility on Galaxy Map

Empire fleet ships appear as **small icons on the galaxy map**, moving
along their assigned routes. The player can see their empire operating
in the world — haulers moving between stations, mining vessels parked at
extraction sites, corvettes patrolling contested corridors.

This serves three purposes:
1. **Trust**: The player sees the FO's decisions manifesting spatially
2. **Satisfaction**: A mature empire with 8+ ships moving across the map
   is the visual payoff for hours of exploration (Factorio belt-watching)
3. **Awareness**: The player notices when ships cluster, reroute, or
   disappear — spatial information that supplements FO briefings

Ship icons use fleet-appropriate colors (distinct from NPC trader icons)
and show a tooltip on hover: ship name, route, status, cargo.

#### Visual Distinction from NPC Ships

Player fleet ships MUST be visually distinguishable from NPC traders at a
glance. Elite Dangerous's #1 fleet complaint: players confuse their ships
with NPCs. Dead Space's principle: distinguish through visual treatment,
not just color.

- Player fleet icons have a subtle **persistent glow** (soft cyan halo)
  that NPC icons lack — visible at galaxy-map zoom without being garish
- Route lines for managed routes are drawn as **thin colored lines**
  between source and destination: green = healthy, yellow = degraded,
  red pulsing = dead/critical
- NPCs have no route lines on the galaxy map

#### The Belt-Watching Experience

*Reference: Factorio FFF #280 — "Visual feedback is the king." Sennett
(2008): The Craftsman — satisfaction of watching productive systems you
built. Lazzaro (2004): "Altered States" — contemplative observation is a
legitimate and underserved emotional target.*

Watching your empire operate must be *satisfying*, not just informational.
The difference between "dots on a map" (screensaver) and "my empire at
work" (belt-watching) is multi-sensory feedback:

| Element | Spec | Why |
|---------|------|-----|
| **Visible throughput** | Ships animate along route lines at consistent cadence | Confirmation of mastery — "I built this and it works" |
| **Station arrival/departure** | Ship icon decelerates → brief dock flash → re-accelerates on departure | Rhythmic satisfaction — regular cadence is meditative (Factorio belt rhythm) |
| **Bottleneck visibility** | Stalled ships show a yellow pulse at their stopped position; degraded routes shift line color | Diagnostic at a glance — "that route needs attention" |
| **Route attribution tooltip** | Tooltip includes: "Discovered: tick 450, Node 12 expedition" | Agency residue (Kaptelinin & Nardi 2006) — "I found that route" |
| **Zoom-to-detail** | Clicking a fleet ship opens Route Query (FO explains in character) | Connects map observation to FO interaction seamlessly |
| **Ambient commerce hum** | When 3+ routes are running, a low rhythmic audio texture is faintly audible during idle moments (same delivery window as NETWORK_MILESTONE). Varies in density with fleet size. See **Route Heartbeat** in §Audio Vocabulary for full spec | Factorio's belt sounds equivalent. The player HEARS their empire. |

**Galaxy map layering**: Fleet icons and discovery markers are **spatial
furniture** — always visible once unlocked, because they represent things
IN the galaxy. Data overlays (Security, Trade Flow, Intel Age, Scanner
Range) remain one-at-a-time via V-key cycle, because they represent data
ABOUT the galaxy. This prevents the X4 anti-pattern of combining all data
types into one unreadable view (Civ VI's lens model: one analytical view
at a time, spatial furniture always on).

#### Empire Health Indicator (Persistent HUD)

After the player's first managed route, the HUD shows a persistent **Empire
Health icon** — a small diamond that communicates fleet status at a glance:

| State | Color | Pulse | Meaning |
|-------|-------|-------|---------|
| **Healthy** | Green | None (steady) | All routes profitable, sustain reserves adequate |
| **Degraded** | Yellow | Slow pulse (2s cycle) | 1+ routes below 5% margin OR sustain stock < 3 cycles |
| **Critical** | Red | Fast pulse (0.5s cycle) | 1+ routes dead OR sustain critical OR ship lost |

**Design rules:**
- The icon appears ONLY after the first route is managed — no empty-state clutter
- Clicking the icon opens the Empire Dashboard FO tab (one-tap deep dive)
- The icon does NOT show numbers — it communicates "everything's fine" vs "check
  the FO" via color alone. Numbers belong in the dashboard, not the HUD
- State transitions use the FO ALERT sting (see Audio Vocabulary below) for
  Healthy→Degraded and Degraded→Critical only. Recovery (Critical→Healthy) is
  silent — the absence of alarm IS the signal
- The icon sits near the credits display (top-left HUD cluster), reinforcing the
  connection between empire health and income

This addresses the post-automation attention gap: the player can be deep in
fracture space, exploring anomaly chains, and still know at a glance whether
the FO needs attention. Elite Dangerous's #1 fleet management complaint is
"I didn't know my fleet was failing until it was too late." One icon solves this.

#### Automation & Empire Audio Vocabulary

*Reference: Factorio FFF #197 — "Sounds that communicate system state without
requiring visual attention." Subnautica — 4 distinct scanner audio signatures
create Pavlovian response to discovery. Dead Space — diegetic UI sounds reinforce
spatial awareness.*

The FO Trade Manager introduces automated systems that operate off-screen. Audio
is the ONLY feedback channel when the player's visual attention is elsewhere
(fracture navigation, combat, exploration). Every automation state must have a
distinct audio signature.

| Signature | Trigger | Sound Profile | Purpose |
|-----------|---------|--------------|---------|
| **Anomaly Ping** | Scanner detects anomaly in range | Sharp, metallic 2-note rising tone (200ms). Distinct from combat radar | "Something's out there" — exploration hook. Pavlovian: player learns to associate this with discovery |
| **Scan Process** | Active scan in progress | Low rhythmic pulse (1.2s loop), subtle harmonic shift as scan progresses | "Working on it" — patience anchor. Subnautica's scan sound: player waits because the sound promises a reward |
| **Discovery Reveal** | Scan completes, discovery classified | 3-note ascending chime (C-E-G), slight reverb tail. Major key = positive | "Found something!" — the dopamine hit. Must be distinct from combat and trade sounds |
| **Insight Chime** | Knowledge Graph link confirmed (Obra Dinn batch) | Glass bell tone with harmonic overtone (400ms). Rarer than Discovery Reveal | "I was right" — the detective reward. Rarity makes it precious. Outer Wilds' Ship Log completion sound |
| **FO Comm Open** | FO begins speaking (any dialogue trigger) | Soft radio crackle + single low tone (150ms) | "Your FO has something to say" — attention redirect without alarm |
| **FO Decision Tone** | FO presents a decision requiring player input | Two-note rising query tone after Comm Open (300ms) | "This one needs you" — distinguishes info-only FO comms from decisions |
| **Route Heartbeat** | Background pulse when 3+ routes running (belt-watching) | Low rhythmic texture, density scales with fleet size (3 routes = sparse, 15 = rich). Inaudible during combat/dialogue | "Your empire is alive" — Factorio belt hum equivalent. Ambient, not alert |
| **ALERT Sting** | Empire Health transitions to Degraded or Critical | Sharp descending 2-note (D-Bb, 250ms). Critical adds second hit 200ms later | "Check the FO" — urgency without panic. Must cut through combat audio |
| **Flip Moment Fanfare** | Empire crosses net-positive revenue | 4-note brass motif (500ms) + soft sustained pad (2s fade). Plays once per playthrough | "You built something real" — the centaur payoff. Rare enough to feel monumental |
| **Revelation Fanfare** | Knowledge Graph revelation fires (R1/R3/R5) | Deep resonance tone → crystalline ascending phrase (1.5s). Different pitch for each revelation tier | "The mystery deepens" — lore progression marker. 5 total across the game |
| **Batch Insight** | 3+ speculative links confirmed simultaneously | Insight Chime × 3 in rapid cascade (low-mid-high), followed by sustained harmonic | "The picture comes together" — the Obra Dinn triple-confirmation. Extremely rare and extremely satisfying |

**Layering rules:**
- Only ONE alert-tier sound at a time (ALERT Sting, Fanfare). Queue, don't stack
- Route Heartbeat ducks (volume -12dB) during FO dialogue and combat
- Discovery sounds (Anomaly Ping, Scan Process, Reveal) layer freely with Route
  Heartbeat — the player should hear both exploration and empire simultaneously
- All signatures use the same reverb space as the ship cockpit — diegetic, not
  menu-UI. The sounds come from "inside the ship," reinforcing spatial immersion

### Haven Gates Fleet Capacity

| Haven Tier | Max Routes | Max Owned Ships | Unlocks |
|------------|-----------|-----------------|---------|
| 1 (Powered) | 2 | 0 (charter only) | Basic logistics |
| 2 (Habitable) | 4 | 2 | Drydock, first purchases |
| 3 (Operational) | 8 | 4 | Construction contracts |
| 4 (Expanded) | 12 | 8 | Extended network |
| 5 (Awakened) | 16+ | 12+ | Full empire logistics |

---

## Construction: Always Contracted

Construction projects (Depot, Shipyard, Refinery, Science Center, Extraction)
use credit-per-step, ticks-per-step progression. No construction ship. The FO
hires a crew for a fixed cost.

```
FO: "Extraction Station at Node 12 — 4 phases, 250cr each.
Once built, I'll run it with a chartered mining vessel.

 [Start construction]  [Not now]  [Tell me more]"
```

Haven upgrades are overseen by the non-chosen FO at Haven:

```
FO: "Haven Tier 4 is ready. Dask says he can oversee the build
while we're out. 200 ticks.

 [Start the upgrade]  [Wait]"
```

---

## Module Sustain Integration

### Sustain Priority Cascade

The FO allocates resources in this order:

1. **SUSTAIN** — Keep modules alive. 3-cycle minimum reserve. Divert logistics
   if shortage imminent. FO warns proactively.
2. **FUEL** — Keep fleet moving. Maintain player fuel above 25%.
3. **WARFRONT DIRECTIVE** — Execute player's strategic choice (support/neutral/peace).
4. **OPPORTUNITY** — FO initiative within explored space (gated by competence).

### Sustain Connects To Everything

- Tech requires goods → goods come from faction space → warfronts disrupt
  supply → modules go offline without goods → extraction stations provide
  independence. The FO prevents module failure (its #1 job).

---

## FO Competence

### Growth Through Experience, Not XP

The FO grows through accumulated experience — but the player doesn't manage
competence domains, track XP, or think about progression. The FO simply gets
better at things as the player plays. The growth manifests as DIALOGUE and
CAPABILITY, not numbers.

**Three tiers, not five domains:**

| Tier | Name | Capability | When It Happens |
|------|------|-----------|-----------------|
| 1 | **Novice** | Charter-only. Runs 1-2 demonstrated routes. Basic sustain (fuel). | FO selected during tutorial |
| 2 | **Competent** | Runs 3-5 routes. Manages all sustain. Suggests adjacent-node extensions. Recommends first ship purchase. | ~15 manual trades, 5+ nodes explored, first warfront survived |
| 3 | **Master** | Full network (15+ routes). Fleet optimization. Proactive rebalancing. Warfront briefings with strategic depth. Economic anomaly detection. | Haven operational, 8+ systems explored, endgame tier approached |

### Growth Through Crisis, Not Accumulation

The FO's most meaningful growth happens when they survive adversity. A sustain
emergency handled well. A warfront that disrupted routes and was navigated.
A ship loss and recovery. These events trigger level-up dialogue:

```
FO (reaching Competent after first warfront):
Dask: "After the composites crisis, I started stockpiling 2 extra
cycles of reserves. Won't get caught short again. Give me a few
more routes — I can handle them now."
```

This makes growth feel earned and narrative, not mechanical.

### FO Service Record (Trust-Building Display)

*Research: Hoff & Bashir (2015) — learned trust is the most designable
trust layer, built through visible track record. Dzindolet et al. (2003) —
users who see WHY the system failed trust it MORE than users who just see
success without understanding.*

The player should be able to inspect the FO's track record in the Empire
tab (FO section). This is NOT a trust meter or confidence bar — it's a
**service record**, matching the pattern already established for fleet ships
(NarrativeDesign.md §7: "Fleet ships accumulate visible history").

```
Maren — Competent (Tier 2)
Routes managed: 7 | Recommendations taken: 12 of 14
Profitable recommendations: 10 (83%)
Crises handled: 2 (Valorin embargo, Chitin piracy)
Notable: Predicted Communion demand spike 50t early (Ancient Data Cache)
Worst call: Route Beta reroute cost 120cr before recovery (Tick 834)
```

**Design rules:**
- The record is factual and clinical — matching NarrativeDesign.md Principle
  3 ("Restraint is the instrument: military professionalism in all system
  communications")
- "Worst call" is always included. This builds trust through transparency
  (Dzindolet et al.: error explanation increases trust). The FO doesn't
  hide mistakes — they're on the record
- "Notable" highlights moments where the FO was right in a way that
  mattered — especially when ancient tech or discovery intel contributed
- The record grows over the playthrough. At endgame, it's a shared history
  that feeds the final FO dialogue

---

## Non-Chosen FOs at Haven

The two non-chosen candidates relocate to Haven after discovery. They provide
specialty intel that the chosen FO integrates:

| At Haven | Specialty | Provides |
|----------|-----------|----------|
| **Maren** | Market intelligence | Faction price trends, demand shifts, pattern analysis |
| **Dask** | Station operations | Haven sustain, supply levels, construction oversight |
| **Lira** | Local exploration | Instability patterns, frontier opportunities |

All three perspectives available regardless of which FO you chose. Your FO
integrates Haven crew intel into their own briefings using their own voice.

Haven crew grow as Haven upgrades:

| Haven Tier | Crew Capability |
|------------|----------------|
| 2 | Basic reports. |
| 3 | Trend analysis. |
| 4 | Predictive intel. Strategic dissent during warfront decisions. |
| 5 | Full counsel. |

---

## The Endgame: Trade Empire as Narrative Payoff

### Economic Consequences of the Three Paths

The player's trade network should mechanically reflect their endgame choice.
The FO adapts the fleet to whichever path the player pursues:

**Reinforce** (stabilize threads, narrow the network):
```
FO: "If we're shoring up the threads, I need to pull our fracture-
adjacent operations. Tighter network, but sustainable. The routes
that survive will be the galaxy's new infrastructure.

We built this to last, Captain."
```

**Naturalize** (adapt to metric bleed, expand through fracture):
```
FO: "The old routes are degrading. But the new ones... Captain,
these margins shouldn't exist. The economy is rewriting itself
around the paths you opened.

We didn't just build a trade network. We built the proof that
the galaxy doesn't need its cage."
```

**Renegotiate** (dialogue with instability, accept uncertainty):
```
FO: "I can't predict margins anymore. The numbers keep shifting.
But the routes that work are extraordinary — like the galaxy is
trying to find a new equilibrium.

Whatever this is... we're part of it now."
```

### Trade Empire as Endgame Leverage

The player's network determines which endings are AVAILABLE:

- A player who built extensive Communion supply routes has the infrastructure
  to reinforce threads — the empire IS the maintenance system.
- A player who built fracture-space routes has proven commerce doesn't need
  threads — the empire IS the evidence for naturalization.
- A player who traded with everyone equally is the only trusted neutral party
  — the empire IS the peace brokering mechanism.

The endgame doesn't ask "which button do you press?" It asks "what did you
build?" 50+ hours of trade decisions retroactively gain meaning.

### The FO Notices You Changing

The fracture module's neurological adaptation (factions_and_lore_v0.md) is
the game's deepest lore thread. The FO — who spends more time with the player
than anyone — is the one who notices:

**Early fracture use:**
```
FO: "You seem to find fracture routes more easily now."
```

**Heavy fracture use:**
```
FO: "Captain, you chose that route by instinct. My analysis agrees,
but you decided before I ran the numbers. How did you know?"
```

**Late game:**
```
FO: "I've been reviewing your decisions. The ones that don't make
economic sense... they make a different kind of sense. The module
is changing how you see the galaxy. I trust you. But I want to
understand what you're seeing that I can't."
```

---

## What Happens to the Current Program System

### Internal Implementation

Existing program types become internal implementation details the FO manages.
The player never sees "TRADE_CHARTER_V0" or "RESOURCE_TAP_V0."

| Current | Becomes |
|---------|---------|
| `CreateTradeCharterProgram(...)` | FO creates when player hands off a route |
| `CreateResourceTapProgram(...)` | FO creates when player approves extraction |
| `CreateAutoBuyProgram(...)` | **Removed.** Absorbed into FO sustain management |
| `CreateAutoSellProgram(...)` | **Removed.** FO sells surplus as part of route management |
| `StartProgram / PauseProgram / CancelProgram` | FO manages lifecycle. Player can override: "shut down Route X" |

### Reconciliation with AutomationPrograms.md

This document supersedes AutomationPrograms.md's *creation model* (player
manually configures every program parameter). The following principles from
AutomationPrograms.md are **preserved**:

| Principle | Status | How Preserved |
|-----------|--------|---------------|
| Every automated action must be predictable | **Preserved** | Teaching Replay, Route Query, FO explains decisions in character |
| Override in one action | **Preserved** | Player can pause/cancel any route with one action |
| Failure is visible, not silent | **Preserved** | Exception surfacing — FO reports failures with reason + suggestion |
| Automation earns trust gradually | **Preserved** | Competence tiers: Novice → Competent → Master |

The following principles are **superseded**:

| Principle | Status | Why |
|-----------|--------|-----|
| Player is the architect (designs every program) | **Superseded** | FO proposes, player approves. Player's skill is strategic, not configurational |
| Never auto-create programs player didn't request | **Superseded** | FO suggests routes from intel (Competent+), always with player approval |
| New programs start Paused | **Superseded** | FO creates programs in Running state after player approval |

The "Automation Spectrum" section of AutomationPrograms.md (what the player
always controls vs what the game executes) remains valid — the FO Trade
Manager doesn't change WHAT is automated, only HOW automation is initiated
(FO-mediated instead of player-configured).

### Migration Path

1. **Phase 1**: This document + FO Trade Manager entity model. No code removal.
2. **Phase 2**: FO route handoff (player trades → FO offers to manage). Charter-
   only. Tutorial rewrite. Runs alongside existing programs.
3. **Phase 3**: FO competence tiers. FO suggests routes from intel. Ancient tech
   multipliers. Discovery emphasis by personality.
4. **Phase 4**: Empire fleet (charter → own). Ship identity. Construction.
   Warfront briefings. Sustain management. Flip moment.
5. **Phase 5**: Trade data as lore evidence. Economic anomaly → Knowledge Graph.
   Endgame path economic consequences. FO notices player changing.
6. **Phase 6**: Deprecate manual program creation. FO is the only automation
   interface. Remove AutoBuy/AutoSell.

### Cross-System Dependencies (CRITICAL)

**The FO Trade Manager only works if exploration content is rich enough to
fill the automation gap.** When the player hands off routes to the FO, they
need something compelling to DO. If the exploration pipeline is empty, the
player becomes a spectator — this is the Phase 4 "spectator trough" that
research identifies as the highest churn risk.

#### The Pipeline (Current Status)

The full exploration→automation pipeline has 6 links. Links 2-3 are broken:

```
Player explores ──→ Discovery yields ──→ FO evaluates ──→ FO builds    ──→ Revenue funds  ──→ Cycle
                     trade intel           intel            automation      exploration       repeats
     [DONE]          [BROKEN]             [BROKEN]          [DONE]          [DONE]           [DONE]
```

Without links 2-3, this doc's thesis — "the FO builds the trade empire from
what you discovered" — has no mechanical backing. Exploration and automation
feel like separate games. The full build order and mechanical specs for the
missing links are in `ExplorationDiscovery.md` §Implementation Roadmap.

#### Prerequisites by phase

| FO Phase | Requires | From Doc | Status |
|----------|----------|----------|--------|
| Phase 2 (handoff) | Intel decay → margin buffer wiring (P0) | ExplorationDiscovery.md §Intel Decay | **Not implemented** |
| Phase 2 (handoff) | Discovery-as-trade-intelligence + EconomicIntel entity (P0) | ExplorationDiscovery.md §Economic Intel Types | **Not implemented** |
| Phase 2 (handoff) | DISCOVERY_OPPORTUNITY trigger plumbing (P0) | ExplorationDiscovery.md §FO Evaluation Beat | **Not implemented** |
| Phase 3 (competence) | Anomaly chain intel per step (P1) | ExplorationDiscovery.md §Chain → FO Intel Pipeline | **Not implemented** |
| Phase 3 (ancient tech) | Artifact → AncientTechUpgrade pipeline (P2) | ExplorationDiscovery.md §Artifact Research | **Not implemented** |
| Phase 3 (discovery emphasis) | FO personality → discovery system integration | fo_commentary_v0.md | Not implemented |
| Phase 4 (warfront briefings) | Warfront economic cascades fully implemented | dynamic_tension_v0.md Pillar 3 | Done |
| Phase 4 (sustain management) | Module sustain enforcement active | dynamic_tension_v0.md Pillar 2 | Done |
| Phase 5 (lore evidence) | Knowledge graph revelation system | factions_and_lore_v0.md | Done (T39) |
| Phase 5 (pipeline inversion) | FO economic analysis → exploration leads | ExplorationDiscovery.md §Pipeline Inversion | **Not implemented** |

#### The 6 TODO S6 epics ARE FO Trade Manager prerequisites

The following epics in `54_EPICS.md` are tagged `[FO_TRADE_MANAGER_PREREQ]` and
must be prioritized accordingly during gate generation:

- EPIC.S6.ANOMALY_ECOLOGY — procedural anomaly distribution (spatial logic)
- EPIC.S6.ARTIFACT_RESEARCH — identification, containment, experiments
- EPIC.S6.TECH_LEADS — tech leads → prototype candidates
- EPIC.S6.EXPEDITION_PROG — survey/salvage/multi-step programs
- EPIC.S6.SCIENCE_CENTER — analysis throughput, reverse engineering
- EPIC.S6.UI_DISCOVERY — discovery UI overhaul (player-facing feedback)

These are not "nice to have exploration features." They are the content that
fills the gap between handing off routes and having something to do. Without
them, the spectator trough kills engagement.

#### The spectator trough mitigation

Between tick 200 (first handoff) and tick 600 (fracture discovery), the
player MUST have: new systems to explore, combat encounters, faction
contacts to build, missions to run, and discovery sites to scan. **And
critically, those discoveries must produce economic intel that visibly
improves the FO's operations** — the DISCOVERY_OPPORTUNITY trigger and FO
evaluation beat (see ExplorationDiscovery.md §FO Evaluation Beat) are the
moment the player FEELS the pipeline working. Without this beat, the
player explores and the FO runs routes in parallel, never intersecting.

The FO Trade Manager is NOT a standalone system — it is the economic
backbone that funds and rewards an exploration game. If exploration content
is thin, this system amplifies the problem.

### Tutorial Rewrite (Phase 2)

Current tutorial Act 7 has the player create a program manually BEFORE
selecting their FO. This contradicts the entire design.

**New Act 7:**
1. FO_Selection — Player picks FO based on personality (after meeting all 3)
2. One more manual trade — FO observes
3. FO offers handoff with Teaching Replay — "I watched you run that route.
   Same pattern?" Player confirms or corrects.
4. FO charters a hauler — route runs autonomously
5. Player sees credits arriving while they explore something new
6. Graduation — the game has fundamentally changed

This is the "first conveyor belt" moment. The player felt the pain of manual
trading (3 required trades), then experienced the relief of automation
through a character they chose and trust.

---

## FO Communication

### Dialogue Triggers (New Trade-Management Triggers)

These extend the existing `FirstOfficerSystem.TryAutoDetectTriggers`:

| Trigger | Condition | Purpose |
|---------|-----------|---------|
| `ROUTE_OFFERED` | Player completes profitable manual trade | FO offers handoff with Teaching Replay |
| `ROUTE_DEGRADED` | Margin below 5% for 3+ cycles | FO warns about dying route |
| `ROUTE_DEAD` | Unprofitable for 10+ cycles | FO recommends shutdown |
| `ROUTE_SUGGESTED` | FO identifies opportunity from explored nodes (Competent+) | FO proposes new route |
| `SUSTAIN_WARNING` | Good drops below 3-cycle reserve | Supply risk alert |
| `SUSTAIN_CRITICAL` | Good drops below 1-cycle reserve | Emergency diversion |
| `FLEET_PURCHASE_REC` | Route stable 200+ ticks, charter cost > breakeven | FO recommends buying |
| `CONSTRUCTION_REC` | Resource site analyzed, extraction profitable | FO recommends building |
| `SHIP_LOST` | Owned ship destroyed | FO narrates loss, reallocates |
| `COMPETENCE_GROWTH` | FO tier threshold reached through crisis survival | FO announces new capability |
| `WARFRONT_BRIEFING` | Intensity change affecting managed routes | Crisis briefing |
| `WARFRONT_DISSENT` | Haven crew disagrees with stance | Haven crew note |
| `NETWORK_MILESTONE` | Total routes reaches 5 / 10 / 15 | FO reflects on growth |
| `FLIP_MOMENT` | Empire crosses to net-positive revenue | Character beat |
| `DISCOVERY_OPPORTUNITY` | Player's exploration yields FO-actionable intel | FO evaluates discovery |
| `ANCIENT_TECH_INSTALLED` | Exploration artifact slotted into FO operation | FO acknowledges upgrade |
| `LORE_ANOMALY` | Trade data reveals pentagon ring evidence | FO voices economic mystery |
| `FRACTURE_ADAPTATION` | Player's fracture use changes decision patterns | FO notices player changing |
| `ENDGAME_ECONOMY` | Endgame path choice manifests in trade network | FO reflects on what was built |
| `FO_PUSHBACK` | FO disagrees with player decision | FO voices concern |

Each trigger has **3 variants** (one per FO personality), consistent with
the existing `fo_dialogue_v0.json` format.

### Dialogue Delivery Timing

*Inspired by God of War's Mimir boat stories — lore during idle windows,
interruptible and resumable.*

FO observations accumulate and deliver during **low-engagement windows**:

| Window | Trigger Types | Why |
|--------|--------------|-----|
| **Travel** (warp transit, cruise) | LORE_ANOMALY, DISCOVERY_OPPORTUNITY, FRACTURE_ADAPTATION | Player's hands are idle, attention available |
| **Dock arrival** (first 3 seconds) | ROUTE_OFFERED, ROUTE_SUGGESTED, FLEET_PURCHASE_REC | Natural decision point |
| **Post-combat** (debrief moment) | WARFRONT_BRIEFING, COMPETENCE_GROWTH | Emotional window, player processing |
| **Idle** (> 30 seconds no input) | NETWORK_MILESTONE, FLIP_MOMENT | Reflective moments |

**Never during**: Active combat, manual trading, fracture navigation,
dialogue with NPCs. If the player interrupts a delivery with action, the
FO notes it: *"Remind me to finish that thought."* The observation queues
and delivers during the next eligible window.

ALERT-tier notifications (SUSTAIN_CRITICAL, SHIP_LOST) bypass timing
rules — they fire immediately as toasts regardless of player activity.

### Dock Arrival Recap ("While You Were Away")

*Research: Pielot & Rello (2015) — batch notifications at natural break
points are 3x more likely to be read than mid-action notifications. Hades
model: all NPC commentary delivered in the hub between runs, not mid-run.*

When the player docks after 100+ ticks since last dock, the FO delivers a
**batch summary** — 2-3 lines covering what happened while the player was
exploring:

```
Maren: "While you were out — 3 trades completed, net +840cr.
Route Beta's degrading, Chitin tariffs again. And I spotted
something near that system you visited. Worth discussing."

Dask: "Ran 3 cycles smooth, plus 840 to the books. One problem:
Route Beta's taking hits from the tariff. Also — new opportunity
from your last flyby. When you're ready."

Lira: "Your routes earned 840 credits while you were flying.
Route Beta's struggling with Chitin tariffs — I'll explain.
Oh, and that system you found? I have an idea."
```

**Design rules:**
- Always leads with the positive (credits earned, trades completed)
- Flags exactly ONE issue that needs attention (most severe)
- Teases ONE opportunity (most relevant to recent exploration)
- Maximum 3 lines. If more happened, the FO says "Details in the Empire
  tab" — pull model, not push
- Fires at dock arrival, before the dock menu opens — the FO speaks in the
  3-second dock window (see Dialogue Delivery Timing table above)
- Does NOT fire if nothing meaningful happened (< 100 ticks, no events)

### Post-Automation Dock Experience

The dock menu was designed for manual trading. After the FO manages 5+
routes, the dock experience must evolve from "execute trades" to "gather
intelligence and make strategic decisions." The dock should become MORE
interesting, not less.

**Dock menu evolution:**

| Pre-FO (Manual Phase) | Post-FO (Automation Phase) |
|---|---|
| Market tab: buy/sell is the primary action | Market tab: intel gathering + occasional manual trade to teach FO new routes |
| Intel tab: price data for planning | Intel tab: discovery leads + FO recommendations |
| No FO context | FO route annotations on managed goods |
| All goods equally prominent | Managed goods show "handled" indicator; unmanaged goods visually prominent (inviting discovery) |

**Market tab FO annotations:** When docked at a station that's on an FO
route, managed goods show a subtle FO annotation:

```
Ore         12 cr    [Route Beta: buying here → Proxima, +50%, Fresh]
Electronics 340 cr   [Route Delta: selling here ← Kepler, +22%, Aging]
Food        8 cr     — (no managed route — opportunity for player)
```

This connects the local dock experience to the empire-level automation
without requiring the player to open the Empire Dashboard. Matches CK3's
nested tooltip principle: context appears where you are, not in a
separate screen.

**Cost-basis display:** When the player has goods in cargo, the market
shows their buy price and current profit/loss per unit:

```
Ore     Cargo: 50    Bought: 2 cr    Here: 142 cr    Profit: +7,000 cr
```

This addresses the gap identified in first_hour_experience_v0.md: "No
cost-basis display — player can't see 'bought at X, sells for Y here.'"
The geography-is-money lesson requires visible margin. Elite Dangerous's
#1 community complaint is "I can't tell if this trade is profitable
without a calculator." We solve this.

### Route Query Interaction

*Addresses automation surprise: "Why did the FO do that?"*

The player can **inspect any managed route** on the dashboard or galaxy
map and hear the FO explain its current state in character:

```
[Player taps Route Delta on galaxy map]

Maren: "Route Delta. Running since tick 450. Current margin: 18%.
I rerouted through Node 9 last cycle — the direct path through
Chitin space has a 15% tariff surcharge right now. Node 9 adds
one hop but saves 12% on fees. Net gain."

Dask: "Route Delta's been steady. Had to swing wide around the
Chitin corridor — tariffs. Longer route but the numbers work."

Lira: "Route Delta — I pulled her off the main road when Chitin
started charging through the nose. She's running a back route
through Node 9 now. Quieter. Profitable."
```

This prevents the automation surprise problem (Sarter, Woods & Billings
1997) — every FO decision is explainable, in character, on demand. The
player never needs to wonder "why did my ship go THERE?" — they ask, and
the FO answers. This also serves trust calibration: seeing the FO's
reasoning builds confidence in delegation.

---

## Empire Dashboard & Dock Menu Architecture

### Dashboard Consolidation (5-Tab Model)

*Reference: CK3's information architecture — 5 top-level tabs, deep drill-down
within each. Stellaris lesson: 9 top-level tabs = player opens none of them.*

The current Empire Dashboard has 9 tabs (Overview, Trade, Production, Programs,
Intel, Research, Stats, Factions, Warfronts). The dock menu has 7 tabs (Market,
Jobs, Ship, Station, Intel, Haven, Diplomacy). That's 16 tabs across two
contexts — too many for a game where the player should be exploring, not managing.

**Consolidated Empire Dashboard: 5 tabs based on player mental model:**

| Tab | Contains | Old Tabs Merged |
|-----|----------|----------------|
| **Overview** | Credits, net income, empire health summary, FO status, recent events. The "glance" tab | Overview + Stats (summary) |
| **Empire** | Route list (with health/margin), fleet roster, active programs, construction projects, FO service record. Everything the FO manages | Trade + Production + Programs |
| **Exploration** | Knowledge Graph (dual-mode), discovery list, scanner status, active expeditions, anomaly chain progress | Intel |
| **Factions** | Faction standings, territory map, warfront status, diplomacy actions, treaties. All political context | Factions + Warfronts |
| **Research** | Tech tree, active research, artifact analysis, Haven science center. All progression | Research |

**Why 5 and not 9:**
- 5 is within Miller's 7±2 for chunked categories — the player can hold the
  full dashboard structure in working memory
- Each tab maps to a player question: "How am I doing?" (Overview), "What's my
  empire doing?" (Empire), "What have I found?" (Exploration), "Who are my
  allies?" (Factions), "What can I build?" (Research)
- Stats (detailed numbers) becomes a drill-down within Overview, not a
  top-level peer of Empire management
- Trade/Production/Programs are the FO's domain — the player thinks of them as
  one thing ("my trade empire"), not three

**Progressive tab visibility (preserved):**
- Overview: always visible
- Empire: visible after first managed route
- Exploration: visible after first discovery
- Factions: visible after first faction contact
- Research: visible after Haven research lab visited

**Dock menu (7 tabs) is unchanged** — dock tabs serve a different purpose
(local station interaction vs empire-level management). The dock menu answers
"What can I do HERE?" while the dashboard answers "What's happening EVERYWHERE?"

### Unified Terminology: "Route"

*Research: Norman (2013) Design of Everyday Things — conceptual model
consistency. If the same thing has two names, the player builds two mental
models for one concept.*

All player-facing UI uses **"Route"** as the single term for managed trade
paths. Internal code names (TradeCharter, Program, AutoBuy) never appear
in the UI.

| Context | Player Sees | Internal Name |
|---------|------------|---------------|
| FO offers to manage a trade path | "I'll set up a **route**" | CreateTradeCharterProgram |
| Dashboard route list | "**Route** Alpha: Kepler → Proxima" | ManagedRoute entity |
| Galaxy map route lines | "**Route** Delta (degraded)" | ManagedRoute.Status |
| FO dialogue about automation | "Your **routes** earned 840cr" | ProgramSummary aggregate |
| HUD Empire Health icon tooltip | "3 **routes** healthy, 1 degraded" | TradeManager.RouteHealth |
| Tutorial handoff moment | "Want me to run this **route**?" | ROUTE_OFFERED trigger |

**Never in player-facing UI:**
- "Program" — too abstract, sounds like software
- "TradeCharter" — too formal, sounds like a legal document
- "AutoBuy / AutoSell" — too mechanical, breaks FO immersion
- "Charter" — acceptable only in the fleet context ("chartered hauler" vs
  "owned ship"), never for the trade path itself

This aligns with AutomationPrograms.md's vocabulary section and prevents
the Elite Dangerous problem where "trade route," "commodity route," and
"trade program" all mean the same thing in different screens.

---

## Entity Model (Draft)

### FO Trade State (extends existing FirstOfficer entity)

```
FirstOfficer (existing)
+-- CandidateType: Analyst | Veteran | Pathfinder
+-- RelationshipScore: int
+-- DialogueTier: Early | Mid | Fracture | Revelation | Endgame
+-- BlindSpotExposed: bool
|
+-- TradeManager (NEW)
    +-- CompetenceTier: int (1-3: Novice, Competent, Master)
    +-- CrisesHandled: int (contributes to tier advancement)
    |
    +-- RouteKnowledge: Dictionary<string, RouteMemory>
    |   +-- RouteMemory
    |       +-- SourceNodeId, DestNodeId, GoodId: string
    |       +-- ObservedSpread: int
    |       +-- TimesPlayerTraded: int
    |       +-- FirstObservedTick, LastObservedTick: int
    |       +-- PlayerCorrections: List<string> (from Teaching Replay)
    |       +-- Status: Offered | Managed | Degraded | Dead | Rejected
    |
    +-- ManagedRoutes: List<ManagedRoute>
    |   +-- ManagedRoute
    |       +-- RouteId, ProgramId: string
    |       +-- ShipMode: Chartered | Owned
    |       +-- ShipId: string (if owned, empty if chartered)
    |       +-- OperatingCostPerCycle: int
    |       +-- TotalProfit: long
    |       +-- TotalRuns: int
    |       +-- LastMargin: int
    |       +-- ConsecutiveUnprofitableRuns: int
    |       +-- CreatedTick: int
    |
    +-- EmpireFleet: EmpireFleetState
    |   +-- OwnedShips: List<EmpireShip>
    |   |   +-- EmpireShip
    |   |       +-- ShipId, ShipName, ShipClassId: string
    |   |       +-- AssignedRouteId: string
    |   |       +-- Status: Active | Mothballed | Destroyed
    |   |       +-- PurchaseTick, PurchasePrice: int
    |   |       +-- ActiveTicks: int
    |   |       +-- IsVeteran: bool (ActiveTicks >= 500)
    |   |       +-- CargoDelivered: long
    |   |       +-- RevenueGenerated: long
    |   +-- CharteredCount: int
    |   +-- TotalUpkeepPerCycle: int
    |   +-- IsNetPositive: bool (for flip moment detection)
    |
    +-- AncientTechSlots: Dictionary<string, AncientTechUpgrade>
    |   +-- AncientTechUpgrade
    |       +-- ArtifactId: string
    |       +-- SourceDiscoveryId: string
    |       +-- EffectType: RouteEfficiency | MarketPrediction | ScannerRange | RouteReveal
    |       +-- EffectMagnitude: int (bps)
    |       +-- InstalledTick: int
    |
    +-- ConstructionProjects: List<string>
    |
    +-- SustainPlan: SustainPlanState
    |   +-- TrackedGoods: Dictionary<string, SustainGoodState>
    |   |   +-- SustainGoodState
    |   |       +-- CurrentStock, CyclesRemaining: int
    |   |       +-- SourceNodeId: string
    |   |       +-- Status: Healthy | Low | Critical
    |   +-- DivertedRouteCount: int
    |
    +-- LoreAnomaliesDetected: List<string> (feeds Knowledge Graph)
    +-- DiscoveryEmphasis: DiscoveryEmphasisType (set by CandidateType)
```

---

## The Player Experience, Start to Finish

### Tutorial (Tick 0-200)

Player learns to fly, dock, trade. 3 manual trades (pain before relief). Meets
all three FO candidates through scripted cameos. Selects FO based on
personality appeal. FO offers to take over Route 1 with Teaching Replay.
First charter runs. Player sees credits arriving while they fly somewhere new.

*"That's my route now, Captain. Go find us something new."*

### Early Game (Tick 200-600)

Player explores new systems. Each node visited is intel the FO can use. FO
occasionally surfaces: *"That mining colony you visited — I can set up a
supply run. 80cr/cycle net."* Player approves or ignores and keeps exploring.

Empire grows as a CONSEQUENCE of exploration. Player barely opens the
dashboard. FO reaches Competent after first sustain crisis. First warfront
disrupts routes — FO handles it, surfaces only if player input needed.

**Player is: exploring, discovering, fighting, learning factions.**
**FO is: building the trade network behind them.**

### Mid Game (Tick 600-1200)

Fracture module discovered. Player explores off-thread space — the frontier
only they can access. Ancient tech sites yield artifacts that upgrade FO
operations. Haven discovered; non-chosen FOs take up residence.

Haven Tier 2 unlocks ship purchases. First "buy" decision. Player names
their first Hauler. The flip moment: empire goes net-positive. FO delivers
the character beat.

Warfront escalations trigger crisis briefings. Player sets stances. Haven
crew offers dissent. FO begins detecting economic anomalies that feed the
Knowledge Graph — the pentagon ring's artificial nature starts emerging from
trade data.

**Player is: exploring fracture space, following anomaly chains, uncovering
the ancient mystery. Each discovery feeds the FO.**
**FO is: managing 5-8 routes, handling warfronts, detecting lore anomalies.**

### Late Game (Tick 1200-2000)

FO at Master level. Manages 10-15 routes autonomously. Player is deep in the
ancient mystery. FO's economic data has become lore evidence — the pentagon
ring is proven artificial through trade patterns.

FO notices the fracture module's effect on the player. Warfront crises require
strategic choices. Empire reflects the player's exploration path — heavy on
fracture-adjacent routes, shaped by faction alliances.

**Player is: piecing together the central mystery, making faction choices
that will determine the endgame.**
**FO is: providing the economic evidence, managing the empire that funds
the player's exploration, and noticing the player changing.**

### Endgame (Tick 2000+)

The player's trade network IS their endgame leverage. The empire built over
50+ hours determines which endings are available and what they cost.

The FO adapts the fleet to the chosen path. The final FO dialogue reflects
everything they built together:

```
FO: "We started with one shuttle and a hunch about food prices at
Kepler. Now we're running 15 routes across three faction territories
and a dimension that shouldn't exist. 'Steady Run' has been running
Route Alpha since tick 450.

Whatever you decide about the threads — I'm with you, Captain."
```

---

## Resolved Design Questions

### Player Role
**Resolved: Explorer-strategist, not empire manager.** The player's attention
is on the galaxy. The FO is peripheral — a trusted partner who surfaces only
when judgment is needed. "Han Solo, not Emperor Palpatine."

### FO Personality and Mechanical Impact
**Resolved: Flavor only for economics. Discovery emphasis for exploration.**
All three FOs reach the same economic endpoint. Personality affects
communication style. The ONE divergence: FO personality colors what
discoveries are noticed — different breadcrumbs through the same mystery.

### Manual Override
**Resolved: Always available.** The player can manually trade at any time.
The FO pauses managed routes when the player is active at those nodes.

### Multiple FOs
**Resolved: Single FO.** Non-chosen FOs fill Haven roles. All perspectives
available. Haven crew can voice dissent during major decisions.

### FO Mistakes
**Resolved: World failure, not FO incompetence.** The FO makes correct
decisions that produce bad outcomes because the galaxy is unpredictable.
Pirates hit between intel refreshes. Warfronts disrupt routes. The FO's
competence isn't in question; the galaxy's reliability is.

### FO Efficiency
**Resolved: 85-95% of optimal manual margin.** FO-managed routes must
perform well enough that players don't revert to micro-management (the
Stellaris sector lesson: if AI performs measurably worse, players ALWAYS
micro-manage and resent the system). The 5-15% gap comes from wider safety
buffers on margins and slower reaction to price changes (intel freshness
lag), not from poor decision-making. This gap is visible and explainable:
the FO will say *"I'm running wider margins on Route Delta because the
intel is 800 ticks old — tighter margins need fresher data."* Manual
override remains viable for players who enjoy trading, but should never
feel mandatory. The player's strategic skill is in choosing WHICH routes
to open, not in executing them more efficiently than the FO.

### Player Strategic Skill
**Resolved: Strategic, not operational.** The player's trade skill is
choosing where to explore, which routes to approve, when to buy ships,
and how to respond to warfront disruptions — strategic judgment. The FO's
skill is route execution, margin optimization, and logistics — operational
execution. During crisis briefings, the player makes strategic choices
("support Valorin" or "stay neutral"), not trade execution decisions
("buy 50 units of munitions at Node 7"). This means the Bainbridge
automation irony (player loses operational skills) is intentional — the
player never needed operational skills because the FO handles that layer.

### Save/Load
**Resolved: Straightforward.** `TradeManager` serializes with QuickSaveV2.

### Construction
**Resolved: Always contracted.** Credit-per-step projects, no construction
ships. Operational ships follow normal charter/own model.

### Market Reports
**Resolved: Background system, not physical items.** Visiting nodes
refreshes intel automatically. No inventory management. The player's
contribution is going places, period.

### Competence Model
**Resolved: 3 tiers, growth through crisis survival.** No domains, no XP
bars. The FO gets better at things as the player plays. Growth manifests
as dialogue and capability, not numbers.

### Negotiation
**Resolved: Cut.** Out of genre for an explorer-strategist game. Player's
skill is in finding opportunities and making strategic choices, not haggling.

---

## Success Metrics

| Metric | Target | How Measured |
|--------|--------|-------------|
| Player understands handoff in < 2 min | No tooltip needed | First-hour bot: time to first handoff |
| FO personality distinct | Identify FO from dialogue alone | Playtest: >90% accuracy |
| Module sustain never fails with FO active | 0 disables after Competent tier | Bot assertion |
| Player never opens program config screen | Zero manual CreateProgram calls | UI telemetry |
| Warfront briefings drive decisions | >70% result in player choice (not dismiss) | Analytics |
| Charter→own feels natural | First purchase tick 400-800 | Bot sweep |
| Flip moment is memorable | Players mention it in feedback | Playtest survey |
| Player keeps exploring during managed phase | 2+ new nodes between briefings | Analytics |
| Ship loss hurts | Players mention ship names | Playtest: "tell me about your fleet" |
| Ancient tech feels like exploration payoff | Players connect artifact to FO improvement | Playtest |
| Economic anomalies intrigue | Players investigate FO's lore observations | Analytics: Knowledge Graph entries from trade data |
| Discovery emphasis creates replayability | Players replay with different FO for new breadcrumbs | Analytics: second playthrough FO switch rate |
| Endgame path reflects trade history | Players feel trade choices mattered | Playtest: "did your empire affect the ending?" |
| FO relationship feels real | Players describe FO as partner, not tool | Playtest survey |
| FO notices fracture adaptation | Players discuss FO's observations about their changing | Community discussion tracking |
