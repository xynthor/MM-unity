using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMNorthSourceSeamMicroPolishQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthSourceSeamMicroPolishQueued20260925(){
  EditorApplication.delayCall+=Run;
 }
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_SEAM_MICROPOLISH.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_NORTH_SEAM_MICROPOLISH.flag");
  try{
   MMNorthSourceSeamMicroPolish20260925.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"north_seam_micro_polish_runtime.txt",
    "PASS targeted north source profile correction; linked scene rebuilt\n");
   Debug.Log("NORTH_SOURCE_SEAM_MICROPOLISH_LINKED_REBUILT");
  }catch(Exception ex){
   File.WriteAllText(V+"north_seam_micro_polish_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
