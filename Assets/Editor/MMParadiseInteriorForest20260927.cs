using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
[InitializeOnLoad]
public static class MMParadiseInteriorForest20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string Flag=V+"RUN_PARADISE_INTERIOR_FOREST_20260927.flag";
 const string Terrain="Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset";
 const string Layer="Assets/World/WorldExtensions/Generated/ReferenceWestForestInterior.terrainlayer";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeParadiseInteriorForest_20260927\manifest.json";
 const string Expected="fa75731b93bf5c42444db89a789bd8db81318830dad9ccfd3d752c8891502930";
 static double next;
 static MMParadiseInteriorForest20260927(){EditorApplication.update+=Poll;}
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3f-2f*t);}
 static Color32[] Pixels(string file){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!t.LoadImage(File.ReadAllBytes(V+file))||t.width!=513||t.height!=513)
   throw new Exception("Bad authority mask: "+file);
  var p=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return p;
 } static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Forest rollback missing");
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(Terrain);
  if(!td||td.alphamapResolution!=512||td.alphamapLayers!=7)
   throw new Exception("Unexpected Paradise West layer layout");
  var layers=td.terrainLayers;
  if(layers[4]&&layers[4].name=="ReferenceWestForestInterior"){
   File.WriteAllText(V+"paradise_interior_forest_20260927.txt","ALREADY_APPLIED");return;
  }
  if(!layers[0]||layers[0].name!="Realistic_Green"||
     !layers[4]||layers[4].name!="Realistic_Snow")
   throw new Exception("Forest/snow layer slots changed, aborting");
  using(var sha=SHA256.Create()){
   string current=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Terrain))).Replace("-","").ToLowerInvariant();
   if(current!=Expected)throw new Exception("Terrain changed since backup, no write");
  }
  var native=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Materials/RealisticWorld/Green.terrainlayer");
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(
   "Assets/World/WorldExtensions/Generated/ReferenceWestForestGreen.png");
  if(!native||!tex)throw new Exception("Verified forest texture unavailable");
  var photo=Pixels("ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var coast=Pixels("ReferenceMasks/ParadiseValley_West.png");
  var original=td.GetAlphamaps(0,0,512,512);
  var a=(float[,,])original.Clone();
  for(int z=0;z<512;z++)for(int x=0;x<512;x++)
   if(original[z,x,4]>.00001f)throw new Exception("Snow channel in use, cannot repurpose");
  int painted=0,woodland=0;double moved=0;float edge=0f;  for(int z=232;z<480;z++)for(int x=24;x<480;x++){
   float height=-24f+td.GetInterpolatedHeight(x/512f,z/512f);
   if(height<.85f||a[z,x,6]>.18f)continue;
   float dist=(coast[z*513+x].r-128f)*512f/(148f*3f);
   if(dist<8f)continue;
   var p=photo[z*513+x];float forest=p.g/255f,rock=p.r/255f;
   float woods=S(.27f,.68f,forest)*S(-.14f,.16f,forest-rock);
   if(woods<.10f||a[z,x,0]<.14f)continue;
   float slope=td.GetSteepness(x/512f,z/512f);
   float fade=S(24,60,x)*(1-S(436,480,x))*
              S(232,300,z)*(1-S(448,480,z));
   float w=.80f*woods*(1-S(18,34,slope))*fade*S(8,25,dist);
   float transfer=Mathf.Min(a[z,x,0]*.85f,w);
   if(transfer<.005f)continue;
   a[z,x,0]-=transfer;a[z,x,4]+=transfer;
   painted++;moved+=transfer;if(woods>.5f)woodland++;
  }
  if(painted<5000||woodland<2500)
   throw new Exception("Insufficient photo-confirmed interior: "+painted+"/"+woodland);
  for(int i=0;i<512;i++)for(int k=0;k<7;k++){
   edge=Mathf.Max(edge,Mathf.Abs(original[511,i,k]-a[511,i,k]));
   edge=Mathf.Max(edge,Mathf.Abs(original[i,511,k]-a[i,511,k]));
  }
  if(edge>.000001f)throw new Exception("Protected border changed");
  var variant=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Layer);
  if(!variant){
   variant=new TerrainLayer();EditorUtility.CopySerialized(native,variant);
   variant.name="ReferenceWestForestInterior";variant.diffuseTexture=tex;
   AssetDatabase.CreateAsset(variant,Layer);
  }
  layers[4]=variant;td.terrainLayers=layers;td.SetAlphamaps(0,0,a);
  EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  File.WriteAllText(V+"paradise_interior_forest_20260927.txt",
   "PASS photoForestPixels="+painted+" strongForest="+woodland+
   " shiftedGreenWeight="+moved.ToString("F0")+
   " borderAlphaError="+edge.ToString("F6")+" heightWrites=0 canonicalWrites=0\n");
 } static void Poll(){
  if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(EditorApplication.timeSinceStartup<next)return;
  next=EditorApplication.timeSinceStartup+4;
  var sc=SceneManager.GetActiveScene();
  if(sc.isDirty||sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity")return;
  File.Delete(Flag);
  try{
   Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   File.WriteAllText(V+"paradise_interior_forest_runtime_20260927.txt",
    "PASS photo woodland variant applied, linked world rebuilt and QA rendered\n");
  }catch(Exception ex){
   File.WriteAllText(V+"paradise_interior_forest_runtime_20260927.txt",
    "FAIL "+ex+"\n");Debug.LogException(ex);
  }
 }
}
