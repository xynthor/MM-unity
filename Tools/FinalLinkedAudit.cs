using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 bool Water(string n){return n.StartsWith("Water -")||n=="Internal Water - Smooth"||n=="Global Ocean Surface"||n=="Ocean + Central Lake Water"||n=="Unified Water Bottom"||n.StartsWith("Curved Seabed");}
 bool Bad(Renderer r){string s=(AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh:(r is SkinnedMeshRenderer sk?sk.sharedMesh:null))+"|"+r.name+"|"+r.transform.parent?.name).ToLowerInvariant();return s.Contains("searsia_lucida")||s.Contains("pine_00")||s.Contains("pine_d")||s.Contains("pine_c")||s.Contains("pine_b");}
 public void Execute(ExecutionResult result){
  var linked=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var root=linked.GetRootGameObjects().Single();var all=root.GetComponentsInChildren<Transform>(true);
  var ns=all.FirstOrDefault(t=>t.name.StartsWith("New Sorpigal - LINKED"));if(!ns)throw new Exception("Linked Sorpigal root missing");
  int nsMF=ns.GetComponentsInChildren<MeshFilter>(true).Length,nsR=ns.GetComponentsInChildren<Renderer>(true).Length,nsV=ns.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled&&r.gameObject.activeInHierarchy);
  int nsVeg=ns.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("ApprovedReplacement_")||t.name.StartsWith("GroveTree_")||t.name.StartsWith("SourceTree_"));
  int bad=all.SelectMany(t=>t.GetComponents<Renderer>()).Count(r=>r.enabled&&r.gameObject.activeInHierarchy&&Bad(r));
  var waters=all.SelectMany(t=>t.GetComponents<Renderer>()).Where(r=>Water(r.name)).ToArray();
  var rows=new List<string>{"name|enabled|matPath|matName|shader|y|mesh"};
  foreach(var r in waters){var m=r.sharedMaterial;var mf=r.GetComponent<MeshFilter>();rows.Add(r.name+"|"+r.enabled+"|"+AssetDatabase.GetAssetPath(m)+"|"+(m?m.name:"NULL")+"|"+(m&&m.shader?m.shader.name:"NULL")+"|"+r.bounds.center.y+"|"+AssetDatabase.GetAssetPath(mf?mf.sharedMesh:null));}
  var standalone=EditorSceneManager.OpenScene("Assets/Scenes/Regions/NewSorpigal.unity",OpenSceneMode.Additive);
  var sr=standalone.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);if(!sr)throw new Exception("Standalone Sorpigal root missing");
  int stMF=sr.GetComponentsInChildren<MeshFilter>(true).Length,stR=sr.GetComponentsInChildren<Renderer>(true).Length,stV=sr.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled&&r.gameObject.activeInHierarchy);
  int stVeg=sr.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("ApprovedReplacement_")||t.name.StartsWith("GroveTree_")||t.name.StartsWith("SourceTree_"));
  EditorSceneManager.CloseScene(standalone,true);EditorSceneManager.SetActiveScene(linked);
  Directory.CreateDirectory("Validation/FinalWorld");
  File.WriteAllText("Validation/FinalWorld/linked_parity.txt","linked_sorpigal_meshfilters="+nsMF+"\nstandalone_sorpigal_meshfilters="+stMF+"\nlinked_sorpigal_renderers="+nsR+"\nstandalone_sorpigal_renderers="+stR+"\nlinked_sorpigal_visible_renderers="+nsV+"\nstandalone_sorpigal_visible_renderers="+stV+"\nlinked_sorpigal_veg_roots="+nsVeg+"\nstandalone_sorpigal_veg_roots="+stVeg+"\nlinked_visible_rejected_renderers="+bad+"\nwater_renderers="+waters.Length+"\n");
  File.WriteAllLines("Validation/FinalWorld/linked_water_inventory.txt",rows);
  result.Log("Sorpigal parity MF "+nsMF+"/"+stMF+" R "+nsR+"/"+stR+" veg "+nsVeg+"/"+stVeg+" badVisible="+bad+" waters="+waters.Length);
 }
}