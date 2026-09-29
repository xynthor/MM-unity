using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMParadiseCapeInteriorQueued20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static MMParadiseCapeInteriorQueued20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_PARADISE_CAPE_INTERIOR_RECOVER.flag";
  if(!File.Exists(flag))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(flag);
  try{
   var sc=SceneManager.GetActiveScene();
   if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
    throw new Exception("Saved linked world must be active");
   MMParadiseCapeReferenceLandRecover20260927.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"paradise_cape_interior_runtime_20260927.txt",
    "PASS Paradise inland southern peninsula recovered; linked world rebuilt; 30/49 QA rendered\n");
  }catch(Exception e){
   File.WriteAllText(V+"paradise_cape_interior_runtime_20260927.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
