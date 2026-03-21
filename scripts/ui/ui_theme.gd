# scripts/ui/ui_theme.gd
# Design system autoload for Space Trade Empire.
# Single source of truth for all UI tokens: colors, typography, spacing, panels.
# Register as autoload "UITheme" in project.godot.
extends Node

# ============================================================================
# CORE PALETTE — semantic, not hue-based
# ============================================================================
const CYAN        := Color(0.4, 0.85, 1.0)        # Player, interactive, titles
const GREEN       := Color(0.2, 1.0, 0.4)         # Safe, profit, sell, success
const GREEN_SOFT  := Color(0.6, 1.0, 0.6)         # Industry, passive positive
const RED         := Color(1.0, 0.15, 0.15)        # Danger, hostile, critical
const RED_LIGHT   := Color(1.0, 0.45, 0.45)        # Combat highlight, enemy text
const ORANGE      := Color(1.0, 0.6, 0.2)          # Warning, caution, buy price
const YELLOW      := Color(1.0, 0.85, 0.2)         # Scanned, attention
const GOLD        := Color(1.0, 0.85, 0.4)         # Credits, rewards, missions
const BLUE        := Color(0.4, 0.7, 1.0)          # Moderate, neutral info
const PURPLE_LIGHT := Color(0.9, 0.85, 1.0)        # Discovery, anomaly

# ============================================================================
# TEXT HIERARCHY
# ============================================================================
const TEXT_PRIMARY  := Color(0.85, 0.85, 0.9)      # Body text, readable content
const TEXT_SECONDARY := Color(0.7, 0.7, 0.78)      # Column headers, labels (WCAG AA)
const TEXT_INFO     := Color(0.65, 0.8, 0.95)       # Contextual info (blue tint)
const TEXT_DISABLED := Color(0.55, 0.55, 0.65)     # Empty, locked, footer (WCAG AA on dark)
const TEXT_MUTED   := Color(0.7, 0.7, 0.7)         # Low-priority metadata
const TEXT_WHITE   := Color(1.0, 1.0, 1.0)         # Full white (overlays, game over)

# ============================================================================
# PANEL / STRUCTURAL
# ============================================================================
const PANEL_BG         := Color(0.05, 0.07, 0.12, 0.94) # Standard panel background
const PANEL_BG_LIGHT   := Color(0.08, 0.08, 0.12, 0.85) # Toast/tooltip
const PANEL_BG_OVERLAY := Color(0.0, 0.0, 0.0, 0.6)     # Scrim / pause overlay
const BORDER_DEFAULT   := Color(0.15, 0.25, 0.5, 1.0)   # Subtle navy border
const BORDER_ACCENT    := Color(0.3, 0.6, 1.0, 0.7)     # Blue glow border
const BORDER_DANGER    := Color(1.0, 0.3, 0.3, 0.7)     # Combat/danger border

# ============================================================================
# TYPOGRAPHY SCALE
# ============================================================================
const FONT_TITLE    := 22   # Panel titles
const FONT_SECTION  := 18   # Section headers
const FONT_BODY     := 16   # Body text, sub-headers
const FONT_SMALL    := 15   # Detail rows, contextual info
const FONT_CAPTION  := 13   # Column headers, fine print
const FONT_HUD_HUGE  := 72  # GAME OVER
const FONT_HUD_LARGE := 48  # PAUSED
const FONT_HUD_MED   := 32  # Restart prompt

## Monospace font for number columns (prevents column jitter).
var FONT_MONO: Font = null

# ============================================================================
# COLORBLIND MODE — deuteranopia-safe alternative palette
# ============================================================================
var colorblind_mode: bool = false

## Profit color: green normally, blue-cyan in colorblind mode.
func profit_color() -> Color:
	return Color(0.3, 0.7, 1.0) if colorblind_mode else GREEN

