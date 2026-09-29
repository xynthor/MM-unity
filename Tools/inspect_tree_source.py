import bpy, sys, os
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.fbx_import(filepath=r'C:\MMUnityPort\Assets\EnvironmentAssets\PolyHaven\TreeSmall02\tree_small_02_1k.fbx')
for o in bpy.context.scene.objects:
    if o.type=='MESH': print(o.name, len(o.data.vertices), len(o.data.polygons))
