using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMNorthReferenceRebuild20261006
{
 const float Water=.12f;
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string ReportDir="Validation/EdgeGrid20260923/NorthExtensionSafe20261006";
 const string WaterObject="Linked connected sea and existing lowland waters 20260929";
 const string InlandWaterObject="North Extension Inland Icy Water 20261006";
 const string InlandWaterAsset="Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset";
 const string ForestRoot="North Extension Reference Forests 20261006";

 static readonly string[] ExtPaths={
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"};

 static readonly string[] BaselinePaths={
  "Assets/World/WorldExtensions/Generated/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/SilverCove_NorthTerrain.asset"};

 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static float G(float x,float z,float cx,float cz,float rx,float rz){float dx=(x-cx)/rx,dz=(z-cz)/rz;return Mathf.Exp(-1.75f*(dx*dx+dz*dz));}
 static float P(float x,float z,float cx,float cz,float rx,float rz,float h,float phase){
  float g=G(x,z,cx,cz,rx,rz);
  float n0=(Mathf.PerlinNoise((x+1800f+phase*83f)*.022f,(z+800f-phase*47f)*.022f)-.5f)*.30f;
  float n1=(Mathf.PerlinNoise((x-700f+phase*131f)*.051f,(z+1200f+phase*61f)*.051f)-.5f)*.16f;
  float ridge=1f-Mathf.Abs(Mathf.PerlinNoise((x+phase*97f)*.034f,(z-phase*53f)*.034f)*2f-1f);
  return h*Mathf.Pow(g,.82f)*(1f+n0+n1+.14f*ridge*g);
 }
 static float NorthLimit(float x){
  float coast=1205f+34f*Mathf.Sin((x+430f)*.0057f)+18f*Mathf.Sin((x-150f)*.014f)+9f*Mathf.Sin((x+70f)*.031f);
  coast-=92f*G(x,0,-1170,0,118,1);
  coast-=104f*G(x,0,650,0,96,1);
  coast-=38f*G(x,0,-690,0,92,1);
  coast-=30f*G(x,0,330,0,78,1);
  return Mathf.Clamp(coast,1068f,1250f);
 }
 static float MountainArc(float x,float z){
  float m=0f;
  m=Mathf.Max(m,P(x,z,-1120,925,92,88,38,.2f));m=Mathf.Max(m,P(x,z,-1010,975,98,92,48,.5f));m=Mathf.Max(m,P(x,z,-890,1025,102,94,60,.8f));
  m=Mathf.Max(m,P(x,z,-760,1065,104,94,68,1.1f));m=Mathf.Max(m,P(x,z,-620,1095,108,92,74,1.4f));m=Mathf.Max(m,P(x,z,-470,1115,110,90,80,1.7f));
  m=Mathf.Max(m,P(x,z,-300,1120,110,88,84,2.0f));m=Mathf.Max(m,P(x,z,-125,1110,106,90,80,2.3f));m=Mathf.Max(m,P(x,z,45,1090,102,92,72,2.6f));
  m=Mathf.Max(m,P(x,z,205,1060,100,94,66,2.9f));m=Mathf.Max(m,P(x,z,350,1020,96,94,58,3.2f));m=Mathf.Max(m,P(x,z,485,975,92,90,48,3.5f));m=Mathf.Max(m,P(x,z,600,925,86,84,36,3.8f));
  m*=1f-.48f*G(x,z,-940,1005,66,56);m*=1f-.44f*G(x,z,-690,1080,66,54);m*=1f-.42f*G(x,z,-380,1110,68,52);
  m*=1f-.46f*G(x,z,110,1080,66,56);m*=1f-.48f*G(x,z,420,995,64,56);
  return m*S(865f,955f,z);
 }
 static float ChannelCenter(float z,float z0,float z1,float x0,float x1,float wiggle,float phase){
  float t=Mathf.Clamp01((z-z0)/Mathf.Max(.001f,z1-z0));
  return Mathf.Lerp(x0,x1,t)+Mathf.Sin(t*6.283f+phase)*wiggle+Mathf.Sin(t*15.1f+phase*.5f)*wiggle*.28f;
 }
 static float RiverCarve(float x,float z,float y,float z0,float z1,float x0,float x1,float width,float wiggle,float phase,float y0,float y1){
  if(z<z0||z>z1)return y;
  float t=(z-z0)/(z1-z0),cx=ChannelCenter(z,z0,z1,x0,x1,wiggle,phase);
  float dist=Mathf.Abs(x-cx)/width;
  if(dist>=3.8f)return y;
  float surface=Mathf.Lerp(y0,y1,t);
  float bank=1f-S(1.15f,3.8f,dist),core=1f-S(.48f,1.15f,dist);
  float q=Mathf.Lerp(y,Mathf.Min(y,surface+2.0f),bank*.72f);
  q=Mathf.Lerp(q,surface-.65f,core);
  return q;
 }
 static float Hydro(float x,float z,float y){
  const float lakeSurface=21.0f;
  float lake=Mathf.Max(G(x,z,-20,1015,150,68),Mathf.Max(.78f*G(x,z,-88,1028,92,54),.72f*G(x,z,52,995,84,50)));
  float bank=S(.08f,.46f,lake),core=S(.46f,.78f,lake);
  float q=Mathf.Lerp(y,Mathf.Min(y,lakeSurface+2.8f),bank*.84f);
  q=Mathf.Lerp(q,lakeSurface-.80f,core);
  q=RiverCarve(x,z,q,1010,1175,-10,115,11.0f,23,.5f,lakeSurface,26.0f);
  q=RiverCarve(x,z,q,1018,1160,-60,-310,9.5f,18,1.4f,lakeSurface,25.0f);
  q=RiverCarve(x,z,q,1020,1148,50,265,9.0f,16,2.2f,lakeSurface,24.5f);
  return q;
 }
 static float Formula(float x,float z,float edgeY,float edgeSmooth,float edgeSlope){
  float n0=(Mathf.PerlinNoise((x+1700f)*.007f,(z+500f)*.007f)-.5f)*5.0f;
  float n1=(Mathf.PerlinNoise((x+300f)*.020f,(z+1500f)*.020f)-.5f)*1.8f;
  float dz=Mathf.Max(0f,z-768f);
  float rawContinuation=edgeY+edgeSlope*Mathf.Min(dz,92f);
  float smoothContinuation=edgeSmooth+edgeSlope*Mathf.Min(dz,92f);
  float inherited=Mathf.Lerp(rawContinuation,smoothContinuation,S(768f,838f,z));
  float plateauTarget=24f+.72f*(edgeSmooth-24f)+n0*.62f+n1*.48f;
  plateauTarget+=5.0f*G(x,z,-1080,900,195,130)+4.0f*G(x,z,570,900,185,125)+3.0f*G(x,z,-720,930,230,130);
  float transitionShift=(Mathf.PerlinNoise((x+1450f)*.0065f,.23f)-.5f)*90f+(Mathf.PerlinNoise((x-350f)*.018f,.71f)-.5f)*28f;
  float plain=Mathf.Lerp(inherited,plateauTarget,S(900f+transitionShift,1160f+transitionShift,z));
  float y=plain+MountainArc(x,z);
  float center=G(x,z,-40,930,300,170);
  y=Mathf.Lerp(y,Mathf.Min(y,34f+n0*.45f),.20f*center);

  float coast=NorthLimit(x);
  float dist=coast-z;
  float lowShore=Mathf.Max(G(x,0,-845,0,90,1),G(x,0,430,0,78,1));
  float edgeNoise=(Mathf.PerlinNoise((x+900f)*.018f,.63f)-.5f)*3.0f;
  float ledgeY=5.0f+edgeNoise;
  float lower=S(-8f,3.5f+8f*lowShore,dist);
  float upper=S(5.5f,16f+13f*lowShore,dist);
  float coastal=Mathf.Lerp(-9.0f,ledgeY,lower);
  coastal=Mathf.Lerp(coastal,y,upper);
  y=coastal;

  return Hydro(x,z,y);
 }
 static float SmoothLegacyEdge(Terrain[] legacySouth,float wx){
  float sum=0f,ws=0f;
  for(int dx=-48;dx<=48;dx+=12){
   float sx=wx+dx;
   var t=Find(legacySouth,sx,700f);
   if(!t)continue;
   float w=1f-Mathf.Abs(dx)/60f;
   sum+=Ground(t,sx,768f)*w;ws+=w;
  }
  return ws>0f?sum/ws:0f;
 }
 static float SmoothLegacySlope(Terrain[] legacySouth,float wx){
  float sum=0f,ws=0f;
  for(int dx=-48;dx<=48;dx+=12){
   float sx=wx+dx;
   var t=Find(legacySouth,sx,700f);
   if(!t)continue;
   float y0=Ground(t,sx,704f),y1=Ground(t,sx,768f);
   float slope=Mathf.Clamp((y1-y0)/64f,-.16f,.16f);
   float w=1f-Mathf.Abs(dx)/60f;
   sum+=slope*w;ws+=w;
  }
  return ws>0f?sum/ws:0f;
 }
 static int Layer(TerrainData d,string token){
  for(int i=0;i<d.terrainLayers.Length;i++){var l=d.terrainLayers[i];if(l&&l.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0)return i;}
  return -1;
 }
 static Terrain Find(Terrain[] ts,float x,float z){
  return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);
 }
 static float Ground(Terrain t,float x,float z){return t.transform.position.y+t.SampleHeight(new Vector3(x,0,z));}

 static int AddZone(Transform parent,Transform[] templates,Terrain[] ext,float cx,float cz,float rx,float rz,int count,float phase){
  const float golden=2.39996323f;int added=0;
  for(int i=0;i<count;i++){
   float q=(i+.5f)/count,rad=Mathf.Sqrt(q),ang=i*golden+phase;
   float wx=cx+Mathf.Cos(ang)*rx*rad,wz=cz+Mathf.Sin(ang)*rz*rad;
   if(wz<820f)continue;
   var terr=Find(ext,wx,wz);if(!terr)continue;
   float u=Mathf.Clamp01((wx-terr.transform.position.x)/terr.terrainData.size.x),v=Mathf.Clamp01((wz-terr.transform.position.z)/terr.terrainData.size.z);
   float slope=terr.terrainData.GetSteepness(u,v),gy=Ground(terr,wx,wz);
   if(gy<1.6f||slope>30f)continue;
   var src=templates[i%templates.Length];
   var go=UnityEngine.Object.Instantiate(src.gameObject,parent);
   go.name="NorthRefForestCluster_"+added+"_"+Mathf.RoundToInt(wx)+"_"+Mathf.RoundToInt(wz);
   go.transform.position=new Vector3(wx,gy,wz);
   go.transform.rotation=Quaternion.Euler(0,(i*137.50776f+phase*47f)%360f,0);
   float sc=1.05f+.30f*Mathf.PerlinNoise(i*.173f+phase,phase*.417f);
   go.transform.localScale=src.localScale*sc;
   var rs=go.GetComponentsInChildren<Renderer>(false);
   if(rs.Length>0){Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);go.transform.position+=Vector3.up*(gy-b.min.y);}
   added++;
  }
  return added;
 }

 static void AddLake(List<Vector3> verts,List<int> tris,List<Vector2> uvs){
  const int seg=72;int center=verts.Count;verts.Add(new Vector3(-20f,21.05f,1015f));uvs.Add(new Vector2(.5f,.5f));
  int ring=verts.Count;
  for(int i=0;i<seg;i++){
   float a=i*Mathf.PI*2f/seg;
   float mod=1f+.11f*Mathf.Sin(a*3f+.4f)+.07f*Mathf.Sin(a*7f-1.1f)+.04f*Mathf.Sin(a*11f+.8f);
   float rx=104f*mod,rz=46f*(1f+.07f*Mathf.Sin(a*5f+.7f));
   verts.Add(new Vector3(-20f+Mathf.Cos(a)*rx,21.05f,1015f+Mathf.Sin(a)*rz));
   uvs.Add(new Vector2(.5f+Mathf.Cos(a)*.5f,.5f+Mathf.Sin(a)*.5f));
  }
  for(int i=0;i<seg;i++){int n=(i+1)%seg;tris.Add(center);tris.Add(ring+i);tris.Add(ring+n);}
 }
 static void AddRiver(List<Vector3> verts,List<int> tris,List<Vector2> uvs,float z0,float z1,float x0,float x1,float halfWidth,float wiggle,float phase,float y0,float y1){
  int samples=Mathf.Max(8,Mathf.CeilToInt((z1-z0)/6f)+1),start=verts.Count;
  for(int i=0;i<samples;i++){
   float t=i/(float)(samples-1),z=Mathf.Lerp(z0,z1,t),x=ChannelCenter(z,z0,z1,x0,x1,wiggle,phase),y=Mathf.Lerp(y0,y1,t)+.05f;
   float wobble=.82f+.18f*Mathf.Sin(t*9.7f+phase);
   float w=halfWidth*wobble;
   verts.Add(new Vector3(x-w,y,z));verts.Add(new Vector3(x+w,y,z));
   uvs.Add(new Vector2(0,t*3f));uvs.Add(new Vector2(1,t*3f));
  }
  for(int i=0;i<samples-1;i++){int a=start+i*2,b=a+1,c=a+2,e=a+3;tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(b);tris.Add(c);tris.Add(e);}
 }
 static Mesh BuildInlandWaterMesh(){
  var verts=new List<Vector3>();var tris=new List<int>();var uvs=new List<Vector2>();
  AddLake(verts,tris,uvs);
  AddRiver(verts,tris,uvs,1010,1175,-10,115,9.0f,23,.5f,21.0f,26.0f);
  AddRiver(verts,tris,uvs,1018,1160,-60,-310,8.0f,18,1.4f,21.0f,25.0f);
  AddRiver(verts,tris,uvs,1020,1148,50,265,7.5f,16,2.2f,21.0f,24.5f);
  var m=new Mesh();m.name="NorthInlandIcyWater20261006";m.indexFormat=IndexFormat.UInt32;
  m.SetVertices(verts);m.SetTriangles(tris,0);m.SetUVs(0,uvs);m.RecalculateNormals();m.RecalculateBounds();return m;
 }

 [MenuItem("MMUnity/Reference 2026/Rebuild North Extension From User Reference")]
 public static void Run(){
  var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Enroth must open clean.");
  Directory.CreateDirectory(ReportDir);
  string reportPath=ReportDir+"/apply.txt";
  if(File.Exists(reportPath))throw new Exception("Safe north extension rebuild already applied; refusing cumulative rerun.");

  var active=Terrain.activeTerrains;
  var ext=new Terrain[4];var data=new TerrainData[4];var baseData=new TerrainData[4];
  var legacySouth=new Terrain[4];
  for(int i=0;i<4;i++){
   data[i]=AssetDatabase.LoadAssetAtPath<TerrainData>(ExtPaths[i]);
   baseData[i]=AssetDatabase.LoadAssetAtPath<TerrainData>(BaselinePaths[i]);
   if(!data[i]||!baseData[i])throw new Exception("Missing extension/baseline TerrainData.");
   ext[i]=active.FirstOrDefault(t=>t.terrainData==data[i]);
   if(!ext[i])throw new Exception("Extension terrain not live: "+ExtPaths[i]);
   legacySouth[i]=active.FirstOrDefault(t=>Mathf.Abs(t.transform.position.x-ext[i].transform.position.x)<.1f&&Mathf.Abs(t.transform.position.z-256f)<.1f);
   if(!legacySouth[i])throw new Exception("Legacy south terrain missing for "+ext[i].name);
  }

  int changed=0,painted=0;
  for(int i=0;i<4;i++){
   var t=ext[i];var d=data[i];var south=legacySouth[i];
   var west=active.FirstOrDefault(x=>Mathf.Abs(t.transform.position.x+1280f)<.1f&&Mathf.Abs(x.transform.position.x+1792f)<.1f&&Mathf.Abs(x.transform.position.z-768f)<.1f);
   var east=active.FirstOrDefault(x=>Mathf.Abs(t.transform.position.x-256f)<.1f&&Mathf.Abs(x.transform.position.x-768f)<.1f&&Mathf.Abs(x.transform.position.z-768f)<.1f);
   var h=d.GetHeights(0,0,513,513);
   for(int x=0;x<=512;x++){
    float wx=t.transform.position.x+x;
    float edge=Ground(south,wx,768f);
    float edgeSmooth=SmoothLegacyEdge(legacySouth,wx);
    float edgeSlope=SmoothLegacySlope(legacySouth,wx);
    for(int z=0;z<=512;z++){
     float wz=t.transform.position.z+z;
     float old=t.transform.position.y+h[z,x]*d.size.y;
     float yy=Formula(wx,wz,edge,edgeSmooth,edgeSlope);
     if(west&&x<=60){float wy=Ground(west,-1280f,wz);yy=Mathf.Lerp(wy,yy,S(0,60,x));}
     if(east&&x>=452){float ey=Ground(east,768f,wz);yy=Mathf.Lerp(yy,ey,S(452,512,x));}
     if(z==0)yy=edge;
     if(Mathf.Abs(yy-old)>.01f){h[z,x]=Mathf.Clamp01((yy-t.transform.position.y)/d.size.y);changed++;}
    }
   }
   d.SetHeights(0,0,h);

   var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
   var baseA=baseData[i].GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
   var baseLayers=baseData[i].terrainLayers;
   var baseMap=new int[d.alphamapLayers];
   for(int k=0;k<baseMap.Length;k++){
    baseMap[k]=-1;var cur=d.terrainLayers[k];if(!cur)continue;
    string cp=AssetDatabase.GetAssetPath(cur);
    for(int j=0;j<baseLayers.Length;j++){var bl=baseLayers[j];if(!bl)continue;if(bl==cur||AssetDatabase.GetAssetPath(bl)==cp||bl.name==cur.name){baseMap[k]=j;break;}}
   }
   int snow=Layer(d,"Snow"),rock=Layer(d,"Volcanic");if(rock<0)rock=Layer(d,"Rock");
   if(snow<0||rock<0)throw new Exception("Snow/rock layers missing on "+t.name);
   for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
    float u=x/(float)(d.alphamapWidth-1),v=z/(float)(d.alphamapHeight-1);
    float y=t.transform.position.y+d.GetInterpolatedHeight(u,v),slope=d.GetSteepness(u,v);
    float rw=y<0?.96f:Mathf.Lerp(.07f,.85f,S(17f,43f,slope)),sw=1f-rw;
    var target=new float[d.alphamapLayers];target[snow]=sw;target[rock]=rw;
    float sum=0f,cold=S(0,.30f,v);
    for(int k=0;k<d.alphamapLayers;k++){float bw=(baseMap[k]>=0&&baseMap[k]<baseA.GetLength(2))?baseA[z,x,baseMap[k]]:0f;a[z,x,k]=Mathf.Lerp(bw,target[k],.995f*cold);sum+=a[z,x,k];}
    if(sum>.001f)for(int k=0;k<d.alphamapLayers;k++)a[z,x,k]/=sum;
    painted++;
   }
   d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
  }

  // Extend the existing coherent water only where the new extension became wet.
  var waterTr=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==WaterObject);
  if(!waterTr)throw new Exception("Connected world water object missing.");

  if(scene.GetRootGameObjects().Any(g=>g.name==InlandWaterObject))throw new Exception("North inland water object already exists.");
  if(AssetDatabase.LoadAssetAtPath<Mesh>(InlandWaterAsset))throw new Exception("North inland water asset already exists.");
  var inlandMesh=BuildInlandWaterMesh();
  AssetDatabase.CreateAsset(inlandMesh,InlandWaterAsset);
  var inlandGo=new GameObject(InlandWaterObject);
  var imf=inlandGo.AddComponent<MeshFilter>();imf.sharedMesh=inlandMesh;
  var imr=inlandGo.AddComponent<MeshRenderer>();
  var sourceWaterRenderer=waterTr.GetComponent<Renderer>();
  if(!sourceWaterRenderer||!sourceWaterRenderer.sharedMaterial)throw new Exception("Connected water material missing.");
  imr.sharedMaterial=sourceWaterRenderer.sharedMaterial;
  imr.shadowCastingMode=ShadowCastingMode.Off;imr.receiveShadows=false;

  var wf=waterTr.GetComponent<MeshFilter>();if(!wf||!wf.sharedMesh)throw new Exception("Connected water mesh missing.");
  var waterMesh=wf.sharedMesh;
  // Re-entry is guarded by the persisted apply report and forest root; keep the mesh's canonical asset name.

  const float x1=-1280f,x2=768f,z1=768f,z2=1280f,step=2f;
  int W=Mathf.CeilToInt((x2-x1)/step),H=Mathf.CeilToInt((z2-z1)/step);
  var wet=new bool[W*H];var draw=new bool[W*H];int newlyWet=0;
  for(int iz=0;iz<H;iz++)for(int ix=0;ix<W;ix++){
   float wx=x1+(ix+.5f)*step,wz=z1+(iz+.5f)*step;
   var t=Find(ext,wx,wz);if(!t)continue;int qi=Array.IndexOf(ext,t);
   float u=Mathf.Clamp01((wx-t.transform.position.x)/512f),v=Mathf.Clamp01((wz-t.transform.position.z)/512f);
   float oldY=t.transform.position.y+baseData[qi].GetInterpolatedHeight(u,v);
   float newY=Ground(t,wx,wz);
   bool nw=oldY>Water+.05f&&newY<Water-.05f;
   wet[iz*W+ix]=nw;draw[iz*W+ix]=nw;if(nw)newlyWet++;
  }
  for(int iz=0;iz<H;iz++)for(int ix=0;ix<W;ix++)if(wet[iz*W+ix])for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){
   int xx=ix+dx,zz=iz+dz;if(xx>=0&&xx<W&&zz>=0&&zz<H)draw[zz*W+xx]=true;
  }
  var ov=waterMesh.vertices;var ot=waterMesh.triangles;
  var verts=new List<Vector3>(ov.Length+30000);verts.AddRange(ov);
  var tris=new List<int>(ot.Length+50000);tris.AddRange(ot);
  var rects=new List<Vector4>();var activeRuns=new Dictionary<long,int>();
  for(int z=0;z<H;z++){
   var next=new Dictionary<long,int>();
   for(int x=0;x<W;){
    if(!draw[z*W+x]){x++;continue;}int start=x;while(x<W&&draw[z*W+x])x++;
    long key=((long)start<<32)|(uint)x;
    if(activeRuns.TryGetValue(key,out int idx)){var r=rects[idx];r.w=z+1;rects[idx]=r;next[key]=idx;}
    else{next[key]=rects.Count;rects.Add(new Vector4(start,z,x,z+1));}
   }
   activeRuns=next;
  }
  foreach(var r in rects){
   float ax=x1+r.x*step,bx=x1+r.z*step,az=z1+r.y*step,bz=z1+r.w*step;if(bx-ax<.001f||bz-az<.001f)continue;
   int n=verts.Count;
   verts.Add(waterTr.InverseTransformPoint(new Vector3(ax,Water+.015f,az)));
   verts.Add(waterTr.InverseTransformPoint(new Vector3(ax,Water+.015f,bz)));
   verts.Add(waterTr.InverseTransformPoint(new Vector3(bx,Water+.015f,bz)));
   verts.Add(waterTr.InverseTransformPoint(new Vector3(bx,Water+.015f,az)));
   tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
  }
  waterMesh.Clear();waterMesh.indexFormat=IndexFormat.UInt32;waterMesh.SetVertices(verts);waterMesh.SetTriangles(tris,0);
  waterMesh.RecalculateNormals();waterMesh.RecalculateBounds();EditorUtility.SetDirty(waterMesh);

  // Re-ground only extension vegetation. Legacy maps/props south of z=768 are never touched.
  int grounded=0,disabled=0;
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  foreach(var tr in all){
   var p=tr.position;if(p.x<-1280||p.x>768||p.z<768||p.z>1280)continue;
   bool candidate=tr.name.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase)||tr.name.StartsWith("Eco_",StringComparison.OrdinalIgnoreCase);
   if(!candidate)continue;
   var terr=Find(ext,p.x,p.z);if(!terr)continue;float gy=Ground(terr,p.x,p.z);
   if(gy<=Water+.15f){if(tr.gameObject.activeSelf){tr.gameObject.SetActive(false);disabled++;}continue;}
   var rs=tr.GetComponentsInChildren<Renderer>(false);if(rs.Length==0)continue;
   Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);
   float gap=b.min.y-gy;if(Mathf.Abs(gap)>.05f){tr.position=new Vector3(p.x,p.y-gap,p.z);grounded++;}
  }

  if(scene.GetRootGameObjects().Any(g=>g.name==ForestRoot))throw new Exception("North extension forest root already exists.");
  var templates=new[]{
   all.FirstOrDefault(t=>t.name=="SourceTree_0134_6tree29"),
   all.FirstOrDefault(t=>t.name=="SourceTree_0096_6tree28"),
   all.FirstOrDefault(t=>t.name=="SourceTree_0200_6tree28"),
   all.FirstOrDefault(t=>t.name=="SourceTree_0228_6tree28")}.Where(t=>t!=null).ToArray();
  if(templates.Length<3)throw new Exception("Production conifer cluster templates missing.");
  var forestRoot=new GameObject(ForestRoot);int forestClusters=0;
  forestClusters+=AddZone(forestRoot.transform,templates,ext,-1110,885,155,86,48,.2f);
  forestClusters+=AddZone(forestRoot.transform,templates,ext,-925,955,155,96,46,.9f);
  forestClusters+=AddZone(forestRoot.transform,templates,ext,-720,1010,145,96,38,1.6f);
  forestClusters+=AddZone(forestRoot.transform,templates,ext,-505,965,115,76,28,2.2f);
  forestClusters+=AddZone(forestRoot.transform,templates,ext,305,940,125,82,30,3.1f);
  forestClusters+=AddZone(forestRoot.transform,templates,ext,500,965,125,84,38,3.8f);
  forestClusters+=AddZone(forestRoot.transform,templates,ext,625,890,92,68,30,4.6f);

  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save Enroth.");
  AssetDatabase.SaveAssets();

  var lines=new[]{
   "PASS extension-only north rebuild",
   "legacyTerrainWrites=0",
   "extensionTerrainCount=4",
   "changedHeightVertices="+changed,
   "paintedAlphaPixels="+painted,
   "newlyWet2mCells="+newlyWet,
   "waterRectangles="+rects.Count,
   "waterVertices="+waterMesh.vertexCount,
   "extensionVegetationGrounded="+grounded,
   "extensionVegetationDisabledInWater="+disabled,
   "forestClustersAdded="+forestClusters,
   "forestZones=7",
   "legacySouthPropsTouched=false",
   "sceneDirtyAfterSave="+scene.isDirty};
  File.WriteAllLines(reportPath,lines);
  Debug.Log("NORTH_EXTENSION_SAFE_20261006_DONE "+string.Join(" | ",lines));
 }
}
