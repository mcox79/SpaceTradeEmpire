class_name ShipMeshBuilder
## Procedural ship geometry with proper multi-section hulls, wings, nacelles,
## cockpit canopy, and thrust-reactive engine glow.
##
## Usage:
##   var ship_visual := ShipMeshBuilder.build_ship(role, faction_color)
##   parent.add_child(ship_visual)
##
## Roles: -1=player, 0=trader, 1=hauler, 2=patrol.

# Hull base colors per role.
const HULL_COLOR_PLAYER := Color(0.65, 0.72, 0.82)
const HULL_COLOR_TRADER := Color(0.58, 0.62, 0.52)
const HULL_COLOR_HAULER := Color(0.52, 0.48, 0.42)
const HULL_COLOR_PATROL := Color(0.48, 0.48, 0.52)

# Accent/trim colors per role (panels, wing tips, stripes).
const ACCENT_PLAYER := Color(0.25, 0.45, 0.85)
const ACCENT_TRADER := Color(0.35, 0.55, 0.30)
const ACCENT_HAULER := Color(0.65, 0.45, 0.15)
const ACCENT_PATROL := Color(0.75, 0.20, 0.20)

# Engine glow colors per role.
const ENGINE_COLOR_PLAYER := Color(0.3, 0.6, 1.0)
const ENGINE_COLOR_TRADER := Color(0.2, 0.8, 0.4)
const ENGINE_COLOR_HAULER := Color(1.0, 0.6, 0.2)
const ENGINE_COLOR_PATROL := Color(1.0, 0.3, 0.3)


## Build a complete ship visual node.
## fleet_hash: for deterministic variation (slight geometry tweaks per ship).
static func build_ship(role: int, faction_color: Color = Color.WHITE, fleet_hash: int = 0) -> Node3D:
	var root := Node3D.new()
	root.name = "FleetModel"

	var hull_color := _get_hull_color(role)
	var accent := _get_accent_color(role)
	if faction_color != Color.WHITE:
		hull_color = hull_color.lerp(faction_color, 0.30)
		accent = accent.lerp(faction_color, 0.20)

	var engine_color := _get_engine_color(role)
	var h := absi(fleet_hash)

	match role:
		-1: _build_player_ship(root, hull_color, accent, h)
		0:  _build_trader_ship(root, hull_color, accent, h)
		1:  _build_hauler_ship(root, hull_color, accent, h)
		2:  _build_patrol_ship(root, hull_color, accent, h)
		_:  _build_trader_ship(root, hull_color, accent, h)

	# Engine glow (emissive plume + light at rear).
	var engine := _build_engine_glow(role, engine_color)
	root.add_child(engine)

	return root


# ═══════════════════════════════════════════════════════════
# Ship builders — each role gets a distinct silhouette
# ═══════════════════════════════════════════════════════════

## Player ship: sleek fighter with swept wings and twin tail fins.
static func _build_player_ship(root: Node3D, hull: Color, accent: Color, h: int) -> void:
	# Main fuselage — elongated tapered body.
	var fuse := _create_fuselage(2.0, 5.5, 0.7, 0.20, hull)
	root.add_child(fuse)

	# Cockpit canopy — transparent raised bubble at front.
	var canopy := _create_canopy(0.55, 1.0, 0.35, Vector3(0.0, 0.38, -1.8))
	root.add_child(canopy)

	# Swept wings — angled back, thin.
	var wing_l := _create_wing(3.0, 1.8, 0.08, accent, true)
	wing_l.position = Vector3(-0.6, -0.05, 0.3)
	wing_l.rotation.y = deg_to_rad(-15.0)
	root.add_child(wing_l)

	var wing_r := _create_wing(3.0, 1.8, 0.08, accent, false)
	wing_r.position = Vector3(0.6, -0.05, 0.3)
	wing_r.rotation.y = deg_to_rad(15.0)
	root.add_child(wing_r)

	# Twin vertical tail fins.
	var fin_l := _create_fin(0.4, 0.9, 0.06, accent)
	fin_l.position = Vector3(-0.5, 0.45, 2.2)
	fin_l.rotation.z = deg_to_rad(10.0)
	root.add_child(fin_l)

	var fin_r := _create_fin(0.4, 0.9, 0.06, accent)
	fin_r.position = Vector3(0.5, 0.45, 2.2)
	fin_r.rotation.z = deg_to_rad(-10.0)
	root.add_child(fin_r)

	# Hull panel accent stripe along the spine.
	var stripe := _create_stripe(0.15, 4.0, hull.lerp(accent, 0.5))
	stripe.position = Vector3(0.0, 0.36, -0.5)
	root.add_child(stripe)


