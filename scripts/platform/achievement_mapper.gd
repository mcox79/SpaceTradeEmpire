extends Node
## GATE.T46.STEAM.ACHIEVEMENT_BRIDGE.001
## AchievementMapper: listens to SimBridge milestone polling results and
## delegates to SteamInterface.set_achievement(). Safe when Steam is absent.
##
## Wiring: game_manager._ready() instantiates this node after _init_steam_v0().
## Usage:  call unlock(milestone_id) from _poll_milestone_celebrations_v0().

# Milestone ID → Steam achievement API name.
# All IDs are lowercase_snake_case matching the Steamworks dashboard entries.
# Milestone IDs come from SimCore/Content/MilestoneContentV0.cs.
const ACHIEVEMENT_MAP: Dictionary = {
	# Trade
	"first_trade":     "first_trade",
	"trader_100":      "trader_100",
	"trader_1000":     "trader_1000",
	"merchant_1000":   "merchant_1000",
	"tycoon_10000":    "tycoon_10000",
	"magnate_100000":  "magnate_100000",
	# Exploration
	"explorer_5":      "explorer_5",
	"explorer_15":     "explorer_15",
	"explorer_30":     "explorer_30",
	# Research
	"researcher_1":    "researcher_1",
	"researcher_5":    "researcher_5",
	"researcher_15":   "researcher_15",
	# Mission
	"captain_1":       "captain_1",
	"captain_5":       "captain_5",
	"captain_15":      "captain_15",
	# Combat
	"combat_1":        "combat_1",
	"combat_10":       "combat_10",
	"combat_25":       "combat_25",
}

# Tracks which achievements have been sent to Steam this session to avoid
# redundant setAchievement / storeStats calls.
var _unlocked_this_session: Dictionary = {}

func _ready() -> void:
	print("[AchievementMapper] Ready. Mapped %d achievements." % ACHIEVEMENT_MAP.size())

## unlock: call this whenever a milestone is newly achieved.
## Returns true if the achievement was forwarded to Steam, false otherwise.
func unlock(milestone_id: String) -> bool:
	if milestone_id.is_empty():
		return false

	# Guard: already sent this session.
	if _unlocked_this_session.has(milestone_id):
		print("[AchievementMapper] Already unlocked this session: %s" % milestone_id)
		return false

	# Resolve Steam achievement name from map; fall back to the milestone ID itself
	# so future milestones added to MilestoneContentV0 work without a code change.
	var achievement_name: String = ACHIEVEMENT_MAP.get(milestone_id, milestone_id)

	print("[AchievementMapper] Attempting unlock: milestone=%s achievement=%s" \
		% [milestone_id, achievement_name])

	# Mark as attempted before the Steam call so a crash/error doesn't cause retry loops.
	_unlocked_this_session[milestone_id] = true

	# Delegate to SteamInterface autoload. Safe when Steam is unavailable —
	# SteamInterface.set_achievement() returns false without calling GodotSteam.
	var steam_iface = get_node_or_null("/root/SteamInterface")
	if steam_iface == null:
		push_warning("[AchievementMapper] /root/SteamInterface not found — cannot unlock %s" \
			% achievement_name)
		return false

	if not steam_iface.has_method("set_achievement"):
		push_warning("[AchievementMapper] SteamInterface.set_achievement() missing — cannot unlock %s" \
			% achievement_name)
		return false

	var success: bool = steam_iface.call("set_achievement", achievement_name)
	if success:
		print("[AchievementMapper] Steam unlock confirmed: %s" % achievement_name)
	else:
		print("[AchievementMapper] Steam unavailable — unlock logged locally only: %s" \
			% achievement_name)
	return success
