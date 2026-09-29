using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWorldExtensionVegetationTransition {
 enum Side { North, West, South }
 sealed class V {
  public string target,source; public Side side;
  public V(string t,string s,Side d){target=t;source=s;side=d;}
 }
 static readonly V[] Vals={
  new V("SweetWater_North","SweetWater_SourceGrid",Side.North),
  new V("Kriegspire_North","Kriegspire_SourceGrid",Side.North),
  new V("FrozenHighlands_North","FrozenHighlands_SourceGrid",Side.North),
  new V("SilverCove_North","SilverCove_SourceGrid",Side.North),
  new V("EelInfestedWaters_North","EelInfestedWaters_SourceGrid",Side.North),
  new V("ParadiseValley_West","ParadiseValley_SourceGrid",Side.West),
  new V("HermitsIsle_West","HermitsIsle_SourceGrid",Side.West),
  new V("HermitsIsle_South","HermitsIsle_SourceGrid",Side.South),
  new V("Dragonsand_South","Dragonsand_SourceGrid",Side.South),
  new V("MireOfTheDamned_South","MireOfTheDamned_SourceGrid",Side.South),
  new V("CastleIronfist_South","CastleIronfist_SourceGrid",Side.South),
  new V("ArchipelagoOfTheAncients","NewSorpigal_OpenWorld",Side.South),
  new V("DragonIsle_South","SweetWater_SourceGrid",Side.West)
 };
 const float Band=64f;
 static bool IsVegRoot(Transform t){
  string n=t.name;
  bool named=n.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("SourceBush_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("Desert_Shrub",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("GreenTree_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("GreenShrub_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("Calibrated_",StringComparison.OrdinalIgnoreCase);
  if(!named)return false;
  return t.GetComponentsInChildren<Renderer>(true).Length>0;
 }
 static float Hash01(string n,Vector3 p){
  unchecked{
   int h=17;foreach(char c in n)h=h*31+c;
   h=h*31+Mathf.RoundToInt(p.x*10);h=h*31+Mathf.RoundToInt(p.z*10);
   uint u=(uint)h;u^=u<<13;u^=u>>17;u^=u<<5;
   return (u&0xFFFFFF)/16777215f;
  }
 }

 static float DistanceToSourceEdge(Vector3 p,Side side){
  if(side==Side.North)return 256f-p.z;
  if(side==Side.West)return p.x+256f;
  return p.z+256f;
 }
 static float DistanceIntoTarget(Vector3 p,Side side){
  if(side==Side.North)return p.z+256f;
  if(side==Side.West)return 256f-p.x;
  return 256f-p.z;
 }
 static float Smooth(float a,float b,float x){
  float t=Mathf.Clamp01((x-a)/Mathf.Max(.0001f,b-a));return t*t*(3f-2f*t);
 }
 static bool GeneratedVegetation(string n){
  return n.StartsWith("Pine_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("YoungPine_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("Shrub_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("Searsia_",StringComparison.OrdinalIgnoreCase)||
   n.StartsWith("Cactus_",StringComparison.OrdinalIgnoreCase);
 }
 static Vector3 TargetLocal(Vector3 p,Side side,float jitter){
  if(side==Side.North)return new Vector3(p.x+jitter,p.y,-256f+(256f-p.z));
  if(side==Side.West)return new Vector3(256f-(p.x+256f),p.y,p.z+jitter);
  return new Vector3(p.x+jitter,p.y,256f-(p.z+256f));
 }
 static bool MaterialOK(GameObject go){
  foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
   if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
   if(r.sharedMaterials==null||r.sharedMaterials.Length==0)return false;
   foreach(var m in r.sharedMaterials)
    if(!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader")
     return false;
  }
  return true;
 }
 static string ApplyOne(V v){
  string tp="Assets/Scenes/"+v.target+".unity";
  string sp="Assets/Scenes/"+v.source+".unity";
  var target=EditorSceneManager.OpenScene(tp,OpenSceneMode.Single);
  var tr=target.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
  if(!tr)throw new Exception("Target root missing "+v.target);
  var terrain=tr.GetComponentInChildren<Terrain>(true);
  if(!terrain)throw new Exception("Target terrain missing "+v.target);
  var old=tr.transform.Find("Seam Vegetation Transition");
  if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  var group=new GameObject("Seam Vegetation Transition");group.transform.SetParent(tr.transform,false);
  var source=EditorSceneManager.OpenScene(sp,OpenSceneMode.Additive);
  var sr=source.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
  if(!sr)throw new Exception("Source root missing "+v.source);
  var candidates=sr.GetComponentsInChildren<Transform>(true).Where(IsVegRoot).ToArray();
  int near=0,kept=0,bad=0,underwater=0,pruned=0;
  // Cross-fade generic dressing out at the seam while source vegetation fades in.
  var dressing=tr.transform.Find("Region Dressing - Reference Matched")??
               tr.transform.Find("Edge Dressing - Reference Matched");
  if(dressing){
   var children=dressing.Cast<Transform>().ToArray();
   foreach(var c in children){
    if(!GeneratedVegetation(c.name))continue;
    float q=DistanceIntoTarget(c.localPosition,v.side);
    if(q<0f||q>Band)continue;
    float genericKeep=Smooth(8f,Band,q);
    if(Hash01("generic_"+c.name,c.localPosition)>genericKeep){
     UnityEngine.Object.DestroyImmediate(c.gameObject);pruned++;
    }
   }
  }
  var used=new List<Vector2>();
  foreach(var c in candidates){
   Vector3 lp=sr.transform.InverseTransformPoint(c.position);
   float d=DistanceToSourceEdge(lp,v.side);
   if(d<0f||d>Band)continue;
   near++;
   float keep=1f-.84f*Mathf.Pow(d/Band,.80f);
   if(Hash01(c.name,lp)>keep)continue;
   float jitter=(Hash01(c.name+"j",lp)-.5f)*4f;
   Vector3 loc=TargetLocal(lp,v.side,jitter);
   Vector3 wp=tr.transform.TransformPoint(new Vector3(loc.x,0,loc.z));
   float gy=terrain.SampleHeight(wp)+terrain.transform.position.y;
   float nx=Mathf.Clamp01((wp.x-terrain.transform.position.x)/terrain.terrainData.size.x);
   float nz=Mathf.Clamp01((wp.z-terrain.transform.position.z)/terrain.terrainData.size.z);
   float slope=terrain.terrainData.GetSteepness(nx,nz);
   if(gy<.65f||slope>38f){underwater++;continue;}
   Vector2 p2=new Vector2(loc.x,loc.z);
   if(used.Any(u=>(u-p2).sqrMagnitude<4f))continue;
   var clone=UnityEngine.Object.Instantiate(c.gameObject);
   clone.name=c.name+" - Seam Continuation";
   SceneManager.MoveGameObjectToScene(clone,target);
   clone.transform.SetParent(group.transform,false);
   clone.transform.localPosition=new Vector3(loc.x,gy-tr.transform.position.y,loc.z);
   clone.transform.rotation=c.rotation;
   clone.transform.localScale=c.lossyScale;
   if(!MaterialOK(clone)){UnityEngine.Object.DestroyImmediate(clone);bad++;continue;}
   used.Add(p2);kept++;
  }
  EditorSceneManager.CloseScene(source,true);
  EditorSceneManager.MarkSceneDirty(target);
  if(!EditorSceneManager.SaveScene(target,tp))throw new IOException("Save failed "+tp);
  return v.target+",source="+v.source+",source_band_candidates="+near+
   ",continued="+kept+",generic_pruned="+pruned+
   ",rejected_ground="+underwater+",bad_materials="+bad;
 }

 [MenuItem("MMUnity/World/Blend Vegetation Across Original Extension Seams")]
 public static void ApplyAll(){
  var rows=new List<string>();
  foreach(var v in Vals)rows.Add(ApplyOne(v));
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/original_to_extension_vegetation.csv",rows);
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  Debug.Log("ORIGINAL_TO_EXTENSION_VEGETATION_DONE seams="+rows.Count);
 }
 [MenuItem("MMUnity/World/Blend Dragon South Western Forest Edge Only")]
 public static void ApplyDragonSouthOnly(){
  string row=ApplyOne(Vals.Single(v=>v.target=="DragonIsle_South"));
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllText("Validation/EdgeGrid20260923/dragon_south_source_edge_vegetation.txt",row);
  AssetDatabase.SaveAssets();Debug.Log("DRAGON_SOUTH_SOURCE_EDGE "+row);
 }
}
