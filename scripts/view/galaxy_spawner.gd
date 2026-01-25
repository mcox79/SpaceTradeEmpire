extends Node3D

var ship_meshes: Dictionary = {}
var ship_mat: StandardMaterial3D

func _ready():
	await get_tree().process_frame
	var sim = GameManager.sim
	_draw_galaxy(sim)
	
	# Setup Ship Material (Bright Red for contrast)
	ship_mat = StandardMaterial3D.new()
	ship_mat.albedo_color = Color(1.0, 0.1, 0.1)
	ship_mat.emission_enabled = true
	ship_mat.emission = Color(1.0, 0.0, 0.0)
	ship_mat.emission_energy_multiplier = 3.0

func _draw_galaxy(sim):
	for star in sim.galaxy_map.stars:
		var s = MeshInstance3D.new()
		s.mesh = SphereMesh.new(); s.mesh.radius = 0.5; s.mesh.height = 1.0
		s.position = star.pos
		add_child(s)
	for lane in sim.galaxy_map.lanes:
		var mid = (lane.from + lane.to) / 2.0
		var l = MeshInstance3D.new()
		l.mesh = CylinderMesh.new(); l.mesh.top_radius=0.05; l.mesh.bottom_radius=0.05; l.mesh.height = lane.from.distance_to(lane.to)
		l.position = mid
		l.look_at_from_position(mid, lane.to, Vector3.UP)
		l.rotate_object_local(Vector3.RIGHT, PI/2)
		add_child(l)

# FIXED: Prefixed _delta to satisfy strict compiler warnings
func _process(_delta):
	var sim = GameManager.sim
	if not sim: return
	
	# 1. READ: Sync View with Sim State
	for fleet in sim.active_fleets:
		if not ship_meshes.has(fleet.id):
			var mesh_inst = MeshInstance3D.new()
			mesh_inst.mesh = BoxMesh.new()
			# FIXED: Upscaled 600% for Strategic Camera visibility
			mesh_inst.mesh.size = Vector3(3.0, 3.0, 6.0) 
			mesh_inst.material_override = ship_mat
			add_child(mesh_inst)
			ship_meshes[fleet.id] = mesh_inst
		
		# 2. RENDER: Interpolate position along the spline
		var visual_ship = ship_meshes[fleet.id]
		visual_ship.position = fleet.from.lerp(fleet.to, fleet.progress)
		
		# FIXED: Offset Y-axis by 2.0 meters to prevent Z-fighting with stars
		visual_ship.position.y += 2.0 
		visual_ship.look_at(fleet.to, Vector3.UP)