## Loss/danger color: red normally, orange-amber in colorblind mode.
func loss_color() -> Color:
	return Color(1.0, 0.7, 0.15) if colorblind_mode else RED_LIGHT

## Safe color: green normally, blue in colorblind mode.
func safe_color() -> Color:
	return Color(0.3, 0.6, 1.0) if colorblind_mode else GREEN

## Danger/hostile color: red normally, warm amber in colorblind mode.
func danger_color() -> Color:
	return Color(1.0, 0.55, 0.1) if colorblind_mode else RED

## Health gradient: ratio 1.0=full → 0.0=empty. Colorblind-safe.
func health_gradient(ratio: float) -> Color:
	if colorblind_mode:
		# Cyan-blue at full → yellow at half → orange at low
		if ratio > 0.5:
			return Color(0.3, 0.7, 1.0).lerp(Color(0.95, 0.85, 0.2), (1.0 - ratio) * 2.0)
		else:
			return Color(0.95, 0.85, 0.2).lerp(Color(1.0, 0.55, 0.1), (0.5 - ratio) * 2.0)
	else:
		# Green at full → yellow at half → red at low
		if ratio > 0.5:
			return Color(0.2, 0.85, 0.3).lerp(Color(0.95, 0.85, 0.2), (1.0 - ratio) * 2.0)
		else:
			return Color(0.95, 0.85, 0.2).lerp(Color(0.9, 0.15, 0.1), (0.5 - ratio) * 2.0)

# ============================================================================
# SPACING SYSTEM (base unit: 4px)
# ============================================================================
const SPACE_XS  := 2    # Tight row gap
const SPACE_SM  := 4    # Standard list separation
const SPACE_MD  := 6    # VBox section separation
const SPACE_LG  := 8    # Content margins, panel padding
const SPACE_XL  := 12   # Generous padding
const SPACE_2XL := 16   # Large content margins
const SPACE_3XL := 20   # Extra-wide padding

const CORNER_SM := 6    # Small panels (dock, toast)
const CORNER_MD := 8    # Standard panels (popup, modal)
const BORDER_W  := 2    # Standard border width

# ============================================================================
# NUMBER FORMATTING (consistent across all UI)
# ============================================================================

## Format credits with thousands separator for readability.
func fmt_credits(amount: int) -> String:
	if amount < 0:
		return "-%scr" % _fmt_thousands(-amount)
	return "%scr" % _fmt_thousands(amount)

## Format a number with comma thousands separator.
func _fmt_thousands(n: int) -> String:
	var s := str(n)
	if s.length() <= 3:
		return s
	var result := ""
	var count := 0
	for i in range(s.length() - 1, -1, -1):
		if count > 0 and count % 3 == 0:
			result = "," + result
		result = s[i] + result
		count += 1
	return result

## Apply monospace font to a Label (for number columns). Call after adding to tree.
func apply_mono(label: Label) -> void:
	if FONT_MONO != null:
		label.add_theme_font_override("font", FONT_MONO)

## Format percentage with consistent decimal places.
func fmt_pct(value: float, decimals: int = 0) -> String:
	if decimals == 0:
		return "%d%%" % int(value)
	return ("%." + str(decimals) + "f%%") % value

# ============================================================================
# PANEL FACTORY FUNCTIONS
# ============================================================================

## Dock menu panel (subtle border, compact padding).
func make_panel_dock() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = PANEL_BG
	s.border_color = BORDER_DEFAULT
	s.set_border_width_all(BORDER_W)
	s.set_corner_radius_all(CORNER_SM)
	s.set_content_margin_all(float(SPACE_LG))
	return s

## Standard bordered panel (popup, combat log, info panels).
func make_panel_standard(border_color: Color = BORDER_ACCENT) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = PANEL_BG
	s.border_color = border_color
	s.set_border_width_all(BORDER_W)
	s.set_corner_radius_all(CORNER_MD)
	s.content_margin_left = 12.0
	s.content_margin_right = 12.0
	s.content_margin_top = 8.0
	s.content_margin_bottom = 10.0
	return s

