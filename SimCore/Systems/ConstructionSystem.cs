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
        public readonly List<string> SortedDiscoveryIds = new();
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
        int activeCount = 0;
        int nodeCount = 0;
        foreach (var p in state.Construction.Projects.Values)
        {
            if (p.Completed) continue;
            activeCount++;
            if (string.Equals(p.NodeId, nodeId, StringComparison.Ordinal)) nodeCount++;
        }
        if (activeCount >= ConstructionTweaksV0.MaxTotalProjects)
            return new StartResult { Success = false, Reason = "max_total_projects" };

        // Check max projects per node
        if (nodeCount >= ConstructionTweaksV0.MaxProjectsPerNode)
            return new StartResult { Success = false, Reason = "max_projects_at_node" };

        // Check initial credits
        if (state.PlayerCredits < def.CreditCostPerStep)
            return new StartResult { Success = false, Reason = "insufficient_credits" };

        // GATE.EXTRACT.BRIDGE_WIRE.001: Extraction requires analyzed discovery at node.
        if (def.Type == ConstructionType.Extraction && !HasAnalyzedDiscoveryAtNode(state, nodeId))
            return new StartResult { Success = false, Reason = "no_analyzed_discovery" };

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

        // GATE.EXTRACT.BUILD_CREATES_INDUSTRY.001: Create IndustrySite when extraction station completes.
        var def = ConstructionContentV0.GetById(project.ProjectDefId);
        if (def != null && def.Type == ConstructionType.Extraction)
        {
            CreateExtractionSite(state, project);
        }
    }

    // GATE.EXTRACT.BUILD_CREATES_INDUSTRY.001: Create an IndustrySite at the node
    // that produces the discovery's associated good.
    private static void CreateExtractionSite(SimState state, ConstructionProject project)
    {
        var nodeId = project.NodeId;
        if (string.IsNullOrEmpty(nodeId) || !state.Nodes.TryGetValue(nodeId, out var node)) return;

        // Look up the node's SeededDiscoveryIds for an analyzed discovery.
        string outputGood = "";
        if (node.SeededDiscoveryIds != null && state.Intel?.Discoveries != null)
        {
            var scratch = s_scratch.GetOrCreateValue(state);
            var sortedDiscIds = scratch.SortedDiscoveryIds;
            sortedDiscIds.Clear();
            foreach (var d in node.SeededDiscoveryIds) sortedDiscIds.Add(d);
            sortedDiscIds.Sort(StringComparer.Ordinal);
            foreach (var discId in sortedDiscIds)
            {
                if (!state.Intel.Discoveries.TryGetValue(discId, out var disc)) continue;
                if (disc.Phase != DiscoveryPhase.Analyzed) continue;

                // Parse discovery kind and map to output good.
                string kind = DiscoveryOutcomeSystem.ParseDiscoveryKind(discId);
                string family = kind switch
                {
                    "RESOURCE_POOL_MARKER" => "RUIN",
                    "CORRIDOR_TRACE" => "SIGNAL",
                    _ => kind
                };

                outputGood = family switch
                {
                    "RUIN" => WellKnownGoodIds.ExoticMatter,
                    "DERELICT" => WellKnownGoodIds.SalvagedTech,
                    "SIGNAL" => WellKnownGoodIds.RareMetals,
                    _ => ""
                };

                if (!string.IsNullOrEmpty(outputGood)) break;
            }
        }

        // Fallback: if no analyzed discovery found, default to rare_metals.
        if (string.IsNullOrEmpty(outputGood))
            outputGood = WellKnownGoodIds.RareMetals;

        var siteId = $"extract_{project.ProjectId}";
        var outputs = new Dictionary<string, int>
        {
            [outputGood] = ExtractionTweaksV0.ExtractionOutputPerTick
        };

        // GATE.T55.SUPPLY.RARE_METALS_EXTRACTION.001: Mining-related extraction sites
        // produce rare_metals as secondary output (1 unit/tick) alongside primary good.
        if (node.SeededDiscoveryIds != null && state.Intel?.Discoveries != null)
        {
            var scratch2 = s_scratch.GetOrCreateValue(state);
            var sortedDiscIds2 = scratch2.SortedDiscoveryIds;
            sortedDiscIds2.Clear();
            foreach (var d in node.SeededDiscoveryIds) sortedDiscIds2.Add(d);
            sortedDiscIds2.Sort(StringComparer.Ordinal);
            foreach (var discId2 in sortedDiscIds2)
            {
                if (!state.Intel.Discoveries.TryGetValue(discId2, out var disc2)) continue;
                if (disc2.Phase != DiscoveryPhase.Analyzed) continue;

                string kind2 = DiscoveryOutcomeSystem.ParseDiscoveryKind(discId2);
                if (kind2 == "RESOURCE_POOL_MARKER")
                {
                    // Check RefId segment (index 3) for mining indicators.
                    var parts = discId2.Split('|');
                    string refId = parts.Length >= 4 ? parts[3] : "";
                    if (refId.Contains("ore", StringComparison.OrdinalIgnoreCase) ||
                        refId.Contains("mine", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!outputs.ContainsKey(WellKnownGoodIds.RareMetals))
                            outputs[WellKnownGoodIds.RareMetals] = 1;
                        break;
                    }
                }
            }
        }

        var site = new IndustrySite
        {
            Id = siteId,
            NodeId = nodeId,
            Active = true,
            Outputs = outputs
        };

        state.IndustrySites[siteId] = site;
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

        int activeCount2 = 0;
        int nodeCount2 = 0;
        foreach (var p in state.Construction.Projects.Values)
        {
            if (p.Completed) continue;
            activeCount2++;
            if (string.Equals(p.NodeId, nodeId, StringComparison.Ordinal)) nodeCount2++;
        }
        if (activeCount2 >= ConstructionTweaksV0.MaxTotalProjects)
            return "max_total_projects";

        if (nodeCount2 >= ConstructionTweaksV0.MaxProjectsPerNode)
            return "max_projects_at_node";

        if (state.PlayerCredits < def.CreditCostPerStep)
            return "insufficient_credits";

        // GATE.EXTRACT.BRIDGE_WIRE.001: Extraction requires analyzed discovery at node.
        if (def.Type == ConstructionType.Extraction && !HasAnalyzedDiscoveryAtNode(state, nodeId))
            return "no_analyzed_discovery";

        return "";
    }

    /// <summary>
    /// GATE.EXTRACT.BRIDGE_WIRE.001: Check if the node has at least one analyzed discovery
    /// of a relevant family (RUIN, SIGNAL, DERELICT).
    /// </summary>
    public static bool HasAnalyzedDiscoveryAtNode(SimState state, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        if (!state.Nodes.TryGetValue(nodeId, out var node)) return false;
        if (node.SeededDiscoveryIds == null || node.SeededDiscoveryIds.Count == 0) return false;
        if (state.Intel?.Discoveries == null) return false;

        foreach (var discId in node.SeededDiscoveryIds)
        {
            if (!state.Intel.Discoveries.TryGetValue(discId, out var disc)) continue;
            if (disc.Phase != DiscoveryPhase.Analyzed) continue;

            string kind = DiscoveryOutcomeSystem.ParseDiscoveryKind(discId);
            // Accept RESOURCE_POOL_MARKER (RUIN), CORRIDOR_TRACE (SIGNAL), or any DERELICT.
            if (kind == "RESOURCE_POOL_MARKER" || kind == "CORRIDOR_TRACE" || kind == "DERELICT")
                return true;
        }

        return false;
    }
}
