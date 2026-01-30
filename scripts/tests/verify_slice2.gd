extends SceneTree

func _init():
print("[VerifySlice2] Starting Integration Test...")

# 1. Create State
var state = SimCore.SimState.new(12345)

# 2. Setup Market with Ore
var mkt = SimCore.Entities.Market.new()
mkt.Id = "station_alpha"
mkt.Inventory["ore"] = 100
mkt.Inventory["metal"] = 0
state.Markets.Add("station_alpha", mkt)

# 3. Setup Industry (Refinery)
var site = SimCore.Entities.IndustrySite.new()
site.Id = "refinery_1"
site.NodeId = "station_alpha"
site.Inputs.Add("ore", 10)
site.Outputs.Add("metal", 5)
state.IndustrySites.Add("refinery_1", site)

print("[VerifySlice2] Initial: Ore=", mkt.Inventory["ore"], " Metal=", mkt.Inventory["metal"])

# 4. Run Industry Process Tick
SimCore.Systems.IndustrySystem.Process(state)

print("[VerifySlice2] Final: Ore=", mkt.Inventory["ore"], " Metal=", mkt.Inventory["metal"])

# 5. Assertions
if mkt.Inventory["ore"] != 90:
print("[FAIL] Ore should be 90")
quit(1)

if mkt.Inventory["metal"] != 5:
print("[FAIL] Metal should be 5")
quit(1)

print("[SUCCESS] VERIFICATION PASSED: Conservation of Mass Confirmed.")
quit(0)