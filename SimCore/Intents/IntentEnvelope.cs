using System.Text.Json.Serialization;

namespace SimCore.Intents;

/// <summary>
/// Deterministic wrapper for intents.
/// Seq is assigned in SimState and provides stable ordering for same-tick submits.
/// </summary>
public sealed class IntentEnvelope
{
	[JsonInclude] public long Seq { get; set; } = 0;

	[JsonInclude] public int CreatedTick { get; set; } = 0;

	// For debugging and future serialization routing
	[JsonInclude] public string Kind { get; set; } = "";

	// In Slice 1, we keep the intent object in memory.
	// Save/load determinism will be handled later (GATE.SAVE.001).
	[JsonIgnore] public IIntent Intent { get; set; } = default!;
}
