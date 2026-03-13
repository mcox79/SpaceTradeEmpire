extends SceneTree

# GATE.S8.HAVEN.HEADLESS_DEPTH.001
# Headless proof: boot → verify Haven depth features:
# residents list, trophy wall state, adaptation fragments,
# resonance pairs, T3 module catalog, ancient hull catalog.
# Emits: HD1|HAVEN_DEPTH_PROOF|PASS

const PREFIX := "HD1|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	VERIFY_RESIDENTS,
	VERIFY_TROPHY_WALL,
	VERIFY_FRAGMENTS,
	VERIFY_RESONANCE_PAIRS,
	VERIFY_T3_MODULES,
	VERIFY_ANCIENT_HULLS,
	VERIFY_NEAREST_LOOT,
	DONE
}

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null


func _initialize() -> void:
	print(PREFIX + "START")


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_BRIDGE:
			_bridge = root.get_node_or_null("SimBridge")
			if _bridge != null:
				_polls = 0
				_phase = Phase.WAIT_READY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_found")

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_phase = Phase.VERIFY_RESIDENTS
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.VERIFY_RESIDENTS:
			if not _bridge.has_method("GetHavenResidentsV0"):
				_fail("GetHavenResidentsV0_missing")
				return false
			var residents: Array = _bridge.call("GetHavenResidentsV0")
			# At boot, Haven may have The Keeper if discovered, or empty if not
			print(PREFIX + "RESIDENTS|count=%d|PASS" % residents.size())
			_phase = Phase.VERIFY_TROPHY_WALL

		Phase.VERIFY_TROPHY_WALL:
			if not _bridge.has_method("GetTrophyWallV0"):
				_fail("GetTrophyWallV0_missing")
				return false
			var trophy: Array = _bridge.call("GetTrophyWallV0")
			# All 16 fragments listed (most uncollected at boot)
			if trophy.size() != 16:
				_fail("trophy_wall_count=%d_expected_16" % trophy.size())
				return false
			print(PREFIX + "TROPHY_WALL|count=%d|PASS" % trophy.size())
			_phase = Phase.VERIFY_FRAGMENTS

		Phase.VERIFY_FRAGMENTS:
			if not _bridge.has_method("GetAdaptationFragmentsV0"):
				_fail("GetAdaptationFragmentsV0_missing")
				return false
			var frags: Array = _bridge.call("GetAdaptationFragmentsV0")
			if frags.size() != 16:
				_fail("fragments_count=%d_expected_16" % frags.size())
				return false
			# Verify each has required fields
			for entry in frags:
				if typeof(entry) != TYPE_DICTIONARY:
					_fail("fragment_entry_not_dict")
					return false
				var fid: String = str(entry.get("fragment_id", ""))
				if fid.is_empty():
					_fail("fragment_missing_id")
					return false
			print(PREFIX + "FRAGMENTS|count=%d|PASS" % frags.size())
			_phase = Phase.VERIFY_RESONANCE_PAIRS

		Phase.VERIFY_RESONANCE_PAIRS:
			if not _bridge.has_method("GetResonancePairsV0"):
				_fail("GetResonancePairsV0_missing")
				return false
			var pairs: Array = _bridge.call("GetResonancePairsV0")
			if pairs.size() != 8:
				_fail("pairs_count=%d_expected_8" % pairs.size())
				return false
			print(PREFIX + "RESONANCE_PAIRS|count=%d|PASS" % pairs.size())
			_phase = Phase.VERIFY_T3_MODULES

		Phase.VERIFY_T3_MODULES:
			if not _bridge.has_method("GetT3ModuleCatalogV0"):
				_fail("GetT3ModuleCatalogV0_missing")
				return false
			var modules: Array = _bridge.call("GetT3ModuleCatalogV0")
			# 9 T3 precursor modules
			if modules.size() < 9:
				_fail("t3_modules_count=%d_expected_gte_9" % modules.size())
				return false
			print(PREFIX + "T3_MODULES|count=%d|PASS" % modules.size())
			_phase = Phase.VERIFY_ANCIENT_HULLS

		Phase.VERIFY_ANCIENT_HULLS:
			if not _bridge.has_method("GetAncientHullsV0"):
				_fail("GetAncientHullsV0_missing")
				return false
			var hulls: Array = _bridge.call("GetAncientHullsV0")
			if hulls.size() != 3:
				_fail("hulls_count=%d_expected_3" % hulls.size())
				return false
			# Verify each has required fields
			for entry in hulls:
				if typeof(entry) != TYPE_DICTIONARY:
					_fail("hull_entry_not_dict")
					return false
				var cid: String = str(entry.get("ship_class_id", ""))
				if cid.is_empty():
					_fail("hull_missing_class_id")
					return false
			print(PREFIX + "ANCIENT_HULLS|count=%d|PASS" % hulls.size())
			_phase = Phase.VERIFY_NEAREST_LOOT

		Phase.VERIFY_NEAREST_LOOT:
			if not _bridge.has_method("GetNearestLootV0"):
				print(PREFIX + "NEAREST_LOOT|method_missing|SKIP")
			else:
				var loot: Dictionary = _bridge.call("GetNearestLootV0")
				var has_loot: bool = bool(loot.get("has_loot", false))
				print(PREFIX + "NEAREST_LOOT|has_loot=%s|PASS" % str(has_loot))
			print(PREFIX + "HAVEN_DEPTH_PROOF|PASS")
			_phase = Phase.DONE
			_quit()

		Phase.DONE:
			pass

	return false


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE
	_quit()


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
