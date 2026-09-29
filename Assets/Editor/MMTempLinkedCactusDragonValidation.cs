using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTempLinkedCactusDragonValidation
{
    static bool Cactus(MeshFilter mf)
    {
        if(!mf||!mf.sharedMesh)return false;
        string p=AssetDatabase.GetAssetPath(mf.sharedMesh).Replace('\\','/').ToLowerInvariant();
        return p.Contains("/desertvegetation/cactus.fbx");
    }
    [MenuItem("MMUnity/Validation/Temp Linked Cactus Dragon")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var sc=EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
        var ts=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var lines=new List<string>{"zone,active_cactus,textureless,oversized_gt6m"};
        foreach(string zone in new[]{"Blackshire","Dragonsand","Paradise Valley","Hermits Isle"})
        {
            var root=ts.FirstOrDefault(t=>t.name.StartsWith(zone,StringComparison.OrdinalIgnoreCase)&&t.name.IndexOf("LINKED",StringComparison.OrdinalIgnoreCase)>=0);
            if(!root){lines.Add($"{zone},-1,-1,-1");continue;}
            int total=0,bad=0,big=0;
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true).Where(Cactus))
            {
                if(!mf.gameObject.activeInHierarchy)continue;
                var r=mf.GetComponent<Renderer>();if(!r||!r.enabled)continue;
                total++;
                if(r.sharedMaterials==null||r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||m.GetTexture("_MainTex")==null))bad++;
                var b=r.bounds.size;if(Mathf.Max(b.x,Mathf.Max(b.y,b.z))>6f)big++;
            }
            lines.Add($"{zone},{total},{bad},{big}");
        }
        var di=ts.FirstOrDefault(t=>t.name.StartsWith("Dragon Isle",StringComparison.OrdinalIgnoreCase)&&t.name.IndexOf("LINKED",StringComparison.OrdinalIgnoreCase)>=0);
        int add=0,invalid=0;
        if(di)
        {
            var a=di.Find("Dragon Isle - Realism Additions");add=a?a.childCount:0;
            if(a)foreach(Transform c in a)
                foreach(var r in c.GetComponentsInChildren<Renderer>(true))
                    if(r.enabled&&(r.sharedMaterials==null||r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader"))){invalid++;break;}
        }
        File.WriteAllLines("Validation/CactusDragonAudit/linked_final_validation.csv",lines);
        File.WriteAllText("Validation/CactusDragonAudit/linked_dragon_realism.txt",$"additions={add}\ninvalid_material_objects={invalid}\n");
        Debug.Log("MM_LINKED_CACTUS_DRAGON_VALIDATION_DONE");
    }
}