using SimCore.Entities;

namespace SimCore.Systems;

public static class IntelSystem
{
    // Slice 1 rule: local market id is the same as PlayerLocationNodeId
    private static string GetLocalMarketId(SimState state) => state.PlayerLocationNodeId ?? "";

    public static void Process(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        // Ensure intel book exists on state (see Edit 4 below)
        if (state.Intel is null) state.Intel = new IntelBook();

        var localMarketId = GetLocalMarketId(state);
        if (string.IsNullOrWhiteSpace(localMarketId)) return;
        if (!state.Markets.TryGetValue(localMarketId, out var localMarket)) return;

        // Refresh intel ONLY for the local market
        // Deterministic ordering: iterate goods in ordinal order
        var goodIds = localMarket.Inventory.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
        foreach (var goodId in goodIds)
        {
            var qty = localMarket.Inventory.TryGetValue(goodId, out var v) ? v : 0;
            var key = IntelBook.Key(localMarketId, goodId);

            if (!state.Intel.Observations.TryGetValue(key, out var obs))
            {
                obs = new IntelObservation();
                state.Intel.Observations[key] = obs;
            }

            obs.ObservedTick = state.Tick;
            obs.ObservedInventoryQty = qty;
        }
    }

    public static MarketGoodView GetMarketGoodView(SimState state, string targetMarketId, string goodId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(targetMarketId)) throw new ArgumentException("targetMarketId required", nameof(targetMarketId));
        if (string.IsNullOrWhiteSpace(goodId)) throw new ArgumentException("goodId required", nameof(goodId));

        var localMarketId = GetLocalMarketId(state);

        // Local truth
        if (string.Equals(localMarketId, targetMarketId, StringComparison.Ordinal))
        {
            if (!state.Markets.TryGetValue(targetMarketId, out var m))
            {
                return new MarketGoodView { Kind = MarketGoodViewKind.LocalTruth, ExactInventoryQty = 0, AgeTicks = 0, InventoryBand = InventoryBand.Unknown };
            }

            var qty = m.Inventory.TryGetValue(goodId, out var v) ? v : 0;

            return new MarketGoodView
            {
                Kind = MarketGoodViewKind.LocalTruth,
                ExactInventoryQty = qty,
                InventoryBand = InventoryBand.Unknown,
                AgeTicks = 0
            };
        }

        // Remote intel
        if (state.Intel is null) state.Intel = new IntelBook();

        var key = IntelBook.Key(targetMarketId, goodId);
        if (!state.Intel.Observations.TryGetValue(key, out var obs))
        {
            return new MarketGoodView
            {
                Kind = MarketGoodViewKind.RemoteIntel,
                ExactInventoryQty = 0,
                InventoryBand = InventoryBand.Unknown,
                AgeTicks = -1
            };
        }

        var age = state.Tick - obs.ObservedTick;
        if (age < 0) age = 0;

