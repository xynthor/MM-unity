using UnityEngine;
using UnityEditor;
using System;
using System.IO;

// C1 continuation of the untouched Sweet Water west-edge height slope.
// Preserve Dragon South's reference shoreline by correcting only the first 12m.
public static class MMDragonSouthShoreHeightSmooth {
 const string D="Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset";
 const string S="Assets/World/SweetWater/Generated/SweetWaterTerrain.asset";
 const int BAND=12;
 static float Ease(float u){u=Mathf.Clamp01(u);return u*u*(3f-2f*u);}
 static float LocalCorrection(float delta,int q){
  float t=(q-1f)/(BAND-1f),a=t*t,b=a*t;
  return (2*b-3*a+1)*delta+(b-2*a+t)*(BAND-1f)*delta;
 }
 [MenuItem("MMUnity/World/Smooth Dragon South To Sweet Water Height Slope")]
 public static void Apply(){
  var dragon=AssetDatabase.LoadAssetAtPath<TerrainData>(D);
  var sweet=AssetDatabase.LoadAssetAtPath<TerrainData>(S);
  if(!dragon||!sweet||dragon.heightmapResolution!=513||sweet.heightmapResolution!=513)
   throw new Exception("Dragon South or canonical Sweet Water TerrainData unavailable");
  var h=dragon.GetHeights(0,0,513,513);
  var src=sweet.GetHeights(0,0,513,513);
  float before=0f,after=0f,maxMove=0f,maxBorder=0f,curvBefore=0f,curvAfter=0f;
  for(int z=0;z<513;z++){
   float srcSlope=src[z,1]-src[z,0];
   float oldSlope=h[z,512]-h[z,511];
   before=Mathf.Max(before,Mathf.Abs(oldSlope-srcSlope)*320f);
   float borderFade=Ease(z/16f)*Ease((512f-z)/16f);
   // The earlier full-profile cubic displaced some terrain by 24m.
   // Match the first two derivatives and feather the correction back
   // into the EXISTING coastline without re-sculpting its inland height.
   float desired511=src[z,0]-srcSlope;
   float delta=desired511-h[z,511];
   for(int q=1;q<=BAND;q++){
    int x=512-q;
    float correction=LocalCorrection(delta,q)*borderFade;
    float prior=h[z,x];
    h[z,x]=Mathf.Clamp01(prior+correction);
    maxMove=Mathf.Max(maxMove,Mathf.Abs(h[z,x]-prior)*320f);
   }
   h[z,512]=src[z,0];
   after=Mathf.Max(after,Mathf.Abs((h[z,512]-h[z,511])-srcSlope)*320f);
   // Match Sweet Water curvature as well as height and first slope. Only the
   // second generated vertex needs correction; feather its <=0.4m delta out.
   float srcCurv=src[z,2]-2f*src[z,1]+src[z,0];
   curvBefore=Mathf.Max(curvBefore,
     Mathf.Abs((h[z,510]-2f*h[z,511]+h[z,512])-srcCurv)*320f);
   float desired510=srcCurv+2f*h[z,511]-h[z,512];
   float delta2=desired510-h[z,510];
   for(int q=2;q<=BAND;q++){
    float f=1f-Ease((q-2f)/(BAND-2f));
    int x=512-q;float prior=h[z,x];
    h[z,x]=Mathf.Clamp01(prior+delta2*f);
    maxMove=Mathf.Max(maxMove,Mathf.Abs(h[z,x]-prior)*320f);
   }
   curvAfter=Mathf.Max(curvAfter,
     Mathf.Abs((h[z,510]-2f*h[z,511]+h[z,512])-srcCurv)*320f);
   maxBorder=Mathf.Max(maxBorder,Mathf.Abs(h[z,512]-src[z,0])*320f);
  }
  if(maxBorder>.001f||after>.8f||curvAfter>.02f||maxMove>7f)
   throw new Exception("Dragon South slope polish failed: border="+maxBorder+" slope="+after+" curvature="+curvAfter);
  dragon.SetHeights(0,0,h);EditorUtility.SetDirty(dragon);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllLines("Validation/EdgeGrid20260923/dragon_south_sweetwater_c1_slope.txt",
    new[]{"ribbon_width_m="+BAND,
      "slope_jump_before_max_m_per_m="+before.ToString("F5"),
      "slope_jump_after_max_m_per_m="+after.ToString("F5"),
      "curvature_jump_before="+curvBefore.ToString("F5"),
      "curvature_jump_after="+curvAfter.ToString("F5"),
      "largest_generated_height_adjustment_m="+maxMove.ToString("F5"),
      "shared_edge_height_error_m="+maxBorder.ToString("F6"),
      "original_sweetwater_terrain_edits=0"});
  Debug.Log("DRAGON_SOUTH_C1_SLOPE_OK before="+before.ToString("F3")+
     " after="+after.ToString("F3")+" height_move="+maxMove.ToString("F3"));
 }
}

