using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.World;
using SimCore.Programs;
using SimCore.Systems;

namespace SimCore.Tests.Programs;

[TestFixture]
public sealed class ProgramExecutionIntegrationTests
{
    private static SimKernel KernelWithWorld001()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_001",
            Markets =
                        {
                                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 10, ["food"] = 3 } },
                                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 1,  ["food"] = 12 } }
                        },
            Nodes =
                        {
                                new WorldNode { Id = "stn_a", Kind = "Station", Name = "Alpha Station", MarketId = "mkt_a", Pos = new float[] { 0f, 0f, 0f } },
                                new WorldNode { Id = "stn_b", Kind = "Station", Name = "Beta Station",  MarketId = "mkt_b", Pos = new float[] { 10f, 0f, 0f } }
                        },
            Edges =
                        {
                                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 5 }
                        },
            Player = new WorldPlayerStart { Credits = 1000, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(k.State, def);
        return k;
    }

    [Test]
    public void PlayLoopProof_ReportSchema_ContractTest_CanonicalTokens_Present_And_Ordered()
    {
        // GATE.S3_6.PLAY_LOOP_PROOF.001
        // Contract: canonical step token set is complete and ordered exactly as declared.
        // Determinism: stable ordering is by explicit list index (no sorting, no timestamps).

        var expected = new[]
        {
            ProgramExplain.PlayLoopProof.EXPLORE_SITE,
            ProgramExplain.PlayLoopProof.DOCK_HUB,
            ProgramExplain.PlayLoopProof.TRADE_LOOP_IDENTIFIED,
            ProgramExplain.PlayLoopProof.FREIGHTER_ACQUIRED,
            ProgramExplain.PlayLoopProof.TRADE_CHARTER_REVENUE,
            ProgramExplain.PlayLoopProof.RESOURCE_TAP_ACTIVE,
            ProgramExplain.PlayLoopProof.TECH_UNLOCK_REALIZED,
            ProgramExplain.PlayLoopProof.LORE_LEAD_SURFACED,
            ProgramExplain.PlayLoopProof.PIRACY_INCIDENT_LEGIBLE,
            ProgramExplain.PlayLoopProof.REMOTE_RESOLUTION_COUNT_GTE_2
        };

        Assert.That(ProgramExplain.PlayLoopProof.CanonicalStepTokensOrdered, Is.EqualTo(expected),
            "PlayLoopProof canonical step tokens must be present and ordered exactly as the contract declares.");
    }

    [Test]
    public void PROG_EXEC_002_TradeProgram_DrivesBuySell_Intents_AgainstWorld001_And_OnlyAffectsOutcomesViaTick()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        // Seed player cargo so SELL has something to do.
        s.PlayerCargo["food"] = 5;

        // Programs: buy ore, sell food, both against mkt_a
        var buyId = s.CreateAutoBuyProgram("mkt_a", "ore", quantity: 1, cadenceTicks: 1);
        var sellId = s.CreateAutoSellProgram("mkt_a", "food", quantity: 1, cadenceTicks: 1);

        s.Programs.Instances[buyId].Status = ProgramStatus.Running;
        s.Programs.Instances[sellId].Status = ProgramStatus.Running;

        // Ensure both are due now
        s.Programs.Instances[buyId].NextRunTick = s.Tick;
        s.Programs.Instances[sellId].NextRunTick = s.Tick;

        var oreInMarketBefore = s.Markets["mkt_a"].Inventory["ore"];
        var foodInMarketBefore = s.Markets["mkt_a"].Inventory["food"];

        var oreInCargoBefore = s.PlayerCargo.TryGetValue("ore", out var oreBefore) ? oreBefore : 0;
        var foodInCargoBefore = s.PlayerCargo["food"];

        var creditsBefore = s.PlayerCredits;

        // 1) ProgramSystem alone may enqueue intents, but must not mutate market/cargo/credits.
        ProgramSystem.Process(s);

        Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.EqualTo(oreInMarketBefore));
        Assert.That(s.Markets["mkt_a"].Inventory["food"], Is.EqualTo(foodInMarketBefore));

        var oreInCargoAfterProgram = s.PlayerCargo.TryGetValue("ore", out var oreAfterProg) ? oreAfterProg : 0;
        Assert.That(oreInCargoAfterProgram, Is.EqualTo(oreInCargoBefore));
        Assert.That(s.PlayerCargo["food"], Is.EqualTo(foodInCargoBefore));
        Assert.That(s.PlayerCredits, Is.EqualTo(creditsBefore));

        // ProgramSystem should have emitted 2 intents.
        Assert.That(s.PendingIntents.Count, Is.EqualTo(2));

        // Clear to ensure the only effects come from a normal tick pipeline.
        s.PendingIntents.Clear();

        // Re-arm: ProgramSystem.Process advanced NextRunTick; ensure programs are runnable for the upcoming tick.
        s.Programs.Instances[buyId].NextRunTick = s.Tick;
        s.Programs.Instances[sellId].NextRunTick = s.Tick;

        // 2) Now run one tick: program emission + intent processing should change state via commands.
        k.Step();

        // Intents should be cleared by the pipeline.
        Assert.That(s.PendingIntents.Count, Is.EqualTo(0));

        // BUY: market ore down, cargo ore up
        Assert.That(s.Markets["mkt_a"].Inventory["ore"], Is.LessThan(oreInMarketBefore));
        var oreInCargoAfterTick = s.PlayerCargo.TryGetValue("ore", out var oreAfterTick) ? oreAfterTick : 0;
        Assert.That(oreInCargoAfterTick, Is.GreaterThan(oreInCargoBefore));

        // SELL: market food up, cargo food down
        Assert.That(s.Markets["mkt_a"].Inventory["food"], Is.GreaterThan(foodInMarketBefore));
        Assert.That(s.PlayerCargo["food"], Is.LessThan(foodInCargoBefore));

        // Credits should change as a result of buy and sell commands (direction depends on pricing).
        Assert.That(s.PlayerCredits, Is.Not.EqualTo(creditsBefore));
    }

    [Test]
    public void PROG_EXEC_003_ConstructionProgram_SuppliesStageInputs_And_ConstructionConsumesRecipe_And_ProducesCapModule_ViaTickPipeline()
    {
        var k = KernelWithWorld001();
        var s = k.State;

        // Add an opt-in construction site bound to mkt_a (IndustrySystem expects site.NodeId to be a market id).
        var siteId = "site_1";
        s.IndustrySites[siteId] = new SimCore.Entities.IndustrySite
        {
            Active = true,
            NodeId = "mkt_a",
            ConstructionEnabled = true
        };

        // Ensure stage input goods exist as keys in market inventory (start at 0 so the program must supply them).
        var in0 = SimCore.Tweaks.IndustryTweaksV0.Stage0InGood;
        var q0 = SimCore.Tweaks.IndustryTweaksV0.Stage0InQty;
        var in1 = SimCore.Tweaks.IndustryTweaksV0.Stage1InGood;
        var q1 = SimCore.Tweaks.IndustryTweaksV0.Stage1InQty;

        if (!s.Markets["mkt_a"].Inventory.ContainsKey(in0)) s.Markets["mkt_a"].Inventory[in0] = 0;
        if (!s.Markets["mkt_a"].Inventory.ContainsKey(in1)) s.Markets["mkt_a"].Inventory[in1] = 0;

        // Seed player cargo with enough inputs to supply both stages deterministically via SellIntents.
        s.PlayerCargo[in0] = q0 + q0;
        s.PlayerCargo[in1] = q1 + q1;

        // Create and run the construction program bound to the site.
        var pid = s.CreateConstrCapModuleProgramV0(siteId, cadenceTicks: 1);
        s.Programs.Instances[pid].Status = ProgramStatus.Running;
        s.Programs.Instances[pid].NextRunTick = s.Tick;

        // Run enough ticks to cover both stages:
        // Start tick does not decrement StageTicksRemaining, so add a small buffer.
        var d0 = SimCore.Tweaks.IndustryTweaksV0.Stage0DurationTicks;
        var d1 = SimCore.Tweaks.IndustryTweaksV0.Stage1DurationTicks;
        var ticks = d0 + d1 + 6;

        var outGood = SimCore.Tweaks.IndustryTweaksV0.Stage1OutGood;
        var before = s.Markets["mkt_a"].Inventory.TryGetValue(outGood, out var v0) ? v0 : 0;

        for (int i = 0; i < ticks; i++)
        {
            k.Step();
        }

        var after = s.Markets["mkt_a"].Inventory.TryGetValue(outGood, out var v1) ? v1 : 0;

        // Module production proof: output good increased in the market.
        Assert.That(after, Is.GreaterThan(before));
    }

    [Test]
    public void PROG_EXEC_004_ExpeditionProgram_ProducesAcceptedLead_Or_RejectReason_Seed42_SiteBlueprint()
    {
        // GATE.S3_6.EXPEDITION_PROGRAMS.002
        // Proof: EXPEDITION_V0 program runs over IntelTweaksV0.ExpeditionDurationTicks then emits
        // ExpeditionIntentV0 which sets LastExpeditionAcceptedLeadId (lead found in Intel.Discoveries)
        // or LastExpeditionRejectReason (SiteNotFound). Against Seed 42.
        const int seed = 42;
        const string leadId = "lead_exped_42";
        const string fleetId = "fleet_exped_42";

        var k = new SimKernel(seed);
        var s = k.State;

        // Seed a discovery entry so the intent can resolve the lead.
        s.Intel.Discoveries[leadId] = new SimCore.Entities.DiscoveryStateV0
        {
            DiscoveryId = leadId,
            Phase = SimCore.Entities.DiscoveryPhase.Seen
        };

        s.Fleets[fleetId] = new SimCore.Entities.Fleet
        {
            Id = fleetId,
            State = SimCore.Entities.FleetState.Idle
        };

        var pid = s.CreateExpeditionProgramV0(leadId, fleetId, cadenceTicks: 1);
        s.Programs.Instances[pid].Status = ProgramStatus.Running;
        s.Programs.Instances[pid].NextRunTick = s.Tick;

        // First tick arms. Each subsequent tick decrements. Completion fires when TicksRemaining reaches 0.
        var runTicks = SimCore.Tweaks.IntelTweaksV0.ExpeditionDurationTicks + 2;
        for (int i = 0; i < runTicks; i++)
        {
            k.Step();
        }

        var leadSet = !string.IsNullOrEmpty(s.LastExpeditionAcceptedLeadId);
        var rejectSet = !string.IsNullOrEmpty(s.LastExpeditionRejectReason);

        Assert.That(leadSet || rejectSet, Is.True,
            $"PROG_EXEC_004: ExpeditionIntentV0 must have fired and set LastExpeditionAcceptedLeadId or LastExpeditionRejectReason (Seed={seed} LeadId={leadId} ProgramId={pid}).");

        // With a valid discovery entry the intent must succeed.
        Assert.That(leadSet, Is.True,
            $"PROG_EXEC_004: Expected lead accepted for known LeadId (Seed={seed} LeadId={leadId} RejectReason={s.LastExpeditionRejectReason}).");
        Assert.That(s.LastExpeditionAcceptedLeadId, Is.EqualTo(leadId),
            $"PROG_EXEC_004: AcceptedLeadId must match the seeded LeadId (Seed={seed}).");
    }

    [Test]
    public void TradeCharter_Seed42_EmitsCashDelta()
    {
        // GATE.S3_6.EXPLOITATION_PACKAGES.002
        // Proof: TRADE_CHARTER_V0 program emits TradePnL token in ExploitationEventLog
        // and net credits change after buy+sell cycle. Seed 42.
        // Determinism: no wall-clock, no shared RNG; token ordering stable (tick-order Apply calls).
        const int seed = 42;

        var k = new SimKernel(seed);
        var s = k.State;

        var def = new WorldDefinition
        {
            WorldId = "tc_world_42",
            Markets =
            {
                new WorldMarket { Id = "mkt_src", Inventory = new() { ["ore"] = 20 } },
                new WorldMarket { Id = "mkt_dst", Inventory = new() { ["ore"] = 0  } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_src", Kind = "Station", Name = "Source", MarketId = "mkt_src", Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_dst", Kind = "Station", Name = "Dest",   MarketId = "mkt_dst", Pos = new float[] { 5f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_sd", FromNodeId = "stn_src", ToNodeId = "stn_dst", Distance = 1.0f, TotalCapacity = 5 }
            },
            Player = new WorldPlayerStart { Credits = 1000, LocationNodeId = "stn_src" }
        };
        WorldLoader.Apply(s, def);

        // Seed player cargo so the sell leg fires immediately on tick 1.
        s.PlayerCargo["ore"] = 4;

        var credBefore = s.PlayerCredits;

        // Create and arm: buy ore from mkt_src, sell ore to mkt_dst.
        var pid = s.CreateTradeCharterV0Program("mkt_src", "mkt_dst", "ore", "ore", cadenceTicks: 1);
        s.Programs.Instances[pid].Status = ProgramStatus.Running;
        s.Programs.Instances[pid].NextRunTick = s.Tick;

        for (int i = 0; i < 5; i++) k.Step();

        // Credits must have changed (buy deducts, sell adds; net depends on tweaks).
        Assert.That(s.PlayerCredits, Is.Not.EqualTo(credBefore),
            $"TradeCharter_Seed42_EmitsCashDelta: PlayerCredits must change after buy+sell cycle (Seed={seed}).");

        // ExploitationEventLog must contain TradePnL (sell leg token).
        Assert.That(s.ExploitationEventLog.Exists(e => e.Contains("TradePnL")), Is.True,
            $"TradeCharter_Seed42_EmitsCashDelta: ExploitationEventLog must contain TradePnL (Seed={seed} count={s.ExploitationEventLog.Count}).");

        // ExploitationEventLog must contain InventoryLoaded (buy leg token).
        Assert.That(s.ExploitationEventLog.Exists(e => e.Contains("InventoryLoaded")), Is.True,
            $"TradeCharter_Seed42_EmitsCashDelta: ExploitationEventLog must contain InventoryLoaded (Seed={seed}).");

        // Save/load mid-execution: ExploitationEventLog must survive round-trip.
        var logCountBefore = s.ExploitationEventLog.Count;
        var json = k.SaveToString();
        k.LoadFromString(json);
        Assert.That(k.State.ExploitationEventLog.Count, Is.EqualTo(logCountBefore),
            $"TradeCharter_Seed42_EmitsCashDelta: ExploitationEventLog count must survive save/load (Seed={seed}).");
    }

    [Test]
    public void ResourceTap_Seed42_EmitsInventoryDelta()
    {
        // GATE.S3_6.EXPLOITATION_PACKAGES.002
        // Proof: RESOURCE_TAP_V0 program emits Produced and InventoryUnloaded tokens in ExploitationEventLog
        // and moves goods to player cargo. Seed 42.
        // Determinism: no wall-clock, no shared RNG; token ordering stable.
        const int seed = 42;

        var k = new SimKernel(seed);
        var s = k.State;

        var def = new WorldDefinition
        {
            WorldId = "rt_world_42",
            Markets =
            {
                new WorldMarket { Id = "mkt_site", Inventory = new() { ["ore"] = 0 } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_site", Kind = "Station", Name = "Site", MarketId = "mkt_site", Pos = new float[] { 0f, 0f, 0f } }
            },
            Edges = { },
            Player = new WorldPlayerStart { Credits = 500, LocationNodeId = "stn_site" }
        };
        WorldLoader.Apply(s, def);

        var cargoBefore = s.PlayerCargo.TryGetValue("ore", out var cb) ? cb : 0;

        var pid = s.CreateResourceTapV0Program("mkt_site", "ore", cadenceTicks: 1);
        s.Programs.Instances[pid].Status = ProgramStatus.Running;
        s.Programs.Instances[pid].NextRunTick = s.Tick;

        for (int i = 0; i < 4; i++) k.Step();

        // ExploitationEventLog must contain Produced token.
        Assert.That(s.ExploitationEventLog.Exists(e => e.Contains("Produced")), Is.True,
            $"ResourceTap_Seed42_EmitsInventoryDelta: ExploitationEventLog must contain Produced (Seed={seed} count={s.ExploitationEventLog.Count}).");

        // ExploitationEventLog must contain InventoryUnloaded token.
        Assert.That(s.ExploitationEventLog.Exists(e => e.Contains("InventoryUnloaded")), Is.True,
            $"ResourceTap_Seed42_EmitsInventoryDelta: ExploitationEventLog must contain InventoryUnloaded (Seed={seed}).");

        // Player cargo ore must have increased.
        var cargoAfter = s.PlayerCargo.TryGetValue("ore", out var ca) ? ca : 0;
        Assert.That(cargoAfter, Is.GreaterThan(cargoBefore),
            $"ResourceTap_Seed42_EmitsInventoryDelta: PlayerCargo[ore] must increase (Seed={seed} before={cargoBefore} after={cargoAfter}).");

        // Save/load mid-execution: ExploitationEventLog must survive round-trip.
        var logCountBefore = s.ExploitationEventLog.Count;
        var json = k.SaveToString();
        k.LoadFromString(json);
        Assert.That(k.State.ExploitationEventLog.Count, Is.EqualTo(logCountBefore),
            $"ResourceTap_Seed42_EmitsInventoryDelta: ExploitationEventLog count must survive save/load (Seed={seed}).");
    }
}
