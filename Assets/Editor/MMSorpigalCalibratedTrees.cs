using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

// Sorpigal vegetation only. No import hooks and no calls to legacy pipelines.
public static class MMSorpigalCalibratedTrees
{
    const string ScenePath="Assets/Scenes/NewSorpigal_OpenWorld.unity";
    const string MatDir="Assets/Environment/ValidatedTrees/Materials";
    [Serializable] public class Entry { public string id,path,modelSHA256,importerSHA256,evidence; public float rotationX; public bool visuallyReviewed; }
    [Serializable] public class Catalogue { public Entry[] entries; }
    static string Digest(string p) { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant(); }
    static uint Seed(string s) { unchecked { uint h=2166136261; foreach(char c in s) h=(h^c)*16777619; return h; } }
    static string Key(Transform t)=>GlobalObjectId.GetGlobalObjectIdSlow(t).ToString();
    static Transform[] All(Scene sc)=>sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    static Dictionary<string,Vector2> Snapshot(Scene sc, HashSet<Transform> wrappers)
    {
        return All(sc).Where(t=>!Ancestors(t.parent).Any(wrappers.Contains)).ToDictionary(t=>wrappers.Contains(t)?"anchor:"+t.name:Key(t),t=>new Vector2(t.position.x,t.position.z));
    }
    static IEnumerable<Transform> Ancestors(Transform t) { for(;t;t=t.parent)yield return t; }
    static void Verify(Dictionary<string,Vector2> before,Dictionary<string,Vector2> after)
    {
        if(before.Count!=after.Count || before.Any(k=>!after.ContainsKey(k.Key)||!k.Value.x.Equals(after[k.Key].x)||!k.Value.y.Equals(after[k.Key].y))) throw new Exception("Immutable object X/Z or identity changed. Scene not accepted.");
    }
    static Material MaterialFor(Entry e,string original)
    {
        string lower=original.ToLowerInvariant(); bool leaf=lower.Contains("leav")||lower.Contains("twig")||e.id=="searsia_lucida";
        string baseDir=System.IO.Path.GetDirectoryName(e.path).Replace('\\','/')+"/textures/";
        string diff,normal;
        if(e.id=="searsia_lucida") {diff=baseDir+e.id+"_rgba_1k.png";normal=baseDir+e.id+"_nor_gl_1k.exr";}
        else if(e.id=="fir_sapling") {diff=baseDir+e.id+(leaf?"_twigs_rgba_1k.png":"_branches_diff_1k.png");normal=baseDir+e.id+(leaf?"_twigs_nor_gl_1k.png":"_branches_nor_gl_1k.png");}
        else if(leaf) {diff="Assets/Materials/RealisticWorld/Sorpigal/Textures/"+e.id+"_leaves_rgba.png";normal=baseDir+e.id+"_leaves_nor_gl_1k.png";}
        else if(lower.Contains("branches")) {diff=baseDir+e.id+"_branches_diff_1k.png";normal=baseDir+e.id+"_branches_nor_gl_1k.png";}
        else if(e.id=="jacaranda_tree") {diff=baseDir+e.id+"_trunk_diff_1k.png";normal=baseDir+e.id+"_trunk_nor_gl_1k.png";}
        else {diff=baseDir+e.id+"_diff_1k.jpg";normal=baseDir+e.id+"_nor_gl_1k.exr";}
        string path=MatDir+"/"+original+".mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing)return existing;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(diff);if(!texture)throw new Exception("Missing albedo "+diff);
        var m=new Material(Shader.Find("Standard"));m.name=original;m.mainTexture=texture;m.color=Color.white;
        m.SetFloat("_Glossiness",.03f);m.SetFloat("_Metallic",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.SetFloat("_SpecularHighlights",0);
        if(leaf){m.SetFloat("_Mode",1);m.SetFloat("_Cutoff",.34f);m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;}
        if(File.Exists(normal)) {
            // Copy normals so fixing import settings cannot alter other regions.
            string dest=MatDir+"/"+System.IO.Path.GetFileName(normal);
            if(!File.Exists(dest))File.Copy(normal,dest);
            AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(dest); importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;importer.SaveAndReimport();
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dest));m.SetFloat("_BumpScale",.55f);m.EnableKeyword("_NORMALMAP");
        }
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static void CheckMaterials(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy)) {
            if(r.sharedMaterials.Length==0)throw new Exception("No material slots");
            foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported||!m.mainTexture)throw new Exception("Invalid material: "+r.name);
        }
    }
    [MenuItem("MMUnity/Sequential/3 Repair Sorpigal trees only _F11")]
    public static void Run()=>Repair(0);
    [MenuItem("MMUnity/Sequential/5 Repair preserved Sorpigal vegetation _F1")]
    public static void RepairPreserved()=>Repair(1);
    [MenuItem("MMUnity/Sequential/6 Repair legacy source tree anchors _F4")]
    public static void RepairLegacy()=>Repair(2);
    static bool IsPreserved(Transform t)=>t.parent&&t.parent.name=="Green Trees and Bushes"&&(t.name.StartsWith("GreenTree_")||t.name=="GreenShrub_28");
    static bool OnVolcanicIsland(Transform t)=>IsPreserved(t)&&(t.name=="GreenTree_23"||t.name=="GreenTree_30"||t.name=="GreenTree_31"||t.name=="GreenShrub_28");
    static void Repair(int mode)
    {
        if(File.Exists(MMSequentialEvidence.Output+"/trees_mode_"+mode+".frozen"))throw new Exception("This vegetation pass is frozen. Review its evidence before making a new version; do not rerun it.");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit play mode");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene; refusing discard");
        if(!File.Exists(MMSequentialEvidence.Output+"/baseline.complete"))throw new Exception("Missing baseline");
        var entries=JsonUtility.FromJson<Catalogue>(File.ReadAllText("Assets/Environment/ValidatedTrees/Calibration.json")).entries;
        foreach(var e in entries)if(!e.visuallyReviewed||!File.Exists(e.evidence)||Digest(e.path)!=e.modelSHA256||Digest(e.path+".meta")!=e.importerSHA256)throw new Exception("Calibration stale: "+e.id);
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var terrain=All(sc).Select(t=>t.GetComponent<Terrain>()).First(t=>t);
        var container=All(sc).First(t=>t.name=="Trees" && Ancestors(t).Any(a=>a.name=="Vegetation - MM Anchors + Natural Groves"));
        var wrappers=mode==1?All(sc).Where(IsPreserved).ToArray():container.Cast<Transform>().Where(t=>mode==2?t.name.StartsWith("tree06a_"):(t.name.StartsWith("6tree")||t.name.StartsWith("GroveTree_"))).ToArray();
        if(wrappers.Length==0||wrappers.Any(t=>t.GetComponent<Renderer>()))throw new Exception("Unexpected tree wrapper structure");
        var wrapperSet=new HashSet<Transform>(wrappers);var fixedBefore=Snapshot(sc,wrapperSet);
        string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");string output=MMSequentialEvidence.Output+"/trees_"+stamp;
        string backup="Backups/SorpigalTrees_"+stamp;Directory.CreateDirectory(backup);Directory.CreateDirectory(output);Directory.CreateDirectory(MatDir);AssetDatabase.Refresh();
        File.Copy(ScenePath,backup+"/NewSorpigal_OpenWorld.unity");
        if(Directory.Exists(MatDir))foreach(var f in Directory.GetFiles(MatDir))File.Copy(f,backup+"/"+System.IO.Path.GetFileName(f));
        File.WriteAllLines(output+"/fixed_before.csv",new[]{"id,x,z"}.Concat(fixedBefore.Select(k=>$"{k.Key},{k.Value.x:R},{k.Value.y:R}")));
        var rows=new List<string>{"anchor,species,rotation_x,ground_gap,source,x,z"};
        try {
            foreach(var wrap in wrappers) {
                var p=wrap.position;var seed=Seed(wrap.name);
                if(PrefabUtility.IsPartOfPrefabInstance(wrap.gameObject))PrefabUtility.UnpackPrefabInstance(wrap.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                // Broadleaf-dominant mixture; saplings occur occasionally.
                int[] pool={2,3,5,2,3,5,4,0,1};var e=entries[pool[seed%(uint)pool.Length]];
                bool dead=OnVolcanicIsland(wrap); if(dead)e=entries[4];
                foreach(Transform child in wrap.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                wrap.rotation=Quaternion.Euler(0,seed%36000/100f,0);wrap.localScale=Vector3.one;
                var src=AssetDatabase.LoadAssetAtPath<GameObject>(e.path);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(src,sc);visual.transform.SetParent(wrap,false);
                visual.name="Calibrated_"+e.id;visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.Euler(e.rotationX,0,0);visual.transform.localScale=Vector3.one;
                var renderers=visual.GetComponentsInChildren<Renderer>(true);
                Transform plantPivot=null;
                if(e.id=="searsia_lucida"||e.id=="fir_sapling") {
                    var chosen=renderers.OrderBy(r=>r.name).ElementAt((int)(seed%(uint)renderers.Length));
                    plantPivot=chosen.transform;
                    foreach(var r in renderers)r.enabled=r==chosen;
                    var offset=chosen.transform.position-wrap.position;visual.transform.position-=new Vector3(offset.x,0,offset.z);
                }
                foreach(var r in renderers) {
                    var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++) {if(!slots[i])throw new Exception("Missing source slot");slots[i]=MaterialFor(e,slots[i].name);}r.sharedMaterials=slots;
                    if(dead) {
                        var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)throw new Exception("Expected static dead-tree mesh");
                        string meshPath=MatDir+"/"+e.id+"_dead.asset";
                        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(!mesh) {mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=e.id+"_dead";
                            for(int i=0;i<slots.Length;i++)if(slots[i].name.ToLowerInvariant().Contains("leav"))mesh.SetTriangles(new int[0],i);
                            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
                        mf.sharedMesh=mesh;
                    }
                }
                float size=seed%1000/999f;
                float h=e.id=="searsia_lucida"?Mathf.Lerp(2.3f,4.2f,size):e.id=="fir_sapling"?Mathf.Lerp(2.8f,5f,size):e.id=="jacaranda_tree"?Mathf.Lerp(8f,12f,size):Mathf.Lerp(5f,8.5f,size);
                if(dead)h=wrap.name.StartsWith("GreenShrub")?1.3f:Mathf.Lerp(2f,3.5f,size);
                var b=MMSequentialEvidence.BoundsOf(visual);visual.transform.localScale*=h/b.size.y;
                if(plantPivot) { var offset=plantPivot.position-wrap.position;visual.transform.position-=new Vector3(offset.x,0,offset.z); }
                b=MMSequentialEvidence.BoundsOf(visual);float ground=terrain.SampleHeight(p)+terrain.transform.position.y;
                wrap.position=new Vector3(p.x,wrap.position.y+ground-b.min.y,p.z);
                float gap=MMSequentialEvidence.BoundsOf(visual).min.y-ground;
                CheckMaterials(visual);
                if(Mathf.Abs(gap)>.02f||Quaternion.Angle(visual.transform.localRotation,Quaternion.Euler(e.rotationX,0,0))>.01f)throw new Exception("Calibrated tree validation failed");
                rows.Add($"{wrap.name},{e.id},{e.rotationX},{gap:R},{wrap.name.StartsWith("6tree")||wrap.name.StartsWith("tree06a_")},{p.x:R},{p.z:R}");
            }
            Verify(fixedBefore,Snapshot(sc,wrapperSet));AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,ScenePath);
            sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var savedWrappers=new HashSet<Transform>(All(sc).Where(t=>mode==1?IsPreserved(t):(t.parent&&t.parent.name=="Trees"&&(mode==2?t.name.StartsWith("tree06a_"):(t.name.StartsWith("6tree")||t.name.StartsWith("GroveTree_"))))));
            Verify(fixedBefore,Snapshot(sc,savedWrappers));
            File.WriteAllLines(output+"/trees.csv",rows);
            File.WriteAllText(output+"/numeric_summary.json",$"{{\"fixed_objects\":{fixedBefore.Count},\"missing\":0,\"extra\":0,\"xz_changed\":0,\"trees\":{savedWrappers.Count},\"source_anchors\":{savedWrappers.Count(t=>t.name.StartsWith("6tree")||t.name.StartsWith("tree06a_"))},\"visual_status\":\"pending\"}}");
            MMSequentialEvidence.CaptureSorpigal(sc,"trees_"+stamp);
            File.WriteAllText(MMSequentialEvidence.Output+"/latest_trees.txt",output);
            File.WriteAllText(MMSequentialEvidence.Output+"/trees_mode_"+mode+".frozen",output);
        } catch { EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);throw; }
    }
}
