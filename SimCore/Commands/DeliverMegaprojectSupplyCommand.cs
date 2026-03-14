using SimCore.Systems;

namespace SimCore.Commands;

// GATE.S8.MEGAPROJECT.SYSTEM.001: Deliver goods to a megaproject.
public sealed class DeliverMegaprojectSupplyCommand : ICommand
{
    public string MegaprojectId { get; set; }
    public string GoodId { get; set; }
    public int Quantity { get; set; }

    public DeliverMegaprojectSupplyCommand(string megaprojectId, string goodId, int quantity)
    {
        MegaprojectId = megaprojectId;
        GoodId = goodId;
        Quantity = quantity;
    }

    public void Execute(SimState state)
    {
        MegaprojectSystem.DeliverSupply(state, MegaprojectId, GoodId, Quantity);
    }
}
