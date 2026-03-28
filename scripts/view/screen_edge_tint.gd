extends ColorRect
class_name ScreenEdgeTint
## GATE.S7.RISK_METER_UI.SCREEN_EDGE.001
## GATE.S7.RISK_METER_UI.COMPOUND.001 — compound_threat intensifies vignette.
## Full-screen vignette overlay that tints screen edges based on risk meter levels.
## Red = Heat, Blue = Influence, Green = Trace.
## At 0.0 the edge is completely transparent; at 1.0 the glow is clearly visible.
## Wire from hud.gd: call update_risk_levels(heat, influence, trace) each slow-poll.

## Inline fragment shader — canvas_item vignette with three risk color channels.
const VIGNETTE_SHADER := "
shader_type canvas_item;

uniform float heat_level : hint_range(0.0, 1.0) = 0.0;
uniform float influence_level : hint_range(0.0, 1.0) = 0.0;
uniform float trace_level : hint_range(0.0, 1.0) = 0.0;
uniform float compound_threat : hint_range(0.0, 1.0) = 0.0;
uniform float combat_overheat : hint_range(0.0, 1.0) = 0.0;
uniform float combat_damage : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	// Normalized UV centered at (0, 0) with range [-1, 1].
	vec2 centered = UV * 2.0 - 1.0;

	// Distance from center, boosted at corners. Range ~0.0 (center) to ~1.41 (corner).
	float dist = length(centered);

	// Vignette mask: 0 at center, 1 at edges/corners.
	// smoothstep inner/outer controls how far the glow reaches inward.
	// GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001: Combat overheat pushes vignette inward.
	float inner_edge = 0.8 - combat_overheat * 0.3;
	float vignette = smoothstep(inner_edge, 1.4, dist);

	// Each risk channel contributes its color scaled by its level and the vignette mask.
	vec3 heat_color      = vec3(1.0, 0.15, 0.08);  // red
	vec3 influence_color = vec3(0.2, 0.45, 1.0);    // blue
	vec3 trace_color     = vec3(0.15, 1.0, 0.35);   // green

	// GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001: Combat overheat shimmer.
	vec3 overheat_color = vec3(1.0, 0.5, 0.05); // orange shimmer
	float overheat_pulse = 1.0 + 0.15 * sin(TIME * 6.0) * combat_overheat;

	// GATE.T64.UI.COMBAT_VIGNETTE.001: Combat damage channel (red-orange, distinct from heat red).
	vec3 damage_color = vec3(1.0, 0.25, 0.05); // warm red-orange
	float damage_pulse = 1.0 + 0.2 * sin(TIME * 4.0) * combat_damage;

	vec3 combined = heat_color * heat_level
	              + influence_color * influence_level
	              + trace_color * trace_level
	              + overheat_color * combat_overheat * overheat_pulse
	              + damage_color * combat_damage * damage_pulse;

	// Alpha: strongest channel drives opacity, scaled by vignette distance.
	float max_level = max(heat_level, max(influence_level, max(trace_level, max(combat_overheat, combat_damage))));

	// Intensity curve: gentle at low values, pronounced near 1.0.
	// Quartic curve: invisible at low-medium risk, visible only when genuinely critical.
	float intensity = max_level * max_level * max_level * max_level;

	// Peak edge alpha: 0.15 at maximum risk — subtle tint, not a color wash.
	// Compound threat multiplier: 1.0 normally, up to 1.2 when 2+ meters critical.
	float threat_mult = 1.0 + compound_threat * 0.2;
	float alpha = vignette * intensity * 0.08 * threat_mult;

	COLOR = vec4(combined, alpha);
}
"

var _shader_mat: ShaderMaterial

func _ready() -> void:
	name = "ScreenEdgeTint"

	# Full-screen anchors.
	set_anchors_preset(Control.PRESET_FULL_RECT)
	offset_left = 0
	offset_top = 0
	offset_right = 0
	offset_bottom = 0

	# Never intercept input.
	mouse_filter = Control.MOUSE_FILTER_IGNORE

	# Build shader material.
	var shader := Shader.new()
	shader.code = VIGNETTE_SHADER
	_shader_mat = ShaderMaterial.new()
	_shader_mat.shader = shader
	_shader_mat.set_shader_parameter("heat_level", 0.0)
	_shader_mat.set_shader_parameter("influence_level", 0.0)
	_shader_mat.set_shader_parameter("trace_level", 0.0)
	_shader_mat.set_shader_parameter("compound_threat", 0.0)
	_shader_mat.set_shader_parameter("combat_damage", 0.0)
	material = _shader_mat

	# The ColorRect itself must be white so the shader drives all color.
	color = Color.WHITE


## Public API — call from hud.gd or any controller each update cycle.
## heat, influence, trace: float 0.0 to 1.0.
func update_risk_levels(heat: float, influence: float, trace: float) -> void:
	if _shader_mat == null:
		return
	_shader_mat.set_shader_parameter("heat_level", clampf(heat, 0.0, 1.0))
	_shader_mat.set_shader_parameter("influence_level", clampf(influence, 0.0, 1.0))
	_shader_mat.set_shader_parameter("trace_level", clampf(trace, 0.0, 1.0))


## GATE.S7.COMBAT_PHASE2.OVERHEAT_VFX.001: Combat overheat shimmer.
## pct: 0.0 = cool, 0.75+ = shimmer begins, 1.0 = full overheat glow.
func set_combat_overheat(pct: float) -> void:
	if _shader_mat == null:
		return
	# Remap: below 0.75 = 0, above 0.75 = 0..1 scaled
	var mapped := clampf((pct - 0.75) / 0.25, 0.0, 1.0) if pct >= 0.75 else 0.0
	_shader_mat.set_shader_parameter("combat_overheat", mapped)


## GATE.T64.UI.COMBAT_VIGNETTE.001: Combat damage vignette channel.
## Call when player hull < 80% during combat. intensity: 0.0 (hull full) to 1.0 (hull critical).
func set_combat_damage(active: bool, intensity: float = 0.5) -> void:
	if _shader_mat == null:
		return
	if not active:
		_shader_mat.set_shader_parameter("combat_damage", 0.0)
		return
	_shader_mat.set_shader_parameter("combat_damage", clampf(intensity, 0.0, 1.0))


## Public API — call when compound threat state changes.
## active = true when 2+ risk meters exceed 0.7.
## Intensifies the vignette alpha by 1.5x when active.
func set_compound_threat(active: bool) -> void:
	if _shader_mat == null:
		return
	_shader_mat.set_shader_parameter("compound_threat", 1.0 if active else 0.0)
