extends Node3D
class_name ShieldRipple
## GATE.S7.COMBAT_JUICE.SHIELD_VFX.001
## Shield hit ripple: hex pattern on a sphere around the ship hull.
## Shield break: full flash + electric discharge particles.
## Usage:
##   ShieldRipple.spawn_hit(parent, ship_pos, impact_point)
##   ShieldRipple.spawn_break(parent, ship_pos)
## Auto-frees after animation.

## Shield hit ripple duration (seconds).
const RIPPLE_DURATION: float = 0.4

## Shield break flash duration (seconds).
const BREAK_DURATION: float = 0.5

## Hex ripple fragment shader (inline).
## Uniforms: impact_point (local-space), impact_time, elapsed.
const HEX_RIPPLE_SHADER := "
shader_type spatial;
render_mode blend_add, depth_draw_never, unshaded, cull_front;

uniform vec3 impact_point = vec3(0.0, 0.0, 1.0);
uniform float impact_time = 0.0;
uniform float elapsed = 0.0;
uniform float ripple_duration = 0.4;

// Hex grid distance function.
float hex_dist(vec2 p) {
	p = abs(p);
	return max(dot(p, vec2(0.866025, 0.5)), p.y);
}

vec2 hex_coords(vec2 uv, float scale) {
	vec2 r = vec2(1.0, 1.732);
	vec2 h = r * 0.5;
	vec2 a = mod(uv * scale, r) - h;
	vec2 b = mod(uv * scale - h, r) - h;
	vec2 g = (dot(a, a) < dot(b, b)) ? a : b;
	return g;
}

void fragment() {
	float t = elapsed - impact_time;
	if (t < 0.0 || t > ripple_duration) {
		discard;
	}

	// Normalized time 0..1.
	float nt = t / ripple_duration;

	// Distance from fragment to impact point (on sphere surface).
	float dist_to_impact = distance(VERTEX, impact_point);

	// Ripple ring: expanding wave front.
	float ring_radius = nt * 3.0;
	float ring_width = 0.4;
	float ring = 1.0 - smoothstep(0.0, ring_width, abs(dist_to_impact - ring_radius));

	// Hex pattern overlay using UV.
	vec2 hex_uv = hex_coords(UV * 4.0, 6.0);
	float hex_edge = smoothstep(0.35, 0.4, hex_dist(hex_uv));

	// Combine: ring intensity * hex pattern.
	float alpha = ring * hex_edge * (1.0 - nt);

	// Blue-white shield color.
	vec3 color = mix(vec3(0.3, 0.6, 1.0), vec3(0.8, 0.9, 1.0), ring);

	ALBEDO = color;
	ALPHA = clamp(alpha * 0.8, 0.0, 1.0);
}
"


## Spawn a shield hit ripple at the impact point.
static func spawn_hit(parent: Node, ship_pos: Vector3, impact_point: Vector3) -> Node3D:
	var effect := Node3D.new()
	effect.name = "ShieldRippleVfx"
	effect.position = ship_pos
	parent.add_child(effect)

	# Shield sphere mesh — sized to be visible at camera altitude ~80.
	# GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001: Enlarged radius + render_priority
	# for clear visibility from top-down camera.
	var mesh_inst := MeshInstance3D.new()
	mesh_inst.name = "ShieldSphere"
	var sphere := SphereMesh.new()
	sphere.radius = 18.0
	sphere.height = 36.0
	sphere.radial_segments = 32
	sphere.rings = 16
	mesh_inst.mesh = sphere
	mesh_inst.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	mesh_inst.render_priority = 12

	# Shader material.
	var shader := Shader.new()
	shader.code = HEX_RIPPLE_SHADER
	var shader_mat := ShaderMaterial.new()
	shader_mat.shader = shader

	# Convert impact point to local space of the shield sphere.
	var local_impact := impact_point - ship_pos
	shader_mat.set_shader_parameter("impact_point", local_impact)
	shader_mat.set_shader_parameter("impact_time", 0.0)
	shader_mat.set_shader_parameter("elapsed", 0.0)
	shader_mat.set_shader_parameter("ripple_duration", RIPPLE_DURATION)
	mesh_inst.material_override = shader_mat

	effect.add_child(mesh_inst)

	# Animate elapsed uniform over RIPPLE_DURATION.
	var tween := effect.create_tween()
	tween.tween_method(
		func(v: float):
			if is_instance_valid(shader_mat):
				shader_mat.set_shader_parameter("elapsed", v),
		0.0, RIPPLE_DURATION, RIPPLE_DURATION
	)
	tween.tween_callback(func():
		if is_instance_valid(effect):
			effect.queue_free()
	)

	return effect


