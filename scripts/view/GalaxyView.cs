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
    Threat = 6,       // GATE.T61.SECURITY.THREAT_MAP.001
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
    [Export] public float DiscoverySiteMarkerRadiusU { get; set; } = 0.5f;
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

    // GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Scan hold timer + progress ring VFX.
    private const float ScanCeremonyDurationSeconds = 4.0f;
    private bool _scanCeremonyActive = false;
    private float _scanCeremonyElapsed = 0f;
    private string _scanCeremonySiteId = "";
    private MeshInstance3D _scanCeremonyRing;
    private StandardMaterial3D _scanCeremonyMat;
    private bool _scanCeremonyCelebrating = false;
    private float _scanCeremonyCelebrationElapsed = 0f;
    private const float ScanCeremonyCelebrationDuration = 1.5f; // 0.5s hold + 1.0s fade
    private Color _scanCeremonyFamilyColor = new Color(0.4f, 0.85f, 1.0f); // default cyan

    // GATE.T43.SCAN_UI.SIGNAL_LINES.001: Signal triangulation lines between SignalLead nodes.
    private readonly List<MeshInstance3D> _signalTriangulationLines = new();

    // GATE.T59.DISC_VIZ.APPROACH_FEEDBACK.001: Distance-based discovery approach feedback.
    // Presentation-only thresholds (NOT SimCore tweaks).
    private const float DiscoveryBlipRange = 30.0f;       // >30u: scanner blip only (faint glow)
    private const float DiscoverySilhouetteRange = 15.0f;  // 15-30u: silhouette alpha ramp; <15u: full detail
    private double _discoveryApproachTime = 0.0;           // Monotonic time for scanner ping sine wave.

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

            // GATE.T65.SPATIAL.LANE_GATE_VIS.001: Show persistent lanes at flight altitude (>=50u).
            // Previously hidden below 600u — caused INVISIBLE_LANES in fh_6 verification.
            // Flight-altitude lanes are faint (alpha ramps from 0→0.4 over 50-200u),
            // then full brightness at galaxy-map altitude (600u+).
            bool shouldShow = altitude >= 50f;
            _persistentLanesRoot.Visible = shouldShow;

            if (shouldShow && _sharedLaneMaterial != null)
            {
                float fadeAlpha;
                if (altitude < 200f)
                    fadeAlpha = (altitude - 50f) / 150f * 0.4f; // 0→0.4 over 50-200u (faint at flight)
                else if (altitude < 600f)
                    fadeAlpha = 0.4f + (altitude - 200f) / 400f * 0.2f; // 0.4→0.6 over 200-600u
                else if (altitude < 750f)
                    fadeAlpha = 0.6f + (altitude - 600f) / 150f * 0.4f; // 0.6→1 over 600-750u
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

            // GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Update scan ceremony progress ring + celebration.
            // UpdateScanCeremonyV0((float)delta); // TODO: implement in T59

            // GATE.T59.DISC_VIZ.APPROACH_FEEDBACK.001: Distance-based alpha/emission/ping on discovery sites.
            // _discoveryApproachTime += delta; // TODO: implement in T59
            // UpdateDiscoveryApproachFeedbackV0((float)delta); // TODO: implement in T59
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

                // GATE.T63.SPATIAL.LANE_LABELS.001: Lane gate destination labels use wider
                // distance thresholds so they remain readable at ~200u flight camera altitude.
                // Camera at 200u altitude + gate at 130u horizontal ≈ 238u slant distance.
                bool isGateLabel = label.IsInGroup("GateDestLabel");
                if (isGateLabel)
                {
                    if (dist < 5f)
                    {
                        label.Visible = false;
                    }
                    else if (dist < 40f)
                    {
                        float t = (dist - 5f) / 35f;
                        float scale = Mathf.Clamp(t, 0.3f, 1f);
                        label.PixelSize = 0.25f * scale;
                        label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, scale);
                        label.Visible = true;
                    }
                    else if (dist > 350f)
                    {
                        label.Visible = false;
                    }
                    else if (dist > 280f)
                    {
                        float alpha = Mathf.Clamp(1f - (dist - 280f) / 70f, 0f, 1f);
                        label.PixelSize = 0.25f;
                        label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, alpha);
                        label.Visible = alpha > 0.01f;
                    }
                    else
                    {
                        label.PixelSize = 0.25f;
                        label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, 1f);
                        label.Visible = true;
                    }
                }
                // Default label clamping for all other labels.
                else if (dist < 5f)
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
                case Key.T when !key.CtrlPressed:
                    ToggleV2OverlayV0(GalaxyMapV2Overlay.Threat);
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
}
