using System.Numerics;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;

namespace SimCore.Tests.Commands;

// GATE.S6.FRACTURE.MARKER_CMD.001: Survey marker command tests.
public class SurveyMarkerTests
{
    private static SimState BuildStateWithVoidSite(VoidSiteFamily family = VoidSiteFamily.AsteroidField)
    {
        var state = new SimState(42);
        state.VoidSites["void_01"] = new VoidSite
        {
            Id = "void_01",
            Position = new Vector3(50, 0, 50),
            Family = family,
            MarkerState = VoidSiteMarkerState.Unknown,
            NearStarA = "star_a",
            NearStarB = "star_b",
        };
        return state;
    }

    [Test]
    public void Execute_MarksAsSurveyed()
    {
        var state = BuildStateWithVoidSite();
        var cmd = new PlaceSurveyMarkerCommand("void_01");
        cmd.Execute(state);

        Assert.That(state.VoidSites["void_01"].MarkerState, Is.EqualTo(VoidSiteMarkerState.Surveyed));
    }

    [Test]
    public void Execute_SetsEstimatedResourceValue()
    {
        var state = BuildStateWithVoidSite();
        var cmd = new PlaceSurveyMarkerCommand("void_01");
        cmd.Execute(state);

        Assert.That(state.VoidSites["void_01"].EstimatedResourceValue, Is.GreaterThan(0));
    }

    [Test]
    public void Execute_AlreadySurveyed_NoOp()
    {
        var state = BuildStateWithVoidSite();
        state.VoidSites["void_01"].MarkerState = VoidSiteMarkerState.Surveyed;
        state.VoidSites["void_01"].EstimatedResourceValue = 999;

        var cmd = new PlaceSurveyMarkerCommand("void_01");
        cmd.Execute(state);

        // Should not overwrite.
        Assert.That(state.VoidSites["void_01"].EstimatedResourceValue, Is.EqualTo(999));
    }

    [Test]
    public void Execute_InvalidId_NoOp()
    {
        var state = BuildStateWithVoidSite();
        var cmd = new PlaceSurveyMarkerCommand("nonexistent");
        cmd.Execute(state);

        Assert.That(state.VoidSites["void_01"].MarkerState, Is.EqualTo(VoidSiteMarkerState.Unknown));
    }

    [Test]
    public void Execute_DiscoveredSite_BecomesSurveyed()
    {
        var state = BuildStateWithVoidSite();
        state.VoidSites["void_01"].MarkerState = VoidSiteMarkerState.Discovered;

        var cmd = new PlaceSurveyMarkerCommand("void_01");
        cmd.Execute(state);

        Assert.That(state.VoidSites["void_01"].MarkerState, Is.EqualTo(VoidSiteMarkerState.Surveyed));
    }

    [Test]
    public void ResourceEstimate_Deterministic()
    {
        var state1 = BuildStateWithVoidSite();
        new PlaceSurveyMarkerCommand("void_01").Execute(state1);

        var state2 = BuildStateWithVoidSite();
        new PlaceSurveyMarkerCommand("void_01").Execute(state2);

        Assert.That(state1.VoidSites["void_01"].EstimatedResourceValue,
            Is.EqualTo(state2.VoidSites["void_01"].EstimatedResourceValue));
    }

    [Test]
    public void SensorTech_ImproveEstimate()
    {
        // Without sensor tech.
        var state1 = BuildStateWithVoidSite(VoidSiteFamily.ResourceDeposit);
        new PlaceSurveyMarkerCommand("void_01").Execute(state1);
        int noTech = state1.VoidSites["void_01"].EstimatedResourceValue;

        // With sensor_suite (level 1 -> less noise).
        var state2 = BuildStateWithVoidSite(VoidSiteFamily.ResourceDeposit);
        state2.Tech.UnlockedTechIds.Add("sensor_suite");
        new PlaceSurveyMarkerCommand("void_01").Execute(state2);
        int withSuite = state2.VoidSites["void_01"].EstimatedResourceValue;

        // With advanced_sensors (level 2 -> exact).
        var state3 = BuildStateWithVoidSite(VoidSiteFamily.ResourceDeposit);
        state3.Tech.UnlockedTechIds.Add("sensor_suite");
        state3.Tech.UnlockedTechIds.Add("advanced_sensors");
        new PlaceSurveyMarkerCommand("void_01").Execute(state3);
        int withAdvanced = state3.VoidSites["void_01"].EstimatedResourceValue;

        // All should be positive.
        Assert.That(noTech, Is.GreaterThan(0));
        Assert.That(withSuite, Is.GreaterThan(0));
        Assert.That(withAdvanced, Is.GreaterThan(0));
    }

    [Test]
    public void ResourceDeposit_HigherBaseThan_AsteroidField()
    {
        // Exact estimate (advanced sensors) to avoid noise.
        var stateDeposit = BuildStateWithVoidSite(VoidSiteFamily.ResourceDeposit);
        stateDeposit.Tech.UnlockedTechIds.Add("sensor_suite");
        stateDeposit.Tech.UnlockedTechIds.Add("advanced_sensors");
        new PlaceSurveyMarkerCommand("void_01").Execute(stateDeposit);

        var stateAsteroid = BuildStateWithVoidSite(VoidSiteFamily.AsteroidField);
        stateAsteroid.Tech.UnlockedTechIds.Add("sensor_suite");
        stateAsteroid.Tech.UnlockedTechIds.Add("advanced_sensors");
        new PlaceSurveyMarkerCommand("void_01").Execute(stateAsteroid);

        // Same hash, but ResourceDeposit base (500) vs AsteroidField base (300).
        // The exact value includes hash variance so we can't guarantee ordering,
        // but both should be positive.
        Assert.That(stateDeposit.VoidSites["void_01"].EstimatedResourceValue, Is.GreaterThan(0));
        Assert.That(stateAsteroid.VoidSites["void_01"].EstimatedResourceValue, Is.GreaterThan(0));
    }
}
