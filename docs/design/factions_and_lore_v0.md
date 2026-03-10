# Factions & Lore Design v0

**Status**: RESEARCH COMPLETE — ready for review
**Last updated**: 2026-03-07

---

## Implementation Status (as of Tranche 20, 2026-03-08)

- Faction identities & territories: ✅ Implemented (FactionTweaksV0.cs, GalaxyGenerator.cs)
- Pentagon dependency ring: ✅ Implemented (FactionTweaksV0.PentagonRing)
- Warfront seeding (2 wars): ✅ Implemented (GalaxyGenerator.SeedWarfrontsV0)
- Embargo system: ✅ Implemented (EmbargoState.cs, GalaxyGenerator.SeedEmbargoesV0)
- Instability phases (5-phase model): ✅ Implemented (InstabilitySystem.cs, InstabilityTweaksV0.cs)
- Reputation system: ✅ Implemented (ReputationSystem.cs — tiers, decay, war profiteering)
- Warfront demand & supply: ✅ Implemented (WarfrontDemandSystem.cs — supply ledger, intensity shift)
- Faction tariffs/aggression: ⚠️ REDESIGN NEEDED — code values diverged from original design (see below)
- Adaptation fragments (12): 🔮 Future — Not Yet Implemented
- Haven starbase: 🔮 Future — Not Yet Implemented
- Resonance pairs: 🔮 Future — Not Yet Implemented
- Endgame paths (Reinforce/Naturalize/Renegotiate): 🔮 Future — Not Yet Implemented
- Metric bleed gameplay effects: 🔮 Future — Not Yet Implemented
- Lattice drones: 🔮 Future — Not Yet Implemented

---

## Core Premise

Star threads are not natural. They are **containment infrastructure** built by an ancient civilization to make interstellar civilization possible. Reliable measurement and reliable transit are the same problem — both require metric consistency across light-years. The threads solve both by suppressing spacetime's natural turbulence within fixed corridors. Every faction's identity is shaped by their relationship to this infrastructure.

The player discovers a **fracture module** — ancient accommodation technology that allows off-thread travel. No other faction has this capability. Using it has systemic consequences.

---

## What IS the Instability? — Metric Bleed

**The instability is the breakdown of consistent measurement.**

In stable space (within thread containment), one meter is one meter everywhere. One second is one second. Mass is mass. The threads enforce metric consistency — they are a coordinate system imposed on spacetime.

In unstable space, metrics drift. Not randomly, but by *leaking into each other*. Distance and time become entangled. Mass and volume decouple.

**Signature manifestation**: Objects in unstable space change their apparent properties depending on how you measure them. A cargo hold that reads as 100 tonnes by mass sensor reads as 80 by volume scanner. A journey predicted at 12 hours takes 9 — or 16. An ore sample assays as iron-rich Tuesday and silicon-rich Thursday, not because it changed, but because the measurement relationship is no longer fixed.

### Why This Works for a Trading Game

The entire economy is built on reliable measurement. Trade requires buyer and seller to agree on what "one unit of metal" means. Insurance requires calculable risk. Navigation requires fuel cost predictable from distance. **Metric bleed attacks the preconditions of commerce, not commerce itself.** You can still trade in unstable space — but margins are uncertain, manifests are unreliable, and price discovery becomes adventure rather than arithmetic.

### Metric Bleed and the Player's Instruments

During instability events, metric bleed affects the player's own UI — making the abstract concept tangibly felt through gameplay. The severity scales with local instability phase:

- **Phase 1 (Shimmer)**: Market prices display with +/- ranges instead of exact numbers. A good listed at "~48-52 credits" in Shimmer space would show as "50 credits" in Stable space. The player can still trade profitably but must account for uncertainty in their margin calculations
- **Phase 2 (Drift)**: Fuel cost estimates flicker between two values, settling on one when the player commits to a route. Cargo manifests occasionally show **phantom entries** — a line item for cargo the player doesn't have, which vanishes on the next UI refresh. Navigation ETAs drift by +/-15%, displayed as a wavering number rather than a solid readout. The player's hold total and credit balance may disagree by 1-3% between different UI panels
- **Phase 3 (Fracture)**: All numeric displays acquire visible instability — numbers render with a slight jitter, as if the digits themselves are uncertain. Buy/sell confirmations show the agreed price, then briefly flash an alternate number before settling. Route planning shows ghost routes that don't correspond to real connections — they appear for 1-2 seconds then dissolve. The HUD frame itself develops subtle geometric distortion at the edges
- **Phase 4 (Void)**: Per existing design, instruments paradoxically stabilize — see Phase 4 section. The player's sensors are the last tether to comprehensible reality

**Design principle:** Metric bleed UI effects NEVER falsify final transaction outcomes. The player always receives what they actually paid for. The unreliability is in the *prediction and display*, not in the *execution*. The player learns to distrust their instruments in unstable space — but the instruments, when committed to, still work. This preserves gameplay fairness while making instability feel viscerally real. The discomfort comes from uncertainty, not from being cheated.

### Why Containment Makes Sense

The threads are literally a coordinate grid. They impose a stable reference frame across spacetime — survey stakes driven into reality that define "here" and "now" consistently. Stable geometry between two points is inherently traversable geometry. The ancients didn't build roads and accidentally enable trade — they built infrastructure for civilization, which requires both consistent measurement and reliable transit. These aren't separate goals; they're the same physics.

### Visual Signature

**Parallax errors.** Objects in unstable space appear to shift position when you change viewing angle, as if existing at multiple distances simultaneously. The further instability has progressed, the more severe the parallax — at high levels, objects seem to smear across space as probability distributions. Stars appear to breathe. Station outlines double. Hull readings flicker.

### The Physics: Spacetime Foam Turbulence

Spacetime is not smooth. At the Planck scale (~10^-35 meters), it is a roiling foam of geometric fluctuations — virtual wormholes, topology changes, metric chaos. John Wheeler called it "spacetime foam" in 1957. At everyday scales this turbulence is invisible, the way ocean waves are invisible from orbit. But it is always there.

Within the thread network, foam-scale turbulence is actively suppressed — dampened to irrelevance by infrastructure that functions as **error correction on the geometry of space itself**. The threads detect metric fluctuations and correct them, enforcing geometric coherence within each corridor. The physical substrate being stabilized is the shape of compactified extra dimensions at each point in space. When that shape drifts, local physics changes — particle masses shift, force strengths wander, measurement becomes unreliable. That drift is metric bleed.

Over interstellar distances without suppression, foam-scale noise amplifies. Microscopic geometric inconsistencies compound into macroscopic measurement breakdown — the metric bleed that makes off-thread space hostile to civilization. This amplification is not linear. Error-correcting systems have a **threshold**: below a certain error rate, they correct indefinitely; above it, they collapse suddenly. This is why thread degradation is gradual for centuries and then catastrophic overnight. The Lattice is approaching its threshold.

The threads follow natural dark matter micro-filaments between star systems — gravitational channels in the cosmic web's local structure. The ancient builders didn't impose corridors on arbitrary space; they reinforced existing lines of gravitational coherence. This is why the thread network has the topology it does: the routes were already there in the dark matter scaffolding. The builders turned faint paths into highways.

**Two engineering responses to the same turbulence define the ancient schism:**
- **Containment**: Suppress the foam. Force metric consistency by brute-force error correction. Effective but expensive, requires constant maintenance, and fails catastrophically when the error rate exceeds the correction threshold. The thread network and the Lattice are containment engineering.
- **Accommodation**: Shape the foam. Instead of fighting turbulence, read its flow patterns and guide them into self-sustaining stable configurations — the way a riverbed shapes water into a predictable current without damming it. Cheaper, self-maintaining, but requires a deeper understanding of the underlying physics. The fracture module and the Haven are accommodation engineering.

---

## The Ancient Civilization

- Built the thread network and the Lattice maintenance system
- Experienced an internal schism: **Containment** (constrain spacetime AND civilization) vs **Adaptation** (enable independence from the infrastructure)
- Containment faction won. Adaptation faction's research was suppressed
- A single rebel/dissident ship carrying adaptation research was hidden — the player's fracture module comes from this lineage
- The civilization disappeared. The Lattice persists autonomously

### The Schism Was About Control, Not Just Physics

The surface-level narrative (which the player assembles first) frames Containment vs Adaptation as an engineering debate: suppress turbulence or shape it? Both approaches have technical merits. The player initially sides with Adaptation because the Lattice is failing and accommodation geometry clearly works (the Haven proves it).

The deeper truth (which the player discovers through gameplay before any data log confirms it): **Containment was never just about stabilizing spacetime.** The thread network doesn't only enforce metric consistency — it enforces *economic dependency*. The resource distribution patterns, the trade route geography, the faction codependencies that the player has been navigating for hours — these are containment infrastructure applied to *civilization*, not just physics. The threads constrain what species can produce, where they can trade, and who they must depend on. Independence is architecturally impossible within the thread system. That's not a side effect. It's the design.

The Adaptation faction's argument was therefore not merely "accommodation is better engineering." It was: **accommodation enables freedom.** Metric-variant space allows species to develop independently, find alternative resources, build self-sufficient economies. The Containment faction's response was not merely "that's risky engineering." It was: **freedom is dangerous because unconstrained civilizations do unpredictable things.**

The ancient schism was a political debate disguised as a scientific one. The data logs the player finds present it as science (because the scientists recording them experienced it as science). The truth — that it was always about whether civilizations should be trusted with self-determination — emerges from gameplay evidence, not from text.

### The Adaptation Faction's Argument

Metric bleed is not a disease. It is spacetime's natural state — the foam turbulence that exists everywhere, always. The threads don't "cure" instability — they suppress it, the way a dam suppresses a river. The water doesn't go away. It builds up. The Containment approach works, but requires perpetual maintenance, and the pressure behind the dam never stops growing. Every thread built creates a larger eventual failure point.

The Adaptation faction's alternative: don't dam the river — *read the current and shape a channel*. Turbulence can be guided into self-sustaining stable patterns without being suppressed. A whirlpool sustains itself. An eddy persists for millennia. The Haven system is a calm eddy in the foam — not suppressed, just *shaped right* — and it has been stable for millions of years with no Lattice, no maintenance, no infrastructure. That's the proof of concept the Containment faction refused to acknowledge.

### What the Rebel Ship Carried: Accommodation Geometry

The rebel ship carried research into **accommodation geometry** — structural and material designs that function correctly under metric bleed rather than requiring stable metrics to operate. Ordinary engineering assumes a beam built one meter long stays one meter long. Accommodation geometry designs the beam so it functions as structural support *regardless of what "one meter" currently means in local spacetime*. Function decoupled from metric properties.

The fracture module is an accommodation geometry engine. It doesn't fight instability or suppress it. It allows the ship to move through metric-inconsistent space because its navigation depends on **topology** (what's connected to what) rather than **geometry** (how far apart things are).

### Why This Is Sympathetic

The adaptation faction weren't reckless. They were engineers who looked at the math and concluded containment was a losing long-term strategy. They were right — the Lattice is degrading, and the ancients' containment eventually failed (they disappeared). The rebel ship wasn't carrying a weapon. It was carrying a backup plan.

The tragedy: the Containment faction won the political argument, suppressed the research, and disappeared anyway. The fracture module lineage is the only surviving alternative to an approach that already failed once.

### Naming the Ancient Civilization

There is no canonical name for the ancient civilization. They left no self-reference that modern species can decode — or if they did, no one has found it yet. Each faction uses their own term, reflecting their values:

| Faction | Their Term | Why |
|---------|-----------|-----|
| Concord | "the Founders" | Institutional respect for the builders of the order Concord protects |
| Chitin | "the Prior Distribution" | Probability framing — the ancients are the prior, modern species are the posterior |
| Weavers | "the First Engineers" | Builder recognizes builder |
| Valorin | "the Gone" | They left. That's what matters. Valorin are direct |
| Communion | "the Listeners" | Experiential framing — they listened to spacetime before anyone else |

