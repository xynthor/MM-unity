using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMRoadBorderProfileAudit
{
    class Q{public string k,s;public Q(string a,string b){k=a;s=b;}}
    static Q PV=new Q("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity");
    static Q BS=new Q("Blackshire","Assets/Scenes/Regions/Blackshire.unity");
    static Q FH=new Q("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity");
    static Q SC=new Q("SilverCove","Assets/Scenes/Regions/SilverCove.unity");
    static bool Road(byte[] t,byte[] g,byte[] s,int x,int y){byte r=t[y*128+x],f=s[r],q=g[r];return (f&8)!=0||(q>=8&&q<255);}
    static void Load(Q q,out Terrain tr,out byte[] t,out byte[] g,out byte[] s,out byte[] h)
    {
        var sc=EditorSceneManager.OpenScene(q.s,OpenSceneMode.Single);var root=sc.GetRootGameObjects().First(x=>x.name.IndexOf("Open World",System.StringComparison.OrdinalIgnoreCase)>=0);
        tr=root.GetComponentInChildren<Terrain>(true);string d=$"Assets/World/{q.k}/Data";t=File.ReadAllBytes(d+"/tilemap_u8.bin");g=File.ReadAllBytes(d+"/tile_groups_u8.bin");s=File.ReadAllBytes(d+"/tile_semantics_u8.bin");h=File.ReadAllBytes(d+"/heightmap_u8.bin");
    }

    static void Pair(Q a,Q b,int sy,List<string> o)
    {
        Load(a,out var ta,out var at,out var ag,out var ass,out var ah);Load(b,out var tb,out var bt,out var bg,out var bs,out var bh);
        for(int x=120;x<128;x++){float wx=(x+.5f-64f)*4f,wz=(64f-(sy+.5f))*4f;float y=ta.SampleHeight(new Vector3(wx,0,wz))+ta.transform.position.y;o.Add($"{a.k},{sy},{x},{Road(at,ag,ass,x,sy)},{y:F3},{ah[sy*128+x]*.25f:F3}");}
        for(int x=0;x<8;x++){float wx=(x+.5f-64f)*4f,wz=(64f-(sy+.5f))*4f;float y=tb.SampleHeight(new Vector3(wx,0,wz))+tb.transform.position.y;o.Add($"{b.k},{sy},{x},{Road(bt,bg,bs,x,sy)},{y:F3},{bh[sy*128+x]*.25f:F3}");}
    }
    [MenuItem("MMUnity/Audit Road Border Profiles")]
    public static void Run(){var o=new List<string>{"zone,sy,sx,road,currentH,sourceH"};Pair(PV,BS,26,o);Pair(FH,SC,71,o);Directory.CreateDirectory("Validation");File.WriteAllLines("Validation/RoadBorderProfiles.csv",o);Debug.Log("ROAD_BORDER_PROFILE_DONE");}
}