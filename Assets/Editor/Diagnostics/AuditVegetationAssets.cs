using UnityEditor;
using UnityEngine;
using System;

public static class AuditVegetationAssets
{
    static Bounds B(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static void Check(string path)
    {
        var a=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!a){Debug.Log("VEG_ASSET_MISSING "+path);return;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(a);Bounds b=B(go);
        Debug.Log($"VEG_ASSET {path} size={b.size} center={b.center} rootScale={go.transform.localScale} rootRot={go.transform.rotation.eulerAngles}");
        UnityEngine.Object.DestroyImmediate(go);
    }
    public static void Run()
    {
        Check("Assets/Environment/DesertVegetation/Cactus.fbx");
        Check("Assets/Environment/DesertVegetation/desert_shrubs.fbx");
        Check("Assets/EnvironmentAssets/Gobkit/TreeHigh003.fbx");
        Check("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX");
        Debug.Log("VEG_ASSET_AUDIT_DONE");
    }
}
