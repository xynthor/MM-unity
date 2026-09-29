using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWorldExtensionDressing
{
    const string GROUP="Region Dressing - Reference Matched";
    const string PINE="Assets/TempDragonTemplate/TracePine.prefab";
    const string YOUNG="Assets/TempDragonTemplate/TraceYoungPine.prefab";
    const string SHRUB="Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx";
    const string SHRUBMAT="Assets/Materials/RealisticWorld/Shrub02.mat";
    const string SEARSIA="Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx";
    const string SEARSIA_WOOD="Assets/Environment/ValidatedTrees/Materials/searsia_lucida.mat";
    const string SEARSIA_LEAF="Assets/Environment/ValidatedTrees/Materials/searsia_lucida_leaves.mat";
    const string SEARSIA_TWIG="Assets/Environment/ValidatedTrees/Materials/searsia_lucida_twigs.mat";
    const string ROCK1="Assets/EnvironmentAssets/Gobkit/Rock001.fbx";
    const string ROCK2="Assets/EnvironmentAssets/Gobkit/Rock002.fbx";
    const string ROCKMAT="Assets/Materials/Source3D/Stone.mat";
    const string CACTUS="Assets/Environment/DesertVegetation/Cactus.fbx";

    sealed class DressSpec {
        public string scene; public int pine,young,shrub,searsia,rock,cactus,seed;
    }
    static readonly DressSpec[] SPECS={
        new DressSpec{scene="SweetWater_North",pine=4,young=4,shrub=10,rock=8,seed=8001},
        new DressSpec{scene="Kriegspire_North",pine=6,young=4,shrub=8,rock=18,seed=8002},
        new DressSpec{scene="FrozenHighlands_North",pine=2,young=2,shrub=4,rock=15,seed=8003},
        new DressSpec{scene="SilverCove_North",pine=0,young=0,shrub=8,rock=7,seed=8004},
        new DressSpec{scene="EelInfestedWaters_North",seed=8005},
        new DressSpec{scene="ParadiseValley_West",pine=18,young=25,shrub=80,searsia=185,rock=25,seed=8101},
        new DressSpec{scene="HermitsIsle_West",seed=8102},
        new DressSpec{scene="ArchipelagoOfTheAncients",shrub=100,searsia=165,rock=38,seed=8201},
    };

    static Bounds BoundsOf(GameObject go){
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static float Slope(Terrain t,float x,float z){
        var p=new Vector3(x,0,z)-t.transform.position;var s=t.terrainData.size;
        return t.terrainData.GetSteepness(Mathf.Clamp01(p.x/s.x),Mathf.Clamp01(p.z/s.z));
    }
    static float Height(Terrain t,float x,float z)=>t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;

    static void FitAndGround(GameObject go,Terrain t,float x,float z,float targetSpan,float sink){
        go.transform.position=new Vector3(x,0,z);
        var b=BoundsOf(go);float span=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(span>.001f)go.transform.localScale*=targetSpan/span;
        b=BoundsOf(go);
        go.transform.position+=new Vector3(x-b.center.x,0,z-b.center.z);
        b=BoundsOf(go);float gy=Height(t,x,z);
        go.transform.position+=Vector3.up*(gy-b.min.y-sink);
    }

    static bool MaterialsOK(GameObject go){
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
            if(r.sharedMaterials==null||r.sharedMaterials.Length==0)return false;
            foreach(var m in r.sharedMaterials){
                if(!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader")return false;
                if(!m.GetTexturePropertyNames().Any(n=>m.GetTexture(n)))return false;
            }
        }
        return true;
    }
    static GameObject InstantiateAsset(string path,Transform parent){
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!src)throw new Exception("Missing asset "+path);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);return go;
    }

    static GameObject SpawnPine(string path,Transform parent,Terrain t,System.Random rng,float x,float z,bool young,int idx){
        var go=InstantiateAsset(path,parent);go.name=(young?"YoungPine_":"Pine_")+idx;
        go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
        FitAndGround(go,t,x,z,young?5.0f+(float)rng.NextDouble()*3.0f:8.0f+(float)rng.NextDouble()*5.0f,.03f);
        if(!MaterialsOK(go)){UnityEngine.Object.DestroyImmediate(go);return null;}return go;
    }

    static GameObject SpawnShrub(Transform parent,Terrain t,System.Random rng,float x,float z,int idx){
        var go=InstantiateAsset(SHRUB,parent);go.name="Shrub_"+idx;var mat=AssetDatabase.LoadAssetAtPath<Material>(SHRUBMAT);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
        go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
        FitAndGround(go,t,x,z,.65f+(float)rng.NextDouble()*1.0f,.02f);
        if(!MaterialsOK(go)){UnityEngine.Object.DestroyImmediate(go);return null;}return go;
    }

    static GameObject SpawnSearsia(Transform parent,Terrain t,System.Random rng,float x,float z,int idx,float scaleBoost=1f){
        var go=InstantiateAsset(SEARSIA,parent);go.name="Searsia_"+idx;
        var wood=AssetDatabase.LoadAssetAtPath<Material>(SEARSIA_WOOD);
        var leaf=AssetDatabase.LoadAssetAtPath<Material>(SEARSIA_LEAF);
        var twig=AssetDatabase.LoadAssetAtPath<Material>(SEARSIA_TWIG);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
            var a=r.sharedMaterials;
            for(int i=0;i<a.Length;i++){
                string n=a[i]?a[i].name.ToLowerInvariant():"";
                a[i]=n.Contains("leaf")?leaf:n.Contains("twig")?twig:wood;
            }
            if(a.Length==0)a=new[]{wood};r.sharedMaterials=a;
        }
        go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
        FitAndGround(go,t,x,z,(5.0f+(float)rng.NextDouble()*4.0f)*scaleBoost,.04f);
        if(!MaterialsOK(go)){UnityEngine.Object.DestroyImmediate(go);return null;}return go;
    }

    static GameObject SpawnRock(Transform parent,Terrain t,System.Random rng,float x,float z,int idx){
        var go=InstantiateAsset((idx%2==0)?ROCK1:ROCK2,parent);go.name="Rock_"+idx;
        var mat=AssetDatabase.LoadAssetAtPath<Material>(ROCKMAT);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
        go.transform.rotation=Quaternion.Euler((float)rng.NextDouble()*8f-4f,(float)rng.NextDouble()*360f,(float)rng.NextDouble()*8f-4f);
        float target=.8f+(float)rng.NextDouble()*3.1f;FitAndGround(go,t,x,z,target,Mathf.Min(.35f,target*.12f));
        if(!MaterialsOK(go)){UnityEngine.Object.DestroyImmediate(go);return null;}return go;
    }

    static GameObject SpawnCactus(Transform parent,Terrain t,System.Random rng,float x,float z,int idx){
        var go=InstantiateAsset(CACTUS,parent);go.name="Cactus_"+idx;int chosen=rng.Next(1,10);
        foreach(Transform c in go.transform)c.gameObject.SetActive(c.name.Equals("cactus_"+chosen,StringComparison.OrdinalIgnoreCase));
        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RealisticWorld/Cactus_"+chosen+".mat");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))if(r.gameObject.activeInHierarchy)r.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
        go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
        FitAndGround(go,t,x,z,1.9f+(float)rng.NextDouble()*2.0f,.03f);
        if(!MaterialsOK(go)){UnityEngine.Object.DestroyImmediate(go);return null;}return go;
    }
    static bool FindPoint(Terrain t,System.Random rng,float minH,float maxH,float maxSlope,List<Vector2> used,float spacing,out float x,out float z){
        var p=t.transform.position;var s=t.terrainData.size;
        for(int q=0;q<350;q++){
            x=p.x+7f+(float)rng.NextDouble()*(s.x-14f);
            z=p.z+7f+(float)rng.NextDouble()*(s.z-14f);
            float h=Height(t,x,z),sl=Slope(t,x,z);if(h<minH||h>maxH||sl>maxSlope)continue;
            var v=new Vector2(x,z);if(used.Any(u=>(u-v).sqrMagnitude<spacing*spacing))continue;
            return true;
        }
        x=z=0;return false;
    }

    static int SpawnMany(Terrain t,Transform g,System.Random rng,string kind,int target,List<Vector2> used,float treeBoost=1f){
        int n=0,tries=0;
        while(n<target&&tries++<target*20+200){
            float maxSlope=kind=="Rock"?43f:kind=="Cactus"?24f:28f;
            float minH=kind=="Rock"?.35f:.75f;
            float maxH=kind=="Cactus"?42f:kind=="Searsia"?28f:kind=="Shrub"?45f:70f;
            float spacing=kind=="Rock"?5f:kind=="Shrub"?4.5f:kind=="Cactus"?6f:kind=="Searsia"?8.5f:8f;
            if(!FindPoint(t,rng,minH,maxH,maxSlope,used,spacing,out float x,out float z))break;
            GameObject go=null;
            if(kind=="Pine")go=SpawnPine(PINE,g,t,rng,x,z,false,n);
            else if(kind=="Young")go=SpawnPine(YOUNG,g,t,rng,x,z,true,n);
            else if(kind=="Shrub")go=SpawnShrub(g,t,rng,x,z,n);
            else if(kind=="Searsia")go=SpawnSearsia(g,t,rng,x,z,n,treeBoost);
            else if(kind=="Rock")go=SpawnRock(g,t,rng,x,z,n);
            else if(kind=="Cactus")go=SpawnCactus(g,t,rng,x,z,n);
            if(go){used.Add(new Vector2(x,z));n++;}
        }
        return n;
    }


    static int ClusterArchipelagoTrees(Terrain t,Transform group,System.Random rng,int target,List<Vector2> used){
        // Vegetation follows the four forested islands, not the pale central sandy island.
        var centers=new []{
            new Vector2(-81f,14f),new Vector2(-33f,31f), // west island
            new Vector2(0f,86f),new Vector2(45f,59f),   // north forest
            new Vector2(-43f,-68f),new Vector2(4f,-81f),// south island
            new Vector2(122f,36f)                      // eastern island
        };
        int n=0,tries=0;
        while(n<target&&tries++<target*190){
            int c=rng.Next(centers.Length);
            float a=(float)rng.NextDouble()*Mathf.PI*2f;
            float rad=2f+Mathf.Sqrt((float)rng.NextDouble())*30f;
            Vector2 shift=c<=1?new Vector2(35f,10f):c<=3?new Vector2(-2f,17f):
                c<=5?new Vector2(-8f,-26f):new Vector2(59f,16f);
            float x=(centers[c].x+shift.x)*.84f-145f+Mathf.Cos(a)*rad;
            float z=(centers[c].y+shift.y)*1.06f+Mathf.Sin(a)*rad;
            if(x<-245f||x>245f||z<-245f||z>245f)continue;
            if(Height(t,x,z)<2.4f||Slope(t,x,z)>27f)continue;
            var v=new Vector2(x,z);
            if(used.Any(u=>(v-u).sqrMagnitude<29f))continue;
            var go=SpawnSearsia(group,t,rng,x,z,n);
            if(go){used.Add(v);n++;}
        }
        return n;
    }

    static void DressOne(DressSpec s,List<string> report){
        string path="Assets/Scenes/"+s.scene+".unity";
        var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
        if(!root)throw new Exception("Edge root missing "+s.scene);
        var t=root.GetComponentInChildren<Terrain>(true);if(!t)throw new Exception("Edge terrain missing "+s.scene);
        var old=root.transform.Find(GROUP);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var group=new GameObject(GROUP);group.transform.SetParent(root.transform,false);
        var rng=new System.Random(s.seed);var used=new List<Vector2>();
        int pine=SpawnMany(t,group.transform,rng,"Pine",s.pine,used);
        int young=SpawnMany(t,group.transform,rng,"Young",s.young,used);
        int searsia=s.scene=="ArchipelagoOfTheAncients" ? ClusterArchipelagoTrees(t,group.transform,rng,s.searsia,used) : SpawnMany(t,group.transform,rng,"Searsia",s.searsia,used,
            s.scene=="ParadiseValley_West"?1.55f:1f);
        int shrub=SpawnMany(t,group.transform,rng,"Shrub",s.shrub,used);
        int rock=SpawnMany(t,group.transform,rng,"Rock",s.rock,used);
        int cactus=SpawnMany(t,group.transform,rng,"Cactus",s.cactus,used);

        int bad=0,oversize=0;
        foreach(Transform c in group.transform){
            if(!MaterialsOK(c.gameObject))bad++;
            var b=BoundsOf(c.gameObject);if(Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))>15f)oversize++;
        }
        EditorSceneManager.MarkSceneDirty(sc);if(!EditorSceneManager.SaveScene(sc,path))throw new IOException("Save failed "+path);
        report.Add($"{s.scene},{pine},{young},{shrub},{searsia},{rock},{cactus},{bad},{oversize}");
    }

    const string DRAGON_GROUP="Reference Woodland - Dragon Isle";
    static void DressDragonOne(string half,List<string> report){
        string path="Assets/Scenes/DragonIsle_"+half+".unity";
        var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.Contains("Open World"));
        if(!root)throw new Exception("Dragon scene root missing: "+half);
        var t=root.GetComponentInChildren<Terrain>(true);
        if(!t)throw new Exception("Dragon terrain missing: "+half);
        var mask=MMReferenceEdgeProfiles.Load("DragonIsle_"+half);
        if(mask==null)throw new Exception("Dragon island reference mask missing: "+half);
        var old=root.transform.Find(DRAGON_GROUP);
        if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var group=new GameObject(DRAGON_GROUP);
        group.transform.SetParent(root.transform,false);
        var rng=new System.Random(20260924+(half=="North"?101:202));
        var used=new List<Vector2>();
        int trees=0,pines=0,shrubs=0;
        bool north=half=="North";
        // Keep the pointed northern cape and dry rocky tip open, wooded lower
        // Dragon Isle and the separate eastern Sweet Water mainland only.
        int treeTarget=north?32:155, pineTarget=north?8:18,shrubTarget=north?18:75;
        int targetTotal=treeTarget+pineTarget+shrubTarget;
        int attempts=0;
        while(trees+pines+shrubs<targetTotal&&attempts++<targetTotal*180){
            float x=-239f+(float)rng.NextDouble()*478f;
            float z=-239f+(float)rng.NextDouble()*478f;
            int ix=Mathf.Clamp(Mathf.RoundToInt(x+256f),0,512);
            int iz=Mathf.Clamp(Mathf.RoundToInt(z+256f),0,512);
            float signed=(mask.At(ix,iz).r-128f)*512f/(148f*3f);
            if(signed<11f)continue;
            bool island=ix<343;
            if(north){
                if(!island||iz>215)continue;
            }else {
                if(island&&iz>372)continue;
                if(!island&&iz>420)continue;
            }
            float h=Height(t,x,z),slope=Slope(t,x,z);
            if(h<1.6f||h>48f||slope>27f)continue;
            var v=new Vector2(x,z);
            bool pine=pines<pineTarget && (trees>=treeTarget||rng.NextDouble()<.11);
            bool shrub=!pine && shrubs<shrubTarget &&
                (trees>=treeTarget||rng.NextDouble()<.33);
            float spacing=shrub?3.8f:pine?8.0f:6.8f;
            if(used.Any(u=>(u-v).sqrMagnitude<spacing*spacing))continue;
            GameObject go;
            if(pine){
                go=SpawnPine((rng.NextDouble()<.27)?YOUNG:PINE,group.transform,t,rng,x,z,false,pines);
                if(go)pines++;
            }else if(shrub){
                go=SpawnShrub(group.transform,t,rng,x,z,shrubs);
                if(go)shrubs++;
            }else if(trees<treeTarget){
                go=SpawnSearsia(group.transform,t,rng,x,z,trees,north?1.15f:1.60f);
                if(go)trees++;
            }else continue;
            if(go)used.Add(v);
        }
        int bad=0;
        foreach(Transform child in group.transform)
            if(!MaterialsOK(child.gameObject))bad++;
        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,path))
            throw new IOException("Cannot save Dragon woodland "+half);
        report.Add("DragonIsle_"+half+",searsia="+trees+",pine="+pines+
            ",shrub="+shrubs+",bad_materials="+bad);
    }
    [MenuItem("MMUnity/World/Reference Dragon Isle and Sweet Water Woodland")]
    public static void DressDragonAll(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>();
        DressDragonOne("North",report);
        DressDragonOne("South",report);
        MMWorldExtensionVegetationTransition.ApplyAll();
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        File.WriteAllLines("Validation/EdgeGrid20260923/dragon_reference_woodland.csv",report);
        Debug.Log("DRAGON_REFERENCE_WOODLAND_COMPLETE "+string.Join(" | ",report));
    }

    [MenuItem("MMUnity/World/Redress Dragon South Coast Only")]
    public static void DressDragonSouthOnly(){
        var rows=new List<string>();
        DressDragonOne("South",rows);
        MMWorldExtensionVegetationTransition.ApplyDragonSouthOnly();
        File.WriteAllLines("Validation/EdgeGrid20260923/dragon_south_coast_redress.txt",rows);
        AssetDatabase.SaveAssets();
        Debug.Log("DRAGON_SOUTH_COAST_REDRESS "+string.Join(" | ",rows));
    }

    [MenuItem("MMUnity/World/Dress Silver Cove North Only")]
    public static void DressSilverOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,pines,young_pines,shrubs,searsia,rocks,cacti,bad_material_objects,oversize_gt15m"};
        DressOne(SPECS.Single(s=>s.scene=="SilverCove_North"),report);
        File.WriteAllLines("Validation/EdgeGrid20260923/silver_north_reference_dressing.csv",report);
        AssetDatabase.SaveAssets();
        Debug.Log("SILVER_NORTH_REFERENCE_DRESSED");
    }

    [MenuItem("MMUnity/World/Dress Southeast Archipelago Only")]
    public static void DressSoutheastOnly(){
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var rows=new List<string>{"scene,pines,young_pines,shrubs,searsia,rocks,cacti,bad_material_objects,oversize_gt15m"};
        DressOne(SPECS.Last(),rows);
        MMWorldExtensionVegetationTransition.ApplyAll();
        File.WriteAllLines("Validation/CactusDragonAudit/archipelago_reference_dressing.csv",rows);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("MMUnity/World/Dress Paradise Valley West Only")]
    public static void DressParadiseWestOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,pines,young_pines,shrubs,searsia,rocks,cacti,bad_material_objects,oversize_gt15m"};
        DressOne(SPECS.Single(s=>s.scene=="ParadiseValley_West"),report);
        MMWorldExtensionVegetationTransition.ApplyAll();
        File.WriteAllLines("Validation/EdgeGrid20260923/visible_paradise_west_dressing.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("PARADISE_VISIBLE_WEST_COAST_DRESSED");
    }

    [MenuItem("MMUnity/World/Dress Smoothed North and Western Coast")]
    public static void DressWestNorthOnly(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var report=new List<string>{"scene,pines,young_pines,shrubs,searsia,rocks,cacti,bad_material_objects,oversize_gt15m"};
        foreach(var s in SPECS.Where(s=>s.scene=="ParadiseValley_West"||
            (s.scene=="SweetWater_North"||s.scene=="Kriegspire_North"||
             s.scene=="FrozenHighlands_North"||s.scene=="SilverCove_North")))
            DressOne(s,report);
        MMWorldExtensionVegetationTransition.ApplyAll();
        File.WriteAllLines("Validation/EdgeGrid20260923/west_north_smooth_dressing.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("WEST_NORTH_SMOOTH_DRESSING_DONE count=5");
    }

    [MenuItem("MMUnity/World/Dress World Extension Tiles")]
    public static void Run(){
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var report=new List<string>{"scene,pines,young_pines,shrubs,searsia,rocks,cacti,bad_material_objects,oversize_gt15m"};
        foreach(var s in SPECS)DressOne(s,report);
        MMWorldExtensionVegetationTransition.ApplyAll();
        File.WriteAllLines("Validation/CactusDragonAudit/world_extension_dressing.csv",report);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("WORLD_EXTENSION_DRESSING_DONE tiles="+SPECS.Length);
    }
}
