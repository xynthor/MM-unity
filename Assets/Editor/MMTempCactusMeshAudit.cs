using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTempCactusMeshAudit
{
    static bool CactusMesh(MeshFilter mf)
    {
        if(!mf||!mf.sharedMesh)return false;
        var p=AssetDatabase.GetAssetPath(mf.sharedMesh).Replace('\\','/').ToLowerInvariant();
        return p.Contains("/desertvegetation/cactus.fbx");
    }
    static string Mat(Renderer r)
    {
        if(!r)return "NO_RENDERER";
        if(r.sharedMaterials==null||r.sharedMaterials.Length==0)return "NO_MATERIAL";
        if(r.sharedMaterials.Any(m=>!m))return "NULL_MATERIAL";
        if(r.sharedMaterials.Any(m=>!m.shader||m.shader.name=="Hidden/InternalErrorShader"))return "ERROR_SHADER";
        return string.Join(";",r.sharedMaterials.Select(m=>m.name+"|"+m.shader.name).Distinct());
    }
    static string Zone(Transform t)
    {
        for(var p=t;p;p=p.parent)
            if(p.name.IndexOf("LINKED",StringComparison.OrdinalIgnoreCase)>=0)return p.name.Replace(',',';');
        var root=t.root; return root?root.name.Replace(',',';'):"UNKNOWN";
    }
    static void Scan(string scope,string scene,List<string> rows)
    {
        var sc=EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
        foreach(var mf in sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(CactusMesh))
        {
            var r=mf.GetComponent<Renderer>();
            Bounds b=r?r.bounds:new Bounds(mf.transform.position,Vector3.zero);
            var s=mf.transform.lossyScale;
            rows.Add(string.Join(",",new[]{
                scope,Zone(mf.transform),mf.transform.name.Replace(',',';'),
                mf.transform.parent?mf.transform.parent.name.Replace(',',';'):"",
                mf.transform.position.x.ToString("F3"),mf.transform.position.y.ToString("F3"),mf.transform.position.z.ToString("F3"),
                s.x.ToString("F3"),s.y.ToString("F3"),s.z.ToString("F3"),
                b.size.x.ToString("F3"),b.size.y.ToString("F3"),b.size.z.ToString("F3"),
                "\""+Mat(r).Replace("\"","'")+"\""
            }));
        }
    }
    [MenuItem("MMUnity/Validation/Temp Audit Cactus Meshes")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var rows=new List<string>{"scope,zone,mesh,parent,x,y,z,sx,sy,sz,bx,by,bz,material"};
        Scan("standalone_blackshire","Assets/Scenes/Blackshire_SourceGrid.unity",rows);
        Scan("standalone_dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity",rows);
        Scan("standalone_paradise","Assets/Scenes/ParadiseValley_SourceGrid.unity",rows);
        Scan("standalone_hermits","Assets/Scenes/HermitsIsle_SourceGrid.unity",rows);
        Scan("linked","Assets/Scenes/Enroth_Linked_OpenWorld.unity",rows);
        File.WriteAllLines("Validation/CactusDragonAudit/cactus_meshes.csv",rows);
        Debug.Log("MM_CACTUS_MESH_AUDIT rows="+(rows.Count-1));
    }
}