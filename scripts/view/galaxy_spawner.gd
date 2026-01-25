extends Node3D

func _ready():
	await get_tree().process_frame
	print("[SPAWNER] Rendering Stars and Network Lanes...")
	var sim = GameManager.sim
	
	# Material for Stars
	var star_mat = StandardMaterial3D.new()
	star_mat.albedo_color = Color(1.0, 0.9, 0.5)
	star_mat.emission_enabled = true
	star_mat.emission = Color(1.0, 0.8, 0.2)
	star_mat.emission_energy_multiplier = 2.0
	
	# Material for Lanes (Subtle Blue)
	var lane_mat = StandardMaterial3D.new()
	lane_mat.albedo_color = Color(0.2, 0.4, 0.8, 0.5)
	lane_mat.emission_enabled = true
	lane_mat.emission = Color(0.1, 0.3, 0.6)
	lane_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA

	# 1. Spawn Stars
	for star in sim.galaxy_map.stars:
		var mesh_inst = MeshInstance3D.new()
		var sphere = SphereMesh.new()
		sphere.radius = 0.5; sphere.height = 1.0
		mesh_inst.mesh = sphere
		mesh_inst.material_override = star_mat
		mesh_inst.position = star.pos
		add_child(mesh_inst)

	# 2. Spawn Network Lanes
	for lane in sim.galaxy_map.lanes:
		var mid_point = (lane.from + lane.to) / 2.0
		var dist = lane.from.distance_to(lane.to)
		var mesh_inst = MeshInstance3D.new()
		var cylinder = CylinderMesh.new()
		cylinder.top_radius = 0.05; cylinder.bottom_radius = 0.05
		cylinder.height = dist
		mesh_inst.mesh = cylinder
		mesh_inst.material_override = lane_mat
		mesh_inst.position = mid_point
		# Orient the cylinder to point from A to B
		mesh_inst.look_at_from_position(mid_point, lane.to, Vector3.UP)
		mesh_inst.rotate_object_local(Vector3.RIGHT, PI/2)
		add_child(mesh_inst)

	print("[SPAWNER] Network rendering complete.")