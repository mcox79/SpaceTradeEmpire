using System;
using SimCore.Systems;

namespace SimCore.Commands;

public sealed class LoadCargoCommand : ICommand
{
    public string FleetId { get; }
    public string MarketId { get; }
    public string GoodId { get; }
    public int Quantity { get; }

    public LoadCargoCommand(string fleetId, string marketId, string goodId, int quantity)
    {
        FleetId = fleetId ?? "";
        MarketId = marketId ?? "";
        GoodId = goodId ?? "";
        Quantity = quantity;
    }

    public void Execute(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (Quantity <= 0) return;
        if (string.IsNullOrWhiteSpace(FleetId)) return;
        if (string.IsNullOrWhiteSpace(MarketId)) return;
        if (string.IsNullOrWhiteSpace(GoodId)) return;

        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;
        if (!state.Markets.TryGetValue(MarketId, out var market)) return;

        // Clamp to available market inventory.
        var available = market.Inventory.TryGetValue(GoodId, out var v) ? v : 0;
        if (available <= 0) return;

        var qty = Math.Min(available, Quantity);
        if (qty <= 0) return;

        if (!InventoryLedger.TryRemoveMarket(market.Inventory, GoodId, qty)) return;
        InventoryLedger.AddCargo(fleet.Cargo, GoodId, qty);
    }
}
