using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.Events;

/// <summary>
/// Slice 3 / GATE.S3.RISK_MODEL.001
/// Schema-bound, deterministic security incident event stream.
/// Stored in SimState for debugging, UI explain surfaces, and determinism validation.
/// </summary>
public static class SecurityEvents
{
    public const int EventsVersion = 1;

    public enum SecurityEventType
    {
        Unknown = 0,
        Delay = 1,
        Loss = 2,
        Inspection = 3
    }

    public sealed class Event
    {
        [JsonInclude] public int Version { get; set; } = EventsVersion;

        // Deterministic ordering key inside a tick.
        [JsonInclude] public long Seq { get; set; } = 0;

        // Non-serialized emission order (used to preserve within-edge ordering before Seq is finalized).
        [JsonIgnore] public long EmitOrder { get; set; } = 0;

        [JsonInclude] public int Tick { get; set; } = 0;

        [JsonInclude] public SecurityEventType Type { get; set; } = SecurityEventType.Unknown;

        // Lane context
        [JsonInclude] public string EdgeId { get; set; } = "";
        [JsonInclude] public string FromNodeId { get; set; } = "";
        [JsonInclude] public string ToNodeId { get; set; } = "";

        // Derived, deterministic v0 band (0..3).
        [JsonInclude] public int RiskBand { get; set; } = 0;

        // Outcomes (only one is expected to be nonzero for v0).
        [JsonInclude] public int DelayTicks { get; set; } = 0;
        [JsonInclude] public int LossUnits { get; set; } = 0;
        [JsonInclude] public int InspectionTicks { get; set; } = 0;

        // Deterministic, schema-bound cause chain (ASCII-safe).
        [JsonInclude] public string CauseChain { get; set; } = "";

        // Optional explanatory text for UI, not for logic.
        [JsonInclude] public string Note { get; set; } = "";
    }

    public sealed class Payload
    {
        [JsonInclude] public int Version { get; set; } = EventsVersion;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public List<Event> Events { get; set; } = new();
    }

    public static Payload BuildPayload(int tick, IReadOnlyList<Event> events)
    {
        return new Payload
        {
            Version = EventsVersion,
            Tick = tick,
            Events = events is null ? new List<Event>() : events.ToList()
        };
    }

    public static string ToDeterministicJson(Payload payload)
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        return JsonSerializer.Serialize(payload, opts);
    }

    public static void ValidateJsonIsSchemaBound(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("SecurityEvents payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Events" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Events", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Events").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Each security event must be a JSON object.");

            RequireOnlyKeys(item, new[]
            {
                "Version","Seq","Tick","Type",
                "EdgeId","FromNodeId","ToNodeId",
                "RiskBand","DelayTicks","LossUnits","InspectionTicks",
                "CauseChain","Note"
            });

            RequireKey(item, "Version", JsonValueKind.Number);
            RequireKey(item, "Seq", JsonValueKind.Number);
            RequireKey(item, "Tick", JsonValueKind.Number);
            RequireKey(item, "Type", JsonValueKind.Number);

            RequireKey(item, "EdgeId", JsonValueKind.String);
            RequireKey(item, "FromNodeId", JsonValueKind.String);
            RequireKey(item, "ToNodeId", JsonValueKind.String);

            RequireKey(item, "RiskBand", JsonValueKind.Number);
            RequireKey(item, "DelayTicks", JsonValueKind.Number);
            RequireKey(item, "LossUnits", JsonValueKind.Number);
            RequireKey(item, "InspectionTicks", JsonValueKind.Number);

            RequireKey(item, "CauseChain", JsonValueKind.String);
            RequireKey(item, "Note", JsonValueKind.String);
        }
    }

    private static void RequireKey(JsonElement obj, string name, JsonValueKind kind)
    {
        if (!obj.TryGetProperty(name, out var prop)) throw new InvalidOperationException($"Missing key: {name}");
        if (prop.ValueKind != kind) throw new InvalidOperationException($"Key {name} must be {kind}.");
    }

    private static void RequireOnlyKeys(JsonElement obj, string[] allowed)
    {
        var allowedSet = allowed.ToHashSet(StringComparer.Ordinal);
        foreach (var p in obj.EnumerateObject())
        {
            if (!allowedSet.Contains(p.Name)) throw new InvalidOperationException($"Unknown key: {p.Name}");
        }
    }
}
