using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SimCore.Content;
using SimCore.Entities;
using SimCore.Tweaks;

namespace SimCore.Systems;

// GATE.T42.PLANET_SCAN.ORBITAL.001 through GATE.T42.PLANET_SCAN.INSTAB_LEAD.001:
// Planet scanning system — orbital scan, landing scan, atmospheric sample,
// finding category selection, fragment drops, signal leads, evidence, archives.
public static class PlanetScanSystem
{
    private sealed class Scratch
    {
        public readonly List<string> SortedKeys = new();
    }
    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();

    // GATE.T42.PLANET_SCAN.INSTAB_LEAD.001: Per-tick check for instability reveals.
    // When instability rises at a node with instability-gated discoveries,
    // create a Signal Lead at the player's current location (no backtracking).
    public static void Process(SimState state)
    {
        if (state?.Intel?.Discoveries is null) return;

        // Recharge scanner charges over time.
        ProcessRecharge(state);

        // Check for instability reveals that create new signal leads.
        ProcessInstabilityReveals(state);
    }

    private static void ProcessRecharge(SimState state)
    {
        if (state.ScannerChargesUsed <= 0) return; // STRUCTURAL: nothing to recharge

        int rechargeTicks = PlanetScanTweaksV0.GetRechargeTicks(state.ScannerTier);
        if (rechargeTicks <= 0) return; // STRUCTURAL: safety

        int ticksSinceRecharge = state.Tick - state.ScannerLastRechargeTick;
        if (ticksSinceRecharge >= rechargeTicks)
        {
            state.ScannerChargesUsed = Math.Max(0, state.ScannerChargesUsed - 1); // STRUCTURAL: decrement
            state.ScannerLastRechargeTick = state.Tick;
        }
    }

    // GATE.T42.PLANET_SCAN.INSTAB_LEAD.001: Instability-reveal creates Signal Lead at player location.
    private static void ProcessInstabilityReveals(SimState state)
    {
        if (state.Intel.Discoveries.Count == 0) return; // STRUCTURAL: early exit

        var scratch = s_scratch.GetOrCreateValue(state);
        var sortedDiscIds = scratch.SortedKeys;
        sortedDiscIds.Clear();
        foreach (var k in state.Intel.Discoveries.Keys) sortedDiscIds.Add(k);
        sortedDiscIds.Sort(StringComparer.Ordinal);

        string playerNode = state.PlayerLocationNodeId ?? "";

        foreach (var discId in sortedDiscIds)
        {
            if (!state.Intel.Discoveries.TryGetValue(discId, out var disc)) continue;
            if (disc is null || disc.InstabilityGate <= 0) continue; // STRUCTURAL: skip non-gated

            // Find the node for this discovery.
            string nodeId = FindNodeForDiscovery(state, discId);
            if (string.IsNullOrEmpty(nodeId)) continue;
            if (!state.Nodes.TryGetValue(nodeId, out var node)) continue;

            int localInstability = node.InstabilityLevel;
            if (localInstability < disc.InstabilityGate) continue; // Not yet revealed

            // Check if we already created a signal lead for this reveal.
            string leadKey = $"PLANET_SCAN_INSTAB_LEAD|{discId}";
            if (state.Intel.RumorLeads.ContainsKey(leadKey)) continue;

            // Create a Signal Lead at the player's current location (not backtracking obligation).
            state.Intel.RumorLeads[leadKey] = new RumorLead
            {
                LeadId = leadKey,
                Status = RumorLeadStatus.Active,
                SourceVerbToken = "INSTABILITY_REVEAL",
                Hint = new HintPayloadV0
                {
                    ImpliedPayoffToken = DiscoveryOutcomeSystem.ParseDiscoveryKind(discId),
                    CoarseLocationToken = nodeId
                }
            };
        }
    }

    // ── Public API: Execute orbital scan ──
    // Returns the scan result, or null if scan cannot be performed.
    public static PlanetScanResult? ExecuteOrbitalScan(SimState state, string nodeId, ScanMode mode)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return null;
        if (!state.Planets.TryGetValue(nodeId, out var planet)) return null;

        // Check mode availability based on scanner tier.
        if (!IsModeAvailable(state.ScannerTier, mode)) return null;

