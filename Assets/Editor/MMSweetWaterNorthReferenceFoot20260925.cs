using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
// Paint only the green Sweet Water foothills confirmed in the authority image.
// The rest of northern snow and volcanic exposure are deliberately preserved.
public static class MMSweetWaterNorthReferenceFoot20260925{
 const string V="Validation/EdgeGrid20260923/";
 const string P="Assets/World/WorldExtensions/Generated/SweetWater_NorthTerrain.asset";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeSweetWaterReferenceNorthFootBiome_20260925\manifest.json";
 static float Ease(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3-2*t);
 }
 static int Layer(TerrainData d,string n){
  int i=Array.FindIndex(d.terrainLayers,x=>x&&x.name==n);
  if(i<0)throw new Exception("Missing north material "+n);
  return i;
 }
 [MenuItem("MMUnity/Reference 2026/Paint Source-Map Sweet Water North Forest Foot")]
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Generated north terrain backup missing");
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
   throw new Exception("Saved linked scene required");
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(P);
  if(!td||td.heightmapResolution!=513||td.alphamapResolution!=512)
   throw new Exception("Generated Sweet Water north not ready");
  string maskPath=V+"ReferenceBiomeCandidates_20260925/SweetWater_North_SourceForest.png";
  var img=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!img.LoadImage(File.ReadAllBytes(maskPath))||img.width!=513||img.height!=513)
   throw new Exception("Invalid source-image forest map");
  var photo=img.GetPixels32();UnityEngine.Object.DestroyImmediate(img);
  int green=Layer(td,"Realistic_Green"),light=Layer(td,"Realistic_LightGreen");
  int volcanic=Layer(td,"Realistic_Volcanic"),dirt=Layer(td,"Realistic_Arid");
  int road=Layer(td,"Realistic_RoadOverlay");
  float[,,] a=td.GetAlphamaps(0,0,512,512);
  float[,] h=td.GetHeights(0,0,513,129);
  int altered=0,strong=0;float before=0f,after=0f,maxMove=0f;
  for(int z=0;z<=127;z++)for(int x=0;x<512;x++){
   float f=photo[z*513+x].g/255f;
   if(f<.08f)continue;
   float height=-24f+320f*h[z,x];
   if(height<.3f)continue;
   float angle=td.GetSteepness(x/512f,z/512f);
   float influence=.92f*Ease(.10f,.58f,f)*
     (1f-Ease(95f,128f,z))*(1f-Ease(29f,47f,angle))*
     Ease(-.1f,.5f,height)*(1f-a[z,x,road]);
   if(influence<.002f)continue;
   if(f>.6f){strong++;before+=a[z,x,green]+a[z,x,light];}
   float old=a[z,x,green]+a[z,x,light];
   for(int k=0;k<td.alphamapLayers;k++){
    float target=k==green?.72f:k==light?.17f:
      k==dirt?.06f:k==volcanic?.05f:0f;
    a[z,x,k]=Mathf.Lerp(a[z,x,k],target,influence);
   }
   float sum=0f;for(int k=0;k<td.alphamapLayers;k++)sum+=a[z,x,k];
   for(int k=0;k<td.alphamapLayers;k++)a[z,x,k]/=sum;
   float now=a[z,x,green]+a[z,x,light];
   if(f>.6f)after+=now;
   maxMove=Mathf.Max(maxMove,Mathf.Abs(now-old));
   altered++;
  }
  if(altered<3000||strong<1000||after/strong<before/strong+.12f)
   throw new Exception("Authority forest candidate insufficient altered="+altered+
    " strong="+strong+" before="+before/Mathf.Max(1,strong)+
    " after="+after/Mathf.Max(1,strong));
  td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  Directory.CreateDirectory(V);
  string report="PASS painted_pixels="+altered+" photo_strong_cells="+strong+
   " original_forest_weight="+(before/strong).ToString("F3")+
   " new_forest_weight="+(after/strong).ToString("F3")+
   " max_splat_change="+maxMove.ToString("F3")+
   " north_band_z=0..127 original_scene_and_terrain_edits=0";
  File.WriteAllText(V+"sweetwater_north_reference_foot_biome_20260925.txt",report+"\n");
  Debug.Log("SWEET_WATER_NORTH_REFERENCE_FOOT "+report);
 }
}
[InitializeOnLoad]
internal static class MMSweetWaterNorthReferenceFootQueued20260925{
 const string V="Validation/EdgeGrid20260923/";
 static MMSweetWaterNorthReferenceFootQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_SWEETWATER_NORTH_FOREST_FOOT.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_SWEETWATER_NORTH_FOREST_FOOT.flag");
  try{
   MMSweetWaterNorthReferenceFoot20260925.Apply();
   File.WriteAllText(V+"sweetwater_north_forest_runtime.txt","PASS reference forest foothill painted\n");
  }catch(Exception ex){
   File.WriteAllText(V+"sweetwater_north_forest_runtime.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
