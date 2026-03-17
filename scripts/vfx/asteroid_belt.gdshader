shader_type spatial;
render_mode cull_back, depth_draw_opaque, diffuse_lambert;

// GPU-driven asteroid belt orbital animation.
// INSTANCE_CUSTOM layout:
//   .x = orbital_radius
//   .y = angular_speed (rad/sec)
//   .z = phase_offset (rad)
//   .w = packed: integer part = y_offset * 10, fractional part = material_index / 10
//
// 5 material palettes (index 0-4):
//   0 = carbonaceous (dark), 1 = silicate (sandy), 2 = metallic (shiny),
//   3 = icy (blue-white), 4 = iron-rich (rusty)
// Ore veins encoded: material_index >= 5 means ore vein (subtract 5 for base mat).

uniform float roughness_base : hint_range(0.0, 1.0) = 0.85;

// Material palette — 5 base colors.
const vec3 MAT_CARBONACEOUS = vec3(0.22, 0.20, 0.17);
const vec3 MAT_SILICATE     = vec3(0.52, 0.47, 0.38);
const vec3 MAT_METALLIC     = vec3(0.55, 0.52, 0.48);
const vec3 MAT_ICY          = vec3(0.65, 0.70, 0.78);
const vec3 MAT_IRON         = vec3(0.40, 0.28, 0.20);

// Ore vein glow colors.
const vec3 ORE_BLUE   = vec3(0.1, 0.4, 0.6);
const vec3 ORE_COPPER = vec3(0.5, 0.2, 0.1);
const vec3 ORE_GOLD   = vec3(0.6, 0.5, 0.1);
const vec3 ORE_GREEN  = vec3(0.2, 0.5, 0.2);

varying flat int v_mat_index;
varying float v_phase_offset;

void vertex() {
	float orbital_radius = INSTANCE_CUSTOM.x;
	float angular_speed  = INSTANCE_CUSTOM.y;
	float phase_offset   = INSTANCE_CUSTOM.z;
	float packed_w       = INSTANCE_CUSTOM.w;

	// Pass phase to fragment for ore pulse animation.
	v_phase_offset = phase_offset;

	// Unpack y_offset and material index from .w
	float y_offset = floor(packed_w) * 0.1;
	v_mat_index = int(round(fract(abs(packed_w)) * 10.0));

	// Compute orbital position at current time.
	float angle = TIME * angular_speed + phase_offset;
	vec3 orbit_center = vec3(
		orbital_radius * cos(angle),
		y_offset,
		orbital_radius * sin(angle)
	);

	// Get rotated/scaled vertex offset from instance transform, discard translation.
	vec3 world_vert = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 world_origin = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
	vec3 local_offset = world_vert - world_origin;

	// Final world position: scaled/rotated vertex + orbital position.
	vec3 final_pos = local_offset + orbit_center;

	// Write directly to clip space, bypassing the normal MODEL_MATRIX pipeline.
	POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(final_pos, 1.0);

	// Transform normal to view space for lighting.
	NORMAL = normalize((VIEW_MATRIX * MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

vec3 get_base_color(int idx) {
	int base_idx = idx >= 5 ? idx - 5 : idx;
	if (base_idx == 0) return MAT_CARBONACEOUS;
	if (base_idx == 1) return MAT_SILICATE;
	if (base_idx == 2) return MAT_METALLIC;
	if (base_idx == 3) return MAT_ICY;
	return MAT_IRON;
}

void fragment() {
	ALBEDO = get_base_color(v_mat_index);

	// Metallic rocks are shinier.
	int base_idx = v_mat_index >= 5 ? v_mat_index - 5 : v_mat_index;
	ROUGHNESS = base_idx == 2 ? 0.60 : (base_idx == 3 ? 0.70 : roughness_base);
	METALLIC  = base_idx == 2 ? 0.7 : 0.0;

	// Ore vein glow — uses varying instead of INSTANCE_CUSTOM (vertex-only).
	if (v_mat_index >= 5) {
		int ore_idx = (v_mat_index - 5) % 4;
		vec3 ore_col = ORE_BLUE;
		if (ore_idx == 1) ore_col = ORE_COPPER;
		else if (ore_idx == 2) ore_col = ORE_GOLD;
		else if (ore_idx == 3) ore_col = ORE_GREEN;
		float pulse = 0.7 + 0.3 * sin(TIME * 2.0 + v_phase_offset);
		EMISSION = ore_col * 1.5 * pulse;
	}
}
