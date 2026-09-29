using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

[InitializeOnLoad]
public static class MMGlobalPerformanceAudit
{
    const string OutDir = "C:/MMUnityPort/Validation/Optimization";
    const string Trigger = "C:/MMUnityPort/Temp/RUN_GLOBAL_PERF_AUDIT.trigger";

    static MMGlobalPerformanceAudit()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        if (File.Exists(Trigger)) {
            File.Delete(Trigger);
            StartAudit();
        }
    }
    const string RestoreScene = "Assets/Scenes/Regions/NewSorpigal.unity";
    static string[] scenes;
    static int index;
    static StringBuilder sceneCsv;
    static Dictionary<string, MeshRec> meshes;
    static Dictionary<string, MatRec> mats;

    class MeshRec {
        public string asset, name;
        public long triEach, effective;
        public int verts, uses;
        public HashSet<string> scenes = new HashSet<string>();
    }
    class MatRec {
        public string asset, name, shader;
        public int uses;
        public bool instancing;
        public HashSet<string> scenes = new HashSet<string>();
    }

    [MenuItem("MM6/Optimization/Audit All Live Scenes")]
    public static void StartAudit()
    {
        Directory.CreateDirectory(OutDir);
        scenes = AssetDatabase.FindAssets("t:Scene", new[]{"Assets/Scenes"})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.Contains("/RestoreBackups/"))
            .Where(p => !Path.GetFileName(p).StartsWith("_Recovery_"))
            .OrderBy(p => p).ToArray();
        index = 0;
        meshes = new Dictionary<string, MeshRec>();
        mats = new Dictionary<string, MatRec>();
        sceneCsv = new StringBuilder();
        sceneCsv.AppendLine("scene,gameobjects,renderers,meshfilters,unique_meshes,instance_triangles,unique_materials,instancing_slots,shadow_renderers");
        File.WriteAllText(OutDir + "/GLOBAL_AUDIT_STATUS.txt",
            "RUNNING 0/" + scenes.Length);
        EditorApplication.update -= Step;
        EditorApplication.update += Step;
    }
    static void Step()
    {
        if (scenes == null) return;
        if (index >= scenes.Length) {
            Finish();
            return;
        }
        string sp = scenes[index];
        try {
            var sc = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
            AuditScene(sc, sp);
            index++;
            File.WriteAllText(OutDir + "/GLOBAL_AUDIT_STATUS.txt",
                "RUNNING " + index + "/" + scenes.Length + "\n" + sp);
        } catch (Exception ex) {
            File.AppendAllText(OutDir + "/GLOBAL_AUDIT_ERRORS.txt",
                sp + "\n" + ex + "\n\n");
            index++;
        }
    }

    static void AuditScene(Scene sc, string sp)
    {
        int go=0, rend=0, mfCount=0, shadows=0, instSlots=0;
        long sceneTris=0;
        var sm = new HashSet<string>();
        var sceneMats = new HashSet<string>();

        foreach (var root in sc.GetRootGameObjects()) {
            go += root.GetComponentsInChildren<Transform>(true).Length;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) {
                rend++;
                if (r.shadowCastingMode != ShadowCastingMode.Off) shadows++;
                foreach (var m in r.sharedMaterials) {
                    if (!m) continue;
                    string ap = AssetDatabase.GetAssetPath(m);
                    string key = ap + "|" + m.name;
                    sceneMats.Add(key);
                    if (m.enableInstancing) instSlots++;
                    if (!mats.TryGetValue(key, out var mr)) {
                        mr = new MatRec {
                            asset=ap, name=m.name,
                            shader=m.shader ? m.shader.name : "<null>",
                            instancing=m.enableInstancing
                        };
                        mats[key]=mr;
                    }
                    mr.uses++;
                    mr.scenes.Add(sp);
                }
            }

            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
                mfCount++;
                var m = mf.sharedMesh;
                if (!m) continue;
                string ap = AssetDatabase.GetAssetPath(m);
                string key = ap + "|" + m.name;
                sm.Add(key);
                long mt=0;
                for (int s=0; s<m.subMeshCount; s++)
                    mt += (long)m.GetIndexCount(s)/3;
                sceneTris += mt;
                if (!meshes.TryGetValue(key, out var rec)) {
                    rec = new MeshRec {
                        asset=ap, name=m.name, triEach=mt,
                        verts=m.vertexCount
                    };
                    meshes[key]=rec;
                }
                rec.uses++;
                rec.effective += mt;
                rec.scenes.Add(sp);
            }
        }

        sceneCsv.AppendLine(string.Join(",", new[]{
            Q(sp), go.ToString(), rend.ToString(), mfCount.ToString(),
            sm.Count.ToString(), sceneTris.ToString(),
            sceneMats.Count.ToString(), instSlots.ToString(),
            shadows.ToString()
        }));
    }

    static string Q(string s)
    {
        return "\"" + (s ?? "").Replace("\"","\"\"") + "\"";
    }
    static void Finish()
    {
        EditorApplication.update -= Step;
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(OutDir + "/global_scene_perf.csv",
            sceneCsv.ToString());

        var msb = new StringBuilder();
        msb.AppendLine("effective_triangles,uses,triangles_each,vertices_each,asset,mesh_name,scene_count");
        foreach (var r in meshes.Values.OrderByDescending(x=>x.effective)) {
            msb.AppendLine(string.Join(",", new[]{
                r.effective.ToString(), r.uses.ToString(),
                r.triEach.ToString(), r.verts.ToString(),
                Q(r.asset), Q(r.name), r.scenes.Count.ToString()
            }));
        }
        File.WriteAllText(OutDir + "/global_mesh_cost.csv", msb.ToString());

        var matb = new StringBuilder();
        matb.AppendLine("uses,instancing,shader,asset,material,scene_count");
        foreach (var r in mats.Values.OrderByDescending(x=>x.uses)) {
            matb.AppendLine(string.Join(",", new[]{
                r.uses.ToString(), r.instancing.ToString(),
                Q(r.shader), Q(r.asset), Q(r.name),
                r.scenes.Count.ToString()
            }));
        }
        File.WriteAllText(OutDir + "/global_material_use.csv",
            matb.ToString());

        File.WriteAllText(OutDir + "/GLOBAL_AUDIT_STATUS.txt",
            "DONE " + scenes.Length + "/" + scenes.Length +
            "\nmeshes=" + meshes.Count +
            "\nmaterials=" + mats.Count);

        try {
            EditorSceneManager.OpenScene(RestoreScene, OpenSceneMode.Single);
        } catch (Exception ex) {
            File.AppendAllText(OutDir + "/GLOBAL_AUDIT_ERRORS.txt",
                "RESTORE FAILED\n" + ex + "\n");
        }

        scenes = null;
        meshes = null;
        mats = null;
    }
}
