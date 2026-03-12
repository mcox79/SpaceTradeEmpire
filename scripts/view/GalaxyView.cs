using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

// GATE.S6.MAP_GALAXY.OVERLAY_SYS.001: Galaxy map overlay mode.
public enum GalaxyOverlayMode
{
    None = -1,         // No overlay coloring — raw 3D scene
    Default = 0,       // Security coloring (existing behavior)
    TradeFlow = 1,     // Trade route profitability + NPC volume
    IntelFreshness = 2 // Node intel age coloring
}

// GATE.S7.GALAXY_MAP_V2.OVERLAYS.001: V2 overlay modes toggled via hotkeys.
public enum GalaxyMapV2Overlay
{
    Off = 0,
    Faction = 1,
    Fleet = 2,
    Heat = 3,
    Exploration = 4,  // GATE.S7.GALAXY_MAP_V2.EXPLORATION_OVL.001
    Warfront = 5,     // GATE.S7.GALAXY_MAP_V2.WARFRONT_OVL.001
}

public partial class GalaxyView : Node3D
{
    private SimBridge _bridge;

    private bool _overlayOpen = false;

    // GATE.S6.MAP_GALAXY.OVERLAY_SYS.001: Active overlay mode.
    private GalaxyOverlayMode _currentOverlayMode = GalaxyOverlayMode.Default;
    // GATE.S17.REAL_SPACE.GALAXY_MAP.001: _cameraPositionedThisOpen removed (follow camera drives altitude).

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
    private double _playerRingPulseTime = 0.0;

    // GATE.S12.NPC_CIRC.VOLUME_LABELS.001: Trade volume labels on edges.
    private readonly Dictionary<string, Label3D> _volumeLabelsByKey = new();

    // GATE.S15.FEEL.FACTION_LABELS.001: Faction territory Label3D nodes keyed by faction_id.
    private readonly Dictionary<string, Label3D> _factionLabelsByFactionId = new();

    // GATE.S7.FACTION.TERRITORY_OVERLAY.001: Semi-transparent territory fill discs at claimed nodes.
    private readonly Dictionary<string, MeshInstance3D> _territoryDiscsByNodeId = new();

    // GATE.S5.LOOT.BRIDGE_PROOF.001: Loot drop markers (glowing spheres) at current node.
    private readonly Dictionary<string, MeshInstance3D> _lootMarkersByDropId = new();

    // ── GATE.S7.GALAXY_MAP_V2.OVERLAYS.001: V2 overlay state ──
    private GalaxyMapV2Overlay _v2OverlayMode = GalaxyMapV2Overlay.Off;
    private readonly Dictionary<string, MeshInstance3D> _v2OverlayDiscsByNodeId = new();

    // ── GATE.S7.GALAXY_MAP_V2.ROUTE_PLANNER.001: Route planner state ──
    private bool _routePlannerActive = false;
    private string _routePlannerDestNodeId = "";
    private readonly List<MeshInstance3D> _routePolylineSegments = new();
    private Label3D _routeTravelTimeLabel;

    // ── GATE.S7.GALAXY_MAP_V2.SEARCH.001: Galaxy search bar state ──
    private Control _searchBarRoot;
    private LineEdit _searchLineEdit;
    private ItemList _searchDropdown;
    private bool _searchBarVisible = false;

    // ── GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001: Semantic zoom detail levels ──
    // Thresholds: close < 500u, medium 500-2000u, galaxy > 2000u.
    private float _lastSemanticAltitude = 0f;

    // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Persistent star billboards at galactic-scale positions.
    private Node3D _persistentStarsRoot;
    // Persistent lane lines between stars (always visible in real-space flight).
    private Node3D _persistentLanesRoot;
    // Shared lane material for alpha fade during altitude transitions.
    private StandardMaterial3D _sharedLaneMaterial;
    private const float LaneBaseAlpha = 0.5f;

    // Transit mode: skip per-frame RefreshFromSnapshotV0 and hide non-essential nodes.
    private bool _transitMode = false;
    // Label suppression during transit/cinematic (prevents ClampLabelsRecursive from re-showing).
    private bool _localLabelsHidden = false;
    // GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: 2D UI panel active (dock menu, station menu).
    // When true, all 3D overlay elements are hidden to prevent bleed-through.
    private bool _uiPanelActive = false;
    private string _transitOriginId = "";
    private string _transitDestId = "";

    // Pre-computed gate local positions: "nodeId|neighborId" → position relative to star center.
    // Computed once during init; used by lane rendering, transit camera, and hero positioning.
    private readonly Dictionary<string, Vector3> _gateLocalPositionCache = new();

    private int _lastNodeCount = 0;
    private int _lastEdgeCount = 0;
    private bool _lastPlayerHighlighted = false;

    // --- Local system config (named exported fields; no numeric literals in .cs or .tscn) ---
    // Pace overhaul: 1.6x spread for spacious systems. Planets 18-40u, gates 85u, radius 120u.
    // At 80u camera altitude + 60° FOV, visible radius ≈ 46u — player sees planets but must fly to gates.
    // VISUAL_OVERHAUL: 1.5x system scale for spacious, vast-feeling systems.
    [Export] public float SystemSceneRadiusU { get; set; } = 180.0f;
    [Export] public float StationOrbitRadiusU { get; set; } = 54.0f;
    [Export] public float LaneGateDistanceU { get; set; } = 130.0f;
    [Export] public float DiscoverySiteOrbitRadiusU { get; set; } = 85.0f;
    [Export] public float StarVisualRadiusU { get; set; } = 6.0f;
    [Export] public float LaneGateMarkerRadiusU { get; set; } = 1.5f;
    [Export] public float DiscoverySiteMarkerRadiusU { get; set; } = 1.0f;
    // GATE.S5.COMBAT_PLAYABLE.ENCOUNTER_TRIGGER.001
    // VISUAL_OVERHAUL: fleet orbit spread 1.5x.
    [Export] public float FleetOrbitRadiusU { get; set; } = 52.0f;
    [Export] public float FleetMarkerRadiusU { get; set; } = 1.2f;
    [Export] public PackedScene StationPrefab { get; set; }
    // GATE.S16.NPC_ALIVE.SPAWN_SYSTEM.001
    [Export] public PackedScene NpcShipScene { get; set; }

    // Sensor range: lanes are only visible if at least one endpoint is within this distance
    // (in galactic-scale units) of a visited system. Set to 0 to disable range limit.
    [Export] public float SensorRangeGalacticU { get; set; } = 500.0f;

    // Local system state
    private Node3D _localSystemRoot;
    private string _currentNodeId = "";

    // GATE.S15.FEEL.NPC_PROXIMITY.001: Periodic fleet refresh for NPC arrivals/departures.
    private double _fleetRefreshTimer = 0.0;
    private string _currentLocalNodeId = "";

    // GATE.S17.REAL_SPACE.GALAXY_MAP.001: No dedicated overlay camera — the follow camera
    // raises to altitude. Use GetViewport().GetCamera3D() for projection queries.

    public override void _Ready()
    {
        _bridge = GetNodeOrNull<SimBridge>("/root/SimBridge");
        // GATE.S17.REAL_SPACE.GALAXY_MAP.001: overlay camera removed; follow camera is the map camera.

        // Default OFF: the playable prototype is a local-space view until Tab opens the overlay.
        Visible = false;
        SetProcess(false);
        // GATE.S13.WORLD.LABEL_CLAMP.001: Physics process always on for local label distance clamping.
        SetPhysicsProcess(true);

        // Allocate local system container; it will be added to the parent in the deferred boot call
        // (adding children during _Ready while the parent is busy building children is not allowed).
        _localSystemRoot = new Node3D { Name = "LocalSystem" };

        // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Persistent stars container (always visible).
        _persistentStarsRoot = new Node3D { Name = "PersistentStars" };
        _persistentLanesRoot = new Node3D { Name = "PersistentLanes" };
        _persistentLanesRoot.Visible = false; // Only visible during galaxy map.

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
        if (_overlayOpen == isOpen) return; // Idempotent — avoid per-frame refresh cost.
        _overlayOpen = isOpen;

        // GalaxyView's own overlay rendering (nodes, edges, colors).
        Visible = isOpen;
        SetProcess(isOpen);

        if (isOpen)
        {
            // Defer one frame so SimBridge can finish boot sequences.
            CallDeferred(nameof(RefreshFromSnapshotV0));
        }
        else
        {
            // GATE.S7.GALAXY_MAP_V2: Clean up V2 overlays, route planner, and search bar on close.
            ClearV2OverlayV0();
            ClearRoutePlannerV0();
            HideSearchBarV0();
        }
    }

    /// GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Notify GalaxyView that a 2D UI panel
    /// (dock menu, station menu) is covering the screen. When active, hide all 3D overlay
    /// elements (node beacons, labels, edges, territory discs, persistent stars/lanes)
    /// so they don't bleed through the 2D Control-based UI.
    public void SetUiPanelActiveV0(bool isActive)
    {
        _uiPanelActive = isActive;

        if (isActive)
        {
            // Hide persistent stars and lanes (they render through 2D panels).
            if (_persistentStarsRoot != null)
                _persistentStarsRoot.Visible = false;
            if (_persistentLanesRoot != null)
                _persistentLanesRoot.Visible = false;

            // Hide all overlay node roots (beacons + labels at galactic scale).
            foreach (var kvp in _nodeRootsById)
                kvp.Value.Visible = false;
            foreach (var kvp in _edgeMeshesByKey)
                kvp.Value.Visible = false;
            foreach (var kvp in _factionLabelsByFactionId)
                kvp.Value.Visible = false;
            foreach (var kvp in _territoryDiscsByNodeId)
                kvp.Value.Visible = false;
            foreach (var kvp in _v2OverlayDiscsByNodeId)
                kvp.Value.Visible = false;
            foreach (var kvp in _npcRouteMeshesByKey)
                kvp.Value.Visible = false;
            foreach (var kvp in _volumeLabelsByKey)
                kvp.Value.Visible = false;
            foreach (var kvp in _lootMarkersByDropId)
                kvp.Value.Visible = false;
        }
        else
        {
            // Restore visibility — UpdateAltitudeLodV0 will set correct LOD state
            // on the next physics frame when the camera re-syncs altitude.
            // Persistent stars/lanes are restored by altitude LOD.
            // Overlay nodes are restored by the next RefreshFromSnapshotV0 if overlay is open.
            // For non-overlay mode, just ensure persistent elements are altitude-driven again.
            // Force a LOD refresh by calling UpdateAltitudeLodV0 with current state.
            if (_persistentStarsRoot != null)
                _persistentStarsRoot.Visible = true; // LOD will refine on next frame.
        }
    }

    /// Continuous LOD update driven by camera altitude (Feature 2: Seamless Zoom).
    /// Called every physics frame from player_follow_camera.gd via _sync_altitude().
    /// Manages 3D scene root visibility. Overlay on/off is handled by SetOverlayOpenV0.
    public void UpdateAltitudeLodV0(float altitude)
    {
        // GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: When a 2D UI panel is active
        // (dock menu, station menu), suppress all 3D visibility updates. The panel
        // covers the viewport, and 3D elements would bleed through.
        if (_uiPanelActive) return;

        // Local system (star, planets, stations): visible below 500u.
        if (_localSystemRoot != null)
            _localSystemRoot.Visible = altitude < 500f;

        // Persistent star billboards: visible above 100u.
        if (_persistentStarsRoot != null)
        {
            _persistentStarsRoot.Visible = altitude >= 100f;
            // Hide the persistent star for the current local system to prevent z-fighting
            // with the local star mesh in the 100-500u overlap zone.
            if (altitude >= 100f && !string.IsNullOrEmpty(_currentLocalNodeId))
            {
                var currentStar = _persistentStarsRoot.GetNodeOrNull<Node3D>("PersistentStar_" + _currentLocalNodeId);
                if (currentStar != null)
                    currentStar.Visible = altitude >= 500f; // Only show once local system is hidden.
            }
        }

        // GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001: Update detail levels by altitude.
        if (_overlayOpen)
            UpdateSemanticZoomV0(altitude);

        // Persistent 3D lane lines: fade in over 200-400u, solid above 400u.
        if (_persistentLanesRoot != null)
        {
            // Eagerly build lanes if root is empty and we're zooming out.
            if (altitude >= 200f && _persistentLanesRoot.GetChildCount() == 0)
                SpawnPersistentLanesV0();

            bool shouldShow = altitude >= 200f;
            _persistentLanesRoot.Visible = shouldShow;

            if (shouldShow && _sharedLaneMaterial != null)
            {
                float fadeAlpha;
                if (altitude < 300f)
                    fadeAlpha = (altitude - 200f) / 100f; // 0→1 over 200-300u
                else if (altitude < 400f)
                    fadeAlpha = (altitude - 300f) / 100f * 0.5f + 0.5f; // 0.5→1 over 300-400u
                else
                    fadeAlpha = 1f;
                fadeAlpha = Mathf.Clamp(fadeAlpha, 0f, 1f);
                var c = _sharedLaneMaterial.AlbedoColor;
                _sharedLaneMaterial.AlbedoColor = new Color(c.R, c.G, c.B, LaneBaseAlpha * fadeAlpha);
            }
        }
    }

    /// Ensure persistent lane meshes are built (called during transit before overlay opens).
    public void EnsurePersistentLanesBuiltV0()
    {
        if (_persistentLanesRoot != null && _persistentLanesRoot.GetChildCount() == 0)
            SpawnPersistentLanesV0();
    }

    /// Rebuild persistent lanes (call after visiting a new node to update fog-of-war).
    public void RebuildPersistentLanesV0()
    {
        if (_persistentLanesRoot == null) return;
        foreach (var child in _persistentLanesRoot.GetChildren())
            child.QueueFree();
        SpawnPersistentLanesV0();
    }

    /// Pre-build galaxy overlay visuals (nodes, edges, lanes) without making them visible.
    /// Call this when the player enters gate approach so transit animation starts instantly.
    public void PrewarmOverlayV0()
    {
        // Build persistent lanes (3D meshes between stars).
        EnsurePersistentLanesBuiltV0();
        // Build overlay node/edge visuals (creates meshes in _nodeRootsById).
        // Keep them hidden — SetOverlayOpenV0(true) will flip Visible later.
        if (!_overlayOpen)
        {
            RefreshFromSnapshotV0();
        }
    }

    /// Enter/exit transit mode. In transit mode:
    /// - RefreshFromSnapshotV0 is skipped (prewarmed data is sufficient)
    /// - Only origin + destination nodes and the connecting lane are visible
    public void SetTransitModeV0(bool isTransit, string originNodeId, string destNodeId)
    {
        _transitMode = isTransit;
        _transitOriginId = originNodeId;
        _transitDestId = destNodeId;

        if (isTransit)
        {
            // Hide all node roots except origin and destination.
            foreach (var kvp in _nodeRootsById)
            {
                kvp.Value.Visible = kvp.Key == originNodeId || kvp.Key == destNodeId;
            }
            // Hide all edge meshes except the origin→dest lane.
            var transitKey1 = $"{originNodeId}→{destNodeId}";
            var transitKey2 = $"{destNodeId}→{originNodeId}";
            foreach (var kvp in _edgeMeshesByKey)
            {
                kvp.Value.Visible = kvp.Key == transitKey1 || kvp.Key == transitKey2;
            }
            // Hide NPC route lines and flow dots during transit.
            foreach (var kvp in _npcRouteMeshesByKey)
                kvp.Value.Visible = false;
            foreach (var kvp in _flowDotsByKey)
                kvp.Value.Visible = false;
            foreach (var kvp in _volumeLabelsByKey)
                kvp.Value.Visible = false;
            foreach (var kvp in _factionLabelsByFactionId)
                kvp.Value.Visible = false;
        }
        else
        {
            // Restore all nodes/edges to visible (next RefreshFromSnapshotV0 will set correct state).
            foreach (var kvp in _nodeRootsById)
                kvp.Value.Visible = true;
            foreach (var kvp in _edgeMeshesByKey)
                kvp.Value.Visible = true;
            foreach (var kvp in _npcRouteMeshesByKey)
                kvp.Value.Visible = true;
            foreach (var kvp in _flowDotsByKey)
                kvp.Value.Visible = true;
            foreach (var kvp in _volumeLabelsByKey)
                kvp.Value.Visible = true;
            foreach (var kvp in _factionLabelsByFactionId)
                kvp.Value.Visible = true;
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
        if (_bridge == null || _bridge.IsLoading) return;
        // GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Skip all processing when UI panel active.
        if (_uiPanelActive) return;

        // GATE.S15.FEEL.NPC_PROXIMITY.001: Periodic fleet refresh for NPC arrivals/departures.
        if (!_overlayOpen && !string.IsNullOrEmpty(_currentLocalNodeId))
        {
            _fleetRefreshTimer -= delta;
            if (_fleetRefreshTimer <= 0.0)
            {
                RefreshLocalFleetsV0();
                _fleetRefreshTimer = 1.0;
            }
        }

        if (!_overlayOpen) return;

        // In transit mode, skip per-frame refresh — prewarmed data is sufficient.
        // Only animate flow dots and player ring pulse.
        if (_transitMode)
        {
            _flowAnimTime += delta;
            _playerRingPulseTime += delta;
            return;
        }

        // GATE.S12.NPC_CIRC.FLOW_ANIM.001: Accumulate time for flow dot animation.
        _flowAnimTime += delta;

        // GATE.S14.MAP.PLAYER_INDICATOR.001: Pulse the player ring on the galaxy map.
        _PulsePlayerRingV0(delta);

        RefreshFromSnapshotV0();

        // GATE.S7.GALAXY_MAP_V2.OVERLAYS.001: Refresh V2 overlay visuals after snapshot.
        if (_v2OverlayMode != GalaxyMapV2Overlay.Off)
            UpdateV2OverlayVisualsV0();
    }

    // GATE.S13.WORLD.LABEL_CLAMP.001: Distance-based label readability for local system labels.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsInsideTree()) return;
        var cam = GetViewport()?.GetCamera3D();
        if (cam == null || !cam.IsInsideTree()) return;

        // Move sky nodes to follow camera so starfield extends everywhere.
        // Must run ALWAYS (even during overlay/transit) so stars stay visible.
        var skyParent = GetParent();
        if (skyParent != null)
        {
            var starlightSky = skyParent.GetNodeOrNull<Node3D>("StarlightSky");
            if (starlightSky != null)
                starlightSky.GlobalPosition = cam.GlobalPosition;
            var galacticSky = skyParent.GetNodeOrNull<Node3D>("GalacticSky");
            if (galacticSky != null)
                galacticSky.GlobalPosition = cam.GlobalPosition;
        }

        if (_localSystemRoot == null || _overlayOpen) return;
        if (!_localSystemRoot.IsInsideTree()) return;
        // GATE.S7.RUNTIME_STABILITY.GALAXY_VIEW_FIX.001: Skip label clamping when UI panel active.
        if (_uiPanelActive) return;
        if (_localLabelsHidden)
        {
            // Actively suppress overlay labels every physics frame during transit/cinematic.
            // Something (PrewarmOverlayV0, RefreshFromSnapshotV0, or scene default) may re-show them.
            foreach (var kvp in _nodeRootsById)
            {
                var nl = kvp.Value.GetNodeOrNull<Label3D>("NodeLabel");
                if (nl != null && nl.Visible) nl.Visible = false;
                var fl = kvp.Value.GetNodeOrNull<Label3D>("FleetLabel");
                if (fl != null && fl.Visible) fl.Visible = false;
            }
            return; // Skip local label clamping.
        }

        var camPos = cam.GlobalPosition;
        ClampLabelsInSubtree(_localSystemRoot, camPos);
    }

