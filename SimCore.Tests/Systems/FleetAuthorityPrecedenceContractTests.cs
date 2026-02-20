using System;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Commands;
using SimCore.Entities;
using SimCore.Events;
using SimCore.Gen;
using SimCore.Systems;

namespace SimCore.Tests.Systems;

public class FleetAuthorityPrecedenceContractTests
{
    [Test]
    public void ManualOverride_CancelsActiveLogisticsJob_AndDoesNotResumeWhenCleared()
    {
        const int seed = 123;

        var sim = new SimKernel(seed);
        GalaxyGenerator.Generate(sim.State, 20, 100f);

        // Prevent LogisticsSystem from assigning a new job after override cancels the current one.
        foreach (var site in sim.State.IndustrySites.Values)
            site.Active = false;

        var fleet = sim.State.Fleets.Values
            .OrderBy(f => f.Id, StringComparer.Ordinal)
            .First();

        // Install an active job and some route state.
        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = "ore",
            SourceNodeId = "N_A",
            TargetNodeId = "N_B",
            Amount = 5,
            Phase = LogisticsJobPhase.Pickup,
            PickupTransferIssued = true,
            DeliveryTransferIssued = false
        };
        fleet.RouteEdgeIds.Clear();
        fleet.RouteEdgeIds.Add("E_1");
        fleet.RouteEdgeIndex = 0;
        fleet.FinalDestinationNodeId = "N_A";
        fleet.CurrentTask = "Fetching ore from N_A";

        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.LogisticsJob),
            "With an active logistics job and no manual override, authority must be LogisticsJob.");

        // Deterministic override target that exists and differs from current.
        var targetNodeId = sim.State.Nodes.Values
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Select(n => n.Id)
            .First(id => !string.Equals(id, fleet.CurrentNodeId, StringComparison.Ordinal));

        // Act: issuing manual override must cancel job.
        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, targetNodeId, "test_override"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(targetNodeId));
        Assert.That(fleet.CurrentJob, Is.Null, "Manual override must cancel active logistics job.");
        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.ManualOverride),
        "While override is set, authority must be ManualOverride.");

        // Do not assert FinalDestinationNodeId immediately after the command.
        // MovementSystem may already have an in-flight request; the override doctrine is that it takes precedence
        // and should drive FinalDestinationNodeId on the next planning observation.

        // Allow MovementSystem to observe override and set/plan toward override.
        sim.Step();

        Assert.That(fleet.FinalDestinationNodeId, Is.EqualTo(targetNodeId),
            "While override is set, routing request must align to override.");

        // Clearing override must NOT resume canceled job.
        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, "", "test_clear"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(""));
        Assert.That(fleet.CurrentJob, Is.Null, "Clearing override must not resume canceled jobs.");
        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.None),
        "After override is cleared and no job is active, authority must be None.");
        // Program controller path and precedence.
        fleet.ProgramId = "P_TEST";
        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.Program),
            "With ProgramId set and no job and no override, authority must be Program.");

        // Job must take precedence over Program.
        fleet.CurrentJob = new LogisticsJob
        {
            GoodId = "ore",
            SourceNodeId = "N_A",
            TargetNodeId = "N_B",
            Amount = 5,
            Phase = LogisticsJobPhase.Pickup,
            PickupTransferIssued = true,
            DeliveryTransferIssued = false
        };
        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.LogisticsJob),
            "With an active logistics job, authority must be LogisticsJob even if ProgramId is set.");

        // Manual override must take precedence over both Job and Program, and must cancel the job deterministically.
        var targetNodeId2 = sim.State.Nodes.Values
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Select(n => n.Id)
            .First(id => !string.Equals(id, fleet.CurrentNodeId, StringComparison.Ordinal));

        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, targetNodeId2, "test_override_2"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(targetNodeId2));
        Assert.That(fleet.CurrentJob, Is.Null, "Manual override must cancel active logistics job (program precedence scenario).");
        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.ManualOverride),
            "While override is set, authority must be ManualOverride even if ProgramId is set.");

        // Clearing override should return authority to Program (job stays canceled).
        sim.EnqueueCommand(new FleetSetDestinationCommand(fleet.Id, "", "test_clear_2"));
        sim.Step();

        Assert.That(fleet.ManualOverrideNodeId, Is.EqualTo(""));
        Assert.That(fleet.CurrentJob, Is.Null, "Clearing override must not resume canceled jobs (program precedence scenario).");
        Assert.That(fleet.ActiveController, Is.EqualTo(FleetActiveController.Program),
            "After override is cleared and ProgramId is set, authority must be Program.");
    }

    private sealed class TestRouteChoiceIntent : IntentSystem.IFleetRouteChoiceIntent
    {
        public string Kind { get; }
        public string FleetId { get; }
        public string RouteId { get; }
        public int ProfitScore { get; }
        public int CapacityScore { get; }
        public int RiskScore { get; }

        public TestRouteChoiceIntent(string kind, string fleetId, string routeId, int profit, int capacity, int risk)
        {
            Kind = kind;
            FleetId = fleetId;
            RouteId = routeId;
            ProfitScore = profit;
            CapacityScore = capacity;
            RiskScore = risk;
        }

        public void Apply(SimState state)
        {
            if (state is null) return;
            if (string.IsNullOrWhiteSpace(FleetId)) return;
            if (!state.Fleets.TryGetValue(FleetId, out var fleet) || fleet is null) return;

            // Persisted proof surface required by gate: survives save%load.
            fleet.LastRouteChoiceRouteId = RouteId;
        }
    }

    [Test]
    public void FleetRoles_RouteChoice_IsDeterministic_EmitsSchemaBoundEvent_AndPersistsThroughSaveLoad()
    {
        const int seed = 7;
        var state = new SimState(seed);

        state.Fleets["F_TRADER"] = new Fleet { Id = "F_TRADER", Role = FleetRole.Trader };
        state.Fleets["F_HAULER"] = new Fleet { Id = "F_HAULER", Role = FleetRole.Hauler };
        state.Fleets["F_PATROL"] = new Fleet { Id = "F_PATROL", Role = FleetRole.Patrol };

        // Crafted competing routes with intentional tradeoffs:
        // A: high profit, low capacity, high risk
        // B: low profit, high capacity, low risk
        const string routeA = "R_A";
        const string routeB = "R_B";

        foreach (var fleetId in state.Fleets.Keys.OrderBy(x => x, StringComparer.Ordinal))
        {
            state.EnqueueIntent(new TestRouteChoiceIntent("route_choice_test", fleetId, routeA, profit: 10, capacity: 1, risk: 9));
            state.EnqueueIntent(new TestRouteChoiceIntent("route_choice_test", fleetId, routeB, profit: 2, capacity: 10, risk: 1));
        }

        // Resolve competing choices deterministically and emit events.
        IntentSystem.Process(state);
        state.AdvanceTick(); // finalizes FleetEventLog Seq deterministically

        Assert.That(state.Fleets["F_TRADER"].LastRouteChoiceRouteId, Is.EqualTo(routeA), "Trader must prefer profit route.");
        Assert.That(state.Fleets["F_HAULER"].LastRouteChoiceRouteId, Is.EqualTo(routeB), "Hauler must prefer capacity route.");
        Assert.That(state.Fleets["F_PATROL"].LastRouteChoiceRouteId, Is.EqualTo(routeB), "Patrol must prefer low-risk route.");

        // Schema-bound event validation (exact keys).
        var payload = FleetEvents.BuildPayload(tick: 0, events: state.FleetEventLog);
        var json = FleetEvents.ToDeterministicJson(payload);
        FleetEvents.ValidateJsonIsSchemaBound(json);

        // Save%load: pending intents are discarded on load, so the persisted fleet field must retain the choice.
        var opts = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        var save = JsonSerializer.Serialize(state, opts);
        var loaded = JsonSerializer.Deserialize<SimState>(save, opts);
        Assert.That(loaded, Is.Not.Null);

        loaded!.HydrateAfterLoad();

        Assert.That(loaded.Fleets["F_TRADER"].LastRouteChoiceRouteId, Is.EqualTo(routeA));
        Assert.That(loaded.Fleets["F_HAULER"].LastRouteChoiceRouteId, Is.EqualTo(routeB));
        Assert.That(loaded.Fleets["F_PATROL"].LastRouteChoiceRouteId, Is.EqualTo(routeB));

        // Event log must persist deterministically as well.
        Assert.That(loaded.FleetEventLog.Count, Is.EqualTo(state.FleetEventLog.Count));
        Assert.That(loaded.FleetEventLog.Select(e => e.Seq).ToArray(), Is.EqualTo(state.FleetEventLog.Select(e => e.Seq).ToArray()));
        Assert.That(loaded.FleetEventLog.Select(e => e.FleetId).ToArray(), Is.EqualTo(state.FleetEventLog.Select(e => e.FleetId).ToArray()));
        Assert.That(loaded.FleetEventLog.Select(e => e.ChosenRouteId).ToArray(), Is.EqualTo(state.FleetEventLog.Select(e => e.ChosenRouteId).ToArray()));
    }
}
