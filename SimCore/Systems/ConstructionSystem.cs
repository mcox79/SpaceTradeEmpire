using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.S4.CONSTR_PROG.SYSTEM.001: Construction system — start, tick progress, complete.
public static class ConstructionSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedProjectIds = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
    public sealed class StartResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; } = "";
        public string ProjectId { get; set; } = "";
    }

    public static StartResult StartConstruction(SimState state, string projectDefId, string nodeId)
    {
        if (string.IsNullOrEmpty(projectDefId))
            return new StartResult { Success = false, Reason = "empty_project_def_id" };

        if (string.IsNullOrEmpty(nodeId))
            return new StartResult { Success = false, Reason = "empty_node_id" };

        var def = ConstructionContentV0.GetById(projectDefId);
        if (def == null)
            return new StartResult { Success = false, Reason = "unknown_project" };

        if (!state.Nodes.ContainsKey(nodeId))
            return new StartResult { Success = false, Reason = "unknown_node" };

        // Check prerequisites
        if (!ConstructionContentV0.PrerequisitesMet(projectDefId, state.Tech.UnlockedTechIds))
            return new StartResult { Success = false, Reason = "prerequisites_not_met" };

        // Check max total projects
        int activeCount = state.Construction.Projects.Values.Count(p => !p.Completed);
        if (activeCount >= ConstructionTweaksV0.MaxTotalProjects)
            return new StartResult { Success = false, Reason = "max_total_projects" };

        // Check max projects per node
        int nodeCount = state.Construction.Projects.Values.Count(p => !p.Completed && p.NodeId == nodeId);
        if (nodeCount >= ConstructionTweaksV0.MaxProjectsPerNode)
            return new StartResult { Success = false, Reason = "max_projects_at_node" };

        // Check initial credits
        if (state.PlayerCredits < def.CreditCostPerStep)
            return new StartResult { Success = false, Reason = "insufficient_credits" };

        // Create project
        var projectId = $"CP{state.Construction.NextProjectSeq}";
        state.Construction.NextProjectSeq++;

        var project = new ConstructionProject
        {
            ProjectId = projectId,
            ProjectDefId = projectDefId,
            NodeId = nodeId,
            CurrentStep = 0,
            StepProgressTicks = 0,
            TotalSteps = def.TotalSteps,
            TicksPerStep = def.TicksPerStep,
            Completed = false,
            StartedTick = state.Tick,
        };

        state.Construction.Projects[projectId] = project;

        // Log event
        state.Construction.EventLog.Add(new ConstructionEvent
        {
            Seq = state.Construction.NextEventSeq++,
            Tick = state.Tick,
            ProjectId = projectId,
            ProjectDefId = projectDefId,
            EventType = "Started",
            StepIndex = 0,
        });

        return new StartResult { Success = true, ProjectId = projectId };
    }

    public static void ProcessConstruction(SimState state)
    {
        if (state?.Construction == null) return;

        // Iterate active projects in deterministic order
        var scratch = s_scratch.GetOrCreateValue(state);
        var projectIds = scratch.SortedProjectIds;
        projectIds.Clear();
        foreach (var k in state.Construction.Projects.Keys) projectIds.Add(k);
        projectIds.Sort(StringComparer.Ordinal);

        foreach (var pid in projectIds)
        {
            var project = state.Construction.Projects[pid];
            if (project.Completed) continue;

            var def = ConstructionContentV0.GetById(project.ProjectDefId);
            if (def == null)
            {
                // Content removed — cancel
                CancelProject(state, project);
                continue;
            }

            // Check credits for this step
            int tickCost = def.CreditCostPerStep / (def.TicksPerStep > 0 ? def.TicksPerStep : 1);
            if (tickCost < 1) tickCost = 1;

            if (state.PlayerCredits < tickCost)
            {
                // Stall — don't progress but don't cancel
                continue;
            }

            // Deduct credits and advance
            state.PlayerCredits -= tickCost;
            project.StepProgressTicks += ConstructionTweaksV0.ProgressPerTick;

            // Check step completion
            if (project.StepProgressTicks >= project.TicksPerStep)
            {
                project.CurrentStep++;
                project.StepProgressTicks = 0;

                state.Construction.EventLog.Add(new ConstructionEvent
                {
                    Seq = state.Construction.NextEventSeq++,
                    Tick = state.Tick,
                    ProjectId = project.ProjectId,
                    ProjectDefId = project.ProjectDefId,
                    EventType = "StepCompleted",
                    StepIndex = project.CurrentStep - 1,
                });

                // Check project completion
                if (project.CurrentStep >= project.TotalSteps)
                {
                    CompleteProject(state, project);
                }
            }
        }
    }

    private static void CompleteProject(SimState state, ConstructionProject project)
    {
        project.Completed = true;
        project.CompletedTick = state.Tick;

        state.Construction.EventLog.Add(new ConstructionEvent
        {
            Seq = state.Construction.NextEventSeq++,
            Tick = state.Tick,
            ProjectId = project.ProjectId,
            ProjectDefId = project.ProjectDefId,
            EventType = "Completed",
            StepIndex = project.CurrentStep,
        });
    }

    private static void CancelProject(SimState state, ConstructionProject project)
    {
        project.Completed = true;
        project.CompletedTick = state.Tick;

        state.Construction.EventLog.Add(new ConstructionEvent
        {
            Seq = state.Construction.NextEventSeq++,
            Tick = state.Tick,
            ProjectId = project.ProjectId,
            ProjectDefId = project.ProjectDefId,
            EventType = "Cancelled",
            StepIndex = project.CurrentStep,
        });
    }

    public static string GetBlockReason(SimState state, string projectDefId, string nodeId)
    {
        if (string.IsNullOrEmpty(projectDefId)) return "empty_project_def_id";

        var def = ConstructionContentV0.GetById(projectDefId);
        if (def == null) return "unknown_project";

        if (string.IsNullOrEmpty(nodeId) || !state.Nodes.ContainsKey(nodeId))
            return "unknown_node";

        if (!ConstructionContentV0.PrerequisitesMet(projectDefId, state.Tech.UnlockedTechIds))
            return "prerequisites_not_met";

        int activeCount = state.Construction.Projects.Values.Count(p => !p.Completed);
        if (activeCount >= ConstructionTweaksV0.MaxTotalProjects)
            return "max_total_projects";

        int nodeCount = state.Construction.Projects.Values.Count(p => !p.Completed && p.NodeId == nodeId);
        if (nodeCount >= ConstructionTweaksV0.MaxProjectsPerNode)
            return "max_projects_at_node";

        if (state.PlayerCredits < def.CreditCostPerStep)
            return "insufficient_credits";

        return "";
    }
}