## Spawn shield break effect: full-ship flash + electric discharge particles.
static func spawn_break(parent: Node, ship_pos: Vector3) -> Node3D:
	var effect := Node3D.new()
	effect.name = "ShieldBreakVfx"
	effect.position = ship_pos
	parent.add_child(effect)

	# Flash: bright overlay sphere — sized for camera altitude ~80.
	# GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001: Enlarged + higher emission for altitude.
	var flash := MeshInstance3D.new()
	flash.name = "BreakFlash"
	var sphere := SphereMesh.new()
	sphere.radius = 22.0
	sphere.height = 44.0
	flash.mesh = sphere
	var flash_mat := StandardMaterial3D.new()
	flash_mat.albedo_color = Color(0.6, 0.8, 1.0, 0.85)
	flash_mat.emission_enabled = true
	flash_mat.emission = Color(0.6, 0.8, 1.0)
	flash_mat.emission_energy_multiplier = 12.0
	flash_mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	flash_mat.no_depth_test = true
	flash.material_override = flash_mat
	flash.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	flash.render_priority = 12
	effect.add_child(flash)

	# Flash fade tween (0.2s).
	var flash_tween := effect.create_tween()
	flash_tween.tween_method(
		func(v: float):
			if is_instance_valid(flash_mat):
				flash_mat.albedo_color.a = v,
		0.7, 0.0, 0.2
	)
	flash_tween.parallel().tween_property(flash, "scale", Vector3(1.3, 1.3, 1.3), 0.2)
	flash_tween.tween_callback(func():
		if is_instance_valid(flash):
			flash.queue_free()
	)

	# Electric discharge particles — scaled for camera altitude ~80.
	# GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001: Enlarged particles for altitude.
	var particles := GPUParticles3D.new()
	particles.name = "DischargeParticles"
	particles.amount = 36
	particles.lifetime = 0.6
	particles.one_shot = true
	particles.explosiveness = 0.9
	particles.randomness = 0.5
	particles.emitting = true
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	proc_mat.direction = Vector3(0, 0, 0)
	proc_mat.spread = 180.0
	proc_mat.initial_velocity_min = 35.0
	proc_mat.initial_velocity_max = 75.0
	proc_mat.gravity = Vector3.ZERO
	proc_mat.scale_min = 0.5
	proc_mat.scale_max = 1.2
	proc_mat.damping_min = 5.0
	proc_mat.damping_max = 10.0
	# Electric blue-white color with fade.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(0.6, 0.8, 1.0, 1.0))
	gradient.add_point(0.5, Color(0.3, 0.5, 1.0, 0.8))
	gradient.add_point(1.0, Color(0.2, 0.3, 0.8, 0.0))
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.6
	mesh.height = 1.2
	particles.draw_pass_1 = mesh

	effect.add_child(particles)

	# Auto-cleanup.
	var tree := parent.get_tree()
	if tree:
		var timer := tree.create_timer(BREAK_DURATION + 0.2)
		timer.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect


## GATE.S7.COMBAT_FEEL_POLISH.SHIELD_VFX.001: Spawn orange spark shower for hull hits
## (shields are down). Visible from camera altitude ~80: particles spread wide, bright emissive.
static func spawn_hull_sparks(parent: Node, ship_pos: Vector3, impact_point: Vector3) -> Node3D:
	var effect := Node3D.new()
	effect.name = "HullSparksVfx"
	effect.position = ship_pos
	parent.add_child(effect)

	# GATE.S7.RUNTIME_STABILITY.COMBAT_VFX_V2.001: Enlarged spark particles + higher
	# velocity spread for clear visibility from camera altitude ~80.
	var particles := GPUParticles3D.new()
	particles.name = "HullSparkParticles"
	particles.amount = 32
	particles.lifetime = 0.55
	particles.one_shot = true
	particles.explosiveness = 0.9
	particles.randomness = 0.4
	particles.emitting = true
	particles.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var proc_mat := ParticleProcessMaterial.new()
	# Sparks fly outward from the impact direction — scaled for camera altitude ~80.
	var local_dir := (impact_point - ship_pos).normalized()
	proc_mat.direction = local_dir if local_dir.length() > 0.1 else Vector3(0, 1, 0)
	proc_mat.spread = 65.0
	proc_mat.initial_velocity_min = 30.0
	proc_mat.initial_velocity_max = 65.0
	proc_mat.gravity = Vector3(0, -1.0, 0)
	proc_mat.scale_min = 0.8
	proc_mat.scale_max = 1.8
	proc_mat.damping_min = 4.0
	proc_mat.damping_max = 8.0
	# Orange-yellow spark gradient with fade — brighter for altitude visibility.
	var gradient := Gradient.new()
	gradient.set_color(0, Color(1.0, 0.9, 0.3, 1.0))   # Bright yellow-orange
	gradient.add_point(0.4, Color(1.0, 0.6, 0.15, 0.9)) # Orange
	gradient.add_point(1.0, Color(0.8, 0.2, 0.0, 0.0))  # Red-orange fade out
	var color_ramp := GradientTexture1D.new()
	color_ramp.gradient = gradient
	proc_mat.color_ramp = color_ramp
	particles.process_material = proc_mat

	var mesh := SphereMesh.new()
	mesh.radius = 0.8
	mesh.height = 1.6
	particles.draw_pass_1 = mesh

	effect.add_child(particles)

	# Auto-cleanup.
	var tree2 := parent.get_tree()
	if tree2:
		var timer2 := tree2.create_timer(0.6)
		timer2.timeout.connect(func(): if is_instance_valid(effect): effect.queue_free())

	return effect
