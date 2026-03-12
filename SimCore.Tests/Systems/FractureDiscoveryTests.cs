using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Numerics;

namespace SimCore.Tests.Systems;

// GATE.S6.FRACTURE_DISCOVERY.UNLOCK.001: Contract tests for fracture discovery unlock flow.
public class FractureDiscoveryTests
{
    private SimState CreateStateWithDerelict(int tick = 300)
    {
        var state = new SimState(42);
        // Advance to target tick.
        for (int i = 0; i < tick; i++) state.AdvanceTick();

        // Create a node near the derelict.
        state.Nodes["star_far"] = new Node { Id = "star_far", Position = new Vector3(100, 0, 0) };

        // Create a FractureDerelict VoidSite.
        state.VoidSites["void_fracture_derelict_0"] = new VoidSite
        {
            Id = "void_fracture_derelict_0",
            Position = new Vector3(110, 0, 10),
            Family = VoidSiteFamily.FractureDerelict,
            MarkerState = VoidSiteMarkerState.Discovered,
            NearStarA = "star_far",
            NearStarB = "star_far",
        };

        return state;
    }

    [Test]
    public void FractureDerelict_Surveyed_UnlocksFracture()
    {
        var state = CreateStateWithDerelict();
        state.VoidSites["void_fracture_derelict_0"].MarkerState = VoidSiteMarkerState.Surveyed;

        Assert.That(state.FractureUnlocked, Is.False);

        DiscoveryOutcomeSystem.CheckFractureDerelictUnlock(state);

        Assert.That(state.FractureUnlocked, Is.True);
        Assert.That(state.FractureDiscoveryTick, Is.EqualTo(state.Tick));
    }

    [Test]
    public void FractureDerelict_Discovered_DoesNotUnlock()
    {
        var state = CreateStateWithDerelict();
        // MarkerState is Discovered (not Surveyed) — should NOT unlock.

        DiscoveryOutcomeSystem.CheckFractureDerelictUnlock(state);

        Assert.That(state.FractureUnlocked, Is.False);
    }

    [Test]
    public void FractureDerelict_BeforeMinTick_DoesNotUnlock()
    {
        var state = CreateStateWithDerelict(tick: FractureTweaksV0.FractureDiscoveryMinTick - 1);
        state.VoidSites["void_fracture_derelict_0"].MarkerState = VoidSiteMarkerState.Surveyed;

        DiscoveryOutcomeSystem.CheckFractureDerelictUnlock(state);

        Assert.That(state.FractureUnlocked, Is.False);
    }

    [Test]
    public void FractureDerelict_AlreadyUnlocked_NoOp()
    {
        var state = CreateStateWithDerelict();
        state.VoidSites["void_fracture_derelict_0"].MarkerState = VoidSiteMarkerState.Surveyed;
        state.FractureUnlocked = true;
        state.FractureDiscoveryTick = 100;

        DiscoveryOutcomeSystem.CheckFractureDerelictUnlock(state);

        // Tick should NOT be overwritten.
        Assert.That(state.FractureDiscoveryTick, Is.EqualTo(100));
    }

    [Test]
    public void FractureDerelict_NonFractureVoidSite_DoesNotUnlock()
    {
        var state = CreateStateWithDerelict();
        // Change family to something else.
        state.VoidSites["void_fracture_derelict_0"].Family = VoidSiteFamily.AsteroidField;
        state.VoidSites["void_fracture_derelict_0"].MarkerState = VoidSiteMarkerState.Surveyed;

        DiscoveryOutcomeSystem.CheckFractureDerelictUnlock(state);

        Assert.That(state.FractureUnlocked, Is.False);
    }

    [Test]
    public void FractureDerelict_ViaProcess_UnlocksFracture()
    {
        // Full integration: DiscoveryOutcomeSystem.Process should trigger the unlock.
        var state = CreateStateWithDerelict();
        state.VoidSites["void_fracture_derelict_0"].MarkerState = VoidSiteMarkerState.Surveyed;

        // Process needs Intel to not be null.
        state.Intel ??= new IntelBook();

        DiscoveryOutcomeSystem.Process(state);

        Assert.That(state.FractureUnlocked, Is.True);
        Assert.That(state.FractureDiscoveryTick, Is.EqualTo(state.Tick));
    }
}
