using NUnit.Framework;
using SimCore;
using SimCore.Gen;
using SimCore.Entities;
using SimCore.Content;
using SimCore.Systems;
using SimCore.Tweaks;
using System.Collections.Generic;
using System.Linq;

namespace SimCore.Tests.Systems;

// GATE.S4.CONSTR_PROG.SYSTEM.001 + GATE.S4.CONSTR_PROG.SAVE.001: Construction system contract tests.
public class ConstructionSystemTests
{
    private SimState CreateTestState()
    {
        var kernel = new SimKernel(42);
        GalaxyGenerator.Generate(kernel.State, 12, 100f);
        kernel.State.PlayerCredits = 10000;
        return kernel.State;
    }

    [Test]
    public void StartConstruction_ValidProject_Succeeds()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();

        var result = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ProjectId, Is.Not.Empty);
        Assert.That(state.Construction.Projects.ContainsKey(result.ProjectId), Is.True);
    }

    [Test]
    public void StartConstruction_UnknownProject_Fails()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();

        var result = ConstructionSystem.StartConstruction(state, "nonexistent", nodeId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("unknown_project"));
    }

    [Test]
    public void StartConstruction_UnknownNode_Fails()
    {
        var state = CreateTestState();

        var result = ConstructionSystem.StartConstruction(state, "constr_depot_v0", "fake_node");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("unknown_node"));
    }

    [Test]
    public void StartConstruction_InsufficientCredits_Fails()
    {
        var state = CreateTestState();
        state.PlayerCredits = 0;
        var nodeId = state.Nodes.Keys.First();

        var result = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("insufficient_credits"));
    }

    [Test]
    public void StartConstruction_MaxTotalProjects_Fails()
    {
        var state = CreateTestState();
        var nodeIds = state.Nodes.Keys.Take(4).ToList();

        // Start MaxTotalProjects projects
        for (int i = 0; i < ConstructionTweaksV0.MaxTotalProjects; i++)
        {
            var r = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeIds[i % nodeIds.Count]);
            Assert.That(r.Success, Is.True, $"Project {i} should succeed");
        }

        // Next one should fail
        var result = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeIds[0]);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reason, Is.EqualTo("max_total_projects"));
    }

    [Test]
    public void ProcessConstruction_AdvancesProgress()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();

        var startResult = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);
        Assert.That(startResult.Success, Is.True);

        var project = state.Construction.Projects[startResult.ProjectId];
        Assert.That(project.StepProgressTicks, Is.EqualTo(0));

        // Process one tick
        ConstructionSystem.ProcessConstruction(state);

        Assert.That(project.StepProgressTicks, Is.GreaterThan(0));
    }

    [Test]
    public void ProcessConstruction_CompletesProject()
    {
        var state = CreateTestState();
        state.PlayerCredits = 1000000; // Ensure enough credits
        var nodeId = state.Nodes.Keys.First();

        var startResult = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);
        var project = state.Construction.Projects[startResult.ProjectId];
        var def = ConstructionContentV0.GetById("constr_depot_v0")!;

        // Process enough ticks to complete
        int maxTicks = def.TotalSteps * def.TicksPerStep + 10;
        for (int i = 0; i < maxTicks && !project.Completed; i++)
        {
            ConstructionSystem.ProcessConstruction(state);
        }

        Assert.That(project.Completed, Is.True);
        Assert.That(project.CompletedTick, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void ProcessConstruction_StallsOnInsufficientCredits()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();

        var startResult = ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);
        var project = state.Construction.Projects[startResult.ProjectId];

        // Set credits to 0 — should stall
        state.PlayerCredits = 0;
        ConstructionSystem.ProcessConstruction(state);

        Assert.That(project.StepProgressTicks, Is.EqualTo(0));
        Assert.That(project.Completed, Is.False);
    }

    [Test]
    public void StartConstruction_LogsEvent()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();

        int eventsBefore = state.Construction.EventLog.Count;
        ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);

        Assert.That(state.Construction.EventLog.Count, Is.GreaterThan(eventsBefore));
        Assert.That(state.Construction.EventLog.Last().EventType, Is.EqualTo("Started"));
    }

    [Test]
    public void GetBlockReason_NoBlock_ReturnsEmpty()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();

        var reason = ConstructionSystem.GetBlockReason(state, "constr_depot_v0", nodeId);

        Assert.That(reason, Is.EqualTo(""));
    }

    [Test]
    public void ConstructionState_SurvivesSaveLoad()
    {
        var state = CreateTestState();
        var nodeId = state.Nodes.Keys.First();
        ConstructionSystem.StartConstruction(state, "constr_depot_v0", nodeId);

        // Serialize and deserialize
        var json = System.Text.Json.JsonSerializer.Serialize(state);
        var loaded = System.Text.Json.JsonSerializer.Deserialize<SimState>(json)!;
        loaded.HydrateAfterLoad();

        Assert.That(loaded.Construction.Projects.Count, Is.EqualTo(state.Construction.Projects.Count));
    }
}
