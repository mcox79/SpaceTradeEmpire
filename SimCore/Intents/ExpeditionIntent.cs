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
    // Schema-bound intent kind token (stable).
    public const string KindToken = ProgramKind.ExpeditionV0;

    public string Kind => KindToken;

    // Contract fields v0
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
        // Single-mutation pipeline: intent delegates all state decisions to system layer.
        SimCore.Systems.IntelSystem.ApplyExpedition(
            state,
            fleetId: FleetId,
            leadId: LeadId,
            kind: ExpeditionKind,
            applyTick: ApplyTick);
    }
}
