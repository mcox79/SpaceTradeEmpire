using Godot;
using SimCore.Entities;
using SpaceTradeEmpire.Bridge;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    
    // Visual Cache
    private Dictionary<string, Node3D> _nodeVisuals = new();
    private Dictionary<string, MeshInstance3D> _fleetVisuals = new();

    private Mesh _shipMesh;
    private Material _shipMat;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        
        // Preload Assets
        _shipMesh = new PrismMesh { Size = new Vector3(1f, 1f, 2f) }; // Triangle Ship
        _shipMat = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.5f, 0f) }; // Orange

        if (_bridge != null) DrawStaticMap();
    }

    public override void _Process(double delta)
    {
        if (_bridge == null || _bridge.Kernel == null) return;
        
        UpdateFleets((float)delta);
    }

    private void DrawStaticMap()
    {
        var state = _bridge.Kernel.State;
        
        // Stars
        var starMesh = new SphereMesh { Radius = 1.0f };
        var starMat = new StandardMaterial3D { AlbedoColor = new Color(0, 0.6f, 1.0f), EmissionEnabled = true, Emission = new Color(0, 0.6f, 1.0f) };
        starMesh.Material = starMat;

        foreach (var node in state.Nodes.Values)
        {
            var instance = new MeshInstance3D { Mesh = starMesh, Name = node.Id };
            instance.Position = new Vector3(node.Position.X, node.Position.Y, node.Position.Z);
            AddChild(instance);
            _nodeVisuals[node.Id] = instance;
            
            // Name Tag
            var lbl = new Label3D { Text = node.Name, Position = new Vector3(0, 2f, 0), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled };
            instance.AddChild(lbl);
        }

        // Lanes
        var lineMat = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.3f) };
        foreach (var edge in state.Edges.Values)
        {
            if (!state.Nodes.ContainsKey(edge.FromNodeId) || !state.Nodes.ContainsKey(edge.ToNodeId)) continue;
            var p1 = state.Nodes[edge.FromNodeId].Position;
            var p2 = state.Nodes[edge.ToNodeId].Position;
            DrawLine(new Vector3(p1.X, p1.Y, p1.Z), new Vector3(p2.X, p2.Y, p2.Z), lineMat);
        }
    }

    private void UpdateFleets(float delta)
    {
        var fleets = _bridge.Kernel.State.Fleets;

        foreach (var fleet in fleets.Values)
        {
            // 1. Create Visual if missing
            if (!_fleetVisuals.ContainsKey(fleet.Id))
            {
                var mesh = new MeshInstance3D { Mesh = _shipMesh, MaterialOverride = _shipMat, Name = fleet.Id };
                AddChild(mesh);
                _fleetVisuals[fleet.Id] = mesh;
            }

            var visual = _fleetVisuals[fleet.Id];
            
            // 2. Calculate Position
            Vector3 targetPos = Vector3.Zero;
            
            if (fleet.State == FleetState.Docked || fleet.State == FleetState.Idle)
            {
                // Docked: Sit at the Node
                if (_nodeVisuals.ContainsKey(fleet.CurrentNodeId))
                {
                    targetPos = _nodeVisuals[fleet.CurrentNodeId].Position + new Vector3(0, 1.5f, 0);
                }
            }
            else if (fleet.State == FleetState.Travel)
            {
                // Travel: Lerp between nodes
                // Note: CurrentNodeId is the START node during travel
                if (_nodeVisuals.ContainsKey(fleet.CurrentNodeId) && _nodeVisuals.ContainsKey(fleet.DestinationNodeId))
                {
                    var start = _nodeVisuals[fleet.CurrentNodeId].Position;
                    var end = _nodeVisuals[fleet.DestinationNodeId].Position;
                    targetPos = start.Lerp(end, fleet.TravelProgress);
                    
                    // Look at destination
                    if (start.DistanceTo(end) > 0.1f)
                        visual.LookAt(end, Vector3.Up);
                }
            }

            // 3. Smooth Update (Simple check to avoid teleport jitter)
            if (visual.Position.DistanceTo(targetPos) > 100f) 
                visual.Position = targetPos; // Teleport if far (init)
            else
                visual.Position = visual.Position.Lerp(targetPos, 10f * delta); // Smooth slide
        }
    }

    private void DrawLine(Vector3 start, Vector3 end, Material mat)
    {
        var mid = (start + end) / 2f;
        var dist = start.DistanceTo(end);
        var mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = dist, Material = mat };
        var instance = new MeshInstance3D { Mesh = mesh, Position = mid };
        instance.LookAtFromPosition(mid, end, Vector3.Up);
        instance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
        AddChild(instance);
    }
}