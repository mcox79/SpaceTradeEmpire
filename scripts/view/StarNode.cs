using Godot;

namespace SpaceTradeEmpire.View;

public partial class StarNode : Area3D
{
    // Explicit dock trigger range (units).
    // Godot-side only; no SimCore impact.
    private const float DOCK_RANGE_U = 12.0f;

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

        EnsureDockShapeV0();

        // Make sure StationMenu can resolve this node via meta (it already knows "sim_market_id")
        if (!HasMeta("sim_market_id"))
            SetMeta("sim_market_id", NodeId);

        BodyEntered += OnBodyEntered;
    }

    private void EnsureDockShapeV0()
    {
        // Make the trigger range explicit and stable regardless of scene authoring.
        var shapeNode = GetNodeOrNull<CollisionShape3D>("DockShape")
            ?? GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

        if (shapeNode == null)
        {
            shapeNode = new CollisionShape3D { Name = "DockShape" };
            AddChild(shapeNode);
        }

        // Only force radius if it is a sphere or absent.
        if (shapeNode.Shape == null || shapeNode.Shape is SphereShape3D)
        {
            var sphere = shapeNode.Shape as SphereShape3D ?? new SphereShape3D();
            sphere.Radius = DOCK_RANGE_U;
            shapeNode.Shape = sphere;
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body == null || !body.IsInGroup("Player"))
            return;

        GD.Print($"[StarNode] Player entered gravity well of {NodeId}");

        // Canonical path: route docking intent through GameManager so player ship state is centralized.
        var gm = GetTree()?.Root?.FindChild("GameManager", true, false);
        if (gm != null && gm.HasMethod("on_proximity_dock_entered_v0"))
        {
            gm.Call("on_proximity_dock_entered_v0", this);
        }

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
