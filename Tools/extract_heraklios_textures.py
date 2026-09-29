import bpy, os
src=r"C:\MMUnityPort\Imports\Mixamo\Character\Heraklios By A. Dizon.fbx"
out=r"C:\MMUnityPort\Assets\Player\Hero\Textures"
os.makedirs(out,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
mapping={
 "file1":"Heraklios_Body_Diffuse.png",
 "file15":"Heraklios_Outfit_Diffuse.png",
 "file4":"Heraklios_Normal.png",
 "file5":"Heraklios_Specular.png",
}
for name,fn in mapping.items():
    im=bpy.data.images.get(name)
    if not im:
        print("MISSING",name); continue
    im.filepath_raw=os.path.join(out,fn)
    im.file_format='PNG'
    im.save()
    print("SAVED",fn,im.size[:])
