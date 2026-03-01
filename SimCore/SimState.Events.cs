using System.Text.Json.Serialization;
using System.Collections.Generic;
using System;

namespace SimCore;

public partial class SimState
{
    // Cap constants — oldest events are evicted when a log exceeds its limit.
    // Sized to cover several hundred ticks of typical activity while bounding save-file and LINQ cost.
    private const int MaxLogisticsEventLog = 2000;
    private const int MaxSecurityEventLog  = 2000;
    private const int MaxFleetEventLog     = 2000;
    private const int MaxExploitationEventLog = 500;
    private const int MaxIndustryEventLog     = 500;

    // Logistics event stream (Slice 3 / GATE.LOGI.EVENT.001)
    [JsonInclude] public long NextLogisticsEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-fleet ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextLogisticsEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.LogisticsEvents.Event> LogisticsEventLog { get; private set; } = new();

    // Reusable buffers for per-tick deterministic logistics event finalization.
    // Private so they are not serialized and do not affect determinism across fresh runs.
    private readonly List<int> _logiFinalizeIdx = new();
    private readonly List<int> _logiFinalizeDest = new();
    private SimCore.Events.LogisticsEvents.Event[] _logiFinalizeTemp = Array.Empty<SimCore.Events.LogisticsEvents.Event>();

    // Security incident event stream (Slice 3 / GATE.S3.RISK_MODEL.001)
    [JsonInclude] public long NextSecurityEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-edge ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextSecurityEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.SecurityEvents.Event> SecurityEventLog { get; private set; } = new();

    // Reusable buffers for per-tick deterministic security event finalization.
    // Private so they are not serialized and do not affect determinism across fresh runs.
    private readonly List<int> _secFinalizeIdx = new();
    private readonly List<int> _secFinalizeDest = new();
    private SimCore.Events.SecurityEvents.Event[] _secFinalizeTemp = Array.Empty<SimCore.Events.SecurityEvents.Event>();

    // Fleet event stream (Slice 3 / GATE.S3.FLEET.ROLES.001)
    [JsonInclude] public long NextFleetEventSeq { get; set; } = 1;

    // Non-serialized emission counter used to preserve within-fleet ordering prior to tick-final Seq assignment.
    [JsonInclude] public long NextFleetEmitOrder { get; set; } = 1;

    [JsonInclude] public List<SimCore.Events.FleetEvents.Event> FleetEventLog { get; private set; } = new();

    // Reusable buffers for per-tick deterministic fleet event finalization.
    // Private so they are not serialized and do not affect determinism across fresh runs.
    private readonly List<int> _fleetFinalizeIdx = new();
    private readonly List<int> _fleetFinalizeDest = new();
    private SimCore.Events.FleetEvents.Event[] _fleetFinalizeTemp = Array.Empty<SimCore.Events.FleetEvents.Event>();

    public void EmitLogisticsEvent(SimCore.Events.LogisticsEvents.Event e)
    {
        if (e is null) return;

        // Buffer event and finalize Seq at end of tick using deterministic ordering rules.
        var emitOrder = NextLogisticsEmitOrder;
        NextLogisticsEmitOrder = checked(NextLogisticsEmitOrder + 1);

        e.Version = SimCore.Events.LogisticsEvents.EventsVersion;
        e.Seq = 0; // assigned during tick finalization
        e.EmitOrder = emitOrder;
        e.Tick = Tick;

        LogisticsEventLog ??= new List<SimCore.Events.LogisticsEvents.Event>();
        LogisticsEventLog.Add(e);
    }

    public void EmitSecurityEvent(SimCore.Events.SecurityEvents.Event e)
    {
        if (e is null) return;

        // Buffer event and finalize Seq at end of tick using deterministic ordering rules.
        var emitOrder = NextSecurityEmitOrder;
        NextSecurityEmitOrder = checked(NextSecurityEmitOrder + 1);

        e.Version = SimCore.Events.SecurityEvents.EventsVersion;
        e.Seq = 0; // assigned during tick finalization
        e.EmitOrder = emitOrder;
        e.Tick = Tick;

        SecurityEventLog ??= new List<SimCore.Events.SecurityEvents.Event>();
        SecurityEventLog.Add(e);
    }

