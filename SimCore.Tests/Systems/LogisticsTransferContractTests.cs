using System;
using System.Linq;
using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Intents;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

public class LogisticsTransferContractTests
{
    [Test]
    public void LoadCargo_ClampsToMarketAvailable_AndNoNegatives()
    {
        const int seed = 123;
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Pick deterministic market+good: lowest market id, then lowest good key.
        var market = sim.State.Markets.Values
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .First();

        var goodId = market.Inventory.Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .First();

        // Force known starting values
        market.Inventory[goodId] = 5;
        fleet.Cargo.Clear();

        // Isolate transfer semantics: resolve intents only (do not run full kernel step which includes IndustrySystem).
        sim.State.EnqueueIntent(new LoadCargoIntent(fleet.Id, market.Id, goodId, 999));
        IntentSystem.Process(sim.State);

        Assert.That(market.Inventory[goodId], Is.EqualTo(0));
        Assert.That(fleet.Cargo.TryGetValue(goodId, out var qty) && qty == 5, Is.True);
        Assert.That(market.Inventory[goodId], Is.GreaterThanOrEqualTo(0));
        Assert.That(fleet.Cargo[goodId], Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void UnloadCargo_ClampsToCargoAvailable_AndNoNegatives()
    {
        const int seed = 123;
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        var market = sim.State.Markets.Values
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .First();

        var goodId = market.Inventory.Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .First();

        // Force known starting values
        market.Inventory[goodId] = 0;
        fleet.Cargo.Clear();
        fleet.Cargo[goodId] = 4;

        // Isolate transfer semantics: resolve intents only.
        sim.State.EnqueueIntent(new UnloadCargoIntent(fleet.Id, market.Id, goodId, 999));
        IntentSystem.Process(sim.State);

        Assert.That(fleet.Cargo.ContainsKey(goodId), Is.False, "Cargo zero key should be removed.");
        Assert.That(market.Inventory[goodId], Is.EqualTo(4));
        Assert.That(market.Inventory[goodId], Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Transfer_IsDeterministic_ForSameInputs()
    {
        const int seed = 123;

        string RunOnce()
        {
            var sim = new SimKernel(seed);
            GalaxyGenerator.Generate(sim.State, 20, 100f);

            var fleet = sim.State.Fleets.Values
                .OrderBy(f => f.Id, StringComparer.Ordinal)
                .First();

            var market = sim.State.Markets.Values
                .OrderBy(m => m.Id, StringComparer.Ordinal)
                .First();

            var goodId = market.Inventory.Keys
                .OrderBy(k => k, StringComparer.Ordinal)
                .First();

            market.Inventory[goodId] = 7;
            fleet.Cargo.Clear();

            sim.State.EnqueueIntent(new LoadCargoIntent(fleet.Id, market.Id, goodId, 3));
            sim.State.EnqueueIntent(new UnloadCargoIntent(fleet.Id, market.Id, goodId, 2));

            // Resolve intents only (no IndustrySystem side effects).
            IntentSystem.Process(sim.State);

            return sim.State.GetSignature();
        }

        var a = RunOnce();
        var b = RunOnce();

        Assert.That(b, Is.EqualTo(a));
    }
}
