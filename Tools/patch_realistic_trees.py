from pathlib import Path
import re
root=Path(r'C:\MMUnityPort\Assets\Editor')
exclude={'BuildNewSorpigalOpenWorld.cs','BuildCastleIronfistOpenWorld.cs','BuildDragonIsleOpenWorld.cs','BuildEnrothLinkedOpenWorld.cs'}
files=[p for p in root.glob('Build*OpenWorld.cs') if p.name not in exclude]
for p in files:
    s=p.read_text(encoding='utf-8'); old=s
    s=re.sub(r'e\.treePrefab\s*=\s*AssetDatabase\.LoadAssetAtPath<GameObject>\([^;]+;', 'e.treePrefab = null;', s)
    s=re.sub(r'e\.treePrefab2\s*=\s*AssetDatabase\.LoadAssetAtPath<GameObject>\([^;]+;', 'e.treePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_1.FBX");', s)
    s=re.sub(r'e\.treePrefab3\s*=\s*AssetDatabase\.LoadAssetAtPath<GameObject>\([^;]+;', 'e.treePrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_01.FBX");', s)
    s=re.sub(r'e\.treePrefab4\s*=\s*AssetDatabase\.LoadAssetAtPath<GameObject>\([^;]+;', 'e.treePrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_01.FBX");', s)
    s=re.sub(r'e\.treeSmallPrefab\s*=\s*AssetDatabase\.LoadAssetAtPath<GameObject>\([^;]+;', 'e.treeSmallPrefab = null;', s)
    s=s.replace('e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx");','e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX");')
    s=s.replace('e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");','e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/BroadleafShrub_01/Broadleaf_Shrub_01_Var1.FBX");')
    s=s.replace('e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/fern_02/fern_02_1k.fbx");','e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Fern_var01.FBX");')
    if s!=old:
        p.write_text(s,encoding='utf-8')
        print('patched',p.name)
print('done',len(files))
