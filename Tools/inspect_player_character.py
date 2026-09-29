import bpy, json, os, sys
p=r"C:\Users\batsi\Desktop\PlayerCharacter\player_character.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=p)
out={"objects":[],"materials":[],"images":[]}
for o in bpy.context.scene.objects:
    rec={"name":o.name,"type":o.type}
    if o.type=="MESH":
        rec.update(vertices=len(o.data.vertices),polygons=len(o.data.polygons),materials=[m.name if m else None for m in o.data.materials])
    if o.type=="ARMATURE":
        rec.update(bones=[b.name for b in o.data.bones],bone_count=len(o.data.bones))
    out["objects"].append(rec)
for m in bpy.data.materials:
    out["materials"].append({"name":m.name,"use_nodes":m.use_nodes})
for im in bpy.data.images:
    out["images"].append({"name":im.name,"filepath":im.filepath,"packed":bool(im.packed_file),"size":list(im.size)})
print(json.dumps(out,indent=2))