**Dev doc convention:** Use "the thread builders" or "the ancient civilization" (lowercase, descriptive) as neutral references. Do NOT use "Precursors," "Forerunners," "Ancients" (capitalized), or any other genre-standard proper noun. These people had a name; we don't know it. That's more interesting than giving them a label.

**T3 tech tier:** "Relic" (not "Precursor"). These are found artifacts, not products of a named brand.

**In-game:** Faction NPCs use their own term. Data logs use no civilization name at all — the scientists don't identify themselves because why would they. The player never learns a "real" name. The absence of a name is itself a small mystery.

### The Scientists Behind the Data Logs

Five named scientists appear across the ancient data logs. Each has a position in the Containment-vs-Accommodation debate, but also a **personal contradiction** that makes them more than a talking point. These contradictions are revealed across multiple logs found at different sites — the player assembles each scientist's personality the way they assemble the larger mystery.

**Kesh** — Containment advocate. Cautious, methodical, senior.
- **Position:** The threads work. Accommodation is untested at scale. Don't gamble 400 billion lives on theory.
- **Contradiction:** Kesh *privately agrees with Vael*. Has known since cycle 3,800 that the resource distribution patterns are engineered, that containment constrains civilizations as well as spacetime. Supports containment publicly because admitting the truth would destabilize everything the threads protect. His caution isn't intellectual — it's moral cowardice he recognizes in himself. Late-stage logs show him arguing containment's case with visible exhaustion, defending a position he no longer believes in because the alternative terrifies him.

**Vael** — Accommodation advocate. Optimistic, passionate, junior to Kesh.
- **Position:** Accommodation geometry works. The threads are a crutch. Build something that doesn't need stable metrics.
- **Contradiction:** Vael hides fear. The accommodation geometry works beautifully in Phase 1-2 conditions. In Phase 3-4, where metrics break down completely, her models show... uncertainty she doesn't share. Her optimism is partially a front because admitting doubt would give the Containment faction ammunition. She pushes for accommodation knowing she's not sure it's safe at the extremes. Her private logs show calculations she never published, with margins of error she never discussed.

**Oruth** — Project lead. Pragmatic, managerial, responsible for final decisions.
- **Position:** The infrastructure must be maintained. Resource distribution optimization is necessary.
- **Contradiction:** Oruth approved the economic engineering (the pentagon dependency ring) knowing it was ethically wrong. Rationalized it as necessary infrastructure maintenance — "the lattice needs users." Privately keeps a personal log arguing against his own decision. The "topology optimization" conversation with Senn is Oruth performing approval while internally screaming. His later logs are increasingly terse, the language of a person who has stopped justifying and started enduring.

**Senn** — Experimentalist. Designed the pentagon dependency ring's resource distribution.
- **Position:** Elegant engineering solves the maintenance problem. Circular dependency makes the lattice self-funding.
- **Contradiction:** Genuinely amoral — not evil, but incapable of seeing systems as having moral dimensions. When others call the pentagon ring "a cage," Senn is hurt: "It's infrastructure maintenance." Designed economic containment the way an architect designs load-bearing walls — as a structural necessity, not a statement about freedom. The most unsettling character in the logs because Senn sleeps well. The player who reads enough of Senn's work realizes: the most dangerous people aren't the ones who choose evil. They're the ones for whom the question of evil simply never arises.

**Tal** — Builder. Infrastructure engineer. Built the physical thread network.
- **Position:** The threads are good. They work. Maintain them.
- **Contradiction:** The most emotionally invested. Tal built the thread network (metaphorically) with their own hands. Watching it degrade is watching their life's work die. Their later logs are grief-stricken — not about the political debate, but about the physical infrastructure failing. The most likely scientist to WANT to address the future, which makes their silence (per design rules, logs are never addressed to posterity) the most poignant absence. Tal clearly wanted to leave a message for whoever comes next. They didn't. That restraint — or that inability to find the words — is more haunting than any message would have been.

**Design rule:** No single log should reveal a scientist's full contradiction. The player assembles each personality across 4-6 logs found at different sites. Early logs show the public position. Later logs reveal the private truth. The player who finds only two Kesh logs sees a cautious scientist. The player who finds five sees a man trapped in his own lie.

### The Module Adapts the Pilot

The fracture module's "accommodation process" is not limited to the ship's hull. It includes **neurological adaptation** of the pilot.

The "hull stress" from fracture travel is partially a proxy for perceptual recalibration. The UI effects the player sees in unstable space — chromatic aberration, parallax, spatial distortion — are not degraded perception. They are the pilot **seeing space more accurately** than stable-space beings can. Normal space, within containment, is the simplified view — a low-resolution rendering of reality imposed by the thread infrastructure. The "distortion" in metric-variant space is high-fidelity.

The module doesn't need a ship. It needs a **mind** that can perceive metric-variant space and make decisions in it. The ship is the vehicle. The pilot is the real accommodation geometry.

This raises an unresolvable question at the endgame: has the module shaped the pilot's thinking as well as their perception? Are the player's endgame choices truly their own — or has accommodation geometry guided them toward the choices it needs them to make? The game does not answer this. It lets the player sit with it.

**Design rule:** This is NEVER stated explicitly in-game. The player may infer it from evidence: the UI effects feel less "wrong" over time, a Communion elder remarks that the player "sees like we do now," a data log describes the original Adaptation scientists developing "metric perception" through prolonged exposure. The conclusion is the player's to draw.

### Mechanical Expression of Module Adaptation

As the module accumulates fracture exposure, its integration with ship systems deepens. These improvements manifest as subtle instrument refinements that the player notices through gameplay rather than through any explicit notification:

- **Scanner precision**: Scan results start with +/-5% reading variance at low fracture exposure. After 20+ cumulative fracture jumps, variance tightens to +/-3%. After 50+ jumps, to +/-2%. The player's scan numbers become more consistent — they may notice this when comparing early and late scans of the same node, or when their trade calculations start matching actual outcomes more precisely
- **Navigation predictions**: Travel time estimates for fracture jumps start with a wide uncertainty window (shown as a range, e.g., "ETA: 9-15 ticks"). As cumulative exposure grows, the range narrows. After significant fracture travel, ETAs become nearly exact. The module is learning the foam's flow patterns through the pilot's accumulated experience
- **Cargo manifest optimization**: After 30+ fracture jumps, cargo manifests begin showing subtle suggestions — a faint highlight on goods whose metric properties are favorable for the player's current route, or a barely-visible indicator on cargo that will measure heavier at the destination. These hints are never labeled or explained. They simply appear, as if the manifest display has become slightly smarter
- **Instability reading clarity**: The visual distortion effects in unstable space gradually shift from chaotic to structured. At low exposure, shimmer-zone effects look like noise. At high exposure, the same effects resolve into readable patterns — flowing lines rather than static, directional movement rather than random flicker. The instability hasn't changed. The player's perception of it has

**These improvements are NEVER explained to the player** — they simply notice that their instruments are getting better, their estimates are getting tighter, their interface seems more helpful. This is the module adapting to its pilot. Players who compare notes will discover they have different calibration levels depending on their fracture travel history, creating organic "my ship is different from yours" conversations.

---

## Why the Fracture Module Can't Be Copied

The module was **grown, not built**. Its internal structure was cultivated in unstable space over centuries of controlled exposure to metric bleed. The material lattice adapted to metric inconsistency the way coral adapts to ocean currents — not by design, but by iterative response to environmental pressure.

Four reasons for irreproducibility:

1. **The substrate doesn't exist anymore.** The base material was engineered using fabrication techniques requiring stable metrics at extreme precision. Paradoxically, the material was designed to *operate* without stable metrics but *manufacturing* it required stability the ancients had but no modern faction can replicate.

2. **Growth conditions can't be replicated safely.** Growing accommodation geometry requires prolonged, controlled exposure to metric bleed — exposure that destroys conventional instruments and kills unprotected crews. The rebel ship was a cultivation vessel with life-support accommodation designs that survive nowhere in modern databases.

3. **The module has adapted to the specific player ship.** It continues its accommodation process, adapting its internal geometry to the hull it's installed on. Removing and reinstalling would require decades of re-accommodation.

4. **Measurement defeats itself.** The module works by being metric-invariant. Any instrument precise enough to analyze its structure introduces a stable reference frame that *changes the thing being measured*. A macroscale uncertainty principle: you can know what the module does, or how it's built, but not both — because precise measurement imposes the metric consistency the module is designed to be independent of.

---

## The Lattice

- **Not a faction** — a distributed maintenance system
- Autonomous nodes that monitor and repair thread infrastructure
- No political agenda, no territory, no diplomacy
- Factions interact with it (Concord protects it, Chitin scavenges from it, etc.)
- Its effectiveness is degrading over time — this is the ticking clock

### Lattice Drones as Escalating Threat

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

As the Lattice degrades, its maintenance drones malfunction. They were repair
bots; now they attack anything that moves in deteriorating thread segments.

- **Phase 0-1 (Stable/Shimmer)**: Drones are passive. Visible at Lattice
  nodes, repairing infrastructure. Concord patrols avoid disturbing them.
- **Phase 2 (Drift)**: Drones become territorial. They defend Lattice nodes
  aggressively — attacking ships that approach too closely. Not hostile on
  sight, but dangerous if you stray near maintenance infrastructure.
- **Phase 3 (Fracture)**: Drones are fully hostile. They patrol thread segments
  adjacent to failing nodes, attacking any ship. Their numbers increase as
  more nodes degrade. They cannot be reasoned with, bribed, or intimidated.
- **Phase 4 (Void)**: Drones are absent. Whatever they were maintaining no
  longer exists here.

**Why this works**: The Lattice is not a new faction bolted onto the threat
model. It is the existing infrastructure developing teeth as it fails. The
ticking clock is not just economic (thread degradation → trade disruption) — it
is physical (the repair bots that kept you safe are now trying to kill you).
Drone density scales with instability, creating a natural difficulty gradient
that the player can see and plan around.

**Gameplay interaction**: The Adaptation Fragment "Lattice Reading" (Fragment
6) allows the player to interact with Lattice nodes for temporary
stabilization — calming drones in the area. This creates a unique tool for
navigating degraded space that no faction possesses.

---

## Factions (5 + Lattice)

### Concord (Order)
- **Role**: Preserve the existing thread infrastructure and political order
- **Trade Policy**: Open | **Tariff**: 0.08 (low, flat) | **Aggression**: 1 (defensive)
- **What they want**: Stability, predictability, functioning trade
- **What they're wrong about**: The threads can be maintained indefinitely
- **Endgame alignment**: Reinforce

**Why Concord Is Genuinely Good**: Concord runs the galaxy's humanitarian infrastructure. When a Communion waystation runs low on Food, Concord dispatches relief convoys — at cost, not for profit. When a Valorin frontier settlement faces piracy, Concord patrols respond even though the Valorin didn't ask and won't say thank you. Concord subsidizes Food production to keep prices stable for the poorest stations. Their medical supply chains reach every corner of thread-space. The player sees this directly: Concord stations are comfortable, well-stocked, and welcoming because Concord genuinely invests in making them that way. The bureaucracy is real, but so is the public service behind it.

**The Secret**: Concord knows the threads are failing and has known for decades. Internal intelligence reports show Lattice degradation curves predicting cascading thread failure within a century. They have suppressed this information because publishing it would cause the panic and territorial scramble that would accelerate collapse. Their public position ("the threads are eternal") is a conscious, deliberate lie told for what they believe are the best possible reasons.

**Why the betrayal must hurt**: The player who discovers Concord's suppression should feel the way you feel learning a parent lied to protect you. Not "I knew they were corrupt." Instead: "They did real, visible, selfless good — and they were also hiding the most important truth in the galaxy." The betrayal lands because the good was genuine, not a facade. Concord's humanitarian convoys are real. The lie about thread stability is also real. Both are true simultaneously. That's harder than a villain unmasking.

