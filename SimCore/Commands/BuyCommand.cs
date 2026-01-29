using SimCore.Entities;

namespace SimCore.Commands;

public class BuyCommand : ICommand
{
    public string MarketId { get; set; }
    public int Quantity { get; set; }

    public BuyCommand(string marketId, int quantity)
    {
        MarketId = marketId;
        Quantity = quantity;
    }

    public void Execute(SimState state)
    {
        if (!state.Markets.ContainsKey(MarketId)) return;
        var market = state.Markets[MarketId];
        int cost = market.CurrentPrice * Quantity;
        
        if (state.PlayerCredits >= cost && market.Inventory >= Quantity)
        {
            state.PlayerCredits -= cost;
            market.Inventory -= Quantity;
            
            // FIX: Universal Cargo
            string cargoId = "generic_goods";
            
            if (!state.PlayerCargo.ContainsKey(cargoId)) state.PlayerCargo[cargoId] = 0;
            state.PlayerCargo[cargoId] += Quantity;
        }
    }
}