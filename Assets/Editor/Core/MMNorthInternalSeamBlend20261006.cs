using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMNorthInternalSeamBlend20261006
{
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
 static readonly float[] Seams={-768f,-256f,256f};
 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static float Ground(Terrain t,TerrainData d,float x,float z){
  float u=Mathf.Clamp01((x-t.transform.position.x)/512f),v=Mathf.Clamp01((z-t.transform.position.z)/512f);
  return t.transform.position.y+d.GetInterpolatedHeight(u,v);
 }

 [MenuItem("MMUnity/Reference 2026/Blend Northern Internal Height Derivatives")]
 public static void Run()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  if(scene.isDirty)throw new Exception("Enroth opened dirty.");
  Directory.CreateDirectory(ReportDir);
  string reportPath=ReportDir+"/internal_seam_blend.txt";
  if(File.Exists(reportPath))throw new Exception("Northern internal seam blend already applied; refusing cumulative rerun.");

  var active=Terrain.activeTerrains;
  var live=new Terrain[Paths.Length];
  var data=new TerrainData[Paths.Length];
  var source=new float[Paths.Length][,];
  for(int i=0;i<Paths.Length;i++){
   data[i]=AssetDatabase.LoadAssetAtPath<TerrainData>(Paths[i]);
   if(!data[i])throw new Exception("Missing terrain "+Paths[i]);
   live[i]=active.FirstOrDefault(t=>t.terrainData==data[i]);
   if(!live[i])throw new Exception("Terrain not live "+Paths[i]);
   source[i]=data[i].GetHeights(0,0,513,513);
  }

  Func<float,float,float> sourceY=(wx,wz)=>{
   for(int i=0;i<live.Length;i++){
    var p=live[i].transform.position;
    if(wx>=p.x-.01f&&wx<=p.x+512.01f&&wz>=p.z-.01f&&wz<=p.z+512.01f){
     int ix=Mathf.Clamp(Mathf.RoundToInt(wx-p.x),0,512);
     int iz=Mathf.Clamp(Mathf.RoundToInt(wz-p.z),0,512);
     return p.y+source[i][iz,ix]*data[i].size.y;
    }
   }
   return -99f;
  };

  int totalChanged=0;float maxMove=0f;
  const float R=56f;
  foreach(float seam in Seams){
   for(int i=0;i<live.Length;i++){
    var t=live[i];var d=data[i];var p=t.transform.position;
    if(seam<p.x-R||seam>p.x+512f+R)continue;
    var h=d.GetHeights(0,0,513,513);int changed=0;
    for(int z=0;z<513;z++){
     float wz=p.z+z;if(wz<650f)continue;
     float zw=S(650f,710f,wz);
     for(int x=0;x<513;x++){
      float wx=p.x+x,dx=wx-seam;if(Mathf.Abs(dx)>R)continue;
      float current=p.y+h[z,x]*d.size.y;
      float left=sourceY(seam-R,wz),right=sourceY(seam+R,wz);
      float target=Mathf.Lerp(left,right,S(-R,R,dx));
      float w=(1f-S(0f,R,Mathf.Abs(dx)))*.88f*zw;
      float ny=Mathf.Lerp(current,target,w);
      float mv=Mathf.Abs(ny-current);
      if(mv>.01f){
       h[z,x]=Mathf.Clamp01((ny-p.y)/d.size.y);
       changed++;totalChanged++;maxMove=Mathf.Max(maxMove,mv);
      }
     }
    }
    if(changed>0){d.SetHeights(0,0,h);EditorUtility.SetDirty(d);}
   }
  }
  AssetDatabase.SaveAssets();

  float worst=0f;int joins=0;
  var all=Terrain.activeTerrains;
  for(int ai=0;ai<all.Length;ai++)for(int bi=ai+1;bi<all.Length;bi++){
   var a=all[ai];var b=all[bi];var ap=a.transform.position;var bp=b.transform.position;
   if(Mathf.Abs(ap.z-bp.z)<.1f&&Mathf.Abs(ap.x+512f-bp.x)<.1f){
    float m=0;for(int z=0;z<=512;z+=4)m=Mathf.Max(m,Mathf.Abs(Ground(a,a.terrainData,ap.x+512,ap.z+z)-Ground(b,b.terrainData,bp.x,bp.z+z)));
    worst=Mathf.Max(worst,m);joins++;
   }
   if(Mathf.Abs(ap.x-bp.x)<.1f&&Mathf.Abs(ap.z+512f-bp.z)<.1f){
    float m=0;for(int x=0;x<=512;x+=4)m=Mathf.Max(m,Mathf.Abs(Ground(a,a.terrainData,ap.x+x,ap.z+512)-Ground(b,b.terrainData,bp.x+x,bp.z)));
    worst=Mathf.Max(worst,m);joins++;
   }
  }

  var lines=new[]{
   "changedVertices="+totalChanged,
   "maxHeightMoveM="+maxMove.ToString("F3"),
   "sampledAdjacentJoins="+joins,
   "worstHeightGapM="+worst.ToString("F6"),
   "sceneDirty="+scene.isDirty
  };
  File.WriteAllLines(reportPath,lines);
  Debug.Log("NORTH_INTERNAL_SEAM_BLEND_20261006_DONE "+string.Join(" | ",lines));
 }
}