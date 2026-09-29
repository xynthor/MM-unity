using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
internal class CommandScript:IRunCommand{
 public void Execute(ExecutionResult result){
  foreach(var sn in new[]{"FreeHaven_SourceGrid","MireOfTheDamned_SourceGrid","FrozenHighlands_SourceGrid"}){
   var s=EditorSceneManager.OpenScene("Assets/Scenes/"+sn+".unity",OpenSceneMode.Single);
   var r=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).FirstOrDefault(x=>x.name=="Internal Water - Smooth");
   if(!r){result.Log(sn+" NO Internal Water");continue;}
   var mf=r.GetComponent<MeshFilter>();var m=mf?mf.sharedMesh:null;
   result.Log(sn+" | mesh="+(m?m.name:"NULL")+" | path="+(m?AssetDatabase.GetAssetPath(m):"")+" | y="+r.transform.position.y+" | mat="+(r.sharedMaterial?r.sharedMaterial.name:"NULL"));
  }
 }
}