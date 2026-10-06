using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

public static class MMNorthManualEdgeCliffRefine20261006
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string ReportDir="Validation/EdgeGrid20260923/NorthManualEdgeHeight20261006";
 const string ReportPath=ReportDir+"/cliff_refine.txt";

 static readonly string[] ExtPaths={
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"};

 static readonly string[] ManualLegacyPaths={
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/b2adcca80c21fb542bd822ac5a7b75cf.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/9a29f5601247e3f4ba63f7800b155bea.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/d85d07a7edcfde747bfd4436baa76b47.asset"};

 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static float Y(Terrain t,float[,] h,int x,int z){return t.transform.position.y+h[z,x]*t.terrainData.size.y;}
 static void SetY(Terrain t,float[,] h,int x,int z,float y){h[z,x]=Mathf.Clamp01((y-t.transform.position.y)/t.terrainData.size.y);}
 static Terrain Find(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);}
 static float Ground(Terrain t,float x,float z){return t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));}

 static string Hash(string assetPath){
  string abs=Path.Combine(Directory.GetCurrentDirectory(),assetPath.Replace('/',Path.DirectorySeparatorChar));
  using(var sha=SHA256.Create())using(var fs=File.OpenRead(abs))return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-","").ToLowerInvariant();
 }

 static void GroundExtensionObjects(Scene scene,Terrain[] ext,out int moved,out int disabled){
  moved=0;disabled=0;
  foreach(var tr in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))){
   Vector3 p=tr.position;
   if(p.x<-1280f||p.x>768f||p.z<768f||p.z>1280f)continue;
   bool candidate=
    tr.name.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase)||
    tr.name.StartsWith("Eco_",StringComparison.OrdinalIgnoreCase)||
    tr.name.StartsWith("NorthRefForestCluster_",StringComparison.OrdinalIgnoreCase)||
    tr.name.StartsWith("NorthCoastalForest_",StringComparison.OrdinalIgnoreCase);
   if(!candidate)continue;
   var terr=Find(ext,p.x,p.z);if(!terr)continue;
   float gy=Ground(terr,p.x,p.z);
   if(gy<=.20f){if(tr.gameObject.activeSelf){tr.gameObject.SetActive(false);disabled++;}continue;}
   var rs=tr.GetComponentsInChildren<Renderer>(false);if(rs.Length==0)continue;
   Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);
   float gap=b.min.y-gy;
   if(Mathf.Abs(gap)>.05f){tr.position=new Vector3(p.x,p.y-gap,p.z);moved++;}
  }
 }

 [MenuItem("MMUnity/Reference 2026/Refine North Cliff From Manual Edge")]
 public static void Run(){
  Directory.CreateDirectory(ReportDir);
  if(File.Exists(ReportPath))throw new Exception("North cliff refinement already applied; refusing cumulative rerun.");

  var legacyBefore=ManualLegacyPaths.ToDictionary(p=>p,p=>Hash(p));
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Enroth must open clean.");

  var all=Terrain.activeTerrains;
  var ext=new Terrain[4];var hs=new float[4][,];
  for(int i=0;i<4;i++){
   var td=AssetDatabase.LoadAssetAtPath<TerrainData>(ExtPaths[i]);
   if(!td)throw new Exception("Missing extension TerrainData: "+ExtPaths[i]);
   ext[i]=all.FirstOrDefault(t=>t.terrainData==td);
   if(!ext[i])throw new Exception("Extension terrain not live: "+ExtPaths[i]);
   hs[i]=td.GetHeights(0,0,513,513);
  }

  int changed=0;
  for(int i=0;i<4;i++){
   var t=ext[i];var h=hs[i];
   for(int x=0;x<=512;x++){
    float wx=t.transform.position.x+x;
    int coast=512;bool found=false;
    for(int z=512;z>=180;z--){if(Y(t,h,x,z)>1.0f){coast=z;found=true;break;}}
    if(!found)continue;

    int a=Mathf.Max(180,coast-128),b=Mathf.Max(180,coast-92),c=Mathf.Max(180,coast-64);
    float shelf=.40f*Y(t,h,x,a)+.35f*Y(t,h,x,b)+.25f*Y(t,h,x,c);
    shelf=Mathf.Clamp(shelf,18f,38f);
    shelf+=(Mathf.PerlinNoise((wx+1600f)*.010f,.37f)-.5f)*5f+(Mathf.PerlinNoise((wx-420f)*.027f,.71f)-.5f)*2f;

    float cove=Mathf.Max(
      Mathf.Exp(-Mathf.Pow((wx+1030f)/72f,2f)),
      Mathf.Max(Mathf.Exp(-Mathf.Pow((wx+520f)/58f,2f)),Mathf.Exp(-Mathf.Pow((wx-470f)/68f,2f))));
    shelf=Mathf.Lerp(shelf,Mathf.Max(13f,shelf-10f),.72f*cove);

    float sectionNoise=Mathf.PerlinNoise((wx+720f)*.018f,.51f);
    float gullyMask=S(.56f,.78f,Mathf.PerlinNoise((wx+190f)*.0105f,.66f));
    gullyMask=Mathf.Max(gullyMask,.85f*S(.60f,.80f,Mathf.PerlinNoise((wx+980f)*.015f,.29f)));
    float toeY=1.8f+(Mathf.PerlinNoise((wx+940f)*.024f,.61f)-.5f)*1.8f;
    float cliffW=Mathf.Lerp(13f,34f,gullyMask)+Mathf.Lerp(-2f,3f,sectionNoise);
    float capBlendW=68f+24f*Mathf.PerlinNoise((wx+300f)*.012f,.83f);

    int start=Mathf.Max(180,coast-125);
    for(int z=start;z<=512;z++){
     float dist=coast-z,old=Y(t,h,x,z),ny=old;
     if(z>coast)ny=-7.5f;
     else if(dist<=cliffW){
      float q=S(0f,cliffW,dist);
      float face=Mathf.Lerp(toeY,shelf,q);
      float rough=(Mathf.PerlinNoise((wx+z)*.041f,.31f)-.5f)*2.0f*q*(1f-q);
      ny=face+rough;
     }else{
      float cap=shelf+(Mathf.PerlinNoise((wx+z)*.018f,.82f)-.5f)*1.6f;
      ny=Mathf.Lerp(cap,old,S(cliffW,cliffW+capBlendW,dist));
     }
     if(Mathf.Abs(ny-old)>.005f){SetY(t,h,x,z,ny);changed++;}
    }
   }
  }

  // Exact internal extension seams only. Legacy terrain remains read-only.
  for(int i=0;i<3;i++)for(int z=0;z<=512;z++){
   float yy=.5f*(Y(ext[i],hs[i],512,z)+Y(ext[i+1],hs[i+1],0,z));
   SetY(ext[i],hs[i],512,z,yy);SetY(ext[i+1],hs[i+1],0,z,yy);
  }

  for(int i=0;i<4;i++){
   ext[i].terrainData.SetHeights(0,0,hs[i]);
   ext[i].terrainData.SetBaseMapDirty();
   EditorUtility.SetDirty(ext[i].terrainData);
   ext[i].Flush();
  }

  GroundExtensionObjects(scene,ext,out int moved,out int disabled);
  if(moved>0||disabled>0){
   EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save Enroth after cliff refinement grounding.");
  }
  AssetDatabase.SaveAssets();

  foreach(var p in ManualLegacyPaths)if(legacyBefore[p]!=Hash(p))throw new Exception("LEGACY TERRAIN CHANGED: "+p);

  File.WriteAllLines(ReportPath,new[]{
   "PASS coast-only height refinement",
   "terrainPainting=false",
   "alphamapsModified=false",
   "legacyTerrainWrites=0",
   "manualLegacyHashInvariant=true",
   "changedHeightSamples="+changed,
   "extensionObjectsGrounded="+moved,
   "extensionObjectsDisabledInWater="+disabled,
   "sceneDirtyAfterSave="+scene.isDirty
  });
  Debug.Log("NORTH_MANUAL_EDGE_CLIFF_REFINE_DONE changed="+changed+" grounded="+moved+" disabled="+disabled);
 }
}
