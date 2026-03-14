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
const TEXT_SECONDARY := Color(0.6, 0.6, 0.7)       # Column headers, labels
const TEXT_INFO     := Color(0.65, 0.8, 0.95)       # Contextual info (blue tint)
const TEXT_DISABLED := Color(0.5, 0.5, 0.6)        # Empty, locked, footer
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
func security_color(band: String) -> Color:
	match band:
		"hostile":   return RED
		"dangerous": return ORANGE
		"safe":      return GREEN
		_:           return BLUE

## Discovery phase → color mapping.
func discovery_phase_color(phase: String) -> Color:
	match phase:
		"SEEN":     return TEXT_DISABLED
		"SCANNED":  return YELLOW
		"ANALYZED": return GREEN
	return TEXT_DISABLED

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
