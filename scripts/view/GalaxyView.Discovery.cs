using Godot;
using SpaceTradeEmpire.Bridge;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.View;

public partial class GalaxyView
{
    private Node3D CreateLaneGateMarkerV0(string neighborId, string displayName = "")
    {
        var root = new Node3D { Name = "LaneGate_" + neighborId };
        root.SetMeta("neighbor_node_id", neighborId);

        // GATE.T41.SPATIAL.LANE_GATES.001: Bright emissive torus — visible from 80u flight altitude.
        // Increased emission energy (8.0) so gate ring is visible at full flight camera distance.
        var orbMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.4f, 0.6f, 1.0f, 0.9f),
            EmissionEnabled = true,
            Emission = new Color(0.4f, 0.65f, 1.0f),
            EmissionEnergyMultiplier = 8.0f,
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

        // GATE.T41.SPATIAL.LANE_GATES.001: Destination label — shows connected system name.
        // Billboard mode so it always faces the camera. Visible from flight altitude (80u).
        if (!string.IsNullOrEmpty(displayName))
        {
            // Strip parenthesized resource tags for clean gate label.
            string cleanName = displayName;
            int parenIdx = cleanName.IndexOf('(');
            if (parenIdx > 0)
                cleanName = cleanName.Substring(0, parenIdx).Trim();

            // GATE.T63.SPATIAL.LANE_LABELS.001: Large, bright, emissive label readable at
            // ~200u flight camera altitude. PixelSize 0.25 + FontSize 48 gives good readability.
            // Billboard mode ensures label always faces camera. NoDepthTest keeps it visible
            // even when behind gate geometry. Render priority ensures it draws on top.
            var gateLabel = new Label3D
            {
                Name = "GateDestLabel",
                Text = "→ " + cleanName,
                PixelSize = 0.25f,
                FontSize = 48,
                OutlineSize = 8,
                Modulate = new Color(0.6f, 0.85f, 1.0f, 1.0f),
                OutlineModulate = new Color(0.0f, 0.0f, 0.15f, 0.9f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Position = new Vector3(0, 14.0f, 0), // Above the gate ring
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                NoDepthTest = true,
                RenderPriority = 10,
            };
            gateLabel.AddToGroup("GateDestLabel"); // Identified by ClampLabelsRecursive for wider visibility range.
            root.AddChild(gateLabel);
        }

        // GATE.T68.SPATIAL.LANE_3D.001: Point light beacon — makes gate glow visible from 130u+.
        // Cyan-white light matches the torus emissive palette. Range 40u ensures visibility
        // at full flight camera distance (~80u altitude) without bleeding into other systems.
        var gateLight = new OmniLight3D
        {
            Name = "GateBeaconLight",
            LightColor = new Color(0.4f, 0.65f, 1.0f),
            LightEnergy = 3.0f,
            OmniRange = 40.0f,
            OmniAttenuation = 1.5f,
            Position = new Vector3(0f, 0f, 0f), // STRUCTURAL: centered on gate
            ShadowEnabled = false,
        };
        root.AddChild(gateLight);

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

    // GATE.T59.DISC_VIZ.FAMILY_PHASE.001: Per-family procedural 3D compositions with phase-dependent visual states.
    // Family meshes: DERELICT (hull fragments), RUIN (angular geometry), SIGNAL (antenna array),
    //   RESOURCE_POOL (mineral cluster), CORRIDOR (navigation trail). Unknown families fall back to generic sphere.
    // Phase LOD: SEEN (ghostly alpha 0.3), SCANNED (solid, emissive, particles), ANALYZED (bright, green accent).
    private Node3D CreateDiscoverySiteMarkerV0(string siteId, string family, string phase)
    {
        var root = new Node3D { Name = "DiscoverySite_" + siteId };

        // --- Phase-dependent parameters ---
        float phaseAlpha = phase switch
        {
            "ANALYZED" => 1.0f,
            "SCANNED"  => 1.0f,
            "SEEN"     => 0.3f,
            _          => 0.3f
        };
        float phaseEmission = phase switch
        {
            "ANALYZED" => 6.0f,
            "SCANNED"  => 3.0f,
            "SEEN"     => 0.5f,
            _          => 0.5f
        };

        // GATE.T59.DISC_VIZ.APPROACH_FEEDBACK.001: Store base phase values as metadata
        // so the per-frame approach ramp can reference them.
        root.SetMeta("phase_alpha", phaseAlpha);
        root.SetMeta("phase_emission", phaseEmission);
        bool particlesActive = !string.Equals(phase, "SEEN", System.StringComparison.Ordinal);
        // ANALYZED gets a green accent glow; SCANNED uses family color; SEEN is dim.
        Color accentColor = string.Equals(phase, "ANALYZED", System.StringComparison.Ordinal)
            ? new Color(0.2f, 1.0f, 0.4f) // Green accent for ANALYZED
            : GetFamilyColor(family);

        // --- Build per-family mesh composition ---
        var meshRoot = new Node3D { Name = "FamilyMeshRoot" };
        switch (family)
        {
            case "DERELICT":
                BuildDerelictMeshV0(meshRoot, phaseAlpha, phaseEmission, accentColor);
                break;
            case "RUIN":
                BuildRuinMeshV0(meshRoot, phaseAlpha, phaseEmission, accentColor);
                break;
            case "SIGNAL":
                BuildSignalMeshV0(meshRoot, phaseAlpha, phaseEmission, accentColor);
                break;
            case "RESOURCE_POOL":
                BuildResourcePoolMeshV0(meshRoot, phaseAlpha, phaseEmission, accentColor);
                break;
            case "CORRIDOR":
                BuildCorridorMeshV0(meshRoot, phaseAlpha, phaseEmission, accentColor);
                break;
            default:
                BuildGenericMeshV0(meshRoot, phaseAlpha, phaseEmission, accentColor);
                break;
        }
        root.AddChild(meshRoot);

        // --- Phase-dependent particles (active for SCANNED and ANALYZED only) ---
        if (particlesActive)
        {
            Color particleColor = new Color(accentColor.R, accentColor.G, accentColor.B, 0.6f);
            int particleCount = string.Equals(phase, "ANALYZED", System.StringComparison.Ordinal) ? 20 : 12;
            var siteParticleProc = new ParticleProcessMaterial
            {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
                EmissionSphereRadius = 2.0f,
                Gravity = Vector3.Zero,
                InitialVelocityMin = 0.2f,
                InitialVelocityMax = 0.8f,
                Color = particleColor,
                ScaleMin = 0.05f,
                ScaleMax = 0.15f,
            };
            var siteParticles = new GpuParticles3D
            {
                Name = "SiteParticles",
                Amount = particleCount,
                Lifetime = 3.0f,
                SpeedScale = 0.3f,
                ProcessMaterial = siteParticleProc,
                DrawPass1 = new SphereMesh { Radius = 0.04f, Height = 0.08f },
            };
            root.AddChild(siteParticles);
        }

        // --- Proximity trigger: player RigidBody3D entering this area notifies GameManager ---
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

    // --- GATE.T59.DISC_VIZ.FAMILY_PHASE.001: Per-family mesh builders ---

    private static Color GetFamilyColor(string family) => family switch
    {
        "DERELICT"      => new Color(0.8f, 0.4f, 0.2f),  // Warm orange-rust
        "RUIN"          => new Color(0.6f, 0.5f, 0.9f),  // Pale violet-stone
        "SIGNAL"        => new Color(0.3f, 0.7f, 1.0f),  // Electric blue
        "RESOURCE_POOL" => new Color(0.9f, 0.8f, 0.2f),  // Gold-amber
        "CORRIDOR"      => new Color(0.4f, 0.9f, 0.7f),  // Teal-green
        _               => new Color(1.0f, 0.7f, 0.1f),  // Legacy orange
    };

    private StandardMaterial3D MakeDiscoveryMaterialV0(Color baseColor, float alpha, float emissionEnergy, Color accentColor)
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha),
            EmissionEnabled = true,
            Emission = accentColor,
            EmissionEnergyMultiplier = emissionEnergy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        if (alpha < 1.0f)
        {
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        }
        return mat;
    }

