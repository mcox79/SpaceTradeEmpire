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
	"Every empire begins at the edge of the unknown.",
	"The void is patient. Commerce is not.",
	"Dead stars still cast long shadows on the ledger.",
	"Fractures in space are doors left ajar.",
	"What the Precursors built, the bold inherit.",
	"A ship without cargo is a question without an answer.",
	"The first trade route was drawn in starlight.",
	"In the hum of the gate, a forgotten language stirs.",
	"Empires rise on credit lines and courage.",
	"Somewhere beyond the rift, prices are better.",
]

# --- Galaxy gen progress messages (GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001) ---
const GEN_MESSAGES: Array[String] = [
	"Charting the void...",
	"Igniting warfronts...",
	"Seeding trade routes...",
	"Awakening factions...",
	"Calibrating jump drives...",
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
# GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Captain name input for new voyage wizard.
var _captain_name_input: LineEdit

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

# GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001: Galaxy gen overlay.
var _gen_overlay: ColorRect
var _gen_message_label: Label
var _gen_progress_bar: ProgressBar
var _gen_continue_label: Label
var _gen_active: bool = false
var _gen_elapsed: float = 0.0
var _gen_duration: float = 3.0
var _gen_message_timer: float = 0.0
var _gen_message_index: int = 0
var _gen_complete: bool = false

# Menu VBox (needed for galaxy gen overlay positioning).
var _menu_vbox: VBoxContainer
var _starfield_bg: ColorRect

# Save metadata for silhouette decision.
var _has_save: bool = false
var _save_meta: Dictionary = {}


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

	# GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Captain name input.
	var name_label := Label.new()
	name_label.text = "Captain Name"
	name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_label.add_theme_font_size_override("font_size", 18)
	name_label.add_theme_color_override("font_color", Color(0.6, 0.7, 0.85))
	_menu_vbox.add_child(name_label)

	_captain_name_input = LineEdit.new()
	_captain_name_input.text = "Commander"
	_captain_name_input.placeholder_text = "Enter captain name"
	_captain_name_input.max_length = 32
	_captain_name_input.custom_minimum_size = Vector2(280, 40)
	_captain_name_input.alignment = HORIZONTAL_ALIGNMENT_CENTER
	_captain_name_input.add_theme_font_size_override("font_size", 18)
	_menu_vbox.add_child(_captain_name_input)

	var name_spacer := Control.new()
	name_spacer.custom_minimum_size = Vector2(0, 8)
	_menu_vbox.add_child(name_spacer)

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
	_btn_continue.disabled = not _has_save
	_btn_milestones.disabled = not _has_save

	# ---- Galaxy gen overlay (GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001) ----
	_build_galaxy_gen_overlay()

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
# GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001: Galaxy generation overlay
# =============================================================================

func _build_galaxy_gen_overlay() -> void:
	_gen_overlay = ColorRect.new()
	_gen_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_gen_overlay.color = Color(0.01, 0.01, 0.04, 0.95)
	_gen_overlay.visible = false
	_gen_overlay.mouse_filter = Control.MOUSE_FILTER_STOP  # Block input to menu behind.
	add_child(_gen_overlay)

	var gen_vbox := VBoxContainer.new()
	gen_vbox.set_anchors_preset(Control.PRESET_CENTER)
	gen_vbox.grow_horizontal = Control.GROW_DIRECTION_BOTH
	gen_vbox.grow_vertical = Control.GROW_DIRECTION_BOTH
	gen_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	gen_vbox.add_theme_constant_override("separation", 24)
	_gen_overlay.add_child(gen_vbox)

	# Title for generation screen.
	var gen_title := Label.new()
	gen_title.text = "INITIALIZING GALAXY"
	gen_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	gen_title.add_theme_font_size_override("font_size", 36)
	gen_title.add_theme_color_override("font_color", Color(0.7, 0.8, 1.0))
	gen_vbox.add_child(gen_title)

	# Progress message.
	_gen_message_label = Label.new()
	_gen_message_label.text = GEN_MESSAGES[0]
	_gen_message_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_gen_message_label.add_theme_font_size_override("font_size", 20)
	_gen_message_label.add_theme_color_override("font_color", Color(0.5, 0.6, 0.8))
	gen_vbox.add_child(_gen_message_label)

	# Progress bar.
	_gen_progress_bar = ProgressBar.new()
	_gen_progress_bar.custom_minimum_size = Vector2(400, 20)
	_gen_progress_bar.min_value = 0.0
	_gen_progress_bar.max_value = 1.0
	_gen_progress_bar.value = 0.0
	_gen_progress_bar.show_percentage = false
	# Style the progress bar.
	var bar_bg := StyleBoxFlat.new()
	bar_bg.bg_color = Color(0.05, 0.05, 0.12)
	bar_bg.border_color = Color(0.2, 0.25, 0.4)
	bar_bg.set_border_width_all(1)
	bar_bg.set_corner_radius_all(4)
	_gen_progress_bar.add_theme_stylebox_override("background", bar_bg)
	var bar_fill := StyleBoxFlat.new()
	bar_fill.bg_color = Color(0.25, 0.4, 0.8)
	bar_fill.set_corner_radius_all(3)
	_gen_progress_bar.add_theme_stylebox_override("fill", bar_fill)
	gen_vbox.add_child(_gen_progress_bar)

	# "Press any key to continue" label (hidden until generation complete).
	_gen_continue_label = Label.new()
	_gen_continue_label.text = "Press any key to continue..."
	_gen_continue_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_gen_continue_label.add_theme_font_size_override("font_size", 18)
	_gen_continue_label.add_theme_color_override("font_color", Color(0.6, 0.65, 0.8, 1.0))
	_gen_continue_label.modulate.a = 0.0
	gen_vbox.add_child(_gen_continue_label)


func _start_galaxy_gen() -> void:
	_gen_active = true
	_gen_complete = false
	_gen_elapsed = 0.0
	_gen_message_timer = 0.0
	_gen_message_index = 0
	_gen_progress_bar.value = 0.0
	_gen_message_label.text = GEN_MESSAGES[0]
	_gen_continue_label.modulate.a = 0.0
	_gen_overlay.visible = true

	# Fade in the overlay.
	_gen_overlay.modulate.a = 0.0
	var fade_tween := create_tween()
	fade_tween.tween_property(_gen_overlay, "modulate:a", 1.0, 0.4).set_ease(Tween.EASE_OUT)


# =============================================================================
# Process loop
# =============================================================================

func _process(delta: float) -> void:
	# Rotate silhouette mesh (GATE.S9.MENU_ATMOSPHERE.SILHOUETTE.001).
	if _silhouette_mesh:
		_silhouette_mesh.rotate_y(delta * _silhouette_rotation_speed)

	# Galaxy gen overlay update (GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001).
	if _gen_active and not _gen_complete:
		_gen_elapsed += delta
		var progress := clampf(_gen_elapsed / _gen_duration, 0.0, 1.0)
		_gen_progress_bar.value = progress

		# Cycle progress messages.
		_gen_message_timer += delta
		var message_interval: float = _gen_duration / float(GEN_MESSAGES.size())
		if _gen_message_timer >= message_interval and _gen_message_index < GEN_MESSAGES.size() - 1:
			_gen_message_timer = 0.0
			_gen_message_index += 1
			_gen_message_label.text = GEN_MESSAGES[_gen_message_index]

		# Generation complete.
		if _gen_elapsed >= _gen_duration:
			_gen_complete = true
			_gen_progress_bar.value = 1.0
			_gen_message_label.text = "Galaxy initialized."
			# Fade in continue prompt.
			var prompt_tween := create_tween()
			prompt_tween.tween_property(_gen_continue_label, "modulate:a", 1.0, 0.8).set_ease(Tween.EASE_IN_OUT)
			# Gentle pulse on the continue label.
			prompt_tween.set_loops()
			prompt_tween.tween_property(_gen_continue_label, "modulate:a", 0.4, 1.2).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
			prompt_tween.tween_property(_gen_continue_label, "modulate:a", 1.0, 1.2).set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)


