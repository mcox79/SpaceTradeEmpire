## EPIC.X.EXPERIENCE_PROOF.V0 — Component 1
## Captures "what the player sees" as structured Dictionary.
## Instantiated by scenario scripts; NOT a test script itself.

var _tree: SceneTree = null
var _bridge = null


func init_v0(tree: SceneTree) -> void:
	_tree = tree
	_bridge = tree.root.get_node_or_null("SimBridge")


func capture_full_report_v0() -> Dictionary:
	return {
		"observer_version": 1,
		"tick": _get_tick(),
		"hud": capture_hud_v0(),
		"player": capture_player_state_v0(),
		"scene": capture_scene_structure_v0(),
		"materials": capture_materials_v0(),
		"particles": capture_particles_v0(),
		"audio": capture_audio_v0(),
		"camera": capture_camera_v0(),
		"galaxy": capture_galaxy_v0(),
		"local_system": capture_local_system_v0(),
		"ui_panels": capture_ui_panels_v0(),
	}


func capture_hud_v0() -> Dictionary:
	var hud = _find_hud()
	if hud == null:
		return {"found": false}
	var labels: Array = []
	var bars: Array = []
	var buttons: Array = []
	_walk_hud_node(hud, labels, bars, buttons)
	return {
		"found": true,
		"labels": labels,
		"bars": bars,
		"buttons": buttons,
	}


func capture_player_state_v0() -> Dictionary:
	if _bridge == null:
		return {"bridge": false}
	var ps: Dictionary = {}
	if _bridge.has_method("GetPlayerStateV0"):
		ps = _bridge.call("GetPlayerStateV0")
	var hp: Dictionary = {}
	if _bridge.has_method("GetFleetCombatHpV0"):
		hp = _bridge.call("GetFleetCombatHpV0", "fleet_trader_1")
	var ship_state := ""
	if _bridge.has_method("GetPlayerShipStateNameV0"):
		ship_state = str(_bridge.call("GetPlayerShipStateNameV0"))
	return {
		"bridge": true,
		"credits": int(ps.get("credits", 0)),
		"cargo_count": int(ps.get("cargo_count", 0)),
		"current_node_id": str(ps.get("current_node_id", "")),
		"ship_state": ship_state,
		"hull": int(hp.get("hull", 0)),
		"hull_max": int(hp.get("hull_max", 0)),
		"shield": int(hp.get("shield", 0)),
		"shield_max": int(hp.get("shield_max", 0)),
	}


func capture_scene_structure_v0() -> Dictionary:
	if _tree == null:
		return {}
	var groups := ["Station", "FleetShip", "Player", "LaneLine", "Bullet"]
	var counts := {}
	for g in groups:
		counts[g] = _tree.get_nodes_in_group(g).size()
	var has_world_env := _tree.root.get_node_or_null("Main/WorldEnvironment") != null
	var has_starfield := _tree.root.get_node_or_null("Main/StarField") != null
	var total_nodes := _count_nodes(_tree.root)
	return {
		"group_counts": counts,
		"has_world_environment": has_world_env,
		"has_starfield": has_starfield,
		"total_node_count": total_nodes,
	}


func capture_materials_v0() -> Array:
	var results: Array = []
	if _tree == null:
		return results
	var meshes := _find_all_of_type(_tree.root, "MeshInstance3D")
	for m in meshes:
		var mi: MeshInstance3D = m as MeshInstance3D
		if mi == null:
			continue
		var mat = mi.get_active_material(0)
		var entry := {"name": str(mi.name), "path": str(mi.get_path())}
		if mat is StandardMaterial3D:
			var sm: StandardMaterial3D = mat as StandardMaterial3D
			entry["albedo_color"] = _color_str(sm.albedo_color)
			entry["emission_enabled"] = sm.emission_enabled
			if sm.emission_enabled:
				entry["emission_color"] = _color_str(sm.emission)
				entry["emission_energy"] = sm.emission_energy_multiplier
		results.append(entry)
	return results


func capture_particles_v0() -> Array:
	var results: Array = []
	if _tree == null:
		return results
	var particles := _find_all_of_type(_tree.root, "GPUParticles3D")
	for p in particles:
		var gp: GPUParticles3D = p as GPUParticles3D
		if gp == null:
			continue
		results.append({
			"name": str(gp.name),
			"path": str(gp.get_path()),
			"emitting": gp.emitting,
			"amount": gp.amount,
			"visible": gp.visible,
		})
	return results


func capture_audio_v0() -> Array:
	var results: Array = []
	if _tree == null:
		return results
	var players := _find_all_of_type(_tree.root, "AudioStreamPlayer")
	for a in players:
		var asp: AudioStreamPlayer = a as AudioStreamPlayer
		if asp == null:
			continue
		results.append({
			"name": str(asp.name),
			"playing": asp.playing,
			"volume_db": asp.volume_db,
			"has_stream": asp.stream != null,
		})
	# Also check 3D audio
	var players3d := _find_all_of_type(_tree.root, "AudioStreamPlayer3D")
	for a in players3d:
		var asp3: AudioStreamPlayer3D = a as AudioStreamPlayer3D
		if asp3 == null:
			continue
		results.append({
			"name": str(asp3.name),
			"playing": asp3.playing,
			"volume_db": asp3.volume_db,
			"has_stream": asp3.stream != null,
			"is_3d": true,
		})
	return results


