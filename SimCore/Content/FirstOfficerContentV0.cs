using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T18.NARRATIVE.FO_CONTENT.001: First Officer dialogue content.
// Three candidates x 5 dialogue tiers. Each candidate has a personality,
// a blind spot, and an endgame lean. ~26 triggers x 3 candidates = 78 lines.
//
// Dialogue triggers fire when the player does something — not on a timer.
// The FO reacts to player ACTIONS, making the relationship feel earned.
//
// Key design rule: If the player chooses the endgame path the FO DOESN'T
// lean toward, the FO disagrees — not angrily, but with the weight of
// 20 hours of shared experience.
public static class FirstOfficerContentV0
{
    public sealed class CandidateProfile
    {
        public FirstOfficerCandidate Type { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string BlindSpot { get; init; } = "";
        public string EndgameLean { get; init; } = "";  // Reinforce, Naturalize, Renegotiate
    }

    public sealed class DialogueLine
    {
        public string TriggerToken { get; init; } = "";
        public FirstOfficerCandidate CandidateType { get; init; }
        public DialogueTier MinTier { get; init; }
        public string Text { get; init; } = "";
        public int RelationshipDelta { get; init; }
    }

    public static readonly IReadOnlyList<CandidateProfile> Candidates = new List<CandidateProfile>
    {
        new CandidateProfile
        {
            Type = FirstOfficerCandidate.Analyst,
            Name = "Maren",
            Description = "Probability-driven. Dry humor. Quietly caring beneath the numbers.",
            BlindSpot = "Uses data to avoid moral judgment. Pentagon ring response is clinical, not human.",
            EndgameLean = "Naturalize"
        },
        new CandidateProfile
        {
            Type = FirstOfficerCandidate.Veteran,
            Name = "Dask",
            Description = "Institutional. Competent. Loyal to structures that work.",
            BlindSpot = "Still believes in the Concord. Makes excuses as evidence mounts. Becomes Kesh.",
            EndgameLean = "Reinforce"
        },
        new CandidateProfile
        {
            Type = FirstOfficerCandidate.Pathfinder,
            Name = "Lira",
            Description = "Warm. Observational. Comfortable with chaos. Already adapted.",
            BlindSpot = "Already further along the adaptation curve than the player. Is that genuine — or module influence?",
            EndgameLean = "Renegotiate"
        },
    };

