using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMFinalNorthSplatProfileOnce20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMFinalNorthSplatProfileOnce20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_FINAL_NORTH_SPLAT_PROFILE.flag"))return;
  File.Delete(V+"RUN_FINAL_NORTH_SPLAT_PROFILE.flag");
  var b=new StringBuilder();
  try{
   foreach(var n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
    var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var linked=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    var north=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset");
    foreach(var pair in new[]{new{d=source,label="CANONICAL"},new{d=linked,label="LINKED"},new{d=north,label="NORTH"}}){
     int[] rows=pair.label=="NORTH"?new[]{0,16,48,96,160,256,400,511}:new[]{0,128,256,352,416,464,496,511};
     int snow=Array.FindIndex(pair.d.terrainLayers,t=>t&&t.name=="Realistic_Snow");
     int ash=Array.FindIndex(pair.d.terrainLayers,t=>t&&t.name=="Realistic_Volcanic");
     int green=Array.FindIndex(pair.d.terrainLayers,t=>t&&t.name=="Realistic_Green");
     int light=Array.FindIndex(pair.d.terrainLayers,t=>t&&t.name=="Realistic_LightGreen");
     int arid=Array.FindIndex(pair.d.terrainLayers,t=>t&&t.name=="Realistic_Arid");
     var alphas=pair.d.GetAlphamaps(0,0,512,512);
     for(int i=0;i<rows.Length;i++){
      int z=rows[i];float s0=0,r0=0,g0=0,a0=0;int count=0;
      for(int x=32;x<480;x++){
       s0+=alphas[z,x,snow];r0+=alphas[z,x,ash];
       g0+=alphas[z,x,green]+alphas[z,x,light];a0+=alphas[z,x,arid];count++;
      }
      b.AppendLine(n+" "+pair.label+" z="+z+" SNOW="+(s0/count).ToString("F3")+
       " ASH="+(r0/count).ToString("F3")+" GREEN="+(g0/count).ToString("F3")+
       " ARID="+(a0/count).ToString("F3"));
     }
    }
   }
  }catch(Exception e){b.AppendLine("ERROR "+e);}
  File.WriteAllText(V+"final_north_splat_profile_20260926.txt",b.ToString());
 }
}
