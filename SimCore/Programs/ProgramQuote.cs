using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimCore.Programs;

/// <summary>
/// Deterministic, schema-bound quote for a program.
/// Input: request + ProgramQuoteSnapshot.Snapshot
/// Output: Quote payload
/// </summary>
public static class ProgramQuote
{
    public const int QuoteVersion = 1;

    public static class RiskToken
    {
        public const string PriceMovesWithInventory = "PRICE_MOVES_WITH_INVENTORY";
        public const string FillNotGuaranteed = "FILL_NOT_GUARANTEED";
        public const string CadenceMayOverrunBudget = "CADENCE_MAY_OVERRUN_BUDGET";
    }

    // GATE.S3_6.EXPLOITATION_PACKAGES.001: stable risk tokens for exploitation quotes.
    // TopRisks sorted: magnitude desc, then token Ordinal asc (tie-break).
    public static class ExploitationRiskToken
    {
        public const string NoRoutingService = "NO_ROUTING_SERVICE";
        public const string LowExpectedThroughput = "LOW_EXPECTED_THROUGHPUT";
        public const string HighCapitalLockup = "HIGH_CAPITAL_LOCKUP";
        public const string FrontierAccessRequired = "FRONTIER_ACCESS_REQUIRED";
    }

    // GATE.S3_6.EXPLOITATION_PACKAGES.001: stable intervention verb tokens.
    // SuggestedMitigations sorted: verb Ordinal asc.
    public static class ExploitationMitigationVerb
    {
        public const string AssignRoutingFleet = "ASSIGN_ROUTING_FLEET";
        public const string IncreaseExtractionBudget = "INCREASE_EXTRACTION_BUDGET";
        public const string InsureShipment = "INSURE_SHIPMENT";
        public const string PausePackage = "PAUSE_PACKAGE";
        public const string RerouteAroundChokepoint = "REROUTE_AROUND_CHOKEPOINT";
        public const string SubstituteInputGood = "SUBSTITUTE_INPUT_GOOD";
    }

    // GATE.S3_6.EXPLOITATION_PACKAGES.001: deterministic quote payload for TRADE_CHARTER_V0 / RESOURCE_TAP_V0.
    public sealed class ExploitationQuote
    {
        [JsonInclude] public int Version { get; set; } = QuoteVersion;
        [JsonInclude] public int QuoteTick { get; set; } = 0;
        [JsonInclude] public string ProgramKind { get; set; } = "";
        [JsonInclude] public string ScopeId { get; set; } = "";

        // Cost model (all in credits; rates per game day = 1440 ticks per canonical doc 21).
        [JsonInclude] public long UpfrontCost { get; set; } = 0;
        [JsonInclude] public long OngoingCostPerDay { get; set; } = 0;
        [JsonInclude] public int TimeToActivate { get; set; } = 0; // in ticks

        // KPI bands (p10/p50/p90 credits per day).
        [JsonInclude] public long ExpectedOutputBands_p10 { get; set; } = 0;
        [JsonInclude] public long ExpectedOutputBands_p50 { get; set; } = 0;
        [JsonInclude] public long ExpectedOutputBands_p90 { get; set; } = 0;

        // TopRisks: ordered magnitude desc, token Ordinal asc as tie-break.
        [JsonInclude] public List<ExploitationRisk> TopRisks { get; set; } = new();

        // SuggestedMitigations: verb tokens sorted Ordinal asc.
        [JsonInclude] public List<string> SuggestedMitigations { get; set; } = new();
    }

    public sealed class ExploitationRisk
    {
        [JsonInclude] public string Token { get; set; } = "";
        [JsonInclude] public int Magnitude { get; set; } = 0; // 0-100 per canonical risk scale
    }

    /// <summary>
    /// Build a deterministic ExploitationQuote from explicit inputs.
    /// Ordering contract: TopRisks magnitude desc then token Ordinal asc; SuggestedMitigations verb Ordinal asc.
    /// </summary>
    public static ExploitationQuote BuildExploitationQuote(
            int quoteTick,
            string programKind,
            string scopeId,
            long upfrontCost,
            long ongoingCostPerDay,
            int timeToActivateTicks,
            long p10,
            long p50,
            long p90,
            List<ExploitationRisk> risks,
            List<string> mitigationVerbs)
    {
        if (risks is null) throw new ArgumentNullException(nameof(risks));
        if (mitigationVerbs is null) throw new ArgumentNullException(nameof(mitigationVerbs));

        // Deterministic sort: magnitude desc, then token Ordinal asc.
        var sortedRisks = risks
                .OrderByDescending(r => r.Magnitude)
                .ThenBy(r => r.Token, StringComparer.Ordinal)
                .ToList();

        // Deterministic sort: verb Ordinal asc.
        var sortedVerbs = mitigationVerbs
                .OrderBy(v => v, StringComparer.Ordinal)
                .ToList();

        return new ExploitationQuote
        {
            Version = QuoteVersion,
            QuoteTick = quoteTick,
            ProgramKind = programKind ?? "",
            ScopeId = scopeId ?? "",
            UpfrontCost = upfrontCost,
            OngoingCostPerDay = ongoingCostPerDay,
            TimeToActivate = timeToActivateTicks,
            ExpectedOutputBands_p10 = p10,
            ExpectedOutputBands_p50 = p50,
            ExpectedOutputBands_p90 = p90,
            TopRisks = sortedRisks,
            SuggestedMitigations = sortedVerbs
        };
    }

