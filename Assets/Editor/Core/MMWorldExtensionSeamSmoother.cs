using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWorldExtensionSeamSmoother {
 const string G="Assets/World/WorldExtensions/Generated/";
 const float H=320f;
 enum Side { North, West, South }
 sealed class Seam {
  public string ext,src; public Side side; public int band;
  public Seam(string e,string s,Side x,int b=56){ext=e;src=s;side=x;band=b;}
 }
 static readonly Seam[] Seams={
  new Seam("SweetWater_North","SweetWater",Side.North),
  new Seam("Kriegspire_North","Kriegspire",Side.North),
  new Seam("FrozenHighlands_North","FrozenHighlands",Side.North),
  new Seam("SilverCove_North","SilverCove",Side.North),
  new Seam("EelInfestedWaters_North","EelInfestedWaters",Side.North),
  new Seam("ParadiseValley_West","ParadiseValley",Side.West),
  new Seam("HermitsIsle_West","HermitsIsle",Side.West),
  new Seam("HermitsIsle_South","HermitsIsle",Side.South),
  new Seam("Dragonsand_South","Dragonsand",Side.South),
  new Seam("MireOfTheDamned_South","MireOfTheDamned",Side.South),
  new Seam("CastleIronfist_South","CastleIronfist",Side.South),
  new Seam("ArchipelagoOfTheAncients","NewSorpigal",Side.South,64)
 };
 static float Hermite(float y0,float y1,float m0,float m1,float t,float span){
  float t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1)*y0+(t3-2*t2+t)*span*m0+
         (-2*t3+3*t2)*y1+(t3-t2)*span*m1;
 }
 static float ClampTangent(float d)=>Mathf.Clamp(d,-.0047f,.0047f);

 static TerrainData Src(string n){
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(
   "Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
  if(!t)throw new Exception("Source TerrainData missing "+n);
  return t;
 }
 static TerrainData Ext(string n){
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"Terrain.asset");
  if(!t)throw new Exception("Extension TerrainData missing "+n);
  return t;
 }
 static int[] Match(TerrainLayer[] dst,TerrainLayer[] src){
  return dst.Select(d=>{
   if(!d)return -1;
   int exact=Array.FindIndex(src,s=>s&&s.name==d.name);
   if(exact>=0)return exact;
   string n=d.name.ToLowerInvariant();
   string term=n.Contains("light")?"lightgreen":n.Contains("green")||n.Contains("grass")?"green":
    n.Contains("snow")?"snow":n.Contains("sand")||n.Contains("desert")?"desert":
    n.Contains("rock")||n.Contains("volcan")?"volcan":n.Contains("dirt")||n.Contains("arid")?"arid":"";
   if(term=="")return -1;
   return Array.FindIndex(src,s=>s&&s.name.ToLowerInvariant().Contains(term));
  }).ToArray();
 }
 static float Smooth(float a,float b,float x){
  float t=Mathf.Clamp01((x-a)/Mathf.Max(.0001f,b-a));return t*t*(3f-2f*t);
 }
 static void BlendAlpha(Seam s,TerrainData src,TerrainData ext){
  int A=Mathf.Min(32,ext.alphamapResolution/8);
  var sa=src.GetAlphamaps(0,0,src.alphamapResolution,src.alphamapResolution);
  var ea=ext.GetAlphamaps(0,0,ext.alphamapResolution,ext.alphamapResolution);
  int n=ext.alphamapResolution,L=ext.alphamapLayers;
  var map=Match(ext.terrainLayers,src.terrainLayers);
  for(int along=0;along<n;along++)for(int q=0;q<=A;q++){
   int ex=along,ez=along,sx=along,sz=along;
   if(s.side==Side.North){ex=along;ez=q;sx=along;sz=src.alphamapResolution-1;}
   else if(s.side==Side.West){ex=n-1-q;ez=along;sx=0;sz=along;}
   else {ex=along;ez=n-1-q;sx=along;sz=0;}
   float f=1f-Smooth(0f,A,q),sum=0f;
   for(int k=0;k<L;k++){
    if(map[k]>=0)ea[ez,ex,k]=Mathf.Lerp(ea[ez,ex,k],sa[sz,sx,map[k]],f);
    sum+=ea[ez,ex,k];
   }
   if(sum>.00001f)for(int k=0;k<L;k++)ea[ez,ex,k]/=sum;
  }
  ext.SetAlphamaps(0,0,ea);
 }

 static string SmoothOne(Seam s){
  var src=Src(s.src);var ext=Ext(s.ext);
  if(src.heightmapResolution!=513||ext.heightmapResolution!=513)
   throw new Exception("Unexpected height resolution "+s.ext);
  var sh=src.GetHeights(0,0,513,513);
  var eh=ext.GetHeights(0,0,513,513);
  float maxSourceSlope=0f;
  for(int i=0;i<513;i++){
   float m=s.side==Side.North?sh[512,i]-sh[511,i]:
           s.side==Side.West?sh[i,0]-sh[i,1]:
           sh[0,i]-sh[1,i];
   maxSourceSlope=Mathf.Max(maxSourceSlope,Mathf.Abs(m)*H);
  }
  int B=s.band;
  if(maxSourceSlope>3f)B=Mathf.Min(B,12);
  else if(maxSourceSlope>1f)B=Mathf.Min(B,24);
  else if(maxSourceSlope>.5f)B=Mathf.Min(B,36);
  float before=0f,after=0f,maxMove=0f;
  for(int i=0;i<513;i++){
   float y0,m0,y1,m1,start;
   if(s.side==Side.North){
    y0=sh[512,i];m0=sh[512,i]-sh[511,i];
    y1=eh[B,i];m1=ClampTangent(eh[B+1,i]-eh[B,i]);start=y0+m0;
    before=Mathf.Max(before,Mathf.Abs((eh[1,i]-eh[0,i])-m0)*H);
    float old0=eh[0,i],old1=eh[1,i];eh[0,i]=y0;eh[1,i]=start;
    maxMove=Mathf.Max(maxMove,Mathf.Max(Mathf.Abs(y0-old0),Mathf.Abs(start-old1))*H);
    for(int q=2;q<=B;q++){
     float old=eh[q,i],t=(q-1f)/(B-1f);
     float v=Hermite(start,y1,m0,m1,t,B-1);
     eh[q,i]=Mathf.Clamp01(v);maxMove=Mathf.Max(maxMove,Mathf.Abs(eh[q,i]-old)*H);
    }
    after=Mathf.Max(after,Mathf.Abs((eh[1,i]-eh[0,i])-m0)*H);
   }else if(s.side==Side.West){
    y0=sh[i,0];m0=sh[i,0]-sh[i,1];int xB=512-B;
    y1=eh[i,xB];m1=ClampTangent(eh[i,xB-1]-eh[i,xB]);start=y0+m0;
    before=Mathf.Max(before,Mathf.Abs((eh[i,511]-eh[i,512])-m0)*H);
    float old0=eh[i,512],old1=eh[i,511];eh[i,512]=y0;eh[i,511]=start;
    maxMove=Mathf.Max(maxMove,Mathf.Max(Mathf.Abs(y0-old0),Mathf.Abs(start-old1))*H);
    for(int q=2;q<=B;q++){
     int x=512-q;float old=eh[i,x],t=(q-1f)/(B-1f);
     float v=Hermite(start,y1,m0,m1,t,B-1);
     eh[i,x]=Mathf.Clamp01(v);maxMove=Mathf.Max(maxMove,Mathf.Abs(eh[i,x]-old)*H);
    }
    after=Mathf.Max(after,Mathf.Abs((eh[i,511]-eh[i,512])-m0)*H);
   }else{
    y0=sh[0,i];m0=sh[0,i]-sh[1,i];int zB=512-B;
    y1=eh[zB,i];m1=ClampTangent(eh[zB-1,i]-eh[zB,i]);start=y0+m0;
    before=Mathf.Max(before,Mathf.Abs((eh[511,i]-eh[512,i])-m0)*H);
    float old0=eh[512,i],old1=eh[511,i];eh[512,i]=y0;eh[511,i]=start;
    maxMove=Mathf.Max(maxMove,Mathf.Max(Mathf.Abs(y0-old0),Mathf.Abs(start-old1))*H);
    for(int q=2;q<=B;q++){
     int z=512-q;float old=eh[z,i],t=(q-1f)/(B-1f);
     float v=Hermite(start,y1,m0,m1,t,B-1);
     eh[z,i]=Mathf.Clamp01(v);maxMove=Mathf.Max(maxMove,Mathf.Abs(eh[z,i]-old)*H);
    }
    after=Mathf.Max(after,Mathf.Abs((eh[511,i]-eh[512,i])-m0)*H);
   }
  }
  ext.SetHeights(0,0,eh);BlendAlpha(s,src,ext);EditorUtility.SetDirty(ext);
  return s.ext+",source="+s.src+",side="+s.side+",band_m="+B+
   ",source_max_slope_m_per_m="+maxSourceSlope.ToString("F3")+
   ",slope_jump_before="+before.ToString("F4")+
   ",after="+after.ToString("F4")+",max_height_move_m="+maxMove.ToString("F3");
 }

 [MenuItem("MMUnity/World/Smooth Original To Extension Seams")]
 public static void ApplyAll(){
  var rows=new List<string>();
  foreach(var s in Seams)rows.Add(SmoothOne(s));
  // Dragon shares Sweet Water / Paradise boundaries; retain its dedicated joins.
  MMDragonWestEdgeJoin.Apply();
  // Re-lock extension-to-extension borders without touching canonical terrain.
  MMWorldExtensionInternalSeamLock.Apply();
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/original_to_extension_smoothing.csv",rows);
  Debug.Log("ORIGINAL_TO_EXTENSION_SMOOTHING_DONE seams="+rows.Count);
 }
}
