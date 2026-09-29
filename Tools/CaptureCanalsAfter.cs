using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const string Out="Validation/CanalApproval_20260920";
 void Cap(string scene,float cx,float cz,float span,string tag){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity",OpenSceneMode.Single);
  var terr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
  float gy=terr.SampleHeight(new Vector3(cx,0,cz))+terr.transform.position.y;
  var go=new GameObject("__CanalAfterCamera");var c=go.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=2500;
  c.orthographic=true;c.orthographicSize=Mathf.Max(25f,span*.75f);c.transform.position=new Vector3(cx,180,cz);c.transform.rotation=Quaternion.Euler(90,0,0);
  MMSequentialEvidence.Render(c,Out+"/"+tag+"_after_top.png",1000,900);
  c.orthographic=false;c.fieldOfView=50;float d=Mathf.Max(35f,span*1.2f);c.transform.position=new Vector3(cx+d*.55f,gy+d*.55f,cz-d*.75f);c.transform.LookAt(new Vector3(cx,.1f,cz));
  MMSequentialEvidence.Render(c,Out+"/"+tag+"_after_oblique.png",1000,800);
  UnityEngine.Object.DestroyImmediate(go);
 }
 public void Execute(ExecutionResult result){
  Directory.CreateDirectory(Out);
  Cap("FreeHaven",25.1f,-5.7f,55f,"FreeHaven");
  Cap("MireOfTheDamned",29.7f,130.6f,80f,"Mire");
  Cap("FrozenHighlands",-158.56f,21.78f,45f,"WhiteCap");
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var go=new GameObject("__LinkedOverview");SceneManager.MoveGameObjectToScene(go,s);var c=go.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=5000;c.orthographic=true;c.orthographicSize=1100;c.transform.position=new Vector3(-256,2400,256);c.transform.rotation=Quaternion.Euler(90,0,0);
  MMSequentialEvidence.Render(c,Out+"/Linked_after_water_fix.png",1400,900);UnityEngine.Object.DestroyImmediate(go);
  result.Log("After canal + linked water captures written");
 }
}