    // All dialogue lines, keyed by trigger token and candidate type.
    // Lines fire once (logged in DialogueEventLog to prevent repeats).
    public static readonly IReadOnlyList<DialogueLine> AllLines = new List<DialogueLine>
    {
        // ── EARLY TIER (tick 0-300): Establish personality ──────────

        // FIRST_WARP — thread lore: introduce the infrastructure mystery
        new DialogueLine { TriggerToken = "FIRST_WARP", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Transit complete. The thread held steady — 0.3% variance. I wonder how long they've been maintaining these lanes. The infrastructure cost must be... significant.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_WARP", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Clean jump. These lanes are well-maintained. Someone's pouring credits into keeping the network alive. In the service, we never asked who. Maybe we should have.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_WARP", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "Did you feel it? The hum changes in every lane. Each thread has its own frequency. Like they're singing. Or straining.", RelationshipDelta = 1 },

        // FIRST_NPC_MET
        new DialogueLine { TriggerToken = "FIRST_NPC_MET", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Another trader. Their cargo manifest suggests they're running a similar margin calculation. Competition narrows profit windows.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_NPC_MET", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Fleet contact. They're flying standard patrol patterns — disciplined. Whoever trained them knew what they were doing.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_NPC_MET", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "I wonder where they've been. Every ship out here has a story written in its hull scoring and cargo stains.", RelationshipDelta = 1 },

        // FIRST_DISCOVERY
        new DialogueLine { TriggerToken = "FIRST_DISCOVERY", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Anomalous readings. The signal pattern doesn't match anything in the registry. I've logged it for analysis.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "FIRST_DISCOVERY", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Contact. Unknown signature. In the service we'd call it in and wait for specialists. Out here... we ARE the specialists.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "FIRST_DISCOVERY", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "There. Do you see it? Something left that here on purpose. Or something left it here by accident. Either way — it's waiting.", RelationshipDelta = 2 },

        // FIRST_TRADE_LOSS
        new DialogueLine { TriggerToken = "FIRST_TRADE_LOSS", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Negative margin. Happens to everyone exactly once. I've already recalculated the route.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_TRADE_LOSS", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Losses happen. The route didn't fail — the intel was stale. We'll update our sources.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_TRADE_LOSS", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "Well. Now we know what that route actually costs. Better to learn it cheap.", RelationshipDelta = 1 },

        // FIRST_PROFITABLE_TRADE
        new DialogueLine { TriggerToken = "FIRST_PROFITABLE_TRADE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Margin positive. The model works. I've flagged three similar opportunities.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_PROFITABLE_TRADE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Clean trade. Good route. That's how empires start — one honest run at a time.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_PROFITABLE_TRADE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "Did you notice the station felt different when we left with their money? Lighter, somehow.", RelationshipDelta = 1 },

        // FIRST_DOCK_WARZONE — warfront economics + faction dependency
        new DialogueLine { TriggerToken = "FIRST_DOCK_WARZONE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Contested space. Demand data shows munitions at 3-4x baseline, fuel spiking too. Wars are expensive — for them, profitable for us. Neutrality carries a tariff surcharge.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_DOCK_WARZONE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Warzone. I've seen what happens when the supply lines break. These factions can't fight without each other's goods. That dependency... it's the real weapon here.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_DOCK_WARZONE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "Feel that tension? Two factions tearing at each other, but they still need what the other produces. The threads connect enemies. Strange, isn't it?", RelationshipDelta = 1 },

        // FIRST_INDUSTRY_SEEN — when docked at node with IndustrySites
        new DialogueLine { TriggerToken = "FIRST_INDUSTRY_SEEN", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Active industry here — converting inputs to outputs. Supply the chain and the margins compound. Worth programming a resource tap.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_INDUSTRY_SEEN", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Mining and refining. Ore goes in, metal comes out. Automate the supply line and this station prints credits for us.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_INDUSTRY_SEEN", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "Listen — you can hear the refineries. This station eats ore and breathes metal. Beautiful. And profitable, if we keep it fed.", RelationshipDelta = 1 },

        // GATE.S19.ONBOARD.FO_TRIGGERS.003: FIRST_SALE_COMPLETE — supply chain + automation hint
        new DialogueLine { TriggerToken = "FIRST_SALE_COMPLETE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Transaction logged. {GOOD} at {STATION}. A profitable route like this should be automated — programs run it while we pursue other opportunities.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_SALE_COMPLETE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Good trade. These stations can't survive without each other. Set up a program for this route, Captain. We've got bigger fish to find.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_SALE_COMPLETE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "One station's surplus, another's lifeline. You could automate this run and go looking for what else is out there.", RelationshipDelta = 1 },

        // GATE.S19.ONBOARD.FO_TRIGGERS.003: FIRST_COMBAT_WIN (fires after first NPC kill)
        new DialogueLine { TriggerToken = "FIRST_COMBAT_WIN", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "Hostile neutralized. Ship integrity nominal. I'd recommend checking the Ship tab at our next dock — better modules reduce hull stress.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_COMBAT_WIN", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "Clean engagement. Well fought. Next dock, check the Ship tab — proper outfitting makes the difference.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_COMBAT_WIN", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "They're gone. I hope they had somewhere to respawn. The Ship tab has upgrades that might keep us out of fights like that.", RelationshipDelta = 1 },

        // GATE.S19.ONBOARD.FO_TRIGGERS.003: ARRIVAL_NEW_SYSTEM — galaxy map + faction territory
        new DialogueLine { TriggerToken = "ARRIVAL_NEW_SYSTEM", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Early,
            Text = "New system. Different faction territory, different tariffs. The galaxy map (M) tracks where each faction holds sway. Profitable data.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "ARRIVAL_NEW_SYSTEM", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Early,
            Text = "New stars, new rules. Every faction controls its own space. Press M — the galaxy map shows whose territory you're trading in.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "ARRIVAL_NEW_SYSTEM", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Early,
            Text = "Different stars, different people, different stories. Press M — you can see where one faction's reach ends and another begins.", RelationshipDelta = 1 },

        // ── MID TIER (tick 300-600): Reveal moral lens ─────────────

        // FACTION_REP_GAINED
        new DialogueLine { TriggerToken = "FACTION_REP_GAINED", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Mid,
            Text = "Reputation increase logged. Faction trust is a currency with compound interest — every tier unlocks exponentially more access.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FACTION_REP_GAINED", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Mid,
            Text = "They're warming up to us. Good. In my experience, the first favor a faction asks of you tells you everything about what they really want.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FACTION_REP_GAINED", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Mid,
            Text = "Interesting — the station crew smiled at us today. Not because we're charming. Because we're useful. Big difference.", RelationshipDelta = 1 },

        // FIRST_MODULE_REFIT
        new DialogueLine { TriggerToken = "FIRST_MODULE_REFIT", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Mid,
            Text = "Module installed. Ship capability matrix updated. I've run projections — this changes our optimal trade routes by 12%.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_MODULE_REFIT", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Mid,
            Text = "New hardware. The ship feels different under thrust now. Like putting on better boots — you don't realize how bad the old ones were.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_MODULE_REFIT", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Mid,
            Text = "It's strange how a ship changes personality with each module. Like it's deciding what kind of vessel it wants to be.", RelationshipDelta = 1 },

        // SUPPLY_CHAIN_NOTICED
        new DialogueLine { TriggerToken = "SUPPLY_CHAIN_NOTICED", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Mid,
            Text = "This production chain is three nodes deep. Each station depends on the one before it. Break any link and the downstream stations starve. Elegant and terrifying.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "SUPPLY_CHAIN_NOTICED", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Mid,
            Text = "Supply lines. In the service, we'd guard these with warships. Out here, they're just... trusted to work. Someone has a lot of faith in the pentagon.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "SUPPLY_CHAIN_NOTICED", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Mid,
            Text = "Have you noticed how every station needs something from somewhere else? Nobody's self-sufficient. It's like the whole galaxy was designed to make everyone depend on everyone.", RelationshipDelta = 2 },

        // FIRST_WAR_GOODS_SALE
        new DialogueLine { TriggerToken = "FIRST_WAR_GOODS_SALE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Mid,
            Text = "War goods sold. Profit margin: excellent. Downstream casualty correlation: nonzero. Both facts are in the ledger now.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "FIRST_WAR_GOODS_SALE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Mid,
            Text = "Those munitions will reach the front within a cycle. Someone needed them. That's enough justification for today.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "FIRST_WAR_GOODS_SALE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Mid,
            Text = "I watched the dock crew load those crates. They didn't look at us. People who handle munitions learn not to look at the supplier.", RelationshipDelta = 2 },

        // SELL_BOTH_SIDES
        new DialogueLine { TriggerToken = "SELL_BOTH_SIDES", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Mid,
            Text = "Supplying both combatants. Economically rational. The probability that we're prolonging the war is... I'd rather not calculate it.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "SELL_BOTH_SIDES", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Mid,
            Text = "We're selling to both sides now. I've seen contractors do this. They always have reasons. The reasons are always good.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "SELL_BOTH_SIDES", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Mid,
            Text = "Both sides. You know what that makes us? Neutral. Perfectly, precisely, profitably neutral.", RelationshipDelta = 2 },

        // FIRST_EMBARGO_HIT
        new DialogueLine { TriggerToken = "FIRST_EMBARGO_HIT", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Mid,
            Text = "Embargo enforced. Revenue impact: significant. But embargoes create arbitrage, and arbitrage is where we live.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_EMBARGO_HIT", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Mid,
            Text = "Embargo. The Concord does this when they're losing. It means the war is going badly for someone important.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "FIRST_EMBARGO_HIT", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Mid,
            Text = "Closed borders. Funny how 'security' always means 'you can't go there.' Never means 'we'll protect you here.'", RelationshipDelta = 1 },

        // ── FRACTURE TIER (tick 600-1000): Foreshadow arc ──────────

        // INSTRUMENT_DIVERGENCE
        new DialogueLine { TriggerToken = "INSTRUMENT_DIVERGENCE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "The standard and fracture instruments disagree by 14%. Both are calibrated. Both are correct. That's not possible. That's not... that's not how measurement works.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "INSTRUMENT_DIVERGENCE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "Two instruments, two answers. In the service, we'd trust the older one. But what if the older one was built before whatever changed?", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "INSTRUMENT_DIVERGENCE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "The readings disagree because they're measuring different truths. What if neither one is wrong? What if 'wrong' is a lane-space idea?", RelationshipDelta = 2 },

        // VOID_SITE_EXPLORED
        new DialogueLine { TriggerToken = "VOID_SITE_EXPLORED", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "Void site survey complete. The energy signatures are... the data doesn't decompose into any known basis. I don't have a model for this. I don't think anyone does.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "VOID_SITE_EXPLORED", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "This place wasn't built. It wasn't grown. It's like a scar that decided to become something else. My training has nothing for this.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "VOID_SITE_EXPLORED", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "Can you hear it? Not with ears. With... I don't have a word. This place is trying to show us something. We just need to learn to look differently.", RelationshipDelta = 3 },

        // TOPOLOGY_SHIFT
        new DialogueLine { TriggerToken = "TOPOLOGY_SHIFT", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "The lane topology changed. The graph edge mutated. Stars don't do that. Lanes don't do that. The infrastructure we built our economy on just... reorganized itself.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "TOPOLOGY_SHIFT", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "The map is wrong. Not our map — the actual map. The lanes moved. I've been navigating for twenty years and the ground just shifted.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "TOPOLOGY_SHIFT", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "The lanes are breathing. Expanding and contracting. The galaxy isn't a fixed thing — it's a living thing, and we've been building cities on its skin.", RelationshipDelta = 2 },

        // FIRST_FRACTURE_JUMP
        new DialogueLine { TriggerToken = "FIRST_FRACTURE_JUMP", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "The nav readings don't match any model I have. That's... that's not supposed to happen. I'll need time with this data.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "FIRST_FRACTURE_JUMP", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "I've flown through combat zones. I've flown through debris fields. I have never felt a ship do what ours just did.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "FIRST_FRACTURE_JUMP", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "...Did you feel that? The way space looked for a moment before the jump stabilized? It was almost... readable.", RelationshipDelta = 3 },

        // FRACTURE_WEIGHT_SURPRISE
        new DialogueLine { TriggerToken = "FRACTURE_WEIGHT_SURPRISE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "The manifest doesn't match. We loaded 100, we have 87. The cargo didn't change — the measurement relationship did. I need to rethink everything.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "FRACTURE_WEIGHT_SURPRISE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "Cargo discrepancy. In the service, we'd call this fraud. Out here... I don't know what to call it. The hold is the same size. The metal is the same metal.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "FRACTURE_WEIGHT_SURPRISE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "The weight shifted. Don't you think that's beautiful? Space isn't lying to us — it's showing us that 'weight' was always an agreement, not a fact.", RelationshipDelta = 2 },

        // REGULAR_NPC_VANISHES
        new DialogueLine { TriggerToken = "REGULAR_NPC_VANISHES", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "They're not on the registry anymore. The probability of survival given the engagement reports is... I'm not going to say the number.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "REGULAR_NPC_VANISHES", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "I checked the bulletin three times. Listed as lost. Just... gone. This is what happens. This is what war does.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "REGULAR_NPC_VANISHES", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "Remember when they docked ahead of us at Waystation? Bought all the composites? I was annoyed. I'd like to be annoyed at them again.", RelationshipDelta = 3 },

        // ── REVELATION TIER (tick 1000-1500): Midpoint turn ────────

        // ANCIENT_DISCOVERY
        new DialogueLine { TriggerToken = "ANCIENT_DISCOVERY", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Revelation,
            Text = "The artifact predates every known civilization in the registry. The alloy composition uses elements that don't exist in normal space. Someone was here before the lanes.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "ANCIENT_DISCOVERY", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Revelation,
            Text = "This is older than the Concord. Older than the factions. Older than... everything. Whoever made this didn't need lanes. They didn't need any of it.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "ANCIENT_DISCOVERY", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Revelation,
            Text = "It's beautiful. And terrifying. Because if they could build THIS and they're still gone... what happened to them? What happens to everyone who goes this far?", RelationshipDelta = 4 },

        // KNOWLEDGE_WEB_INSIGHT
        new DialogueLine { TriggerToken = "KNOWLEDGE_WEB_INSIGHT", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Revelation,
            Text = "The connections in the discovery web are forming a pattern. Cross-reference complete. This isn't random scatter — it's a message written across light-years.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "KNOWLEDGE_WEB_INSIGHT", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Revelation,
            Text = "I've been looking at the map wrong. The discoveries aren't anomalies — they're landmarks. Waypoints left for someone who'd eventually ask the right questions.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "KNOWLEDGE_WEB_INSIGHT", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Revelation,
            Text = "Do you see the web now? Every log, every site, every shifted lane — they're all part of the same conversation. And we're finally learning the language.", RelationshipDelta = 3 },

        // GATE.S8.STORY.FO_REVELATION.001: FO reactions to the 5 Recontextualizations.

        // REVELATION_MODULE_ORIGIN (R1): Fracture drive modules aren't human-made.
        new DialogueLine { TriggerToken = "REVELATION_MODULE_ORIGIN", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Fracture,
            Text = "The crystalline lattice in the drive core isn't manufactured. It's grown. The isotope ratios predate human metallurgy by millennia. We've been flying something we never built.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "REVELATION_MODULE_ORIGIN", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Fracture,
            Text = "I've maintained these drives for fifteen years. Replaced parts, tuned resonance, calibrated frequency. And the core was never ours? What were we even maintaining?", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "REVELATION_MODULE_ORIGIN", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Fracture,
            Text = "The module... sings. I always thought it was sympathetic vibration. But it's not resonating with the ship. It's resonating with wherever it came from.", RelationshipDelta = 4 },

        // REVELATION_CONCORD_SUPPRESSION (R2): Concord knew about fracture space — containment not peacekeeping.
        new DialogueLine { TriggerToken = "REVELATION_CONCORD_SUPPRESSION", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Revelation,
            Text = "Containment. Not peacekeeping — containment. The regulatory framework, the trade restrictions, the research embargoes. They weren't protecting commerce. They were hiding what's beyond the lanes.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "REVELATION_CONCORD_SUPPRESSION", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Revelation,
            Text = "I served the Concord for a decade. Enforced their checkpoints. Believed in their mission. And the whole time... they knew. They KNEW and they let me believe I was keeping the peace.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "REVELATION_CONCORD_SUPPRESSION", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Revelation,
            Text = "All those restricted zones. All those 'navigational hazard' warnings. I flew around them every time. They weren't hazards. They were doors, and the Concord nailed them shut.", RelationshipDelta = 4 },

        // REVELATION_COMMUNION_TRUTH (R4): Communion 'unity' masks species privilege.
        new DialogueLine { TriggerToken = "REVELATION_COMMUNION_TRUTH", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Revelation,
            Text = "The Communion's 'universal harmony' model has a species-weighted utility function. Their definition of unity was always hierarchical. The math doesn't lie — but they used it to.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "REVELATION_COMMUNION_TRUTH", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Revelation,
            Text = "They talk about unity like it's a gift. But it's not given equally, is it? Some species are more unified than others. Some voices count more in their chorus.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "REVELATION_COMMUNION_TRUTH", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Revelation,
            Text = "I wanted to believe them. The outreach, the shared songs, the promise that everyone belongs. But belonging isn't the same as being heard. And they never let everyone speak.", RelationshipDelta = 5 },

        // REVELATION_LIVING_GEOMETRY (R5): Fracture space is alive — trade network is the wound.
        new DialogueLine { TriggerToken = "REVELATION_LIVING_GEOMETRY", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Endgame,
            Text = "The geometry responds to stimulus. It adapts. It heals. The fracture zones aren't damage — they're immune responses. We built a civilization inside a wound and the wound is waking up.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "REVELATION_LIVING_GEOMETRY", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Endgame,
            Text = "Alive. All of it. The lattice drones, the topology shifts, the instability — it's not random. It's not hostile. It's trying to close the wound we carved into it.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "REVELATION_LIVING_GEOMETRY", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Endgame,
            Text = "I felt it. In the fracture jumps, in the way the module hums when we approach void sites. It's been talking to us the whole time. We just didn't know how to listen.", RelationshipDelta = 5 },

        // PENTAGON_BREAK
        new DialogueLine { TriggerToken = "PENTAGON_BREAK", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Revelation,
            Text = "The dependency ring is engineered. I should have seen it. The probability distributions were too clean. Too stable. I was looking at a designed system and calling it emergent.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "PENTAGON_BREAK", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Revelation,
            Text = "The trade routes... the dependencies... all of it? Designed? No. The Concord built something real. People chose this system. People CHOSE it.", RelationshipDelta = 4 },
        new DialogueLine { TriggerToken = "PENTAGON_BREAK", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Revelation,
            Text = "Oh. Oh, I think I always knew. The way the routes feel when you've flown enough of them — too convenient. Too necessary. Like someone decided what 'necessary' means.", RelationshipDelta = 4 },

        // BLINDSPOT_EXPOSED
        new DialogueLine { TriggerToken = "BLINDSPOT_EXPOSED", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Revelation,
            Text = "You're right. I've been hiding behind the numbers. The data doesn't tell you what to DO. I knew that. I used it as an excuse not to feel anything.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "BLINDSPOT_EXPOSED", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Revelation,
            Text = "I've been making excuses for the Concord. Every new piece of evidence, another explanation. I sound like... I sound like Kesh. Don't I.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "BLINDSPOT_EXPOSED", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Revelation,
            Text = "How long have I been adapting? Before you found the module — or after? I can't tell anymore. And that... that's the question the module doesn't want me to ask.", RelationshipDelta = 5 },

        // ── ENDGAME TIER (tick 1500+): Agrees, argues, or goes silent ──

        // ENDGAME_REINFORCE
        new DialogueLine { TriggerToken = "ENDGAME_REINFORCE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Endgame,
            Text = "Reinforce. The data supports it. The cage works. I just wish the data didn't make me feel like I'm choosing the cage because I'm afraid of what's outside it.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "ENDGAME_REINFORCE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Endgame,
            Text = "Yes. Reinforce. I've been waiting for you to see it. The threads work. The pentagon holds. What we're preserving is imperfect, but it's real, and it's ours.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "ENDGAME_REINFORCE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Endgame,
            Text = "Reinforce? After everything we've seen? After what the module showed us? ...All right. I'll stay. But I need you to know: I think we're choosing to go back to sleep.", RelationshipDelta = -2 },

        // ENDGAME_NATURALIZE
        new DialogueLine { TriggerToken = "ENDGAME_NATURALIZE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Endgame,
            Text = "Naturalize. The math says it works. The transition period says people die. I've run the models. The acceptable loss rate is... there is no acceptable loss rate, is there.", RelationshipDelta = 5 },
        new DialogueLine { TriggerToken = "ENDGAME_NATURALIZE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Endgame,
            Text = "You're tearing down everything that kept people alive for centuries because you found something shinier. I hope you're right. I really do. Because if you're wrong, the people who trusted the threads pay for it.", RelationshipDelta = -2 },
        new DialogueLine { TriggerToken = "ENDGAME_NATURALIZE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Endgame,
            Text = "Freedom. Real freedom, not the kind someone designed for us. It'll hurt. It'll cost. But the alternative is staying in a cage forever. Let's go.", RelationshipDelta = 3 },

        // ENDGAME_RENEGOTIATE
        new DialogueLine { TriggerToken = "ENDGAME_RENEGOTIATE", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Endgame,
            Text = "Renegotiate. The data is insufficient. The historical precedent is zero successful outcomes. And you want to do it anyway. ...I'll run the numbers. Someone should.", RelationshipDelta = 2 },
        new DialogueLine { TriggerToken = "ENDGAME_RENEGOTIATE", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Endgame,
            Text = "Every threshold-crosser before you either died or vanished. The Communion told you that. And you're still going. I don't understand. But I'm not leaving.", RelationshipDelta = 1 },
        new DialogueLine { TriggerToken = "ENDGAME_RENEGOTIATE", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Endgame,
            Text = "Yes. This is what the module was for. This is what WE'RE for. I've felt it since the first fracture jump. Haven't you?", RelationshipDelta = 5 },

        // FO_FAREWELL
        new DialogueLine { TriggerToken = "FO_FAREWELL", CandidateType = FirstOfficerCandidate.Analyst, MinTier = DialogueTier.Endgame,
            Text = "Whatever happens next — I want you to know: the numbers never captured what this journey meant. And I think that's the most important thing the numbers ever taught me.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "FO_FAREWELL", CandidateType = FirstOfficerCandidate.Veteran, MinTier = DialogueTier.Endgame,
            Text = "I've served under commanders who knew what they were doing. I've served under commanders who didn't. You're the first one who asked me what I thought. That mattered.", RelationshipDelta = 3 },
        new DialogueLine { TriggerToken = "FO_FAREWELL", CandidateType = FirstOfficerCandidate.Pathfinder, MinTier = DialogueTier.Endgame,
            Text = "This was always a one-way trip, wasn't it? From the first lane jump to here. We've been falling forward the whole time. And I wouldn't have missed a single light-year.", RelationshipDelta = 3 },
    };

    /// <summary>
    /// Get the dialogue line for a specific trigger and candidate type.
    /// Returns null if no line exists for this combination.
    /// </summary>
    public static DialogueLine? GetLine(string triggerToken, FirstOfficerCandidate candidateType)
    {
        foreach (var line in AllLines)
        {
            if (line.CandidateType == candidateType &&
                string.Equals(line.TriggerToken, triggerToken, System.StringComparison.Ordinal))
            {
                return line;
            }
        }
        return null;
    }
}
