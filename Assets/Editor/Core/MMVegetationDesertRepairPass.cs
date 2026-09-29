using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMVegetationDesertRepairPass
{
    const int N=128;
    const string MatDir="Assets/Materials/RealisticWorld/VegetationRepair";
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity")};

    sealed class Mats
    {
        public Material bark,leaf,atlas,smallBark,smallLeaf,pineBark,pineLeaf,shrub02,fern,quiverTrunk,quiverLeaf,genericDry;
        public readonly Dictionary<int,Material> cactus=new Dictionary<int,Material>();
        public readonly Dictionary<string,Material> downloadedShrub=new Dictionary<string,Material>(StringComparer.OrdinalIgnoreCase);
    }

    static GameObject LoadGO(string p)
    {
        var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if(g)return g;
        return AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }

    static Material Mat(string name,string diff,string normal=null,bool cutout=false,Color? tint=null)
    {
        Directory.CreateDirectory(MatDir);
        string path=$"{MatDir}/{name}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        var d=string.IsNullOrEmpty(diff)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(diff);
        var n=string.IsNullOrEmpty(normal)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
        if(d)m.SetTexture("_MainTex",d);
        if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        m.color=tint??Color.white;
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.035f);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
        if(cutout)
        {
            m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.38f);m.SetOverrideTag("RenderType","TransparentCutout");
            m.EnableKeyword("_ALPHATEST_ON");m.DisableKeyword("_ALPHABLEND_ON");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=2450;
            if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);
        }
        EditorUtility.SetDirty(m);return m;
    }

    static Mats Materials()
    {
        var m=new Mats();
        m.bark=Mat("TreeBark",null,null,false,new Color(.31f,.18f,.09f));
        m.leaf=Mat("TreeLeaves",null,null,false,new Color(.18f,.48f,.13f));
        m.atlas=Mat("TreeAtlas","Assets/EnvironmentAssets/Gobkit/TreeAtlas.png",null,true);
        m.smallBark=Mat("TreeSmallBark","Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbm/tree_small_02_branch_diff_1k.png");
        m.smallLeaf=Mat("TreeSmallLeaves","Assets/EnvironmentAssets/Biomes/tree_small_leaves_rgba.png",null,true);
        m.pineBark=Mat("PineBark","Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_bark_diff_1k.png");
        m.pineLeaf=Mat("PineLeaves","Assets/EnvironmentAssets/Biomes/pine_twig_rgba.png",null,true);
        m.shrub02=Mat("Shrub02","Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_rgba_1k.png","Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_nor_gl_1k.exr",true);
        m.fern=Mat("Fern02","Assets/Environment/PolyHaven/Models/fern_02/fern_02_rgba_1k.png",null,true);
        m.quiverTrunk=Mat("QuiverTrunk","Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/textures/quiver_tree_01_trunk_diff_1k.png","Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/textures/quiver_tree_01_trunk_nor_gl_1k.png");
        m.quiverLeaf=Mat("QuiverLeaf","Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/textures/quiver_tree_01_leaf_diff_1k.png","Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/textures/quiver_tree_01_leaf_nor_gl_1k.png",true);
        m.genericDry=Mat("GenericDry",null,null,false,new Color(.43f,.35f,.18f));
        for(int i=1;i<=9;i++)m.cactus[i]=Mat("Cactus_"+i,$"Assets/Environment/DesertVegetation/Textures/cactus_{i}__pbrs2a_diffuse.png",$"Assets/Environment/DesertVegetation/Textures/cactus_{i}__pbrs2a_normal.png");
        foreach(string n in new[]{"shrub_01","shrub_03","shrub_04","wild_rooibos_bush"})
            m.downloadedShrub[n]=Mat("Dry_"+n,$"Assets/Environment/PolyHaven/Downloaded/{n}/textures/{n}_diff_1k.jpg",$"Assets/Environment/PolyHaven/Downloaded/{n}/textures/{n}_nor_gl_1k.exr",true);
        return m;
    }

    static Bounds B(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static string SourcePath(GameObject go)
    {
        var src=PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) as GameObject;
        if(!src)src=PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
        return src?AssetDatabase.GetAssetPath(src):"";
    }
    static bool Bad(Material m)
    {
        if(!m||!m.shader)return true;
        string n=m.shader.name??"";
        if(n.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0)return true;
        if(!m.shader.isSupported)return true;
        return false;
    }
    static bool AllMaterialsValid(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled)continue;
            var a=r.sharedMaterials;if(a==null||a.Length==0)return false;
            foreach(var m in a)if(Bad(m))return false;
        }
        return true;
    }
    static int CactusIndex(string s)
    {
        s=(s??"").ToLowerInvariant();
        for(int i=1;i<=9;i++)if(s.Contains("cactus_"+i))return i;
        return 0;
    }
    static void SetAll(Renderer r,Material m)
    {
        int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
        var a=new Material[n];for(int i=0;i<n;i++)a[i]=m;r.sharedMaterials=a;
    }
    static void SafeMaterials(GameObject go,Mats m)
    {
        string p=SourcePath(go).ToLowerInvariant();
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled)continue;
            string rn=(r.gameObject.name+" "+string.Join(" ",(r.sharedMaterials??new Material[0]).Where(x=>x).Select(x=>x.name))).ToLowerInvariant();
            int ci=CactusIndex(rn);
            if(ci>0){SetAll(r,m.cactus[ci]);continue;}
            if(p.Contains("treehigh")){SetAll(r,m.atlas);continue;}
            if(p.Contains("tree_small_02")||p.Contains("treesmall02"))
            {
                SetAll(r,(rn.Contains("leaf")||rn.Contains("foliage"))?m.smallLeaf:m.smallBark);continue;
            }
            if(p.Contains("pine_sapling")||p.Contains("pine_00")||p.Contains("pine_a")||p.Contains("pine_b")||p.Contains("pine_c")||p.Contains("pine_d"))
            {
                SetAll(r,(rn.Contains("twig")||rn.Contains("needle")||rn.Contains("leaf")||rn.Contains("branch"))?m.pineLeaf:m.pineBark);continue;
            }
            if(p.Contains("banyan"))
            {
                SetAll(r,(rn.Contains("leaf")||rn.Contains("canopy")||rn.Contains("branch"))?m.leaf:m.bark);continue;
            }
            if(p.Contains("shrub_02")){SetAll(r,m.shrub02);continue;}
            if(p.Contains("fern_02")){SetAll(r,m.fern);continue;}
            if(p.Contains("quiver_tree_01"))
            {
                SetAll(r,(rn.Contains("leaf")||rn.Contains("branch"))?m.quiverLeaf:m.quiverTrunk);continue;
            }
            bool matched=false;
            foreach(var kv in m.downloadedShrub)if(p.Contains(kv.Key)){SetAll(r,kv.Value);matched=true;break;}
            if(matched)continue;
            if((r.sharedMaterials??new Material[0]).Any(Bad))
                SetAll(r,(rn.Contains("leaf")||rn.Contains("canopy")||rn.Contains("branch")||rn.Contains("shrub")||rn.Contains("fern"))?m.leaf:m.bark);
        }
    }

    static bool IsVegRootName(string n)
    {
        n=(n??"").ToLowerInvariant();
        return n.Contains("vegetation")||n.Contains("ecosystem");
    }
    static bool IsInstanceContainer(Transform t)
    {
        if(!t)return false;
        string n=t.name.ToLowerInvariant();
        if(n=="trees"||n=="understory")return IsVegRootName(t.parent?t.parent.name:"");
        if(IsVegRootName(n))
        {
            return n.Contains("source anchored")||n.Contains("supplementary")||n.Contains("desert vegetation");
        }
        return false;
    }
    static List<GameObject> ExistingInstances(GameObject[] roots)
    {
        var list=new List<GameObject>();var seen=new HashSet<GameObject>();
        foreach(var t in roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        {
            if(!IsInstanceContainer(t))continue;
            foreach(Transform c in t)
            {
                var go=c.gameObject;
                if(go.GetComponentInChildren<Renderer>(true)&&seen.Add(go))list.Add(go);
            }
        }
        return list;
    }
    static float GroundInstance(GameObject go,Terrain t)
    {
        float x=go.transform.position.x,z=go.transform.position.z;
        var e=go.transform.eulerAngles;
        go.transform.rotation=Quaternion.Euler(0f,e.y,0f);
        var b=B(go);if(b.size==Vector3.zero)return 999f;
        float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(gy-b.min.y);
        go.transform.position=new Vector3(x,go.transform.position.y,z);
        b=B(go);
        return b.min.y-gy;
    }
    static float VerticalRatio(GameObject go)
    {
        var b=B(go);return b.size.y/Mathf.Max(.05f,Mathf.Max(b.size.x,b.size.z));
    }
    static bool TreeLike(GameObject go)
    {
        string n=go.name.ToLowerInvariant();
        string p=SourcePath(go).ToLowerInvariant();
        if(p.Contains("quiver_tree")||n.Contains("quiver"))return false; // naturally broad but upright; transform audit handles tilt
        return n.Contains("tree")||n.Contains("pine")||n.Contains("grove")||p.Contains("tree")||p.Contains("pine");
    }
    static int SX(float x)=>Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,N-1);
    static int SY(float z)=>Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);
    static bool WaterRoad(byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=SX(x),sy=SY(z);byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
        return (f&1)!=0||(f&8)!=0||(g>=8&&g<255);
    }
    static int ClassAt(string zone,byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=SX(x),sy=SY(z);byte raw=tile[sy*N+sx];
        return MMRealisticTerrainBiomePass.BaseClassAt(zone,grp[raw],sem[raw],sy);
    }
    static bool Near(Bounds[] bs,float x,float z,float m)
    {
        foreach(var b in bs)if(x>=b.min.x-m&&x<=b.max.x+m&&z>=b.min.z-m&&z<=b.max.z+m)return true;
        return false;
    }
    static bool Close(List<Vector2> pts,float x,float z,float min)
    {
        float d=min*min;var q=new Vector2(x,z);
        foreach(var p in pts)if((p-q).sqrMagnitude<d)return true;
        return false;
    }

    static void ScaleHeight(GameObject go,float h)
    {
        var b=B(go);if(b.size.y>.03f)go.transform.localScale*=h/b.size.y;
    }
    static void ClampCactusHeight(GameObject go,float target)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&CactusIndex(r.gameObject.name)>0).ToArray();
        if(rs.Length==0)return;
        float maxH=rs.Max(r=>r.bounds.size.y);
        if(maxH>Mathf.Max(5.0f,target*1.15f) && maxH>.01f)
            go.transform.localScale*=target/maxH;
    }
    static GameObject[] GoodTrees()=>new[]{
        LoadGO("Assets/EnvironmentAssets/UnitySamples/BanyanTree.fbx"),
        LoadGO("Assets/EnvironmentAssets/Gobkit/TreeHigh001.fbx"),
        LoadGO("Assets/EnvironmentAssets/Gobkit/TreeHigh002.fbx"),
        LoadGO("Assets/EnvironmentAssets/Gobkit/TreeHigh003.fbx"),
        LoadGO("Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbx"),
        LoadGO("Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx")
    }.Where(x=>x).ToArray();

    static GameObject ReplaceHorizontalTree(GameObject old,Terrain t,Mats m,GameObject[] pool,int seed)
    {
        var parent=old.transform.parent;string name=old.name;
        float x=old.transform.position.x,z=old.transform.position.z,yaw=old.transform.eulerAngles.y;
        float h=Mathf.Clamp(B(old).size.y,6f,13f);
        for(int k=0;k<pool.Length;k++)
        {
            var src=pool[Mathf.Abs(seed+k*7)%pool.Length];
            var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
            go.name=name;go.transform.SetParent(parent,true);go.transform.position=new Vector3(x,0,z);
            go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;
            SafeMaterials(go,m);ScaleHeight(go,h);float gap=GroundInstance(go,t);
            float vr=VerticalRatio(go);
            if(vr>=.55f&&Mathf.Abs(gap)<=.05f&&AllMaterialsValid(go))
            {
                UnityEngine.Object.DestroyImmediate(old);
                return go;
            }
            UnityEngine.Object.DestroyImmediate(go);
        }
        return old;
    }

    static int PrepareCactus(GameObject go,int variant,Mats m)
    {
        int kept=0;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            int ci=CactusIndex(r.gameObject.name);
            if(ci==0)continue;
            bool on=ci==variant;r.gameObject.SetActive(on);
            if(on){SetAll(r,m.cactus[ci]);kept++;}
        }
        return kept;
    }
    static void AssignDownloadedShrub(GameObject go,string key,Mats m)
    {
        if(!m.downloadedShrub.TryGetValue(key,out var mat))return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))if(r.enabled)SetAll(r,mat);
    }
    static void AssignQuiver(GameObject go,Mats m)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            string n=(r.gameObject.name+" "+string.Join(" ",(r.sharedMaterials??new Material[0]).Where(x=>x).Select(x=>x.name))).ToLowerInvariant();
            SetAll(r,(n.Contains("leaf")||n.Contains("branch"))?m.quiverLeaf:m.quiverTrunk);
        }
    }

    static List<Vector2Int> DesertCells(string zone,byte[] tile,byte[] grp,byte[] sem)
    {
        var a=new List<Vector2Int>();
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)
        {
            byte raw=tile[sy*N+sx],f=sem[raw],g=grp[raw];
            if((f&1)!=0||(f&8)!=0||(g>=8&&g<255))continue;
            if(MMRealisticTerrainBiomePass.BaseClassAt(zone,g,f,sy)==MMRealisticTerrainBiomePass.Desert)
                a.Add(new Vector2Int(sx,sy));
        }
        return a;
    }

    static GameObject SpawnDesert(GameObject prefab,string kind,int variant,Transform parent,Terrain t,Mats m,
                                  float x,float z,float yaw,float h,int seed,out float gap)
    {
        gap=999f;if(!prefab)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name=$"Desert_{kind}_{seed:00000}";
        go.transform.SetParent(parent,true);go.transform.position=new Vector3(x,0,z);
        go.transform.rotation=Quaternion.Euler(0f,yaw,0f);go.transform.localScale=Vector3.one;

        if(kind=="Cactus")
        {
            if(PrepareCactus(go,variant,m)==0){UnityEngine.Object.DestroyImmediate(go);return null;}
        }
        else if(kind.StartsWith("Shrub_"))AssignDownloadedShrub(go,kind.Substring(6),m);
        else if(kind=="Shrub02")foreach(var r in go.GetComponentsInChildren<Renderer>(true))if(r.enabled)SetAll(r,m.shrub02);
        else if(kind=="Quiver")AssignQuiver(go,m);

        ScaleHeight(go,h);if(kind=="Cactus")ClampCactusHeight(go,h);gap=GroundInstance(go,t);
        go.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        if(Mathf.Abs(gap)>.06f||!AllMaterialsValid(go))
        {
            UnityEngine.Object.DestroyImmediate(go);return null;
        }
        return go;
    }

    static int BuildDesert(Z z,GameObject region,Terrain t,byte[] tile,byte[] grp,byte[] sem,Bounds[] arch,
                           Mats m,List<Vector2> occupied,List<string> rows)
    {
        var cells=DesertCells(z.key,tile,grp,sem);
        var old=region.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="Desert Vegetation - Randomized Supplement");
        if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        if(cells.Count==0)return 0;

        var root=new GameObject("Desert Vegetation - Randomized Supplement");root.transform.SetParent(region.transform,false);
        var cactus=LoadGO("Assets/Environment/DesertVegetation/Cactus.fbx");
        var shrub02=LoadGO("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
        var quiver=LoadGO("Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");
        var dry=new Dictionary<string,GameObject>();
        foreach(string n in new[]{"shrub_01","shrub_03","shrub_04","wild_rooibos_bush"})
            dry[n]=LoadGO($"Assets/Environment/PolyHaven/Downloaded/{n}/{n}_1k.fbx");

        int target=Mathf.Clamp(Mathf.RoundToInt(cells.Count/6.0f),40,1700);
        var rng=new System.Random(unchecked(z.key.GetHashCode()*397+915731));
        int made=0,attempt=0,maxAttempt=target*45;
        while(made<target&&attempt++<maxAttempt)
        {
            var cell=cells[rng.Next(cells.Count)];
            float x=(cell.x-64f)*4f+(float)(rng.NextDouble()*3.6-1.8);
            float zz=(64f-cell.y)*4f+(float)(rng.NextDouble()*3.6-1.8);
            if(WaterRoad(tile,grp,sem,x,zz))continue;
            if(ClassAt(z.key,tile,grp,sem,x,zz)!=MMRealisticTerrainBiomePass.Desert)continue;
            if(Near(arch,x,zz,3.25f))continue;
            if(Close(occupied,x,zz,2.75f))continue;
            float nx=(x-t.transform.position.x)/t.terrainData.size.x,nz=(zz-t.transform.position.z)/t.terrainData.size.z;
            if(nx<.01f||nz<.01f||nx>.99f||nz>.99f)continue;
            float slope=t.terrainData.GetSteepness(nx,nz);if(slope>30f)continue;

            double q=rng.NextDouble();GameObject pf=null;string kind="";int variant=0;float h=1f;
            if(q<.43)
            {
                pf=cactus;kind="Cactus";variant=1+rng.Next(9);h=Mathf.Lerp(1.4f,4.8f,(float)rng.NextDouble());
            }
            else if(q<.92)
            {
                int pick=rng.Next(5);
                if(pick==0){pf=shrub02;kind="Shrub02";}
                else
                {
                    string[] names={"shrub_01","shrub_03","shrub_04","wild_rooibos_bush"};
                    string n=names[pick-1];pf=dry[n];kind="Shrub_"+n;
                }
                h=Mathf.Lerp(.45f,1.65f,(float)rng.NextDouble());
            }
            else
            {
                pf=quiver;kind="Quiver";h=Mathf.Lerp(2.4f,5.8f,(float)rng.NextDouble());
            }
            if(!pf)continue;
            float yaw=(float)rng.NextDouble()*360f;float gap;
            var go=SpawnDesert(pf,kind,variant,root.transform,t,m,x,zz,yaw,h,made+attempt,out gap);
            if(!go)continue;
            float dx=go.transform.position.x-x,dz=go.transform.position.z-zz;
            if(Mathf.Abs(dx)>.002f||Mathf.Abs(dz)>.002f){UnityEngine.Object.DestroyImmediate(go);continue;}
            occupied.Add(new Vector2(x,zz));made++;
            rows.Add($"{z.key},DESERT_ADD,{go.name},{kind},{x:F3},{zz:F3},{yaw:F2},{h:F2},{gap:F4},{slope:F2},PASS");
        }
        Debug.Log($"DESERT_RANDOM {z.key} cells={cells.Count} target={target} made={made} attempts={attempt}");
        return made;
    }

    static void RepairExisting(Z z,Terrain t,Mats m,GameObject[] roots,GameObject[] goodTrees,
                               List<Vector2> occupied,List<string> rows,
                               out int before,out int after,out int matFix,out int groundFix,out int uprightFix,out int replaced)
    {
        var items=ExistingInstances(roots);
        before=items.Count;matFix=groundFix=uprightFix=replaced=0;
        int seed=0;
        foreach(var initial in items)
        {
            if(!initial)continue;seed++;
            var go=initial;
            float x0=go.transform.position.x,z0=go.transform.position.z;
            var bb0=B(go);float gy0=t.SampleHeight(new Vector3(x0,0,z0))+t.transform.position.y;
            float gap0=bb0.size==Vector3.zero?999f:bb0.min.y-gy0;
            bool badBefore=!AllMaterialsValid(go);
            float vr0=VerticalRatio(go);
            bool wasTilt=Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.x,0f))>.05f||
                         Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.z,0f))>.05f;

            SafeMaterials(go,m);
            if(badBefore)matFix++;

            float gap=GroundInstance(go,t);
            if(Mathf.Abs(gap0)>.06f)groundFix++;
            if(wasTilt)uprightFix++;

            if(TreeLike(go)&&VerticalRatio(go)<.48f)
            {
                var repl=ReplaceHorizontalTree(go,t,m,goodTrees,unchecked(z.key.GetHashCode()+seed*7919));
                if(repl!=go){go=repl;replaced++;uprightFix++;}
            }

            SafeMaterials(go,m);
            gap=GroundInstance(go,t);
            float dx=go.transform.position.x-x0,dz=go.transform.position.z-z0;
            float vr=VerticalRatio(go);
            bool mats=AllMaterialsValid(go);
            bool rot=Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.x,0f))<=.05f&&
                     Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.z,0f))<=.05f;
            string status=(Mathf.Abs(dx)<=.002f&&Mathf.Abs(dz)<=.002f&&Mathf.Abs(gap)<=.06f&&mats&&rot&&(!TreeLike(go)||vr>=.48f))?"PASS":"FAIL";
            rows.Add($"{z.key},EXISTING,{go.name},{SourcePath(go)},{x0:F3},{z0:F3},{go.transform.eulerAngles.y:F2},{B(go).size.y:F2},{gap:F4},{0:F2},{status};vr={vr:F3};dx={dx:F4};dz={dz:F4};mat={(mats?1:0)}");
            if(status!="PASS")throw new Exception($"Vegetation validation failed {z.key} {go.name} status={status} vr={vr} gap={gap} dx={dx} dz={dz} mats={mats}");
            occupied.Add(new Vector2(x0,z0));
        }
        after=ExistingInstances(roots).Count;
        if(after!=before)throw new Exception($"{z.key} existing vegetation coverage changed before={before} after={after}");
    }

    static int ApplyZone(Z z,Mats m,GameObject[] goodTrees,List<string> rows,List<string> summary)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var roots=sc.GetRootGameObjects();
        var t=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        var region=roots.FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true)==t)??roots.First();

        string dd=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(dd+"/tilemap_u8.bin");
        var grp=File.ReadAllBytes(dd+"/tile_groups_u8.bin");
        var sem=File.ReadAllBytes(dd+"/tile_semantics_u8.bin");

        var archT=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(x=>x.name.IndexOf("Architecture",StringComparison.OrdinalIgnoreCase)>=0);
        var arch=archT?archT.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Select(r=>r.bounds).ToArray():new Bounds[0];

        var occupied=new List<Vector2>();
        RepairExisting(z,t,m,roots,goodTrees,occupied,rows,
                       out int before,out int after,out int matFix,out int groundFix,out int uprightFix,out int replaced);

        int added=BuildDesert(z,region,t,tile,grp,sem,arch,m,occupied,rows);

        // Revalidate every vegetation instance after desert addition.
        int invalid=0;int total=0;
        foreach(var go in ExistingInstances(roots))
        {
            if(!go)continue;total++;
            float x=go.transform.position.x,zv=go.transform.position.z;
            SafeMaterials(go,m);float gap=GroundInstance(go,t);
            bool ok=AllMaterialsValid(go)&&Mathf.Abs(gap)<=.06f&&
                    Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.x,0f))<=.05f&&
                    Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.z,0f))<=.05f&&
                    (!TreeLike(go)||VerticalRatio(go)>=.48f);
            if(!ok)invalid++;
            // X/Z intentionally read-only during this final validation.
            go.transform.position=new Vector3(x,go.transform.position.y,zv);
        }
        if(invalid>0)throw new Exception($"{z.key} vegetation invalid after pass={invalid}");

        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        summary.Add($"{z.key},{before},{after},{added},{total},{matFix},{groundFix},{uprightFix},{replaced},{invalid}");
        Debug.Log($"VEG_REPAIR {z.key} kept={before} addedDesert={added} materialFix={matFix} groundFix={groundFix} uprightFix={uprightFix} replacedHorizontal={replaced} invalid={invalid}");
        return added;
    }

    [MenuItem("MMUnity/Locked/Vegetation Material Ground Desert Repair")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var m=Materials();var goodTrees=GoodTrees();
        if(goodTrees.Length<5)throw new Exception("Good tree pool incomplete "+goodTrees.Length);

        Directory.CreateDirectory("Validation");
        var rows=new List<string>{"zone,kind,name,asset_or_type,x,z,yaw,height,groundGap,slope,status"};
        var summary=new List<string>{"zone,existing_before,existing_after,desert_added,total_after,material_fixed,ground_fixed,upright_fixed,horizontal_replaced,invalid"};
        int totalAdded=0;
        foreach(var z in Zones)totalAdded+=ApplyZone(z,m,goodTrees,rows,summary);

        File.WriteAllLines("Validation/VegetationMaterialGroundDesertAudit.csv",rows);
        File.WriteAllLines("Validation/VegetationMaterialGroundDesertSummary.csv",summary);
        AssetDatabase.SaveAssets();
        Debug.Log($"VEGETATION_MATERIAL_GROUND_DESERT_DONE desertAdded={totalAdded}");
    }
}