## Toast / tooltip panel (borderless, lighter bg).
func make_panel_toast() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = PANEL_BG_LIGHT
	s.set_corner_radius_all(CORNER_SM)
	s.content_margin_left = 12.0
	s.content_margin_right = 12.0
	s.content_margin_top = 8.0
	s.content_margin_bottom = 8.0
	return s

## Floating HUD element (borderless, standard bg).
func make_panel_hud() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = PANEL_BG
	s.set_corner_radius_all(CORNER_SM)
	s.content_margin_left = 10.0
	s.content_margin_right = 10.0
	s.content_margin_top = 8.0
	s.content_margin_bottom = 8.0
	return s

## Wide-padding modal (help, settings overlays).
func make_panel_modal() -> StyleBoxFlat:
	var s := make_panel_standard(BORDER_ACCENT)
	s.content_margin_left = 20.0
	s.content_margin_right = 20.0
	s.content_margin_top = 16.0
	s.content_margin_bottom = 16.0
	return s

# ============================================================================
# SHARED COLOR HELPERS
# ============================================================================

## Security band → color mapping (used by HUD, dock menu, node popup).
## Uses colorblind-safe alternatives when colorblind_mode is active.
func security_color(band: String) -> Color:
	match band:
		"hostile":   return danger_color()
		"dangerous": return ORANGE
		"safe":      return safe_color()
		_:           return BLUE

## Security band → icon+text (redundant shape indicator for color accessibility).
func security_icon_text(band: String) -> String:
	match band:
		"hostile":   return "⬟ Threat: High"
		"dangerous": return "▲ Threat: Elevated"
		"safe":      return "● Secure Space"
		_:           return "◆ Threat: Moderate"

## Discovery phase → color mapping.
func discovery_phase_color(phase: String) -> Color:
	match phase:
		"SEEN":     return TEXT_DISABLED
		"SCANNED":  return YELLOW
		"ANALYZED": return GREEN
	return TEXT_DISABLED

## Discovery phase → icon+label (redundant shape for accessibility).
func discovery_phase_icon(phase: String) -> String:
	match phase:
		"SEEN":     return "○"
		"SCANNED":  return "◐"
		"ANALYZED": return "●"
	return "○"

# ============================================================================
# FACTION THEMING (ready for future use)
# ============================================================================

enum Faction { NEUTRAL, TRADE, MILITARY, FRONTIER, SCIENTIFIC }

func faction_border_color(faction: Faction) -> Color:
	match faction:
		Faction.TRADE:      return Color(0.9, 0.75, 0.2, 0.8)
		Faction.MILITARY:   return Color(0.8, 0.2, 0.2, 0.8)
		Faction.FRONTIER:   return Color(0.85, 0.55, 0.15, 0.8)
		Faction.SCIENTIFIC: return Color(0.5, 0.3, 0.9, 0.8)
		_:                  return BORDER_DEFAULT

func faction_title_color(faction: Faction) -> Color:
	match faction:
		Faction.TRADE:      return GOLD
		Faction.MILITARY:   return RED_LIGHT
		Faction.FRONTIER:   return ORANGE
		Faction.SCIENTIFIC: return PURPLE_LIGHT
		_:                  return CYAN


# ============================================================================
# L1.1: SHIP COMPUTER VISUAL LANGUAGE
# ============================================================================

## Tab button StyleBox — flat with bottom accent bar when active.
func make_tab_active() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color(0.08, 0.10, 0.18, 0.9)
	s.border_color = CYAN
	s.border_width_bottom = 3
	s.border_width_top = 0
	s.border_width_left = 0
	s.border_width_right = 0
	s.set_corner_radius_all(0)
	s.content_margin_left = 8.0
	s.content_margin_right = 8.0
	s.content_margin_top = 6.0
	s.content_margin_bottom = 6.0
	return s

