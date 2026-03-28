extends Node3D
class_name DamageNumber
## GATE.S7.COMBAT_JUICE.DAMAGE_NUMBERS.001
## Floating damage number at impact point.
## Billboard Label3D, drifts upward, fades out.
## Usage:
##   DamageNumber.spawn(parent, position, amount, type)
## Types: "shield", "hull", "critical"
## Auto-frees after animation (~0.6s).

## Damage type colors.
const COLOR_SHIELD := Color("4488FF")   # Blue
const COLOR_HULL := Color("FF8844")     # Orange
const COLOR_CRITICAL := Color("FFFFFF") # White

## Font sizes by type.
const SIZE_SHIELD: int = 48
const SIZE_HULL: int = 56
const SIZE_CRITICAL: int = 72

## Animation parameters.
# GATE.T41.COMBAT.VFX_VERIFY.001: Verified visible at game camera altitude ~50u.
const DRIFT_HEIGHT: float = 60.0
const DRIFT_DURATION: float = 1.0
const FADE_START: float = 0.4
const CRIT_PULSE_SCALE: float = 1.3
const CRIT_PULSE_DURATION: float = 0.1

## Stacking offset tracker: class-level counter to stagger simultaneous hits.
## Reset periodically by the auto-free timing (numbers live < 1s).
static var _stack_offset: int = 0
static var _stack_reset_time: float = 0.0


## Spawn a floating damage number.
## `parent` — scene root or local system node.
## `pos` — world position of impact.
## `amount` — damage value (displayed as negative, e.g., "-8").
## `type` — "shield", "hull", or "critical".
static func spawn(parent: Node, pos: Vector3, amount: int, type: String = "hull") -> Node3D:
	var effect := Node3D.new()
	effect.name = "DmgNum"

	# Stacking offset: shift Y so simultaneous hits don't overlap.
	var time_now := Time.get_ticks_msec() / 1000.0
	if time_now - _stack_reset_time > 0.3:
		_stack_offset = 0
		_stack_reset_time = time_now
	var y_offset := float(_stack_offset) * 12.0
	_stack_offset += 1

	effect.position = pos + Vector3(0, y_offset, 0)
	parent.add_child(effect)

	var label := Label3D.new()
	label.name = "DmgLabel"
	# GATE.T41.COMBAT.VFX_VERIFY.001: Verified visible at game camera altitude ~50u.
	label.pixel_size = 0.8
	label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label.no_depth_test = true
	label.render_priority = 15
	label.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	label.outline_size = 20
	label.outline_modulate = Color(0, 0, 0, 0.95)

	# Configure by damage type.
	match type:
		"shield":
			label.text = "-%d" % amount
			label.modulate = COLOR_SHIELD
			label.font_size = SIZE_SHIELD
		"critical":
			label.text = "-%d!" % amount
			label.modulate = COLOR_CRITICAL
			label.font_size = SIZE_CRITICAL
		_:  # "hull" and fallback
			label.text = "-%d" % amount
			label.modulate = COLOR_HULL
			label.font_size = SIZE_HULL

	effect.add_child(label)

	# Animation: drift upward + fade out.
	var end_pos := effect.position + Vector3(0, DRIFT_HEIGHT, 0)
	var tween := effect.create_tween()
	tween.set_ease(Tween.EASE_OUT)
	tween.set_trans(Tween.TRANS_QUAD)
	tween.tween_property(effect, "position", end_pos, DRIFT_DURATION)

	# Fade: alpha goes from 1 to 0 in the latter portion.
	var fade_tween := effect.create_tween()
	fade_tween.tween_interval(DRIFT_DURATION * FADE_START)
	fade_tween.tween_method(
		func(v: float):
			if is_instance_valid(label):
				var c := label.modulate
				c.a = v
				label.modulate = c,
		1.0, 0.0, DRIFT_DURATION * (1.0 - FADE_START)
	)

	# Critical hit: brief scale pulse.
	if type == "critical":
		var pulse_tween := effect.create_tween()
		pulse_tween.tween_property(effect, "scale",
			Vector3(CRIT_PULSE_SCALE, CRIT_PULSE_SCALE, CRIT_PULSE_SCALE),
			CRIT_PULSE_DURATION)
		pulse_tween.tween_property(effect, "scale",
			Vector3.ONE,
			CRIT_PULSE_DURATION)

	# Auto-cleanup.
	var tree := parent.get_tree()
	if tree:
		var timer := tree.create_timer(DRIFT_DURATION + 0.1)
		timer.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect
