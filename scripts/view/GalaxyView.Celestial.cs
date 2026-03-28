using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView
{
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
}
