using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMParadiseForestMaterialRestore20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string TerrainPath="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/18ab3d932291f3d48ab29d6192d05cc0.asset";
 const string LayerPath="Assets/World/WorldExtensions/Generated/ReferenceWestForestInterior.terrainlayer";
 const string BaselineSha="53bbe6f55e09b21c967d9aaadf94ce898acc55a6d32229bed46c5be6c41b662d";
 const string Backup="Backups/BeforeParadiseForestMaterialRestore_20261001/manifest.json";
 static string Sha(string p){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 public static void Run(){
  if(!File.Exists(Backup))throw new Exception("Verified backup missing");
  if(Sha(TerrainPath)!=BaselineSha)throw new Exception("Paradise West TerrainData changed since backup");
  var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var root=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Paradise Valley West - LINKED EXTENSION");
  var t=root.GetComponentInChildren<Terrain>(true);var td=t.terrainData;
  if(AssetDatabase.GetAssetPath(td)!=TerrainPath||td.alphamapLayers!=9)throw new Exception("Unexpected Paradise West terrain");
  var layers=td.terrainLayers;
  int forest=4;
  if(!layers[forest]||!layers[forest].diffuseTexture||AssetDatabase.GetAssetPath(layers[forest].diffuseTexture)!="Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_CS.png")
    throw new Exception("Forest slot no longer matches expected pre-restore wrapper");
  var replacement=AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);if(!replacement||!replacement.diffuseTexture)throw new Exception("Reference forest layer missing");
  var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);double sum=0;int dom=0;
  for(int y=0;y<td.alphamapHeight;y++)for(int x=0;x<td.alphamapWidth;x++){sum+=a[y,x,forest];int b=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,b])b=k;if(b==forest)dom++;}
  if(dom<15000||sum<8000)throw new Exception($"Forest mask unexpectedly weak dominant={dom} alphaSum={sum}");
  layers[forest]=replacement;td.terrainLayers=layers;EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  // Alpha and heights were never written; verify the assigned slot persisted.
  var check=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
  if(check.terrainLayers.Length!=9||check.terrainLayers[forest]!=replacement)throw new Exception("Forest material assignment did not persist");
  Directory.CreateDirectory("Validation/EdgeGrid20260923/WestDragon20261001");
  string report=$"PASS forestSlot={forest} dominantForestPixels={dom} forestAlphaSum={sum:F1} diffuse={AssetDatabase.GetAssetPath(replacement.diffuseTexture)} tile={replacement.tileSize} alphamapWrites=0 heightWrites=0 sceneWrites=0 otherTerrainWrites=0";
  File.WriteAllText("Validation/EdgeGrid20260923/WestDragon20261001/paradise_forest_material_restore.txt",report+"\n");Debug.Log(report);
 }
}