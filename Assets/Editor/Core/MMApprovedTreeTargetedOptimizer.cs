using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Globalization;

public static class MMApprovedTreeTargetedOptimizer
{
    class Spec
    {
        public string name,src,outPath;
        public float q;
        public Spec(string n,string s,string o,float quality){name=n;src=s;outPath=o;q=quality;}
    }
    static readonly Spec[] Specs={
        new Spec("ac43f7d2_jacaranda_tree_LOD0_q50",
            "Assets/Optimization/GeneratedMeshes/ac43f7d2_jacaranda_tree_LOD0_q50.asset",
            "Assets/Environment/FinalWorldVegetation/jacaranda_tree_final_q200.asset",0.20f),
        new Spec("bd0d6a6b_island_tree_01_LOD0_q50",
            "Assets/Optimization/GeneratedMeshes/bd0d6a6b_island_tree_01_LOD0_q50.asset",
            "Assets/Environment/FinalWorldVegetation/island_tree_01_final_q200.asset",0.20f),
        new Spec("cc6eeadc_island_tree_02_LOD0_q50",
            "Assets/Optimization/GeneratedMeshes/cc6eeadc_island_tree_02_LOD0_q50.asset",
            "Assets/Environment/FinalWorldVegetation/island_tree_02_final_q200.asset",0.20f),
        new Spec("d37345cb_island_tree_03_LOD0_q50",
            "Assets/Optimization/GeneratedMeshes/d37345cb_island_tree_03_LOD0_q50.asset",
            "Assets/Environment/FinalWorldVegetation/island_tree_03_final_q200.asset",0.20f)
    };
    const string Report="Validation/EnvironmentRealism/approved_tree_targeted_optimization.txt";

    [MenuItem("MM6/Optimization/Test Approved Tree Mesh Reduction")]
    public static void Apply()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var sc=SceneManager.GetActiveScene();
        if(sc.path!="Assets/Scenes/World/Enroth.unity")
            throw new Exception("Expected linked scene.");
        string pre=TransformHash(sc);
        var replacements=new Dictionary<string,Mesh>();
        var report=new StringBuilder();
        report.AppendLine("scene="+sc.path);
        report.AppendLine("scene_hash_pre="+pre);
        foreach(var s in Specs)
        {
            var src=AssetDatabase.LoadAssetAtPath<Mesh>(s.src);
            if(!src)throw new Exception("Missing "+s.src);
            var dst=EnsureSimplified(src,s.outPath,s.q,s.name.Replace("_q50","_final_q200"));
            replacements[s.name]=dst;
            report.AppendLine(s.name+"|src="+Tris(src)+"|final="+Tris(dst)+"|quality="+s.q.ToString("0.00",CultureInfo.InvariantCulture));
        }
        var counts=Specs.ToDictionary(x=>x.name,x=>0);
        foreach(var root in sc.GetRootGameObjects())
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;
            if(replacements.TryGetValue(mf.sharedMesh.name,out var dst))
            {
                counts[mf.sharedMesh.name]++;
                mf.sharedMesh=dst;
                EditorUtility.SetDirty(mf);
                if(PrefabUtility.IsPartOfPrefabInstance(mf))PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
            }
        }
        string post=TransformHash(sc);
        report.AppendLine("scene_hash_post="+post);
        report.AppendLine("scene_transform_match="+(pre==post));
        foreach(var s in Specs)report.AppendLine("replaced|"+s.name+"|"+counts[s.name]);
        if(pre!=post)throw new Exception("Transform hash changed; scene not saved.");
        EditorSceneManager.SaveScene(sc);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Report,report.ToString());
        Debug.Log("MM_TREE_OPT_APPLIED "+string.Join(" ",counts.Select(kv=>kv.Key+"="+kv.Value))+" hashes=True");
    }

    [MenuItem("MM6/Optimization/Revert Approved Tree Mesh Reduction")]
    public static void Revert()
    {
        var sc=SceneManager.GetActiveScene();
        string pre=TransformHash(sc);
        var map=new Dictionary<string,Mesh>();
        foreach(var s in Specs)
        {
            var src=AssetDatabase.LoadAssetAtPath<Mesh>(s.src);
            var dst=AssetDatabase.LoadAssetAtPath<Mesh>(s.outPath);
            if(src&&dst)map[dst.name]=src;
        }
        int n=0;
        foreach(var root in sc.GetRootGameObjects())
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(mf.sharedMesh && map.TryGetValue(mf.sharedMesh.name,out var src))
            {
                mf.sharedMesh=src;n++;EditorUtility.SetDirty(mf);
                if(PrefabUtility.IsPartOfPrefabInstance(mf))PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
            }
        }
        string post=TransformHash(sc);
        if(pre!=post)throw new Exception("Transform hash changed during revert.");
        EditorSceneManager.SaveScene(sc);
        Debug.Log("MM_TREE_OPT_REVERTED count="+n+" hashes=True");
    }

    static Mesh EnsureSimplified(Mesh src,string outPath,float q,string name)
    {
        var e=AssetDatabase.LoadAssetAtPath<Mesh>(outPath);if(e)return e;
        var simp=new MeshSimplifier();
        var opts=SimplificationOptions.Default;opts.EnableSmartLink=true;opts.MaxIterationCount=100;opts.Agressiveness=7.0;
        simp.SimplificationOptions=opts;simp.Initialize(src);simp.SimplifyMesh(q);
        var m=simp.ToMesh();m.name=name;m.RecalculateBounds();
        if(m.subMeshCount!=src.subMeshCount)throw new Exception("Submesh mismatch "+src.name);
        AssetDatabase.CreateAsset(m,outPath);AssetDatabase.SaveAssets();return m;
    }
    static long Tris(Mesh m){long n=0;for(int s=0;s<m.subMeshCount;s++)n+=(long)m.GetIndexCount(s)/3;return n;}
    static string TransformHash(Scene sc)
    {
        var sb=new StringBuilder(1024*128);
        foreach(var root in sc.GetRootGameObjects().OrderBy(x=>x.transform.GetSiblingIndex()))
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            sb.Append(HierarchyKey(t)).Append('|');V(sb,t.localPosition);sb.Append('|');
            var q=t.localRotation;sb.Append(F(q.x)).Append(',').Append(F(q.y)).Append(',').Append(F(q.z)).Append(',').Append(F(q.w)).Append('|');
            V(sb,t.localScale);sb.Append('|').Append(t.gameObject.activeSelf?'1':'0').Append(';');
        }
        using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-","");
    }
    static string HierarchyKey(Transform t){var p=new List<string>();while(t!=null){p.Add(t.GetSiblingIndex()+":"+t.name);t=t.parent;}p.Reverse();return string.Join("/",p);}
    static void V(StringBuilder sb,Vector3 v){sb.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z));}
    static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
}
