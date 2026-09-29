using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMTempPlantAssetAudit {
 static readonly string[] P={
 "Assets/Environment/PolyHaven/Models/fern_02/fern_02_1k.fbx",
 "Assets/Environment/PolyHaven/Models/grass_medium_01/grass_medium_01_1k.fbx",
 "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Fern_var01_Prefab.prefab",
 "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Clover_01_Var1_Prefab.prefab"
 };
 static Bounds B(GameObject g){var r=g.GetComponentsInChildren<Renderer>(true);if(r.Length==0)return new Bounds();var b=r[0].bounds;for(int i=1;i<r.Length;i++)b.Encapsulate(r[i].bounds);return b;}
 [MenuItem("MMUnity/Validation/Temp Plant Asset Audit")] public static void Run(){
  Directory.CreateDirectory("Validation/SourceDecoration3D");
  var o=new List<string>{"asset,size_x,size_y,size_z,shaders"};
  foreach(var p in P){var s=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(!s){o.Add(p+",MISSING");continue;}var g=Object.Instantiate(s);g.hideFlags=HideFlags.HideAndDontSave;var b=B(g);var sh=g.GetComponentsInChildren<Renderer>(true).SelectMany(x=>x.sharedMaterials).Where(m=>m).Select(m=>m.shader?m.shader.name:"NULL").Distinct();o.Add(p+","+$"{b.size.x:F3},{b.size.y:F3},{b.size.z:F3},\""+string.Join(";",sh)+"\"");Object.DestroyImmediate(g);}
  File.WriteAllLines("Validation/SourceDecoration3D/plant_asset_audit.csv",o);
 }
}