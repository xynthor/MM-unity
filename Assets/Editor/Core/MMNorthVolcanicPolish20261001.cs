using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class MMNorthVolcanicPolish20261001
{
 struct Q{public string n,p;public Q(string N,string P){n=N;p=P;}}
 static readonly Q[] Z={
  new Q("SweetWater","Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset"),
  new Q("Kriegspire","Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset"),
  new Q("FrozenHighlands","Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset"),
  new Q("SilverCove","Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset")
 };
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static Color32[] Mask(string n){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var p="Validation/EdgeGrid20260923/ReferenceMasks/"+n+"_North.png";if(!t.LoadImage(File.ReadAllBytes(p)))throw new Exception(p);var a=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return a;}
 static int F(TerrainData d,string s)=>Array.FindIndex(d.terrainLayers,l=>l&&l.name==s);
 static void Norm(float[,,]a,int z,int x,int L){float s=0;for(int k=0;k<L;k++)s+=a[z,x,k];if(s<.0001f)return;for(int k=0;k<L;k++)a[z,x,k]/=s;}
 static string ApplyOne(Q q){
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(q.p);if(!d)throw new Exception("missing "+q.n);
  var m=Mask(q.n);var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int L=d.alphamapLayers;
  int volc=F(d,"Realistic_Volcanic"),arid=F(d,"Realistic_Arid"),road=F(d,"Realistic_RoadOverlay");
  var snow=Enumerable.Range(0,L).Where(k=>d.terrainLayers[k]&&(d.terrainLayers[k].name=="Realistic_Snow"||d.terrainLayers[k].name.IndexOf("Trace_Snow",StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
  if(volc<0||arid<0||snow.Length==0)throw new Exception(q.n+" layers missing");
  float frac=q.n=="Kriegspire"?.72f:q.n=="FrozenHighlands"?.48f:q.n=="SweetWater"?.34f:.10f;
  float vshare=q.n=="Kriegspire"?.88f:q.n=="FrozenHighlands"?.68f:q.n=="SweetWater"?.74f:.42f;
  int moved=0,vent=0;double movedWeight=0;
  int W=d.alphamapWidth,H=d.alphamapHeight;
  for(int z=0;z<H;z++)for(int x=0;x<W;x++){
   float land=S(145,170,m[z*513+x].r);if(land<.03f)continue;
   if(road>=0&&a[z,x,road]>.25f)continue;
   float y=-24f+d.GetInterpolatedHeight(x/(float)(W-1),z/(float)(H-1));
   float low=1-S(24,62,y);if(low<.01f)continue;
   float st=0;foreach(int k in snow)st+=a[z,x,k];if(st<.015f)continue;
   float mv=st*frac*low*land;if(mv<.003f)continue;
   foreach(int k in snow)a[z,x,k]*=(st-mv)/st;
   a[z,x,volc]+=mv*vshare;a[z,x,arid]+=mv*(1-vshare);Norm(a,z,x,L);
   moved++;movedWeight+=mv;
  }
  if(q.n=="SweetWater"||q.n=="Kriegspire"){
   int cx=q.n=="Kriegspire"?189:462,cz=q.n=="Kriegspire"?331:263;float rx=q.n=="Kriegspire"?86:62,rz=q.n=="Kriegspire"?74:54;
   for(int z=Mathf.Max(2,cz-(int)rz-20);z<Mathf.Min(H-2,cz+(int)rz+20);z++)for(int x=Mathf.Max(2,cx-(int)rx-20);x<Mathf.Min(W-2,cx+(int)rx+20);x++){
    float rr=Mathf.Sqrt(Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((z-cz)/rz,2));float ring=S(.28f,.58f,rr)*(1-S(1.02f,1.34f,rr));if(ring<.01f)continue;
    float land=S(145,170,m[z*513+x].r);if(land<.05f)continue;float goal=Mathf.Lerp(a[z,x,volc],.94f,ring*.92f*land);float del=goal-a[z,x,volc],rest=1-a[z,x,volc];if(del<=0||rest<=.0001f)continue;float nr=1-goal;
    for(int k=0;k<L;k++)if(k!=volc)a[z,x,k]*=nr/rest;a[z,x,volc]=goal;Norm(a,z,x,L);vent++;
   }
  }
  d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
  return q.n+" snowShiftPixels="+moved+" meanShift="+(movedWeight/Math.Max(1,moved)).ToString("F3")+" ventPixels="+vent;
 }
 public static void Run(){var rows=Z.Select(ApplyOne).ToArray();AssetDatabase.SaveAssets();Directory.CreateDirectory("Validation/EdgeGrid20260923/NorthTerrainQA20261001");File.WriteAllLines("Validation/EdgeGrid20260923/NorthTerrainQA20261001/volcanic_polish.txt",rows);Debug.Log("NORTH_VOLCANIC_POLISH_DONE "+string.Join(" | ",rows));}
}
