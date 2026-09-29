using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMArchitectureContactRepair
{
    sealed class T {
        public string zone,scene,linked,name;
        public T(string z,string s,string l,string n){zone=z;scene=s;linked=l;name=n;}
    }
    static readonly T[] Targets={
        new T("Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity","Dragonsand - LINKED REFERENCE","014_M013_obelisk_granite"),
        new T("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity","Kriegspire - LINKED REFERENCE","020_M019_c5front")
    };
    const string LinkedScene="Assets/Scenes/Enroth_Linked_OpenWorld.unity";
    const string Report="Validation/EnvironmentRealism/architecture_contact_repair.csv";

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }    static Transform Find(Transform root,string n)
    {
        if(!root)return null;if(root.name==n)return root;
        foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;
        return null;
    }

    static void Gaps(Transform tr,Terrain t,out float minGap,out float maxGap)
    {
        Bounds b=BoundsOf(tr.gameObject);minGap=999f;maxGap=-999f;
        for(int iz=0;iz<5;iz++)for(int ix=0;ix<5;ix++)
        {
            float x=Mathf.Lerp(b.min.x,b.max.x,(ix+.5f)/5f);
            float z=Mathf.Lerp(b.min.z,b.max.z,(iz+.5f)/5f);
            float y=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
            float g=b.min.y-y;minGap=Mathf.Min(minGap,g);maxGap=Mathf.Max(maxGap,g);
        }
    }

    static bool Repair(Transform tr,Terrain terrain,out float delta,out float beforeMin,out float beforeMax,out float afterMin,out float afterMax)
    {
        Vector3 p0=tr.position,s0=tr.localScale;Quaternion r0=tr.localRotation;
        Gaps(tr,terrain,out beforeMin,out beforeMax);delta=0f;
        if(beforeMin>.38f)delta=-beforeMin;
        else
        {
            float lo=-.84f-beforeMin,hi=.37f-beforeMax;
            if(lo<=hi)
            {
                if(0f<lo)delta=lo;else if(0f>hi)delta=hi;else delta=0f;
                if(Mathf.Abs(delta)>.0001f)delta=(lo+hi)*.5f;
            }
        }
        if(Mathf.Abs(delta)>.0001f)tr.position=new Vector3(p0.x,p0.y+delta,p0.z);
        Gaps(tr,terrain,out afterMin,out afterMax);        bool xz=Mathf.Abs(tr.position.x-p0.x)<.0001f&&Mathf.Abs(tr.position.z-p0.z)<.0001f;
        bool rot=Quaternion.Angle(tr.localRotation,r0)<.0001f;
        bool scl=(tr.localScale-s0).sqrMagnitude<1e-10f;
        bool ok=xz&&rot&&scl&&afterMin>=-.85f&&afterMax<=.38f;
        if(!ok){tr.position=p0;tr.localRotation=r0;tr.localScale=s0;Gaps(tr,terrain,out afterMin,out afterMax);return false;}
        EditorUtility.SetDirty(tr);return true;
    }

    static string F(float v)=>v.ToString("0.######",CultureInfo.InvariantCulture);

    [MenuItem("MMUnity/Environment/Fix Remaining Architecture Contacts")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"scope,zone,name,delta,beforeMin,beforeMax,afterMin,afterMax,status"};
        foreach(var x in Targets)
        {
            var sc=EditorSceneManager.OpenScene(x.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
            var tr=root?Find(root.transform,x.name):null;
            if(!terrain||!tr){rows.Add($"standalone,{x.zone},{x.name},,,,,,MISSING");continue;}
            bool ok=Repair(tr,terrain,out float d,out float b0,out float b1,out float a0,out float a1);
            if(ok)EditorSceneManager.MarkSceneDirty(sc);
            if(sc.isDirty)EditorSceneManager.SaveScene(sc);
            rows.Add(string.Join(",",new[]{"standalone",x.zone,x.name,F(d),F(b0),F(b1),F(a0),F(a1),ok?"PASS":"FAILED"}));
        }        var lsc=EditorSceneManager.OpenScene(LinkedScene,OpenSceneMode.Single);
        var world=lsc.GetRootGameObjects().FirstOrDefault();
        foreach(var x in Targets)
        {
            var rr=Find(world.transform,x.linked);
            var terrain=rr?rr.GetComponentInChildren<Terrain>(true):null;
            var tr=rr?Find(rr,x.name):null;
            if(!terrain||!tr){rows.Add($"linked,{x.zone},{x.name},,,,,,MISSING");continue;}
            bool ok=Repair(tr,terrain,out float d,out float b0,out float b1,out float a0,out float a1);
            if(ok)EditorSceneManager.MarkSceneDirty(lsc);
            rows.Add(string.Join(",",new[]{"linked",x.zone,x.name,F(d),F(b0),F(b1),F(a0),F(a1),ok?"PASS":"FAILED"}));
        }
        if(lsc.isDirty)EditorSceneManager.SaveScene(lsc);
        File.WriteAllLines(Report,rows);AssetDatabase.SaveAssets();
        Debug.Log("MM_ARCH_CONTACT_REPAIR_DONE");
    }
}