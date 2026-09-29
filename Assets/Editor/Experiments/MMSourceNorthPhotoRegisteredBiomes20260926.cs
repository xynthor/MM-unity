using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
// Align the north end of the linked-only ORIGINAL maps with the actual
// reference-photo forest/rock/snow boundary (image y=158..308).
// Standalone canonical source scenes and source TerrainData are never painted.
public static class MMSourceNorthPhotoRegisteredBiomes20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Targets=V+"SourceNorthPhotoRegistration_20260926/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeLinkedNorthSourcePhotoRegistration_20260926\manifest.json";
 static float S(float a,float b,float x){
  float v=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));
  return v*v*(3f-2f*v);
 }
 static Color32[] Load(string name){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  string path=Targets+name+"_SourcePhotoBiomes.png";
  if(!File.Exists(path)||!t.LoadImage(File.ReadAllBytes(path))||
     t.width!=513||t.height!=513)throw new Exception("Missing photo registration "+name);
  var pixels=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return pixels;
 }
 static int Find(TerrainData d,string name){
  int k=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_"+name);
  if(k<0)throw new Exception("Native material "+name+" missing");
  return k;
 }
 public static void Apply(string n,TerrainData original,TerrainData linked,TerrainData north){
  if(!File.Exists(Backup))throw new Exception("Pre-paint backup required");
  if(original.alphamapResolution!=512||linked.alphamapResolution!=512||
     north.alphamapResolution!=512||original.alphamapLayers!=7||
     linked.alphamapLayers!=7||north.alphamapLayers!=7)
   throw new Exception(n+" expected seven original reference materials");
  for(int k=0;k<7;k++)
   if(original.terrainLayers[k]!=linked.terrainLayers[k])
    throw new Exception(n+" original northern copy material mismatch");
  var mask=Load(n);var south=linked.GetAlphamaps(0,0,512,512);
  var ext=north.GetAlphamaps(0,0,512,512);
  var h=original.GetHeights(0,0,513,513);
  var nh=north.GetHeights(0,0,513,1);
  int snow=Find(original,"Snow"),rock=Find(original,"Volcanic"),
      green=Find(original,"Green"),light=Find(original,"LightGreen"),
      arid=Find(original,"Arid"),road=Find(original,"RoadOverlay");
  int coastProtected=0,columns=0,sourcePixels=0;float worstAlphaGap=0;
  double oldSnow=0,newSnow=0;
  var targets=new float[512,7];var active=new bool[512];
  for(int x=0;x<512;x++){
   float sea=-24f+320f*h[512,x],nsea=-24f+320f*nh[0,x];
   float side=S(0f,26f,x)*S(0f,26f,511f-x);
   if(sea<.2f||nsea<.2f||side<.001f){coastProtected++;continue;}
   var p=mask[511*513+x];
   float snowPhoto=p.r/255f,rockPhoto=p.g/255f,
     greenPhoto=p.b/255f,soilPhoto=p.a/255f;
   float confidence=snowPhoto+rockPhoto+greenPhoto+soilPhoto;
   if(confidence<.12f){coastProtected++;continue;}
   float ns=.85f*snowPhoto,nr=1.20f*rockPhoto,
     ng=1.85f*greenPhoto,na=.96f*soilPhoto;
   float sum=ns+nr+ng+na;
   if(sum<.08f)continue;
   var target=new float[7];
   target[snow]=ns/sum;target[rock]=nr/sum;
   target[green]=.62f*ng/sum;target[light]=.38f*ng/sum;
   target[arid]=na/sum;
   float roadKeep=Mathf.Clamp01(south[511,x,road]*1.5f);
   float joinStrength=.66f*side*S(.10f,.38f,confidence)*(1f-.62f*roadKeep);
   for(int k=0;k<7;k++)
    targets[x,k]=Mathf.Lerp(south[511,x,k],target[k],joinStrength);
   active[x]=true;columns++;
  }
  if(columns<90&&n!="SilverCove")
   throw new Exception(n+" insufficient reference-registered land columns "+columns);
  // Recolor the northern 416 m of the LINKED-ONLY source copy using
  // the reference's own photographically registered two-dimensional biome field.
  // Use the actual per-pixel photo image, not one flat strip across X.
  for(int z=32;z<512;z++)for(int x=0;x<512;x++){
   if(!active[x])continue;
   float y=-24f+320f*h[z,x];
   if(y<.2f)continue;
   float border=S(32f,186f,z);
   float side=S(0f,24f,x)*S(0f,24f,511f-x);
   if(border<.001f||side<.001f)continue;
   var pix=mask[z*513+x];
   float ns=pix.r/255f,nr=pix.g/255f,ng=pix.b/255f,na=pix.a/255f;
   float c=ns+nr+ng+na;
   if(c<.11f)continue;
   float sum=.85f*ns+1.2f*nr+1.85f*ng+.96f*na;
   if(sum<.001f)continue;
   float[] goal=new float[7];
   goal[snow]=.85f*ns/sum;goal[rock]=1.2f*nr/sum;
   goal[green]=1.85f*.45f*ng/sum;goal[light]=1.85f*.32f*ng/sum;
   goal[arid]=(.96f*na+.43f*ng)/sum;
   float roads=Mathf.Clamp01(south[z,x,road]*2.25f);
   float steep=original.terrainLayers.Length>0 ?
     original.size.y*Mathf.Abs(h[Mathf.Min(512,z+2),x]-h[Mathf.Max(0,z-2),x])/4f : 0f;
   if(steep>.95f){ // retain exposed native cliff and ridge materials
    float recast=.65f*goal[green]+.65f*goal[light];
    goal[rock]+=recast;
    goal[green]*=.35f;goal[light]*=.35f;
   }
   float w=.96f*border*side*S(.10f,.43f,c)*(1f-.94f*roads);
   // Keep map detail and avoid a photographic flat-colored horizontal strip.
   float wrinkle=(Mathf.PerlinNoise(x*.018f+9.7f,z*.025f+12.3f)-.5f)*.22f;
   w=Mathf.Clamp01(w*(1f+wrinkle));
   if(w<.001f)continue;
   oldSnow+=south[z,x,snow];
   for(int k=0;k<7;k++)south[z,x,k]=Mathf.Lerp(south[z,x,k],goal[k],w);
   newSnow+=south[z,x,snow];
   sourcePixels++;
  }
  // Boundary rows of both linked surfaces must be IDENTICAL at every land x.
  // To avoid a new horizontal line, taper the adjoining NORTH area over 104 m
  // while retaining the existing crater/volcanic/geological patterns farther north.
  for(int x=0;x<512;x++){
   if(!active[x])continue;
   float joinStrength=.85f*S(0f,24f,x)*S(0f,24f,511f-x);
   for(int k=0;k<7;k++){
    // Use the actual post-paint native edge to preserve tiny source texture features.
    float target=south[511,x,k];
    for(int z=0;z<104;z++){
     float jag=(Mathf.PerlinNoise(x*.026f+5.2f,z*.031f+7.1f)-.5f)*13f;
     float w=joinStrength*(1f-S(0f,104f,z+jag*S(0f,18f,z)));
     if(z==0)w=1f;
     ext[z,x,k]=Mathf.Lerp(ext[z,x,k],target,w);
    }
    ext[0,x,k]=target;
    worstAlphaGap=Mathf.Max(worstAlphaGap,Mathf.Abs(ext[0,x,k]-south[511,x,k]));
   }
  }
  if(worstAlphaGap>.0001f||sourcePixels<3000&&n!="SilverCove")
   throw new Exception(n+" source photo pass invalid pixels="+sourcePixels+" gap="+worstAlphaGap);
  linked.SetAlphamaps(0,0,south);
  north.SetAlphamaps(0,0,ext);
  EditorUtility.SetDirty(linked);EditorUtility.SetDirty(north);
  File.AppendAllText(V+"linked_source_photo_registered_20260926.txt",
   n+" sourcePhotoPixels="+sourcePixels+" columns="+columns+
   " coastProtected="+coastProtected+
   " originalSnowMean="+(oldSnow/Mathf.Max(1,sourcePixels)).ToString("F3")+
   " linkedSnowMean="+(newSnow/Mathf.Max(1,sourcePixels)).ToString("F3")+
   " alphaBoundaryGap="+worstAlphaGap.ToString("F6")+
   " sourcePhotoBand=480m northContinuation=104m canonicalSourceWrites=0 heightWrites=0\n");
 }
}
