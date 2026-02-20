using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.Events;

/// <summary>
/// Schema-bound, deterministic fleet event stream.
/// Stored in SimState for debugging, UI explain surfaces, and determinism validation.
/// </summary>
public static class FleetEvents
{
    public const int EventsVersion = 1;

    public enum FleetEventType
    {
        Unknown = 0,
        RouteChoice = 1
    }

    public sealed class Event
    {
        [JsonInclude] public int Version { get; set; } = EventsVersion;

        // Deterministic ordering key inside a tick.
        [JsonInclude] public long Seq { get; set; } = 0;

        // Non-serialized emission order (used to preserve within-fleet ordering
        // before Seq is finalized for the tick).
        [JsonIgnore] public long EmitOrder { get; set; } = 0;

        // Tick when event was emitted.
        [JsonInclude] public int Tick { get; set; } = 0;

        [JsonInclude] public FleetEventType Type { get; set; } = FleetEventType.Unknown;

        // Who
        [JsonInclude] public string FleetId { get; set; } = "";

        // Role encoded as numeric enum for stable schema.
        [JsonInclude] public int Role { get; set; } = 0;

        // What (route-choice payload)
        [JsonInclude] public string ChosenRouteId { get; set; } = "";
        [JsonInclude] public int ProfitScore { get; set; } = 0;
        [JsonInclude] public int CapacityScore { get; set; } = 0;
        [JsonInclude] public int RiskScore { get; set; } = 0;

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
            throw new InvalidOperationException("FleetEvents payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Events" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Events", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Events").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Each fleet event must be a JSON object.");

            RequireOnlyKeys(item, new[]
            {
                "Version","Seq","Tick","Type","FleetId","Role",
                "ChosenRouteId","ProfitScore","CapacityScore","RiskScore","Note"
            });

            RequireKey(item, "Version", JsonValueKind.Number);
            RequireKey(item, "Seq", JsonValueKind.Number);
            RequireKey(item, "Tick", JsonValueKind.Number);
            RequireKey(item, "Type", JsonValueKind.Number);

            RequireKey(item, "FleetId", JsonValueKind.String);
            RequireKey(item, "Role", JsonValueKind.Number);

            RequireKey(item, "ChosenRouteId", JsonValueKind.String);
            RequireKey(item, "ProfitScore", JsonValueKind.Number);
            RequireKey(item, "CapacityScore", JsonValueKind.Number);
            RequireKey(item, "RiskScore", JsonValueKind.Number);

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
