using System.Collections.Generic;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.Systems;

// GATE.S7.FACTION.TARIFF_ENFORCE.001: Tariff enforcement by faction reputation + doctrine.
public class TariffEnforcementTests
{
    private static SimState BuildTariffWorld(float tariffRate = 0.15f, int reputation = 0)
    {
        var def = ScenarioHarnessV0.New("tariff_test")
            .Market("mkt_a", ("ore", 100), ("food", 100))
            .Node("stn_a", "Station", "Alpha", "mkt_a", new float[] { 0f, 0f, 0f })
            .Node("stn_b", "Station", "Beta", "", new float[] { 10f, 0f, 0f })
            .Lane("lane_ab", "stn_a", "stn_b", 1.0f, 5)
            .Player(10000, "stn_a")
            .Build();

        // Add a faction controlling stn_a with the specified tariff rate.
        def.Factions.Add(new WorldFaction
        {
            FactionId = "faction_miners",
            HomeNodeId = "stn_a",
            RoleTag = "Miner",
            TariffRate = tariffRate,
            ControlledNodeIds = new List<string> { "stn_a" }
        });

        var state = new SimState();
        WorldLoader.Apply(state, def);

        // Set player reputation with the faction.
        if (reputation != 0)
            ReputationSystem.AdjustReputation(state, "faction_miners", reputation);

        return state;
    }

    [Test]
    public void NodeFactionId_Populated_ByWorldLoader()
    {
        var state = BuildTariffWorld();
        Assert.That(state.NodeFactionId.ContainsKey("stn_a"), Is.True);
        Assert.That(state.NodeFactionId["stn_a"], Is.EqualTo("faction_miners"));
    }

    [Test]
    public void FactionTariffRates_Populated_ByWorldLoader()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f);
        Assert.That(state.FactionTariffRates.ContainsKey("faction_miners"), Is.True);
        Assert.That(state.FactionTariffRates["faction_miners"], Is.EqualTo(0.15f));
    }

    [Test]
    public void EffectiveTariff_NeutralRep_EqualsBase()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: 0);
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");
        Assert.That(bps, Is.EqualTo(1500)); // 0.15 * 10000 * (1 - 0/100) = 1500
    }

    [Test]
    public void EffectiveTariff_MaxRep_ZeroTariff()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: 100);
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");
        Assert.That(bps, Is.EqualTo(0)); // 1500 * (100 - 100) / 100 = 0
    }

    [Test]
    public void EffectiveTariff_BadRep_HigherTariff()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: -100);
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_a");
        Assert.That(bps, Is.EqualTo(3000)); // 1500 * (100 - (-100)) / 100 = 3000
    }

    [Test]
    public void Buy_NeutralRep_CostsMore_WithTariff()
    {
        var state = BuildTariffWorld(tariffRate: 0.20f, reputation: 0);

        long creditsBefore = state.PlayerCredits;
        var cmd = new BuyCommand("mkt_a", "ore", 1);
        cmd.Execute(state);

        long spent = creditsBefore - state.PlayerCredits;
        Assert.That(spent, Is.GreaterThan(0), "Player should have spent credits");
        Assert.That(InventoryLedger.Get(state.PlayerCargo, "ore"), Is.EqualTo(1));
    }

    [Test]
    public void Trade_BelowThreshold_Blocked()
    {
        // reputation = -60 is below TradeBlockedRepThreshold (-50)
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: -60);

        long creditsBefore = state.PlayerCredits;
        var cmd = new BuyCommand("mkt_a", "ore", 1);
        cmd.Execute(state);

        // Trade should have been blocked — no credits spent, no goods received.
        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore));
        Assert.That(InventoryLedger.Get(state.PlayerCargo, "ore"), Is.EqualTo(0));
    }

    [Test]
    public void Sell_BelowThreshold_Blocked()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: -60);
        InventoryLedger.AddCargo(state.PlayerCargo, "food", 5);

        long creditsBefore = state.PlayerCredits;
        var cmd = new SellCommand("mkt_a", "food", 1);
        cmd.Execute(state);

        Assert.That(state.PlayerCredits, Is.EqualTo(creditsBefore));
        Assert.That(InventoryLedger.Get(state.PlayerCargo, "food"), Is.EqualTo(5));
    }

    [Test]
    public void NoFaction_NoTariff()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f);
        int bps = MarketSystem.GetEffectiveTariffBps(state, "mkt_nonexistent");
        Assert.That(bps, Is.EqualTo(0));
    }

    [Test]
    public void CanTradeByReputation_AtThreshold_Allowed()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: -50);
        Assert.That(MarketSystem.CanTradeByReputation(state, "mkt_a"), Is.True);
    }

    [Test]
    public void CanTradeByReputation_BelowThreshold_Blocked()
    {
        var state = BuildTariffWorld(tariffRate: 0.15f, reputation: -51);
        Assert.That(MarketSystem.CanTradeByReputation(state, "mkt_a"), Is.False);
    }

    // --- IsGoodEmbargoed tests ---

    [Test]
    public void IsGoodEmbargoed_MatchingEmbargo_ReturnsTrue()
    {
        var state = BuildTariffWorld();
        state.Embargoes.Add(new EmbargoState
        {
            Id = "emb_1",
            EnforcingFactionId = "faction_miners",
            TargetFactionId = "other",
            GoodId = "ore"
        });

        Assert.That(MarketSystem.IsGoodEmbargoed(state, "mkt_a", "ore"), Is.True);
    }

    [Test]
    public void IsGoodEmbargoed_DifferentGood_ReturnsFalse()
    {
        var state = BuildTariffWorld();
        state.Embargoes.Add(new EmbargoState
        {
            Id = "emb_1",
            EnforcingFactionId = "faction_miners",
            TargetFactionId = "other",
            GoodId = "ore"
        });

        Assert.That(MarketSystem.IsGoodEmbargoed(state, "mkt_a", "food"), Is.False);
    }

    [Test]
    public void IsGoodEmbargoed_NoEmbargoes_ReturnsFalse()
    {
        var state = BuildTariffWorld();
        Assert.That(MarketSystem.IsGoodEmbargoed(state, "mkt_a", "ore"), Is.False);
    }

    [Test]
    public void IsGoodEmbargoed_NoFaction_ReturnsFalse()
    {
        var state = BuildTariffWorld();
        state.Embargoes.Add(new EmbargoState
        {
            Id = "emb_1",
            EnforcingFactionId = "faction_miners",
            TargetFactionId = "other",
            GoodId = "ore"
        });

        // mkt_nonexistent has no controlling faction
        Assert.That(MarketSystem.IsGoodEmbargoed(state, "mkt_nonexistent", "ore"), Is.False);
    }
}
