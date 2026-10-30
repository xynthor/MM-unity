using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthSourceSlopeHotspotAudit20260925{
 const string V="Validation/EdgeGrid20260923/",G="Assets/World/WorldExtensions/Generated/";
 static readonly string[] N={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthSourceSlopeHotspotAudit20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_SLOPE_HOTSPOTS.flag"))return;
  File.Delete(V+"RUN_NORTH_SLOPE_HOTSPOTS.flag");
  var lines=new List<string>();
  try{
   var ts=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
   foreach(string n in N){
    var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
    var ext=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
    var linked=AssetDatabase.LoadAssetAtPath<TerrainData>(G+"LinkedSourceTransitions/"+n+"_LinkedNorthProfile.asset");
    if(!original||!ext||!linked||!ts.Any(t=>t.terrainData==linked))
     throw new Exception("Linked terrain not active "+n);
    var o=original.GetHeights(0,0,513,513);
    var a=linked.GetHeights(0,511,513,2);
    var b=ext.GetHeights(0,0,513,17);
    var maskImg=new Texture2D(2,2);maskImg.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+n+"_North.png"));
    var mask=maskImg.GetPixels32();UnityEngine.Object.DestroyImmediate(maskImg);
    var cols=new List<KeyValuePair<float,string>>();
    float maxDry=0;int dry=0,notDry=0,high=0;
    for(int x=8;x<=504;x++){
     float edge=-24f+320f*a[1,x],inside=-24f+320f*o[496,x];
     float jump=Mathf.Abs(((a[1,x]-a[0,x])-(b[1,x]-b[0,x]))*320f);
     int auth=mask[16*513+x].r;
     bool dryLand=auth>=147&&edge>.15f&&inside>=3f;
     if(dryLand){dry++;maxDry=Mathf.Max(maxDry,jump);}else notDry++;
     if(jump>.3f)high++;
     cols.Add(new KeyValuePair<float,string>(jump,"x="+x+" jump_mPerM="+jump.ToString("F3")+
       " photoLand16="+auth+" authorityDry="+dryLand+
       " source512Y="+(-24f+320f*o[512,x]).ToString("F2")+
       " source72mY="+inside.ToString("F2")+
       " sourceLastSlope="+((o[512,x]-o[511,x])*320f).ToString("F2")+
       " linkedLastSlope="+((a[1,x]-a[0,x])*320f).ToString("F2")+
       " extFirstSlope="+((b[1,x]-b[0,x])*320f).ToString("F2")+
       " ext16Y="+(-24f+320f*b[16,x]).ToString("F2")));
    }
    lines.Add(n+" photoConfirmedDry="+dry+" unmasked="+notDry+
      " maxPhotoLandSlopeJump="+maxDry.ToString("F3")+" colsJumpAbove0.3="+high);
    lines.AddRange(cols.OrderByDescending(x=>x.Key).Take(18).Select(x=>"  "+x.Value));
   }
  }catch(Exception e){lines.Add("FAIL "+e);}
  File.WriteAllLines(V+"north_slope_hotspots_postC1_20260925.txt",lines);
 }
}

// final narrow ridge preserving join QA refresh

// post 22:46 saved-scene QA refresh

// post slope QA recompile 20260925
