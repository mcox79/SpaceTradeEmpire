extends Node3D
class_name AmbientShuttle
## Cosmetic shuttle that orbits a station. Spawned per traffic_level.
## economy_simulation_v0.md Category 1: Station Traffic

## Node ID this shuttle orbits around.
var _node_id: String = ""
## Orbital radius (semi-major axis).
var _orbit_radius: float = 2.0
## Faction tint applied to shuttle mesh.
var _faction_color: Color = Color.WHITE

## Elliptical orbit parameters.
var _orbit_speed: float = 0.5
var _orbit_phase: float = 0.0
## Eccentricity factor for elliptical path (semi-minor = radius * this).
const ORBIT_ECCENTRICITY: float = 0.7

## Internal refs.
var _mesh: MeshInstance3D = null
var _time: float = 0.0


## Initialize the shuttle with its orbit parameters and faction color.
func setup(node_id: String, orbit_radius: float, faction_color: Color) -> void:
	_node_id = node_id
	_orbit_radius = orbit_radius
	_faction_color = faction_color

	# Randomize per-instance so shuttles don't stack.
	_orbit_speed = randf_range(0.3, 0.8)
	_orbit_phase = randf_range(0.0, TAU)

	_build_mesh()


func _build_mesh() -> void:
	_mesh = MeshInstance3D.new()
	_mesh.name = "ShuttleMesh"
	var box := BoxMesh.new()
	box.size = Vector3(0.3, 0.1, 0.1)
	_mesh.mesh = box

	var mat := StandardMaterial3D.new()
	mat.albedo_color = _faction_color
	mat.emission_enabled = true
	mat.emission = _faction_color * 0.4
	mat.emission_energy_multiplier = 0.5
	_mesh.material_override = mat
	_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	add_child(_mesh)


func _process(delta: float) -> void:
	_time += delta
	var angle := _orbit_phase + _time * _orbit_speed
	var x := cos(angle) * _orbit_radius
	var z := sin(angle) * _orbit_radius * ORBIT_ECCENTRICITY
	position = Vector3(x, 0.0, z)

	# Face direction of travel.
	var next_angle := angle + 0.01
	var next_x := cos(next_angle) * _orbit_radius
	var next_z := sin(next_angle) * _orbit_radius * ORBIT_ECCENTRICITY
	var dir := Vector3(next_x - x, 0.0, next_z - z).normalized()
	if dir.length_squared() > 0.001:
		look_at(global_position + dir, Vector3.UP)
