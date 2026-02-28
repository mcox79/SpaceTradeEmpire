using SimCore.Programs;

namespace SimCore.Intents;

// GATE.S3_6.EXPEDITION_PROGRAMS.001: ExpeditionProgram contract v0.
// Deterministic: rejection uses stable ReasonCode tokens; Apply uses dictionary lookup only.

/// <summary>
/// Expedition kind v0: the four canonical expedition activities.
/// </summary>
public enum ExpeditionKind
{
    Survey,
    Sample,
    Salvage,
    Analyze
}

/// <summary>
/// Intent v0 for dispatching an expedition fleet to a known IntelBook lead entry.
/// Targets a LeadId in IntelBook; rejection returns SiteNotFound on unknown LeadId.
/// </summary>
public sealed class ExpeditionIntentV0 : IIntent
{
    public const string KindToken = "EXPEDITION_V0";

    public string Kind => KindToken;

    public string LeadId { get; }
    public ExpeditionKind ExpeditionKind { get; }
    public string FleetId { get; }
    public int ApplyTick { get; }

    public ExpeditionIntentV0(string leadId, ExpeditionKind expeditionKind, string fleetId, int applyTick)
    {
        LeadId = leadId ?? "";
        ExpeditionKind = expeditionKind;
        FleetId = fleetId ?? "";
        ApplyTick = applyTick;
    }

    public void Apply(SimState state)
    {
        // Deterministic: single dictionary lookup for rejection; no iteration, no wall-clock.
        if (state.Intel == null || !state.Intel.Discoveries.ContainsKey(LeadId))
        {
            // Emit schema-bound rejection reason. No mutation of state on rejection.
            state.LastExpeditionRejectReason = ProgramExplain.ReasonCodes.SiteNotFound;
            return;
        }

        // v0: record intent as accepted; full execution pipeline is a later gate.
        state.LastExpeditionRejectReason = null;
        state.LastExpeditionAcceptedLeadId = LeadId;
        state.LastExpeditionAcceptedKind = ExpeditionKind.ToString();
    }
}
