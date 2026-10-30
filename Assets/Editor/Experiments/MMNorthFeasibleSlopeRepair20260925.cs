using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Fix only the source-compatible portions of the four original/north joins.
// Cliff toes where the original southward slope requires submerged land are excluded.
public static class MMNorthFeasibleSlopeRepair20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthDrySourceSlopeRestore_20260925\manifest.json";
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);
 }
 [MenuItem("MMUnity/Reference 2026/Repair Feasible Source-North Slope Seams")]
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Required backup missing");
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/World/Enroth.unity"||sc.isDirty)
   throw new Exception("Saved linked world must be active");
  var data=new List<TerrainData>();var candidates=new List<float[,]>();var reports=new List<string>();
  foreach(var name in Names){
   var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+name+"_NorthTerrain.asset");
   var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+name+"/Generated/"+name+"Terrain.asset");
   if(!source||!ext)throw new Exception("Missing source/extension "+name);
   var h=ext.GetHeights(0,0,513,513);var orig=source.GetHeights(0,511,513,2);
   var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+name+"_North.png"));
   if(tex.width!=513||tex.height!=513)throw new Exception("Bad reference mask "+name);
   var px=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);
   float before=0,after=0,maxMove=0,edgeError=0;int changed=0,skippedCliffs=0;
   for(int x=8;x<=504;x++){
    float srcSlope=(orig[1,x]-orig[0,x])*320f;
    float curSlope=(h[1,x]-h[0,x])*320f;
    float edge=-24f+320f*h[0,x],interior=-24f+320f*h[24,x];
    if(px[16*513+x].r<145||edge<.15f||interior<1f)continue;
    float mismatch=Mathf.Abs(srcSlope-curSlope);
    if(edge+srcSlope<.15f){if(mismatch>.5f)skippedCliffs++;continue;}
    before=Mathf.Max(before,mismatch);
    if(mismatch<.05f)continue;
    float fade=S(0f,32f,x)*S(0f,32f,512f-x);
    var delta=srcSlope-curSlope;
    for(int z=1;z<=16;z++){
     float weight=1f-S(1f,16f,z);
     float prior=h[z,x],next=prior+delta/320f*weight*fade;
     if(px[z*513+x].r>=155 && -24f+320f*next<.12f)continue;
     h[z,x]=Mathf.Clamp01(next);
     if(Mathf.Abs(h[z,x]-prior)*320f>.001f)changed++;
     maxMove=Mathf.Max(maxMove,Mathf.Abs(h[z,x]-prior)*320f);
    }
   }
   for(int x=0;x<513;x++){
    edgeError=Mathf.Max(edgeError,Mathf.Abs(h[0,x]-orig[1,x])*320f);
    if(x<8||x>504)continue;
    float srcSlope=(orig[1,x]-orig[0,x])*320f;
    float edge=-24f+320f*h[0,x],interior=-24f+320f*h[24,x];
    if(px[16*513+x].r<145||edge<.15f||interior<1f||edge+srcSlope<.15f)continue;
    after=Mathf.Max(after,Mathf.Abs((h[1,x]-h[0,x])*320f-srcSlope));
   }
   if(edgeError>.02f||maxMove>2f||after>Mathf.Max(before,.1f)+.001f)
    throw new Exception(name+" unsafe candidate edge="+edgeError+" move="+maxMove+
      " feasibleBefore="+before+" feasibleAfter="+after);
   data.Add(ext);candidates.Add(h);
   reports.Add(name+"_North feasibleSlopeJumpBefore="+before.ToString("F4")+
    " feasibleSlopeJumpAfter="+after.ToString("F4")+
    " adjustedVertices="+changed+" maxMoveM="+maxMove.ToString("F4")+
    " skippedSourceCliffColumns="+skippedCliffs+" sourceEdgeErrorM="+edgeError.ToString("F6"));
  }
  for(int i=0;i<data.Count;i++){data[i].SetHeights(0,0,candidates[i]);EditorUtility.SetDirty(data[i]);}
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"north_source_feasible_slope_repair_20260925.csv",reports);
  Debug.Log("NORTH_FEASIBLE_SLOPE_REPAIR "+string.Join(" | ",reports));
 }
}
[InitializeOnLoad]
internal static class MMNorthFeasibleSlopeRepairQueued20260925{
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthFeasibleSlopeRepairQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_FEASIBLE_SLOPE_REPAIR.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_NORTH_FEASIBLE_SLOPE_REPAIR.flag");
  try{
   MMNorthFeasibleSlopeRepair20260925.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"north_feasible_slope_repair_runtime.txt","PASS four generated northern tiles refined, linked rebuilt\n");
  }catch(Exception e){
   File.WriteAllText(V+"north_feasible_slope_repair_runtime.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
