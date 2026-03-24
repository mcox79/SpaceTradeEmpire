extends Node3D
class_name TargetHighlight
## GATE.T52.COMBAT.TARGET_HIGHLIGHT.001
## Pulsing OmniLight3D glow ring on the engaged combat target NPC.
## Usage:
##   TargetHighlight.apply(npc_node)   — add/refresh highlight on target
##   TargetHighlight.clear(npc_node)   — remove highlight from target
##   TargetHighlight.clear_all(tree)   — remove all active highlights
## Auto-pulses between warm orange and cyan over 2.5s cycle.

const HIGHLIGHT_NAME := "TargetHighlightVfx"
## Pulse period (seconds for one full orange→cyan→orange cycle).
const PULSE_PERIOD: float = 2.5
## Light range scaled for camera altitude ~2500u.
const LIGHT_RANGE: float = 120.0
const LIGHT_ENERGY_MIN: float = 2.0
const LIGHT_ENERGY_MAX: float = 5.0

## Color endpoints for the pulse.
const COLOR_A := Color(1.0, 0.6, 0.15)  # Warm orange
const COLOR_B := Color(0.2, 0.9, 0.9)   # Cyan

var _elapsed: float = 0.0
var _light: OmniLight3D = null
var _ring_mesh: MeshInstance3D = null


## Apply (or refresh) a target highlight on the given NPC node.
## If one already exists, resets the pulse timer. Idempotent.
static func apply(target: Node3D) -> void:
	if target == null or not is_instance_valid(target):
		return
	# Already highlighted — just reset elapsed.
	var existing = target.get_node_or_null(HIGHLIGHT_NAME)
	if existing and is_instance_valid(existing):
		existing._elapsed = 0.0
		return
	# Create new highlight.
	var highlight := TargetHighlight.new()
	highlight.name = HIGHLIGHT_NAME
	target.add_child(highlight)


## Remove highlight from a specific NPC node.
static func clear(target: Node3D) -> void:
	if target == null or not is_instance_valid(target):
		return
	var existing = target.get_node_or_null(HIGHLIGHT_NAME)
	if existing and is_instance_valid(existing):
		existing.queue_free()


## Remove all active target highlights in the scene tree.
static func clear_all(tree: SceneTree) -> void:
	if tree == null:
		return
	for node in tree.get_nodes_in_group("TargetHighlightActive"):
		if is_instance_valid(node):
			node.queue_free()


func _ready() -> void:
	add_to_group("TargetHighlightActive")

	# OmniLight3D — the main glow.
	_light = OmniLight3D.new()
	_light.name = "HighlightGlow"
	_light.light_color = COLOR_A
	_light.light_energy = LIGHT_ENERGY_MAX
	_light.omni_range = LIGHT_RANGE
	_light.omni_attenuation = 1.2
	add_child(_light)

	# Visible glow ring mesh (torus approximation via flattened sphere).
	_ring_mesh = MeshInstance3D.new()
	_ring_mesh.name = "GlowRing"
	var sphere := SphereMesh.new()
	sphere.radius = 35.0
	sphere.height = 6.0  # Flattened into a disc/ring shape
	sphere.radial_segments = 24
	sphere.rings = 4
	_ring_mesh.mesh = sphere
	_ring_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(COLOR_A.r, COLOR_A.g, COLOR_A.b, 0.35)
	mat.emission_enabled = true
	mat.emission = COLOR_A
	mat.emission_energy_multiplier = 3.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.no_depth_test = true
	mat.render_priority = 10
	_ring_mesh.material_override = mat
	add_child(_ring_mesh)


func _process(delta: float) -> void:
	_elapsed += delta
	# Ping-pong t between 0 and 1 over PULSE_PERIOD.
	var raw_t: float = fmod(_elapsed, PULSE_PERIOD) / PULSE_PERIOD
	var t: float = 1.0 - abs(2.0 * raw_t - 1.0)  # Triangle wave 0→1→0

	var color := COLOR_A.lerp(COLOR_B, t)
	var energy := lerpf(LIGHT_ENERGY_MIN, LIGHT_ENERGY_MAX, 1.0 - t)

	if _light and is_instance_valid(_light):
		_light.light_color = color
		_light.light_energy = energy

	if _ring_mesh and is_instance_valid(_ring_mesh):
		var mat: StandardMaterial3D = _ring_mesh.material_override as StandardMaterial3D
		if mat:
			mat.albedo_color = Color(color.r, color.g, color.b, 0.2 + 0.25 * (1.0 - t))
			mat.emission = color
			mat.emission_energy_multiplier = 2.0 + 3.0 * (1.0 - t)