    public void EmitFleetEvent(SimCore.Events.FleetEvents.Event e)
    {
        if (e is null) return;

        // Buffer event and finalize Seq at end of tick using deterministic ordering rules.
        var emitOrder = NextFleetEmitOrder;
        NextFleetEmitOrder = checked(NextFleetEmitOrder + 1);

        e.Version = SimCore.Events.FleetEvents.EventsVersion;
        e.Seq = 0; // assigned during tick finalization
        e.EmitOrder = emitOrder;
        e.Tick = Tick;

        FleetEventLog ??= new List<SimCore.Events.FleetEvents.Event>();
        FleetEventLog.Add(e);
    }

    // GATE.S4.INDU.MIN_LOOP.001
    // Deterministic industry event emission (Seq assigned immediately; ordering is defined by deterministic system iteration order).
    // GATE.S3_6.EXPLOITATION_PACKAGES.002
    // Deterministic exploitation event emission.
    // Ordering is deterministic: emitted in Apply order, driven by stable program-id iteration in ProgramSystem.
    public void AppendExploitationEvent(string entry)
    {
        if (entry is null) entry = "";
        ExploitationEventLog ??= new List<string>();
        ExploitationEventLog.Add(entry);
        if (ExploitationEventLog.Count > MaxExploitationEventLog)
            ExploitationEventLog.RemoveRange(0, ExploitationEventLog.Count - MaxExploitationEventLog);
    }

    public void EmitIndustryEvent(string note)
    {
        if (note is null) note = "";
        var seq = NextIndustryEventSeq;
        NextIndustryEventSeq = checked(NextIndustryEventSeq + 1);

        IndustryEventLog ??= new List<string>();
        IndustryEventLog.Add($"I{seq} tick={Tick} {note}");
        if (IndustryEventLog.Count > MaxIndustryEventLog)
            IndustryEventLog.RemoveRange(0, IndustryEventLog.Count - MaxIndustryEventLog);
    }

