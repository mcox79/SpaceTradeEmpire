using Godot;
using System.Collections.Generic;
using SimCore;
using SpaceTradeEmpire.Bridge;

namespace SpaceTradeEmpire.View;

public partial class GhostSpawner : Node3D
{
    [Export] public PackedScene EnemyPrefab { get; set; }

    private SimBridge _bridge;
    private Node3D _playerNode;
    private Dictionary<string, Node3D> _activeGhosts = new();
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _bridge = GetNodeOrNull<SimBridge>("/root/SimBridge");
        // Safe cast to Node3D
        var player = GetTree().GetFirstNodeInGroup("Player");
        if (player is Node3D p3d) _playerNode = p3d;
        
        if (_bridge == null) GD.PrintErr("[GHOST] SimBridge not found!");
        
        // Fallback load
        if (EnemyPrefab == null)
        {
            EnemyPrefab = GD.Load<PackedScene>("res://scenes/enemy.tscn");
        }
    }

    public override void _Process(double delta)
    {
        if (_bridge == null || _bridge.Kernel == null) return;
        
        var simState = _bridge.Kernel.State;
        var playerLoc = simState.PlayerLocationNodeId;
        var currentFrameIds = new HashSet<string>();

        // 1. SPAWN / UPDATE
        foreach (var kvp in simState.Fleets)
        {
            var fleet = kvp.Value;
            
            // FILTER: Same node, not player
            if (fleet.CurrentNodeId == playerLoc && fleet.OwnerId != "player")
            {
                currentFrameIds.Add(fleet.Id);
                UpdateOrSpawnGhost(fleet);
            }
        }

        // 2. DESPAWN STALE
        var toRemove = new List<string>();
        foreach (var id in _activeGhosts.Keys)
        {
            if (!currentFrameIds.Contains(id)) toRemove.Add(id);
        }

        foreach (var id in toRemove)
        {
            if (IsInstanceValid(_activeGhosts[id])) _activeGhosts[id].QueueFree();
            _activeGhosts.Remove(id);
        }
    }

    private void UpdateOrSpawnGhost(SimCore.Entities.Fleet fleet)
    {
        if (_activeGhosts.ContainsKey(fleet.Id)) return;

        // SPAWN NEW
        GD.Print($"[GHOST] Spawning {fleet.Id}");
        var instance = EnemyPrefab.Instantiate<Node3D>();
        AddChild(instance);

        // Randomize Position
        float x = _rng.RandfRange(-50, 50);
        float z = _rng.RandfRange(-50, 50);
        var offset = new Vector3(x, 0, z);

        instance.GlobalPosition = (_playerNode != null) ? _playerNode.GlobalPosition + offset : offset;
        instance.Name = fleet.Id;
        instance.SetMeta("sim_id", fleet.Id);

        _activeGhosts[fleet.Id] = instance;
    }
}