        // Check charge budget.
        int maxCharges = PlanetScanTweaksV0.GetMaxCharges(state.ScannerTier);
        if (state.ScannerChargesUsed >= maxCharges) return null;

        // Consume charge.
        state.ScannerChargesUsed++;

        // Record that this mode was used from orbit at this planet.
        planet.OrbitalScans ??= new Dictionary<ScanMode, int>();
        planet.OrbitalScans[mode] = state.Tick;

        // Compute affinity.
        int affinityBps = PlanetScanTweaksV0.GetAffinityBps(mode, planet.Type);

        // Select finding category (deterministic hash-based).
        var category = SelectFindingCategory(state, mode, planet.Type, affinityBps, ScanPhase.Orbital, nodeId);

        // Generate flavor text.
        string flavorText = GenerateScanFlavorText(state, planet.Type, category, nodeId);

        // Generate hint text (what a different mode might find).
        string hintText = GenerateHintText(planet.Type, mode);

        // Create discovery if applicable (ResourceIntel, SignalLead, PhysicalEvidence).
        string discoveryId = "";
        if (category == FindingCategory.ResourceIntel)
            discoveryId = GenerateResourceIntel(state, nodeId, planet);
        else if (category == FindingCategory.SignalLead)
            discoveryId = GenerateSignalLead(state, nodeId, planet, mode);

        // Physical Evidence from orbital = hint only (Seen phase).
        if (category == FindingCategory.PhysicalEvidence)
            discoveryId = GeneratePhysicalEvidence(state, nodeId, planet, DiscoveryPhase.Seen);

        // FragmentCache and DataArchive never from orbital.
        if (category == FindingCategory.FragmentCache || category == FindingCategory.DataArchive)
        {
            // Redistribute to ResourceIntel for orbital scans.
            category = FindingCategory.ResourceIntel;
            discoveryId = GenerateResourceIntel(state, nodeId, planet);
        }

        var scanId = $"SCAN_{state.NextPlanetScanSeq}";
        state.NextPlanetScanSeq++;

        var result = new PlanetScanResult
        {
            ScanId = scanId,
            NodeId = nodeId,
            Mode = mode,
            Phase = ScanPhase.Orbital,
            Category = category,
            DiscoveryId = discoveryId,
            FlavorText = flavorText,
            HintText = hintText,
            Tick = state.Tick,
            AffinityBps = affinityBps
        };

        state.PlanetScanResults[scanId] = result;
        planet.ScanResults ??= new List<string>();
        planet.ScanResults.Add(scanId);