## Trader: utilitarian freighter with wide flat hull and stubby wings.
static func _build_trader_ship(root: Node3D, hull: Color, accent: Color, h: int) -> void:
	# Wider, flatter fuselage.
	var fuse := _create_fuselage(2.2, 5.0, 0.9, 0.45, hull)
	root.add_child(fuse)

	# Cargo bay bulge (wider box section in the middle).
	var cargo := _create_box_section(1.8, 2.0, 0.7, hull.darkened(0.1))
	cargo.position = Vector3(0.0, -0.15, 0.2)
	root.add_child(cargo)

	# Cockpit canopy.
	var canopy := _create_canopy(0.5, 0.8, 0.30, Vector3(0.0, 0.48, -1.8))
	root.add_child(canopy)

	# Stubby wings — short, functional.
	var wing_l := _create_wing(1.8, 1.4, 0.12, accent, true)
	wing_l.position = Vector3(-0.8, -0.1, 0.5)
	root.add_child(wing_l)

	var wing_r := _create_wing(1.8, 1.4, 0.12, accent, false)
	wing_r.position = Vector3(0.8, -0.1, 0.5)
	root.add_child(wing_r)

	# Side running lights.
	_add_running_light(root, Vector3(-1.1, 0.0, -0.5), accent)
	_add_running_light(root, Vector3(1.1, 0.0, -0.5), accent)


## Hauler: massive heavy freighter with blocky hull and side nacelles.
static func _build_hauler_ship(root: Node3D, hull: Color, accent: Color, h: int) -> void:
	# Thick, blocky fuselage — less taper.
	var fuse := _create_fuselage(3.0, 6.0, 1.3, 0.55, hull)
	root.add_child(fuse)

	# Large cargo containers on top.
	var cargo_a := _create_box_section(1.6, 3.0, 0.6, hull.darkened(0.15))
	cargo_a.position = Vector3(0.0, 0.7, 0.0)
	root.add_child(cargo_a)

	# Side nacelles / cargo pods.
	var nacelle_l := _create_nacelle(0.6, 3.5, 0.5, hull.darkened(0.08))
	nacelle_l.position = Vector3(-1.8, -0.2, 0.5)
	root.add_child(nacelle_l)

	var nacelle_r := _create_nacelle(0.6, 3.5, 0.5, hull.darkened(0.08))
	nacelle_r.position = Vector3(1.8, -0.2, 0.5)
	root.add_child(nacelle_r)

	# Cockpit — small relative to hull.
	var canopy := _create_canopy(0.45, 0.7, 0.25, Vector3(0.0, 0.68, -2.3))
	root.add_child(canopy)

	# Accent stripes on nacelles.
	var stripe_l := _create_stripe(0.08, 2.5, accent)
	stripe_l.position = Vector3(-1.8, 0.25, 0.5)
	root.add_child(stripe_l)
	var stripe_r := _create_stripe(0.08, 2.5, accent)
	stripe_r.position = Vector3(1.8, 0.25, 0.5)
	root.add_child(stripe_r)

	# Running lights on nacelle tips.
	_add_running_light(root, Vector3(-1.8, 0.0, -1.3), accent)
	_add_running_light(root, Vector3(1.8, 0.0, -1.3), accent)


