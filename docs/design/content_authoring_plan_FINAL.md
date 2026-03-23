# Content Authoring — Final Plan

**Date**: 2026-03-21
**Status**: FINAL — incorporates critical evaluation corrections
**Companion doc**: `content_authoring_research_v0.md` (raw research)

---

## Critical Corrections Applied

The initial research proposals were stress-tested against player data, codebase audit, and industry failure modes. Six major corrections:

| Original Proposal | Problem Found | Correction |
|---|---|---|
| 8-mission faction chains | 40-60% player dropout by mission 4 (Tychsen 2008, Dormans 2010). Only half see your climax. | **5-mission chains** with 3 decision points. Show chain length upfront (goal gradient effect). |
| Hard cross-chain dependencies | 80% of players miss them (Dragon Age Origins postmortem). Cognitive overload in complex games. | **Consequential reactions** — faction B *comments on* faction A actions, never *locks/unlocks* based on them. |
| "Midpoint twist" (revelation) | "Secret villain" twists retroactively invalidate prior missions. Players feel manipulated. | **Cumulative disclosure** — deepen understanding, don't reverse it. Recontextualize, don't invalidate. |
| Timer-based FO commentary | Context-blind commentary is the #1 BG3 complaint, not frequency. | **Event-triggered** commentary after significant actions (trade, arrival, combat). 3-5 actions between comments. |
| Equal mission type distribution | Elite Dangerous: 60% massacre missions dominated boards. Equal distribution still feels repetitive. | **Weight toward player's current activity**. Surface consequences visibly in the world. |
| Faction signature mechanics (5 unique verbs) | 3x implementation cost per faction: unique UI + unique tutorial + unique balance pass (Stellaris DLC pattern). | **Phase implementation**: 2 verbs at EA launch, remaining 3 in content waves. Start with lowest-complexity verbs. |

---

## Codebase Readiness Audit

What exists TODAY vs what needs new code:

| System | Content-Ready Today | New Code Required |
|--------|-------------------|-------------------|
| **Missions** | 10 templates, scales to 65. MissionDef supports prerequisites, steps, binding tokens. | Faction pool filtering, multi-good step support, reward variety (module/rep rewards) |
| **FO Dialogue** | ~200 lines, 26 triggers, 3 archetypes. DialogueLine supports tier gating + interpolation. | No conversation memory. No "remember your choice" state machine. |
| **Haven** | 5 tiers, Keeper evolution, AccommodationProgress threads, TrophyWall. | No narrative log system (chronicle). No resident interaction. No interior visuals. |
| **Knowledge Web** | 15 connection templates, 5 revelations, pattern tokens. Scales to 200+. | No procedural edges. No player annotations. |
| **Megaprojects** | 3 types (Fracture Anchor, Trade Corridor, Sensor Pylon). MegaprojectDef scales. | No cascading dependencies. No visual representation. No player choice UI. |
| **Modules** | 54 modules. FactionId + FactionRepRequired already works for exclusives. | No scripted effects (ECM, stealth, conditional behaviors). Stats-only. |
| **Win Conditions** | GameResult enum (Victory/Death/Bankruptcy), 3 EndgamePaths, rep drift. | **NO victory trigger code**. Death/bankruptcy untriggered. No epilogue scene. |
| **Music/Audio** | Engine hum only. Zero infrastructure. | **Everything**: music system, audio buses, content registry, adaptive transitions. |

**Verdict**: The simulation layer is content-ready for narrative depth. The two critical-path gaps are **victory/loss conditions** (minimal code, high impact) and **music system** (significant code, can defer).

---

## The Plan — 8 Content Epics, Ordered by Impact

### Priority 1: FACTION_STORYLINES

**Why first**: Highest narrative impact. Drives faction identity more than any other content. Uses existing mission infrastructure. Every other content epic references faction chains.

#### Corrected Design: 5-Mission Chains (Not 8)

Based on dropout data (40-60% per mission beyond M4), chains are compressed to 5 missions with 3 major decision points. The chain length is shown to the player upfront (goal gradient effect — Star Citizen "Blockade Runner" feedback).

**Arc structure**:

| Mission | Phase | Function |
|---------|-------|----------|
| M1 | Introduction | First contact + prove competence. Combined from original M1+M2. |
| M2 | Investment | Inner circle access. Faction-specific privilege unlocked. |
| M3 | **Disclosure** | The faction's uncomfortable truth surfaces. NOT a "twist" — cumulative deepening. Player *discovers evidence*, doesn't get told by a villain. |
| M4 | **Complicity** | Moral compromise. Asked to do something that serves the faction at a cost. Player's FO has strong archetype-specific opinion. |
| M5 | **Resolution** | Reflects M4 choice. Two distinct endings per chain. Epilogue seed. |

