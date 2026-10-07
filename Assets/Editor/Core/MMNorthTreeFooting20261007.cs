using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
public static class MMNorthTreeFooting20261007
{
 public static void Run(bool keep=false)
 {
  var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean saved Enroth");if(File.Exists("Validation/SequentialRepair/tree_footing_kept.txt"))throw new Exception("Already kept");var states=new List<System.Tuple<Transform,Vector3,bool>>();bool saved=false;int moved=0,removed=0;
  try
  {
   for(int square=0;square<4;square++){var root=UnityEngine.Object.FindObjectsByType<Transform>().First(t=>t.name.StartsWith("SQ"+square+" Authored")&&t.name.Contains("Forest 20261007"));var terrain=Terrain.activeTerrains.First(t=>t.transform.position.x==-1280+512*square&&t.transform.position.z==768);foreach(Transform tree in root){if(!tree.gameObject.activeSelf)continue;var p=tree.position;float ground=MMNorthAuthoredTools20261007.Ground(terrain,p.x,p.z);float slope=terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/512,(p.z-768)/512);states.Add(System.Tuple.Create(tree,p,true));if(ground<.8f||slope>36){tree.gameObject.SetActive(false);removed++;continue;}var rs=tree.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);float delta=ground-bounds.min.y-.12f;if(Mathf.Abs(delta)>.001f){tree.position+=Vector3.up*delta;moved++;}}
    string dir="Preview/NorthAuthored20261007/TreeFooting"+square+"/";Directory.CreateDirectory(dir);float center=-1024+512*square;MMNorthAuthoredTools20261007.Capture(dir,"top",new Vector3(center,850,1024),new Vector3(center,0,1024),true);MMNorthAuthoredTools20261007.Capture(dir,"transition",new Vector3(center,155,530),new Vector3(center,35,950),false);if(square==2)MMNorthAuthoredTools20261007.Capture(dir,"lake",new Vector3(-35,53,920),new Vector3(15,24,1070),false);
   }
   if(keep){string backup="Backups/NorthAuthored20261007/TreeFootingBefore";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/Enroth.unity"))File.Copy(scene.path,backup+"/Enroth.unity");EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;File.WriteAllText("Validation/SequentialRepair/tree_footing_kept.txt","Grounded visible production mesh bounds after orphan LOD cleanup: moved="+moved+" steep/wet removed="+removed+". XZ, rotation, scale and source materials preserved.");}
  }finally{if(!saved)foreach(var s in states){s.Item1.position=s.Item2;s.Item1.gameObject.SetActive(s.Item3);}}
 }
}
