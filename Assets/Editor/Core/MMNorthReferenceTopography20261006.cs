using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMNorthReferenceTopography20261006
{
 const float Water=.10f;
 const string WaterObject="Linked connected sea and existing lowland waters 20260929";
 const string ReportDir="Validation/EdgeGrid20260923/NorthReference20261006";
 static readonly string[] Paths={
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/b2adcca80c21fb542bd822ac5a7b75cf.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/9a29f5601247e3f4ba63f7800b155bea.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/d85d07a7edcfde747bfd4436baa76b47.asset",
  "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/1a659482bbf7f2e4bb722f6f5a3bbf78.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"
 };
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static float G(float x,float z,float cx,float cz,float rx,float rz){float dx=(x-cx)/rx,dz=(z-cz)/rz;return Mathf.Exp(-1.7f*(dx*dx+dz*dz));}
 static float Fjord(float x,float z,float baseX,float width,float startZ,float wiggle){if(z<startZ)return 0f;float cx=baseX+Mathf.Sin((z-startZ)*.017f)*wiggle+Mathf.Sin((z-startZ)*.006f)*wiggle*.5f;return (1f-S(width*.34f,width,Mathf.Abs(x-cx)))*S(startZ,startZ+95f,z);}
 static int Layer(TerrainData d,string exact,string fallback=null){for(int i=0;i<d.terrainLayers.Length;i++){var l=d.terrainLayers[i];if(!l)continue;if(l.name==exact||(fallback!=null&&l.name.IndexOf(fallback,StringComparison.OrdinalIgnoreCase)>=0))return i;}return -1;}
 static Terrain FindTerrain(Terrain[] ts,float x,float z){return ts.FirstOrDefault(t=>x>=t.transform.position.x-.01f&&x<=t.transform.position.x+512.01f&&z>=t.transform.position.z-.01f&&z<=t.transform.position.z+512.01f);}
 static float Ground(Terrain t,TerrainData d,float x,float z){float u=Mathf.Clamp01((x-t.transform.position.x)/512f),v=Mathf.Clamp01((z-t.transform.position.z)/512f);return t.transform.position.y+d.GetInterpolatedHeight(u,v);}

 [MenuItem("MMUnity/Reference 2026/Rebuild Northern Topography From Latest Reference")]
 public static void Run()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Enroth opened dirty.");

  var waterTr=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==WaterObject);
  if(!waterTr)throw new Exception("Global connected water object missing.");
  var waterFilter=waterTr.GetComponent<MeshFilter>();
  if(!waterFilter||!waterFilter.sharedMesh)throw new Exception("Global connected water mesh missing.");
  var waterMesh=waterFilter.sharedMesh;
  if(waterMesh.name.EndsWith("_North20261006",StringComparison.Ordinal))throw new Exception("Northern 20261006 topography already applied.");

  var active=Terrain.activeTerrains;
  var ts=new Terrain[Paths.Length];var data=new TerrainData[Paths.Length];var oldHeights=new float[Paths.Length][,];
  for(int i=0;i<Paths.Length;i++){
   data[i]=AssetDatabase.LoadAssetAtPath<TerrainData>(Paths[i]);if(!data[i])throw new Exception("Missing terrain "+Paths[i]);
   ts[i]=active.FirstOrDefault(t=>t.terrainData==data[i]);if(!ts[i])throw new Exception("Terrain not live "+Paths[i]);
   oldHeights[i]=data[i].GetHeights(0,0,513,513);
  }
  Func<float,float,float> OY=(wx,wz)=>{
   for(int i=0;i<ts.Length;i++){var p=ts[i].transform.position;if(wx>=p.x-.01f&&wx<=p.x+512.01f&&wz>=p.z-.01f&&wz<=p.z+512.01f){int ix=Mathf.Clamp(Mathf.RoundToInt(wx-p.x),0,512),iz=Mathf.Clamp(Mathf.RoundToInt(wz-p.z),0,512);return p.y+oldHeights[i][iz,ix]*data[i].size.y;}}
   return -99f;
  };

  const int SX=513;var shore=new float[SX];
  for(int ix=0;ix<SX;ix++){float wx=-1280f+ix*4f,found=768f;for(float wz=1280f;wz>=768f;wz-=4f){if(OY(wx,wz)>.25f){found=wz;break;}}shore[ix]=found;}

  var report=new List<string>();int totalChanged=0;
  for(int i=0;i<ts.Length;i++){
   var t=ts[i];var d=data[i];var h=d.GetHeights(0,0,513,513);int changed=0;float maxMove=0f;
   for(int z=0;z<513;z++)for(int x=0;x<513;x++){
    float wx=t.transform.position.x+x,wz=t.transform.position.z+z;if(wz<650f)continue;
    float side=S(0f,20f,wx+1280f)*S(0f,20f,768f-wx);if(side<=.0001f)continue;
    float y=t.transform.position.y+h[z,x]*d.size.y,target=y;int si=Mathf.Clamp(Mathf.RoundToInt((wx+1280f)/4f),0,SX-1);

    if(wz>=680f&&wz<=865f){
     float ym32=OY(wx,wz-32f),ym16=OY(wx,wz-16f),yp16=OY(wx,wz+16f),yp32=OY(wx,wz+32f);
     float avg=(ym32+2f*ym16+3f*y+2f*yp16+yp32)/9f;
     float grad=Mathf.Abs(yp16-ym16)/32f;
     float seamBand=S(680f,730f,wz)*(1f-S(835f,865f,wz));
     float steepW=S(.28f,.72f,grad)*.86f*seamBand;
     target=Mathf.Lerp(target,avg,steepW);
     float pass=Mathf.Max(G(wx,wz,-1085f,775f,115f,85f),G(wx,wz,365f,780f,120f,90f));
     float passFloor=8.5f+2f*Mathf.PerlinNoise((wx+1400f)*.015f,(wz+200f)*.015f);
     target=Mathf.Lerp(target,Mathf.Min(target,passFloor),.72f*pass*seamBand);
    }

    if(wz>=835f&&y>.15f){
     float trim=58f+30f*Mathf.PerlinNoise((wx+1900f)*.0048f,.37f)+18f*G(wx,1000f,-520f,1000f,430f,500f);
     float newCoast=Mathf.Max(880f,shore[si]-trim);
     float coastCut=S(newCoast-24f,newCoast+10f,wz);
     float f1=Fjord(wx,wz,-865f,72f,955f,26f),f2=Fjord(wx,wz,120f,66f,965f,20f);
     float lake=G(wx,wz,-535f,1005f,70f,52f);
     float river=(1f-S(12f,27f,Mathf.Abs(wx-(-520f+Mathf.Sin((wz-940f)*.025f)*18f))))*S(930f,975f,wz)*(1f-S(1080f,1115f,wz));
     float carve=Mathf.Clamp01(Mathf.Max(coastCut,Mathf.Max(.94f*f1,Mathf.Max(.92f*f2,Mathf.Max(.96f*S(.36f,.70f,lake),.68f*river)))));
     if(carve>.001f)target=Mathf.Lerp(target,-7.4f,carve);
    }
    target=Mathf.Lerp(y,target,side);float mv=Mathf.Abs(target-y);
    if(mv>.02f){h[z,x]=Mathf.Clamp01((target-t.transform.position.y)/d.size.y);changed++;maxMove=Mathf.Max(maxMove,mv);}
   }
   d.SetHeights(0,0,h);

   var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int L=d.alphamapLayers;
   int snow=Layer(d,"Realistic_Snow","Snow"),rock=Layer(d,"Realistic_Volcanic","Rock");if(rock<0)rock=Layer(d,"Trace_Rock","Volcanic");if(snow<0||rock<0)throw new Exception("Snow/rock layers missing "+Paths[i]);
   int painted=0;
   for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
    float wx=t.transform.position.x+x,wz=t.transform.position.z+z;if(wz<620f)continue;
    float edge=S(0f,6f,wx+1280f)*S(0f,6f,768f-wx),cold=S(620f,705f,wz)*edge;if(cold<=0)continue;
    float u=x/(float)(d.alphamapWidth-1),v=z/(float)(d.alphamapHeight-1);float yy=t.transform.position.y+d.GetInterpolatedHeight(u,v),slope=d.GetSteepness(u,v);
    float rw;if(yy<-.2f)rw=.97f;else if(yy<3f)rw=.48f;else rw=Mathf.Lerp(.10f,.86f,S(14f,40f,slope));
    float sw=1f-rw;var target=new float[L];target[snow]=sw;target[rock]=rw;float sum=0,w=.992f*cold;
    for(int k=0;k<L;k++){a[z,x,k]=Mathf.Lerp(a[z,x,k],target[k],w);sum+=a[z,x,k];}
    if(sum>.00001f)for(int k=0;k<L;k++)a[z,x,k]/=sum;painted++;
   }
   d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
   totalChanged+=changed;report.Add(Path.GetFileName(Paths[i])+" changedVertices="+changed+" maxHeightMoveM="+maxMove.ToString("F2")+" paintedPixels="+painted);
  }

  const float x1=-1280f,x2=768f,z1=600f,z2=1280f,step=2f;
  int W=Mathf.CeilToInt((x2-x1)/step),H=Mathf.CeilToInt((z2-z1)/step);var wet=new bool[W*H];var draw=new bool[W*H];int newlyWet=0;
  for(int iz=0;iz<H;iz++)for(int ix=0;ix<W;ix++){
   float wx=x1+(ix+.5f)*step,wz=z1+(iz+.5f)*step;var t=FindTerrain(ts,wx,wz);if(!t)continue;int qi=Array.IndexOf(ts,t);
   float oldY=t.transform.position.y+oldHeights[qi][Mathf.Clamp(Mathf.RoundToInt(wz-t.transform.position.z),0,512),Mathf.Clamp(Mathf.RoundToInt(wx-t.transform.position.x),0,512)]*data[qi].size.y;
   float newY=Ground(t,data[qi],wx,wz);bool nw=oldY>Water+.05f&&newY<Water-.05f;wet[iz*W+ix]=nw;draw[iz*W+ix]=nw;if(nw)newlyWet++;
  }
  for(int iz=0;iz<H;iz++)for(int ix=0;ix<W;ix++)if(wet[iz*W+ix])for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++){int xx=ix+dx,zz=iz+dz;if(xx>=0&&xx<W&&zz>=0&&zz<H)draw[zz*W+xx]=true;}

  var ov=waterMesh.vertices;var ot=waterMesh.triangles;var verts=new List<Vector3>(ov.Length+30000);verts.AddRange(ov);var tris=new List<int>(ot.Length+45000);tris.AddRange(ot);
  Action<float,float,float,float> Quad=(ax,az,bx,bz)=>{if(bx-ax<.001f||bz-az<.001f)return;int n=verts.Count;verts.Add(waterTr.InverseTransformPoint(new Vector3(ax,Water+.015f,az)));verts.Add(waterTr.InverseTransformPoint(new Vector3(ax,Water+.015f,bz)));verts.Add(waterTr.InverseTransformPoint(new Vector3(bx,Water+.015f,bz)));verts.Add(waterTr.InverseTransformPoint(new Vector3(bx,Water+.015f,az)));tris.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});};
  var activeRuns=new Dictionary<long,int>();var rects=new List<Vector4>();
  for(int z=0;z<H;z++){var next=new Dictionary<long,int>();for(int x=0;x<W;){if(!draw[z*W+x]){x++;continue;}int start=x;while(x<W&&draw[z*W+x])x++;long key=((long)start<<32)|(uint)x;if(activeRuns.TryGetValue(key,out int idx)){var r=rects[idx];r.w=z+1;rects[idx]=r;next[key]=idx;}else{next[key]=rects.Count;rects.Add(new Vector4(start,z,x,z+1));}}activeRuns=next;}
  foreach(var r in rects)Quad(x1+r.x*step,z1+r.y*step,x1+r.z*step,z1+r.w*step);
  waterMesh.Clear();waterMesh.indexFormat=IndexFormat.UInt32;waterMesh.SetVertices(verts);waterMesh.SetTriangles(tris,0);waterMesh.RecalculateNormals();waterMesh.RecalculateBounds();waterMesh.name=waterMesh.name+"_North20261006";EditorUtility.SetDirty(waterMesh);
  AssetDatabase.SaveAssets();

  float worst=0f;int joins=0;var all=Terrain.activeTerrains;
  for(int ai=0;ai<all.Length;ai++)for(int bi=ai+1;bi<all.Length;bi++){
   var aT=all[ai];var bT=all[bi];var ap=aT.transform.position;var bp=bT.transform.position;
   if(Mathf.Abs(ap.z-bp.z)<.1f&&Mathf.Abs(ap.x+512f-bp.x)<.1f){float m=0;for(int z=0;z<=512;z+=4)m=Mathf.Max(m,Mathf.Abs(Ground(aT,aT.terrainData,ap.x+512,ap.z+z)-Ground(bT,bT.terrainData,bp.x,bp.z+z)));worst=Mathf.Max(worst,m);joins++;}
   if(Mathf.Abs(ap.x-bp.x)<.1f&&Mathf.Abs(ap.z+512f-bp.z)<.1f){float m=0;for(int x=0;x<=512;x+=4)m=Mathf.Max(m,Mathf.Abs(Ground(aT,aT.terrainData,ap.x+x,ap.z+512)-Ground(bT,bT.terrainData,bp.x+x,bp.z)));worst=Mathf.Max(worst,m);joins++;}
  }
  report.Add("newlyWetCells2m="+newlyWet+" waterRects="+rects.Count+" waterVerts="+waterMesh.vertexCount);
  report.Add("sampledAdjacentJoins="+joins+" worstHeightGapM="+worst.ToString("F6"));
  report.Add("sceneDirty="+scene.isDirty+" totalChangedVertices="+totalChanged);
  Directory.CreateDirectory(ReportDir);File.WriteAllLines(ReportDir+"/apply.txt",report);
  Debug.Log("NORTH_REFERENCE_TOPOGRAPHY_20261006_DONE "+string.Join(" | ",report));
 }
}