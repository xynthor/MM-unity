using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMCactusScaleTextureRepairAndDragonRealism
{
    static readonly (string key,string scene)[] CactusZones={
        ("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        ("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        ("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        ("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity")
    };
    static readonly HashSet<string> Containers=new HashSet<string>(StringComparer.OrdinalIgnoreCase){
        "Desert Vegetation - Randomized Supplement",
        "Vegetation - Source Anchored Final",
        "Realistic Ecosystem Supplementary",
        "Source Decorations 3D - OneToOne"
    };
    static int CactusIndex(string s)
    {
        s=(s??"").ToLowerInvariant();
        for(int i=1;i<=9;i++)if(s.Contains("cactus_"+i))return i;
        return 0;
    }
    static bool IsCactusMesh(MeshFilter mf)
    {
        if(!mf||!mf.sharedMesh)return false;
        string p=AssetDatabase.GetAssetPath(mf.sharedMesh).Replace('\\','/').ToLowerInvariant();
        return p.Contains("/desertvegetation/cactus.fbx");
    }
    static Material EnsureCactusMaterial(int i)
    {
        string dir="Assets/Materials/RealisticWorld";Directory.CreateDirectory(dir);
        string path=$"{dir}/Cactus_{i}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        string b="Assets/Environment/DesertVegetation/Textures/";
        var d=AssetDatabase.LoadAssetAtPath<Texture2D>(b+$"cactus_{i}__pbrs2a_diffuse.png");
        var n=AssetDatabase.LoadAssetAtPath<Texture2D>(b+$"cactus_{i}__pbrs2a_normal.png");
        if(d)m.SetTexture("_MainTex",d);
        if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        if(m.HasProperty("_Color"))m.color=Color.white;
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.05f);
        EditorUtility.SetDirty(m);return m;
    }
    static Transform WrapperFor(Transform t)
    {
        for(var p=t;p&&p.parent;p=p.parent)
            if(Containers.Contains(p.parent.name))return p;
        return null;
    }
    static Renderer[] ActiveCactusRenderers(Transform wrapper)
    {
        return wrapper.GetComponentsInChildren<MeshFilter>(true)
            .Where(IsCactusMesh)
            .Where(m=>m.gameObject.activeInHierarchy)
            .Select(m=>m.GetComponent<Renderer>())
            .Where(r=>r&&r.enabled).ToArray();
    }
    static Bounds BoundsOf(Renderer[] rs)
    {
        if(rs.Length==0)return new Bounds();
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static int StableHash(string s)
    {
        unchecked{int h=17;foreach(char c in s??"")h=h*31+c;return h&0x7fffffff;}
    }
    static void RepairZone(string key,string scene,List<string> rows)
    {
        var sc=EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(!root)throw new Exception(key+" root missing");
        var terrain=root.GetComponentInChildren<Terrain>(true);
        int texFix=0,scaleFix=0,meshes=0;
        var wrappers=new HashSet<Transform>();

        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true).Where(IsCactusMesh))
        {
            meshes++;
            var r=mf.GetComponent<Renderer>();int ci=CactusIndex(mf.gameObject.name);
            if(r&&ci>0)
            {
                var good=EnsureCactusMaterial(ci);
                bool wrong=r.sharedMaterials==null||r.sharedMaterials.Length==0||
                           r.sharedMaterials.Any(m=>!m||m.name.StartsWith("cacturs_",StringComparison.OrdinalIgnoreCase)||
                           m.GetTexture("_MainTex")==null);
                int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
                var a=new Material[n];for(int k=0;k<n;k++)a[k]=good;r.sharedMaterials=a;
                if(wrong)texFix++;
            }
            var w=WrapperFor(mf.transform);if(w)wrappers.Add(w);
        }

        foreach(var w in wrappers)
        {
            var rs=ActiveCactusRenderers(w);if(rs.Length==0)continue;
            var b=BoundsOf(rs);float maxDim=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
            if(maxDim<=6.0f)continue;
            Vector3 anchor=w.position;
            float target=3.0f+(StableHash(key+"|"+w.name)%1400)/1000f;
            float f=target/maxDim;
            w.localScale*=f;
            w.position=new Vector3(anchor.x,w.position.y,anchor.z);
            rs=ActiveCactusRenderers(w);b=BoundsOf(rs);
            float gy=terrain.SampleHeight(new Vector3(anchor.x,0,anchor.z))+terrain.transform.position.y;
            w.position+=Vector3.up*(gy-b.min.y);
            scaleFix++;
            rows.Add($"{key},{w.parent.name},{w.name.Replace(',',';')},{maxDim:F3},{target:F3},{f:F5},SCALE_FIXED");
        }

        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,scene))throw new IOException("Save failed "+scene);
        rows.Add($"{key},SUMMARY,meshes={meshes},0,0,0,textureFix={texFix};scaleFix={scaleFix}");
        Debug.Log($"CACTUS_REPAIR {key} meshes={meshes} textureFix={texFix} scaleFix={scaleFix}");
    }

    [MenuItem("MMUnity/Recovery/Fix Cactus Textures And Size")]
    public static void FixCacti()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var rows=new List<string>{"zone,container,object,before_max_m,target_max_m,factor,status"};
        foreach(var z in CactusZones)RepairZone(z.key,z.scene,rows);
        File.WriteAllLines("Validation/CactusDragonAudit/cactus_repair.csv",rows);
        AssetDatabase.SaveAssets();
    }
    static bool ValidMaterials(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
            if(r.sharedMaterials==null||r.sharedMaterials.Length==0)return false;
            foreach(var m in r.sharedMaterials)
                if(!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader")return false;
        }
        return true;
    }
    static Bounds ObjBounds(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static void Ground(GameObject go,Terrain t,float x,float z)
    {
        var b=ObjBounds(go);float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(gy-b.min.y);
    }
    static string DominantLayer(Terrain t,float x,float z)
    {
        var td=t.terrainData;Vector3 l=new Vector3(x,0,z)-t.transform.position;
        int ax=Mathf.Clamp(Mathf.FloorToInt(l.x/td.size.x*td.alphamapWidth),0,td.alphamapWidth-1);
        int ay=Mathf.Clamp(Mathf.FloorToInt(l.z/td.size.z*td.alphamapHeight),0,td.alphamapHeight-1);
        var a=td.GetAlphamaps(ax,ay,1,1);int best=0;float v=a[0,0,0];
        for(int k=1;k<td.alphamapLayers;k++)if(a[0,0,k]>v){v=a[0,0,k];best=k;}
        return td.terrainLayers[best]?td.terrainLayers[best].name:"";
    }
    static GameObject InstantiateAsset(string path,Transform parent)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);return go;
    }
    static bool Near(List<Vector2> pts,float x,float z,float min)
    {
        float d=min*min;var q=new Vector2(x,z);
        foreach(var p in pts)if((p-q).sqrMagnitude<d)return true;
        return false;
    }
    [MenuItem("MMUnity/Environment/Add Subtle Dragon Isle Realism")]
    public static void AddDragonRealism()
    {
        const string scene="Assets/Scenes/Regions/DragonIsle.unity";
        var sc=EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(!root)throw new Exception("Dragon Isle root missing");
        var t=root.GetComponentInChildren<Terrain>(true);
        var old=root.transform.Find("Dragon Isle - Realism Additions");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var add=new GameObject("Dragon Isle - Realism Additions");add.transform.SetParent(root.transform,false);

        string rockPath="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx";
        string shrubPath="Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx";
        var shrubMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RealisticWorld/Shrub02.mat");
        var rng=new System.Random(62291);
        var occupied=new List<Vector2>();
        var forest=root.transform.Find("Forest - Natural Sparse");
        if(forest)foreach(Transform c in forest){var b=ObjBounds(c.gameObject);if(b.size!=Vector3.zero)occupied.Add(new Vector2(b.center.x,b.center.z));}
        var landmarks=root.transform.Find("Landmarks - Reference");
        var landmarkBounds=landmarks?landmarks.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Select(r=>r.bounds).ToArray():new Bounds[0];

        int rocks=0,shrubs=0,attempts=0;
        Vector3 tp=t.transform.position,sz=t.terrainData.size;
        while((rocks<30||shrubs<16)&&attempts++<7000)
        {
            float x=tp.x+14f+(float)rng.NextDouble()*(sz.x-28f);
            float z=tp.z+14f+(float)rng.NextDouble()*(sz.z-28f);
            float y=t.SampleHeight(new Vector3(x,0,z))+tp.y;if(y<.45f)continue;
            float nx=(x-tp.x)/sz.x,nz=(z-tp.z)/sz.z;float slope=t.terrainData.GetSteepness(nx,nz);
            string layer=DominantLayer(t,x,z);
            bool nearLand=landmarkBounds.Any(b=>x>=b.min.x-9&&x<=b.max.x+9&&z>=b.min.z-9&&z<=b.max.z+9);
            if(nearLand)continue;

            bool wantRock=rocks<30&&(layer.IndexOf("Rock",StringComparison.OrdinalIgnoreCase)>=0||
                                    layer.IndexOf("Dirt",StringComparison.OrdinalIgnoreCase)>=0||
                                    layer.IndexOf("Shore",StringComparison.OrdinalIgnoreCase)>=0);
            if(wantRock&&slope<=38f&&!Near(occupied,x,z,7.5f))
            {
                var go=InstantiateAsset(rockPath,add.transform);if(!go)break;
                go.name=$"DI_Rock_{rocks:000}";go.transform.position=new Vector3(x,0,z);
                go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
                float s=.55f+(float)rng.NextDouble()*.70f;go.transform.localScale*=s;Ground(go,t,x,z);
                var b=ObjBounds(go);
                if(!ValidMaterials(go)||Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))>6f){UnityEngine.Object.DestroyImmediate(go);continue;}
                occupied.Add(new Vector2(x,z));rocks++;continue;
            }

            bool wantShrub=shrubs<16&&(layer.IndexOf("Grass",StringComparison.OrdinalIgnoreCase)>=0||
                                      layer.IndexOf("Dirt",StringComparison.OrdinalIgnoreCase)>=0);
            if(wantShrub&&slope<=24f&&!Near(occupied,x,z,8.5f))
            {
                var go=InstantiateAsset(shrubPath,add.transform);if(!go)break;
                go.name=$"DI_Shrub_{shrubs:000}";go.transform.position=new Vector3(x,0,z);
                go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
                float s=.28f+(float)rng.NextDouble()*.14f;go.transform.localScale*=s;
                if(shrubMat)foreach(var r in go.GetComponentsInChildren<Renderer>(true)){int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;var a=new Material[n];for(int k=0;k<n;k++)a[k]=shrubMat;r.sharedMaterials=a;}
                Ground(go,t,x,z);var b=ObjBounds(go);
                if(!ValidMaterials(go)||Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))>2.5f){UnityEngine.Object.DestroyImmediate(go);continue;}
                occupied.Add(new Vector2(x,z));shrubs++;
            }
        }

        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,scene))throw new IOException("Dragon Isle save failed");
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        File.WriteAllText("Validation/CactusDragonAudit/dragon_isle_realism.txt",$"rocks={rocks}\nshrubs={shrubs}\nattempts={attempts}\nterrain_changed=false\n");
        AssetDatabase.SaveAssets();
        Debug.Log($"DRAGON_ISLE_REALISM rocks={rocks} shrubs={shrubs}");
    }
    [MenuItem("MMUnity/Validation/Validate Cactus And Dragon Realism")]
    public static void Validate()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var lines=new List<string>{"scene,cactus_renderers,textureless,oversized_gt6m,dragon_additions,invalid_materials"};
        foreach(var z in CactusZones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            var mfs=root.GetComponentsInChildren<MeshFilter>(true).Where(IsCactusMesh).ToArray();
            int noTex=0,big=0;
            foreach(var mf in mfs)
            {
                var r=mf.GetComponent<Renderer>();if(!r)continue;
                if(r.sharedMaterials==null||r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||m.GetTexture("_MainTex")==null))noTex++;
                var b=r.bounds.size;if(Mathf.Max(b.x,Mathf.Max(b.y,b.z))>6f)big++;
            }
            lines.Add($"{z.key},{mfs.Length},{noTex},{big},0,0");
        }
        {
            var sc=EditorSceneManager.OpenScene("Assets/Scenes/Regions/DragonIsle.unity",OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            var add=root.transform.Find("Dragon Isle - Realism Additions");
            int cnt=add?add.childCount:0,invalid=0;
            if(add)foreach(Transform c in add)if(!ValidMaterials(c.gameObject))invalid++;
            lines.Add($"DragonIsle,0,0,0,{cnt},{invalid}");
        }
        File.WriteAllLines("Validation/CactusDragonAudit/final_validation.csv",lines);
    }
}
