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

void fragment() {
	// Normalized UV centered at (0, 0) with range [-1, 1].
	vec2 centered = UV * 2.0 - 1.0;

	// Distance from center, boosted at corners. Range ~0.0 (center) to ~1.41 (corner).
	float dist = length(centered);

	// Vignette mask: 0 at center, 1 at edges/corners.
	// smoothstep inner/outer controls how far the glow reaches inward.
	float vignette = smoothstep(0.4, 1.1, dist);

	// Each risk channel contributes its color scaled by its level and the vignette mask.
	vec3 heat_color      = vec3(1.0, 0.15, 0.08);  // red
	vec3 influence_color = vec3(0.2, 0.45, 1.0);    // blue
	vec3 trace_color     = vec3(0.15, 1.0, 0.35);   // green

	vec3 combined = heat_color * heat_level
	              + influence_color * influence_level
	              + trace_color * trace_level;

	// Alpha: strongest channel drives opacity, scaled by vignette distance.
	float max_level = max(heat_level, max(influence_level, trace_level));

	// Intensity curve: gentle at low values, pronounced near 1.0.
	// Square the level to keep the effect subtle until risk is genuinely high.
	float intensity = max_level * max_level;

	// Peak edge alpha: 0.55 at maximum risk — visible but not opaque.
	// Compound threat multiplier: 1.0 normally, up to 1.5 when 2+ meters critical.
	float threat_mult = 1.0 + compound_threat * 0.5;
	float alpha = vignette * intensity * 0.55 * threat_mult;

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


## Public API — call when compound threat state changes.
## active = true when 2+ risk meters exceed 0.7.
## Intensifies the vignette alpha by 1.5x when active.
func set_compound_threat(active: bool) -> void:
	if _shader_mat == null:
		return
	_shader_mat.set_shader_parameter("compound_threat", 1.0 if active else 0.0)
