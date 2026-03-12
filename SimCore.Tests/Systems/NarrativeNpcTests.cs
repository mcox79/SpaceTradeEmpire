using NUnit.Framework;
using SimCore;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Systems;
using SimCore.Tweaks;

namespace SimCore.Tests.Systems;

// GATE.T18.NARRATIVE.WAR_FACES.001
[TestFixture]
public sealed class NarrativeNpcTests
{
    private static void AdvanceTo(SimState state, int targetTick)
    {
        while (state.Tick < targetTick)
            state.AdvanceTick();
    }

    private SimState MakeStateWithNpcs()
    {
        var state = new SimState(42);

        state.Nodes["star_0"] = new Node { Id = "star_0" };
        state.Nodes["star_1"] = new Node { Id = "star_1" };
        state.NodeFactionId["star_0"] = "Valorin";
        state.NodeFactionId["star_1"] = "Communion";

        state.NarrativeNpcs[WarFacesContentV0.RegularNpcId] = new NarrativeNpc
        {
            NpcId = WarFacesContentV0.RegularNpcId,
            Kind = NarrativeNpcKind.Regular,
            Name = WarFacesContentV0.RegularName,
            NodeId = "star_0",
            HomeNodeId = "star_0",
            FactionId = WarFacesContentV0.RegularFaction,
            IsAlive = true
        };

        state.NarrativeNpcs[WarFacesContentV0.StationmasterNpcId] = new NarrativeNpc
        {
            NpcId = WarFacesContentV0.StationmasterNpcId,
            Kind = NarrativeNpcKind.Stationmaster,
            Name = WarFacesContentV0.StationmasterDefaultName,
            NodeId = "star_0",
            FactionId = "Valorin",
            IsAlive = true
        };

        state.NarrativeNpcs[WarFacesContentV0.EnemyNpcId] = new NarrativeNpc
        {
            NpcId = WarFacesContentV0.EnemyNpcId,
            Kind = NarrativeNpcKind.Enemy,
            Name = WarFacesContentV0.EnemyName,
            NodeId = "star_1",
            FactionId = WarFacesContentV0.EnemyFaction,
            IsAlive = true
        };

        return state;
    }

    [Test]
    public void Regular_VanishesWhenWarReachesHome()
    {
        var state = MakeStateWithNpcs();

        state.Warfronts["wf_1"] = new WarfrontState
        {
            Id = "wf_1",
            CombatantA = "Valorin",
            CombatantB = "Communion",
            Intensity = WarfrontIntensity.Skirmish
        };

        NarrativeNpcSystem.Process(state);
        var npc = state.NarrativeNpcs[WarFacesContentV0.RegularNpcId];
        Assert.That(npc.VanishTick, Is.GreaterThan(0));
        Assert.That(npc.IsAlive, Is.True);

        AdvanceTo(state, npc.VanishTick);
        NarrativeNpcSystem.Process(state);
        Assert.That(npc.IsAlive, Is.False);
        Assert.That(npc.VanishReason, Is.EqualTo(WarFacesContentV0.RegularVanishBulletin));
    }

    [Test]
    public void Regular_NoVanishWithoutWarfront()
    {
        var state = MakeStateWithNpcs();
        NarrativeNpcSystem.Process(state);
        var npc = state.NarrativeNpcs[WarFacesContentV0.RegularNpcId];
        Assert.That(npc.VanishTick, Is.EqualTo(0));
        Assert.That(npc.IsAlive, Is.True);
    }

    [Test]
    public void Regular_MentionIsDeterministic()
    {
        var state = MakeStateWithNpcs();
        AdvanceTo(state, 100);

        string mention1 = NarrativeNpcSystem.GetRegularMention(state, "star_0");
        string mention2 = NarrativeNpcSystem.GetRegularMention(state, "star_0");

        Assert.That(mention1, Is.Not.Empty);
        Assert.That(mention1, Is.EqualTo(mention2));
    }

