using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

public static class AuditDesertAssetDetails
{
    static void Dump(string path)
    {
        var a=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!a){Debug.Log("DESERT_ASSET_MISSING "+path);return;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(a);
        Debug.Log($"DESERT_ASSET_BEGIN {path} renderers={go.GetComponentsInChildren<Renderer>(true).Length}");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            string mats=string.Join("|",r.sharedMaterials.Select(m=>m?m.name+":"+(m.mainTexture?AssetDatabase.GetAssetPath(m.mainTexture):"NO_MAIN_TEX"):"NULL"));
            Debug.Log($"DESERT_RENDERER {path} name={r.name} enabled={r.enabled} bounds={r.bounds.size} mats={mats}");
        }
        foreach(var t in go.GetComponentsInChildren<Transform>(true))
            if(t!=go.transform && t.childCount==0) Debug.Log($"DESERT_LEAF {path} {t.name}");
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log("DESERT_ASSET_END "+path);
    }
    [MenuItem("MMUnity/Audit Desert Asset Details")]
    public static void Run()
    {
        Dump("Assets/Environment/DesertVegetation/Cactus.fbx");
        Dump("Assets/Environment/DesertVegetation/desert_shrubs.fbx");
        Dump("Assets/Environment/DesertVegetation/Wüstenstrauch.obj.obj");
        Debug.Log("DESERT_ASSET_DETAILS_DONE");
    }
}