## Patrol: aggressive interceptor with forward-swept wings and angular hull.
static func _build_patrol_ship(root: Node3D, hull: Color, accent: Color, h: int) -> void:
	# Narrow, angular fuselage — sharp taper.
	var fuse := _create_fuselage(1.6, 6.0, 0.55, 0.10, hull)
	root.add_child(fuse)

	# Angular chin plate (ventral armor wedge).
	var chin := _create_box_section(1.0, 2.0, 0.15, hull.lightened(0.05))
	chin.position = Vector3(0.0, -0.35, -0.8)
	chin.rotation.x = deg_to_rad(-5.0)
	root.add_child(chin)

	# Forward-swept wings — aggressive look.
	var wing_l := _create_wing(2.8, 2.0, 0.06, accent, true)
	wing_l.position = Vector3(-0.5, 0.0, -0.3)
	wing_l.rotation.y = deg_to_rad(20.0)  # Forward sweep
	root.add_child(wing_l)

	var wing_r := _create_wing(2.8, 2.0, 0.06, accent, false)
	wing_r.position = Vector3(0.5, 0.0, -0.3)
	wing_r.rotation.y = deg_to_rad(-20.0)
	root.add_child(wing_r)

	# Dorsal spine / weapon mount.
	var spine := _create_fin(0.3, 0.6, 0.06, accent)
	spine.position = Vector3(0.0, 0.30, -0.5)
	root.add_child(spine)

	# Narrow cockpit.
	var canopy := _create_canopy(0.40, 0.9, 0.25, Vector3(0.0, 0.30, -2.2))
	root.add_child(canopy)

	# Weapon hardpoint indicators (small accent boxes on wing roots).
	var hp_l := _create_box_section(0.15, 0.4, 0.15, accent)
	hp_l.position = Vector3(-1.2, -0.05, -1.0)
	root.add_child(hp_l)
	var hp_r := _create_box_section(0.15, 0.4, 0.15, accent)
	hp_r.position = Vector3(1.2, -0.05, -1.0)
	root.add_child(hp_r)

	# Wing tip lights.
	_add_running_light(root, Vector3(-2.5, 0.0, -1.5), accent)
	_add_running_light(root, Vector3(2.5, 0.0, -1.5), accent)


# ═══════════════════════════════════════════════════════════
# Geometry primitives — reusable ship components
# ═══════════════════════════════════════════════════════════

