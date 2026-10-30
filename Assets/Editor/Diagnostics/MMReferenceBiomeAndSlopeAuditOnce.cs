using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
[InitializeOnLoad]
internal static class MMReferenceBiomeAndSlopeAuditOnce {
 const string Dir="Validation/EdgeGrid20260923/";
 static MMReferenceBiomeAndSlopeAuditOnce(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(Dir+"RUN_REFERENCE_BIOME_AUDIT.flag"))return;
  File.Delete(Dir+"RUN_REFERENCE_BIOME_AUDIT.flag");
  var sb=new StringBuilder();
  try {
   var n=new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove","EelInfestedWaters"};
   for(int t=0;t<5;t++) {
    var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n[t]+"/Generated/"+n[t]+"Terrain.asset");
    var ext=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/"+n[t]+"_NorthTerrain.asset");
    if(!src||!ext)throw new Exception("North terrain missing "+n[t]);
    var a=src.GetHeights(0,0,513,513);var b=ext.GetHeights(0,0,513,513);
    sb.AppendLine("TILE "+n[t]+"_North LAYERS="+string.Join(";",ext.terrainLayers.Select(l=>l?l.name:"NULL")));
    foreach(int x in new[]{0,64,128,192,256,320,384,448,512}) {
     string line="x="+x+" source[-16,-8,-2,-1,0]=";
     foreach(int z in new[]{496,504,510,511,512})line+=(-24f+320f*a[z,x]).ToString("F1")+",";
     line+=" ext[0,1,4,8,12,16,24,32,48,64,96,128]=";
     foreach(int z in new[]{0,1,4,8,12,16,24,32,48,64,96,128})line+=(-24f+320f*b[z,x]).ToString("F1")+",";
     sb.AppendLine(line);
    }
   }
   foreach(string name in new[]{"ParadiseValley_West","DragonIsle_North","DragonIsle_South"}){
    string asset=name.StartsWith("Dragon")?"Assets/World/DragonIsle/Generated/"+name.Replace("_","")+"Terrain.asset":
      "Assets/World/WorldExtensions/Generated/"+name+"Terrain.asset";
    var td=AssetDatabase.LoadAssetAtPath<TerrainData>(asset);
    if(!td){sb.AppendLine(name+" MISSING "+asset);continue;}
    sb.AppendLine(name+" LAYERS="+string.Join(";",td.terrainLayers.Select(l=>l?l.name:"NULL")));
    var h=td.GetHeights(0,0,513,513);var a=td.GetAlphamaps(0,0,512,512);
    foreach(int z in new[]{64,192,320,448})foreach(int x in new[]{64,192,320,448}){
     var w=Enumerable.Range(0,td.alphamapLayers).Select(k=>a[z,x,k].ToString("F2")).ToArray();
     sb.AppendLine(name+" x="+x+" z="+z+" worldHeight="+(-24f+320f*h[z,x]).ToString("F2")+
       " weights="+string.Join(",",w));
    }
   }
  } catch(Exception ex){sb.AppendLine("ERROR="+ex);}
  File.WriteAllText(Dir+"reference_biome_and_north_slope_live_qa.txt",sb.ToString());
  Debug.Log("REFERENCE_BIOME_SLOPE_AUDIT_WRITTEN");
 }
}

// QA refresh post canopy, 20260925 20:14

// post north 20260925 micro polish QA reload

// read-only west-layer verification 20260927 2300

// post-forest layer-name verification, 20260927
