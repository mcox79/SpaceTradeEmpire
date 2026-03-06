shader_type spatial;
render_mode blend_add, specular_disabled, unshaded;

uniform sampler2D tex_frg_4;
uniform vec4 Emission : source_color;
uniform float Fade = 1.0;

void vertex() {
}

void fragment() {
	vec3 n_out2p0 = vec3(UV, 0.0);

	float n_out10p0 = n_out2p0.x;
	float n_out10p1 = n_out2p0.y;

	float n_in14p1 = 3.00000;
	float n_out14p0 = pow(n_out10p1, n_in14p1);

	float n_out3p0 = TIME;

	float n_in15p1 = 2.00000;
	float n_out15p0 = n_out3p0 * n_in15p1;

	float n_out12p0 = n_out14p0 + n_out15p0;

	float n_in11p2 = 0.00000;
	vec3 n_out11p0 = vec3(n_out10p0, n_out12p0, n_in11p2);

	vec4 tex_frg_4_read = texture(tex_frg_4, n_out11p0.xy);
	vec3 n_out4p0 = tex_frg_4_read.rgb;

	vec3 n_out6p0 = Emission.rgb;
	float n_out6p1 = Emission.a;

	vec3 n_out7p0 = n_out4p0 * n_out6p0;

	float n_out17p0 = Fade;

	float n_out16p0 = pow(n_out10p1, n_out17p0);

	float n_out18p0 = n_out6p1 * n_out16p0;

	ALBEDO = n_out7p0;
	ALPHA = n_out18p0;
}
