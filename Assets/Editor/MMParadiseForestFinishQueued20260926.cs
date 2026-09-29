using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
[InitializeOnLoad]
internal static class MMParadiseForestFinishQueued20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Flag=V+"RUN_PARADISE_FOREST_FINISH_20260926.flag";
 static double next=0;
 static MMParadiseForestFinishQueued20260926(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(!File.Exists(Flag)){EditorApplication.update-=Poll;return;}
  if(EditorApplication.timeSinceStartup<next)return;
  next=EditorApplication.timeSinceStartup+2.0;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists(V+"RUN_LINKED_NORTH_SOURCE_PROFILE.flag"))return;
  string r=V+"north_photo_snow_join_20260926.txt";
  string done=V+"linked_only_north_cliff_runtime.txt";
  if(!File.Exists(r)||!File.Exists(done))return;
  var lines=File.ReadAllLines(r);
  if(!new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}.All(
    n=>lines.Any(l=>l.StartsWith(n+" "))))return;
  if(File.GetLastWriteTimeUtc(done)<File.GetLastWriteTimeUtc(r)||
     !File.ReadAllText(done).StartsWith("PASS"))return;
  File.Delete(Flag);
  try{
   var sc=SceneManager.GetActiveScene();
   if(string.IsNullOrEmpty(sc.path)&&!sc.isDirty){
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
      "Assets/Scenes/World/Enroth.unity",
      UnityEditor.SceneManagement.OpenSceneMode.Single);
    sc=SceneManager.GetActiveScene();
   }
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked Enroth scene required");
   if(!File.Exists(V+"paradise_photo_canopy_finish_20260926.txt"))
    MMParadiseReferenceCanopyFinish20260926.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"paradise_forest_finish_runtime_20260926.txt",
    "PASS native photo-mask woodland installed; linked scene rebuilt\n");
  }catch(Exception e){
   File.WriteAllText(V+"paradise_forest_finish_runtime_20260926.txt",
    "FAIL "+e+"\n");
   Debug.LogError(e);
  }finally{EditorApplication.update-=Poll;}
 }
}
