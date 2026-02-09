using SimCore.Commands;

namespace SimCore.Intents;

public sealed class LoadCargoIntent : IIntent
{
    public string Kind => "LOAD_CARGO";

    public string FleetId { get; }
    public string MarketId { get; }
    public string GoodId { get; }
    public int Quantity { get; }

    public LoadCargoIntent(string fleetId, string marketId, string goodId, int quantity)
    {
        FleetId = fleetId ?? "";
        MarketId = marketId ?? "";
        GoodId = goodId ?? "";
        Quantity = quantity;
    }

    public void Apply(SimState state)
    {
        var cmd = new LoadCargoCommand(FleetId, MarketId, GoodId, Quantity);
        cmd.Execute(state);
    }
}
