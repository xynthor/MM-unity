from pathlib import Path
from PIL import Image
import re
ROOT=Path(r'C:\MMUnityPort')
ED=ROOT/'Assets'/'Editor'
BIOME=ROOT/'Assets'/'EnvironmentAssets'/'Biomes'
regions=['SweetWater','ParadiseValley','HermitsIsle','Kriegspire','Blackshire','Dragonsand','FrozenHighlands','FreeHaven','MireOfTheDamned','SilverCove','BootlegBay','CastleIronfist','EelInfestedWaters','MistyIslands','NewSorpigal']
kind={'SweetWater':'ash','ParadiseValley':'temperate','HermitsIsle':'rocky','Kriegspire':'darkforest','Blackshire':'darkforest','Dragonsand':'desert','FrozenHighlands':'snow','FreeHaven':'temperate','MireOfTheDamned':'swamp','SilverCove':'temperate','BootlegBay':'tropical','CastleIronfist':'temperate','EelInfestedWaters':'tropical','MistyIslands':'tropical','NewSorpigal':'temperate'}
# RGBA for the second broadleaf tree
src=ROOT/'Assets'/'EnvironmentAssets'/'PolyHaven'/'TreeSmall02'
diff=Image.open(src/'tree_small_02_leaves_diff_1k.png').convert('RGB')
a=Image.open(src/'tree_small_02_leaves_alpha_1k.png').convert('L')
im=diff.convert('RGBA'); im.putalpha(a); im.save(BIOME/'tree_small_leaves_rgba.png')
print('tree-small RGBA ready')

def tree_picker(b):
    if b=='snow': return 'if(seed%7==0 && e.deadTreePrefab) return e.deadTreePrefab; if(e.pinePrefab) return e.pinePrefab; return e.treeSmallPrefab?e.treeSmallPrefab:e.treePrefab;'
    if b=='desert': return 'if(e.deadTreePrefab) return e.deadTreePrefab; return e.treeSmallPrefab?e.treeSmallPrefab:e.treePrefab;'
    if b=='swamp': return 'int k=Mathf.Abs(seed)%5; if(k==0 && e.deadTreePrefab) return e.deadTreePrefab; if(k==1 && e.pinePrefab) return e.pinePrefab; if(e.treeSmallPrefab && k<=3) return e.treeSmallPrefab; return e.treePrefab;'
    if b=='darkforest': return 'int k=Mathf.Abs(seed)%5; if(k<=1 && e.pinePrefab) return e.pinePrefab; if(k<=3 && e.treeSmallPrefab) return e.treeSmallPrefab; return e.treePrefab;'
    if b=='tropical': return 'return (seed%3==0 && e.treeSmallPrefab)?e.treeSmallPrefab:e.treePrefab;'
    return 'int k=Mathf.Abs(seed)%4; if(k==0 && e.pinePrefab) return e.pinePrefab; if(k<=2 && e.treeSmallPrefab) return e.treeSmallPrefab; return e.treePrefab;'
for region in regions:
    p=ED/f'Build{region}OpenWorld.cs'
    s=p.read_text(encoding='utf-8')
    # Give mountain profiles enough vertical headroom.
    s=s.replace('const float TerrainBaseY = -18f;','const float TerrainBaseY = -24f;')
    s=s.replace('const float TerrainHeight = 240f;','const float TerrainHeight = 320f;')
    # Extend environment asset fields for safe tree variety.
    s=s.replace('public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;','public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, treeSmallPrefab, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;')
    s=s.replace('public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, pineBark, pineLeaves, shrubMaterial, fernMaterial;','public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, treeSmallBark, treeSmallLeaves, pineBark, pineLeaves, shrubMaterial, fernMaterial;')
    load='e.deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/dead_tree_trunk_02/dead_tree_trunk_02_1k.fbx");'
    if 'treeSmallPrefab = AssetDatabase' not in s:
        s=s.replace(load,load+'\n        e.treeSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbx");')
    mat='e.pineLeaves = CreateCutoutMaterial("PineLeaves", "Assets/EnvironmentAssets/Biomes/pine_twig_rgba.png", 0.42f);'
    if 'e.treeSmallBark =' not in s:
        s=s.replace(mat,'e.treeSmallBark = CreateTexturedMaterial("TreeSmallBark", "Assets/EnvironmentAssets/PolyHaven/TreeSmall02/tree_small_02_branch_diff_1k.png", 0.06f);\n        e.treeSmallLeaves = CreateCutoutMaterial("TreeSmallLeaves", "Assets/EnvironmentAssets/Biomes/tree_small_leaves_rgba.png", 0.42f);\n        '+mat)
    p.write_text(s,encoding='utf-8')
