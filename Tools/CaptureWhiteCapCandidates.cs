using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor.SceneManagement;
internal class CommandScript:IRunCommand{
 void Cap(float cx,float cz,float span,string tag){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/FrozenHighlands_SourceGrid.unity",OpenSceneMode.Single);
  var terr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
  float gy=terr.SampleHeight(new Vector3(cx,0,cz))+terr.transform.position.y;
  Directory.CreateDirectory("Validation/CanalApproval_20260920");
  var go=new GameObject("__cap");var c=go.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=2000;
  c.orthographic=true;c.orthographicSize=Mathf.Max(25,span*.75f);c.transform.position=new Vector3(cx,180,cz);c.transform.rotation=Quaternion.Euler(90,0,0);
  MMSequentialEvidence.Render(c,"Validation/CanalApproval_20260920/"+tag+"_top.png",1000,900);
  c.orthographic=false;c.fieldOfView=50;float d=Mathf.Max(35,span*1.2f);c.transform.position=new Vector3(cx+d*.55f,gy+d*.55f,cz-d*.75f);c.transform.LookAt(new Vector3(cx,.1f,cz));
  MMSequentialEvidence.Render(c,"Validation/CanalApproval_20260920/"+tag+"_oblique.png",1000,800);
  UnityEngine.Object.DestroyImmediate(go);
 }
 public void Execute(ExecutionResult result){
  Cap(-158.56f,21.78f,45f,"WhiteCap_candidate47");
  Cap(-104.06f,-179.44f,75f,"WhiteCap_candidate5");
  result.Log("WhiteCap candidate captures done");
 }
}