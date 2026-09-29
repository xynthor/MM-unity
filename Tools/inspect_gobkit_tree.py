import bpy
from pathlib import Path
p=Path(r'C:\MMUnityPort\External\gobkit-free-assets\nature\TreeHigh001.glb')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(p))
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        print('OBJ',o.name,'dim',tuple(round(x,3) for x in o.dimensions),'rot',tuple(round(x,3) for x in o.rotation_euler))
        for m in o.data.materials:
            print('MAT',m.name,'diffuse',tuple(round(x,3) for x in m.diffuse_color))
            if m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type=='TEX_IMAGE' and n.image:
                        print('IMG',n.image.name,n.image.filepath,n.image.size[:])
