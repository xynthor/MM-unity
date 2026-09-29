using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

// Updates only the generated legacy snow layer; never rewrites painted terrain.
public static class MMTraceSnowPBR20260929
{
 const string Target="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Layers/0045748e4c2ea3843901e0fe4dbf7b7f.terrainlayer";
 const string Source="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Layers/05bcd9452f5a4994fbb20319a2a92a54.terrainlayer";
 public static void Apply()
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
  if(!File.Exists("Backups/BeforeTraceSnowPBR_20260929/manifest.json"))throw new Exception("Backup missing");
  var target=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Target);
  var source=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Source);
  if(!target||!source||!AssetDatabase.GetAssetPath(source.diffuseTexture).Contains("Snow02_Seamless"))throw new Exception("Unexpected snow assets");
  if(target.diffuseTexture==source.diffuseTexture&&target.normalMapTexture==source.normalMapTexture){Debug.Log("Trace snow PBR already applied");return;}
  using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Target))).Replace("-","")!="A812DB0CDC9D58C8C43CCBE9A8004EDD16560871741599ADC0FF76F91656722F")throw new Exception("Layer baseline changed");
  string name=target.name;
  EditorUtility.CopySerialized(source,target);target.name=name;target.tileOffset=Vector2.zero;
  EditorUtility.SetDirty(target);AssetDatabase.SaveAssets();
  foreach(var t in Terrain.activeTerrains.Where(t=>t.terrainData.terrainLayers.Contains(target)))t.terrainData.SetBaseMapDirty();
  File.WriteAllText("Validation/EdgeGrid20260923/Resume_TraceSnowPBR_20260929.txt","Updated generated Trace_Snow layer to existing seamless Snow02 PBR. Painted weights, layer order, terrain heights and object transforms unchanged. Canonical writes=0.\n");
  Debug.Log("Generated Trace_Snow PBR updated; no terrain paint/height writes");
 }
}