## Tab button StyleBox — inactive (dim dotted feel via subtle bottom border).
func make_tab_inactive() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color(0.04, 0.05, 0.10, 0.6)
	s.border_color = Color(0.2, 0.25, 0.4, 0.3)
	s.border_width_bottom = 1
	s.border_width_top = 0
	s.border_width_left = 0
	s.border_width_right = 0
	s.set_corner_radius_all(0)
	s.content_margin_left = 8.0
	s.content_margin_right = 8.0
	s.content_margin_top = 6.0
	s.content_margin_bottom = 6.0
	return s

## Ship computer panel — scan-line feel via subtle inner shadow + accent border.
## Diegetic: outer glow + thicker accent edge + shadow depth.
func make_panel_ship_computer(accent: Color = BORDER_ACCENT) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = PANEL_BG
	s.border_color = accent
	s.set_border_width_all(1)
	s.border_width_left = 3  # Thicker left edge — ship computer readout feel
	s.set_corner_radius_all(2)  # Sharp corners — military precision
	s.content_margin_left = 12.0
	s.content_margin_right = 12.0
	s.content_margin_top = 10.0
	s.content_margin_bottom = 10.0
	# Outer glow — diegetic holographic projection feel
	s.shadow_color = Color(accent.r, accent.g, accent.b, 0.15)
	s.shadow_size = 8
	s.shadow_offset = Vector2(0, 0)
	return s


# ============================================================================
# L1.4: SHARED PANEL HEADER FACTORY
# ============================================================================

## Build a standard section header: ALL CAPS title + thin horizontal rule.
## Returns a VBoxContainer with the title label and separator.
func make_section_header(title: String, accent_color: Color = CYAN) -> VBoxContainer:
	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 2)
	var lbl := Label.new()
	lbl.text = title.to_upper()
	lbl.add_theme_font_size_override("font_size", FONT_SECTION)
	lbl.add_theme_color_override("font_color", accent_color)
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	vbox.add_child(lbl)
	var rule := ColorRect.new()
	rule.custom_minimum_size = Vector2(0, 1)
	rule.color = Color(accent_color.r, accent_color.g, accent_color.b, 0.35)
	rule.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	rule.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(rule)
	return vbox

## Scanline overlay — adds subtle horizontal lines for diegetic CRT/hologram feel.
## Call after the parent is sized. Alpha controls intensity (0.03 = very subtle).
func add_scanline_overlay(parent: Control, line_alpha: float = 0.03) -> void:
	var overlay := Control.new()
	overlay.name = "ScanlineOverlay"
	overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.set_script(_ScanlineDraw)
	overlay.set_meta("scanline_alpha", line_alpha)
	parent.add_child(overlay)

## Corner bracket decorators for a Control — adds L-shaped marks at corners.
## Call after the control is added to the tree and sized.
func add_corner_brackets(parent: Control, bracket_color: Color = BORDER_ACCENT, size: float = 8.0) -> void:
	for i in range(4):
		var bracket := Control.new()
		bracket.name = "CornerBracket%d" % i
		bracket.custom_minimum_size = Vector2(size, size)
		bracket.mouse_filter = Control.MOUSE_FILTER_IGNORE
		bracket.set_script(_CornerBracketDraw)
		bracket.set_meta("bracket_color", bracket_color)
		bracket.set_meta("bracket_corner", i)  # 0=TL, 1=TR, 2=BL, 3=BR
		parent.add_child(bracket)
		match i:
			0:  # Top-left
				bracket.set_anchors_preset(Control.PRESET_TOP_LEFT)
			1:  # Top-right
				bracket.set_anchors_preset(Control.PRESET_TOP_RIGHT)
				bracket.offset_left = -size
			2:  # Bottom-left
				bracket.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
				bracket.offset_top = -size
			3:  # Bottom-right
				bracket.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
				bracket.offset_left = -size
				bracket.offset_top = -size

