using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

// Original Sweet Water's WEST border is 92% Realistic_Arid, 8% LightGreen.
// Dragon South used only Trace_Dirt there, creating a hard color line.
// Blend actual SOURCE texture layers into the last 88m without changing height.
public static class MMDragonSouthSweetWaterMaterialBlend {
 const string T="Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset";
 const string S="Assets/World/SweetWater/Generated/SweetWaterTerrain.asset";
 const float B=88f;
 static float Ease(float v){v=Mathf.Clamp01(v);return v*v*(3f-2f*v);}
 static TerrainLayer AlignedClone(TerrainLayer src,string path,string label){
  var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
  if(!layer){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,path);}
  EditorUtility.CopySerialized(src,layer);
  layer.name=label;
  // Dragon South is exactly one 512m tile WEST of Sweet Water.
  // Offset the repeated texture by the non-integer 512m phase remainder.
  float ox=src.tileOffset.x-Mathf.Repeat(512f,src.tileSize.x);
  layer.tileOffset=new Vector2(ox,src.tileOffset.y);
  EditorUtility.SetDirty(layer);
  return layer;
 }
 [MenuItem("MMUnity/World/Blend Dragon South With Sweet Water Original Ground")]
 public static void Apply(){
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(T);
  var src=AssetDatabase.LoadAssetAtPath<TerrainData>(S);
  if(!td||!src||td.alphamapResolution!=512||src.alphamapResolution!=512)
   throw new Exception("Missing western coast TerrainData");
  var before=td.GetAlphamaps(0,0,512,512);
  var orig=td.terrainLayers;
  if(orig.Length!=6&&orig.Length!=8&&orig.Length!=12)
   throw new Exception("Dragon South requires 6 base or 8 already-blended layers, got "+orig.Length);
  if(orig.Take(6).Any(x=>!x||x.name.EndsWith("_ReferenceTerrainLayer")))
   throw new Exception("Dragon ground no longer uses six original tiled materials");
  var arid=src.terrainLayers.FirstOrDefault(x=>x&&x.name=="Realistic_Arid");
  var light=src.terrainLayers.FirstOrDefault(x=>x&&x.name=="Realistic_LightGreen");
  if(!arid||!light)throw new Exception("Original Sweet Water material assets missing");
  string root="Assets/World/DragonIsle/Generated/";
  var aridAligned=AlignedClone(arid,root+"SweetWaterWest_Arid_Aligned.terrainlayer",
    "SweetWaterWest_Arid_Aligned");
  var lightAligned=AlignedClone(light,root+"SweetWaterWest_LightGreen_Aligned.terrainlayer",
    "SweetWaterWest_LightGreen_Aligned");
  var srcAlpha=src.GetAlphamaps(0,0,1,512);
  int ia=Array.IndexOf(src.terrainLayers,arid),il=Array.IndexOf(src.terrainLayers,light);

  if(orig.Length>=8 &&
     (!(orig[6]==arid||orig[6]==aridAligned) ||
      !(orig[7]==light||orig[7]==lightAligned)))
   throw new Exception("Unexpected existing border materials");
  int totalLayers=orig.Length==12?12:8;
  var weights=new float[512,512,totalLayers];
  float error=0f;
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   float f=Ease((x-(511f-B))/B);
   float baseSum=0f;
   for(int k=0;k<6;k++)baseSum+=before[z,x,k];
   if(orig.Length==12)for(int k=8;k<12;k++)baseSum+=before[z,x,k];
   if(baseSum>.0001f)
    for(int k=0;k<6;k++)weights[z,x,k]=before[z,x,k]/baseSum*(1f-f);
   else weights[z,x,2]=1f-f; // Original Trace_Dirt fallback at canonical edge.
   if(orig.Length==12&&baseSum>.0001f)for(int k=8;k<12;k++)
    weights[z,x,k]=before[z,x,k]/baseSum*(1f-f);
   float aw=srcAlpha[z,0,ia],lw=srcAlpha[z,0,il];
   float norm=Mathf.Max(.00001f,aw+lw);
   weights[z,x,6]=f*aw/norm;
   weights[z,x,7]=f*lw/norm;
   if(x==511){
    error=Mathf.Max(error,Mathf.Abs(weights[z,x,6]-aw/norm));
    error=Mathf.Max(error,Mathf.Abs(weights[z,x,7]-lw/norm));
   }
  }
  td.terrainLayers=orig.Take(6).Concat(new[]{aridAligned,lightAligned})
   .Concat(orig.Length==12?orig.Skip(8):Enumerable.Empty<TerrainLayer>()).ToArray();
  td.SetAlphamaps(0,0,weights);
  // Check the committed TerrainData, not merely the intended weight buffer.
  // A prior rebuild left this edge as 100% Trace_Dirt despite a success log.
  var committed=td.GetAlphamaps(511,0,1,512);
  for(int z=0;z<512;z++){
   float aw=srcAlpha[z,0,ia],lw=srcAlpha[z,0,il];
   float norm=Mathf.Max(.00001f,aw+lw);
   error=Mathf.Max(error,Mathf.Abs(committed[z,0,6]-aw/norm));
   error=Mathf.Max(error,Mathf.Abs(committed[z,0,7]-lw/norm));
   for(int k=0;k<6;k++)error=Mathf.Max(error,committed[z,0,k]);
  }
  if(error>.001f)throw new Exception("Persisted Sweet Water border weights differ from canonical source: "+error);
  EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllText("Validation/EdgeGrid20260923/sweetwater_west_ground_transition.txt",
   "layer_count="+totalLayers+"\nsource_arid="+arid.name+"\nsource_light="+light.name+
   "\nblend_width_m="+B+"\nsource_border_weight_error="+error.ToString("F6")+
   "\narid_aligned_offset="+aridAligned.tileOffset+
   "\nlightgreen_aligned_offset="+lightAligned.tileOffset+
   "\ncanonical_terrain_edits=0\nheightmap_edits=0\n");
  if(error>.001f)throw new Exception("Sweet Water border colors did not match original");
  Debug.Log("SWEETWATER_WEST_GROUND_TRANSITION_OK eight original tiled layers, 88m color blend, source edge matched");
 }
}

