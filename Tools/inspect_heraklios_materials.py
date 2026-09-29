import bpy, json
p=r"C:\MMUnityPort\Imports\Mixamo\Character\Heraklios By A. Dizon.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=p)
for m in bpy.data.materials:
    print("MAT",m.name)
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type=="TEX_IMAGE" and n.image:
                print(" IMG",n.name,n.image.name,n.image.filepath)
            elif n.type=="BSDF_PRINCIPLED":
                print(" PRINCIPLED",n.name)
