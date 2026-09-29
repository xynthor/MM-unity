using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMNorthC1RebuildQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthC1RebuildQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_C1_REBUILD.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_NORTH_C1_REBUILD.flag");
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked scene must be active and clean");
   if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeLinkedNorthC1Finalizer_20260925\manifest.json"))
    throw new Exception("Immutable C1 scene/assets snapshot missing");
   File.WriteAllText(V+"linked_north_c1_transition_20260925.csv","tile,metrics\n");
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"linked_north_c1_rebuild_runtime.txt",
    "PASS: four source-to-north linked-only C1 profiles and linked scene saved\n");
  }catch(Exception ex){
   File.WriteAllText(V+"linked_north_c1_rebuild_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
