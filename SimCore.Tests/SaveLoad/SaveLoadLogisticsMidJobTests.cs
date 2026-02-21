using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using SimCore;
using SimCore.Schemas;
using SimCore.Systems;
using SimCore.World;

namespace SimCore.Tests.SaveLoad;

[TestFixture]
public sealed class SaveLoadLogisticsMidJobTests
{
    private static SimKernel KernelWithThreeStations()
    {
        var k = new SimKernel(seed: 123);

        var def = new WorldDefinition
        {
            WorldId = "micro_world_logi_saveload_001",
            WorldClasses =
            {
                // Deterministic fee state surface for save%load scaling gate:
                // Explicit class fee multipliers must persist across save%load.
                new WorldClassDefinition { WorldClassId = "class_low_fee",  FeeMultiplier = 0.90f },
                new WorldClassDefinition { WorldClassId = "class_mid_fee",  FeeMultiplier = 1.00f },
                new WorldClassDefinition { WorldClassId = "class_high_fee", FeeMultiplier = 1.10f }
            },
            Markets =
            {
                new WorldMarket { Id = "mkt_a", Inventory = new() { ["ore"] = 0 } },
                new WorldMarket { Id = "mkt_b", Inventory = new() { ["ore"] = 25 } },
                new WorldMarket { Id = "mkt_c", Inventory = new() { ["ore"] = 0 } }
            },
            Nodes =
            {
                new WorldNode { Id = "stn_a", Kind = "Station", Name = "A", MarketId = "mkt_a", WorldClassId = "class_low_fee",  Pos = new float[] { 0f, 0f, 0f } },
                new WorldNode { Id = "stn_b", Kind = "Station", Name = "B", MarketId = "mkt_b", WorldClassId = "class_mid_fee",  Pos = new float[] { 1f, 0f, 0f } },
                new WorldNode { Id = "stn_c", Kind = "Station", Name = "C", MarketId = "mkt_c", WorldClassId = "class_high_fee", Pos = new float[] { 2f, 0f, 0f } }
            },
            Edges =
            {
                new WorldEdge { Id = "lane_ab", FromNodeId = "stn_a", ToNodeId = "stn_b", Distance = 1.0f, TotalCapacity = 2 },
                new WorldEdge { Id = "lane_bc", FromNodeId = "stn_b", ToNodeId = "stn_c", Distance = 1.0f, TotalCapacity = 2 }
            },
            Player = new WorldPlayerStart { Credits = 0, LocationNodeId = "stn_a" }
        };

        WorldLoader.Apply(k.State, def);

        // Make travel multi-tick per edge so there is a real "mid-job" window after load applies.
        k.State.Fleets["fleet_trader_1"].Speed = 0.5f;

        return k;
    }

    private static void StepUntilMidJobLoaded(SimKernel k, int maxSteps)
    {
        var s = k.State;
        var f = s.Fleets["fleet_trader_1"];

        for (var i = 0; i < maxSteps; i++)
        {
            k.Step();

            var job = f.CurrentJob;
            if (job is null) continue;

            // We must save AFTER the pickup intent has applied, because PendingIntents are not persisted.
            // Condition: phase is Deliver, pickup latch set, cargo loaded, and we are NOT yet at destination.
            var midJob =
                job.Phase == SimCore.Entities.LogisticsJobPhase.Deliver &&
                job.PickupTransferIssued &&
                !job.DeliveryTransferIssued &&
                f.GetCargoUnits(job.GoodId) > 0 &&
                !string.Equals(f.CurrentNodeId, job.TargetNodeId, StringComparison.Ordinal);

            if (midJob)
                return;
        }

        Assert.Fail("Did not reach a mid-job loaded state within the step limit.");
    }

    private static int GetQueuedCountFromLaneReport(string report, string laneId)
    {
        // Format from LaneFlowSystem:
        // LANE_UTIL_REPORT_V0
        // tick=<n>
        // lane_id|delivered|capacity|queued
        // lane_ab|2|2|1
        var lines = report.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (!line.StartsWith(laneId + "|", StringComparison.Ordinal))
                continue;

            var parts = line.Split('|');
            if (parts.Length < 4) return 0;

            if (int.TryParse(parts[3], out var queued))
                return queued;

            return 0;
        }

