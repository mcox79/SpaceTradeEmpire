extends Node3D
class_name StationIdentity
## Per-faction color tinting + tier-based size scaling + detail meshes.
## Per station_visual_design_v0.md
## GATE.T46.STATION.NODE_MESH.001

const FACTION_COLORS := {
	"hegemony": Color(0.831, 0.627, 0.090),   # #D4A017
	"sovereignty": Color(0.290, 0.486, 0.710), # #4A7CB5
	"collective": Color(0.180, 0.545, 0.341),  # #2E8B57
	"dominion": Color(0.545, 0.145, 0.0),      # #8B2500
	"communion": Color(0.482, 0.247, 0.620),   # #7B3F9E
	"concord": Color(0.6, 0.6, 0.65),          # neutral gray-blue
	"chitin": Color(0.831, 0.627, 0.090),       # alias
	"valorin": Color(0.545, 0.145, 0.0),        # alias
	"weaver": Color(0.482, 0.247, 0.620),       # alias
}

## Tier thresholds and scale factors.
const TIER_OUTPOST_MAX := 2
const TIER_HUB_MAX := 4
const SCALE_OUTPOST := 0.6
const SCALE_HUB := 1.0
const SCALE_CAPITAL := 1.4
const FACTION_BLEND := 0.6  # 60% faction color, 40% original

var _faction_id: String = ""
var _industry_count: int = 0
var _market_breadth: int = 0
var _tier_name: String = "Outpost"
var _tier_scale: float = SCALE_OUTPOST
var _detail_nodes: Array[Node3D] = []


func setup(faction_id: String, industry_count: int, market_breadth: int) -> void:
	_faction_id = faction_id
	_industry_count = industry_count
	_market_breadth = market_breadth
	_classify_tier()


func _classify_tier() -> void:
	if _industry_count <= TIER_OUTPOST_MAX:
		_tier_name = "Outpost"
		_tier_scale = SCALE_OUTPOST
	elif _industry_count <= TIER_HUB_MAX:
		_tier_name = "Hub"
		_tier_scale = SCALE_HUB
	else:
		_tier_name = "Capital"
		_tier_scale = SCALE_CAPITAL


func apply_to_mesh(mesh: MeshInstance3D) -> void:
	if mesh == null:
		return

	# Scale based on tier.
	mesh.scale = Vector3.ONE * _tier_scale

	# Get or create material override.
	var mat: StandardMaterial3D = null
	if mesh.material_override is StandardMaterial3D:
		mat = mesh.material_override as StandardMaterial3D
	else:
		mat = StandardMaterial3D.new()
		mesh.material_override = mat

	# Blend faction color with existing albedo.
	var faction_color: Color = FACTION_COLORS.get(_faction_id, Color.WHITE)
	var original: Color = mat.albedo_color
	mat.albedo_color = original.lerp(faction_color, FACTION_BLEND)


## Add tier-appropriate detail meshes as children of the given parent node.
## Outpost: bare sphere (no extras). Hub: antenna array. Capital: orbital ring + antenna.
func add_detail_meshes(parent: Node3D, base_radius: float) -> void:
	_clear_detail_nodes()
	var faction_color: Color = FACTION_COLORS.get(_faction_id, Color.WHITE)

	if _tier_name == "Hub":
		_add_antenna_array(parent, base_radius, faction_color)
	elif _tier_name == "Capital":
		_add_orbital_ring(parent, base_radius, faction_color)
		_add_antenna_array(parent, base_radius, faction_color)


func _add_antenna_array(parent: Node3D, base_radius: float, color: Color) -> void:
	# Two antenna spires on top of the station sphere
	for i in range(2):
		var antenna := MeshInstance3D.new()
		var cyl := CylinderMesh.new()
		cyl.top_radius = base_radius * 0.05
		cyl.bottom_radius = base_radius * 0.08
		cyl.height = base_radius * 1.2
		antenna.mesh = cyl
		# Offset above sphere center, spread apart
		var x_offset: float = base_radius * 0.3 * (1.0 if i == 0 else -1.0)
		antenna.position = Vector3(x_offset, base_radius * 0.8, 0.0)
		var mat := StandardMaterial3D.new()
		mat.albedo_color = color.lerp(Color(0.3, 0.3, 0.35), 0.5)
		mat.metallic = 0.6
		mat.roughness = 0.4
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = 0.5
		antenna.material_override = mat
		parent.add_child(antenna)
		_detail_nodes.append(antenna)

	# Tip light on each antenna
	for i in range(2):
		var tip := MeshInstance3D.new()
		var sphere := SphereMesh.new()
		sphere.radius = base_radius * 0.08
		sphere.height = base_radius * 0.16
		tip.mesh = sphere
		var x_offset: float = base_radius * 0.3 * (1.0 if i == 0 else -1.0)
		tip.position = Vector3(x_offset, base_radius * 1.4, 0.0)
		var mat := StandardMaterial3D.new()
		mat.albedo_color = color
		mat.emission_enabled = true
		mat.emission = color
		mat.emission_energy_multiplier = 3.0
		tip.material_override = mat
		parent.add_child(tip)
		_detail_nodes.append(tip)


func _add_orbital_ring(parent: Node3D, base_radius: float, color: Color) -> void:
	var ring := MeshInstance3D.new()
	var torus := TorusMesh.new()
	var ring_radius: float = base_radius * 2.0
	torus.inner_radius = ring_radius - base_radius * 0.06
	torus.outer_radius = ring_radius + base_radius * 0.06
	ring.mesh = torus
	ring.position = Vector3.ZERO
	ring.rotate_x(PI / 2.0)
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color.lerp(Color(0.2, 0.2, 0.25), 0.4)
	mat.metallic = 0.5
	mat.roughness = 0.4
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = 1.2
	ring.material_override = mat
	parent.add_child(ring)
	_detail_nodes.append(ring)


func _clear_detail_nodes() -> void:
	for node in _detail_nodes:
		if is_instance_valid(node):
			node.queue_free()
	_detail_nodes.clear()


func get_tier_name() -> String:
	return _tier_name