## Inline script for corner bracket _draw().
var _CornerBracketDraw: GDScript = null
## Inline script for scanline overlay _draw().
var _ScanlineDraw: GDScript = null
## Inline script for scroll-fade gradient _draw().
var _ScrollFadeDraw: GDScript = null

func _ready() -> void:
	# Load monospace font for number columns.
	# Load monospace font — prefer addon's imported copy, fall back to assets/.
	var mono_path := "res://addons/tooltips_pro/examples/resources/fonts/JetBrainsMonoNL-SemiBold.ttf"
	if not ResourceLoader.exists(mono_path):
		mono_path = "res://assets/fonts/JetBrainsMonoNL-SemiBold.ttf"
	if ResourceLoader.exists(mono_path):
		FONT_MONO = load(mono_path)
	# Create the scanline draw script once at startup.
	_ScanlineDraw = GDScript.new()
	_ScanlineDraw.source_code = """extends Control
func _draw() -> void:
	var a: float = get_meta("scanline_alpha", 0.03)
	var h: float = size.y
	var w: float = size.x
	var c := Color(1.0, 1.0, 1.0, a)
	var y := 0.0
	while y < h:
		draw_line(Vector2(0, y), Vector2(w, y), c, 1.0)
		y += 3.0
"""
	_ScanlineDraw.reload()
	# Create the bracket draw script once at startup.
	_CornerBracketDraw = GDScript.new()
	_CornerBracketDraw.source_code = """extends Control
func _draw() -> void:
	var c: Color = get_meta("bracket_color", Color(0.3, 0.6, 1.0, 0.3))
	c.a = 0.55
	var corner: int = int(get_meta("bracket_corner", 0))
	var sz: float = size.x
	var w: float = 1.5
	match corner:
		0:  # Top-left
			draw_line(Vector2(0, 0), Vector2(sz, 0), c, w)
			draw_line(Vector2(0, 0), Vector2(0, sz), c, w)
		1:  # Top-right
			draw_line(Vector2(0, 0), Vector2(sz, 0), c, w)
			draw_line(Vector2(sz, 0), Vector2(sz, sz), c, w)
		2:  # Bottom-left
			draw_line(Vector2(0, sz), Vector2(sz, sz), c, w)
			draw_line(Vector2(0, 0), Vector2(0, sz), c, w)
		3:  # Bottom-right
			draw_line(Vector2(0, sz), Vector2(sz, sz), c, w)
			draw_line(Vector2(sz, 0), Vector2(sz, sz), c, w)
"""
	_CornerBracketDraw.reload()
	# Create the scroll-fade gradient script once at startup.
	_ScrollFadeDraw = GDScript.new()
	_ScrollFadeDraw.source_code = """extends Control
func _process(_delta: float) -> void:
	var sc = get_meta("scroll_ref", null) as ScrollContainer
	if sc == null:
		visible = false
		return
	var content_h := 0.0
	if sc.get_child_count() > 0:
		content_h = sc.get_child(0).size.y
	var can_scroll_down: bool = (content_h - sc.scroll_vertical) > sc.size.y + 2.0
	visible = can_scroll_down
	if visible:
		queue_redraw()
func _draw() -> void:
	var h: float = get_meta("fade_height", 24.0)
	var w: float = size.x
	for i in range(int(h)):
		var t := float(i) / h
		var alpha := t * 0.6
		draw_line(Vector2(0, i), Vector2(w, i), Color(0.05, 0.07, 0.12, alpha), 1.0)
"""
	_ScrollFadeDraw.reload()


## Add a bottom-edge scroll fade gradient to a ScrollContainer's parent.
## Shows a translucent gradient at the bottom when content overflows, hinting scrollability.
func add_scroll_fade(scroll: ScrollContainer, fade_height: float = 24.0) -> void:
	var fade := Control.new()
	fade.name = "ScrollFade"
	fade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	fade.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	fade.offset_top = -fade_height
	fade.set_script(_ScrollFadeDraw)
	fade.set_meta("scroll_ref", scroll)
	fade.set_meta("fade_height", fade_height)
	scroll.get_parent().add_child(fade)


