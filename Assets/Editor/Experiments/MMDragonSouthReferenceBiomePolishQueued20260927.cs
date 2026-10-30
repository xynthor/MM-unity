using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMDragonSouthReferenceBiomePolishQueued20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static double next;
 static MMDragonSouthReferenceBiomePolishQueued20260927(){EditorApplication.delayCall+=Run;EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2.0;
  if(!File.Exists(V+"RUN_DRAGON_SOUTH_BIOME_POLISH.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  Run();
 }
 static void Run(){
  string flag=V+"RUN_DRAGON_SOUTH_BIOME_POLISH.flag";
  if(!File.Exists(flag))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(flag);
  try{
   var sc=SceneManager.GetActiveScene();
   if(string.IsNullOrEmpty(sc.path)&&!sc.isDirty)
    sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked scene required");
   MMDragonSouthReferenceBiomePolish20260927.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"dragon_south_reference_biome_polish_runtime_20260927.txt",
    "PASS Dragon South reference dry/forest biome saved; linked rebuilt and QA rendered\n");
  }catch(Exception e){
   File.WriteAllText(V+"dragon_south_reference_biome_polish_runtime_20260927.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
