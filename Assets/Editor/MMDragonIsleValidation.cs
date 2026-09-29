using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMDragonIsleValidation
{
    const string Standalone="Assets/Scenes/Regions/DragonIsle.unity";
    const string Linked="Assets/Scenes/World/Enroth.unity";
    const string Report="Validation/EnvironmentRealism/dragon_isle_validation.csv";
    static string F(float v)=>v.ToString("0.######",CultureInfo.InvariantCulture);

    static Bounds B(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }

    static Transform Find(Transform r,string n)
    {
        if(!r)return null;if(r.name==n)return r;
        foreach(var t in r.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;
        return null;
    }    static string BadAsset(GameObject go)
    {
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;string p=AssetDatabase.GetAssetPath(mf.sharedMesh).ToLowerInvariant();
            if(p.Contains("searsia_lucida")||p.Contains("tree_small_02")||
               p.Contains("/pine_002_new/")||p.Contains("/pine_004_new/")||p.Contains("/pine_005/")||
               p.Contains("/pine_006/")||p.Contains("/pine_007/")||p.Contains("/pine_a/")||
               p.Contains("/pine_b/")||p.Contains("/pine_c/")||p.Contains("/pine_d/"))return p;
        }
        return "";
    }

    static int TextureIssues(GameObject go)
    {
        int n=0;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
            foreach(var m in r.sharedMaterials)
            {
                if(!m){n++;continue;}
                if(m.HasProperty("_MainTex")&&!m.mainTexture)n++;
            }
        }
        return n;
    }

    static long Triangles(GameObject go)
    {
        long n=0;
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh||!mf.gameObject.activeInHierarchy)continue;
            for(int s=0;s<mf.sharedMesh.subMeshCount;s++)n+=(long)mf.sharedMesh.GetIndexCount(s)/3;
        }
        return n;
    }    static void AuditRoot(string scope,Transform root,List<string> rows)
    {
        var terrain=root.GetComponentInChildren<Terrain>(true);
        var forest=Find(root,"Forest - Natural Sparse");
        var water=Find(root,"Ocean + Central Lake Water");
        if(!terrain||!forest){rows.Add($"{scope},ROOT,-,0,0,0,0,0,MISSING");return;}
        int bad=0,tex=0,contact=0,wet=0,shape=0,count=0;long tris=0;
        foreach(Transform c in forest.Cast<Transform>())
        {
            count++;Bounds b=B(c.gameObject);if(b.size==Vector3.zero){shape++;continue;}
            float gy=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;
            float gap=b.min.y-gy;if(Mathf.Abs(gap)>.06f)contact++;
            if(gy<=.18f)wet++;
            float ratio=b.size.y/Mathf.Max(.05f,Mathf.Max(b.size.x,b.size.z));
            if(c.name.StartsWith("Natural_shrub_")?ratio<.18f:ratio<.55f)shape++;
            if(!string.IsNullOrEmpty(BadAsset(c.gameObject)))bad++;
            tex+=TextureIssues(c.gameObject);tris+=Triangles(c.gameObject);
        }
        string wmat="";
        if(water)
        {
            var mr=water.GetComponentInChildren<MeshRenderer>(true);
            if(mr&&mr.sharedMaterial)wmat=AssetDatabase.GetAssetPath(mr.sharedMaterial);
        }
        string status=(bad==0&&tex==0&&contact==0&&wet==0&&shape==0)?"PASS":"FAILED";
        rows.Add(string.Join(",",new[]{scope,"FOREST",count.ToString(),bad.ToString(),tex.ToString(),contact.ToString(),wet.ToString(),shape.ToString(),tris.ToString(),status}));
        rows.Add(string.Join(",",new[]{scope,"WATER",water?"1":"0","0","0","0","0","0","0",wmat.Replace(',',';')}));
    }    [MenuItem("MMUnity/Validation/Validate Dragon Isle Deep")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"scope,kind,count,bad_assets,texture_issues,ground_issues,wet_vegetation,shape_issues,triangles,status_or_material"};
        var sc=EditorSceneManager.OpenScene(Standalone,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(root)AuditRoot("standalone",root.transform,rows);else rows.Add("standalone,ROOT,-,0,0,0,0,0,0,MISSING");

        var lsc=EditorSceneManager.OpenScene(Linked,OpenSceneMode.Single);
        var world=lsc.GetRootGameObjects().FirstOrDefault();
        var lr=world?Find(world.transform,"Dragon Isle - LINKED REFERENCE"):null;
        if(lr)AuditRoot("linked",lr,rows);else rows.Add("linked,ROOT,-,0,0,0,0,0,0,MISSING");

        File.WriteAllLines(Report,rows);
        Debug.Log("MM_DRAGON_DEEP_VALIDATION rows="+(rows.Count-1));
    }
}