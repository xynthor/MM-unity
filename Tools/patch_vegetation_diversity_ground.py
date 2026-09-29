from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticVegetationPass.cs")
s=p.read_text(encoding="utf-8-sig")

s=s.replace('''    static GameObject[] DesertPool()=>LoadMany(
        "Assets/Environment/DesertVegetation/Cactus.fbx",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");''',
'''    static GameObject[] DesertPool()=>LoadMany(
        "Assets/Environment/DesertVegetation/Cactus.fbx",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_01/shrub_01_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_03/shrub_03_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_04/shrub_04_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/wild_rooibos_bush/wild_rooibos_bush_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");''')

s=s.replace('''    static GameObject[] AridPool()=>LoadMany(
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Tree_Dead_001/tree_dead_001.prefab",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");''',
'''    static GameObject[] AridPool()=>LoadMany(
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Tree_Dead_001/tree_dead_001.prefab",
        "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_01/shrub_01_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_03/shrub_03_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_04/shrub_04_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/wild_rooibos_bush/wild_rooibos_bush_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");''')

# Generic material setup for newly downloaded shrubs/bushes.
needle='''        else if(p.Contains("quiver_tree_01"))
        {
            string b="Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/textures/";
            wood=Mat("QuiverTree01_Wood",b+"quiver_tree_01_trunk_diff_1k.png",b+"quiver_tree_01_trunk_nor_gl_1k.png");
            leaf=Mat("QuiverTree01_Leaves",b+"quiver_tree_01_leaf_diff_1k.png",b+"quiver_tree_01_leaf_nor_gl_1k.png",b+"quiver_tree_01_leaf_diff_1k.png");
        }
        if(!wood)return;'''
repl='''        else if(p.Contains("quiver_tree_01"))
        {
            string b="Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/textures/";
            wood=Mat("QuiverTree01_Wood",b+"quiver_tree_01_trunk_diff_1k.png",b+"quiver_tree_01_trunk_nor_gl_1k.png");
            leaf=Mat("QuiverTree01_Leaves",b+"quiver_tree_01_leaf_diff_1k.png",b+"quiver_tree_01_leaf_nor_gl_1k.png",b+"quiver_tree_01_leaf_diff_1k.png");
        }
        else
        {
            string[] names={"shrub_01","shrub_03","shrub_04","wild_rooibos_bush"};
            foreach(string n in names)if(p.Contains(n))
            {
                string b="Assets/Environment/PolyHaven/Downloaded/"+n+"/textures/";
                wood=Mat("DL_"+n,b+n+"_diff_1k.jpg",b+n+"_nor_gl_1k.exr",b+n+"_alpha_1k.png");
                leaf=wood;break;
            }
        }
        if(!wood)return;'''
if needle not in s: raise SystemExit("download material insertion missing")
s=s.replace(needle,repl,1)

# Ground exactly at the anchor X/Z, never at an offset mesh center.
old='''    static void Ground(GameObject go,Terrain t)
    {
        var b=BoundsOf(go);if(b.size==Vector3.zero)return;
        float y=t.SampleHeight(new Vector3(b.center.x,0,b.center.z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(y-b.min.y);
    }'''
new='''    static void Ground(GameObject go,Terrain t)
    {
        var b=BoundsOf(go);if(b.size==Vector3.zero)return;
        float y=t.SampleHeight(new Vector3(go.transform.position.x,0,go.transform.position.z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(y-b.min.y);
    }'''
if old not in s: raise SystemExit("ground block missing")
s=s.replace(old,new,1)

p.write_text(s,encoding="utf-8")
print("VEGETATION_DIVERSITY_GROUND_PATCHED")
