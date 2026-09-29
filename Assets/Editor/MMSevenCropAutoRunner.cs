using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// One-shot, restart-safe launcher for the seven reference corrections.
// Runs only after an explicit request marker is present.
[InitializeOnLoad]
public static class MMSevenCropAutoRunner {
 const string Base="Validation/EdgeGrid20260923/";
 const string Request=Base+"RUN_SEVEN_REFERENCE_REPAIR.request";
 static MMSevenCropAutoRunner(){EditorApplication.delayCall+=RunIfRequested;}
 static void Status(string s){
  Directory.CreateDirectory(Base);
  File.WriteAllText(Base+"WORK_STEP.txt",s);
  File.AppendAllText(Base+"seven_crop_build_steps.log",
    DateTime.Now.ToString("o")+" "+s+"\n");
  Debug.Log("SEVEN_CROP_BUILD "+s);
 }
 static void RunIfRequested(){
  if(!File.Exists(Request))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
    EditorApplication.delayCall+=RunIfRequested;return;
  }
  var sc=SceneManager.GetActiveScene();
  if(sc.isDirty){
    Status("STOP_UNSAVED_ACTIVE_SCENE_"+sc.path);
    return;
  }
  File.Delete(Request);
  try{
   Status("BUILDING_FIVE_REFERENCE_NORTH_WEST_TILES");
   MMWorldExtensionBuilder.BuildWestNorthOnly();
   Status("RESTORING_REFERENCE_VEGETATION");
   MMWorldExtensionDressing.DressWestNorthOnly();
   Status("BUILDING_DENSE_NONOVERLAPPING_ICE_SHELVES");
   MMReferenceIceShelf.DressNorthAll();
   Status("REBUILDING_TWO_DRAGON_REFERENCE_HALVES");
   MMDragonReferenceReshape.Apply();
   Status("REBUILDING_THIRTY_TILE_LINKED_WORLD");
   BuildEnrothLinkedOpenWorld.Build();
   Status("AUTOMATED_GRID_AND_SEAM_AUDIT");
   string audit=MMSevenCropBuiltInAudit.Run();
   File.WriteAllText(Base+"seven_crop_auto_audit.txt",audit);
   Status(audit.StartsWith("PASS")?
    "BUILT_AWAITING_VISUAL_REFERENCE_REVIEW":"AUDIT_FAILED_REQUIRES_CORRECTION");
  }catch(Exception ex){
   File.WriteAllText(Base+"seven_crop_autorun_error.txt",ex.ToString());
   Status("BUILD_FAILED_"+ex.GetType().Name+"_"+ex.Message);
   Debug.LogException(ex);
  }
 }
}
public static class MMSevenCropBuiltInAudit {
 public static string Run(){
  var scene=SceneManager.GetActiveScene();
  var t=scene.GetRootGameObjects()
   .SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
   .Where(v=>v&&v.terrainData&&v.gameObject.activeInHierarchy).ToArray();
  var grid=new Dictionary<string,Terrain>();
  int badSize=0,badPlace=0,dups=0;
  foreach(var x in t){
   var sz=x.terrainData.size;
   if(Mathf.Abs(sz.x-512f)>.01f||Mathf.Abs(sz.z-512f)>.01f)badSize++;
   int gx=Mathf.RoundToInt((x.transform.position.x+256f)/512f);
   int gz=Mathf.RoundToInt((x.transform.position.z+256f)/512f);
   if(Mathf.Abs(x.transform.position.x+256f-gx*512f)>.05f||
      Mathf.Abs(x.transform.position.z+256f-gz*512f)>.05f)badPlace++;
   string key=gx+","+gz;
   if(grid.ContainsKey(key))dups++;
   else grid.Add(key,x);
  }
  float worst=0f;int seams=0;
  var heightCache=new Dictionary<Terrain,float[,]>();
  foreach(var x in t)heightCache[x]=x.terrainData.GetHeights(0,0,513,513);
  foreach(var kv in grid){
   var a=kv.Value;var h=heightCache[a];
   var parts=kv.Key.Split(',');
   int gx=int.Parse(parts[0]),gz=int.Parse(parts[1]);
   foreach(var dir in new[]{new Vector2Int(1,0),new Vector2Int(0,1)}){
    if(!grid.TryGetValue((gx+dir.x)+","+(gz+dir.y),out var b))continue;
    seams++;var bh=heightCache[b];
    for(int i=0;i<513;i++){
     float ay=a.transform.position.y+(dir.x==1?h[i,512]:h[512,i])*a.terrainData.size.y;
     float by=b.transform.position.y+(dir.x==1?bh[i,0]:bh[0,i])*b.terrainData.size.y;
     worst=Mathf.Max(worst,Mathf.Abs(ay-by));
    }
   }
  }
  bool pass=scene.path=="Assets/Scenes/Enroth_Linked_OpenWorld.unity"&&
    t.Length==30&&grid.Count==30&&dups==0&&badSize==0&&badPlace==0&&
    seams==49&&worst<=.10f;
  return (pass?"PASS":"FAIL")+" scene="+scene.path+
     " terrains="+t.Length+"/30 unique="+grid.Count+
     " duplicates="+dups+" badSize="+badSize+" badPlace="+badPlace+
     " seams="+seams+"/49 worst_m="+worst.ToString("F5");
 }
}

