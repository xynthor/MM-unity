using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
[InitializeOnLoad]
internal static class MMRepairBridgePing {
 static MMRepairBridgePing() {
   EditorApplication.delayCall += Ping;
 }
 static void Ping(){
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  var sc=SceneManager.GetActiveScene();
  File.WriteAllText("Validation/EdgeGrid20260923/editor_domain_ping.txt",
     "TIME="+DateTime.Now.ToString("o")+"\nSCENE="+sc.path+
     "\nDIRTY="+sc.isDirty+"\nIS_COMPILING="+EditorApplication.isCompiling);
  Debug.Log("SEVEN_CROP_AUTORUNNER_EDITOR_DOMAIN_READY");
 }
}