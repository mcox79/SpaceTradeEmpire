using System;
using System.Linq;
using SimCore.Content;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.T59.SHIP.SHIPYARD_SYSTEM.001: Sell a ship at a station shipyard.
public sealed class SellShipCommand : ICommand
{
    public string FleetId { get; }

    public SellShipCommand(string fleetId)
    {
        FleetId = fleetId;
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrEmpty(FleetId)) return;
        if (!state.Fleets.TryGetValue(FleetId, out var fleet)) return;

        // Cannot sell non-player ships.
        if (!string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) return;

        // Cannot sell the hero ship (active ship).
        var heroFleet = state.Fleets.Values.FirstOrDefault(f =>
            string.Equals(f.OwnerId, "player", StringComparison.Ordinal) && !f.IsStored);
        if (heroFleet != null && string.Equals(heroFleet.Id, FleetId, StringComparison.Ordinal)) return;

        // Ship must have empty cargo (or no cargo tracking for stored ships).
        // Check PlayerCargo only applies to active fleet; stored ships have no cargo.

        // Ship must be docked or stored at a shipyard station.
        if (!ShipyardSystem.IsShipyardStation(state, fleet.CurrentNodeId)) return;

        // Calculate sell-back price (80% of purchase price).
        int purchasePrice = ShipyardSystem.GetPurchasePrice(fleet.ShipClassId);
        int sellPrice = (int)((long)purchasePrice * ShipyardTweaksV0.SellBackPctBps / ShipyardTweaksV0.BpsDivisor);
        if (sellPrice <= 0) return;

        // Credit the player.
        state.PlayerCredits += sellPrice;

        // Remove the fleet.
        state.Fleets.Remove(FleetId);

        // Record transaction.
        state.AppendTransaction(new TransactionRecord
        {
            CashDelta = sellPrice,
            GoodId = "",
            Quantity = 1,
            Source = "ShipSale",
            NodeId = fleet.CurrentNodeId,
        });
    }
}
