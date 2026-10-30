using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
// Removes broad vertical alpha banding by smoothing the FOUR north tiles as one
// continuous 2048m-wide field. Linked-only source copies and generated extensions only.
public static class MMNorthGlobalLateralDenoise20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthGlobalLateralDenoise_20260927\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static TerrainData D(string n,bool north){
  string p=north?G+n+"_NorthTerrain.asset":G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
  if(!d||d.alphamapResolution!=512||d.alphamapLayers!=7)
   throw new Exception("Missing denoise terrain "+p);
  return d;
 }
 static float Weight(int d){
  float x=d/17f;return Mathf.Exp(-.5f*x*x);
 }
 static void SmoothRow(float[,,] field,int z,int width,int layers,float[] road,float[] dry){
  var src=new float[width,layers];
  for(int x=0;x<width;x++)for(int k=0;k<layers;k++)src[x,k]=field[z,x,k];
  const int R=42;
  for(int x=0;x<width;x++){
   if(dry[x]<.5f||road[x]>.10f)continue;
   float[] avg=new float[layers];float sum=0;
   for(int q=-R;q<=R;q++){
    int xx=Mathf.Clamp(x+q,0,width-1);
    if(dry[xx]<.5f)continue;
    float w=Weight(q);sum+=w;
    for(int k=0;k<layers;k++)avg[k]+=src[xx,k]*w;
   }
   if(sum<.001f)continue;
   for(int k=0;k<layers;k++)avg[k]/=sum;
   float maxDiff=0;for(int k=0;k<layers;k++)maxDiff=Mathf.Max(maxDiff,Mathf.Abs(avg[k]-src[x,k]));
   float strength=Mathf.Clamp01((maxDiff-.010f)/.13f)*.90f;
   strength*=1f-Mathf.Clamp01(road[x]*6f);
   if(strength<.001f)continue;
   float norm=0;
   for(int k=0;k<layers;k++){field[z,x,k]=Mathf.Lerp(src[x,k],avg[k],strength);norm+=field[z,x,k];}
   if(norm>.00001f)for(int k=0;k<layers;k++)field[z,x,k]/=norm;
  }
 }
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("North denoise rollback snapshot missing");
  File.WriteAllText(V+"north_global_lateral_denoise_20260927.txt","");
  foreach(bool north in new[]{false,true}){
   var data=new TerrainData[4];for(int i=0;i<4;i++)data[i]=D(N[i],north);
   int layers=7,width=2048;
   var field=new float[512,width,layers];
   var heights=new float[512,width];
   int road=Array.FindIndex(data[0].terrainLayers,l=>l&&l.name=="Realistic_RoadOverlay");
   if(road<0)throw new Exception("Road layer missing in north denoise");
   for(int i=0;i<4;i++){
    var a=data[i].GetAlphamaps(0,0,512,512);
    var h=data[i].GetHeights(0,0,513,513);
    for(int z=0;z<512;z++)for(int x=0;x<512;x++){
     int gx=i*512+x;heights[z,gx]=-24f+320f*h[z,x];
     for(int k=0;k<layers;k++)field[z,gx,k]=a[z,x,k];
    }
   }
   double totalChange=0;int changed=0;
   for(int z=0;z<512;z++){
    var roads=new float[width];var dry=new float[width];
    for(int x=0;x<width;x++){roads[x]=field[z,x,road];dry[x]=heights[z,x]>.18f?1f:0f;}
    var before=new float[width,layers];
    for(int x=0;x<width;x++)for(int k=0;k<layers;k++)before[x,k]=field[z,x,k];
    SmoothRow(field,z,width,layers,roads,dry);
    for(int x=0;x<width;x++){
     float d=0;for(int k=0;k<layers;k++)d=Mathf.Max(d,Mathf.Abs(field[z,x,k]-before[x,k]));
     if(d>.002f){changed++;totalChange+=d;}
    }
   }
   for(int i=0;i<4;i++){
    var a=new float[512,512,layers];
    for(int z=0;z<512;z++)for(int x=0;x<512;x++)for(int k=0;k<layers;k++)
     a[z,x,k]=field[z,i*512+x,k];
    data[i].SetAlphamaps(0,0,a);data[i].SetBaseMapDirty();EditorUtility.SetDirty(data[i]);
   }
   AssetDatabase.SaveAssets();
   File.AppendAllText(V+"north_global_lateral_denoise_20260927.txt",
    (north?"EXT":"SRC")+" changedPixels="+changed+
    " meanMaxAlphaChange="+(changed>0?totalChange/changed:0).ToString("F5")+
    " radiusM=42 sigmaM=17 roadProtected=true waterProtected=true canonicalWrites=0 heightWrites=0\n");
  }
 }
}

// force Unity compile before queued rerun 20260927
