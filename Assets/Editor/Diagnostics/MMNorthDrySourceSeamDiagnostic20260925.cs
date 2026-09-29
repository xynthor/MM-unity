using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
[InitializeOnLoad]
internal static class MMNorthDrySourceSeamDiagnostic20260925 {
 const string V="Validation/EdgeGrid20260923/";
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static MMNorthDrySourceSeamDiagnostic20260925(){EditorApplication.delayCall+=Run;}
 static void Run(){
  if(!File.Exists(V+"RUN_NORTH_DRY_SLOPE_DIAGNOSTIC.flag"))return;
  if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Run;return;}
  File.Delete(V+"RUN_NORTH_DRY_SLOPE_DIAGNOSTIC.flag");
  var report=new List<string>();
  try{
   report.Add("active_scene="+SceneManager.GetActiveScene().path+" dirty="+SceneManager.GetActiveScene().isDirty);
   foreach(string name in Names){
    var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+name+"/Generated/"+name+"Terrain.asset");
    var ext=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/WorldExtensions/Generated/"+name+"_NorthTerrain.asset");
    if(!src||!ext)throw new Exception("Missing terrain "+name);
    var a=src.GetHeights(0,511,513,2);var b=ext.GetHeights(0,0,513,33);
    var mask=new Texture2D(2,2);mask.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/"+name+"_North.png"));
    var px=mask.GetPixels32();UnityEngine.Object.DestroyImmediate(mask);
    float worst=0,worstFeasible=0,worstCliff=0;int large=0,feasibleCount=0,cliff=0,wet=0;
    var extremes=new List<string>();
    for(int x=8;x<=504;x++){
     float sourceSlope=(a[1,x]-a[0,x])*320f;
     float actualSlope=(b[1,x]-b[0,x])*320f;
     float jump=Mathf.Abs(actualSlope-sourceSlope);
     float edge=-24f+b[0,x]*320f;
     bool authLand=px[16*513+x].r>=145;
     bool feasible=authLand&&edge>=.15f&&edge+sourceSlope>=.15f&&
       -24f+b[24,x]*320f>=1f;
     bool steep=sourceSlope<-.8f&&edge+sourceSlope<.15f;
     if(jump>.5f)large++;
     if(feasible){worstFeasible=Mathf.Max(worstFeasible,jump);feasibleCount++;}
     if(steep){worstCliff=Mathf.Max(worstCliff,jump);cliff++;}
     if(edge<.15f)wet++;
     worst=Mathf.Max(worst,jump);
     if(jump>1.0f)extremes.Add("x="+x+" edge_m="+edge.ToString("F2")+
      " srcSlope="+sourceSlope.ToString("F2")+" currentSlope="+actualSlope.ToString("F2")+
      " jump="+jump.ToString("F2")+" authorityLand="+authLand+
      " feasible="+feasible+" sourceCliff="+steep);
    }
    report.Add(name+"_North maxSlopeJump="+worst.ToString("F3")+
      " feasibleMax="+worstFeasible.ToString("F3")+" sourceCliffMax="+worstCliff.ToString("F3")+
      " columnsJumpGt0.5="+large+" feasibleColumns="+feasibleCount+
      " steepSourceCliffColumns="+cliff+" wetEdgeColumns="+wet);
    report.AddRange(extremes.OrderByDescending(line=>{
      int i=line.IndexOf(" jump=");if(i<0)return 0f;
      var num=line.Substring(i+6).Split(' ')[0];float.TryParse(num,out float v);return v;
    }).Take(15).Select(s=>"  "+s));
   }
  }catch(Exception e){report.Add("FAILED "+e);}
  File.WriteAllLines(V+"north_dry_source_slope_diagnostic_20260925.txt",report);
  Debug.Log("NORTH_DRY_SOURCE_SLOPE_DIAGNOSTIC "+report[0]);
 }
}

// compiled final geometry QA 20260925
