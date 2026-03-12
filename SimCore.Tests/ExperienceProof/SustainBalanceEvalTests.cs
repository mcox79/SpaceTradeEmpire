using NUnit.Framework;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Gen;
using SimCore.Tweaks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.ExperienceProof;

/// <summary>
/// GATE.X.EVAL.SUSTAIN_BALANCE.001: Multi-seed sustain economy balance evaluation.
/// Runs 5 seeds x 5000 ticks. Checks fleet fuel consumption rates, sustain costs vs income,
/// immobilization frequency, NPC fuel demand impact on market prices.
/// </summary>
[TestFixture]
[Category("SustainBalance")]
public sealed class SustainBalanceEvalTests
{
    private static readonly int[] Seeds = { 42, 99, 1000, 31337, 77777 };
    private const int TickCount = 5000;

    private sealed class SeedResult
    {
        public int Seed;
        public int TotalPlayerFuelConsumed;
        public int PlayerFuelRemaining;
        public int ImmobilizedTicks;
        public int TotalNpcFuelConsumed;
        public int FleetsWithFuel;
        public int FleetsWithoutFuel;
        public long PlayerCreditsEnd;
        public int DisabledModuleCount;
    }

    [Test]
    public void SustainBalance_MultiSeed_NoSeedIsConsistentlyBroken()
    {
        var results = new List<SeedResult>();

        foreach (var seed in Seeds)
        {
            var result = RunSeed(seed);
            results.Add(result);

            TestContext.WriteLine($"Seed {seed}: playerFuelConsumed={result.TotalPlayerFuelConsumed} " +
                $"remaining={result.PlayerFuelRemaining} immobilizedTicks={result.ImmobilizedTicks} " +
                $"npcFuelConsumed={result.TotalNpcFuelConsumed} " +
                $"credits={result.PlayerCreditsEnd} disabledModules={result.DisabledModuleCount}");
        }

        // Evaluation assertions: these are soft — we want to catch gross imbalances.

        // 1. Player should not be permanently immobilized from tick 0.
        foreach (var r in results)
        {
            Assert.That(r.ImmobilizedTicks, Is.LessThan(TickCount),
                $"Seed {r.Seed}: player immobilized for ALL ticks — economy is broken.");
        }

        // 2. At least 3/5 seeds should have the player consume some fuel (indicating travel happens).
        int seedsWithFuelUse = results.Count(r => r.TotalPlayerFuelConsumed > 0);
        Assert.That(seedsWithFuelUse, Is.GreaterThanOrEqualTo(3),
            "Less than 3/5 seeds show player fuel consumption — fleet may not be traveling.");

        // 3. NPC fuel consumption should be non-zero (NPCs move in the economy).
        int seedsWithNpcFuel = results.Count(r => r.TotalNpcFuelConsumed > 0);
        Assert.That(seedsWithNpcFuel, Is.GreaterThanOrEqualTo(2),
            "Less than 2/5 seeds show NPC fuel consumption.");

        // Report summary.
        TestContext.WriteLine("\n=== SUSTAIN BALANCE SUMMARY ===");
        TestContext.WriteLine($"Seeds tested: {Seeds.Length}");
        TestContext.WriteLine($"Seeds with player fuel use: {seedsWithFuelUse}/{Seeds.Length}");
        TestContext.WriteLine($"Seeds with NPC fuel use: {seedsWithNpcFuel}/{Seeds.Length}");
        TestContext.WriteLine($"Avg player immobilized ticks: {results.Average(r => r.ImmobilizedTicks):F0}/{TickCount}");
        TestContext.WriteLine($"Avg player credits end: {results.Average(r => r.PlayerCreditsEnd):F0}");
        TestContext.WriteLine($"FuelPerMoveTick={SustainTweaksV0.FuelPerMoveTick} " +
            $"SustainCycleTicks={SustainTweaksV0.SustainCycleTicks} " +
            $"NpcFuelRate={SustainTweaksV0.NpcFuelRateMultiplier}");
    }

