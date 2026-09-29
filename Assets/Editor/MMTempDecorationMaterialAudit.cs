using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTempDecorationMaterialAudit
{
    static readonly string[] Paths={
        "Assets/Environment/PolyHaven/Downloaded/shrub_01/shrub_01_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_03/shrub_03_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/shrub_04/shrub_04_1k.fbx",
        "Assets/Environment/PolyHaven/Downloaded/wild_rooibos_bush/wild_rooibos_bush_1k.fbx",
        "Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Plants/Plant_Perennials/PH_Plant_Perennials_a2_1x1x2_A_Prefab.prefab",
        "Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Plants/Plant_Perennials/PH_Plant_Perennials_a2_1x1x2_B_Prefab.prefab",
        "Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Plants/Plant_Perennials/PH_Plant_Perennials_a2_1x1x2_C_Prefab.prefab",
        "Assets/Art/Environment/Vegetation/Trees/Generic_Stump/Stump_04/Stump_04.prefab",
        "Assets/Environment/PolyHaven/Models/wine_barrel_01/wine_barrel_01_1k.fbx",
        "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Juniper_Bush_01/Juniper_Bush_01_Var2.prefab",
        "Assets/Environment/DesertVegetation/Cactus.fbx",
        "Assets/EnvironmentAssets/Gobkit/Rock001.fbx",
        "Assets/EnvironmentAssets/Gobkit/Rock002.fbx"
    };

    [MenuItem("MMUnity/Validation/Temp Decoration Material Audit")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/SourceDecoration3D");
        var rows=new List<string>{"asset,renderers,materials,shaders"};
        foreach(var p in Paths)
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if(!go){rows.Add(p+",0,0,MISSING");continue;}
            var rs=go.GetComponentsInChildren<Renderer>(true);
            var mats=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
            var shaders=mats.Select(m=>m.shader?m.shader.name:"NULL").Distinct().ToArray();
            rows.Add(p+","+rs.Length+","+mats.Length+",\""+string.Join(";",shaders)+"\"");
        }
        File.WriteAllLines("Validation/SourceDecoration3D/material_audit.csv",rows);
    }
}