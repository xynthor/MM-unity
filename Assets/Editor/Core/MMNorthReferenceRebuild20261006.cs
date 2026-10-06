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
 const string ReportDir="Validation/EdgeGrid20260923/NorthReferenceRebuild20261006";
 const string WaterObject="Linked connected sea and existing lowland waters 20260929";
 static readonly string[] Paths={
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/b2adcca80c21fb542bd822ac5a7b75cf.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/9a29f5601247e3f4ba63f7800b155bea.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/d85d07a7edcfde747bfd4436baa76b47.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/1a659482bbf7f2e4bb722f6f5a3bbf78.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"};

 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static float G(float x,float z,float cx,float cz,float rx,float rz){float dx=(x-cx)/rx,dz=(z-cz)/rz;return Mathf.Exp(-1.8f*(dx*dx+dz*dz));}
 static float Peak(float x,float z,float cx,float cz,float rx,float rz,float h,float phase){
   float g=G(x,z,cx,cz,rx,rz);
   float n0=(Mathf.PerlinNoise((x+2300f+phase*113f)*.028f,(z+900f-phase*71f)*.028f)-.5f);
   float n1=(Mathf.PerlinNoise((x+600f-phase*41f)*.055f,(z+1500f+phase*29f)*.055f)-.5f);
   return h*Mathf.Pow(g,.80f)*(1f+n0*.24f+n1*.10f);
 }
 static float Channel(float x,float z,float baseX,float width,float z0,float z1,float wiggle,float phase){
   if(z<z0||z>z1)return 0f;float cx=baseX+Mathf.Sin((z-z0)*.016f+phase)*wiggle+Mathf.Sin((z-z0)*.006f+phase*.4f)*wiggle*.45f;
   return (1f-S(width*.30f,width,Mathf.Abs(x-cx)))*S(z0,z0+48,z)*(1f-S(z1-60,z1,z));
 }
 static float NorthLimit(float x){
   float z=1095f+65f*Mathf.Sin((x+720f)*.0055f)+32f*Mathf.Sin((x-150f)*.014f);
   z+=95f*G(x,0,-320,0,360,1);z-=65f*G(x,0,-1080,0,190,1);z-=90f*G(x,0,560,0,170,1);
   return Mathf.Clamp(z,955f,1230f);
 }
 static float LandSupport(float x,float z){
   float m=0f;
   m=Mathf.Max(m,G(x,z,-1080,730,235,185));m=Mathf.Max(m,G(x,z,-850,835,285,230));m=Mathf.Max(m,G(x,z,-520,900,360,280));
   m=Mathf.Max(m,G(x,z,-160,900,390,285));m=Mathf.Max(m,G(x,z,170,855,315,250));m=Mathf.Max(m,G(x,z,465,760,245,185));
   m=Mathf.Max(m,G(x,z,-510,1080,330,210));m=Mathf.Max(m,G(x,z,-170,1105,370,210));m=Mathf.Max(m,G(x,z,140,1035,300,205));
   m=Mathf.Max(m,1f-S(645f,785f,z));
   float land=S(.17f,.44f,m);
   float westBay=Channel(x,z,-1045f,92f,815f,1060f,48f,.3f),eastBay=Channel(x,z,575f,86f,735f,960f,34f,1.3f);
   land*=1f-.86f*westBay;land*=1f-.90f*eastBay;land*=1f-S(NorthLimit(x)-26f,NorthLimit(x)+18f,z);
   land*=S(-1278f,-1234f,x)*(1f-S(704f,758f,x));return Mathf.Clamp01(land);
 }
 static float Mountains(float x,float z){
   float m=0f;
   m=Mathf.Max(m,Peak(x,z,-1110,705,76,86,51,.2f));m=Mathf.Max(m,Peak(x,z,-1015,775,82,92,60,.5f));m=Mathf.Max(m,Peak(x,z,-930,850,86,95,66,.8f));m=Mathf.Max(m,Peak(x,z,-860,925,82,88,60,1.1f));
   m=Mathf.Max(m,Peak(x,z,-785,995,92,92,72,1.4f));m=Mathf.Max(m,Peak(x,z,-695,1065,92,88,83,1.7f));m=Mathf.Max(m,Peak(x,z,-600,1120,94,86,91,2.0f));
   m=Mathf.Max(m,Peak(x,z,-500,1160,96,82,98,2.3f));m=Mathf.Max(m,Peak(x,z,-390,1188,98,82,104,2.6f));m=Mathf.Max(m,Peak(x,z,-270,1175,90,82,93,2.9f));m=Mathf.Max(m,Peak(x,z,-145,1145,88,84,82,3.2f));
   m=Mathf.Max(m,Peak(x,z,-5,1110,86,88,76,3.5f));m=Mathf.Max(m,Peak(x,z,125,1060,88,92,79,3.8f));m=Mathf.Max(m,Peak(x,z,245,1005,84,92,76,4.1f));m=Mathf.Max(m,Peak(x,z,350,935,82,90,70,4.4f));m=Mathf.Max(m,Peak(x,z,455,855,78,84,62,4.7f));m=Mathf.Max(m,Peak(x,z,555,775,72,78,51,5.0f));
   m*=1f-.42f*G(x,z,-905,890,70,64);m*=1f-.46f*G(x,z,-650,1035,70,60);m*=1f-.40f*G(x,z,-330,1110,70,56);m*=1f-.42f*G(x,z,70,1040,66,58);m*=1f-.46f*G(x,z,330,900,68,60);
   return m;
 }
 static float WaterCarve(float x,float z){
   float lakeA=G(x,z,-70,958,150,42),lakeB=G(x,z,35,978,128,42),bend=G(x,z,88,1018,90,54),river=Channel(x,z,105f,24f,995f,1145f,22f,.55f);
   float body=Mathf.Max(lakeA,Mathf.Max(lakeB,bend));return Mathf.Clamp01(Mathf.Max(S(.26f,.66f,body),.72f*river));
 }
 static float CityProtect(float x,float z){float p=0f;p=Mathf.Max(p,G(x,z,-1024,512,175,150));p=Mathf.Max(p,G(x,z,-512,512,175,150));p=Mathf.Max(p,G(x,z,0,512,195,170));p=Mathf.Max(p,G(x,z,512,512,185,155));return S(.18f,.65f,p);}
 static float NewHeight(float x,float z){
   float land=LandSupport(x,z),n0=(Mathf.PerlinNoise((x+1900f)*.007f,(z+650f)*.007f)-.5f)*5.2f,n1=(Mathf.PerlinNoise((x+350f)*.020f,(z+1550f)*.020f)-.5f)*1.8f;
   float y=5.0f+n0+n1+Mountains(x,z);
   float plain=G(x,z,-70,820,245,150),rolling=10f+5f*(Mathf.PerlinNoise((x+900f)*.006f,(z+120f)*.006f)-.5f);y=Mathf.Lerp(y,Mathf.Min(y,rolling+7f),.38f*plain);
   float east=G(x,z,590,760,165,140);y=Mathf.Lerp(y,Mathf.Min(y,11f+n0*.2f),.48f*east);y=Mathf.Lerp(-9f,y,S(.18f,.61f,land));y=Mathf.Lerp(y,-3.5f,WaterCarve(x,z));return y;
 }
 static float SnowStart(float x){float z=668f;z-=120f*G(x,0,0,0,310,1);z+=28f*G(x,0,-1030,0,225,1);z+=35f*G(x,0,555,0,210,1);z+=28f*Mathf.Sin((x+250f)*.008f);return z;}
 static int Layer(TerrainData d,string exact,string fallback=null){for(int i=0;i<d.terrainLayers.Length;i++){var l=d.terrainLayers[i];if(!l)continue;if(l.name==exact||(fallback!=null&&l.name.IndexOf(fallback,StringComparison.OrdinalIgnoreCase)>=0))return i;}return -1;}
 static float Ground(Terrain t,TerrainData d,float x,float z){float u=Mathf.Clamp01((x-t.transform.position.x)/d.size.x),v=Mathf.Clamp01((z-t.transform.position.z)/d.size.z);return t.transform.position.y+d.GetInterpolatedHeight(u,v);}
 static Terrain Find(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+t.terrainData.size.x+.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+t.terrainData.size.z+.01f);}

 [MenuItem("MMUnity/Reference 2026/Rebuild North From User Reference")]
 public static void Run(){
   var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);if(scene.isDirty)throw new Exception("Enroth must open clean.");
   Directory.CreateDirectory(ReportDir);string reportPath=ReportDir+"/apply.txt";if(File.Exists(reportPath))throw new Exception("North reference rebuild already applied; refusing cumulative rerun.");

   var active=Terrain.activeTerrains;var ts=new Terrain[Paths.Length];var data=new TerrainData[Paths.Length];var oldH=new float[Paths.Length][,];
   for(int i=0;i<Paths.Length;i++){data[i]=AssetDatabase.LoadAssetAtPath<TerrainData>(Paths[i]);ts[i]=active.FirstOrDefault(t=>t.terrainData==data[i]);if(!data[i]||!ts[i])throw new Exception("Missing live terrain "+Paths[i]);oldH[i]=data[i].GetHeights(0,0,513,513);}
   int changed=0,painted=0;
   for(int i=0;i<ts.Length;i++){
     var t=ts[i];var d=data[i];var h=d.GetHeights(0,0,513,513);
     for(int z=0;z<513;z++)for(int x=0;x<513;x++){
       float wx=t.transform.position.x+x,wz=t.transform.position.z+z;if(wz<555f)continue;float old=t.transform.position.y+h[z,x]*d.size.y;
       float blend=S(555f,770f,wz)*(1f-.93f*CityProtect(wx,wz));float target=Mathf.Lerp(old,NewHeight(wx,wz),blend);float side=S(-1280f,-1265f,wx)*(1f-S(753f,768f,wx));target=Mathf.Lerp(old,target,side);
       if(Mathf.Abs(target-old)>.015f){h[z,x]=Mathf.Clamp01((target-t.transform.position.y)/d.size.y);changed++;}
     }d.SetHeights(0,0,h);
     var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int L=d.alphamapLayers;int snow=Layer(d,"Realistic_Snow","Snow"),rock=Layer(d,"Realistic_Volcanic","Volcanic");if(rock<0)rock=Layer(d,"Trace_Rock","Rock");if(snow<0||rock<0)throw new Exception("Cold terrain layers missing "+Paths[i]);
     for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){float wx=t.transform.position.x+x,wz=t.transform.position.z+z,start=SnowStart(wx);if(wz<start-65f)continue;float cold=S(start-65f,start+80f,wz);float u=x/(float)(d.alphamapWidth-1),v=z/(float)(d.alphamapHeight-1),y=t.transform.position.y+d.GetInterpolatedHeight(u,v),slope=d.GetSteepness(u,v);float rw=y<0?.96f:Mathf.Lerp(.06f,.84f,S(15f,40f,slope)),sw=1-rw;var target=new float[L];target[snow]=sw;target[rock]=rw;float sum=0,w=.995f*cold;for(int k=0;k<L;k++){a[z,x,k]=Mathf.Lerp(a[z,x,k],target[k],w);sum+=a[z,x,k];}if(sum>.0001f)for(int k=0;k<L;k++)a[z,x,k]/=sum;painted++;}d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
   }

   // extend existing coherent world water only into cells newly submerged by this rebuild
   var waterTr=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==WaterObject);
   if(!waterTr)throw new Exception("Connected world water object missing.");var wf=waterTr.GetComponent<MeshFilter>();if(!wf||!wf.sharedMesh)throw new Exception("Connected water mesh missing.");var wm=wf.sharedMesh;
   const float x1=-1280f,x2=768f,z1=540f,z2=1280f,step=2f;int W=Mathf.CeilToInt((x2-x1)/step),H=Mathf.CeilToInt((z2-z1)/step);var wet=new bool[W*H];var draw=new bool[W*H];int newlyWet=0;
   for(int iz=0;iz<H;iz++)for(int ix=0;ix<W;ix++){float wx=x1+(ix+.5f)*step,wz=z1+(iz+.5f)*step;var t=Find(ts,wx,wz);if(!t)continue;int qi=Array.IndexOf(ts,t);int hx=Mathf.Clamp(Mathf.RoundToInt(wx-t.transform.position.x),0,512),hz=Mathf.Clamp(Mathf.RoundToInt(wz-t.transform.position.z),0,512);float oldY=t.transform.position.y+oldH[qi][hz,hx]*data[qi].size.y,newY=Ground(t,data[qi],wx,wz);bool nw=oldY>Water+.05f&&newY<Water-.05f;wet[iz*W+ix]=nw;draw[iz*W+ix]=nw;if(nw)newlyWet++;}
   for(int iz=0;iz<H;iz++)for(int ix=0;ix<W;ix++)if(wet[iz*W+ix])for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){int xx=ix+dx,zz=iz+dz;if(xx>=0&&xx<W&&zz>=0&&zz<H)draw[zz*W+xx]=true;}
   var ov=wm.vertices;var ot=wm.triangles;var verts=new List<Vector3>(ov.Length+40000);verts.AddRange(ov);var tris=new List<int>(ot.Length+60000);tris.AddRange(ot);var rects=new List<Vector4>();var activeRuns=new Dictionary<long,int>();
   for(int z=0;z<H;z++){var next=new Dictionary<long,int>();for(int x=0;x<W;){if(!draw[z*W+x]){x++;continue;}int st=x;while(x<W&&draw[z*W+x])x++;long key=((long)st<<32)|(uint)x;if(activeRuns.TryGetValue(key,out int idx)){var rr=rects[idx];rr.w=z+1;rects[idx]=rr;next[key]=idx;}else{next[key]=rects.Count;rects.Add(new Vector4(st,z,x,z+1));}}activeRuns=next;}
   foreach(var rr in rects){float ax=x1+rr.x*step,bx=x1+rr.z*step,az=z1+rr.y*step,bz=z1+rr.w*step;if(bx-ax<.001f||bz-az<.001f)continue;int n=verts.Count;verts.Add(waterTr.InverseTransformPoint(new Vector3(ax,Water+.015f,az)));verts.Add(waterTr.InverseTransformPoint(new Vector3(ax,Water+.015f,bz)));verts.Add(waterTr.InverseTransformPoint(new Vector3(bx,Water+.015f,bz)));verts.Add(waterTr.InverseTransformPoint(new Vector3(bx,Water+.015f,az)));tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
   wm.Clear();wm.indexFormat=IndexFormat.UInt32;wm.SetVertices(verts);wm.SetTriangles(tris,0);wm.RecalculateNormals();wm.RecalculateBounds();wm.name=wm.name+"_NorthReference20261006";EditorUtility.SetDirty(wm);

   // re-ground existing production vegetation; remove trees/ecosystem props that now sit in water
   int grounded=0,disabled=0;var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   foreach(var tr in all){
     var p=tr.position;if(p.x<-1280||p.x>768||p.z<555||p.z>1280)continue;string n=tr.name;bool candidate=n.StartsWith("SourceTree_",StringComparison.OrdinalIgnoreCase)||n.StartsWith("Eco_",StringComparison.OrdinalIgnoreCase);if(!candidate)continue;
     var terr=Find(ts,p.x,p.z);if(!terr)continue;float gy=Ground(terr,terr.terrainData,p.x,p.z);
     if(gy<=Water+.15f){if(tr.gameObject.activeSelf){tr.gameObject.SetActive(false);disabled++;}continue;}
     var rs=tr.GetComponentsInChildren<Renderer>(false);if(rs.Length==0)continue;Bounds b=rs[0].bounds;for(int k=1;k<rs.Length;k++)b.Encapsulate(rs[k].bounds);float gap=b.min.y-gy;if(Mathf.Abs(gap)>.05f){tr.position=new Vector3(p.x,p.y-gap,p.z);grounded++;}
   }
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save Enroth.");AssetDatabase.SaveAssets();

   var lines=new[]{
     "PASS reference-first north V6 persisted",
     "changedHeightVertices="+changed,
     "paintedAlphaPixels="+painted,
     "newlyWet2mCells="+newlyWet,
     "waterRectangles="+rects.Count,
     "waterVertices="+wm.vertexCount,
     "vegetationGrounded="+grounded,
     "vegetationDisabledInWater="+disabled,
     "sceneDirtyAfterSave="+scene.isDirty};
   File.WriteAllLines(reportPath,lines);Debug.Log("NORTH_REFERENCE_REBUILD_20261006_DONE "+string.Join(" | ",lines));
 }
}