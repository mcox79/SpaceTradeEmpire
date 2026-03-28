using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView
{
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

            // GATE.T41.UI.FACTION_COLORS.001: Use authoritative faction accent color from FactionTweaksV0.
            // Falls back to role-based coloring only for legacy faction IDs (faction_0/1/2).
            Color labelColor;
            if (_bridge != null && _bridge.HasMethod("GetFactionColorsV0"))
            {
                var fColors = _bridge.Call("GetFactionColorsV0", factionId).AsGodotDictionary();
                if (fColors != null && fColors.ContainsKey("found") && (bool)fColors["found"])
                {
                    labelColor = (Color)fColors["accent"];
                }
                else
                {
                    // Legacy fallback for synthetic faction IDs.
                    labelColor = roleTag switch
                    {
                        "Trader" => new Color(0.2f, 1.0f, 0.4f),
                        "Miner"  => new Color(1.0f, 0.6f, 0.1f),
                        "Pirate" => new Color(1.0f, 0.2f, 0.2f),
                        _        => new Color(0.9f, 0.9f, 0.9f),
                    };
                }
            }
            else
            {
                labelColor = new Color(0.9f, 0.9f, 0.9f);
            }

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

            // GATE.T41.UI.FACTION_COLORS.001: Show capitalized faction name instead of legacy role tag.
            string displayName = factionId;
            if (displayName.Length > 0)
                displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            lbl.Text = displayName;
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
        if (mode >= 0 && mode <= 6)
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
            case GalaxyMapV2Overlay.Threat:
                UpdateThreatOverlayDiscsV0();
                break;
            default:
                ClearV2OverlayV0();
                break;
        }
    }

    // GATE.T41.UI.FACTION_COLORS.001: Faction colors from FactionTweaksV0 constants.
    // Returns the faction's primary color at overlay alpha. Falls back to hash-to-hue for unknown IDs.
    private static Color FactionOverlayColorV0(string factionId)
    {
        if (string.IsNullOrEmpty(factionId)) return new Color(0.5f, 0.5f, 0.5f, 0.3f);

        var colors = SimCore.Tweaks.FactionTweaksV0.GetFactionColors(factionId);
        // GetFactionColors returns gray (0.5,0.5,0.5) for unknown factions — detect and fallback.
        bool isKnown = colors.Primary.R != 0.5f || colors.Primary.G != 0.5f || colors.Primary.B != 0.5f;
        if (isKnown)
        {
            return new Color(colors.Primary.R, colors.Primary.G, colors.Primary.B, 0.35f);
        }

        // Fallback: hash-to-hue for unrecognized faction IDs.
        uint hash = 0;
        foreach (char c in factionId) { hash = hash * 31 + (uint)c; } // STRUCTURAL: hash computation
        float hue = (hash % 360) / 360.0f;                            // STRUCTURAL: hue from hash
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

    // GATE.T61.SECURITY.THREAT_MAP.001: Threat heat overlay — combined security band + heat.
    // Color: green (safe) -> yellow (moderate) -> orange (dangerous) -> red (hostile/high heat).
    private void UpdateThreatOverlayDiscsV0()
    {
        var data = _bridge.GetThreatOverlayV0();
        var seenNodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in data.Keys)
        {
            var nodeId = key.AsString();
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!_nodeRootsById.TryGetValue(nodeId, out var nodeRoot)) continue;

            var info = data[key].AsGodotDictionary();
            float threat = info.ContainsKey("threat") ? (float)info["threat"] : 0f;
            if (threat <= 0.01f) continue;

            seenNodes.Add(nodeId);

            // Threat gradient: green (0) -> yellow (0.3) -> orange (0.6) -> red (1.0).
            Color color;
            if (threat < 0.3f)
                color = new Color(0.2f, 0.8f, 0.2f, 0.15f + threat * 0.5f);
            else if (threat < 0.6f)
                color = new Color(0.9f, 0.8f, 0.1f, 0.2f + threat * 0.3f);
            else if (threat < 0.85f)
                color = new Color(1.0f, 0.5f, 0.1f, 0.3f + threat * 0.2f);
            else
                color = new Color(1.0f, 0.15f, 0.1f, 0.4f + threat * 0.15f);

            EnsureV2OverlayDisc(nodeId, nodeRoot.GlobalPosition, color, 280.0f + threat * 200.0f);
        }

        EnsurePlayerFallbackDiscV0(seenNodes, new Color(0.2f, 0.7f, 0.2f, 0.4f), 260.0f);
        PruneV2OverlayDiscs(seenNodes);
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
}
