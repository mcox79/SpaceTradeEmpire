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

    // FEEL_POST_FIX_5: Subtle dark nebula background plane for galaxy map depth.
    private MeshInstance3D _galaxyMapBg;

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

    // L2.4: Hover-to-show node detail popup state.
    private string _hoveredNodeId = "";
    private float _hoverDwellTime = 0f;
    private const float HoverDwellThreshold = 0.3f; // seconds before popup appears
    [Export] public float NodeHoverThresholdPx { get; set; } = 40.0f;

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

    // GATE.S8.HAVEN.GALAXY_ICON.001: Haven starbase icon on galaxy map (visible when discovered).
    private MeshInstance3D _havenIconMesh;
    private Label3D _havenIconLabel;

    // GATE.S8.PENTAGON.DELIVERY.001: Pentagon trade dependency overlay on galaxy map.
    private readonly System.Collections.Generic.List<MeshInstance3D> _pentagonEdgeMeshes = new();
    private bool _pentagonOverlayVisible;

    // GATE.S6.UI_DISCOVERY.PHASE_MARKERS.001: Discovery phase markers on galaxy map.
    private readonly Dictionary<string, MeshInstance3D> _discoveryPhaseMarkersByDiscId = new();

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

    // GATE.T52.DISC.BREADCRUMB.001: Breadcrumb trail connecting visited nodes by visit order.
    private Node3D _breadcrumbTrailRoot;
    private readonly List<MeshInstance3D> _breadcrumbSegments = new();

    // GATE.S17.REAL_SPACE.GALAXY_RENDER.001: Persistent star billboards at galactic-scale positions.
    private Node3D _persistentStarsRoot;
    // Persistent lane lines between stars (always visible in real-space flight).
    private Node3D _persistentLanesRoot;
    // Shared lane material for alpha fade during altitude transitions.
    private StandardMaterial3D _sharedLaneMaterial;
    private const float LaneBaseAlpha = 0.25f;

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
    private int _snapshotRetryCount = 0; // Retry counter for read-lock contention
    private bool _lastPlayerHighlighted = false;

    // --- Local system config (named exported fields; no numeric literals in .cs or .tscn) ---
    // Pace overhaul: 1.6x spread for spacious systems. Planets 18-40u, gates 85u, radius 120u.
    // At 80u camera altitude + 60° FOV, visible radius ≈ 46u — player sees planets but must fly to gates.
    // VISUAL_OVERHAUL: 1.5x system scale for spacious, vast-feeling systems.
    [Export] public float SystemSceneRadiusU { get; set; } = 180.0f;
    [Export] public float StationOrbitRadiusU { get; set; } = 54.0f;
    [Export] public float LaneGateDistanceU { get; set; } = 130.0f;
    [Export] public float DiscoverySiteOrbitRadiusU { get; set; } = 85.0f;
    [Export] public float StarVisualRadiusU { get; set; } = 20.0f;
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
        // Sensor range: lanes visible only if an endpoint is within this distance of a visited node.
    // At GalacticScaleFactor=150, typical edge length ~3000-6000u. 720u reveals only directly adjacent lanes.
    [Export] public float SensorRangeGalacticU { get; set; } = 720.0f;

    // Local system state
    private Node3D _localSystemRoot;
    private Node3D _currentSolarTilt; // Solar tilt pivot — all orbital content parents here.
    private string _currentNodeId = "";

    // Binary star layout state — reset per system in DrawLocalSystemV0.
    private bool _currentSystemIsBinary = false;
    private float _binaryPlanetScaleFactor = 1.0f; // 1.6 for binary, 1.0 for solo
    private float _binarySeparation = 0f;
    private float _minPlanetOrbitRadius = 0f; // Outermost star edge + margin; planets must orbit beyond this.

    // GATE.S15.FEEL.NPC_PROXIMITY.001: Periodic fleet refresh for NPC arrivals/departures.
    private double _fleetRefreshTimer = 0.0;
    private string _currentLocalNodeId = "";

    // Arrival warp queue — spawn arriving ships one at a time for visible warp-in sequence.
    private readonly Queue<(string fleetId, Godot.Collections.Dictionary data, Vector3 spawnPos, Vector3 targetPos, float speed)> _arrivalQueue = new();
    private float _arrivalQueueTimer = 0f;
    private const float ArrivalWarpInterval = 2.5f; // seconds between arrival warps

    // Deferred spawn queue — staggers idle/docked fleet spawning across frames to avoid FPS spikes.
    private readonly Queue<(string fleetId, Vector3 spawnPos, Vector3 targetPos, float orbitRadius, float orbitSpeed, bool isDeparting, Godot.Collections.Dictionary data)> _deferredSpawnQueue = new();
    private int _deferredSpawnPerFrame = 1; // spawn 1 ship per frame

    // GATE.T43.SCAN_UI.GALAXY_MARKERS.001: Track scan markers refresh timer.
    private double _scanMarkerRefreshTimer = 0.0;
    private const double ScanMarkerRefreshInterval = 3.0; // seconds

    // GATE.T52.DISC.SCANNER_VIS.001: Dashed scanner range circle on galaxy map.
    private MeshInstance3D _scannerRangeRing;
    private int _lastScannerTier = -1;
    // Scanner range in galactic units per tier: Basic=600, Mk1=900, Mk2=1200, Mk3=1500, Fracture=2100.
    private static readonly float[] ScannerRangeByTier = { 600f, 900f, 1200f, 1500f, 2100f };
    private const int ScannerRingSegmentCount = 64;
    private const float ScannerRingDashRatio = 0.5f; // fraction of each segment that is visible

    // GATE.T44.AMBIENT.LANE_TRAFFIC.001: Lane traffic sprite refresh timer.
    private double _laneTrafficRefreshTimer = 0.0;
    private const double LaneTrafficRefreshInterval = 5.0; // seconds
    private bool _laneTrafficDirty = true;

    // GATE.T43.SCAN_UI.SIGNAL_LINES.001: Signal triangulation lines between SignalLead nodes.
    private readonly List<MeshInstance3D> _signalTriangulationLines = new();

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

        // GATE.T52.DISC.BREADCRUMB.001: Breadcrumb trail container (visible during galaxy map).
        _breadcrumbTrailRoot = new Node3D { Name = "BreadcrumbTrail" };
        _breadcrumbTrailRoot.Visible = false;

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

        // GATE.T44.AMBIENT.LANE_TRAFFIC.001: Mark lane traffic dirty on overlay open for immediate refresh.
        if (isOpen) _laneTrafficDirty = true;

        // GalaxyView's own overlay rendering (nodes, edges, colors).
        Visible = isOpen;
        SetProcess(isOpen);

        // FEEL_POST_FIX_4: Dim Starlight skybox during galaxy map so beacons pop.
        // A 2D scrim dims beacons equally (they're 3D). Instead, dim the sky itself.
        var skyParent = GetParent();
        if (skyParent != null)
        {
            var starlightSky = skyParent.GetNodeOrNull<Node3D>("StarlightSky");
            if (starlightSky != null)
                starlightSky.Visible = !isOpen;
            // Also dim the milky way nebula band (GalacticSky).
            var galacticSky = skyParent.GetNodeOrNull<Node3D>("GalacticSky");
            if (galacticSky != null)
                galacticSky.Visible = !isOpen;
        }

        // Hide background starfield CanvasLayer during galaxy map so procedural stars
        // don't visually compete with actual star system nodes.
        var bgGroup = GetTree()?.GetNodesInGroup("BackgroundStarfield");
        if (bgGroup != null)
        {
            for (int bi = 0; bi < bgGroup.Count; bi++)
            {
                if (bgGroup[bi] is CanvasLayer bgCanvas)
                    bgCanvas.Visible = !isOpen;
            }
        }

        // FEEL_POST_FIX_5: Show/hide subtle galaxy background plane + starfield for depth.
        EnsureGalaxyMapBgV0();
        if (_galaxyMapBg != null)
            _galaxyMapBg.Visible = isOpen;
        if (_galaxyMapStars != null)
            _galaxyMapStars.Visible = isOpen;

        if (isOpen)
        {
            // Defer one frame so SimBridge can finish boot sequences.
            // Reset retry counter — RefreshFromSnapshotV0 retries if read-lock fails.
            _snapshotRetryCount = 0;
            CallDeferred(nameof(RefreshFromSnapshotV0));
        }
        else
        {
            // GATE.S7.GALAXY_MAP_V2: Clean up V2 overlays, route planner, and search bar on close.
            ClearV2OverlayV0();
            ClearRoutePlannerV0();
            HideSearchBarV0();
            HideNodeDetailPopupV0();
            // GATE.T52.DISC: Hide scanner range ring and discovery phase markers on close.
            if (_scannerRangeRing != null && GodotObject.IsInstanceValid(_scannerRangeRing))
                _scannerRangeRing.Visible = false;
            ClearDiscoveryPhaseMarkersV0();
            // GATE.T52.DISC.BREADCRUMB.001: Clear breadcrumb trail on overlay close.
            ClearBreadcrumbTrailV0();
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
            // GATE.T52.DISC.SCANNER_VIS.001: Hide scanner range ring during UI panel.
            if (_scannerRangeRing != null && GodotObject.IsInstanceValid(_scannerRangeRing))
                _scannerRangeRing.Visible = false;
            // GATE.T52.DISC.BREADCRUMB.001: Hide breadcrumb trail during UI panel.
            if (_breadcrumbTrailRoot != null)
                _breadcrumbTrailRoot.Visible = false;
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

        // Persistent stars + distant system details: always visible so neighbors are seen from any mode.
        if (_persistentStarsRoot != null)
        {
            _persistentStarsRoot.Visible = true;
            // Hide the current system's persistent star when the detailed local system is visible.
            if (!string.IsNullOrEmpty(_currentLocalNodeId))
            {
                var currentStar = _persistentStarsRoot.GetNodeOrNull<Node3D>("PersistentStar_" + _currentLocalNodeId);
                if (currentStar != null)
                    currentStar.Visible = altitude >= 500f; // Only show once local system is hidden.
            }
        }

        // GATE.S7.GALAXY_MAP_V2.SEMANTIC_ZOOM.001: Update detail levels by altitude.
        if (_overlayOpen)
            UpdateSemanticZoomV0(altitude);

        // Persistent 3D lane lines: fade in over 600-900u, solid above 900u.
        // Starts AFTER local system hides (500u) so lanes don't overlap planet view.
        if (_persistentLanesRoot != null)
        {
            // Eagerly build lanes if root is empty and we're zooming out.
            if (altitude >= 600f && _persistentLanesRoot.GetChildCount() == 0)
                SpawnPersistentLanesV0();

            bool shouldShow = altitude >= 600f;
            _persistentLanesRoot.Visible = shouldShow;

            if (shouldShow && _sharedLaneMaterial != null)
            {
                float fadeAlpha;
                if (altitude < 750f)
                    fadeAlpha = (altitude - 600f) / 150f; // 0→1 over 600-750u
                else if (altitude < 900f)
                    fadeAlpha = (altitude - 750f) / 150f * 0.5f + 0.5f; // 0.5→1 over 750-900u
                else
                    fadeAlpha = 1f;
                fadeAlpha = Mathf.Clamp(fadeAlpha, 0f, 1f);
                var c = _sharedLaneMaterial.AlbedoColor;
                _sharedLaneMaterial.AlbedoColor = new Color(c.R, c.G, c.B, LaneBaseAlpha * fadeAlpha);
            }
        }

        // GATE.T52.DISC.BREADCRUMB.001: Breadcrumb trail visibility matches lanes.
        if (_breadcrumbTrailRoot != null)
        {
            if (altitude >= 600f && _breadcrumbTrailRoot.GetChildCount() == 0)
                SpawnBreadcrumbTrailV0();
            _breadcrumbTrailRoot.Visible = altitude >= 600f;
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

        // GATE.T52.DISC.BREADCRUMB.001: Rebuild breadcrumb trail when lanes rebuild (new node visited).
        RebuildBreadcrumbTrailV0();
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
            _snapshotRetryCount = 0;
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

            // Process arrival warp queue — spawn one arriving ship at a time.
            if (_arrivalQueue.Count > 0)
            {
                _arrivalQueueTimer -= (float)delta;
                if (_arrivalQueueTimer <= 0f)
                {
                    _arrivalQueueTimer = ArrivalWarpInterval;
                    var (fleetId, data, spawnPos, targetPos, speed) = _arrivalQueue.Dequeue();
                    SpawnQueuedArrivalV0(fleetId, data, spawnPos, targetPos, speed);
                }
            }

            // Process deferred spawn queue — 1 ship per frame to avoid FPS spikes.
            if (_deferredSpawnQueue.Count > 0)
            {
                var (dfId, dfSpawn, dfTarget, dfOrbit, dfOrbitSpd, dfDepart, dfData) = _deferredSpawnQueue.Dequeue();
                SpawnDeferredFleetV0(dfId, dfSpawn, dfTarget, dfOrbit, dfOrbitSpd, dfDepart, dfData);
            }
        }

        if (!_overlayOpen) return;

        // GATE.T43.SCAN_UI.GALAXY_MARKERS.001: Periodic scan marker refresh.
        _scanMarkerRefreshTimer += delta;
        if (_scanMarkerRefreshTimer >= ScanMarkerRefreshInterval)
        {
            _scanMarkerRefreshTimer = 0.0;
            UpdateScanMarkersV0();
        }

        // L2.4: Hover detection — find nearest node to mouse and show popup after dwell.
        UpdateHoverDetectionV0((float)delta);

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

        // GATE.T50.VISUAL.GALAXY_ECON.001: Pulse economic activity glow on PlanetDot nodes.
        _PulseEconGlowV0(delta);

        RefreshFromSnapshotV0();

        // GATE.S7.GALAXY_MAP_V2.OVERLAYS.001: Refresh V2 overlay visuals after snapshot.
        if (_v2OverlayMode != GalaxyMapV2Overlay.Off)
            UpdateV2OverlayVisualsV0();

        // GATE.S6.UI_DISCOVERY.SCAN_VIZ.001: Update scan pulse animation.
        UpdateScanPulseV0((float)delta);

        // GATE.S6.UI_DISCOVERY.SCAN_VIZ.001: Discovery highlight at current node.
        if (!_overlayOpen)
            UpdateDiscoveryHighlightsV0();

        // GATE.T44.AMBIENT.LANE_TRAFFIC.001 + GATE.T44.DIGEST.MEGAPROJECT_MAP.001:
        // Throttled refresh for lane traffic sprites and megaproject markers (every 5s).
        if (_overlayOpen)
        {
            _laneTrafficRefreshTimer += delta;
            if (_laneTrafficDirty || _laneTrafficRefreshTimer >= LaneTrafficRefreshInterval)
            {
                _laneTrafficRefreshTimer = 0.0;
                _laneTrafficDirty = false;
                RefreshLaneTrafficAndMegaprojectsV0();
            }
        }
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

    private void HideNodeDetailPopupV0()
    {
        if (_nodeDetailPopup != null && GodotObject.IsInstanceValid(_nodeDetailPopup))
        {
            _nodeDetailPopup.Set("visible", false);
        }
        _hoveredNodeId = "";
        _hoverDwellTime = 0f;
    }

    /// <summary>
    /// Bot helper: directly show node detail popup without mouse hover.
    /// </summary>
    public void ShowNodePopupForBot(string nodeId)
    {
        var vpSize = GetViewport().GetVisibleRect().Size;
        ShowNodeDetailPopupV0(nodeId, vpSize * 0.5f);
    }

    /// <summary>
    /// L2.4: Per-frame hover detection — find closest node to mouse cursor.
    /// After HoverDwellThreshold seconds on the same node, show the detail popup.
    /// </summary>
    private void UpdateHoverDetectionV0(float delta)
    {
        var activeCam = GetViewport()?.GetCamera3D();
        if (activeCam == null) return;

        var mousePos = GetViewport().GetMousePosition();
        string closestNodeId = null;
        float closestDist = NodeHoverThresholdPx;

        foreach (var kv in _nodeRootsById)
        {
            var root = kv.Value;
            if (root == null || !root.IsInsideTree() || !root.Visible) continue;

            var worldPos = root.GlobalPosition;
            if (activeCam.IsPositionBehind(worldPos)) continue;

            var screenPos = activeCam.UnprojectPosition(worldPos);
            float dist = screenPos.DistanceTo(mousePos);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestNodeId = kv.Key;
            }
        }

        if (closestNodeId == null)
        {
            _hoveredNodeId = "";
            _hoverDwellTime = 0f;
            return;
        }

        if (closestNodeId != _hoveredNodeId)
        {
            _hoveredNodeId = closestNodeId;
            _hoverDwellTime = 0f;
            return;
        }

        _hoverDwellTime += delta;
        if (_hoverDwellTime >= HoverDwellThreshold && _hoverDwellTime - delta < HoverDwellThreshold)
        {
            // Just crossed the threshold — show popup at mouse position.
            ShowNodeDetailPopupV0(closestNodeId, mousePos);
        }
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

        // GATE.T52.DISC.BREADCRUMB.001: Breadcrumb trail container.
        if (_breadcrumbTrailRoot != null && _breadcrumbTrailRoot.GetParent() == null)
        {
            GetParent().AddChild(_breadcrumbTrailRoot);
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

            float emissionStrength = isDiscovered ? 14.0f : 3.0f;
            float starSize = isDiscovered ? 35.0f : 18.0f;
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

            // Discovered stars get a ring halo for visual differentiation from background.
            // Stellaris-style: colored ring around each known system node.
            if (isDiscovered)
            {
                var ringMat = new StandardMaterial3D
                {
                    AlbedoColor = new Color(starColor.R, starColor.G, starColor.B, 0.5f),
                    EmissionEnabled = true,
                    Emission = starColor,
                    EmissionEnergyMultiplier = 6.0f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                    NoDepthTest = true,
                };
                var ring = new MeshInstance3D
                {
                    Name = "StarRing",
                    Mesh = new TorusMesh { InnerRadius = starSize * 1.8f, OuterRadius = starSize * 2.5f, Rings = 16, RingSegments = 24 },
                    MaterialOverride = ringMat,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                star.AddChild(ring);

                // Add simplified planet dots + orbital disc so neighbors look like tiny solar systems.
                AddDistantSystemDetailsV0(star, nodeId);
            }

            _persistentStarsRoot.AddChild(star);
        }
    }

    /// Adds simplified planet dots and a faint orbital disc to a persistent star node,
    /// making distant systems visible as tiny solar systems from any flight altitude.
    private void AddDistantSystemDetailsV0(Node3D starNode, string nodeId)
    {
        // Solar tilt for this system (same hash as local view so tilts match).
        var tiltHash = Fnv1a64(nodeId + "_solar_tilt");
        float tiltXRad = ((tiltHash % 1000UL) / 1000f - 0.5f) * 2f * 0.30f; // ±17°
        float tiltZRad = (((tiltHash >> 16) % 1000UL) / 1000f - 0.5f) * 2f * 0.25f; // ±14°

        var tiltPivot = new Node3D { Name = "DistantTilt" };
        tiltPivot.RotationDegrees = new Vector3(
            tiltXRad * (180f / MathF.PI), 0f, tiltZRad * (180f / MathF.PI));
        starNode.AddChild(tiltPivot);

        // Faint orbital disc showing the ecliptic plane.
        var discMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.6f, 0.9f, 0.04f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            NoDepthTest = true,
        };
        var disc = new MeshInstance3D
        {
            Name = "OrbitalDisc",
            Mesh = new TorusMesh { InnerRadius = 50f, OuterRadius = 250f, Rings = 6, RingSegments = 32 },
            MaterialOverride = discMat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        tiltPivot.AddChild(disc);

        // 2-4 planet dots at hash-based orbital positions.
        var pHash = Fnv1a64(nodeId + "_distant_planets");
        int planetCount = 2 + (int)(pHash % 3); // 2-4

        for (int i = 0; i < planetCount; i++)
        {
            var pH = Fnv1a64(nodeId + "_dplanet_" + i);
            float orbitR = 80f + (pH % 170); // 80-250u orbit radius
            float angle = ((pH >> 8) % 3600) / 10f * (MathF.PI / 180f);
            float dotSize = 5f + (pH % 50) / 10f; // 5-10u

            // Subtle planet color variation.
            float cr = 0.5f + (pH % 40) / 100f;
            float cg = 0.5f + ((pH >> 4) % 40) / 100f;
            float cb = 0.55f + ((pH >> 8) % 35) / 100f;

            var planetDot = new MeshInstance3D
            {
                Name = "DistantPlanet_" + i,
                Mesh = new SphereMesh { Radius = dotSize, Height = dotSize * 2f },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(cr, cg, cb),
                    EmissionEnabled = true,
                    Emission = new Color(cr * 0.4f, cg * 0.4f, cb * 0.4f),
                    EmissionEnergyMultiplier = 2.5f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
                Position = new Vector3(MathF.Cos(angle) * orbitR, 0f, MathF.Sin(angle) * orbitR),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            tiltPivot.AddChild(planetDot);
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
            AlbedoColor = new Color(0.3f, 0.45f, 0.7f, LaneBaseAlpha),
            EmissionEnabled = true,
            Emission = new Color(0.2f, 0.35f, 0.6f),
            EmissionEnergyMultiplier = 1.2f,
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

            // Fog of war: only show lanes where BOTH endpoints have been visited.
            // Lanes to unexplored systems stay hidden until the player arrives there.
            if (_bridge != null)
            {
                bool fromVisited = !_bridge.IsFirstVisitV0(fromId);
                bool toVisited = !_bridge.IsFirstVisitV0(toId);
                if (!fromVisited || !toVisited) continue;
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
                Mesh = new CylinderMesh { TopRadius = 12.0f, BottomRadius = 12.0f, Height = 1.0f },
                MaterialOverride = laneMat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            // Connect gate-to-gate: use pre-computed gate positions so the lane line
            // starts/ends at the exact gate marker positions.
            var gateFrom = GetCachedGateGlobalPositionV0(fromId, toId);
            var gateTo = GetCachedGateGlobalPositionV0(toId, fromId);
            if (drawnKeys.Count <= 3) // Debug first 3 lanes
            {
                var starFrom = GetNodeScaledPositionV0(fromId);
                var starTo = GetNodeScaledPositionV0(toId);
            }
            UpdateEdgeTransformV0(mesh, gateFrom, gateTo);
            _persistentLanesRoot.AddChild(mesh);
        }
    }

    // ── GATE.T52.DISC.BREADCRUMB.001: Breadcrumb trail connecting visited nodes ──

    private void ClearBreadcrumbTrailV0()
    {
        if (_breadcrumbTrailRoot == null) return;
        foreach (var child in _breadcrumbTrailRoot.GetChildren())
            child.QueueFree();
        _breadcrumbSegments.Clear();
    }

    private void RebuildBreadcrumbTrailV0()
    {
        ClearBreadcrumbTrailV0();
        SpawnBreadcrumbTrailV0();
    }

    /// <summary>
    /// Draw thin connecting lines between visited nodes in visit order.
    /// Lines fade by recency: most recent = bright white-blue, oldest = faint.
    /// Lines are thinner than lane lines (radius 4 vs 12) to distinguish them.
    /// </summary>
    private void SpawnBreadcrumbTrailV0()
    {
        if (_bridge == null || _breadcrumbTrailRoot == null) return;

        var visitHistory = _bridge.GetVisitHistoryV0();
        if (visitHistory == null || visitHistory.Count < 2) return;

        // Build node position lookup from galaxy snapshot.
        var galSnap = _bridge.GetGalaxySnapshotV0();
        if (galSnap == null || !galSnap.ContainsKey("system_nodes")) return;

        float scale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;
        var rawNodes = galSnap.ContainsKey("system_nodes")
            ? (Godot.Collections.Array)galSnap["system_nodes"]
            : new Godot.Collections.Array();

        var posById = new Dictionary<string, Vector3>();
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
            posById[nid] = new Vector3(px * scale, py * scale, pz * scale);
        }

        // Extract ordered node IDs from visit history (already sorted by tick asc).
        int segmentCount = visitHistory.Count - 1;
        for (int i = 0; i < segmentCount; i++)
        {
            var fromEntry = visitHistory[i].AsGodotDictionary();
            var toEntry = visitHistory[i + 1].AsGodotDictionary();
            var fromId = fromEntry.ContainsKey("node_id") ? (string)(Variant)fromEntry["node_id"] : "";
            var toId = toEntry.ContainsKey("node_id") ? (string)(Variant)toEntry["node_id"] : "";

            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId)) continue;
            if (!posById.TryGetValue(fromId, out var fromPos)) continue;
            if (!posById.TryGetValue(toId, out var toPos)) continue;

            // Recency alpha: oldest segment (i=0) = 0.15, newest (i=segmentCount-1) = 0.85.
            float t = segmentCount > 1 ? (float)i / (segmentCount - 1) : 1f;
            float alpha = Mathf.Lerp(0.15f, 0.85f, t);

            var mat = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.6f, 0.8f, 1.0f, alpha),
                EmissionEnabled = true,
                Emission = new Color(0.5f, 0.7f, 0.95f),
                EmissionEnergyMultiplier = 0.8f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            };

            var mesh = new MeshInstance3D
            {
                Name = "Breadcrumb_" + i,
                Mesh = new CylinderMesh { TopRadius = 4.0f, BottomRadius = 4.0f, Height = 1.0f },
                MaterialOverride = mat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };

            UpdateEdgeTransformV0(mesh, fromPos, toPos);
            _breadcrumbTrailRoot.AddChild(mesh);
            _breadcrumbSegments.Add(mesh);
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

        // Reset binary state for this system.
        _currentSystemIsBinary = false;
        _binaryPlanetScaleFactor = 1.0f;
        _binarySeparation = 0f;
        _minPlanetOrbitRadius = 0f;

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

        // 1. Star system (single / binary / trinary with mutual orbits).
        var starAnchor = SpawnStarSystemV0(nodeId, starColor, starClass);
        starAnchor.AddToGroup("LocalStar");
        _localSystemRoot.AddChild(starAnchor);

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
        // Per-system hue tint on fog for visual variety.
        var tintedFog = ApplySystemHueTintV0(profile.FogAlbedo, nodeId);
        // Emission scales with glow multiplier — hot stars have brighter corona haze.
        float glowMul = profile.GlowMultiplier;
        var fogMat = new FogMaterial
        {
            Density = profile.FogDensity,
            Albedo = tintedFog,
            Emission = new Color(
                starColor.R * 0.15f * glowMul,
                starColor.G * 0.12f * glowMul,
                starColor.B * 0.10f * glowMul),
        };
        fogVol.Material = fogMat;
        _localSystemRoot.AddChild(fogVol);

        // Solar tilt: each system gets a unique orbital plane tilt (seeded from nodeId).
        // All orbital content (planets, moons, stations, belt) parents under this tilt node
        // so the entire ecliptic plane is angled. Gates and discovery sites stay untilted.
        _currentSolarTilt = new Node3D { Name = "SolarTiltPivot" };
        var tiltHash = Fnv1a64(nodeId + "_solar_tilt");
        float tiltXRad = ((tiltHash % 1000UL) / 1000f - 0.5f) * 2f * 0.30f; // ±17°
        float tiltZRad = (((tiltHash >> 16) % 1000UL) / 1000f - 0.5f) * 2f * 0.25f; // ±14°
        _currentSolarTilt.RotationDegrees = new Vector3(
            tiltXRad * (180f / MathF.PI), 0f, tiltZRad * (180f / MathF.PI));
        _localSystemRoot.AddChild(_currentSolarTilt);

        // 1b. Planet orbiting the star.
        var (planetPos, planetType, planetOrbitPivot) = SpawnLocalPlanetV0(nodeId, lumScale);

        // 1c. Moons around the planet (orbit inside planet pivot).
        SpawnMoonsV0(nodeId, planetPos, planetType, planetOrbitPivot);

        // 1d. Visual-only outer planets for system depth.
        SpawnOuterPlanetsV0(nodeId, lumScale, planetType);

        // 1e. Asteroid belt between inner and outer zones.
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

        // GATE.T44.AMBIENT: Economy-driven ambient visuals (shuttles, mining, prosperity, warfront).
        SpawnAmbientEconomyVisualsV0(nodeId);

        // GATE.S15.FEEL.NPC_PROXIMITY.001: Enable periodic fleet refresh.
        // Short initial timer (0.1s) so first refresh fires quickly — catches fleets
        // missed by SpawnFleetsV0 due to SimBridge read-lock contention at boot.
        _fleetRefreshTimer = 0.1;
    }

    // GATE.S15.FEEL.AMBIENT_SYSTEM.001: Ambient dust particle systems.
    // Star dust (all systems): diffuse white/pale-blue motes spread across a large sphere.
    // Asteroid dust (60% of systems): tan/brown motes near the belt radius.
    // VISUAL_OVERHAUL: Star-class drives dust color via visual profile.
    private void SpawnAmbientDustV0(string nodeId, string starClass = "ClassG")
    {
        var dustProfile = GetSystemVisualProfileV0(starClass);
        // Per-system hue tint applied to dust for visual variety.
        var tintedDust = ApplySystemHueTintV0(dustProfile.DustColor, nodeId);
        // ── Star dust (every system) ──
        var starDustProc = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 75.0f, // VISUAL_OVERHAUL: 1.5x scale
            Gravity = Vector3.Zero,
            InitialVelocityMin = 0.1f,
            InitialVelocityMax = 0.5f,
            Color = new Color(tintedDust.R, tintedDust.G, tintedDust.B, dustProfile.DustAlpha),
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
            float beltRadius = _currentSystemIsBinary ? 155.0f : 120.0f;

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
            if (_currentSolarTilt != null)
                _currentSolarTilt.AddChild(beltDust);
            else
                _localSystemRoot.AddChild(beltDust);
        }
    }

    private void ClearLocalSystemV0()
    {
        // GATE.S15.FEEL.NPC_PROXIMITY.001: Stop periodic fleet refresh on system clear.
        _currentLocalNodeId = "";
        _deferredSpawnQueue.Clear();

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
        // FEEL_POST_FIX_5: Compact station Label3D — strip ALL resource tags.
        // Full name preserved in dock panel header. Label3D shows "System 10 Station".
        var stationDisplayName = stationDict.ContainsKey("node_name") && !string.IsNullOrEmpty((string)stationDict["node_name"])
            ? StripResourceTagsV0((string)stationDict["node_name"]) + " Station"
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
            // Height 10u to catch ships at minor Y-lift altitudes.
            Shape = new BoxShape3D { Size = new Vector3(5f, 10f, 5f) }
        };
        station.AddChild(collider);

        // GATE.S1.VISUAL_POLISH.STRUCTURES.001: ring/cylinder station geometry with slow rotation.
        var stationVisual = new Node3D { Name = "StationVisual" };
        // Attach spinning script for slow Y-axis rotation.
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null) stationVisual.SetScript(spinScript);

        // GATE.X.STATION_IDENTITY.VISUAL.001: Faction hull tint and tier-based size variation.
        var hullTint = new Color(0.28f, 0.30f, 0.34f); // default neutral gray
        string stationFactionId = "";
        if (_bridge != null && !string.IsNullOrEmpty(nodeId))
        {
            var terr = _bridge.GetTerritoryAccessV0(nodeId);
            stationFactionId = terr.ContainsKey("faction_id") ? (string)terr["faction_id"] : "";
            if (!string.IsNullOrEmpty(stationFactionId))
            {
                var fColors = _bridge.GetFactionColorsV0(stationFactionId);
                if (fColors.ContainsKey("primary"))
                {
                    var fc = (Color)fColors["primary"];
                    // Blend faction primary with hull base for subtle tint (40% faction, 60% base).
                    hullTint = new Color(
                        0.6f * 0.28f + 0.4f * fc.R,
                        0.6f * 0.30f + 0.4f * fc.G,
                        0.6f * 0.34f + 0.4f * fc.B);
                }
            }
        }
        // Tier heuristic: lane_gate count → outpost (1) / hub (2-3) / capital (4+).
        int laneGateCount = snap.ContainsKey("lane_gate") ? snap["lane_gate"].AsGodotArray().Count : 0;
        int stationTier = laneGateCount >= 4 ? 2 : (laneGateCount <= 1 ? 0 : 1); // STRUCTURAL: 0/1/2
        float tierScale = stationTier switch { 0 => 0.6f, 2 => 1.5f, _ => 1.0f }; // STRUCTURAL: tier scale factors
        stationVisual.Scale = Vector3.One * tierScale;

        var hullMat = new StandardMaterial3D
        {
            AlbedoColor = hullTint,
            Roughness = 0.50f,
            Metallic = 0.55f,
            EmissionEnabled = true,
            Emission = new Color(hullTint.R * 0.2f, hullTint.G * 0.2f, hullTint.B * 0.2f),
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
                // Use the Kenney model's own materials/textures — don't override.
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

        // Station/faction labels removed — no floating text in space.
        // Station identity conveyed via HUD when in proximity.

        // Station orbit pivot: centered at planet position, slow orbit around planet.
        var stationOrbitPivot = new Node3D { Name = "StationOrbitPivot" };
        stationOrbitPivot.Position = planetPos; // Planet position within the planet orbit pivot
        var stationOrbitSpin = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (stationOrbitSpin != null)
        {
            stationOrbitPivot.SetScript(stationOrbitSpin);
            stationOrbitPivot.Set("spin_speed_y", 0.08f); // Station orbits planet
        }

        // Station orbits well outside planet sphere (planets up to ~9u visual radius).
        var stationOrbitPos = DeriveOrbitPositionV0(nodeId + "_station_offset", 15.0f);
        station.Position = new Vector3(stationOrbitPos.X, 1.0f, stationOrbitPos.Z); // Y=1 so station renders above ships.
        station.SetMeta("avoidance_radius", 8.0);
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
        else if (_currentSolarTilt != null)
            _currentSolarTilt.AddChild(station);
        else
            _localSystemRoot.AddChild(station);
    }

    private void SpawnLaneGatesV0(Godot.Collections.Dictionary snap)
    {
        if (!snap.ContainsKey("lane_gate")) return;
        var gates = snap["lane_gate"].AsGodotArray();

        // Use _currentNodeId (set by DrawLocalSystemV0) — snap doesn't contain "node_id".
        var currentNodeId = _currentNodeId ?? "";

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
                    // Flatten to XZ plane — gates stay on the orbital plane.
                    var dir3d = neighborPos - currentPos;
                    var dir2d = new Vector3(dir3d.X, 0f, dir3d.Z).Normalized();
                    gatePos = dir2d * LaneGateDistanceU;
                }
                else
                {
                    gatePos = DeriveLaneGatePositionV0(i, gates.Count, LaneGateDistanceU);
                }
            }
            // Force Y=0 — all gates on the orbital plane regardless of source.
            gatePos = new Vector3(gatePos.X, 0f, gatePos.Z);
            marker.Position = gatePos;
            if (gatePos != Vector3.Zero)
            {
                // Gate faces outward — explicit Y rotation only (no tilting).
                // atan2(X, Z) gives the angle from +Z toward +X in the XZ plane.
                float yaw = MathF.Atan2(gatePos.X, gatePos.Z);
                marker.Rotation = new Vector3(0f, yaw, 0f);
            }
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

            // Use the same travel-progress-based positioning as RefreshLocalFleetsV0.
            var currentNodeId = f.ContainsKey("current_node_id") ? (string)f["current_node_id"] : "";
            var destNodeId = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
            var fleetState = f.ContainsKey("state") ? (string)f["state"] : "Idle";
            float travelProgress = f.ContainsKey("travel_progress") ? (float)f["travel_progress"] : 0f;
            int arrRole = f.ContainsKey("role") ? (int)f["role"] : 0;
            float arrOrbit = _currentSystemIsBinary
                ? (arrRole == 2 ? 130.0f : 95.0f)   // Binary: outside stability zone
                : (arrRole == 2 ? 65.0f  : 25.0f);  // Solo: unchanged
            float arrSpd = arrRole == 2 ? 8.0f : arrRole == 1 ? 4.5f : 5.0f;
            var currentTask = f.ContainsKey("current_task") ? (string)f["current_task"] : "Idle";
            var finalDest = f.ContainsKey("final_destination_node_id") ? (string)f["final_destination_node_id"] : "";

            bool atThisNode = StringComparer.Ordinal.Equals(currentNodeId, _currentLocalNodeId);
            Vector3 spawnPos;
            Vector3 targetPos;

            if (atThisNode && fleetState != "Traveling")
            {
                // Fleet is idle/docked — spawn near station. If it has a destination, target the gate.
                spawnPos = DeriveOrbitPositionV0(fleetId + "_local", arrOrbit);
                var idleDest = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                if (!string.IsNullOrEmpty(idleDest) && !StringComparer.Ordinal.Equals(idleDest, _currentLocalNodeId))
                {
                    targetPos = GetGateLocalPositionForNeighborV0(idleDest);
                    if (targetPos == Vector3.Zero)
                        targetPos = DeriveOrbitPositionV0(fleetId + "_depart", LaneGateDistanceU);
                }
                else
                {
                    targetPos = DeriveOrbitPositionV0(fleetId + "_hold", arrOrbit * 0.8f);
                }
            }
            else if (atThisNode && fleetState == "Traveling" && !string.IsNullOrEmpty(destNodeId)
                && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId))
            {
                // Fleet is departing — position between station and departure gate.
                var gatePos = GetGateLocalPositionForNeighborV0(destNodeId);
                if (gatePos == Vector3.Zero)
                    gatePos = DeriveOrbitPositionV0(fleetId + "_depart", LaneGateDistanceU);
                var stationPos = DeriveOrbitPositionV0(fleetId + "_local", arrOrbit);
                spawnPos = stationPos.Lerp(gatePos, Mathf.Clamp(travelProgress * 2f, 0f, 1f));
                targetPos = gatePos;
            }
            else
            {
                // Fleet arriving from another system — queue for staggered warp-in.
                var sourceNeighbor = !string.IsNullOrEmpty(currentNodeId) ? currentNodeId : "";
                var gatePos = !string.IsNullOrEmpty(sourceNeighbor)
                    ? GetGateLocalPositionForNeighborV0(sourceNeighbor)
                    : Vector3.Zero;
                if (gatePos == Vector3.Zero)
                    gatePos = DeriveOrbitPositionV0(fleetId + "_arrive", LaneGateDistanceU);
                var orbitPos = DeriveOrbitPositionV0(fleetId + "_local", arrOrbit);

                // If well past the gate (progress > 0.7), spawn immediately (already in-system).
                if (travelProgress > 0.7f)
                {
                    float arrivalT = Mathf.Clamp((travelProgress - 0.5f) * 2f, 0f, 1f);
                    spawnPos = gatePos.Lerp(orbitPos, arrivalT);
                    targetPos = orbitPos;
                }
                else
                {
                    // Enqueue for staggered warp-in at gate. Ship not yet spawned.
                    ship.QueueFree(); // Don't spawn yet — SpawnQueuedArrivalV0 will re-create.
                    _arrivalQueue.Enqueue((fleetId, f, gatePos, orbitPos, arrSpd));
                    continue;
                }
            }

            bool isDeparting = atThisNode && fleetState == "Traveling" && !string.IsNullOrEmpty(destNodeId)
                && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId);
            float orbitAngularSpeed = (atThisNode && fleetState != "Traveling")
                ? KeplerOrbitSpeed(arrOrbit, KeplerK_Planet) * 0.5f : 0f;

            // Spawn ALL ships immediately at boot — starter systems have 4-6 ships,
            // not enough to justify deferral. Fixes NPC=1 census bug (5 consecutive audits).
            ship.Position = spawnPos;
            ship.AddToGroup("FleetShip");
            _localSystemRoot.AddChild(ship);
            if (ship.HasMethod("update_transit"))
                ship.Call("update_transit", f);
            if (isDeparting && ship.HasMethod("begin_departure_v0"))
                ship.Call("begin_departure_v0", targetPos);
            else if (orbitAngularSpeed > 0f && ship.HasMethod("set_orbit_v0"))
                ship.Call("set_orbit_v0", arrOrbit, orbitAngularSpeed);
            else if (ship.HasMethod("set_target"))
                ship.Call("set_target", targetPos, arrSpd);
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

        // Departing fleets: mark for departure. They'll fly to the nearest gate
        // and warp out when they arrive (handled by npc_ship.gd _physics_process).
        var warpScript = GD.Load<Script>("res://scripts/vfx/warp_effect.gd");
        foreach (var id in existingFleetIds)
        {
            if (!transitFleetIds.Contains(id) && existingNodes.TryGetValue(id, out var node))
            {
                if (node is Node3D n3d && n3d.HasMethod("begin_departure_v0"))
                {
                    // Find the nearest gate for this fleet to fly toward.
                    var gatePos = FindNearestGateLocalPositionV0(n3d.Position);
                    n3d.Call("begin_departure_v0", gatePos);
                }
                else if (node is Node3D n3d2 && warpScript != null)
                {
                    warpScript.Call("play_warp_out", n3d2);
                }
                else
                {
                    node.QueueFree();
                }
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

                // Determine spawn position based on fleet transit state + progress.
                var currentNodeId = f.ContainsKey("current_node_id") ? (string)f["current_node_id"] : "";
                var destNodeId = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                var fleetState = f.ContainsKey("state") ? (string)f["state"] : "Idle";
                float travelProgress = f.ContainsKey("travel_progress") ? (float)f["travel_progress"] : 0f;
                int arrRole = f.ContainsKey("role") ? (int)f["role"] : 0;
                float arrOrbit = _currentSystemIsBinary
                ? (arrRole == 2 ? 130.0f : 95.0f)   // Binary: outside stability zone
                : (arrRole == 2 ? 65.0f  : 25.0f);  // Solo: unchanged
                float arrSpd = arrRole == 2 ? 8.0f : arrRole == 1 ? 4.5f : 5.0f;

                // Is this fleet already at our node, or arriving from elsewhere?
                bool atThisNode = StringComparer.Ordinal.Equals(currentNodeId, _currentLocalNodeId);

                Vector3 spawnPos;
                Vector3 targetPos;
                bool playWarpIn = false;

                if (atThisNode && fleetState != "Traveling")
                {
                    // Fleet is idle/docked — spawn near station, target gate if has destination.
                    spawnPos = DeriveOrbitPositionV0(id + "_local", arrOrbit);
                    var idleDest = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                    if (!string.IsNullOrEmpty(idleDest) && !StringComparer.Ordinal.Equals(idleDest, _currentLocalNodeId))
                    {
                        targetPos = GetGateLocalPositionForNeighborV0(idleDest);
                        if (targetPos == Vector3.Zero)
                            targetPos = DeriveOrbitPositionV0(id + "_depart", LaneGateDistanceU);
                    }
                    else
                    {
                        targetPos = DeriveOrbitPositionV0(id + "_hold", arrOrbit * 0.8f);
                    }
                }
                else if (atThisNode && fleetState == "Traveling" && !string.IsNullOrEmpty(destNodeId)
                    && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId))
                {
                    // Fleet is departing — position between station and departure gate based on progress.
                    var gatePos = GetGateLocalPositionForNeighborV0(destNodeId);
                    if (gatePos == Vector3.Zero)
                        gatePos = DeriveOrbitPositionV0(id + "_depart", LaneGateDistanceU);
                    var stationPos = DeriveOrbitPositionV0(id + "_local", arrOrbit);
                    // Lerp from station to gate based on travel progress.
                    spawnPos = stationPos.Lerp(gatePos, Mathf.Clamp(travelProgress * 2f, 0f, 1f));
                    targetPos = gatePos;
                }
                else
                {
                    // Fleet arriving from another system — queue for staggered warp-in.
                    var sourceNeighbor = !string.IsNullOrEmpty(currentNodeId) ? currentNodeId : "";
                    var gatePos = !string.IsNullOrEmpty(sourceNeighbor)
                        ? GetGateLocalPositionForNeighborV0(sourceNeighbor)
                        : Vector3.Zero;
                    if (gatePos == Vector3.Zero)
                        gatePos = DeriveOrbitPositionV0(id + "_arrive", LaneGateDistanceU);
                    var orbitPos = DeriveOrbitPositionV0(id + "_local", arrOrbit);

                    if (travelProgress > 0.7f)
                    {
                        // Already well into system — spawn immediately.
                        float arrivalT = Mathf.Clamp((travelProgress - 0.5f) * 2f, 0f, 1f);
                        spawnPos = gatePos.Lerp(orbitPos, arrivalT);
                        targetPos = orbitPos;
                    }
                    else
                    {
                        // Queue for staggered warp-in at gate.
                        ship.QueueFree();
                        _arrivalQueue.Enqueue((id, f, gatePos, orbitPos, arrSpd));
                        continue;
                    }
                    playWarpIn = false; // Handled by queue system now.
                }

                var arrTask = f.ContainsKey("current_task") ? (string)f["current_task"] : "Idle";
                var arrFinalDest = f.ContainsKey("final_destination_node_id") ? (string)f["final_destination_node_id"] : "";
                var arrDest = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                ship.Position = spawnPos;
                ship.AddToGroup("FleetShip");
                _localSystemRoot.AddChild(ship);

                if (playWarpIn && warpScript != null)
                    warpScript.Call("play_warp_in", _localSystemRoot, spawnPos);

                if (ship.HasMethod("update_transit"))
                    ship.Call("update_transit", f);

                // Check if this newly-spawned ship is departing — mark it so it warps out at gate.
                var spawnFleetState = f.ContainsKey("state") ? (string)f["state"] : "Idle";
                var spawnDest = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                bool spawnDeparting = atThisNode && spawnFleetState == "Traveling" && !string.IsNullOrEmpty(spawnDest)
                    && !StringComparer.Ordinal.Equals(spawnDest, _currentLocalNodeId);
                if (spawnDeparting && ship.HasMethod("begin_departure_v0"))
                    ship.Call("begin_departure_v0", targetPos);
                else if (atThisNode && spawnFleetState != "Traveling" && ship.HasMethod("set_orbit_v0"))
                {
                    float orbitAngularSpeed = KeplerOrbitSpeed(arrOrbit, KeplerK_Planet) * 0.5f;
                    ship.Call("set_orbit_v0", arrOrbit, orbitAngularSpeed);
                }
                else if (ship.HasMethod("set_target"))
                    ship.Call("set_target", targetPos, arrSpd);
            }
            else if (existingNodes.TryGetValue(id, out var existingNode))
            {
                // Update movement target for existing ships based on transit state.
                var state = f.ContainsKey("state") ? (string)f["state"] : "Idle";
                var destNodeId = f.ContainsKey("destination_node_id") ? (string)f["destination_node_id"] : "";
                var finalDestId = f.ContainsKey("final_destination_node_id") ? (string)f["final_destination_node_id"] : "";
                int role = f.ContainsKey("role") ? (int)f["role"] : 0;

                if (existingNode.HasMethod("update_transit"))
                    existingNode.Call("update_transit", f);

                var currentTask = f.ContainsKey("current_task") ? (string)f["current_task"] : "Idle";

                if (existingNode.HasMethod("set_target"))
                {
                    Vector3 target;
                    float speed;
                    string reason;

                    if (state == "Traveling" && !string.IsNullOrEmpty(destNodeId)
                        && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId))
                    {
                        // Fleet leaving this system: fly to the departure gate and warp out.
                        target = GetGateLocalPositionForNeighborV0(destNodeId);
                        if (target == Vector3.Zero)
                            target = DeriveOrbitPositionV0(id + "_depart", LaneGateDistanceU);
                        speed = role == 2 ? 8.0f : 6.0f;
                        reason = $"departing→{destNodeId}";
                        // Mark as departing so NPC warps out at gate.
                        if (existingNode.HasMethod("begin_departure_v0"))
                            existingNode.Call("begin_departure_v0", target);
                    }
                    else if (!string.IsNullOrEmpty(finalDestId)
                        && !StringComparer.Ordinal.Equals(finalDestId, _currentLocalNodeId))
                    {
                        // Fleet has a destination queued (about to depart): fly toward departure gate.
                        var gateNeighbor = !string.IsNullOrEmpty(destNodeId)
                            && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId)
                            ? destNodeId : finalDestId;
                        target = GetGateLocalPositionForNeighborV0(gateNeighbor);
                        if (target == Vector3.Zero)
                        {
                            target = DeriveOrbitPositionV0(id + "_hold", role == 2 ? 50.0f : 20.0f);
                            speed = 2.0f;
                            reason = $"no_gate_for_queued→{gateNeighbor}";
                        }
                        else
                        {
                            speed = role == 2 ? 8.0f : 5.0f;
                            reason = $"queued→{finalDestId}";
                        }
                    }
                    else if (!string.IsNullOrEmpty(destNodeId)
                        && !StringComparer.Ordinal.Equals(destNodeId, _currentLocalNodeId))
                    {
                        // Fleet idle but has a destination at a DIFFERENT node — fly toward departure gate.
                        target = GetGateLocalPositionForNeighborV0(destNodeId);
                        if (target == Vector3.Zero)
                        {
                            // No gate for this neighbor — hold near station instead.
                            target = DeriveOrbitPositionV0(id + "_hold", role == 2 ? 50.0f : 20.0f);
                            speed = 2.0f;
                            reason = $"no_gate_for→{destNodeId}";
                        }
                        else
                        {
                            speed = role == 2 ? 6.0f : role == 1 ? 4.0f : 4.5f;
                            reason = $"heading_to_gate→{destNodeId}";
                        }
                    }
                    else
                    {
                        // Truly idle, no destination or dest is current node — hold position near station.
                        target = DeriveOrbitPositionV0(id + "_hold", role == 2 ? 50.0f : 20.0f);
                        speed = 2.0f;
                        reason = "station_hold";
                    }
                    existingNode.Call("set_target", target, speed);
                    // Debug logging removed.
                }
            }
        }
    }

    // Spawn a single queued arrival: create ship at gate, play warp-in VFX, fly inward.
    private void SpawnQueuedArrivalV0(string fleetId, Godot.Collections.Dictionary data, Vector3 gatePos, Vector3 orbitPos, float speed)
    {
        if (_localSystemRoot == null) return;

        // Check if this fleet is still relevant (might have left by the time we process the queue).
        var transitFacts = _bridge?.GetFleetTransitFactsV0(_currentLocalNodeId ?? "");
        if (transitFacts != null)
        {
            bool stillPresent = false;
            for (int i = 0; i < transitFacts.Count; i++)
            {
                var tf = transitFacts[i].AsGodotDictionary();
                if (tf.ContainsKey("fleet_id") && (string)tf["fleet_id"] == fleetId)
                {
                    stillPresent = true;
                    break;
                }
            }
            if (!stillPresent)
            {
                return;
            }
        }

        // Check if ship already exists in scene.
        foreach (Node child in _localSystemRoot.GetChildren())
        {
            if (child is Node3D n3d && n3d.IsInGroup("FleetShip") && n3d.Name.ToString() == "Fleet_" + fleetId)
                return; // Already spawned.
        }

        var ship = SpawnNpcShipV0(fleetId);
        if (ship == null) return;

        ship.Position = gatePos;
        ship.AddToGroup("FleetShip");
        _localSystemRoot.AddChild(ship);

        // Play warp-in VFX at gate.
        var warpScript = GD.Load<Script>("res://scripts/vfx/warp_effect.gd");
        if (warpScript != null)
            warpScript.Call("play_warp_in", _localSystemRoot, gatePos);

        if (ship.HasMethod("update_transit"))
            ship.Call("update_transit", data);
        if (ship.HasMethod("set_target"))
            ship.Call("set_target", orbitPos, speed);
    }

    // Spawn a deferred fleet ship — called 1 per frame from _Process to avoid FPS spikes.
    private void SpawnDeferredFleetV0(string fleetId, Vector3 spawnPos, Vector3 targetPos, float orbitRadius, float orbitSpeed, bool isDeparting, Godot.Collections.Dictionary data)
    {
        if (_localSystemRoot == null) return;

        // Check if ship already exists.
        foreach (Node child in _localSystemRoot.GetChildren())
        {
            if (child is Node3D n3d && n3d.IsInGroup("FleetShip") && n3d.Name.ToString() == "Fleet_" + fleetId)
                return;
        }

        var ship = SpawnNpcShipV0(fleetId);
        if (ship == null) return;

        ship.Position = spawnPos;
        ship.AddToGroup("FleetShip");
        _localSystemRoot.AddChild(ship);

        if (ship.HasMethod("update_transit"))
            ship.Call("update_transit", data);

        if (isDeparting && ship.HasMethod("begin_departure_v0"))
            ship.Call("begin_departure_v0", targetPos);
        else if (orbitSpeed > 0f && ship.HasMethod("set_orbit_v0"))
            ship.Call("set_orbit_v0", orbitRadius, orbitSpeed);
        else if (ship.HasMethod("set_target"))
            ship.Call("set_target", targetPos, orbitSpeed > 0f ? 5.0f : 5.0f);
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

    // Find the nearest lane gate position to the given local-space position.
    private Vector3 FindNearestGateLocalPositionV0(Vector3 fromPos)
    {
        if (_localSystemRoot == null) return new Vector3(LaneGateDistanceU, 0, 0);
        float bestDist = float.MaxValue;
        Vector3 bestPos = new Vector3(LaneGateDistanceU, 0, 0);
        foreach (var child in _localSystemRoot.GetChildren())
        {
            if (child is not Node3D n3d || !n3d.IsInGroup("LaneGate")) continue;
            float dist = n3d.Position.DistanceTo(fromPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = n3d.Position;
            }
        }
        return bestPos;
    }

    // GATE.S12.FLEET_SUBSTANCE.QUATERNIUS.001: Procedural ship model by FleetRole via ShipMeshBuilder.
    // GATE.S12.FLEET_SUBSTANCE.VARIETY.001: Hash-based model variants + player ship.
    private static GDScript _shipMeshBuilderScript;

    private Node3D LoadFleetModelV0(string fleetId)
    {
        _shipMeshBuilderScript ??= GD.Load<GDScript>("res://scripts/view/ship_mesh_builder.gd");
        if (_shipMeshBuilderScript == null) return null;

        int roleInt;
        if (StringComparer.Ordinal.Equals(fleetId, "fleet_trader_1"))
            roleInt = -1; // Player ship
        else
            roleInt = (_bridge != null) ? _bridge.GetFleetRoleV0(fleetId) : 0;

        uint hash = 0;
        foreach (char c in fleetId) { hash = hash * 31 + (uint)c; }

        var model = (Node3D)_shipMeshBuilderScript.Call("build_ship", roleInt, Colors.White, (int)hash);
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

        var ship = NpcShipScene.Instantiate<Node3D>();
        ship.Name = "Fleet_" + fleetId;
        ship.Set("fleet_id", fleetId);

        // Binary exclusion zone: tell NPC ships how far to stay from origin in binary systems.
        if (_currentSystemIsBinary)
            ship.Set("binary_exclusion_zone", _binarySeparation * 0.7f + 10.0f); // ~45u for ClassG

        // Build procedural ship model by fleet role.
        int roleInt = (_bridge != null) ? _bridge.GetFleetRoleV0(fleetId) : 0;
        if (ship.HasMethod("load_model_v1"))
            ship.Call("load_model_v1", roleInt);

        // Set hostile meta — default non-hostile; npc_ship.gd resolves from reputation.
        ship.SetMeta("fleet_id", fleetId);
        ship.SetMeta("is_hostile", false);

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

    private Node3D CreateFleetMarkerV0(string fleetId)
    {
        var root = new Node3D { Name = "Fleet_" + fleetId };

        // GATE.S12.FLEET_SUBSTANCE.QUATERNIUS.001: Procedural model by FleetRole.
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

        // NPC overhead HP bar — hull (green-to-red) + shield (cyan).
        // Starts hidden; fleet_ai.gd shows during combat/engage state.
        var hpBarLabel = new Label3D
        {
            Name = "HpBar",
            Text = "",
            PixelSize = 0.08f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(1.0f, 1.0f, 1.0f, 0.9f),
            OutlineModulate = new Color(0f, 0f, 0f, 0.8f),
            OutlineSize = 12,
            FontSize = 48,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            NoDepthTest = true,
            Visible = false,
        };
        hpBarLabel.Position = new Vector3(0f, FleetMarkerRadiusU * 2.0f + 1.5f, 0f);
        root.AddChild(hpBarLabel);

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
        // Default non-hostile; npc_ship.gd resolves hostility from faction reputation.
        var fleetAiScript = GD.Load<Script>("res://scripts/core/fleet_ai.gd");
        if (fleetAiScript != null)
        {
            root.SetScript(fleetAiScript);
            root.SetMeta("is_hostile", false);
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
        float radius = StarVisualRadiusU * classScale;

        var container = new Node3D { Name = "LocalStar" };
        container.Position = Vector3.Zero;

        // Seed for per-star noise variation.
        var seedHash = Fnv1a64((_currentNodeId ?? "star") + "_star_seed");
        float seedOffset = (float)(seedHash % 100UL) * 0.37f;

        // ── Photosphere: procedural surface shader on a sphere ──
        var bodySphere = new SphereMesh
        {
            Radius = radius, Height = radius * 2.0f,
            RadialSegments = 48, Rings = 32,
        };
        var bodyMI = new MeshInstance3D
        {
            Name = "StarBody",
            Mesh = bodySphere,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        var surfaceShader = GD.Load<Shader>("res://scripts/vfx/star_surface.gdshader");
        if (surfaceShader != null)
        {
            var mat = new ShaderMaterial { Shader = surfaceShader };
            // Per-class color ramp: center(white) → mid(yellow) → limb(orange/red).
            var (center, mid, limb) = StarClassDiskColorsV0(starColor, starClass);
            mat.SetShaderParameter("color_center", center);
            mat.SetShaderParameter("color_mid", mid);
            mat.SetShaderParameter("color_limb", limb);
            // Per-class emission: hotter stars are brighter.
            mat.SetShaderParameter("emission_peak", StarClassEmissionPeakV0(starClass));
            // Vary granule density per star — readable convection cells, not subpixel noise.
            mat.SetShaderParameter("granule_scale", 18.0f + (float)(seedHash % 8UL));
            bodyMI.MaterialOverride = mat;
        }
        else
        {
            GD.PrintErr("STAR_SHADER_MISSING: star_surface.gdshader not found!");
            bodyMI.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = starColor,
                EmissionEnabled = true,
                Emission = starColor,
                EmissionEnergyMultiplier = 2.0f
            };
        }
        container.AddChild(bodyMI);

        // Corona removed — the surface shader's Fresnel rim glow + WorldEnvironment
        // bloom handle the star halo. A separate geometry sphere creates an ugly
        // "atmosphere ring" artifact that reads as a planet, not a star.

        // ── Spinning rotation for surface animation variety ──
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null)
        {
            container.SetScript(spinScript);
            container.Set("spin_speed_y", 0.02f); // Very slow rotation.
        }

        // Add a point light so the star actually illuminates ships/stations/planets.
        var starLight = new OmniLight3D
        {
            Name = "StarLight",
            LightColor = new Color(
                Mathf.Min(starColor.R * 0.8f + 0.2f, 1.0f),
                Mathf.Min(starColor.G * 0.8f + 0.2f, 1.0f),
                Mathf.Min(starColor.B * 0.8f + 0.2f, 1.0f)),
            LightEnergy = 8.0f * classScale,
            OmniRange = 300.0f,
            OmniAttenuation = 0.5f,
            ShadowEnabled = false,
        };
        // Light at world origin (star center), not inside the scaled container.
        _localSystemRoot.CallDeferred("add_child", starLight);

        return container;
    }

    // Per-class disk color ramp: center → mid → limb.
    // Derived from blackbody radiation (Tanner Helland algorithm), with saturation
    // boosted for SDO/H-alpha dramatic aesthetic. Limb darkening makes edges ~2000K cooler.
    // O: 40000K blue-white, A: 10000K pale blue, F: 7500K yellow-white,
    // G: 5800K warm yellow, K: 4000K orange, M: 3000K deep red-orange.
    private static (Color center, Color mid, Color limb) StarClassDiskColorsV0(Color starColor, string starClass) => starClass switch
    {
        "ClassO" => (new Color(0.65f, 0.78f, 1.0f),   // 40000K blue-white center
                     new Color(0.45f, 0.60f, 1.0f),   // 25000K blue mid
                     new Color(0.30f, 0.50f, 0.95f)),  // 15000K deep blue limb
        "ClassA" => (new Color(0.82f, 0.85f, 1.0f),   // 10000K pale blue-white center
                     new Color(0.75f, 0.75f, 0.95f),  // 8000K lavender mid
                     new Color(0.60f, 0.60f, 0.85f)),  // 6000K blue-gray limb
        "ClassF" => (new Color(1.0f, 0.90f, 0.75f),   // 7500K warm white center
                     new Color(1.0f, 0.78f, 0.50f),   // 6000K warm yellow mid
                     new Color(1.0f, 0.60f, 0.25f)),   // 4500K orange limb
        "ClassG" => (new Color(1.0f, 0.85f, 0.50f),   // 5800K bright warm yellow center
                     new Color(1.0f, 0.60f, 0.18f),   // 4500K deep orange mid
                     new Color(0.90f, 0.30f, 0.06f)),  // 3500K deep red-orange limb
        "ClassK" => (new Color(1.0f, 0.70f, 0.25f),   // 4000K orange center
                     new Color(1.0f, 0.50f, 0.10f),   // 3200K deep orange mid
                     new Color(0.85f, 0.25f, 0.04f)),  // 2500K red limb
        "ClassM" => (new Color(1.0f, 0.55f, 0.12f),   // 3000K deep orange center
                     new Color(0.90f, 0.35f, 0.05f),  // 2500K red-orange mid
                     new Color(0.70f, 0.15f, 0.02f)),  // 2000K deep red limb
        _ =>        (new Color(1.0f, 0.85f, 0.50f),
                     new Color(1.0f, 0.60f, 0.18f),
                     new Color(0.90f, 0.30f, 0.06f)),
    };

    // Per-class emission peak: tuned for ACES filmic tonemapping (tonemap_white=6.0).
    // Values above ~4.0 enter the ACES desaturation zone → gray instead of white.
    // Keep center in sweet spot; Fresnel corona (fresnel_glow=1.5) creates bloom halo.
    private static float StarClassEmissionPeakV0(string starClass) => starClass switch
    {
        "ClassO" => 5.0f,   // Blazing blue — just under ACES gray-out
        "ClassA" => 4.5f,   // Brilliant white
        "ClassF" => 3.8f,   // Warm white
        "ClassG" => 3.2f,   // Sol — warm white center, Fresnel does the bloom
        "ClassK" => 2.5f,   // Subdued orange
        "ClassM" => 1.8f,   // Dim red dwarf — brooding
        _ => 3.2f,
    };

    // Enhanced visual scale range for more dramatic star class differences.
    private static float StarClassVisualScaleV0(string starClass) => starClass switch
    {
        "ClassO" => 2.0f,   // Blue giant — imposing
        "ClassA" => 1.4f,   // White — large
        "ClassF" => 1.15f,  // White-yellow
        "ClassG" => 1.0f,   // Sol baseline
        "ClassK" => 0.8f,   // Orange — compact
        "ClassM" => 0.55f,  // Red dwarf — small and dim
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

    // TintStarShaderV0 removed — procedural star shaders accept star_color directly.

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

    // Per-system hue tinting: HSV-space rotation from node ID hash.
    // Additive RGB on dark ambient colors was invisible — HSV rotation works
    // regardless of brightness because it shifts the hue angle directly.
    private static Color ApplySystemHueTintV0(Color baseColor, string nodeId)
    {
        var hash = Fnv1a64(nodeId + "_hue_tint");
        // ±0.10 hue rotation (±36° of 360°) — clearly visible color temperature shift.
        float hueShift = ((float)(hash % 200UL) - 100.0f) / 1000.0f; // ±0.10
        // ±15% saturation boost/cut — makes some systems more vivid, others more muted.
        float satMul = 1.0f + ((float)((hash >> 8) % 30UL) - 15.0f) / 100.0f; // 0.85–1.15

        float h = baseColor.H + hueShift;
        if (h < 0f) h += 1f;
        if (h > 1f) h -= 1f;
        float s = Mathf.Clamp(baseColor.S * satMul, 0f, 1f);
        return Color.FromHsv(h, s, baseColor.V, baseColor.A);
    }

    // VISUAL_OVERHAUL: Star-class ambient light override — each system has a distinct color mood.
    // Per-system hue tint applied so no two systems of same class look identical.
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
        col = ApplySystemHueTintV0(col, _currentNodeId ?? "default");
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
        "Lava"        => 45.0f,   // Innermost — volcanic, near star
        "Sand"        => 55.0f,   // Inner warm zone
        "Terrestrial" => 65.0f,   // Habitable zone
        "Barren"      => 75.0f,   // Outer rocky
        "Ice"         => 88.0f,   // Outer cold zone
        "Gaseous"     => 105.0f,  // Far out — gas giant
        _             => 65.0f,
    };

    // Kepler orbital speed: ω = K / r^1.5. Higher K = faster orbits.
    private const float KeplerK_Planet = 42.0f; // STRUCTURAL: tuned so Terrestrial@65u ≈ 0.08 rad/s
    private const float KeplerK_Moon = 4.0f;    // STRUCTURAL: tuned so moon@9u ≈ 0.15 rad/s
    private static float KeplerOrbitSpeed(float radius, float k) =>
        Mathf.Clamp(k / MathF.Pow(radius, 1.5f), 0.01f, 0.20f);

    // Planet visual scale by type. Star is ~6u radius, largest planet ~4u (70% of star).
    // Addon scenes have ~400u baked scale, so 0.01 → ~4u visible radius.
    // VISUAL_OVERHAUL: Increased ~25% for better visibility from camera altitude.
    private static float PlanetVisualScaleV0(string planetType) => planetType switch
    {
        "Gaseous"     => 0.022f,   // ~8.8u — imposing gas giant
        "Terrestrial" => 0.017f,   // ~6.8u
        "Ice"         => 0.015f,   // ~6.0u
        "Sand"        => 0.015f,   // ~6.0u
        "Lava"        => 0.014f,   // ~5.6u
        "Barren"      => 0.012f,   // ~4.8u
        _             => 0.017f,
    };

    // Binary star companion — ~20% of systems are binaries (seeded).
    // Spawn single, binary (20%), or trinary (5%) star system with mutual orbits.
    // Returns the root anchor node (may be a barycenter pivot for multi-star systems).
    // Kepler constant for binary mutual orbit speed (rad/s = K / sep^1.5).
    private const float KeplerK_Binary = 3.5f;

    private Node3D SpawnStarSystemV0(string nodeId, Color starColor, string starClass)
    {
        var primary = CreateStarMeshV0(starColor, starClass);
        primary.Name = "PrimaryStar";

        var hash = Fnv1a64(nodeId + "_binary");
        bool isBinary = hash % 100UL < 20; // 20% binary
        if (!isBinary)
        {
            // Solo star: planets must clear the star's visual edge + 10u margin.
            float primaryRadius = StarVisualRadiusU * StarClassVisualScaleV0(starClass);
            _minPlanetOrbitRadius = primaryRadius + 10.0f;
            return primary;
        }

        // Binary: create barycenter pivot for mutual orbit.
        float classScl = StarClassVisualScaleV0(starClass);
        float separation = StarVisualRadiusU * classScl * 2.5f; // Holman-Wiegert: 2.5x star radius
        const float massRatio = 0.3f; // STRUCTURAL: companion is 30% mass of primary

        // Publish binary state for planet/fleet orbit scaling.
        _currentSystemIsBinary = true;
        _binarySeparation = separation;
        _binaryPlanetScaleFactor = 1.6f; // Holman-Wiegert stability compression

        var companionColor = new Color(
            Mathf.Min(starColor.R * 1.1f, 1.0f),
            starColor.G * 0.6f,
            starColor.B * 0.4f);
        var companion = CreateStarMeshV0(companionColor, starClass, 0.5f);
        companion.Name = "BinaryCompanion";

        var barycenter = new Node3D { Name = "BinaryBarycenter" };
        var orbitSpin = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (orbitSpin != null)
        {
            barycenter.SetScript(orbitSpin);
            // Kepler-derived binary orbit speed: visible arc from camera altitude.
            float binaryOrbitSpeed = Mathf.Clamp(KeplerK_Binary / MathF.Pow(separation, 1.5f), 0.005f, 0.04f);
            barycenter.Set("spin_speed_y", binaryOrbitSpeed);
            barycenter.Set("pause_when_docked", true);
        }

        // Primary offset from barycenter (toward companion side, small).
        float angle = (float)(hash % 360UL) * Mathf.DegToRad(1.0f);
        float primaryOff = separation * massRatio;
        primary.Position = new Vector3(MathF.Cos(angle) * -primaryOff, 0f, MathF.Sin(angle) * -primaryOff);
        companion.Position = new Vector3(MathF.Cos(angle) * (separation - primaryOff), 0f,
                                          MathF.Sin(angle) * (separation - primaryOff));
        barycenter.AddChild(primary);
        barycenter.AddChild(companion);

        // Binary minimum orbit: companion edge + 15u safety margin.
        float companionRadius = StarVisualRadiusU * classScl * 0.5f;
        float companionEdge = (separation - primaryOff) + companionRadius;
        _minPlanetOrbitRadius = companionEdge + 15.0f;

        // Trinary: 25% of binaries also get a C star (5% total).
        // Hierarchical stability: C star at >3× AB separation (Alpha Centauri architecture).
        var triHash = Fnv1a64(nodeId + "_trinary");
        if (triHash % 100UL < 25)
        {
            float cSeparation = separation * 3.0f + (float)(triHash % 30UL); // 3× AB + jitter
            var cColor = new Color(
                Mathf.Min(starColor.R * 1.05f, 1.0f),
                starColor.G * 0.8f,
                starColor.B * 0.5f);
            var cStar = CreateStarMeshV0(cColor, starClass, 0.35f);
            cStar.Name = "TrinaryStar";

            // Outer orbit pivot for C around AB barycenter.
            var outerPivot = new Node3D { Name = "TrinaryOuterPivot" };
            if (orbitSpin != null)
            {
                outerPivot.SetScript(orbitSpin);
                float cOrbitSpeed = Mathf.Clamp(KeplerK_Binary / MathF.Pow(cSeparation, 1.5f), 0.001f, 0.01f);
                outerPivot.Set("spin_speed_y", cOrbitSpeed);
                outerPivot.Set("pause_when_docked", true);
            }
            float cAngle = (float)(triHash % 360UL) * Mathf.DegToRad(1.0f);
            cStar.Position = new Vector3(MathF.Cos(cAngle) * cSeparation, 0f,
                                          MathF.Sin(cAngle) * cSeparation);
            outerPivot.AddChild(cStar);

            // Trinary: planets must clear the C star orbit + C star radius + margin.
            float cRadius = StarVisualRadiusU * classScl * 0.35f;
            _minPlanetOrbitRadius = cSeparation + cRadius + 15.0f;

            var root = new Node3D { Name = "TrinarySystem" };
            root.AddChild(barycenter);
            root.AddChild(outerPivot);
            return root;
        }

        return barycenter;
    }

    // Ensure addon scene AnimationTree is active so planets/stars rotate.
    // ActivateAnimationTreeV0 removed — procedural stars animate via shader TIME.

    // Set SphereMesh resolution appropriate for viewing distance (~80u top-down camera).
    // 24/24 segments looks perfectly smooth from that distance (1,152 tris vs 8,192 at 128/64).
    private static void UpgradePlanetMeshResolutionV0(Node3D root)
    {
        foreach (var child in root.FindChildren("*", "MeshInstance3D"))
        {
            if (child is MeshInstance3D meshInst && meshInst.Mesh is SphereMesh sphere)
            {
                sphere.RadialSegments = 24;
                sphere.Rings = 24;
            }
        }
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

        var moonSpin = GD.Load<Script>("res://scripts/spinning_node.gd");

        for (int i = 0; i < count; i++)
        {
            var moonHash = Fnv1a64(nodeId + "_moon_" + i);
            float moonOrbit = 7.0f + (float)(moonHash % 5UL); // 7-11u from planet
            var moonOffset = DeriveOrbitPositionV0(nodeId + "_moon_" + i, moonOrbit);

            // Procedural barren moon (low-poly, no atmosphere).
            Node3D moonNode = CreateProceduralPlanetV0("Barren", nodeId + "_moon_" + i);

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
                float orbitSpeed = KeplerOrbitSpeed(moonOrbit, KeplerK_Moon);
                moonOrbitPivot.Set("spin_speed_y", orbitSpeed);
            }
            moonOrbitPivot.AddChild(container);

            // Add to planet orbit pivot so moons follow the planet around the star.
            if (planetOrbitPivot != null)
                planetOrbitPivot.AddChild(moonOrbitPivot);
            else if (_currentSolarTilt != null)
                _currentSolarTilt.AddChild(moonOrbitPivot);
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
        if (hash % 100UL >= 60) return; // ~60% of systems have a belt

        float beltRadiusBase = _currentSystemIsBinary ? 155.0f : 120.0f;
        float beltRadius = MathF.Max(beltRadiusBase * lumScale, _currentSystemIsBinary ? 130.0f : 100.0f);

        // Richness tiers: sparse / normal / dense — vary rock count and band width.
        ulong richnessRoll = hash % 100UL;
        int rockCount;
        float bandWidth;
        if (richnessRoll < 12) // 12/60 ≈ 20% sparse
        {
            rockCount = 25 + (int)((hash >> 8) % 15UL);  // 25-39
            bandWidth = 6.0f;
        }
        else if (richnessRoll < 42) // 30/60 ≈ 50% normal
        {
            rockCount = 55 + (int)((hash >> 8) % 30UL);  // 55-84
            bandWidth = 12.0f;
        }
        else // 18/60 ≈ 30% dense
        {
            rockCount = 95 + (int)((hash >> 8) % 40UL);  // 95-134
            bandWidth = 18.0f;
        }

        // GPU-driven MultiMesh: 1 draw call for all rocks, orbital animation in vertex shader.
        var rockMesh = new SphereMesh
        {
            Radius = 1.0f, Height = 1.4f, // Slightly oblate; scale per-instance.
            RadialSegments = 8, Rings = 6, // Low-poly is fine for rocks.
        };

        var mm = new MultiMesh();
        mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        mm.UseCustomData = true; // MUST be set before InstanceCount.
        mm.UseColors = false;
        mm.InstanceCount = rockCount;
        mm.Mesh = rockMesh;

        float baseOrbitalSpeed = KeplerOrbitSpeed(beltRadius, KeplerK_Planet);

        for (int i = 0; i < rockCount; i++)
        {
            var rockHash = Fnv1a64(nodeId + "_rock_" + i);
            float angle = ((float)i / rockCount) * 2.0f * MathF.PI;
            float rJitter = beltRadius + ((float)(rockHash % (ulong)(bandWidth * 2)) - bandWidth);
            float yJitter = ((float)(rockHash % 9UL) - 4.0f) * 1.2f; // ±4.8u vertical

            // Continuous size distribution: many small, few large.
            ulong sizeRoll = (rockHash >> 12) % 100UL;
            float rockSize;
            if (sizeRoll < 50)      // 50% small
                rockSize = 0.5f + (float)((rockHash >> 20) % 15UL) * 0.1f;  // 0.5-2.0u
            else if (sizeRoll < 80) // 30% medium
                rockSize = 2.0f + (float)((rockHash >> 20) % 20UL) * 0.1f;  // 2.0-4.0u
            else                     // 20% large
                rockSize = 4.0f + (float)((rockHash >> 20) % 30UL) * 0.1f;  // 4.0-7.0u

            // Material index: 0-4 normal, 5-9 ore vein.
            int matIdx = (int)((rockHash >> 4) % 5UL);
            if (matIdx == 3 && beltRadius < 110.0f) matIdx = 0; // No icy in inner belts

            int oreChance = matIdx switch { 2 => 20, 4 => 20, 1 => 15, 3 => 5, _ => 8 };
            bool hasOre = (int)(rockHash % 100UL) < oreChance;
            int shaderMatIdx = hasOre ? matIdx + 5 : matIdx;

            // Random rotation for rock tumble.
            float rx = (float)(rockHash % 360UL) * (MathF.PI / 180f);
            float ry = (float)((rockHash >> 8) % 360UL) * (MathF.PI / 180f);

            var t = Transform3D.Identity
                .Scaled(Vector3.One * rockSize * 0.5f)
                .Rotated(Vector3.Up, ry)
                .Rotated(Vector3.Right, rx);
            // Initial position (shader will animate orbit from INSTANCE_CUSTOM).
            t.Origin = new Vector3(MathF.Cos(angle) * rJitter, yJitter, MathF.Sin(angle) * rJitter);
            mm.SetInstanceTransform(i, t);

            // Pack orbital params: .x=radius, .y=speed, .z=phase, .w=packed(y_offset + matIdx).
            float perturbation = 1.0f + ((float)(rockHash % 20UL) - 10.0f) * 0.01f;
            float speed = baseOrbitalSpeed * perturbation;
            // Pack .w: integer part = y_offset * 10 (rounded), fractional = matIdx / 10.
            float packedW = MathF.Round(yJitter * 10.0f) + shaderMatIdx * 0.1f;
            mm.SetInstanceCustomData(i, new Color(rJitter, speed, angle, packedW));
        }

        // Load belt shader.
        var beltShader = GD.Load<Shader>("res://scripts/vfx/asteroid_belt.gdshader");
        ShaderMaterial beltMat;
        if (beltShader != null)
        {
            beltMat = new ShaderMaterial { Shader = beltShader };
        }
        else
        {
            // Fallback: plain gray material.
            beltMat = null;
        }

        var mmInstance = new MultiMeshInstance3D
        {
            Name = "AsteroidBelt",
            Multimesh = mm,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        if (beltMat != null)
            mmInstance.MaterialOverride = beltMat;

        // AABB must cover entire orbit range so frustum culling works with shader animation.
        float maxR = beltRadius + bandWidth + 10.0f;
        mmInstance.CustomAabb = new Aabb(
            new Vector3(-maxR, -8f, -maxR),
            new Vector3(maxR * 2f, 16f, maxR * 2f));

        if (_currentSolarTilt != null)
            _currentSolarTilt.AddChild(mmInstance);
        else
            _localSystemRoot.AddChild(mmInstance);
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

        // Procedural planet: low-poly sphere + surface shader + atmosphere halo.
        // Falls back to addon scene if shader fails to load.
        Node3D planetNode = CreateProceduralPlanetV0(planetType, nodeId);


        // Orbit radius: base distance * luminosity scale + seed jitter (±1.5u).
        // Binary systems: ×1.6 factor pushes planets outside Holman-Wiegert stability radius.
        float baseOrbit = PlanetBaseOrbitV0(planetType);
        var jitterHash = Fnv1a64(nodeId + "_orbit_jitter");
        float jitter = ((float)(jitterHash % 30UL) - 15.0f) * 0.1f; // ±1.5u
        float orbitRadius = (baseOrbit * lumScale + jitter) * _binaryPlanetScaleFactor;
        // Clamp: planet must orbit beyond all stars in the system (binary/trinary safe).
        if (orbitRadius < _minPlanetOrbitRadius)
            orbitRadius = _minPlanetOrbitRadius;

        // Visual scale varies by planet type (gas giants bigger).
        // Canonical planet gets 1.4x scale for clear size hierarchy over outer planets.
        float vScale = PlanetVisualScaleV0(planetType) * 1.4f;

        // Orbital motion: pivot at star center rotates slowly, planet child orbits.
        var orbitPivot = new Node3D { Name = "PlanetOrbitPivot" };
        var orbitSpin = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (orbitSpin != null)
        {
            orbitPivot.SetScript(orbitSpin);
            float orbitSpeed = KeplerOrbitSpeed(orbitRadius, KeplerK_Planet);
            orbitPivot.Set("spin_speed_y", orbitSpeed);
        }

        var container = new Node3D { Name = "LocalPlanet" };
        container.Scale = new Vector3(vScale, vScale, vScale);
        var planetOrbitPos = DeriveOrbitPositionV0(nodeId + "_planet", orbitRadius);
        container.Position = planetOrbitPos;
        container.AddChild(planetNode);

        // Avoidance metadata: ships use this to Y-lift over planets.
        float visualRadius = vScale * 400.0f; // Addon scenes have ~400u baked scale.
        container.SetMeta("avoidance_radius", (double)(visualRadius + 5.0f));
        container.SetMeta("visual_radius", (double)visualRadius);
        container.AddToGroup("PlanetBody"); // All planets (landable or not) for ship avoidance.

        // AtmosphereGlow sphere removed — was placeholder programmer art.

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

            // Planet name label removed — no floating text in space.

            orbitPivot.AddChild(dockArea);
        }

        if (_currentSolarTilt != null)
            _currentSolarTilt.AddChild(orbitPivot);
        else
            _localSystemRoot.AddChild(orbitPivot);
        return (planetOrbitPos, planetType, orbitPivot);
    }

    // Visual-only outer planets — no collision, no dock, no labels. Add system depth.
    private static readonly string[] OuterPlanetPool = { "Barren", "Ice", "Gaseous", "Sand", "Terrestrial" };
    private void SpawnOuterPlanetsV0(string nodeId, float lumScale, string canonicalType)
    {
        var hash = Fnv1a64(nodeId + "_outer_planets");
        int count = 1 + (int)(hash % 2UL); // 1-2 outer planets

        float canonicalOrbit = PlanetBaseOrbitV0(canonicalType) * lumScale;
        var orbitSpin = GD.Load<Script>("res://scripts/spinning_node.gd");

        var usedTypes = new HashSet<string> { canonicalType };
        for (int i = 0; i < count; i++)
        {
            var pH = Fnv1a64(nodeId + "_outer_" + i);
            // Pick type, skipping canonical AND previously-used types to avoid visual duplicates.
            string outerType = OuterPlanetPool[(int)(pH % (ulong)OuterPlanetPool.Length)];
            int attempts = 0; // STRUCTURAL: loop guard
            while (usedTypes.Contains(outerType) && attempts < OuterPlanetPool.Length)
            {
                outerType = OuterPlanetPool[(int)((pH + (ulong)++attempts) % (ulong)OuterPlanetPool.Length)];
            }
            usedTypes.Add(outerType);

            // Phi-ratio spacing: golden ratio progression for naturalistic orbital gaps.
            float phi = 1.618f;
            float gap = 20.0f * MathF.Pow(phi, i); // ~20u, ~32u, ~52u...
            float orbitRadius = (canonicalOrbit + gap + ((float)(pH % 6UL) - 3.0f)) * _binaryPlanetScaleFactor;
            // Clamp: outer planets must also clear all stars in the system.
            if (orbitRadius < _minPlanetOrbitRadius)
                orbitRadius = _minPlanetOrbitRadius + gap;
            float vScale = PlanetVisualScaleV0(outerType);

            Node3D planetNode = CreateProceduralPlanetV0(outerType, nodeId + "_outer_" + i);

            var container = new Node3D { Name = "OuterPlanet_" + i };
            container.Scale = new Vector3(vScale, vScale, vScale);
            container.Position = DeriveOrbitPositionV0(nodeId + "_outer_pos_" + i, orbitRadius);
            container.AddChild(planetNode);

            // Avoidance metadata for ship Y-lift.
            float outerVisualRadius = vScale * 400.0f;
            container.SetMeta("avoidance_radius", (double)(outerVisualRadius + 5.0f));
            container.SetMeta("visual_radius", (double)outerVisualRadius);
            container.AddToGroup("PlanetBody");

            var pivot = new Node3D { Name = "OuterPlanetOrbit_" + i };
            if (orbitSpin != null)
            {
                pivot.SetScript(orbitSpin);
                pivot.Set("spin_speed_y", KeplerOrbitSpeed(orbitRadius, KeplerK_Planet));
            }
            pivot.AddChild(container);
            if (_currentSolarTilt != null)
                _currentSolarTilt.AddChild(pivot);
            else
                _localSystemRoot.AddChild(pivot);
        }
    }

    // Map PlanetType enum string to addon scene path (fallback if procedural shader missing).
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

    // Map PlanetType to procedural surface shader path.
    private static string GetPlanetShaderPath(string planetType) => planetType switch
    {
        "Terrestrial" => "res://scripts/vfx/planet_terrestrial.gdshader",
        "Ice"         => "res://scripts/vfx/planet_ice.gdshader",
        "Sand"        => "res://scripts/vfx/planet_sand.gdshader",
        "Lava"        => "res://scripts/vfx/planet_lava.gdshader",
        "Gaseous"     => "res://scripts/vfx/planet_gaseous.gdshader",
        "Barren"      => "res://scripts/vfx/planet_barren.gdshader",
        _             => "res://scripts/vfx/planet_terrestrial.gdshader",
    };

    // Atmosphere color per planet type (for fresnel halo).
    private static Color PlanetAtmoColorV0(string planetType) => planetType switch
    {
        "Terrestrial" => new Color(0.3f, 0.55f, 1.0f),
        "Ice"         => new Color(0.6f, 0.8f, 1.0f),
        "Sand"        => new Color(0.95f, 0.7f, 0.4f),
        "Lava"        => new Color(1.0f, 0.3f, 0.05f),
        "Gaseous"     => new Color(0.9f, 0.75f, 0.55f),
        "Barren"      => new Color(0.4f, 0.4f, 0.45f),   // Cool gray silhouette glow.
        _             => new Color(0.5f, 0.5f, 0.5f),
    };

    // Probability (0-1) that a planet type has a visible atmosphere.
    private static float PlanetAtmosphereChanceV0(string planetType) => planetType switch
    {
        "Gaseous"     => 1.0f,   // Always — they're gas giants
        "Terrestrial" => 0.85f,  // Most have atmosphere
        "Sand"        => 0.40f,  // Mars-like: sometimes thin haze
        "Ice"         => 0.25f,  // Rare thin frost haze
        "Lava"        => 0.20f,  // Rare volcanic outgassing
        "Barren"      => 1.0f,   // Always — subtle silhouette glow (not atmosphere, just rim).
        _             => 0.0f,
    };

    // Base atmosphere brightness/thickness (0-2 scale).
    private static float PlanetAtmosphereStrengthV0(string planetType) => planetType switch
    {
        "Gaseous"     => 1.5f,   // Thick, prominent haze
        "Terrestrial" => 1.0f,   // Earth-like visible ring
        "Sand"        => 0.4f,   // Thin dusty haze
        "Ice"         => 0.3f,   // Very subtle frost shimmer
        "Lava"        => 0.6f,   // Volcanic glow haze
        "Barren"      => 0.15f,  // Very faint — just enough to see the edge against space.
        _             => 0.0f,
    };

    // Create a procedural planet node: low-poly sphere + surface shader + atmosphere halo.
    // Seeded by nodeId suffix for per-planet noise variation.
    private Node3D CreateProceduralPlanetV0(string planetType, string seedId)
    {
        var root = new Node3D { Name = "ProceduralPlanet" };

        // Body sphere: 64/48 segments — smooth at close zoom.
        var bodySphere = new SphereMesh
        {
            Radius = 400.0f, Height = 800.0f, // Planet generator addon baked scale.
            RadialSegments = 64, Rings = 48,
        };
        var bodyMI = new MeshInstance3D
        {
            Name = "PlanetBody",
            Mesh = bodySphere,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        // Load procedural surface shader.
        var shaderPath = GetPlanetShaderPath(planetType);
        // Seed noise variation per planet so no two look identical.
        var seedHash = Fnv1a64(seedId + "_planet_seed");
        var shader = GD.Load<Shader>(shaderPath);
        if (shader != null)
        {
            var mat = new ShaderMaterial { Shader = shader };
            float seedOffset = (float)(seedHash % 100UL) * 0.37f;
            // All planet types get seed_offset for per-seed color + noise variation.
            mat.SetShaderParameter("seed_offset", seedOffset);
            if (planetType == "Gaseous")
            {
                mat.SetShaderParameter("band_freq", 6.0f + (float)(seedHash % 8UL));
                mat.SetShaderParameter("storm_latitude", -0.3f + (float)(seedHash % 6UL) * 0.1f);
            }
            else if (planetType == "Terrestrial")
            {
                mat.SetShaderParameter("continent_scale", 2.0f + (float)(seedHash % 3UL) * 0.5f);
                mat.SetShaderParameter("sea_level", 0.42f + (float)(seedHash % 8UL) * 0.02f);
            }
            bodyMI.MaterialOverride = mat;
        }
        else
        {
            // Fallback: basic colored material.
            bodyMI.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.4f, 0.5f, 0.6f), Roughness = 0.8f,
            };
        }
        root.AddChild(bodyMI);

        // Atmosphere halo — probability and intensity vary by planet type.
        // Gaseous/Terrestrial: almost always. Ice/Sand/Lava: rare and thin. Barren: never.
        var atmoChance = PlanetAtmosphereChanceV0(planetType);
        float atmoRoll = (float)(seedHash % 100UL) / 100.0f;
        if (atmoRoll < atmoChance)
        {
            var atmoShader = GD.Load<Shader>("res://scripts/vfx/planet_atmosphere.gdshader");
            if (atmoShader != null)
            {
                // Thickness varies: gas giants thick, rocky worlds thin.
                float baseStrength = PlanetAtmosphereStrengthV0(planetType);
                // Per-planet variation ±30%.
                float variation = 0.7f + (float)((seedHash >> 16) % 60UL) / 100.0f;
                float strength = baseStrength * variation;

                // Scale: thicker atmospheres extend further from the surface.
                float atmoScale = 1.05f + strength * 0.08f; // 1.05x to 1.13x body radius.
                float atmoR = 400.0f * atmoScale;

                var atmoSphere = new SphereMesh
                {
                    Radius = atmoR, Height = atmoR * 2.0f,
                    RadialSegments = 64, Rings = 48,
                };
                var atmoMat = new ShaderMaterial { Shader = atmoShader };
                atmoMat.SetShaderParameter("atmo_color", PlanetAtmoColorV0(planetType));
                atmoMat.SetShaderParameter("atmo_strength", strength);
                // Thinner atmospheres have sharper falloff (more concentrated at rim).
                float power = strength < 0.5f ? 5.0f : (strength < 1.0f ? 4.0f : 3.5f);
                atmoMat.SetShaderParameter("atmo_power", power);
                var atmoMI = new MeshInstance3D
                {
                    Name = "PlanetAtmosphere",
                    Mesh = atmoSphere,
                    MaterialOverride = atmoMat,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                };
                root.AddChild(atmoMI);
            }
        }

        return root;
    }

    // GATE.S1.HERO_SHIP_LOOP.LANE_GATE_LABEL.001: displayName from NeighborDisplayName; falls back to neighborId.
    // GATE.S1.VISUAL_POLISH.STRUCTURES.001: arch/frame gate geometry with emissive glow.
    private Node3D CreateLaneGateMarkerV0(string neighborId, string displayName = "")
    {
        var root = new Node3D { Name = "LaneGate_" + neighborId };
        root.SetMeta("neighbor_node_id", neighborId);

        // Centered emissive torus — upright gate ring. Origin-aligned so vortex VFX matches.
        var orbMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.4f, 0.6f, 1.0f, 0.85f),
            EmissionEnabled = true,
            Emission = new Color(0.4f, 0.65f, 1.0f),
            EmissionEnergyMultiplier = 5.0f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        var orb = new MeshInstance3D
        {
            Name = "GateOrb",
            Mesh = new TorusMesh { InnerRadius = 6.0f, OuterRadius = 8.0f, Rings = 32, RingSegments = 24 },
            MaterialOverride = orbMat,
            // Upright ring (XY plane) — parent marker's LookAt orients it to face the lane.
            // Ship flies through the ring opening when approaching.
            Rotation = new Vector3(Mathf.DegToRad(90f), 0f, 0f),
        };
        root.AddChild(orb);

        // Stargate-like event horizon disc inside the torus ring.
        var portalShader = GD.Load<Shader>("res://scripts/vfx/gate_portal.gdshader");
        if (portalShader != null)
        {
            var portalMat = new ShaderMaterial { Shader = portalShader };
            var portal = new MeshInstance3D
            {
                Name = "GatePortal",
                Mesh = new PlaneMesh { Size = new Vector2(12.0f, 12.0f) }, // Inner radius 6u → diameter 12u
                MaterialOverride = portalMat,
                Rotation = new Vector3(Mathf.DegToRad(90f), 0f, 0f), // Same plane as torus
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            root.AddChild(portal);
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

        // Gate label removed — no floating text in space. Gate destination shown in HUD on approach.

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
            // Tall cylinder shape: 10u XZ radius, 30u height — catches ships at Y-lift altitude over planets.
            Shape = new CylinderShape3D { Radius = 10.0f, Height = 30.0f }
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

    // Continuous circular orbit target: each fleet gets a hash-based starting angle
    // that advances smoothly over time. The target is always ~90° ahead on the circle
    // so the ship naturally follows a circular path.
    private static Vector3 ComputeOrbitTargetV0(string fleetId, float radius, float angularSpeed)
    {
        var hash = Fnv1a64(fleetId);
        float baseAngle = (float)(hash % 360UL) * (MathF.PI / 180f);
        float elapsed = (float)Time.GetTicksMsec() / 1000f;
        // Current angle = base + time * speed. Target is 90° ahead (quarter circle).
        float currentAngle = baseAngle + elapsed * angularSpeed;
        float targetAngle = currentAngle + MathF.PI * 0.5f; // 90° ahead
        return new Vector3(MathF.Cos(targetAngle) * radius, 0f, MathF.Sin(targetAngle) * radius);
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
        {
            var cachedStarPos = GetNodeScaledPositionV0(nodeId);
            return cachedStarPos + localPos;
        }
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
        if (snap == null || !snap.ContainsKey("system_nodes"))
        {
            // Read-lock contention — TryExecuteSafeRead(0) returned empty.
            // Retry up to 10 times (deferred = one per frame).
            if (_overlayOpen && _snapshotRetryCount < 10)
            {
                _snapshotRetryCount++;
                CallDeferred(nameof(RefreshFromSnapshotV0));
            }
            return;
        }

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

            // GATE.T50.VISUAL: Parse enriched galaxy map data.
            string factionId = n.ContainsKey("faction_id") ? (string)(Variant)n["faction_id"] : "";
            float fR = n.ContainsKey("faction_r") ? (float)(Variant)n["faction_r"] : 0.5f;
            float fG = n.ContainsKey("faction_g") ? (float)(Variant)n["faction_g"] : 0.5f;
            float fB = n.ContainsKey("faction_b") ? (float)(Variant)n["faction_b"] : 0.5f;
            string worldClass = n.ContainsKey("world_class") ? (string)(Variant)n["world_class"] : "";
            string primaryIndustry = n.ContainsKey("primary_industry") ? (string)(Variant)n["primary_industry"] : "";
            int industryCount = n.ContainsKey("industry_count") ? (int)(Variant)n["industry_count"] : 0;
            int totalInventory = n.ContainsKey("total_inventory") ? (int)(Variant)n["total_inventory"] : 0;

            nodes.Add(new NodeSnapV0(nodeId, stateToken, displayText, new Vector3(x, y, z),
                fleetCount, factionId, new Color(fR, fG, fB), worldClass, primaryIndustry,
                industryCount, totalInventory));
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
            // Restore node visibility — SetUiPanelActiveV0(true) hides all nodes
            // on dock, and RefreshFromSnapshotV0 must re-show them when the map opens.
            root.Visible = true;

            var label = root.GetNodeOrNull<Label3D>("NodeLabel");
            if (label != null)
            {
                // Token contract: RUMORED => "???", VISITED => name, MAPPED => name+count.
                // FEEL_POST_FIX_8: Use StripResourceTagsV0 for clean system names on galaxy map.
                // GATE.S7.INSTABILITY_EFFECTS.BRIDGE.001: Append instability phase to node label.
                // Only show real system names for nodes the player has visited/mapped.
                // All other states (RUMORED, HIDDEN that somehow survived) → "???".
                bool isKnownNode = StringComparer.Ordinal.Equals(n.DisplayStateToken, "VISITED")
                    || StringComparer.Ordinal.Equals(n.DisplayStateToken, "MAPPED");
                string baseText = isKnownNode
                    ? StripResourceTagsV0(n.DisplayText ?? "")
                    : "???";
                // Instability phase tag stripped from galaxy map labels for cleaner presentation.
                // Phase info available via overlay tooltip instead.
                label.Text = baseText;
                // FEEL_POST_FIX_10: Explicitly set label visibility based on current state.
                // Previously only hid labels; now also shows them when not hidden,
                // ensuring RefreshFromSnapshotV0 (deferred) doesn't leave labels invisible
                // after UpdateSemanticZoomV0 already ran for the current band.
                label.Visible = !_localLabelsHidden;
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
                // FEEL_POST_FIX_10: Sync fleet label visibility with label hidden state.
                fleetLabel.Visible = !_localLabelsHidden;
            }

            var mesh = root.GetNodeOrNull<MeshInstance3D>("NodeMesh");
            if (mesh != null && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                bool isPlayer = !string.IsNullOrEmpty(playerNodeId) && StringComparer.Ordinal.Equals(n.NodeId, playerNodeId);
                if (isPlayer)
                {
                    playerHighlighted = true;
                    // L2.1: Cyan player marker per ship-computer visual language.
                    mat.AlbedoColor = new Color(0.4f, 0.85f, 1.0f);
                    mat.EmissionEnabled = true;
                    mat.Emission = new Color(0.4f, 0.85f, 1.0f);
                    // FEEL_POST_FIX_3: Bright enough to dominate over Starlight skybox.
                    mat.EmissionEnergyMultiplier = 12.0f;

                    // GATE.S14.MAP.PLAYER_INDICATOR.001: "YOU" label + pulsing ring
                    if (root.GetNodeOrNull("YouLabel") == null)
                    {
                        // FEEL_POST_FIX_3: Label + ring scaled for altitude ~5000u.
                        var youLabel = new Label3D
                        {
                            Name = "YouLabel",
                            Text = "▼ YOU ▼",
                            PixelSize = 4.0f,
                            FontSize = 84,
                            OutlineSize = 18,
                            Modulate = new Color(0.2f, 1.0f, 0.4f),
                            OutlineModulate = new Color(0.0f, 0.0f, 0.0f, 1.0f),
                            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                            Position = new Vector3(0, 300f, 0),
                            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                            NoDepthTest = true,
                        };
                        root.AddChild(youLabel);

                        var ringMat = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(0.2f, 1.0f, 0.4f, 0.9f),
                            EmissionEnabled = true,
                            Emission = new Color(0.1f, 1.0f, 0.3f),
                            EmissionEnergyMultiplier = 16.0f,
                            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                            NoDepthTest = true,
                        };
                        var ringMesh = new TorusMesh { InnerRadius = 80.0f, OuterRadius = 100.0f };
                        var ringInst = new MeshInstance3D
                        {
                            Name = "PlayerRing",
                            Mesh = ringMesh,
                            MaterialOverride = ringMat,
                            Rotation = new Vector3(Mathf.Pi / 2f, 0, 0),
                        };
                        root.AddChild(ringInst);

                        // Outer ring for visual separation from node glow.
                        var outerRingMat = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(1.0f, 0.85f, 0.1f, 0.4f),
                            EmissionEnabled = true,
                            Emission = new Color(1.0f, 0.75f, 0.0f),
                            EmissionEnergyMultiplier = 6.0f,
                            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                            NoDepthTest = true,
                        };
                        var outerRingMesh = new TorusMesh { InnerRadius = 120.0f, OuterRadius = 130.0f };
                        var outerRingInst = new MeshInstance3D
                        {
                            Name = "PlayerOuterRing",
                            Mesh = outerRingMesh,
                            MaterialOverride = outerRingMat,
                            Rotation = new Vector3(Mathf.Pi / 2f, 0, 0),
                            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                        };
                        root.AddChild(outerRingInst);

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

                    // GATE.T50.VISUAL.GALAXY_NODES.001: Base color by primary industry type.
                    // Mining=amber, Refinery=blue, Farming=green, Trading=white, Unknown=soft blue.
                    Color industryBaseColor;
                    if (isRumored)
                    {
                        industryBaseColor = new Color(0.4f, 0.4f, 0.5f); // Gray-blue for unknown systems
                    }
                    else if (_currentOverlayMode == GalaxyOverlayMode.IntelFreshness)
                    {
                        industryBaseColor = GetIntelFreshnessNodeColor(n.NodeId);
                    }
                    else
                    {
                        industryBaseColor = n.PrimaryIndustry switch
                        {
                            "mining" => new Color(1.0f, 0.75f, 0.2f),    // Amber
                            "refinery" => new Color(0.3f, 0.6f, 1.0f),   // Blue
                            "farming" => new Color(0.3f, 0.9f, 0.3f),    // Green
                            "trading" => new Color(0.9f, 0.9f, 1.0f),    // White
                            _ => new Color(0.0f, 0.6f, 1.0f),            // Default soft blue
                        };
                    }

                    // GATE.T50.VISUAL.GALAXY_FACTION.001: Blend faction primary color into node color.
                    // 30% faction tint preserves industry readability while showing territory.
                    Color nodeColor = industryBaseColor;
                    if (!isRumored && !string.IsNullOrEmpty(n.FactionId))
                    {
                        nodeColor = industryBaseColor.Lerp(n.FactionColor, 0.3f);
                    }

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

                    // GATE.S7.INSTABILITY.VISUAL.001 + GATE.T45.DEEP_DREAD.GALAXY_DREAD.001: Tint by instability phase.
                    if (_bridge != null)
                    {
                        var instab = _bridge.GetNodeInstabilityV0(n.NodeId);
                        int phaseIdx = instab.ContainsKey("phase_index") ? (int)(Variant)instab["phase_index"] : 0;
                        if (phaseIdx >= 4) // Void — distinctive purple with bright emission
                            nodeColor = new Color(0.6f, 0.0f, 0.8f); // deep purple
                        else if (phaseIdx >= 3) // Fracture — heavy red-purple
                            nodeColor = nodeColor.Lerp(new Color(0.7f, 0.1f, 0.9f), 0.5f);
                        else if (phaseIdx >= 2) // Drift — faint purple wash
                            nodeColor = nodeColor.Lerp(new Color(0.5f, 0.3f, 0.8f), 0.3f);

                        // GATE.T45.DEEP_DREAD.GALAXY_DREAD.001: Dim nodes outside patrol coverage.
                        // Nodes with zero patrol (hops 5+) get dimmer emission = visual isolation.
                        if (_bridge.HasMethod("GetDreadStateV0") && phaseIdx >= 1)
                        {
                            // Slight alpha-based dimming for nodes in dread space.
                            nodeColor = nodeColor.Lerp(new Color(nodeColor.R * 0.7f, nodeColor.G * 0.5f, nodeColor.B * 0.6f), 0.2f);
                        }
                    }

                    mat.AlbedoColor = nodeColor;
                    mat.EmissionEnabled = true;
                    mat.Emission = nodeColor;

                    // GATE.T50.VISUAL.GALAXY_NODES.001: Size by world class (station tier proxy).
                    // CORE = large (capital hubs), FRONTIER = medium, RIM = small (outposts).
                    // Combined with discovery state: RUMORED always small, MAPPED gets a bonus.
                    bool isMapped = StringComparer.Ordinal.Equals(n.DisplayStateToken, "MAPPED");
                    float worldClassScale = n.WorldClass switch
                    {
                        "CORE" => 1.3f,       // Capital / hub — large
                        "FRONTIER" => 1.0f,    // Medium systems
                        "RIM" => 0.8f,         // Outpost — small
                        _ => 1.0f,
                    };

                    if (isRumored)
                    {
                        mat.EmissionEnergyMultiplier = 8.0f;
                        root.Scale = new Vector3(0.6f, 0.6f, 0.6f);
                    }
                    else if (isMapped)
                    {
                        mat.EmissionEnergyMultiplier = 20.0f;
                        float s = 1.15f * worldClassScale;
                        root.Scale = new Vector3(s, s, s);
                    }
                    else // VISITED
                    {
                        mat.EmissionEnergyMultiplier = 15.0f;
                        root.Scale = new Vector3(worldClassScale, worldClassScale, worldClassScale);
                    }

                    // GATE.T50.VISUAL.GALAXY_ECON.001: Economic activity indicator via PlanetDot glow.
                    // Total inventory drives brightness; industry count drives visibility.
                    var planetDot = root.GetNodeOrNull<MeshInstance3D>("PlanetDot");
                    if (planetDot != null && !isRumored && n.IndustryCount > 0)
                    {
                        planetDot.Visible = true;
                        // Economic intensity: clamp inventory into 0-1 range (500 units = full glow).
                        float econIntensity = Mathf.Clamp(n.TotalInventory / 500.0f, 0.1f, 1.0f);
                        // Color matches the industry base color but brighter as a "halo".
                        Color econColor = industryBaseColor.Lerp(new Color(1.0f, 1.0f, 1.0f), 0.3f);
                        econColor.A = 0.3f + 0.5f * econIntensity;
                        if (planetDot.MaterialOverride is StandardMaterial3D econMat)
                        {
                            econMat.AlbedoColor = econColor;
                            econMat.Emission = econColor;
                            econMat.EmissionEnergyMultiplier = 2.0f + 4.0f * econIntensity;
                        }
                    }
                    else if (planetDot != null)
                    {
                        planetDot.Visible = false;
                    }
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

            // Fog of war: only show lanes where BOTH endpoints have been visited.
            // Lanes to unexplored "???" systems stay hidden until the player arrives.
            if (_bridge != null)
            {
                bool fromVisited = !_bridge.IsFirstVisitV0(e.FromId);
                bool toVisited = !_bridge.IsFirstVisitV0(e.ToId);
                if (!fromVisited || !toVisited) continue;
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
                    // FEEL_POST_FIX_3: Edge emission boosted for altitude ~5000u visibility.
                    EmissionEnergyMultiplier = 4.0f,
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
            // Restore edge visibility — SetUiPanelActiveV0(true) hides all edges
            // on dock, and RefreshFromSnapshotV0 must re-show them when the map opens.
            mesh.Visible = true;

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

        // GATE.S8.HAVEN.GALAXY_ICON.001: Haven starbase icon on galaxy map.
        UpdateHavenIconV0();

        // GATE.S8.PENTAGON.DELIVERY.001: Pentagon trade dependency overlay.
        UpdatePentagonOverlayV0();

        // GATE.S6.UI_DISCOVERY.PHASE_MARKERS.001: Discovery phase markers on galaxy map.
        UpdateDiscoveryPhaseMarkersV0();

        // GATE.T52.DISC.SCANNER_VIS.001: Scanner range dashed circle centered on player node.
        UpdateScannerRangeRingV0(playerNodeId);

        // GATE.S7.TERRITORY_SHIFT.MAP_UPDATE.001: Periodic territory overlay refresh.
        // Re-poll node-faction map every ~2 seconds to catch mid-game territory shifts.
        _territoryRefreshTimer -= Engine.GetProcessFrames() > 0 ? (1.0 / 60.0) : 0.016;
        if (_territoryRefreshTimer <= 0.0)
        {
            UpdateTerritoryOverlayV0();
            UpdateFactionLabelsV0();
            _territoryRefreshTimer = 2.0;
        }
    }

    // GATE.S7.TERRITORY_SHIFT.MAP_UPDATE.001: Timer for periodic territory refresh.
    private double _territoryRefreshTimer = 2.0;

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

        // FEEL_POST_FIX_3: Beacon sized for altitude ~5000u (galactic scale 25x).
        // At 5000u altitude, 60u radius ≈ 13px — barely visible. 150u ≈ 34px — clearly visible.
        var beacon = new MeshInstance3D();
        beacon.Name = "NodeBeacon";
        beacon.Mesh = new SphereMesh { Radius = 150.0f, Height = 300.0f };
        beacon.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.7f, 0.9f, 0.6f),
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.5f, 0.7f),
            EmissionEnergyMultiplier = 3.0f,
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
            // FEEL_POST_FIX_7: Larger text for readability at auto-fit altitude (3000-8000u).
            PixelSize = 4.0f,
            FontSize = 64,
            OutlineSize = 14,
            Modulate = new Color(0.85f, 0.85f, 0.9f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Width = 200f,
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        lbl.Position = new Vector3(0, 180.0f, 0);
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

        // GATE.T43.SCAN_UI.GALAXY_MARKERS.001: Planet type icon (colored dot below beacon).
        var planetDot = new MeshInstance3D();
        planetDot.Name = "PlanetDot";
        planetDot.Mesh = new SphereMesh { Radius = 60.0f, Height = 120.0f };
        planetDot.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.5f, 0.5f, 0.0f), // Hidden by default
            EmissionEnabled = true,
            Emission = new Color(0.5f, 0.5f, 0.5f),
            EmissionEnergyMultiplier = 2.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        planetDot.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        planetDot.Position = new Vector3(0, -100.0f, 0);
        planetDot.Visible = false;
        root.AddChild(planetDot);

        // GATE.T43.SCAN_UI.GALAXY_MARKERS.001: Scan state ring (torus around node).
        var scanRing = new MeshInstance3D();
        scanRing.Name = "ScanRing";
        scanRing.Mesh = new TorusMesh { InnerRadius = 170.0f, OuterRadius = 200.0f };
        scanRing.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.85f, 0.2f, 0.0f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.85f, 0.2f),
            EmissionEnergyMultiplier = 2.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        scanRing.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        scanRing.Visible = false;
        root.AddChild(scanRing);

        return root;
    }

    private static MeshInstance3D CreateEdgeMeshV0(Material mat)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = "GalaxyEdge";

        // Cylinder oriented along +Y then rotated into place.
        // FEEL_POST_FIX_3: Radius 30u visible at strategic altitude ~5000u.
        // At 8u radius, lanes were ~1.7px at altitude 5000 — invisible.
        // At 30u radius, lanes are ~6.5px — clearly visible like Stellaris/MOO lane lines.
        var cyl = new CylinderMesh
        {
            TopRadius = 30.0f,
            BottomRadius = 30.0f,
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
        // GATE.T50.VISUAL: Enriched galaxy map data.
        public readonly string FactionId;
        public readonly Color FactionColor;
        public readonly string WorldClass;
        public readonly string PrimaryIndustry;
        public readonly int IndustryCount;
        public readonly int TotalInventory;

        public NodeSnapV0(string nodeId, string displayStateToken, string displayText, Vector3 position,
            int fleetCount = 0, string factionId = "", Color factionColor = default,
            string worldClass = "", string primaryIndustry = "", int industryCount = 0, int totalInventory = 0)
        {
            NodeId = nodeId ?? "";
            DisplayStateToken = displayStateToken ?? "";
            DisplayText = displayText ?? "";
            Position = position;
            FleetCount = fleetCount;
            FactionId = factionId ?? "";
            FactionColor = factionColor;
            WorldClass = worldClass ?? "";
            PrimaryIndustry = primaryIndustry ?? "";
            IndustryCount = industryCount;
            TotalInventory = totalInventory;
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

    // GATE.T50.VISUAL.GALAXY_ECON.001: Subtle pulsing glow on PlanetDot to indicate economic activity.
    // Nodes with active industries pulse gently; the pulse speed varies with total inventory.
    private double _econPulseTime = 0.0;
    private void _PulseEconGlowV0(double delta)
    {
        _econPulseTime += delta;
        float baseT = (float)_econPulseTime;
        // Gentle sine-wave pulse on emission energy (period ~3s, amplitude 20%).
        float pulse = 1.0f + 0.2f * Mathf.Sin(baseT * 2.1f);

        foreach (var kv in _nodeRootsById)
        {
            var planetDot = kv.Value.GetNodeOrNull<MeshInstance3D>("PlanetDot");
            if (planetDot == null || !planetDot.Visible) continue;

            if (planetDot.MaterialOverride is StandardMaterial3D econMat)
            {
                // Store base emission in the material's metadata via albedo alpha channel.
                // Base energy was set in RefreshFromSnapshotV0 (2.0-6.0 range).
                // Apply pulse factor multiplicatively from the base.
                float baseEnergy = econMat.AlbedoColor.A > 0.01f
                    ? 2.0f + 4.0f * Mathf.Clamp((econMat.AlbedoColor.A - 0.3f) / 0.5f, 0f, 1f)
                    : 3.0f;
                econMat.EmissionEnergyMultiplier = baseEnergy * pulse;
            }
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
                // Territory disc sized for galaxy altitude (~5000u). 12u was invisible (~2px).
                // 600u ≈ 130px at 5000u altitude — clearly visible faction territory fill.
                var mesh = new PlaneMesh { Size = new Vector2(600f, 600f) };
                disc.Mesh = mesh;
                _territoryDiscsByNodeId[nodeId] = disc;
                AddChild(disc);
            }

            // Restore visibility — SetUiPanelActiveV0(true) hides territory discs on dock.
            disc.Visible = true;

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
        // LootMarker spheres removed — were placeholder programmer art.
        ClearLootMarkersV0();
    }

    private void ClearLootMarkersV0()
    {
        foreach (var kv in _lootMarkersByDropId)
            kv.Value.QueueFree();
        _lootMarkersByDropId.Clear();
    }

    // GATE.S8.HAVEN.GALAXY_ICON.001: Show a distinct Haven icon on the galaxy map when discovered.
    // Diamond-shaped mesh (rotated box) in gold/amber, with a "HAVEN" label above.
    // Sized for visibility at STRATEGIC_ALTITUDE (~1800u).
    private void UpdateHavenIconV0()
    {
        if (_bridge == null || !_bridge.HasMethod("GetHavenStatusV0"))
        {
            ClearHavenIconV0();
            return;
        }

        var status = _bridge.Call("GetHavenStatusV0").AsGodotDictionary();
        if (status == null || status.Count == 0)
        {
            ClearHavenIconV0();
            return;
        }

        bool discovered = status.ContainsKey("discovered") && (bool)status["discovered"];
        string nodeId = status.ContainsKey("node_id") ? (string)(Variant)status["node_id"] : "";

        if (!discovered || string.IsNullOrEmpty(nodeId))
        {
            ClearHavenIconV0();
            return;
        }

        // Find the node position from rendered galaxy nodes.
        if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot))
        {
            ClearHavenIconV0();
            return;
        }

        // GATE.S8.HAVEN.VISUAL_TIERS.001: Haven icon scales with tier level.
        int tierLevel = status.ContainsKey("tier") ? (int)(Variant)status["tier"] : 1;
        // Tier 1=200, 2=260, 3=320, 4=380, 5=440 size; emission 25→65.
        float iconSize = 200.0f + (tierLevel - 1) * 60.0f;
        float emissionEnergy = 25.0f + (tierLevel - 1) * 10.0f;

        // Haven color shifts from gold (T1) toward bright white-gold (T5).
        float whiteMix = (tierLevel - 1) * 0.05f; // 0..0.20
        var havenColor = new Color(1.0f, 0.85f + whiteMix, 0.2f + whiteMix * 2.0f, 1.0f);

        // Create or update the diamond mesh.
        if (_havenIconMesh == null)
        {
            _havenIconMesh = new MeshInstance3D
            {
                Name = "HavenIcon",
                Mesh = new BoxMesh
                {
                    Size = new Vector3(iconSize, iconSize, iconSize),
                },
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            _havenIconMesh.RotationDegrees = new Vector3(0f, 45f, 45f);
            _havenIconMesh.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = havenColor,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                EmissionEnabled = true,
                Emission = havenColor,
                EmissionEnergyMultiplier = emissionEnergy,
            };
            AddChild(_havenIconMesh);
        }
        else
        {
            // Update existing mesh size and material for tier changes.
            if (_havenIconMesh.Mesh is BoxMesh box)
                box.Size = new Vector3(iconSize, iconSize, iconSize);
            if (_havenIconMesh.MaterialOverride is StandardMaterial3D mat)
            {
                mat.AlbedoColor = havenColor;
                mat.Emission = havenColor;
                mat.EmissionEnergyMultiplier = emissionEnergy;
            }
        }

        // Position at the Haven node, slightly above the beacon.
        _havenIconMesh.GlobalPosition = nodeRoot.GlobalPosition + new Vector3(0f, 250.0f, 0f);
        _havenIconMesh.Visible = true;

        // Create or update the "HAVEN" label.
        if (_havenIconLabel == null)
        {
            string tierName = status.ContainsKey("tier_name") ? (string)(Variant)status["tier_name"] : "";
            _havenIconLabel = new Label3D
            {
                Name = "HavenLabel",
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                PixelSize = 4.0f,
                FontSize = 64,
                OutlineSize = 14,
                Modulate = havenColor,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            AddChild(_havenIconLabel);
        }

        // Update label text with tier info.
        string currentTierName = status.ContainsKey("tier_name") ? (string)(Variant)status["tier_name"] : "";
        _havenIconLabel.Text = string.IsNullOrEmpty(currentTierName) ? "HAVEN" : $"HAVEN ({currentTierName})";
        _havenIconLabel.GlobalPosition = nodeRoot.GlobalPosition + new Vector3(0f, 420.0f, 0f);
        _havenIconLabel.Visible = true;
    }

    private void ClearHavenIconV0()
    {
        if (_havenIconMesh != null)
        {
            _havenIconMesh.Visible = false;
        }
        if (_havenIconLabel != null)
        {
            _havenIconLabel.Visible = false;
        }
    }

    // GATE.S8.PENTAGON.DELIVERY.001: Pentagon trade dependency overlay.
    // Shows 5 faction home nodes connected by lines. Active cascade = red broken link.
    private void UpdatePentagonOverlayV0()
    {
        if (_bridge == null || !_bridge.HasMethod("GetPentagonStateV0") || !_bridge.HasMethod("GetFactionMapV0"))
        {
            ClearPentagonOverlayV0();
            return;
        }

        var pentState = _bridge.Call("GetPentagonStateV0").AsGodotDictionary();
        if (pentState == null || pentState.Count == 0)
        {
            ClearPentagonOverlayV0();
            return;
        }

        bool cascadeActive = pentState.ContainsKey("cascade_active") && (bool)(Variant)pentState["cascade_active"];
        bool hasR3 = pentState.ContainsKey("has_r3") && (bool)(Variant)pentState["has_r3"];
        if (!hasR3)
        {
            ClearPentagonOverlayV0();
            return;
        }

        // Get faction home node positions.
        var factions = _bridge.Call("GetFactionMapV0").AsGodotArray();
        if (factions == null || factions.Count == 0) { ClearPentagonOverlayV0(); return; }

        // Pentagon ring order: concord, weavers, chitin, valorin, communion.
        string[] ringOrder = { "concord", "weavers", "chitin", "valorin", "communion" };
        var positions = new System.Collections.Generic.Dictionary<string, Vector3>();

        foreach (var fv in factions)
        {
            var f = fv.AsGodotDictionary();
            if (f == null) continue;
            var fid = f.ContainsKey("faction_id") ? (string)(Variant)f["faction_id"] : "";
            var homeId = f.ContainsKey("home_node_id") ? (string)(Variant)f["home_node_id"] : "";
            if (string.IsNullOrEmpty(fid) || string.IsNullOrEmpty(homeId)) continue;

            var nodeRoot = FindChild(homeId, true, false) as Node3D;
            if (nodeRoot != null)
                positions[fid] = nodeRoot.GlobalPosition + new Vector3(0f, 200.0f, 0f);
        }

        if (positions.Count < 5) { ClearPentagonOverlayV0(); return; }

        // Create/update edge meshes.
        while (_pentagonEdgeMeshes.Count < 5)
        {
            var mesh = new MeshInstance3D { Name = $"PentagonEdge{_pentagonEdgeMeshes.Count}" };
            AddChild(mesh);
            _pentagonEdgeMeshes.Add(mesh);
        }

        for (int i = 0; i < 5; i++)
        {
            var fromFaction = ringOrder[i];
            var toFaction = ringOrder[(i + 1) % 5];

            if (!positions.TryGetValue(fromFaction, out var fromPos) ||
                !positions.TryGetValue(toFaction, out var toPos))
            {
                _pentagonEdgeMeshes[i].Visible = false;
                continue;
            }

            // Line as a stretched cylinder between two points.
            var midpoint = (fromPos + toPos) / 2f;
            var direction = toPos - fromPos;
            float length = direction.Length();
            if (length < 1f) { _pentagonEdgeMeshes[i].Visible = false; continue; }

            var cylinder = new CylinderMesh
            {
                TopRadius = 4.0f,
                BottomRadius = 4.0f,
                Height = length,
                RadialSegments = 6,
            };
            _pentagonEdgeMeshes[i].Mesh = cylinder;
            _pentagonEdgeMeshes[i].GlobalPosition = midpoint;

            // Orient cylinder along the edge direction.
            var up = direction.Normalized();
            var right = up.Cross(Vector3.Up).Normalized();
            if (right.LengthSquared() < 0.01f)
                right = up.Cross(Vector3.Right).Normalized();
            var forward = right.Cross(up).Normalized();
            _pentagonEdgeMeshes[i].GlobalTransform = new Transform3D(
                new Basis(right, up, forward),
                midpoint
            );

            // Color: gold normally, red if cascade is active (broken dependency).
            var edgeColor = cascadeActive
                ? new Color(1.0f, 0.2f, 0.15f)
                : new Color(0.9f, 0.75f, 0.2f);
            _pentagonEdgeMeshes[i].MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = edgeColor,
                EmissionEnabled = true,
                Emission = edgeColor,
                EmissionEnergyMultiplier = 15.0f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            };
            _pentagonEdgeMeshes[i].Visible = true;
        }
        _pentagonOverlayVisible = true;
    }

    private void ClearPentagonOverlayV0()
    {
        foreach (var mesh in _pentagonEdgeMeshes)
            mesh.Visible = false;
        _pentagonOverlayVisible = false;
    }

    // GATE.T52.DISC.PHASE_MARKERS.001: Draw discovery phase markers on the galaxy map.
    // Gray = undiscovered (no discoveries found), Amber = partially scanned, Green = fully analyzed.
    // Small indicator ring offset below each galaxy node.
    private void UpdateDiscoveryPhaseMarkersV0()
    {
        if (_bridge == null) return;

        // Track which nodes we update this frame — remove stale markers afterward.
        var activeNodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var kv in _nodeRootsById)
        {
            var nodeId = kv.Key;
            var root = kv.Value;
            if (root == null || !root.IsInsideTree()) continue;

            var summary = _bridge.GetNodeDiscoveryPhaseSummaryV0(nodeId);
            int total = (int)(Variant)summary["total"];

            // No discovery sites seeded at this node — skip (no marker).
            if (total == 0) continue;

            string phaseToken = (string)(Variant)summary["phase_token"];
            int analyzed = (int)(Variant)summary["analyzed"];
            int scanned = (int)(Variant)summary["scanned"];

            // Determine marker color based on discovery progress.
            Color markerColor;
            if (StringComparer.Ordinal.Equals(phaseToken, "COMPLETE"))
            {
                // All discoveries fully analyzed — green.
                markerColor = new Color(0.2f, 1.0f, 0.4f, 0.7f);
            }
            else if (scanned > 0 || analyzed > 0)
            {
                // Some progress (scanned or partially analyzed) — amber.
                markerColor = new Color(1.0f, 0.75f, 0.2f, 0.7f);
            }
            else
            {
                // Only Seen or no progress — gray.
                markerColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            activeNodeIds.Add(nodeId);

            if (!_discoveryPhaseMarkersByDiscId.TryGetValue(nodeId, out var marker) || !GodotObject.IsInstanceValid(marker))
            {
                // Create small torus ring offset below the node beacon.
                var mat = new StandardMaterial3D
                {
                    AlbedoColor = markerColor,
                    EmissionEnabled = true,
                    Emission = new Color(markerColor.R, markerColor.G, markerColor.B),
                    EmissionEnergyMultiplier = 4.0f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    NoDepthTest = true,
                };
                marker = new MeshInstance3D
                {
                    Name = "DiscPhaseMarker_" + nodeId,
                    Mesh = new TorusMesh { InnerRadius = 110.0f, OuterRadius = 130.0f },
                    MaterialOverride = mat,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    Rotation = new Vector3(Mathf.Pi / 2f, 0, 0),
                    Position = new Vector3(0, -60.0f, 0),
                };
                root.AddChild(marker);
                _discoveryPhaseMarkersByDiscId[nodeId] = marker;
            }
            else
            {
                // Update color on existing marker.
                if (marker.MaterialOverride is StandardMaterial3D existingMat)
                {
                    existingMat.AlbedoColor = markerColor;
                    existingMat.Emission = new Color(markerColor.R, markerColor.G, markerColor.B);
                }
            }
        }

        // Remove stale markers for nodes no longer in the galaxy view.
        var staleKeys = new List<string>();
        foreach (var kv in _discoveryPhaseMarkersByDiscId)
        {
            if (!activeNodeIds.Contains(kv.Key))
                staleKeys.Add(kv.Key);
        }
        for (int i = 0; i < staleKeys.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_discoveryPhaseMarkersByDiscId[staleKeys[i]]))
                _discoveryPhaseMarkersByDiscId[staleKeys[i]].QueueFree();
            _discoveryPhaseMarkersByDiscId.Remove(staleKeys[i]);
        }
    }

    private void ClearDiscoveryPhaseMarkersV0()
    {
        foreach (var kv in _discoveryPhaseMarkersByDiscId)
        {
            if (GodotObject.IsInstanceValid(kv.Value))
                kv.Value.QueueFree();
        }
        _discoveryPhaseMarkersByDiscId.Clear();
    }

    // GATE.T52.DISC.SCANNER_VIS.001: Dashed scanner range circle centered on the player's current node.
    // Circle radius scales with scanner tier. Cyan with 50% alpha, dashed segments.
    private void UpdateScannerRangeRingV0(string playerNodeId)
    {
        if (_bridge == null || string.IsNullOrEmpty(playerNodeId)) return;

        // Get player node position.
        if (!_nodeRootsById.TryGetValue(playerNodeId, out var playerRoot)) return;
        if (playerRoot == null || !playerRoot.IsInsideTree()) return;

        int tier = _bridge.GetScannerTierV0();
        // Clamp tier to valid range.
        if (tier < 0) tier = 0;
        if (tier >= ScannerRangeByTier.Length) tier = ScannerRangeByTier.Length - 1;

        float radius = ScannerRangeByTier[tier];

        // Rebuild mesh only if tier changed or ring not yet created.
        if (_scannerRangeRing == null || !GodotObject.IsInstanceValid(_scannerRangeRing) || tier != _lastScannerTier)
        {
            // Remove old ring.
            if (_scannerRangeRing != null && GodotObject.IsInstanceValid(_scannerRangeRing))
                _scannerRangeRing.QueueFree();

            _scannerRangeRing = BuildDashedRingMeshV0(radius, ScannerRingSegmentCount, ScannerRingDashRatio);
            AddChild(_scannerRangeRing);
            _lastScannerTier = tier;
        }

        // Position the ring at the player node, in the XZ plane (Y=0 relative to galaxy view).
        _scannerRangeRing.GlobalPosition = new Vector3(
            playerRoot.GlobalPosition.X,
            playerRoot.GlobalPosition.Y - 20.0f, // Slightly below node beacon.
            playerRoot.GlobalPosition.Z
        );
        _scannerRangeRing.Visible = true;
    }

    // Build a dashed ring mesh in the XZ plane using ImmediateMesh with alternating visible segments.
    private static MeshInstance3D BuildDashedRingMeshV0(float radius, int segmentCount, float dashRatio)
    {
        var immMesh = new ImmediateMesh();

        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.3f, 0.9f, 1.0f, 0.5f), // Cyan 50% alpha
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.9f, 1.0f),
            EmissionEnergyMultiplier = 3.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            NoDepthTest = true,
        };

        // Ring thickness: use a thin strip of triangles per dash segment.
        float innerR = radius - 15.0f;
        float outerR = radius + 15.0f;
        float angleStep = Mathf.Tau / segmentCount;
        int dashSegs = Mathf.Max(1, (int)(segmentCount * dashRatio / 2)); // segments per dash
        int gapSegs = Mathf.Max(1, segmentCount / (dashSegs * 2) > 0 ? (int)(segmentCount * (1.0f - dashRatio) / 2) : 1);

        // Simple approach: iterate all segments, draw a dash segment, skip a gap segment.
        // Each "group" = dashSegs visible + gapSegs invisible.
        int groupSize = dashSegs + gapSegs;
        // Recalculate for even distribution: 8 dashes around the ring.
        int dashCount = 8;
        int segsPerDash = segmentCount / (dashCount * 2); // half visible, half gap
        if (segsPerDash < 1) segsPerDash = 1;

        immMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        for (int d = 0; d < dashCount; d++)
        {
            int startSeg = d * (segmentCount / dashCount);
            int endSeg = startSeg + segsPerDash;
            if (endSeg > segmentCount) endSeg = segmentCount;

            for (int s = startSeg; s < endSeg; s++)
            {
                float a0 = s * angleStep;
                float a1 = (s + 1) * angleStep;

                // Four corners of this segment strip.
                var innerA = new Vector3(Mathf.Cos(a0) * innerR, 0, Mathf.Sin(a0) * innerR);
                var outerA = new Vector3(Mathf.Cos(a0) * outerR, 0, Mathf.Sin(a0) * outerR);
                var innerB = new Vector3(Mathf.Cos(a1) * innerR, 0, Mathf.Sin(a1) * innerR);
                var outerB = new Vector3(Mathf.Cos(a1) * outerR, 0, Mathf.Sin(a1) * outerR);

                // Triangle 1: innerA, outerA, outerB
                immMesh.SurfaceAddVertex(innerA);
                immMesh.SurfaceAddVertex(outerA);
                immMesh.SurfaceAddVertex(outerB);
                // Triangle 2: innerA, outerB, innerB
                immMesh.SurfaceAddVertex(innerA);
                immMesh.SurfaceAddVertex(outerB);
                immMesh.SurfaceAddVertex(innerB);
            }
        }

        immMesh.SurfaceEnd();

        var inst = new MeshInstance3D
        {
            Name = "ScannerRangeRing",
            Mesh = immMesh,
            MaterialOverride = mat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        return inst;
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
            lbl.Visible = true; // Restore after SetUiPanelActiveV0 hide.
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

    // FEEL_POST_FIX_5: Create a large dark-blue plane behind galaxy map beacons for depth.
    // FEEL_PASS6_P1: Added particle starfield for visual richness.
    private GpuParticles3D _galaxyMapStars;
    private void EnsureGalaxyMapBgV0()
    {
        if (_galaxyMapBg != null) return;
        _galaxyMapBg = new MeshInstance3D
        {
            Name = "GalaxyMapBg",
            Mesh = new PlaneMesh { Size = new Vector2(40000f, 40000f) },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = new Color(0.08f, 0.07f, 0.14f),
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            },
            Position = new Vector3(0f, -200f, 0f),
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        // Add to GalaxyView's parent so it's below beacons but in the 3D scene.
        var parent = GetParent();
        if (parent != null)
            parent.AddChild(_galaxyMapBg);
        else
            AddChild(_galaxyMapBg);

        // FEEL_PASS6_P1: Particle starfield — dim static stars across the map background.
        _galaxyMapStars = new GpuParticles3D
        {
            Name = "GalaxyMapStarfield",
            Amount = 400,
            Lifetime = 100f,    // Effectively static (very long life)
            Explosiveness = 1.0f, // All spawn at once
            OneShot = false,
            VisibilityAabb = new Aabb(new Vector3(-20000, -300, -20000), new Vector3(40000, 600, 40000)),
            Position = new Vector3(0f, -180f, 0f),
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        var starMat = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(8000f, 10f, 8000f),
            Direction = new Vector3(0, 0, 0),
            InitialVelocityMin = 0f,
            InitialVelocityMax = 0f,
            Gravity = Vector3.Zero,
            ScaleMin = 0.3f,
            ScaleMax = 1.2f,
        };
        _galaxyMapStars.ProcessMaterial = starMat;
        var starMesh = new SphereMesh { Radius = 2f, Height = 4f, RadialSegments = 4, Rings = 2 };
        var starDrawMat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.5f, 0.55f, 0.7f, 0.4f),
            EmissionEnabled = true,
            Emission = new Color(0.4f, 0.45f, 0.65f),
            EmissionEnergyMultiplier = 0.8f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        starMesh.Material = starDrawMat;
        _galaxyMapStars.DrawPass1 = starMesh;
        if (parent != null)
            parent.AddChild(_galaxyMapStars);
        else
            AddChild(_galaxyMapStars);
    }

    // FEEL_POST_FIX_6: Compute camera altitude + centroid to fit all visible nodes in the viewport.
    // Returns Dictionary with "altitude", "center_x", "center_z".
    // Called by player_follow_camera.gd when opening galaxy map via TAB.
    public Godot.Collections.Dictionary GetAutoFitFrameV0()
    {
        var result = new Godot.Collections.Dictionary
        {
            ["altitude"] = 5000f,
            ["center_x"] = 0f,
            ["center_z"] = 0f,
        };

        if (_bridge == null) return result;
        var snap = _bridge.GetGalaxySnapshotV0();
        if (snap == null) return result;

        var rawNodes = snap.ContainsKey("system_nodes")
            ? (Godot.Collections.Array)snap["system_nodes"]
            : null;
        if (rawNodes == null || rawNodes.Count == 0) return result;

        float galScale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        int visibleCount = 0;

        for (int i = 0; i < rawNodes.Count; i++)
        {
            if (rawNodes[i].VariantType != Variant.Type.Dictionary) continue;
            var n = rawNodes[i].AsGodotDictionary();
            var token = n.ContainsKey("display_state_token") ? (string)(Variant)n["display_state_token"] : "";
            if (StringComparer.Ordinal.Equals(token, "HIDDEN")) continue;

            float x = (n.ContainsKey("pos_x") ? (float)(Variant)n["pos_x"] : 0f) * galScale;
            float z = (n.ContainsKey("pos_z") ? (float)(Variant)n["pos_z"] : 0f) * galScale;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
            visibleCount++;
        }

        if (visibleCount <= 1) return result;

        float dx = maxX - minX;
        float dz = maxZ - minZ;
        // Ensure minimum span so camera doesn't zoom too close for nearby nodes.
        dx = Mathf.Max(dx, 2000f);
        dz = Mathf.Max(dz, 2000f);

        // For vertical FOV=60, tan(30)=0.577. Aspect 16:9=1.778.
        float tanHalf = Mathf.Tan(30f * Mathf.Pi / 180f);
        float aspect = 1920f / 1080f;
        // Fit into 70% of frame (15% padding each side).
        float hFromX = dx / (0.7f * 2f * tanHalf * aspect);
        float hFromZ = dz / (0.7f * 2f * tanHalf);

        float altitude = Mathf.Max(hFromX, hFromZ);
        result["altitude"] = Mathf.Clamp(altitude, 3000f, 15000f);
        result["center_x"] = (minX + maxX) / 2f;
        result["center_z"] = (minZ + maxZ) / 2f;
        return result;
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
        // L2.3: Notify HUD of V2 mode change for legend + mode label.
        NotifyHudV2ModeV0();
    }

    /// Notify the HUD of the current V2 overlay mode.
    private void NotifyHudV2ModeV0()
    {
        var hud = GetTree()?.Root?.FindChild("HUD", true, false);
        if (hud != null && hud.HasMethod("set_v2_overlay_mode_v0"))
            hud.Call("set_v2_overlay_mode_v0", (int)_v2OverlayMode);
    }

    /// Public API for external callers (e.g., toolbar buttons).
    public void SetV2OverlayModeV0(int mode)
    {
        if (mode >= 0 && mode <= 5)
            _v2OverlayMode = (GalaxyMapV2Overlay)mode;
        else
            _v2OverlayMode = GalaxyMapV2Overlay.Off;
        // Immediate visual refresh so bot screenshots capture the change.
        if (_v2OverlayMode != GalaxyMapV2Overlay.Off)
            UpdateV2OverlayVisualsV0();
        else
            ClearV2OverlayV0();
        NotifyHudV2ModeV0();
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
            color.A = Mathf.Clamp(influence * 0.6f, 0.35f, 0.65f);

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, 350.0f);
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
            float radius = 250.0f + fleetCount * 30.0f;

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, Mathf.Min(radius, 450.0f));
        }

        // Always show player location in fleet overlay.
        EnsurePlayerFallbackDiscV0(seenNodes, new Color(0.2f, 0.8f, 1.0f, 0.4f), 280.0f);

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

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, 300.0f + heat * 150.0f);
        }

        // Always show player location in heat overlay (cool = no heat).
        EnsurePlayerFallbackDiscV0(seenNodes, new Color(0.2f, 0.6f, 0.2f, 0.4f), 280.0f);

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
                    color = new Color(0.7f, 0.2f, 0.9f, 0.5f);
                    size = 130.0f;
                    break;
                case "mapped":
                    color = new Color(0.1f, 0.9f, 0.3f, 0.55f);
                    size = 320.0f;
                    break;
                case "visited":
                    color = new Color(0.5f, 0.7f, 1.0f, 0.5f);
                    size = 300.0f;
                    break;
                default: // "unvisited"
                    color = new Color(0.3f, 0.3f, 0.35f, 0.4f);
                    size = 280.0f;
                    break;
            }

            seenNodes.Add(nodeId);
            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, size);
        }

        PruneV2OverlayDiscs(seenNodes);
    }

    // GATE.S7.GALAXY_MAP_V2.WARFRONT_OVL.001: Warfront intensity overlay.
    // GATE.S7.UI_WARFRONT.MAP_OVERLAY.001: Enhanced warfront overlay with pulsing contested nodes,
    // objective icons, and supply line edges.
    private float _warfrontPulseTime = 0f;
    private readonly Dictionary<string, MeshInstance3D> _supplyLineMeshesByKey = new();
    private readonly Dictionary<string, MeshInstance3D> _objectiveMarkersByNodeId = new();

    private void UpdateWarfrontOverlayDiscsV0()
    {
        var data = _bridge.GetWarfrontOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        // Pulse: time-based sinusoidal alpha oscillation for contested nodes.
        _warfrontPulseTime += 0.016f; // ~60fps tick
        float pulse = 0.5f + 0.5f * Mathf.Sin(_warfrontPulseTime * 3.0f);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            float intensity = (float)data[key];
            if (intensity <= 0.01f) continue;

            seenNodes.Add(nodeId);

            // Red gradient: darker red at low intensity, bright red at high.
            // Pulse contested nodes (high intensity pulses more).
            float r = 0.5f + intensity * 0.5f;
            float g = Mathf.Clamp(0.15f - intensity * 0.1f, 0f, 0.15f);
            float b = 0.05f;
            float baseAlpha = 0.15f + intensity * 0.4f;
            float a = baseAlpha * (0.6f + 0.4f * pulse); // Pulse alpha
            var color = new Color(r, g, b, a);

            float size = 300.0f + intensity * 150.0f;
            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, size);
        }

        PruneV2OverlayDiscs(seenNodes);

        // GATE.S7.UI_WARFRONT.SUPPLY.001: Draw supply lines from warfront data.
        UpdateSupplyLineVisualsV0();

        // GATE.S7.UI_WARFRONT.MAP_OVERLAY.001: Objective markers at contested nodes.
        UpdateObjectiveMarkersV0(data);
    }

    // GATE.S7.UI_WARFRONT.MAP_OVERLAY.001: Objective markers (small icon meshes at strategic objectives).
    private void UpdateObjectiveMarkersV0(Godot.Collections.Dictionary overlayData)
    {
        // ObjMarker spheres removed — were placeholder programmer art.
        // Clean up any existing markers from prior frames.
        foreach (var kv in _objectiveMarkersByNodeId) kv.Value.QueueFree();
        _objectiveMarkersByNodeId.Clear();
    }

    // GATE.S7.UI_WARFRONT.SUPPLY.001: Supply line visualization — edges from
    // faction HQ nodes to front-line contested nodes. Severed/weak lines shown red.
    private void UpdateSupplyLineVisualsV0()
    {
        var overlayData = _bridge.GetWarfrontOverlayV0();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        // Build list of contested nodes.
        var contestedNodeIds = new List<string>();
        foreach (var key in overlayData.Keys)
        {
            var nodeId = key.AsString();
            if (!string.IsNullOrEmpty(nodeId)) contestedNodeIds.Add(nodeId);
        }

        if (contestedNodeIds.Count == 0)
        {
            ClearSupplyLinesV0();
            return;
        }

        // Draw supply edges between adjacent contested nodes.
        // Pairs: for each pair of contested nodes that share an edge, draw a supply line.
        for (int i = 0; i < contestedNodeIds.Count; i++)
        {
            for (int j = i + 1; j < contestedNodeIds.Count; j++)
            {
                var fromId = contestedNodeIds[i];
                var toId = contestedNodeIds[j];

                // Check if these nodes have an edge.
                string edgeKey = fromId + "->" + toId;
                string edgeKeyRev = toId + "->" + fromId;
                bool hasEdge = _edgeMeshesByKey.ContainsKey(edgeKey) || _edgeMeshesByKey.ContainsKey(edgeKeyRev);
                if (!hasEdge) continue;

                seenKeys.Add(edgeKey);

                if (!_nodeRootsById.TryGetValue(fromId, out var fromRoot)) continue;
                if (!_nodeRootsById.TryGetValue(toId, out var toRoot)) continue;

                float fromIntensity = overlayData.ContainsKey(fromId) ? (float)overlayData[fromId] : 0f;
                float toIntensity = overlayData.ContainsKey(toId) ? (float)overlayData[toId] : 0f;
                float avgIntensity = (fromIntensity + toIntensity) * 0.5f;

                // High intensity = severed/broken (red), low = intact (orange/yellow).
                Color lineColor;
                if (avgIntensity >= 0.75f)
                    lineColor = new Color(1.0f, 0.1f, 0.1f, 0.8f); // Severed — bright red
                else if (avgIntensity >= 0.5f)
                    lineColor = new Color(1.0f, 0.4f, 0.1f, 0.6f); // Stressed — orange
                else
                    lineColor = new Color(1.0f, 0.8f, 0.2f, 0.4f); // Intact — yellow

                if (!_supplyLineMeshesByKey.TryGetValue(edgeKey, out var lineMesh))
                {
                    var mat = new StandardMaterial3D
                    {
                        AlbedoColor = lineColor,
                        EmissionEnabled = true,
                        Emission = new Color(lineColor.R, lineColor.G, lineColor.B),
                        EmissionEnergyMultiplier = 3.0f,
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    };
                    lineMesh = CreateEdgeMeshV0(mat);
                    _supplyLineMeshesByKey[edgeKey] = lineMesh;
                    AddChild(lineMesh);
                }

                // Position the supply line slightly above the normal edge.
                UpdateEdgeTransformV0(lineMesh, fromRoot.GlobalPosition + new Vector3(0, 5, 0),
                                      toRoot.GlobalPosition + new Vector3(0, 5, 0));

                if (lineMesh.MaterialOverride is StandardMaterial3D existingMat)
                {
                    existingMat.AlbedoColor = lineColor;
                    existingMat.Emission = new Color(lineColor.R, lineColor.G, lineColor.B);
                }
            }
        }

        // Prune stale supply line meshes.
        var toRemove = new List<string>();
        foreach (var kv in _supplyLineMeshesByKey)
        {
            if (!seenKeys.Contains(kv.Key))
            {
                kv.Value.QueueFree();
                toRemove.Add(kv.Key);
            }
        }
        foreach (var k in toRemove) _supplyLineMeshesByKey.Remove(k);
    }

    private void ClearSupplyLinesV0()
    {
        foreach (var kv in _supplyLineMeshesByKey) kv.Value.QueueFree();
        _supplyLineMeshesByKey.Clear();
        foreach (var kv in _objectiveMarkersByNodeId) kv.Value.QueueFree();
        _objectiveMarkersByNodeId.Clear();
    }

    // ════════════════════════════════════════════════════════════════════════
    // GATE.S6.UI_DISCOVERY.SCAN_VIZ.001: Scan pulse effect + discovery highlights
    // ════════════════════════════════════════════════════════════════════════

    private MeshInstance3D _scanPulseRing;
    private float _scanPulseRadius = 0f;
    private bool _scanPulseActive = false;
    private float _scanPulseMaxRadius = 120.0f;
    private float _scanPulseSpeed = 80.0f; // units per second
    private Vector3 _scanPulseOrigin;
    private readonly Dictionary<string, MeshInstance3D> _discoveryHighlightsByNodeId = new();

    /// <summary>
    /// Trigger a scanning pulse effect from a node position.
    /// Called when player initiates a scan at their current node.
    /// </summary>
    public void TriggerScanPulseV0(string nodeId)
    {
        if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) return;

        _scanPulseOrigin = nodeRoot.GlobalPosition;
        _scanPulseRadius = 0f;
        _scanPulseActive = true;

        if (_scanPulseRing == null)
        {
            _scanPulseRing = new MeshInstance3D
            {
                Name = "ScanPulseRing",
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            AddChild(_scanPulseRing);
        }
    }

    /// <summary>
    /// Update scan pulse animation each frame.
    /// Called from _Process if scan is active.
    /// </summary>
    private void UpdateScanPulseV0(float delta)
    {
        if (!_scanPulseActive || _scanPulseRing == null) return;

        _scanPulseRadius += _scanPulseSpeed * delta;

        if (_scanPulseRadius >= _scanPulseMaxRadius)
        {
            _scanPulseActive = false;
            _scanPulseRing.Visible = false;
            return;
        }

        _scanPulseRing.Visible = true;
        _scanPulseRing.GlobalPosition = _scanPulseOrigin + new Vector3(0f, 2.0f, 0f);

        // Scale the ring as an expanding torus/disc.
        float fade = 1.0f - (_scanPulseRadius / _scanPulseMaxRadius);
        var pulseColor = new Color(0.4f, 0.85f, 1.0f, fade * 0.6f); // Cyan fade

        _scanPulseRing.Mesh = new TorusMesh
        {
            InnerRadius = _scanPulseRadius - 3.0f,
            OuterRadius = _scanPulseRadius + 3.0f,
        };

        _scanPulseRing.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = pulseColor,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(0.4f, 0.85f, 1.0f),
            EmissionEnergyMultiplier = 2.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }

    /// <summary>
    /// Highlight nodes that have discoverable sites. Yellow pulsing glow for SEEN,
    /// green for SCANNED, purple for anomalies.
    /// </summary>
    public void UpdateDiscoveryHighlightsV0()
    {
        // DiscHighlight spheres removed — were placeholder programmer art.
        foreach (var kv in _discoveryHighlightsByNodeId) kv.Value.QueueFree();
        _discoveryHighlightsByNodeId.Clear();
    }

    private void EnsureV2OverlayDisc(string nodeId, Vector3 worldPos, Color color, float size)
    {
        float radius = size * 0.5f;
        if (!_v2OverlayDiscsByNodeId.TryGetValue(nodeId, out var disc))
        {
            disc = new MeshInstance3D
            {
                Name = "V2Overlay_" + nodeId,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new CylinderMesh
                {
                    TopRadius = radius,
                    BottomRadius = radius,
                    Height = 0.5f,
                    RadialSegments = 32,
                    Rings = 0,
                },
            };
            _v2OverlayDiscsByNodeId[nodeId] = disc;
            AddChild(disc);
        }

        disc.GlobalPosition = worldPos + new Vector3(0f, 8.0f, 0f);

        // Update radius if needed.
        if (disc.Mesh is CylinderMesh cm && Mathf.Abs(cm.TopRadius - radius) > 1f)
        {
            cm.TopRadius = radius;
            cm.BottomRadius = radius;
        }

        disc.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            NoDepthTest = true,
            EmissionEnabled = true,
            Emission = new Color(color.R, color.G, color.B),
            EmissionEnergyMultiplier = 4.0f,
        };
    }

    /// <summary>
    /// Ensure the player's current node always has a disc in overlays that might
    /// otherwise be empty early-game (fleet, heat).
    /// </summary>
    private void EnsurePlayerFallbackDiscV0(HashSet<string> seenNodes, Color color, float size)
    {
        var ps = _bridge.GetPlayerStateV0();
        var playerNodeId = ps.ContainsKey("current_node_id") ? ps["current_node_id"].AsString() : "";
        if (!string.IsNullOrEmpty(playerNodeId) && !seenNodes.Contains(playerNodeId)
            && _nodeRootsById.TryGetValue(playerNodeId, out var playerRoot))
        {
            seenNodes.Add(playerNodeId);
            EnsureV2OverlayDisc(playerNodeId, playerRoot.GlobalPosition, color, size);
        }
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
        // GATE.S7.UI_WARFRONT.SUPPLY.001: Clear supply lines and objective markers.
        ClearSupplyLinesV0();
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
        // RouteWaypoint spheres removed — were placeholder programmer art.

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

    // GATE.T43.SCAN_UI.GALAXY_MARKERS.001: Update planet type markers + scan state rings on galaxy nodes.
    public void UpdateScanMarkersV0()
    {
        if (_bridge == null) return;

        foreach (var kv in _nodeRootsById)
        {
            var nodeId = kv.Key;
            var root = kv.Value;
            if (root == null || !root.IsInsideTree()) continue;

            var planetDot = root.GetNodeOrNull<MeshInstance3D>("PlanetDot");
            var scanRing = root.GetNodeOrNull<MeshInstance3D>("ScanRing");
            if (planetDot == null || scanRing == null) continue;

            var planetInfo = _bridge.GetPlanetInfoV0(nodeId);
            if (planetInfo.Count == 0)
            {
                planetDot.Visible = false;
                scanRing.Visible = false;
                continue;
            }

            // Show planet type dot with type-specific color.
            planetDot.Visible = true;
            var planetType = planetInfo.GetValueOrDefault("planet_type", "").ToString();
            var dotColor = GetPlanetTypeColor(planetType);
            if (planetDot.MaterialOverride is StandardMaterial3D dotMat)
            {
                dotMat.AlbedoColor = new Color(dotColor.R, dotColor.G, dotColor.B, 0.7f);
                dotMat.Emission = dotColor;
            }

            // Scan state ring: check scan results.
            var results = _bridge.GetPlanetScanResultsV0(nodeId);
            int scanCount = results.Count;
            if (scanCount > 0)
            {
                scanRing.Visible = true;
                // Yellow = partially scanned, Green = 3+ scans (thorough).
                bool thorough = scanCount >= 3;
                var ringColor = thorough
                    ? new Color(0.2f, 1.0f, 0.4f) // Green
                    : new Color(1.0f, 0.85f, 0.2f); // Yellow
                if (scanRing.MaterialOverride is StandardMaterial3D ringMat)
                {
                    ringMat.AlbedoColor = new Color(ringColor.R, ringColor.G, ringColor.B, 0.5f);
                    ringMat.Emission = ringColor;
                }
            }
            else
            {
                scanRing.Visible = false;
            }
        }

        // GATE.T43.SCAN_UI.SIGNAL_LINES.001: Signal triangulation lines between SignalLead nodes.
        UpdateSignalTriangulationV0();
    }

    // GATE.T43.SCAN_UI.SIGNAL_LINES.001: Draw dashed purple lines between nodes with SignalLead findings.
    private void UpdateSignalTriangulationV0()
    {
        // Clear previous lines.
        foreach (var line in _signalTriangulationLines)
        {
            if (GodotObject.IsInstanceValid(line))
                line.QueueFree();
        }
        _signalTriangulationLines.Clear();

        if (_bridge == null) return;

        // Collect nodes with SignalLead scan results.
        var signalNodes = new List<string>();
        foreach (var kv in _nodeRootsById)
        {
            var nodeId = kv.Key;
            var results = _bridge.GetPlanetScanResultsV0(nodeId);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].AsGodotDictionary().GetValueOrDefault("category", "").ToString() == "SignalLead")
                {
                    signalNodes.Add(nodeId);
                    break;
                }
            }
        }

        if (signalNodes.Count < 2) return;

        // Dashed purple material.
        var lineMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.6f, 0.3f, 1.0f, 0.4f),
            EmissionEnabled = true,
            Emission = new Color(0.6f, 0.3f, 1.0f),
            EmissionEnergyMultiplier = 2.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        // Draw lines between all signal lead pairs.
        for (int i = 0; i < signalNodes.Count; i++)
        {
            for (int j = i + 1; j < signalNodes.Count; j++)
            {
                if (!_nodeRootsById.TryGetValue(signalNodes[i], out var fromRoot)) continue;
                if (!_nodeRootsById.TryGetValue(signalNodes[j], out var toRoot)) continue;

                var fromPos = fromRoot.GlobalPosition + new Vector3(0f, 4.0f, 0f);
                var toPos = toRoot.GlobalPosition + new Vector3(0f, 4.0f, 0f);
                var dist = fromPos.DistanceTo(toPos);

                // Dashed segments: 80u dash, 40u gap.
                float dashLen = 80.0f;
                float gapLen = 40.0f;
                float totalLen = dashLen + gapLen;
                int dashCount = Mathf.Max(1, (int)(dist / totalLen));
                var dir = (toPos - fromPos).Normalized();

                for (int d = 0; d < dashCount; d++)
                {
                    var segStart = fromPos + dir * (d * totalLen);
                    var segEnd = fromPos + dir * (d * totalLen + dashLen);
                    // Clamp to line endpoint.
                    if (segStart.DistanceTo(fromPos) > dist) break;
                    if (segEnd.DistanceTo(fromPos) > dist) segEnd = toPos;

                    var seg = new MeshInstance3D
                    {
                        Name = "SignalLine_" + i + "_" + j + "_" + d,
                        Mesh = new CylinderMesh { TopRadius = 6.0f, BottomRadius = 6.0f, Height = 1.0f },
                        MaterialOverride = lineMat,
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    };
                    AddChild(seg);
                    UpdateEdgeTransformV0(seg, segStart, segEnd);
                    _signalTriangulationLines.Add(seg);
                }
            }
        }
    }

    // ── GATE.T44.AMBIENT: Economy-driven ambient visuals ──
    // Reads GetNodeEconomySnapshotV0 and spawns GDScript visual components.
    private void SpawnAmbientEconomyVisualsV0(string nodeId)
    {
        if (_bridge == null) return;
        var ecoSnap = _bridge.Call("GetNodeEconomySnapshotV0", nodeId).AsGodotDictionary();
        if (ecoSnap == null || ecoSnap.Count == 0) return;

        int trafficLevel = ecoSnap.ContainsKey("traffic_level") ? (int)ecoSnap["traffic_level"] : 0;
        float prosperity = ecoSnap.ContainsKey("prosperity") ? (float)ecoSnap["prosperity"] : 0.5f;
        string industryType = ecoSnap.ContainsKey("industry_type") ? (string)ecoSnap["industry_type"] : "none";
        int warfrontTier = ecoSnap.ContainsKey("warfront_tier") ? (int)ecoSnap["warfront_tier"] : 0;
        string factionId = ecoSnap.ContainsKey("faction_id") ? (string)ecoSnap["faction_id"] : "";

        // Resolve faction color for shuttles.
        var factionColor = new Color(0.5f, 0.5f, 0.6f);
        if (_bridge != null && !string.IsNullOrEmpty(factionId))
        {
            var fColors = _bridge.GetFactionColorsV0(factionId);
            if (fColors.ContainsKey("primary"))
                factionColor = (Color)fColors["primary"];
        }

        // GATE.T44.AMBIENT.SHUTTLE_TRAFFIC.001: Cosmetic shuttles (1-5 based on traffic).
        int shuttleCount = Math.Clamp(trafficLevel, 0, 5);
        var shuttleScript = GD.Load<Script>("res://scripts/view/ambient_shuttle.gd");
        if (shuttleScript != null && shuttleCount > 0)
        {
            for (int i = 0; i < shuttleCount; i++)
            {
                var shuttle = new Node3D();
                shuttle.SetScript(shuttleScript);
                _localSystemRoot.AddChild(shuttle);
                shuttle.Call("setup", nodeId + "_" + i, 5.0f + i * 2.0f, factionColor);
            }
        }

        // GATE.T44.AMBIENT.MINING_VFX.001: Extraction beam at mine/fuel_well nodes.
        if (industryType == "mine" || industryType == "fuel_well")
        {
            var miningScript = GD.Load<Script>("res://scripts/vfx/mining_beam_vfx.gd");
            if (miningScript != null)
            {
                var beam = new Node3D();
                beam.SetScript(miningScript);
                _localSystemRoot.AddChild(beam);
                beam.Call("setup", industryType, prosperity);
                beam.Position = new Vector3(8f, 0f, 8f); // Offset from station
            }
        }

        // GATE.T44.AMBIENT.PROSPERITY.001: Station lighting tiers.
        var prosperityScript = GD.Load<Script>("res://scripts/view/station_prosperity.gd");
        if (prosperityScript != null)
        {
            var prosLight = new Node3D();
            prosLight.SetScript(prosperityScript);
            _localSystemRoot.AddChild(prosLight);
            prosLight.Call("setup", prosperity);
        }

        // GATE.T44.AMBIENT.WARFRONT_ATMO.001: Red particles at warfront nodes.
        if (warfrontTier > 0)
        {
            var warfrontScript = GD.Load<Script>("res://scripts/vfx/warfront_atmosphere.gd");
            if (warfrontScript != null)
            {
                var atmo = new Node3D();
                atmo.SetScript(warfrontScript);
                _localSystemRoot.AddChild(atmo);
                atmo.Call("setup", warfrontTier);
            }
        }

        // GATE.T44.STATION.NAMEPLATE.001: Station name + faction insignia Label3D.
        var nameplateScript = GD.Load<Script>("res://scripts/view/station_nameplate.gd");
        if (nameplateScript != null)
        {
            var nameplate = new Node3D();
            nameplate.SetScript(nameplateScript);
            _localSystemRoot.AddChild(nameplate);
            nameplate.Call("setup", SimBridge.FormatDisplayNameV0(nodeId), factionId, factionColor);
        }

        // GATE.T44.DIGEST.CONSTRUCTION_VFX.001: Megaproject construction sparks.
        if (_bridge.HasMethod("GetMegaprojectsV0"))
        {
            var projects = _bridge.Call("GetMegaprojectsV0").AsGodotArray();
            if (projects != null)
            {
                foreach (var proj in projects)
                {
                    var pd = proj.AsGodotDictionary();
                    if (pd == null) continue;
                    var pNodeId = pd.ContainsKey("node_id") ? (string)pd["node_id"] : "";
                    if (pNodeId != nodeId) continue;
                    int stage = pd.ContainsKey("current_stage") ? (int)pd["current_stage"] : 0;
                    int totalStages = pd.ContainsKey("total_stages") ? (int)pd["total_stages"] : 1;
                    if (stage < totalStages) // Active construction
                    {
                        var conScript = GD.Load<Script>("res://scripts/vfx/construction_vfx.gd");
                        if (conScript != null)
                        {
                            var vfx = new Node3D();
                            vfx.SetScript(conScript);
                            _localSystemRoot.AddChild(vfx);
                            vfx.Call("setup", stage, totalStages);
                        }
                    }
                }
            }
        }
    }

    // ── GATE.T44: Throttled galaxy-scale economy visual refresh ──
    private void RefreshLaneTrafficAndMegaprojectsV0()
    {
        var snap = _bridge?.GetGalaxySnapshotV0();
        if (snap == null || !snap.ContainsKey("system_nodes")) return;

        var rawNodes = (Godot.Collections.Array)snap["system_nodes"];
        var rawEdges = snap.ContainsKey("lane_edges") ? (Godot.Collections.Array)snap["lane_edges"] : new Godot.Collections.Array();

        float galScale = SimCore.Tweaks.RealSpaceTweaksV0.GalacticScaleFactor;
        var nodeMap = new Dictionary<string, NodeSnapV0>();
        var nodeList = new List<NodeSnapV0>();
        foreach (Variant v in rawNodes)
        {
            if (v.VariantType != Variant.Type.Dictionary) continue;
            var n = v.AsGodotDictionary();
            var nodeId = n.ContainsKey("node_id") ? (string)(Variant)n["node_id"] : "";
            float x = (n.ContainsKey("pos_x") ? (float)(Variant)n["pos_x"] : 0f) * galScale;
            float y = (n.ContainsKey("pos_y") ? (float)(Variant)n["pos_y"] : 0f) * galScale;
            float z = (n.ContainsKey("pos_z") ? (float)(Variant)n["pos_z"] : 0f) * galScale;
            var ns = new NodeSnapV0(nodeId, "", "", new Vector3(x, y, z));
            nodeMap[nodeId] = ns;
        }

        var edgeList = new List<EdgeSnapV0>();
        foreach (Variant v in rawEdges)
        {
            if (v.VariantType != Variant.Type.Dictionary) continue;
            var e = v.AsGodotDictionary();
            edgeList.Add(new EdgeSnapV0(
                e.ContainsKey("from_id") ? (string)(Variant)e["from_id"] : "",
                e.ContainsKey("to_id") ? (string)(Variant)e["to_id"] : ""));
        }

        RefreshLaneTrafficV0(edgeList, nodeMap);
        RefreshMegaprojectMarkersV0(nodeMap);
    }

    // ── GATE.T44.AMBIENT.LANE_TRAFFIC.001: Spawn lane traffic sprites on galaxy map ──
    // Called from RefreshLaneTrafficAndMegaprojectsV0 (throttled every 5s).
    private readonly Dictionary<string, List<Node3D>> _laneTrafficByEdge = new();

    private void RefreshLaneTrafficV0(List<EdgeSnapV0> edges, Dictionary<string, NodeSnapV0> nodeMap)
    {
        // Clean up old sprites.
        foreach (var kvp in _laneTrafficByEdge)
        {
            foreach (var n in kvp.Value)
            {
                if (IsInstanceValid(n)) n.QueueFree();
            }
        }
        _laneTrafficByEdge.Clear();

        if (_bridge == null || !_overlayOpen) return;

        var trafficScript = GD.Load<Script>("res://scripts/view/lane_traffic_sprite.gd");
        if (trafficScript == null) return;

        foreach (var edge in edges)
        {
            if (!nodeMap.TryGetValue(edge.FromId, out var fromNode)) continue;
            if (!nodeMap.TryGetValue(edge.ToId, out var toNode)) continue;

            // Average traffic of connected nodes.
            int avgTraffic = 0;
            var ecoFrom = _bridge.Call("GetNodeEconomySnapshotV0", edge.FromId).AsGodotDictionary();
            var ecoTo = _bridge.Call("GetNodeEconomySnapshotV0", edge.ToId).AsGodotDictionary();
            if (ecoFrom != null && ecoFrom.ContainsKey("traffic_level"))
                avgTraffic += (int)ecoFrom["traffic_level"];
            if (ecoTo != null && ecoTo.ContainsKey("traffic_level"))
                avgTraffic += (int)ecoTo["traffic_level"];
            avgTraffic /= 2; // STRUCTURAL: average of two endpoints

            int spriteCount = Math.Clamp(avgTraffic / 2, 0, 3); // STRUCTURAL: 0-3 sprites per lane
            if (spriteCount == 0) continue;

            var edgeKey = edge.FromId + "|" + edge.ToId;
            var sprites = new List<Node3D>();

            for (int i = 0; i < spriteCount; i++)
            {
                var sprite = new Node3D();
                sprite.SetScript(trafficScript);
                AddChild(sprite); // Add to GalaxyView (galactic scale)
                float speed = 0.5f + (float)(Fnv1a64(edgeKey + "_" + i) % 100UL) / 66f;
                sprite.Call("setup", fromNode.Position, toNode.Position, speed);
                sprites.Add(sprite);
            }
            _laneTrafficByEdge[edgeKey] = sprites;
        }
    }

    // ── GATE.T44.DIGEST.MEGAPROJECT_MAP.001: Megaproject markers on galaxy map ──
    private readonly Dictionary<string, Node3D> _megaprojectMarkersByNodeId = new();

    private void RefreshMegaprojectMarkersV0(Dictionary<string, NodeSnapV0> nodeMap)
    {
        // Clean up old markers.
        foreach (var kvp in _megaprojectMarkersByNodeId)
        {
            if (IsInstanceValid(kvp.Value)) kvp.Value.QueueFree();
        }
        _megaprojectMarkersByNodeId.Clear();

        if (_bridge == null || !_bridge.HasMethod("GetMegaprojectsV0")) return;

        var projects = _bridge.Call("GetMegaprojectsV0").AsGodotArray();
        if (projects == null || projects.Count == 0) return;

        foreach (var proj in projects)
        {
            var pd = proj.AsGodotDictionary();
            if (pd == null) continue;
            var pNodeId = pd.ContainsKey("node_id") ? (string)pd["node_id"] : "";
            if (string.IsNullOrEmpty(pNodeId) || !nodeMap.ContainsKey(pNodeId)) continue;

            int stage = pd.ContainsKey("current_stage") ? (int)pd["current_stage"] : 0;
            int totalStages = pd.ContainsKey("total_stages") ? (int)pd["total_stages"] : 1;
            string typeId = pd.ContainsKey("type_id") ? (string)pd["type_id"] : "";

            // Color by type.
            var markerColor = typeId switch
            {
                "anchor" => new Color(0.3f, 0.5f, 1.0f),     // Blue
                "corridor" => new Color(0.3f, 0.9f, 0.4f),   // Green
                "pylon" => new Color(1.0f, 0.8f, 0.2f),      // Amber
                _ => new Color(0.7f, 0.7f, 0.7f),
            };

            var nodePos = nodeMap[pNodeId].Position;

            // Ring progress indicator.
            float progress = totalStages > 0 ? (float)stage / totalStages : 0f;
            var ringMat = new StandardMaterial3D
            {
                AlbedoColor = markerColor,
                EmissionEnabled = true,
                Emission = markerColor,
                EmissionEnergyMultiplier = 2.0f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            };

            var ring = new MeshInstance3D
            {
                Name = "MegaprojectMarker_" + pNodeId,
                Mesh = new TorusMesh
                {
                    InnerRadius = 25f,
                    OuterRadius = 30f,
                    Rings = 24,
                    RingSegments = 8,
                },
                MaterialOverride = ringMat,
                Position = nodePos + Vector3.Up * 10f,
            };
            ring.RotateX(Mathf.Pi / 2.0f);
            // Scale ring by progress (partial ring effect via material alpha).
            ringMat.AlbedoColor = new Color(markerColor.R, markerColor.G, markerColor.B, 0.3f + 0.7f * progress);

            // Label.
            var label = new Label3D
            {
                Text = typeId.Capitalize() + " " + stage + "/" + totalStages,
                FontSize = 28,
                PixelSize = 0.5f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Position = nodePos + Vector3.Up * 50f,
                Modulate = markerColor,
                OutlineSize = 3,
            };

            var marker = new Node3D { Name = "MegaprojectGroup_" + pNodeId };
            marker.AddChild(ring);
            marker.AddChild(label);
            AddChild(marker);
            _megaprojectMarkersByNodeId[pNodeId] = marker;
        }
    }

    private static Color GetPlanetTypeColor(string planetType) => planetType switch
    {
        "Sand" => new Color(0.76f, 0.6f, 0.35f),        // Brown
        "Ice" => new Color(0.85f, 0.92f, 1.0f),          // White-blue
        "Lava" => new Color(1.0f, 0.3f, 0.15f),          // Red
        "Gaseous" => new Color(0.3f, 0.5f, 0.9f),        // Blue
        "Barren" => new Color(0.5f, 0.5f, 0.5f),         // Grey
        "Terrestrial" => new Color(0.3f, 0.8f, 0.4f),    // Green
        _ => new Color(0.6f, 0.6f, 0.6f),
    };
}
