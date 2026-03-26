using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;

namespace SimCore.Tests.Systems;

/// <summary>
/// GATE.T56 Tier 3 proof gates — verify the SimCore mechanics the consolidated bot exercises.
/// </summary>
[TestFixture]
public sealed class T56BotProofTests
{
    // ── GATE.T56.BOT.COMBAT_LOOT_PROOF.001 ──
    // Pirate loot contains rare_metals + salvaged_tech in cargo after collect.

    [Test]
    public void PirateLoot_ContainsRareMetals_And_SalvagedTech()
    {
        var state = new SimState(42);
        state.Nodes["node_a"] = new Node { Id = "node_a", Name = "Alpha" };
        state.PlayerCredits = 100;
        state.Fleets["fleet_trader_1"] = new Fleet
        {
            Id = "fleet_trader_1",
            OwnerId = "player",
            CurrentNodeId = "node_a",
            State = FleetState.Idle
        };

        // Roll pirate loot at player's node.
        LootTableSystem.RollPirateLoot(state, "fleet_pirate_1", "node_a");

        Assert.That(state.LootDrops.Count, Is.EqualTo(1), "Expected exactly one loot drop");
        var dropId = "";
        foreach (var kv in state.LootDrops) dropId = kv.Key;
        var drop = state.LootDrops[dropId];

        // Pirate loot must contain salvaged_tech and rare_metals.
        Assert.That(drop.Goods.ContainsKey(WellKnownGoodIds.SalvagedTech), Is.True,
            "Pirate loot missing salvaged_tech");
        Assert.That(drop.Goods[WellKnownGoodIds.SalvagedTech], Is.GreaterThan(0));
        Assert.That(drop.Goods.ContainsKey(WellKnownGoodIds.RareMetals), Is.True,
            "Pirate loot missing rare_metals");
        Assert.That(drop.Goods[WellKnownGoodIds.RareMetals], Is.GreaterThan(0));

        // Collect loot — goods should transfer to player cargo.
        var cmd = new CollectLootCommand(dropId);
        cmd.Execute(state);

        Assert.That(state.PlayerCargo.ContainsKey(WellKnownGoodIds.SalvagedTech), Is.True,
            "salvaged_tech not in player cargo after collect");
        Assert.That(state.PlayerCargo[WellKnownGoodIds.SalvagedTech], Is.GreaterThan(0));
        Assert.That(state.PlayerCargo.ContainsKey(WellKnownGoodIds.RareMetals), Is.True,
            "rare_metals not in player cargo after collect");
        Assert.That(state.PlayerCargo[WellKnownGoodIds.RareMetals], Is.GreaterThan(0));
    }

    // ── GATE.T56.BOT.HAVEN_TIER3_PROOF.001 ──
    // Haven can reach tier 3 (Operational) given sufficient resources + fragments.

    [Test]
    public void Haven_ReachesTier3_WithResources()
    {
        var state = new SimState(42);
        state.Haven = new HavenStarbase
        {
            NodeId = "haven_node",
            Discovered = true,
            Tier = HavenTier.Powered // Tier 1
        };
        state.Nodes["haven_node"] = new Node { Id = "haven_node", Name = "Haven" };
        state.Markets["haven_node"] = new Market();

        // Give player enough resources for tier 2 + tier 3.
        state.PlayerCredits = HavenTweaksV0.UpgradeCreditsTier2 + HavenTweaksV0.UpgradeCreditsTier3 + 1000;
        state.PlayerCargo[WellKnownGoodIds.ExoticMatter] =
            HavenTweaksV0.UpgradeExoticMatterTier2 + HavenTweaksV0.UpgradeExoticMatterTier3;
        state.PlayerCargo[WellKnownGoodIds.Composites] =
            HavenTweaksV0.UpgradeCompositesTier2 + HavenTweaksV0.UpgradeCompositesTier3;
        state.PlayerCargo[WellKnownGoodIds.Electronics] = HavenTweaksV0.UpgradeElectronicsTier2;
        state.PlayerCargo[WellKnownGoodIds.RareMetals] = HavenTweaksV0.UpgradeRareMetalsTier3;

        // Fragment required for tier 3.
        for (int i = 0; i < HavenTweaksV0.FragmentsRequiredTier3; i++)
            state.Haven.InstalledFragmentIds.Add($"frag_{i}");

        // Upgrade to tier 2.
        Assert.That(HavenUpgradeSystem.CanUpgrade(state), Is.True, "Should be able to upgrade to tier 2");
        new UpgradeHavenCommand().Execute(state);
        Assert.That(state.Haven.UpgradeTargetTier, Is.EqualTo(HavenTier.Inhabited));

        // Tick through upgrade duration.
        for (int i = 0; i < HavenTweaksV0.UpgradeDurationTier2; i++)
            HavenUpgradeSystem.Process(state);
        Assert.That(state.Haven.Tier, Is.EqualTo(HavenTier.Inhabited), "Haven should be tier 2 now");

        // Upgrade to tier 3.
        Assert.That(HavenUpgradeSystem.CanUpgrade(state), Is.True, "Should be able to upgrade to tier 3");
        new UpgradeHavenCommand().Execute(state);
        Assert.That(state.Haven.UpgradeTargetTier, Is.EqualTo(HavenTier.Operational));

        for (int i = 0; i < HavenTweaksV0.UpgradeDurationTier3; i++)
            HavenUpgradeSystem.Process(state);

        Assert.That(state.Haven.Tier, Is.EqualTo(HavenTier.Operational), "Haven should be tier 3 (Operational)");
        Assert.That((int)state.Haven.Tier, Is.GreaterThanOrEqualTo(3));
    }

    // ── GATE.T56.BOT.MISSION_REP_PROOF.001 ──
    // Mission completion grants +5 faction reputation.

    [Test]
    public void MissionCompletion_Grants5Rep()
    {
        const string missionId = "t56_rep_proof_mission";
        const string factionId = "faction_test_a";

        var def = new MissionDef
        {
            MissionId = missionId,
            Title = "Rep Proof Mission",
            Description = "Deliver to node",
            FactionId = factionId,
            Steps = new List<MissionStepDef>
            {
                new MissionStepDef
                {
                    StepIndex = 0,
                    ObjectiveText = "Arrive at target",
                    TriggerType = MissionTriggerType.ArriveAtNode,
                    TargetNodeId = "n1"
                }
            },
            CreditReward = 50
        };

        MissionContentV0.RegisterTestMission(def);
        try
        {
            var state = new SimState(42);
            state.PlayerLocationNodeId = "n1";
            state.Nodes["n1"] = new Node { Id = "n1", Kind = NodeKind.Station };
            state.FactionReputation[factionId] = 0;

            MissionSystem.AcceptMission(state, missionId);
            MissionSystem.Process(state); // Player at n1, triggers ArriveAtNode → completes.

            Assert.That(state.Missions.CompletedMissionIds, Does.Contain(missionId),
                "Mission should be completed");
            Assert.That(state.FactionReputation[factionId], Is.EqualTo(FactionTweaksV0.MissionRepGain),
                $"Expected +{FactionTweaksV0.MissionRepGain} rep from mission completion");
        }
        finally
        {
            MissionContentV0.UnregisterTestMission(missionId);
        }
    }
}
