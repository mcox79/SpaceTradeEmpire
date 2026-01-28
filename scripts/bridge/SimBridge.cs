using Godot;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Commands;
using System.Linq;

namespace SpaceTradeEmpire.Bridge;

public partial class SimBridge : Godot.Node
{
    public SimKernel Kernel { get; private set; }
    
    [Export] public int WorldSeed { get; set; } = 12345;
    [Export] public int StarCount { get; set; } = 20;
    
    private double _tickTimer = 0.0;
    [Export] public double TickInterval { get; set; } = 0.1;

    public override void _Ready()
    {
        GD.Print("[BRIDGE] Initializing SimCore Kernel...");
        Kernel = new SimKernel(WorldSeed);
        
        GD.Print($"[BRIDGE] Generating Galaxy ({StarCount} stars)...");
        GalaxyGenerator.Generate(Kernel.State, StarCount, 200f);
        GD.Print($"[BRIDGE] Generation Complete. Nodes: {Kernel.State.Nodes.Count} Edges: {Kernel.State.Edges.Count}");
        
        if (Kernel.State.Edges.Count > 0)
        {
            var edge = Kernel.State.Edges.Values.First();
            var fleet = new Fleet { Id = "test_ship_01", OwnerId = "player", CurrentNodeId = edge.FromNodeId, State = FleetState.Docked, Speed = 0.05f };
            
            Kernel.State.Fleets.Add(fleet.Id, fleet);
            GD.Print($"[BRIDGE] Spawned Fleet '{fleet.Id}' at '{fleet.CurrentNodeId}'.");
            
            var cmd = new TravelCommand(fleet.Id, edge.ToNodeId);
            Kernel.EnqueueCommand(cmd);
            GD.Print($"[BRIDGE] Commanded Travel to '{edge.ToNodeId}'.");
        }
        else
        {
            GD.PrintErr("[BRIDGE] ERROR: No edges generated! Map is broken.");
        }
    }

    public override void _Process(double delta)
    {
        _tickTimer += delta;
        if (_tickTimer >= TickInterval)
        {
            _tickTimer = 0.0;
            Kernel.Step();
            
            if (Kernel.State.Tick % 10 == 0)
            {
                var f = Kernel.State.Fleets.Values.FirstOrDefault();
                if (f != null)
                {
                    GD.Print($"[Tick {Kernel.State.Tick}] Fleet: {f.State} | Progress: {f.TravelProgress:F2} | Loc: {f.CurrentNodeId}");
                }
            }
        }
    }
}