    [Test]
    public void Regular_GhostMentionWhenDead()
    {
        // GATE.T18.CHARACTER.WARFACES_DEPTH.001: Dead Regular returns ghost mentions, not empty.
        var state = MakeStateWithNpcs();
        state.NarrativeNpcs[WarFacesContentV0.RegularNpcId].IsAlive = false;

        string mention = NarrativeNpcSystem.GetRegularMention(state, "star_0");
        Assert.That(mention, Is.Not.Empty);
        Assert.That(WarFacesContentV0.RegularGhostMentions, Does.Contain(mention));
    }

    [Test]
    public void Stationmaster_DialogueFiresOnce()
    {
        var state = MakeStateWithNpcs();

        string line1 = NarrativeNpcSystem.TryStationmasterDialogue(state, "SM_FIRST_MUNITIONS");
        Assert.That(line1, Is.Not.Empty);

        string line2 = NarrativeNpcSystem.TryStationmasterDialogue(state, "SM_FIRST_MUNITIONS");
        Assert.That(line2, Is.Empty);
    }

    [Test]
    public void Stationmaster_TriggerForDelivery_ReturnsCorrectToken()
    {
        var state = MakeStateWithNpcs();

        string token = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Munitions);
        Assert.That(token, Is.EqualTo("SM_FIRST_MUNITIONS"));

        NarrativeNpcSystem.TryStationmasterDialogue(state, "SM_FIRST_MUNITIONS");

