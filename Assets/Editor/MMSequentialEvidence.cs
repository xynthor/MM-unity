using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

// Explicit commands only. This class never runs on import or editor reload.
public static class MMSequentialEvidence
{
    public const string Output = "Validation/SequentialRepair";
    static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);
    static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    public static Bounds BoundsOf(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        if (rs.Length == 0) throw new Exception("No visible geometry: " + g.name);
        var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    public static void Render(Camera cam, string path, int width = 1200, int height = 900)
    {
        var previous = RenderTexture.active;
        var rt = new RenderTexture(width, height, 24);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        try {
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
        } finally { cam.targetTexture = null; RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); }
    }
    static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit play mode first");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: evidence capture will not discard it");
    }
    [MenuItem("MMUnity/Sequential/1 Capture immutable baseline _F3")]
    public static void Baseline()
    {
        Guard(); Directory.CreateDirectory(Output);
        if (File.Exists(Output + "/baseline.complete")) throw new Exception("Baseline already frozen; refusing overwrite");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var rows = new List<string>{"scene,object_id,path,x,y,z,rotation_x,rotation_y,rotation_z,scale_x,scale_y,scale_z"};
        try {
            foreach (var path in Directory.GetFiles("Assets/Scenes", "*.unity").Where(p => p.Contains("SourceGrid") || p.EndsWith("NewSorpigal_OpenWorld.unity") || p.EndsWith("Enroth_Linked_OpenWorld.unity"))) {
                var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var t in sc.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))) {
                    var p = t.position; var e = t.eulerAngles; var s = t.lossyScale;
                    rows.Add(string.Join(",", Q(path), Q(GlobalObjectId.GetGlobalObjectIdSlow(t).ToString()), Q(PathOf(t)), F(p.x), F(p.y), F(p.z), F(e.x), F(e.y), F(e.z), F(s.x), F(s.y), F(s.z)));
                }
                if (path.EndsWith("NewSorpigal_OpenWorld.unity")) CaptureSorpigal(sc, "before");
            }
            File.WriteAllLines(Output + "/all_transforms_baseline.csv", rows);
            File.WriteAllText(Output + "/baseline.complete", DateTime.UtcNow.ToString("O"));
        } finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    public static void CaptureSorpigal(Scene sc, string stage)
    {
        var t = sc.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).First();
        var cameraGO = new GameObject("__SequentialEvidenceCamera"); SceneManager.MoveGameObjectToScene(cameraGO, sc);
        var cam = cameraGO.AddComponent<Camera>(); cam.farClipPlane = 2500; cam.nearClipPlane = .1f;
        var center = t.transform.position + t.terrainData.size * .5f;
        center.y = t.SampleHeight(center) + t.transform.position.y;
        try {
            cam.orthographic = true; cam.orthographicSize = Mathf.Max(t.terrainData.size.x, t.terrainData.size.z) * .55f;
            cam.transform.position = center + Vector3.up * 800; cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            Render(cam, Output + "/" + stage + "/top.png");
            cam.orthographic = false; cam.fieldOfView = 55;
            cam.transform.position = center + new Vector3(350, 350, -380); cam.transform.LookAt(center);
            Render(cam, Output + "/" + stage + "/oblique.png");
            var points=new[]{new Vector2(-75,-60),new Vector2(50,-30),new Vector2(-20,-125),new Vector2(-110,125)};
            var labels=new[]{"settlement","shoreline","forest","volcanic_boundary"};
            for(int i=0;i<points.Length;i++) {
                var target=new Vector3(points[i].x,0,points[i].y);target.y=t.SampleHeight(target)+t.transform.position.y+2;
                cam.transform.position=target+new Vector3(28,22,-32);cam.transform.LookAt(target);
                Render(cam,Output+"/"+stage+"/"+labels[i]+".png");
            }
            File.WriteAllText(Output + "/" + stage + "/terrain.txt", $"position={t.transform.position} size={t.terrainData.size} center={center}");
            var details=new List<string>{"height_resolution="+t.terrainData.heightmapResolution,"alpha_resolution="+t.terrainData.alphamapResolution,"terrain_asset="+AssetDatabase.GetAssetPath(t.terrainData)};
            foreach(var l in t.terrainData.terrainLayers)details.Add($"LAYER {l.name} path={AssetDatabase.GetAssetPath(l)} diffuse={AssetDatabase.GetAssetPath(l.diffuseTexture)} normal={AssetDatabase.GetAssetPath(l.normalMapTexture)} tiling={l.tileSize}");
            foreach(var r in sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>PathOf(r.transform).ToLowerInvariant().Contains("water")||PathOf(r.transform).ToLowerInvariant().Contains("shore")||PathOf(r.transform).ToLowerInvariant().Contains("ocean")||PathOf(r.transform).ToLowerInvariant().Contains("seabed")))
                details.Add($"RENDERER {PathOf(r.transform)} enabled={r.enabled} active={r.gameObject.activeInHierarchy} bounds={r.bounds} mesh={AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>() ? r.GetComponent<MeshFilter>().sharedMesh : null)} material={string.Join(";",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)))}");
            File.WriteAllLines(Output+"/"+stage+"/geometry.txt",details);
        } finally { UnityEngine.Object.DestroyImmediate(cameraGO); }
    }
    [MenuItem("MMUnity/Sequential/4 Capture current Sorpigal views _F12")]
    public static void CaptureCurrent()
    {
        Guard();var sc=SceneManager.GetActiveScene();
        if(sc.path!="Assets/Scenes/NewSorpigal_OpenWorld.unity")throw new Exception("Open Sorpigal first");
        CaptureSorpigal(sc,"review_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
    }
    public static readonly string[] Ids = {"searsia_lucida", "fir_sapling", "island_tree_01", "island_tree_02", "island_tree_03", "jacaranda_tree"};
    public static string AssetPath(string id) => "Assets/Environment/PolyHaven/" + ((id == "searsia_lucida" || id == "fir_sapling") ? "Models/" : "Downloaded/") + id + "/" + id + "_1k.fbx";
    [MenuItem("MMUnity/Sequential/2 Render orientation candidates _F10")]
    public static void Calibration()
    {
        Guard(); Directory.CreateDirectory(Output + "/calibration");
        var active = SceneManager.GetActiveScene();
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(sc);
        var materials = new List<Material>();
        try {
            var light = new GameObject("Calibration light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(35, -30, 0);
            light.cullingMask = 1 << 31;
            var cam = new GameObject("Calibration camera").AddComponent<Camera>(); cam.scene = sc; cam.cullingMask = 1 << 31;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.28f,.32f,.36f); cam.orthographic = true; cam.farClipPlane = 2000;
            var rotations = new[]{Vector3.zero,new Vector3(90,0,0),new Vector3(-90,0,0),new Vector3(0,0,90),new Vector3(0,0,-90)};
            var names = new[]{"identity","plus90X","minus90X","plus90Z","minus90Z"};
            var report = new List<string>{"asset,renderer,slot,original_material,assigned_material,texture"};
            foreach (var id in Ids) {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(id)); if (!src) throw new Exception("Missing " + id);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src, sc);
                foreach(var child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) {
                    var slots = r.sharedMaterials;
                    for (int i=0; i<slots.Length; i++) {
                        var original = slots[i] ? slots[i].name : "NULL";
                        var lower = original.ToLowerInvariant();
                        bool leaf = lower.Contains("leav") || lower.Contains("twig") || id == "searsia_lucida";
                        string texturePath;
                        if (id == "searsia_lucida") texturePath = "Assets/Environment/PolyHaven/Models/"+id+"/textures/"+id+"_rgba_1k.png";
                        else if (id == "fir_sapling") texturePath = "Assets/Environment/PolyHaven/Models/"+id+"/textures/"+id+(leaf?"_twigs_rgba_1k.png":"_branches_diff_1k.png");
                        else if (leaf) texturePath = "Assets/Materials/RealisticWorld/Sorpigal/Textures/"+id+"_leaves_rgba.png";
                        else texturePath = "Assets/Environment/PolyHaven/Downloaded/"+id+"/textures/"+id+(lower.Contains("trunk")?"_trunk_diff_1k.png":"_branches_diff_1k.png");
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                        if (!texture) throw new Exception("Missing calibration texture "+texturePath);
                        var mat = new Material(Shader.Find("Standard")); materials.Add(mat);
                        mat.mainTexture=texture; mat.color=Color.white; mat.SetFloat("_Glossiness",.04f); mat.SetFloat("_Metallic",0);
                        if(leaf) { mat.SetFloat("_Mode",1); mat.SetFloat("_Cutoff",.34f); mat.EnableKeyword("_ALPHATEST_ON"); mat.SetOverrideTag("RenderType","TransparentCutout"); mat.renderQueue=2450; }
                        slots[i]=mat;
                        report.Add(string.Join(",",id,Q(r.name),i,Q(original),Q(mat.name),Q(texturePath)));
                    }
                    r.sharedMaterials = slots;
                }
                for (int i=0;i<rotations.Length;i++) {
                    go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.Euler(rotations[i]);
                    var b = BoundsOf(go); go.transform.position -= Vector3.up*b.min.y; b=BoundsOf(go);
                    cam.orthographicSize = Mathf.Max(b.size.y,b.size.x,b.size.z)*.68f;
                    cam.transform.position = b.center + new Vector3(0,.12f,-1)*Mathf.Max(40,b.size.magnitude*2); cam.transform.LookAt(b.center);
                    Render(cam,Output+"/calibration/"+id+"_"+names[i]+".png",600,600);
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllLines(Output+"/calibration/material_slots.csv",report);
            File.WriteAllText(Output+"/calibration/complete.txt",DateTime.UtcNow.ToString("O"));
        } finally {
            foreach(var m in materials) UnityEngine.Object.DestroyImmediate(m);
            EditorSceneManager.CloseScene(sc,true); SceneManager.SetActiveScene(active);
        }
    }
}
