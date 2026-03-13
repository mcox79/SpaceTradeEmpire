extends Node3D
## GATE.S5.TRACTOR.VFX.001
## Tractor beam VFX — brief glowing energy line from player ship to loot target.
## Usage:
##   TractorBeam.spawn(parent, from_pos, to_pos)
## Auto-frees after duration (~0.5s).

const DURATION: float = 0.5
const BEAM_COLOR := Color(0.2, 0.8, 1.0, 0.8)
const BEAM_EMISSION := Color(0.2, 0.8, 1.0)
const BEAM_RADIUS: float = 0.06


## Spawn a tractor beam between two world positions.
## Adds itself as a child of `parent`.
static func spawn(parent: Node, from_pos: Vector3, to_pos: Vector3) -> Node3D:
	var beam := Node3D.new()
	beam.name = "TractorBeamVfx"

	var dist := from_pos.distance_to(to_pos)
	if dist < 0.01:
		beam.queue_free()
		return beam

	# Position at midpoint between source and target.
	var mid := (from_pos + to_pos) * 0.5
	beam.global_position = mid

	# Create cylinder mesh along beam axis.
	var mesh_inst := MeshInstance3D.new()
	mesh_inst.name = "BeamCylinder"
	mesh_inst.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	var cyl := CylinderMesh.new()
	cyl.top_radius = BEAM_RADIUS
	cyl.bottom_radius = BEAM_RADIUS
	cyl.height = dist
	mesh_inst.mesh = cyl

	# Glowing cyan material.
	var mat := StandardMaterial3D.new()
	mat.albedo_color = BEAM_COLOR
	mat.emission_enabled = true
	mat.emission = BEAM_EMISSION
	mat.emission_energy_multiplier = 3.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.no_depth_test = true
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mesh_inst.material_override = mat

	beam.add_child(mesh_inst)
	parent.add_child(beam)

	# Orient beam toward target.
	# CylinderMesh is Y-up by default. We look_at the target then rotate so the
	# cylinder's Y axis aligns with the from→to direction.
	var dir := (to_pos - from_pos).normalized()
	# Use look_at with a safe up vector (avoid parallel to dir).
	var up := Vector3.UP
	if abs(dir.dot(up)) > 0.99:
		up = Vector3.RIGHT
	beam.look_at(to_pos, up)
	mesh_inst.rotate_x(PI / 2.0)

	# Fade-out tween over duration, then auto-free.
	var tween := beam.create_tween()
	tween.tween_method(
		func(v: float):
			if is_instance_valid(mat):
				mat.albedo_color.a = BEAM_COLOR.a * v
				mat.emission_energy_multiplier = 3.0 * v,
		1.0, 0.0, DURATION
	)
	tween.tween_callback(func():
		if is_instance_valid(beam):
			beam.queue_free()
	)

	return beam
