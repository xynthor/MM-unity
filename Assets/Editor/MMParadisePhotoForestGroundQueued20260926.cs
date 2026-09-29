using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMParadisePhotoForestGroundQueued20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Flag=V+"RUN_PARADISE_PHOTO_FOREST_GROUND_20260926.flag";
 static MMParadisePhotoForestGroundQueued20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(Flag))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(Flag);
  try{
   var sc=SceneManager.GetActiveScene();
   if(string.IsNullOrEmpty(sc.path)&&!sc.isDirty){
    EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
    sc=SceneManager.GetActiveScene();
   }
   if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
    throw new Exception("Saved linked Enroth world must be active");
   MMParadisePhotoForestGround20260926.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"paradise_photo_forest_ground_runtime_20260926.txt",
    "PASS generated Paradise woodland photo-ground, linked scene rebuilt and rendered\n");
  }catch(Exception e){
   File.WriteAllText(V+"paradise_photo_forest_ground_runtime_20260926.txt",
    "FAIL "+e+"\n");Debug.LogError(e);
  }
 }
}

// photo-confirmed woodland palette pass, 20260926 23:18