        token = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Munitions);
        Assert.That(token, Is.EqualTo("SM_REPEAT_MUNITIONS"));
    }

    [Test]
    public void Stationmaster_FoodDeliveryTrigger()
    {
        var state = MakeStateWithNpcs();
        string token = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Food);
        Assert.That(token, Is.EqualTo("SM_FOOD_DELIVERY"));
    }

    [Test]
    public void Stationmaster_EmptyForUnknownGood()
    {
        var state = MakeStateWithNpcs();
        string token = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, "unobtainium");
        Assert.That(token, Is.Empty);
    }

    [Test]
    public void Stationmaster_ReliableTriggerTakesPriority()
    {
        var state = MakeStateWithNpcs();
        var smNpc = state.NarrativeNpcs[WarFacesContentV0.StationmasterNpcId];

        for (int i = 0; i < NarrativeTweaksV0.StationmasterReliableThreshold; i++)
            StationMemorySystem.RecordDelivery(state, smNpc.NodeId, WellKnownGoodIds.Food, 10);

        string token = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Food);
        Assert.That(token, Is.EqualTo("SM_RELIABLE"));
    }

    [Test]
    public void Enemy_InterdictionFiresOnce()
    {
        var state = MakeStateWithNpcs();

        string text1 = NarrativeNpcSystem.TriggerInterdiction(state);
        Assert.That(text1, Is.Not.Empty);
        Assert.That(text1, Is.EqualTo(WarFacesContentV0.EnemyInterdictionText));

        string text2 = NarrativeNpcSystem.TriggerInterdiction(state);
        Assert.That(text2, Is.Empty);
    }

    [Test]
    public void Enemy_CommunionEncounterAfterDelay()
    {
        var state = MakeStateWithNpcs();
        NarrativeNpcSystem.TriggerInterdiction(state);

        AdvanceTo(state, NarrativeTweaksV0.WarConsequenceDelayTicks - 1);
        NarrativeNpcSystem.Process(state);
        var npc = state.NarrativeNpcs[WarFacesContentV0.EnemyNpcId];
        Assert.That(npc.CommunionEncounterAvailable, Is.False);

        AdvanceTo(state, NarrativeTweaksV0.WarConsequenceDelayTicks);
        NarrativeNpcSystem.Process(state);
        Assert.That(npc.CommunionEncounterAvailable, Is.True);

        string text = NarrativeNpcSystem.CheckCommunionEncounter(state, "star_1");
        Assert.That(text, Is.Not.Empty);
        Assert.That(text, Is.EqualTo(WarFacesContentV0.EnemyCommunionEncounterText));
        Assert.That(npc.CommunionEncounterAvailable, Is.False);
    }

    [Test]
    public void Enemy_CommunionEncounterFailsAtNonCommunionNode()
    {
        var state = MakeStateWithNpcs();
        NarrativeNpcSystem.TriggerInterdiction(state);

        AdvanceTo(state, NarrativeTweaksV0.WarConsequenceDelayTicks);
        NarrativeNpcSystem.Process(state);

        string text = NarrativeNpcSystem.CheckCommunionEncounter(state, "star_0");
        Assert.That(text, Is.Empty);
    }

    [Test]
    public void Process_EmptyNpcs_NoException()
    {
        var state = new SimState(42);
        NarrativeNpcSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void Process_DeadNpcs_Skipped()
    {
        var state = MakeStateWithNpcs();
        foreach (var kv in state.NarrativeNpcs)
            kv.Value.IsAlive = false;
        NarrativeNpcSystem.Process(state);
        Assert.Pass();
    }

    [Test]
    public void Regular_PeaceWarfront_NoVanish()
    {
        var state = MakeStateWithNpcs();
        state.Warfronts["wf_1"] = new WarfrontState
        {
            Id = "wf_1",
            CombatantA = "Valorin",
            CombatantB = "Communion",
            Intensity = WarfrontIntensity.Peace // intensity 0
        };

        NarrativeNpcSystem.Process(state);
        var npc = state.NarrativeNpcs[WarFacesContentV0.RegularNpcId];
        Assert.That(npc.VanishTick, Is.EqualTo(0));
        Assert.That(npc.IsAlive, Is.True);
    }

    [Test]
    public void Stationmaster_DeadNpc_DialogueReturnsEmpty()
    {
        var state = MakeStateWithNpcs();
        state.NarrativeNpcs[WarFacesContentV0.StationmasterNpcId].IsAlive = false;

        string line = NarrativeNpcSystem.TryStationmasterDialogue(state, "SM_FIRST_MUNITIONS");
        Assert.That(line, Is.Empty);
    }

    [Test]
    public void Stationmaster_DeadNpc_TriggerReturnsEmpty()
    {
        var state = MakeStateWithNpcs();
        state.NarrativeNpcs[WarFacesContentV0.StationmasterNpcId].IsAlive = false;

        string token = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Munitions);
        Assert.That(token, Is.Empty);
    }

    [Test]
    public void Enemy_DeadNpc_InterdictionReturnsEmpty()
    {
        var state = MakeStateWithNpcs();
        state.NarrativeNpcs[WarFacesContentV0.EnemyNpcId].IsAlive = false;

        string text = NarrativeNpcSystem.TriggerInterdiction(state);
        Assert.That(text, Is.Empty);
    }

    [Test]
    public void Enemy_CommunionEncounter_NonexistentNode_ReturnsEmpty()
    {
        var state = MakeStateWithNpcs();
        NarrativeNpcSystem.TriggerInterdiction(state);
        AdvanceTo(state, NarrativeTweaksV0.WarConsequenceDelayTicks);
        NarrativeNpcSystem.Process(state);

        string text = NarrativeNpcSystem.CheckCommunionEncounter(state, "nonexistent_node");
        Assert.That(text, Is.Empty);
    }

    [Test]
    public void Stationmaster_CompositesAndElectronics_HaveTriggers()
    {
        var state = MakeStateWithNpcs();

        string compToken = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Composites);
        Assert.That(compToken, Is.EqualTo("SM_COMPOSITES"));

        string elecToken = NarrativeNpcSystem.GetStationmasterTriggerForDelivery(
            state, WellKnownGoodIds.Electronics);
        Assert.That(elecToken, Is.EqualTo("SM_ELECTRONICS"));
    }
}
