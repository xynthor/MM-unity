using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Forest is placed only where the supplied Enroth map identifies woodland.
// The canonical Paradise Valley scene and TerrainData are read-only.
public static class MMParadiseReferenceCanopy20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string ScenePath="Assets/Scenes/ParadiseValley_West.unity";
 const string Group="Map-Traced Western Paradise Forest 20260925";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeParadiseWestReferenceCanopy_20260925\manifest.json";
 static Color32[] Read(string p){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)
   throw new Exception("Missing or malformed source-map mask "+p);
  var px=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return px;
 }
 static Bounds Bounds(GameObject g){
  var rs=g.GetComponentsInChildren<Renderer>(true);
  if(rs.Length==0)throw new Exception("Source tree has no renderer");
  var b=rs[0].bounds;
  foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
 }
 static bool Far(Vector2 point,List<Vector2> points,float radius){
  float d=radius*radius;
  foreach(var v in points)if((point-v).sqrMagnitude<d)return false;
  return true;
 }
 [MenuItem("MMUnity/Reference 2026/Trace Paradise West Forest From Source Map")]
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Verified backup manifest is required");
  var active=SceneManager.GetActiveScene();
  if(active.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||active.isDirty)
   throw new Exception("Saved linked world must be active");
  var forest=Read(V+"ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var land=Read(V+"ReferenceMasks/ParadiseValley_West.png");
  var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
  try{
   var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
   if(!root)throw new Exception("Paradise generated root absent");
   var t=root.GetComponentInChildren<Terrain>(true);
   if(!t)throw new Exception("Paradise generated terrain absent");
   if(root.transform.Find(Group))throw new Exception("Source canopy already installed; no duplicate pass");
   var existing=root.transform.Find("Region Dressing - Reference Matched");
   if(!existing)throw new Exception("Generated source dressing not found");
   var prototypes=existing.Cast<Transform>().Where(x=>x.name.StartsWith("Searsia_",StringComparison.Ordinal)).ToArray();
   if(prototypes.Length<100)throw new Exception("Source woodland tree models missing");
   var used=existing.Cast<Transform>().Select(v=>new Vector2(v.position.x,v.position.z)).ToList();
   var group=new GameObject(Group);group.transform.SetParent(root.transform,false);
   var rng=new System.Random(20260925);
   int placed=0,attempted=0,rejectedMask=0,rejectedSlope=0,rejectedSpace=0;
   const int target=350;
   var points=new List<Vector2>();
   for(int z=10;z<503;z+=7)for(int x=10;x<503;x+=7)
    points.Add(new Vector2(x+(float)(rng.NextDouble()*4-2),
      z+(float)(rng.NextDouble()*4-2)));
   for(int i=points.Count-1;i>0;i--){
    int j=rng.Next(i+1);var v=points[i];points[i]=points[j];points[j]=v;
   }
   foreach(var pt in points){
    if(placed>=target)break;attempted++;
    int x=Mathf.Clamp(Mathf.RoundToInt(pt.x),0,512);
    int z=Mathf.Clamp(Mathf.RoundToInt(pt.y),0,512);
    var color=forest[z*513+x];var ground=land[z*513+x];
    if(ground.r<145||color.g<110){rejectedMask++;continue;}
    float slope=t.terrainData.GetSteepness(x/512f,z/512f);
    float wx=t.transform.position.x+pt.x,wz=t.transform.position.z+pt.y;
    float height=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;
    if(slope>25f||height<1.5f||height>80f){rejectedSlope++;continue;}
    var loc=new Vector2(wx,wz);
    if(!Far(loc,used,6.0f)){rejectedSpace++;continue;}
    var original=prototypes[rng.Next(prototypes.Length)];
    var go=UnityEngine.Object.Instantiate(original.gameObject,group.transform);
    go.name="SourceMap_Searsia_"+placed;
    go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
    go.transform.localScale*=.84f+(float)rng.NextDouble()*.31f;
    go.transform.position=new Vector3(wx,height,wz);
    var b=Bounds(go);
    if(Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))>16f){
     UnityEngine.Object.DestroyImmediate(go);continue;
    }
    go.transform.position+=new Vector3(0,height-b.min.y-.04f,0);
    used.Add(loc);placed++;
   }
   if(placed<200)throw new Exception("Not enough valid source forest sites "+placed+
     "; refuse incomplete reference canopy");
   int badMaterials=group.GetComponentsInChildren<Renderer>(true).Count(r=>
     r.sharedMaterials==null||r.sharedMaterials.Length==0||
     r.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported));
   if(badMaterials!=0)throw new Exception("Invalid generated canopy materials: "+badMaterials);
   EditorSceneManager.MarkSceneDirty(sc);
   if(!EditorSceneManager.SaveScene(sc,ScenePath))
    throw new IOException("Failed to save Paradise west standalone");
   File.WriteAllText(V+"paradise_map_traced_forest_20260925.txt",
    "reference_candidate=ParadiseValley_West_ReferenceBiomes.png\n"+
    "new_source_model_trees="+placed+" existing_source_trees="+prototypes.Length+
    " sampled_sites="+attempted+" rejected_nonforest_or_water="+rejectedMask+
    " rejected_steep_or_elevation="+rejectedSlope+
    " rejected_spacing="+rejectedSpace+" invalid_materials="+badMaterials+
    " canonical_source_edits=0\n");
   Debug.Log("PARADISE_MAP_TRACED_FOREST_SAVED count="+placed);
  }finally {
   EditorSceneManager.CloseScene(sc,true);
   EditorSceneManager.SetActiveScene(active);
  }
 }
}
[InitializeOnLoad]
internal static class MMParadiseCanopyQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMParadiseCanopyQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_PARADISE_MAP_CANOPY.flag"))return;
  File.Delete(V+"RUN_PARADISE_MAP_CANOPY.flag");
  try {
   MMParadiseReferenceCanopy20260925.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"paradise_canopy_linked_runtime.txt","PASS source-map canopy saved; linked scene rebuilt\n");
  }catch(Exception ex) {
   File.WriteAllText(V+"paradise_canopy_linked_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
