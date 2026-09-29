using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
// One-time, generated-only warm headland pass from the approved authority raster.
[InitializeOnLoad]
public static class MMParadiseOchreCape20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string Flag=V+"RUN_PARADISE_OCHRE_CAPE_20260927.flag";
 const string TerrainPath="Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset";
 const string TexturePath="Assets/World/WorldExtensions/Generated/ReferenceWestOchreCape.png";
 const string LayerPath="Assets/World/WorldExtensions/Generated/ReferenceWestOchreCape.terrainlayer";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeParadiseOchreMaterial_20260927\manifest.json";
 const string Expected="6ba2d474ee458af03d26b714298bee6b1a4c765876928f24a5b0e6b53895855e";
 static double due;
 static MMParadiseOchreCape20260927(){EditorApplication.update+=Poll;}
 static float S(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.01f,b-a));return t*t*(3-2*t);}
 static Color32[] Load(string rel){
  var im=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!im.LoadImage(File.ReadAllBytes(V+rel))||im.width!=513||im.height!=513)
   throw new Exception("Authority photo/mask missing: "+rel);
  var px=im.GetPixels32();UnityEngine.Object.DestroyImmediate(im);return px;
 }
 static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Ochre rollback manifest missing");
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
  if(!td||td.alphamapResolution!=512||td.heightmapResolution!=513||td.alphamapLayers!=7)
   throw new Exception("Unexpected west terrain dimensions/layers");
  var oldLayers=td.terrainLayers;
  if(oldLayers[2].name!="Realistic_Desert"||
     oldLayers[4].name!="ReferenceWestForestInterior"||
     oldLayers[5].name!="Realistic_Arid")
   throw new Exception("West material state changed; do not overwrite");
  if(AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath))
   throw new Exception("Ochre layer already present, one-time edit refused");
  using(var sha=SHA256.Create()){
   var actual=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(TerrainPath)))
    .Replace("-","").ToLowerInvariant();
   if(actual!=Expected)throw new Exception("West SHA changed since backup");
  }
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
  if(!tex||tex.width!=1024||tex.height!=1024)
   throw new Exception("New generated-only ochre diffuse missing");
  var photo=Load("ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var coast=Load("ReferenceMasks/ParadiseValley_West.png");
  var old=td.GetAlphamaps(0,0,512,512);
  var a=new float[512,512,8];
  for(int z=0;z<512;z++)for(int x=0;x<512;x++)
   for(int k=0;k<7;k++)a[z,x,k]=old[z,x,k];
  int painted=0,water=0,forest=0;double moved=0;float northGap=0,eastGap=0;
  for(int z=18;z<280;z++)for(int x=13;x<487;x++){
   float height=-24f+td.GetInterpolatedHeight(x/512f,z/512f);
   float coastM=(coast[z*513+x].r-128f)*512f/(148f*3f);
   if(height<.6f||coastM<7f){water++;continue;}
   if(old[z,x,4]>.025f||old[z,x,6]>.08f){forest++;continue;}
   var p=photo[z*513+x];
   float woods=p.g/255f;
   float stone=Mathf.Max(p.r/255f,.72f*p.b/255f);
   float rock=stone*(1f-.68f*woods);
   if(woods>.42f||rock<.16f||old[z,x,2]<.12f){forest++;continue;}
   float cape=1f-S(124f,280f,z);
   float xFade=S(12f,36f,x)*(1f-S(436f,487f,x));
   float w=.96f*cape*xFade*S(.12f,.47f,rock)*
    S(7f,25f,coastM);
   if(w<.003f)continue;
   float fromDesert=old[z,x,2]*.86f*w;
   float fromArid=old[z,x,5]*.55f*w;
   a[z,x,2]-=fromDesert;a[z,x,5]-=fromArid;
   a[z,x,7]=fromDesert+fromArid;
   moved+=a[z,x,7];painted++;
  }
  if(painted<4500||moved<700)
   throw new Exception("Authority brown cape paint too small "+painted+"/"+moved);
  for(int i=0;i<512;i++){
   for(int k=0;k<7;k++){
    northGap=Mathf.Max(northGap,Mathf.Abs(old[511,i,k]-a[511,i,k]));
    eastGap=Mathf.Max(eastGap,Mathf.Abs(old[i,511,k]-a[i,511,k]));
   }
   northGap=Mathf.Max(northGap,a[511,i,7]);
   eastGap=Mathf.Max(eastGap,a[i,511,7]);
  }
  if(northGap>.000001f||eastGap>.000001f)
   throw new Exception("West original join material border changed");
  var layer=new TerrainLayer();
  EditorUtility.CopySerialized(oldLayers[2],layer);
  layer.name="ReferenceWestOchreCape";layer.diffuseTexture=tex;
  AssetDatabase.CreateAsset(layer,LayerPath);
  var ls=new TerrainLayer[8];
  Array.Copy(oldLayers,ls,7);ls[7]=layer;
  td.terrainLayers=ls;
  td.SetAlphamaps(0,0,a);
  EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  File.WriteAllText(V+"paradise_ochre_cape_20260927.txt",
   "PASS photoDryCapePixels="+painted+" ochreAlphaSum="+moved.ToString("F1")+
   " waterProtected="+water+" woodlandAndRoadProtected="+forest+
   " northAlphaGap="+northGap.ToString("F6")+
   " eastAlphaGap="+eastGap.ToString("F6")+
   " heightWrites=0 canonicalWrites=0 layerCount=8\n");
 }
 static void Poll(){
  if(!File.Exists(Flag)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(EditorApplication.timeSinceStartup<due)return;
  due=EditorApplication.timeSinceStartup+4;
  var sc=SceneManager.GetActiveScene();
  if(sc.isDirty||sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity")return;
  File.Delete(Flag);
  try{
   Apply();
   BuildEnrothLinkedOpenWorld.Build();
   MMReference20260925VisualQAOnce.RunFinalQA();
   var td=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
   if(!td||td.alphamapLayers!=8||
      td.terrainLayers[7].name!="ReferenceWestOchreCape")
    throw new Exception("Ochre material lost after linked rebuild");
   File.WriteAllText(V+"paradise_ochre_cape_runtime_20260927.txt",
    "PASS warm photo headland material saved, linked world rebuilt, visual QA captured\n");
  }catch(Exception ex){
   File.WriteAllText(V+"paradise_ochre_cape_runtime_20260927.txt",
    "FAIL "+ex+"\n");
   Debug.LogException(ex);
  }
 }
}

// post-ochre perspective QA, 20260927
