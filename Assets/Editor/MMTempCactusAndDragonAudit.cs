using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTempCactusAndDragonAudit
{
    static Bounds B(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); return b;
    }
    static bool IsCactus(GameObject go)
    {
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh) continue;
            var p=AssetDatabase.GetAssetPath(mf.sharedMesh).Replace('\\','/').ToLowerInvariant();
            if(p.Contains("/desertvegetation/cactus.fbx")) return true;
        }
        return go.name.IndexOf("cactus",StringComparison.OrdinalIgnoreCase)>=0 ||
               go.name.IndexOf("_cac",StringComparison.OrdinalIgnoreCase)>=0;
    }
    static string MatState(GameObject go)
    {
        var mats=go.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
        if(mats.Length==0)return "NO_MATERIALS";
        if(mats.Any(m=>!m))return "NULL_MATERIAL";
        if(mats.Any(m=>!m.shader||m.shader.name=="Hidden/InternalErrorShader"))return "ERROR_SHADER";
        return string.Join(";",mats.Select(m=>m.name+"|"+m.shader.name).Distinct());
    }
    static void AuditRoot(string scope,string zone,Transform root,List<string> rows)
    {
        if(!root)return;
        int i=0;
        foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if(!IsCactus(t.gameObject))continue;
            if(t.parent!=null && IsCactus(t.parent.gameObject))continue;
            var b=B(t.gameObject); var s=t.lossyScale;
            rows.Add(string.Join(",",new[]{
                scope,zone,(i++).ToString(),t.name.Replace(',',';'),
                t.position.x.ToString("F3"),t.position.y.ToString("F3"),t.position.z.ToString("F3"),
                s.x.ToString("F3"),s.y.ToString("F3"),s.z.ToString("F3"),
                b.size.x.ToString("F3"),b.size.y.ToString("F3"),b.size.z.ToString("F3"),
                "\""+MatState(t.gameObject).Replace("\"","'")+"\""
            }));
        }
    }
    static Transform RegionRoot(Scene sc)
    {
        var g=sc.GetRootGameObjects().FirstOrDefault(x=>x.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
        return g?g.transform:null;
    }

    [MenuItem("MMUnity/Validation/Temp Audit Cactus And Dragon Isle")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var rows=new List<string>{"scope,zone,index,name,x,y,z,sx,sy,sz,bx,by,bz,material_state"};
        var zones=new[]{
            new[]{"Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"},
            new[]{"Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"},
            new[]{"ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"},
            new[]{"HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity"},
            new[]{"DragonIsle","Assets/Scenes/DragonIsle_Reference.unity"}
        };
        foreach(var z in zones)
        {
            var sc=EditorSceneManager.OpenScene(z[1],OpenSceneMode.Single);
            AuditRoot("standalone",z[0],RegionRoot(sc),rows);
        }

        var ls=EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
        var master=ls.GetRootGameObjects().FirstOrDefault(g=>g.name=="ENROTH - LINKED SOURCE REGIONS");
        if(master)
        {
            foreach(Transform t in master.GetComponentsInChildren<Transform>(true))
            {
                if(t.name.Contains("Blackshire - LINKED")||t.name.Contains("Dragonsand - LINKED")||
                   t.name.Contains("Paradise Valley - LINKED")||t.name.Contains("Hermits Isle - LINKED")||
                   t.name.Contains("Dragon Isle - LINKED"))
                    AuditRoot("linked",t.name,t,rows);
            }
        }
        File.WriteAllLines("Validation/CactusDragonAudit/cactus_instances.csv",rows);

        var dsc=EditorSceneManager.OpenScene("Assets/Scenes/DragonIsle_Reference.unity",OpenSceneMode.Single);
        var dr=RegionRoot(dsc);
        var cats=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        if(dr) foreach(Transform c in dr)
        {
            string k=c.name;
            if(k.IndexOf("Vegetation",StringComparison.OrdinalIgnoreCase)>=0)k="VEGETATION:"+k;
            else if(k.IndexOf("Water",StringComparison.OrdinalIgnoreCase)>=0)k="WATER:"+k;
            else if(k.IndexOf("Rock",StringComparison.OrdinalIgnoreCase)>=0)k="ROCK:"+k;
            else if(k.IndexOf("Terrain",StringComparison.OrdinalIgnoreCase)>=0)k="TERRAIN:"+k;
            else k="OTHER:"+k;
            cats[k]=c.childCount;
        }
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_isle_root.txt",cats.Select(kv=>kv.Key+"="+kv.Value));
        Debug.Log("MM_TEMP_CACTUS_DRAGON_AUDIT_DONE");
    }
}