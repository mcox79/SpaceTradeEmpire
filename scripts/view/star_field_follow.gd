extends GPUParticles3D

# Keeps the star field emission box centered on the active camera so stars
# surround the player at all times instead of clustering at world origin.

func _process(_delta: float) -> void:
	var vp := get_viewport()
	if vp == null:
		return
	var cam := vp.get_camera_3d()
	if cam:
		global_position = cam.global_position
