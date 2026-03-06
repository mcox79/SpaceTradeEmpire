"""
Render each Quaternius ship from FBX with its blue texture.
Run: blender --background --python render_quaternius.py
"""

import os
import bpy
import math
from mathutils import Vector

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PACK_DIR = os.path.join("C:/Users/marsh/AppData/Local/Temp/quaternius", "original",
                        "Ultimate Spaceships - May 2021")
OUT_DIR = os.path.join(SCRIPT_DIR, "previews", "quaternius")
os.makedirs(OUT_DIR, exist_ok=True)

# Render setup
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

# Lighting
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

# Ground plane
bpy.ops.mesh.primitive_plane_add(size=100, location=(0, 0, -5))
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

# Keep track of non-ship objects
permanent_objects = set(o.name for o in bpy.data.objects)

ships = sorted([d for d in os.listdir(PACK_DIR)
                if os.path.isdir(os.path.join(PACK_DIR, d))])

for ship_name in ships:
    fbx_path = os.path.join(PACK_DIR, ship_name, "FBX", f"{ship_name}.fbx")
    tex_path = os.path.join(PACK_DIR, ship_name, "Textures", f"{ship_name}_Blue.png")

    if not os.path.exists(fbx_path):
        print(f"  SKIP {ship_name}: no FBX")
        continue

    print(f"\n=== {ship_name} ===")

    # Clear previous ship meshes
    for obj in list(bpy.data.objects):
        if obj.name not in permanent_objects:
            bpy.data.objects.remove(obj, do_unlink=True)

    # Import FBX
    bpy.ops.import_scene.fbx(filepath=fbx_path)

    # Find imported meshes
    imported = [o for o in bpy.data.objects if o.name not in permanent_objects and o.type == 'MESH']
    if not imported:
        print(f"  No meshes imported for {ship_name}")
        continue

    # Apply blue texture to all imported meshes
    if os.path.exists(tex_path):
        img = bpy.data.images.load(tex_path)
        mat = bpy.data.materials.new(f"{ship_name}_Blue")
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes["Principled BSDF"]
        tex_node = mat.node_tree.nodes.new('ShaderNodeTexImage')
        tex_node.image = img
        mat.node_tree.links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])

        for obj in imported:
            obj.data.materials.clear()
            obj.data.materials.append(mat)

    # Compute bounding box across all imported meshes
    all_verts = []
    for obj in imported:
        for v in obj.bound_box:
            all_verts.append(obj.matrix_world @ Vector(v))

    mn = Vector((min(v.x for v in all_verts), min(v.y for v in all_verts), min(v.z for v in all_verts)))
    mx = Vector((max(v.x for v in all_verts), max(v.y for v in all_verts), max(v.z for v in all_verts)))
    center = (mn + mx) / 2
    size = mx - mn
    max_dim = max(size.x, size.y, size.z)
    print(f"  Size: {size.x:.1f} x {size.y:.1f} x {size.z:.1f}")

    # Position ground
    ground.location.z = mn.z - 0.2

    # Frame camera
    cam_dist = max_dim * 2.0
    cam_pos = Vector((cam_dist * 0.7, cam_dist * -0.5, cam_dist * 0.35)) + center
    cam.location = cam_pos
    cam.rotation_euler = (center - cam_pos).to_track_quat('-Z', 'Y').to_euler()

    # Render
    scene.render.filepath = os.path.join(OUT_DIR, f"{ship_name.lower()}.png")
    bpy.ops.render.render(write_still=True)
    print(f"  Rendered: {ship_name.lower()}.png")

print("\n=== ALL DONE ===")
