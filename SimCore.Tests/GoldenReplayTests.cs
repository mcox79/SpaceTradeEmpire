using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimCore.Tests;

public class GoldenReplayTests
{
    private const string ExpectedGenesisHash = "DAB2BB84ADD27BC3C1CE13472CAB3DE7B912D8E6316671B7B7545E409412BBFF";
    private const string ExpectedFinalHash = "626451B545E976A5F33A40FC20F58F21057F4D85FE0D633A2C398771C373FA52";

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
        bool updateGolden = string.Equals(Environment.GetEnvironmentVariable("STE_UPDATE_GOLDEN"), "1", StringComparison.Ordinal);

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

        if (updateGolden)
        {
            TestContext.Out.WriteLine($"PASTE_GENESIS: {hashA_Initial}");
            TestContext.Out.WriteLine($"PASTE_FINAL:   {hashA_Final}");

            Directory.CreateDirectory("docs/generated/snapshots");
            File.WriteAllText(
                Path.Combine("docs/generated/snapshots", "golden_replay_hashes.txt"),
                $"Genesis={hashA_Initial}{Environment.NewLine}Final={hashA_Final}{Environment.NewLine}");

            Assert.Fail("Golden hashes updated. Copy PASTE_* values into ExpectedGenesisHash/ExpectedFinalHash.");
        }
        else
        {
            Assert.That(
                hashA_Initial,
                Is.EqualTo(ExpectedGenesisHash),
                "Golden genesis hash changed. If intentional, rerun with STE_UPDATE_GOLDEN=1 and update constants.");

            Assert.That(
                hashA_Final,
                Is.EqualTo(ExpectedFinalHash),
                "Golden final hash changed. If intentional, rerun with STE_UPDATE_GOLDEN=1 and update constants.");
        }

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
