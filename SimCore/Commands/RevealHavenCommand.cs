using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.S8.HAVEN.REVEAL_THREAD.001: Reveal Haven to a faction ally (Tier 4+, one-time choice).
public sealed class RevealHavenCommand : ICommand
{
    public string FactionId { get; }

    public RevealHavenCommand(string factionId)
    {
        FactionId = factionId ?? "";
    }

    public void Execute(SimState state)
    {
        var haven = state.Haven;
        if (haven == null || !haven.Discovered) return;

        // Requires Tier 4+ (Expanded).
        if (haven.Tier < HavenTier.Expanded) return;

        // Already revealed to someone — one-time choice.
        if (!string.IsNullOrEmpty(haven.RevealedToFactionId)) return;

        // Must have positive reputation with the faction.
        if (string.IsNullOrEmpty(FactionId)) return;
        int rep = state.FactionReputation.TryGetValue(FactionId, out var r) ? r : 0;
        if (rep < HavenTweaksV0.RevealMinFactionRep) return;

        // Reveal.
        haven.RevealedToFactionId = FactionId;

        // Permanent rep boost.
        Systems.ReputationSystem.AdjustReputation(state, FactionId, HavenTweaksV0.RevealRepBonus);

        // Spawn faction visitor as Haven resident.
        haven.Residents.Add(new HavenResident
        {
            ResidentId = $"visitor_{FactionId}",
            Name = $"{FactionId} Envoy",
            Role = "faction_visitor",
            AppearedAtTier = (int)haven.Tier,
            AppearedTick = state.Tick
        });
    }
}
