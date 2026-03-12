extends MeshInstance3D
## Renders a procedural Milky Way band and nearby galaxy patches on a sky sphere.
## Seed-driven: each procedural universe gets a unique sky.

@export var sky_radius: float = 3500.0
@export var shader_path: String = "res://scripts/view/galactic_sky.gdshader"
## Universe seed — set from GameManager or manually. Drives all noise offsets.
@export var universe_seed: float = 42.0

var _mat: ShaderMaterial

func _ready() -> void:
	var sphere := SphereMesh.new()
	sphere.radius = sky_radius
	sphere.height = sky_radius * 2.0
	sphere.radial_segments = 128
	sphere.rings = 64
	sphere.is_hemisphere = false
	mesh = sphere

	var shader = load(shader_path) as Shader
	if shader == null:
		push_warning("[GalacticSky] Could not load shader at: " + shader_path)
		visible = false
		return
	_mat = ShaderMaterial.new()
	_mat.shader = shader
	_mat.set_shader_parameter("universe_seed", universe_seed)
	material_override = _mat

	cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	# Try to pick up the game seed from GameManager.
	_try_sync_seed()

func _try_sync_seed() -> void:
	var gm = _find_game_manager()
	if gm == null:
		return
	# GameManager exposes rng_seed or galaxy_seed depending on version.
	for prop in ["rng_seed", "galaxy_seed", "world_seed"]:
		var val = gm.get(prop)
		if val != null and val is int:
			set_universe_seed(float(val))
			return

func set_universe_seed(seed_val: float) -> void:
	universe_seed = seed_val
	if _mat:
		_mat.set_shader_parameter("universe_seed", universe_seed)

func _process(_delta: float) -> void:
	var cam := get_viewport().get_camera_3d()
	if cam:
		global_position = cam.global_position

func _find_game_manager():
	var parent = get_parent()
	if parent:
		var gm = parent.get_node_or_null("GameManager")
		if gm:
			return gm
	var tree = get_tree()
	if tree == null:
		return null
	var gm_auto = tree.root.get_node_or_null("GameManager")
	if gm_auto:
		return gm_auto
	return null
