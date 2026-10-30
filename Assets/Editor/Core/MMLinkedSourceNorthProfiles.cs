using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// LINKED-WORLD ONLY. The protected canonical source scenes and TerrainData are immutable.
// Reprofile ONLY the final 16m toe, preserving the original photo-mapped ridge
// conflicts with the reference-confirmed DRY north extension.
public static class MMLinkedSourceNorthProfiles {
 const string G="Assets/World/WorldExtensions/Generated/";
 const string TargetDir=G+"LinkedSourceTransitions";
 const string V="Validation/EdgeGrid20260923/";
 const int W=16;
 static float Ease(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3-2*t);
 }
 static float Hermite(float a,float b,float da,float db,float t,int span){
  float t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1)*a+(t3-2*t2+t)*da*span+
   (-2*t3+3*t2)*b+(t3-t2)*db*span;
 }
 static string Name(string label){
  switch(label){
   case "Sweet Water":return "SweetWater";
   case "Kriegspire":return "Kriegspire";
   case "Frozen Highlands":return "FrozenHighlands";
   case "Silver Cove":return "SilverCove";
   default:return null;
  }
 }
 static byte[] Mask(string n){
  var img=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!img.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+n+"_North.png"))||
   img.width!=513||img.height!=513)throw new Exception("Invalid source authority "+n);
  var v=img.GetPixels32().Select(c=>c.r).ToArray();
  UnityEngine.Object.DestroyImmediate(img);return v;
 }
 public static void Apply(GameObject linked,string label){
  string n=Name(label);if(n==null)return;
  var t=linked.GetComponentInChildren<Terrain>(true);
  var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
  var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
  if(!t||!src||!ext||t.terrainData!=src)
   throw new Exception("Source-aligned terrain missing in linked "+n);
  var s=src.GetHeights(0,0,513,513);
  var e=ext.GetHeights(0,0,513,2);
  var land=Mask(n);var h=(float[,])s.Clone();
  int band=W;
  var severity=new float[513];float sourceWorst=0;
  for(int x=8;x<=504;x++){
   float slope=(s[512,x]-s[511,x])*320f;
   float next=(e[1,x]-e[0,x])*320f;
   float edge=-24f+320f*s[512,x];
   if(land[16*513+x]<150||edge<.14f||
     -24f+320f*s[512-band,x]<3f||slope>-.8f)continue;
   severity[x]=Ease(.7f,2.0f,next-slope);
   sourceWorst=Mathf.Max(sourceWorst,Mathf.Abs(next-slope));
  }
  var smooth=new float[513];int columns=0,changed=0;float maxMove=0f,afterWorst=0f;
  for(int x=8;x<=504;x++){
   float total=0f,v=0f;
   for(int dx=-6;dx<=6;dx++){
    float w=7-Mathf.Abs(dx);v+=w*severity[x+dx];total+=w;
   }
   smooth[x]=v/total*Ease(8f,32f,x)*Ease(8f,32f,512f-x);
   if(smooth[x]<.01f)continue;
   float start=s[512-band,x],end=s[512,x];
   float dStart=(s[513-band,x]-s[511-band,x])*.5f;
   float dEnd=e[1,x]-e[0,x];
   // Avoid cubic overshoot into unsupported peaks/water.
   float lo=Mathf.Min(start,end)-.5f/320f;
   float hi=Mathf.Max(start,end)+.5f/320f;
   int local=0;
   for(int z=512-band+1;z<512;z++){
    float t0=(z-(512f-band))/band;
    float candidate=Mathf.Clamp(Hermite(start,end,dStart,dEnd,t0,band),lo,hi);
    float prior=h[z,x];
    h[z,x]=Mathf.Lerp(prior,candidate,smooth[x]);
    float move=Mathf.Abs(h[z,x]-prior)*320f;
    maxMove=Mathf.Max(maxMove,move);
    if(move>.002f){changed++;local++;}
   }
   if(local>0)columns++;
   afterWorst=Mathf.Max(afterWorst,
    Mathf.Abs((h[512,x]-h[511,x]-(e[1,x]-e[0,x]))*320f));
  }
  // Photo-confirmed dry land needs an exact tangent across the shared
  // source/extension border. Cubic interpolation over only 16m preserves
  // the ridge peak, but its last discrete sample still has residual slope.
  // Correct ONLY the last 8m on linked clones, never the immutable source.
  int tangentVertices=0;float maxTangentCorrection=0f;
  for(int x=8;x<=504;x++){
   float edge=-24f+320f*s[512,x];
   float peak=-24f+320f*s[496,x];
   float sourceSlope=(s[512,x]-s[511,x])*320f;
   if(land[16*513+x]<147||edge<.14f||peak<3f||sourceSlope>-.20f)
    continue;
   float desired=h[512,x]-(e[1,x]-e[0,x]);
   if(-24f+desired*320f<.12f)continue;
   float correction=desired-h[511,x];
   float coastalWeight=Ease(145f,161f,land[16*513+x]);
   if(Mathf.Abs(correction*coastalWeight)*320f<.025f)continue;
   for(int z=504;z<512;z++){
    float w=Ease(504f,511f,z)*coastalWeight;
    float next=h[z,x]+correction*w;
    if(-24f+next*320f<.12f)continue;
    float before=h[z,x];h[z,x]=next;
    if(Mathf.Abs(next-before)*320f>.003f){changed++;tangentVertices++;}
    maxTangentCorrection=Mathf.Max(maxTangentCorrection,Mathf.Abs(next-before)*320f);
    maxMove=Mathf.Max(maxMove,Mathf.Abs(h[z,x]-s[z,x])*320f);
   }
  }
  afterWorst=0f;
  for(int x=8;x<=504;x++){
   if(land[16*513+x]<150||-24f+320f*s[496,x]<3f||
      -24f+320f*s[512,x]<.14f)continue;
   afterWorst=Mathf.Max(afterWorst,
    Mathf.Abs((h[512,x]-h[511,x]-(e[1,x]-e[0,x]))*320f));
  }
  // Both the source and all generated tile borders must remain EXACT.
  float originalEdge=0f;
  for(int x=0;x<513;x++)originalEdge=Mathf.Max(originalEdge,
     Mathf.Abs(h[512,x]-s[512,x])*320f);
  float side=0f;
  for(int z=0;z<513;z++){
   side=Mathf.Max(side,Mathf.Abs(h[z,0]-s[z,0])*320f);
   side=Mathf.Max(side,Mathf.Abs(h[z,512]-s[z,512])*320f);
  }
  if(columns<10||maxMove>28f||originalEdge>.00001f||side>.00001f)
   throw new Exception(n+" linked north profile unsafe columns="+columns+
    " displacement="+maxMove+" originalEdge="+originalEdge+" side="+side);
  if(!AssetDatabase.IsValidFolder(TargetDir))
   AssetDatabase.CreateFolder(G.TrimEnd('/'),"LinkedSourceTransitions");
  string path=TargetDir+"/"+n+"_LinkedNorthProfile.asset";
  var copy=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
  if(!copy){
   copy=UnityEngine.Object.Instantiate(src);copy.name=n+"_LinkedNorthProfile";
   AssetDatabase.CreateAsset(copy,path);
  }else if(copy.heightmapResolution!=513||copy.alphamapLayers!=src.alphamapLayers)
   throw new Exception("Existing linked clone is incompatible: "+path);
  // Unity TerrainData.Instantiate/CreateAsset can retain layers but initialize
  // the newly persisted clone's alphamaps to 100% layer zero (grass).
  // Geometry-only reprofiling MUST copy every source splat pixel exactly.
  // Reapply on every build, including when reusing an existing clone.
  var srcLayers=src.terrainLayers;
  if(copy.alphamapResolution!=src.alphamapResolution ||
     copy.alphamapLayers!=src.alphamapLayers)
    throw new Exception(n+" linked terrain alphamap incompatible with source");
  copy.terrainLayers=srcLayers;
  ext.terrainLayers=srcLayers; // reset generated phase clones before deterministic repaint
  copy.SetAlphamaps(0,0,src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight));
  copy.SetHeights(0,0,h);EditorUtility.SetDirty(copy);
  // Always reset generated north splats from the clean pre-experiment snapshot first;
  // otherwise repeated visual passes compound into vertical smearing/banding.
  MMNorthBaselineAlphaReset20260927.Apply(n,ext);
  // In the linked world, match BOTH the original-border copy and generated north
  // extension to the same authority-image biome field. Canonical MM6 TerrainData stays untouched.
  MMUnifiedNorthReferencePaint20260926.Apply(n,src,copy,ext);
  MMGlobalNorthTerrainPhase20260926.Apply(n,src,copy,ext);
  MMPhotoVolcanoAsh20260926.Apply(n,ext);
  t.terrainData=copy;
  var collider=t.GetComponent<TerrainCollider>();
  if(collider)collider.terrainData=copy;
  // Preserve source-grounded vegetation where the linked-only terrain changes.
  // Move only model roots in known forest/dressing groups, never water or buildings.
  int grounded=0;
  string[] groups={"Vegetation - Source Anchored Final",
   "Vegetation - Preserved User Additions","Region Dressing - Reference Matched"};
  foreach(var group in linked.GetComponentsInChildren<Transform>(true)
    .Where(tr=>groups.Contains(tr.name))){
   foreach(Transform child in group){
    float u=(child.position.x-t.transform.position.x)/512f;
    float v=(child.position.z-t.transform.position.z)/512f;
    if(u<0f||u>1f||v<0f||v>1f||v<(512f-W)/512f)continue;
    float delta=copy.GetInterpolatedHeight(u,v)-src.GetInterpolatedHeight(u,v);
    if(Mathf.Abs(delta)<.002f)continue;
    child.position+=new Vector3(0f,delta,0f);grounded++;
   }
  }
  Directory.CreateDirectory(V);
  File.AppendAllText(V+"linked_only_north_cliff_profiles_20260925.csv",
   n+",sourceWorstSlopeJump="+sourceWorst.ToString("F3")+
   ",resultMaxSlopeJump="+afterWorst.ToString("F3")+
   ",changedColumns="+columns+",changedVertices="+changed+
   ",maxTerrainDisplacementM="+maxMove.ToString("F3")+
   ",tangentAdjustedVertices="+tangentVertices+
   ",maxTangentCorrectionM="+maxTangentCorrection.ToString("F3")+
   ",repositionedVegetation="+grounded+
   ",sourceEdgeErrorM="+originalEdge.ToString("F6")+
   ",sideBorderErrorM="+side.ToString("F6")+
   ",canonicalAssetsChanged=0\n");
  Debug.Log("LINKED_NORTH_SOURCE_PROFILE "+n+" columns="+columns+
   " maxHeightMove="+maxMove.ToString("F2")+" vegetation="+grounded);
 }
}

// force Unity domain reload for global phase hook 20260926

// FORCE_UNIFIED_NORTH_DOMAIN_RELOAD_20260927_1709
