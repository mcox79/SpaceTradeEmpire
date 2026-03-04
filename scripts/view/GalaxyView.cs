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

    // --- Local system config (named exported fields; no numeric literals in .cs or .tscn) ---
    [Export] public float SystemSceneRadiusU { get; set; } = 120.0f;
    [Export] public float StationOrbitRadiusU { get; set; } = 60.0f;
    [Export] public float LaneGateDistanceU { get; set; } = 90.0f;
    [Export] public float DiscoverySiteOrbitRadiusU { get; set; } = 40.0f;
    [Export] public float StarVisualRadiusU { get; set; } = 3.0f;
    [Export] public float LaneGateMarkerRadiusU { get; set; } = 1.5f;
    [Export] public float DiscoverySiteMarkerRadiusU { get; set; } = 1.0f;
    // GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001
    [Export] public float FleetOrbitRadiusU { get; set; } = 50.0f;
    [Export] public float FleetMarkerRadiusU { get; set; } = 1.2f;
    [Export] public PackedScene StationPrefab { get; set; }

    // Local system state
    private Node3D _localSystemRoot;
    private string _currentNodeId = "";

    // Galaxy overlay camera (sibling node); repositioned in RefreshFromSnapshotV0 to frame visible nodes.
    private Camera3D _overlayCamera;

    public override void _Ready()
    {
        _bridge = GetNodeOrNull<SimBridge>("/root/SimBridge");
        _overlayCamera = GetParent()?.GetNodeOrNull<Camera3D>("GalaxyOverlayCamera");

        // Default OFF: the playable prototype is a local-space view until Tab opens the overlay.
        Visible = false;
        SetProcess(false);

        // Allocate local system container; it will be added to the parent in the deferred boot call
        // (adding children during _Ready while the parent is busy building children is not allowed).
        _localSystemRoot = new Node3D { Name = "LocalSystem" };

        // Defer one frame so SimBridge finishes its own _Ready before we query it.
        CallDeferred(nameof(DrawLocalSystemBootV0));
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

        // Show/hide local system opposite to overlay: open=hide, closed=show.
        if (_localSystemRoot != null)
            _localSystemRoot.Visible = !isOpen;

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

    // Returns counts of local system objects via scene groups for headless proof.
    public Godot.Collections.Dictionary GetLocalSystemMetricsV0()
    {
        int starCount = GetTree().GetNodesInGroup("LocalStar").Count;
        int stationCount = GetTree().GetNodesInGroup("Station").Count;
        int laneGateCount = GetTree().GetNodesInGroup("LaneGate").Count;
        int discoverySiteCount = GetTree().GetNodesInGroup("DiscoverySite").Count;
        return new Godot.Collections.Dictionary
        {
            ["star_count"] = starCount,
            ["station_count"] = stationCount,
            ["lane_gate_count"] = laneGateCount,
            ["discovery_site_count"] = discoverySiteCount
        };
    }

    public override void _Process(double delta)
    {
        if (!_overlayOpen) return;
        if (_bridge == null || _bridge.IsLoading) return;

        RefreshFromSnapshotV0();
    }

    // --- Local system rendering ---

    // Called deferred from _Ready: adds local system root to scene then draws.
    // Deferred ensures the parent has finished building children before we call AddChild.
    private void DrawLocalSystemBootV0()
    {
        if (_bridge == null) return;

        // Safe to AddChild here — _Ready is complete and the parent is no longer busy.
        if (_localSystemRoot != null && _localSystemRoot.GetParent() == null)
        {
            GetParent().AddChild(_localSystemRoot);
        }

        var galaxySnap = _bridge.GetGalaxySnapshotV0();
        if (galaxySnap == null) return;

        var nodeId = galaxySnap.ContainsKey("player_current_node_id")
            ? (string)galaxySnap["player_current_node_id"]
            : "";

        if (string.IsNullOrEmpty(nodeId)) return;

        DrawLocalSystemV0(nodeId);
    }

    // GDScript-callable: tears down the current local system and rebuilds for the given nodeId.
    // Called by game_manager.on_lane_arrival_v0 after the hero ship completes a lane transit.
    public void RebuildLocalSystemV0(string nodeId)
    {
        DrawLocalSystemV0(nodeId);
    }

    // Spawns local system interior from GetSystemSnapshotV0: star, station, lane gates, discovery sites.
    // All positions are seed-derived (deterministic, no wall-clock).
    private void DrawLocalSystemV0(string nodeId)
    {
        ClearLocalSystemV0();
        _currentNodeId = nodeId;

        if (_bridge == null) return;

        var snap = _bridge.GetSystemSnapshotV0(nodeId);
        if (snap == null) return;

        // 1. Star mesh at origin.
        var star = CreateStarMeshV0();
        star.AddToGroup("LocalStar");
        _localSystemRoot.AddChild(star);

        // 2. Station at seed-derived orbit position.
        SpawnStationV0(snap, nodeId);

        // 3. Lane gate markers (one per neighbor).
        SpawnLaneGatesV0(snap);

        // 4. Discovery site markers at seed-derived orbit positions.
        SpawnDiscoverySitesV0(snap, nodeId);

        // 5. Fleet markers at seed-derived orbit positions (GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001).
        SpawnFleetsV0(snap);
    }

    private void ClearLocalSystemV0()
    {
        if (_localSystemRoot == null) return;
        foreach (Node child in _localSystemRoot.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void SpawnStationV0(Godot.Collections.Dictionary snap, string nodeId)
    {
        var stationDict = snap.ContainsKey("station")
            ? snap["station"].AsGodotDictionary()
            : null;
        if (stationDict == null) return;

        var stationId = stationDict.ContainsKey("node_id")
            ? (string)stationDict["node_id"]
            : nodeId;

        // Station as Area3D so body_entered fires when the player ship (collision_layer=2) enters.
        var station = new Area3D
        {
            Name = "LocalStation_" + stationId,
            Monitoring = true,
            Monitorable = true,
            CollisionLayer = 0,  // Stations don't need their own layer.
            CollisionMask = 2,   // Detect Ships layer (player RigidBody3D).
        };

        var collider = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(10f, 5f, 10f) }
        };
        station.AddChild(collider);

        var mesh = new MeshInstance3D
        {
            Name = "StationMesh",
            Mesh = new BoxMesh { Size = new Vector3(10f, 5f, 10f) },
            MaterialOverride = new StandardMaterial3D
            {
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = new Color(0f, 1f, 0f, 0.3f)
            }
        };
        station.AddChild(mesh);

        station.Position = DeriveOrbitPositionV0(nodeId + "_station", StationOrbitRadiusU);
        station.AddToGroup("Station");
        RegisterDockTargetV0(station, "STATION", stationId);

        // Connect body_entered → GameManager.on_proximity_dock_entered_v0(station).
        station.BodyEntered += (body) =>
        {
            var gm = GetNode<Node>("/root/GameManager");
            if (gm != null && gm.HasMethod("on_proximity_dock_entered_v0"))
                gm.Call("on_proximity_dock_entered_v0", station);
        };

        _localSystemRoot.AddChild(station);
    }

    private void SpawnLaneGatesV0(Godot.Collections.Dictionary snap)
    {
        if (!snap.ContainsKey("lane_gate")) return;
        var gates = snap["lane_gate"].AsGodotArray();

        for (int i = 0; i < gates.Count; i++)
        {
            var g = gates[i].AsGodotDictionary();
            var neighborId = g.ContainsKey("neighbor_node_id")
                ? (string)g["neighbor_node_id"]
                : "";
            var displayName = g.ContainsKey("neighbor_display_name")
                ? (string)g["neighbor_display_name"]
                : "";

            var marker = CreateLaneGateMarkerV0(neighborId, displayName);
            marker.Position = DeriveLaneGatePositionV0(i, gates.Count, LaneGateDistanceU);
            marker.AddToGroup("LaneGate");
            _localSystemRoot.AddChild(marker);
        }
    }

    private void SpawnDiscoverySitesV0(Godot.Collections.Dictionary snap, string nodeId)
    {
        if (!snap.ContainsKey("discovery_sites")) return;
        var sites = snap["discovery_sites"].AsGodotArray();

        for (int i = 0; i < sites.Count; i++)
        {
            var s = sites[i].AsGodotDictionary();
            var siteId = s.ContainsKey("site_id") ? (string)s["site_id"] : (nodeId + "_site_" + i);

            var marker = CreateDiscoverySiteMarkerV0(siteId);
            marker.Position = DeriveOrbitPositionV0(siteId + "_discovery", DiscoverySiteOrbitRadiusU);
            marker.AddToGroup("DiscoverySite");
            _localSystemRoot.AddChild(marker);
        }
    }

    // GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001
    private void SpawnFleetsV0(Godot.Collections.Dictionary snap)
    {
        if (!snap.ContainsKey("fleets")) return;
        var fleets = snap["fleets"].AsGodotArray();

        for (int i = 0; i < fleets.Count; i++)
        {
            var f = fleets[i].AsGodotDictionary();
            var fleetId = f.ContainsKey("fleet_id") ? (string)f["fleet_id"] : "";
            if (string.IsNullOrEmpty(fleetId)) continue;

            var marker = CreateFleetMarkerV0(fleetId);
            marker.Position = DeriveOrbitPositionV0(fleetId + "_local", FleetOrbitRadiusU);
            marker.AddToGroup("FleetShip");
            _localSystemRoot.AddChild(marker);
        }

        // Init combat HP for all fleets (idempotent).
        var bridge = GetNodeOrNull<Node>("/root/SimBridge");
        if (bridge != null && bridge.HasMethod("InitFleetCombatHpV0"))
            bridge.Call("InitFleetCombatHpV0");
    }

    private Node3D CreateFleetMarkerV0(string fleetId)
    {
        var root = new Node3D { Name = "Fleet_" + fleetId };

        var mesh = new MeshInstance3D
        {
            Name = "FleetMesh",
            Mesh = new SphereMesh { Radius = FleetMarkerRadiusU },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.9f, 0.2f, 0.2f) // Red for enemy fleets
            }
        };
        root.AddChild(mesh);

        // Proximity trigger + bullet target: player RigidBody3D and bullets detect this.
        var area = new Area3D
        {
            Name = "FleetArea",
            Monitoring = true,
            Monitorable = true,
            CollisionLayer = 4,  // FleetTarget layer (bit 2) — player bullets detect this.
            CollisionMask = 2,   // Detect Ships layer (player RigidBody3D).
        };
        var shape = new CollisionShape3D
        {
            Name = "FleetShape",
            Shape = new SphereShape3D { Radius = FleetMarkerRadiusU * 4.0f }
        };
        area.AddChild(shape);
        area.SetMeta("fleet_id", fleetId);
        area.BodyEntered += (body) => _OnFleetBodyEnteredV0(body, fleetId);
        root.AddChild(area);

        return root;
    }

    private void _OnFleetBodyEnteredV0(Node3D body, string fleetId)
    {
        if (!body.IsInGroup("Player")) return;
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm == null) return;
        if (gm.HasMethod("on_fleet_proximity_entered_v0"))
            gm.Call("on_fleet_proximity_entered_v0", fleetId);
    }

    private Node3D CreateStarMeshV0()
    {
        var root = new Node3D { Name = "LocalStar" };

        var mesh = new MeshInstance3D
        {
            Name = "StarMesh",
            Mesh = new SphereMesh { Radius = StarVisualRadiusU },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1.0f, 0.8f, 0.2f),
                EmissionEnabled = true,
                Emission = new Color(1.0f, 0.8f, 0.2f),
                EmissionEnergyMultiplier = 3.0f
            }
        };

        root.AddChild(mesh);
        root.Position = Vector3.Zero;
        return root;
    }

    // GATE.S1.HERO_SHIP_LOOP.LANE_GATE_LABEL.001: displayName from NeighborDisplayName; falls back to neighborId.
    private Node3D CreateLaneGateMarkerV0(string neighborId, string displayName = "")
    {
        var root = new Node3D { Name = "LaneGate_" + neighborId };

        var mesh = new MeshInstance3D
        {
            Name = "LaneGateMesh",
            Mesh = new SphereMesh { Radius = LaneGateMarkerRadiusU },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.3f, 0.3f, 1.0f),
                EmissionEnabled = true,
                Emission = new Color(0.3f, 0.3f, 1.0f),
                EmissionEnergyMultiplier = 1.5f
            }
        };
        root.AddChild(mesh);

        var lbl = new Label3D
        {
            Name = "GateLabel",
            Text = "\u2192 " + (string.IsNullOrEmpty(displayName) ? neighborId : displayName),
            PixelSize = 0.01f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        };
        lbl.Position = new Vector3(0f, LaneGateMarkerRadiusU + 0.5f, 0f);
        root.AddChild(lbl);

        // Proximity trigger: player RigidBody3D entering this area notifies GameManager.
        var area = new Area3D
        {
            Name = "LaneGateArea",
            Monitoring = true,
            Monitorable = true,
            CollisionLayer = 0,
            CollisionMask = 2,   // Detect Ships layer (player RigidBody3D).
        };
        var shape = new CollisionShape3D
        {
            Name = "LaneGateShape",
            Shape = new SphereShape3D { Radius = LaneGateMarkerRadiusU * 4.0f }
        };
        area.AddChild(shape);
        // Store neighbor id for the signal handler to forward.
        area.SetMeta("lane_neighbor_id", neighborId);
        area.BodyEntered += (body) => _OnLaneGateBodyEnteredV0(body, neighborId);
        root.AddChild(area);

        return root;
    }

    private void _OnLaneGateBodyEnteredV0(Node3D body, string neighborId)
    {
        if (!body.IsInGroup("Player")) return;
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm == null) return;
        if (gm.HasMethod("on_lane_gate_proximity_entered_v0"))
            gm.Call("on_lane_gate_proximity_entered_v0", neighborId);
    }

    private Node3D CreateDiscoverySiteMarkerV0(string siteId)
    {
        var root = new Node3D { Name = "DiscoverySite_" + siteId };

        var mesh = new MeshInstance3D
        {
            Name = "SiteMesh",
            Mesh = new SphereMesh { Radius = DiscoverySiteMarkerRadiusU },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1.0f, 0.6f, 0.0f)
            }
        };
        root.AddChild(mesh);

        // Proximity trigger: player RigidBody3D entering this area notifies GameManager.
        var area = new Area3D
        {
            Name = "DiscoverySiteArea",
            Monitoring = true,
            Monitorable = true,
            CollisionLayer = 0,
            CollisionMask = 2,   // Detect Ships layer (player RigidBody3D).
        };
        var shape = new CollisionShape3D
        {
            Name = "DiscoverySiteShape",
            Shape = new SphereShape3D { Radius = DiscoverySiteMarkerRadiusU * 4.0f }
        };
        area.AddChild(shape);
        area.SetMeta("discovery_site_id", siteId);
        area.BodyEntered += (body) => _OnDiscoverySiteBodyEnteredV0(body, siteId);
        root.AddChild(area);

        return root;
    }

    private void _OnDiscoverySiteBodyEnteredV0(Node3D body, string siteId)
    {
        if (!body.IsInGroup("Player")) return;
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm == null) return;
        if (gm.HasMethod("on_discovery_site_proximity_entered_v0"))
            gm.Call("on_discovery_site_proximity_entered_v0", siteId);
    }

    // --- Deterministic orbit position helpers ---

    // FNV-1a 64-bit hash: GameShell-only math, no SimCore dependency.
    private static ulong Fnv1a64(string s)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong h = offset;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= (byte)s[i];
                h *= prime;
            }
            return h;
        }
    }

    // Deterministic XZ orbit position from seedKey hash. Y=0 (local physics plane).
    private static Vector3 DeriveOrbitPositionV0(string seedKey, float radius)
    {
        var hash = Fnv1a64(seedKey);
        float angle = (float)(hash % 360UL) * (MathF.PI / 180f);
        return new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
    }

    // Evenly-spaced XZ positions for lane gate markers (deterministic by index+total).
    private static Vector3 DeriveLaneGatePositionV0(int index, int total, float distance)
    {
        float angle = total > 0 ? ((float)index / total) * 2f * MathF.PI : 0f;
        return new Vector3(MathF.Cos(angle) * distance, 0f, MathF.Sin(angle) * distance);
    }

    // --- Galaxy overlay rendering ---

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

            int fleetCount = n.ContainsKey("fleet_count") ? (int)(Variant)n["fleet_count"] : 0;
            nodes.Add(new NodeSnapV0(nodeId, stateToken, displayText, new Vector3(x, y, z), fleetCount));
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

        _lastEdgeCount = edges.Count;

        // Nodes: create/update visuals (HIDDEN nodes are suppressed from the overlay).
        bool playerHighlighted = false;
        int renderedNodeCount = 0;
        Vector3 playerNodePos = Vector3.Zero;
        bool playerNodePosFound = false;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (string.IsNullOrEmpty(n.NodeId)) continue;

            bool isHidden = StringComparer.Ordinal.Equals(n.DisplayStateToken, "HIDDEN");

            if (!_nodeRootsById.TryGetValue(n.NodeId, out var root))
            {
                if (isHidden) continue; // Never create a visual for HIDDEN nodes.
                root = CreateNodeVisualV0(n.NodeId);
                _nodeRootsById[n.NodeId] = root;
                AddChild(root);
            }
            else if (isHidden)
            {
                // Node transitioned to HIDDEN — remove its visual.
                root.QueueFree();
                _nodeRootsById.Remove(n.NodeId);
                continue;
            }

            renderedNodeCount++;
            root.Position = n.Position;

            var label = root.GetNodeOrNull<Label3D>("NodeLabel");
            if (label != null)
            {
                // Token contract: RUMORED => "???", VISITED => name, MAPPED => name+count.
                label.Text = StringComparer.Ordinal.Equals(n.DisplayStateToken, "RUMORED")
                    ? "???"
                    : (n.DisplayText ?? "");
            }

            var fleetLabel = root.GetNodeOrNull<Label3D>("FleetLabel");
            if (fleetLabel != null)
                fleetLabel.Text = n.FleetCount > 0 ? "[" + n.FleetCount + " fleets]" : "";

            var mesh = root.GetNodeOrNull<MeshInstance3D>("NodeMesh");
            if (mesh != null && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                bool isPlayer = !string.IsNullOrEmpty(playerNodeId) && StringComparer.Ordinal.Equals(n.NodeId, playerNodeId);
                if (isPlayer)
                {
                    playerHighlighted = true;
                    playerNodePos = n.Position;
                    playerNodePosFound = true;
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

        _lastNodeCount = renderedNodeCount;
        _lastPlayerHighlighted = playerHighlighted;

        // Reposition overlay camera so the player's current system is always in frame.
        // The camera keeps its 45° downward tilt (Transform3D basis from scene) and shifts
        // its XZ position to be above+behind the player node.
        if (_overlayCamera != null && playerNodePosFound)
        {
            var t = _overlayCamera.Transform;
            t.Origin = new Vector3(playerNodePos.X, 150f, playerNodePos.Z + 150f);
            _overlayCamera.Transform = t;
        }

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
        mesh.Mesh = new SphereMesh { Radius = 5.0f };

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
            PixelSize = 0.05f
        };
        lbl.Position = new Vector3(0, 8.0f, 0);
        root.AddChild(lbl);

        // GATE.S1.GALAXY_MAP.FLEET_COUNTS.001: fleet count overlay label (hidden when zero).
        var fleetLbl = new Label3D
        {
            Name = "FleetLabel",
            Text = "",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.05f,
            Modulate = new Color(1.0f, 0.8f, 0.2f)
        };
        fleetLbl.Position = new Vector3(0, 11.0f, 0);
        root.AddChild(fleetLbl);

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
        public readonly int FleetCount;

        public NodeSnapV0(string nodeId, string displayStateToken, string displayText, Vector3 position, int fleetCount = 0)
        {
            NodeId = nodeId ?? "";
            DisplayStateToken = displayStateToken ?? "";
            DisplayText = displayText ?? "";
            Position = position;
            FleetCount = fleetCount;
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
