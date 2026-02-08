using System;
using System.Text.Json.Serialization;
using SimCore.Entities;

namespace SimCore.Programs;

/// <summary>
/// Deterministic snapshot for quote generation.
/// Rule: quote logic must depend ONLY on this snapshot + request.
/// </summary>
public static class ProgramQuoteSnapshot
{
        public const int SnapshotVersion = 1;

        public sealed class Snapshot
        {
                [JsonInclude] public int Version { get; set; } = SnapshotVersion;
                [JsonInclude] public int Tick { get; set; } = 0;

                [JsonInclude] public ProgramSlice Program { get; set; } = new();
                [JsonInclude] public MarketSlice Market { get; set; } = new();
                [JsonInclude] public PlayerSlice Player { get; set; } = new();
        }

        public sealed class ProgramSlice
        {
                [JsonInclude] public string ProgramId { get; set; } = "";
                [JsonInclude] public string Kind { get; set; } = "";
                [JsonInclude] public string MarketId { get; set; } = "";
                [JsonInclude] public string GoodId { get; set; } = "";
                [JsonInclude] public int Quantity { get; set; } = 0;
                [JsonInclude] public int CadenceTicks { get; set; } = 1;
        }

        public sealed class MarketSlice
        {
                [JsonInclude] public string MarketId { get; set; } = "";
                [JsonInclude] public string GoodId { get; set; } = "";

                [JsonInclude] public int StockUnits { get; set; } = 0;

                // Prices computed at snapshot time using the same pricing model as commands.
                [JsonInclude] public int MidPrice { get; set; } = 0;
                [JsonInclude] public int BuyPrice { get; set; } = 0;
                [JsonInclude] public int SellPrice { get; set; } = 0;
        }

        public sealed class PlayerSlice
        {
                [JsonInclude] public long Credits { get; set; } = 0;
                [JsonInclude] public int CargoUnits { get; set; } = 0; // of GoodId for SELL quotes
        }

        public static Snapshot Capture(SimState state, string programId)
        {
                if (state is null) throw new ArgumentNullException(nameof(state));
                if (string.IsNullOrWhiteSpace(programId)) throw new ArgumentException("programId must be non-empty.", nameof(programId));
                if (state.Programs is null) throw new InvalidOperationException("Programs book missing.");

                if (!state.Programs.Instances.TryGetValue(programId, out var p) || p is null)
                        throw new InvalidOperationException($"Program not found: {programId}");

                var snap = new Snapshot
                {
                        Version = SnapshotVersion,
                        Tick = state.Tick,
                        Program = new ProgramSlice
                        {
                                ProgramId = p.Id ?? "",
                                Kind = p.Kind ?? "",
                                MarketId = p.MarketId ?? "",
                                GoodId = p.GoodId ?? "",
                                Quantity = p.Quantity,
                                CadenceTicks = p.CadenceTicks <= 0 ? 1 : p.CadenceTicks
                        },
                        Market = new MarketSlice
                        {
                                MarketId = p.MarketId ?? "",
                                GoodId = p.GoodId ?? ""
                        },
                        Player = new PlayerSlice
                        {
                                Credits = state.PlayerCredits,
                                CargoUnits = state.PlayerCargo.TryGetValue(p.GoodId ?? "", out var have) ? have : 0
                        }
                };

                if (!state.Markets.TryGetValue(snap.Market.MarketId, out var market) || market is null)
                {
                        // Market missing: prices stay 0, stock stays 0. Quote will surface constraints.
                        return snap;
                }

                var goodId = snap.Market.GoodId;
                var stock = market.Inventory.TryGetValue(goodId, out var v) ? v : 0;

                snap.Market.StockUnits = stock;
                snap.Market.MidPrice = market.GetMidPrice(goodId);
                snap.Market.BuyPrice = market.GetBuyPrice(goodId);
                snap.Market.SellPrice = market.GetSellPrice(goodId);

                return snap;
        }
}
