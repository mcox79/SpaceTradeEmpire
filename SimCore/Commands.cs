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
        
        // Validation (The ""Rules"")
        if (state.PlayerCredits >= cost && market.Inventory >= Quantity)
        {
            // Mutate State
            state.PlayerCredits -= cost;
            market.Inventory -= Quantity;
            
            // Add to cargo
            if (!state.PlayerCargo.ContainsKey(MarketId)) state.PlayerCargo[MarketId] = 0;
            state.PlayerCargo[MarketId] += Quantity;
        }
    }
}