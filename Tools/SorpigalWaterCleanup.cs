using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
internal class CommandScript : IRunCommand {
 public void Execute(ExecutionResult result) {
  var s=SceneManager.GetActiveScene();
  if(s.path!="Assets/Scenes/Regions/NewSorpigal.unity"||s.isDirty||EditorApplication.isPlaying)throw new Exception("Requires clean Sorpigal");
  var tag="water_cleanup_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");var dir="Backups/"+tag;Directory.CreateDirectory(dir);File.Copy(s.path,dir+"/NewSorpigal.unity");
  var ts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var before=ts.ToDictionary(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),t=>t.position);
  File.WriteAllLines(dir+"/positions.csv",before.Select(k=>k.Key+","+k.Value.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+k.Value.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
  long removed=0;int renderers=0;
  foreach(var name in new[]{"Unified Water Bottom","Internal Water - Smooth"}) {
   var r=ts.Single(t=>t.name==name).GetComponent<Renderer>(); if(!r.enabled)continue;
   var mesh=r.GetComponent<MeshFilter>().sharedMesh;
   for(int i=0;i<mesh.subMeshCount;i++)removed+=(long)mesh.GetIndexCount(i)/3;
   result.RegisterObjectModification(r);Undo.RecordObject(r,"Remove redundant Sorpigal shoreline renderers");r.enabled=false;EditorUtility.SetDirty(r);renderers++;
  }
  if(ts.Any(t=>t.position!=before[GlobalObjectId.GetGlobalObjectIdSlow(t).ToString()]))throw new Exception("Position changed; refusing save");
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");
  s=EditorSceneManager.OpenScene(s.path,OpenSceneMode.Single);
  var after=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToDictionary(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),t=>t.position);
  int changed=before.Count(k=>!after.ContainsKey(k.Key)||after[k.Key]!=k.Value);
  if(changed!=0||after.Count!=before.Count)throw new Exception("Saved position audit failed");
  MMSequentialEvidence.CaptureSorpigal(s,tag);
  File.WriteAllText("Validation/SequentialRepair/"+tag+"/audit.txt","transforms="+before.Count+" positions_changed="+changed+" renderers_disabled="+renderers+" rendered_triangles_removed="+removed+" triangles_added=0");
  result.Log(tag+" positions_changed="+changed+" renderers_disabled="+renderers+" rendered_triangles_removed="+removed+" triangles_added=0");
 }
}
