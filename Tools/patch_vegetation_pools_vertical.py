from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticVegetationPass.cs")
s=p.read_text(encoding="utf-8-sig")

# Add more existing local models to the green pool; downloaded broadleaf models already included.
s=s.replace('''        "Assets/Environment/PolyHaven/Downloaded/tree_small_02/tree_small_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/island_tree_02/island_tree_02_1k.fbx");''',
'''        "Assets/Environment/PolyHaven/Downloaded/tree_small_02/tree_small_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/island_tree_02/island_tree_02_1k.fbx",
        "Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx",
        "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx");''')

needle='''    static GameObject[] DesertPool()=>LoadMany(
        "Assets/Environment/DesertVegetation/Cactus.fbx",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");
'''
insert='''    static GameObject[] DesertPool()=>LoadMany(
        "Assets/Environment/DesertVegetation/Cactus.fbx",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");

    static GameObject[] SnowPool()=>LoadMany(
        "Assets/Environment/WinterVegetation/WinterTree5/winter-tree5_LOW_RES.fbx",
        "Assets/Environment/WinterVegetation/WinterTree8/winter-tree8_LOW_RES.fbx")
        .Concat(GreenPool()).Where(x=>x).Distinct().ToArray();

    static GameObject[] AridPool()=>LoadMany(
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Tree_Dead_001/tree_dead_001.prefab",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");
'''
if needle not in s: raise SystemExit("pool insertion missing")
s=s.replace(needle,insert,1)

# Texture validation: any assigned texture property counts, not only mainTexture.
old='''            var ms=r.sharedMaterials;if(ms==null||ms.Length==0)return false;
            foreach(var m in ms)if(!m||!m.mainTexture)return false;'''
new='''            var ms=r.sharedMaterials;if(ms==null||ms.Length==0)return false;
            foreach(var m in ms)
            {
                if(!m)return false;
                bool hasTex=false;
                foreach(var pn in m.GetTexturePropertyNames())if(m.GetTexture(pn)){hasTex=true;break;}
                if(!hasTex)return false;
            }'''
if old not in s: raise SystemExit("texture validation missing")
s=s.replace(old,new,1)

# Region-aware north/south terrain classification.
s=s.replace('''        int sx=CellX(x),sy=CellY(z);byte raw=tile[sy*N+sx];
        return MMRealisticTerrainBiomePass.BaseClass(zone,grp[raw],sem[raw]);''',
'''        int sx=CellX(x),sy=CellY(z);byte raw=tile[sy*N+sx];
        return MMRealisticTerrainBiomePass.BaseClassAt(zone,grp[raw],sem[raw],sy);''')

# Spawn vertical threshold.
old='''        ratio=VerticalRatio(wrap);
        if(ratio<.35f||!TexturesOK(wrap))
        {
            UnityEngine.Object.DestroyImmediate(wrap);
            return null;
        }'''
new='''        ratio=VerticalRatio(wrap);
        string low=ap.ToLowerInvariant();
        bool shrubLike=low.Contains("shrub")||low.Contains("cactus")||low.Contains("quiver_tree");
        float minRatio=shrubLike?.22f:.70f;
        if(ratio<minRatio||!TexturesOK(wrap))
        {
            UnityEngine.Object.DestroyImmediate(wrap);
            return null;
        }'''
if old not in s: raise SystemExit("spawn threshold missing")
s=s.replace(old,new,1)

p.write_text(s,encoding="utf-8")
print("VEGETATION_POOLS_VERTICAL_PATCHED")
