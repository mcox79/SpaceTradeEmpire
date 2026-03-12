using Godot;

namespace SpaceTradeEmpire.View;

public partial class StarNode : Area3D
{
    // Explicit dock trigger range (units).
    // Godot-side only; no SimCore impact.
    // Pace overhaul: tighter dock trigger (was 12u), confirmation required.
    private const float DOCK_RANGE_U = 8.0f;

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
        BodyExited += OnBodyExited;
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

        // Dock confirmation: show prompt, player presses E to commit.
        var gm = GetTree()?.Root?.FindChild("GameManager", true, false);
        if (gm != null && gm.HasMethod("on_dock_proximity_v0"))
        {
            gm.Call("on_dock_proximity_v0", this);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body == null || !body.IsInGroup("Player"))
            return;

        _openedForThisDock = false;
        var gm = GetTree()?.Root?.FindChild("GameManager", true, false);
        if (gm != null && gm.HasMethod("on_dock_proximity_exit_v0"))
        {
            gm.Call("on_dock_proximity_exit_v0", this);
        }
    }
}
