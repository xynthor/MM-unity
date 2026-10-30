using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

public static class MMLinkedWorldTexturePhase20260929
{
 const string Root="Assets/World/WorldExtensions/Generated/LinkedWorldTexturePhase20260929";
 const string Backup="Backups/BeforeWorldTexturePhaseFinal_20260929/";
 static Vector2 Offset(Terrain t,TerrainLayer l){return new Vector2(Mathf.Repeat(t.transform.position.x,l.tileSize.x),Mathf.Repeat(t.transform.position.z,l.tileSize.y));}
 static bool Needs(Terrain t,TerrainLayer l){var d=l.tileOffset-Offset(t,l);float x=Mathf.Repeat(d.x,l.tileSize.x),z=Mathf.Repeat(d.y,l.tileSize.y);return Mathf.Min(x,l.tileSize.x-x)>.002f||Mathf.Min(z,l.tileSize.y-z)>.002f;}
 static string Hash(Array values){var bytes=new byte[Buffer.ByteLength(values)];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(bytes));}
 public static void Apply()
 {
  if(EditorApplication.isPlaying||!File.Exists(Backup+"manifest.json"))throw new Exception("Saved baseline backup and Edit Mode required");
  var terrains=Terrain.activeTerrains.Where(t=>t.terrainData.terrainLayers.Any(l=>Needs(t,l))).ToArray();
  foreach(var t in terrains){string path=AssetDatabase.GetAssetPath(t.terrainData);if(!path.StartsWith("Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/"))throw new Exception("Protected terrain encountered");using(var hash=SHA256.Create())if(!hash.ComputeHash(File.ReadAllBytes(path)).SequenceEqual(hash.ComputeHash(File.ReadAllBytes(Backup+System.IO.Path.GetFileName(path)))))throw new Exception("Terrain changed since backup: "+path);}
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedWorldTexturePhase20260929");
  int channels=0;var rows=new List<string>();
  foreach(var t in terrains){var d=t.terrainData;var layers=d.terrainLayers;string alpha=Hash(d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight)),height=Hash(d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution));
   for(int k=0;k<layers.Length;k++){var layer=layers[k];if(!Needs(t,layer))continue;var offset=Offset(t,layer);string path=Root+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(d))+"_"+k+".terrainlayer";if(AssetDatabase.LoadAssetAtPath<TerrainLayer>(path))throw new Exception("Unreviewed existing phase candidate "+path);var copy=UnityEngine.Object.Instantiate(layer);copy.name=layer.name;copy.tileOffset=offset;AssetDatabase.CreateAsset(copy,path);layers[k]=copy;channels++;}
   d.terrainLayers=layers;d.SetBaseMapDirty();t.Flush();
   if(alpha!=Hash(d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight))||height!=Hash(d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution)))throw new Exception("Unexpected paint/height change");
   EditorUtility.SetDirty(d);rows.Add(d.name+" alphaSHAUnchanged=true heightSHAUnchanged=true channels="+layers.Length);
  }
  AssetDatabase.SaveAssets();rows.Insert(0,"World-aligned texture phase: terrains="+terrains.Length+" generatedLayerCopies="+channels+" paintWrites=0 heightWrites=0 canonicalWrites=0 textureScaleChanges=0");File.WriteAllLines("Validation/EdgeGrid20260923/Resume_WorldTexturePhase_20260929.txt",rows);Debug.Log(rows[0]);
 }
}
