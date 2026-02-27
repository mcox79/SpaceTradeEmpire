using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.Programs;

/// <summary>
/// Schema-bound explanation payload for program UI/intel.
/// </summary>
public static class ProgramExplain
{
    public const int ExplainVersion = 1;
    public const int EventVersion = 1;

    public sealed class Payload
    {
        [JsonInclude] public int Version { get; set; } = ExplainVersion;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public List<Entry> Programs { get; set; } = new();
    }

    public sealed class Entry
    {
        [JsonInclude] public string Id { get; set; } = "";
        [JsonInclude] public string Kind { get; set; } = "";
        [JsonInclude] public string Status { get; set; } = "";
        [JsonInclude] public int CadenceTicks { get; set; } = 0;
        [JsonInclude] public int NextRunTick { get; set; } = 0;
        [JsonInclude] public int LastRunTick { get; set; } = -1;

        // For AUTO_BUY
        // For AUTO_BUY
        [JsonInclude] public string MarketId { get; set; } = "";
        [JsonInclude] public string GoodId { get; set; } = "";
        [JsonInclude] public int Quantity { get; set; } = 0;
    }

    // --- Discovery explainability v0 (GATE.S3_6.DISCOVERY_STATE.007) ---
    // Schema-bound explanation payload for discovery scan%analyze blockers and suggested interventions.
    // NOTE: Version uses ExplainVersion to avoid introducing new numeric literals in SimCore.
    public sealed class DiscoveryPayload
    {
        [JsonInclude] public int Version { get; set; } = ExplainVersion;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public List<DiscoveryEntry> Discoveries { get; set; } = new();
    }

    public sealed class DiscoveryEntry
    {
        [JsonInclude] public string DiscoveryId { get; set; } = "";

        // Schema-bound tokens (no free-text).
        [JsonInclude] public string ScanReasonCode { get; set; } = "";
        [JsonInclude] public string AnalyzeReasonCode { get; set; } = "";

        // Intervention verbs (tokens) in stable order.
        [JsonInclude] public List<string> ScanActions { get; set; } = new();
        [JsonInclude] public List<string> AnalyzeActions { get; set; } = new();

        // Optional compact explain chain as "phase:reason" segments, already in stable order.
        [JsonInclude] public List<string> ExplainChain { get; set; } = new();
    }

    public sealed class EventPayload
    {
        [JsonInclude] public int Version { get; set; } = EventVersion;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public List<EventEntry> Events { get; set; } = new();
    }

    public sealed class EventEntry
    {
        [JsonInclude] public int Version { get; set; } = EventVersion;
        [JsonInclude] public long Seq { get; set; } = 0;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public int Type { get; set; } = 0;
        [JsonInclude] public string ProgramId { get; set; } = "";
        [JsonInclude] public string MarketId { get; set; } = "";
        [JsonInclude] public string GoodId { get; set; } = "";
        [JsonInclude] public string Note { get; set; } = "";
    }

    public static Payload Build(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        var payload = new Payload
        {
            Version = ExplainVersion,
            Tick = state.Tick,
            Programs = new List<Entry>()
        };

        if (state.Programs is null || state.Programs.Instances.Count == 0) return payload;

        foreach (var kv in state.Programs.Instances.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var p = kv.Value;
            if (p is null) continue;

            payload.Programs.Add(new Entry
            {
                Id = p.Id,
                Kind = p.Kind,
                Status = p.Status.ToString(),
                CadenceTicks = p.CadenceTicks,
                NextRunTick = p.NextRunTick,
                LastRunTick = p.LastRunTick,
                MarketId = p.MarketId ?? "",
                GoodId = p.GoodId ?? "",
                Quantity = p.Quantity
            });
        }

        return payload;
    }

    public static string ToDeterministicJson(Payload payload)
    {
        // Determinism rule: stable property order as emitted by System.Text.Json for POCOs in declaration order.
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        return JsonSerializer.Serialize(payload, opts);
    }

    public static string ToDeterministicJson(EventPayload payload)
    {
        // Determinism rule: stable property order as emitted by System.Text.Json for POCOs in declaration order.
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
        // Minimal validator: required fields exist, and no unknown fields at top-level or program-entry level.
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ProgramExplain payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Programs" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Programs", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Programs").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Each program entry must be an object.");

            RequireOnlyKeys(item, new[]
            {
                "Id","Kind","Status","CadenceTicks","NextRunTick","LastRunTick","MarketId","GoodId","Quantity"
            });

            RequireKey(item, "Id", JsonValueKind.String);
            RequireKey(item, "Kind", JsonValueKind.String);
            RequireKey(item, "Status", JsonValueKind.String);
            RequireKey(item, "CadenceTicks", JsonValueKind.Number);
            RequireKey(item, "NextRunTick", JsonValueKind.Number);
            RequireKey(item, "LastRunTick", JsonValueKind.Number);
            RequireKey(item, "MarketId", JsonValueKind.String);
            RequireKey(item, "GoodId", JsonValueKind.String);
            RequireKey(item, "Quantity", JsonValueKind.Number);
        }
    }

    public static void ValidateEventJsonIsSchemaBound(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ProgramExplain event payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Events" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Events", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Events").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Each program event entry must be an object.");

            RequireOnlyKeys(item, new[]
            {
                "Version","Seq","Tick","Type","ProgramId","MarketId","GoodId","Note"
            });

            RequireKey(item, "Version", JsonValueKind.Number);
            RequireKey(item, "Seq", JsonValueKind.Number);
            RequireKey(item, "Tick", JsonValueKind.Number);
            RequireKey(item, "Type", JsonValueKind.Number);
            RequireKey(item, "ProgramId", JsonValueKind.String);
            RequireKey(item, "MarketId", JsonValueKind.String);
            RequireKey(item, "GoodId", JsonValueKind.String);
            RequireKey(item, "Note", JsonValueKind.String);
        }
    }

    public static void ValidateDiscoveryJsonIsSchemaBound(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ProgramExplain discovery payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Discoveries" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Discoveries", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Discoveries").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Each discovery entry must be an object.");

            RequireOnlyKeys(item, new[]
            {
                "DiscoveryId","ScanReasonCode","AnalyzeReasonCode","ScanActions","AnalyzeActions","ExplainChain"
            });

            RequireKey(item, "DiscoveryId", JsonValueKind.String);
            RequireKey(item, "ScanReasonCode", JsonValueKind.String);
            RequireKey(item, "AnalyzeReasonCode", JsonValueKind.String);
            RequireKey(item, "ScanActions", JsonValueKind.Array);
            RequireKey(item, "AnalyzeActions", JsonValueKind.Array);
            RequireKey(item, "ExplainChain", JsonValueKind.Array);
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
