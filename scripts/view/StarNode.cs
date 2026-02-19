using Godot;

namespace SpaceTradeEmpire.View;

public partial class StarNode : Area3D
{
    // The SimCore ID for this star (e.g., "star_0")
    public string NodeId { get; set; } = "";

    private bool _openedForThisDock = false;

    public override void _Ready()
    {
        // This Area3D is the dock trigger.
        CollisionLayer = 1; // World
        CollisionMask = 2;  // Player
        Monitoring = true;
        Monitorable = true;

        // Make sure StationMenu can resolve this node via meta (it already knows "sim_market_id")
        if (!HasMeta("sim_market_id"))
            SetMeta("sim_market_id", NodeId);

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body == null || !body.IsInGroup("Player"))
            return;

        GD.Print($"[StarNode] Player entered gravity well of {NodeId}");

        // OPEN THE MENU for any StarNode dock.
        // This is the missing link that prevents "freeze with no menu".
        // Guard so we don't re-open every physics tick if Godot fires multiple enters.
        if (!_openedForThisDock && body.HasSignal("shop_toggled"))
        {
            _openedForThisDock = true;
            body.EmitSignal("shop_toggled", true, (Variant)this);
        }

        // Keep legacy behavior if you still want it (it may set player dock state / freeze)
        if (body.HasMethod("dock_at_station"))
        {
            body.Call("dock_at_station", this);
        }
    }

    // Optional: if you ever want auto-close on leaving the gravity well, add:
    // public override void _ExitTree() { ... } or BodyExited handling.
    // I am not adding BodyExited by default because your docking UX seems to be "stay docked until menu close".
}
