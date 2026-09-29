using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Paint only generated north terrains + linked-only source copies from the SAME
// registered authority-image biome field so the y=158 border is spatially continuous.
public static class MMUnifiedNorthReferencePaint20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string T=V+"CombinedNorthReferenceBiomes_20260927/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeUnifiedNorthReferencePaint_20260926\manifest.json";
 static float S(float a,float b,float x){
  float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);
 }
 static Color32[] Load(string n,string suffix){
  var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  string p=T+n+"_"+suffix+".png";
  if(!File.Exists(p)||!tex.LoadImage(File.ReadAllBytes(p))||
     tex.width!=513||tex.height!=513)throw new Exception("Missing unified north target "+p);
  var d=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);return d;
 }
 static int Find(TerrainData d,string n){
  int k=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_"+n);
  if(k<0)throw new Exception("Missing native layer "+n);return k;
 }
 static void Goal(Color32 p,float[] g,int snow,int rock,int green,int light,int arid,bool northern){
  float sn=p.r/255f,ro=p.g/255f,fo=p.b/255f,oc=p.a/255f;
  // Snow-dominant photographic pixels are bright snowy slopes; preserve
  // exposed dark rock at low photo-snow intensity and volcanic crater shadows.
  // Both sides sample one registered field. A tile-dependent snow boost
  // creates an artificial ecotone even when the border row is averaged.
  float snowy=S(.18f,.52f,sn);
  float snowWeight=Mathf.Lerp(.95f,1.72f,snowy);
  float rockWeight=Mathf.Lerp(1.15f,.79f,snowy);
  float sum=snowWeight*sn+rockWeight*ro+1.75f*fo+.92f*oc;
  Array.Clear(g,0,g.Length);
  if(sum<.05f)return;
  g[snow]=snowWeight*sn/sum;g[rock]=rockWeight*ro/sum;
  g[green]=1.75f*.62f*fo/sum;g[light]=1.75f*.38f*fo/sum;
  g[arid]=.92f*oc/sum;
 }
 public static void Apply(string n,TerrainData canonical,TerrainData linked,TerrainData north){
  if(!File.Exists(Backup))throw new Exception("Unified north restore point missing");
  if(canonical.alphamapLayers!=7||linked.alphamapLayers!=7||north.alphamapLayers!=7)
   throw new Exception(n+" unexpected layer count");
  var srcTarget=Load(n,"SourceNorth");var extTarget=Load(n,"North");
  var s=linked.GetAlphamaps(0,0,512,512);var e=north.GetAlphamaps(0,0,512,512);
  var sh=canonical.GetHeights(0,0,513,513);var eh=north.GetHeights(0,0,513,513);
  int snow=Find(canonical,"Snow"),rock=Find(canonical,"Volcanic"),
      green=Find(canonical,"Green"),light=Find(canonical,"LightGreen"),
      arid=Find(canonical,"Arid"),road=Find(canonical,"RoadOverlay");
  int sourcePx=0,extPx=0;float edgeGap=0;var goal=new float[7];
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   float y=-24f+320f*sh[z,x];if(y<.15f)continue;
   float side=S(0f,22f,x)*S(0f,22f,511f-x);
   // The registered y158..308 crop describes this entire source tile.
   // Restricting it to the northern 168m retained large rectangular legacy
   // snow patches in areas mapped as forest/ochre. Preserve the adjoining
   // southern border with a 32m transition; no height filtering is involved.
   float northWeight=S(0f,32f,z)*side;
   if(northWeight<.001f)continue;
   var p=srcTarget[z*513+x];
   float confidence=(p.r+p.g+p.b+p.a)/(4f*255f);
   if(confidence<.014f)continue;
   Goal(p,goal,snow,rock,green,light,arid,false);
   float roads=Mathf.Clamp01(s[z,x,road]*2.5f);
   // Dark green and ochre in the actual authority image have low absolute
   // RGB-classifier confidence. The former .22 cutoff left nearly all
   // original Kriegspire and Frozen Highlands snow untouched, creating the
   // visible straight WHITE belt immediately south of the north extension.
   float w=.90f*northWeight*S(.014f,.060f,confidence)*(1f-.92f*roads);
   float dz=Mathf.Abs(sh[Mathf.Min(512,z+2),x]-sh[Mathf.Max(0,z-2),x])*80f;
   if(dz>.65f){
    float f=goal[green]+goal[light];
    goal[rock]+=f*.72f;goal[green]*=.28f;goal[light]*=.28f;
   }
   for(int k=0;k<7;k++)s[z,x,k]=Mathf.Lerp(s[z,x,k],goal[k],w);
   sourcePx++;
  }
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   float y=-24f+320f*eh[z,x];if(y<.15f)continue;
   float side=S(0f,22f,x)*S(0f,22f,511f-x);
   if(side<.001f)continue;
   var p=extTarget[z*513+x];
   float confidence=(p.r+p.g+p.b+p.a)/(4f*255f);
   if(confidence<.014f)continue;
   Goal(p,goal,snow,rock,green,light,arid,true);
   float roads=Mathf.Clamp01(e[z,x,road]*2.5f);
   float w=.90f*side*S(.014f,.060f,confidence)*(1f-.92f*roads);
   float dz=Mathf.Abs(eh[Mathf.Min(512,z+2),x]-eh[Mathf.Max(0,z-2),x])*80f;
   if(dz>.65f){
    float f=goal[green]+goal[light];
    goal[rock]+=f*.72f;goal[green]*=.28f;goal[light]*=.28f;
   }
   for(int k=0;k<7;k++)e[z,x,k]=Mathf.Lerp(e[z,x,k],goal[k],w);
   extPx++;
  }
  for(int x=0;x<512;x++){
   float ys=-24f+320f*sh[512,x],yn=-24f+320f*eh[0,x];
   if(ys<.15f||yn<.15f)continue;
   float side=S(0f,20f,x)*S(0f,20f,511f-x);
   if(side<.001f)continue;
   for(int k=0;k<7;k++){
    float shared=.5f*(s[511,x,k]+e[0,x,k]);
    s[511,x,k]=shared;e[0,x,k]=shared;
    edgeGap=Mathf.Max(edgeGap,Mathf.Abs(s[511,x,k]-e[0,x,k]));
   }
  }
  if(edgeGap>.0001f||sourcePx<1000||extPx<1000)
   throw new Exception(n+" unified photo paint invalid src="+sourcePx+" ext="+extPx+" gap="+edgeGap);
  linked.SetAlphamaps(0,0,s);north.SetAlphamaps(0,0,e);
  linked.SetBaseMapDirty();north.SetBaseMapDirty();
  EditorUtility.SetDirty(linked);EditorUtility.SetDirty(north);
  File.AppendAllText(V+"unified_north_reference_paint_20260926.txt",
   n+" sourcePixels="+sourcePx+" extensionPixels="+extPx+
   " boundaryAlphaGap="+edgeGap.ToString("F6")+
   " canonicalSourceWrites=0 heightWrites=0"+
   " sourceTarget=y158..308 extensionTarget=y8..158\n");
 }
}
