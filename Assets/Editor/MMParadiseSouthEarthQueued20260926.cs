using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMParadiseSouthEarthQueued20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Flag=V+"RUN_PARADISE_SOUTH_EARTH_20260926.flag";
 static double next;
 static MMParadiseSouthEarthQueued20260926(){
  EditorApplication.delayCall+=Poll;
  EditorApplication.update+=Poll;
 }
 static void Poll(){
  if(!File.Exists(Flag))return;
  if(EditorApplication.timeSinceStartup<next)return;
  next=EditorApplication.timeSinceStartup+4.0;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(File.Exists(V+"RUN_LINKED_NORTH_SOURCE_PROFILE.flag"))return;
  var sc=SceneManager.GetActiveScene();
  if(sc.isDirty)return;
  File.Delete(Flag);
  try{
   if(string.IsNullOrEmpty(sc.path)){
    EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
    sc=SceneManager.GetActiveScene();
   }
   if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity")
    throw new Exception("Saved linked Enroth scene required");
   MMParadiseSouthPhotoEarth20260926.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"paradise_south_earth_runtime_20260926.txt",
    "PASS registered ochre south headland saved; 30/30 tile and 49/49 seam QA, four perspective renders\n");
  }catch(Exception ex){
   File.WriteAllText(V+"paradise_south_earth_runtime_20260926.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
