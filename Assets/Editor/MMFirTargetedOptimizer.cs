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

public static class MMFirTargetedOptimizer
{
    const string PrefabPath = "Assets/Environment/FinalWorldVegetation/fir_sapling.prefab";
    const string SrcB = "Assets/Optimization/GeneratedMeshes/7972739b_fir_sapling_b_q200.asset";
    const string SrcC = "Assets/Optimization/GeneratedMeshes/7972739b_fir_sapling_c_q200.asset";
    const string OutB = "Assets/Environment/FinalWorldVegetation/fir_sapling_b_final_q200.asset";
    const string OutC = "Assets/Environment/FinalWorldVegetation/fir_sapling_c_final_q200.asset";
    const string Report = "Validation/EnvironmentRealism/fir_targeted_optimization.txt";

    [MenuItem("MM6/Optimization/Fix Fir Branch Twig Meshes")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var sc = SceneManager.GetActiveScene();
        if (sc.path != "Assets/Scenes/Enroth_Linked_OpenWorld.unity")
            throw new Exception("Expected linked scene open, got: " + sc.path);

        string scenePre = TransformHash(sc);
        var b = EnsureSimplified(SrcB, OutB, 0.20f, "fir_sapling_b_final_q200");
        var c = EnsureSimplified(SrcC, OutC, 0.20f, "fir_sapling_c_final_q200");
        if (!b || !c) throw new Exception("Failed to create final fir meshes.");

        string prefabPre, prefabPost;
        int prefabB=0, prefabC=0;
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            prefabPre = TransformHash(root);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue;
                if (IsB(mf.sharedMesh)) { mf.sharedMesh=b; prefabB++; EditorUtility.SetDirty(mf); }
                else if (IsC(mf.sharedMesh)) { mf.sharedMesh=c; prefabC++; EditorUtility.SetDirty(mf); }
            }
            prefabPost = TransformHash(root);
            if (prefabPre != prefabPost) throw new Exception("Prefab transform hash changed.");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        int sceneB=0, sceneC=0;
        foreach (var go in sc.GetRootGameObjects())
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mf.sharedMesh) continue;
            if (IsB(mf.sharedMesh))
            {
                mf.sharedMesh=b; sceneB++; EditorUtility.SetDirty(mf);
                if (PrefabUtility.IsPartOfPrefabInstance(mf)) PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
            }
            else if (IsC(mf.sharedMesh))
            {
                mf.sharedMesh=c; sceneC++; EditorUtility.SetDirty(mf);
                if (PrefabUtility.IsPartOfPrefabInstance(mf)) PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
            }
        }

        string scenePost = TransformHash(sc);
        if (scenePre != scenePost) throw new Exception("Linked scene transform hash changed; not saving.");

        EditorSceneManager.SaveScene(sc);
        AssetDatabase.SaveAssets();

        var srcB=AssetDatabase.LoadAssetAtPath<Mesh>(SrcB);
        var srcC=AssetDatabase.LoadAssetAtPath<Mesh>(SrcC);
        long srcBT=Tris(srcB), srcCT=Tris(srcC), outBT=Tris(b), outCT=Tris(c);
        long saved = sceneB*(srcBT-outBT) + sceneC*(srcCT-outCT);

        File.WriteAllText(Report,
            "scene="+sc.path+"\n"+
            "scene_hash_pre="+scenePre+"\n"+
            "scene_hash_post="+scenePost+"\n"+
            "scene_transform_match="+(scenePre==scenePost)+"\n"+
            "prefab_transform_match="+(prefabPre==prefabPost)+"\n"+
            "prefab_replaced_b="+prefabB+"\n"+
            "prefab_replaced_c="+prefabC+"\n"+
            "scene_replaced_b="+sceneB+"\n"+
            "scene_replaced_c="+sceneC+"\n"+
            "source_b_tris="+srcBT+"\n"+
            "final_b_tris="+outBT+"\n"+
            "source_c_tris="+srcCT+"\n"+
            "final_c_tris="+outCT+"\n"+
            "estimated_scene_triangles_saved_vs_q200_candidates="+saved+"\n");

        Debug.Log("MM_FIR_OPT_DONE b="+sceneB+" c="+sceneC+" saved="+saved+
                  " hashes="+(scenePre==scenePost)+" bTris="+outBT+" cTris="+outCT);
    }

    static Mesh EnsureSimplified(string srcPath,string outPath,float quality,string outName)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(outPath);
        if(existing) return existing;
        var src=AssetDatabase.LoadAssetAtPath<Mesh>(srcPath);
        if(!src) throw new Exception("Missing source mesh: "+srcPath);
        var simp=new MeshSimplifier();
        var opts=SimplificationOptions.Default;
        opts.EnableSmartLink=true;
        opts.MaxIterationCount=100;
        opts.Agressiveness=7.0;
        simp.SimplificationOptions=opts;
        simp.Initialize(src);
        simp.SimplifyMesh(quality);
        var opt=simp.ToMesh();
        opt.name=outName;
        opt.RecalculateBounds();
        if(opt.subMeshCount!=src.subMeshCount)
            throw new Exception("Submesh mismatch "+src.name+" "+src.subMeshCount+" -> "+opt.subMeshCount);
        AssetDatabase.CreateAsset(opt,outPath);
        AssetDatabase.SaveAssets();
        return opt;
    }

    static bool IsB(Mesh m)
    {
        string n=m.name.ToLowerInvariant();
        return n=="fir_sapling_b" || n.Contains("fir_sapling_b_q200");
    }
    static bool IsC(Mesh m)
    {
        string n=m.name.ToLowerInvariant();
        return n=="fir_sapling_c" || n.Contains("fir_sapling_c_q200");
    }

    static long Tris(Mesh m)
    {
        if(!m)return 0; long n=0;
        for(int s=0;s<m.subMeshCount;s++) n+=(long)m.GetIndexCount(s)/3;
        return n;
    }

    static string TransformHash(Scene sc)
    {
        var sb=new StringBuilder(1024*128);
        foreach(var root in sc.GetRootGameObjects().OrderBy(x=>x.transform.GetSiblingIndex()))
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                AppendTransform(sb,t);
        return Hash(sb.ToString());
    }

    static string TransformHash(GameObject root)
    {
        var sb=new StringBuilder();
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
            AppendTransform(sb,t);
        return Hash(sb.ToString());
    }

    static void AppendTransform(StringBuilder sb,Transform t)
    {
        sb.Append(HierarchyKey(t)).Append('|');
        AppendV(sb,t.localPosition); sb.Append('|');
        var q=t.localRotation;
        sb.Append(F(q.x)).Append(',').Append(F(q.y)).Append(',').Append(F(q.z)).Append(',').Append(F(q.w)).Append('|');
        AppendV(sb,t.localScale); sb.Append('|');
        sb.Append(t.gameObject.activeSelf?'1':'0').Append(';');
    }

    static string HierarchyKey(Transform t)
    {
        var p=new List<string>();
        while(t!=null){p.Add(t.GetSiblingIndex()+":"+t.name);t=t.parent;}
        p.Reverse(); return string.Join("/",p);
    }
    static void AppendV(StringBuilder sb,Vector3 v)
    { sb.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z)); }
    static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
    static string Hash(string s)
    {
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","");
    }
}
