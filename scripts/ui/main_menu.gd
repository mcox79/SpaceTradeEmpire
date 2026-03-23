extends Control

# GATE.S7.MAIN_MENU.SCENE.001: Main menu with New Voyage, Continue, Settings, Quit.
# GATE.S9.MENU_ATMOSPHERE.STARFIELD.001: Parallax starfield shader background.
# GATE.S9.MENU_ATMOSPHERE.TITLE.001: Title fade-in + rotating Precursor subtitle.
# GATE.S9.MENU_ATMOSPHERE.AUDIO.001: Menu audio timing + silence palette.
# GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001: Adaptive foreground silhouette.
# GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001: Galaxy gen loading screen.
# Programmatic UI -- no .tscn dependency for layout.

const PLAYABLE_SCENE := "res://scenes/playable_prototype.tscn"

# --- Precursor subtitle pool (GATE.S9.MENU_ATMOSPHERE.TITLE.001) ---
const PRECURSOR_FRAGMENTS: Array[String] = [
	"The lanes remember what stars forget.",
	"Between the gates, silence trades in secrets.",
	"The void is patient. Commerce is not.",
	"Dead stars still cast long shadows on the ledger.",
	"A ship without cargo is a question without an answer.",
	"The first trade route was drawn in starlight.",
	"In the hum of the gate, a forgotten language stirs.",
	"Empires rise on credit lines and courage.",
	"Somewhere beyond the rift, prices are better.",
	"Every station remembers its best customer.",
	"The difference between profit and loss is one jump.",
	"Someone built the gates. Nobody asks why.",
]

# --- Audio resource paths (GATE.S9.MENU_ATMOSPHERE.AUDIO.001) ---
# Placeholder paths -- actual .ogg/.wav files may not exist yet.
const AUDIO_SINGLE_NOTE := "res://assets/audio/menu_single_note.ogg"
const AUDIO_AMBIENT_DRONE := "res://assets/audio/menu_ambient_drone.ogg"
const AUDIO_THEME := "res://assets/audio/menu_theme.ogg"

# --- Node references ---
var _btn_continue: Button
var _btn_new_voyage: Button
var _btn_milestones: Button
var _btn_credits: Button
var _btn_settings: Button
var _btn_quit: Button
var _save_card: PanelContainer
# GATE.S9.MENU_ATMOSPHERE.TITLE.001: Title + subtitle refs.
var _title_label: Label
var _subtitle_label: Label

# GATE.S9.MENU_ATMOSPHERE.AUDIO.001: Audio players.
var _audio_note: AudioStreamPlayer
var _audio_drone: AudioStreamPlayer
var _audio_theme: AudioStreamPlayer

# GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001: Silhouette viewport.
var _silhouette_container: SubViewportContainer
var _silhouette_viewport: SubViewport
var _silhouette_mesh: MeshInstance3D
var _silhouette_camera: Camera3D
var _silhouette_rotation_speed: float = 0.3

# Menu VBox (needed for galaxy gen overlay positioning).
var _menu_vbox: VBoxContainer
var _starfield_bg: ColorRect

# Save metadata for silhouette decision.
var _has_save: bool = false
var _save_meta: Dictionary = {}

# GATE.T47.SAVE.SLOT_MANAGEMENT.001: Save slot management state.
var _save_slot_panel: PanelContainer
var _save_slot_list_vbox: VBoxContainer
var _save_slot_no_saves_label: Label
var _save_slot_visible: bool = false

# GATE.T47.SAVE.RECOVERY_UX.001: Corruption tracking per slot path.
var _corrupted_slots: Dictionary = {}  # path -> reason string

# Confirmation/rename dialog refs.
var _confirm_overlay: ColorRect
var _rename_overlay: ColorRect
var _rename_edit: LineEdit
var _pending_delete_path: String = ""
var _pending_rename_path: String = ""


