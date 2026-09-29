using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
[InitializeOnLoad]
internal static class MMReloadNorthRollbackOnce20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMReloadNorthRollbackOnce20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_RELOAD_NORTH_ROLLBACK_20260926.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_RELOAD_NORTH_ROLLBACK_20260926.flag");
  try{
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   foreach(var n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
    AssetDatabase.ImportAsset("Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset",
      ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
    AssetDatabase.ImportAsset("Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset",
      ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
   }
   EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"reload_north_rollback_runtime_20260926.txt","PASS forced terrain asset reload and fresh render\n");
  }catch(System.Exception e){
   File.WriteAllText(V+"reload_north_rollback_runtime_20260926.txt","FAIL "+e+"\n");Debug.LogError(e);
  }
 }
}

// Force-reference-reload after Gaussian tangent QA
