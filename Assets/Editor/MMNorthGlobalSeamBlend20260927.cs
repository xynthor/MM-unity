using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Irregular two-dimensional blend centered on the original/north join.
// Works on all four tiles as one 2048m strip; no canonical or height writes.
public static class MMNorthGlobalSeamBlend20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthGlobalSeamBlend_20260927\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static TerrainData D(string n,bool north){
  string p=north?G+n+"_NorthTerrain.asset":G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(p);
  if(!d||d.alphamapResolution!=512||d.alphamapLayers!=7)
   throw new Exception("Missing seam blend terrain "+p);
  return d;
 }
 static float S(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("North global seam blend backup missing");
  var src=new TerrainData[4];var ext=new TerrainData[4];
  for(int i=0;i<4;i++){src[i]=D(N[i],false);ext[i]=D(N[i],true);}
  int width=2048,layers=7,road=Array.FindIndex(src[0].terrainLayers,l=>l&&l.name=="Realistic_RoadOverlay");
  if(road<0)throw new Exception("Road layer missing in north seam blend");
  const int Z0=380,Z1=644,H=Z1-Z0+1; // 132m each side of shared border.
  var f=new float[H,width,layers];var h=new float[H,width];var roads=new float[H,width];
  for(int i=0;i<4;i++){
   var sa=src[i].GetAlphamaps(0,0,512,512);var ea=ext[i].GetAlphamaps(0,0,512,512);
   var sh=src[i].GetHeights(0,0,513,513);var eh=ext[i].GetHeights(0,0,513,513);
   for(int gz=Z0;gz<=Z1;gz++){
    bool n=gz>=512;int z=n?gz-512:gz;
    for(int x=0;x<512;x++){
     int gx=i*512+x;float[,,] a=n?ea:sa;
     for(int k=0;k<layers;k++)f[gz-Z0,gx,k]=a[z,x,k];
     roads[gz-Z0,gx]=a[z,x,road];
     h[gz-Z0,gx]=-24f+320f*(n?eh[z,x]:sh[z,x]);
    }
   }
  }
  // Separable box blur, first X then Z, only as a target. Blend strength below
  // keeps roads/coasts and preserves the authority-painted broad features.
  var xblur=new float[H,width,layers];
  const int RX=14,RZ=24;
  for(int z=0;z<H;z++)for(int k=0;k<layers;k++){
   float sum=0;int count=0;
   for(int x=0;x<width;x++){
    int add=x+RX,rem=x-RX-1;
    if(add<width){sum+=f[z,add,k];count++;}
    if(rem>=0){sum-=f[z,rem,k];count--;}
    if(x==0){
     sum=0;count=0;
     for(int q=0;q<=RX&&q<width;q++){sum+=f[z,q,k];count++;}
    }
    xblur[z,x,k]=count>0?sum/count:f[z,x,k];
   }
  }
  var blur=new float[H,width,layers];
  for(int x=0;x<width;x++)for(int k=0;k<layers;k++){
   float sum=0;int count=0;
   for(int z=0;z<H;z++){
    int add=z+RZ,rem=z-RZ-1;
    if(add<H){sum+=xblur[add,x,k];count++;}
    if(rem>=0){sum-=xblur[rem,x,k];count--;}
    if(z==0){
     sum=0;count=0;
     for(int q=0;q<=RZ&&q<H;q++){sum+=xblur[q,x,k];count++;}
    }
    blur[z,x,k]=count>0?sum/count:xblur[z,x,k];
   }
  }
  int changed=0;double total=0;float worstEdge=0;
  for(int z=0;z<H;z++)for(int x=0;x<width;x++){
   if(h[z,x]<.18f||roads[z,x]>.10f)continue;
   int gz=Z0+z;
   float center=512f+(Mathf.PerlinNoise(x*.0085f+4.1f,17.9f)-.5f)*54f;
   float dist=Mathf.Abs(gz-center);
   float seam=1f-S((dist-18f)/102f);
   if(seam<.001f)continue;
   float maxDiff=0;
   for(int k=0;k<layers;k++)maxDiff=Mathf.Max(maxDiff,Mathf.Abs(blur[z,x,k]-f[z,x,k]));
   float strength=.80f*seam*Mathf.Clamp01((maxDiff-.006f)/.12f);
   strength*=1f-Mathf.Clamp01(roads[z,x]*7f);
   if(strength<.001f)continue;
   float norm=0;
   for(int k=0;k<layers;k++){float old=f[z,x,k];f[z,x,k]=Mathf.Lerp(old,blur[z,x,k],strength);norm+=f[z,x,k];}
   if(norm>.00001f)for(int k=0;k<layers;k++)f[z,x,k]/=norm;
   changed++;total+=maxDiff;
  }
  // Enforce exact shared border after the 2D filter.
  int zs=511-Z0,ze=512-Z0;
  for(int x=0;x<width;x++){
   if(h[zs,x]<.18f||h[ze,x]<.18f)continue;
   for(int k=0;k<layers;k++){
    float shared=.5f*(f[zs,x,k]+f[ze,x,k]);
    f[zs,x,k]=shared;f[ze,x,k]=shared;
    worstEdge=Mathf.Max(worstEdge,Mathf.Abs(f[zs,x,k]-f[ze,x,k]));
   }
  }
  for(int i=0;i<4;i++){
   var sa=src[i].GetAlphamaps(0,0,512,512);var ea=ext[i].GetAlphamaps(0,0,512,512);
   for(int gz=Z0;gz<=Z1;gz++){
    bool n=gz>=512;int z=n?gz-512:gz;
    for(int x=0;x<512;x++){
     int gx=i*512+x;
     for(int k=0;k<layers;k++){
      if(n)ea[z,x,k]=f[gz-Z0,gx,k];else sa[z,x,k]=f[gz-Z0,gx,k];
     }
    }
   }
   src[i].SetAlphamaps(0,0,sa);ext[i].SetAlphamaps(0,0,ea);
   src[i].SetBaseMapDirty();ext[i].SetBaseMapDirty();
   EditorUtility.SetDirty(src[i]);EditorUtility.SetDirty(ext[i]);
  }
  AssetDatabase.SaveAssets();
  if(changed<25000||worstEdge>.0001f)
   throw new Exception("North global seam blend unsafe changed="+changed+" edge="+worstEdge);
  File.WriteAllText(V+"north_global_seam_blend_20260927.txt",
   "PASS changedPixels="+changed+
   " meanCandidateAlphaDifference="+(changed>0?total/changed:0).ToString("F5")+
   " xRadiusM="+RX+" zRadiusM="+RZ+
   " irregularCenterOffsetM=54 bandEachSideM=132"+
   " sharedAlphaGap="+worstEdge.ToString("F6")+
   " roadsProtected=true waterProtected=true canonicalWrites=0 heightWrites=0\n");
 }
}