func _ready() -> void:
	# ---- Starfield background (GATE.S9.MENU_ATMOSPHERE.STARFIELD.001) ----
	_starfield_bg = ColorRect.new()
	_starfield_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	var shader := load("res://scripts/view/starfield_menu.gdshader") as Shader
	if shader:
		var mat := ShaderMaterial.new()
		mat.shader = shader
		_starfield_bg.material = mat
		_starfield_bg.color = Color(1.0, 1.0, 1.0, 1.0)  # White so shader colors show.
	else:
		# Fallback dark background if shader not found.
		_starfield_bg.color = Color(0.02, 0.02, 0.06, 1.0)
	add_child(_starfield_bg)

	# ---- Silhouette viewport layer (GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001) ----
	_build_silhouette_layer()

	# ---- Menu UI layer ----
	# Center container for menu items.
	_menu_vbox = VBoxContainer.new()
	_menu_vbox.set_anchors_preset(Control.PRESET_CENTER)
	_menu_vbox.grow_horizontal = Control.GROW_DIRECTION_BOTH
	_menu_vbox.grow_vertical = Control.GROW_DIRECTION_BOTH
	_menu_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_menu_vbox.add_theme_constant_override("separation", 16)
	add_child(_menu_vbox)

	# ---- Title with fade-in (GATE.S9.MENU_ATMOSPHERE.TITLE.001) ----
	_title_label = Label.new()
	_title_label.text = "SPACE TRADE EMPIRE"
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", 54)
	_title_label.add_theme_color_override("font_color", Color(0.85, 0.9, 1.0))
	_title_label.modulate.a = 0.0  # Start invisible for fade-in.
	_menu_vbox.add_child(_title_label)

	# Rotating Precursor subtitle.
	_subtitle_label = Label.new()
	var frag_index: int = randi() % PRECURSOR_FRAGMENTS.size()
	_subtitle_label.text = PRECURSOR_FRAGMENTS[frag_index]
	_subtitle_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_subtitle_label.add_theme_font_size_override("font_size", 16)
	_subtitle_label.add_theme_color_override("font_color", Color(0.5, 0.55, 0.7, 0.8))
	_subtitle_label.modulate.a = 0.0  # Fades in after title.
	_subtitle_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_subtitle_label.custom_minimum_size = Vector2(500, 0)
	_menu_vbox.add_child(_subtitle_label)

	# Title fade-in tween: 2s fade for title, then 1s delay, then subtitle.
	var title_tween := create_tween()
	title_tween.tween_property(_title_label, "modulate:a", 1.0, 2.0).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	title_tween.tween_interval(0.5)
	title_tween.tween_property(_subtitle_label, "modulate:a", 1.0, 1.5).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)

	# Spacer.
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 40)
	_menu_vbox.add_child(spacer)

	# Buttons.
	_btn_continue = _make_button("Continue", _menu_vbox)
	_btn_new_voyage = _make_button("New Voyage", _menu_vbox)
	_btn_milestones = _make_button("Milestones", _menu_vbox)
	_btn_credits = _make_button("Credits", _menu_vbox)
	_btn_settings = _make_button("Settings", _menu_vbox)
	_btn_quit = _make_button("Quit", _menu_vbox)

	_btn_continue.pressed.connect(_on_continue)
	_btn_new_voyage.pressed.connect(_on_new_voyage)
	_btn_milestones.pressed.connect(_on_milestones)
	_btn_credits.pressed.connect(_on_credits)
	_btn_settings.pressed.connect(_on_settings)
	_btn_quit.pressed.connect(_on_quit)

	# Check if save file exists for Continue button and show metadata card.
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetSaveSlotMetadataV0"):
		_save_meta = bridge.call("GetSaveSlotMetadataV0", 1)
		_has_save = _save_meta.get("exists", false)
		if _has_save:
			_build_save_card(_menu_vbox, _save_meta)
	# GATE.T47.SAVE.SLOT_MANAGEMENT.001: Also check for save files on disk.
	if not _has_save:
		_has_save = _any_save_files_exist()
	_btn_continue.disabled = not _has_save
	_btn_milestones.disabled = not _has_save

	# Tell GameManager we're on the main menu.
	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.set("_on_main_menu", true)

	# ---- Audio setup (GATE.S9.MENU_ATMOSPHERE.AUDIO.001) ----
	_setup_audio()


# =============================================================================
# GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001: 3D silhouette via SubViewport
# =============================================================================

func _build_silhouette_layer() -> void:
	_silhouette_container = SubViewportContainer.new()
	_silhouette_container.set_anchors_preset(Control.PRESET_FULL_RECT)
	_silhouette_container.stretch = true
	_silhouette_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_silhouette_container)

	_silhouette_viewport = SubViewport.new()
	_silhouette_viewport.size = Vector2i(640, 480)
	_silhouette_viewport.transparent_bg = true
	_silhouette_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	_silhouette_container.add_child(_silhouette_viewport)

	# Camera looking at the mesh.
	_silhouette_camera = Camera3D.new()
	_silhouette_camera.position = Vector3(0.0, 0.0, 4.0)
	_silhouette_camera.look_at_from_position(Vector3(0.0, 0.0, 4.0), Vector3.ZERO)
	_silhouette_camera.fov = 40.0
	_silhouette_viewport.add_child(_silhouette_camera)

	# Backlight -- dim rim light behind the silhouette for edge definition.
	var back_light := DirectionalLight3D.new()
	back_light.rotation_degrees = Vector3(10.0, 180.0, 0.0)
	back_light.light_energy = 0.4
	back_light.light_color = Color(0.3, 0.4, 0.7)
	_silhouette_viewport.add_child(back_light)

	# Faint fill from front -- just enough to see edges.
	var fill_light := DirectionalLight3D.new()
	fill_light.rotation_degrees = Vector3(-15.0, 20.0, 0.0)
	fill_light.light_energy = 0.15
	fill_light.light_color = Color(0.2, 0.25, 0.4)
	_silhouette_viewport.add_child(fill_light)

	# Choose mesh based on game state.
	_silhouette_mesh = MeshInstance3D.new()
	_silhouette_mesh.position = Vector3(0.0, -0.5, 0.0)
	var mesh_material := StandardMaterial3D.new()
	mesh_material.albedo_color = Color(0.03, 0.04, 0.08)  # Near-black for silhouette.
	mesh_material.roughness = 1.0
	mesh_material.metallic = 0.0

	if _has_save:
		# Mid-campaign or completed: show a ship-like shape.
		# Use a prism (triangular) as ship stand-in.
		var ship_mesh := PrismMesh.new()
		ship_mesh.size = Vector3(1.0, 0.4, 2.5)
		_silhouette_mesh.mesh = ship_mesh
	else:
		# No saves: show gate structure (box as placeholder).
		var gate_mesh := BoxMesh.new()
		gate_mesh.size = Vector3(2.0, 3.0, 0.3)
		_silhouette_mesh.mesh = gate_mesh

	_silhouette_mesh.set_surface_override_material(0, mesh_material)
	_silhouette_viewport.add_child(_silhouette_mesh)


