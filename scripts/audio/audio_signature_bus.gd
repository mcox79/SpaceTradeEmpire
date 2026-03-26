# scripts/audio/audio_signature_bus.gd
# GATE.T58.AUDIO.SIGNATURES.001: 11 audio signature definitions + bus routing.
# Per fo_trade_manager_v0.md §Audio Vocabulary.
# Manages audio cue playback with priority tiers and layering rules.
extends Node

# ── Audio cue IDs matching AudioSignatureContentV0 in SimCore ──
const CUE_ANOMALY_PING := "anomaly_ping"
const CUE_SCAN_PROCESS := "scan_process"
const CUE_DISCOVERY_REVEAL := "discovery_reveal"
const CUE_INSIGHT_CHIME := "insight_chime"
const CUE_BATCH_INSIGHT := "batch_insight"
const CUE_REVELATION_FANFARE := "revelation_fanfare"
const CUE_FO_COMM_OPEN := "fo_comm_open"
const CUE_FO_DECISION_TONE := "fo_decision_tone"
const CUE_ROUTE_HEARTBEAT := "route_heartbeat"
const CUE_ALERT_STING := "alert_sting"
const CUE_FLIP_MOMENT_FANFARE := "flip_moment_fanfare"

# ── Bus categories ──
const BUS_EXPLORATION := "Exploration"
const BUS_KNOWLEDGE := "Knowledge"
const BUS_FO_COMM := "FOComm"
const BUS_EMPIRE := "Empire"

# ── Priority tiers ──
enum AudioTier { PUNCTUAL, AMBIENT, ALERT }

# ── State ──
var _alert_queue: Array[String] = []
var _alert_playing := false
var _heartbeat_active := false
var _heartbeat_ducked := false

# Bus assignment per cue.
var _cue_bus := {
	CUE_ANOMALY_PING: BUS_EXPLORATION,
	CUE_SCAN_PROCESS: BUS_EXPLORATION,
	CUE_DISCOVERY_REVEAL: BUS_EXPLORATION,
	CUE_INSIGHT_CHIME: BUS_KNOWLEDGE,
	CUE_BATCH_INSIGHT: BUS_KNOWLEDGE,
	CUE_REVELATION_FANFARE: BUS_KNOWLEDGE,
	CUE_FO_COMM_OPEN: BUS_FO_COMM,
	CUE_FO_DECISION_TONE: BUS_FO_COMM,
	CUE_ROUTE_HEARTBEAT: BUS_EMPIRE,
	CUE_ALERT_STING: BUS_EMPIRE,
	CUE_FLIP_MOMENT_FANFARE: BUS_EMPIRE,
}

# Tier assignment per cue.
var _cue_tier := {
	CUE_ANOMALY_PING: AudioTier.PUNCTUAL,
	CUE_SCAN_PROCESS: AudioTier.PUNCTUAL,
	CUE_DISCOVERY_REVEAL: AudioTier.PUNCTUAL,
	CUE_INSIGHT_CHIME: AudioTier.PUNCTUAL,
	CUE_BATCH_INSIGHT: AudioTier.PUNCTUAL,
	CUE_REVELATION_FANFARE: AudioTier.ALERT,
	CUE_FO_COMM_OPEN: AudioTier.PUNCTUAL,
	CUE_FO_DECISION_TONE: AudioTier.PUNCTUAL,
	CUE_ROUTE_HEARTBEAT: AudioTier.AMBIENT,
	CUE_ALERT_STING: AudioTier.ALERT,
	CUE_FLIP_MOMENT_FANFARE: AudioTier.ALERT,
}


## Play an audio cue by ID. Handles priority and layering rules.
func play_cue(cue_id: String) -> void:
	var tier: int = _cue_tier.get(cue_id, AudioTier.PUNCTUAL)

	match tier:
		AudioTier.ALERT:
			# Rule: one alert at a time, queue don't stack.
			if _alert_playing:
				if cue_id not in _alert_queue:
					_alert_queue.append(cue_id)
				return
			_play_alert(cue_id)

		AudioTier.AMBIENT:
			# Route Heartbeat: start looping if not active.
			if cue_id == CUE_ROUTE_HEARTBEAT and not _heartbeat_active:
				_start_heartbeat()

		AudioTier.PUNCTUAL:
			# Plays immediately, layers freely with ambient.
			_play_punctual(cue_id)


## Stop an audio cue (primarily for ambient/loops).
func stop_cue(cue_id: String) -> void:
	if cue_id == CUE_ROUTE_HEARTBEAT:
		_stop_heartbeat()


## Duck the Route Heartbeat during dialogue/combat (-12dB).
func duck_heartbeat(ducked: bool) -> void:
	_heartbeat_ducked = ducked
	# Actual volume change would be applied to the AudioStreamPlayer.
	# Placeholder: print debug.
	if ducked:
		print("DEBUG_AUDIO: Route Heartbeat ducked (-12dB)")
	else:
		print("DEBUG_AUDIO: Route Heartbeat restored")


# ── Internal playback ──

func _play_alert(cue_id: String) -> void:
	_alert_playing = true
	# Duck heartbeat during alert.
	if _heartbeat_active:
		duck_heartbeat(true)
	print("DEBUG_AUDIO: Playing ALERT cue: ", cue_id, " (bus: ", _cue_bus.get(cue_id, ""), ")")
	# Placeholder: would play actual AudioStreamPlayer here.
	# On completion, call _on_alert_finished().
	# For now, simulate with a timer.
	var timer := get_tree().create_timer(2.0)
	timer.timeout.connect(_on_alert_finished)


func _on_alert_finished() -> void:
	_alert_playing = false
	# Restore heartbeat.
	if _heartbeat_active:
		duck_heartbeat(false)
	# Dequeue next alert if any.
	if _alert_queue.size() > 0:
		var next: String = _alert_queue[0]
		_alert_queue.remove_at(0)
		_play_alert(next)


func _play_punctual(cue_id: String) -> void:
	print("DEBUG_AUDIO: Playing PUNCTUAL cue: ", cue_id, " (bus: ", _cue_bus.get(cue_id, ""), ")")
	# Placeholder: would play actual AudioStreamPlayer here.


func _start_heartbeat() -> void:
	_heartbeat_active = true
	print("DEBUG_AUDIO: Route Heartbeat started (bus: ", BUS_EMPIRE, ")")
	# Placeholder: would start looping AudioStreamPlayer here.


func _stop_heartbeat() -> void:
	_heartbeat_active = false
	_heartbeat_ducked = false
	print("DEBUG_AUDIO: Route Heartbeat stopped")
	# Placeholder: would stop looping AudioStreamPlayer here.
