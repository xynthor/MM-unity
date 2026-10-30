using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;

[InitializeOnLoad]
public static class MMGlobalPerformanceOptimizer
{
    const string Root = "C:/MMUnityPort";
    const string OutDir = Root + "/Validation/Optimization";
    const string GenDir = "Assets/Optimization/GeneratedMeshes";
    const string RestoreScene = "Assets/Scenes/Regions/NewSorpigal.unity";
    const string Trigger = Root + "/Temp/RUN_GLOBAL_PERF_OPTIMIZER.trigger";

    enum Phase { Idle, Collect, MakeReadable, Simplify, RestoreReadable, Replace, Verify, Done }
    static Phase phase = Phase.Idle;
    static string[] scenes;
    static int sceneIndex, itemIndex;
    static Dictionary<string,Target> targets;
    static Dictionary<string,string> preHash;
    static Dictionary<string,bool> originalReadable;
    static List<string> modelAssets;
    static List<Target> targetList;
    static StringBuilder mapLog;
    static StringBuilder sceneLog;
    static StringBuilder errorLog;

    class Target
    {
        public string key, asset, meshName, generatedPath;
        public long triEach, optimizedTri;
        public int uses, verts;
        public float quality;
        public Mesh optimizedMesh;
    }

    static MMGlobalPerformanceOptimizer()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        if (File.Exists(Trigger)) {
            File.Delete(Trigger);
            Start();
        }
    }

    [MenuItem("MM6/Optimization/Optimize All Live Maps - Performance Only")]
    public static void Start()
    {
        if (phase != Phase.Idle && phase != Phase.Done) return;
        Directory.CreateDirectory(OutDir);
        EnsureAssetFolder("Assets/Optimization");
        EnsureAssetFolder(GenDir);
        scenes = AssetDatabase.FindAssets("t:Scene", new[]{"Assets/Scenes"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/RestoreBackups/"))
            .Where(p => !Path.GetFileName(p).StartsWith("_Recovery_"))
            .OrderBy(p => p).ToArray();

        targets = new Dictionary<string,Target>();
        preHash = new Dictionary<string,string>();
        originalReadable = new Dictionary<string,bool>();
        targetList = null;
        modelAssets = null;
        sceneIndex = 0;
        itemIndex = 0;
        mapLog = new StringBuilder();
        sceneLog = new StringBuilder();
        errorLog = new StringBuilder();
        mapLog.AppendLine("source_asset,mesh_name,uses,original_triangles,quality,optimized_triangles,generated_asset");
        sceneLog.AppendLine("scene,replacements,pre_transform_hash,post_transform_hash,hash_match,triangles_after");
        phase = Phase.Collect;
        WriteStatus("COLLECT 0/" + scenes.Length);
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
    }

    static void Step()
    {
        try {
            switch (phase) {
                case Phase.Collect: StepCollect(); break;
                case Phase.MakeReadable: StepMakeReadable(); break;
                case Phase.Simplify: StepSimplify(); break;
                case Phase.RestoreReadable: StepRestoreReadable(); break;
                case Phase.Replace: StepReplace(); break;
                case Phase.Verify: StepVerify(); break;
                case Phase.Done: Finish(); break;
            }
        } catch (Exception ex) {
            errorLog.AppendLine("FATAL phase=" + phase + " index=" + sceneIndex + "/" + itemIndex);
            errorLog.AppendLine(ex.ToString());
            File.WriteAllText(OutDir + "/optimizer_errors.txt", errorLog.ToString());
            WriteStatus("FAILED phase=" + phase + "\n" + ex.Message);
            RestoreSorpigal();
            EditorApplication.update -= Step;
            phase = Phase.Idle;
        }
    }

    static void StepCollect()
    {
        if (sceneIndex >= scenes.Length) {
            targetList = targets.Values
                .Where(t => t.uses >= 10 && t.triEach >= 50000 && t.asset.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.triEach * (long)t.uses).ToList();
            modelAssets = targetList.Select(t=>t.asset).Distinct().OrderBy(x=>x).ToList();
            File.WriteAllText(OutDir + "/pre_transform_hashes.csv",
                "scene,transform_hash\n" + string.Join("\n", preHash.OrderBy(x=>x.Key).Select(x=>Q(x.Key)+","+x.Value)));
            File.WriteAllText(OutDir + "/optimizer_targets.txt",
                string.Join("\n", targetList.Select(t=>$"{t.uses}x {t.triEach} {t.asset} :: {t.meshName}")));
            itemIndex = 0;
            phase = Phase.MakeReadable;
            WriteStatus("TARGETS " + targetList.Count + " meshes in " + modelAssets.Count + " model assets");
            return;
        }

        string sp = scenes[sceneIndex];
        WriteStatus("COLLECT " + (sceneIndex+1) + "/" + scenes.Length + "\n" + sp);
        var sc = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
        preHash[sp] = TransformHash(sc);

        foreach (var root in sc.GetRootGameObjects()) {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
                var m = mf.sharedMesh;
                if (!m) continue;
                string ap = AssetDatabase.GetAssetPath(m);
                long tri = TriangleCount(m);
                if (tri < 50000 || !IsVegetation(ap, m.name)) continue;
                string key = ap + "|" + m.name;
                if (!targets.TryGetValue(key, out var t)) {
                    t = new Target { key=key, asset=ap, meshName=m.name, triEach=tri, verts=m.vertexCount };
                    targets[key]=t;
                }
                t.uses++;
            }
        }
        sceneIndex++;
    }

    static void StepMakeReadable()
    {
        if (itemIndex >= modelAssets.Count) {
            itemIndex = 0;
            phase = Phase.Simplify;
            return;
        }
        string ap = modelAssets[itemIndex];
        WriteStatus("MAKE READABLE " + (itemIndex+1) + "/" + modelAssets.Count + "\n" + ap);
        var imp = AssetImporter.GetAtPath(ap) as ModelImporter;
        if (imp != null) {
            originalReadable[ap] = imp.isReadable;
            if (!imp.isReadable) {
                imp.isReadable = true;
                imp.SaveAndReimport();
            }
        }
        itemIndex++;
    }

    static void StepSimplify()
    {
        if (itemIndex >= targetList.Count) {
            AssetDatabase.SaveAssets();
            itemIndex = 0;
            phase = Phase.RestoreReadable;
            return;
        }

        var t = targetList[itemIndex];
        WriteStatus("SIMPLIFY " + (itemIndex+1) + "/" + targetList.Count +
            "\n" + t.asset + "\n" + t.meshName + "\ntris=" + t.triEach);

        var src = AssetDatabase.LoadAllAssetsAtPath(t.asset).OfType<Mesh>().FirstOrDefault(m=>m.name==t.meshName);
        if (!src) {
            errorLog.AppendLine("MISSING SOURCE " + t.key);
            itemIndex++;
            return;
        }
        t.quality = QualityFor(t.triEach);
        string guid = AssetDatabase.AssetPathToGUID(t.asset);
        string safe = SafeName(t.meshName);
        t.generatedPath = GenDir + "/" + guid.Substring(0,8) + "_" + safe + "_q" + Mathf.RoundToInt(t.quality*1000) + ".asset";

        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(t.generatedPath);
        if (existing) {
            t.optimizedMesh = existing;
            t.optimizedTri = TriangleCount(existing);
            AppendMap(t);
            itemIndex++;
            return;
        }

        var simp = new MeshSimplifier();
        var opts = SimplificationOptions.Default;
        opts.EnableSmartLink = true;
        opts.MaxIterationCount = 100;
        opts.Agressiveness = 7.0;
        simp.SimplificationOptions = opts;
        simp.Initialize(src);
        simp.SimplifyMesh(t.quality);
        var opt = simp.ToMesh();
        opt.name = t.meshName + "_OPT";
        opt.RecalculateBounds();

        if (opt.subMeshCount != src.subMeshCount)
            throw new Exception("Submesh mismatch for " + t.key + ": " + src.subMeshCount + " -> " + opt.subMeshCount);
        AssetDatabase.CreateAsset(opt, t.generatedPath);
        AssetDatabase.SaveAssets();
        t.optimizedMesh = opt;
        t.optimizedTri = TriangleCount(opt);
        AppendMap(t);
        itemIndex++;
        GC.Collect();
    }

    static void AppendMap(Target t)
    {
        mapLog.AppendLine(string.Join(",", new[]{
            Q(t.asset), Q(t.meshName), t.uses.ToString(),
            t.triEach.ToString(), t.quality.ToString("0.###",CultureInfo.InvariantCulture),
            t.optimizedTri.ToString(), Q(t.generatedPath)
        }));
        File.WriteAllText(OutDir + "/optimized_mesh_mapping.csv", mapLog.ToString());
    }

    static void StepRestoreReadable()
    {
        if (itemIndex >= modelAssets.Count) {
            itemIndex = 0;
            sceneIndex = 0;
            phase = Phase.Replace;
            AssetDatabase.SaveAssets();
            return;
        }
        string ap = modelAssets[itemIndex];
        WriteStatus("RESTORE IMPORTER " + (itemIndex+1) + "/" + modelAssets.Count + "\n" + ap);
        var imp = AssetImporter.GetAtPath(ap) as ModelImporter;
        if (imp != null && originalReadable.TryGetValue(ap,out bool was) && imp.isReadable != was) {
            imp.isReadable = was;
            imp.SaveAndReimport();
        }
        itemIndex++;
    }

    static void StepReplace()
    {
        if (sceneIndex >= scenes.Length) {
            sceneIndex = 0;
            phase = Phase.Verify;
            return;
        }

        string sp = scenes[sceneIndex];
        WriteStatus("REPLACE " + (sceneIndex+1) + "/" + scenes.Length + "\n" + sp);
        var sc = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
        string before = TransformHash(sc);
        if (!preHash.TryGetValue(sp,out var expected) || before != expected) {
            errorLog.AppendLine("PRE-HASH MISMATCH " + sp + " expected=" + expected + " got=" + before);
            sceneLog.AppendLine(Q(sp)+",0,"+before+","+before+",False,0");
            sceneIndex++;
            return;
        }

        int replaced = 0;
        var lookup = targetList.Where(t=>t.optimizedMesh).ToDictionary(t=>t.key,t=>t.optimizedMesh);
        foreach (var root in sc.GetRootGameObjects()) {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
                var m=mf.sharedMesh;
                if (!m) continue;
                string key=AssetDatabase.GetAssetPath(m)+"|"+m.name;
                if (!lookup.TryGetValue(key,out var opt)) continue;
                mf.sharedMesh = opt;
                EditorUtility.SetDirty(mf);
                if (PrefabUtility.IsPartOfPrefabInstance(mf))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
                replaced++;
            }
        }

        string after = TransformHash(sc);
        bool match = before == after;
        long triAfter = SceneTriangles(sc);
        if (!match) {
            errorLog.AppendLine("POST-HASH MISMATCH " + sp + " before=" + before + " after=" + after);
            EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
            sceneLog.AppendLine(Q(sp)+","+replaced+","+before+","+after+",False,"+triAfter);
            sceneIndex++;
            return;
        }

        if (replaced > 0) EditorSceneManager.SaveScene(sc);
        sceneLog.AppendLine(Q(sp)+","+replaced+","+before+","+after+",True,"+triAfter);
        File.WriteAllText(OutDir + "/optimized_scene_results.csv", sceneLog.ToString());
        sceneIndex++;
    }

    static void StepVerify()
    {
        if (sceneIndex >= scenes.Length) {
            phase = Phase.Done;
            return;
        }
        string sp=scenes[sceneIndex];
        WriteStatus("VERIFY " + (sceneIndex+1) + "/" + scenes.Length + "\n" + sp);
        var sc=EditorSceneManager.OpenScene(sp,OpenSceneMode.Single);
        string h=TransformHash(sc);
        bool ok=preHash.TryGetValue(sp,out var pre) && h==pre;
        long tris=SceneTriangles(sc);
        File.AppendAllText(OutDir+"/post_verify.csv",
            (sceneIndex==0 ? "scene,transform_hash,hash_match,instance_triangles\n" : "") +
            Q(sp)+","+h+","+ok+","+tris+"\n");
        if(!ok) errorLog.AppendLine("VERIFY HASH MISMATCH "+sp+" pre="+pre+" post="+h);
        sceneIndex++;
    }

    static void Finish()
    {
        File.WriteAllText(OutDir + "/optimized_mesh_mapping.csv", mapLog.ToString());
        File.WriteAllText(OutDir + "/optimized_scene_results.csv", sceneLog.ToString());
        File.WriteAllText(OutDir + "/optimizer_errors.txt", errorLog.ToString());
        RestoreSorpigal();
        WriteStatus("DONE\ntargets=" + targetList.Count +
            "\nerrors=" + (errorLog.Length==0 ? 0 : 1));
        EditorApplication.update -= Step;
        phase = Phase.Idle;
    }

    static void RestoreSorpigal()
    {
        try { EditorSceneManager.OpenScene(RestoreScene,OpenSceneMode.Single); }
        catch(Exception ex) { errorLog.AppendLine("RESTORE SORPIGAL FAILED "+ex); }
    }
    static float QualityFor(long tri)
    {
        if (tri >= 1000000) return 0.05f;
        if (tri >= 250000) return 0.10f;
        if (tri >= 100000) return 0.20f;
        return 0.35f;
    }

    static bool IsVegetation(string path,string meshName)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string s=(path+"|"+meshName).ToLowerInvariant();
        string[] words={"tree","shrub","bush","plant","pine","cactus","fern","grass",
            "vegetation","sapling","rooibos","quiver","searsia","jacaranda"};
        return words.Any(w=>s.Contains(w));
    }

    static long TriangleCount(Mesh m)
    {
        long n=0;
        for(int s=0;s<m.subMeshCount;s++) n+=(long)m.GetIndexCount(s)/3;
        return n;
    }

    static long SceneTriangles(Scene sc)
    {
        long n=0;
        foreach(var root in sc.GetRootGameObjects())
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if(mf.sharedMesh) n+=TriangleCount(mf.sharedMesh);
        return n;
    }
    static string TransformHash(Scene sc)
    {
        var sb=new StringBuilder(1024*128);
        foreach(var root in sc.GetRootGameObjects().OrderBy(x=>x.transform.GetSiblingIndex())) {
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
                sb.Append(HierarchyKey(t)).Append('|');
                AppendV(sb,t.localPosition); sb.Append('|');
                var q=t.localRotation;
                sb.Append(F(q.x)).Append(',').Append(F(q.y)).Append(',').Append(F(q.z)).Append(',').Append(F(q.w)).Append('|');
                AppendV(sb,t.localScale); sb.Append('|');
                sb.Append(t.gameObject.activeSelf?'1':'0').Append(';');
            }
        }
        using(var sha=SHA256.Create()) {
            var b=Encoding.UTF8.GetBytes(sb.ToString());
            return BitConverter.ToString(sha.ComputeHash(b)).Replace("-","");
        }
    }

    static string HierarchyKey(Transform t)
    {
        var p=new List<string>();
        while(t!=null) { p.Add(t.GetSiblingIndex()+":"+t.name); t=t.parent; }
        p.Reverse();
        return string.Join("/",p);
    }

    static void AppendV(StringBuilder sb,Vector3 v)
    {
        sb.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z));
    }
    static string F(float v) => v.ToString("R",CultureInfo.InvariantCulture);

    static string SafeName(string s)
    {
        foreach(char c in Path.GetInvalidFileNameChars()) s=s.Replace(c,'_');
        return s.Replace(' ','_').Replace('/','_').Replace('\\','_');
    }

    static string Q(string s)
    {
        return "\"" + (s ?? "").Replace("\"","\"\"") + "\"";
    }

    static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');
        string name=Path.GetFileName(path);
        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent,name);
    }

    static void WriteStatus(string s)
    {
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(OutDir+"/GLOBAL_OPTIMIZER_STATUS.txt",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"\n"+s);
    }
}

