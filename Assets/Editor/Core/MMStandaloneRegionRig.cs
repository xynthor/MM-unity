using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMStandaloneRegionRig {
 static readonly string[] Scenes={
  "SweetWater_North","Kriegspire_North","FrozenHighlands_North","SilverCove_North",
  "EelInfestedWaters_North","ParadiseValley_West","HermitsIsle_West","SouthwestOcean",
  "HermitsIsle_South","Dragonsand_South","MireOfTheDamned_South","CastleIronfist_South",
  "ArchipelagoOfTheAncients","DragonIsle_North","DragonIsle_South"
 };
 static bool FindSpawn(Terrain t,out Vector3 world){
  float best=float.NegativeInfinity;world=Vector3.zero;
  Vector3 p=t.transform.position;Vector3 sz=t.terrainData.size;
  for(int z=2;z<=14;z++)for(int x=2;x<=14;x++){
   float nx=x/16f,nz=z/16f;
   float y=t.SampleHeight(new Vector3(p.x+nx*sz.x,0,p.z+nz*sz.z))+p.y;
   float slope=t.terrainData.GetSteepness(nx,nz);
   if(y<1.0f||slope>30f)continue;
   float dx=nx-.5f,dz=nz-.5f;
   float score=-(dx*dx+dz*dz)*80f+y*.08f-slope*.15f;
   if(score>best){best=score;world=new Vector3(p.x+nx*sz.x,y+1.15f,p.z+nz*sz.z);}
  }
  return best>float.NegativeInfinity;
 }
 static void RemoveExisting(GameObject root){
  foreach(var c in root.GetComponentsInChildren<MMThirdPersonController>(true))
   if(c.gameObject.name=="Player - Third Person")UnityEngine.Object.DestroyImmediate(c.gameObject);
  foreach(var c in root.GetComponentsInChildren<Camera>(true))
   if(c.gameObject.name=="Player Camera"||c.gameObject.name=="Standalone Overview Camera")
    UnityEngine.Object.DestroyImmediate(c.gameObject);
 }

 public static string EnsureScene(string scenePath){
  var target=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
  var root=target.GetRootGameObjects().FirstOrDefault(g=>
   g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
  if(!root)throw new Exception("Standalone root missing "+scenePath);
  var terrain=root.GetComponentInChildren<Terrain>(true);
  if(!terrain)throw new Exception("Standalone terrain missing "+scenePath);
  RemoveExisting(root);
  var source=EditorSceneManager.OpenScene("Assets/Scenes/Regions/NewSorpigal.unity",OpenSceneMode.Additive);
  var sroot=source.GetRootGameObjects().FirstOrDefault(g=>
   g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
  var srcCtrl=sroot?sroot.GetComponentInChildren<MMThirdPersonController>(true):null;
  var srcCam=sroot?sroot.GetComponentInChildren<Camera>(true):null;
  if(!srcCam)throw new Exception("Canonical player camera missing");
  bool playable=FindSpawn(terrain,out Vector3 spawn);
  Camera cam;
  if(playable&&srcCtrl){
   var pg=UnityEngine.Object.Instantiate(srcCtrl.gameObject);
   pg.name="Player - Third Person";
   SceneManager.MoveGameObjectToScene(pg,target);pg.transform.SetParent(root.transform,true);
   pg.transform.position=spawn;pg.transform.rotation=Quaternion.identity;
   var cg=UnityEngine.Object.Instantiate(srcCam.gameObject);
   cg.name="Player Camera";SceneManager.MoveGameObjectToScene(cg,target);
   cg.transform.SetParent(root.transform,true);
   cam=cg.GetComponent<Camera>();
   cam.tag="MainCamera";cam.enabled=true;
   cg.transform.position=spawn+new Vector3(0,3.8f,-6.5f);
   cg.transform.LookAt(spawn+Vector3.up*1.4f);
   var ctrl=pg.GetComponent<MMThirdPersonController>();
   ctrl.playerCamera=cam;ctrl.enabled=true;
  }else{
   var cg=UnityEngine.Object.Instantiate(srcCam.gameObject);
   cg.name="Standalone Overview Camera";SceneManager.MoveGameObjectToScene(cg,target);
   cg.transform.SetParent(root.transform,true);
   cam=cg.GetComponent<Camera>();cam.tag="MainCamera";cam.enabled=true;
   Vector3 center=terrain.transform.position+terrain.terrainData.size*.5f;
   cg.transform.position=center+new Vector3(0,115f,-145f);
   cg.transform.LookAt(center+Vector3.up*2f);
  }
  foreach(var c in root.GetComponentsInChildren<Camera>(true))if(c!=cam)c.enabled=false;
  var listeners=root.GetComponentsInChildren<AudioListener>(true);
  bool kept=false;foreach(var a in listeners){if(a.gameObject==cam.gameObject&&!kept){a.enabled=true;kept=true;}else a.enabled=false;}
  if(!cam.GetComponent<AudioListener>())cam.gameObject.AddComponent<AudioListener>();
  EditorSceneManager.CloseScene(source,true);
  EditorSceneManager.MarkSceneDirty(target);
  if(!EditorSceneManager.SaveScene(target,scenePath))throw new IOException("Save failed "+scenePath);
  return Path.GetFileNameWithoutExtension(scenePath)+",mode="+(playable?"THIRD_PERSON":"OVERVIEW")+
   ",camera="+cam.name+",spawn="+(playable?spawn.ToString():"NO_DRY_SPAWN");
 }

 [MenuItem("MMUnity/World/Install Standalone Cameras And Players")]
 public static void EnsureAll(){
  var rows=new List<string>();
  foreach(string n in Scenes)rows.Add(EnsureScene("Assets/Scenes/"+n+".unity"));
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/standalone_camera_player_audit.csv",rows);
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  Debug.Log("STANDALONE_CAMERA_PLAYER_DONE scenes="+rows.Count);
 }
}
