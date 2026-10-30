using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMGlobalVegetationContactRepair
{
    class Z {
        public string key,scene,linked;
        public Z(string k,string s,string l){key=k;scene=s;linked=l;}
    }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity","New Sorpigal - LINKED REFERENCE"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity","Castle Ironfist - LINKED ADJUSTABLE"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity","Mire of the Damned - LINKED REFERENCE"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity","Dragonsand - LINKED REFERENCE"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity","Hermits Isle - LINKED REFERENCE"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity","Misty Islands - LINKED REFERENCE"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity","Bootleg Bay - LINKED REFERENCE"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity","Free Haven - LINKED REFERENCE"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity","Blackshire - LINKED REFERENCE"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity","Paradise Valley - LINKED REFERENCE"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity","Eel Infested Waters - LINKED REFERENCE"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity","Silver Cove - LINKED REFERENCE"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity","White Cap / Frozen Highlands - LINKED REFERENCE"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity","Kriegspire - LINKED REFERENCE"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity","Sweet Water - LINKED REFERENCE")
    };
    const string Audit="Validation/Everything_Audit_Details.csv";
    const string Linked="Assets/Scenes/World/Enroth.unity";
    const string Report="Validation/EnvironmentRealism/global_vegetation_contact_repair.csv";

    class Target { public string zone,name; public float oldAuditGap; public bool remove; }

    [MenuItem("MMUnity/Environment/Fix Global Vegetation Contacts")]
    public static void Run()
    {
        if(!File.Exists(Audit)) throw new Exception("Missing audit "+Audit);
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var targets=LoadTargets();
        var rows=new List<string>{"scope,zone,name,x_before,z_before,y_before,y_after,x_after,z_after,gap_before,gap_after,rot_match,scale_match,status"};
        int fixedStandalone=0,fixedLinked=0,skipped=0;

        foreach(var z in Zones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
            if(!root||!terrain) throw new Exception("Missing root/terrain "+z.key);
            foreach(var t in targets.Where(x=>x.zone==z.key))
            {
                var tr=FindByName(root.transform,t.name);
                if(!tr){rows.Add($"standalone,{z.key},{t.name},,,,,,,,,,,MISSING");skipped++;continue;}
                if(t.remove)
                {
                    Vector3 p=tr.position;UnityEngine.Object.DestroyImmediate(tr.gameObject);
                    EditorSceneManager.MarkSceneDirty(sc);fixedStandalone++;
                    rows.Add($"standalone,{z.key},{t.name},{F(p.x)},{F(p.z)},{F(p.y)},REMOVED,{F(p.x)},{F(p.z)},,,,True,True,REMOVED_ROAD_OR_WET");
                }
                else if(RepairOne(tr,terrain,out string row))
                { EditorSceneManager.MarkSceneDirty(sc);fixedStandalone++; rows.Add("standalone,"+z.key+","+t.name+","+row); }
                else { skipped++; rows.Add("standalone,"+z.key+","+t.name+","+row); }
            }
            if(EditorSceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(sc);
        }

        var lsc=EditorSceneManager.OpenScene(Linked,OpenSceneMode.Single);
        var world=lsc.GetRootGameObjects().FirstOrDefault();
        if(!world)throw new Exception("Linked root missing");
        foreach(var z in Zones)
        {
            var rr=FindByName(world.transform,z.linked);
            if(!rr){rows.Add($"linked,{z.key},{z.linked},,,,,,,,,,,REGION_MISSING");continue;}
            var terrain=rr.GetComponentInChildren<Terrain>(true);
            if(!terrain){rows.Add($"linked,{z.key},{z.linked},,,,,,,,,,,TERRAIN_MISSING");continue;}
            foreach(var t in targets.Where(x=>x.zone==z.key))
            {
                var tr=FindByName(rr,t.name);
                if(!tr){rows.Add($"linked,{z.key},{t.name},,,,,,,,,,,MISSING");skipped++;continue;}
                if(t.remove)
                {
                    Vector3 p=tr.position;UnityEngine.Object.DestroyImmediate(tr.gameObject);
                    EditorSceneManager.MarkSceneDirty(lsc);fixedLinked++;
                    rows.Add($"linked,{z.key},{t.name},{F(p.x)},{F(p.z)},{F(p.y)},REMOVED,{F(p.x)},{F(p.z)},,,,True,True,REMOVED_ROAD_OR_WET");
                }
                else if(RepairOne(tr,terrain,out string row))
                { EditorSceneManager.MarkSceneDirty(lsc);fixedLinked++; rows.Add("linked,"+z.key+","+t.name+","+row); }
                else { skipped++; rows.Add("linked,"+z.key+","+t.name+","+row); }
            }
        }
        if(lsc.isDirty) EditorSceneManager.SaveScene(lsc);
        File.WriteAllLines(Report,rows);
        AssetDatabase.SaveAssets();
        Debug.Log($"MM_GLOBAL_VEG_CONTACT_DONE standalone={fixedStandalone} linked={fixedLinked} skipped={skipped} targets={targets.Count}");
    }

    static List<Target> LoadTargets()
    {
        var list=new List<Target>();
        var wanted=new HashSet<string>(Zones.Select(z=>z.key));
        foreach(var line in File.ReadAllLines(Audit).Skip(1))
        {
            var p=line.Split(',');
            if(p.Length<5)continue;
            string zone=p[0],cat=p[1],name=p[2],detail=string.Join(",",p.Skip(4));
            if(!wanted.Contains(zone)||cat!="VEGETATION_CONTACT")continue;
            bool remove=detail.Contains("road=True")||detail.Contains("wet=True");
            if(remove){list.Add(new Target{zone=zone,name=name,oldAuditGap=0,remove=true});continue;}
            int gi=detail.IndexOf("gap=",StringComparison.OrdinalIgnoreCase);
            if(gi<0)continue;
            int ge=detail.IndexOf(';',gi);
            string gs=ge>gi?detail.Substring(gi+4,ge-(gi+4)):detail.Substring(gi+4);
            if(!float.TryParse(gs,NumberStyles.Float,CultureInfo.InvariantCulture,out float gap))continue;
            if(gap<=.05f&&gap>=-.05f)continue;
            list.Add(new Target{zone=zone,name=name,oldAuditGap=gap,remove=false});
        }
        return list.GroupBy(x=>x.zone+"|"+x.name).Select(g=>g.First()).ToList();
    }

    static bool RepairOne(Transform tr,Terrain terrain,out string row)
    {
        Vector3 p0=tr.position,s0=tr.localScale;Quaternion r0=tr.localRotation;
        Bounds b=BoundsOf(tr.gameObject);
        float gy=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;
        float gap0=b.min.y-gy;
        if(gap0<=.05f&&gap0>=-.05f)
        {
            row=F(p0.x)+","+F(p0.z)+","+F(p0.y)+","+F(p0.y)+","+F(tr.position.x)+","+F(tr.position.z)+","+F(gap0)+","+F(gap0)+",True,True,ALREADY_OK";
            return false;
        }
        tr.position += Vector3.up*(-gap0);
        Bounds b2=BoundsOf(tr.gameObject);
        float gy2=terrain.SampleHeight(new Vector3(b2.center.x,0,b2.center.z))+terrain.transform.position.y;
        float gap1=b2.min.y-gy2;
        bool xz=Mathf.Abs(tr.position.x-p0.x)<.0001f&&Mathf.Abs(tr.position.z-p0.z)<.0001f;
        bool rot=Quaternion.Angle(tr.localRotation,r0)<.0001f;
        bool scl=(tr.localScale-s0).sqrMagnitude<1e-10f;
        if(!xz||!rot||!scl||Mathf.Abs(gap1)>.03f)
        {
            tr.position=p0; tr.localRotation=r0; tr.localScale=s0;
            row=F(p0.x)+","+F(p0.z)+","+F(p0.y)+","+F(p0.y)+","+F(p0.x)+","+F(p0.z)+","+F(gap0)+","+F(gap0)+","+rot+","+scl+",REVERTED";
            return false;
        }
        EditorUtility.SetDirty(tr);
        row=F(p0.x)+","+F(p0.z)+","+F(p0.y)+","+F(tr.position.y)+","+F(tr.position.x)+","+F(tr.position.z)+","+F(gap0)+","+F(gap1)+","+rot+","+scl+",PASS";
        return true;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static Transform FindByName(Transform root,string n)
    {
        if(root.name==n)return root;
        foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;
        return null;
    }
    static string F(float v)=>v.ToString("0.######",CultureInfo.InvariantCulture);
}
