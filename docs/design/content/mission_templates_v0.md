# Mission Templates v0 — Procedural Mission Content

> 59 templates across 13 families. Each template includes fiction-first briefing,
> world-state variants, NPC contact flavor, binding tokens, step structure, and
> reward tier. Companion to `Mission.cs` (entity model) and `MissionContentV0.cs`
> (content registry).

---

## Spawn Weight Table

| Family | Count | Weight | Cumulative |
|---|---|---|---|
| Trade Route | 10 | 25% | 25% |
| Supply Crisis | 8 | 22% | 47% |
| Escort | 5 | 10% | 57% |
| Bounty | 6 | 9% | 66% |
| Investigation | 5 | 7% | 73% |
| Salvage | 4 | 5% | 78% |
| Diplomacy | 4 | 5% | 83% |
| Construction | 3 | 3% | 86% |
| Contraband | 3 | 2% | 88% |
| Survey | 3 | 2% | 90% |
| Discovery | 3 | 3% | 93% |
| Celebration | 2 | 2% | 95% |
| Ceremony | 3 | 5% | 100% |

---

## 1. Trade Route (10 templates, 30% weight)

### TRADE_01 — Fabrication Deadline

**Briefing:** The fabrication run at $TARGET_NODE starts in 40 ticks. Without 20 $MARKET_GOOD_1, Quartermaster Vael has to choose which hull plates to skip — and skipped plates mean the next convoy out flies with exposed seams. Deliver 20 $MARKET_GOOD_1 from $PLAYER_START to $TARGET_NODE before the deadline.

**Variants:**
- *warfront_active:* "The fabrication queue just doubled — $FACTION_1 requisitioned every plate we had for warfront repairs. Vael needs 30 units now, not 20, and half the usual suppliers won't cross the contested lane."
- *embargo_active:* "Concord customs flagged our usual shipment. The embargo means $MARKET_GOOD_1 has to come through unofficial channels. Vael doesn't care how it arrives, only that it does."

**NPC Contact:** $CONTACT_NAME (e.g., Quartermaster Seren Drossik). *"I've been patching this station together with spit and promises for three cycles. Don't make me patch it with prayers too."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1, $FACTION_1, $CONTACT_NAME