Three layers:
1. **Public face**: Genuine public servants. Open trade, fair tariffs, humanitarian relief, subsidized food. Not a mask — this is who most Concord people actually are.
2. **Private reality**: A small intelligence directorate that monitors and suppresses information about thread degradation. They classify fracture data, redirect Lattice researchers, maintain informational control. An engineering division actively works to reverse-engineer ancient technology and build replacement infrastructure. Most Concord citizens don't know about either program.
3. **Genuine belief**: The directorate thinks they are buying time. Every year of stability is a year of research toward an engineering solution. They are not cynics. They are idealists who made a terrible compromise and have been living inside it for decades.

**The blindspot**: They've been buying time so long that buying-time became the strategy. They have no plan for what happens if the engineering solution doesn't arrive. Their engineering division is specifically **techno-optimist in a setting where that optimism is dangerous** — they see the player's fracture module and think "mass production," not "sacred artifact" or "existential risk."

**Player experience**: Concord is the best place to trade early game — open markets, low tariffs, predictable pricing, visible humanitarian operations. The player should genuinely like and respect Concord before any cracks appear. As reputation grows, the player gets intelligence briefings revealing degradation data. At max rep, the player discovers the suppression — and must decide whether Concord was wrong, or whether the lie saved more lives than the truth would have.