        return new MarketGoodView
        {
            Kind = MarketGoodViewKind.RemoteIntel,
            ExactInventoryQty = 0,
            InventoryBand = BandInventory(obs.ObservedInventoryQty),
            AgeTicks = age
        };
    }

    public static InventoryBand BandInventory(int qty)
    {
        // Deterministic, fixed thresholds. Adjust if you prefer, but then lock tests accordingly.
        if (qty <= 0) return InventoryBand.VeryLow;
        if (qty <= 10) return InventoryBand.Low;
        if (qty <= 50) return InventoryBand.Medium;
        if (qty <= 200) return InventoryBand.High;
        return InventoryBand.VeryHigh;
    }

    // GATE.S3_6.DISCOVERY_STATE.001
    // Stable listing of discoveries: DiscoveryId asc (ordinal).
    public static IReadOnlyList<DiscoveryStateV0> GetDiscoveriesAscending(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Intel?.Discoveries is null)
            return Array.Empty<DiscoveryStateV0>();

        return state.Intel.Discoveries.Values
            .OrderBy(d => d.DiscoveryId, StringComparer.Ordinal)
            .ToList();
    }

    // GATE.S3_6.DISCOVERY_STATE.001
    // Returns the reason code for a scan attempt on the given discovery.
    public static DiscoveryReasonCode GetScanReasonCode(SimState state, string discoveryId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrEmpty(discoveryId)) return DiscoveryReasonCode.NotSeen;
        if (state.Intel?.Discoveries is null || !state.Intel.Discoveries.TryGetValue(discoveryId, out var d) || d is null)
            return DiscoveryReasonCode.NotSeen;

        // Scan is only valid from Seen. Any other phase is rejected deterministically.
        if (d.Phase == DiscoveryPhase.Analyzed)
            return DiscoveryReasonCode.AlreadyAnalyzed;
        if (d.Phase != DiscoveryPhase.Seen)
            return DiscoveryReasonCode.NotSeen;

        return DiscoveryReasonCode.Ok;
    }

    // GATE.S3_6.DISCOVERY_STATE.003
    // Applies a scan attempt: Seen -> Scanned if allowed; otherwise no-op with deterministic reason code.
    public static DiscoveryReasonCode ApplyScan(SimState state, string fleetId, string discoveryId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        _ = fleetId;

        var rc = GetScanReasonCode(state, discoveryId);
        if (rc != DiscoveryReasonCode.Ok) return rc;

        // Guaranteed present due to GetScanReasonCode pre-checks.
        var d = state.Intel!.Discoveries[discoveryId];
        d.Phase = DiscoveryPhase.Scanned;
        state.Intel.Discoveries[discoveryId] = d;

        return DiscoveryReasonCode.Ok;
    }

    // GATE.S3_6.DISCOVERY_STATE.001
    // Returns the reason code for an analyze attempt on the given discovery.
    public static DiscoveryReasonCode GetAnalyzeReasonCode(SimState state, string discoveryId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrEmpty(discoveryId)) return DiscoveryReasonCode.NotSeen;
        if (state.Intel?.Discoveries is null || !state.Intel.Discoveries.TryGetValue(discoveryId, out var d))
            return DiscoveryReasonCode.NotSeen;
        if (d.Phase == DiscoveryPhase.Analyzed)
            return DiscoveryReasonCode.AlreadyAnalyzed;

        // Analyze is only valid from Scanned. Any other phase is rejected deterministically.
        if (d.Phase != DiscoveryPhase.Scanned)
            return DiscoveryReasonCode.NotScanned;

        return DiscoveryReasonCode.Ok;
    }

    // GATE.S3_6.DISCOVERY_STATE.004
    // Applies an analyze attempt: Scanned -> Analyzed if allowed and fleet is at hub; otherwise no-op with deterministic reason code.
    // Hub rule: fleet.CurrentNodeId must equal state.PlayerLocationNodeId (ordinal compare).
    // Emits deterministic analysis_outcome event payload stub for all attempts (success or rejection).
    public static DiscoveryReasonCode ApplyAnalyze(SimState state, string fleetId, string discoveryId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (fleetId is null) fleetId = "";

        var rc = GetAnalyzeReasonCode(state, discoveryId);
        if (rc != DiscoveryReasonCode.Ok)
        {
            EmitAnalyzeOutcome(state, fleetId, discoveryId, rc, phaseAfter: GetPhaseAfter_NoMutation(state, discoveryId));
            return rc;
        }

        // Guaranteed present due to GetAnalyzeReasonCode pre-checks.
        var d = state.Intel!.Discoveries[discoveryId];

        if (!IsFleetAtHub(state, fleetId, out var nodeId))
        {
            rc = DiscoveryReasonCode.OffHub;
            EmitAnalyzeOutcome(state, fleetId, discoveryId, rc, phaseAfter: (int)d.Phase, nodeId: nodeId);
            return rc;
        }

        d.Phase = DiscoveryPhase.Analyzed;
        state.Intel.Discoveries[discoveryId] = d;

        EmitAnalyzeOutcome(state, fleetId, discoveryId, DiscoveryReasonCode.Ok, phaseAfter: (int)DiscoveryPhase.Analyzed, nodeId: nodeId);
        return DiscoveryReasonCode.Ok;
    }

    private static bool IsFleetAtHub(SimState state, string fleetId, out string nodeId)
    {
        nodeId = "";

        var hubNodeId = state.PlayerLocationNodeId ?? "";
        if (hubNodeId.Length == default) return false;
        if (fleetId.Length == default) return false;

        if (!state.Fleets.TryGetValue(fleetId, out var f) || f is null) return false;

        nodeId = f.CurrentNodeId ?? "";
        if (nodeId.Length == default) return false;

        return string.Equals(nodeId, hubNodeId, StringComparison.Ordinal);
    }

    private static int GetPhaseAfter_NoMutation(SimState state, string discoveryId)
    {
        if (state.Intel?.Discoveries is null) return (int)DiscoveryPhase.Seen;
        if (string.IsNullOrEmpty(discoveryId)) return (int)DiscoveryPhase.Seen;
        if (!state.Intel.Discoveries.TryGetValue(discoveryId, out var d) || d is null) return (int)DiscoveryPhase.Seen;
        return (int)d.Phase;
    }

    private static void EmitAnalyzeOutcome(SimState state, string fleetId, string discoveryId, DiscoveryReasonCode reasonCode, int phaseAfter, string nodeId = "")
    {
        if (state is null) return;

        state.EmitFleetEvent(new SimCore.Events.FleetEvents.Event
        {
            Type = SimCore.Events.FleetEvents.FleetEventType.DiscoveryAnalysisOutcome,
            FleetId = fleetId ?? "",
            DiscoveryId = discoveryId ?? "",
            NodeId = nodeId ?? "",
            ReasonCode = (int)reasonCode,
            PhaseAfter = phaseAfter
        });
    }

    // Gate: DiscoveryState Seen on node entry
    // Entering a node with seeded discovery markers marks discoveries Seen idempotently
    // and emits deterministic DiscoverySeen transition events.
    // Deterministic ordering: DiscoveryId asc (StringComparer.Ordinal); ties: none.
    public static void ApplySeenFromNodeEntry(SimState state, string fleetId, string nodeId, IReadOnlyList<string> seededDiscoveryIds)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (seededDiscoveryIds is null || seededDiscoveryIds.Count == default) return;

        if (state.Intel is null) state.Intel = new IntelBook();

        // IntelBook.Discoveries setter may be private; mutate the existing dictionary.
        var discoveries = state.Intel.Discoveries;
        if (discoveries is null)
            throw new InvalidOperationException("IntelBook.Discoveries must be initialized");

        // Collect ids, sort deterministically, then skip duplicates by adjacency.
        var ids = new List<string>(seededDiscoveryIds.Count);
        for (int i = default; i < seededDiscoveryIds.Count; i++)
        {
            var id = seededDiscoveryIds[i] ?? "";
            if (id.Length == default) continue;
            ids.Add(id);
        }
        if (ids.Count == default) return;

        ids.Sort(StringComparer.Ordinal);

        string prev = "";
        for (int i = default; i < ids.Count; i++)
        {
            var discoveryId = ids[i];

            // Skip duplicates deterministically.
            if (i != default && string.Equals(discoveryId, prev, StringComparison.Ordinal))
                continue;
            prev = discoveryId;

            // Idempotent: if already present, it is Seen+; do not emit again.
            if (state.Intel.Discoveries.TryGetValue(discoveryId, out var existing) && existing is not null)
                continue;

            var st = new DiscoveryStateV0
            {
                DiscoveryId = discoveryId,
                Phase = DiscoveryPhase.Seen
            };
            state.Intel.Discoveries[discoveryId] = st;

            // Deterministic transition event (schema-bound).
            // Avoid new numeric literals: omit explicitly-zero fields and rely on Event defaults.
            state.EmitFleetEvent(new SimCore.Events.FleetEvents.Event
            {
                Type = SimCore.Events.FleetEvents.FleetEventType.DiscoverySeen,
                FleetId = fleetId ?? "",
                DiscoveryId = discoveryId,
                NodeId = nodeId ?? ""
            });
        }
    }
}
