using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTreeVerticalHardPass
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity")};

    static bool VegContext(Transform t)
    {
        for(var p=t;p;p=p.parent)
        {
            string n=p.name.ToLowerInvariant();
            if(n.Contains("vegetation")||n.Contains("ecosystem")||n.Contains("grove")||n.Contains("forest"))return true;
        }
        return false;
    }
    static bool TreeLike(Transform t)
    {
        string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)??"";
        string s=(path+" "+t.name).ToLowerInvariant();
        return s.Contains("tree")||s.Contains("pine")||s.Contains("fir")||s.Contains("winter")||s.Contains("dead_quiver");
    }
    static Bounds VisualBounds(Transform t)
    {
        var rs=t.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(t.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static float Score(Transform t)
    {
        var b=VisualBounds(t);return b.size.y/Mathf.Max(.05f,Mathf.Max(b.size.x,b.size.z));
    }
    static Transform CandidateRoot(Transform t)
    {
        var go=PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject);
        if(go&&VegContext(go.transform))return go.transform;
        var p=t;while(p.parent&&VegContext(p.parent)&&!p.parent.name.ToLowerInvariant().Contains("vegetation")&&!p.parent.name.ToLowerInvariant().Contains("ecosystem"))p=p.parent;
        return p;
    }
    [MenuItem("MMUnity/Witcher World - HARD Vertical Trees")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory("Validation");
        var log=new List<string>{"zone,name,x,z,beforeScore,afterScore,rotX,rotY,rotZ"};
        int total=0;
        foreach(var z in Zones)total+=Apply(z,log);
        File.WriteAllLines("Validation/TreeVerticalHardPass.csv",log);
        AssetDatabase.SaveAssets();
        Debug.Log("TREE_VERTICAL_HARD_ALL_DONE fixed="+total);
    }
    static int Apply(Z z,List<string> log)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var roots=sc.GetRootGameObjects();var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception(z.key+" terrain missing");
        var set=new HashSet<Transform>();
        foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
        {
            if(!r||!VegContext(r.transform)||!TreeLike(r.transform))continue;
            var c=CandidateRoot(r.transform);if(c)set.Add(c);
        }
        int changed=0;
        foreach(var t in set)
        {
            if(!t)continue;float before=Score(t);float yaw=t.eulerAngles.y;
            Quaternion[] qs={
                Quaternion.Euler(0,yaw,0),
                Quaternion.Euler(0,yaw,0)*Quaternion.Euler(90,0,0),
                Quaternion.Euler(0,yaw,0)*Quaternion.Euler(-90,0,0),
                Quaternion.Euler(0,yaw,0)*Quaternion.Euler(0,0,90),
                Quaternion.Euler(0,yaw,0)*Quaternion.Euler(0,0,-90)};
            Quaternion best=qs[0];float bestScore=-1f;
            foreach(var q in qs){t.rotation=q;float s=Score(t);if(s>bestScore){bestScore=s;best=q;}}
            t.rotation=best;
            var b=VisualBounds(t);float gy=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;
            t.position+=Vector3.up*(gy-b.min.y);
            var e=t.eulerAngles;
            log.Add($"{z.key},{t.name},{t.position.x:F3},{t.position.z:F3},{before:F3},{bestScore:F3},{e.x:F2},{e.y:F2},{e.z:F2}");
            changed++;
        }
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"TREE_VERTICAL_HARD {z.key} checked={set.Count} fixed={changed}");return changed;
    }
}
