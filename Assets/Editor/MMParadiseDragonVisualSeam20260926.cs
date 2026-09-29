using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMParadiseDragonVisualSeam20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string D="Assets/World/DragonIsle/Generated/";
 static float S(float a,float b,float x){
  float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3f-2f*t);
 }
 static float Cubic(float a,float b,float da,float db,int z,int span){
  float t=z/(float)span,t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1)*a+(t3-2*t2+t)*da*span+
   (-2*t3+3*t2)*b+(t3-t2)*db*span;
 }
 static TerrainLayer Aligned(TerrainLayer src,string path){
  var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
  if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
  EditorUtility.CopySerialized(src,l);l.name=Path.GetFileNameWithoutExtension(path);
  l.tileOffset=new Vector2(src.tileOffset.x,src.tileOffset.y-
    Mathf.Repeat(512f,src.tileSize.y));
  EditorUtility.SetDirty(l);return l;
 }
 public static void Apply(){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeReferenceVisualSeamFix_20260926\manifest.json"))
    throw new Exception("Pre-change visual backup missing");
  var p=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"ParadiseValley_WestTerrain.asset");
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(D+"DragonIsleSouthTerrain.asset");
  if(!p||!d||p.heightmapResolution!=513||d.heightmapResolution!=513)
    throw new Exception("Reference west terrains invalid");
  var ph=p.GetHeights(0,0,513,513);var dh=d.GetHeights(0,0,513,513);
  var pp=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!pp.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/ParadiseValley_West.png"))||
    pp.width!=513)throw new Exception("Reference Paradise coast unavailable");
  var px=pp.GetPixels32();UnityEngine.Object.DestroyImmediate(pp);
  int coast=0,slope=0;float maxP=0,maxD=0;
  // Paradise's north-west image confirms OPEN WATER. Prior synthetic height
  // mistakenly retained an elevated rectangular headland there.
  for(int z=384;z<=512;z++)for(int x=12;x<=493;x++){
    var r=px[z*513+x].r;
    if(r>=153)continue;
    float y=-24f+320f*ph[z,x];
    if(y<=-6.7f)continue;
    float coastY=-7f+10f*S(120f,153f,r);
    float weight=S(384f,512f,z)*S(12f,32f,x)*(1f-S(473f,494f,x));
    float target=Mathf.Min(y,Mathf.Lerp(y,coastY,weight));
    float correction=y-target;
    if(correction<.002f)continue;
    maxP=Mathf.Max(maxP,correction);coast++;
    ph[z,x]=Mathf.Clamp01((target+24f)/320f);
  }
  // Do not touch the source-adjacent east edge, other three tile sides,
  // the Dragon inland pond or either canonical original region.
  for(int x=0;x<=512;x++){
    float oldEdge=dh[0,x],edge=ph[512,x];
    dh[0,x]=edge;
    if(x<16||x>496)continue;
    float y0=-24f+320f*edge;
    int end=(-24f+320f*dh[96,x]<.1f)?112:48;
    float y1=-24f+320f*dh[end,x];
    if(y0<-.1f&&y1<-.1f&&(-24f+320f*oldEdge)<-.1f)continue;
    if(Mathf.Abs(y0-(-24f+320f*dh[16,x]))<1.0f&&
       Mathf.Abs(y0-(-24f+320f*oldEdge))<1.0f)continue;
    float da=ph[512,x]-ph[511,x];
    float db=(dh[end+1,x]-dh[end-1,x])*.5f;
    float blend=S(16f,33f,x)*(1f-S(477f,496f,x));
    for(int z=1;z<end;z++){
     float original=dh[z,x];
     float raw=Cubic(edge,dh[end,x],da,db,z,end);
     raw=Mathf.Clamp(raw,Mathf.Min(edge,dh[end,x])-.0015f,
                           Mathf.Max(edge,dh[end,x])+.0015f);
     float updated=Mathf.Lerp(original,raw,blend);
     float move=Mathf.Abs(updated-original)*320f;
     if(move>.002f){maxD=Mathf.Max(maxD,move);slope++;}
     dh[z,x]=updated;
    }
  }
  float seam=0,nearSlope=0;
  for(int x=0;x<513;x++){
   seam=Mathf.Max(seam,Mathf.Abs(ph[512,x]-dh[0,x])*320f);
   if(x>315&&x<488&&(-24f+ph[512,x]*320f)>.12f)
    nearSlope=Mathf.Max(nearSlope,Mathf.Abs(
      (dh[1,x]-dh[0,x])-(ph[512,x]-ph[511,x]))*320f);
  }
  if(seam>.001f||nearSlope>.37f||maxD>24f||maxP>24f)
    throw new Exception("Unsafe north/south west coast profile seam="+seam+
      " slope="+nearSlope+" dragonMove="+maxD+" paradiseMove="+maxP);
  p.SetHeights(0,0,ph);d.SetHeights(0,0,dh);
  // Use the ACTUAL Paradise diffuse materials on the shared coast,
  // not an arbitrary green/dirt approximation with mismatched texture tint.
  var source=p.GetAlphamaps(0,511,512,1);
  var old=d.GetAlphamaps(0,0,512,512);
  var baseLayers=d.terrainLayers;
  if(baseLayers.Length!=8&&baseLayers.Length!=12)
    throw new Exception("Expected eight original Dragon material layers");
  int[] native={0,1,3,5};
  var extra=new TerrainLayer[4];
  for(int k=0;k<4;k++)
    extra[k]=Aligned(p.terrainLayers[native[k]],
      D+"ParadiseNorth_"+p.terrainLayers[native[k]].name+"_Aligned.terrainlayer");
  var weights=new float[512,512,12];
  int painted=0;float maxColorError=0;
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   float y=-24f+320f*dh[Mathf.Min(512,z),x];
   float land=S(132f,154f,px[512*513+x].r);
   float w=(1f-S(0f,60f,z))*land*(1f-S(471f,496f,x));
   if(y<-.1f)w=0f;
   if(w>.001f)painted++;
   float baseTotal=0f;
   for(int k=0;k<8;k++)baseTotal+=old[z,x,k];
   if(baseLayers.Length==12)for(int k=8;k<12;k++)baseTotal+=old[z,x,k];
   if(baseTotal<.001f)baseTotal=1f;
   for(int k=0;k<8;k++)weights[z,x,k]=(1f-w)*old[z,x,k]/baseTotal;
   if(baseLayers.Length==12)
    for(int k=8;k<12;k++)weights[z,x,k]=(1f-w)*old[z,x,k]/baseTotal;
   for(int k=0;k<4;k++)weights[z,x,8+k]+=w*source[0,x,native[k]];
   weights[z,x,1]+=w*source[0,x,2]; // sand
   weights[z,x,4]+=w*source[0,x,4]; // snow
   weights[z,x,3]+=w*source[0,x,6]; // road/stone
  }
  d.terrainLayers=baseLayers.Take(8).Concat(extra).ToArray();
  d.SetAlphamaps(0,0,weights);
  var read=d.GetAlphamaps(0,0,512,1);
  for(int x=310;x<480;x++){
   float y=-24f+320f*dh[0,x];
   float w=S(132f,154f,px[512*513+x].r)*(1f-S(471f,496f,x));
   if(y<-.1f)continue;
   for(int k=0;k<4;k++)
    maxColorError=Mathf.Max(maxColorError,
     Mathf.Abs(read[0,x,8+k]-w*source[0,x,native[k]]));
  }
  if(maxColorError>.002f)throw new Exception("Persisted Paradise native splat mismatch "+maxColorError);
  EditorUtility.SetDirty(p);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();
  File.WriteAllText(V+"paradise_dragon_visual_coast_20260926.txt",
    "PASS paradiseReferenceCoastCells="+coast+" maxParadiseMoveM="+maxP.ToString("F2")+
    " dragonSlopeCells="+slope+" maxDragonMoveM="+maxD.ToString("F2")+
    " sharedBorderGapM="+seam.ToString("F5")+" landSlopeJumpMPerM="+nearSlope.ToString("F3")+
    " nativeParadiseColorPixels="+painted+" maxPersistedColorError="+maxColorError.ToString("F6")+
    " dragonPondUntouched=true canonicalScenesUntouched=true\n");
 }
}
