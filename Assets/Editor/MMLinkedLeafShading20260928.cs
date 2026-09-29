using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
public static class MMLinkedLeafShading20260928 {
 public static void Apply(Scene scene){
  if(scene.name!="Enroth_Linked_OpenWorld"||!File.Exists("Backups/BeforeLinkedLeafShading_20260928/manifest.json"))throw new Exception("Linked scene and backup required");
  var shader=Shader.Find("MMUnity/Linked Two Sided Leaves");if(!shader||!shader.isSupported)throw new Exception("Leaf shader unavailable");
  var mats=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).SelectMany(r=>r.sharedMaterials).Where(m=>m&&AssetDatabase.GetAssetPath(m).StartsWith("Assets/World/WorldExtensions/Generated/LinkedFoliageCoverage/")).Distinct().ToArray();
  foreach(var m in mats){m.shader=shader;m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.18f);m.enableInstancing=true;EditorUtility.SetDirty(m);}
  AssetDatabase.SaveAssets();File.WriteAllText("Validation/EdgeGrid20260923/Takeover_LeafShading_Result.txt","twoSidedMaterials="+mats.Length+" canonicalWrites=0\n");
 }
}
