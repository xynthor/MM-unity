using System;using System.Linq;using UnityEngine;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name;
 public void Execute(ExecutionResult result){
  var ts=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  foreach(var t in ts.Where(t=>t.name=="Pine_A_LOD0"||t.name.StartsWith("Eco_GREEN_38_64")||t.name.Contains("Castle Ironfist"))) result.Log(P(t));
 }
}