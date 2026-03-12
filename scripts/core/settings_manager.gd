# GATE.S9.SETTINGS.INFRASTRUCTURE.001: Settings persistence singleton.
# Autoload "SettingsManager" — persists to user://settings.json.
# Provides get/set API with defaults. Emits settings_changed on every mutation.
extends Node

signal settings_changed(key: String, value: Variant)

const SAVE_PATH := "user://settings.json"

var _settings: Dictionary = {}
var _defaults: Dictionary = {
	"audio_master": 1.0,
	"audio_music": 0.8,
	"audio_sfx": 1.0,
	"audio_ambient": 0.7,
	"audio_ui": 0.9,
	"display_resolution_x": 1920,
	"display_resolution_y": 1080,
	"display_mode": 0,       # 0=Windowed, 1=Fullscreen, 2=Borderless
	"display_vsync": true,
	"display_quality": 1,    # 0=Low, 1=Medium, 2=High
	"gameplay_auto_pause": true,
	"gameplay_tutorial_toasts": true,
	"gameplay_tooltip_delay": 1.0,
	"gameplay_camera_sensitivity": 1.0,
	# GATE.S9.ACCESSIBILITY.FONT_SCALE.001: font size override (100-200%)
	"accessibility_font_scale": 100,
	# GATE.S9.ACCESSIBILITY.COLORBLIND.001: colorblind mode (0=None, 1=Deuteranopia, 2=Protanopia, 3=Tritanopia)
	"accessibility_colorblind_mode": 0,
	# GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001: high contrast toggle
	"accessibility_high_contrast": false,
	# GATE.S9.SETTINGS.ACCESSIBILITY_TAB.001: reduced screen shake toggle
	"accessibility_reduced_shake": false,
	# GATE.S9.ACCESSIBILITY.FIRST_LAUNCH.001: tracks whether first-launch prompt was shown
	"first_launch_shown": false,
}


func _ready() -> void:
	_load()
	# GATE.S9.ACCESSIBILITY.FONT_SCALE.001: apply font scale on startup
	_apply_font_scale(int(get_setting("accessibility_font_scale")))
	settings_changed.connect(_on_own_settings_changed)
	print("UUIR|SETTINGS_MANAGER|READY|keys=%d" % _settings.size())


## GATE.S9.ACCESSIBILITY.FONT_SCALE.001: apply font scale via ThemeDB
func _apply_font_scale(pct: int) -> void:
	var clamped := clampi(pct, 100, 200)
	ThemeDB.fallback_base_scale = float(clamped) / 100.0
	print("UUIR|SETTINGS_MANAGER|FONT_SCALE=%d" % clamped)


func _on_own_settings_changed(key: String, value: Variant) -> void:
	if key == "accessibility_font_scale":
		_apply_font_scale(int(value))


## Return a setting value, falling back to the built-in default.
func get_setting(key: String) -> Variant:
	return _settings.get(key, _defaults.get(key))


## Update a setting, emit the change signal, and persist immediately.
func set_setting(key: String, value: Variant) -> void:
	_settings[key] = value
	settings_changed.emit(key, value)
	_save()


## Reset a single key to its default value.
func reset_setting(key: String) -> void:
	if _defaults.has(key):
		set_setting(key, _defaults[key])


## Reset ALL settings to defaults and persist.
func reset_all() -> void:
	_settings = _defaults.duplicate()
	_save()
	for key in _settings:
		settings_changed.emit(key, _settings[key])


## Return a copy of the defaults dictionary (read-only convenience).
func get_defaults() -> Dictionary:
	return _defaults.duplicate()


# ── Persistence ─────────────────────────────────────────────────────────────

func _load() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		# First launch — seed from defaults.
		_settings = _defaults.duplicate()
		return
	var file := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if file == null:
		push_warning("SettingsManager: could not open %s — using defaults" % SAVE_PATH)
		_settings = _defaults.duplicate()
		return
	var text := file.get_as_text()
	file.close()
	var json := JSON.new()
	var err := json.parse(text)
	if err != OK:
		push_warning("SettingsManager: JSON parse error — using defaults")
		_settings = _defaults.duplicate()
		return
	var parsed = json.data
	if parsed is Dictionary:
		# Merge parsed values on top of defaults so new keys get defaults.
		_settings = _defaults.duplicate()
		for key in parsed:
			_settings[key] = parsed[key]
	else:
		_settings = _defaults.duplicate()


func _save() -> void:
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file == null:
		push_warning("SettingsManager: could not write %s" % SAVE_PATH)
		return
	file.store_string(JSON.stringify(_settings, "\t"))
	file.close()
