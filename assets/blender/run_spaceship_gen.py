"""
Generate a single fighter ship, render preview, export GLB.
Run: blender --background --python run_spaceship_gen.py
"""

import sys
import os
import bpy
import math
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
GEN_DIR = "C:/Users/marsh/AppData/Local/Temp/SpaceshipGenerator"
sys.path.insert(0, GEN_DIR)

import spaceship_generator as sg

# Fix: the original reset_scene uses old API
def reset_scene_fixed():
    for item in bpy.data.objects:
        item.select_set(item.name.startswith('Spaceship'))
    bpy.ops.object.delete()
    for material in bpy.data.materials:
        if not material.users:
            bpy.data.materials.remove(material)
    for texture in bpy.data.textures:
        if not texture.users:
            bpy.data.textures.remove(texture)

sg.reset_scene = reset_scene_fixed

# Output
out_dir = os.path.join(SCRIPT_DIR, "..", "models", "candidates")
preview_dir = os.path.join(SCRIPT_DIR, "previews", "candidates")
os.makedirs(out_dir, exist_ok=True)
os.makedirs(preview_dir, exist_ok=True)

# Render setup — Cycles
scene = bpy.data.scenes[0]
scene.render.engine = 'CYCLES'
scene.cycles.samples = 64
scene.cycles.use_denoising = True
scene.render.resolution_x = 800
scene.render.resolution_y = 600
scene.render.film_transparent = False

# Dark background
scene.world = bpy.data.worlds.new("BG")
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.05, 0.05, 0.06, 1)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.3

# 3-point lighting
bpy.ops.object.light_add(type='SUN', location=(5, -3, 6))
sun = bpy.context.active_object
sun.data.energy = 2.0
sun.rotation_euler = (math.radians(55), math.radians(10), math.radians(30))

bpy.ops.object.light_add(type='AREA', location=(-5, -3, 2))
fill = bpy.context.active_object
fill.data.energy = 50.0
fill.data.size = 5.0
fill.rotation_euler = (math.radians(70), 0, math.radians(-30))

bpy.ops.object.light_add(type='POINT', location=(0, 5, 3))
bpy.context.active_object.data.energy = 100.0

# Reflective ground plane
bpy.ops.mesh.primitive_plane_add(size=50, location=(0, 0, -3))
ground = bpy.context.active_object
ground.name = "GroundPlane"
mat_ground = bpy.data.materials.new("GroundMat")
mat_ground.use_nodes = True
shader = mat_ground.node_tree.nodes["Principled BSDF"]
shader.inputs["Base Color"].default_value = (0.02, 0.02, 0.025, 1)
shader.inputs["Metallic"].default_value = 0.8
shader.inputs["Roughness"].default_value = 0.15
ground.data.materials.append(mat_ground)

bpy.ops.object.camera_add()
cam = bpy.context.active_object
cam.name = "PreviewCam"
scene.camera = cam

# Delete default cube
for obj in list(bpy.data.objects):
    if obj.type == 'MESH' and obj.name != "GroundPlane":
        bpy.data.objects.remove(obj, do_unlink=True)

# === Generate ONE fighter ===
SEED = 1337
print(f"\n=== Generating fighter seed {SEED} ===")

sg.generate_spaceship(
    random_seed=str(SEED),
    num_hull_segments_min=2,
    num_hull_segments_max=4,
    create_asymmetry_segments=True,
    num_asymmetry_segments_min=1,
    num_asymmetry_segments_max=3,
    create_face_detail=True,
    allow_horizontal_symmetry=True,
    allow_vertical_symmetry=False,
    apply_bevel_modifier=True,
    assign_materials=True,
    ship_class='fighter',
)

ship = None
for obj in bpy.data.objects:
    if obj.name.startswith('Spaceship'):
        ship = obj
        break

# Position ground
bb = [ship.matrix_world @ Vector(c) for c in ship.bound_box]
lowest_z = min(v.z for v in bb)
ground.location.z = lowest_z - 0.2

# Apply modifiers
bpy.ops.object.select_all(action='DESELECT')
ship.select_set(True)
bpy.context.view_layer.objects.active = ship
for mod in ship.modifiers:
    try:
        bpy.ops.object.modifier_apply(modifier=mod.name)
    except:
        pass

# Stats
verts = len(ship.data.vertices)
faces = len(ship.data.polygons)
bb = [ship.matrix_world @ Vector(c) for c in ship.bound_box]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
size = mx - mn
print(f"  Verts: {verts}, Faces: {faces}, Size: {size.x:.1f} x {size.y:.1f} x {size.z:.1f}")

# Export GLB
glb_path = os.path.join(out_dir, f"fighter_seed_{SEED}.glb")
bpy.ops.export_scene.gltf(
    filepath=glb_path, export_format='GLB',
    use_selection=True, export_apply=True, export_yup=True)
print(f"  Exported: {glb_path}")

# Render 3/4 view
max_dim = max(size.x, size.y, size.z)
cam_dist = max_dim * 1.8
cam_pos = (cam_dist * 0.7, cam_dist * -0.5, cam_dist * 0.4)
cam.location = Vector(cam_pos)
cam.rotation_euler = (Vector((0,0,0)) - Vector(cam_pos)).to_track_quat('-Z','Y').to_euler()
scene.render.filepath = os.path.join(preview_dir, f"fighter_seed_{SEED}.png")
bpy.ops.render.render(write_still=True)

print(f"  Rendered: fighter_seed_{SEED}.png")
print("\n=== DONE ===")
