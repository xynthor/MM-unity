using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
[InitializeOnLoad]
internal static class MMNorthBasemapDistance5000Test20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthBasemapDistance5000Test20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string f=V+"RUN_NORTH_BASEMAP_5000_TEST.flag";
  if(!File.Exists(f))return; File.Delete(f);
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
   throw new Exception("Saved linked world required");
  int changed=0;
  foreach(var t in UnityEngine.Object.FindObjectsByType<Terrain>(UnityEngine.FindObjectsSortMode.None)){
   var p=t.transform.position;
   bool row=(Mathf.Abs(p.z-256f)<1f||Mathf.Abs(p.z-768f)<1f);
   bool col=p.x>=-1281f&&p.x<=257f;
   if(!row||!col)continue;
   t.basemapDistance=5000f;t.heightmapPixelError=2f;t.drawInstanced=true;changed++;
  }
  if(changed!=8)throw new Exception("Expected 8 north source/extension terrains, got "+changed);
  EditorSceneManager.MarkSceneDirty(sc);
  if(!EditorSceneManager.SaveScene(sc,sc.path))throw new IOException("Could not save basemap parity test");
  MMReference20260925VisualQAOnce.RunFinalQA();
  File.WriteAllText(V+"north_basemap_5000_test_20260927.txt","PASS terrains="+changed+" basemapDistance=5000 pixelError=2\n");
 }
}

// trigger 20260927
