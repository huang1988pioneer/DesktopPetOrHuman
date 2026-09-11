"""Render one character GLB into the pet's mood poses. Runs inside Blender.

Invoked by tools/render_characters_3d.py, not directly:

    blender --background --factory-startup --python tools/blender_render_pet.py -- \
        --in Tu3D.glb --outdir Assets/Characters3D --name xiaotu

The models are static sculpts with no skeleton, so a mood is expressed by
orienting the model and moving the camera rather than by posing a rig.
Each view is `label:yaw:pitch:zoom:tilt` in degrees, where tilt lays the model
down around its own centre and zoom scales the orthographic frame.
"""

import argparse
import math
import os
import sys

import bpy

DEFAULT_VIEWS = "idle:0:8:1.0:0,happy:-14:2:0.86:-7,sleep:-20:26:1.15:78"
FRAME_SCALE = 2.6


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--in", dest="source", required=True)
    p.add_argument("--outdir", required=True)
    p.add_argument("--name", default=None)
    p.add_argument("--size", type=int, default=640)
    p.add_argument("--samples", type=int, default=64)
    p.add_argument("--views", default=DEFAULT_VIEWS)
    p.add_argument("--engine", default="BLENDER_EEVEE_NEXT")
    return p.parse_args(argv)


def select_only(objects, active=None):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = active or objects[0]


def normalise(objects, target_height=2.0):
    """Flatten the import into one mesh centred and pivoting on its own bounds.

    The glTF importer parents the mesh under a Y-up conversion empty, so the
    object origin sits nowhere near the model. Poses rotate the object about
    its origin, so that origin has to be the middle of the model or the lying
    down pose swings out of frame.
    """
    meshes = [o for o in objects if o.type == "MESH"]
    if not meshes:
        raise SystemExit("model contains no mesh data")

    select_only(meshes)
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    if len(meshes) > 1:
        select_only(meshes)
        bpy.ops.object.join()
    model = bpy.context.view_layer.objects.active

    select_only([model])
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")

    # The importer leaves objects in quaternion mode, where assigning
    # rotation_euler is silently ignored.
    model.rotation_mode = "XYZ"
    model.location = (0.0, 0.0, 0.0)
    model.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()

    scale = target_height / max(model.dimensions.z, 1e-6)
    model.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    return model


def add_lighting():
    """Soft three-point rig so the sculpted volumes still read at 115 px."""
    def area(name, location, rotation, energy, size, colour=(1.0, 1.0, 1.0)):
        data = bpy.data.lights.new(name, type="AREA")
        data.energy = energy
        data.size = size
        data.color = colour
        obj = bpy.data.objects.new(name, data)
        obj.location = location
        obj.rotation_euler = rotation
        bpy.context.scene.collection.objects.link(obj)

    area("Key", (2.6, -3.4, 3.4), (math.radians(52), 0, math.radians(38)), 900, 5.0)
    area("Fill", (-3.2, -2.6, 1.6), (math.radians(74), 0, math.radians(-52)), 260,
         6.0, (0.86, 0.90, 1.0))
    area("Rim", (-1.0, 3.6, 3.0), (math.radians(122), 0, math.radians(196)), 420,
         4.0, (1.0, 0.95, 0.88))

    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    background = world.node_tree.nodes["Background"]
    background.inputs[0].default_value = (1.0, 1.0, 1.0, 1.0)
    background.inputs[1].default_value = 0.35
    bpy.context.scene.world = world


def add_camera():
    data = bpy.data.cameras.new("Camera")
    data.type = "ORTHO"
    data.ortho_scale = FRAME_SCALE
    obj = bpy.data.objects.new("Camera", data)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.scene.camera = obj
    return obj


def place_camera(cam, yaw_deg, pitch_deg, distance=8.0):
    yaw = math.radians(yaw_deg)
    pitch = math.radians(pitch_deg)
    cam.location = (
        distance * math.sin(yaw) * math.cos(pitch),
        -distance * math.cos(yaw) * math.cos(pitch),
        distance * math.sin(pitch),
    )
    cam.rotation_euler = (math.radians(90) - pitch, 0.0, yaw)


def configure_render(engine, size, samples):
    scene = bpy.context.scene
    try:
        scene.render.engine = engine
    except TypeError:
        scene.render.engine = "CYCLES"

    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.compression = 90

    if scene.render.engine == "CYCLES":
        scene.cycles.samples = samples
        scene.cycles.use_denoising = True
    else:
        for attr, value in (("taa_render_samples", samples),
                            ("use_raytracing", True),
                            ("use_shadows", True)):
            if hasattr(scene.eevee, attr):
                setattr(scene.eevee, attr, value)

    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"


def parse_views(spec):
    views = []
    for chunk in spec.split(","):
        parts = chunk.split(":")
        if len(parts) != 5:
            raise SystemExit(f"bad view spec: {chunk}")
        label, yaw, pitch, zoom, tilt = parts
        views.append((label, float(yaw), float(pitch), float(zoom), float(tilt)))
    return views


def main():
    args = parse_args()
    name = args.name or os.path.splitext(os.path.basename(args.source))[0]
    os.makedirs(args.outdir, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=args.source)
    model = normalise([o for o in bpy.data.objects if o not in before])

    add_lighting()
    cam = add_camera()
    configure_render(args.engine, args.size, args.samples)

    for label, yaw, pitch, zoom, tilt in parse_views(args.views):
        model.rotation_euler = (0.0, math.radians(tilt), 0.0)
        bpy.context.view_layer.update()

        cam.data.ortho_scale = FRAME_SCALE * zoom
        place_camera(cam, yaw, pitch)

        out = os.path.join(args.outdir, f"{name}_{label}.png")
        bpy.context.scene.render.filepath = out
        bpy.ops.render.render(write_still=True)
        print(f"WROTE {out}", flush=True)


main()