# =============================================================================
# GATE.S9.MENU_ATMOSPHERE.AUDIO.001: Menu audio with timing
# =============================================================================

func _setup_audio() -> void:
	_audio_note = AudioStreamPlayer.new()
	_audio_note.bus = "Master"
	add_child(_audio_note)

	_audio_drone = AudioStreamPlayer.new()
	_audio_drone.bus = "Master"
	_audio_drone.volume_db = -20.0
	add_child(_audio_drone)

	_audio_theme = AudioStreamPlayer.new()
	_audio_theme.bus = "Master"
	_audio_theme.volume_db = -40.0  # Start silent for fade-in.
	add_child(_audio_theme)

	# Load streams (gracefully handle missing files).
	var note_stream = _try_load_audio(AUDIO_SINGLE_NOTE)
	var drone_stream = _try_load_audio(AUDIO_AMBIENT_DRONE)
	var theme_stream = _try_load_audio(AUDIO_THEME)

	if note_stream:
		_audio_note.stream = note_stream
	if drone_stream:
		_audio_drone.stream = drone_stream
	if theme_stream:
		_audio_theme.stream = theme_stream

	# Determine first-launch vs returning.
	var is_first_launch := not FileAccess.file_exists("user://settings.json")

	if is_first_launch:
		# First-launch sequence: 2s silence -> single note -> drone swell -> theme fade.
		_start_first_launch_audio_sequence()
	else:
		# Returning player: quick 0.5s fade-in to theme.
		_start_returning_audio_sequence()


func _try_load_audio(path: String) -> AudioStream:
	if not ResourceLoader.exists(path):
		return null
	var res = load(path)
	if res is AudioStream:
		return res
	return null


func _start_first_launch_audio_sequence() -> void:
	var audio_tween := create_tween()

	# 2 seconds of silence.
	audio_tween.tween_interval(2.0)

	# Single note.
	audio_tween.tween_callback(func():
		if _audio_note.stream:
			_audio_note.volume_db = -10.0
			_audio_note.play()
	)

	# After 1.5s, start ambient drone swell.
	audio_tween.tween_interval(1.5)
	audio_tween.tween_callback(func():
		if _audio_drone.stream:
			_audio_drone.volume_db = -30.0
			_audio_drone.play()
	)
	# Swell drone volume over 2s.
	audio_tween.tween_property(_audio_drone, "volume_db", -8.0, 2.0).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)

	# Fade in theme over 3s.
	audio_tween.tween_callback(func():
		if _audio_theme.stream:
			_audio_theme.volume_db = -40.0
			_audio_theme.play()
	)
	audio_tween.tween_property(_audio_theme, "volume_db", -6.0, 3.0).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_QUAD)


func _start_returning_audio_sequence() -> void:
	var audio_tween := create_tween()

	# Quick theme start with 0.5s fade-in.
	audio_tween.tween_callback(func():
		if _audio_theme.stream:
			_audio_theme.volume_db = -40.0
			_audio_theme.play()
		if _audio_drone.stream:
			_audio_drone.volume_db = -20.0
			_audio_drone.play()
	)
	audio_tween.tween_property(_audio_theme, "volume_db", -6.0, 0.5).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	audio_tween.parallel().tween_property(_audio_drone, "volume_db", -10.0, 0.5).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)



# =============================================================================
# Process loop
# =============================================================================

func _process(delta: float) -> void:
	# Rotate silhouette mesh (GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001).
	if _silhouette_mesh:
		_silhouette_mesh.rotate_y(delta * _silhouette_rotation_speed)


# =============================================================================
# UI builders (unchanged from original)
# =============================================================================

func _make_button(text: String, parent: Node) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(280, 50)
	btn.add_theme_font_size_override("font_size", 22)
	# Semi-transparent button style for atmosphere.
	var btn_style := StyleBoxFlat.new()
	btn_style.bg_color = Color(0.06, 0.08, 0.16, 0.7)
	btn_style.border_color = Color(0.2, 0.3, 0.5, 0.5)
	btn_style.set_border_width_all(1)
	btn_style.set_corner_radius_all(6)
	btn_style.set_content_margin_all(8)
	btn.add_theme_stylebox_override("normal", btn_style)
	var btn_hover := StyleBoxFlat.new()
	btn_hover.bg_color = Color(0.1, 0.14, 0.28, 0.85)
	btn_hover.border_color = Color(0.3, 0.45, 0.7, 0.8)
	btn_hover.set_border_width_all(1)
	btn_hover.set_corner_radius_all(6)
	btn_hover.set_content_margin_all(8)
	btn.add_theme_stylebox_override("hover", btn_hover)
	var btn_pressed := StyleBoxFlat.new()
	btn_pressed.bg_color = Color(0.15, 0.2, 0.35, 0.9)
	btn_pressed.border_color = Color(0.35, 0.5, 0.8, 0.9)
	btn_pressed.set_border_width_all(1)
	btn_pressed.set_corner_radius_all(6)
	btn_pressed.set_content_margin_all(8)
	btn.add_theme_stylebox_override("pressed", btn_pressed)
	parent.add_child(btn)
	return btn


