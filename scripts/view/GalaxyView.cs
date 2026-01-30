using Godot;
using SimCore.Entities;
using SpaceTradeEmpire.Bridge;
using SpaceTradeEmpire.UI;
using System.Collections.Generic;
using System.Linq;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    private StationMenu _menu;
    private Dictionary<string, StarNode> _starNodes = new();
    
    // SLICE 2: MULTIMESH OPTIMIZATION
    private MultiMeshInstance3D _fleetMultiMeshInstance;
    private MultiMesh _fleetMultiMesh;
    private bool _playerConnected = false;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        
        // Initialize MultiMesh for Fleets
        _fleetMultiMeshInstance = new MultiMeshInstance3D();
        _fleetMultiMesh = new MultiMesh();
        _fleetMultiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        // VISUAL TWEAK: Made ships larger (1.5x, 3.0z) and Orange for visibility
        _fleetMultiMesh.Mesh = new PrismMesh { Size = new Vector3(1.5f, 1.5f, 3.0f) };
        _fleetMultiMesh.InstanceCount = 0;
        _fleetMultiMesh.VisibleInstanceCount = 0;
        _fleetMultiMeshInstance.Multimesh = _fleetMultiMesh;
        
        // Material setup: Bright Orange Emission so they pop against the black background
        var shipMat = new StandardMaterial3D 
        { 
            AlbedoColor = new Color(1f, 0.6f, 0f),
            EmissionEnabled = true,
            Emission = new Color(1f, 0.4f, 0f),
            EmissionEnergyMultiplier = 2.0f
        };
        _fleetMultiMeshInstance.MaterialOverride = shipMat;
        AddChild(_fleetMultiMeshInstance);

        var uiLayer = new CanvasLayer { Layer = 1 };
        AddChild(uiLayer);
        _menu = new StationMenu();
        uiLayer.AddChild(_menu);
        
        _menu.RequestUndock += OnMenuUndockRequested;

        if (_bridge != null)
        {
            _bridge.Connect(SimBridge.SignalName.SimLoaded, new Callable(this, nameof(OnSimLoaded)));
            CallDeferred(nameof(DrawPhysicalMap));
        }
    }

    private void OnMenuUndockRequested()
    {
        var player = GetTree().GetFirstNodeInGroup("Player");
        if (player != null && player.HasMethod("undock"))
        {
            player.Call("undock");
        }
    }

    public override void _Process(double delta)
    {
        if (!_playerConnected)
        {
            var player = GetTree().GetFirstNodeInGroup("Player");
            if (player != null && player.HasSignal("shop_toggled"))
            {
                player.Connect("shop_toggled", new Callable(_menu, "OnShopToggled"));
                _playerConnected = true;
            }
        }

        if (_bridge == null || _bridge.IsLoading) return;
        _bridge.ExecuteSafeRead(state => UpdateFleets(state));
    }

    private void DrawPhysicalMap()
    {
        _bridge.ExecuteSafeRead(state => 
        {
            if (state.Nodes == null) return;
            var sphereShape = new SphereShape3D { Radius = 2.5f };
            var starMesh = new SphereMesh { Radius = 1.0f };
            var starMat = new StandardMaterial3D { AlbedoColor = new Color(0, 0.6f, 1.0f), EmissionEnabled = true, Emission = new Color(0, 0.6f, 1.0f) };
            starMesh.Material = starMat;
            var lineMat = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.3f) };

            foreach (var node in state.Nodes.Values)
            {
                if (_starNodes.ContainsKey(node.Id)) continue;
                var starNode = new StarNode();
                starNode.Name = node.Id;
                starNode.NodeId = node.Id;
                starNode.Position = new Vector3(node.Position.X, node.Position.Y, node.Position.Z);
                AddChild(starNode);
    
                _starNodes[node.Id] = starNode;
                var col = new CollisionShape3D { Shape = sphereShape };
                starNode.AddChild(col);
                var mesh = new MeshInstance3D { Mesh = starMesh };
                starNode.AddChild(mesh);
                var lbl = new Label3D { Text = node.Name, Position = new Vector3(0, 3.5f, 0), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 32 };
                starNode.AddChild(lbl);
            }

            if (state.Edges != null)
            {
                foreach (var edge in state.Edges.Values)
                {
                    if (!state.Nodes.ContainsKey(edge.FromNodeId) || !state.Nodes.ContainsKey(edge.ToNodeId)) continue;
                    var p1 = state.Nodes[edge.FromNodeId].Position;
                    var p2 = state.Nodes[edge.ToNodeId].Position;
                    DrawLine(new Vector3(p1.X, p1.Y, p1.Z), new Vector3(p2.X, p2.Y, p2.Z), lineMat);
                }
            }
        });
    }

    private void UpdateFleets(SimCore.SimState state)
    {
        if (state.Fleets == null) return;
        
        // Filter fleets: Not player
        var visibleFleets = state.Fleets.Values.Where(f => f.OwnerId != "player").ToList();
        int count = visibleFleets.Count;

        if (_fleetMultiMesh.InstanceCount < count)
        {
            _fleetMultiMesh.InstanceCount = count + 100;
        }
        _fleetMultiMesh.VisibleInstanceCount = count;

        for (int i = 0; i < count; i++)
        {
            var fleet = visibleFleets[i];
            Vector3 pos = Vector3.Zero;
            Vector3 lookTarget = Vector3.Zero;
            
            // Get Start Position
            if (state.Nodes.TryGetValue(fleet.CurrentNodeId, out var startNode))
            {
                pos = new Vector3(startNode.Position.X, startNode.Position.Y, startNode.Position.Z);
            }

            // INTERPOLATION LOGIC
            if (fleet.State == FleetState.Traveling && state.Nodes.TryGetValue(fleet.DestinationNodeId, out var endNode))
            {
                var endPos = new Vector3(endNode.Position.X, endNode.Position.Y, endNode.Position.Z);
                pos = pos.Lerp(endPos, fleet.TravelProgress);
                lookTarget = endPos;
            }
            else
            {
                // If idle, look slightly up/random to show activity?
                // For now, just look forward
                lookTarget = pos + Vector3.Forward;
            }

            // Vertical Offset to fly ABOVE the lane lines (Star Radius is 1.0, Lane is at 0)
            // Increased to 2.5f to ensure visibility above the Star Mesh
            pos.Y += 2.5f;
            lookTarget.Y += 2.5f;

            // Transform Construction
            Transform3D t = new Transform3D(Basis.Identity, pos);
            
            // Rotation: Look at destination
            if (pos.DistanceSquaredTo(lookTarget) > 0.01f)
            {
                 t = t.LookingAt(lookTarget, Vector3.Up);
            }
            
            _fleetMultiMesh.SetInstanceTransform(i, t);
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

    private void OnSimLoaded()
    {
        GD.Print("[GalaxyView] Sim Loaded. Resetting visuals.");
        if (_menu != null) _menu.Close();
        ClearVisuals();
        CallDeferred(nameof(DrawPhysicalMap));
    }

    private void ClearVisuals()
    {
        _fleetMultiMesh.VisibleInstanceCount = 0;
        foreach (var kv in _starNodes) if (IsInstanceValid(kv.Value)) kv.Value.QueueFree();
        _starNodes.Clear();
    }
}