    public static string ToDeterministicJson(ExploitationQuote quote)
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        return JsonSerializer.Serialize(quote, opts);
    }

    public static void ValidateExploitationJsonIsSchemaBound(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("ExploitationQuote payload must be a JSON object.");

        RequireOnlyKeys(root, new[]
        {
                        "Version","QuoteTick","ProgramKind","ScopeId",
                        "UpfrontCost","OngoingCostPerDay","TimeToActivate",
                        "ExpectedOutputBands_p10","ExpectedOutputBands_p50","ExpectedOutputBands_p90",
                        "TopRisks","SuggestedMitigations"
                });

        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "QuoteTick", JsonValueKind.Number);
        RequireKey(root, "ProgramKind", JsonValueKind.String);
        RequireKey(root, "ScopeId", JsonValueKind.String);
        RequireKey(root, "UpfrontCost", JsonValueKind.Number);
        RequireKey(root, "OngoingCostPerDay", JsonValueKind.Number);
        RequireKey(root, "TimeToActivate", JsonValueKind.Number);
        RequireKey(root, "ExpectedOutputBands_p10", JsonValueKind.Number);
        RequireKey(root, "ExpectedOutputBands_p50", JsonValueKind.Number);
        RequireKey(root, "ExpectedOutputBands_p90", JsonValueKind.Number);
        RequireKey(root, "TopRisks", JsonValueKind.Array);
        RequireKey(root, "SuggestedMitigations", JsonValueKind.Array);

        foreach (var item in root.GetProperty("TopRisks").EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Each TopRisk must be an object.");
            RequireOnlyKeys(item, new[] { "Token", "Magnitude" });
            RequireKey(item, "Token", JsonValueKind.String);
            RequireKey(item, "Magnitude", JsonValueKind.Number);
        }

        foreach (var v in root.GetProperty("SuggestedMitigations").EnumerateArray())
        {
            if (v.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("SuggestedMitigations must be array of strings.");
        }
    }

    public sealed class Payload
    {
        [JsonInclude] public int Version { get; set; } = QuoteVersion;

        [JsonInclude] public int QuoteTick { get; set; } = 0;

        [JsonInclude] public string ProgramId { get; set; } = "";
        [JsonInclude] public string Kind { get; set; } = "";

        [JsonInclude] public string MarketId { get; set; } = "";
        [JsonInclude] public string GoodId { get; set; } = "";
        [JsonInclude] public int Quantity { get; set; } = 0;
        [JsonInclude] public int CadenceTicks { get; set; } = 1;

        // Pricing now (from snapshot)
        [JsonInclude] public int UnitPriceNow { get; set; } = 0;
        [JsonInclude] public long EstCostOrValuePerRun { get; set; } = 0; // BUY cost, SELL value
        [JsonInclude] public long EstRunsPerDay { get; set; } = 0;
        [JsonInclude] public long EstDailyCostOrValue { get; set; } = 0;

        [JsonInclude] public ConstraintsPayload Constraints { get; set; } = new();

        [JsonInclude] public List<string> Risks { get; set; } = new();
    }

    public sealed class ConstraintsPayload
    {
        [JsonInclude] public bool MarketExists { get; set; } = false;

        // For BUY
        [JsonInclude] public bool HasEnoughCreditsNow { get; set; } = false;
        [JsonInclude] public bool HasEnoughSupplyNow { get; set; } = false;

        // For SELL
        [JsonInclude] public bool HasEnoughCargoNow { get; set; } = false;
    }

    public static Payload BuildFromSnapshot(ProgramQuoteSnapshot.Snapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var p = snapshot.Program;
        var m = snapshot.Market;
        var pl = snapshot.Player;

        var cadence = p.CadenceTicks <= 0 ? 1 : p.CadenceTicks;
        long runsPerDay = 1440L / (long)cadence;

        int unitPriceNow = 0;

        if (string.Equals(p.Kind, ProgramKind.AutoBuy, StringComparison.Ordinal))
            unitPriceNow = m.BuyPrice;
        else if (string.Equals(p.Kind, ProgramKind.AutoSell, StringComparison.Ordinal))
            unitPriceNow = m.SellPrice;

        long perRun = (long)unitPriceNow * (long)p.Quantity;
        long perDay = perRun * runsPerDay;

        var constraints = new ConstraintsPayload
        {
            MarketExists = !string.IsNullOrWhiteSpace(m.MarketId) && unitPriceNow > 0
        };

        if (string.Equals(p.Kind, ProgramKind.AutoBuy, StringComparison.Ordinal))
        {
            constraints.HasEnoughCreditsNow = pl.Credits >= perRun;
            constraints.HasEnoughSupplyNow = m.StockUnits >= p.Quantity;
        }
        else if (string.Equals(p.Kind, ProgramKind.AutoSell, StringComparison.Ordinal))
        {
            constraints.HasEnoughCargoNow = pl.CargoUnits >= p.Quantity;
        }

        var risks = new List<string>
                {
                        RiskToken.PriceMovesWithInventory,
                        RiskToken.FillNotGuaranteed
                };

        // Simple deterministic heuristic: if BUY and cadence implies many runs, warn about budget overrun.
        if (string.Equals(p.Kind, ProgramKind.AutoBuy, StringComparison.Ordinal) && runsPerDay >= 100)
        {
            risks.Add(RiskToken.CadenceMayOverrunBudget);
        }

        // Sort risks for determinism.
        risks = risks.OrderBy(x => x, StringComparer.Ordinal).ToList();

        return new Payload
        {
            Version = QuoteVersion,
            QuoteTick = snapshot.Tick,

            ProgramId = p.ProgramId ?? "",
            Kind = p.Kind ?? "",

            MarketId = p.MarketId ?? "",
            GoodId = p.GoodId ?? "",
            Quantity = p.Quantity,
            CadenceTicks = cadence,

            UnitPriceNow = unitPriceNow,
            EstCostOrValuePerRun = perRun,
            EstRunsPerDay = runsPerDay,
            EstDailyCostOrValue = perDay,

            Constraints = constraints,
            Risks = risks
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

        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("ProgramQuote payload must be a JSON object.");

        RequireOnlyKeys(root, new[]
        {
                        "Version","QuoteTick","ProgramId","Kind","MarketId","GoodId","Quantity","CadenceTicks",
                        "UnitPriceNow","EstCostOrValuePerRun","EstRunsPerDay","EstDailyCostOrValue",
                        "Constraints","Risks"
                });

        RequireKey(root, "Version", JsonValueKind.Number);
        RequireKey(root, "QuoteTick", JsonValueKind.Number);
        RequireKey(root, "ProgramId", JsonValueKind.String);
        RequireKey(root, "Kind", JsonValueKind.String);
        RequireKey(root, "MarketId", JsonValueKind.String);
        RequireKey(root, "GoodId", JsonValueKind.String);
        RequireKey(root, "Quantity", JsonValueKind.Number);
        RequireKey(root, "CadenceTicks", JsonValueKind.Number);

        RequireKey(root, "UnitPriceNow", JsonValueKind.Number);
        RequireKey(root, "EstCostOrValuePerRun", JsonValueKind.Number);
        RequireKey(root, "EstRunsPerDay", JsonValueKind.Number);
        RequireKey(root, "EstDailyCostOrValue", JsonValueKind.Number);

        RequireKey(root, "Risks", JsonValueKind.Array);

        var cons = root.GetProperty("Constraints");
        if (cons.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Constraints must be an object.");

        RequireOnlyKeys(cons, new[] { "MarketExists", "HasEnoughCreditsNow", "HasEnoughSupplyNow", "HasEnoughCargoNow" });
        RequireBoolKey(cons, "MarketExists");
        RequireBoolKey(cons, "HasEnoughCreditsNow");

        RequireBoolKey(cons, "HasEnoughSupplyNow");
        RequireBoolKey(cons, "HasEnoughCargoNow");

        foreach (var r in root.GetProperty("Risks").EnumerateArray())
        {
            if (r.ValueKind != JsonValueKind.String) throw new InvalidOperationException("Risks must be array of strings.");
        }
    }

    private static void RequireKey(JsonElement obj, string name, JsonValueKind kind)
    {
        if (!obj.TryGetProperty(name, out var prop)) throw new InvalidOperationException($"Missing key: {name}");
        if (prop.ValueKind != kind) throw new InvalidOperationException($"Key {name} must be {kind}.");
    }

    private static void RequireBoolKey(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var prop)) throw new InvalidOperationException($"Missing key: {name}");
        if (prop.ValueKind != JsonValueKind.True && prop.ValueKind != JsonValueKind.False)
            throw new InvalidOperationException($"Key {name} must be boolean.");
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