**Personality traits**: Obsessive record-keeping. Genuine hospitality (most comfortable stations in the game). Visible public service (relief convoys, medical supply chains, subsidized goods). Bureaucratic euphemisms that the player only recognizes as euphemisms *after* the revelation ("thread optimization events" = thread failures — but the player heard this for hours without suspecting anything because Concord's competence made it sound routine).

**Gameplay niche**:
- **Produces**: Food (stable, subsidized), Components (precision manufacturing — civilizational specialty), Munitions
- **Needs**: Composites (from Weavers — can't armor fleet without them)
- **Ships**: Balanced cruisers/frigates. Higher BaseShield. Highest SlotCount for weight class — the ship is a platform; modules make it what you need. Coast guard with engineering flexibility
- **Tech focus**: Sensor, Shield, and Utility modules. Unique: "Regulatory Transponder" — zero tariffs and best prices at Concord stations, but position trackable by Concord patrols. Transparency for advantage. Secondary unique: "Universal Mount Adapter" — converts one weapon slot to utility or vice versa (engineering heritage)
- **Max rep unlock**: Concord patrol escorts for convoys. Intelligence briefings reveal all thread degradation locations. *And the truth about the suppression program.* Engineering division partnership — submit Salvaged Tech and Exotic Matter for analysis, yielding unique cross-faction module blueprints (slow: 100+ ticks). Engineering analysis of fracture module reduces hull stress from fracture travel

### Chitin Syndicates (Adaptation)
- **Species**: Beetle-like, metamorphic lifecycle
- **Trade Policy**: Guarded | **Tariff**: 0.12 (variable — fluctuates every 50 ticks) | **Aggression**: 0 (peaceful)
- **What they want**: Optionality — never be locked into one system
- **What they're wrong about**: You can always hedge your way out of existential risk
- **Endgame alignment**: Naturalize (pragmatically)

**Biology -> Philosophy**: Holometabolous insects undergo complete metamorphosis — during the pupal stage, the organism *literally dissolves its own body* and reconstructs from imaginal discs. The pre-pupal individual is a different being than the post-pupal one. Every Chitin has personally experienced total self-dissolution and reconstitution. Their concept of "self" is a probability distribution across possible selves they could have become. Gambling isn't vice — **making bets is how you test your model of reality**. A bet forces you to quantify uncertainty. Anyone who refuses to bet has never honestly confronted uncertainty.

The elytra principle extends to economics: maintain a hard shell of guaranteed survival around volatile core capabilities. Never go all-in (the elytra protect), never play entirely safe (flight wings under elytra need freedom).

Chemical communication -> syndicate structure: information flows through market signals, not commands. No central authority tells a Chitin trader what to do; they read the market the way ancestors read the wind.

Molt cycle drinking: they metabolize ethanol-analogues to stabilize neurochemistry during metamorphic transitions. Their bars are **metabolic support infrastructure** that doubles as social space. Offering a drink = offering trust.

**Gameplay niche**:
- **Produces**: Electronics (fracture-border processing hubs), Munitions
- **Needs**: Rare Metals (from Valorin frontier territories)
- **Ships**: Fast clippers/corvettes. Highest ScanRange. Low armor, moderate shields. "See everything, commit to nothing"
- **Tech focus**: Scanner and Engine modules. Unique: "Probability Engine" — 15% chance each tick of consuming zero fuel for travel. At max rep, also scrambles cargo manifest data during inspections
- **Max rep unlock**: Real-time price data for all Chitin stations. Chitin probability models for thread degradation. Access to futures contracts (pre-purchase goods at today's price for delivery in 100 ticks)

**Trading with Chitin — The Casino Floor:**

Chitin stations should feel like trading floors — fast, volatile, exciting. Every transaction is a bet. The mechanics reinforce this:

- **Bet-framing**: Chitin present every trade as a wager with stated odds. Instead of "Buy 10 Electronics for 500 credits," the UI shows "Wager: 500 credits that Electronics hold value. Current odds: 3:2 in your favor." This is cosmetic framing with real information — the "odds" encode the Chitin's own probability model for whether the good's price will rise or fall in the next 20 ticks. Reading the odds IS reading the market
- **Spread offers**: Buy/sell prices at Chitin stations are displayed as spreads rather than fixed numbers (e.g., "Electronics: BUY 48-52 / SELL 55-61"). The actual price within the spread is determined at transaction time based on current volatility. Higher Chitin reputation narrows the spread — at tier 1 the spread is +/-8%, at max rep it tightens to +/-2%. The Chitin reward traders who've proven they can read the market
- **Hot goods rotation**: Every 15 ticks, Chitin stations designate 1-2 goods as "hot" — displayed prominently with outsized margins (2-3x normal profit potential). Hot goods change unpredictably but follow patterns that observant players can learn (goods that were cold for 30+ ticks tend to go hot; goods that just went hot rarely repeat within 45 ticks). Chasing hot goods is the Chitin's invitation to gamble — and like all good gambling, the house edge is thin enough that skilled players profit while impulsive ones break even

*The net effect: Chitin stations feel alive — prices in motion, opportunities flashing, the UI itself vibrating with the energy of a trading pit. The player who visits a Chitin station and a Weaver station back-to-back should feel like they've traveled between two different genres of game.*

### Weavers (Structure)
- **Species**: Spider-like, silk-based engineering
- **Trade Policy**: Guarded | **Tariff**: 0.15 (high, non-negotiable) | **Aggression**: 1 (defensive)
- **What they want**: To be the ones who build whatever comes next
- **What they're wrong about**: Structure is neutral — the builders should decide what gets built
- **Endgame alignment**: Reinforce or Renegotiate (depends on who's paying)

**Biology -> Philosophy**: Orb-weavers produce up to seven types of silk with distinct mechanical properties. They sense the world through vibration — every strand is a sensor. Ambush predators whose survival strategy is *building the right structure and waiting*.

Weavers think in tensions, not positions. A web is a stress diagram — every node exists because of forces acting on it. They see the thread network as a stress management system where every thread bears load that would otherwise destabilize the region. When a Weaver says "I should build this," they mean "I can feel the stress pattern and I know where the load-bearing points are."

Vibration sensing = sensing changes in thread network traffic patterns, load fluctuations, maintenance drift through almost proprioceptive awareness. They don't just use infrastructure; they feel it. Deeply uncomfortable when anyone introduces forces the network wasn't designed to handle.

Patience of ambush predation -> trade policy. They don't chase opportunities. They build structures and wait for opportunities to come. "Guarded" not because hostile, but because they've positioned themselves at chokepoints. You trade through their infrastructure, on their terms.

Seven types of silk = seven types of infrastructure service: shipyards, thread repair, station construction, orbital habitats, cargo handling, communications relays, defensive installations.

**Gameplay niche**:
- **Produces**: Composites (silk-based manufacturing, best in known space), Metal
- **Needs**: Electronics (from Chitin — sensor-web maintenance systems)
- **Ships**: Tanky haulers/cruisers. Highest BaseZoneArmor, lowest speed. Mobile stations, not nimble fighters
- **Tech focus**: Armor and Hull modules. Unique: "Silk Lattice Reinforcement" — when armor on any zone reaches 0, automatically redistributes 10 armor from highest-armor zone. Structure adapts load
- **Max rep unlock**: Weaver Drydock Access — commission Weaver-built hull variants (+2 module slots, +30% armor, -15% speed). Contracts to repair Lattice nodes (direct interaction with thread degradation mechanic)

**Trading with Weavers — Patience Rewarded:**

Trading at Weaver stations should feel fundamentally different from other factions. The Weavers are ambush predators — they build and wait. Their market mechanics reflect this:

- **Delayed price updates**: Weaver markets update prices every 10 ticks instead of every tick. Prices hold steady between updates, then shift in larger increments. Players who learn the Weaver market cycle can time their arrivals to buy just before a favorable shift or sell just after one. Patience is rewarded — impulsive traders get average prices, attentive traders get excellent ones
- **Returning-customer bonus**: After 3+ trades at the same Weaver station, the player receives a "Thread-Bonded" modifier: -3% tariff per tier (stacking up to -12% at 4+ visits). The Weavers remember who uses their infrastructure reliably. This bonus resets if the player trades at a competing Weaver station within 50 ticks — loyalty to a specific node, not just the faction
- **Thread-reading predictions**: Weaver stations display price predictions for goods at neighboring systems — and unlike other factions' noisy estimates, Weaver predictions are accurate to within 2%. The Weavers feel vibrations in the trade network the way they feel vibrations in their webs. At higher reputation, predictions extend to 2-hop neighbors. This makes Weaver stations the best place to plan multi-stop trade routes — even if you buy nothing, docking at a Weaver station for intelligence is valuable

*The net effect: Weaver stations feel like visiting a master craftsman's workshop. Everything is deliberate. Nothing is rushed. The player who matches the Weavers' tempo — returning regularly, trading patiently, reading the predictions — earns margins no other faction offers.*

### Valorin Clans (Expansion)
- **Species**: Small-bodied, rodent-like (hamster adjacent), neurologically fearless
- **Trade Policy**: Open | **Tariff**: 0.03 (minimal) | **Aggression**: 2 (hostile to strangers, loyal to friends)
- **What they want**: New space, new worlds, room to grow
- **What they're wrong about**: There's always more frontier — expansion solves everything
- **Endgame alignment**: Naturalize

**Biology -> Philosophy**: Rodents are ~40% of all mammal species. Success from: rapid reproduction, burrowing (microclimates independent of surface), high metabolism, whisker-based spatial awareness in darkness, hoarding.

Fearlessness is not bravery — it's a **neurological adaptation to living in darkness**. Ancestors navigated pitch-black burrow networks via whiskers. They evolved without seeing threats before they arrived. Their amygdala-equivalent processes threat without producing paralysis. They feel danger, they don't feel dread.

Other factions look at off-thread space and see cosmic darkness full of unknown threats. Valorin see a burrow they haven't mapped yet. Not ideology — perceptual. They literally do not process "unknown space" as threatening.

Rapid reproduction -> clan structure and frontier settlement. A clan that settles has a viable population within a generation. Not reckless — a species-level strategy that works because individual failure rate is acceptable with many simultaneous attempts.

Hoarding -> supply chain philosophy. Distributed caches, not centralized warehouses. Multiple small stores, never one big one. Resilient to disruption but "messy" by Concord standards.

High metabolism -> economic pace. Need to eat more, move more, trade more. Highest goods turnover. Most willing to run marginal routes.

**Military threat — the swarm**: Valorin aggression 2 does not mean strong ships. It means **many ships**. Individual Valorin corvettes are the weakest in the game. But they appear in groups of 8-12. Entering Valorin territory without Blood-Kin status triggers a swarm response — one corvette is nothing; twelve appearing on your sensors is terrifying. Zerglings, not Ultralisks. The player can kill one. The player cannot kill twelve.

**Gameplay niche**:
- **Produces**: Rare Metals (settle where no one else will), Ore (raw extraction)
- **Needs**: Exotic Crystals (from Drifter Communion — frontier sensor calibration for increasingly unstable expansion space)
- **Ships**: Cheap shuttles/corvettes. Lowest individual stats but highest NPC density (3-4x other factions). They swarm
- **Tech focus**: Engine and Cargo modules. Unique: "Cache Beacon" — establish hidden cargo caches at any node, invisible to other faction scans. Hoarding instinct as technology
- **Max rep unlock**: Clan Blood-Kin status — Valorin share frontier maps (extended visibility), NPC fleets become escorts, access to distributed cache network across frontier space

### Drifter Communion (Understanding)
- **Species**: Human
- **Trade Policy**: Open (but chaotic — inventory unpredictable) | **Tariff**: 0.02 (nearly zero) | **Aggression**: 0 (peaceful)
- **What they want**: Direct, embodied knowledge of what instability actually is. Not through sensors — through exposure
- **What they're wrong about**: Individual experience scales to civilizational wisdom. A pilot who "feels" the drift is not a theory of drift
- **Endgame alignment**: Renegotiate (the player's natural ally for the third path)

Anti-institutional in a setting full of institutions. The only faction that actively seeks out instability. Their "spirituality" is the insistence that embodied experience is a valid epistemology. Think Le Guin's Anarresti or The Expanse's Belters — people whose identity comes from living in the margins.

**Shimmer-zone edge-dwellers**: The Communion lives in Phase 1 (Shimmer) space — the boundary between stable and unstable. They harvest phase-locked crystals from shimmer-space asteroid fields, wade ankle-deep into instability, and interpret sensor jitter as profound experience. They do NOT have fracture capability. They are edge-dwellers, not divers. The player is the one who dives.

This sharpens the contrast: the Communion's "spirituality" comes from wading in the shallows and finding it transformative. When the player arrives with fracture capability and descriptions of Phase 3-4 space, the Communion is simultaneously awed and terrified — the player has gone deeper than any Communion pilot has ever been or could survive.

**The Communion's Secret**: The Communion is not as innocent as it appears. Generations of shimmer-zone dwelling have given them knowledge they don't advertise — they can read accommodation geometry signatures the way a naturalist reads animal tracks. When the player first docks at a Communion station, the Communion **recognizes the module's signature immediately**. They know what it is. They know what it means. They say nothing.

But this is NOT because they selected the player. **The Communion has seen this before.** Every few generations, someone stumbles into a piece of accommodation geometry. A Valorin scout pulls a strange device from a frontier wreck. A Chitin researcher buys an anomalous artifact at auction. A nameless pilot finds a derelict. The Communion recognizes the signature. They always do.

They've learned — through painful experience — that interference doesn't help. The last pilot they tried to warn went deeper to prove she could. The one before that sold the artifact to Concord Intelligence, who classified it and dismantled it for study. The Communion has a word for these people: *threshold-crossers*. Most don't last long. Some come back changed. A few simply vanish into deep fracture space.

So the Communion does what they always do. They help when asked. They share shimmer-zone navigation data — which they share with *everyone*, because that's their culture. They offer hospitality — which they offer to *everyone*, because that's who they are. The warmth is real. The gratitude ("you're the one who kept Waystation Kell alive") is real. **They are not cultivating the player. They are being themselves** — while also watching, because they always watch threshold-crossers, and hoping, because they always hope.

**The reveal at max rep** is the simplest, most devastating moment in the game. A Communion elder tells the truth directly: "You're not the first. Every few generations, someone finds a piece of it. We recognize it. We always do. We've learned that telling people what they carry doesn't help — the last one who knew went deeper to prove she could. So we watch. We help when you ask. We hope. You've gone further than any of them." No drama. No betrayal music. Just calm honesty — after every other faction has been hiding things, one faction simply tells the truth. And what they reveal is not a conspiracy but a pattern: you are the latest in a long line of threshold-crossers, and the Communion has been quietly mourning most of your predecessors.

**Why this works narratively**: The player is NOT special. Not chosen, not destined, not selected by wise elders. They are a statistical inevitability — someone was always going to find a piece of accommodation geometry eventually. The devastating question is not "was I chosen?" (hero narrative) but **"am I repeating a pattern?"** What happened to the others? Will the player end the same way? The Communion doesn't know. They've never had a threshold-crosser go this far. Their hope is genuine — and fragile, because every previous hope ended in loss.

**Gameplay niche**:
- **Produces**: Exotic Crystals (harvested from shimmer-zone asteroid fields at the boundary of stable space), Salvaged Tech (entire economy is recovery and reuse)
- **Needs**: Food and Fuel (from Concord — can't sustain themselves without thread-space supply lines)
- **Ships**: Scout shuttles/clippers. Highest ScanRange, lowest Mass, minimal cargo/armor. Eyes, not fists
- **Tech focus**: Scanner and Navigation modules. Unique: "Metric Harmonics Array" — fracture travel fuel -25%, reveals additional void site details
- **Max rep unlock**: Communion Pathfinder status — Drifter navigators join crew (reduced hull stress, hazard warnings during fracture travel). Most critically: observations about patterns in instability — first data pointing toward Renegotiate path

### Communion Outreach — They Find You

The Communion does not wait for the player to visit Communion space. Once the player's Communion reputation reaches **Curious (tier 2)**, Communion representatives begin appearing at non-Communion stations across the galaxy. This mechanic reinforces the Communion's theme of connection and omnipresence — they are the faction that reaches out rather than drawing you in.

**How it works:**
- At tier 2 (Curious), a Communion contact appears at one non-Communion station the player has visited recently. They offer **fragment trades** — exchanging shimmer-zone data or Exotic Crystals for goods the Communion needs (Food, Fuel)
- At tier 3 (Trusted), contacts appear at up to three stations. They add **cryptic warnings** about instability patterns in nearby systems — vague enough to preserve mystery, specific enough to be actionable ("The shimmer readings near Kell Station feel... wrong. Like they did before the Drift event at Waystation Lara")
- At tier 4 (Pathfinder), contacts appear at any station the player docks at. They offer **invitations to visit Communion research sites** — specific void-adjacent locations where the Communion has been studying instability patterns for generations. These sites contain unique trade opportunities and fragment-adjacent lore

**Why this works:** Most factions require the player to come to them. The Communion comes to you. This makes them feel like a network rather than a territory — consistent with their nature as drifters. It also ensures the player encounters Communion narrative content even if they never prioritize visiting Communion stations, keeping the Renegotiate path accessible to players who discover it organically through Communion contacts at their regular trade stops.

---

## Faction Relationships — Pentagon Dependency

### Primary Ring

| Faction | Needs from | Good type |
|---------|-----------|-----------|
| Concord | Weavers | Composites (infrastructure materials) |
| Weavers | Chitin | Electronics (sensor-web maintenance) |
| Chitin | Valorin | Rare Metals (frontier resources) |
| Valorin | Drifter Communion | Exotic Crystals (frontier sensor calibration) |
| Drifter Communion | Concord | Food, Fuel (baseline survival) |

Every faction depends on someone they philosophically disagree with.

### Secondary Cross-Links (Web)

The primary ring is the backbone. Secondary needs create cross-links that add
arbitrage depth and prevent single-link fragility:

| Faction | Primary Need (ring) | Secondary Need (web) |
|---------|-------------------|---------------------|
| Concord | Composites (Weavers) | Rare Metals (Valorin — military hardware) |
| Weavers | Electronics (Chitin) | Exotic Crystals (Communion — lattice resonance tuning) |
| Chitin | Rare Metals (Valorin) | Composites (Weavers — hull materials for clipper fleet) |
| Valorin | Exotic Crystals (Communion) | Components (Concord — precision engineering) |
| Communion | Food, Fuel (Concord) | Metal (Weavers — station hull patching) |

The player learns the ring first through early trade. Cross-links emerge as
the player visits more stations and sees secondary buy orders with smaller
volumes but viable margins.

**Reputation tension mechanic**: When you trade with Faction A at a contested node, their rival in the dependency chain loses 1 reputation. Pure trading slowly raises everyone, which is too neutral — cross-faction tension creates meaningful choices.

### The Deeper Truth: The Ring Is Engineered

> This section describes the game's central narrative twist. The player discovers this through **gameplay** (fracture-space trade that breaks the ring pattern) before any data log confirms it.

The pentagon dependency is not natural. It was **designed** by the same intelligence that built the threads.

The thread network doesn't just enforce metric consistency between stars. It shapes resource distribution patterns, trade route geometry, and production capabilities such that **no faction can achieve self-sufficiency**. The Communion can't grow Food because their shimmer-zone soil doesn't support agriculture — not because shimmer-space prevents farming, but because the thread infrastructure's metric corrections actively suppress the biological processes that would allow it. The Valorin can't manufacture Electronics because the precision required depends on metric stability the frontier doesn't have — but metric stability is something the threads *choose* to provide to some regions and not others.

Every link in the pentagon ring is a constraint imposed by the infrastructure, not an emergent property of the species. The factions evolved their economies within a cage. They think the cage is the natural shape of the galaxy.

**How the player discovers this**: NOT through a data log. Through gameplay. The player establishes a fracture-space trade route to a Communion station. In metric-variant space, free from thread infrastructure, the Communion station begins producing its own Food using techniques that work *because* local metrics are variable, not despite it. The dependency breaks. And things are... fine. The station thrives. The pentagon ring was never a natural law. It was a rule that only applied inside the cage.

Data logs found later CONFIRM what the player already observed:
- Ancient engineering documents describing "economic topology optimization" — designing resource distribution to ensure no species cluster achieves productive independence
- Arguments between Containment scientists about whether constraining civilizations was ethical ("We're not protecting them from instability. We're protecting the infrastructure from them.")
- The Adaptation faction's realization that accommodation geometry doesn't just enable off-thread travel — it enables off-*system* economics

**Why this twist works for STE specifically**: No other game can deliver this revelation because no other game has the player spend 15+ hours running trade routes within the very system that turns out to be the cage. Every Composites delivery to Concord, every Rare Metals haul to Chitin — the player was maintaining containment infrastructure through commerce without knowing it. The twist recontextualizes *gameplay*, not just backstory.

**Implementation priority: #1.** This twist is the game's single strongest narrative asset — the one that only a trading game can deliver. It must be implemented with maximum fidelity: the gameplay trigger (Communion station produces Food in fracture space), the UI notification, the moment the player realizes what they've been doing for 15 hours. Every other revelation can land at 80% and still work. This one needs 100%. See `NarrativeDesign.md` → Recontextualization 3 and `LoreContent_TBA.md` → LORE.PENTAGON_EVIDENCE for delivery architecture.

**Why this creates genuine moral complexity**: The cage kept species alive. Remove the dependency and the Valorin might consume everything. Without needing Concord's Food, the Communion might drift into shimmer-space permanently and lose contact with civilization. The pentagon ring forced cooperation between species that would otherwise have nothing to do with each other. Was that wrong? The player must decide.

---

## Instability Phases (Per-Node Integer, 0-100+)

> ✅ **IMPLEMENTED** in InstabilityTweaksV0.cs and InstabilitySystem.cs.
> Phase thresholds: Stable 0-24, Shimmer 25-49, Drift 50-74, Fracture 75-99, Void 100+.
> Gain: BaseGainPerTick * warfront intensity at contested nodes. Decay: 1 per 100 ticks.
> Note: Phase effects (price jitter ±5%, thread delay +20%, trade failure 10%, market closure)
> are defined in tweaks but not yet mechanically applied to MarketSystem/LaneFlowSystem.

### Phase 0: Stable (0-24)
*"Normal space. The threads hold."*
- All instruments accurate. Standard everything. No special mechanics.

### Phase 1: Shimmer (25-49)
*"Your instruments disagree with each other, but only sometimes."*
- **Sensor jitter**: Scan results +/-10% error. Trade good assay values flicker
- **Price flutter**: Market prices update with +/-5% random offsets each tick
- **Visual**: Subtle chromatic aberration on distant objects. Faint skybox shimmer
- **Opportunity**: Phase-locked crystals harvestable from shimmer-space asteroid fields. This is where Drifter Communion stations operate
- **Narrative**: "Space weather." Concord issues advisories. Valorin ignore it. Chitin price it in

### Phase 2: Drift (50-74)
*"Distance lies. Your nav computer says 12 hours. It takes 9. Or 16."*
- **Travel time variance**: Fracture travel takes 0.7x-1.3x predicted time (deterministic by seed)
- **Cargo metric shift**: Goods purchased in drift space have a **fixed fracture weight ratio** per good type. When brought to stable space, actual quantity differs from what you paid for. Each good's ratio is discoverable through experience or scanning — once known, it becomes reliable arbitrage. Ore tends to measure heavy (buy cheap, sell more). Electronics tend to measure light (buy at a loss, avoid). **This is a knowledge puzzle, not random noise** — the player invests effort to learn which goods are profitable in drift space
- **Lattice interference**: Nearby thread connections have reduced throughput
- **Lattice Drones**: Territorial around maintenance nodes. Dangerous if provoked
- **Visual**: Objects at slightly wrong positions. Parallax noticeable. Distant stars doubled. Low resonant audio hum
- **Opportunity**: Null-mass alloy deposits accessible. Metric arbitrage becomes a viable trading strategy for players who've learned the weight ratios
- **Narrative**: Fracture module becomes genuinely useful. Valorin send scout clans. Chitin price the variance into their models

### Phase 3: Fracture (75-99)
*"Space is broken here. The rules work differently."*
- **Topology shift**: Connections between points can change. Routes may not exist between visits. Local map unreliable — must re-scan
- **Fracture refinery**: The player can choose to expose specific cargo to instability at designated sites, with **discoverable transmutation recipes**. "Ore + Phase 3 instability = chance of Exotic Crystal." Risk/reward the player opts into — not random silent mutation. Recipes are found through experimentation and ancient data logs. Wrong combinations destroy cargo. Right combinations produce rare materials unavailable through any other means
- **Accommodation requirement**: Ships without accommodation hull modifications take hull damage per tick. Fracture module protects drive; hull needs separate protection (gated behind tech progression)
- **Lattice Drones**: Fully hostile. Patrol failing thread segments, attack any ship
- **Visual**: Heavy spatial distortion. Objects stretch/compress. Color shifts ultraviolet. Sounds arrive at wrong times
- **Opportunity**: Ancient accommodation-geometry ruins (T3 components, dreadnought fragments, research data cores). Highest-value trade goods in the game. Fracture refinery recipes
- **Narrative**: Endgame exploration territory. Only the player and malfunctioning Lattice drones operate here

### Phase 4: Void (100+)
*"This is not space anymore."*
- **No conventional navigation**: Requires fully accommodation-geometry hull with Metric Drive Core (T3 engine). Navigate by topological connection only — no coordinates, no map, no distance
- **Visual distortion only**: Screen edge warping, color shifts, ship hull appears to refract, stars in wrong positions, UI frame jitter, audio dissonance. **The instruments still read true** — the ship's sensors are the player's last tether to comprehensible reality. The alienness is conveyed through atmosphere, not through lying to the player about gameplay-critical numbers
- **Void sites only**: AnomalyRifts — not ruins or natural formations, but points where spacetime's organizational principle is fundamentally different
- **Lattice Drones**: Absent. Whatever they were maintaining no longer exists here
- **Opportunity**: The endgame revelation. What instability actually is. The three endgame paths each interpret what's found here differently

Phase boundaries are **hard snaps, not gradual fades**. When instability crosses from 49 to 50, drift mechanics engage immediately. Creates meaningful spatial boundaries on the galaxy map. **Transitions are announced**: rising instability triggers visual/audio warnings and UI alerts ("Instability rising: Shimmer threshold in ~50 ticks") so the player can prepare. The snap is dramatic but never a surprise.

---

## Unstable Space Economy (Three Layers)

### Layer 1: Substrate Materials (consumable, ongoing demand)
Unstable space preserves pre-containment material states. Threads crystallized matter into safe, predictable forms. Off-thread systems have pockets of original-state material — more energetic, more useful, more dangerous.

- **Uncontained Alloys** (`exotic_matter`): Required input for T3 module fabrication. Off-thread void sites are the renewable source; anomaly encounters are the discovery source
- **Resonance Crystals** (`exotic_crystals`): Required for accommodation geometry progression. Found in Phase 1+ systems (harvested by Drifter Communion in shimmer zones)
- **Salvaged Tech**: Ancient derelicts in off-thread space are higher-value because containment infrastructure recycles wreckage in thread space, but unstable zones preserve ancient wrecks indefinitely

**Key hook**: T3 module sustain costs consume exotic matter and exotic crystals every 60 ticks. A Dreadnought with T3 loadout needs 6-8 exotic matter per cycle. Fracture travel to off-thread void sites is the T3 supply line.

### Layer 2: Metric Arbitrage (Phase 2+ trading)

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

In Phase 2 (Drift) space, cargo metric shift means goods have fixed fracture weight ratios — each good type consistently measures heavier or lighter when brought to stable space. Players learn these ratios through experience or scanning, creating a knowledge-based trading strategy. This is a unique opportunity that only the player can exploit — no NPC faction has fracture capability to reach these markets.

Maps to existing `FractureSystem.FracturePricingV0` with 1.5x volatility and 2x spread.

### Layer 3: Adaptation Fragments (exploration rewards)
Off-thread void sites are the primary source of adaptation research fragments. The Adaptation faction's experimental sites were in regions of natural instability. The Containment faction couldn't destroy what was already outside containment.

Three reasons to return repeatedly: (1) sustain materials for T3 equipment, (2) metric arbitrage margins, (3) research fragments unlocking new capabilities.

---

## Fracture Module Revelation Arc

### Phase 1: "Experimental Drive" (Hours 3-12)
**Cover story**: Prototype drive from a defunct research group. Found in a derelict. Works by "resonating with micro-fractures in the thread lattice" to create temporary shortcuts. Abandoned because dangerous and damages infrastructure.

UI text: "Structural Resonance Engine," "Lattice Microfracture Drive." Player treats TechLevel progression as "drive calibration." Accurate description of observations, completely wrong about underlying mechanism.

Note: The player has already spent Hours 0-3 as a pure thread trader. They know
what normal space feels like. The fracture module's strangeness has a baseline
to contrast against.

### Phase 2: "That's Not How Resonance Works" (Hours 12-22)
Clues accumulate through the discovery system:
- Anomaly encounters yield data logs from ancient facilities. Dates are wrong. Technology is too advanced for a "defunct corporation"
- Off-thread instability zones follow geometric patterns, not random distribution. Map reveals spokes radiating from hubs
- Fracture module activates sensors the player didn't know they had near certain void sites
- Faction reactions shift: Concord stops saying "that drive is illegal" and starts saying "where did you *get* that?" Chitin offer to buy scans of the module itself

At TechLevel 2, discoveries start including `ADAPTATION_FRAGMENT` items. First one is ambiguously labeled. Second references the first using terminology matching no known faction.

### Phase 3: "It's Not a Drive" (Hours 22-30+)
Paradigm shift: the fracture module predates the threads. It's Adaptation faction technology — designed not to break containment but to operate where containment doesn't exist. The threads aren't protecting you from instability; the module is adapting you to it.

Convergence triggers:
1. Research fragment includes a schematic of the player's own module — millions of years old
2. Module's Trace signature matches patterns in the oldest Adaptation ruins
3. A Lattice maintenance node responds to the module not with alarm but with *recognition* — using authentication protocols the Lattice was designed to accept

What changes: Module UI updates to true name. FractureTier becomes "adaptation depth" not "drive calibration." New VoidSiteFamily types become visible. Five systemic effects are recontextualized — the module isn't damaging threads, it's de-containing space, which weakens nearby threads as side effect.

---

## The Haven — Player Hideout & Ancient Starbase

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

### Discovery

Near where the player first finds the fracture module, there's a system one short fracture jump away — close enough that even the untested drive can reach it safely. This is the player's first fracture destination: a **low-instability pocket** (Phase 0-1) that shouldn't exist off the thread network.

The reason it's stable: the Adaptation faction built a **local stabilizer** here using accommodation geometry, not containment. It doesn't suppress instability — it *shapes* it into a calm eddy. This is proof-of-concept that the Adaptation approach works. The system has been sitting here, self-maintaining, for millions of years.

### What's There

- **An ancient starbase** — Adaptation faction staging outpost, dormant but intact. Recognizes the fracture module's authentication signature and powers up when the player docks
- **A secret thread** — not a containment thread but an accommodation-geometry passage. **One-way outbound only**: Haven TO thread-space, not anywhere TO Haven. To return home, the player must fracture-travel back. This preserves the Haven as a staging base and launching pad while keeping fracture exploration tense — the "do I push further or turn back?" question is never trivialized by a free teleport home. Late-game upgrade (Tier 3-4 starbase) unlocks bidirectional travel, rewarding investment
- **A small system** — a dim star, 1-2 rocky bodies, an asteroid field. Unimpressive to look at. The value is entirely in the starbase and what you build

### Starbase Upgrades (Resource Sink / Progression)

The starbase starts minimal — a powered-up dock with basic repair. The player invests resources (trade goods, exotic matter, adaptation fragments) to unlock tiers:

| Tier | Investment | Unlocks |
|------|-----------|---------|
| **0 — Powered** | (automatic on first dock) | Basic repair, cargo storage, save point |
| **1 — Restored** | Metal, Components, credits | Full repair bay, module swap station, personal cargo vault (unlimited storage) |
| **2 — Operational** | Composites, Electronics, Exotic Crystals | Research lab (study adaptation fragments for lore/bonuses), basic shipyard (refit existing ship) |
| **3 — Expanded** | Rare Metals, Exotic Matter, multiple fragments | Advanced shipyard (build accommodation-geometry hull variants), trade depot (set buy/sell orders that NPC Drifter Communion traders fill over time). **Secret thread becomes bidirectional** |
| **4 — Awakened** | Late-game fragments + high resource cost | Accommodation fabrication (craft T3 modules here instead of finding them), resonance pulse emitter (affect instability in adjacent systems from home base), deep-space scanner (reveals all void sites in the galaxy) |

### Why This Works

- **Early game anchor**: The player has a safe place to stash cargo and repair from Hour 1 of fracture exploration. No more limping back to Concord space after every off-thread trip
- **Progression sink**: Resources have a destination beyond "sell for credits." Every trade good has a use case in starbase upgrades
- **Lore delivery**: The starbase is where the player learns about the Adaptation faction. Upgrading the research lab unlocks lore entries. The starbase's own existence is the strongest argument for the Adaptation philosophy — it's been stable for millions of years without containment
- **Endgame relevance**: The Tier 4 starbase becomes a strategic asset in all three endgame paths — Reinforce (hand it to Concord/Weavers as proof accommodation works), Naturalize (expand it as the seed of a new civilization), Renegotiate (use it as the base for final void expeditions)
- **Player identity**: "This is MY base" — the emotional hook that other faction stations don't provide. You built this. It's yours

### The Secret Thread

The accommodation-geometry thread is important because:
- It proves threads CAN be built without containment (Concord's engineering division would kill for this data)
- It's invisible to the Lattice (which only monitors containment infrastructure)
- One-way outbound initially — the player can deploy from Haven but must fracture-travel home. Bidirectional at Tier 3
- Other factions can't follow you here unless you share the route
- Late game: the player can choose to reveal the thread to faction allies (permanent reputation boost + faction gets access to Haven system)

**Why one-way?** Accommodation engineering shapes spacetime turbulence into stable flow patterns — and flow has a direction. The Haven thread is a shaped vortex: turbulence guided into a self-sustaining current that runs outbound from Haven to thread-space. Like a river, you can ride it one way without effort. Going upstream (inbound) requires actively shaping a counter-current — a harder trick that requires deeper mastery of accommodation geometry. The Tier 3 bidirectional upgrade represents exactly this breakthrough: learning to create a counter-vortex. This is why it requires multiple adaptation fragments and significant resources — it's not a power increase, it's a qualitative leap in understanding the physics.

---

## Adaptation Fragment Web (12 Fragments, 6 Resonance Pairs)

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

Each fragment is independently useful. When two fragments in a resonance pair are both found, a combined effect activates that's more than the sum of parts. No linear progression — discovery in any order.

### Navigation Fragments (found in off-thread void sites)
1. **Void Cartography** — Reveals void site positions within 2 systems and reads ancient fracture traces to uncover hidden AnomalyRift sites. (Solo: fracture fuel -20%)
2. **Current Reading** — Shows instability flow patterns. (Solo: fracture speed +30%)
3. **Depth Sensing** — Detects resource deposits in unstable space. (Solo: survey markers give exact estimates)

### Material Fragments (found in ancient ruins/derelicts)
4. **Substrate Shaping** — Work with uncontained alloys. (Solo: T3 sustain costs -25%)
5. **Lattice Reading** — Decode Lattice protocols. (Solo: interact with Lattice nodes for temporary stabilization — calms malfunctioning drones in the area)
6. **Resonance Tuning** — Calibrate equipment for unstable space. (Solo: no sensor degradation in unstable space)
7. **Phase Tolerance** — Hull adapts to instability. (Solo: fracture hull stress eliminated)

### Structural Fragments (found in Accommodation Geometry sites)
8. **Geometric Suspension** — Reduce instability interaction across hull and shields. (Solo: +20% zone armor in unstable space, full shield capacity retained)
9. **Adaptive Plating** — Hull reconfigures to conditions and distributes stress across zones. (Solo: slow hull regen in unstable space, zone damage distributed across all zones)

### Communication Fragments (found in deepest/most dangerous sites)
10. **Pattern Recognition** — Identify repeating structures and filter signals from noise. (Solo: reveals instability has patterns, detects other ships in unstable space)
11. **Frequency Matching** — Produce signals instability responds to. (Solo: calm local instability temporarily)
12. **Dialogue Protocol** — Framework for structured interaction. (Solo: opens Renegotiate endgame path)

### Resonance Pairs

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

| Fragment A | Fragment B | Combined Effect |
|---|---|---|
| 1 Void Cartography | 3 Depth Sensing | Full galaxy void-site map with resource estimates, including hidden AnomalyRift sites |
| 4 Substrate Shaping | 6 Resonance Tuning | Fabricate T3 modules at the Haven |
| 5 Lattice Reading | 7 Phase Tolerance | Travel through Lattice internal network (fast travel) |
| 8 Geometric Suspension | 9 Adaptive Plating | Ship becomes instability-tolerant (no negative effects, full regen and shield retention) |
| 10 Pattern Recognition | 11 Frequency Matching | Predict instability events before they happen and calm them on arrival |
| 2 Current Reading | 12 Dialogue Protocol | Initiate Renegotiate endgame sequence — read instability’s flow and open structured dialogue with it |

Players don’t see a checklist of 12 blanks. Fragments are discovered through exploration, initially named opaquely ("Artifact XV-7"). Resonance pair completion triggers automatically with fanfare. Different players with different fragment sets have very different play experiences.

---

## Three Endgame Paths (Emergent, Not Chosen)

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

### How Endgame Emerges

Three accumulation vectors tracked throughout play:

**Vector 1: Faction Reputation Balance**
- Concord rep > 50 AND Weaver rep > 50 -> Strong Reinforce alignment
- Valorin rep > 50 AND Chitin rep > 50 -> Strong Naturalize alignment
- Communion rep > 50 AND mixed others -> Opens Renegotiate

**Vector 2: Fracture Usage Intensity**
- Low total Trace -> Thread-economy player -> Favors Reinforce
- High total Trace -> Off-thread life -> Favors Naturalize
- High Trace + Communication Fragments -> Tried to understand -> Renegotiate signal

**Vector 3: Research Fragment Portfolio**
- Reinforce requires: Lattice Reading (5)
- Naturalize requires: Phase Tolerance (7) + Geometric Suspension (8)
- Renegotiate requires: Dialogue Protocol (12) — rarest fragment, deepest site

### Reinforce
**What the player has been doing**: Running stable thread routes, supplying Concord with Composites, commissioning Weaver repairs.
**The crisis demand**: Lattice needs cascading repair operations across multiple systems simultaneously. Only a player with deep Concord/Weaver ties has the intelligence data and engineering contracts to coordinate.
**Endgame activity**: A series of supply-chain missions — deliver Composites to Lattice Node Alpha, Components to Node Beta. The most important trade routes of your career.
**What it costs**: Sealing off-thread space. Permanently losing fracture travel. Exotic Crystal supply chain dies. Drifter Communion loses their way of life. **And the player now knows the cage is a cage.** The pentagon ring is engineered. Reinforce means choosing to preserve the dependency architecture *with full knowledge that it constrains every species in the galaxy*. The player is choosing stability over freedom — the same choice the Containment faction made, for the same reasons, with the same blindspot.
**The personal cost**: The Communion elder who told you the truth asks you not to do this. You do it anyway. The last Communion message: "We understand. We hoped you would choose differently. We were wrong to hope."

### Naturalize
**What the player has been doing**: Running frontier routes, supplying Valorin with Exotic Crystals, trading Chitin speculative markets, building cache networks.
**The crisis demand**: When threads fail catastrophically, the player's alternative network is the only functioning trade system.
**Endgame activity**: Expand frontier network to rescue thread-dependent populations. Deliver Food to starving Concord stations whose supply lines died. Establish new off-thread routes.
**What it costs**: Accepting permanent instability. Thread-space becomes more dangerous. Concord collapses. Lattice shuts down permanently. **The pentagon ring shatters.** Without engineered dependency, factions become self-sufficient — but the interdependence was also what prevented total war. The Valorin no longer need anyone. The Weavers no longer gate access to materials. The cooperation the player navigated for hours was *forced* cooperation, and removing the force doesn't guarantee the cooperation survives.
**The personal cost**: The Concord relief convoys stop. Stations the player traded at for hours go dark. The player built the alternative network — but the people who can't reach it are dying in the transition.

### Renegotiate
**What the player has been doing**: Extensive fracture exploration, Exotic Crystal/Matter trading, Communion relationship-building, void site surveying.
**The crisis demand**: Neither Reinforce nor Naturalize works permanently. Only understanding what instability actually is can end the cycle.
**Endgame activity**: Final fracture expeditions to specific void sites. Use accumulated data, engineering analysis, and fracture module to map instability's structure. Final discovery: instability is not entropy or decay — it is *process*. The containment wasn't just suppressing physics; it was interrupting something.

**Concrete gameplay**: The player maps fracture corridors by visiting systems during active instability events, recording metric variance patterns at each site using the module's Pattern Recognition capability. Each mapped corridor reveals a fragment of the instability's deeper structure — variance signatures that repeat across distant systems, suggesting coordination rather than chaos. After mapping 5+ corridors, the player can propose contact protocols through the Concord diplomatic interface — choosing which factions receive instability data and which are kept in the dark. The Communion provides navigation waypoints to the deepest void sites, but the player must physically fly each corridor during active Phase 3+ instability windows (which last 50-100 ticks before shifting). Missed windows mean waiting — or finding another corridor. The final expedition requires the player to enter a Phase 4 void site with the Dialogue Protocol fragment active and hold position for 30 ticks while the module translates instability patterns into structured signal exchanges. The player watches their instruments display readings that shouldn't be possible — and then the readings stabilize into something coherent.

**What it costs**: Abandoning both thread system and adaptation approach. Every other faction thinks this is insane. Only the Communion supports it. **And the Communion just told you that every previous threshold-crosser either died or vanished.** The player must sit with the question: am I repeating a pattern that always ends the same way? Has the module been shaping my perception — and my judgment — this entire time? The Renegotiate path requires the most faith because the player cannot be certain their faith is their own, and the historical record offers zero successful precedents.
**The personal cost**: Every faction the player built relationships with — the Concord contacts who trusted them, the Chitin traders who shared data, the Valorin clans who named them Blood-Kin — all of them think the player has gone insane. The Communion is the only ally — and they just told you that every previous ally they've watched do this has never come back.

**The game does NOT resolve** whether instability is alive, conscious, intelligent, or just physics. It resolves whether the player is willing to act as though it matters — which is a richer question than any definitive answer. It also does not resolve whether the module influenced the player's choices. That ambiguity is the point.

### The New Vegas Principle

The endgame is not "choose your ending." By the time the crisis hits, the player has already made their choice through hundreds of trade decisions. If strongly aligned with Reinforce factions but high Trace and communication fragments, the game creates genuine tension — economic relationships pull one way, exploration knowledge pulls another. The "I tried to please everyone" player faces the hardest endgame: no faction fully backs them and they haven't committed to understanding instability.

**The pentagon revelation makes every path harder.** Before learning the ring is engineered, each path has a clear moral frame (preserve stability / enable freedom / seek understanding). After the revelation, each path carries the weight of knowing that the galaxy's economic geography is artificial. None of the paths feel "right" anymore. They all feel like different kinds of necessary.

---

## Existing Mechanical Framework

| System | How factions differ |
|--------|-------------------|
| **FactionReputation** (-100 to 100) | Rep unlocks tech, ships, preferred pricing |
| **TradePolicy** (Open/Guarded/Closed) | Affects dock access and trade availability |
| **TariffRate** (0.0-1.0) | Different tariff structures per faction |
| **AggressionLevel** (0/1/2) | NPC patrol behavior, combat frequency |
| **PreferredGoods** | Each faction has distinct supply/demand curves |
| **Tech trees** (via reputation gating) | Unique modules/upgrades per faction |
| **Ship classes** | Unique ship designs per faction |

---

## Summary Table

> **⚠️ REDESIGN PENDING**: Tariff rates and aggression levels in both this document
> and FactionTweaksV0.cs need holistic revision to align with species/philosophy
> lore. See EPIC.S7.FACTION_IDENTITY_REDESIGN.V0. Until redesign completes,
> code values (FactionTweaksV0.cs) are authoritative for gameplay.
>
> Current code values: Concord 5%, Chitin 15%, Weavers 8%, Valorin 20%, Communion 3%.
> Aggression: Concord 0, Chitin 1, Weavers 0, Valorin 2, Communion 0.

| Faction | Policy | Tariff | Aggr | Produces | Needs | Ships | Tech | Path |
|---------|--------|--------|------|----------|-------|-------|------|------|
| Concord | Open | 0.08 | 1 | Food, Components, Munitions | Composites | Balanced cruisers (shield+slots) | Sensors, Shields, Utility | Reinforce |
| Chitin | Guarded | 0.12* | 0 | Electronics, Munitions | Rare Metals | Fast clippers (scan) | Scanners, Engines | Naturalize |
| Weavers | Guarded | 0.15 | 1 | Composites, Metal | Electronics | Tanky haulers (armor) | Armor, Hull | Reinforce |
| Valorin | Open | 0.03 | 2 | Rare Metals, Ore | Exotic Crystals | Cheap swarm (numbers, 3-4x density) | Engines, Cargo | Naturalize |
| Communion | Open | 0.02 | 0 | Exotic Crystals, Salvage | Food, Fuel | Scout clippers (scan) | Scanners, Nav | Renegotiate |

*Chitin tariff fluctuates every 50 ticks.

---

## Warfront Seeding

> See also: `dynamic_tension_v0.md` — Pillar 1 (The Galaxy Starts at War)

> ✅ **IMPLEMENTED** in GalaxyGenerator.SeedWarfrontsV0.
> Hot war: Valorin vs Weavers at OpenWar (intensity 3).
> Cold war: Concord vs Chitin at Tension (intensity 1).
> Contested nodes: BFS depth 1 from faction borders.

The galaxy does not begin at peace. The procedural generator seeds active
conflicts as part of world creation. The player spawns into a galaxy already
shaped by war.

### Natural Fault Lines

Faction philosophies and the dependency ring create two tiers of conflict
probability:

| Tier | Pair | Conflict Driver | War Character |
|------|------|----------------|---------------|
| **Hot war** | Valorin vs Weavers | Territorial: Valorin expand into Weaver chokepoints. Weavers block frontier access to protect infrastructure. Aggression 2 vs 1 | Shooting war. Thread disruptions. Refugee flows. Ore/Composites and Rare Metals/Exotic Crystals supply chains under direct pressure |
| **Cold war** | Concord vs Chitin | Informational: Concord suppresses thread degradation data. Chitin price and trade that data. Concord's surveillance state vs Chitin's information markets | Espionage, trade restrictions, inspection harassment. Chitin tariffs spike. Concord closes ports to Chitin-flagged vessels |

The Drifter Communion is too weak and peaceful to initiate conflict. They are
collateral damage — their supply lines break when the factions they depend on
go to war.

### Seed Rules

1. **Every seed has exactly one hot war at tick 0.** The generator places the
   Valorin-Weaver contested border within 2-3 hops of the player's starting
   system. The player's first trade decisions are shaped by this conflict.

2. **Every seed has one simmering conflict.** The Concord-Chitin cold war
   begins at elevated tension (not yet shooting). This conflict escalates to
   open hostility between tick 200-600 based on seed-deterministic triggers.
   The player sees it coming and can position accordingly.

3. **The player's starting system belongs to a non-combatant faction.** You
   start in Concord or Chitin space — close enough to the front to feel
   prices, far enough to not get shot immediately. This gives the player a
   safe-ish base while the warfront is 1-2 hops away with its inflated
   margins.

4. **Warfront location determines which dependency chain links are stressed.**
   A Valorin-Weaver war disrupts Rare Metals (Chitin starves) and Composites
   (Concord starves). A Concord-Chitin cold war disrupts Food/Components
   (Communion and Valorin starve) and Electronics (Weavers starve). The
   generator picks the war; the pentagon dependency determines the economic
   blast radius.

### Cascade Example: Valorin-Weaver War

```
Valorin territory contested
  -> Rare Metals production drops 40%
    -> Chitin can't sustain Electronics output
      -> Weavers lose sensor-web maintenance capability
        -> Composites quality degrades
          -> Concord fleet armor weakens
            -> Concord border patrols thin out
              -> Piracy rises in Concord space
                -> Player's safe trade routes become less safe
```

The player feels this war through **six degrees of economic separation**. They
never see a Valorin fleet fire on a Weaver station. They see Rare Metal prices
triple at Chitin markets, and their Composites trade route margins collapse.

### Warfront Evolution

Wars are not static. Seed-deterministic event chains drive warfront shifts:

- **Tick 0-200**: Initial hot war. One simmering conflict. Player learns the
  economic landscape.
- **Tick 200-600**: Simmering conflict escalates. Second front opens. Supply
  chains that survived the first war now face cross-pressure.
- **Tick 600-1200**: Ceasefire possible on first front (temporary relief).
  The dependency ring is now stressed at multiple points. Lattice drones
  become territorial in degraded sectors.
- **Tick 1200+**: Warfronts interact with Fracture threat. Lattice
  degradation weakens threads near combat zones. Factions blame each other.
  Instability phases advance faster in war-damaged regions. Lattice drones
  become fully hostile in Phase 3 sectors.

Ceasefires create breathing room — the player can rebuild disrupted supply
chains — but they don't last. New conflicts open as old ones cool. The galaxy
is never fully at peace and never fully at war.

---

## Wartime Economics

> See also: `dynamic_tension_v0.md` — Pillars 3 and 4

### Tariff Scaling

Base tariffs in the Summary Table are **peacetime floors**. During active
conflict, tariffs scale with warfront intensity:

```
Effective Tariff = BaseTariff + (WarSurcharge * WarfrontIntensity)
```

| Warfront Intensity | War Surcharge | Example: Concord (base 0.08) |
|--------------------|---------------|------------------------------|
| 0 — Peace | 0.00 | 0.08 |
| 1 — Tension | 0.03 | 0.11 |
| 2 — Skirmish | 0.07 | 0.15 |
| 3 — Open War | 0.12 | 0.20 |
| 4 — Total War | 0.20 | 0.28 |

Warfront intensity is per-system, not per-faction. A Concord system on the
front line charges 0.20. A Concord system in the deep interior charges 0.08.
The player sees the surcharge broken out in UI: "Tariff: 20% (base 8% + war
surcharge 12%)."

### Wartime Demand Shocks

When a faction is actively fighting, it consumes goods at elevated rates. This
creates scarcity that ripples through connected markets:

| Good | Peacetime Demand | Wartime Demand | Effect |
|------|-----------------|----------------|--------|
| Munitions | 1x | 3-5x | Price spike at faction stations. Shortages downstream |
| Composites | 1x | 2-3x | Armor repair and fleet maintenance. Weavers can't keep up |
| Fuel | 1x | 2-4x | Military logistics burn fuel. Civilian routes face fuel shortages |
| Metal | 1x | 1.5-2x | Ship repair, ammunition casings. Mild but persistent pressure |
| Food | 1x | 1.5x | Refugee populations, garrison supply. Concord stations especially |
| Components | 1x | 2x | Equipment replacement, sensor repair. Concord bottleneck |

These multipliers apply to NPC faction consumption, not player prices directly.
The player sees the effect as **market prices rising** at stations near the
front — buy prices climb because the station's inventory is being consumed
faster than NPCs can resupply it.

**The player opportunity**: Wartime demand shocks are the source of the best
margins in the game. Running Munitions to a front-line station during open war
can yield 200-400% markup. But the route passes through contested space with
hostile patrols, inspections, and potential interdiction.

### The Neutrality Tax

Factions tolerate neutral traders during peacetime. During war, neutrality
becomes suspicious — and expensive.

| Warfront Intensity | Neutral Trader Experience |
|--------------------|--------------------------|
| 0 — Peace | Full access. No questions asked |
| 1 — Tension | Occasional patrol scans. "Papers, please" flavor text. No gameplay effect |
| 2 — Skirmish | Inspection frequency 2x. Cargo scanned for contraband. +5% tariff surcharge for unaligned traders |
| 3 — Open War | Inspection frequency 4x. Cargo scan + reputation check. +10% neutrality surcharge. Faction-exclusive goods restricted (cannot buy military-grade modules) |
| 4 — Total War | Mandatory allegiance declaration to dock. Unaligned traders denied port access at front-line stations. Interior stations still accessible but at +15% surcharge. Faction offers "emergency trade contract" — commit now for Allied pricing, or lose access |

**The shrinking middle**: At Intensity 0-1, neutrality is free. At Intensity
2-3, neutrality costs money but preserves access to all factions. At Intensity
4, neutrality becomes nearly unplayable in the war zone — the player must
either commit to a side or retreat to interior systems with thin margins.

### Exclusive Supply Contracts

Starting at Warfront Intensity 2, factions offer the player exclusive supply
contracts:

```
CONCORD CONTRACT OFFER
"Deliver Munitions exclusively to Concord stations for the duration of
hostilities. In exchange: Allied pricing (-15% buy, +15% sell), patrol
escort on Concord routes, access to military-grade Shield modules."

PENALTY: Selling Munitions to any non-Concord station while under contract
triggers immediate reputation drop to Unfriendly (-30). Contract voided.
Concord patrol response becomes hostile for 200 ticks.

[Accept]  [Decline — no penalty]  [Counter-offer: 100-tick term limit]
```

Key design rules for contracts:
- **Declining has no penalty.** The player is never punished for saying no.
  Pressure comes from the escalating neutrality tax, not from contract
  refusal.
- **Contracts are time-limited** (100-500 ticks). The player is not
  permanently locked in. But breaking a contract is catastrophic for
  reputation.
- **Counter-offers are possible.** The player can negotiate term length,
  which goods are covered, and which stations count. This is a trade
  negotiation, not a binary choice.
- **Both sides of a war offer contracts.** The player can see both offers
  and choose which (if either) to accept. Accepting one automatically
  declines the other.
- **Contracts stack with reputation.** Contract performance builds reputation
  faster than normal trading. A 200-tick contract completed successfully is
  worth +15-20 reputation — equivalent to hundreds of individual trades.

### War Profiteering

The player CAN supply both sides of a conflict — but it's risky:

- Selling to Faction A's enemy while under no contract: reputation with A
  drops by 1 per transaction (slow bleed, manageable).
- Selling to Faction A's enemy while under contract with A: contract breach,
  reputation crashes, patrol response hostile. Catastrophic.
- Selling to both sides with no contracts: viable but increasingly difficult
  as inspections intensify. Cargo scan reveals goods purchased at enemy
  stations. The player can mitigate with Chitin "Probability Engine" (cargo
  manifest scrambling at max rep), or by laundering goods through neutral
  third-party stations.

War profiteering is not forbidden — it's a high-risk, high-reward playstyle
that maps naturally to the Chitin/smuggler identity.

---

## Drifter Communion Early-Game Vulnerability

> See also: `dynamic_tension_v0.md` — Pillar 5 (Fracture Temptation)

The Communion depends on Concord for Food and Fuel — the most basic survival
goods. When the galaxy starts at war and Concord is involved (even indirectly
through cascade effects), the Communion's supply line is immediately at risk.

### Why This Is Good for Gameplay

The Communion is the player's natural ally for the Renegotiate endgame path —
the most narratively rich and hardest-to-reach ending. By putting the Communion
in early crisis, the game creates:

1. **An early humanitarian hook.** The player sees Communion stations running
   low on Food within the first 100 ticks. Running Food to Communion stations
   is profitable (scarcity pricing), morally sympathetic, and builds
   reputation toward the faction that eventually opens the Renegotiate path.

2. **A reason to engage with Fracture early.** The Communion trades in Exotic
   Crystals and Salvaged Tech — goods harvested from shimmer-zone boundaries.
   If the player befriends the Communion by running Food, the Communion offers
   shimmer-zone navigation data in return. This is the on-ramp to the
   Fracture temptation: you start helping refugees, and they introduce you to
   the boundary economy.

3. **A faction that needs YOU specifically.** Every other faction has robust
   NPC trade networks. The Communion's supply lines are fragile because
   they're marginal — few NPC traders bother running Food to remote Communion
   stations. The player's individual contribution matters more here than
   anywhere else. This is the "useful neutral" pattern: you matter because
   you go where others don't.

4. **A contrast with Concord's stability.** Concord stations are comfortable,
   well-supplied, predictable. Communion stations are desperate, chaotic,
   grateful. The player experiences the galaxy's inequality firsthand through
   trade — the "haves" and "have-nots" are not an abstract statistic but a
   difference in docking experience.

### Mechanical Expression

- Communion stations start with **50% Food and Fuel inventory** at tick 0
  (all other factions start at 80-100%).
- NPC trade frequency to Communion stations is **half** the rate of other
  factions (fewer ships run marginal routes).
- Communion buy prices for Food and Fuel are **1.5x base** from tick 0 —
  immediate arbitrage opportunity for the player.
- At **tick 100** without resupply, Communion stations begin rationing:
  services degrade (repair takes longer, module swap unavailable). At
  **tick 200**, station begins broadcasting distress — visible on galaxy map
  as a pulsing icon.
- Running Food or Fuel to a Communion station in distress gives **3x normal
  reputation gain.** The player is rewarded for helping, not just trading.

### Narrative Beat

The Communion is the first faction that treats the player as a person rather
than a customer. Concord is polite-but-surveillance. Chitin are transactional.
Weavers are patient-but-aloof. Valorin are friendly-but-chaotic.

The Communion remembers. The captain who ran Food to a starving station gets
named in Communion internal communications. NPCs at other Communion stations
greet the player by reputation. "You're the one who kept Waystation Kell
alive." This is the emotional hook — not credits, not modules, but a faction
that actually cares that you showed up.

---

## The First Officer — Player-Chosen Anchor Character

> 🔮 **FUTURE — NOT YET IMPLEMENTED**. This section describes aspirational design for future tranches.

### The Problem This Solves

The story's strongest elements are systemic (pentagon twist, warfront cascades) and archaeological (data logs, fragments). But the best game stories also have at least one **person** the player cares about — a Kim Kitsuragi, a Garrus, a GLaDOS. Without an anchor character, the narrative risks feeling admirable but cold. Revelations land intellectually but not emotionally.

### Design: Player Chooses a First Officer

Early in the game (tick 50-150, during the Establish phase), the player's growing operation requires a chief of staff. Three candidates are available as crew members, each a competent professional who left their previous life for different reasons. The player **promotes one to First Officer**. This determines:

1. **Narrative voice** — the First Officer's personality colors fleet reports, program updates, and commentary on key story beats
2. **Gameplay bonus** — each candidate brings a mechanical specialty
3. **Emotional anchor** — the First Officer reacts to revelations, making abstract twists personal
4. **Endgame perspective** — each has an opinion about which path to take (that may differ from the player's)

The unpromoted candidates remain as secondary crew — available for faction-specific intel or occasional dialogue, but without the First Officer's narrative prominence. This creates replayability: different FO = different emotional experience of the same story.

### Candidate 1: The Analyst

- **Background**: Former Chitin Syndicate probability assessor
- **Why they left**: Disagreed with the Syndicate's willingness to treat genocide probability as a market variable. "The odds of the Communion starving are 73%. We could trade on that." They couldn't stomach it
- **Personality**: Dry, precise, quietly caring. Everything framed as probabilities. But underneath the math — someone who uses data to avoid feeling helpless. "The odds of survival in that sector are 31%. ...I've been wrong before. Let's go."
- **Gameplay**: Enhanced market intelligence — price predictions, demand trend warnings, better program efficiency. The player who picks the Analyst trades better
- **Narrative function**: Reacts to revelations through data. The pentagon twist breaks their models — "These distribution patterns aren't stochastic. They can't be. Someone DESIGNED this." The Concord betrayal is statistical: "I ran the degradation curves. They had the same data I did. They KNEW."
- **Endgame lean**: Naturalize. Pragmatic — accommodation works, containment doesn't, the numbers are clear. But respects the player's choice
- **Emotional arc**: Starts detached (everything is odds). Events force them to confront that some things can't be reduced to probability. The endgame question: when the math doesn't help, what do you trust?

### Candidate 2: The Veteran

- **Background**: Former Concord fleet logistics coordinator
- **Why they left**: Asked too many questions about classified thread degradation data. Wasn't fired — was "reassigned to a fulfilling role" at a backwater monitoring station. Quit instead. Not a whistleblower — a professional who preferred honest uncertainty to comfortable lies
- **Personality**: Clinical, competent, institutional. The voice of fleet reports and program updates. Occasionally dry humor. Their competence IS their character — the Homeworld principle. When the mask cracks, you notice
- **Gameplay**: Fleet automation efficiency bonus, better supply chain management, Concord patrol route intelligence. The player who picks the Veteran runs a tighter operation
- **Narrative function**: The Concord betrayal is personal. Their former employer knew — and the questions they were told to stop asking were the right questions. "I spent twelve years running their supply chains. Twelve years. And I was maintaining a cage."
- **Endgame lean**: Reinforce. Institutional instinct — fix the system from within, don't burn it down. The threads work. Make them work better. But haunted by the knowledge that "making it work" is what Concord has been saying for decades
- **Emotional arc**: Starts professional and controlled. The Concord revelation breaks the control — their entire career was participation in suppression. The endgame question: do you fix the institution that failed you, or do you walk away from everything you were trained to believe?

### Candidate 3: The Pathfinder

- **Background**: Independent navigator, Communion-adjacent but never formally joined. Spent years mapping shimmer-zone boundaries for anyone who'd pay
- **Why they're available**: Ran out of money after a client defaulted. Pragmatic enough to take a steady job, curious enough to find a trading operation near the frontier interesting
- **Personality**: Warm, present, observational. Notices things other people don't. Speaks in sensory impressions sometimes. Not mystical — perceptive. "The star positions looked wrong for a moment. Probably parallax." (It wasn't parallax.)
- **Gameplay**: Fracture travel bonuses — reduced fuel cost, reduced hull stress, better discovery scan outcomes. The player who picks the Pathfinder explores deeper
- **Narrative function**: The Communion revelation hits them differently — they're not surprised. "I always wondered why they were so interested in my shimmer-zone charts." The module adaptation revelation resonates personally — they've been seeing differently their whole career. They're the first to say: "The distortion doesn't feel like distortion anymore. It feels like... resolution."
- **Endgame lean**: Renegotiate. Curiosity — "What IS it? We have to know." But they also know the cost, because they've seen what deep fracture space does to people
- **Emotional arc**: Starts warm and grounded. Fracture travel awakens something in them — perception sharpening, comfort with instability growing. The endgame question: if understanding metric-variant space requires becoming something other than what you were, is it still understanding?

### Design Rules

- **Three candidates, not four.** Clean choice. Each clearly distinct. Each aligned with a different endgame path but not REQUIRING that path — the player can choose the Analyst and still go Renegotiate
- **Present from tick 1.** All three are crew members before the promotion. The promotion formalizes the relationship, not creates it
- **30 lines total across 20 hours.** Restraint is the instrument. Most of the time, the First Officer is silent — their competence IS their presence. When they speak, it matters because it's rare
- **The unpromoted stay.** Secondary crew members remain available for faction-specific intel checks or skill bonuses. Not wasted — just not the anchor
- **Their endgame opinion is visible but not coercive.** If the player chooses a different path, the First Officer goes along (loyalty) but the cost is visible. They stay, but they're not the same
- **Named ships reference them.** Fleet reports include the First Officer's commentary: "The *Argent Crossing* completed Trade Charter Sirius→Proxima: +340 cr." becomes "Argent Crossing home safe. +340 cr. She's getting good at that run." — only if the FO speaks in that register

---

## Fracture Module Timing (Clarification)

> Reconciles `dynamic_tension_v0.md` Pillar 5 with the Revelation Arc above.

The fracture module is **not available at game start.** The player spends their
first 2-4 hours (roughly tick 0-400) in pure thread-space: learning to trade,
feeling warfront pressure, upgrading with standard T1 equipment, and
establishing their first trade routes. The module's arrival is a narrative
turning point that recontextualizes the game.

### Why Delayed Discovery Works

1. **The player learns the rules before they can break them.** Thread-space
   trading, warfront economics, faction reputation, tariffs, inspections —
   the player internalizes these systems when they're the ONLY systems. When
   the fracture module arrives, the player understands exactly what they're
   circumventing and what the stakes are.

2. **Standard equipment first.** The player should have time to visit
   multiple stations, compare T1 modules, make fitting decisions, and feel
   ownership of a ship they built with conventional tech. The fracture module
   and exotic materials are more exciting when you already have a baseline to
   compare them against.

3. **The warfront is personal before the escape valve appears.** By tick 300,
   the player has lost a trade route to a warfront shift, paid war tariffs,
   been inspected by hostile patrols. They WANT an alternative. The fracture
   module arrives when the player is already frustrated with the constraints
   of thread-space — making it feel like a genuine temptation, not a tutorial
   unlock.

4. **The Communion relationship builds naturally.** The player encounters
   struggling Communion stations during thread-space play (they're on the
   margins of the thread network). Running Food to them is profitable even
   without fracture capability. When the module arrives, the Communion are
   already friends — and they have navigational data to share.

### Pacing

- **Tick 0-150 (Hour 0-2): Pure thread trader.** Learn markets, feel the
  warfront, make first credits. Upgrade to T1 modules. Encounter Communion
  stations in need. No hint of fracture tech.

- **Tick 150-300 (Hour 2-3): Foreshadowing.** Anomalous scanner readings at
  the edge of explored space. A derelict that doesn't match any known
  faction's design language. NPC dialogue hints: Drifter Communion mentions
  "the old routes." Chitin traders reference "off-ledger goods" with no
  source station.

- **Tick 300-400 (Hour 3-4): Discovery.** The player finds the derelict
  containing the fracture module. The discovery is gated behind a light
  exploration requirement (visit N systems, or reach a specific frontier
  node near the warfront). The module installs automatically — it's too
  valuable to leave behind, too strange to fully understand.

- **Tick 400-500 (Hour 4-5): First fracture jump.** Fuel is expensive.
  Range is limited. The Haven is discoverable within the module's initial
  range. The player's first off-thread experience is disorienting — sensor
  readings flicker, travel time is uncertain, the galaxy looks different
  from out here.

- **Tick 500-800 (Hour 5-8): The temptation grows.** Warfront pressure
  intensifies (second conflict activates). Thread tariffs climb. The player
  discovers that fracture routes bypass blockades. Exotic materials start
  appearing. The cost-benefit calculation shifts — fracture travel goes from
  "expensive curiosity" to "economically rational alternative."

- **Tick 800+ (Hour 8+): Dual doom clock.** Thread routes are expensive and
  dangerous. Fracture routes accumulate Trace. The player must balance
  both pressures. The Revelation Arc (Phase 2: "That's Not How Resonance
  Works") kicks in as discoveries accumulate.

### Discovery Trigger

The fracture module derelict appears at a **frontier node adjacent to the
active warfront.** This is deliberate — the player is drawn toward the front
by profit margins, and the derelict is one hop past the last safe station. The
player must make a risky journey to reach it, reinforcing the
danger-profit correlation (Pillar 1) at the moment of the game's biggest
unlock.

The derelict does NOT appear on the map until the player has:
- Visited at least 4 distinct star systems (ensures basic exploration)
- Completed at least 3 trade transactions (ensures understanding of economy)
- Reached tick 150+ (ensures time for warfront to be felt)

These are soft gates, not hard quests. The player hits them naturally through
normal play. The derelict simply appears on scanners when ready — no quest
marker, no "go here" directive. The player discovers it.

---

## Research Inspirations

### Fiction
- **Vernor Vinge, A Deepness in the Sky** — spider civilization, Zones of Thought (containment layers)
- **Adrian Tchaikovsky, Children of Time** — spider engineering culture, portid silk technology
- **Iain M. Banks, The Culture** — Affront, post-scarcity faction dynamics
- **Peter Watts, Blindsight** — alien intelligence without consciousness, reality-breaking physics
- **Alastair Reynolds, Revelation Space** — Melding Plague (infrastructure decay), Inhibitors (containment enforcement)
- **Liu Cixin, Three-Body Problem** — dark forest theory, existential threat from physics itself
- **Ursula K. Le Guin, The Dispossessed** — Anarresti alternative economics
- **China Mieville, Embassytown** — truly alien communication
- **Jeff VanderMeer, Annihilation** — mutation as communication, Area X's phase boundaries
- **Stanislav Lem, Solaris** — phenomenon that resists understanding

### Games
- **EVE Online** — thread topology as economic geography, wormhole space (unique resources feeding main economy)
- **Starsector** — faction personality through trade policy + military doctrine, remnant systems
- **Sunless Skies/Sea** — cosmic horror integrated with trade, narratively textured trade goods
- **Fallout: New Vegas** — endgame emerging from accumulated faction relationships
- **Alpha Centauri** — factions defined by philosophical stance, not species
- **Outer Wilds** — archaeology-driven revelation, knowledge graph progression
- **Subnautica** — environmental storytelling, biome-distributed progression

### Economic Models
- **Kula Ring** — circular exchange, inspiration for circular faction dependency
- **Hawala** — trust-based transfer, model for reputation-gated trade
- **Potlatch** — competitive generosity, alternative to profit-maximization
- **Damascus Steel / Roman Concrete** — technology that can't be reverse-engineered
