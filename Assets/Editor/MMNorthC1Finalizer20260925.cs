using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Linked-world-only C1 completion of the four canonical/north terrain joins.
// Neither the original scenes nor their TerrainData are edited.
public static class MMNorthC1Finalizer20260925 {
 const string G="Assets/World/WorldExtensions/Generated/";
 const string V="Validation/EdgeGrid20260923/";
 const string B=@"C:\MMUnityPort\Backups\BeforeLinkedNorthC1Finalizer_20260925\manifest.json";
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);}
 static float H(float a,float b,float da,float db,float t,int span){
  float t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1f)*a+(t3-2*t2+t)*da*span+
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
 static float Height(float[,] a,float u,float v){
  float xx=Mathf.Clamp01(u)*512f,zz=Mathf.Clamp01(v)*512f;
  int x=Mathf.Min(511,Mathf.FloorToInt(xx)),z=Mathf.Min(511,Mathf.FloorToInt(zz));
  float fx=xx-x,fz=zz-z;
  return Mathf.Lerp(Mathf.Lerp(a[z,x],a[z,x+1],fx),
     Mathf.Lerp(a[z+1,x],a[z+1,x+1],fx),fz)*320f;
 }
 public static void Apply(GameObject linked,string label){
  string n=Name(label);if(n==null)return;
  if(!File.Exists(B))throw new Exception("Required C1 source-protection snapshot missing");
  var t=linked.GetComponentInChildren<Terrain>(true);
  var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
  var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
  if(!t||!src||!ext||src.heightmapResolution!=513||ext.heightmapResolution!=513)
   throw new Exception(n+" C1 terrain source missing");
  bool silver=n=="SilverCove";
  string path=G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset";
  var copy=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
  if(!silver&&(!copy||t.terrainData!=copy))
   throw new Exception(n+" linked source clone not prepared by base pass");
  if(silver&&t.terrainData!=src)
   throw new Exception("Silver Cove linked root already changed unexpectedly");
  var current=silver?src:copy;
  int band=silver?112:72;
  var h=current.GetHeights(0,0,513,513);
  var prior=(float[,])h.Clone();
  var s=src.GetHeights(0,0,513,513);var e=ext.GetHeights(0,0,513,2);
  var img=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!img.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+n+"_North.png"))||
    img.width!=513||img.height!=513)throw new Exception(n+" source-map coast mask missing");
  var land=img.GetPixels32();UnityEngine.Object.DestroyImmediate(img);
  var severity=new float[513];
  for(int x=8;x<=504;x++){
   float edge=-24f+h[512,x]*320f,inside=-24f+h[512-band,x]*320f;
   if(land[16*513+x].r<147||edge<.15f||inside<3f)continue;
   float jump=Mathf.Abs((h[512,x]-h[511,x]-(e[1,x]-e[0,x]))*320f);
   severity[x]=S(.18f,.9f,jump)*S(143f,156f,land[16*513+x].r);
  }
  int selected=0,changed=0,seaRisk=0;float maxMove=0,maxBefore=0,maxSelectedAfter=0;
  for(int x=8;x<=504;x++){
   float total=0,v=0;
   for(int dx=-8;dx<=8;dx++){float k=9-Mathf.Abs(dx);total+=k;v+=k*severity[x+dx];}
   float weight=S(.04f,.34f,v/total)*S(8f,28f,x)*S(8f,28f,512f-x);
   float mismatch=Mathf.Abs((h[512,x]-h[511,x]-(e[1,x]-e[0,x]))*320f);
   maxBefore=Mathf.Max(maxBefore,mismatch);
   if(weight<.01f)continue;
   selected++;
   int start=512-band;
   float a=h[start,x],b=h[512,x];
   float da=(h[start+1,x]-h[start-1,x])*.5f,db=e[1,x]-e[0,x];
   float lo=Mathf.Min(a,b)-.2f/320f,hi=Mathf.Max(a,b)+.5f/320f;
   for(int z=start+1;z<512;z++){
    float ratio=(z-start)/(float)band;
    float target=Mathf.Clamp(H(a,b,da,db,ratio,band),lo,hi);
    float old=h[z,x],next=Mathf.Lerp(old,target,weight);
    float move=Mathf.Abs(next-old)*320f;
    if(move>.003f)changed++;
    maxMove=Mathf.Max(maxMove,move);
    if(-24f+next*320f<-.10f&&-24f+old*320f>.15f)seaRisk++;
    h[z,x]=next;
   }
   maxSelectedAfter=Mathf.Max(maxSelectedAfter,
     Mathf.Abs((h[512,x]-h[511,x]-db)*320f));
  }
  float sourceEdge=0,extensionEdge=0,side=0,globalJump=0;
  for(int x=0;x<513;x++){
   sourceEdge=Mathf.Max(sourceEdge,Mathf.Abs(h[512,x]-s[512,x])*320f);
   extensionEdge=Mathf.Max(extensionEdge,Mathf.Abs(h[512,x]-e[0,x])*320f);
   globalJump=Mathf.Max(globalJump,
     Mathf.Abs((h[512,x]-h[511,x])-(e[1,x]-e[0,x]))*320f);
  }
  for(int z=0;z<513;z++){
   side=Mathf.Max(side,Mathf.Abs(h[z,0]-s[z,0])*320f);
   side=Mathf.Max(side,Mathf.Abs(h[z,512]-s[z,512])*320f);
  }
  if(selected<15||changed<100||seaRisk>0||maxMove>30f||
     sourceEdge>.0001f||extensionEdge>.02f||side>.0001f)
   throw new Exception(n+" C1 candidate rejected columns="+selected+
    " flood="+seaRisk+" move="+maxMove+" originalBorder="+sourceEdge+
    " northBorder="+extensionEdge+" side="+side);
  if(!copy){
   if(!silver)throw new Exception("Existing linked profile absent "+n);
   copy=UnityEngine.Object.Instantiate(src);
   copy.name=n+"_LinkedNorthProfile";
   AssetDatabase.CreateAsset(copy,path);
  }
  if(copy.heightmapResolution!=513||copy.alphamapResolution!=src.alphamapResolution||
     copy.alphamapLayers!=src.alphamapLayers)
   throw new Exception(n+" linked terrain clone incompatible");
  // Exact source splat data on the clone, never Unity's default all-green map.
  copy.terrainLayers=src.terrainLayers;
  copy.SetAlphamaps(0,0,src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight));
  copy.SetHeights(0,0,h);EditorUtility.SetDirty(copy);
  t.terrainData=copy;
  var collider=t.GetComponent<TerrainCollider>();
  if(collider)collider.terrainData=copy;
  t.Flush();
  int grounded=0;float maxPropMove=0f;const float eps=.002f;
  string[] groups={"Vegetation - Source Anchored Final",
   "Vegetation - Preserved User Additions","Region Dressing - Reference Matched"};
  var sources=linked.GetComponentsInChildren<Transform>(true)
    .Where(q=>groups.Contains(q.name)).ToArray();
  foreach(var group in sources)foreach(Transform child in group){
   float u=(child.position.x-t.transform.position.x)/512f;
   float v=(child.position.z-t.transform.position.z)/512f;
   if(u<0f||u>1f||v<0f||v>1f||v<(512f-band)/512f)continue;
   float dy=copy.GetInterpolatedHeight(u,v)-Height(prior,u,v);
   if(Mathf.Abs(dy)<eps)continue;
   child.position+=new Vector3(0f,dy,0f);
   maxPropMove=Mathf.Max(maxPropMove,Mathf.Abs(dy));grounded++;
  }
  string line=n+",C1_source_border_m="+sourceEdge.ToString("F6")+
   ",C1_extension_border_m="+extensionEdge.ToString("F6")+
   ",C1_side_borders_m="+side.ToString("F6")+
   ",affected_columns="+selected+",affected_vertices="+changed+
   ",max_height_change_m="+maxMove.ToString("F3")+
   ",max_full_border_slope_before="+maxBefore.ToString("F3")+
   ",max_selected_border_slope_after="+maxSelectedAfter.ToString("F3")+
   ",max_full_border_slope_after="+globalJump.ToString("F3")+
   ",new_flooded_cells="+seaRisk+
   ",regrounded_vegetation="+grounded+",largest_prop_move_m="+maxPropMove.ToString("F3")+
   ",source_scenes_untouched=true";
  File.AppendAllText(V+"linked_north_c1_transition_20260925.csv",line+"\n");
  Debug.Log("LINKED_C1_NORTH_JOIN "+line);
 }
}
