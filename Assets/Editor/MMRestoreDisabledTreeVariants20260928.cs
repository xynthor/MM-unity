using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMRestoreDisabledTreeVariants20260928
{
 static string Relative(Transform t,Transform root){string p=t.name;while(t.parent&&t.parent!=root){t=t.parent;p=t.name+"/"+p;}return p;}
 public static void Run(bool apply)
 {
  var linked=SceneManager.GetActiveScene();if(linked.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity")throw new Exception("Linked scene required");
  if(apply&&!File.Exists("Backups/BeforeDisabledTreeRestore_20260928/manifest.json"))throw new Exception("Backup missing");
  string[] scenes={"SweetWater_SourceGrid","Kriegspire_SourceGrid","FrozenHighlands_SourceGrid","SilverCove_SourceGrid","EelInfestedWaters_SourceGrid","ParadiseValley_SourceGrid","Blackshire_SourceGrid","FreeHaven_SourceGrid","BootlegBay_SourceGrid","MistyIslands_SourceGrid","HermitsIsle_SourceGrid","Dragonsand_SourceGrid","MireOfTheDamned_SourceGrid","CastleIronfist_SourceGrid","NewSorpigal_OpenWorld"};
  string[] names={"Sweet Water","Kriegspire","White Cap / Frozen Highlands","Silver Cove","Eel Infested Waters","Paradise Valley","Blackshire","Free Haven","Bootleg Bay","Misty Islands","Hermits Isle","Dragonsand","Mire of the Damned","Castle Ironfist","New Sorpigal"};
  var transforms=linked.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var targets=new List<LODGroup>();var rows=new List<string>();
  for(int i=0;i<scenes.Length;i++){
   var clone=transforms.Single(t=>t.name==names[i]+(i==13?" - LINKED ADJUSTABLE":" - LINKED REFERENCE"));
   var source=EditorSceneManager.OpenScene("Assets/Scenes/"+scenes[i]+".unity",OpenSceneMode.Additive);
   try{
    var root=source.GetRootGameObjects().First(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
    foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.enabled&&!r.GetComponentInParent<LODGroup>())){
     string relative=Relative(r.transform,root.transform);var target=clone.Find(relative);if(!target)continue;
     var group=target.GetComponent<LODGroup>();if(!group||!target.Find("Linked distance LOD 2"))continue;
     var a=r.GetComponent<MeshFilter>();var b=target.GetComponent<MeshFilter>();if(!a||!b||a.sharedMesh!=b.sharedMesh)continue;
     if(!group.enabled)continue;
     targets.Add(group);rows.Add(names[i]+"/"+relative);
    }
   }finally{EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(linked);}
  }
  if(apply){foreach(var group in targets){group.enabled=false;foreach(var lod in group.GetLODs())foreach(var r in lod.renderers)if(r)r.enabled=false;}EditorSceneManager.MarkSceneDirty(linked);EditorSceneManager.SaveScene(linked);}
  rows.Insert(0,"matchingUnwantedVariantGroups="+targets.Count+" applied="+apply+" originalScenesModified=0 placementsModified=0");
  File.WriteAllLines("Validation/EdgeGrid20260923/Resume_DisabledVariants_"+(apply?"Applied":"Audit")+".txt",rows);Debug.Log(rows[0]);
 }
}