print('asset fields/headroom patched')

for region in regions:
    p=ED/f'Build{region}OpenWorld.cs'; s=p.read_text(encoding='utf-8')
    # Remove unsafe compact-piece scaling; only clearly standalone objects may get +20%.
    s=re.sub(r'bool explicitSmall=.*?;\n\s*bool compact=.*?;\n\s*return \(explicitSmall\|\|compact\)\?1\.20f:1f;',
             'bool explicitSmall=n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("temple")||n.Contains("guild")||n.Contains("fountain")||n.Contains("well")||n.Contains("stable");\n        return explicitSmall?1.20f:1f;',s)
    # Two-sided MM BSP surfaces so camera angles do not expose missing faces.
    s=s.replace('mat.shader = Shader.Find("Standard");\n            mat.name = key;','mat.shader = Shader.Find("Standard");\n            if(mat.HasProperty("_Cull")) mat.SetInt("_Cull",0);\n            mat.name = key;')
    p.write_text(s,encoding='utf-8')
print('architecture safety/two-sided patch applied')

for region in regions:
    p=ED/f'Build{region}OpenWorld.cs'; s=p.read_text(encoding='utf-8'); b=kind[region]
    if 'static GameObject PickTreePrefab' not in s:
        keep='return true;'
        if b=='desert': keep='return Mathf.Abs(seed)%5==0;'
        elif b=='snow': keep='return Mathf.Abs(seed)%6!=0;'
        elif b=='rocky': keep='return Mathf.Abs(seed)%3!=0;'
        helper='    static GameObject PickTreePrefab(EnvAssets e,int seed)\n    { '+tree_picker(b)+' }\n\n    static bool KeepTreeAnchor(int seed)\n    { '+keep+' }\n\n'
        s=s.replace('    static void BuildVegetation(Transform parent, Terrain terrain, EnvAssets e)',helper+'    static void BuildVegetation(Transform parent, Terrain terrain, EnvAssets e)')
    s=s.replace('if((name.StartsWith("6tree")||name.StartsWith("tree"))&&e.treePrefab)','if(name.StartsWith("6tree")||name.StartsWith("tree"))')
    old='{\n                if(!IsSuitableNaturalSpot(terrain,architectureRoot,x,z,true,false)){skippedTrees++;continue;}'
    new='{\n                var anchorPrefab=PickTreePrefab(e,i); if(!anchorPrefab || !KeepTreeAnchor(i)){skippedTrees++;continue;}\n                if(!IsSuitableNaturalSpot(terrain,architectureRoot,x,z,true,false)){skippedTrees++;continue;}'
    s=s.replace(old,new)
    s=s.replace('SpawnTree(e.treePrefab,trees.transform,e,x,z,SampleTerrainY(terrain,x,z),yaw,','SpawnTree(anchorPrefab,trees.transform,e,x,z,SampleTerrainY(terrain,x,z),yaw,')
    s=s.replace('SpawnTree(e.treePrefab,trees.transform,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(i*109+k)*360f,','SpawnTree(PickTreePrefab(e,i*109+k),trees.transform,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(i*109+k)*360f,')
    extras={'desert':'0','snow':'1+(i%2)','swamp':'2+(i%3)','darkforest':'2+(i%3)','tropical':'2+(i%3)'}.get(b,'1+(i%3)')
    s=s.replace('int extras=1+(i%3);',f'int extras={extras};')
    p.write_text(s,encoding='utf-8')
print('biome tree selection/density patched')

for region in regions:
    p=ED/f'Build{region}OpenWorld.cs'; s=p.read_text(encoding='utf-8')
    old='dst[i]=(mn.Contains("branch")||mn.Contains("leaf")||mn.Contains("canopy"))?e.treeLeaves:e.treeBark;'
    new='if(mn.Contains("atlas")) dst[i]=e.treeAtlas; else if(mn.Contains("twig")||mn.Contains("needle")) dst[i]=e.pineLeaves; else if(mn.Contains("pine")&&mn.Contains("bark")) dst[i]=e.pineBark; else if(mn.Contains("small")&&mn.Contains("leaf")) dst[i]=e.treeSmallLeaves; else if(mn.Contains("small")&&mn.Contains("branch")) dst[i]=e.treeSmallBark; else dst[i]=(mn.Contains("leaf")||mn.Contains("canopy"))?e.treeLeaves:e.treeBark;'
    s=s.replace(old,new)
    p.write_text(s,encoding='utf-8')
print('tree material routing patched')