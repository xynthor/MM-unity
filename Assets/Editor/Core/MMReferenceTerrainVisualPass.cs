using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMReferenceTerrainVisualPass
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity")};
    const string RefDir="Assets/EnvironmentAssets/Biomes/ReferenceWorld";
    const int N=128;
    [MenuItem("MMUnity/Reference World - Terrain Visuals SAFE")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Validation");
        var log=new List<string>{"zone,layer,index,before,after"};
        foreach(var z in Zones) Apply(z,log);
        File.WriteAllLines("Validation/ReferenceTerrainVisuals.csv",log);
        AssetDatabase.SaveAssets();
        Debug.Log("REFERENCE_TERRAIN_VISUALS_DONE zones=15 geometryTouched=false alphamapBasePreserved=true");
    }
    static Texture2D Tex(string p)=>AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    static void Set(TerrainLayer[] a,int i,string p,string z,List<string> log)
    {
        if(i<0||i>=a.Length||!a[i])return;
        string before=a[i].diffuseTexture?AssetDatabase.GetAssetPath(a[i].diffuseTexture):"";
        var t=Tex(p);if(!t)throw new Exception("Missing texture "+p);
        a[i].diffuseTexture=t;EditorUtility.SetDirty(a[i]);
        log.Add($"{z},{a[i].name},{i},{before},{p}");
    }
    static Terrain TerrainIn(UnityEngine.SceneManagement.Scene sc)
        =>sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
    static void Apply(Z z,List<string> log)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var t=TerrainIn(sc);if(!t)throw new Exception(z.key+" terrain missing");
        var a=t.terrainData.terrainLayers;if(a==null||a.Length<4)throw new Exception(z.key+" terrain layers <4");
        string grass="Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_CH.png";
        string velen=RefDir+"/velen_mud_grass.png";
        string mire=RefDir+"/mire_wet_green.png";
        string volc=RefDir+"/volcanic_grey.png";
        string desert=RefDir+"/desert_warm.png";
        string black="Assets/EnvironmentAssets/Biomes/black_soil.png";
        string coast="Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg";

        // Same green visual family across the canonical green world.
        if(z.key=="NewSorpigal"||z.key=="CastleIronfist"||z.key=="MistyIslands"||
           z.key=="EelInfestedWaters"||z.key=="BootlegBay"||z.key=="SilverCove")
            Set(a,0,grass,z.key,log);

        // Mire: green/wet forest + dark soil; never pale/white.
        if(z.key=="MireOfTheDamned")
        {
            Set(a,0,mire,z.key,log);Set(a,1,velen,z.key,log);Set(a,3,black,z.key,log);
        }

        // Yellow desert.
        if(z.key=="Dragonsand")
        {
            Set(a,0,desert,z.key,log);Set(a,1,coast,z.key,log);
        }

        // Grey volcanic island: all natural land variants stay neutral volcanic grey.
        if(z.key=="HermitsIsle")
        {
            Set(a,0,volc,z.key,log);Set(a,1,black,z.key,log);Set(a,3,volc,z.key,log);
        }

        // Witcher/Velen-like brown belt: muted earth with visible grass, not flat brown.
        if(z.key=="FreeHaven"||z.key=="Blackshire"||z.key=="ParadiseValley")
            Set(a,0,velen,z.key,log);

        EditorUtility.SetDirty(t.terrainData);
        t.Flush();
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
    }
}
