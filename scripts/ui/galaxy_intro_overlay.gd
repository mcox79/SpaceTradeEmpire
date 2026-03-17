# scripts/ui/galaxy_intro_overlay.gd
# Full-screen procedural spiral galaxy overlay for the new-game cinematic.
# Shows a spiral galaxy from a slight angle, zooms into an outer arm region
# that dissolves into scattered stars — matching the game's sector view.
#
# Usage (from game_manager.gd):
#   var overlay = preload("res://scripts/ui/galaxy_intro_overlay.gd").new()
#   add_child(overlay)
#   await overlay.play_intro()
#   overlay.queue_free()
extends CanvasLayer

const GALAXY_SHADER := """
shader_type canvas_item;

uniform float time : hint_range(0.0, 100.0) = 0.0;
uniform float zoom : hint_range(0.5, 200.0) = 1.0;
uniform vec2 pan_offset = vec2(0.0);
uniform float alpha : hint_range(0.0, 1.0) = 1.0;
uniform float tilt : hint_range(0.3, 1.0) = 0.55;

// ── Noise primitives ──

float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float hash2(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // quintic interpolation
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p, int octaves) {
	float v = 0.0;
	float a = 0.5;
	mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
	for (int i = 0; i < octaves; i++) {
		v += a * noise(p);
		p = rot * p * 2.0;
		a *= 0.5;
	}
	return v;
}

// ── Spiral arm function ──

float spiral_arm(float angle, float r, float arm_offset, float winding) {
	// Logarithmic spiral: angle = winding * ln(r) + offset
	float spiral_angle = winding * log(max(r, 0.001)) + arm_offset;
	float diff = angle - spiral_angle;
	// Wrap to [-PI, PI]
	diff = mod(diff + 3.14159, 6.28318) - 3.14159;
	// Arm width narrows toward center, broadens outward
	float width = 0.3 + r * 0.4;
	return exp(-diff * diff / (width * width * 0.08));
}

void fragment() {
	// Apply pan and zoom (pan shifts what part of the galaxy is centered)
	vec2 uv = (UV - 0.5) / zoom - pan_offset;

	// Slight perspective tilt — squash Y to simulate viewing angle
	uv.y /= tilt;

	// Very slow rotation for life
	float rot_angle = time * 0.008;
	float cs = cos(rot_angle);
	float sn = sin(rot_angle);
	uv = vec2(uv.x * cs - uv.y * sn, uv.x * sn + uv.y * cs);

	float r = length(uv);
	float angle = atan(uv.y, uv.x);

	// ── Spiral arms (2 major + 2 minor) ──
	float winding = 2.8; // tighter winding = more revolutions
	float arm1 = spiral_arm(angle, r, 0.0, winding);
	float arm2 = spiral_arm(angle, r, 3.14159, winding);
	float arm3 = spiral_arm(angle, r, 1.5708, winding) * 0.4; // minor arm
	float arm4 = spiral_arm(angle, r, 4.7124, winding) * 0.4; // minor arm
	float arms = arm1 + arm2 + arm3 + arm4;

	// Arm intensity fades at galaxy edge
	arms *= smoothstep(0.65, 0.08, r);

	// Turbulence in the arms (makes them clumpy, not smooth tubes)
	float turb = fbm(uv * 8.0 + vec2(time * 0.005), 6);
	arms *= 0.6 + turb * 0.8;

	// ── Dust lanes (dark regions between and within arms) ──
	float dust_lane = fbm(uv * 14.0 + vec2(3.7, 1.2), 5);
	float dark_dust = smoothstep(0.35, 0.55, dust_lane) * smoothstep(0.6, 0.1, r);
	arms *= 1.0 - dark_dust * 0.6; // darken arms where dust sits

	// ── Core (bulge) ──
	float core_r = r / max(tilt, 0.3); // compensate tilt for round core
	float core = exp(-core_r * 12.0) * 2.0;
	float core_halo = exp(-core_r * 4.0) * 0.5;
	// Core is slightly elliptical (bar galaxy hint)
	float bar_angle = angle - 0.3;
	float bar = exp(-r * 6.0) * 0.3 * pow(max(cos(bar_angle), 0.0), 4.0);

	// ── Star field (multiple layers for depth) ──
	// Dense small stars in arms
	float star_density = arms * smoothstep(0.6, 0.0, r);
	vec2 star_cell_1 = floor(uv * 400.0);
	float star1 = step(0.96 - star_density * 0.15, hash2(star_cell_1));
	float star1_bright = hash2(star_cell_1 + 100.0);
	star1 *= star1_bright * star1_bright; // vary brightness

	// Medium scattered stars
	vec2 star_cell_2 = floor(uv * 150.0);
	float star2 = step(0.985 - arms * 0.02, hash2(star_cell_2));
	float star2_bright = hash2(star_cell_2 + 200.0);
	star2 *= (0.5 + star2_bright * 0.5);

	// Bright foreground stars (sparse)
	vec2 star_cell_3 = floor(uv * 50.0);
	float star3 = step(0.993, hash2(star_cell_3));
	star3 *= smoothstep(0.7, 0.0, r) * 1.5;

	// Background stars (always visible, even outside galaxy)
	vec2 bg_cell = floor((UV - 0.5) * 500.0 / zoom);
	float bg_star = step(0.992, hash2(bg_cell));
	float bg_bright = hash2(bg_cell + 300.0);
	bg_star *= 0.08 + bg_bright * 0.07;

	// ── Nebula regions (emission nebulae in arms) ──
	float nebula = fbm(uv * 20.0 + vec2(7.3, 2.1), 4);
	nebula = pow(max(nebula - 0.4, 0.0) * 2.5, 2.0) * arms * smoothstep(0.5, 0.1, r);

	// ── Color grading ──
	// Core: warm yellow-white
	vec3 core_color = vec3(1.0, 0.92, 0.75);
	// Arms: blue-white with variation
	float arm_hue = fbm(uv * 6.0, 3);
	vec3 arm_color = mix(
		vec3(0.55, 0.7, 1.0),  // blue
		vec3(0.85, 0.8, 1.0),  // pale blue-white
		arm_hue
	);
	// Outer arms: slightly redder (older stars)
	arm_color = mix(arm_color, vec3(0.9, 0.7, 0.5), smoothstep(0.2, 0.55, r) * 0.3);
	// Dust: dark reddish-brown
	vec3 dust_color = vec3(0.15, 0.08, 0.05);
	// Nebulae: pink/magenta H-II regions
	vec3 nebula_color = mix(vec3(0.9, 0.3, 0.5), vec3(0.4, 0.3, 0.9), hash2(floor(uv * 30.0)));
	// Star colors
	float star_temp = hash2(floor(uv * 400.0) + 500.0);
	vec3 star_color = mix(
		vec3(0.8, 0.85, 1.0),  // blue-white
		vec3(1.0, 0.9, 0.7),   // yellow
		star_temp
	);

	// ── Compositing ──
	vec3 col = vec3(0.0);

	// Galaxy body
	col += arm_color * arms * 0.55;
	col += core_color * (core + core_halo + bar);

	// Dust darkening
	col *= 1.0 - dark_dust * 0.4 * vec3(0.7, 0.8, 1.0);

	// Stars
	col += star_color * star1 * 0.2;
	col += vec3(0.9, 0.92, 1.0) * star2 * 0.3;
	col += vec3(1.0, 0.95, 0.85) * star3;

	// Nebulae
	col += nebula_color * nebula * 0.15;

	// Background stars
	col += vec3(0.7, 0.75, 0.85) * bg_star;

	// Slight vignette
	float vignette = 1.0 - smoothstep(0.3, 0.75, length(UV - 0.5));
	col *= 0.85 + vignette * 0.15;

	// Tone mapping (prevent blowout)
	col = col / (1.0 + col * 0.3);

	COLOR = vec4(col, alpha);
}
"""