func _build_save_card(parent_vbox: VBoxContainer, meta: Dictionary) -> void:
	# Insert save metadata preview card right after the Continue button.
	_save_card = PanelContainer.new()
	_save_card.custom_minimum_size = Vector2(280, 0)

	# Dark semi-transparent panel style.
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.08, 0.08, 0.15, 0.85)
	style.border_color = Color(0.25, 0.3, 0.5, 0.6)
	style.set_border_width_all(1)
	style.set_corner_radius_all(6)
	style.set_content_margin_all(12)
	_save_card.add_theme_stylebox_override("panel", style)

	var card_vbox := VBoxContainer.new()
	card_vbox.add_theme_constant_override("separation", 4)
	_save_card.add_child(card_vbox)

	# Saved date row.
	var timestamp: String = meta.get("timestamp", "")
	if timestamp != "":
		var date_label := Label.new()
		date_label.text = "Saved: %s" % timestamp
		date_label.add_theme_font_size_override("font_size", 14)
		date_label.add_theme_color_override("font_color", Color(0.65, 0.7, 0.8))
		date_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		card_vbox.add_child(date_label)

	# Credits row.
	var credits: int = meta.get("credits", 0)
	if credits > 0:
		var credits_label := Label.new()
		credits_label.text = "Credits: %s" % _format_credits(credits)
		credits_label.add_theme_font_size_override("font_size", 14)
		credits_label.add_theme_color_override("font_color", Color(0.9, 0.85, 0.4))
		credits_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		card_vbox.add_child(credits_label)

	# System/location row.
	var system_name: String = meta.get("system_name", "")
	if system_name != "":
		var loc_label := Label.new()
		loc_label.text = "Location: %s" % system_name
		loc_label.add_theme_font_size_override("font_size", 14)
		loc_label.add_theme_color_override("font_color", Color(0.5, 0.75, 0.9))
		loc_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		card_vbox.add_child(loc_label)

	# Insert the card after the Continue button.
	var continue_idx := _btn_continue.get_index()
	parent_vbox.add_child(_save_card)
	parent_vbox.move_child(_save_card, continue_idx + 1)


func _format_credits(amount: int) -> String:
	# Format with comma separators: 1234567 -> "1,234,567".
	var s := str(amount)
	var result := ""
	var count := 0
	for i in range(s.length() - 1, -1, -1):
		if count > 0 and count % 3 == 0:
			result = "," + result
		result = s[i] + result
		count += 1
	return result


# =============================================================================
# GATE.T47.SAVE.SLOT_MANAGEMENT.001 + GATE.T47.SAVE.RECOVERY_UX.001
# Save slot list panel — built programmatically, shown over menu.
# =============================================================================

const SAVE_FILE_PATTERNS: Array[String] = ["quicksave.json", "autosave.json"]
const SAVE_SLOT_PREFIX := "save_slot_"

func _any_save_files_exist() -> bool:
	var dir := DirAccess.open("user://")
	if not dir:
		return false
	dir.list_dir_begin()
	var f := dir.get_next()
	while f != "":
		if not dir.current_is_dir() and f.ends_with(".json"):
			for pattern in SAVE_FILE_PATTERNS:
				if f == pattern:
					dir.list_dir_end()
					return true
			if f.begins_with(SAVE_SLOT_PREFIX):
				dir.list_dir_end()
				return true
		f = dir.get_next()
	dir.list_dir_end()
	return false