## Main fuselage: tapered body with nose and stern.
## nose_taper: 0.0 = needle point, 1.0 = blunt as rear.
static func _create_fuselage(width: float, length: float, height: float,
		nose_taper: float, color: Color) -> MeshInstance3D:
	var hw := width * 0.5
	var hl := length * 0.5
	var hh := height * 0.5
	var nt := nose_taper

	# 12 vertices: nose (4, tapered), mid (4, widest), stern (4, slightly tapered).
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	# Nose section → mid section.
	var nose := PackedVector3Array([
		Vector3(-hw * nt, hh * 0.6, -hl),
		Vector3( hw * nt, hh * 0.6, -hl),
		Vector3( hw * nt, -hh * 0.4, -hl),
		Vector3(-hw * nt, -hh * 0.4, -hl),
	])
	var mid := PackedVector3Array([
		Vector3(-hw, hh, -hl * 0.1),
		Vector3( hw, hh, -hl * 0.1),
		Vector3( hw, -hh, -hl * 0.1),
		Vector3(-hw, -hh, -hl * 0.1),
	])
	var stern := PackedVector3Array([
		Vector3(-hw * 0.85, hh * 0.9, hl),
		Vector3( hw * 0.85, hh * 0.9, hl),
		Vector3( hw * 0.85, -hh * 0.9, hl),
		Vector3(-hw * 0.85, -hh * 0.9, hl),
	])
	_add_box_faces(st, nose, mid)
	_add_box_faces(st, mid, stern)
	st.generate_normals()
	var mesh := ArrayMesh.new()
	st.commit(mesh)

	var mi := MeshInstance3D.new()
	mi.name = "Fuselage"
	mi.mesh = mesh
	mi.material_override = _make_hull_material(color)
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Cockpit canopy: transparent raised bubble.
static func _create_canopy(width: float, length: float, height: float,
		pos: Vector3) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = "Canopy"
	var box := BoxMesh.new()
	box.size = Vector3(width, height, length)
	mi.mesh = box
	mi.position = pos
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.15, 0.25, 0.4, 0.7)
	mat.metallic = 0.8
	mat.roughness = 0.1
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.emission_enabled = true
	mat.emission = Color(0.1, 0.2, 0.35)
	mat.emission_energy_multiplier = 1.5
	mi.material_override = mat
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Wing: flat tapered panel extending to one side.
static func _create_wing(span: float, chord: float, thickness: float,
		color: Color, is_left: bool) -> MeshInstance3D:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var ht := thickness * 0.5
	var hc := chord * 0.5
	var tip_chord := chord * 0.3  # Wing tip is narrower.
	var htc := tip_chord * 0.5
	var side := -span if is_left else span

	# Root (at fuselage) → tip.
	var root_verts := PackedVector3Array([
		Vector3(0.0, ht, -hc),
		Vector3(0.0, ht, hc),
		Vector3(0.0, -ht, hc),
		Vector3(0.0, -ht, -hc),
	])
	var tip_verts := PackedVector3Array([
		Vector3(side, ht * 0.5, -htc),
		Vector3(side, ht * 0.5, htc * 1.5),  # Tip trails back slightly.
		Vector3(side, -ht * 0.5, htc * 1.5),
		Vector3(side, -ht * 0.5, -htc),
	])
	# Swap winding for right wing.
	if not is_left:
		_add_box_faces(st, root_verts, tip_verts)
	else:
		_add_box_faces(st, tip_verts, root_verts)
	st.generate_normals()
	var mesh := ArrayMesh.new()
	st.commit(mesh)

	var mi := MeshInstance3D.new()
	mi.name = "Wing_L" if is_left else "Wing_R"
	mi.mesh = mesh
	mi.material_override = _make_hull_material(color)
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Vertical fin (tail stabilizer or dorsal spine).
static func _create_fin(chord: float, height: float, thickness: float,
		color: Color) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = "Fin"
	var box := BoxMesh.new()
	box.size = Vector3(thickness, height, chord)
	mi.mesh = box
	mi.material_override = _make_hull_material(color)
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Box section (cargo bay, armor plate, etc).
static func _create_box_section(width: float, length: float, height: float,
		color: Color) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = "BoxSection"
	var box := BoxMesh.new()
	box.size = Vector3(width, height, length)
	mi.mesh = box
	mi.material_override = _make_hull_material(color)
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Nacelle / engine pod — elongated cylinder with end caps.
static func _create_nacelle(radius: float, length: float, _height: float,
		color: Color) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = "Nacelle"
	var cyl := CylinderMesh.new()
	cyl.top_radius = radius * 0.7
	cyl.bottom_radius = radius
	cyl.height = length
	cyl.radial_segments = 12
	mi.mesh = cyl
	mi.rotation.x = deg_to_rad(90.0)  # Align along Z axis.
	mi.material_override = _make_hull_material(color)
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Accent stripe along the hull spine.
static func _create_stripe(width: float, length: float, color: Color) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = "Stripe"
	var box := BoxMesh.new()
	box.size = Vector3(width, 0.02, length)
	mi.mesh = box
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = 2.0
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mi.material_override = mat
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	return mi


## Small emissive sphere for running/nav lights.
static func _add_running_light(parent: Node3D, pos: Vector3, color: Color) -> void:
	var mi := MeshInstance3D.new()
	mi.name = "NavLight"
	var sphere := SphereMesh.new()
	sphere.radius = 0.08
	sphere.height = 0.16
	sphere.radial_segments = 6
	sphere.rings = 4
	mi.mesh = sphere
	mi.position = pos
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = 4.0
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mi.material_override = mat
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(mi)

	# Tiny point light.
	var light := OmniLight3D.new()
	light.light_color = color
	light.light_energy = 0.15
	light.omni_range = 2.0
	light.omni_attenuation = 2.0
	light.position = pos
	parent.add_child(light)


# ═══════════════════════════════════════════════════════════
# Geometry helpers
# ═══════════════════════════════════════════════════════════

## Add 6 faces between two quad cross-sections (front 0-3, rear 0-3).
static func _add_box_faces(st: SurfaceTool, front: PackedVector3Array,
		rear: PackedVector3Array) -> void:
	# Front cap.
	_add_quad(st, front[0], front[1], front[2], front[3])
	# Rear cap (reversed winding).
	_add_quad(st, rear[1], rear[0], rear[3], rear[2])
	# Top.
	_add_quad(st, front[0], rear[0], rear[1], front[1])
	# Bottom.
	_add_quad(st, front[3], front[2], rear[2], rear[3])
	# Left.
	_add_quad(st, rear[0], front[0], front[3], rear[3])
	# Right.
	_add_quad(st, front[1], rear[1], rear[2], front[2])


