using SimCore.Entities;
using System.Collections.Generic;

namespace SimCore.Content;

// GATE.T18.NARRATIVE.WAR_FACES.001: Three NPCs that give wars a human face.
//
// The Regular: NPC trader on overlapping route. Stationmasters mention them.
//   War reaches home system -> ship listed as lost. Silent disappearance.
//
// The Stationmaster: Named NPC at player's most-visited station.
//   8-10 contextual lines based on what player delivers.
//
// The Enemy: Valorin patrol captain. Interdicts player once.
//   Hours later: same captain at Communion station, buying food, not in uniform.
public static class WarFacesContentV0
{
    // ── The Regular ──────────────────────────────────────────────

    public const string RegularNpcId = "npc_regular_keris";
    public const string RegularName = "Keris";
    public const string RegularFaction = "Valorin";

    // Stationmaster mentions of the Regular (ambient lines at shared stations)
    // GATE.T18.CHARACTER.WARFACES_DEPTH.001: Expanded from 3 to 6 mentions for variety.
    public static readonly IReadOnlyList<string> RegularMentions = new List<string>
    {
        "Keris docked earlier. Bought all the composites again.",
        "That Valorin trader was here — Keris. Loaded up on electronics and left in a hurry.",
        "Keris asked about you, actually. Said you have good route timing.",
        "Keris had engine trouble last cycle. Still made the run on time. Stubborn.",
        "Saw the Duskrunner's transponder on approach. Keris does this route like clockwork.",
        "Keris turned down a Communion escort contract. Said independent traders don't take sides.",
    };

    // GATE.T18.CHARACTER.WARFACES_DEPTH.001: Ghost mentions — after the Regular vanishes,
    // stations still remember them. These appear instead of live mentions.
    public static readonly IReadOnlyList<string> RegularGhostMentions = new List<string>
    {
        "The Duskrunner's berth is still reserved. Nobody's cancelled it yet.",
        "Someone left flowers at docking bay 7. That was Keris's usual spot.",
        "A Valorin shipping clerk came by asking about Keris's last manifest. I couldn't help.",
    };

    // Bulletin text when the Regular vanishes
    public const string RegularVanishBulletin =
        "Concord Shipping Bulletin: Vessel 'Duskrunner' (reg. Keris val-Toren) listed as lost. " +
        "Last known position: contested sector. No distress signal received.";

    // ── The Stationmaster ────────────────────────────────────────

    public const string StationmasterNpcId = "npc_stationmaster";
    public const string StationmasterDefaultName = "Overseer Hale";

    public sealed class StationmasterLine
    {
        public string TriggerToken { get; init; } = "";
        public string Text { get; init; } = "";
    }

    // Stationmaster lines, triggered by what the player delivers.
    // Order matters: earlier lines fire first. DialogueState tracks progress.
    public static readonly IReadOnlyList<StationmasterLine> StationmasterLines = new List<StationmasterLine>
    {
        new StationmasterLine { TriggerToken = "SM_FIRST_MUNITIONS",
            Text = "Munitions. The front is hungry." },
        new StationmasterLine { TriggerToken = "SM_REPEAT_MUNITIONS",
            Text = "Do you know there used to be a school module here?" },
        new StationmasterLine { TriggerToken = "SM_FOOD_DELIVERY",
            Text = "Thank you. People forget stations need food too." },
        new StationmasterLine { TriggerToken = "SM_COMPOSITES",
            Text = "Composites go straight to the military dock now." },
        new StationmasterLine { TriggerToken = "SM_ELECTRONICS",
            Text = "Electronics? We haven't had those in months." },
        new StationmasterLine { TriggerToken = "SM_WAR_INTENSIFIES",
            Text = "I don't remember what this station was for before the war." },
        new StationmasterLine { TriggerToken = "SM_RELIABLE",
            Text = "You're reliable. That matters more than you know." },
        new StationmasterLine { TriggerToken = "SM_PENTAGON_BREAK",
            Text = "Something changed. The supply came from... inside?" },
        new StationmasterLine { TriggerToken = "SM_NATURALIZE_DISTRESS",
            Text = "The lights went out. All of them." },
        new StationmasterLine { TriggerToken = "SM_POST_RESCUE",
            Text = "I didn't know the threads did that. I thought they were just... roads." },
    };

    // ── The Enemy ────────────────────────────────────────────────

    public const string EnemyNpcId = "npc_enemy_captain";
    public const string EnemyName = "Captain Voss";
    public const string EnemyFaction = "Valorin";

    // Interdiction dialogue
    public const string EnemyInterdictionText =
        "Valorin Patrol. You're carrying cargo through contested space without a convoy marker. " +
        "We're confiscating twenty percent as a security contribution. Don't take it personally.";

    // Communion station encounter — optional interaction
    public const string EnemyCommunionEncounterText =
        "The Valorin captain from the interdiction is here. Out of uniform. Buying food. " +
        "They notice you. A small nod. No words. They go back to their transaction.";

    // GATE.T18.CHARACTER.WARFACES_DEPTH.001: Recontextualization variants.
    // These replace the base encounter text based on player actions since interdiction.

    // If player has sold munitions to both sides
    public const string EnemyRecontextBothSides =
        "Captain Voss is at the Communion market. Still out of uniform. They see you and pause. " +
        "'I checked the shipping logs. You sell to everyone.' A beat. 'I used to think that was wrong. " +
        "Now I'm buying food at a Communion station. Maybe we're both just surviving.'";

    // If player has high Valorin reputation despite the interdiction
    public const string EnemyRecontextValRep =
        "Captain Voss is here again. They look older than the interdiction. Tired. " +
        "'The admiralty speaks well of you now. Funny, isn't it? I took your cargo and you " +
        "still trade with us.' They push a food crate onto a gravity loader. 'Loyalty is " +
        "strange out here.'";

    // If the Regular has vanished (Keris was Valorin too)
    public const string EnemyRecontextAfterVanish =
        "Captain Voss sits alone at a Communion food stall. They see you and don't look away. " +
        "'You knew Keris. The Duskrunner.' It isn't a question. 'I signed the patrol order that " +
        "pulled coverage from that sector.' They go back to their food. 'The war took them. " +
        "I just... pointed it in the right direction.'";

    // If player has completed the pentagon break (revelation moment)
    public const string EnemyRecontextPostRevelation =
        "Captain Voss is here. They look different — not defeated, but recalibrated. " +
        "'I heard what you found. About the dependency ring. About all of it.' They stare at " +
        "their food. 'I enforced trade laws for a system that was... designed. Every patrol, " +
        "every confiscation — maintaining someone else's architecture. And I was proud of it.'";
}
