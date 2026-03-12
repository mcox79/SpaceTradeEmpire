using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;
using System;
using System.Linq;

namespace SimCore.Systems;

// GATE.T18.NARRATIVE.WAR_FACES.001: Narrative NPC lifecycle system.
// Manages three war face NPCs: Regular (vanishes), Stationmaster (contextual),
// Enemy (reappears). All state mutations are deterministic.
public static class NarrativeNpcSystem
{
    /// <summary>
    /// Per-tick processing for narrative NPCs.
    /// - Regular: check if war reached home, trigger vanish after delay.
    /// - Stationmaster: advance dialogue based on player deliveries.
    /// - Enemy: manage interdiction→encounter progression.
    /// </summary>
    public static void Process(SimState state)
    {
        if (state.NarrativeNpcs.Count == 0) return;

        foreach (var kv in state.NarrativeNpcs)
        {
            var npc = kv.Value;
            if (!npc.IsAlive) continue;

            switch (npc.Kind)
            {
                case NarrativeNpcKind.Regular:
                    ProcessRegular(state, npc);
                    break;
                case NarrativeNpcKind.Stationmaster:
                    // Stationmaster dialogue is triggered by trade events, not tick processing
                    break;
                case NarrativeNpcKind.Enemy:
                    ProcessEnemy(state, npc);
                    break;
            }
        }
    }

    // ── Regular NPC ──────────────────────────────────────────────

    private static void ProcessRegular(SimState state, NarrativeNpc npc)
    {
        // Check if war has reached the Regular's home node
        if (npc.VanishTick > 0)
        {
            // Vanish timer already set — check if delay has elapsed
            if (state.Tick >= npc.VanishTick)
            {
                npc.IsAlive = false;
                npc.VanishReason = WarFacesContentV0.RegularVanishBulletin;
            }
            return;
        }

        // Check if home node is now in an active warfront
        if (IsNodeInActiveWarfront(state, npc.HomeNodeId))
        {
            // Start vanish countdown
            npc.VanishTick = state.Tick + NarrativeTweaksV0.RegularVanishDelayTicks;
        }
    }

    /// <summary>
    /// Get the Regular NPC's mention text for a station (if the Regular
    /// has been to this station). Returns empty string if no mention.
    /// GATE.T18.CHARACTER.WARFACES_DEPTH.001: Ghost mentions after vanish.
    /// </summary>
    public static string GetRegularMention(SimState state, string nodeId)
    {
        if (!state.NarrativeNpcs.TryGetValue(WarFacesContentV0.RegularNpcId, out var npc))
            return "";

        // Use deterministic selection based on node+tick
        ulong h = HashString(nodeId);
        h ^= (uint)state.Tick;
        h *= 1099511628211UL;

        // If the Regular has vanished, return ghost mentions instead
        if (!npc.IsAlive)
        {
            var ghostMentions = WarFacesContentV0.RegularGhostMentions;
            if (ghostMentions.Count == 0) return "";
            int gIdx = (int)(h % (ulong)ghostMentions.Count);
            return ghostMentions[gIdx];
        }

        // Regular mentions are available at nodes on their route
        var mentions = WarFacesContentV0.RegularMentions;
        if (mentions.Count == 0) return "";
        int idx = (int)(h % (ulong)mentions.Count);
        return mentions[idx];
    }

    // ── Stationmaster NPC ────────────────────────────────────────

    /// <summary>
    /// Try to get the next Stationmaster dialogue line based on a delivery trigger.
    /// Returns the line text if a new line fires, empty string otherwise.
    /// </summary>
    public static string TryStationmasterDialogue(SimState state, string triggerToken)
    {
        if (!state.NarrativeNpcs.TryGetValue(WarFacesContentV0.StationmasterNpcId, out var npc))
            return "";
        if (!npc.IsAlive) return "";

        // Check if this trigger has already fired
        if (npc.FiredTriggers.Contains(triggerToken)) return "";

        // Find the matching line
        foreach (var line in WarFacesContentV0.StationmasterLines)
        {
            if (string.Equals(line.TriggerToken, triggerToken, StringComparison.Ordinal))
            {
                npc.FiredTriggers.Add(triggerToken);
                npc.DialogueState++;
                return line.Text;
            }
        }

        return "";
    }

    /// <summary>
    /// Determine the stationmaster trigger token based on a goods delivery.
    /// Returns the appropriate trigger token or empty string.
    /// </summary>
    public static string GetStationmasterTriggerForDelivery(SimState state, string goodId)
    {
        if (!state.NarrativeNpcs.TryGetValue(WarFacesContentV0.StationmasterNpcId, out var npc))
            return "";
        if (!npc.IsAlive) return "";

        // Check for reliable threshold first
        if (StationMemorySystem.IsReliableAtStation(state, npc.NodeId) &&
            !npc.FiredTriggers.Contains("SM_RELIABLE"))
        {
            return "SM_RELIABLE";
        }

        return goodId switch
        {
            WellKnownGoodIds.Munitions when !npc.FiredTriggers.Contains("SM_FIRST_MUNITIONS") => "SM_FIRST_MUNITIONS",
            WellKnownGoodIds.Munitions when !npc.FiredTriggers.Contains("SM_REPEAT_MUNITIONS") => "SM_REPEAT_MUNITIONS",
            WellKnownGoodIds.Food when !npc.FiredTriggers.Contains("SM_FOOD_DELIVERY") => "SM_FOOD_DELIVERY",
            WellKnownGoodIds.Composites when !npc.FiredTriggers.Contains("SM_COMPOSITES") => "SM_COMPOSITES",
            WellKnownGoodIds.Electronics when !npc.FiredTriggers.Contains("SM_ELECTRONICS") => "SM_ELECTRONICS",
            _ => ""
        };
    }

