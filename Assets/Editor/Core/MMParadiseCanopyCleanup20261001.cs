using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMParadiseCanopyCleanup20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string RedundantRoot="Paradise Combined Reference Canopy 20260930";

 [MenuItem("MMUnity/World/Remove Redundant Paradise Combined Canopy")]
 public static void Run(){
  var s=SceneManager.GetActiveScene();if(s.path!=ScenePath)throw new Exception("Enroth must be active");
  var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var root=all.FirstOrDefault(t=>t.name==RedundantRoot);if(!root)throw new Exception("Redundant Paradise canopy not found");
  int cells=root.childCount,renderers=root.GetComponentsInChildren<Renderer>(true).Length;
  UnityEngine.Object.DestroyImmediate(root.gameObject);
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save Paradise canopy cleanup");
  Directory.CreateDirectory("Validation/EdgeGrid20260923/ParadiseForestQA20261001");
  File.WriteAllText("Validation/EdgeGrid20260923/ParadiseForestQA20261001/cleanup.txt",$"PASS removedRoot={RedundantRoot} cells={cells} renderers={renderers} terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0\n");
  Debug.Log($"PARADISE_CANOPY_CLEANUP cells={cells} renderers={renderers}");
 }
}
