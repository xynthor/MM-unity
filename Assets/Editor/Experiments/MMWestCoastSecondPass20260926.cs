using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Corrects the excessive southern cliff without inventing a rectangular
// Paradise peninsula. Generated assets only; canonical originals untouched.
public static class MMWestCoastSecondPass20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string S="Assets/World/WorldExtensions/Generated/";
 const string D="Assets/World/DragonIsle/Generated/";
 const string B=@"C:\MMUnityPort\Backups\BeforeReferenceVisualSeamFix_20260926\";
 const string TmpP="Assets/TempParadisePreVisualCoast_20260926.asset";
 const string TmpD="Assets/TempDragonSouthPreVisualCoast_20260926.asset";
 static float E(float a,float b,float x){
  float u=Mathf.Clamp01((x-a)/(b-a));return u*u*(3f-2f*u);
 }
 static float H(float a,float b,float da,float db,int z,int width){
  float t=z/(float)width,t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1)*a+(t3-2*t2+t)*da*width+
         (-2*t3+3*t2)*b+(t3-t2)*db*width;
 }
 public static void Apply(){
  string ps=B+@"Assets\World\WorldExtensions\Generated\ParadiseValley_WestTerrain.asset";
  string ds=B+@"Assets\World\DragonIsle\Generated\DragonIsleSouthTerrain.asset";
  if(!File.Exists(ps)||!File.Exists(ds)||File.Exists(Path.GetFullPath(TmpP))||
     File.Exists(Path.GetFullPath(TmpD)))
   throw new Exception("Pre-fix source backup or scratch asset invalid");
  bool impP=false,impD=false;
  try{
   File.Copy(ps,Path.GetFullPath(TmpP));impP=true;
   File.Copy(ds,Path.GetFullPath(TmpD));impD=true;
   AssetDatabase.ImportAsset(TmpP,ImportAssetOptions.ForceSynchronousImport);
   AssetDatabase.ImportAsset(TmpD,ImportAssetOptions.ForceSynchronousImport);
   var origP=AssetDatabase.LoadAssetAtPath<TerrainData>(TmpP);
   var origD=AssetDatabase.LoadAssetAtPath<TerrainData>(TmpD);
   var p=AssetDatabase.LoadAssetAtPath<TerrainData>(S+"ParadiseValley_WestTerrain.asset");
   var d=AssetDatabase.LoadAssetAtPath<TerrainData>(D+"DragonIsleSouthTerrain.asset");
   if(!origP||!origD||!p||!d||d.alphamapLayers!=12)
    throw new Exception("Original generated terrains unavailable or Dragon native materials not installed");
   var ph=origP.GetHeights(0,0,513,513);
   var dh=origD.GetHeights(0,0,513,513);
   int modified=0;float biggest=0,highestJump=0;
   // Keep Paradise's native headland terrain. Previous mask reduction had
   // created a flat, stepped shelf on its western shore.
   for(int x=0;x<=512;x++){
    float seam=ph[512,x];
    if(Mathf.Abs(seam-dh[0,x])*320f>.002f)
      throw new Exception("Pre-fix Dragon/Paradise seam not canonical "+x);
    float y0=-24f+320f*seam,y96=-24f+320f*dh[96,x];
    if(x<300||x>495||y0<.1f)continue;
    if(Mathf.Abs(y0-(-24f+320f*dh[16,x]))<1.5f)continue;
    int width=y96<.1f?112:48;
    float startSlope=ph[512,x]-ph[511,x];
    float endSlope=(dh[width+1,x]-dh[width-1,x])*.5f;
    float lateral=E(300f,320f,x)*(1f-E(478f,496f,x));
    if(lateral<.001f)continue;
    for(int z=1;z<width;z++){
     float old=dh[z,x];
     float raw=H(seam,dh[width,x],startSlope,endSlope,z,width);
     raw=Mathf.Clamp(raw,Mathf.Min(seam,dh[width,x])-.002f,
                           Mathf.Max(seam,dh[width,x])+.002f);
     dh[z,x]=Mathf.Lerp(old,raw,lateral);
     float m=Mathf.Abs(dh[z,x]-old)*320f;
     if(m>.002f){modified++;biggest=Mathf.Max(biggest,m);}
    }
   }
   // Match the first derivative where the native Paradise coastline is dry.
   for(int x=320;x<=488;x++){
    if(-24f+ph[512,x]*320f<.12f)continue;
    float slope=ph[512,x]-ph[511,x];
    float discrepancy=slope-(dh[1,x]-dh[0,x]);
    float lateral=E(316f,333f,x)*(1f-E(477f,496f,x));
    if(Mathf.Abs(discrepancy*320f)<.035f)continue;
    for(int z=1;z<=12;z++){
     float w=z*(1f-E(0f,12f,z))*lateral;
     float y=dh[z,x]+discrepancy*w;
     if(-24f+y*320f<-.05f&&-24f+dh[z,x]*320f>.12f)continue;
     dh[z,x]=y;
    }
   }
   float maxBorder=0f;
   for(int x=0;x<513;x++){
    maxBorder=Mathf.Max(maxBorder,Mathf.Abs(dh[0,x]-ph[512,x])*320f);
    if(x>=325&&x<=478&&(-24f+ph[512,x]*320f)>.12f)
     highestJump=Mathf.Max(highestJump,Mathf.Abs(
       (dh[1,x]-dh[0,x])-(ph[512,x]-ph[511,x]))*320f);
   }
   if(maxBorder>.001f||highestJump>.24f||biggest>23f)
    throw new Exception("West coast safety guard border="+maxBorder+
      " maxDrySlopeJump="+highestJump+" maxMove="+biggest);
   p.SetHeights(0,0,ph);
   d.SetHeights(0,0,dh);
   var current=d.GetAlphamaps(0,0,512,512);
   var source=p.GetAlphamaps(0,511,512,1);
   var outA=new float[512,512,12];int colored=0;
   for(int z=0;z<512;z++)for(int x=0;x<512;x++){
    float h=-24f+dh[z,x]*320f;
    float w=(1f-E(0f,60f,z))*(1f-E(478f,496f,x))*
      E(-.1f,.6f,h);
    float baseSum=0f;
    for(int k=0;k<8;k++)baseSum+=current[z,x,k];
    if(baseSum<.0001f)baseSum=1f;
    for(int k=0;k<8;k++)
     outA[z,x,k]=(1f-w)*current[z,x,k]/baseSum;
    if(w>.001f)colored++;
    outA[z,x,8]=w*source[0,x,0]; // exact Paradise forest soil
    outA[z,x,9]=w*source[0,x,1]; // exact Paradise light forest
    outA[z,x,10]=w*source[0,x,3]; // exact Paradise rocky soil
    outA[z,x,11]=w*source[0,x,5]; // exact Paradise dry ground
    outA[z,x,1]+=w*source[0,x,2]; // native sand weight
    outA[z,x,4]+=w*source[0,x,4]; // snow
    outA[z,x,3]+=w*source[0,x,6]; // stone/road
   }
   d.SetAlphamaps(0,0,outA);
   var committed=d.GetAlphamaps(0,0,512,1);
   float colorError=0f;
   for(int x=320;x<=470;x++){
    float y=-24f+dh[0,x]*320f;
    if(y<.6f)continue;
    for(int k=0;k<4;k++){
     int from=new[]{0,1,3,5}[k];
     colorError=Mathf.Max(colorError,Mathf.Abs(
      committed[0,x,8+k]-source[0,x,from]));
    }
   }
   if(colorError>.002f)
    throw new Exception("Paradise/Dragon native terrain-color edge error "+colorError);
   var pond=d.GetHeights(458,185,38,46);
   int pondWet=0;
   for(int z=0;z<46;z++)for(int x=0;x<38;x++)
    if(-24f+pond[z,x]*320f<=.1f)pondWet++;
   if(pondWet!=150)throw new Exception("Protected Dragon South pond changed "+pondWet);
   EditorUtility.SetDirty(p);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();
   File.WriteAllText(V+"west_coast_second_pass_20260926.txt",
    "PASS restored native Paradise peninsula profile without synthetic shelf"+
    " dragonInterpolatedVertices="+modified+" maxDragonHeightCorrectionM="+biggest.ToString("F2")+
    " sharedBorderGapM="+maxBorder.ToString("F6")+
    " maxDrySlopeJumpMPerM="+highestJump.ToString("F3")+
    " nativeSourceColorPixels="+colored+" colorEdgeError="+colorError.ToString("F5")+
    " pondWet="+pondWet+" noCanonicalSourceEdits=true\n");
  }finally{
   if(impP)AssetDatabase.DeleteAsset(TmpP);
   if(impD)AssetDatabase.DeleteAsset(TmpD);
  }
 }
}
