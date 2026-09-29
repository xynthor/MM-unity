using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor.SceneManagement;
internal class CommandScript:IRunCommand{
 const string Out="Validation/CanalApproval_20260920";
 void Cap(string scene,float cx,float cz,float span,string tag,bool full=false){
   var s=EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity",OpenSceneMode.Single);
   var terr=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
   Directory.CreateDirectory(Out);
   float ground=terr?terr.SampleHeight(new Vector3(cx,0,cz))+terr.transform.position.y:0f;
   var go=new GameObject("__CanalApprovalCamera");var c=go.AddComponent<Camera>();c.clearFlags=CameraClearFlags.Skybox;c.farClipPlane=3000;
   c.orthographic=true;c.orthographicSize=full?300f:Mathf.Max(24f,span*.75f);c.transform.position=new Vector3(cx,220,cz);c.transform.rotation=Quaternion.Euler(90,0,0);
   MMSequentialEvidence.Render(c,Out+"/"+tag+"_top.png",1100,900);
   c.orthographic=false;c.fieldOfView=48;float d=full?260f:Mathf.Max(30f,span*1.2f);c.transform.position=new Vector3(cx+d*.55f,ground+d*.55f,cz-d*.75f);c.transform.LookAt(new Vector3(cx,ground,cz));
   MMSequentialEvidence.Render(c,Out+"/"+tag+"_oblique.png",1100,800);
   UnityEngine.Object.DestroyImmediate(go);
 }
 public void Execute(ExecutionResult result){
   Cap("FreeHaven",25.1f,-5.7f,55f,"FreeHaven");
   Cap("MireOfTheDamned",29.7f,130.6f,80f,"Mire");
   Cap("FrozenHighlands",0f,0f,512f,"WhiteCap_full",true);
   result.Log("Canal approval captures written");
 }
}