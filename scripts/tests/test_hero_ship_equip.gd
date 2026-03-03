extends SceneTree

# Headless proof for GATE.S4.MODULE_MODEL.EQUIP.001
# Equips weapon_laser_mk1 to slot_weapon_0, then verifies via GetHeroShipLoadoutV0.
# Emits: EQUIP|PASS|slot_id=slot_weapon_0|installed_module_id=weapon_laser_mk1
#    or: EQUIP|FAIL|reason=<msg>

func _initialize():
	var packed = load("res://scenes/playable_prototype.tscn")
	var root = packed.instantiate()
	root.name = "Main"
	get_root().add_child(root)

	await process_frame
	await process_frame

	var bridge = get_root().get_node_or_null("SimBridge")
	if bridge == null:
		print("EQUIP|FAIL|reason=no_bridge")
		quit()
		return

	if not bridge.has_method("DispatchEquipModuleV0"):
		print("EQUIP|FAIL|reason=no_DispatchEquipModuleV0")
		quit()
		return

	# Dispatch equip command
	var tick_before: int = bridge.call("GetSimTickV0")
	bridge.call("DispatchEquipModuleV0", "slot_weapon_0", "weapon_laser_mk1")

	# Wait for sim to process the command
	for _i in range(500):
		await process_frame
		var t: int = bridge.call("GetSimTickV0")
		if t >= tick_before + 2:
			break

	# Read loadout
	var loadout = bridge.call("GetHeroShipLoadoutV0")
	var found_slot_id := ""
	var found_module_id := ""

	if typeof(loadout) == TYPE_ARRAY:
		for entry in loadout:
			if typeof(entry) == TYPE_DICTIONARY:
				if str(entry.get("slot_id", "")) == "slot_weapon_0":
					found_slot_id = str(entry.get("slot_id", ""))
					found_module_id = str(entry.get("installed_module_id", ""))
					break

	if found_slot_id != "slot_weapon_0":
		print("EQUIP|FAIL|reason=slot_weapon_0_not_in_loadout")
		bridge.call("StopSimV0")
		quit()
		return

	if found_module_id != "weapon_laser_mk1":
		print("EQUIP|FAIL|reason=installed_module_id=%s_expected=weapon_laser_mk1" % found_module_id)
		bridge.call("StopSimV0")
		quit()
		return

	print("EQUIP|PASS|slot_id=%s|installed_module_id=%s" % [found_slot_id, found_module_id])

	bridge.call("StopSimV0")
	quit()