        return 0;
    }

    private static string GetLogisticsRouteChoiceSnapshot(string saveJson)
    {
        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;
        var stateEl = root.TryGetProperty("State", out var st) ? st : root;

        if (!TryFindProperty(stateEl, "LogisticsEventLog", out var logEl) || logEl.ValueKind != JsonValueKind.Array)
        {
            return "LOGI_ROUTE_CHOICES_V0\n<missing>\n";
        }

        var rows = new List<string>();
        foreach (var e in logEl.EnumerateArray())
        {
            var chosen = ReadString(e, "ChosenRouteId");
            var tie = ReadString(e, "TieBreakReason");

            // Only keep actual route choice entries.
            if (string.IsNullOrEmpty(chosen) && string.IsNullOrEmpty(tie))
                continue;

            var tick = ReadInt(e, "Tick");
            var fleetId = ReadString(e, "FleetId");
            var origin = ReadString(e, "OriginId");
            var dest = ReadString(e, "DestId");

            rows.Add($"t={tick}|fleet={fleetId}|{origin}>{dest}|route={chosen}|tie={tie}");
        }

        rows.Sort(StringComparer.Ordinal);
        return "LOGI_ROUTE_CHOICES_V0\n" + string.Join("\n", rows) + "\n";
    }

    private static string GetWorldClassFeeSnapshot(string saveJson)
    {
        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;
        var stateEl = root.TryGetProperty("State", out var st) ? st : root;

        // Prefer explicit WorldClasses list (schema surface), otherwise fall back to leaf crawl.
        if (TryFindProperty(stateEl, "WorldClasses", out var classesEl) && classesEl.ValueKind == JsonValueKind.Array)
        {
            var rows = new List<string>();
            foreach (var c in classesEl.EnumerateArray())
            {
                var id = ReadString(c, "WorldClassId");
                var fm = ReadString(c, "FeeMultiplier");
                if (string.IsNullOrEmpty(fm) && c.ValueKind == JsonValueKind.Object && c.TryGetProperty("FeeMultiplier", out var v))
                    fm = v.GetRawText();

                rows.Add($"{id}|fee_multiplier={fm}");
            }

            rows.Sort(StringComparer.Ordinal);
            return "WORLD_CLASS_FEES_V0\n" + string.Join("\n", rows) + "\n";
        }

        // Fallback: deterministic leaf crawl restricted to FeeMultiplier only.
        var pairs = new List<string>();
        CollectFeeMultiplierSignals(stateEl, pathPrefix: "State", pairs);
        pairs.Sort(StringComparer.Ordinal);
        return "WORLD_CLASS_FEES_V0\n" + string.Join("\n", pairs) + "\n";
    }

    private static void CollectFeeMultiplierSignals(JsonElement el, string pathPrefix, List<string> outPairs)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    foreach (var p in el.EnumerateObject())
                    {
                        var name = p.Name;
                        var nextPath = pathPrefix + "." + name;

                        if (string.Equals(name, "FeeMultiplier", StringComparison.Ordinal))
                        {
                            if (IsLeaf(p.Value))
                                outPairs.Add($"{nextPath}={LeafToString(p.Value)}");
                        }

                        CollectFeeMultiplierSignals(p.Value, nextPath, outPairs);
                    }

                    break;
                }

            case JsonValueKind.Array:
                {
                    var idx = 0;
                    foreach (var item in el.EnumerateArray())
                    {
                        CollectFeeMultiplierSignals(item, pathPrefix + "[" + idx.ToString() + "]", outPairs);
                        idx++;
                    }

                    break;
                }
        }
    }

    private static string GetInFlightQueueSnapshot(string saveJson)
    {
        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        // Envelope-aware: save format may be { Version, Seed, State }.
        var stateEl = root.TryGetProperty("State", out var st) ? st : root;

        if (!TryFindProperty(stateEl, "InFlightTransfers", out var transfersEl) || transfersEl.ValueKind != JsonValueKind.Array)
        {
            return "INFLIGHT_SNAPSHOT_V0\n<missing>\n";
        }

        var rows = new List<string>();
        foreach (var t in transfersEl.EnumerateArray())
        {
            var id = ReadString(t, "Id");
            var edgeId = ReadString(t, "EdgeId");
            var qty = ReadInt(t, "Quantity");
            var arrive = ReadLong(t, "ArriveTick");

            rows.Add($"{edgeId}|{id}|qty={qty}|arrive={arrive}");
        }

        rows.Sort(StringComparer.Ordinal);

        return "INFLIGHT_SNAPSHOT_V0\n" + string.Join("\n", rows) + "\n";
    }

    private static string GetRouteFeeSignalSnapshot(string saveJson)
    {
        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var stateEl = root.TryGetProperty("State", out var st) ? st : root;

        var pairs = new List<string>();
        CollectSignals(stateEl, pathPrefix: "State", pairs);

        pairs.Sort(StringComparer.Ordinal);

        return "ROUTE_FEE_SIGNALS_V0\n" + string.Join("\n", pairs) + "\n";
    }

    private static void CollectSignals(JsonElement el, string pathPrefix, List<string> outPairs)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    foreach (var p in el.EnumerateObject())
                    {
                        var name = p.Name;
                        var nextPath = pathPrefix + "." + name;

                        // Only capture route%fee related leaves (and avoid full dumps).
                        // Case-insensitive contains check, deterministic ordering enforced by sorting at the end.
                        if (name.Contains("Route", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Fee", StringComparison.OrdinalIgnoreCase))
                        {
                            if (IsLeaf(p.Value))
                            {
                                outPairs.Add($"{nextPath}={LeafToString(p.Value)}");
                            }
                        }

                        CollectSignals(p.Value, nextPath, outPairs);
                    }

                    break;
                }

            case JsonValueKind.Array:
                {
                    var idx = 0;
                    foreach (var item in el.EnumerateArray())
                    {
                        CollectSignals(item, pathPrefix + "[" + idx.ToString() + "]", outPairs);
                        idx++;
                    }

                    break;
                }
        }
    }

    private static bool TryFindProperty(JsonElement el, string name, out JsonElement found)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty(name, out found))
                return true;

            foreach (var p in el.EnumerateObject())
            {
                if (TryFindProperty(p.Value, name, out found))
                    return true;
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                if (TryFindProperty(item, name, out found))
                    return true;
            }
        }

        found = default;
        return false;
    }

    private static string ReadString(JsonElement obj, string prop)
    {
        if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString() ?? "";
        return "";
    }

    private static int ReadInt(JsonElement obj, string prop)
    {
        if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
            return i;
        return 0;
    }

    private static long ReadLong(JsonElement obj, string prop)
    {
        if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var l))
            return l;
        return 0L;
    }

    private static bool IsLeaf(JsonElement el)
    {
        return el.ValueKind == JsonValueKind.String ||
               el.ValueKind == JsonValueKind.Number ||
               el.ValueKind == JsonValueKind.True ||
               el.ValueKind == JsonValueKind.False ||
               el.ValueKind == JsonValueKind.Null;
    }

    private static string LeafToString(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? "",
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => el.GetRawText()
        };
    }

    [Test]
    public void SaveLoad_MidLogisticsJob_ContinuesToSameFinalHash_AsUninterrupted()
    {
        // Uninterrupted run (A)
        var simA = KernelWithThreeStations();
        var sA = simA.State;
        var fA = sA.Fleets["fleet_trader_1"];

        // Force a deterministic lane overflow queue (capacity=2, 3 simultaneous transfers due at tick 1).
        // This asserts "lane capacity queues are preserved across save%load".
        sA.Markets["mkt_a"].Inventory["ore"] = 10;

        Assert.That(LaneFlowSystem.TryEnqueueTransfer(sA, "stn_a", "stn_b", "ore", 1, "q_xfer_001"), Is.True);
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(sA, "stn_a", "stn_b", "ore", 1, "q_xfer_002"), Is.True);
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(sA, "stn_a", "stn_b", "ore", 1, "q_xfer_003"), Is.True);

        Assert.That(LogisticsSystem.PlanLogistics(sA, fA, "mkt_b", "mkt_c", "ore", 5), Is.True);

        // Advance until we are mid-job (after load applied).
        StepUntilMidJobLoaded(simA, maxSteps: 500);

        // Inject lane-pressure transfers at the mid-job point, then step once so LaneFlow realizes them.
        // Capture the depart tick so we can assert queueing against exactly these injected transfers.
        var injectedDepartTick = simA.State.Tick;

        Assert.That(LaneFlowSystem.TryEnqueueTransfer(sA, "stn_a", "stn_b", "ore", 1, "q_xfer_101"), Is.True);
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(sA, "stn_a", "stn_b", "ore", 1, "q_xfer_102"), Is.True);
        Assert.That(LaneFlowSystem.TryEnqueueTransfer(sA, "stn_a", "stn_b", "ore", 1, "q_xfer_103"), Is.True);

        simA.Step();

        // Save at mid-job and after lane-pressure transfers have been processed into persisted state.
        // Explicit queue assertion (not proxy-only):
        var reportAtSave = LaneFlowSystem.GetLastLaneUtilizationReport(simA.State);
        Assert.That(reportAtSave, Does.Contain("lane_id|delivered|capacity|queued"), "Expected lane utilization report header at save tick.");
        Assert.That(reportAtSave, Does.Contain("lane_ab|"), "Expected lane_ab row in lane utilization report at save tick.");

        // Explicit queue assertion (authoritative):
        // LaneFlowSystem encodes overflow by setting ArriveTick = now + 1.
        Assert.That(simA.State.InFlightTransfers.Any(t =>
                t.Quantity > 0 &&
                t.DepartTick == injectedDepartTick &&
                string.Equals(t.FromNodeId, "stn_a", StringComparison.Ordinal) &&
                string.Equals(t.ToNodeId, "stn_b", StringComparison.Ordinal) &&
                t.ArriveTick > injectedDepartTick),
            Is.True, "Expected at least one injected stn_a>stn_b transfer to be deferred (ArriveTick > DepartTick) and still in-flight at save tick.");

        var midJson = simA.SaveToString();

        // Loaded-from-save run (B)
        var simB = new SimKernel(seed: 123);
        simB.LoadFromString(midJson);

        // Immediately compare required persisted state snapshots (explicit surfaces).
        var inflightA0 = GetInFlightQueueSnapshot(simA.SaveToString());
        var inflightB0 = GetInFlightQueueSnapshot(simB.SaveToString());
        Assert.That(inflightB0, Is.EqualTo(inflightA0), "In-flight transfer queue snapshot drift immediately after load.");

        var routeA0 = GetLogisticsRouteChoiceSnapshot(simA.SaveToString());
        var routeB0 = GetLogisticsRouteChoiceSnapshot(simB.SaveToString());
        Assert.That(routeA0, Does.Not.Contain("<missing>"), "Expected LogisticsEventLog to be present in save JSON.");
        Assert.That(routeA0.Split('\n').Length, Is.GreaterThan(2), "Expected at least one route choice record in LogisticsEventLog snapshot.");
        Assert.That(routeB0, Is.EqualTo(routeA0), "ChosenRouteId%TieBreakReason snapshot drift immediately after load.");

        var feeA0 = GetWorldClassFeeSnapshot(simA.SaveToString());
        var feeB0 = GetWorldClassFeeSnapshot(simB.SaveToString());
        Assert.That(feeA0, Does.Not.Contain("<missing>"), "Expected WorldClasses fee config to be present in save JSON.");
        Assert.That(feeB0, Is.EqualTo(feeA0), "World class fee multiplier snapshot drift immediately after load.");

        // Continue both in lockstep, comparing deterministic LaneFlow report as an event-stream proxy.
        const int cap = 200;
        var extra = 5;

        for (var i = 0; i < cap; i++)
        {
            var aHas = simA.State.Fleets["fleet_trader_1"].CurrentJob is not null;
            var bHas = simB.State.Fleets["fleet_trader_1"].CurrentJob is not null;

            if (!aHas && !bHas)
                break;

            simA.Step();
            simB.Step();

            var reportA = LaneFlowSystem.GetLastLaneUtilizationReport(simA.State);
            var reportB = LaneFlowSystem.GetLastLaneUtilizationReport(simB.State);
            Assert.That(reportB, Is.EqualTo(reportA), $"LaneFlow report drift at loop tick i={i}.");

            var inflightA = GetInFlightQueueSnapshot(simA.SaveToString());
            var inflightB = GetInFlightQueueSnapshot(simB.SaveToString());
            Assert.That(inflightB, Is.EqualTo(inflightA), $"In-flight transfer queue drift at loop tick i={i}.");

            var routeA = GetLogisticsRouteChoiceSnapshot(simA.SaveToString());
            var routeB = GetLogisticsRouteChoiceSnapshot(simB.SaveToString());
            Assert.That(routeB, Is.EqualTo(routeA), $"ChosenRouteId%TieBreakReason drift at loop tick i={i}.");

            var feeA = GetWorldClassFeeSnapshot(simA.SaveToString());
            var feeB = GetWorldClassFeeSnapshot(simB.SaveToString());
            Assert.That(feeB, Is.EqualTo(feeA), $"World class fee multiplier drift at loop tick i={i}.");
        }

        for (var i = 0; i < extra; i++)
        {
            simA.Step();
            simB.Step();

            var reportA = LaneFlowSystem.GetLastLaneUtilizationReport(simA.State);
            var reportB = LaneFlowSystem.GetLastLaneUtilizationReport(simB.State);
            Assert.That(reportB, Is.EqualTo(reportA), $"LaneFlow report drift during extra tick i={i}.");
        }

        var hashA = simA.State.GetSignature();
        var hashB = simB.State.GetSignature();

        TestContext.Out.WriteLine($"Uninterrupted Hash: {hashA}");
        TestContext.Out.WriteLine($"SaveLoad     Hash: {hashB}");

        Assert.That(hashB, Is.EqualTo(hashA), "Save/load mid-job continuation drift detected.");
    }
}
