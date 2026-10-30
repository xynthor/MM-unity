using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Bilateral photo-derived snow/rock relief without touching canonical source scenes.
// Rebuilds deterministically from protected baseline to avoid compounded painting.
public static class MMNorthPhotoSnowJoin20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Baseline=@"C:\MMUnityPort\Backups\BeforeNorthAndWestVisualContinuity_20260926\Assets\World\WorldExtensions\Generated\";
 const int NorthSpan=220,SouthSpan=136;
 static float S(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));
  return t*t*(3f-2f*t);
 }
 static float[,,] ReadBaseline(string n,TerrainData ext){
  string path="Assets/TempNorthPhotoBaseline_"+n+"_20260926.asset";
  string file=Baseline+n+"_NorthTerrain.asset";
  if(!File.Exists(file)||File.Exists(Path.GetFullPath(path)))
   throw new Exception(n+" missing native reference baseline or temporary asset collision");
  File.Copy(file,Path.GetFullPath(path));
  try{
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
   if(!data||data.alphamapResolution!=512||
      data.alphamapLayers!=ext.alphamapLayers)
    throw new Exception(n+" bad baseline splatmaps");
   return data.GetAlphamaps(0,0,512,512);
  }finally{AssetDatabase.DeleteAsset(path);}
 }
 public static void Apply(string n,TerrainData native,TerrainData linked,TerrainData ext){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeNorthAndWestVisualContinuity_20260926\manifest.json"))
   throw new Exception("Immutable terrain backup required");
  if(native.alphamapResolution!=512||linked.alphamapResolution!=512||
     ext.alphamapResolution!=512||native.alphamapLayers!=linked.alphamapLayers||
     native.alphamapLayers!=ext.alphamapLayers)
    throw new Exception(n+" incompatible native texture map sizes");
  int layers=native.alphamapLayers;
  for(int k=0;k<layers;k++){
   var original=native.terrainLayers[k];
   var current=ext.terrainLayers[k];
   var aligned=AssetDatabase.LoadAssetAtPath<TerrainLayer>(
    "Assets/World/WorldExtensions/Generated/NorthernPhaseLayers/"+n+"_"+k+"_NorthPhaseAligned.terrainlayer");
   if(linked.terrainLayers[k]!=original||
      (current!=original&&current!=aligned))
    throw new Exception(n+" unexpected north material "+k);
  }
  var source=native.GetAlphamaps(0,0,512,512);
  var baseline=ReadBaseline(n,ext);
  var photo=(float[,,])baseline.Clone();
  var sourceH=native.GetHeights(0,512,513,1);
  var northH=ext.GetHeights(0,0,513,1);
  var mask=MMReferenceEdgeProfiles.Load(n+"_North");
  int snow=Array.FindIndex(native.terrainLayers,l=>l&&l.name=="Realistic_Snow");
  if(snow<0)throw new Exception(n+" missing original snow");
  var smoothPhoto=new float[512,layers];
  var smoothNative=new float[512,layers];int columns=0;
  // Spatially average the photographic target to avoid long vertical stripes.
  for(int x=0;x<512;x++)for(int k=0;k<layers;k++){
   float sum=0,nativeSum=0,weight=0;
   for(int d=-50;d<=50;d++){
    int xx=Mathf.Clamp(x+d,0,511);
    if(northH[0,xx]*320f-24f<.15f||sourceH[0,xx]*320f-24f<.15f)continue;
    float w=Mathf.Exp(-d*d/1250f);
    sum+=baseline[185,xx,k]*w;
    nativeSum+=source[511,xx,k]*w;
    weight+=w;
   }
   smoothPhoto[x,k]=weight>.001f?sum/weight:baseline[185,x,k];
   smoothNative[x,k]=weight>.001f?nativeSum/weight:source[511,x,k];
  }
  for(int x=0;x<512;x++){
   if(sourceH[0,x]*320f-24f<.15f||
      northH[0,x]*320f-24f<.15f||
      mask.At(x,16).r<148)continue;
   float nativeSnow=source[511,x,snow];
   float photoSnow=smoothPhoto[x,snow];
   if(Mathf.Max(nativeSnow,photoSnow)<.31f)continue;
   float side=S(0f,24f,x)*S(0f,24f,511f-x);
   float[] target=new float[layers];float total=0;
   for(int k=0;k<layers;k++){
    float nativeEdge=Mathf.Lerp(source[511,x,k],smoothNative[x,k],.86f*side);
    target[k]=Mathf.Lerp(nativeEdge,smoothPhoto[x,k],.53f*side);
    total+=target[k];
   }
   for(int k=0;k<layers;k++)target[k]/=Mathf.Max(.00001f,total);
   float ns=157f+60f*Mathf.PerlinNoise(x*.017f+4.3f,14.6f);
   float ss=101f+31f*Mathf.PerlinNoise(x*.021f+3.5f,28.6f);
   // Fade dark photo rock into the northern EDGE of the linked-only source,
   // preserving the 15 standalone canonical source regions in their entirety.
   for(int z=512-SouthSpan;z<512;z++){
    float wrinkle=Mathf.PerlinNoise(x*.023f+7.1f,z*.011f+9.2f)-.5f;
    float w=S(0f,ss,z-(512-SouthSpan)+wrinkle*16f*
      S(0f,28f,z-(512-SouthSpan)));
    float dist=512f-z;
    float detail=S(0f,38f,dist)*.33f;
    float shift=Mathf.PerlinNoise(x*.025f+8.1f,z*.017f+5.6f)-.5f;
    int zPhoto=Mathf.Clamp(Mathf.RoundToInt(185f-dist*.32f+shift*30f),0,NorthSpan);
    for(int k=0;k<layers;k++){
     float organic=Mathf.Lerp(target[k],baseline[zPhoto,x,k],detail);
     source[z,x,k]=Mathf.Lerp(source[z,x,k],organic,w);
    }
   }
   for(int k=0;k<layers;k++)source[511,x,k]=target[k];
   for(int z=0;z<=NorthSpan;z++){
    float wrinkle=Mathf.PerlinNoise(x*.025f+6.5f,z*.012f+11.2f)-.5f;
    float w=S(0f,ns,z+wrinkle*27f*S(0f,30f,z));
    float ornament=.40f*S(0f,31f,z)*(1f-S(162f,NorthSpan,z));
    int zPhoto=Mathf.Clamp(Mathf.RoundToInt(185f+z*.15f+
       wrinkle*30f),0,NorthSpan);
    for(int k=0;k<layers;k++){
     float organic=Mathf.Lerp(target[k],baseline[zPhoto,x,k],ornament);
     photo[z,x,k]=Mathf.Lerp(organic,baseline[z,x,k],w);
    }
   }
   for(int k=0;k<layers;k++)photo[0,x,k]=target[k];
   columns++;
  }
  if(columns<10)throw new Exception(n+" photo-confirmed snow area too small: "+columns);
  linked.SetAlphamaps(0,0,source);
  ext.SetAlphamaps(0,0,photo);
  MMNorthTexturePhaseAlign20260926.Apply(n,native,ext);
  var south=linked.GetAlphamaps(0,511,512,1);
  var north=ext.GetAlphamaps(0,0,512,1);
  float gap=0;
  for(int x=0;x<512;x++)for(int k=0;k<layers;k++)
   if(sourceH[0,x]*320f-24f>.15f&&northH[0,x]*320f-24f>.15f&&
      mask.At(x,16).r>=148&&
      Mathf.Max(source[511,x,snow],smoothPhoto[x,snow])>=.31f)
    gap=Mathf.Max(gap,Mathf.Abs(south[0,x,k]-north[0,x,k]));
  if(gap>.002f)throw new Exception(n+" committed source/photo border mismatch "+gap);
  EditorUtility.SetDirty(linked);EditorUtility.SetDirty(ext);
  File.AppendAllText(V+"north_photo_snow_join_20260926.txt",
    n+" bilateralPhotoSnowColumns="+columns+" sourceCopyBandM="+SouthSpan+
    " northPhotoBandM="+NorthSpan+" committedAlphaGap="+gap.ToString("F6")+
    " protectedOriginalsUntouched=true terrainHeightWrites=0\n");
 }
}
