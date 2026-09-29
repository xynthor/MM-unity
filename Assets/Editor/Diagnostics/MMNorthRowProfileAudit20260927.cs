using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthRowProfileAudit20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthRowProfileAudit20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_ROW_PROFILE_AUDIT.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var lines=new List<string>();
  try{
   foreach(var n in N){
    var s=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    var e=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
    if(!s||!e)throw new Exception("Missing "+n);
    lines.Add(n);
    foreach(var spec in new[]{("S",s,new[]{480,500,506,510,511}),("E",e,new[]{0,1,5,12,32,64,96})}){
     var a=spec.Item2.GetAlphamaps(0,0,512,512);
     foreach(int z in spec.Item3){
      var m=new double[7];
      for(int x=8;x<504;x++)for(int k=0;k<7;k++)m[k]+=a[z,x,k];
      string row=spec.Item1+" z="+z;
      for(int k=0;k<7;k++)row+=" "+spec.Item2.terrainLayers[k].name+"="+(m[k]/496.0).ToString("F4");
      lines.Add(row);
     }
    }
   }
   File.WriteAllLines(V+"north_row_profile_20260927.txt",lines);
  }catch(Exception e){File.WriteAllText(V+"north_row_profile_20260927.txt","FAIL "+e);}
 }
}