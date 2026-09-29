from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=['BuildNewSorpigalOpenWorld.cs','BuildCastleIronfistOpenWorld.cs','BuildMistyIslandsOpenWorld.cs','BuildBootlegBayOpenWorld.cs']
old='''            var src = r.sharedMaterials;\n            int n = (src == null || src.Length == 0) ? 1 : src.Length;\n            var dst = new Material[n];\n            for (int i=0;i<n;i++) dst[i] = e.treeAtlas ? e.treeAtlas : e.treeLeaves;\n            r.sharedMaterials = dst;'''
new='''            var src = r.sharedMaterials;\n            int n = (src == null || src.Length == 0) ? 1 : src.Length;\n            var dst = new Material[n];\n            for (int i=0;i<n;i++)\n            {\n                string mn=(src!=null && i<src.Length && src[i])?src[i].name.ToLowerInvariant():\"\";\n                dst[i]=(mn.Contains(\"branch\")||mn.Contains(\"leaf\")||mn.Contains(\"canopy\"))?e.treeLeaves:e.treeBark;\n            }\n            r.sharedMaterials = dst;'''
for fn in files:
    p=root/fn; s=p.read_text()
    if old not in s: print('NO_MATCH',fn); continue
    p.write_text(s.replace(old,new,1)); print('PATCHED',fn)