**Steps:**
1. Purchase/acquire 20 $MARKET_GOOD_1 (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver cargo (NoCargoAtNode)

**Reward:** Medium credits + $FACTION_1 rep (+5)

---

### TRADE_02 — The Long Haul

**Briefing:** Chitin probability markets just priced $MARKET_GOOD_1 at a 340% premium two hops down the pentagon ring. The spread won't last — every clipper captain with a cargo bay is already spinning up their drives. But you're closer. Two jumps, clean lanes, and you pocket the difference before the Syndicate arbitrage bots flatten the curve. Buy at $PLAYER_START, sell at $TARGET_NODE.

**Variants:**
- *warfront_active:* "One of those two jumps crosses contested space. The premium just went to 400% because half the haulers turned back. Your call."
- *pentagon_link_broken:* "The direct lane is down — fracture event collapsed the thread between $ADJACENT_1 and $TARGET_NODE. Three-hop detour or fracture jump. Either way, the window's shrinking."

**NPC Contact:** $CONTACT_NAME (e.g., Broker Tiesh Miraal). *"The spread is real, I ran the model four times. But spreads are like sunsets — beautiful and brief."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $ADJACENT_1, $MARKET_GOOD_1, $CONTACT_NAME

**Steps:**
1. Purchase 15+ $MARKET_GOOD_1 at $PLAYER_START (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Sell all $MARKET_GOOD_1 (NoCargoAtNode)

**Reward:** High credits (scales with actual sale price)

---

### TRADE_03 — Subsistence Run

**Briefing:** The hydroponics bay at Communion station $TARGET_NODE failed nine ticks ago. They've been rationing since tick three. The elders won't ask for help — pride, or maybe faith that the shimmer provides. Their children don't eat faith. Deliver 25 food before the rationing turns permanent.

**Variants:**
- *warfront_active:* "Valorin raiders hit the relief convoy last cycle. The Communion won't arm their ships. Someone who can fight and haul needs to make this run."
- *embargo_active:* "Concord suspended food subsidies to Communion stations pending a 'regulatory review.' The review is political. The hunger is real."

**NPC Contact:** $CONTACT_NAME (e.g., Elder Oressi Kael). *"We have observed that supply ships which dock here tend to leave with fewer provisions and more shimmer-frequency data. The exchange has been... mutually informative."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (food), $CONTACT_NAME

**Steps:**
1. Acquire 25 food (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver food (NoCargoAtNode)

**Reward:** Low credits + Communion rep (+10). Communion stations remember — future docking discount.

---

### TRADE_04 — Munitions Resupply

**Briefing:** The Valorin border garrison at $TARGET_NODE burned through their munitions reserve in a skirmish with Weaver defense drones three ticks ago. Clan-Captain Drenn has fighters sitting in launch bays with empty magazines. Every tick without resupply is a tick where the next Weaver probe finds an open door. Deliver 15 munitions.

**Variants:**
- *warfront_active:* "The skirmish wasn't a skirmish — it was the opening salvo. Drenn needs 25 munitions now and is paying combat rates."
- *pentagon_link_broken:* "The Concord lane that usually supplies Valorin munitions components is down. Drenn is offering triple rate because he's eight ticks from pulling his garrison back."

**NPC Contact:** $CONTACT_NAME (e.g., Clan-Captain Drenn Vassk). *"My pilots are brave. Brave pilots with empty guns are dead pilots. How fast can you move?"*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (munitions), $FACTION_1 (Valorin), $CONTACT_NAME

**Steps:**
1. Acquire 15 munitions (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver munitions (NoCargoAtNode)

**Reward:** Medium credits + Valorin rep (+8)

---

### TRADE_05 — Crystal Calibration Shipment

**Briefing:** Weaver navigational arrays require exotic crystal recalibration every 200 ticks. Station $TARGET_NODE is 15 ticks past due. Their lane predictions are drifting — last cycle a freighter followed a Weaver nav-fix into an asteroid that wasn't on the chart three ticks earlier. The Weavers won't say "urgent." They'll say "the tension pattern requires attention." It means the same thing. Deliver 10 exotic_crystals.

**Variants:**
- *warfront_active:* "Valorin forward scouts are probing Weaver space. Without calibrated arrays, the Weavers can't distinguish scout signatures from merchant traffic. Every misidentification risks an incident."
- *embargo_active:* "Communion crystal harvesters are refusing to sell to Weaver intermediaries — some old grudge about shimmer-zone access rights. The crystals need to come from someone outside the dispute."

**NPC Contact:** $CONTACT_NAME (e.g., Threadkeeper Lisse Vohn). *"The array is not broken. It is... imprecise. We dislike imprecision the way you dislike vacuum."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (exotic_crystals), $CONTACT_NAME

**Steps:**
1. Acquire 10 exotic_crystals (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver crystals (NoCargoAtNode)

**Reward:** Medium credits + Weavers rep (+5)

---

### TRADE_06 — Pentagon Relay

**Briefing:** A shipment of $MARKET_GOOD_1 was supposed to move through the standard pentagon chain — $FACTION_1 to $FACTION_2 — but the regular hauler's drive gave out at $ADJACENT_1 and the cargo is sitting in a holding bay accruing storage fees. The receiving station doesn't care about excuses; they care about delivery windows. Pick up at $ADJACENT_1, deliver to $TARGET_NODE.

**Variants:**
- *warfront_active:* "The hauler didn't break down. She turned back when she saw weapons fire on long-range scan. Smart woman. The cargo still needs to move."
- *pentagon_link_broken:* "The thread between $ADJACENT_1 and $TARGET_NODE collapsed. The cargo is now three hops from its destination instead of one. Storage fees are eating the margin, but the contract penalty for non-delivery is worse."

**NPC Contact:** $CONTACT_NAME (e.g., Dispatcher Fen Okari). *"I don't need heroes, I need someone with cargo space and a working drive. You qualify on both counts?"*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $MARKET_GOOD_1, $FACTION_1, $FACTION_2, $CONTACT_NAME

**Steps:**
1. Arrive at $ADJACENT_1 (ArriveAtNode)
2. Pick up cargo — 20 $MARKET_GOOD_1 auto-loaded (HaveCargoMin)
3. Arrive at $TARGET_NODE (ArriveAtNode)
4. Deliver cargo (NoCargoAtNode)

**Reward:** Medium credits

---

### TRADE_07 — Rare Metal Rush

**Briefing:** A Valorin prospector clan struck a rare metal vein in the frontier and they're selling at extraction cost — no markup, no Chitin spread, no Concord tariff. The catch: they'll sell to the first ship that docks, and three Chitin clippers are already in transit. Get to $TARGET_NODE, buy 12 rare_metals, and bring them to $PLAYER_START before the price normalizes.

**Variants:**
- *warfront_active:* "The Chitin clippers turned back — warfront patrols are boarding Syndicate-flagged vessels. The Valorin are still selling, but the lane has teeth now."
- *embargo_active:* "Concord just classified rare metals as strategic materiel. Anyone carrying them through Concord space without a transit permit gets impounded. The Valorin don't issue permits."

**NPC Contact:** $CONTACT_NAME (e.g., Prospector Kaeli Thresh). *"We dig, you haul. Simple arrangement. Don't make it complicated by showing up late."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (rare_metals), $CONTACT_NAME

**Steps:**
1. Arrive at $TARGET_NODE (ArriveAtNode)
2. Purchase 12 rare_metals (HaveCargoMin)
3. Return to $PLAYER_START (ArriveAtNode)

**Reward:** High credits (buy-low margin)

---

### TRADE_08 — Component Shortage Chain

**Briefing:** Concord engineering yards consume components the way stars consume hydrogen — constantly and without gratitude. Station $TARGET_NODE just placed an emergency order because their universal mount adapter production line seized. Three stations in your sector have partial stocks. No single station has enough. You'll need to aggregate from multiple stops. Deliver 30 components total.

**Variants:**
- *warfront_active:* "Half the component supply is being diverted to military refit yards. Civilian allocations are being 'deferred.' The engineering chief is offering hazard pay because the components you'll be carrying are now classified as military logistics."
- *pentagon_link_broken:* "The component chain runs through Chitin intermediaries, and the thread to their nearest trade hub just failed. You'll need to source from whoever has stock, wherever that is."

**NPC Contact:** $CONTACT_NAME (e.g., Chief Engineer Maros Thane). *"I can build anything in this galaxy if you give me the parts. Right now I can't build a sandwich."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $ADJACENT_1, $MARKET_GOOD_1 (components), $CONTACT_NAME

**Steps:**
1. Acquire 30 components — any source (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver components (NoCargoAtNode)

**Reward:** High credits + Concord rep (+5)

---

### TRADE_09 — Salvage Brokerage

**Briefing:** A Communion drifter found a derelict two sectors out and stripped it clean — 18 units of salvaged_tech sitting in their cargo hold. They don't want credits; they want fuel. Their shimmer-drives run on exotic matter, but conventional thrusters need conventional fuel, and they're running dry. Trade 18 fuel for their 18 salvaged_tech at $TARGET_NODE, then sell the tech wherever you like.

**Variants:**
- *warfront_active:* "The drifter is parked in contested space. They didn't notice — Communion ships don't carry threat scanners. You'll need to dock fast and leave faster."
- *embargo_active:* "Concord considers salvaged_tech an 'unregulated technology product.' If customs catches you carrying it through monitored lanes, they confiscate. Plan your route accordingly."

**NPC Contact:** $CONTACT_NAME (e.g., Drifter Solanei). *"The metal sings if you hold it right. We don't need the singing. We need the fuel."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (fuel), $MARKET_GOOD_2 (salvaged_tech), $CONTACT_NAME

**Steps:**
1. Acquire 18 fuel (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Exchange fuel for salvaged_tech (NoCargoAtNode + HaveCargoMin salvaged_tech)

**Reward:** Medium credits (from resale) + Communion rep (+5)

---

### TRADE_10 — Election Supply Contract

**Briefing:** Every 500 ticks, Concord stations hold administrative rotations — what they call "elections" and what everyone else calls "bureaucratic furniture rearrangement." The incoming administrator at $TARGET_NODE needs to demonstrate competence in her first 20 ticks. She's placed a standing order for electronics, composites, and metal — the three goods that make station infrastructure visibly improve. Deliver all three.

**Variants:**
- *warfront_active:* "The new administrator is a war hawk. Half her order is composites for defensive hardening. She's paying military rates and asking no questions about sourcing."
- *pentagon_link_broken:* "The thread failure means $TARGET_NODE is cut off from its usual Weaver composites supplier. The new administrator is panicking quietly. She needs the goods more than she's letting on."

**NPC Contact:** $CONTACT_NAME (e.g., Administrator-Elect Jenna Sorath). *"I have 20 ticks to prove I'm not the worst appointment this station has ever seen. Help me clear that extremely low bar."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (electronics), $MARKET_GOOD_2 (composites), $MARKET_GOOD_3 (metal), $CONTACT_NAME

**Steps:**
1. Acquire 10 electronics (HaveCargoMin)
2. Acquire 10 composites (HaveCargoMin)
3. Acquire 8 metal (HaveCargoMin)
4. Arrive at $TARGET_NODE (ArriveAtNode)
5. Deliver all goods (NoCargoAtNode x3)

**Reward:** High credits + Concord rep (+8)

---

## 2. Supply Crisis (8 templates, 25% weight)

### SUPPLY_01 — Famine Response

**Briefing:** The agricultural ring at $TARGET_NODE suffered a coolant breach. Sixteen hydroponic bays are offline and 4,000 people are eating emergency rations that expire in 30 ticks. Concord relief coordination flagged this as Priority Two — which means the bureaucracy will respond in 60 ticks. The people will be hungry in 25. Deliver 30 food before the rations run out.

**Variants:**
- *warfront_active:* "Relief convoys are being diverted to warfront staging areas. The agricultural failure isn't classified as 'defense-critical.' The people eating emergency rations might disagree."
- *embargo_active:* "The station is in embargo territory. Concord relief coordination can't officially authorize shipments. Unofficially, the coordinator left a cargo manifest on an unsecured terminal."

**NPC Contact:** $CONTACT_NAME (e.g., Relief Coordinator Asha Dunn). *"Priority Two means someone in an office decided this isn't urgent enough. I'd like that someone to eat emergency rations for a week and reassess."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (food), $CONTACT_NAME

**Steps:**
1. Acquire 30 food (HaveCargoMin)
2. Arrive at $TARGET_NODE before deadline (ArriveAtNode + TimerExpired at 30 ticks)
3. Deliver food (NoCargoAtNode)

**Reward:** Medium credits + faction rep (+10) + future food price discount at $TARGET_NODE

---

### SUPPLY_02 — Fuel Drought

**Briefing:** The refinery at $ADJACENT_1 went dark — no explanation, no timeline, just silence on all hailing frequencies. Every station within two hops is burning through fuel reserves. $TARGET_NODE has 12 ticks of fuel left before they start cold-shutting systems bay by bay, starting from the outer ring and working inward. Deliver 20 fuel.

**Variants:**
- *warfront_active:* "Military requisitions are siphoning fuel from civilian reserves. The station commander at $TARGET_NODE just received a requisition order for fuel she doesn't have."
- *pentagon_link_broken:* "The refinery didn't go dark — the thread to the refinery collapsed. The fuel is there. The lane isn't."

**NPC Contact:** $CONTACT_NAME (e.g., Station Manager Kel Bratta). *"I'm about to tell 800 people in the outer ring that their air recyclers are 'non-essential.' I'd rather not."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $MARKET_GOOD_1 (fuel), $CONTACT_NAME

**Steps:**
1. Acquire 20 fuel (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver fuel (NoCargoAtNode)

**Reward:** Medium credits + faction rep (+8)

---

### SUPPLY_03 — Surplus Dump

**Briefing:** Chitin speculative markets over-indexed on $MARKET_GOOD_1 and three Syndicate warehouses at $PLAYER_START are bursting. The Chitin don't panic — they recalculate. Their probability models say dumping the surplus at $TARGET_NODE, where demand just spiked from a construction project, nets a 15% recovery on the overinvestment. They need a hauler who can move volume fast. Transport 40 $MARKET_GOOD_1.

**Variants:**
- *warfront_active:* "The demand spike at $TARGET_NODE is military construction. Chitin surplus meets Valorin war machine. Everyone profits. Probably."
- *embargo_active:* "The receiving station is under trade restrictions. The Chitin suggest 'creative invoicing' — list the goods as something else. You'll carry the customs risk."

**NPC Contact:** $CONTACT_NAME (e.g., Syndicate Comptroller Raza Miir). *"The model was correct within stated confidence intervals. The confidence interval was too wide. Hence: you."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1, $CONTACT_NAME

**Steps:**
1. Load 40 $MARKET_GOOD_1 at $PLAYER_START (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Sell/deliver all cargo (NoCargoAtNode)

**Reward:** Medium credits (flat fee — Chitin eats the loss)

---

### SUPPLY_04 — Embargo Workaround

**Briefing:** A Concord administrative reclassification moved $MARKET_GOOD_1 from "general trade" to "strategic reserve" fourteen ticks ago. Nobody in $FACTION_1 space was consulted. The reclassification wasn't political — it was an automated response to a supply model that flagged declining stockpiles. The model was correct. The response was disproportionate. Now $FACTION_1 stations that depend on regular $MARKET_GOOD_1 deliveries are rationing while the appeal works through Concord's review process, which takes 60 ticks on a good day. A station manager is willing to pay above-market if someone can deliver before the bureaucracy catches up.

**Variants:**
- *warfront_active:* "The embargo is cover for military positioning. Concord patrols are thicker than usual on the border lanes. Getting through clean requires either a fast ship or a creative route."
- *pentagon_link_broken:* "The thread failure gives you natural cover — Concord patrols can't monitor a lane that doesn't exist. The detour adds time but removes the customs problem."

**NPC Contact:** $CONTACT_NAME (e.g., Station Manager Vriss Tael). *"The appeal is filed. The review board meets in forty ticks. My rationing plan runs out in fifteen. I need someone who can do arithmetic."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1, $FACTION_1, $CONTACT_NAME

**Steps:**
1. Acquire 20 $MARKET_GOOD_1 (HaveCargoMin)
2. Avoid Concord patrol node — optional routing challenge
3. Arrive at $TARGET_NODE (ArriveAtNode)
4. Deliver cargo (NoCargoAtNode)

**Reward:** High credits + $FACTION_1 rep (+5), Concord rep (-3)

---

### SUPPLY_05 — Medical Emergency

**Briefing:** A viral outbreak at $TARGET_NODE is burning through antiviral stocks. The station medic has been synthesizing replacements from raw electronics and composites, but she's out of both. Without resupply in 20 ticks, the quarantine ward overflows into general population. This isn't a market opportunity. This is triage. Deliver 10 electronics and 10 composites.

**Variants:**
- *warfront_active:* "The outbreak started in the refugee quarter — war displaced civilians packed into spaces designed for a third the occupancy. The medic says the war created the crisis. The generals say the crisis is unrelated."
- *embargo_active:* "Medical supplies are technically exempt from embargo. 'Technically' means the customs officer has discretion. The current customs officer is not feeling generous."

**NPC Contact:** $CONTACT_NAME (e.g., Dr. Senna Ahriq). *"I became a doctor to help people. Today I'm rationing who gets to live. Get me those supplies."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (electronics), $MARKET_GOOD_2 (composites), $CONTACT_NAME

**Steps:**
1. Acquire 10 electronics (HaveCargoMin)
2. Acquire 10 composites (HaveCargoMin)
3. Arrive at $TARGET_NODE before deadline (ArriveAtNode + TimerExpired at 20 ticks)
4. Deliver supplies (NoCargoAtNode x2)

**Reward:** Medium credits + faction rep (+12)

---

### SUPPLY_06 — Ore Glut Arbitrage

**Briefing:** Three Valorin mining clans hit the same vein in the same cycle. The ore market at $PLAYER_START is flooded — prices are below extraction cost and the miners are threatening to dump raw ore into the lane to make a point. Meanwhile, $TARGET_NODE hasn't seen an ore shipment in 40 ticks because everyone was sending their supply to the now-glutted hub. Buy low here, sell high there. Basic economics that someone needs to actually execute.

**Variants:**
- *warfront_active:* "The ore glut is from wartime overproduction. The miners were told to dig for the war effort. The war doesn't need this much ore. Nobody told the miners."
- *pentagon_link_broken:* "The thread failure between the mining sector and the usual processing stations created the glut. The ore is piling up because the road is gone."

**NPC Contact:** $CONTACT_NAME (e.g., Minemaster Grev Sollan). *"We've got ore coming out of our airlocks. Literally. Take it. Please."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (ore), $CONTACT_NAME

**Steps:**
1. Purchase 35 ore at $PLAYER_START (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Sell ore (NoCargoAtNode)

**Reward:** High credits (market spread)

---

### SUPPLY_07 — Construction Bottleneck

**Briefing:** The Weaver construction project at $TARGET_NODE has been running for 200 ticks and they're three-quarters done building a new lattice anchor node — the kind of infrastructure that stabilizes an entire sector. They just burned through their metal reserves on the structural frame. Without 20 metal in the next 25 ticks, the exposed framework starts degrading. Two hundred ticks of work, undone by a stockpile miscalculation.

**Variants:**
- *warfront_active:* "The lattice anchor isn't just infrastructure — it's strategic. Controlling the anchor means controlling navigation in the sector. Both sides want it finished. Both sides are also shooting near it."
- *embargo_active:* "Metal sourced from Valorin territory is embargoed. The Weavers could buy from Concord at triple price, but their budget was calculated on Valorin rates."

**NPC Contact:** $CONTACT_NAME (e.g., Architect Tessyn Lohm). *"The stress calculations are perfect. The framework is sound. The only variable I didn't model was 'running out of metal.' An oversight I will not repeat."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (metal), $CONTACT_NAME

**Steps:**
1. Acquire 20 metal (HaveCargoMin)
2. Arrive at $TARGET_NODE before deadline (ArriveAtNode + TimerExpired at 25 ticks)
3. Deliver metal (NoCargoAtNode)

**Reward:** Medium credits + Weavers rep (+8)

---

### SUPPLY_08 — Cascading Shortage

**Briefing:** The pentagon ring just demonstrated why it was designed the way it was — and why that design is fragile. A Chitin electronics shortage cascaded into a Weaver composite shortfall, which left Concord stations underequipped to supply Communion waystations, whose harvester crews couldn't produce the exotic crystals that Valorin frontier stations depend on. Five factions, five shortages, one root cause. The Concord dispatch office at $PLAYER_START is coordinating emergency supply runs. They need someone to handle the first leg: deliver 15 electronics to the Chitin hub at $TARGET_NODE to restart the chain.

**Variants:**
- *warfront_active:* "The cascade hit during a warfront escalation. Concord is prioritizing military supply chains, which means civilian cascade recovery is 'deferred.' The dispatcher is going off-book."
- *pentagon_link_broken:* "The cascade was caused by the thread failure. Restarting the chain while the thread is down means routing every shipment the long way around. Twice the distance, same deadline."

**NPC Contact:** $CONTACT_NAME (e.g., Dispatch Coordinator Hess Miran). *"I've got four factions yelling at me in four languages about four different shortages. They're all the same shortage. Help me fix the first one and the rest might fix themselves."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (electronics), $CONTACT_NAME

**Steps:**
1. Acquire 15 electronics (HaveCargoMin)
2. Arrive at $TARGET_NODE (ArriveAtNode)
3. Deliver electronics (NoCargoAtNode)

**Reward:** High credits + Concord rep (+5) + Chitin rep (+3)

---

## 3. Escort (5 templates, 10% weight)

### ESCORT_01 — Relief Convoy

**Briefing:** Concord relief convoy H-7 is carrying medical supplies and food to three stations hit by the coolant cascade. The convoy master is an excellent logistics planner and a terrible fighter. Last time she flew without escort, pirates stripped her lead freighter to the frame while she was calculating optimal unloading sequences. Escort the convoy from $PLAYER_START to $TARGET_NODE.

**Variants:**
- *warfront_active:* "The convoy route passes through contested space. The relief supplies are neutral-flagged, but pirates don't read flags. Neither do stray munitions."
- *embargo_active:* "The convoy is delivering to an embargoed station. Concord officially doesn't know about it. Your escort contract is filed under 'navigational consultation.'"

**NPC Contact:** $CONTACT_NAME (e.g., Convoy Master Lyra Denning). *"I have calculated the optimal route, load distribution, and delivery schedule to within 0.3% variance. I have not calculated how to dodge missiles. That's your department."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Rendezvous at $PLAYER_START (ArriveAtNode)
2. Escort convoy — survive 2 combat encounters en route
3. Arrive at $TARGET_NODE with convoy intact (ArriveAtNode)

**Reward:** Medium credits + Concord rep (+8)

---

### ESCORT_02 — Chitin Clipper Fleet

**Briefing:** A Chitin clipper fleet is making a high-value run through the contested corridor between Syndicate and Valorin space. The cargo manifest is classified, but the insurance premium tells you everything — the Chitin don't pay 800 credits for escort unless what they're carrying is worth 8,000. Escort six clippers from $PLAYER_START to $TARGET_NODE. The clippers are fast. Try to keep up.

**Variants:**
- *warfront_active:* "Valorin patrols are boarding Chitin vessels on suspicion of 'wartime profiteering.' The clippers aren't smuggling — but they're not stopping to prove it, either."
- *pentagon_link_broken:* "The fleet is taking a fracture-adjacent route to avoid the collapsed thread. The probability of pirate encounters just tripled. The insurance premium just quintupled."

**NPC Contact:** $CONTACT_NAME (e.g., Fleet Captain Oriis Zhenn). *"We calculated a 23% chance of hostile contact. We also calculated that paying you is cheaper than losing one clipper. Don't take it personally."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Rendezvous at $PLAYER_START (ArriveAtNode)
2. Escort fleet through 2-3 hostile encounters
3. Arrive at $TARGET_NODE with minimum 4/6 clippers surviving (ArriveAtNode)

**Reward:** High credits + Chitin rep (+5)

---

### ESCORT_03 — Drifter Migration

**Briefing:** The Communion drifter congregation at $PLAYER_START is moving. They don't explain why — they say the shimmer told them. Fourteen ships, including three with no weapons and no shields, carrying families and elders and children who have never seen a station that doesn't move. They need escort through two sectors of space where pirates consider Communion ships free salvage. Protect them.

**Variants:**
- *warfront_active:* "The migration path crosses the warfront. The Communion elders say the shimmer told them to go this way. The shimmer apparently doesn't watch the news."
- *embargo_active:* "Concord border stations are turning away Communion vessels under the embargo. The drifters will need to pass through without docking, which means no emergency resupply if something goes wrong."

**NPC Contact:** $CONTACT_NAME (e.g., Elder Mossenveil). *"We have traveled the shimmer-roads for a thousand years without weapons. We are not proud of this. We simply cannot aim."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Rendezvous with drifter fleet at $PLAYER_START (ArriveAtNode)
2. Escort through 3 potential combat zones
3. Arrive at $TARGET_NODE with zero civilian casualties (ArriveAtNode)

**Reward:** Low credits + Communion rep (+15)

---

### ESCORT_04 — VIP Transport

**Briefing:** A Weaver master architect is traveling to $TARGET_NODE to inspect a lattice anchor under construction. She won't fly armed — active weapon systems create resonance interference that distorts her structural sensitivity — and her personal transport has the defensive capabilities of a cargo container with ambitions. The Weavers are paying well because the architect is irreplaceable. Not sentimentally. Literally. She's the only one who can read the stress patterns in the new anchor design.

**Variants:**
- *warfront_active:* "The inspection site is near the Valorin border. The Weavers suspect the Valorin know about the architect's travel and might try to 'redirect' her expertise."
- *pentagon_link_broken:* "The direct route is down. The detour adds three hostile-probability sectors. The architect refuses to delay. 'The stress patterns wait for no one,' she says, apparently without irony."

**NPC Contact:** $CONTACT_NAME (e.g., Master Architect Revenna Strand). *"I perceive in six dimensions. I navigate in three. This is why I require an escort and not a compass."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Dock at $PLAYER_START to pick up VIP (ArriveAtNode)
2. Escort VIP through 1-2 hostile encounters
3. Deliver VIP to $TARGET_NODE (ArriveAtNode)

**Reward:** High credits + Weavers rep (+10)

---

### ESCORT_05 — Prisoner Transfer

**Briefing:** Concord captured a Valorin raider captain during the last skirmish and is transferring her to a judicial station at $TARGET_NODE. The raider's clan has already made two intercept attempts. Concord doesn't want a martyr — they want a trial. The transport ship has good armor but limited firepower, and the escort budget suggests Concord expects at least one more intercept attempt. Escort the prison transport.

**Variants:**
- *warfront_active:* "The prisoner is a war hero in Valorin territory. Her clan is offering a bounty on anyone who helps Concord move her. Taking this job makes you a target."
- *embargo_active:* "The prisoner knows things about embargo violations that Concord would prefer stayed in a courtroom rather than a Valorin debriefing room. The intercept attempts are escalating."

**NPC Contact:** $CONTACT_NAME (e.g., Marshal Cade Ortun). *"I believe in justice. I also believe in firepower. Right now I'm short on the second one."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $FACTION_1 (Valorin), $CONTACT_NAME

**Steps:**
1. Rendezvous with transport at $PLAYER_START (ArriveAtNode)
2. Escort through 2-3 intercept encounters
3. Deliver transport to $TARGET_NODE (ArriveAtNode)

**Reward:** Medium credits + Concord rep (+8), Valorin rep (-5)

---

## 4. Bounty (6 templates, 10% weight)

### BOUNTY_01 — Pirate Elimination

**Briefing:** Three pirate vessels have been hitting freighters on the lane between $PLAYER_START and $ADJACENT_1 for the past 50 ticks. Fourteen crews dead, cargo worth 3,000 credits lost, and two insurance companies just pulled coverage for the route. The bounty is pooled from a dozen merchants who would rather pay a fighter than lose another shipment. Eliminate all three pirate vessels near $TARGET_NODE.

**Variants:**
- *warfront_active:* "The pirates are former Valorin auxiliaries who went rogue when their contract expired. They still fly military-grade hardware. Budget accordingly."
- *embargo_active:* "The pirates are exploiting the embargo — hitting ships that can't call for Concord assistance because they're carrying embargoed goods. Victims can't report the crime without confessing to one."

**NPC Contact:** $CONTACT_NAME (e.g., Merchant Guildmaster Pol Revik). *"Fourteen dead. I knew six of them by name. Make it stop."*

**Tokens:** $PLAYER_START, $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to pirate patrol area near $TARGET_NODE (ArriveAtNode)
2. Eliminate 3 pirate vessels (combat encounters)
3. Return to $PLAYER_START to confirm kills (ArriveAtNode)

**Reward:** High credits + faction rep (+5)

---

### BOUNTY_02 — Deserter Hunt

**Briefing:** Lieutenant Koss Verain deserted from Concord naval service six ticks ago, taking a patrol corvette, its weapons loadout, and classified navigation data with her. Concord wants the ship back. They say they want Koss back too, but the bounty payout is the same whether she's aboard or not. She was last seen near $TARGET_NODE, running dark. Your First Officer notes that Koss's service record includes a formal complaint about "orders incompatible with oath of service." The complaint was denied.

**Variants:**
- *warfront_active:* "The navigation data Koss took includes warfront patrol routes. If that reaches Valorin intelligence, Concord loses tactical advantage in three sectors. The bounty just doubled."
- *embargo_active:* "Koss publicly denounced the embargo before deserting. She's become a folk hero in $FACTION_1 space. Finding her means someone might help you. Or someone might warn her."

**NPC Contact:** $CONTACT_NAME (e.g., Commander Issara Thorne). *"Koss was a good officer. She's also a thief and a deserter. I can hold both truths. Bring her in."*

**Tokens:** $TARGET_NODE, $FACTION_1, $CONTACT_NAME

**Steps:**
1. Investigate last known position at $TARGET_NODE (ArriveAtNode)
2. Scan for corvette signature (Investigation)
3. Engage or negotiate with Koss (combat or Choice)
4. Return to Concord station (ArriveAtNode)

**Reward:** High credits + Concord rep (+5). Choice: returning Koss alive grants bonus rep; letting her go grants $FACTION_1 rep (+8) instead.

---

### BOUNTY_03 — Syndicate Enforcer

**Briefing:** A Chitin Syndicate enforcer named Razik has been "renegotiating" debts with independent traders using missile locks instead of contracts. Two traders paid. One didn't. She's drifting in a cargo pod near $ADJACENT_1 with a broken transponder and a story about Razik's negotiation style. The Syndicate officially denies Razik's methods while unofficially profiting from the fear he creates. The bounty is posted by the independent traders' guild — not the Chitin.

**Variants:**
- *warfront_active:* "Razik has been hitting warfront supply haulers, which means military logistics are affected. Concord posted a secondary bounty. Combined, it's the highest payout you've seen."
- *pentagon_link_broken:* "The thread failure pushed independent traders onto Razik's turf — they have no choice but to route through his patrol zone. Complaints tripled in a week."

**NPC Contact:** $CONTACT_NAME (e.g., Trader Nella Voss, from her cargo pod). *"He said 'pay or float.' I chose float. I'd like to choose differently next time."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to Razik's patrol zone near $TARGET_NODE (ArriveAtNode)
2. Eliminate Razik's enforcer vessel
3. Return to $ADJACENT_1 to confirm (ArriveAtNode)

**Reward:** Medium credits + independent trader discount at local stations

---

### BOUNTY_04 — Warfront Deserters

**Briefing:** A Valorin scout squadron broke formation during the last engagement and ran. Clan law says cowardice is answered with exile. The clan elders say something harsher — their word translates roughly to "erasure." The bounty is for destruction of three scout vessels. Your First Officer observes that the scouts broke formation when ordered to fire on a Communion medical ship that wandered into the combat zone. The elders didn't mention that part.

**Variants:**
- *warfront_active:* "The scouts are hiding in a debris field near the frontline. Hunting them means operating in active combat space. The elders consider this acceptable risk. For you."
- *embargo_active:* "The scouts fled to Concord space and requested asylum. Concord hasn't responded. The elders want them found before Concord decides to grant it."

**NPC Contact:** $CONTACT_NAME (e.g., Elder Kharv Durran). *"They shamed the clan. The method of their shame is irrelevant."* — Your First Officer, privately: *"The method seems relevant to me."*

**Tokens:** $TARGET_NODE, $FACTION_1 (Valorin), $CONTACT_NAME

**Steps:**
1. Travel to last known position near $TARGET_NODE (ArriveAtNode)
2. Locate 3 scout vessels
3. Choice: Eliminate scouts (Valorin rep +10) OR warn them and help them escape (Valorin rep -10, Communion rep +10)

**Reward:** High credits from Valorin (if eliminated). Choice branch: no credits but Communion gratitude if warned.

---

### BOUNTY_05 — Lattice Parasite

**Briefing:** Something is draining power from the lattice relay near $TARGET_NODE. The Weavers traced it to a modified salvage ship that's literally welded itself to the relay housing and is siphoning energy to run an illegal mining operation in the adjacent asteroid field. The pilot isn't hostile — she's an engineer who found a clever exploit and doesn't understand why the Weavers are upset. The Weavers are upset because the power drain is degrading navigation accuracy for every ship in the sector.

**Variants:**
- *warfront_active:* "The navigation degradation caused a friendly-fire incident in the sector. The Weavers want the parasite removed. The military wants it removed faster."
- *pentagon_link_broken:* "With the thread down, the remaining lattice relays are operating at capacity. The parasite's drain could cascade into a sector-wide navigation failure."

**NPC Contact:** $CONTACT_NAME (e.g., Threadkeeper Orath Senn). *"She is clever. Cleverness without understanding is vandalism."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to lattice relay near $TARGET_NODE (ArriveAtNode)
2. Confront the salvage pilot (Choice: demand she detach peacefully, or disable her ship)
3. Confirm relay power restored
4. Return to Weaver station (ArriveAtNode)

**Reward:** Medium credits + Weavers rep (+5). Peaceful resolution grants bonus Weavers rep (+3).

---

### BOUNTY_06 — Ghost Ship

**Briefing:** A vessel matching no known registry has been appearing on long-range scans near $TARGET_NODE for the past 30 ticks. It doesn't respond to hails. It doesn't dock. It doesn't trade. It just... orbits. Concord filed it as "anomalous contact" and forgot about it. Then a freighter captain reported that the ghost ship scanned her cargo bay with equipment that hasn't been manufactured in centuries. Someone built that ship a long time ago. Someone is flying it now. Find out who, and what they want.

**Variants:**
- *warfront_active:* "The ghost ship appeared after the last major engagement. Some pilots think it's a Valorin stealth prototype. Others think it's something older."
- *pentagon_link_broken:* "The ghost ship appeared near the collapsed thread. Communion elders say 'the shimmer remembers what was there.' That's not helpful, but it's unsettling."

**NPC Contact:** $CONTACT_NAME (e.g., Freighter Captain Dorin Waal). *"It scanned me for forty seconds. My equipment said the scan frequency was impossible. Then it left. I haven't slept since."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to $TARGET_NODE (ArriveAtNode)
2. Scan the ghost ship (Investigation)
3. Choice: Engage (combat) OR attempt communication (reputation check)
4. Report findings (ArriveAtNode at origin)

**Reward:** High credits + intel lead (IntelLeadNodeId). If communicated: Salvaged Tech bonus.

---

## 5. Investigation (5 templates, 8% weight)

### INVEST_01 — Thread Degradation Survey

**Briefing:** Concord's classified models predict that the thread connecting $ADJACENT_1 to $TARGET_NODE will fail within 100 ticks. They've been wrong before — but they've also been right, and when they're right, people die. A Concord intelligence analyst needs someone outside the official structure to independently verify the degradation readings. Official surveyors are too visible. You're not. Visit both endpoints and scan the thread metrics.

**Variants:**
- *warfront_active:* "The thread in question runs through contested space. If it fails during active combat operations, both sides lose their supply lines simultaneously. Concord wants to know if they should plan for that."
- *embargo_active:* "The analyst can't send official surveyors because the thread endpoints are in embargoed territory. She needs a civilian ship with good sensors and no Concord transponder."

**NPC Contact:** $CONTACT_NAME (e.g., Analyst Maren Sorel). *"My models say this thread is dying. My superiors say my models are 'pessimistic.' I'd like to know which of us is wrong before 200 people find out the hard way."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Arrive at $ADJACENT_1 and scan thread endpoint (ArriveAtNode)
2. Arrive at $TARGET_NODE and scan thread endpoint (ArriveAtNode)
3. Return scan data to analyst (ArriveAtNode at origin)

**Reward:** Medium credits + Concord rep (+5) + intel on future thread events

---

### INVEST_02 — Metric Anomaly

**Briefing:** A Communion elder reported that the shimmer patterns near $TARGET_NODE are "singing wrong." In Communion terms, this means the local spacetime metric is exhibiting fluctuations that shouldn't be possible this far from a fracture zone. Either the Communion elder's perceptions are drifting — which happens — or something is generating artificial metric instability. The elder asked you specifically because "your ship hums like ours now." She means the fracture module. Investigate.

**Variants:**
- *warfront_active:* "Weapons fire can generate localized metric disturbances — but the readings the elder described are too sustained for combat echoes. Something else is happening."
- *pentagon_link_broken:* "The thread failure near $TARGET_NODE is producing residual metric bleed. The question is whether the anomaly is residual, or whether something is actively preventing the metric from stabilizing."

**NPC Contact:** $CONTACT_NAME (e.g., Elder Shimmerwind). *"The space between spaces is thicker here. Something is pressing on it. I do not know what presses, but I know it is not natural."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to $TARGET_NODE (ArriveAtNode)
2. Perform deep scan of metric anomaly (scan/investigation)
3. Investigate source — may require visiting 1-2 additional nodes
4. Report to Elder (ArriveAtNode at Communion station)

**Reward:** Medium credits + Communion rep (+8) + fracture module calibration data (scanner precision improvement)

---

### INVEST_03 — Missing Freighter

**Briefing:** The freighter *Kestrel's Margin* left $ADJACENT_1 twelve ticks ago bound for $TARGET_NODE with 40 units of composites. She never arrived. Her transponder went dark six ticks into the voyage. Concord filed a search request that will activate in 20 ticks — standard processing time. The captain's wife is at $ADJACENT_1. She doesn't have 20 ticks of patience. She has credits and a description of the ship. Find the *Kestrel's Margin*.

**Variants:**
- *warfront_active:* "Three freighters have gone missing on that route this month. Two were confirmed pirate kills. The third — the *Kestrel's Margin* — might be different. Or might not."
- *pentagon_link_broken:* "The *Kestrel's Margin* may have been diverted by the thread collapse and ended up in uncharted space. The transponder could be blocked by metric interference rather than destruction."

**NPC Contact:** $CONTACT_NAME (e.g., Seyla Mott, captain's wife). *"He always comms me when he's an hour out. Always. For twelve years, always. Something is wrong."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Investigate last known position between $ADJACENT_1 and $TARGET_NODE (ArriveAtNode at intermediate)
2. Scan for transponder debris or signal
3. Locate freighter (ArriveAtNode at discovery location)
4. Report findings (ArriveAtNode at $ADJACENT_1)

**Reward:** Medium credits + intel. Outcome varies: freighter found intact (bonus rep), found destroyed (salvage reward), found captured (triggers rescue follow-up).

---

### INVEST_04 — Lattice Resonance Mapping

**Briefing:** The Weavers believe the lattice — the ancient navigational substrate that the thread network runs on — has a resonance frequency that shifts with local metric conditions. If they can map the resonance pattern across three nodes, they can predict thread failures before they happen. They've never been able to do this because their ships can't reach the measurement points fast enough — the resonance shifts every 15 ticks and the readings need to be near-simultaneous. Your fracture drive changes the math.

**Variants:**
- *warfront_active:* "One of the measurement points is in contested space. The Weavers note that the resonance data is 'faction-neutral' and hope this will protect you. It will not."
- *pentagon_link_broken:* "The thread failure is actively distorting the resonance pattern, which makes the data more valuable and the measurement more dangerous. The Weavers say 'more informative.' Same thing."

**NPC Contact:** $CONTACT_NAME (e.g., Resonance Theorist Kael Vohn). *"The lattice hums at a frequency that predates every species in this sector. I want to learn its song. Your ship can hear it."*

**Tokens:** $TARGET_NODE, $ADJACENT_1, $ADJACENT_2, $CONTACT_NAME

**Steps:**
1. Arrive at $TARGET_NODE and take resonance reading (ArriveAtNode)
2. Arrive at $ADJACENT_1 within 15 ticks and take reading (ArriveAtNode + TimerExpired)
3. Arrive at $ADJACENT_2 within 15 ticks and take reading (ArriveAtNode + TimerExpired)
4. Deliver data to Weaver station (ArriveAtNode)

**Reward:** High credits + Weavers rep (+10) + navigation data (thread failure early warning)

---

### INVEST_05 — Signal Source

**Briefing:** Chitin listening posts intercepted a repeating signal near $TARGET_NODE that matches no known communication protocol. It's not encrypted — it's structured in a way that assumes the listener can perceive metric-space relationships that no current species can. Except, possibly, a pilot with prolonged fracture module exposure. The Chitin want the signal decoded. They'll pay for raw data even if you can't read it. They'll pay more if you can.

**Variants:**
- *warfront_active:* "Both sides in the warfront are convinced the signal is the other side's secret communication channel. It's neither. But explaining that requires getting close enough to prove it."
- *pentagon_link_broken:* "The signal appeared after the thread collapse. It may be the lattice itself — the ancient infrastructure trying to communicate its damage state. Or it may be something that was hidden by the thread's containment field."

**NPC Contact:** $CONTACT_NAME (e.g., Signal Analyst Kiris Taal). *"We ran it through every decryption model we have. The probability that it's random noise is 0.003%. The probability that we can read it is also 0.003%. We need someone who hears differently."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to signal source near $TARGET_NODE (ArriveAtNode)
2. Record signal data (scan)
3. Attempt fracture-enhanced decode (tech check — TechUnlocked for fracture module)
4. Deliver data to Chitin station (ArriveAtNode)

**Reward:** High credits + Chitin rep (+8) + potential discovery lead

---

## 6. Salvage (4 templates, 5% weight)

### SALVAGE_01 — Derelict Recovery

**Briefing:** A pre-war Concord patrol cruiser has been spotted drifting near $TARGET_NODE. She's been dead for at least 200 ticks — power signature cold, hull breached in three places, crew status unknown but presumed final. The hull is worthless. The cargo bay might not be. Pre-war cruisers carried strategic reserves: electronics, components, sometimes rare metals. Concord is offering a standard salvage license: 60% of recovered goods to you, 40% to Concord for "heritage preservation." Get there before the Chitin scavenger fleets do.

**Variants:**
- *warfront_active:* "The cruiser drifted into contested space during the latest warfront shift. Both sides claim salvage rights. Neither side is there yet. You are."
- *embargo_active:* "Concord heritage preservation has been suspended during the embargo. The salvage license is unofficial. If customs stops you, the cargo is 'pre-existing inventory of unknown origin.'"

**NPC Contact:** $CONTACT_NAME (e.g., Salvage Registrar Tomis Kree). *"That ship served thirty years. She deserves to be remembered. But first, she deserves to be useful."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to derelict near $TARGET_NODE (ArriveAtNode)
2. Scan and assess salvage (scan)
3. Extract cargo — 10-20 mixed goods loaded
4. Return to Concord station (ArriveAtNode)
5. Deliver 40% share (NoCargoAtNode partial)

**Reward:** Medium credits (from 60% retained goods) + Concord rep (+3)

---

### SALVAGE_02 — Battlefield Scrap

**Briefing:** The skirmish near $TARGET_NODE ended six ticks ago. The combatants have moved on. The debris hasn't. Twelve destroyed or disabled vessels are leaking atmosphere, fuel, and salvageable components into the void. In another 20 ticks the scrap will disperse beyond recovery range. A Chitin Syndicate recycler is offering premium rates for battlefield salvage — no questions about whose flag was on the hull. Recover what you can.

**Variants:**
- *warfront_active:* "The battlefield is still technically in the combat exclusion zone. Military patrols may challenge your presence. The Chitin suggest 'environmental remediation contractor' as a cover story."
- *embargo_active:* "Salvage from warships may contain embargoed components. The Chitin don't care. Concord customs might."

**NPC Contact:** $CONTACT_NAME (e.g., Recycler Boss Zenn Ottar). *"Dead ships don't need their parts. Living stations do. I facilitate the transition."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to battlefield near $TARGET_NODE (ArriveAtNode)
2. Scan debris field (scan)
3. Recover salvage — 15-25 mixed goods (metal, components, salvaged_tech)
4. Deliver to Chitin station (ArriveAtNode)

**Reward:** Medium credits + Chitin rep (+3)

---

### SALVAGE_03 — Ancient Fragment Recovery

**Briefing:** A fracture event near $TARGET_NODE exposed something that should not exist this close to inhabited space: an accommodation-geometry structure fragment, partially embedded in an asteroid. The fragment is small — maybe two meters across — but the energy readings are consistent with the same technology as your fracture module. Communion scholars want it recovered intact. The Weavers want it studied in place. You're the only one with a ship that can safely approach accommodation-geometry artifacts without sensor distortion.

**Variants:**
- *warfront_active:* "Both warfront factions have noticed the energy readings. A recovery team means a ship lingering near a high-value anomaly in contested space. Work fast."
- *pentagon_link_broken:* "The fracture event that exposed the fragment is the same one that collapsed the thread. The Communion believe this is not a coincidence. The Weavers believe the Communion is being mystical. Both want the fragment."

**NPC Contact:** $CONTACT_NAME (e.g., Scholar Vael Thistledown). *"The fragment remembers what it was part of. If we listen carefully, it might tell us."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to fracture site near $TARGET_NODE (ArriveAtNode)
2. Scan fragment (scan — TechUnlocked fracture module)
3. Extract fragment carefully (timed sequence)
4. Deliver to Communion or Weaver station (ArriveAtNode — Choice)

**Reward:** High credits + faction rep (+10 to chosen faction, -3 to other) + exotic matter

---

### SALVAGE_04 — Emergency Pod Retrieval

**Briefing:** A Weaver transport broke apart during a lattice resonance event near $TARGET_NODE. The crew made it into emergency pods, but the pods are drifting toward unstable space where metric fluctuations will scramble their life support in 25 ticks. The Weavers have no combat or rescue vessels nearby — their fleet is structural, not fast. Retrieve the three emergency pods before the metric boundary swallows them.

**Variants:**
- *warfront_active:* "The pods are drifting toward the combat zone. Weapons fire accelerates metric instability. Every tick the warfront continues, the safe recovery window shrinks."
- *pentagon_link_broken:* "The pods are drifting toward the collapsed thread. What used to be a navigational corridor is now a metric wound. Nothing that enters comes out intact."

**NPC Contact:** $CONTACT_NAME (e.g., Threadkeeper Sarrin Osse). *"Three of our engineers are in those pods. They built the infrastructure you fly through. Please bring them home."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to pod drift zone near $TARGET_NODE (ArriveAtNode)
2. Retrieve pod 1 (scan + approach)
3. Retrieve pod 2 (scan + approach, timer pressure)
4. Retrieve pod 3 (scan + approach, timer pressure)
5. Deliver survivors to Weaver station (ArriveAtNode)

**Reward:** Medium credits + Weavers rep (+12)

---

## 7. Diplomacy (4 templates, 5% weight)

### DIPLO_01 — Ceasefire Courier

**Briefing:** The Valorin-Weaver border skirmishes have killed 43 people in the last 30 ticks. A Concord mediator has drafted ceasefire terms that neither side hates — which is the best you get in pentagon politics. The terms exist on a physical data crystal because neither the Valorin nor the Weavers trust digital transmission through Concord-monitored channels. Someone needs to physically carry the crystal from the Concord mediation office at $PLAYER_START to the Valorin command at $ADJACENT_1, then to the Weaver council at $TARGET_NODE, and return with both responses.

**Variants:**
- *warfront_active:* "The ceasefire terms were drafted during active combat. Both sides are still shooting while negotiating. Carry the crystal through the crossfire."
- *embargo_active:* "The embargo complicates the terms — Concord is asking the Valorin to stop shooting while simultaneously restricting their trade. The Valorin response may be... emphatic."

**NPC Contact:** $CONTACT_NAME (e.g., Mediator Solas Brin). *"Forty-three dead and counting. This crystal contains words that might stop the counting. Don't drop it."*

**Tokens:** $PLAYER_START, $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Receive data crystal at $PLAYER_START (ArriveAtNode)
2. Deliver to Valorin command at $ADJACENT_1 (ArriveAtNode)
3. Wait for response (TimerExpired — 5 ticks)
4. Deliver to Weaver council at $TARGET_NODE (ArriveAtNode)
5. Wait for response (TimerExpired — 5 ticks)
6. Return responses to $PLAYER_START (ArriveAtNode)

**Reward:** High credits + Concord rep (+5) + Valorin rep (+3) + Weavers rep (+3)

---

### DIPLO_02 — Trade Negotiation Proxy

**Briefing:** Chitin Syndicate traders and Communion drifters don't negotiate well face-to-face. The Chitin talk in probabilities and spreads. The Communion talk in shimmer-songs and feelings. Last time they tried direct negotiation, a Chitin broker told a Communion elder that her "feelings about the crystal price" had a "negative expected value" and the elder responded by humming at him for forty minutes. Both sides have agreed to use a proxy — someone who speaks both languages. You've traded with both. They trust you. Broker a crystal supply agreement.

**Variants:**
- *warfront_active:* "The crystal supply is critical for warfront sensor arrays. Whoever controls the deal controls military capability. Both sides know this. Neither will say it out loud."
- *pentagon_link_broken:* "The thread failure disrupted the existing crystal supply chain. This negotiation isn't about getting a better deal — it's about having any deal at all."

**NPC Contact:** $CONTACT_NAME (Chitin: Broker Vesk Tarim; Communion: Elder Brightveil). Vesk: *"Quantify her requirements. I can price anything she can define."* Brightveil: *"He speaks in numbers. Numbers are the skeleton of meaning. I need the flesh."*

**Tokens:** $PLAYER_START, $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Meet Chitin broker at $ADJACENT_1 (ArriveAtNode)
2. Receive terms
3. Deliver to Communion elder at $TARGET_NODE (ArriveAtNode)
4. Negotiate response (Choice: favor Chitin pricing, favor Communion terms, or balanced)
5. Return agreement to $ADJACENT_1 (ArriveAtNode)

**Reward:** Medium credits + Chitin rep (+5) + Communion rep (+5). Choice shifts rep balance.

---

### DIPLO_03 — Faction Introduction

**Briefing:** You've built enough reputation with $FACTION_1 that their diplomatic office wants to use you as an introduction vector for a new trade relationship with $FACTION_2. The factions aren't hostile — they're simply unfamiliar. Pentagon ring politics mean they've never needed to trade directly; goods always flowed through intermediaries. But intermediary margins are eating both sides' profits, and a direct channel would benefit everyone except the middlemen. Carry credentials from $FACTION_1 to $FACTION_2 and facilitate initial terms.

**Variants:**
- *warfront_active:* "The warfront disrupted the intermediary chain, which is the only reason $FACTION_1 is willing to try direct trade. Necessity as diplomacy."
- *embargo_active:* "The embargo closed the intermediary route. Direct trade isn't a preference — it's the only option left."

**NPC Contact:** $CONTACT_NAME (e.g., Ambassador Tessik Ohn). *"We are not enemies with $FACTION_2. We are strangers. Strangers with complementary cargo manifests. Help us become acquaintances."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $FACTION_1, $FACTION_2, $CONTACT_NAME

**Steps:**
1. Receive diplomatic credentials at $PLAYER_START (ArriveAtNode)
2. Deliver to $FACTION_2 station at $TARGET_NODE (ArriveAtNode)
3. Facilitate response (Choice: recommend trade terms)
4. Return to $FACTION_1 station (ArriveAtNode)

**Reward:** Medium credits + $FACTION_1 rep (+5) + $FACTION_2 rep (+5)

---

### DIPLO_04 — Apology Delegation

**Briefing:** A Valorin patrol destroyed a Communion medical ship 60 ticks ago. The Valorin clan responsible has been internally punished — the patrol commander was stripped of rank — but no one told the Communion. The Communion elders don't want blood. They want acknowledgment. The Valorin don't apologize — culturally, they "restate the obligation." A Valorin delegation is willing to travel to the Communion gathering at $TARGET_NODE and restate their obligation to not fire on medical vessels. They need escort and, more importantly, a translator. Not of language. Of intent.

**Variants:**
- *warfront_active:* "The destroyed medical ship was carrying warfront casualties. The Communion saved Valorin wounded before the missile hit. The apology is complicated by the fact that the Communion was helping Valorin soldiers when Valorin weapons killed the Communion crew."
- *pentagon_link_broken:* "The delegation must travel through unstable space to reach the Communion. The Valorin see this as fitting — 'an obligation restated must cost something.' The Communion would prefer everyone arrive alive."

**NPC Contact:** $CONTACT_NAME (Valorin: Former-Commander Thessa Kree; Communion: Elder Quietwater). Thessa: *"I will say what must be said. I will not say I am sorry. I will say it will not happen again. That is more honest."* Quietwater: *"We do not need her sorrow. We need her promise."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $FACTION_1 (Valorin), $FACTION_2 (Communion), $CONTACT_NAME

**Steps:**
1. Pick up Valorin delegation at $PLAYER_START (ArriveAtNode)
2. Escort to Communion gathering at $TARGET_NODE (ArriveAtNode, possible encounters)
3. Facilitate meeting (Choice: encourage Valorin directness, coach diplomatic softening, or stay neutral)
4. Return delegation (ArriveAtNode at origin)

**Reward:** Low credits + Valorin rep (+3) + Communion rep (+8)

---

## 8. Construction (3 templates, 3% weight)

### CONSTRUCT_01 — Station Expansion

**Briefing:** Station $TARGET_NODE has been at capacity for 50 ticks. The docking queue averages 8 ticks wait. The station administrator approved an expansion module — an additional docking ring and cargo processing bay — but the approval was the easy part. The hard part is getting 25 metal, 15 composites, and 10 components to a station that can barely handle its current traffic, let alone construction material deliveries. Multiple trips or one very full cargo bay.

**Variants:**
- *warfront_active:* "The expansion is military-funded — the docking ring will service warfront patrol vessels. Military funding means military deadlines. The administrator is sweating."
- *embargo_active:* "Composites are embargoed. The administrator has approval for the expansion but not for the materials. She's hoping someone shows up with composites and doesn't mention where they came from."

**NPC Contact:** $CONTACT_NAME (e.g., Administrator Voss Brennan). *"I approved the expansion six cycles ago. I've been apologizing to every captain in the docking queue since. Help me stop apologizing."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (metal), $MARKET_GOOD_2 (composites), $MARKET_GOOD_3 (components), $CONTACT_NAME

**Steps:**
1. Acquire 25 metal (HaveCargoMin)
2. Acquire 15 composites (HaveCargoMin)
3. Acquire 10 components (HaveCargoMin)
4. Deliver all to $TARGET_NODE (ArriveAtNode + NoCargoAtNode x3)

**Reward:** High credits + faction rep (+8) + permanent docking priority at $TARGET_NODE

---

### CONSTRUCT_02 — Defense Platform

**Briefing:** The lane junction at $TARGET_NODE has been hit by pirates four times in 30 ticks. The local faction council voted to build a defense platform — automated turrets and a sensor grid — but the vote didn't include a construction crew or materials. Valorin engineering doctrine says "the clan that needs the wall builds the wall." You're not Valorin, but your cargo bay is. Deliver 20 metal, 15 munitions, and 10 electronics for the platform systems.

**Variants:**
- *warfront_active:* "The defense platform will also serve as a forward observation post. Military command is 'suggesting' additional munitions for the weapons array. The suggestion comes with a budget increase."
- *embargo_active:* "Munitions delivery to frontier stations is restricted under the embargo. The pirates are not restricted under the embargo. The irony is not lost on the station crew."

**NPC Contact:** $CONTACT_NAME (e.g., Clan-Engineer Borrik Staal). *"I can build a platform that shrugs off pirate railguns. First, I need the railguns. And the platform. And the metal to make the platform."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (metal), $MARKET_GOOD_2 (munitions), $MARKET_GOOD_3 (electronics), $CONTACT_NAME

**Steps:**
1. Acquire 20 metal (HaveCargoMin)
2. Acquire 15 munitions (HaveCargoMin)
3. Acquire 10 electronics (HaveCargoMin)
4. Deliver all to $TARGET_NODE (ArriveAtNode + NoCargoAtNode x3)

**Reward:** High credits + Valorin rep (+10)

---

### CONSTRUCT_03 — Haven Module

**Briefing:** Your Haven starbase is functional but incomplete. The engineering bay needs a structural upgrade to support the next tier of research equipment. The materials list reads like a pentagon shopping trip: Weaver composites for the framework, Chitin electronics for the control systems, Valorin rare metals for the shielding, Communion exotic crystals for the resonance couplings. Four factions, four goods, four stops. The only thing they all agree on is that your credits are acceptable.

**Variants:**
- *warfront_active:* "Two of the four suppliers are on opposite sides of the warfront. Buying from both means crossing the combat zone twice. Your Haven doesn't care about politics. Your hull does."
- *pentagon_link_broken:* "The thread failure cuts off one supplier. You'll need to find an alternative source or take the fracture route. Either way, the upgrade waits for nobody — the research window is 40 ticks."

**NPC Contact:** Internal — your First Officer. *"Four stops, four factions, one shopping list. I've plotted three routes. The fastest crosses contested space. The safest adds 20 ticks. The interesting one uses the fracture drive."*

**Tokens:** $TARGET_NODE (Haven), $ADJACENT_1, $ADJACENT_2, $CONTACT_NAME (First Officer)

**Steps:**
1. Acquire 10 composites from Weaver station (HaveCargoMin)
2. Acquire 8 electronics from Chitin station (HaveCargoMin)
3. Acquire 5 rare_metals from Valorin station (HaveCargoMin)
4. Acquire 5 exotic_crystals from Communion station (HaveCargoMin)
5. Return to Haven and deliver (ArriveAtNode + NoCargoAtNode x4)

**Reward:** Haven upgrade completion (structural) + research capability unlock

---

## 9. Contraband (3 templates, 2% weight)

### CONTRA_01 — Embargo Runner

**Briefing:** Concord classified $MARKET_GOOD_1 as restricted goods in $FACTION_1 space four ticks ago. The official reason is "supply chain security." The unofficial reason is political pressure. A $FACTION_1 station that depends on $MARKET_GOOD_1 for daily operations is offering triple market rate for a quiet delivery. The route passes through two Concord checkpoint lanes. Your transponder will be scanned. Your cargo bay will be scanned. Your conscience is your own problem.

**Variants:**
- *warfront_active:* "Concord checkpoints are reinforced with military scanners during wartime. Getting through clean requires either a sensor-dampening module or a very convincing cargo manifest."
- *pentagon_link_broken:* "The thread failure opened an unmonitored route that bypasses both checkpoints. The route is longer and passes through pirate territory. Choose your risk."

**NPC Contact:** $CONTACT_NAME (e.g., Dock Foreman Krell Asvin). *"I don't care what Concord calls it. My people call it 'the thing that keeps the lights on.' Bring it."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1, $FACTION_1, $CONTACT_NAME

**Steps:**
1. Acquire 15 $MARKET_GOOD_1 (HaveCargoMin)
2. Navigate past checkpoint lanes (ArriveAtNode at intermediate — risk of scan)
3. Deliver to $TARGET_NODE (ArriveAtNode + NoCargoAtNode)

**Reward:** High credits + $FACTION_1 rep (+5), Concord rep (-5) if detected

---

### CONTRA_02 — Black Market Tech

**Briefing:** Salvaged technology from ancient derelicts is classified as "heritage material" under Concord law — meaning it legally belongs to Concord's engineering division for study. In practice, this means every piece of salvaged_tech you recover in the field should be surrendered for a 15% finder's fee. A Chitin research syndicate is offering 200% market rate, no questions, no paperwork, no Concord. They want to reverse-engineer the accommodation geometry. Concord wants to control who reverse-engineers it. You have 12 units of salvaged_tech and a decision to make.

**Variants:**
- *warfront_active:* "Concord's engineering division is prioritizing military applications of salvaged tech. The Chitin argue that civilian research shouldn't be gated by military priorities. Concord argues that everything is military during wartime."
- *pentagon_link_broken:* "The thread failure exposed more ancient structures. Salvaged tech supply just increased, which means Concord heritage patrols just increased. Timing is terrible. Or perfect, depending on your cargo bay."

**NPC Contact:** $CONTACT_NAME (e.g., Research Director Axiis Ren). *"Concord wants to study it in a vault for a century. We want to study it now. Progress has a price. We're offering to pay it."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1 (salvaged_tech), $CONTACT_NAME

**Steps:**
1. Have or acquire 12 salvaged_tech (HaveCargoMin)
2. Navigate to Chitin research station at $TARGET_NODE (ArriveAtNode)
3. Deliver tech (NoCargoAtNode)

**Reward:** Very high credits + Chitin rep (+8), Concord rep (-8)

---

### CONTRA_03 — Tariff Evasion

**Briefing:** Weaver station tariffs are calculated on cargo mass at point of entry. A Chitin mathematician discovered that if you dock at the secondary bay — which uses an older weighing system calibrated for light cargo — anything under 50 units registers at 60% actual mass. The Weavers know about the discrepancy. They haven't fixed it because fixing it costs more than the lost tariff revenue. A Chitin merchant wants to exploit the window before the Weavers do the math and decide to fix it. Carry her cargo of 40 $MARKET_GOOD_1 through the secondary bay.

**Variants:**
- *warfront_active:* "The Weavers just increased tariffs to fund warfront infrastructure. The discount from the secondary bay exploit is now even more profitable. And even more likely to be noticed."
- *embargo_active:* "The secondary bay isn't subject to embargo inspections — it's classified as 'maintenance access.' Whether this is an oversight or intentional is a question nobody is asking loudly."

**NPC Contact:** $CONTACT_NAME (e.g., Merchant Talli Vex). *"It's not illegal. It's not even unethical. It's just... arithmetic that the Weavers chose not to do. I'm helping them by demonstrating the problem."*

**Tokens:** $PLAYER_START, $TARGET_NODE, $MARKET_GOOD_1, $CONTACT_NAME

**Steps:**
1. Acquire 40 $MARKET_GOOD_1 (HaveCargoMin)
2. Dock at $TARGET_NODE secondary bay (ArriveAtNode — specific dock)
3. Complete trade at reduced tariff (NoCargoAtNode)

**Reward:** Medium credits (tariff savings passed to player) + Chitin rep (+3), Weavers rep (-2)

---

## 10. Survey (3 templates, 2% weight)

### SURVEY_01 — Frontier Mapping

**Briefing:** Beyond the last Valorin outpost at $ADJACENT_1, there are three star systems that appear on ancient charts but not on any current navigation database. The Valorin don't care — if it's not worth fighting over, it's not worth mapping. Concord cartography wants those systems surveyed and added to the official registry. Your fracture drive can reach them. Your sensors can map them. Your report will determine whether anyone else ever goes there.

**Variants:**
- *warfront_active:* "Military intelligence wants the survey data before cartography publishes it. Unmapped systems near the warfront are either strategic assets or strategic liabilities. The military wants to decide which before the data becomes public."
- *pentagon_link_broken:* "The thread failure may have shifted access to unmapped systems. The survey will verify whether the ancient charts still correspond to reachable space."

**NPC Contact:** $CONTACT_NAME (e.g., Chief Cartographer Doss Merrin). *"Three systems no one has visited in a thousand years. Either they're empty or they're full of things that preferred not being visited. Either way, I want a map."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $ADJACENT_2, $CONTACT_NAME

**Steps:**
1. Travel to $TARGET_NODE — unmapped system 1 (ArriveAtNode)
2. Perform full survey scan
3. Travel to system 2 (ArriveAtNode)
4. Perform full survey scan
5. Travel to system 3 (ArriveAtNode)
6. Perform full survey scan
7. Return data to Concord station (ArriveAtNode)

**Reward:** High credits + Concord rep (+8) + discovery data for 3 systems

---

### SURVEY_02 — Shimmer-Zone Census

**Briefing:** The Communion tracks "shimmer density" — their term for the concentration of metric anomalies in a region of space. Elder Quietwater says the shimmer density near $TARGET_NODE has changed in ways that suggest "new listeners." In Communion cosmology, this means undiscovered life — or undiscovered technology — interacting with the local metric field. The Communion doesn't have survey equipment. They have intuition and a thousand years of pattern recognition. They want you to bring the equipment they lack to the place their intuition found.

**Variants:**
- *warfront_active:* "The shimmer zone is near the warfront. Military sensors may have contaminated the readings. Or the military activity may have attracted whatever the Communion is sensing."
- *pentagon_link_broken:* "The thread collapse may have exposed the shimmer zone. What was previously hidden by the thread's containment field is now... present. The Communion elders are excited. That makes everyone else nervous."

**NPC Contact:** $CONTACT_NAME (e.g., Elder Quietwater). *"Something new is singing in the shimmer. We cannot see it with our eyes. You have better eyes. Please look."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to shimmer zone near $TARGET_NODE (ArriveAtNode)
2. Deploy survey sensors
3. Perform 3-point triangulation scan (visit 3 positions within zone)
4. Analyze results
5. Return findings to Communion station (ArriveAtNode)

**Reward:** Medium credits + Communion rep (+10) + potential discovery lead + exotic matter

---

### SURVEY_03 — Resource Prospecting

**Briefing:** The Chitin Syndicates run probability models on everything, including where undiscovered resource deposits might exist. Their latest model predicts a rare metals vein in the asteroids near $TARGET_NODE with 67% confidence. They want ground truth — actual sensor readings to validate the model. If the model is right, they'll stake a mining claim. If it's wrong, they'll refine the model. Either way, they pay for data. The Chitin never lose. They just update their priors.

**Variants:**
- *warfront_active:* "The predicted deposit is in contested space. If it exists, it becomes a strategic resource that both warfront parties will fight over. The Chitin want the data before anyone else knows to fight."
- *pentagon_link_broken:* "The thread failure shifted known resource maps. The Chitin model accounts for this — the predicted deposit might have been exposed by the metric shift. Or it might have been buried deeper."

**NPC Contact:** $CONTACT_NAME (e.g., Model Analyst Ferrik Zhaal). *"67% confidence means 33% chance I'm wrong. I need to know which third I'm in. Go scan the rocks."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to asteroid field near $TARGET_NODE (ArriveAtNode)
2. Perform geological scan at 3 survey points
3. Compile results
4. Return data to Chitin station (ArriveAtNode)

**Reward:** Medium credits + Chitin rep (+5) + potential mining location intel

---

## 11. Discovery (3 templates, 3% weight)

### DISC_01 — Anomaly Survey

**Briefing:** A Communion wayfinder station near $ADJACENT_1 detected unusual metric readings — patterns that don't match any known stellar phenomenon or thread activity. The readings are intermittent, which means they're either an instrument artifact or something that appears and disappears. The station's lead researcher wants a ship with modern scanning equipment to visit the source coordinates near $TARGET_NODE and perform a calibrated three-point survey. No crisis. No deadline. Just the kind of question that keeps scientists awake at night.

**Variants:**
- *warfront_active:* "Military sensor arrays in the area are generating interference. The anomaly readings might be real or might be echoes of electronic warfare. The researcher needs clean data from civilian instruments — yours."
- *high_instability:* "Metric instability is increasing in the region. The anomaly might be a symptom. Or it might be the cause. Either answer would be useful."

**NPC Contact:** $CONTACT_NAME (e.g., Researcher Pela Shenn). *"I've been staring at these readings for eighteen cycles. Either this is the most interesting thing I've ever found or I need to recalibrate the entire array. Please help me find out which."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to anomaly coordinates near $TARGET_NODE (ArriveAtNode)
2. Perform scan at point Alpha
3. Reposition and scan at point Beta
4. Reposition and scan at point Gamma
5. Return data to researcher at $ADJACENT_1 (ArriveAtNode)

**Reward:** Medium credits + Communion rep (+5) + discovery lead (potential anomaly chain unlock)

---

### DISC_02 — Deep Retrieval

**Briefing:** A Chitin probability model identified a void-adjacent debris field near $TARGET_NODE as containing a specific type of data core — pre-collapse computational hardware with intact storage. These cores surface in derelict surveys occasionally, but the model predicts this particular field has one with navigation data that could chart three unmapped fracture routes. The Chitin don't want the routes — they want the prediction validated. You get the routes. Everyone's happy. Assuming the debris field doesn't contain anything that objects to you poking around.

**Variants:**
- *warfront_active:* "The debris field is in contested space. Military salvage teams have been through once already and left — they were looking for weapons components, not data cores. What they missed is exactly what you're looking for."
- *pentagon_link_broken:* "The thread failure may have shifted the debris field. The Chitin model accounts for a 12% positional error. If the field moved more than that, you'll need to search wider."

**NPC Contact:** $CONTACT_NAME (e.g., Model Analyst Quorra Tesk). *"The model says the core is there. The model has an 81% confidence interval. I'd like you to turn that 81 into a 100 or a 0. Uncertainty is expensive."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to void-adjacent debris field at $TARGET_NODE (ArriveAtNode)
2. Scan debris field for data core signature
3. Extract data core (possible hazard encounter)
4. Return to Chitin station (ArriveAtNode)

**Reward:** Medium credits + Chitin rep (+5) + fracture route data (3 new map edges)

---

### DISC_03 — Route Pathfinding

**Briefing:** The Weavers have been monitoring lattice tension patterns near $TARGET_NODE and identified a stable corridor through space that's currently unmapped — a natural path that follows tension minima, like water finding the lowest channel. If verified, this route would cut transit time between $ADJACENT_1 and $TARGET_NODE by 40%. But tension patterns shift, and a corridor that's stable today might not be stable tomorrow. Architect Tessyn needs someone to fly the route while her instruments track whether the tension holds under an actual ship's metric shadow.

**Variants:**
- *warfront_active:* "Military patrols use the existing routes. A shortcut unknown to the fleet would be strategically valuable. The Weavers aren't offering it to the military — they're offering it to everyone. The military may have opinions about that."
- *high_instability:* "Instability events are making the tension readings unreliable. The corridor might be stable or it might collapse while you're inside it. Architect Tessyn calls this 'empirical verification with personal stakes.'"

**NPC Contact:** $CONTACT_NAME (e.g., Architect Tessyn Lohm). *"I can read the tension from here. But reading and walking are different verbs. I need someone to walk it while I read. The corridor won't bite. Probably."*

**Tokens:** $ADJACENT_1, $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Receive route coordinates from Weaver station at $ADJACENT_1 (ArriveAtNode)
2. Enter corridor at waypoint Alpha (ArriveAtNode)
3. Navigate through 3 intermediate waypoints (sequential ArriveAtNode)
4. Exit corridor at $TARGET_NODE (ArriveAtNode)
5. Return confirmation data to $ADJACENT_1 (ArriveAtNode)

**Reward:** Medium credits + Weavers rep (+8) + permanent shortcut route unlock

---

## 12. Celebration (2 templates, 2% weight)

### CELEB_01 — Station Anniversary

**Briefing:** Waystation $TARGET_NODE is celebrating its centennial — one hundred cycles of continuous habitation, which in pentagon space means one hundred cycles of not being destroyed, abandoned, or condemned. The station administrator wants to mark the occasion properly, which means a feast, which means specific goods that aren't available locally. She needs 10 food (Communion), 5 exotic crystals (ceremonial), and 5 electronics (for the lighting display). The budget is generous because the alternative is "another hundred years of institutional embarrassment."

**Variants:**
- *warfront_active:* "The celebration is going ahead despite the warfront. 'Especially because of the warfront,' the administrator says. 'People need to remember what we're fighting for.' Security is tight. Morale is the mission."
- *embargo_active:* "The exotic crystals are embargoed. The administrator suggests listing them as 'decorative minerals' on the manifest. She has done this before."

**NPC Contact:** $CONTACT_NAME (e.g., Station Administrator Kora Vellis). *"One hundred years. My grandmother was born here. Her grandmother docked here when it was just a fuel stop. I'd like the party to be worth the wait."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (food), $MARKET_GOOD_2 (exotic crystals), $MARKET_GOOD_3 (electronics), $CONTACT_NAME

**Steps:**
1. Acquire 10 food (HaveCargoMin)
2. Acquire 5 exotic crystals (HaveCargoMin)
3. Acquire 5 electronics (HaveCargoMin)
4. Deliver all to $TARGET_NODE (ArriveAtNode + NoCargoAtNode x3)

**Reward:** Medium credits + faction rep (+5) + station loyalty bonus (future price discount at $TARGET_NODE)

---

### CELEB_02 — Warfront Respite

**Briefing:** The warfront in the $FACTION_1-$FACTION_2 corridor just went quiet. Not a ceasefire — the fighting simply stopped for reasons neither side is willing to explain. A station commander at $TARGET_NODE is seizing the window. She's organizing what she calls a "maintenance celebration" — officially a systems check and resupply, unofficially the first time in 60 ticks that her crew can breathe without checking the threat board. She needs 15 composites (hull patches — the station took damage), 10 food (the good kind, not emergency rations), and one trader willing to dock at a station that might become a target again at any moment.

**Variants:**
- *warfront_active:* "This IS the warfront variant. The celebration happens in the eye of the storm. The fighting could resume mid-toast."
- *pentagon_link_broken:* "The thread failure is why the fighting stopped — both sides lost supply access. The celebration is happening because the war can't. Yet."

**NPC Contact:** $CONTACT_NAME (e.g., Commander Jessa Ralk). *"Sixty ticks of combat readiness. My people haven't slept properly in two months. This isn't a party — it's a medical necessity with better food."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (composites), $MARKET_GOOD_2 (food), $FACTION_1, $FACTION_2, $CONTACT_NAME

**Steps:**
1. Acquire 15 composites (HaveCargoMin)
2. Acquire 10 food (HaveCargoMin)
3. Deliver to $TARGET_NODE (ArriveAtNode + NoCargoAtNode x2)

**Reward:** Medium credits + $FACTION_1 rep (+3) + $FACTION_2 rep (+3) + crew morale narrative event

---

## 13. Ceremony (3 templates, 2% weight)

### CEREMONY_01 — Blood-Kin Witness

**Briefing:** Two Valorin clans are formalizing a Blood-Kin pact at $TARGET_NODE — the most binding alliance in Valorin culture. The ceremony requires an outsider witness, someone with no clan obligation who can verify the pact terms were stated clearly and accepted freely. You've traded with both clans. Your reputation is clean. Clan Elder Torvass has invited you to serve as Voice-External — the formal witness role. The ceremony takes 10 ticks. Your job is to be present, listen, and afterward sign the record crystal.

**Variants:**
- *warfront_active:* "The Blood-Kin pact is a military consolidation — two clans pooling warships. The ceremony is tradition. The outcome is strategic. Your witness signature will appear on a document that redistributes military power."
- *embargo_active:* "One clan is in embargoed territory. Attending the ceremony means docking at a station Concord considers off-limits. The Valorin consider Concord's opinion about Valorin ceremonies 'irrelevant.'"

**NPC Contact:** $CONTACT_NAME (e.g., Clan Elder Torvass Khenn). *"We do not need your approval. We need your eyes. Stand, watch, and tell the truth about what you saw. That is all the ceremony asks."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to ceremony site at $TARGET_NODE (ArriveAtNode)
2. Attend ceremony (TimerExpired — 10 ticks, no combat/trade during)
3. Sign record crystal
4. Return to any station (ArriveAtNode)

**Reward:** Low credits + Valorin rep (+10) + "Blood-Kin Witness" title (permanent reputation modifier)

---

### CEREMONY_02 — Bridge Dedication

**Briefing:** The Weavers completed a lattice bridge connecting two previously isolated sectors — a 400-tick construction project that reroutes metric tension through a new stabilization point. Architect Sareth-Ul is hosting a dedication ceremony at $TARGET_NODE, and she wants traders who use the lanes to attend. "The people who walk the bridges should see one born," she says. The ceremony involves a structural resonance test where the bridge is stressed to 115% rated capacity while everyone watches. The Weavers find this beautiful. Everyone else finds it terrifying.

**Variants:**
- *warfront_active:* "The new bridge has strategic value. Military representatives from both warfront parties have been invited. The Weavers don't build for war, but what they build gets used for war. Sareth-Ul is aware of the irony."
- *pentagon_link_broken:* "The bridge was built to replace a collapsed thread. The dedication ceremony is equal parts celebration and memorial — for the traffic lost during the collapse."

**NPC Contact:** $CONTACT_NAME (e.g., Architect Sareth-Ul). *"A bridge is a conversation between two places. Today we introduce them. If the resonance holds — and it will hold — the conversation becomes permanent."*

**Tokens:** $TARGET_NODE, $CONTACT_NAME

**Steps:**
1. Travel to dedication site at $TARGET_NODE (ArriveAtNode)
2. Attend resonance test (TimerExpired — 8 ticks)
3. Witness structural confirmation
4. Optional: deliver 5 composites as dedication gift (HaveCargoMin — bonus reward)

**Reward:** Low credits + Weavers rep (+8) + new bridge route enabled on map. Bonus: +3 Weavers rep for dedication gift.

---

### CEREMONY_03 — Mediation Feast

**Briefing:** A trade dispute between a Chitin syndicate and a Concord regulatory office has been resolved — mostly. Both sides saved face, both sides lost money, and both sides are pretending the outcome was their idea. Concord protocol requires a "Resolution Acknowledgment" — which is bureaucratic for "dinner." The Chitin call it a "Post-Arbitrage Social Function" — which is financial for "dinner." Someone needs to supply the dinner. 15 food, varied enough to satisfy both Concord's institutional catering standards and the Chitin's probabilistic dietary preferences. The contact promises that "the speeches will be brief." She is lying.

**Variants:**
- *warfront_active:* "The dispute resolution is being fast-tracked because both sides need to cooperate on warfront supply chains. The feast is both celebration and strategic necessity."
- *embargo_active:* "The dispute WAS about the embargo. The feast celebrates the end of the argument, not the end of the embargo. The embargo continues. The appetizers are excellent."

**NPC Contact:** $CONTACT_NAME (e.g., Concord Protocol Officer Halla Dessin). *"I need food that says 'we are civilized colleagues who resolved this matter professionally.' Not food that says 'we nearly went to economic war over import tariffs.' There is a difference. It is mostly in the garnish."*

**Tokens:** $TARGET_NODE, $MARKET_GOOD_1 (food), $CONTACT_NAME

**Steps:**
1. Acquire 15 food — varied sources preferred (HaveCargoMin)
2. Deliver to $TARGET_NODE (ArriveAtNode + NoCargoAtNode)
3. Attend Resolution Acknowledgment (TimerExpired — 5 ticks)

**Reward:** Low credits + Concord rep (+5) + Chitin rep (+5)

---

## Appendix: Binding Token Reference

| Token | Resolution | Example |
|---|---|---|
| $PLAYER_START | Player's current node at mission accept | Ironpeak Station |
| $ADJACENT_1 | Random node 1 hop from $PLAYER_START | Lattice Junction K-7 |
| $ADJACENT_2 | Random node 2 hops from $PLAYER_START | Frontier Post Theta |
| $TARGET_NODE | Mission objective node (may be far) | Communion Drifter Anchor |
| $MARKET_GOOD_1 | Primary trade good for mission | components |
| $MARKET_GOOD_2 | Secondary trade good (multi-good missions) | composites |
| $MARKET_GOOD_3 | Tertiary trade good (construction missions) | metal |
| $FACTION_1 | Primary faction involved | Valorin |
| $FACTION_2 | Secondary faction (diplomacy missions) | Communion |
| $CONTACT_NAME | Procedural NPC name | Quartermaster Seren Drossik |

## Appendix: Reward Tier Guidelines

| Tier | Credit Range | When Used |
|---|---|---|
| Low | 50-150 cr | Simple tasks, reputation-heavy rewards |
| Medium | 150-400 cr | Standard missions, moderate risk |
| High | 400-1000 cr | Multi-step, high-risk, or time-critical |
| Very High | 1000+ cr | Contraband, rare discoveries, chain missions |

Credit values scale with game progression. Early-game missions spawn at the low end of each tier; late-game at the high end. Reputation rewards do not scale.

## Appendix: Emotional Hook Uniqueness Check

Each template within a family uses a distinct emotional driver:

**Trade Route**: deadline pressure (01), market window (02), humanitarian (03), military urgency (04), technical precision (05), logistical relay (06), competitive rush (07), aggregation challenge (08), barter exchange (09), political ambition (10)

**Supply Crisis**: starvation (01), infrastructure failure (02), market correction (03), bureaucratic inertia (04), medical emergency (05), market glut (06), construction deadline (07), cascade failure (08)

**Escort**: competence gap (01), insurance calculation (02), vulnerability (03), irreplaceability (04), moral complexity (05)

**Bounty**: retribution (01), moral ambiguity (02), abuse of power (03), contested honor (04), clever vandalism (05), cosmic mystery (06)

**Investigation**: suppressed truth (01), perceptual mystery (02), personal stakes (03), scientific discovery (04), alien signal (05)

**Salvage**: heritage recovery (01), battlefield pragmatism (02), ancient technology (03), rescue urgency (04)

**Diplomacy**: peace urgency (01), cultural translation (02), economic opportunity (03), restorative justice (04)

**Construction**: capacity crisis (01), security need (02), personal progression (03)

**Contraband**: necessity vs. law (01), knowledge vs. control (02), clever exploitation (03)

**Survey**: exploration drive (01), intuitive sensing (02), probabilistic validation (03)

**Discovery**: scientific curiosity (01), data retrieval (02), route pathfinding (03)

**Celebration**: communal pride (01), warfront relief (02)

**Ceremony**: honor witness (01), engineering dedication (02), diplomatic resolution (03)
