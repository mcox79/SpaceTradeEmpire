extends SceneTree

# GATE.S1.VISUAL_POLISH.SCENE_PROOF.001
# Headless proof: verifies all visual polish gate artifacts exist in the scene tree.
# Checks: WorldEnvironment, StarField, EngineTrail, StationUpgrade, FleetMesh, HudHpBar, FleetAI.

const PREFIX := "VISUAL_SCENE_V0|"
const MAX_POLLS := 600  # 10 seconds at ~60fps

enum Phase {
	WAIT_BRIDGE,
	WAIT_READY,
	WAIT_LOCAL_SYSTEM,
	RUN_CHECKS,
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
				_phase = Phase.WAIT_LOCAL_SYSTEM
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					_fail("bridge_ready_timeout")

		Phase.WAIT_LOCAL_SYSTEM:
			# Wait for GalaxyView to spawn the local system (stations + fleets).
			# GalaxyView uses CallDeferred so it spawns one frame after bridge ready.
			# We check for at least one Station group node as the sentinel.
			var stations := get_nodes_in_group("Station")
			if stations.size() > 0:
				_polls = 0
				_phase = Phase.RUN_CHECKS
			else:
				_polls += 1
				if _polls >= MAX_POLLS:
					# Local system may not have spawned; run checks anyway so we report
					# accurate FAIL counts rather than timing out silently.
					_phase = Phase.RUN_CHECKS

		Phase.RUN_CHECKS:
			_run_checks()
			_phase = Phase.DONE

		Phase.DONE:
			pass

	return false


func _run_checks() -> void:
	var pass_count := 0
	var total := 7

	# --- CHECK 1: WorldEnvironment ---
	var world_env = root.get_node_or_null("Main/WorldEnvironment")
	var we_pass := world_env != null
	print("CHECK: WorldEnvironment ... " + ("PASS" if we_pass else "FAIL"))
	if we_pass:
		pass_count += 1

	# --- CHECK 2: StarField particles ---
	var starfield = root.get_node_or_null("Main/StarField")
	var sf_pass := starfield != null and starfield is GPUParticles3D
	print("CHECK: StarField ... " + ("PASS" if sf_pass else "FAIL"))
	if sf_pass:
		pass_count += 1

	# --- CHECK 3: EngineTrail on player ship ---
	# Player is RigidBody3D; EngineTrail is at ShipVisual/Engine/EngineTrail.
	var engine_trail_pass := false
	var player_node = root.get_node_or_null("Main/Player")
	if player_node == null:
		# Player may be added to group "Player"; try group lookup.
		var players := get_nodes_in_group("Player")
		if players.size() > 0:
			player_node = players[0]
	if player_node != null:
		# Check direct path first, then fall back to recursive search.
		var trail = player_node.get_node_or_null("ShipVisual/Engine/EngineTrail")
		if trail == null:
			trail = _find_child_by_name(player_node, "EngineTrail")
		engine_trail_pass = trail != null and trail is GPUParticles3D
	print("CHECK: EngineTrail ... " + ("PASS" if engine_trail_pass else "FAIL"))
	if engine_trail_pass:
		pass_count += 1

	# --- CHECK 4: Station has upgraded mesh (StationVisual container with Hub/Ring/Accent) ---
	var station_upgrade_pass := false
	var station_nodes := get_nodes_in_group("Station")
	if station_nodes.size() > 0:
		var sta = station_nodes[0]
		# GalaxyView.cs SpawnStationV0 creates StationVisual > StationHub, StationRing, StationAccent.
		# station.tscn also has Hub, Ring, AccentBand under StationVisual.
		var sta_visual = sta.get_node_or_null("StationVisual")
		if sta_visual != null:
			var has_hub = (sta_visual.get_node_or_null("StationHub") != null
				or sta_visual.get_node_or_null("Hub") != null)
			var has_ring = (sta_visual.get_node_or_null("StationRing") != null
				or sta_visual.get_node_or_null("Ring") != null)
			var has_accent = (sta_visual.get_node_or_null("StationAccent") != null
				or sta_visual.get_node_or_null("AccentBand") != null)
			station_upgrade_pass = has_hub or has_ring or has_accent
		else:
			# Fallback: look for Hub/Ring/AccentBand directly as children.
			var has_hub = sta.get_node_or_null("Hub") != null
			var has_ring = sta.get_node_or_null("Ring") != null
			var has_accent = sta.get_node_or_null("AccentBand") != null
			station_upgrade_pass = has_hub or has_ring or has_accent
	print("CHECK: StationUpgrade ... " + ("PASS" if station_upgrade_pass else "FAIL"))
	if station_upgrade_pass:
		pass_count += 1

