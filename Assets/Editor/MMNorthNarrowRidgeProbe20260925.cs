using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthNarrowRidgeProbe20260925{
 const string V="Validation/EdgeGrid20260923/",G="Assets/World/WorldExtensions/Generated/";
 static MMNorthNarrowRidgeProbe20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_NARROW_RIDGE_PROBE.flag"))return;
  File.Delete(V+"RUN_NORTH_NARROW_RIDGE_PROBE.flag");
  var lines=new List<string>();
  foreach(var n in new[]{"SweetWater","SilverCove"}){
   var o=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
   var c=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
   if(!o||!c){lines.Add(n+" MISSING");continue;}
   var a=o.GetHeights(0,0,513,513);var b=c.GetHeights(0,0,513,513);
   int[] xs=n=="SweetWater"?new[]{344,358,364,376,384,400,452,476,492}:
    new[]{17,24,45,58,78,90,94,97,99,103,110,120};
   int[] zz={400,440,464,472,480,484,488,492,496,500,504,508,510,511,512};
   foreach(int x in xs){
    string line=n+" x="+x+" src=";
    foreach(int z in zz)line+=z+":"+(-24f+320f*a[z,x]).ToString("F1")+",";
    line+=" linked=";
    foreach(int z in zz)line+=z+":"+(-24f+320f*b[z,x]).ToString("F1")+",";
    lines.Add(line);
   }
  }
  File.WriteAllLines(V+"north_narrow_ridge_height_profiles_20260925.txt",lines);
 }
}

// wake requested read-only ridge profile diagnostic
