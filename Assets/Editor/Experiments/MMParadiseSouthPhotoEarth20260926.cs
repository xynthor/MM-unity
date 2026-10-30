using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
// The reference's western Paradise promontory is warm brown exposed soil/rock,
// not uniform gray volcanic ash. Paint only mask-confirmed south cape.
// Keep forested north, exact Dragon South color seam, and original scenes.
public static class MMParadiseSouthPhotoEarth20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string Asset="Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeParadiseWestOchreSouth_20260926\manifest.json";
 static float Smooth(float a,float b,float x){
  float v=Mathf.Clamp01((x-a)/Mathf.Max(.001f,b-a));return v*v*(3f-2f*v);
 }
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Paradise south rollback missing");
  // Repeatably start from the verified pre-ochre terrain; never compound
  // the previous pass or damage forest ground painted before this snapshot.
  const string Previous=@"C:\MMUnityPort\Backups\BeforeParadiseWestOchreSouth_20260926\Assets\World\WorldExtensions\Generated\ParadiseValley_WestTerrain.asset";
  if(!File.Exists(Previous))throw new Exception("Pre-ochre baseline unavailable");
  var t=AssetDatabase.LoadAssetAtPath<TerrainData>(Asset);
  if(!t||t.alphamapResolution!=512||t.alphamapLayers!=7)
   throw new Exception("Unexpected Paradise West generated terrain");
  var p=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  var m=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  try{
   if(!p.LoadImage(File.ReadAllBytes(V+"ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png"))||
      !m.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/ParadiseValley_West.png"))||
      p.width!=513||p.height!=513||m.width!=513||m.height!=513)
     throw new Exception("Missing photo mask for brown west promontory");
   var photo=p.GetPixels32();var land=m.GetPixels32();
   string scratch="Assets/TempParadiseOriginalOchre_20260926.asset";
   if(File.Exists(Path.GetFullPath(scratch)))throw new Exception("Scratch terrain collision");
   File.Copy(Previous,Path.GetFullPath(scratch));
   float[,,] a;
   try{
    AssetDatabase.ImportAsset(scratch,ImportAssetOptions.ForceSynchronousImport);
    var prior=AssetDatabase.LoadAssetAtPath<TerrainData>(scratch);
    if(!prior||prior.alphamapLayers!=t.alphamapLayers)
     throw new Exception("Invalid pre-ochre terrain texture snapshot");
    a=prior.GetAlphamaps(0,0,512,512);
   }finally{AssetDatabase.DeleteAsset(scratch);}
   var h=t.GetHeights(0,0,513,513);
   int rock=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
   int soil=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Arid");
   int warmSand=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Desert");
   int green=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_Green");
   int light=Array.FindIndex(t.terrainLayers,l=>l&&l.name=="Realistic_LightGreen");
   if(rock<0||soil<0||warmSand<0||green<0||light<0)throw new Exception("Photo source layers missing");
   int painted=0,woodlandProtected=0,shoreProtected=0;
   double beforeSoil=0,afterSoil=0;float northGap=0,eastGap=0;
   var original=t.GetAlphamaps(0,0,512,512);
   for(int z=18;z<280;z++)for(int x=13;x<487;x++){
    var landPixel=land[z*513+x];
    float dist=(landPixel.r-128f)*512f/(148f*3f);
    float y=-24f+320f*h[z,x];
    if(dist<5f||y<.5f){shoreProtected++;continue;}
    var px=photo[z*513+x];
    float forest=px.g/255f;
    float stone=Mathf.Max(px.r/255f,.72f*px.b/255f);
    float cape=1f-Smooth(124f,280f,z);
    float xFade=Smooth(11f,36f,x)*(1f-Smooth(438f,486f,x));
    if(cape<.001f||xFade<.001f)continue;
    // Preserve photographed woodland in the north and any existing
    // patches of green within the predominantly brown southern headland.
    float rocky=stone*(1f-.68f*forest);
    if(rocky<.13f||forest>.60f){woodlandProtected++;continue;}
    float w=.97f*cape*xFade*Smooth(.10f,.50f,rocky)*
      Smooth(5f,22f,dist);
    if(w<.002f)continue;
    float[] target=new float[7];
    // Map's dry western headland is warm ochre, not monochrome grey.
    // Blend native dry-soil cracks with the existing native golden sand.
    // Native diffuse sampling: Desert is warmer/lighter than Arid, while
    // Volcanic darkens the headland. Keep volcanic primarily on mapped rock.
    target[soil]=.29f;target[rock]=.12f;target[warmSand]=.52f;
    target[green]=.05f;target[light]=.02f;
    float sum=0;
    for(int k=0;k<7;k++)sum+=a[z,x,k];
    if(sum<.01f)continue;
    beforeSoil+=a[z,x,soil];
    for(int k=0;k<7;k++)
     a[z,x,k]=Mathf.Lerp(a[z,x,k],target[k],w);
    afterSoil+=a[z,x,soil];
    painted++;
   }
   if(painted<1500)
    throw new Exception("Reference brown peninsula paint too small "+painted);
   for(int x=0;x<512;x++)for(int k=0;k<7;k++)
    northGap=Mathf.Max(northGap,Mathf.Abs(original[511,x,k]-a[511,x,k]));
   for(int z=0;z<512;z++)for(int k=0;k<7;k++)
    eastGap=Mathf.Max(eastGap,Mathf.Abs(original[z,511,k]-a[z,511,k]));
   if(northGap>.00001f||eastGap>.00001f)
    throw new Exception("Protected Paradise west coast material boundary moved");
   t.SetAlphamaps(0,0,a);EditorUtility.SetDirty(t);
   AssetDatabase.SaveAssets();
   File.WriteAllText(V+"paradise_south_photo_earth_20260926.txt",
    "PASS photoBrownCapePixels="+painted+
    " woodlandProtected="+woodlandProtected+" coastProtected="+shoreProtected+
    " meanAridBefore="+(beforeSoil/painted).ToString("F4")+
    " meanAridAfter="+(afterSoil/painted).ToString("F4")+
    " northAlphaError="+northGap.ToString("F6")+
    " eastAlphaError="+eastGap.ToString("F6")+
    " canonicalSourceWrites=0 heightWrites=0\n");
  }finally{
   UnityEngine.Object.DestroyImmediate(p);
   UnityEngine.Object.DestroyImmediate(m);
  }
 }
}
