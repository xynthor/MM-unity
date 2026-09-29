using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
internal class CommandScript:IRunCommand{
 public void Execute(ExecutionResult result){
  string[] scenes={"FreeHaven","MireOfTheDamned","FrozenHighlands","BootlegBay","SilverCove"};
  Directory.CreateDirectory("Validation/LinkedWaterAudit_20260920");
  var outL=new List<string>{"scene\tname\tenabled\tmesh\tmeshPath\ttris\tminx\tmaxx\tminz\tmaxz\tminy\tmaxy\tmaterial"};
  foreach(var sn in scenes){
   var sc=EditorSceneManager.OpenScene("Assets/Scenes/"+sn+".unity",OpenSceneMode.Single);
   foreach(var r in sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){
    string n=r.name.ToLowerInvariant();
    if(!(n.Contains("water")||n.Contains("ocean")||n.Contains("river")||n.Contains("lake")))continue;
    if(n.Contains("bottom")||n.Contains("seabed")||n.Contains("bathymetric"))continue;
    var mf=r.GetComponent<MeshFilter>();var m=mf?mf.sharedMesh:null;
    var b=r.bounds;
    outL.Add(string.Join("\t",new[]{sn,r.name,r.enabled.ToString(),m?m.name:"",m?AssetDatabase.GetAssetPath(m):"",m?(m.triangles.Length/3).ToString():"0",b.min.x.ToString("F2"),b.max.x.ToString("F2"),b.min.z.ToString("F2"),b.max.z.ToString("F2"),b.min.y.ToString("F3"),b.max.y.ToString("F3"),r.sharedMaterial?r.sharedMaterial.name:""}));
   }
  }
  File.WriteAllLines("Validation/LinkedWaterAudit_20260920/layers.tsv",outL);
  result.Log("WATER_LAYER_AUDIT rows="+(outL.Count-1));
 }
}