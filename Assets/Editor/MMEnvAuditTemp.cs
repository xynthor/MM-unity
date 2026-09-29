using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

public static class MMEnvAuditTemp
{
    [MenuItem("MMUnity/Validation/Environment Triangle Audit")]
    public static void Run()
    {
        var rs=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var smrs=UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var rows=new List<string>();
        long total=0;
        foreach(var r in rs)
        {
            var mf=r.GetComponent<MeshFilter>();
            var m=mf?mf.sharedMesh:null;
            if(!m) continue;
            long t=0; for(int s=0;s<m.subMeshCount;s++) t += (long)m.GetIndexCount(s)/3;
            total += t;
            rows.Add(t+"\t"+r.name+"\t"+m.name+"\t"+Hierarchy(r.transform)+"\t"+r.bounds.center+"\t"+r.bounds.size);
        }
        foreach(var r in smrs)
        {
            var m=r.sharedMesh; if(!m) continue;
            long t=0; for(int s=0;s<m.subMeshCount;s++) t += (long)m.GetIndexCount(s)/3;
            total += t;
            rows.Add(t+"\t"+r.name+"\t"+m.name+"\t"+Hierarchy(r.transform)+"\t"+r.bounds.center+"\t"+r.bounds.size);
        }
        rows.Sort((a,b)=>{
            long aa=long.Parse(a.Substring(0,a.IndexOf('\t')));
            long bb=long.Parse(b.Substring(0,b.IndexOf('\t')));
            return bb.CompareTo(aa);
        });
        System.IO.Directory.CreateDirectory("Validation/EnvironmentRealism");
        System.IO.File.WriteAllLines("Validation/EnvironmentRealism/top_triangle_renderers.tsv",
            new[]{"triangles\trenderer\tmesh\thierarchy\tcenter\tsize"}.Concat(rows.Take(500)));
        var byMesh=rows.GroupBy(x=>x.Split('\t')[2]).Select(g=>new {
            mesh=g.Key,count=g.Count(),tris=g.Sum(x=>long.Parse(x.Split('\t')[0]))
        }).OrderByDescending(x=>x.tris).Take(200);
        System.IO.File.WriteAllLines("Validation/EnvironmentRealism/top_triangle_meshes.tsv",
            new[]{"total_triangles\tinstances\tmesh"}.Concat(byMesh.Select(x=>$"{x.tris}\t{x.count}\t{x.mesh}")));
        Debug.Log($"MM_ENV_TRI_AUDIT renderers={rows.Count} totalRendererTris={total}");
    }
    [MenuItem("MMUnity/Validation/Report Optimization Candidates")]
    public static void ReportCandidates()
    {
        string[] paths = {
            "Assets/Optimization/GeneratedMeshes/7972739b_fir_sapling_a_q200.asset",
            "Assets/Optimization/GeneratedMeshes/7972739b_fir_sapling_b_q200.asset",
            "Assets/Optimization/GeneratedMeshes/7972739b_fir_sapling_c_q200.asset",
            "Assets/Optimization/GeneratedMeshes/ac43f7d2_jacaranda_tree_LOD0_q50.asset",
            "Assets/Optimization/GeneratedMeshes/bd0d6a6b_island_tree_01_LOD0_q50.asset",
            "Assets/Optimization/GeneratedMeshes/cc6eeadc_island_tree_02_LOD0_q50.asset",
            "Assets/Optimization/GeneratedMeshes/d37345cb_island_tree_03_LOD0_q50.asset"
        };
        foreach(string p in paths)
        {
            var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);
            long t=0;
            if(m) for(int s=0;s<m.subMeshCount;s++) t += (long)m.GetIndexCount(s)/3;
            Debug.Log("MM_ENV_CANDIDATE path="+p+" exists="+(m!=null)+" tris="+t+" name="+(m?m.name:"null"));
        }
        foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                 .Where(x=>x.sharedMesh && (x.sharedMesh.name.Contains("fir_sapling") || x.sharedMesh.name.Contains("jacaranda"))).Take(12))
        {
            Debug.Log("MM_ENV_ACTIVE mesh="+mf.sharedMesh.name+" tris="+mf.sharedMesh.triangles.Length/3+
                      " asset="+AssetDatabase.GetAssetPath(mf.sharedMesh)+" path="+Hierarchy(mf.transform));
        }
    }

    static string Hierarchy(Transform t)
    {
        string p=t.name;
        while(t.parent){t=t.parent;p=t.name+"/"+p;}
        return p;
    }
}