        return result;
    }

    // ── Public API: Execute landing scan ──
    public static PlanetScanResult? ExecuteLandingScan(SimState state, string nodeId, ScanMode mode)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return null;
        if (!state.Planets.TryGetValue(nodeId, out var planet)) return null;

        // Must be landable.
        if (!planet.Landable) return null;
        // Gaseous planets can never be landed on — use AtmosphericSample instead.
        if (planet.Type == PlanetType.Gaseous) return null;

        // Check tech tier for landing.
        if (planet.LandingTechTier > 0 && state.ScannerTier < planet.LandingTechTier) return null;

        // Check mode availability.
        if (!IsModeAvailable(state.ScannerTier, mode)) return null;

        // Check charge budget.
        int maxCharges = PlanetScanTweaksV0.GetMaxCharges(state.ScannerTier);
        if (state.ScannerChargesUsed >= maxCharges) return null;

        // Consume charge.
        state.ScannerChargesUsed++;

        // Record landing scan.
        planet.LandingScanTick = state.Tick;
        planet.LandingScanMode = mode;

        int affinityBps = PlanetScanTweaksV0.GetAffinityBps(mode, planet.Type);

        // Landing scans can produce all 5 categories.
        var category = SelectFindingCategory(state, mode, planet.Type, affinityBps, ScanPhase.Landing, nodeId);

        string flavorText = GenerateScanFlavorText(state, planet.Type, category, nodeId);
        string discoveryId = "";
        string fragmentId = "";
        bool investigationAvailable = false;

        switch (category)
        {
            case FindingCategory.ResourceIntel:
                discoveryId = GenerateResourceIntel(state, nodeId, planet);
                break;
            case FindingCategory.SignalLead:
                discoveryId = GenerateSignalLead(state, nodeId, planet, mode);
                break;
            case FindingCategory.PhysicalEvidence:
                discoveryId = GeneratePhysicalEvidence(state, nodeId, planet, DiscoveryPhase.Analyzed);
                investigationAvailable = true;
                break;
            case FindingCategory.FragmentCache:
                fragmentId = TryDropFragment(state, planet);
                break;
            case FindingCategory.DataArchive:
                GenerateDataArchive(state, nodeId, planet);
                break;
        }

        var scanId = $"SCAN_{state.NextPlanetScanSeq}";
        state.NextPlanetScanSeq++;

        var result = new PlanetScanResult
        {
            ScanId = scanId,
            NodeId = nodeId,
            Mode = mode,
            Phase = ScanPhase.Landing,
            Category = category,
            DiscoveryId = discoveryId,
            FlavorText = flavorText,
            Tick = state.Tick,
            AffinityBps = affinityBps,
            InvestigationAvailable = investigationAvailable,
            FragmentId = fragmentId
        };

        state.PlanetScanResults[scanId] = result;
        planet.ScanResults ??= new List<string>();
        planet.ScanResults.Add(scanId);

        return result;
    }

    // ── Public API: Execute atmospheric sample (Gaseous only) ──
    public static PlanetScanResult? ExecuteAtmosphericSample(SimState state, string nodeId, ScanMode mode)
    {
        if (state is null || string.IsNullOrEmpty(nodeId)) return null;
        if (!state.Planets.TryGetValue(nodeId, out var planet)) return null;

        // Gaseous only.
        if (planet.Type != PlanetType.Gaseous) return null;

        // Check mode availability.
        if (!IsModeAvailable(state.ScannerTier, mode)) return null;

        // Check charge budget.
        int maxCharges = PlanetScanTweaksV0.GetMaxCharges(state.ScannerTier);
        if (state.ScannerChargesUsed >= maxCharges) return null;

        // Check fuel cost.
        string fuelId = WellKnownGoodIds.Fuel;
        int fuelInCargo = state.PlayerCargo.TryGetValue(fuelId, out var fuelQty) ? fuelQty : 0;
        if (fuelInCargo < PlanetScanTweaksV0.AtmosphericSampleFuelCost) return null;

        // Consume charge + fuel.
        state.ScannerChargesUsed++;
        state.PlayerCargo[fuelId] = fuelInCargo - PlanetScanTweaksV0.AtmosphericSampleFuelCost;

        int affinityBps = PlanetScanTweaksV0.GetAffinityBps(mode, planet.Type);

        // Atmospheric sample can produce all 5 categories (like landing scan for gas giants).
        var category = SelectFindingCategory(state, mode, planet.Type, affinityBps, ScanPhase.AtmosphericSample, nodeId);

        string flavorText = GenerateScanFlavorText(state, planet.Type, category, nodeId);
        string discoveryId = "";
        string fragmentId = "";

        switch (category)
        {
            case FindingCategory.ResourceIntel:
                discoveryId = GenerateResourceIntel(state, nodeId, planet);
                break;
            case FindingCategory.SignalLead:
                discoveryId = GenerateSignalLead(state, nodeId, planet, mode);
                break;
            case FindingCategory.PhysicalEvidence:
                discoveryId = GeneratePhysicalEvidence(state, nodeId, planet, DiscoveryPhase.Scanned);
                break;
            case FindingCategory.FragmentCache:
                fragmentId = TryDropFragment(state, planet);
                break;
            case FindingCategory.DataArchive:
                GenerateDataArchive(state, nodeId, planet);
                break;
        }

        var scanId = $"SCAN_{state.NextPlanetScanSeq}";
        state.NextPlanetScanSeq++;

        var result = new PlanetScanResult
        {
            ScanId = scanId,
            NodeId = nodeId,
            Mode = mode,
            Phase = ScanPhase.AtmosphericSample,
            Category = category,
            DiscoveryId = discoveryId,
            FlavorText = flavorText,
            Tick = state.Tick,
            AffinityBps = affinityBps,
            FragmentId = fragmentId
        };

        state.PlanetScanResults[scanId] = result;
        planet.ScanResults ??= new List<string>();
        planet.ScanResults.Add(scanId);

        return result;
    }

    // ── Public API: Investigate a Physical Evidence finding ──
    public static bool InvestigateFinding(SimState state, string scanId)
    {
        if (state is null || string.IsNullOrEmpty(scanId)) return false;
        if (!state.PlanetScanResults.TryGetValue(scanId, out var result)) return false;
        if (!result.InvestigationAvailable || result.Investigated) return false;

        result.Investigated = true;

        // Generate bonus KnowledgeGraph connections.
        string nodeId = result.NodeId;
        for (int i = 0; i < PlanetScanTweaksV0.InvestigationBonusKgConnections; i++)
        {
            string connId = $"KC.PLANET_SCAN.INV.{scanId}.{i}";
            state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
            {
                ConnectionId = connId,
                SourceDiscoveryId = result.DiscoveryId,
                TargetDiscoveryId = "",
                ConnectionType = KnowledgeConnectionType.LoreFragment,
                Description = $"Investigation at {nodeId} revealed additional data.",
                IsRevealed = true
            });
        }

        // Investigation may also reveal a Signal Lead.
        ulong investigationHash = DeterministicHash(state, $"INV_LEAD|{scanId}|{state.Tick}");
        if (investigationHash % 3 == 0) // ~33% chance  // STRUCTURAL: modulo for probability
        {
            string leadKey = $"PLANET_SCAN_INV_LEAD|{scanId}";
            if (!state.Intel.RumorLeads.ContainsKey(leadKey))
            {
                state.Intel.RumorLeads[leadKey] = new RumorLead
                {
                    LeadId = leadKey,
                    Status = RumorLeadStatus.Active,
                    SourceVerbToken = "INVESTIGATION",
                    Hint = new HintPayloadV0
                    {
                        ImpliedPayoffToken = "RUIN",
                        CoarseLocationToken = nodeId
                    }
                };
            }
        }

        return true;
    }

    // ── Public API: Get remaining scanner charges ──
    public static int GetRemainingCharges(SimState state)
    {
        if (state is null) return 0; // STRUCTURAL: null check
        int max = PlanetScanTweaksV0.GetMaxCharges(state.ScannerTier);
        return Math.Max(0, max - state.ScannerChargesUsed); // STRUCTURAL: clamp
    }

    // ── Public API: Reset charges (called on travel to new system) ──
    public static void ResetChargesOnTravel(SimState state)
    {
        if (state is null) return;
        state.ScannerChargesUsed = 0;
        state.ScannerLastRechargeTick = state.Tick;
    }

    // ── Mode availability ──
    public static bool IsModeAvailable(int scannerTier, ScanMode mode)
    {
        return mode switch
        {
            ScanMode.MineralSurvey => true,  // Always available
            ScanMode.SignalSweep => scannerTier >= 1,   // STRUCTURAL: Mk1+
            ScanMode.Archaeological => scannerTier >= 2, // STRUCTURAL: Mk2+
            _ => false
        };
    }

    // ── Finding category selection (deterministic) ──
    private static FindingCategory SelectFindingCategory(
        SimState state, ScanMode mode, PlanetType planetType,
        int affinityBps, ScanPhase phase, string nodeId)
    {
        // Hash for deterministic selection.
        ulong hash = DeterministicHash(state, $"CATEGORY|{nodeId}|{(int)mode}|{(int)phase}|{state.Tick}");

        // Get weights for this mode at this affinity level.
        int wResource, wSignal, wEvidence, wFragment, wArchive;
        GetCategoryWeights(mode, affinityBps, phase, out wResource, out wSignal, out wEvidence, out wFragment, out wArchive);

        int total = wResource + wSignal + wEvidence + wFragment + wArchive;
        if (total <= 0) return FindingCategory.ResourceIntel; // STRUCTURAL: fallback

        int roll = (int)(hash % (ulong)total);
        if (roll < wResource) return FindingCategory.ResourceIntel;
        roll -= wResource;
        if (roll < wSignal) return FindingCategory.SignalLead;
        roll -= wSignal;
        if (roll < wEvidence) return FindingCategory.PhysicalEvidence;
        roll -= wEvidence;
        if (roll < wFragment) return FindingCategory.FragmentCache;
        return FindingCategory.DataArchive;
    }

    private static void GetCategoryWeights(
        ScanMode mode, int affinityBps, ScanPhase phase,
        out int wResource, out int wSignal, out int wEvidence, out int wFragment, out int wArchive)
    {
        // Base weights from tweaks by mode (using high-affinity weights, scaled by actual affinity).
        switch (mode)
        {
            case ScanMode.MineralSurvey:
                wResource = PlanetScanTweaksV0.MineralHighResourceIntel;
                wSignal = PlanetScanTweaksV0.MineralHighSignalLead;
                wEvidence = PlanetScanTweaksV0.MineralHighPhysicalEvidence;
                wFragment = PlanetScanTweaksV0.MineralHighFragmentCache;
                wArchive = PlanetScanTweaksV0.MineralHighDataArchive;
                break;
            case ScanMode.SignalSweep:
                wResource = PlanetScanTweaksV0.SignalHighResourceIntel;
                wSignal = PlanetScanTweaksV0.SignalHighSignalLead;
                wEvidence = PlanetScanTweaksV0.SignalHighPhysicalEvidence;
                wFragment = PlanetScanTweaksV0.SignalHighFragmentCache;
                wArchive = PlanetScanTweaksV0.SignalHighDataArchive;
                break;
            case ScanMode.Archaeological:
                wResource = PlanetScanTweaksV0.ArchHighResourceIntel;
                wSignal = PlanetScanTweaksV0.ArchHighSignalLead;
                wEvidence = PlanetScanTweaksV0.ArchHighPhysicalEvidence;
                wFragment = PlanetScanTweaksV0.ArchHighFragmentCache;
                wArchive = PlanetScanTweaksV0.ArchHighDataArchive;
                break;
            default:
                wResource = PlanetScanTweaksV0.DefaultResourceIntel;
                wSignal = PlanetScanTweaksV0.DefaultSignalLead;
                wEvidence = PlanetScanTweaksV0.DefaultPhysicalEvidence;
                wFragment = PlanetScanTweaksV0.DefaultFragmentCache;
                wArchive = PlanetScanTweaksV0.DefaultDataArchive;
                break;
        }

        // Scale non-primary weights by affinity (low affinity = more secondary categories).
        if (affinityBps < PlanetScanTweaksV0.MidAffinityThresholdBps)
        {
            // Low affinity: boost secondary categories, reduce primary.
            int primaryBoost = wResource > wSignal && wResource > wEvidence ? wResource : (wSignal > wEvidence ? wSignal : wEvidence);
            // Transfer 30% from primary to secondaries evenly.
            int transfer = primaryBoost * PlanetScanTweaksV0.LowAffinityTransferBps / 10000;
            if (wResource == primaryBoost) { wResource -= transfer; wSignal += transfer / 2; wEvidence += transfer / 2; }
            else if (wSignal == primaryBoost) { wSignal -= transfer; wResource += transfer / 2; wEvidence += transfer / 2; }
            else { wEvidence -= transfer; wResource += transfer / 2; wSignal += transfer / 2; }
        }

        // Orbital scans: no FragmentCache or DataArchive.
        if (phase == ScanPhase.Orbital)
        {
            // Redistribute to top 3 categories.
            int extra = wFragment + wArchive;
            wFragment = 0;
            wArchive = 0;
            wResource += extra / 3;
            wSignal += extra / 3;
            wEvidence += extra - (extra / 3) * 2; // STRUCTURAL: remainder goes to evidence
        }
    }

    // ── Resource Intel generation ──
    private static string GenerateResourceIntel(SimState state, string nodeId, Planet planet)
    {
        // Create a TradeRouteIntel via the existing discovery pipeline.
        // Find adjacent nodes with markets and identify profitable routes.
        if (!state.Markets.TryGetValue(nodeId, out var localMarket)) return "";

        var adjacentNodes = GetAdjacentNodes(state, nodeId);
        string bestDest = "";
        string bestGood = "";
        int bestProfit = 0;

        foreach (var adjNode in adjacentNodes)
        {
            if (!state.Markets.TryGetValue(adjNode, out var adjMarket)) continue;

            foreach (var goodEntry in localMarket.Inventory)
            {
                string goodId = goodEntry.Key;
                int localPrice = localMarket.GetPrice(goodId);
                int adjPrice = adjMarket.GetPrice(goodId);
                if (localPrice <= 0 || adjPrice <= 0) continue;

                int profit = adjPrice - localPrice;
                if (profit > bestProfit)
                {
                    bestProfit = profit;
                    bestDest = adjNode;
                    bestGood = goodId;
                }
            }
        }

        if (bestProfit < DiscoveryIntelTweaksV0.DiscoveryRouteMinProfit || string.IsNullOrEmpty(bestDest)) return "";

        string discoveryId = $"disc_v0|RESOURCE_POOL_MARKER|{nodeId}|{bestGood}|PLANET_SCAN_{state.NextPlanetScanSeq}";
        string routeId = IntelBook.RouteKey(nodeId, bestDest, bestGood);

        if (!state.Intel.TradeRoutes.ContainsKey(routeId))
        {
            state.Intel.TradeRoutes[routeId] = new TradeRouteIntel
            {
                RouteId = routeId,
                SourceNodeId = nodeId,
                DestNodeId = bestDest,
                GoodId = bestGood,
                EstimatedProfitPerUnit = bestProfit,
                DiscoveredTick = state.Tick,
                LastValidatedTick = state.Tick,
                Status = TradeRouteStatus.Discovered,
                SourceDiscoveryId = discoveryId
            };
        }

        return discoveryId;
    }

    // ── Signal Lead generation ──
    private static string GenerateSignalLead(SimState state, string nodeId, Planet planet, ScanMode mode)
    {
        // Create a SIGNAL discovery at Seen phase pointing to an adjacent region.
        string discoveryId = $"disc_v0|SIGNAL|{nodeId}|PLANET_SCAN_SIG_{state.NextPlanetScanSeq}|{(int)mode}";

        // Find a target node for the lead (within 2 hops, deterministic hash selection).
        var adjacentNodes = GetAdjacentNodes(state, nodeId);
        if (adjacentNodes.Count == 0) return "";

        ulong hash = DeterministicHash(state, $"SIGNAL_TARGET|{nodeId}|{state.Tick}|{(int)mode}");
        string targetNode = adjacentNodes[(int)(hash % (ulong)adjacentNodes.Count)];

        // Add to Intel discoveries as a Seen-phase signal.
        if (!state.Intel.Discoveries.ContainsKey(discoveryId))
        {
            state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
            {
                DiscoveryId = discoveryId,
                Phase = DiscoveryPhase.Seen
            };

            // Seed the discovery on the target node.
            if (state.Nodes.TryGetValue(targetNode, out var targetNodeObj))
            {
                targetNodeObj.SeededDiscoveryIds ??= new List<string>();
                if (!targetNodeObj.SeededDiscoveryIds.Contains(discoveryId))
                    targetNodeObj.SeededDiscoveryIds.Add(discoveryId);
            }
        }

        // Create RumorLead pointing to the target.
        string leadKey = $"PLANET_SCAN_SIGNAL|{discoveryId}";
        if (!state.Intel.RumorLeads.ContainsKey(leadKey))
        {
            state.Intel.RumorLeads[leadKey] = new RumorLead
            {
                LeadId = leadKey,
                Status = RumorLeadStatus.Active,
                SourceVerbToken = "PLANET_SCAN",
                Hint = new HintPayloadV0
                {
                    ImpliedPayoffToken = "SIGNAL",
                    CoarseLocationToken = targetNode
                }
            };
        }

        return discoveryId;
    }

    // ── Physical Evidence generation ──
    private static string GeneratePhysicalEvidence(SimState state, string nodeId, Planet planet, DiscoveryPhase phase)
    {
        string kind = planet.Type switch
        {
            PlanetType.Ice => "DERELICT",
            PlanetType.Lava => "RUIN",
            PlanetType.Barren => "RUIN",
            _ => "RUIN"
        };

        string discoveryId = $"disc_v0|{kind}|{nodeId}|PLANET_SCAN_EV_{state.NextPlanetScanSeq}|evidence";

        if (!state.Intel.Discoveries.ContainsKey(discoveryId))
        {
            state.Intel.Discoveries[discoveryId] = new DiscoveryStateV0
            {
                DiscoveryId = discoveryId,
                Phase = phase
            };

            // Seed the discovery on this node.
            if (state.Nodes.TryGetValue(nodeId, out var nodeObj))
            {
                nodeObj.SeededDiscoveryIds ??= new List<string>();
                if (!nodeObj.SeededDiscoveryIds.Contains(discoveryId))
                    nodeObj.SeededDiscoveryIds.Add(discoveryId);
            }

            // Create KnowledgeConnection.
            string connId = $"KC.PLANET_SCAN.EV.{discoveryId}";
            state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
            {
                ConnectionId = connId,
                SourceDiscoveryId = discoveryId,
                TargetDiscoveryId = "",
                ConnectionType = KnowledgeConnectionType.LoreFragment,
                Description = GetEvidenceDescription(planet.Type),
                IsRevealed = true
            });
        }

        return discoveryId;
    }

    // ── Fragment Cache drop ──
    private static string TryDropFragment(SimState state, Planet planet)
    {
        // Find uncollected fragments matching planet type affinity.
        var candidates = new List<string>();
        AdaptationFragmentKind favoredKind = planet.Type switch
        {
            PlanetType.Ice => AdaptationFragmentKind.Biological,
            PlanetType.Sand => AdaptationFragmentKind.Structural,
            PlanetType.Lava => AdaptationFragmentKind.Energetic,
            PlanetType.Gaseous => AdaptationFragmentKind.Cognitive,
            _ => AdaptationFragmentKind.Biological // Barren/Terrestrial: no bias, handled below
        };

        // Scan all adaptation fragments for uncollected ones.
        foreach (var kvp in state.AdaptationFragments)
        {
            var frag = kvp.Value;
            if (frag is null || frag.IsCollected) continue;

            if (planet.Type == PlanetType.Barren || planet.Type == PlanetType.Terrestrial)
            {
                // Barren: any kind. Terrestrial: rare but any.
                candidates.Add(frag.FragmentId);
            }
            else if (frag.Kind == favoredKind)
            {
                candidates.Add(frag.FragmentId);
            }
        }

        if (candidates.Count == 0)
        {
            // No matching uncollected fragments — fall back to any uncollected.
            foreach (var kvp in state.AdaptationFragments)
            {
                var frag = kvp.Value;
                if (frag is null || frag.IsCollected) continue;
                candidates.Add(frag.FragmentId);
            }
        }

        if (candidates.Count == 0) return ""; // All fragments collected

        candidates.Sort(StringComparer.Ordinal);

        // Deterministic selection.
        ulong hash = DeterministicHash(state, $"FRAGMENT|{planet.NodeId}|{state.Tick}");
        int idx = (int)(hash % (ulong)candidates.Count);
        string selectedId = candidates[idx];

        // Collect the fragment.
        if (state.AdaptationFragments.TryGetValue(selectedId, out var selectedFrag))
        {
            selectedFrag.CollectedTick = state.Tick;
            selectedFrag.NodeId = planet.NodeId;
        }

        return selectedId;
    }

    // ── Data Archive generation ──
    private static void GenerateDataArchive(SimState state, string nodeId, Planet planet)
    {
        // Create a DataLog entry in the Knowledge Graph.
        string logId = $"LOG.PLANET_SCAN.{nodeId}.{state.Tick}";
        if (state.DataLogs.ContainsKey(logId)) return;

        var thread = GetDataLogThreadForPlanetType(planet.Type);
        string archiveText = GetArchiveFlavorText(planet.Type);

        var log = new DataLog
        {
            LogId = logId,
            Thread = thread,
            RevelationTier = 1, // STRUCTURAL: base tier
            MechanicalHook = "TRADE_INTEL"
        };
        log.Speakers.Add("Unknown");
        log.Entries.Add(new DataLogEntry
        {
            EntryIndex = 0, // STRUCTURAL: first entry
            Speaker = "Unknown",
            Text = archiveText
        });

        state.DataLogs[logId] = log;

        // Create KnowledgeConnection.
        string connId = $"KC.PLANET_SCAN.ARCH.{logId}";
        state.Intel.KnowledgeConnections.Add(new KnowledgeConnection
        {
            ConnectionId = connId,
            SourceDiscoveryId = logId,
            TargetDiscoveryId = "",
            ConnectionType = KnowledgeConnectionType.LoreFragment,
            Description = $"Data archive recovered from {planet.Type} world.",
            IsRevealed = true
        });
    }

    // ── Helpers ──

    private static string FindNodeForDiscovery(SimState state, string discoveryId)
    {
        foreach (var nodeKvp in state.Nodes)
        {
            var node = nodeKvp.Value;
            if (node?.SeededDiscoveryIds is null) continue;
            if (node.SeededDiscoveryIds.Contains(discoveryId))
                return node.Id ?? "";
        }
        return "";
    }

    private static List<string> GetAdjacentNodes(SimState state, string nodeId)
    {
        var result = new List<string>();
        foreach (var edge in state.Edges.Values)
        {
            if (edge is null) continue;
            if (string.Equals(edge.FromNodeId, nodeId, StringComparison.Ordinal) && !result.Contains(edge.ToNodeId))
                result.Add(edge.ToNodeId);
            else if (string.Equals(edge.ToNodeId, nodeId, StringComparison.Ordinal) && !result.Contains(edge.FromNodeId))
                result.Add(edge.FromNodeId);
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static string GenerateScanFlavorText(SimState state, PlanetType planetType, FindingCategory category, string nodeId)
    {
        var flavors = PlanetScanContentV0.GetFlavors(planetType, category);
        if (flavors.Count == 0) return $"{category} finding detected.";

        ulong hash = DeterministicHash(state, $"FLAVOR|{nodeId}|{(int)category}|{state.Tick}");
        int idx = (int)(hash % (ulong)flavors.Count);
        string template = flavors[idx].Text;

        // Simple placeholder replacement.
        string nodeName = "";
        if (state.Nodes.TryGetValue(nodeId, out var node) && !string.IsNullOrEmpty(node.Name))
            nodeName = node.Name;
        else
            nodeName = nodeId;

        return template.Replace("{node}", nodeName, StringComparison.Ordinal)
                       .Replace("{good}", "resources", StringComparison.Ordinal);
    }

    private static string GenerateHintText(PlanetType planetType, ScanMode currentMode)
    {
        var hints = PlanetScanContentV0.GetHints(planetType, currentMode);
        if (hints.Count == 0) return "";
        return hints[0].HintText; // STRUCTURAL: first hint
    }

    private static ulong DeterministicHash(SimState state, string input)
    {
        // FNV-1a hash combining state tick, seed, and input.
        ulong hash = RiskModelV0.FnvOffset;
        hash = FnvMix(hash, (ulong)state.InitialSeed);
        hash = FnvMix(hash, (ulong)state.Tick);
        foreach (char c in input)
            hash = FnvMix(hash, (ulong)c);
        return hash;
    }

    private static ulong FnvMix(ulong hash, ulong val)
    {
        hash ^= val;
        hash *= RiskModelV0.FnvPrime;
        return hash;
    }

    private static string GetEvidenceDescription(PlanetType type)
    {
        return type switch
        {
            PlanetType.Terrestrial => "Faction archive fragment recovered.",
            PlanetType.Ice => "Thread Lattice fossil preserved in ice cores.",
            PlanetType.Sand => "Ancient excavation site exposed by wind erosion.",
            PlanetType.Lava => "Thread Emergence Point — energy pushing through rock.",
            PlanetType.Gaseous => "Resonance Pocket in atmospheric equilibrium.",
            PlanetType.Barren => "Intact installation on airless surface.",
            _ => "Physical evidence recovered."
        };
    }

    private static DataLogThread GetDataLogThreadForPlanetType(PlanetType type)
    {
        return type switch
        {
            PlanetType.Terrestrial => DataLogThread.EconTopology,
            PlanetType.Ice => DataLogThread.Containment,
            PlanetType.Sand => DataLogThread.EconTopology,
            PlanetType.Lava => DataLogThread.Accommodation,
            PlanetType.Gaseous => DataLogThread.Warning,
            PlanetType.Barren => DataLogThread.Departure,
            _ => DataLogThread.Containment
        };
    }

    private static string GetArchiveFlavorText(PlanetType type)
    {
        return type switch
        {
            PlanetType.Terrestrial => "Economic topology notes — someone mapped every dependency arrow.",
            PlanetType.Ice => "A worried voice: 'Should we cage what we cannot understand?'",
            PlanetType.Sand => "Survey notes carved into alloy. Counting resource nodes, drawing dependency arrows.",
            PlanetType.Lava => "Accommodation calculations: 'If the geometry holds, we can shape it.'",
            PlanetType.Gaseous => "Warning decoded from atmospheric noise. Urgent, personal, very old.",
            PlanetType.Barren => "Departure records. Dates, coordinates, one recurring word: 'Timeline.'",
            _ => "Data archive recovered."
        };
    }
}
