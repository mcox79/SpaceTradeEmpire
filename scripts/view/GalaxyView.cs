using Godot;
using SimCore.Entities;
using SpaceTradeEmpire.Bridge;
using SpaceTradeEmpire.UI;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    private StationMenu _menu;
    private Dictionary<string, StarNode> _starNodes = new();
    private Dictionary<string, MeshInstance3D> _fleetVisuals = new();
    private Mesh _shipMesh;
    private Material _shipMat;
    private bool _playerConnected = false;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        _shipMesh = new PrismMesh { Size = new Vector3(1f, 1f, 2f) };
        _shipMat = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.5f, 0f) };

        var uiLayer = new CanvasLayer { Layer = 1 };
        AddChild(uiLayer);
        _menu = new StationMenu();
        uiLayer.AddChild(_menu);

        // DECOUPLING: View handles the wiring between UI and Player
        _menu.RequestUndock += OnMenuUndockRequested;

        if (_bridge != null)
        {
            _bridge.Connect(SimBridge.SignalName.SimLoaded, new Callable(this, nameof(OnSimLoaded)));
            CallDeferred(nameof(DrawPhysicalMap));
        }
    }

    // Handle UI Signal -> Player Action
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
        // Lazy Connect: Player Signals -> UI
        if (!_playerConnected)
        {
            var player = GetTree().GetFirstNodeInGroup("Player");
            if (player != null && player.HasSignal("shop_toggled"))
            {
                player.Connect("shop_toggled", new Callable(_menu, "OnShopToggled"));
                _playerConnected = true;
                GD.Print("[GalaxyView] Player signal connected to StationMenu.");
            }
        }

        if (_bridge == null || _bridge.IsLoading) return;
        // Safe Read Wrapper for Fleet Visuals
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
        foreach (var fleet in state.Fleets.Values)
        {
            if (fleet.OwnerId == "player" && fleet.Id == "test_ship_01") continue;
            if (!_fleetVisuals.TryGetValue(fleet.Id, out var visual))
            {
                visual = new MeshInstance3D { Mesh = _shipMesh, MaterialOverride = _shipMat, Name = fleet.Id };
                AddChild(visual);
                _fleetVisuals[fleet.Id] = visual;
            }
            // Basic position update (Refine in next slice for movement interpolation)
            // For now, we assume Node-to-Node jumps are instant in visual or handled by SimBridge interpolation (TODO)
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
        foreach (var kv in _fleetVisuals) if (IsInstanceValid(kv.Value)) kv.Value.QueueFree();
        _fleetVisuals.Clear();
        foreach (var kv in _starNodes) if (IsInstanceValid(kv.Value)) kv.Value.QueueFree();
        _starNodes.Clear();
    }
}