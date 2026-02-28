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

    // GATE.S3_6.EXPEDITION_PROGRAMS.001: stable rejection reason codes for expedition intents.
    // GATE.S3_6.EXPLOITATION_PACKAGES.001: exploitation package rejection reason codes.
    public static class ReasonCodes
    {
        public const string SiteNotFound = "SiteNotFound";
        public const string InsufficientExpeditionCapacity = "InsufficientExpeditionCapacity";
        public const string MissingSiteBlueprintUnlock = "MissingSiteBlueprintUnlock";

        // GATE.S3_6.EXPLOITATION_PACKAGES.001
        public const string ServiceUnavailable = "ServiceUnavailable";
        public const string InsufficientCapacity = "InsufficientCapacity";
        public const string NoExportRoute = "NoExportRoute";
        public const string BudgetExhausted = "BudgetExhausted";

        // GATE.S3_6.RUMOR_INTEL_MIN.001
        public const string LeadBlocked = "LeadBlocked";
        public const string LeadMissingHint = "LeadMissingHint";
    }

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

    // --- Unlock explainability v0 (GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.005) ---
    // Schema-bound explanation payload for unlock gained%blocked outcomes and suggested interventions.
    // NOTE: Version uses ExplainVersion to avoid introducing new numeric literals in SimCore.
    public sealed class UnlockPayload
    {
        [JsonInclude] public int Version { get; set; } = ExplainVersion;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public List<UnlockEntry> Unlocks { get; set; } = new();
    }

    public sealed class UnlockEntry
    {
        [JsonInclude] public string UnlockId { get; set; } = "";

        // Schema-bound tokens (no free-text).
        [JsonInclude] public string AcquireReasonCode { get; set; } = "";

        // 1 to 3 intervention verbs (tokens) in stable order.
        [JsonInclude] public List<string> Actions { get; set; } = new();

        // Optional compact explain chain as tokens, already in stable order.
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

    public static string ToDeterministicJson(UnlockPayload payload)
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

    public static void ValidateUnlockJsonIsSchemaBound(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ProgramExplain unlock payload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Unlocks" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Unlocks", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Unlocks").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Each unlock entry must be an object.");

            RequireOnlyKeys(item, new[]
            {
                "UnlockId","AcquireReasonCode","Actions","ExplainChain"
            });

            RequireKey(item, "UnlockId", JsonValueKind.String);
            RequireKey(item, "AcquireReasonCode", JsonValueKind.String);
            RequireKey(item, "Actions", JsonValueKind.Array);
            RequireKey(item, "ExplainChain", JsonValueKind.Array);
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

    // --- Exploitation package explainability v0 (GATE.S3_6.EXPLOITATION_PACKAGES.003) ---
    // Schema-bound explain snapshot for exploitation packages (TradeCharter, ResourceTap, etc.).
    // ExplainChain ordering contract: primary entries first (IsPrimary=true), then secondary, both groups Ordinal asc on Token.
    // InterventionVerbs ordering contract: Ordinal asc.
    // ExceptionPolicyLevers: token list, Ordinal asc.
    public sealed class ExploitationPackagePayload
    {
        [JsonInclude] public int Version { get; set; } = ExplainVersion;
        [JsonInclude] public int Tick { get; set; } = 0;
        [JsonInclude] public List<ExploitationPackageEntry> Packages { get; set; } = new();
    }

    public sealed class ExploitationPackageEntry
    {
        [JsonInclude] public string PackageId { get; set; } = "";
        [JsonInclude] public string Status { get; set; } = "";

        // Ordered: primary entries first, then secondary, both groups Ordinal asc on Token.
        [JsonInclude] public List<ExplainChainToken> ExplainChain { get; set; } = new();

        // Intervention verbs: Ordinal asc.
        [JsonInclude] public List<string> InterventionVerbs { get; set; } = new();

        // Exception policy levers: token list, Ordinal asc.
        [JsonInclude] public List<string> ExceptionPolicyLevers { get; set; } = new();
    }

    public sealed class ExplainChainToken
    {
        [JsonInclude] public string Token { get; set; } = "";
        [JsonInclude] public bool IsPrimary { get; set; } = false;
    }

    public static string ToDeterministicJson(ExploitationPackagePayload payload)
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        return JsonSerializer.Serialize(payload, opts);
    }

    public static void ValidateExploitationPackageJsonIsSchemaBound(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ExploitationPackagePayload must be a JSON object.");

        RequireOnlyKeys(root, new[] { "Version", "Tick", "Packages" });
        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "Tick", JsonValueKind.Number);
        RequireKey(root, "Packages", JsonValueKind.Array);

        foreach (var item in root.GetProperty("Packages").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Each package entry must be an object.");
            RequireOnlyKeys(item, new[] { "PackageId", "Status", "ExplainChain", "InterventionVerbs", "ExceptionPolicyLevers" });
            RequireKey(item, "PackageId", JsonValueKind.String);
            RequireKey(item, "Status", JsonValueKind.String);
            RequireKey(item, "ExplainChain", JsonValueKind.Array);
            RequireKey(item, "InterventionVerbs", JsonValueKind.Array);
            RequireKey(item, "ExceptionPolicyLevers", JsonValueKind.Array);

            foreach (var chain in item.GetProperty("ExplainChain").EnumerateArray())
            {
                if (chain.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ExplainChain entry must be an object.");
                RequireOnlyKeys(chain, new[] { "Token", "IsPrimary" });
                RequireKey(chain, "Token", JsonValueKind.String);
                RequireKey(chain, "IsPrimary", JsonValueKind.True == chain.GetProperty("IsPrimary").ValueKind ? JsonValueKind.True : JsonValueKind.False);
            }
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
