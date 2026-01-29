extends Node3D

# NEUTRALIZED PLAYER CONTROLLER
# For Slice 1, the Player is a passive camera anchor attached to the Fleet.
# We strip the physics logic to prevent conflicts with SimCore.

@export var visual_scale: float = 0.35

func _ready():
add_to_group("player")
# Hide self to avoid double-rendering (GalaxyView handles the fleet mesh)
visible = false 

func _process(_delta):
# OPTIONAL: Snap to fleet position if we want camera to follow
# For now, we leave it static or controlled by 'prototype_camera.gd'
pass

# Stub methods to prevent crashes if other systems call them
func set_input_enabled(_val): pass
func receive_payment(_val): pass
func add_cargo(_id, _amt): return true
func remove_cargo(_id, _amt): return true