## Create a styled empty-state placeholder (icon + message + hint). Returns a VBoxContainer.
func make_empty_state(icon: String, message: String, hint: String = "") -> VBoxContainer:
	var box := VBoxContainer.new()
	box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	box.size_flags_vertical = Control.SIZE_EXPAND_FILL
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	box.add_theme_constant_override("separation", 8)
	var icon_lbl := Label.new()
	icon_lbl.text = icon
	icon_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	icon_lbl.add_theme_font_size_override("font_size", 36)
	icon_lbl.add_theme_color_override("font_color", TEXT_DISABLED)
	box.add_child(icon_lbl)
	var msg_lbl := Label.new()
	msg_lbl.text = message
	msg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	msg_lbl.add_theme_color_override("font_color", TEXT_SECONDARY)
	msg_lbl.add_theme_font_size_override("font_size", FONT_BODY)
	box.add_child(msg_lbl)
	if not hint.is_empty():
		var hint_lbl := Label.new()
		hint_lbl.text = hint
		hint_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		hint_lbl.add_theme_color_override("font_color", TEXT_DISABLED)
		hint_lbl.add_theme_font_size_override("font_size", FONT_CAPTION)
		box.add_child(hint_lbl)
	return box


## Create a dismiss hint footer label (e.g. "Press J to close"). Returns the Label.
func make_dismiss_hint(key_name: String) -> Label:
	var lbl := Label.new()
	lbl.text = "Press %s to close" % key_name
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", TEXT_DISABLED)
	lbl.add_theme_font_size_override("font_size", FONT_CAPTION)
	return lbl


## Style a Button with a colored border and themed appearance. Flavor: "accept" (green), "reject" (red), "neutral" (cyan).
func style_action_button(btn: Button, flavor: String = "neutral") -> void:
	var color: Color
	match flavor:
		"accept": color = GREEN
		"reject": color = RED_LIGHT
		_: color = CYAN
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(color.r, color.g, color.b, 0.1)
	sb.border_color = Color(color.r, color.g, color.b, 0.6)
	sb.set_border_width_all(1)
	sb.set_corner_radius_all(2)
	sb.content_margin_left = 12
	sb.content_margin_right = 12
	sb.content_margin_top = 4
	sb.content_margin_bottom = 4
	btn.add_theme_stylebox_override("normal", sb)
	var hover := sb.duplicate() as StyleBoxFlat
	hover.bg_color = Color(color.r, color.g, color.b, 0.25)
	hover.border_color = Color(color.r, color.g, color.b, 0.9)
	btn.add_theme_stylebox_override("hover", hover)
	btn.add_theme_color_override("font_color", color)
	btn.add_theme_color_override("font_hover_color", Color(1, 1, 1))
	btn.add_theme_font_size_override("font_size", FONT_SMALL)


## Animate a Control open (fade in + slide up). Call after setting visible = true.
func animate_open(ctrl: Control, duration: float = 0.2) -> void:
	ctrl.modulate.a = 0.0
	ctrl.position.y += 12.0
	var tw := ctrl.create_tween()
	tw.set_parallel(true)
	tw.tween_property(ctrl, "modulate:a", 1.0, duration).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	tw.tween_property(ctrl, "position:y", ctrl.position.y - 12.0, duration).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)


## Animate a Control closed (fade out), then call hide_callback. Call BEFORE setting visible = false.
func animate_close(ctrl: Control, hide_callback: Callable, duration: float = 0.12) -> void:
	var tw := ctrl.create_tween()
	tw.tween_property(ctrl, "modulate:a", 0.0, duration).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)
	tw.tween_callback(func():
		ctrl.modulate.a = 1.0
		hide_callback.call()
	)
