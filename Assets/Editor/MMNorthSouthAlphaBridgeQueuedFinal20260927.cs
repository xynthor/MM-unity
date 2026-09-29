using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMNorthSouthAlphaBridgeQueuedFinal20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static double next;
 static MMNorthSouthAlphaBridgeQueuedFinal20260927(){EditorApplication.update+=Run;}
 static void Run(){
  if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;
  string flag=V+"RUN_FINAL_NS_ALPHA_BRIDGE.flag";
  if(!File.Exists(flag))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  File.Delete(flag);
  try{
   var s=SceneManager.GetActiveScene();
   if(s.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||s.isDirty)
    throw new Exception("Saved linked world required");
   MMNorthSouthAlphaBridge20260926.Apply();
   foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(UnityEngine.FindObjectsSortMode.None))t.Flush();
   AssetDatabase.SaveAssets();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"final_ns_alpha_bridge_runtime_20260927.txt","PASS current bridge executed and QA rendered\n");
  }catch(Exception e){
   File.WriteAllText(V+"final_ns_alpha_bridge_runtime_20260927.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }finally{EditorApplication.update-=Run;}
 }
}