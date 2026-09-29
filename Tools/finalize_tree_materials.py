from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=list(root.glob('Build*OpenWorld.cs'))
files=[p for p in files if p.name!='BuildEnrothFullWorld.cs' and 'BuildEnrothOpenWorld' not in p.name]
for p in files:
    s=p.read_text(encoding='utf-8')
    s=s.replace('Assets/EnvironmentAssets/PolyHaven/TreeSmall02/tree_small_02_branch_diff_1k.png','Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbm/tree_small_02_branch_diff_1k.png')
    s=s.replace('ScaleToHeight(go,h);var cc=go.AddComponent<CapsuleCollider>();','ScaleToHeight(go,h);AssignTreeMaterials(go,e);var cc=go.AddComponent<CapsuleCollider>();')
    s=s.replace('mn.Contains("leaf")||mn.Contains("canopy")','mn.Contains("leaf")||mn.Contains("canopy")||mn.Contains("branches")')
    p.write_text(s,encoding='utf-8')
print('patched',len(files))