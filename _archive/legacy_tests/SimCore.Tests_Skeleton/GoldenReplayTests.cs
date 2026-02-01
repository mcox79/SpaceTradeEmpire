using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests;

public class GoldenReplayTests
{
    private struct RecordedCommand
    {
        public int Tick;
        public ICommand Cmd;
    }

    [Test]
    public void Simulation_Is_Deterministic_With_Input()
    {
        int seed = 42;
        int ticks = 1000;
        var recordedInputs = new List<RecordedCommand>();

        // --- RUN A: RECORDING PHASE ---
        var simA = new SimKernel(seed);
        GalaxyGenerator.Generate(simA.State, 20, 100f);
        string hashA_Initial = simA.State.GetSignature();

        // Helper to generate deterministic "random" inputs
        var inputRng = new System.Random(seed);
        var markets = simA.State.Markets.Keys.ToList();

        for (int i = 0; i < ticks; i++)
        {
            // 10% chance to buy something every tick
            if (inputRng.NextDouble() < 0.1)
            {
                var cmd = new BuyCommand(markets[inputRng.Next(markets.Count)], "fuel", 1);
                simA.EnqueueCommand(cmd);
                recordedInputs.Add(new RecordedCommand { Tick = i, Cmd = cmd });
            }
            simA.Step();
        }
        string hashA_Final = simA.State.GetSignature();

        // --- RUN B: REPLAY PHASE ---
        var simB = new SimKernel(seed);
        GalaxyGenerator.Generate(simB.State, 20, 100f);
        string hashB_Initial = simB.State.GetSignature();

        // ASSERT INITIAL STATE
        Assert.That(hashB_Initial, Is.EqualTo(hashA_Initial), "Genesis state mismatch!");

        int inputIndex = 0;
        for (int i = 0; i < ticks; i++)
        {
            // Inject commands at exact same tick
            while (inputIndex < recordedInputs.Count && recordedInputs[inputIndex].Tick == i)
            {
                // Note: In a real replay, we'd serialize/deserialize commands.
                // Here we reuse the object or recreate identical one. Reusing is fine for POD commands.
                simB.EnqueueCommand(recordedInputs[inputIndex].Cmd);
                inputIndex++;
            }
            simB.Step();
        }
        string hashB_Final = simB.State.GetSignature();

        // LOGGING FOR VISIBILITY
        TestContext.Out.WriteLine($"Genesis Hash: {hashA_Initial}");
        TestContext.Out.WriteLine($"Final Hash A: {hashA_Final}");
        TestContext.Out.WriteLine($"Final Hash B: {hashB_Final}");

        // ASSERT FINAL STATE
        Assert.That(hashB_Final, Is.EqualTo(hashA_Final), "CRITICAL: Simulation Drift Detected!");
    }
}