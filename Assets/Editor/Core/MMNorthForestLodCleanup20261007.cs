using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;using UnityEditor.SceneManagement;
public static class MMNorthForestLodCleanup20261007
{
 public static void Run(int square,bool keep=false)
 {
  var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");string report="Validation/SequentialRepair/sq"+square+"_lod_cleanup.txt";if(File.Exists(report))throw new Exception("Already kept");var root=UnityEngine.Object.FindObjectsByType<Transform>().First(t=>t.name.StartsWith("SQ"+square+" Authored")&&t.name.Contains("Forest 20261007"));var disabled=new HashSet<GameObject>();bool saved=false;
  try
  {
   foreach(Transform tree in root){if(!tree.gameObject.activeSelf)continue;var managed=tree.GetComponentsInChildren<LODGroup>(true).SelectMany(g=>g.GetLODs()).SelectMany(l=>l.renderers).Where(r=>r).ToHashSet();foreach(var renderer in tree.GetComponentsInChildren<Renderer>())if(!managed.Contains(renderer)&&renderer.gameObject.activeSelf){disabled.Add(renderer.gameObject);renderer.gameObject.SetActive(false);}}
   string dir="Preview/NorthAuthored20261007/LodCleanup"+square+"/";Directory.CreateDirectory(dir);float center=-1024+512*square;MMNorthAuthoredTools20261007.Capture(dir,"top",new Vector3(center,850,1024),new Vector3(center,0,1024),true);MMNorthAuthoredTools20261007.Capture(dir,"oblique",new Vector3(center-346,225,740),new Vector3(center+14,55,1050),false);if(square==2)MMNorthAuthoredTools20261007.Capture(dir,"lake",new Vector3(-35,53,920),new Vector3(15,24,1070),false);
   if(keep){string backup="Backups/NorthAuthored20261007/LodCleanup"+square+"Before";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/Enroth.unity"))File.Copy(scene.path,backup+"/Enroth.unity");EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;File.WriteAllText(report,"Disabled "+disabled.Count+" orphaned lower LOD renderer objects; production LOD0/1 and shared materials preserved.");}
  }finally{if(!saved)foreach(var g in disabled)if(g)g.SetActive(true);}
 }
}