    // DERELICT: Damaged hull fragments (2-3 rotated box meshes), flickering point light, small debris field.
    private void BuildDerelictMeshV0(Node3D parent, float alpha, float emission, Color accent)
    {
        float r = DiscoverySiteMarkerRadiusU;
        var mat = MakeDiscoveryMaterialV0(new Color(0.8f, 0.4f, 0.2f), alpha, emission, accent);

        // Fragment 1: large tilted hull plate.
        var frag1 = new MeshInstance3D
        {
            Name = "HullFrag1",
            Mesh = new BoxMesh { Size = new Vector3(r * 2.0f, r * 0.4f, r * 1.5f) },
            MaterialOverride = mat,
        };
        frag1.RotateZ(0.3f);
        frag1.RotateY(0.8f);
        parent.AddChild(frag1);

        // Fragment 2: smaller debris plate offset.
        var frag2 = new MeshInstance3D
        {
            Name = "HullFrag2",
            Mesh = new BoxMesh { Size = new Vector3(r * 1.2f, r * 0.3f, r * 0.8f) },
            MaterialOverride = mat,
        };
        frag2.Position = new Vector3(r * 1.0f, r * 0.5f, -r * 0.3f);
        frag2.RotateX(-0.5f);
        frag2.RotateZ(1.2f);
        parent.AddChild(frag2);

        // Fragment 3: small tumbling shard.
        var frag3 = new MeshInstance3D
        {
            Name = "HullFrag3",
            Mesh = new BoxMesh { Size = new Vector3(r * 0.6f, r * 0.2f, r * 0.5f) },
            MaterialOverride = mat,
        };
        frag3.Position = new Vector3(-r * 0.8f, -r * 0.3f, r * 0.6f);
        frag3.RotateY(2.1f);
        frag3.RotateX(0.7f);
        // Slow spin for tumbling debris effect.
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null)
        {
            frag3.SetScript(spinScript);
            frag3.Set("spin_speed_y", 0.15f);
        }
        parent.AddChild(frag3);

