using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Linked-world only: blend EAST/WEST height normals of the four ORIGINAL-source
// copies. Never move a boundary vertex, coastal water, or canonical TerrainData.
public static class MMNorthLateralHeightTangent20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthLateralTangent_20260926\manifest.json";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));
  return t*t*(3f-2f*t);
 }
 static float OriginalAt(float[,] a,float x,float z){
  x=Mathf.Clamp(x,0f,512f);z=Mathf.Clamp(z,0f,512f);
  int ix=Mathf.FloorToInt(x),iz=Mathf.FloorToInt(z);
  int xx=Mathf.Min(512,ix+1),zz=Mathf.Min(512,iz+1);
  float tx=x-ix,tz=z-iz;
  return Mathf.Lerp(Mathf.Lerp(a[iz,ix],a[iz,xx],tx),
    Mathf.Lerp(a[zz,ix],a[zz,xx],tx),tz)*320f;
 }
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Linked source tangent backup missing");
  var scene=SceneManager.GetActiveScene();
  if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty)
    throw new Exception("Saved linked scene required");
  var maps=N.Select(n=>AssetDatabase.LoadAssetAtPath<TerrainData>(
    G+n+"_LinkedNorthProfile.asset")).ToArray();
  if(maps.Any(d=>!d||d.heightmapResolution!=513||d.alphamapLayers!=7))
   throw new Exception("Missing generated linked source terrain");
  var h=maps.Select(d=>d.GetHeights(0,0,513,513)).ToArray();
  var old=h.Select(a=>(float[,])a.Clone()).ToArray();
  var stats=new List<string>();float maximumMove=0;int adjusted=0;
  for(int pair=0;pair<3;pair++){
   var a=h[pair];var b=h[pair+1];
   var am=maps[pair].GetAlphamaps(0,0,512,512);
   var bm=maps[pair+1].GetAlphamaps(0,0,512,512);
   var difBefore=new List<float>();var difAfter=new List<float>();
   int skippedWater=0,skippedRoad=0,changedRows=0;
   // Native source boundaries contain isolated high-frequency height spikes.
   // Applying their RAW derivative mismatch row-by-row creates artificial
   // horizontal terracing. Filter the requested displacement ALONG Z first.
   var raw=new float[513];var eligible=new bool[513];
   for(int z=5;z<507;z++){
    if(Mathf.Min(-24f+320f*a[z,512],-24f+320f*b[z,0])<1f)continue;
    if(Mathf.Abs((a[z,512]-b[z,0])*320f)>.02f)continue;
    if(am[z,509,6]>.30f||bm[z,2,6]>.30f)continue;
    float l=(a[z,512]-a[z,511])*320f;
    float r=(b[z,1]-b[z,0])*320f;
    if(Mathf.Abs(l-r)<.14f)continue;
    raw[z]=Mathf.Clamp(.5f*(l-r),-1.3f,1.3f);
    eligible[z]=true;
   }
   for(int z=5;z<507;z++){
    float ya=-24f+320f*a[z,512],yb=-24f+320f*b[z,0];
    if(Mathf.Min(ya,yb)<1f){skippedWater++;continue;}
    if(Mathf.Abs(ya-yb)>.02f)
     throw new Exception("Existing x height border not aligned "+N[pair]+" z="+z);
    if(am[z,509,6]>.30f||bm[z,2,6]>.30f){skippedRoad++;continue;}
    float sl=(a[z,512]-a[z,511])*320f;
    float sr=(b[z,1]-b[z,0])*320f;
    float jump=sl-sr;
    float smooth=S(5f,30f,z)*S(5f,30f,512f-z);
    if(!eligible[z]||smooth<.001f)continue;
    float smoothed=0f,weight=0f;
    for(int dz=-18;dz<=18;dz++){
     int zz=z+dz;if(zz<5||zz>=507||!eligible[zz])continue;
     float w=Mathf.Exp(-.5f*dz*dz/(6f*6f));
     smoothed+=raw[zz]*w;weight+=w;
    }
    if(weight<.001f)continue;
    smoothed/=weight;
    // Never make an opposing slope correction or overshoot native relief.
    if(Mathf.Sign(raw[z])!=Mathf.Sign(smoothed))continue;
    float delta=Mathf.Sign(raw[z])*Mathf.Min(Mathf.Abs(raw[z]),
      Mathf.Abs(smoothed))*smooth;
    if(Mathf.Abs(delta)<.025f)continue;
    bool safe=true;
    for(int d=1;d<=9;d++){
     float bump=delta*d*Mathf.Exp(-.5f*Mathf.Pow((d-1)/1.9f,2f));
     if(-24f+320f*a[z,512-d]+bump<.15f||
        -24f+320f*b[z,d]+bump<.15f){safe=false;break;}
    }
    if(!safe)continue;
    difBefore.Add(Mathf.Abs(jump));
    for(int d=1;d<=9;d++){
     float bump=delta*d*Mathf.Exp(-.5f*Mathf.Pow((d-1)/1.9f,2f));
     a[z,512-d]+=bump/320f;b[z,d]+=bump/320f;
     maximumMove=Mathf.Max(maximumMove,Mathf.Abs(bump));
     if(Mathf.Abs(bump)>.002f)adjusted+=2;
    }
    float newSl=(a[z,512]-a[z,511])*320f;
    float newSr=(b[z,1]-b[z,0])*320f;
    difAfter.Add(Mathf.Abs(newSl-newSr));changedRows++;
   }
   if(difAfter.Count==0)throw new Exception("No lateral source rows adjusted "+pair);
   difBefore.Sort();difAfter.Sort();
   stats.Add(N[pair]+"-"+N[pair+1]+" dryAdjusted="+changedRows+
    " maxBefore="+difBefore.Last().ToString("F4")+
    " maxAfter="+difAfter.Last().ToString("F4")+
    " p95Before="+difBefore[(int)((difBefore.Count-1)*.95f)].ToString("F4")+
    " p95After="+difAfter[(int)((difAfter.Count-1)*.95f)].ToString("F4")+
    " skippedWater="+skippedWater+" skippedRoad="+skippedRoad);
   if(difAfter.Last()>difBefore.Last()+.0001f)
    throw new Exception("Lateral tangent profile worsened "+N[pair]);
  }
  if(maximumMove>2.7f||adjusted<300)
    throw new Exception("Unsafe lateral source adjustment maxMove="+maximumMove+
      " modifiedVertices="+adjusted);
  // Assert exact immutable tile edges and original south/north tile joins.
  float border=0;
  for(int i=0;i<4;i++)for(int z=0;z<513;z++){
   border=Mathf.Max(border,Mathf.Abs(h[i][z,0]-old[i][z,0])*320f);
   border=Mathf.Max(border,Mathf.Abs(h[i][z,512]-old[i][z,512])*320f);
  }
  for(int i=0;i<4;i++)for(int x=0;x<513;x++){
   border=Mathf.Max(border,Mathf.Abs(h[i][0,x]-old[i][0,x])*320f);
   border=Mathf.Max(border,Mathf.Abs(h[i][512,x]-old[i][512,x])*320f);
  }
  if(border>.00001f)throw new Exception("Modified canonical/shared boundary "+border);
  for(int i=0;i<4;i++){maps[i].SetHeights(0,0,h[i]);EditorUtility.SetDirty(maps[i]);}
  int regrounded=0;var processed=new HashSet<Transform>();
  var terrains=scene.GetRootGameObjects().SelectMany(r=>
    r.GetComponentsInChildren<Terrain>(true)).ToArray();
  foreach(var t in terrains){
   int i=Array.IndexOf(maps,t.terrainData);
   if(i<0)continue;
   var scope=t.transform.parent?t.transform.parent:t.transform;
   foreach(var g in scope.GetComponentsInChildren<Transform>(true).Where(tr=>
     tr.name=="Vegetation - Source Anchored Final"||
     tr.name=="Vegetation - Preserved User Additions"||
     tr.name=="Region Dressing - Reference Matched")){
    foreach(Transform tree in g){
     if(!processed.Add(tree))continue;
     float xx=tree.position.x-t.transform.position.x;
     float zz=tree.position.z-t.transform.position.z;
     if(xx<0f||xx>512f||zz<5f||zz>507f||
        (xx>11f&&xx<501f))continue;
     float before=OriginalAt(old[i],xx,zz);
     float after=t.terrainData.GetInterpolatedHeight(xx/512f,zz/512f);
     float dy=after-before;
     if(Mathf.Abs(dy)<.003f)continue;
     if(Mathf.Abs(dy)>3.1f)throw new Exception("Vegetation adjustment too large "+dy);
     tree.position+=Vector3.up*dy;regrounded++;
    }
   }
  }
  AssetDatabase.SaveAssets();
  if(regrounded>0){
   EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save regrounded linked scene");
  }
  File.WriteAllLines(V+"north_lateral_tangent_repair_20260926.txt",stats.Concat(
    new[]{"SUMMARY adjustedVertices="+adjusted+" largestDisplacementM="+
    maximumMove.ToString("F3")+" protectedEdgeErrorM="+border.ToString("F6")+
    " reGroundedSourceModels="+regrounded+
    " canonicalTerrainWrites=0 canonicalSourceScenesUntouched=true"}));
 }
}