    private void FinalizeFleetEventsForTick()
    {
        if (FleetEventLog is null) return;
        if (FleetEventLog.Count == 0) return;

        // Reuse buffers to avoid per-tick allocations.
        _fleetFinalizeIdx.Clear();

        // Gather indices of events emitted this tick that have not yet been assigned a Seq.
        for (var i = 0; i < FleetEventLog.Count; i++)
        {
            var e = FleetEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            _fleetFinalizeIdx.Add(i);
        }

        if (_fleetFinalizeIdx.Count == 0) return;

        // Sort indices in deterministic event order without LINQ allocations.
        _fleetFinalizeIdx.Sort((ai, bi) =>
        {
            var a = FleetEventLog[ai];
            var b = FleetEventLog[bi];

            int c;
            c = string.CompareOrdinal(a.FleetId ?? "", b.FleetId ?? ""); if (c != 0) return c;
            c = a.EmitOrder.CompareTo(b.EmitOrder); if (c != 0) return c;
            c = ((int)a.Type).CompareTo((int)b.Type); if (c != 0) return c;
            c = string.CompareOrdinal(a.DiscoveryId ?? "", b.DiscoveryId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.NodeId ?? "", b.NodeId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.ChosenRouteId ?? "", b.ChosenRouteId ?? ""); if (c != 0) return c;
            c = a.Role.CompareTo(b.Role); if (c != 0) return c;
            c = a.ProfitScore.CompareTo(b.ProfitScore); if (c != 0) return c;
            c = a.CapacityScore.CompareTo(b.CapacityScore); if (c != 0) return c;
            c = a.RiskScore.CompareTo(b.RiskScore); if (c != 0) return c;
            c = a.ReasonCode.CompareTo(b.ReasonCode); if (c != 0) return c;
            c = a.PhaseAfter.CompareTo(b.PhaseAfter); if (c != 0) return c;
            c = string.CompareOrdinal(a.Note ?? "", b.Note ?? ""); if (c != 0) return c;

            // Absolute final tiebreak: index to keep ordering deterministic even if all keys match.
            return ai.CompareTo(bi);
        });

        // Assign Seq in deterministic order (idx is now sorted by event order).
        for (var j = 0; j < _fleetFinalizeIdx.Count; j++)
        {
            var e = FleetEventLog[_fleetFinalizeIdx[j]];
            var seq = NextFleetEventSeq;
            NextFleetEventSeq = checked(NextFleetEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        // Write the sorted events back into the same set of slots in ascending index order.
        _fleetFinalizeDest.Clear();
        _fleetFinalizeDest.AddRange(_fleetFinalizeIdx);
        _fleetFinalizeDest.Sort();

        if (_fleetFinalizeTemp.Length < _fleetFinalizeIdx.Count)
            _fleetFinalizeTemp = new SimCore.Events.FleetEvents.Event[_fleetFinalizeIdx.Count];

        for (var j = 0; j < _fleetFinalizeIdx.Count; j++)
            _fleetFinalizeTemp[j] = FleetEventLog[_fleetFinalizeIdx[j]];

        for (var j = 0; j < _fleetFinalizeDest.Count; j++)
            FleetEventLog[_fleetFinalizeDest[j]] = _fleetFinalizeTemp[j];

        if (FleetEventLog.Count > MaxFleetEventLog)
            FleetEventLog.RemoveRange(0, FleetEventLog.Count - MaxFleetEventLog);
    }

    private void FinalizeLogisticsEventsForTick()
    {
        if (LogisticsEventLog is null) return;
        if (LogisticsEventLog.Count == 0) return;

        // Reuse buffers to avoid per-tick allocations.
        _logiFinalizeIdx.Clear();

        // Gather indices of events emitted this tick that have not yet been assigned a Seq.
        for (var i = 0; i < LogisticsEventLog.Count; i++)
        {
            var e = LogisticsEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            _logiFinalizeIdx.Add(i);
        }

        if (_logiFinalizeIdx.Count == 0) return;

        // Sort indices in deterministic event order without LINQ allocations.
        _logiFinalizeIdx.Sort((ai, bi) =>
        {
            var a = LogisticsEventLog[ai];
            var b = LogisticsEventLog[bi];

            int c;
            c = string.CompareOrdinal(a.FleetId ?? "", b.FleetId ?? ""); if (c != 0) return c;
            c = a.EmitOrder.CompareTo(b.EmitOrder); if (c != 0) return c;
            c = ((int)a.Type).CompareTo((int)b.Type); if (c != 0) return c;
            c = string.CompareOrdinal(a.GoodId ?? "", b.GoodId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.SourceNodeId ?? "", b.SourceNodeId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.TargetNodeId ?? "", b.TargetNodeId ?? ""); if (c != 0) return c;
            c = a.Amount.CompareTo(b.Amount); if (c != 0) return c;
            c = string.CompareOrdinal(a.Note ?? "", b.Note ?? ""); if (c != 0) return c;

            // Absolute final tiebreak: index to keep ordering deterministic even if all keys match.
            return ai.CompareTo(bi);
        });

        // Assign Seq in deterministic order (idx is now sorted by event order).
        for (var j = 0; j < _logiFinalizeIdx.Count; j++)
        {
            var e = LogisticsEventLog[_logiFinalizeIdx[j]];
            var seq = NextLogisticsEventSeq;
            NextLogisticsEventSeq = checked(NextLogisticsEventSeq + 1);
            e.Seq = seq;
        }

        // Reorder the log in-place for this tick only, so list order matches deterministic order.
        // Write the sorted events back into the same set of slots in ascending index order.
        _logiFinalizeDest.Clear();
        _logiFinalizeDest.AddRange(_logiFinalizeIdx);
        _logiFinalizeDest.Sort();

        if (_logiFinalizeTemp.Length < _logiFinalizeIdx.Count)
            _logiFinalizeTemp = new SimCore.Events.LogisticsEvents.Event[_logiFinalizeIdx.Count];

        for (var j = 0; j < _logiFinalizeIdx.Count; j++)
            _logiFinalizeTemp[j] = LogisticsEventLog[_logiFinalizeIdx[j]];

        for (var j = 0; j < _logiFinalizeDest.Count; j++)
            LogisticsEventLog[_logiFinalizeDest[j]] = _logiFinalizeTemp[j];

        if (LogisticsEventLog.Count > MaxLogisticsEventLog)
            LogisticsEventLog.RemoveRange(0, LogisticsEventLog.Count - MaxLogisticsEventLog);
    }

    private void FinalizeSecurityEventsForTick()
    {
        if (SecurityEventLog is null) return;
        if (SecurityEventLog.Count == 0) return;

        _secFinalizeIdx.Clear();

        for (var i = 0; i < SecurityEventLog.Count; i++)
        {
            var e = SecurityEventLog[i];
            if (e is null) continue;
            if (e.Tick != Tick) continue;
            if (e.Seq != 0) continue;
            _secFinalizeIdx.Add(i);
        }

        if (_secFinalizeIdx.Count == 0) return;

        _secFinalizeIdx.Sort((ai, bi) =>
        {
            var a = SecurityEventLog[ai];
            var b = SecurityEventLog[bi];

            int c;
            c = string.CompareOrdinal(a.EdgeId ?? "", b.EdgeId ?? ""); if (c != 0) return c;
            c = a.EmitOrder.CompareTo(b.EmitOrder); if (c != 0) return c;
            c = ((int)a.Type).CompareTo((int)b.Type); if (c != 0) return c;
            c = string.CompareOrdinal(a.FromNodeId ?? "", b.FromNodeId ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.ToNodeId ?? "", b.ToNodeId ?? ""); if (c != 0) return c;
            c = a.RiskBand.CompareTo(b.RiskBand); if (c != 0) return c;
            c = a.DelayTicks.CompareTo(b.DelayTicks); if (c != 0) return c;
            c = a.LossUnits.CompareTo(b.LossUnits); if (c != 0) return c;
            c = a.InspectionTicks.CompareTo(b.InspectionTicks); if (c != 0) return c;
            c = string.CompareOrdinal(a.CauseChain ?? "", b.CauseChain ?? ""); if (c != 0) return c;
            c = string.CompareOrdinal(a.Note ?? "", b.Note ?? ""); if (c != 0) return c;

            return ai.CompareTo(bi);
        });

        for (var j = 0; j < _secFinalizeIdx.Count; j++)
        {
            var e = SecurityEventLog[_secFinalizeIdx[j]];
            var seq = NextSecurityEventSeq;
            NextSecurityEventSeq = checked(NextSecurityEventSeq + 1);
            e.Seq = seq;
        }

        _secFinalizeDest.Clear();
        _secFinalizeDest.AddRange(_secFinalizeIdx);
        _secFinalizeDest.Sort();

        if (_secFinalizeTemp.Length < _secFinalizeIdx.Count)
            _secFinalizeTemp = new SimCore.Events.SecurityEvents.Event[_secFinalizeIdx.Count];

        for (var j = 0; j < _secFinalizeIdx.Count; j++)
            _secFinalizeTemp[j] = SecurityEventLog[_secFinalizeIdx[j]];

        for (var j = 0; j < _secFinalizeDest.Count; j++)
            SecurityEventLog[_secFinalizeDest[j]] = _secFinalizeTemp[j];

        if (SecurityEventLog.Count > MaxSecurityEventLog)
            SecurityEventLog.RemoveRange(0, SecurityEventLog.Count - MaxSecurityEventLog);
    }
}
