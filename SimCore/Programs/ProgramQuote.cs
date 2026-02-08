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

                RequireOnlyKeys(cons, new[] { "MarketExists","HasEnoughCreditsNow","HasEnoughSupplyNow","HasEnoughCargoNow" });
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