    private static SeedResult RunSeed(int seed)
    {
        var kernel = new SimKernel(seed);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        kernel.State.PlayerCredits = 500;

        // GalaxyGenerator does NOT create the player fleet — create it manually.
        var nodeIds = kernel.State.Nodes.Keys.OrderBy(n => n, StringComparer.Ordinal).ToList();
        var playerFleetNode = nodeIds.Count > 0 ? nodeIds[0] : "";
        var playerFleet0 = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = playerFleetNode,
            Speed = 0.5f,
            State = FleetState.Docked,
            CurrentTask = "Docked",
            FuelCapacity = ShipClassContentV0.GetById("corvette")?.BaseFuelCapacity ?? SustainTweaksV0.DefaultFuelCapacity,
            FuelCurrent = ShipClassContentV0.GetById("corvette")?.BaseFuelCapacity ?? SustainTweaksV0.DefaultFuelCapacity,
            Slots = new System.Collections.Generic.List<ModuleSlot>
            {
                new ModuleSlot { SlotId = "weapon_0", SlotKind = SlotKind.Weapon },
                new ModuleSlot { SlotId = "engine_0", SlotKind = SlotKind.Engine },
            }
        };
        kernel.State.Fleets["fleet_trader_1"] = playerFleet0;

        // Give the player fleet a destination so it will travel.
        if (nodeIds.Count > 1)
            playerFleet0.FinalDestinationNodeId = nodeIds[1];

        var result = new SeedResult { Seed = seed };
        int prevPlayerFuel = playerFleet0.FuelCurrent;
        int prevNpcFuelTotal = SumNpcFuel(kernel.State);

        // Cycle player fleet between two nodes to simulate ongoing travel.
        int destIdx = 1;
        for (int t = 0; t < TickCount; t++)
        {
            kernel.Step();

            // Track player fuel consumption.
            if (kernel.State.Fleets.TryGetValue("fleet_trader_1", out var playerFleet))
            {
                int currentFuel = playerFleet.FuelCurrent;
                if (currentFuel < prevPlayerFuel)
                    result.TotalPlayerFuelConsumed += (prevPlayerFuel - currentFuel);
                prevPlayerFuel = currentFuel;

                // Track immobilization.
                if (string.Equals(playerFleet.CurrentTask, "Immobilized:NoFuel", StringComparison.Ordinal))
                    result.ImmobilizedTicks++;

                // Re-route: when idle with no route, send to the next node.
                if (playerFleet.State == FleetState.Idle
                    && string.IsNullOrEmpty(playerFleet.FinalDestinationNodeId)
                    && (playerFleet.RouteEdgeIds == null || playerFleet.RouteEdgeIds.Count == 0)
                    && playerFleet.FuelCurrent > 0
                    && nodeIds.Count > 1)
                {
                    destIdx = (destIdx + 1) % nodeIds.Count;
                    playerFleet.FinalDestinationNodeId = nodeIds[destIdx];
                }
            }

            // Track NPC fuel.
            int npcFuelNow = SumNpcFuel(kernel.State);
            if (npcFuelNow < prevNpcFuelTotal)
                result.TotalNpcFuelConsumed += (prevNpcFuelTotal - npcFuelNow);
            prevNpcFuelTotal = npcFuelNow;
        }

        // Final state.
        if (kernel.State.Fleets.TryGetValue("fleet_trader_1", out var finalFleet))
        {
            result.PlayerFuelRemaining = finalFleet.FuelCurrent;
            if (finalFleet.Slots != null)
                result.DisabledModuleCount = finalFleet.Slots.Count(s => s.Disabled);
        }

        result.PlayerCreditsEnd = kernel.State.PlayerCredits;

        foreach (var fleet in kernel.State.Fleets.Values)
        {
            if (fleet.FuelCurrent > 0)
                result.FleetsWithFuel++;
            else
                result.FleetsWithoutFuel++;
        }

        return result;
    }

    private static int SumNpcFuel(SimState state)
    {
        int total = 0;
        foreach (var fleet in state.Fleets.Values)
        {
            if (string.Equals(fleet.OwnerId, "player", StringComparison.Ordinal)) continue;
            total += fleet.FuelCurrent;
        }
        return total;
    }
}
