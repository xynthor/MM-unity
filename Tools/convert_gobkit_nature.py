import bpy
from pathlib import Path
src = Path(r'C:\MMUnityPort\External\gobkit-free-assets\nature')
out = Path(r'C:\MMUnityPort\Assets\EnvironmentAssets\Gobkit')
out.mkdir(parents=True, exist_ok=True)
files = ['TreeHigh001.glb','TreeHigh002.glb','TreeHigh003.glb','Rock001.glb','Rock002.glb','Rock003.glb']
for name in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(src / name))
    bpy.ops.export_scene.fbx(
        filepath=str(out / (Path(name).stem + '.fbx')),
        use_selection=False,
        add_leaf_bones=False,
        path_mode='COPY',
        embed_textures=True,
        bake_anim=False,
    )
    print('EXPORTED', name)
