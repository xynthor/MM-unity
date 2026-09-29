using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMSorpigalTreeUpgrade
{
    const string ScenePath="Assets/Scenes/NewSorpigal_OpenWorld.unity";
    const string MatDir="Assets/Materials/RealisticWorld/Sorpigal/Trees";

    sealed class Spec
    {
        public string id,path,trunkDiff,trunkNor,leafDiff,leafNor;
        public bool singleCutout;
        public Spec(string i,string p,string td,string tn,string ld,string ln,bool one=false)
        {id=i;path=p;trunkDiff=td;trunkNor=tn;leafDiff=ld;leafNor=ln;singleCutout=one;}
    }

    static readonly Spec[] Specs={
        new Spec("searsia_lucida",
            "Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx",
            "Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_rgba_1k.png",null,
            "Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_rgba_1k.png",
            "Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_nor_gl_1k.exr",true),
        new Spec("fir_sapling",
            "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx",
            "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_branches_diff_1k.png",
            "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_branches_nor_gl_1k.png",
            "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_twigs_rgba_1k.png",
            "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_twigs_nor_gl_1k.png"),
        new Spec("island_tree_01",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_01/island_tree_01_1k.fbx",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_01/textures/island_tree_01_branches_diff_1k.png",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_01/textures/island_tree_01_branches_nor_gl_1k.png",
            "Assets/Materials/RealisticWorld/Sorpigal/Textures/island_tree_01_leaves_rgba.png",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_01/textures/island_tree_01_leaves_nor_gl_1k.png"),
        new Spec("island_tree_02",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_02/island_tree_02_1k.fbx",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_02/textures/island_tree_02_branches_diff_1k.png",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_02/textures/island_tree_02_branches_nor_gl_1k.png",
            "Assets/Materials/RealisticWorld/Sorpigal/Textures/island_tree_02_leaves_rgba.png",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_02/textures/island_tree_02_leaves_nor_gl_1k.png"),
        new Spec("island_tree_03",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_03/island_tree_03_1k.fbx",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_03/textures/island_tree_03_branches_diff_1k.png",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_03/textures/island_tree_03_branches_nor_gl_1k.png",
            "Assets/Materials/RealisticWorld/Sorpigal/Textures/island_tree_03_leaves_rgba.png",
            "Assets/Environment/PolyHaven/Downloaded/island_tree_03/textures/island_tree_03_leaves_nor_gl_1k.png"),
        new Spec("jacaranda_tree",
            "Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/jacaranda_tree_1k.fbx",
            "Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/textures/jacaranda_tree_trunk_diff_1k.png",
            "Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/textures/jacaranda_tree_trunk_nor_gl_1k.png",
            "Assets/Materials/RealisticWorld/Sorpigal/Textures/jacaranda_tree_leaves_rgba.png",
            "Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/textures/jacaranda_tree_leaves_nor_gl_1k.png")
    };

    static GameObject LoadGO(string p)
    {
        var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        return g ? g : AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }

    static Material Mat(string name,string diff,string normal,bool cutout)
    {
        Directory.CreateDirectory(MatDir);
        string p=$"{MatDir}/{name}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}else m.shader=sh;
        var d=string.IsNullOrEmpty(diff)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(diff);
        var n=string.IsNullOrEmpty(normal)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
        if(!d)throw new Exception("Missing diffuse texture for "+name+" path="+diff);
        m.SetTexture("_MainTex",d);
        if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        m.color=Color.white;
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.025f);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0f);
        if(cutout)
        {
            m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.34f);m.SetOverrideTag("RenderType","TransparentCutout");
            m.EnableKeyword("_ALPHATEST_ON");m.DisableKeyword("_ALPHABLEND_ON");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue=2450;if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);
        }
        EditorUtility.SetDirty(m);return m;
    }

    static void ApplyMaterials(GameObject go,Spec s)
    {
        var trunk=Mat(s.id+"_trunk",s.trunkDiff,s.trunkNor,s.singleCutout);
        var leaf=Mat(s.id+"_leaf",s.leafDiff,s.leafNor,true);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled)continue;
            string n=(r.gameObject.name+" "+string.Join(" ",(r.sharedMaterials??new Material[0]).Where(x=>x).Select(x=>x.name))).ToLowerInvariant();
            bool leafy=s.singleCutout||n.Contains("leaf")||n.Contains("leaves")||n.Contains("twig")||n.Contains("foliage");
            int count=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
            var a=new Material[count];for(int i=0;i<count;i++)a[i]=leafy?leaf:trunk;r.sharedMaterials=a;
        }
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }

    static float Ratio(Bounds b)=>b.size.y/Mathf.Max(.05f,Mathf.Max(b.size.x,b.size.z));

    static Quaternion BestVisualRotation(GameObject wrapper,GameObject visual,out float bestRatio)
    {
        var candidates=new[]{
            Quaternion.identity,
            Quaternion.Euler(90,0,0),Quaternion.Euler(-90,0,0),
            Quaternion.Euler(0,0,90),Quaternion.Euler(0,0,-90)
        };
        float best=-999f;Quaternion bestQ=Quaternion.identity;bestRatio=0f;
        foreach(var q in candidates)
        {
            visual.transform.localRotation=q;
            var b=BoundsOf(wrapper);if(b.size==Vector3.zero)continue;
            float ratio=Ratio(b);
            float above=Mathf.Max(0f,b.max.y-wrapper.transform.position.y);
            float below=Mathf.Max(0f,wrapper.transform.position.y-b.min.y);
            float upright=above/Mathf.Max(.001f,above+below);
            float score=ratio+upright*.85f;
            if(score>best){best=score;bestQ=q;bestRatio=ratio;}
        }
        visual.transform.localRotation=bestQ;
        return bestQ;
    }

    static void ScaleHeight(GameObject go,float h)
    {
        var b=BoundsOf(go);if(b.size.y>.05f)go.transform.localScale*=h/b.size.y;
    }

    static float Ground(GameObject go,Terrain t)
    {
        float x=go.transform.position.x,z=go.transform.position.z;
        var b=BoundsOf(go);if(b.size==Vector3.zero)return 999f;
        float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(gy-b.min.y);
        go.transform.position=new Vector3(x,go.transform.position.y,z);
        return BoundsOf(go).min.y-gy;
    }

    static int Hash(string n,float x,float z)
    {
        unchecked{return n.GetHashCode()*397 ^ Mathf.RoundToInt(x*100f)*73856093 ^ Mathf.RoundToInt(z*100f)*19349663;}
    }

    static bool MaterialsOK(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled)continue;
            var a=r.sharedMaterials;if(a==null||a.Length==0)return false;
            foreach(var m in a)
                if(!m||!m.shader||!m.shader.isSupported||m.shader.name.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0||!m.mainTexture)
                    return false;
        }
        return true;
    }

    static GameObject SpawnOne(GameObject old,Transform parent,Terrain terrain,Spec spec,int seed,float targetH,out string audit)
    {
        float x=old.transform.position.x,z=old.transform.position.z;
        string name=old.name;
        var src=LoadGO(spec.path);if(!src)throw new Exception("Missing tree asset "+spec.path);

        var wrap=new GameObject(name);
        wrap.transform.SetParent(parent,true);
        wrap.transform.position=new Vector3(x,0f,z);
        wrap.transform.rotation=Quaternion.Euler(0f,(Mathf.Abs(seed)%36000)/100f,0f);
        wrap.transform.localScale=Vector3.one;

        var visual=(GameObject)PrefabUtility.InstantiatePrefab(src);
        visual.name="Visual_"+spec.id;
        visual.transform.SetParent(wrap.transform,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        visual.transform.localScale=Vector3.one;
        ApplyMaterials(visual,spec);

        ScaleHeight(wrap,targetH);
        float gap=Ground(wrap,terrain);
        float ratio=Ratio(BoundsOf(wrap));
        bool rootUpright=Mathf.Abs(Mathf.DeltaAngle(wrap.transform.eulerAngles.x,0f))<.01f&&Mathf.Abs(Mathf.DeltaAngle(wrap.transform.eulerAngles.z,0f))<.01f;
        bool visualUpright=Mathf.Abs(Mathf.DeltaAngle(visual.transform.localEulerAngles.x,0f))<.01f&&Mathf.Abs(Mathf.DeltaAngle(visual.transform.localEulerAngles.z,0f))<.01f;
        bool ok=Mathf.Abs(gap)<=.06f&&MaterialsOK(wrap)&&rootUpright&&visualUpright&&
                Mathf.Abs(wrap.transform.position.x-x)<=.002f&&Mathf.Abs(wrap.transform.position.z-z)<=.002f;
        audit=$"{name},{spec.id},{x:F3},{z:F3},{wrap.transform.eulerAngles.y:F2},0.0,0.0,{ratio:F3},{gap:F4},{(ok?"PASS":"FAIL")}";
        if(!ok){UnityEngine.Object.DestroyImmediate(wrap);throw new Exception("Tree failed validation "+audit);}
        UnityEngine.Object.DestroyImmediate(old);
        return wrap;
    }

    static IEnumerable<GameObject> TreeInstances(Transform trees)
    {
        foreach(Transform c in trees)
            if(c.gameObject.GetComponentInChildren<Renderer>(true))yield return c.gameObject;
    }

    static bool IsSource(GameObject go)=>go.name.StartsWith("6tree",StringComparison.OrdinalIgnoreCase);
    static bool IsGrove(GameObject go)=>go.name.StartsWith("GroveTree_",StringComparison.OrdinalIgnoreCase);

    [MenuItem("MMUnity/Locked/SORPIGAL Tree Upgrade _F7")]
    public static void Run()
    {
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var terrain=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception("Sorpigal terrain missing");
        var veg=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                  .FirstOrDefault(t=>t.name=="Vegetation - MM Anchors + Natural Groves");
        if(!veg)throw new Exception("Sorpigal vegetation root missing");
        var trees=veg.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Trees");
        if(!trees)throw new Exception("Sorpigal Trees container missing");

        var all=TreeInstances(trees).ToList();
        var sources=all.Where(IsSource).ToList();
        var groves=all.Where(IsGrove).ToList();
        if(sources.Count==0)throw new Exception("No Sorpigal source trees found");

        var sourceXZ=sources.ToDictionary(g=>g.name,g=>new Vector2(g.transform.position.x,g.transform.position.z));
        var rows=new List<string>{"name,variant,x,z,yaw,visualRotX,visualRotZ,verticalRatio,groundGap,status"};
        int removedGrove=0,replaced=0;
        var usage=Specs.ToDictionary(s=>s.id,s=>0);

        foreach(var old in sources.ToList())
        {
            int seed=Hash(old.name,old.transform.position.x,old.transform.position.z);
            var spec=Specs[Mathf.Abs(seed)%Specs.Length];
            float oldH=BoundsOf(old).size.y;
            float h=oldH>2f?Mathf.Clamp(oldH,6.0f,12.5f):Mathf.Lerp(7f,11f,(Mathf.Abs(seed)%1000)/999f);
            SpawnOne(old,trees,terrain,spec,seed,h,out string audit);rows.Add(audit);usage[spec.id]++;replaced++;
        }

        foreach(var old in groves.ToList())
        {
            int seed=Hash(old.name,old.transform.position.x,old.transform.position.z);
            // Density is frozen here: replace quality/orientation only, never thin again.
            var spec=Specs[Mathf.Abs(seed/7+113)%Specs.Length];
            float h=Mathf.Lerp(5.5f,10.0f,(Mathf.Abs(seed)%1000)/999f);
            SpawnOne(old,trees,terrain,spec,seed,h,out string audit);rows.Add(audit);usage[spec.id]++;replaced++;
        }

        var now=TreeInstances(trees).ToList();
        int forbidden=0,horizontal=0,floaters=0,badMat=0;
        foreach(var go in now)
        {
            string p="";
            var child=go.transform.Cast<Transform>().FirstOrDefault();
            if(child)
            {
                var src=PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject) as GameObject;
                if(src)p=AssetDatabase.GetAssetPath(src);
            }
            if(p.IndexOf("Banyan",StringComparison.OrdinalIgnoreCase)>=0||
               p.IndexOf("tree_small_02",StringComparison.OrdinalIgnoreCase)>=0||
               p.IndexOf("/Pines/",StringComparison.OrdinalIgnoreCase)>=0)forbidden++;
            var visual=go.transform.Cast<Transform>().FirstOrDefault();
            bool badRootTilt=Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.x,0f))>.01f||
                             Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.z,0f))>.01f;
            bool badVisualTilt=visual&&(Mathf.Abs(Mathf.DeltaAngle(visual.localEulerAngles.x,0f))>.01f||
                                       Mathf.Abs(Mathf.DeltaAngle(visual.localEulerAngles.z,0f))>.01f);
            if(badRootTilt||badVisualTilt)horizontal++;
            float gy=terrain.SampleHeight(go.transform.position)+terrain.transform.position.y;
            if(Mathf.Abs(BoundsOf(go).min.y-gy)>.06f)floaters++;
            if(!MaterialsOK(go))badMat++;
        }
        if(forbidden>0||horizontal>0||floaters>0||badMat>0)
            throw new Exception($"Sorpigal tree audit forbidden={forbidden} horizontal={horizontal} floaters={floaters} badMat={badMat}");

        foreach(var kv in sourceXZ)
        {
            var t=trees.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name==kv.Key);
            if(!t)throw new Exception("Missing source tree "+kv.Key);
            if(Mathf.Abs(t.position.x-kv.Value.x)>.002f||Mathf.Abs(t.position.z-kv.Value.y)>.002f)
                throw new Exception("Source tree XZ changed "+kv.Key);
        }

        Directory.CreateDirectory("Validation/SorpigalTreeUpgrade");
        File.WriteAllLines("Validation/SorpigalTreeUpgrade/trees.csv",rows);
        File.WriteAllLines("Validation/SorpigalTreeUpgrade/summary.csv",new[]{
            "source_before,grove_before,total_after,grove_removed,replaced,forbidden,horizontal,floaters,bad_material",
            $"{sources.Count},{groves.Count},{now.Count},{removedGrove},{replaced},{forbidden},{horizontal},{floaters},{badMat}"
        });
        File.WriteAllLines("Validation/SorpigalTreeUpgrade/variants.csv",
            new[]{"variant,count"}.Concat(usage.Select(kv=>$"{kv.Key},{kv.Value}")));

        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"SORPIGAL_TREE_UPGRADE_DONE source={sources.Count} groveBefore={groves.Count} groveRemoved={removedGrove} totalAfter={now.Count} variants={usage.Count}");
    }
}
