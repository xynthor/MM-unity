using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Two new extension terrains receive forest-tinted COPIES of source layers.
// Neither canonical regions nor their shared TerrainLayers/textures are edited.
public static class MMReferenceForestSurfaces{
 const string West="Assets/World/WorldExtensions/Generated/";
 const string Dragon="Assets/World/DragonIsle/Generated/";
 static TerrainLayer CloneWithTexture(string original,string output,string texture,string title){
  var src=AssetDatabase.LoadAssetAtPath<TerrainLayer>(original);
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
  if(!src||!tex)throw new Exception("Forest source layer or new texture missing: "+original+" / "+texture);
  var dst=AssetDatabase.LoadAssetAtPath<TerrainLayer>(output);
  if(!dst){
   dst=new TerrainLayer();
   EditorUtility.CopySerialized(src,dst);
   dst.name=title;
   dst.diffuseTexture=tex;
   AssetDatabase.CreateAsset(dst,output);
  }else{
   dst.name=title;
   dst.diffuseTexture=tex;
  }
  EditorUtility.SetDirty(dst);
  return dst;
 }
 static void Attach(string asset,params Tuple<int,TerrainLayer>[] patches){
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(asset);
  if(!td)throw new Exception("Forest TerrainData missing "+asset);
  var old=td.terrainLayers;
  var next=(TerrainLayer[])old.Clone();
  foreach(var x in patches){
   if(x.Item1<0||x.Item1>=next.Length)
    throw new Exception("Forest layer index outside terrain "+asset);
   next[x.Item1]=x.Item2;
  }
  td.terrainLayers=next;
  EditorUtility.SetDirty(td);
 }
 [MenuItem("MMUnity/World/Reference Forest Surfaces - Extensions Only")]
 public static void Apply(){
  AssetDatabase.Refresh();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  var westGreen=CloneWithTexture(
   "Assets/Materials/RealisticWorld/Green.terrainlayer",
   West+"ReferenceWestForestGreen.terrainlayer",
   West+"ReferenceWestForestGreen.png",
   "Realistic_Green_ReferenceForest");
  var westLight=CloneWithTexture(
   "Assets/Materials/RealisticWorld/LightGreen.terrainlayer",
   West+"ReferenceWestForestLight.terrainlayer",
   West+"ReferenceWestForestLight.png",
   "Realistic_LightGreen_ReferenceForest");
  var dragonGreen=CloneWithTexture(
   "Assets/TempDragonTemplate/Trace_Grass.terrainlayer",
   Dragon+"ReferenceDragonForestGrass.terrainlayer",
   Dragon+"ReferenceDragonForestGrass.png",
   "Trace_Grass_ReferenceForest");
  Attach(West+"ParadiseValley_WestTerrain.asset",
   new Tuple<int,TerrainLayer>(0,westGreen),new Tuple<int,TerrainLayer>(1,westLight));
  Attach(Dragon+"DragonIsleSouthTerrain.asset",
   new Tuple<int,TerrainLayer>(0,dragonGreen));
  AssetDatabase.SaveAssets();
  File.WriteAllLines("Validation/EdgeGrid20260923/reference_forest_surfaces.txt",
   new []{
    "WEST_GREEN="+AssetDatabase.GetAssetPath(westGreen),
    "WEST_LIGHT="+AssetDatabase.GetAssetPath(westLight),
    "DRAGON_SOUTH_GREEN="+AssetDatabase.GetAssetPath(dragonGreen),
    "CANONICAL_TERRAIN_AND_LAYER_ASSETS=UNCHANGED",
   });
  Debug.Log("REFERENCE_EXTENSION_FOREST_SURFACES_APPLIED");
 }
}