func _build_save_slot_panel() -> void:
	if _save_slot_panel:
		_save_slot_panel.queue_free()
		_save_slot_panel = null

	_save_slot_panel = PanelContainer.new()
	_save_slot_panel.custom_minimum_size = Vector2(520, 400)
	_save_slot_panel.set_anchors_preset(Control.PRESET_CENTER)
	_save_slot_panel.grow_horizontal = Control.GROW_DIRECTION_BOTH
	_save_slot_panel.grow_vertical = Control.GROW_DIRECTION_BOTH

	var panel_style := StyleBoxFlat.new()
	panel_style.bg_color = Color(0.04, 0.06, 0.12, 0.96)
	panel_style.border_color = Color(0.2, 0.35, 0.6, 0.8)
	panel_style.set_border_width_all(2)
	panel_style.set_corner_radius_all(8)
	panel_style.set_content_margin_all(16)
	_save_slot_panel.add_theme_stylebox_override("panel", panel_style)

	var outer_vbox := VBoxContainer.new()
	outer_vbox.add_theme_constant_override("separation", 12)
	_save_slot_panel.add_child(outer_vbox)

	# Header row with title + close button.
	var header_hbox := HBoxContainer.new()
	outer_vbox.add_child(header_hbox)
	var header_label := Label.new()
	header_label.text = "Save Files"
	header_label.add_theme_font_size_override("font_size", 22)
	header_label.add_theme_color_override("font_color", Color(0.4, 0.85, 1.0))
	header_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header_hbox.add_child(header_label)
	var close_btn := Button.new()
	close_btn.text = "X"
	close_btn.custom_minimum_size = Vector2(36, 36)
	close_btn.add_theme_font_size_override("font_size", 16)
	close_btn.pressed.connect(_close_save_slot_panel)
	header_hbox.add_child(close_btn)

	# Scrollable list area.
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.custom_minimum_size = Vector2(0, 300)
	outer_vbox.add_child(scroll)

	_save_slot_list_vbox = VBoxContainer.new()
	_save_slot_list_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_save_slot_list_vbox.add_theme_constant_override("separation", 6)
	scroll.add_child(_save_slot_list_vbox)

	add_child(_save_slot_panel)
	_save_slot_visible = true

	_populate_save_slot_list()


func _close_save_slot_panel() -> void:
	if _save_slot_panel:
		_save_slot_panel.queue_free()
		_save_slot_panel = null
	_save_slot_visible = false


func _populate_save_slot_list() -> void:
	if not _save_slot_list_vbox:
		return
	# Clear existing entries.
	for child in _save_slot_list_vbox.get_children():
		child.queue_free()
	_corrupted_slots.clear()

	var save_files: Array[Dictionary] = _scan_save_files()

	if save_files.is_empty():
		_save_slot_no_saves_label = Label.new()
		_save_slot_no_saves_label.text = "No save files found."
		_save_slot_no_saves_label.add_theme_font_size_override("font_size", 16)
		_save_slot_no_saves_label.add_theme_color_override("font_color", Color(0.55, 0.55, 0.65))
		_save_slot_no_saves_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		_save_slot_list_vbox.add_child(_save_slot_no_saves_label)
		return

	# Sort: autosave/quicksave first, then by modification time descending.
	save_files.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		var a_auto: bool = a.get("is_auto", false)
		var b_auto: bool = b.get("is_auto", false)
		if a_auto != b_auto:
			return a_auto  # auto saves first
		return a.get("modified", 0) > b.get("modified", 0)
	)

	for entry in save_files:
		_build_slot_entry(entry)


func _scan_save_files() -> Array[Dictionary]:
	var results: Array[Dictionary] = []
	var user_dir := DirAccess.open("user://")
	if not user_dir:
		return results

	user_dir.list_dir_begin()
	var file_name := user_dir.get_next()
	while file_name != "":
		if not user_dir.current_is_dir() and file_name.ends_with(".json"):
			var is_save := false
			var is_auto := false
			for pattern in SAVE_FILE_PATTERNS:
				if file_name == pattern:
					is_save = true
					is_auto = true
					break
			if not is_save and file_name.begins_with(SAVE_SLOT_PREFIX) and file_name.ends_with(".json"):
				is_save = true

			if is_save:
				var full_path := "user://" + file_name
				var mod_time: int = FileAccess.get_modified_time(full_path)
				var file_size: int = 0
				var f := FileAccess.open(full_path, FileAccess.READ)
				if f:
					file_size = f.get_length()
					f.close()

				results.append({
					"file_name": file_name,
					"path": full_path,
					"modified": mod_time,
					"size": file_size,
					"is_auto": is_auto,
				})
		file_name = user_dir.get_next()
	user_dir.list_dir_end()
	return results


