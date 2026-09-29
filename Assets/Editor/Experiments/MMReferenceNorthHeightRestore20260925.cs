using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMReferenceNorthHeightRestore20260925 {
 const string G="Assets/World/WorldExtensions/Generated/";
 const string V="Validation/EdgeGrid20260923/";
 const float H=320f;
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);}
 static float Hermite(float a,float b,float ma,float mb,float t,float span){
  float t2=t*t,t3=t2*t;
  return (2*t3-3*t2+1)*a+(t3-2*t2+t)*ma*span+
         (-2*t3+3*t2)*b+(t3-t2)*mb*span;
 }
 static float World(float y)=>-24f+320f*y;
 [MenuItem("MMUnity/Reference 2026/Repair Source-North Height Profiles")]
 public static void Apply(){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeFullReferenceBiomeAndNorthHeight_20260925\manifest.json"))
   throw new Exception("Expected immutable backup before applying north height repair");
  if(SceneManager.GetActiveScene().isDirty)
   throw new Exception("Active scene has unsaved edits; height reconstruction aborted");
  var candidates=new Dictionary<TerrainData,float[,]>();
  var report=new List<string>();
  foreach(string n in Names){
   var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
   var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
   if(!ext||!src||ext.heightmapResolution!=513||src.heightmapResolution!=513)
    throw new Exception("Missing source/generated terrain "+n);
   var h=ext.GetHeights(0,0,513,513);
   var s=src.GetHeights(0,511,513,2);
   float beforePits=0,afterPits=0,maxMove=0,maxSeam=0,maxSlope=0;int touched=0;
   for(int x=0;x<513;x++){
    float seam=s[1,x],sourceSlope=s[1,x]-s[0,x];
    maxSeam=Mathf.Max(maxSeam,Mathf.Abs(h[0,x]-seam)*H);
    maxSlope=Mathf.Max(maxSlope,Mathf.Abs((h[1,x]-h[0,x])-sourceSlope)*H);
    float oldMin=999f;
    for(int q=2;q<=24;q++)oldMin=Mathf.Min(oldMin,h[q,x]);
    float oldPit=Mathf.Max(0f,(Mathf.Min(h[1,x],h[64,x])-oldMin)*H);
    beforePits=Mathf.Max(beforePits,oldPit);
    float rise=(h[64,x]-h[0,x])*H;
    if(rise<6f || World(h[64,x])<3f)continue;
    float severity=S(.7f,4f,oldPit);
    float weight=.35f+.65f*severity;
    float start=h[1,x],end=h[64,x];
    float mStart=Mathf.Clamp(h[1,x]-h[0,x],-.00156f,.00156f);
    float mEnd=Mathf.Clamp((h[65,x]-h[63,x])*.5f,-.003f,.003f);
    for(int q=2;q<64;q++){
     float candidate=Mathf.Clamp01(Hermite(start,end,mStart,mEnd,(q-1f)/63f,63f));
     float prior=h[q,x],next=Mathf.Lerp(prior,candidate,weight);
     maxMove=Mathf.Max(maxMove,Mathf.Abs(next-prior)*H);
     if(Mathf.Abs(next-prior)*H>.005f)touched++;
     h[q,x]=next;
    }
    float newMin=999f;
    for(int q=2;q<=24;q++)newMin=Mathf.Min(newMin,h[q,x]);
    afterPits=Mathf.Max(afterPits,Mathf.Max(0f,(Mathf.Min(h[1,x],h[64,x])-newMin)*H));
   }
   if(maxSeam>.1f||maxSlope>.1f||maxMove>30f)
    throw new Exception(n+" north height reconstruction rejected seam="+maxSeam+" slope="+maxSlope+" move="+maxMove);
   candidates.Add(ext,h);
   report.Add(n+"_North,vertices="+touched+",old_additional_pit_m="+beforePits.ToString("F3")+
    ",new_additional_pit_m="+afterPits.ToString("F3")+",max_height_move_m="+maxMove.ToString("F3")+
    ",source_edge_m="+maxSeam.ToString("F5")+",source_slope_m_per_m="+maxSlope.ToString("F5"));
  }
  foreach(var pair in candidates){pair.Key.SetHeights(0,0,pair.Value);EditorUtility.SetDirty(pair.Key);}
  MMWorldExtensionInternalSeamLock.Apply();
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"north_height_reference_reconstruction_20260925.csv",report);
  Debug.Log("SOURCE_NORTH_HEIGHT_RECONSTRUCTION_COMPLETE "+string.Join(" | ",report));
 }
}
[InitializeOnLoad]
internal static class MMReferenceHeightQueued20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMReferenceHeightQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_HEIGHT_RECONSTRUCTION.flag"))return;
  File.Delete(V+"RUN_NORTH_HEIGHT_RECONSTRUCTION.flag");
  try{
   MMReferenceNorthHeightRestore20260925.Apply();
   File.WriteAllText(V+"north_height_reconstruction_runtime.txt","PASS saved_generated_terrain=true\n");
  }catch(Exception ex){
   File.WriteAllText(V+"north_height_reconstruction_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
