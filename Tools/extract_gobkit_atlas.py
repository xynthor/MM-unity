import bpy
from pathlib import Path
src=Path(r'C:\MMUnityPort\External\gobkit-free-assets\nature\TreeHigh001.glb')
out=Path(r'C:\MMUnityPort\Assets\EnvironmentAssets\Gobkit\TreeAtlas.png')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(src))
for img in bpy.data.images:
    if img.name != 'Render Result':
        img.filepath_raw=str(out)
        img.file_format='PNG'
        img.save()
        print('SAVED',out,img.size[:])
        break
