#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

// GATE.S7.PLANET.BRIDGE.001: SimBridge planet + star queries.
public partial class SimBridge
{
    private Godot.Collections.Dictionary _cachedPlanetInfoV0 = new Godot.Collections.Dictionary();
    private Godot.Collections.Dictionary _cachedStarInfoV0 = new Godot.Collections.Dictionary();

    /// <summary>
    /// Returns planet info for a node: type, gravity, atmosphere, temperature, landable, specialization.
    /// Empty dictionary if no planet exists at the node.
    /// </summary>
    public Godot.Collections.Dictionary GetPlanetInfoV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(nodeId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Planets.TryGetValue(nodeId, out var planet)) return;

            result["node_id"] = planet.NodeId ?? "";
            result["planet_type"] = planet.Type.ToString();
            result["display_name"] = planet.DisplayName ?? "";
            result["gravity_bps"] = planet.GravityBps;
            result["atmosphere_bps"] = planet.AtmosphereBps;
            result["temperature_bps"] = planet.TemperatureBps;
            result["landable"] = planet.Landable;
            result["landing_tech_tier"] = planet.LandingTechTier;
            result["specialization"] = planet.Specialization.ToString();

            // GATE.S7.PLANET.TECH_GATE.001: Effective landability factors in player tech.
            // Planet.Landable = true means physically possible, but tech tier > 0 requires the tech.
            bool effectiveLandable = planet.Landable;
            if (planet.LandingTechTier > 0)
            {
                // Check if player has planetary_landing_mk1 (tech tier 1).
                effectiveLandable = state.Tech.UnlockedTechIds.Contains("planetary_landing_mk1");
            }
            result["effective_landable"] = effectiveLandable;

            _cachedPlanetInfoV0 = result;
        });

        return result.Count > 0 ? result : _cachedPlanetInfoV0;
    }

    /// <summary>
    /// Returns star info for a node: class, luminosity, display name.
    /// Empty dictionary if no star exists at the node.
    /// </summary>
    public Godot.Collections.Dictionary GetStarInfoV0(string nodeId)
    {
        var result = new Godot.Collections.Dictionary();
        if (string.IsNullOrEmpty(nodeId)) return result;

        TryExecuteSafeRead(state =>
        {
            if (!state.Stars.TryGetValue(nodeId, out var star)) return;

            result["node_id"] = star.NodeId ?? "";
            result["star_class"] = star.Class.ToString();
            result["display_name"] = star.DisplayName ?? "";
            result["luminosity_bps"] = star.LuminosityBps;

            // Include visual color for GDScript rendering.
            var (r, g, b) = PlanetContentV0.GetStarColor(star.Class);
            result["color_r"] = r;
            result["color_g"] = g;
            result["color_b"] = b;

            _cachedStarInfoV0 = result;
        });

        return result.Count > 0 ? result : _cachedStarInfoV0;
    }
}
