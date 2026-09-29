import bpy, sys, os
for f in sys.argv[sys.argv.index('--')+1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.fbx_import(filepath=f)
    verts=sum(len(o.data.vertices) for o in bpy.context.scene.objects if o.type=='MESH')
    polys=sum(len(o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH')
    print(os.path.basename(f), 'verts', verts, 'polys', polys)
