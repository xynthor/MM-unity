using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWestNorthVegetationFamilyAudit
{
    class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Z("HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity")
    };
    [MenuItem("MMUnity/Validation/West North Vegetation Families")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"zone,root,name,count"};
        foreach(var z in Zones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            if(!root)continue;
            foreach(string rn in new[]{"Vegetation - Source Anchored Final","Realistic Ecosystem Supplementary","Vegetation - Preserved User Additions"})
            {
                var vr=root.transform.Find(rn);if(!vr)continue;
                var dict=new Dictionary<string,int>();
                foreach(var mf in vr.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!mf.sharedMesh)continue;
                    string ap=AssetDatabase.GetAssetPath(mf.sharedMesh);
                    string key=string.IsNullOrEmpty(ap)?mf.sharedMesh.name:(ap+" :: "+mf.sharedMesh.name);
                    dict[key]=dict.TryGetValue(key,out int n)?n+1:1;
                }
                foreach(var kv in dict.OrderByDescending(x=>x.Value).Take(40))
                    rows.Add(z.key+","+rn.Replace(',',';')+","+kv.Key.Replace(',',';')+","+kv.Value);
            }
        }
        File.WriteAllLines("Validation/EnvironmentRealism/west_north_vegetation_families.csv",rows);
        Debug.Log("MM_WN_VEG_FAMILY_AUDIT rows="+(rows.Count-1));
    }
}
