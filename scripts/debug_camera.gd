extends Node

func _ready():
	await get_tree().process_frame
	print("Has PhantomCameraManager: ", has_node("/root/PhantomCameraManager"))

	var cam := get_viewport().get_camera_3d()
	if cam:
		print("ACTIVE CAMERA: ", cam.name)
		print("PATH: ", cam.get_path())
		print("SCRIPT: ", cam.get_script())
		print("GROUPS: ", cam.get_groups())
	else:
		print("ACTIVE CAMERA: <none>")
