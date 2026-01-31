extends Node3D

var ship_meshes: Dictionary = {}
var ship_mat: StandardMaterial3D
var star_meshes: Dictionary = {}
var _heat_materials: Dictionary = {}

func _ready():
	await get_tree().process_frame
	var game_manager = get_node_or_null("/root/Main/GameManager")
	if game_manager and game_manager.sim:
		_draw_galaxy(game_manager.sim)

	ship_mat = StandardMaterial3D.new()
	ship_mat.albedo_color = Color(1.0, 0.1, 0.1)
	ship_mat.emission_enabled = true
	ship_mat.emission = Color(1.0, 0.0, 0.0)
	ship_mat.emission_energy_multiplier = 3.0

func _draw_galaxy(sim):
	for star in sim.galaxy_map.stars:
		var s = MeshInstance3D.new()
		s.mesh = SphereMesh.new()
		s.mesh.radius = 0.5
		s.mesh.height = 1.0
		s.position = star.pos
		var mat = StandardMaterial3D.new()
		mat.albedo_color = Color(0, 0.5, 1.0)
		mat.emission_enabled = true
		s.material_override = mat
		_heat_materials[star.id] = mat
		star_meshes[star.id] = s
		add_child(s)

	for lane in sim.galaxy_map.lanes:
		var mid = (lane.from + lane.to) / 2.0
		var l = MeshInstance3D.new()
		l.mesh = CylinderMesh.new()
		l.mesh.top_radius = 0.05
		l.mesh.bottom_radius = 0.05
		l.mesh.height = lane.from.distance_to(lane.to)
		l.position = mid
		l.look_at_from_position(mid, lane.to, Vector3.UP)
		l.rotate_object_local(Vector3.RIGHT, PI/2)
		add_child(l)

func _process(_delta):
	var game_manager = get_node_or_null("/root/Main/GameManager")
	if not game_manager or not game_manager.sim: return

	var sim = game_manager.sim

	# HEAT UPDATE
	for star_id in star_meshes.keys():
		var heat = sim.info.get_node_heat(star_id)
		var heat_factor = clamp(heat / 50.0, 0.0, 1.0)
		var target_color = Color(0, 0.5, 1.0).lerp(Color(1.0, 0.0, 0.0), heat_factor)
		var mat = _heat_materials[star_id]
		mat.albedo_color = target_color
		mat.emission = target_color
		mat.emission_energy_multiplier = 1.0 + (heat_factor * 2.0)

	# FLEET UPDATE
	for fleet in sim.active_fleets:
		if not ship_meshes.has(fleet.id):
			var mesh_inst = MeshInstance3D.new()
			mesh_inst.mesh = BoxMesh.new()
			mesh_inst.mesh.size = Vector3(1.0, 0.5, 2.0)
			mesh_inst.material_override = ship_mat
			add_child(mesh_inst)
			ship_meshes[fleet.id] = mesh_inst

		var visual_ship = ship_meshes[fleet.id]
		visual_ship.position = fleet.current_pos
		visual_ship.position.y += 2.0

		# SAFE ORIENTATION
		var target_look = fleet.to + Vector3(0, 2.0, 0)
		if visual_ship.position.distance_to(target_look) > 0.1:
			visual_ship.look_at(target_look, Vector3.UP)