	# --- CHECK 5: Fleet markers have ship mesh (FleetHull or FleetNose child) ---
	# GalaxyView.cs CreateFleetMarkerV0 adds FleetHull and FleetNose as direct children.
	# Fleet markers are in group "FleetShip".
	var fleet_mesh_pass := false
	var fleet_nodes := get_nodes_in_group("FleetShip")
	if fleet_nodes.size() > 0:
		var fleet = fleet_nodes[0]
		var has_hull = fleet.get_node_or_null("FleetHull") != null
		var has_nose = fleet.get_node_or_null("FleetNose") != null
		fleet_mesh_pass = has_hull or has_nose
	print("CHECK: FleetMesh ... " + ("PASS" if fleet_mesh_pass else "FAIL"))
	if fleet_mesh_pass:
		pass_count += 1

	# --- CHECK 6: HUD has HullBar and ShieldBar ProgressBar children ---
	# In Playable_Prototype.tscn: HUD is a CanvasLayer at Main/HUD (sibling of other CanvasLayers).
	var hud_pass := false
	var hud = root.get_node_or_null("Main/HUD")
	if hud == null:
		# Try alternate path (HUD may be direct child of root).
		hud = root.get_node_or_null("HUD")
	if hud != null:
		var hull_bar = _find_child_by_name(hud, "HullBar")
		var shield_bar = _find_child_by_name(hud, "ShieldBar")
		hud_pass = hull_bar != null and shield_bar != null
	print("CHECK: HudHpBar ... " + ("PASS" if hud_pass else "FAIL"))
	if hud_pass:
		pass_count += 1

	# --- CHECK 7: Fleet AI script attached to fleet marker ---
	# GalaxyView.cs loads fleet_ai.gd and sets it as the script on the fleet root node.
	# fleet_ai.gd exports patrol_speed: the property is present on nodes with the script.
	var fleet_ai_pass := false
	if fleet_nodes.size() > 0:
		var fleet = fleet_nodes[0]
		# Check for an @export var from fleet_ai.gd as proof the script is attached.
		fleet_ai_pass = fleet.get("patrol_speed") != null
	print("CHECK: FleetAI ... " + ("PASS" if fleet_ai_pass else "FAIL"))
	if fleet_ai_pass:
		pass_count += 1

	# --- Summary ---
	print("---")
	if pass_count == total:
		print("PASS: visual_scene_v0 (%d/%d checks passed)" % [pass_count, total])
	else:
		print("FAIL: visual_scene_v0 (%d/%d checks passed)" % [pass_count, total])

	_quit()


# Recursive depth-first child search by node name.
func _find_child_by_name(node: Node, target_name: String) -> Node:
	for child in node.get_children():
		if child.name == target_name:
			return child
		var found = _find_child_by_name(child, target_name)
		if found != null:
			return found
	return null


func _fail(msg: String) -> void:
	print(PREFIX + "FAIL|" + msg)
	_phase = Phase.DONE
	_quit()


func _quit() -> void:
	_phase = Phase.DONE
	if _bridge != null and _bridge.has_method("StopSimV0"):
		_bridge.call("StopSimV0")
	quit(0)
