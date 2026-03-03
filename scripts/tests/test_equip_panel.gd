extends SceneTree

# GATE.S4.MODULE_MODEL.EQUIP_PANEL.001
# Verifies: GetHeroShipLoadoutV0() returns >= 4 slots.
# Verifies: DispatchEquipModuleV0 installs weapon_laser_mk1 into slot_weapon_0.

const PREFIX := "EQPV0|"
const MAX_POLLS := 400

enum Phase { WAIT_BRIDGE, WAIT_READY, CHECK_SLOTS, WAIT_EQUIP, DONE }

var _phase := Phase.WAIT_BRIDGE
var _polls := 0
var _bridge = null


func _initialize() -> void:
	print(PREFIX + "EQUIP_PANEL_V0")


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
					print(PREFIX + "ERROR: SimBridge not found")
					_quit()

		Phase.WAIT_READY:
			var ready := false
			if _bridge.has_method("GetBridgeReadyV0"):
				ready = bool(_bridge.call("GetBridgeReadyV0"))
			else:
				ready = true
			if ready:
				_polls = 0
				_phase = Phase.CHECK_SLOTS
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					print(PREFIX + "ERROR: ready timeout")
					_quit()

		Phase.CHECK_SLOTS:
			# GetHeroShipLoadoutV0 is TryExecuteSafeRead (non-blocking) — retry until slots arrive.
			var slots = _bridge.call("GetHeroShipLoadoutV0")
			if typeof(slots) == TYPE_ARRAY and slots.size() >= 4:
				print(PREFIX + "SLOTS|PASS|count=%d" % slots.size())
				_bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_laser_mk1")
				_polls = 0
				_phase = Phase.WAIT_EQUIP
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					print(PREFIX + "FAIL|slot_count=%d expected>=4" % (slots.size() if typeof(slots) == TYPE_ARRAY else 0))
					_quit()

		Phase.WAIT_EQUIP:
			var slots2 = _bridge.call("GetHeroShipLoadoutV0")
			if typeof(slots2) == TYPE_ARRAY:
				for sv in slots2:
					if typeof(sv) == TYPE_DICTIONARY:
						if str(sv.get("slot_id", "")) == "slot_weapon_0" and str(sv.get("installed_module_id", "")) == "weapon_laser_mk1":
							print(PREFIX + "EQUIP|PASS|slot=slot_weapon_0|module=weapon_laser_mk1")
							print(PREFIX + "EQUIP_PANEL|PASS")
							_quit()
							return false

			_polls += 1
			if _polls >= MAX_POLLS:
				print(PREFIX + "FAIL: module not installed after timeout")
				_quit()

		Phase.DONE:
			pass

	return false


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
