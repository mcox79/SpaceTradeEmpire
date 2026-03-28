using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView
{
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

                // GATE.T41.UI.FACTION_COLORS.001: Tint node label by controlling faction's primary color.
                // Known nodes with a faction get a 60% faction tint over the default gray.
                // Rumored/unknown nodes stay default gray for fog-of-war clarity.
                if (isKnownNode && !string.IsNullOrEmpty(n.FactionId))
                {
                    var defaultLabelColor = new Color(0.85f, 0.85f, 0.9f);
                    label.Modulate = defaultLabelColor.Lerp(n.FactionColor, 0.6f);
                }
                else
                {
                    label.Modulate = new Color(0.85f, 0.85f, 0.9f);
                }

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
                    // GATE.T41.SPATIAL.PLAYER_MARKER.001: Bright cyan player node beacon.
                    mat.AlbedoColor = new Color(0.3f, 0.9f, 1.0f);
                    mat.EmissionEnabled = true;
                    mat.Emission = new Color(0.3f, 0.9f, 1.0f);
                    mat.EmissionEnergyMultiplier = 14.0f;

                    // GATE.T41.SPATIAL.PLAYER_MARKER.001: "YOU ARE HERE" label + pulsing cyan/gold rings
                    if (root.GetNodeOrNull("YouLabel") == null)
                    {
                        var youLabel = new Label3D
                        {
                            Name = "YouLabel",
                            Text = "▼ YOU ARE HERE ▼",
                            PixelSize = 4.0f,
                            FontSize = 84,
                            OutlineSize = 18,
                            Modulate = new Color(0.3f, 0.95f, 1.0f),
                            OutlineModulate = new Color(0.0f, 0.0f, 0.0f, 1.0f),
                            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                            Position = new Vector3(0, 300f, 0),
                            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                            NoDepthTest = true,
                        };
                        root.AddChild(youLabel);

                        // Inner ring: bright cyan with alpha pulse (0.5-1.0, period ~1.5s).
                        var ringMat = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(0.3f, 0.95f, 1.0f, 1.0f),
                            EmissionEnabled = true,
                            Emission = new Color(0.3f, 0.9f, 1.0f),
                            EmissionEnergyMultiplier = 18.0f,
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

                        // Outer ring: warm gold, counter-pulse for visual depth.
                        var outerRingMat = new StandardMaterial3D
                        {
                            AlbedoColor = new Color(1.0f, 0.85f, 0.2f, 0.6f),
                            EmissionEnabled = true,
                            Emission = new Color(1.0f, 0.8f, 0.1f),
                            EmissionEnergyMultiplier = 8.0f,
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

    // GATE.T41.SPATIAL.PLAYER_MARKER.001: Pulse player rings — alpha + scale, period ~1.5s.
    private void _PulsePlayerRingV0(double delta)
    {
        _playerRingPulseTime += delta;
        // Period ~1.5s → angular frequency = 2*PI / 1.5 ≈ 4.19
        float t = (float)_playerRingPulseTime * 4.19f;
        foreach (var kv in _nodeRootsById)
        {
            var ring = kv.Value.GetNodeOrNull<MeshInstance3D>("PlayerRing");
            if (ring == null) continue;
            // Scale pulse: 1.0 - 1.25
            float s = 1.0f + 0.25f * Mathf.Sin(t);
            ring.Scale = new Vector3(s, s, s);
            // Alpha pulse: 0.5 - 1.0 on inner ring material
            if (ring.MaterialOverride is StandardMaterial3D innerMat)
            {
                float alpha = 0.75f + 0.25f * Mathf.Sin(t);
                innerMat.AlbedoColor = new Color(innerMat.AlbedoColor.R, innerMat.AlbedoColor.G, innerMat.AlbedoColor.B, alpha);
            }
            // Outer ring: counter-phase alpha (0.3 - 0.7) for visual depth
            var outerRing = kv.Value.GetNodeOrNull<MeshInstance3D>("PlayerOuterRing");
            if (outerRing != null && outerRing.MaterialOverride is StandardMaterial3D outerMat)
            {
                float outerAlpha = 0.5f + 0.2f * Mathf.Sin(t + Mathf.Pi); // Counter-phase
                outerMat.AlbedoColor = new Color(outerMat.AlbedoColor.R, outerMat.AlbedoColor.G, outerMat.AlbedoColor.B, outerAlpha);
                float os = 1.0f + 0.15f * Mathf.Sin(t + Mathf.Pi);
                outerRing.Scale = new Vector3(os, os, os);
            }
            // "YOU ARE HERE" label: gentle alpha pulse
            var youLabel = kv.Value.GetNodeOrNull<Label3D>("YouLabel");
            if (youLabel != null)
            {
                float labelAlpha = 0.7f + 0.3f * Mathf.Sin(t);
                youLabel.Modulate = new Color(youLabel.Modulate.R, youLabel.Modulate.G, youLabel.Modulate.B, labelAlpha);
            }
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
}
