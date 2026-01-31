using Godot;
using System;

namespace SpaceTradeEmpire.View;

public partial class StarNode : Area3D
{
    // The SimCore ID for this star (e.g., 'star_0')
    public string NodeId { get; set; } = "";

    public override void _Ready()
    {
        // Layer 1 (World) - Monitor for Player (Layer 2)
        CollisionLayer = 1;
        CollisionMask = 2;
        Monitoring = true;
        Monitorable = true;
        
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        // Check if the entering body is the Player
        if (body.IsInGroup("Player"))
        {
            GD.Print($"[StarNode] Player entered gravity well of {NodeId}");
            
            // Call 'dock_at_station' on the player script (GDScript)
            if (body.HasMethod("dock_at_station"))
            {
                body.Call("dock_at_station", this);
            }
        }
    }
}