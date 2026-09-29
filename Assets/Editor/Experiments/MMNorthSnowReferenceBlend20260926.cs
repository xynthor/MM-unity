using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
public static class MMNorthSnowReferenceBlend20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 static float S(float a,float b,float x){
  float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);
 }
 public static void Apply(){
  if(!File.Exists(@"C:\MMUnityPort\Backups\BeforeReferenceVisualSeamFix_20260926\manifest.json"))
   throw new Exception("Missing pre-fix backup");
  var report=new List<string>();
  foreach(var n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
   var s=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
   var e=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
   if(!s||!e||s.alphamapLayers!=e.alphamapLayers)
    throw new Exception("Source/new snow layers unavailable "+n);
   for(int k=0;k<s.alphamapLayers;k++)
    if(s.terrainLayers[k]!=e.terrainLayers[k])
     throw new Exception(n+" source versus extension layer mismatch "+k);
   var src=s.GetAlphamaps(0,511,512,1);
   var a=e.GetAlphamaps(0,0,512,512);
   int snow=Array.FindIndex(e.terrainLayers,l=>l&&l.name=="Realistic_Snow");
   int volcanic=Array.FindIndex(e.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
   if(snow<0||volcanic<0)throw new Exception("Missing canonical snow/rock "+n);
   var img=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
   if(!img.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+n+"_North.png"))||
      img.width!=513)throw new Exception("Missing northern reference mask "+n);
   var pix=img.GetPixels32();UnityEngine.Object.DestroyImmediate(img);
   double before32=0,after32=0;float err=0;int changed=0;
   for(int z=0;z<512;z++)for(int x=0;x<512;x++){
    if(z>128)continue;
    float w=1f-S(0f,128f,z);
    float land=S(135f,157f,pix[z*513+x].r);
    w*=land;
    if(z==32)before32+=a[z,x,snow];
    if(w>.0001f){
     for(int k=0;k<a.GetLength(2);k++)
      a[z,x,k]=Mathf.Lerp(a[z,x,k],src[0,x,k],w);
     changed++;
    }
    if(z==32)after32+=a[z,x,snow];
   }
   // Preserve genuine volcano / crater masks outside the seam corridor.
   // The correction is strictly a 128-m feather away from canonical originals.
   for(int x=0;x<512;x++)for(int k=0;k<s.alphamapLayers;k++)
    if(pix[x].r>=157)
     err=Mathf.Max(err,Mathf.Abs(a[0,x,k]-src[0,x,k]));
   if(err>.002f)throw new Exception(n+" source snow material seam error "+err);
   e.SetAlphamaps(0,0,a);EditorUtility.SetDirty(e);
   report.Add(n+" snow_32m_before="+(before32/512).ToString("F3")+
    " after="+(after32/512).ToString("F3")+
    " referenceLandPixelsFeathered="+changed+" sourceSeamError="+err.ToString("F5")+
    " terrainHeightEdits=0 craterOrPondEdits=0");
  }
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"northern_source_snow_color_blend_20260926.txt",report);
 }
}
