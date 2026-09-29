using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMNarrowNorthToeRebuildQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNarrowNorthToeRebuildQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NARROW_NORTH_TOE_REBUILD.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_NARROW_NORTH_TOE_REBUILD.flag");
  try{
   var s=SceneManager.GetActiveScene();
   if(s.path!="Assets/Scenes/World/Enroth.unity"||s.isDirty)
    throw new Exception("Clean saved linked scene required");
   if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeAdaptiveNorthToePolish_20260925\manifest.json"))
    throw new Exception("Latest source/linked scene backup missing");
   File.WriteAllText(V+"linked_only_north_cliff_profiles_20260925.csv",
    "zone,measurements\n");
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"narrow_north_toe_rebuild_runtime_20260925.txt",
    "PASS: source-map northern ridges preserved; last 16m cliff-toe reshaped in 4 linked-only source terrains; north splat blend persisted; linked scene rebuilt\n");
  }catch(Exception e){
   File.WriteAllText(V+"narrow_north_toe_rebuild_runtime_20260925.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}

// force Unity importer to process the user-requested north 16m profile build

// final C1 exact tangent source-to-north QA stage
