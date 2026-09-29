using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthBasemapDistanceDiagnosticOnce20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthBasemapDistanceDiagnosticOnce20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_BASEMAP_DISTANCE_DIAG.flag";
  if(!File.Exists(flag))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(flag);
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
    throw new Exception("Saved linked world required for basemap diagnostic");
   var terrains=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
   var prior=terrains.ToDictionary(t=>t,t=>t.basemapDistance);
   foreach(var t in terrains)t.basemapDistance=10000f;
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.Copy("Preview/Enroth_Reference_After20260925_North.png",
             "Preview/Enroth_Reference_After20260925_North_fullresdiag.png",true);
   foreach(var kv in prior)if(kv.Key)kv.Key.basemapDistance=kv.Value;
   File.WriteAllText(V+"north_basemap_distance_diag_20260926.txt",
     "PASS terrains="+terrains.Length+" renderedWithBasemapDistance=10000 restored=true\n");
  }catch(Exception e){
   File.WriteAllText(V+"north_basemap_distance_diag_20260926.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
