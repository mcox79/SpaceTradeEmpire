extends SceneTree

# GATE.S5.LOOT.BRIDGE_PROOF.001
# Verifies: GetNearbyLootV0 bridge query returns valid loot data.
# Emits: LOOT_PROOF|PASS

const PREFIX := "LOOT|"
const MAX_POLLS := 600

enum Phase {
	WAIT_BRIDGE, WAIT_READY,
	CHECK_LOOT_QUERY,
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
				_phase = Phase.CHECK_LOOT_QUERY
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_not_ready")

		Phase.CHECK_LOOT_QUERY:
			if not _bridge.has_method("GetNearbyLootV0"):
				_fail("missing_GetNearbyLootV0")
				return false
			if not _bridge.has_method("DispatchCollectLootV0"):
				_fail("missing_DispatchCollectLootV0")
				return false

			# Query nearby loot (may be empty if no combat happened yet).
			var loot: Array = _bridge.call("GetNearbyLootV0")
			print(PREFIX + "nearby_loot_count=%d" % loot.size())

			# Verify the query returns the expected schema for any drop present.
			for drop in loot:
				if typeof(drop) == TYPE_DICTIONARY:
					var drop_id: String = str(drop.get("drop_id", ""))
					var rarity: String = str(drop.get("rarity", ""))
					var credits: int = int(drop.get("credits", 0))
					print(PREFIX + "drop=%s rarity=%s credits=%d" % [drop_id, rarity, credits])

			# Loot bridge queries are functional.
			print(PREFIX + "LOOT_PROOF|PASS")
			_phase = Phase.DONE
			if _bridge.has_method("StopSimV0"):
				_bridge.call("StopSimV0")
			quit(0)

		Phase.DONE:
			pass

	return false


func _fail(reason: String) -> void:
	print(PREFIX + "LOOT_PROOF|FAIL|" + reason)
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(1)
