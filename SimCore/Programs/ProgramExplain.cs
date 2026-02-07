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
        [JsonInclude] public string MarketId { get; set; } = "";
        [JsonInclude] public string GoodId { get; set; } = "";
        [JsonInclude] public int Quantity { get; set; } = 0;
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
