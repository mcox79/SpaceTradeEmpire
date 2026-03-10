# GATE.S7.NARRATIVE_DELIVERY.TEXT_PANEL.001: Narrative text display panel with faction styling.
# Bottom-center toast-like panel for flavor text, faction greetings, and intel entries.
# Instantiated by hud.gd; called via show_narrative(text, faction_id).
extends PanelContainer

# ── Faction color map (border + title tint) ──
# Keys match SimCore faction IDs (lowercase). Values = border/accent color.
const FACTION_COLORS := {
	"concord":   Color(0.27, 0.53, 0.80),   # #4488CC blue
	"chitin":    Color(0.27, 0.80, 0.27),    # #44CC44 green
	"weavers":   Color(0.67, 0.27, 0.80),    # #AA44CC purple
	"valorin":   Color(0.80, 0.27, 0.27),    # #CC4444 red
	"communion": Color(0.80, 0.67, 0.27),    # #CCAA44 gold
}

const DEFAULT_BORDER_COLOR := Color(0.3, 0.6, 1.0, 0.7)  # Neutral blue accent
const AUTO_HIDE_SECONDS := 5.0
const FADE_DURATION := 0.4
const PANEL_WIDTH := 520.0
const PANEL_BOTTOM_MARGIN := 60.0  # Above Zone G bar (40px bar + 20px gap)

var _hbox: HBoxContainer = null
var _portrait_rect: TextureRect = null
var _text_label: RichTextLabel = null
var _border_rect: ColorRect = null
var _fade_tween: Tween = null
var _hide_tween: Tween = null


func _ready() -> void:
	name = "NarrativePanel"
	visible = false
	mouse_filter = Control.MOUSE_FILTER_STOP

	# Panel style: dark bg with faction-colored border (border color set per show_narrative call).
	var style := StyleBoxFlat.new()
	style.bg_color = UITheme.PANEL_BG
	style.border_color = DEFAULT_BORDER_COLOR
	style.set_border_width_all(UITheme.BORDER_W)
	style.set_corner_radius_all(UITheme.CORNER_MD)
	style.content_margin_left = 12.0
	style.content_margin_right = 12.0
	style.content_margin_top = 10.0
	style.content_margin_bottom = 10.0
	add_theme_stylebox_override("panel", style)

	# Fixed width, auto height — positioned in _position_panel().
	custom_minimum_size = Vector2(PANEL_WIDTH, 0)

	# Layout: [portrait placeholder] [left border strip] [rich text]
	_hbox = HBoxContainer.new()
	_hbox.name = "ContentHBox"
	_hbox.add_theme_constant_override("separation", UITheme.SPACE_LG)
	_hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_hbox)

	# Portrait placeholder (left side, hidden by default — ready for future art).
	_portrait_rect = TextureRect.new()
	_portrait_rect.name = "PortraitPlaceholder"
	_portrait_rect.custom_minimum_size = Vector2(48, 48)
	_portrait_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_portrait_rect.visible = false  # Hidden until portrait textures are authored
	_portrait_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_hbox.add_child(_portrait_rect)

	# Faction color border strip (like toast priority border).
	_border_rect = ColorRect.new()
	_border_rect.name = "FactionBorder"
	_border_rect.custom_minimum_size = Vector2(4, 0)
	_border_rect.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_border_rect.color = DEFAULT_BORDER_COLOR
	_border_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_hbox.add_child(_border_rect)

	# RichTextLabel for narrative text (supports BBCode for future formatting).
	_text_label = RichTextLabel.new()
	_text_label.name = "NarrativeText"
	_text_label.bbcode_enabled = true
	_text_label.fit_content = true
	_text_label.scroll_active = false
	_text_label.custom_minimum_size = Vector2(PANEL_WIDTH - 80, 0)
	_text_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_text_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_text_label.add_theme_font_size_override("normal_font_size", UITheme.FONT_BODY)
	_text_label.add_theme_color_override("default_color", UITheme.TEXT_PRIMARY)
	_hbox.add_child(_text_label)

	# Click to dismiss.
	gui_input.connect(_on_gui_input)


## Display narrative text with faction-specific styling.
## text: the narrative/flavor string to show.
## faction_id: one of "concord", "chitin", "weavers", "valorin", "communion", or "" for neutral.
func show_narrative(text: String, faction_id: String = "") -> void:
	if text.is_empty():
		return

	# Cancel any pending hide/fade.
	_cancel_tweens()

	# Resolve faction accent color.
	var accent: Color = FACTION_COLORS.get(faction_id, DEFAULT_BORDER_COLOR)

	# Try SimBridge for authoritative faction colors (accent field).
	var bridge = get_node_or_null("/root/SimBridge")
	if bridge != null and not faction_id.is_empty() and bridge.has_method("GetFactionColorsV0"):
		var colors: Dictionary = bridge.call("GetFactionColorsV0", faction_id)
		if colors.get("found", false):
			accent = colors.get("accent", accent)

	# Apply faction border color to panel style.
	var style: StyleBoxFlat = get_theme_stylebox("panel") as StyleBoxFlat
	if style == null:
		style = StyleBoxFlat.new()
		style.bg_color = UITheme.PANEL_BG
		style.set_border_width_all(UITheme.BORDER_W)
		style.set_corner_radius_all(UITheme.CORNER_MD)
		style.content_margin_left = 12.0
		style.content_margin_right = 12.0
		style.content_margin_top = 10.0
		style.content_margin_bottom = 10.0
	style.border_color = accent
	add_theme_stylebox_override("panel", style)

	# Faction border strip color.
	if _border_rect:
		_border_rect.color = accent

	# Set text content (use BBCode color tag for faction-tinted text).
	if _text_label:
		_text_label.clear()
		var hex := accent.to_html(false)
		_text_label.append_text("[color=#%s]%s[/color]" % [hex, _escape_bbcode(text)])

	# Position and show.
	_position_panel()
	visible = true
	modulate.a = 0.0

	# Fade in.
	_fade_tween = create_tween()
	_fade_tween.tween_property(self, "modulate:a", 1.0, FADE_DURATION).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)

	# Schedule auto-hide after delay.
	_hide_tween = create_tween()
	_hide_tween.tween_interval(AUTO_HIDE_SECONDS)
	_hide_tween.tween_property(self, "modulate:a", 0.0, FADE_DURATION)
	_hide_tween.tween_callback(_on_auto_hide)

	print("UUIR|NARRATIVE_PANEL|SHOW|faction=%s|len=%d" % [faction_id, text.length()])


## Hide the panel immediately (no fade).
func hide_narrative() -> void:
	_cancel_tweens()
	visible = false
	modulate.a = 1.0


## Position panel at bottom-center of screen, above the Zone G bar.
func _position_panel() -> void:
	var vp_size := get_viewport_rect().size
	var x := (vp_size.x - PANEL_WIDTH) * 0.5
	var y := vp_size.y - PANEL_BOTTOM_MARGIN - 80  # Approximate panel height
	position = Vector2(x, y)
	size = Vector2(PANEL_WIDTH, 0)  # Let it auto-size vertically


func _cancel_tweens() -> void:
	if _fade_tween != null and _fade_tween.is_valid():
		_fade_tween.kill()
		_fade_tween = null
	if _hide_tween != null and _hide_tween.is_valid():
		_hide_tween.kill()
		_hide_tween = null


func _on_auto_hide() -> void:
	visible = false
	modulate.a = 1.0


func _on_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		hide_narrative()


## Escape BBCode special characters in plain text to prevent injection.
func _escape_bbcode(text: String) -> String:
	return text.replace("[", "[lb]")
