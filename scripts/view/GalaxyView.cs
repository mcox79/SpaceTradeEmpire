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

    // CACHE FOR DYNAMIC UPDATES
    private Dictionary<string, StarNode> _starNodes = new();
    private Dictionary<string, MeshInstance3D> _edgeMeshes = new();

    private MultiMeshInstance3D _fleetMultiMeshInstance;
    private MultiMesh _fleetMultiMesh;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");

        // Initialize MultiMesh for Fleets
        _fleetMultiMeshInstance = new MultiMeshInstance3D();
        _fleetMultiMesh = new MultiMesh();
        _fleetMultiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        _fleetMultiMesh.Mesh = new PrismMesh { Size = new Vector3(1.5f, 1.5f, 3.0f) };
        _fleetMultiMesh.InstanceCount = 0;
        _fleetMultiMesh.VisibleInstanceCount = 0;
        _fleetMultiMeshInstance.Multimesh = _fleetMultiMesh;

        // Material setup: Bright Orange Emission
        var shipMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.6f, 0f),
            EmissionEnabled = true,
            Emission = new Color(1f, 0.4f, 0f),
            EmissionEnergyMultiplier = 2.0f
        };
        _fleetMultiMeshInstance.MaterialOverride = shipMat;
        AddChild(_fleetMultiMeshInstance);

        // Use the StationMenu already present in scenes/playable_prototype.tscn under UI/StationMenu
        _menu = GetNode<StationMenu>("/root/Main/UI/StationMenu");
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
        // StationMenu is responsible for connecting to PlayerShip.shop_toggled.
        // Do not connect it here, or you will get duplicate connection errors.

        if (_bridge == null || _bridge.IsLoading) return;

        if (!_bridge.TryExecuteSafeRead(state =>
        {
            UpdateFleets(state);
            UpdateEnvironment(state); // New: Heat/Trace Visualization
        }, timeoutMs: 0))
        {
            // Sim is stepping; do not stall the frame.
            return;
        }
    }

    private void DrawPhysicalMap()
    {
        if (!_bridge.TryExecuteSafeRead(state =>
        {
            if (state.Nodes == null) return;
            var sphereShape = new SphereShape3D { Radius = 2.5f };
            var starMesh = new SphereMesh { Radius = 1.0f };

            // Base materials (will be overridden by dynamic updates)
            var starMat = new StandardMaterial3D { AlbedoColor = new Color(0, 0.6f, 1.0f), EmissionEnabled = true };
            starMesh.Material = starMat;

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
                // UNIQUE MATERIAL per star to allow individual tinting
                mesh.MaterialOverride = starMat.Duplicate() as Material;
                starNode.AddChild(mesh);

                var lbl = new Label3D { Text = node.Name, Position = new Vector3(0, 3.5f, 0), Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 32 };
                starNode.AddChild(lbl);
            }

            if (state.Edges != null)
            {
                foreach (var edge in state.Edges.Values)
                {
                    if (_edgeMeshes.ContainsKey(edge.Id)) continue;
                    if (!state.Nodes.ContainsKey(edge.FromNodeId) || !state.Nodes.ContainsKey(edge.ToNodeId)) continue;

                    var p1 = state.Nodes[edge.FromNodeId].Position;
                    var p2 = state.Nodes[edge.ToNodeId].Position;

                    // Create unique material for this edge
                    var lineMat = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.3f), EmissionEnabled = true };
                    var meshInstance = DrawLine(new Vector3(p1.X, p1.Y, p1.Z), new Vector3(p2.X, p2.Y, p2.Z), lineMat);

                    _edgeMeshes[edge.Id] = meshInstance;
                }
            }
        }, timeoutMs: 0))
        {
            // If the sim is stepping, retry next idle frame so the map eventually draws without stalling.
            CallDeferred(nameof(DrawPhysicalMap));
            return;
        }
    }

    private void UpdateEnvironment(SimCore.SimState state)
    {
        // 1. Update Edge Heat (Glow)
        foreach (var edge in state.Edges.Values)
        {
            if (_edgeMeshes.TryGetValue(edge.Id, out var mesh))
            {
                if (mesh.MaterialOverride is StandardMaterial3D mat)
                {
                    float heat = edge.Heat;
                    if (heat > 0.1f)
                    {
                        // Heat > 0.1: Glow Red/Orange
                        float intensity = Mathf.Clamp(heat, 0f, 5f);
                        mat.AlbedoColor = new Color(1f, 0.5f, 0.2f);
                        mat.Emission = new Color(1f, 0.2f, 0f);
                        mat.EmissionEnergyMultiplier = intensity;
                    }
                    else
                    {
                        // Cool: Grey
                        mat.AlbedoColor = new Color(0.3f, 0.3f, 0.3f);
                        mat.Emission = Colors.Black;
                        mat.EmissionEnergyMultiplier = 0f;
                    }
                }
            }
        }

        // 2. Update Node Trace (Pollution Tint)
        foreach (var node in state.Nodes.Values)
        {
            if (_starNodes.TryGetValue(node.Id, out var starNode))
            {
                var mesh = starNode.GetChild<MeshInstance3D>(1); // Index 1 is Mesh (0 is Col)
                if (mesh != null && mesh.MaterialOverride is StandardMaterial3D mat)
                {
                    if (node.Trace > 0.5f)
                    {
                        // High Trace: Sick Green/Purple
                        float t = Mathf.Clamp(node.Trace / 5.0f, 0f, 1f);
                        mat.AlbedoColor = new Color(0f, 0.6f, 1f).Lerp(new Color(0.6f, 0f, 1f), t); // Blue -> Purple
                        mat.Emission = mat.AlbedoColor;
                    }
                    else
                    {
                        // Clean: Standard Blue
                        mat.AlbedoColor = new Color(0f, 0.6f, 1f);
                        mat.Emission = mat.AlbedoColor;
                    }
                }
            }
        }
    }

    private void UpdateFleets(SimCore.SimState state)
    {
        if (state.Fleets == null) return;

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

            if (state.Nodes.TryGetValue(fleet.CurrentNodeId, out var startNode))
            {
                pos = new Vector3(startNode.Position.X, startNode.Position.Y, startNode.Position.Z);
            }

            if (fleet.State == FleetState.Traveling && state.Nodes.TryGetValue(fleet.DestinationNodeId, out var endNode))
            {
                var endPos = new Vector3(endNode.Position.X, endNode.Position.Y, endNode.Position.Z);
                pos = pos.Lerp(endPos, fleet.TravelProgress);
                lookTarget = endPos;
            }
            else
            {
                lookTarget = pos + Vector3.Forward;
            }

            pos.Y += 2.5f;
            lookTarget.Y += 2.5f;

            Transform3D t = new Transform3D(Basis.Identity, pos);
            if (pos.DistanceSquaredTo(lookTarget) > 0.01f)
            {
                t = t.LookingAt(lookTarget, Vector3.Up);
            }

            _fleetMultiMesh.SetInstanceTransform(i, t);
        }
    }

    private MeshInstance3D DrawLine(Vector3 start, Vector3 end, Material mat)
    {
        var mid = (start + end) / 2f;
        var dist = start.DistanceTo(end);
        var mesh = new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = dist }; // Mat assigned to instance
        var instance = new MeshInstance3D { Mesh = mesh, Position = mid };
        instance.MaterialOverride = mat; // Must set Override to update dynamic properties
        instance.LookAtFromPosition(mid, end, Vector3.Up);
        instance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
        AddChild(instance);
        return instance;
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
        foreach (var kv in _edgeMeshes) if (IsInstanceValid(kv.Value)) kv.Value.QueueFree();
        _edgeMeshes.Clear();
    }
}