    // GATE.X.UI_POLISH.LABEL_OVERLAP.001: Track placed label positions for anti-collision.
    private static readonly System.Collections.Generic.List<Vector3> _placedLabelPositions = new();

    private static void ClampLabelsInSubtree(Node root, Vector3 camPos)
    {
        // Recurse entire subtree — labels may be nested 3-4 levels deep
        // (e.g., _localSystemRoot -> PlanetRoot -> DockArea -> Station -> StationLabel).
        _placedLabelPositions.Clear();
        ClampLabelsRecursive(root, camPos);
    }

    private static void ClampLabelsRecursive(Node node, Vector3 camPos)
    {
        // Skip NPC ship subtrees — they manage their own label visibility
        // (role label, hostile label, HP bar) via npc_ship.gd.
        if (node.IsInGroup("FleetShip")) return;

        foreach (var child in node.GetChildren())
        {
            if (child is Label3D label && node is Node3D parent3d)
            {
                float dist = camPos.DistanceTo(parent3d.GlobalPosition);
                // Sweet spot: 30-60u = full readability. <5u hidden, 5-30u shrink+fade, >60u fade out.
                if (dist < 5f)
                {
                    label.Visible = false;
                }
                else if (dist < 30f)
                {
                    float t = (dist - 5f) / 25f; // 0..1 over 5-30u
                    float scale = Mathf.Clamp(t, 0.2f, 1f);
                    label.PixelSize = 0.12f * scale;
                    label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, scale);
                    label.Visible = true;
                }
                else if (dist > 150f)
                {
                    // Fully hidden beyond 150u — prevents distant label artifacts.
                    label.Visible = false;
                }
                else if (dist > 80f)
                {
                    float alpha = Mathf.Clamp(1f - (dist - 80f) / 70f, 0f, 1f);
                    label.PixelSize = 0.12f;
                    label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, alpha);
                    label.Visible = alpha > 0.01f;
                }
                else
                {
                    label.PixelSize = 0.12f;
                    label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, 1f);
                    label.Visible = true;
                }