**Genre identities** (tonal north star for authoring):

| Faction | Genre | One-Sentence Thesis |
|---------|-------|---------------------|
| Concord | Political thriller | "Order requires sacrifice — and you've been the sacrifice." |
| Chitin | Philosophical detective | "The hive found the truth. No individual would have chosen it." |
| Weavers | Craftsman's tragedy | "Everything they built works because of something they don't understand." |
| Valorin | Frontier western | "The frontier wasn't empty. It was emptied." |
| Communion | Mystical revelation | "They've been listening. It's been trying to answer." |

#### Per-Faction Mission Outlines

**Concord** (Political Thriller):
1. **"Standard Operating Procedure"** — Deliver supplies. Everything works. Concord is reliable. (Investment: competence)
2. **"The Audit Trail"** — Investigate trade irregularity. Gain inner-circle clearance. (Investment: trust)
3. **"The Sealed Report"** — Access restricted archive. Discover Concord is suppressing fracture data. The "irregularity" was someone trying to leak it. (Disclosure: evidence-based, not told)
4. **"Containment Protocol"** — Seal fracture vents per orders. Later see the downstream damage your sealing caused. (Complicity: you did the thing)
5. **"Whistleblower"** — Report the data to other factions (break trust, gain cross-faction standing) OR help Concord control the narrative (maintain trust, cover-up continues). FO has strong opinion. (Resolution: two endings)

**Chitin** (Philosophical Detective):
1. **"Market Anomaly"** — Investigate a price discrepancy. The Chitin already know the answer — they're testing how you think. (Investment)
2. **"Metamorphic Access"** — Witness a molting ceremony. Gain data network access. Understand: they see probability fields, not objects. (Investment)
3. **"Pattern Break"** — Models detect communication patterns in lattice decay. The collective is accidentally eavesdropping on something. (Disclosure)
4. **"The Bet"** — Act on a prediction that harms Valorin trade routes. The prediction may be right, but acting on it is economic warfare. (Complicity)
5. **"Full Spectrum"** — Models achieve convergence. What they found either validates the communication hypothesis or was an artifact of their own observation. (Resolution)

