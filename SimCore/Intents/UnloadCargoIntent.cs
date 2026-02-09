using SimCore.Commands;

namespace SimCore.Intents;

public sealed class UnloadCargoIntent : IIntent
{
    public string Kind => "UNLOAD_CARGO";

    public string FleetId { get; }
    public string MarketId { get; }
    public string GoodId { get; }
    public int Quantity { get; }

    public UnloadCargoIntent(string fleetId, string marketId, string goodId, int quantity)
    {
        FleetId = fleetId ?? "";
        MarketId = marketId ?? "";
        GoodId = goodId ?? "";
        Quantity = quantity;
    }

    public void Apply(SimState state)
    {
        var cmd = new UnloadCargoCommand(FleetId, MarketId, GoodId, Quantity);
        cmd.Execute(state);
    }
}
