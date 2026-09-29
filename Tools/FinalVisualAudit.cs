using System;using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 public void Execute(ExecutionResult result){
  var s=SceneManager.GetActiveScene(); if(s.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity") throw new Exception("Linked world not active");
  Directory.CreateDirectory("Validation/FinalWorld");
  var go=new GameObject("__FinalVisualAudit");SceneManager.MoveGameObjectToScene(go,s);
  try{
   var c=go.AddComponent<Camera>();c.depthTextureMode=DepthTextureMode.Depth;c.farClipPlane=10000;c.clearFlags=CameraClearFlags.Skybox;
   c.orthographic=true;c.orthographicSize=1700;c.transform.position=new Vector3(-100,4000,0);c.transform.rotation=Quaternion.Euler(90,0,0);
   MMSequentialEvidence.Render(c,"Validation/FinalWorld/final_linked_overview.png",1600,1000);
   c.orthographic=false;c.fieldOfView=50;c.transform.position=new Vector3(-950,900,-1650);c.transform.LookAt(new Vector3(-100,0,0));
   MMSequentialEvidence.Render(c,"Validation/FinalWorld/final_linked_oblique.png",1600,1000);
  } finally {UnityEngine.Object.DestroyImmediate(go);}
  result.Log("Final linked overview + oblique captured");
 }
}