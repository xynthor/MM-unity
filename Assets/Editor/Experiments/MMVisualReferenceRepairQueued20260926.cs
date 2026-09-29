using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMVisualReferenceRepairQueued20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMVisualReferenceRepairQueued20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_VISUAL_REFERENCE_REPAIR_20260926.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
    EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_VISUAL_REFERENCE_REPAIR_20260926.flag");
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked Unity scene must be active");
   MMNorthSnowReferenceBlend20260926.Apply();
   MMParadiseDragonVisualSeam20260926.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"visual_reference_repair_runtime_20260926.txt",
    "PASS: 4 north source snow seams, Paradise map coast and ground, DragonSouth coast and 12-layer native materials, linked world rebuilt\n");
  }catch(Exception e){
   File.WriteAllText(V+"visual_reference_repair_runtime_20260926.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
