using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Native layers reset their UV phase at every 512m Unity terrain tile.
// Keep ORIGINAL TerrainLayers intact; generated northern extensions use
// identical diffuse and normal textures with the missing 512m z phase.
public static class MMNorthTexturePhaseAlign20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Root="Assets/World/WorldExtensions/Generated/NorthernPhaseLayers";
 static void EnsureFolder(){
  if(!AssetDatabase.IsValidFolder(Root))
   AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","NorthernPhaseLayers");
 }
 public static TerrainLayer Aligned(string n,int index,TerrainLayer source){
  if(!source||source.tileSize.y<.01f)throw new Exception(n+" missing phase source layer "+index);
  EnsureFolder();
  string path=Root+"/"+n+"_"+index+"_NorthPhaseAligned.terrainlayer";
  var result=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
  if(!result){
   result=new TerrainLayer();
   AssetDatabase.CreateAsset(result,path);
  }
  EditorUtility.CopySerialized(source,result);
  result.name=source.name;
  result.tileOffset=new Vector2(source.tileOffset.x,
   source.tileOffset.y+Mathf.Repeat(512f,source.tileSize.y));
  if(result.diffuseTexture!=source.diffuseTexture||
     result.normalMapTexture!=source.normalMapTexture||
     result.tileSize!=source.tileSize)
   throw new Exception(n+" phase clone changed original material "+index);
  float before=Mathf.Repeat(512f+source.tileOffset.y,source.tileSize.y);
  float after=Mathf.Repeat(result.tileOffset.y,source.tileSize.y);
  float gap=Mathf.Abs(before-after);
  if(Mathf.Min(gap,source.tileSize.y-gap)>.001f)
   throw new Exception(n+" incorrect north texture phase "+index+" "+gap);
  EditorUtility.SetDirty(result);
  return result;
 }
 public static void Apply(string name,TerrainData source,TerrainData extension){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeNorthernTexturePhase20260926\manifest.json"))
   throw new Exception("Northern texture-phase restore snapshot not found");
  if(source.terrainLayers.Length!=extension.terrainLayers.Length)
   throw new Exception(name+" mismatched northern material counts");
  var layers=new TerrainLayer[source.terrainLayers.Length];
  float maxPhaseError=0f;int aligned=0;
  for(int k=0;k<layers.Length;k++){
   var native=source.terrainLayers[k];
   var existing=extension.terrainLayers[k];
   var phase=Aligned(name,k,native);
   if(existing!=native&&existing!=phase)
    throw new Exception(name+" unexpected existing material "+k);
   layers[k]=phase;
   float t=native.tileSize.y;
   float before=Mathf.Repeat(512f+native.tileOffset.y,t);
   float after=Mathf.Repeat(phase.tileOffset.y,t);
   float gap=Mathf.Abs(before-after);
   maxPhaseError=Mathf.Max(maxPhaseError,Mathf.Min(gap,t-gap));
   aligned++;
  }
  extension.terrainLayers=layers;
  EditorUtility.SetDirty(extension);
  AssetDatabase.SaveAssets();
  if(maxPhaseError>.001f)throw new Exception(name+" north tile seam texture phase error "+maxPhaseError);
  File.AppendAllText(V+"northern_texture_phase_20260926.txt",
   name+" originalDiffuseAndNormal=EXACT tilingLayers="+aligned+
   " maxZPhaseMismatchM="+maxPhaseError.ToString("F6")+
   " canonicalLayersUntouched=true\n");
 }
}
