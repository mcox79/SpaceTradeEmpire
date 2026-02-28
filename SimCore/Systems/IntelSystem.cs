using System.Runtime.CompilerServices;
using SimCore.Entities;
using SimCore.Intents;
using SimCore.Programs;

namespace SimCore.Systems;

public static class IntelSystem
{
    private sealed class Scratch
    {
        public readonly List<string> GoodIds = new();
    }

    private static readonly ConditionalWeakTable<SimState, Scratch> s_scratch = new();
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
        var scratch = s_scratch.GetOrCreateValue(state);
        var goodIds = scratch.GoodIds;
        goodIds.Clear();
        foreach (var key in localMarket.Inventory.Keys) goodIds.Add(key);
        goodIds.Sort(StringComparer.Ordinal);
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

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001
    // Stable listing of unlocks: UnlockId asc (StringComparer.Ordinal).
    public static IReadOnlyList<UnlockContractV0> GetUnlocksAscending(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Intel?.Unlocks is null)
            return Array.Empty<UnlockContractV0>();

        return state.Intel.Unlocks.Values
            .OrderBy(u => u.UnlockId, StringComparer.Ordinal)
            .ToList();
    }

    // GATE.S3_6.RUMOR_INTEL_MIN.001
    // Stable listing of rumor leads: LeadId asc (StringComparer.Ordinal).
    public static IReadOnlyList<RumorLead> GetRumorLeadsAscending(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Intel?.RumorLeads is null)
            return Array.Empty<RumorLead>();

        return state.Intel.RumorLeads.Values
            .OrderBy(r => r.LeadId, StringComparer.Ordinal)
            .ToList();
    }

    // GATE.S3_6.EXPEDITION_PROGRAMS.001
    // Expedition intent apply v0.
    // Rejection precedence (deterministic):
    // 1) unknown LeadId -> SiteNotFound
    // 2) missing any acquired SiteBlueprint unlock -> MissingSiteBlueprintUnlock
    // 3) missing fleet record -> InsufficientExpeditionCapacity
    // Success: writes accepted lead%kind and clears reject reason.
    public static void ApplyExpedition(SimState state, string fleetId, string leadId, ExpeditionKind kind, int applyTick)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        _ = applyTick; // v0: captured on the intent, scheduling consumed by later gates.

        fleetId ??= "";
        leadId ??= "";

        // Ensure intel exists (failure-safe).
        state.Intel ??= new IntelBook();

        // Deterministic: unknown lead is always SiteNotFound regardless of other missing prerequisites.
        // Contract: LeadId resolves against IntelBook.Discoveries (see PROG_EXEC_004).
        if (leadId.Length == default || state.Intel.Discoveries is null || !state.Intel.Discoveries.ContainsKey(leadId))
        {
            state.LastExpeditionRejectReason = ProgramExplain.ReasonCodes.SiteNotFound;
            state.LastExpeditionAcceptedLeadId = null;
            state.LastExpeditionAcceptedKind = null;
            return;
        }

        // Capacity gate (v0): fleet record must exist.
        if (fleetId.Length == default || !state.Fleets.TryGetValue(fleetId, out var _))
        {
            state.LastExpeditionRejectReason = ProgramExplain.ReasonCodes.InsufficientExpeditionCapacity;
            state.LastExpeditionAcceptedLeadId = null;
            state.LastExpeditionAcceptedKind = null;
            return;
        }

        // v0 rule: Survey requires only a known discovery lead + fleet capacity.
        // Other kinds require an acquired SiteBlueprint unlock (deterministic scan).
        if (kind != ExpeditionKind.Survey && !HasAnyAcquiredSiteBlueprintUnlock(state))
        {
            state.LastExpeditionRejectReason = ProgramExplain.ReasonCodes.MissingSiteBlueprintUnlock;
            state.LastExpeditionAcceptedLeadId = null;
            state.LastExpeditionAcceptedKind = null;
            return;
        }

        state.LastExpeditionRejectReason = null;
        state.LastExpeditionAcceptedLeadId = leadId;
        state.LastExpeditionAcceptedKind = kind.ToString();
    }

    private static bool HasAnyAcquiredSiteBlueprintUnlock(SimState state)
    {
        if (state.Intel?.Unlocks is null || state.Intel.Unlocks.Count == default) return false;

        foreach (var kv in state.Intel.Unlocks.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var u = kv.Value;
            if (u is null) continue;
            if (u.Kind != UnlockKind.SiteBlueprint) continue;
            if (!u.IsAcquired) continue;
            return true;
        }

        return false;
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.001
    // Returns the reason code for an acquire attempt on the given unlock.
    public static UnlockReasonCode GetAcquireUnlockReasonCode(SimState state, string unlockId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrEmpty(unlockId)) return UnlockReasonCode.NotKnown;

        if (state.Intel?.Unlocks is null || !state.Intel.Unlocks.TryGetValue(unlockId, out var u) || u is null)
            return UnlockReasonCode.NotKnown;

        if (u.IsAcquired) return UnlockReasonCode.AlreadyAcquired;
        if (u.IsBlocked) return UnlockReasonCode.Blocked;

        return UnlockReasonCode.Ok;
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.005
    // Unlock explainability v0: schema-bound reason tokens, 1..3 action tokens, and stable explain chain tokens.
    // Ordering: Unlock entries sorted by UnlockId asc (StringComparer.Ordinal).
    public const string UnlockReasonToken_Ok = "UNLOCK_RC_OK_V0";
    public const string UnlockReasonToken_NotKnown = "UNLOCK_RC_NOT_KNOWN_V0";
    public const string UnlockReasonToken_AlreadyAcquired = "UNLOCK_RC_ALREADY_ACQUIRED_V0";
    public const string UnlockReasonToken_Blocked = "UNLOCK_RC_BLOCKED_V0";

    public const string UnlockActionToken_Acquire = "UNLOCK_ACTION_ACQUIRE_V0";
    public const string UnlockActionToken_Use = "UNLOCK_ACTION_USE_V0";
    public const string UnlockActionToken_DiscoverMore = "UNLOCK_ACTION_DISCOVER_MORE_V0";
    public const string UnlockActionToken_SatisfyPrereqs = "UNLOCK_ACTION_SATISFY_PREREQS_V0";
    public const string UnlockActionToken_CheckIntel = "UNLOCK_ACTION_CHECK_INTEL_V0";

    public const string UnlockChainToken_ExplainRoot = "UNLOCK_EXPLAIN_V0";
    public const string UnlockChainToken_Acquired = "UNLOCK_ACQUIRED_V0";
    public const string UnlockChainToken_BlockedFlag = "UNLOCK_BLOCKED_V0";

    public static string GetAcquireUnlockReasonToken(UnlockReasonCode rc)
    {
        if (rc == UnlockReasonCode.Ok) return UnlockReasonToken_Ok;
        if (rc == UnlockReasonCode.NotKnown) return UnlockReasonToken_NotKnown;
        if (rc == UnlockReasonCode.AlreadyAcquired) return UnlockReasonToken_AlreadyAcquired;
        if (rc == UnlockReasonCode.Blocked) return UnlockReasonToken_Blocked;
        return UnlockReasonToken_NotKnown;
    }

    public static ProgramExplain.UnlockPayload BuildUnlockExplainPayload(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        var payload = new ProgramExplain.UnlockPayload
        {
            Version = ProgramExplain.ExplainVersion,
            Tick = state.Tick,
            Unlocks = new List<ProgramExplain.UnlockEntry>()
        };

        if (state.Intel?.Unlocks is null || state.Intel.Unlocks.Count == default) return payload;

        foreach (var kv in state.Intel.Unlocks.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var unlockId = kv.Key ?? "";
            if (unlockId.Length == default) continue;

            var rc = GetAcquireUnlockReasonCode(state, unlockId);
            var reasonToken = GetAcquireUnlockReasonToken(rc);

            var actions = GetUnlockActionsForReason(rc);
            var chain = BuildUnlockExplainChainTokens(state, unlockId, rc);

            payload.Unlocks.Add(new ProgramExplain.UnlockEntry
            {
                UnlockId = unlockId,
                AcquireReasonCode = reasonToken,
                Actions = actions,
                ExplainChain = chain
            });
        }

        return payload;
    }

    private static List<string> GetUnlockActionsForReason(UnlockReasonCode rc)
    {
        // Deterministic action sets in deterministic order. No free-text. 1..3 tokens.
        if (rc == UnlockReasonCode.Ok)
            return new List<string> { UnlockActionToken_Acquire };

        if (rc == UnlockReasonCode.AlreadyAcquired)
            return new List<string> { UnlockActionToken_Use };

        if (rc == UnlockReasonCode.Blocked)
            return new List<string> { UnlockActionToken_SatisfyPrereqs, UnlockActionToken_CheckIntel };

        // NotKnown and any unexpected values
        return new List<string> { UnlockActionToken_DiscoverMore };
    }

    private static List<string> BuildUnlockExplainChainTokens(SimState state, string unlockId, UnlockReasonCode rc)
    {
        _ = unlockId;

        // Stable, compact chain: root token, reason token, optional state flags.
        var chain = new List<string> { UnlockChainToken_ExplainRoot, GetAcquireUnlockReasonToken(rc) };

        if (state.Intel?.Unlocks is null) return chain;
        if (!state.Intel.Unlocks.TryGetValue(unlockId, out var u) || u is null) return chain;

        if (u.IsAcquired) chain.Add(UnlockChainToken_Acquired);
        else if (u.IsBlocked) chain.Add(UnlockChainToken_BlockedFlag);

        return chain;
    }

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.004
    // Verb unlock ids are stable tokens (not player-facing strings).
    public const string UnlockId_DiscoveryScanVerbV0 = "UNLOCK_VERB_DISCOVERY_SCAN_V0";
    public const string UnlockId_DiscoveryAnalyzeVerbV0 = "UNLOCK_VERB_DISCOVERY_ANALYZE_V0";
    public const string UnlockId_DiscoveryExpeditionVerbV0 = "UNLOCK_VERB_DISCOVERY_EXPEDITION_V0";

    // GATE.S3_6.DISCOVERY_UNLOCK_CONTRACT.004
    // Deterministic verb unlock acquisition derived from discovery phases.
    // Ordering: evaluation is unordered (aggregate flags only). Persisted ordering is via GetUnlocksAscending.
    public static void RefreshVerbUnlocksFromDiscoveryPhases(SimState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Intel?.Discoveries is null) return;

        bool anySeenOrBetter = false;
        bool anyScannedOrBetter = false;
        bool anyAnalyzed = false;

        foreach (var kvp in state.Intel.Discoveries)
        {
            var d = kvp.Value;
            if (d is null) continue;

            // Discovery phases are monotonic; treat higher phases as inclusive for verb availability.
            if (d.Phase == DiscoveryPhase.Seen)
            {
                anySeenOrBetter = true;
                continue;
            }

            if (d.Phase == DiscoveryPhase.Scanned)
            {
                anySeenOrBetter = true;
                anyScannedOrBetter = true;
                continue;
            }

            if (d.Phase == DiscoveryPhase.Analyzed)
            {
                anySeenOrBetter = true;
                anyScannedOrBetter = true;
                anyAnalyzed = true;
            }
        }

        if (!anySeenOrBetter) return;

        EnsureUnlockAcquired(state, UnlockId_DiscoveryScanVerbV0);
        if (anyScannedOrBetter) EnsureUnlockAcquired(state, UnlockId_DiscoveryAnalyzeVerbV0);
        if (anyAnalyzed) EnsureUnlockAcquired(state, UnlockId_DiscoveryExpeditionVerbV0);
    }

    private static void EnsureUnlockAcquired(SimState state, string unlockId)
    {
        if (state.Intel is null) state.Intel = new IntelBook();
        if (state.Intel.Unlocks is null) return;
        if (string.IsNullOrEmpty(unlockId)) return;

        if (state.Intel.Unlocks.TryGetValue(unlockId, out var existing) && existing is not null)
        {
            if (!existing.IsAcquired)
            {
                existing.IsAcquired = true;
                state.Intel.Unlocks[unlockId] = existing;
            }

            return;
        }

        state.Intel.Unlocks[unlockId] = new UnlockContractV0
        {
            UnlockId = unlockId,
            IsAcquired = true
        };
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

        RefreshVerbUnlocksFromDiscoveryPhases(state);

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

        RefreshVerbUnlocksFromDiscoveryPhases(state);

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

        RefreshVerbUnlocksFromDiscoveryPhases(state);
    }

    // GATE.S3_6.RUMOR_INTEL_MIN.002
    // Grant a rumor lead on an explore action. leadId must be stable and caller-derived.
    // Idempotent: no-op if leadId already present.
    public static void GrantRumorLeadOnExplore(SimState state, string leadId, string sourceNodeId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrEmpty(leadId)) return;

        if (state.Intel is null) state.Intel = new IntelBook();
        if (state.Intel.RumorLeads.ContainsKey(leadId)) return;

        state.Intel.RumorLeads[leadId] = new RumorLead
        {
            LeadId = leadId,
            Status = RumorLeadStatus.Active,
            SourceVerbToken = "EXPLORE",
            Hint = new HintPayloadV0
            {
                RegionTags = new List<string> { sourceNodeId ?? "" },
                CoarseLocationToken = sourceNodeId ?? "",
                PrerequisiteTokens = new List<string> { "EXPLORATION" },
                ImpliedPayoffToken = "SITE_BLUEPRINT"
            }
        };
    }

    // GATE.S3_6.RUMOR_INTEL_MIN.002
    // Grant a rumor lead on a hub-analysis action. leadId must be stable and caller-derived.
    // Idempotent: no-op if leadId already present.
    public static void GrantRumorLeadOnHubAnalysis(SimState state, string leadId, string discoveryId)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrEmpty(leadId)) return;

        if (state.Intel is null) state.Intel = new IntelBook();
        if (state.Intel.RumorLeads.ContainsKey(leadId)) return;

        state.Intel.RumorLeads[leadId] = new RumorLead
        {
            LeadId = leadId,
            Status = RumorLeadStatus.Active,
            SourceVerbToken = "HUB_ANALYSIS",
            Hint = new HintPayloadV0
            {
                RegionTags = new List<string>(),
                CoarseLocationToken = "",
                PrerequisiteTokens = new List<string> { "HUB_ANALYSIS" },
                ImpliedPayoffToken = "BROKER_UNLOCK"
            }
        };

        _ = discoveryId; // reserved for future hint enrichment; not gameplay-affecting in v0
    }
}
