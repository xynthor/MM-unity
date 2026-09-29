import bpy, json
files=[
r"C:\MMUnityPort\Assets\Player\Hero\Animations\Locomotion Pack\idle.fbx",
r"C:\MMUnityPort\Assets\Player\Hero\Animations\Pro Melee Axe Pack\standing melee attack horizontal.fbx"]
for p in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=p)
    print("FILE",p)
    for o in bpy.context.scene.objects:
        if o.type=="ARMATURE":
            print("ARM",o.name,"bones",len(o.data.bones))
            print([b.name for b in o.data.bones][:80])
        elif o.type=="MESH":
            print("MESH",o.name,len(o.data.vertices))