signal intro_finished

var _rect: ColorRect
var _shader_mat: ShaderMaterial
var _elapsed := 0.0
var _playing := false

# Zoom target: a point on the outer arm (~60% from center, slight angle)
# This is where our "sector" lives in the galaxy.
const TARGET_X := 0.12
const TARGET_Y := 0.08

# Timing — longer to let the galaxy breathe
const HOLD_TIME := 2.5    # Admire the full galaxy
const ZOOM_TIME := 5.0    # Long zoom into the arm (most of the cinematic)
const FADE_TIME := 1.5    # Crossfade to game view
const TOTAL_TIME := HOLD_TIME + ZOOM_TIME + FADE_TIME


func _ready() -> void:
	layer = 200  # Above everything
	name = "GalaxyIntroOverlay"

	_rect = ColorRect.new()
	_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var shader := Shader.new()
	shader.code = GALAXY_SHADER
	_shader_mat = ShaderMaterial.new()
	_shader_mat.shader = shader
	_shader_mat.set_shader_parameter("time", 0.0)
	_shader_mat.set_shader_parameter("zoom", 1.0)
	_shader_mat.set_shader_parameter("pan_offset", Vector2.ZERO)
	_shader_mat.set_shader_parameter("alpha", 1.0)
	_shader_mat.set_shader_parameter("tilt", 0.55)
	_rect.material = _shader_mat

	add_child(_rect)


