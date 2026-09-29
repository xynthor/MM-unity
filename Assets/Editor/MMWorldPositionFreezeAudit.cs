using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMWorldPositionFreezeAudit
{
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),new Z("MistyIslands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),new Z("BootlegBay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Z("SilverCove","Assets/Scenes/SilverCove_SourceGrid.unity"),new Z("Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Z("HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),new Z("Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),new Z("FreeHaven","Assets/Scenes/FreeHaven_SourceGrid.unity"),
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity"),new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity")};

    static string PathOf(Transform t)
    {
        var s=new Stack<string>();for(var p=t;p;p=p.parent)s.Push(p.name);return string.Join("/",s.ToArray());
    }
    static bool Ignore(Transform t)
    {
        for(var p=t;p;p=p.parent)
        {
            string n=p.name.ToLowerInvariant();
            if(n.Contains("vegetation")||n.Contains("ecosystem")||n.Contains("terrain")||n.Contains("water")||
               n.Contains("ocean")||n.Contains("shore")||n.Contains("seabed")||n.Contains("audit")||
               n.Contains("camera")||n.Contains("light"))return true;
        }
        return false;
    }
    static IEnumerable<Transform> Frozen(UnityEngine.SceneManagement.Scene sc)
    {
        foreach(var r in sc.GetRootGameObjects())
        foreach(var t in r.GetComponentsInChildren<Transform>(true))
        {
            if(Ignore(t))continue;
            if(t.GetComponent<Renderer>()||t.GetComponent<Collider>())yield return t;
        }
    }
    static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
    static string Q(string s)=>"\""+(s??"").Replace("\"","\"\"")+"\"";

    static Dictionary<string,string> ReadBaseline(string file)
    {
        var d=new Dictionary<string,string>(StringComparer.Ordinal);
        if(!File.Exists(file))return d;
        foreach(var line in File.ReadAllLines(file).Skip(1))
        {
            int p=line.IndexOf(',');if(p<0)continue;
            string k=line.Substring(0,p);d[k]=line.Substring(p+1);
        }
        return d;
    }

    [MenuItem("MMUnity/Realistic World/0 Snapshot Fixed Objects")]
    public static void Snapshot()
    {
        Directory.CreateDirectory("Validation/PositionFreeze");
        foreach(var z in Zones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var rows=new List<string>{"key,x,y,z,rx,ry,rz,sx,sy,sz"};
            foreach(var t in Frozen(sc).OrderBy(x=>PathOf(x)))
            {
                string key=Q(PathOf(t));
                Vector3 p=t.position,e=t.eulerAngles,s=t.lossyScale;
                rows.Add(string.Join(",",new[]{key,F(p.x),F(p.y),F(p.z),F(e.x),F(e.y),F(e.z),F(s.x),F(s.y),F(s.z)}));
            }
            File.WriteAllLines($"Validation/PositionFreeze/{z.key}_baseline.csv",rows);
            Debug.Log($"POSITION_FREEZE_SNAPSHOT {z.key} count={rows.Count-1}");
        }
    }

    [MenuItem("MMUnity/Realistic World/9 Verify Fixed Objects")]
    public static void Verify()
    {
        var summary=new List<string>{"zone,baseline,current,missing,added,changed"};
        int totalChanged=0;
        foreach(var z in Zones)
        {
            string file=$"Validation/PositionFreeze/{z.key}_baseline.csv";
            var baseRows=ReadBaseline(file);
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var cur=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var t in Frozen(sc))
            {
                string k=Q(PathOf(t));Vector3 p=t.position,e=t.eulerAngles,s=t.lossyScale;
                cur[k]=string.Join(",",new[]{F(p.x),F(p.y),F(p.z),F(e.x),F(e.y),F(e.z),F(s.x),F(s.y),F(s.z)});
            }
            int miss=baseRows.Keys.Count(k=>!cur.ContainsKey(k));
            int add=cur.Keys.Count(k=>!baseRows.ContainsKey(k));
            int changed=baseRows.Keys.Count(k=>cur.ContainsKey(k)&&baseRows[k]!=cur[k]);
            totalChanged+=changed+miss+add;
            summary.Add($"{z.key},{baseRows.Count},{cur.Count},{miss},{add},{changed}");
            Debug.Log($"POSITION_FREEZE_VERIFY {z.key} missing={miss} added={add} changed={changed}");
        }
        File.WriteAllLines("Validation/PositionFreeze/verify_summary.csv",summary);
        Debug.Log("POSITION_FREEZE_VERIFY_DONE totalDiff="+totalChanged);
    }
}
