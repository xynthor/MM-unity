using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMReferenceGeoSiteProbe20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static MMReferenceGeoSiteProbe20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_REFERENCE_GEO_SITE_PROBE.flag"))return;
  File.Delete(V+"RUN_REFERENCE_GEO_SITE_PROBE.flag");
  var lines=new List<string>();
  try{
   string[] names={"SweetWater_North","Kriegspire_North","FrozenHighlands_North","ParadiseValley_West"};
   int[][] positions={new[]{462,263,390,345,365,215},new[]{189,331,250,310,370,230},new[]{200,300,310,360},new[]{370,420,415,300,440,90}};
   for(int i=0;i<names.Length;i++){
    var td=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/"+names[i]+"Terrain.asset");
    if(!td){lines.Add(names[i]+" MISSING");continue;}
    var h=td.GetHeights(0,0,513,513);var a=td.GetAlphamaps(0,0,512,512);
    lines.Add("ZONE "+names[i]+" layers="+string.Join(";",Array.ConvertAll(td.terrainLayers,l=>l?l.name:"NULL")));
    for(int j=0;j<positions[i].Length;j+=2){
     int x=positions[i][j],z=positions[i][j+1];float min=999,max=-999;int wet=0;
     for(int zz=Mathf.Max(0,z-23);zz<=Mathf.Min(512,z+23);zz++)
      for(int xx=Mathf.Max(0,x-23);xx<=Mathf.Min(512,x+23);xx++){
       float y=-24f+320f*h[zz,xx];min=Mathf.Min(min,y);max=Mathf.Max(max,y);
       if(y<=.1f)wet++;
      }
     var weights=new string[td.alphamapLayers];
     for(int k=0;k<weights.Length;k++)weights[k]=a[Mathf.Min(511,z),Mathf.Min(511,x),k].ToString("F3");
     lines.Add("  x="+x+" z="+z+" y="+(-24f+320f*h[z,x]).ToString("F2")+
       " neighborhoodMin="+min.ToString("F2")+" max="+max.ToString("F2")+
       " wetRadius23="+wet+" splats="+string.Join(",",weights));
    }
   }
  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"reference_geo_site_probe_20260925.txt",lines);
 }
}
