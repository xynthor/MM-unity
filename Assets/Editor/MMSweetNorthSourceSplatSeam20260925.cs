using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
[InitializeOnLoad]
internal static class MMSweetNorthSourceSplatSeam20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMSweetNorthSourceSplatSeam20260925(){EditorApplication.delayCall+=Run;}
 static float S(float a,float b,float t){
  float u=Mathf.Clamp01((t-a)/(b-a));return u*u*(3f-2f*u);
 }
 static void Run(){
  if(!File.Exists(V+"RUN_SWEET_NORTH_SOURCE_SPLAT_SEAM.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_SWEET_NORTH_SOURCE_SPLAT_SEAM.flag");
  try{
   const string backup=@"C:\MMUnityPort\Backups\BeforeAdaptiveNorthToePolish_20260925\manifest.json";
   if(!File.Exists(backup))throw new Exception("Pre-change terrain backup missing");
   var scene=SceneManager.GetActiveScene();
   if(scene.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||scene.isDirty)
    throw new Exception("Saved linked world required");
   var src=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/SweetWater/Generated/SweetWaterTerrain.asset");
   var dst=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/WorldExtensions/Generated/SweetWater_NorthTerrain.asset");
   if(!src||!dst||src.alphamapResolution!=512||dst.alphamapResolution!=512||
      src.alphamapLayers!=dst.alphamapLayers)
    throw new Exception("Source/new Sweet Water splatmap dimensions mismatch");
   var s=src.GetAlphamaps(0,511,512,1);
   var a=dst.GetAlphamaps(0,0,512,512);
   for(int k=0;k<src.alphamapLayers;k++){
    if(src.terrainLayers[k]!=dst.terrainLayers[k])
     throw new Exception("Mismatched layer "+k+" for source-border material transition");
   }
   double oldGrass=0,newGrass=0;float error=0;int count=0;
   for(int z=0;z<=36;z++){
    float blend=1f-S(0f,36f,z);
    for(int x=0;x<512;x++){
     oldGrass+=a[z,x,0]+a[z,x,1];
     for(int k=0;k<dst.alphamapLayers;k++)
      a[z,x,k]=Mathf.Lerp(a[z,x,k],s[0,x,k],blend);
     newGrass+=a[z,x,0]+a[z,x,1];count++;
    }
   }
   for(int x=0;x<512;x++)for(int k=0;k<dst.alphamapLayers;k++)
    error=Mathf.Max(error,Mathf.Abs(a[0,x,k]-s[0,x,k]));
   if(error>.0001f)throw new Exception("Photo forest root does not match original material boundary");
   dst.SetAlphamaps(0,0,a);EditorUtility.SetDirty(dst);AssetDatabase.SaveAssets();
   string report="PASS original_source_splat_match_error="+error.ToString("F6")+
    " transitionWidthM=36 greenMeanBefore="+(oldGrass/count).ToString("F4")+
    " greenMeanAfter="+(newGrass/count).ToString("F4")+
    " northFootForestBeyond36mPreserved=true originalSceneAndHeightsUntouched=true";
   File.WriteAllText(V+"sweet_north_source_splat_transition_20260925.txt",report+"\n");
   Debug.Log("SWEET_NORTH_SNOW_FOREST_SPLAT_SEAM "+report);
  }catch(Exception e){
   File.WriteAllText(V+"sweet_north_source_splat_transition_20260925.txt","FAIL "+e+"\n");
   Debug.LogError(e);
  }
 }
}
