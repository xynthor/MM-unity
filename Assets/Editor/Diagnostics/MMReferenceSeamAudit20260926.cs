using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
[InitializeOnLoad]
internal static class MMReferenceSeamAudit20260926 {
 const string V="Validation/EdgeGrid20260923/";
 static MMReferenceSeamAudit20260926(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_REFERENCE_SEAM_AUDIT_20260926.flag"))return;
  File.Delete(V+"RUN_REFERENCE_SEAM_AUDIT_20260926.flag");
  var lines=new List<string>();
  try{
   foreach(var n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
    var s=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var e=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/"+n+"_NorthTerrain.asset");
    var sa=s.GetAlphamaps(0,511,512,1);var a=e.GetAlphamaps(0,0,512,512);
    int snow=Array.FindIndex(e.terrainLayers,l=>l&&l.name.Contains("Snow"));
    int vol=Array.FindIndex(e.terrainLayers,l=>l&&l.name.Contains("Volcanic"));
    var means=new double[5];var src=new double[2];int[] zs={0,4,16,32,64};
    for(int x=0;x<512;x++){src[0]+=sa[0,x,snow];src[1]+=sa[0,x,vol];}
    for(int i=0;i<zs.Length;i++)for(int x=0;x<512;x++)means[i]+=a[zs[i],x,snow];
    lines.Add(n+" snow_src="+(src[0]/512).ToString("F3")+" rock_src="+(src[1]/512).ToString("F3")+
      " snow_z0_4_16_32_64="+string.Join("/",Array.ConvertAll(means,q=>(q/512).ToString("F3")))+
      " layersMatch="+(s.terrainLayers.Length==e.terrainLayers.Length));
   }
   var d=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset");
   var p=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset");
   var dh=d.GetHeights(0,0,513,97);var ph=p.GetHeights(0,480,513,33);
   float height=0,jump=0,curvature=0;int nValid=0;
   for(int x=12;x<501;x++){
    height=Mathf.Max(height,Mathf.Abs(dh[0,x]-ph[32,x])*320);
    float ds=(dh[1,x]-dh[0,x])*320,ps=(ph[32,x]-ph[31,x])*320;
    jump=Mathf.Max(jump,Mathf.Abs(ds-ps));
    curvature=Mathf.Max(curvature,Mathf.Abs((dh[2,x]-2*dh[1,x]+dh[0,x])-(ph[32,x]-2*ph[31,x]+ph[30,x]))*320);
    nValid++;
   }
   lines.Add("DragonSouth ParadiseWest heightMax_m="+height.ToString("F3")+" slopeJumpMax="+jump.ToString("F3")+" curvature="+curvature.ToString("F3")+" xSamples="+nValid);
   var da=d.GetAlphamaps(0,0,512,64);var pa=p.GetAlphamaps(0,448,512,64);
   lines.Add("DragonSouth layers="+string.Join(",",Array.ConvertAll(d.terrainLayers,l=>l.name)));
   lines.Add("ParadiseWest layers="+string.Join(",",Array.ConvertAll(p.terrainLayers,l=>l.name)));
   for(int x=0;x<=512;x+=32){
    int q=Mathf.Min(511,x);
    string blend=string.Join("/",System.Linq.Enumerable.Select(
      System.Linq.Enumerable.Range(0,p.alphamapLayers),k=>pa[63,q,k].ToString("F2")));
    lines.Add("WEST_BORDER x="+x+" y_m="+(-24f+320f*dh[0,x]).ToString("F2")+
      " dragonIn8_16_32_64_96_m="+string.Join("/",new[]{8,16,32,64,96}.Select(z=>(-24f+320f*dh[z,x]).ToString("F2")))+
      " paradiseIn8_m="+(-24f+320f*ph[24,x]).ToString("F2")+" pWeights="+blend);
   }

  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"reference_seam_audit_20260926.txt",lines);
 }
}

// post visual reconstruction QA refresh

// second-pass final QA

// final photo biome audit 20260926

// final photo fade render reload
