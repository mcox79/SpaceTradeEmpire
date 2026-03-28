using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView
{
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

    // ════════════════════════════════════════════════════════════════════════
    // GATE.T59.DISC_VIZ.SCAN_CEREMONY.001: Scan hold timer + progress ring VFX + celebration
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Begin a scan ceremony for a discovery site. Creates a progress ring torus
    /// around the discovery marker that fills over SCAN_DURATION_SECONDS.
    /// Called from DiscoverySitePanel when player presses Scan.
    /// </summary>
    public void BeginScanCeremonyV0(string siteId, string familyHint)
    {
        // Cancel any existing ceremony.
        CancelScanCeremonyV0();

        _scanCeremonySiteId = siteId;
        _scanCeremonyElapsed = 0f;
        _scanCeremonyActive = true;
        _scanCeremonyCelebrating = false;
        _scanCeremonyCelebrationElapsed = 0f;

        // Determine family color from hint (or default cyan).
        _scanCeremonyFamilyColor = string.IsNullOrEmpty(familyHint)
            ? new Color(0.4f, 0.85f, 1.0f)
            : GetFamilyColor(familyHint);

        // Find the discovery site Node3D in the local system by group.
        Node3D siteMarker = FindDiscoverySiteMarkerV0(siteId);
        if (siteMarker == null)
        {
            _scanCeremonyActive = false;
            return;
        }

        // Create progress ring torus around the discovery site.
        _scanCeremonyMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(_scanCeremonyFamilyColor.R, _scanCeremonyFamilyColor.G, _scanCeremonyFamilyColor.B, 0.6f),
            EmissionEnabled = true,
            Emission = _scanCeremonyFamilyColor,
            EmissionEnergyMultiplier = 2.0f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            NoDepthTest = true,
        };

        _scanCeremonyRing = new MeshInstance3D
        {
            Name = "ScanCeremonyRing_" + siteId,
            Mesh = new TorusMesh
            {
                InnerRadius = DiscoverySiteMarkerRadiusU * 6.0f,
                OuterRadius = DiscoverySiteMarkerRadiusU * 8.0f,
            },
            MaterialOverride = _scanCeremonyMat,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Rotation = new Vector3(Mathf.Pi / 2f, 0f, 0f), // Lie flat in XZ plane.
        };

        siteMarker.AddChild(_scanCeremonyRing);
        // Start at zero scale — tween up.
        _scanCeremonyRing.Scale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    /// <summary>
    /// Cancel an in-progress scan ceremony (player moved away or cancelled).
    /// </summary>
    public void CancelScanCeremonyV0()
    {
        _scanCeremonyActive = false;
        _scanCeremonyCelebrating = false;
        _scanCeremonyElapsed = 0f;
        _scanCeremonyCelebrationElapsed = 0f;
        _scanCeremonySiteId = "";

        if (_scanCeremonyRing != null && GodotObject.IsInstanceValid(_scanCeremonyRing))
        {
            _scanCeremonyRing.QueueFree();
        }
        _scanCeremonyRing = null;
        _scanCeremonyMat = null;
    }

    /// <summary>
    /// Returns true if a scan ceremony is currently active (holding or celebrating).
    /// </summary>
    public bool IsScanCeremonyActiveV0() => _scanCeremonyActive || _scanCeremonyCelebrating;

    /// <summary>
    /// Returns the current scan progress as a float [0.0, 1.0].
    /// </summary>
    public float GetScanCeremonyProgressV0() => _scanCeremonyActive
        ? Mathf.Clamp(_scanCeremonyElapsed / ScanCeremonyDurationSeconds, 0f, 1f)
        : (_scanCeremonyCelebrating ? 1f : 0f);

    /// <summary>
    /// Per-frame update for scan ceremony progress ring + celebration flash.
    /// </summary>
    private void UpdateScanCeremonyV0(float delta)
    {
        if (!_scanCeremonyActive && !_scanCeremonyCelebrating) return;
        if (_scanCeremonyRing == null || !GodotObject.IsInstanceValid(_scanCeremonyRing))
        {
            // Ring was freed externally — reset state.
            _scanCeremonyActive = false;
            _scanCeremonyCelebrating = false;
            return;
        }

        if (_scanCeremonyActive)
        {
            _scanCeremonyElapsed += delta;
            float progress = Mathf.Clamp(_scanCeremonyElapsed / ScanCeremonyDurationSeconds, 0f, 1f);

            // Scale from 0 → 1 as progress increases (ring "grows in").
            float scaleT = Mathf.Clamp(progress * 4f, 0f, 1f); // Reaches full size at 25% progress.
            float s = Mathf.Lerp(0.01f, 1.0f, scaleT);
            _scanCeremonyRing.Scale = new Vector3(s, s, s);

            // Pulsing emission: base 2.0 + sine pulse that intensifies with progress.
            float pulse = 1.0f + 0.5f * MathF.Sin((float)(_scanCeremonyElapsed * 4.0f * Math.PI));
            float emissionEnergy = Mathf.Lerp(2.0f, 5.0f, progress) * pulse;

            // Color alpha fills from 0.3 → 0.8 as ring progresses.
            float alpha = Mathf.Lerp(0.3f, 0.8f, progress);
            _scanCeremonyMat.AlbedoColor = new Color(
                _scanCeremonyFamilyColor.R, _scanCeremonyFamilyColor.G, _scanCeremonyFamilyColor.B, alpha);
            _scanCeremonyMat.EmissionEnergyMultiplier = emissionEnergy;

            // Rotate the ring slowly during scan for visual interest.
            _scanCeremonyRing.RotateY(delta * 1.5f);

            if (progress >= 1.0f)
            {
                // Scan hold complete — transition to celebration phase.
                _scanCeremonyActive = false;
                _scanCeremonyCelebrating = true;
                _scanCeremonyCelebrationElapsed = 0f;

                // Spike emission to max for the celebration flash.
                _scanCeremonyMat.EmissionEnergyMultiplier = 10.0f;
                _scanCeremonyMat.AlbedoColor = new Color(1f, 1f, 1f, 0.9f);
                _scanCeremonyMat.Emission = new Color(1f, 1f, 1f);

                // Dispatch the actual scan command now that the hold is complete.
                if (_bridge != null)
                {
                    _bridge.DispatchScanDiscoveryV0(_scanCeremonySiteId);
                }

                // Emit signal for UI to react.
                EmitSignal(SignalName.ScanCeremonyCompleted, _scanCeremonySiteId);
            }
        }
        else if (_scanCeremonyCelebrating)
        {
            _scanCeremonyCelebrationElapsed += delta;
            float celebT = _scanCeremonyCelebrationElapsed / ScanCeremonyCelebrationDuration;

            if (celebT <= 0.33f)
            {
                // Hold phase (0.5s of 1.5s total): ring stays bright white.
                // No changes — keep the spike.
            }
            else if (celebT <= 1.0f)
            {
                // Fade phase: emission fades from 10 → 0, alpha fades out.
                float fadeProgress = (celebT - 0.33f) / 0.67f; // 0 → 1 over fade period.
                float emFade = Mathf.Lerp(10.0f, 0.0f, fadeProgress);
                float alphaFade = Mathf.Lerp(0.9f, 0.0f, fadeProgress);
                _scanCeremonyMat.EmissionEnergyMultiplier = emFade;
                _scanCeremonyMat.AlbedoColor = new Color(1f, 1f, 1f, alphaFade);

                // Scale up slightly during fade for "burst" feel.
                float burstScale = Mathf.Lerp(1.0f, 1.5f, fadeProgress);
                _scanCeremonyRing.Scale = new Vector3(burstScale, burstScale, burstScale);
            }
            else
            {
                // Celebration complete — clean up.
                CancelScanCeremonyV0();
            }
        }
    }

    /// <summary>
    /// Find the discovery site marker Node3D by site_id in the DiscoverySite group.
    /// </summary>
    private Node3D FindDiscoverySiteMarkerV0(string siteId)
    {
        if (_localSystemRoot == null || !_localSystemRoot.IsInsideTree()) return null;

        // Discovery sites are added to group "DiscoverySite" and named "DiscoverySite_{siteId}".
        var expectedName = "DiscoverySite_" + siteId;
        foreach (var node in GetTree().GetNodesInGroup("DiscoverySite"))
        {
            if (node is Node3D n3d && n3d.Name == expectedName)
                return n3d;
        }
        return null;
    }

    // Signal emitted when scan ceremony completes (scan hold finished + scan dispatched).
    [Signal]
    public delegate void ScanCeremonyCompletedEventHandler(string siteId);
}
