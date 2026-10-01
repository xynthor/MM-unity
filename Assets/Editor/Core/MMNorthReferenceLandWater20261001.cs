using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMNorthReferenceLandWater20261001
{
 const string V="Validation/EdgeGrid20260923/";
 struct T { public string n,p; public T(string N,string P){n=N;p=P;} }
 static readonly T[] Z={
  new T("SweetWater","Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset"),
  new T("Kriegspire","Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset"),
  new T("FrozenHighlands","Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset"),
  new T("SilverCove","Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset")
 };
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static Color32[] Img(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)throw new Exception("Bad reference image "+p);var px=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return px;}
 static int Find(TerrainData d,string exact){return Array.FindIndex(d.terrainLayers,l=>l&&l.name==exact);}
 static void Normalize(float[,,]a,int z,int x,int L){float q=0;for(int k=0;k<L;k++)q+=a[z,x,k];if(q<.00001f)return;for(int k=0;k<L;k++)a[z,x,k]/=q;}
 static string ApplyOne(T q){
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(q.p);if(!d||d.heightmapResolution!=513)throw new Exception("Missing north terrain "+q.n);
  var mask=Img(V+"ReferenceMasks/"+q.n+"_North.png");
  var bio=Img(V+"CombinedNorthReferenceBiomes_20260927/"+q.n+"_North.png");
  var h=d.GetHeights(0,0,513,513);var old=(float[,])h.Clone();
  int lowered=0,raised=0;float maxMove=0;
  for(int z=1;z<513;z++)for(int x=1;x<512;x++){
   float side=S(0,18,x)*S(0,18,512-x);
   float south=S(0,26,z);
   float blendLock=side*south;if(blendLock<.001f)continue;
   float r=mask[z*513+x].r;
   float y=-24f+h[z,x]*320f,ny=y;
   if(r<145f){
    float wc=1f-S(108f,148f,r);
    float shore=S(72f,145f,r);
    float target=Mathf.Lerp(-6.8f,-.65f,shore);
    if(y>target+.03f){ny=Mathf.Lerp(y,target,wc*blendLock);if(ny<y-.003f)lowered++;}
   }else{
    float lc=S(145f,178f,r);
    float target=.75f+1.35f*lc;
    if(y<target){ny=Mathf.Lerp(y,target,lc*blendLock);if(ny>y+.003f)raised++;}
   }
   h[z,x]=Mathf.Clamp01((ny+24f)/320f);
   maxMove=Mathf.Max(maxMove,Mathf.Abs(h[z,x]-old[z,x])*320f);
  }
  // Exact lock on south and both lateral joins.
  for(int x=0;x<513;x++)h[0,x]=old[0,x];
  for(int z=0;z<513;z++){h[z,0]=old[z,0];h[z,512]=old[z,512];}
  float borderErr=0;for(int x=0;x<513;x++)borderErr=Mathf.Max(borderErr,Mathf.Abs(h[0,x]-old[0,x])*320f);
  for(int z=0;z<513;z++){borderErr=Mathf.Max(borderErr,Mathf.Abs(h[z,0]-old[z,0])*320f);borderErr=Mathf.Max(borderErr,Mathf.Abs(h[z,512]-old[z,512])*320f);}
  if(borderErr>.00001f||maxMove>48f)throw new Exception(q.n+" unsafe height pass border="+borderErr+" move="+maxMove);
  d.SetHeights(0,0,h);

  var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int L=d.alphamapLayers;
  int green=Find(d,"Realistic_Green"),light=Find(d,"Realistic_LightGreen"),desert=Find(d,"Realistic_Desert"),volc=Find(d,"Realistic_Volcanic"),arid=Find(d,"Realistic_Arid"),road=Find(d,"Realistic_RoadOverlay");
  var snow=Enumerable.Range(0,L).Where(k=>d.terrainLayers[k]&&d.terrainLayers[k].name=="Realistic_Snow").ToArray();
  var traceSnow=Enumerable.Range(0,L).Where(k=>d.terrainLayers[k]&&d.terrainLayers[k].name.IndexOf("Trace_Snow",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  var lava=Enumerable.Range(0,L).Where(k=>d.terrainLayers[k]&&d.terrainLayers[k].name.IndexOf("Lava",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
  if(green<0||light<0||desert<0||volc<0||arid<0||snow.Length==0)throw new Exception(q.n+" expected linked layers missing");
  int painted=0;double oldDes=0,newDes=0,oldVol=0,newVol=0,oldSnow=0,newSnow=0;
  int W=d.alphamapWidth,H=d.alphamapHeight;
  for(int z=0;z<H;z++)for(int x=0;x<W;x++){
   float ry=-24f+d.GetInterpolatedHeight(x/(float)(W-1),z/(float)(H-1));
   float land=S(142f,164f,mask[z*513+x].r);if(land<.02f||ry<.12f)continue;
   var p=bio[z*513+x];float sn=p.r/255f,ro=p.g/255f,fo=p.b/255f,oc=p.a/255f;
   float conf=(sn+ro+fo+oc)*.25f;if(conf<.012f)continue;
   float snowy=S(.18f,.52f,sn);float sw=Mathf.Lerp(.95f,1.72f,snowy)*sn;float rw=Mathf.Lerp(1.15f,.79f,snowy)*ro;float fw=1.75f*fo;float aw=.92f*oc;float dw=.025f*oc;
   // Steep northern slopes should read as exposed rock rather than green lawn.
   float slope=d.GetSteepness(x/(float)(W-1),z/(float)(H-1));if(slope>20f){float shift=fw*Mathf.Clamp01((slope-20f)/35f)*.72f;fw-=shift;rw+=shift;}
   float sum=sw+rw+fw+aw+dw;if(sum<.001f)continue;
   sw/=sum;rw/=sum;fw/=sum;aw/=sum;dw/=sum;
   float reserved=0;if(road>=0)reserved+=a[z,x,road];foreach(int k in lava)reserved+=a[z,x,k];reserved=Mathf.Clamp01(reserved);float avail=1-reserved;
   oldDes+=a[z,x,desert];oldVol+=a[z,x,volc];foreach(int k in snow)oldSnow+=a[z,x,k];
   var target=new float[L];target[green]=avail*fw*.62f;target[light]=avail*fw*.38f;target[volc]=avail*rw;target[arid]=avail*aw;target[desert]=avail*dw;target[snow[0]]=avail*sw;
   if(road>=0)target[road]=a[z,x,road];foreach(int k in lava)target[k]=a[z,x,k];
   float w=.93f*land*S(.012f,.055f,conf);
   for(int k=0;k<L;k++)a[z,x,k]=Mathf.Lerp(a[z,x,k],target[k],w);
   Normalize(a,z,x,L);
   newDes+=a[z,x,desert];newVol+=a[z,x,volc];foreach(int k in snow)newSnow+=a[z,x,k];
   painted++;
  }
  d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
  return q.n+" loweredWaterVertices="+lowered+" raisedLandVertices="+raised+" maxHeightMoveM="+maxMove.ToString("F2")+" borderErrorM="+borderErr.ToString("F6")+" painted="+painted+" desertMeanTouched="+(oldDes/Math.Max(1,painted)).ToString("F3")+"->"+(newDes/Math.Max(1,painted)).ToString("F3")+" volcanicMeanTouched="+(oldVol/Math.Max(1,painted)).ToString("F3")+"->"+(newVol/Math.Max(1,painted)).ToString("F3")+" snowMeanTouched="+(oldSnow/Math.Max(1,painted)).ToString("F3")+"->"+(newSnow/Math.Max(1,painted)).ToString("F3");
 }
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);var rows=new List<string>();
  foreach(var q in Z)rows.Add(ApplyOne(q));
  AssetDatabase.SaveAssets();
  // Rebuild the already-installed lava mesh against the corrected terrain.
  if(s.GetRootGameObjects().Any(g=>g.name=="Enroth Source-Registered Lava 20261001"))MMEnrothLavaOverlays20261001.BuildAssets();
  Directory.CreateDirectory(V+"NorthTerrainQA20261001");File.WriteAllLines(V+"NorthTerrainQA20261001/reference_land_water_pass.txt",rows);
  Debug.Log("NORTH_REFERENCE_LAND_WATER_COMPLETE "+string.Join(" | ",rows));
 }
}
