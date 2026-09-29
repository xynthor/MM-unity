using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMWestCoastSecondPassQueued20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMWestCoastSecondPassQueued20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_WEST_COAST_SECOND_PASS_20260926.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_WEST_COAST_SECOND_PASS_20260926.flag");
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked Enroth scene required for west source correction");
   MMWestCoastSecondPass20260926.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"west_coast_second_pass_runtime_20260926.txt",
    "PASS restored reference Paradise peninsula, smooth Dragon southern peninsula and source-matched native color; rebuilt linked world\n");
  }catch(Exception e){
   File.WriteAllText(V+"west_coast_second_pass_runtime_20260926.txt",
    "FAIL "+e+"\n");Debug.LogError(e);
  }
 }
}
