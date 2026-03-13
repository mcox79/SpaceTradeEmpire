# scripts/ui/warp_transit_hud.gd
# GATE.X.WARP.TRANSIT_HUD.001: Warp transit HUD overlay.
# Shows destination name, ETA progress bar, and distance remaining
# during lane transit. Auto-shows/hides based on player state.
extends PanelContainer

var _bridge = null

# UI elements
var _dest_label: Label = null
var _progress_bar: ProgressBar = null
var _distance_label: Label = null
var _eta_label: Label = null


func _ready() -> void:
	name = "WarpTransitHud"
	visible = false
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	_bridge = get_node_or_null("/root/SimBridge")

	# Panel style: dark semi-transparent with subtle cyan border (top-center).
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.03, 0.05, 0.10, 0.88)
	style.border_color = Color(0.3, 0.6, 1.0, 0.35)
	style.set_border_width_all(1)
	style.set_corner_radius_all(4)
	style.content_margin_left = 16.0
	style.content_margin_right = 16.0
	style.content_margin_top = 10.0
	style.content_margin_bottom = 10.0
	add_theme_stylebox_override("panel", style)

	# Size and position: top-center, 320px wide.
	custom_minimum_size = Vector2(320, 0)
	# Position will be set in _process based on viewport width.

	# --- Layout: VBoxContainer ---
	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 6)
	vbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(vbox)

	# Row 1: "WARP TRANSIT" header + destination name.
	_dest_label = Label.new()
	_dest_label.name = "DestLabel"
	_dest_label.text = "WARP TRANSIT"
	_dest_label.add_theme_font_size_override("font_size", UITheme.FONT_BODY)
	_dest_label.add_theme_color_override("font_color", UITheme.CYAN)
	_dest_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_dest_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(_dest_label)

	# Row 2: Progress bar (fills as transit progresses).
	_progress_bar = ProgressBar.new()
	_progress_bar.name = "TransitProgressBar"
	_progress_bar.min_value = 0.0
	_progress_bar.max_value = 1.0
	_progress_bar.value = 0.0
	_progress_bar.show_percentage = false
	_progress_bar.custom_minimum_size = Vector2(280, 8)
	_progress_bar.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Fill style: cyan-blue gradient feel.
	var fill_style := StyleBoxFlat.new()
	fill_style.bg_color = Color(0.35, 0.7, 1.0, 0.85)
	fill_style.set_corner_radius_all(2)
	_progress_bar.add_theme_stylebox_override("fill", fill_style)

	# Background style: dark track.
	var bg_style := StyleBoxFlat.new()
	bg_style.bg_color = Color(0.1, 0.12, 0.18, 0.6)
	bg_style.set_corner_radius_all(2)
	_progress_bar.add_theme_stylebox_override("background", bg_style)

	vbox.add_child(_progress_bar)

	# Row 3: HBox with ETA (left) and distance (right).
	var info_row := HBoxContainer.new()
	info_row.add_theme_constant_override("separation", 8)
	info_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(info_row)

	_eta_label = Label.new()
	_eta_label.name = "EtaLabel"
	_eta_label.text = "ETA: --"
	_eta_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_eta_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_eta_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_eta_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	info_row.add_child(_eta_label)

	_distance_label = Label.new()
	_distance_label.name = "DistanceLabel"
	_distance_label.text = ""
	_distance_label.add_theme_font_size_override("font_size", UITheme.FONT_CAPTION)
	_distance_label.add_theme_color_override("font_color", UITheme.TEXT_MUTED)
	_distance_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_distance_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_distance_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	info_row.add_child(_distance_label)


func _process(_delta: float) -> void:
	var gm = get_node_or_null("/root/GameManager")
	if gm == null:
		visible = false
		return

	var is_transit: bool = gm.get("current_player_state") == gm.PlayerShipState.IN_LANE_TRANSIT
	if not is_transit:
		visible = false
		return

	visible = true

	# Center horizontally at top of screen.
	var vp_size := get_viewport().get_visible_rect().size
	position = Vector2((vp_size.x - size.x) * 0.5, 60)

	# Destination name.
	var dest_id = gm.get("_lane_dest_node_id")
	var dest_name := ""
	if dest_id != null and not str(dest_id).is_empty():
		if _bridge and _bridge.has_method("GetNodeDisplayNameV0"):
			dest_name = str(_bridge.call("GetNodeDisplayNameV0", str(dest_id)))
		else:
			dest_name = str(dest_id)
	# FEEL_POST_FIX_8: Strip parenthesized tags for clean player-facing text.
	var paren_idx: int = dest_name.find("(")
	if paren_idx > 0:
		dest_name = dest_name.substr(0, paren_idx).strip_edges()
	if dest_name.is_empty():
		_dest_label.text = "WARP TRANSIT"
	else:
		_dest_label.text = "WARP TRANSIT  >  %s" % dest_name

	# Progress: compute from transit timing.
	var start_msec: int = int(gm.get("warp_transit_start_msec"))
	var duration_sec: float = float(gm.get("warp_transit_duration_sec"))
	var progress: float = 0.0
	if duration_sec > 0.0 and start_msec > 0:
		var elapsed_sec: float = (Time.get_ticks_msec() - start_msec) / 1000.0
		progress = clampf(elapsed_sec / duration_sec, 0.0, 1.0)
	_progress_bar.value = progress

	# ETA remaining.
	var eta_remaining: float = 0.0
	if duration_sec > 0.0 and start_msec > 0:
		var elapsed_sec: float = (Time.get_ticks_msec() - start_msec) / 1000.0
		eta_remaining = maxf(duration_sec - elapsed_sec, 0.0)
	if eta_remaining > 0.1:
		_eta_label.text = "ETA: %.1fs" % eta_remaining
	else:
		_eta_label.text = "Arriving..."

	# Distance remaining: interpolate from total distance based on progress.
	var origin_pos: Vector3 = gm.get("warp_transit_origin_pos") if gm.get("warp_transit_origin_pos") != null else Vector3.ZERO
	var dest_pos: Vector3 = gm.get("warp_transit_dest_pos") if gm.get("warp_transit_dest_pos") != null else Vector3.ZERO
	var total_dist: float = origin_pos.distance_to(dest_pos)
	if total_dist > 1.0:
		var remaining_dist: float = total_dist * (1.0 - progress)
		# Display in light-years (meters_per_ly = 50, from memory).
		var dist_ly: float = remaining_dist / 50.0
		if dist_ly >= 1.0:
			_distance_label.text = "%.1f ly" % dist_ly
		else:
			_distance_label.text = "%.0f u" % remaining_dist
	else:
		_distance_label.text = ""
