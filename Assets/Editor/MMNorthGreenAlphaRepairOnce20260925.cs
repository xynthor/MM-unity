using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthGreenAlphaRepairOnce20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string B=@"C:\MMUnityPort\Backups\BeforeNorthGreenAlphaRecovery_20260925\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands"};
 static MMNorthGreenAlphaRepairOnce20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_GREEN_ALPHA_REPAIR.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_NORTH_GREEN_ALPHA_REPAIR.flag");
  var lines=new List<string>();
  try{
   if(!File.Exists(B))throw new Exception("Verified pre-repair backup missing");
   var sources=new List<TerrainData>();var clones=new List<TerrainData>();
   foreach(var n in N){
    var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var dst=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    if(!src||!dst||src.alphamapResolution!=dst.alphamapResolution||
      src.alphamapLayers!=dst.alphamapLayers||
      src.terrainLayers.Length!=dst.terrainLayers.Length)
     throw new Exception(n+" mismatching terrain splatmaps or missing assets");
    sources.Add(src);clones.Add(dst);
   }
   for(int i=0;i<N.Length;i++){
    var src=sources[i];var dst=clones[i];
    var original=src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight);
    var previous=dst.GetAlphamaps(0,0,dst.alphamapWidth,dst.alphamapHeight);
    float worstBefore=0;double firstLayerBefore=0;
    for(int z=0;z<src.alphamapHeight;z+=4)for(int x=0;x<src.alphamapWidth;x+=4){
     firstLayerBefore+=previous[z,x,0];
     for(int k=0;k<src.alphamapLayers;k++)
      worstBefore=Mathf.Max(worstBefore,Mathf.Abs(original[z,x,k]-previous[z,x,k]));
    }
    dst.terrainLayers=src.terrainLayers;
    dst.SetAlphamaps(0,0,original);
    var readback=dst.GetAlphamaps(0,0,dst.alphamapWidth,dst.alphamapHeight);
    float worstAfter=0;double firstLayerAfter=0;
    for(int z=0;z<src.alphamapHeight;z+=4)for(int x=0;x<src.alphamapWidth;x+=4){
     firstLayerAfter+=readback[z,x,0];
     for(int k=0;k<src.alphamapLayers;k++)
      worstAfter=Mathf.Max(worstAfter,Mathf.Abs(original[z,x,k]-readback[z,x,k]));
    }
    if(worstAfter>.002f)throw new Exception(N[i]+" color restoration failed max alpha error="+worstAfter);
    EditorUtility.SetDirty(dst);
    lines.Add(N[i]+" RESTORED linked-only original splatmaps; greenWeightBefore="+
      (firstLayerBefore/16384).ToString("F4")+" greenWeightAfter="+
      (firstLayerAfter/16384).ToString("F4")+
      " maxBeforeError="+worstBefore.ToString("F5")+
      " maxAfterError="+worstAfter.ToString("F6")+
      " originalSourceUntouched=true");
   }
   AssetDatabase.SaveAssets();
   File.WriteAllLines(V+"north_green_alpha_repair_20260925.txt",lines);
   File.WriteAllText(V+"north_green_alpha_repair_runtime.txt","PASS three generated linked-only terrains restored exact source alpha maps\n");
   Debug.Log("NORTH_GREEN_ALPHA_REPAIRED "+string.Join(" | ",lines));
   SceneView.RepaintAll();
  }catch(Exception e){
   File.WriteAllText(V+"north_green_alpha_repair_runtime.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}

// completed file for safe domain reload
