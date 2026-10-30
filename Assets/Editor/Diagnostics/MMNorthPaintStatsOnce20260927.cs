using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMNorthPaintStatsOnce20260927{
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthPaintStatsOnce20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string f=V+"RUN_NORTH_PAINT_STATS.flag";if(!File.Exists(f))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(f);var sb=new StringBuilder();
  foreach(var n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
   foreach(bool north in new[]{false,true}){
    string path=north?"Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset":
      "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
    var d=AssetDatabase.LoadAssetAtPath<TerrainData>(path);if(!d){sb.AppendLine("MISSING "+path);continue;}
    int snow=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_Snow");
    int rock=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
    int green=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_Green");
    var a=d.GetAlphamaps(0,0,512,512);
    double sn=0,ro=0,gr=0;int count=0;
    for(int z=0;z<512;z+=4)for(int x=0;x<512;x+=4){
      sn+=a[z,x,snow];ro+=a[z,x,rock];gr+=a[z,x,green];count++;
    }
    double snNear=0,roNear=0,grNear=0;int c2=0;
    int z0=north?0:416,z1=north?96:512;
    for(int z=z0;z<z1;z+=2)for(int x=0;x<512;x+=2){
      snNear+=a[z,x,snow];roNear+=a[z,x,rock];grNear+=a[z,x,green];c2++;
    }
    sb.AppendLine(n+" "+(north?"EXT":"SRC")+" meanSnow="+(sn/count).ToString("F3")+
      " meanRock="+(ro/count).ToString("F3")+" meanGreen="+(gr/count).ToString("F3")+
      " joinBandSnow="+(snNear/c2).ToString("F3")+" joinBandRock="+(roNear/c2).ToString("F3")+
      " joinBandGreen="+(grNear/c2).ToString("F3"));
   }
  }
  File.WriteAllText(V+"north_paint_stats_20260927.txt",sb.ToString());
 }
}