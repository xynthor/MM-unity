using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthMaterialRowAuditOnce20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthMaterialRowAuditOnce20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_MATERIAL_ROW_AUDIT.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){
   EditorApplication.delayCall+=Run;return;
  }
  File.Delete(V+"RUN_NORTH_MATERIAL_ROW_AUDIT.flag");
  var lines=new List<string>();
  try {
   lines.Add("SCENE="+SceneManager.GetActiveScene().path);
   foreach(var name in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
    var src=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/"+name+"/Generated/"+name+"Terrain.asset");
    var clone=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+name+"_LinkedNorthProfile.asset");
    var north=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/WorldExtensions/Generated/"+name+"_NorthTerrain.asset");
    foreach(var pair in new[]{new{label="ORIGINAL",d=src},new{label="LINKED",d=clone},
                               new{label="NORTH",d=north}}){
     var t=pair.d;
     if(!t)throw new Exception(name+" missing "+pair.label);
     var a=t.GetAlphamaps(0,0,512,512);
     int snow=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Snow");
     int rock=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
     int green=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Green");
     int light=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_LightGreen");
     int soil=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Arid");
     foreach(int z in new[]{0,32,64,128,192,256,320,384,448,480,511}){
      double[] m=new double[5];int count=0;
      for(int x=40;x<472;x++){
       m[0]+=a[z,x,snow];m[1]+=a[z,x,rock];m[2]+=a[z,x,green];
       m[3]+=a[z,x,light];m[4]+=a[z,x,soil];count++;
      }
      lines.Add(name+" "+pair.label+" z="+z+" SN="+(m[0]/count).ToString("F3")+
       " RK="+(m[1]/count).ToString("F3")+" GR="+(m[2]/count).ToString("F3")+
       " LG="+(m[3]/count).ToString("F3")+" SO="+(m[4]/count).ToString("F3"));
     }
    }
   }
  }catch(Exception ex){lines.Add("ERROR "+ex);}
  File.WriteAllLines(V+"north_material_row_audit_20260926.txt",lines);
 }
}

// verify new photo confidence after rebuild
