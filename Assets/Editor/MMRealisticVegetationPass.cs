using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMRealisticVegetationPass
{
    const int N=128;
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity")};

    static GameObject Load(string p)
    {
        var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if(g)return g;
        return AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }
    static GameObject[] LoadMany(params string[] p)=>p.Select(Load).Where(x=>x).Distinct().ToArray();

    static GameObject[] GreenPool()=>LoadMany(
        "Assets/Environment/FinalWorldVegetation/island_tree_01.prefab",
        "Assets/Environment/FinalWorldVegetation/island_tree_02.prefab",
        "Assets/Environment/FinalWorldVegetation/fir_sapling.prefab");

    static GameObject[] DeadPool()=>LoadMany(
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab",
        "Assets/Environment/PolyHaven/Models/dead_quiver_trunk/dead_quiver_trunk_1k.fbx",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab");

    static GameObject[] DesertPool()=>LoadMany(
        "Assets/Environment/DesertVegetation/Cactus.fbx",
        "Assets/Environment/PolyHaven/Downloaded/quiver_tree_01/quiver_tree_01_1k.fbx");

    static GameObject[] SnowPool()=>LoadMany(
        "Assets/Environment/FinalWorldVegetation/fir_sapling.prefab",
        "Assets/Environment/WinterVegetation/WinterTree5/winter-tree5_LOW_RES.fbx",
        "Assets/Environment/WinterVegetation/WinterTree8/winter-tree8_LOW_RES.fbx");

    static GameObject[] AridPool()=>LoadMany(
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab",
        "Assets/Environment/PolyHaven/Models/dead_quiver_trunk/dead_quiver_trunk_1k.fbx");

    static bool VegName(string n)
    {
        n=n.ToLowerInvariant();
        return n.StartsWith("6tree")||n.StartsWith("tree")||n.StartsWith("swptree")||n.StartsWith("snotre");
    }
    static bool P(string s,out float v)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    static int Hash(string a,int i){unchecked{return (a.GetHashCode()*397)^i;}}
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }

    static Material Mat(string key,string diff,string normal,string alpha=null,Color? tint=null)
    {
        string dir="Assets/Materials/RealisticWorld";Directory.CreateDirectory(dir);
        string path=$"{dir}/{key}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        var d=AssetDatabase.LoadAssetAtPath<Texture2D>(diff);var n=string.IsNullOrEmpty(normal)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
        if(d)m.SetTexture("_MainTex",d);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        if(tint.HasValue)m.color=tint.Value;else m.color=Color.white;
        if(!string.IsNullOrEmpty(alpha))
        {
            m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.32f);m.SetOverrideTag("RenderType","TransparentCutout");
            m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);
        }
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.05f);EditorUtility.SetDirty(m);return m;
    }

    static void ApplyDownloadedMaterials(GameObject go,string assetPath)
    {
        string p=assetPath.ToLowerInvariant();
        Material wood=null,leaf=null;
        if(p.Contains("tree_small_02"))
        {
            string b="Assets/Environment/PolyHaven/Downloaded/tree_small_02/textures/";
            wood=Mat("TreeSmall02_Wood",b+"tree_small_02_diff_1k.jpg",b+"tree_small_02_nor_gl_1k.exr");
            leaf=Mat("TreeSmall02_Leaves",b+"tree_small_02_leaves_diff_1k.png",b+"tree_small_02_leaves_nor_gl_1k.png",b+"tree_small_02_leaves_alpha_1k.png");
        }
        else if(p.Contains("island_tree_02"))
        {
            string b="Assets/Environment/PolyHaven/Downloaded/island_tree_02/textures/";
            wood=Mat("IslandTree02_Wood",b+"island_tree_02_diff_1k.jpg",b+"island_tree_02_nor_gl_1k.exr");
            leaf=Mat("IslandTree02_Leaves",b+"island_tree_02_leaves_diff_1k.png",b+"island_tree_02_leaves_nor_gl_1k.png",b+"island_tree_02_leaves_alpha_1k.png");
        }
        else if(p.Contains("quiver_tree_01"))
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
        if(!wood)return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;if(a==null||a.Length==0)a=new Material[1];
            for(int i=0;i<a.Length;i++)
            {
                string n=((a[i]?a[i].name:"")+" "+r.gameObject.name).ToLowerInvariant();
                a[i]=(n.Contains("leaf")||n.Contains("twig")||n.Contains("branch"))?(leaf?leaf:wood):wood;
            }
            r.sharedMaterials=a;
        }
    }

    static Material SnowMaterial(Material src)
    {
        if(!src)return null;
        string safe=new string(src.name.Select(ch=>char.IsLetterOrDigit(ch)?ch:'_').ToArray());
        string dir="Assets/Materials/RealisticWorld/SnowTrees";Directory.CreateDirectory(dir);
        string path=$"{dir}/{safe}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(src);AssetDatabase.CreateAsset(m,path);}
        if(m.HasProperty("_Color"))
        {
            Color c=src.HasProperty("_Color")?src.color:Color.white;
            m.color=new Color(Mathf.Lerp(c.r,.88f,.64f),Mathf.Lerp(c.g,.92f,.64f),Mathf.Lerp(c.b,.88f,.64f),c.a);
        }
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(m);return m;
    }
    static void Snowify(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;if(a==null)continue;
            for(int i=0;i<a.Length;i++)if(a[i])a[i]=SnowMaterial(a[i]);
            r.sharedMaterials=a;
        }
    }
    static bool TexturesOK(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return false;
        foreach(var r in rs)
        {
            var ms=r.sharedMaterials;if(ms==null||ms.Length==0)return false;
            foreach(var m in ms)
            {
                if(!m)return false;
                bool hasTex=false;
                foreach(var pn in m.GetTexturePropertyNames())if(m.GetTexture(pn)){hasTex=true;break;}
                if(!hasTex)return false;
            }
        }
        return true;
    }
    static void ScaleHeight(GameObject go,float height)
    {
        var b=BoundsOf(go);if(b.size.y>.05f)go.transform.localScale*=height/b.size.y;
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
    static void Ground(GameObject go,Terrain t)
    {
        var b=BoundsOf(go);if(b.size==Vector3.zero)return;
        float y=t.SampleHeight(new Vector3(b.center.x,0,b.center.z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(y-b.min.y);
    }
    static float VerticalRatio(GameObject go)
    {
        var b=BoundsOf(go);return b.size.y/Mathf.Max(.05f,Mathf.Max(b.size.x,b.size.z));
    }

    static int CellX(float x){return Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,N-1);}
    static int CellY(float z){return Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);}
    static int TerrainClass(string zone,byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=CellX(x),sy=CellY(z);byte raw=tile[sy*N+sx];
        return MMRealisticTerrainBiomePass.BaseClassAt(zone,grp[raw],sem[raw],sy);
    }

    static bool RoadOrWater(byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=CellX(x),sy=CellY(z);byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
        return (f&1)!=0||(f&8)!=0||(g>=8&&g<255);
    }
    static bool NearArchitecture(Bounds[] bounds,float x,float z,float margin)
    {
        foreach(var b in bounds)
            if(x>=b.min.x-margin&&x<=b.max.x+margin&&z>=b.min.z-margin&&z<=b.max.z+margin)return true;
        return false;
    }

    static bool TooClose(List<Vector2> pts,float x,float z,float min)
    {
        float m2=min*min;
        foreach(var p in pts)if((p-new Vector2(x,z)).sqrMagnitude<m2)return true;
        return false;
    }
    static GameObject Pick(GameObject[] a,int seed,int attempt)
    {
        if(a==null||a.Length==0)return null;
        int i=Mathf.Abs(seed+attempt*104729)%a.Length;
        return a[i];
    }
    static GameObject[] SourcePool(string zone,int cls,int seed,GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid)
    {
        if(zone=="SweetWater"||zone=="Kriegspire")return deadPool; // no live green trees in either dry/snow battle-scarred region
        if(zone=="ParadiseValley")return cls==MMRealisticTerrainBiomePass.Desert?desert:deadPool;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return deadPool;
        if(cls==MMRealisticTerrainBiomePass.Desert)return desert;
        if(cls==MMRealisticTerrainBiomePass.Snow)return snow;
        if(cls==MMRealisticTerrainBiomePass.Arid)return arid;
        return green;
    }

    static int CactusIndex(string s)
    {
        if(string.IsNullOrEmpty(s))return 0;s=s.ToLowerInvariant();
        for(int i=1;i<=9;i++)if(s.Contains("cactus_"+i))return i;
        return 0;
    }
    static Material CactusMaterial(int i)
    {
        string b="Assets/Environment/DesertVegetation/Textures/";
        return Mat("Cactus_"+i,b+$"cactus_{i}__pbrs2a_diffuse.png",b+$"cactus_{i}__pbrs2a_normal.png");
    }
    static void PrepareCactus(GameObject go,int seed)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>CactusIndex(r.gameObject.name)>0).OrderBy(r=>CactusIndex(r.gameObject.name)).ToArray();
        if(rs.Length==0)return;int keep=Mathf.Abs(seed)%rs.Length;
        for(int i=0;i<rs.Length;i++)
        {
            rs[i].gameObject.SetActive(i==keep);
            if(i==keep)
            {
                int ci=CactusIndex(rs[i].gameObject.name);var a=rs[i].sharedMaterials;
                if(a==null||a.Length==0)a=new Material[1];
                for(int k=0;k<a.Length;k++)a[k]=CactusMaterial(Mathf.Max(1,ci));
                rs[i].sharedMaterials=a;
            }
        }
    }

    static void PrepareShrub(GameObject go)
    {
        string b="Assets/Environment/PolyHaven/Models/shrub_02/";
        var m=Mat("Shrub02",b+"shrub_02_rgba_1k.png",b+"shrub_02_nor_gl_1k.exr",b+"shrub_02_alpha_1k.png");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;if(a==null||a.Length==0)a=new Material[1];
            for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;
        }
    }

    static GameObject SpawnChecked(GameObject prefab,Transform parent,Terrain terrain,float x,float z,float yaw,float height,int seed,bool snow,string label,out float ratio)
    {
        ratio=0f;if(!prefab)return null;
        var wrap=new GameObject(label);wrap.transform.SetParent(parent,true);
        wrap.transform.position=new Vector3(x,0f,z);
        wrap.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        wrap.transform.localScale=Vector3.one;
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        visual.transform.SetParent(wrap.transform,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        visual.transform.localScale=Vector3.one;
        string ap=AssetDatabase.GetAssetPath(prefab);
        if(ap.IndexOf("/Downloaded/",StringComparison.OrdinalIgnoreCase)>=0)ApplyDownloadedMaterials(visual,ap);
        if(ap.IndexOf("Cactus.fbx",StringComparison.OrdinalIgnoreCase)>=0)PrepareCactus(visual,seed);
        if(ap.IndexOf("/shrub_02/",StringComparison.OrdinalIgnoreCase)>=0)PrepareShrub(visual);
        if(snow)Snowify(visual);
        ScaleHeight(wrap,height);
        if(ap.IndexOf("Cactus.fbx",StringComparison.OrdinalIgnoreCase)>=0)ClampCactusHeight(wrap,height);
        Ground(wrap,terrain);
        wrap.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        ratio=VerticalRatio(wrap);
        string low=ap.ToLowerInvariant();
        bool shrubLike=low.Contains("shrub")||low.Contains("cactus")||low.Contains("quiver_tree");
        float minRatio=shrubLike?.22f:.70f;
        if(ratio<minRatio||!TexturesOK(wrap))
        {
            UnityEngine.Object.DestroyImmediate(wrap);
            return null;
        }
        return wrap;
    }
    static float HeightFor(int cls,int seed)
    {
        float u=(Mathf.Abs(seed)%1000)/999f;
        if(cls==MMRealisticTerrainBiomePass.Sand)return Mathf.Lerp(2.0f,5.8f,u);
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return Mathf.Lerp(4.8f,10.0f,u);
        if(cls==MMRealisticTerrainBiomePass.DryBrown)return Mathf.Lerp(5.0f,10.5f,u);
        if(cls==MMRealisticTerrainBiomePass.SlightGreen)return Mathf.Lerp(5.5f,11.5f,u);
        if(cls==MMRealisticTerrainBiomePass.Snow)return Mathf.Lerp(6.0f,13.0f,u);
        return Mathf.Lerp(6.0f,13.5f,u);
    }

    static int RebuildSourceAnchors(Z z,Transform regionRoot,Terrain terrain,byte[] tile,byte[] grp,byte[] sem,
                                    GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid,List<string> audit,List<Vector2> occupied)
    {
        var old=regionRoot.Find("Vegetation - Source Anchored Final");
        if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Vegetation - Source Anchored Final");root.transform.SetParent(regionRoot,false);
        string file=$"Assets/World/{z.key}/Data/decorations.csv";int made=0;
        foreach(var line in File.ReadAllLines(file).Skip(1))
        {
            var q=line.Split(',');if(q.Length<10)continue;
            string nm=q[1].Trim().ToLowerInvariant();if(!VegName(nm))continue;
            if(!P(q[4],out float x)||!P(q[6],out float oz))continue;float zz=-oz;
            int idx=made;if(q.Length>0)int.TryParse(q[0],out idx);
            int seed=Hash(nm,idx+1),cls=TerrainClass(z.key,tile,grp,sem,x,zz);
            if(z.key=="HermitsIsle")continue; // volcanic ash island: no tree anchors
            if(RoadOrWater(tile,grp,sem,x,zz))continue; // never preserve a source tree on a road or water cell
            var pool=SourcePool(z.key,cls,seed,green,deadPool,desert,snow,arid);
            GameObject go=null;GameObject chosen=null;float ratio=0f;
            for(int a=0;a<Mathf.Max(1,pool.Length);a++)
            {
                chosen=Pick(pool,seed,a);if(!chosen)continue;
                float yaw=((seed&0x7fffffff)%10000)*.036f;
                go=SpawnChecked(chosen,root.transform,terrain,x,zz,yaw,HeightFor(cls,seed+a*13),seed+a,cls==MMRealisticTerrainBiomePass.Snow,
                                $"SourceTree_{idx:0000}_{nm}",out ratio);
                if(go)break;
            }
            if(!go)
            {
                audit.Add($"{z.key},SOURCE,{idx},{nm},{x:F3},{zz:F3},{x:F3},{zz:F3},0,0,{MMRealisticTerrainBiomePass.ClassName(cls)},NONE,0,0,0,0,1,0,FAILED_NO_UPRIGHT_TEXTURED_PREFAB");
                continue;
            }
            var b=BoundsOf(go);
            bool rw=RoadOrWater(tile,grp,sem,b.center.x,b.center.z);
            if(rw){UnityEngine.Object.DestroyImmediate(go);continue;} // visual footprint center must also stay off roads/water
            float gy=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;float gap=b.min.y-gy;
            float nx=(b.center.x-terrain.transform.position.x)/terrain.terrainData.size.x,nz=(b.center.z-terrain.transform.position.z)/terrain.terrainData.size.z;
            float slope=(nx>=0&&nz>=0&&nx<=1&&nz<=1)?terrain.terrainData.GetSteepness(nx,nz):0f;
            audit.Add($"{z.key},SOURCE,{idx},{nm},{go.transform.position.x:F3},{go.transform.position.z:F3},{x:F3},{zz:F3},{go.transform.position.x-x:F4},{go.transform.position.z-zz:F4},{MMRealisticTerrainBiomePass.ClassName(cls)},{AssetDatabase.GetAssetPath(chosen)},{go.transform.eulerAngles.x:F2},{go.transform.eulerAngles.z:F2},{ratio:F3},{gap:F3},0,{slope:F2},PASS");
            occupied.Add(new Vector2(x,zz));made++;
        }
        return made;
    }

    static bool AcceptDensity(int cls,float cluster,float rnd)
    {
        if(cls==MMRealisticTerrainBiomePass.Green)return cluster>.38f&&rnd<.32f;
        if(cls==MMRealisticTerrainBiomePass.LightGreen)return cluster>.48f&&rnd<.14f;
        if(cls==MMRealisticTerrainBiomePass.Snow)return cluster>.43f&&rnd<.20f;
        if(cls==MMRealisticTerrainBiomePass.Arid)return cluster>.56f&&rnd<.065f;
        if(cls==MMRealisticTerrainBiomePass.Desert)return cluster>.50f&&rnd<.09f;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return cluster>.60f&&rnd<.055f;
        return false;
    }
    static GameObject[] SupplementPool(string zone,int cls,int seed,GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid)
    {
        if(zone=="SweetWater"||zone=="Kriegspire")return deadPool;
        if(zone=="ParadiseValley")return cls==MMRealisticTerrainBiomePass.Desert?desert:deadPool;
        if(cls==MMRealisticTerrainBiomePass.Desert)return desert;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return deadPool;
        if(cls==MMRealisticTerrainBiomePass.Snow)return snow;
        if(cls==MMRealisticTerrainBiomePass.Arid)return arid;
        return green;
    }

    static int BuildSupplementary(Z z,Transform regionRoot,Terrain terrain,byte[] tile,byte[] grp,byte[] sem,
                                  GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid,List<string> audit,List<Vector2> occupied)
    {
        var old=regionRoot.Find("Realistic Ecosystem Supplementary");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Realistic Ecosystem Supplementary");root.transform.SetParent(regionRoot,false);
        var arch=regionRoot.Find("Architecture - MM6 Original Layout Expanded");
        var ab=arch?arch.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Select(r=>r.bounds).ToArray():new Bounds[0];
        int made=0;
        for(int sy=2;sy<N-2;sy+=2)for(int sx=2;sx<N-2;sx+=2)
        {
            int seed=(sx*73856093)^(sy*19349663)^z.key.GetHashCode();
            float rnd=((seed&0x7fffffff)%10000)/10000f;
            float cluster=Mathf.PerlinNoise((sx+19.3f)*.075f,(sy+41.7f)*.075f);
            float x=(sx-64f)*4f+(((seed>>4)&255)/255f-.5f)*6.0f;
            float zz=(64f-sy)*4f+(((seed>>12)&255)/255f-.5f)*6.0f;
            int cls=TerrainClass(z.key,tile,grp,sem,x,zz);
            if(z.key=="HermitsIsle")continue; // preserve barren volcanic-ash character
            if(!AcceptDensity(cls,cluster,rnd))continue;
            if(RoadOrWater(tile,grp,sem,x,zz))continue;
            if(NearArchitecture(ab,x,zz,5.5f))continue;
            if(TooClose(occupied,x,zz,5.0f))continue;
            float nx=(x-terrain.transform.position.x)/terrain.terrainData.size.x;
            float nz=(zz-terrain.transform.position.z)/terrain.terrainData.size.z;
            if(nx<.015f||nz<.015f||nx>.985f||nz>.985f)continue;
            float slope=terrain.terrainData.GetSteepness(nx,nz);
            if(slope>36f)continue;
            var pool=SupplementPool(z.key,cls,seed,green,deadPool,desert,snow,arid);if(pool.Length==0)continue;
            GameObject go=null,chosen=null;float ratio=0f;
            for(int a=0;a<Mathf.Min(pool.Length,10);a++)
            {
                chosen=Pick(pool,seed,a);if(!chosen)continue;
                float yaw=((seed+a*977)&0x7fffffff)%36000/100f;
                go=SpawnChecked(chosen,root.transform,terrain,x,zz,yaw,HeightFor(cls,seed+a*31),seed+a,
                                cls==MMRealisticTerrainBiomePass.Snow,$"Eco_{MMRealisticTerrainBiomePass.ClassName(cls)}_{sx}_{sy}",out ratio);
                if(go)break;
            }
            if(!go)continue;
            float dx=go.transform.position.x-x,dz=go.transform.position.z-zz;
            bool coordOk=Mathf.Abs(dx)<.01f&&Mathf.Abs(dz)<.01f;
            bool rotOk=Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.x,0f))<.1f&&Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.z,0f))<.1f;
            var b=BoundsOf(go);float gy=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;float gap=b.min.y-gy;
            bool visualSafe=!RoadOrWater(tile,grp,sem,b.center.x,b.center.z);
            string status=coordOk&&rotOk&&visualSafe&&ratio>=.35f&&Mathf.Abs(gap)<.08f?"PASS":"FAILED";
            audit.Add($"{z.key},SUPPLEMENTARY,{made},- ,{go.transform.position.x:F3},{go.transform.position.z:F3},{x:F3},{zz:F3},{dx:F4},{dz:F4},{MMRealisticTerrainBiomePass.ClassName(cls)},{AssetDatabase.GetAssetPath(chosen)},{go.transform.eulerAngles.x:F2},{go.transform.eulerAngles.z:F2},{ratio:F3},{gap:F3},0,{slope:F2},{status}");
            if(status!="PASS"){UnityEngine.Object.DestroyImmediate(go);continue;}
            occupied.Add(new Vector2(x,zz));made++;
        }
        return made;
    }

    static int ApplyZone(Z z,GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid,List<string> audit)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var roots=sc.GetRootGameObjects();
        var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception(z.key+" terrain missing");
        var regionRoot=roots.FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true)==terrain)??roots.First();
        foreach(string n in new[]{"Vegetation - MM Anchors + Natural Groves","Biome Vegetation - Source Grid","Witcher Ecosystem - Reference Map"})
        {
            var o=regionRoot.transform.Find(n);if(o)UnityEngine.Object.DestroyImmediate(o.gameObject);
        }
        string dd=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(dd+"/tilemap_u8.bin");var grp=File.ReadAllBytes(dd+"/tile_groups_u8.bin");var sem=File.ReadAllBytes(dd+"/tile_semantics_u8.bin");
        var occupied=new List<Vector2>();
        int source=RebuildSourceAnchors(z,regionRoot.transform,terrain,tile,grp,sem,green,deadPool,desert,snow,arid,audit,occupied);
        int extra=BuildSupplementary(z,regionRoot.transform,terrain,tile,grp,sem,green,deadPool,desert,snow,arid,audit,occupied);
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"REALISTIC_VEGETATION {z.key} source={source} supplementary={extra} variants={green.Length+deadPool.Length+desert.Length}");
        return source+extra;
    }

    [MenuItem("MMUnity/Realistic World/3b Priority Dry Vegetation")]
    public static void ApplyPriorityDry()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var green=GreenPool();var deadPool=DeadPool();var desert=DesertPool();var snow=SnowPool();var arid=AridPool();
        Directory.CreateDirectory("Validation");
        var audit=new List<string>{"zone,kind,index,name,x,z,targetX,targetZ,dx,dz,terrainClass,prefab,rootRotX,rootRotZ,verticalRatio,groundGap,roadOrWater,slope,status"};
        int total=0;
        foreach(var key in new[]{"HermitsIsle","ParadiseValley","SweetWater","Kriegspire"})
            total+=ApplyZone(Zones.First(x=>x.key==key),green,deadPool,desert,snow,arid,audit);
        File.WriteAllLines("Validation/PriorityDryTreePlacementAudit.csv",audit);
        AssetDatabase.SaveAssets();
        Debug.Log("REALISTIC_VEGETATION_PRIORITY_DRY_DONE total="+total);
    }

    [MenuItem("MMUnity/Realistic World/3 Vegetation")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var green=GreenPool();var deadPool=DeadPool();var desert=DesertPool();var snow=SnowPool();var arid=AridPool();
        Directory.CreateDirectory("Validation");
        var audit=new List<string>{"zone,kind,index,name,x,z,targetX,targetZ,dx,dz,terrainClass,prefab,rootRotX,rootRotZ,verticalRatio,groundGap,roadOrWater,slope,status"};
        int total=0;
        foreach(var z in Zones)total+=ApplyZone(z,green,deadPool,desert,snow,arid,audit);
        File.WriteAllLines("Validation/RealisticTreePlacementAudit.csv",audit);
        AssetDatabase.SaveAssets();
        Debug.Log($"REALISTIC_VEGETATION_ALL_DONE total={total} greenVariants={green.Length} snowVariants={snow.Length} deadVariants={deadPool.Length} desertVariants={desert.Length} aridVariants={arid.Length}");
    }
}