**Weavers** (Craftsman's Tragedy):
1. **"Raw Materials"** — Deliver construction materials. See the patient, precise building process. (Investment)
2. **"The Master Builder"** — Tour their greatest construction. Understand: they build for permanence. (Investment)
3. **"Echo Architecture"** — Scan substrate of a megastructure. It's built on ancient Accommodation pillars they didn't know existed. Their "instinct" is actually resonance with ancient patterns. (Disclosure)
4. **"Structural Failure"** — A construction collapses because underlying Accommodation support is degrading. Weavers blame themselves. You know the real cause. (Complicity: silence is complicity)
5. **"The Retrofit"** — Share Accommodation data (forces philosophical reckoning but stronger builds) OR let them proceed their way (beautiful but fragile). (Resolution)

**Valorin** (Frontier Western):
1. **"Patrol Duty"** — Ride along with a patrol. See discipline, camaraderie, blunt threat assessment. (Investment)
2. **"Weapons Test"** — Test new munitions. Everything about their culture is martial but not cruel. They respect competence. (Investment)
3. **"Collateral"** — Visit a system the Valorin "secured" last year. Independent traders are gone. Not killed — displaced. The frontier wasn't empty. (Disclosure)
4. **"Extraction Zone"** — Help extract rare metals using the Valorin method (fast, efficient, destabilizing). You see the aftermath. (Complicity)
5. **"Last Patrol"** — Old routes are untenable as fracture intensity increases. Retreat to defensible space OR push further into unknown. Your relationship determines which they choose. (Resolution)

**Communion** (Mystical Revelation):
1. **"The Frequency"** — Listen to a signal at a specific location. You hear nothing. They hear everything. They're patient. (Investment)
2. **"The Silent Record"** — Access experiential archives. Sit in a resonance chamber and *listen*. The record is the absence of certain frequencies. (Investment)
3. **"Previous Visitors"** — They know about the Accommodation civilization — not from data, but from listening. Something has been signaling for a very long time. (Disclosure)
4. **"The Threshold"** — Reach a specific fracture depth and report what you perceive. They can't go — too fragile. You are their instrument. (Complicity: you become part of their practice)
5. **"The Answer"** — Attempt to *respond* through the metric. Not just listen — transmit. Resolution depends on endgame path alignment. (Resolution: most variable mission in the game)

#### Cross-Chain Design: Reactions, Not Dependencies

NO hard locks. Instead, consequential reactions:

| If Player Did... | Then In Other Chains... |
|---|---|
| Concord M3 (sealed report) | Chitin M3: "We already had that data. Interesting that Concord tried to hide it." |
| Chitin M4 (the bet) | Valorin M3: "Someone manipulated our trade routes. The Chitin were involved." |
| Weavers M3 (echo architecture) | Communion M3: "You've seen what we hear. The echoes in the structure." |
| Valorin M3 (collateral) | Weavers M4: "The Valorin destabilized our construction site. You were there." |
| Communion M3 (previous visitors) | All M5s: Adds a dialogue option referencing the ancient compact |

**Implementation**: Check PlayerStats flags (e.g., `CompletedConcordM3`) and inject alternate dialogue lines. No quest-gating, no lock-outs. 15 reaction lines total (3 per faction).

#### Authoring Volume

| Item | Count | Words |
|------|-------|-------|
| Mission briefs (5 × 5 factions) | 25 | ~5,000 (200/brief) |
| NPC dialogue per mission (~4 exchanges) | 100 | ~10,000 (100/exchange) |
| FO mission commentary (3 archetypes × 25 missions) | 75 | ~3,750 (50/line) |
| Cross-chain reaction lines | 15 | ~750 (50/line) |
| Resolution variant text (2 endings × 5 factions) | 10 | ~2,000 (200/ending) |
| **Subtotal** | **225** | **~21,500 words** |

#### Code Required

- [ ] Add `FactionId` field to `MissionDef` for faction pool filtering
- [ ] Add `ReputationReward` field to `MissionDef` for standing changes
- [ ] Add `ModuleReward` field for equipment rewards (Weavers M2, Valorin M2)
- [ ] Add `PlayerStatsFlag` set on mission completion (for cross-chain reactions)
- [ ] Add bridge method `GetFactionMissionsV0(factionId)` for UI filtering

---

### Priority 2: TEMPLATE_MISSIONS (50-65 Templates)

**Why second**: Core gameplay loop. Each template makes the world feel alive between faction chain missions.

#### Corrected Design: Fiction-First Sentences + Weighted Distribution

Every template needs a **fiction-first opening sentence** (Star Citizen policy) — a sentence that could only be true in STE's universe.

Weak: "Deliver 20 components to Station X."
Strong: "The fabrication run at Ironpeak starts in 40 ticks. Without 20 components, Quartermaster Vael has to choose which repairs to skip."

#### Template Families (Weighted by Player Activity)

| Family | Count | Weight | Description |
|--------|-------|--------|-------------|
| Trade Route | 10 | HIGH (30%) | Deliver goods between stations. The core loop. |
| Supply Crisis | 8 | HIGH (25%) | Respond to shortage/surplus. Rewards urgency. |
| Escort | 5 | MEDIUM (10%) | Protect convoys. Uses existing escort system. |
| Bounty | 6 | MEDIUM (10%) | Eliminate threats. Combat variety. |
| Investigation | 5 | LOW (8%) | Visit/scan locations. Discovery-adjacent. |
| Salvage | 4 | LOW (5%) | Recover goods from derelicts. |
| Diplomacy | 4 | LOW (5%) | Carry messages between factions. |
| Construction | 3 | LOW (3%) | Deliver materials for upgrades. |
| Contraband | 3 | LOW (2%) | Smuggle past tariffs. Risk/reward. |
| Survey | 3 | LOW (2%) | Map/scan new areas. |
| **Total** | **51** | **100%** | |

**Distribution rule**: Weight missions toward the player's current dominant activity. If trading, 55% of available missions are trade/supply. If fighting, more bounties spawn. Elite Dangerous' failure was static distribution — weight dynamically.

#### World-State Text Variants

Each template has 2-3 text variants keyed to world state. This yields ~100-150 authored text blocks from 51 templates.

- **Warfront active**: "The usual route is hot. Valorin patrols interdicting everything west of the line."
- **Embargo**: "Concord locked the lanes, but there's a gap at Station 4 if you time it right."
- **Pentagon link broken**: "Nobody's trading along the Chitin-Valorin corridor anymore. Triple price for rare metals."

#### NPC Contact Names

Add `$NPC_CONTACT_NAME` slot. Name is seed-generated but persistent per station. Over playthroughs, "Vael" becomes a real person.

#### Authoring Volume

| Item | Count | Words |
|------|-------|-------|
| Template briefs (51 × 3 variants) | 153 | ~15,300 (100/brief) |
| NPC contact flavor lines (30 unique) | 30 | ~1,500 (50/line) |
| **Subtotal** | **183** | **~16,800 words** |

#### Code Required

- [ ] Mission distribution weighting system (activity-based spawn weights)
- [ ] `$NPC_CONTACT_NAME` binding token in MissionSystem
- [ ] World-state variant selector in mission text resolution

---

### Priority 3: MISSION_POLISH (FO Commentary)

**Why third**: 390 lines of FO commentary transforms the experience from "quiet game" to "companioned journey." Lowest effort-to-impact ratio.

#### Corrected Design: Event-Triggered, Not Timed

Based on Hades' priority-queue system and BG3's context-blindness complaints:

- Commentary triggers on **significant player actions**, not timers
- Minimum 3 significant actions between FO comments (prevents chatter fatigue)
- Each trigger has a **cooldown** (won't fire again for 50 ticks)
- **Silence triggers** defined: post-death, first 30 seconds of session, during warp transit, after reading logs

#### Commentary Tiers

**Tier 1 — Immediate Reactions** (1-2 lines, inline with gameplay):

| Trigger | Example (Analyst) | Example (Veteran) | Example (Pathfinder) |
|---------|-------------------|-------------------|---------------------|
| Price anomaly >35% | "Rare metals at 147% of median. Not seasonal — something structural." | "Price way up. Either supply broke or someone's hoarding for a fight." | "Look at that spread. Someone's about to make a fortune." |
| First visit to new market | "Eight commodities. The spread between {GOOD} here and next system is worth calculating." | "New port. Watch the dock workers — they'll tell you what's really moving." | "Fresh market. I want to check the exotics board first." |
| Combat engagement | "Engagement probability suggests we focus fire on the lead ship." | "Contact. Weapons hot. Focus on the closest threat." | "Hostiles. Let's make this quick — I want to see what they're carrying." |
| >200% profit trade | "That margin exceeds three standard deviations. Efficient." | "Good haul. Don't get used to it — margins like that attract competition." | "That felt like finding a vein of pure crystal. Where's the next one?" |
| Hull below 50% | "Hull integrity critical. I'm running structural projections." | "We're hit hard. Find a dock or find cover." | "That's a lot of warning lights. Let's not push our luck." |

**Count**: 15 triggers × 3 archetypes × 2 lines = **90 lines**

**Tier 2 — Dock Commentary** (fires on docking, references the journey):

| Condition | Example |
|-----------|---------|
| Multi-hop journey (3+) | "Three systems in one run. I noticed the Chitin patrol density shifted." |
| Took hull damage en route | "We lost 8 hull on that last fracture. Worth accounting for before we route through again." |
| Profitable trade | "Net positive. The {GOOD} spread was the right call." |
| Unprofitable trade | "We lost credits on that run. Market shifted while we were in transit." |
| First faction visit | "First time in {FACTION} space. Different pace here." |

**Count**: 10 conditions × 3 archetypes × 2 lines = **60 lines**

**Tier 3 — Haven Reflective Logs** (full paragraph, discovered in Haven panel):

8 trigger events × 3 FO types = **24 entries** (~150 words each)

Triggers: First dock, first ancient log, first faction NPC visit, Haven combat, endgame path progress, pentagon break, max relationship, final fragment.

#### Authoring Volume

| Item | Count | Words |
|------|-------|-------|
| Tier 1 lines | 90 | ~2,700 (30/line) |
| Tier 2 lines | 60 | ~1,800 (30/line) |
| Tier 3 entries | 24 | ~3,600 (150/entry) |
| Silence trigger definitions | 4 | ~200 |
| **Subtotal** | **178** | **~8,300 words** |

#### Code Required

- [ ] Priority queue system for FO commentary (minimum-gap timer between triggers)
- [ ] Cooldown per trigger token (50-tick minimum between same trigger)
- [ ] Silence trigger suppression list
- [ ] Haven log panel UI (display Tier 3 entries)

---

### Priority 4: NARRATIVE_CONTENT (Haven Logs + Endgame)

#### Haven Ancient Logs — 14 Entries

Three-layer architecture (see research doc Section 2a for full detail):

| Layer | Entries | System Need |
|-------|---------|-------------|
| A: Ancient logs (fixed, discovered at tier unlocks) | 14 | Content-only (add to existing AccommodationProgress) |
| B: FO reflective entries (at trigger events) | 24 | Covered in P3 above |
| C: Player journey chronicle (auto-generated) | 10 templates | New: timestamped event log system |

**Ancient log tone rules**:
- Operational logs: clipped, professional, mundane. The mundanity IS the point.
- Research logs: genuine intellectual passion. Arguments between colleagues.
- Personal logs: private, unguarded. Gap between their expectations and what the player knows = dramatic irony.
- Philosophical logs: careful, weighty. Modal uncertainty — "if the threading holds."

#### Endgame Narratives

**Knowledge gates per path** (beyond mechanical requirements):

| Path | Must Have Discovered | Why |
|------|---------------------|-----|
| Reinforce | Fragments 1-6 + Concord chain M3 | Must understand containment is failing |
| Naturalize | Fragments 7-10 + Communion chain M3 | Must understand the ancient compact |
| Renegotiate | All 12 fragments + Communion M5 + Haven Tier 4 | Must understand full accommodation physics |

**Epilogue cards**: 10 total (2 per faction based on relationship state). Written in second person, present tense:
> "The Weavers completed the bridge. You are not there to see it. They named one span after you anyway."

**Victory trigger** (CRITICAL GAP — code exists as enum but never fires):
- Reinforce: Haven Tier 4 + all fracture vents sealed + Concord standing Allied
- Naturalize: Haven Tier 4 + Pentagon Resonance Array complete + all faction standings Neutral+
- Renegotiate: Haven Tier 4 + Precursor Relay active + Communion M5 complete

#### Authoring Volume

| Item | Count | Words |
|------|-------|-------|
| Ancient logs | 14 | ~2,800 (200/log) |
| Epilogue cards | 10 | ~1,500 (150/card) |
| Victory scene text (3 paths) | 3 | ~1,500 (500/scene) |
| Knowledge web entries (authored content for 69 nodes) | 69 | ~10,350 (150/entry) |
| **Subtotal** | **96** | **~16,150 words** |

#### Code Required

- [ ] Victory trigger conditions per path
- [ ] Loss condition triggers (hull ≤ 0, credits ≤ 0 for N ticks)
- [ ] Epilogue scene system (text + FO voice + camera)
- [ ] Haven chronicle log (timestamped event stream)
- [ ] Knowledge gate checks before path selection

---

### Priority 5: FACTION_IDENTITY_REDESIGN (Signature Mechanics + T2 Modules)

#### Corrected Design: Phased Implementation

Unique faction verbs cost 3x normal features (unique UI + tutorial + balance). Phase them:

**EA Launch (2 verbs)**:
| Verb | Faction | Why First |
|------|---------|-----------|
| **Commission** | Weavers | Lowest complexity. "Place custom module order" = new mission type + timer. No new UI paradigm. |
| **Conscript** | Valorin | "Hire mercenary escort" = existing escort system + faction standing check. Minimal new code. |

**Wave 1 Post-EA (2 verbs)**:
| Verb | Faction | Complexity |
|------|---------|-----------|
| **Broker** | Chitin | Medium. Requires trade intel data pipeline + sell interface. |
| **Commune** | Communion | Medium. Requires hidden-state revelation system + standing check. |

**Wave 2 Post-EA (1 verb)**:
| Verb | Faction | Complexity |
|------|---------|-----------|
| **Arbitrate** | Concord | Highest complexity. Multi-faction treaty system = new diplomacy layer. |

**Hostile restrictions** (implement all at EA — these are simpler):
| Faction | Restriction | Implementation |
|---------|------------|----------------|
| Concord | Double tariffs | Modify tariff calc — trivial |
| Chitin | Wider price prediction intervals | Modify market UI confidence display |
| Weavers | Cap repairs at 50% | Modify repair command max |
| Valorin | Active interdiction | Modify patrol AI targeting |
| Communion | Block exotic crystal supply | Modify trade availability check |

#### T2 Modules — 40 Total (8 per faction)

The existing `ModuleDef` supports `FactionId` + `FactionRepRequired`. All 40 modules are content-only additions.

**Design rule**: Each module must enable a playstyle that is **impossible without it**. Not "10% better stats" — "this build doesn't exist without this module."

**Module framework per faction** (8 each):

| Slot | Concord | Chitin | Weavers | Valorin | Communion |
|------|---------|--------|---------|---------|-----------|
| Weapon 1 | Standard Railgun (zero variance) | Metamorphic Cannon (adapts to weakness) | Welding Beam (heals allies) | Kinetic Accelerator (extreme range) | Harmonic Lens (bypasses shields) |
| Weapon 2 | Point Defense Matrix (intercepts) | Hivemind Targeting (scales w/ allies) | Construction Drones (repair+attack) | Swarm Coordinator (drone swarm) | Crystal Hull Coating (self-heal) |
| Defense | Diplomatic Shield (shares damage) | Adaptive Carapace (hardens per hit) | Composite Layering (regen OOC) | Reactive Armor (retaliates) | Silence Field (10s invisibility) |
| Drive | Institutional (15% less fuel) | Phase-Shift (micro-jump, no gates) | Echo Drive (fast but hull stress) | Raid Drive (fastest, fragile) | Meditation Drive (-30% fracture CD) |
| Utility 1 | Logistics Optimizer (+cargo contextual) | Data Siphon (collects trade intel) | Structural Reinforcement (+2 slots) | Kill-Mark Targeter (scales w/ kills) | Void Sense (+3 hop discovery range) |
| Utility 2 | Fleet Command Relay (NPC accuracy) | ECM Suite (reduces enemy sensors) | Tractor Fabricator (auto-salvage) | Salvage Rights Scanner (+loot) | Phase Attunement (-50% deep stress) |
| Special 1 | Audit Scanner (reveals tariffs) | Probability Engine (market predictions) | Resonance Forge (Commission enabler) | Conscription Beacon (merc enabler) | Resonance Receiver (Commune enabler) |
| Special 2 | Regulatory Transponder Mk II (zero tariff+tracked) | Signal Broker (Broker enabler) | Load-Bearing Frame (-25% Haven cost) | Garrison Beacon (claims system) | Frequency Emitter (endgame transmission) |

#### Ship Variant Visual Language

| Faction | Silhouette | Color | Material |
|---------|-----------|-------|----------|
| Concord | Bilateral symmetry, docking collars, rounded | White/grey | Polished institutional metal |
| Chitin | Segmented carapace, asymmetric sensors | Dark amber | Organic plates, amber glow |
| Weavers | Layered plates, visible scaffolding, thick | Earth tones | Composite panels, exposed frame |
| Valorin | Angular, forward-swept weapons, engine-heavy | Grey/red accents | Ablative armor, kill-marks |
| Communion | Crystalline protrusions, no crew windows | Purple/iridescent | Crystal growths, phase-shimmer |

#### Authoring Volume

| Item | Count | Words |
|------|-------|-------|
| Module descriptions (40) | 40 | ~2,000 (50/desc) |
| Module lore flavor text (40) | 40 | ~2,000 (50/text) |
| Signature mechanic UI text (5) | 5 | ~500 (100/mechanic) |
| Ship variant descriptions (5 × 3 hull classes) | 15 | ~1,500 (100/desc) |
| **Subtotal** | **100** | **~6,000 words** |

#### Code Required

- [ ] Commission system (new mission type + timer + custom ModuleDef output)
- [ ] Conscript system (temp escort from Valorin standing, doesn't use fleet cap)
- [ ] Broker system (trade intel data package + sell to third faction)
- [ ] Commune system (hidden state revelation within N hops)
- [ ] Arbitrate system (multi-faction treaty proposal — post-EA)
- [ ] Hostile restriction modifiers (5 penalty systems)
- [ ] Module scripted effects beyond stat bonuses (ECM, stealth, conditional)

---

### Priority 6: MEGAPROJECT_SET (4-5 Canonical Types)

#### Corrected Design: Destinations, Not Gates

Stellaris data: only 30% of players reach megastructure tech. Megaprojects should be satisfying **destinations** for invested players, not required for core experience. The capstone rule (Factorio) applies: each megaproject demands engagement with 3+ game systems.

#### Canonical Types

| Megaproject | Fantasy | Path Alignment | Systems Touched |
|-------------|---------|---------------|-----------------|
| **Haven Citadel** | "I built a home" | Core (all paths) | Trade + Construction + Diplomacy |
| **Pentagon Resonance Array** | "I united the factions" | Naturalize | Diplomacy + Exploration + Trade |
| **Precursor Relay Network** | "I unlocked the secrets" | Renegotiate | Exploration + Research + Combat (void sites) |
| **Warfront Fortress** | "I ended the wars" | Reinforce | Combat + Trade + Diplomacy |
| **Knowledge Singularity** | "I understood everything" | Any (bonus) | All 5 systems (completionist capstone) |

**Stage structure** (4 stages each, partial benefits at each):
1. Survey/Plan → 2. Foundation → 3. Construction → 4. Activation

**The megaproject log**: Visible in Haven UI. Every resource contributed, every stage cleared, with timestamp. Personal narrative of the playthrough. Low code cost, high immersion value.

#### Authoring Volume

| Item | Count | Words |
|------|-------|-------|
| Megaproject descriptions (5) | 5 | ~1,000 (200/desc) |
| Stage descriptions (5 × 4 stages) | 20 | ~2,000 (100/stage) |
| Completion FO speeches (5 × 3 archetypes) | 15 | ~3,000 (200/speech) |
| Megaproject log templates | 10 | ~500 (50/template) |
| **Subtotal** | **50** | **~6,500 words** |

#### Code Required

- [ ] Extend `MegaprojectDef` with multi-system requirement checks
- [ ] Cascading megaproject dependencies (haven tier gates others)
- [ ] Visual indicator at galaxy-map nodes for active/complete megaprojects
- [ ] Megaproject completion event (FO speech + achievement)
- [ ] Megaproject log system (timestamped entries)

---

### Priority 7: CONTENT_WAVES (Module Families + Tier Progression)

#### Build Identities (The Coherence Test)

Each player should describe their build in one sentence. 8 target identities:

| Build | Key Modules | Sentence |
|-------|-------------|----------|
| Long-Haul Trader | Large cargo, efficient drive, logistics optimizer | "I make money by hauling bulk across the pentagon." |
| Speed Runner | Fast drive, minimal hull, nav upgrades | "I cover maximum distance and find everything first." |
| Combat Specialist | Weapons, armor, targeting | "I clear warfronts and collect bounties." |
| Fleet Commander | Command relay, escort modules | "I lead NPC fleets into battle." |
| Deep Explorer | Fracture drive, scanner, phase attunement | "I push into Phase 3-4 and find Accommodation sites." |
| Info Broker | Sensors, ECM, data siphon, probability engine | "I trade on intel and avoid fights." |
| Station Builder | Construction modules, tractor, load-bearing frame | "I build Haven and supply construction projects." |
| Diplomatic Agent | Audit scanner, transponder, diplomatic shields | "I arbitrate between factions." |

#### Wave Strategy

| Wave | Content | Timing |
|------|---------|--------|
| EA Launch | 54 existing modules (sufficient for 8 build identities) + 2 signature mechanics | Launch |
| Wave 1 | 16 faction T2 modules (highest-impact 2 per faction) | Month 1-2 |
| Wave 2 | Remaining 24 faction T2 modules (complete all 40) | Month 3-4 |
| Wave 3 | Mk II/III tier progression for 10 most-used base modules | Month 5-6 |
| Wave 4 | Cross-faction hybrid modules (require 2-faction standing) | Post-EA |
| Wave 5 | Precursor T3 modules, ancient ship hulls | Post-EA |

#### Cross-Faction Modules (Pentagon Ring Expression)

| Module | Required Standing | Why It Works |
|--------|------------------|-------------|
| Diplomatic Hull Coating | Concord + Weavers | Supply chain mastery + building mastery |
| Adaptive Sensor Grid | Chitin + Communion | Information science + mystical perception |
| Reinforced Swarm Bay | Valorin + Chitin | Military hardware + targeting intelligence |
| Crystal-Composite Armor | Communion + Weavers | Exotic material + structural engineering |
| Strike Logistics Module | Concord + Valorin | Supply chain + weapons platform |

---

### Priority 8: MUSIC (Soundtrack)

#### Corrected Design: Custom Godot Implementation Required

**No ready-made Godot 4 adaptive music plugin exists** (as of training cutoff). FMOD has an unofficial community module. Most Godot implementations are custom `AudioStreamPlayer` layering.

#### Minimum Viable Music System

**`MusicSystem.gd`** autoload:
- 5 states: PEACEFUL / ALERT / COMBAT / FACTION_TERRITORY / DOCKED
- `AudioStreamPlayer` per stem layer
- `Tween` for crossfades (0.3s combat entry, 3s combat exit, 6s territory change)
- State driven by SimBridge queries (combat state, territory, dock state)
- Audio bus hierarchy: Master → Music → SFX → Dialogue → Ambience

#### Budget Reality Check

| Approach | Cost | Quality | Timeline |
|----------|------|---------|----------|
| AI-assisted placeholder (AIVA/Soundraw) | $50-200/month | Functional, generic | Immediate |
| Solo indie composer (FTL model) | $5K-15K | Distinctive, authored | 2-4 months |
| Hybrid (AI stems + human polish) | $3K-8K | Good, semi-unique | 1-2 months |

**Recommendation**: Start with AI placeholder stems to validate the adaptive system architecture. Commission a composer for final stems once the system is proven. This avoids paying for music that doesn't fit the adaptive transitions.

#### Faction Timbral Palettes (Composer Brief)

| Faction | Palette | Reference |
|---------|---------|-----------|
| Concord | Clean brass, sustained strings, neutral reverb | Civilization opening |
| Chitin | Processed clicks, marimba, gamelan, irregular meter (5/8) | Hollow Knight |
| Weavers | Low cello, hammer dulcimer, anvil percussion, steady pulse | Dwarf Fortress forge |
| Valorin | Military snare, low brass, staccato strings, march tempo | Mass Effect "Suicide Mission" |
| Communion | Singing bowls, prepared piano, long reverb, choir vowels | Ico / NMS ambient |

**Pentagon adjacency blending**: Adjacent factions share a timbral element. In mixed systems, blend palettes. This makes faction territory musically tangible.

**Silence as design element**: Communion zones have longer silence gaps between phrases. Their lore IS their audio design.

#### Track List (14 minimum)

"Open Space" (flight), "The Lanes" (transit), "Tension" (contested), "Engagement" (combat), "Station Air" (docked), 5 faction themes, "Haven" (home), "The Threading" (fracture), "Pentagon Break" (crisis), "Resolution" (credits, 3 variants)

14 tracks × 3-4 stems = **~50 stem files**

#### Code Required

- [ ] `MusicSystem.gd` autoload (state machine + stem mixing)
- [ ] Audio bus hierarchy setup in Godot project
- [ ] SimBridge music state query (`GetMusicContextV0`)
- [ ] Faction territory detection for theme selection
- [ ] Combat state detection for intensity transitions

---

## Total Authoring Summary

| Epic | Items | Words | Priority |
|------|-------|-------|----------|
| FACTION_STORYLINES | 225 | ~21,500 | P1 |
| TEMPLATE_MISSIONS | 183 | ~16,800 | P2 |
| MISSION_POLISH (FO commentary) | 178 | ~8,300 | P3 |
| NARRATIVE_CONTENT | 96 | ~16,150 | P4 |
| FACTION_IDENTITY | 100 | ~6,000 | P5 |
| MEGAPROJECT_SET | 50 | ~6,500 | P6 |
| CONTENT_WAVES | (module defs, minimal text) | ~1,000 | P7 |
| MUSIC | (stems, minimal text) | ~2,100 | P8 |
| **TOTAL** | **~832 items** | **~78,350 words** |

## Code Development Required (Across All Epics)

### Must-Have for EA

| System | Effort | Epic |
|--------|--------|------|
| Victory/loss trigger conditions | Small | P4 |
| Mission faction pool filtering | Small | P1 |
| FO commentary priority queue + cooldowns | Medium | P3 |
| Commission mechanic (Weavers) | Medium | P5 |
| Conscript mechanic (Valorin) | Medium | P5 |
| Hostile faction restrictions (5) | Small | P5 |
| Haven chronicle log | Medium | P4 |
| Music system (basic 5-state) | Medium | P8 |

### Can Defer Post-EA

| System | Effort | Epic |
|--------|--------|------|
| Arbitrate mechanic (Concord) | Large | P5 |
| Broker mechanic (Chitin) | Medium | P5 |
| Commune mechanic (Communion) | Medium | P5 |
| Module scripted effects | Large | P5/P7 |
| Megaproject cascading dependencies | Medium | P6 |
| Megaproject galaxy-map visuals | Medium | P6 |
| Epilogue cinematic system | Medium | P4 |
| Cross-faction hybrid modules | Small | P7 |

---

## Recommended Execution Sequence

1. **Now**: Author Concord + Communion faction chains (most accessible + most unique genres). Author 15 trade/supply mission templates.
2. **Next**: Implement FO commentary system + author Tier 1 lines. Implement Commission + Conscript mechanics.
3. **Then**: Author remaining 3 faction chains. Author remaining 36 mission templates.
4. **Then**: Author Haven ancient logs. Implement victory conditions. Author epilogue cards.
5. **Then**: Build music system with placeholder stems. Author T2 module descriptions.
6. **Then**: Design megaproject details. Author knowledge web entries.
7. **Post-EA**: Implement remaining signature mechanics. Commission final soundtrack. Content waves.