func _build_slot_entry(entry: Dictionary) -> void:
	var path: String = entry.get("path", "")
	var file_name: String = entry.get("file_name", "")
	var is_auto: bool = entry.get("is_auto", false)
	var mod_time: int = entry.get("modified", 0)
	var file_size: int = entry.get("size", 0)

	# GATE.T47.SAVE.RECOVERY_UX.001: Check integrity.
	var is_corrupted := false
	var corruption_reason := ""
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("GetSaveIntegrityV0"):
		var integrity_result = bridge.call("GetSaveIntegrityV0", file_name)
		if integrity_result is Dictionary:
			if not integrity_result.get("is_valid", true):
				is_corrupted = true
				corruption_reason = integrity_result.get("reason", "Unknown corruption")
	if not is_corrupted:
		# Fallback: basic JSON parse check.
		var f := FileAccess.open(path, FileAccess.READ)
		if f:
			var content := f.get_as_text()
			f.close()
			if content.is_empty():
				is_corrupted = true
				corruption_reason = "File is empty"
			else:
				var json := JSON.new()
				var parse_err := json.parse(content)
				if parse_err != OK:
					is_corrupted = true
					corruption_reason = "Invalid JSON: " + json.get_error_message()
		else:
			is_corrupted = true
			corruption_reason = "Cannot open file"

	if is_corrupted:
		_corrupted_slots[path] = corruption_reason

	# Build row container.
	var row := PanelContainer.new()
	var row_style := StyleBoxFlat.new()
	if is_corrupted:
		row_style.bg_color = Color(0.18, 0.05, 0.05, 0.85)
		row_style.border_color = Color(0.8, 0.2, 0.2, 0.6)
	else:
		row_style.bg_color = Color(0.06, 0.08, 0.14, 0.8)
		row_style.border_color = Color(0.2, 0.3, 0.5, 0.4)
	row_style.set_border_width_all(1)
	row_style.set_corner_radius_all(4)
	row_style.set_content_margin_all(8)
	row.add_theme_stylebox_override("panel", row_style)

	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 8)
	row.add_child(hbox)

	# Info column.
	var info_vbox := VBoxContainer.new()
	info_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	info_vbox.add_theme_constant_override("separation", 2)
	hbox.add_child(info_vbox)

	# Name line.
	var display_name := file_name.get_basename()
	var name_prefix := ""
	if is_auto:
		name_prefix = "[Auto] "
	if is_corrupted:
		name_prefix = "[!] " + name_prefix
	var name_label := Label.new()
	name_label.text = name_prefix + display_name
	name_label.add_theme_font_size_override("font_size", 16)
	if is_corrupted:
		name_label.add_theme_color_override("font_color", Color(1.0, 0.4, 0.4))
	elif is_auto:
		name_label.add_theme_color_override("font_color", Color(0.4, 0.85, 1.0))
	else:
		name_label.add_theme_color_override("font_color", Color(0.85, 0.85, 0.9))
	info_vbox.add_child(name_label)

	# Date + size line.
	var date_str := _format_unix_timestamp(mod_time) if mod_time > 0 else "Unknown date"
	var size_str := _format_file_size(file_size)
	var meta_label := Label.new()
	meta_label.text = "%s  |  %s" % [date_str, size_str]
	meta_label.add_theme_font_size_override("font_size", 13)
	meta_label.add_theme_color_override("font_color", Color(0.55, 0.6, 0.7))
	info_vbox.add_child(meta_label)

	# Bridge metadata (captain, system, playtime) if available.
	if not is_corrupted and bridge and bridge.has_method("GetSaveMetadataV0"):
		var slot_meta = bridge.call("GetSaveMetadataV0", file_name)
		if slot_meta is Dictionary:
			var extra_parts: Array[String] = []
			var captain: String = slot_meta.get("captain_name", "")
			if captain != "":
				extra_parts.append("Captain: %s" % captain)
			var sys_name: String = slot_meta.get("system_name", "")
			if sys_name != "":
				extra_parts.append("System: %s" % sys_name)
			var playtime: String = slot_meta.get("playtime", "")
			if playtime != "":
				extra_parts.append("Time: %s" % playtime)
			if not extra_parts.is_empty():
				var extra_label := Label.new()
				extra_label.text = "  ".join(extra_parts)
				extra_label.add_theme_font_size_override("font_size", 13)
				extra_label.add_theme_color_override("font_color", Color(0.5, 0.75, 0.9))
				info_vbox.add_child(extra_label)

	# Corruption reason tooltip/label.
	if is_corrupted and corruption_reason != "":
		var reason_label := Label.new()
		reason_label.text = corruption_reason
		reason_label.add_theme_font_size_override("font_size", 12)
		reason_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.3, 0.8))
		info_vbox.add_child(reason_label)

	# Button column.
	var btn_vbox := VBoxContainer.new()
	btn_vbox.add_theme_constant_override("separation", 4)
	hbox.add_child(btn_vbox)

	if is_corrupted:
		# GATE.T47.SAVE.RECOVERY_UX.001: Corrupted — Try Load Anyway + Delete only.
		var try_btn := _make_slot_button("Try Load", Color(1.0, 0.6, 0.2))
		try_btn.pressed.connect(_on_slot_load.bind(path))
		btn_vbox.add_child(try_btn)
	else:
		var load_btn := _make_slot_button("Load", Color(0.4, 0.85, 1.0))
		load_btn.pressed.connect(_on_slot_load.bind(path))
		btn_vbox.add_child(load_btn)

		if not is_auto:
			var rename_btn := _make_slot_button("Rename", Color(0.7, 0.7, 0.78))
			rename_btn.pressed.connect(_on_slot_rename_begin.bind(path, file_name))
			btn_vbox.add_child(rename_btn)

	var delete_btn := _make_slot_button("Delete", Color(1.0, 0.3, 0.3))
	delete_btn.pressed.connect(_on_slot_delete_begin.bind(path, file_name))
	btn_vbox.add_child(delete_btn)

	_save_slot_list_vbox.add_child(row)


func _make_slot_button(text: String, color: Color) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(90, 30)
	btn.add_theme_font_size_override("font_size", 13)
	var style := StyleBoxFlat.new()
	style.bg_color = Color(color.r * 0.15, color.g * 0.15, color.b * 0.15, 0.8)
	style.border_color = Color(color.r, color.g, color.b, 0.5)
	style.set_border_width_all(1)
	style.set_corner_radius_all(4)
	style.set_content_margin_all(4)
	btn.add_theme_stylebox_override("normal", style)
	var hover_style := StyleBoxFlat.new()
	hover_style.bg_color = Color(color.r * 0.25, color.g * 0.25, color.b * 0.25, 0.9)
	hover_style.border_color = Color(color.r, color.g, color.b, 0.8)
	hover_style.set_border_width_all(1)
	hover_style.set_corner_radius_all(4)
	hover_style.set_content_margin_all(4)
	btn.add_theme_stylebox_override("hover", hover_style)
	return btn


