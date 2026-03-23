extends Node
class_name SteamInterface

## Steam integration stub. When GodotSteam is installed, this delegates to it.
## When not installed, all calls are no-ops that return safe defaults.

var _steam_available := false
var _app_id := 0
var _steam_id := 0

signal steam_initialized(success: bool)

func _ready() -> void:
	# Check if GodotSteam singleton exists
	if Engine.has_singleton("Steam"):
		_steam_available = true
		var steam = Engine.get_singleton("Steam")
		# Try to read steam_appid.txt
		var f := FileAccess.open("res://steam_appid.txt", FileAccess.READ)
		if f:
			_app_id = int(f.get_as_text().strip_edges())
			f.close()
		# Initialize Steam
		var init_result: Dictionary = steam.steamInitEx(false, _app_id)
		if init_result.get("status", 1) == 0:  # 0 = OK
			_steam_id = steam.getSteamID()
			print("[STEAM] Initialized. App=%d User=%d" % [_app_id, _steam_id])
			steam_initialized.emit(true)
		else:
			_steam_available = false
			push_warning("[STEAM] Init failed: %s" % str(init_result))
			steam_initialized.emit(false)
	else:
		push_warning("[STEAM] GodotSteam not installed — running in stub mode")
		steam_initialized.emit(false)

func is_available() -> bool:
	return _steam_available

func get_steam_id() -> int:
	return _steam_id

func set_achievement(achievement_name: String) -> bool:
	if not _steam_available: return false
	var steam = Engine.get_singleton("Steam")
	steam.setAchievement(achievement_name)
	steam.storeStats()
	print("[STEAM] Achievement unlocked: %s" % achievement_name)
	return true

func clear_achievement(achievement_name: String) -> bool:
	if not _steam_available: return false
	var steam = Engine.get_singleton("Steam")
	steam.clearAchievement(achievement_name)
	steam.storeStats()
	return true

func set_stat_int(stat_name: String, value: int) -> bool:
	if not _steam_available: return false
	var steam = Engine.get_singleton("Steam")
	steam.setStatInt(stat_name, value)
	steam.storeStats()
	return true

func get_stat_int(stat_name: String) -> int:
	if not _steam_available: return 0
	var steam = Engine.get_singleton("Steam")
	return steam.getStatInt(stat_name)

func set_rich_presence(key: String, value: String) -> void:
	if not _steam_available: return
	var steam = Engine.get_singleton("Steam")
	steam.setRichPresence(key, value)

# ── GATE.T51.STEAM.CLOUD_SAVES.001: Steam Cloud save sync ──────────────────

## Write a save file to Steam Cloud. Returns true on success.
func cloud_save_write(filename: String, data: String) -> bool:
	if not _steam_available:
		return false
	var steam = Engine.get_singleton("Steam")
	if not steam.isCloudEnabled():
		push_warning("[STEAM] Cloud storage is disabled by user")
		return false
	var bytes := data.to_utf8_buffer()
	var success: bool = steam.fileWrite(filename, bytes)
	if success:
		print("[STEAM] Cloud save written: %s (%d bytes)" % [filename, bytes.size()])
	else:
		push_warning("[STEAM] Cloud save write failed: %s" % filename)
	return success


## Read a save file from Steam Cloud. Returns content string or "" on failure.
func cloud_save_read(filename: String) -> String:
	if not _steam_available:
		return ""
	var steam = Engine.get_singleton("Steam")
	if not steam.isCloudEnabled():
		return ""
	var size: int = steam.getFileSize(filename)
	if size <= 0:
		return ""
	var data: Dictionary = steam.fileRead(filename, size)
	if data.get("ret", false):
		var content: String = data.get("buf", PackedByteArray()).get_string_from_utf8()
		print("[STEAM] Cloud save read: %s (%d bytes)" % [filename, size])
		return content
	return ""


## Check if a save file exists in Steam Cloud.
func cloud_save_exists(filename: String) -> bool:
	if not _steam_available:
		return false
	var steam = Engine.get_singleton("Steam")
	return steam.fileExists(filename)


## Delete a save file from Steam Cloud.
func cloud_save_delete(filename: String) -> bool:
	if not _steam_available:
		return false
	var steam = Engine.get_singleton("Steam")
	var success: bool = steam.fileDelete(filename)
	if success:
		print("[STEAM] Cloud save deleted: %s" % filename)
	return success


## Check if Steam Cloud storage is enabled for this user.
func is_cloud_enabled() -> bool:
	if not _steam_available:
		return false
	return Engine.get_singleton("Steam").isCloudEnabled()


func _process(_delta: float) -> void:
	if _steam_available:
		Engine.get_singleton("Steam").run_callbacks()
