using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.Events;

/// <summary>
/// Schema-bound, deterministic logistics event stream.
/// Stored in SimState for debugging, UI explain surfaces, and determinism validation.
/// </summary>
public static class LogisticsEvents
{
    public const int EventsVersion = 1;

    public enum LogisticsEventType
    {
        Unknown = 0,

        // Job lifecycle
        JobPlanned = 1,
        PhaseChangedToDeliver = 2,
        JobCompleted = 3,
        JobCanceled = 4,

        // Authority / control (manual override)
        ManualOverrideSet = 5,

        // Actions issued (intents enqueued)
        PickupIssued = 10,
        DeliveryIssued = 11,

        // Slice 3 / GATE.LOGI.RESERVE.001
        ReservationCreated = 12,
        ReservationReleased = 13
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

        [JsonInclude] public LogisticsEventType Type { get; set; } = LogisticsEventType.Unknown;

        // Who
        [JsonInclude] public string FleetId { get; set; } = "";

        // What
        [JsonInclude] public string GoodId { get; set; } = "";
        [JsonInclude] public int Amount { get; set; } = 0;

        // Where
        [JsonInclude] public string SourceNodeId { get; set; } = "";
        [JsonInclude] public string TargetNodeId { get; set; } = "";
        [JsonInclude] public string SourceMarketId { get; set; } = "";
        [JsonInclude] public string TargetMarketId { get; set; } = "";

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
            throw new InvalidOperationException("LogisticsEvents payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Events" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Events", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Events").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Each logistics event must be a JSON object.");

            RequireOnlyKeys(item, new[]
            {
                "Version","Seq","Tick","Type","FleetId","GoodId","Amount",
                "SourceNodeId","TargetNodeId","SourceMarketId","TargetMarketId","Note"
            });

            RequireKey(item, "Version", JsonValueKind.Number);
            RequireKey(item, "Seq", JsonValueKind.Number);
            RequireKey(item, "Tick", JsonValueKind.Number);
            RequireKey(item, "Type", JsonValueKind.Number);

            RequireKey(item, "FleetId", JsonValueKind.String);
            RequireKey(item, "GoodId", JsonValueKind.String);

            RequireKey(item, "Amount", JsonValueKind.Number);

            RequireKey(item, "SourceNodeId", JsonValueKind.String);
            RequireKey(item, "TargetNodeId", JsonValueKind.String);
            RequireKey(item, "SourceMarketId", JsonValueKind.String);
            RequireKey(item, "TargetMarketId", JsonValueKind.String);

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
