using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;

public static class MMNorthMaterialSeamBlend20261006
{
 const string ReportDir="Validation/EdgeGrid20260923/NorthReference20261006";
 const float R=48f;
 static readonly string[] Paths={
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SweetWater_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/Kriegspire_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/FrozenHighlands_NorthTerrain.asset",
  "Assets/World/WorldExtensions/Generated/NorthReference20261001/SilverCove_NorthTerrain.asset"};

 static float S(float a,float b,float v){float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));return t*t*(3f-2f*t);}
 static bool Snow(TerrainLayer l){if(!l)return false;string n=l.name.ToLowerInvariant();string p=l.diffuseTexture?AssetDatabase.GetAssetPath(l.diffuseTexture).ToLowerInvariant():"";return n.Contains("snow")||p.Contains("snow");}
 static bool Rock(TerrainLayer l){if(!l)return false;string n=l.name.ToLowerInvariant();string p=l.diffuseTexture?AssetDatabase.GetAssetPath(l.diffuseTexture).ToLowerInvariant():"";return n.Contains("volcan")||n.Contains("rock")||p.Contains("ash_")||p.Contains("rocky_terrain");}
 static float Ratio(TerrainData d,float[,,] a,int z,int x){float s=0,r=0;for(int k=0;k<d.alphamapLayers;k++){if(Snow(d.terrainLayers[k]))s+=a[z,x,k];else if(Rock(d.terrainLayers[k]))r+=a[z,x,k];}return s/Mathf.Max(.0001f,s+r);}
 static bool SetRatio(TerrainData d,float[,,] a,int z,int x,float desired,float blend){
  float s=0,r=0;var si=new List<int>();var ri=new List<int>();
  for(int k=0;k<d.alphamapLayers;k++){if(Snow(d.terrainLayers[k])){s+=a[z,x,k];si.Add(k);}else if(Rock(d.terrainLayers[k])){r+=a[z,x,k];ri.Add(k);}}
  float total=s+r;if(total<.05f)return false;
  float targetS=total*desired,targetR=total*(1-desired),ns=Mathf.Lerp(s,targetS,blend),nr=Mathf.Lerp(r,targetR,blend);
  if(s>.0001f)foreach(int k in si)a[z,x,k]*=ns/s;else if(si.Count>0)a[z,x,si[0]]=ns;
  if(r>.0001f)foreach(int k in ri)a[z,x,k]*=nr/r;else if(ri.Count>0)a[z,x,ri[0]]=nr;
  return Mathf.Abs(ns-s)>.0001f||Mathf.Abs(nr-r)>.0001f;
 }

 [MenuItem("MMUnity/Reference 2026/Blend Northern Internal Snow Rock Materials")]
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  Directory.CreateDirectory(ReportDir);string report=ReportDir+"/material_seam_blend.txt";
  if(File.Exists(report))throw new Exception("Northern material seam blend already applied; refusing cumulative rerun.");
  var d=new TerrainData[4];var a=new float[4][,,];
  for(int i=0;i<4;i++){d[i]=AssetDatabase.LoadAssetAtPath<TerrainData>(Paths[i]);if(!d[i])throw new Exception("Missing terrain "+Paths[i]);a[i]=d[i].GetAlphamaps(0,0,d[i].alphamapWidth,d[i].alphamapHeight);}
  int changed=0;
  for(int seam=0;seam<3;seam++){
   var wd=d[seam];var ed=d[seam+1];
   int rows=Mathf.Min(wd.alphamapHeight,ed.alphamapHeight);
   int wxSample=Mathf.Clamp(Mathf.RoundToInt((512f-R)/512f*(wd.alphamapWidth-1)),0,wd.alphamapWidth-1);
   int exSample=Mathf.Clamp(Mathf.RoundToInt(R/512f*(ed.alphamapWidth-1)),0,ed.alphamapWidth-1);
   int wBand=Mathf.CeilToInt(R/512f*(wd.alphamapWidth-1)),eBand=Mathf.CeilToInt(R/512f*(ed.alphamapWidth-1));
   for(int z=0;z<rows;z++){
    float wz=768f+z/(float)Mathf.Max(1,rows-1)*512f;if(wz<650f)continue;
    int wzA=Mathf.RoundToInt(z/(float)Mathf.Max(1,rows-1)*(wd.alphamapHeight-1)),ezA=Mathf.RoundToInt(z/(float)Mathf.Max(1,rows-1)*(ed.alphamapHeight-1));
    float wr=Ratio(wd,a[seam],wzA,wxSample),er=Ratio(ed,a[seam+1],ezA,exSample);
    for(int n=0;n<=wBand;n++){int x=wd.alphamapWidth-1-n;float dx=-(n/(float)Mathf.Max(1,wBand))*R;float target=Mathf.Lerp(wr,er,S(-R,R,dx));float blend=(1f-S(0,R,Mathf.Abs(dx)))*.92f;if(SetRatio(wd,a[seam],wzA,x,target,blend))changed++;}
    for(int n=0;n<=eBand;n++){int x=n;float dx=(n/(float)Mathf.Max(1,eBand))*R;float target=Mathf.Lerp(wr,er,S(-R,R,dx));float blend=(1f-S(0,R,Mathf.Abs(dx)))*.92f;if(SetRatio(ed,a[seam+1],ezA,x,target,blend))changed++;}
   }
  }
  for(int i=0;i<4;i++){d[i].SetAlphamaps(0,0,a[i]);d[i].SetBaseMapDirty();EditorUtility.SetDirty(d[i]);}
  AssetDatabase.SaveAssets();
  File.WriteAllLines(report,new[]{"PASS heightsUntouched=true terrainLayersUntouched=true bandM="+R,"changedPixels="+changed,"pairs=3"});
  Debug.Log("NORTH_MATERIAL_SEAM_BLEND_20261006_DONE changedPixels="+changed);
 }
}