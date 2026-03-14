using System.Collections.Generic;

namespace SimCore.Tweaks;

// GATE.S7.TECH_ACCESS.LOCK.001: Faction-locked module access definitions.
public static class TechAccessTweaksV0
{
    // Per-module faction lock: moduleId -> (factionId, requiredRepTier).
    // Rep tiers: Allied=75, Friendly=25, Neutral=-25.
    // Modules not in this table are universally available.
    public static readonly Dictionary<string, (string FactionId, int RequiredRep)> FactionLockedModules = new()
    {
        // Concord T2 modules — require Friendly standing with Concord
        { "mod_precision_nav_t2", ("concord", 25) },
        { "mod_shield_matrix_t2", ("concord", 25) },

        // Chitin T2 modules — require Friendly standing with Chitin
        { "mod_adaptive_hull_t2", ("chitin", 25) },
        { "mod_swarm_projector_t2", ("chitin", 25) },

        // Weavers T2 modules — require Friendly standing with Weavers
        { "mod_lattice_weave_t2", ("weavers", 25) },
        { "mod_cargo_optimizer_t2", ("weavers", 25) },

        // Valorin T2 modules — require Friendly standing with Valorin
        { "mod_kinetic_amplifier_t2", ("valorin", 25) },
        { "mod_rapid_loader_t2", ("valorin", 25) },

        // Communion T2 modules — require Friendly standing with Communion
        { "mod_fracture_lens_t2", ("communion", 25) },
        { "mod_harmony_field_t2", ("communion", 25) },

        // Allied-tier modules (75 rep required)
        { "mod_concord_elite_t2", ("concord", 75) },
        { "mod_chitin_elite_t2", ("chitin", 75) },
        { "mod_weavers_elite_t2", ("weavers", 75) },
        { "mod_valorin_elite_t2", ("valorin", 75) },
        { "mod_communion_elite_t2", ("communion", 75) },
    };

    /// <summary>
    /// Check if a module is faction-locked and whether the player meets the rep requirement.
    /// Returns (isLocked, factionId, requiredRep, playerMeetsRequirement).
    /// </summary>
    public static (bool IsLocked, string FactionId, int RequiredRep, bool HasAccess) CheckAccess(
        string moduleId, Dictionary<string, int> factionReputation)
    {
        if (string.IsNullOrEmpty(moduleId) || !FactionLockedModules.TryGetValue(moduleId, out var lockInfo))
            return (false, "", 0, true); // Not locked

        int playerRep = 0;
        factionReputation?.TryGetValue(lockInfo.FactionId, out playerRep);

        return (true, lockInfo.FactionId, lockInfo.RequiredRep, playerRep >= lockInfo.RequiredRep);
    }
}
