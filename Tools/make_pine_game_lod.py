import bpy, sys, os
from pathlib import Path

src = Path(r"C:\MMUnityPort\Assets\Environment\PolyHaven\Models\pine_sapling_small\pine_sapling_small_1k.fbx")
dst = Path(r"C:\MMUnityPort\Assets\Environment\Generated\pine_sapling_game.fbx")
dst.parent.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(src))

mesh_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
print('imported meshes', [(o.name, len(o.data.polygons)) for o in mesh_objs])

for obj in mesh_objs:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    mod = obj.modifiers.new(name='MMUnity_GameLOD', type='DECIMATE')
    mod.decimate_type = 'COLLAPSE'
    mod.ratio = 0.12
    mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    print('decimated', obj.name, len(obj.data.polygons))
    obj.select_set(False)
bpy.ops.export_scene.fbx(
    filepath=str(dst),
    use_selection=False,
    object_types={'MESH'},
    apply_scale_options='FBX_SCALE_ALL',
    path_mode='RELATIVE',
    embed_textures=False,
    add_leaf_bones=False,
    bake_anim=False,
)
print('wrote', dst, dst.stat().st_size)
