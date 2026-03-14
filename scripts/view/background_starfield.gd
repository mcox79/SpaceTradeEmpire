extends CanvasLayer
## In-game background starfield: CanvasLayer at layer -1 behind 3D scene.
## Adapts the menu starfield shader (2 layers: deep dim + mid bright stars + nebula).
## Parallax responds to player ship XZ position for subtle motion.

const BG_STARFIELD_SHADER := "
shader_type canvas_item;

uniform vec2 world_offset = vec2(0.0, 0.0);
uniform float drift_speed : hint_range(0.0, 0.1) = 0.002;
uniform float parallax_scale : hint_range(0.0, 0.01) = 0.001;

float hash21(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float snoise_simple(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float fbm2(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	for (int i = 0; i < 4; i++) {
		v += a * snoise_simple(p);
		p *= 2.03;
		a *= 0.5;
	}
	return v;
}

float star_layer(vec2 uv, float density, float brightness_floor, float brightness_ceil) {
	vec2 cell = floor(uv * density);
	vec2 local = fract(uv * density);
	vec2 star_pos = vec2(hash21(cell), hash21(cell + vec2(127.1, 311.7)));
	float dist = length(local - star_pos);
	float radius = 0.015 + hash21(cell + vec2(73.3, 43.7)) * 0.025;
	float core = smoothstep(radius, radius * 0.2, dist);
	float glow = smoothstep(radius * 4.0, radius * 0.5, dist) * 0.3;
	float star_val = core + glow;
	float brightness = mix(brightness_floor, brightness_ceil, hash21(cell + vec2(53.9, 97.1)));
	float twinkle = sin(TIME * (1.5 + hash21(cell + vec2(19.3, 71.7)) * 3.0) + hash21(cell) * 6.28) * 0.15 + 0.85;
	return star_val * brightness * twinkle;
}

void fragment() {
	vec2 uv = UV;
	float t = TIME * drift_speed;
	vec2 offset = world_offset * parallax_scale;

	// Layer 1: Deep dim stars — sparse background field. Subdued so 3D objects stand out.
	vec2 uv_deep = uv + vec2(t * 0.3, t * 0.15) + offset * 0.5;
	float deep_stars = star_layer(uv_deep, 50.0, 0.1, 0.3);
	float deep_temp = hash21(floor(uv_deep * 50.0) + vec2(200.0, 300.0));
	vec3 deep_color = mix(vec3(0.7, 0.8, 1.0), vec3(1.0, 0.9, 0.75), deep_temp);
	vec3 layer1 = deep_color * deep_stars;

	// Layer 2: Mid-field stars — sparser, subtle parallax.
	vec2 uv_mid = uv + vec2(t * 0.6, t * 0.3) + offset;
	float mid_stars = star_layer(uv_mid, 20.0, 0.2, 0.5);
	float mid_temp = hash21(floor(uv_mid * 20.0) + vec2(400.0, 500.0));
	vec3 mid_color = mix(vec3(0.6, 0.75, 1.0), vec3(1.0, 0.92, 0.8), mid_temp);
	vec3 layer2 = mid_color * mid_stars;

	// Nebula wash — subtle blue-purple atmosphere.
	vec2 neb_uv = uv * 2.0 + vec2(t * 0.4, t * 0.2) + offset * 0.3;
	float neb_val = fbm2(neb_uv * 1.5);
	neb_val = pow(neb_val, 2.5);
	vec3 neb_color = mix(vec3(0.02, 0.03, 0.08), vec3(0.06, 0.08, 0.18), neb_val);
	vec3 nebula = neb_color * neb_val * 0.6;

	// Compose — dark background lets 3D scene objects be the visual focus.
	vec3 base = vec3(0.005, 0.005, 0.012);
	vec3 color = base + layer1 + nebula + layer2;

	// Soft vignette.
	vec2 vig_uv = uv * 2.0 - 1.0;
	float vignette = 1.0 - dot(vig_uv, vig_uv) * 0.2;
	color *= clamp(vignette, 0.0, 1.0);

	// Alpha: transparent where dark (3D scene shows through), opaque where stars are bright.
	float luminance = max(color.r, max(color.g, color.b));
	float alpha = smoothstep(0.015, 0.08, luminance);
	COLOR = vec4(color, alpha);
}
"

var _rect: ColorRect
var _shader_mat: ShaderMaterial

func _ready() -> void:
	name = "BackgroundStarfield"
	layer = -1
	add_to_group("BackgroundStarfield")
	# Full-screen ColorRect with shader.
	_rect = ColorRect.new()
	_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var shader := Shader.new()
	shader.code = BG_STARFIELD_SHADER
	_shader_mat = ShaderMaterial.new()
	_shader_mat.shader = shader
	_rect.material = _shader_mat
	_rect.color = Color.WHITE
	add_child(_rect)


func _process(_delta: float) -> void:
	# Feed player ship XZ position for parallax.
	var players := get_tree().get_nodes_in_group("Player") if get_tree() else []
	if players.size() > 0 and players[0] is Node3D:
		var pos: Vector3 = (players[0] as Node3D).global_position
		_shader_mat.set_shader_parameter("world_offset", Vector2(pos.x, pos.z))
