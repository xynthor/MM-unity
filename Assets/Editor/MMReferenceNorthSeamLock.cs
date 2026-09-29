using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMReferenceNorthSeamLock {
 const string EDGE="Assets/World/WorldExtensions/Generated/";
 static readonly string[] Sources={"SweetWater","Kriegspire","FrozenHighlands","SilverCove","EelInfestedWaters"};
 static readonly string[] Extensions={"SweetWater_North","Kriegspire_North","FrozenHighlands_North","SilverCove_North","EelInfestedWaters_North"};
 static float Smooth(float a,float b,float n){
  var t=Mathf.Clamp01((n-a)/(b-a));return t*t*(3f-2f*t);
 }
 public static void Apply(List<string> report){
  foreach(var name in Sources) {
   int col=Array.IndexOf(Sources,name);
   string sp="Assets/World/"+name+"/Generated/"+name+"Terrain.asset";
   string ep=EDGE+Extensions[col]+"Terrain.asset";
   var src=AssetDatabase.LoadAssetAtPath<TerrainData>(sp);
   var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(ep);
   if(!src||!ext||src.heightmapResolution!=513||ext.heightmapResolution!=513)
    throw new Exception("North seam source missing "+sp+" or "+ep);
   var sh=src.GetHeights(0,512,513,1);var eh=ext.GetHeights(0,0,513,513);
   float before=0f;
   for(int x=0;x<513;x++){
    float delta=sh[0,x]-eh[0,x];
    before=Mathf.Max(before,Mathf.Abs(delta)*320f);
    for(int z=0;z<=16;z++){
      float fade=1f-Smooth(0f,16f,z);
      eh[z,x]+=delta*fade;
    }
   }
   ext.SetHeights(0,0,eh);
   var sa=src.GetAlphamaps(0,511,512,1);
   var ea=ext.GetAlphamaps(0,0,512,512);
   int L=ext.alphamapLayers;
   int[] ids=ext.terrainLayers.Select(layer=>Array.FindIndex(
     src.terrainLayers,sl=>sl&&layer&&sl.name==layer.name)).ToArray();
   for(int x=0;x<512;x++){
    var delta=new float[L];
    for(int k=0;k<L;k++)if(ids[k]>=0)
       delta[k]=sa[0,x,ids[k]]-ea[0,x,k];
    for(int z=0;z<=16;z++){
      float fade=1f-Smooth(0f,16f,z),sum=0f;
      for(int k=0;k<L;k++){
        ea[z,x,k]=Mathf.Max(0f,ea[z,x,k]+delta[k]*fade);
        sum+=ea[z,x,k];
      }
      if(sum>0f)for(int k=0;k<L;k++)ea[z,x,k]/=sum;
    }
   }
   ext.SetAlphamaps(0,0,ea);
   EditorUtility.SetDirty(ext);
   float after=0f;
   for(int x=0;x<513;x++)
      after=Mathf.Max(after,Mathf.Abs(sh[0,x]-eh[0,x])*320f);
   report.Add($"NORTH_LOCK,{col},{name},before_m={before:F5},after_m={after:F5}");
   if(after>.02f)throw new Exception("North edge not locked "+name);
  }
  AssetDatabase.SaveAssets();
 }
 [MenuItem("MMUnity/World/Relock North Reference Seams")]
 public static void RelockOnly(){
  var lines=new List<string>();
  Apply(lines);
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/north_seam_relock.csv",lines);
  Debug.Log("NORTH_REFERENCE_RELOCK_DONE "+lines.Count);
 }
}