func _format_unix_timestamp(unix: int) -> String:
	var dt := Time.get_datetime_dict_from_unix_time(unix)
	return "%04d-%02d-%02d %02d:%02d" % [dt.get("year", 0), dt.get("month", 0), dt.get("day", 0), dt.get("hour", 0), dt.get("minute", 0)]


func _format_file_size(bytes: int) -> String:
	if bytes < 1024:
		return "%d B" % bytes
	elif bytes < 1048576:
		return "%.1f KB" % (bytes / 1024.0)
	else:
		return "%.1f MB" % (bytes / 1048576.0)


# --- Slot actions ---

func _on_slot_load(path: String) -> void:
	_close_save_slot_panel()
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("RequestLoadFileV0"):
		bridge.call("RequestLoadFileV0", path)
	elif bridge and bridge.has_method("RequestLoad"):
		bridge.call("RequestLoad")
	_transition_to_game()


func _on_slot_delete_begin(path: String, file_name: String) -> void:
	_pending_delete_path = path
	_show_confirm_dialog("Delete save '%s'?\nThis cannot be undone." % file_name.get_basename())


func _show_confirm_dialog(message: String) -> void:
	if _confirm_overlay:
		_confirm_overlay.queue_free()
	_confirm_overlay = ColorRect.new()
	_confirm_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_confirm_overlay.color = Color(0.0, 0.0, 0.0, 0.6)
	add_child(_confirm_overlay)

	var dialog_panel := PanelContainer.new()
	dialog_panel.set_anchors_preset(Control.PRESET_CENTER)
	dialog_panel.grow_horizontal = Control.GROW_DIRECTION_BOTH
	dialog_panel.grow_vertical = Control.GROW_DIRECTION_BOTH
	dialog_panel.custom_minimum_size = Vector2(380, 0)
	var ds := StyleBoxFlat.new()
	ds.bg_color = Color(0.06, 0.08, 0.16, 0.96)
	ds.border_color = Color(0.8, 0.2, 0.2, 0.7)
	ds.set_border_width_all(2)
	ds.set_corner_radius_all(8)
	ds.set_content_margin_all(20)
	dialog_panel.add_theme_stylebox_override("panel", ds)
	_confirm_overlay.add_child(dialog_panel)

	var dvbox := VBoxContainer.new()
	dvbox.add_theme_constant_override("separation", 16)
	dialog_panel.add_child(dvbox)

	var msg_label := Label.new()
	msg_label.text = message
	msg_label.add_theme_font_size_override("font_size", 16)
	msg_label.add_theme_color_override("font_color", Color(0.9, 0.85, 0.8))
	msg_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	msg_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	dvbox.add_child(msg_label)

	var btn_hbox := HBoxContainer.new()
	btn_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_hbox.add_theme_constant_override("separation", 16)
	dvbox.add_child(btn_hbox)

	var cancel_btn := _make_slot_button("Cancel", Color(0.7, 0.7, 0.78))
	cancel_btn.custom_minimum_size = Vector2(120, 36)
	cancel_btn.pressed.connect(_on_confirm_cancel)
	btn_hbox.add_child(cancel_btn)

	var confirm_btn := _make_slot_button("Delete", Color(1.0, 0.3, 0.3))
	confirm_btn.custom_minimum_size = Vector2(120, 36)
	confirm_btn.pressed.connect(_on_confirm_delete)
	btn_hbox.add_child(confirm_btn)


func _on_confirm_cancel() -> void:
	_pending_delete_path = ""
	if _confirm_overlay:
		_confirm_overlay.queue_free()
		_confirm_overlay = null


func _on_confirm_delete() -> void:
	if _pending_delete_path != "":
		DirAccess.remove_absolute(_pending_delete_path)
		_pending_delete_path = ""
	if _confirm_overlay:
		_confirm_overlay.queue_free()
		_confirm_overlay = null
	_populate_save_slot_list()


func _on_slot_rename_begin(path: String, file_name: String) -> void:
	_pending_rename_path = path
	_show_rename_dialog(file_name.get_basename())


