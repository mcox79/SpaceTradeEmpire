namespace SimCore.Entities;

// GATE.S7.TERRITORY.EMBARGO_MODEL.001: Embargo entity.
// Factions at war embargo enemy key goods from the pentagon ring.
public class EmbargoState
{
    public string Id { get; set; } = "";
    public string EnforcingFactionId { get; set; } = "";
    public string TargetFactionId { get; set; } = "";
    public string GoodId { get; set; } = "";
    public string WarfrontId { get; set; } = "";
}
