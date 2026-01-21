extends Camera3D

# SETTINGS: 40 meters up, 25 meters back
var offset = Vector3(0, 40, 25)
var target_node

func _ready():
	# 1. THE EJECT BUTTON
	# This tells the engine: "Ignore my parent's rotation. Use Global coordinates."
	set_as_top_level(true)
	
	# 2. FIND THE SHIP
	# If we are inside the ship, our parent is the ship.
	var parent = get_parent()
	if parent.name == "Player":
		target_node = parent
	else:
		# If we are neighbors, find it by name
		target_node = parent.get_node_or_null("Player")

	if target_node:
		# Teleport to starting position
		global_position = target_node.global_position + offset
		look_at(target_node.global_position)

func _physics_process(_delta):
	if target_node:
		# 3. CHASE THE SHIP
		# Since we are "Top Level" now, we must manually update our position every frame.
		global_position = target_node.global_position + offset
