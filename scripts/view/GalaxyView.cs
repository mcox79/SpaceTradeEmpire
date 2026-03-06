using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

// GATE.S6.MAP_GALAXY.OVERLAY_SYS.001: Galaxy map overlay mode.
public enum GalaxyOverlayMode
{
    Default = 0,       // Security coloring (existing behavior)
    TradeFlow = 1,     // Trade route profitability + NPC volume
    IntelFreshness = 2 // Node intel age coloring
}

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;

    private bool _overlayOpen = false;

    // GATE.S6.MAP_GALAXY.OVERLAY_SYS.001: Active overlay mode.
    private GalaxyOverlayMode _currentOverlayMode = GalaxyOverlayMode.Default;
    private bool _cameraPositionedThisOpen = false;

    // Visual caches (deterministic keys)
    private readonly Dictionary<string, Node3D> _nodeRootsById = new();
    private readonly Dictionary<string, MeshInstance3D> _edgeMeshesByKey = new();

    // GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001: Trade flow edge cache (populated per refresh).
    private readonly HashSet<string> _tradeFlowEdges = new();
    // GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001: Intel freshness cache keyed by node_id.
    private readonly Dictionary<string, long> _intelAgeByNode = new();

    // GATE.S6.MAP_GALAXY.NODE_CLICK.001: Node detail popup reference.
    private Node _nodeDetailPopup;

    // GATE.S11.GAME_FEEL.NPC_ROUTE_VIS.001: NPC route line cache.
    private readonly Dictionary<string, MeshInstance3D> _npcRouteMeshesByKey = new();

    // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Animated flow dots on NPC trade routes.
    private readonly Dictionary<string, MeshInstance3D> _flowDotsByKey = new();
    private double _flowAnimTime = 0.0;

    // GATE.S12.NPC_CIRC.VOLUME_LABELS.001: Trade volume labels on edges.
    private readonly Dictionary<string, Label3D> _volumeLabelsByKey = new();

    private int _lastNodeCount = 0;
    private int _lastEdgeCount = 0;
    private bool _lastPlayerHighlighted = false;

    // --- Local system config (named exported fields; no numeric literals in .cs or .tscn) ---
    [Export] public float SystemSceneRadiusU { get; set; } = 120.0f;
    [Export] public float StationOrbitRadiusU { get; set; } = 60.0f;
    [Export] public float LaneGateDistanceU { get; set; } = 90.0f;
    [Export] public float DiscoverySiteOrbitRadiusU { get; set; } = 55.0f;
    [Export] public float StarVisualRadiusU { get; set; } = 6.0f;
    [Export] public float LaneGateMarkerRadiusU { get; set; } = 1.5f;
    [Export] public float DiscoverySiteMarkerRadiusU { get; set; } = 1.0f;
    // GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001
    [Export] public float FleetOrbitRadiusU { get; set; } = 65.0f;
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
        _cameraPositionedThisOpen = false;

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

        // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Accumulate time for flow dot animation.
        _flowAnimTime += delta;

        RefreshFromSnapshotV0();
    }

    // GATE.S6.MAP_GALAXY.NODE_CLICK.001: Click detection on galaxy overlay nodes.
    // Projects all node positions to screen space and picks the closest within threshold.
    [Export] public float NodeClickThresholdPx { get; set; } = 30.0f;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_overlayOpen) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        if (_overlayCamera == null || !_overlayCamera.Current) return;

        var clickPos = mb.Position;
        string closestNodeId = null;
        float closestDist = NodeClickThresholdPx;

        foreach (var kv in _nodeRootsById)
        {
            var root = kv.Value;
            if (root == null || !root.IsInsideTree()) continue;

            // Only pick visible nodes
            if (!root.Visible) continue;

            var worldPos = root.GlobalPosition;
            if (_overlayCamera.IsPositionBehind(worldPos)) continue;

            var screenPos = _overlayCamera.UnprojectPosition(worldPos);
            float dist = screenPos.DistanceTo(clickPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestNodeId = kv.Key;
            }
        }

        if (closestNodeId != null)
        {
            ShowNodeDetailPopupV0(closestNodeId, clickPos);
            GetViewport().SetInputAsHandled();
        }
    }

    private void ShowNodeDetailPopupV0(string nodeId, Vector2 screenPos)
    {
        // Lazy-load popup from autoload or create if needed
        if (_nodeDetailPopup == null || !GodotObject.IsInstanceValid(_nodeDetailPopup))
        {
            var script = GD.Load<Script>("res://scripts/ui/node_detail_popup.gd");
            if (script == null) return;
            _nodeDetailPopup = new CanvasLayer();
            _nodeDetailPopup.SetScript(script);
            _nodeDetailPopup.Name = "NodeDetailPopup";
            GetTree().Root.AddChild(_nodeDetailPopup);
        }

        if (_nodeDetailPopup.HasMethod("show_for_node"))
            _nodeDetailPopup.Call("show_for_node", nodeId, screenPos);
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

        // Query star info upfront — luminosity drives orbit scaling.
        float r = 1.0f, g = 0.8f, b = 0.2f;
        int luminosityBps = 10000;
        string starClass = "ClassG";
        if (_bridge.HasMethod("GetStarInfoV0"))
        {
            var sInfo = _bridge.Call("GetStarInfoV0", nodeId).AsGodotDictionary();
            if (sInfo != null && sInfo.Count > 0)
            {
                r = sInfo.ContainsKey("color_r") ? (float)sInfo["color_r"] : r;
                g = sInfo.ContainsKey("color_g") ? (float)sInfo["color_g"] : g;
                b = sInfo.ContainsKey("color_b") ? (float)sInfo["color_b"] : b;
                luminosityBps = sInfo.ContainsKey("luminosity_bps") ? (int)sInfo["luminosity_bps"] : 10000;
                starClass = sInfo.ContainsKey("star_class") ? (string)sInfo["star_class"] : "ClassG";
            }
        }
        var starColor = new Color(r, g, b);
        float lumScale = MathF.Sqrt(luminosityBps / 10000.0f);

        // 1. Primary star at origin.
        var star = CreateStarMeshV0(starColor, starClass);
        star.AddToGroup("LocalStar");
        _localSystemRoot.AddChild(star);

        // 1a. Binary companion (~20% chance, seeded).
        SpawnBinaryCompanionV0(nodeId, starColor, starClass);

        // 1b. Planet orbiting the star.
        var (planetPos, planetType) = SpawnLocalPlanetV0(nodeId, lumScale);

        // 1c. Moons around the planet.
        SpawnMoonsV0(nodeId, planetPos, planetType);

        // 1d. Asteroid belt between inner and outer zones.
        SpawnAsteroidBeltV0(nodeId, lumScale);

        // 2. Station orbiting near the planet.
        SpawnStationV0(snap, nodeId, planetPos);

        // 3. Lane gate markers (one per neighbor).
        SpawnLaneGatesV0(snap);

        // 4. Discovery site markers at seed-derived orbit positions.
        SpawnDiscoverySitesV0(snap, nodeId);

        // 5. Fleet markers at seed-derived orbit positions.
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

    private void SpawnStationV0(Godot.Collections.Dictionary snap, string nodeId, Vector3 planetPos)
    {
        var stationDict = snap.ContainsKey("station")
            ? snap["station"].AsGodotDictionary()
            : null;
        if (stationDict == null) return;

        var stationId = stationDict.ContainsKey("node_id")
            ? (string)stationDict["node_id"]
            : nodeId;
        var stationDisplayName = stationDict.ContainsKey("node_name") && !string.IsNullOrEmpty((string)stationDict["node_name"])
            ? (string)stationDict["node_name"] + " Station"
            : SimBridge.FormatDisplayNameV0(stationId);

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

        // GATE.S1.VISUAL_POLISH.STRUCTURES.001: ring/cylinder station geometry with slow rotation.
        var stationVisual = new Node3D { Name = "StationVisual" };
        // Attach spinning script for slow Y-axis rotation.
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null) stationVisual.SetScript(spinScript);

        var hullMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.22f, 0.26f, 0.30f),
            Roughness = 0.55f,
            Metallic = 0.45f
        };
        var hubMesh = new MeshInstance3D
        {
            Name = "StationHub",
            Mesh = new CylinderMesh
            {
                TopRadius = 2.8f,
                BottomRadius = 2.8f,
                Height = 4.0f,
                RadialSegments = 12
            },
            MaterialOverride = hullMat
        };
        stationVisual.AddChild(hubMesh);

        var ringMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.25f, 0.30f, 0.38f),
            Roughness = 0.40f,
            Metallic = 0.65f
        };
        var ringMesh = new MeshInstance3D
        {
            Name = "StationRing",
            Mesh = new TorusMesh
            {
                InnerRadius = 4.8f,
                OuterRadius = 5.8f,
                Rings = 24,
                RingSegments = 12
            },
            MaterialOverride = ringMat
        };
        // Torus lies in XZ plane by default — rotate to be horizontal around the hub.
        ringMesh.RotateX(Mathf.Pi / 2.0f);
        stationVisual.AddChild(ringMesh);

        var accentMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.3f, 0.7f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.7f, 1.0f),
            EmissionEnergyMultiplier = 2.5f
        };
        var accentBand = new MeshInstance3D
        {
            Name = "StationAccent",
            Mesh = new TorusMesh
            {
                InnerRadius = 5.5f,
                OuterRadius = 5.9f,
                Rings = 24,
                RingSegments = 8
            },
            MaterialOverride = accentMat
        };
        accentBand.RotateX(Mathf.Pi / 2.0f);
        stationVisual.AddChild(accentBand);

        station.AddChild(stationVisual);

        // Invisible legacy mesh kept so callers searching for "StationMesh" by name still work.
        var mesh = new MeshInstance3D
        {
            Name = "StationMesh",
            Visible = false,
            Mesh = new BoxMesh { Size = new Vector3(10f, 5f, 10f) },
            MaterialOverride = new StandardMaterial3D
            {
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = new Color(0f, 1f, 0f, 0.3f)
            }
        };
        station.AddChild(mesh);

        // GATE.S1.VISUAL_POLISH.HUD_LABELS.001: Label3D over station showing station name.
        var stationLabel = new Label3D
        {
            Name = "StationLabel",
            Text = stationDisplayName,
            PixelSize = 0.12f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(0.4f, 1.0f, 0.4f)
        };
        stationLabel.Position = new Vector3(0f, 8f, 0f);
        station.AddChild(stationLabel);

        // Station orbits near the planet (12u offset from planet position).
        station.Position = planetPos + DeriveOrbitPositionV0(nodeId + "_station_offset", 12.0f);
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

    // GATE.S12.FLEET_SUBSTANCE.QUATERNIUS.001: Load Quaternius .tscn model by FleetRole and return as a scaled Node3D.
    // GATE.S12.FLEET_SUBSTANCE.VARIETY.001: Hash-based model variants + player ship.
    private static readonly string[] TraderModels = { "dispatcher", "pancake", "omen" };
    private static readonly string[] TraderColors = { "blue", "green", "orange" };
    private static readonly string[] HaulerModels = { "bob", "zenith" };
    private static readonly string[] HaulerColors = { "blue", "green", "orange" };
    private static readonly string[] PatrolModels = { "spitfire", "striker", "insurgent" };
    private static readonly string[] PatrolColors = { "blue", "red", "orange" };

    private Node3D LoadFleetModelV0(string fleetId)
    {
        string modelPath;

        if (StringComparer.Ordinal.Equals(fleetId, "fleet_trader_1"))
        {
            // Player fleet always uses challenger_blue.
            modelPath = "res://addons/quaternius-ultimate-spaceships-pack/meshes/challenger/challenger_blue.tscn";
        }
        else
        {
            // Hash-based selection for NPC fleets.
            uint hash = 0;
            foreach (char c in fleetId) { hash = hash * 31 + (uint)c; }

            int roleInt = (_bridge != null) ? _bridge.GetFleetRoleV0(fleetId) : 0;
            string[] models;
            string[] colors;
            switch (roleInt)
            {
                case 1: models = HaulerModels; colors = HaulerColors; break;
                case 2: models = PatrolModels; colors = PatrolColors; break;
                default: models = TraderModels; colors = TraderColors; break;
            }

            string modelName = models[hash % (uint)models.Length];
            string colorName = colors[(hash / 7) % (uint)colors.Length];
            modelPath = $"res://addons/quaternius-ultimate-spaceships-pack/meshes/{modelName}/{modelName}_{colorName}.tscn";
        }

        Node3D model = null;
        if (Godot.FileAccess.FileExists(modelPath))
        {
            var scene = GD.Load<PackedScene>(modelPath);
            if (scene != null)
                model = scene.Instantiate<Node3D>();
        }

        if (model == null)
        {
            // Fallback: try Kenney craft_racer if Quaternius model failed to load.
            const string FallbackPath = "res://addons/kenney_space_kit/Models/GLTF format/craft_racer.glb";
            if (Godot.FileAccess.FileExists(FallbackPath))
            {
                var scene = GD.Load<PackedScene>(FallbackPath);
                if (scene != null)
                    model = scene.Instantiate<Node3D>();
            }
        }

        if (model != null)
        {
            model.Name = "FleetModel";
            model.Scale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        return model;
    }

    private Node3D CreateFleetMarkerV0(string fleetId)
    {
        var root = new Node3D { Name = "Fleet_" + fleetId };

        // GATE.S12.FLEET_SUBSTANCE.QUATERNIUS.001: Quaternius model by FleetRole.
        var fleetModel = LoadFleetModelV0(fleetId);
        if (fleetModel != null)
            root.AddChild(fleetModel);

        // Placeholder mesh for legacy code that looked for "FleetMesh" by name — kept hidden.
        var mesh = new MeshInstance3D
        {
            Name = "FleetMesh",
            Visible = false,
            Mesh = new SphereMesh { Radius = FleetMarkerRadiusU },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.9f, 0.2f, 0.2f) // Red for enemy fleets
            }
        };
        root.AddChild(mesh);

        // GATE.S1.VISUAL_POLISH.HUD_LABELS.001: Label3D over fleet showing role name.
        int fleetRole = _bridge != null && _bridge.HasMethod("GetFleetRoleV0")
            ? (int)_bridge.Call("GetFleetRoleV0", fleetId) : 0;
        string fleetDisplayName = fleetRole switch
        {
            2 => "Patrol",
            1 => "Hauler",
            _ => "Trader"
        };
        var fleetLabel = new Label3D
        {
            Name = "FleetLabel",
            Text = fleetDisplayName,
            PixelSize = 0.12f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(1.0f, 0.4f, 0.4f)
        };
        fleetLabel.Position = new Vector3(0f, FleetMarkerRadiusU * 2.0f + 0.5f, 0f);
        root.AddChild(fleetLabel);

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

        // GATE.S1.VISUAL_POLISH.FLEET_AI.001: attach fleet_ai.gd for autonomous patrol/dock/engage movement.
        // All scene-spawned fleet markers are hostile (player fleet is the Player RigidBody3D, not a marker).
        // spawn_origin is NOT set here — fleet_ai.gd captures global_position in _ready() after the node
        // has been inserted into the scene tree with its final position assigned.
        var fleetAiScript = GD.Load<Script>("res://scripts/core/fleet_ai.gd");
        if (fleetAiScript != null)
        {
            root.SetScript(fleetAiScript);
            root.SetMeta("is_hostile", true);
        }

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

    private Node3D CreateStarMeshV0(Color starColor, string starClass, float scaleMult = 1.0f)
    {
        // Star visual size scales with class (blue giants big, red dwarfs small).
        float classScale = StarClassVisualScaleV0(starClass) * scaleMult;

        const string StarScenePath = "res://addons/naejimer_3d_planet_generator/scenes/star.tscn";
        Node3D starNode = null;
        if (Godot.FileAccess.FileExists(StarScenePath))
        {
            var scene = GD.Load<PackedScene>(StarScenePath);
            if (scene != null)
            {
                starNode = scene.Instantiate<Node3D>();
                TintStarShaderV0(starNode, starColor);
                ActivateAnimationTreeV0(starNode);
            }
        }

        if (starNode == null)
        {
            starNode = new Node3D { Name = "StarFallback" };
            var mesh = new MeshInstance3D
            {
                Name = "StarMesh",
                Mesh = new SphereMesh { Radius = StarVisualRadiusU * classScale },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = starColor,
                    EmissionEnabled = true,
                    Emission = starColor,
                    EmissionEnergyMultiplier = 3.0f
                }
            };
            starNode.AddChild(mesh);
        }

        // Star scene has ~1200 unit baked scale. Scale to match StarVisualRadiusU * classScale.
        var container = new Node3D { Name = "LocalStar" };
        float s = (StarVisualRadiusU * classScale) / 1200.0f;
        container.Scale = new Vector3(s, s, s);
        container.Position = Vector3.Zero;
        container.AddChild(starNode);

        // Add a point light so the star actually illuminates ships/stations/planets.
        var starLight = new OmniLight3D
        {
            Name = "StarLight",
            LightColor = new Color(
                Mathf.Min(starColor.R * 0.8f + 0.2f, 1.0f),
                Mathf.Min(starColor.G * 0.8f + 0.2f, 1.0f),
                Mathf.Min(starColor.B * 0.8f + 0.2f, 1.0f)),
            LightEnergy = 6.0f * classScale,
            OmniRange = 200.0f,
            OmniAttenuation = 0.5f,
            ShadowEnabled = false,
        };
        // Light at world origin (star center), not inside the scaled container.
        _localSystemRoot.CallDeferred("add_child", starLight);

        return container;
    }

    private static float StarClassVisualScaleV0(string starClass) => starClass switch
    {
        "ClassO" => 1.8f,   // Blue giant
        "ClassA" => 1.3f,   // White
        "ClassF" => 1.1f,   // White-yellow
        "ClassG" => 1.0f,   // Sol baseline
        "ClassK" => 0.85f,  // Orange
        "ClassM" => 0.6f,   // Red dwarf
        _ => 1.0f,
    };

    private static void TintStarShaderV0(Node3D starNode, Color starColor)
    {
        // Derive a dark and bright variant from the star class color.
        var darkColor = new Color(starColor.R * 0.4f, starColor.G * 0.12f, starColor.B * 0.0f);
        var brightColor = new Color(
            Mathf.Min(starColor.R, 1.0f),
            Mathf.Min(starColor.G * 0.7f, 1.0f),
            Mathf.Min(starColor.B * 0.3f, 1.0f));

        // Tint body shader (root MeshInstance3D).
        if (starNode is MeshInstance3D bodyMesh && bodyMesh.Mesh != null)
        {
            var mat = bodyMesh.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
            if (mat != null)
            {
                mat.SetShaderParameter("color_1", darkColor);
                mat.SetShaderParameter("color_2", starColor);
                mat.SetShaderParameter("color_3", brightColor);
                mat.SetShaderParameter("color_4", starColor);
                mat.SetShaderParameter("color_5", brightColor);
            }
        }

        // Tint atmosphere shader (child named "Atmosphere").
        var atmo = starNode.GetNodeOrNull<MeshInstance3D>("Atmosphere");
        if (atmo != null && atmo.Mesh != null)
        {
            var mat = atmo.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
            if (mat != null)
            {
                mat.SetShaderParameter("color_1", darkColor);
                mat.SetShaderParameter("color_2", brightColor);
            }
        }
    }

    // Base planet orbit radius by type. Scaled by lumScale at call site.
    // Star visual radius ~6u, so innermost orbit starts well clear.
    private static float PlanetBaseOrbitV0(string planetType) => planetType switch
    {
        "Lava"        => 20.0f,   // Innermost — volcanic, near star
        "Sand"        => 23.0f,   // Inner zone
        "Terrestrial" => 26.0f,   // Habitable zone
        "Barren"      => 29.0f,   // Outer rocky
        "Ice"         => 32.0f,   // Outer cold zone
        "Gaseous"     => 38.0f,   // Far out — gas giant
        _             => 26.0f,
    };

    // Planet visual scale by type. Star is ~6u radius, planets must be smaller.
    // Addon scenes have ~400u baked scale, so 0.01 → ~4u visible radius.
    private static float PlanetVisualScaleV0(string planetType) => planetType switch
    {
        "Gaseous"     => 0.015f,   // ~6u — gas giant, nearly star-sized
        "Terrestrial" => 0.010f,   // ~4u
        "Ice"         => 0.008f,   // ~3.2u
        "Sand"        => 0.008f,   // ~3.2u
        "Lava"        => 0.007f,   // ~2.8u
        "Barren"      => 0.006f,   // ~2.4u
        _             => 0.010f,
    };

    // Binary star companion — ~20% of systems are binaries (seeded).
    private void SpawnBinaryCompanionV0(string nodeId, Color starColor, string starClass)
    {
        var hash = Fnv1a64(nodeId + "_binary");
        if (hash % 100UL >= 20) return; // 20% chance

        // Companion is cooler/smaller: shift color toward red, scale down.
        var companionColor = new Color(
            Mathf.Min(starColor.R * 1.1f, 1.0f),
            starColor.G * 0.6f,
            starColor.B * 0.4f);
        var companion = CreateStarMeshV0(companionColor, starClass, 0.5f);
        companion.Name = "BinaryCompanion";

        // Offset companion from primary at seed-derived angle.
        float separation = StarVisualRadiusU * StarClassVisualScaleV0(starClass) * 2.5f;
        companion.Position = DeriveOrbitPositionV0(nodeId + "_binary_pos", separation);

        _localSystemRoot.AddChild(companion);
    }

    // Ensure addon scene AnimationTree is active so planets/stars rotate.
    private static void ActivateAnimationTreeV0(Node3D sceneRoot)
    {
        var animTree = sceneRoot.GetNodeOrNull<AnimationTree>("AnimationTree");
        if (animTree != null)
            animTree.Active = true;
    }

    // Moon count by planet type.
    private static int MoonCountV0(string planetType, ulong hash) => planetType switch
    {
        "Gaseous"     => 1 + (int)(hash % 3UL),  // 1-3 moons
        "Terrestrial" => (int)(hash % 2UL),       // 0-1 moons
        "Ice"         => (int)(hash % 2UL),        // 0-1 moons
        _             => 0,
    };

    private void SpawnMoonsV0(string nodeId, Vector3 planetPos, string planetType)
    {
        var hash = Fnv1a64(nodeId + "_moons");
        int count = MoonCountV0(planetType, hash);
        if (count <= 0) return;

        const string MoonScenePath = "res://addons/naejimer_3d_planet_generator/scenes/planet_no_atmosphere.tscn";
        PackedScene moonScene = null;
        if (Godot.FileAccess.FileExists(MoonScenePath))
            moonScene = GD.Load<PackedScene>(MoonScenePath);

        for (int i = 0; i < count; i++)
        {
            var moonHash = Fnv1a64(nodeId + "_moon_" + i);
            float moonOrbit = 7.0f + (float)(moonHash % 5UL); // 7-11u from planet
            var moonOffset = DeriveOrbitPositionV0(nodeId + "_moon_" + i, moonOrbit);

            Node3D moonNode = null;
            if (moonScene != null)
            {
                moonNode = moonScene.Instantiate<Node3D>();
                ActivateAnimationTreeV0(moonNode);
            }
            else
            {
                moonNode = new Node3D();
                moonNode.AddChild(new MeshInstance3D
                {
                    Mesh = new SphereMesh { Radius = 0.5f },
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.5f, 0.5f, 0.5f) }
                });
            }

            var container = new Node3D { Name = "Moon_" + i };
            float moonScale = 0.005f + (float)(moonHash % 3UL) * 0.001f; // Vary size
            container.Scale = new Vector3(moonScale, moonScale, moonScale);
            container.Position = planetPos + moonOffset;
            container.AddChild(moonNode);
            _localSystemRoot.AddChild(container);
        }
    }

    // Asteroid belt — ring of rocky debris between inner and outer zones.
    private void SpawnAsteroidBeltV0(string nodeId, float lumScale)
    {
        var hash = Fnv1a64(nodeId + "_asteroids");
        // ~60% of systems have a visible asteroid belt.
        if (hash % 100UL >= 60) return;

        float beltRadius = 45.0f * lumScale; // Between planet zone and discovery sites
        int rockCount = 25 + (int)(hash % 20UL); // 25-44 rocks

        var rockMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.4f, 0.35f, 0.28f),
            Roughness = 0.9f
        };

        for (int i = 0; i < rockCount; i++)
        {
            var rockHash = Fnv1a64(nodeId + "_rock_" + i);
            float angle = ((float)i / rockCount) * 2.0f * MathF.PI;
            // Jitter radius and Y to make belt look natural.
            float rJitter = beltRadius + ((float)(rockHash % 5UL) - 2.0f);
            float yJitter = ((float)(rockHash % 3UL) - 1.0f) * 0.5f;

            float rockSize = 0.15f + (float)(rockHash % 4UL) * 0.1f; // 0.15-0.45u
            var rock = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(rockSize, rockSize * 0.7f, rockSize * 1.2f) },
                MaterialOverride = rockMat
            };
            // Random rotation for irregular look.
            rock.RotateX((float)(rockHash % 360UL) * (MathF.PI / 180f));
            rock.RotateY((float)((rockHash >> 8) % 360UL) * (MathF.PI / 180f));

            rock.Position = new Vector3(
                MathF.Cos(angle) * rJitter,
                yJitter,
                MathF.Sin(angle) * rJitter);

            rock.Name = "AsteroidRock_" + i;
            _localSystemRoot.AddChild(rock);
        }
    }

    // Spawn planet with type-matched scene, luminosity-scaled orbit, self-rotation.
    // Returns (planetPos, planetType) so station + moons can reference it.
    private (Vector3, string) SpawnLocalPlanetV0(string nodeId, float lumScale)
    {
        string planetType = "";
        bool landable = false;
        string displayName = "";
        if (_bridge != null && _bridge.HasMethod("GetPlanetInfoV0"))
        {
            var info = _bridge.Call("GetPlanetInfoV0", nodeId).AsGodotDictionary();
            if (info != null && info.Count > 0)
            {
                planetType = info.ContainsKey("planet_type") ? (string)info["planet_type"] : "";
                landable = info.ContainsKey("effective_landable") && (bool)info["effective_landable"];
                displayName = info.ContainsKey("display_name") ? (string)info["display_name"] : "";
            }
        }

        var scenePath = GetPlanetScenePath(planetType);

        Node3D planetNode = null;
        if (Godot.FileAccess.FileExists(scenePath))
        {
            var scene = GD.Load<PackedScene>(scenePath);
            if (scene != null)
            {
                planetNode = scene.Instantiate<Node3D>();
                ActivateAnimationTreeV0(planetNode); // Self-rotation
            }
        }

        if (planetNode == null)
        {
            planetNode = new Node3D();
            planetNode.AddChild(new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 4.0f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.4f, 0.5f, 0.6f),
                    Roughness = 0.8f
                }
            });
        }

        // Orbit radius: base distance * luminosity scale + seed jitter (±1.5u).
        float baseOrbit = PlanetBaseOrbitV0(planetType);
        var jitterHash = Fnv1a64(nodeId + "_orbit_jitter");
        float jitter = ((float)(jitterHash % 30UL) - 15.0f) * 0.1f; // ±1.5u
        float orbitRadius = baseOrbit * lumScale + jitter;

        // Visual scale varies by planet type (gas giants bigger).
        float vScale = PlanetVisualScaleV0(planetType);

        var container = new Node3D { Name = "LocalPlanet" };
        container.Scale = new Vector3(vScale, vScale, vScale);
        container.Position = DeriveOrbitPositionV0(nodeId + "_planet", orbitRadius);
        container.AddChild(planetNode);

        if (landable)
        {
            // Add dockable Area3D around the planet (same pattern as station).
            var dockArea = new Area3D
            {
                Name = "PlanetDock_" + nodeId,
                Monitoring = true,
                Monitorable = true,
                CollisionLayer = 0,
                CollisionMask = 2, // Detect Ships layer (player RigidBody3D).
            };

            var collider = new CollisionShape3D
            {
                Shape = new SphereShape3D { Radius = 12.0f }
            };
            dockArea.AddChild(collider);

            dockArea.AddToGroup("Planet");
            RegisterDockTargetV0(dockArea, "PLANET", nodeId);

            dockArea.BodyEntered += (body) =>
            {
                var gm = GetNode<Node>("/root/GameManager");
                if (gm != null && gm.HasMethod("on_proximity_dock_entered_v0"))
                    gm.Call("on_proximity_dock_entered_v0", dockArea);
            };

            // Attach dock area at the planet container's position in the system root.
            dockArea.Position = container.Position;

            // Add planet name label.
            if (!string.IsNullOrEmpty(displayName))
            {
                var label = new Label3D
                {
                    Text = displayName,
                    PixelSize = 0.02f,
                    FontSize = 32,
                    OutlineSize = 8,
                    Position = new Vector3(0, 6, 0),
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    Modulate = new Color(0.7f, 0.85f, 1.0f)
                };
                dockArea.AddChild(label);
            }

            _localSystemRoot.AddChild(dockArea);
        }

        _localSystemRoot.AddChild(container);
        return (container.Position, planetType);
    }

    // Map PlanetType enum string to addon scene path.
    private static string GetPlanetScenePath(string planetType)
    {
        return planetType switch
        {
            "Terrestrial" => "res://addons/naejimer_3d_planet_generator/scenes/planet_terrestrial.tscn",
            "Ice" => "res://addons/naejimer_3d_planet_generator/scenes/planet_ice.tscn",
            "Sand" => "res://addons/naejimer_3d_planet_generator/scenes/planet_sand.tscn",
            "Lava" => "res://addons/naejimer_3d_planet_generator/scenes/planet_lava.tscn",
            "Gaseous" => "res://addons/naejimer_3d_planet_generator/scenes/planet_gaseous.tscn",
            "Barren" => "res://addons/naejimer_3d_planet_generator/scenes/planet_no_atmosphere.tscn",
            _ => PlanetScenes[0], // Fallback to terrestrial
        };
    }

    // GATE.S1.HERO_SHIP_LOOP.LANE_GATE_LABEL.001: displayName from NeighborDisplayName; falls back to neighborId.
    // GATE.S1.VISUAL_POLISH.STRUCTURES.001: arch/frame gate geometry with emissive glow.
    private Node3D CreateLaneGateMarkerV0(string neighborId, string displayName = "")
    {
        var root = new Node3D { Name = "LaneGate_" + neighborId };

        var gateMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.15f, 0.25f, 0.8f),
            EmissionEnabled = true,
            Emission = new Color(0.2f, 0.4f, 1.0f),
            EmissionEnergyMultiplier = 2.0f,
            Roughness = 0.3f,
            Metallic = 0.7f
        };

        // Arch frame: two vertical pillars + a top crossbar forming a gate shape.
        float R = LaneGateMarkerRadiusU;
        float pillarH = R * 2.8f;
        float pillarW = R * 0.28f;
        float gateHalfWidth = R * 1.1f;

        var pillarMeshBase = new BoxMesh { Size = new Vector3(pillarW, pillarH, pillarW) };

        // Left pillar
        var pillarL = new MeshInstance3D
        {
            Name = "GatePillarL",
            Mesh = pillarMeshBase,
            MaterialOverride = gateMat
        };
        pillarL.Position = new Vector3(-gateHalfWidth, 0f, 0f);
        root.AddChild(pillarL);

        // Right pillar
        var pillarR = new MeshInstance3D
        {
            Name = "GatePillarR",
            Mesh = pillarMeshBase,
            MaterialOverride = gateMat
        };
        pillarR.Position = new Vector3(gateHalfWidth, 0f, 0f);
        root.AddChild(pillarR);

        // Top crossbar
        var crossbar = new MeshInstance3D
        {
            Name = "GateCrossbar",
            Mesh = new BoxMesh { Size = new Vector3(gateHalfWidth * 2f + pillarW, pillarW, pillarW) },
            MaterialOverride = gateMat
        };
        crossbar.Position = new Vector3(0f, pillarH * 0.5f, 0f);
        root.AddChild(crossbar);

        // Central emissive orb (jump-point beacon).
        var orbMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.7f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.5f, 0.7f, 1.0f),
            EmissionEnergyMultiplier = 3.5f
        };
        var orb = new MeshInstance3D
        {
            Name = "GateOrb",
            Mesh = new SphereMesh { Radius = R * 0.38f },
            MaterialOverride = orbMat
        };
        orb.Position = new Vector3(0f, 0f, 0f);
        root.AddChild(orb);

        // Keep a hidden "LaneGateMesh" node for any legacy lookup by name.
        var mesh = new MeshInstance3D
        {
            Name = "LaneGateMesh",
            Visible = false,
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
            PixelSize = 0.02f,
            FontSize = 32,
            OutlineSize = 8,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled
        };
        lbl.Position = new Vector3(0f, LaneGateMarkerRadiusU + 1.0f, 0f);
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

            // GATE.S11.GAME_FEEL.FLEET_STATUS.001: Show fleet role breakdown on galaxy map.
            var fleetLabel = root.GetNodeOrNull<Label3D>("FleetLabel");
            if (fleetLabel != null)
            {
                if (n.FleetCount > 0 && _bridge != null && _bridge.HasMethod("GetNodeFleetBreakdownV0"))
                {
                    var breakdown = _bridge.Call("GetNodeFleetBreakdownV0", n.NodeId).AsGodotDictionary();
                    var summary = breakdown.ContainsKey("summary") ? (string)(Variant)breakdown["summary"] : "";
                    fleetLabel.Text = !string.IsNullOrEmpty(summary) ? "[" + summary + "]" : "[" + n.FleetCount + "]";

                    // Color: gold for traders, blue tint for patrol presence, gray for hauler-only
                    int patrols = breakdown.ContainsKey("patrols") ? (int)(Variant)breakdown["patrols"] : 0;
                    int traders = breakdown.ContainsKey("traders") ? (int)(Variant)breakdown["traders"] : 0;
                    if (patrols > 0)
                        fleetLabel.Modulate = new Color(0.5f, 0.7f, 1.0f); // blue for patrol presence
                    else if (traders > 0)
                        fleetLabel.Modulate = new Color(1.0f, 0.85f, 0.3f); // gold for traders
                    else
                        fleetLabel.Modulate = new Color(0.7f, 0.7f, 0.7f); // gray for hauler-only
                }
                else
                {
                    fleetLabel.Text = n.FleetCount > 0 ? "[" + n.FleetCount + " fleets]" : "";
                    fleetLabel.Modulate = new Color(1.0f, 0.8f, 0.2f);
                }
            }

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
                    // GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001: Tint non-player nodes by intel freshness.
                    Color nodeColor = _currentOverlayMode == GalaxyOverlayMode.IntelFreshness
                        ? GetIntelFreshnessNodeColor(n.NodeId)
                        : new Color(0f, 0.6f, 1.0f);
                    mat.AlbedoColor = nodeColor;
                    mat.EmissionEnabled = true;
                    mat.Emission = nodeColor;
                    mat.EmissionEnergyMultiplier = 1.0f;
                }
            }
        }

        _lastNodeCount = renderedNodeCount;
        _lastPlayerHighlighted = playerHighlighted;

        // Reposition overlay camera ONCE when overlay opens (not per-frame, so user can pan/zoom).
        if (_overlayCamera != null && playerNodePosFound && !_cameraPositionedThisOpen)
        {
            _cameraPositionedThisOpen = true;
            var t = _overlayCamera.Transform;
            t.Origin = new Vector3(playerNodePos.X, 45f, playerNodePos.Z + 45f);
            _overlayCamera.Transform = t;
        }

        // GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001: Cache trade flow edges once before iterating.
        if (_currentOverlayMode == GalaxyOverlayMode.TradeFlow)
            CacheTradeFlowEdges();

        // GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001: Cache intel freshness ages once before iterating.
        if (_currentOverlayMode == GalaxyOverlayMode.IntelFreshness)
            CacheIntelFreshnessNodes();

        // Edges: create/update visuals — GATE.S5.SEC_LANES.UI.001: tint by security band
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (string.IsNullOrEmpty(e.FromId) || string.IsNullOrEmpty(e.ToId)) continue;

            string key = e.FromId + "->" + e.ToId;
            var edgeColor = GetEdgeColorForOverlay(e.FromId, e.ToId);

            if (!_edgeMeshesByKey.TryGetValue(key, out var mesh))
            {
                var mat = new StandardMaterial3D
                {
                    AlbedoColor = edgeColor,
                    EmissionEnabled = true,
                    Emission = edgeColor,
                    EmissionEnergyMultiplier = 1.2f
                };
                mesh = CreateEdgeMeshV0(mat);
                _edgeMeshesByKey[key] = mesh;
                AddChild(mesh);
            }
            else
            {
                // Update color on refresh as security levels / overlay mode changes
                var mi = mesh as MeshInstance3D ?? mesh.GetChildOrNull<MeshInstance3D>(0);
                if (mi?.MaterialOverride is StandardMaterial3D existingMat)
                {
                    existingMat.AlbedoColor = edgeColor;
                    existingMat.Emission = edgeColor;
                }
            }

            if (!_nodeRootsById.TryGetValue(e.FromId, out var fromRoot)) continue;
            if (!_nodeRootsById.TryGetValue(e.ToId, out var toRoot)) continue;

            UpdateEdgeTransformV0(mesh, fromRoot.GlobalPosition, toRoot.GlobalPosition);
        }

        // GATE.S11.GAME_FEEL.NPC_ROUTE_VIS.001: Draw NPC fleet route lines.
        if (_currentOverlayMode == GalaxyOverlayMode.Default || _currentOverlayMode == GalaxyOverlayMode.TradeFlow)
            UpdateNpcRouteLinesV0();
        else
            ClearNpcRouteLinesV0();
    }

    // GATE.S5.SEC_LANES.UI.001: Map security BPS to lane display color.
    private Color GetSecurityLaneColorV0(string fromId, string toId)
    {
        if (_bridge == null) return new Color(0.4f, 0.7f, 1.0f); // default blue
        int bps = _bridge.GetLaneSecurityV0(fromId, toId);
        if (bps < SimCore.Tweaks.SecurityTweaksV0.HostileBps)
            return new Color(1.0f, 0.15f, 0.15f); // red
        if (bps < SimCore.Tweaks.SecurityTweaksV0.DangerousBps)
            return new Color(1.0f, 0.6f, 0.2f);   // orange
        if (bps >= SimCore.Tweaks.SecurityTweaksV0.SafeBps)
            return new Color(0.2f, 1.0f, 0.4f);   // green
        return new Color(0.4f, 0.7f, 1.0f);        // default blue = moderate
    }

    // GATE.S1.VISUAL_UPGRADE.WORLD_MESHES.001: planet type scenes from planet generator addon
    private static readonly string[] PlanetScenes = new[]
    {
        "res://addons/naejimer_3d_planet_generator/scenes/planet_terrestrial.tscn",
        "res://addons/naejimer_3d_planet_generator/scenes/planet_ice.tscn",
        "res://addons/naejimer_3d_planet_generator/scenes/planet_sand.tscn",
        "res://addons/naejimer_3d_planet_generator/scenes/planet_lava.tscn",
        "res://addons/naejimer_3d_planet_generator/scenes/planet_gaseous.tscn",
        "res://addons/naejimer_3d_planet_generator/scenes/planet_no_atmosphere.tscn",
    };

    private static Node3D CreateNodeVisualV0(string nodeId)
    {
        var root = new Node3D();
        root.Name = "GalaxyNode_" + nodeId;

        // Try to use procedural planet from addon, fall back to SphereMesh
        Node3D planetNode = null;
        int hash = nodeId.GetHashCode() & 0x7FFFFFFF;
        var scenePath = PlanetScenes[hash % PlanetScenes.Length];
        if (Godot.FileAccess.FileExists(scenePath))
        {
            var scene = GD.Load<PackedScene>(scenePath);
            if (scene != null)
            {
                planetNode = scene.Instantiate<Node3D>();
                planetNode.Name = "NodeMesh";
                planetNode.Scale = new Vector3(5.0f, 5.0f, 5.0f);
            }
        }

        if (planetNode == null)
        {
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
            planetNode = mesh;
        }

        root.AddChild(planetNode);

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
            TopRadius = 0.4f,
            BottomRadius = 0.4f,
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

    // GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001: Cache profitable trade routes as bidirectional edge keys.
    private void CacheTradeFlowEdges()
    {
        _tradeFlowEdges.Clear();
        if (_bridge == null) return;
        var routes = _bridge.Call("GetTradeRoutesV0").AsGodotArray();
        foreach (var r in routes)
        {
            if (r.VariantType != Variant.Type.Dictionary) continue;
            var d = r.AsGodotDictionary();
            var src = d.ContainsKey("source_node_id") ? d["source_node_id"].AsString() : "";
            var dst = d.ContainsKey("dest_node_id") ? d["dest_node_id"].AsString() : "";
            if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst)) continue;
            _tradeFlowEdges.Add(src + "|" + dst);
            _tradeFlowEdges.Add(dst + "|" + src); // bidirectional
        }
    }

    // GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001: Return gold if a trade route runs on this edge, gray otherwise.
    private Color GetTradeFlowEdgeColor(string fromId, string toId)
    {
        return _tradeFlowEdges.Contains(fromId + "|" + toId)
            ? new Color(1.0f, 0.85f, 0.2f) // gold
            : new Color(0.3f, 0.3f, 0.3f);  // gray
    }

    // GATE.S6.MAP_GALAXY.TRADE_OVERLAY.001 + INTEL_OVERLAY.001: Route edge color through overlay mode.
    private Color GetEdgeColorForOverlay(string fromId, string toId)
    {
        switch (_currentOverlayMode)
        {
            case GalaxyOverlayMode.TradeFlow:
                return GetTradeFlowEdgeColor(fromId, toId);
            case GalaxyOverlayMode.IntelFreshness:
                return GetSecurityLaneColorV0(fromId, toId); // edges keep security colors in intel mode
            default:
                return GetSecurityLaneColorV0(fromId, toId);
        }
    }

    // GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001: Cache intel age_ticks keyed by node_id.
    private void CacheIntelFreshnessNodes()
    {
        _intelAgeByNode.Clear();
        if (_bridge == null) return;
        var entries = _bridge.Call("GetIntelFreshnessByNodeV0").AsGodotArray();
        foreach (var e in entries)
        {
            if (e.VariantType != Variant.Type.Dictionary) continue;
            var d = e.AsGodotDictionary();
            var nodeId = d.ContainsKey("node_id") ? d["node_id"].AsString() : "";
            if (string.IsNullOrEmpty(nodeId)) continue;
            long ageTicks = d.ContainsKey("age_ticks") ? d["age_ticks"].AsInt64() : long.MaxValue;
            _intelAgeByNode[nodeId] = ageTicks;
        }
    }

    // GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001: Map intel age to a display color (green → red gradient).
    private Color GetIntelFreshnessNodeColor(string nodeId)
    {
        if (!_intelAgeByNode.TryGetValue(nodeId, out long ageTicks))
            return new Color(0.4f, 0.4f, 0.4f); // gray — no intel
        if (ageTicks < 500)
            return new Color(0.2f, 1.0f, 0.4f);  // green — fresh
        if (ageTicks < 1500)
            return new Color(1.0f, 1.0f, 0.2f);  // yellow — aging
        if (ageTicks < 3000)
            return new Color(1.0f, 0.6f, 0.2f);  // orange — stale
        return new Color(1.0f, 0.15f, 0.15f);    // red — very stale
    }

    // GATE.S6.MAP_GALAXY.OVERLAY_SYS.001: Overlay mode API for toolbar.
    public void SetOverlayModeV0(int mode)
    {
        if (Enum.IsDefined(typeof(GalaxyOverlayMode), mode))
            _currentOverlayMode = (GalaxyOverlayMode)mode;
    }

    public int GetOverlayModeV0() => (int)_currentOverlayMode;

    // GATE.S11.GAME_FEEL.NPC_ROUTE_VIS.001: Draw route lines for NPC fleets currently traveling.
    // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Animated flow dots on trade routes.
    // GATE.S12.NPC_CIRC.VOLUME_LABELS.001: Trade volume labels on edges.
    // Gold for traders, blue for patrol fleets.
    private void UpdateNpcRouteLinesV0()
    {
        if (_bridge == null) { ClearNpcRouteLinesV0(); return; }
        if (!_bridge.HasMethod("GetNpcTradeRoutesV0")) { ClearNpcRouteLinesV0(); return; }

        // Extended tuple now carries GoodId and Qty for volume labels.
        var allNpcRoutes = new List<(string FleetId, string SourceId, string DestId, bool IsPatrol, string GoodId, int Qty)>();

        // Traders from GetNpcTradeRoutesV0
        var tradeRoutes = _bridge.Call("GetNpcTradeRoutesV0").AsGodotArray();
        foreach (var r in tradeRoutes)
        {
            if (r.VariantType != Variant.Type.Dictionary) continue;
            var d = r.AsGodotDictionary();
            var fleetId = d.ContainsKey("fleet_id") ? d["fleet_id"].AsString() : "";
            var srcId = d.ContainsKey("source_node_id") ? d["source_node_id"].AsString() : "";
            var dstId = d.ContainsKey("dest_node_id") ? d["dest_node_id"].AsString() : "";
            if (string.IsNullOrEmpty(srcId) || string.IsNullOrEmpty(dstId)) continue;
            if (StringComparer.Ordinal.Equals(srcId, dstId)) continue;
            var goodId = d.ContainsKey("good_id") ? d["good_id"].AsString() : "";
            int qty = d.ContainsKey("qty") ? d["qty"].AsInt32() : 0;
            allNpcRoutes.Add((fleetId, srcId, dstId, false, goodId, qty));
        }

        // Patrol fleets: query via bridge.GetNpcPatrolRoutesV0 if it exists, otherwise skip.
        if (_bridge.HasMethod("GetNpcPatrolRoutesV0"))
        {
            var patrolRoutes = _bridge.Call("GetNpcPatrolRoutesV0").AsGodotArray();
            foreach (var r in patrolRoutes)
            {
                if (r.VariantType != Variant.Type.Dictionary) continue;
                var d = r.AsGodotDictionary();
                var fleetId = d.ContainsKey("fleet_id") ? d["fleet_id"].AsString() : "";
                var srcId = d.ContainsKey("source_node_id") ? d["source_node_id"].AsString() : "";
                var dstId = d.ContainsKey("dest_node_id") ? d["dest_node_id"].AsString() : "";
                if (string.IsNullOrEmpty(srcId) || string.IsNullOrEmpty(dstId)) continue;
                if (StringComparer.Ordinal.Equals(srcId, dstId)) continue;
                allNpcRoutes.Add((fleetId, srcId, dstId, true, "", 0));
            }
        }

        // Track which keys are active this frame to clean up stale entries.
        var activeRouteKeys = new HashSet<string>(StringComparer.Ordinal);
        var activeFlowKeys = new HashSet<string>(StringComparer.Ordinal);
        var activeVolumeKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var route in allNpcRoutes)
        {
            var key = "npc_" + route.FleetId;
            activeRouteKeys.Add(key);

            if (!_nodeRootsById.TryGetValue(route.SourceId, out var fromRoot)) continue;
            if (!_nodeRootsById.TryGetValue(route.DestId, out var toRoot)) continue;

            var routeColor = route.IsPatrol
                ? new Color(0.3f, 0.5f, 1.0f, 0.6f)   // Blue for patrol
                : new Color(1.0f, 0.85f, 0.2f, 0.6f);  // Gold for trader

            if (!_npcRouteMeshesByKey.TryGetValue(key, out var mesh))
            {
                var mat = new StandardMaterial3D
                {
                    AlbedoColor = routeColor,
                    EmissionEnabled = true,
                    Emission = new Color(routeColor.R, routeColor.G, routeColor.B),
                    EmissionEnergyMultiplier = 1.5f,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha
                };
                mesh = CreateEdgeMeshV0(mat);
                mesh.Name = "NpcRoute_" + route.FleetId;
                _npcRouteMeshesByKey[key] = mesh;
                AddChild(mesh);
            }
            else
            {
                if (mesh.MaterialOverride is StandardMaterial3D existingMat)
                {
                    existingMat.AlbedoColor = routeColor;
                    existingMat.Emission = new Color(routeColor.R, routeColor.G, routeColor.B);
                }
            }

            // Offset Y slightly so route lines are visually distinct from lane edges.
            var fromPos = fromRoot.GlobalPosition + new Vector3(0f, 1.5f, 0f);
            var toPos = toRoot.GlobalPosition + new Vector3(0f, 1.5f, 0f);
            UpdateEdgeTransformV0(mesh, fromPos, toPos);

            // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Animated flow dot along route.
            var flowKey = "flow_" + route.FleetId;
            activeFlowKeys.Add(flowKey);
            if (!_flowDotsByKey.TryGetValue(flowKey, out var flowDot))
            {
                var dotColor = route.IsPatrol
                    ? new Color(0.4f, 0.6f, 1.0f)   // Blue for patrol
                    : new Color(1.0f, 0.9f, 0.3f);   // Gold for trader
                var dotMat = new StandardMaterial3D
                {
                    AlbedoColor = dotColor,
                    EmissionEnabled = true,
                    Emission = dotColor,
                    EmissionEnergyMultiplier = 2.0f
                };
                var sphere = new SphereMesh { Radius = 0.3f, Height = 0.6f };
                flowDot = new MeshInstance3D
                {
                    Name = "FlowDot_" + route.FleetId,
                    Mesh = sphere,
                    MaterialOverride = dotMat
                };
                _flowDotsByKey[flowKey] = flowDot;
                AddChild(flowDot);
            }
            float t = (float)((_flowAnimTime * 0.3) % 1.0);
            flowDot.GlobalPosition = fromPos.Lerp(toPos, t);

            // GATE.S12.NPC_CIRC.VOLUME_LABELS.001: Volume label at route midpoint (traders only).
            if (!route.IsPatrol && route.Qty > 0 && !string.IsNullOrEmpty(route.GoodId))
            {
                var volKey = "vol_" + route.FleetId;
                activeVolumeKeys.Add(volKey);
                if (!_volumeLabelsByKey.TryGetValue(volKey, out var volLabel))
                {
                    volLabel = new Label3D
                    {
                        Name = "VolLabel_" + route.FleetId,
                        PixelSize = 0.02f,
                        FontSize = 24,
                        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                        OutlineSize = 6,
                        Modulate = new Color(1.0f, 0.95f, 0.7f)
                    };
                    _volumeLabelsByKey[volKey] = volLabel;
                    AddChild(volLabel);
                }
                volLabel.Text = $"{route.GoodId} x{route.Qty}";
                volLabel.GlobalPosition = (fromPos + toPos) * 0.5f + new Vector3(0f, 2.0f, 0f);
            }
        }

        // Remove stale route lines (fleets that stopped traveling).
        RemoveStaleEntries(_npcRouteMeshesByKey, activeRouteKeys);

        // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Remove stale flow dots.
        RemoveStaleEntries(_flowDotsByKey, activeFlowKeys);

        // GATE.S12.NPC_CIRC.VOLUME_LABELS.001: Remove stale volume labels.
        RemoveStaleLabels(_volumeLabelsByKey, activeVolumeKeys);
    }

    private static void RemoveStaleEntries(Dictionary<string, MeshInstance3D> dict, HashSet<string> activeKeys)
    {
        var staleKeys = new List<string>();
        foreach (var kv in dict)
        {
            if (!activeKeys.Contains(kv.Key))
                staleKeys.Add(kv.Key);
        }
        foreach (var key in staleKeys)
        {
            if (dict.TryGetValue(key, out var node))
            {
                node.QueueFree();
                dict.Remove(key);
            }
        }
    }

    private static void RemoveStaleLabels(Dictionary<string, Label3D> dict, HashSet<string> activeKeys)
    {
        var staleKeys = new List<string>();
        foreach (var kv in dict)
        {
            if (!activeKeys.Contains(kv.Key))
                staleKeys.Add(kv.Key);
        }
        foreach (var key in staleKeys)
        {
            if (dict.TryGetValue(key, out var node))
            {
                node.QueueFree();
                dict.Remove(key);
            }
        }
    }

    private void ClearNpcRouteLinesV0()
    {
        foreach (var mesh in _npcRouteMeshesByKey.Values)
            mesh.QueueFree();
        _npcRouteMeshesByKey.Clear();

        // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Clear flow dots.
        foreach (var dot in _flowDotsByKey.Values)
            dot.QueueFree();
        _flowDotsByKey.Clear();

        // GATE.S12.NPC_CIRC.VOLUME_LABELS.001: Clear volume labels.
        foreach (var lbl in _volumeLabelsByKey.Values)
            lbl.QueueFree();
        _volumeLabelsByKey.Clear();
    }
}