func _input(event: InputEvent) -> void:
	# GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001: "Press any key" gate after gen complete.
	if _gen_complete and _gen_active:
		if event is InputEventKey or event is InputEventMouseButton:
			if event.is_pressed():
				_gen_active = false
				get_viewport().set_input_as_handled()
				_transition_to_game()


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
# Button handlers
# =============================================================================

func _on_continue() -> void:
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("RequestLoad"):
		bridge.call("RequestLoad")
	_transition_to_game()


func _on_new_voyage() -> void:
	# Flag new game so GameManager shows welcome overlay (not on continue/load).
	var gm = get_node_or_null("/root/GameManager")
	if gm:
		gm.set("_is_new_game", true)
		gm.set("intro_active", true)  # Suppress gameplay from frame 1 until welcome overlay dismisses.
	# GATE.S7.MAIN_MENU.CAPTAIN_NAME.001: Pass captain name to SimBridge before starting.
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge and bridge.has_method("SetCaptainNameV0"):
		var name_text: String = _captain_name_input.text.strip_edges()
		if name_text == "":
			name_text = "Commander"
		bridge.call("SetCaptainNameV0", name_text)
	# GATE.S9.MENU_ATMOSPHERE.GALAXY_GEN.001: Show galaxy gen screen before transitioning.
	_start_galaxy_gen()


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
