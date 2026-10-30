using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMBridgeSupportAudit
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
    [MenuItem("MMUnity/Audit Bridge Supports")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation");
        var outp=new List<string>{"zone,object,axis,endA_gap,endB_gap,minY,maxY,status"};
        foreach(var z in Zones) AuditZone(z,outp);
        File.WriteAllLines("Validation/BridgeSupportAudit.csv",outp);
        Debug.Log("BRIDGE_SUPPORT_AUDIT_DONE rows="+(outp.Count-1));
    }
    static void AuditZone(Z z,List<string> o)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);
        var a=root.transform.Find("Architecture - MM6 Original Layout Expanded");
        if(!t||!a)return;
        foreach(Transform c in a.Cast<Transform>())
        {
            string n=c.name.ToLowerInvariant();
            if(!n.Contains("bridge")||n.Contains("trigger"))continue;
            var rs=c.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
            if(rs.Length==0)continue;
            Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
            bool xAxis=b.size.x>=b.size.z;
            float gA=EndGap(c,t,b,xAxis,false),gB=EndGap(c,t,b,xAxis,true);
            string status=(gA>.55f||gB>.55f)?"FLOAT":((gA< -2.0f||gB< -2.0f)?"BURIED":"OK");
            o.Add($"{z.key},{c.name},{(xAxis?"X":"Z")},{gA:F3},{gB:F3},{b.min.y:F3},{b.max.y:F3},{status}");
        }
    }
    static float EndGap(Transform obj,Terrain t,Bounds b,bool xAxis,bool high)
    {
        var pts=new List<Vector3>();
        float edge=xAxis?(high?b.max.x:b.min.x):(high?b.max.z:b.min.z);
        float band=(xAxis?b.size.x:b.size.z)*.16f;
        foreach(var mf in obj.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;
            foreach(var v in mf.sharedMesh.vertices)
            {
                Vector3 w=mf.transform.TransformPoint(v);
                float q=xAxis?w.x:w.z;
                if(Mathf.Abs(q-edge)<=band)pts.Add(w);
            }
        }
        if(pts.Count==0)
        {
            Vector3 p=b.center;
            if(xAxis)p.x=edge;else p.z=edge;
            float ty=t.SampleHeight(p)+t.transform.position.y;
            return b.min.y-ty;
        }
        pts=pts.OrderBy(p=>p.y).Take(Mathf.Max(1,pts.Count/8)).ToList();
        float sum=0f;int n=0;
        foreach(var p in pts)
        {
            float ty=t.SampleHeight(p)+t.transform.position.y;
            sum+=p.y-ty;n++;
        }
        return n>0?sum/n:0f;
    }
}