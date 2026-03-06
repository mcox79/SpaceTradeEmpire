"""
Blender Python — stylized starfighter v6 (final).
Extrusion-based modeling from a single cube.

Run: blender --background --python generate_player_ship.py
"""

import bpy
import bmesh
import os
from math import radians
from mathutils import Vector, Matrix

# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in bpy.data.materials:
    bpy.data.materials.remove(mat)
for mesh in bpy.data.meshes:
    bpy.data.meshes.remove(mesh)

COL = bpy.context.collection or bpy.data.scenes[0].collection

# ---------------------------------------------------------------------------
# Materials
# ---------------------------------------------------------------------------
def make_mat(name, color, metallic=0.0, roughness=0.5, emission=None, estr=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = emission
        bsdf.inputs["Emission Strength"].default_value = estr
    return mat

MAT_HULL    = 0
MAT_DARK    = 1
MAT_COCKPIT = 2
MAT_ENGINE  = 3
MAT_GLOW    = 4
MAT_TIP     = 5

materials = [
    make_mat("Hull",    (0.25, 0.28, 0.35, 1), metallic=0.7, roughness=0.3),
    make_mat("Dark",    (0.12, 0.14, 0.18, 1), metallic=0.8, roughness=0.4),
    make_mat("Cockpit", (0.10, 0.45, 0.80, 1), metallic=0.9, roughness=0.06,
             emission=(0.08, 0.3, 0.6, 1), estr=2.0),
    make_mat("Engine",  (0.10, 0.12, 0.16, 1), metallic=0.9, roughness=0.4),
    make_mat("Glow",    (0.15, 0.55, 1.0, 1),  metallic=0.0, roughness=0.5,
             emission=(0.3, 0.8, 1.0, 1), estr=12.0),
    make_mat("Tip",     (0.9, 0.10, 0.03, 1),  metallic=0.0, roughness=0.5,
             emission=(1.0, 0.12, 0.02, 1), estr=5.0),
]

# ---------------------------------------------------------------------------
# BMesh helpers
# ---------------------------------------------------------------------------
def get_face_matrix(face, pos=None):
    verts = face.verts
    if len(verts) < 3:
        return Matrix.Identity(4)
    x_axis = (verts[1].co - verts[0].co).normalized()
    z_axis = -face.normal
    y_axis = z_axis.cross(x_axis).normalized()
    if not pos:
        pos = face.calc_center_bounds()
    mat = Matrix()
    mat[0][0], mat[1][0], mat[2][0], mat[3][0] = x_axis.x, x_axis.y, x_axis.z, 0
    mat[0][1], mat[1][1], mat[2][1], mat[3][1] = y_axis.x, y_axis.y, y_axis.z, 0
    mat[0][2], mat[1][2], mat[2][2], mat[3][2] = z_axis.x, z_axis.y, z_axis.z, 0
    mat[0][3], mat[1][3], mat[2][3], mat[3][3] = pos.x, pos.y, pos.z, 1
    return mat

def extrude_face(bm, face, distance, side_faces_out=None):
    result = bmesh.ops.extrude_discrete_faces(bm, faces=[face])
    new_faces = result['faces']
    if side_faces_out is not None:
        side_faces_out.extend(new_faces)
    new_face = new_faces[0]
    bmesh.ops.translate(bm, vec=new_face.normal * distance, verts=list(new_face.verts))
    return new_face

def scale_face(bm, face, sx, sy, sz):
    mat = get_face_matrix(face)
    mat.invert()
    bmesh.ops.scale(bm, vec=Vector((sx, sy, sz)), space=mat, verts=list(face.verts))

def translate_face(bm, face, vec):
    bmesh.ops.translate(bm, vec=vec, verts=list(face.verts))

def get_faces_by_normal(bm, direction, threshold=0.7):
    d = direction.normalized()
    return [f for f in bm.faces if f.normal.dot(d) > threshold]

# ===================================================================
# BUILD THE SHIP
# ===================================================================
bm = bmesh.new()

# Step 1: Start cube — wide body, good height, elongated
bmesh.ops.create_cube(bm, size=1.0)
bmesh.ops.scale(bm, vec=Vector((0.85, 0.75, 1.4)), verts=bm.verts)
bm.faces.ensure_lookup_table()

# Step 2: Nose (-Z direction)
front_faces = get_faces_by_normal(bm, Vector((0, 0, -1)), 0.9)
if front_faces:
    nose = front_faces[0]
    nose = extrude_face(bm, nose, 0.9)
    scale_face(bm, nose, 0.72, 0.85, 1.0)
    nose = extrude_face(bm, nose, 1.1)
    scale_face(bm, nose, 0.5, 0.55, 1.0)
    translate_face(bm, nose, Vector((0, -0.08, 0)))
    nose = extrude_face(bm, nose, 0.6)
    scale_face(bm, nose, 0.2, 0.4, 1.0)
    translate_face(bm, nose, Vector((0, -0.05, 0)))

# Step 3: Tail (+Z direction)
bm.faces.ensure_lookup_table()
rear_faces = get_faces_by_normal(bm, Vector((0, 0, 1)), 0.9)
if rear_faces:
    tail = rear_faces[0]
    tail = extrude_face(bm, tail, 0.5)
    scale_face(bm, tail, 0.88, 0.9, 1.0)
    tail = extrude_face(bm, tail, 0.4)
    scale_face(bm, tail, 0.75, 0.85, 1.0)

# Step 4: Wings (±X direction)
bm.faces.ensure_lookup_table()
bm.verts.ensure_lookup_table()
right_faces = get_faces_by_normal(bm, Vector((1, 0, 0)), 0.9)
left_faces = get_faces_by_normal(bm, Vector((-1, 0, 0)), 0.9)

for side_faces in [right_faces, left_faces]:
    mid_faces = [f for f in side_faces if abs(f.calc_center_bounds().z) < 0.9]
    if mid_faces:
        mid_faces.sort(key=lambda f: f.calc_area(), reverse=True)
        wing_root = mid_faces[0]
        wing = extrude_face(bm, wing_root, 0.6)
        scale_face(bm, wing, 1.0, 0.55, 0.85)
        translate_face(bm, wing, Vector((0, -0.1, 0.15)))
        wing = extrude_face(bm, wing, 0.8)
        scale_face(bm, wing, 1.0, 0.4, 0.7)
        translate_face(bm, wing, Vector((0, -0.12, 0.2)))
        wing = extrude_face(bm, wing, 0.4)
        scale_face(bm, wing, 1.0, 0.3, 0.45)
        translate_face(bm, wing, Vector((0, -0.05, 0.15)))
        wing.material_index = MAT_TIP

# Step 5: Cockpit canopy (top face near front)
bm.faces.ensure_lookup_table()
top_faces = get_faces_by_normal(bm, Vector((0, 1, 0)), 0.9)
cockpit_candidates = [f for f in top_faces
                      if f.calc_center_bounds().z < -0.2
                      and abs(f.calc_center_bounds().x) < 0.5
                      and f.calc_area() > 0.15]
if cockpit_candidates:
    cockpit_candidates.sort(key=lambda f: f.calc_center_bounds().z)
    cock_face = cockpit_candidates[0]
    bmesh.ops.subdivide_edges(bm, edges=list(cock_face.edges), cuts=1, use_grid_fill=True)
    bm.faces.ensure_lookup_table()
    new_top_faces = [f for f in bm.faces
                     if f.normal.dot(Vector((0,1,0))) > 0.9
                     and f.calc_center_bounds().z < -0.2
                     and abs(f.calc_center_bounds().x) < 0.35
                     and f.calc_area() < 0.5]
    if new_top_faces:
        new_top_faces.sort(key=lambda f: abs(f.calc_center_bounds().x))
        for cf in new_top_faces[:1]:
            bump = extrude_face(bm, cf, 0.22)
            scale_face(bm, bump, 1.5, 1.0, 1.6)
            bump = extrude_face(bm, bump, 0.18)
            scale_face(bm, bump, 0.75, 1.0, 0.85)
            bump.material_index = MAT_COCKPIT

# Step 6: Ventral keel (bottom face near center)
bm.faces.ensure_lookup_table()
bottom_faces = get_faces_by_normal(bm, Vector((0, -1, 0)), 0.9)
keel_candidates = [f for f in bottom_faces
                   if abs(f.calc_center_bounds().x) < 0.4
                   and abs(f.calc_center_bounds().z) < 0.8
                   and f.calc_area() > 0.15]
if keel_candidates:
    keel_candidates.sort(key=lambda f: f.calc_area(), reverse=True)
    kf = keel_candidates[0]
    keel = extrude_face(bm, kf, 0.15)
    scale_face(bm, keel, 0.6, 1.0, 1.2)
    keel.material_index = MAT_DARK

# Step 7: Engine nacelles — extrude from rear off-center faces
bm.faces.ensure_lookup_table()
rear_faces2 = get_faces_by_normal(bm, Vector((0, 0, 1)), 0.7)
engine_candidates = [f for f in rear_faces2
                     if abs(f.calc_center_bounds().x) > 0.1
                     and abs(f.calc_center_bounds().x) < 2.0
                     and f.calc_area() > 0.02]
engine_candidates.sort(key=lambda f: f.calc_area(), reverse=True)
used_engines = []
for ef in engine_candidates:
    cx = ef.calc_center_bounds().x
    if any(abs(cx - ux) < 0.25 for ux in used_engines):
        continue
    used_engines.append(cx)
    if len(used_engines) > 2:
        break
    sides = []
    eng = extrude_face(bm, ef, 0.7, sides)
    scale_face(bm, eng, 0.75, 0.75, 1.0)
    eng.material_index = MAT_ENGINE
    for sf in sides:
        if sf.is_valid:
            sf.material_index = MAT_ENGINE
    sides2 = []
    noz = extrude_face(bm, eng, 0.2, sides2)
    scale_face(bm, noz, 1.4, 1.4, 1.0)
    noz.material_index = MAT_DARK
    for sf in sides2:
        if sf.is_valid:
            sf.material_index = MAT_DARK
    glow = extrude_face(bm, noz, 0.1)
    scale_face(bm, glow, 0.85, 0.85, 1.0)
    glow.material_index = MAT_GLOW

# Step 8: Panel lines on large hull faces
bm.faces.ensure_lookup_table()
large_faces = [f for f in bm.faces
               if f.calc_area() > 0.25
               and f.material_index == MAT_HULL
               and abs(f.normal.y) < 0.5]
if large_faces:
    bmesh.ops.subdivide_edges(bm,
        edges=list(set(e for f in large_faces[:8] for e in f.edges)),
        cuts=1, use_grid_fill=True)

# Step 9: Dark accent on small rear/bottom faces
bm.faces.ensure_lookup_table()
for f in bm.faces:
    if f.material_index == MAT_HULL and f.calc_area() < 0.15:
        if f.normal.z > 0.5 or f.normal.y < -0.5:
            f.material_index = MAT_DARK

# Step 10: Symmetrize
bmesh.ops.symmetrize(bm, input=bm.verts[:] + bm.edges[:] + bm.faces[:], direction="X")
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

# ===================================================================
# FINALIZE
# ===================================================================
mesh = bpy.data.meshes.new("PlayerShip")
bm.to_mesh(mesh)
bm.free()

obj = bpy.data.objects.new("PlayerShip", mesh)
COL.objects.link(obj)

for mat in materials:
    obj.data.materials.append(mat)

bpy.context.view_layer.objects.active = obj
obj.select_set(True)

bpy.ops.object.origin_set(type='ORIGIN_CENTER_OF_MASS')
obj.location = (0, 0, 0)

# Bevel then Subdivision
bevel = obj.modifiers.new('Bevel', 'BEVEL')
bevel.width = 0.025
bevel.segments = 2
bevel.profile = 0.5
bevel.limit_method = 'ANGLE'
bevel.angle_limit = radians(30)

sub = obj.modifiers.new('Subdivision', 'SUBSURF')
sub.levels = 1
sub.render_levels = 2

bpy.ops.object.modifier_apply(modifier='Bevel')
bpy.ops.object.modifier_apply(modifier='Subdivision')

bpy.ops.object.shade_smooth()
if hasattr(obj.data, 'use_auto_smooth'):
    obj.data.use_auto_smooth = True
    obj.data.auto_smooth_angle = radians(35)

# ===================================================================
# EXPORT
# ===================================================================
script_dir = os.path.dirname(os.path.abspath(__file__))
out_dir = os.path.normpath(os.path.join(script_dir, "..", "models"))
os.makedirs(out_dir, exist_ok=True)

out_path = os.path.join(out_dir, "player_ship.glb")
bpy.ops.export_scene.gltf(
    filepath=out_path, export_format='GLB',
    use_selection=True, export_apply=True, export_yup=True)

blend_path = os.path.join(script_dir, "player_ship.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)

print(f"\n=== Exported: {out_path} ===")
print(f"=== Verts: {len(obj.data.vertices)}, Faces: {len(obj.data.polygons)} ===")
bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
print(f"=== Center: {(mn+mx)/2} ===")
print(f"=== Size: {mx-mn} ===")

# ===================================================================
# RENDER PREVIEWS — with bloom for emission glow
# ===================================================================
preview_dir = os.path.join(script_dir, "previews")
os.makedirs(preview_dir, exist_ok=True)

scene = bpy.data.scenes[0]
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 800
scene.render.resolution_y = 600
scene.render.film_transparent = True

# Note: Bloom removed in EEVEE 5.0. Emissive materials render as bright colors.
# In Godot, WorldEnvironment glow will provide the bloom effect.

# Key light
bpy.ops.object.light_add(type='SUN', location=(3, -2, 5))
sun = bpy.context.active_object
sun.data.energy = 3.5
sun.rotation_euler = (radians(40), radians(10), radians(25))

# Fill light
bpy.ops.object.light_add(type='POINT', location=(-4, -3, 2))
bpy.context.active_object.data.energy = 200.0

# Rim light from behind
bpy.ops.object.light_add(type='POINT', location=(0, 1, 4))
bpy.context.active_object.data.energy = 80.0

# Dark space background
scene.world = bpy.data.worlds.new("BG")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.01, 0.015, 0.03, 1)

bpy.ops.object.camera_add()
cam = bpy.context.active_object
scene.camera = cam

# Camera views — ship nose at -Z, tail at +Z
for name, pos in [
    ("3quarter_front", (4, -3, -3)),
    ("3quarter_rear",  (4, -3, 3)),
    ("rear",           (0.5, -1.5, 5)),
    ("side",           (6, -0.5, 0.3)),
    ("top",            (0.2, 6, 0.3)),
]:
    cam.location = Vector(pos)
    cam.rotation_euler = (Vector((0,0,0)) - Vector(pos)).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath = os.path.join(preview_dir, f"ship_{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"=== Rendered: ship_{name}.png ===")