    // ── Enemy NPC ────────────────────────────────────────────────

    private static void ProcessEnemy(SimState state, NarrativeNpc npc)
    {
        // After interdiction, check if enough time has passed for Communion encounter
        if (npc.HasInterdicted && !npc.CommunionEncounterAvailable)
        {
            // Make encounter available after a substantial delay (war consequence delay)
            int ticksSinceInterdiction = state.Tick - npc.DialogueState; // DialogueState stores interdiction tick
            if (ticksSinceInterdiction >= NarrativeTweaksV0.WarConsequenceDelayTicks)
            {
                npc.CommunionEncounterAvailable = true;
            }
        }
    }

    /// <summary>
    /// Trigger the Enemy interdiction. Returns interdiction text.
    /// Called when player enters contested space and interdiction conditions are met.
    /// </summary>
    public static string TriggerInterdiction(SimState state)
    {
        if (!state.NarrativeNpcs.TryGetValue(WarFacesContentV0.EnemyNpcId, out var npc))
            return "";
        if (!npc.IsAlive || npc.HasInterdicted) return "";

        npc.HasInterdicted = true;
        npc.DialogueState = state.Tick; // Store interdiction tick

        return WarFacesContentV0.EnemyInterdictionText;
    }

    /// <summary>
    /// Check if the Enemy Communion encounter is available at a node.
    /// Returns encounter text if available, empty string otherwise.
    /// GATE.T18.CHARACTER.WARFACES_DEPTH.001: Recontextualization — encounter text
    /// varies based on player actions since the interdiction.
    /// </summary>
    public static string CheckCommunionEncounter(SimState state, string nodeId)
    {
        if (!state.NarrativeNpcs.TryGetValue(WarFacesContentV0.EnemyNpcId, out var npc))
            return "";
        if (!npc.IsAlive || !npc.CommunionEncounterAvailable) return "";

        // Only triggers at Communion stations
        if (!state.NodeFactionId.TryGetValue(nodeId, out var fid)) return "";
        if (!string.Equals(fid, "Communion", StringComparison.Ordinal)) return "";

        // Mark as consumed
        npc.CommunionEncounterAvailable = false;
        npc.FiredTriggers.Add("COMMUNION_ENCOUNTER");

        // Select recontextualized text based on player state
        return SelectEnemyRecontextText(state);
    }

    /// <summary>
    /// Select the most appropriate enemy encounter text based on player actions.
    /// Priority: post-revelation > Regular vanished > both-sides > Valorin rep > base.
    /// </summary>
    private static string SelectEnemyRecontextText(SimState state)
    {
        // Post-revelation: knowledge graph has revealed the pentagon
        if (state.FirstOfficer != null && state.FirstOfficer.Tier >= DialogueTier.Revelation
            && state.FirstOfficer.DialogueEventLog.Any(e =>
                string.Equals(e.TriggerToken, "PENTAGON_BREAK", StringComparison.Ordinal)))
        {
            return WarFacesContentV0.EnemyRecontextPostRevelation;
        }

        // Regular vanished: both Keris and Voss are Valorin — personal connection
        if (state.NarrativeNpcs.TryGetValue(WarFacesContentV0.RegularNpcId, out var regular)
            && !regular.IsAlive)
        {
            return WarFacesContentV0.EnemyRecontextAfterVanish;
        }

        // Positive Valorin reputation despite the interdiction
        if (state.FactionReputation.TryGetValue("Valorin", out int valRep)
            && valRep > NarrativeTweaksV0.EnemyRecontextValorinRepThreshold)
        {
            return WarFacesContentV0.EnemyRecontextValRep;
        }

        // Default encounter text
        return WarFacesContentV0.EnemyCommunionEncounterText;
    }

    // ── Utilities ────────────────────────────────────────────────

    private static bool IsNodeInActiveWarfront(SimState state, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        if (!state.NodeFactionId.TryGetValue(nodeId, out var fid)) return false;

        foreach (var wf in state.Warfronts.Values)
        {
            if (string.Equals(fid, wf.CombatantA, StringComparison.Ordinal) ||
                string.Equals(fid, wf.CombatantB, StringComparison.Ordinal))
            {
                // Check if warfront intensity is high enough (active war)
                if (wf.Intensity > 0) return true;
            }
        }
        return false;
    }

    // FNV-1a 64-bit hash
    private static ulong HashString(string s)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in s)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }
}
