#nullable enable

using Godot;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Bridge;

// GATE.S7.PLANET.BRIDGE.001: SimBridge planet + star queries.
// GATE.T42.PLANET_SCAN.BRIDGE.001: Planet scanning bridge methods.
public partial class SimBridge
{
    private Godot.Collections.Dictionary _cachedPlanetInfoV0 = new Godot.Collections.Dictionary();
    private Godot.Collections.Dictionary _cachedStarInfoV0 = new Godot.Collections.Dictionary();
    private Godot.Collections.Array _cachedPlanetScanResultsV0 = new();
    private Godot.Collections.Dictionary _cachedScanChargesV0 = new();

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

    // ── GATE.T42.PLANET_SCAN.BRIDGE.001: Planet scanning bridge methods ──

    /// <summary>Execute an orbital scan at the given node with the specified mode.</summary>
    public Godot.Collections.Dictionary OrbitalScanV0(string nodeId, string modeStr)
    {
        var result = new Godot.Collections.Dictionary();
        if (!Enum.TryParse<ScanMode>(modeStr, true, out var mode))
        {
            result["error"] = $"Invalid scan mode: {modeStr}";
            return result;
        }
        try
        {
            _stateLock.EnterWriteLock();
            var state = _kernel.State;
            var scan = PlanetScanSystem.ExecuteOrbitalScan(state, nodeId, mode);
            if (scan == null) { result["error"] = "Cannot scan (no planet, no charges, or mode unavailable)"; return result; }
            PackScanResult(result, scan);
        }
        finally { if (_stateLock.IsWriteLockHeld) _stateLock.ExitWriteLock(); }
        return result;
    }

    /// <summary>Execute a landing scan at the given node with the specified mode.</summary>
    public Godot.Collections.Dictionary LandingScanV0(string nodeId, string modeStr)
    {
        var result = new Godot.Collections.Dictionary();
        if (!Enum.TryParse<ScanMode>(modeStr, true, out var mode))
        {
            result["error"] = $"Invalid scan mode: {modeStr}";
            return result;
        }
        try
        {
            _stateLock.EnterWriteLock();
            var state = _kernel.State;
            var scan = PlanetScanSystem.ExecuteLandingScan(state, nodeId, mode);
            if (scan == null) { result["error"] = "Cannot land-scan (not landable, no charges, or mode unavailable)"; return result; }
            PackScanResult(result, scan);
        }
        finally { if (_stateLock.IsWriteLockHeld) _stateLock.ExitWriteLock(); }
        return result;
    }

    /// <summary>Execute atmospheric sample at a gaseous planet.</summary>
    public Godot.Collections.Dictionary AtmosphericSampleV0(string nodeId, string modeStr)
    {
        var result = new Godot.Collections.Dictionary();
        if (!Enum.TryParse<ScanMode>(modeStr, true, out var mode))
        {
            result["error"] = $"Invalid scan mode: {modeStr}";
            return result;
        }
        try
        {
            _stateLock.EnterWriteLock();
            var state = _kernel.State;
            var scan = PlanetScanSystem.ExecuteAtmosphericSample(state, nodeId, mode);
            if (scan == null) { result["error"] = "Cannot sample (not gaseous, no charges, or no fuel)"; return result; }
            PackScanResult(result, scan);
        }
        finally { if (_stateLock.IsWriteLockHeld) _stateLock.ExitWriteLock(); }
        return result;
    }

    /// <summary>Get all scan results for a planet node.</summary>
    public Godot.Collections.Array GetPlanetScanResultsV0(string nodeId)
    {
        TryExecuteSafeRead(state =>
        {
            var arr = new Godot.Collections.Array();
            if (state.Planets.TryGetValue(nodeId, out var planet) && planet.ScanResults != null)
            {
                foreach (var scanId in planet.ScanResults)
                {
                    if (!state.PlanetScanResults.TryGetValue(scanId, out var scan)) continue;
                    var d = new Godot.Collections.Dictionary();
                    PackScanResult(d, scan);
                    arr.Add(d);
                }
            }
            lock (_snapshotLock) { _cachedPlanetScanResultsV0 = arr; }
        });
        lock (_snapshotLock) { return _cachedPlanetScanResultsV0; }
    }

    /// <summary>Investigate a Physical Evidence finding for bonus KG connections.</summary>
    public Godot.Collections.Dictionary InvestigateFindingV0(string scanId)
    {
        var result = new Godot.Collections.Dictionary();
        try
        {
            _stateLock.EnterWriteLock();
            bool ok = PlanetScanSystem.InvestigateFinding(_kernel.State, scanId);
            result["success"] = ok;
            if (!ok) result["error"] = "Not investigatable or already investigated";
        }
        finally { if (_stateLock.IsWriteLockHeld) _stateLock.ExitWriteLock(); }
        return result;
    }

    /// <summary>Get current scanner charge status.</summary>
    public Godot.Collections.Dictionary GetScanChargesV0()
    {
        TryExecuteSafeRead(state =>
        {
            var d = new Godot.Collections.Dictionary
            {
                ["remaining"] = PlanetScanSystem.GetRemainingCharges(state),
                ["max"] = PlanetScanTweaksV0.GetMaxCharges(state.ScannerTier),
                ["used"] = state.ScannerChargesUsed,
                ["tier"] = state.ScannerTier,
                ["mineral_available"] = PlanetScanSystem.IsModeAvailable(state.ScannerTier, ScanMode.MineralSurvey),
                ["signal_available"] = PlanetScanSystem.IsModeAvailable(state.ScannerTier, ScanMode.SignalSweep),
                ["archaeological_available"] = PlanetScanSystem.IsModeAvailable(state.ScannerTier, ScanMode.Archaeological)
            };
            lock (_snapshotLock) { _cachedScanChargesV0 = d; }
        });
        lock (_snapshotLock) { return _cachedScanChargesV0; }
    }

    /// <summary>Get scan mode affinity (bps) for a planet at a node.</summary>
    public int GetScanAffinityV0(string nodeId, string modeStr)
    {
        int result = 10000; // Default 1.0x
        if (!Enum.TryParse<ScanMode>(modeStr, true, out var mode))
            return result;

        TryExecuteSafeRead(state =>
        {
            if (state.Planets.TryGetValue(nodeId, out var planet))
                result = PlanetScanTweaksV0.GetAffinityBps(mode, planet.Type);
        });
        return result;
    }

    private static void PackScanResult(Godot.Collections.Dictionary d, SimCore.Entities.PlanetScanResult scan)
    {
        d["scan_id"] = scan.ScanId;
        d["node_id"] = scan.NodeId;
        d["mode"] = scan.Mode.ToString();
        d["phase"] = scan.Phase.ToString();
        d["category"] = scan.Category.ToString();
        d["discovery_id"] = scan.DiscoveryId;
        d["flavor_text"] = scan.FlavorText;
        d["hint_text"] = scan.HintText;
        d["tick"] = scan.Tick;
        d["affinity_bps"] = scan.AffinityBps;
        d["investigation_available"] = scan.InvestigationAvailable;
        d["investigated"] = scan.Investigated;
        d["fragment_id"] = scan.FragmentId;
    }
}
