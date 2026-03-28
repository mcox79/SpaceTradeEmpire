extends Node3D
class_name StationNameplate
## Station name Label3D + faction insignia diamond.

var _label: Label3D = null
var _insignia: MeshInstance3D = null


func setup(station_name: String, _faction_id: String, faction_color: Color) -> void:
	# Format "Star_N" → "Star N".
	var display_name: String = station_name.replace("_", " ")

	# --- Name label ---
	_label = Label3D.new()
	_label.name = "NameLabel"
	_label.text = display_name
	_label.font_size = 32
	_label.pixel_size = 0.01
	_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_label.position = Vector3(0, 2.5, 0)
	_label.modulate = Color.WHITE
	_label.outline_size = 4
	_label.outline_modulate = Color(0, 0, 0, 0.5)
	add_child(_label)

	# --- Faction insignia diamond ---
	_insignia = MeshInstance3D.new()
	_insignia.name = "FactionInsignia"
	var prism := PrismMesh.new()
	prism.size = Vector3(0.3, 0.3, 0.3)
	_insignia.mesh = prism
	_insignia.position = Vector3(0, 2.0, 0)

	var mat := StandardMaterial3D.new()
	mat.albedo_color = faction_color
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	_insignia.material_override = mat
	add_child(_insignia)


func _process(_delta: float) -> void:
	var cam: Camera3D = get_viewport().get_camera_3d()
	var dist: float = 100.0
	if cam:
		dist = global_position.distance_to(cam.global_position)

	# Fade between 30-70 units distance.
	var alpha: float = clampf(1.0 - (dist - 30.0) / 40.0, 0.0, 1.0)

	if _label:
		_label.modulate.a = alpha
	if _insignia:
		var mat := _insignia.material_override as StandardMaterial3D
		if mat:
			mat.albedo_color.a = alpha