func _show_rename_dialog(current_name: String) -> void:
	if _rename_overlay:
		_rename_overlay.queue_free()
	_rename_overlay = ColorRect.new()
	_rename_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rename_overlay.color = Color(0.0, 0.0, 0.0, 0.6)
	add_child(_rename_overlay)

	var dialog_panel := PanelContainer.new()
	dialog_panel.set_anchors_preset(Control.PRESET_CENTER)
	dialog_panel.grow_horizontal = Control.GROW_DIRECTION_BOTH
	dialog_panel.grow_vertical = Control.GROW_DIRECTION_BOTH
	dialog_panel.custom_minimum_size = Vector2(380, 0)
	var ds := StyleBoxFlat.new()
	ds.bg_color = Color(0.06, 0.08, 0.16, 0.96)
	ds.border_color = Color(0.2, 0.35, 0.6, 0.8)
	ds.set_border_width_all(2)
	ds.set_corner_radius_all(8)
	ds.set_content_margin_all(20)
	dialog_panel.add_theme_stylebox_override("panel", ds)
	_rename_overlay.add_child(dialog_panel)

	var dvbox := VBoxContainer.new()
	dvbox.add_theme_constant_override("separation", 12)
	dialog_panel.add_child(dvbox)

	var title_label := Label.new()
	title_label.text = "Rename Save"
	title_label.add_theme_font_size_override("font_size", 18)
	title_label.add_theme_color_override("font_color", Color(0.4, 0.85, 1.0))
	title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	dvbox.add_child(title_label)

	_rename_edit = LineEdit.new()
	_rename_edit.text = current_name
	_rename_edit.select_all()
	_rename_edit.add_theme_font_size_override("font_size", 16)
	_rename_edit.custom_minimum_size = Vector2(300, 36)
	dvbox.add_child(_rename_edit)

	var btn_hbox := HBoxContainer.new()
	btn_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_hbox.add_theme_constant_override("separation", 16)
	dvbox.add_child(btn_hbox)

	var cancel_btn := _make_slot_button("Cancel", Color(0.7, 0.7, 0.78))
	cancel_btn.custom_minimum_size = Vector2(120, 36)
	cancel_btn.pressed.connect(_on_rename_cancel)
	btn_hbox.add_child(cancel_btn)

	var ok_btn := _make_slot_button("Rename", Color(0.2, 1.0, 0.4))
	ok_btn.custom_minimum_size = Vector2(120, 36)
	ok_btn.pressed.connect(_on_rename_confirm)
	btn_hbox.add_child(ok_btn)

	_rename_edit.text_submitted.connect(func(_t: String): _on_rename_confirm())
	_rename_edit.grab_focus()


func _on_rename_cancel() -> void:
	_pending_rename_path = ""
	if _rename_overlay:
		_rename_overlay.queue_free()
		_rename_overlay = null


func _on_rename_confirm() -> void:
	if _pending_rename_path != "" and _rename_edit:
		var new_name := _rename_edit.text.strip_edges()
		if new_name != "" and new_name.is_valid_filename():
			var new_path := "user://" + SAVE_SLOT_PREFIX + new_name + ".json"
			if not FileAccess.file_exists(new_path):
				var dir := DirAccess.open("user://")
				if dir:
					dir.rename(_pending_rename_path, new_path)
	_pending_rename_path = ""
	if _rename_overlay:
		_rename_overlay.queue_free()
		_rename_overlay = null
	_populate_save_slot_list()


# =============================================================================
# Button handlers
# =============================================================================

func _on_continue() -> void:
	# GATE.T47.SAVE.SLOT_MANAGEMENT.001: Show save slot list instead of direct load.
	if _save_slot_visible:
		_close_save_slot_panel()
	else:
		_build_save_slot_panel()


func _on_new_voyage() -> void:
	# Reset sim kernel for fresh galaxy (handles Continue → Menu → New Voyage case).
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("ReinitializeForNewGameV0"):
		bridge.call("ReinitializeForNewGameV0")
	# Flag new game so GameManager runs intro sequence (not on continue/load).
	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.set("_is_new_game", true)
		gm.set("intro_active", true)  # Suppress gameplay until intro sequence completes.
		gm.set("_onboarding_shown", false)  # Reset so tutorial triggers again.
	_transition_to_game()


func _on_milestones() -> void:
	# GATE.S9.MILESTONES.VIEWER.001: Open milestone viewer panel as overlay.
	var viewer_script = load("res://scripts/ui/milestone_viewer.gd")
	if viewer_script:
		var viewer := Control.new()
		viewer.set_script(viewer_script)
		add_child(viewer)


func _on_credits() -> void:
	# GATE.S9.CREDITS.SCROLL.001: Open credits scroll overlay.
	var credits_script = load("res://scripts/ui/credits_scroll.gd")
	if credits_script:
		var credits := Control.new()
		credits.set_script(credits_script)
		add_child(credits)


func _on_settings() -> void:
	var sp = get_node_or_null("/root/SettingsPanel")
	if sp and sp.has_method("toggle_v0"):
		sp.toggle_v0()


func _on_quit() -> void:
	get_tree().quit()


func _transition_to_game() -> void:
	# Fade out audio before transition.
	if _audio_theme.playing:
		var fade_out := create_tween()
		fade_out.tween_property(_audio_theme, "volume_db", -60.0, 0.5)
		fade_out.tween_callback(func(): _audio_theme.stop())
	if _audio_drone.playing:
		var fade_out2 := create_tween()
		fade_out2.tween_property(_audio_drone, "volume_db", -60.0, 0.5)
		fade_out2.tween_callback(func(): _audio_drone.stop())

	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.set("_on_main_menu", false)

	# Brief delay to let audio fade, then change scene.
	var scene_tween := create_tween()
	scene_tween.tween_interval(0.6)
	scene_tween.tween_callback(func():
		get_tree().change_scene_to_file(PLAYABLE_SCENE)
	)
