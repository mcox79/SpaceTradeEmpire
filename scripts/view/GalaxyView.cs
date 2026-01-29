using Godot;
using SimCore.Entities;
using SimCore.Commands;
using SimCore;
using SpaceTradeEmpire.Bridge;
using SpaceTradeEmpire.UI; // New Namespace
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;
    private Camera3D _camera;
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

        // --- INIT UI ---
        _menu = new StationMenu();
        AddChild(_menu);
        // ---------------

        if (_bridge != null) DrawStaticMap();
    }

    public override void _Process(double delta)
    {
        if (_bridge == null || _bridge.Kernel == null) return;
        if (_camera == null) _camera = GetViewport().GetCamera3D(); 

        UpdateFleets((float)delta);
        HandleInput();
    }

    private void HandleInput()
    {
        // Don interaction if Menu is open
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

            if (!string.IsNullOrEmpty(clickedNodeId))
            {
                SelectStar(clickedNodeId);
            }
        }
        if (!down) _mouseLeftHeld = false;
    }

    private void SelectStar(string nodeId)
    {
        if (!_nodeVisuals.ContainsKey(nodeId)) return;

        _selectionRing.Visible = true;
        _selectionRing.Position = _nodeVisuals[nodeId].Position;

        if (_bridge == null || _bridge.Kernel == null) { _menu.Open(nodeId); return; }

        var state = _bridge.Kernel.State;
        if (!state.Fleets.TryGetValue("test_ship_01", out var fleet)) { _menu.Open(nodeId); return; }

        // If already here, open the station menu
        if (fleet.CurrentNodeId == nodeId)
        {
            _menu.Open(nodeId);
            return;
        }

        // If reachable, travel and close the menu
        if (MapQueries.AreConnected(state, fleet.CurrentNodeId, nodeId))
        {
            _bridge.Kernel.EnqueueCommand(new TravelCommand(fleet.Id, nodeId));
            _menu.Close();
            return;
        }

        // Not reachable: still open menu for feedback
        _menu.Open(nodeId);
    }

    // --- DRAWING LOGIC (Minimally Changed) ---
    private void DrawStaticMap()
    {
        var state = _bridge.Kernel.State;
        
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
        foreach (var fleet in _bridge.Kernel.State.Fleets.Values)
        {
            if (!_fleetVisuals.ContainsKey(fleet.Id))
            {
                var mesh = new MeshInstance3D { Mesh = _shipMesh, MaterialOverride = _shipMat, Name = fleet.Id };
                AddChild(mesh);
                _fleetVisuals[fleet.Id] = mesh;
            }

            var visual = _fleetVisuals[fleet.Id];
            Vector3 targetPos = Vector3.Zero;
            
            if (fleet.State == FleetState.Travel && _nodeVisuals.ContainsKey(fleet.CurrentNodeId) && _nodeVisuals.ContainsKey(fleet.DestinationNodeId))
            {
                var start = _nodeVisuals[fleet.CurrentNodeId].Position;
                var end = _nodeVisuals[fleet.DestinationNodeId].Position;
                targetPos = start.Lerp(end, fleet.TravelProgress);
                if (start.DistanceTo(end) > 0.1f) visual.LookAt(end, Vector3.Up);
            }
            else if (_nodeVisuals.ContainsKey(fleet.CurrentNodeId))
            {
                targetPos = _nodeVisuals[fleet.CurrentNodeId].Position + new Vector3(0, 1.5f, 0);
            }

            if (visual.Position.DistanceTo(targetPos) > 100f) visual.Position = targetPos;
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
}
