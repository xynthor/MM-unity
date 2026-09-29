using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
internal class CommandScript:IRunCommand{
 public void Execute(ExecutionResult result){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
  var rs=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true));
  var lines=new List<string>{"region\tpath\trenderer\tactive\tenabled\tmaterial\tmatPath\tshader\tcolor"};
  foreach(var r in rs){
   string path=PathOf(r.transform); string low=path.ToLowerInvariant();
   if(!(low.Contains("water")||low.Contains("ocean")||low.Contains("sea")||low.Contains("lake")||low.Contains("river")))continue;
   var mat=r.sharedMaterial; string mp=mat?AssetDatabase.GetAssetPath(mat):""; string sh=mat&&mat.shader?mat.shader.name:"";
   string col="";
   if(mat){foreach(var p in new[]{"_ShallowColor","_MidColor","_DeepColor","_BaseColor","_Color"})if(mat.HasProperty(p)){var c=mat.GetColor(p);col+=p+"="+c.r.ToString("F3")+","+c.g.ToString("F3")+","+c.b.ToString("F3")+","+c.a.ToString("F3")+";";}}
   string region=RegionOf(r.transform);
   lines.Add(string.Join("\t",new[]{region,path,r.name,r.gameObject.activeInHierarchy.ToString(),r.enabled.ToString(),mat?mat.name:"<null>",mp,sh,col}));
  }
  Directory.CreateDirectory("Validation/LinkedWaterAudit_20260920");
  File.WriteAllLines("Validation/LinkedWaterAudit_20260920/current.tsv",lines);
  var groups=lines.Skip(1).Select(x=>x.Split('\t')).GroupBy(p=>p.Length>6?p[5]+"|"+p[6]+"|"+p[7]+"|"+p[8]:"bad").Select(g=>g.Key+"\tcount="+g.Count());
  File.WriteAllLines("Validation/LinkedWaterAudit_20260920/groups.txt",groups);
  result.Log("LINKED_WATER_AUDIT rows="+(lines.Count-1));
 }
 string PathOf(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
 string RegionOf(Transform t){while(t!=null){if(t.name.Contains("LINKED"))return t.name;t=t.parent;}return "<root>";}
}