using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthTerrainRendererAudit20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthTerrainRendererAudit20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_RENDERER_AUDIT.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var lines=new List<string>();
  try{
   var sc=SceneManager.GetActiveScene();
   var ts=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
    .Where(t=>Mathf.Abs(t.transform.position.z-256f)<1f||Mathf.Abs(t.transform.position.z-768f)<1f)
    .OrderBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x).ToArray();
   foreach(var t in ts){
    var p=t.transform.position;
    lines.Add(t.name+" pos=("+p.x.ToString("F0")+","+p.z.ToString("F0")+")"+
      " pixelError="+t.heightmapPixelError+
      " baseMapDistance="+t.basemapDistance+
      " drawInstanced="+t.drawInstanced+
      " groupingID="+t.groupingID+
      " allowAutoConnect="+t.allowAutoConnect+
      " shadow="+t.shadowCastingMode+
      " material="+(t.materialTemplate?t.materialTemplate.name:"BUILTIN")+
      " data="+t.terrainData.name);
   }
  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"north_terrain_renderer_audit_20260926.txt",lines);
 }
}
