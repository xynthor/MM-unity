using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Paint photo-confirmed woodland in the GENERATED Paradise west terrain.
// Rocky southern cape, exact native east edge and northern Dragon join are excluded.
public static class MMParadisePhotoForestGround20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Terra="Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeParadiseForestGround_20260926\manifest.json";
 static float S(float a,float b,float p){
  float t=Mathf.Clamp01((p-a)/Mathf.Max(.001f,b-a));
  return t*t*(3f-2f*t);
 }
 static Color32[] Read(string p){
  var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!tex.LoadImage(File.ReadAllBytes(p))||tex.width!=513||tex.height!=513)
   throw new Exception("Missing Paradise original photo raster "+p);
  var px=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);return px;
 }
 static int Layer(TerrainData t,string label){
  int i=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_"+label);
  if(i<0)throw new Exception("Native Paradise material unavailable "+label);
  return i;
 }
 [MenuItem("MMUnity/Reference 2026/Paradise Photo Forest Ground")]
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Immutable pre-change Paradise terrain snapshot missing");
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(Terra);
  if(!t||t.alphamapResolution!=512||t.alphamapLayers!=7)
   throw new Exception("Paradise reference TerrainData unexpected layout");
  var photo=Read(V+"ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var coast=Read(V+"ReferenceMasks/ParadiseValley_West.png");
  var h=t.GetHeights(0,0,513,513);
  var a=t.GetAlphamaps(0,0,512,512);
  int green=Layer(t,"Green"),light=Layer(t,"LightGreen"),arid=Layer(t,"Arid"),
      rock=Layer(t,"Volcanic"),road=Layer(t,"RoadOverlay");
  int forestCells=0,painted=0,protectedCoast=0,protectedRock=0;
  double beforeGreen=0,afterGreen=0;
  float maxMove=0,borderDiff=0;
  var target=new float[7];
  for(int z=215;z<493;z++)for(int x=16;x<508;x++){
   float y=-24f+h[z,x]*320f;
   if(y<.85f){protectedCoast++;continue;}
   float dist=(coast[z*513+x].r-128f)*512f/(148f*3f);
   if(dist<6f){protectedCoast++;continue;}
   var p=photo[z*513+x];
   float f=p.g/255f,r=p.r/255f;
   float woodland=S(.27f,.68f,f)*S(-.14f,.16f,f-r);
   if(woodland<.05f){protectedRock++;continue;}
   float slope=t.GetSteepness(x/512f,z/512f);
   float relief=1f-S(18f,33f,slope);
   float fade=S(215f,292f,z)*(1f-S(465f,493f,z))*
     S(10f,28f,x)*(1f-S(457f,505f,x));
   float w=.92f*woodland*relief*fade*S(6f,28f,dist);
   float roadKeep=Mathf.Clamp01(a[z,x,road]*2f);
   w*=1f-.92f*roadKeep;
   if(w<.005f)continue;
   Array.Clear(target,0,target.Length);
   // Deep-green moss plus dry leaf soil; no neon lawn and no ash-grey canopy.
   target[green]=.72f;target[light]=.035f;
   target[arid]=.20f;target[rock]=.045f;
   float pre=a[z,x,green]+a[z,x,light];
   beforeGreen+=pre;
   for(int k=0;k<7;k++){
    float old=a[z,x,k];
    a[z,x,k]=Mathf.Lerp(old,target[k],w);
    maxMove=Mathf.Max(maxMove,Mathf.Abs(a[z,x,k]-old));
   }
   afterGreen+=a[z,x,green]+a[z,x,light];
   if(woodland>.5f)forestCells++;painted++;
  }
  if(painted<3500||forestCells<2000||maxMove>.92f)
   throw new Exception("Unsafe Paradise photo forest paint count="+painted+
     " verifiedForest="+forestCells+" alphaMove="+maxMove);
  var prior=t.GetAlphamaps(0,0,512,512);
  for(int z=0;z<512;z++)for(int k=0;k<7;k++)
   borderDiff=Mathf.Max(borderDiff,Mathf.Abs(prior[z,511,k]-a[z,511,k]));
  for(int x=0;x<512;x++)for(int k=0;k<7;k++)
   borderDiff=Mathf.Max(borderDiff,Mathf.Abs(prior[511,x,k]-a[511,x,k]));
  if(borderDiff>.000001f)throw new Exception("Protected Paradise edge altered");
  t.SetAlphamaps(0,0,a);EditorUtility.SetDirty(t);AssetDatabase.SaveAssets();
  File.WriteAllText(V+"paradise_photo_forest_ground_20260926.txt",
    "PASS photoForestPixels="+painted+" strongForestPixels="+forestCells+
    " waterProtected="+protectedCoast+" southRockProtected="+protectedRock+
    " meanGreenBefore="+(beforeGreen/painted).ToString("F3")+
    " meanGreenAfter="+(afterGreen/painted).ToString("F3")+
    " borderAlphaError="+borderDiff.ToString("F6")+
    " protectedCanonicalEdits=0 heightEdits=0 originalNativeMaterials=true\n");
 }
}
