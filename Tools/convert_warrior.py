import bpy
from pathlib import Path
src = Path(r'C:\MMUnityPort\External\cairnfall-public\assets\vendor\quaternius_rpg\Warrior.gltf')
outdir = Path(r'C:\MMUnityPort\Assets\Player\Warrior')
outdir.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(src))
for obj in bpy.context.scene.objects:
    obj.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=str(outdir / 'MM_Warrior.fbx'),
    use_selection=False,
    add_leaf_bones=False,
    path_mode='COPY',
    embed_textures=True,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_simplify_factor=0.0,
)
print('EXPORTED', outdir / 'MM_Warrior.fbx')
