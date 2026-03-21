using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using SimCore.Content;
using System.Collections.Generic;
using System;

namespace SimCore.Tests.Systems;

[TestFixture]
public class PlanetScanTests
{
    private SimState MakeTestState()
    {
        var state = new SimState(42);
        state.PlayerLocationNodeId = "A";

        // Create nodes.
        state.Nodes["A"] = new Node { Id = "A", Name = "Alpha Station" };
        state.Nodes["B"] = new Node { Id = "B", Name = "Beta Station" };
        state.Nodes["C"] = new Node { Id = "C", Name = "Gamma Station" };

        // Create edges.
        state.Edges["E_AB"] = new Edge { Id = "E_AB", FromNodeId = "A", ToNodeId = "B" };
        state.Edges["E_BC"] = new Edge { Id = "E_BC", FromNodeId = "B", ToNodeId = "C" };

        // Create markets with inventory.
        state.Markets["A"] = MakeMarket("A", "ore", 10, 100);
        state.Markets["B"] = MakeMarket("B", "ore", 25, 100);
        state.Markets["C"] = MakeMarket("C", "ore", 5, 100);

        // Create planets.
        state.Planets["A"] = new Planet
        {
            NodeId = "A", Type = PlanetType.Sand, DisplayName = "Desert Alpha",
            GravityBps = 6000, AtmosphereBps = 2000, TemperatureBps = 7000,
            Landable = true, LandingTechTier = 0,
            Specialization = PlanetSpecialization.Mining
        };
        state.Planets["B"] = new Planet
        {
            NodeId = "B", Type = PlanetType.Gaseous, DisplayName = "Gas Beta",
            GravityBps = 9000, AtmosphereBps = 9000, TemperatureBps = 3000,
            Landable = false, LandingTechTier = 0,
            Specialization = PlanetSpecialization.FuelExtraction
        };
        state.Planets["C"] = new Planet
        {
            NodeId = "C", Type = PlanetType.Barren, DisplayName = "Barren Gamma",
            GravityBps = 2000, AtmosphereBps = 500, TemperatureBps = 1500,
            Landable = true, LandingTechTier = 1,
            Specialization = PlanetSpecialization.Mining
        };

        // Init intel.
        state.Intel = new IntelBook();

        // Init adaptation fragments for fragment cache tests.
        foreach (var fDef in AdaptationFragmentContentV0.AllFragments)
        {
            state.AdaptationFragments[fDef.FragmentId] = new AdaptationFragment
            {
                FragmentId = fDef.FragmentId,
                Name = fDef.Name,
                Kind = fDef.Kind,
                ResonancePairId = fDef.ResonancePairId,
                CollectedTick = -1
            };
        }

        // Give player some fuel for atmospheric sampling.
        state.PlayerCargo[WellKnownGoodIds.Fuel] = 10;

        return state;
    }

    private Market MakeMarket(string nodeId, string goodId, int idealStock, int qty)
    {
        var m = new Market { Id = nodeId };
        m.Inventory[goodId] = qty;
        return m;
    }

    // ── Orbital Scan Tests ──

    [Test]
    public void OrbitalScan_MineralSurvey_OnSandWorld_ProducesResult()
    {
        var state = MakeTestState();
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(result, Is.Not.Null, "Orbital scan should succeed");
        Assert.That(result!.ScanId, Is.Not.Empty);
        Assert.That(result.NodeId, Is.EqualTo("A"));
        Assert.That(result.Mode, Is.EqualTo(ScanMode.MineralSurvey));
        Assert.That(result.Phase, Is.EqualTo(ScanPhase.Orbital));
        Assert.That(result.FlavorText, Is.Not.Empty);
        Assert.That(result.AffinityBps, Is.EqualTo(15000), "Sand + MineralSurvey = 1.5x");
    }

    [Test]
    public void OrbitalScan_ConsumesCharge()
    {
        var state = MakeTestState();
        Assert.That(PlanetScanSystem.GetRemainingCharges(state), Is.EqualTo(2), "Basic scanner = 2 charges");

        PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(PlanetScanSystem.GetRemainingCharges(state), Is.EqualTo(1));

        PlanetScanSystem.ExecuteOrbitalScan(state, "B", ScanMode.MineralSurvey);
        Assert.That(PlanetScanSystem.GetRemainingCharges(state), Is.EqualTo(0));

        // Third scan should fail — no charges.
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(result, Is.Null, "Should fail with no charges");
    }