static func _add_quad(st: SurfaceTool, a: Vector3, b: Vector3,
		c: Vector3, d: Vector3) -> void:
	st.add_vertex(a)
	st.add_vertex(b)
	st.add_vertex(c)
	st.add_vertex(a)
	st.add_vertex(c)
	st.add_vertex(d)


# ═══════════════════════════════════════════════════════════
# Engine glow
# ═══════════════════════════════════════════════════════════

static func _build_engine_glow(role: int, color: Color) -> Node3D:
	var engine := Node3D.new()
	engine.name = "EngineGlow"
	engine.position = _get_engine_offset(role)

	# Emissive cone mesh (the visible glow plume).
	var cone_mi := MeshInstance3D.new()
	cone_mi.name = "GlowCone"
	var cone := CylinderMesh.new()
	var cone_radius := _get_hull_width(role) * 0.25
	cone.top_radius = cone_radius
	cone.bottom_radius = 0.02
	cone.height = 1.8
	cone.radial_segments = 10
	cone_mi.mesh = cone
	cone_mi.rotation.x = deg_to_rad(90.0)
	cone_mi.position.z = 0.9

	var mat := StandardMaterial3D.new()
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.blend_mode = BaseMaterial3D.BLEND_MODE_ADD
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(color.r, color.g, color.b, 0.6)
	mat.emission_enabled = true
	mat.emission = color
	mat.emission_energy_multiplier = 3.5
	mat.no_depth_test = true
	cone_mi.material_override = mat
	cone_mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	engine.add_child(cone_mi)

	# OmniLight for glow cast.
	var light := OmniLight3D.new()
	light.name = "EngineLight"
	light.light_color = color
	light.light_energy = 0.5
	light.omni_range = 6.0
	light.omni_attenuation = 2.0
	engine.add_child(light)

	# Attach the engine glow controller script.
	engine.set_script(preload("res://scripts/view/engine_glow_controller.gd"))

	return engine


# ═══════════════════════════════════════════════════════════
# Materials
# ═══════════════════════════════════════════════════════════

static func _make_hull_material(color: Color) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.metallic = 0.55
	mat.roughness = 0.40
	mat.emission_enabled = true
	mat.emission = color * 0.10
	mat.emission_energy_multiplier = 1.2
	return mat


# ═══════════════════════════════════════════════════════════
# Per-role lookups
# ═══════════════════════════════════════════════════════════

static func _get_hull_color(role: int) -> Color:
	match role:
		-1: return HULL_COLOR_PLAYER
		1:  return HULL_COLOR_HAULER
		2:  return HULL_COLOR_PATROL
		_:  return HULL_COLOR_TRADER

static func _get_accent_color(role: int) -> Color:
	match role:
		-1: return ACCENT_PLAYER
		1:  return ACCENT_HAULER
		2:  return ACCENT_PATROL
		_:  return ACCENT_TRADER

static func _get_engine_color(role: int) -> Color:
	match role:
		-1: return ENGINE_COLOR_PLAYER
		1:  return ENGINE_COLOR_HAULER
		2:  return ENGINE_COLOR_PATROL
		_:  return ENGINE_COLOR_TRADER

static func _get_engine_offset(role: int) -> Vector3:
	match role:
		-1: return Vector3(0.0, 0.0, 2.8)
		1:  return Vector3(0.0, 0.0, 3.0)
		2:  return Vector3(0.0, 0.0, 3.0)
		_:  return Vector3(0.0, 0.0, 2.5)

static func _get_hull_length(role: int) -> float:
	match role:
		-1: return 5.5
		1:  return 6.0
		2:  return 6.0
		_:  return 5.0

static func _get_hull_width(role: int) -> float:
	match role:
		-1: return 2.0
		1:  return 3.0
		2:  return 1.6
		_:  return 2.2
