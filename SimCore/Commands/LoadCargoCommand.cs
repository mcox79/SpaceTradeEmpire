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

        // Clamp to available market inventory, respecting logistics reservations (Slice 3 / GATE.LOGI.RESERVE.001).
        var availableInv = market.Inventory.TryGetValue(GoodId, out var v) ? v : 0;
        if (availableInv <= 0) return;

        // Total reserved remaining (includes this fleet's reservation if any).
        var totalReserved = state.GetTotalReservedRemaining(MarketId, GoodId);

        // If this fleet has an active pickup job reservation for this market+good, it may consume its own reserved units.
        var ownReservationId = "";
        var ownRemaining = 0;

        var job = fleet.CurrentJob;
        if (job is not null && !string.IsNullOrWhiteSpace(job.ReservationId))
        {
            if (state.TryGetLogisticsReservation(job.ReservationId, out var r) && r is not null)
            {
                if (string.Equals(r.MarketId, MarketId, StringComparison.Ordinal) &&
                    string.Equals(r.GoodId, GoodId, StringComparison.Ordinal) &&
                    string.Equals(r.FleetId, FleetId, StringComparison.Ordinal) &&
                    r.Remaining > 0)
                {
                    ownReservationId = job.ReservationId;
                    ownRemaining = r.Remaining;
                }
            }
        }

        // Unreserved pool is inventory minus all reservations.
        var unreserved = availableInv - totalReserved;
        if (unreserved < 0) unreserved = 0;

        // Non-owner loads may only take from unreserved pool.
        // Owner load may take from unreserved + its own reserved remaining.
        var allowed = unreserved;
        if (!string.IsNullOrWhiteSpace(ownReservationId))
        {
            allowed = checked(unreserved + ownRemaining);
        }

        if (allowed <= 0) return;

        var qty = Math.Min(allowed, Quantity);
        if (qty <= 0) return;

        if (!InventoryLedger.TryRemoveMarket(market.Inventory, GoodId, qty)) return;
        InventoryLedger.AddCargo(fleet.Cargo, GoodId, qty);

        // If this was an owner pickup, consume reservation first.
        if (!string.IsNullOrWhiteSpace(ownReservationId))
        {
            var consume = Math.Min(ownRemaining, qty);
            if (consume > 0)
            {
                state.ConsumeLogisticsReservation(ownReservationId, consume);
            }
        }
    }
}
