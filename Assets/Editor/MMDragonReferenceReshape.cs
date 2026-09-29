using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMDragonReferenceReshape {
 const string ROOT="Assets/World/DragonIsle/Generated/";
 const float BASE=-24f, HEIGHT=320f, WATER=.10f;
 static float Smooth(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/Mathf.Max(.0001f,b-a));
  return t*t*(3f-2f*t);
 }
 static float World(float h)=>BASE+h*HEIGHT;
 static float Norm(float y)=>Mathf.Clamp01((y-BASE)/HEIGHT);
 static int Layer(TerrainLayer[] a,string text){
  return Array.FindIndex(a,x=>x&&x.name.IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0);
 }
 static float SignedMeters(Color32 p){
  return (p.r-128f)*512f/(148f*3f);
 }
 static void BuildHalf(string half,List<string> report){
  string asset=ROOT+"DragonIsle"+half+"Terrain.asset";
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(asset);
  if(!td||td.heightmapResolution!=513||td.alphamapResolution!=512)
   throw new Exception("Dragon terrain missing: "+asset);
  var map=MMReferenceEdgeProfiles.Load("DragonIsle_"+half);
  if(map==null)throw new Exception("Dragon reference mask missing "+half);
  // Reconstruct each untouched half directly from the protected source trace.
  var baseline=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/TempDragonTemplate/DragonIsleReferenceTerrain_PREVIEW.asset");
  if(!baseline||baseline.heightmapResolution!=513||baseline.alphamapResolution!=512)
   throw new Exception("Protected original Dragon trace missing");
  // The source reconstruction consumes its original terrain-layer set. Any legacy
  // stretched-photo layer is stripped, then original tiled materials are restored.
  if(td.alphamapLayers==baseline.alphamapLayers+1 &&
     td.terrainLayers.Last() &&
     td.terrainLayers.Last().name.EndsWith("_ReferenceTerrainLayer"))
   td.terrainLayers=td.terrainLayers.Take(baseline.alphamapLayers).ToArray();
  // Dragon South's two EXTRA source-texture layers are seam dressing only.
  // Strip them before reconstructing the six protected original trace layers.
  if(!string.Equals(half,"North")&&td.alphamapLayers==baseline.alphamapLayers+2&&
     td.terrainLayers[6]&&(td.terrainLayers[6].name=="Realistic_Arid"||
                            td.terrainLayers[6].name=="SweetWaterWest_Arid_Aligned")&&
     td.terrainLayers[7]&&(td.terrainLayers[7].name=="Realistic_LightGreen"||
                            td.terrainLayers[7].name=="SweetWaterWest_LightGreen_Aligned"))
   td.terrainLayers=td.terrainLayers.Take(baseline.alphamapLayers).ToArray();
  bool north=half=="North";
  TerrainData sweet=null; float[,] sweetH=null;
  if(!north){
   sweet=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/SweetWater/Generated/SweetWaterTerrain.asset");
   if(!sweet||sweet.heightmapResolution!=513)
    throw new Exception("Canonical Sweet Water terrain missing for west-shore refinement");
   sweetH=sweet.GetHeights(0,0,513,513);
  }
  var original=baseline.GetHeights(0,0,513,513);
  var old=new float[513,513];
  for(int zz=0;zz<513;zz++)for(int xx=0;xx<513;xx++){
   if(north && zz<=256)old[zz,xx]=original[zz+256,xx];
   else if(!north && zz>=256)old[zz,xx]=original[zz-256,xx];
   else {
    int edge=north?512:0;
    float u=(north?zz-256:256-zz)/64f;
    old[zz,xx]=Mathf.Lerp(original[edge,xx],Norm(-6.7f),Smooth(0f,1f,u));
   }
  }
  var h=new float[513,513];
  int land=0;
  for(int z=0;z<513;z++)for(int x=0;x<513;x++){
   var px=map.At(x,z);
   float d=SignedMeters(px);
   float coast=Smooth(-5f,9f,d);
   float interior=Smooth(2f,48f,d);
   float ox=x-256f,oz=z-256f;
   float n0=Mathf.PerlinNoise((ox+971f)*.011f,(oz+513f)*.011f);
   float n1=Mathf.PerlinNoise((ox+227f)*.032f,(oz+819f)*.032f);
   float baseY=Mathf.Lerp(-6.7f,2.5f+8f*n0+3f*n1,coast);
   baseY+=interior*(px.b/255f)*10f;
   // EAST side of Dragon South is the authority-traced Sweet Water west shore.
   // Continue the real Sweet Water west terrain profile across that mainland,
   // while the authority mask alone still defines its shoreline.
   if(!north && x>=326 && d>0f){
    int srcX=Mathf.Clamp(Mathf.RoundToInt((512-x)*.95f),0,190);
    float sweetY=World(sweetH[z,srcX]);
    float mainland=Smooth(2f,28f,d)*Smooth(326f,390f,x);
    baseY=Mathf.Lerp(baseY,sweetY,.88f*mainland);
   }
   float oldY=World(old[z,x]);
   if(oldY>WATER+.45f && d>-8f)
    baseY=Mathf.Max(baseY,Mathf.Lerp(2f,oldY,Smooth(0f,24f,d)));
   if(d<0f)baseY=Mathf.Min(baseY,Mathf.Lerp(-6.7f,.35f,Smooth(-8f,0f,d)));
   h[z,x]=Norm(baseY);
   if(baseY>WATER+.45f)land++;
  }
  td.SetHeights(0,0,h);
  var originalA=baseline.GetAlphamaps(0,0,512,512);
  int L=td.alphamapLayers;
  if(L!=baseline.alphamapLayers)throw new Exception("Dragon source layer count changed");
  var oldA=new float[512,512,L];
  for(int zz=0;zz<512;zz++)for(int xx=0;xx<512;xx++){
   bool keep=north?zz<256:zz>=256;
   if(keep){
    int srcZ=north?zz+256:zz-256;
    for(int k=0;k<L;k++)oldA[zz,xx,k]=originalA[srcZ,xx,k];
   }else oldA[zz,xx,0]=1f;
  }
  int green=Layer(td.terrainLayers,"Grass");
  int sand=Layer(td.terrainLayers,"Sand");
  int rock=Layer(td.terrainLayers,"Rock");
  int arid=Layer(td.terrainLayers,"Dirt");
  int snow=Layer(td.terrainLayers,"Snow");
  if(green<0||sand<0||rock<0||arid<0)throw new Exception("Dragon layers missing");
  var a=new float[512,512,L];
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   int hx=Mathf.Min(512,x),hz=Mathf.Min(512,z);
   var px=map.At(hx,hz);
   float d=SignedMeters(px);
   float y=World(h[hz,hx]);
   float dx=(h[hz,Mathf.Min(512,hx+1)]-h[hz,Mathf.Max(0,hx-1)])*HEIGHT*.5f;
   float dz=(h[Mathf.Min(512,hz+1),hx]-h[Mathf.Max(0,hz-1),hx])*HEIGHT*.5f;
   float slope=Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
   bool keepOld=World(old[hz,hx])>WATER+.45f && d>2f;
   if(keepOld){
    float sum=0f;for(int k=0;k<L;k++){a[z,x,k]=oldA[z,x,k];sum+=a[z,x,k];}
    if(sum>0f)for(int k=0;k<L;k++)a[z,x,k]/=sum;
    if(!north && z<365 && d>10f && y>1.25f && slope<29f){
     // Retain the protected source texture underneath a greener south coast.
     float forest=.62f*(1f-Smooth(24f,29f,slope));
     for(int k=0;k<L;k++)a[z,x,k]*=(1f-forest);
     a[z,x,green]+=.87f*forest;
     a[z,x,arid]+=.13f*forest;
    }
    continue;
   }
   float beach=(1f-Smooth(.5f,5f,y))*(1f-Smooth(11f,30f,slope));
   float steep=Smooth(18f,38f,slope);
   float landf=Smooth(-2f,8f,d);
   float g=.72f*landf*(1f-steep)*(1f-beach);
   float sa=1.2f*beach+.15f*(1f-landf);
   float ro=.22f*landf+.95f*steep*landf;
   float ar=.55f*landf*(1f-steep);
   if(north && z>355){
    // Pointed arid northern cape: pale ridge and sparse vegetation.
    float cape=Smooth(355f,450f,z)*landf;
    sa+=.95f*cape; ar+=.28f*cape; g*=1f-.62f*cape;
   }
   if(!north && x>320 && z<410){
    // Sweet Water's eastern mainland is wooded, not Dragon Isle sand.
    float forest=Smooth(320f,390f,x)*(1f-Smooth(340f,430f,z));
    g+=1.30f*forest*landf*(1f-steep);
    ar*=1f-.52f*forest;
    ro*=1f-.24f*forest;
   }
   var wt=new float[L];
   wt[green]=g;wt[sand]=sa;wt[rock]=ro;wt[arid]=ar;
   if(snow>=0)wt[snow]=Mathf.Max(0f,(px.g/255f-.72f))*1.2f*landf;
   float total=wt.Sum()+.0001f;
   for(int k=0;k<L;k++)a[z,x,k]=wt[k]/total;
  }
  td.SetAlphamaps(0,0,a);
  if(!north){
   // Keep the isolated southern island's reference forest layer after
   // subsequent terrain reconstruction; the original Dragon layer is untouched.
   var forest=AssetDatabase.LoadAssetAtPath<TerrainLayer>(
     ROOT+"ReferenceDragonForestGrass.terrainlayer");
   if(!forest)throw new Exception("Reference Dragon South forest layer missing");
   var layers=(TerrainLayer[])td.terrainLayers.Clone();
   layers[0]=forest;td.terrainLayers=layers;
  }
  EditorUtility.SetDirty(td);
  report.Add($"DragonIsle_{half},land_pct={land*100f/(513f*513f):F2},mask={map.name}");
 }
 public static void Apply(){
  var report=new List<string>();
  BuildHalf("North",report);
  BuildHalf("South",report);
  var n=AssetDatabase.LoadAssetAtPath<TerrainData>(ROOT+"DragonIsleNorthTerrain.asset");
  var s=AssetDatabase.LoadAssetAtPath<TerrainData>(ROOT+"DragonIsleSouthTerrain.asset");
  var nh=n.GetHeights(0,0,513,513);
  var sh=s.GetHeights(0,0,513,513);
  float before=0f;
  // A shared border alone hides height discontinuity in the numeric audit
  // but leaves a visible cliff one vertex inside each tile. Blend 22m
  // inward on BOTH halves, preserving their more distant island silhouettes.
  for(int x=0;x<513;x++){
   before=Mathf.Max(before,Mathf.Abs(nh[0,x]-sh[512,x])*HEIGHT);
   float mid=(nh[0,x]+sh[512,x])*.5f;
   float northDelta=mid-nh[0,x],southDelta=mid-sh[512,x];
   for(int q=0;q<=22;q++){
    float f=1f-Smooth(0f,1f,q/22f);
    nh[q,x]+=northDelta*f;
    sh[512-q,x]+=southDelta*f;
   }
  }
  n.SetHeights(0,0,nh);s.SetHeights(0,0,sh);
  // Blend the corresponding terrain-layer weights across this same
  // boundary so its vegetation and rock coloration don't change abruptly.
  var na=n.GetAlphamaps(0,0,512,512);
  var sa=s.GetAlphamaps(0,0,512,512);
  if(n.alphamapLayers==s.alphamapLayers){
   int layers=n.alphamapLayers;
   for(int x=0;x<512;x++)for(int k=0;k<layers;k++){
    float common=(na[0,x,k]+sa[511,x,k])*.5f;
    float dn=common-na[0,x,k],ds=common-sa[511,x,k];
    for(int q=0;q<=12;q++){
     float f=1f-Smooth(0f,1f,q/12f);
     na[q,x,k]+=dn*f;sa[511-q,x,k]+=ds*f;
    }
   }
   n.SetAlphamaps(0,0,na);s.SetAlphamaps(0,0,sa);
  }
  EditorUtility.SetDirty(n);EditorUtility.SetDirty(s);
  AssetDatabase.SaveAssets();
  report.Add($"internal_seam_before_m={before:F5}");
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/dragon_reference_reshape.txt",report);
  MMDragonWestEdgeJoin.Apply();
   MMReferenceTiledGround.ApplyOne("DragonIsle_North");
   MMReferenceTiledGround.ApplyOne("DragonIsle_South");
  MMStandaloneRegionRig.EnsureScene("Assets/Scenes/Extensions/DragonIsle_North.unity");
  MMStandaloneRegionRig.EnsureScene("Assets/Scenes/Extensions/DragonIsle_South.unity");
  AssetDatabase.SaveAssets();
  Debug.Log("DRAGON_REFERENCE_RESHAPE_DONE "+string.Join(" | ",report));
 }
 public static void ApplySouthOnly(){
  var report=new List<string>();
  BuildHalf("South",report);
  var north=AssetDatabase.LoadAssetAtPath<TerrainData>(ROOT+"DragonIsleNorthTerrain.asset");
  var south=AssetDatabase.LoadAssetAtPath<TerrainData>(ROOT+"DragonIsleSouthTerrain.asset");
  if(!north||!south)throw new Exception("Dragon Isle halves missing");
  var nh=north.GetHeights(0,0,513,513);
  var sh=south.GetHeights(0,0,513,513);
  float mismatch=0f;
  for(int x=0;x<513;x++){
   float delta=nh[0,x]-sh[512,x];
   mismatch=Mathf.Max(mismatch,Mathf.Abs(delta)*HEIGHT);
   for(int q=0;q<=24;q++){
    float f=1f-Smooth(0f,1f,q/24f);
    sh[512-q,x]+=delta*f;
   }
  }
  south.SetHeights(0,0,sh);
  EditorUtility.SetDirty(south);AssetDatabase.SaveAssets();
  report.Add($"dragon_north_south_seam_before_m={mismatch:F5}");
  MMDragonWestEdgeJoin.Apply();
   MMReferenceTiledGround.ApplyOne("DragonIsle_North");
   MMReferenceTiledGround.ApplyOne("DragonIsle_South");
  MMStandaloneRegionRig.EnsureScene("Assets/Scenes/Extensions/DragonIsle_North.unity");
  MMStandaloneRegionRig.EnsureScene("Assets/Scenes/Extensions/DragonIsle_South.unity");
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/visible_sweetwater_west_rebuild.txt",report);
  Debug.Log("VISIBLE_SWEETWATER_WEST_REBUILD_DONE "+string.Join(" | ",report));
 }
 [MenuItem("MMUnity/World/Rebuild Sweet Water West Coast Without Touching Dragon North")]
 public static void MenuApplySouth(){ApplySouthOnly();}
 [MenuItem("MMUnity/World/Reshape Dragon Isle From Authority Map")]
 public static void MenuApply(){Apply();}
}
