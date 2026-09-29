import bpy, json
p=r"C:\MMUnityPort\Imports\Mixamo\Character\Heraklios By A. Dizon.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=p)
out={"objects":[],"materials":[],"images":[]}
for o in bpy.context.scene.objects:
    rec={"name":o.name,"type":o.type}
    if o.type=="MESH":
        rec.update(vertices=len(o.data.vertices),polygons=len(o.data.polygons),materials=[m.name if m else None for m in o.data.materials])
    elif o.type=="ARMATURE":
        rec.update(bone_count=len(o.data.bones),bones=[b.name for b in o.data.bones][:20])
    out["objects"].append(rec)
for m in bpy.data.materials: out["materials"].append(m.name)
for im in bpy.data.images: out["images"].append({"name":im.name,"packed":bool(im.packed_file),"size":list(im.size),"filepath":im.filepath})
print(json.dumps(out,indent=2))
