using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView
{
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
            // GATE.T65.PERF.FPS_PROFILE.001: Cull distant system details to improve FPS.
            // Orbital disc only visible within 2500u (~2 systems away). Fades over 500u.
            VisibilityRangeEnd = 2500f,
            VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
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
                // GATE.T65.PERF.FPS_PROFILE.001: Cull distant planet dots to improve FPS.
                VisibilityRangeEnd = 2500f,
                VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
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
            mesh.AddToGroup("LaneLine"); // GATE.T65.SPATIAL.LANE_GATE_VIS.001: Group for bot detection.
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
            // GATE.T65.PERF.FPS_PROFILE.001: Reduced from 80→50 particles.
            Amount = 50,
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
                // GATE.T65.PERF.FPS_PROFILE.001: Reduced from 30→20 particles.
                Amount = 20,
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
            // GATE.T59.DISC_VIZ.FAMILY_PHASE.001: Extract family and phase for per-family procedural visuals.
            var family = s.ContainsKey("family") ? (string)s["family"] : "";
            var phase = s.ContainsKey("phase_token") ? (string)s["phase_token"] : "SEEN";

            var marker = CreateDiscoverySiteMarkerV0(siteId, family, phase);
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

}