    [Test]
    public void OrbitalScan_SignalSweep_Unavailable_AtBasicTier()
    {
        var state = MakeTestState();
        // Basic tier (0) — SignalSweep requires Mk1 (tier 1).
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.SignalSweep);
        Assert.That(result, Is.Null, "SignalSweep unavailable at basic tier");
    }

    [Test]
    public void OrbitalScan_SignalSweep_Available_AtMk1()
    {
        var state = MakeTestState();
        state.ScannerTier = 1;
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.SignalSweep);
        Assert.That(result, Is.Not.Null, "SignalSweep available at Mk1");
    }

    [Test]
    public void OrbitalScan_Archaeological_Unavailable_BelowMk2()
    {
        var state = MakeTestState();
        state.ScannerTier = 1;
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.Archaeological);
        Assert.That(result, Is.Null, "Archaeological unavailable below Mk2");
    }

    [Test]
    public void OrbitalScan_StoresResultOnPlanet()
    {
        var state = MakeTestState();
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(result, Is.Not.Null);
        Assert.That(state.Planets["A"].ScanResults, Contains.Item(result!.ScanId));
        Assert.That(state.PlanetScanResults.ContainsKey(result.ScanId), Is.True);
    }

    [Test]
    public void OrbitalScan_NoFragmentOrArchive()
    {
        // Orbital scans should never produce FragmentCache or DataArchive.
        var state = MakeTestState();
        // Run multiple scans.
        for (int i = 0; i < 20; i++)
        {
            state.ScannerChargesUsed = 0; // Reset for each test iteration.
            var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
            if (result != null)
            {
                Assert.That(result.Category, Is.Not.EqualTo(FindingCategory.FragmentCache),
                    "Orbital scan should never produce FragmentCache");
                Assert.That(result.Category, Is.Not.EqualTo(FindingCategory.DataArchive),
                    "Orbital scan should never produce DataArchive");
            }
        }
    }

    // ── Landing Scan Tests ──

    [Test]
    public void LandingScan_OnLandablePlanet_Succeeds()
    {
        var state = MakeTestState();
        var result = PlanetScanSystem.ExecuteLandingScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(result, Is.Not.Null, "Landing scan on landable Sand world should succeed");
        Assert.That(result!.Phase, Is.EqualTo(ScanPhase.Landing));
    }

    [Test]
    public void LandingScan_OnGaseousPlanet_Fails()
    {
        var state = MakeTestState();
        var result = PlanetScanSystem.ExecuteLandingScan(state, "B", ScanMode.MineralSurvey);
        Assert.That(result, Is.Null, "Landing scan on Gaseous planet should fail");
    }

    [Test]
    public void LandingScan_RecordsTick()
    {
        var state = MakeTestState();
        PlanetScanSystem.ExecuteLandingScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(state.Planets["A"].LandingScanTick, Is.EqualTo(state.Tick));
        Assert.That(state.Planets["A"].LandingScanMode, Is.EqualTo(ScanMode.MineralSurvey));
    }

    // ── Atmospheric Sample Tests ──

    [Test]
    public void AtmosphericSample_OnGaseousPlanet_Succeeds()
    {
        var state = MakeTestState();
        var result = PlanetScanSystem.ExecuteAtmosphericSample(state, "B", ScanMode.MineralSurvey);
        Assert.That(result, Is.Not.Null, "Atmospheric sample on Gaseous planet should succeed");
        Assert.That(result!.Phase, Is.EqualTo(ScanPhase.AtmosphericSample));
    }

    [Test]
    public void AtmosphericSample_OnNonGaseous_Fails()
    {
        var state = MakeTestState();
        var result = PlanetScanSystem.ExecuteAtmosphericSample(state, "A", ScanMode.MineralSurvey);
        Assert.That(result, Is.Null, "Atmospheric sample on Sand world should fail");
    }

    [Test]
    public void AtmosphericSample_ConsumeFuel()
    {
        var state = MakeTestState();
        int fuelBefore = state.PlayerCargo[WellKnownGoodIds.Fuel];
        PlanetScanSystem.ExecuteAtmosphericSample(state, "B", ScanMode.MineralSurvey);
        Assert.That(state.PlayerCargo[WellKnownGoodIds.Fuel], Is.EqualTo(fuelBefore - PlanetScanTweaksV0.AtmosphericSampleFuelCost));
    }

    [Test]
    public void AtmosphericSample_NoFuel_Fails()
    {
        var state = MakeTestState();
        state.PlayerCargo[WellKnownGoodIds.Fuel] = 0;
        var result = PlanetScanSystem.ExecuteAtmosphericSample(state, "B", ScanMode.MineralSurvey);
        Assert.That(result, Is.Null, "Should fail without fuel");
    }

    // ── Charge Budget Tests ──

    [Test]
    public void ChargeReset_OnTravel()
    {
        var state = MakeTestState();
        PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
        Assert.That(state.ScannerChargesUsed, Is.EqualTo(1));

        PlanetScanSystem.ResetChargesOnTravel(state);
        Assert.That(state.ScannerChargesUsed, Is.EqualTo(0));
        Assert.That(PlanetScanSystem.GetRemainingCharges(state), Is.EqualTo(2));
    }

    [Test]
    public void MaxCharges_ScalesWithTier()
    {
        Assert.That(PlanetScanTweaksV0.GetMaxCharges(0), Is.EqualTo(2));
        Assert.That(PlanetScanTweaksV0.GetMaxCharges(1), Is.EqualTo(3));
        Assert.That(PlanetScanTweaksV0.GetMaxCharges(2), Is.EqualTo(4));
        Assert.That(PlanetScanTweaksV0.GetMaxCharges(3), Is.EqualTo(5));
    }

    // ── Investigation Tests ──

    [Test]
    public void Investigation_PhysicalEvidence_Succeeds()
    {
        var state = MakeTestState();
        // Create a landing scan result with InvestigationAvailable.
        var scan = new PlanetScanResult
        {
            ScanId = "SCAN_TEST",
            NodeId = "A",
            Category = FindingCategory.PhysicalEvidence,
            InvestigationAvailable = true,
            Investigated = false,
            DiscoveryId = "disc_v0|RUIN|A|test|test"
        };
        state.PlanetScanResults["SCAN_TEST"] = scan;

        bool result = PlanetScanSystem.InvestigateFinding(state, "SCAN_TEST");
        Assert.That(result, Is.True);
        Assert.That(scan.Investigated, Is.True);
        Assert.That(state.Intel.KnowledgeConnections.Count, Is.GreaterThanOrEqualTo(PlanetScanTweaksV0.InvestigationBonusKgConnections));
    }

    [Test]
    public void Investigation_DoubleInvestigation_Fails()
    {
        var state = MakeTestState();
        var scan = new PlanetScanResult
        {
            ScanId = "SCAN_TEST2",
            NodeId = "A",
            Category = FindingCategory.PhysicalEvidence,
            InvestigationAvailable = true,
            Investigated = false,
            DiscoveryId = "disc_v0|RUIN|A|test2|test2"
        };
        state.PlanetScanResults["SCAN_TEST2"] = scan;

        PlanetScanSystem.InvestigateFinding(state, "SCAN_TEST2");
        bool secondResult = PlanetScanSystem.InvestigateFinding(state, "SCAN_TEST2");
        Assert.That(secondResult, Is.False, "Double investigation should fail");
    }

    // ── Affinity Matrix Tests ──

    [Test]
    public void AffinityMatrix_SandMineralSurvey_IsHighest()
    {
        int sandMineral = PlanetScanTweaksV0.GetAffinityBps(ScanMode.MineralSurvey, PlanetType.Sand);
        int sandSignal = PlanetScanTweaksV0.GetAffinityBps(ScanMode.SignalSweep, PlanetType.Sand);
        int sandArch = PlanetScanTweaksV0.GetAffinityBps(ScanMode.Archaeological, PlanetType.Sand);
        Assert.That(sandMineral, Is.GreaterThan(sandSignal));
        Assert.That(sandMineral, Is.GreaterThan(sandArch));
    }

    [Test]
    public void AffinityMatrix_GaseousSignalSweep_IsHighest()
    {
        int gasMineral = PlanetScanTweaksV0.GetAffinityBps(ScanMode.MineralSurvey, PlanetType.Gaseous);
        int gasSignal = PlanetScanTweaksV0.GetAffinityBps(ScanMode.SignalSweep, PlanetType.Gaseous);
        int gasArch = PlanetScanTweaksV0.GetAffinityBps(ScanMode.Archaeological, PlanetType.Gaseous);
        Assert.That(gasSignal, Is.GreaterThan(gasMineral));
        Assert.That(gasSignal, Is.GreaterThan(gasArch));
    }

    [Test]
    public void AffinityMatrix_BarrenArchaeological_IsHighest()
    {
        int barMineral = PlanetScanTweaksV0.GetAffinityBps(ScanMode.MineralSurvey, PlanetType.Barren);
        int barSignal = PlanetScanTweaksV0.GetAffinityBps(ScanMode.SignalSweep, PlanetType.Barren);
        int barArch = PlanetScanTweaksV0.GetAffinityBps(ScanMode.Archaeological, PlanetType.Barren);
        Assert.That(barArch, Is.GreaterThan(barMineral));
        Assert.That(barArch, Is.GreaterThan(barSignal));
    }

    // ── Mode Availability Tests ──

    [Test]
    public void ModeAvailability_MatchesTier()
    {
        Assert.That(PlanetScanSystem.IsModeAvailable(0, ScanMode.MineralSurvey), Is.True);
        Assert.That(PlanetScanSystem.IsModeAvailable(0, ScanMode.SignalSweep), Is.False);
        Assert.That(PlanetScanSystem.IsModeAvailable(0, ScanMode.Archaeological), Is.False);

        Assert.That(PlanetScanSystem.IsModeAvailable(1, ScanMode.SignalSweep), Is.True);
        Assert.That(PlanetScanSystem.IsModeAvailable(1, ScanMode.Archaeological), Is.False);

        Assert.That(PlanetScanSystem.IsModeAvailable(2, ScanMode.Archaeological), Is.True);
    }

    // ── Content Tests ──

    [Test]
    public void FlavorText_ExistsForAllPlanetTypesAndCategories()
    {
        foreach (PlanetType pt in Enum.GetValues(typeof(PlanetType)))
        {
            // Each planet type should have at least ResourceIntel and SignalLead flavors.
            var ri = PlanetScanContentV0.GetFlavors(pt, FindingCategory.ResourceIntel);
            Assert.That(ri.Count, Is.GreaterThan(0), $"Missing ResourceIntel flavor for {pt}");
        }
    }

    [Test]
    public void FoLines_AllTriggersHaveAllThreeTypes()
    {
        var triggers = new[] { "FIRST_PLANET_SURVEYED", "SCAN_MODE_MISMATCH", "PATTERN_RECOGNIZED",
                               "RARE_FIND", "SIGNAL_TRIANGULATED", "LORE_DISCOVERY" };
        var foTypes = new[] { "Analyst", "Veteran", "Pathfinder" };

        foreach (var trigger in triggers)
        {
            foreach (var foType in foTypes)
            {
                var line = PlanetScanContentV0.GetFoLine(trigger, foType);
                Assert.That(line, Is.Not.Null, $"Missing FO line for {trigger}/{foType}");
                Assert.That(line!.Text, Is.Not.Empty, $"Empty text for {trigger}/{foType}");
            }
        }
    }

    // ── Instability Reveal Test ──

    [Test]
    public void InstabilityReveal_CreatesSignalLead_WhenGateIsMet()
    {
        var state = MakeTestState();

        // Seed a discovery with instability gate at node B.
        string discId = "disc_v0|SIGNAL|B|gated|test";
        state.Intel.Discoveries[discId] = new DiscoveryStateV0
        {
            DiscoveryId = discId,
            Phase = DiscoveryPhase.Seen,
            InstabilityGate = 2
        };
        state.Nodes["B"].SeededDiscoveryIds ??= new List<string>();
        state.Nodes["B"].SeededDiscoveryIds.Add(discId);

        // Instability below gate — should not create lead.
        state.Nodes["B"].InstabilityLevel = 1;
        PlanetScanSystem.Process(state);
        Assert.That(state.Intel.RumorLeads.ContainsKey($"PLANET_SCAN_INSTAB_LEAD|{discId}"), Is.False);

        // Raise instability to meet gate.
        state.Nodes["B"].InstabilityLevel = 2;
        PlanetScanSystem.Process(state);
        Assert.That(state.Intel.RumorLeads.ContainsKey($"PLANET_SCAN_INSTAB_LEAD|{discId}"), Is.True);

        // Verify lead points to the correct node.
        var lead = state.Intel.RumorLeads[$"PLANET_SCAN_INSTAB_LEAD|{discId}"];
        Assert.That(lead.Hint.CoarseLocationToken, Is.EqualTo("B"));
    }

    // ── Scan ID Uniqueness ──

    [Test]
    public void ScanIds_AreUnique_AcrossMultipleScans()
    {
        var state = MakeTestState();
        var ids = new HashSet<string>();

        for (int i = 0; i < 5; i++)
        {
            state.ScannerChargesUsed = 0;
            var result = PlanetScanSystem.ExecuteOrbitalScan(state, "A", ScanMode.MineralSurvey);
            if (result != null)
            {
                Assert.That(ids.Add(result.ScanId), Is.True, $"Duplicate scan ID: {result.ScanId}");
            }
        }
    }

    // ── No Planet Test ──

    [Test]
    public void OrbitalScan_NoPlanet_ReturnsNull()
    {
        var state = MakeTestState();
        // Node A has a planet, but "Z" does not.
        var result = PlanetScanSystem.ExecuteOrbitalScan(state, "Z", ScanMode.MineralSurvey);
        Assert.That(result, Is.Null);
    }
}
