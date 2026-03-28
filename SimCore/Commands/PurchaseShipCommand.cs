using System;
using System.Linq;
using SimCore.Content;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Commands;

// GATE.T59.SHIP.SHIPYARD_SYSTEM.001: Purchase a ship at a station shipyard.
public sealed class PurchaseShipCommand : ICommand
{
    public string ClassId { get; }
    public string StationNodeId { get; }

    public PurchaseShipCommand(string classId, string stationNodeId)
    {
        ClassId = classId;
        StationNodeId = stationNodeId;
    }

    public void Execute(SimState state)
    {
        if (string.IsNullOrEmpty(ClassId) || string.IsNullOrEmpty(StationNodeId)) return;

        var classDef = ShipClassContentV0.GetById(ClassId);
        if (classDef == null) return;

        // Look up price from ShipyardTweaksV0 price table.
        int price = ShipyardSystem.GetPurchasePrice(ClassId);
        if (price <= 0) return;

        // Credit check.
        if (state.PlayerCredits < price) return;

        // Faction rep check for variants.
        if (!string.IsNullOrEmpty(classDef.FactionId))
        {
            int requiredRep = ShipyardTweaksV0.VariantRepRequired;
            state.FactionReputation.TryGetValue(classDef.FactionId, out int playerRep);
            if (playerRep < requiredRep) return;
        }

        // Station must be a shipyard-capable station (faction-owned, tier >= MinStationTierForShipyard).
        if (!ShipyardSystem.IsShipyardStation(state, StationNodeId)) return;

        // Player must be docked at the station.
        var heroFleet = state.Fleets.Values.FirstOrDefault(f =>
            string.Equals(f.OwnerId, "player", StringComparison.Ordinal) && !f.IsStored);
        if (heroFleet == null) return;
        if (!string.Equals(heroFleet.CurrentNodeId, StationNodeId, StringComparison.Ordinal)) return;
        if (heroFleet.State != Entities.FleetState.Docked) return;

        // Deduct credits.
        state.PlayerCredits -= price;

        // Create new fleet entity with class stats.
        var newFleet = ShipyardSystem.CreateFleetFromClass(classDef, StationNodeId, state);
        state.Fleets[newFleet.Id] = newFleet;

        // GATE.T62.SHIP.MODULE_REASSIGN.001: Transfer compatible modules from hero ship to new ship.
        RefitSystem.TransferModules(heroFleet, newFleet);

        // Record transaction.
        state.AppendTransaction(new TransactionRecord
        {
            CashDelta = -price,
            GoodId = "",
            Quantity = 1,
            Source = "ShipPurchase",
            NodeId = StationNodeId,
        });
    }
}
