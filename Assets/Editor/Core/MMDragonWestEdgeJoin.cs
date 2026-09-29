using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Two Dragon tiles share the western column with the easternmost sliver of
// the Sweet Water coastline. One TerrainData per 512x512 square, no overlays.
public static class MMDragonWestEdgeJoin {
 const string D="Assets/World/DragonIsle/Generated/";
 const string W="Assets/World/WorldExtensions/Generated/";
 const string S="Assets/World/SweetWater/Generated/";
 static float Smooth(float u) {u=Mathf.Clamp01(u);return u*u*(3-2*u);}
 static TerrainData Load(string path){
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
  if(!t||t.heightmapResolution!=513||t.alphamapResolution!=512)
   throw new Exception("Reference terrain missing or unexpected resolution: "+path);
  return t;
 }
 static int[] Match(TerrainLayer[] dragon,TerrainLayer[] neighbor){
  return dragon.Select(q=>{
   if(!q)return -1;
   int exact=Array.FindIndex(neighbor,n=>n&&n.name==q.name);
   if(exact>=0)return exact;
   string n=q.name;
   string term=n.Contains("Grass")?"_Green":n.Contains("Sand")?"_Desert":
    n.Contains("Dirt")?"_Arid":n.Contains("Snow")?"_Snow":"_Volcanic";
   return Array.FindIndex(neighbor,layer=>layer&&layer.name.EndsWith(term,StringComparison.OrdinalIgnoreCase));
  }).ToArray();
 }
 public static void Apply(){
  var north=Load(D+"DragonIsleNorthTerrain.asset");
  var south=Load(D+"DragonIsleSouthTerrain.asset");
  var sweet=Load(S+"SweetWaterTerrain.asset");
  var nord=Load(W+"SweetWater_NorthTerrain.asset");
  var paradise=Load(W+"ParadiseValley_WestTerrain.asset");
  var nh=north.GetHeights(0,0,513,513);
  var sh=south.GetHeights(0,0,513,513);
  var sw=sweet.GetHeights(0,0,513,513);
  var nr=nord.GetHeights(0,0,513,513);
  var ph=paradise.GetHeights(0,0,513,513);
  // Blend only the eastern 64m into the unchanged Sweet Water boundary;
  // a 142m profile copy erased the traced western coastline.
  const int bandStart=448;
  float beforeN=0f,beforeS=0f,joined=0f;
  for(int z=0;z<513;z++){
   beforeN=Mathf.Max(beforeN,Mathf.Abs(nh[z,512]-nr[z,0])*320f);
   beforeS=Mathf.Max(beforeS,Mathf.Abs(sh[z,512]-sw[z,0])*320f);
   // Preserve already-matched, authority-traced geography on rebuild.
   if(Mathf.Abs(nh[z,512]-nr[z,0])*320f>.015f){
    for(int x=bandStart;x<513;x++){
     float f=Smooth((x-bandStart)/(512f-bandStart));
     int srcX=Mathf.Clamp(Mathf.RoundToInt((512-x)*.87f),0,512);
     nh[z,x]=Mathf.Lerp(nh[z,x],nr[z,srcX],f);
    }
   }
   // A repeated 64m South projection erased the eastern half of a traced
   // inland pond. Restrict initial edge repair to 12m, only when needed.
   if(Mathf.Abs(sh[z,512]-sw[z,0])*320f>.015f){
    const int southStart=500;
    for(int x=southStart;x<513;x++){
     float f=Smooth((x-southStart)/(512f-southStart));
     int srcX=Mathf.Clamp(Mathf.RoundToInt((512-x)*.87f),0,512);
     sh[z,x]=Mathf.Lerp(sh[z,x],sw[z,srcX],f);
    }
   }
  }
  // Dragon South sits directly north of Paradise Valley West.
  float paradiseBefore=0f,paradiseAfter=0f;
  for(int x=0;x<513;x++){
   float delta=ph[512,x]-sh[0,x];
   paradiseBefore=Mathf.Max(paradiseBefore,Mathf.Abs(delta)*320f);
   for(int z=0;z<=12;z++){
    float f=1f-Smooth(z/12f);
    sh[z,x]+=delta*f;
   }
   paradiseAfter=Mathf.Max(paradiseAfter,Mathf.Abs(ph[512,x]-sh[0,x])*320f);
  }
  // Original north/south seam was exact. Source adjacent tiles also
  // share a locked boundary; remove submillimeter quantization only.
  for(int x=bandStart;x<513;x++){
   float mid=(nh[0,x]+sh[512,x])*.5f;
   float dn=mid-nh[0,x],ds=mid-sh[512,x];
   for(int q=0;q<=12;q++){
    float f=1f-Smooth(q/12f);
    nh[q,x]+=dn*f;sh[512-q,x]+=ds*f;
   }
  }
  for(int x=0;x<513;x++)
   joined=Mathf.Max(joined,Mathf.Abs(nh[0,x]-sh[512,x])*320f);
  north.SetHeights(0,0,nh);
  south.SetHeights(0,0,sh);
  // Keep material layers consistent across the narrow continental strip.
  var na=north.GetAlphamaps(0,0,512,512);
  var sa=south.GetAlphamaps(0,0,512,512);
  var wa=sweet.GetAlphamaps(0,0,512,512);
  var ea=nord.GetAlphamaps(0,0,512,512);
  var pa=paradise.GetAlphamaps(0,511,512,1);
  var ni=Match(north.terrainLayers,nord.terrainLayers);
  var si=Match(south.terrainLayers,sweet.terrainLayers);
  var pi=Match(south.terrainLayers,paradise.terrainLayers);
  // Final aligned Sweet Water layers already own this edge. Re-mapping them
  // through Trace_Dirt previously recreated the straight brown seam.
  bool southAlreadyBlended=south.terrainLayers.Length==8 &&
   south.terrainLayers[6] && south.terrainLayers[6].name=="SweetWaterWest_Arid_Aligned" &&
   south.terrainLayers[7] && south.terrainLayers[7].name=="SweetWaterWest_LightGreen_Aligned";
  for(int z=0;z<512;z++)
  for(int x=bandStart;x<512;x++){
   float f=Smooth((x-bandStart)/(511f-bandStart));
   int srcX=Mathf.Clamp(Mathf.RoundToInt((511-x)*.87f),0,511);
   float sumN=0f,sumS=0f;
   for(int k=0;k<ni.Length;k++){
    if(ni[k]>=0)na[z,x,k]=Mathf.Lerp(na[z,x,k],ea[z,srcX,ni[k]],f);
    sumN+=na[z,x,k];
   }
   if(!southAlreadyBlended)for(int k=0;k<si.Length;k++){
    if(si[k]>=0)sa[z,x,k]=Mathf.Lerp(sa[z,x,k],wa[z,srcX,si[k]],f);
    sumS+=sa[z,x,k];
   }
   if(sumN>0f)for(int k=0;k<ni.Length;k++)na[z,x,k]/=sumN;
   if(sumS>0f)for(int k=0;k<si.Length;k++)sa[z,x,k]/=sumS;
  }
  // Match the real Paradise Valley West shoreline texture at Dragon South's
  // southern edge as well as its height: no arbitrary generic brown strip.
  for(int z=0;z<=12;z++)for(int x=0;x<512;x++){
   float f=1f-Smooth(z/12f),sum=0f;
   for(int k=0;k<pi.Length;k++){
    if(pi[k]>=0)sa[z,x,k]=Mathf.Lerp(sa[z,x,k],pa[0,x,pi[k]],f);
    sum+=sa[z,x,k];
   }
   if(sum>0f)for(int k=0;k<pi.Length;k++)sa[z,x,k]/=sum;
  }
  north.SetAlphamaps(0,0,na);south.SetAlphamaps(0,0,sa);
  EditorUtility.SetDirty(north);EditorUtility.SetDirty(south);
  AssetDatabase.SaveAssets();
  float endN=0f,endS=0f;
  for(int z=0;z<513;z++){
   endN=Mathf.Max(endN,Mathf.Abs(nh[z,512]-nr[z,0])*320f);
   endS=Mathf.Max(endS,Mathf.Abs(sh[z,512]-sw[z,0])*320f);
  }
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/dragon_east_reference_join.txt",
    new []{$"north_border_before_m={beforeN:F5}",
      $"south_border_before_m={beforeS:F5}",
      $"north_border_after_m={endN:F5}",
      $"south_border_after_m={endS:F5}",
      $"dragon_internal_join_after_m={joined:F5}",
      $"changed_eastern_band_m={512-bandStart}",
      $"paradise_border_before_m={paradiseBefore:F5}",
      $"paradise_border_after_m={paradiseAfter:F5}"});
  if(joined>.02f||endN>.02f||endS>.02f||paradiseAfter>.02f)
   throw new Exception($"Dragon west reference seam failed N={endN} S={endS} internal={joined}");
  // The aligned height border alone had opposing slopes and a visible V-shaped join.
  MMDragonSouthShoreHeightSmooth.Apply();
  // This MUST run after every height/alpha seam pass, not before it.
  MMDragonSouthSweetWaterMaterialBlend.Apply();
  // Restore the real Paradise material and traced southern coast after the 8-layer Sweet Water pass.
  MMWestCoastSecondPass20260926.Apply();
  Debug.Log($"DRAGON_REFERENCE_JOIN_DONE natural coast band={512-bandStart}m N={endN:F5} S={endS:F5}");
 }
}