func play_intro() -> void:
	_playing = true
	_elapsed = 0.0
	await intro_finished


func _unhandled_input(event: InputEvent) -> void:
	if not _playing:
		return
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		_playing = false
		intro_finished.emit()
		get_viewport().set_input_as_handled()

func _process(delta: float) -> void:
	if not _playing:
		return

	_elapsed += delta
	_shader_mat.set_shader_parameter("time", _elapsed)

	if _elapsed <= HOLD_TIME:
		# Phase 1: Hold on full galaxy with gentle breathing zoom
		var t := _elapsed / HOLD_TIME
		var z := 1.0 + t * 0.15
		_shader_mat.set_shader_parameter("zoom", z)
		_shader_mat.set_shader_parameter("pan_offset", Vector2(TARGET_X * t * 0.1, TARGET_Y * t * 0.1))
		_shader_mat.set_shader_parameter("alpha", 1.0)

	elif _elapsed <= HOLD_TIME + ZOOM_TIME:
		# Phase 2: Accelerating zoom toward the target arm region.
		# Ease-in-out (smooth start, fast middle, gentle arrival).
		var t := (_elapsed - HOLD_TIME) / ZOOM_TIME
		var ease_t := t * t * (3.0 - 2.0 * t)  # smoothstep
		# Zoom from ~1.15 to 120 (deep into the arm, individual "stars" visible)
		var z := 1.15 * pow(120.0 / 1.15, ease_t)
		# Pan toward target point on the outer arm
		var pan := Vector2(TARGET_X * ease_t, TARGET_Y * ease_t)
		_shader_mat.set_shader_parameter("zoom", z)
		_shader_mat.set_shader_parameter("pan_offset", pan)
		_shader_mat.set_shader_parameter("alpha", 1.0)

	elif _elapsed <= TOTAL_TIME:
		# Phase 3: Fade out — we're deep in the arm now, stars fill the screen.
		# This dissolves into the game's actual star field.
		var t := (_elapsed - HOLD_TIME - ZOOM_TIME) / FADE_TIME
		_shader_mat.set_shader_parameter("zoom", 120.0)
		_shader_mat.set_shader_parameter("pan_offset", Vector2(TARGET_X, TARGET_Y))
		_shader_mat.set_shader_parameter("alpha", 1.0 - t * t)  # ease-out fade

	else:
		_playing = false
		intro_finished.emit()
