using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

// GATE.S12.PROGRESSION.TESTS.001: Contract tests for PlayerStats + MilestoneSystem.
[TestFixture]
public class ProgressionTests
{
    private SimState CreateTestState()
    {
        var state = new SimState(42);
        state.Nodes["star_0"] = new Node { Id = "star_0", Name = "Sol" };
        state.Nodes["star_1"] = new Node { Id = "star_1", Name = "Alpha" };
        state.Nodes["star_2"] = new Node { Id = "star_2", Name = "Beta" };
        state.Markets["star_0"] = new Market { Id = "star_0" };
        state.Markets["star_0"].Inventory["fuel"] = 50;
        state.Markets["star_1"] = new Market { Id = "star_1" };
        state.PlayerLocationNodeId = "star_0";
        state.PlayerVisitedNodeIds.Add("star_0");
        state.PlayerStats = new PlayerStats();
        state.PlayerStats.NodesVisited = 1;
        return state;
    }

    [Test]
    public void PlayerArriveCommand_IncrementsNodesVisited()
    {
        var state = CreateTestState();
        Assert.That(state.PlayerStats.NodesVisited, Is.EqualTo(1));

        var cmd = new PlayerArriveCommand("star_1");
        cmd.Execute(state);

        Assert.That(state.PlayerStats.NodesVisited, Is.EqualTo(2));
    }

    [Test]
    public void PlayerArriveCommand_DuplicateNodeDoesNotIncrement()
    {
        var state = CreateTestState();
        var cmd = new PlayerArriveCommand("star_0"); // already visited
        cmd.Execute(state);

        Assert.That(state.PlayerStats.NodesVisited, Is.EqualTo(1));
    }

    [Test]
    public void SellCommand_IncrementsGoodsTradedAndCreditsEarned()
    {
        var state = CreateTestState();
        state.PlayerCargo["fuel"] = 10;

        var cmd = new SellCommand("star_0", "fuel", 5);
        cmd.Execute(state);

        Assert.That(state.PlayerStats.GoodsTraded, Is.EqualTo(5));
        Assert.That(state.PlayerStats.TotalCreditsEarned, Is.GreaterThan(0));
    }

    [Test]
    public void TradeCommand_Sell_IncrementsGoodsTradedAndCreditsEarned()
    {
        var state = CreateTestState();
        state.PlayerCargo["fuel"] = 10;

        var cmd = new TradeCommand("player", "star_0", "fuel", 3, TradeType.Sell);
        cmd.Execute(state);

        Assert.That(state.PlayerStats.GoodsTraded, Is.EqualTo(3));
        Assert.That(state.PlayerStats.TotalCreditsEarned, Is.GreaterThan(0));
    }

    [Test]
    public void TradeCommand_Buy_DoesNotIncrementStats()
    {
        var state = CreateTestState();
        state.PlayerCredits = 10000;

        var cmd = new TradeCommand("player", "star_0", "fuel", 2, TradeType.Buy);
        cmd.Execute(state);

        // Buy does not increment goods traded or credits earned
        Assert.That(state.PlayerStats.GoodsTraded, Is.EqualTo(0));
        Assert.That(state.PlayerStats.TotalCreditsEarned, Is.EqualTo(0));
    }

    [Test]
    public void MilestoneSystem_AchievesFirstTrade()
    {
        var state = CreateTestState();
        state.PlayerStats.GoodsTraded = 1;

        MilestoneSystem.Process(state);

        Assert.That(state.PlayerStats.AchievedMilestoneIds, Contains.Item("first_trade"));
    }

    [Test]
    public void MilestoneSystem_AchievesExplorer()
    {
        var state = CreateTestState();
        state.PlayerStats.NodesVisited = 5;

        MilestoneSystem.Process(state);

        Assert.That(state.PlayerStats.AchievedMilestoneIds, Contains.Item("explorer_5"));
    }

    [Test]
    public void MilestoneSystem_DoesNotDuplicateAchievements()
    {
        var state = CreateTestState();
        state.PlayerStats.GoodsTraded = 1;

        MilestoneSystem.Process(state);
        MilestoneSystem.Process(state);

        int count = 0;
        foreach (var id in state.PlayerStats.AchievedMilestoneIds)
            if (id == "first_trade") count++;
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void MilestoneSystem_MultipleThresholds()
    {
        var state = CreateTestState();
        state.PlayerStats.GoodsTraded = 100;
        state.PlayerStats.NodesVisited = 15;
        state.PlayerStats.TotalCreditsEarned = 10000;
        state.PlayerStats.TechsUnlocked = 1;
        state.PlayerStats.MissionsCompleted = 1;

        MilestoneSystem.Process(state);

        var achieved = state.PlayerStats.AchievedMilestoneIds;
        Assert.That(achieved, Contains.Item("first_trade"));
        Assert.That(achieved, Contains.Item("explorer_5"));
        Assert.That(achieved, Contains.Item("merchant_1000"));
        Assert.That(achieved, Contains.Item("researcher_1"));
        Assert.That(achieved, Contains.Item("captain_1"));
        Assert.That(achieved, Contains.Item("trader_100"));
        Assert.That(achieved, Contains.Item("explorer_15"));
        Assert.That(achieved, Contains.Item("tycoon_10000"));
        Assert.That(achieved.Count, Is.EqualTo(8));
    }

    [Test]
    public void MilestoneSystem_BelowThresholdNotAchieved()
    {
        var state = CreateTestState();
        state.PlayerStats.GoodsTraded = 0;
        state.PlayerStats.NodesVisited = 4;

        MilestoneSystem.Process(state);

        Assert.That(state.PlayerStats.AchievedMilestoneIds, Is.Empty);
    }

    [Test]
    public void GetStatValue_UnknownKeyReturnsZero()
    {
        var stats = new PlayerStats();
        Assert.That(MilestoneSystem.GetStatValue(stats, "bogus_key"), Is.EqualTo(0));
    }
}
