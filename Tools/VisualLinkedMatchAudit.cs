using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 const string Out="Validation/VisualRefinement";
 string P(Transform t){return t.parent?P(t.parent)+"/"+t.name:t.name;}
 public void Execute(ExecutionResult result){
  var rs=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();
  var paths=rs.Select(r=>new{r=r,p=P(r.transform)}).ToArray();
  var rows=new Dictionary<string,string>();
  foreach(var file in Directory.GetFiles(Out,"*_material_changes.txt")) foreach(var line in File.ReadAllLines(file)){var parts=line.Split('|');rows[parts[0]]=parts[1];}
  var miss=new List<string>();var multi=new List<string>();int one=0;
  foreach(var row in rows){int idx=row.Key.IndexOf('/');string root=row.Key.Substring(0,idx).Split(new[]{" - "},StringSplitOptions.None)[0];string rel=row.Key.Substring(idx);var m=paths.Where(x=>x.p.IndexOf("/"+root+" - ",StringComparison.OrdinalIgnoreCase)>=0&&x.p.EndsWith(rel,StringComparison.Ordinal)).ToArray();if(m.Length==1)one++;else if(m.Length==0)miss.Add(row.Key);else multi.Add(row.Key+"|"+m.Length);}
  File.WriteAllLines(Out+"/linked_match_missing.txt",miss);File.WriteAllLines(Out+"/linked_match_multiple.txt",multi);result.Log("rows="+rows.Count+" exact="+one+" missing="+miss.Count+" multi="+multi.Count);
 }
}