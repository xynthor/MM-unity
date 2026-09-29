using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTempTreeAssetAudit
{
    static readonly string[] Paths={
        "Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab",
        "Assets/Environment/FinalWorldVegetation/fir_sapling.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Tree_Dead_001/tree_dead_001.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab"
    };
    static Bounds B(GameObject go){
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0)return new Bounds();
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    [MenuItem("MMUnity/Validation/Temp Tree Asset Audit")]
    public static void Run(){
        Directory.CreateDirectory("Validation/SourceDecoration3D");
        var rows=new List<string>{"asset,size_x,size_y,size_z,renderers,shaders"};
        foreach(var p in Paths){
            var src=AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if(!src){rows.Add(p+",MISSING");continue;}
            var go=Object.Instantiate(src);go.hideFlags=HideFlags.HideAndDontSave;
            var b=B(go);
            var rs=go.GetComponentsInChildren<Renderer>(true);
            var shaders=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader?m.shader.name:"NULL").Distinct();
            rows.Add(p+","+$"{b.size.x:F3},{b.size.y:F3},{b.size.z:F3},"+rs.Length+",\""+string.Join(";",shaders)+"\"");
            Object.DestroyImmediate(go);
        }
        File.WriteAllLines("Validation/SourceDecoration3D/tree_asset_audit.csv",rows);
    }
}