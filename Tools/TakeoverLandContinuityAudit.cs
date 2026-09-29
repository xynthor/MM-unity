using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
internal class CommandScript : IRunCommand {
 public void Execute(ExecutionResult result) {
  var ts=Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
  var grid=new Dictionary<Vector2Int,Terrain>();
  int duplicates=0,badSize=0,badPosition=0,invalidWeights=0,holes=0,joins=0;
  float worstHeight=0,worstNormal=0,worstSum=0,worstMaterial=0;
  foreach(var t in ts) {
   var p=t.transform.position; var d=t.terrainData;
   var cell=new Vector2Int(Mathf.RoundToInt((p.x+1792)/512),Mathf.RoundToInt((p.z+1280)/512));
   if(grid.ContainsKey(cell))duplicates++;else grid.Add(cell,t);
   if(d.size.x!=512||d.size.z!=512)badSize++;
   if(Mathf.Abs(p.x-(-1792+cell.x*512))>.001f||Mathf.Abs(p.z-(-1280+cell.y*512))>.001f||p.y!=-24)badPosition++;
   var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
   for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++) {
    float sum=0;for(int k=0;k<a.GetLength(2);k++){float w=a[z,x,k];if(float.IsNaN(w)||w<0||w>1)invalidWeights++;sum+=w;}
    worstSum=Mathf.Max(worstSum,Mathf.Abs(sum-1));
   }
  }
  for(int z=0;z<5;z++)for(int x=0;x<6;x++)if(!grid.ContainsKey(new Vector2Int(x,z)))holes++;
  foreach(var pair in grid)foreach(var dir in new[]{Vector2Int.right,Vector2Int.up}) {
   Terrain b;if(!grid.TryGetValue(pair.Key+dir,out b))continue;
   var a=pair.Value;var ad=a.terrainData;var bd=b.terrainData;bool east=dir.x==1;
   joins++;float gap=0,norm=0,mat=0;
   for(int i=0;i<=512;i++) {
    float v=i/512f;float ax=east?1:v,az=east?v:1,bx=east?0:v,bz=east?v:0;
    gap=Mathf.Max(gap,Mathf.Abs(a.transform.position.y+ad.GetInterpolatedHeight(ax,az)-b.transform.position.y-bd.GetInterpolatedHeight(bx,bz)));
    if(i%16==0 && a.transform.position.y+ad.GetInterpolatedHeight(ax,az)>.2f && b.transform.position.y+bd.GetInterpolatedHeight(bx,bz)>.2f)norm=Mathf.Max(norm,Vector3.Angle(ad.GetInterpolatedNormal(ax,az),bd.GetInterpolatedNormal(bx,bz)));
   }
   var aa=ad.GetAlphamaps(east?511:0,east?0:511,east?1:512,east?512:1);
   var ba=bd.GetAlphamaps(0,0,east?1:512,east?512:1);
   for(int i=0;i<512;i++) {
    float v=i/511f; if(a.transform.position.y+ad.GetInterpolatedHeight(east?1:v,east?v:1)<=.2f || b.transform.position.y+bd.GetInterpolatedHeight(east?0:v,east?v:0)<=.2f)continue; var weights=new Dictionary<Texture2D,float>();
    for(int k=0;k<ad.alphamapLayers;k++){var tex=ad.terrainLayers[k].diffuseTexture;if(!tex)continue;float prior;weights.TryGetValue(tex,out prior);weights[tex]=prior+aa[east?i:0,east?0:i,k];}
    for(int k=0;k<bd.alphamapLayers;k++){var tex=bd.terrainLayers[k].diffuseTexture;if(!tex)continue;float prior;weights.TryGetValue(tex,out prior);weights[tex]=prior-ba[east?i:0,east?0:i,k];}
    foreach(var w in weights.Values)mat=Mathf.Max(mat,Mathf.Abs(w));
   }
   worstHeight=Mathf.Max(worstHeight,gap);worstNormal=Mathf.Max(worstNormal,norm);worstMaterial=Mathf.Max(worstMaterial,mat);
   result.Log("JOIN "+pair.Key+" "+dir+" heightM="+gap.ToString("F5")+" normalDeg="+norm.ToString("F2")+" diffuseWeightGap="+mat.ToString("F4"));
  }
  result.Log("LAND_ONLY_CONTINUITY SUMMARY tiles="+ts.Length+" duplicate="+duplicates+" holes="+holes+" badSize="+badSize+" badPosition="+badPosition+" joins="+joins+" worstHeightM="+worstHeight.ToString("F5")+" worstNormalDeg="+worstNormal.ToString("F2")+" invalidWeights="+invalidWeights+" maxWeightSumError="+worstSum.ToString("F6")+" worstDiffuseWeightGap="+worstMaterial.ToString("F4"));
 }
}