        // Flickering point light (orange, low range).
        var light = new OmniLight3D
        {
            Name = "DerelictFlicker",
            LightColor = accent,
            LightEnergy = emission * 0.3f,
            OmniRange = r * 5.0f,
            OmniAttenuation = 2.0f,
        };
        parent.AddChild(light);
    }

    // RUIN: Angular geometric structure (stacked rotated cubes/cylinders), faint energy emission, stone-like material.
    private void BuildRuinMeshV0(Node3D parent, float alpha, float emission, Color accent)
    {
        float r = DiscoverySiteMarkerRadiusU;
        var mat = MakeDiscoveryMaterialV0(new Color(0.5f, 0.45f, 0.55f), alpha, emission, accent);

        // Base: squat cylinder (stone pedestal).
        var baseCyl = new MeshInstance3D
        {
            Name = "RuinBase",
            Mesh = new CylinderMesh { TopRadius = r * 1.2f, BottomRadius = r * 1.5f, Height = r * 0.8f },
            MaterialOverride = mat,
        };
        parent.AddChild(baseCyl);

        // Mid: rotated cube (angular monolith).
        var midCube = new MeshInstance3D
        {
            Name = "RuinMonolith",
            Mesh = new BoxMesh { Size = new Vector3(r * 0.8f, r * 2.5f, r * 0.8f) },
            MaterialOverride = mat,
        };
        midCube.Position = new Vector3(0f, r * 1.2f, 0f);
        midCube.RotateY(Mathf.Pi / 6.0f);
        parent.AddChild(midCube);

        // Top: small tilted cylinder (capstone element).
        var topCyl = new MeshInstance3D
        {
            Name = "RuinCapstone",
            Mesh = new CylinderMesh { TopRadius = r * 0.3f, BottomRadius = r * 0.5f, Height = r * 0.6f },
            MaterialOverride = mat,
        };
        topCyl.Position = new Vector3(0f, r * 2.8f, 0f);
        topCyl.RotateZ(0.2f);
        parent.AddChild(topCyl);

        // Faint energy emission ring around base.
        var emitMat = MakeDiscoveryMaterialV0(accent, alpha * 0.5f, emission * 0.5f, accent);
        var emitRing = new MeshInstance3D
        {
            Name = "RuinEmitRing",
            Mesh = new TorusMesh { InnerRadius = r * 1.8f, OuterRadius = r * 2.1f },
            MaterialOverride = emitMat,
        };
        emitRing.RotateX(Mathf.Pi / 2.0f);
        emitRing.Position = new Vector3(0f, r * 0.1f, 0f);
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null)
        {
            emitRing.SetScript(spinScript);
            emitRing.Set("spin_speed_y", 0.2f);
        }
        parent.AddChild(emitRing);
    }

    // SIGNAL: Antenna array (thin cylinder + sphere tip), pulsing electromagnetic distortion ring, beacon flash.
    private void BuildSignalMeshV0(Node3D parent, float alpha, float emission, Color accent)
    {
        float r = DiscoverySiteMarkerRadiusU;
        var mat = MakeDiscoveryMaterialV0(new Color(0.3f, 0.6f, 0.9f), alpha, emission, accent);

        // Antenna mast (thin tall cylinder).
        var mast = new MeshInstance3D
        {
            Name = "SignalMast",
            Mesh = new CylinderMesh { TopRadius = r * 0.1f, BottomRadius = r * 0.15f, Height = r * 3.5f },
            MaterialOverride = mat,
        };
        mast.Position = new Vector3(0f, r * 1.75f, 0f);
        parent.AddChild(mast);

        // Beacon tip (sphere at top of mast).
        var beaconMat = MakeDiscoveryMaterialV0(accent, alpha, emission * 1.5f, accent);
        var beacon = new MeshInstance3D
        {
            Name = "SignalBeacon",
            Mesh = new SphereMesh { Radius = r * 0.4f },
            MaterialOverride = beaconMat,
        };
        beacon.Position = new Vector3(0f, r * 3.7f, 0f);
        parent.AddChild(beacon);

        // Electromagnetic distortion ring (spinning torus around mast midpoint).
        var ringMat = MakeDiscoveryMaterialV0(accent, alpha * 0.6f, emission * 0.8f, accent);
        var ring = new MeshInstance3D
        {
            Name = "SignalDistortionRing",
            Mesh = new TorusMesh { InnerRadius = r * 1.8f, OuterRadius = r * 2.2f },
            MaterialOverride = ringMat,
        };
        ring.Position = new Vector3(0f, r * 2.0f, 0f);
        ring.RotateX(Mathf.Pi / 2.0f);
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null)
        {
            ring.SetScript(spinScript);
            ring.Set("spin_speed_y", 0.8f);
        }
        parent.AddChild(ring);

        // Beacon point light.
        var light = new OmniLight3D
        {
            Name = "SignalBeaconLight",
            LightColor = accent,
            LightEnergy = emission * 0.5f,
            OmniRange = r * 6.0f,
            OmniAttenuation = 1.5f,
        };
        light.Position = new Vector3(0f, r * 3.7f, 0f);
        parent.AddChild(light);
    }

    // RESOURCE_POOL: Mineral cluster (3-5 small irregular spheres), faint resource-colored glow.
    private void BuildResourcePoolMeshV0(Node3D parent, float alpha, float emission, Color accent)
    {
        float r = DiscoverySiteMarkerRadiusU;
        var mat = MakeDiscoveryMaterialV0(new Color(0.9f, 0.75f, 0.2f), alpha, emission, accent);

        // Central large mineral.
        var core = new MeshInstance3D
        {
            Name = "MineralCore",
            Mesh = new SphereMesh { Radius = r * 1.0f },
            MaterialOverride = mat,
        };
        core.Scale = new Vector3(1.0f, 0.7f, 1.2f); // Slightly irregular.
        parent.AddChild(core);

        // Satellite minerals (4 small spheres at irregular offsets).
        float[][] offsets = new float[][]
        {
            new[] { 1.3f, 0.2f, 0.4f, 0.55f },
            new[] { -0.8f, 0.5f, 0.9f, 0.4f },
            new[] { 0.3f, -0.4f, -1.1f, 0.5f },
            new[] { -0.6f, 0.8f, -0.5f, 0.35f },
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            var o = offsets[i];
            var sat = new MeshInstance3D
            {
                Name = "Mineral_" + i,
                Mesh = new SphereMesh { Radius = r * o[3] },
                MaterialOverride = mat,
            };
            sat.Position = new Vector3(r * o[0], r * o[1], r * o[2]);
            sat.Scale = new Vector3(1.1f, 0.8f, 0.9f); // Slightly irregular.
            parent.AddChild(sat);
        }

        // Faint resource glow (point light).
        var light = new OmniLight3D
        {
            Name = "ResourceGlow",
            LightColor = accent,
            LightEnergy = emission * 0.25f,
            OmniRange = r * 4.0f,
            OmniAttenuation = 2.0f,
        };
        parent.AddChild(light);
    }

    // CORRIDOR: Faint trail of navigation markers (3 small spheres in a line), subtle directional glow.
    private void BuildCorridorMeshV0(Node3D parent, float alpha, float emission, Color accent)
    {
        float r = DiscoverySiteMarkerRadiusU;
        var mat = MakeDiscoveryMaterialV0(new Color(0.4f, 0.85f, 0.65f), alpha, emission, accent);

        // 3 small beacon spheres in a line, spaced apart.
        for (int i = 0; i < 3; i++)
        {
            float offset = (i - 1) * r * 2.0f; // -2r, 0, +2r along X axis.
            var beacon = new MeshInstance3D
            {
                Name = "NavMarker_" + i,
                Mesh = new SphereMesh { Radius = r * 0.5f },
                MaterialOverride = mat,
            };
            beacon.Position = new Vector3(offset, 0f, 0f);
            parent.AddChild(beacon);
        }

        // Directional glow bar connecting the markers.
        var barMat = MakeDiscoveryMaterialV0(accent, alpha * 0.4f, emission * 0.3f, accent);
        var bar = new MeshInstance3D
        {
            Name = "CorridorBar",
            Mesh = new BoxMesh { Size = new Vector3(r * 5.0f, r * 0.1f, r * 0.1f) },
            MaterialOverride = barMat,
        };
        parent.AddChild(bar);
    }

    // Fallback: generic emissive sphere + spinning ring (legacy look).
    private void BuildGenericMeshV0(Node3D parent, float alpha, float emission, Color accent)
    {
        float r = DiscoverySiteMarkerRadiusU;
        var mat = MakeDiscoveryMaterialV0(new Color(1.0f, 0.7f, 0.1f), alpha, emission, accent);

        var mesh = new MeshInstance3D
        {
            Name = "SiteMesh",
            Mesh = new SphereMesh { Radius = r * 1.5f },
            MaterialOverride = mat,
        };
        parent.AddChild(mesh);

        // Spinning scan ring.
        var ringMat = MakeDiscoveryMaterialV0(accent, alpha * 0.6f, emission * 0.5f, accent);
        var ring = new MeshInstance3D
        {
            Name = "SiteRing",
            Mesh = new TorusMesh
            {
                InnerRadius = r * 2.5f,
                OuterRadius = r * 3.0f,
            },
            MaterialOverride = ringMat,
        };
        ring.RotateX(Mathf.Pi / 2.0f);
        var spinScript = GD.Load<Script>("res://scripts/spinning_node.gd");
        if (spinScript != null)
        {
            ring.SetScript(spinScript);
            ring.Set("spin_speed_y", 0.5f);
        }
        parent.AddChild(ring);
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

    // GATE.T59.DISC_VIZ.APPROACH_FEEDBACK.001: Per-frame distance-based feedback on discovery markers.
    // >30u: faint blip (alpha 0.05, low emission). 15-30u: silhouette ramp (alpha 0.2→phase*0.6).
    // <15u: full phase detail. Scanner ping intensifies as distance decreases.
    private void UpdateDiscoveryApproachFeedbackV0(float dt)
    {
        var player = GetTree()?.Root?.GetNodeOrNull<Node3D>("Main/Player");
        if (player == null || _localSystemRoot == null) return;

        Vector3 playerPos = player.GlobalPosition;
        float time = (float)_discoveryApproachTime;

        var sites = GetTree().GetNodesInGroup("DiscoverySite");
        for (int i = 0; i < sites.Count; i++)
        {
            if (sites[i] is not Node3D siteNode) continue;
            if (!siteNode.IsInsideTree()) continue;

            float dist = playerPos.DistanceTo(siteNode.GlobalPosition);

            // Retrieve base phase values stored as metadata.
            float baseAlpha = siteNode.HasMeta("phase_alpha") ? (float)siteNode.GetMeta("phase_alpha") : 0.3f;
            float baseEmission = siteNode.HasMeta("phase_emission") ? (float)siteNode.GetMeta("phase_emission") : 0.5f;

            // --- Compute distance-adjusted alpha and emission ---
            float effectiveAlpha;
            float effectiveEmission;
            float pingFreq;

            if (dist > DiscoveryBlipRange)
            {
                // Far away: scanner blip only — very faint glow.
                effectiveAlpha = 0.05f;
                effectiveEmission = baseEmission * 0.1f;
                pingFreq = 0.5f; // Slow, distant ping.
            }
            else if (dist > DiscoverySilhouetteRange)
            {
                // Silhouette range: linear ramp from blip to partial visibility.
                // t=0 at 30u (blip edge), t=1 at 15u (full silhouette).
                float t = 1.0f - (dist - DiscoverySilhouetteRange) / (DiscoveryBlipRange - DiscoverySilhouetteRange);
                t = Mathf.Clamp(t, 0f, 1f);
                effectiveAlpha = Mathf.Lerp(0.05f, baseAlpha * 0.6f, t);
                effectiveEmission = Mathf.Lerp(baseEmission * 0.1f, baseEmission * 0.6f, t);
                pingFreq = Mathf.Lerp(0.5f, 2.0f, t); // Ping speeds up as you approach.
            }
            else
            {
                // Close range: full phase detail resolves.
                effectiveAlpha = baseAlpha;
                effectiveEmission = baseEmission;
                pingFreq = 3.0f; // Fast, insistent ping at close range.
            }

            // --- Scanner ping: emission oscillation that intensifies with proximity ---
            float pingMod = 1.0f + 0.3f * MathF.Sin(time * pingFreq * Mathf.Tau);
            effectiveEmission *= pingMod;

            // --- Apply to all MeshInstance3D children in FamilyMeshRoot ---
            var meshRoot = siteNode.GetNodeOrNull<Node3D>("FamilyMeshRoot");
            if (meshRoot != null)
            {
                ApplyDiscoveryApproachMaterialV0(meshRoot, effectiveAlpha, effectiveEmission);
            }

            // --- Scale oscillation: subtle breathing effect that grows with proximity ---
            float scaleBase = 1.0f;
            if (dist < DiscoveryBlipRange)
            {
                float proximity01 = 1.0f - Mathf.Clamp(dist / DiscoveryBlipRange, 0f, 1f);
                float breathAmplitude = Mathf.Lerp(0.02f, 0.08f, proximity01);
                scaleBase = 1.0f + breathAmplitude * MathF.Sin(time * pingFreq * 0.5f * Mathf.Tau);
            }
            if (meshRoot != null)
                meshRoot.Scale = Vector3.One * scaleBase;

            // --- Particle visibility: only show when within silhouette range ---
            var particles = siteNode.GetNodeOrNull<GpuParticles3D>("SiteParticles");
            if (particles != null)
            {
                particles.Emitting = dist < DiscoverySilhouetteRange;
            }
        }
    }

    // GATE.T59.DISC_VIZ.APPROACH_FEEDBACK.001: Recursively apply alpha/emission to all mesh materials.
    private static void ApplyDiscoveryApproachMaterialV0(Node3D root, float alpha, float emission)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                mat.AlbedoColor = new Color(mat.AlbedoColor.R, mat.AlbedoColor.G, mat.AlbedoColor.B, alpha);
                mat.EmissionEnergyMultiplier = emission;
                if (alpha < 1.0f)
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                else
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
            }
            else if (child is OmniLight3D light)
            {
                light.LightEnergy = emission * 0.3f;
            }
            else if (child is Node3D sub)
            {
                ApplyDiscoveryApproachMaterialV0(sub, alpha, emission);
            }
        }
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
}
