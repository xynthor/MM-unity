using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

public static class MMRegionEnvironmentAudit
{
    [MenuItem("MMUnity/Validation/Region Environment Audit")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var terrains=UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
        var groups=new Dictionary<Transform,List<Terrain>>();
        foreach(var t in terrains)
        {
            Transform r=t.transform;
            while(r.parent && !r.name.Contains(" - LINKED",StringComparison.OrdinalIgnoreCase)) r=r.parent;
            if(!r.name.Contains(" - LINKED",StringComparison.OrdinalIgnoreCase)) continue;
            if(!groups.TryGetValue(r,out var list)){list=new List<Terrain>();groups[r]=list;}
            list.Add(t);
        }

        var rows=new List<string>();
        foreach(var kv in groups)
        {
            Transform r=kv.Key;
            var rends=r.GetComponentsInChildren<Renderer>(true).Where(x=>x.enabled&&x.gameObject.activeInHierarchy).ToArray();
            long tris=0;
            foreach(var mr in r.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!mr.enabled||!mr.gameObject.activeInHierarchy)continue;
                var mf=mr.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
                for(int s=0;s<mf.sharedMesh.subMeshCount;s++)tris+=(long)mf.sharedMesh.GetIndexCount(s)/3;
            }
            Bounds b=new Bounds(r.position,Vector3.zero);bool have=false;
            foreach(var t in kv.Value)
            {
                var td=t.terrainData;if(!td)continue;
                var tb=new Bounds(t.transform.position+td.size*0.5f,td.size);
                if(!have){b=tb;have=true;}else b.Encapsulate(tb);
            }
            int vegTransforms=r.GetComponentsInChildren<Transform>(true).Count(x=>{
                string p=PathOf(x).ToLowerInvariant();
                return p.Contains("vegetation")||p.Contains("ecosystem")||p.Contains("grove")||p.Contains("source anchored");
            });
            int meshRenderers=r.GetComponentsInChildren<MeshRenderer>(true).Count(x=>x.enabled&&x.gameObject.activeInHierarchy);
            int lods=r.GetComponentsInChildren<LODGroup>(true).Length;
            int waters=r.GetComponentsInChildren<Renderer>(true).Count(x=>x.enabled&&x.gameObject.activeInHierarchy&&x.name.ToLowerInvariant().Contains("water"));
            rows.Add(string.Join("\t",new[]{
                r.name,
                r.position.x.ToString("0.###"),r.position.y.ToString("0.###"),r.position.z.ToString("0.###"),
                b.center.x.ToString("0.###"),b.center.z.ToString("0.###"),b.size.x.ToString("0.###"),b.size.z.ToString("0.###"),
                kv.Value.Count.ToString(),meshRenderers.ToString(),tris.ToString(),vegTransforms.ToString(),lods.ToString(),waters.ToString()
            }));
        }
        rows=rows.OrderBy(x=>float.Parse(x.Split('\t')[4],System.Globalization.CultureInfo.InvariantCulture))
                 .ThenByDescending(x=>float.Parse(x.Split('\t')[5],System.Globalization.CultureInfo.InvariantCulture)).ToList();
        File.WriteAllLines("Validation/EnvironmentRealism/region_inventory.tsv",
            new[]{"region\troot_x\troot_y\troot_z\tcenter_x\tcenter_z\tsize_x\tsize_z\tterrains\tmesh_renderers\tmesh_triangles\tvegetation_path_transforms\tlodgroups\twater_renderers"}.Concat(rows));

        var centers=rows.Select(x=>x.Split('\t')).Select(a=>new{
            name=a[0],
            x=float.Parse(a[4],System.Globalization.CultureInfo.InvariantCulture),
            z=float.Parse(a[5],System.Globalization.CultureInfo.InvariantCulture)
        }).ToList();
        float medianX=centers.OrderBy(x=>x.x).ElementAt(centers.Count/2).x;
        float medianZ=centers.OrderBy(x=>x.z).ElementAt(centers.Count/2).z;
        File.WriteAllLines("Validation/EnvironmentRealism/west_north_regions.txt",
            new[]{"median_x="+medianX.ToString("0.###"),"median_z="+medianZ.ToString("0.###"),"WEST:"}
            .Concat(centers.Where(x=>x.x<medianX).OrderBy(x=>x.x).Select(x=>$"{x.name}|x={x.x:0.###}|z={x.z:0.###}"))
            .Concat(new[]{"NORTH:"})
            .Concat(centers.Where(x=>x.z>medianZ).OrderByDescending(x=>x.z).Select(x=>$"{x.name}|x={x.x:0.###}|z={x.z:0.###}")));

        Debug.Log("MM_REGION_AUDIT regions="+rows.Count+" medianX="+medianX+" medianZ="+medianZ);
    }
    [MenuItem("MMUnity/Validation/Audit Dragon Isle")]
    public static void AuditDragon()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var r=all.FirstOrDefault(t=>t.name=="Dragon Isle - LINKED REFERENCE");
        if(!r)throw new Exception("Dragon Isle linked root missing");
        var lines=new List<string>{"name\tactive\tposition\trenderers\tterrains\tmesh_triangles"};
        foreach(Transform c in r)
        {
            long tris=0;
            foreach(var mf in c.GetComponentsInChildren<MeshFilter>(true))
                if(mf.sharedMesh)for(int s=0;s<mf.sharedMesh.subMeshCount;s++)tris+=(long)mf.sharedMesh.GetIndexCount(s)/3;
            lines.Add(c.name+"\t"+c.gameObject.activeSelf+"\t"+c.position+"\t"+
                      c.GetComponentsInChildren<Renderer>(true).Length+"\t"+
                      c.GetComponentsInChildren<Terrain>(true).Length+"\t"+tris);
        }
        File.WriteAllLines("Validation/EnvironmentRealism/dragon_isle_children.tsv",lines);
        Debug.Log("MM_DRAGON_AUDIT children="+r.childCount);
    }

    static string PathOf(Transform t){string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
}