func capture_camera_v0() -> Dictionary:
	if _tree == null:
		return {}
	var cam := _tree.root.get_viewport().get_camera_3d()
	if cam == null:
		return {"found": false}
	return {
		"found": true,
		"name": str(cam.name),
		"fov": cam.fov,
		"position": _vec3_str(cam.global_position),
		"is_current": cam.current,
	}


func capture_galaxy_v0() -> Dictionary:
	if _bridge == null:
		return {"bridge": false}
	if not _bridge.has_method("GetGalaxySnapshotV0"):
		return {"bridge": true, "method_missing": true}
	var snap: Dictionary = _bridge.call("GetGalaxySnapshotV0")
	var node_count: int = 0
	var lane_count: int = 0
	var nodes = snap.get("system_nodes", null)
	if nodes is Array:
		node_count = nodes.size()
	var lanes = snap.get("lane_edges", null)
	if lanes is Array:
		lane_count = lanes.size()
	return {
		"bridge": true,
		"node_count": node_count,
		"lane_count": lane_count,
		"tick": int(snap.get("tick", 0)),
	}


func capture_local_system_v0() -> Dictionary:
	if _bridge == null:
		return {"bridge": false}
	var ps: Dictionary = {}
	if _bridge.has_method("GetPlayerStateV0"):
		ps = _bridge.call("GetPlayerStateV0")
	var node_id: String = str(ps.get("current_node_id", ""))
	if node_id.is_empty():
		return {"bridge": true, "node_id": ""}
	if not _bridge.has_method("GetSystemSnapshotV0"):
		return {"bridge": true, "node_id": node_id, "method_missing": true}
	var sys: Dictionary = _bridge.call("GetSystemSnapshotV0", node_id)
	var station_count: int = 0
	var fleet_count: int = 0
	var stations = sys.get("stations", null)
	if stations is Array:
		station_count = stations.size()
	var fleets = sys.get("fleets", null)
	if fleets is Array:
		fleet_count = fleets.size()
	return {
		"bridge": true,
		"node_id": node_id,
		"station_count": station_count,
		"fleet_count": fleet_count,
	}


func capture_ui_panels_v0() -> Dictionary:
	var result := {}
	var hud = _find_hud()
	if hud == null:
		return {"hud_found": false}
	# Check known panels
	var panel_names := ["GameOverPanel", "PauseMenuPanel", "MissionPanel", "CombatLabel"]
	for pn in panel_names:
		var node = _find_child_by_name(hud, pn)
		if node != null:
			result[pn] = {"found": true, "visible": node.visible}
		else:
			result[pn] = {"found": false}
	result["hud_found"] = true
	return result


# --- JSON file output ---

func write_report_json_v0(report: Dictionary, output_path: String) -> void:
	var json_str := JSON.stringify(report, "\t")
	var dir_path := output_path.get_base_dir()
	DirAccess.make_dir_recursive_absolute(dir_path)
	var f := FileAccess.open(output_path, FileAccess.WRITE)
	if f != null:
		f.store_string(json_str)
		f.close()
		print("EXPV0|REPORT_SAVED|" + output_path)
	else:
		print("EXPV0|REPORT_SAVE_FAILED|" + output_path)


# --- Helpers ---

func _get_tick() -> int:
	if _bridge != null and _bridge.has_method("GetSimTickV0"):
		return int(_bridge.call("GetSimTickV0"))
	return -1


func _find_hud() -> Node:
	if _tree == null:
		return null
	var hud = _tree.root.get_node_or_null("Main/HUD")
	if hud == null:
		hud = _tree.root.get_node_or_null("HUD")
	return hud


func _walk_hud_node(node: Node, labels: Array, bars: Array, buttons: Array) -> void:
	if node is Label:
		var lbl: Label = node as Label
		labels.append({
			"name": str(lbl.name),
			"text": lbl.text,
			"visible": lbl.visible,
		})
	elif node is ProgressBar:
		var bar: ProgressBar = node as ProgressBar
		bars.append({
			"name": str(bar.name),
			"value": bar.value,
			"max_value": bar.max_value,
			"visible": bar.visible,
		})
	elif node is Button:
		var btn: Button = node as Button
		buttons.append({
			"name": str(btn.name),
			"text": btn.text,
			"visible": btn.visible,
		})
	for child in node.get_children():
		_walk_hud_node(child, labels, bars, buttons)


func _find_child_by_name(node: Node, target_name: String) -> Node:
	for child in node.get_children():
		if child.name == target_name:
			return child
		var found = _find_child_by_name(child, target_name)
		if found != null:
			return found
	return null


func _find_all_of_type(node: Node, type_name: String) -> Array:
	var result: Array = []
	if node.get_class() == type_name:
		result.append(node)
	for child in node.get_children():
		result.append_array(_find_all_of_type(child, type_name))
	return result


func _count_nodes(node: Node) -> int:
	var count := 1
	for child in node.get_children():
		count += _count_nodes(child)
	return count


func _color_str(c: Color) -> String:
	return "(%0.2f, %0.2f, %0.2f, %0.2f)" % [c.r, c.g, c.b, c.a]


func _vec3_str(v: Vector3) -> String:
	return "(%0.1f, %0.1f, %0.1f)" % [v.x, v.y, v.z]
