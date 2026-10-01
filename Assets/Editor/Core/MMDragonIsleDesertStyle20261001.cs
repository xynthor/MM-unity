using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMDragonIsleDesertStyle20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string Root="Assets/World/WorldExtensions/Generated/DesertStyle20261001";
 const string Template="Assets/World/WorldExtensions/Generated/LinkedWorldTexturePhase20260929/93b2b2aa74ad40546ae5b165f4ff17c0_1.terrainlayer";
 static readonly string[] Parents={
  "Dragonsand - LINKED REFERENCE",
  "Dragonsand South - LINKED EXTENSION",
  "Blackshire - LINKED REFERENCE",
  "Hermits Isle - LINKED REFERENCE",
  "Hermits Isle West - LINKED EXTENSION",
  "Hermits Isle South - LINKED EXTENSION"
 };
 static readonly string[] Originals={
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/837f2eae42b28b14397dc749fe3ff18e.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/592bc26e8619f5f448cc76ff7f7e3f20.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/0a93b329871131147b5b239aeb0969c3.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/f0407a8ed9f08cb4a8fe9d603a04a21f.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/33be81ce19178794b9f480b1e769f79f.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/924f64977ef5f074eb754e5ca519da71.asset"
 };
 static string Safe(string s){foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return s.Replace(" - LINKED REFERENCE","").Replace(" - LINKED EXTENSION","").Replace(" ","");}
 static int SandLayer(TerrainData d){
  for(int i=0;i<d.terrainLayers.Length;i++){
   var l=d.terrainLayers[i];if(!l||!l.diffuseTexture)continue;
   string p=AssetDatabase.GetAssetPath(l.diffuseTexture);
   if(p.IndexOf("coast_sand_02_diff_1k.jpg",StringComparison.OrdinalIgnoreCase)>=0)return i;
  }
  return -1;
 }
 static TerrainLayer MakeLayer(string key,Vector3 pos){
  string p=Root+"/"+key+"_DragonIsleSand.terrainlayer";
  var dst=AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
  if(dst)return dst;
  var src=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Template);if(!src)throw new Exception("Dragon Isle sand template missing");
  dst=UnityEngine.Object.Instantiate(src);dst.name=key+"_DragonIsleSand";
  dst.tileSize=new Vector2(9f,9f);
  dst.tileOffset=new Vector2(Mathf.Repeat(pos.x,9f),Mathf.Repeat(pos.z,9f));
  AssetDatabase.CreateAsset(dst,p);return dst;
 }
 public static void Run(){
  var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(!AssetDatabase.IsValidFolder(Root)){
   if(!AssetDatabase.IsValidFolder("Assets/World/WorldExtensions/Generated"))throw new Exception("Generated folder missing");
   AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","DesertStyle20261001");
  }
  var rows=new List<string>();
  for(int i=0;i<Parents.Length;i++){
   string parentName=Parents[i];
   var parent=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==parentName);
   if(!parent)throw new Exception("Missing region "+parentName);
   var t=parent.GetComponentInChildren<Terrain>(true);if(!t)throw new Exception("Missing terrain "+parentName);
   var src=AssetDatabase.LoadAssetAtPath<TerrainData>(Originals[i]);if(!src)throw new Exception("Missing original terrain "+Originals[i]);
   string key=Safe(parentName),terrainPath=Root+"/"+key+"_Terrain.asset";
   var d=AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath);
   if(!d){d=UnityEngine.Object.Instantiate(src);d.name=src.name;AssetDatabase.CreateAsset(d,terrainPath);}
   if(d.heightmapResolution!=src.heightmapResolution||d.alphamapResolution!=src.alphamapResolution||d.alphamapLayers!=src.alphamapLayers)
    throw new Exception("Clone incompatible "+parentName);
   // Persisted TerrainData clones can initialize to 100% layer zero. Restore the
   // source height/splat data deterministically before changing only the desert material.
   d.SetHeights(0,0,src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution));
   d.terrainLayers=src.terrainLayers;
   d.SetAlphamaps(0,0,src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight));
   int sand=SandLayer(d);if(sand<0)throw new Exception("No coast-sand desert layer "+parentName);
   var layers=d.terrainLayers.ToArray();var replacement=MakeLayer(key,t.transform.position);string old=AssetDatabase.GetAssetPath(layers[sand]);layers[sand]=replacement;d.terrainLayers=layers;d.SetBaseMapDirty();EditorUtility.SetDirty(d);
   t.terrainData=d;var c=t.GetComponent<TerrainCollider>();if(c)c.terrainData=d;
   rows.Add(parentName+"|terrain="+terrainPath+"|slot="+sand+"|old="+old+"|new="+AssetDatabase.GetAssetPath(replacement)+"|sourceHeightCopied=1|sourceAlphamapCopied=1");
  }
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save desert-style scene");
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/DesertStyle20261001");
  File.WriteAllLines("Validation/EdgeGrid20260923/DesertStyle20261001/apply.txt",rows);
  Debug.Log("DRAGON_ISLE_DESERT_STYLE_DONE "+rows.Count);
 }
}