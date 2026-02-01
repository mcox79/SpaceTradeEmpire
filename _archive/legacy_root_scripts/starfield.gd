extends Node3D

func _ready():
	var rng = RandomNumberGenerator.new()
	# Create 500 stars for density
	for i in range(500):
		var star = MeshInstance3D.new()
		star.mesh = BoxMesh.new()
		star.mesh.size = Vector3(0.8, 0.8, 0.8) # Large enough to see
		
		# Create a glowing material
		var mat = StandardMaterial3D.new()
		mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		mat.albedo_color = Color(1, 1, 1) # Pure White
		star.mesh.material = mat
		
		# Spread them out WIDE
		var x = rng.randf_range(-300, 300)
		var z = rng.randf_range(-300, 300)
		var y = rng.randf_range(-40, -100) # Deep below player
		
		star.position = Vector3(x, y, z)
		add_child(star)
