using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;

public static class MMNorthManualEdgeHeightRebuild20261006
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string ReportDir="Validation/EdgeGrid20260923/NorthManualEdgeHeight20261006";
 const string ReportPath=ReportDir+"/apply.txt";

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
 static float G(float x,float z,float cx,float cz,float rx,float rz){float dx=(x-cx)/rx,dz=(z-cz)/rz;return Mathf.Exp(-1.75f*(dx*dx+dz*dz));}

 static float MountainArc(float x,float z){
  float m=0f;
  m=Mathf.Max(m,34f*G(x,z,-1100,945,115,85));
  m=Mathf.Max(m,42f*G(x,z,-930,995,125,92));
  m=Mathf.Max(m,48f*G(x,z,-755,1045,130,98));
  m=Mathf.Max(m,52f*G(x,z,-570,1080,135,100));
  m=Mathf.Max(m,50f*G(x,z,-385,1095,135,100));
  m=Mathf.Max(m,46f*G(x,z,-195,1085,130,98));
  m=Mathf.Max(m,42f*G(x,z,-10,1055,125,94));
  m=Mathf.Max(m,36f*G(x,z,170,1015,118,90));
  m=Mathf.Max(m,30f*G(x,z,335,965,110,84));
  m*=1f-.45f*G(x,z,-845,1015,62,54);
  m*=1f-.40f*G(x,z,-470,1085,64,52);
  m*=1f-.42f*G(x,z,-100,1065,60,50);
  m*=1f-.46f*G(x,z,245,990,58,48);
  return m;
 }

 static float Channel(float x,float z,float z0,float z1,float x0,float x1,float width,float phase){
  if(z<z0||z>z1)return 99f;
  float t=(z-z0)/(z1-z0);
  float cx=Mathf.Lerp(x0,x1,t)+Mathf.Sin(t*6.283f+phase)*13f+Mathf.Sin(t*14.5f+phase*.6f)*4f;
  return Mathf.Abs(x-cx)/width;
 }

 static float InteriorHeight(float x,float z,float edge){
  float n0=(Mathf.PerlinNoise((x+1700f)*.0065f,(z+500f)*.0065f)-.5f)*5.0f;
  float n1=(Mathf.PerlinNoise((x+280f)*.019f,(z+1500f)*.019f)-.5f)*1.8f;
  float plateau=Mathf.Clamp(11f+.46f*Mathf.Max(edge,0f)+n0+n1,8f,31f);
  plateau+=4.5f*G(x,z,-1080,885,180,110)+3.5f*G(x,z,390,885,165,105);
  float y=plateau+MountainArc(x,z);

  float lake=Mathf.Max(G(x,z,-20,1015,145,66),Mathf.Max(.75f*G(x,z,-88,1025,90,52),.70f*G(x,z,52,998,84,48)));
  y=Mathf.Lerp(y,18.2f,S(.12f,.72f,lake));

  float d1=Channel(x,z,1010,1170,-10,115,12f,.5f);
  float d2=Channel(x,z,1018,1155,-60,-300,10f,1.4f);
  float d3=Channel(x,z,1020,1145,50,255,10f,2.2f);
  foreach(float q in new[]{d1,d2,d3})if(q<2.4f){
   float w=1f-S(.65f,2.4f,q);
   y=Mathf.Lerp(y,20.0f,w*.72f);
  }
  return y;
 }

 static Terrain Find(Terrain[] ts,float x,float z){
  return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);
 }
 static float Ground(Terrain t,float x,float z){return t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));}
 static float Y(Terrain t,float[,] h,int x,int z){return t.transform.position.y+h[z,x]*t.terrainData.size.y;}
 static void SetY(Terrain t,float[,] h,int x,int z,float y){h[z,x]=Mathf.Clamp01((y-t.transform.position.y)/t.terrainData.size.y);}

 static string Hash(string assetPath){
  string abs=Path.Combine(Directory.GetCurrentDirectory(),assetPath.Replace('/','\\'));
  using(var sha=SHA256.Create())using(var fs=File.OpenRead(abs))return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-","").ToLowerInvariant();
 }

 static void GroundExtensionObjects(Scene scene,Terrain[] ext,out int moved,out int disabled){
  moved=0;disabled=0;
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  foreach(var tr in all){
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
   if(gy<=.20f){
    if(tr.gameObject.activeSelf){tr.gameObject.SetActive(false);disabled++;}
    continue;
   }
   var rs=tr.GetComponentsInChildren<Renderer>(false);if(rs.Length==0)continue;
   Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);
   float gap=b.min.y-gy;
   if(Mathf.Abs(gap)>.05f){tr.position=new Vector3(p.x,p.y-gap,p.z);moved++;}
  }
 }

 [MenuItem("MMUnity/Reference 2026/Rebuild North Heights From Manual Legacy Edge")]
 public static void Run(){
  Directory.CreateDirectory(ReportDir);
  if(File.Exists(ReportPath))throw new Exception("Manual-edge height rebuild already applied; refusing cumulative rerun.");

  var before=ManualLegacyPaths.ToDictionary(p=>p,p=>Hash(p));
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Enroth must open clean.");

  var all=Terrain.activeTerrains;
  var ext=new Terrain[4];
  var hs=new float[4][,];
  var legacySouth=new Terrain[4];

  for(int i=0;i<4;i++){
   var td=AssetDatabase.LoadAssetAtPath<TerrainData>(ExtPaths[i]);
   if(!td)throw new Exception("Missing extension TerrainData: "+ExtPaths[i]);
   ext[i]=all.FirstOrDefault(t=>t.terrainData==td);
   if(!ext[i])throw new Exception("Extension terrain not live: "+ExtPaths[i]);
   hs[i]=td.GetHeights(0,0,513,513);
   legacySouth[i]=all.FirstOrDefault(t=>Mathf.Abs(t.transform.position.x-ext[i].transform.position.x)<.1f&&Mathf.Abs(t.transform.position.z-256f)<.1f);
   if(!legacySouth[i])throw new Exception("Legacy north-edge source missing for "+ext[i].name);
  }

  int changed=0;

  // Exact manual legacy edge + short slope continuation, extension-side only.
  for(int i=0;i<4;i++){
   var t=ext[i];var h=hs[i];var south=legacySouth[i];
   for(int x=0;x<=512;x++){
    float wx=t.transform.position.x+x;
    float edge=Ground(south,wx,768f);
    float back32=Ground(south,wx,736f),back64=Ground(south,wx,704f);
    float legacySlope=.58f*Mathf.Clamp((edge-back32)/32f,-.14f,.14f)+.42f*Mathf.Clamp((edge-back64)/64f,-.14f,.14f);
    float old0=Y(t,h,x,0),old64=Y(t,h,x,64);
    float oldSlope=Mathf.Clamp((old64-old0)/64f,-.14f,.14f);
    float delta=edge-old0,slopeDelta=legacySlope-oldSlope;
    for(int z=0;z<=180;z++){
     float old=Y(t,h,x,z),wd=1f-S(0f,180f,z),ws=1f-S(0f,105f,z);
     float ny=old+delta*wd+slopeDelta*z*ws;if(z==0)ny=edge;
     if(Mathf.Abs(ny-old)>.005f){SetY(t,h,x,z,ny);changed++;}
    }
   }
  }

  // Reconstruct the extension interior from the manual legacy height scale.
  for(int i=0;i<4;i++){
   var t=ext[i];var h=hs[i];var south=legacySouth[i];
   for(int x=0;x<=512;x++){
    float wx=t.transform.position.x+x;
    float edge=Ground(south,wx,768f);
    float back=Ground(south,wx,704f);
    float slope=Mathf.Clamp((edge-back)/64f,-.12f,.12f);
    if(edge<-.25f)continue;
    for(int z=80;z<=430;z++){
     float wz=768f+z,old=Y(t,h,x,z);
     float target=InteriorHeight(wx,wz,edge);
     float inherited=edge+slope*Mathf.Min(z,110);
     target=Mathf.Lerp(inherited,target,S(80f,210f,z));
     float ny=Mathf.Lerp(old,target,S(80f,205f,z));
     if(Mathf.Abs(ny-old)>.005f){SetY(t,h,x,z,ny);changed++;}
    }
   }
  }

  // Step the actual north sea edge into a broad snow shelf + irregular two-stage arctic cliff.
  for(int i=0;i<4;i++){
   var t=ext[i];var h=hs[i];
   for(int x=0;x<=512;x++){
    float wx=t.transform.position.x+x;
    int coast=512;bool found=false;
    for(int z=512;z>=180;z--){if(Y(t,h,x,z)>1.0f){coast=z;found=true;break;}}
    if(!found)continue;

    float coastWarp=(Mathf.PerlinNoise((wx+1450f)*.012f,.19f)-.5f)*18f+(Mathf.PerlinNoise((wx-260f)*.034f,.79f)-.5f)*6f;
    float inletWarp=10f*Mathf.Exp(-Mathf.Pow((wx+1030f)/72f,2f))+8f*Mathf.Exp(-Mathf.Pow((wx+520f)/58f,2f))+9f*Mathf.Exp(-Mathf.Pow((wx-470f)/68f,2f));
    coast=Mathf.Clamp(coast+Mathf.RoundToInt(coastWarp-inletWarp),180,512);

    int a=Mathf.Max(180,coast-128),b=Mathf.Max(180,coast-92),c=Mathf.Max(180,coast-64);
    float shelf=.40f*Y(t,h,x,a)+.35f*Y(t,h,x,b)+.25f*Y(t,h,x,c);
    shelf=Mathf.Clamp(shelf,18f,38f);
    shelf+=(Mathf.PerlinNoise((wx+1600f)*.010f,.37f)-.5f)*5f+(Mathf.PerlinNoise((wx-420f)*.027f,.71f)-.5f)*2f;

    float cove=Mathf.Max(Mathf.Exp(-Mathf.Pow((wx+1030f)/72f,2f)),Mathf.Max(Mathf.Exp(-Mathf.Pow((wx+520f)/58f,2f)),Mathf.Exp(-Mathf.Pow((wx-470f)/68f,2f))));
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

  // Match the external west/east terrain neighbors without editing those neighbors.
  var westNeighbor=all.FirstOrDefault(q=>Mathf.Abs(q.transform.position.x+1792f)<.1f&&Mathf.Abs(q.transform.position.z-768f)<.1f);
  var eastNeighbor=all.FirstOrDefault(q=>Mathf.Abs(q.transform.position.x-768f)<.1f&&Mathf.Abs(q.transform.position.z-768f)<.1f);
  if(westNeighbor){
   var t=ext[0];var h=hs[0];
   for(int z=0;z<=512;z++)for(int x=0;x<=52;x++){
    float wz=768f+z,edge=Ground(westNeighbor,-1280f,wz),old=Y(t,h,x,z),ny=Mathf.Lerp(edge,old,S(0,52,x));
    SetY(t,h,x,z,ny);
   }
  }
  if(eastNeighbor){
   var t=ext[3];var h=hs[3];
   for(int z=0;z<=512;z++)for(int x=460;x<=512;x++){
    float wz=768f+z,edge=Ground(eastNeighbor,768f,wz),old=Y(t,h,x,z),ny=Mathf.Lerp(old,edge,S(460,512,x));
    SetY(t,h,x,z,ny);
   }
  }

  // Exact internal extension seams.
  for(int i=0;i<3;i++)for(int z=0;z<=512;z++){
   float yy=.5f*(Y(ext[i],hs[i],512,z)+Y(ext[i+1],hs[i+1],0,z));
   SetY(ext[i],hs[i],512,z,yy);SetY(ext[i+1],hs[i+1],0,z,yy);
  }

  // Final authority: the user's manually fixed legacy north edge is immutable and exact.
  // Re-pin every extension south-edge sample after all other seam operations.
  for(int i=0;i<4;i++){
   var t=ext[i];var south=legacySouth[i];
   for(int x=0;x<=512;x++){
    float wx=t.transform.position.x+x;
    SetY(t,hs[i],x,0,Ground(south,wx,768f));
   }
  }

  for(int i=0;i<4;i++){
   ext[i].terrainData.SetHeights(0,0,hs[i]);
   ext[i].terrainData.SetBaseMapDirty();
   EditorUtility.SetDirty(ext[i].terrainData);
   ext[i].Flush();
  }

  GroundExtensionObjects(scene,ext,out int moved,out int disabled);
  if(moved>0||disabled>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save Enroth after extension-only re-grounding.");}
  AssetDatabase.SaveAssets();

  var after=ManualLegacyPaths.ToDictionary(p=>p,p=>Hash(p));
  foreach(var p in ManualLegacyPaths)if(before[p]!=after[p])throw new Exception("LEGACY TERRAIN CHANGED: "+p);

  File.WriteAllLines(ReportPath,new[]{
   "PASS manual-legacy-edge height-only north rebuild",
   "terrainPainting=false",
   "alphamapsModified=false",
   "legacyTerrainWrites=0",
   "manualLegacyHashInvariant=true",
   "extensionTerrainCount=4",
   "changedHeightSamples="+changed,
   "extensionObjectsGrounded="+moved,
   "extensionObjectsDisabledInWater="+disabled,
   "sceneDirtyAfterSave="+scene.isDirty
  });
  Debug.Log("NORTH_MANUAL_EDGE_HEIGHT_REBUILD_DONE changed="+changed+" grounded="+moved+" disabled="+disabled);
 }
}
