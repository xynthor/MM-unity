using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
internal class CommandScript : IRunCommand
{
 public void Execute(ExecutionResult result) {
  var s=SceneManager.GetActiveScene();
  if(s.path!="Assets/Scenes/NewSorpigal_OpenWorld.unity"||s.isDirty||EditorApplication.isPlaying)throw new Exception("Requires clean Sorpigal");
  string tag="water_compare_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
  string backup="Backups/"+tag; Directory.CreateDirectory(backup); File.Copy(s.path,backup+"/NewSorpigal_OpenWorld.unity");
  var ts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var positions=ts.ToDictionary(t=>t.GetEntityId(),t=>t.position);
  File.WriteAllLines(backup+"/positions.csv",ts.Select(t=>t.GetEntityId()+","+t.position.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+t.position.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
  var old=ts.Single(t=>t.name=="Water - Smoothed Original River Pond Shore").GetComponent<Renderer>();
  var bottom=ts.Single(t=>t.name=="Unified Water Bottom").GetComponent<Renderer>();
  var newer=ts.Single(t=>t.name=="Internal Water - Smooth").GetComponent<Renderer>();
  bool a=old.enabled,b=bottom.enabled,c=newer.enabled;
  try {
   bottom.enabled=false; MMSequentialEvidence.CaptureSorpigal(s,tag+"/without_overlay");
   old.enabled=false; MMSequentialEvidence.CaptureSorpigal(s,tag+"/single_new_water");
   old.enabled=true;newer.enabled=false; MMSequentialEvidence.CaptureSorpigal(s,tag+"/single_old_water");
  } finally {old.enabled=a;bottom.enabled=b;newer.enabled=c;}
  int moved=ts.Count(t=>t.position!=positions[t.GetEntityId()]);
  File.WriteAllText("Validation/SequentialRepair/"+tag+"/audit.txt","positions_changed="+moved+" scene_dirty="+s.isDirty+" saved=false");
  result.Log("Comparison: "+tag+" positions_changed="+moved+" scene_dirty="+s.isDirty);
 }
}

