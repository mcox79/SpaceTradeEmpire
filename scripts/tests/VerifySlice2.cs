using Godot;
using System;
using SimCore;
using SimCore.Entities;
using SimCore.Systems;
using System.Collections.Generic;

namespace SpaceTradeEmpire.Tests;

public partial class VerifySlice2 : SceneTree
{
    public override void _Initialize()
    {
        GD.Print("[VerifySlice2] Starting C# Integration Test...");

        try
        {
            // 1. Create State
            var state = new SimState(12345);

            // 2. Setup Market with Ore
            var mkt = new Market { Id = "station_alpha" };
            mkt.Inventory["ore"] = 100;
            mkt.Inventory["metal"] = 0;
            state.Markets.Add(mkt.Id, mkt);

            // 3. Setup Industry (Refinery)
            var site = new IndustrySite
            {
                Id = "refinery_1",
                NodeId = "station_alpha",
                Inputs = new Dictionary<string, int> { { "ore", 10 } },
                Outputs = new Dictionary<string, int> { { "metal", 5 } }
            };
            state.IndustrySites.Add(site.Id, site);

            GD.Print($"[VerifySlice2] Initial: Ore={mkt.Inventory["ore"]} Metal={mkt.Inventory.GetValueOrDefault("metal", 0)}");

            // 4. Run Industry Process Tick
            IndustrySystem.Process(state);

            GD.Print($"[VerifySlice2] Final: Ore={mkt.Inventory["ore"]} Metal={mkt.Inventory.GetValueOrDefault("metal", 0)}");

            // 5. Assertions
            if (mkt.Inventory["ore"] != 90)
                throw new Exception($"FAIL: Ore should be 90 but is {mkt.Inventory["ore"]}");

            if (mkt.Inventory["metal"] != 5)
                throw new Exception($"FAIL: Metal should be 5 but is {mkt.Inventory["metal"]}");

            GD.Print("[SUCCESS] VERIFICATION PASSED: Conservation of Mass Confirmed.");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CRITICAL FAIL] {ex.Message}");
            GD.PrintErr(ex.StackTrace);
            Quit(1);
        }
    }
}