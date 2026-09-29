using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Photo-derived biome zoning. Only generated west/north tiles may change.
public static class MMReferenceParadiseAndNorthernGeology20260925 {
 const string G="Assets/World/WorldExtensions/Generated/";
 const string V="Validation/EdgeGrid20260923/";
 const string B=@"C:\MMUnityPort\Backups\BeforeParadiseNorthReferenceRework_20260925\manifest.json";
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);}
 static Color32[] Img(string p){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)
   throw new Exception("Source photo authority missing "+p);
  var px=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return px;
 }
 static int Id(TerrainData td,string n){
  int i=Array.FindIndex(td.terrainLayers,l=>l&&l.name=="Realistic_"+n);
  if(i<0)throw new Exception("Expected ground layer "+n+" missing");return i;
 }
 static string FixParadise(){
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"ParadiseValley_WestTerrain.asset");
  if(!td||td.alphamapLayers!=7)throw new Exception("Paradise generated terrain missing");
  var ph=Img(V+"ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png");
  var land=Img(V+"ReferenceMasks/ParadiseValley_West.png");
  var a=td.GetAlphamaps(0,0,512,512);
  int green=Id(td,"Green"),light=Id(td,"LightGreen"),rock=Id(td,"Volcanic");
  int arid=Id(td,"Arid"),sand=Id(td,"Desert"),road=Id(td,"RoadOverlay");
  var target=new float[7];int touched=0,forest=0,stone=0;
  double oldGreen=0,newGreen=0,oldRock=0,newRock=0;
  for(int z=0;z<512;z++)for(int x=0;x<512;x++){
   float d=(land[z*513+x].r-128f)*512f/(148f*3f);
   if(d<2f)continue;
   var p=ph[z*513+x];float f=p.g/255f,r=p.r/255f,tan=p.b/255f;
   float south=1f-S(120f,265f,z);
   float stoneStrength=Mathf.Max(r,.74f*south);
   float canopy=S(.18f,.61f,f)*(1f-.55f*stoneStrength);
   float coast=1f-S(2f,18f,d);
   Array.Clear(target,0,7);
   // Northern reference is DARK WOODLAND, not lawn. Forest ground combines
   // moss with dry leaf soil. The map's southern cape is exposed brown rock.
   target[green]=.025f+.34f*canopy;
   target[light]=.008f+.055f*canopy;
   target[rock]=.10f+.84f*stoneStrength;
   target[arid]=.26f+.35f*(1f-canopy)+.19f*tan;
   target[sand]=.015f+.18f*coast;
   target[road]=0f;
   float sum=target.Sum();for(int k=0;k<7;k++)target[k]/=sum;
   float influence=S(2f,18f,d)*(1f-S(467f,511f,x));
   if(influence<.001f)continue;
   oldGreen+=a[z,x,green]+a[z,x,light];oldRock+=a[z,x,rock]+a[z,x,arid];
   for(int k=0;k<7;k++)a[z,x,k]=Mathf.Lerp(a[z,x,k],target[k],influence);
   newGreen+=a[z,x,green]+a[z,x,light];newRock+=a[z,x,rock]+a[z,x,arid];
   if(f>.58f)forest++;if(stoneStrength>.58f)stone++;touched++;
  }
  if(touched<80000)throw new Exception("Paradise source map not aligned "+touched);
  td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);
  return "ParadiseWest photoMappedPixels="+touched+" forestPixels="+forest+
   " rockyPixels="+stone+" originalGreenMean="+(oldGreen/touched).ToString("F3")+
   " correctedGreenMean="+(newGreen/touched).ToString("F3")+
   " originalRockEarthMean="+(oldRock/touched).ToString("F3")+
   " correctedRockEarthMean="+(newRock/touched).ToString("F3")+
   " originalScenesTouched=0 heightWrites=0";
 }
 static float Gauss(float r,float mu,float sigma){
  float t=(r-mu)/sigma;return Mathf.Exp(-.5f*t*t);
 }
 static string ShapeNorthernReference(){
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"SweetWater_NorthTerrain.asset");
  if(!td)throw new Exception("Sweet Water north missing");
  var land=Img(V+"ReferenceMasks/SweetWater_North.png");
  var h=td.GetHeights(0,0,513,513);var alpha=td.GetAlphamaps(0,0,512,512);
  int rock=Id(td,"Volcanic"),soil=Id(td,"Arid");
  int modified=0,rocky=0;float maxMove=0,maxBorder=0;
  for(int z=140;z<=318;z++)for(int x=305;x<=497;x++){
   float authority=S(139f,157f,land[z*513+x].r);
   if(authority<.01f)continue;
   float vx=(x-462f)/30f,vz=(z-263f)/26f;
   float r=Mathf.Sqrt(vx*vx+vz*vz);
   float darkVent=(3.9f*Gauss(r,.63f,.14f)-5.3f*Gauss(r,0f,.29f))*(1f-S(.98f,1.24f,r));
   // The same source-map crop shows adjacent exposed irregular hills.
   float h1=6.1f*Mathf.Exp(-2.0f*(Mathf.Pow((x-365f)/43f,2)+Mathf.Pow((z-215f)/28f,2)));
   float h2=3.4f*Mathf.Exp(-2.0f*(Mathf.Pow((x-412f)/30f,2)+Mathf.Pow((z-193f)/25f,2)));
   float border=S(0f,20f,x)*S(0f,20f,512f-x)*S(0f,20f,z)*S(0f,20f,512f-z);
   float delta=(darkVent+h1+h2)*authority*border;
   if(Mathf.Abs(delta)<.003f)continue;
   float old=h[z,x];h[z,x]=Mathf.Clamp01(old+delta/320f);
   maxMove=Mathf.Max(maxMove,Mathf.Abs(h[z,x]-old)*320f);modified++;
   if(x<512&&z<512&&r<1.25f){
    float strength=(1f-S(.85f,1.24f,r))*authority;
    var w=new float[td.alphamapLayers];
    for(int k=0;k<w.Length;k++)w[k]=alpha[z,x,k];
    // Volcanic ash and brown exposed soil, not grass or snow on the rim.
    for(int k=0;k<w.Length;k++)w[k]*=1f-.78f*strength;
    w[rock]+=.59f*strength;w[soil]+=.23f*strength;
    float sum=w.Sum();for(int k=0;k<w.Length;k++)alpha[z,x,k]=w[k]/sum;
    rocky++;
   }
  }
  for(int z=0;z<513;z++){
   maxBorder=Mathf.Max(maxBorder,Mathf.Abs(h[z,0]-td.GetHeight(0,z)/320f)*320f);
   maxBorder=Mathf.Max(maxBorder,Mathf.Abs(h[z,512]-td.GetHeight(512,z)/320f)*320f);
  }
  if(maxMove>8f||maxBorder>.0001f||modified<1000)
   throw new Exception("Northern photo geology unsafe move="+maxMove+" edge="+maxBorder+" edits="+modified);
  td.SetHeights(0,0,h);td.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(td);
  return "SweetWater_North sourcePhotoWesternVolcanicVents=(462,263) nearbyRockyHills=(365,215)(412,193)"+
   " adjustedVertices="+modified+" exposedVentPixels="+rocky+
   " maxHeightChangeM="+maxMove.ToString("F3")+" sideEdgeErrorM="+maxBorder.ToString("F6");
 }
 static string AddMappedNorthernPond(){
  const string scenePath="Assets/Scenes/Kriegspire_North.unity";
  const string meshPath=G+"Kriegspire_North_ReferenceCraterPond.asset";
  if(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath))
   throw new Exception("Reference water mesh already exists; prevent duplicate pass");
  var source=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"Kriegspire_NorthTerrain.asset");
  if(!source)throw new Exception("Kriegspire northern terrain missing");
  var mask=Img(V+"ReferenceMasks/Kriegspire_North.png");
  if(mask[331*513+189].r<145)throw new Exception("Lake outside reference land");
  var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
  try{
   var root=scene.GetRootGameObjects().FirstOrDefault(x=>x.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
   if(!root)throw new Exception("Kriegspire extension scene root missing");
   if(root.transform.Find("Water - Reference Northern Crater Pond"))
    throw new Exception("Reference pond already in generated northern scene");
   var terrain=root.GetComponentInChildren<Terrain>(true);
   if(!terrain||terrain.terrainData!=source)throw new Exception("Unexpected Kriegspire standalone TerrainData");
   var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SourceGridTerrain/UnifiedEnrothWater.mat");
   if(!material||!material.shader||!material.shader.isSupported)
    throw new Exception("Native source water material unavailable");
   const int cx=189,cz=331,rays=96;
   float cy=terrain.transform.position.y+source.GetInterpolatedHeight(cx/512f,cz/512f);
   float waterY=cy+5.1f;
   var vertices=new Vector3[rays+1];var uv=new Vector2[rays+1];
   var center=new Vector3(terrain.transform.position.x+cx,waterY,terrain.transform.position.z+cz);
   vertices[0]=root.transform.InverseTransformPoint(center);uv[0]=new Vector2(.5f,.5f);
   float minRadius=999,maxRadius=0,area=0;
   for(int i=0;i<rays;i++){
    float a=2f*Mathf.PI*i/rays,c=Mathf.Cos(a),s=Mathf.Sin(a);
    float radius=24f;
    for(float r=1f;r<=24f;r+=.5f){
     float x=cx+c*r,z=cz+s*r;
     float ground=terrain.transform.position.y+
      source.GetInterpolatedHeight(x/512f,z/512f);
     if(ground>waterY-.27f){radius=Mathf.Max(1.3f,r-.7f);break;}
    }
    minRadius=Mathf.Min(minRadius,radius);maxRadius=Mathf.Max(maxRadius,radius);
    area+=Mathf.PI*radius*radius/rays;
    var point=new Vector3(center.x+radius*c,waterY,center.z+radius*s);
    vertices[i+1]=root.transform.InverseTransformPoint(point);
    uv[i+1]=new Vector2(.5f+radius*c/48f,.5f+radius*s/48f);
   }
   if(minRadius<2.5f||maxRadius>24f||area<35f)
    throw new Exception("Crater pond radius not supported by saved heightmap "+minRadius+"/"+maxRadius+" area="+area);
   var tri=new int[rays*3];
   for(int i=0;i<rays;i++){tri[3*i]=0;tri[3*i+1]=(i+1)%rays+1;tri[3*i+2]=i+1;}
   var mesh=new Mesh{name="Northern Source-Map Crater Lake"};
   mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tri;
   mesh.RecalculateNormals();mesh.RecalculateBounds();
   AssetDatabase.CreateAsset(mesh,meshPath);
   var go=new GameObject("Water - Reference Northern Crater Pond");
   go.transform.SetParent(root.transform,false);
   var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=mesh;
   var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=material;
   mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene,scenePath))
    throw new IOException("Could not save generated northern lake scene");
   return "Kriegspire_North referenceDarkBlueBasin=(189,331) bedElevationM="+
    cy.ToString("F2")+" waterLevelM="+waterY.ToString("F2")+
    " minRadiusM="+minRadius.ToString("F2")+" maxRadiusM="+maxRadius.ToString("F2")+
    " shorelineAreaSqM="+area.ToString("F1")+" savedMesh="+meshPath;
  }finally{EditorSceneManager.CloseScene(scene,true);}
 }
 [MenuItem("MMUnity/Reference 2026/Repair Paradise Ground and Northern Geology")]
 public static void Apply(){
  if(!File.Exists(B))throw new Exception("Pre-change reference backup missing");
  if(File.Exists(V+"paradise_north_geo_pass_runtime.txt"))
   throw new Exception("Reference geology already applied; do not double-add heights");
  var active=SceneManager.GetActiveScene();
  if(active.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||active.isDirty)
   throw new Exception("Saved linked scene must be active");
  if(AssetDatabase.LoadAssetAtPath<Mesh>(G+"Kriegspire_North_ReferenceCraterPond.asset"))
   throw new Exception("Crater water previously generated");
  var lines=new List<string>();
  lines.Add(FixParadise());lines.Add(ShapeNorthernReference());
  lines.Add(AddMappedNorthernPond());
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"paradise_north_geo_photo_match_20260925.txt",lines);
  Debug.Log("REFERENCE_PHOTO_GEOGRAPHY_PASS "+string.Join(" | ",lines));
 }
}
[InitializeOnLoad]
internal static class MMParadiseNorthGeologyQueued20260925{
 const string V="Validation/EdgeGrid20260923/";
 static MMParadiseNorthGeologyQueued20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_PARADISE_NORTH_GEOLOGY.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_PARADISE_NORTH_GEOLOGY.flag");
  try{
   MMReferenceParadiseAndNorthernGeology20260925.Apply();
   BuildEnrothLinkedOpenWorld.Build();
   File.WriteAllText(V+"paradise_north_geo_pass_runtime.txt",
    "PASS photo-ground west, north vent+mapped rocky hills, high-altitude crater pond and linked rebuild\n");
  }catch(Exception ex){
   File.WriteAllText(V+"paradise_north_geo_error.txt","FAIL "+ex+"\n");
   Debug.LogError(ex);
  }
 }
}
