using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
public static class InspectRecoveryVegetation
{
    [MenuItem("MMUnity/Inspect Recovery Vegetation")]
    public static void Run()
    {
        string scenePath="Assets/Scenes/_Recovery_NewSorpigal_Vegetation.unity";
        var sc=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        var sb=new StringBuilder(); sb.AppendLine("RECOVERY VEGETATION HIERARCHY");
        foreach(var root in sc.GetRootGameObjects())
        {
            sb.AppendLine("ROOT "+root.name);
            foreach(Transform c in root.transform)
            {
                string n=c.name.ToLowerInvariant(); if(!n.Contains("veget")&&!n.Contains("tree")&&!n.Contains("forest"))continue;
                int total=c.GetComponentsInChildren<Transform>(true).Length-1;
                sb.AppendLine($"  {c.name} immediate={c.childCount} total={total}");
                foreach(Transform g in c)
                {
                    sb.AppendLine($"    {g.name} immediate={g.childCount} total={g.GetComponentsInChildren<Transform>(true).Length-1}");
                    int generated=0,source=0,manual=0; var byPath=new System.Collections.Generic.Dictionary<string,int>();
                    foreach(Transform item in g)
                    {
                        string nn=item.name.ToLowerInvariant(); bool gen=nn.StartsWith("grovetree_")||nn.StartsWith("forestfill")||nn.StartsWith("shrub_")||nn.StartsWith("fern_")||nn.StartsWith("wateredge_");
                        bool src=nn.StartsWith("6tree")||nn.StartsWith("tree")||nn.StartsWith("6rock")||nn.StartsWith("6flower")||nn.StartsWith("mmbarrel_");
                        if(gen)generated++; else if(src)source++; else {manual++; var po=PrefabUtility.GetCorrespondingObjectFromSource(item.gameObject) as GameObject; string ap=po?AssetDatabase.GetAssetPath(po):"<non-prefab>"; byPath[ap]=byPath.ContainsKey(ap)?byPath[ap]+1:1; sb.AppendLine($"      MANUAL {item.name} pos={item.position} prefab={ap}");}
                    }
                    sb.AppendLine($"      CLASS generated={generated} source={source} manual={manual}");
                    foreach(var kv in byPath.OrderByDescending(k=>k.Value)) sb.AppendLine($"      MANUAL_ASSET {kv.Value} {kv.Key}");
                }
            }
        }
        Directory.CreateDirectory("C:/MMUnityPort/Validation");File.WriteAllText("C:/MMUnityPort/Validation/RecoveryVegetationHierarchy.txt",sb.ToString());
        Debug.Log(sb.ToString());
    }
}