                // GATE.X.UI_POLISH.LABEL_OVERLAP.001: Anti-collision vertical offset.
                if (label.Visible)
                {
                    var worldPos = parent3d.GlobalPosition;
                    float offsetY = 0f;
                    foreach (var placed in _placedLabelPositions)
                    {
                        float dx = worldPos.X - placed.X;
                        float dz = worldPos.Z - placed.Z;
                        if (dx * dx + dz * dz < 9f) // 3u threshold
                            offsetY += 2f;
                    }
                    label.Position = new Vector3(label.Position.X, offsetY, label.Position.Z);
                    _placedLabelPositions.Add(worldPos);
                }
            }
            else if (child is Node3D)
            {
                ClampLabelsRecursive(child, camPos);
            }
        }
    }

    // GATE.S6.MAP_GALAXY.NODE_CLICK.001: Click detection on galaxy overlay nodes.
    // Projects all node positions to screen space and picks the closest within threshold.
    [Export] public float NodeClickThresholdPx { get; set; } = 30.0f;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_overlayOpen) return;

        // ── GATE.S7.GALAXY_MAP_V2.OVERLAYS.001: Hotkey overlay toggles (F/L/H) ──
        // ── GATE.S7.GALAXY_MAP_V2.ROUTE_PLANNER.001: Escape cancels route planner ──
        // ── GATE.S7.GALAXY_MAP_V2.SEARCH.001: Ctrl+F toggles search bar ──
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            // Don't process hotkeys while search bar is focused.
            if (_searchBarVisible && _searchLineEdit != null && _searchLineEdit.HasFocus())
            {
                if (key.Keycode == Key.Escape)
                {
                    HideSearchBarV0();
                    GetViewport().SetInputAsHandled();
                }
                return;
            }

            switch (key.Keycode)
            {
                case Key.F when !key.CtrlPressed:
                    ToggleV2OverlayV0(GalaxyMapV2Overlay.Faction);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.L:
                    ToggleV2OverlayV0(GalaxyMapV2Overlay.Fleet);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.H:
                    ToggleV2OverlayV0(GalaxyMapV2Overlay.Heat);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.E when !key.CtrlPressed:
                    ToggleV2OverlayV0(GalaxyMapV2Overlay.Exploration);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.W when !key.CtrlPressed:
                    ToggleV2OverlayV0(GalaxyMapV2Overlay.Warfront);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Escape:
                    if (_routePlannerActive)
                    {
                        ClearRoutePlannerV0();
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                    if (_searchBarVisible)
                    {
                        HideSearchBarV0();
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                    break;
                case Key.F when key.CtrlPressed:
                    ToggleSearchBarV0();
                    GetViewport().SetInputAsHandled();
                    return;
            }
        }

        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        // GATE.S17.REAL_SPACE.GALAXY_MAP.001: Use viewport camera (the follow camera at altitude).
        var activeCam = GetViewport()?.GetCamera3D();
        if (activeCam == null) return;

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
            if (activeCam.IsPositionBehind(worldPos)) continue;

            var screenPos = activeCam.UnprojectPosition(worldPos);
            float dist = screenPos.DistanceTo(clickPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestNodeId = kv.Key;
            }
        }

        if (closestNodeId != null)
        {
            // GATE.S7.GALAXY_MAP_V2.ROUTE_PLANNER.001: Route planner intercepts node clicks.
            if (_routePlannerActive || Input.IsKeyPressed(Key.Shift))
            {
                SetRoutePlannerDestV0(closestNodeId);
                GetViewport().SetInputAsHandled();
                return;
            }

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

        // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Persistent stars container.
        if (_persistentStarsRoot != null && _persistentStarsRoot.GetParent() == null)
        {
            GetParent().AddChild(_persistentStarsRoot);
        }

        // Persistent lane lines container (always visible).
        if (_persistentLanesRoot != null && _persistentLanesRoot.GetParent() == null)
        {
            GetParent().AddChild(_persistentLanesRoot);
        }

        var galaxySnap = _bridge.GetGalaxySnapshotV0();
        if (galaxySnap == null) return;

        // Pre-compute gate positions for ALL systems upfront.
        PrecomputeAllGatePositionsV0();

        var nodeId = galaxySnap.ContainsKey("player_current_node_id")
            ? (string)galaxySnap["player_current_node_id"]
            : "";

        if (string.IsNullOrEmpty(nodeId)) return;

        DrawLocalSystemV0(nodeId);

        // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Spawn persistent star billboards for all systems.
        SpawnPersistentStarsV0();

        // Spawn persistent 3D lane lines between connected stars (visible during flight).
        SpawnPersistentLanesV0();

        // Teleport hero ship near the station on initial boot (not inside the star).
        var player = GetTree()?.Root?.GetNodeOrNull<Node3D>("Main/Player");
        if (player != null)
        {
            var starPos = _localSystemRoot.GlobalPosition;
            player.GlobalPosition = starPos + new Vector3(StationOrbitRadiusU, 0f, 0f);
        }
    }

    // GDScript-callable: tears down the current local system and rebuilds for the given nodeId.
    // Called by game_manager.on_lane_arrival_v0 after the hero ship completes a lane transit.
    public void RebuildLocalSystemV0(string nodeId)
    {
        DrawLocalSystemV0(nodeId);
    }

    // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Spawn persistent star billboards at galactic-scale positions.
    private void SpawnPersistentStarsV0()
    {
        if (_bridge == null || _persistentStarsRoot == null) return;

        foreach (var child in _persistentStarsRoot.GetChildren())
            child.QueueFree();

        var galSnap = _bridge.GetGalaxySnapshotV0();
        if (galSnap == null) return;

        var rawNodes = galSnap.ContainsKey("system_nodes")
            ? (Godot.Collections.Array)galSnap["system_nodes"]
            : new Godot.Collections.Array();

        float scale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;

        for (int i = 0; i < rawNodes.Count; i++)
        {
            Variant v = rawNodes[i];
            if (v.VariantType != Variant.Type.Dictionary) continue;

            var n = v.AsGodotDictionary();
            var nodeId = n.ContainsKey("node_id") ? (string)(Variant)n["node_id"] : "";
            if (string.IsNullOrEmpty(nodeId)) continue;

            var stateToken = n.ContainsKey("display_state_token") ? (string)(Variant)n["display_state_token"] : "";

            float px = n.ContainsKey("pos_x") ? (float)(Variant)n["pos_x"] : 0f;
            float py = n.ContainsKey("pos_y") ? (float)(Variant)n["pos_y"] : 0f;
            float pz = n.ContainsKey("pos_z") ? (float)(Variant)n["pos_z"] : 0f;

            // Discovered stars: bright, colored. Undiscovered: dim white (look like background stars).
            bool isDiscovered = stateToken == "VISITED" || stateToken == "MAPPED";
            float r = 1.0f, g = 0.9f, b = 0.6f;
            if (isDiscovered && _bridge.HasMethod("GetStarInfoV0"))
            {
                var sInfo = _bridge.Call("GetStarInfoV0", nodeId).AsGodotDictionary();
                if (sInfo != null && sInfo.Count > 0)
                {
                    r = sInfo.ContainsKey("color_r") ? (float)sInfo["color_r"] : r;
                    g = sInfo.ContainsKey("color_g") ? (float)sInfo["color_g"] : g;
                    b = sInfo.ContainsKey("color_b") ? (float)sInfo["color_b"] : b;
                }
            }
            else if (!isDiscovered)
            {
                // Dim white — blends with background stars.
                r = 0.7f; g = 0.7f; b = 0.75f;
            }

            float emissionStrength = isDiscovered ? 8.0f : 1.5f;
            float starSize = isDiscovered ? 4.0f : 2.0f;
            var starColor = new Color(r, g, b);
            var star = new MeshInstance3D
            {
                Name = "PersistentStar_" + nodeId,
                Mesh = new SphereMesh { Radius = starSize, Height = starSize * 2.0f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = starColor,
                    EmissionEnabled = true,
                    Emission = starColor,
                    EmissionEnergyMultiplier = emissionStrength,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
                Position = new Vector3(px * scale, py * scale, pz * scale),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            star.AddToGroup("PersistentStar");
            _persistentStarsRoot.AddChild(star);
        }
    }

    // Spawn persistent 3D lane lines between connected stars for real-space flight navigation.
    private void SpawnPersistentLanesV0()
    {
        if (_bridge == null || _persistentLanesRoot == null) return;

        foreach (var child in _persistentLanesRoot.GetChildren())
            child.QueueFree();

        var galSnap = _bridge.GetGalaxySnapshotV0();
        if (galSnap == null) return;

        // Build node position lookup.
        var rawNodes = galSnap.ContainsKey("system_nodes")
            ? (Godot.Collections.Array)galSnap["system_nodes"]
            : new Godot.Collections.Array();
        float scale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;

        var posById = new Dictionary<string, Vector3>();
        var visitedPositions = new List<Vector3>();
        for (int i = 0; i < rawNodes.Count; i++)
        {
            Variant v = rawNodes[i];
            if (v.VariantType != Variant.Type.Dictionary) continue;
            var n = v.AsGodotDictionary();
            var nid = n.ContainsKey("node_id") ? (string)(Variant)n["node_id"] : "";
            if (string.IsNullOrEmpty(nid)) continue;
            float px = n.ContainsKey("pos_x") ? (float)(Variant)n["pos_x"] : 0f;
            float py = n.ContainsKey("pos_y") ? (float)(Variant)n["pos_y"] : 0f;
            float pz = n.ContainsKey("pos_z") ? (float)(Variant)n["pos_z"] : 0f;
            var pos = new Vector3(px * scale, py * scale, pz * scale);
            posById[nid] = pos;
            // Track positions of visited systems for sensor range check.
            if (_bridge != null && !_bridge.IsFirstVisitV0(nid))
                visitedPositions.Add(pos);
        }

        float sensorRange = SensorRangeGalacticU;

        // Draw lane edges.
        var rawEdges = galSnap.ContainsKey("lane_edges")
            ? (Godot.Collections.Array)galSnap["lane_edges"]
            : new Godot.Collections.Array();

        _sharedLaneMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.3f, 0.5f, 0.9f, LaneBaseAlpha),
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.5f, 0.9f),
            EmissionEnergyMultiplier = 3.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        var laneMat = _sharedLaneMaterial;

        var drawnKeys = new HashSet<string>();
        for (int i = 0; i < rawEdges.Count; i++)
        {
            Variant v = rawEdges[i];
            if (v.VariantType != Variant.Type.Dictionary) continue;
            var e = v.AsGodotDictionary();
            var fromId = e.ContainsKey("from_id") ? (string)(Variant)e["from_id"] : "";
            var toId = e.ContainsKey("to_id") ? (string)(Variant)e["to_id"] : "";
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId)) continue;

            // Fog of war: only show lanes where at least one endpoint has been visited.
            if (_bridge != null)
            {
                bool fromVisited = !_bridge.IsFirstVisitV0(fromId);
                bool toVisited = !_bridge.IsFirstVisitV0(toId);
                if (!fromVisited && !toVisited) continue;
            }

            // Sensor range: only show lanes where at least one endpoint is within
            // sensor range of any visited system.
            if (sensorRange > 0 && posById.TryGetValue(fromId, out var fromCheck) && posById.TryGetValue(toId, out var toCheck))
            {
                bool inRange = false;
                for (int vi = 0; vi < visitedPositions.Count; vi++)
                {
                    var vp = visitedPositions[vi];
                    if (fromCheck.DistanceTo(vp) <= sensorRange || toCheck.DistanceTo(vp) <= sensorRange)
                    {
                        inRange = true;
                        break;
                    }
                }
                if (!inRange) continue;
            }

            // Deduplicate bidirectional edges.
            var key = StringComparer.Ordinal.Compare(fromId, toId) < 0
                ? fromId + "|" + toId
                : toId + "|" + fromId;
            if (!drawnKeys.Add(key)) continue;

            if (!posById.TryGetValue(fromId, out var fromPos)) continue;
            if (!posById.TryGetValue(toId, out var toPos)) continue;

            var mesh = new MeshInstance3D
            {
                Name = "PersistentLane_" + key,
                Mesh = new CylinderMesh { TopRadius = 5.0f, BottomRadius = 5.0f, Height = 1.0f },
                MaterialOverride = laneMat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            // Connect gate-to-gate: use pre-computed gate positions so the lane line
            // starts/ends at the exact gate marker positions.
            var gateFrom = GetCachedGateGlobalPositionV0(fromId, toId);
            var gateTo = GetCachedGateGlobalPositionV0(toId, fromId);
            UpdateEdgeTransformV0(mesh, gateFrom, gateTo);
            _persistentLanesRoot.AddChild(mesh);
        }
    }

    // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Get a node's position at galactic scale.
    private Vector3 GetNodeScaledPositionV0(string nodeId)
    {
        if (_bridge == null || string.IsNullOrEmpty(nodeId)) return Vector3.Zero;

        float scale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;
        var galSnap = _bridge.GetGalaxySnapshotV0();
        if (galSnap == null || !galSnap.ContainsKey("system_nodes")) return Vector3.Zero;

        var rawNodes = galSnap["system_nodes"].AsGodotArray();
        for (int i = 0; i < rawNodes.Count; i++)
        {
            var nd = rawNodes[i].AsGodotDictionary();
            var nid = nd.ContainsKey("node_id") ? (string)(Variant)nd["node_id"] : "";
            if (!StringComparer.Ordinal.Equals(nid, nodeId)) continue;

            float px = nd.ContainsKey("pos_x") ? (float)(Variant)nd["pos_x"] : 0f;
            float py = nd.ContainsKey("pos_y") ? (float)(Variant)nd["pos_y"] : 0f;
            float pz = nd.ContainsKey("pos_z") ? (float)(Variant)nd["pos_z"] : 0f;
            return new Vector3(px * scale, py * scale, pz * scale);
        }
        return Vector3.Zero;
    }

    // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Get current star's world position for hero ship positioning.
    public Vector3 GetCurrentStarGlobalPositionV0()
    {
        return _localSystemRoot?.GlobalPosition ?? Vector3.Zero;
    }

    /// Hide/show all Label3D nodes in the local system (suppress during transit/cinematic).
    /// Also hides galaxy overlay labels (NodeLabel, FleetLabel) on all overlay node roots.
    public void SetLocalLabelsVisibleV0(bool visible)
    {
        _localLabelsHidden = !visible;
        if (_localSystemRoot != null)
            SetLabelsVisibleRecursive(_localSystemRoot, visible);

        // Suppress ALL galaxy overlay NodeLabel/FleetLabel nodes.
        // During transit, RefreshFromSnapshotV0 doesn't run (!_overlayOpen),
        // so we must hide them here directly.
        foreach (var kvp in _nodeRootsById)
        {
            var nodeLabel = kvp.Value.GetNodeOrNull<Label3D>("NodeLabel");
            if (nodeLabel != null) nodeLabel.Visible = visible;
            var fleetLabel = kvp.Value.GetNodeOrNull<Label3D>("FleetLabel");
            if (fleetLabel != null) fleetLabel.Visible = visible;
        }
    }

    private static void SetLabelsVisibleRecursive(Node root, bool visible)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Label3D label)
                label.Visible = visible;
            if (child is Node node)
                SetLabelsVisibleRecursive(node, visible);
        }
    }

    // Spawns local system interior from GetSystemSnapshotV0: star, station, lane gates, discovery sites.
    // All positions are seed-derived (deterministic, no wall-clock).
    private void DrawLocalSystemV0(string nodeId)
    {
        ClearLocalSystemV0();
        _currentNodeId = nodeId;

        // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Position local system at star's galactic-scale position.
        if (_localSystemRoot != null)
        {
            _localSystemRoot.Position = GetNodeScaledPositionV0(nodeId);
            // Ensure visible after transit (LOD may have hidden it during travel).
            _localSystemRoot.Visible = true;
        }

        if (_bridge == null) return;

        var snap = _bridge.GetSystemSnapshotV0(nodeId);
        if (snap == null || snap.Count == 0) return;

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

        // Solar repulsion is handled by the flight controller (checks "LocalStar" group distance).

        // GATE.S15.FEEL.STAR_LIGHTING.001: DirectionalLight3D tinted by star class.
        var dirLight = new DirectionalLight3D
        {
            Name = "StarDirLight",
            LightColor = StarClassLightColorV0(starClass),
            LightEnergy = 0.8f,
        };
        _localSystemRoot.AddChild(dirLight);

        // VISUAL_OVERHAUL: Star-class ambient light mood.
        SetSystemAmbientV0(starClass);

        // VISUAL_OVERHAUL: Volumetric fog near star — star-class tinted corona haze.
        float classScale = StarClassVisualScaleV0(starClass);
        var profile = GetSystemVisualProfileV0(starClass);
        var fogVol = new FogVolume
        {
            Name = "StarFogVolume",
            Size = new Vector3(30f * classScale, 10f * classScale, 30f * classScale),
        };
        var fogMat = new FogMaterial
        {
            Density = profile.FogDensity,
            Albedo = profile.FogAlbedo,
            Emission = new Color(
                starColor.R * 0.15f,
                starColor.G * 0.12f,
                starColor.B * 0.10f),
        };
        fogVol.Material = fogMat;
        _localSystemRoot.AddChild(fogVol);

        // 1a. Binary companion (~20% chance, seeded).
        SpawnBinaryCompanionV0(nodeId, starColor, starClass);

        // 1b. Planet orbiting the star.
        var (planetPos, planetType, planetOrbitPivot) = SpawnLocalPlanetV0(nodeId, lumScale);

        // 1c. Moons around the planet (orbit inside planet pivot).
        SpawnMoonsV0(nodeId, planetPos, planetType, planetOrbitPivot);

        // 1d. Asteroid belt between inner and outer zones.
        SpawnAsteroidBeltV0(nodeId, lumScale);

        // 2. Station orbiting near the planet (orbit inside planet pivot).
        SpawnStationV0(snap, nodeId, planetPos, planetOrbitPivot);

        // 3. Lane gate markers (one per neighbor).
        SpawnLaneGatesV0(snap);

        // 4. Discovery site markers at seed-derived orbit positions.
        SpawnDiscoverySitesV0(snap, nodeId);

        // GATE.S15.FEEL.NPC_PROXIMITY.001: Set local node ID before fleet spawn
        // (SpawnFleetsV0 uses _currentLocalNodeId for transit facts query).
        _currentLocalNodeId = nodeId;

        // 5. Fleet markers using transit facts.
        SpawnFleetsV0(snap);

        // GATE.S15.FEEL.AMBIENT_SYSTEM.001: Ambient dust particles — star dust (all systems).
        SpawnAmbientDustV0(nodeId, starClass);

        // GATE.S15.FEEL.NPC_PROXIMITY.001: Enable periodic fleet refresh.
        _fleetRefreshTimer = 1.0;
    }

    // GATE.S15.FEEL.AMBIENT_SYSTEM.001: Ambient dust particle systems.
    // Star dust (all systems): diffuse white/pale-blue motes spread across a large sphere.
    // Asteroid dust (60% of systems): tan/brown motes near the belt radius.
    // VISUAL_OVERHAUL: Star-class drives dust color via visual profile.
    private void SpawnAmbientDustV0(string nodeId, string starClass = "ClassG")
    {
        var dustProfile = GetSystemVisualProfileV0(starClass);
        // ── Star dust (every system) ──
        var starDustProc = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 75.0f, // VISUAL_OVERHAUL: 1.5x scale
            Gravity = Vector3.Zero,
            InitialVelocityMin = 0.1f,
            InitialVelocityMax = 0.5f,
            Color = new Color(dustProfile.DustColor.R, dustProfile.DustColor.G, dustProfile.DustColor.B, dustProfile.DustAlpha),
            ScaleMin = 0.08f,
            ScaleMax = 0.22f,
        };

        var starDust = new GpuParticles3D
        {
            Name = "AmbientStarDust",
            Amount = 80,
            Lifetime = 12.0f,
            SpeedScale = 0.25f,
            ProcessMaterial = starDustProc,
            DrawPass1 = new SphereMesh { Radius = 0.06f, Height = 0.12f },
            Explosiveness = 0.0f,
            Randomness = 1.0f,
        };
        _localSystemRoot.AddChild(starDust);

        // ── Asteroid belt dust (~60% of systems, same seeding as SpawnAsteroidBeltV0) ──
        var beltHash = Fnv1a64(nodeId + "_asteroids");
        if (beltHash % 100UL < 60)
        {
            float beltRadius = 68.0f; // VISUAL_OVERHAUL: 1.5x scale match

            var beltDustProc = new ParticleProcessMaterial
            {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Ring,
                EmissionRingRadius = beltRadius,
                EmissionRingInnerRadius = beltRadius - 6.0f,
                EmissionRingHeight = 4.0f,
                EmissionRingAxis = Vector3.Up,
                Gravity = Vector3.Zero,
                InitialVelocityMin = 0.05f,
                InitialVelocityMax = 0.2f,
                Color = new Color(0.65f, 0.55f, 0.40f, 0.45f),
                ScaleMin = 0.04f,
                ScaleMax = 0.14f,
            };

            var beltDust = new GpuParticles3D
            {
                Name = "AmbientBeltDust",
                Amount = 30,
                Lifetime = 10.0f,
                SpeedScale = 0.2f,
                ProcessMaterial = beltDustProc,
                DrawPass1 = new SphereMesh { Radius = 0.05f, Height = 0.10f },
                Explosiveness = 0.0f,
                Randomness = 1.0f,
            };
            _localSystemRoot.AddChild(beltDust);
        }
    }

    private void ClearLocalSystemV0()
    {
        // GATE.S15.FEEL.NPC_PROXIMITY.001: Stop periodic fleet refresh on system clear.
        _currentLocalNodeId = "";

        if (_localSystemRoot == null) return;
        foreach (Node child in _localSystemRoot.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void SpawnStationV0(Godot.Collections.Dictionary snap, string nodeId, Vector3 planetPos, Node3D planetOrbitPivot)
    {
        var stationDict = snap.ContainsKey("station")
            ? snap["station"].AsGodotDictionary()
            : null;
        if (stationDict == null) return;

        var stationId = stationDict.ContainsKey("node_id")
            ? (string)stationDict["node_id"]
            : nodeId;
        // GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001: Truncate long resource-type lists in station names.
        var stationDisplayName = stationDict.ContainsKey("node_name") && !string.IsNullOrEmpty((string)stationDict["node_name"])
            ? TruncateResourceTypesV0((string)stationDict["node_name"]) + " Station"
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
            // GATE.S14.DOCK.PROXIMITY_TIGHTEN.001: Station dock proximity box.
            Shape = new BoxShape3D { Size = new Vector3(5f, 3f, 5f) }
        };
        station.AddChild(collider);

        // GATE.S1.VISUAL_POLISH.STRUCTURES.001: ring/cylinder station geometry with slow rotation.
        var stationVisual = new Node3D { Name = "StationVisual" };
        // Attach spinning script for slow Y-axis rotation.
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null) stationVisual.SetScript(spinScript);

        var hullMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.28f, 0.30f, 0.34f),
            Roughness = 0.50f,
            Metallic = 0.55f,
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.06f, 0.08f),
            EmissionEnergyMultiplier = 0.8f,
        };

        // VISUAL_OVERHAUL: Hash-select Kenney hangar model for station variety.
        bool modelLoaded = false;
        string[] stationModels =
        {
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_largeA.glb",
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_largeB.glb",
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_roundA.glb",
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_roundB.glb",
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_roundGlass.glb",
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_smallA.glb",
            "res://addons/kenney_space_kit/Models/GLTF format/hangar_smallB.glb",
        };
        var stationHash = Fnv1a64(nodeId + "_station_model");
        int modelIdx = (int)(stationHash % (ulong)stationModels.Length);
        if (Godot.FileAccess.FileExists(stationModels[modelIdx]))
        {
            var modelScene = GD.Load<PackedScene>(stationModels[modelIdx]);
            if (modelScene != null)
            {
                var stationModel = modelScene.Instantiate<Node3D>();
                stationModel.Name = "StationModel";
                stationModel.Scale = Vector3.One * 3.0f;
                foreach (var child in stationModel.FindChildren("*", "MeshInstance3D"))
                {
                    if (child is MeshInstance3D meshChild)
                        meshChild.MaterialOverride = hullMat;
                }
                stationVisual.AddChild(stationModel);
                modelLoaded = true;
            }
        }

        // Fallback: procedural hub + ring if model load fails.
        if (!modelLoaded)
        {
            var hubMesh = new MeshInstance3D
            {
                Name = "StationHub",
                Mesh = new CylinderMesh
                {
                    TopRadius = 1.0f,
                    BottomRadius = 1.0f,
                    Height = 2.0f,
                    RadialSegments = 12
                },
                MaterialOverride = hullMat
            };
            stationVisual.AddChild(hubMesh);

            var ringMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.25f, 0.30f, 0.38f),
                Roughness = 0.40f,
                Metallic = 0.65f,
                EmissionEnabled = true,
                Emission = new Color(0.04f, 0.05f, 0.07f),
                EmissionEnergyMultiplier = 0.6f,
            };
            var ringMesh = new MeshInstance3D
            {
                Name = "StationRing",
                Mesh = new TorusMesh
                {
                    InnerRadius = 1.6f,
                    OuterRadius = 2.0f,
                    Rings = 24,
                    RingSegments = 12
                },
                MaterialOverride = ringMat
            };
            ringMesh.RotateX(Mathf.Pi / 2.0f);
            stationVisual.AddChild(ringMesh);
        }

        // GATE.S7.FACTION_VIS.STATION_STYLE.001: Accent color from controlling faction.
        var accentColor = new Color(0.3f, 0.7f, 1.0f); // default blue
        if (_bridge != null && !string.IsNullOrEmpty(nodeId))
        {
            var territory = _bridge.GetTerritoryAccessV0(nodeId);
            var factionId = territory.ContainsKey("faction_id") ? (string)territory["faction_id"] : "";
            if (!string.IsNullOrEmpty(factionId))
            {
                var colors = _bridge.GetFactionColorsV0(factionId);
                if (colors.ContainsKey("accent"))
                    accentColor = (Color)colors["accent"];
            }
        }

        // Emissive accent band — navigation beacon / docking lights.
        var accentMat = new StandardMaterial3D
        {
            AlbedoColor = accentColor,
            EmissionEnabled = true,
            Emission = accentColor,
            EmissionEnergyMultiplier = 3.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        var accentBand = new MeshInstance3D
        {
            Name = "StationAccent",
            Mesh = new TorusMesh
            {
                InnerRadius = 1.9f,
                OuterRadius = 2.1f,
                Rings = 24,
                RingSegments = 8
            },
            MaterialOverride = accentMat
        };
        accentBand.RotateX(Mathf.Pi / 2.0f);
        stationVisual.AddChild(accentBand);

        // Window lights on the hub — small emissive spheres.
        var windowMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.9f, 0.7f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.9f, 0.7f),
            EmissionEnergyMultiplier = 4.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        for (int wi = 0; wi < 6; wi++)
        {
            float wAngle = wi * MathF.PI * 2f / 6f;
            var window = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.08f, Height = 0.16f },
                MaterialOverride = windowMat,
                Position = new Vector3(MathF.Cos(wAngle) * 1.05f, 0.3f, MathF.Sin(wAngle) * 1.05f),
            };
            window.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            stationVisual.AddChild(window);
        }

        station.AddChild(stationVisual);

        // Invisible legacy mesh kept so callers searching for "StationMesh" by name still work.
        var mesh = new MeshInstance3D
        {
            Name = "StationMesh",
            Visible = false,
            Mesh = new BoxMesh { Size = new Vector3(4f, 2f, 4f) },
            MaterialOverride = new StandardMaterial3D
            {
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = new Color(0f, 1f, 0f, 0.3f)
            }
        };
        station.AddChild(mesh);

        // GATE.S1.VISUAL_POLISH.HUD_LABELS.001: Label3D over station showing station name.
        // GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001: Width clamped to prevent overlap/truncation.
        var stationLabel = new Label3D
        {
            Name = "StationLabel",
            Text = stationDisplayName,
            PixelSize = 0.12f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(0.4f, 1.0f, 0.4f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Width = 200f,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        stationLabel.Position = new Vector3(0f, 4f, 0f);
        station.AddChild(stationLabel);

        // GATE.S7.FACTION_VIS.STATION_STYLE.001: Faction name banner below station label.
        if (_bridge != null && !string.IsNullOrEmpty(nodeId))
        {
            var terr = _bridge.GetTerritoryAccessV0(nodeId);
            var fid = terr.ContainsKey("faction_id") ? (string)terr["faction_id"] : "";
            if (!string.IsNullOrEmpty(fid))
            {
                var factionBanner = new Label3D
                {
                    Name = "FactionBanner",
                    Text = fid,
                    PixelSize = 0.08f,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    Modulate = accentColor,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                factionBanner.Position = new Vector3(0f, 5.5f, 0f);
                station.AddChild(factionBanner);
            }
        }

        // Station orbit pivot: centered at planet position, slow orbit around planet.
        var stationOrbitPivot = new Node3D { Name = "StationOrbitPivot" };
        stationOrbitPivot.Position = planetPos; // Planet position within the planet orbit pivot
        var stationOrbitSpin = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (stationOrbitSpin != null)
        {
            stationOrbitPivot.SetScript(stationOrbitSpin);
            stationOrbitPivot.Set("spin_speed_y", 0.08f); // Station orbits planet
        }

        // GATE.X.UI_POLISH.LOCAL_DENSITY.001: Station closer to planet (was 8u).
        station.Position = DeriveOrbitPositionV0(nodeId + "_station_offset", 4.0f);
        station.AddToGroup("Station");
        RegisterDockTargetV0(station, "STATION", stationId);

        // Dock confirmation: show prompt on proximity, dock on E key.
        station.BodyEntered += (body) =>
        {
            var gm = GetNode<Node>("/root/GameManager");
            if (gm != null && gm.HasMethod("on_dock_proximity_v0"))
                gm.Call("on_dock_proximity_v0", station);
        };
        station.BodyExited += (body) =>
        {
            var gm = GetNode<Node>("/root/GameManager");
            if (gm != null && gm.HasMethod("on_dock_proximity_exit_v0"))
                gm.Call("on_dock_proximity_exit_v0", station);
        };

        stationOrbitPivot.AddChild(station);
        // Add to planet orbit pivot so station follows the planet around the star.
        if (planetOrbitPivot != null)
            planetOrbitPivot.AddChild(stationOrbitPivot);
        else
            _localSystemRoot.AddChild(station);
    }

    private void SpawnLaneGatesV0(Godot.Collections.Dictionary snap)
    {
        if (!snap.ContainsKey("lane_gate")) return;
        var gates = snap["lane_gate"].AsGodotArray();

        var currentNodeId = snap.ContainsKey("node_id") ? (string)snap["node_id"] : "";

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
            _localSystemRoot.AddChild(marker);
            marker.AddToGroup("LaneGate");

            // Use pre-computed gate position from cache.
            var cacheKey = currentNodeId + "|" + neighborId;
            Vector3 gatePos;
            if (_gateLocalPositionCache.TryGetValue(cacheKey, out var cachedPos))
            {
                gatePos = cachedPos;
            }
            else
            {
                // Fallback: direction-based (cache may not be populated for this pair).
                var neighborPos = GetNodeScaledPositionV0(neighborId);
                var currentPos = GetNodeScaledPositionV0(currentNodeId);
                if (currentPos != Vector3.Zero && neighborPos != Vector3.Zero && currentPos != neighborPos)
                {
                    var dir = (neighborPos - currentPos).Normalized();
                    gatePos = dir * LaneGateDistanceU;
                }
                else
                {
                    gatePos = DeriveLaneGatePositionV0(i, gates.Count, LaneGateDistanceU);
                }
            }
            marker.Position = gatePos;
            if (gatePos != Vector3.Zero)
                marker.LookAt(marker.Position + gatePos.Normalized(), Vector3.Up);
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

    // GATE.S16.NPC_ALIVE.SPAWN_SYSTEM.001: Spawn physical NPC ships using transit facts.
    // Ships are placed at their current logical position: idle fleets orbit, arriving fleets start at gate.
    private void SpawnFleetsV0(Godot.Collections.Dictionary snap)
    {
        if (_bridge == null) return;
        var transitFacts = _bridge.GetFleetTransitFactsV0(_currentLocalNodeId ?? "");

        for (int i = 0; i < transitFacts.Count; i++)
        {
            var f = transitFacts[i].AsGodotDictionary();
            var fleetId = f.ContainsKey("fleet_id") ? (string)f["fleet_id"] : "";
            if (string.IsNullOrEmpty(fleetId)) continue;
            // Skip player fleet.
            if (StringComparer.Ordinal.Equals(fleetId, "fleet_trader_1")) continue;

            var ship = SpawnNpcShipV0(fleetId);
            if (ship == null) continue;

            var state = f.ContainsKey("state") ? (string)f["state"] : "Idle";
            var destNodeId = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";

            // Position ship based on state.
            if (state == "Traveling" && !string.IsNullOrEmpty(destNodeId))
            {
                // Arriving from a neighboring system — start at the gate.
                var gatePos = GetGateLocalPositionForNeighborV0(destNodeId);
                ship.Position = gatePos != Vector3.Zero ? gatePos : DeriveOrbitPositionV0(fleetId + "_local", LaneGateDistanceU);
            }
            else
            {
                // Idle at this node — orbit position.
                ship.Position = DeriveOrbitPositionV0(fleetId + "_local", FleetOrbitRadiusU);
            }

            ship.AddToGroup("FleetShip");
            _localSystemRoot.AddChild(ship);

            // Set movement target: idle ships patrol around, arriving ships fly to orbit.
            if (ship.HasMethod("set_target"))
            {
                var targetPos = DeriveOrbitPositionV0(fleetId + "_local", FleetOrbitRadiusU);
                ship.Call("set_target", targetPos, (float)6.0);
            }
            if (ship.HasMethod("update_transit"))
                ship.Call("update_transit", f);
        }

        // Init combat HP for all fleets (idempotent).
        var bridge = GetNodeOrNull<Node>("/root/SimBridge");
        if (bridge != null && bridge.HasMethod("InitFleetCombatHpV0"))
            bridge.Call("InitFleetCombatHpV0");
    }

    // GATE.S15.FEEL.NPC_PROXIMITY.001: Refresh fleet markers for NPC arrivals/departures.
    // Uses transit facts to drive NPC ship positions and warp in/out at gates.
    private void RefreshLocalFleetsV0()
    {
        if (_bridge == null || _localSystemRoot == null) return;
        if (string.IsNullOrEmpty(_currentLocalNodeId)) return;

        var transitFacts = _bridge.GetFleetTransitFactsV0(_currentLocalNodeId);

        // Collect fleet IDs from transit facts.
        var transitFleetIds = new HashSet<string>(StringComparer.Ordinal);
        var transitById = new Dictionary<string, Godot.Collections.Dictionary>(StringComparer.Ordinal);
        for (int i = 0; i < transitFacts.Count; i++)
        {
            var f = transitFacts[i].AsGodotDictionary();
            var fleetId = f.ContainsKey("fleet_id") ? (string)f["fleet_id"] : "";
            if (string.IsNullOrEmpty(fleetId)) continue;
            if (StringComparer.Ordinal.Equals(fleetId, "fleet_trader_1")) continue;
            transitFleetIds.Add(fleetId);
            transitById[fleetId] = f;
        }

        // Collect existing fleet ship IDs from the scene tree.
        var existingFleetIds = new HashSet<string>(StringComparer.Ordinal);
        var existingNodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        foreach (Node child in _localSystemRoot.GetChildren())
        {
            if (child is Node3D n3d && n3d.IsInGroup("FleetShip"))
            {
                var name = n3d.Name.ToString();
                var id = name.StartsWith("Fleet_") ? name.Substring(6) : name;
                existingFleetIds.Add(id);
                existingNodes[id] = child;
            }
        }

        // Departing fleets: fly to departure gate, then warp out.
        var warpScript = GD.Load<Script>("res://scripts/vfx/warp_effect.gd");
        foreach (var id in existingFleetIds)
        {
            if (!transitFleetIds.Contains(id) && existingNodes.TryGetValue(id, out var node))
            {
                if (node is Node3D n3d && warpScript != null)
                    warpScript.Call("play_warp_out", n3d);
                else
                    node.QueueFree();
            }
        }

        // Newly arrived fleets: spawn at arrival gate, fly inward.
        foreach (var id in transitFleetIds)
        {
            var f = transitById[id];
            if (!existingFleetIds.Contains(id))
            {
                var ship = SpawnNpcShipV0(id);
                if (ship == null) continue;

                // Determine arrival gate from the fleet's source edge/neighbor.
                var currentNodeId = f.ContainsKey("current_node_id") ? (string)f["current_node_id"] : "";
                var destNodeId = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                var sourceNeighbor = !StringComparer.Ordinal.Equals(currentNodeId, _currentLocalNodeId) ? currentNodeId : "";
                var gatePos = !string.IsNullOrEmpty(sourceNeighbor)
                    ? GetGateLocalPositionForNeighborV0(sourceNeighbor)
                    : Vector3.Zero;
                if (gatePos == Vector3.Zero)
                    gatePos = DeriveOrbitPositionV0(id + "_arrive", LaneGateDistanceU);

                ship.Position = gatePos;
                ship.AddToGroup("FleetShip");
                _localSystemRoot.AddChild(ship);

                // Play warp-in VFX at the gate.
                if (warpScript != null)
                    warpScript.Call("play_warp_in", _localSystemRoot, gatePos);

                // Set target to orbit position so the ship flies inward from the gate.
                if (ship.HasMethod("set_target"))
                    ship.Call("set_target", DeriveOrbitPositionV0(id + "_local", FleetOrbitRadiusU), (float)6.0);
                if (ship.HasMethod("update_transit"))
                    ship.Call("update_transit", f);
            }
            else if (existingNodes.TryGetValue(id, out var existingNode))
            {
                // Update movement target for existing ships based on transit state.
                var state = f.ContainsKey("state") ? (string)f["state"] : "Idle";
                var destNodeId = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";

                if (existingNode.HasMethod("update_transit"))
                    existingNode.Call("update_transit", f);

                if (existingNode.HasMethod("set_target"))
                {
                    Vector3 target;
                    if (state == "Traveling" && !string.IsNullOrEmpty(destNodeId)
                        && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId))
                    {
                        // Fleet leaving this system: fly to the departure gate.
                        target = GetGateLocalPositionForNeighborV0(destNodeId);
                        if (target == Vector3.Zero)
                            target = DeriveOrbitPositionV0(id + "_depart", LaneGateDistanceU);
                    }
                    else
                    {
                        // Idle or docked: orbit around the system.
                        target = DeriveOrbitPositionV0(id + "_local", FleetOrbitRadiusU);
                    }
                    existingNode.Call("set_target", target, (float)6.0);
                }
            }
        }
    }

    // Find a lane gate's local-space position for a given neighbor node ID.
    private Vector3 GetGateLocalPositionForNeighborV0(string neighborId)
    {
        // Primary: pre-computed cache (no scene-tree dependency).
        if (!string.IsNullOrEmpty(_currentLocalNodeId) && !string.IsNullOrEmpty(neighborId))
        {
            var cacheKey = _currentLocalNodeId + "|" + neighborId;
            if (_gateLocalPositionCache.TryGetValue(cacheKey, out var cached))
                return cached;
        }

        // Fallback: scene tree search.
        if (_localSystemRoot == null || string.IsNullOrEmpty(neighborId)) return Vector3.Zero;
        foreach (var child in _localSystemRoot.GetChildren())
        {
            if (child is not Node3D n3d) continue;
            if (!n3d.IsInGroup("LaneGate")) continue;
            if (n3d.HasMeta("neighbor_node_id") && (string)n3d.GetMeta("neighbor_node_id") == neighborId)
                return n3d.Position;
        }
        return Vector3.Zero;
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

    // GATE.S16.NPC_ALIVE.SPAWN_SYSTEM.001: Instantiate physical NPC ship from npc_ship.tscn.
    // Falls back to CreateFleetMarkerV0 if scene not assigned.
    private Node3D SpawnNpcShipV0(string fleetId)
    {
        // Player fleet still uses the old marker (player is a separate RigidBody3D).
        if (StringComparer.Ordinal.Equals(fleetId, "fleet_trader_1"))
            return CreateFleetMarkerV0(fleetId);

        if (NpcShipScene == null)
            return CreateFleetMarkerV0(fleetId); // fallback

        var ship = NpcShipScene.Instantiate<CharacterBody3D>();
        ship.Name = "Fleet_" + fleetId;
        ship.Set("fleet_id", fleetId);

        // Load Quaternius model and attach to ShipVisual.
        var modelScene = LoadFleetModelSceneV0(fleetId);
        if (modelScene != null && ship.HasMethod("load_model"))
            ship.Call("load_model", modelScene);

        // Set hostile meta — only Patrol fleets (role 2) are hostile.
        ship.SetMeta("fleet_id", fleetId);
        int role = (_bridge != null) ? _bridge.GetFleetRoleV0(fleetId) : 0;
        ship.SetMeta("is_hostile", role == 2);

        // GATE.T30.GALPOP.BRIDGE_TRANSIT.007: Apply faction color from fleet's owner faction.
        // Previously used territory of current node — now uses fleet's actual owner for
        // correct tinting when a faction's fleet visits another faction's territory.
        if (_bridge != null && ship.HasMethod("set_faction_color"))
        {
            var ownerId = GetFleetOwnerIdV0(fleetId);
            if (!string.IsNullOrEmpty(ownerId))
            {
                ship.SetMeta("owner_id", ownerId);
                var colors = _bridge.GetFactionColorsV0(ownerId);
                if (colors.ContainsKey("primary"))
                    ship.Call("set_faction_color", colors["primary"]);
            }
        }

        // Wire FleetArea body_entered signal for combat proximity.
        var area = ship.GetNodeOrNull<Area3D>("FleetArea");
        if (area != null)
        {
            area.SetMeta("fleet_id", fleetId);
            area.BodyEntered += (body) => _OnFleetBodyEnteredV0(body, fleetId);
        }

        return ship;
    }

    // GATE.T30.GALPOP.BRIDGE_TRANSIT.007: Get fleet owner faction ID via bridge.
    private string GetFleetOwnerIdV0(string fleetId)
    {
        if (_bridge == null) return "";
        return _bridge.GetFleetOwnerIdV0(fleetId);
    }

    // Returns the PackedScene for the Quaternius model (used by SpawnNpcShipV0).
    private PackedScene LoadFleetModelSceneV0(string fleetId)
    {
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
        string modelPath = $"res://addons/quaternius-ultimate-spaceships-pack/meshes/{modelName}/{modelName}_{colorName}.tscn";

        if (Godot.FileAccess.FileExists(modelPath))
            return GD.Load<PackedScene>(modelPath);

        const string FallbackPath = "res://addons/kenney_space_kit/Models/GLTF format/craft_racer.glb";
        if (Godot.FileAccess.FileExists(FallbackPath))
            return GD.Load<PackedScene>(FallbackPath);

        return null;
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

        // GATE.S13.WORLD.HOSTILE_LABELS.001: Show "HOSTILE" in red for enemy fleets.
        // Starts hidden — fleet_ai.gd manages visibility based on reputation.
        var fleetLabel = new Label3D
        {
            Name = "FleetLabel",
            Text = "HOSTILE",
            PixelSize = 0.12f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(1.0f, 0.2f, 0.2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
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
        // Hostile meta determined by role — only Patrol (role 2) starts hostile.
        // fleet_ai.gd._ready() further resolves via reputation check.
        var fleetAiScript = GD.Load<Script>("res://scripts/core/fleet_ai.gd");
        if (fleetAiScript != null)
        {
            root.SetScript(fleetAiScript);
            int markerRole = (_bridge != null) ? _bridge.GetFleetRoleV0(fleetId) : 0;
            root.SetMeta("is_hostile", markerRole == 2);
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

    // VISUAL_OVERHAUL: Star-class visual profile — drives fog, dust, and ambient per system.
    private record SystemVisualProfile(
        float FogDensity, Color FogAlbedo,
        Color DustColor, float DustAlpha,
        float GlowMultiplier);

    private static SystemVisualProfile GetSystemVisualProfileV0(string starClass) => starClass switch
    {
        "ClassO" => new(0.025f, new Color(0.15f, 0.20f, 0.40f),
            new Color(0.6f, 0.7f, 1.0f), 0.4f, 1.3f),
        "ClassA" => new(0.018f, new Color(0.25f, 0.25f, 0.35f),
            new Color(0.7f, 0.75f, 0.9f), 0.45f, 1.1f),
        "ClassF" => new(0.015f, new Color(0.28f, 0.26f, 0.22f),
            new Color(0.85f, 0.88f, 1.0f), 0.5f, 1.0f),
        "ClassG" => new(0.015f, new Color(0.30f, 0.25f, 0.15f),
            new Color(0.85f, 0.90f, 1.0f), 0.55f, 1.0f),
        "ClassK" => new(0.020f, new Color(0.35f, 0.20f, 0.08f),
            new Color(0.9f, 0.7f, 0.5f), 0.5f, 0.9f),
        "ClassM" => new(0.030f, new Color(0.30f, 0.08f, 0.04f),
            new Color(1.0f, 0.6f, 0.4f), 0.6f, 0.8f),
        _ => new(0.015f, new Color(0.28f, 0.26f, 0.22f),
            new Color(0.85f, 0.88f, 1.0f), 0.5f, 1.0f),
    };

    // GATE.S15.FEEL.STAR_LIGHTING.001: Star-class to directional light color mapping.
    private static Color StarClassLightColorV0(string starClass) => starClass switch
    {
        "ClassO" => new Color(0.6f, 0.7f, 1.0f),   // Blue-white
        "ClassA" => new Color(0.9f, 0.9f, 1.0f),   // White
        "ClassF" => new Color(1.0f, 0.95f, 0.8f),  // Yellow-white
        "ClassG" => new Color(1.0f, 0.9f, 0.6f),   // Warm yellow
        "ClassK" => new Color(1.0f, 0.7f, 0.4f),   // Orange
        "ClassM" => new Color(1.0f, 0.4f, 0.2f),   // Deep red
        _ => new Color(1.0f, 0.9f, 0.6f),          // Default warm yellow
    };

    private static void TintStarShaderV0(Node3D starNode, Color starColor)
    {
        // Derive a dark and bright variant from the star class color.
        // GATE.S14.STAR.TINT_FIX.001: Preserve color identity — uniform scaling keeps hue intact.
        var darkColor = new Color(starColor.R * 0.25f, starColor.G * 0.25f, starColor.B * 0.25f);
        var brightColor = new Color(
            Mathf.Min(starColor.R * 1.1f, 1.0f),
            Mathf.Min(starColor.G * 1.1f, 1.0f),
            Mathf.Min(starColor.B * 1.1f, 1.0f));

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
        // Override baked values: disable emit so glow post-process ignores
        // the Fresnel HDR, and reduce intensity/alpha to a subtle halo.
        var atmo = starNode.GetNodeOrNull<MeshInstance3D>("Atmosphere");
        if (atmo != null)
        {
            var atmoMat = atmo.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
            if (atmoMat != null)
            {
                atmoMat.SetShaderParameter("color_2", brightColor);
                atmoMat.SetShaderParameter("intensity", 6.0f);
                atmoMat.SetShaderParameter("alpha", 0.35f);
                atmoMat.SetShaderParameter("emit", true);
                atmoMat.SetShaderParameter("amount", 3.5f);
            }
        }
    }

    // VISUAL_OVERHAUL: Planet atmosphere emission + type-specific tinting.
    private static void TintPlanetAtmosphereV0(Node3D planetNode, string planetType, string nodeId)
    {
        // Only planets with atmosphere child (Terrestrial, Gaseous, Ice, Sand have them).
        var atmo = planetNode.GetNodeOrNull<MeshInstance3D>("Atmosphere");
        if (atmo == null) return;
        var atmoMat = atmo.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
        if (atmoMat == null) return;

        // Enable emission so atmosphere rim blooms with post-processing.
        atmoMat.SetShaderParameter("emit", true);
        atmoMat.SetShaderParameter("intensity", 2.5f);
        atmoMat.SetShaderParameter("alpha", 0.25f);
        atmoMat.SetShaderParameter("amount", 4.0f);

        // Type-specific atmosphere tint.
        var rimColor = planetType switch
        {
            "Terrestrial" => new Color(0.4f, 0.65f, 1.0f),  // Earth-like blue
            "Ice"         => new Color(0.5f, 0.7f, 1.0f),   // Cold blue
            "Gaseous"     => new Color(0.8f, 0.6f, 0.3f),   // Warm amber
            "Sand"        => new Color(0.7f, 0.5f, 0.2f),   // Dusty orange
            "Lava"        => new Color(1.0f, 0.3f, 0.1f),   // Volcanic red
            _             => new Color(0.6f, 0.65f, 0.7f),  // Neutral
        };
        atmoMat.SetShaderParameter("color_2", rimColor);

        // Hash-driven hue shift on body shader (±15%) so two same-type planets differ.
        var tintHash = Fnv1a64(nodeId + "_planet_tint");
        float hueShift = ((float)(tintHash % 30UL) - 15.0f) / 100.0f;
        if (planetNode is MeshInstance3D bodyMesh && bodyMesh.Mesh != null)
        {
            var bodyMat = bodyMesh.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
            if (bodyMat != null)
            {
                // Shift color_2 (primary surface color) slightly for variety.
                var origColor = bodyMat.GetShaderParameter("color_2");
                if (origColor.VariantType == Variant.Type.Color)
                {
                    var c = origColor.AsColor();
                    c.H = Mathf.PosMod(c.H + hueShift, 1.0f);
                    bodyMat.SetShaderParameter("color_2", c);
                }
            }
        }
    }

    // VISUAL_OVERHAUL: Star-class ambient light override — each system has a distinct color mood.
    private void SetSystemAmbientV0(string starClass)
    {
        var we = GetNodeOrNull<WorldEnvironment>("/root/Main/WorldEnvironment");
        if (we?.Environment == null) return;
        var (col, energy) = starClass switch
        {
            "ClassO" => (new Color(0.08f, 0.10f, 0.20f), 0.25f),
            "ClassA" => (new Color(0.12f, 0.12f, 0.18f), 0.28f),
            "ClassF" => (new Color(0.14f, 0.13f, 0.12f), 0.30f),
            "ClassG" => (new Color(0.15f, 0.13f, 0.10f), 0.32f),
            "ClassK" => (new Color(0.16f, 0.10f, 0.06f), 0.28f),
            "ClassM" => (new Color(0.14f, 0.06f, 0.04f), 0.20f),
            _ => (new Color(0.15f, 0.13f, 0.10f), 0.30f),
        };
        we.Environment.AmbientLightColor = col;
        we.Environment.AmbientLightEnergy = energy;
    }

    // Base planet orbit radius by type. Scaled by lumScale at call site.
    // Star visual radius ~6u, so innermost orbit starts well clear.
    // GATE.X.UI_POLISH.LOCAL_DENSITY.001: ~40% tighter orbits for denser local systems.
    // Pace overhaul: 1.6x spread so systems feel spacious at 80u camera altitude.
    // VISUAL_OVERHAUL: 1.5x scale for vast-feeling systems.
    private static float PlanetBaseOrbitV0(string planetType) => planetType switch
    {
        "Lava"        => 27.0f,   // Innermost — volcanic, near star
        "Sand"        => 33.0f,   // Inner zone
        "Terrestrial" => 39.0f,   // Habitable zone
        "Barren"      => 45.0f,   // Outer rocky
        "Ice"         => 51.0f,   // Outer cold zone
        "Gaseous"     => 60.0f,   // Far out — gas giant
        _             => 39.0f,
    };

    // Planet visual scale by type. Star is ~6u radius, largest planet ~4u (70% of star).
    // Addon scenes have ~400u baked scale, so 0.01 → ~4u visible radius.
    // VISUAL_OVERHAUL: Increased ~25% for better visibility from camera altitude.
    private static float PlanetVisualScaleV0(string planetType) => planetType switch
    {
        "Gaseous"     => 0.013f,   // ~5.2u — imposing gas giant
        "Terrestrial" => 0.010f,   // ~4.0u
        "Ice"         => 0.009f,   // ~3.6u
        "Sand"        => 0.009f,   // ~3.6u
        "Lava"        => 0.008f,   // ~3.2u
        "Barren"      => 0.007f,   // ~2.8u
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

    private void SpawnMoonsV0(string nodeId, Vector3 planetPos, string planetType, Node3D planetOrbitPivot)
    {
        var hash = Fnv1a64(nodeId + "_moons");
        int count = MoonCountV0(planetType, hash);
        if (count <= 0) return;

        const string MoonScenePath = "res://addons/naejimer_3d_planet_generator/scenes/planet_no_atmosphere.tscn";
        PackedScene moonScene = null;
        if (Godot.FileAccess.FileExists(MoonScenePath))
            moonScene = GD.Load<PackedScene>(MoonScenePath);

        var moonSpin = GD.Load<Script>("res://scripts/spinning_node.gd");

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
            float moonScale = 0.005f + (float)(moonHash % 3UL) * 0.001f;
            container.Scale = new Vector3(moonScale, moonScale, moonScale);
            container.Position = moonOffset; // Offset relative to moon orbit pivot center
            container.AddChild(moonNode);

            // Moon orbit pivot: centered at planet position, spins to orbit the planet.
            var moonOrbitPivot = new Node3D { Name = "MoonOrbitPivot_" + i };
            moonOrbitPivot.Position = planetPos; // Planet position within the planet orbit pivot
            if (moonSpin != null)
            {
                moonOrbitPivot.SetScript(moonSpin);
                float orbitSpeed = 0.15f + (float)(moonHash % 5UL) * 0.05f; // 0.15-0.35
                moonOrbitPivot.Set("spin_speed_y", orbitSpeed);
            }
            moonOrbitPivot.AddChild(container);

            // Add to planet orbit pivot so moons follow the planet around the star.
            if (planetOrbitPivot != null)
                planetOrbitPivot.AddChild(moonOrbitPivot);
            else
                _localSystemRoot.AddChild(moonOrbitPivot);
        }
    }

    // Asteroid belt — ring of rocky debris between inner and outer zones.
    // Kenney Space Kit meteor models for realistic asteroid shapes.
    private static readonly string[] MeteorModelPaths =
    {
        "res://addons/kenney_space_kit/Models/GLTF format/meteor.glb",
        "res://addons/kenney_space_kit/Models/GLTF format/meteor_detailed.glb",
        "res://addons/kenney_space_kit/Models/GLTF format/meteor_half.glb",
    };

    private void SpawnAsteroidBeltV0(string nodeId, float lumScale)
    {
        var hash = Fnv1a64(nodeId + "_asteroids");
        // ~60% of systems have a visible asteroid belt.
        if (hash % 100UL >= 60) return;

        // VISUAL_OVERHAUL: Belt at 68u base (1.5x scale).
        float beltRadius = MathF.Max(68.0f * lumScale, 60.0f);
        int rockCount = 40 + (int)(hash % 30UL); // 40-69 rocks

        // Load Kenney meteor models for realistic rocky shapes.
        var meteorScenes = new PackedScene[MeteorModelPaths.Length];
        int loadedCount = 0;
        for (int m = 0; m < MeteorModelPaths.Length; m++)
        {
            meteorScenes[m] = GD.Load<PackedScene>(MeteorModelPaths[m]);
            if (meteorScenes[m] != null) loadedCount++;
        }

        // Belt pivot: all rocks orbit the star together, very slow rotation.
        var beltPivot = new Node3D { Name = "AsteroidBeltPivot" };
        var beltSpin = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (beltSpin != null)
        {
            beltPivot.SetScript(beltSpin);
            beltPivot.Set("spin_speed_y", 0.005f);
        }

        // GATE.S14.ASTEROID.SHAPE_VARIETY.001: Mixed shapes and materials.
        // Rocks need subtle emission to be visible from camera altitude (~80u).
        var rockMatLight = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.45f, 0.40f, 0.32f),
            Roughness = 0.85f,
            EmissionEnabled = true,
            Emission = new Color(0.08f, 0.07f, 0.05f),
            EmissionEnergyMultiplier = 0.5f,
        };
        var rockMatDark = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.30f, 0.26f, 0.20f),
            Roughness = 0.90f,
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.04f, 0.03f),
            EmissionEnergyMultiplier = 0.4f,
        };

        for (int i = 0; i < rockCount; i++)
        {
            var rockHash = Fnv1a64(nodeId + "_rock_" + i);
            float angle = ((float)i / rockCount) * 2.0f * MathF.PI;
            float rJitter = beltRadius + ((float)(rockHash % 8UL) - 4.0f);
            // VISUAL_OVERHAUL: Increased Y scatter for visible 3D depth from top-down camera.
            float yJitter = ((float)(rockHash % 9UL) - 4.0f) * 1.2f; // ±4.8u (was ±1.2u)

            // Visible rock sizes: 1.0-4.0u (4 size tiers for natural variety).
            float rockSize = 1.0f + (float)(rockHash % 4UL) * 1.0f; // 1.0, 2.0, 3.0, 4.0u
            var mat = (rockHash % 2UL == 0) ? rockMatLight : rockMatDark;

            // VISUAL_OVERHAUL: ~15% of rocks have emissive ore veins.
            if (rockHash % 100UL < 15)
            {
                var oreColor = ((rockHash >> 24) % 3UL) switch
                {
                    0 => new Color(0.1f, 0.4f, 0.6f), // blue crystal
                    1 => new Color(0.5f, 0.2f, 0.1f), // copper
                    _ => new Color(0.2f, 0.5f, 0.2f), // green mineral
                };
                mat = new StandardMaterial3D
                {
                    AlbedoColor = mat.AlbedoColor,
                    Roughness = mat.Roughness,
                    EmissionEnabled = true,
                    Emission = oreColor,
                    EmissionEnergyMultiplier = 1.5f,
                };
            }

            Node3D rock;
            int modelIdx = (int)(rockHash % (ulong)MeteorModelPaths.Length);
            if (loadedCount > 0 && meteorScenes[modelIdx] != null)
            {
                // Use Kenney meteor models — irregular rocky shapes.
                rock = meteorScenes[modelIdx].Instantiate<Node3D>();
                rock.Scale = Vector3.One * rockSize * 0.5f;
                // Apply asteroid material to all mesh children.
                foreach (var child in rock.FindChildren("*", "MeshInstance3D"))
                {
                    if (child is MeshInstance3D meshChild)
                        meshChild.MaterialOverride = mat;
                }
            }
            else
            {
                // Fallback: deformed sphere if models unavailable.
                var meshInst = new MeshInstance3D
                {
                    Mesh = new SphereMesh { Radius = rockSize * 0.5f, Height = rockSize * 0.7f },
                    MaterialOverride = mat,
                };
                rock = meshInst;
            }

            rock.RotateX((float)(rockHash % 360UL) * (MathF.PI / 180f));
            rock.RotateY((float)((rockHash >> 8) % 360UL) * (MathF.PI / 180f));
            rock.RotateZ((float)((rockHash >> 16) % 360UL) * (MathF.PI / 180f));

            rock.Position = new Vector3(
                MathF.Cos(angle) * rJitter,
                yJitter,
                MathF.Sin(angle) * rJitter);

            rock.Name = "AsteroidRock_" + i;
            beltPivot.AddChild(rock);
        }

        _localSystemRoot.AddChild(beltPivot);
    }

    // Spawn planet with type-matched scene, luminosity-scaled orbit, self-rotation.
    // Returns (planetPos, planetType) so station + moons can reference it.
    private (Vector3, string, Node3D) SpawnLocalPlanetV0(string nodeId, float lumScale)
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
                TintPlanetAtmosphereV0(planetNode, planetType, nodeId); // VISUAL_OVERHAUL
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

        // Orbital motion: pivot at star center rotates slowly, planet child orbits.
        var orbitPivot = new Node3D { Name = "PlanetOrbitPivot" };
        var orbitSpin = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (orbitSpin != null)
        {
            orbitPivot.SetScript(orbitSpin);
            float orbitSpeed = planetType switch
            {
                "Gaseous" => 0.06f,
                "Ice"     => 0.08f,
                _         => 0.12f,
            };
            orbitPivot.Set("spin_speed_y", orbitSpeed);
        }

        var container = new Node3D { Name = "LocalPlanet" };
        container.Scale = new Vector3(vScale, vScale, vScale);
        var planetOrbitPos = DeriveOrbitPositionV0(nodeId + "_planet", orbitRadius);
        container.Position = planetOrbitPos;
        container.AddChild(planetNode);

        // FEEL_POST_BASELINE: Atmospheric glow halo around planets.
        // Slightly larger additive sphere in the planet's atmosphere color.
        var atmosColor = planetType switch
        {
            "Terrestrial" => new Color(0.3f, 0.5f, 0.9f, 0.08f),  // Blue haze
            "Sand"        => new Color(0.8f, 0.6f, 0.3f, 0.06f),  // Amber haze
            "Lava"        => new Color(0.9f, 0.3f, 0.1f, 0.10f),  // Orange glow
            "Ice"         => new Color(0.5f, 0.7f, 1.0f, 0.07f),  // Pale blue
            "Gaseous"     => new Color(0.4f, 0.8f, 0.6f, 0.08f),  // Teal mist
            _             => new Color(0.4f, 0.4f, 0.5f, 0.05f),  // Neutral
        };
        var atmosEmission = new Color(atmosColor.R, atmosColor.G, atmosColor.B);
        var atmosGlow = new MeshInstance3D
        {
            Name = "AtmosphereGlow",
            Mesh = new SphereMesh { Radius = 5.5f, Height = 11.0f },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = atmosColor,
                EmissionEnabled = true,
                Emission = atmosEmission,
                EmissionEnergyMultiplier = 2.0f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                CullMode = BaseMaterial3D.CullModeEnum.Back,
                DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
                NoDepthTest = true,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        container.AddChild(atmosGlow);

        orbitPivot.AddChild(container);

        if (landable)
        {
            // Add dockable Area3D around the planet (same pattern as station).
            // Dock area orbits with the planet inside the pivot.
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
                // GATE.S14.DOCK.PROXIMITY_TIGHTEN.001: Planet dock area.
                Shape = new SphereShape3D { Radius = 6.0f }
            };
            dockArea.AddChild(collider);

            dockArea.AddToGroup("Planet");
            RegisterDockTargetV0(dockArea, "PLANET", nodeId);

            // Dock confirmation: show prompt on proximity, dock on E key.
            dockArea.BodyEntered += (body) =>
            {
                var gm = GetNode<Node>("/root/GameManager");
                if (gm != null && gm.HasMethod("on_dock_proximity_v0"))
                    gm.Call("on_dock_proximity_v0", dockArea);
            };
            dockArea.BodyExited += (body) =>
            {
                var gm = GetNode<Node>("/root/GameManager");
                if (gm != null && gm.HasMethod("on_dock_proximity_exit_v0"))
                    gm.Call("on_dock_proximity_exit_v0", dockArea);
            };

            // Attach dock area inside orbit pivot so it moves with the planet.
            dockArea.Position = planetOrbitPos;

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
                    Modulate = new Color(0.7f, 0.85f, 1.0f),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                dockArea.AddChild(label);
            }

            orbitPivot.AddChild(dockArea);
        }

        _localSystemRoot.AddChild(orbitPivot);
        return (planetOrbitPos, planetType, orbitPivot);
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
        root.SetMeta("neighbor_node_id", neighborId);

        // GATE.S14.GATE_VISUAL.KENNEY_MODEL.001: Use Kenney Space Kit gate model.
        const string GateScenePath = "res://addons/kenney_space_kit/Models/GLTF format/gate_complex.glb";
        bool gateModelLoaded = false;
        if (Godot.FileAccess.FileExists(GateScenePath))
        {
            var gateScene = GD.Load<PackedScene>(GateScenePath);
            if (gateScene != null)
            {
                var gateModel = gateScene.Instantiate<Node3D>();
                gateModel.Name = "GateModel";
                // Scale Kenney model to match ~3u gate footprint.
                float s = 3.0f;
                gateModel.Scale = new Vector3(s, s, s);
                root.AddChild(gateModel);
                gateModelLoaded = true;
            }
        }
        if (!gateModelLoaded)
        {
            // Fallback: emissive torus if Kenney asset missing or import cache stale.
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
                Mesh = new TorusMesh { InnerRadius = LaneGateMarkerRadiusU * 0.8f, OuterRadius = LaneGateMarkerRadiusU * 1.2f },
                MaterialOverride = orbMat
            };
            root.AddChild(orb);
        }

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
            // FEEL_BASELINE: Strip resource-type tags from gate labels — player only needs system name.
            Text = "\u2192 " + StripResourceTagsV0(string.IsNullOrEmpty(displayName) ? neighborId : displayName),
            PixelSize = 0.02f,
            FontSize = 32,
            OutlineSize = 8,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        lbl.Position = new Vector3(0f, LaneGateMarkerRadiusU + 1.0f, 0f);
        root.AddChild(lbl);

        // Approach zone: player RigidBody3D entering triggers GATE_APPROACH state + popup.
        // Exiting the zone cancels approach if still pending.
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
            Shape = new SphereShape3D { Radius = 10.0f } // Pace overhaul: larger trigger (was 8u) to compensate for 1.6x spread
        };
        area.AddChild(shape);
        area.SetMeta("lane_neighbor_id", neighborId);
        area.BodyEntered += (body) => _OnLaneGateApproachEnteredV0(body, neighborId);
        area.BodyExited += (body) => _OnLaneGateApproachExitedV0(body);
        root.AddChild(area);

        return root;
    }

    private void _OnLaneGateApproachEnteredV0(Node3D body, string neighborId)
    {
        if (!body.IsInGroup("Player")) return;
        // MUST target the autoload GameManager — it owns _unhandled_input.
        // Scene-child (/root/Main/GameManager) does not receive input events.
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm == null) return;
        if (gm.HasMethod("on_lane_gate_approach_entered_v0"))
            gm.Call("on_lane_gate_approach_entered_v0", neighborId);
    }

    private void _OnLaneGateApproachExitedV0(Node3D body)
    {
        if (!body.IsInGroup("Player")) return;
        var gm = GetNodeOrNull<Node>("/root/GameManager");
        if (gm == null) return;
        if (gm.HasMethod("on_lane_gate_approach_exited_v0"))
            gm.Call("on_lane_gate_approach_exited_v0");
    }

    private Node3D CreateDiscoverySiteMarkerV0(string siteId)
    {
        var root = new Node3D { Name = "DiscoverySite_" + siteId };

        // VISUAL_OVERHAUL: Emissive sphere + spinning ring + particle motes.
        var mesh = new MeshInstance3D
        {
            Name = "SiteMesh",
            Mesh = new SphereMesh { Radius = DiscoverySiteMarkerRadiusU * 1.5f },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1.0f, 0.7f, 0.1f),
                EmissionEnabled = true,
                Emission = new Color(1.0f, 0.6f, 0.0f),
                EmissionEnergyMultiplier = 4.0f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            }
        };
        root.AddChild(mesh);

        // Spinning scan ring.
        var ringMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.8f, 0.2f, 0.4f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.7f, 0.1f),
            EmissionEnergyMultiplier = 2.0f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        var ring = new MeshInstance3D
        {
            Name = "SiteRing",
            Mesh = new TorusMesh
            {
                InnerRadius = DiscoverySiteMarkerRadiusU * 2.5f,
                OuterRadius = DiscoverySiteMarkerRadiusU * 3.0f,
            },
            MaterialOverride = ringMat,
        };
        ring.RotateX(Mathf.Pi / 2.0f);
        var ringSpinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (ringSpinScript != null)
        {
            ring.SetScript(ringSpinScript);
            ring.Set("spin_speed_y", 0.5f);
        }
        root.AddChild(ring);

        // Mystery energy particle motes.
        var siteParticleProc = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 2.0f,
            Gravity = Vector3.Zero,
            InitialVelocityMin = 0.2f,
            InitialVelocityMax = 0.8f,
            Color = new Color(1.0f, 0.8f, 0.2f, 0.6f),
            ScaleMin = 0.05f,
            ScaleMax = 0.15f,
        };
        var siteParticles = new GpuParticles3D
        {
            Name = "SiteParticles",
            Amount = 12,
            Lifetime = 3.0f,
            SpeedScale = 0.3f,
            ProcessMaterial = siteParticleProc,
            DrawPass1 = new SphereMesh { Radius = 0.04f, Height = 0.08f },
        };
        root.AddChild(siteParticles);

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
        var gm = GetNodeOrNull<Node>("/root/Main/GameManager")
            ?? GetNodeOrNull<Node>("/root/GameManager");
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

    // GATE.S13.WORLD.GATE_ARRIVAL.001: Get gate position for a neighbor node (for arrival positioning).
    public Vector3 GetGatePositionV0(string neighborId)
    {
        // Primary: use pre-computed cache (always available, no scene-tree dependency).
        if (!string.IsNullOrEmpty(_currentNodeId) && !string.IsNullOrEmpty(neighborId))
        {
            var cached = GetCachedGateGlobalPositionV0(_currentNodeId, neighborId);
            if (cached != Vector3.Zero)
                return cached;
        }

        // Fallback: scene tree search (legacy).
        if (_localSystemRoot == null || string.IsNullOrEmpty(neighborId)) return Vector3.Zero;
        var rootPos = _localSystemRoot.IsInsideTree()
            ? _localSystemRoot.GlobalPosition
            : _localSystemRoot.Position;
        foreach (var child in _localSystemRoot.GetChildren())
        {
            if (child is not Node3D n3d) continue;
            if (!n3d.IsInGroup("LaneGate")) continue;
            if (n3d.HasMeta("neighbor_node_id") && (string)n3d.GetMeta("neighbor_node_id") == neighborId)
                return rootPos + n3d.Position;
        }
        return Vector3.Zero;
    }

    // Evenly-spaced XZ positions for lane gate markers (deterministic by index+total).
    private static Vector3 DeriveLaneGatePositionV0(int index, int total, float distance)
    {
        float angle = total > 0 ? ((float)index / total) * 2f * MathF.PI : 0f;
        return new Vector3(MathF.Cos(angle) * distance, 0f, MathF.Sin(angle) * distance);
    }

    // Pre-compute gate positions for ALL systems from galaxy data.
    // Uses the same direction+nudging algorithm as SpawnLaneGatesV0 but runs upfront
    // so gate positions are known before any local system is drawn.
    // Key: "nodeId|neighborId" → local position relative to star center.
    private void PrecomputeAllGatePositionsV0()
    {
        _gateLocalPositionCache.Clear();
        if (_bridge == null) return;
        var galSnap = _bridge.GetGalaxySnapshotV0();
        if (galSnap == null) return;

        // Build node positions (unscaled, for direction computation only).
        var rawNodes = galSnap.ContainsKey("system_nodes")
            ? galSnap["system_nodes"].AsGodotArray()
            : new Godot.Collections.Array();
        var nodePositions = new Dictionary<string, Vector3>();
        for (int i = 0; i < rawNodes.Count; i++)
        {
            var nd = rawNodes[i].AsGodotDictionary();
            var nid = nd.ContainsKey("node_id") ? (string)(Variant)nd["node_id"] : "";
            float nx = nd.ContainsKey("pos_x") ? (float)(Variant)nd["pos_x"] : 0f;
            float nz = nd.ContainsKey("pos_z") ? (float)(Variant)nd["pos_z"] : 0f;
            if (!string.IsNullOrEmpty(nid))
                nodePositions[nid] = new Vector3(nx, 0f, nz);
        }

        // Build per-node neighbor lists from edges (sorted for deterministic nudging order).
        var rawEdges = galSnap.ContainsKey("lane_edges")
            ? galSnap["lane_edges"].AsGodotArray()
            : new Godot.Collections.Array();
        var neighborsByNode = new Dictionary<string, List<string>>();
        for (int i = 0; i < rawEdges.Count; i++)
        {
            var e = rawEdges[i].AsGodotDictionary();
            var fromId = e.ContainsKey("from_id") ? (string)(Variant)e["from_id"] : "";
            var toId = e.ContainsKey("to_id") ? (string)(Variant)e["to_id"] : "";
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId)) continue;
            if (!neighborsByNode.ContainsKey(fromId))
                neighborsByNode[fromId] = new List<string>();
            if (!neighborsByNode[fromId].Contains(toId))
                neighborsByNode[fromId].Add(toId);
            if (!neighborsByNode.ContainsKey(toId))
                neighborsByNode[toId] = new List<string>();
            if (!neighborsByNode[toId].Contains(fromId))
                neighborsByNode[toId].Add(fromId);
        }

        const float MinGateSeparationU = 20.0f;

        // For each node, compute gate positions for all neighbors.
        foreach (var kv in neighborsByNode)
        {
            var nodeId = kv.Key;
            var neighbors = kv.Value;
            neighbors.Sort(StringComparer.Ordinal); // Deterministic order for nudging.

            if (!nodePositions.TryGetValue(nodeId, out var currentGalPos))
                continue;

            var placedGatePositions = new List<Vector3>();

            for (int i = 0; i < neighbors.Count; i++)
            {
                var neighborId = neighbors[i];
                Vector3 gatePos;
                Vector3 dir2d;

                if (nodePositions.TryGetValue(neighborId, out var neighborGalPos) && currentGalPos != neighborGalPos)
                {
                    dir2d = (neighborGalPos - currentGalPos).Normalized();
                    gatePos = dir2d * LaneGateDistanceU;
                }
                else
                {
                    gatePos = DeriveLaneGatePositionV0(i, neighbors.Count, LaneGateDistanceU);
                    dir2d = gatePos.Normalized();
                }

                // Enforce minimum separation: nudge if too close to already-placed gates.
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    bool tooClose = false;
                    for (int j = 0; j < placedGatePositions.Count; j++)
                    {
                        if (gatePos.DistanceTo(placedGatePositions[j]) < MinGateSeparationU)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose) break;
                    float nudgeAngle = (attempt + 1) * 15.0f * MathF.PI / 180.0f;
                    gatePos = new Vector3(
                        dir2d.X * MathF.Cos(nudgeAngle) - dir2d.Z * MathF.Sin(nudgeAngle),
                        0f,
                        dir2d.X * MathF.Sin(nudgeAngle) + dir2d.Z * MathF.Cos(nudgeAngle)
                    ) * LaneGateDistanceU;
                }

                placedGatePositions.Add(gatePos);
                _gateLocalPositionCache[nodeId + "|" + neighborId] = gatePos;
            }
        }
    }

    // Get a gate's world-space position from the pre-computed cache.
    // Returns the galactic-scaled star position + local gate offset.
    // Callable from GDScript for transit camera targeting.
    public Vector3 GetCachedGateGlobalPositionV0(string nodeId, string neighborId)
    {
        var key = nodeId + "|" + neighborId;
        if (_gateLocalPositionCache.TryGetValue(key, out var localPos))
            return GetNodeScaledPositionV0(nodeId) + localPos;
        // Fallback: direction-based estimate (no nudging).
        // Note: a star CAN be at (0,0,0) — only reject if both positions are identical.
        var starPos = GetNodeScaledPositionV0(nodeId);
        var neighborPos = GetNodeScaledPositionV0(neighborId);
        if (starPos != neighborPos)
        {
            var dir = (neighborPos - starPos).Normalized();
            return starPos + dir * LaneGateDistanceU;
        }
        // Stars overlap — generate a deterministic offset so gates aren't on top of each other.
        int hash = (nodeId + neighborId).GetHashCode();
        float angle = (hash & 0x7FFFFFFF) * 0.001f;
        var synthDir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        return starPos + synthDir * LaneGateDistanceU;
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
            // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Scale to galactic coordinates.
            float galScale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;
            float x = (n.ContainsKey("pos_x") ? (float)(Variant)n["pos_x"] : 0f) * galScale;
            float y = (n.ContainsKey("pos_y") ? (float)(Variant)n["pos_y"] : 0f) * galScale;
            float z = (n.ContainsKey("pos_z") ? (float)(Variant)n["pos_z"] : 0f) * galScale;

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
        // GATE.S17.REAL_SPACE.GALAXY_MAP.001: playerNodePos/playerNodePosFound removed (camera positioning now in GDScript).
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
                // GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001: Truncate long resource-type lists.
                // GATE.S7.INSTABILITY_EFFECTS.BRIDGE.001: Append instability phase to node label.
                string baseText = StringComparer.Ordinal.Equals(n.DisplayStateToken, "RUMORED")
                    ? "???"
                    : TruncateResourceTypesV0(n.DisplayText ?? "");
                if (_bridge != null && !StringComparer.Ordinal.Equals(n.DisplayStateToken, "RUMORED"))
                {
                    var instab = _bridge.GetNodeInstabilityV0(n.NodeId);
                    int phaseIdx = instab.ContainsKey("phase_index") ? (int)(Variant)instab["phase_index"] : 0;
                    if (phaseIdx >= 1) // Shimmer+
                    {
                        string phase = instab.ContainsKey("phase") ? (string)(Variant)instab["phase"] : "";
                        baseText += "\n[" + phase + "]";
                    }
                }
                label.Text = baseText;
                // Suppress all overlay labels during transit/cinematic.
                if (_localLabelsHidden)
                    label.Visible = false;
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
                // Suppress all overlay labels during transit/cinematic.
                if (_localLabelsHidden)
                    fleetLabel.Visible = false;
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
                    mat.EmissionEnergyMultiplier = 4.0f;

                    // GATE.S14.MAP.PLAYER_INDICATOR.001: "YOU" label + pulsing ring
                    if (root.GetNodeOrNull("YouLabel") == null)
                    {
                        var youLabel = new Label3D
                        {
                            Name = "YouLabel",
                            Text = "YOU",
                            PixelSize = 1.5f,
                            FontSize = 64,
                            OutlineSize = 12,
                            Modulate = new Color(0.2f, 1.0f, 0.4f),
                            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                            Position = new Vector3(0, 60f, 0),
                            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                        };
                        root.AddChild(youLabel);

                        var ringMat = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(0.2f, 1.0f, 0.4f, 0.6f),
                            EmissionEnabled = true,
                            Emission = new Color(0.2f, 1.0f, 0.4f),
                            EmissionEnergyMultiplier = 5.0f,
                            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        };
                        var ringMesh = new TorusMesh { InnerRadius = 30.0f, OuterRadius = 38.0f };
                        var ringInst = new MeshInstance3D
                        {
                            Name = "PlayerRing",
                            Mesh = ringMesh,
                            MaterialOverride = ringMat,
                            Rotation = new Vector3(Mathf.Pi / 2f, 0, 0),
                        };
                        root.AddChild(ringInst);

                        // Sensor range ring: faint circle showing detection radius.
                        if (SensorRangeGalacticU > 0)
                        {
                            var sensorMat = new StandardMaterial3D
                            {
                                AlbedoColor = new Color(0.3f, 0.6f, 1.0f, 0.15f),
                                EmissionEnabled = true,
                                Emission = new Color(0.3f, 0.6f, 1.0f),
                                EmissionEnergyMultiplier = 1.5f,
                                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                            };
                            float sensorR = SensorRangeGalacticU;
                            var sensorMesh = new TorusMesh { InnerRadius = sensorR - 3.0f, OuterRadius = sensorR + 3.0f };
                            var sensorInst = new MeshInstance3D
                            {
                                Name = "SensorRing",
                                Mesh = sensorMesh,
                                MaterialOverride = sensorMat,
                                Rotation = new Vector3(Mathf.Pi / 2f, 0, 0),
                                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                            };
                            root.AddChild(sensorInst);
                        }
                    }
                }
                else
                {
                    // FEEL_POST_BASELINE: Dim RUMORED nodes so they appear as unknown destinations.
                    bool isRumored = StringComparer.Ordinal.Equals(n.DisplayStateToken, "RUMORED");

                    // GATE.S6.MAP_GALAXY.INTEL_OVERLAY.001: Tint non-player nodes by intel freshness.
                    Color nodeColor = isRumored
                        ? new Color(0.4f, 0.4f, 0.5f) // Gray-blue for unknown systems
                        : _currentOverlayMode == GalaxyOverlayMode.IntelFreshness
                            ? GetIntelFreshnessNodeColor(n.NodeId)
                            : new Color(0f, 0.6f, 1.0f);

                    // GATE.S7.WARFRONT.UI_MAP.001: Tint contested war-zone nodes.
                    if (_bridge != null)
                    {
                        int warIntensity = _bridge.GetNodeWarIntensityV0(n.NodeId);
                        if (warIntensity >= 3) // Open War / Total War
                            nodeColor = new Color(1.0f, 0.2f, 0.1f); // hot red
                        else if (warIntensity >= 2) // Skirmish
                            nodeColor = new Color(1.0f, 0.5f, 0.1f); // orange
                        else if (warIntensity >= 1) // Tension
                            nodeColor = nodeColor.Lerp(new Color(1.0f, 0.85f, 0.2f), 0.3f); // slight yellow tint
                    }

                    // GATE.S7.INSTABILITY.VISUAL.001: Tint high-instability nodes.
                    if (_bridge != null)
                    {
                        var instab = _bridge.GetNodeInstabilityV0(n.NodeId);
                        int phaseIdx = instab.ContainsKey("phase_index") ? (int)(Variant)instab["phase_index"] : 0;
                        if (phaseIdx >= 4) // Void
                            nodeColor = new Color(0.6f, 0.0f, 0.8f); // deep purple
                        else if (phaseIdx >= 3) // Fracture
                            nodeColor = nodeColor.Lerp(new Color(0.7f, 0.1f, 0.9f), 0.5f); // purple tint
                        else if (phaseIdx >= 2) // Drift
                            nodeColor = nodeColor.Lerp(new Color(0.5f, 0.3f, 0.8f), 0.3f); // faint purple
                    }

                    mat.AlbedoColor = nodeColor;
                    mat.EmissionEnabled = true;
                    mat.Emission = nodeColor;
                    // FEEL_POST_BASELINE: RUMORED nodes glow dimmer than explored nodes.
                    mat.EmissionEnergyMultiplier = isRumored ? 1.0f : 2.5f;
                }
            }
        }

        _lastNodeCount = renderedNodeCount;
        _lastPlayerHighlighted = playerHighlighted;

        // GATE.S17.REAL_SPACE.GALAXY_MAP.001: Camera positioning removed — player_follow_camera.gd
        // handles altitude and centering when in GALAXY_MAP mode.

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

            // Fog of war: only show lanes where at least one endpoint has been visited.
            if (_bridge != null)
            {
                bool fromVisited = !_bridge.IsFirstVisitV0(e.FromId);
                bool toVisited = !_bridge.IsFirstVisitV0(e.ToId);
                if (!fromVisited && !toVisited) continue;
            }

            string key = e.FromId + "->" + e.ToId;
            var edgeColor = GetEdgeColorForOverlay(e.FromId, e.ToId);

            if (!_edgeMeshesByKey.TryGetValue(key, out var mesh))
            {
                var mat = new StandardMaterial3D
                {
                    AlbedoColor = edgeColor,
                    EmissionEnabled = true,
                    Emission = edgeColor,
                    EmissionEnergyMultiplier = 1.2f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
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

        // GATE.S15.FEEL.FACTION_LABELS.001: Faction territory labels on galaxy map.
        UpdateFactionLabelsV0();

        // GATE.S7.FACTION.TERRITORY_OVERLAY.001: Territory fill discs at claimed nodes.
        UpdateTerritoryOverlayV0();

        // GATE.S5.LOOT.BRIDGE_PROOF.001: Loot drop markers.
        UpdateLootMarkersV0();
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

        // Emissive beacon sphere — primary visual for galaxy map at altitude ~1800u.
        // Must be large enough to see and unshaded (no light sources at this altitude).
        var beacon = new MeshInstance3D();
        beacon.Name = "NodeBeacon";
        beacon.Mesh = new SphereMesh { Radius = 60.0f, Height = 120.0f };
        beacon.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.6f, 0.9f, 1.0f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.6f, 0.9f, 1.0f),
            EmissionEnergyMultiplier = 20.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        beacon.Name = "NodeMesh"; // Keep name for player highlight lookups.
        beacon.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        root.AddChild(beacon);

        // GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001: Width clamped to prevent overlap between adjacent nodes.
        var lbl = new Label3D
        {
            Name = "NodeLabel",
            Text = "",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 1.2f,
            FontSize = 48,
            OutlineSize = 10,
            Modulate = new Color(0.85f, 0.85f, 0.9f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Width = 200f,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        lbl.Position = new Vector3(0, 40.0f, 0);
        root.AddChild(lbl);

        // GATE.S1.GALAXY_MAP.FLEET_COUNTS.001: fleet count overlay label (hidden when zero).
        var fleetLbl = new Label3D
        {
            Name = "FleetLabel",
            Text = "",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.9f,
            FontSize = 36,
            OutlineSize = 8,
            Modulate = new Color(1.0f, 0.8f, 0.2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        fleetLbl.Position = new Vector3(0, 55.0f, 0);
        root.AddChild(fleetLbl);

        return root;
    }

    private static MeshInstance3D CreateEdgeMeshV0(Material mat)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = "GalaxyEdge";

        // Cylinder oriented along +Y then rotated into place.
        // Radius 8.0 visible at strategic altitude (~1800u).
        var cyl = new CylinderMesh
        {
            TopRadius = 8.0f,
            BottomRadius = 8.0f,
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
        else
            _currentOverlayMode = GalaxyOverlayMode.None;
    }

    public int GetOverlayModeV0() => (int)_currentOverlayMode;

    // GATE.S14.MAP.PLAYER_INDICATOR.001: Pulse the player ring scale via sine wave.
    private void _PulsePlayerRingV0(double delta)
    {
        _playerRingPulseTime += delta;
        foreach (var kv in _nodeRootsById)
        {
            var ring = kv.Value.GetNodeOrNull<MeshInstance3D>("PlayerRing");
            if (ring == null) continue;
            float s = 1.0f + 0.3f * Mathf.Sin((float)_playerRingPulseTime * 3.0f);
            ring.Scale = new Vector3(s, s, s);
            return; // Only one player ring
        }
    }

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
                        Modulate = new Color(1.0f, 0.95f, 0.7f),
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
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

    // GATE.S7.FACTION.TERRITORY_OVERLAY.001: Draw/update semi-transparent territory fill discs
    // at each claimed node. Color = faction primary color at low alpha.
    private void UpdateTerritoryOverlayV0()
    {
        if (_bridge == null || !_bridge.HasMethod("GetNodeFactionMapV0")) return;

        var mapping = _bridge.Call("GetNodeFactionMapV0").AsGodotArray();
        if (mapping == null || mapping.Count == 0)
        {
            ClearTerritoryOverlayV0();
            return;
        }

        // Cache faction colors: faction_id → primary color.
        var factionColors = new Dictionary<string, Color>(StringComparer.Ordinal);

        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < mapping.Count; i++)
        {
            var entry = mapping[i].AsGodotDictionary();
            if (entry == null) continue;

            var nodeId = entry.ContainsKey("node_id") ? (string)(Variant)entry["node_id"] : "";
            var factionId = entry.ContainsKey("faction_id") ? (string)(Variant)entry["faction_id"] : "";
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(factionId)) continue;

            seenNodes.Add(nodeId);

            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            // Resolve faction color (cached per faction).
            if (!factionColors.TryGetValue(factionId, out var baseColor))
            {
                baseColor = new Color(0.5f, 0.5f, 0.8f); // fallback blue-grey
                if (_bridge.HasMethod("GetFactionColorsV0"))
                {
                    var colors = _bridge.Call("GetFactionColorsV0", factionId).AsGodotDictionary();
                    if (colors != null && colors.ContainsKey("primary"))
                    {
                        baseColor = (Color)colors["primary"];
                    }
                }
                factionColors[factionId] = baseColor;
            }

            if (!_territoryDiscsByNodeId.TryGetValue(nodeId, out var disc))
            {
                disc = new MeshInstance3D
                {
                    Name = "TerritoryDisc_" + nodeId,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                var mesh = new PlaneMesh { Size = new Vector2(12f, 12f) };
                disc.Mesh = mesh;
                _territoryDiscsByNodeId[nodeId] = disc;
                AddChild(disc);
            }

            // Position slightly below the node to avoid Z-fighting with other elements.
            disc.GlobalPosition = nodeRoot.GlobalPosition + new Vector3(0f, -0.5f, 0f);

            // Apply faction color with transparency.
            var mat = new StandardMaterial3D
            {
                AlbedoColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.18f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            };
            disc.MaterialOverride = mat;
        }

        // Remove discs for nodes no longer claimed.
        var toRemove = new List<string>();
        foreach (var kv in _territoryDiscsByNodeId)
        {
            if (!seenNodes.Contains(kv.Key))
            {
                kv.Value.QueueFree();
                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove)
            _territoryDiscsByNodeId.Remove(key);
    }

    // GATE.S5.LOOT.BRIDGE_PROOF.001: Show glowing sphere markers for loot drops at current node.
    private void UpdateLootMarkersV0()
    {
        if (_bridge == null || !_bridge.HasMethod("GetNearbyLootV0"))
        {
            ClearLootMarkersV0();
            return;
        }

        var drops = _bridge.Call("GetNearbyLootV0").AsGodotArray();
        if (drops == null || drops.Count == 0)
        {
            ClearLootMarkersV0();
            return;
        }

        // Find current node position for placing loot markers.
        string currentNodeId = _currentLocalNodeId ?? "";
        if (string.IsNullOrEmpty(currentNodeId) || !_nodeRootsById.TryGetValue(currentNodeId, out var nodeRoot))
        {
            ClearLootMarkersV0();
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        int idx = 0;

        for (int i = 0; i < drops.Count; i++)
        {
            var entry = drops[i].AsGodotDictionary();
            if (entry == null) continue;
            var dropId = entry.ContainsKey("drop_id") ? (string)(Variant)entry["drop_id"] : "";
            if (string.IsNullOrEmpty(dropId)) continue;

            seenIds.Add(dropId);
            var rarity = entry.ContainsKey("rarity") ? (string)(Variant)entry["rarity"] : "Common";

            if (!_lootMarkersByDropId.TryGetValue(dropId, out var marker))
            {
                marker = new MeshInstance3D
                {
                    Name = "LootMarker_" + dropId,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                marker.Mesh = new SphereMesh { Radius = 0.8f, Height = 1.6f };
                _lootMarkersByDropId[dropId] = marker;
                AddChild(marker);
            }

            // Spread markers in a ring around the node.
            float angle = idx * Mathf.Tau / drops.Count;
            float spread = 4f;
            var offset = new Vector3(Mathf.Cos(angle) * spread, 2f, Mathf.Sin(angle) * spread);
            marker.GlobalPosition = nodeRoot.GlobalPosition + offset;

            // Rarity color.
            Color lootColor = rarity switch
            {
                "Uncommon" => new Color(0.2f, 0.8f, 1.0f),
                "Rare" => new Color(0.9f, 0.2f, 1.0f),
                _ => new Color(1.0f, 0.9f, 0.3f), // Common = gold
            };
            marker.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = lootColor,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                EmissionEnabled = true,
                Emission = lootColor,
            };
            idx++;
        }

        // Remove markers for collected/despawned drops.
        var toRemove = new List<string>();
        foreach (var kv in _lootMarkersByDropId)
        {
            if (!seenIds.Contains(kv.Key))
            {
                kv.Value.QueueFree();
                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove)
            _lootMarkersByDropId.Remove(key);
    }

    private void ClearLootMarkersV0()
    {
        foreach (var kv in _lootMarkersByDropId)
            kv.Value.QueueFree();
        _lootMarkersByDropId.Clear();
    }

    private void ClearTerritoryOverlayV0()
    {
        foreach (var kv in _territoryDiscsByNodeId)
            kv.Value.QueueFree();
        _territoryDiscsByNodeId.Clear();
    }

    // GATE.S15.FEEL.FACTION_LABELS.001: Draw/update faction territory Label3Ds on the galaxy map.
    // Called each RefreshFromSnapshotV0. Labels are placed at the home node position + Y offset.
    private void UpdateFactionLabelsV0()
    {
        if (_bridge == null || !_bridge.HasMethod("GetFactionMapV0")) return;

        var factions = _bridge.Call("GetFactionMapV0").AsGodotArray();
        if (factions == null || factions.Count == 0) return;

        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < factions.Count; i++)
        {
            var f = factions[i].AsGodotDictionary();
            if (f == null || f.Count == 0) continue;

            var factionId = f.ContainsKey("faction_id") ? (string)(Variant)f["faction_id"] : "";
            var roleTag   = f.ContainsKey("role_tag")   ? (string)(Variant)f["role_tag"]   : "";
            var homeNodeId = f.ContainsKey("home_node_id") ? (string)(Variant)f["home_node_id"] : "";
            if (string.IsNullOrEmpty(factionId) || string.IsNullOrEmpty(homeNodeId)) continue;

            seenIds.Add(factionId);

            // Find home node position from the rendered node visuals.
            if (!_nodeRootsById.TryGetValue(homeNodeId, out var homeRoot)) continue;

            // Color-code by role tag.
            Color labelColor = roleTag switch
            {
                "Trader" => new Color(0.2f, 1.0f, 0.4f),  // green
                "Miner"  => new Color(1.0f, 0.6f, 0.1f),  // orange
                "Pirate" => new Color(1.0f, 0.2f, 0.2f),  // red
                _        => new Color(0.9f, 0.9f, 0.9f),  // white fallback
            };

            if (!_factionLabelsByFactionId.TryGetValue(factionId, out var lbl))
            {
                lbl = new Label3D
                {
                    Name = "FactionLabel_" + factionId,
                    PixelSize = 0.02f,
                    FontSize = 32,
                    OutlineSize = 8,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                _factionLabelsByFactionId[factionId] = lbl;
                AddChild(lbl);
            }

            lbl.Text = roleTag;
            lbl.Modulate = labelColor;
            // Place 16u above the home node (above the NodeLabel at 8u, above the FleetLabel at 11u).
            lbl.GlobalPosition = homeRoot.GlobalPosition + new Vector3(0f, 16f, 0f);
        }

        // Remove labels whose factions are no longer reported.
        var toRemove = new List<string>();
        foreach (var kv in _factionLabelsByFactionId)
        {
            if (!seenIds.Contains(kv.Key))
            {
                kv.Value.QueueFree();
                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove)
            _factionLabelsByFactionId.Remove(key);
    }

    // FEEL_BASELINE: Strip all parenthesized resource tags from a display name.
    // "System 10 (RareMin)(Mining)(Munitions)" => "System 10"
    private static string StripResourceTagsV0(string displayText)
    {
        if (string.IsNullOrEmpty(displayText)) return displayText;
        int firstOpen = displayText.IndexOf('(');
        if (firstOpen < 0) return displayText;
        return displayText.Substring(0, firstOpen).TrimEnd();
    }

    // GATE.S7.GALAXY_MAP_V2.LABEL_FIX.001: Truncate long resource-type lists in node/station names.
    // If more than 1 parenthesized resource tag, show first 1 + "...".
    // E.g., "System 10 (RareMin)(Mining)(Munitions)" => "System 10 (RareMin)..."
    private static string TruncateResourceTypesV0(string displayText)
    {
        if (string.IsNullOrEmpty(displayText)) return displayText;

        int firstOpen = displayText.IndexOf('(');
        if (firstOpen < 0) return displayText;

        int firstClose = displayText.IndexOf(')', firstOpen);
        if (firstClose < 0) return displayText;

        // Check if there's a second tag after the first.
        int secondOpen = displayText.IndexOf('(', firstClose + 1);
        if (secondOpen >= 0)
        {
            // Keep only the first tag, append "..."
            return displayText.Substring(0, firstClose + 1) + "...";
        }

        return displayText;
    }

    // ════════════════════════════════════════════════════════════════════════
    // GATE.S7.GALAXY_MAP_V2.OVERLAYS.001: V2 overlay modes (Faction / Fleet / Heat)
    // ════════════════════════════════════════════════════════════════════════

    /// Toggle a V2 overlay mode; pressing the same hotkey again turns it off.
    private void ToggleV2OverlayV0(GalaxyMapV2Overlay mode)
    {
        if (_v2OverlayMode == mode)
        {
            _v2OverlayMode = GalaxyMapV2Overlay.Off;
            ClearV2OverlayV0();
        }
        else
        {
            _v2OverlayMode = mode;
            // Immediate refresh on mode switch.
            UpdateV2OverlayVisualsV0();
        }
    }

    /// Public API for external callers (e.g., toolbar buttons).
    public void SetV2OverlayModeV0(int mode)
    {
        if (mode >= 0 && mode <= 5)
            _v2OverlayMode = (GalaxyMapV2Overlay)mode;
        else
            _v2OverlayMode = GalaxyMapV2Overlay.Off;
    }

    public int GetV2OverlayModeV0() => (int)_v2OverlayMode;

    /// Refresh V2 overlay visuals: colored discs/indicators per system node.
    private void UpdateV2OverlayVisualsV0()
    {
        if (_bridge == null) return;

        switch (_v2OverlayMode)
        {
            case GalaxyMapV2Overlay.Faction:
                UpdateFactionOverlayDiscsV0();
                break;
            case GalaxyMapV2Overlay.Fleet:
                UpdateFleetOverlayDiscsV0();
                break;
            case GalaxyMapV2Overlay.Heat:
                UpdateHeatOverlayDiscsV0();
                break;
            case GalaxyMapV2Overlay.Exploration:
                UpdateExplorationOverlayDiscsV0();
                break;
            case GalaxyMapV2Overlay.Warfront:
                UpdateWarfrontOverlayDiscsV0();
                break;
            default:
                ClearV2OverlayV0();
                break;
        }
    }

    // Faction colors by faction name (deterministic palette).
    private static Color FactionOverlayColorV0(string factionId)
    {
        if (string.IsNullOrEmpty(factionId)) return new Color(0.5f, 0.5f, 0.5f, 0.3f);
        // Simple hash-to-hue for consistent faction coloring.
        uint hash = 0;
        foreach (char c in factionId) { hash = hash * 31 + (uint)c; }
        float hue = (hash % 360) / 360.0f;
        return Color.FromHsv(hue, 0.7f, 0.9f, 0.35f);
    }

    private void UpdateFactionOverlayDiscsV0()
    {
        var data = _bridge.GetFactionTerritoryOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            seenNodes.Add(nodeId);

            var info = data[key].AsGodotDictionary();
            var factionId = info.ContainsKey("controlling_faction") ? info["controlling_faction"].AsString() : "";
            float influence = info.ContainsKey("influence_pct") ? (float)info["influence_pct"] : 0.5f;

            var color = FactionOverlayColorV0(factionId);
            color.A = Mathf.Clamp(influence * 0.5f, 0.1f, 0.5f);

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, 60.0f);
        }

        // Remove discs for nodes not in the current data set.
        PruneV2OverlayDiscs(seenNodes);
    }

    private void UpdateFleetOverlayDiscsV0()
    {
        var data = _bridge.GetFleetPositionsOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            seenNodes.Add(nodeId);

            var fleetArray = data[key].AsGodotArray();
            int fleetCount = fleetArray?.Count ?? 0;
            if (fleetCount <= 0) continue;

            // More fleets = brighter/larger indicator.
            float intensity = Mathf.Clamp(fleetCount / 5.0f, 0.2f, 1.0f);
            var color = new Color(0.2f, 0.8f, 1.0f, 0.2f + intensity * 0.3f);
            float radius = 40.0f + fleetCount * 5.0f;

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, Mathf.Min(radius, 80.0f));
        }

        PruneV2OverlayDiscs(seenNodes);
    }

    private void UpdateHeatOverlayDiscsV0()
    {
        var data = _bridge.GetHeatOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            float heat = (float)data[key];
            if (heat <= 0.01f) continue;

            seenNodes.Add(nodeId);

            // Heat gradient: green (low) -> yellow -> red (high).
            float r = Mathf.Clamp(heat * 2.0f, 0f, 1f);
            float g = Mathf.Clamp(1.0f - heat, 0f, 1f);
            var color = new Color(r, g, 0.1f, 0.15f + heat * 0.35f);

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, 50.0f + heat * 30.0f);
        }

        PruneV2OverlayDiscs(seenNodes);
    }

    // GATE.S7.GALAXY_MAP_V2.EXPLORATION_OVL.001: Exploration status overlay.
    private void UpdateExplorationOverlayDiscsV0()
    {
        var data = _bridge.GetExplorationOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            var status = data[key].AsString();

            Color color;
            float size;
            switch (status)
            {
                case "anomaly":
                    color = new Color(0.7f, 0.2f, 0.9f, 0.4f);
                    size = 65.0f;
                    break;
                case "mapped":
                    color = new Color(0.2f, 0.8f, 0.3f, 0.35f);
                    size = 55.0f;
                    break;
                case "visited":
                    color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
                    size = 50.0f;
                    break;
                default: // "unvisited"
                    color = new Color(0.4f, 0.4f, 0.4f, 0.2f);
                    size = 45.0f;
                    break;
            }

            seenNodes.Add(nodeId);
            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, size);
        }

        PruneV2OverlayDiscs(seenNodes);
    }

    // GATE.S7.GALAXY_MAP_V2.WARFRONT_OVL.001: Warfront intensity overlay.
    private void UpdateWarfrontOverlayDiscsV0()
    {
        var data = _bridge.GetWarfrontOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            float intensity = (float)data[key];
            if (intensity <= 0.01f) continue;

            seenNodes.Add(nodeId);

            // Red gradient: darker red at low intensity, bright red at high.
            float r = 0.5f + intensity * 0.5f;
            float g = Mathf.Clamp(0.15f - intensity * 0.1f, 0f, 0.15f);
            float b = 0.05f;
            float a = 0.15f + intensity * 0.4f;
            var color = new Color(r, g, b, a);

            float size = 50.0f + intensity * 35.0f;
            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, size);
        }

        PruneV2OverlayDiscs(seenNodes);
    }

    private void EnsureV2OverlayDisc(string nodeId, Vector3 worldPos, Color color, float size)
    {
        if (!_v2OverlayDiscsByNodeId.TryGetValue(nodeId, out var disc))
        {
            disc = new MeshInstance3D
            {
                Name = "V2Overlay_" + nodeId,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new PlaneMesh { Size = new Vector2(size, size) },
            };
            _v2OverlayDiscsByNodeId[nodeId] = disc;
            AddChild(disc);
        }

        disc.GlobalPosition = worldPos + new Vector3(0f, -1.0f, 0f);

        // Update size if needed.
        if (disc.Mesh is PlaneMesh pm && (pm.Size.X != size || pm.Size.Y != size))
            pm.Size = new Vector2(size, size);

        disc.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            EmissionEnabled = true,
            Emission = new Color(color.R, color.G, color.B),
            EmissionEnergyMultiplier = 1.5f,
        };
    }

    private void PruneV2OverlayDiscs(HashSet<string> seenNodes)
    {
        var toRemove = new List<string>();
        foreach (var kv in _v2OverlayDiscsByNodeId)
        {
            if (!seenNodes.Contains(kv.Key))
            {
                kv.Value.QueueFree();
                toRemove.Add(kv.Key);
            }
        }
        foreach (var key in toRemove)
            _v2OverlayDiscsByNodeId.Remove(key);
    }

    private void ClearV2OverlayV0()
    {
        _v2OverlayMode = GalaxyMapV2Overlay.Off;
        foreach (var kv in _v2OverlayDiscsByNodeId)
            kv.Value.QueueFree();
        _v2OverlayDiscsByNodeId.Clear();
    }

    // ════════════════════════════════════════════════════════════════════════
    // GATE.S7.GALAXY_MAP_V2.ROUTE_PLANNER.001: Multi-hop route planner
    // ════════════════════════════════════════════════════════════════════════

    /// Public API: activate/deactivate route planner mode.
    public void SetRoutePlannerActiveV0(bool active)
    {
        _routePlannerActive = active;
        if (!active)
            ClearRoutePlannerV0();
    }

    public bool IsRoutePlannerActiveV0() => _routePlannerActive;

    /// Set route destination and draw the path.
    private void SetRoutePlannerDestV0(string destNodeId)
    {
        if (_bridge == null || string.IsNullOrEmpty(destNodeId)) return;

        _routePlannerActive = true;
        _routePlannerDestNodeId = destNodeId;

        // Clear previous route visuals.
        ClearRouteVisualsV0();

        // Query route from SimBridge.
        var routeResult = _bridge.GetRoutePathV0(destNodeId);
        var path = routeResult.ContainsKey("path") ? routeResult["path"].AsGodotArray() : null;
        int travelTime = routeResult.ContainsKey("travel_time") ? (int)routeResult["travel_time"] : 0;

        if (path == null || path.Count < 2)
        {
            // No valid route; leave planner active for next click.
            return;
        }

        // Draw polyline segments connecting each hop.
        var routeColor = new Color(0.0f, 1.0f, 0.6f, 0.8f);
        var routeMat = new StandardMaterial3D
        {
            AlbedoColor = routeColor,
            EmissionEnabled = true,
            Emission = new Color(0.0f, 1.0f, 0.6f),
            EmissionEnergyMultiplier = 3.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        for (int i = 0; i < path.Count - 1; i++)
        {
            var fromId = path[i].AsString();
            var toId = path[i + 1].AsString();

            if (!_nodeRootsById.TryGetValue(fromId, out var fromRoot)) continue;
            if (!_nodeRootsById.TryGetValue(toId, out var toRoot)) continue;

            var segment = new MeshInstance3D
            {
                Name = "RouteSegment_" + i,
                Mesh = new CylinderMesh { TopRadius = 12.0f, BottomRadius = 12.0f, Height = 1.0f },
                MaterialOverride = routeMat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            AddChild(segment);

            // Offset slightly above lane edges for visibility.
            var fromPos = fromRoot.GlobalPosition + new Vector3(0f, 3.0f, 0f);
            var toPos = toRoot.GlobalPosition + new Vector3(0f, 3.0f, 0f);
            UpdateEdgeTransformV0(segment, fromPos, toPos);

            _routePolylineSegments.Add(segment);
        }

        // Waypoint markers at each hop.
        for (int i = 1; i < path.Count - 1; i++)
        {
            var hopId = path[i].AsString();
            if (!_nodeRootsById.TryGetValue(hopId, out var hopRoot)) continue;

            var waypoint = new MeshInstance3D
            {
                Name = "RouteWaypoint_" + i,
                Mesh = new SphereMesh { Radius = 18.0f, Height = 36.0f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.0f, 1.0f, 0.6f, 0.5f),
                    EmissionEnabled = true,
                    Emission = new Color(0.0f, 1.0f, 0.6f),
                    EmissionEnergyMultiplier = 2.0f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                },
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            waypoint.GlobalPosition = hopRoot.GlobalPosition;
            AddChild(waypoint);
            _routePolylineSegments.Add(waypoint);
        }

        // Destination marker: bright ring.
        if (_nodeRootsById.TryGetValue(destNodeId, out var destRoot))
        {
            var destMarker = new MeshInstance3D
            {
                Name = "RouteDest",
                Mesh = new TorusMesh { InnerRadius = 28.0f, OuterRadius = 36.0f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.0f, 1.0f, 0.6f, 0.7f),
                    EmissionEnabled = true,
                    Emission = new Color(0.0f, 1.0f, 0.6f),
                    EmissionEnergyMultiplier = 4.0f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                },
                Rotation = new Vector3(Mathf.Pi / 2f, 0, 0),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            destMarker.GlobalPosition = destRoot.GlobalPosition;
            AddChild(destMarker);
            _routePolylineSegments.Add(destMarker);
        }

        // Travel time label near the destination.
        if (_nodeRootsById.TryGetValue(destNodeId, out var destRootForLabel))
        {
            if (_routeTravelTimeLabel != null && GodotObject.IsInstanceValid(_routeTravelTimeLabel))
                _routeTravelTimeLabel.QueueFree();

            _routeTravelTimeLabel = new Label3D
            {
                Name = "RouteTravelTime",
                Text = travelTime > 0 ? "~" + travelTime + " ticks" : "Route set",
                PixelSize = 1.0f,
                FontSize = 48,
                OutlineSize = 10,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Modulate = new Color(0.0f, 1.0f, 0.6f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            _routeTravelTimeLabel.GlobalPosition = destRootForLabel.GlobalPosition + new Vector3(0f, 70.0f, 0f);
            AddChild(_routeTravelTimeLabel);
        }
    }

    private void ClearRouteVisualsV0()
    {
        foreach (var seg in _routePolylineSegments)
        {
            if (GodotObject.IsInstanceValid(seg))
                seg.QueueFree();
        }
        _routePolylineSegments.Clear();

        if (_routeTravelTimeLabel != null && GodotObject.IsInstanceValid(_routeTravelTimeLabel))
        {
            _routeTravelTimeLabel.QueueFree();
            _routeTravelTimeLabel = null;
        }
    }

    private void ClearRoutePlannerV0()
    {
        _routePlannerActive = false;
        _routePlannerDestNodeId = "";
        ClearRouteVisualsV0();
    }

    // ════════════════════════════════════════════════════════════════════════
    // GATE.S7.GALAXY_MAP_V2.SEARCH.001: Galaxy search bar
    // ════════════════════════════════════════════════════════════════════════

    /// Toggle search bar visibility.
    private void ToggleSearchBarV0()
    {
        if (_searchBarVisible)
            HideSearchBarV0();
        else
            ShowSearchBarV0();
    }

    /// Public API: show search bar.
    public void ShowSearchBarV0()
    {
        _searchBarVisible = true;
        EnsureSearchBarCreatedV0();
        if (_searchBarRoot != null)
        {
            _searchBarRoot.Visible = true;
            _searchLineEdit?.GrabFocus();
        }
    }

    /// Public API: hide search bar.
    public void HideSearchBarV0()
    {
        _searchBarVisible = false;
        if (_searchBarRoot != null)
            _searchBarRoot.Visible = false;
        if (_searchDropdown != null)
            _searchDropdown.Visible = false;
    }

    private void EnsureSearchBarCreatedV0()
    {
        if (_searchBarRoot != null && GodotObject.IsInstanceValid(_searchBarRoot)) return;

        // Build search UI as CanvasLayer children so they render in screen space.
        var canvasLayer = new CanvasLayer { Name = "GalaxySearchLayer", Layer = 100 };

        _searchBarRoot = new Control { Name = "SearchBarRoot" };

        _searchLineEdit = new LineEdit
        {
            Name = "GalaxySearchInput",
            PlaceholderText = "Search system...",
            Position = new Vector2(10, 10),
            Size = new Vector2(300, 36),
        };
        _searchLineEdit.TextChanged += OnSearchTextChangedV0;
        _searchLineEdit.TextSubmitted += OnSearchTextSubmittedV0;
        _searchBarRoot.AddChild(_searchLineEdit);

        _searchDropdown = new ItemList
        {
            Name = "SearchDropdown",
            Position = new Vector2(10, 50),
            Size = new Vector2(300, 200),
            Visible = false,
        };
        _searchDropdown.ItemSelected += OnSearchItemSelectedV0;
        _searchBarRoot.AddChild(_searchDropdown);

        canvasLayer.AddChild(_searchBarRoot);
        GetTree().Root.AddChild(canvasLayer);
    }

    private void OnSearchTextChangedV0(string newText)
    {
        if (_bridge == null || _searchDropdown == null) return;

        _searchDropdown.Clear();

        if (string.IsNullOrEmpty(newText) || newText.Length < 1)
        {
            _searchDropdown.Visible = false;
            return;
        }

        var results = _bridge.GetSystemSearchV0(newText);
        if (results == null || results.Count == 0)
        {
            _searchDropdown.Visible = false;
            return;
        }

        int maxItems = Math.Min(results.Count, 10);
        for (int i = 0; i < maxItems; i++)
        {
            var entry = results[i].AsGodotDictionary();
            var systemId = entry.ContainsKey("system_id") ? entry["system_id"].AsString() : "";
            var name = entry.ContainsKey("name") ? entry["name"].AsString() : "";
            _searchDropdown.AddItem(name);
            _searchDropdown.SetItemMetadata(i, systemId);
        }

        _searchDropdown.Visible = true;
    }

    private void OnSearchTextSubmittedV0(string text)
    {
        if (_bridge == null) return;

        var results = _bridge.GetSystemSearchV0(text);
        if (results != null && results.Count > 0)
        {
            var entry = results[0].AsGodotDictionary();
            var systemId = entry.ContainsKey("system_id") ? entry["system_id"].AsString() : "";
            SnapCameraToNodeV0(systemId);
        }

        HideSearchBarV0();
    }

    private void OnSearchItemSelectedV0(long index)
    {
        if (_searchDropdown == null) return;

        var systemId = _searchDropdown.GetItemMetadata((int)index).AsString();
        SnapCameraToNodeV0(systemId);
        HideSearchBarV0();
    }

    /// Snap the camera to a node's world position on the galaxy map.
    private void SnapCameraToNodeV0(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) return;

        var cam = GetViewport()?.GetCamera3D();
        if (cam == null) return;

        // Move camera X/Z to node position, keep Y (altitude).
        var nodePos = nodeRoot.GlobalPosition;
        cam.GlobalPosition = new Vector3(nodePos.X, cam.GlobalPosition.Y, nodePos.Z);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001: Detail levels by camera altitude
    // ════════════════════════════════════════════════════════════════════════

    /// Called from UpdateAltitudeLodV0 every physics frame. Adjusts overlay
    /// label and detail visibility based on camera altitude thresholds.
    /// Close (<500u): full detail with station/planet labels.
    /// Medium (500-2000u): system names + faction colors only.
    /// Galaxy (>2000u): minimal dots + region labels.
    private void UpdateSemanticZoomV0(float altitude)
    {
        // Avoid redundant work if altitude band hasn't changed.
        int currentBand = altitude < 500f ? 0 : (altitude < 2000f ? 1 : 2);
        int lastBand = _lastSemanticAltitude < 500f ? 0 : (_lastSemanticAltitude < 2000f ? 1 : 2);

        _lastSemanticAltitude = altitude;

        if (currentBand == lastBand && _lastSemanticAltitude > 0f) return;

        // Iterate overlay nodes to adjust detail levels.
        foreach (var kv in _nodeRootsById)
        {
            var root = kv.Value;
            if (root == null || !root.IsInsideTree()) continue;

            var nodeLabel = root.GetNodeOrNull<Label3D>("NodeLabel");
            var fleetLabel = root.GetNodeOrNull<Label3D>("FleetLabel");
            var nodeBeacon = root.GetNodeOrNull<MeshInstance3D>("NodeMesh");
            var youLabel = root.GetNodeOrNull<Label3D>("YouLabel");
            var playerRing = root.GetNodeOrNull<MeshInstance3D>("PlayerRing");
            var sensorRing = root.GetNodeOrNull<MeshInstance3D>("SensorRing");

            switch (currentBand)
            {
                case 0: // Close range: full detail
                    if (nodeLabel != null && !_localLabelsHidden) { nodeLabel.Visible = true; nodeLabel.PixelSize = 1.2f; }
                    if (fleetLabel != null && !_localLabelsHidden) { fleetLabel.Visible = true; fleetLabel.PixelSize = 0.9f; }
                    if (nodeBeacon != null) nodeBeacon.Visible = true;
                    if (youLabel != null) youLabel.Visible = true;
                    if (playerRing != null) playerRing.Visible = true;
                    if (sensorRing != null) sensorRing.Visible = true;
                    break;

                case 1: // Medium range: system names + faction colors, no fleet detail
                    if (nodeLabel != null && !_localLabelsHidden) { nodeLabel.Visible = true; nodeLabel.PixelSize = 1.2f; }
                    if (fleetLabel != null) fleetLabel.Visible = false;
                    if (nodeBeacon != null) nodeBeacon.Visible = true;
                    if (youLabel != null) youLabel.Visible = true;
                    if (playerRing != null) playerRing.Visible = true;
                    if (sensorRing != null) sensorRing.Visible = true;
                    break;

                case 2: // Galaxy scale: minimal dots, no labels except region/faction
                    if (nodeLabel != null) nodeLabel.Visible = false;
                    if (fleetLabel != null) fleetLabel.Visible = false;
                    if (nodeBeacon != null) nodeBeacon.Visible = true;
                    if (youLabel != null) youLabel.Visible = true;
                    if (playerRing != null) playerRing.Visible = true;
                    if (sensorRing != null) sensorRing.Visible = false;
                    break;
            }
        }

        // Faction labels: visible only at medium+ altitude.
        bool showFactionLabels = currentBand >= 1;
        foreach (var kv in _factionLabelsByFactionId)
        {
            kv.Value.Visible = showFactionLabels;
        }

        // Territory discs: visible only at medium+ altitude.
        bool showTerritory = currentBand >= 1;
        foreach (var kv in _territoryDiscsByNodeId)
        {
            kv.Value.Visible = showTerritory;
        }

        // V2 overlay discs: visible only at medium+ altitude.
        bool showV2Overlays = currentBand >= 1;
        foreach (var kv in _v2OverlayDiscsByNodeId)
        {
            kv.Value.Visible = showV2Overlays;
        }

        // Volume labels: hidden at galaxy scale.
        bool showVolume = currentBand < 2;
        foreach (var kv in _volumeLabelsByKey)
        {
            kv.Value.Visible = showVolume;
        }
    }
}
