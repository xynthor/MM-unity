import bpy
from pathlib import Path

src = Path(r"C:\MMUnityPort\Assets\EnvironmentAssets\PolyHaven\TreeSmall02\tree_small_02_1k.fbx")
out = Path(r"C:\MMUnityPort\Assets\EnvironmentAssets\PolyHaven\TreeSmall02\GameLOD2")
out.mkdir(parents=True, exist_ok=True)
levels = [("LOD0", 0.24), ("LOD1", 0.08), ("LOD2", 0.03)]

for label, ratio in levels:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(src))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for obj in meshes:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        mod = obj.modifiers.new(name="GameDecimate", type='DECIMATE')
        mod.decimate_type = 'COLLAPSE'
        mod.ratio = ratio
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in bpy.context.scene.objects:
        obj.select_set(True)
    target = out / f"tree_small_02_{label}.fbx"
    bpy.ops.export_scene.fbx(filepath=str(target), use_selection=True, add_leaf_bones=False,
                             bake_anim=False, path_mode='COPY', embed_textures=False)
    print(label, target)
