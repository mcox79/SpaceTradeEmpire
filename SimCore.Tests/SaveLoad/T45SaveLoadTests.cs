using System.Collections.Generic;
using NUnit.Framework;
using SimCore;
using SimCore.Entities;
using SimCore.Gen;

namespace SimCore.Tests.SaveLoad;

[TestFixture]
[Category("T45SaveLoad")]
public sealed class T45SaveLoadTests
{
    private static SimKernel CreateAndRun(int seed = 42, int ticks = 100)
    {
        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);
        for (int i = 0; i < ticks; i++)
            sim.Step();
        return sim;
    }

    private static SimKernel RoundTrip(SimKernel sim)
    {
        var json = sim.SaveToString();
        var sim2 = new SimKernel(999);
        sim2.LoadFromString(json);
        return sim2;
    }

    [Test]
    public void SensorGhosts_SurviveRoundTrip()
    {
        var sim = CreateAndRun();
        sim.State.SensorGhosts.Add(new SensorGhost
        {
            Id = "ghost_test_1",
            NodeId = "nodeA",
            ApparentFleetType = "trader",
            SpawnTick = 50,
            ExpiryTick = 80
        });

        var sim2 = RoundTrip(sim);

        Assert.That(sim2.State.SensorGhosts, Has.Count.EqualTo(1));
        Assert.That(sim2.State.SensorGhosts[0].Id, Is.EqualTo("ghost_test_1"));
        Assert.That(sim2.State.SensorGhosts[0].NodeId, Is.EqualTo("nodeA"));
        Assert.That(sim2.State.SensorGhosts[0].ApparentFleetType, Is.EqualTo("trader"));
        Assert.That(sim2.State.SensorGhosts[0].SpawnTick, Is.EqualTo(50));
        Assert.That(sim2.State.SensorGhosts[0].ExpiryTick, Is.EqualTo(80));
    }

    [Test]
    public void LatticeFauna_SurviveRoundTrip()
    {
        var sim = CreateAndRun();
        sim.State.LatticeFauna.Add(new LatticeFauna
        {
            Id = "fauna_test_1",
            NodeId = "nodeB",
            State = LatticeFaunaState.Present,
            SpawnTick = 30,
            ArrivalTick = 40,
            DarkTicksAccumulated = 5
        });

        var sim2 = RoundTrip(sim);

        Assert.That(sim2.State.LatticeFauna, Has.Count.EqualTo(1));
        Assert.That(sim2.State.LatticeFauna[0].Id, Is.EqualTo("fauna_test_1"));
        Assert.That(sim2.State.LatticeFauna[0].State, Is.EqualTo(LatticeFaunaState.Present));
        Assert.That(sim2.State.LatticeFauna[0].ArrivalTick, Is.EqualTo(40));
        Assert.That(sim2.State.LatticeFauna[0].DarkTicksAccumulated, Is.EqualTo(5));
    }

    [Test]
    public void LatticeFaunaResidue_SurvivesRoundTrip()
    {
        var sim = CreateAndRun();
        sim.State.LatticeFaunaResidue["nodeC"] = 200;
        sim.State.LatticeFaunaResidue["nodeD"] = 350;

        var sim2 = RoundTrip(sim);

        Assert.That(sim2.State.LatticeFaunaResidue, Has.Count.EqualTo(2));
        Assert.That(sim2.State.LatticeFaunaResidue["nodeC"], Is.EqualTo(200));
        Assert.That(sim2.State.LatticeFaunaResidue["nodeD"], Is.EqualTo(350));
    }

    [Test]
    public void DeepExposure_SurvivesRoundTrip()
    {
        var sim = CreateAndRun();
        sim.State.DeepExposure = 42;

        var sim2 = RoundTrip(sim);

        Assert.That(sim2.State.DeepExposure, Is.EqualTo(42));
    }

    [Test]
    public void AnomalyChains_SurviveRoundTrip()
    {
        var sim = CreateAndRun();
        sim.State.AnomalyChains["chain_test_1"] = new AnomalyChain
        {
            ChainId = "chain_test_1",
            CurrentStepIndex = 1,
            Status = AnomalyChainStatus.Active,
            StartedTick = 10,
            StarterNodeId = "nodeE",
            Steps = new List<AnomalyChainStep>
            {
                new()
                {
                    StepIndex = 0,
                    DiscoveryKind = "signal",
                    MinHopsFromStarter = 0,
                    MaxHopsFromStarter = 2,
                    NarrativeText = "A strange signal...",
                    IsCompleted = true
                },
                new()
                {
                    StepIndex = 1,
                    DiscoveryKind = "artifact",
                    MinHopsFromStarter = 1,
                    MaxHopsFromStarter = 3,
                    NarrativeText = "The signal leads here.",
                    IsCompleted = false
                }
            }
        };

        int chainCountBefore = sim.State.AnomalyChains.Count;

        var sim2 = RoundTrip(sim);

        Assert.That(sim2.State.AnomalyChains, Has.Count.EqualTo(chainCountBefore));
        Assert.That(sim2.State.AnomalyChains.ContainsKey("chain_test_1"), Is.True);
        var chain = sim2.State.AnomalyChains["chain_test_1"];
        Assert.That(chain.ChainId, Is.EqualTo("chain_test_1"));
        Assert.That(chain.CurrentStepIndex, Is.EqualTo(1));
        Assert.That(chain.Status, Is.EqualTo(AnomalyChainStatus.Active));
        Assert.That(chain.StarterNodeId, Is.EqualTo("nodeE"));
        Assert.That(chain.Steps, Has.Count.EqualTo(2));
        Assert.That(chain.Steps[0].IsCompleted, Is.True);
        Assert.That(chain.Steps[1].IsCompleted, Is.False);
        Assert.That(chain.Steps[1].DiscoveryKind, Is.EqualTo("artifact"));
    }
}
