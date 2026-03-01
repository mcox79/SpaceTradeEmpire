using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;

    private bool _overlayOpen = false;

    // Visual caches (deterministic keys)
    private readonly Dictionary<string, Node3D> _nodeRootsById = new();
    private readonly Dictionary<string, MeshInstance3D> _edgeMeshesByKey = new();

    private int _lastNodeCount = 0;
    private int _lastEdgeCount = 0;
    private bool _lastPlayerHighlighted = false;

    public override void _Ready()
    {
        _bridge = GetNodeOrNull<SimBridge>("/root/SimBridge");

        // Default OFF: the playable prototype is a local-space view until Tab opens the overlay.
        Visible = false;
        SetProcess(false);
    }

    // Deterministic helper for spawners: mark any scene node as a proximity dock target.
    // Ordering: no iteration; only direct node mutation (group + meta).
    public static void RegisterDockTargetV0(Node node, string kindToken, string targetId)
    {
        if (node == null) return;

        if (!node.IsInGroup("DockTarget"))
        {
            node.AddToGroup("DockTarget");
        }

        if (!string.IsNullOrEmpty(kindToken))
        {
            node.SetMeta("dock_target_kind", kindToken);
        }

        if (!string.IsNullOrEmpty(targetId))
        {
            node.SetMeta("dock_target_id", targetId);
        }
    }

    public void SetOverlayOpenV0(bool isOpen)
    {
        _overlayOpen = isOpen;

        Visible = isOpen;
        SetProcess(isOpen);

        if (isOpen)
        {
            // Defer one frame so SimBridge can finish boot sequences.
            CallDeferred(nameof(RefreshFromSnapshotV0));
        }
    }

    public Godot.Collections.Dictionary GetOverlayMetricsV0()
    {
        return new Godot.Collections.Dictionary
        {
            ["node_count"] = _lastNodeCount,
            ["edge_count"] = _lastEdgeCount,
            ["player_node_highlighted"] = _lastPlayerHighlighted
        };
    }

    public override void _Process(double delta)
    {
        if (!_overlayOpen) return;
        if (_bridge == null || _bridge.IsLoading) return;

        RefreshFromSnapshotV0();
    }

    private void RefreshFromSnapshotV0()
    {
        var snap = _bridge.GetGalaxySnapshotV0();
        if (snap == null) return;

        var playerNodeId = snap.ContainsKey("player_current_node_id")
            ? (string)snap["player_current_node_id"]
            : "";

        var rawNodes = snap.ContainsKey("system_nodes")
            ? (Godot.Collections.Array)snap["system_nodes"]
            : new Godot.Collections.Array();

        var rawEdges = snap.ContainsKey("lane_edges")
            ? (Godot.Collections.Array)snap["lane_edges"]
            : new Godot.Collections.Array();

        // Parse nodes into a stable, explicitly sorted list (node_id Ordinal asc).
        var nodes = new List<NodeSnapV0>(rawNodes.Count);
        for (int i = 0; i < rawNodes.Count; i++)
        {
            Variant v = rawNodes[i];
            if (v.VariantType != Variant.Type.Dictionary) continue;

            Godot.Collections.Dictionary n = v.AsGodotDictionary();

            var nodeId = n.ContainsKey("node_id") ? (string)(Variant)n["node_id"] : "";
            var stateToken = n.ContainsKey("display_state_token") ? (string)(Variant)n["display_state_token"] : "";
            var displayText = n.ContainsKey("display_text") ? (string)(Variant)n["display_text"] : "";

            // Cast from Variant to float directly to avoid locale parsing issues.
            float x = n.ContainsKey("pos_x") ? (float)(Variant)n["pos_x"] : 0f;
            float y = n.ContainsKey("pos_y") ? (float)(Variant)n["pos_y"] : 0f;
            float z = n.ContainsKey("pos_z") ? (float)(Variant)n["pos_z"] : 0f;

            nodes.Add(new NodeSnapV0(nodeId, stateToken, displayText, new Vector3(x, y, z)));
        }

        nodes.Sort(static (a, b) => StringComparer.Ordinal.Compare(a.NodeId, b.NodeId));

        // Parse edges into a stable, explicitly sorted list (from_id Ordinal asc, then to_id Ordinal asc).
        var edges = new List<EdgeSnapV0>(rawEdges.Count);
        for (int i = 0; i < rawEdges.Count; i++)
        {
            Variant v = rawEdges[i];
            if (v.VariantType != Variant.Type.Dictionary) continue;

            Godot.Collections.Dictionary e = v.AsGodotDictionary();

            var fromId = e.ContainsKey("from_id") ? (string)(Variant)e["from_id"] : "";
            var toId = e.ContainsKey("to_id") ? (string)(Variant)e["to_id"] : "";

            edges.Add(new EdgeSnapV0(fromId, toId));
        }

        edges.Sort(static (a, b) =>
        {
            int c = StringComparer.Ordinal.Compare(a.FromId, b.FromId);
            if (c != 0) return c;
            return StringComparer.Ordinal.Compare(a.ToId, b.ToId);
        });

        _lastNodeCount = nodes.Count;
        _lastEdgeCount = edges.Count;

        // Nodes: create/update visuals
        bool playerHighlighted = false;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (string.IsNullOrEmpty(n.NodeId)) continue;

            if (!_nodeRootsById.TryGetValue(n.NodeId, out var root))
            {
                root = CreateNodeVisualV0(n.NodeId);
                _nodeRootsById[n.NodeId] = root;
                AddChild(root);
            }

            root.Position = n.Position;

            var label = root.GetNodeOrNull<Label3D>("NodeLabel");
            if (label != null)
            {
                // Rumored nodes must not reveal their true name.
                // Token contract: DisplayStateToken == "RUMORED" => render "???".
                label.Text = StringComparer.Ordinal.Equals(n.DisplayStateToken, "RUMORED")
                    ? "???"
                    : (n.DisplayText ?? "");
            }

            var mesh = root.GetNodeOrNull<MeshInstance3D>("NodeMesh");
            if (mesh != null && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                bool isPlayer = !string.IsNullOrEmpty(playerNodeId) && StringComparer.Ordinal.Equals(n.NodeId, playerNodeId);
                if (isPlayer)
                {
                    playerHighlighted = true;
                    mat.AlbedoColor = new Color(0.2f, 1.0f, 0.4f);
                    mat.EmissionEnabled = true;
                    mat.Emission = new Color(0.2f, 1.0f, 0.4f);
                    mat.EmissionEnergyMultiplier = 2.0f;
                }
                else
                {
                    mat.AlbedoColor = new Color(0f, 0.6f, 1.0f);
                    mat.EmissionEnabled = true;
                    mat.Emission = new Color(0f, 0.6f, 1.0f);
                    mat.EmissionEnergyMultiplier = 1.0f;
                }
            }
        }

        _lastPlayerHighlighted = playerHighlighted;

        // Edges: create/update visuals
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (string.IsNullOrEmpty(e.FromId) || string.IsNullOrEmpty(e.ToId)) continue;

            // Key is deterministic for the edge list ordering.
            string key = e.FromId + "->" + e.ToId;

            if (!_edgeMeshesByKey.TryGetValue(key, out var mesh))
            {
                var mat = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.3f, 0.3f, 0.3f),
                    EmissionEnabled = true,
                    Emission = new Color(0.1f, 0.1f, 0.1f),
                    EmissionEnergyMultiplier = 0.5f
                };
                mesh = CreateEdgeMeshV0(mat);
                _edgeMeshesByKey[key] = mesh;
                AddChild(mesh);
            }

            if (!_nodeRootsById.TryGetValue(e.FromId, out var fromRoot)) continue;
            if (!_nodeRootsById.TryGetValue(e.ToId, out var toRoot)) continue;

            UpdateEdgeTransformV0(mesh, fromRoot.GlobalPosition, toRoot.GlobalPosition);
        }
    }

    private static Node3D CreateNodeVisualV0(string nodeId)
    {
        var root = new Node3D();
        root.Name = "GalaxyNode_" + nodeId;

        var mesh = new MeshInstance3D();
        mesh.Name = "NodeMesh";
        mesh.Mesh = new SphereMesh { Radius = 1.0f };

        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0f, 0.6f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0f, 0.6f, 1.0f),
            EmissionEnergyMultiplier = 1.0f
        };

        root.AddChild(mesh);

        var lbl = new Label3D
        {
            Name = "NodeLabel",
            Text = "",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.01f
        };
        lbl.Position = new Vector3(0, 3.0f, 0);
        root.AddChild(lbl);

        return root;
    }

    private static MeshInstance3D CreateEdgeMeshV0(Material mat)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = "GalaxyEdge";

        // Thin cylinder oriented along +Y then rotated into place.
        var cyl = new CylinderMesh
        {
            TopRadius = 0.05f,
            BottomRadius = 0.05f,
            Height = 1.0f
        };
        mesh.Mesh = cyl;
        mesh.MaterialOverride = mat;

        return mesh;
    }

    private static void UpdateEdgeTransformV0(MeshInstance3D mesh, Vector3 start, Vector3 end)
    {
        var mid = (start + end) * 0.5f;
        var dist = start.DistanceTo(end);

        // CylinderMesh is centered; height along Y axis.
        mesh.Position = mid;

        // Build a basis that points Y towards (end-start).
        var dir = (end - start).Normalized();

        // Godot: Basis.LookingAt points -Z; we want +Y. Build from orthonormal axes.
        Vector3 up = dir;
        Vector3 fwd = Vector3.Forward;
        if (Mathf.Abs(up.Dot(fwd)) > 0.99f) fwd = Vector3.Right;
        Vector3 right = fwd.Cross(up).Normalized();
        fwd = up.Cross(right).Normalized();

        // Columns are X, Y, Z axes.
        var basis = new Basis(right, up, fwd);

        mesh.Basis = basis;

        if (mesh.Mesh is CylinderMesh cyl)
        {
            cyl.Height = dist;
        }
    }

    private readonly struct NodeSnapV0
    {
        public readonly string NodeId;
        public readonly string DisplayStateToken;
        public readonly string DisplayText;
        public readonly Vector3 Position;

        public NodeSnapV0(string nodeId, string displayStateToken, string displayText, Vector3 position)
        {
            NodeId = nodeId ?? "";
            DisplayStateToken = displayStateToken ?? "";
            DisplayText = displayText ?? "";
            Position = position;
        }
    }

    private readonly struct EdgeSnapV0
    {
        public readonly string FromId;
        public readonly string ToId;

        public EdgeSnapV0(string fromId, string toId)
        {
            FromId = fromId ?? "";
            ToId = toId ?? "";
        }
    }
}
