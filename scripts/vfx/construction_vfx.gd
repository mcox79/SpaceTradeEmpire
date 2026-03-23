extends Node3D
class_name ConstructionVFX
## Welding sparks + scaffolding at active megaproject nodes.
## Per vfx_visual_roadmap_v0.md construction visuals.

var _particles: GPUParticles3D = null
var _scaffolding: MeshInstance3D = null
var _stage: int = 0
var _total_stages: int = 1


func setup(stage: int, total_stages: int) -> void:
	_stage = stage
	_total_stages = max(total_stages, 1)
	_create_sparks()
	_create_scaffolding()
	_update_spark_count()


func _create_sparks() -> void:
	_particles = GPUParticles3D.new()
	_particles.name = "WeldingSparks"
	_particles.emitting = true
	_particles.amount = 8
	_particles.lifetime = 0.5
	_particles.one_shot = false
	_particles.explosiveness = 0.6

	var mat := ParticleProcessMaterial.new()
	mat.direction = Vector3(0, 1, 0)
	mat.spread = 30.0
	mat.initial_velocity_min = 2.0
	mat.initial_velocity_max = 5.0
	mat.gravity = Vector3(0, -4.0, 0)
	mat.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_SPHERE
	mat.emission_sphere_radius = 1.5
	mat.scale_min = 0.05
	mat.scale_max = 0.12
	mat.color = Color(1.0, 0.8, 0.3)
	# Ramp from orange-white to white-yellow over lifetime.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 0.8, 0.3))
	gradient.set_color(1, Color(1.0, 1.0, 0.9, 0.0))
	var tex := GradientTexture1D.new()
	tex.gradient = gradient
	mat.color_ramp = tex
	_particles.process_material = mat

	# Small quad mesh for each particle.
	var quad := QuadMesh.new()
	quad.size = Vector2(0.08, 0.08)
	var quad_mat := StandardMaterial3D.new()
	quad_mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	quad_mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	quad_mat.vertex_color_use_as_albedo = true
	quad_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	quad.material = quad_mat
	_particles.draw_pass_1 = quad

	add_child(_particles)


func _create_scaffolding() -> void:
	_scaffolding = MeshInstance3D.new()
	_scaffolding.name = "Scaffolding"

	# Build a wireframe box from line segments using ImmediateMesh.
	var im := ImmediateMesh.new()
	var s: float = 1.0  # half-extent (2x2x2 box)

	# 4 vertical corner lines.
	var corners: Array[Vector3] = [
		Vector3(-s, -s, -s), Vector3(-s, s, -s),
		Vector3(s, -s, -s), Vector3(s, s, -s),
		Vector3(s, -s, s), Vector3(s, s, s),
		Vector3(-s, -s, s), Vector3(-s, s, s),
	]

	im.surface_begin(Mesh.PRIMITIVE_LINES)
	for i in range(0, corners.size(), 2):
		im.surface_add_vertex(corners[i])
		im.surface_add_vertex(corners[i + 1])
	# Top ring.
	im.surface_add_vertex(Vector3(-s, s, -s))
	im.surface_add_vertex(Vector3(s, s, -s))
	im.surface_add_vertex(Vector3(s, s, -s))
	im.surface_add_vertex(Vector3(s, s, s))
	im.surface_add_vertex(Vector3(s, s, s))
	im.surface_add_vertex(Vector3(-s, s, s))
	im.surface_add_vertex(Vector3(-s, s, s))
	im.surface_add_vertex(Vector3(-s, s, -s))
	# Bottom ring.
	im.surface_add_vertex(Vector3(-s, -s, -s))
	im.surface_add_vertex(Vector3(s, -s, -s))
	im.surface_add_vertex(Vector3(s, -s, -s))
	im.surface_add_vertex(Vector3(s, -s, s))
	im.surface_add_vertex(Vector3(s, -s, s))
	im.surface_add_vertex(Vector3(-s, -s, s))
	im.surface_add_vertex(Vector3(-s, -s, s))
	im.surface_add_vertex(Vector3(-s, -s, -s))
	im.surface_end()

	_scaffolding.mesh = im

	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.3, 0.7, 1.0, 0.4)
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.no_depth_test = true
	_scaffolding.material_override = mat

	add_child(_scaffolding)


func _update_spark_count() -> void:
	if _particles:
		_particles.amount = 4 + (_stage * 4)


func update_progress(stage: int, total_stages: int) -> void:
	_stage = stage
	_total_stages = max(total_stages, 1)
	_update_spark_count()


func set_active(active: bool) -> void:
	visible = active
	if _particles:
		_particles.emitting = active
