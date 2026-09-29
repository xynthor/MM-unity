using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
// Match northern generated Terrain renderer settings to the immediately south linked source.
// Prevents a visible LOD/basemap break at the row boundary.
public static class MMNorthRendererMatch20260927 {
 const string V="Validation/EdgeGrid20260923/";
 public static void Apply(){
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity")
   throw new Exception("Linked world required for north renderer match");
  var ts=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
  var names=new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
  var lines=new System.Collections.Generic.List<string>();
  foreach(var n in names){
   Terrain src=null,ext=null;
   foreach(var t in ts){
    var d=t.terrainData?t.terrainData.name:"";
    if(d==n+"_LinkedNorthProfile")src=t;
    if(d==n+"_NorthTerrain")ext=t;
   }
   if(!src||!ext)throw new Exception("Missing renderer pair "+n);
   ext.heightmapPixelError=src.heightmapPixelError;
   ext.basemapDistance=src.basemapDistance;
   ext.drawInstanced=src.drawInstanced;
   ext.detailObjectDistance=src.detailObjectDistance;
   ext.detailObjectDensity=src.detailObjectDensity;
   ext.shadowCastingMode=src.shadowCastingMode;
   ext.reflectionProbeUsage=src.reflectionProbeUsage;
   ext.allowAutoConnect=src.allowAutoConnect;
   ext.groupingID=src.groupingID;
   EditorUtility.SetDirty(ext);
   lines.Add(n+" pixelError="+ext.heightmapPixelError+
    " basemap="+ext.basemapDistance+
    " drawInstanced="+ext.drawInstanced+
    " detailDistance="+ext.detailObjectDistance+
    " groupingID="+ext.groupingID);
  }
  File.WriteAllLines(V+"north_renderer_match_20260927.txt",lines);
 }
}
