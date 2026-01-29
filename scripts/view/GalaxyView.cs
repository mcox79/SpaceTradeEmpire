using Godot;
using SimCore.Entities;
using SimCore.Commands;
using SimCore;
using SpaceTradeEmpire.Bridge;
using SpaceTradeEmpire.UI;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    private Camera3D _camera;
    private CanvasLayer _uiLayer;
    private StationMenu _menu; 
    private bool _mouseLeftHeld;
    
    private Dictionary<string, Node3D> _nodeVisuals = new();
    private Dictionary<string, MeshInstance3D> _fleetVisuals = new();
    
    private MeshInstance3D _selectionRing;
    private Mesh _shipMesh;
    private Material _shipMat;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        _camera = GetViewport().GetCamera3D(); 
        
        _shipMesh = new PrismMesh { Size = new Vector3(1f, 1f, 2f) }; 
        _shipMat = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.5f, 0f) }; 

        _selectionRing = new MeshInstance3D 
        { 
            Mesh = new TorusMesh { InnerRadius = 1.5f, OuterRadius = 1.8f },
            Visible = false 
        };
        AddChild(_selectionRing);

        _uiLayer = new CanvasLayer { Layer = 1 };
        AddChild(_uiLayer);

        _menu = new StationMenu();
        _uiLayer.AddChild(_menu);

        if (_bridge != null)
        {
            _bridge.Connect(SimBridge.SignalName.SimLoaded, new Callable(this, nameof(OnSimLoaded)));
            DrawStaticMap();
            SyncSelectionFromState();
        }
    }

    public override void _Process(double delta)
    {
        // SAFETY: Do not process view if bridge is gone, invalid, or actively loading
        if (_bridge == null || _bridge.Kernel == null || _bridge.IsLoading) return;
        if (_camera == null) _camera = GetViewport().GetCamera3D(); 

        try
        {
            UpdateFleets((float)delta);
            HandleInput();
        }
        catch (System.Exception ex)
        {
            // Log but do not crash the game loop
            GD.PrintErr($"[GalaxyView] Error: {ex.Message}");
        }
    }

    private void HandleInput()
    {
        if (_menu.Visible) return;
        var hovered = GetViewport().GuiGetHoveredControl();
        if (hovered != null) return;

        var down = Input.IsMouseButtonPressed(MouseButton.Left);
        if (down && !_mouseLeftHeld)
        {
            _mouseLeftHeld = true;
            if (_camera == null) return;

            var mousePos = GetViewport().GetMousePosition();
            var from = _camera.ProjectRayOrigin(mousePos);
            var dir = _camera.ProjectRayNormal(mousePos);

            string clickedNodeId = "";
            float closestDist = float.MaxValue;
            if (_bridge.Kernel.State.Nodes == null) return;

            foreach (var node in _bridge.Kernel.State.Nodes.Values)
            {
                var starPos = new Vector3(node.Position.X, node.Position.Y, node.Position.Z);
                var diff = starPos - from;
                var cross = diff.Cross(dir);
                var distToRay = cross.Length();

                if (distToRay < 2.0f && distToRay < closestDist) 
                {
                    closestDist = distToRay;
                    clickedNodeId = node.Id;
                }
            }

            if (!string.IsNullOrEmpty(clickedNodeId)) SelectStar(clickedNodeId);
        }
        if (!down) _mouseLeftHeld = false;
    }

    private void SelectStar(string nodeId)
    {
        if (!_nodeVisuals.ContainsKey(nodeId)) return;
        _selectionRing.Visible = true;
        _selectionRing.Position = _nodeVisuals[nodeId].Position;

        if (_bridge?.Kernel?.State == null) { _menu.Open(nodeId); return; }
        var state = _bridge.Kernel.State;
        state.PlayerSelectedDestinationNodeId = nodeId;

        if (!state.Fleets.TryGetValue("test_ship_01", out var fleet)) { _menu.Open(nodeId); return; }
        if (fleet.CurrentNodeId == nodeId) { _menu.Open(nodeId); return; }
        if (MapQueries.AreConnected(state, fleet.CurrentNodeId, nodeId))
        {
            _bridge.Kernel.EnqueueCommand(new TravelCommand(fleet.Id, nodeId));
            _menu.Close();
            return;
        }
        _menu.Open(nodeId);
    }

    private void DrawStaticMap()
    {
        var state = _bridge.Kernel.State;
        if (state.Nodes == null) return;

        var starMesh = new SphereMesh { Radius = 1.0f };
        var starMat = new StandardMaterial3D { AlbedoColor = new Color(0, 0.6f, 1.0f), EmissionEnabled = true, Emission = new Color(0, 0.6f, 1.0f) };
        starMesh.Material = starMat;

        foreach (var node in state.Nodes.Values)
        {
            if (_nodeVisuals.ContainsKey(node.Id)) continue; 
            var instance = new MeshInstance3D { Mesh = starMesh, Name = node.Id };
            instance.Position = new Vector3(node.Position.X, node.Position.Y, node.Position.Z);
            AddChild(instance);
            _nodeVisuals[node.Id] = instance;
            var lbl = new Label3D { Text = node.Name, Position = new Vector3(0, 2.5f, 0), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 32 };
            instance.AddChild(lbl);
        }

        var lineMat = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.3f) };
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
    }

    private void UpdateFleets(float delta)
    {
        if (_bridge.Kernel.State.Fleets == null) return;

        foreach (var fleet in _bridge.Kernel.State.Fleets.Values)
        {
            Vector3 targetPos = Vector3.Zero;
            Vector3? lookAtTarget = null;

            if (fleet.State == FleetState.Travel 
                && !string.IsNullOrEmpty(fleet.CurrentNodeId) 
                && !string.IsNullOrEmpty(fleet.DestinationNodeId)
                && _nodeVisuals.TryGetValue(fleet.CurrentNodeId, out var startNode)
                && _nodeVisuals.TryGetValue(fleet.DestinationNodeId, out var endNode))
            {
                targetPos = startNode.Position.Lerp(endNode.Position, fleet.TravelProgress);
                lookAtTarget = endNode.Position;
            }
            else if (!string.IsNullOrEmpty(fleet.CurrentNodeId) && _nodeVisuals.TryGetValue(fleet.CurrentNodeId, out var node))
            {
                targetPos = node.Position + new Vector3(0, 1.5f, 0);
            }

            if (!_fleetVisuals.TryGetValue(fleet.Id, out var visual))
            {
                visual = new MeshInstance3D { Mesh = _shipMesh, MaterialOverride = _shipMat, Name = fleet.Id };
                AddChild(visual);
                visual.Position = targetPos;
                if (lookAtTarget.HasValue) visual.LookAt(lookAtTarget.Value, Vector3.Up);
                _fleetVisuals[fleet.Id] = visual;
            }

            if (lookAtTarget.HasValue && visual.Position.DistanceTo(lookAtTarget.Value) > 0.1f)
            {
                visual.LookAt(lookAtTarget.Value, Vector3.Up);
            }

            float dist = visual.Position.DistanceTo(targetPos);
            if (dist > 100f) visual.Position = targetPos;
            else visual.Position = visual.Position.Lerp(targetPos, 10f * delta);
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
    
    private void SyncSelectionFromState()
    {
        _selectionRing.Visible = false;
        if (_bridge?.Kernel?.State == null) return;

        var id = _bridge.Kernel.State.PlayerSelectedDestinationNodeId;
        if (!string.IsNullOrEmpty(id) && _nodeVisuals.TryGetValue(id, out var visual))
        {
            _selectionRing.Visible = true;
            _selectionRing.Position = visual.Position;
        }
    }

    private void OnSimLoaded()
    {
        GD.Print("[GalaxyView] Sim Loaded. Resetting visuals.");
        if (_menu != null) _menu.Close();
        ClearVisuals();
        DrawStaticMap();
        SyncSelectionFromState();
    }

    private void ClearVisuals()
    {
        foreach (var kv in _fleetVisuals) if (IsInstanceValid(kv.Value)) kv.Value.QueueFree();
        _fleetVisuals.Clear();
        foreach (var kv in _nodeVisuals) if (IsInstanceValid(kv.Value)) kv.Value.QueueFree();
        _nodeVisuals.Clear();
    }
}