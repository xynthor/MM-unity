using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class MMArchipelagoWestEast20261001
{
 const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string SourceAsset="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/8b884b7b95eecaa4499052eb050c0e37.asset";
 const string Root="Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001";
 const string WestAsset=Root+"/ArchipelagoWest_Terrain.asset";
 const string EastAsset=Root+"/ArchipelagoEast_Terrain.asset";
 const float TerrainY=-24f,TerrainH=320f,Sea=-6.7f,Water=.10f,Scale=1.40f,ZShift=-30f;
 static Vector2[][] shore;
 static Vector3[] islets;
 static Vector2[] spreading;

 static float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/Mathf.Max(.0001f,b-a));return t*t*(3f-2f*t);}
 static float Signed(Vector2 p,Vector2[] poly){
  bool inside=false;float ds=float.MaxValue;
  for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){
   var a=poly[j];var b=poly[i];var v=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,v)/Mathf.Max(v.sqrMagnitude,.0001f));
   ds=Mathf.Min(ds,(p-(a+t*v)).sqrMagnitude);
   if(((a.y>p.y)!=(b.y>p.y))&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
  }
  return (inside?1f:-1f)*Mathf.Sqrt(ds);
 }
 static void LoadReferenceGeometry(){
  var bf=BindingFlags.Static|BindingFlags.NonPublic;
  var ty=typeof(MMWorldExtensionBuilder);
  shore=(Vector2[][])ty.GetField("CurvedAncientShore",bf).GetValue(null);
  islets=(Vector3[])ty.GetField("AncientIslets",bf).GetValue(null);
  spreading=(Vector2[])ty.GetField("AncientSpreading",bf).GetValue(null);
  if(shore==null||shore.Length!=5||islets==null||spreading==null)throw new Exception("Archipelago reference geometry unavailable");
 }
 static float Noise(float x,float z,int i){return Mathf.PerlinNoise((x+4096f+i*73f)*.027f,(z+8192f-i*41f)*.027f)-.5f;}
 static float WorldHeight(float wx,float wz,out float central,out float forest){
  float gx=wx-1280f,gz=wz+1024f;
  var p=new Vector2(gx/Scale,(gz-ZShift)/Scale);
  float raised=0;central=0;forest=0;
  for(int i=0;i<shore.Length;i++){
   float d=Signed(p-spreading[i],shore[i]);
   float erosion=3.7f*Noise(wx,wz,i)+1.8f*Noise(wx*2.7f,wz*2.7f,i+11);
   float coast=Smooth(-13f,15f,d+erosion);
   float inner=Smooth(7f,30f,d);
   float crest=3.6f+4.4f*Mathf.PerlinNoise((wx+i*89f)*.018f,(wz-i*53f)*.018f);
   if(i==3)crest*=.34f;
   float r=coast*(14.5f+crest*inner);
   raised=Mathf.Max(raised,r);
   if(i==3)central=Mathf.Max(central,Smooth(-7f,25f,d));
   else forest=Mathf.Max(forest,Smooth(-5f,26f,d));
  }
  for(int i=0;i<islets.Length;i++){
   var q=islets[i];float dx=(p.x-q.x)/q.z,dz=(p.y-q.y)/q.z;
   float a=Mathf.Atan2(dz,dx),rr=Mathf.Sqrt(dx*dx+dz*dz);
   float wav=1f+.16f*Mathf.Sin(a*3f+i*1.7f)+.09f*Mathf.Sin(a*5f-i*.6f);
   float d=(wav-rr)*q.z;
   raised=Mathf.Max(raised,Smooth(-3f,3f,d)*13.5f);
  }
  // Keep the source-like shallow seabed around the islands. Open-ocean
  // deepening is applied only at unconnected outer tile edges in BuildHeights.
  float shelf=Smooth(.8f,5.8f,raised);
  return Mathf.Lerp(Sea,Sea+raised,shelf);
 }
 static float[,] BuildHeights(bool east,TerrainData source,float sourceY,out float seamErr,out float landPct){
  int n=513;var h=new float[n,n];var old=source.GetHeights(0,0,n,n);var westEdge=new float[n];var northEdge=new float[n];
  for(int z=0;z<n;z++)westEdge[z]=sourceY+old[z,0]*source.size.y;
  for(int x=0;x<n;x++)northEdge[x]=sourceY+old[n-1,x]*source.size.y;
  float originX=east?1280f:768f,originZ=-1280f;int land=0;seamErr=0;
  float ne=northEdge[n-1];
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   float wx=originX+x,wz=originZ+z;float c,f;float y=WorldHeight(wx,wz,out c,out f);
   if(!east){
    if(x<32){float u=Smooth(0,32,x);y=Mathf.Lerp(westEdge[z],y,u);}
    if(z>480){float u=Smooth(0,32,512-z);y=Mathf.Lerp(northEdge[x],y,u);}
   }else{
    // East has no northern/eastern terrain neighbor. At the north-west corner
    // retain the exact West/North corner height, then deepen naturally outward.
    if(z>400&&y<Water+.45f){
     float target=Mathf.Lerp(ne,-18f,Smooth(0,160,x));
     float u=Smooth(0,112,512-z);y=Mathf.Lerp(target,y,u);
    }
   }
   // South is open ocean for both quadrants; east is open ocean on its east edge.
   // Only submerged seabed is deepened, so reference islands/islets remain intact.
   if(y<Water+.45f&&z<112){float u=Smooth(0,112,z);y=Mathf.Lerp(-18f,y,u);}
   if(east&&y<Water+.45f&&x>400){float u=Smooth(0,112,512-x);y=Mathf.Lerp(-18f,y,u);}
   if(y>Water+.45f)land++;
   h[z,x]=Mathf.Clamp01((y-TerrainY)/TerrainH);
  }
  if(!east){
   for(int z=0;z<n;z++){float y=TerrainY+h[z,0]*TerrainH;seamErr=Mathf.Max(seamErr,Mathf.Abs(y-westEdge[z]));}
   for(int x=0;x<n;x++){float y=TerrainY+h[n-1,x]*TerrainH;seamErr=Mathf.Max(seamErr,Mathf.Abs(y-northEdge[x]));}
  }
  landPct=land*100f/(n*n);return h;
 }
 static int ByDiffuse(TerrainData d,string needle){return Array.FindIndex(d.terrainLayers,l=>l&&l.diffuseTexture&&AssetDatabase.GetAssetPath(l.diffuseTexture).IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0);}
 static int ByName(TerrainData d,string needle){return Array.FindIndex(d.terrainLayers,l=>l&&l.name.IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0);}
 static float Slope(float[,] h,int x,int z){
  int n=h.GetLength(0);float dx=(h[z,Mathf.Min(n-1,x+1)]-h[z,Mathf.Max(0,x-1)])*TerrainH*.5f;float dz=(h[Mathf.Min(n-1,z+1),x]-h[Mathf.Max(0,z-1),x])*TerrainH*.5f;
  return Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
 }
 static float[,,] BuildAlpha(bool east,TerrainData d,float[,] hm){
  int a=512,L=d.alphamapLayers;var o=new float[a,a,L];
  int green=ByDiffuse(d,"ground_grass_fells_mossy"),light=ByDiffuse(d,"LightGreen_blend"),sand=ByDiffuse(d,"coast_sand_02"),volc=ByDiffuse(d,"ash_ground"),arid=ByDiffuse(d,"dry_soil"),snow=ByDiffuse(d,"Snow02_Seamless"),road=ByDiffuse(d,"stone_ground");
  if(green<0)green=0;if(light<0)light=1;if(sand<0)sand=2;if(volc<0)volc=3;if(arid<0)arid=Mathf.Min(5,L-1);
  float originX=east?1280f:768f,originZ=-1280f;
  for(int z=0;z<a;z++)for(int x=0;x<a;x++){
   int hx=Mathf.RoundToInt((x+.5f)*512f/a),hz=Mathf.RoundToInt((z+.5f)*512f/a);float y=TerrainY+hm[hz,hx]*TerrainH,sl=Slope(hm,hx,hz);
   float wx=originX+(x+.5f)*512f/a,wz=originZ+(z+.5f)*512f/a;float central,forest;WorldHeight(wx,wz,out central,out forest);
   float beachRaw=Smooth(-2.4f,-.35f,y)*(1-Smooth(.45f,5.2f,y))*(1-Smooth(12f,30f,sl));
   // Keep sand to the actual shallow-water/shore band. Deeper seabed uses the
   // muted dry-soil family so transparent water does not reveal pale island masks.
   float beach=beachRaw*(1f-.78f*forest);float deep=1-Smooth(-4f,-1.5f,y);float steep=Smooth(18f,39f,sl);
   float g=.52f*(1-steep)*(1-beach)*(1-.90f*central)*(1+1.20f*forest);
   float lg=.32f*(1-steep)*(1-beach)*(1-.80f*central)*(1+.72f*forest);
   float sd=.025f+.92f*beach+1.18f*central*(1-.82f*deep);
   float vo=.08f+.72f*steep;float ar=.14f*(1-steep)+.22f*central+1.05f*deep;float sn=0,st=.025f*steep;
   var w=new float[L];w[green]=Mathf.Max(0,g);if(light>=0&&light<L)w[light]=Mathf.Max(0,lg);w[sand]=Mathf.Max(0,sd);w[volc]=Mathf.Max(0,vo);if(arid>=0&&arid<L)w[arid]=Mathf.Max(0,ar);if(snow>=0&&snow<L)w[snow]=sn;if(road>=0&&road<L)w[road]=Mathf.Max(0,st);
   float sum=w.Sum()+.0001f;for(int k=0;k<L;k++)o[z,x,k]=w[k]/sum;
  }
  return o;
 }
 static bool[,] NoHoles(){
  int n=512;var holes=new bool[n,n];
  for(int z=0;z<n;z++)for(int x=0;x<n;x++)holes[z,x]=true;
  return holes;
 }
 static float Bilinear(float[,] h,float x,float z){
  int n=h.GetLength(0);x=Mathf.Clamp(x,0,n-1.001f);z=Mathf.Clamp(z,0,n-1.001f);
  int x0=Mathf.FloorToInt(x),z0=Mathf.FloorToInt(z),x1=Mathf.Min(n-1,x0+1),z1=Mathf.Min(n-1,z0+1);
  float tx=x-x0,tz=z-z0;return Mathf.Lerp(Mathf.Lerp(h[z0,x0],h[z0,x1],tx),Mathf.Lerp(h[z1,x0],h[z1,x1],tx),tz);
 }
 static void BuildScaledSource(TerrainData source,out float[,] combined,out int[,] coastDist){
  const int W=1025,H=513;combined=new float[H,W];coastDist=new int[H,W];
  var sh=source.GetHeights(0,0,513,513);var q=new Queue<Vector2Int>();const int Inf=999999;
  for(int z=0;z<H;z++)for(int x=0;x<W;x++){
   float wx=768f+x,wz=-1280f+z,sx=256f+(wx-1280f)/Scale,sz=256f+((wz+1024f)-ZShift)/Scale;
   float y=(sx>=0&&sx<=512&&sz>=0&&sz<=512)?TerrainY+Bilinear(sh,sx,sz)*TerrainH:Sea;
   combined[z,x]=y;bool land=y>Water+.45f;coastDist[z,x]=land?0:Inf;if(land)q.Enqueue(new Vector2Int(x,z));
  }
  int[] dx={1,-1,0,0},dz={0,0,1,-1};
  while(q.Count>0){var p=q.Dequeue();int nd=coastDist[p.y,p.x]+1;for(int i=0;i<4;i++){int x=p.x+dx[i],z=p.y+dz[i];if(x<0||z<0||x>=W||z>=H||coastDist[z,x]<=nd)continue;coastDist[z,x]=nd;q.Enqueue(new Vector2Int(x,z));}}
  for(int z=0;z<H;z++)for(int x=0;x<W;x++)if(combined[z,x]<=Water+.45f){float u=Smooth(10f,58f,coastDist[z,x]);combined[z,x]=Mathf.Lerp(combined[z,x],-23.5f,u);}
  for(int z=481;z<513;z++)for(int x=0;x<513;x++){float target=TerrainY+sh[512,x]*TerrainH;float u=Smooth(0,32,512-z);combined[z,x]=Mathf.Lerp(target,combined[z,x],u);}
  for(int z=0;z<513;z++)for(int x=0;x<32;x++){float target=TerrainY+sh[z,0]*TerrainH;float u=Smooth(0,32,x);combined[z,x]=Mathf.Lerp(target,combined[z,x],u);}
  for(int z=0;z<513;z++)combined[z,0]=TerrainY+sh[z,0]*TerrainH;
  for(int x=0;x<513;x++)combined[512,x]=TerrainY+sh[512,x]*TerrainH;
 }
 static float[,] SliceHeights(bool east,float[,] combined,out float landPct){
  var h=new float[513,513];int xo=east?512:0,land=0;
  for(int z=0;z<513;z++)for(int x=0;x<513;x++){float y=combined[z,x+xo];if(y>Water+.45f)land++;h[z,x]=Mathf.Clamp01((y-TerrainY)/TerrainH);}
  landPct=land*100f/(513f*513f);return h;
 }
 static float[,,] BuildScaledAlpha(bool east,TerrainData source){
  int a=512,L=source.alphamapLayers;var src=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);var o=new float[a,a,L];
  float ox=east?1280f:768f;
  for(int z=0;z<a;z++)for(int x=0;x<a;x++){
   float wx=ox+x+.5f,wz=-1280f+z+.5f,sx=256f+(wx-1280f)/Scale,sz=256f+((wz+1024f)-ZShift)/Scale;
   if(sx>=0&&sx<512&&sz>=0&&sz<512){int ix=Mathf.Clamp(Mathf.FloorToInt(sx),0,511),iz=Mathf.Clamp(Mathf.FloorToInt(sz),0,511);for(int k=0;k<L;k++)o[z,x,k]=src[iz,ix,k];}
   else o[z,x,Mathf.Min(5,L-1)]=1f;
  }
  return o;
 }
 static TerrainData EnsureData(string path,string name,TerrainData source,float[,] hm){
  var d=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
  if(!d){d=new TerrainData();d.name=name;d.heightmapResolution=513;d.size=new Vector3(512,TerrainH,512);d.alphamapResolution=512;d.baseMapResolution=512;d.terrainLayers=source.terrainLayers;AssetDatabase.CreateAsset(d,path);}
  else {d.heightmapResolution=513;d.size=new Vector3(512,TerrainH,512);d.alphamapResolution=512;d.baseMapResolution=512;d.terrainLayers=source.terrainLayers;}
  if(source.detailPrototypes!=null&&source.detailPrototypes.Length>0)d.detailPrototypes=source.detailPrototypes;
  if(source.treePrototypes!=null&&source.treePrototypes.Length>0)d.treePrototypes=source.treePrototypes;
  d.SetHeights(0,0,hm);return d;
 }
 static GameObject MakeWater(Transform parent){
  var go=GameObject.CreatePrimitive(PrimitiveType.Plane);go.name="Archipelago East Water - Smooth";go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,Water,0);go.transform.localScale=new Vector3(51.2f,1,51.2f);
  var col=go.GetComponent<Collider>();if(col)UnityEngine.Object.DestroyImmediate(col);
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/EnrothReferenceLinkedWater.mat");if(!mat)throw new Exception("Linked water material missing");
  var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.enabled=true;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;return go;
 }
 static float Ground(Terrain t,float x,float z){return t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;}
 static void PatchGlobalWater(Terrain west,Terrain east){
  const float x1=768f,x2=1792f,z1=-1280f,z2=-768f,step=2f;
  var water=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Linked connected sea and existing lowland waters 20260929");
  if(!water)throw new Exception("Global connected water object missing");
  var mf=water.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)throw new Exception("Global connected water mesh missing");
  var mesh=mf.sharedMesh;var ov=mesh.vertices;if(ov.Length%4!=0||mesh.triangles.Length!=ov.Length/4*6)throw new Exception("Unexpected global water mesh topology");
  var verts=new List<Vector3>();var tris=new List<int>();
  void Quad(float ax,float az,float bx,float bz){if(bx-ax<.001f||bz-az<.001f)return;int n=verts.Count;verts.Add(new Vector3(ax,Water,az));verts.Add(new Vector3(ax,Water,bz));verts.Add(new Vector3(bx,Water,bz));verts.Add(new Vector3(bx,Water,az));tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
  for(int i=0;i<ov.Length;i+=4){
   float ax=Mathf.Min(Mathf.Min(ov[i].x,ov[i+1].x),Mathf.Min(ov[i+2].x,ov[i+3].x)),bx=Mathf.Max(Mathf.Max(ov[i].x,ov[i+1].x),Mathf.Max(ov[i+2].x,ov[i+3].x));
   float az=Mathf.Min(Mathf.Min(ov[i].z,ov[i+1].z),Mathf.Min(ov[i+2].z,ov[i+3].z)),bz=Mathf.Max(Mathf.Max(ov[i].z,ov[i+1].z),Mathf.Max(ov[i+2].z,ov[i+3].z));
   float ix1=Mathf.Max(ax,x1),ix2=Mathf.Min(bx,x2),iz1=Mathf.Max(az,z1),iz2=Mathf.Min(bz,z2);
   if(ix1>=ix2||iz1>=iz2){Quad(ax,az,bx,bz);continue;}
   Quad(ax,az,ix1,bz);Quad(ix2,az,bx,bz);Quad(ix1,az,ix2,iz1);Quad(ix1,iz2,ix2,bz);
  }
  const int W=512,H=256;var wet=new bool[W*H];var draw=new bool[W*H];
  for(int z=0;z<H;z++)for(int x=0;x<W;x++){float wx=x1+(x+.5f)*step,wz=z1+(z+.5f)*step;var t=wx<1280f?west:east;wet[z*W+x]=Ground(t,wx,wz)<Water;draw[z*W+x]=wet[z*W+x];}
  for(int z=0;z<H;z++)for(int x=0;x<W;x++)if(wet[z*W+x])for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,zz=z+dz;if(xx>=0&&zz>=0&&xx<W&&zz<H&&!wet[zz*W+xx])draw[zz*W+xx]=true;}
  var active=new Dictionary<long,int>();var rects=new List<Vector4>();
  for(int z=0;z<H;z++){var next=new Dictionary<long,int>();for(int x=0;x<W;){if(!draw[z*W+x]){x++;continue;}int start=x;while(x<W&&draw[z*W+x])x++;long key=((long)start<<32)|(uint)x;if(active.TryGetValue(key,out int idx)){var r=rects[idx];r.w=z+1;rects[idx]=r;next[key]=idx;}else{next[key]=rects.Count;rects.Add(new Vector4(start,z,x,z+1));}}active=next;}
  foreach(var r in rects)Quad(x1+r.x*step,z1+r.y*step,x1+r.z*step,z1+r.w*step);
  mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
 }
 static Vector3 ExpandPos(Vector3 old,Vector3 oldCenter){
  float rx=(old.x-oldCenter.x+145f)/.84f;float rz=(old.z-oldCenter.z)/1.06f;
  return new Vector3(1280f+rx*Scale,old.y,-1024f+rz*Scale+ZShift);
 }
 public static void Run(){
  LoadReferenceGeometry();
  var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var west=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Archipelago of the Ancients - LINKED EXTENSION");
  if(!west)west=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Archipelago of the Ancients West - LINKED EXTENSION");
  if(!west)throw new Exception("Current Archipelago root missing");
  bool alreadySplit=west.name=="Archipelago of the Ancients West - LINKED EXTENSION";
  var world=west.parent;var oldCenter=west.position;var oldTerrain=west.GetComponentInChildren<Terrain>(true);if(!oldTerrain)throw new Exception("Current Archipelago terrain missing");
  var source=AssetDatabase.LoadAssetAtPath<TerrainData>(SourceAsset);if(!source)throw new Exception("Archipelago source TerrainData missing");
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","ArchipelagoWestEast20261001");
  BuildScaledSource(source,out var combined,out var coastDist);
  var wh=SliceHeights(false,combined,out float westLand);var eh=SliceHeights(true,combined,out float eastLand);
  var wd=EnsureData(WestAsset,"ArchipelagoOfTheAncientsWestTerrain",source,wh);var ed=EnsureData(EastAsset,"ArchipelagoOfTheAncientsEastTerrain",source,eh);
  wd.SetAlphamaps(0,0,BuildScaledAlpha(false,source));ed.SetAlphamaps(0,0,BuildScaledAlpha(true,source));
  wd.SetHoles(0,0,NoHoles());ed.SetHoles(0,0,NoHoles());
  var sourceH=source.GetHeights(0,0,513,513);float westSeam=0;
  for(int z=0;z<513;z++)westSeam=Mathf.Max(westSeam,Mathf.Abs((TerrainY+wh[z,0]*TerrainH)-(TerrainY+sourceH[z,0]*TerrainH)));
  for(int x=0;x<513;x++)westSeam=Mathf.Max(westSeam,Mathf.Abs((TerrainY+wh[512,x]*TerrainH)-(TerrainY+sourceH[512,x]*TerrainH)));
  wd.SetBaseMapDirty();ed.SetBaseMapDirty();EditorUtility.SetDirty(wd);EditorUtility.SetDirty(ed);AssetDatabase.SaveAssets();

  // Terrain-first pass. The old dressing was authored for the smaller single tile.
  // Do not stretch or duplicate it across the enlarged West/East geography.
  var oldDress=west.Find("Region Dressing - Reference Matched");
  if(oldDress)UnityEngine.Object.DestroyImmediate(oldDress.gameObject);
  var legacyWater=west.Find("Internal Water - Smooth");
  if(legacyWater)UnityEngine.Object.DestroyImmediate(legacyWater.gameObject);
  var oldExpanded=west.Find("Region Dressing - Expanded Reference");
  if(oldExpanded)UnityEngine.Object.DestroyImmediate(oldExpanded.gameObject);

  west.name="Archipelago of the Ancients West - LINKED EXTENSION";
  oldTerrain.terrainData=wd;var wc=oldTerrain.GetComponent<TerrainCollider>();if(wc)wc.terrainData=wd;oldTerrain.name="Terrain_ArchipelagoOfTheAncientsWest";
  oldTerrain.drawInstanced=true;oldTerrain.heightmapPixelError=5;oldTerrain.basemapDistance=1000;

  var existingEast=world.Cast<Transform>().FirstOrDefault(t=>t.name=="Archipelago of the Ancients East - LINKED EXTENSION");
  if(existingEast)UnityEngine.Object.DestroyImmediate(existingEast.gameObject);
  var eastRoot=new GameObject("Archipelago of the Ancients East - LINKED EXTENSION");eastRoot.transform.SetParent(world,false);eastRoot.transform.position=new Vector3(1536,0,-1024);
  var etg=Terrain.CreateTerrainGameObject(ed);etg.name="Terrain_ArchipelagoOfTheAncientsEast";etg.transform.SetParent(eastRoot.transform,false);etg.transform.localPosition=new Vector3(-256,TerrainY,-256);
  var eastTerrain=etg.GetComponent<Terrain>();eastTerrain.drawInstanced=true;eastTerrain.heightmapPixelError=5;eastTerrain.basemapDistance=1000;
  // Keep the single coherent linked-water system. Patch only the expanded
  // Archipelago rectangle so stale one-tile cutouts cannot survive.
  PatchGlobalWater(oldTerrain,eastTerrain);

  var wg=new GameObject("Region Dressing - Expanded Reference");wg.transform.SetParent(west,false);
  var eg=new GameObject("Region Dressing - Expanded Reference");eg.transform.SetParent(eastRoot.transform,false);
  int primary=0,secondary=0,skipped=0;

  // Move the standalone player/camera with the enlarged island layout.
  var player=west.Find("Player - Third Person");var camera=west.Find("Player Camera");
  if(!alreadySplit&&player){var np=ExpandPos(player.position,oldCenter);var tt=np.x<1280?oldTerrain:eastTerrain;np.y=Ground(tt,np.x,np.z)+1.35f;player.SetParent(np.x<1280?west:eastRoot.transform,true);player.position=np;}
  if(!alreadySplit&&camera){var np=ExpandPos(camera.position,oldCenter);var tt=np.x<1280?oldTerrain:eastTerrain;np.y=Mathf.Max(np.y,Ground(tt,np.x,np.z)+5f);camera.SetParent(np.x<1280?west:eastRoot.transform,true);camera.position=np;}
  if(world.name.Contains("13 EXTENSION TILES"))world.name=world.name.Replace("13 EXTENSION TILES","14 EXTENSION TILES");

  // Verify the newly-created West/East join numerically.
  var wa=wd.GetHeights(512,0,1,513);var ea=ed.GetHeights(0,0,1,513);float join=0;
  for(int z=0;z<513;z++)join=Mathf.Max(join,Mathf.Abs((TerrainY+wa[z,0]*TerrainH)-(TerrainY+ea[z,0]*TerrainH)));
  if(join>.001f||westSeam>.095f)throw new Exception($"Archipelago seam failure external={westSeam} internal={join}");
  EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save West/East Archipelago");
  AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/ArchipelagoExpansion20261001");
  File.WriteAllText("Validation/EdgeGrid20260923/ArchipelagoExpansion20261001/build.txt",$"PASS westLandPct={westLand:F2} eastLandPct={eastLand:F2} externalWestNorthSeamMaxM={westSeam:F6} westEastJoinMaxM={join:F6} dressingPrimary={primary} dressingSecondary={secondary} dressingSkipped={skipped} terrainCountDelta=+1\n");
  Debug.Log($"ARCHIPELAGO_WEST_EAST_DONE westLand={westLand:F1} eastLand={eastLand:F1} join={join:F6} primary={primary} secondary={secondary}");
 }
}