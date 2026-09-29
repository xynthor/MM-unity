using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMDragonIsleReferenceTraceBuilder {
 const string Dir="Assets/TempDragonTemplate/";
 const string ScenePath="Assets/Scenes/__PREVIEW_DragonIsle_ReferenceTrace.unity";
 const string TerrainPath=Dir+"DragonIsleReferenceTerrain_PREVIEW.asset";
 const string Result="Validation/CactusDragonAudit/dragon_reference_trace_report.txt";
 const float Water=.12f;
 static readonly System.Random rng=new System.Random(230991);
 static Vector2[] coast,islet,lake,crater,forestW,forestE,forestS,forestSE,meadow;
 static float Rand(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
 static float Step(float a,float b,float x){float q=Mathf.Clamp01((x-a)/(b-a));return q*q*(3-2*q);}
 static float G(float x,float z,float cx,float cz,float rx,float rz) {
  float u=(x-cx)/rx,v=(z-cz)/rz;return Mathf.Exp(-1.7f*(u*u+v*v));}
 const float TraceTiltDeg=12f;
 static readonly Vector2 TracePivot=new Vector2(-25f,-18f);
 static Vector2 RotateTrace(Vector2 q,float deg){
  float a=deg*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);q-=TracePivot;
  return TracePivot+new Vector2(c*q.x-s*q.y,s*q.x+c*q.y);
 }
 static Vector2 Map(Vector2 p){
  float dx=p.x-610f,n=610f-p.y;
  var q=new Vector2((.8f*dx-.6f*n)*.40f,(.6f*dx+.8f*n-100f)*.35f);
  return RotateTrace(q,-TraceTiltDeg);
 }
 static Vector2 Pixel(float x,float z){
  var q=RotateTrace(new Vector2(x,z),TraceTiltDeg);
  float t=q.y/.35f+100f,c=q.x/.40f;
  return new Vector2(610f+.6f*t+.8f*c,610f-(.8f*t-.6f*c));
 }
 static Vector2[] Read(string file){return File.ReadAllLines(Dir+file+".csv")
  .Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>s.Split(',')).Select(a=>new Vector2(
  float.Parse(a[0],System.Globalization.CultureInfo.InvariantCulture),
  float.Parse(a[1],System.Globalization.CultureInfo.InvariantCulture))).ToArray();}
 static void Load(){coast=Read("coast");islet=Read("islet");lake=Read("lake");crater=Read("crater");
  forestW=Read("forest_west");forestE=Read("forest_east");forestS=Read("forest_south");
  forestSE=Read("forest_se");meadow=Read("meadow");}
 static bool Inside(Vector2 p,Vector2[] poly) {
  bool b=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++) {
   var a=poly[i];var q=poly[j];
   if((a.y>p.y)!=(q.y>p.y)&&p.x<(q.x-a.x)*(p.y-a.y)/(q.y-a.y)+a.x)b=!b;
  }return b;
 }
 static float EdgeDist(Vector2 p,Vector2[] poly) {
  float best=float.MaxValue;
  for(int i=0;i<poly.Length;i++){Vector2 a=poly[i],b=poly[(i+1)%poly.Length];
   var ab=b-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(1e-5f,ab.sqrMagnitude));
   float dd=(p-(a+ab*f)).sqrMagnitude;if(dd<best)best=dd;
  }return Mathf.Sqrt(best);
 }
 static float Region(Vector2 p,Vector2[] poly,float fade) {
  float d=EdgeDist(p,poly);return Step(-fade,fade,Inside(p,poly)?d:-d);
 }
 static float Habitat(Vector2 p) {
  float fw=Mathf.Max(Region(p,forestW,13),Region(p,forestE,13));
  float fs=Mathf.Max(Region(p,forestS,14),Region(p,forestSE,12));
  float open=Region(p,meadow,11);
  return Mathf.Max(fw,fs)*(1f-open*.88f);
 }
 static float Mesa(float x,float z,float cx,float cz,float rx,float rz,float h){
  float dx=(x-cx)/rx,dz=(z-cz)/rz,ang=Mathf.Atan2(dz,dx);
  float warp=1f+.17f*Mathf.Sin(5f*ang+.67f)+.09f*Mathf.Sin(9f*ang-1.2f)
             +.10f*(Mathf.PerlinNoise((x+284f)*.071f,(z+692f)*.071f)-.5f);
  float r=Mathf.Sqrt(dx*dx+dz*dz)/Mathf.Max(.69f,warp);
  float shoulder=1f-Step(.45f,1.32f,r);
  float ledge=Step(.56f,.61f,r)*(1f-Step(.72f,.78f,r))*.09f;
  return h*(shoulder+ledge);
 }
 static float Ridge(float x,float z,Vector2 a,Vector2 b,float halfWidth,float height){
  var q=new Vector2(x,z);var t=b-a;float u=Mathf.Clamp01(Vector2.Dot(q-a,t)/t.sqrMagnitude);
  float d=Vector2.Distance(q,a+t*u);return height*(1f-Step(0,halfWidth,d))*Mathf.Sin(Mathf.PI*(.12f+.76f*u));
 }
 static float Shape(float x,float z,Vector2 pp,float d,bool smallIsland){
  float baseH=Water+.55f+1.9f*Step(0,8.2f,d)+.26f*Mathf.Min(d,34f);
  if(smallIsland){return baseH+Mesa(x,z,11f,-222f,16f,13f,13f);}
  float sw=Ridge(x,z,new Vector2(-112,-219),new Vector2(-52,-159),20,28)
           +13f*G(x,z,-88,-197,25,31)+6f*G(x,z,-70,-180,22,35);
  float se=Ridge(x,z,new Vector2(55,-194),new Vector2(-3,-145),19,23)
           +10f*G(x,z,32,-169,24,27);
  float north=Mesa(x,z,-102,1,13,20,30)+Mesa(x,z,-74,44,14,17,34)+Mesa(x,z,-6,62,16,19,28);
  float upland=7f*G(x,z,-38,32,53,51)+3f*G(x,z,-45,81,42,43);
  float ang=Mathf.Atan2((z-30f)/18f,(x+49f)/22f);
  float rw=1f+.09f*Mathf.Sin(5f*ang)+.055f*Mathf.Sin(9f*ang+1.6f);
  float cr=Mathf.Sqrt(Mathf.Pow((x+49f)/22f,2)+Mathf.Pow((z-30f)/18f,2))/rw;
  float crRim=9f*Mathf.Exp(-Mathf.Pow((cr-1f)/.24f,2));
  float crBowl=-6f*(1f-Step(0,.82f,cr));
  float valley=-2.8f*G(x,z,-33,-107,46,51);
  float micro=(Mathf.PerlinNoise((x+414f)*.028f,(z+633f)*.028f)-.5f)
             +.36f*(Mathf.PerlinNoise((x+19f)*.086f,(z+43f)*.086f)-.5f);
  float snowTail=Step(114f,183f,z);
  return baseH+(sw+se+north+upland+crRim+crBowl+valley+micro*.72f)*(1f-snowTail*.35f);
 }
 static float[,] Distance(bool[,] land,int n){
  var dist=new float[n,n];const float inf=99999f;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++)dist[z,x]=land[z,x]?inf:0;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(land[z,x]){
   float v=dist[z,x];
   if(x>0)v=Mathf.Min(v,dist[z,x-1]+1);
   if(z>0){v=Mathf.Min(v,dist[z-1,x]+1);
    if(x>0)v=Mathf.Min(v,dist[z-1,x-1]+1.414f);
    if(x<n-1)v=Mathf.Min(v,dist[z-1,x+1]+1.414f);}
   dist[z,x]=v;
  }
  for(int z=n-1;z>=0;z--)for(int x=n-1;x>=0;x--)if(land[z,x]){
   float v=dist[z,x];
   if(x<n-1)v=Mathf.Min(v,dist[z,x+1]+1);
   if(z<n-1){v=Mathf.Min(v,dist[z+1,x]+1);
    if(x>0)v=Mathf.Min(v,dist[z+1,x-1]+1.414f);
    if(x<n-1)v=Mathf.Min(v,dist[z+1,x+1]+1.414f);}
   dist[z,x]=v;
  }return dist;
 }
 static TerrainLayer Layer(string key,string diffuse,string normal,float tiling){
  string path=Dir+"Trace_"+key+".terrainlayer";
  var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
  if(!l){l=new TerrainLayer();l.name="Trace_"+key;AssetDatabase.CreateAsset(l,path);}
  l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse);
  l.normalMapTexture=string.IsNullOrEmpty(normal)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
  if(!l.diffuseTexture)throw new Exception("Missing layer diffuse "+key+" "+diffuse);
  l.tileSize=Vector2.one*tiling;
  l.metallic=0;l.smoothness=.06f;EditorUtility.SetDirty(l);return l;
 }
 static TerrainLayer[] Layers()=>new[]{
  Layer("Grass","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_nor_gl_1k.jpg",11),
  Layer("Sand","Assets/EnvironmentAssets/Biomes/sand.png","",9),
  Layer("Dirt","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png","",10),
  Layer("Rock","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",8),
  Layer("Snow","Assets/EnvironmentAssets/Biomes/snow.png","",12),
  Layer("Ash","Assets/EnvironmentAssets/Biomes/ash_ground.png","",9)
 };
 static void Sculpt(Terrain t,out int landPixels,out int lakePixels,out int isletPixels){
  var td=t.terrainData;int n=td.heightmapResolution;bool[,] land=new bool[n,n],lakeMask=new bool[n,n],islMask=new bool[n,n];
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   float wx=t.transform.position.x+x*td.size.x/(n-1f),wz=t.transform.position.z+z*td.size.z/(n-1f);
   var pic=Pixel(wx,wz);bool isl=Inside(pic,islet),lk=Inside(pic,lake);
   land[z,x]=(Inside(pic,coast)||isl)&&!lk;lakeMask[z,x]=lk;islMask[z,x]=isl;
  }
  var dist=Distance(land,n);var hs=new float[n,n];landPixels=lakePixels=isletPixels=0;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   float wx=t.transform.position.x+x*td.size.x/(n-1f),wz=t.transform.position.z+z*td.size.z/(n-1f);
   float h;
   if(!land[z,x]){
    var p=Pixel(wx,wz);
    if(lakeMask[z,x])h=-2.2f;
    else {float offshore=Mathf.Min(EdgeDist(p,coast),EdgeDist(p,islet))*.37f;
     h=Water-.45f-5.65f*Step(0f,19f,offshore);}
   } else {
    var p=Pixel(wx,wz);
    h=Shape(wx,wz,p,dist[z,x],islMask[z,x]);
    if(!islMask[z,x]&&EdgeDist(p,lake)<41f){
     float lakeDist=EdgeDist(p,lake)*.37f;
     float smooth=1f-Step(1f,16f,lakeDist);
     h=Mathf.Lerp(h,Water+.55f+.28f*lakeDist,smooth*.92f);
    }
    landPixels++;if(islMask[z,x])isletPixels++;
   }
   if(lakeMask[z,x])lakePixels++;
   hs[z,x]=Mathf.Clamp01((h-t.transform.position.y)/td.size.y);
  }
  td.SetHeights(0,0,hs);t.Flush();EditorUtility.SetDirty(td);
 }
 static float Slope(Terrain t,float wx,float wz){
  var td=t.terrainData;var p=t.transform.position;var s=td.size;
  return td.GetSteepness(Mathf.Clamp01((wx-p.x)/s.x),Mathf.Clamp01((wz-p.z)/s.z));
 }
 static void Paint(Terrain t,out double[] mix){
  var td=t.terrainData;var ly=Layers();td.terrainLayers=ly;
  int w=td.alphamapWidth,h=td.alphamapHeight;var alpha=new float[h,w,ly.Length];
  mix=new double[ly.Length];int np=0;
  for(int z=0;z<h;z++)for(int x=0;x<w;x++){
   float wx=t.transform.position.x+(x+.5f)/w*td.size.x,wz=t.transform.position.z+(z+.5f)/h*td.size.z;
   var p=Pixel(wx,wz);bool land=Inside(p,coast)||Inside(p,islet),lk=Inside(p,lake);
   float elevation=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;
   float slope=Slope(t,wx,wz),exposed=Step(16,38,slope);
   float coastGap=land?Mathf.Min(EdgeDist(p,coast),EdgeDist(p,islet))*.35f:0;
   float waterEdge=Mathf.Min(coastGap,EdgeDist(p,lake)*.35f);
   float forest=Habitat(p),south=1f-Step(-40,12,wz),snow=Step(111,158,wz);
   float arid=Step(-23,24,wz)*(1f-Step(117,160,wz));
   float cr=Inside(p,crater)?Step(0,9,EdgeDist(p,crater)):.0f;
   float beach=(1f-Step(.6f,4.5f,waterEdge))*(1f-exposed);
   float green=forest*(.55f+.18f*south)*(1f-exposed*.85f)*(1f-snow);
   float dirt=(.12f+.56f*arid+.14f*forest)*(1f-exposed*.7f)*(1f-snow*.8f);
   float rock=.17f+.82f*exposed+.27f*arid+.26f*Step(20,52,elevation);
   float ice=snow*(.83f+.20f*(1f-exposed));
   float ash=cr*.92f+arid*.08f*(1f-snow);
   if(Inside(p,islet)){green=0;rock=2f;dirt=.23f;beach=.08f;ice=0;ash=0;}
   if(!land||lk){beach=.02f;green=0;dirt=0;rock=1;ice=0;ash=0;}
   if(land&&waterEdge<2.2f&&slope<13f&&wz<110)beach+=.53f;
   float[] wt={green,beach,dirt,rock,ice,ash};float sum=wt.Sum()+.0001f;
   for(int k=0;k<wt.Length;k++){alpha[z,x,k]=Mathf.Max(0,wt[k])/sum;if(land&&!lk)mix[k]+=alpha[z,x,k];}
   if(land&&!lk)np++;
  }
  for(int k=0;k<mix.Length;k++)mix[k]=mix[k]*100/Math.Max(1,np);
  td.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(td);
 }
 static Material RepairMaterial(Material old,string tag){
  if(!old)throw new Exception("NULL material "+tag);
  var tex=old.HasProperty("_MainTex")?old.GetTexture("_MainTex"):null;
  if(!tex&&old.HasProperty("_BaseColorMap"))tex=old.GetTexture("_BaseColorMap");
  if(!tex){
   string fallback=null;
   if(tag=="SmallCliff_A")fallback="Assets/Art/Environment/Cliffs/SmallCliff_01/Materials/SmallCliff_01_Albedo.tif";
   else if(tag=="RockSlussen_01")fallback="Assets/Art/Environment/Cliffs/RockSlussen_01/Materials/RockSlussen_01_Albedo.tif";
   else if(tag=="FlatRock_01")fallback="Assets/Art/Environment/Cliffs/FlatRock_01/Materials/FlatRock_01_Albedo.tif";
   else if(tag=="Rock_06")fallback="Assets/Art/Environment/Cliffs/Rock_06/Materials/Rock_06_Albedo.tif";
   else if(tag.StartsWith("TracePine")||tag.StartsWith("TraceYoungPine")){
    var low=old.name.ToLowerInvariant();
    if(low.Contains("stump"))fallback="Assets/Art/Environment/Vegetation/Trees/Generic_Stump/Stump_03/Materials/stump_Albedo.tif";
    else if(low.Contains("trunk"))fallback="Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Materials/trunk_Albedo.tif";
    else if(low.Contains("branch"))fallback="Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Materials/Branches_albedo.srgb.dds.asset";
    else if(low.Contains("billboard"))fallback="Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Materials/billboards_albedo.srgb.dds.asset";
   }
   if(fallback!=null)tex=AssetDatabase.LoadAssetAtPath<Texture2D>(fallback);
   if(!tex)throw new Exception("No real diffuse for "+old.name+" "+tag);
  }
  string filename=new string((tag+"_"+old.name).Select(ch=>char.IsLetterOrDigit(ch)||ch=='_'?ch:'_').ToArray());
  string path=Dir+"Mat_"+filename+".mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
  m=new Material(Shader.Find("Standard")){name="Dragon_"+filename};
  m.mainTexture=tex;m.color=Color.white;m.SetFloat("_Glossiness",.08f);m.SetFloat("_Metallic",0);
  var normal=old.HasProperty("_BumpMap")?old.GetTexture("_BumpMap"):null;
  if(normal){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");}
  string n=old.name.ToLowerInvariant();
  if(n.Contains("branch")||n.Contains("twig")||n.Contains("leaf")||n.Contains("billboard")){
   m.SetFloat("_Mode",1);m.SetOverrideTag("RenderType","TransparentCutout");
   m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
   m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.Zero);m.SetInt("_ZWrite",1);
   m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.37f);m.SetInt("_Cull",0);
   m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.AlphaTest;
  }
  AssetDatabase.CreateAsset(m,path);return m;
 }
 static GameObject TreePrefab(string source,string name){
  string path=Dir+name+".prefab";
  var exists=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(exists)return exists;
  var original=AssetDatabase.LoadAssetAtPath<GameObject>(source);
  if(!original)throw new Exception("No source tree "+source);
  var go=(GameObject)PrefabUtility.InstantiatePrefab(original);
  foreach(var r in go.GetComponentsInChildren<Renderer>(true))
    r.sharedMaterials=r.sharedMaterials.Select(m=>RepairMaterial(m,name)).ToArray();
  var asset=PrefabUtility.SaveAsPrefabAsset(go,path);
  UnityEngine.Object.DestroyImmediate(go);if(!asset)throw new Exception("Tree prefab save failed "+name);return asset;
 }
 static void Plant(Terrain t,out int count){
  var td=t.terrainData;
  var pine=TreePrefab("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_M.prefab","TracePine");
  var smaller=TreePrefab("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_S2.prefab","TraceYoungPine");
  td.treePrototypes=new[]{new TreePrototype{prefab=pine,bendFactor=.07f},new TreePrototype{prefab=smaller,bendFactor=.09f}};
  var trees=new List<TreeInstance>();var rng=new System.Random(600112);
  for(float z=-237;z<221;z+=4.3f)for(float x=-137;x<75;x+=4.3f){
   float xx=x+(float)(rng.NextDouble()-.5)*3f,zz=z+(float)(rng.NextDouble()-.5)*3f;
   var p=Pixel(xx,zz);if(!Inside(p,coast)||Inside(p,lake))continue;
   float forest=Habitat(p);if(forest<.20f||zz>100||Slope(t,xx,zz)>34f)continue;
   if(EdgeDist(p,coast)<13||EdgeDist(p,lake)<19)continue;
   if(Vector2.Distance(new Vector2(xx,zz),Map(new Vector2(454,760)))<17||
      Vector2.Distance(new Vector2(xx,zz),Map(new Vector2(765,378)))<19)continue;
   float prob=forest*.58f;if(rng.NextDouble()>prob)continue;
   int type=rng.NextDouble()<.24?1:0;
   var ti=new TreeInstance();ti.prototypeIndex=type;
   float nx=(xx-t.transform.position.x)/td.size.x,nz=(zz-t.transform.position.z)/td.size.z;
   ti.position=new Vector3(nx,td.GetInterpolatedHeight(nx,nz)/td.size.y,nz);
   ti.widthScale=Rand(.74f,1.24f);ti.heightScale=type==0?Rand(.60f,1.06f):Rand(.76f,1.28f);
   ti.rotation=Rand(0,Mathf.PI*2);ti.color=Color.white;ti.lightmapColor=Color.white;trees.Add(ti);
   if(trees.Count>=1100)break;
  }
  td.treeInstances=trees.ToArray();t.drawTreesAndFoliage=true;t.treeDistance=440;t.treeBillboardDistance=115;
  t.treeMaximumFullLODCount=50;EditorUtility.SetDirty(td);count=trees.Count;
 }
 static Bounds BoundsOf(GameObject go){
  var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
  if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
  var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
 }
 static float WorldY(Terrain t,float x,float z)=>t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
 static Material[] MaterialsFor(GameObject go,string tag){
  return go.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m)
   .Distinct().Select(m=>RepairMaterial(m,tag)).ToArray();
 }
 static void Texturize(GameObject go,string tag){
  foreach(var r in go.GetComponentsInChildren<Renderer>(true))
   r.sharedMaterials=r.sharedMaterials.Select(m=>RepairMaterial(m,tag)).ToArray();
 }
 static GameObject Rock(Terrain t,Transform group,string src,float x,float z,float span,float sink,float yaw){
  var original=AssetDatabase.LoadAssetAtPath<GameObject>(src);if(!original)throw new Exception("No rock prefab "+src);
  var go=(GameObject)PrefabUtility.InstantiatePrefab(original);
  go.transform.SetParent(group,false);go.transform.position=new Vector3(x,0,z);
  go.transform.rotation=Quaternion.Euler(0,yaw,0);
  Texturize(go,System.IO.Path.GetFileNameWithoutExtension(src));
  var b=BoundsOf(go);float big=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
  if(big<.001f)throw new Exception("Empty rock "+src);
  go.transform.localScale*=span/big;b=BoundsOf(go);
  go.transform.position+=Vector3.up*(WorldY(t,x,z)-b.min.y-sink);
  return go;
 }
 static readonly string[] Rocks={
  "Assets/Art/Environment/Cliffs/SmallCliff_01/SmallCliff_A.prefab",
  "Assets/Art/Environment/Cliffs/RockSlussen_01/RockSlussen_01.prefab",
  "Assets/Art/Environment/Cliffs/FlatRock_01/FlatRock_01.prefab",
  "Assets/Art/Environment/Cliffs/Rock_06/Rock_06.prefab"
 };
 static int Cluster(Terrain t,Transform root,string label,float cx,float cz,float rx,float rz,int n,float span){
  var gr=new GameObject(label);gr.transform.SetParent(root,false);int added=0;
  for(int i=0;i<n;i++){float theta=Rand(0,Mathf.PI*2),r=Mathf.Sqrt(Rand(.05f,1f));
   float x=cx+Mathf.Cos(theta)*r*rx,z=cz+Mathf.Sin(theta)*r*rz;
   if(!Inside(Pixel(x,z),coast)||Inside(Pixel(x,z),lake))continue;
   if(Mathf.Abs(x+14f)<14f&&Mathf.Abs(z+110f)<15f)continue;
   int k=i%Rocks.Length;var go=Rock(t,gr.transform,Rocks[k],x,z,i==0?span:span*Rand(.28f,.72f),Rand(.4f,1.1f),Rand(0,360));
   go.name="Embedded_"+label+"_"+i;added++;
  }return added;
 }
 static int GeologicalProps(Terrain t,Transform root){
  var group=new GameObject("Geological formations - traced from reference");
  group.transform.SetParent(root,false);int count=0;
  count+=Cluster(t,group.transform,"SW coastal mountain",-74,-197,18,23,9,19);
  count+=Cluster(t,group.transform,"SE coastal mountains",32,-164,17,20,8,17);
  count+=Cluster(t,group.transform,"Western mesa",-102,1,10,16,5,14);
  count+=Cluster(t,group.transform,"Northern mesa",-74,44,10,14,6,15);
  count+=Cluster(t,group.transform,"Eastern mesa",-6,62,10,13,6,14);
  count+=Cluster(t,group.transform,"South islet",11,-222,8,7,5,11);
  var cr=new GameObject("Crater rim exposed rock");cr.transform.SetParent(group.transform,false);
  for(int j=0;j<12;j++){float a=j*Mathf.PI*2/12f+Rand(-.10f,.10f);
   float x=-49+Mathf.Cos(a)*21.5f,z=30+Mathf.Sin(a)*17.6f;
   if(!Inside(Pixel(x,z),coast))continue;
   var obj=Rock(t,cr.transform,Rocks[j%4],x,z,Rand(3.5f,7.5f),Rand(.55f,1.2f),a*Mathf.Rad2Deg);
   obj.name="Crater_Rim_"+j;count++;
  }
  return count;
 }
 static int Shrubs(Terrain t,Transform root){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
  var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RealisticWorld/Shrub02.mat");
  if(!source||!m||!m.mainTexture)throw new Exception("No textured shrub source");
  var parent=new GameObject("Understory patches - sheltered ground");parent.transform.SetParent(root,false);
  int n=0;for(float z=-220;z<88;z+=6.2f)for(float x=-130;x<65;x+=6.2f){
   if(n>=110)break;
   float wx=x+Rand(-2,2),wz=z+Rand(-2,2);var p=Pixel(wx,wz);
   if(!Inside(p,coast)||Inside(p,lake)||Slope(t,wx,wz)>25)continue;
   float h=Habitat(p);if(h<.17f||EdgeDist(p,coast)<12||EdgeDist(p,lake)<14)continue;
   if(Rand(0,1)>h*.17f)continue;
   if(Vector2.Distance(new Vector2(wx,wz),Map(new Vector2(454,760)))<17)continue;
   var go=(GameObject)PrefabUtility.InstantiatePrefab(source);
   go.transform.SetParent(parent.transform,false);go.name="Sheltered_Shrub_"+n;
   go.transform.rotation=Quaternion.Euler(0,Rand(0,360),0);go.transform.position=new Vector3(wx,0,wz);
   foreach(var r in go.GetComponentsInChildren<Renderer>(true))
    r.sharedMaterials=Enumerable.Repeat(m,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
   var b=BoundsOf(go);float big=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
   go.transform.localScale*=Rand(.85f,1.85f)/big;b=BoundsOf(go);
   go.transform.position+=Vector3.up*(WorldY(t,wx,wz)-b.min.y-.04f);n++;
  }return n;
 }
 static void Landmarks(Transform root,Terrain t){
  var group=root.Find("Landmarks - Reference");
  if(!group)throw new Exception("Landmarks missing");
  foreach(Transform go in group){
   if(go.name.StartsWith("Central Crater Lake"))go.position=new Vector3(-47,Water,-19);
   else if(go.name.StartsWith("Northern Mountain Spine"))go.position=new Vector3(0,WorldY(t,0,150),150);
   else if(go.name.StartsWith("Vampire Ruins")){
    var b=BoundsOf(go.gameObject);var target=Map(new Vector2(454,760));
    go.position+=new Vector3(target.x-b.center.x,WorldY(t,target.x,target.y)-b.min.y,target.y-b.center.z);
   }else if(go.name.StartsWith("Dragon Lair")){
    var b=BoundsOf(go.gameObject);var target=Map(new Vector2(765,378));
    go.position+=new Vector3(target.x-b.center.x,WorldY(t,target.x,target.y)-b.min.y,target.y-b.center.z);
   }
  }
 }
 static void ClearOld(Transform root){
  foreach(string key in new[]{"Forest - Natural Sparse","Dragon Isle - Realism Additions",
  "Dragon Isle - Full Realism","Dragon Isle - Geology Preview",
  "Dragon Isle - Reference Trace"}){
   var old=root.Find(key);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
  }
 }
 [MenuItem("MMUnity/Dragon Isle/Build From Image Trace PREVIEW")]
 public static void Preview(){
  Load();AssetDatabase.Refresh();
  var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
  var t=root?root.GetComponentInChildren<Terrain>(true):null;
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
  if(!t||!td)throw new Exception("Missing preview terrain or isolated terrain asset");
  t.terrainData=td;var coll=t.GetComponent<TerrainCollider>();if(coll)coll.terrainData=td;
  ClearOld(root.transform);
  Sculpt(t,out int land,out int lakeN,out int isletN);
  Paint(t,out double[] mix);
  Landmarks(root.transform,t);
  var group=new GameObject("Dragon Isle - Reference Trace");group.transform.SetParent(root.transform,false);
  int rocks=GeologicalProps(t,group.transform),shrubs=Shrubs(t,group.transform);
  Plant(t,out int trees);
  t.drawInstanced=true;
  EditorSceneManager.MarkSceneDirty(sc);
  if(!EditorSceneManager.SaveScene(sc,ScenePath))throw new IOException("Failed saving reference preview");
  AssetDatabase.SaveAssets();Directory.CreateDirectory("Validation/CactusDragonAudit");
  var lines=new List<string>{
   "version=source_image_traced","scene="+ScenePath,"terrain="+AssetDatabase.GetAssetPath(td),
   "original_mainland_contour_vertices="+coast.Length,
   "original_islet_contour_vertices="+islet.Length,
   "lake_vertices="+lake.Length,"crater_vertices="+crater.Length,
   "land_pixels="+land,"central_lake_pixels="+lakeN,"south_islet_pixels="+isletN,
   "native_terrain_trees="+trees,"rock_objects="+rocks,"ground_shrubs="+shrubs};
  for(int i=0;i<mix.Length;i++)lines.Add(td.terrainLayers[i].name+"="+mix[i].ToString("F2")+"%");
  File.WriteAllLines(Result,lines);Debug.Log("DRAGON_REFERENCE_BUILD "+string.Join(";",lines));
 }
}
